using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using AlarmSystem.Common.Material;

namespace TriggerTest
{
    public class TriggerRequestSender
    {
        private string _uri = "http://localhost:2525/trigger";

        public string Uri
        {
            get
            {
                return _uri;
            }
            set { _uri = value; }
        }

        public string Username { get; set; }
        public string Password { get; set; }

        public bool Authenticate { get; set; }

        public string ApiKey { get; set; }

        public TriggerRequestSender(string uri)
            :this(uri, false)
        {
        }

        public TriggerRequestSender(string uri, bool authenticate)
        {
            Authenticate = authenticate;
            _uri = uri;
        }

        public bool Send(TriggerRequest request)
        {
            using (HttpClient client = new HttpClient())
            {
                XDocument doc = new XDocument();
                XElement root;
                doc.Add(root = new XElement("Request"));
                root.Add(new XElement("ApiKey", ApiKey));
                root.Add(new XElement("Type", "TriggerRequest"));
                
                root.Add(XElement.Parse(request.ToString()));

                HttpContent content = new StringContent(doc.ToString(), Encoding.UTF8);

                if (Authenticate)
                {
                    string authString = String.Format("{0}:{1}", Username, Password);

                    var bytes = Encoding.UTF8.GetBytes(authString);
                    var header = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(bytes));

                    client.DefaultRequestHeaders.Authorization = header;
                }

                using (HttpResponseMessage resp = client.PostAsync(_uri, content).Result)
                {
                    Console.WriteLine(resp.StatusCode);
                    if (resp.IsSuccessStatusCode)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        
    }
}
