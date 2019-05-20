using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
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
            registryKey.SetValue("", @"F:\Studia\II_stopien\II_semestr\mpw\fileBox\klient\bin\Debug\netcoreapp2.2\win10-x64\publish\Klient.exe " + "\"" + "%1" + "\"");

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
