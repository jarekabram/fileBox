using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Common
{
    [Serializable]
    public class Data
    {
        // byte ordered data
        public Data(int managedThreadId, string p_header, byte[] p_message)
        {
            // init
            ManagedThreadId = managedThreadId;
            Header = p_header;
            Message = p_message;
        }

        public int ManagedThreadId { get; private set; }
        public string Header { get; private set; }
        public byte[] Message { get; private set; }

    }
}