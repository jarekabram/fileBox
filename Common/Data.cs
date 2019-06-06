using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Common
{
    [Serializable]
    public class Data
    {
        // byte ordered data
        public Data(string p_header, byte[] p_message)
        {
            // init
            Header = p_header;
            Message = p_message;
        }

        public string Header { get; private set; }
        public byte[] Message { get; private set; }

    }
}