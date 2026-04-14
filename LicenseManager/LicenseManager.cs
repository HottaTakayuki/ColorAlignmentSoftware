using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CAS
{
    public class LicenseManager
    {
        private const string NTP_SERVER = "https://ntp-a1.nict.go.jp/cgi-bin/time";
        private const string SALT = "その道に入らんと思ふ心こそ我身ながらの師匠なりけれ";
        private const string password = "茶の湯とはただ湯をわかし茶を点ててのむばかりなることと知るべし";

        private static string appliPath;

        public static bool CheckLicense()
        {
            DateTime curDate, buildDate, lastDate;
            DateTime cordingDate = new DateTime(2016, 10, 19);

            // 現在日時を取得する
            if (getNtpTime(out curDate) != true)
            {
                // NTPサーバーから日時を取得できない場合はシステムの時間を使用する
                curDate = System.DateTime.Now;
            }

            // DLLの更新日時を取得する
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
            appliPath = System.IO.Path.GetDirectoryName(asm.Location);
            var info = new System.IO.FileInfo(asm.Location);
            buildDate = info.LastWriteTime;

            // Build時から1年以内であること
            if(curDate < cordingDate || curDate < buildDate)
            { return false; }

            if (curDate > cordingDate.AddYears(1) || curDate > buildDate.AddYears(1))
            { return false; }

            // 前回起動時以降であること
            loadLastDate(out lastDate);
            if (lastDate > curDate)
            { return false; }
            
            // 最終日時を更新する
            saveLastDate(curDate);

            return true;
        }

        private static bool getNtpTime(out DateTime currentTime)
        {
            currentTime = new DateTime();

            //HttpWebRequestを作成
            System.Net.HttpWebRequest webreq = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(NTP_SERVER);

            try
            {
                //サーバーからの応答を受信するためのHttpWebResponseを取得
                using (System.Net.HttpWebResponse webres = (System.Net.HttpWebResponse)webreq.GetResponse())
                {
                    //応答データを受信するためのStreamを取得
                    using (System.IO.Stream st = webres.GetResponseStream())
                    {
                        //文字コードを指定して、StreamReaderを作成
                        using (System.IO.StreamReader sr = new System.IO.StreamReader(st, System.Text.Encoding.UTF8))
                        {
                            //データをすべて受信
                            string htmlSource = sr.ReadToEnd();
                            htmlSource = htmlSource.Replace("\n", "");
                            htmlSource = htmlSource.Replace(" JST", "");

                            //currentTime = DateTime.Parse(htmlSource);

                            //"Wed, 25 Dec 2002 13:32:22 +0900"をDateTime型に変更する
                            //ここでは"ddd, d MMM yyyy HH':'mm':'ss zzz"
                            currentTime = System.DateTime.ParseExact(htmlSource,
                                        "ddd MMM d HH':'mm':'ss yyyy",
                                        System.Globalization.DateTimeFormatInfo.InvariantInfo,
                                        System.Globalization.DateTimeStyles.None);
                        }
                    }
                }
            }
            catch
            { return false; }            

            return true;
        }

        private static bool loadLastDate(out DateTime lastDate)
        {
            lastDate = new DateTime(2100, 1, 1);
            string strDate;

            using (System.IO.StreamReader sr = new System.IO.StreamReader(appliPath + "\\devenv.dll"))
            { strDate = sr.ReadToEnd(); }

            strDate = DecryptString(strDate, password);

            lastDate = DateTime.Parse(strDate);

            return true;
        }

        private static bool saveLastDate(DateTime date)
        {
            string strDate = date.ToString("yyyy/MM/dd HH:mm:ss");

            strDate = EncryptString(strDate, password);

            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(appliPath + "\\devenv.dll"))
            { sw.Write(strDate); }

            return true;
        }

        /// <summary>
        /// 文字列を暗号化する
        /// </summary>
        /// <param name="sourceString">暗号化する文字列</param>
        /// <param name="password">暗号化に使用するパスワード</param>
        /// <returns>暗号化された文字列</returns>
        public static string EncryptString(string sourceString, string password)
        {
            //RijndaelManagedオブジェクトを作成
            System.Security.Cryptography.RijndaelManaged rijndael =
                new System.Security.Cryptography.RijndaelManaged();

            //パスワードから共有キーと初期化ベクタを作成
            byte[] key, iv;
            GenerateKeyFromPassword(
                password, rijndael.KeySize, out key, rijndael.BlockSize, out iv);
            rijndael.Key = key;
            rijndael.IV = iv;

            //文字列をバイト型配列に変換する
            byte[] strBytes = System.Text.Encoding.UTF8.GetBytes(sourceString);

            //対称暗号化オブジェクトの作成
            System.Security.Cryptography.ICryptoTransform encryptor =
                rijndael.CreateEncryptor();
            //バイト型配列を暗号化する
            byte[] encBytes = encryptor.TransformFinalBlock(strBytes, 0, strBytes.Length);
            //閉じる
            encryptor.Dispose();

            //バイト型配列を文字列に変換して返す
            return System.Convert.ToBase64String(encBytes);
        }

        /// <summary>
        /// 暗号化された文字列を復号化する
        /// </summary>
        /// <param name="sourceString">暗号化された文字列</param>
        /// <param name="password">暗号化に使用したパスワード</param>
        /// <returns>復号化された文字列</returns>
        public static string DecryptString(string sourceString, string password)
        {
            //RijndaelManagedオブジェクトを作成
            System.Security.Cryptography.RijndaelManaged rijndael =
                new System.Security.Cryptography.RijndaelManaged();

            //パスワードから共有キーと初期化ベクタを作成
            byte[] key, iv;
            GenerateKeyFromPassword(
                password, rijndael.KeySize, out key, rijndael.BlockSize, out iv);
            rijndael.Key = key;
            rijndael.IV = iv;

            //文字列をバイト型配列に戻す
            byte[] strBytes = System.Convert.FromBase64String(sourceString);

            //対称暗号化オブジェクトの作成
            System.Security.Cryptography.ICryptoTransform decryptor =
                rijndael.CreateDecryptor();
            //バイト型配列を復号化する
            //復号化に失敗すると例外CryptographicExceptionが発生
            byte[] decBytes = decryptor.TransformFinalBlock(strBytes, 0, strBytes.Length);
            //閉じる
            decryptor.Dispose();

            //バイト型配列を文字列に戻して返す
            return System.Text.Encoding.UTF8.GetString(decBytes);
        }

        /// <summary>
        /// パスワードから共有キーと初期化ベクタを生成する
        /// </summary>
        /// <param name="password">基になるパスワード</param>
        /// <param name="keySize">共有キーのサイズ（ビット）</param>
        /// <param name="key">作成された共有キー</param>
        /// <param name="blockSize">初期化ベクタのサイズ（ビット）</param>
        /// <param name="iv">作成された初期化ベクタ</param>
        private static void GenerateKeyFromPassword(string password,
            int keySize, out byte[] key, int blockSize, out byte[] iv)
        {
            //パスワードから共有キーと初期化ベクタを作成する
            //saltを決める
            byte[] salt = System.Text.Encoding.UTF8.GetBytes(SALT);
            //Rfc2898DeriveBytesオブジェクトを作成する
            System.Security.Cryptography.Rfc2898DeriveBytes deriveBytes =
                new System.Security.Cryptography.Rfc2898DeriveBytes(password, salt);
            //.NET Framework 1.1以下の時は、PasswordDeriveBytesを使用する
            //System.Security.Cryptography.PasswordDeriveBytes deriveBytes =
            //    new System.Security.Cryptography.PasswordDeriveBytes(password, salt);
            //反復処理回数を指定する デフォルトで1000回
            deriveBytes.IterationCount = 1000;

            //共有キーと初期化ベクタを生成する
            key = deriveBytes.GetBytes(keySize / 8);
            iv = deriveBytes.GetBytes(blockSize / 8);
        }
    }
}
