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
        private TcpClient m_tcpClient;
        
        private const int m_Timeout = 2500;
        private int m_retryCount;
        private int m_Port;
        private string m_Address;
        private string m_User = null;
        private int m_bufferSize = 1024;

        public ClientConnection(string p_User, string p_Address, int p_Port)
        {
            m_User = p_User;
            m_Address = p_Address;
            m_Port = p_Port;
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
                if (m_tcpClient == null)
                {
                    LogHandler.GetLogHandler.Log("Waiting for server to connect with.");
                    Thread.Sleep(m_Timeout);

                    Connect();
                }
                else
                {
                    if (m_tcpClient.Client.Connected)
                    {
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
                                byte[] fileContent = File.ReadAllBytes(Path.Combine(p_Path, myFile.Name));
                                
                                // prepare header
                                string headerStr = "filename:" + myFile.Name + "|" + "filesize:" + fileContent.Length + "|" + "username:" + m_User;
                                LogHandler.GetLogHandler.Log("Prepared header to send: {" + headerStr + "}");

                                Data dataToSend = new Data(headerStr, fileContent);
                                byte[] data = Utils.ObjectToByteArray(dataToSend);
                                byte[] requestBuffer = new byte[m_bufferSize];
                                Array.Copy(data, requestBuffer, data.Length);
                                m_tcpClient.Client.Send(requestBuffer, requestBuffer.Length, SocketFlags.Partial);

                                fileCount++;
                            }
                            catch (Exception p_exc)
                            {
                                LogHandler.GetLogHandler.Log(p_exc.Message);
                            }
                        }
                        else if (fileCount > Directory.GetFiles(p_Path).Length)
                        {
                            LogHandler.GetLogHandler.Log("Removed a file or directory");
                            fileCount = Directory.GetFiles(p_Path).Length;
                        }

                        // ping server to check connection availability
                        Utils.Ping(m_tcpClient);
                    }
                    else
                    {
                        // wait until next connection check
                        Thread.Sleep(m_Timeout);
                        LogHandler.GetLogHandler.Log("Lost connection with server, trying again ( " + m_retryCount + " retries left... )");

                        // if retries are exhausted, finish job
                        if (m_retryCount == 0)
                        {
                            return;
                        }
                        // retry connection
                        if (!Connect())
                        {
                            //m_retryCount--;
                        }
                    }
                }
            }
        }

        private bool Connect()
        {
            try
            {
                m_tcpClient = new TcpClient(m_Address, m_Port);
                m_retryCount = 5;
                if (m_tcpClient.Connected)
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
