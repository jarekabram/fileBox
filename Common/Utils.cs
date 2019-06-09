using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Net.Sockets;

namespace Common
{
    public class Config
    {
        public static string ClientPath
        {
            get
            {
                return ConfigurationManager.AppSettings.Get("ClientPath");
            }
        }

        public static string ServerAddress
        {
            get
            {
                return ConfigurationManager.AppSettings.Get("ServerAddress");
            }
        }
        public static string FileList
        {
            get
            {
                return ConfigurationManager.AppSettings.Get("FileList");
            }
        }

        public static int ServerPort
        {
            get
            {
                return Convert.ToInt32(ConfigurationManager.AppSettings.Get("ServerPort"));
            }
        }

        public static string ClientAddress
        {
            get
            {
                return ConfigurationManager.AppSettings.Get("ClientAddress");
            }
        }
        public static int ClientPort
        {
            get
            {
                return Convert.ToInt32(ConfigurationManager.AppSettings.Get("ClientPort"));
            }
        }
        public static int ThreadPort
        {
            get
            {
                return Convert.ToInt32(ConfigurationManager.AppSettings.Get("ThreadPort"));
            }
        }
    }
    public class Utils
    {
        /// <summary>
        /// Truncates the buffer array to the first met null termination character and converts bytes to chars
        /// </summary>
        /// <param name="p_buffer"></param>
        /// <param name="p_fixedSize"></param>
        /// <returns>Decoded value</returns>
        public static string FixedSizeStringFromBufferArray(byte[] p_buffer, int p_fixedSize)
        {
            int temp = 0;
            char[] arr = new char[p_fixedSize + 1];
            while (temp != p_fixedSize)
            {
                int b = p_buffer[temp];
                arr[temp] = Convert.ToChar(b);
                temp++;
            }
            arr[p_fixedSize] = '\0';
            string str = new string(arr);
            return str;

        }

        /// <summary>
        /// Cuts the rest of empty buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static int FixedBufferSize(byte[] buffer)
        {
            int temp = 0;
            while (temp != buffer.Length)
            {
                if (buffer[temp] == 0)
                {
                    break;
                }
                temp++;
            }
            return temp;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_bytes"></param>
        /// <returns></returns>
        public static string ReadBytes(byte[] p_bytes)
        {
            string result = null;
            foreach (var singleByte in p_bytes)
            {
                result += Convert.ToChar(singleByte);
            }

            return result;
        }

        /// <summary>
        /// This function is used only for checking of file availability flag
        /// </summary>
        /// <param name="p_bytes"></param>
        /// <returns></returns>
        public static int GetIntFromBytes(byte[] p_bytes)
        {
            if (p_bytes.Length == 1024)
            {
                return BitConverter.ToInt32(p_bytes);
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Converts provided object into byte array
        /// </summary>
        /// <param name="p_obj"></param>
        /// <returns></returns>
        public static byte[] ObjectToByteArray(object p_obj)
        {
            if (p_obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, p_obj);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Checks the connection with the remote host.
        /// </summary>
        /// <param name="p_client"></param>
        /// <returns></returns>
        public static bool Ping(TcpClient p_client)
        {
            bool result = true;
            
            // Detect if host connected or not
            if (p_client.Client.Poll(0, SelectMode.SelectRead))
            {
                byte[] buff = new byte[1];
                try
                {
                    p_client.Client.Receive(buff, SocketFlags.Peek);
                    result = true;
                }
                catch (Exception)
                {
                    // Client disconnected
                    LogHandler.GetLogHandler.Log("Remote host has been disconnected ...");
                    result = false;
                }
            }
            return result;
        }

        /// <summary>
        /// Adding field in context menu under right mouse click
        /// </summary>
        /// <returns></returns>
        public static RegistryKey AddContextMenuButton()
        {
            RegistryKey registryKey;

            registryKey = Registry.ClassesRoot.CreateSubKey(@"Directory\shell\fileBox");
            registryKey = Registry.ClassesRoot.CreateSubKey(@"Directory\shell\fileBox\command");
            registryKey.SetValue("", Config.ClientPath + " \"" + "%1" + "\"");
            return registryKey;
        }
        public static void RemoveContextMenuButton(RegistryKey registryKey)
        {
            registryKey.DeleteValue("");
            Registry.ClassesRoot.DeleteSubKey(@"Directory\shell\fileBox\command");
            Registry.ClassesRoot.DeleteSubKey(@"Directory\shell\fileBox");
        }
    }
}
