using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
            if (!File.Exists(@"F:\Studia\II_stopien\II_semestr\mpw\fileBox\klient\bin\Debug\netcoreapp2.2\win10-x64\publish\Klient.exe"))
            {
                throw new FileNotFoundException("please install client application first");
            }

            // Insert client application to windows context menu while server is working
            LogHandler.GetLogHandler.Log("Init");
            _registryKey = Utils.AddContextMenuButton();

            // Add event of cleaning item in windows context menu due to it would be no longer needed
            AppDomain.CurrentDomain.ProcessExit += AppDomain_ProcessExit;


            try
            {
                ServerConnection serverConnection = new ServerConnection("127.0.0.1", 4444);
                while (true)
                {
                    serverConnection.ListenToClient();
                }
            }
            catch (Exception ex)
            {
                LogHandler.GetLogHandler.Log(ex.Message);
            }
        }

        private static void AppDomain_ProcessExit(object sender, EventArgs e)
        {
            LogHandler.GetLogHandler.Log("Destruction");
            Utils.RemoveContextMenuButton(_registryKey);
        }
    }
}
