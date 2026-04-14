using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CAS
{
    class ADCPClass
    {
        #region Fields

        public const string LED_PITCH1_2 = "\"1.2\"";
        public const string LED_PITCH1_5 = "\"1.5\"";

        public const string ZRD_B12A = "\"zrd-b12a\"";
        public const string ZRD_B15A = "\"zrd-b15a\"";
        public const string ZRD_C12A = "\"zrd-c12a\"";
        public const string ZRD_C15A = "\"zrd-c15a\"";
        public const string ZRD_BH12D = "\"zrd-bh12d\"";
        public const string ZRD_BH15D = "\"zrd-bh15d\"";
        public const string ZRD_CH12D = "\"zrd-ch12d\"";
        public const string ZRD_CH15D = "\"zrd-ch15d\"";
        public const string ZRD_BH12D_S3 = "\"zrd-bh12d/3\"";
        public const string ZRD_BH15D_S3 = "\"zrd-bh15d/3\"";
        public const string ZRD_CH12D_S3 = "\"zrd-ch12d/3\"";
        public const string ZRD_CH15D_S3 = "\"zrd-ch15d/3\"";
        #endregion Fields

        #region Commands

        // ControllerのPower Status取得するコマンド
        public static readonly string CmdControllerPowerStausGet = "power_status ?";

        // Controllerに設定された LED Model取得するコマンド
        public static readonly string CmdLedModelGet = "led_model ?";

        // Controllerに設定された LED Pitch取得するコマンド
        public static readonly string CmdLedPitchGet = "led_pitch ?";

        #endregion Commands

        // Sockets
        private static System.Net.Sockets.TcpClient tcp;
        private static System.Net.Sockets.NetworkStream ns;

        public static bool sendAdcpCommand(string cmd, out string response, string ip, string password)
        {
            response = "";
            string msg;

            try
            {
                if (login(ip, password) != true)
                {
                    msg = "SendADCP NG : Failed in login.";
                    MainWindow.ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 380, 210);
                    return false;
                }

                if (sendCommand(cmd, out response) != true)
                {
                    msg = "SendADCP NG : Failed in sendCommand.";
                    MainWindow.ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 380, 210);
                    return false;
                }
            }
            catch (Exception ex)
            {
                msg = "[sendAdcpCommand] Source : " + ex.Source + "\r\nException Message : " + ex.Message;
                MainWindow.ShowMessageWindow(msg, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }

            return true;
        }

        private static bool login(string ip, string password)
        {
            const int port = 53595;

            tcp = new System.Net.Sockets.TcpClient(ip, port);
            ns = tcp.GetStream();

            ns.ReadTimeout = 10000;
            ns.WriteTimeout = 10000;

            System.Threading.Thread.Sleep(500);

            //サーバーから送られたデータを受信する
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            byte[] resBytes = new byte[256];
            int resSize = 0;

            do
            {
                //データの一部を受信する
                resSize = ns.Read(resBytes, 0, resBytes.Length);

                //Readが0を返した時はサーバーが切断したと判断
                if (resSize == 0)
                {
                    //Console.WriteLine("サーバーが切断しました。");
                    break;
                }

                //受信したデータを蓄積する
                ms.Write(resBytes, 0, resSize);
            }
            while (ns.DataAvailable || resBytes[resSize - 1] != '\n');

            //受信したデータを文字列に変換
            System.Text.Encoding enc = System.Text.Encoding.UTF8;

            string resMsg = enc.GetString(ms.GetBuffer(), 0, (int)ms.Length);
            ms.Close();

            resMsg = resMsg.Replace("\r\n", "");
            if (resMsg == "NOKEY")
            { return true; }

            resMsg += password;

            string sendMsg;
            getSha256Hash(resMsg, out sendMsg);

            // Send Hash
            //文字列をByte型配列に変換
            byte[] sendBytes = enc.GetBytes(sendMsg + "\r\n");

            //データを送信する
            ns.Write(sendBytes, 0, sendBytes.Length);
            Console.WriteLine(sendMsg);

            //サーバーから送られたデータを受信する
            ms = new System.IO.MemoryStream();
            resBytes = new byte[256];
            resSize = 0;

            do
            {
                //データの一部を受信する
                resSize = ns.Read(resBytes, 0, resBytes.Length);
                //Readが0を返した時はサーバーが切断したと判断
                if (resSize == 0)
                {
                    Console.WriteLine("サーバーが切断しました。");
                    break;
                }

                //受信したデータを蓄積する
                ms.Write(resBytes, 0, resSize);
            }
            while (ns.DataAvailable || resBytes[resSize - 1] != '\n');

            //受信したデータを文字列に変換
            resMsg = enc.GetString(ms.GetBuffer(), 0, (int)ms.Length);
            ms.Close();

            resMsg = resMsg.Replace("\r\n", "");

            if (resMsg != "OK")
            { return false; }

            return true;
        }

        private static bool getSha256Hash(string source, out string hash)
        {
            hash = "";

            // SHA256のハッシュ値を取得する
            using (SHA256 crypto = new SHA256CryptoServiceProvider())
            {
                // テキストをUTF-8エンコードでバイト配列化
                byte[] byteValue = Encoding.UTF8.GetBytes(source);

                byte[] hashValue = crypto.ComputeHash(byteValue);

                // バイト配列をUTF8エンコードで文字列化
                StringBuilder hashedText = new StringBuilder();
                for (int i = 0; i < hashValue.Length; i++)
                { hashedText.AppendFormat("{0:X2}", hashValue[i]); }

                hash = hashedText.ToString();
            }

            return true;
        }

        private static bool sendCommand(string cmd, out string res)
        {
            res = "";

            System.Text.Encoding enc = System.Text.Encoding.UTF8;

            //文字列をByte型配列に変換
            byte[] sendBytes = enc.GetBytes(cmd + "\r\n");

            //データを送信する
            ns.Write(sendBytes, 0, sendBytes.Length);

            System.Threading.Thread.Sleep(500);

            //サーバーから送られたデータを受信する
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            byte[] resBytes = new byte[256];
            int resSize = 0;

            do
            {
                //データの一部を受信する
                resSize = ns.Read(resBytes, 0, resBytes.Length);
                //Readが0を返した時はサーバーが切断したと判断
                if (resSize == 0)
                {
                    Console.WriteLine("サーバーが切断しました。");
                    break;
                }

                //受信したデータを蓄積する
                ms.Write(resBytes, 0, resSize);
            }
            while (ns.DataAvailable || resBytes[resSize - 1] != '\n');

            //受信したデータを文字列に変換
            string resMsg = enc.GetString(ms.GetBuffer(), 0, (int)ms.Length);
            res = resMsg.Replace("\r\n", "");

            //閉じる
            ms.Close();
            ns.Close();
            tcp.Close();

            return true;
        }
    }
}
