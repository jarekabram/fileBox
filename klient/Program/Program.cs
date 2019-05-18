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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                throw new Exception("Platform is not supported right now ...");
            }
            if (args.Length != 1)
            {
                System.Threading.Thread.Sleep(1000);
                LogHandler.GetLogHandler.Log("wrong Number of parameters");
                Console.ReadKey();
            }
            string userName = Console.ReadLine();
            Console.WriteLine("Insert user name: ");

            LogHandler.GetLogHandler.Log("Path: " + args[0] + " Username: "+ userName);
            try
            {
                FileManager fm = new FileManager(args[0], "127.0.0.1", 4444);
                fm.WatchDirectory(args[1]);
            }
            catch (Exception ex)
            {
                LogHandler.GetLogHandler.Log(ex.Message);
            }
        }
    }
}
