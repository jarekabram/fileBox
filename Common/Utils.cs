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
    }
}
