using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Serwer.Manager
{
    public class ServerConnection
    {
        private const int m_BufferSize = 1024;
        private static TcpListener m_Server = null;
        private Dictionary<string, string> m_UserThread;
        private List<UserData> m_UserDataList;
        //private static Mutex m_Mutex = new Mutex();

        public ServerConnection(string p_Address, int p_Port)
        {
            IPAddress localAddr = IPAddress.Parse(p_Address);
            // Set the TcpListener
            m_Server = new TcpListener(localAddr, p_Port);

            // Initialize members
            m_UserThread = new Dictionary<string, string>();
            m_UserDataList = new List<UserData>();
        }

        public void ListenToClient()
        {
            LogHandler.GetLogHandler.Log("server listening");

            // Start listening for client requests.
            m_Server.Start();
            while (true)
            {
                TcpClient client = m_Server.AcceptTcpClient();
                Thread thread = new Thread(ProcessFile);

                Guid guid = Guid.NewGuid();
                thread.Name = guid.ToString();
                thread.Start(client);
            }

        }

        private void ProcessFile(object input)
        {
            
            while (true)
            {
                var client = input as TcpClient;
                byte[] header = new byte[m_BufferSize];
                try
                {
                    client.Client.Receive(header);
                    if (header != null)
                    {
                        LogHandler.GetLogHandler.Log("(" + Thread.CurrentThread.Name + ") - " + "received message");

                        string headerStr = Encoding.ASCII.GetString(header);
                        int terminate = headerStr.IndexOf("\0");
                        headerStr = headerStr.Substring(0, terminate);

                        string[] splitted = headerStr.Split("|", StringSplitOptions.None);
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

                        // finish thread work if user exists with other thread id
                        if (m_UserThread.ContainsKey(user))
                        {
                            if (m_UserThread[user] != Thread.CurrentThread.Name)
                            {
                                client.Close();
                                LogHandler.GetLogHandler.Log("Actually such user " + user + " exists!");
                                return;
                            }
                        }
                        else
                        {
                            // in other case add user with corresponding thread
                            m_UserThread.Add(user, Thread.CurrentThread.Name);
                        }


                        byte[] buffer = null;
                        LogHandler.GetLogHandler.Log("(" + Thread.CurrentThread.Name + ") - " + "received header: { file: " + filename + " size: " + fileSize + " user: " + user);

                        FileStream fileStream = new FileStream(filename, FileMode.OpenOrCreate);
                        while (fileSize > 0)
                        {
                            buffer = new byte[m_BufferSize];
                            int size = client.Client.Receive(buffer, SocketFlags.Partial);
                            string message = Utils.encodeBuffer(buffer, Utils.fixedBufferSize(buffer));
                            LogHandler.GetLogHandler.Log("(" + Thread.CurrentThread.Name + ") - " + "file received - content: {" + message + "}");
                            fileSize -= size;
                            fileStream.Write(buffer, 0, size);
                        }
                        fileStream.Close();
                    }
                    else
                    {
                        throw new Exception("(" + Thread.CurrentThread.Name + ") - " + "Buffer is empty");
                    }
                }
                catch (Exception ex)
                {
                    LogHandler.GetLogHandler.Log("(" + Thread.CurrentThread.Name + ") - " + ex.Message);
                    throw;
                }
            }
        }
    }
}