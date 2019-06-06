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
        private const int FILE_AVAILABLE_REQUEST = 0x01;
        private const int m_BufferSize = 1024;
        private const int m_Timeout = 5000;
        private static TcpClient m_tcpClient = null;
        
        private string m_Address;
        private int m_Port;
        private readonly object locker = new object();

        public ServerConnection(string p_Address, int p_Port)
        {
            m_Address = p_Address;
            m_Port = p_Port;

            Connect();
        }

        public void ProcessFile()
        {
            bool stop = true;
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
                        lock (locker)
                        {
                            if (stop)
                            {
                                LogHandler.GetLogHandler.Log("Receive 1");
                                m_tcpClient.Client.Receive(requestBuffer, SocketFlags.Partial);
                                LogHandler.GetLogHandler.Log("after Receive 1");
                            }
                        }
                        int fileCount = Utils.GetIntFromBytes(requestBuffer);
                        LogHandler.GetLogHandler.Log("Number of files to be processed: " + fileCount);
                        int counter = 0;
                        for (int i = 0; i < fileCount; i++)
                        {
                            stop = false;
                            var thread = new Thread(() =>
                            {
                                lock (locker)
                                {
                                    byte[] header = null;
                                    header = new byte[m_BufferSize];

                                    LogHandler.GetLogHandler.Log("Receive 2");
                                    m_tcpClient.Client.Receive(header);
                                    LogHandler.GetLogHandler.Log("after Receive 2");
                                    Dictionary<string, string> headers = ProcessHeader(header);

                                    string filename = String.Empty;
                                    string tempFileSize = String.Empty;
                                    string user = String.Empty;
                                    int fileSize = 0;

                                    try
                                    {
                                        bool ok = headers.TryGetValue("filename", out filename);

                                        // Force retry due to some missing packets when sending from queue
                                        while (!ok)
                                        {
                                            header = new byte[m_BufferSize];

                                            LogHandler.GetLogHandler.Log("Receive 3");
                                            m_tcpClient.Client.Receive(header);
                                            LogHandler.GetLogHandler.Log("after Receive 3");
                                            headers = ProcessHeader(header);
                                            ok = headers.TryGetValue("filename", out filename);
                                        }
                                        headers.TryGetValue("filesize", out tempFileSize);
                                        headers.TryGetValue("username", out user);
                                        fileSize = Convert.ToInt32(tempFileSize);

                                        byte[] buffer = null;
                                        LogHandler.GetLogHandler.Log("received header: { file: " + filename + " size: " + fileSize + " user: " + user);

                                        //requestBuffer = new byte[m_BufferSize];
                                        //Array.Copy(Encoding.ASCII.GetBytes("ok"), requestBuffer, Encoding.ASCII.GetBytes("ok").Length);
                                        //m_tcpClient.Client.Send(requestBuffer);
                                        string name = GenerateFileName();

                                        FileStream fs = new FileStream(name, FileMode.OpenOrCreate);
                                        while (fileSize > 0)
                                        {
                                            buffer = new byte[m_BufferSize];
                                            LogHandler.GetLogHandler.Log("Receive 4");
                                            int size = m_tcpClient.Client.Receive(buffer, SocketFlags.Partial);
                                            LogHandler.GetLogHandler.Log("after Receive 4");
                                            string message = Utils.DecodeBuffer(buffer, Utils.FixedBufferSize(buffer));
                                            fs.Write(buffer, 0, fileSize);
                                            LogHandler.GetLogHandler.Log("file received - content: {" + message + "}");
                                            fileSize -= size;
                                        }
                                        fs.Close();


                                        ReaderWriterLock locker = new ReaderWriterLock();
                                        try
                                        {
                                            locker.AcquireWriterLock(int.MaxValue);
                                            File.AppendAllLines(Config.FileList, new string[]
                                            {
                                                user + "," + filename + "," + Convert.ToString(fileSize)
                                            });
                                        }
                                        finally
                                        {
                                            locker.ReleaseWriterLock();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        LogHandler.GetLogHandler.Log("Failed to save file: " + ex.Message);
                                    }

                                    counter++;
                                    LogHandler.GetLogHandler.Log("Thread " + Thread.CurrentThread.ManagedThreadId + " finished work");
                                    if (counter == fileCount)
                                    {
                                        stop = true;
                                        counter = 0;
                                    }
                                }
                                // thread finished
                            });
                            thread.Start();

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

        private static Dictionary<string, string> ProcessHeader(byte[] header)
        {
            string headerStr = Encoding.ASCII.GetString(header);
            //LogHandler.GetLogHandler.Log("received message " + headerStr);

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

            return headers;
        }

        private bool Connect()
        {
            try
            {
                m_tcpClient = new TcpClient(m_Address, m_Port);
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