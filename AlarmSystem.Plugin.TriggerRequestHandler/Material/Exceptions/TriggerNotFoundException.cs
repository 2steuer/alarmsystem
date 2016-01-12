using System;

namespace AlarmSystem.Plugin.TriggerRequestHandler.Material.Exceptions
{
    class TriggerNotFoundException :Exception
    {
        public TriggerNotFoundException(string triggerText)
            :base("Could not find trigger " + triggerText)
        {
            
        }
    }
}
