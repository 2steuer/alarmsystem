using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AlarmSystem.Plugin.Http.Providers
{
    public delegate bool AuthenticateCredentialsDelegate(string user, string password);

    public abstract class HttpServer
    {
        private TcpListener _listener;
        private Thread _listenThread;
        private bool _runThread;
        private long _connectionCounter = 0;

        public bool Active { get; set; }

        public int Port { get; private set; }

        public bool RequiresAuthentication { get; set; }

        public HttpServer(int port)
        {
            Port = port;
            _listener = new TcpListener(IPAddress.Any, port);
        }

        public HttpServer(int port, bool requiresauthentication)
            :this(port)
        {
            RequiresAuthentication = requiresauthentication;
        }

        public virtual void Start()
        {
            _listenThread = new Thread(Runner);
            _runThread = true;
            _listenThread.Start();
        }

        public virtual void Stop()
        {
            _runThread = false;
        }

        private void Runner()
        {
            Active = true;
            _listener.Start();

            _listener.BeginAcceptTcpClient(new AsyncCallback(AsyncAcceptClient), null);

            while (_runThread)
            {
                Thread.Sleep(10);
            }

            _listener.Stop();
            Active = false;
        }

        private void AsyncAcceptClient(IAsyncResult asyncResult)
        {
            if (!_runThread)
                return;

            TcpClient newClient = _listener.EndAcceptTcpClient(asyncResult);

            HttpConnectionHandler newHandler = new HttpConnectionHandler(newClient, CheckCredentials);
            newHandler.RequestHandler = RequestHandler;
            
            newHandler.StartProcessing();

            _listener.BeginAcceptTcpClient(new AsyncCallback(AsyncAcceptClient), null);
        }

        protected abstract void RequestHandler(HttpConnectionHandler request);

        protected virtual bool CheckCredentials(string username, string password)
        {
            if (RequiresAuthentication)
            {
                return false;
            }

            return true;
        }
    }
}
