using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using AlarmSystem.Common.Logging;
using AlarmSystem.Common.Logging.Material;
using AlarmSystem.Common.Material;
using AlarmSystem.Common.Plugins;
using AlarmSystem.Common.Plugins.Interfaces;
using AlarmSystem.Plugin.Http.Material;
using AlarmSystem.Plugin.Http.Providers;
using MySql.Data.MySqlClient;

namespace AlarmSystem.Plugin.Http
{
    class HttpRequestProcessor : AlarmSystemPluginBase, ITriggerRequestSource
    {
        #region Plugin Information
        public override string PluginName
        {
            get { return "ServerPlugin"; }
        }

        public override string PluginAuthor
        {
            get { return "Merlin Steuer"; }
        }

        public override string PluginDescription
        {
            get { return "A HTTP Request Interface for AlarmSystem"; }
        }

        public override string PluginVersion
        {
            get { return "1.0"; }
        }
        #endregion

        public event TriggerRequestDelegate OnTriggerRequest;

        private RequestReceiver _rcv;

        protected override void InitRoutine()
        {
            int port = GetConfigInteger("Port");
            string uri = GetConfigString("Path");
            bool auth = GetConfigBoolean("Authentication");
            
            _rcv = new RequestReceiver(port);
            _rcv.RequestUri = uri;

            if (auth)
            {
                _rcv.EnableAuthentication(GetConfigString("Credentials", "User"),
                    GetConfigString("Credentials", "Password"));
            }

            _rcv.ProcessRequestString = ProcessRequestString;
        }

        private RequestProcessingResult ProcessRequestString(string requestString)
        {
            Log.Add(LogLevel.Verbose, PluginName, requestString);
            XDocument doc = XDocument.Parse(requestString);
            XElement root = doc.Element("Request");

            if (root == null)
            {
                return RequestProcessingResult.XmlError;
            }

            XElement typeElement = root.Element("Type");
            XElement keyElement = root.Element("ApiKey");

            string source = string.Empty;

            if (typeElement == null)
            {
                return RequestProcessingResult.XmlError;
            }

            if (keyElement == null)
            {
                return RequestProcessingResult.XmlError;
            }

            // Resolve API-Key

            MySqlCommand cmd = DatabaseService.TryCreateCommand();

            string sql = "SELECT name FROM api_keys WHERE `key`=@key";
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("key", keyElement.Value);

            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    source = reader.GetString("name");

                    reader.Close();

                    Log.Add(LogLevel.Verbose, PluginName, string.Format("Found application {0} for key {1}", source, keyElement.Value));
                }
                else
                {
                    Log.Add(LogLevel.Warning, PluginName, "No Application found for key " + keyElement.Value);
                    return RequestProcessingResult.ApiKeyError;
                }
            }

            // Execute Request
            string type = typeElement.Value;

            if (type.Equals("TriggerRequest"))
            {
                XElement requestElement = root.Element("TriggerRequest");

                if (requestElement != null)
                {
                    TriggerRequest request = TriggerRequest.Parse(requestElement.ToString());
                    request.Source = source;

                    if (OnTriggerRequest != null)
                    {
                        OnTriggerRequest(this, request);
                    }

                    return RequestProcessingResult.Success;
                }
                else
                {
                    return RequestProcessingResult.XmlError;
                }
            }
            else
            {
                return RequestProcessingResult.UnknownRequest;
            }
        }

        public override void Start()
        {
            _rcv.Start();
        }

        public override void Stop()
        {
            _rcv.Stop();
        }

        
    }
}
