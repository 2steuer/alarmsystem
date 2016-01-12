using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmSystem.Plugin.Http.Material
{
    public enum RequestProcessingResult
    {
        Success,
        XmlError,
        UnknownRequest,
        Error,
        ApiKeyError
    }
}
