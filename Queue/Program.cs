using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using Common;

namespace test_queue
{
    class Program
    {
        // buffer size constant
        private const int m_BufferSize = 1024;
        private const int m_Timeout = 2500;

        private const int m_threadCount = 5;

        // for polling from client side
        static TcpListener m_clientListener = null;
        // for polling from server side
        static TcpListener m_serverListener = null;
        static TcpListener m_serverThreadListener = null;

        static TcpClient m_serverClient = null;
        static TcpClient m_threadClient = null;

        // message queue - data sent from client is placed
        static Queue<Data> m_queue = new Queue<Data>();
        static HashSet<TcpClient> m_listConnectedClients = new HashSet<TcpClient>();
        static readonly object m_threadLock = new object();
        
        static void Main(string[] args)
        {

            // for polling from client side
            m_clientListener = new TcpListener(IPAddress.Parse(Config.ClientAddress), Config.ClientPort);
            m_clientListener.Start();
            // for polling from server side
            m_serverListener = new TcpListener(IPAddress.Parse(Config.ServerAddress), Config.ServerPort);
            m_serverListener.Start();

            m_serverThreadListener = new TcpListener(IPAddress.Parse(Config.ServerAddress), 4446);
            m_serverThreadListener.Start();

            Thread clientThread = new Thread(() => {
                ProcessClient();
            });

            Thread serverThread = new Thread(() => {
                ProcessServer();
            });

            clientThread.Start();
            serverThread.Start();

        }

        /// <summary>
        /// Handle connection with multiple clients
        /// </summary>
        private static void ProcessClient()
        {
            // that would be connection for requesting a data for a specified amout of time
            while (true)
            {
                LogHandler.GetLogHandler.Log("processClient()");
                try
                {
                    TcpClient client = m_clientListener.AcceptTcpClient();
                    m_listConnectedClients.Add(client);

                    Thread clientThread = new Thread(() =>
                    {
                        while (true)
                        {
                            byte[] buffer = null;

                            while (client.Available > 0)
                            {
                                buffer = new byte[m_BufferSize];
                                int size = client.Client.Receive(buffer, SocketFlags.Partial);
                                IFormatter formatter = new BinaryFormatter();
                                Stream stream = new MemoryStream(buffer);
                                Data receivedData = (Data)formatter.Deserialize(stream);

                                LogHandler.GetLogHandler.Log("Clients: " + m_listConnectedClients.Count +
                                                             ", Queue count: " + m_queue.Count +
                                                             ", Header: " + receivedData.Header +
                                                             ", Message: " + Utils.ReadBytes(receivedData.Message) + "}");
                                m_queue.Enqueue(receivedData);
                            }

                            // ping server to check connection availability
                            if (!Utils.Ping(client))
                            {
                                m_listConnectedClients.Remove(client);
                                return;
                            }
                        }
                    });
                    clientThread.Start();
                }
                catch (Exception p_exc)
                {
                    LogHandler.GetLogHandler.Log(p_exc.Message);
                }
            }
        }

        /// <summary>
        /// Handle connection with one server
        /// </summary>
        private static void ProcessServer()
        {
            // that would be connection for requesting a data for a specified amout of time
            while (true)
            {
                try
                {
                    m_serverClient = m_serverListener.AcceptTcpClient();
                    m_threadClient = m_serverThreadListener.AcceptTcpClient();

                    while (true)
                    {
                        byte[] requestBuffer = new byte[m_BufferSize];
                        // if the received buffer contains FILE_AVAILABLE_REQUEST value then try to send data or 
                        // inform back that file is not available
                        m_serverClient.Client.Receive(requestBuffer, SocketFlags.Partial);
                        int receivedValue = Utils.GetIntFromBytes(requestBuffer);
                        LogHandler.GetLogHandler.Log("received message from server: " + receivedValue);

                        requestBuffer = new byte[m_BufferSize];
                        Array.Copy(BitConverter.GetBytes(m_queue.Count), requestBuffer, BitConverter.GetBytes(m_queue.Count).Length);

                        m_serverClient.Client.Send(requestBuffer);

                        if (receivedValue == 0x01)
                        {
                            if (m_queue.Count != 0)
                            {
                                for (int i = 0; i < m_threadCount; i++)
                                {
                                    ThreadPool.QueueUserWorkItem(new WaitCallback(SendDataCallback));
                                }
                            }
                        }
                        else if (receivedValue == -1)
                        {
                            LogHandler.GetLogHandler.Log("invalid data");
                            Thread.Sleep(m_Timeout);
                        }
                        else
                        {
                            LogHandler.GetLogHandler.Log("there were no request sent");
                            Thread.Sleep(m_Timeout);
                        }
                    }
                }
                catch (Exception p_exc)
                {
                    LogHandler.GetLogHandler.Log(p_exc.Message);
                }
            }
        }

        private static void SendDataCallback(object state)
        {
            LogHandler.GetLogHandler.Log("Thread " + Thread.CurrentThread.ManagedThreadId + " started work");
            while (m_queue.Count > 0)
            {
                // ok send file or inform that queue is empty
                Data data = null;
                bool result = false;
                lock (m_threadLock)
                {
                    result = m_queue.TryDequeue(out data);
                }
                if (result == true)
                {
                    byte[] array = Utils.ObjectToByteArray(data);
                    byte[] message = new byte[m_BufferSize];
                    Array.Copy(array, message, array.Length);
                    m_threadClient.Client.Send(message, message.Length, SocketFlags.Partial);

                    LogHandler.GetLogHandler.Log("Message from Thread: " + Thread.CurrentThread.ManagedThreadId + " " + data.Header);
                }
                else
                {
                    LogHandler.GetLogHandler.Log("Message from Thread: " + Thread.CurrentThread.ManagedThreadId + " - Queue is empty");
                }
                Thread.Sleep(1000);
            }
            LogHandler.GetLogHandler.Log("Message from Thread: " + Thread.CurrentThread.ManagedThreadId + " finished work");
        }
    }
}
