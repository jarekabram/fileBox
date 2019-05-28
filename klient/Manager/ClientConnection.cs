using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Common;


namespace Klient.Manager
{
    class ClientConnection
    {
        private TcpClient _tcpClient;
        
        private bool m_serverAvailable = false;
        private const int m_BufferSize = 1024;
        private const int m_Timeout = 2500;
        private int m_retryCount;
        private int m_Port;
        private string m_Server;
        private string m_User = null;
        
        public ClientConnection(string p_User, string p_Server, int p_Port)
        {
            m_User = p_User;
            m_Server = p_Server;
            m_Port = p_Port;

            Connect();
        }

        internal void WatchDirectory(object input)
        {
            string p_Path = input as string;
            p_Path = Path.Combine(p_Path, m_User);
            if (!Directory.Exists(p_Path))
            {
                Directory.CreateDirectory(p_Path);
            }

            // get number of files in specified direcotry - have to consider how to change that later...
            int fileCount = Directory.GetFiles(p_Path).Length;
            while (true)
            {
                if (_tcpClient == null)
                {
                    LogHandler.GetLogHandler.Log("Waiting for server to connect with.");
                    Thread.Sleep(m_Timeout);
                    Connect();
                }
                else
                {
                    //if (m_serverAvailable)
                    if (_tcpClient.Client.Connected)
                    {
                        byte[] buffer = null;
                        byte[] header = null;

                        if (fileCount < Directory.GetFiles(p_Path).Length)
                        {
                            try
                            {
                                LogHandler.GetLogHandler.Log("Added new file or directory - sending to server (directory: " + p_Path + ")");

                                // get file which was recently added to curent directory
                                DirectoryInfo directory = new DirectoryInfo(p_Path);
                                FileInfo myFile = directory.GetFiles()
                                                    .OrderByDescending(f => f.LastAccessTime)
                                                    .First();

                                // prepare file
                                FileStream fileStream = new FileStream(Path.Combine(p_Path, myFile.Name), FileMode.Open);
                                int bufferCount = Convert.ToInt32(Math.Ceiling((double)fileStream.Length / (double)m_BufferSize));

                                // prepare header
                                string headerStr = "filename:" + myFile.Name + "|" + "filesize:" + fileStream.Length + "|" + "username:" + m_User;
                                LogHandler.GetLogHandler.Log("Prepared header to send: {" + headerStr + "}");
                                header = new byte[m_BufferSize];

                                // copy characters to header
                                Array.Copy(Encoding.ASCII.GetBytes(headerStr), header, Encoding.ASCII.GetBytes(headerStr).Length);
                                // send header to server
                                _tcpClient.Client.Send(header);

                                // send file to server
                                for (int i = 0; i < bufferCount; i++)
                                {
                                    buffer = new byte[m_BufferSize];
                                    int size = fileStream.Read(buffer, 0, m_BufferSize);

                                    _tcpClient.Client.Send(buffer, size, SocketFlags.Partial);

                                }
                                fileStream.Close();

                                fileCount++;
                            }
                            catch (Exception)
                            {
                                throw;
                            }
                        }
                        else if (fileCount > Directory.GetFiles(p_Path).Length)
                        {
                            LogHandler.GetLogHandler.Log("Removed a file or directory");
                            fileCount = Directory.GetFiles(p_Path).Length;
                        }

                        PingServer();
                    }
                    else
                    {
                        Thread.Sleep(m_Timeout);
                        LogHandler.GetLogHandler.Log("Lost connection with server, trying again ( " + m_retryCount + " retries left... )");
                        if (m_retryCount == 0)
                        {
                            return;
                        }
                        // retry connection
                        if (!Connect())
                        {
                            m_retryCount--;
                        }
                    }
                }
            }
        }

        private void PingServer()
        {
            // Detect if client disconnected
            if (_tcpClient.Client.Poll(0, SelectMode.SelectRead))
            {
                byte[] buff = new byte[1];
                try
                {
                    _tcpClient.Client.Receive(buff, SocketFlags.Peek);
                }
                catch (Exception)
                {
                    // Client disconnected
                    LogHandler.GetLogHandler.Log("Client is disconnected...");
                }
            }
        }

        private bool Connect()
        {
            try
            {
                _tcpClient = new TcpClient(m_Server, m_Port);
                m_retryCount = 5;
                if (_tcpClient.Connected)
                {
                    LogHandler.GetLogHandler.Log("Connected with server");
                }
            }
            catch (Exception)
            {
                LogHandler.GetLogHandler.Log("Connection with server failed");
                return false;
            }
            return true;
        }

    }
}
