using System;
using System.Runtime.InteropServices;
using Common;
using Klient.Manager;
using System.Threading;


namespace Klient
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                LogHandler.GetLogHandler.Log("wrong Number of parameters");
                Console.ReadKey();
            }
            string selectedDirectoryPath = args[0];
            Console.WriteLine("Insert user name: ");
            string userName = Console.ReadLine();

            LogHandler.GetLogHandler.Log("Path: " + selectedDirectoryPath + " Username: "+ userName);
            try
            {
                ClientConnection clientConnection = new ClientConnection(userName, Config.ClientAddress, Config.ClientPort);

                Thread watchDirectoryThread = new Thread(clientConnection.WatchDirectory);
                watchDirectoryThread.Start(selectedDirectoryPath);
            }
            catch (Exception ex)
            {
                LogHandler.GetLogHandler.Log(ex.Message);
                Console.ReadKey();
            }
        }
    }
}
