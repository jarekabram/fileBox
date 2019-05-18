using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Win32;
using Common;

namespace serwer
{
    class Program
    {
        private const int m_BufferSize = 1024;
        private static RegistryKey _registryKey;
        private static TcpListener _server = null;

        public static void Main(string[] args)
        {

            // hardcode definition of client application path
            if (!File.Exists(@"F:\Studia\II_stopien\II_semestr\mpw\fileBox\klient\bin\Debug\netcoreapp2.2\win-x64\Klient.exe"))
            {
                throw new FileNotFoundException("please install client application first");
            }

            // Insert client application to windows context menu while server is working
            LogHandler.GetLogHandler.Log("Init");
            _registryKey = Registry.ClassesRoot.CreateSubKey(@"Directory\shell\fileBox");
            _registryKey = Registry.ClassesRoot.CreateSubKey(@"Directory\shell\fileBox\command");
            _registryKey.SetValue("", @"F:\Studia\II_stopien\II_semestr\mpw\fileBox\klient\bin\Debug\netcoreapp2.2\win-x64\Klient.exe " + "\"" + "%1" + "\"");

            // Add event of cleaning item in windows context menu due to it would be no longer needed
            AppDomain.CurrentDomain.ProcessExit += AppDomain_ProcessExit;

            try
            {
                // Set the TcpListener on port 4444.
                int port = 4444;
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");

                // TcpListener server = new TcpListener(port);
                _server = new TcpListener(localAddr, port);

                // Start listening for client requests.
                _server.Start();
                Socket socket = _server.AcceptSocket();

                // Enter the listening loop.
                while (true)
                {
                    byte[] header = null;
                    header = new byte[m_BufferSize];
                    try
                    {
                        socket.Receive(header);
                        if (header != null)
                        {
                            LogHandler.GetLogHandler.Log("received message");

                            string headerStr = Encoding.ASCII.GetString(header);
                            int terminate = headerStr.IndexOf("\0");
                            headerStr = headerStr.Substring(0, terminate + 1);

                            string[] splitted = headerStr.Split(new string[] { "|" }, StringSplitOptions.None);
                            Dictionary<string, string> headers = new Dictionary<string, string>();
                            foreach (string s in splitted)
                            {
                                if (s.Contains(":"))
                                {
                                    headers.Add(s.Substring(0, s.IndexOf(":")), s.Substring(s.IndexOf(":") + 1));
                                }
                            }
                            string filename = headers["filename"];
                            int fileSize = Convert.ToInt32(headers["filesize"]);
                            string user = headers["username"];
                            byte[] buffer = null;

                            FileStream fs = new FileStream(filename, FileMode.OpenOrCreate);
                            while (fileSize > 0)
                            {
                                buffer = new byte[m_BufferSize];
                                int size = socket.Receive(buffer, SocketFlags.Partial);
                                string message = Utils.encodeBuffer(buffer, Utils.fixedBufferSize(buffer));
                                LogHandler.GetLogHandler.Log("file received - content: {" + message + "}");
                                fileSize -= size;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHandler.GetLogHandler.Log(ex.Message);
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHandler.GetLogHandler.Log(ex.Message);
                throw;
            }

        }
        private static void AppDomain_ProcessExit(object sender, EventArgs e)
        {
            LogHandler.GetLogHandler.Log("Destruction");
            _registryKey.DeleteValue("");
            Registry.ClassesRoot.DeleteSubKey(@"Directory\shell\fileBox\command");
            Registry.ClassesRoot.DeleteSubKey(@"Directory\shell\fileBox");
        }
    }
}
