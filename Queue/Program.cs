using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Common;

namespace test_queue
{
    class Program
    {
        // buffer size constant
        private const int m_BufferSize = 1024;
        private const int m_Timeout = 2500;

        // for polling from client side
        static TcpListener m_clientListener = null;
        // for polling from server side
        static TcpListener m_serverListener = null;

        // message queue - data sent from client is placed
        static Queue<Data> m_queue = new Queue<Data>();
        static HashSet<TcpClient> m_listConnectedClients = new HashSet<TcpClient>();
        static readonly object locker = new object();

        static void Main(string[] args)
        {

            // for polling from client side
            m_clientListener = new TcpListener(IPAddress.Parse(Config.ClientAddress), Config.ClientPort);
            m_clientListener.Start();
            // for polling from server side
            m_serverListener = new TcpListener(IPAddress.Parse(Config.ServerAddress), Config.ServerPort);
            m_serverListener.Start();

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
                                                             ", Received Data: { Id: " + receivedData.ManagedThreadId +
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
                LogHandler.GetLogHandler.Log("processServer()");
                try
                {
                    TcpClient client = m_serverListener.AcceptTcpClient();

                    while (true)
                    {
                        byte[] requestBuffer = new byte[4];
                        // if the received buffer contains FILE_AVAILABLE_REQUEST value then try to send data or 
                        // inform back that file is not available
                        client.Client.Receive(requestBuffer, SocketFlags.Partial);
                        int result = Utils.GetIntFromBytes(requestBuffer);
                        LogHandler.GetLogHandler.Log("received message from server: " + result);

                        requestBuffer = null;
                        requestBuffer = BitConverter.GetBytes(m_queue.Count);
                        client.Client.Send(requestBuffer);

                        if (result == 0x01)
                        {
                            for (int i = 0; i < m_queue.Count; i++)
                            {
                                Thread thread = new Thread(() => {

                                    // ok send file or inform that queue is empty
                                    if (m_queue.Count > 0)
                                    {
                                        lock (locker)
                                        {
                                            Data data = m_queue.Dequeue();
                                            byte[] response = Utils.ObjectToByteArray(data);
                                            client.Client.Send(response);
                                            Thread.Sleep(m_Timeout);
                                        }
                                    }
                                    else
                                    {
                                        // nothing to send, just set timeout in client 
                                        byte[] buffer = new byte[1];
                                        client.Client.Send(buffer, 1, SocketFlags.Partial);
                                    }
                                });
                                thread.Start();
                            }
                        }
                        else if (result == -1)
                        {
                            // invalid data
                            LogHandler.GetLogHandler.Log("invalid data");
                            Thread.Sleep(m_Timeout);
                        }
                        else
                        {
                            // there were no request sent
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
    }
}
