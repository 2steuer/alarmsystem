using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using AlarmSystem.Material;

namespace AlarmSystem
{
    class ConfigProvider
    {
        private string _file;
        private XDocument _doc;

        public ConfigProvider(string fileName)
        {
            _file = fileName;

            if (File.Exists(_file))
            {
                _doc = XDocument.Load(_file);
            }
            else
            {
                _doc = new XDocument();
                _doc.Add(new XElement("AlarmSystem"));
                _doc.Save(_file);
            }
        }

        public string GetString(string section, params string[] path)
        {
            XElement current = _doc.Element("AlarmSystem");

            string breadCrumbs = "Root > AlarmSystem";

            if (current == null)
            {
                throw new InvalidConfigurationException("XML Format Error. Root element AlarmSystem missing!", breadCrumbs);
            }

            List<string> pathList = new List<string>();
            pathList.Add("Config");
            pathList.Add(section);
            pathList.AddRange(path);

            for (int i = 0; i < pathList.Count; i++)
            {
                breadCrumbs += " > " + pathList[i];

                current = current.Element(pathList[i]);

                if (current == null)
                {
                    throw new InvalidConfigurationException("Key " + pathList[i] + " not found in config XML.", breadCrumbs);
                }
            }

            return  current.Value;
        }

        public bool GetBoolean(string section, params string[] path)
        {
            return bool.Parse(GetString(section, path));
        }

        public int GetInteger(string section, params string[] path)
        {
            return int.Parse(GetString(section, path));
        }

        public List<WireInfo> GetWiring()
        {
            List<WireInfo> list = new List<WireInfo>();

            XElement element = _doc.Element("AlarmSystem");

            if (element != null)
            {
                element = element.Element("Wiring");

                if (element != null)
                {
                    foreach (var wire in element.Elements("Wire"))
                    {
                        WireInfo newInfo = new WireInfo()
                        {
                            Type = wire.Attribute("type").Value ?? string.Empty,
                            Source = wire.Attribute("source").Value ?? string.Empty,
                            Sink = wire.Attribute("sink").Value ?? string.Empty
                        };
                        list.Add(newInfo);
                    }
                }
            }

            return list;
        }
    }
}
