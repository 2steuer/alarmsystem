using System;
using System.Text;
using AlarmSystem.Common.Logging;
using AlarmSystem.Common.Logging.Material;
using AlarmSystem.Common.Material;
using AlarmSystem.Common.Services;
using AlarmSystem.Plugin.Http.Material;

namespace AlarmSystem.Plugin.Http.Providers
{
    public delegate RequestProcessingResult ProcessRequestStringDelegate(string requestString);

    public class RequestReceiver : HttpServer
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public ProcessRequestStringDelegate ProcessRequestString;

        private string _requestUri = "alarm";

        public string RequestUri
        {
            get { return _requestUri; } 
            set { _requestUri = value; }
            
        }

        private DatabaseService _db;
        private bool _dbEnabled = false;

        public RequestReceiver(int port) : base(port)
        {
            Log.Add(LogLevel.Debug, "TriggerReceiver", "Started server on port " + port + ". No authentication.");
        }

        public RequestReceiver(int port, string username, string password) : this(port)
        {
            EnableAuthentication(username, password);
        }

        public void SetDatabaseConnection(DatabaseService service)
        {
            _db = service;
            _dbEnabled = true;
        }

        public override void Start()
        {
            Log.Add(LogLevel.Info, "TriggerReceiver", "Starting server...");
            base.Start();
        }

        public override void Stop()
        {
            Log.Add(LogLevel.Info, "TriggerReceiver", "Stopping server...");
            base.Stop();
        }

        public void EnableAuthentication(string user, string pass)
        {
            Log.Add(LogLevel.Debug, "TriggerReceiver", "Enabling authentication.");
            RequiresAuthentication = true;
            Username = user;
            Password = pass;
        }

        protected override void RequestHandler(HttpConnectionHandler request)
        {
            Log.Add(LogLevel.Debug, "TriggerReceiver", String.Format("Received request on /{0}", request.RequestUriParts[0]));

            if (request.Method == HttpMethod.Post)
            {
                /*switch (request.RequestUriParts[0])
                {
                    case RequestUri:
                        ProcessRequest(request);
                        break;
                    default:
                        request.ResultCode = HttpResult.NotFound;
                        break;
                }*/
                if (request.RequestUriParts[0].Equals(RequestUri))
                {
                    ProcessRequest(request);
                }
                else
                {
                    request.ResultCode = HttpResult.NotFound;
                }
            }
            else
            {
                request.ResultCode = HttpResult.MethodNotAllowed;
            }
            
        }

        protected override bool CheckCredentials(string username, string password)
        {
            if (RequiresAuthentication)
            {
                bool success = (username.Equals(Username) && password.Equals(Password));

                if (!success)
                {
                    Log.Add(LogLevel.Warning, "TriggerReceiver", String.Format("Failed authentication {0}:{1}", username, password));
                }

                return success;
            }
            else
            {
                return true;
            }
        }

        private void ProcessRequest(HttpConnectionHandler handler)
        {
            try
            {
                int length = int.Parse(handler.GetRequestHeader("Content-Length"));
                byte[] requestBytes = new byte[length];
                handler.RequestStream.Read(requestBytes, 0, length);

                string xml = Encoding.UTF8.GetString(requestBytes);

                var result = ProcessRequestString(xml);

                switch (result)
                {
                        case RequestProcessingResult.Success:
                        handler.ResultCode = HttpResult.Ok;
                        break;

                        case RequestProcessingResult.Error:
                        handler.ResultCode = HttpResult.InternalServerError;
                        break;

                        case RequestProcessingResult.XmlError:
                        handler.ResultCode = HttpResult.BadRequest;
                        break;

                        case RequestProcessingResult.ApiKeyError:
                        handler.ResultCode = HttpResult.Forbidden;
                        break;

                        case RequestProcessingResult.UnknownRequest:
                        handler.ResultCode = HttpResult.NotImplemented;
                        break;
                }

                
            }
            catch (FormatException ex)
            {
                Log.AddException("TriggerReceiver", ex);
                handler.ResultCode = HttpResult.BadRequest;
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.AddException("TriggerReceiver", ex);
                handler.ResultCode = HttpResult.Forbidden;
            }
            catch (Exception ex)
            {
                Log.AddException("TriggerReceiver", ex);
                handler.ResultCode = HttpResult.InternalServerError;
            }
            
        }
    }
}
