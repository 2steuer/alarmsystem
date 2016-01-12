using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmSystem.Common;
using AlarmSystem.Common.Material;
using AlarmSystem.Common.Services;
using NDesk.Options;

namespace TriggerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string name = "TEST";
            string message = String.Empty;
            string uri = "http://localhost:2525/alarm";

            bool auth = false;
            string user = string.Empty;
            string pass = string.Empty;
            string apikey = string.Empty;

            bool help = false;

            OptionSet paramset = new OptionSet()
            {
                {"m|message=", "The {MESSAGE} of the Triggertest.", s => message = s},
                {"n|name=",     "The {NAME} of the Trigger to test.", s => name = s},
                {"u|uri=", "The {URI} to send the trigger request to.", s => uri = s},
                {"a|auth", "Authenticate at the server", s => auth = s != null},
                {"c|user=", "Authenticate as {USER}", s => user = s},
                {"k|key=", "Set API-Key to {KEY}", s => apikey = s},
                {"p|pass=", "Authenticate with password {PASSWORD}", s => pass = s},
                {"h|help", "Show help", v => help = v != null}
            };

            paramset.Parse(args);

            if (name == String.Empty)
            {
                Console.WriteLine("AlarmSystem TriggerTest Program");
                Console.WriteLine("By Merlin Steuer in 2015");
                Console.WriteLine();
                paramset.WriteOptionDescriptions(Console.Out);
            }
            else
            {
                TriggerRequest test = new TriggerRequest(name, (message == string.Empty), message, apikey);
                
                TriggerRequestSender sender = new TriggerRequestSender(uri);
                sender.Authenticate = auth;
                sender.Username = user;
                sender.Password = pass;
                sender.ApiKey = apikey;

                if (sender.Send(test))
                {
                    Console.WriteLine("Success!");
                }
                else
                {
                    Console.WriteLine("Error!");
                }
            }
        }
    }
}
