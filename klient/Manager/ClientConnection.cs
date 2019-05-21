using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Common;


namespace Klient.Manager
{
    class ClientConnection
    {
        private TcpClient _tcpClient;
        private const int m_BufferSize = 1024;
        private string m_User = null;
        public ClientConnection(string p_User, string p_Server, int p_Port)
        {
            m_User = p_User;
            _tcpClient = new TcpClient(p_Server, p_Port);
            _tcpClient.SendTimeout = 600000;
            _tcpClient.ReceiveTimeout = 600000;
        }

        public void WatchDirectory(string p_Path)
        {
            p_Path = Path.Combine(p_Path, m_User);

            A: if (Directory.Exists(p_Path))
            {
                int fileCount = Directory.GetFiles(p_Path).Length;

                while (true)
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
                        catch (Exception ex)
                        {
                            LogHandler.GetLogHandler.Log(ex.Message);
                            throw;
                        }
                    }
                    else if (fileCount > Directory.GetFiles(p_Path).Length)
                    {
                        LogHandler.GetLogHandler.Log("Removed a file or directory");
                        fileCount = Directory.GetFiles(p_Path).Length;
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(p_Path);
                goto A;
            }
        }
    }
}
