using System;
using System.Threading.Tasks;
using Akka.Actor;
using AkkaDotNet.SensorData.Shared.Actors;
using AkkaDotNet.SensorData.Shared.Database;
using AkkaDotNet.SensorData.Shared.Helpers;
using Microsoft.Extensions.Configuration;

namespace AkkaDotNet.SensorData.ActorSystemHost
{
    class Program
    {
        static async Task Main()
        {
            ReadConnectionString();
            Console.WriteLine("Starting the ActorSystem ...");

            var config = ConfigurationReader.ReadAkkaConfigurationFile();

            //TODO  1: start the ActorSystem & initialize the DevicesActor

            Console.WriteLine("ActorSystem stopped. Press any key to exit ...");
            Console.ReadKey();
        }

        private static void ReadConnectionString()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appSettings.json", true, true)
                .Build();

            DbSettings.HistoryConnectionString = config["HistoryConnectionString"];
        }

        private static async Task ProcessConsoleCommandsUntilExit(ActorSystem actorSystem)
        {
            var stopped = false;

            while (!stopped)
            {
                Console.WriteLine("Please type exit to shut down:");
                var command = Console.ReadLine();
                switch (command)
                {
                    case "exit":
                        await actorSystem.Terminate();
                        stopped = true;
                        break;
                }
            }
        }
    }
}
