using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CAS
{
    // プロセス間通信用のObject
    public class IpcObject : MarshalByRefObject
    {
        public bool CloseFlag;

        public bool ShootFlag;
        public string Path;

        public int ImageSize = 1;
        public string FNumber = "";
        public string Shutter = "";
        public string ISO = "";
        public string WB = "";
        public uint CompressionType = 16;

        //public void SetTargetDir(string targetDir)
        //{
        //    TargetDir = targetDir;
        //}

        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
