using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.ServiceProcess;
using AlarmSystem.Common.Logging;
using AlarmSystem.Common.Logging.Material;
using AlarmSystem.Common.Plugins;
using AlarmSystem.Common.Plugins.Interfaces;
using AlarmSystem.Common.Plugins.Material;
using AlarmSystem.Common.Services;
using AlarmSystem.Material;


namespace AlarmSystem
{
    class AlarmSystemService : ServiceBase
    {
        private ConfigProvider _cfg;
        private Log _log;
        private DatabaseService _db;

        [ImportMany(typeof (AlarmSystemPluginBase))] private AlarmSystemPluginBase[] _rawPlugins;

        Dictionary<string, AlarmSystemPluginBase> _plugins = new Dictionary<string, AlarmSystemPluginBase>(); 

        private string _cfgFile = "AlarmSystemConfig.xml";

        public string ConfigFile
        {
            get { return _cfgFile; }
            set { _cfgFile = value; }
        }

        protected override void OnStart(string[] args)
        {
            _cfg = new ConfigProvider(_cfgFile);

            _log = new Log(
                _cfg.GetBoolean("Log", "Console", "Enabled"),
                (LogLevel)_cfg.GetInteger("Log", "Console", "Level"),
                _cfg.GetBoolean("Log", "File", "Enabled"),
                (LogLevel)_cfg.GetInteger("Log", "File", "Level"),
                _cfg.GetString("Log", "File", "FileName"));
            _log.StartLogging();

            Log.Add(LogLevel.Info, "System", "System started.");

            try
            {
                _db = new DatabaseService(
                    _cfg.GetString("Database", "Host"),
                    _cfg.GetString("Database", "User"),
                    _cfg.GetString("Database", "Password"),
                    _cfg.GetString("Database", "DatabaseName"));
                _db.Open();

                DirectoryCatalog catalog = new DirectoryCatalog("./Plugins");
                
                Log.Add(LogLevel.Info, "System", "Loading Plugins from " + catalog.FullPath);

                try
                {
                    CompositionContainer container = new CompositionContainer(catalog);

                    container.ComposeParts(this);
                }
                catch(System.Reflection.ReflectionTypeLoadException ex)
                {
                    

                    foreach(Exception ex2 in ex.LoaderExceptions)
                    {
                        Console.WriteLine("{0}: {1} / {2}", ex2.GetType(), ex2.Message, ex2.StackTrace);
                        Console.WriteLine();
                    }
                }

                Log.Add(LogLevel.Info, "System", "Found " + _rawPlugins.Length + " plugins.");

                foreach (AlarmSystemPluginBase plugin in _rawPlugins)
                {
                    Log.Add(LogLevel.Info, "System", string.Format("Loading Plugin {0} {1}", plugin.PluginName, plugin.PluginVersion));
                    plugin.SetConfigDelegates(PluginConfigString, PluginConfigInteger, PluginConfigBoolean);
                    plugin.SetDatabaseService(_db);
                    plugin.Init();

                    _plugins.Add(plugin.PluginName, plugin);
                }

                foreach (var wire in _cfg.GetWiring())
                {
                    Log.Add(LogLevel.Debug, "System", string.Format("Wiring up {0} with {1} for {2}", wire.Source, wire.Sink, wire.Type));
                    AlarmSystemPluginBase src, snk;
                    if (_plugins.ContainsKey(wire.Source))
                    {
                        src = _plugins[wire.Source];
                    }
                    else
                    {
                        Log.Add(LogLevel.Warning, "System", string.Format("Plugin {0} not found while wiring up.", wire.Source));
                        continue;
                    }

                    if (_plugins.ContainsKey(wire.Sink))
                    {
                        snk = _plugins[wire.Sink];
                    }
                    else
                    {
                        Log.Add(LogLevel.Warning, "System", string.Format("Plugin {0} not found while wiring up.", wire.Source));
                        continue;
                    }

                    if (wire.Type.Equals("Message"))
                    {
                        if (src.HasCapability(PluginCapability.MessageSource) &&
                            snk.HasCapability(PluginCapability.MessageSink))
                        {
                            ((IMessageSource) _plugins[wire.Source]).OnMessageReceived +=
                                ((IMessageSink) _plugins[wire.Sink]).HandleMessage;
                        }
                        else
                        {
                            Log.Add(LogLevel.Warning, "System", string.Format("Plugins {0} and {1} do not match for {2}", src.PluginName, snk.PluginName, wire.Type));
                        }
                    }
                    else if(wire.Type.Equals("Freetext"))
                    {
                        if (src.HasCapability(PluginCapability.FreetextSource) &&
                            snk.HasCapability(PluginCapability.FreetextSink))
                        {
                            ((IFreetextSource)_plugins[wire.Source]).OnFreetextMessage +=
                                ((IFreetextSink)_plugins[wire.Sink]).HandleFreetext;
                        }
                        else
                        {
                            Log.Add(LogLevel.Warning, "System", string.Format("Plugins {0} and {1} do not match for {2}", src.PluginName, snk.PluginName, wire.Type));
                        }
                    }
                    else if (wire.Type.Equals("TriggerRequest"))
                    {
                        if (src.HasCapability(PluginCapability.TriggerRequestSource) &&
                            snk.HasCapability(PluginCapability.TriggerRequestSink))
                        {
                            ((ITriggerRequestSource) _plugins[wire.Source]).OnTriggerRequest +=
                                ((ITriggerRequestSink) _plugins[wire.Sink]).HandleTriggerRequest;
                        }
                        else
                        {
                            Log.Add(LogLevel.Warning, "System",
                                string.Format("Plugins {0} and {1} do not match for {2}", src.PluginName, snk.PluginName,
                                    wire.Type));
                        }
                    }
                    else if (wire.Type.Equals("TriggerMessage"))
                    {
                        if (src.HasCapability(PluginCapability.TriggerMessageSource) &&
                            snk.HasCapability(PluginCapability.TriggerMessageSink))
                        {
                            ((ITriggerMessageSource) _plugins[wire.Source]).OnTriggerMessage +=
                                ((ITriggerMessageSink) _plugins[wire.Sink]).HandleTriggerMessage;
                        }
                        else
                        {
                            Log.Add(LogLevel.Warning, "System",
                                string.Format("Plugins {0} and {1} do not match for {2}", src.PluginName, snk.PluginName,
                                    wire.Type));
                        }
                    }
                    else
                    {
                        Log.Add(LogLevel.Warning, "System", wire.Type + " is no known wire type.");
                    }
                }

                foreach (AlarmSystemPluginBase plugin in _rawPlugins)
                {
                    Log.Add(LogLevel.Debug, "System", string.Format("Starting {0}...", plugin.PluginName));
                    plugin.Start();
                }

                base.OnStart(args);
            }
            catch (InvalidConfigurationException ex)
            {
                Log.AddException("Config", ex);
                throw;
            }
            catch (Exception ex)
            {
                Log.AddException("SYSTEM/CRITICAL", ex);
                throw;
            }
        }

        

        
        protected override void OnStop()
        {
            foreach (AlarmSystemPluginBase plugin in _rawPlugins)
            {
                Log.Add(LogLevel.Debug, "System", "Stopping " + plugin.PluginName);
                plugin.Stop();
            }

            Log.Add(LogLevel.Info, "System", "Shutting down....");
            
        }

        public void Start()
        {
            OnStart(new string[] {});
        }

        public void Stop()
        {
            OnStop();
        }

        private string PluginConfigString(AlarmSystemPluginBase plugin, params string[] path)
        {
            List<string> p = new List<string>();
            p.Add(plugin.PluginName);
            p.AddRange(path.ToList());

            return _cfg.GetString("PluginConfig", p.ToArray());
        }

        private int PluginConfigInteger(AlarmSystemPluginBase plugin, params string[] path)
        {
            List<string> p = new List<string>();
            p.Add(plugin.PluginName);
            p.AddRange(path.ToList());

            return _cfg.GetInteger("PluginConfig", p.ToArray());
        }

        private bool PluginConfigBoolean(AlarmSystemPluginBase plugin, params string[] path)
        {
            List<string> p = new List<string>();
            p.Add(plugin.PluginName);
            p.AddRange(path.ToList());

            return _cfg.GetBoolean("PluginConfig", p.ToArray());
        }
    }
}
