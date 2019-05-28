using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public class UserData
    {
        public string Username { get; }
        public List<string> FileName { get; }

        public UserData(string p_Username)
        {
            Username = p_Username;
        }

        public void AddFile(string p_Filename)
        {
            FileName.Add(p_Filename);
        }

        public void RemoveFile(string p_Filename)
        {
            FileName.Remove(p_Filename);
        }

    }
}
