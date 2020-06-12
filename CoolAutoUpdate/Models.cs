using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIH.Update
{
    public class UpdateConfig
    {
        public string Version { get; set; }
        public string Url { get; set; }
        public bool Mandatory { get; set; }
        public string MD5 { get; set; }
        public string Message { get; set; }
        public int TimSpan { get; set; }
    }

    public enum OpeType
    {
        Start = 0,
        Stop = 1
    }
}
