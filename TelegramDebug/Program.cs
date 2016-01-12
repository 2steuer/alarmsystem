using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NDesk.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramDebug
{
    class Program
    {
        static void Main(string[] args)
        {
            string apiKey = String.Empty;

            OptionSet set = new OptionSet
            {
                {"k|key=", "Telegram Bot {API-Key}", s => apiKey = s}
            };

            set.Parse(args);

            if (apiKey == String.Empty)
            {
                Console.WriteLine("Telegram Bot Debug Tool");
                Console.WriteLine("By Merlin Steuer in 2015");
                Console.WriteLine();
                Console.WriteLine("Usage:");
                set.WriteOptionDescriptions(Console.Out);
            }
            else
            {
                Console.WriteLine("Using API-Key {0}", apiKey);
                Console.WriteLine();

                Api telegram = new Api(apiKey);
                User me = telegram.GetMe().Result;

                int offset = 0;

                Console.WriteLine("Logged in as");
                Console.WriteLine("User {0} ({1})", me.Username, me.Id);
                Console.WriteLine("Full {0} {1}", me.FirstName, me.LastName);
                Console.WriteLine();
                Console.WriteLine("Receiving Text-Messages...");

                while (true)
                {
                    Update[] updates = telegram.GetUpdates(offset).Result;

                    foreach (Update update in updates)
                    {
                        if (update.Message.Type == MessageType.TextMessage)
                        {
                            Console.WriteLine("TextMessage received:");
                            Console.WriteLine("Chat ID: {0}", update.Message.Chat.Id);
                            Console.WriteLine("From: {0} {1} / {2}", update.Message.From.FirstName, update.Message.From.LastName, update.Message.From.Username);
                            Console.WriteLine("Message: {0}", update.Message.Text);
                            Console.WriteLine();
                        }

                        offset = update.Id + 1;
                    }

                    Thread.Sleep(1000);
                }
            }
            
        }
    }
}
