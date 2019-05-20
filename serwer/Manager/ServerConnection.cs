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
        private static Mutex m_Mutex = new Mutex();

        public ServerConnection(string p_Address, int p_Port)
        {
            // Set the TcpListener on port 4444.
            IPAddress localAddr = IPAddress.Parse(p_Address);

            m_Server = new TcpListener(localAddr, p_Port);
        }

        public void ListenToClient()
        {
            LogHandler.GetLogHandler.Log("server listening");

            // Start listening for client requests.
            m_Server.Start();
            if (m_Server.Pending())
            {
                Socket socket = m_Server.AcceptSocket();
                ProcessFile(socket);
            }
            else
            {
                Thread.Sleep(100);
            }
            
        }

        private void ProcessFile(Socket socket)
        {
            var thread = new Thread(() =>
            {
                m_Mutex.WaitOne();
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

                        byte[] buffer = null;
                        LogHandler.GetLogHandler.Log("received header: { file: " + filename + " size: " + fileSize + " user: " + user);

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
                    else
                    {
                        m_Mutex.ReleaseMutex();
                        throw new Exception("Buffer is empty");
                    }
                }
                catch (Exception ex)
                {
                    LogHandler.GetLogHandler.Log(ex.Message);
                    m_Mutex.ReleaseMutex();
                    throw;
                }

            });
            thread.Start();
        }
    }
}