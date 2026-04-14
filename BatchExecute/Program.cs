using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;

namespace BatchExecute
{
    class Program
    {
        static void Main(string[] args)
        {
            callBatch(args[0]);
        }

        static private int callBatch(string batFile)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = @"C:\WINDOWS\system32\cmd.exe"; //System.Environment.GetEnvironmentVariable("ComSpec");//@"C:\WINDOWS\system32\cmd.exe";
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            //startInfo.Verb = "RunAs";
            startInfo.Arguments = "/c " + batFile + " ";
            Process process = Process.Start(startInfo);
            process.WaitForExit();
            return process.ExitCode;
        }
    }
}
