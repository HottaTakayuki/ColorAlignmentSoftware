using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using Bitmap = System.Drawing.Bitmap;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;

using CameraControllerSharp;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.Blob;
using CameraDataClass;

namespace CAS
{
    public class CameraControl
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject); // gdi32.dllのDeleteObjectメソッドの使用を宣言する。

        public bool IsCameraOpened = false;
        public string appliPath;

        private CCameraController m_Camera;        

        public unsafe void OpenCamera(string name)
        {
            bool status;
            List<string> lstDevices = new List<string>();

            m_Camera = new CCameraController();

            // 接続名の一覧を取得
            int num = m_Camera.EnumerateDevices();
            if (num == 0)
            {
                Exception ex = new Exception("Failed to get the number of camera connections.");
                throw ex;
            }

            char[] charArray = new char[100];
            for (int n = 0; n < num; n++)
            {
                fixed (char* p = &charArray[0])
                {
                    status = m_Camera.GetDeviceName((uint)n, p, (uint)charArray.Length);
                    if (status != true)
                    {
                        Exception ex = new Exception("Failed to get the camera name.");
                        throw ex;
                    }

                    lstDevices.Add(new string(p));
                }
            }

            // 指定されているデバイス名が何番目か走査
            int index = 0;
            for (; index < num; index++)
            {
                if (lstDevices[index] == name)
                { break; }
            }

            if (index >= num)
            {
                Exception ex = new Exception("The specified camera (" + name + ") was not found. ");
                throw ex;
            }

            // 接続
            status = m_Camera.ConnectDevice((uint)index); // 少なくとも、MANUALモードでは成功。AUTOモードでは失敗。
            if (status != true)
            {
                // modified by Hotta 2022/03/09
                /*
                Exception ex = new Exception("Camera Connection failed.");
                throw ex;
                */
                Thread.Sleep(1000);
                status = m_Camera.ConnectDevice((uint)index);
                if(status != true)
                {
                    Exception ex = new Exception("Failed to connect with the camera.");
                    throw ex;
                }
            }

            IsCameraOpened = true;
        }

        public unsafe void CloseCamera()
        {
            if(IsCameraOpened == true)
            {
                bool status = m_Camera.DisconnectDevice();
                if (status != true)
                { throw new Exception("Failed to disconnect the camera."); }

                IsCameraOpened = false;
            }
        }

        public unsafe void CaptureImage(string path)
        {
#if NO_CAMERA
            return;
#endif
            bool status;
            byte[] image_data;
            uint image_data_size;

            IntPtr ptr = IntPtr.Zero;

            status = m_Camera.GetImage((byte**)&ptr, &image_data_size, true);
            if (status != true)
            {
                // 再撮影
                Thread.Sleep(2000);
                status = m_Camera.GetImage((byte**)&ptr, &image_data_size, true);
                if (status != true)
                {
                    Exception ex = new Exception("Shooting failed.");
                    throw ex;
                }
            }

            image_data = new byte[image_data_size];

            // マネージ配列へコピー
            Marshal.Copy(ptr, image_data, 0, (int)image_data_size);

            // アンマネージ配列のメモリを解放
            Marshal.FreeCoTaskMem(ptr);

            // 画像の保存
            saveImage(image_data, path);
        }

        // added by Hotta 2022/03/08
        string[] getFnumberOrder(string current, string target)
        {
            string[] str_fnumber = new string[]
            {
                "F2.8", "F3.2", "F3.5", "F4.0", "F4.5", "F5.0", "F5.6", "F6.3", "F7.1", "F8.0", "F9.0", "F10", "F11", "F13", "F14", "F16", "F18", "F20", "F22"
            };

            if (current == target)
                return null;

            int start_index = -1;
            for(int n=0;n<str_fnumber.Length;n++)
            {
                if(current == str_fnumber[n])
                {
                    start_index = n;
                    break;
                }
            }
            if(start_index == -1)
            {
                Exception ex = new Exception(string.Format("The current F-number is wrong. [{0}]", current));
                throw ex;
            }

            int end_index = -1;
            for (int n = 0; n < str_fnumber.Length; n++)
            {
                if (target == str_fnumber[n])
                {
                    end_index = n;
                    break;
                }
            }
            if (end_index == -1)
            {
                Exception ex = new Exception(string.Format("The target F-number is wrong. [{0}]", target));
                throw ex;
            }

            string[] order;
            int index;
            if(end_index > start_index)
            {
                order = new string[end_index - start_index];
                index = 0;
                for(int n=start_index+1;n<=end_index;n++)
                {
                    order[index++] = str_fnumber[n];
                }
            }
            else
            {
                order = new string[start_index - end_index];
                index = 0;
                for (int n = start_index - 1; n >= end_index; n--)
                {
                    order[index++] = str_fnumber[n];
                }
            }
            return order;
        }

        string[] getShutterOrder(string current, string target)
        {
            string[] str_shutter = new string[]
            {
                "1/8000", "1/6400", "1/5000", "1/4000", "1/3200", "1/2500", "1/2000", "1/1600", "1/1250", "1/1000", "1/800", "1/640", "1/500", "1/400", "1/320", "1/250", "1/200", "1/160", "1/125",
                "1/100", "1/80", "1/60", "1/50", "1/40", "1/30", "1/25", "1/20", "1/15", "1/13", "1/10", "1/8", "1/6", "1/5", "1/4", "1/3", "0.4\"", "0.5\"", "0.6\"", "0.8\"", "1\"", "1.3\"",
                "1.6\"", "2\"", "2.5\"", "3.2\"", "4\"", "5\"", "6\"", "8\"", "10\"", "13\"", "15\"", "20\"", "25\"", "30\"", "BULB"
            };

            if (current == target)
                return null;

            int start_index = -1;
            for (int n = 0; n < str_shutter.Length; n++)
            {
                if (current == str_shutter[n])
                {
                    start_index = n;
                    break;
                }
            }
            if (start_index == -1)
            {
                Exception ex = new Exception(string.Format("The current Shutter speed is wrong. [{0}]", current));
                throw ex;
            }

            int end_index = -1;
            for (int n = 0; n < str_shutter.Length; n++)
            {
                if (target == str_shutter[n])
                {
                    end_index = n;
                    break;
                }
            }
            if (end_index == -1)
            {
                Exception ex = new Exception(string.Format("The target Shutter speed is wrong. [{0}]", target));
                throw ex;
            }

            string[] order;
            int index;
            if (end_index > start_index)
            {
                order = new string[end_index - start_index];
                index = 0;
                for (int n = start_index + 1; n <= end_index; n++)
                {
                    order[index++] = str_shutter[n];
                }
            }
            else
            {
                order = new string[start_index - end_index];
                index = 0;
                for (int n = start_index - 1; n >= end_index; n--)
                {
                    order[index++] = str_shutter[n];
                }
            }
            return order;
        }

        string[] getISOOrder(string current, string target)
        {
            string[] str_ISO = new string[]
            {
                "AUTO", "50", "64", "80", "100", "125", "160", "200", "250", "320", "400", "500", "640", "800", "1000", "1250", "1600", "2000", "2500", "3200", "4000", "5000", "6400", "8000",
                "10000", "12800", "16000", "20000", "25600", "32000", "40000", "51200", "64000", "80000", "102400"
            };

            if (current == target)
                return null;

            int start_index = -1;
            for (int n = 0; n < str_ISO.Length; n++)
            {
                if (current == str_ISO[n])
                {
                    start_index = n;
                    break;
                }
            }
            if (start_index == -1)
            {
                Exception ex = new Exception(string.Format("The current ISO sensitivity is wrong. [{0}]", current));
                throw ex;
            }

            int end_index = -1;
            for (int n = 0; n < str_ISO.Length; n++)
            {
                if (target == str_ISO[n])
                {
                    end_index = n;
                    break;
                }
            }
            if (end_index == -1)
            {
                Exception ex = new Exception(string.Format("The target ISO sensitivity is wrong. [{0}]", target));
                throw ex;
            }

            string[] order;
            int index;
            if (end_index > start_index)
            {
                order = new string[end_index - start_index];
                index = 0;
                for (int n = start_index + 1; n <= end_index; n++)
                {
                    order[index++] = str_ISO[n];
                }
            }
            else
            {
                order = new string[start_index - end_index];
                index = 0;
                for (int n = start_index - 1; n >= end_index; n--)
                {
                    order[index++] = str_ISO[n];
                }
            }
            return order;
        }
        //


        public unsafe bool SetCameraSettings(ShootCondition condition)
        {
            bool status;
            int aryLen = 100;
            int captureInterval = 10;

            // modified by Hotta 2022/03/09
            // 本来の画面サイズと圧縮形式は、最後に移動
            /*
            // 画面サイズ
            status = m_Camera.SetImageSize((ImageSizeValue)(condition.ImageSize));
            if (status != true)
            {
                Exception ex = new Exception("Failed to set the image size.");
                throw ex;
            }

            // 圧縮形式
            status = m_Camera.SetCompressionSetting((CompressionSetting)condition.CompressionType);
            if (status != true)
            {
                Exception ex = new Exception("Failed to set the compression format.");
                throw ex;
            }
            */

            // 画面サイズ（ダミー用）
            status = m_Camera.SetImageSize((ImageSizeValue)2);
            if (status != true)
            {
                Exception ex = new Exception("Failed to set the image size.");
                throw ex;
            }

            // 圧縮形式（ダミー用）
            status = m_Camera.SetCompressionSetting((CompressionSetting)2);
            if (status != true)
            {
                Exception ex = new Exception("Failed to set the image compression format.");
                throw ex;
            }


            // 絞り(F No.)
            // modified by Hotta 2022/03/08
            /*
            byte[] array = Encoding.ASCII.GetBytes(condition.FNumber);
            fixed (byte* p = &array[0])
            { status = m_Camera.SetFNumber((sbyte*)p); }
            if (status != true)
            {
                Exception ex = new Exception("Failed to set the aperture.");
                throw ex;
            }
            */
            byte[] array = new byte[aryLen];
            fixed (byte* pCur = &array[0])
            {
                status = m_Camera.GetFNumber((sbyte*)pCur, (uint)aryLen);
            }
            if (status != true)
            {
                Exception ex = new Exception("Failed to get the F-number.");
                throw ex;
            }
            int len = 0;
            for (int n = 0; n < aryLen; n++)
            {
                if (array[n] == 0)
                    break;
                len++;
            }
            string[] order = getFnumberOrder(System.Text.Encoding.ASCII.GetString(array, 0, len), condition.FNumber);

            if(order != null)
            {
                for (int n = 0; n < order.Length; n++)
                {
                    array = Encoding.ASCII.GetBytes(order[n]);
                    fixed (byte* p = &array[0])
                    {
                        status = m_Camera.SetFNumber((sbyte*)p);
                    }
                    if (status != true)
                    {
                        Exception ex = new Exception("Failed to set the F-number.");
                        throw ex;
                    }
                    if(n % captureInterval == 0)
                    {
                        uint image_data_size;

                        IntPtr ptr = IntPtr.Zero;
                        status = m_Camera.GetImage((byte**)&ptr, &image_data_size, true);
                        if (status != true)
                        {
                            Exception ex = new Exception("Shooting failed.");
                            throw ex;
                        }
                        Thread.Sleep(100);
                    }
                }
            }


            // シャッタースピード
            // modified by Hotta 2022/03/08
            /*
            array = Encoding.ASCII.GetBytes(condition.Shutter);
            fixed (byte* p = &array[0])
            { status = m_Camera.SetShutterSpeed((sbyte*)p); }
            if (status != true)
            {
                Exception ex = new Exception("Failed to set the shutter speed.");
                throw ex;
            }
            */
            array = new byte[aryLen];
            fixed (byte* pCur = &array[0])
            {
                status = m_Camera.GetShutterSpeed((sbyte*)pCur, (uint)aryLen);
            }
            if (status != true)
            {
                Exception ex = new Exception("Failed to get the Shutter speed.");
                throw ex;
            }
            len = 0;
            for (int n = 0; n < aryLen; n++)
            {
                if (array[n] == 0)
                    break;
                len++;
            }
            //order = getShutterOrder(System.Text.Encoding.ASCII.GetString(array, 0, len), condition.Shutter);
            string cur_fnumber = System.Text.Encoding.ASCII.GetString(array, 0, len);
            if (cur_fnumber.Contains("/") != true && cur_fnumber.Contains("\"") != true)
                cur_fnumber += "\"";
            string tar_fnumber = condition.Shutter;
            if (tar_fnumber.Contains("/") != true && tar_fnumber.Contains("\"") != true)
                tar_fnumber += "\"";

            order = getShutterOrder(cur_fnumber, tar_fnumber);

            if (order != null)
            {
                for (int n = 0; n < order.Length; n++)
                {
                    array = Encoding.ASCII.GetBytes(order[n]);
                    if (array[array.Length - 1] == '"')
                        array[array.Length - 1] = 0;
                    fixed (byte* p = &array[0])
                    {
                        status = m_Camera.SetShutterSpeed((sbyte*)p);
                    }
                    if (status != true)
                    {
                        Exception ex = new Exception("Failed to set the Shutter speed.");
                        throw ex;
                    }
                    if (n % captureInterval == 0)
                    {
                        uint image_data_size;

                        IntPtr ptr = IntPtr.Zero;
                        status = m_Camera.GetImage((byte**)&ptr, &image_data_size, true);
                        if (status != true)
                        {
                            Exception ex = new Exception("Shooting failed.");
                            throw ex;
                        }
                        Thread.Sleep(100);
                    }
                }
            }

            // ISO感度
            array = Encoding.ASCII.GetBytes(condition.ISO);
            fixed (byte* p = &array[0])
            { status = m_Camera.SetISO((sbyte*)p); }
            if (status != true)
            {
                Exception ex = new Exception("Failed to set the ISO sensitivity.");
                throw ex;
            }

            // ホワイトバランス
            array = Encoding.ASCII.GetBytes(condition.WB);
            fixed (byte* p = &array[0])
            { status = m_Camera.SetWhiteBalance((sbyte*)p); }
            if (status != true)
            {
                Exception ex = new Exception("Failed to set the white balance.");
                throw ex;
            }

            // 画面サイズ
            status = m_Camera.SetImageSize((ImageSizeValue)(condition.ImageSize));
            if (status != true)
            {
                Exception ex = new Exception("Failed to set the image size.");
                throw ex;
            }

            // 圧縮形式
            status = m_Camera.SetCompressionSetting((CompressionSetting)condition.CompressionType);
            if (status != true)
            {
                Exception ex = new Exception("Failed to set the image compression format.");
                throw ex;
            }


            return true;
        }

        public unsafe bool SetFocusMode(string mode)
        {
            bool status;

            byte[] array = Encoding.ASCII.GetBytes(mode);
            fixed (byte* p = &array[0])
            {
                status = m_Camera.SetFocusMode((sbyte*)p);
            }
            if (status != true)
            { throw new Exception("Failed to set the focus mode."); }

            return true;
        }

        public unsafe System.Windows.Media.ImageSource GetLiveViewImage(int thresh, int area, out MarkerPosition marker)
        {
            bool status;
            byte[] image_data;
            uint image_data_size;
            IntPtr ptr = IntPtr.Zero;

            marker = new MarkerPosition();

            try
            { status = m_Camera.GetLiveImage((byte**)&ptr, &image_data_size); }
            catch
            { return null; }
            if (status != true)
            {
                //MessageBox.Show("ライブ画像の取得に失敗しました。");
                return null;
            }

            image_data = new byte[image_data_size];
            // マネージ配列へコピー
            Marshal.Copy(ptr, image_data, 0, (int)image_data_size);

            // アンマネージ配列のメモリを解放
            Marshal.FreeCoTaskMem(ptr);

            int offset = (image_data[3] << 24) + (image_data[2] << 16) + (image_data[1] << 8) + image_data[0];
            int size = (image_data[7] << 24) + (image_data[6] << 16) + (image_data[5] << 8) + image_data[4];

            /* データを格納したバッファからMemoryStream生成 */
            MemoryStream ms = new MemoryStream(image_data, offset, size);

            /* ストリームからBitmap生成 */
            Bitmap bmp = new Bitmap(ms);

            ms.Close();

            List<CvBlob> lstBlob;

            searchMaker(bmp, thresh, area, out lstBlob);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                foreach (CvBlob blob in lstBlob)
                {
                    System.Drawing.Rectangle rect = new System.Drawing.Rectangle(blob.MinX, blob.MinY, blob.MaxX - blob.MinX, blob.MaxY - blob.MinY);

                    // 外枠
                    g.DrawRectangle(Pens.Yellow, rect);

                    // 文字列
                    String drawString = "(" + blob.Centroid.X.ToString("0.00") + ", " + blob.Centroid.Y.ToString("0.00") + ")";

                    Font drawFont = new Font("Arial", 12);
                    SolidBrush drawBrush = new SolidBrush(System.Drawing.Color.Yellow);

                    float x = blob.MaxX;
                    float y = blob.MaxY;

                    StringFormat drawFormat = new StringFormat();
                    //drawFormat.FormatFlags = StringFormatFlags.;

                    g.DrawString(drawString, drawFont, drawBrush, x, y, drawFormat);
                }
            }
            double top, bottom, right, left;
            //MarkerPostion marker;
            calcDistance(lstBlob, bmp, out top, out bottom, out right, out left, out marker);

            IntPtr hbitmap = bmp.GetHbitmap();
            System.Windows.Media.ImageSource source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hbitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            DeleteObject(hbitmap);

            return source;
        }

        public unsafe System.Windows.Media.ImageSource GetLiveViewImage(int thresh, out List<CvBlob> lstMarkers)
        {
            bool status;
            byte[] image_data;
            uint image_data_size;
            IntPtr ptr = IntPtr.Zero;

            lstMarkers = new List<CvBlob>();

            try
            { status = m_Camera.GetLiveImage((byte**)&ptr, &image_data_size); }
            catch
            { return null; }
            if (status != true)
            {
                //MessageBox.Show("ライブ画像の取得に失敗しました。");
                return null;
            }

            image_data = new byte[image_data_size];
            // マネージ配列へコピー
            Marshal.Copy(ptr, image_data, 0, (int)image_data_size);

            // アンマネージ配列のメモリを解放
            Marshal.FreeCoTaskMem(ptr);

            int offset = (image_data[3] << 24) + (image_data[2] << 16) + (image_data[1] << 8) + image_data[0];
            int size = (image_data[7] << 24) + (image_data[6] << 16) + (image_data[5] << 8) + image_data[4];

            /* データを格納したバッファからMemoryStream生成 */
            MemoryStream ms = new MemoryStream(image_data, offset, size);

            /* ストリームからBitmap生成 */
            Bitmap bmp = new Bitmap(ms);

            ms.Close();

            List<CvBlob> lstBlob;

            searchMaker(bmp, thresh, 0, out lstBlob);

            lstMarkers = lstBlob;

            using (Graphics g = Graphics.FromImage(bmp))
            {
                foreach (CvBlob blob in lstBlob)
                {
                    System.Drawing.Rectangle rect = new System.Drawing.Rectangle(blob.MinX, blob.MinY, blob.MaxX - blob.MinX, blob.MaxY - blob.MinY);

                    // 外枠
                    g.DrawRectangle(Pens.Yellow, rect);

                    // 文字列
                    String drawString = "(" + blob.Centroid.X.ToString("0.00") + ", " + blob.Centroid.Y.ToString("0.00") + ")";

                    Font drawFont = new Font("Arial", 12);
                    SolidBrush drawBrush = new SolidBrush(System.Drawing.Color.Yellow);

                    float x = blob.MaxX;
                    float y = blob.MaxY;

                    StringFormat drawFormat = new StringFormat();
                    //drawFormat.FormatFlags = StringFormatFlags.;

                    g.DrawString(drawString, drawFont, drawBrush, x, y, drawFormat);
                }
            }

            //double top, bottom, right, left;
            //MarkerPostion marker;
            //calcDistance(lstBlob, bmp, out top, out bottom, out right, out left, out marker);

            IntPtr hbitmap = bmp.GetHbitmap();
            System.Windows.Media.ImageSource source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hbitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            DeleteObject(hbitmap);

            return source;
        }


        // added by Hotta 2022/06/06 for ライブビュー
        unsafe public void LiveView(string savePath)
        {
            bool status;
            byte[] image_data;
            uint image_data_size;
            IntPtr ptr = IntPtr.Zero;

            try
            {
                status = m_Camera.GetLiveImage((byte**)&ptr, &image_data_size);
            }
            catch
            {
                return;
            }
            if (status != true)
            {
                return;
            }
            try
            {
                image_data = new byte[image_data_size];
                // マネージ配列へコピー
                Marshal.Copy(ptr, image_data, 0, (int)image_data_size);

                // アンマネージ配列のメモリを解放
                Marshal.FreeCoTaskMem(ptr);

                int offset = (image_data[3] << 24) + (image_data[2] << 16) + (image_data[1] << 8) + image_data[0];
                int size = (image_data[7] << 24) + (image_data[6] << 16) + (image_data[5] << 8) + image_data[4];

                /*
                // データを格納したバッファからMemoryStream生成
                MemoryStream ms = new MemoryStream(image_data, offset, size);

                // ストリームからBitmap生成
                Bitmap bmp = new Bitmap(ms);
                ms.Close();
                */

                using (System.IO.FileStream fs = new System.IO.FileStream(savePath + ".jpg", System.IO.FileMode.Create, System.IO.FileAccess.Write, FileShare.None))
                {
                    fs.Write(image_data, offset, size);
                }
                //fs.Close();
            }
            catch //(Exception ex)
            {
                ;
            }
        }

        // modified by Hotta 2022/03/03
        /*
        // added by Hotta 2021/12/03
        public bool AutoFocus()
        {
            bool status;

            status = SetFocusMode("AF_S");
            Thread.Sleep(1000);
            if (status == true)
            {
                status = m_Camera.AutoFocusSingle();
            }
            Thread.Sleep(1000);
            if (status == true)
            {
                status = SetFocusMode("MF");
            }
            return status;
        }
        */
        public bool AutoFocus()
        {
            bool status;

            status = SetFocusMode("AF_S");
            if(status != true)
            {
                throw new Exception("Failed to set the focus mode to AF_S.");
            }
            Thread.Sleep(1000);

            for(int retry=0;retry<10;retry++)
            {
                status = m_Camera.AutoFocusSingle();
                if (status == true)
                    break;
                Thread.Sleep(1000);
            }
            if (status != true)
            {
                throw new Exception("Failed to execute AF_S.");
            }
            Thread.Sleep(1000);

            status = SetFocusMode("MF");
            if (status != true)
            {
                throw new Exception("Failed to set the focus mode to MF.");
            }
            Thread.Sleep(1000);

            return status;
        }

        //

        // added by Hotta 2022/05/17 for AFエリア
        public unsafe bool SetAfArea(AfAreaSetting afArea)
        {
            bool status;

            // フォーカスエリアの設定
            // Wide, Zone, Center, FlexibleSpotS, FlexibleSpotM, FlexibleSpotL
            if (afArea.focusAreaType != "Wide" && afArea.focusAreaType != "Zone" && afArea.focusAreaType != "Center" &&
                afArea.focusAreaType != "FlexibleSpotS" && afArea.focusAreaType != "FlexibleSpotM" && afArea.focusAreaType != "FlexibleSpotL")
            {
                throw new Exception("The target AF area type is wrong.");
            }

            byte[] array = Encoding.ASCII.GetBytes(afArea.focusAreaType);
            fixed (byte* p = &array[0])
            {
                status = m_Camera.SetFocusArea((sbyte*)p);
            }
            if (status != true)
            {
                throw new Exception("Failed to set the AF area type.");
            }

            // フォーカスエリアがフレキシブルでなければ、ここで終了
            if (afArea.focusAreaType != "FlexibleSpotS" && afArea.focusAreaType != "FlexibleSpotM" && afArea.focusAreaType != "FlexibleSpotL")
                return status;

            // AFエリアポジションの設定
            // フォーカスエリアごとの最小・最大値
            ushort[] xMin = new ushort[] { 65, 79, 94 };
            ushort[] yMin = new ushort[] { 53, 63, 73 };
            ushort[] xMax = new ushort[] { 574, 560, 545 };
            ushort[] yMax = new ushort[] { 374, 364, 354 };

            int index;
            if (afArea.focusAreaType == "FlexibleSpotS")
                index = 0;
            else if (afArea.focusAreaType == "FlexibleSpotM")
                index = 1;
            else if (afArea.focusAreaType == "FlexibleSpotL")
                index = 2;
            else
                throw new Exception("The target AF area type is wrong.");

            if (afArea.focusAreaX < xMin[index] || afArea.focusAreaY < yMin[index] || afArea.focusAreaX > xMax[index] || afArea.focusAreaY > yMax[index])
            {
                throw new Exception(string.Format("The target AF area is out of settable range. x:{0}-{1}, y:{2}-{3}", xMin[index], xMax[index], yMin[index], yMax[index]));
            }

            status = m_Camera.SetAfAreaPosition(afArea.focusAreaX, afArea.focusAreaY);
            if (status != true)
            {
                throw new Exception("Failed to set the AF area.");
            }

            return status;
        }

        //


        #region Private Methods

        private unsafe bool saveImage(byte[] image_data, string path)
        {
            uint com;

            bool status = m_Camera.GetCompressionSetting(&com);
            if (status != true)
            {
                Exception ex = new Exception("Failed to get the image compression format.");
                throw ex;
            }

            // 0x01:ECO, 0x02:STD, 0x03:FINE, 0x04:XFINE, 0x10:RAW, 0x13:RAW_JPG, 0x20:RAWC, 0x23:RAWC_JPG
            string ext = "";
            if (com == 0x02 || com == 0x03 || com == 0x04)
            { ext = "jpg"; }
            else if (com == 0x10)
            { ext = "arw"; }
            else
            {
                Exception ex = new Exception("The image compression format is wrong.");
                throw ex;
            }

            using (BinaryWriter bw = new BinaryWriter(new FileStream(path + "." + ext, FileMode.Create)))
            { bw.Write(image_data); }

            return true;
        }

        private void searchMaker(Bitmap bmp, int thresh, int area, out List<CvBlob> lstBlob)
        {
            lstBlob = new List<CvBlob>();

            using (Mat src = BitmapConverter.ToMat(bmp))
            using (Mat gray = src.CvtColor(ColorConversionCodes.BGR2GRAY))
            using (Mat binary = gray.Threshold(thresh, 255, ThresholdTypes.Binary))
            using (Mat dilate = binary.Clone())
            {
                Cv2.Dilate(binary, dilate, null);

                CvBlobs blobs = new CvBlobs(dilate);

                if (area != 0)
                { blobs.FilterByArea((int)(area * 0.8), (int)(area * 1.2)); }

                foreach (KeyValuePair<int, CvBlob> keyValue in blobs)
                {
                    double ratio = (double)keyValue.Value.Rect.Height / (double)keyValue.Value.Rect.Width;

                    if (ratio > 0.5 && ratio < 1.5) // 正方形に近いのものみ選出
                    { lstBlob.Add(keyValue.Value); }
                }

                src.Dispose();
                gray.Dispose();
                binary.Dispose();
                dilate.Dispose();
            }
        }

        private void calcDistance(List<CvBlob> lstBlob, Bitmap bmp, out double top, out double bottom, out double right, out double left, out MarkerPosition marker)
        {
            CvBlob topLeft = null, topRight = null, bottomLeft = null, bottomRight = null;
            marker = new MarkerPosition();

            top = 0;
            bottom = 0;
            right = 0;
            left = 0;

            if (lstBlob.Count != 4)
            { return; }

            double dist_tl = double.MaxValue, dist_tr = double.MaxValue, dist_bl = double.MaxValue, dist_br = double.MaxValue; // 各コーナーからの距離の最小値        

            foreach (CvBlob blob in lstBlob)
            {
                double dist;

                // 左上からの距離
                dist = Math.Sqrt(Math.Pow(blob.Centroid.X, 2) + Math.Pow(blob.Centroid.Y, 2));
                if (dist < dist_tl)
                {
                    topLeft = blob;
                    dist_tl = dist;
                }

                // 右上からの距離
                dist = Math.Sqrt(Math.Pow(bmp.Width - blob.Centroid.X, 2) + Math.Pow(blob.Centroid.Y, 2));
                if (dist < dist_tr)
                {
                    topRight = blob;
                    dist_tr = dist;
                }

                // 左下からの距離
                dist = Math.Sqrt(Math.Pow(blob.Centroid.X, 2) + Math.Pow(bmp.Height - blob.Centroid.Y, 2));
                if (dist < dist_bl)
                {
                    bottomLeft = blob;
                    dist_bl = dist;
                }

                // 右下からの距離
                dist = Math.Sqrt(Math.Pow(bmp.Width - blob.Centroid.X, 2) + Math.Pow(bmp.Height - blob.Centroid.Y, 2));
                if (dist < dist_br)
                {
                    bottomRight = blob;
                    dist_br = dist;
                }
            }

            // 基準点間の距離を算出
            if (topLeft != null && topRight != null && bottomLeft != null && bottomRight != null)
            {
                top = Math.Sqrt(Math.Pow(topRight.Centroid.X - topLeft.Centroid.X, 2) + Math.Pow(topRight.Centroid.Y - topLeft.Centroid.Y, 2));
                bottom = Math.Sqrt(Math.Pow(bottomRight.Centroid.X - bottomLeft.Centroid.X, 2) + Math.Pow(bottomRight.Centroid.Y - bottomLeft.Centroid.Y, 2));
                left = Math.Sqrt(Math.Pow(bottomLeft.Centroid.X - topLeft.Centroid.X, 2) + Math.Pow(bottomLeft.Centroid.Y - topLeft.Centroid.Y, 2));
                right = Math.Sqrt(Math.Pow(bottomRight.Centroid.X - topRight.Centroid.X, 2) + Math.Pow(bottomRight.Centroid.Y - topRight.Centroid.Y, 2));

                marker.TopLeft = new Coordinate(topLeft.Centroid.X, topLeft.Centroid.Y);
                marker.TopRight = new Coordinate(topRight.Centroid.X, topRight.Centroid.Y);
                marker.BottomLeft = new Coordinate(bottomLeft.Centroid.X, bottomLeft.Centroid.Y);
                marker.BottomRight = new Coordinate(bottomRight.Centroid.X, bottomRight.Centroid.Y);
            }
        }

        #endregion Private Methods
    }
}
