using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;

namespace Serwer.Manager
{
    public class ServerConnection
    {
        private string m_ServerAddress;
        private int m_ServerPort;
        private int m_ThreadPort;

        #region constants
        private const int FILE_AVAILABLE_REQUEST = 0x01;
        private const int m_BufferSize = 1024;
        private const int m_Timeout = 5000;
        private const int m_threadCount = 5;
        #endregion
        private static TcpClient m_tcpClient = null;
        private static TcpClient m_tcpThreadClient = null;

        private readonly object m_fileLocker = new object();
        private readonly object m_threadLocker = new object();
        private volatile int m_fileCount = 0;
        public ServerConnection(string p_Address, int p_Port)
        {
            m_ServerAddress = p_Address;
            m_ServerPort = p_Port;
            // added to handle separate connection between threads
            m_ThreadPort = Config.ThreadPort;

            Connect();
        }

        public void ProcessFile()
        {
            while (true)
            {
                try
                {
                    if (m_tcpClient.Client.Connected)
                    {
                        // Send request to check file availability
                        byte[] requestBuffer = new byte[m_BufferSize];
                        Array.Copy(BitConverter.GetBytes(FILE_AVAILABLE_REQUEST), requestBuffer, BitConverter.GetBytes(FILE_AVAILABLE_REQUEST).Length);
                        m_tcpClient.Client.Send(requestBuffer);

                        // if response is ok, have to wait for number of files as response
                        requestBuffer = new byte[m_BufferSize];
                        m_tcpClient.Client.Receive(requestBuffer, SocketFlags.Partial);

                        m_fileCount = Utils.GetIntFromBytes(requestBuffer);
                        LogHandler.GetLogHandler.Log("Number of files to be processed: " + m_fileCount);

                        if (m_fileCount != 0)
                        {
                            for (int i = 0; i < m_threadCount; i++)
                            {
                                ThreadPool.QueueUserWorkItem(new WaitCallback(ReceiveDataCallback));
                            }
                        }

                    }
                    else
                    {
                        Connect();
                    }
                }
                catch (SocketException p_exc)
                {
                    LogHandler.GetLogHandler.Log("(" + Thread.CurrentThread.Name + ") - " + p_exc.Message);
                }
                catch (SerializationException)
                {
                    LogHandler.GetLogHandler.Log("Received wrong data");
                    Thread.Sleep(m_Timeout);
                }

                Thread.Sleep(m_Timeout);
            }
        }

        /// <summary>
        /// Callback function to handle receiving data
        /// </summary>
        /// <param name="state"></param>
        private void ReceiveDataCallback(object state)
        {
            LogHandler.GetLogHandler.Log("Thread " + Thread.CurrentThread.ManagedThreadId + " started work");
            while (m_fileCount > 0)
            {
                while (m_tcpThreadClient.Client.Available > 0)
                {
                    byte[] buffer = new byte[m_BufferSize];
                    m_tcpThreadClient.Client.Receive(buffer, SocketFlags.Partial);
                    IFormatter formatter = new BinaryFormatter();
                    Stream stream = new MemoryStream(buffer);
                    Data receivedData = (Data)formatter.Deserialize(stream);
                    LogHandler.GetLogHandler.Log("Message from Thread: " + Thread.CurrentThread.ManagedThreadId +
                                                 " - file received - content: {" + receivedData.Header +
                                                 "\n" + Utils.ReadBytes(receivedData.Message) + "}");

                    Dictionary<string, string> headers = ProcessHeader(receivedData.Header);
                    try
                    {
                        string originFilename = headers["filename"];
                        string tempFileSize = headers["filesize"];
                        string user = headers["username"];
                        int fileSize = Convert.ToInt32(tempFileSize);

                        string name = GenerateFileName();

                        string userDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, user);
                        if (!Directory.Exists(userDirPath))
                        {
                            Directory.CreateDirectory(userDirPath);
                        }
                        
                        // save data to the file
                        FileStream fs = new FileStream(Path.Combine(userDirPath, name), FileMode.OpenOrCreate);
                        fs.Write(receivedData.Message, 0, fileSize);
                        fs.Close();

                        lock (m_fileLocker)
                        {
                            ReaderWriterLock locker = new ReaderWriterLock();
                            try
                            {
                                locker.AcquireWriterLock(int.MaxValue);
                                // save file information to csv file.
                                File.AppendAllLines(Path.Combine(userDirPath, Config.FileList), new string[]
                                {
                                    user + "," + originFilename + "," + fileSize + "," + name
                                });
                            }
                            finally
                            {
                                locker.ReleaseWriterLock();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHandler.GetLogHandler.Log("Thread " + Thread.CurrentThread.ManagedThreadId + " Failed to save file: " + ex.Message);
                    }

                    lock (m_threadLocker)
                    {
                        m_fileCount--;
                    }
                }
            }
            LogHandler.GetLogHandler.Log("Thread " + Thread.CurrentThread.ManagedThreadId + " finished work");
            // thread finished
        }

        /// <summary>
        /// Creates randomized file name 
        /// </summary>
        /// <returns>Generated file name</returns>
        private string GenerateFileName()
        {
            Random random = new Random();
            string name = String.Empty;

            for (int i = 0; i < 4; i++)
            {
                name += Convert.ToChar(random.Next(65, 90));
                name += random.Next(0, 9);
            }
            return name + ".txt";
        }

        private static Dictionary<string, string> ProcessHeader(string p_header)
        {
            int terminate = p_header.IndexOf("\0");
            if (terminate != -1)
            {
                p_header = p_header.Substring(0, terminate);
            }

            string[] splitted = p_header.Split("|", StringSplitOptions.None);
            Dictionary<string, string> headers = new Dictionary<string, string>();
            foreach (string s in splitted)
            {
                if (s.Contains(":"))
                {
                    headers.Add(s.Substring(0, s.IndexOf(":")), s.Substring(s.IndexOf(":") + 1));
                }
            }

            return headers;
        }

        private bool Connect()
        {
            try
            {
                m_tcpClient = new TcpClient(m_ServerAddress, m_ServerPort);
                m_tcpThreadClient = new TcpClient(m_ServerAddress, m_ThreadPort);
            }
            catch (Exception)
            {
                LogHandler.GetLogHandler.Log("Connection failed");
                return false;
            }
            return true;
        }

    }
}