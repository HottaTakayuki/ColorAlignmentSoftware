using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Threading;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting.Channels;
using System.Reflection;

using AlphaCameraController;
//using AlphaCameraControl;
using CameraDataClass;
using CAS;using System.Security.Cryptography;

namespace AlphaCameraController
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Windows;

    /// <summary>
    /// タスクトレイ通知アイコン
    /// </summary>
    public partial class NotifyIconWrapper : Component
    {
        //#region

        //// 暗号化・復号化IV/Key
        //private const string AES_IV = @"H6fwdH9FW03cG3L2";
        //private const string AES_Key = @"hI6dOeG82H2Dvgq9";

        //#endregion 

        #region Fields

        DispatcherTimer timer;

        private static string appliPath;

        private Type ccTypeInfo; // CameraControl Class
        private dynamic cc;

        // プロセス間通信用
        //private IpcClientChannel Channel;
        //private IpcObject IpcObj;

        private readonly string[] str_focusmode = new string[] { "MF", "AF_S", "close_up", "AF_C", "AF_A", "DMF", "MF_R", "AF_D", "PF" };

        #endregion Fields

        /// <summary>
        /// NotifyIconWrapper クラス を生成、初期化します。
        /// </summary>
        public NotifyIconWrapper()
        {
            InitializeComponent();

#if DEBUG
            //toolStripMenuItem_Test.Visible = true;
#endif

            // コンテキストメニューのイベントを設定
            this.toolStripMenuItem_Exit.Click += this.toolStripMenuItem_Exit_Click;
            //this.toolStripMenuItem_Test.Click += this.toolStripMenuItem_Test_Click;

            System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
            System.Version ver = asm.GetName().Version;

            // Applicationのパスを取得する
            appliPath = System.IO.Path.GetDirectoryName(asm.Location);

            // 設定値Load
            AccSettings.LoadFromXmlFile(appliPath + "\\AccSettings.xml");

            // カメラ制御
            // DLL Load
            try
            {
                var asmDll = Assembly.LoadFrom(appliPath + "\\CameraControl.dll");
                ccTypeInfo = asmDll.GetType("CAS.CameraControl");
                cc = Activator.CreateInstance(ccTypeInfo);

                cc.OpenCamera(AccSettings.Ins.Common.CameraName);
                cc.SetFocusMode(str_focusmode[0]); // 0 = "MF", 1 = "AF_S"
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Alpha Camera Controller Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // タイマーの設定
            timer = new DispatcherTimer(DispatcherPriority.Normal);
            // modified by Hotta 2022/06/06 for カメラ位置           
            //timer.Interval = new TimeSpan(0, 0, 1);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
        }

        public NotifyIconWrapper(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
        }

        #region Events

        /// <summary>
        /// コンテキストメニュー "終了" を選択したとき呼ばれます。
        /// </summary>
        /// <param name="sender">呼び出し元オブジェクト</param>
        /// <param name="e">イベントデータ</param>
        private void toolStripMenuItem_Exit_Click(object sender, EventArgs e)
        {
            // 設定値Save
            //Settings.SaveToXmlFile();

            // カメラ制御の終了
            cc.CloseCamera();

            // 現在のアプリケーションを終了
            Application.Current.Shutdown();
        }

        private void toolStripMenuItem_Test_Click(object sender, EventArgs e)
        {
            //ShootCondition condition = new ShootCondition();

            //condition.ImageSize = 1;
            //condition.FNumber = "F22";
            //condition.Shutter = "0.5";
            //condition.ISO = "200";
            //condition.WB = "6500K";
            //condition.CompressionType = 3;

            //cc.SetCameraSettings(condition);

            //string path = @"C:\CAS\Temp\Test";
            //cc.CaptureImage(path);

            // 暗号化テスト
            //CameraControl.LoadFromXmlFile(AccSettings.Ins.CameraControlFile, out CameraControl control);
            //CameraControlData.LoadFromEncryptFile(AccSettings.Ins.CameraControlFile, out CameraControlData control);

            //CameraControlData.SaveToEncryptFile(AccSettings.Ins.CameraControlFile, control);
        }

        // added by Hotta 2022/06/07 for ライブビュー
        ShootCondition m_LastCondition = new ShootCondition();
        //
        private void timer_Tick(object sender, EventArgs e)
        {
            bool status;

            timer.Stop();

            try
            {
                // 終了
                if (CheckCloseFlag() == true)
                {
                    // カメラ制御の終了
                    cc.CloseCamera();

                    // 現在のアプリケーションを終了
                    Application.Current.Shutdown();
                }

                // 撮影
                if (CheckShootFlag() == true)
                {
                    GetCondition(out string path, out ShootCondition condition);

                    // 設定
                    //setCameraSettingsDll(condition);
                    cc.SetCameraSettings(condition);

                    // 撮影
                    //captureCameraImageDll(path);
                    cc.CaptureImage(path);

                    // 制御ファイル削除
                    try
                    {
                        SetShootFlag(false);
                    }
                    catch { } // 無視

                    //Thread.Sleep(AccSettings.Ins.Common.CameraWait);
                }
                // added by Hotta 2021/12/03
                else if (CheckAutoFocusFlag() == true)
                {
                    GetCondition(out string path, out ShootCondition condition);

                    // 設定
                    //setCameraSettingsDll(condition);
                    cc.SetCameraSettings(condition);

                    // added by Hotta 2022/05/18 for AFエリア
                    GetAfAreaSetting(out AfAreaSetting afArea);

                    // AFエリア設定
                    status = cc.SetAfArea(afArea);
                    //

                    // 撮影
                    //captureCameraImageDll(path);
                    status = cc.AutoFocus();
                    // deleted by Hotta 2022/03/03
                    /*
                    if (status != true)
                    {
                        MessageBox.Show("Fail to Auto-Focus.");
                    }
                    */
                    // 撮影
                    cc.CaptureImage(path);
                    //

                    // 制御ファイル削除
                    try
                    {
                        SetAutoFocusFlag(false);
                    }
                    catch { } // 無視

                    //Thread.Sleep(AccSettings.Ins.Common.CameraWait);
                }
                // added by Hotta 2022/06/06 for ライブビュー
                else if (CheckLiveViewFlag() == 1)
                {
                    GetCondition(out string path, out ShootCondition condition);

                    // 設定
                    if (condition.Equals(m_LastCondition) != true)
                    {
                        cc.SetCameraSettings(condition);
                        m_LastCondition = new ShootCondition(condition);
                    }

                    // ライブビュー撮影
                    cc.LiveView(path);

                    // 制御ファイル削除
                    try
                    {
                        SetLiveViewFlag(0);
                    }
                    catch { } // 無視
                }
                else if (CheckLiveViewFlag() == 2)
                {
                    GetCondition(out string path, out ShootCondition condition);

                    // 設定
                    if (condition.Equals(m_LastCondition) != true)
                    {
                        cc.SetCameraSettings(condition);
                        m_LastCondition = new ShootCondition(condition);
                    }

                    // ライブビュー撮影
                    cc.LiveView(path);
                    /*
                    // 制御ファイル削除
                    try
                    {
                        SetLiveViewFlag(CameraControlData.LiveViewMode.Off);
                    }
                    catch { } // 無視
                    */
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Alpha Camera Controller Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }

            timer.Start();
        }

        #endregion Events

        #region Private Methods

        private bool CheckCloseFlag()
        {
            bool flag = false;

            if (System.IO.File.Exists(AccSettings.Ins.CameraControlFile) == true)
            {
                //CameraControlData.LoadFromEncryptFile(AccSettings.Ins.CameraControlFile, out CameraControlData control, CalcKey(AES_Key), CalcKey(AES_IV));
                CameraControlData.LoadFromXmlFile(AccSettings.Ins.CameraControlFile, out CameraControlData control);
                flag = control.CloseFlag;
            }

            return flag;
        }

        private bool CheckShootFlag()
        {
            bool flag = false;

            if (System.IO.File.Exists(AccSettings.Ins.CameraControlFile) == true)
            {
                //CameraControlData.LoadFromEncryptFile(AccSettings.Ins.CameraControlFile, out CameraControlData control, CalcKey(AES_Key), CalcKey(AES_IV));
                CameraControlData.LoadFromXmlFile(AccSettings.Ins.CameraControlFile, out CameraControlData control);
                flag = control.ShootFlag;
            }

            return flag;
        }

        // added by Hotta 2021/12/03
        private bool CheckAutoFocusFlag()
        {
            bool flag = false;

            if (System.IO.File.Exists(AccSettings.Ins.CameraControlFile) == true)
            {
                //CameraControlData.LoadFromEncryptFile(AccSettings.Ins.CameraControlFile, out CameraControlData control, CalcKey(AES_Key), CalcKey(AES_IV));
                CameraControlData.LoadFromXmlFile(AccSettings.Ins.CameraControlFile, out CameraControlData control);
                flag = control.AutoFocusFlag;
            }

            return flag;
        }
        //

        // added by Hotta 2022/06/06 for ライブビュー
        /// <summary>
        /// 0 : Off, 1 : Single, 2 : Continuous
        /// </summary>
        /// <returns></returns>
        private int CheckLiveViewFlag()
        {
            int flag = 0;

            if (System.IO.File.Exists(AccSettings.Ins.CameraControlFile) == true)
            {
                //CameraControlData.LoadFromEncryptFile(AccSettings.Ins.CameraControlFile, out CameraControlData control, CalcKey(AES_Key), CalcKey(AES_IV));
                CameraControlData.LoadFromXmlFile(AccSettings.Ins.CameraControlFile, out CameraControlData control);
                flag = control.LiveViewFlag;
            }

            return flag;
        }
        //

        private void GetCondition(out string path, out ShootCondition condition)
        {
            condition = new ShootCondition();
            path = "";

            if (System.IO.File.Exists(AccSettings.Ins.CameraControlFile) == true)
            {
                //CameraControlData.LoadFromEncryptFile(AccSettings.Ins.CameraControlFile, out CameraControlData control, CalcKey(AES_Key), CalcKey(AES_IV));
                CameraControlData.LoadFromXmlFile(AccSettings.Ins.CameraControlFile, out CameraControlData control);
                condition = control.Condition;
                path = control.ImgPath;
            }
        }

        // added by Hotta 2022/05/18 for AFエリア
        private void GetAfAreaSetting(out AfAreaSetting afArea)
        {
            afArea = new AfAreaSetting();

            if (System.IO.File.Exists(AccSettings.Ins.CameraControlFile) == true)
            {
                //CameraControlData.LoadFromEncryptFile(AccSettings.Ins.CameraControlFile, out CameraControlData control, CalcKey(AES_Key), CalcKey(AES_IV));
                CameraControlData.LoadFromXmlFile(AccSettings.Ins.CameraControlFile, out CameraControlData control);
                afArea = control.AfArea;
            }
        }

        private void SetShootFlag(bool flag)
        {
            if (System.IO.File.Exists(AccSettings.Ins.CameraControlFile) == true)
            {
                //CameraControlData.LoadFromEncryptFile(AccSettings.Ins.CameraControlFile, out CameraControlData control, CalcKey(AES_Key), CalcKey(AES_IV));
                CameraControlData.LoadFromXmlFile(AccSettings.Ins.CameraControlFile, out CameraControlData control);
                control.ShootFlag = flag;

                //CameraControlData.SaveToEncryptFile(AccSettings.Ins.CameraControlFile, control, CalcKey(AES_Key), CalcKey(AES_IV));
                CameraControlData.SaveToXmlFile(AccSettings.Ins.CameraControlFile, control);
            }
        }

        // added by Hotta 2021/12/03
        private void SetAutoFocusFlag(bool flag)
        {
            if (System.IO.File.Exists(AccSettings.Ins.CameraControlFile) == true)
            {
                //CameraControlData.LoadFromEncryptFile(AccSettings.Ins.CameraControlFile, out CameraControlData control, CalcKey(AES_Key), CalcKey(AES_IV));
                CameraControlData.LoadFromXmlFile(AccSettings.Ins.CameraControlFile, out CameraControlData control);
                control.AutoFocusFlag = flag;

                //CameraControlData.SaveToEncryptFile(AccSettings.Ins.CameraControlFile, control, CalcKey(AES_Key), CalcKey(AES_IV));
                CameraControlData.SaveToXmlFile(AccSettings.Ins.CameraControlFile, control);
            }
        }

        // added by Hotta 2022/06/06 for ライブビュー
        /// <summary>
        /// 0 : Off, 1 : Single, 2 : Continuous
        /// </summary>
        /// <param name="flag"></param>
        private void SetLiveViewFlag(int flag)
        {
            if (System.IO.File.Exists(AccSettings.Ins.CameraControlFile) == true)
            {
                //CameraControlData.LoadFromEncryptFile(AccSettings.Ins.CameraControlFile, out CameraControlData control, CalcKey(AES_Key), CalcKey(AES_IV));
                CameraControlData.LoadFromXmlFile(AccSettings.Ins.CameraControlFile, out CameraControlData control);
                control.LiveViewFlag = flag;

                //CameraControlData.SaveToEncryptFile(AccSettings.Ins.CameraControlFile, control, CalcKey(AES_Key), CalcKey(AES_IV));
                CameraControlData.SaveToXmlFile(AccSettings.Ins.CameraControlFile, control);
            }
        }

        private static string CalcKey(string key)
        {
            string newKey = "";

            newKey = new string(key.Substring(8, 8).Reverse().ToArray()) + key.Substring(0, 8).Replace(key.Substring(3, 1), "@");

            return newKey;
        }

        #endregion Private Methods
    }
}
