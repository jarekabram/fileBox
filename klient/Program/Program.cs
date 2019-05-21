using System;
using System.Runtime.InteropServices;
using Common;
using Klient.Manager;


namespace Klient
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                System.Threading.Thread.Sleep(1000);
                LogHandler.GetLogHandler.Log("wrong Number of parameters");
                Console.ReadKey();
            }
            Console.WriteLine("Insert user name: ");
            string userName = Console.ReadLine();

            LogHandler.GetLogHandler.Log("Path: " + args[0] + " Username: "+ userName);
            try
            {
                ClientConnection clientConnection = new ClientConnection(userName, "127.0.0.1", 4444);
                clientConnection.WatchDirectory(args[0]);
            }
            catch (Exception ex)
            {
                LogHandler.GetLogHandler.Log(ex.Message);
                Console.ReadKey();
            }
        }
    }
}
