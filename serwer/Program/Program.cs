using System;
using System.IO;
using Microsoft.Win32;
using Common;
using Serwer.Manager;
using System.Threading;

namespace serwer
{
    class Program
    {

        private static RegistryKey _registryKey;
        public static void Main(string[] args)
        {

            // hardcode definition of client application path
            if (!File.Exists(Config.ClientPath))
            {
                throw new FileNotFoundException("please install client application first");
            }

            // Insert client application to windows context menu while server is working
            LogHandler.GetLogHandler.Log("Init");
            _registryKey = Utils.AddContextMenuButton();

            // Add event of cleaning item in windows context menu due to it would be no longer needed
            AppDomain.CurrentDomain.ProcessExit += AppDomain_ProcessExit;

            while (true)
            {
                try
                {
                    ServerConnection serverConnection = new ServerConnection(Config.ServerAddress, Config.ServerPort);
                    serverConnection.ProcessFile();
                }
                catch (Exception ex)
                {
                    LogHandler.GetLogHandler.Log(ex.Message);
                    Thread.Sleep(3500);
                }
            }
        }

        private static void AppDomain_ProcessExit(object sender, EventArgs e)
        {
            LogHandler.GetLogHandler.Log("Destruction");
            Utils.RemoveContextMenuButton(_registryKey);
        }
    }
}
