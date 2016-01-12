using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmSystem.Common.Plugins.Material;
using AlarmSystem.Common.Services;

namespace AlarmSystem.Common.Plugins
{
    /// <summary>
    /// A function that returns a config value from the config file for the given plugin's section. The path
    /// of the entry is given in path.
    /// </summary>
    /// <param name="plugin">The plugin.</param>
    /// <param name="path">The path.</param>
    /// <returns>The string config value from the config file for this plugin, specified by its path.</returns>
    public delegate string PluginConfigStringDelegate(AlarmSystemPluginBase plugin, params string[] path);

    /// <summary>
    /// A function that returns a config value from the config file for the given plugin's section. The path
    /// of the entry is given in path.
    /// </summary>
    /// <param name="plugin">The plugin.</param>
    /// <param name="path">The path.</param>
    /// <returns>The integer config value from the config file for this plugin, specified by its path.</returns>
    public delegate int PluginConfigIntegerDelegate(AlarmSystemPluginBase plugin, params string[] path);

    /// <summary>
    /// A function that returns a config value from the config file for the given plugin's section. The path
    /// of the entry is given in path.
    /// </summary>
    /// <param name="plugin">The plugin.</param>
    /// <param name="path">The path.</param>
    /// <returns>The boolean config value from the config file for this plugin, specified by its path.</returns>
    public delegate bool PluginConfigBooleanDelegate(AlarmSystemPluginBase plugin, params string[] path);

    /// <summary>
    /// Base class for all Plugins created for AlarmSystem.
    /// </summary>
    [InheritedExport(typeof(AlarmSystemPluginBase))]
    public abstract class AlarmSystemPluginBase
    {
        /// <summary>
        /// Contains the name of the plugin.
        /// </summary>
        public abstract string PluginName { get; }

        /// <summary>
        /// Contains the author of the plugin.
        /// </summary>
        public abstract string PluginAuthor { get; }

        /// <summary>
        /// Contains a short description about the plugin.
        /// </summary>
        public abstract string PluginDescription { get; }

        /// <summary>
        /// Contains a string representing the version of a plugin.
        /// </summary>
        public abstract string PluginVersion { get; }

        private PluginConfigStringDelegate GetConfigStringDelegate;
        private PluginConfigIntegerDelegate GetConfigIntegerDelegate;
        private PluginConfigBooleanDelegate GetConfigBooleanDelegate;

        /// <summary>
        /// Gets a string from the configuration.
        /// </summary>
        /// <param name="path">The path to the element in the XML tree where the information shall be obtained from.
        /// Pass each level as entry, so if you want to get the element "Server"->"Connection"->"Port" call
        /// it with each of these entries as a string.</param>
        /// <returns>The value of the config element.</returns>
        protected string GetConfigString(params string[] path)
        {
            return GetConfigStringDelegate(this, path);
        }

        /// <summary>
        /// Gets an integer from the configuration.
        /// </summary>
        /// <param name="path">The path to the element in the XML tree where the information shall be obtained from.
        /// Pass each level as entry, so if you want to get the element "Server"->"Connection"->"Port" call
        /// it with each of these entries as a string.</param>
        /// <returns>The value of the config element.</returns>
        protected int GetConfigInteger(params string[] path)
        {
            return GetConfigIntegerDelegate(this, path);
        }

        /// <summary>
        /// Gets a boolean from the configuration.
        /// </summary>
        /// <param name="path">The path to the element in the XML tree where the information shall be obtained from.
        /// Pass each level as entry, so if you want to get the element "Server"->"Connection"->"Port" call
        /// it with each of these entries as a string.</param>
        /// <returns>The value of the config element.</returns>
        protected bool GetConfigBoolean(params string[] path)
        {
            return GetConfigBooleanDelegate(this, path);
        }

        /// <summary>
        /// Sets the configuration delegates. <b>This function is called by the plugin host and MUST NOT be called from user code!</b>
        /// </summary>
        /// <param name="cfgStringDelegate">The CFG string delegate.</param>
        /// <param name="cfgIntDelegate">The CFG int delegate.</param>
        /// <param name="cfgBoolDelegate">The CFG bool delegate.</param>
        public void SetConfigDelegates(PluginConfigStringDelegate cfgStringDelegate,
            PluginConfigIntegerDelegate cfgIntDelegate, PluginConfigBooleanDelegate cfgBoolDelegate)
        {
            GetConfigStringDelegate = cfgStringDelegate;
            GetConfigIntegerDelegate = cfgIntDelegate;
            GetConfigBooleanDelegate = cfgBoolDelegate;
        }

        /// <summary>
        /// The database service set by the plugin host system. Can be used for any database transactions.
        /// </summary>
        protected DatabaseService DatabaseService;

        /// <summary>
        /// Sets the database service. <b>This function is cllaed by the plugin host and MUST NOT be called from user code!</b>
        /// </summary>
        /// <param name="service">The database service to be used by this plugin.</param>
        public void SetDatabaseService(DatabaseService service)
        {
            DatabaseService = service;
        }

        /// <summary>
        /// This function is called to initialise the Plugin.
        /// </summary>
        public void Init()
        {
            if (GetConfigBooleanDelegate == null || GetConfigIntegerDelegate == null || GetConfigStringDelegate == null)
            {
                throw new Exception("The configuration delegates must be set before initialising a plugin.");
            }

            if (DatabaseService == null)
            {
                throw new Exception("Database Service must be given before initialising a plugin.");
            }

            InitRoutine();
        }

        /// <summary>
        /// This function must be overridden in the plugin user code and should execute all needed initialisation stuff for the plugin.
        /// </summary>
        protected abstract void InitRoutine();

        /// <summary>
        /// Within this function, services, threads etc. shall be started.
        /// </summary>
        public abstract void Start();

        /// <summary>
        /// Within this function all working services, threads etc. shall be stopped and all used resources should be freed.
        /// </summary>
        public abstract void Stop();

        /// <summary>
        /// Determines whether the Plugin has the given capability.
        /// This is derived from the implemented interfaces. I.e. when the Interface IMessageSource is implemented,
        /// the Plugin has the capability MessageSource.
        /// </summary>
        /// <param name="capability">The capability to check.</param>
        /// <returns>true if the plugin can do the given task, false if not.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The given capability was not known to this function.</exception>
        public bool HasCapability(PluginCapability capability)
        {
            switch (capability)
            {
                case PluginCapability.MessageSource:
                    return GetType().GetInterface("IMessageSource") != null;
                    
                case PluginCapability.MessageSink:
                    return GetType().GetInterface("IMessageSink") != null;
                    
                case PluginCapability.FreetextSource:
                    return GetType().GetInterface("IFreetextSource") != null;
                    
                case PluginCapability.FreetextSink:
                    return GetType().GetInterface("IFreetextSink") != null;
                    
                case PluginCapability.TriggerMessageSource:
                    return GetType().GetInterface("ITriggerMessageSource") != null;
                    
                case PluginCapability.TriggerMessageSink:
                    return GetType().GetInterface("ITriggerMessageSink") != null;

                case PluginCapability.TriggerRequestSource:
                    return GetType().GetInterface("ITriggerRequestSource") != null;

                case PluginCapability.TriggerRequestSink:
                    return GetType().GetInterface("ITriggerRequestSink") != null;

                default:
                    throw new ArgumentOutOfRangeException("capability");
            }
        }
    }
}
