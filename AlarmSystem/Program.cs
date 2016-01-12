using System;
using System.Reflection;
using System.ServiceProcess;
using NDesk.Options;

namespace AlarmSystem
{
    class Program
    {
        private static OptionSet cmd;

        static void Main(string[] args)
        {
            bool service = false;
            bool standalone = false;
            string cfgFile = "AlarmSystemConfig.xml";
            bool showHelp = false;

            cmd = new OptionSet()
            {
                {"d|daemon", "Start as background daemon", s => service = s != null},
                {"s|standalone", "Start not as daemon, but standalone", s => standalone = s != null},
                {"c|config=", "Set the config {FILE} to use", s => cfgFile = s},
                {"h|help", "Show help", s => showHelp = s != null}
            };

            cmd.Parse(args);

            if (service && standalone)
            {
                Console.WriteLine("Sorry, you cannot start as daemon and standalone at the same time.");
                PrintHelp();
            }
            else
            {
                AlarmSystemService alarmsys = new AlarmSystemService();
                alarmsys.ConfigFile = cfgFile;

                if (service)
                {
                    ServiceBase.Run(alarmsys);
                }
                else if(standalone)
                {
                    alarmsys.Start();

                    Console.ReadLine();

                    alarmsys.Stop();
                }
                else if(showHelp)
                {
                    PrintHelp();
                }
                else
                {
                    PrintHelp();
                }
            }
        }
        

        protected static void PrintHelp()
        {
            Console.WriteLine("AlarmSystem version " + Assembly.GetExecutingAssembly().GetName().Version);
            Console.WriteLine("Written in 2015 by Merlin Steuer");
            Console.WriteLine();
            cmd.WriteOptionDescriptions(Console.Out);
        }
    }
}
