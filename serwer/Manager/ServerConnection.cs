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

        public ServerConnection(string p_Address, int p_Port)
        {
            m_Address = p_Address;
            m_Port = p_Port;

            Connect();
        }

        public void ProcessFile()
        {
            while (true)
            {
                byte[] header = new byte[m_BufferSize];
                try
                {
                    if (m_tcpClient.Client.Connected)
                    {
                        // Send request to check file availability
                        byte[] requestBuffer = BitConverter.GetBytes(FILE_AVAILABLE_REQUEST);
                        m_tcpClient.Client.Send(requestBuffer);

                        requestBuffer = null;
                        requestBuffer = new byte[4];

                        // if response is ok, have to wait for number of files as response
                        m_tcpClient.Client.Receive(requestBuffer, SocketFlags.Partial);
                        int fileCount = Utils.GetIntFromBytes(requestBuffer);
                        LogHandler.GetLogHandler.Log("Number of files to be processed: " + fileCount);

                        for (int i = 0; i < fileCount; i++)
                        {
                            var thread = new Thread(() =>
                            {
                                byte[] responseBuffer = null;
                                responseBuffer = new byte[m_BufferSize];

                                // receive will block the connection until next message comes
                                int size = m_tcpClient.Client.Receive(responseBuffer, SocketFlags.Partial);
                                IFormatter formatter = new BinaryFormatter();
                                Stream stream = new MemoryStream(responseBuffer);
                                Data receivedData = (Data)formatter.Deserialize(stream);

                                string[] splitted = receivedData.Header.Split("|", StringSplitOptions.None);
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
                                Console.WriteLine("Received Data: { Id: " + receivedData.ManagedThreadId +
                                                                    ", Header: " + receivedData.Header +
                                                                    ", Message: " + Utils.ReadBytes(receivedData.Message) + "}");
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