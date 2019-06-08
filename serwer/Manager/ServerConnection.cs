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
        private static TcpClient m_tcpThreadClient = null;

        private string m_Address;
        private int m_Port;
        private readonly object threadLock = new object();

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
                        LogHandler.GetLogHandler.Log("");
                        // Send request to check file availability
                        byte[] requestBuffer = new byte[m_BufferSize];
                        Array.Copy(BitConverter.GetBytes(FILE_AVAILABLE_REQUEST), requestBuffer, BitConverter.GetBytes(FILE_AVAILABLE_REQUEST).Length);
                        m_tcpClient.Client.Send(requestBuffer);
                        LogHandler.GetLogHandler.Log("");

                        // if response is ok, have to wait for number of files as response
                        requestBuffer = new byte[m_BufferSize];
                        if (stop)
                        {
                            LogHandler.GetLogHandler.Log("");
                            m_tcpClient.Client.Receive(requestBuffer, SocketFlags.Partial);
                            LogHandler.GetLogHandler.Log("");
                        }
                        int fileCount = Utils.GetIntFromBytes(requestBuffer);
                        LogHandler.GetLogHandler.Log("Number of files to be processed: " + fileCount);
                        int counter = 0;
                        for (int i = 0; i < fileCount; i++)
                        {
                            stop = false;
                            var thread = new Thread(() =>
                            {
                                while (m_tcpThreadClient.Client.Available > 0)
                                {
                                    byte[] buffer = new byte[m_BufferSize];
                                    int size = m_tcpThreadClient.Client.Receive(buffer, SocketFlags.Partial);
                                    IFormatter formatter = new BinaryFormatter();
                                    Stream stream = new MemoryStream(buffer);
                                    Data receivedData = (Data)formatter.Deserialize(stream);

                                    Dictionary<string, string> headers = ProcessHeader(receivedData.Header);

                                    try
                                    {
                                        string originFilename = headers["filename"];
                                        string tempFileSize = headers["filesize"];
                                        string user = headers["username"];
                                        int fileSize = Convert.ToInt32(tempFileSize);

                                        LogHandler.GetLogHandler.Log("Message from Thread: " + Thread.CurrentThread.ManagedThreadId + " - received header: { file: " + originFilename + " size: " + fileSize + " user: " + user);

                                        string name = GenerateFileName();

                                        FileStream fs = new FileStream(name, FileMode.OpenOrCreate);

                                        LogHandler.GetLogHandler.Log("Message from Thread: " + Thread.CurrentThread.ManagedThreadId + " WAitnig for message");
                                        fs.Write(receivedData.Message, 0, fileSize);
                                        LogHandler.GetLogHandler.Log("Message from Thread: " + Thread.CurrentThread.ManagedThreadId + " - file received - content: {" + Utils.ReadBytes(receivedData.Message) + "}");

                                        fs.Close();

                                        ReaderWriterLock locker = new ReaderWriterLock();
                                        try
                                        {
                                            locker.AcquireWriterLock(int.MaxValue);
                                            File.AppendAllLines(Config.FileList, new string[]
                                            {
                                            user + "," + originFilename + "," + fileSize + "," + name
                                            });
                                        }
                                        finally
                                        {
                                            locker.ReleaseWriterLock();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        LogHandler.GetLogHandler.Log("Thread " + Thread.CurrentThread.ManagedThreadId + " Failed to save file: " + ex.Message);
                                    }
                                }

                                

                                counter++;
                                LogHandler.GetLogHandler.Log("Thread " + Thread.CurrentThread.ManagedThreadId + " finished work");
                                if (counter == fileCount)
                                {
                                    stop = true;
                                    counter = 0;
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
                LogHandler.GetLogHandler.Log("");

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

        private static Dictionary<string, string> ProcessHeader(byte[] p_header)
        {
            string headerStr = Encoding.ASCII.GetString(p_header);
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
                m_tcpClient = new TcpClient(m_Address, m_Port);
                m_tcpThreadClient = new TcpClient(m_Address, 4446);
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