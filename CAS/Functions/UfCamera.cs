// added by Hotta 2024/08/28
#define ForCrosstalkCameraUF
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using System.IO;
using System.Diagnostics;
using Microsoft.VisualBasic.FileIO;
using System.Runtime.InteropServices;
using Bitmap = System.Drawing.Bitmap;
using System.Drawing;
using System.Threading;
using System.Reflection;
using System.Windows.Forms;
using Size = OpenCvSharp.Size;
using Brush = System.Windows.Media.Brush;

using OpenCvSharp;
using OpenCvSharp.Blob;
using AcquisitionARW;
using CameraDataClass;
using MakeUFData;
using CheckBox = System.Windows.Controls.CheckBox;

namespace CAS
{
    public partial class MainWindow : System.Windows.Window
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject); // gdi32.dllのDeleteObjectメソッドの使用を宣言する。

        #region Constants

        // カメラ名称
        // カメラテスト
        //private readonly string[] CamNames = new string[] { "ILCE-6400", "ILCE-7RM3" };
        private readonly string[] CamNames = new string[] { "ILCE-6400", "ILCE-7RM3" };
        // modified by Hotta 2025/07/04 for LED-8511
        /*
        private readonly string[] LensNames = new string[] { "E PZ 16-50mm F3.5-5.6 OSS", "FE 24-70mm F2.8 GM" };// SELP1650 / SEL2470GM ARWファイルに記載のレンズ名
        */
        private readonly string[] LensNames = new string[] { "E PZ 16-50mm F3.5-5.6 OSS", "FE 24-70mm F2.8 GM", "E PZ 16-50mm F3.5-5.6 OSS II" };// SELP1650 / SEL2470GM / SELP16502 ARWファイルに記載のレンズ名
        //
        private readonly string[] LensModels = new string[] { "SELP1650", "SEL2470GM" }; // レンズ型式名

        // カメラの撮影画像サイズ
        // α7R3
        private readonly Size PictSize_7R3 = new Size(7952, 5304);

        // α6400
        private readonly Size PictSize_6400 = new Size(6000, 4000);

        private const string OpenCvInstDir = @"C:\CAS_OSS\"; 
        public const string CameraControlFile = "CamCont.bin";

        private const string CamCorrectDir = "CameraCorrection\\";

        // ファイル名称指定
        private const string fn_AreaFile = "MeasArea.arw";
        private const string fn_MaskFile = "Mask.jpg";
        private const string fn_RefBlackFile = "Black.jpg"; // 映り込み確認用
        private const string fn_MeasureArea = "MeasureArea";
        private const string fn_LensDist = "LensDistortion";        

        private const string fn_CameraCorrectionData = "Ccd_";
        private const string fn_LedCorrectionData = "Lcd_";

        private const string fn_FlatWhite = "White";
        private const string fn_FlatRed = "Red";
        private const string fn_FlatGreen = "Green";
        private const string fn_FlatBlue = "Blue";        

        private const string fn_Reference = "Ref";
        private const string fn_RefMaskTop = "_MaskTop";
        private const string fn_RefMaskBottom = "_MaskBottom";
        private const string fn_RefMaskLeft = "_MaskLeft";
        private const string fn_RefMaskRight = "_MaskRight";
        private const string fn_MeasureAreaMod = "Module_";

        private const string fn_MaskRef = "MaskRef.jpg";
        private const string fn_RefArea = "RefArea.arw";
        private const string fn_MaskTrim = "MaskTrim";
        private const string fn_BlackDiff = "BlackDiff.jpg";

        private readonly string[] str_focusmode = new string[] { "MF", "AF_S", "close_up", "AF_C", "AF_A", "DMF", "MF_R", "AF_D", "PF" };

        //// Offset, Step, Max, BitMask, Shift
        //private readonly MATPARAM[] matParam = new MATPARAM[9]{
        //    new MATPARAM(   0.09375, 0.00390625 , 1.08984375 , 0xFF, 56),
        //    new MATPARAM(  -0.15625, 0.004882813, 0.463867188, 0x7F, 49),
        //    new MATPARAM(  -0.1875 , 0.002929688, 0.184570313, 0x7F, 42),
        //    new MATPARAM(  -0.15625, 0.004882813, 0.151367188, 0x3F, 36),
        //    new MATPARAM(   0.09375, 0.00390625 , 1.08984375 , 0xFF, 28),
        //    new MATPARAM(  -0.1875 , 0.002929688, 0.184570313, 0x7F, 21),
        //    new MATPARAM(  -0.15625, 0.004882813, 0.151367188, 0x3F, 15),
        //    new MATPARAM(  -0.1875 , 0.002929688, 0.184570313, 0x7F,  8),
        //    new MATPARAM(   0.09375, 0.00390625 , 1.08984375 , 0xFF,  0)
        //};

        // DLL動的参照
        //private readonly string ccDllPath = "\\CameraControl.dll";

        // LiveView-撮影画像比
        //private const double ViewRatioH = 7.765625;
        //private const double ViewRatioV = 7.8;

        private const int ModuleAreaMin = 200;
        private const int ModuleAreaMax = 500000;

        public const int UfMeasSplitCount = 9; // UF測定時のModule分割数 3*3

        private const int TrimmingAreaMin = 2500; // 50 * 50(適当)
        private const int TrimmingAreaMax = 90000; // 300 * 300(適当)

        private const int ModuleCountX = 4; // Cabinet内でModuleがX方向にいくつあるか
        //private const int ModuleCountY = 2; // Cabinett内でModuleがY方向にいくつあるか

        // 位置合わせ用
        private const double RealPitch_P12 = 1.26; // 実際のLEDピッチ長さ[mm]
        private const double RealPitch_P15 = 1.58;

        //private const double TransRate_7R3 = 0.5952380952380952380952380952381; // 画角から算出
        //private const double TransRate_6400 = 0.76923076923076923076923076923077;

        // cmos→mm変換係数(理論値より計算)
        private const double TransRateH_7R3 = 0.75235885537509667440061871616396;
        private const double TransRateV_7R3 = 0.75413450937155457552370452039691;
        private const double TransRateH_6400 = 0.97906602254428341384863123993559;
        private const double TransRateV_6400 = 0.97906602254428341384863123993559;

        private const double CapDistance_P12 = 4000; //[mm]、撮影距離、4[m]
        private const double CapDistance_P15 = 5000; //[mm]、撮影距離、5[m]

        private const double WallPixelSizeH_7R3 = 6465; // 4k2kWallの撮影範囲(カメラ画素数)
        private const double WallPixelSizeV_7R3 = 3628;
        private const double WallPixelSizeH_6400 = 4968;
        private const double WallPixelSizeV_6400 = 2808;

        //private const double CabinetSizeH = 608; // [mm]、Cabinetのサイズ
        //private const double CabinetSizeV = 342; // [mm]

        // Measure描画時の協調表示
        //private const double ViewGain = 5;//2.0;

        private const string LensCdAveNameKey = "Default";

        //private const int RefrectionMinSize = 9; // 映り込み最小サイズ

        // 暗号化・復号化IV/Key
        //private const string AES_IV = @"H6fwdH9FW03cG3L2";
        //private const string AES_Key = @"hI6dOeG82H2Dvgq9";

        private const int SaturationSpec = 8000; // サチっていると判断する画素値閾値

        private const double ShootingDistNearLimit_P12 = 3600; // 撮影距離限界(P1.2)[mm] 近い方
        private const double ShootingDistFarLimit_P12 = 8800; // 撮影距離限界(P1.2)[mm] 遠い方 計算上は8K4K+1Cabi(17x17Cabi)で8500mm(+6.25％)でよいが若干の余裕を見る
        private const double ShootingDistNearLimit_P15 = 4500; // 撮影距離限界(P1.5)[mm] 近い方
        private const double ShootingDistFarLimit_P15 = 11000; // 撮影距離限界(P1.5)[mm] 遠い方
        private const double PanLimit = 50.0; // 調整限界角度(Pan)[deg] これを越えると校正テーブルがない&補正値バラつきが大きくなるため調整できない
        private const double TiltLimit = 30.0; // 調整限界角度(Tilt)[deg]

        private const int IntSigLvMax = 1023;

        #endregion Constants

        #region Fields

        private bool IsCameraOpened = false;
        //private bool capCancel = false;
        private CameraCorrectionData ccd; // カメラ特性データ
        private LedCorrectionData lcd; // Led配光特性データ
        //private CabinetPosition cabiRefPos; // 基準位置

        private UfCamMeasLog ufCamMeasLog;
        private UfCamAdjLog ufCamAdjLog;

        private string CamContFile;

        // カメラ撮影画像サイズ
        private Size PictSize;

        // カメラ位置合せの基準位置、選択エリアの4隅のマーカー座標の目標位置
        private MarkerPosition tgtCamPos;
        // added by Hotta 2022/06/07 for カメラ位置
        private MarkerPosition tgtCamPos_canUse;
        // modified by Hotta 2022/11/14 for 外部入力されたCabinet配置
        /*
        float[] tgtCamPos_HorLineSpec;
        float[] tgtCamPos_VerLineSpec;
        */
        float[][] tgtCamPos_HorLineSpec;
        float[][] tgtCamPos_VerLineSpec;

        private UfAdjAlgorithm ufCamAdjAlgo = UfAdjAlgorithm.CommonColor;

        // cmos→mm変換係数
        private double SetPosTransRateH;
        private double SetPosTransRateV;

        // カメラ位置合せ前のMasterコントローラの設定、設定の書き戻しが非同期のTimer内で行っているのでClass変数とする
        private UserSetting userSetting;

        //private double CapDistance;

        //private double WallPixelSizeH;
        //private double WallPixelSizeV;

        // 調整前のユーザー設定
        //List<UserSetting> lstCamSetPosUserSettings;

        // added by Hotta 2024/09/09 for crosstalk
#if ForCrosstalkCameraUF
        public class CrosstalkInfo
        {
            public int ControllerID;
            public int X;
            public int Y;
            public float[][] Crosstalk;

            public CrosstalkInfo()
            {
                ControllerID = X = Y = -1;
            }
        }
        static List<CrosstalkInfo> m_lstCrosstalkInfo = new List<CrosstalkInfo>();
        public static List<CrosstalkInfo> ListCrosstalkInfo
        {
            get { return m_lstCrosstalkInfo; }
            set { m_lstCrosstalkInfo = value; }
        }

        public int ReferenceOneModuleNo = -1;

#endif


        #endregion Fields

        #region Events

        #region Camera Settings

        private void cmbxUfCamCamera_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                cmbxGapCamCamera.SelectedIndex = cmbxUfCamCamera.SelectedIndex;

                // カメラ選択
                Settings.Ins.Camera.Name = CamNames[cmbxUfCamCamera.SelectedIndex];

                ShowLensCdFiles(cmbxUfCamCamera.SelectedIndex);
            }
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                return;
            }
        }

        private void cmbxUfCamLensCd_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            { cmbxGapCamLensCd.SelectedIndex = cmbxUfCamLensCd.SelectedIndex; }
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                return;
            }
        }

        private void btnUfCamConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                actionButton(sender, "Connect Camera.");

                // Tempフォルダ作成
                if (Directory.Exists(tempPath) == false)
                { Directory.CreateDirectory(tempPath); }

                DisconnectCamera();
                ConnectCamera();

                // UI有効化
                // UF Camera
                cmbxUfCamCamera.IsEnabled = false;
                cmbxUfCamLensCd.IsEnabled = false;
                btnUfCamConnect.IsEnabled = false;
                btnUfCamDisconnect.IsEnabled = true;
                gdUfCamSetPos.IsEnabled = true;

                // Gap Camera
                cmbxGapCamCamera.IsEnabled = false;
                cmbxGapCamLensCd.IsEnabled = false;
                btnGapCamConnect.IsEnabled = false;
                btnGapCamDisconnect.IsEnabled = true;
                gdGapCamSetPos.IsEnabled = true;

                if (appliMode == ApplicationMode.Developer)
                {
                    btnUfCamMeasStart.IsEnabled = true;
                    //btnUfCamSample.IsEnabled = true;
                    btnUfCamAdjustStart.IsEnabled = true;
                    btnGapCamMeasStart.IsEnabled = true;
                    btnGapCamAdjStart.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                return;
            }
        }

        private void btnUfCamDisconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                actionButton(sender, "Disconnect Camera.");

                // Measurementが開始されていたら終了する
                // 同期処理かつCancellationTokenはないので、フラグを落として自動終了させる
                tbtnUfCamSetPos.IsChecked = false;
                tbtnGapCamSetPos.IsChecked = false;

                DisconnectCamera();

                // UI無効化
                // UF(Cam)
                //cmbxUfCamCamera.IsEnabled = true;
                //cmbxUfCamLensCd.IsEnabled = true;
                btnUfCamConnect.IsEnabled = true;
                btnUfCamDisconnect.IsEnabled = false;
                gdUfCamSetPos.IsEnabled = false;
                btnUfCamMeasStart.IsEnabled = false;
                btnUfCamAdjustStart.IsEnabled = false;

                // GAP(Cam)
                //cmbxGapCamCamera.IsEnabled = true;
                //cmbxGapCamLensCd.IsEnabled = true;
                btnGapCamConnect.IsEnabled = true;
                btnGapCamDisconnect.IsEnabled = false;
                gdGapCamSetPos.IsEnabled = false;
                btnGapCamMeasStart.IsEnabled = false;
                btnGapCamAdjStart.IsEnabled = false;
            }
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                return;
            }
        }

        #endregion Camera Settings

        // added by Tei 2024/12/24
        string m_CamUfMeasPath;

        private async void btnUfCamMeasStart_Click(object sender, RoutedEventArgs e)
        {
            bool status = true;
            bool waitFlag = false;
            List<UnitInfo> lstTgtCabi;

            ufCamMeasLog = new UfCamMeasLog();

            actionButton(sender, "Measurement UF Start.");

            // Unitが選択されているか、矩形になっているか再度確認
            try
            { CheckSelectedUnits(aryUnitUfCam, out lstTgtCabi); }
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);

                // タブの表示ページを切り替え
                tcUfCamView.SelectedIndex = 0;

                releaseButton(sender, "Measurement UF Done.");
                return;
            }

            // 対象Controllerの初期化
            foreach (ControllerInfo controller in dicController.Values)
            { controller.Target = false; }

            foreach (UnitInfo unit in lstTgtCabi)
            { dicController[unit.ControllerID].Target = true; }

            // modified by Hotta 2024/12/26
            /*
            winProgress = new WindowProgress("Measurement UF Progress");
            */
            winProgress = new WindowProgress("Measurement UF Progress", 180, 400, WindowProgress.TAbortType.Measurement);

            winProgress.Show();
            winProgress.ShowMessage("Start Measurement.");

            if (tbtnUfCamSetPos.IsChecked == true)
            {
                timerUfCam.Enabled = false;
                tbtnUfCamSetPos.IsChecked = false;
                tbtnUfCamSetPos_Click(sender, new RoutedEventArgs());
                waitFlag = true;

                // added by Hotta 2023/11/16 for カメラ位置合わせを終了しないまま、本ボタンを押された時、ここでコントローラの画質設定を元に戻す
                try
                {
                    SetThroughMode(false);
                    setUserSettingSetPos(userSetting);
                }
                catch (Exception ex)
                {
                    ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                    // タブの表示ページを切り替え
                    tcUfCamView.SelectedIndex = 0;
                    releaseButton(sender, "Measurement UF Done.");
                    return;
                }
                //
            }

            // added by Hotta 2023/12/07
            if (timerUfCam.Enabled == true)
            {
                timerUfCam.Enabled = false;
                tbtnUfCamSetPos.IsChecked = false;
                tbtnUfCamSetPos_Click(sender, new RoutedEventArgs());
                waitFlag = true;
            }
            //

            tcMain.IsEnabled = false;

            // 視聴点調整モード設定
            //bool isViewPtMode = (bool)cbUfCamViewPtMode.IsChecked;

            // Target Only Flag for Debug
            bool tgtOnly = (bool)cbUfCamMeasTgtOnly.IsChecked;

            // 撮影距離を取得
            double dist, wallH = double.NaN, camH = double.NaN;
            dist = double.Parse(txbConfigDist.Text); // 撮影距離は必ず必要なので例外時はそのままエラー

            // 撮影距離の確認
            CheckShootingDist(dist);

            // カメラ-Wall相対位置を取得
            if (rbConfigCamPosCustom.IsChecked == true)
            {
                try { wallH = double.Parse(txbConfigWallHeight.Text); }
                catch { wallH = double.NaN; }
                try { camH = double.Parse(txbConfigCamHeight.Text); }
                catch { camH = double.NaN; }
            }

            ufCamMeasLog.WallCamDistance = dist;
            ufCamMeasLog.WallHeight = wallH;
            ufCamMeasLog.CamHeight = camH;

            // タブの表示ページを切り替え
            tcUfCamView.SelectedIndex = 2;

            if (waitFlag == true)
            { Thread.Sleep(5000); } // 画像の取得が完全に終わるまで待つ

            // フォルダの作成
            string measPath = applicationPath + MeasDir + "UF_" + DateTime.Now.ToString("yyyyMMddHHmm") + "\\";
            
            if (Directory.Exists(measPath) == false)
            { Directory.CreateDirectory(measPath); }

            m_CamUfMeasPath = measPath;

            saveUfLog("Start Measurement UF.");

            string msg = "";

            // added by Hotta 2024/09/30
            m_lstUserSetting = null;
            //

            try { await Task.Run(() => MeasureUfAsync(lstTgtCabi, measPath, new ViewPoint(), dist, wallH, camH, tgtOnly)); } // Measurementでは視聴点調整モードは無効にする。
            // added by Hotta 2025/01/31
            catch (CameraCasUserAbortException ex)
            {
                ShowMessageWindow(ex.Message, "Abort!", System.Drawing.SystemIcons.Information, 500, 210);
                status = false;
                winProgress.Operation = UserOperation.None;
            }
            //
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                status = false;
            }
            finally
            {
                // added by Hotta 2024/09/30
                if (m_lstUserSetting != null)
                {
                    // modified by Hotta 2025/02/05
                    /*
                    SetThroughMode(false);
                    setUserSetting(m_lstUserSetting);
                    */
                    winProgress.ShowMessage("Set Normal Settings.");
                    winProgress.PutForward1Step();
                    DoEvents();
                    saveLog("Set Normal Settings.");
                    SetThroughMode(false);

                    winProgress.ShowMessage("Restore User Settings.");
                    winProgress.PutForward1Step();
                    DoEvents();
                    saveLog("Restore User Settings.");
                    setUserSetting(m_lstUserSetting);
                    //
                }
                //

                // 不要な画像ファイルの削除
                if (appliMode == ApplicationMode.Normal)
                { DeleteUnwantedImagesMeas(measPath); }

                // カメラ校正テーブルの初期化（隠蔽のため）
                if (appliMode != ApplicationMode.Developer)
                {
                    foreach (UfCamMeasValue cv in ufCamMeasLog.lstUfCamMeas)
                    {
                        foreach (UfCamModule mod in cv.aryUfCamModules)
                        {
                            foreach (UfCamMp mp in mod.aryUfCamMp)
                            {
                                mp.CCV = new CameraCorrectionValue();
                                mp.CCV.CcdHash = ccd.SHA256Hash;
                                mp.CCV.LcdHash = lcd.SHA256Hash;
                                mp.LCV = new LedCorrectionValue();
                            }
                        }
                    }
                }

                // 測定結果を保存
                //UfCamMeasLog.SaveToEncryptFile(measPath + "UfMeasResult.bin", ufCamMeasLog, CalcKey(AES_IV), CalcKey(AES_Key)); // 1step
                UfCamMeasLog.SaveToXmlFile(measPath + "UfMeasResult.xml", ufCamMeasLog); // 1step

                // Logの世代管理
                if (appliMode == ApplicationMode.Normal)
                { ManageLogGen(applicationPath + MeasDir, "UF_"); }
            }

            saveUfLog("Finish Measurement UF.");

            msg = "";
            string caption = "";
            if (status == true)
            {
                msg = "Measurement UF Complete!";
                caption = "Complete";
            }
            else
            {
                msg = "Failed in Measurement UF.";
                caption = "Error";
            }

            winProgress.StopRemainTimer();
            winProgress.Close();

            playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
            WindowMessage winMessage = new WindowMessage(msg, caption);
            winMessage.ShowDialog();

            releaseButton(sender, "Measurement UF Done.");
            tcMain.IsEnabled = true;
        }

        private async void btnUfCamAdjustStart_Click(object sender, RoutedEventArgs e)
        {
            bool waitFlag = false;
            UfCamAdjustType type;
            WindowMessage winMessage;
            string msg, caption;
            List<UnitInfo> lstObjCabi; // 調整目標Cabinet
            List<UnitInfo> lstTgtCabi;

            // added by Hotta 2024/01/31
            bool status = true;
            //

            ufCamAdjLog = new UfCamAdjLog();

            actionButton(sender, "UF Camera Adjust Start.");

            // Unitが選択されているか、矩形になっているか再度確認
            try
            { CheckSelectedUnits(aryUnitUfCam, out lstTgtCabi); }
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);

                // タブの表示ページを切り替え
                tcUfCamView.SelectedIndex = 0;

                releaseButton(sender, "Adjustment UF Done.");
                return;
            }

            // 対象Controllerの初期化
            foreach (ControllerInfo controller in dicController.Values)
            { controller.Target = false; }

            foreach (UnitInfo unit in lstTgtCabi)
            { dicController[unit.ControllerID].Target = true; }

            // modified by Hotta 2024/12/26
            /*
            winProgress = new WindowProgress("Adjustment UF Progress");
            */
            winProgress = new WindowProgress("Adjustment UF Progress", 180, 400, WindowProgress.TAbortType.Adjustment);

            winProgress.Show();
            winProgress.ShowMessage("Start Adjustment.");

            if (tbtnUfCamSetPos.IsChecked == true)
            {
                timerUfCam.Enabled = false;
                tbtnUfCamSetPos.IsChecked = false;
                tbtnUfCamSetPos_Click(sender, new RoutedEventArgs());
                waitFlag = true;

                // added by Hotta 2023/11/16 for カメラ位置合わせを終了しないまま、本ボタンを押された時、ここでコントローラの画質設定を元に戻す
                try
                {
                    SetThroughMode(false);
                    setUserSettingSetPos(userSetting);
                }
                catch (Exception ex)
                {
                    ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                    // タブの表示ページを切り替え
                    tcUfCamView.SelectedIndex = 0;
                    releaseButton(sender, "Adjustment UF Done.");
                    return;
                }
                //
            }

            // added by Hotta 2023/12/07
            if (timerUfCam.Enabled == true)
            {
                timerUfCam.Enabled = false;
                tbtnUfCamSetPos.IsChecked = false;
                tbtnUfCamSetPos_Click(sender, new RoutedEventArgs());
                waitFlag = true;
            }
            //

            tcMain.IsEnabled = false;

            if (waitFlag == true)
            { Thread.Sleep(5000); } // 画像の取得が完全に終わるまで待つ

            if (IsCameraOpened == false)
            {
                ShowMessageWindow("Camera is not opened.", "CAS Error!", System.Drawing.SystemIcons.Error, 400, 200);

                releaseButton(sender, "UF Camera Adjust Done.");
                tcMain.IsEnabled = true;
                return;
            }

            // Tempフォルダ作成
            if (Directory.Exists(tempPath) == false)
            { Directory.CreateDirectory(tempPath); }

            // 調整Log保存用のフォルダ作成
            string logDir = applicationPath + LogDir + "CamUF_" + DateTime.Now.ToString("yyyyMMddHHmm") + "\\";
            if (Directory.Exists(logDir) == false)
            { Directory.CreateDirectory(logDir); }

            // added by Tei 2024/12/24 ログ出力対応
            m_CamUfMeasPath = logDir;

            saveUfLog("Start Adjustment UF.");

            int processSec = initialUfCameraAdjustmentProcessSec(lstTgtCabi.Count);
            winProgress.StartRemainTimer(processSec);

            saveUfLog("Inital Settings.");

#if DEBUG
#else
            // deleted by Hotta 2023/12/08
            //
            //// Tempフォルダの中を削除
            //string[] files = Directory.GetFiles(tempPath);
            //foreach (string file in files)
            //{
            //    try { File.Delete(file); }
            //    catch { } // 無視
            //}
            //
#endif

            stopIntSig();
            btnUfCamAdjustStart.IsEnabled = false;

            // 目標色度設定
            setChromTarget();

            //// Relative Target選択時は目標色度を更新
            //if ((rbStandard.IsChecked == true || rbMeasureOnly.IsChecked == true || rbStrict.IsChecked == true)
            //    && cmbxTargetMode.SelectedIndex == 1)
            //{ ufTargetChrom = new ChromCustom(ColorPurpose.Relative); }
            
            try
            {
                // 調整モードを設定
                if (rbUfCamEachMod.IsChecked == true)
                { type = UfCamAdjustType.EachModule; }
                else if (rbUfCamRadiator.IsChecked == true)
                { type = UfCamAdjustType.Radiator; }
                else if (rbUfCam9pt.IsChecked == true)
                { type = UfCamAdjustType.Cabi_9pt; }
                else
                { type = UfCamAdjustType.Cabinet; }

                // 視聴点調整モード設定
                ViewPoint vp = new ViewPoint();
                //bool isViewPtMode = (bool)cbUfCamViewPtMode.IsChecked;
                if (cbUfCamViewPtMode.IsChecked == true)
                { vp.Vertical = true; }
                if (cbUfCamHViewPt.IsChecked == true)
                { vp.Horizontal = true; }

                // 目標Cabinetを設定
                ObjectiveLine objEdge = null;

                if (rbUfCamTgtCabiCustom.IsChecked == true) // Cabinet
                { StoreObjectiveCabinet(cmbxUfCamTgtCabi.Text, out lstObjCabi); }
                else if (rbUfCamTgtCabiLine.IsChecked == true) // Line
                {
                    objEdge = new ObjectiveLine(cbUfCamTgtCabiTop.IsChecked, cbUfCamTgtCabiBottom.IsChecked, cbUfCamTgtCabiLeft.IsChecked, cbUfCamTgtCabiRight.IsChecked);
                    StoreObjectiveCabinet(lstTgtCabi, objEdge, out lstObjCabi); // 意味ないかも
                }
                else
                { StoreObjectiveCabinet(lstTgtCabi, out lstObjCabi); } // 真ん中

                // 基準Cabinetが調整対象Cabinetの中に含まれているか確認
                CheckObjectiveCabinet(lstObjCabi, lstTgtCabi);

                // 撮影距離を取得
                double dist, wallH = double.NaN, camH = double.NaN;
                dist = double.Parse(txbConfigDist.Text); // 撮影距離は必ず必要なので例外時はそのままエラー

                // 撮影距離の確認
                CheckShootingDist(dist);

                // カメラ-Wall相対位置を取得
                if (rbConfigCamPosCustom.IsChecked == true)
                {
                    try { wallH = double.Parse(txbConfigWallHeight.Text); }
                    catch { wallH = double.NaN; }
                    try { camH = double.Parse(txbConfigCamHeight.Text); }
                    catch { camH = double.NaN; }
                }

                ufCamAdjLog.WallCamDistance = dist;
                ufCamAdjLog.WallHeight = wallH;
                ufCamAdjLog.CamHeight = camH;

                // added by Hotta 2024/09/30
                m_lstUserSetting = null;
                //

                // UF調整
                await Task.Run(() => AdjustUfCamAsync(logDir, lstTgtCabi, type, lstObjCabi, objEdge, vp, dist, wallH, camH));

                // 調整後の測定
                if (cbUfCamMeasResult.IsChecked == true)
                {
                    //
                    // 調整後の計測「だけ」なので、ESCキー押下による中断は、あえて有効にしない →　要件定義の結果、有効にする
                    //
                    // added by Hotta 2025/01/24
                    winProgress.AbortType = WindowProgress.TAbortType.Measurement;
                    //

                    ufCamMeasLog = new UfCamMeasLog();

                    // タブの表示ページを切り替え
                    tcUfCamView.SelectedIndex = 2;

                    // フォルダの作成
                    string measPath = applicationPath + MeasDir + "UF_" + DateTime.Now.ToString("yyyyMMddHHmm") + "\\";

                    if (Directory.Exists(measPath) == false)
                    { Directory.CreateDirectory(measPath); }

                    try { await Task.Run(() => MeasureUfAsync(lstTgtCabi, measPath, new ViewPoint(), dist, wallH, camH/*isViewPtMode*/)); } // Measurementでは視聴点調整モードは無効にする。
                    // added by Hotta 2025/01/31
                    catch (CameraCasUserAbortException ex)
                    {
                        ShowMessageWindow(ex.Message, "Abort!", System.Drawing.SystemIcons.Information, 500, 210);
                        status = false;
                        winProgress.Operation = UserOperation.None;
                    }
                    catch (Exception ex)
                    {
                        ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                        // added by Hotta 2025/01/31
                        status = false;
                        //
                    }
                    finally
                    {
                        // 不要な画像ファイルの削除
                        if (appliMode == ApplicationMode.Normal)
                        { DeleteUnwantedImagesMeas(measPath); }

                        // カメラ校正テーブルの初期化（隠蔽のため）
                        if (appliMode != ApplicationMode.Developer)
                        {
                            foreach (UfCamMeasValue cv in ufCamMeasLog.lstUfCamMeas)
                            {
                                foreach (UfCamModule mod in cv.aryUfCamModules)
                                {
                                    foreach (UfCamMp mp in mod.aryUfCamMp)
                                    {
                                        mp.CCV = new CameraCorrectionValue();
                                        mp.CCV.CcdHash = ccd.SHA256Hash;
                                        mp.CCV.LcdHash = lcd.SHA256Hash;
                                        mp.LCV = new LedCorrectionValue();
                                    }
                                }
                            }
                        }

                        // 測定結果を保存
                        //UfCamMeasLog.SaveToEncryptFile(measPath + "UfMeasResult.bin", ufCamMeasLog, CalcKey(AES_IV), CalcKey(AES_Key)); // 1step
                        UfCamMeasLog.SaveToXmlFile(measPath + "UfMeasResult.xml", ufCamMeasLog); // 1step

                        // Logの世代管理
                        if (appliMode == ApplicationMode.Normal)
                        { ManageLogGen(applicationPath + MeasDir, "UF_"); }
                    }
                }

                // moved by Hotta 2025/01/31
                // ここだと、正常完了であっても、エラーや中断であっても、同じメッセージが表示されてしまうので、
                // 結果に応じて表示内容を変えるよう、finally 部分に移動する
                /*
                caption = "Complete";
                msg = "UF Camera Adjustment Complete!";

                winMessage = new WindowMessage(msg, caption);
                winMessage.ShowDialog();
                */
                
                saveUfLog("Finish Adjustment UF.");
            }
            // added by Hotta 2025/01/31
            catch (CameraCasUserAbortException ex)
            {
                ShowMessageWindow(ex.Message, "Abort!", System.Drawing.SystemIcons.Information, 500, 210);
                status = false;
                winProgress.Operation = UserOperation.None;
            }
            //
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                // added by Hotta 2025/01/31
                status = false;
                //
            }
            finally
            {
                // added by Hotta 2024/09/30
                if (m_lstUserSetting != null)
                {
                    // modified by Hotta 2025/02/05
                    /*
                    SetThroughMode(false);
                    setUserSetting(m_lstUserSetting);
                    */
                    winProgress.ShowMessage("Set Normal Settings.");
                    winProgress.PutForward1Step();
                    DoEvents();
                    saveLog("Set Normal Settings.");
                    SetThroughMode(false);

                    winProgress.ShowMessage("Restore User Settings.");
                    winProgress.PutForward1Step();
                    DoEvents();
                    saveLog("Restore User Settings.");
                    setUserSetting(m_lstUserSetting);
                }
                //

                // カメラ校正テーブルの初期化（隠蔽のため）
                if (appliMode != ApplicationMode.Developer)
                {
                    foreach (UfCamCabinetCpInfo info in ufCamAdjLog.lstUnitCpInfo)
                    {
                        foreach (UfCamCorrectionPoint cp in info.lstCp)
                        {
                            cp.CCV = new CameraCorrectionValue();
                            cp.CCV.CcdHash = ccd.SHA256Hash;
                            cp.CCV.LcdHash = lcd.SHA256Hash;
                            cp.LCV = new LedCorrectionValue();
                        }
                    }
                }

                // 測定結果を保存
                //UfCamAdjLog.SaveToEncryptFile(logDir + "UnitCpInfo.bin", ufCamAdjLog, CalcKey(AES_IV), CalcKey(AES_Key));
                UfCamAdjLog.SaveToXmlFile(logDir + "UnitCpInfo.xml", ufCamAdjLog);

                // Logの世代管理
                if (appliMode == ApplicationMode.Normal)
                { ManageLogGen(applicationPath + LogDir, "CamUF_"); }

                winProgress.StopRemainTimer();
                winProgress.Close();


                // added by Hotta 2025/01/31
                // 上のtry部分から移動し、結果に応じてメッセージを変更
                if (status == true)
                {
                    caption = "Complete";
                    msg = "UF Camera Adjustment Complete!";
                }
                else
                {
                    caption = "Error";
                    msg = "Failed in Adjustment UF.";
                }
                winMessage = new WindowMessage(msg, caption);
                winMessage.ShowDialog();
                //

                btnUfCamAdjustStart.IsEnabled = true;
                //capCancel = false;

                playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
                releaseButton(sender, "UF Camera Adjust Done.");

                tcMain.IsEnabled = true;
            }
        }

        private void rbUfCamTgtCabi_Click(object sender, RoutedEventArgs e)
        {
            if (rbUfCamTgtCabiCustom.IsChecked == true)
            { cmbxUfCamTgtCabi.IsEnabled = true; }
            else
            { cmbxUfCamTgtCabi.IsEnabled = false; }
        }

        private void rbUfCamTgtCabiLine_Checked(object sender, RoutedEventArgs e)
        {
            cbUfCamTgtCabiTop.IsEnabled = true;
            cbUfCamTgtCabiBottom.IsEnabled = true;
            cbUfCamTgtCabiLeft.IsEnabled = true;
            cbUfCamTgtCabiRight.IsEnabled = true;
        }

        private void rbUfCamTgtCabiLine_Unchecked(object sender, RoutedEventArgs e)
        {
            cbUfCamTgtCabiTop.IsEnabled = false;
            cbUfCamTgtCabiBottom.IsEnabled = false;
            cbUfCamTgtCabiLeft.IsEnabled = false;
            cbUfCamTgtCabiRight.IsEnabled = false;
        }

        private void cbUfCamTgtCabi_Checked(object sender, RoutedEventArgs e)
        {
            int count = 0;

            if(cbUfCamTgtCabiTop.IsChecked == true)
            { count++; }
            if(cbUfCamTgtCabiBottom.IsChecked == true)
            { count++; }
            if (cbUfCamTgtCabiLeft.IsChecked == true)
            { count++; }
            if (cbUfCamTgtCabiRight.IsChecked == true)
            { count++; }

            if (count > 2)
            {
                string msg = "Up to 2 lines can be selected.";
                ShowMessageWindow(msg, "Caution!", System.Drawing.SystemIcons.Information, 300, 200);
                ((CheckBox)sender).IsChecked = false;
            }

            if((cbUfCamTgtCabiTop.IsChecked == true && cbUfCamTgtCabiBottom.IsChecked == true)
                || (cbUfCamTgtCabiLeft.IsChecked == true && cbUfCamTgtCabiRight.IsChecked == true))
            {
                string msg = "Top and Bottom / Left and Right cannot be selected at the same time.";
                ShowMessageWindow(msg, "Caution!", System.Drawing.SystemIcons.Information, 400, 200);
                ((CheckBox)sender).IsChecked = false;
            }
        }

        private void tbtnUfCamSetPos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                playSound(applicationPath + "\\Components\\Sound\\button70.mp3");

                if (tbtnUfCamSetPos.IsChecked == true)
                {
                    List<UnitInfo> lstTgtUnits;
                    double dist, wallH = double.NaN, camH = double.NaN;

                    txtbStatus.Text = "Setting Camera Position...";
                    doEvents();

                    // マスクの信号レベルを設定
                    m_MeasureLevel = brightness.UF_20pc; // 20IRE = 492

                    dist = double.Parse(txbConfigDist.Text); // 撮影距離は必ず必要なので例外時はそのままエラー
                    
                    // 撮影距離の確認
                    CheckShootingDist(dist);

                    if (rbConfigCamPosCustom.IsChecked == true)
                    {
                        try { wallH = double.Parse(txbConfigWallHeight.Text); }
                        catch { wallH = double.NaN; }
                        try { camH = double.Parse(txbConfigCamHeight.Text); }
                        catch { camH = double.NaN; }
                    }

                    // Unitが選択されているか、矩形になっているか確認
                    try
                    { CheckSelectedUnits(aryUnitUfCam, out lstTgtUnits); }
                    catch (Exception ex)
                    {
                        ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);

                        tbtnUfCamSetPos.IsChecked = false;

                        // タブの表示ページを切り替え
                        tcUfCamView.SelectedIndex = 0;

                        return;
                    }

                    // 対象Controllerの初期化
                    foreach (ControllerInfo controller in dicController.Values)
                    { controller.Target = false; }

                    foreach (UnitInfo unit in lstTgtUnits)
                    { dicController[unit.ControllerID].Target = true; }

                    // マスク用ファイルを削除
                    string maskImgPath = tempPath + "SetPosMask.jpg";
                    if (File.Exists(maskImgPath) == true) // 位置合せ用ファイルはJpeg
                    { File.Delete(maskImgPath); }

                    // ユーザー設定を保存 1Step                  
                    getUserSettingSetPos(out userSetting);

                    // ThroughMode設定 → TempCorrection, LightOutputも設定する仕様に変更
                    // modified by Hotta 2024/09/30
                    /*
                    setAdjustSetting();
                    */
                    setAdjustSettingSetPos();
                    //SetThroughMode(true);

                    // Layout情報Off
                    foreach (ControllerInfo cont in dicController.Values)
                    { sendSdcpCommand(SDCPClass.CmdDispUnitAddrOff, 0, cont.IPAddress); }

                    // AutoFocus
                    ShootCondition codition = Settings.Ins.Camera.SetPosSetting;
                    m_CamMeasPath = tempPath;

                    outputIntSigChecker(0, 0, modDy, modDx, modDx, modDy); // 20IRE
                    Thread.Sleep(Settings.Ins.Camera.PatternWait);
                    AutoFocus(codition, new AfAreaSetting());

                    // 目標位置を設定
                    SetCamPosTarget();

                    // Cabinet位置(空間座標)を設定
                    SetCabinetPos(lstTgtUnits, dist, wallH, camH);

                    // タブの表示ページを切り替え
                    tcUfCamView.SelectedIndex = 1;

                    // Timer Start
                    timerUfCam.Enabled = true;
                }
                else
                {
                    txtbStatus.Text = "Done.";
                }
            }
            catch (Exception ex)
            {
                // ThroughMode設定を解除 1Step
                SetThroughMode(false);

                // ユーザー設定に書き戻し
                setUserSettingSetPos(userSetting);

                // 内部信号OFF
                stopIntSig();

                ((ToggleButton)sender).IsChecked = false;
                ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                return;
            }
        }

        private void timerUfCam_Tick(object sender, EventArgs e)
        {
            try
            {
                AdjustCameraPosUf((System.Windows.Forms.Timer)sender, imgUfCamCameraView, tbtnUfCamSetPos);

            }
            catch (Exception ex)
            {
                // ThroughMode設定を解除 1Step
                SetThroughMode(false);

                // ユーザー設定に書き戻し
                setUserSettingSetPos(userSetting);

                // 内部信号OFF
                stopIntSig();

                timerUfCam.Enabled = false;
                tbtnUfCamSetPos.IsChecked = false;
                tbtnUfCamSetPos_Click(sender, new RoutedEventArgs());

                ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
            }
        }

        #region tcUfCamView(Cabinet Allocation / Camera View / Adjustment/Measurement)

        private void btnUfCamSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            { actionButton(sender, "Select All"); }

            for (int i = 0; i < allocInfo.MaxX; i++)
            {
                for (int j = 0; j < allocInfo.MaxY; j++)
                {
                    if (aryUnitUfCam[i, j].UnitInfo != null)
                    { Dispatcher.Invoke(new Action(() => { aryUnitUfCam[i, j].IsChecked = true; })); }
                }
            }

            if (sender != null)
            {
                System.Threading.Thread.Sleep(600);
                releaseButton(sender);
            }
        }

        private void btnUfCamDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            { actionButton(sender, "Deselect All"); }

            for (int i = 0; i < allocInfo.MaxX; i++)
            {
                for (int j = 0; j < allocInfo.MaxY; j++)
                {
                    if (aryUnitUfCam[i, j].UnitInfo != null)
                    { Dispatcher.Invoke(new Action(() => { aryUnitUfCam[i, j].IsChecked = false; })); }
                }
            }

            if (sender != null)
            {
                System.Threading.Thread.Sleep(600);
                releaseButton(sender);
            }
        }

        private void btnUfCamResultOpen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.InitialDirectory = applicationPath + MeasDir;
                    ofd.FileName = "";
                    ofd.Filter = "XML Files(*.xml)|*.xml|All Files(*.*)|*.*";
                    ofd.FilterIndex = 1;
                    ofd.Title = "Please select UF Mesurement File.";
                    ofd.RestoreDirectory = true;
                    ofd.CheckFileExists = true;
                    ofd.CheckPathExists = true;

                    if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        UfCamMeasLog.LoadFromXmlFile(ofd.FileName, out ufCamMeasLog);
                        //UfCamMeasLog.LoadFromEncryptFile(ofd.FileName, out ufCamMeasLog, CalcKey(AES_IV), CalcKey(AES_Key));

                        dispUfMeasResult();
                    }
                }
            }
            catch
            {
                string msg = "The format of the opened file is incorrect.\r\nOpen the UF Measurement file.";
                ShowMessageWindow(msg, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                return;
            }
        }

        private void utbUfCam_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            rectAreaUfCam.Visibility = System.Windows.Visibility.Visible;
            rectAreaUfCam.Margin = new Thickness(e.GetPosition(this).X - 2, e.GetPosition(this).Y - 48, 0, 0);

            startPos = new System.Windows.Point(rectAreaUfCam.Margin.Left, rectAreaUfCam.Margin.Top);
        }

        private void utbUfCam_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            endPos = new System.Windows.Point(e.GetPosition(this).X - 2, e.GetPosition(this).Y - 48);

            DragArea area = new DragArea();

            if (startPos.X <= endPos.X)
            {
                area.StartX = startPos.X;
                area.EndX = endPos.X;
            }
            else
            {
                area.StartX = endPos.X;
                area.EndX = startPos.X;
            }

            if (startPos.Y <= endPos.Y)
            {
                area.StartY = startPos.Y;
                area.EndY = endPos.Y;
            }
            else
            {
                area.StartY = endPos.Y;
                area.EndY = startPos.Y;
            }

            double offsetX = tcMain.Margin.Left + gdUfCam.Margin.Left + gdTcViewUfCam.Margin.Left + gdUfCamAlloc.Margin.Left + gdUfCamAllocLayout.Margin.Left + cabinetAllocColumnHeaderWidth - svUfCam.HorizontalOffset;
            double offsetY = tcMain.Margin.Top + gdUfCam.Margin.Top + gdTcViewUfCam.Margin.Top + tbiUfCamUnitAlloc.Height + gdUfCamAlloc.Margin.Top + gdUfCamAllocLayout.Margin.Top + cabinetAllocRowHeaderHeight - svUfCam.VerticalOffset;

            for (int y = 0; y < allocInfo.MaxY; y++)
            {
                for (int x = 0; x < allocInfo.MaxX; x++)
                {
                    if (aryUnitUfCam[x, y].IsEnabled == false)
                    { continue; }

                    if (isContainedDragArea(aryUnitUfCam[x, y], area, offsetX, offsetY) == true)
                    {
                        if (aryUnitUfCam[x, y].IsChecked == true)
                        { aryUnitUfCam[x, y].IsChecked = false; }
                        else
                        { aryUnitUfCam[x, y].IsChecked = true; }
                    }
                }
            }

            rectAreaUfCam.Visibility = System.Windows.Visibility.Hidden;
        }

        private void utbUfCam_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            double height = e.GetPosition(this).Y - 48 - startPos.Y;
            try
            {
                if (height >= 0)
                { rectAreaUfCam.Height = height; }
                else
                {
                    rectAreaUfCam.Margin = new Thickness(rectAreaUfCam.Margin.Left, startPos.Y + height, 0, 0);
                    rectAreaUfCam.Height = Math.Abs(height);
                }
            }
            catch { rectAreaUfCam.Height = 0; }

            double width = e.GetPosition(this).X - 2 - startPos.X;
            try
            {
                if (width >= 0)
                { rectAreaUfCam.Width = width; }
                else
                {
                    rectAreaUfCam.Margin = new Thickness(startPos.X + width, rectAreaUfCam.Margin.Top, 0, 0);
                    rectAreaUfCam.Width = Math.Abs(width);
                }
            }
            catch { rectAreaUfCam.Width = 0; }
        }

        #endregion tcUfCamView

        #endregion Events

        #region Private Methods

        #region Common

        private void ExecGC()
        {
            System.GC.Collect(); // アクセス不可能なオブジェクトを除去
            System.GC.WaitForPendingFinalizers(); // ファイナライゼーションが終わるまでスレッド待機
            System.GC.Collect(); // ファイナライズされたばかりのオブジェクトに関連するメモリを開放

            Thread.Sleep(Settings.Ins.Camera.GcWait);
        }

        static int CompareX(Area a, Area b)
        {
            return (int)(a.StartPos.X - b.StartPos.X);
        }

        static int CompareY(Area a, Area b)
        {
            return (int)(a.StartPos.Y - b.StartPos.Y);
        }

        static int CompareX(RectangleArea a, RectangleArea b)
        {
            return (int)(a.TopLeft.X - b.TopLeft.X);
        }

        static int CompareY(RectangleArea a, RectangleArea b)
        {
            return (int)(a.TopLeft.Y - b.TopLeft.Y);
        }

        static int CompareX(Coordinate a, Coordinate b)
        {
            return (int)(a.X - b.X);
        }

        static int CompareY(Coordinate a, Coordinate b)
        {
            return (int)(a.Y - b.Y);
        }

        private void ConnectCamera()
        {
            // added by Hotta 2022/02/14
#if NO_CAP
            IsCameraOpened = true;
#else
            // AlphaCameraController.exeの設定ファイルを書き換え
            UpdateAccSettings();

            // カメラ制御プロセスの起動
            StartCameraController();

            if (ChechCcProcess() == true)
            { IsCameraOpened = true; }
            else
            { throw new Exception("Failed to start the camera control process."); }

            string lensSerial = "";
            Dispatcher.Invoke(() => (lensSerial = cmbxUfCamLensCd.SelectedItem.ToString()));

            // カメラ視野角特性(レンズ)補正データのLoad
            string ccdFile = applicationPath + CompDir + CamCorrectDir + fn_CameraCorrectionData + Settings.Ins.Camera.Name + "_" + lensSerial + ".xml";
            CameraCorrectionData.LoadFromXmlFile(ccdFile, out ccd);

            // SHA256ハッシュの格納
            ccd.SHA256Hash = CalcSha256Hash(ccdFile);
#endif

            // added by Hotta 2022/02/14
#if NO_CONTROLLER
#else
            // LED配光特性補正データのLoad とりあえず適当なLED校正テーブルをLoadする
            string[] ledFiles = Directory.GetFiles(applicationPath + CompDir + CamCorrectDir, fn_LedCorrectionData + "*");
            if (ledFiles.Length <= 0)
            { throw new Exception("The LED Calibration Data was not found."); }
            LedCorrectionData.LoadFromXmlFile(ledFiles[0], out lcd);
            
            // SHA256ハッシュの格納
            lcd.SHA256Hash = CalcSha256Hash(ledFiles[0]);

            // カメラのISO・目地計測信号レベルの設定
            if (allocInfo.LEDModel == ZRD_C12A 
                || allocInfo.LEDModel == ZRD_C15A 
                || allocInfo.LEDModel == ZRD_CH12D 
                || allocInfo.LEDModel == ZRD_CH15D
                || allocInfo.LEDModel == ZRD_CH12D_S3
                || allocInfo.LEDModel == ZRD_CH15D_S3)
            {
#if (DEBUG || Release_log)
                if (Settings.Ins.ExecLog == true)
                {
                    MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                }
#endif
                Settings.Ins.Camera.SetPosSetting.ISO = "200";
                Settings.Ins.Camera.TrimAreaSetting.ISO = "200";
                Settings.Ins.Camera.MeasAreaSetting.ISO = "200";

                m_MeasureLevel = Settings.Ins.GapCam.MeasLevel_CModel;
            }
            else
            {
#if (DEBUG || Release_log)
                if (Settings.Ins.ExecLog == true)
                {
                    MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                }
#endif
                Settings.Ins.Camera.SetPosSetting.ISO = "100";
                Settings.Ins.Camera.TrimAreaSetting.ISO = "100";
                Settings.Ins.Camera.MeasAreaSetting.ISO = "100";

                m_MeasureLevel = Settings.Ins.GapCam.MeasLevel_BModel;
            }

            // 基準位置ファイルのロード
            //string file = applicationPath + CompDir + CamCorrectDir + fn_PanelReferencePos + Settings.Ins.Camera.Name + ".xml";
            //CabinetPosition.LoadFromXmlFile(file, out cabiRefPos);
#endif

            // 撮影画像サイズ
            if (Settings.Ins.Camera.Name == CamNames[0])
            {
                // 6400
                PictSize = new Size(PictSize_6400.Width, PictSize_6400.Height);

                // 位置合せの[mm]変換係数
                SetPosTransRateH = TransRateH_6400;
                SetPosTransRateV = TransRateV_6400;

                // 4KWallの画像上のサイズ
                //WallPixelSizeH = WallPixelSizeH_6400;
                //WallPixelSizeV = WallPixelSizeV_6400;
            }
            else if (Settings.Ins.Camera.Name == CamNames[1])
            {
                // 7R3
                PictSize = new Size(PictSize_7R3.Width, PictSize_7R3.Height);

                // 位置合せの[mm]変換係数
                SetPosTransRateH = TransRateH_7R3;
                SetPosTransRateV = TransRateV_7R3;

                // 4KWallの画像上のサイズ
                //WallPixelSizeH = WallPixelSizeH_7R3;
                //WallPixelSizeV = WallPixelSizeV_7R3;
            }

            // マスク用ファイルを削除
            string maskImgPath = tempPath + "SetPosMask.jpg";
            if (File.Exists(maskImgPath) == true) // 位置合せ用ファイルはJpeg
            { File.Delete(maskImgPath); }
        }

        private void DisconnectCamera()
        {
            // Camera切断
            CloseCamera();

            // Camera制御ProcessのKill
            KillCcProcess();

            IsCameraOpened = false;
        }

        private void StartCameraController()
        {
            if (ChechCcProcess() == false)
            {
                //ProcessStartInfoオブジェクトを作成する
                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
                //起動する実行ファイルのパスを設定する
                psi.FileName = applicationPath + "\\Components\\AlphaCameraController.exe";
                psi.UseShellExecute = false;
                //起動する
                System.Diagnostics.Process p = System.Diagnostics.Process.Start(psi);
            }
        }

        private bool ChechCcProcess()
        {
            bool running = false;

            System.Diagnostics.Process[] ps = Process.GetProcessesByName("AlphaCameraController");

            if(ps != null && ps.Length > 0)
            { running = true; }

            return running;
        }

        private void KillCcProcess()
        {
            System.Diagnostics.Process[] ps = System.Diagnostics.Process.GetProcesses();

            foreach (System.Diagnostics.Process p in ps)
            {
                if (p.ProcessName == "AlphaCameraController")
                {
                    p.Kill();
                }
            }
        }

        private void CaptureImage(string imgPath)
        {
            // added by Hotta 2024/12/26
            CheckUserAbort();
            //

            CameraControlData cont;

            // カメラ制御プロセスの起動を確認
            StartCameraController();

            try
            {
                // 古いファイルをLoad（前の設定を引き継ぐ)
                if (File.Exists(CamContFile) == true)
                {
                    //CameraControlData.LoadFromEncryptFile(CamContFile, out cont, CalcKey(AES_Key), CalcKey(AES_IV));
                    CameraControlData.LoadFromXmlFile(CamContFile, out cont);
                }
                else
                { cont = new CameraControlData(); }
            }
            catch { return; } // CamCont.xmlへのアクセスが拒否された場合は諦める

            // modified by Hotta 2022/02/21
            // Wait4Capture()で、arw/jpgの区別していないので、とにかく削除する
            /*
            // added by Hotta 2022/02/01
            if (cont.Condition.CompressionType == 16 && File.Exists(imgPath + ".arw") == true)
            {
                File.Delete(imgPath + ".arw");
            }
            else if (cont.Condition.CompressionType == 3 && File.Exists(imgPath + ".jpg") == true)
            {
                File.Delete(imgPath + ".jpg");
            }
            */
            if (File.Exists(imgPath + ".arw") == true)
            {
                File.Delete(imgPath + ".arw");
            }
            if (File.Exists(imgPath + ".jpg") == true)
            {
                File.Delete(imgPath + ".jpg");
            }
            //

            cont.ImgPath = imgPath;
            cont.ShootFlag = true;
            // added by Hotta 2022/06/16 for ライブビュー
            cont.LiveViewFlag = 0;
            //
            try
            {
                //CameraControlData.SaveToEncryptFile(CamContFile, cont, CalcKey(AES_Key), CalcKey(AES_IV));
                CameraControlData.SaveToXmlFile(CamContFile, cont);
            }
            catch { return; }

            // 撮影が完了するまで待機
            try { Wait4Capturing(imgPath); }
            catch
            {
                // Retry
                DisconnectCamera();
                ConnectCamera();
                //CameraControlData.SaveToEncryptFile(CamContFile, cont, CalcKey(AES_Key), CalcKey(AES_IV));
                CameraControlData.SaveToXmlFile(CamContFile, cont);
                Wait4Capturing(imgPath);
            }

            playSound(applicationPath + "\\Components\\Sound\\Camera-Phone03-1.mp3");

            Thread.Sleep(Settings.Ins.Camera.CameraWait);
        }

        private void CaptureImage(string imgPath, ShootCondition condition)
        {
            // added by Hotta 2024/12/26
            CheckUserAbort();
            //

            // modified by Hotta 2022/02/21
            // Wait4Capture()で、arw/jpgの区別していないので、とにかく削除する
            /*
            // added by Hotta 2022/02/01
            if (condition.CompressionType == 16 && File.Exists(imgPath + ".arw") == true)
            {
                File.Delete(imgPath + ".arw");
            }
            else if (condition.CompressionType == 3 && File.Exists(imgPath + ".jpg") == true)
            {
                File.Delete(imgPath + ".jpg");
            }
            */
            if (File.Exists(imgPath + ".arw") == true)
            {
                File.Delete(imgPath + ".arw");
            }
            if (File.Exists(imgPath + ".jpg") == true)
            {
                File.Delete(imgPath + ".jpg");
            }
            //

            // カメラ制御プロセスの起動を確認
            StartCameraController();

            CameraControlData cont = new CameraControlData();
            cont.Condition = condition;
            cont.ImgPath = imgPath;
            cont.ShootFlag = true;
            // added by Hotta 2022/06/06 for ライブビュー
            cont.LiveViewFlag = 0;
            //

            try
            {
                //CameraControlData.SaveToEncryptFile(CamContFile, cont, CalcKey(AES_Key), CalcKey(AES_IV));
                CameraControlData.SaveToXmlFile(CamContFile, cont);
            }
            catch
            { return; }

            // 撮影が完了するまで待機
            try { Wait4Capturing(imgPath); }
            catch
            {
                // Retry
                DisconnectCamera();
                ConnectCamera();
                //CameraControlData.SaveToEncryptFile(CamContFile, cont, CalcKey(AES_Key), CalcKey(AES_IV));
                CameraControlData.SaveToXmlFile(CamContFile, cont);
                Wait4Capturing(imgPath);
            }

            playSound(applicationPath + "\\Components\\Sound\\Camera-Phone03-1.mp3");

            Thread.Sleep(Settings.Ins.Camera.CameraWait);
        }

        // modified by Hotta 2022/05/18 for AFエリア
        /*
        // added by Hotta 2021/12/03
        private bool AutoFocus(ShootCondition condition)
        */
        private bool AutoFocus(ShootCondition condition, AfAreaSetting afArea = null)
        {
            // added by Hotta 2024/12/26
            CheckUserAbort();
            //

            // カメラ制御プロセスの起動を確認
            StartCameraController();

            CameraControlData cont = new CameraControlData();
            cont.Condition = condition;
            DateTime dt = DateTime.Now;
            string imgPath = "AutoFocus"; //string.Format("AutoFocus_{0:D2}{1:D2}{2:D2}", dt.Hour, dt.Minute, dt.Second);

            // modified by Hotta 2022/03/09
            // ファイル名が一致したら、拡張子に関係なく削除する
            /*
            // added by Hotta 2022/02/10
            if (condition.CompressionType == 16 && File.Exists(m_CamMeasPath + imgPath + ".arw") == true)
            {
                File.Delete(m_CamMeasPath + imgPath + ".arw");
            }
            else if (condition.CompressionType == 3 && File.Exists(m_CamMeasPath + imgPath + ".jpg") == true)
            {
                File.Delete(m_CamMeasPath + imgPath + ".jpg");
            }
            */
            if (File.Exists(m_CamMeasPath + imgPath + ".arw") == true)
            {
                File.Delete(m_CamMeasPath + imgPath + ".arw");
            }
            if (File.Exists(m_CamMeasPath + imgPath + ".jpg") == true)
            {
                File.Delete(m_CamMeasPath + imgPath + ".jpg");
            }
            //

            cont.ImgPath = m_CamMeasPath + imgPath;
            cont.ShootFlag = false;
            cont.AutoFocusFlag = true;
            // added by Hotta 2022/06/06 for ライブビュー
            cont.LiveViewFlag = 0;
            //

            if (afArea == null)
            {
                cont.AfArea.focusAreaType = "Center";
                cont.AfArea.focusAreaX = 320;
                cont.AfArea.focusAreaY = 210;
            }
            else
            {
                cont.AfArea.focusAreaType = afArea.focusAreaType;
                cont.AfArea.focusAreaX = afArea.focusAreaX;
                cont.AfArea.focusAreaY = afArea.focusAreaY;
            }

            try
            {
                //CameraControlData.SaveToEncryptFile(CamContFile, cont, CalcKey(AES_Key), CalcKey(AES_IV));
                CameraControlData.SaveToXmlFile(CamContFile, cont);
            }
            catch
            { return false; }

            Thread.Sleep(Settings.Ins.Camera.CameraWait);

            // modified by Hotta 2022/01/20
            /*
            // added by Hotta 2022/01/18
            int count = 0;
            while (true)
            {
                if (File.Exists(tempPath + "\\AutoFocus.txt") != true)
                    break;
                System.Threading.Thread.Sleep(300);
                if (count > 100)
                {
                    throw new Exception("Fail to Auto-Focus.");
                }
                count++;
            }
            //
            */

            // 撮影が完了するまで待機
            try { Wait4Capturing(m_CamMeasPath + imgPath); }
            catch
            {
                // Retry
                DisconnectCamera();
                ConnectCamera();
                //CameraControlData.SaveToEncryptFile(CamContFile, cont, CalcKey(AES_Key), CalcKey(AES_IV));
                CameraControlData.SaveToXmlFile(CamContFile, cont);
                Wait4Capturing(m_CamMeasPath + imgPath);
            }

            return true;
        }

        // added by Hotta 2022/06/06 for ライブビュー
        private void LiveView(string imgPath, ShootCondition condition, bool continuous = false)
        {
            string str = "";
            Stopwatch stopw = new Stopwatch();
            stopw.Reset();
            stopw.Start();

            if (continuous != true)
            {
                if (File.Exists(imgPath + ".jpg") == true)
                {
                    File.Delete(imgPath + ".jpg");
                }
                // added by Hotta 2022/06/21
                // Wait4CApturing()は、拡張子に関係なく有り無しを見ているので、arwを追加
                if (File.Exists(imgPath + ".arw") == true)
                {
                    File.Delete(imgPath + ".arw");
                }
            }

            str += stopw.ElapsedMilliseconds.ToString() + ",";

            // カメラ制御プロセスの起動を確認
            StartCameraController();

            CameraControlData cont = new CameraControlData();
            cont.Condition = condition;
            cont.ImgPath = imgPath;
            cont.ShootFlag = false;
            cont.AutoFocusFlag = false;
            if (continuous != true)
                cont.LiveViewFlag = 1;
            else
                cont.LiveViewFlag = 2;

            str += stopw.ElapsedMilliseconds.ToString() + ",";

            try
            {
                //CameraControlData.SaveToEncryptFile(CamContFile, cont, CalcKey(AES_Key), CalcKey(AES_IV));
                CameraControlData.SaveToXmlFile(CamContFile, cont);
            }
            catch
            { return; }

            str += stopw.ElapsedMilliseconds.ToString() + ",";


            if (continuous != true)
            {
                // 撮影が完了するまで待機
                try
                {
                    Wait4Capturing(imgPath);
                }
                catch
                {
                    // Retry
                    DisconnectCamera();
                    ConnectCamera();
                    //CameraControlData.SaveToEncryptFile(CamContFile, cont, CalcKey(AES_Key), CalcKey(AES_IV));
                    CameraControlData.SaveToXmlFile(CamContFile, cont);
                    Wait4Capturing(imgPath);
                }
            }
            str += stopw.ElapsedMilliseconds.ToString() + ",";
            /*
            using (StreamWriter sw = new StreamWriter("c:\\temp\\live.txt", true))
            {
                sw.WriteLine(str);
            }
            */

            /*
            playSound(applicationPath + "\\Components\\Sound\\Camera-Phone03-1.mp3");
            */
            /*
            Thread.Sleep(Settings.Ins.Camera.CameraWait);
            */
        }

        private void CloseCamera()
        {
            CameraControlData cont;

            // 古いファイルをLoad（前の設定を引き継ぐ)
            if (File.Exists(CamContFile) == true)
            {
                //CameraControlData.LoadFromEncryptFile(CamContFile, out cont, CalcKey(AES_Key), CalcKey(AES_IV));
                CameraControlData.LoadFromXmlFile(CamContFile, out cont);
            }
            else
            { cont = new CameraControlData(); }

            cont.CloseFlag = true;

            //CameraControlData.SaveToEncryptFile(CamContFile, cont, CalcKey(AES_Key), CalcKey(AES_IV));
            CameraControlData.SaveToXmlFile(CamContFile, cont);

            Thread.Sleep(2000); // アプリが終了するまでちょっと待つ
        }

        private void Wait4Capturing(string imgPath)
        {
            DateTime startTime = DateTime.Now;
            while (true)
            {
                // 拡張子が不明なのでJPEGかARWファイルがあればOK
                if (File.Exists(imgPath + ".jpg") == true || File.Exists(imgPath + ".arw") == true)
                { return; }

                // AlphaCameraControllerが停止している場合は起動                
                StartCameraController();

                // タイムアウトの確認
                if ((DateTime.Now - startTime).TotalMilliseconds > Settings.Ins.Camera.CaptureTimeout)
                { throw new Exception("Faild to save Picture data."); }

                // modified by Hotta 2022/06/09 for ライブビュー
                //System.Threading.Thread.Sleep(100);
                System.Threading.Thread.Sleep(1);
            }
        }

        // modified by Hotta 2022/06/06 for カメラ位置
        //private void DispImageFileUnlock(string file, System.Windows.Controls.Image image, MarkerPosition marker = null)
        private void DispImageFileUnlock(string file, System.Windows.Controls.Image image, MarkerPosition marker = null, bool GapCorrect = false)
        {
            // 画像を表示
            try
            {
                using (Stream stream = new FileStream(
                    file,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete
                ))
                {
                    // ロックしないように指定したstreamを使用する。
                    BitmapDecoder decoder = BitmapDecoder.Create(
                        stream,
                        BitmapCreateOptions.None, // この辺のオプションは適宜
                        BitmapCacheOption.Default // これも
                    );
                    BitmapSource bmp = new WriteableBitmap(decoder.Frames[0]);
                    bmp.Freeze();

                    // modified by Hotta 2022/06/06 for カメラ位置
                    /*
                    // xamlでImageを記述 → imgSync
                    image.Source = DrawAuxiliaryLines(bmp, marker);
                    */
                    if (GapCorrect != true)
                        // xamlでImageを記述 → imgSync
                        image.Source = DrawAuxiliaryLines(bmp, marker);
                    else
                        image.Source = DrawAuxiliaryLinesGap(bmp, marker);
                }
            }
            catch (Exception ex)
            {
                // modified by Hotta 2022/06/06 for カメラ位置
                //ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                if (GapCorrect != true)
                    ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
            }
        }

        private void UpdateAccSettings()
        {
            string acc = applicationPath + "\\Components\\AccSettings.xml";

            try
            {
                AccSettings.LoadFromXmlFile(acc);
                AccSettings.Ins.Common.CameraName = Settings.Ins.Camera.Name;
                AccSettings.Ins.Common.CameraWait = Settings.Ins.Camera.CameraWait;
                AccSettings.Ins.CameraControlFile = tempPath + CameraControlFile;
                AccSettings.SaveToXmlFile(acc);

                if(File.Exists(AccSettings.Ins.CameraControlFile) == true)
                { File.Delete(AccSettings.Ins.CameraControlFile); }
            }
            catch { } // 無視
        }

        private void CheckOpenCvSharpDll()
        {
            string dllPath = applicationPath + "\\Components\\";

            // 全てのファイルがあるときはチェックしない
            if (File.Exists(dllPath + @"dll\x86\OpenCvSharpExtern.dll") == true &&
               File.Exists(dllPath + "OpenCvSharp.dll") == true &&
               File.Exists(dllPath + "OpenCvSharp.Blob.dll") == true)
            { return; }

            // OpenCvSharpがインストール済みか確認
            if (File.Exists(OpenCvInstDir + @"OpenCvSharpExtern.dll") == false ||
                File.Exists(OpenCvInstDir + @"OpenCvSharp.dll") == false ||
                File.Exists(OpenCvInstDir + @"OpenCvSharp.Blob.dll") == false)
            { throw new Exception("This Software requires the OpenCVSharp.\r\nPlease install the OpenCVSharp using included \"Download_OpenCVSharp\",  and try again."); }

            if (File.Exists(dllPath + @"dll\x86\OpenCvSharpExtern.dll") == false)
            {
                Directory.CreateDirectory(dllPath + "dll\\x86\\");
                File.Copy(OpenCvInstDir + @"OpenCvSharpExtern.dll", dllPath + @"dll\x86\OpenCvSharpExtern.dll");
            }

            if (File.Exists(dllPath + "OpenCvSharp.dll") == false)
            { File.Copy(OpenCvInstDir + @"OpenCvSharp.dll", dllPath + "OpenCvSharp.dll"); }

            if (File.Exists(dllPath + "OpenCvSharp.Blob.dll") == false)
            { File.Copy(OpenCvInstDir + @"OpenCvSharp.Blob.dll", dllPath + "OpenCvSharp.Blob.dll"); }
        }

        /// <summary>
        /// ユーザーが選択したCabinetを格納する。
        /// こちらのメソッドを使用すると位置合せ時のエリア拡大を行わない。
        /// </summary>
        /// <param name="aryUnit">UIに表示されているCabinetアレイ</param>
        /// <param name="lstTgtUnit">対象Cabinet</param>
        private void CheckSelectedUnits(UnitToggleButton[,] aryUnit, out List<UnitInfo> lstTgtUnit)
        {
            CheckSelectedUnits(aryUnit, out lstTgtUnit, false, out m_lstCamPosUnits);
        }

        // modified by Hotta 2022/12/02
        // modified by Hotta 2022/06/22 for カメラ位置合わせ
        //private void CheckSelectedUnits(UnitToggleButton[,] aryUnit, out List<UnitInfo> lstTgtUnit, bool cameraPos, out List<UnitInfo> lstCamPosUnit)
        private void CheckSelectedUnits(UnitToggleButton[,] aryUnit, out List<UnitInfo> lstTgtUnit, bool cameraPos, out List<UnitInfo> lstCamPosUnit, bool standardSize = false)
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = 0, maxY = 0;

            lstTgtUnit = new List<UnitInfo>();

            // added by Hotta 2022/06/22 for カメラ位置
            lstCamPosUnit = new List<UnitInfo>();
            //

            // 選択されているCabinetを格納、X, Y最大値を捜査する
            for (int y = 0; y < allocInfo.MaxY; y++)
            {
                for (int x = 0; x < allocInfo.MaxX; x++)
                {
                    if (aryUnit[x, y] != null && aryUnit[x, y].IsChecked == true)
                    {
                        lstTgtUnit.Add(aryUnit[x, y].UnitInfo);

                        if (aryUnit[x, y].UnitInfo.X < minX)
                        { minX = aryUnit[x, y].UnitInfo.X; }

                        if (aryUnit[x, y].UnitInfo.Y < minY)
                        { minY = aryUnit[x, y].UnitInfo.Y; }

                        if (aryUnit[x, y].UnitInfo.X > maxX)
                        { maxX = aryUnit[x, y].UnitInfo.X; }

                        if (aryUnit[x, y].UnitInfo.Y > maxY)
                        { maxY = aryUnit[x, y].UnitInfo.Y; }
                    }
                }
            }

            // 矩形の確認
            int area = (maxX - minX + 1) * (maxY - minY + 1);

            if (lstTgtUnit.Count == 0 || area != lstTgtUnit.Count)
            { throw new Exception("The target cabinet area is not selected, or  The selected area is not rectangular."); }

            // 最大サイズ(4k2k)の確認
            int lenX = maxX - minX + 1;
            int lenY = maxY - minY + 1;
            int lenMax;

            // modified by Hotta 2022/12/02
            /*
            if (allocInfo.LEDModel == ZRD_C12A || allocInfo.LEDModel == ZRD_B12A || allocInfo.LEDModel == ZRD_CH12D || allocInfo.LEDModel == ZRD_BH12D)
            { lenMax = CabinetLength_P12 * 2 + 1; } // 8K4Kまで対応　1：分割調整のオーバーラップ分
            else
            { lenMax = CabinetLength_P15 * 2 + 1; } // 8K4Kまで対応　1：分割調整のオーバーラップ分
            */
            if (standardSize != true)
            {
                if (allocInfo.LEDModel == ZRD_C12A 
                    || allocInfo.LEDModel == ZRD_B12A 
                    || allocInfo.LEDModel == ZRD_CH12D 
                    || allocInfo.LEDModel == ZRD_BH12D
                    || allocInfo.LEDModel == ZRD_CH12D_S3
                    || allocInfo.LEDModel == ZRD_BH12D_S3)
                {
#if (DEBUG || Release_log)
                    if (Settings.Ins.ExecLog == true)
                    {
                        MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                    }
#endif
                    lenMax = CabinetLength_P12 * 2 + 1;
                } // 8K4Kまで対応　1：分割調整のオーバーラップ分
                else
                {
#if (DEBUG || Release_log)
                    if (Settings.Ins.ExecLog == true)
                    {
                        MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                    }
#endif
                    lenMax = CabinetLength_P15 * 2 + 1;
                } // 8K4Kまで対応　1：分割調整のオーバーラップ分
            }
            else
            {
                if (allocInfo.LEDModel == ZRD_C12A 
                    || allocInfo.LEDModel == ZRD_B12A 
                    || allocInfo.LEDModel == ZRD_CH12D 
                    || allocInfo.LEDModel == ZRD_BH12D
                    || allocInfo.LEDModel == ZRD_CH12D_S3
                    || allocInfo.LEDModel == ZRD_BH12D_S3)
                {
#if (DEBUG || Release_log)
                    if (Settings.Ins.ExecLog == true)
                    {
                        MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                    }
#endif
                    lenMax = CabinetLength_P12;
                } // 4K2Kまで
                else
                {
#if (DEBUG || Release_log)
                    if (Settings.Ins.ExecLog == true)
                    {
                        MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                    }
#endif
                    lenMax = CabinetLength_P15;
                } // 8K4Kまで
            }
            //

            if (lenX > lenMax || lenY > lenMax || lstTgtUnit.Count > Math.Pow(lenMax, 2))
            {
                if (standardSize == true)
                    throw new Exception("The selected area is out of the adjustable range.\r\nPlease select 4K2K size or less.");
                else
                    throw new Exception("The selected area is out of the adjustable range.\r\nPlease select 8K4K size or less.");
            }

            //-------------------------------------------------------------
            // added by Hotta 2022/06/21 for カメラ位置
            if (cameraPos != true)
            {
                lstCamPosUnit = lstTgtUnit;
                return;
            }

            // added by Hotta 2023/01/16
            // ハンチング防止のため、4K2Kに拡張していたが、
            // 拡張サイズを、最小限の大きさに変更する
            if (allocInfo.LEDModel == ZRD_C12A 
                || allocInfo.LEDModel == ZRD_B12A 
                || allocInfo.LEDModel == ZRD_CH12D 
                || allocInfo.LEDModel == ZRD_BH12D
                || allocInfo.LEDModel == ZRD_CH12D_S3
                || allocInfo.LEDModel == ZRD_BH12D_S3)
            {
                // 4x4Cabinet以上を選択されているなら、それをそのまま使用
                if ((maxX - minX + 1) >= 4 && (maxY - minY + 1) >= 4)
                {
                    lstCamPosUnit = lstTgtUnit;
                }
            }
            else
            {
                // 5x5Cabinet以上を選択されているなら、それをそのまま使用
                if ((maxX - minX + 1) >= 5 && (maxY - minY + 1) >= 5)
                {
                    lstCamPosUnit = lstTgtUnit;
                }
            }

            if(lstCamPosUnit.Count == 0)
            {
                int lenMaxX, lenMaxY;

                if (allocInfo.LEDModel == ZRD_C12A 
                    || allocInfo.LEDModel == ZRD_B12A 
                    || allocInfo.LEDModel == ZRD_CH12D 
                    || allocInfo.LEDModel == ZRD_BH12D
                    || allocInfo.LEDModel == ZRD_CH12D_S3
                    || allocInfo.LEDModel == ZRD_BH12D_S3)
                {
                    if ((maxX - minX + 1) <= 3) { lenMaxX = 4; }    // 拡張
                    else { lenMaxX = (maxX - minX + 1); }           // そのまま

                    if ((maxY - minY + 1) <= 3) { lenMaxY = 4; }
                    else { lenMaxY = (maxY - minY + 1); }
                }
                else
                {
                    if ((maxX - minX + 1) <= 4) { lenMaxX = 5; }
                    else { lenMaxX = (maxX - minX + 1); }

                    if ((maxY - minY + 1) <= 4) { lenMaxY = 5; }
                    else { lenMaxY = (maxY - minY + 1); }
                }
                //

                // modified by Hotta 2022/08/29 離れ小島対策（1ケのProfileに、不連続なWalが複数存在する場合の対策
                int actMinX = int.MaxValue;
                int actMaxX = int.MinValue;
                int actMinY = int.MaxValue;
                int actMaxY = int.MinValue;
                /*
                // 実際に存在するCabinetの位置を取得する。
                for (int y = 0; y < allocInfo.MaxY; y++)
                {
                    for (int x = 0; x < allocInfo.MaxX; x++)
                    {
                        if (aryUnit[x, y].UnitInfo != null)
                        {
                            if (aryUnit[x, y].UnitInfo.X < actMinX)
                                actMinX = aryUnit[x, y].UnitInfo.X;
                            if (aryUnit[x, y].UnitInfo.X > actMaxX)
                                actMaxX = aryUnit[x, y].UnitInfo.X;
                            if (aryUnit[x, y].UnitInfo.Y < actMinY)
                                actMinY = aryUnit[x, y].UnitInfo.Y;
                            if (aryUnit[x, y].UnitInfo.Y > actMaxY)
                                actMaxY = aryUnit[x, y].UnitInfo.Y;
                        }
                    }
                }
                */
                // 選択されたCabinetを含む、物理的に連続したCabinetの範囲を取得する
                // とりあえず、選択されたCabinetのうち、左上のCabinetを取得する
                int selX = -1, selY = -1;

                for (int y = 0; y < allocInfo.MaxY; y++)
                {
                    for (int x = 0; x < allocInfo.MaxX; x++)
                    {
                        if (aryUnit[x, y].IsChecked == true)
                        {
                            selX = x;
                            selY = y;
                            break;
                        }
                    }
                }

                if (selX == -1 || selY == -1)
                { throw new Exception("The target area does not selected."); }

                // ブロブ処理(連続するBlockを探すために使用)
                using (Mat mat = new Mat(new OpenCvSharp.Size(allocInfo.MaxX, allocInfo.MaxY), MatType.CV_8UC1))
                {
                    // modified by Hotta 2022/09/06
                    /*
                    mat.SetTo(0);
                    */
                    for (int y = 0; y < allocInfo.MaxY; y++)
                    {
                        for (int x = 0; x < allocInfo.MaxX; x++)
                        { mat.Set(y, x, 0); }
                    }
                    //
                    for (int y = 0; y < allocInfo.MaxY; y++)
                    {
                        for (int x = 0; x < allocInfo.MaxX; x++)
                        {
                            if (aryUnit[x, y].UnitInfo != null)
                            { mat.Set(y, x, 1); }
                        }
                    }

                    ////////
                    //mat.SaveImage(applicationPath + "\\Temp\\allocinfo.bmp");
                    ///////

                    // modified by Hotta 2022/11/11
                    // ブロブ処理を、8連結→4連結に変更
                    // ■8連結
                    // 000000000
                    // 011100000
                    // 011100000
                    // 000011110
                    // 000011110
                    // 000000000

                    // ■4連結
                    // 000000000
                    // 011100000
                    // 011100000
                    // 000022220
                    // 000022220
                    // 000000000

                    /*
                    CvBlobs blobs = new CvBlobs(mat);

                    // 対象Cabinetを含んでいるblobのMin/maxを取得
                    foreach (KeyValuePair<int, CvBlob> item in blobs)
                    {
                        //int labelValue = item.Key;
                        CvBlob blob = item.Value;

                        if(blob.MinX <= selX && selX <= blob.MaxX && blob.MinY <= selY && selY <= blob.MaxY)
                        {
                            actMinX = blob.MinX + 1;    // 以降のソースで、actMinX は、1からスタートするようになっている
                            actMaxX = blob.MaxX + 1;
                            actMinY = blob.MinY + 1;
                            actMaxY = blob.MaxY + 1;
                            break;
                        }
                    }
                    */
                    ConnectedComponents cc = Cv2.ConnectedComponentsEx(mat, PixelConnectivity.Connectivity4);
                    // cv::connectedComponentsは、背景領域をラベル値0の1つのblobとして扱う仕様なので、最初の要素をスキップ
                    foreach (var blob in cc.Blobs.Skip(1))
                    {
                        if (blob.Left <= selX && selX <= blob.Left + blob.Width - 1 && blob.Top <= selY && selY <= blob.Top + blob.Height - 1)
                        {
                            actMinX = blob.Left + 1;    // 以降のソースで、actMinX は、1からスタートするようになっている
                            actMaxX = (blob.Left + blob.Width - 1) + 1;
                            actMinY = blob.Top + 1;
                            actMaxY = (blob.Top + blob.Height - 1) + 1;
                            break;
                        }
                    }
                }
                //

                // カメラ位置合わせに使用するCabinet数（V)
                // 4K2K or Wall の小さい方を採用
                int camPosCabinetVNum = 0;
                // modified by Hotta 2023/01/16
                // if (lenMax < actMaxY - actMinY + 1)
                if (lenMaxY < actMaxY - actMinY + 1)
                {
                    // modified by Hotta 2023/01/16
                    //camPosCabinetVNum = lenMax;
                    camPosCabinetVNum = lenMaxY;
                }
                else
                { camPosCabinetVNum = actMaxY - actMinY + 1; }

                // カメラ位置合わせに使用するCabinet位置（V)
                int camPosCabinetBottom;
                int camPosCabinetTop;
                // modified by Hotta 2023/01/16
                //if (actMaxY - minY + 1 <= lenMax)    // 補正対象の一番上(minY)が、存在する一番下(actMaxY)から2K以内であれば、一番下（actMaxY)からcamPosCabinetVNumだけ使う。
                if (actMaxY - minY + 1 <= lenMaxY)    // 補正対象の一番上(minY)が、存在する一番下(actMaxY)から拡張Cabinet分以内であれば、一番下（actMaxY)からcamPosCabinetVNumだけ使う。
                {
                    camPosCabinetBottom = actMaxY - 1;   // MaxY：1 からスタートなので、-1する
                    camPosCabinetTop = camPosCabinetBottom - camPosCabinetVNum + 1;
                }
                else// 補正対象の一番上(minY)が、存在する一番下(actMaxY)から拡張Cabinet分より上であれば（なるべく上にあげないようにする）
                {
                    camPosCabinetTop = minY - 1;
                    // modified by Hotta 2023/01/16
                    // camPosCabinetBottom = camPosCabinetTop + lenMax - 1;
                    camPosCabinetBottom = camPosCabinetTop + lenMaxY - 1;
                }

                // カメラ位置合わせに使用するCabinet数（H）
                // 4K2K or Wall の小さい方を採用
                int camPosCabinetHNum = 0;
                // modified by Hotta 2023/01/16
                // if (lenMax < actMaxX - actMinX + 1)
                if (lenMaxX < actMaxX - actMinX + 1)
                {
                    // modified by Hotta 2023/01/16
                    //camPosCabinetHNum = lenMax;
                    camPosCabinetHNum = lenMaxX;
                }
                else
                { camPosCabinetHNum = actMaxX - actMinX + 1; }

                int correctCabinetLeft = -1, correctCabinetRight = -1;
                for (int x = 0; x < allocInfo.MaxX; x++)
                {
                    for (int y = 0; y < allocInfo.MaxY; y++)
                    {
                        if (aryUnit[x, y] != null && aryUnit[x, y].IsChecked == true)
                        {
                            correctCabinetLeft = x;
                            break;
                        }
                    }
                    if (correctCabinetLeft != -1)
                        break;
                }
                if (correctCabinetLeft == -1)
                {
                    throw new Exception("Fail to find left cabinet to set camera position.");
                }

                for (int x = allocInfo.MaxX - 1; x >= 0; x--)
                {
                    for (int y = 0; y < allocInfo.MaxY; y++)
                    {
                        if (aryUnit[x, y] != null && aryUnit[x, y].IsChecked == true)
                        {
                            correctCabinetRight = x;
                            break;
                        }
                    }
                    if (correctCabinetRight != -1)
                    { break; }
                }

                if (correctCabinetRight == -1)
                { throw new Exception("Fail to find right cabinet to set camera position."); }

                int camPosCabinetLeft = (int)((double)(correctCabinetLeft + correctCabinetRight) / 2 - (double)camPosCabinetHNum / 2 + 0.5);
                while (camPosCabinetLeft < actMinX - 1)
                { camPosCabinetLeft++; }

                int camPosCabinetRight = camPosCabinetLeft + camPosCabinetHNum - 1;
                while (camPosCabinetRight > actMaxX - 1)
                {
                    camPosCabinetRight--;
                    camPosCabinetLeft--;
                }

                // added by Hotta 2022/08/01 for 有効なCabinetで、矩形にAssyされていない場合
                // 各端に無効なCabinetがあったら、各辺を1行ずつ削っていく
                // 取りうる最大サイズのCabinetを選択する
                while (true)
                {
                    bool changed = false;

                    // V方向のハンチングが強いので、左右を先に削る
                    for (int y = camPosCabinetTop; y <= camPosCabinetBottom; y++)
                    {
                        if (aryUnit[camPosCabinetLeft, y].UnitInfo == null && camPosCabinetLeft < minX - 1) // minX : 1からスタート
                        {
                            changed = true;
                            camPosCabinetLeft++;
                            break;
                        }
                    }
                    for (int y = camPosCabinetTop; y <= camPosCabinetBottom; y++)
                    {
                        if (aryUnit[camPosCabinetRight, y].UnitInfo == null && camPosCabinetRight > maxX - 1)
                        {
                            changed = true;
                            camPosCabinetRight--;
                            break;
                        }
                    }

                    // 上下は、左右の後で削る
                    // 先に左右を削ったことで、上下を削らなくてよいケースもある。
                    for (int x = camPosCabinetLeft; x <= camPosCabinetRight; x++)
                    {
                        if (aryUnit[x, camPosCabinetTop].UnitInfo == null && camPosCabinetTop < minY - 1)
                        {
                            changed = true;
                            camPosCabinetTop++;
                            break;
                        }
                    }

                    for (int x = camPosCabinetLeft; x <= camPosCabinetRight; x++)
                    {
                        if (aryUnit[x, camPosCabinetBottom].UnitInfo == null && camPosCabinetBottom > maxY - 1)
                        {
                            changed = true;
                            camPosCabinetBottom--;
                            break;
                        }
                    }

                    if (changed == false)
                    { break; }
                }
                //

                for (int y = camPosCabinetTop; y <= camPosCabinetBottom; y++)
                {
                    for (int x = camPosCabinetLeft; x <= camPosCabinetRight; x++)
                    { lstCamPosUnit.Add(aryUnit[x, y].UnitInfo); }
                }
            }

            /*
            // デバッグ
            int cabinetHSize = 160;
            int cabinetVSize = 90;

            Mat _mat = new Mat(new OpenCvSharp.Size(cabinetHSize * allocInfo.MaxX, cabinetVSize * allocInfo.MaxY), MatType.CV_8UC3);

            for (int y = 0; y < allocInfo.MaxY; y++)
            {
                for (int x = 0; x < allocInfo.MaxX; x++)
                {
                    if(aryUnit[x, y].UnitInfo == null)
                    {
                        OpenCvSharp.Rect rect = new OpenCvSharp.Rect(cabinetHSize * x, cabinetVSize * y, cabinetHSize, cabinetVSize);
                        _mat.Rectangle(rect, new Scalar(60, 60, 60), -1);
                    }
                }
            }
            foreach (UnitInfo unit in lstTgtUnit)
            {
                OpenCvSharp.Rect rect = new OpenCvSharp.Rect(cabinetHSize * (unit.X - 1), cabinetVSize * (unit.Y - 1), cabinetHSize, cabinetVSize);
                _mat.Rectangle(rect, new Scalar(100, 100, 0), -1);
            }

            foreach (UnitInfo unit in lstCamPosUnit)
            {
                OpenCvSharp.Rect rect = new OpenCvSharp.Rect(cabinetHSize * (unit.X - 1), cabinetVSize * (unit.Y - 1), cabinetHSize, cabinetVSize);
                _mat.Rectangle(rect, new Scalar(0, 200, 200), 5);
            }

            for (int y = 0; y < allocInfo.MaxY; y++)
            {
                for (int x = 0; x < allocInfo.MaxX; x++)
                {
                    OpenCvSharp.Rect rect = new OpenCvSharp.Rect(cabinetHSize * x, cabinetVSize * y, cabinetHSize, cabinetVSize);
                    _mat.Rectangle(rect, new Scalar(200, 200, 200), 1);
                }
            }

            _mat.SaveImage(applicationPath + "\\Temp\\lstCamPosUnit.bmp");

            _mat.Dispose();
            //
            */

            return;
        }

        public double ToDegree(double radian)
        {
            return (double)(radian * 180 / Math.PI);
        }

        public double ToRadian(double angle)
        {
            return (double)(angle * Math.PI / 180);
        }

        private double CalcDistance(Coordinate a, Coordinate b)
        {
            return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        }

        public void ShowLensCdFiles(int CameraSelection)
        {
            string dir = applicationPath + CompDir + CamCorrectDir;
            string pattern = fn_CameraCorrectionData + Settings.Ins.Camera.Name + "_*";

            string[] files = System.IO.Directory.GetFiles(dir, pattern);
            string aveFile = fn_CameraCorrectionData + Settings.Ins.Camera.Name + "_" + LensCdAveNameKey;

            cmbxUfCamLensCd.Items.Clear();
            cmbxGapCamLensCd.Items.Clear();

            // ILCE-6400の場合のみ
            // カメラテスト
            if (Settings.Ins.Camera.Name == CamNames[0])            
            {
                cmbxUfCamLensCd.Items.Add(LensCdAveNameKey);
                cmbxGapCamLensCd.Items.Add(LensCdAveNameKey);
            }

            for (int i = 0; i < files.Length; i++)
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(files[i]);

                if (name != aveFile)
                {
                    cmbxUfCamLensCd.Items.Add(name.Replace(fn_CameraCorrectionData + Settings.Ins.Camera.Name + "_", ""));
                    cmbxGapCamLensCd.Items.Add(name.Replace(fn_CameraCorrectionData + Settings.Ins.Camera.Name + "_", ""));
                }
            }

            cmbxUfCamLensCd.SelectedIndex = 0;
            cmbxGapCamLensCd.SelectedIndex = 0;
        }

        //public void CheckLightingReflection(List<UnitInfo> lstTgtUnit, bool GapCorrect = false)
        //{
        //    // マスク用信号出力
        //    outputGapCamTargetArea(lstTgtUnit, true);

        //    Thread.Sleep(500);

        //    // added by Hotta 2022/06/21
        //    ShootCondition condition = new ShootCondition(Settings.Ins.GapCam.Setting_A6400);
        //    condition.CompressionType = 2;
        //    //

        //    // マスク画像撮影(jpeg)
        //    string maskFile = tempPath + System.IO.Path.GetFileNameWithoutExtension(fn_MaskFile);
        //    // modified by Hotta 2022/06/21
        //    // CaptureImage(maskFile, Settings.Ins.Camera.SetPosSetting);
        //    if(GapCorrect != true)
        //        CaptureImage(maskFile, Settings.Ins.Camera.SetPosSetting);
        //    else
        //        CaptureImage(maskFile, condition);
        //    //
        //    // 内部信号停止
        //    stopIntSig();

        //    Thread.Sleep(500);

        //    // 黒画像撮影(jpeg)
        //    string blackFile = tempPath + System.IO.Path.GetFileNameWithoutExtension(fn_RefBlackFile);
        //    // modified by Hotta 2022/06/21
        //    //CaptureImage(blackFile, Settings.Ins.Camera.SetPosSetting);
        //    if(GapCorrect != true)
        //        CaptureImage(blackFile, Settings.Ins.Camera.SetPosSetting);
        //    else
        //        CaptureImage(blackFile, condition);
        //    //

        //    // マスク内の一定以上の明るさ、面積のエリアを検出
        //    try { CalcMaxPvForReflection(maskFile + ".jpg", blackFile + ".jpg"); }
        //    catch (Exception ex)
        //    {
        //        bool? result;
        //        string msg = ex.Message + "\r\n\r\nDo you continue processing?";

        //        showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 500, 200, "Yes", "No");

        //        if (result != true)
        //        { throw new Exception("The Processing has been canceled."); }
        //    }
        //}

        //public void CalcMaxPvForReflection(string maskFile, string blackFile)
        //{
        //    //double max = 0;

        //    using (Mat src = new Mat(blackFile, ImreadModes.Color))
        //    using (Mat gray = src.CvtColor(ColorConversionCodes.BGR2GRAY, 0))
        //    using (Mat mask = new Mat(maskFile, ImreadModes.Grayscale).Threshold(0, 0xff, ThresholdTypes.Otsu))            
        //    {
        //        // 最大のエリアをMaskにする
        //        CvBlobs blobs = new CvBlobs(mask);
        //        CvBlob measArea = new CvBlob();

        //        // 最大のBlobを検索
        //        foreach (KeyValuePair<int, CvBlob> pair in blobs)
        //        {
        //            if (pair.Value.Area > measArea.Area)
        //            { measArea = pair.Value; }
        //        }

        //        //if(measArea.Area < cabiDx * cabiDy * 0.5) // 大体カメラの1画素でLED1画素くらいに映るので1Cabiの半分以下ならたぶんうまくいってない。
        //        //{ throw new Exception("The masked area is too small."); }

        //        using (Mat masked = gray.Clone(new OpenCvSharp.Rect(measArea.Rect.X, measArea.Rect.Y, measArea.Rect.Width, measArea.Rect.Height)))
        //        using (Mat binary = masked.Threshold(255 * Math.Pow((double)Settings.Ins.Camera.SpecReflection / 255, 1 / 2.2), 0xff, ThresholdTypes.Binary))
        //        {
        //            CvBlobs lightBlobs = new CvBlobs(binary);
        //            lightBlobs.FilterByArea(Settings.Ins.Camera.SpecRefArea, int.MaxValue);

        //            if (Settings.Ins.Camera.SaveIntImage)
        //            {
        //                //Cv2.ImWrite(applicationPath + @"\Temp\gray.jpg", gray, (int[])null);
        //                Cv2.ImWrite(applicationPath + @"\Temp\gen_mask.jpg", mask);
        //                Cv2.ImWrite(applicationPath + @"\Temp\masked.jpg", masked);
        //                Cv2.ImWrite(applicationPath + @"\Temp\binary.jpg", binary);
        //            }

        //            CvBlob maxBlob = new CvBlob();

        //            foreach(CvBlob blob in lightBlobs.Values)
        //            {
        //                if(blob.Area > maxBlob.Area)
        //                { maxBlob = blob; }
        //            }

        //            if (lightBlobs.Count > 0)
        //            {
        //                string msg = "(Reflection Point [x, y][%] : [" + (maxBlob.Centroid.X / binary.Width * 100).ToString("0") + ", " + (maxBlob.Centroid.Y / binary.Height * 100).ToString("0") + "], Area : " + maxBlob.Area + ")";
        //                throw new Exception("The reflection of light was detected.\r\n" + msg);
        //            }
        //        }
        //    }

        //    return;
        //}

        private void ManageLogGen(string dir, string key)
        {
            // key + "yyyyMMddHHmm"形式に対応
            string[] dirs = System.IO.Directory.GetDirectories(dir, key + "*");

            List<DateTime> lstDate = new List<DateTime>();

            for (int i = 0; i < dirs.Length; i++)
            {
                try
                {
                    string[] dirNames = dirs[i].Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
                    string strDate = dirNames[dirNames.Length - 1].Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries)[1];
                    DateTime date = DateTime.ParseExact(strDate, "yyyyMMddHHmm", null);

                    lstDate.Add(date);
                }
                catch { continue; }
            }

            // Keyで並び替え
            lstDate.Sort();

            // Logの最大世代数より多い場合、削除
            for (; lstDate.Count > Settings.Ins.Camera.LogMaxGen;)
            {
                try { System.IO.Directory.Delete(dir + "\\" + key + lstDate[0].ToString("yyyyMMddHHmm"), true); }
                catch { } // 失敗した場合は無視

                lstDate.RemoveAt(0);
            }
        }

        private void loadArwFile(string file, out AcqARW arwHelper, bool loadLedCt = false)
        {
            // added by Hotta 2024/01/10
            CheckUserAbort();
            //

            byte[] buffer;
            arwHelper = new AcqARW();

            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
            }

            if (arwHelper.SetARW(buffer) == false || arwHelper.SetTiffHeader() == false || arwHelper.Set0thIFD() == false || arwHelper.Set1stIFD() == false || arwHelper.Set2ndIFD() == false || arwHelper.SetExifIFD() == false || arwHelper.SetRawMainIFD() == false)
            { throw new Exception(arwHelper.LastErrorMessage); }

            // カメラ・レンズ確認/カメラ設定確認(Zoom)
            string cameraModel = arwHelper._0thIFD.Model;
            string lensModel = arwHelper.ExifIFD.LensModel;
            int zoom = (int)arwHelper.ExifIFD.FocalLength[0];

            if (Settings.Ins.Camera.Name == CamNames[0]) // ILCE-6400
            {
                if (cameraModel != CamNames[0])
                { throw new Exception("An unregistered camera is being used."); }

                // modified by Hotta 2025/07/04 for LED-8511
                /*
                if (lensModel != LensNames[0])
                { throw new Exception("An unregistered lens is being used."); }
                */
                if (lensModel != LensNames[0] && lensModel != LensNames[2])
                { throw new Exception("An unregistered lens is being used."); }
                //

                if (zoom != 160) // 16mm(↑レンズのワイド端固定)
                { throw new Exception("The zoom position is inappropriate.\r\nPlease use at the wide end."); }
            }
            else if (Settings.Ins.Camera.Name == CamNames[1]) // ILCE-7RM3
            {
                if (cameraModel != CamNames[1])
                { throw new Exception("An unregistered camera is being used."); }

                if (lensModel != LensNames[1])
                { throw new Exception("An unregistered lens is being used."); }

                if (zoom != 240) // 24mm(↑レンズのワイド端固定)
                { throw new Exception("The zoom position is inappropriate.\r\nPlease use at the wide end."); }
            }
            else
            {
                throw new Exception("An unregistered camera is being used.");
            }

            // LED校正テーブルのLoad
            if (loadLedCt == true)
            {
                string lens = "";
                // modified by Hotta 2025/07/04 for LED-8511
                /*
                if (lensModel == LensNames[0])
                { lens = LensModels[0]; }
                */
                if (lensModel == LensNames[0] || lensModel == LensNames[2])
                { lens = LensModels[0]; }
                //
                else if (lensModel == LensNames[1])
                { lens = LensModels[1]; }

                var ledFileName = allocInfo.LEDModel;
                switch (ledFileName)
                {
                    case ZRD_BH12D_S3:
                        ledFileName = ZRD_BH12D;
                        break;
                    case ZRD_BH15D_S3:
                        ledFileName = ZRD_BH15D;
                        break;
                    case ZRD_CH12D_S3:
                        ledFileName = ZRD_CH12D;
                        break;
                    case ZRD_CH15D_S3:
                        ledFileName = ZRD_CH15D;
                        break;
                }
                string lcdFile = applicationPath + CompDir + CamCorrectDir + fn_LedCorrectionData + ledFileName + "_" + cameraModel + "_" + lens + ".xml";
                LedCorrectionData.LoadFromXmlFile(lcdFile, out lcd);

                lcd.SHA256Hash = CalcSha256Hash(lcdFile);
            }
        }

        private bool SetThroughMode(bool flag)
        {
            foreach (ControllerInfo cont in dicController.Values)
            {
                // Through Mode
                if (flag == true)
                {
                    if (sendSdcpCommand(SDCPClass.CmdThroughModeOn, cont.IPAddress) != true)
                    { return false; }
                }
                else
                {
                    if (sendSdcpCommand(SDCPClass.CmdThroughModeOff, cont.IPAddress) != true)
                    { return false; }
                }
            }

            return true;
        }

        #region Pattern

        //private void outputPosMaker(List<UnitInfo> lstTgtUnit)
        //{
        //    // Chiron Wall上の座標を取得する
        //    getTargetDispPos(lstTgtUnit, out int startX, out int startY, out int endX, out int endY);

        //    Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
        //    Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

        //    setPosMakerCommand(ref cmd, startX, startY, endX - startX, endY - startY);

        //    foreach (ControllerInfo cont in dicController.Values)
        //    { sendSdcpCommand(cmd, 0, cont.IPAddress); }

        //    Thread.Sleep(Settings.Ins.Camera.PatternWait);
        //}

        //private void setPosMakerCommand(ref Byte[] cmd, int startX, int startY, int width, int height)
        //{
        //    try
        //    {
        //        // Foreground Color
        //        cmd[22] = (byte)(IntSigLvMax >> 8); // Red
        //        cmd[23] = (byte)(IntSigLvMax & 0xFF);
        //        cmd[24] = (byte)(IntSigLvMax >> 8); // Green
        //        cmd[25] = (byte)(IntSigLvMax & 0xFF);
        //        cmd[26] = (byte)(IntSigLvMax >> 8); // Blue
        //        cmd[27] = (byte)(IntSigLvMax & 0xFF);

        //        // Background Color
        //        cmd[28] = (byte)(0 >> 8); ; // Red
        //        cmd[29] = (byte)(0 & 0xFF);
        //        cmd[30] = (byte)(0 >> 8); ; // Green
        //        cmd[31] = (byte)(0 & 0xFF);
        //        cmd[32] = (byte)(0 >> 8); ; // Blue
        //        cmd[33] = (byte)(0 & 0xFF);

        //        // Start Position
        //        cmd[34] = (byte)(startX >> 8);
        //        cmd[35] = (byte)(startX & 0xFF);
        //        cmd[36] = (byte)(startY >> 8);
        //        cmd[37] = (byte)(startY & 0xFF);

        //        // H, V Width
        //        cmd[38] = (byte)(Settings.Ins.Camera.MakerSize >> 8);
        //        cmd[39] = (byte)(Settings.Ins.Camera.MakerSize & 0xFF);
        //        cmd[40] = (byte)(Settings.Ins.Camera.MakerSize >> 8);
        //        cmd[41] = (byte)(Settings.Ins.Camera.MakerSize & 0xFF);

        //        // H, V Pitch
        //        cmd[42] = (byte)((width - Settings.Ins.Camera.MakerSize) >> 8);
        //        cmd[43] = (byte)((width - Settings.Ins.Camera.MakerSize) & 0xFF);
        //        cmd[44] = (byte)((height - Settings.Ins.Camera.MakerSize) >> 8);
        //        cmd[45] = (byte)((height - Settings.Ins.Camera.MakerSize) & 0xFF);
        //    }
        //    catch { } // 無視する

        //    // RGB
        //    cmd[21] = 0;
        //    cmd[21] += 0x40;
        //    cmd[21] += 0x20;
        //    cmd[21] += 0x10;

        //    cmd[21] += 0x01; // Hatch            
        //}

        /// <summary>
        /// 基準点を点灯させる。
        /// Default/Cabinetの1箇所のみ点灯するバージョン。
        /// </summary>
        /// <param name="tgtCabinet"></param>
        /// <param name="quad"></param>
        private void outputTrimAreaRef(UnitInfo tgtCabinet, out Quadrant quad)
        {
            PickReferenceModule(tgtCabinet, out quad, out int module);

            // added by Hotta 2024/09/10 for crosstalk
#if ForCrosstalkCameraUF
            ReferenceOneModuleNo = module;
#endif

            foreach (ControllerInfo cont in dicController.Values)
            {
                if (cont.ControllerID == tgtCabinet.ControllerID)
                {
                    Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
                    Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

                    setTrimAreaWindowCommand(ref cmd, tgtCabinet, module);
                    sendSdcpCommand(cmd, 0, cont.IPAddress);
                }
            }

            Thread.Sleep(Settings.Ins.Camera.PatternWait);
        }

        private void PickReferenceModule(UnitInfo tgtCabinet, out Quadrant quad, out int module)
        {
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            //int module; // 目標Module

            // Allocation情報を走査
            for (int x = 0; x < allocInfo.lstUnits.Count; x++)
            {
                for (int y = 0; y < allocInfo.lstUnits[x].Count; y++)
                {
                    // Null=Cabinetがない場合はSkip
                    if (allocInfo.lstUnits[x][y] == null)
                    { continue; }

                    if (allocInfo.lstUnits[x][y].X < minX)
                    { minX = allocInfo.lstUnits[x][y].X; }

                    if (allocInfo.lstUnits[x][y].X > maxX)
                    { maxX = allocInfo.lstUnits[x][y].X; }

                    if (allocInfo.lstUnits[x][y].Y < minY)
                    { minY = allocInfo.lstUnits[x][y].Y; }

                    if (allocInfo.lstUnits[x][y].Y > maxY)
                    { maxY = allocInfo.lstUnits[x][y].Y; }
                }
            }

            // 中心に一番近いModuleを選択する
            // 第一象限
            if (tgtCabinet.X > Math.Floor(((double)(maxX + minX) / 2)) && tgtCabinet.Y <= Math.Floor(((double)(maxY + minY) / 2)))
            {
                quad = Quadrant.Quad_1;

                if (moduleCount == ModuleCount_Module4x2) // Chiron
                { module = 4; }
                else // Cancun
                { module = 8; }
            }
            // 第二象限
            else if (tgtCabinet.X <= Math.Floor(((double)(maxX + minX) / 2)) && tgtCabinet.Y <= Math.Floor(((double)(maxY + minY) / 2)))
            {
                quad = Quadrant.Quad_2;

                if (moduleCount == ModuleCount_Module4x2) // Chiron
                { module = 7; }
                else // Cancun
                { module = 11; }
            }
            // 第三象限
            else if (tgtCabinet.X <= Math.Floor(((double)(maxX + minX) / 2)) && tgtCabinet.Y > Math.Floor(((double)(maxY + minY) / 2)))
            {
                quad = Quadrant.Quad_3;

                //if (moduleCount == ModuleCount_Module4x2) // Chiron
                //{ module = 3; }
                //else // Cancun
                { module = 3; } // Coverity指摘のため修正
            }
            // 第四象限
            else
            {
                quad = Quadrant.Quad_4;

                //if (moduleCount == ModuleCount_Module4x2) // Chiron
                //{ module = 0; }
                //else // Cancun
                { module = 0; } // Coverity指摘のため修正
            }
        }

        private void setTrimAreaWindowCommand(ref byte[] cmd, UnitInfo unit, int cellNum)
        {
            try
            {
                // Foreground Color
                cmd[22] = (byte)(IntSigLvMax >> 8); // Red
                cmd[23] = (byte)(IntSigLvMax & 0xFF);
                cmd[24] = (byte)(IntSigLvMax >> 8); // Green
                cmd[25] = (byte)(IntSigLvMax & 0xFF);
                cmd[26] = (byte)(IntSigLvMax >> 8); // Blue
                cmd[27] = (byte)(IntSigLvMax & 0xFF);

                // Background Color
                cmd[28] = (byte)(0 >> 8); ; // Red
                cmd[29] = (byte)(0 & 0xFF);
                cmd[30] = (byte)(0 >> 8); ; // Green
                cmd[31] = (byte)(0 & 0xFF);
                cmd[32] = (byte)(0 >> 8); ; // Blue
                cmd[33] = (byte)(0 & 0xFF);

                // Start Position
                int startX = unit.PixelX + (modDx / 2 - Settings.Ins.Camera.TrimmingSize / 2) + modDx * (cellNum % 4); // 4 = CellのX方向の数
                int startY = unit.PixelY + (modDy / 2 - Settings.Ins.Camera.TrimmingSize / 2) + modDy * (cellNum / 4);

                cmd[34] = (byte)(startX >> 8);
                cmd[35] = (byte)(startX & 0xFF);
                cmd[36] = (byte)(startY >> 8);
                cmd[37] = (byte)(startY & 0xFF);

                // H, V Width
                cmd[38] = (byte)(Settings.Ins.Camera.TrimmingSize >> 8);
                cmd[39] = (byte)(Settings.Ins.Camera.TrimmingSize & 0xFF);
                cmd[40] = (byte)(Settings.Ins.Camera.TrimmingSize >> 8);
                cmd[41] = (byte)(Settings.Ins.Camera.TrimmingSize & 0xFF);

                // H, V Pitch
                cmd[42] = (byte)(cabiDx >> 8);
                cmd[43] = (byte)(cabiDx & 0xFF);
                cmd[44] = (byte)(cabiDy >> 8);
                cmd[45] = (byte)(cabiDy & 0xFF);
            }
            catch { } // 無視する

            // RGB
            cmd[21] = 0;
            cmd[21] += 0x40;
            cmd[21] += 0x20;
            cmd[21] += 0x10;

            cmd[21] += 0x09; // Window
        }

        /// <summary>
        /// Base Cabinet(Line)用のマスク画像を出力する
        /// </summary>
        /// <param name="lstTgtCabi"></param>
        /// <param name="edge"></param>
        private void OutputMaskLineRef(List<UnitInfo> lstTgtCabi, ObjectiveEdge edge)
        {
            int moduleY;

            if (allocInfo.LEDModel == ZRD_CH12D 
                || allocInfo.LEDModel == ZRD_CH15D 
                || allocInfo.LEDModel == ZRD_BH12D 
                || allocInfo.LEDModel == ZRD_BH15D
                || allocInfo.LEDModel == ZRD_CH12D_S3
                || allocInfo.LEDModel == ZRD_CH15D_S3
                || allocInfo.LEDModel == ZRD_BH12D_S3
                || allocInfo.LEDModel == ZRD_BH15D_S3) // Cancun
            {
#if (DEBUG || Release_log)
                if (Settings.Ins.ExecLog == true)
                {
                    MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                }
#endif
                moduleY = 3;
            }
            else // Chiron
            {
#if (DEBUG || Release_log)
                if (Settings.Ins.ExecLog == true)
                {
                    MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                }
#endif
                moduleY = 2;
            }

            // コントローラ毎に出力範囲を決定
            foreach (ControllerInfo cont in dicController.Values)
            {
                int minX = int.MaxValue, minY = int.MaxValue, maxX = 0, maxY = 0;
                
                // 辺を探すため、先にX, Yの最大値、最小値を求める
                foreach (UnitInfo unit in lstTgtCabi)
                {
                    if (unit.X < minX)
                    { minX = unit.X; }

                    if (unit.Y < minY)
                    { minY = unit.Y; }

                    if (unit.X > maxX)
                    { maxX = unit.X; }

                    if (unit.Y > maxY)
                    { maxY = unit.Y; }                    
                }

                int startX = int.MaxValue, startY = int.MaxValue, endX = 0, endY = 0;                
                bool flag = false;

                foreach (UnitInfo cabi in lstTgtCabi)
                {
                    if (edge == ObjectiveEdge.Top)
                    {
                        if (cabi.ControllerID == cont.ControllerID && cabi.Y == minY)
                        {
                            if(cabi.PixelX <= startX)
                            { startX = cabi.PixelX; }
                            if(cabi.PixelY <= startY)
                            { startY = cabi.PixelY + modDy * (moduleY - 1); }
                            if(cabi.PixelX >= endX)
                            { endX = cabi.PixelX; }
                            if(cabi.PixelY >= endY)
                            { endY = cabi.PixelY; }

                            flag = true;
                        }
                    }
                    else if (edge == ObjectiveEdge.Bottom)
                    {
                        if (cabi.ControllerID == cont.ControllerID && cabi.Y == maxY)
                        {
                            if (cabi.PixelX <= startX)
                            { startX = cabi.PixelX; }
                            if (cabi.PixelY <= startY)
                            { startY = cabi.PixelY; }
                            if (cabi.PixelX >= endX)
                            { endX = cabi.PixelX; }
                            if (cabi.PixelY >= endY)
                            { endY = cabi.PixelY - modDy * (moduleY - 1); }

                            flag = true;
                        }
                    }
                    else if (edge == ObjectiveEdge.Left)
                    {
                        if (cabi.ControllerID == cont.ControllerID && cabi.X == minX)
                        {
                            if (cabi.PixelX <= startX)
                            { startX = cabi.PixelX + cabiDx - modDx; }
                            if (cabi.PixelY <= startY)
                            { startY = cabi.PixelY; }
                            if (cabi.PixelX >= endX)
                            { endX = cabi.PixelX; }
                            if (cabi.PixelY >= endY)
                            { endY = cabi.PixelY; }

                            flag = true;
                        }
                    }
                    else if (edge == ObjectiveEdge.Right)
                    {
                        if (cabi.ControllerID == cont.ControllerID && cabi.X == maxX)
                        {
                            if (cabi.PixelX <= startX)
                            { startX = cabi.PixelX; }
                            if (cabi.PixelY <= startY)
                            { startY = cabi.PixelY; }
                            if (cabi.PixelX >= endX)
                            { endX = cabi.PixelX - cabiDx + modDx; }
                            if (cabi.PixelY >= endY)
                            { endY = cabi.PixelY; }

                            flag = true;
                        }
                    }
                }

                if (flag == true)
                {
                    int height = endY - startY + cabiDy;
                    int width = endX - startX + cabiDx;

                    Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
                    Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

                    setIntSigWindowCommand(ref cmd, startX, startY, height, width, m_MeasureLevel, m_MeasureLevel, m_MeasureLevel);

                    sendSdcpCommand(cmd, 0, cont.IPAddress);
                }
                else
                {                    
                    Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
                    Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);
                    setFlatCommand(ref cmd, 0, 0, 0);
                    sendSdcpCommand(cmd, 0, cont.IPAddress);
                }
            }

            Thread.Sleep(Settings.Ins.IntSignalWait);
        }

        /// <summary>
        /// Base Cabinet(Line)用に前面にTrimmingAreaを表示する
        /// </summary>
        private void OutputTrimAreaRefAll()
        {
            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            setTrimAreaCommand(ref cmd);

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(cmd, 0, cont.IPAddress); }

            Thread.Sleep(Settings.Ins.Camera.PatternWait);
        }

        private void setTrimAreaCommand(ref Byte[] cmd)
        {
            try
            {
                // Foreground Color
                cmd[22] = (byte)(IntSigLvMax >> 8); // Red
                cmd[23] = (byte)(IntSigLvMax & 0xFF);
                cmd[24] = (byte)(IntSigLvMax >> 8); // Green
                cmd[25] = (byte)(IntSigLvMax & 0xFF);
                cmd[26] = (byte)(IntSigLvMax >> 8); // Blue
                cmd[27] = (byte)(IntSigLvMax & 0xFF);

                // Background Color
                cmd[28] = (byte)(0 >> 8); ; // Red
                cmd[29] = (byte)(0 & 0xFF);
                cmd[30] = (byte)(0 >> 8); ; // Green
                cmd[31] = (byte)(0 & 0xFF);
                cmd[32] = (byte)(0 >> 8); ; // Blue
                cmd[33] = (byte)(0 & 0xFF);

                // Start Position
                int startX = (modDx / 2 - Settings.Ins.Camera.TrimmingSize / 2);
                int startY = (modDy / 2 - Settings.Ins.Camera.TrimmingSize / 2);

                cmd[34] = (byte)(startX >> 8);
                cmd[35] = (byte)(startX & 0xFF);
                cmd[36] = (byte)(startY >> 8);
                cmd[37] = (byte)(startY & 0xFF);

                // H, V Width
                cmd[38] = (byte)(Settings.Ins.Camera.TrimmingSize >> 8);
                cmd[39] = (byte)(Settings.Ins.Camera.TrimmingSize & 0xFF);
                cmd[40] = (byte)(Settings.Ins.Camera.TrimmingSize >> 8);
                cmd[41] = (byte)(Settings.Ins.Camera.TrimmingSize & 0xFF);

                // H, V Pitch
                cmd[42] = (byte)(modDx >> 8);
                cmd[43] = (byte)(modDx & 0xFF);
                cmd[44] = (byte)(modDy >> 8);
                cmd[45] = (byte)(modDy & 0xFF);
            }
            catch { } // 無視する

            // RGB
            cmd[21] = 0;
            cmd[21] += 0x40;
            cmd[21] += 0x20;
            cmd[21] += 0x10;

            cmd[21] += 0x01; // Hatch     
        }

        private void outputTrimArea(UfCamCorrectPos pos)
        {
            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            setTrimAreaCommand(ref cmd, pos);

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(cmd, 0, cont.IPAddress); }

            Thread.Sleep(Settings.Ins.Camera.PatternWait);
        }

        private void setTrimAreaCommand(ref Byte[] cmd, UfCamCorrectPos pos)
        {
            try
            {
                // Foreground Color
                cmd[22] = (byte)(IntSigLvMax >> 8); // Red
                cmd[23] = (byte)(IntSigLvMax & 0xFF);
                cmd[24] = (byte)(IntSigLvMax >> 8); // Green
                cmd[25] = (byte)(IntSigLvMax & 0xFF);
                cmd[26] = (byte)(IntSigLvMax >> 8); // Blue
                cmd[27] = (byte)(IntSigLvMax & 0xFF);

                // Background Color
                cmd[28] = (byte)(0 >> 8); ; // Red
                cmd[29] = (byte)(0 & 0xFF);
                cmd[30] = (byte)(0 >> 8); ; // Green
                cmd[31] = (byte)(0 & 0xFF);
                cmd[32] = (byte)(0 >> 8); ; // Blue
                cmd[33] = (byte)(0 & 0xFF);

                // Start Position
                int startX = 0, startY = 0;
                int pitchX = 0;

                if (pos == UfCamCorrectPos.Cabinet_Top)
                {
                    startX = cabiDx / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    startY = 0;
                    pitchX = cabiDx;
                }
                else if (pos == UfCamCorrectPos.Cabinet_Bottom)
                {
                    startX = cabiDx / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    startY = cabiDy - Settings.Ins.Camera.TrimmingSize;
                    pitchX = cabiDx;
                }
                else if (pos == UfCamCorrectPos.Cabinet_Left)
                {
                    startX = 0;
                    startY = cabiDy / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    pitchX = cabiDx;
                }
                else if (pos == UfCamCorrectPos.Cabinet_Right)
                {
                    startX = cabiDx - Settings.Ins.Camera.TrimmingSize;
                    startY = cabiDy / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    pitchX = cabiDx;
                }
                else if (pos == UfCamCorrectPos.Radiator_L_Top || pos == UfCamCorrectPos.Radiator_R_Top)
                {
                    startX = cabiDx / 4 - Settings.Ins.Camera.TrimmingSize / 2;
                    startY = 0;
                    pitchX = cabiDx / 2;
                }
                else if (pos == UfCamCorrectPos.Radiator_L_Bottom || pos == UfCamCorrectPos.Radiator_R_Bottom)
                {
                    startX = cabiDx / 4 - Settings.Ins.Camera.TrimmingSize / 2;
                    startY = cabiDy - Settings.Ins.Camera.TrimmingSize;
                    pitchX = cabiDx / 2;
                }
                else if (pos == UfCamCorrectPos.Radiator_L_Left || pos == UfCamCorrectPos.Radiator_R_Left)
                {
                    startX = 0;
                    startY = cabiDy / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    pitchX = cabiDx / 2;
                }
                else if (pos == UfCamCorrectPos.Radiator_L_Right || pos == UfCamCorrectPos.Radiator_R_Right)
                {
                    startX = cabiDx / 2 - Settings.Ins.Camera.TrimmingSize;
                    startY = cabiDy / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    pitchX = cabiDx / 2;
                }
                else if (pos == UfCamCorrectPos.Module_0)
                {
                    startX = modDx / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    startY = modDy / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    pitchX = cabiDx;
                }
                else if (pos == UfCamCorrectPos.Module_1)
                {
                    startX = modDx + modDx / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    startY = modDy / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    pitchX = cabiDx;
                }
                else if (pos == UfCamCorrectPos.Module_2)
                {
                    startX = modDx * 2 + modDx / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    startY = modDy / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    pitchX = cabiDx;
                }
                else if (pos == UfCamCorrectPos.Module_3)
                {
                    startX = modDx * 3 + modDx / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    startY = modDy / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    pitchX = cabiDx;
                }
                else if (pos == UfCamCorrectPos.Module_4)
                {
                    startX = modDx / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    startY = modDy + modDy / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    pitchX = cabiDx;
                }
                else if (pos == UfCamCorrectPos.Module_5)
                {
                    startX = modDx + modDx / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    startY = modDy + modDy / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    pitchX = cabiDx;
                }
                else if (pos == UfCamCorrectPos.Module_6)
                {
                    startX = modDx * 2 + modDx / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    startY = modDy + modDy / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    pitchX = cabiDx;
                }
                else if (pos == UfCamCorrectPos.Module_7)
                {
                    startX = modDx * 3 + modDx / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    startY = modDy + modDy / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    pitchX = cabiDx;
                }
                else if (pos == UfCamCorrectPos.Module_8)
                {
                    startX = modDx / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    startY = modDy * 2 + modDy / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    pitchX = cabiDx;
                }
                else if (pos == UfCamCorrectPos.Module_9)
                {
                    startX = modDx + modDx / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    startY = modDy * 2 + modDy / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    pitchX = cabiDx;
                }
                else if (pos == UfCamCorrectPos.Module_10)
                {
                    startX = modDx * 2 + modDx / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    startY = modDy * 2 + modDy / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    pitchX = cabiDx;
                }
                else if (pos == UfCamCorrectPos.Module_11)
                {
                    startX = modDx * 3 + modDx / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    startY = modDy * 2 + modDy / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    pitchX = cabiDx;
                }
                else if (pos == UfCamCorrectPos._9pt_TopLeft)
                {
                    startX = 0;
                    startY = 0;
                    pitchX = cabiDx;
                }
                else if (pos == UfCamCorrectPos._9pt_TopRight)
                {
                    startX = cabiDx - Settings.Ins.Camera.TrimmingSize;
                    startY = 0;
                    pitchX = cabiDx;
                }
                else if (pos == UfCamCorrectPos._9pt_Center)
                {
                    startX = cabiDx / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    startY = cabiDy / 2 - Settings.Ins.Camera.TrimmingSize / 2;
                    pitchX = cabiDx;
                }
                else if (pos == UfCamCorrectPos._9pt_BottomLeft)
                {
                    startX = 0;
                    startY = cabiDy - Settings.Ins.Camera.TrimmingSize;
                    pitchX = cabiDx;
                }
                else if (pos == UfCamCorrectPos._9pt_BottomRight)
                {
                    startX = cabiDx - Settings.Ins.Camera.TrimmingSize;
                    startY = cabiDy - Settings.Ins.Camera.TrimmingSize;
                    pitchX = cabiDx;
                }


                cmd[34] = (byte)(startX >> 8);
                cmd[35] = (byte)(startX & 0xFF);
                cmd[36] = (byte)(startY >> 8);
                cmd[37] = (byte)(startY & 0xFF);

                // H, V Width
                cmd[38] = (byte)(Settings.Ins.Camera.TrimmingSize >> 8);
                cmd[39] = (byte)(Settings.Ins.Camera.TrimmingSize & 0xFF);
                cmd[40] = (byte)(Settings.Ins.Camera.TrimmingSize >> 8);
                cmd[41] = (byte)(Settings.Ins.Camera.TrimmingSize & 0xFF);

                // H, V Pitch
                cmd[42] = (byte)(pitchX >> 8);
                cmd[43] = (byte)(pitchX & 0xFF);
                cmd[44] = (byte)(cabiDy >> 8);
                cmd[45] = (byte)(cabiDy & 0xFF);
            }
            catch { } // 無視する

            // RGB
            cmd[21] = 0;
            cmd[21] += 0x40;
            cmd[21] += 0x20;
            cmd[21] += 0x10;

            cmd[21] += 0x01; // Hatch     
        }

        private void outputFlatPattern(int r, int g, int b)
        {
            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            setFlatCommand(ref cmd, r, g, b);

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(cmd, 0, cont.IPAddress); }

            Thread.Sleep(Settings.Ins.Camera.PatternWait);
        }

        private void setFlatCommand(ref Byte[] cmd, int r, int g, int b)
        {
            try
            {
                // Foreground Color
                cmd[22] = (byte)(r >> 8); // Red
                cmd[23] = (byte)(r & 0xFF);
                cmd[24] = (byte)(g >> 8); // Green
                cmd[25] = (byte)(g & 0xFF);
                cmd[26] = (byte)(b >> 8); // Blue
                cmd[27] = (byte)(b & 0xFF);

                // Background Color
                cmd[28] = (byte)(0 >> 8); ; // Red
                cmd[29] = (byte)(0 & 0xFF);
                cmd[30] = (byte)(0 >> 8); ; // Green
                cmd[31] = (byte)(0 & 0xFF);
                cmd[32] = (byte)(0 >> 8); ; // Blue
                cmd[33] = (byte)(0 & 0xFF);

                cmd[34] = (byte)(0 >> 8);
                cmd[35] = (byte)(0 & 0xFF);
                cmd[36] = (byte)(0 >> 8);
                cmd[37] = (byte)(0 & 0xFF);

                // H, V Width
                cmd[38] = (byte)(0 >> 8);
                cmd[39] = (byte)(0 & 0xFF);
                cmd[40] = (byte)(0 >> 8);
                cmd[41] = (byte)(0 & 0xFF);

                // H, V Pitch
                cmd[42] = (byte)(0 >> 8);
                cmd[43] = (byte)(0 & 0xFF);
                cmd[44] = (byte)(0 >> 8);
                cmd[45] = (byte)(0 & 0xFF);
            }
            catch { } // 無視する

            // RGB
            cmd[21] = 0;
            cmd[21] += 0x40;
            cmd[21] += 0x20;
            cmd[21] += 0x10;

            cmd[21] += 0x00; // Hatch            
        }

        private void OutputTargetArea(List<UnitInfo> lstTgtUnits, bool isGreen = false)
        {
            if (isGreen == true)
            { OutputTargetArea(lstTgtUnits, 0, m_MeasureLevel, 0); }
            else
            { OutputTargetArea(lstTgtUnits, m_MeasureLevel, m_MeasureLevel, m_MeasureLevel); }
        }
        
        private void OutputTargetArea(List<UnitInfo> lstTgtUnits, int r, int g, int b)
        {
            // コントローラ毎に出力範囲を決定
            foreach (ControllerInfo cont in dicController.Values)
            {
                int startX = int.MaxValue, startY = int.MaxValue, endX = 0, endY = 0;
                int height = 0, width = 0;

                // added by Hotta 2022/11/10
                bool flag = false;
                //
                foreach (UnitInfo unit in lstTgtUnits)
                {
                    if (unit.ControllerID == cont.ControllerID)
                    {
                        if (unit.PixelX < startX)
                        { startX = unit.PixelX; }

                        if (unit.PixelY < startY)
                        { startY = unit.PixelY; }

                        if (unit.PixelX > endX)
                        { endX = unit.PixelX; }

                        if (unit.PixelY > endY)
                        { endY = unit.PixelY; }

                        // added by Hotta 2022/11/10
                        flag = true;
                        //
                    }
                }

                // modified by Hotta 2022/11/10
                /*
                height = endY - startY + cabiDy;
                width = endX - startX + cabiDx;

                Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
                Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

                if (isGreen == true)
                { setIntSigWindowCommand(ref cmd, startX, startY, height, width, 0, m_MeasureLevel, 0); }
                else
                { setIntSigWindowCommand(ref cmd, startX, startY, height, width, m_MeasureLevel, m_MeasureLevel, m_MeasureLevel); }

                sendSdcpCommand(cmd, 0, cont.IPAddress);
                */
                if (flag == true)
                {
                    height = endY - startY + cabiDy;
                    width = endX - startX + cabiDx;

                    Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
                    Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

                    setIntSigWindowCommand(ref cmd, startX, startY, height, width, r, g, b);
                    
                    sendSdcpCommand(cmd, 0, cont.IPAddress);

                    // added by Hotta 2022/12/13 for 日産テスト
                    /*
                    using (StreamWriter sw = new StreamWriter(applicationPath + "\\Temp\\Output.txt", true))
                    {
                        sw.WriteLine("Controller ID : " + cont.ControllerID.ToString());
                        sw.WriteLine("startX : " + startX.ToString());
                        sw.WriteLine("startY : " + startY.ToString());
                        sw.WriteLine("width : " + width.ToString());
                        sw.WriteLine("height : " + height.ToString());
                        sw.WriteLine("");
                    }
                    */
                    //

                }
                else
                {
                    Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
                    Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);
                    setFlatCommand(ref cmd, 0, 0, 0);
                    sendSdcpCommand(cmd, 0, cont.IPAddress);
                }
            }

            Thread.Sleep(Settings.Ins.IntSignalWait);
        }

        #endregion Pattern

        #region Cabinet Position

        /// <summary>
        /// 選択範囲の中央にカメラが来るように全Cabinetの空間座標を設定します。
        /// </summary>
        /// <param name="lstTgtUnits">調整対象Cabinet</param>
        /// <param name="dist">撮影距離</param>
        private void SetCabinetPos(List<UnitInfo> lstTgtUnits, double dist)
        {
            SetCabinetPos(lstTgtUnits, dist, double.NaN, double.NaN);
        }

        /// <summary>
        /// カメラの光学中心を基準として全Cabinetの空間座標を設定します。
        /// </summary>
        /// <param name="lstTgtUnits">調整対象Cabinet</param>
        /// <param name="dist">撮影距離[mm]</param>
        /// <param name="wallH">Wall下端高さ[mm]</param>
        /// <param name="camH">カメラ中心高さ[mm]</param>
        private void SetCabinetPos(List<UnitInfo> lstTgtUnits, double dist, double wallH, double camH)
        {
            bool isRound = false; // Round形状かどうか
            double[] rotateAngle = null;

            CalcRelativePosition(lstTgtUnits, dist, wallH, camH, out double pan, out double tilt, out double x, out double y);

            // Wall形状がCurveに設定されている場合はLoad
            bool? IsCurveChecked = false;
            Dispatcher.Invoke(new Action(() => { IsCurveChecked = rbConfigWallFormCurve.IsChecked; }));            
            if (IsCurveChecked == true)
            {
                try { LoadWallFormFile(out rotateAngle); }
                catch(Exception ex) { throw new Exception("Failed to load wall form file.\r\n" + ex.Message); }
                isRound = true;
            }

            // Cabinetの有効エリアを走査
            int actMaxX = 0, actMaxY = 0;
            int lastCy = -1; // 最後にCabinetが存在した行、有効行カウント用
            for(int cy = 0; cy < allocInfo.MaxY; cy++)
            {
                int cntX = 0;
                for(int cx = 0; cx < allocInfo.MaxX; cx++)
                {
                    if (allocInfo.lstUnits[cx][cy] != null)
                    {
                        cntX++;

                        if (cy != lastCy)
                        {
                            actMaxY++;
                            lastCy = cy;
                        }
                    }
                }

                if(cntX > actMaxX)
                { actMaxX = cntX; }
            }

            // 原点（左下）のCabinetの座標を設定
            // いったんBlankを除外した実際のCabinet数分の配列に座標を格納する(Blank、離れ小島対策)
            CabinetCoordinate[][] tempCabiPos = new CabinetCoordinate[actMaxX][];
            for (int cx = 0; cx < actMaxX; cx++)
            {
                tempCabiPos[cx] = new CabinetCoordinate[actMaxY];

                for (int cy = 0; cy < actMaxY; cy++)
                { tempCabiPos[cx][cy] = new CabinetCoordinate(); }
            }

            // Bottom-Left(原点)
            tempCabiPos[0][actMaxY - 1].BottomLeft.x = 0;
            tempCabiPos[0][actMaxY - 1].BottomLeft.y = 0;
            tempCabiPos[0][actMaxY - 1].BottomLeft.z = dist;

            // Top-Left
            tempCabiPos[0][actMaxY - 1].TopLeft.x = 0;
            tempCabiPos[0][actMaxY - 1].TopLeft.y = cabinetSizeV;
            tempCabiPos[0][actMaxY - 1].TopLeft.z = dist;

            // Bottom-Right
            tempCabiPos[0][actMaxY - 1].BottomRight.x = cabinetSizeH;
            tempCabiPos[0][actMaxY - 1].BottomRight.y = 0;
            tempCabiPos[0][actMaxY - 1].BottomRight.z = dist;

            // 最下段のCabinetに座標を設定
            double deg = 0; // 累積回転角
            for (int cx = 1; cx <= actMaxX; cx++) // 1番目のCabinetは上で設定済みなので2番目から
            {
                double dx = cabinetSizeH * Math.Cos(ToRadian(deg));
                double dz = cabinetSizeH * Math.Sin(ToRadian(deg));

                if (cx < actMaxX)
                {
                    // Bottom-Left
                    tempCabiPos[cx][actMaxY - 1].BottomLeft.x = tempCabiPos[cx - 1][actMaxY - 1].BottomLeft.x + dx;
                    tempCabiPos[cx][actMaxY - 1].BottomLeft.y = tempCabiPos[cx - 1][actMaxY - 1].BottomLeft.y;
                    tempCabiPos[cx][actMaxY - 1].BottomLeft.z = tempCabiPos[cx - 1][actMaxY - 1].BottomLeft.z + dz;

                    // Top-Left
                    tempCabiPos[cx][actMaxY - 1].TopLeft.x = tempCabiPos[cx][actMaxY - 1].BottomLeft.x;
                    tempCabiPos[cx][actMaxY - 1].TopLeft.y = tempCabiPos[cx][actMaxY - 1].BottomLeft.y + cabinetSizeV;
                    tempCabiPos[cx][actMaxY - 1].TopLeft.z = tempCabiPos[cx][actMaxY - 1].BottomLeft.z;
                }

                // Bottom-Right
                tempCabiPos[cx - 1][actMaxY - 1].BottomRight.x = tempCabiPos[cx - 1][actMaxY - 1].BottomLeft.x + dx;
                tempCabiPos[cx - 1][actMaxY - 1].BottomRight.y = tempCabiPos[cx - 1][actMaxY - 1].BottomLeft.y;
                tempCabiPos[cx - 1][actMaxY - 1].BottomRight.z = tempCabiPos[cx - 1][actMaxY - 1].BottomLeft.z + dz;

                if (isRound == true && (cx - 1) < rotateAngle.Length)
                { deg -= rotateAngle[cx - 1]; }
            }

            // 各Cabinetに座標を設定
            for (int cy = actMaxY - 2; cy >= 0; cy--)
            {
                for(int cx = 0; cx < actMaxX; cx++)
                {
                    // Bottom-Left
                    tempCabiPos[cx][cy].BottomLeft.x = tempCabiPos[cx][cy + 1].BottomLeft.x;
                    tempCabiPos[cx][cy].BottomLeft.y = tempCabiPos[cx][cy + 1].BottomLeft.y + cabinetSizeV;
                    tempCabiPos[cx][cy].BottomLeft.z = tempCabiPos[cx][cy + 1].BottomLeft.z;

                    // Top-Left
                    tempCabiPos[cx][cy].TopLeft.x = tempCabiPos[cx][cy].BottomLeft.x;
                    tempCabiPos[cx][cy].TopLeft.y = tempCabiPos[cx][cy].BottomLeft.y + cabinetSizeV;
                    tempCabiPos[cx][cy].TopLeft.z = tempCabiPos[cx][cy].BottomLeft.z;

                    // Bottom-Right
                    tempCabiPos[cx][cy].BottomRight.x = tempCabiPos[cx][cy + 1].BottomRight.x;
                    tempCabiPos[cx][cy].BottomRight.y = tempCabiPos[cx][cy + 1].BottomRight.y + cabinetSizeV;
                    tempCabiPos[cx][cy].BottomRight.z = tempCabiPos[cx][cy + 1].BottomRight.z;
                }
            }

            // allocInfoにコピー
            int curY = -1, lastY = -1;
            for (int cy = 0; cy < allocInfo.MaxY; cy++)
            {
                int curX = 0;
                for (int cx = 0; cx < allocInfo.MaxX; cx++)
                {
                    if (allocInfo.lstUnits[cx][cy] != null)
                    {
                        if (cy != lastY)
                        {
                            curY++;
                            lastY = cy;
                        }

                        allocInfo.lstUnits[cx][cy].CabinetPos = tempCabiPos[curX][curY];
                        curX++;
                    }
                }
            }

            // 指定の位置へ移動
            MoveCabinetPosSelectedCenter(lstTgtUnits, dist); // とりあえず選択範囲の中心がカメラ光学中心に来るように移動            
            MoveCabinetPos(pan, -tilt, 0, x, y, 0); // 選択範囲がカメラ中央に来るようにTilt角を調整する。（実際にはTiltだけ）            
        }

        /// <summary>
        /// カメラと選択されたCabinet中心との相対位置を計算する。
        /// 距離は指定なのでzの出力はなし、意図的に回転させる必要もないのでRollの出力もなし。
        /// </summary>
        /// <param name="lstTgtUnits">選択されたCabinet</param>
        /// <param name="dist">カメラ距離 Wall-Camera Distance</param>
        /// <param name="wallH">Wall下端高さ Wall Bottom Height</param>
        /// <param name="camH">カメラ高さ Cam Height</param>
        /// <param name="pan">Panの回転角 今のところカメラを左右に偏らせて撮影することは想定していないので0</param>
        /// <param name="tilt">Tiltの回転角</param>
        /// <param name="x">xの変位量 今のところカメラを左右に偏らせて撮影することは想定していないので0</param>
        /// <param name="y">yの変位量</param>
        private void CalcRelativePosition(List<UnitInfo> lstTgtUnits, double dist, double wallH, double camH, out double pan, out double tilt, out double x, out double y)
        {
            pan = 0; // とりあえず0
            x = 0; // とりあえず0

            // 設定されていない場合は選択範囲の中央
            if (double.IsNaN(wallH) == true || double.IsNaN(camH) == true)
            {
                tilt = 0;
                y = 0;
                return;
            }

            int maxV = 0, minV = int.MaxValue;
            foreach (UnitInfo cabi in lstTgtUnits)
            {
                if (cabi.Y < minV)
                { minV = cabi.Y; }

                if (cabi.Y > maxV)
                { maxV = cabi.Y; }
            }

            // 選択Cabinetの段数
            int cabiV = maxV - minV + 1;

            // 選択範囲の下にあるCabinet段数
            int unsel = allocInfo.MaxY - maxV;

            // Wallの高さ
            double wallV = cabinetSizeV * cabiV;

            y = (wallH - camH) + (wallV / 2) + (cabinetSizeV * unsel);
            tilt = ToDegree(Math.Atan(y / dist));
        }

        private void LoadWallFormFile(out double[] rotateAngle)
        {
            rotateAngle = null;

            string text = "";

            try
            {
                using (StreamReader sr = new StreamReader(Settings.Ins.WallFormFile))
                { text = sr.ReadToEnd(); }
            }
            catch { throw new Exception("File could not be opened. Please check the file path."); }

            try
            {
                string[] lines = text.Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                List<double> lstRotAngle = new List<double>();

                for (int i = 0; i < lines.Length; i++)
                {
                    string[] parts = lines[i].Replace(",", ".").Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length >= 3 && string.IsNullOrWhiteSpace(parts[2]) != true)
                    { lstRotAngle.Add(double.Parse(parts[2])); }
                    else
                    { break; }
                }

                rotateAngle = new double[lstRotAngle.Count];

                int cur = 0;
                foreach (double angle in lstRotAngle)
                {
                    rotateAngle[cur] = lstRotAngle[cur];
                    cur++;
                }
            }
            catch { throw new Exception("Failed to process the described content. Please check the format."); }

            // 有効Cabient(Blankじゃないやつ)エリアの幅を走査
            int validWidth = 0;
            for (int y = 0; y < allocInfo.MaxY; y++)
            {
                int count = 0;
                for(int x = 0; x < allocInfo.MaxX; x++)
                {
                    if(allocInfo.lstUnits[x][y] != null)
                    { count++; }
                }

                if(count > validWidth)
                { validWidth = count; }
            }

            // ProfileとWall Formファイルの設定内容に整合性がない場合、例外をスロー
            if (rotateAngle.Length != validWidth - 1)
            { throw new Exception("Wall form file content does not match selected profile."); }
        }

        /// <summary>
        /// 選択されたCabinetの中心にカメラが来るように全Cabinetの空間座標を再設定します。
        /// </summary>
        private void MoveCabinetPosSelectedCenter(List<UnitInfo> lstTgtUnits, double z)
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = 0, maxY = 0;
            double centerX = 0, centerY = 0, centerDz = 0, pan = 0, tilt = 0;
            CameraDataClass.CabinetCoordinate minXsc = new CabinetCoordinate(), maxXsc = new CabinetCoordinate(), minYsc = new CabinetCoordinate(), maxYsc = new CabinetCoordinate();

            // 選択されているCabinetのX, Y最大値を捜査する
            foreach(UnitInfo unit in lstTgtUnits)
            {
                if (unit.X < minX)
                {
                    minX = unit.X;
                    minXsc = unit.CabinetPos;
                }

                if (unit.Y < minY)
                {
                    minY = unit.Y;
                    minYsc = unit.CabinetPos;
                }

                if (unit.X > maxX)
                {
                    maxX = unit.X;
                    maxXsc = unit.CabinetPos;
                }

                if (unit.Y > maxY)
                {
                    maxY = unit.Y;
                    maxYsc = unit.CabinetPos;
                }
            }

            int lenX = (maxX - minX) + 1;
            int lenY = (maxY - minY) + 1;

            double dx, dy, dz;

            // Pan
            if (lenX % 2 == 0) // 横方向が偶数
            {
                // 選択範囲の左右端の座標から回転角を求める
                dx = maxXsc.BottomRight.x - minXsc.BottomLeft.x;
                dz = maxXsc.BottomRight.z - minXsc.BottomLeft.z;
            }
            else // 奇数
            {
                // 中心Cabinetの左右端の座標から回転角を求める
                int ux = (maxX + minX) / 2;
                UnitInfo unit = searchUnit(lstTgtUnits, ux, minY);

                dx = unit.CabinetPos.BottomRight.x - unit.CabinetPos.BottomLeft.x;
                dz = unit.CabinetPos.BottomRight.z - unit.CabinetPos.BottomLeft.z;
            }

            // modified by Hotta 2023/01/12
            /*
            pan = ToDegree(Math.Atan(dz / dx));
            */
            if (dx > 0 && dz > 0)
                pan = ToDegree(Math.Atan(Math.Abs(dz / dx)));
            else if (dx < 0 && dz > 0)
                pan = 180 - ToDegree(Math.Atan(Math.Abs(dz / dx)));
            else if (dx < 0 && dz < 0)
                pan = 180 + ToDegree(Math.Atan(Math.Abs(dz / dx)));
            else
                pan = -ToDegree(Math.Atan(Math.Abs(dz / dx)));
            //

            // Tilt
            if (lenY % 2 == 0) // 縦方向が偶数
            {
                // 選択範囲の上下端の座標から回転角を求める
                dz = maxYsc.TopLeft.z - minYsc.BottomLeft.z;
                dy = minYsc.TopLeft.y - maxYsc.BottomLeft.y;
            }
            else // 奇数
            {
                // 中心Cabinetの上下端の座標から回転角を求める
                int uy = (maxY + minY) / 2;
                UnitInfo unit = searchUnit(lstTgtUnits, minX, uy);

                dz = unit.CabinetPos.TopLeft.z - unit.CabinetPos.BottomLeft.z;
                dy = unit.CabinetPos.TopLeft.y - unit.CabinetPos.BottomLeft.y;
            }

            tilt = 90 - ToDegree(Math.Atan(dy / dz)); // Tilt角は90°(直交)を目標とするので90°との差分を計算する。
            MoveCabinetPos(-pan, -tilt, 0, 0, 0, 0);

            // 直交座標
            // x
            if (lenX % 2 == 0) // 横方向が偶数
            {
                // 選択範囲の左右端の座標から中心座標を求める
                centerX = (maxXsc.BottomRight.x + minXsc.BottomLeft.x) / 2;
            }
            else // 奇数
            {
                // 中心Cabinetの左右端の座標から中心座標を求める
                int ux = (maxX + minX) / 2;
                UnitInfo unit = searchUnit(lstTgtUnits, ux, minY);

                centerX = (unit.CabinetPos.BottomRight.x + unit.CabinetPos.BottomLeft.x) / 2;
            }

            // y
            if (lenY % 2 == 0) // 縦方向が偶数
            {
                // 選択範囲の上下端の座標から中心座標を求める
                centerY = (minYsc.TopLeft.y + maxYsc.BottomLeft.y) / 2;
            }
            else // 奇数
            {
                // 中心Cabinetの上下端の座標から中心座標を求める
                int uy = (maxY + minY) / 2;
                UnitInfo unit = searchUnit(lstTgtUnits, minX, uy);

                centerY = (unit.CabinetPos.TopLeft.y + unit.CabinetPos.BottomLeft.y) / 2;
            }

            // z
            if (lenX % 2 == 0) // 横方向が偶数
            {
                // 中心Cabinet(中心はCabinetの間にくる)の座標から距離の変位量を求める
                int ux = (maxX + minX) / 2; // 切り捨てで中心の左側のCabiが選ばれるはず
                UnitInfo unit = searchUnit(lstTgtUnits, ux, minY);

                centerDz = z - unit.CabinetPos.BottomRight.z; // 右端が中心
            }
            else // 奇数
            {
                // 中心Cabinetのz座標から距離の変位量を求める
                int ux = (maxX + minX) / 2;
                UnitInfo unit = searchUnit(lstTgtUnits, ux, minY);

                centerDz = z - unit.CabinetPos.BottomLeft.z; // 中心CabinetはZ軸と直交している
            }

            MoveCabinetPos(0, 0, 0, -centerX, -centerY, centerDz);
        }

        /// <summary>
        /// 全Cabinetの空間座標を引数分移動させて再設定します。
        /// </summary>
        /// <param name="pan">Pan回転角[deg] Wallを左方向(カメラが右に回転)に回転がプラス</param>
        /// <param name="tilt">Tilt回転角[deg] Wallを上方向(カメラが下に回転)に回転がプラス</param>
        /// <param name="roll">Roll回転角[deg] Wallを反時計回り(カメラが時計回り)に回転がプラス</param>
        /// <param name="dx">横方向移動量[mm] 右方向がプラス</param>
        /// <param name="dy">縦方向移動量[mm] 上方向がプラス</param>
        /// <param name="dz">奥行方向移動量[mm] Wallが遠ざかる方向がプラス</param>
        private void MoveCabinetPos(double pan, double tilt, double roll, double dx, double dy, double dz)
        {
            for(int cy = 0; cy < allocInfo.MaxY; cy++)
            {
                for(int cx = 0; cx < allocInfo.MaxX; cx++)
                {
                    if(allocInfo.lstUnits[cx][cy] == null)
                    { continue; }

                    #region 平行移動

                    // Bottom-Left
                    allocInfo.lstUnits[cx][cy].CabinetPos.BottomLeft.x += dx;
                    allocInfo.lstUnits[cx][cy].CabinetPos.BottomLeft.y += dy;
                    allocInfo.lstUnits[cx][cy].CabinetPos.BottomLeft.z += dz;

                    // Top-Left
                    allocInfo.lstUnits[cx][cy].CabinetPos.TopLeft.x += dx;
                    allocInfo.lstUnits[cx][cy].CabinetPos.TopLeft.y += dy;
                    allocInfo.lstUnits[cx][cy].CabinetPos.TopLeft.z += dz;

                    // Bottom-Right
                    allocInfo.lstUnits[cx][cy].CabinetPos.BottomRight.x += dx;
                    allocInfo.lstUnits[cx][cy].CabinetPos.BottomRight.y += dy;
                    allocInfo.lstUnits[cx][cy].CabinetPos.BottomRight.z += dz;

                    #endregion 平行移動

                    #region 回転移動

                    // Pan(xz平面で回転)
                    // Bottom-Left
                    double x = allocInfo.lstUnits[cx][cy].CabinetPos.BottomLeft.x;
                    double z = allocInfo.lstUnits[cx][cy].CabinetPos.BottomLeft.z;
                    RotateCoordinate(x, z, pan, out double xd, out double zd);

                    allocInfo.lstUnits[cx][cy].CabinetPos.BottomLeft.x = xd;
                    allocInfo.lstUnits[cx][cy].CabinetPos.BottomLeft.z = zd;

                    // Top-Left
                    x = allocInfo.lstUnits[cx][cy].CabinetPos.TopLeft.x;
                    z = allocInfo.lstUnits[cx][cy].CabinetPos.TopLeft.z;
                    RotateCoordinate(x, z, pan, out xd, out zd);

                    allocInfo.lstUnits[cx][cy].CabinetPos.TopLeft.x = xd;
                    allocInfo.lstUnits[cx][cy].CabinetPos.TopLeft.z = zd;

                    // Bottom-Right
                    x = allocInfo.lstUnits[cx][cy].CabinetPos.BottomRight.x;
                    z = allocInfo.lstUnits[cx][cy].CabinetPos.BottomRight.z;
                    RotateCoordinate(x, z, pan, out xd, out zd);

                    allocInfo.lstUnits[cx][cy].CabinetPos.BottomRight.x = xd;
                    allocInfo.lstUnits[cx][cy].CabinetPos.BottomRight.z = zd;

                    // Tilt(yz平面で回転)
                    // Bottom-Left
                    double y = allocInfo.lstUnits[cx][cy].CabinetPos.BottomLeft.y;
                    z = allocInfo.lstUnits[cx][cy].CabinetPos.BottomLeft.z;
                    RotateCoordinate(z, y, tilt, out zd, out double yd);

                    allocInfo.lstUnits[cx][cy].CabinetPos.BottomLeft.y = yd;
                    allocInfo.lstUnits[cx][cy].CabinetPos.BottomLeft.z = zd;

                    // Top-Left
                    y = allocInfo.lstUnits[cx][cy].CabinetPos.TopLeft.y;
                    z = allocInfo.lstUnits[cx][cy].CabinetPos.TopLeft.z;
                    RotateCoordinate(z, y, tilt, out zd, out yd);

                    allocInfo.lstUnits[cx][cy].CabinetPos.TopLeft.y = yd;
                    allocInfo.lstUnits[cx][cy].CabinetPos.TopLeft.z = zd;

                    // Bottom-Right
                    y = allocInfo.lstUnits[cx][cy].CabinetPos.BottomRight.y;
                    z = allocInfo.lstUnits[cx][cy].CabinetPos.BottomRight.z;
                    RotateCoordinate(z, y, tilt, out zd, out yd);

                    allocInfo.lstUnits[cx][cy].CabinetPos.BottomRight.y = yd;
                    allocInfo.lstUnits[cx][cy].CabinetPos.BottomRight.z = zd;

                    // Roll(xy平面で回転)
                    // Bottom-Left
                    x = allocInfo.lstUnits[cx][cy].CabinetPos.BottomLeft.x;
                    y = allocInfo.lstUnits[cx][cy].CabinetPos.BottomLeft.y;
                    RotateCoordinate(x, y, roll, out xd, out yd);

                    allocInfo.lstUnits[cx][cy].CabinetPos.BottomLeft.x = xd;
                    allocInfo.lstUnits[cx][cy].CabinetPos.BottomLeft.y = yd;

                    // Top-Left
                    x = allocInfo.lstUnits[cx][cy].CabinetPos.TopLeft.x;
                    y = allocInfo.lstUnits[cx][cy].CabinetPos.TopLeft.y;
                    RotateCoordinate(x, y, roll, out xd, out yd);

                    allocInfo.lstUnits[cx][cy].CabinetPos.TopLeft.x = xd;
                    allocInfo.lstUnits[cx][cy].CabinetPos.TopLeft.y = yd;

                    // Bottom-Right
                    x = allocInfo.lstUnits[cx][cy].CabinetPos.BottomRight.x;
                    y = allocInfo.lstUnits[cx][cy].CabinetPos.BottomRight.y;
                    RotateCoordinate(x, y, roll, out xd, out yd);

                    allocInfo.lstUnits[cx][cy].CabinetPos.BottomRight.x = xd;
                    allocInfo.lstUnits[cx][cy].CabinetPos.BottomRight.y = yd;

                    #endregion  回転移動

                    // 各補正点のPan, Tilt角を再計算して格納する
                    allocInfo.lstUnits[cx][cy].lstCpAngle = CalcCpAngle(allocInfo.lstUnits[cx][cy]);
                }
            }
        }

        private void RotateCoordinate(double x, double y, double deg, out double xd, out double yd)
        {
            // x′= xcos(θ)−ysin(θ)
            // y′= xsin(θ) + ycos(θ)
            xd = x * Math.Cos(ToRadian(deg)) - y * Math.Sin(ToRadian(deg));
            yd = x * Math.Sin(ToRadian(deg)) + y * Math.Cos(ToRadian(deg));
        }

        #endregion Cabinet Position

        #endregion Common

        #region Set Position

        /// <summary>
        /// ユーザーが設定した撮影距離が仕様上の撮影距離限界を超えていないかチェック
        /// </summary>
        /// <param name="dist">撮影距離</param>
        private void CheckShootingDist(double dist)
        {
            double distFarLimit, distNearLimit;
            // Pitch 1.2
            if (allocInfo.LEDModel == ZRD_C12A 
                || allocInfo.LEDModel == ZRD_B12A 
                || allocInfo.LEDModel == ZRD_CH12D 
                || allocInfo.LEDModel == ZRD_BH12D
                || allocInfo.LEDModel == ZRD_CH12D_S3
                || allocInfo.LEDModel == ZRD_BH12D_S3) // Cancun
            {
#if (DEBUG || Release_log)
                if (Settings.Ins.ExecLog == true)
                {
                    MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                }
#endif
                distNearLimit = ShootingDistNearLimit_P12;
                distFarLimit = ShootingDistFarLimit_P12;
            }
            else // 1.5
            {
#if (DEBUG || Release_log)
                if (Settings.Ins.ExecLog == true)
                {
                    MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                }
#endif
                distNearLimit = ShootingDistNearLimit_P15;
                distFarLimit = ShootingDistFarLimit_P15;
            }

            if(dist < distNearLimit || distFarLimit < dist)
            { throw new Exception("The shooting distance exceeds the UF adjustable distance."); }
        }


        private void AdjustCameraPosUf(System.Windows.Forms.Timer timer, System.Windows.Controls.Image img, ToggleButton tbtn)
        {
            bool status = true;

            timer.Enabled = false;

            // カメラ推定位置を格納
            CvBlob[,] aryBlob;
            CameraPosition camPos;

            try
            { status = GetCameraPosUf(img, out aryBlob, out camPos); } // status = 正しくTileを認識できているか
            catch (Exception ex)
            {
                // added by Hotta 2024/09/30
                SetThroughMode(false);
                setUserSettingSetPos(userSetting);
                //

                // 継続しない
                ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                tbtnUfCamSetPos.IsChecked = false;
                tcUfCamView.SelectedIndex = 0;

                return;
            }


            // added by Hotta 2024/09/30
            // 上のGetCameraPosUf()実行中に、tbtnUfCamSetPosやbtnUfCamAdjustStart等が押されたときの対策
            if (tbtnUfCamSetPos.IsChecked != true)
            {
                SetThroughMode(false);
                setUserSettingSetPos(userSetting);
                return;
            }
            //


            try
            {
                bool canProgress = true; // 次のStep(Measure/Adjust)に進めるかどうか

                // Tileを正しく取得できた場合
                if (status == true)
                {
                    canProgress = true;

                    // タイルを検出できたので、次回、黒/ラスターを計測しない
                    // m_Enable_Capture_MaskImage = false; // 一部分のみを対象にすると隣のCabiのタイルを検出してしまうので毎回撮るようにする

                    // 表示用のTile+線画像を生成
                    using (Mat matImageProcess = new Mat(m_CamPos_TileImagePath + ".jpg"))
                    {
                        for (int y = 0; y < aryBlob.GetLength(1); y++)
                        {
                            for (int x = 0; x < aryBlob.GetLength(0) - 1; x++)
                            {
                                Cv2.Line(matImageProcess,
                                    new OpenCvSharp.Point(aryBlob[x, y].Centroid.X, aryBlob[x, y].Centroid.Y),
                                    new OpenCvSharp.Point(aryBlob[x + 1, y].Centroid.X, aryBlob[x + 1, y].Centroid.Y),
                                    new Scalar(0, 255, 255), 1);
                            }
                        }

                        for (int x = 0; x < aryBlob.GetLength(0); x++)
                        {
                            for (int y = 0; y < aryBlob.GetLength(1) - 1; y++)
                            {
                                Cv2.Line(matImageProcess,
                                    new OpenCvSharp.Point(aryBlob[x, y].Centroid.X, aryBlob[x, y].Centroid.Y),
                                    new OpenCvSharp.Point(aryBlob[x, y + 1].Centroid.X, aryBlob[x, y + 1].Centroid.Y),
                                    new Scalar(0, 255, 255), 1);
                            }
                        }

                        matImageProcess.SaveImage(tempPath + "imageProcess.jpg");

                        // 画像を表示
                        DispImageFileUnlock(tempPath + "imageProcess.jpg", img, null, true);
                        //DoEvents();
                    }

                    // 規格に入っているかチェック
                    status = CheckCameraPos(aryBlob, camPos);

                    if (status == true)
                    {
                        canProgress = true;
                        txbUfCamPos.Foreground = System.Windows.Media.Brushes.Lime;
                        txbUfCamPos.Text = "OK";
                    }
                    else
                    {
                        canProgress = false;
                        txbUfCamPos.Foreground = System.Windows.Media.Brushes.Red;
                        txbUfCamPos.Text = "NG";
                    }

                   #region UI表示

                    double[] aryValue = new double[] { Math.Abs(m_CamPos_Pan), Math.Abs(m_CamPos_Tilt), Math.Abs(m_CamPos_Roll / 2), Math.Abs(m_CamPos_Tx / 3), Math.Abs(m_CamPos_Ty / 3), Math.Abs(m_CamPos_Tz / 3) }; // rollは感度を落とす

                    double max_value = 0;
                    int max_index = 0;

                    // 200mm以上ずれていたら、Zを赤字にする
                    if (Math.Abs(m_CamPos_Tz) > 200.0)
                    { max_index = 5; }
                    // Pan/Tilt/Rollのいずれかが3.0°以上ズレている場合はその中のWorstを強調表示
                    else if (Math.Abs(m_CamPos_Pan) > 3.0 || Math.Abs(m_CamPos_Tilt) > 3.0 || Math.Abs(m_CamPos_Roll) > 3.0)
                    {
                        for (int n = 0; n < 3; n++)
                        {
                            if (max_value < aryValue[n])
                            {
                                max_value = aryValue[n];
                                max_index = n;
                            }
                        }
                    }
                    // X/Yが20mm以上ズレている場合は悪い方を強調表示
                    else if (Math.Abs(m_CamPos_Tx) > 20.0 || Math.Abs(m_CamPos_Ty) > 20.0)
                    {
                        for (int n = 0; n < 5; n++)
                        {
                            if (max_value < aryValue[n])
                            {
                                max_value = aryValue[n];
                                max_index = n;
                            }
                        }
                    }
                    else // 全てがある程度合っている場合はWorstを強調表示
                    {
                        for (int n = 0; n < aryValue.Length; n++)
                        {
                            if (max_value < aryValue[n])
                            {
                                max_value = aryValue[n];
                                max_index = n;
                            }
                        }
                    }

                    System.Windows.Media.Brush brush;
                    System.Windows.Media.Brush goodBrush = new SolidColorBrush(Colors.Green);
                    System.Windows.Media.Brush badBrush = new SolidColorBrush(Colors.Red);

                    // ●Pan
                    if (canProgress != true && max_index == 0)
                    { brush = badBrush; }
                    else
                    { brush = goodBrush; }
                    setText(txbUfCamPan, m_CamPos_Pan.ToString("+0.0;-0.0"), brush);

                    if (m_CamPos_Pan >= 0)
                    { imgUfCamPan.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_2.gif")); }
                    else
                    { imgUfCamPan.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_4.gif")); }

                    // ●Tilt
                    if (canProgress != true && max_index == 1)
                    { brush = badBrush; }
                    else
                    { brush = goodBrush; }
                    setText(txbUfCamTilt, m_CamPos_Tilt.ToString("+0.0;-0.0"), brush);

                    if (m_CamPos_Tilt >= 0)
                    { imgUfCamTilt.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_1.gif")); }
                    else
                    { imgUfCamTilt.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_3.gif")); }

                    // ●Roll
                    if (canProgress != true && max_index == 2)
                    { brush = badBrush; }
                    else
                    { brush = goodBrush; }
                    setText(txbUfCamRoll, m_CamPos_Roll.ToString("+0.0;-0.0"), brush);

                    if (m_CamPos_Roll >= 0)
                    { imgUfCamRoll.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/RightTurn.png")); }
                    else
                    { imgUfCamRoll.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/LeftTurn.png")); }

                    // ●X
                    if (canProgress != true && max_index == 3)
                    { brush = badBrush; }
                    else
                    { brush = goodBrush; }
                    setText(txbUfCamPosX, m_CamPos_Tx.ToString("+0.0;-0.0"), brush);

                    if (m_CamPos_Tx >= 0)
                    { imgUfCamX.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_2.gif")); }
                    else
                    { imgUfCamX.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_4.gif")); }

                    // ●Y
                    if (canProgress != true && max_index == 4)
                    { brush = badBrush; }
                    else
                    { brush = goodBrush; }
                    setText(txbUfCamPosY, m_CamPos_Ty.ToString("+0.0;-0.0"), brush);

                    if (m_CamPos_Ty >= 0)
                    { imgUfCamY.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_3.gif")); }
                    else
                    { imgUfCamY.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_1.gif")); }

                    // ●Z
                    if (canProgress != true && max_index == 5)
                    { brush = badBrush; }
                    else
                    { brush = goodBrush; }
                    setText(txbUfCamPosZ, m_CamPos_Tz.ToString("+0.0;-0.0"), brush);

                    if (m_CamPos_Tz >= 0)
                    { imgUfCamZ.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_1.gif")); }
                    else
                    { imgUfCamZ.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_3.gif")); }

                    #endregion UI表示
                }
                // Tileを正しく発見できなかった場合
                else
                {
                    if (txbUfCamPosX.Text != "")
                    { txbUfCamPosX.Text = ""; }
                    if (txbUfCamPosY.Text != "")
                    { txbUfCamPosY.Text = ""; }
                    if (txbUfCamPosZ.Text != "")
                    { txbUfCamPosZ.Text = ""; }
                    if (txbUfCamPan.Text != "")
                    { txbUfCamPan.Text = ""; }
                    if (txbUfCamTilt.Text != "")
                    { txbUfCamTilt.Text = ""; }
                    if (txbUfCamRoll.Text != "")
                    { txbUfCamRoll.Text = ""; }

                    if (imgUfCamPan.Source != null)
                    { imgUfCamPan.Source = null; }
                    if (imgUfCamTilt.Source != null)
                    { imgUfCamTilt.Source = null; }
                    if (imgUfCamRoll.Source != null)
                    { imgUfCamRoll.Source = null; }
                    if (imgUfCamX.Source != null)
                    { imgUfCamX.Source = null; }
                    if (imgUfCamY.Source != null)
                    { imgUfCamY.Source = null; }
                    if (imgUfCamZ.Source != null)
                    { imgUfCamZ.Source = null; }

                    // タイルを検出できなかったので、処理を中断し、次回、黒/ラスターを計測する
                    m_Enable_Capture_MaskImage = true;

                    canProgress = false; // 次のStep(Measure/Adjust)に進めない

                    bool over_top = false;
                    bool over_bottom = false;
                    bool over_left = false;
                    bool over_right = false;
                    status = true;

                    for (int n = 0; n < m_Max_contours.Length; n++)
                    {
                        if (m_Max_contours[n].X <= tgtCamPos_canUse.TopLeft.X || m_Max_contours[n].X <= tgtCamPos_canUse.BottomLeft.X)
                        {
                            status = false;
                            over_left = true;
                        }
                        if (m_Max_contours[n].X >= tgtCamPos_canUse.TopRight.X || m_Max_contours[n].X >= tgtCamPos_canUse.BottomRight.X)
                        {
                            status = false;
                            over_right = true;
                        }
                        if (m_Max_contours[n].Y <= tgtCamPos_canUse.TopLeft.Y || m_Max_contours[n].Y <= tgtCamPos_canUse.TopRight.Y)
                        {
                            status = false;
                            over_top = true;
                        }
                        if (m_Max_contours[n].Y >= tgtCamPos_canUse.BottomLeft.Y || m_Max_contours[n].Y >= tgtCamPos_canUse.BottomRight.Y)
                        {
                            status = false;
                            over_bottom = true;
                        }
                    }

                    int over_count = 0;
                    if (over_top == true)
                    { over_count++; }
                    if (over_bottom == true)
                    { over_count++; }
                    if (over_left == true)
                    { over_count++; }
                    if (over_right == true)
                    { over_count++; }

                    if (over_count >= 2)
                    {
                        imgUfCamZ.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_3.gif"));
                    }
                    else if (over_left == true)
                    {
                        imgUfCamX.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_4.gif"));
                        imgUfCamPan.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_4.gif"));
                    }
                    else if (over_right == true)
                    {
                        imgUfCamX.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_2.gif"));
                        imgUfCamPan.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_2.gif"));
                    }
                    else if (over_top == true)
                    {
                        imgUfCamY.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_1.gif"));
                        imgUfCamTilt.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_1.gif"));
                    }
                    else if (over_bottom == true)
                    {
                        imgUfCamY.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_3.gif"));
                        imgUfCamTilt.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_3.gif"));
                    }

                    txbUfCamPos.Foreground = System.Windows.Media.Brushes.Red;
                    txbUfCamPos.Text = "NG";
                }

                // OKなら次の工程に進める
                if (appliMode == ApplicationMode.Developer || canProgress == true)
                {
                    btnUfCamMeasStart.IsEnabled = true;
                    btnUfCamAdjustStart.IsEnabled = true;
                }
                else
                {
                    btnUfCamMeasStart.IsEnabled = false;
                    btnUfCamAdjustStart.IsEnabled = false;
                }
            }
            catch
            {
                // 継続する
                timer.Enabled = true;

                return;
            }

            // トグルボタンの状態を確認
            bool? btnStatus = null;
            base.Dispatcher.Invoke(() => btnStatus = tbtn.IsChecked);

            if (btnStatus == null || btnStatus == false)
            {
                // ボタンが解除されている場合は終了処理                
                // ThroughMode設定を解除 1Step
                SetThroughMode(false);

                // ユーザー設定に書き戻し
                setUserSettingSetPos(userSetting);

                // 内部信号OFF
                stopIntSig();

                timer.Enabled = false;
            }
            else
            {
                timer.Enabled = true;
            }

        }

        private bool GetCameraPosUf(System.Windows.Controls.Image img, out CvBlob[,] aryBlob, out CameraPosition camPos)
        {
            m_CamPos_BlackImagePath = tempPath + "black_CamPos";
            m_CamPos_RasterImagePath = tempPath + "raster_CamPos";
            m_CamPos_TileImagePath = tempPath + "tile_CamPos";

            // 写真撮影((Black, Raster,) Tile)
            captureCamPos(img, true);

            // TileのBlobを取得
            detectTileCamPos(out CvBlobs blobs);

            bool status = true;
            aryBlob = new CvBlob[m_CabinetXNum_CamPos * 2, m_CabinetYNum_CamPos * 2];

            try
            {
                // BlobをX-Y順に整列
                getTilePosition(blobs, m_CabinetXNum_CamPos * 2, m_CabinetYNum_CamPos * 2, out aryBlob);

                // カメラ姿勢の推定
                estimateCamPos(aryBlob);

                camPos = new CameraPosition(m_CamPos_Tx, m_CamPos_Ty, m_CamPos_Tz, m_CamPos_Pan, m_CamPos_Tilt, m_CamPos_Roll);
            }
            catch //(Exception ex)
            {
                camPos = new CameraPosition();
                status = false;
            }

            return status;
        }

        /// <summary>
        /// カメラの位置が規格に入っているか確認する
        /// </summary>
        /// <param name="aryBlob">1Cabinetあたり4か所表示されるBlobの配列</param>
        /// <param name="camPos">目標とのズレ量</param>
        /// <returns></returns>
        private bool CheckCameraPos(CvBlob[,] aryBlob, CameraPosition camPos)
        {
            bool status = true;

            // 各辺の長さを計算
            //m_CamPos_TopLen = Math.Sqrt(
            //    Math.Pow(aryBlob[0, 0].Centroid.X - aryBlob[m_CabinetXNum_CamPos * 2 - 1, 0].Centroid.X, 2) +
            //    Math.Pow(aryBlob[0, 0].Centroid.Y - aryBlob[m_CabinetXNum_CamPos * 2 - 1, 0].Centroid.Y, 2));
            //m_CamPos_BottomLen = Math.Sqrt(
            //    Math.Pow(aryBlob[0, m_CabinetYNum_CamPos * 2 - 1].Centroid.X - aryBlob[m_CabinetXNum_CamPos * 2 - 1, m_CabinetYNum_CamPos * 2 - 1].Centroid.X, 2) +
            //    Math.Pow(aryBlob[0, m_CabinetYNum_CamPos * 2 - 1].Centroid.Y - aryBlob[m_CabinetXNum_CamPos * 2 - 1, m_CabinetYNum_CamPos * 2 - 1].Centroid.Y, 2));
            //m_CamPos_LeftLen = Math.Sqrt(
            //    Math.Pow(aryBlob[0, 0].Centroid.X - aryBlob[0, m_CabinetYNum_CamPos * 2 - 1].Centroid.X, 2) +
            //    Math.Pow(aryBlob[0, 0].Centroid.Y - aryBlob[0, m_CabinetYNum_CamPos * 2 - 1].Centroid.Y, 2));
            //m_CamPos_RightLen = Math.Sqrt(
            //    Math.Pow(aryBlob[m_CabinetXNum_CamPos * 2 - 1, 0].Centroid.X - aryBlob[m_CabinetXNum_CamPos * 2 - 1, m_CabinetYNum_CamPos * 2 - 1].Centroid.X, 2) +
            //    Math.Pow(aryBlob[m_CabinetXNum_CamPos * 2 - 1, 0].Centroid.Y - aryBlob[m_CabinetXNum_CamPos * 2 - 1, m_CabinetYNum_CamPos * 2 - 1].Centroid.Y, 2));

            // UFは周囲7%使用不可
            double canNotUseArea = 0.09;

            int notUse = (int)(m_CameraParam.SensorResH * canNotUseArea + 0.5);
            tgtCamPos_canUse.TopLeft.X = notUse;
            tgtCamPos_canUse.TopLeft.Y = notUse;
            tgtCamPos_canUse.TopRight.X = m_CameraParam.SensorResH - notUse;
            tgtCamPos_canUse.TopRight.Y = notUse;
            tgtCamPos_canUse.BottomRight.X = m_CameraParam.SensorResH - notUse;
            tgtCamPos_canUse.BottomRight.Y = m_CameraParam.SensorResV - notUse;
            tgtCamPos_canUse.BottomLeft.X = notUse;
            tgtCamPos_canUse.BottomLeft.Y = m_CameraParam.SensorResV - notUse;

            // 各頂点位置（カメラ画像の外周部は使用しない）
            if (aryBlob[0, 0].Centroid.X < tgtCamPos_canUse.TopLeft.X || aryBlob[0, 0].Centroid.Y < tgtCamPos_canUse.TopLeft.Y ||   // 左上
                aryBlob[m_CabinetXNum_CamPos * 2 - 1, 0].Centroid.X > tgtCamPos_canUse.TopRight.X || aryBlob[m_CabinetXNum_CamPos * 2 - 1, 0].Centroid.Y < tgtCamPos_canUse.TopRight.Y || // 右上
                aryBlob[m_CabinetXNum_CamPos * 2 - 1, m_CabinetYNum_CamPos * 2 - 1].Centroid.X > tgtCamPos_canUse.BottomRight.X || aryBlob[m_CabinetXNum_CamPos * 2 - 1, m_CabinetYNum_CamPos * 2 - 1].Centroid.Y > tgtCamPos_canUse.BottomRight.Y || // 右下
                aryBlob[0, m_CabinetYNum_CamPos * 2 - 1].Centroid.X < tgtCamPos_canUse.BottomLeft.X || aryBlob[0, m_CabinetYNum_CamPos * 2 - 1].Centroid.Y > tgtCamPos_canUse.BottomLeft.Y)   // 左下
            { status = false; }

            // Pan/Tilt/Roll
            if(Math.Abs(camPos.Pan) > Settings.Ins.Camera.SpecSetPosPan
                || Math.Abs(camPos.Tilt) > Settings.Ins.Camera.SpecSetPosTilt
                || Math.Abs(camPos.Roll) > Settings.Ins.Camera.SpecSetPosRoll)
            { status = false; }

            // X/Y/Z
            if(Math.Abs(camPos.X) > Settings.Ins.Camera.SpecSetPosX
                || Math.Abs(camPos.Y) > Settings.Ins.Camera.SpecSetPosY
                || Math.Abs(camPos.Z) > Settings.Ins.Camera.SpecSetPosZ)
            { status = false; }

            return status;
        }

        private void getTargetDispPos(List<UnitInfo> lstTgtUnits, out int startX, out int startY, out int endX, out int endY)
        {
            startX = int.MaxValue;
            startY = int.MaxValue;
            endX = 0;
            endY = 0;

            foreach (UnitInfo unit in lstTgtUnits)
            {
                if (unit.PixelX < startX)
                { startX = unit.PixelX; }

                if (unit.PixelY < startY)
                { startY = unit.PixelY; }

                if (unit.PixelX > endX)
                { endX = unit.PixelX; }

                if (unit.PixelY > endY)
                { endY = unit.PixelY; }
            }

            // UnitのPixelX/YはUnitの始点なので1Unit分足す
            endX += cabiDx;
            endY += cabiDy;
        }

        private ImageSource DrawAuxiliaryLines(BitmapSource src, MarkerPosition marker)
        {
            BitmapSource bmp = src.Clone();

            DrawingGroup drawingGroup = new DrawingGroup();
            using (DrawingContext drawContent = drawingGroup.Open())
            {
                drawContent.DrawImage(src, new System.Windows.Rect(0, 0, src.PixelWidth, src.PixelHeight));

                // Penの設定
                System.Windows.Media.Pen penY = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Yellow, 5);
                System.Windows.Media.Pen penB = new System.Windows.Media.Pen(System.Windows.Media.Brushes.DeepSkyBlue, 5);
                System.Windows.Media.Pen penBDash = new System.Windows.Media.Pen(System.Windows.Media.Brushes.DeepSkyBlue, 5);
                penBDash.DashStyle = new DashStyle(new double[] { 7, 7 }, 0);
                System.Windows.Media.Pen penG = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Lime, 5);

                System.Windows.Media.Pen penPDash = new System.Windows.Media.Pen(System.Windows.Media.Brushes.HotPink, 3);
                penPDash.DashStyle = new DashStyle(new double[] { 7, 7 }, 0);

                // 目標位置を描画
                System.Windows.Rect rect = new System.Windows.Rect(new System.Windows.Point(tgtCamPos.TopLeft.X, tgtCamPos.TopLeft.Y), new System.Windows.Size(tgtCamPos.BottomRight.X - tgtCamPos.TopLeft.X, tgtCamPos.BottomRight.Y - tgtCamPos.TopLeft.Y));
                drawContent.DrawRectangle(null, penBDash, rect);
                drawContent.DrawLine(penBDash, new System.Windows.Point(src.PixelWidth / 2, 0), new System.Windows.Point(src.PixelWidth / 2, src.PixelHeight));
                drawContent.DrawLine(penBDash, new System.Windows.Point(0, src.PixelHeight / 2), new System.Windows.Point(src.PixelWidth, src.PixelHeight / 2));

                double radX = Settings.Ins.Camera.SpecSetPosX / SetPosTransRateH;
                double radY = Settings.Ins.Camera.SpecSetPosY / SetPosTransRateV;
                drawContent.DrawEllipse(null, penBDash, new System.Windows.Point(tgtCamPos.TopLeft.X, tgtCamPos.TopLeft.Y), radX, radY);
                drawContent.DrawEllipse(null, penBDash, new System.Windows.Point(tgtCamPos.TopRight.X, tgtCamPos.TopRight.Y), radX, radY);
                drawContent.DrawEllipse(null, penBDash, new System.Windows.Point(tgtCamPos.BottomLeft.X, tgtCamPos.BottomLeft.Y), radX, radY);
                drawContent.DrawEllipse(null, penBDash, new System.Windows.Point(tgtCamPos.BottomRight.X, tgtCamPos.BottomRight.Y), radX, radY);

                // 現在位置を描画
                if (marker != null)
                {
                    double rad = 3.0;

                    // Top-Left
                    drawContent.DrawEllipse(null, penY, new System.Windows.Point(marker.TopLeft.X, marker.TopLeft.Y), rad, rad);

                    // Top-Right
                    drawContent.DrawEllipse(null, penY, new System.Windows.Point(marker.TopRight.X, marker.TopRight.Y), rad, rad);

                    // Bottom-Left
                    drawContent.DrawEllipse(null, penY, new System.Windows.Point(marker.BottomLeft.X, marker.BottomLeft.Y), rad, rad);

                    // Bottom-Right
                    drawContent.DrawEllipse(null, penY, new System.Windows.Point(marker.BottomRight.X, marker.BottomRight.Y), rad, rad);

                    // Rect
                    drawContent.DrawLine(penY, new System.Windows.Point(marker.TopLeft.X, marker.TopLeft.Y), new System.Windows.Point(marker.TopRight.X, marker.TopRight.Y));
                    drawContent.DrawLine(penY, new System.Windows.Point(marker.TopLeft.X, marker.TopLeft.Y), new System.Windows.Point(marker.BottomLeft.X, marker.BottomLeft.Y));
                    drawContent.DrawLine(penY, new System.Windows.Point(marker.TopRight.X, marker.TopRight.Y), new System.Windows.Point(marker.BottomRight.X, marker.BottomRight.Y));
                    drawContent.DrawLine(penY, new System.Windows.Point(marker.BottomLeft.X, marker.BottomLeft.Y), new System.Windows.Point(marker.BottomRight.X, marker.BottomRight.Y));

                    // 対角線
                    if(Settings.Ins.Camera.ShowDiagonal == true)
                    {
                        drawContent.DrawLine(penY, new System.Windows.Point(marker.TopLeft.X, marker.TopLeft.Y), new System.Windows.Point(marker.BottomRight.X, marker.BottomRight.Y));
                        drawContent.DrawLine(penY, new System.Windows.Point(marker.TopRight.X, marker.TopRight.Y), new System.Windows.Point(marker.BottomLeft.X, marker.BottomLeft.Y));
                    }
                }
            }

            return new DrawingImage(drawingGroup);
        }

        // modified by Hotta 2022/05/26 for カメラ位置
        private ImageSource DrawAuxiliaryLinesGap(BitmapSource src, MarkerPosition marker)
        {
            BitmapSource bmp = src.Clone();

            DrawingGroup drawingGroup = new DrawingGroup();
            using (DrawingContext drawContent = drawingGroup.Open())
            {
                drawContent.DrawImage(src, new System.Windows.Rect(0, 0, src.PixelWidth, src.PixelHeight));

                // Penの設定
                System.Windows.Media.Pen penY = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Yellow, 1);
                System.Windows.Media.Pen penB = new System.Windows.Media.Pen(System.Windows.Media.Brushes.DeepSkyBlue, 1);
                System.Windows.Media.Pen penBDash = new System.Windows.Media.Pen(System.Windows.Media.Brushes.DeepSkyBlue, 1);
                penBDash.DashStyle = new DashStyle(new double[] { 3, 3 }, 0);
                System.Windows.Media.Pen penG = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Lime, 1);
                System.Windows.Media.Pen penCDash = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Cyan, 1);
                penCDash.DashStyle = new DashStyle(new double[] { 3, 3 }, 0);
                System.Windows.Media.Pen penR = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Red, 2);

                // 目標位置を描画
                // modified by Hotta 2022/11/14 for 外部入力されたCabinet配置
                /*
                // modified by Hotta 2022/05/26 for カメラ位置
                // 基準
                int width = (int)(tgtCamPos.BottomRight.X - tgtCamPos.TopLeft.X + 0.5);
                int height = (int)(tgtCamPos.BottomRight.Y - tgtCamPos.TopLeft.Y + 0.5);
                System.Windows.Rect rect =
                    new System.Windows.Rect(new System.Windows.Point((int)(tgtCamPos.TopLeft.X + 0.5), (int)(tgtCamPos.TopLeft.Y + 0.5)),
                    new System.Windows.Size(width, height));
                drawContent.DrawRectangle(null, penCDash, rect);
                */
                drawContent.DrawLine(penCDash, new System.Windows.Point(tgtCamPos.TopLeft.X, tgtCamPos.TopLeft.Y), new System.Windows.Point(tgtCamPos.TopRight.X, tgtCamPos.TopRight.Y));
                drawContent.DrawLine(penCDash, new System.Windows.Point(tgtCamPos.TopRight.X, tgtCamPos.TopRight.Y), new System.Windows.Point(tgtCamPos.BottomRight.X, tgtCamPos.BottomRight.Y));
                drawContent.DrawLine(penCDash, new System.Windows.Point(tgtCamPos.BottomRight.X, tgtCamPos.BottomRight.Y), new System.Windows.Point(tgtCamPos.BottomLeft.X, tgtCamPos.BottomLeft.Y));
                drawContent.DrawLine(penCDash, new System.Windows.Point(tgtCamPos.BottomLeft.X, tgtCamPos.BottomLeft.Y), new System.Windows.Point(tgtCamPos.TopLeft.X, tgtCamPos.TopLeft.Y));

                // 使用可能範囲
                int width = (int)(tgtCamPos_canUse.BottomRight.X - tgtCamPos_canUse.TopLeft.X + 0.5);
                int height = (int)(tgtCamPos_canUse.BottomRight.Y - tgtCamPos_canUse.TopLeft.Y + 0.5);
                System.Windows.Rect rect = new System.Windows.Rect(new System.Windows.Point((int)(tgtCamPos_canUse.TopLeft.X + 0.5), (int)(tgtCamPos_canUse.TopLeft.Y + 0.5)),
                    new System.Windows.Size(width, height));
                drawContent.DrawRectangle(null, penY, rect);

                /*
                // 外側
                width = (int)(tgtCamPos_outSide.BottomRight.X - tgtCamPos_outSide.TopLeft.X + 0.5);
                height = (int)(tgtCamPos_outSide.BottomRight.Y - tgtCamPos_outSide.TopLeft.Y + 0.5);
                rect = new System.Windows.Rect(new System.Windows.Point((int)(tgtCamPos_outSide.TopLeft.X + 0.5), (int)(tgtCamPos_outSide.TopLeft.Y + 0.5)),
                    new System.Windows.Size(width, height));
                drawContent.DrawRectangle(null, penBDash, rect);

                // 内側
                width = (int)(tgtCamPos_inSide.BottomRight.X - tgtCamPos_inSide.TopLeft.X + 0.5);
                height = (int)(tgtCamPos_inSide.BottomRight.Y - tgtCamPos_inSide.TopLeft.Y + 0.5);
                rect = new System.Windows.Rect(new System.Windows.Point((int)(tgtCamPos_inSide.TopLeft.X + 0.5), (int)(tgtCamPos_inSide.TopLeft.Y + 0.5)),
                    new System.Windows.Size(width, height));
                drawContent.DrawRectangle(null, penBDash, rect);
                */

                //drawContent.DrawLine(penBDash, new System.Windows.Point(src.PixelWidth / 2, 0), new System.Windows.Point(src.PixelWidth / 2, src.PixelHeight));
                //drawContent.DrawLine(penBDash, new System.Windows.Point(0, src.PixelHeight / 2), new System.Windows.Point(src.PixelWidth, src.PixelHeight / 2));

                //double radX = Settings.Ins.Camera.SpecSetPosX / SetPosTransRateH;
                //double radY = Settings.Ins.Camera.SpecSetPosY / SetPosTransRateV;
                //drawContent.DrawEllipse(null, penBDash, new System.Windows.Point(tgtCamPos.TopLeft.X, tgtCamPos.TopLeft.Y), radX, radY);
                //drawContent.DrawEllipse(null, penBDash, new System.Windows.Point(tgtCamPos.TopRight.X, tgtCamPos.TopRight.Y), radX, radY);
                //drawContent.DrawEllipse(null, penBDash, new System.Windows.Point(tgtCamPos.BottomLeft.X, tgtCamPos.BottomLeft.Y), radX, radY);
                //drawContent.DrawEllipse(null, penBDash, new System.Windows.Point(tgtCamPos.BottomRight.X, tgtCamPos.BottomRight.Y), radX, radY);

                // 現在位置を描画
                if (marker != null)
                {
                    double rad = 3.0;

                    // Top-Left
                    drawContent.DrawEllipse(null, penY, new System.Windows.Point(marker.TopLeft.X, marker.TopLeft.Y), rad, rad);

                    // Top-Right
                    drawContent.DrawEllipse(null, penY, new System.Windows.Point(marker.TopRight.X, marker.TopRight.Y), rad, rad);

                    // Bottom-Left
                    drawContent.DrawEllipse(null, penY, new System.Windows.Point(marker.BottomLeft.X, marker.BottomLeft.Y), rad, rad);

                    // Bottom-Right
                    drawContent.DrawEllipse(null, penY, new System.Windows.Point(marker.BottomRight.X, marker.BottomRight.Y), rad, rad);

                    // Rect
                    drawContent.DrawLine(penY, new System.Windows.Point(marker.TopLeft.X, marker.TopLeft.Y), new System.Windows.Point(marker.TopRight.X, marker.TopRight.Y));
                    drawContent.DrawLine(penY, new System.Windows.Point(marker.TopLeft.X, marker.TopLeft.Y), new System.Windows.Point(marker.BottomLeft.X, marker.BottomLeft.Y));
                    drawContent.DrawLine(penY, new System.Windows.Point(marker.TopRight.X, marker.TopRight.Y), new System.Windows.Point(marker.BottomRight.X, marker.BottomRight.Y));
                    drawContent.DrawLine(penY, new System.Windows.Point(marker.BottomLeft.X, marker.BottomLeft.Y), new System.Windows.Point(marker.BottomRight.X, marker.BottomRight.Y));

                    // 対角線
                    if (Settings.Ins.Camera.ShowDiagonal == true)
                    {
                        drawContent.DrawLine(penY, new System.Windows.Point(marker.TopLeft.X, marker.TopLeft.Y), new System.Windows.Point(marker.BottomRight.X, marker.BottomRight.Y));
                        drawContent.DrawLine(penY, new System.Windows.Point(marker.TopRight.X, marker.TopRight.Y), new System.Windows.Point(marker.BottomLeft.X, marker.BottomLeft.Y));
                    }
                }
            }

            return new DrawingImage(drawingGroup);
        }

        private bool getUserSettingSetPos(out UserSetting userSetting)
        {
            if (Settings.Ins.ExecLog == true)
            {
                SaveExecLog("\t[0] getUserSetting start ( Controller Count : " + dicController.Count.ToString() + " )");
                //saveExecLog(dicController.ToString());
            }

            string buff;
            userSetting = null;

            foreach (ControllerInfo controller in dicController.Values)
            {
                // Masterコントローラの値のみを取得
                if (controller.Master == true)
                {
                    userSetting = new UserSetting();

                    if (Settings.Ins.ExecLog == true)
                    { SaveExecLog("\t[*1] (Controller Loop) ControllerID : " + controller.ControllerID.ToString()); }

                    userSetting.ControllerID = controller.ControllerID;

                    // Temp Corection
                    if (Settings.Ins.ExecLog == true)
                    { SaveExecLog("\t[*2] Temp Corection"); }

                    if (sendSdcpCommand(SDCPClass.CmdTempCorrectGet, out buff, controller.IPAddress) != true)
                    { return false; }
                    try { userSetting.TempCorrection = Convert.ToInt32(buff, 16); }
                    catch (Exception ex)
                    {
                        string errStr = "[getUserSetting(Temp Corection)] Source : " + ex.Source + "\r\nException Messsage : " + ex.Message + "\r\nBuff = " + buff.ToString();
                        SaveErrorLog(errStr);
                        ShowMessageWindow(errStr, "Exception! (Temp Corection)", System.Drawing.SystemIcons.Error, 500, 210);
                        return false;
                    }

                    // Low Brightness Mode
                    if (Settings.Ins.ExecLog == true)
                    { SaveExecLog("\t[*3] Low Brightness Mode"); }

                    if (sendSdcpCommand(SDCPClass.CmdLowBrightModeGet, out buff, controller.IPAddress) != true)
                    { return false; }
                    try { userSetting.LowBrightnessMode = Convert.ToInt32(buff, 16); }
                    catch (Exception ex)
                    {
                        string errStr = "[getUserSetting(Low Brightness Mode)] Source : " + ex.Source + "\r\nException Messsage : " + ex.Message + "\r\nBuff = " + buff.ToString();
                        SaveErrorLog(errStr);
                        ShowMessageWindow(errStr, "Exception! (Low Brightness Mode)", System.Drawing.SystemIcons.Error, 500, 210);
                        return false;
                    }

                    /*
                    // added by Hotta 2024/09/30
                    // Signal Mode 2
                    if (Settings.Ins.ExecLog == true)
                    { SaveExecLog("\t[*4] Signal Mode (2)"); }

                    if (sendSdcpCommand(SDCPClass.CmdSigModeTwoGet, out buff, controller.IPAddress) != true)
                    { return false; }
                    try { userSetting.SignalModeTwo = Convert.ToInt32(buff, 16); }
                    catch (Exception ex)
                    {
                        string errStr = "[getUserSetting(Signal Mode (2))] Source : " + ex.Source + "\r\nException Messsage : " + ex.Message + "\r\nBuff = " + buff.ToString();
                        SaveErrorLog(errStr);
                        ShowMessageWindow(errStr, "Exception! (Signal Mode (2))", System.Drawing.SystemIcons.Error, 500, 210);
                        return false;
                    }
                    //
                    */
                }
            }

            return true;
        }

        // modified by Hotta 2024/09/30
        private void setUserSettingSetPos(UserSetting userSetting)
        //private void setUserSettingSetPos(UserSetting userSetting, bool isNormal = true)
        {
            if(userSetting == null)
            { throw new Exception("Master controller settings have not been saved."); }

            foreach (ControllerInfo cont in dicController.Values)
            {
                if (cont.Target == true)
                {
                    // Temp Corection
                    Byte[] cmd = new byte[SDCPClass.CmdTempCorrectSet.Length];
                    Array.Copy(SDCPClass.CmdTempCorrectSet, cmd, SDCPClass.CmdTempCorrectSet.Length);

                    cmd[20] = (byte)userSetting.TempCorrection;
                    sendSdcpCommand(cmd, dicController[cont.ControllerID].IPAddress);

                    // Low Brightness Mode
                    cmd = new byte[SDCPClass.CmdLowBrightModeSet.Length];
                    Array.Copy(SDCPClass.CmdLowBrightModeSet, cmd, SDCPClass.CmdLowBrightModeSet.Length);

                    cmd[20] = (byte)userSetting.LowBrightnessMode;
                    sendSdcpCommand(cmd, dicController[cont.ControllerID].IPAddress);

                    /*
                    // added by Hotta 2024/09/30
                    if (isNormal)
                    {
                        // Module Gamma画面ではSignal Mode 2の固定パラメータ設定する箇所があるので、ここでは処理しない
                        // Signal Mode 2
                        int param = m_lstUserSetting.Find(x => x.ControllerID == cont.ControllerID).SignalModeTwo;
                        if (checkSignalModeTwoVacStatus(param))
                        {
                            param &= ~0x08; // VAC bitをoff
                            Byte[] cmd2 = new byte[SDCPClass.CmdSigModeTwoSet.Length];
                            Array.Copy(SDCPClass.CmdSigModeTwoSet, cmd2, SDCPClass.CmdSigModeTwoSet.Length);

                            cmd2[20] = (byte)param;
                            //if (!sendSdcpCommand(cmd, cont.IPAddress))
                            //{ return false; }
                            sendSdcpCommand(cmd2, dicController[cont.ControllerID].IPAddress);
                        }
                    }
                    //
                    */
                }
            }
        }

        // added bby Hotta 2024/09/30
        private void setAdjustSettingSetPos()
        {
            foreach (ControllerInfo cont in dicController.Values)
            {
                if (cont.Target == true)
                {
                    // Through Mode
                    sendSdcpCommand(SDCPClass.CmdThroughModeOn, cont.IPAddress);

                    // Temp Corection
                    sendSdcpCommand(SDCPClass.CmdTempCorrectSet, cont.IPAddress);

                    // Low Brightness Mode
                    sendSdcpCommand(SDCPClass.CmdLowBrightModeSet, cont.IPAddress);
                }
            }
        }
        //

        #endregion Set Position

        #region Mesurement

        /// <summary>
        /// カメラUF測定(Measurement)のエントリーポイント
        /// </summary>
        /// <param name="lstTgtCabi">測定対象Cabinet</param>
        /// <param name="measPath">UF測定Log保存フォルダ</param>
        /// <param name="isViewPtMode">視聴点調整モードフラグ true=視聴点調整モード</param>
        /// <param name="targetOnly">ラスター画像撮影時に対象のみを光らせるかどうかのフラグ 周辺減光調査用</param>
        private void MeasureUfAsync(List<UnitInfo> lstTgtCabi, string measPath, ViewPoint vp, double dist, double wallH, double camH, bool targetOnly = false)
        {
            int processSec = initialUfCameraMeasurementProcessSec(lstTgtCabi.Count);
            winProgress.StartRemainTimer(processSec);

            saveUfLog("Inital Settings.");

            List<UserSetting> lstUserSettings;

            // 全体のStep数を設定
            winProgress.SetWholeSteps(5 + moduleCount + 4 + moduleCount + 9);

            Dispatcher.Invoke(new Action(() =>
            {
                txbUfCamMeasResult.Text = "";
                txbUfCamMeasResult.Background = System.Windows.Media.Brushes.Gray;
                txbUfCamMeasResult.Foreground = System.Windows.Media.Brushes.Gray;
            }));

            // マスクの信号レベルを設定
            m_MeasureLevel = brightness.UF_20pc; // 20IRE = 492

            // OpenCVSharpのDllがあるか確認する
            CheckOpenCvSharpDll();

            // added by Hotta 2023/12/11
            // たまに、UserSettingのまま先に進んでしまう。
            // 目地は問題ないので、目地の処理を真似て、パターン表示、時間待ちを設定する
            outputIntSigChecker(0, 0, modDy, modDx, modDx, modDy); // 20IRE
            Thread.Sleep(Settings.Ins.Camera.PatternWait);

            // Cabinet on
            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdUnitPowerOn, cont.IPAddress); }
            Thread.Sleep(5000);
            //


            // カメラの目標位置を再設定
            SetCamPosTarget();

            // ユーザー設定を保存 1Step
            winProgress.ShowMessage("Store User Settings.");
            winProgress.PutForward1Step();
            saveUfLog("Store User Settings.");
            // modified by Hotta 2024/09/30
            /*
            getUserSetting(out lstUserSettings);
            */
            bool st = getUserSetting(out lstUserSettings);
            // Measure単体の時、保存する。 Adjsut -> Measure の場合は、保存しない
            if (st == true && m_lstUserSetting == null)
                m_lstUserSetting = lstUserSettings;
            //

            // 調整用設定 1Step
            winProgress.ShowMessage("Set Adjust Settings.");
            winProgress.PutForward1Step();
            saveUfLog("Set Adjust Settings.");
            setAdjustSetting();

            // Layout情報Off
            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdDispUnitAddrOff, 0, cont.IPAddress); }

            processSec = calcUfCameraMeasurementProcessSec(1);
            winProgress.SetRemainTimer(processSec);

            // AutoFocus 1Step
            winProgress.ShowMessage("Execute AutoFocus.");
            winProgress.PutForward1Step();
            saveUfLog("Execute AutoFocus.");
            ShootCondition codition = Settings.Ins.Camera.SetPosSetting;
            m_CamMeasPath = tempPath;

            outputIntSigChecker(0, 0, modDy, modDx, modDx, modDy); // 20IRE
            AutoFocus(codition, new AfAreaSetting());

            // 照明の映り込みをチェック 1Step
            //winProgress.ShowMessage("Check Light Reflections.");
            //winProgress.PutForward1Step();
            //CheckLightingReflection(lstTgtUnit);

            processSec = calcUfCameraMeasurementProcessSec(2);
            winProgress.SetRemainTimer(processSec);

            // 開始時のカメラ位置を保存 1Step
            winProgress.ShowMessage("Store Camera Position.");
            winProgress.PutForward1Step();
            saveUfLog("Store Camera Position.");
            bool status = GetCameraPosUf(imgUfCamCameraView, out CvBlob[,] aryBlob, out CameraPosition startCamPos);

            // added by Hotta 2025/02/20 for LED-7748
            if (status != true)
            { throw new Exception("Failed to get the camera position."); }
            //                
            if (targetOnly == false) // 通常モード
            {
                // 撮影エリアの画像上の割合を計算(小さい場合はハンチングの可能性があるのでカメラずれ量を使用しない。)
                double sensorArea = m_CameraParam.SensorResV * m_CameraParam.SensorResH;
                double targetArea = CalcTargetArea(aryBlob);

                if (targetArea <= sensorArea * 0.10) // 画面全体の10％より小さい(4m、2x2だとおおよそ8.5％)
                { startCamPos = new CameraPosition(); }

                ufCamMeasLog.StartCamPos = startCamPos;
                if (status != true)
                { throw new Exception("Failed to get the camera position."); }

                status = CheckCameraPos(aryBlob, startCamPos);
                if (status != true)
                { throw new Exception("The camera position is inappropriate.\r\nAfter alignment, the camera is moving. Please re-align the camera."); }
            }
            else // Target Only
            { startCamPos = new CameraPosition(); }

            ufCamMeasLog.StartCamPos = startCamPos;

            // Cabinet位置(空間座標)を再設定(カメラ位置合わせを実施するとカメラ-Wall相対位置情報がリセットされる)
            SetCabinetPos(lstTgtCabi, dist, wallH, camH);

            // カメラ設置ズレ分を反映
            MoveCabinetPos(startCamPos.Pan, startCamPos.Tilt, startCamPos.Roll, startCamPos.X, startCamPos.Y, startCamPos.Z);

            // 調整点の最大角度を確認(Pan, Tiltとも±50が限界)
            CheckCpAngle(lstTgtCabi);

            processSec = calcUfCameraMeasurementProcessSec(3);
            winProgress.SetRemainTimer(processSec);

#if NO_CAP
            // カメラ視野角特性補正データのLoad
            //CameraCorrectionData.LoadFromXmlFile(applicationPath + CompDir + CamCorrectDir + "Ccd_ILCE-6400.xml", out ccd);

            // LED配光特性補正データのLoad
            //CameraCorrectionData.LoadFromXmlFile(applicationPath + CompDir + CamCorrectDir + "Lcd_ZRD-B15A_73.xml", out lcd);

            /*string*/ measPath = @"D:\Documents\Chiron\東雲出張\測定値\UF Measurment\UF_202112241054_FHDセンター_±5%ずらし_1\";
#else
            // 撮影
            CaptureUfImages(lstTgtCabi, measPath, targetOnly); // 13step
#endif

            // added by Hotta 2024/01/09
            // ESCキー押下による中断を無効化
            winProgress.AbortType = WindowProgress.TAbortType.None;
            //

            processSec = calcUfCameraMeasurementProcessSec(4);
            winProgress.SetRemainTimer(processSec);

            // 処理
            calcMeasAreaPv(lstTgtCabi, measPath, vp); // 12step

            processSec = calcUfCameraMeasurementProcessSec(5);
            winProgress.SetRemainTimer(processSec);

            // 終了時のカメラ位置を保存 1Step
            winProgress.ShowMessage("Store Camera Position.");
            winProgress.PutForward1Step();
            saveUfLog("Store Camera Position.");
            GetCameraPosUf(imgUfCamCameraView, out aryBlob, out CameraPosition endCamPos);
            ufCamMeasLog.EndCamPos = endCamPos;

            // 内部信号停止
            stopIntSig();

            // 調整設定を解除 1Step
            winProgress.ShowMessage("Set Normal Settings.");
            winProgress.PutForward1Step();
            saveUfLog("Set Normal Settings.");
            //setNormalSetting();
            SetThroughMode(false);

            // ユーザー設定に書き戻し 1Step
            winProgress.ShowMessage("Restore User Settings.");
            winProgress.PutForward1Step();
            saveUfLog("Restore User Settings.");
            setUserSetting(lstUserSettings);

            //if (status != true)
            //{ throw new Exception("The camera position is inappropriate.\r\nAfter alignment, the camera is moving. Please re-align the camera."); }

            // 表示
            winProgress.ShowMessage("Show Result.");
            winProgress.PutForward1Step();
            saveUfLog("Show Result.");
            dispUfMeasResult(); // 1step
        }

        private double CalcTargetArea(CvBlob[,] aryBlob)
        {
            double area = 0;

            area = (aryBlob[aryBlob.GetLength(0) - 1, aryBlob.GetLength(1) - 1].Centroid.X - aryBlob[0, 0].Centroid.X) * (aryBlob[aryBlob.GetLength(0) - 1, aryBlob.GetLength(1) - 1].Centroid.Y - aryBlob[0, 0].Centroid.Y);

            return area;
        }

        private void CheckCpAngle(List<UnitInfo> lstTgtCabi)
        {
            foreach (UnitInfo cabi in lstTgtCabi)
            {
                foreach (UfCamCpAngle cp in cabi.lstCpAngle)
                {
                    if (Math.Abs(cp.Angle.Pan) > PanLimit || Math.Abs(cp.Angle.Tilt) > TiltLimit)
                    { throw new Exception("The relative angle between the camera and the wall exceeds the upper limit."); }
                }
            }
        }

        /// <summary>
        /// カメラUF測定(Measurement)用の画像取得
        /// Measurementでは撮影と処理が別々のメソッド
        /// </summary>
        /// <param name="lstTgtCabi">測定対象Cabinet</param>
        /// <param name="measPath">UF測定Log保存フォルダ</param>
        /// <param name="targetOnly">ラスター画像撮影時に対象のみを光らせるかどうかのフラグ</param>
        private void CaptureUfImages(List<UnitInfo> lstTgtCabi, string measPath, bool targetOnly)
        {
            // Black
            winProgress.ShowMessage("Capture Black Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Black Image.");
            stopIntSig(); // Black出力(信号Stop)
            CaptureImage(measPath + fn_BlackFile, Settings.Ins.Camera.MeasAreaSetting);

            // マスク用信号出力
            winProgress.ShowMessage("Capture Mask Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Mask Image.");
            OutputTargetArea(lstTgtCabi);

            string imgPath = measPath + System.IO.Path.GetFileNameWithoutExtension(fn_AreaFile);
            CaptureImage(imgPath, Settings.Ins.Camera.MeasAreaSetting);

            // TrimmingArea
            CaptureModuleAreaImage(measPath); // 8 or 12step

            // Flat
            CaptureMeasFlatImage(measPath, lstTgtCabi, targetOnly); // 4step

            // 内部信号停止
            stopIntSig();
        }

        /// <summary>
        /// Module画像の取得
        /// </summary>
        /// <param name="path">UF測定Log保存フォルダ</param>
        private void CaptureModuleAreaImage(string path)
        {
            for (int mod = 0; mod < moduleCount; mod++)
            {
                int startX = modDx * (mod % ModuleCountX);
                int startY = modDy * (mod / ModuleCountX);

                winProgress.ShowMessage("Capture Module-" + (mod + 1) + " Image.");
                winProgress.PutForward1Step();
                saveUfLog($"Capture Module-{mod + 1} Image.");
                outputIntSigHatch(startX, startY, modDy, modDx, cabiDx, cabiDy);

                string imgPath = path + fn_MeasureAreaMod + mod;
                CaptureImage(imgPath, Settings.Ins.Camera.MeasAreaSetting);
            }
        }

        /// <summary>
        /// ラスター画像の取得
        /// </summary>
        /// <param name="path">UF測定Log保存フォルダ</param>
        /// <param name="lstTgtCabi">測定対象Cabinet</param>
        /// <param name="targetOnly">ラスター画像撮影時に対象のみを光らせるかどうかのフラグ</param>
        private void CaptureMeasFlatImage(string path, List<UnitInfo> lstTgtCabi, bool targetOnly = false)
        {
            // modified by Hotta 2021/09/01
#if NO_CAP
            // カメラ設定
            winProgress.ShowMessage("Set Camera Settings. (MeasArea)");
            winProgress.PutForward1Step();
            //cc.SetCameraSettings(Settings.Ins.Camera.MeasAreaSetting);

            // Red
            winProgress.ShowMessage("Capture Flat-Red Image.");
            winProgress.PutForward1Step();
            //outputIntSigFlat(brightness._20pc, 0, 0);

            //string imgPath = path + fn_FlatRed;
            //cc.CaptureImage(imgPath);
            //Thread.Sleep(Settings.Ins.Camera.CameraWait);

            // Green
            winProgress.ShowMessage("Capture Flat-Green Image.");
            winProgress.PutForward1Step();
            //outputIntSigFlat(0, brightness._20pc, 0);

            //imgPath = path + fn_FlatGreen;
            //cc.CaptureImage(imgPath);
            //Thread.Sleep(Settings.Ins.Camera.CameraWait);

            // Blue
            winProgress.ShowMessage("Capture Flat-Blue Image.");
            winProgress.PutForward1Step();
            //outputIntSigFlat(0, 0, brightness._20pc);

            //imgPath = path + fn_FlatBlue;
            //cc.CaptureImage(imgPath);
            //Thread.Sleep(Settings.Ins.Camera.CameraWait);

            // White
            winProgress.ShowMessage("Capture Flat-White Image.");
            winProgress.PutForward1Step();
            //outputIntSigFlat(brightness._20pc, brightness._20pc, brightness._20pc);

            //imgPath = path + fn_FlatWhite;
            //cc.CaptureImage(imgPath);
            //Thread.Sleep(Settings.Ins.Camera.CameraWait);

#else
            // Red
            winProgress.ShowMessage("Capture Flat-Red Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Flat-Red Image.");

            if (targetOnly == true)
            {
                OutputTargetArea(lstTgtCabi, m_MeasureLevel, 0, 0);
                //outputIntSigWindow(lstTgtCabi[0].PixelX, lstTgtCabi[0].PixelY, cabiDy, cabiDx, brightness._20pc, 0, 0);
            }
            else
            { outputIntSigFlat(brightness._20pc, 0, 0); }

            string imgPath = path + fn_FlatRed;
            CaptureImage(imgPath, Settings.Ins.Camera.MeasAreaSetting); // カメラ設定付き

            Thread.Sleep(100); // Flagの更新が間に合わないみたい

            // Green
            winProgress.ShowMessage("Capture Flat-Green Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Flat-Green Image.");

            if (targetOnly == true)
            {
                OutputTargetArea(lstTgtCabi, 0, m_MeasureLevel, 0);
                //outputIntSigWindow(lstTgtCabi[0].PixelX, lstTgtCabi[0].PixelY, cabiDy, cabiDx, 0, brightness._20pc, 0);
            }
            else
            { outputIntSigFlat(0, brightness._20pc, 0); }

            imgPath = path + fn_FlatGreen;
            CaptureImage(imgPath);

            Thread.Sleep(100);

            // Blue
            winProgress.ShowMessage("Capture Flat-Blue Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Flat-Blue Image.");

            if (targetOnly == true)
            {
                OutputTargetArea(lstTgtCabi, 0, 0, m_MeasureLevel);
                //outputIntSigWindow(lstTgtCabi[0].PixelX, lstTgtCabi[0].PixelY, cabiDy, cabiDx, 0, 0, brightness._20pc);
            }
            else
            { outputIntSigFlat(0, 0, brightness._20pc); }

            imgPath = path + fn_FlatBlue;
            CaptureImage(imgPath);

            Thread.Sleep(100);

            // White
            winProgress.ShowMessage("Capture Flat-White Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Flat-White Image.");

            if (targetOnly == true)
            {
                OutputTargetArea(lstTgtCabi, m_MeasureLevel, m_MeasureLevel, m_MeasureLevel);
                //outputIntSigWindow(lstTgtCabi[0].PixelX, lstTgtCabi[0].PixelY, cabiDy, cabiDx, brightness._20pc, brightness._20pc, brightness._20pc);
            }
            else
            { outputIntSigFlat(brightness._20pc, brightness._20pc, brightness._20pc); }

            imgPath = path + fn_FlatWhite;
            CaptureImage(imgPath);
#endif
        }

        /// <summary>
        /// 測定点を格納し、その測定点の画素値を格納
        /// </summary>
        /// <param name="lstTgtCabi">測定対象Cabinet</param>
        /// <param name="path">UF測定Log保存フォルダ</param>
        /// <param name="isViewPtMode">視聴点調整モードフラグ</param>
        private void calcMeasAreaPv(List<UnitInfo> lstTgtCabi, string path, ViewPoint vp)
        {
            // 対象エリアのマスク画像を作成
            winProgress.ShowMessage("Generate Mask Image.");
            winProgress.PutForward1Step();
            saveUfLog("Generate Mask Image.");
            string file = path + fn_AreaFile;
            string blackFile = path + fn_BlackFile + ".arw";
            MakeMaskImageArw(file, blackFile);

            // 測定点を格納
            storeUfMeasArea(lstTgtCabi, path, vp); // 8 / 12step

            // RAW画像から各測定点の画素値を格納
            storeUfCamPv(path); // 4step
        }

        /// <summary>
        /// 各測定点の座標と校正テーブルの値を格納
        /// </summary>
        /// <param name="lstTgtCabi">測定対象Cabinet</param>
        /// <param name="path">UF測定Log保存フォルダ</param>
        /// <param name="isViewPtMode"></param>
        private void storeUfMeasArea(List<UnitInfo> lstTgtCabi, string path, ViewPoint vp)
        {
            List<UfCamMeasArea>[] aryLstCell = new List<UfCamMeasArea>[moduleCount];
            UfCamMeasArea[][,] aryMeasArea = new UfCamMeasArea[moduleCount][,];

            // TrimmingAreaを抽出
            for (int mod = 0; mod < moduleCount; mod++)
            {
                winProgress.ShowMessage("Load Module-" + (mod + 1) + " Image.");
                winProgress.PutForward1Step();
                saveUfLog($"Load Module-{mod + 1} Image.");
                string file = path + fn_MeasureAreaMod + mod + ".arw";
                string blackFile = path + fn_BlackFile + ".arw";
                calcTrimmingAreaCellMask(file, blackFile, out aryLstCell[mod]);

                if (aryLstCell[mod].Count != lstTgtCabi.Count)
                { throw new Exception("The number of detected modules is incorrect.\r\nPlease confirm the camera settings and try shooting again."); }
            }

            // 調整点の格納 ※矩形前提
            int StartUnitX = int.MaxValue, StartUnitY = int.MaxValue; // 対象Unitの左上のユニット位置（1ベース）
            int EndUnitX = 0, EndUnitY = 0;

            // Unitの位置を調査
            foreach (UnitInfo unit in lstTgtCabi)
            {
                if (unit.X < StartUnitX)
                { StartUnitX = unit.X; }

                if (unit.Y < StartUnitY)
                { StartUnitY = unit.Y; }

                if (unit.X > EndUnitX)
                { EndUnitX = unit.X; }

                if (unit.Y > EndUnitY)
                { EndUnitY = unit.Y; }
            }

            int LenX = EndUnitX - StartUnitX + 1; // 対象UnitのX方向Unit数
            int LenY = EndUnitY - StartUnitY + 1; // 対象UnitのY方向Unit数

            // 視聴点モード用に中心CabinetのTilt角を抽出する（現在はMeasurementに視聴点モードはない）
            double centerTilt = double.NaN;
            if (vp.Vertical == true)
            {
                int centerX = StartUnitX + LenX / 2;
                int centerY = StartUnitY + LenY / 2;
                centerTilt = GetTiltAngle(lstTgtCabi, centerX, centerY);
            }

            // TrimmingAreaをCabinet順にSortしておく
            for (int mod = 0; mod < moduleCount; mod++)
            {
                SortTrimmingArea(aryLstCell[mod], LenX, LenY, out UfCamMeasArea[,] aryTemp);
                aryMeasArea[mod] = aryTemp;
            }

            // Cabinetごとのループ
            for (int y = StartUnitY; y <= EndUnitY; y++)
            {
                for (int x = StartUnitX; x <= EndUnitX; x++)
                {
                    UfCamMeasValue ufMv = new UfCamMeasValue(moduleCount);

                    // 対象のUnitを探す
                    ufMv.Unit = searchUnit(lstTgtCabi, x, y);

                    // Moduleのループ
                    for (int mod = 0; mod < moduleCount; mod++)
                    {
                        winProgress.ShowMessage("Calc Trimming Area.\r\n(X = " + x + ", Y = " + y + ", Module = " + (mod + 1) + ")");
                                                
                        // ●9分割して格納
                        // 該当のAreaを抽出
                        RectangleArea cellArea = aryMeasArea[mod][x - StartUnitX, y - StartUnitY].RectArea;//lstAreaLine[x - StartUnitX];

                        // 16個の交点を求める
                        CalcCrossPoint(cellArea, out Coordinate[] aryCp);

                        // 測定点(3x3)のループ
                        for (int pos = 0; pos < UfMeasSplitCount; pos++) // 9：Cell内の分割数、3×3
                        {
                            UfCamMp mp = new UfCamMp();

                            mp.CellNo = (CellNum)Enum.ToObject(typeof(CellNum), mod);
                            mp.PosNo = pos;

                            int startX, startY, endX, endY;

                            int offset = 0;

                            if (pos > 2 && pos <= 5) { offset = 1; }
                            else if (pos > 5) { offset = 2; }

                            startX = (int)aryCp[pos + offset].X;
                            startY = (int)aryCp[pos + offset].Y;
                            endX = (int)aryCp[pos + offset + 5].X;
                            endY = (int)aryCp[pos + offset + 5].Y;

                            int height = endX - startX;
                            int width = endY - startY;

                            if (height <= 0 || width <= 0)
                            { throw new Exception("The setting of the measurement area is inappropriate.\r\n(X = " + x + ", Y = " + y + ", Module = " + mod + ", Pos. = " + pos + ")"); }

                            mp.CamArea = new Area(startX, startY, endY - startY, endX - startX);

                            // カメラ補正値
                            UfCamCorrectionPoint temp = new UfCamCorrectionPoint();
                            temp.Unit = ufMv.Unit;
                            temp.Pos = (UfCamCorrectPos)Enum.ToObject(typeof(UfCamCorrectPos), 12 + mod); // 12 = UfCamCorrectPosのModuleの先頭
                            temp.CameraArea = mp.CamArea;
                            SearchCameraCorrectValue(temp, out CameraCorrectionValue ccv, out LedCorrectionValue lcv, vp); // lcvはModuleの値を使用する
                            mp.CCV = ccv;
                            mp.LCV = lcv;

                            ufMv.aryUfCamModules[mod].aryUfCamMp[pos] = mp;
                        }
                    }

                    ufCamMeasLog.lstUfCamMeas.Add(ufMv);
                }
            }
        }

        /// <summary>
        /// 各Moduleのエリアを抽出する。
        /// </summary>
        /// <param name="file">Module画像ファイル</param>
        /// <param name="blackFile">Black画像ファイル</param>
        /// <param name="lstArea">Moduleエリアのリスト</param>
        unsafe private void calcTrimmingAreaCellMask(string file, string blackFile, out List<UfCamMeasArea> lstArea)
        {
            lstArea = new List<UfCamMeasArea>();

            string maskFile = System.IO.Path.GetDirectoryName(file) + "\\" + fn_MaskFile;
            AcqARW arw, arwBlack;
            loadArwFile(file, out arw);
            loadArwFile(blackFile, out arwBlack);
            int width = arw.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
            int height = arw.RawMainIFD.ImageHeight / 2;

            using (Mat src = new Mat(new Size(width, height), MatType.CV_16UC1))
            using (Mat gray = new Mat())
            {
                ushort* pMat = (ushort*)(src.Data);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int pv = arw.RawData[1][x, y] - arwBlack.RawData[1][x, y];
                        if (pv < 0) { pv = 0; }
                        pMat[y * width + x] = (ushort)pv;
                    }
                }

                src.ConvertTo(gray, MatType.CV_8UC1, 1.0 / 16);

                using (Mat mask = new Mat(maskFile, ImreadModes.Grayscale))
                using (Mat masked = new Mat())
                using (Mat dilate = new Mat())
                {
                    gray.CopyTo(masked, mask.Threshold(0, 0xff, ThresholdTypes.Otsu));

                    using (Mat binary = masked.Threshold(0, 0xff, ThresholdTypes.Otsu))
                    {
                        Cv2.Dilate(binary, dilate, null);

                        if (Settings.Ins.Camera.SaveIntImage == true)
                        {
                            Cv2.ImWrite(applicationPath + @"\Temp\gray.jpg", gray, (int[])null);
                            Cv2.ImWrite(applicationPath + @"\Temp\binary.jpg", binary, (int[])null);
                            Cv2.ImWrite(applicationPath + @"\Temp\mask.jpg", mask);
                            Cv2.ImWrite(applicationPath + @"\Temp\masked.jpg", masked);
                            Cv2.ImWrite(applicationPath + @"\Temp\dilate.jpg", dilate);
                        }

                        CvBlobs blobs = new CvBlobs(binary);
                        double blobArea = calcCellArea(blobs);

                        var contours = binary.FindContoursAsMat(RetrievalModes.List, ContourApproximationModes.ApproxSimple);

                        var candidatre = contours
                            .Select(c =>
                            {
                                var outputMat = new MatOfPoint();
                                Cv2.ApproxPolyDP(c, outputMat, 0.01 * c.ArcLength(true), true);
                                var criteria = new TermCriteria(CriteriaType.Eps | CriteriaType.MaxIter, 100, 0.001);
                                var corners = Cv2.CornerSubPix(gray, outputMat.Select(x => new Point2f(x.X, x.Y)).ToArray(), new Size(5, 5), new Size(-1, -1), criteria);
                                //MEMO : 角のPointコレクションと面積をペアで返します。
                                return new Tuple<Point2f[], double>(corners, Cv2.ContourArea(c.ToArray()));
                            })
                            //MEMO : 面積で区切ってゴミを除去してます。
                            .Where(c => c.Item2 < blobArea * 1.6 && c.Item2 > blobArea * 0.4);

                        var aryCorner = candidatre.Select(x => x.Item1).ToArray();

                        // 各エリアのコーナー座標を格納する　画面コーナーから一番近い点を採用
                        for (int i = 0; i < aryCorner.Length; i++)
                        {
                            RectangleArea area = new RectangleArea();

                            // Top-Left
                            double minDist = double.MaxValue;
                            Coordinate basePt = new Coordinate(0, 0);
                            for(int j = 0; j < aryCorner[i].Length; j++)
                            {
                                double dist = CalcDistance(basePt, new Coordinate(aryCorner[i][j].X, aryCorner[i][j].Y));
                                if (dist < minDist)
                                {
                                    area.TopLeft = new Coordinate(aryCorner[i][j].X, aryCorner[i][j].Y);
                                    minDist = dist;
                                }
                            }

                            // Top-Right
                            minDist = double.MaxValue;
                            basePt = new Coordinate(src.Width, 0);
                            for (int j = 0; j < aryCorner[i].Length; j++)
                            {
                                double dist = CalcDistance(basePt, new Coordinate(aryCorner[i][j].X, aryCorner[i][j].Y));
                                if (dist < minDist)
                                {
                                    area.TopRight = new Coordinate(aryCorner[i][j].X, aryCorner[i][j].Y);
                                    minDist = dist;
                                }
                            }

                            // Bottom-Left
                            minDist = double.MaxValue;
                            basePt = new Coordinate(0, src.Height);
                            for (int j = 0; j < aryCorner[i].Length; j++)
                            {
                                double dist = CalcDistance(basePt, new Coordinate(aryCorner[i][j].X, aryCorner[i][j].Y));
                                if (dist < minDist)
                                {
                                    area.BottomLeft = new Coordinate(aryCorner[i][j].X, aryCorner[i][j].Y);
                                    minDist = dist;
                                }
                            }

                            // Bottom-Right
                            minDist = double.MaxValue;
                            basePt = new Coordinate(src.Width, src.Height);
                            for (int j = 0; j < aryCorner[i].Length; j++)
                            {
                                double dist = CalcDistance(basePt, new Coordinate(aryCorner[i][j].X, aryCorner[i][j].Y));
                                if (dist < minDist)
                                {
                                    area.BottomRight = new Coordinate(aryCorner[i][j].X, aryCorner[i][j].Y);
                                    minDist = dist;
                                }
                            }

                            //List<Coordinate> lstPt = new List<Coordinate>();

                            //for (int j = 0; j < 4; j++)
                            //{ lstPt.Add(new Coordinate((double)aryCorner[i][j].X, (double)aryCorner[i][j].Y)); }

                            //// Y座標順に並び替え
                            //var cy = new Comparison<Coordinate>(CompareY);
                            //lstPt.Sort(cy);

                            //// Top
                            //List<Coordinate> lstAreaLine = lstPt.GetRange(0, 2);

                            //var cx = new Comparison<Coordinate>(CompareX);
                            //lstAreaLine.Sort(cx);

                            //area.TopLeft = lstAreaLine[0];
                            //area.TopRight = lstAreaLine[1];

                            //// Bottom
                            //lstAreaLine = lstPt.GetRange(2, 2);
                            //lstAreaLine.Sort(cx);

                            //area.BottomLeft = lstAreaLine[0];
                            //area.BottomRight = lstAreaLine[1];

                            double ctX = (area.TopLeft.X + area.TopRight.X + area.BottomLeft.X + area.BottomRight.X) / 4;
                            double ctY = (area.TopLeft.Y + area.TopRight.Y + area.BottomLeft.Y + area.BottomRight.Y) / 4;
                            UfCamMeasArea measArea = new UfCamMeasArea(new Coordinate(ctX, ctY), area);

                            lstArea.Add(measArea);
                        }
                    }
                }
            }
        }

        private double calcCellArea(CvBlobs blobs)
        {
            double area = 0;
            List<double> lstArea = new List<double>();

            double areaAve = 0;

            foreach (CvBlob blob in blobs.Values)
            { areaAve += blob.Area; }

            areaAve /= blobs.Count;

            // 極端に大きいもの、小さいものを除外する
            //blobs.FilterByArea(ModuleAreaMin, ModuleAreaMax);
            blobs.FilterByArea((int)(areaAve * 0.5), (int)(areaAve * 10));

            // 見つかった数が1個以下の場合はNG
            if (blobs.Count < 1)
            { return area; }

            foreach (KeyValuePair<int, CvBlob> pair in blobs)
            { lstArea.Add(pair.Value.Area); }

            // 面積順に並び替え
            lstArea.Sort();

            // 大きさで真ん中辺のTrimmingAreaの平均値を求める
            int count = 0;
            double sum = 0;

            int start = (int)((double)lstArea.Count * 0.4);
            int end = (int)((double)lstArea.Count * 0.6);

            for (int i = start; i <= end; i++)
            {
                sum += lstArea[i];
                count++;
            }

            if (count == 0)
            { area = 0; }
            else
            { area = sum / count; }

            return area;
        }

        void SortTrimmingArea(List<UfCamMeasArea> lstBlobs, int hNum, int vNum, out UfCamMeasArea[,] aryBlob)
        {
            if (lstBlobs == null)
            { throw new Exception("The number of found blobs is null."); }

            if (lstBlobs.Count != hNum * vNum)
            { throw new Exception(string.Format("The number of found blobs is not correct. Found:[{0}], Spec:[{1}]", lstBlobs.Count, hNum * vNum)); }

            aryBlob = new UfCamMeasArea[hNum, vNum];

            for (int x = 0; x < hNum; x++)
            {
                int index = 0;
                UfCamMeasArea[] aryClmBlob = new UfCamMeasArea[vNum];

                // 最も左にあるブロブ
                int min_index = -1;
                double min_x = double.MaxValue;
                for (int n = 0; n < lstBlobs.Count; n++)
                {
                    if (lstBlobs[n].Centroid.X < min_x)
                    {
                        min_x = lstBlobs[n].Centroid.X;
                        min_index = n;
                    }
                }
                aryClmBlob[index++] = lstBlobs[min_index];  // 登録
                Point2d u_ref = new Point2d(lstBlobs[min_index].Centroid.X, lstBlobs[min_index].Centroid.Y);  // 上検索用の起点ブロブ
                Point2d d_ref = new Point2d(lstBlobs[min_index].Centroid.X, lstBlobs[min_index].Centroid.Y);  // 下検索用の起点ブロブ
                lstBlobs.RemoveAt(min_index);  // 検索対象から削除


                // added by Hotta 2022/11/11
                // 各列あたり1ケの場合の対応
                if (vNum == 1)
                {
                    aryBlob[x, 0] = aryClmBlob[0];
                    continue;
                }
                //

                BlobIndexDist uBlob;
                BlobIndexDist dBlob;
                while (true)
                {
                    // 上検索　起点ブロブに対して、-45°～-135°以内（上方向）にあり、最も距離の短いブロブを見つける
                    uBlob = new BlobIndexDist();
                    double min = double.MaxValue;
                    for (int n = 0; n < lstBlobs.Count; n++)
                    {
                        double r = Math.Sqrt(Math.Pow(lstBlobs[n].Centroid.X - u_ref.X, 2) + Math.Pow(lstBlobs[n].Centroid.Y - u_ref.Y, 2));
                        double deltaY = lstBlobs[n].Centroid.Y - u_ref.Y;  // 上にある場合、負の数になる
                        double sin = deltaY / r;
                        double asin = Math.Asin(sin) / (2.0 * Math.PI) * 360;
                        if (-135.0 < asin && asin < -45.0)
                        {
                            if (min > r)
                            {
                                min = r;
                                uBlob.Set(n, r);
                            }
                        }
                    }
                    // 下検索　起点ブロブに対して、45°～135°以内（下方向）にあり、最も距離の短いブロブを見つける
                    dBlob = new BlobIndexDist();
                    min = double.MaxValue;
                    for (int n = 0; n < lstBlobs.Count; n++)
                    {
                        double r = Math.Sqrt(Math.Pow(lstBlobs[n].Centroid.X - d_ref.X, 2) + Math.Pow(lstBlobs[n].Centroid.Y - d_ref.Y, 2));
                        double deltaY = lstBlobs[n].Centroid.Y - d_ref.Y;  // 下にある場合、正の数になる
                        double sin = deltaY / r;
                        double asin = Math.Asin(sin) / (2.0 * Math.PI) * 360;
                        if (45.0 < asin && asin < 135.0)
                        {
                            if (min > r)
                            {
                                min = r;
                                dBlob.Set(n, r);
                            }
                        }
                    }

                    // 上下それぞれ見つけたブロブ座標を比較して、距離の短い方を採用
                    if (uBlob.index == -1 && dBlob.index == -1)
                    {
                        // 上下いずれも見つけられなかった場合はエラー
                        throw new Exception(string.Format("Fail to sort blob. (H={0})", x));
                    }
                    else if (dBlob.index == -1 || uBlob.dist <= dBlob.dist)
                    {
                        // 上だけ見つけた　または　上の方の距離が短い場合
                        aryClmBlob[index] = lstBlobs[uBlob.index].Clone();  // 登録
                        u_ref = new Point2d(lstBlobs[uBlob.index].Centroid.X, lstBlobs[uBlob.index].Centroid.Y);    // 上検索用起点ブロブ
                        lstBlobs.RemoveAt(uBlob.index);    // 検索対象から削除
                    }
                    else if (uBlob.index == -1 || uBlob.dist > dBlob.dist)
                    {
                        // 下だけ見つけた　または　下の方の距離が短い場合
                        aryClmBlob[index] = lstBlobs[dBlob.index].Clone();  // 登録
                        d_ref = new Point2d(lstBlobs[dBlob.index].Centroid.X, lstBlobs[dBlob.index].Centroid.Y);    // 下検索用起点ブロブ
                        lstBlobs.RemoveAt(dBlob.index);    // 検索対象から削除
                    }

                    index++;
                    // 一列分のブロブが登録されたら、
                    if (index == vNum)
                    {
                        // V位置でソート
                        var sortBlob = aryClmBlob.OrderBy(blob => blob.Centroid.Y);
                        int count = 0;
                        foreach (UfCamMeasArea blob in sortBlob)
                        {
                            aryBlob[x, count++] = blob;
                        }
                        break;
                    }
                }
            }
        }

        private void CalcCrossPoint(RectangleArea area, out Coordinate[] cp)
        {　// 3x3分割の16交点にしか対応していない
            cp = new Coordinate[16];

            cp[0] = new Coordinate(area.TopLeft.X, area.TopLeft.Y);
            cp[3] = new Coordinate(area.TopRight.X, area.TopRight.Y);
            cp[12] = new Coordinate(area.BottomLeft.X, area.BottomLeft.Y);
            cp[15] = new Coordinate(area.BottomRight.X, area.BottomRight.Y);

            cp[1] = new Coordinate(cp[0].X + (cp[3].X - cp[0].X) / 3, cp[0].Y + (cp[3].Y - cp[0].Y) / 3);
            cp[2] = new Coordinate(cp[0].X + (cp[3].X - cp[0].X) / 3 * 2, cp[0].Y + (cp[3].Y - cp[0].Y) / 3 * 2);

            cp[4] = new Coordinate(cp[0].X + (cp[12].X - cp[0].X) / 3, cp[0].Y + (cp[12].Y - cp[0].Y) / 3);
            cp[8] = new Coordinate(cp[0].X + (cp[12].X - cp[0].X) / 3 * 2, cp[0].Y + (cp[12].Y - cp[0].Y) / 3 * 2);

            cp[7] = new Coordinate(cp[3].X + (cp[15].X - cp[3].X) / 3, cp[3].Y + (cp[15].Y - cp[3].Y) / 3);
            cp[11] = new Coordinate(cp[3].X + (cp[15].X - cp[3].X) / 3 * 2, cp[3].Y + (cp[15].Y - cp[3].Y) / 3 * 2);

            cp[13] = new Coordinate(cp[12].X + (cp[15].X - cp[12].X) / 3, cp[12].Y + (cp[15].Y - cp[12].Y) / 3);
            cp[14] = new Coordinate(cp[12].X + (cp[15].X - cp[12].X) / 3 * 2, cp[12].Y + (cp[15].Y - cp[12].Y) / 3 * 2);

            cp[5] = new Coordinate(cp[4].X + (cp[7].X - cp[4].X) / 3, cp[4].Y + (cp[7].Y - cp[4].Y) / 3);
            cp[6] = new Coordinate(cp[4].X + (cp[7].X - cp[4].X) / 3 * 2, cp[4].Y + (cp[7].Y - cp[4].Y) / 3 * 2);

            cp[9] = new Coordinate(cp[8].X + (cp[11].X - cp[8].X) / 3, cp[8].Y + (cp[11].Y - cp[8].Y) / 3);
            cp[10] = new Coordinate(cp[8].X + (cp[11].X - cp[8].X) / 3 * 2, cp[8].Y + (cp[11].Y - cp[8].Y) / 3 * 2);
        }

        /// <summary>
        /// 各測定点の画素値を格納する。格納時には校正テーブルによる補正が掛かる。
        /// </summary>
        /// <param name="path">UF測定Log保存フォルダ</param>
        private void storeUfCamPv(string path)
        {
            // RAW画像を読み込み
            AcqARW arwR, arwG, arwB, /*arwW,*/ arwBlack;

            winProgress.ShowMessage("Load Raw Picture.(Red)");
            saveUfLog("Load Raw Picture.(Red)");
            loadArwFile(path + fn_FlatRed + ".arw", out arwR);
            winProgress.PutForward1Step();

            winProgress.ShowMessage("Load Raw Picture.(Green)");
            saveUfLog("Load Raw Picture.(Green)");
            loadArwFile(path + fn_FlatGreen + ".arw", out arwG);
            winProgress.PutForward1Step();

            winProgress.ShowMessage("Load Raw Picture.(Blue)");
            saveUfLog("Load Raw Picture.(Blue)");
            loadArwFile(path + fn_FlatBlue + ".arw", out arwB);
            winProgress.PutForward1Step();

            winProgress.ShowMessage("Load Raw Picture.(Black)");
            saveUfLog("Load Raw Picture.(Black)");
            loadArwFile(path + fn_BlackFile + ".arw", out arwBlack);
            winProgress.PutForward1Step();

            //winProgress.ShowMessage("Load Raw Picture. (White)");
            //loadArwFile(path + fn_FlatWhite + ".arw", out arwW);
            //winProgress.PutForward1Step();

            foreach (UfCamMeasValue ufMv in ufCamMeasLog.lstUfCamMeas)
            {
                for (int mod = 0; mod < moduleCount; mod++)
                {
                    for (int pos = 0; pos < UfMeasSplitCount; pos++)
                    {
                        calcAverage(arwR, arwBlack, ufMv.aryUfCamModules[mod].aryUfCamMp[pos].CamArea, out PixelValue pvR, Settings.Ins.Camera.MeasAreaBlanking);
                        calcAverage(arwG, arwBlack, ufMv.aryUfCamModules[mod].aryUfCamMp[pos].CamArea, out PixelValue pvG, Settings.Ins.Camera.MeasAreaBlanking);
                        calcAverage(arwB, arwBlack, ufMv.aryUfCamModules[mod].aryUfCamMp[pos].CamArea, out PixelValue pvB, Settings.Ins.Camera.MeasAreaBlanking);
                        //calcAverage(arwW, ufMv.aryUfCamModules[mod].aryUfCamMp[pos].CamArea, out PixelValue pvW_);
                        calcAverageWhite(arwR, arwG, arwB, arwBlack, ufMv.aryUfCamModules[mod].aryUfCamMp[pos].CamArea, out PixelValue pvW, Settings.Ins.Camera.MeasAreaBlanking);

                        ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueR = pvR.R / (ufMv.aryUfCamModules[mod].aryUfCamMp[pos].CCV.CvGainR * ufMv.aryUfCamModules[mod].aryUfCamMp[pos].LCV.CvGainR);
                        ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PvRawR = pvR.R;

                        ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueG = pvG.G / (ufMv.aryUfCamModules[mod].aryUfCamMp[pos].CCV.CvGainG * ufMv.aryUfCamModules[mod].aryUfCamMp[pos].LCV.CvGainG);
                        ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PvRawG = pvG.G;

                        ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueB = pvB.B / (ufMv.aryUfCamModules[mod].aryUfCamMp[pos].CCV.CvGainB * ufMv.aryUfCamModules[mod].aryUfCamMp[pos].LCV.CvGainB);
                        ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PvRawB = pvB.B;

                        ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueW = (pvW.R + pvW.G + pvW.B) / (ufMv.aryUfCamModules[mod].aryUfCamMp[pos].CCV.CvGainG * ufMv.aryUfCamModules[mod].aryUfCamMp[pos].LCV.CvGainG);
                        ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PvRawW = pvW.R + pvW.G + pvW.B;

                        // 色度バランス
                        ufMv.aryUfCamModules[mod].aryUfCamMp[pos].ChromBalanceR = ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueR / ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueW;
                        ufMv.aryUfCamModules[mod].aryUfCamMp[pos].ChromBalanceG = ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueG / ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueW;
                        ufMv.aryUfCamModules[mod].aryUfCamMp[pos].ChromBalanceB = ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueB / ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueW;
                    }
                }
            }
        }

        private void dispUfMeasResult()
        {
            // 測定データがない場合はエラー
            if(ufCamMeasLog.lstUfCamMeas.Count == 0)
            { throw new Exception("Measurement data does not exist.\r\nMeasurement may not have been performed correctly."); }

            #region 描画サイズ指定
            
            int ModuleCountY; // Cabinett内でModuleがY方向にいくつあるか

            // 調整点の描画サイズ
            int MeasPosSizeH;
            int MeasPosSizeV;
            if (allocInfo.LEDModel == ZRD_CH12D 
                || allocInfo.LEDModel == ZRD_CH15D 
                || allocInfo.LEDModel == ZRD_BH12D 
                || allocInfo.LEDModel == ZRD_BH15D
                || allocInfo.LEDModel == ZRD_CH12D_S3
                || allocInfo.LEDModel == ZRD_CH15D_S3
                || allocInfo.LEDModel == ZRD_BH12D_S3
                || allocInfo.LEDModel == ZRD_BH15D_S3) // Cancun
            {
#if (DEBUG || Release_log)
                if (Settings.Ins.ExecLog == true)
                {
                    MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                }
#endif
                ModuleCountY = 3;

                MeasPosSizeH = 6; // Moduleの縦横比は0.75倍(Chiron)
                MeasPosSizeV = 5;
            }
            else // Chiron
            {
#if (DEBUG || Release_log)
                if (Settings.Ins.ExecLog == true)
                {
                    MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                }
#endif
                ModuleCountY = 2; // Cabinett内でModuleがY方向にいくつあるか

                // 調整点の描画サイズ
                MeasPosSizeH = 6; // Moduleの縦横比は1.125倍(Chiron)
                MeasPosSizeV = 7;
            }

            int MeasUnitSizeH = MeasPosSizeH * 3 * ModuleCountX;
            int MeasUnitSizeV = MeasPosSizeV * 3 * ModuleCountY;

            int MeasCellSizeH = MeasPosSizeH * 3;
            int MeasCellSizeV = MeasPosSizeV * 3;

            #endregion 描画サイズ指定

            #region 計算

            // Summary計算
            double minW = double.MaxValue, minR = double.MaxValue, minG = double.MaxValue, minB = double.MaxValue;
            double maxW = 0, maxR = 0, maxG = 0, maxB = 0;
            double aveW = 0, aveR = 0, aveG = 0, aveB = 0;
            double chromAveR = 0, chromAveG = 0, chromAveB = 0;
            int count = 0; // 測定点の総数（測定範囲全体）
            List<double> lstPcPvW = new List<double>(), lstPcPvR = new List<double>(), lstPcPvG = new List<double>(), lstPcPvB = new List<double>();

            foreach (UfCamMeasValue ufMv in ufCamMeasLog.lstUfCamMeas)
            {
                for (int mod = 0; mod < moduleCount; mod++)
                {
                    for (int pos = 0; pos < UfMeasSplitCount; pos++)
                    {
                        // Min
                        if (ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueW < minW)
                        { minW = ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueW; }
                        if (ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueR < minR)
                        { minR = ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueR; }
                        if (ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueG < minG)
                        { minG = ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueG; }
                        if (ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueB < minB)
                        { minB = ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueB; }

                        // Max
                        if (ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueW > maxW)
                        { maxW = ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueW; }
                        if (ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueR > maxR)
                        { maxR = ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueR; }
                        if (ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueG > maxG)
                        { maxG = ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueG; }
                        if (ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueB > maxB)
                        { maxB = ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueB; }

                        // Ave.
                        aveW += ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueW;
                        aveR += ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueR;
                        aveG += ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueG;
                        aveB += ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueB;

                        // 色度バランス平均値
                        chromAveR += ufMv.aryUfCamModules[mod].aryUfCamMp[pos].ChromBalanceR;
                        chromAveG += ufMv.aryUfCamModules[mod].aryUfCamMp[pos].ChromBalanceG;
                        chromAveB += ufMv.aryUfCamModules[mod].aryUfCamMp[pos].ChromBalanceB;

                        count++;
                    }
                }
            }

            // 0除算対策
            if (count == 0)
            { count = 1; }

            aveW /= (double)count;
            aveR /= (double)count;
            aveG /= (double)count;
            aveB /= (double)count;

            chromAveR /= (double)count;
            chromAveG /= (double)count;
            chromAveB /= (double)count;

            double avePcW = 0, avePcR = 0, avePcG = 0, avePcB = 0;

            // 平均に対してどの程度ずれているか計算する
            foreach (UfCamMeasValue ufMv in ufCamMeasLog.lstUfCamMeas)
            {
                // 隣接測定点間差分計算のために右隣り、下隣のCabinetを捜索
                UfCamMeasValue ufMv_R = null, ufMv_B = null;

                foreach(UfCamMeasValue uf in ufCamMeasLog.lstUfCamMeas)
                {
                    // 右隣り
                    if(uf.Unit.X == ufMv.Unit.X + 1 && uf.Unit.Y == ufMv.Unit.Y)
                    { ufMv_R = uf; }

                    // 下隣り
                    if (uf.Unit.X == ufMv.Unit.X && uf.Unit.Y == ufMv.Unit.Y + 1)
                    { ufMv_B = uf; }
                }

                for (int mod = 0; mod < moduleCount; mod++)
                {
                    #region 隣接Module間との画素値差を計算

                    UfCamModule ufMod_R = null, ufMod_B = null;

                    if (moduleCount == ModuleCount_Module4x2) // Chiron
                    {
                        if (mod == 0)
                        {
                            ufMod_R = ufMv.aryUfCamModules[1];
                            ufMod_B = ufMv.aryUfCamModules[4];
                        }
                        else if (mod == 1)
                        {
                            ufMod_R = ufMv.aryUfCamModules[2];
                            ufMod_B = ufMv.aryUfCamModules[5];
                        }
                        else if (mod == 2)
                        {
                            ufMod_R = ufMv.aryUfCamModules[3];
                            ufMod_B = ufMv.aryUfCamModules[6];
                        }
                        else if (mod == 3)
                        {
                            if (ufMv_R != null)
                            { ufMod_R = ufMv_R.aryUfCamModules[0]; }
                            ufMod_B = ufMv.aryUfCamModules[7];
                        }
                        else if (mod == 4)
                        {
                            ufMod_R = ufMv.aryUfCamModules[5];
                            if (ufMv_B != null)
                            { ufMod_B = ufMv_B.aryUfCamModules[0]; }
                        }
                        else if (mod == 5)
                        {
                            ufMod_R = ufMv.aryUfCamModules[6];
                            if (ufMv_B != null)
                            { ufMod_B = ufMv_B.aryUfCamModules[1]; }
                        }
                        else if (mod == 6)
                        {
                            ufMod_R = ufMv.aryUfCamModules[7];
                            if (ufMv_B != null)
                            { ufMod_B = ufMv_B.aryUfCamModules[2]; }
                        }
                        else if (mod == 7)
                        {
                            if (ufMv_R != null)
                            { ufMod_R = ufMv_R.aryUfCamModules[4]; }
                            if (ufMv_B != null)
                            { ufMod_B = ufMv_B.aryUfCamModules[3]; }
                        }
                    }
                    else // ModuleCount_Module4x3 Cancun
                    {
                        if (mod == 0)
                        {
                            ufMod_R = ufMv.aryUfCamModules[1];
                            ufMod_B = ufMv.aryUfCamModules[4];
                        }
                        else if (mod == 1)
                        {
                            ufMod_R = ufMv.aryUfCamModules[2];
                            ufMod_B = ufMv.aryUfCamModules[5];
                        }
                        else if (mod == 2)
                        {
                            ufMod_R = ufMv.aryUfCamModules[3];
                            ufMod_B = ufMv.aryUfCamModules[6];
                        }
                        else if (mod == 3)
                        {
                            if (ufMv_R != null)
                            { ufMod_R = ufMv_R.aryUfCamModules[0]; }
                            ufMod_B = ufMv.aryUfCamModules[7];
                        }
                        else if (mod == 4)
                        {
                            ufMod_R = ufMv.aryUfCamModules[5];
                            ufMod_B = ufMv.aryUfCamModules[8];
                        }
                        else if (mod == 5)
                        {
                            ufMod_R = ufMv.aryUfCamModules[6];
                            ufMod_B = ufMv.aryUfCamModules[9];
                        }
                        else if (mod == 6)
                        {
                            ufMod_R = ufMv.aryUfCamModules[7];
                            ufMod_B = ufMv.aryUfCamModules[10];
                        }
                        else if (mod == 7)
                        {
                            if (ufMv_R != null)
                            { ufMod_R = ufMv_R.aryUfCamModules[4]; }
                            ufMod_B = ufMv.aryUfCamModules[11];
                        }
                        else if (mod == 8)
                        {
                            ufMod_R = ufMv.aryUfCamModules[9];
                            if (ufMv_B != null)
                            { ufMod_B = ufMv_B.aryUfCamModules[0]; }
                        }
                        else if (mod == 9)
                        {
                            ufMod_R = ufMv.aryUfCamModules[10];
                            if (ufMv_B != null)
                            { ufMod_B = ufMv_B.aryUfCamModules[1]; }
                        }
                        else if (mod == 10)
                        {
                            ufMod_R = ufMv.aryUfCamModules[11];
                            if (ufMv_B != null)
                            { ufMod_B = ufMv_B.aryUfCamModules[2]; }
                        }
                        else if (mod == 11)
                        {
                            if (ufMv_R != null)
                            { ufMod_R = ufMv_R.aryUfCamModules[8]; }
                            if (ufMv_B != null)
                            { ufMod_B = ufMv_B.aryUfCamModules[3]; }
                        }
                    }

                    // Right
                    if (ufMod_R != null)
                    {
                        // 境界の画素値平均を計算
                        double pvRW = (ufMv.aryUfCamModules[mod].aryUfCamMp[2].PixelValueW + ufMv.aryUfCamModules[mod].aryUfCamMp[5].PixelValueW + ufMv.aryUfCamModules[mod].aryUfCamMp[8].PixelValueW
                                    + ufMod_R.aryUfCamMp[0].PixelValueW + +ufMod_R.aryUfCamMp[3].PixelValueW + ufMod_R.aryUfCamMp[6].PixelValueW) / 6.0;
                        double pvRR = (ufMv.aryUfCamModules[mod].aryUfCamMp[2].PixelValueR + ufMv.aryUfCamModules[mod].aryUfCamMp[5].PixelValueR + ufMv.aryUfCamModules[mod].aryUfCamMp[8].PixelValueR
                                    + ufMod_R.aryUfCamMp[0].PixelValueR + +ufMod_R.aryUfCamMp[3].PixelValueR + ufMod_R.aryUfCamMp[6].PixelValueR) / 6.0;
                        double pvRG = (ufMv.aryUfCamModules[mod].aryUfCamMp[2].PixelValueG + ufMv.aryUfCamModules[mod].aryUfCamMp[5].PixelValueG + ufMv.aryUfCamModules[mod].aryUfCamMp[8].PixelValueG
                                    + ufMod_R.aryUfCamMp[0].PixelValueG + +ufMod_R.aryUfCamMp[3].PixelValueG + ufMod_R.aryUfCamMp[6].PixelValueG) / 6.0;
                        double pvRB = (ufMv.aryUfCamModules[mod].aryUfCamMp[2].PixelValueB + ufMv.aryUfCamModules[mod].aryUfCamMp[5].PixelValueB + ufMv.aryUfCamModules[mod].aryUfCamMp[8].PixelValueB
                                    + ufMod_R.aryUfCamMp[0].PixelValueB + +ufMod_R.aryUfCamMp[3].PixelValueB + ufMod_R.aryUfCamMp[6].PixelValueB) / 6.0;

                        // Gap[%]を計算
                        //ufMv.aryUfCamModules[mod].PcPvGapRightW = ((ufMv.aryUfCamModules[mod].aryUfCamMp[2].PixelValueW + ufMv.aryUfCamModules[mod].aryUfCamMp[5].PixelValueW + ufMv.aryUfCamModules[mod].aryUfCamMp[8].PixelValueW)
                        //            - (ufMod_R.aryUfCamMp[0].PixelValueW + +ufMod_R.aryUfCamMp[3].PixelValueW + ufMod_R.aryUfCamMp[6].PixelValueW)) / 3 / pvRW * 100; // 3箇所の平均なので3で割って、％なので100をかける
                        ufMv.aryUfCamModules[mod].PcPvGapRightW = (ufMv.aryUfCamModules[mod].aryUfCamMp[4].PixelValueW - ufMod_R.aryUfCamMp[4].PixelValueW) / pvRW * 100; // 中心比較に変更
                        ufMv.aryUfCamModules[mod].PcPvGapRightR = ((ufMv.aryUfCamModules[mod].aryUfCamMp[2].PixelValueR + ufMv.aryUfCamModules[mod].aryUfCamMp[5].PixelValueR + ufMv.aryUfCamModules[mod].aryUfCamMp[8].PixelValueR)
                                    - (ufMod_R.aryUfCamMp[0].PixelValueR + +ufMod_R.aryUfCamMp[3].PixelValueR + ufMod_R.aryUfCamMp[6].PixelValueR)) / 3 / pvRR * 100;
                        ufMv.aryUfCamModules[mod].PcPvGapRightG = ((ufMv.aryUfCamModules[mod].aryUfCamMp[2].PixelValueG + ufMv.aryUfCamModules[mod].aryUfCamMp[5].PixelValueG + ufMv.aryUfCamModules[mod].aryUfCamMp[8].PixelValueG)
                                    - (ufMod_R.aryUfCamMp[0].PixelValueG + +ufMod_R.aryUfCamMp[3].PixelValueG + ufMod_R.aryUfCamMp[6].PixelValueG)) / 3 / pvRG * 100;
                        ufMv.aryUfCamModules[mod].PcPvGapRightB = ((ufMv.aryUfCamModules[mod].aryUfCamMp[2].PixelValueB + ufMv.aryUfCamModules[mod].aryUfCamMp[5].PixelValueB + ufMv.aryUfCamModules[mod].aryUfCamMp[8].PixelValueB)
                                    - (ufMod_R.aryUfCamMp[0].PixelValueB + +ufMod_R.aryUfCamMp[3].PixelValueB + ufMod_R.aryUfCamMp[6].PixelValueB)) / 3 / pvRB * 100;
                    }

                    // Bottom
                    if (ufMod_B != null)
                    {
                        // 境界の画素値平均を計算
                        double pvBW = (ufMv.aryUfCamModules[mod].aryUfCamMp[6].PixelValueW + ufMv.aryUfCamModules[mod].aryUfCamMp[7].PixelValueW + ufMv.aryUfCamModules[mod].aryUfCamMp[8].PixelValueW
                                    + ufMod_B.aryUfCamMp[0].PixelValueW + +ufMod_B.aryUfCamMp[1].PixelValueW + ufMod_B.aryUfCamMp[2].PixelValueW) / 6.0;
                        double pvBR = (ufMv.aryUfCamModules[mod].aryUfCamMp[6].PixelValueR + ufMv.aryUfCamModules[mod].aryUfCamMp[7].PixelValueR + ufMv.aryUfCamModules[mod].aryUfCamMp[8].PixelValueR
                                    + ufMod_B.aryUfCamMp[0].PixelValueR + +ufMod_B.aryUfCamMp[1].PixelValueR + ufMod_B.aryUfCamMp[2].PixelValueR) / 6.0;
                        double pvBG = (ufMv.aryUfCamModules[mod].aryUfCamMp[6].PixelValueG + ufMv.aryUfCamModules[mod].aryUfCamMp[7].PixelValueG + ufMv.aryUfCamModules[mod].aryUfCamMp[8].PixelValueG
                                    + ufMod_B.aryUfCamMp[0].PixelValueG + +ufMod_B.aryUfCamMp[1].PixelValueG + ufMod_B.aryUfCamMp[2].PixelValueG) / 6.0;
                        double pvBB = (ufMv.aryUfCamModules[mod].aryUfCamMp[6].PixelValueB + ufMv.aryUfCamModules[mod].aryUfCamMp[7].PixelValueB + ufMv.aryUfCamModules[mod].aryUfCamMp[8].PixelValueB
                                    + ufMod_B.aryUfCamMp[0].PixelValueB + +ufMod_B.aryUfCamMp[1].PixelValueB + ufMod_B.aryUfCamMp[2].PixelValueB) / 6.0;

                        // Gap[%]を計算
                        //ufMv.aryUfCamModules[mod].PcPvGapBottomW = ((ufMv.aryUfCamModules[mod].aryUfCamMp[6].PixelValueW + ufMv.aryUfCamModules[mod].aryUfCamMp[7].PixelValueW + ufMv.aryUfCamModules[mod].aryUfCamMp[8].PixelValueW)
                        //            - (ufMod_B.aryUfCamMp[0].PixelValueW + +ufMod_B.aryUfCamMp[1].PixelValueW + ufMod_B.aryUfCamMp[2].PixelValueW)) / 3 / pvBW * 100;
                        ufMv.aryUfCamModules[mod].PcPvGapBottomW = (ufMv.aryUfCamModules[mod].aryUfCamMp[4].PixelValueW - ufMod_B.aryUfCamMp[4].PixelValueW) / pvBW * 100;
                        ufMv.aryUfCamModules[mod].PcPvGapBottomR = ((ufMv.aryUfCamModules[mod].aryUfCamMp[6].PixelValueR + ufMv.aryUfCamModules[mod].aryUfCamMp[7].PixelValueR + ufMv.aryUfCamModules[mod].aryUfCamMp[8].PixelValueR)
                                    - (ufMod_B.aryUfCamMp[0].PixelValueR + +ufMod_B.aryUfCamMp[1].PixelValueR + ufMod_B.aryUfCamMp[2].PixelValueR)) / 3 / pvBR * 100;
                        ufMv.aryUfCamModules[mod].PcPvGapBottomG = ((ufMv.aryUfCamModules[mod].aryUfCamMp[6].PixelValueG + ufMv.aryUfCamModules[mod].aryUfCamMp[7].PixelValueG + ufMv.aryUfCamModules[mod].aryUfCamMp[8].PixelValueG)
                                    - (ufMod_B.aryUfCamMp[0].PixelValueG + +ufMod_B.aryUfCamMp[1].PixelValueG + ufMod_B.aryUfCamMp[2].PixelValueG)) / 3 / pvBG * 100;
                        ufMv.aryUfCamModules[mod].PcPvGapBottomB = ((ufMv.aryUfCamModules[mod].aryUfCamMp[6].PixelValueB + ufMv.aryUfCamModules[mod].aryUfCamMp[7].PixelValueB + ufMv.aryUfCamModules[mod].aryUfCamMp[8].PixelValueB)
                                    - (ufMod_B.aryUfCamMp[0].PixelValueB + +ufMod_B.aryUfCamMp[1].PixelValueB + ufMod_B.aryUfCamMp[2].PixelValueB)) / 3 / pvBB * 100;
                    }

                    #endregion 隣接Module間との画素値差を計算

                    for (int pos = 0; pos < UfMeasSplitCount; pos++)
                    {
                        ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvW = (ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueW - aveW) * 100 / aveW;
                        ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvR = (ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueR - aveR) * 100 / aveR;
                        ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvG = (ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueG - aveG) * 100 / aveG;
                        ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvB = (ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueB - aveB) * 100 / aveB;

                        lstPcPvW.Add(ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvW);
                        lstPcPvR.Add(ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvR);
                        lstPcPvG.Add(ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvG);
                        lstPcPvB.Add(ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvB);

                        avePcW += Math.Abs(ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvW);
                        avePcR += Math.Abs(ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvR);
                        avePcG += Math.Abs(ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvG);
                        avePcB += Math.Abs(ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvB);

                        ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcChromR = (ufMv.aryUfCamModules[mod].aryUfCamMp[pos].ChromBalanceR - chromAveR) * 100 / chromAveR;
                        ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcChromG = (ufMv.aryUfCamModules[mod].aryUfCamMp[pos].ChromBalanceG - chromAveG) * 100 / chromAveG;
                        ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcChromB = (ufMv.aryUfCamModules[mod].aryUfCamMp[pos].ChromBalanceB - chromAveB) * 100 / chromAveB;

                        #region 隣接測定点との差分を計算(中止)

                        //double pvW = ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueW;
                        //double pvR = ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueR;
                        //double pvG = ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueG;
                        //double pvB = ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PixelValueB;

                        //if (pos == 0)
                        //{
                        //    // Right
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightW = (pvW - ufMv.aryUfCamModules[mod].aryUfCamMp[1].PixelValueW) / aveW;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightR = (pvR - ufMv.aryUfCamModules[mod].aryUfCamMp[1].PixelValueR) / aveR;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightG = (pvG - ufMv.aryUfCamModules[mod].aryUfCamMp[1].PixelValueG) / aveG;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightB = (pvB - ufMv.aryUfCamModules[mod].aryUfCamMp[1].PixelValueB) / aveB;

                        //    // Bottom
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightW = (pvW - ufMv.aryUfCamModules[mod].aryUfCamMp[3].PixelValueW) / aveW;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightR = (pvR - ufMv.aryUfCamModules[mod].aryUfCamMp[3].PixelValueR) / aveR;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightG = (pvG - ufMv.aryUfCamModules[mod].aryUfCamMp[3].PixelValueG) / aveG;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightB = (pvB - ufMv.aryUfCamModules[mod].aryUfCamMp[3].PixelValueB) / aveB;
                        //}
                        //else if (pos == 1)
                        //{
                        //    // Right
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightW = (pvW - ufMv.aryUfCamModules[mod].aryUfCamMp[2].PixelValueW) / aveW;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightR = (pvR - ufMv.aryUfCamModules[mod].aryUfCamMp[2].PixelValueR) / aveR;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightG = (pvG - ufMv.aryUfCamModules[mod].aryUfCamMp[2].PixelValueG) / aveG;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightB = (pvB - ufMv.aryUfCamModules[mod].aryUfCamMp[2].PixelValueB) / aveB;

                        //    // Bottom
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightW = (pvW - ufMv.aryUfCamModules[mod].aryUfCamMp[4].PixelValueW) / aveW;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightR = (pvR - ufMv.aryUfCamModules[mod].aryUfCamMp[4].PixelValueR) / aveR;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightG = (pvG - ufMv.aryUfCamModules[mod].aryUfCamMp[4].PixelValueG) / aveG;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightB = (pvB - ufMv.aryUfCamModules[mod].aryUfCamMp[4].PixelValueB) / aveB;
                        //}
                        //else if (pos == 2)
                        //{
                        //    // Right
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightW = (pvW - ufMod_R.aryUfCamMp[0].PixelValueW) / aveW;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightR = (pvR - ufMod_R.aryUfCamMp[0].PixelValueR) / aveR;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightG = (pvG - ufMod_R.aryUfCamMp[0].PixelValueG) / aveG;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightB = (pvB - ufMod_R.aryUfCamMp[0].PixelValueB) / aveB;

                        //    // Bottom
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightW = (pvW - ufMv.aryUfCamModules[mod].aryUfCamMp[5].PixelValueW) / aveW;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightR = (pvR - ufMv.aryUfCamModules[mod].aryUfCamMp[5].PixelValueR) / aveR;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightG = (pvG - ufMv.aryUfCamModules[mod].aryUfCamMp[5].PixelValueG) / aveG;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightB = (pvB - ufMv.aryUfCamModules[mod].aryUfCamMp[5].PixelValueB) / aveB;
                        //}
                        //else if (pos == 3)
                        //{
                        //    // Right
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightW = (pvW - ufMv.aryUfCamModules[mod].aryUfCamMp[4].PixelValueW) / aveW;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightR = (pvR - ufMv.aryUfCamModules[mod].aryUfCamMp[4].PixelValueR) / aveR;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightG = (pvG - ufMv.aryUfCamModules[mod].aryUfCamMp[4].PixelValueG) / aveG;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightB = (pvB - ufMv.aryUfCamModules[mod].aryUfCamMp[4].PixelValueB) / aveB;

                        //    // Bottom
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightW = (pvW - ufMv.aryUfCamModules[mod].aryUfCamMp[6].PixelValueW) / aveW;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightR = (pvR - ufMv.aryUfCamModules[mod].aryUfCamMp[6].PixelValueR) / aveR;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightG = (pvG - ufMv.aryUfCamModules[mod].aryUfCamMp[6].PixelValueG) / aveG;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightB = (pvB - ufMv.aryUfCamModules[mod].aryUfCamMp[6].PixelValueB) / aveB;
                        //}
                        //else if (pos == 4)
                        //{
                        //    // Right
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightW = (pvW - ufMv.aryUfCamModules[mod].aryUfCamMp[5].PixelValueW) / aveW;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightR = (pvR - ufMv.aryUfCamModules[mod].aryUfCamMp[5].PixelValueR) / aveR;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightG = (pvG - ufMv.aryUfCamModules[mod].aryUfCamMp[5].PixelValueG) / aveG;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightB = (pvB - ufMv.aryUfCamModules[mod].aryUfCamMp[5].PixelValueB) / aveB;

                        //    // Bottom
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightW = (pvW - ufMv.aryUfCamModules[mod].aryUfCamMp[7].PixelValueW) / aveW;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightR = (pvR - ufMv.aryUfCamModules[mod].aryUfCamMp[7].PixelValueR) / aveR;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightG = (pvG - ufMv.aryUfCamModules[mod].aryUfCamMp[7].PixelValueG) / aveG;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightB = (pvB - ufMv.aryUfCamModules[mod].aryUfCamMp[7].PixelValueB) / aveB;
                        //}
                        //else if (pos == 5)
                        //{
                        //    // Right
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightW = (pvW - ufMod_R.aryUfCamMp[3].PixelValueW) / aveW;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightR = (pvR - ufMod_R.aryUfCamMp[3].PixelValueR) / aveR;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightG = (pvG - ufMod_R.aryUfCamMp[3].PixelValueG) / aveG;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightB = (pvB - ufMod_R.aryUfCamMp[3].PixelValueB) / aveB;

                        //    // Bottom
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightW = (pvW - ufMv.aryUfCamModules[mod].aryUfCamMp[8].PixelValueW) / aveW;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightR = (pvR - ufMv.aryUfCamModules[mod].aryUfCamMp[8].PixelValueR) / aveR;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightG = (pvG - ufMv.aryUfCamModules[mod].aryUfCamMp[8].PixelValueG) / aveG;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightB = (pvB - ufMv.aryUfCamModules[mod].aryUfCamMp[8].PixelValueB) / aveB;
                        //}
                        //else if (pos == 6)
                        //{
                        //    // Right
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightW = (pvW - ufMv.aryUfCamModules[mod].aryUfCamMp[7].PixelValueW) / aveW;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightR = (pvR - ufMv.aryUfCamModules[mod].aryUfCamMp[7].PixelValueR) / aveR;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightG = (pvG - ufMv.aryUfCamModules[mod].aryUfCamMp[7].PixelValueG) / aveG;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightB = (pvB - ufMv.aryUfCamModules[mod].aryUfCamMp[7].PixelValueB) / aveB;

                        //    // Bottom
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightW = (pvW - ufMod_B.aryUfCamMp[0].PixelValueW) / aveW;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightR = (pvR - ufMod_B.aryUfCamMp[0].PixelValueR) / aveR;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightG = (pvG - ufMod_B.aryUfCamMp[0].PixelValueG) / aveG;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightB = (pvB - ufMod_B.aryUfCamMp[0].PixelValueB) / aveB;
                        //}
                        //else if (pos == 7)
                        //{
                        //    // Right
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightW = (pvW - ufMv.aryUfCamModules[mod].aryUfCamMp[8].PixelValueW) / aveW;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightR = (pvR - ufMv.aryUfCamModules[mod].aryUfCamMp[8].PixelValueR) / aveR;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightG = (pvG - ufMv.aryUfCamModules[mod].aryUfCamMp[8].PixelValueG) / aveG;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightB = (pvB - ufMv.aryUfCamModules[mod].aryUfCamMp[8].PixelValueB) / aveB;

                        //    // Bottom
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightW = (pvW - ufMod_B.aryUfCamMp[1].PixelValueW) / aveW;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightR = (pvR - ufMod_B.aryUfCamMp[1].PixelValueR) / aveR;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightG = (pvG - ufMod_B.aryUfCamMp[1].PixelValueG) / aveG;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightB = (pvB - ufMod_B.aryUfCamMp[1].PixelValueB) / aveB;
                        //}
                        //else if (pos == 8)
                        //{
                        //    // Right
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightW = (pvW - ufMod_R.aryUfCamMp[6].PixelValueW) / aveW;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightR = (pvR - ufMod_R.aryUfCamMp[6].PixelValueR) / aveR;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightG = (pvG - ufMod_R.aryUfCamMp[6].PixelValueG) / aveG;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightB = (pvB - ufMod_R.aryUfCamMp[6].PixelValueB) / aveB;

                        //    // Bottom
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightW = (pvW - ufMod_B.aryUfCamMp[2].PixelValueW) / aveW;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightR = (pvR - ufMod_B.aryUfCamMp[2].PixelValueR) / aveR;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightG = (pvG - ufMod_B.aryUfCamMp[2].PixelValueG) / aveG;
                        //    ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvGapRightB = (pvB - ufMod_B.aryUfCamMp[2].PixelValueB) / aveB;
                        //}

                        #endregion 隣接測定点との差分を計算
                    }
                }
            }

            avePcW /= (double)count;
            avePcR /= (double)count;
            avePcG /= (double)count;
            avePcB /= (double)count;

            double pcMaxW = (maxW - aveW) * 100 / aveW;
            double pcMinW = (minW - aveW) * 100 / aveW;
            double pcMaxR = (maxR - aveR) * 100 / aveR;
            double pcMinR = (minR - aveR) * 100 / aveR;
            double pcMaxG = (maxG - aveG) * 100 / aveG;
            double pcMinG = (minG - aveG) * 100 / aveG;
            double pcMaxB = (maxB - aveB) * 100 / aveB;
            double pcMinB = (minB - aveB) * 100 / aveB;

            setText(txbUfCamMaxW, pcMaxW.ToString("0.00"));
            setText(txbUfCamMinW, pcMinW.ToString("0.00"));
            setText(txbUfCamAveW, avePcW.ToString("0.00"));

            setText(txbUfCamMaxR, pcMaxR.ToString("0.00"));
            setText(txbUfCamMinR, pcMinR.ToString("0.00"));
            setText(txbUfCamAveR, avePcR.ToString("0.00"));

            setText(txbUfCamMaxG, pcMaxG.ToString("0.00"));
            setText(txbUfCamMinG, pcMinG.ToString("0.00"));
            setText(txbUfCamAveG, avePcG.ToString("0.00"));

            setText(txbUfCamMaxB, pcMaxB.ToString("0.00"));
            setText(txbUfCamMinB, pcMinB.ToString("0.00"));
            setText(txbUfCamAveB, avePcB.ToString("0.00"));

            setText(txbUfCamSigmaW, (standardDeviation(lstPcPvW) * 3).ToString("0.00"));
            setText(txbUfCamSigmaR, (standardDeviation(lstPcPvR) * 3).ToString("0.00"));
            setText(txbUfCamSigmaG, (standardDeviation(lstPcPvG) * 3).ToString("0.00"));
            setText(txbUfCamSigmaB, (standardDeviation(lstPcPvB) * 3).ToString("0.00"));

            #endregion 計算

            // 結果画像の生成
            // Canvasサイズの決定
            // 調整点の格納 ※矩形前提
            int StartUnitX = int.MaxValue, StartUnitY = int.MaxValue; // 対象Unitの左上のユニット位置（1ベース）
            int EndUnitX = 0, EndUnitY = 0;

            bool judge = true; // 不良Moduleが一つでもある場合はNG

            // Unitの位置を調査
            foreach (UfCamMeasValue ufMv in ufCamMeasLog.lstUfCamMeas)
            {
                if (ufMv.Unit.X < StartUnitX)
                { StartUnitX = ufMv.Unit.X; }

                if (ufMv.Unit.Y < StartUnitY)
                { StartUnitY = ufMv.Unit.Y; }

                if (ufMv.Unit.X > EndUnitX)
                { EndUnitX = ufMv.Unit.X; }

                if (ufMv.Unit.Y > EndUnitY)
                { EndUnitY = ufMv.Unit.Y; }
            }

            int LenX = EndUnitX - StartUnitX + 1; // 対象UnitのX方向Unit数
            int LenY = EndUnitY - StartUnitY + 1; // 対象UnitのY方向Unit数

            int width = 160 + MeasUnitSizeH * 2 * LenX; // 輝度と色度の2行、160はヘッダー分（適当）
            int height = 300 + MeasUnitSizeV * 4 * LenY; // WRGBの4列、300はヘッダー分（適当）

            using (Bitmap canvas = new Bitmap(width, height)) //描画先とするImageオブジェクトを作成する
            using (Graphics g = Graphics.FromImage(canvas)) //ImageオブジェクトのGraphicsオブジェクトを作成する
            // ブラシ・ペンを定義
            using (System.Drawing.Brush baseBrush = new SolidBrush(System.Drawing.Color.FromArgb(0x40, 0x40, 0x40)))
            using (System.Drawing.Brush strBrush = new SolidBrush(System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xFF)))
            using (System.Drawing.Pen penUnit = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0x20, 0x20, 0x20), 2))
            using (System.Drawing.Pen penCell = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0x60, 0x60, 0x60), 1))
            using (System.Drawing.Pen penGapNg = new System.Drawing.Pen(System.Drawing.Color.HotPink, 5))
            {
                int drawEndX = 0, drawEndY = 0; // 描画の終点(次の始点)                

                //背景を塗りつぶす
                g.FillRectangle(baseBrush, g.VisibleClipBounds);

                // フォントを定義
                Font font = new Font("Yu Gothic UI", 10, System.Drawing.FontStyle.Regular);

                // Unitの開始点とH/V Unit数を調べる
                int unitStartX = int.MaxValue, unitStartY = int.MaxValue;
                int unitEndX = 0, unitEndY = 0;
                int unitH, unitV; // 測定対象の縦横Unit数

                foreach (UfCamMeasValue ufMv in ufCamMeasLog.lstUfCamMeas)
                {
                    if (ufMv.Unit.X < unitStartX)
                    { unitStartX = ufMv.Unit.X; }

                    if (ufMv.Unit.Y < unitStartY)
                    { unitStartY = ufMv.Unit.Y; }

                    if (ufMv.Unit.X > unitEndX)
                    { unitEndX = ufMv.Unit.X; }

                    if (ufMv.Unit.Y > unitEndY)
                    { unitEndY = ufMv.Unit.Y; }
                }

                unitH = unitEndX - unitStartX + 1;
                unitV = unitEndY - unitStartY + 1;

                #region 輝度分布(Luminance Distribution)

                // White
                int startPosX = 10, startPosY = 10;
                g.DrawString("Luminance Distribution", font, strBrush, startPosX, startPosY);
                g.DrawString("White", font, strBrush, startPosX, startPosY += 30);
                startPosY += 30;

                foreach (UfCamMeasValue ufMv in ufCamMeasLog.lstUfCamMeas)
                {
                    // Cabinetの描画開始位置
                    int unitX = startPosX + MeasUnitSizeH * (ufMv.Unit.X - unitStartX);
                    int unitY = startPosY + MeasUnitSizeV * (ufMv.Unit.Y - unitStartY);

                    for (int mod = 0; mod < moduleCount; mod++)
                    {
                        int cellX = unitX + MeasCellSizeH * (mod % ModuleCountX);
                        int cellY = unitY + MeasCellSizeV * (mod / ModuleCountX);

                        //for (int pos = 0; pos < UfMeasSplitCount; pos++)
                        //{
                        //    int posX = cellX + MeasPosSizeH * (pos % 3);
                        //    int posY = cellY + MeasPosSizeV * (pos / 3);

                        //    int level = (int)(128 * (1 + ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvW * Settings.Ins.Camera.MeasureViewGain / 100));
                        //    if (level < 0)
                        //    { level = 0; }
                        //    if (level > 255)
                        //    { level = 255; }

                        //    System.Drawing.Brush fillBrush = new SolidBrush(System.Drawing.Color.FromArgb(level, level, level));
                        //    g.FillRectangle(fillBrush, posX, posY, MeasPosSizeH, MeasPosSizeV);
                        //}

                        // Moduleを単色で描画する
                        double spec = GetSpec(ufMv.Unit, mod);

                        System.Drawing.Brush fillBrush;

                        if (Math.Abs(aveW - ufMv.aryUfCamModules[mod].aryUfCamMp[4].PixelValueW) / aveW * 100 > spec)
                        {
                            fillBrush = new SolidBrush(System.Drawing.Color.FromArgb(0xFF, 0x45, 0));
                            //judge = false;
                        }
                        else
                        { fillBrush = new SolidBrush(System.Drawing.Color.FromArgb(128, 128, 128)); }

                        g.FillRectangle(fillBrush, cellX, cellY, MeasPosSizeH * 3, MeasPosSizeV * 3);

                        // Module枠を描画
                        g.DrawRectangle(penCell, cellX, cellY, MeasCellSizeH, MeasCellSizeV);

                        // Module間Gapがスペック以上だった場合赤線で表示                       
                        // Right
                        if (Math.Abs(ufMv.aryUfCamModules[mod].PcPvGapRightW) > Settings.Ins.Camera.UfGapSpecW)
                        { g.DrawLine(penGapNg, new System.Drawing.Point(cellX + MeasCellSizeH, cellY), new System.Drawing.Point(cellX + MeasCellSizeH, cellY + MeasCellSizeV)); }

                        // Bottom
                        if (Math.Abs(ufMv.aryUfCamModules[mod].PcPvGapBottomW) > Settings.Ins.Camera.UfGapSpecW)
                        { g.DrawLine(penGapNg, new System.Drawing.Point(cellX, cellY + MeasCellSizeV), new System.Drawing.Point(cellX + MeasCellSizeH, cellY + MeasCellSizeV)); }
                    }

                    // Cabinet枠を描画
                    g.DrawRectangle(penUnit, unitX, unitY, MeasUnitSizeH, MeasUnitSizeV);

                    // 描画の終点を記録
                    if (unitX + MeasUnitSizeH > drawEndX)
                    { drawEndX = unitX + MeasUnitSizeH; }

                    if (unitY + MeasUnitSizeV > drawEndY)
                    { drawEndY = unitY + MeasUnitSizeV; }
                }

                // Red
                startPosX = 10;
                startPosY = drawEndY + 30;
                g.DrawString("Red", font, strBrush, startPosX, startPosY);
                startPosY += 30;

                foreach (UfCamMeasValue ufMv in ufCamMeasLog.lstUfCamMeas)
                {
                    // Unitの描画開始位置
                    int unitX = startPosX + MeasUnitSizeH * (ufMv.Unit.X - unitStartX);
                    int unitY = startPosY + MeasUnitSizeV * (ufMv.Unit.Y - unitStartY);

                    for (int mod = 0; mod < moduleCount; mod++)
                    {
                        int cellX = unitX + MeasCellSizeH * (mod % ModuleCountX);
                        int cellY = unitY + MeasCellSizeV * (mod / ModuleCountX);

                        //for (int pos = 0; pos < UfMeasSplitCount; pos++)
                        //{
                        //    int posX = cellX + MeasPosSizeH * (pos % 3);
                        //    int posY = cellY + MeasPosSizeV * (pos / 3);

                        //    int level = (int)(128 * (1 + ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvR * Settings.Ins.Camera.MeasureViewGain / 100));
                        //    if (level < 0)
                        //    { level = 0; }
                        //    if (level > 255)
                        //    { level = 255; }

                        //    System.Drawing.Brush fillBrush = new SolidBrush(System.Drawing.Color.FromArgb(level, 0, 0));
                        //    g.FillRectangle(fillBrush, posX, posY, MeasPosSizeH, MeasPosSizeV);
                        //}

                        // Moduleを単色で描画する
                        double spec = GetSpec(ufMv.Unit, mod);

                        System.Drawing.Brush fillBrush;

                        if (Math.Abs(aveR - ufMv.aryUfCamModules[mod].aryUfCamMp[4].PixelValueR) / aveR * 100 > spec)
                        {
                            fillBrush = new SolidBrush(System.Drawing.Color.FromArgb(0xFF, 0x45, 0));
                            //judge = false;
                        }
                        else
                        { fillBrush = new SolidBrush(System.Drawing.Color.FromArgb(128, 0, 0)); }

                        g.FillRectangle(fillBrush, cellX, cellY, MeasPosSizeH * 3, MeasPosSizeV * 3);

                        // Cell枠を描画
                        g.DrawRectangle(penCell, cellX, cellY, MeasCellSizeH, MeasCellSizeV);

                        //// Module間Gapがスペック以上だった場合赤線で表示                       
                        //// Right
                        //if (Math.Abs(ufMv.aryUfCamModules[mod].PcPvGapRightR) > Settings.Ins.Camera.UfGapSpecR)
                        //{ g.DrawLine(penGapNg, new System.Drawing.Point(cellX + MeasCellSizeH, cellY), new System.Drawing.Point(cellX + MeasCellSizeH, cellY + MeasCellSizeV)); }

                        //// Bottom
                        //if (Math.Abs(ufMv.aryUfCamModules[mod].PcPvGapBottomR) > Settings.Ins.Camera.UfGapSpecR)
                        //{ g.DrawLine(penGapNg, new System.Drawing.Point(cellX, cellY + MeasCellSizeV), new System.Drawing.Point(cellX + MeasCellSizeH, cellY + MeasCellSizeV)); }

                        // WhiteのModule間Gapがスペック以上だった場合赤線で表示
                        // Right
                        if (Math.Abs(ufMv.aryUfCamModules[mod].PcPvGapRightW) > Settings.Ins.Camera.UfGapSpecW)
                        {
                            g.DrawLine(penGapNg, new System.Drawing.Point(cellX + MeasCellSizeH, cellY), new System.Drawing.Point(cellX + MeasCellSizeH, cellY + MeasCellSizeV));
                            judge = false;
                        }

                        // Bottom
                        if (Math.Abs(ufMv.aryUfCamModules[mod].PcPvGapBottomW) > Settings.Ins.Camera.UfGapSpecW)
                        {
                            g.DrawLine(penGapNg, new System.Drawing.Point(cellX, cellY + MeasCellSizeV), new System.Drawing.Point(cellX + MeasCellSizeH, cellY + MeasCellSizeV));
                            judge = false;
                        }
                    }

                    // Unit枠を描画
                    g.DrawRectangle(penUnit, unitX, unitY, MeasUnitSizeH, MeasUnitSizeV);

                    // 描画の終点を記録
                    if (unitX + MeasUnitSizeH > drawEndX)
                    { drawEndX = unitX + MeasUnitSizeH; }

                    if (unitY + MeasUnitSizeV > drawEndY)
                    { drawEndY = unitY + MeasUnitSizeV; }
                }

                // Green
                startPosX = 10;
                startPosY = drawEndY + 30;
                g.DrawString("Green", font, strBrush, startPosX, startPosY);
                startPosY += 30;

                foreach (UfCamMeasValue ufMv in ufCamMeasLog.lstUfCamMeas)
                {
                    // Unitの描画開始位置
                    int unitX = startPosX + MeasUnitSizeH * (ufMv.Unit.X - unitStartX);
                    int unitY = startPosY + MeasUnitSizeV * (ufMv.Unit.Y - unitStartY);

                    for (int mod = 0; mod < moduleCount; mod++)
                    {
                        int cellX = unitX + MeasCellSizeH * (mod % ModuleCountX);
                        int cellY = unitY + MeasCellSizeV * (mod / ModuleCountX);

                        //for (int pos = 0; pos < UfMeasSplitCount; pos++)
                        //{
                        //    int posX = cellX + MeasPosSizeH * (pos % 3);
                        //    int posY = cellY + MeasPosSizeV * (pos / 3);

                        //    int level = (int)(128 * (1 + ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvG * Settings.Ins.Camera.MeasureViewGain / 100));
                        //    if (level < 0)
                        //    { level = 0; }
                        //    if (level > 255)
                        //    { level = 255; }

                        //    System.Drawing.Brush fillBrush = new SolidBrush(System.Drawing.Color.FromArgb(0, level, 0));
                        //    g.FillRectangle(fillBrush, posX, posY, MeasPosSizeH, MeasPosSizeV);
                        //}

                        // Moduleを単色で描画する
                        double spec = GetSpec(ufMv.Unit, mod);

                        System.Drawing.Brush fillBrush;

                        if (Math.Abs(aveG - ufMv.aryUfCamModules[mod].aryUfCamMp[4].PixelValueG) / aveG * 100 > spec)
                        {
                            fillBrush = new SolidBrush(System.Drawing.Color.FromArgb(0xFF, 0x45, 0));
                            //judge = false;
                        }
                        else
                        { fillBrush = new SolidBrush(System.Drawing.Color.FromArgb(0, 128, 0)); }

                        g.FillRectangle(fillBrush, cellX, cellY, MeasPosSizeH * 3, MeasPosSizeV * 3);

                        // Cell枠を描画
                        g.DrawRectangle(penCell, cellX, cellY, MeasCellSizeH, MeasCellSizeV);

                        //// Module間Gapがスペック以上だった場合赤線で表示                       
                        //// Right
                        //if (Math.Abs(ufMv.aryUfCamModules[mod].PcPvGapRightG) > Settings.Ins.Camera.UfGapSpecG)
                        //{ g.DrawLine(penGapNg, new System.Drawing.Point(cellX + MeasCellSizeH, cellY), new System.Drawing.Point(cellX + MeasCellSizeH, cellY + MeasCellSizeV)); }

                        //// Bottom
                        //if (Math.Abs(ufMv.aryUfCamModules[mod].PcPvGapBottomG) > Settings.Ins.Camera.UfGapSpecG)
                        //{ g.DrawLine(penGapNg, new System.Drawing.Point(cellX, cellY + MeasCellSizeV), new System.Drawing.Point(cellX + MeasCellSizeH, cellY + MeasCellSizeV)); }

                        // WhiteのModule間Gapがスペック以上だった場合赤線で表示
                        // Right
                        if (Math.Abs(ufMv.aryUfCamModules[mod].PcPvGapRightW) > Settings.Ins.Camera.UfGapSpecW)
                        { g.DrawLine(penGapNg, new System.Drawing.Point(cellX + MeasCellSizeH, cellY), new System.Drawing.Point(cellX + MeasCellSizeH, cellY + MeasCellSizeV)); }

                        // Bottom
                        if (Math.Abs(ufMv.aryUfCamModules[mod].PcPvGapBottomW) > Settings.Ins.Camera.UfGapSpecW)
                        { g.DrawLine(penGapNg, new System.Drawing.Point(cellX, cellY + MeasCellSizeV), new System.Drawing.Point(cellX + MeasCellSizeH, cellY + MeasCellSizeV)); }
                    }

                    // Unit枠を描画
                    g.DrawRectangle(penUnit, unitX, unitY, MeasUnitSizeH, MeasUnitSizeV);

                    // 描画の終点を記録
                    if (unitX + MeasUnitSizeH > drawEndX)
                    { drawEndX = unitX + MeasUnitSizeH; }

                    if (unitY + MeasUnitSizeV > drawEndY)
                    { drawEndY = unitY + MeasUnitSizeV; }
                }

                // Blue
                startPosX = 10;
                startPosY = drawEndY + 30;
                g.DrawString("Blue", font, strBrush, startPosX, startPosY);
                startPosY += 30;

                foreach (UfCamMeasValue ufMv in ufCamMeasLog.lstUfCamMeas)
                {
                    // Unitの描画開始位置
                    int unitX = startPosX + MeasUnitSizeH * (ufMv.Unit.X - unitStartX);
                    int unitY = startPosY + MeasUnitSizeV * (ufMv.Unit.Y - unitStartY);

                    for (int mod = 0; mod < moduleCount; mod++)
                    {
                        int cellX = unitX + MeasCellSizeH * (mod % ModuleCountX);
                        int cellY = unitY + MeasCellSizeV * (mod / ModuleCountX);

                        //for (int pos = 0; pos < UfMeasSplitCount; pos++)
                        //{
                        //    int posX = cellX + MeasPosSizeH * (pos % 3);
                        //    int posY = cellY + MeasPosSizeV * (pos / 3);

                        //    int level = (int)(128 * (1 + ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcPvB * Settings.Ins.Camera.MeasureViewGain / 100));
                        //    if (level < 0)
                        //    { level = 0; }
                        //    if (level > 255)
                        //    { level = 255; }

                        //    System.Drawing.Brush fillBrush = new SolidBrush(System.Drawing.Color.FromArgb(0, 0, level));
                        //    g.FillRectangle(fillBrush, posX, posY, MeasPosSizeH, MeasPosSizeV);
                        //}

                        // Moduleを単色で描画する
                        double spec = GetSpec(ufMv.Unit, mod);

                        System.Drawing.Brush fillBrush;

                        if (Math.Abs(aveB - ufMv.aryUfCamModules[mod].aryUfCamMp[4].PixelValueB) / aveB * 100 > spec)
                        {
                            fillBrush = new SolidBrush(System.Drawing.Color.FromArgb(0xFF, 0x45, 0));
                            //judge = false;
                        }
                        else
                        { fillBrush = new SolidBrush(System.Drawing.Color.FromArgb(0, 0, 128)); }

                        g.FillRectangle(fillBrush, cellX, cellY, MeasPosSizeH * 3, MeasPosSizeV * 3);

                        // Cell枠を描画
                        g.DrawRectangle(penCell, cellX, cellY, MeasCellSizeH, MeasCellSizeV);

                        //// Module間Gapがスペック以上だった場合赤線で表示                       
                        //// Right
                        //if (Math.Abs(ufMv.aryUfCamModules[mod].PcPvGapRightG) > Settings.Ins.Camera.UfGapSpecB)
                        //{ g.DrawLine(penGapNg, new System.Drawing.Point(cellX + MeasCellSizeH, cellY), new System.Drawing.Point(cellX + MeasCellSizeH, cellY + MeasCellSizeV)); }

                        //// Bottom
                        //if (Math.Abs(ufMv.aryUfCamModules[mod].PcPvGapBottomG) > Settings.Ins.Camera.UfGapSpecB)
                        //{ g.DrawLine(penGapNg, new System.Drawing.Point(cellX, cellY + MeasCellSizeV), new System.Drawing.Point(cellX + MeasCellSizeH, cellY + MeasCellSizeV)); }

                        // WhiteのModule間Gapがスペック以上だった場合赤線で表示
                        // Right
                        if (Math.Abs(ufMv.aryUfCamModules[mod].PcPvGapRightW) > Settings.Ins.Camera.UfGapSpecW)
                        { g.DrawLine(penGapNg, new System.Drawing.Point(cellX + MeasCellSizeH, cellY), new System.Drawing.Point(cellX + MeasCellSizeH, cellY + MeasCellSizeV)); }

                        // Bottom
                        if (Math.Abs(ufMv.aryUfCamModules[mod].PcPvGapBottomW) > Settings.Ins.Camera.UfGapSpecW)
                        { g.DrawLine(penGapNg, new System.Drawing.Point(cellX, cellY + MeasCellSizeV), new System.Drawing.Point(cellX + MeasCellSizeH, cellY + MeasCellSizeV)); }
                    }

                    // Unit枠を描画
                    g.DrawRectangle(penUnit, unitX, unitY, MeasUnitSizeH, MeasUnitSizeV);

                    // 描画の終点を記録
                    if (unitX + MeasUnitSizeH > drawEndX)
                    { drawEndX = unitX + MeasUnitSizeH; }

                    if (unitY + MeasUnitSizeV > drawEndY)
                    { drawEndY = unitY + MeasUnitSizeV; }
                }

                #endregion 輝度分布

                #region 簡易色度(Rough Chromaticity)

                drawEndY = 0;

                // Red
                startPosX = drawEndX + 100;
                startPosY = 10;
                g.DrawString("Rough Chromaticity", font, strBrush, startPosX, startPosY);
                g.DrawString("Red", font, strBrush, startPosX, startPosY += 30);
                startPosY += 30;

                foreach (UfCamMeasValue ufMv in ufCamMeasLog.lstUfCamMeas)
                {
                    // Unitの描画開始位置
                    int unitX = startPosX + MeasUnitSizeH * (ufMv.Unit.X - unitStartX);
                    int unitY = startPosY + MeasUnitSizeV * (ufMv.Unit.Y - unitStartY);

                    for (int mod = 0; mod < moduleCount; mod++)
                    {
                        int cellX = unitX + MeasCellSizeH * (mod % ModuleCountX);
                        int cellY = unitY + MeasCellSizeV * (mod / ModuleCountX);

                        for (int pos = 0; pos < UfMeasSplitCount; pos++)
                        {
                            int posX = cellX + MeasPosSizeH * (pos % 3);
                            int posY = cellY + MeasPosSizeV * (pos / 3);

                            int lvMain = 0, lvSub = 0;
                            if (ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcChromR > 0)
                            {
                                lvMain = 255;
                                lvSub = (int)(255 * (1 - ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcChromR * Settings.Ins.Camera.MeasureViewGain / 100));
                            }
                            else
                            {
                                lvMain = (int)(255 * (1 + ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcChromR * Settings.Ins.Camera.MeasureViewGain / 100));
                                lvSub = 255;
                            }

                            if (lvMain < 0)
                            { lvMain = 0; }
                            if (lvMain > 255)
                            { lvMain = 255; }

                            if (lvSub < 0)
                            { lvSub = 0; }
                            if (lvSub > 255)
                            { lvSub = 255; }

                            System.Drawing.Brush fillBrush = new SolidBrush(System.Drawing.Color.FromArgb(lvMain, lvSub, lvSub));
                            g.FillRectangle(fillBrush, posX, posY, MeasPosSizeH, MeasPosSizeV);
                        }

                        // Cell枠を描画
                        g.DrawRectangle(penCell, cellX, cellY, MeasCellSizeH, MeasCellSizeV);
                    }

                    // Unit枠を描画
                    g.DrawRectangle(penUnit, unitX, unitY, MeasUnitSizeH, MeasUnitSizeV);

                    // 描画の終点を記録
                    if (unitX + MeasUnitSizeH > drawEndX)
                    { drawEndX = unitX + MeasUnitSizeH; }

                    if (unitY + MeasUnitSizeV > drawEndY)
                    { drawEndY = unitY + MeasUnitSizeV; }
                }

                // Green
                startPosY = drawEndY + 30;
                g.DrawString("Green", font, strBrush, startPosX, startPosY);
                startPosY += 30;

                foreach (UfCamMeasValue ufMv in ufCamMeasLog.lstUfCamMeas)
                {
                    // Unitの描画開始位置
                    int unitX = startPosX + MeasUnitSizeH * (ufMv.Unit.X - unitStartX);
                    int unitY = startPosY + MeasUnitSizeV * (ufMv.Unit.Y - unitStartY);

                    for (int mod = 0; mod < moduleCount; mod++)
                    {
                        int cellX = unitX + MeasCellSizeH * (mod % ModuleCountX);
                        int cellY = unitY + MeasCellSizeV * (mod / ModuleCountX);

                        for (int pos = 0; pos < UfMeasSplitCount; pos++)
                        {
                            int posX = cellX + MeasPosSizeH * (pos % 3);
                            int posY = cellY + MeasPosSizeV * (pos / 3);

                            int lvMain = 0, lvSub = 0;
                            if (ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcChromG > 0)
                            {
                                lvMain = 255;
                                lvSub = (int)(255 * (1 - ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcChromG * Settings.Ins.Camera.MeasureViewGain / 100));
                            }
                            else
                            {
                                lvMain = (int)(255 * (1 + ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcChromG * Settings.Ins.Camera.MeasureViewGain / 100));
                                lvSub = 255;
                            }

                            if (lvMain < 0)
                            { lvMain = 0; }
                            if (lvMain > 255)
                            { lvMain = 255; }

                            if (lvSub < 0)
                            { lvSub = 0; }
                            if (lvSub > 255)
                            { lvSub = 255; }

                            System.Drawing.Brush fillBrush = new SolidBrush(System.Drawing.Color.FromArgb(lvSub, lvMain, lvSub));
                            g.FillRectangle(fillBrush, posX, posY, MeasPosSizeH, MeasPosSizeV);
                        }

                        // Cell枠を描画
                        g.DrawRectangle(penCell, cellX, cellY, MeasCellSizeH, MeasCellSizeV);
                    }

                    // Unit枠を描画
                    g.DrawRectangle(penUnit, unitX, unitY, MeasUnitSizeH, MeasUnitSizeV);

                    // 描画の終点を記録
                    if (unitX + MeasUnitSizeH > drawEndX)
                    { drawEndX = unitX + MeasUnitSizeH; }

                    if (unitY + MeasUnitSizeV > drawEndY)
                    { drawEndY = unitY + MeasUnitSizeV; }
                }

                // Blue  
                startPosY = drawEndY + 30;
                g.DrawString("Blue", font, strBrush, startPosX, startPosY);
                startPosY += 30;

                foreach (UfCamMeasValue ufMv in ufCamMeasLog.lstUfCamMeas)
                {
                    // Unitの描画開始位置
                    int unitX = startPosX + MeasUnitSizeH * (ufMv.Unit.X - unitStartX);
                    int unitY = startPosY + MeasUnitSizeV * (ufMv.Unit.Y - unitStartY);

                    for (int mod = 0; mod < moduleCount; mod++)
                    {
                        int cellX = unitX + MeasCellSizeH * (mod % ModuleCountX);
                        int cellY = unitY + MeasCellSizeV * (mod / ModuleCountX);

                        for (int pos = 0; pos < UfMeasSplitCount; pos++)
                        {
                            int posX = cellX + MeasPosSizeH * (pos % 3);
                            int posY = cellY + MeasPosSizeV * (pos / 3);

                            int lvMain = 0, lvSub = 0;
                            if (ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcChromB > 0)
                            {
                                lvMain = 255;
                                lvSub = (int)(255 * (1 - ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcChromB * Settings.Ins.Camera.MeasureViewGain / 100));
                            }
                            else
                            {
                                lvMain = (int)(255 * (1 + ufMv.aryUfCamModules[mod].aryUfCamMp[pos].PcChromB * Settings.Ins.Camera.MeasureViewGain / 100));
                                lvSub = 255;
                            }

                            if (lvMain < 0)
                            { lvMain = 0; }
                            if (lvMain > 255)
                            { lvMain = 255; }

                            if (lvSub < 0)
                            { lvSub = 0; }
                            if (lvSub > 255)
                            { lvSub = 255; }

                            System.Drawing.Brush fillBrush = new SolidBrush(System.Drawing.Color.FromArgb(lvSub, lvSub, lvMain));
                            g.FillRectangle(fillBrush, posX, posY, MeasPosSizeH, MeasPosSizeV);
                        }

                        // Cell枠を描画
                        g.DrawRectangle(penCell, cellX, cellY, MeasCellSizeH, MeasCellSizeV);
                    }

                    // Unit枠を描画
                    g.DrawRectangle(penUnit, unitX, unitY, MeasUnitSizeH, MeasUnitSizeV);

                    // 描画の終点を記録
                    if (unitX + MeasUnitSizeH > drawEndX)
                    { drawEndX = unitX + MeasUnitSizeH; }

                    if (unitY + MeasUnitSizeV > drawEndY)
                    { drawEndY = unitY + MeasUnitSizeV; }
                }

                #endregion 簡易色度

                g.Dispose(); // リソースを解放する

                // imageに画像を設定
                setImage(imgUfCamResult, canvas);

                // OK/NG表示
                if (judge == true)
                {
                    Dispatcher.Invoke(new Action(() => {
                        txbUfCamMeasResult.Text = "OK";
                        txbUfCamMeasResult.Background = System.Windows.Media.Brushes.DarkGreen;
                        txbUfCamMeasResult.Foreground = System.Windows.Media.Brushes.Lime;
                    }));
                }
                else
                {
                    Dispatcher.Invoke(new Action(() => {
                        txbUfCamMeasResult.Text = "NG";
                        txbUfCamMeasResult.Background = System.Windows.Media.Brushes.DarkRed;
                        txbUfCamMeasResult.Foreground = System.Windows.Media.Brushes.Red;
                    }));
                }
            }
        }

        private double GetSpec(UnitInfo cabi, int module)
        {
            double spec = 0;

            // 最外周のModuleかどうか
            if ((cabi.Y == 1 && (module == 0 || module == 1 || module == 2 || module == 3)) // 上辺
                || (cabi.Y == allocInfo.MaxY && (module == 4 || module == 5 || module == 6 || module == 7)) // 底辺
                || (cabi.X == 1 && (module == 0 || module == 4)) // 左辺
                || (cabi.X == allocInfo.MaxX && (module == 3 || module == 7))) // 右辺
            { spec = Settings.Ins.Camera.UfSpecOutside; }
            else
            { spec = Settings.Ins.Camera.UfSpecInside; }

            return spec;
        }

        private void captureFlatImage(string path, List<UnitInfo> lstTgtUnit, bool targetOnly)
        {
            // modified by Hotta 2021/09/01
#if NO_CAP
            // カメラ設定
            winProgress.ShowMessage("Set Camera Settings. (MeasArea)");
            winProgress.PutForward1Step();
            //cc.SetCameraSettings(Settings.Ins.Camera.MeasAreaSetting);

            // Red
            winProgress.ShowMessage("Capture Flat-Red Image.");
            winProgress.PutForward1Step();
            //outputIntSigFlat(brightness._20pc, 0, 0);

            //string imgPath = path + fn_FlatRed;
            //cc.CaptureImage(imgPath);
            //Thread.Sleep(Settings.Ins.Camera.CameraWait);

            // Green
            winProgress.ShowMessage("Capture Flat-Green Image.");
            winProgress.PutForward1Step();
            //outputIntSigFlat(0, brightness._20pc, 0);

            //imgPath = path + fn_FlatGreen;
            //cc.CaptureImage(imgPath);
            //Thread.Sleep(Settings.Ins.Camera.CameraWait);

            // Blue
            winProgress.ShowMessage("Capture Flat-Blue Image.");
            winProgress.PutForward1Step();
            //outputIntSigFlat(0, 0, brightness._20pc);

            //imgPath = path + fn_FlatBlue;
            //cc.CaptureImage(imgPath);
            //Thread.Sleep(Settings.Ins.Camera.CameraWait);

            // White
            winProgress.ShowMessage("Capture Flat-White Image.");
            winProgress.PutForward1Step();
            //outputIntSigFlat(brightness._20pc, brightness._20pc, brightness._20pc);

            //imgPath = path + fn_FlatWhite;
            //cc.CaptureImage(imgPath);
            //Thread.Sleep(Settings.Ins.Camera.CameraWait);

#else
            // Red
            winProgress.ShowMessage("Capture Flat-Red Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Flat-Red Image.");

            if(targetOnly == true)
            { outputIntSigWindow(lstTgtUnit[0].PixelX, lstTgtUnit[0].PixelY, cabiDy, cabiDx, brightness._20pc, 0, 0); }
            else
            { outputIntSigFlat(brightness._20pc, 0, 0); }            

            string imgPath = path + fn_FlatRed;
            CaptureImage(imgPath, Settings.Ins.Camera.MeasAreaSetting); // カメラ設定付き

            Thread.Sleep(100); // Flagの更新が間に合わないみたい
            
            // Green
            winProgress.ShowMessage("Capture Flat-Green Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Flat-Green Image.");

            if (targetOnly == true)
            { outputIntSigWindow(lstTgtUnit[0].PixelX, lstTgtUnit[0].PixelY, cabiDy, cabiDx, 0, brightness._20pc, 0); }
            else
            { outputIntSigFlat(0, brightness._20pc, 0); }

            imgPath = path + fn_FlatGreen;
            CaptureImage(imgPath);
            
            Thread.Sleep(100);
            
            // Blue
            winProgress.ShowMessage("Capture Flat-Blue Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Flat-Blue Image.");

            if (targetOnly == true)
            { outputIntSigWindow(lstTgtUnit[0].PixelX, lstTgtUnit[0].PixelY, cabiDy, cabiDx, 0, 0, brightness._20pc); }
            else
            { outputIntSigFlat(0, 0, brightness._20pc); }

            imgPath = path + fn_FlatBlue;
            CaptureImage(imgPath);

            Thread.Sleep(100);

            // White
            winProgress.ShowMessage("Capture Flat-White Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Flat-White Image.");

            if (targetOnly == true)
            { outputIntSigWindow(lstTgtUnit[0].PixelX, lstTgtUnit[0].PixelY, cabiDy, cabiDx, brightness._20pc, brightness._20pc, brightness._20pc); }
            else
            { outputIntSigFlat(brightness._20pc, brightness._20pc, brightness._20pc); }

            imgPath = path + fn_FlatWhite;
            CaptureImage(imgPath);
#endif
        }

        private UnitInfo searchUnit(List<UnitInfo> lstTgtUnit, int x, int y)
        {
            UnitInfo tgtUnit = null;

            foreach (UnitInfo unit in lstTgtUnit)
            {
                if (unit.X == x && unit.Y == y)
                {
                    tgtUnit = unit;
                    break;
                }
            }

            return tgtUnit;
        }

        /// <summary>
        /// 指定したCabinetの中心のTilt角を取得する
        /// </summary>
        /// <param name="lstTgtCabi"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private double GetTiltAngle(List<UnitInfo> lstTgtCabi, int x, int y)
        {
            double tilt = 0;

            UnitInfo ctCabi = searchUnit(lstTgtCabi, x, y);

            foreach (UfCamCpAngle angle in ctCabi.lstCpAngle)
            {
                if (angle.CorrectPos == UfCamCorrectPos._9pt_Center)
                {
                    tilt = angle.Angle.Tilt;
                    break;
                }
            }

            return tilt;
        }

        private void calcAverage(AcqARW arw, AcqARW arwBlack, Area area, out PixelValue average, double blanking) // blanking = 削り量(割合) 0～0.5未満
        {
            double sum_r = 0.0;
            double sum_g = 0.0;
            double sum_b = 0.0;
            int startX = (int)area.StartPos.X + (int)(area.Width * blanking);
            int endX = (int)area.EndPos.X - (int)(area.Width * blanking);
            int startY = (int)area.StartPos.Y + (int)(area.Height * blanking);
            int endY = (int)area.EndPos.Y - (int)(area.Height * blanking);

            average = new PixelValue();

            int count = 0;
            int saturationCount = 0;

            for (int i = startY; i < endY; i++)
            {
                for (int j = startX; j < endX; j++)
                {
                    double num11 = (int)arw.RawData[0][j, i] - (int)arwBlack.RawData[0][j, i]; // Red
                    if (num11 < 0) { num11 = 0; }
                    double num12 = (int)arw.RawData[1][j, i] - (int)arwBlack.RawData[1][j, i]; // Green
                    if (num12 < 0) { num12 = 0; }
                    double num13 = (int)arw.RawData[2][j, i] - (int)arwBlack.RawData[2][j, i]; // Blue
                    if (num13 < 0) { num13 = 0; }

                    sum_r += num11;
                    sum_g += num12;
                    sum_b += num13;

                    if (num11 > SaturationSpec || num12 > SaturationSpec || num13 > SaturationSpec)
                    { saturationCount++; }

                    count++;
                }
            }

            if (count == 0)
            { count = 1; }

            // サチり確認
            if (saturationCount > Settings.Ins.Camera.SaturationLimit)
            { throw new Exception("There are more than the specified number of saturated pixels.\r\nReview the camera settings."); }

            average.R = sum_r / (double)count;
            average.G = sum_g / (double)count;
            average.B = sum_b / (double)count;

            // Raw画像の明るさ確認
            double pvMax = 0;

            // 最大の画素値（＝主成分）が確認対象
            if (average.R > pvMax)
            { pvMax = average.R; }
            if (average.G > pvMax)
            { pvMax = average.G; }
            if (average.B > pvMax)
            { pvMax = average.B; }

            string msg = "Point [x, y] : [" + area.CenterPos.X + ", " + area.CenterPos.Y + "]\r\nCurrent Value : (Red)" + average.R.ToString("0") + ", (Green)" + average.G.ToString("0") + ", (Blue)" + average.B.ToString("0") + "\r\nSpec : ";

            if (pvMax > Settings.Ins.Camera.MaxRawValue)
            { throw new Exception("The brightness of the raw image is too bright.\r\nReview the camera settings.\r\n\r\n" + msg + Settings.Ins.Camera.MaxRawValue); }

            if (pvMax < Settings.Ins.Camera.MinRawValue)
            { throw new Exception("The brightness of the raw image is too dark.\r\nReview the camera settings.\r\n\r\n" + msg + Settings.Ins.Camera.MinRawValue); }
        }

        private void calcAverageWhite(AcqARW arwR, AcqARW arwG, AcqARW arwB, AcqARW arwBlack, Area area, out PixelValue average, double blanking) // blanking = 削り量(割合) 0～0.5未満
        {
            double sum_r = 0.0;
            double sum_g = 0.0;
            double sum_b = 0.0;
            int startX = (int)area.StartPos.X + (int)(area.Width * blanking);
            int endX = (int)area.EndPos.X - (int)(area.Width * blanking);
            int startY = (int)area.StartPos.Y + (int)(area.Height * blanking);
            int endY = (int)area.EndPos.Y - (int)(area.Height * blanking);

            average = new PixelValue();

            int count = 0;
            for (int i = startY; i < endY; i++)
            {
                for (int j = startX; j < endX; j++)
                {
                    double num11 = (int)arwR.RawData[0][j, i] + (int)arwG.RawData[0][j, i] + (int)arwB.RawData[0][j, i] - (int)arwBlack.RawData[0][j, i] * 3;
                    if (num11 < 0) { num11 = 0; }
                    double num12 = (int)arwR.RawData[1][j, i] + (int)arwG.RawData[1][j, i] + (int)arwB.RawData[1][j, i] - (int)arwBlack.RawData[1][j, i] * 3;
                    if (num12 < 0) { num12 = 0; }
                    double num13 = (int)arwR.RawData[2][j, i] + (int)arwG.RawData[2][j, i] + (int)arwB.RawData[2][j, i] - (int)arwBlack.RawData[2][j, i] * 3;
                    if (num13 < 0) { num13 = 0; }

                    sum_r += num11;
                    sum_g += num12;
                    sum_b += num13;

                    count++;
                }
            }

            if (count == 0)
            { count = 1; }

            average.R = sum_r / (double)count; // - (double)Settings.Ins.Camera.ColorOffset;
            average.G = sum_g / (double)count; // - (double)Settings.Ins.Camera.ColorOffset;
            average.B = sum_b / (double)count; // - (double)Settings.Ins.Camera.ColorOffset;
        }

        private void DeleteUnwantedImagesMeas(string path)
        {
            // MeasArea.arw
            string file = path + fn_AreaFile;

            if(File.Exists(file) == true)
            {
                try { File.Delete(file); }
                catch { } // 無視
            }

            // Module_*.arw
            string[] files = Directory.GetFiles(path, fn_MeasureAreaMod + "*");

            for(int i = 0; i < files.Length; i++)
            {
                try { File.Delete(files[i]); }
                catch { } // 無視
            }

            // Mask.jpg
            file = path + fn_MaskFile; 
            
            if (File.Exists(file) == true)
            {
                try { File.Delete(file); }
                catch { } // 無視
            }
        }

        #endregion Mesurement

        #region Adjust

        /// <summary>
        /// 基準(調整目標)Cabinetを設定する。
        /// Defaultは画面の中心にあるCabinet、ただし選択範囲が偶数の場合は左上(第2象限)のCabinetが選択される
        /// </summary>
        /// <param name="lstTgtUnit">調整対象Cabinet</param>
        /// <param name="lstObjCabi">基準Cabinet</param>
        private void StoreObjectiveCabinet(List<UnitInfo> lstTgtUnit, out List<UnitInfo> lstObjCabi)
        {
            UnitInfo objCabi = null;

            if(lstTgtUnit.Count == 1)
            { objCabi = lstTgtUnit[0]; }
            else
            {
                int minX = int.MaxValue, minY = int.MaxValue, maxX = 0, maxY = 0;

                // 座標の最大、最小値を探す
                for(int i = 0; i < lstTgtUnit.Count; i++)
                {
                    if(lstTgtUnit[i].X < minX)
                    { minX = lstTgtUnit[i].X; }

                    if (lstTgtUnit[i].Y < minY)
                    { minY = lstTgtUnit[i].Y; }

                    if (lstTgtUnit[i].X > maxX)
                    { maxX = lstTgtUnit[i].X; }

                    if (lstTgtUnit[i].Y > maxY)
                    { maxY = lstTgtUnit[i].Y; }
                }

                int centerX = (int)Math.Floor((double)(maxX + minX) / 2.0);
                int centerY = (int)Math.Floor((double)(maxY + minY) / 2.0);

                if(centerX < 0)
                { centerX = 0; }

                if(centerY < 0)
                { centerY = 0; }

                for (int i = 0; i < lstTgtUnit.Count; i++)
                {
                    if (lstTgtUnit[i].X == centerX && lstTgtUnit[i].Y == centerY)
                    {
                        objCabi = lstTgtUnit[i];
                        break;
                    }
                }
            }

            lstObjCabi = new List<UnitInfo>();
            lstObjCabi.Add(objCabi);
        }

        /// <summary>
        /// 基準(調整目標)Cabinetを設定する。
        /// ユーザーが指定したCabinet。
        /// </summary>
        /// <param name="target">ユーザーが選択したCabinetを表す文字列(e.g. C1-1-2)</param>
        /// <param name="lstObjCabi">基準Cabinet</param>
        private void StoreObjectiveCabinet(string target, out List<UnitInfo> lstObjCabi)
        {
            lstObjCabi = new List<UnitInfo>();

            string[] parts = target.Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries);

            int contId = Convert.ToInt32(parts[0].Replace("C", ""));
            int portNo = Convert.ToInt32(parts[1]);
            int unitNo = Convert.ToInt32(parts[2]);

            UnitInfo objCabi = null;

            // Cabinet
            for (int y = 0; y < allocInfo.MaxY; y++)
            {
                for (int x = 0; x < allocInfo.MaxX; x++)
                {
                    if(allocInfo.lstUnits[x][y] == null)
                    { continue; }

                    if (allocInfo.lstUnits[x][y].ControllerID == contId
                        && allocInfo.lstUnits[x][y].PortNo == portNo
                        && allocInfo.lstUnits[x][y].UnitNo == unitNo)
                    {
                        objCabi = allocInfo.lstUnits[x][y];
                        lstObjCabi.Add(objCabi);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// 基準(調整目標)Cabinetを設定する。
        /// </summary>
        /// <param name="lstTgtUnit">調整対象Cabinet</param>
        /// <param name="edge">Line選択時の基準辺</param>
        /// <param name="lstObjCabi">基準Cabinet</param>
        private void StoreObjectiveCabinet(List<UnitInfo> lstTgtUnit, ObjectiveLine edge, out List<UnitInfo> lstObjCabi)
        {
            lstObjCabi = new List<UnitInfo>();

            int minX = int.MaxValue, minY = int.MaxValue, maxX = 0, maxY = 0;

            // 座標の最大、最小値を探す
            for (int i = 0; i < lstTgtUnit.Count; i++)
            {
                if (lstTgtUnit[i].X < minX)
                { minX = lstTgtUnit[i].X; }

                if (lstTgtUnit[i].Y < minY)
                { minY = lstTgtUnit[i].Y; }

                if (lstTgtUnit[i].X > maxX)
                { maxX = lstTgtUnit[i].X; }

                if (lstTgtUnit[i].Y > maxY)
                { maxY = lstTgtUnit[i].Y; }
            }

            int lenX = maxX - minX;
            int lenY = maxY - minY;

            if(edge.Top == true)
            {
                foreach(UnitInfo cabi in lstTgtUnit)
                {
                    if(cabi.Y == minY)
                    { lstObjCabi.Add(cabi); }
                }
            }

            if(edge.Bottom == true)
            {
                foreach (UnitInfo cabi in lstTgtUnit)
                {
                    if (cabi.Y == maxY)
                    { lstObjCabi.Add(cabi); }
                }
            }

            if(edge.Left == true)
            {
                foreach (UnitInfo cabi in lstTgtUnit)
                {
                    if (cabi.X == minX)
                    { lstObjCabi.Add(cabi); }
                }
            }

            if(edge.Right == true)
            {
                foreach (UnitInfo cabi in lstTgtUnit)
                {
                    if (cabi.X == maxX)
                    { lstObjCabi.Add(cabi); }
                }
            }

            if(lstObjCabi.Count <= 0) 
            { throw new Exception("No reference line has been selected."); }
        }

        /// <summary>
        /// 基準Cabinetが調整対象Cabinetに含まれているか確認する。
        /// Customで範囲外を選んでいた場合はNG
        /// </summary>
        /// <param name="lstObjCabi">基準Cabinet</param>
        /// <param name="lstTgtUnit">調整対象Cabinet</param>
        private void CheckObjectiveCabinet(List<UnitInfo> lstObjCabi, List<UnitInfo> lstTgtUnit)
        {
            foreach (UnitInfo objCabi in lstObjCabi)
            {
                bool status = false;

                foreach (UnitInfo cabi in lstTgtUnit)
                {
                    if (cabi == objCabi)
                    {
                        status = true;
                        break;
                    }
                }

                if (status == false)
                { throw new Exception("The reference cabinet is out of the adjustment area.\r\nSelect a reference cabinet in the adjustment area."); }
            }
        }

        /// <summary>
        /// カメラUF調整のエントリーポイント
        /// </summary>
        /// <param name="logDir">UF Log保存フォルダ</param>
        /// <param name="lstTgtCabi">調整対象Cabinet</param>
        /// <param name="type">調整モード(Cabinet/Module/Radiator/9pt)</param>
        /// <param name="lstObjCabi">基準Cabinet</param>
        /// <param name="objEdge">基準Line Line以外の時はnullが入ってくる</param>
        /// <param name="isViewPtMode">視聴点調整モードフラグ</param>
        private void AdjustUfCamAsync(string logDir, List<UnitInfo> lstTgtCabi, UfCamAdjustType type, List<UnitInfo> lstObjCabi, ObjectiveLine objEdge, ViewPoint vp, double dist, double wallH, double camH)
        {
            List<UserSetting> lstUserSetting;
            List<MoveFile> lstMoveFile;

            int steps = 0;
            if(type == UfCamAdjustType.Cabinet || type == UfCamAdjustType.Radiator)
            { steps = 27; }
            else if(type == UfCamAdjustType.EachModule)
            { steps = 19 + moduleCount * 2; } // 撮影 + 読み込み
            else if(type == UfCamAdjustType.Cabi_9pt)
            { steps = 37; }

            // 全体のStep数を設定
            winProgress.SetWholeSteps(9 + steps);

            // マスクの信号レベルを設定
            m_MeasureLevel = brightness.UF_20pc; // 20IRE = 492

            // OpenCVSharpのDllがあるか確認する
            CheckOpenCvSharpDll();

            // added by Hotta 2023/12/11
            // たまに、UserSettingのまま先に進んでしまう。
            // 目地は問題ないので、目地の処理を真似て、パターン表示、時間待ちを設定する
            outputIntSigChecker(0, 0, modDy, modDx, modDx, modDy); // 20IRE
            Thread.Sleep(Settings.Ins.Camera.PatternWait);

            // Cabinet on
            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdUnitPowerOn, cont.IPAddress); }
            Thread.Sleep(5000);
            //

            // カメラの目標位置を再設定
            SetCamPosTarget();

            m_MakeUFData = new CMakeUFData(cabiDx, cabiDy);

            //// Unit Power On 1Step
            //winProgress.ShowMessage("Cabinet Power On.");
            //winProgress.PutForward1Step();
            //foreach (ControllerInfo cont in dicController.Values)
            //{ sendSdcpCommand(SDCPClass.CmdUnitPowerOn, cont.IPAddress); }
            //Thread.Sleep(5000);

            // ユーザー設定を保存 1Step
            winProgress.ShowMessage("Store User Settings.");
            winProgress.PutForward1Step();
            saveUfLog("Store User Settings.");
            // modified by Hotta 2024/09/30
            /*
            getUserSetting(out lstUserSetting);
            */
            bool st = getUserSetting(out lstUserSetting);
            if (st == true)
                m_lstUserSetting = lstUserSetting;
            //


#if NO_CAP
            // カメラ視野角特性補正データのLoad
            //CameraCharacteristicData.LoadFromXmlFile(Settings.Ins.Camera.CameraCorrectionFilePath, out camCharData);
#else
            // 調整用設定 1Step
            winProgress.ShowMessage("Set Adjust Settings.");
            winProgress.PutForward1Step();
            saveUfLog("Set Adjust Settings.");
            setAdjustSetting();
#endif
            // Layout情報Off
            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdDispUnitAddrOff, 0, cont.IPAddress); }

            int processSec = calcUfCameraAdjustmentProcessSec(1);
            winProgress.SetRemainTimer(processSec);

            // AutoFocus
            winProgress.ShowMessage("Execute AutoFocus.");
            winProgress.PutForward1Step();
            saveUfLog("Execute AutoFocus.");
            ShootCondition codition = Settings.Ins.Camera.SetPosSetting;
            m_CamMeasPath = tempPath;

            outputIntSigChecker(0, 0, modDy, modDx, modDx, modDy); // 20IRE
            AutoFocus(codition, new AfAreaSetting());

            // 照明の映り込みをチェック 1Step
            //winProgress.ShowMessage("Check Light Reflections.");
            //winProgress.PutForward1Step();
            //CheckLightingReflection(lstTgtUnit);

            processSec = calcUfCameraAdjustmentProcessSec(2);
            winProgress.SetRemainTimer(processSec);

            // 開始時のカメラ位置を保存 1Step
            winProgress.ShowMessage("Store Camera Position.");
            winProgress.PutForward1Step();
            saveUfLog("Store Camera Position.");
            bool status = GetCameraPosUf(imgUfCamCameraView, out CvBlob[,] aryBlob, out CameraPosition startCamPos);
            if (status != true)
            { throw new Exception("Failed to get the camera position."); }

            status = CheckCameraPos(aryBlob, startCamPos);
            if (status != true)
            { throw new Exception("The camera position is inappropriate.\r\nAfter alignment, the camera is moving. Please re-align the camera."); }

            // 撮影エリアの画像上の割合を計算(小さい場合はハンチングの可能性があるのでカメラずれ量を使用しない。)
            double sensorArea = m_CameraParam.SensorResV * m_CameraParam.SensorResH;
            double targetArea = CalcTargetArea(aryBlob);

            if(targetArea <= sensorArea * 0.10) // 画面全体の10％より小さい(4m、2x2だとおおよそ8.5％)
            { startCamPos = new CameraPosition(); }

            ufCamAdjLog.StartCamPos = startCamPos;
                        
            // Cabinet位置(空間座標)を再設定(カメラ位置合わせを実施するとカメラ-Wall相対位置情報がリセットされる)
            SetCabinetPos(lstTgtCabi, dist, wallH, camH);

            // カメラ設置ズレ分を反映
            MoveCabinetPos(startCamPos.Pan, startCamPos.Tilt, startCamPos.Roll, startCamPos.X, startCamPos.Y, startCamPos.Z);

            // 調整点の最大角度を確認(Pan, Tiltとも±50が限界)
            CheckCpAngle(lstTgtCabi);

            processSec = calcUfCameraAdjustmentProcessSec(3);
            winProgress.SetRemainTimer(processSec);

            // 調整
            if (type == UfCamAdjustType.EachModule)
            { AdjustUfCamEachModule(lstTgtCabi, lstObjCabi, objEdge, vp, logDir, out lstMoveFile); } // 26steps
            else if (type == UfCamAdjustType.Radiator)
            { AdjustUfCamRadiator(lstTgtCabi, lstObjCabi, objEdge, vp, logDir, out lstMoveFile); } // 18Steps
            else if(type == UfCamAdjustType.Cabi_9pt)
            { AdjustUfCam9pt(lstTgtCabi, lstObjCabi, objEdge, vp, logDir, out lstMoveFile); }
            else // Cabinet
            { AdjustUfCamCabinet(lstTgtCabi, lstObjCabi, objEdge, vp, logDir, out lstMoveFile); } // 18Steps


            // added by Hotta 2024/10/02 for TEST
            /*
#if ForCrosstalkCameraUF
            m_MakeUFData.saveFmt();
#endif
            */

            processSec = calcUfCameraAdjustmentProcessSec(4, lstMoveFile.Count);
            winProgress.SetRemainTimer(processSec);

            // 書き込み 1Step
            winProgress.ShowMessage("Write Adjusted UF Data.");
            winProgress.PutForward1Step();
            saveUfLog("Write Adjusted UF Data.");
            if (writeAdjustedData(lstMoveFile) != true)
            {
                foreach (ControllerInfo controller in dicController.Values)
                {
                    if (sendSdcpCommand(SDCPClass.CmdFtpOff, controller.IPAddress) != true)
                    {
                        ShowMessageWindow("Failed to FTP Off.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    }
                }
            }

            processSec = calcUfCameraAdjustmentProcessSec(5);
            winProgress.SetRemainTimer(processSec);

            // Cabinet Power On 1Step
            winProgress.ShowMessage("Cabinet Power On.");
            winProgress.PutForward1Step();
            saveUfLog("Cabinet Power On.");
            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdUnitPowerOn, cont.IPAddress); }
            Thread.Sleep(10000);

            processSec = calcUfCameraAdjustmentProcessSec(6);
            winProgress.SetRemainTimer(processSec);

            // 終了時のカメラ位置を保存 1Step
            winProgress.ShowMessage("Store Camera Position.");
            winProgress.PutForward1Step();
            saveUfLog("Store Camera Position.");
            GetCameraPosUf(imgUfCamCameraView, out aryBlob, out CameraPosition endCamPos);            
            ufCamAdjLog.EndCamPos = endCamPos;

            // Cabinet位置(空間座標)を再設定(Log用)
            SetCabinetPos(lstTgtCabi, dist, wallH, camH);

            // カメラ設置ズレ分を反映(Log用)
            MoveCabinetPos(startCamPos.Pan, startCamPos.Tilt, startCamPos.Roll, startCamPos.X, startCamPos.Y, startCamPos.Z);

            // 内部信号停止
            stopIntSig();

            // 調整設定を解除 1Step
            winProgress.ShowMessage("Set Normal Settings.");
            winProgress.PutForward1Step();
            saveUfLog("Set Normal Settings.");
            //setNormalSetting();
            SetThroughMode(false);

            // ユーザー設定に書き戻し 1Step
            winProgress.ShowMessage("Restore User Settings.");
            winProgress.PutForward1Step();
            saveUfLog("Restore User Settings.");
            setUserSetting(lstUserSetting);

            //if (status != true)
            //{ throw new Exception("The camera position is inappropriate.\r\nAfter alignment, the camera is moving. Please re-align the camera."); }

            // 不要な画像ファイルの削除
            if (appliMode == ApplicationMode.Normal)
            { DeleteUnwantedImagesAdj(logDir); }
            
            // White画像表示
            outputFlatPattern(Settings.Ins.Camera.MeasLevelR, Settings.Ins.Camera.MeasLevelG, Settings.Ins.Camera.MeasLevelB);
        }

        /// <summary>
        /// カメラUF調整(Cabinetモード)
        /// </summary>
        /// <param name="lstTgtUnit">調整対象Cabinet</param>
        /// <param name="lstObjCabi">基準Cabinet</param>
        /// <param name="logDir">UF Log保存フォルダ</param>
        /// <param name="lstMoveFile">移動対象ファイル(hc.bin)</param>
        private void AdjustUfCamCabinet(List<UnitInfo> lstTgtUnit, List<UnitInfo> lstObjCabi, ObjectiveLine objEdge, ViewPoint vp, string logDir, out List<MoveFile> lstMoveFile)
        {
            saveUfLog("AdjustUfCamCabinet Start.");

            //List<UfCamUnitCpInfo> lstUnitCpInfo;
            List<UfCamCorrectionPoint> lstRefPoints; // 基準点

            List<UnitInfo> lstTargetUnit = new List<UnitInfo>();
            bool status;
            FileDirectory baseFileDir;

            lstMoveFile = new List<MoveFile>();

            // modified by Hotta 2024/10/02 for test
#if ForCrosstalkCameraUF_TEST
            double[,][] fmtOriginal = new double[allocInfo.MaxX, allocInfo.MaxY][];
#endif

#if NO_PROC
            UfCamCorrectionPoint.LoadFromXmlFile(tempPath + "RefPoint.xml", out refPoint);
            UfCamUnitCpInfo.LoadFromXmlFile(tempPath + "UnitCpInfo.xml", out lstUnitCpInfo);
#else
            // 測定エリアの取得 13Steps
            saveUfLog("GetCpCabinet Start.");
            GetCpCabinet(lstTgtUnit, lstObjCabi, objEdge, vp, logDir, out ufCamAdjLog.lstUnitCpInfo, out lstRefPoints);
            saveUfLog("GetCpCabinet End.");

            // added by Hotta 2024/09/09 for crosstalk
#if ForCrosstalkCameraUF    
            // クロストーク補正量の取得
            //LoadCrosstalkValue(lstRefPoints, ufCamAdjLog.lstUnitCpInfo);
            saveUfLog("LoadCrosstalkValue Start.");
            LoadCrosstalkValue(lstTgtUnit);
            saveUfLog("LoadCrosstalkValue End.");
#endif

            // 測定用画像(RAW)の取得・平均値の格納 14Steps
            saveUfLog("GetFlatImages Start.");
            GetFlatImages(ref lstRefPoints, ref ufCamAdjLog.lstUnitCpInfo, logDir);
            saveUfLog("GetFlatImages End.");
            //if (appliMode == ApplicationMode.Developer && Settings.Ins.ExecLog == true) // Logは必ず残すように仕様を変更したのでコメントアウト
            //{
            //    UfCamCorrectionPoint.SaveToXmlFile(logDir + "RefPoint.xml", lstRefPoints);
            //    UfCamAdjLog.SaveToXmlFile(logDir + "UnitCpInfo.xml", ufCamAdjLog);
            //}
#endif

            // 調整をするUnitをListに格納 [3]
            foreach (UfCamCabinetCpInfo unitCpInfo in ufCamAdjLog.lstUnitCpInfo)
            { lstTargetUnit.Add(unitCpInfo.Unit); }

            if (lstTargetUnit.Count == 0)
            { throw new Exception("The target cabinet area is not selected."); }

            // 調整データのバックアップがすべてあるか確認 [4]
            status = checkDataFile(lstTargetUnit, out baseFileDir);
            if (status != true)
            { throw new Exception("The UF data file(hc.bin) was not found."); }

            m_MakeUFData.SetTargetValue(ufTargetChrom.Red.x, ufTargetChrom.Red.y, ufTargetChrom.Green.x, ufTargetChrom.Green.y, ufTargetChrom.Blue.x, ufTargetChrom.Blue.y, ufTargetChrom.White.Lv, ufTargetChrom.White.x, ufTargetChrom.White.y);

            // Unit毎のLoop
            foreach (UfCamCabinetCpInfo unitCpInfo in ufCamAdjLog.lstUnitCpInfo)
            {
                winProgress.ShowMessage("Calc new hc.bin.(C" + unitCpInfo.Unit.ControllerID + "-" + unitCpInfo.Unit.PortNo + "-" + unitCpInfo.Unit.UnitNo + ")");
                saveUfLog($"UnitLoop Create New HC C{unitCpInfo.Unit.ControllerID}-{unitCpInfo.Unit.PortNo}-{unitCpInfo.Unit.UnitNo} Start.");

                // ●Cell Dataの補正データ抽出 [*1]
                string filePath = makeFilePath(unitCpInfo.Unit, baseFileDir);

                status = m_MakeUFData.ExtractFmt(filePath, allocInfo.LEDModel);
                if (status != true)
                { throw new Exception("Failed in ExtractFmt."); }

                // ●抽出した補正データと調整目標値から逆算してその時のXYZの値を求める [*2]                
                // modified by Hotta 2024/09/11 for crosstalk
#if ForCrosstalkCameraUF
                foreach (CrosstalkInfo info in m_lstCrosstalkInfo)
                {
                    if (info.ControllerID == unitCpInfo.Unit.ControllerID && info.X == unitCpInfo.Unit.X && info.Y == unitCpInfo.Unit.Y)
                    {
                        if (allocInfo.LEDModel == ZRD_BH12D
                            || allocInfo.LEDModel == ZRD_BH15D
                            || allocInfo.LEDModel == ZRD_CH12D
                            || allocInfo.LEDModel == ZRD_CH15D
                            || allocInfo.LEDModel == ZRD_BH12D_S3
                            || allocInfo.LEDModel == ZRD_BH15D_S3
                            || allocInfo.LEDModel == ZRD_CH12D_S3
                            || allocInfo.LEDModel == ZRD_CH15D_S3)
                        {
#if (DEBUG || Release_log)
                            if (Settings.Ins.ExecLog == true)
                            {
                                MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                            }
#endif
                            status = m_MakeUFData.Fmt2XYZ_Crosstalk(ufTargetChrom.Red.x, ufTargetChrom.Red.y, ufTargetChrom.Green.x, ufTargetChrom.Green.y, ufTargetChrom.Blue.x, ufTargetChrom.Blue.y, ufTargetChrom.White.Lv, ufTargetChrom.White.x, ufTargetChrom.White.y, info.Crosstalk);
                        }
                        else
                        {
#if (DEBUG || Release_log)
                            if (Settings.Ins.ExecLog == true)
                            {
                                MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                            }
#endif
                            status = m_MakeUFData.Fmt2XYZ(ufTargetChrom.Red.x, ufTargetChrom.Red.y, ufTargetChrom.Green.x, ufTargetChrom.Green.y, ufTargetChrom.Blue.x, ufTargetChrom.Blue.y, ufTargetChrom.White.Lv, ufTargetChrom.White.x, ufTargetChrom.White.y);
                        }
                        break;
                    }
                }
#else
                status = m_MakeUFData.Fmt2XYZ(ufTargetChrom.Red.x, ufTargetChrom.Red.y, ufTargetChrom.Green.x, ufTargetChrom.Green.y, ufTargetChrom.Blue.x, ufTargetChrom.Blue.y, ufTargetChrom.White.Lv, ufTargetChrom.White.x, ufTargetChrom.White.y);
#endif
                if (status != true)
                { throw new Exception("Failed in Fmt2XYZ."); }

                // 基準点から基準値(画素目標値)を計算する
                CalcReferenceValue(unitCpInfo, lstRefPoints, objEdge, out UfCamCorrectionPoint refPoint);

                // ●今回の計測値と前の計測値の差分を求めて、画素ごとのXYZデータを更新する [*3]
                if (ufCamAdjAlgo == UfAdjAlgorithm.CommonColor)
                {
                    // modified by Hotta 2024/09/27
#if ForCrosstalkCameraUF
                    if (allocInfo.LEDModel == ZRD_BH12D
                        || allocInfo.LEDModel == ZRD_BH15D
                        || allocInfo.LEDModel == ZRD_CH12D
                        || allocInfo.LEDModel == ZRD_CH15D
                        || allocInfo.LEDModel == ZRD_BH12D_S3
                        || allocInfo.LEDModel == ZRD_BH15D_S3
                        || allocInfo.LEDModel == ZRD_CH12D_S3
                        || allocInfo.LEDModel == ZRD_CH15D_S3)
                    {
                        status = m_MakeUFData.ModifyXYZCam(UfCamAdjustType.Cabinet, refPoint, unitCpInfo, true);
                    }
                    else
                    {
                        status = m_MakeUFData.ModifyXYZCam(UfCamAdjustType.Cabinet, refPoint, unitCpInfo, false);
                    }
#else
                    status = m_MakeUFData.ModifyXYZCam(UfCamAdjustType.Cabinet, refPoint, unitCpInfo);
#endif

                }
                else
                { status = m_MakeUFData.ModifyXYZCamCommonLed(UfCamAdjustType.Cabinet, refPoint, unitCpInfo); }
                if (status != true)
                { throw new Exception("Failed in ModifyXYZCam."); }

                // ●補正データを作成する [*4]
                double targetYw, targetYr, targetYg, targetYb;

                // modified by Hotta 2024/10/07
#if ForCrosstalkCameraUF
#else
                int ucr, ucg, ucb;
#endif

                // modified by Hotta 2024/09/12
#if ForCrosstalkCameraUF
                status = m_MakeUFData.Statistics_CameraUF(unitCpInfo.Unit, out targetYw, out targetYr, out targetYg, out targetYb);
#else
                status = m_MakeUFData.Statistics(-1, out targetYw, out targetYr, out targetYg, out targetYb, out ucr, out ucg, out ucb);
#endif
                if (status != true)
                { throw new Exception("Failed in Statistics."); }

                // ●ファイル保存 [*5]
                string ajustedFile = makeFilePath(unitCpInfo.Unit, FileDirectory.Temp);

                // フォルダがない場合、フォルダ作成
                if (System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(ajustedFile)) != true)
                { System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ajustedFile)); }

                try { status = m_MakeUFData.OverWritePixelData(ajustedFile, allocInfo.LEDModel, true); }
                catch { status = false; }
                if (status != true)
                { throw new Exception("Failed in OverWritePixelData."); }

                MoveFile move = new MoveFile();
                move.ControllerID = unitCpInfo.Unit.ControllerID;
                move.FilePath = ajustedFile;

                lstMoveFile.Add(move);

                saveUfLog("UnitLoop Create New HC End.");
            }

            // Log保存
            // 校正データのリセット
            if (appliMode != ApplicationMode.Developer)
            {
                foreach (UfCamCorrectionPoint refPoint in lstRefPoints)
                {
                    refPoint.CCV = new CameraCorrectionValue();
                    refPoint.CCV.CcdHash = ccd.SHA256Hash;
                    refPoint.CCV.LcdHash = lcd.SHA256Hash;
                    refPoint.LCV = new LedCorrectionValue();
                }
            }

            //UfCamCorrectionPoint.SaveToEncryptFile(logDir + "RefPoint.bin", refPoint, CalcKey(AES_IV), CalcKey(AES_Key));
            UfCamCorrectionPoint.SaveToXmlFile(logDir + "RefPoint.xml", lstRefPoints);

            saveUfLog("AdjustUfCamCabinet End.");
        }

        /// <summary>
        /// カメラUF調整(9ptモード)
        /// </summary>
        /// <param name="lstTgtCabi">調整対象Cabinet</param>
        /// <param name="lstObjCabi">基準Cabinet</param>
        /// <param name="objEdge">基準CabinetのLine時の指定情報</param>
        /// <param name="logDir">UF Log保存フォルダ</param>
        /// <param name="lstMoveFile">移動対象ファイル(hc.bin)</param>
        private void AdjustUfCam9pt(List<UnitInfo> lstTgtCabi, List<UnitInfo> lstObjCabi, ObjectiveLine objEdge, ViewPoint vp, string logDir, out List<MoveFile> lstMoveFile)
        {
            var dispatcher = System.Windows.Application.Current.Dispatcher;
            saveUfLog("AdjustUfCam9pt Start.");

            //List<UfCamUnitCpInfo> lstUnitCpInfo;
            List<UfCamCorrectionPoint> lstRefPoints; // 基準点

            List<UnitInfo> lstTargetUnit = new List<UnitInfo>();
            bool status;
            FileDirectory baseFileDir;

            lstMoveFile = new List<MoveFile>();

#if NO_PROC
            UfCamCorrectionPoint.LoadFromXmlFile(tempPath + "RefPoint.xml", out refPoint);
            UfCamUnitCpInfo.LoadFromXmlFile(tempPath + "UnitCpInfo.xml", out lstUnitCpInfo);
#else
            // 測定エリアの取得 23Steps
            saveUfLog("GetCp9pt Start.");
            GetCp9pt(lstTgtCabi, lstObjCabi, objEdge, vp, logDir, out ufCamAdjLog.lstUnitCpInfo, out lstRefPoints);
            saveUfLog("GetCp9pt End.");

            dispatcher.Invoke(() => winProgress.SetRemainTimer(calcUfCameraAdjustmentProcessSec(UF_RE_CALC_STEP_3_PROCESS_SEC, ufCamAdjLog.lstUnitCpInfo.Count)));

            // added by Hotta 2024/09/09 for crosstalk
#if ForCrosstalkCameraUF
            // クロストーク補正量の取得
            //LoadCrosstalkValue(lstRefPoints, ufCamAdjLog.lstUnitCpInfo);
            saveUfLog("LoadCrosstalkValue Start.");
            LoadCrosstalkValue(lstTgtCabi);
            saveUfLog("LoadCrosstalkValue End.");
#endif

            // 測定用画像(RAW)の取得・平均値の格納 14Steps
            saveUfLog("GetFlatImages Start.");
            GetFlatImages(ref lstRefPoints, ref ufCamAdjLog.lstUnitCpInfo, logDir);
            saveUfLog("GetFlatImages End.");

            //if (appliMode == ApplicationMode.Developer && Settings.Ins.ExecLog == true)
            //{
            //    UfCamCorrectionPoint.SaveToXmlFile(logDir + "RefPoint.xml", lstRefPoints);
            //    UfCamAdjLog.SaveToXmlFile(logDir + "UnitCpInfo.xml", ufCamAdjLog);
            //}
#endif

            // 調整をするUnitをListに格納 [3]
            foreach (UfCamCabinetCpInfo unitCpInfo in ufCamAdjLog.lstUnitCpInfo)
            { lstTargetUnit.Add(unitCpInfo.Unit); }

            if (lstTargetUnit.Count == 0)
            { throw new Exception("The target cabinet area is not selected."); }

            // 調整データのバックアップがすべてあるか確認 [4]
            status = checkDataFile(lstTargetUnit, out baseFileDir);
            if (status != true)
            { throw new Exception("The UF data file(hc.bin) was not found."); }

            m_MakeUFData.SetTargetValue(ufTargetChrom.Red.x, ufTargetChrom.Red.y, ufTargetChrom.Green.x, ufTargetChrom.Green.y, ufTargetChrom.Blue.x, ufTargetChrom.Blue.y, ufTargetChrom.White.Lv, ufTargetChrom.White.x, ufTargetChrom.White.y);
#if DEBUG
            if (Settings.Ins.ExecLog == true)
            {
                MainWindow.SaveExecLog("Method   : " + MethodBase.GetCurrentMethod().Name);
                MainWindow.SaveExecLog("LEDModel : " + allocInfo.LEDModel);
            }
#endif
            // Unit毎のLoop
            foreach (UfCamCabinetCpInfo unitCpInfo in ufCamAdjLog.lstUnitCpInfo)
            {
                winProgress.ShowMessage("Calc new hc.bin.(C" + unitCpInfo.Unit.ControllerID + "-" + unitCpInfo.Unit.PortNo + "-" + unitCpInfo.Unit.UnitNo + ")");
                saveUfLog($"UnitLoop Create New HC C{unitCpInfo.Unit.ControllerID}-{unitCpInfo.Unit.PortNo}-{unitCpInfo.Unit.UnitNo} Start.");

                // ●Cell Dataの補正データ抽出 [*1]
                string filePath = makeFilePath(unitCpInfo.Unit, baseFileDir);

                status = m_MakeUFData.ExtractFmt(filePath, allocInfo.LEDModel);
                if (status != true)
                { throw new Exception("Failed in ExtractFmt."); }

                // ●抽出した補正データと調整目標値から逆算してその時のXYZの値を求める [*2]                
                // modified by Hotta 2024/09/11 for crosstalk
#if ForCrosstalkCameraUF
                foreach (CrosstalkInfo info in m_lstCrosstalkInfo)
                {
                    if (info.ControllerID == unitCpInfo.Unit.ControllerID && info.X == unitCpInfo.Unit.X && info.Y == unitCpInfo.Unit.Y)
                    {
                        if (info.ControllerID == unitCpInfo.Unit.ControllerID && info.X == unitCpInfo.Unit.X && info.Y == unitCpInfo.Unit.Y)
                        {
                            if (allocInfo.LEDModel == ZRD_BH12D
                                || allocInfo.LEDModel == ZRD_BH15D
                                || allocInfo.LEDModel == ZRD_CH12D
                                || allocInfo.LEDModel == ZRD_CH15D
                                || allocInfo.LEDModel == ZRD_BH12D_S3
                                || allocInfo.LEDModel == ZRD_BH15D_S3
                                || allocInfo.LEDModel == ZRD_CH12D_S3
                                || allocInfo.LEDModel == ZRD_CH15D_S3)
                            {
                                status = m_MakeUFData.Fmt2XYZ_Crosstalk(ufTargetChrom.Red.x, ufTargetChrom.Red.y, ufTargetChrom.Green.x, ufTargetChrom.Green.y, ufTargetChrom.Blue.x, ufTargetChrom.Blue.y, ufTargetChrom.White.Lv, ufTargetChrom.White.x, ufTargetChrom.White.y, info.Crosstalk);
                            }
                            else
                            {
                                status = m_MakeUFData.Fmt2XYZ(ufTargetChrom.Red.x, ufTargetChrom.Red.y, ufTargetChrom.Green.x, ufTargetChrom.Green.y, ufTargetChrom.Blue.x, ufTargetChrom.Blue.y, ufTargetChrom.White.Lv, ufTargetChrom.White.x, ufTargetChrom.White.y);
                            }
                            break;
                        }
                    }
                }
#else
                status = m_MakeUFData.Fmt2XYZ(ufTargetChrom.Red.x, ufTargetChrom.Red.y, ufTargetChrom.Green.x, ufTargetChrom.Green.y, ufTargetChrom.Blue.x, ufTargetChrom.Blue.y, ufTargetChrom.White.Lv, ufTargetChrom.White.x, ufTargetChrom.White.y);
#endif
                if (status != true)
                { throw new Exception("Failed in Fmt2XYZ."); }

                // 基準点から基準値(画素目標値)を計算する
                CalcReferenceValue(unitCpInfo, lstRefPoints, objEdge, out UfCamCorrectionPoint refPoint);

                // ●今回の計測値と前の計測値の差分を求めて、画素ごとのXYZデータを更新する [*3]
                if (ufCamAdjAlgo == UfAdjAlgorithm.CommonColor)
                {
                    // modified by Hotta 2024/09/27
#if ForCrosstalkCameraUF
                    if (allocInfo.LEDModel == ZRD_BH12D
                        || allocInfo.LEDModel == ZRD_BH15D
                        || allocInfo.LEDModel == ZRD_CH12D
                        || allocInfo.LEDModel == ZRD_CH15D
                        || allocInfo.LEDModel == ZRD_BH12D_S3
                        || allocInfo.LEDModel == ZRD_BH15D_S3
                        || allocInfo.LEDModel == ZRD_CH12D_S3
                        || allocInfo.LEDModel == ZRD_CH15D_S3)
                    {
                        status = m_MakeUFData.ModifyXYZCam(UfCamAdjustType.Cabi_9pt, refPoint, unitCpInfo, true);
                    }
                    else
                    {
                        status = m_MakeUFData.ModifyXYZCam(UfCamAdjustType.Cabi_9pt, refPoint, unitCpInfo, false);
                    }
#else
                    status = m_MakeUFData.ModifyXYZCam(UfCamAdjustType.Cabi_9pt, refPoint, unitCpInfo);
#endif
                }
                else
                { status = m_MakeUFData.ModifyXYZCamCommonLed(UfCamAdjustType.Cabi_9pt, refPoint, unitCpInfo); }
                if (status != true)
                { throw new Exception("Failed in ModifyXYZCam."); }

                // ●補正データを作成する [*4]
                double targetYw, targetYr, targetYg, targetYb;
                // modified by Hotta 2024/10/07
#if ForCrosstalkCameraUF
#else
                int ucr, ucg, ucb;
#endif
                // modified by Hotta 2024/09/12
#if ForCrosstalkCameraUF
                status = m_MakeUFData.Statistics_CameraUF(unitCpInfo.Unit, out targetYw, out targetYr, out targetYg, out targetYb);
#else
                status = m_MakeUFData.Statistics_(-1, out targetYw, out targetYr, out targetYg, out targetYb, out ucr, out ucg, out ucb);
#endif
                if (status != true)
                { throw new Exception("Failed in Statistics."); }

                // ●ファイル保存 [*5]
                string ajustedFile = makeFilePath(unitCpInfo.Unit, FileDirectory.Temp);

                // フォルダがない場合、フォルダ作成
                if (System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(ajustedFile)) != true)
                { System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ajustedFile)); }

                try { status = m_MakeUFData.OverWritePixelData(ajustedFile, allocInfo.LEDModel, true); }
                catch { status = false; }
                if (status != true)
                { throw new Exception("Failed in OverWritePixelData."); }

                MoveFile move = new MoveFile();
                move.ControllerID = unitCpInfo.Unit.ControllerID;
                move.FilePath = ajustedFile;

                lstMoveFile.Add(move);

                saveUfLog($"UnitLoop Create New HC C{unitCpInfo.Unit.ControllerID}-{unitCpInfo.Unit.PortNo}-{unitCpInfo.Unit.UnitNo} End.");
            }

            // Log保存
            // 校正データのリセット
            if (appliMode != ApplicationMode.Developer)
            {
                foreach (UfCamCorrectionPoint refPoint in lstRefPoints)
                {
                    refPoint.CCV = new CameraCorrectionValue();
                    refPoint.CCV.CcdHash = ccd.SHA256Hash;
                    refPoint.CCV.LcdHash = lcd.SHA256Hash;
                    refPoint.LCV = new LedCorrectionValue();
                }
            }

            //UfCamCorrectionPoint.SaveToEncryptFile(logDir + "RefPoint.bin", refPoint, CalcKey(AES_IV), CalcKey(AES_Key));
            UfCamCorrectionPoint.SaveToXmlFile(logDir + "RefPoint.xml", lstRefPoints);

            saveUfLog("AdjustUfCam9pt End.");
        }

        /// <summary>
        /// カメラUF調整(Radiatorモード)
        /// </summary>
        /// <param name="lstTgtCabi"></param>
        /// <param name="lstObjCabi"></param>
        /// <param name="objEdge"></param>
        /// <param name="logDir"></param>
        /// <param name="lstMoveFile"></param>
        private void AdjustUfCamRadiator(List<UnitInfo> lstTgtCabi, List<UnitInfo> lstObjCabi, ObjectiveLine objEdge, ViewPoint vp, string logDir, out List<MoveFile> lstMoveFile)
        {
            var dispatcher = System.Windows.Application.Current.Dispatcher;
            saveUfLog("AdjustUfCamRadiator Start.");

            //List<UfCamUnitCpInfo> lstUnitCpInfo;
            List<UfCamCorrectionPoint> lstRefPoints; // 基準点

            List<UnitInfo> lstTargetUnit = new List<UnitInfo>();
            bool status;
            FileDirectory baseFileDir;

            lstMoveFile = new List<MoveFile>();

#if NO_CAP
            UfCamCorrectionPoint.LoadFromXmlFile(tempPath + "RefPoint.xml", out lstRefPoints);
            // modified by Hotta 2022/02/28
            // UfCamUnitCpInfo.LoadFromXmlFile(tempPath + "UnitCpInfo.xml", out lstUnitCpInfo);
            UfCamAdjLog.LoadFromXmlFile(tempPath + "UnitCpInfo.xml", out ufCamAdjLog);
#else
            // 測定エリアの取得 13Steps
            saveUfLog("GetCpRadiator Start.");
            GetCpRadiator(lstTgtCabi, lstObjCabi, objEdge, vp, logDir, out ufCamAdjLog.lstUnitCpInfo, out lstRefPoints);
            saveUfLog("GetCpRadiator End.");

            dispatcher.Invoke(() => winProgress.SetRemainTimer(calcUfCameraAdjustmentProcessSec(UF_RE_CALC_STEP_3_PROCESS_SEC, ufCamAdjLog.lstUnitCpInfo.Count)));

            // added by Hotta 2024/09/09 for crosstalk
#if ForCrosstalkCameraUF
            // クロストーク補正量の取得
            //LoadCrosstalkValue(lstRefPoints, ufCamAdjLog.lstUnitCpInfo);
            saveUfLog("LoadCrosstalkValue Start.");
            LoadCrosstalkValue(lstTgtCabi);
            saveUfLog("LoadCrosstalkValue End.");
#endif

            // 測定用画像(RAW)の取得・平均値の格納 14Steps
            saveUfLog("GetFlatImages Start.");
            GetFlatImages(ref lstRefPoints, ref ufCamAdjLog.lstUnitCpInfo, logDir);
            saveUfLog("GetFlatImages End.");

            //if (appliMode == ApplicationMode.Developer && Settings.Ins.ExecLog == true)
            //{
            //    UfCamCorrectionPoint.SaveToXmlFile(logDir + "RefPoint.xml", lstRefPoints);
            //    UfCamAdjLog.SaveToXmlFile(logDir + "UnitCpInfo.xml", ufCamAdjLog);
            //}
#endif

            // 調整をするCabinetをListに格納(Cameraの場合は対象Cabinet)
            foreach (UfCamCabinetCpInfo unitCpInfo in ufCamAdjLog.lstUnitCpInfo)
            { lstTargetUnit.Add(unitCpInfo.Unit); }

            if (lstTargetUnit.Count == 0)
            { throw new Exception("The target cabinet area is not selected."); }

            // 調整データのバックアップがすべてあるか確認 [4]
            status = checkDataFile(lstTargetUnit, out baseFileDir);
            if (status != true)
            { throw new Exception("The UF data file(hc.bin) was not found."); }

            m_MakeUFData.SetTargetValue(ufTargetChrom.Red.x, ufTargetChrom.Red.y, ufTargetChrom.Green.x, ufTargetChrom.Green.y, ufTargetChrom.Blue.x, ufTargetChrom.Blue.y, ufTargetChrom.White.Lv, ufTargetChrom.White.x, ufTargetChrom.White.y);

            // Cabinet毎のLoop
            foreach (UfCamCabinetCpInfo unitCpInfo in ufCamAdjLog.lstUnitCpInfo)
            {
                winProgress.ShowMessage("Calc new hc.bin.(C" + unitCpInfo.Unit.ControllerID + "-" + unitCpInfo.Unit.PortNo + "-" + unitCpInfo.Unit.UnitNo + ")");
                saveUfLog($"UnitLoop Create New HC C{unitCpInfo.Unit.ControllerID}-{unitCpInfo.Unit.PortNo}-{unitCpInfo.Unit.UnitNo} Start.");

                // ●Cell Dataの補正データ抽出 [*1]
                string filePath = makeFilePath(unitCpInfo.Unit, baseFileDir);

                status = m_MakeUFData.ExtractFmt(filePath, allocInfo.LEDModel);
                if (status != true)
                { throw new Exception("Failed in ExtractFmt."); }

                // ●抽出した補正データと調整目標値から逆算してその時のXYZの値を求める [*2]                
                // modified by Hotta 2024/09/11 for crosstalk
#if ForCrosstalkCameraUF
                foreach (CrosstalkInfo info in m_lstCrosstalkInfo)
                {
                    if (info.ControllerID == unitCpInfo.Unit.ControllerID && info.X == unitCpInfo.Unit.X && info.Y == unitCpInfo.Unit.Y)
                    {
                        if (info.ControllerID == unitCpInfo.Unit.ControllerID && info.X == unitCpInfo.Unit.X && info.Y == unitCpInfo.Unit.Y)
                        {
                            if (allocInfo.LEDModel == ZRD_BH12D
                                || allocInfo.LEDModel == ZRD_BH15D
                                || allocInfo.LEDModel == ZRD_CH12D
                                || allocInfo.LEDModel == ZRD_CH15D
                                || allocInfo.LEDModel == ZRD_BH12D_S3
                                || allocInfo.LEDModel == ZRD_BH15D_S3
                                || allocInfo.LEDModel == ZRD_CH12D_S3
                                || allocInfo.LEDModel == ZRD_CH15D_S3)
                            {
#if (DEBUG || Release_log)
                                if (Settings.Ins.ExecLog == true)
                                {
                                    MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                                }
#endif
                                status = m_MakeUFData.Fmt2XYZ_Crosstalk(ufTargetChrom.Red.x, ufTargetChrom.Red.y, ufTargetChrom.Green.x, ufTargetChrom.Green.y, ufTargetChrom.Blue.x, ufTargetChrom.Blue.y, ufTargetChrom.White.Lv, ufTargetChrom.White.x, ufTargetChrom.White.y, info.Crosstalk);
                            }
                            else
                            {
#if (DEBUG || Release_log)
                                if (Settings.Ins.ExecLog == true)
                                {
                                    MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                                }
#endif
                                status = m_MakeUFData.Fmt2XYZ(ufTargetChrom.Red.x, ufTargetChrom.Red.y, ufTargetChrom.Green.x, ufTargetChrom.Green.y, ufTargetChrom.Blue.x, ufTargetChrom.Blue.y, ufTargetChrom.White.Lv, ufTargetChrom.White.x, ufTargetChrom.White.y);
                            }
                            break;
                        }
                    }
                }
#else
                status = m_MakeUFData.Fmt2XYZ(ufTargetChrom.Red.x, ufTargetChrom.Red.y, ufTargetChrom.Green.x, ufTargetChrom.Green.y, ufTargetChrom.Blue.x, ufTargetChrom.Blue.y, ufTargetChrom.White.Lv, ufTargetChrom.White.x, ufTargetChrom.White.y);
#endif
                if (status != true)
                { throw new Exception("Failed in Fmt2XYZ."); }

                // 基準点から基準値(画素目標値)を計算する
                CalcReferenceValue(unitCpInfo, lstRefPoints, objEdge, out UfCamCorrectionPoint refPoint);

                // ●今回の計測値と前の計測値の差分を求めて、画素ごとのXYZデータを更新する [*3]
                if (ufCamAdjAlgo == UfAdjAlgorithm.CommonColor)
                {
                    // modified by Hotta 2024/09/27
#if ForCrosstalkCameraUF
                    if (allocInfo.LEDModel == ZRD_BH12D
                        || allocInfo.LEDModel == ZRD_BH15D
                        || allocInfo.LEDModel == ZRD_CH12D
                        || allocInfo.LEDModel == ZRD_CH15D
                        || allocInfo.LEDModel == ZRD_BH12D_S3
                        || allocInfo.LEDModel == ZRD_BH15D_S3
                        || allocInfo.LEDModel == ZRD_CH12D_S3
                        || allocInfo.LEDModel == ZRD_CH15D_S3)
                    {
                        status = m_MakeUFData.ModifyXYZCam(UfCamAdjustType.Radiator, refPoint, unitCpInfo, true);
                    }
                    else
                    {
                        status = m_MakeUFData.ModifyXYZCam(UfCamAdjustType.Radiator, refPoint, unitCpInfo, false);
                    }
#else
                    status = m_MakeUFData.ModifyXYZCam(UfCamAdjustType.Radiator, refPoint, unitCpInfo);
#endif
                
                }
                else
                { status = m_MakeUFData.ModifyXYZCamCommonLed(UfCamAdjustType.Radiator, refPoint, unitCpInfo); }
                if (status != true)
                { throw new Exception("Failed in ModifyXYZCam."); }

                // ●補正データを作成する [*4]
                double targetYw, targetYr, targetYg, targetYb;
                // modified by Hotta 2024/10/07
#if ForCrosstalkCameraUF
#else
                int ucr, ucg, ucb;
#endif
                // modified by Hotta 2024/09/12
#if ForCrosstalkCameraUF
                status = m_MakeUFData.Statistics_CameraUF(unitCpInfo.Unit, out targetYw, out targetYr, out targetYg, out targetYb);
#else
                status = m_MakeUFData.Statistics(-1, out targetYw, out targetYr, out targetYg, out targetYb, out ucr, out ucg, out ucb);
#endif
                if (status != true)
                { throw new Exception("Failed in Statistics."); }

                // ●ファイル保存 [*5]
                string ajustedFile = makeFilePath(unitCpInfo.Unit, FileDirectory.Temp);

                // フォルダがない場合、フォルダ作成
                if (System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(ajustedFile)) != true)
                { System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ajustedFile)); }

                try { status = m_MakeUFData.OverWritePixelData(ajustedFile, allocInfo.LEDModel, true); }
                catch { status = false; }
                if (status != true)
                { throw new Exception("Failed in OverWritePixelData."); }

                // 移動ファイルリストに追加
                MoveFile move = new MoveFile();
                move.ControllerID = unitCpInfo.Unit.ControllerID;
                move.FilePath = ajustedFile;

                lstMoveFile.Add(move);

                saveUfLog($"UnitLoop Create New HC C{unitCpInfo.Unit.ControllerID}-{unitCpInfo.Unit.PortNo}-{unitCpInfo.Unit.UnitNo} End.");
            }

            // Log保存
            // 校正データのリセット
            if (appliMode != ApplicationMode.Developer)
            {
                foreach (UfCamCorrectionPoint refPoint in lstRefPoints)
                {
                    refPoint.CCV = new CameraCorrectionValue();
                    refPoint.CCV.CcdHash = ccd.SHA256Hash;
                    refPoint.CCV.LcdHash = lcd.SHA256Hash;
                    refPoint.LCV = new LedCorrectionValue();
                }
            }

            //UfCamCorrectionPoint.SaveToEncryptFile(logDir + "RefPoint.bin", refPoint, CalcKey(AES_IV), CalcKey(AES_Key));
            UfCamCorrectionPoint.SaveToXmlFile(logDir + "RefPoint.xml", lstRefPoints);

            saveUfLog("AdjustUfCamRadiator End.");
        }

        /// <summary>
        /// カメラUF調整(Moduleモード)
        /// </summary>
        /// <param name="lstTgtCabi"></param>
        /// <param name="lstObjCabi"></param>
        /// <param name="objEdge"></param>
        /// <param name="logDir"></param>
        /// <param name="lstMoveFile"></param>
        private void AdjustUfCamEachModule(List<UnitInfo> lstTgtCabi, List<UnitInfo> lstObjCabi, ObjectiveLine objEdge, ViewPoint vp, string logDir, out List<MoveFile> lstMoveFile)
        {
            var dispatcher = System.Windows.Application.Current.Dispatcher;
            saveUfLog("AdjustUfCamEachModule Start.");

            //List<UfCamUnitCpInfo> lstUnitCpInfo;
            List<UfCamCorrectionPoint> lstRefPoints; // 基準点

            List<UnitInfo> lstTargetUnit = new List<UnitInfo>();
            bool status;
            FileDirectory baseFileDir;

            lstMoveFile = new List<MoveFile>();

#if NO_PROC
            UfCamCorrectionPoint.LoadFromXmlFile(tempPath + "RefPoint.xml", out refPoint);
            UfCamUnitCpInfo.LoadFromXmlFile(tempPath + "UnitCpInfo.xml", out lstUnitCpInfo);
#else
            // 測定エリアの取得 20 / 28steps(4 + ModuleCount * 2)
            saveUfLog("GetCpEachModule Start.");
            GetCpEachModule(lstTgtCabi, lstObjCabi, objEdge, vp, logDir, out ufCamAdjLog.lstUnitCpInfo, out lstRefPoints);
            saveUfLog("GetCpEachModule End.");

            dispatcher.Invoke(() => winProgress.SetRemainTimer(calcUfCameraAdjustmentProcessSec(UF_RE_CALC_STEP_3_PROCESS_SEC, ufCamAdjLog.lstUnitCpInfo.Count)));

            // added by Hotta 2024/09/09 for crosstalk
#if ForCrosstalkCameraUF
            // クロストーク補正量の取得
            //LoadCrosstalkValue(lstRefPoints, ufCamAdjLog.lstUnitCpInfo);
            saveUfLog("LoadCrosstalkValue Start.");
            LoadCrosstalkValue(lstTgtCabi);
            saveUfLog("LoadCrosstalkValue End.");
#endif

            // 測定用画像(RAW)の取得・平均値の格納 14Steps
            // ここで、カメラの撮影明るさから、クロストーク補正量をキャンセルする
            saveUfLog("GetFlatImages Start.");
            GetFlatImages(ref lstRefPoints, ref ufCamAdjLog.lstUnitCpInfo, logDir);
            saveUfLog("GetFlatImages End.");
#endif

            // 調整をするUnitをListに格納 [3]
            foreach (UfCamCabinetCpInfo unitCpInfo in ufCamAdjLog.lstUnitCpInfo)
            { lstTargetUnit.Add(unitCpInfo.Unit); }

            if (lstTargetUnit.Count == 0)
            { throw new Exception("The target cabinet area is not selected."); }

            // 調整データのバックアップがすべてあるか確認 [4]
            status = checkDataFile(lstTargetUnit, out baseFileDir);
            if (status != true)
            { throw new Exception("The UF data file(hc.bin) was not found."); }

            m_MakeUFData.SetTargetValue(ufTargetChrom.Red.x, ufTargetChrom.Red.y, ufTargetChrom.Green.x, ufTargetChrom.Green.y, ufTargetChrom.Blue.x, ufTargetChrom.Blue.y, ufTargetChrom.White.Lv, ufTargetChrom.White.x, ufTargetChrom.White.y);

            // Unit毎のLoop
            foreach (UfCamCabinetCpInfo unitCpInfo in ufCamAdjLog.lstUnitCpInfo)
            {
                winProgress.ShowMessage("Calc new hc.bin.(C" + unitCpInfo.Unit.ControllerID + "-" + unitCpInfo.Unit.PortNo + "-" + unitCpInfo.Unit.UnitNo + ")");
                saveUfLog($"UnitLoop Create New HC C{unitCpInfo.Unit.ControllerID}-{unitCpInfo.Unit.PortNo}-{unitCpInfo.Unit.UnitNo} Start.");

                // ●Cell Dataの補正データ抽出 [*1]
                string filePath = makeFilePath(unitCpInfo.Unit, baseFileDir);

                status = m_MakeUFData.ExtractFmt(filePath, allocInfo.LEDModel);
                if (status != true)
                { throw new Exception("Failed in ExtractFmt."); }

                // ●抽出した補正データと調整目標値から逆算してその時のXYZの値を求める [*2]                
                // modified by Hotta 2024/09/11 for crosstalk
#if ForCrosstalkCameraUF
                foreach (CrosstalkInfo info in m_lstCrosstalkInfo)
                {
                    if(info.ControllerID == unitCpInfo.Unit.ControllerID && info.X == unitCpInfo.Unit.X && info.Y == unitCpInfo.Unit.Y)
                    {
                        if (info.ControllerID == unitCpInfo.Unit.ControllerID && info.X == unitCpInfo.Unit.X && info.Y == unitCpInfo.Unit.Y)
                        {
                            if (allocInfo.LEDModel == ZRD_BH12D
                                || allocInfo.LEDModel == ZRD_BH15D
                                || allocInfo.LEDModel == ZRD_CH12D
                                || allocInfo.LEDModel == ZRD_CH15D
                                || allocInfo.LEDModel == ZRD_BH12D_S3
                                || allocInfo.LEDModel == ZRD_BH15D_S3
                                || allocInfo.LEDModel == ZRD_CH12D_S3
                                || allocInfo.LEDModel == ZRD_CH15D_S3)
                            {
#if (DEBUG || Release_log)
                                if (Settings.Ins.ExecLog == true)
                                {
                                    MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                                }
#endif
                                status = m_MakeUFData.Fmt2XYZ_Crosstalk(ufTargetChrom.Red.x, ufTargetChrom.Red.y, ufTargetChrom.Green.x, ufTargetChrom.Green.y, ufTargetChrom.Blue.x, ufTargetChrom.Blue.y, ufTargetChrom.White.Lv, ufTargetChrom.White.x, ufTargetChrom.White.y, info.Crosstalk);
                            }
                            else
                            {
#if (DEBUG || Release_log)
                                if (Settings.Ins.ExecLog == true)
                                {
                                    MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                                }
#endif
                                status = m_MakeUFData.Fmt2XYZ(ufTargetChrom.Red.x, ufTargetChrom.Red.y, ufTargetChrom.Green.x, ufTargetChrom.Green.y, ufTargetChrom.Blue.x, ufTargetChrom.Blue.y, ufTargetChrom.White.Lv, ufTargetChrom.White.x, ufTargetChrom.White.y);
                            }
                            break;
                        }
                    }
                }
#else
                status = m_MakeUFData.Fmt2XYZ(ufTargetChrom.Red.x, ufTargetChrom.Red.y, ufTargetChrom.Green.x, ufTargetChrom.Green.y, ufTargetChrom.Blue.x, ufTargetChrom.Blue.y, ufTargetChrom.White.Lv, ufTargetChrom.White.x, ufTargetChrom.White.y);
#endif
                if (status != true)
                { throw new Exception("Failed in Fmt2XYZ."); }

                // 基準点から基準値(画素目標値)を計算する
                CalcReferenceValue(unitCpInfo, lstRefPoints, objEdge, out UfCamCorrectionPoint refPoint);

                // ●今回の計測値と前の計測値の差分を求めて、画素ごとのXYZデータを更新する [*3]
                if (ufCamAdjAlgo == UfAdjAlgorithm.CommonColor)
                {
                    // modified by Hotta 2024/09/27
#if ForCrosstalkCameraUF
                    if (allocInfo.LEDModel == ZRD_BH12D
                        || allocInfo.LEDModel == ZRD_BH15D
                        || allocInfo.LEDModel == ZRD_CH12D
                        || allocInfo.LEDModel == ZRD_CH15D
                        || allocInfo.LEDModel == ZRD_BH12D_S3
                        || allocInfo.LEDModel == ZRD_BH15D_S3
                        || allocInfo.LEDModel == ZRD_CH12D_S3
                        || allocInfo.LEDModel == ZRD_CH15D_S3)
                    {
                        status = m_MakeUFData.ModifyXYZCam(UfCamAdjustType.EachModule, refPoint, unitCpInfo, true);
                    }
                    else
                    {
                        status = m_MakeUFData.ModifyXYZCam(UfCamAdjustType.EachModule, refPoint, unitCpInfo, false);
                    }
#else
                    status = m_MakeUFData.ModifyXYZCam(UfCamAdjustType.EachModule, refPoint, unitCpInfo);
#endif
                }
                else
                { status = m_MakeUFData.ModifyXYZCamCommonLed(UfCamAdjustType.EachModule, refPoint, unitCpInfo); }
                if (status != true)
                { throw new Exception("Failed in ModifyXYZCam."); }

                // ●補正データを作成する [*4]
                double targetYw, targetYr, targetYg, targetYb;
                // modified by Hotta 2024/10/07
#if ForCrosstalkCameraUF
#else
                int ucr, ucg, ucb;
#endif
                // modified by Hotta 2024/09/13
#if ForCrosstalkCameraUF
                status = m_MakeUFData.Statistics_CameraUF(unitCpInfo.Unit, out targetYw, out targetYr, out targetYg, out targetYb);
#else
                status = m_MakeUFData.Statistics(-1, out targetYw, out targetYr, out targetYg, out targetYb, out ucr, out ucg, out ucb);
#endif
                if (status != true)
                { throw new Exception("Failed in Statistics."); }

                // ●ファイル保存 [*5]
                string ajustedFile = makeFilePath(unitCpInfo.Unit, FileDirectory.Temp);

                // フォルダがない場合、フォルダ作成
                if (System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(ajustedFile)) != true)
                { System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ajustedFile)); }

                try { status = m_MakeUFData.OverWritePixelData(ajustedFile, allocInfo.LEDModel, true); }
                catch { status = false; }
                if (status != true)
                { throw new Exception("Failed in OverWritePixelData."); }

                MoveFile move = new MoveFile();
                move.ControllerID = unitCpInfo.Unit.ControllerID;
                move.FilePath = ajustedFile;

                lstMoveFile.Add(move);

                saveUfLog($"UnitLoop Create New HC C{unitCpInfo.Unit.ControllerID}-{unitCpInfo.Unit.PortNo}-{unitCpInfo.Unit.UnitNo} End.");
            }

            // Log保存
            // 校正データのリセット
            if (appliMode != ApplicationMode.Developer)
            {
                foreach (UfCamCorrectionPoint refPoint in lstRefPoints)
                {
                    refPoint.CCV = new CameraCorrectionValue();
                    refPoint.CCV.CcdHash = ccd.SHA256Hash;
                    refPoint.CCV.LcdHash = lcd.SHA256Hash;
                    refPoint.LCV = new LedCorrectionValue();
                }
            }

            //UfCamCorrectionPoint.SaveToEncryptFile(logDir + "RefPoint.bin", refPoint, CalcKey(AES_IV), CalcKey(AES_Key));
            UfCamCorrectionPoint.SaveToXmlFile(logDir + "RefPoint.xml", lstRefPoints);

            saveUfLog("AdjustUfCamEachModule End.");
        }
    
        /// <summary>
        /// 各調整点(Correction Point(Cp))の座標情報を格納する。
        /// 同時に使用する校正テーブルの値(レンズ校正テーブルとLED校正テーブルの積)を格納する。
        /// </summary>
        /// <param name="lstTgtCabi">調整対象Cabinet</param>
        /// <param name="lstObjCabi">基準Cabinet</param>
        /// <param name="logDir">UF Log保存フォルダ</param>
        /// <param name="lstCabiCpInfo">調整対象Cabinetの調整点リスト</param>
        /// <param name="lstRefPoints">基準点</param>
        private void GetCpCabinet(List<UnitInfo> lstTgtCabi, List<UnitInfo> lstObjCabi, ObjectiveLine objEdge, ViewPoint vp, string logDir, out List<UfCamCabinetCpInfo> lstCabiCpInfo, out List<UfCamCorrectionPoint> lstRefPoints)
        {
            List<UnitInfo> lstAdjCabi = null;

            lstCabiCpInfo = new List<UfCamCabinetCpInfo>();
            //lstRefPoints = new List<UfCamCorrectionPoint>();

#region 撮影
#if NO_CAP
            //outputTrimAreaRefDemo(Quadrant.Quad_1, out refPoint.Unit); // RefUnitの格納も行っている
            UfCamCorrectionPoint refPoint = new UfCamCorrectionPoint();
            Quadrant quad = Quadrant.NA;
#else

            // Black
            winProgress.ShowMessage("Capture Black Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Black Image.");
            stopIntSig(); // Black出力(信号Stop)
            CaptureImage(logDir + fn_BlackFile, Settings.Ins.Camera.TrimAreaSetting);

            // 基準点
            CaptureRefPoint(lstTgtCabi, lstObjCabi, objEdge, logDir, out lstAdjCabi, out UfCamCorrectionPoint refPoint);
            
            // マスク用画像を撮影
            winProgress.ShowMessage("Capture Mask Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Mask Image.");
            OutputTargetArea(lstAdjCabi);
            CaptureImage(logDir + System.IO.Path.GetFileNameWithoutExtension(fn_AreaFile));

            // 各Positionを撮影
            // Top
            winProgress.ShowMessage("Capture Top Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Top Image.");
            outputTrimArea(UfCamCorrectPos.Cabinet_Top);
            CaptureImage(logDir + UfCamCorrectPos.Cabinet_Top.ToString());

            // Right
            winProgress.ShowMessage("Capture Right Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Right Image.");
            outputTrimArea(UfCamCorrectPos.Cabinet_Right);
            CaptureImage(logDir + UfCamCorrectPos.Cabinet_Right.ToString());

            // Bottom
            winProgress.ShowMessage("Capture Bottom Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Bottom Image.");
            outputTrimArea(UfCamCorrectPos.Cabinet_Bottom);
            CaptureImage(logDir + UfCamCorrectPos.Cabinet_Bottom.ToString());

            // Left
            winProgress.ShowMessage("Capture Left Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Left Image.");
            outputTrimArea(UfCamCorrectPos.Cabinet_Left);
            CaptureImage(logDir + UfCamCorrectPos.Cabinet_Left.ToString());
#endif

            // 内部信号停止
            stopIntSig();

#endregion 撮影

#region 処理

            // 対象エリアのマスク画像を作成
            winProgress.ShowMessage("Generate Mask Image.");
            winProgress.PutForward1Step();
            saveUfLog("Generate Mask Image.");
            string file = logDir + fn_AreaFile;
            string blackFile = logDir + fn_BlackFile + ".arw";
            MakeMaskImageArw(file, blackFile);

            // 基準点
            ProcRefPoint(lstTgtCabi, lstAdjCabi, refPoint, out lstRefPoints, objEdge, logDir, blackFile, ref vp);
           
            // 各ポジション
            // 矩形にのみ対応
            UfCamCabinetCpInfo unit;

            foreach(UnitInfo tgt in lstAdjCabi)
            {
                unit = new UfCamCabinetCpInfo();
                unit.Unit = tgt;
                lstCabiCpInfo.Add(unit);
            }

            int cabiCnt = lstAdjCabi.Count; // 調整対象Cabinetの数

            // Top
            winProgress.ShowMessage("Load Top Image.");
            winProgress.PutForward1Step();
            saveUfLog("Load Top Image.");
            CalcTrimmingAreaMask(logDir + UfCamCorrectPos.Cabinet_Top.ToString() + ".arw", blackFile, logDir + fn_MaskTrim + "_" + UfCamCorrectPos.Cabinet_Top.ToString() + ".jpg", out List<Area> lstArea, out CvBlobs trimBlobs);
            if (lstArea.Count != cabiCnt)
            { throw new Exception("The number of correction points is incorrect.(Top)"); }

            StoreCp(ref lstCabiCpInfo, UfCamCorrectPos.Cabinet_Top, trimBlobs, vp);

            // Right
            winProgress.ShowMessage("Load Right Image.");
            winProgress.PutForward1Step();
            saveUfLog("Load Right Image.");
            CalcTrimmingAreaMask(logDir + UfCamCorrectPos.Cabinet_Right.ToString() + ".arw", blackFile, logDir + fn_MaskTrim + "_" + UfCamCorrectPos.Cabinet_Right.ToString() + ".jpg", out lstArea, out trimBlobs);
            if (lstArea.Count != cabiCnt)
            { throw new Exception("The number of correction points is incorrect.(Right)"); }

            StoreCp(ref lstCabiCpInfo, UfCamCorrectPos.Cabinet_Right, trimBlobs, vp);

            // Bottom
            winProgress.ShowMessage("Load Bottom Image.");
            winProgress.PutForward1Step();
            saveUfLog("Load Bottom Image.");
            CalcTrimmingAreaMask(logDir + UfCamCorrectPos.Cabinet_Bottom.ToString() + ".arw", blackFile, logDir + fn_MaskTrim + "_" + UfCamCorrectPos.Cabinet_Bottom.ToString() + ".jpg", out lstArea, out trimBlobs);
            if (lstArea.Count != cabiCnt)
            { throw new Exception("The number of correction points is incorrect.(Bottom)"); }

            StoreCp(ref lstCabiCpInfo, UfCamCorrectPos.Cabinet_Bottom, trimBlobs, vp);

            // Left
            winProgress.ShowMessage("Load Left Image.");
            winProgress.PutForward1Step();
            saveUfLog("Load Left Image.");
            CalcTrimmingAreaMask(logDir + UfCamCorrectPos.Cabinet_Left.ToString() + ".arw", blackFile, logDir + fn_MaskTrim + "_" + UfCamCorrectPos.Cabinet_Left.ToString() + ".jpg", out lstArea, out trimBlobs);
            if (lstArea.Count != cabiCnt)
            { throw new Exception("The number of correction points is incorrect.(Left)"); }

            StoreCp(ref lstCabiCpInfo, UfCamCorrectPos.Cabinet_Left, trimBlobs, vp);

            // TrimAreaのマージ画像を生成
            MergeTrimMask(logDir);

#endregion 処理
        }

        private void GetCp9pt(List<UnitInfo> lstTgtCabi, List<UnitInfo> lstObjCabi, ObjectiveLine objEdge, ViewPoint vp, string logDir, out List<UfCamCabinetCpInfo> lstUnitCpInfo, out List<UfCamCorrectionPoint> lstRefPoints)
        {
            List<UnitInfo> lstAdjCabi = null;

            lstUnitCpInfo = new List<UfCamCabinetCpInfo>();
            //lstRefPoints = new List<UfCamCorrectionPoint>();

#region 撮影
#if NO_CAP
            //outputTrimAreaRefDemo(Quadrant.Quad_1, out refPoint.Unit); // RefUnitの格納も行っている
            UfCamCorrectionPoint refPoint = new UfCamCorrectionPoint();
            Quadrant quad = Quadrant.NA;
#else

            // Black
            winProgress.ShowMessage("Capture Black Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Black Image.");
            stopIntSig(); // Black出力(信号Stop)
            CaptureImage(logDir + fn_BlackFile, Settings.Ins.Camera.TrimAreaSetting);

            // 基準点
            CaptureRefPoint(lstTgtCabi, lstObjCabi, objEdge, logDir, out lstAdjCabi, out UfCamCorrectionPoint refPoint);
            
            // マスク用画像を撮影
            winProgress.ShowMessage("Capture Mask Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Mask Image.");
            OutputTargetArea(lstAdjCabi);
            CaptureImage(logDir + System.IO.Path.GetFileNameWithoutExtension(fn_AreaFile));

            // 各Positionを撮影
            // Top-Left
            winProgress.ShowMessage("Capture Top-Left Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Top-Left Image.");
            outputTrimArea(UfCamCorrectPos._9pt_TopLeft);
            CaptureImage(logDir + UfCamCorrectPos._9pt_TopLeft.ToString());

            // Top
            winProgress.ShowMessage("Capture Top Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Top Image.");
            outputTrimArea(UfCamCorrectPos.Cabinet_Top);
            CaptureImage(logDir + UfCamCorrectPos.Cabinet_Top.ToString());

            // Top-Right
            winProgress.ShowMessage("Capture Top-Right Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Top-Right Image.");
            outputTrimArea(UfCamCorrectPos._9pt_TopRight);
            CaptureImage(logDir + UfCamCorrectPos._9pt_TopRight.ToString());

            // Left
            winProgress.ShowMessage("Capture Left Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Left Image.");
            outputTrimArea(UfCamCorrectPos.Cabinet_Left);
            CaptureImage(logDir + UfCamCorrectPos.Cabinet_Left.ToString());

            // Center
            winProgress.ShowMessage("Capture Center Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Center Image.");
            outputTrimArea(UfCamCorrectPos._9pt_Center);
            CaptureImage(logDir + UfCamCorrectPos._9pt_Center.ToString());

            // Right
            winProgress.ShowMessage("Capture Right Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Right Image.");
            outputTrimArea(UfCamCorrectPos.Cabinet_Right);
            CaptureImage(logDir + UfCamCorrectPos.Cabinet_Right.ToString());

            // Bottom-Left
            winProgress.ShowMessage("Capture Bottom-Left Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Bottom-Left Image.");
            outputTrimArea(UfCamCorrectPos._9pt_BottomLeft);
            CaptureImage(logDir + UfCamCorrectPos._9pt_BottomLeft.ToString());

            // Bottom
            winProgress.ShowMessage("Capture Bottom Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Bottom Image.");
            outputTrimArea(UfCamCorrectPos.Cabinet_Bottom);
            CaptureImage(logDir + UfCamCorrectPos.Cabinet_Bottom.ToString());

            // Bottom-Right
            winProgress.ShowMessage("Capture Bottom-Right Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Bottom-Right Image.");
            outputTrimArea(UfCamCorrectPos._9pt_BottomRight);
            CaptureImage(logDir + UfCamCorrectPos._9pt_BottomRight.ToString());
#endif

            // 内部信号停止
            stopIntSig();

#endregion 撮影

#region 処理

            // 対象エリアのマスク画像を作成
            winProgress.ShowMessage("Generate Mask Image.");
            winProgress.PutForward1Step();
            saveUfLog("Generate Mask Image.");
            string file = logDir + fn_AreaFile;
            string blackFile = logDir + fn_BlackFile + ".arw";
            MakeMaskImageArw(file, blackFile);

            // 基準点
            ProcRefPoint(lstTgtCabi, lstAdjCabi, refPoint, out lstRefPoints, objEdge, logDir, blackFile, ref vp);

            // 各ポジション
            // 矩形にのみ対応
            UfCamCabinetCpInfo unit;

            foreach (UnitInfo tgt in lstAdjCabi)
            {
                unit = new UfCamCabinetCpInfo();
                unit.Unit = tgt;
                lstUnitCpInfo.Add(unit);
            }

            int cabiCnt = lstAdjCabi.Count; // 調整対象Cabinetの数

            // Top-Left
            winProgress.ShowMessage("Load Top-Left Image.");
            winProgress.PutForward1Step();
            saveUfLog("Load Top-Left Image.");
            CalcTrimmingAreaMask(logDir + UfCamCorrectPos._9pt_TopLeft.ToString() + ".arw", blackFile, logDir + fn_MaskTrim + "_" + UfCamCorrectPos._9pt_TopLeft.ToString() + ".jpg", out List<Area> lstArea, out CvBlobs trimBlobs);
            if (lstArea.Count != cabiCnt)
            { throw new Exception("The number of correction points is incorrect.(Top-Left)"); }

            StoreCp(ref lstUnitCpInfo, UfCamCorrectPos._9pt_TopLeft, trimBlobs, vp);

            // Top
            winProgress.ShowMessage("Load Top Image.");
            winProgress.PutForward1Step();
            saveUfLog("Load Top Image.");
            CalcTrimmingAreaMask(logDir + UfCamCorrectPos.Cabinet_Top.ToString() + ".arw", blackFile, logDir + fn_MaskTrim + "_" + UfCamCorrectPos.Cabinet_Top.ToString() + ".jpg", out lstArea, out trimBlobs);
            if (lstArea.Count != cabiCnt)
            { throw new Exception("The number of correction points is incorrect.(Top)"); }

            StoreCp(ref lstUnitCpInfo, UfCamCorrectPos.Cabinet_Top, trimBlobs, vp);

            // Top-Right
            winProgress.ShowMessage("Load Top-Right Image.");
            winProgress.PutForward1Step();
            saveUfLog("Load Top-Right Image.");
            CalcTrimmingAreaMask(logDir + UfCamCorrectPos._9pt_TopRight.ToString() + ".arw", blackFile, logDir + fn_MaskTrim + "_" + UfCamCorrectPos._9pt_TopRight.ToString() + ".jpg", out lstArea, out trimBlobs);
            if (lstArea.Count != cabiCnt)
            { throw new Exception("The number of correction points is incorrect.(Top-Right)"); }

            StoreCp(ref lstUnitCpInfo, UfCamCorrectPos._9pt_TopRight, trimBlobs, vp);

            // Left
            winProgress.ShowMessage("Load Left Image.");
            winProgress.PutForward1Step();
            saveUfLog("Load Left Image.");
            CalcTrimmingAreaMask(logDir + UfCamCorrectPos.Cabinet_Left.ToString() + ".arw", blackFile, logDir + fn_MaskTrim + "_" + UfCamCorrectPos.Cabinet_Left.ToString() + ".jpg", out lstArea, out trimBlobs);
            if (lstArea.Count != cabiCnt)
            { throw new Exception("The number of correction points is incorrect.(Top)"); }

            StoreCp(ref lstUnitCpInfo, UfCamCorrectPos.Cabinet_Left, trimBlobs, vp);

            // Center
            winProgress.ShowMessage("Load Center Image.");
            winProgress.PutForward1Step();
            saveUfLog("Load Center Image.");
            CalcTrimmingAreaMask(logDir + UfCamCorrectPos._9pt_Center.ToString() + ".arw", blackFile, logDir + fn_MaskTrim + "_" + UfCamCorrectPos._9pt_Center.ToString() + ".jpg", out lstArea, out trimBlobs);
            if (lstArea.Count != cabiCnt)
            { throw new Exception("The number of correction points is incorrect.(Center)"); }

            StoreCp(ref lstUnitCpInfo, UfCamCorrectPos._9pt_Center, trimBlobs, vp);

            // Right
            winProgress.ShowMessage("Load Right Image.");
            winProgress.PutForward1Step();
            saveUfLog("Load Right Image.");
            CalcTrimmingAreaMask(logDir + UfCamCorrectPos.Cabinet_Right.ToString() + ".arw", blackFile, logDir + fn_MaskTrim + "_" + UfCamCorrectPos.Cabinet_Right.ToString() + ".jpg", out lstArea, out trimBlobs);
            if (lstArea.Count != cabiCnt)
            { throw new Exception("The number of correction points is incorrect.(Top)"); }

            StoreCp(ref lstUnitCpInfo, UfCamCorrectPos.Cabinet_Right, trimBlobs, vp);

            // Bottom-Left
            winProgress.ShowMessage("Load Bottom-Left Image.");
            winProgress.PutForward1Step();
            saveUfLog("Load Bottom-Left Image.");
            CalcTrimmingAreaMask(logDir + UfCamCorrectPos._9pt_BottomLeft.ToString() + ".arw", blackFile, logDir + fn_MaskTrim + "_" + UfCamCorrectPos._9pt_BottomLeft.ToString() + ".jpg", out lstArea, out trimBlobs);
            if (lstArea.Count != cabiCnt)
            { throw new Exception("The number of correction points is incorrect.(Bottom-Left)"); }

            StoreCp(ref lstUnitCpInfo, UfCamCorrectPos._9pt_BottomLeft, trimBlobs, vp);

            // Bottom
            winProgress.ShowMessage("Load Bottom Image.");
            winProgress.PutForward1Step();
            saveUfLog("Load Bottom Image.");
            CalcTrimmingAreaMask(logDir + UfCamCorrectPos.Cabinet_Bottom.ToString() + ".arw", blackFile, logDir + fn_MaskTrim + "_" + UfCamCorrectPos.Cabinet_Bottom.ToString() + ".jpg", out lstArea, out trimBlobs);
            if (lstArea.Count != cabiCnt)
            { throw new Exception("The number of correction points is incorrect.(Top)"); }

            StoreCp(ref lstUnitCpInfo, UfCamCorrectPos.Cabinet_Bottom, trimBlobs, vp);

            // Bottom-Right
            winProgress.ShowMessage("Load Bottom-Right Image.");
            winProgress.PutForward1Step();
            saveUfLog("Load Bottom-Right Image.");
            CalcTrimmingAreaMask(logDir + UfCamCorrectPos._9pt_BottomRight.ToString() + ".arw", blackFile, logDir + fn_MaskTrim + "_" + UfCamCorrectPos._9pt_BottomRight.ToString() + ".jpg", out lstArea, out trimBlobs);
            if (lstArea.Count != cabiCnt)
            { throw new Exception("The number of correction points is incorrect.(Bottom-Right)"); }

            StoreCp(ref lstUnitCpInfo, UfCamCorrectPos._9pt_BottomRight, trimBlobs, vp);

            // TrimAreaのマージ画像を生成
            MergeTrimMask(logDir);

#endregion 処理
        }

        private void GetCpRadiator(List<UnitInfo> lstTgtCabi, List<UnitInfo> lstObjCabi, ObjectiveLine objEdge, ViewPoint vp, string logDir, out List<UfCamCabinetCpInfo> lstUnitCpInfo, out List<UfCamCorrectionPoint> lstRefPoints)
        {
            List<UnitInfo> lstAdjCabi;

            lstUnitCpInfo = new List<UfCamCabinetCpInfo>();
            //lstRefPoints = new List<UfCamCorrectionPoint>();

#region 撮影

            // Black
            winProgress.ShowMessage("Capture Black Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Black Image.");
            stopIntSig(); // Black出力(信号Stop)
            CaptureImage(logDir + fn_BlackFile, Settings.Ins.Camera.TrimAreaSetting);

            // 基準点
            CaptureRefPoint(lstTgtCabi, lstObjCabi, objEdge, logDir, out lstAdjCabi, out UfCamCorrectionPoint refPoint);

            // マスク用画像を撮影
            winProgress.ShowMessage("Capture Mask Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Mask Image.");
            OutputTargetArea(lstAdjCabi);
            CaptureImage(logDir + System.IO.Path.GetFileNameWithoutExtension(fn_AreaFile));

            // 各Positionを撮影
            // Radiatorは左右の分を一度に撮る
            // Top
            winProgress.ShowMessage("Capture Top Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Top Image.");
            outputTrimArea(UfCamCorrectPos.Radiator_L_Top);
            CaptureImage(logDir + UfCamCorrectPos.Radiator_L_Top.ToString());

            // Right
            winProgress.ShowMessage("Capture Right Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Right Image.");
            outputTrimArea(UfCamCorrectPos.Radiator_L_Right);
            CaptureImage(logDir + UfCamCorrectPos.Radiator_L_Right.ToString());

            // Bottom
            winProgress.ShowMessage("Capture Bottom Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Bottom Image.");
            outputTrimArea(UfCamCorrectPos.Radiator_L_Bottom);
            CaptureImage(logDir + UfCamCorrectPos.Radiator_L_Bottom.ToString());

            // Left
            winProgress.ShowMessage("Capture Left Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Left Image.");
            outputTrimArea(UfCamCorrectPos.Radiator_L_Left);
            CaptureImage(logDir + UfCamCorrectPos.Radiator_L_Left.ToString());

            // 内部信号停止
            stopIntSig();

#endregion 撮影

#region 処理

            // 対象エリアのマスク画像を作成
            winProgress.ShowMessage("Generate Mask Image.");
            winProgress.PutForward1Step();
            saveUfLog("Generate Mask Image.");
            string file = logDir + fn_AreaFile;
            string blackFile = logDir + fn_BlackFile + ".arw";
            MakeMaskImageArw(file, blackFile);

            // 基準点
            ProcRefPoint(lstTgtCabi, lstAdjCabi, refPoint, out lstRefPoints, objEdge, logDir, blackFile, ref vp);
            
            // 各ポジション
            // 矩形にのみ対応
            UfCamCabinetCpInfo unit;

            foreach (UnitInfo tgt in lstAdjCabi)
            {
                unit = new UfCamCabinetCpInfo();
                unit.Unit = tgt;
                lstUnitCpInfo.Add(unit);
            }

            int cabiCnt = lstAdjCabi.Count; // 調整対象Cabinetの数

            // Top
            winProgress.ShowMessage("Load Top Image.");
            winProgress.PutForward1Step();
            saveUfLog("Load Top Image.");
            CalcTrimmingAreaMask(logDir + UfCamCorrectPos.Radiator_L_Top.ToString() + ".arw", blackFile, logDir + fn_MaskTrim + "_" + UfCamCorrectPos.Radiator_L_Top.ToString() + ".jpg", out List<Area> lstArea, out CvBlobs trimBlob);
            if (lstArea.Count != cabiCnt * 2)
            { throw new Exception("The number of correction points is incorrect.(Top)"); }

            storeCpRadiator(ref lstUnitCpInfo, UfCamCorrectPos.Radiator_L_Top, trimBlob, vp);

            // Right
            winProgress.ShowMessage("Load Right Image.");
            winProgress.PutForward1Step();
            saveUfLog("Load Right Image.");
            CalcTrimmingAreaMask(logDir + UfCamCorrectPos.Radiator_L_Right.ToString() + ".arw", blackFile, logDir + fn_MaskTrim + "_" + UfCamCorrectPos.Radiator_L_Right.ToString() + ".jpg", out lstArea, out trimBlob);
            if (lstArea.Count != cabiCnt * 2)
            { throw new Exception("The number of correction points is incorrect.(Right)"); }

            storeCpRadiator(ref lstUnitCpInfo, UfCamCorrectPos.Radiator_L_Right, trimBlob, vp);

            // Bottom
            winProgress.ShowMessage("Load Bottom Image.");
            winProgress.PutForward1Step();
            saveUfLog("Load Bottom Image.");
            CalcTrimmingAreaMask(logDir + UfCamCorrectPos.Radiator_L_Bottom.ToString() + ".arw", blackFile, logDir + fn_MaskTrim + "_" + UfCamCorrectPos.Radiator_L_Bottom.ToString() + ".jpg", out lstArea, out trimBlob);
            if (lstArea.Count != cabiCnt * 2)
            { throw new Exception("The number of correction points is incorrect.(Bottom)"); }

            storeCpRadiator(ref lstUnitCpInfo, UfCamCorrectPos.Radiator_L_Bottom, trimBlob, vp);

            // Left
            winProgress.ShowMessage("Load Left Image.");
            winProgress.PutForward1Step();
            saveUfLog("Load Left Image.");
            CalcTrimmingAreaMask(logDir + UfCamCorrectPos.Radiator_L_Left.ToString() + ".arw", blackFile, logDir + fn_MaskTrim + "_" + UfCamCorrectPos.Radiator_L_Left.ToString() + ".jpg", out lstArea, out trimBlob);
            if (lstArea.Count != cabiCnt * 2)
            { throw new Exception("The number of correction points is incorrect.(Left)"); }

            storeCpRadiator(ref lstUnitCpInfo, UfCamCorrectPos.Radiator_L_Left, trimBlob, vp);

            // TrimAreaのマージ画像を生成
            MergeTrimMask(logDir);

#endregion 処理
        }

        private void GetCpEachModule(List<UnitInfo> lstTgtCabi, List<UnitInfo> lstObjCabi, ObjectiveLine objEdge, ViewPoint vp, string logDir, out List<UfCamCabinetCpInfo> lstUnitCpInfo, out List<UfCamCorrectionPoint> lstRefPoints)
        {
            List<UnitInfo> lstAdjCabi = null;

            lstUnitCpInfo = new List<UfCamCabinetCpInfo>();
            //lstRefPoints = new List<UfCamCorrectionPoint>();

#region 撮影
#if NO_CAP
            //outputTrimAreaRefDemo(Quadrant.Quad_1, out refPoint.Unit); // RefUnitの格納も行っている
            UfCamCorrectionPoint refPoint = new UfCamCorrectionPoint();
            Quadrant quad = Quadrant.NA;
#else

            // Black
            winProgress.ShowMessage("Capture Black Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Black Image.");
            stopIntSig(); // Black出力(信号Stop)
            CaptureImage(logDir + fn_BlackFile, Settings.Ins.Camera.TrimAreaSetting);

            // 基準点
            CaptureRefPoint(lstTgtCabi, lstObjCabi, objEdge, logDir, out lstAdjCabi, out UfCamCorrectionPoint refPoint);

            // マスク用画像を撮影
            winProgress.ShowMessage("Capture Mask Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Mask Image.");
            OutputTargetArea(lstAdjCabi);
            CaptureImage(logDir + System.IO.Path.GetFileNameWithoutExtension(fn_AreaFile));

            // 各Positionを撮影
            for (int i = 0; i < moduleCount; i++)
            {
                int pos = 12;

                winProgress.ShowMessage("Capture Module-" + (i + 1).ToString() + " Image.");
                winProgress.PutForward1Step();
                saveUfLog($"Capture Module-{i + 1} Image.");
                outputTrimArea((UfCamCorrectPos)Enum.ToObject(typeof(UfCamCorrectPos), pos + i));
                CaptureImage(logDir + ((UfCamCorrectPos)Enum.ToObject(typeof(UfCamCorrectPos), pos + i)).ToString());                
            }
#endif

            // 内部信号停止
            stopIntSig();

#endregion 撮影

#region 処理

            // 対象エリアのマスク画像を作成
            winProgress.ShowMessage("Generate Mask Image.");
            winProgress.PutForward1Step();
            saveUfLog("Generate Mask Image.");
            string file = logDir + fn_AreaFile;
            string blackFile = logDir + fn_BlackFile + ".arw";
            MakeMaskImageArw(file, blackFile);

            // 基準点
            ProcRefPoint(lstTgtCabi, lstAdjCabi, refPoint, out lstRefPoints, objEdge, logDir, blackFile, ref vp);

            // 各ポジション
            // 矩形にのみ対応
            UfCamCabinetCpInfo unit;

            foreach (UnitInfo tgt in lstAdjCabi)
            {
                unit = new UfCamCabinetCpInfo();
                unit.Unit = tgt;
                lstUnitCpInfo.Add(unit);
            }

            int cabiCnt = lstAdjCabi.Count; // 調整対象Cabinetの数

            // 各Moduleの補正点を格納
            for (int i = 0; i < moduleCount; i++)
            {
                int pos = 12;

                winProgress.ShowMessage("Load Module-" + (i + 1).ToString() + " Image.");
                winProgress.PutForward1Step();
                saveUfLog($"Load Module-{i + 1} Image.");
                CalcTrimmingAreaMask(logDir + ((UfCamCorrectPos)Enum.ToObject(typeof(UfCamCorrectPos), pos + i)).ToString().ToString() + ".arw", blackFile, logDir + fn_MaskTrim + "_" + i + ".jpg", out List<Area> lstArea, out CvBlobs trimBlobs);
                if (lstArea.Count != cabiCnt)
                { throw new Exception("The number of correction points is incorrect.(Module)"); }

                StoreCp(ref lstUnitCpInfo, (UfCamCorrectPos)Enum.ToObject(typeof(UfCamCorrectPos), pos + i), trimBlobs, vp); // Cabinetと共通
            }

            // TrimAreaのマージ画像を生成
            MergeTrimMask(logDir);

#endregion 処理
        }
        
        /// <summary>
        /// 基準Cabinet選択でLineを選んでいる場合は基準になるCabinetを調整対象から除外する。
        /// </summary>
        /// <param name="lstTgtUnit">調整対象Cabinet</param>
        /// <param name="lstObjCabi">基準Cabinet</param>
        /// <param name="edge">Line選択時の基準辺</param>
        private void RemoveObjectiveCabinet(List<UnitInfo> lstTgtUnit, ref List<UnitInfo> lstObjCabi, ObjectiveLine edge, out List<UnitInfo> lstAdjCabi)
        {
            lstAdjCabi = new List<UnitInfo>();

            // 調整対象Cabinetから基準Cabinetを除外
            foreach (UnitInfo cabi in lstTgtUnit)
            {
                bool isObj = false;

                foreach (UnitInfo obj in lstObjCabi)
                {
                    if (cabi == obj)
                    {
                        isObj = true;
                        break; ;
                    }
                }

                if (isObj == false)
                { lstAdjCabi.Add(cabi); }
            }

            //// 基準Cabinetから交点になるCabinetを除外

            //// 座標の最大、最小値を探す
            //int minX = int.MaxValue, minY = int.MaxValue, maxX = 0, maxY = 0;
            //for (int i = 0; i < lstTgtUnit.Count; i++)
            //{
            //    if (lstTgtUnit[i].X < minX)
            //    { minX = lstTgtUnit[i].X; }

            //    if (lstTgtUnit[i].Y < minY)
            //    { minY = lstTgtUnit[i].Y; }

            //    if (lstTgtUnit[i].X > maxX)
            //    { maxX = lstTgtUnit[i].X; }

            //    if (lstTgtUnit[i].Y > maxY)
            //    { maxY = lstTgtUnit[i].Y; }
            //}

            //if (edge.Top == true && edge.Left == true)
            //{
            //    foreach (UnitInfo cabi in lstObjCabi)
            //    {
            //        if (cabi.X == minX && cabi.Y == minY)
            //        {
            //            lstObjCabi.Remove(cabi);
            //            break;
            //        }
            //    }
            //}
            //else if (edge.Top == true && edge.Right == true)
            //{
            //    foreach (UnitInfo cabi in lstObjCabi)
            //    {
            //        if (cabi.X == maxX && cabi.Y == minY)
            //        {
            //            lstObjCabi.Remove(cabi);
            //            break;
            //        }
            //    }
            //}
            //else if (edge.Bottom == true && edge.Left == true)
            //{
            //    foreach (UnitInfo cabi in lstObjCabi)
            //    {
            //        if (cabi.X == minX && cabi.Y == maxY)
            //        {
            //            lstObjCabi.Remove(cabi);
            //            break;
            //        }
            //    }
            //}
            //else if (edge.Bottom == true && edge.Right == true)
            //{
            //    foreach (UnitInfo cabi in lstObjCabi)
            //    {
            //        if (cabi.X == maxX && cabi.Y == maxY)
            //        {
            //            lstObjCabi.Remove(cabi);
            //            break;
            //        }
            //    }
            //}
        }

        private void CaptureRefPoint(List<UnitInfo> lstTgtCabi, List<UnitInfo> lstObjCabi, ObjectiveLine objEdge, string logDir, out List<UnitInfo> lstAdjCabi, out UfCamCorrectionPoint refPoint)
        {
            // Base Cabinet = Default/Cabinet用
            refPoint = new UfCamCorrectionPoint();
            refPoint.Unit = lstObjCabi[0];
            refPoint.Pos = UfCamCorrectPos.Reference;

            // 基準点を撮影
            if (objEdge == null) // Base Cabinet = Default/Cabinet
            {
                winProgress.ShowMessage("Capture Reference Position.");
                winProgress.PutForward1Step();
                saveUfLog("Capture Reference Position.");
                outputTrimAreaRef(lstObjCabi[0], out Quadrant quad);
                CaptureImage(logDir + fn_Reference);

                refPoint.Quad = quad;

                lstAdjCabi = lstTgtCabi;
            }
            else // Line
            {
                // 基準点用のマスク画像を撮影
                
                if (objEdge.Top == true)
                {
                    winProgress.ShowMessage("Capture Reference Position Mask.(Top)");
                    winProgress.PutForward1Step();
                    saveUfLog("Capture Reference Position Mask.(Top)");
                    OutputMaskLineRef(lstTgtCabi, ObjectiveEdge.Top);
                    CaptureImage(logDir + fn_Reference + fn_RefMaskTop);
                }

                if (objEdge.Bottom == true)
                {
                    winProgress.ShowMessage("Capture Reference Position Mask.(Bottom)");
                    winProgress.PutForward1Step();
                    saveUfLog("Capture Reference Position Mask.(Bottom)");
                    OutputMaskLineRef(lstTgtCabi, ObjectiveEdge.Bottom);
                    CaptureImage(logDir + fn_Reference + fn_RefMaskBottom);
                }

                if (objEdge.Left == true)
                {
                    winProgress.ShowMessage("Capture Reference Position Mask.(Left)");
                    winProgress.PutForward1Step();
                    saveUfLog("Capture Reference Position Mask.(Left)");
                    OutputMaskLineRef(lstTgtCabi, ObjectiveEdge.Left);
                    CaptureImage(logDir + fn_Reference + fn_RefMaskLeft);
                }

                if (objEdge.Right == true)
                {
                    winProgress.ShowMessage("Capture Reference Position Mask.(Right)");
                    winProgress.PutForward1Step();
                    saveUfLog("Capture Reference Position Mask.(Right)");
                    OutputMaskLineRef(lstTgtCabi, ObjectiveEdge.Right);
                    CaptureImage(logDir + fn_Reference + fn_RefMaskRight);
                }

                // 基準点(全Module中心)を撮影            
                winProgress.ShowMessage("Capture Reference Position.");
                winProgress.PutForward1Step();
                saveUfLog("Capture Reference Position.");
                OutputTrimAreaRefAll();
                CaptureImage(logDir + fn_Reference);

                // 全体マスク用画像を撮影
                winProgress.ShowMessage("Capture Mask Image.");
                winProgress.PutForward1Step();
                saveUfLog("Capture Mask Image.");
                OutputTargetArea(lstTgtCabi);
                CaptureImage(logDir + System.IO.Path.GetFileNameWithoutExtension(fn_RefArea));

                // 基準Cabinet選択でLineを選んでいる場合は基準になるCabinetを調整対象から除外                
                RemoveObjectiveCabinet(lstTgtCabi, ref lstObjCabi, objEdge, out lstAdjCabi);
            }
        }

        private void ProcRefPoint(List<UnitInfo> lstTgtCabi, List<UnitInfo> lstAdjCabi, UfCamCorrectionPoint refPoint, out List<UfCamCorrectionPoint> lstRefPoints, ObjectiveLine objEdge, string logDir,string blackFile, ref ViewPoint vp)
        {
            lstRefPoints = new List<UfCamCorrectionPoint>();
            vp.RefTilt = double.NaN; // 視聴点調整モード時の基準Tilt角

            if (objEdge == null) // Base Cabinet = Default/Cabinet
            {
                winProgress.ShowMessage("Load Reference Image.");
                winProgress.PutForward1Step();
                saveUfLog("Load Reference Image.");
                CalcTrimmingAreaMask(logDir + fn_Reference + ".arw", blackFile, logDir + fn_MaskRef, out List<Area> lstArea, out CvBlobs trimBlob);
                if (lstArea.Count != 1)
                { throw new Exception("The reference point was not found."); }

                refPoint.CameraArea = lstArea[0];

                // カメラ補正値
                SearchCameraCorrectValue(refPoint, out CameraCorrectionValue ccv, out LedCorrectionValue lcv, new ViewPoint());
                refPoint.CCV = ccv;
                refPoint.LCV = lcv;

                // added by Hotta 2024/09/10 for corsstalk
#if ForCrosstalkCameraUF
                refPoint.ModuleNo = ReferenceOneModuleNo;
#endif

                lstRefPoints.Add(refPoint);

                // 視聴点調整モード時は基準点のTilt角を使用する
                if (vp.Vertical == true)
                { vp.RefTilt = refPoint.LCV.TiltAngle; }

                if (vp.Horizontal == true)
                { vp.RefPan = refPoint.LCV.PanAngle; }
            }
            else // Base Cabinet = Line
            {
                // 視聴点調整モード時は選択範囲の中心のTilt角を使用する
                if (vp.Vertical == true)
                {
                    // Unitの位置を調査
                    int StartUnitX = int.MaxValue, StartUnitY = int.MaxValue; // 対象Unitの左上のユニット位置（1ベース）
                    int EndUnitX = 0, EndUnitY = 0;

                    foreach (UnitInfo cabi in lstAdjCabi)
                    {
                        if (cabi.X < StartUnitX)
                        { StartUnitX = cabi.X; }

                        if (cabi.Y < StartUnitY)
                        { StartUnitY = cabi.Y; }

                        if (cabi.X > EndUnitX)
                        { EndUnitX = cabi.X; }

                        if (cabi.Y > EndUnitY)
                        { EndUnitY = cabi.Y; }
                    }

                    int LenX = EndUnitX - StartUnitX + 1; // 対象UnitのX方向Unit数
                    int LenY = EndUnitY - StartUnitY + 1; // 対象UnitのY方向Unit数

                    int centerX = StartUnitX + LenX / 2;
                    int centerY = StartUnitY + LenY / 2;

                    vp.RefTilt = GetTiltAngle(lstTgtCabi, centerX, centerY);                 
                }

                if (vp.Horizontal == true)
                { vp.RefPan = 0; }

                if (objEdge.Top == true)
                {
                    winProgress.ShowMessage("Load Reference Image(Top).");
                    winProgress.PutForward1Step();
                    saveUfLog("Load Reference Image(Top).");

                    // Refのマスク画像を作成
                    string maskFile = logDir + fn_Reference + fn_RefMaskTop;
                    string newMaskFile = logDir + fn_Reference + fn_RefMaskTop + ".jpg";
                    MakeMaskImageArw(maskFile + ".arw", blackFile, newMaskFile);

                    CalcTrimmingAreaMask(logDir + fn_Reference + ".arw", blackFile, logDir + fn_Reference + "_MaskedTop.jpg", out List<Area> lstArea, out CvBlobs trimBlobs, maskFile + ".jpg");

                    StoreRefPt(lstTgtCabi, ref lstRefPoints, lstArea, ObjectiveEdge.Top, vp);
                }

                if (objEdge.Bottom == true)
                {
                    winProgress.ShowMessage("Load Reference Image(Bottom).");
                    winProgress.PutForward1Step();
                    saveUfLog("Load Reference Image(Bottom).");

                    // Refのマスク画像を作成
                    string maskFile = logDir + fn_Reference + fn_RefMaskBottom;
                    string newMaskFile = logDir + fn_Reference + fn_RefMaskBottom + ".jpg";
                    MakeMaskImageArw(maskFile + ".arw", blackFile, newMaskFile);

                    CalcTrimmingAreaMask(logDir + fn_Reference + ".arw", blackFile, logDir + fn_Reference + "_MaskedBottom.jpg", out List<Area> lstArea, out CvBlobs trimBlobs, maskFile + ".jpg");

                    StoreRefPt(lstTgtCabi, ref lstRefPoints, lstArea, ObjectiveEdge.Bottom, vp);
                }

                if (objEdge.Left == true)
                {
                    winProgress.ShowMessage("Load Reference Image(Left).");
                    winProgress.PutForward1Step();
                    saveUfLog("Load Reference Image(Left).");

                    // Refのマスク画像を作成
                    string maskFile = logDir + fn_Reference + fn_RefMaskLeft;
                    string newMaskFile = logDir + fn_Reference + fn_RefMaskLeft + ".jpg";
                    MakeMaskImageArw(maskFile + ".arw", blackFile, newMaskFile);

                    CalcTrimmingAreaMask(logDir + fn_Reference + ".arw", blackFile, logDir + fn_Reference + "_MaskedLeft.jpg", out List<Area> lstArea, out CvBlobs trimBlobs, maskFile + ".jpg");

                    StoreRefPt(lstTgtCabi, ref lstRefPoints, lstArea, ObjectiveEdge.Left, vp);
                }

                if (objEdge.Right == true)
                {
                    winProgress.ShowMessage("Load Reference Image(Right).");
                    winProgress.PutForward1Step();
                    saveUfLog("Load Reference Image(Right).");

                    // Refのマスク画像を作成
                    string maskFile = logDir + fn_Reference + fn_RefMaskRight;
                    string newMaskFile = logDir + fn_Reference + fn_RefMaskRight + ".jpg";
                    MakeMaskImageArw(maskFile + ".arw", blackFile, newMaskFile);

                    CalcTrimmingAreaMask(logDir + fn_Reference + ".arw", blackFile, logDir + fn_Reference + "_MaskedRight.jpg", out List<Area> lstArea, out CvBlobs trimBlobs, maskFile + ".jpg");

                    StoreRefPt(lstTgtCabi, ref lstRefPoints, lstArea, ObjectiveEdge.Right, vp);
                }

                // 画素値抽出用のマスク画像を用意しておく
                {
                    string maskFile = logDir + fn_RefArea;
                    string newMaskFile = logDir + System.IO.Path.GetFileNameWithoutExtension(fn_RefArea) + ".jpg";
                    MakeMaskImageArw(maskFile, blackFile, newMaskFile);

                    CalcTrimmingAreaMask(logDir + fn_Reference + ".arw", blackFile, logDir + fn_MaskRef, out List<Area> lstArea, out CvBlobs trimBlobs, logDir + System.IO.Path.GetFileNameWithoutExtension(fn_RefArea) + ".jpg");
                }
            }
        }

        /// <summary>
        /// マスク画像(JPEG)を生成する。指定しない場合ファイル名は固定。
        /// </summary>
        /// <param name="file">マスクの元画像(ARW)</param>
        /// <param name="blackFile">Blask画像(ARW)</param>
        /// <param name="newMaskFile">新しいマスク画像ファイル名(JPEG)（フルパス）</param>
        /// <param name="dilateSize">Dilateサイズ</param>
        unsafe private void MakeMaskImageArw(string file, string blackFile, string newMaskFile = "", int dilateSize = 1)
        {
            AcqARW arw, arwBlack;
            loadArwFile(file, out arw, true);
            loadArwFile(blackFile, out arwBlack);
            int width = arw.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
            int height = arw.RawMainIFD.ImageHeight / 2;

            using (Mat src = new Mat(new Size(width, height), MatType.CV_16UC1))
            using (Mat gray = new Mat())
            {
                ushort* pMat = (ushort*)(src.Data);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int pv = arw.RawData[1][x, y] - arwBlack.RawData[1][x, y]; // Green
                        if (pv < 0) { pv = 0; }
                        pMat[y * width + x] = (ushort)pv;
                    }
                }

                src.ConvertTo(gray, MatType.CV_8UC1, 1.0 / 16);

                using (Mat binary = gray.Threshold(0, 0xff, ThresholdTypes.Otsu))
                using (Mat dilate1 = new Mat())
                using (Mat mask = new Mat(new OpenCvSharp.Size(binary.Width, binary.Height), MatType.CV_8UC1))
                using (Mat masked = new Mat())
                using (Mat dilate2 = new Mat())
                {
                    Cv2.Dilate(binary, dilate1, null);
                    CvBlobs blobs = new CvBlobs(dilate1);
                    blobs.FilterByArea(TrimmingAreaMin, binary.Height * binary.Width);
                    CvBlob measArea = new CvBlob();

                    // 最大のBlobを検索
                    foreach (KeyValuePair<int, CvBlob> pair in blobs)
                    {
                        if (pair.Value.Area > measArea.Area)
                        { measArea = pair.Value; }
                    }

                    // Mask画像作成
                    // # 8近傍の定義
                    using (var neiborhood8 = new Mat(new OpenCvSharp.Size(3, 3), MatType.CV_8U))
                    {
                        for (int y = 0; y < 3; y++)
                        {
                            for (int x = 0; x < 3; x++)
                            { neiborhood8.Set<byte>(y, x, 1); }
                        }

                        Scalar color = Scalar.All(255);
                        Cv2.Rectangle(mask, measArea.Rect, color, -1); // 塗りつぶし

                        binary.CopyTo(masked, mask.Threshold(0, 0xff, ThresholdTypes.Otsu));
                        Cv2.Dilate(masked, dilate2, neiborhood8, null, dilateSize);
                    }

                    if (Settings.Ins.Camera.SaveIntImage)
                    {
                        Cv2.ImWrite(applicationPath + @"\Temp\gray.jpg", gray, (int[])null);
                        Cv2.ImWrite(applicationPath + @"\Temp\binary.jpg", binary, (int[])null);
                        Cv2.ImWrite(applicationPath + @"\Temp\mask.jpg", mask);
                        Cv2.ImWrite(applicationPath + @"\Temp\masked.jpg", masked);
                        Cv2.ImWrite(applicationPath + @"\Temp\dilate1.jpg", dilate1);
                        Cv2.ImWrite(applicationPath + @"\Temp\dilate2.jpg", dilate2);
                    }

                    string maskFile;

                    if(string.IsNullOrWhiteSpace(newMaskFile) == true)
                    { maskFile = System.IO.Path.GetDirectoryName(file) + "\\" + fn_MaskFile; }
                    else
                    { maskFile = newMaskFile; }

                    Cv2.ImWrite(maskFile, dilate2);
                }
            }
        }

        private void MergeTrimMask(string logDir)
        {
            string[] files = Directory.GetFiles(logDir, fn_MaskTrim + "_*.jpg");

            if (files.Length <= 0)
            { throw new Exception("The mask file for the trimming area is not output."); }

            using (Mat trimFirst = new Mat(files[0], ImreadModes.Grayscale))
            {
                Mat dest = trimFirst.Clone();

                try
                {
                    for (int i = 1; i < files.Length; i++)
                    {
                        using (Mat trim = new Mat(files[i], ImreadModes.Grayscale))
                        { dest += trim; }
                    }

                    // TrimMask画像を保存
                    Cv2.ImWrite(logDir + fn_MaskTrim + ".jpg", dest);
                }
                catch { }
                finally { dest.Dispose(); }                
            }
        }

        /// <summary>
        /// TrimmingAreaを捜索する。Blackによる減算処理、マスク画像による範囲指定を含む。
        /// </summary>
        /// <param name="file"></param>
        /// <param name="blackFile">Blackファイル（フルパス）</param>
        /// <param name="trimMaskedFile">マスクされた画像ファイル</param>
        /// <param name="lstArea">TrimmingAreaのリスト（現在未使用）</param>
        /// <param name="trimBlobs">TrimmingAreaのクラス</param>
        /// <param name="trimMaskFile">マスク画像ファイル。無指定の場合はデフォルトのマスクファイルが使用される。</param>
        unsafe private void CalcTrimmingAreaMask(string file, string blackFile, string trimMaskedFile, out List<Area> lstArea, out CvBlobs trimBlobs, string trimMaskFile = "")
        {
            lstArea = new List<Area>();

            string maskFile;

            if(string.IsNullOrWhiteSpace(trimMaskFile) == true)
            { maskFile = System.IO.Path.GetDirectoryName(file) + "\\" + fn_MaskFile; }
            else
            { maskFile = trimMaskFile; }

            AcqARW arw, arwBlack;
            loadArwFile(file, out arw);
            loadArwFile(blackFile, out arwBlack);
            int width = arw.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
            int height = arw.RawMainIFD.ImageHeight / 2;

            using (Mat src = new Mat(new Size(width, height), MatType.CV_16UC1))
            using (Mat gray = new Mat())
            {
                ushort* pMat = (ushort*)(src.Data);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int pv = arw.RawData[1][x, y] - arwBlack.RawData[1][x, y];
                        if(pv < 0) { pv = 0; }
                        pMat[y * width + x] = (ushort)pv;
                    }
                }

                src.ConvertTo(gray, MatType.CV_8UC1, 1.0 / 16);

                using (Mat mask = new Mat(maskFile, ImreadModes.Grayscale))
                using (Mat masked = new Mat())
                using (Mat close = new Mat())
                {
                    gray.CopyTo(masked, mask.Threshold(0, 0xff, ThresholdTypes.Otsu));

                    using (Mat binary = masked.Threshold(0, 0xff, ThresholdTypes.Otsu))
                    {
                        Cv2.MorphologyEx(binary, close, MorphTypes.Close, null);

                        if (Settings.Ins.Camera.SaveIntImage == true)
                        {
                            Cv2.ImWrite(tempPath + @"\gray.jpg", gray, (int[])null);
                            Cv2.ImWrite(tempPath + @"\binary.jpg", binary, (int[])null);
                            Cv2.ImWrite(tempPath + @"\mask.jpg", mask);
                            Cv2.ImWrite(tempPath + @"\masked.jpg", masked);
                            Cv2.ImWrite(tempPath + @"\close.jpg", close);
                        }
                    }

                    // TrimMask画像を保存
                    Cv2.ImWrite(trimMaskedFile, close);

                    CvBlobs blobs = new CvBlobs(close);

                    double blobArea = calcCellArea(blobs);

                    blobs.FilterByArea((int)(blobArea * 0.4), (int)(blobArea * 1.6));

                    trimBlobs = blobs;

                    foreach (KeyValuePair<int, CvBlob> pair in blobs)
                    {
                        double ratio = (double)pair.Value.Rect.Height / (double)pair.Value.Rect.Width;
                        double len = Math.Sqrt(blobArea);

                        if (ratio > 0.4 && ratio < 1.6
                            && pair.Value.Rect.Height > len * 0.4 && pair.Value.Rect.Height < len * 1.6
                            && pair.Value.Rect.Width > len * 0.4 && pair.Value.Rect.Width < len * 1.6)
                        {
                            Area area = new Area(pair.Value.Rect.Left, pair.Value.Rect.Top, pair.Value.Rect.Height, pair.Value.Rect.Width);
                            lstArea.Add(area);
                        }
                    }
                }
            }
        }

        private void StoreRefPt(List<UnitInfo> lstTgtCabi, ref List<UfCamCorrectionPoint> lstRefPoints, List<Area> lstArea, ObjectiveEdge edge, ViewPoint vp)
        {
            // ※矩形のディスプレイにしか対応してない

            int minX = int.MaxValue, minY = int.MaxValue, maxX = 0, maxY = 0;

            foreach (UnitInfo cabi in lstTgtCabi)
            {
                if (cabi.X < minX)
                { minX = cabi.X; }

                if (cabi.Y < minY)
                { minY = cabi.Y; }

                if (cabi.X > maxX)
                { maxX = cabi.X; }

                if (cabi.Y > maxY)
                { maxY = cabi.Y; }
            }

            // 調整対象の縦横のCabinet数
            int lenX = maxX - minX + 1;
            int lenY = maxY - minY + 1;

            int ModuleCountY; // Cabinett内でModuleがY方向にいくつあるか
            if (allocInfo.LEDModel == ZRD_CH12D 
                || allocInfo.LEDModel == ZRD_CH15D 
                || allocInfo.LEDModel == ZRD_BH12D 
                || allocInfo.LEDModel == ZRD_BH15D
                || allocInfo.LEDModel == ZRD_CH12D_S3
                || allocInfo.LEDModel == ZRD_CH15D_S3
                || allocInfo.LEDModel == ZRD_BH12D_S3
                || allocInfo.LEDModel == ZRD_BH15D_S3) // Cancun
            {
#if (DEBUG || Release_log)
                if (Settings.Ins.ExecLog == true)
                {
                    MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                }
#endif
                ModuleCountY = 3;
            }
            else // Chiron
            {
#if (DEBUG || Release_log)
                if (Settings.Ins.ExecLog == true)
                {
                    MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                }
#endif
                ModuleCountY = 2;
            }

            if (edge == ObjectiveEdge.Top)
            {
                // X座標順に並び替え
                var cx = new Comparison<Area>(CompareX);
                lstArea.Sort(cx);

                // 対象のCabinetを格納
                List<UnitInfo> lstObjCabi = new List<UnitInfo>();
                foreach (UnitInfo cabi in lstTgtCabi)
                {
                    if (cabi.Y == minY)
                    { lstObjCabi.Add(cabi); }
                }

                int col = 0;
                foreach (UnitInfo cabi in lstObjCabi) // Cabinetは左上から順番に格納されているためforechで順番通りに処理される
                {
                    int pos = 12 + ModuleCountX * (ModuleCountY - 1); // Moduleの先頭

                    // Cabinet1台分抜き出し
                    List<Area> lstAreaCabi = lstArea.GetRange(col * ModuleCountX, ModuleCountX);

                    int cur = 0;
                    foreach(Area area in lstAreaCabi)
                    {
                        UfCamCorrectionPoint tempRefCp = new UfCamCorrectionPoint();
                        tempRefCp.Unit = cabi;
                        tempRefCp.Pos = (UfCamCorrectPos)Enum.ToObject(typeof(UfCamCorrectPos), pos + cur);
                        tempRefCp.CameraArea = area;

                        // カメラ補正値
                        SearchCameraCorrectValue(tempRefCp, out CameraCorrectionValue ccv, out LedCorrectionValue lcv, vp);
                        tempRefCp.CCV = ccv;
                        tempRefCp.LCV = lcv;

                        // adde by Hotta 2024/09/10 for crosstalk
#if ForCrosstalkCameraUF
                        tempRefCp.ModuleNo = tempRefCp.Pos - UfCamCorrectPos.Module_0;
#endif

                        lstRefPoints.Add(tempRefCp);

                        cur++;
                    }

                    col++;
                }
            }

            if (edge == ObjectiveEdge.Bottom)
            {
                // X座標順に並び替え
                var cx = new Comparison<Area>(CompareX);
                lstArea.Sort(cx);

                // 対象のCabinetを格納
                List<UnitInfo> lstObjCabi = new List<UnitInfo>();
                foreach (UnitInfo cabi in lstTgtCabi)
                {
                    if (cabi.Y == maxY)
                    { lstObjCabi.Add(cabi); }
                }

                int col = 0;
                foreach (UnitInfo cabi in lstObjCabi) // Cabinetは左上から順番に格納されているためforechで順番通りに処理される
                {
                    int pos = 12; // Moduleの先頭 12 = Module_0

                    // Cabinet1台分抜き出し
                    List<Area> lstAreaCabi = lstArea.GetRange(col * ModuleCountX, ModuleCountX);

                    int cur = 0;
                    foreach (Area area in lstAreaCabi)
                    {
                        UfCamCorrectionPoint tempRefCp = new UfCamCorrectionPoint();
                        tempRefCp.Unit = cabi;
                        tempRefCp.Pos = (UfCamCorrectPos)Enum.ToObject(typeof(UfCamCorrectPos), pos + cur);
                        tempRefCp.CameraArea = area;

                        // カメラ補正値
                        SearchCameraCorrectValue(tempRefCp, out CameraCorrectionValue ccv, out LedCorrectionValue lcv, vp);
                        tempRefCp.CCV = ccv;
                        tempRefCp.LCV = lcv;

                        // adde by Hotta 2024/09/10 for crosstalk
#if ForCrosstalkCameraUF
                        tempRefCp.ModuleNo = tempRefCp.Pos - UfCamCorrectPos.Module_0;
#endif

                        lstRefPoints.Add(tempRefCp);

                        cur++;
                    }

                    col++;
                }
            }

            if (edge == ObjectiveEdge.Left)
            {
                // Y座標順に並び替え
                var cy = new Comparison<Area>(CompareY);
                lstArea.Sort(cy);

                // 対象のCabinetを格納
                List<UnitInfo> lstObjCabi = new List<UnitInfo>();
                foreach (UnitInfo cabi in lstTgtCabi)
                {
                    if (cabi.X == minX)
                    { lstObjCabi.Add(cabi); }
                }

                int row = 0;
                foreach (UnitInfo cabi in lstObjCabi) // Cabinetは左上から順番に格納されているためforechで順番通りに処理される
                {
                    int pos = 15; // Moduleの先頭 15 = Module_3

                    // Cabinet1台分抜き出し
                    List<Area> lstAreaCabi = lstArea.GetRange(row * ModuleCountY, ModuleCountY);

                    int cur = 0;
                    foreach (Area area in lstAreaCabi)
                    {
                        UfCamCorrectionPoint tempRefCp = new UfCamCorrectionPoint();
                        tempRefCp.Unit = cabi;
                        tempRefCp.Pos = (UfCamCorrectPos)Enum.ToObject(typeof(UfCamCorrectPos), pos + cur);
                        tempRefCp.CameraArea = area;

                        // カメラ補正値
                        SearchCameraCorrectValue(tempRefCp, out CameraCorrectionValue ccv, out LedCorrectionValue lcv, vp);
                        tempRefCp.CCV = ccv;
                        tempRefCp.LCV = lcv;

                        // adde by Hotta 2024/09/10 for crosstalk
#if ForCrosstalkCameraUF
                        tempRefCp.ModuleNo = tempRefCp.Pos - UfCamCorrectPos.Module_0;
#endif

                        lstRefPoints.Add(tempRefCp);

                        cur += ModuleCountX;
                    }

                    row++;
                }
            }

            if (edge == ObjectiveEdge.Right)
            {
                // Y座標順に並び替え
                var cy = new Comparison<Area>(CompareY);
                lstArea.Sort(cy);

                // 対象のCabinetを格納
                List<UnitInfo> lstObjCabi = new List<UnitInfo>();
                foreach (UnitInfo cabi in lstTgtCabi)
                {
                    if (cabi.X == maxX)
                    { lstObjCabi.Add(cabi); }
                }

                int row = 0;
                foreach (UnitInfo cabi in lstObjCabi) // Cabinetは左上から順番に格納されているためforechで順番通りに処理される
                {
                    int pos = 12; // Moduleの先頭 12 = Module_0

                    // Cabinet1台分抜き出し
                    List<Area> lstAreaCabi = lstArea.GetRange(row * ModuleCountY, ModuleCountY);

                    int cur = 0;
                    foreach (Area area in lstAreaCabi)
                    {
                        UfCamCorrectionPoint tempRefCp = new UfCamCorrectionPoint();
                        tempRefCp.Unit = cabi;
                        tempRefCp.Pos = (UfCamCorrectPos)Enum.ToObject(typeof(UfCamCorrectPos), pos + cur);
                        tempRefCp.CameraArea = area;

                        // カメラ補正値
                        SearchCameraCorrectValue(tempRefCp, out CameraCorrectionValue ccv, out LedCorrectionValue lcv, vp);
                        tempRefCp.CCV = ccv;
                        tempRefCp.LCV = lcv;

                        // adde by Hotta 2024/09/10 for crosstalk
#if ForCrosstalkCameraUF
                        tempRefCp.ModuleNo = tempRefCp.Pos - UfCamCorrectPos.Module_0;
#endif

                        lstRefPoints.Add(tempRefCp);

                        cur += ModuleCountX;
                    }

                    row++;
                }
            }
        }

        private void StoreCp(ref List<UfCamCabinetCpInfo> lstUnitCpInfo, UfCamCorrectPos pos, CvBlobs trimBlobs, ViewPoint vp)
        {
            // ※矩形のディスプレイにしか対応してない
            int lenX, lenY; // 調整対象の縦横のCabinet数
            int minX = int.MaxValue, minY = int.MaxValue, maxX = 0, maxY = 0;

            foreach (UfCamCabinetCpInfo info in lstUnitCpInfo)
            {
                if (info.Unit.X < minX)
                { minX = info.Unit.X; }

                if (info.Unit.Y < minY)
                { minY = info.Unit.Y; }

                if (info.Unit.X > maxX)
                { maxX = info.Unit.X; }

                if (info.Unit.Y > maxY)
                { maxY = info.Unit.Y; }
            }

            lenX = maxX - minX + 1;
            lenY = maxY - minY + 1;

            getTilePosition(trimBlobs, lenX, lenY, out CvBlob[,] aryBlobs);

            for (int y = 0; y < lenY; y++)
            {
                for (int x = 0; x < lenX; x++)
                {
                    UfCamCorrectionPoint cp = new UfCamCorrectionPoint();
                    cp.Pos = pos;
                    //cp.CameraArea = lstAreaLine[x];
                    cp.CameraArea = new Area(aryBlobs[x, y].Rect.X, aryBlobs[x, y].Rect.Y, aryBlobs[x, y].Rect.Height, aryBlobs[x, y].Rect.Width);
                    cp.Unit = allocInfo.lstUnits[minX - 1 + x][minY - 1 + y]; // X, Yは1ベースなので+1している

                    // カメラ補正値
                    SearchCameraCorrectValue(cp, out CameraCorrectionValue ccv, out LedCorrectionValue lcv, vp);
                    cp.CCV = ccv;
                    cp.LCV = lcv;

                    foreach (UfCamCabinetCpInfo info in lstUnitCpInfo)
                    {
                        if (info.Unit.ControllerID == cp.Unit.ControllerID
                            && info.Unit.PortNo == cp.Unit.PortNo
                            && info.Unit.UnitNo == cp.Unit.UnitNo)
                        {
                            info.lstCp.Add(cp);
                            break;
                        }
                    }
                }
            }
        }
        
        private void storeCpRadiator(ref List<UfCamCabinetCpInfo> lstUnitCpInfo, UfCamCorrectPos pos, CvBlobs trimBlobs, ViewPoint vp)
        {
            // ※矩形のディスプレイにしか対応してない
            int lenX, lenY; // 調整対象の縦横のCabinet数
            int minX = int.MaxValue, minY = int.MaxValue, maxX = 0, maxY = 0;

            foreach (UfCamCabinetCpInfo info in lstUnitCpInfo)
            {
                if (info.Unit.X < minX)
                { minX = info.Unit.X; }

                if (info.Unit.Y < minY)
                { minY = info.Unit.Y; }

                if (info.Unit.X > maxX)
                { maxX = info.Unit.X; }

                if (info.Unit.Y > maxY)
                { maxY = info.Unit.Y; }
            }

            lenX = maxX - minX + 1;
            lenY = maxY - minY + 1;

            getTilePosition(trimBlobs, lenX * 2, lenY, out CvBlob[,] aryBlobs);

            // 行ごとに処理
            for (int y = 0; y < lenY; y++)
            {
                for (int x = 0; x < lenX; x++)
                {
                    // Left
                    UfCamCorrectionPoint cp = new UfCamCorrectionPoint();
                    cp.Pos = pos;
                    cp.CameraArea = new Area(aryBlobs[x * 2, y].Rect.X, aryBlobs[x * 2, y].Rect.Y, aryBlobs[x * 2, y].Rect.Height, aryBlobs[x * 2, y].Rect.Width); //trimBlobs[x * 2, y];
                    cp.Unit = allocInfo.lstUnits[minX - 1 + x][minY - 1 + y];

                    // カメラ補正値
                    SearchCameraCorrectValue(cp, out CameraCorrectionValue ccvL, out LedCorrectionValue lcvL, vp);
                    cp.CCV = ccvL;
                    cp.LCV = lcvL;

                    foreach (UfCamCabinetCpInfo info in lstUnitCpInfo)
                    {
                        if (info.Unit.ControllerID == cp.Unit.ControllerID
                            && info.Unit.PortNo == cp.Unit.PortNo
                            && info.Unit.UnitNo == cp.Unit.UnitNo)
                        {
                            info.lstCp.Add(cp);
                            break;
                        }
                    }

                    // Right
                    cp = new UfCamCorrectionPoint();

                    if (pos == UfCamCorrectPos.Radiator_L_Top)
                    { cp.Pos = UfCamCorrectPos.Radiator_R_Top; }
                    else if (pos == UfCamCorrectPos.Radiator_L_Bottom)
                    { cp.Pos = UfCamCorrectPos.Radiator_R_Bottom; }
                    else if (pos == UfCamCorrectPos.Radiator_L_Left)
                    { cp.Pos = UfCamCorrectPos.Radiator_R_Left; }
                    else if (pos == UfCamCorrectPos.Radiator_L_Right)
                    { cp.Pos = UfCamCorrectPos.Radiator_R_Right; }
                    else
                    { throw new Exception("The position is specified incorrectly."); }

                    cp.CameraArea = new Area(aryBlobs[x * 2 + 1, y].Rect.X, aryBlobs[x * 2 + 1, y].Rect.Y, aryBlobs[x * 2 + 1, y].Rect.Height, aryBlobs[x * 2 + 1, y].Rect.Width); //trimBlobs[x * 2 + 1, y];
                    cp.Unit = allocInfo.lstUnits[minX - 1 + x][minY - 1 + y];

                    // カメラ補正値
                    SearchCameraCorrectValue(cp, out CameraCorrectionValue ccvR, out LedCorrectionValue lcvR, vp);
                    cp.CCV = ccvR;
                    cp.LCV = lcvR;

                    foreach (UfCamCabinetCpInfo info in lstUnitCpInfo)
                    {
                        if (info.Unit.ControllerID == cp.Unit.ControllerID
                            && info.Unit.PortNo == cp.Unit.PortNo
                            && info.Unit.UnitNo == cp.Unit.UnitNo)
                        {
                            info.lstCp.Add(cp);
                            break;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// ラスター画像を撮影し、各補正点の補正ゲインを計算する。
        /// </summary>
        /// <param name="lstRefPoints"></param>
        /// <param name="lstUnitCpInfo"></param>
        /// <param name="logDir"></param>
        private void GetFlatImages(ref List<UfCamCorrectionPoint> lstRefPoints, ref List<UfCamCabinetCpInfo> lstUnitCpInfo, string logDir)
        {
#region 撮影

#if NO_CAP
#else
            // Black(Red)
            winProgress.ShowMessage("Capture Black Image(Red).");
            winProgress.PutForward1Step();
            saveUfLog("Capture Black Image(Red).");
            stopIntSig(); // Black出力(信号Stop)
            CaptureImage(logDir + fn_BlackFile + "_Red", Settings.Ins.Camera.MeasAreaSetting);

            // Red
            winProgress.ShowMessage("Capture Flat-Red Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Flat-Red Image.");
            outputFlatPattern(Settings.Ins.Camera.MeasLevelR, 0, 0);
            CaptureImage(logDir + fn_FlatRed);

            // Black(Green)
            winProgress.ShowMessage("Capture Black Image(Green).");
            winProgress.PutForward1Step();
            saveUfLog("Capture Black Image(Green).");
            stopIntSig(); // Black出力(信号Stop)
            CaptureImage(logDir + fn_BlackFile + "_Green");

            // Green
            winProgress.ShowMessage("Capture Flat-Green Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Flat-Green Image.");
            outputFlatPattern(0, Settings.Ins.Camera.MeasLevelG, 0);
            CaptureImage(logDir + fn_FlatGreen);

            // Black(Blue)
            winProgress.ShowMessage("Capture Black Image(Blue).");
            winProgress.PutForward1Step();
            saveUfLog("Capture Black Image(Blue).");
            stopIntSig(); // Black出力(信号Stop)
            CaptureImage(logDir + fn_BlackFile + "_Blue");

            // Blue
            winProgress.ShowMessage("Capture Flat-Blue Image.");
            winProgress.PutForward1Step();
            saveUfLog("Capture Flat-Blue Image.");
            outputFlatPattern(0, 0, Settings.Ins.Camera.MeasLevelB);
            CaptureImage(logDir + fn_FlatBlue);

            // Black(Final)
            winProgress.ShowMessage("Capture Black Image(Final).");
            winProgress.PutForward1Step();
            saveUfLog("Capture Black Image(Final).");
            stopIntSig(); // Black出力(信号Stop)
            CaptureImage(logDir + fn_BlackFile + "_Final");
#endif

            // 内部信号停止
            stopIntSig();

            #endregion 撮影

            // added by Hotta 2025/01/24
            // ESCキー押下による中断を無効化
            winProgress.AbortType = WindowProgress.TAbortType.None;
            //

            #region 処理


            // added by Hotta 2024/09/11
#if ForCrosstalkCameraUF
            // RAW画像を読み込み
            AcqARW arwRK, arw, arwBlack;
            ushort[][,] rawR, rawG, rawB, rawRK, rawGK, rawBK;
            int width, height;

            winProgress.ShowMessage("Load Raw Picture.(Black)");
            winProgress.PutForward1Step();
            saveUfLog("Load Raw Picture.(Black)");
            loadArwFile(logDir + fn_BlackFile + "_Red.arw", out arwRK);
            rawRK = new ushort[3][,];
            width = arwRK.RawData[0].GetLength(0);
            height = arwRK.RawData[0].GetLength(1);
            for (int color = 0; color < 3; color++)
            {
                rawRK[color] = new ushort[width, height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        rawRK[color][x, y] = arwRK.RawData[color][x, y];
                    }
                }
            }

            winProgress.ShowMessage("Load Raw Picture.(Red)");
            winProgress.PutForward1Step();
            saveUfLog("Load Raw Picture.(Red)");
            loadArwFile(logDir + fn_FlatRed + ".arw", out arw);
            rawR = new ushort[3][,];
            for (int color = 0; color < 3; color++)
            {
                rawR[color] = new ushort[width, height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        rawR[color][x, y] = arw.RawData[color][x, y];
                    }
                }
            }
            arw = null;
            GC.Collect();

            winProgress.ShowMessage("Load Raw Picture.(Black)");
            winProgress.PutForward1Step();
            saveUfLog("Load Raw Picture.(Black)");
            loadArwFile(logDir + fn_BlackFile + "_Green.arw", out arw);
            rawGK = new ushort[3][,];
            for (int color = 0; color < 3; color++)
            {
                rawGK[color] = new ushort[width, height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        rawGK[color][x, y] = arw.RawData[color][x, y];
                    }
                }
            }
            arw = null;
            GC.Collect();

            winProgress.ShowMessage("Load Raw Picture.(Green)");
            winProgress.PutForward1Step();
            saveUfLog("Load Raw Picture.(Green)");
            loadArwFile(logDir + fn_FlatGreen + ".arw", out arw);
            rawG = new ushort[3][,];
            for (int color = 0; color < 3; color++)
            {
                rawG[color] = new ushort[width, height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        rawG[color][x, y] = arw.RawData[color][x, y];
                    }
                }
            }
            arw = null;
            GC.Collect();

            winProgress.ShowMessage("Load Raw Picture.(Black)");
            winProgress.PutForward1Step();
            saveUfLog("Load Raw Picture.(Black)");
            loadArwFile(logDir + fn_BlackFile + "_Blue.arw", out arw);
            rawBK = new ushort[3][,];
            for (int color = 0; color < 3; color++)
            {
                rawBK[color] = new ushort[width, height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        rawBK[color][x, y] = arw.RawData[color][x, y];
                    }
                }
            }
            arw = null;
            GC.Collect();

            winProgress.ShowMessage("Load Raw Picture.(Blue)");
            winProgress.PutForward1Step();
            saveUfLog("Load Raw Picture.(Blue)");
            loadArwFile(logDir + fn_FlatBlue + ".arw", out arw);
            rawB = new ushort[3][,];
            for (int color = 0; color < 3; color++)
            {
                rawB[color] = new ushort[width, height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        rawB[color][x, y] = arw.RawData[color][x, y];
                    }
                }
            }
            arw = null;
            GC.Collect();

            winProgress.ShowMessage("Load Raw Picture.(Black)");
            winProgress.PutForward1Step();
            saveUfLog("Load Raw Picture.(Black)");
            loadArwFile(logDir + fn_BlackFile + "_Final.arw", out arwBlack);


            // 測定前後のBlack比較
            try { CheckBlackDiff(logDir, arwRK, arwBlack); }
            catch (Exception ex)
            {
                bool? result;
                string msg = ex.Message + "\r\n\r\nDo you continue processing?";

                showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 500, 200, "Yes", "No");

                if (result != true)
                { throw new Exception("The Processing has been canceled."); }
            }
#else
            // RAW画像を読み込み
            AcqARW arwR, arwG, arwB, arwRK, arwGK, arwBK, arwBlack;

            winProgress.ShowMessage("Load Raw Picture.(Black)");
            winProgress.PutForward1Step();
            loadArwFile(logDir + fn_BlackFile + "_Red.arw", out arwRK);

            winProgress.ShowMessage("Load Raw Picture.(Red)");
            winProgress.PutForward1Step();
            loadArwFile(logDir + fn_FlatRed + ".arw", out arwR);

            winProgress.ShowMessage("Load Raw Picture.(Black)");
            winProgress.PutForward1Step();
            loadArwFile(logDir + fn_BlackFile + "_Green.arw", out arwGK);

            winProgress.ShowMessage("Load Raw Picture.(Green)");
            winProgress.PutForward1Step();
            loadArwFile(logDir + fn_FlatGreen + ".arw", out arwG);

            winProgress.ShowMessage("Load Raw Picture.(Black)");
            winProgress.PutForward1Step();
            loadArwFile(logDir + fn_BlackFile + "_Blue.arw", out arwBK);

            winProgress.ShowMessage("Load Raw Picture.(Blue)");
            winProgress.PutForward1Step();
            loadArwFile(logDir + fn_FlatBlue + ".arw", out arwB);

            winProgress.ShowMessage("Load Raw Picture.(Black)");
            winProgress.PutForward1Step();
            loadArwFile(logDir + fn_BlackFile + "_Final.arw", out arwBlack);

            // 測定前後のBlack比較
            try { CheckBlackDiff(logDir, arwRK, arwBlack); }
            catch (Exception ex)
            {
                bool? result;
                string msg = ex.Message + "\r\n\r\nDo you continue processing?";

                showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 500, 200, "Yes", "No");

                if (result != true)
                { throw new Exception("The Processing has been canceled."); }
            }
#endif

#endregion 処理

            // ●基準点
            PixelValue aveR, aveG, aveB;

            using (Mat refMask = new Mat(logDir + fn_MaskRef, ImreadModes.Grayscale))
            // added by Hotta 2024/09/11
#if ForCrosstalkCameraUF
            using (Mat arwMat = ConvertArwToMat(arwBlack))
#endif
            {
                // added by Hotta 2024/09/11
#if ForCrosstalkCameraUF
                MakeBinMask(refMask, arwMat, out Mat binMask);
#else
                MakeBinMask(refMask, arwBlack, out Mat binMask);
#endif
                foreach (UfCamCorrectionPoint refPt in lstRefPoints)
                {
                    // modified by Hotta 2024/09/11
#if ForCrosstalkCameraUF
                    CalcAverage(rawR, rawRK, refPt.CameraArea, binMask, refPt, null, out aveR);
                    CalcAverage(rawG, rawGK, refPt.CameraArea, binMask, refPt, null, out aveG);
                    CalcAverage(rawB, rawBK, refPt.CameraArea, binMask, refPt, null, out aveB);
#else
                    CalcAverage(arwR, arwRK, refPt.CameraArea, binMask, out aveR);
                    CalcAverage(arwG, arwGK, refPt.CameraArea, binMask, out aveG);
                    CalcAverage(arwB, arwBK, refPt.CameraArea, binMask, out aveB);
#endif
                    // 各色の目標値
                    refPt.PixelValueR = aveR.R / (refPt.CCV.CvGainR * refPt.LCV.CvGainR); // Math.Pow(refPoint.CCV.CvGainR, camRate);
                    refPt.PixelValueG = aveG.G / (refPt.CCV.CvGainG * refPt.LCV.CvGainG); // Math.Pow(refPoint.CCV.CvGainG, camRate);
                    refPt.PixelValueB = aveB.B / (refPt.CCV.CvGainB * refPt.LCV.CvGainB); // Math.Pow(refPoint.CCV.CvGainB, camRate);

                    // クロストーク補正量のキャンセルは、CalcAverage()で行う
                    /*
                    // added by Hotta 2024/09/09 for Crosstalk
                    // クロストーク補正量をキャンセル
#if ForCrosstalkCameraUF
                    if (allocInfo.LEDModel == ZRD_BH12D || allocInfo.LEDModel == ZRD_CH12D || allocInfo.LEDModel == ZRD_BH15D || allocInfo.LEDModel == ZRD_CH15D)
                    {
                        foreach (CrosstalkInfo info in m_lstCrosstalkInfo)
                        {
                            if (info.ControllerID == refPt.Unit.ControllerID && info.X == refPt.Unit.X && info.Y == refPt.Unit.Y)
                            {
                                refPt.PixelValueR /= (1.0f + info.Crosstalk[refPt.ModuleNo][0]);
                                refPt.PixelValueG /= (1.0f + info.Crosstalk[refPt.ModuleNo][1]);
                                refPt.PixelValueB /= (1.0f + info.Crosstalk[refPt.ModuleNo][2]);
                                break;
                            }
                        }
                    }
#endif
                    */
                }

                binMask.Dispose();
            }

            // ●各測定点
            using (Mat trimMask = new Mat(logDir + fn_MaskTrim + ".jpg", ImreadModes.Grayscale))
            // added by Hotta 2024/09/11
#if ForCrosstalkCameraUF
            using (Mat arwMat = ConvertArwToMat(arwBlack))
#endif
            {
                // added by Hotta 2024/09/11
#if ForCrosstalkCameraUF
                MakeBinMask(trimMask, arwMat, out Mat binMask);
#else
                MakeBinMask(trimMask, arwBlack, out Mat binMask);
#endif
                foreach (UfCamCabinetCpInfo unit in lstUnitCpInfo)
                {
                    foreach (UfCamCorrectionPoint cp in unit.lstCp)
                    {
                        // modified by Hotta 2024/09/11
#if ForCrosstalkCameraUF
                        CalcAverage(rawR, rawRK, cp.CameraArea, binMask, null, cp, out aveR);
                        CalcAverage(rawG, rawGK, cp.CameraArea, binMask, null, cp, out aveG);
                        CalcAverage(rawB, rawBK, cp.CameraArea, binMask, null, cp, out aveB);
#else
                        CalcAverage(arwR, arwRK, cp.CameraArea, binMask, out aveR);
                        CalcAverage(arwG, arwGK, cp.CameraArea, binMask, out aveG);
                        CalcAverage(arwB, arwBK, cp.CameraArea, binMask, out aveB);
#endif
                        cp.PixelValueR = aveR.R / (cp.CCV.CvGainR * cp.LCV.CvGainR); // Math.Pow(cp.CCV.CvGainR, camRate);
                        cp.PixelValueG = aveG.G / (cp.CCV.CvGainG * cp.LCV.CvGainG); // Math.Pow(cp.CCV.CvGainG, camRate);
                        cp.PixelValueB = aveB.B / (cp.CCV.CvGainB * cp.LCV.CvGainB); // Math.Pow(cp.CCV.CvGainB, camRate);

                        // クロストーク補正量のキャンセルは、CalcAverage()で行う
                        /*
                        // added by Hotta 2024/09/09 for Crosstalk
                        // クロストーク補正量をキャンセル
#if ForCrosstalkCameraUF
                        if (allocInfo.LEDModel == ZRD_BH12D || allocInfo.LEDModel == ZRD_CH12D || allocInfo.LEDModel == ZRD_BH15D || allocInfo.LEDModel == ZRD_CH15D)
                        {
                            foreach (CrosstalkInfo info in m_lstCrosstalkInfo)
                            {
                                if (info.ControllerID == cp.Unit.ControllerID && info.X == cp.Unit.X && info.Y == cp.Unit.Y)
                                {
                                    float[] crosstalk = new float[3];

                                    if (UfCamCorrectPos.Module_0 <= cp.Pos && cp.Pos <= UfCamCorrectPos.Module_11)
                                    {
                                        for (int i = 0; i < 3; i++)
                                            crosstalk[i] = info.Crosstalk[(int)(cp.Pos - UfCamCorrectPos.Module_0)][i];
                                    }
                                    else if (cp.Pos == UfCamCorrectPos.Cabinet_Top)
                                    {
                                        for (int i = 0; i < 3; i++)
                                            crosstalk[i] = (info.Crosstalk[1][i] + info.Crosstalk[2][i]) / 2;
                                    }
                                    else if (cp.Pos == UfCamCorrectPos.Cabinet_Bottom)
                                    {
                                        for (int i = 0; i < 3; i++)
                                            crosstalk[i] = (info.Crosstalk[9][i] + info.Crosstalk[10][i]) / 2;
                                    }
                                    else if (cp.Pos == UfCamCorrectPos.Cabinet_Left)
                                    {
                                        for (int i = 0; i < 3; i++)
                                            crosstalk[i] = info.Crosstalk[4][i];
                                    }
                                    else if (cp.Pos == UfCamCorrectPos.Cabinet_Right)
                                    {
                                        for (int i = 0; i < 3; i++)
                                            crosstalk[i] = info.Crosstalk[7][i];
                                    }
                                    else if (cp.Pos == UfCamCorrectPos.Radiator_L_Top)
                                    {
                                        for (int i = 0; i < 3; i++)
                                            crosstalk[i] = (info.Crosstalk[0][i] + info.Crosstalk[1][i]) / 2;
                                    }
                                    else if (cp.Pos == UfCamCorrectPos.Radiator_R_Top)
                                    {
                                        for (int i = 0; i < 3; i++)
                                            crosstalk[i] = (info.Crosstalk[2][i] + info.Crosstalk[3][i]) / 2;
                                    }
                                    else if (cp.Pos == UfCamCorrectPos.Radiator_L_Bottom)
                                    {
                                        for (int i = 0; i < 3; i++)
                                            crosstalk[i] = (info.Crosstalk[8][i] + info.Crosstalk[9][i]) / 2;
                                    }
                                    else if (cp.Pos == UfCamCorrectPos.Radiator_R_Bottom)
                                    {
                                        for (int i = 0; i < 3; i++)
                                            crosstalk[i] = (info.Crosstalk[10][i] + info.Crosstalk[11][i]) / 2;
                                    }
                                    else if (cp.Pos == UfCamCorrectPos.Radiator_L_Left)
                                    {
                                        for (int i = 0; i < 3; i++)
                                            crosstalk[i] = info.Crosstalk[4][i];
                                    }
                                    else if (cp.Pos == UfCamCorrectPos.Radiator_R_Left)
                                    {
                                        for (int i = 0; i < 3; i++)
                                            crosstalk[i] = info.Crosstalk[6][i];
                                    }
                                    else if (cp.Pos == UfCamCorrectPos.Radiator_L_Right)
                                    {
                                        for (int i = 0; i < 3; i++)
                                            crosstalk[i] = info.Crosstalk[5][i];
                                    }
                                    else if (cp.Pos == UfCamCorrectPos.Radiator_R_Right)
                                    {
                                        for (int i = 0; i < 3; i++)
                                            crosstalk[i] = info.Crosstalk[7][i];
                                    }
                                    else if (cp.Pos == UfCamCorrectPos._9pt_TopLeft)
                                    {
                                        for (int i = 0; i < 3; i++)
                                            crosstalk[i] = info.Crosstalk[0][i];
                                    }
                                    else if (cp.Pos == UfCamCorrectPos._9pt_TopRight)
                                    {
                                        for (int i = 0; i < 3; i++)
                                            crosstalk[i] = info.Crosstalk[3][i];
                                    }
                                    else if (cp.Pos == UfCamCorrectPos._9pt_Center)
                                    {
                                        for (int i = 0; i < 3; i++)
                                            crosstalk[i] = (info.Crosstalk[5][i] + info.Crosstalk[6][i]) / 2;
                                    }
                                    else if (cp.Pos == UfCamCorrectPos._9pt_BottomLeft)
                                    {
                                        for (int i = 0; i < 3; i++)
                                            crosstalk[i] = info.Crosstalk[8][i];
                                    }
                                    else if (cp.Pos == UfCamCorrectPos._9pt_BottomRight)
                                    {
                                        for (int i = 0; i < 3; i++)
                                            crosstalk[i] = info.Crosstalk[11][i];
                                    }

                                    cp.PixelValueR /= (1.0f + crosstalk[0]);
                                    cp.PixelValueG /= (1.0f + crosstalk[1]);
                                    cp.PixelValueB /= (1.0f + crosstalk[2]);
                                    break;

                                }
                            }
                        }
#endif
                        */

                    }
                }

                binMask.Dispose();
            }
        }

        private void MakeBinMask(Mat orgMask, AcqARW arwBlack, out Mat binMask)
        {
            using (Mat arwMat = ConvertArwToMat(arwBlack))
            using (Mat diff = orgMask - arwMat)
            using (Mat binary = diff.Threshold(0, 0xff, ThresholdTypes.Otsu))
            {
                binMask = new Mat();

                // # 8近傍の定義
                var neiborhood8 = new Mat(new OpenCvSharp.Size(3, 3), MatType.CV_8U);
                for (int y = 0; y < 3; y++)
                {
                    for (int x = 0; x < 3; x++)
                    { neiborhood8.Set<byte>(y, x, 1); }
                }

                // 平均的なTrimmingAreaの辺長を求める
                CvBlobs blobs = new CvBlobs(binary);

                double sum = 0;
                foreach (CvBlob blob in blobs.Values)
                { sum += blob.Rect.Width; }

                double aveLen = sum / blobs.Count * 1.1; // 平均の+10％以下

                sum = 0;
                int count = 0;

                foreach (CvBlob blob in blobs.Values)
                {
                    if (blob.Rect.Width < aveLen)
                    {
                        sum += blob.Rect.Width;
                        count++;
                    }
                }

                if (count == 0)
                { count = 1; }

                double len = sum / count;

                //// modified by Hotta 2024/08/29
                //// 収縮やめてみる
                //binMask = binary.Clone();
                //return;
                ////

                // 縮小処理
                Cv2.Erode(binary, binMask, neiborhood8, null, (int)(len * Settings.Ins.Camera.Blanking));
                if (Settings.Ins.Camera.SaveIntImage == true)
                { Cv2.ImWrite(tempPath + @"\MaskTrim.jpg", binMask); }
            }
        }

        // added by Hotta 2024/09/11
#if ForCrosstalkCameraUF
        private void MakeBinMask(Mat orgMask, Mat arwMat, out Mat binMask)
        {
            //using (Mat arwMat = ConvertArwToMat(arwBlack))
            using (Mat diff = orgMask - arwMat)
            using (Mat binary = diff.Threshold(0, 0xff, ThresholdTypes.Otsu))
            {
                binMask = new Mat();

                // # 8近傍の定義
                var neiborhood8 = new Mat(new OpenCvSharp.Size(3, 3), MatType.CV_8U);
                for (int y = 0; y < 3; y++)
                {
                    for (int x = 0; x < 3; x++)
                    { neiborhood8.Set<byte>(y, x, 1); }
                }

                // 平均的なTrimmingAreaの辺長を求める
                CvBlobs blobs = new CvBlobs(binary);

                double sum = 0;
                foreach (CvBlob blob in blobs.Values)
                { sum += blob.Rect.Width; }

                double aveLen = sum / blobs.Count * 1.1; // 平均の+10％以下

                sum = 0;
                int count = 0;

                foreach (CvBlob blob in blobs.Values)
                {
                    if (blob.Rect.Width < aveLen)
                    {
                        sum += blob.Rect.Width;
                        count++;
                    }
                }

                if (count == 0)
                { count = 1; }

                double len = sum / count;

                //// modified by Hotta 2024/08/29
                //// 収縮やめてみる
                //binMask = binary.Clone();
                //return;
                ////

                // 縮小処理
                Cv2.Erode(binary, binMask, neiborhood8, null, (int)(len * Settings.Ins.Camera.Blanking));
                if (Settings.Ins.Camera.SaveIntImage == true)
                { Cv2.ImWrite(tempPath + @"\MaskTrim.jpg", binMask); }
            }
        }


#endif

        unsafe private Mat ConvertArwToMat(AcqARW arw)
        {
            Mat gray = new Mat();

            int width = arw.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
            int height = arw.RawMainIFD.ImageHeight / 2;

            using (Mat src = new Mat(new Size(width, height), MatType.CV_16UC1))
            {
                ushort* pMat = (ushort*)(src.Data);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    { pMat[y * width + x] = (ushort)arw.RawData[1][x, y]; }
                }

                src.ConvertTo(gray, MatType.CV_8UC1, 1.0 / 16);
            }

            return gray;
        }

        unsafe private void CheckBlackDiff(string logDir, AcqARW arwPre, AcqARW arwPost)
        {
            bool result = true;
            int width = arwPre.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
            int height = arwPre.RawMainIFD.ImageHeight / 2;
            string maskFile = logDir + fn_MaskFile;

            using (Mat src = new Mat(new Size(width, height), MatType.CV_16UC1))
            using (Mat gray = new Mat())
            using (Mat mask = new Mat(maskFile, ImreadModes.Grayscale))
            using (Mat masked = new Mat())
            {
                ushort* pMat = (ushort*)(src.Data);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int pv = Math.Abs(arwPre.RawData[1][x, y] - arwPost.RawData[1][x, y]);
                        pMat[y * width + x] = (ushort)pv;
                    }
                }

                src.ConvertTo(gray, MatType.CV_8UC1, 1.0 / 16);

                gray.CopyTo(masked, mask.Threshold(0, 0xff, ThresholdTypes.Otsu));

                int thresh = (int)(255 * 0.03); // 3%

                using (Mat binary = masked.Threshold(thresh, 0xff, ThresholdTypes.Binary))
                {
                    CvBlobs blobs = new CvBlobs(binary);
                    blobs.FilterByArea(5, int.MaxValue);

                    if (Settings.Ins.Camera.SaveIntImage)
                    {
                        Cv2.ImWrite(tempPath + @"\binary.jpg", binary);
                        Cv2.ImWrite(tempPath + fn_BlackDiff, gray);
                        Cv2.ImWrite(tempPath + @"\mask.jpg", mask);
                        Cv2.ImWrite(tempPath + @"\masked.jpg", masked);
                    }

                    // 最大のBlobを探す
                    CvBlob maxBlob = new CvBlob();
                    foreach (CvBlob blob in blobs.Values)
                    {
                        if (blob.Area > maxBlob.Area)
                        { maxBlob = blob; }
                    }

                    if(maxBlob.Area > Settings.Ins.Camera.BlackDiffLimit)
                    { result = false; }
                }
            }

            if (result != true) // Matがちゃんと廃棄されるか心配なのでUsing後に例外をThrow
            { throw new Exception("There is a change in the brightness of the environment before and after shooting."); }
        }

        private void CalcAverage(AcqARW arw, AcqARW arwBlack, Area area, Mat TrimMask, out PixelValue average)
        {
            double sum_r = 0.0;
            double sum_g = 0.0;
            double sum_b = 0.0;
            int startX = (int)area.StartPos.X;
            int endX = (int)area.EndPos.X;
            int startY = (int)area.StartPos.Y;
            int endY = (int)area.EndPos.Y;

            int count = 0;
            int saturationCount = 0;

            average = new PixelValue();

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    // マスク処理
                    int val = TrimMask.At<Vec3b>(y, x)[0];

                    if (val != 0)
                    {
                        double num11 = (int)arw.RawData[0][x, y] - (int)arwBlack.RawData[0][x, y];
                        if (num11 < 0) { num11 = 0; }
                        double num12 = (int)arw.RawData[1][x, y] - (int)arwBlack.RawData[1][x, y];
                        if (num12 < 0) { num12 = 0; }
                        double num13 = (int)arw.RawData[2][x, y] - (int)arwBlack.RawData[2][x, y];
                        if (num13 < 0) { num13 = 0; }

                        sum_r += num11;
                        sum_g += num12;
                        sum_b += num13;

                        if(num11 > SaturationSpec || num12 > SaturationSpec || num13 > SaturationSpec)
                        { saturationCount++; }

                        count++;
                    }
                }
            }

            // サチり確認
            if(saturationCount > Settings.Ins.Camera.SaturationLimit)
            { throw new Exception("There are more than the specified number of saturated pixels.\r\nReview the camera settings."); }

            // 0除算対策
            if (count == 0)
            { count = 1; }

            average.R = sum_r / (double)count; // - (double)Settings.Ins.Camera.ColorOffset;
            average.G = sum_g / (double)count; // - (double)Settings.Ins.Camera.ColorOffset;
            average.B = sum_b / (double)count; // - (double)Settings.Ins.Camera.ColorOffset;

            // Raw画像の明るさ確認
            double pvMax = 0;

            // 最大の画素値（＝主成分）が確認対象
            if (average.R > pvMax)
            { pvMax = average.R; }
            if (average.G > pvMax)
            { pvMax = average.G; }
            if (average.B > pvMax)
            { pvMax = average.B; }

            string msg = "Point [x, y] : [" + area.CenterPos.X + ", " + area.CenterPos.Y + "]\r\nCurrent Value : (Red)" + average.R.ToString("0") + ", (Green)" + average.G.ToString("0") + ", (Blue)" + average.B.ToString("0") + "\r\nSpec : ";

            if (pvMax > Settings.Ins.Camera.MaxRawValue)
            { throw new Exception("The brightness of the raw image is too bright.\r\nReview the camera settings.\r\n\r\n" + msg + Settings.Ins.Camera.MaxRawValue); }

            if (pvMax < Settings.Ins.Camera.MinRawValue)
            { throw new Exception("The brightness of the raw image is too dark.\r\nReview the camera settings.\r\n\r\n" + msg + Settings.Ins.Camera.MinRawValue); }
        }

        // added by Hotta 2024/09/11 for crosstalk
#if ForCrosstalkCameraUF
        /*
                private void CalcAverage(ushort[][,] raw, ushort[][,] rawBlack, Area area, Mat TrimMask, out PixelValue average)
                {
                    double sum_r = 0.0;
                    double sum_g = 0.0;
                    double sum_b = 0.0;
                    int startX = (int)area.StartPos.X;
                    int endX = (int)area.EndPos.X;
                    int startY = (int)area.StartPos.Y;
                    int endY = (int)area.EndPos.Y;

                    int count = 0;
                    int saturationCount = 0;

                    average = new PixelValue();

                    for (int y = startY; y < endY; y++)
                    {
                        for (int x = startX; x < endX; x++)
                        {
                            // マスク処理
                            int val = TrimMask.At<Vec3b>(y, x)[0];

                            if (val != 0)
                            {
                                double num11 = (int)raw[0][x, y] - (int)rawBlack[0][x, y];
                                if (num11 < 0) { num11 = 0; }
                                double num12 = (int)raw[1][x, y] - (int)rawBlack[1][x, y];
                                if (num12 < 0) { num12 = 0; }
                                double num13 = (int)raw[2][x, y] - (int)rawBlack[2][x, y];
                                if (num13 < 0) { num13 = 0; }

                                sum_r += num11;
                                sum_g += num12;
                                sum_b += num13;

                                if (num11 > SaturationSpec || num12 > SaturationSpec || num13 > SaturationSpec)
                                { saturationCount++; }

                                count++;
                            }
                        }
                    }

                    // サチり確認
                    if (saturationCount > Settings.Ins.Camera.SaturationLimit)
                    { throw new Exception("There are more than the specified number of saturated pixels.\r\nReview the camera settings."); }

                    // 0除算対策
                    if (count == 0)
                    { count = 1; }

                    average.R = sum_r / (double)count; // - (double)Settings.Ins.Camera.ColorOffset;
                    average.G = sum_g / (double)count; // - (double)Settings.Ins.Camera.ColorOffset;
                    average.B = sum_b / (double)count; // - (double)Settings.Ins.Camera.ColorOffset;

                    // Raw画像の明るさ確認
                    double pvMax = 0;

                    // 最大の画素値（＝主成分）が確認対象
                    if (average.R > pvMax)
                    { pvMax = average.R; }
                    if (average.G > pvMax)
                    { pvMax = average.G; }
                    if (average.B > pvMax)
                    { pvMax = average.B; }

                    string msg = "Point [x, y] : [" + area.CenterPos.X + ", " + area.CenterPos.Y + "]\r\nCurrent Value : (Red)" + average.R.ToString("0") + ", (Green)" + average.G.ToString("0") + ", (Blue)" + average.B.ToString("0") + "\r\nSpec : ";

                    if (pvMax > Settings.Ins.Camera.MaxRawValue)
                    { throw new Exception("The brightness of the raw image is too bright.\r\nReview the camera settings.\r\n\r\n" + msg + Settings.Ins.Camera.MaxRawValue); }

                    if (pvMax < Settings.Ins.Camera.MinRawValue)
                    { throw new Exception("The brightness of the raw image is too dark.\r\nReview the camera settings.\r\n\r\n" + msg + Settings.Ins.Camera.MinRawValue); }
                }
        */
        private void CalcAverage(ushort[][,] raw, ushort[][,] rawBlack, Area area, Mat TrimMask, UfCamCorrectionPoint refPt, UfCamCorrectionPoint cp, out PixelValue average)
        {
            double sum_r = 0.0;
            double sum_g = 0.0;
            double sum_b = 0.0;
            int startX = (int)area.StartPos.X;
            int endX = (int)area.EndPos.X;
            int startY = (int)area.StartPos.Y;
            int endY = (int)area.EndPos.Y;

            int count = 0;
            int saturationCount = 0;

            average = new PixelValue();

            // Chiron
            // modified by Hotta 2024/10/09
            /*
            if (allocInfo.LEDModel == ZRD_BH12D || allocInfo.LEDModel == ZRD_CH12D || allocInfo.LEDModel == ZRD_BH15D || allocInfo.LEDModel == ZRD_CH15D)
            */
            if (!(allocInfo.LEDModel == ZRD_BH12D 
                || allocInfo.LEDModel == ZRD_CH12D 
                || allocInfo.LEDModel == ZRD_BH15D 
                || allocInfo.LEDModel == ZRD_CH15D
                || allocInfo.LEDModel == ZRD_BH12D_S3
                || allocInfo.LEDModel == ZRD_CH12D_S3
                || allocInfo.LEDModel == ZRD_BH15D_S3
                || allocInfo.LEDModel == ZRD_CH15D_S3))
            {
#if (DEBUG || Release_log)
                if (Settings.Ins.ExecLog == true)
                {
                    MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                }
#endif
                getSpecifiedAreaValue(area, TrimMask, raw, rawBlack, out sum_r, out sum_g, out sum_b, out count, out saturationCount);
            }
            // 基準Module
            else if (cp == null)
            {
#if (DEBUG || Release_log)
                if (Settings.Ins.ExecLog == true)
                {
                    MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                }
#endif
                getSpecifiedAreaValue(area, TrimMask, raw, rawBlack, out sum_r, out sum_g, out sum_b, out count, out saturationCount);

                // クロストーク補正量をキャンセル
                foreach (CrosstalkInfo info in m_lstCrosstalkInfo)
                {
                    if (info.ControllerID == refPt.Unit.ControllerID && info.X == refPt.Unit.X && info.Y == refPt.Unit.Y)
                    {
                        sum_r /= (1.0f + info.Crosstalk[refPt.ModuleNo][0]);
                        sum_g /= (1.0f + info.Crosstalk[refPt.ModuleNo][1]);
                        sum_b /= (1.0f + info.Crosstalk[refPt.ModuleNo][2]);
                        break;
                    }
                }
            }
            // 補正Module
            else
            {
#if (DEBUG || Release_log)
                if (Settings.Ins.ExecLog == true)
                {
                    MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                }
#endif
                // 単一Module
                if ((UfCamCorrectPos.Module_0 <= cp.Pos && cp.Pos <= UfCamCorrectPos.Module_11) ||
                    cp.Pos == UfCamCorrectPos.Cabinet_Left ||
                    cp.Pos == UfCamCorrectPos.Cabinet_Right ||
                    cp.Pos == UfCamCorrectPos.Radiator_L_Left ||
                    cp.Pos == UfCamCorrectPos.Radiator_R_Left ||
                    cp.Pos == UfCamCorrectPos.Radiator_L_Right ||
                    cp.Pos == UfCamCorrectPos.Radiator_R_Right ||
                    cp.Pos == UfCamCorrectPos._9pt_TopLeft ||
                    cp.Pos == UfCamCorrectPos._9pt_TopRight ||
                    cp.Pos == UfCamCorrectPos._9pt_BottomLeft ||
                    cp.Pos == UfCamCorrectPos._9pt_BottomRight
                )
                {
                    getSpecifiedAreaValue(area, TrimMask, raw, rawBlack, out sum_r, out sum_g, out sum_b, out count, out saturationCount);
                    
                    // クロストーク補正量をキャンセル
                    int idx = -1;
                    foreach (CrosstalkInfo info in m_lstCrosstalkInfo)
                    {
                        if (info.ControllerID == cp.Unit.ControllerID && info.X == cp.Unit.X && info.Y == cp.Unit.Y)
                        {
                            if (UfCamCorrectPos.Module_0 <= cp.Pos && cp.Pos <= UfCamCorrectPos.Module_11) { idx = cp.Pos - UfCamCorrectPos.Module_0; }
                            else if (cp.Pos == UfCamCorrectPos.Cabinet_Left) { idx = 4; }
                            else if (cp.Pos == UfCamCorrectPos.Cabinet_Right) { idx = 7; }
                            else if (cp.Pos == UfCamCorrectPos.Radiator_L_Left) { idx = 4; }
                            else if (cp.Pos == UfCamCorrectPos.Radiator_R_Left) { idx = 6; }
                            else if (cp.Pos == UfCamCorrectPos.Radiator_L_Right) { idx = 5; }
                            else if (cp.Pos == UfCamCorrectPos.Radiator_R_Right) { idx = 7; }
                            else if (cp.Pos == UfCamCorrectPos._9pt_TopLeft) { idx = 0; }
                            else if (cp.Pos == UfCamCorrectPos._9pt_TopRight) { idx = 3; }
                            else if (cp.Pos == UfCamCorrectPos._9pt_BottomLeft) { idx = 8; }
                            else if (cp.Pos == UfCamCorrectPos._9pt_BottomRight) { idx = 11; }

                            sum_r /= (1.0f + info.Crosstalk[idx][0]);
                            sum_g /= (1.0f + info.Crosstalk[idx][1]);
                            sum_b /= (1.0f + info.Crosstalk[idx][2]);
                            break;
                        }
                    }
                }
                // 左右並びModule
                else
                {
                    double sum_r0 = 0, sum_r1 = 0;
                    double sum_g0 = 0, sum_g1 = 0;
                    double sum_b0 = 0, sum_b1 = 0;
                    int count0 = 0, count1 = 0;
                    int saturationCount0 = 0, saturationCount1 = 0;

                    // 左半分
                    Area area0 = new Area((int)area.StartPos.X, (int)area.StartPos.Y, (int)(area.EndPos.Y - area.StartPos.Y), (int)(area.EndPos.X - area.StartPos.X) / 2);
                    getSpecifiedAreaValue(area0, TrimMask, raw, rawBlack, out sum_r0, out sum_g0, out sum_b0, out count0, out saturationCount0);

                    // 右半分
                    Area area1 = new Area((int)area.StartPos.X + (int)(area.EndPos.X - area0.StartPos.X) / 2, (int)area.StartPos.Y, (int)(area.EndPos.Y - area.StartPos.Y), (int)(area.EndPos.X - area0.StartPos.X) / 2);
                    getSpecifiedAreaValue(area1, TrimMask, raw, rawBlack, out sum_r1, out sum_g1, out sum_b1, out count1, out saturationCount1);

                    // クロストーク補正量をキャンセル
                    int idx0 = -1, idx1 = -1;
                    foreach (CrosstalkInfo info in m_lstCrosstalkInfo)
                    {
                        if (info.ControllerID == cp.Unit.ControllerID && info.X == cp.Unit.X && info.Y == cp.Unit.Y)
                        {
                            if (cp.Pos == UfCamCorrectPos.Cabinet_Top) { idx0 = 1; idx1 = 2; }
                            else if (cp.Pos == UfCamCorrectPos.Cabinet_Bottom) { idx0 = 9; idx1 = 10; }
                            else if (cp.Pos == UfCamCorrectPos.Radiator_L_Top) { idx0 = 0; idx1 = 1; }
                            else if (cp.Pos == UfCamCorrectPos.Radiator_R_Top) { idx0 = 2; idx1 = 3; }
                            else if (cp.Pos == UfCamCorrectPos.Radiator_L_Bottom) { idx0 = 8; idx1 = 9; }
                            else if (cp.Pos == UfCamCorrectPos.Radiator_R_Bottom) { idx0 = 10; idx1 = 11; }
                            else if (cp.Pos == UfCamCorrectPos._9pt_Center) { idx0 = 5; idx1 = 6; }

                            sum_r0 /= (1.0f + info.Crosstalk[idx0][0]);
                            sum_g0 /= (1.0f + info.Crosstalk[idx0][1]);
                            sum_b0 /= (1.0f + info.Crosstalk[idx0][2]);

                            sum_r1 /= (1.0f + info.Crosstalk[idx1][0]);
                            sum_g1 /= (1.0f + info.Crosstalk[idx1][1]);
                            sum_b1 /= (1.0f + info.Crosstalk[idx1][2]);

                            sum_r = sum_r0 + sum_r1;
                            sum_g = sum_g0 + sum_g1;
                            sum_b = sum_b0 + sum_b1;

                            count = count0 + count1;
                            saturationCount = saturationCount0 + saturationCount1;

                            break;
                        }
                    }
                }
            }

            // サチり確認
            if (saturationCount > Settings.Ins.Camera.SaturationLimit)
            { throw new Exception("There are more than the specified number of saturated pixels.\r\nReview the camera settings."); }

            // 0除算対策
            if (count == 0)
            { count = 1; }

            average.R = sum_r / (double)count; // - (double)Settings.Ins.Camera.ColorOffset;
            average.G = sum_g / (double)count; // - (double)Settings.Ins.Camera.ColorOffset;
            average.B = sum_b / (double)count; // - (double)Settings.Ins.Camera.ColorOffset;

            // Raw画像の明るさ確認
            double pvMax = 0;

            // 最大の画素値（＝主成分）が確認対象
            if (average.R > pvMax)
            { pvMax = average.R; }
            if (average.G > pvMax)
            { pvMax = average.G; }
            if (average.B > pvMax)
            { pvMax = average.B; }

            string msg = "Point [x, y] : [" + area.CenterPos.X + ", " + area.CenterPos.Y + "]\r\nCurrent Value : (Red)" + average.R.ToString("0") + ", (Green)" + average.G.ToString("0") + ", (Blue)" + average.B.ToString("0") + "\r\nSpec : ";

            if (pvMax > Settings.Ins.Camera.MaxRawValue)
            { throw new Exception("The brightness of the raw image is too bright.\r\nReview the camera settings.\r\n\r\n" + msg + Settings.Ins.Camera.MaxRawValue); }

            if (pvMax < Settings.Ins.Camera.MinRawValue)
            { throw new Exception("The brightness of the raw image is too dark.\r\nReview the camera settings.\r\n\r\n" + msg + Settings.Ins.Camera.MinRawValue); }
        }



        void getSpecifiedAreaValue(Area area, Mat TrimMask, ushort[][,] raw, ushort[][,] rawBlack, out double sum_r, out double sum_g, out double sum_b, out int count, out int saturationCount)
        {
            sum_r = sum_g = sum_b = 0;
            count = saturationCount = 0;

            for (int y = (int)area.StartPos.Y; y < (int)area.EndPos.Y; y++)
            {
                for (int x = (int)area.StartPos.X; x < (int)area.EndPos.X; x++)
                {
                    // マスク処理
                    int val = TrimMask.At<Vec3b>(y, x)[0];

                    if (val != 0)
                    {
                        double num11 = (int)raw[0][x, y] - (int)rawBlack[0][x, y];
                        if (num11 < 0) { num11 = 0; }
                        double num12 = (int)raw[1][x, y] - (int)rawBlack[1][x, y];
                        if (num12 < 0) { num12 = 0; }
                        double num13 = (int)raw[2][x, y] - (int)rawBlack[2][x, y];
                        if (num13 < 0) { num13 = 0; }

                        sum_r += num11;
                        sum_g += num12;
                        sum_b += num13;

                        if (num11 > SaturationSpec || num12 > SaturationSpec || num13 > SaturationSpec)
                        { saturationCount++; }

                        count++;
                    }
                }
            }

        }        
#endif

        private void calcAverageWhite(AcqARW arwHelperR, AcqARW arwHelperG, AcqARW arwHelperB, Area area, out PixelValue average, double blanking) // blanking = 削り量(割合) 0～0.5未満
        {
            double sum_r = 0.0;
            double sum_g = 0.0;
            double sum_b = 0.0;
            int startX = (int)area.StartPos.X + (int)(area.Width * blanking);
            int endX = (int)area.EndPos.X - (int)(area.Width * blanking);
            int startY = (int)area.StartPos.Y + (int)(area.Height * blanking);
            int endY = (int)area.EndPos.Y - (int)(area.Height * blanking);

            average = new PixelValue();

            int count = 0;
            for (int i = startY; i < endY; i++)
            {
                for (int j = startX; j < endX; j++)
                {
                    double num11 = (int)arwHelperR.RawData[0][j, i] + (int)arwHelperG.RawData[0][j, i] + (int)arwHelperB.RawData[0][j, i];
                    double num12 = (int)arwHelperR.RawData[1][j, i] + (int)arwHelperG.RawData[1][j, i] + (int)arwHelperB.RawData[1][j, i];
                    double num13 = (int)arwHelperR.RawData[2][j, i] + (int)arwHelperG.RawData[2][j, i] + (int)arwHelperB.RawData[2][j, i];

                    sum_r += num11;
                    sum_g += num12;
                    sum_b += num13;

                    count++;
                }
            }

            // 0除算対策
            if (count == 0)
            { count = 1; }

            average.R = sum_r / (double)count; // - (double)Settings.Ins.Camera.ColorOffset;
            average.G = sum_g / (double)count; // - (double)Settings.Ins.Camera.ColorOffset;
            average.B = sum_b / (double)count; // - (double)Settings.Ins.Camera.ColorOffset;
        }

        /// <summary>
        /// 各調整点の角度を計算して格納する
        /// </summary>
        /// <param name="cabi"></param>
        /// <param name="lstAngle"></param>
        private List<UfCamCpAngle> CalcCpAngle(UnitInfo cabi)
        {
            List<UfCamCpAngle> lstAngle = new List<UfCamCpAngle>();

            // CabinetのH, V辺ベクトル
            SpatialCoordinate vectorH = new SpatialCoordinate(cabi.CabinetPos.BottomRight.x - cabi.CabinetPos.BottomLeft.x, cabi.CabinetPos.BottomRight.y - cabi.CabinetPos.BottomLeft.y, cabi.CabinetPos.BottomRight.z - cabi.CabinetPos.BottomLeft.z);
            SpatialCoordinate vectorV = new SpatialCoordinate(cabi.CabinetPos.TopLeft.x - cabi.CabinetPos.BottomLeft.x, cabi.CabinetPos.TopLeft.y - cabi.CabinetPos.BottomLeft.y, cabi.CabinetPos.TopLeft.z - cabi.CabinetPos.BottomLeft.z);
            UfCamCpAngle tempCpAngle;
            SpatialCoordinate cpSc;

            double cabiPan = ToDegree(Math.Atan((cabi.CabinetPos.BottomRight.z - cabi.CabinetPos.BottomLeft.z) / (cabi.CabinetPos.BottomRight.x - cabi.CabinetPos.BottomLeft.x))); // ラウンド形状の場合、右側は正、左側は負になるはず。フラットの場合は0
            double cabiTilt = ToDegree(Math.Atan((cabi.CabinetPos.TopLeft.z - cabi.CabinetPos.BottomLeft.z)/ (cabi.CabinetPos.TopLeft.y - cabi.CabinetPos.BottomLeft.y)));

            double moduleV = (double)(moduleCount / ModuleCountX); // 縦のModule数 Chiron=2, Cancun=3

#region 9pt(Cabinet含む)

            // Top-Left
            tempCpAngle = new UfCamCpAngle();
            tempCpAngle.CorrectPos = UfCamCorrectPos._9pt_TopLeft;

            cpSc = new SpatialCoordinate(cabi.CabinetPos.BottomLeft);

            double h = 0;
            double v = 1.0;
            cpSc.x += vectorH.x * h + vectorV.x * v;
            cpSc.y += vectorH.y * h + vectorV.y * v;
            cpSc.z += vectorH.z * h + vectorV.z * v;

            double cpPan = -ToDegree(Math.Atan(cpSc.x / cpSc.z)) - cabiPan;
            if (cpPan > 90) { cpPan -= 180; }
            double cpTilt = ToDegree(Math.Atan(cpSc.y / cpSc.z)) + cabiTilt;
            if (cpTilt > 90) { cpTilt -= 180; }

            tempCpAngle.Angle.Pan = cpPan;
            tempCpAngle.Angle.Tilt = cpTilt;
            tempCpAngle.Angle.r = Math.Sqrt(Math.Pow(cpSc.x, 2) + Math.Pow(cpSc.y, 2) + Math.Pow(cpSc.z, 2)); // 多分使わないけど一応
            lstAngle.Add(tempCpAngle);

            // Top-Center
            tempCpAngle = new UfCamCpAngle();
            tempCpAngle.CorrectPos = UfCamCorrectPos.Cabinet_Top;

            cpSc = new SpatialCoordinate(cabi.CabinetPos.BottomLeft);

            h = 0.5;
            v = 1.0;
            cpSc.x += vectorH.x * h + vectorV.x * v;
            cpSc.y += vectorH.y * h + vectorV.y * v;
            cpSc.z += vectorH.z * h + vectorV.z * v;

            cpPan = -ToDegree(Math.Atan(cpSc.x / cpSc.z)) - cabiPan;
            if (cpPan > 90) { cpPan -= 180; }
            cpTilt = ToDegree(Math.Atan(cpSc.y / cpSc.z)) + cabiTilt;
            if (cpTilt > 90) { cpTilt -= 180; }

            tempCpAngle.Angle.Pan = cpPan;
            tempCpAngle.Angle.Tilt = cpTilt;
            tempCpAngle.Angle.r = Math.Sqrt(Math.Pow(cpSc.x, 2) + Math.Pow(cpSc.y, 2) + Math.Pow(cpSc.z, 2)); // 多分使わないけど一応
            lstAngle.Add(tempCpAngle);

            // Top-Right
            tempCpAngle = new UfCamCpAngle();
            tempCpAngle.CorrectPos = UfCamCorrectPos._9pt_TopRight;

            cpSc = new SpatialCoordinate(cabi.CabinetPos.BottomLeft);

            h = 1.0;
            v = 1.0;
            cpSc.x += vectorH.x * h + vectorV.x * v;
            cpSc.y += vectorH.y * h + vectorV.y * v;
            cpSc.z += vectorH.z * h + vectorV.z * v;

            cpPan = -ToDegree(Math.Atan(cpSc.x / cpSc.z)) - cabiPan;
            if (cpPan > 90) { cpPan -= 180; }
            cpTilt = ToDegree(Math.Atan(cpSc.y / cpSc.z)) + cabiTilt;
            if (cpTilt > 90) { cpTilt -= 180; }

            tempCpAngle.Angle.Pan = cpPan;
            tempCpAngle.Angle.Tilt = cpTilt;
            tempCpAngle.Angle.r = Math.Sqrt(Math.Pow(cpSc.x, 2) + Math.Pow(cpSc.y, 2) + Math.Pow(cpSc.z, 2)); // 多分使わないけど一応
            lstAngle.Add(tempCpAngle);

            // Middle-Left
            tempCpAngle = new UfCamCpAngle();
            tempCpAngle.CorrectPos = UfCamCorrectPos.Cabinet_Left;

            cpSc = new SpatialCoordinate(cabi.CabinetPos.BottomLeft);

            h = 0;
            v = 0.5;
            cpSc.x += vectorH.x * h + vectorV.x * v;
            cpSc.y += vectorH.y * h + vectorV.y * v;
            cpSc.z += vectorH.z * h + vectorV.z * v;

            cpPan = -ToDegree(Math.Atan(cpSc.x / cpSc.z)) - cabiPan;
            if (cpPan > 90) { cpPan -= 180; }
            cpTilt = ToDegree(Math.Atan(cpSc.y / cpSc.z)) + cabiTilt;
            if (cpTilt > 90) { cpTilt -= 180; }

            tempCpAngle.Angle.Pan = cpPan;
            tempCpAngle.Angle.Tilt = cpTilt;
            tempCpAngle.Angle.r = Math.Sqrt(Math.Pow(cpSc.x, 2) + Math.Pow(cpSc.y, 2) + Math.Pow(cpSc.z, 2)); // 多分使わないけど一応
            lstAngle.Add(tempCpAngle);

            // Center
            tempCpAngle = new UfCamCpAngle();
            tempCpAngle.CorrectPos = UfCamCorrectPos._9pt_Center;

            cpSc = new SpatialCoordinate(cabi.CabinetPos.BottomLeft);

            h = 0.5;
            v = 0.5;
            cpSc.x += vectorH.x * h + vectorV.x * v;
            cpSc.y += vectorH.y * h + vectorV.y * v;
            cpSc.z += vectorH.z * h + vectorV.z * v;

            cpPan = -ToDegree(Math.Atan(cpSc.x / cpSc.z)) - cabiPan;
            if (cpPan > 90) { cpPan -= 180; }
            cpTilt = ToDegree(Math.Atan(cpSc.y / cpSc.z)) + cabiTilt;
            if (cpTilt > 90) { cpTilt -= 180; }

            tempCpAngle.Angle.Pan = cpPan;
            tempCpAngle.Angle.Tilt = cpTilt;
            tempCpAngle.Angle.r = Math.Sqrt(Math.Pow(cpSc.x, 2) + Math.Pow(cpSc.y, 2) + Math.Pow(cpSc.z, 2)); // 多分使わないけど一応
            lstAngle.Add(tempCpAngle);

            // Middle-Right
            tempCpAngle = new UfCamCpAngle();
            tempCpAngle.CorrectPos = UfCamCorrectPos.Cabinet_Right;

            cpSc = new SpatialCoordinate(cabi.CabinetPos.BottomLeft);

            h = 1.0;
            v = 0.5;
            cpSc.x += vectorH.x * h + vectorV.x * v;
            cpSc.y += vectorH.y * h + vectorV.y * v;
            cpSc.z += vectorH.z * h + vectorV.z * v;

            cpPan = -ToDegree(Math.Atan(cpSc.x / cpSc.z)) - cabiPan;
            if (cpPan > 90) { cpPan -= 180; }
            cpTilt = ToDegree(Math.Atan(cpSc.y / cpSc.z)) + cabiTilt;
            if (cpTilt > 90) { cpTilt -= 180; }

            tempCpAngle.Angle.Pan = cpPan;
            tempCpAngle.Angle.Tilt = cpTilt;
            tempCpAngle.Angle.r = Math.Sqrt(Math.Pow(cpSc.x, 2) + Math.Pow(cpSc.y, 2) + Math.Pow(cpSc.z, 2)); // 多分使わないけど一応
            lstAngle.Add(tempCpAngle);

            // Bottom-Left
            tempCpAngle = new UfCamCpAngle();
            tempCpAngle.CorrectPos = UfCamCorrectPos._9pt_BottomLeft;

            cpSc = new SpatialCoordinate(cabi.CabinetPos.BottomLeft);

            h = 0;
            v = 0;
            cpSc.x += vectorH.x * h + vectorV.x * v;
            cpSc.y += vectorH.y * h + vectorV.y * v;
            cpSc.z += vectorH.z * h + vectorV.z * v;

            cpPan = -ToDegree(Math.Atan(cpSc.x / cpSc.z)) - cabiPan;
            if (cpPan > 90) { cpPan -= 180; }
            cpTilt = ToDegree(Math.Atan(cpSc.y / cpSc.z)) + cabiTilt;
            if (cpTilt > 90) { cpTilt -= 180; }

            tempCpAngle.Angle.Pan = cpPan;
            tempCpAngle.Angle.Tilt = cpTilt;
            tempCpAngle.Angle.r = Math.Sqrt(Math.Pow(cpSc.x, 2) + Math.Pow(cpSc.y, 2) + Math.Pow(cpSc.z, 2)); // 多分使わないけど一応
            lstAngle.Add(tempCpAngle);

            // Bottom-Center
            tempCpAngle = new UfCamCpAngle();
            tempCpAngle.CorrectPos = UfCamCorrectPos.Cabinet_Bottom;

            cpSc = new SpatialCoordinate(cabi.CabinetPos.BottomLeft);

            h = 0.5;
            v = 0;
            cpSc.x += vectorH.x * h + vectorV.x * v;
            cpSc.y += vectorH.y * h + vectorV.y * v;
            cpSc.z += vectorH.z * h + vectorV.z * v;

            cpPan = -ToDegree(Math.Atan(cpSc.x / cpSc.z)) - cabiPan;
            if (cpPan > 90) { cpPan -= 180; }
            cpTilt = ToDegree(Math.Atan(cpSc.y / cpSc.z)) + cabiTilt;
            if (cpTilt > 90) { cpTilt -= 180; }

            tempCpAngle.Angle.Pan = cpPan;
            tempCpAngle.Angle.Tilt = cpTilt;
            tempCpAngle.Angle.r = Math.Sqrt(Math.Pow(cpSc.x, 2) + Math.Pow(cpSc.y, 2) + Math.Pow(cpSc.z, 2)); // 多分使わないけど一応
            lstAngle.Add(tempCpAngle);

            // Bottom-Right
            tempCpAngle = new UfCamCpAngle();
            tempCpAngle.CorrectPos = UfCamCorrectPos._9pt_BottomRight;

            cpSc = new SpatialCoordinate(cabi.CabinetPos.BottomLeft);

            h = 1.0;
            v = 0;
            cpSc.x += vectorH.x * h + vectorV.x * v;
            cpSc.y += vectorH.y * h + vectorV.y * v;
            cpSc.z += vectorH.z * h + vectorV.z * v;

            cpPan = -ToDegree(Math.Atan(cpSc.x / cpSc.z)) - cabiPan;
            if (cpPan > 90) { cpPan -= 180; }
            cpTilt = ToDegree(Math.Atan(cpSc.y / cpSc.z)) + cabiTilt;
            if (cpTilt > 90) { cpTilt -= 180; }

            tempCpAngle.Angle.Pan = cpPan;
            tempCpAngle.Angle.Tilt = cpTilt;
            tempCpAngle.Angle.r = Math.Sqrt(Math.Pow(cpSc.x, 2) + Math.Pow(cpSc.y, 2) + Math.Pow(cpSc.z, 2)); // 多分使わないけど一応
            lstAngle.Add(tempCpAngle);

#endregion 9pt

#region Radiator

            // Left Top
            tempCpAngle = new UfCamCpAngle();
            tempCpAngle.CorrectPos = UfCamCorrectPos.Radiator_L_Top;

            cpSc = new SpatialCoordinate(cabi.CabinetPos.BottomLeft);

            h = 0.25;
            v = 1.0;
            cpSc.x += vectorH.x * h + vectorV.x * v;
            cpSc.y += vectorH.y * h + vectorV.y * v;
            cpSc.z += vectorH.z * h + vectorV.z * v;

            cpPan = -ToDegree(Math.Atan(cpSc.x / cpSc.z)) - cabiPan;
            if (cpPan > 90) { cpPan -= 180; }
            cpTilt = ToDegree(Math.Atan(cpSc.y / cpSc.z)) + cabiTilt;
            if (cpTilt > 90) { cpTilt -= 180; }

            tempCpAngle.Angle.Pan = cpPan;
            tempCpAngle.Angle.Tilt = cpTilt;
            tempCpAngle.Angle.r = Math.Sqrt(Math.Pow(cpSc.x, 2) + Math.Pow(cpSc.y, 2) + Math.Pow(cpSc.z, 2)); // 多分使わないけど一応
            lstAngle.Add(tempCpAngle);

            // Right Top
            tempCpAngle = new UfCamCpAngle();
            tempCpAngle.CorrectPos = UfCamCorrectPos.Radiator_R_Top;

            cpSc = new SpatialCoordinate(cabi.CabinetPos.BottomLeft);

            h = 0.75;
            v = 1.0;
            cpSc.x += vectorH.x * h + vectorV.x * v;
            cpSc.y += vectorH.y * h + vectorV.y * v;
            cpSc.z += vectorH.z * h + vectorV.z * v;

            cpPan = -ToDegree(Math.Atan(cpSc.x / cpSc.z)) - cabiPan;
            if (cpPan > 90) { cpPan -= 180; }
            cpTilt = ToDegree(Math.Atan(cpSc.y / cpSc.z)) + cabiTilt;
            if (cpTilt > 90) { cpTilt -= 180; }

            tempCpAngle.Angle.Pan = cpPan;
            tempCpAngle.Angle.Tilt = cpTilt;
            tempCpAngle.Angle.r = Math.Sqrt(Math.Pow(cpSc.x, 2) + Math.Pow(cpSc.y, 2) + Math.Pow(cpSc.z, 2)); // 多分使わないけど一応
            lstAngle.Add(tempCpAngle);

            // Left Left
            tempCpAngle = new UfCamCpAngle();
            tempCpAngle.CorrectPos = UfCamCorrectPos.Radiator_L_Left;

            cpSc = new SpatialCoordinate(cabi.CabinetPos.BottomLeft);

            h = 0;
            v = 0.5;
            cpSc.x += vectorH.x * h + vectorV.x * v;
            cpSc.y += vectorH.y * h + vectorV.y * v;
            cpSc.z += vectorH.z * h + vectorV.z * v;

            cpPan = -ToDegree(Math.Atan(cpSc.x / cpSc.z)) - cabiPan;
            if (cpPan > 90) { cpPan -= 180; }
            cpTilt = ToDegree(Math.Atan(cpSc.y / cpSc.z)) + cabiTilt;
            if (cpTilt > 90) { cpTilt -= 180; }

            tempCpAngle.Angle.Pan = cpPan;
            tempCpAngle.Angle.Tilt = cpTilt;
            tempCpAngle.Angle.r = Math.Sqrt(Math.Pow(cpSc.x, 2) + Math.Pow(cpSc.y, 2) + Math.Pow(cpSc.z, 2)); // 多分使わないけど一応
            lstAngle.Add(tempCpAngle);

            // Left Right
            tempCpAngle = new UfCamCpAngle();
            tempCpAngle.CorrectPos = UfCamCorrectPos.Radiator_L_Right;

            cpSc = new SpatialCoordinate(cabi.CabinetPos.BottomLeft);

            h = 0.5;
            v = 0.5;
            cpSc.x += vectorH.x * h + vectorV.x * v;
            cpSc.y += vectorH.y * h + vectorV.y * v;
            cpSc.z += vectorH.z * h + vectorV.z * v;

            cpPan = -ToDegree(Math.Atan(cpSc.x / cpSc.z)) - cabiPan;
            if (cpPan > 90) { cpPan -= 180; }
            cpTilt = ToDegree(Math.Atan(cpSc.y / cpSc.z)) + cabiTilt;
            if (cpTilt > 90) { cpTilt -= 180; }

            tempCpAngle.Angle.Pan = cpPan;
            tempCpAngle.Angle.Tilt = cpTilt;
            tempCpAngle.Angle.r = Math.Sqrt(Math.Pow(cpSc.x, 2) + Math.Pow(cpSc.y, 2) + Math.Pow(cpSc.z, 2)); // 多分使わないけど一応
            lstAngle.Add(tempCpAngle);

            // Right Left
            tempCpAngle = new UfCamCpAngle();
            tempCpAngle.CorrectPos = UfCamCorrectPos.Radiator_R_Left;

            cpSc = new SpatialCoordinate(cabi.CabinetPos.BottomLeft);

            h = 0.5;
            v = 0.5;
            cpSc.x += vectorH.x * h + vectorV.x * v;
            cpSc.y += vectorH.y * h + vectorV.y * v;
            cpSc.z += vectorH.z * h + vectorV.z * v;

            cpPan = -ToDegree(Math.Atan(cpSc.x / cpSc.z)) - cabiPan;
            if (cpPan > 90) { cpPan -= 180; }
            cpTilt = ToDegree(Math.Atan(cpSc.y / cpSc.z)) + cabiTilt;
            if (cpTilt > 90) { cpTilt -= 180; }

            tempCpAngle.Angle.Pan = cpPan;
            tempCpAngle.Angle.Tilt = cpTilt;
            tempCpAngle.Angle.r = Math.Sqrt(Math.Pow(cpSc.x, 2) + Math.Pow(cpSc.y, 2) + Math.Pow(cpSc.z, 2)); // 多分使わないけど一応
            lstAngle.Add(tempCpAngle);

            // Right Right
            tempCpAngle = new UfCamCpAngle();
            tempCpAngle.CorrectPos = UfCamCorrectPos.Radiator_R_Right;

            cpSc = new SpatialCoordinate(cabi.CabinetPos.BottomLeft);

            h = 1.0;
            v = 0.5;
            cpSc.x += vectorH.x * h + vectorV.x * v;
            cpSc.y += vectorH.y * h + vectorV.y * v;
            cpSc.z += vectorH.z * h + vectorV.z * v;

            cpPan = -ToDegree(Math.Atan(cpSc.x / cpSc.z)) - cabiPan;
            if (cpPan > 90) { cpPan -= 180; }
            cpTilt = ToDegree(Math.Atan(cpSc.y / cpSc.z)) + cabiTilt;
            if (cpTilt > 90) { cpTilt -= 180; }

            tempCpAngle.Angle.Pan = cpPan;
            tempCpAngle.Angle.Tilt = cpTilt;
            tempCpAngle.Angle.r = Math.Sqrt(Math.Pow(cpSc.x, 2) + Math.Pow(cpSc.y, 2) + Math.Pow(cpSc.z, 2)); // 多分使わないけど一応
            lstAngle.Add(tempCpAngle);

            // Left Bottom
            tempCpAngle = new UfCamCpAngle();
            tempCpAngle.CorrectPos = UfCamCorrectPos.Radiator_L_Bottom;

            cpSc = new SpatialCoordinate(cabi.CabinetPos.BottomLeft);

            h = 0.25;
            v = 0;
            cpSc.x += vectorH.x * h + vectorV.x * v;
            cpSc.y += vectorH.y * h + vectorV.y * v;
            cpSc.z += vectorH.z * h + vectorV.z * v;

            cpPan = -ToDegree(Math.Atan(cpSc.x / cpSc.z)) - cabiPan;
            if (cpPan > 90) { cpPan -= 180; }
            cpTilt = ToDegree(Math.Atan(cpSc.y / cpSc.z)) + cabiTilt;
            if (cpTilt > 90) { cpTilt -= 180; }

            tempCpAngle.Angle.Pan = cpPan;
            tempCpAngle.Angle.Tilt = cpTilt;
            tempCpAngle.Angle.r = Math.Sqrt(Math.Pow(cpSc.x, 2) + Math.Pow(cpSc.y, 2) + Math.Pow(cpSc.z, 2)); // 多分使わないけど一応
            lstAngle.Add(tempCpAngle);

            // Right Bottom
            tempCpAngle = new UfCamCpAngle();
            tempCpAngle.CorrectPos = UfCamCorrectPos.Radiator_R_Bottom;

            cpSc = new SpatialCoordinate(cabi.CabinetPos.BottomLeft);

            h = 0.75;
            v = 0;
            cpSc.x += vectorH.x * h + vectorV.x * v;
            cpSc.y += vectorH.y * h + vectorV.y * v;
            cpSc.z += vectorH.z * h + vectorV.z * v;

            cpPan = -ToDegree(Math.Atan(cpSc.x / cpSc.z)) - cabiPan;
            if (cpPan > 90) { cpPan -= 180; }
            cpTilt = ToDegree(Math.Atan(cpSc.y / cpSc.z)) + cabiTilt;
            if (cpTilt > 90) { cpTilt -= 180; }

            tempCpAngle.Angle.Pan = cpPan;
            tempCpAngle.Angle.Tilt = cpTilt;
            tempCpAngle.Angle.r = Math.Sqrt(Math.Pow(cpSc.x, 2) + Math.Pow(cpSc.y, 2) + Math.Pow(cpSc.z, 2)); // 多分使わないけど一応
            lstAngle.Add(tempCpAngle);

#endregion Radiator

#region Module

            for (int y = 0; y < moduleV; y++)
            {
                for (int x = 0; x < ModuleCountX; x++)
                {
                    tempCpAngle = new UfCamCpAngle();
                    tempCpAngle.CorrectPos = (UfCamCorrectPos)Enum.ToObject(typeof(UfCamCorrectPos), 12 + (ModuleCountX * y + x)); // 12 = EnumのModuleの先頭

                    cpSc = new SpatialCoordinate(cabi.CabinetPos.BottomLeft);

                    h = (1.0 + 2.0 * x) / (ModuleCountX * 2.0);
                    v = (((moduleV * 2.0) - 1.0) - (y * 2.0)) / (moduleV * 2.0);
                    cpSc.x += vectorH.x * h + vectorV.x * v;
                    cpSc.y += vectorH.y * h + vectorV.y * v;
                    cpSc.z += vectorH.z * h + vectorV.z * v;

                    cpPan = -ToDegree(Math.Atan(cpSc.x / cpSc.z)) - cabiPan;
                    if (cpPan > 90) { cpPan -= 180; }
                    cpTilt = ToDegree(Math.Atan(cpSc.y / cpSc.z)) + cabiTilt;
                    if (cpTilt > 90) { cpTilt -= 180; }

                    tempCpAngle.Angle.Pan = cpPan;
                    tempCpAngle.Angle.Tilt = cpTilt;
                    tempCpAngle.Angle.r = Math.Sqrt(Math.Pow(cpSc.x, 2) + Math.Pow(cpSc.y, 2) + Math.Pow(cpSc.z, 2)); // 多分使わないけど一応
                    lstAngle.Add(tempCpAngle);
                }
            }

#endregion Module

            return lstAngle;
        }

        /// <summary>
        /// レンズ、LED補正値を取得する
        /// </summary>
        /// <param name="cp">補正点</param>
        /// <param name="ccv">レンズ補正値</param>
        /// <param name="lcv">LED補正値</param>
        /// <param name="ctTilt">視聴点調整モード時の中心Tilt角、NaNの場合は無効</param>
        private void SearchCameraCorrectValue(UfCamCorrectionPoint cp, out CameraCorrectionValue ccv, out LedCorrectionValue lcv, ViewPoint vp)
        {
            CameraCorrectionValue tempCcdData = null;
            LedCorrectionValue tempLcdData = null;
            CameraCorrectionValue dstData = new CameraCorrectionValue();
            double minDist = double.MaxValue;

            // Ccd(レンズ校正テーブル)
            foreach (CameraCorrectionValue val in ccd.lstCamCorrectValues)
            {
                // 中心間距離が最小のものを選択
                double dist = CalcDistance(cp.CameraArea.CenterPos, val.CenterCoordinate);

                if (dist < minDist)
                {
                    tempCcdData = val;
                    minDist = dist;
                }
            }

            ccv = tempCcdData;

            // Lcd(LED校正テーブル)
            // 測定点のPan, Tilt角を取得する
            GetCpPanTilt(cp, out double pan, out double tilt);

            // 視聴点調整モード時は中心のPan/Tilt角を使用する
            if (vp.Vertical == true)
            { tilt = vp.RefTilt; }

            if (vp.Horizontal == true)
            { pan = vp.RefPan; }

            minDist = double.MaxValue;
            foreach (LedCorrectionValue val in lcd.lstCamCorrectValues)
            {
                // 角度差が最小のものを選択
                double dist = CalcDistance(new Coordinate(pan, tilt), new Coordinate(val.PanAngle, val.TiltAngle));

                if (dist < minDist)
                {
                    tempLcdData = val;
                    minDist = dist;
                }
            }

            lcv = tempLcdData;

            if (tempCcdData == null || tempLcdData == null)
            { throw new Exception("Camera correction data does not exist."); }
        }

        private void GetCpPanTilt(UfCamCorrectionPoint cp, out double pan, out double tilt)
        {
            UfCamCorrectPos pos;

            pan = double.NaN;
            tilt = double.NaN;

            if (cp.Pos == UfCamCorrectPos.Reference)
            {
                PickReferenceModule(cp.Unit, out Quadrant quad, out int module);
                pos = (UfCamCorrectPos)Enum.ToObject(typeof(UfCamCorrectPos), 12 + module); // 12 = EnumのModuleの先頭
            }
            else
            { pos = cp.Pos; }

            foreach(UfCamCpAngle angle in cp.Unit.lstCpAngle)
            {
                if(angle.CorrectPos == pos)
                {
                    pan = angle.Angle.Pan;
                    tilt = angle.Angle.Tilt;
                    break;
                }
            }

            if(double.IsNaN(pan) == true || double.IsNaN(tilt) == true)
            { throw new Exception("Failed to get adjustment point angle."); }
        }

        private bool writeAdjustedData(List<MoveFile> lstMoveFile)
        {
            // ●TestPattern OFF [1]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[1] Test Pattern Off."); }
            winProgress.ShowMessage("Test Pattern Off.");

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdIntSignalOff, 0, cont.IPAddress); }
            //stopIntSig();

            // ●調整をするCabinetが接続されているController格納 [2]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[2] Store Target Controller(s) Info."); }
            winProgress.ShowMessage("Store Target Controller(s) Info.");

            // 初期化
            foreach (ControllerInfo controller in dicController.Values)
            { controller.Target = false; }

            foreach (MoveFile unit in lstMoveFile)
            { dicController[unit.ControllerID].Target = true; }

            // ●Model名・Serialの取得(要らなそう)
            foreach (ControllerInfo cont in dicController.Values)
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*1] Get Model Name."); }
                winProgress.ShowMessage("Get Model Name.");

                // Model名
                if (string.IsNullOrWhiteSpace(cont.ModelName) == true)
                { cont.ModelName = getModelName(cont.IPAddress); }

                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*2] Get Serial No."); }
                winProgress.ShowMessage("Get Serial No.");

                // Serial
                if (string.IsNullOrWhiteSpace(cont.SerialNo) == true)
                { cont.SerialNo = getSerialNo(cont.IPAddress); }
            }

            // ●FTP ON
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[3] FTP On."); }
            winProgress.ShowMessage("FTP On.");

            foreach (ControllerInfo controller in dicController.Values)
            {
                if (sendSdcpCommand(SDCPClass.CmdFtpOn, controller.IPAddress) != true)
                { throw new Exception("Failed to set FTP mode on."); }
            }

            // ●Delete Ftp Files
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[4] Delete FTP Files."); }
            winProgress.ShowMessage("Delete FTP Files.");

            foreach (ControllerInfo controller in dicController.Values)
            {
                if(controller.Target == true)
                { deleteFtpFile(controller); }
            }

            // ●調整済みファイルの移動 [5]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[5] Move Adjusted Files."); }
            winProgress.ShowMessage("Move Adjusted Files.");

            bool status = true;
            foreach (MoveFile move in lstMoveFile)
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\tMoved File = " + move.FilePath); }

                // ファイル移動
                if (Settings.Ins.TransType == TransType.FTP)
                {
                    if (Settings.Ins.ExecLog == true)
                    { SaveExecLog("File Move Start."); }

                    try { status = putFileFtpRetry(dicController[move.ControllerID].IPAddress, move.FilePath); }
                    catch { status = false; }

                    if (Settings.Ins.ExecLog == true)
                    { SaveExecLog("File Move End."); }
                }
                else // NFS
                {
                    try { System.IO.File.Copy(move.FilePath, dicController[move.ControllerID].Drive + "\\" + System.IO.Path.GetFileName(move.FilePath)); }
                    catch { status = false; }
                }

                if (status == false) // ファイルの移動が失敗するケースへの対応
                {
                    System.Threading.Thread.Sleep(5000);

                    if (Settings.Ins.TransType == TransType.FTP)
                    {
                        try { status = putFileFtpRetry(dicController[move.ControllerID].IPAddress, move.FilePath); }
                        catch { status = false; }
                    }
                    else // NFS
                    {
                        try
                        {
                            System.IO.File.Copy(move.FilePath, dicController[move.ControllerID].Drive + "\\" + System.IO.Path.GetFileName(move.FilePath));
                            status = true;
                        }
                        catch { status = false; }
                    }
                }

                // Retry
                if (status == false)
                {
                    if (Settings.Ins.ExecLog == true)
                    { SaveExecLog("\t(Retry to move file.)"); }

                    System.Threading.Thread.Sleep(5000);

                    if (Settings.Ins.TransType == TransType.FTP)
                    {
                        try
                        { status = putFileFtpRetry(dicController[move.ControllerID].IPAddress, move.FilePath); }
                        catch
                        {
                            ShowMessageWindow("Failed in moving adjusted UF data file.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                            return false;
                        }
                    }
                    else // NFS
                    {
                        try
                        {
                            System.IO.File.Copy(move.FilePath, dicController[move.ControllerID].Drive + "\\" + System.IO.Path.GetFileName(move.FilePath));
                            status = true;
                        }
                        catch
                        {
                            ShowMessageWindow("Failed in moving adjusted UF data file.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                            return false;
                        }
                    }

                    if (status == false)
                    {
                        ShowMessageWindow("Failed in moving adjusted UF data file.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                        return false;
                    }
                }
            }

            // ●Cabinet Power Off [6]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[6] Cabinet Power Off."); }
            winProgress.ShowMessage("Cabinet Power Off.");

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdUnitPowerOff, cont.IPAddress); }
            System.Threading.Thread.Sleep(SLEEP_TIME_AFTER_PANEL_OFF);
            
            if (getUnitPowerStatus() != true)
            {
                string msg = "Failed to cabinet power off.";
                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }

            // ●Reconfig [7]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[7] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            sendReconfig();

            // ●書き込みコマンド発行 [8]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[8] Send Write Command."); }
            winProgress.ShowMessage("Send Write Command.");

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdDataWrite, cont.IPAddress); }

            // ●書き込みComplete待ち [9]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[9] Waiting for the response."); }
            winProgress.ShowMessage("Waiting for the response.");

            bool[] complete = new bool[dicController.Count];

            while (true)
            {
                int id = 0;
                foreach (ControllerInfo controller in dicController.Values)
                {
                    if (controller.Target == true)
                    {
                        if (Settings.Ins.TransType == TransType.FTP)
                        { complete[id] = checkCompleteFtp(controller.IPAddress, "write_complete"); }
                        else
                        { complete[id] = checkComplete(controller.Drive, "write_complete"); }
                    }
                    else
                    { complete[id] = true; }

                    id++;
                }

                bool allComplete = true;
                for (int i = 0; i < complete.Length; i++)
                {
                    if (complete[i] == false)
                    { allComplete = false; }
                }

                if (allComplete == true)
                { break; }

                winProgress.ShowMessage("Waiting for the process of controller.");

                System.Threading.Thread.Sleep(1000);

                // Timeout
            }

#if NO_CAP
#else
            // ●Latest → Previousフォルダへコピー [10]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[10] Move Latest to Previous."); }
            winProgress.ShowMessage("Move Latest to Previous.");

            foreach (ControllerInfo controller in dicController.Values)
            { copyLatest2Previous(applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo); }

            // ●Temp → Latestフォルダへコピー [11]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[11] Move Temp to Latest."); }
            winProgress.ShowMessage("Move Temp to Latest.");

            foreach (ControllerInfo controller in dicController.Values)
            { copyTemp2Latest(applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo); }
#endif

            // ●Reconfig [12]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[12] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            sendReconfig();

            // ●Tempフォルダのファイルを削除
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[13] Delete Temp Files."); }
            winProgress.ShowMessage("Delete Temp Files.");

            foreach (ControllerInfo controller in dicController.Values)
            {
                string tempPath = applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo + "_Temp";

                if (Directory.Exists(tempPath) != true)
                { Directory.CreateDirectory(tempPath); }

                string[] files = Directory.GetFiles(tempPath, "*", System.IO.SearchOption.AllDirectories);

                for (int i = 0; i < files.Length; i++)
                {
                    try { File.Delete(files[i]); }
                    catch { return false; }
                }
            }

            // ●FTP Off [14]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[14] FTP Off."); }
            winProgress.ShowMessage("FTP Off.");

            foreach (ControllerInfo controller in dicController.Values)
            {
                if (sendSdcpCommand(SDCPClass.CmdFtpOff, controller.IPAddress) != true)
                {
                    ShowMessageWindow("Failed to FTP Off.", "Error!", System.Drawing.SystemIcons.Error, 420, 180);
                    return false;
                }
            }

            return true;
        }

        private void DeleteUnwantedImagesAdj(string path)
        {
            // MeasArea.arw
            string file = path + fn_AreaFile;
            if (File.Exists(file) == true)
            {
                try { File.Delete(file); }
                catch { } // 無視
            }

            // Mask.jpg
            file = path + fn_MaskFile;
            if (File.Exists(file) == true)
            {
                try { File.Delete(file); }
                catch { } // 無視
            }

            // MaskRef.jpg
            file = path + fn_MaskRef;
            if (File.Exists(file) == true)
            {
                try { File.Delete(file); }
                catch { } // 無視
            }

            // MaskTrim*.jpg
            file = fn_MaskTrim + "*.jpg";
            string[] files = Directory.GetFiles(path, file);
            for (int i = 0; i < files.Length; i++)
            {
                try { File.Delete(files[i]); }
                catch { } // 無視
            }

            // Module_*.arw(RGB以外)
            files = Directory.GetFiles(path, "*.arw");
            for (int i = 0; i < files.Length; i++)
            {
                if (System.IO.Path.GetFileNameWithoutExtension(files[i]) != fn_FlatRed
                    && System.IO.Path.GetFileNameWithoutExtension(files[i]) != fn_FlatGreen
                    && System.IO.Path.GetFileNameWithoutExtension(files[i]) != fn_FlatBlue
                    && System.IO.Path.GetFileNameWithoutExtension(files[i]) != fn_BlackFile)
                {
                    try { File.Delete(files[i]); }
                    catch { } // 無視
                }
            }
        }

        /// <summary>
        /// 調整目標の画素値を計算する。
        /// Base Cabinet = Default, Cabientの場合は1つしかないのでそれをそのまま使用する。
        /// Lineの場合、1辺の時は同じ行／列の値、2辺の場合は距離荷重平均
        /// </summary>
        /// <param name="unitCpInfo"></param>
        /// <param name="lstRefPoints"></param>
        /// <param name="objEdge"></param>
        /// <param name="refPt"></param>
        private void CalcReferenceValue(UfCamCabinetCpInfo unitCpInfo, List<UfCamCorrectionPoint> lstRefPoints, ObjectiveLine objEdge, out UfCamCorrectionPoint refPt)
        {
            if(objEdge == null) // Base Cabinet = Default/Cabinet
            {                
                refPt = lstRefPoints[0];
            }
            else// Base Cabinet = Line
            {
                int lineCount = 0;

                if(objEdge.Top == true)
                { lineCount++; }
                if(objEdge.Bottom == true)
                { lineCount++; }
                if (objEdge.Left == true)
                { lineCount++; }
                if (objEdge.Right == true)
                { lineCount++; }

                refPt = new UfCamCorrectionPoint();
                refPt.Pos = UfCamCorrectPos.Reference;

                if (lineCount == 1)
                {                    
                    if(objEdge.Top == true || objEdge.Bottom == true) // Top/Bottom
                    {
                        double r = 0, g = 0, b = 0;
                        int count = 0;
                        UnitInfo refCabi = new UnitInfo();
                        foreach (UfCamCorrectionPoint pt in lstRefPoints)
                        {
                            if (pt.Unit.X == unitCpInfo.Unit.X)
                            {
                                r += pt.PixelValueR;
                                g += pt.PixelValueG;
                                b += pt.PixelValueB;
                                refCabi = pt.Unit;
                                count++;
                            }
                        }

                        // 0除算の回避
                        if (count == 0)
                        { count = 1; }

                        refPt = new UfCamCorrectionPoint();
                        refPt.PixelValueR = r / count;
                        refPt.PixelValueG = g / count;
                        refPt.PixelValueB = b / count;
                        refPt.Unit = refCabi;
                    }
                    else // Left/Right
                    {
                        double r = 0, g = 0, b = 0;
                        int count = 0;
                        UnitInfo refCabi = null;
                        foreach (UfCamCorrectionPoint pt in lstRefPoints)
                        {
                            if (pt.Unit.Y == unitCpInfo.Unit.Y)
                            {
                                r += pt.PixelValueR;
                                g += pt.PixelValueG;
                                b += pt.PixelValueB;
                                refCabi = pt.Unit;
                                count++;
                            }
                        }

                        // 0除算の回避
                        if (count == 0)
                        { count = 1; }

                        refPt.PixelValueR = r / count;
                        refPt.PixelValueG = g / count;
                        refPt.PixelValueB = b / count;
                        refPt.Unit = refCabi;
                    }
                }
                else if (lineCount == 2)
                {
                    UfCamCorrectionPoint refPtH, refPtV;

                    // Top/Bottom                    
                    double r = 0, g = 0, b = 0, centerX = 0, centerY = 0;
                    int count = 0;
                    UnitInfo refCabi = new UnitInfo();
                    foreach(UfCamCorrectionPoint pt in lstRefPoints)
                    {
                        if (pt.Unit.X == unitCpInfo.Unit.X)
                        {
                            r += pt.PixelValueR;
                            g += pt.PixelValueG;
                            b += pt.PixelValueB;
                            refCabi = pt.Unit;
                            count++;
                        }
                    }

                    // 0除算の回避
                    if (count == 0)
                    { count = 1; }

                    refPtH = new UfCamCorrectionPoint();
                    refPtH.PixelValueR = r / count;
                    refPtH.PixelValueG = g / count;
                    refPtH.PixelValueB = b / count;
                    refPtH.CameraArea = new Area();
                    refPtH.CameraArea.StartPos = new Coordinate(centerX / count, centerY / count); // StartPosに平均座標(カメラ画像座標)を格納する
                    refPtH.Unit = refCabi;

                    // Left/Right
                    r = 0; g = 0; b = 0; centerX = 0; centerY = 0; count = 0;
                    foreach (UfCamCorrectionPoint pt in lstRefPoints)
                    {
                        if (pt.Unit.Y == unitCpInfo.Unit.Y)
                        {
                            r += pt.PixelValueR;
                            g += pt.PixelValueG;
                            b += pt.PixelValueB;
                            refCabi = pt.Unit;
                            count++;
                        }
                    }

                    // 0除算の回避
                    if (count == 0)
                    { count = 1; }

                    refPtV = new UfCamCorrectionPoint();
                    refPtV.PixelValueR = r / count;
                    refPtV.PixelValueG = g / count;
                    refPtV.PixelValueB = b / count;
                    refPtV.CameraArea = new Area();
                    refPtV.CameraArea.StartPos = new Coordinate(centerX / count, centerY / count); // StartPosに平均座標(カメラ画像座標)を格納する
                    refPtV.Unit = refCabi;

                    // 平均を求める(距離荷重平均)
                    // 調整対象Cabinetの中心座標を求める
                    centerX = 0; centerY = 0; count = 0;
                    foreach(UfCamCorrectionPoint cp in unitCpInfo.lstCp)
                    {
                        centerX += cp.CameraArea.CenterPos.X;
                        centerY += cp.CameraArea.CenterPos.Y;
                        count++;
                    }

                    // 0除算の回避
                    if (count == 0)
                    { count = 1; }

                    Coordinate tgtCt = new Coordinate(centerX / count, centerY / count); // 調整対象Cabinet中心座標(カメラ画像座標)

                    double distH = Math.Abs(unitCpInfo.Unit.Y - refPtH.Unit.Y); // H方向の基準Cabinet(平均)との距離(Cabinet数)
                    double distV = Math.Abs(unitCpInfo.Unit.X - refPtV.Unit.X); // V方向

                    refPt.PixelValueR = (refPtH.PixelValueR * distV / (distH + distV)) + (refPtV.PixelValueR * distH / (distH + distV));
                    refPt.PixelValueG = (refPtH.PixelValueG * distV / (distH + distV)) + (refPtV.PixelValueG * distH / (distH + distV));
                    refPt.PixelValueB = (refPtH.PixelValueB * distV / (distH + distV)) + (refPtV.PixelValueB * distH / (distH + distV));
                    refPt.Unit = refPtH.Unit;
                }
                else
                { throw new Exception("Incorrect number of lines selected."); }
            }
        }



        // added by Hotta 2024/09/09 for crosstalk
#if ForCrosstalkCameraUF
        //void LoadCrosstalkValue(List<UfCamCorrectionPoint> lstRefPoints, List<UfCamCabinetCpInfo> lstUnitCpInfo)
        //{
        //    bool status;
        //    FileDirectory baseFileDir;

        //    List<UnitInfo> lstUnit = new List<UnitInfo>();

        //    m_lstCrosstalkInfo = new List<CrosstalkInfo>();
        //    m_lstCrosstalkInfo.Clear();

        //    // lstRefPoints の Module ごとのクロストーク補正量を取得
        //    foreach (UfCamCorrectionPoint refPoint in lstRefPoints)
        //    {
        //        if (lstUnit.Contains(refPoint.Unit) != true)
        //            lstUnit.Add(refPoint.Unit);
        //    }
        //    status = checkDataFile(lstUnit, out baseFileDir);
        //    if (status != true)
        //    { throw new Exception("The UF data file(hc.bin) was not found."); }

        //    foreach (UfCamCorrectionPoint _refPoint in lstRefPoints)
        //    {
        //        // 対象hc.binのファイル名取得
        //        string filePath = makeFilePath(_refPoint.Unit, baseFileDir);
        //        // 対象hc.binからクロストーク補正量を取得
        //        status = m_MakeUFData.ExtractFmt(filePath, allocInfo.LEDModel, _refPoint.Unit);
        //        if (status != true)
        //        { throw new Exception("Failed in ExtractFmt."); }
        //    }

        //    ////////lstUnit.Clear();

        //    //lstUnitCpInfo の Module ごとのクロストーク補正量を取得
        //    foreach (UfCamCabinetCpInfo unitCpInfo in lstUnitCpInfo)
        //    {
        //        if (lstUnit.Contains(unitCpInfo.Unit) != true)
        //            lstUnit.Add(unitCpInfo.Unit);
        //    }
        //    status = checkDataFile(lstUnit, out baseFileDir);
        //    if (status != true)
        //    { throw new Exception("The UF data file(hc.bin) was not found."); }

        //    foreach (UfCamCabinetCpInfo unitCpInfo in ufCamAdjLog.lstUnitCpInfo)
        //    {
        //        // 対象hc.binのファイル名取得
        //        string filePath = makeFilePath(unitCpInfo.Unit, baseFileDir);
        //        // 対象hc.binからクロストーク補正量を取得
        //        status = m_MakeUFData.ExtractFmt(filePath, allocInfo.LEDModel, unitCpInfo.Unit);
        //        if (status != true)
        //        { throw new Exception("Failed in ExtractFmt."); }
        //    }

        //    // unitCpInfo と lstRefPoints の画素明るさのクロストーク補正分をキャンセルする
        //    ////////foreach (UfCamCorrectionPoint refPoint in lstRefPoints)
        //    ////////{
        //    ////////    foreach(CrosstalkInfo info in m_lstCrosstalkInfo)
        //    ////////    {
        //    ////////        if(info.ControllerID == refPoint.Unit.ControllerID && info.X == refPoint.Unit.X && info.Y == refPoint.Unit.Y)
        //    ////////        {
        //    ////////            refPoint.PixelValueR /= (1.0f + info.Crosstalk[(int)refPoint.Pos][0]);
        //    ////////            refPoint.PixelValueG /= (1.0f + info.Crosstalk[(int)refPoint.Pos][1]);
        //    ////////            refPoint.PixelValueB /= (1.0f + info.Crosstalk[(int)refPoint.Pos][2]);
        //    ////////        }
        //    ////////    }
        //    ////////}
        //}


        // modified by Hotta 2024/10/11
        // ①スクリーン全体のModuleの、クロストーク補正値の有無(VDI)をチェックする
        // ②クロストーク補正値の有無の状況により、以下を行う
        // (a)クロストーク補正値あり/なしが混在
        //      → クロストーク補正値なしのModule : クロストーク補正値ありのModuleの平均値を適用
        //      → クロストーク補正値ありのModule : 書込みされているクロストーク補正値を適用
        // (b)全てクロストーク補正値あり
        //      → Moduleに記録されているクロストーク補正値を適用
        // (c)全てクロストーク補正値なし
        //      → 設計デフォルト値を適用
        void LoadCrosstalkValue(List<UnitInfo> lstUnit)
        {
            bool status;
            FileDirectory baseFileDir;

            if (allocInfo.LEDModel == ZRD_C12A || allocInfo.LEDModel == ZRD_C15A || allocInfo.LEDModel == ZRD_B12A || allocInfo.LEDModel == ZRD_B15A)
            {
                foreach (UnitInfo unit in lstUnit)
                {
                    MainWindow.CrosstalkInfo info = new MainWindow.CrosstalkInfo();
                    info.ControllerID = unit.ControllerID;
                    info.X = unit.X;
                    info.Y = unit.Y;
                    info.Crosstalk = new float[moduleCount][];
                    for (int n = 0; n < moduleCount; n++)
                    {
                        info.Crosstalk[n] = new float[3];
                        info.Crosstalk[n][0] = 0;
                        info.Crosstalk[n][1] = 0;
                        info.Crosstalk[n][2] = 0;
                    }
                    MainWindow.ListCrosstalkInfo.Add(info);
                }
            }
            else
            {
                // added by Hotta 2024/10/11
                // 補正対象だけでなく、全てのCabinetをチェックする
                bool existsCtc = false;
                bool notExistCtc = false;

                CrossTalkCorrectionHighColor ctcDefault = new CrossTalkCorrectionHighColor(allocInfo.LEDModel);

                CrossTalkCorrectionHighColor ctcAvg = new CrossTalkCorrectionHighColor();
                int ctcCount;

                existsCtc = false;
                notExistCtc = false;
                ctcCount = 0;
                ctcAvg.Red = ctcAvg.Green = ctcAvg.Blue = 0;
                foreach (List<UnitInfo> listUnit in allocInfo.lstUnits)
                {
                    status = checkDataFile(listUnit, out baseFileDir);
                    if (status != true)
                    { throw new Exception("The UF data file(hc.bin) was not found."); }

                    foreach(UnitInfo unit in listUnit)
                    {
                        // 対象hc.binのファイル名取得
                        string filePath = makeFilePath(unit, baseFileDir);

                        // クロストーク補正量の取得
                        m_MakeUFData.CorUncCrosstalk.Clear();
                        status = m_MakeUFData.GetUncCrosstalk(filePath, null);
                        if (!status)
                        { throw new Exception("Failed in crosstalk correction calc."); }

                        for (int n = 0; n < moduleCount; n++)
                        {
                            if (m_MakeUFData.CorUncCrosstalk[n + 1] == null)
                            {
                                notExistCtc = true;
                            }
                            else
                            {
                                existsCtc = true;
                                ctcAvg.Red += m_MakeUFData.CorUncCrosstalk[n + 1].Red;
                                ctcAvg.Green += m_MakeUFData.CorUncCrosstalk[n + 1].Green;
                                ctcAvg.Blue += m_MakeUFData.CorUncCrosstalk[n + 1].Blue;
                                ctcCount++;
                            }
                        }
                    }
                }
                if(existsCtc == true && ctcCount != 0)   // ctcCount != 0 は、0除算対策
                {
                    ctcAvg.Red /= ctcCount;
                    ctcAvg.Green /= ctcCount;
                    ctcAvg.Blue /= ctcCount;
                }
                //

                m_lstCrosstalkInfo = new List<CrosstalkInfo>();
                m_lstCrosstalkInfo.Clear();

                status = checkDataFile(lstUnit, out baseFileDir);
                if (status != true)
                { throw new Exception("The UF data file(hc.bin) was not found."); }

                foreach (UnitInfo unit in lstUnit)
                {
                    // 対象hc.binのファイル名取得
                    string filePath = makeFilePath(unit, baseFileDir);

                    // 対象hc.binからクロストーク補正量を取得
                    m_MakeUFData.CorUncCrosstalk.Clear();
                    // modified by Hotta 2024/10/11
                    /*
                    status = m_MakeUFData.GetUncCrosstalk(filePath, new CrossTalkCorrectionHighColor(allocInfo.LEDModel));
                    */
                    status = m_MakeUFData.GetUncCrosstalk(filePath, null);
                    //
                    if (!status)
                    {
                        throw new Exception("Failed in crosstalk correction calc.");
                    }

                    MainWindow.CrosstalkInfo info = new MainWindow.CrosstalkInfo();
                    info.ControllerID = unit.ControllerID;
                    info.X = unit.X;
                    info.Y = unit.Y;
                    info.Crosstalk = new float[moduleCount][];
                    for (int n = 0; n < moduleCount; n++)
                    {
                        info.Crosstalk[n] = new float[3];

                        // modified by Hotta 2024/10/11
                        /*
                        info.Crosstalk[n][0] = (float)m_MakeUFData.CorUncCrosstalk[n+1].Red;
                        info.Crosstalk[n][1] = (float)m_MakeUFData.CorUncCrosstalk[n+1].Green;
                        info.Crosstalk[n][2] = (float)m_MakeUFData.CorUncCrosstalk[n+1].Blue;
                        */

                        // クロストーク補正あり/なしのModuleが混在
                        if(existsCtc == true && notExistCtc == true)
                        {
                            if(m_MakeUFData.CorUncCrosstalk[n + 1] != null)
                            {
                                info.Crosstalk[n][0] = (float)m_MakeUFData.CorUncCrosstalk[n + 1].Red;
                                info.Crosstalk[n][1] = (float)m_MakeUFData.CorUncCrosstalk[n + 1].Green;
                                info.Crosstalk[n][2] = (float)m_MakeUFData.CorUncCrosstalk[n + 1].Blue;
                            }
                            else
                            {
                                info.Crosstalk[n][0] = (float)ctcAvg.Red;
                                info.Crosstalk[n][1] = (float)ctcAvg.Green;
                                info.Crosstalk[n][2] = (float)ctcAvg.Blue;
                            }
                        }
                        // 全てのModuleに、クロストーク補正量がある場合
                        else if (existsCtc == true && notExistCtc == false)
                        {
                            info.Crosstalk[n][0] = (float)m_MakeUFData.CorUncCrosstalk[n + 1].Red;
                            info.Crosstalk[n][1] = (float)m_MakeUFData.CorUncCrosstalk[n + 1].Green;
                            info.Crosstalk[n][2] = (float)m_MakeUFData.CorUncCrosstalk[n + 1].Blue;
                        }
                        // 全てのModuleに、クロストーク補正量がない場合
                        else if (existsCtc == false && notExistCtc == true)
                        {
                            info.Crosstalk[n][0] = (float)ctcDefault.Red;
                            info.Crosstalk[n][1] = (float)ctcDefault.Green;
                            info.Crosstalk[n][2] = (float)ctcDefault.Blue;
                        }
                        // ありえない組み合わせ
                        else
                        {
                            throw new Exception("Failed in get crosstalk correction.");
                        }
                        //
                    }
                    MainWindow.ListCrosstalkInfo.Add(info);
                }
            }
        }

        void saveUfLog(string log)
        {
            string str;
            using (StreamWriter sw = new StreamWriter(m_CamUfMeasPath + "log.txt", true))
            {
                DateTime now = DateTime.Now;
                str = string.Format("{0:D4}/{1:D2}/{2:D2},{3:D2}:{4:D2}:{5:D2}.{6},{7}", now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Millisecond, log);
                sw.WriteLine(str);
            }

        }
#endif

        #endregion Adjust

        #endregion Private Methods


        #region Remain Time Fields

        int UF_RE_CALC_STEP_3_PROCESS_SEC = 103;

        int UF_INITIAL_SETTINGS_SEC = 6;
        int USER_SETTING_SEC = 1;
        int ADJUST_SETTING_SEC = 1;
        int AUTO_FOCUS_SEC = 15;
        int STORE_CAMERA_POSITION_SEC = 8;
        int SHOW_RESULT_SEC = 2;

        // 撮影
        int UF_CAPTURE_BLACK_IMAGE_SEC = 7;
        int UF_CAPTURE_REFERENCE_POSITION_IMAGE_SEC = 7;
        int UF_CAPTURE_REFERENCE_POSITION_MASK_IMAGE_SEC = 7;
        int UF_CAPTURE_MASK_IMAGE_SEC = 7;

        double UF_MEASURE_CAPTURE_MODULE_IMAGE_SEC = 6.6;
        double UF_ADJUST_CAPTURE_MODULE_IMAGE_SEC = 7.6;
        int UF_CAPTURE_POSITION_IMAGE_SEC = 7;
        double UF_CAPTURE_FLAT_IMAGE_SEC = 6.7;

        int UF_GET_FLAT_IMAGE_SEC = 52;

        int UF_GENERATE_MASK_IMAGE_SEC = 1;
        double UF_LOAD_MODULE_IMAGE_SEC = 0.7;
        double UF_LOAD_POSITION_IMAGE_SEC = 0.9;
        int UF_LOAD_REFERENCE_IMAGE_SEC = 1;
        double UF_LOAD_REFERENCE_POSITION_IMAGE_SEC = 2.1;
        double UF_CALC_TRIMMING_AREA_SEC = 0.07; // Module単位
        double UF_LOAD_RAW_PICTURE_SEC = 0.4;

        int UNIT_POWER_ON_SEC = 11;

        double UF_MOVE_ADJUSTED_UF_DATA_SEC = 1.45;
        int UNIT_POWER_OFF_SEC = 5;
        int UNIT_RECONFIG_SEC = 20;

        double UF_LOAD_CROSSTALK_VALUE_SEC = 0.05;

        // ZRD-B12A/C12A
        double UF_9POINT_CREATE_HC_FILE_MODULE4X2_P12_SEC = 1.8;
        double UF_EACH_MODULE_CREATE_HC_FILE_MODULE4X2_P12_SEC = 4.1;
        double UF_RADIATOR_CREATE_HC_FILE_MODULE4X2_P12_SEC = 1.8;

        double UF_READ_ADJUST_UF_DATA_MODULE4X2_P12_SEC = 5.4;
        int UF_WRITE_ADJUST_UF_DATA_MODULE4X2_P12_SEC = 47;

        // ZRD-B15A/C15A
        double UF_9POINT_CREATE_HC_FILE_MODULE4X2_P15_SEC = 1.8;
        double UF_EACH_MODULE_CREATE_HC_FILE_MODULE4X2_P15_SEC = 4.1;
        double UF_RADIATOR_CREATE_HC_FILE_MODULE4X2_P15_SEC = 1.8;

        double UF_READ_ADJUST_UF_DATA_MODULE4X2_P15_SEC = 3.7;
        int UF_WRITE_ADJUST_UF_DATA_MODULE4X2_P15_SEC = 31;

        // ZRD-BH12D/CH12D
        double UF_9POINT_CREATE_HC_FILE_MODULE4X3_P12_SEC = 2.5;
        double UF_EACH_MODULE_CREATE_HC_FILE_MODULE4X3_P12_SEC = 8.3;
        double UF_RADIATOR_CREATE_HC_FILE_MODULE4X3_P12_SEC = 2.8;

        double UF_READ_ADJUST_UF_DATA_MODULE4X3_P12_SEC = 5.3;
        int UF_WRITE_ADJUST_UF_DATA_MODULE4X3_P12_SEC = 37;

        // ZRD-BH15D/CH15D
        double UF_9POINT_CREATE_HC_FILE_MODULE4X3_P15_SEC = 2.1;
        double UF_EACH_MODULE_CREATE_HC_FILE_MODULE4X3_P15_SEC = 4.9;
        double UF_RADIATOR_CREATE_HC_FILE_MODULE4X3_P15_SEC = 2.3;

        double UF_READ_ADJUST_UF_DATA_MODULE4X3_P15_SEC = 3.6;
        int UF_WRITE_ADJUST_UF_DATA_MODULE4X3_P15_SEC = 35;

        int UF_CAPTURE_FLAT_IMAGE_COUNT = 4;
        int UF_LOAD_RAW_PICTURE_COUNT = 4;
        int UF_CAPTURE_9_POSITION_IMAGE_COUNT = 9;
        int UF_LOAD_9_POSITION_IMAGE_COUNT = 9;
        int UF_CAPTURE_4_POSITION_IMAGE_COUNT = 4;
        int UF_LOAD_4_POSITION_IMAGE_COUNT = 4;


        double m_Uf9PointCreateHcFileSec;
        double m_UfEachModuleCreateHcFileSec;
        double m_UfRadiatorCreateHcFileSec;
        int m_UfLoadCrosstalkValueSec;
        double m_ReadUfDataSec;
        int m_WriteUfDataSec;

        #endregion Remain Time Fields


        #region Remain Time Private Methods

        /// <summary>
        /// Measurement処理にかかる推定時間を算出
        /// <param name="cabinetCount">選択Cabinet数</param>
        /// </summary>
        private int initialUfCameraMeasurementProcessSec(int cabinetCount)
        {
            //process[0]  初期設定
            int _step0 = UF_INITIAL_SETTINGS_SEC
                        + USER_SETTING_SEC
                        + ADJUST_SETTING_SEC;
            //process[1]  Auto Focus
            int _step1 = AUTO_FOCUS_SEC;
            //process[2]  開始時のカメラ位置を保存
            int _step2 = STORE_CAMERA_POSITION_SEC;
            //process[3]  撮影
            //process[3.1]  Capture Black Image
            //process[3.2]  Capture Mask Image
            //process[3.3]  Capture Module-1~8/12 Image
            //process[3.4]  Capture Flat Image
            int _step3 = UF_CAPTURE_BLACK_IMAGE_SEC
                        + UF_CAPTURE_MASK_IMAGE_SEC
                        + (int)Math.Round(UF_MEASURE_CAPTURE_MODULE_IMAGE_SEC * moduleCount)
                        + (int)Math.Round(UF_CAPTURE_FLAT_IMAGE_SEC * UF_CAPTURE_FLAT_IMAGE_COUNT); // R,G,B,W
            //process[4]  処理
            int _step4 = UF_GENERATE_MASK_IMAGE_SEC
                        + (int)Math.Round(UF_LOAD_MODULE_IMAGE_SEC * moduleCount)
                        + (int)Math.Round(UF_CALC_TRIMMING_AREA_SEC * moduleCount * cabinetCount)
                        + (int)Math.Round(UF_LOAD_RAW_PICTURE_SEC * UF_LOAD_RAW_PICTURE_COUNT); // R,G,B,B(Black)
            //process[5]   
            //process[5.1]  終了時のカメラ位置を保存
            //process[5.2]  調整用設定を解除
            //process[5.3]  ユーザー設定に書き戻し
            int _step5 = STORE_CAMERA_POSITION_SEC
                        + ADJUST_SETTING_SEC
                        + USER_SETTING_SEC
                        + SHOW_RESULT_SEC;

            m_AryProcessSec = new int[6] { _step0, _step1, _step2, _step3, _step4, _step5 };
            int processSec = m_AryProcessSec.Sum();
            saveUfLog($"MeasurementProcessSec: process[0]:{_step0} process[1]:{_step1} process[2]:{_step2} process[3]:{_step3} process[4]:{_step4} process[5]:{_step5}");

            return processSec;
        }

        /// <summary>
        /// MeasurementのRemainTimeを算出
        /// </summary>
        /// <param name="step"></param>
        /// <returns></returns>
        private int calcUfCameraMeasurementProcessSec(int step)
        {
            int processSec = m_AryProcessSec.Skip(step).Sum();
            saveUfLog("UfCameraMeasurementProcessSec step:" + step + " stepSec:" + m_AryProcessSec[step] + " processSec:" + processSec);

            return processSec;
        }

        /// <summary>
        /// Adjustment処理にかかる推定時間を算出
        /// </summary>
        /// <param name="count"></param>
        private int initialUfCameraAdjustmentProcessSec(int count)
        {
            if (allocInfo.LEDModel == ZRD_B12A 
                || allocInfo.LEDModel == ZRD_C12A)
            {
#if (DEBUG || Release_log)
                if (Settings.Ins.ExecLog == true)
                {
                    MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                }
#endif
                m_Uf9PointCreateHcFileSec = UF_9POINT_CREATE_HC_FILE_MODULE4X2_P12_SEC;
                m_UfEachModuleCreateHcFileSec = UF_EACH_MODULE_CREATE_HC_FILE_MODULE4X2_P12_SEC;
                m_UfRadiatorCreateHcFileSec = UF_RADIATOR_CREATE_HC_FILE_MODULE4X2_P12_SEC;
                m_UfLoadCrosstalkValueSec = 0;
                m_ReadUfDataSec = UF_READ_ADJUST_UF_DATA_MODULE4X2_P12_SEC;
                m_WriteUfDataSec = UF_WRITE_ADJUST_UF_DATA_MODULE4X2_P12_SEC;
            }
            else if (allocInfo.LEDModel == ZRD_B15A 
                || allocInfo.LEDModel == ZRD_C15A)
            {
#if (DEBUG || Release_log)
                if (Settings.Ins.ExecLog == true)
                {
                    MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                }
#endif
                m_Uf9PointCreateHcFileSec = UF_9POINT_CREATE_HC_FILE_MODULE4X2_P15_SEC;
                m_UfEachModuleCreateHcFileSec = UF_EACH_MODULE_CREATE_HC_FILE_MODULE4X2_P15_SEC;
                m_UfRadiatorCreateHcFileSec = UF_RADIATOR_CREATE_HC_FILE_MODULE4X2_P15_SEC;
                m_UfLoadCrosstalkValueSec = 0;
                m_ReadUfDataSec = UF_READ_ADJUST_UF_DATA_MODULE4X2_P15_SEC;
                m_WriteUfDataSec = UF_WRITE_ADJUST_UF_DATA_MODULE4X2_P15_SEC;
            }
            else if (allocInfo.LEDModel == ZRD_BH12D 
                || allocInfo.LEDModel == ZRD_CH12D
                || allocInfo.LEDModel == ZRD_BH12D_S3
                || allocInfo.LEDModel == ZRD_CH12D_S3)
            {
#if (DEBUG || Release_log)
                if (Settings.Ins.ExecLog == true)
                {
                    MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                }
#endif
                m_Uf9PointCreateHcFileSec = UF_9POINT_CREATE_HC_FILE_MODULE4X3_P12_SEC;
                m_UfEachModuleCreateHcFileSec = UF_EACH_MODULE_CREATE_HC_FILE_MODULE4X3_P12_SEC;
                m_UfRadiatorCreateHcFileSec = UF_RADIATOR_CREATE_HC_FILE_MODULE4X3_P12_SEC;
                m_UfLoadCrosstalkValueSec = (int)Math.Round(UF_LOAD_CROSSTALK_VALUE_SEC * count);
                m_ReadUfDataSec = UF_READ_ADJUST_UF_DATA_MODULE4X3_P12_SEC;
                m_WriteUfDataSec = UF_WRITE_ADJUST_UF_DATA_MODULE4X3_P12_SEC;
            }
            else if (allocInfo.LEDModel == ZRD_BH15D 
                || allocInfo.LEDModel == ZRD_CH15D
                || allocInfo.LEDModel == ZRD_BH15D_S3
                || allocInfo.LEDModel == ZRD_CH15D_S3)
            {
#if (DEBUG || Release_log)
                if (Settings.Ins.ExecLog == true)
                {
                    MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                }
#endif
                m_Uf9PointCreateHcFileSec = UF_9POINT_CREATE_HC_FILE_MODULE4X3_P15_SEC;
                m_UfEachModuleCreateHcFileSec = UF_EACH_MODULE_CREATE_HC_FILE_MODULE4X3_P15_SEC;
                m_UfRadiatorCreateHcFileSec = UF_RADIATOR_CREATE_HC_FILE_MODULE4X3_P15_SEC;
                m_UfLoadCrosstalkValueSec = (int)Math.Round(UF_LOAD_CROSSTALK_VALUE_SEC * count);
                m_ReadUfDataSec = UF_READ_ADJUST_UF_DATA_MODULE4X3_P15_SEC;
                m_WriteUfDataSec = UF_WRITE_ADJUST_UF_DATA_MODULE4X3_P15_SEC;
            }

            //process[0]  初期設定
            int _step0 = UF_INITIAL_SETTINGS_SEC
                        + USER_SETTING_SEC
                        + ADJUST_SETTING_SEC;
            //process[1]  Auto Focus
            int _step1 = AUTO_FOCUS_SEC;
            //process[2]  開始時のカメラ位置を保存
            int _step2 = STORE_CAMERA_POSITION_SEC;
            //process[3]  測定エリアの取得 + クロストーク補正量取得 + 測定用画像(RAW)の取得・平均値の格納 + バイナリファイル作成
            int _step3 = UF_CAPTURE_BLACK_IMAGE_SEC
                        + UF_CAPTURE_MASK_IMAGE_SEC
                        + UF_CAPTURE_REFERENCE_POSITION_IMAGE_SEC
                        + UF_GENERATE_MASK_IMAGE_SEC
                        + m_UfLoadCrosstalkValueSec
                        + UF_GET_FLAT_IMAGE_SEC;

            // Adjustment Mode
            if (rbUfCam9pt.IsChecked == true)
            {
                _step3 += UF_CAPTURE_POSITION_IMAGE_SEC * UF_CAPTURE_9_POSITION_IMAGE_COUNT // LT,T,RT,L,C,R,LB,B,RB
                         + (int)Math.Round(UF_LOAD_POSITION_IMAGE_SEC * UF_LOAD_9_POSITION_IMAGE_COUNT)
                         + (int)Math.Round(m_Uf9PointCreateHcFileSec * count);
            }
            else if (rbUfCamEachMod.IsChecked == true)
            {
                _step3 += (int)Math.Round(UF_ADJUST_CAPTURE_MODULE_IMAGE_SEC * moduleCount)
                         + (int)Math.Round(UF_LOAD_MODULE_IMAGE_SEC * moduleCount)
                         + (int)Math.Round(m_UfEachModuleCreateHcFileSec * count);
            }
            else if (rbUfCamRadiator.IsChecked == true)
            {
                _step3 += UF_CAPTURE_POSITION_IMAGE_SEC * UF_CAPTURE_4_POSITION_IMAGE_COUNT // T,L,R,B
                         + (int)Math.Round(UF_LOAD_POSITION_IMAGE_SEC * UF_LOAD_4_POSITION_IMAGE_COUNT)
                         + (int)Math.Round(m_UfRadiatorCreateHcFileSec * count);
            }

            // Base CabinetのLine選択の場合
            if (rbUfCamTgtCabiLine.IsChecked == true)
            {
                int checkCount = 0;
                if (cbUfCamTgtCabiTop.IsChecked == true)
                { checkCount++; }

                if (cbUfCamTgtCabiBottom.IsChecked == true)
                { checkCount++; }

                if (cbUfCamTgtCabiLeft.IsChecked == true)
                { checkCount++; }

                if (cbUfCamTgtCabiRight.IsChecked == true)
                { checkCount++; }

                _step3 += (int)Math.Round(UF_CAPTURE_REFERENCE_POSITION_MASK_IMAGE_SEC
                         + UF_CAPTURE_MASK_IMAGE_SEC
                         + UF_LOAD_REFERENCE_POSITION_IMAGE_SEC) * checkCount;
            }
            else
            {
                _step3 += UF_LOAD_REFERENCE_IMAGE_SEC;
            }

            //process[4]  Write Adjusted UF Data
            int _step4 = (int)Math.Round(UF_MOVE_ADJUSTED_UF_DATA_SEC * count)
                        + UNIT_POWER_OFF_SEC
                        + UNIT_RECONFIG_SEC * 2 // 書き込み処理前と後
                        + (int)Math.Round(m_ReadUfDataSec * count)
                        + m_WriteUfDataSec;
            //process[5]  Cabinet Power On
            int _step5 = UNIT_POWER_ON_SEC;

            //process[6]  
            int _step6 = STORE_CAMERA_POSITION_SEC
                        + ADJUST_SETTING_SEC
                        + USER_SETTING_SEC
                        + SHOW_RESULT_SEC;
            //process[7]
            int _step7 = cbUfCamMeasResult.IsChecked == true
                       ? initialUfCameraMeasurementProcessSec(count)
                       : 0;

            m_AryProcessSec = new int[8] { _step0, _step1, _step2, _step3, _step4, _step5, _step6, _step7 };
            int processSec = m_AryProcessSec.Sum();
            saveUfLog($"AdjustmentProcessSec: process[0]:{_step0} process[1]:{_step1} process[2]:{_step2} process[3]:{_step3} process[4]:{_step4} process[5]:{_step5} process[6]:{_step6} process[7]:{_step7}");

            return processSec;
        }

        /// <summary>
        /// Adjustment機能の残りRemainTimeを算出
        /// </summary>
        /// <param name="step">RemainTimeの更新する箇所</param>
        /// <param name="count">ファイル数</param>
        /// <returns></returns>
        private int calcUfCameraAdjustmentProcessSec(int step, int count = 0)
        {
            // HCファイル生成対象Cabinetで残り時間再計算
            if (step == UF_RE_CALC_STEP_3_PROCESS_SEC)
            {
                int _step3 = m_UfLoadCrosstalkValueSec
                            + UF_GET_FLAT_IMAGE_SEC;

                if (rbUfCam9pt.IsChecked == true)
                { _step3 += (int)Math.Round(m_Uf9PointCreateHcFileSec * count); }　// count:生成するファイル数
                else if (rbUfCamEachMod.IsChecked == true)
                { _step3 += (int)Math.Round(m_UfEachModuleCreateHcFileSec * count); }
                else if (rbUfCamRadiator.IsChecked == true)
                { _step3 += (int)Math.Round(m_UfRadiatorCreateHcFileSec * count); }

                m_AryProcessSec[3] = _step3;
                step = 3; // 残り時間の算出ためのstep戻し
            }

            // 書き込み対象Cabinetで残り時間再計算
            if (step == 4)
            {
                int _step4 = (int)Math.Round(UF_MOVE_ADJUSTED_UF_DATA_SEC * count)
                            + UNIT_POWER_OFF_SEC
                            + UNIT_RECONFIG_SEC * 2
                            + (int)Math.Round(m_ReadUfDataSec * count) // count:書き込みファイル数
                            + m_WriteUfDataSec;

                m_AryProcessSec[step] = _step4;
            }

            int processSec = m_AryProcessSec.Skip(step).Sum();
            saveUfLog("UfCameraAdjustmentProcessSec step:" + step + " stepSec:" + m_AryProcessSec[step] + " processSec:" + processSec);

            return processSec;
        }

        #endregion Remain Time Private Methods
    }
}
