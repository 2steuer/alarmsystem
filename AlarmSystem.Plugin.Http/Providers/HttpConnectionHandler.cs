using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;
using AlarmSystem.Common.Logging;
using AlarmSystem.Common.Logging.Material;
using AlarmSystem.Plugin.Http.Material;

namespace AlarmSystem.Plugin.Http.Providers
{
    public delegate void HttpRequestDelegate(HttpConnectionHandler request);

    public class HttpConnectionHandler
    {
        private TcpClient _client;
        private NetworkStream _requestStream;
        private StreamReader _requestStreamReader;

        private MemoryStream _responseStream = new MemoryStream();

        private StreamWriter _responseStreamWriter;

        private Thread _processingThread;
        
        Dictionary<HttpResult, string> _resultStrings = new Dictionary<HttpResult, string>();

        private int _readTimeout = 1000;
        System.Timers.Timer _timout = new System.Timers.Timer();

        // Only one guy is allowed to handle our request - sorry
        public HttpRequestDelegate RequestHandler { get; set; }

        public NetworkStream RequestStream
        {
            get { return _requestStream; }
        }

        public MemoryStream ResponseStream
        {
            get
            {
                
                return _responseStream;
            }
        }

        // Request Stuff
        public HttpMethod Method { get; private set; }
        public string RequestUri { get; private set; }
        public string[] RequestUriParts { get; private set; }

        private Dictionary<string, string> _parameters = new Dictionary<string, string>(); 

        public Dictionary<string, string> Parameters { get { return _parameters; } } 
        public string VersionIdentifier { get; private set; }

        Dictionary<string, string> _requestHeaders = new Dictionary<string, string>();

        private AuthenticateCredentialsDelegate _authenticator;


        // Result Stuff
        private Dictionary<string, string> _responseHeaders = new Dictionary<string, string>(); 
        public HttpResult ResultCode { get; set; }
        
        protected HttpConnectionHandler()
        {
            // init status strings...
            _resultStrings.Add(HttpResult.Ok, "200 OK");
            _resultStrings.Add(HttpResult.Created, "201 Created");
            _resultStrings.Add(HttpResult.NoContent, "204 No Content");

            _resultStrings.Add(HttpResult.BadRequest, "400 Bad Request");
            _resultStrings.Add(HttpResult.Unauthorized, "401 Unauthorized");
            _resultStrings.Add(HttpResult.Forbidden, "403 Forbidden");
            _resultStrings.Add(HttpResult.NotFound, "404 Not Found");
            _resultStrings.Add(HttpResult.MethodNotAllowed, "405 Method Not Allowed");
            _resultStrings.Add(HttpResult.RequestTimeout, "408 Request Time-out");
            _resultStrings.Add(HttpResult.Gone, "410 Gone");
            _resultStrings.Add(HttpResult.LengthRequired, "411 Length Required");
            
            _resultStrings.Add(HttpResult.InternalServerError, "500 Internal Server Error");
            _resultStrings.Add(HttpResult.NotImplemented, "501 Not Implemented");
            _resultStrings.Add(HttpResult.ServiceUnavailable, "503 Service Unavailable");
            _resultStrings.Add(HttpResult.VersionNotSupported, "505 HTTP Version not supported");

            _timout.Interval = _readTimeout;
            _timout.AutoReset = false;
            _timout.Elapsed += _timout_Elapsed;
        }
        
        public HttpConnectionHandler(TcpClient client)
            :this()
        {
            _client = client;
            _requestStream = client.GetStream();
            
            _processingThread = new Thread(ProcessingThread);
        }

        public HttpConnectionHandler(TcpClient client, AuthenticateCredentialsDelegate authenticator)
            :this(client)
        {
            _authenticator = authenticator;
        }

        public void StartProcessing()
        {
            _processingThread.Start();
        }

        public bool RequestHasHeader(string header)
        {
            return _requestHeaders.ContainsKey(header);
        }

        public string GetRequestHeader(string headerName)
        {
            if (_requestHeaders.ContainsKey(headerName.ToUpper()))
            {
                return _requestHeaders[headerName.ToUpper()];
            }
            
            return "";
            
        }

        public void SetResponseHeader(string header, string value)
        {
            if (_responseHeaders.ContainsKey(header))
            {
                _responseHeaders[header] = value;
            }
            else
            {
                _responseHeaders.Add(header, value);
            }
        }

        private void ProcessingThread()
        {
            try
            {
                DateTime begin = DateTime.Now;
                TimeSpan duration;
                ParseRequestLine();
                ParseHeaders();


                SetResponseHeader("Content-type", "text/plain");
                SetResponseHeader("Content-Length", "0");
                SetResponseHeader("Date", DateTime.Now.ToString());
                SetResponseHeader("Server", "Unknown");
                SetResponseHeader("Connection", "close");

                AuthenticationResult authResult = AuthenticateClient();


                if (authResult != AuthenticationResult.Authenticated)
                {
                    switch (authResult)
                    {
                        case AuthenticationResult.NoCredentials:
                            ResultCode = HttpResult.Unauthorized;
                            break;
                        case AuthenticationResult.AuthenticationError:
                            ResultCode = HttpResult.Forbidden;
                            break;
                        case AuthenticationResult.BadRequest:
                            ResultCode = HttpResult.BadRequest;
                            break;
                        case AuthenticationResult.MethodNotSupported:
                            ResultCode = HttpResult.NotImplemented;
                            break;
                    }
                }
                else //Successfully authenticated
                {
                    if (RequestHandler != null)
                    {
                        RequestHandler(this);
                    }
                    else
                    {
                        ResultCode = HttpResult.NotImplemented;

                    }

                }

                SetResponseHeader("Content-Length", _responseStream.Length.ToString());

                SendStatusLine();
                SendHeaders();

                // We need to always send this - even if we have no payload to send:
                WriteLine(""); // Newline to indicate that the headers are over and if we have any payload it begins now.

                if (_responseStream.Length > 0)
                {
                    _responseStream.WriteTo(_requestStream); // Send raw data from MemoryStream to NetworkStream
                }
                _requestStream.Flush();

                _requestStream.Close();
                _responseStream.Close();
                _client.Close();

                duration = DateTime.Now - begin;
                Log.Add(LogLevel.Verbose, "HTTP", String.Format("Client Handling took {0}ms", duration.TotalMilliseconds));
            }
            catch (Exception ex)
            {
                Log.AddException("HTTP", ex);
            }
            
        }

        private AuthenticationResult AuthenticateClient()
        {
            if (_authenticator == null)
                return AuthenticationResult.Authenticated;

            string headerline = GetRequestHeader("Authorization");

            if (headerline != String.Empty)
            {
                string[] splits = headerline.Split(new []{' '}, 2);

                if (splits.Length == 2)
                {
                    if (splits[0].Trim().ToUpper() == "BASIC")
                    {
                        string cred = Encoding.UTF8.GetString(Convert.FromBase64String(splits[1].Trim()));
                        string[] credSplit = cred.Split(new[] {':'}, 2);

                        if (credSplit.Length == 2)
                        {
                            return _authenticator(credSplit[0], credSplit[1]) ? AuthenticationResult.Authenticated : AuthenticationResult.AuthenticationError;
                        }
                        else
                        {
                            return AuthenticationResult.BadRequest;
                        }
                        
                    }
                    else
                    {
                        return AuthenticationResult.MethodNotSupported;
                    }
                }

                return AuthenticationResult.BadRequest;
            }
            else
            {
                return _authenticator("", "") ? AuthenticationResult.Authenticated : AuthenticationResult.AuthenticationError;
            }
        }

        protected void ParseRequestLine()
        {
            string line = ReadLine();
            string[] splits = line.Split(' ');

            Log.Add(LogLevel.Debug, "HTTP", line);

            if (splits.Length == 3)
            {

                switch (splits[0].ToUpper())
                {
                    case "GET":
                        Method = HttpMethod.Get;
                        break;
                    case "POST":
                        Method = HttpMethod.Post;
                        break;
                    case "PUT":
                        Method = HttpMethod.Put;
                        break;

                    case "DELETE":
                        Method = HttpMethod.Delete;
                        break;

                    default:
                        Method = HttpMethod.Delete;
                        break;
                }

                RequestUri = splits[1];

                string tmpUri = RequestUri;

                if (RequestUri[0] == '/')
                    tmpUri = tmpUri.Substring(1);

                int paramStart = tmpUri.IndexOf('?');

                if (paramStart != -1)
                {
                    string[] uriParam = tmpUri.Split(new []{'?'}, 2);

                    if (uriParam[0][uriParam[0].Length - 1] == '/')
                    {
                        uriParam[0] = uriParam[0].Substring(0, uriParam[0].Length - 1);
                    }

                    RequestUriParts = uriParam[0].Split('/');

                    foreach (string s in uriParam[1].Split('&'))
                    {
                        string[] paramSplits = s.Split('=');
                        Parameters.Add(paramSplits[0], paramSplits[1]);
                    }
                }
                else
                {
                    if (tmpUri[tmpUri.Length - 1] == '/')
                    {
                        tmpUri = tmpUri.Substring(0, tmpUri.Length - 1);
                    }

                    RequestUriParts = tmpUri.Split('/');
                }

                VersionIdentifier = splits[2];
            }
            else
            {
                throw new Exception("Bad Request line in HTTP Request: " + line);
            }
        }

        protected void ParseHeaders()
        {
            string l;

            while ((l = ReadLine()) != "") // Empty line indicates end of headers
            {
                string[] splits = l.Split(new[] {':'}, 2);

                if (splits.Length == 2)
                {
                    _requestHeaders.Add(splits[0].Trim().ToUpper(), splits[1].Trim());
                }
            }
        }

        protected void SendStatusLine()
        {
            WriteLine("HTTP/1.1 " + _resultStrings[ResultCode]);
        }

        protected void SendHeaders()
        {
            foreach (KeyValuePair<string, string> responseHeader in _responseHeaders)
            {
                WriteLine(string.Format("{0}: {1}", responseHeader.Key, responseHeader.Value));
            }
        }

        protected string ReadLine()
        {
            _timout.Start();

            string data = "";
            int last = -1;
            int current = -1;

            while (last != '\r' && current != '\n')
            {
                if (current != -1)
                {
                    _timout.Start();
                    if(current != '\r')
                        data += Convert.ToChar(current);
                    last = current;
                }

                current = _requestStream.ReadByte();
            }

            _timout.Stop();
             // Return only payload, no newline characters!
            return data;
        }

        void _timout_Elapsed(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("{0} timout", _client.Client.RemoteEndPoint);
            _client.Close();
            _processingThread.Abort();
        }

        

        protected void WriteLine(string line)
        {
            line += "\r\n";

            foreach (char c in line)
            {
                _requestStream.WriteByte(Convert.ToByte(c));
            }
        }
    }
}
