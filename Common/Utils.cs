using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

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
                return ConfigurationManager.AppSettings.Get("ServerAddress"); ;
            }
        }

        public static int ServerPort
        {
            get
            {
                return Convert.ToInt32(ConfigurationManager.AppSettings.Get("ServerPort"));
            }
        }
    }
    public class Utils
    {
        public static string encodeBuffer(byte[] buffer, int fixedSize)
        {
            int temp = 0;
            char[] arr = new char[fixedSize + 1];
            while (temp != fixedSize)
            {
                int b = buffer[temp];
                arr[temp] = Convert.ToChar(b);
                temp++;
            }
            arr[fixedSize] = '\0';
            string str = new string(arr);
            return str;

        }
        public static int fixedBufferSize(byte[] buffer)
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
