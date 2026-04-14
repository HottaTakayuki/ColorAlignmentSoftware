#define OutputOnlyGreen
// added by Hotta 2022/01/19
#define ImageScale_x5
//

//#define TempFile

#define NoEncript

// added by Hotta 2022/05/09 for あおり撮影
#define CaptureTilt


// added by Hotta 20225/05/24 for カメラ位置決め
#define CameraPosition
//#define Cabi_4x4


// added by Hotta 2022/06/27 for 回転を考慮した計算範囲
#define RotateRect

// added by Hotta 2022/07/06 for 対象の端の目地も補正
#define CorrectTargetEdge

// added by Hotta 2022/07/11 for 補正データのModule一括書き込み
#define BulkSetCorrectValue

// added by Hotta 2022/07/13 for Coverity
#define Coverity

// added by Hotta 2022/09/15 for 内蔵パターン表示遅い
#define ShowPattern_CamPos

// added by Hotta 2022/10/04 for 映り込み
#define Reflection

// added by Hotta 2022/10/26 for 複数コントローラ
#define MultiController

// adde by Hotta 2022/10/31 for 複数コントローラ　テスト : 通常は無効
//#define MultiControllerTest

// added by Hotta 2022/11/10 for Cabinet補正値の取り扱い中止
#define No_CabinetCorrectionValue

// added by Hotta 2022/11/14 for 外部入力されたCabinet配置
#define FlexibleCabinetPosition

// added by Hotta 2022/11/24 for 補正が完了したら、確認メッセージ表示し、そのままWriteDataを実行
#define Auto_WriteData

// added by Hotta 2022/11/28 for 撮影した画像が、直前の画像と変化がなかったら、指定された回数だけ自動リトライ
#define Retry_NotChangeLastImage

// added by Hotta 2022/11/28 for カメラ位置推定の対象範囲が小さい場合の対策
#define CamPos_TargetArea_Small

// added by Hotta 2022/12/13 for 規格をZ距離に応じて変更
#define Spec_by_Zdistance



using AcquisitionARW;
using CameraDataClass;
// added by Hotta 2021/09/09
using DigitalFourierTransform;
using OpenCvSharp;
using OpenCvSharp.Blob;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Application = System.Windows.Application;
using Bitmap = System.Drawing.Bitmap;
using Color = System.Drawing.Color;
using Pen = System.Drawing.Pen;
using Size = OpenCvSharp.Size;

namespace CAS
{
    public partial class MainWindow : System.Windows.Window
    {
        #region Constants

        // 撮影画像ファイル（拡張子なし）
        const string _fn_AreaFile = "MeasArea";

        const string fn_BlackFile = "Black";

        const string fn_WhiteBeforeFile = "WhiteBefore";
        const string fn_WhiteResultFile = "WhiteResult";
        const string fn_WhiteMeasureFile = "WhiteMeasure";

        const string fn_Flat = "Flat";

        const string fn_Top = "Top";
        const string fn_Right = "Right";

        const string fn_GapPos = "GapPos";

        const string fn_MoireArea = "MoireArea";
        const string fn_MoireCheck = "MoireCheck";

        const string fn_AutoFocus = "AutoFocus";

        const int GapTrimmingAreaMin = 50;
        const int GapTrimmingAreaMax = 5000;

        const int CaptureNum = 3;
        //const int CaptureNum = 1;

        int m_MeasureLevel;

        ShootCondition m_ShootCondition;

        #endregion Constants

        #region Fields

        private List<GapCamCorrectionValue> lstGapCamCp;

        public enum GapStatus { Before, Result, Measure };  // Before : 補正前（±0）、Result : 補正後、Measure : 計測のみ
        public GapStatus m_GapStatus;

        // added by Hotta 2022/05/26 for カメラ位置
#if CameraPosition
        //Point3f[] m_PanelCorner;
        float[] m_Transration;
        float[] m_Rotate;
        float[] m_ShiftToCameraCoordinate;
        class CameraParameter
        {
            public float f;
            public float SensorSizeH;
            public float SensorSizeV;
            public int SensorResH;
            public int SensorResV;

            public CameraParameter(float f, float SensorSizeH, float SensorSizeV, int SensorResH, int SensorResV)
            {
                this.f = f;
                this.SensorSizeH = SensorSizeH;
                this.SensorSizeV = SensorSizeV;
                this.SensorResH = SensorResH;
                this.SensorResV = SensorResV;
            }
        }
        CameraParameter m_CameraParam;

        // deleted by Hotta 2023/03/03
        /*
        float m_LedPitch;
        */

        // added by Hotta 2023/04/28
#if NO_CONTROLLER
        float m_LedPitch;
#endif

        int m_ModuleDx, m_ModuleDy;
        int m_CabinetDx, m_CabinetDy;
        int m_PanelDx, m_PanelDy;

        int m_ModuleXNum, m_ModuleYNum;
        int m_CabinetXNum, m_CabinetYNum;
        //added bby Hotta 2022/07/29
        int m_CabinetXNum_CamPos, m_CabinetYNum_CamPos;


#endif

        // deleted by Hotta 2023/03/03
        /*
        bool m_CalcCameraPosition = false;
        */

        // added by Hotta 2022/06/22 for カメラ位置
        List<UnitInfo> m_lstCamPosUnits;


        // added by Hotta 2022/12/13
#if Spec_by_Zdistance
        double[][] m_Spec_by_Zdistance;
#endif


#endregion Fields

        #region Events

        private void cmbxGapCamCamera_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                cmbxUfCamCamera.SelectedIndex = cmbxGapCamCamera.SelectedIndex;
            }
            catch (Exception ex)
            {
                WindowMessage winMessage = new WindowMessage(ex.Message, "Camera Connect Error!!");
                winMessage.ShowDialog();
                return;
            }
        }

        private void cmbxGapCamLensCd_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            { cmbxUfCamLensCd.SelectedIndex = cmbxGapCamLensCd.SelectedIndex; }
            catch (Exception ex)
            {
                WindowMessage winMessage = new WindowMessage(ex.Message, "Camera Connect Error!!");
                winMessage.ShowDialog();
                return;
            }
        }

        private async void btnGapCamBackup_Click(object sender, RoutedEventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.FileName = "";

                if (string.IsNullOrWhiteSpace(Settings.Ins.GapCam.LastBackupFile) == false)
                { sfd.InitialDirectory = System.IO.Path.GetDirectoryName(Settings.Ins.GapCam.LastBackupFile); }
                else
                { sfd.InitialDirectory = "C:\\"; }

                sfd.Filter = "XML File(*.xml)|*.xml|All File(*.*)|*.*";
                sfd.FilterIndex = 1;
                sfd.Title = "Please select a save file name.";
                sfd.RestoreDirectory = true;
                sfd.OverwritePrompt = true;
                sfd.CheckPathExists = true;

                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    bool status = true;

                    Settings.Ins.GapCam.LastBackupFile = sfd.FileName;

                    tcMain.IsEnabled = false;
                    actionButton(sender, "Backup Gap Correction Values Start.");

                    string msg = "";

                    winProgress = new WindowProgress("Backup Gap Correction Values Progress");
                    winProgress.Show();

                    try { await Task.Run(() => backupGapRegAsync(sfd.FileName)); }
                    catch (Exception ex)
                    {
                        ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                        status = false;
                    }

                    msg = "";
                    string caption = "";
                    if (status == true)
                    {
                        msg = "Backup Gap Correction Values Complete!";
                        caption = "Complete";
                    }
                    else
                    {
                        msg = "Failed in Backup Gap Correction Values.";
                        caption = "Error";
                    }

                    winProgress.Close();

                    playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
                    WindowMessage winMessage = new WindowMessage(msg, caption);
                    winMessage.ShowDialog();

                    releaseButton(sender, "Backup Gap Correction Values Done.");
                    tcMain.IsEnabled = true;
                }
            }
        }

        private async void btnGapCamRestore_Click(object sender, RoutedEventArgs e)
        {
            // modified by Hotta 2022/07/12 for 一括書き込み
#if BulkSetCorrectValue
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if (string.IsNullOrWhiteSpace(Settings.Ins.GapCam.LastBackupFile) == false)
                {
                    ofd.InitialDirectory = System.IO.Path.GetDirectoryName(Settings.Ins.GapCam.LastBackupFile);
                    ofd.FileName = System.IO.Path.GetFileName(Settings.Ins.GapCam.LastBackupFile);
                }
                else
                {
                    ofd.InitialDirectory = "C:\\";
                    ofd.FileName = "";
                }
                ofd.Filter = "XML Files(*.xml)|*.xml|All Files(*.*)|*.*";
                ofd.FilterIndex = 1;
                ofd.Title = "Please select Gap Backup File.";
                ofd.RestoreDirectory = true;
                ofd.CheckFileExists = true;
                ofd.CheckPathExists = true;

                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    bool status = true;

                    tcMain.IsEnabled = false;
                    actionButton(sender, "Restore(bulk) Gap Correction Values Start.");

                    string msg = "";

                    winProgress = new WindowProgress("Restore(bulk) Gap Correction Values Progress");
                    winProgress.Show();

                    try { await Task.Run(() => restoreBulkGapRegAsync(ofd.FileName)); }
                    catch (Exception ex)
                    {
                        ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                        status = false;
                    }

                    msg = "";
                    string caption = "";
                    if (status == true)
                    {
                        msg = "Restore(bulk) Gap Correction Values Complete!";
                        caption = "Complete";
                    }
                    else
                    {
                        msg = "Failed in Restore(bulk) Gap Correction Values.";
                        caption = "Error";
                    }

                    winProgress.Close();

                    playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
                    WindowMessage winMessage = new WindowMessage(msg, caption);
                    winMessage.ShowDialog();

                    releaseButton(sender, "Restore(bulk) Gap Correction Values Done.");
                    tcMain.IsEnabled = true;
                }
            }
#else
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if (string.IsNullOrWhiteSpace(Settings.Ins.GapCam.LastBackupFile) == false)
                {
                    ofd.InitialDirectory = System.IO.Path.GetDirectoryName(Settings.Ins.GapCam.LastBackupFile);
                    ofd.FileName = System.IO.Path.GetFileName(Settings.Ins.GapCam.LastBackupFile);
                }
                else
                {
                    ofd.InitialDirectory = "C:\\";
                    ofd.FileName = "";
                }
                ofd.Filter = "XML Files(*.xml)|*.xml|All Files(*.*)|*.*";
                ofd.FilterIndex = 1;
                ofd.Title = "Please select Gap Backup File.";
                ofd.RestoreDirectory = true;
                ofd.CheckFileExists = true;
                ofd.CheckPathExists = true;

                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    bool status = true;

                    tcMain.IsEnabled = false;
                    actionButton(sender, "Restore Gap Correction Values Start.");

                    string msg = "";

                    winProgress = new WindowProgress("Restore Gap Correction Values Progress");
                    winProgress.Show();

                    try { await Task.Run(() => restoreGapRegAsync(ofd.FileName)); }
                    catch (Exception ex)
                    {
                        ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                        status = false;
                    }

                    msg = "";
                    string caption = "";
                    if (status == true)
                    {
                        msg = "Restore Gap Correction Values Complete!";
                        caption = "Complete";
                    }
                    else
                    {
                        msg = "Failed in Restore Gap Correction Values.";
                        caption = "Error";
                    }

                    winProgress.Close();

                    playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
                    WindowMessage winMessage = new WindowMessage(msg, caption);
                    winMessage.ShowDialog();

                    releaseButton(sender, "Restore Gap Correction Values Done.");
                    tcMain.IsEnabled = true;
                }
            }
#endif
        }


        private async void btnGapCamRestoreBulk_Click(object sender, RoutedEventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if (string.IsNullOrWhiteSpace(Settings.Ins.GapCam.LastBackupFile) == false)
                {
                    ofd.InitialDirectory = System.IO.Path.GetDirectoryName(Settings.Ins.GapCam.LastBackupFile);
                    ofd.FileName = System.IO.Path.GetFileName(Settings.Ins.GapCam.LastBackupFile);
                }
                else
                {
                    ofd.InitialDirectory = "C:\\";
                    ofd.FileName = "";
                }
                ofd.Filter = "XML Files(*.xml)|*.xml|All Files(*.*)|*.*";
                ofd.FilterIndex = 1;
                ofd.Title = "Please select Gap Backup File.";
                ofd.RestoreDirectory = true;
                ofd.CheckFileExists = true;
                ofd.CheckPathExists = true;

                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    bool status = true;

                    tcMain.IsEnabled = false;
                    actionButton(sender, "Restore(bulk) Gap Correction Values Start.");

                    string msg = "";

                    winProgress = new WindowProgress("Restore(bulk) Gap Correction Values Progress");
                    winProgress.Show();

                    try { await Task.Run(() => restoreBulkGapRegAsync(ofd.FileName)); }
                    catch (Exception ex)
                    {
                        ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                        status = false;
                    }

                    msg = "";
                    string caption = "";
                    if (status == true)
                    {
                        msg = "Restore(bulk) Gap Correction Values Complete!";
                        caption = "Complete";
                    }
                    else
                    {
                        msg = "Failed in Restore(bulk) Gap Correction Values.";
                        caption = "Error";
                    }

                    winProgress.Close();

                    playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
                    WindowMessage winMessage = new WindowMessage(msg, caption);
                    winMessage.ShowDialog();

                    releaseButton(sender, "Restore(bulk) Gap Correction Values Done.");
                    tcMain.IsEnabled = true;
                }
            }
        }

        unsafe private void tbtnGapCamSetPos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                playSound(applicationPath + "\\Components\\Sound\\button70.mp3");
                // 信号レベルの設定
                if (allocInfo.LEDModel == ZRD_C12A
                    || allocInfo.LEDModel == ZRD_C15A
                    || allocInfo.LEDModel == ZRD_CH12D
                    || allocInfo.LEDModel == ZRD_CH15D
                    || allocInfo.LEDModel == ZRD_CH12D_S3
                    || allocInfo.LEDModel == ZRD_CH15D_S3
                    )
                {
#if (DEBUG || Release_log)
                    if (Settings.Ins.ExecLog == true)
                    {
                        MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                    }
#endif
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
                    m_MeasureLevel = Settings.Ins.GapCam.MeasLevel_BModel;
                }

                // カメラパラメータの設定
                if (Settings.Ins.Camera.Name == "ILCE-6400")
                    m_ShootCondition = Settings.Ins.GapCam.Setting_A6400;
                else
                    m_ShootCondition = Settings.Ins.GapCam.Setting_A7;

                if (tbtnGapCamSetPos.IsChecked == true)
                {
                    List<UnitInfo> lstTgtUnits;

                    txtbStatus.Text = "Setting Camera Position...";
                    doEvents();

                    // Unitが選択されているか、矩形になっているか確認
                    try
                    {
                        // modified by Hotta 2022/06/22 for カメラ位置
                        //CheckSelectedUnits(aryUnitGapCam, out lstTgtUnits);
                        // modified by Hotta 2022/12/02
                        //CheckSelectedUnits(aryUnitGapCam, out lstTgtUnits, true, out m_lstCamPosUnits);
                        CheckSelectedUnits(aryUnitGapCam, out lstTgtUnits, true, out m_lstCamPosUnits, true);
                    }
                    catch (Exception ex)
                    {
                        ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);

                        // modified by Hotta 2022/11/24
                        //tbtnUfCamSetPos.IsChecked = false;
                        tbtnGapCamSetPos.IsChecked = false;

                        // タブの表示ページを切り替え
                        tcGapCamView.SelectedIndex = 0;
                        return;
                    }

                    // deleted by Hotta 2022/12/14
                    // SetCamPosTarget()で、Z距離で判定する
                    /*

                    //--------------------------------------------------------------------
                    // added by Hotta 2022/11/24
                    // カメラ/Wallの設定が、補正可能な内容かどうかチェック

                    // 検査スペック（暫定）
                    double spec = 0;
                    if (allocInfo.LEDModel == ZRD_C12A || allocInfo.LEDModel == ZRD_B12A || allocInfo.LEDModel == ZRD_CH12D || allocInfo.LEDModel == ZRD_BH12D)
                        spec = 10000;
                    else
                        spec = 10000 * 1.58 / 1.28;


                    // Cabinet座標の算出
                    SetCabinetPos(lstTgtUnits, 0);

                    int startX = int.MaxValue;
                    int endX = int.MinValue;
                    int startY = int.MaxValue;
                    int endY = int.MinValue;
                    foreach (UnitInfo unit in lstTgtUnits)
                    {
                        if (startX > unit.X)
                            startX = unit.X;
                        if (endX < unit.X)
                            endX = unit.X;
                        if (startY > unit.Y)
                            startY = unit.Y;
                        if (endY < unit.Y)
                            endY = unit.Y;
                    }
                    startX--;
                    endX--;
                    startY--;
                    endY--;

                    float targetWallVSize = -(float)(allocInfo.lstUnits[startX][endY].CabinetPos.BottomLeft.y - allocInfo.lstUnits[startX][startY].CabinetPos.TopLeft.y);

                    float dist = 0, wallH = 0, camH = 0;
                    try
                    {
                        dist = float.Parse(txbConfigDist.Text); // WD
                    }
                    catch(Exception ex)
                    {
                        ShowMessageWindow("[Wall-Camera Distance] in Configuration is wrong.", "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                        tbtnGapCamSetPos.IsChecked = false;
                        tcGapCamView.SelectedIndex = 0;
                        return;
                    }
                    if (rbConfigCamPosCustom.IsChecked == true)
                    {
                        try
                        {
                            wallH = float.Parse(txbConfigWallHeight.Text);  // 床面から見たWallの下端の高さ
                        }
                        catch (Exception ex)
                        {
                            ShowMessageWindow("[Wall Bottom Height] in Configuration is wrong.", "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                            tbtnGapCamSetPos.IsChecked = false;
                            tcGapCamView.SelectedIndex = 0;
                            return;
                        }

                        try
                        {
                            camH = float.Parse(txbConfigCamHeight.Text);  // 床面から見たカメラの高さ
                        }
                        catch (Exception ex)
                        {
                            ShowMessageWindow("[Camera Height] in Configuration is wrong.", "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                            tbtnGapCamSetPos.IsChecked = false;
                            tcGapCamView.SelectedIndex = 0;
                            return;
                        }
                    }
                    else
                    {
                        wallH = 0;                        // 床面から見たWallの下端の高さ
                        camH = targetWallVSize / 2;     // 床面から見たカメラの高さ
                    }
                    float wallTopH = wallH + targetWallVSize;   // 床面から見たWall上端の高さ
                    float wallTop_camera_Y = wallTopH - camH;     // Wall上端とカメラの距離（Y軸）
                    float wallBottom_camera_Y = camH - wallH;     // Wall下端とカメラの距離（Y軸）
                    //Wall上端とカメラの距離（最短距離）
                    double wallTop_camera = Math.Sqrt(dist * dist + wallTop_camera_Y * wallTop_camera_Y);

                    if(wallTop_camera > spec)
                    {
                        ShowMessageWindow("The distance of Camera and Wall is too long. Please modify [Camera Height] in Configuration.", "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                        tbtnGapCamSetPos.IsChecked = false;
                        tcGapCamView.SelectedIndex = 0;
                        return;
                    }

                    //Wall下端とカメラの距離（最短距離）
                    double wallBottom_camera = Math.Sqrt(dist * dist + wallBottom_camera_Y * wallBottom_camera_Y);
                    if (wallBottom_camera > spec)
                    {
                        ShowMessageWindow("The distance of Camera and Wall is too long. Please modify [Camera Height] in Configuration.", "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                        tbtnGapCamSetPos.IsChecked = false;
                        tcGapCamView.SelectedIndex = 0;
                        return;
                    }

                    //--------------------------------------------------------------------
                    */


                    // added by Hotta 2023/04/28
                    //tryのエリアを上に移動
                    try
                    {
#if NO_CONTROLLER

#else
                        // deleted by Hotta 2022/11/17
                        /*
                        // ユーザー設定を保存 1Step                  
                        lstCamSetPosUserSettings = new List<UserSetting>();
                        getUserSetting(out lstCamSetPosUserSettings);

                        // 調整用設定 1Step                    
                        setAdjustSetting();
                        */


                        // modified by Hotta 2023/04/28 for v1.06
                        /*
                        // added by Hotta 2022/12/01
                        // ThroughMode設定 1Step                    
                        SetThroughMode(true);
                        //
                        */

                        // 対象コントローラの取得
                        foreach (ControllerInfo controller in dicController.Values)
                            controller.Target = false;
                        outputGapCamTargetArea_EdgeExpand(lstTgtUnits, ExpandType.Both, true);
                        // 画質設定するコントローラは、補正対象Cabinetだけでなく、カメラ位置合わせのCabinetに接続されているものも対象になる
                        outputGapCamTargetArea_EdgeExpand(m_lstCamPosUnits, ExpandType.Both, true);
                        //

                        // ユーザー設定を保存 1Step                  
                        getUserSettingSetPos(out userSetting);


                        ////
                        //userSetting.LowBrightnessMode = 5;
                        //userSetting.TempCorrection = 10;
                        //setUserSettingSetPos(userSetting);
                        ////

                        // ThroughMode設定のみ → TempCorrection, LightOutputも設定する仕様に変更
                        // modified by Hotta 2024/09/30
                        /*
                        setAdjustSetting();
                        */
                        setAdjustSettingSetPos();
                        //

                        // added by Hotta 2022/08/08
                        // Layout info : off
                        foreach (ControllerInfo cont in dicController.Values)
                        { sendSdcpCommand(SDCPClass.CmdDispUnitAddrOff, 0, cont.IPAddress); }
    #endif

                        // AutoFocus
                        // カメラパラメータの設定
                        if (Settings.Ins.Camera.Name == "ILCE-6400")
                        { m_ShootCondition = Settings.Ins.GapCam.Setting_A6400; }
                        else
                        { m_ShootCondition = Settings.Ins.GapCam.Setting_A7; }

                        m_CamMeasPath = tempPath;

                        // added by Hotta 2022/12/15
                        if (File.Exists(m_CamMeasPath + "log.txt") == true)
                            File.Delete(m_CamMeasPath + "log.txt");

    #if NO_CONTROLLER
                        outputMonitorChecker(0, 0, 1080 / 8, 1920 / 8, 1920 / 8, 1080 / 8, 0, 255, 0);
    #else
                        // modified by Hotta 2022/02/28
                        // outputIntSigHatchInv(1, 1, modDy - 2, modDx - 2, modDx, modDy, m_MeasureLevel, m_MeasureLevel, m_MeasureLevel);
                        outputIntSigChecker(0, 0, modDy, modDx, modDx, modDy, m_MeasureLevel, m_MeasureLevel, m_MeasureLevel);
    #endif

                        // added by Hotta 2022/07/26
                        Thread.Sleep(Settings.Ins.Camera.PatternWait);
                        //

                        // modified by Hotta 2022/05/24
    #if CameraPosition


                        //AfAreaSetting afAreaSetting = new AfAreaSetting();
                        //afAreaSetting.focusAreaType = "Center";

#if NO_CAP

#else
                        AutoFocus(m_ShootCondition, new AfAreaSetting());
#endif
                        //AfAreaSetting afAreaSetting = new AfAreaSetting();
                        //afAreaSetting.focusAreaType = "FlexibleSpotM";
                        //afAreaSetting.focusAreaX = (ushort)((afAreaSetting.FlexibleSpotXMax() - afAreaSetting.FlexibleSpotXMin()) * 0.3 + afAreaSetting.FlexibleSpotXMin());
                        //afAreaSetting.focusAreaY = (ushort)((afAreaSetting.FlexibleSpotYMax() - afAreaSetting.FlexibleSpotYMin()) * 0.7 + afAreaSetting.FlexibleSpotYMin());
                        //AutoFocus(Settings.Ins.GapCam.Setting_A6400, afAreaSetting);

#else
                        AutoFocus(Settings.Ins.Camera.SetPosSetting);
#endif

                        // modified by Hotta 2022/12/14
                        //SetCamPosTarget();

                        // deleted by Hotta 2023/04/28
                        // tryのエリアを上に移動
                        /*                        
                        try
                        {
                        */
                        if (allocInfo.LEDModel == ZRD_C15A 
                            || allocInfo.LEDModel == ZRD_B15A 
                            || allocInfo.LEDModel == ZRD_CH15D 
                            || allocInfo.LEDModel == ZRD_BH15D
                            || allocInfo.LEDModel == ZRD_CH15D_S3
                            || allocInfo.LEDModel == ZRD_BH15D_S3)
                        {
                            SetCamPosTarget(ImageType_CamPos.LiveView, true, lstTgtUnits, 8000.0);
                            //SetCamPosTarget(ImageType_CamPos.JPEG, true, lstTgtUnits, 8000.0);
                        }
                        else
                        {
                            SetCamPosTarget(ImageType_CamPos.LiveView, true, lstTgtUnits, 8000.0 * (1.26 / 1.58));
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                        tbtnGapCamSetPos.IsChecked = false;
                        tcGapCamView.SelectedIndex = 0;

#if NO_CONTROLLER
#else
                        // added by Hotta 2023/01/23
                        // ThroughMode設定を解除 1Step
                        SetThroughMode(false);

                        // added by Hotta 2023/04/28 for v1.06
                        // ユーザー設定に書き戻し
                          setUserSettingSetPos(userSetting);
                        //

                        // 内部信号OFF
                        stopIntSig();
                        //
#endif
                        return;
                    }
                    //

                    m_Enable_Capture_MaskImage = true;

                    // タブの表示ページを切り替え
                    tcGapCamView.SelectedIndex = 1;

                    // Timer Start
                    timerGapCam.Enabled = true;
                }
                else
                {
                    // deleted by Hotta 2023/02/06
                    /*

                    // Timer Stop
                    timerGapCam.Enabled = false;

#if NO_CONTROLLER

#else
                    // deleted by Hotta 2022/11/17
                    //
                    //// 調整設定を解除 1Step
                    //setNormalSetting();

                    //// ユーザー設定に書き戻し 1Step
                    //setUserSetting(lstCamSetPosUserSettings);
                    //

                    // added by Hotta 2022/12/01
                    // ThroughMode設定を解除 1Step
                    SetThroughMode(false);


                    // 内部信号OFF
                    stopIntSig();
#endif
                    */


                    txtbStatus.Text = "Done.";
                }
            }
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                return;
            }
        }

        // modified by Hotta 2022/11/14 for 外部入力されたCabinet位置
#if FlexibleCabinetPosition

        float m_WorkDistance;                          // Wallとカメラの距離


        // added by Hotta 2022/12/16
        public enum ImageType_CamPos { LiveView = 0, JPEG };
        public ImageType_CamPos m_ImageType_CamPos = ImageType_CamPos.LiveView;
        //

        // added by Hotta 2022/12/16 for カメラ位置合わせハンチング
        public bool m_PreventionHanting = false;
        //


        // modified by Hotta 2022/12/14
        //void SetCamPosTarget()
        void SetCamPosTarget(ImageType_CamPos imageType = ImageType_CamPos.LiveView,  bool log = false, List<UnitInfo> lstUnit = null, double zDistanceSpec = 0)
        {
            m_ImageType_CamPos = imageType;

            float CameraPosHeight = 0;                // 床面から見た、カメラの高さ
            float InstallationWallBottomHeight = 0;   // 床面から見た、設置Wall下端の高さ
            float TargetWallCenterHeight = 0;         // 床面から見た、対象Wallセンターの高さ

            tgtCamPos = new MarkerPosition();
            tgtCamPos_canUse = new MarkerPosition();
            tgtCamPos_HorLineSpec = new float[2][];
            tgtCamPos_VerLineSpec = new float[2][];
            for (int i = 0; i < 2; i++)
            {
                tgtCamPos_HorLineSpec[i] = new float[2];
                tgtCamPos_VerLineSpec[i] = new float[2];
            }

            if (m_lstCamPosUnits.Count <= 0)
            {
                tgtCamPos = null;
                return;
            }

            int startX = int.MaxValue;
            int endX = int.MinValue;
            int startY = int.MaxValue;
            int endY = int.MinValue;
            foreach (UnitInfo unit in m_lstCamPosUnits)
            {
                if (startX > unit.X)
                    startX = unit.X;
                if (endX < unit.X)
                    endX = unit.X;
                if (startY > unit.Y)
                    startY = unit.Y;
                if (endY < unit.Y)
                    endY = unit.Y;
            }
            startX--;
            endX--;
            startY--;
            endY--;

            m_CabinetXNum_CamPos = endX - startX + 1;
            m_CabinetYNum_CamPos = endY - startY + 1;

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
                // deleted by Hotta 2023/03/03
                /*
                m_LedPitch = (float)RealPitch_P12;
                */
                // added by Hotta 2023/04/28
#if NO_CONTROLLER
                m_LedPitch = (float)RealPitch_P12;
#endif

                m_CabinetDx = CabinetDxP12;
                m_CabinetDy = CabinetDyP12;

                if (allocInfo.LEDModel == ZRD_C12A 
                    || allocInfo.LEDModel == ZRD_B12A)
                {
                    m_ModuleDx = ModuleDxP12_Mdoule4x2;
                    m_ModuleDy = ModuleDyP12_Module4x2;
                }
                else
                {
                    m_ModuleDx = ModuleDxP12_Module4x3;
                    m_ModuleDy = ModuleDyP12_Module4x3;
                }
            }
            else if (allocInfo.LEDModel == ZRD_C15A 
                || allocInfo.LEDModel == ZRD_B15A 
                || allocInfo.LEDModel == ZRD_CH15D 
                || allocInfo.LEDModel == ZRD_BH15D
                || allocInfo.LEDModel == ZRD_CH15D_S3
                || allocInfo.LEDModel == ZRD_BH15D_S3)
            {
#if (DEBUG || Release_log)
                if (Settings.Ins.ExecLog == true)
                {
                    MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                }
#endif
                // deleted by Hotta 2023/03/03
                /*
                m_LedPitch = (float)RealPitch_P15;
                */
                // added by Hotta 2023/04/28
#if NO_CONTROLLER
                m_LedPitch = (float)RealPitch_P15;
#endif

                m_CabinetDx = CabinetDxP15;
                m_CabinetDy = CabinetDyP15;

                if (allocInfo.LEDModel == ZRD_C15A 
                    || allocInfo.LEDModel == ZRD_B15A)
                {
                    m_ModuleDx = ModuleDxP15_Module4x2;
                    m_ModuleDy = ModuleDyP15_Module4x2;
                }
                else
                {
                    m_ModuleDx = ModuleDxP15_Module4x3;
                    m_ModuleDy = ModuleDyP15_Module4x3;
                }
            }
            else
            {
#if (DEBUG || Release_log)
                if (Settings.Ins.ExecLog == true)
                {
                    MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                }
#endif
                return;
            }

            m_ModuleXNum = m_CabinetDx / m_ModuleDx;
            m_ModuleYNum = m_CabinetDy / m_ModuleDy;

            // modified by Hotta 2022/12/16
            /*
            m_CameraParam = new CameraParameter(16.0f, 23.5f, 15.6f, 1024, 680);
            */
            if(m_ImageType_CamPos == ImageType_CamPos.LiveView)
                m_CameraParam = new CameraParameter(16.0f, 23.5f, 15.6f, 1024, 680);
            else
                m_CameraParam = new CameraParameter(16.0f, 23.5f, 15.6f, 3008, 2000);
            //

            // modified by Hotta 2023/02/03
            // タイル　→　Cabinetコーナーに対応するため、スペック変更
            /*
            // modified by Hotta 2022/12/06
            // ラウンド対応
            // Wall端が、手前に設置されるので、大きく映る
            // double canNotUseArea = 0.07;
            double canNotUseArea = 0.06;
            */
            double canNotUseArea = 0.03;

            int notUse = (int)(m_CameraParam.SensorResH * canNotUseArea + 0.5);
            tgtCamPos_canUse.TopLeft.X = notUse;
            tgtCamPos_canUse.TopLeft.Y = notUse;
            tgtCamPos_canUse.TopRight.X = m_CameraParam.SensorResH - notUse;
            tgtCamPos_canUse.TopRight.Y = notUse;
            tgtCamPos_canUse.BottomRight.X = m_CameraParam.SensorResH - notUse;
            tgtCamPos_canUse.BottomRight.Y = m_CameraParam.SensorResV - notUse;
            tgtCamPos_canUse.BottomLeft.X = notUse;
            tgtCamPos_canUse.BottomLeft.Y = m_CameraParam.SensorResV - notUse;


            // Cabinet座標の算出
            SetCabinetPos(m_lstCamPosUnits, 0); // この後、画像座標を計算するときに複数ケのZ座標を入力するので、ここでは0を設定する。

            float cabinetWidth = (float)(allocInfo.lstUnits[startX][startY].CabinetPos.BottomRight.x - allocInfo.lstUnits[startX][startY].CabinetPos.BottomLeft.x);
            float cabinetHeight = -(float)(allocInfo.lstUnits[startX][startY].CabinetPos.BottomLeft.y - allocInfo.lstUnits[startX][startY].CabinetPos.TopLeft.y);


            // 対象Wallの大きさ
            double targetWallVSize = -(allocInfo.lstUnits[startX][endY].CabinetPos.BottomLeft.y - allocInfo.lstUnits[startX][startY].CabinetPos.TopLeft.y); // 左手系なので、-1倍する

            // 設置Wallの下端　と、対象Wallの下端の距離
            double targetWallVOffset = (allocInfo.lstUnits[0].Count - (endY + 1)) * cabinetHeight;


            //m_WorkDistance = float.Parse(txbConfigDist.Text); // 撮影距離は必ず必要なので例外時はそのままエラー
            Dispatcher.Invoke(new Action(() => { m_WorkDistance = float.Parse(txbConfigDist.Text); }));

            bool cameraPosDefault = true;
            Dispatcher.Invoke(new Action(() =>
            {
                if (rbConfigCamPosDefault.IsChecked == true)
                    cameraPosDefault = true;
                else
                    cameraPosDefault = false;
            }));
            //if (rbConfigCamPosDefault.IsChecked == true)
            if (cameraPosDefault == true)
            {
                //（設置Wall下端の高さ）＝ 0
                InstallationWallBottomHeight = 0;
                
                //（対象Wallのセンター高さ）＝（床面から見た、設置Wallの下端の高さ）+（設置Wallの下端⇔対象Wall下端）+（対象Wall下端⇔対象Wallセンター）
                TargetWallCenterHeight = (float)(InstallationWallBottomHeight + targetWallVOffset + targetWallVSize / 2);
                
                //（カメラ高さ）＝（対象Wallのセンター高さ）
                CameraPosHeight = TargetWallCenterHeight;
            }
            else
            {
                //（設置Wall下端の高さ）＝ （ユーザー設定値）
                //InstallationWallBottomHeight = float.Parse(txbConfigWallHeight.Text);
                Dispatcher.Invoke(new Action(() => { InstallationWallBottomHeight = float.Parse(txbConfigWallHeight.Text); }));

                // （対象Wallのセンター高さ）＝（床面から見た、設置Wallの下端の高さ）+（設置Wallの下端⇔対象Wall下端）+（対象Wall下端⇔対象Wallセンター）
                TargetWallCenterHeight = (float)(InstallationWallBottomHeight + targetWallVOffset + targetWallVSize / 2);

                //（カメラ高さ）＝（ユーザー設定値）
                //CameraPosHeight = float.Parse(txbConfigCamHeight.Text);
                Dispatcher.Invoke(new Action(() => { CameraPosHeight = float.Parse(txbConfigCamHeight.Text); }));
            }

            // added by Hotta 2022/12/15
            if(log == true)
            {
                saveLog("Set Camera Position Target");
                saveLog(string.Format("Target Cabinet X : {0} - {1}, Y : {2} - {3}", startX + 1, endX + 1, startY + 1, endY + 1));
                saveLog(string.Format("TargetWallVSize : {0}, TargetWallVOffset : {1}, WorkDistance : {2}", targetWallVSize, targetWallVOffset, m_WorkDistance));
                saveLog(string.Format("CameraPosDefault : {0}, InstallationWallBottomHeight : {1}, TargetWallCenterHeight : {2}, CameraPosHeight : {3}", cameraPosDefault.ToString(), InstallationWallBottomHeight, TargetWallCenterHeight, CameraPosHeight));
            }
            //


            TransformImage.TransformImage transform = new TransformImage.TransformImage();

            float transZ = m_WorkDistance;

            //if (allocInfo.LEDModel == ZRD_C15A || allocInfo.LEDModel == ZRD_B15A || allocInfo.LEDModel == ZRD_CH15D || allocInfo.LEDModel == ZRD_BH15D)
            //{ transZ *= (float)(RealPitch_P15 / RealPitch_P12); }

            m_Transration = new float[] { 0, 0, 0 };
            m_Rotate = new float[] { 0.0f, 0.0f, 0.0f };
            m_ShiftToCameraCoordinate = new float[] { 0, 0, 0 };


#if NO_CONTROLLER
            // HP224
            float k;
            if (allocInfo.LEDModel == ZRD_C15A 
                || allocInfo.LEDModel == ZRD_B15A 
                || allocInfo.LEDModel == ZRD_CH15D 
                || allocInfo.LEDModel == ZRD_BH15D
                || allocInfo.LEDModel == ZRD_CH15D_S3 
                || allocInfo.LEDModel == ZRD_BH15D_S3)
            {
                    k = 476.0f / (m_LedPitch * m_CabinetDx * CabinetLength_P15);
            }
            else
            {
                k = 476.0f / (m_LedPitch * m_CabinetDx * CabinetLength_P12);
            }

#if NO_CAP
            k = 1;
#endif

#else
            float k = 1;
#endif


#if NO_CONTROLLER
#if NO_CAP
            k = 1;
#endif
#endif

            transZ *= k;
            CameraPosHeight *= k;
            TargetWallCenterHeight *= k;

            // コーナーの最外の3D座標を求める
            TransformImage.TransformImage transform3D = new TransformImage.TransformImage();


            // 左上
            float left = (float)allocInfo.lstUnits[startX][startY].CabinetPos.TopLeft.x;
            float right = (float)allocInfo.lstUnits[startX][startY].CabinetPos.BottomRight.x;
            float top = -(float)allocInfo.lstUnits[startX][startY].CabinetPos.TopLeft.y;         // -1倍して、左手座標系　→　右手座標系に変換    
            float bottom = -(float)allocInfo.lstUnits[startX][startY].CabinetPos.BottomLeft.y;   // -1倍して、左手座標系　→　右手座標系に変換
            float front = (float)allocInfo.lstUnits[startX][startY].CabinetPos.TopLeft.z;
            float back = (float)allocInfo.lstUnits[startX][startY].CabinetPos.BottomRight.z;

            float xPos = left + 1.0f / 4 * (right - left);
            float yPos = top + 1.0f / 4 * (bottom - top);
            float zPos = front + 1.0f / 4 * (back - front);
            transform.Set3DPoints(0, xPos * k, yPos * k, zPos * k);

            // 右上
            left = (float)allocInfo.lstUnits[endX][startY].CabinetPos.TopLeft.x;
            right = (float)allocInfo.lstUnits[endX][startY].CabinetPos.BottomRight.x;
            top = -(float)allocInfo.lstUnits[endX][startY].CabinetPos.TopLeft.y;         // -1倍して、左手座標系　→　右手座標系に変換    
            bottom = -(float)allocInfo.lstUnits[endX][startY].CabinetPos.BottomLeft.y;   // -1倍して、左手座標系　→　右手座標系に変換
            front = (float)allocInfo.lstUnits[endX][startY].CabinetPos.TopLeft.z;
            back = (float)allocInfo.lstUnits[endX][startY].CabinetPos.BottomRight.z;

            xPos = left + 3.0f / 4 * (right - left);
            yPos = top + 1.0f / 4 * (bottom - top);
            zPos = front + 3.0f / 4 * (back - front);
            transform.Set3DPoints(1, xPos * k, yPos * k, zPos * k);

            // 右下
            left = (float)allocInfo.lstUnits[endX][endY].CabinetPos.TopLeft.x;
            right = (float)allocInfo.lstUnits[endX][endY].CabinetPos.BottomRight.x;
            top = -(float)allocInfo.lstUnits[endX][endY].CabinetPos.TopLeft.y;         // -1倍して、左手座標系　→　右手座標系に変換    
            bottom = -(float)allocInfo.lstUnits[endX][endY].CabinetPos.BottomLeft.y;   // -1倍して、左手座標系　→　右手座標系に変換
            front = (float)allocInfo.lstUnits[endX][endY].CabinetPos.TopLeft.z;
            back = (float)allocInfo.lstUnits[endX][endY].CabinetPos.BottomRight.z;

            xPos = left + 3.0f / 4 * (right - left);
            yPos = top + 3.0f / 4 * (bottom - top);
            zPos = front + 3.0f / 4 * (back - front);
            transform.Set3DPoints(2, xPos * k, yPos * k, zPos * k);

            // 左下
            left = (float)allocInfo.lstUnits[startX][endY].CabinetPos.TopLeft.x;
            right = (float)allocInfo.lstUnits[startX][endY].CabinetPos.BottomRight.x;
            top = -(float)allocInfo.lstUnits[startX][endY].CabinetPos.TopLeft.y;         // -1倍して、左手座標系　→　右手座標系に変換    
            bottom = -(float)allocInfo.lstUnits[startX][endY].CabinetPos.BottomLeft.y;   // -1倍して、左手座標系　→　右手座標系に変換
            front = (float)allocInfo.lstUnits[startX][endY].CabinetPos.TopLeft.z;
            back = (float)allocInfo.lstUnits[startX][endY].CabinetPos.BottomRight.z;

            xPos = left + 1.0f / 4 * (right - left);
            yPos = top + 3.0f / 4 * (bottom - top);
            zPos = front + 1.0f / 4 * (back - front);
            transform.Set3DPoints(3, xPos * k, yPos * k, zPos * k);

            // カメラ高さ、Wall下端の高さ、Wallの大きさ から、あおり角度を決定
            double tiltAngle = Math.Atan((TargetWallCenterHeight - CameraPosHeight) / transZ) / Math.PI * 180.0;
            m_Rotate[0] = -(float)tiltAngle;

            m_Transration[1] = -(float)(TargetWallCenterHeight - CameraPosHeight);


            // added by Hotta 2022/12/15
            if(log == true)
            {
                saveLog(string.Format("Wall Transration : {0:F1}, {1:F1}, {2:F1}", m_Transration[0], m_Transration[1], transZ));
                saveLog(string.Format("Wall Rotate : {0:F1}, {1:F1}, {2:F1}", m_Rotate[0], m_Rotate[1], m_Rotate[2]));
            }
            //


            // 基準の撮影画像座標
            transform.SetTranslation(m_Transration[0], m_Transration[1], transZ);
            transform.SetRx(m_Rotate[0]);
            transform.SetRy(m_Rotate[1]);
            transform.SetRz(m_Rotate[2]);

            transform.SetShiftToCameraCoordinate(m_ShiftToCameraCoordinate[0], m_ShiftToCameraCoordinate[1], m_ShiftToCameraCoordinate[2]);
            transform.CameraParameter.Set(m_CameraParam.f, m_CameraParam.SensorSizeH, m_CameraParam.SensorSizeV, m_CameraParam.SensorResH, m_CameraParam.SensorResV);
            transform.Calc();

            tgtCamPos.TopLeft.X = transform.ImagePoints[0].X;
            tgtCamPos.TopLeft.Y = transform.ImagePoints[0].Y;
            tgtCamPos.TopRight.X = transform.ImagePoints[1].X;
            tgtCamPos.TopRight.Y = transform.ImagePoints[1].Y;
            tgtCamPos.BottomRight.X = transform.ImagePoints[2].X;
            tgtCamPos.BottomRight.Y = transform.ImagePoints[2].Y;
            tgtCamPos.BottomLeft.X = transform.ImagePoints[3].X;
            tgtCamPos.BottomLeft.Y = transform.ImagePoints[3].Y;


            // modified by Hotta 2023/01/17
            /*
            // added by Hotta 2022/12/16
            if((double)(tgtCamPos.BottomLeft.Y - tgtCamPos.TopLeft.Y) / m_CameraParam.SensorResV < 0.2 ||
                (double)(tgtCamPos.TopRight.X - tgtCamPos.TopLeft.X) / m_CameraParam.SensorResH < 0.2)
            */
            if ((double)(tgtCamPos.BottomLeft.Y - tgtCamPos.TopLeft.Y) / m_CameraParam.SensorResV < 0.3 ||
                (double)(tgtCamPos.TopRight.X - tgtCamPos.TopLeft.X) / m_CameraParam.SensorResH < 0.3)
            {
                m_PreventionHanting = true;
            }
            else
            {
                m_PreventionHanting = false;
            }


            // addd by Hotta 2022/12/15
            if (log == true)
            {
                saveLog(string.Format("CanUseArea : ({0},{1}), ({2},{3}), ({4},{5}), ({6},{7}),", tgtCamPos_canUse.TopLeft.X, tgtCamPos_canUse.TopLeft.Y, tgtCamPos_canUse.TopRight.X, tgtCamPos_canUse.TopRight.Y, tgtCamPos_canUse.BottomRight.X, tgtCamPos_canUse.BottomRight.Y, tgtCamPos_canUse.BottomLeft.X, tgtCamPos_canUse.BottomLeft.Y));
                saveLog(string.Format("TargetArea : ({0:F1},{1:F1}), ({2:F1},{3:F1}), ({4:F1},{5:F1}), ({6:F1},{7:F1}),", tgtCamPos.TopLeft.X, tgtCamPos.TopLeft.Y, tgtCamPos.TopRight.X, tgtCamPos.TopRight.Y, tgtCamPos.BottomRight.X, tgtCamPos.BottomRight.Y, tgtCamPos.BottomLeft.X, tgtCamPos.BottomLeft.Y));
            }
            //

#if TempFile
            // 確認
            using(StreamWriter sw = new StreamWriter(applicationPath + "\\Temp\\cabinetPos.csv"))
            {
                string str;
                int xNum = allocInfo.MaxX;
                int yNum = allocInfo.MaxY;

                sw.WriteLine("TopLeft,,,,TopRight,,,,BottomRight,,,,BottomLeft");
                sw.WriteLine("x,y,z,,x,y,z,,x,y,z,,x,y,z");

                for (int y = 0; y < yNum; y++)
                {
                    for (int x = 0; x < xNum; x++)
                    {
                        if (allocInfo.lstUnits[x][y] == null)
                        {
                            str = "-,-,-";
                            str += ",,";
                            str += "-,-,-";
                            str += ",,";
                            str += "-,-,-";
                            str += ",,";
                            str += "-,-,-";
                        }
                        else
                        {
                            str = string.Format("{0},{1},{2}", allocInfo.lstUnits[x][y].CabinetPos.TopLeft.x, allocInfo.lstUnits[x][y].CabinetPos.TopLeft.y, allocInfo.lstUnits[x][y].CabinetPos.TopLeft.z);
                            str += ",,";
                            str += string.Format("{0},{1},{2}", allocInfo.lstUnits[x][y].CabinetPos.TopRight.x, allocInfo.lstUnits[x][y].CabinetPos.TopRight.y, allocInfo.lstUnits[x][y].CabinetPos.TopRight.z);
                            str += ",,";
                            str += string.Format("{0},{1},{2}", allocInfo.lstUnits[x][y].CabinetPos.BottomRight.x, allocInfo.lstUnits[x][y].CabinetPos.BottomRight.y, allocInfo.lstUnits[x][y].CabinetPos.BottomRight.z);
                            str += ",,";
                            str += string.Format("{0},{1},{2}", allocInfo.lstUnits[x][y].CabinetPos.BottomLeft.x, allocInfo.lstUnits[x][y].CabinetPos.BottomLeft.y, allocInfo.lstUnits[x][y].CabinetPos.BottomLeft.z);
                        }
                        sw.WriteLine(str);
                    }
                    sw.WriteLine("");
                }
            }
#endif

            // deleted by Hotta 2023/02/02
            // カーブモデルをあおり撮影した場合、撮影画像中、最も下の座標はコーナーでなく、センターになる。
            // 全てのCabinetに対して、最後にまとめて行うようにする。
            /*
            // added by Hotta 2022/12/14 for はみ出しチェック
            if (tgtCamPos.TopLeft.X <= tgtCamPos_canUse.TopLeft.X ||
                tgtCamPos.TopLeft.Y <= tgtCamPos_canUse.TopLeft.Y ||
                tgtCamPos.TopRight.X >= tgtCamPos_canUse.TopRight.X ||
                tgtCamPos.TopRight.Y <= tgtCamPos_canUse.TopRight.Y ||
                tgtCamPos.BottomRight.X >= tgtCamPos_canUse.BottomRight.X ||
                tgtCamPos.BottomRight.Y >= tgtCamPos_canUse.BottomRight.Y ||
                tgtCamPos.BottomLeft.X <= tgtCamPos_canUse.BottomLeft.X ||
                tgtCamPos.BottomLeft.Y >= tgtCamPos_canUse.BottomLeft.Y)
            {
                // modified by Hotta 2023/01/17
                //throw (new Exception("The distance between the camera and Wall is too short. Please edit [Camera Height] in Configuration so that the distance to Wall is longer."));
                throw (new Exception("The corner position of wall is out of range of camera. Please Select less cabinet again or Edit [Camera Height] in Configuration so that the distance to Wall is longer."));
            }
            //
            */

            // deleted by Hotta 2023/01/27
            // あおりも含めた、カメラの撮像素子に垂直なZ距離でチェックする必要あり。
            // 下に追加
            /*
            // added by Hotta 2023/01/17 for 近すぎによるモアレ防止    
            for(int y=startY;y<=endY;y++)
            {
                for(int x=startX;x<=endX;x++)
                {
                    if(allocInfo.lstUnits[x][y].CabinetPos.TopLeft.z + m_WorkDistance < m_WorkDistance * 0.875 ||
                        allocInfo.lstUnits[x][y].CabinetPos.TopRight.z + m_WorkDistance < m_WorkDistance * 0.875 ||
                        allocInfo.lstUnits[x][y].CabinetPos.BottomRight.z + m_WorkDistance < m_WorkDistance * 0.875 ||
                        allocInfo.lstUnits[x][y].CabinetPos.BottomLeft.z + m_WorkDistance < m_WorkDistance * 0.875)
                    {
                        throw (new Exception("The distance between cabinet and camera is too close. Please Selectless cabinet again or Edit [Camera Height] in Configuration so that the distance to Wall is longer."));
                    }
                }
            }
            */


            // 規格に使用する撮影画像座標
            for (int size = 0; size < 2; size++)
            {
                // WDが大きくなった時のサイズ計算
                if (size == 0)
                    transZ = m_WorkDistance * (1.0f / (float)Settings.Ins.GapCam.CamPos_SizeMin);
                // WDが小さくなった時のサイズ計算
                else
                    transZ = m_WorkDistance * (1.0f / (float)Settings.Ins.GapCam.CamPos_SizeMax);

                transZ *= k;

                //if (allocInfo.LEDModel == ZRD_C15A || allocInfo.LEDModel == ZRD_B15A || allocInfo.LEDModel == ZRD_CH15D || allocInfo.LEDModel == ZRD_BH15D)
                //{ transZ *= (float)(RealPitch_P15 / RealPitch_P12); }
                transform.SetTranslation(m_Transration[0], m_Transration[1], transZ);
                transform.Calc();

                tgtCamPos_HorLineSpec[0][size] = (float)Math.Sqrt(Math.Pow(transform.ImagePoints[1].X - transform.ImagePoints[0].X, 2) + Math.Pow(transform.ImagePoints[1].Y - transform.ImagePoints[0].Y, 2));
                tgtCamPos_HorLineSpec[1][size] = (float)Math.Sqrt(Math.Pow(transform.ImagePoints[2].X - transform.ImagePoints[3].X, 2) + Math.Pow(transform.ImagePoints[2].Y - transform.ImagePoints[3].Y, 2));
                tgtCamPos_VerLineSpec[0][size] = (float)Math.Sqrt(Math.Pow(transform.ImagePoints[3].X - transform.ImagePoints[0].X, 2) + Math.Pow(transform.ImagePoints[3].Y - transform.ImagePoints[0].Y, 2));
                tgtCamPos_VerLineSpec[1][size] = (float)Math.Sqrt(Math.Pow(transform.ImagePoints[2].X - transform.ImagePoints[1].X, 2) + Math.Pow(transform.ImagePoints[2].Y - transform.ImagePoints[1].Y, 2));
            }


            // modified by Hotta 2023/02/02
            // 撮影エリアからのはみ出し/Z距離が近すぎ/Z距離が遠すぎ　を、まとめてチェックする
            /*
            // added by Hotta 2022/12/14 for Z距離チェック
            // 補正対象Cabinetが、遠すぎないことをチェック。
            double longestZ = double.MinValue;
            if (lstUnit != null && zDistanceSpec != 0)
            {
                SetCabinetPos(m_lstCamPosUnits, m_WorkDistance);

                // modified by Hotta 2023/01/30
                //MoveCabinetPos(0, m_Rotate[0], 0, 0, -m_Transration[1], 0);              // あおり撮影
                MoveCabinetPos(0, m_Rotate[0], 0, 0, -m_Transration[1] / k, 0);              // あおり撮影、Cabinetやスペックは、オリジナルのサイズなので、Transもオリジナルに戻す

                foreach(UnitInfo unit in lstUnit)
                {
                    // modified by Hotta 2023/02/02
                    //double z = (unit.CabinetPos.TopLeft.z + unit.CabinetPos.TopRight.z + unit.CabinetPos.BottomRight.z + unit.CabinetPos.BottomLeft.z) / 4;
                    double z = Math.Max(Math.Max(Math.Max(unit.CabinetPos.TopLeft.z, unit.CabinetPos.TopRight.z), unit.CabinetPos.BottomRight.z), unit.CabinetPos.BottomLeft.z);
                    if (longestZ < z)
                        longestZ = z;
                }

                if (log == true)
                    saveLog(string.Format("Longest Z distance to cabinet : {0:F1}", longestZ));

                if (longestZ > zDistanceSpec)
                {
                    throw (new Exception("The distance between the camera and Wall is too long. Please edit [Camera Height] in Configuration so that the distance to Wall is shorter."));
                }
            }

            // added by Hotta 2023/01/27 for 近すぎによるモアレ防止チェック
            // 補正対象Cabinetが、近すぎないことをチェック。
            double shortestZ = double.MaxValue;
            if (lstUnit != null)
            {
                SetCabinetPos(m_lstCamPosUnits, m_WorkDistance);
                // modified by Hotta 2023/01/30
                //MoveCabinetPos(0, m_Rotate[0], 0, 0, -m_Transration[1], 0);              // あおり撮影
                MoveCabinetPos(0, m_Rotate[0], 0, 0, -m_Transration[1] / k, 0);              // あおり撮影、Cabinetやスペックは、オリジナルのサイズなので、Transもオリジナルに戻す

                foreach (UnitInfo unit in lstUnit)
                {
                    double z = Math.Min(Math.Min(Math.Min(unit.CabinetPos.TopLeft.z, unit.CabinetPos.TopRight.z), unit.CabinetPos.BottomRight.z), unit.CabinetPos.BottomLeft.z);
                    if (shortestZ > z)
                        shortestZ = z;
                }

                if (log == true)
                    saveLog(string.Format("Shortest Z distance to cabinet : {0:F1}", shortestZ));

                if (shortestZ < m_WorkDistance * 0.86)
                {
                    throw (new Exception("The distance between cabinet and camera is too close. Please Select less cabinet again or Edit [Camera Height] in Configuration so that the distance to Wall is longer."));
                }
            }
            */

            double longestZ = double.MinValue;
            double shortestZ = double.MaxValue;
            float imageXMin = float.MaxValue;
            float imageXMax = float.MinValue;
            float imageYMin = float.MaxValue;
            float imageYMax = float.MinValue;

            if (lstUnit != null/* && zDistanceSpec != 0*/)
            {

                // added by Hotta 2023/02/20
                // エラーで抜ける前にファイル保存
#if TempFile
                using (StreamWriter sw = new StreamWriter(applicationPath + "\\Temp\\CabinetPosi.csv"))
                {
                    string strOrg = "";

                    SetCabinetPos(m_lstCamPosUnits, /*m_WorkDistance*/0);
                    //MoveCabinetPos(0, m_Rotate[0], 0, 0, -m_Transration[1] / k, 0);              // あおり撮影、Cabinetやスペックは、オリジナルのサイズなので、Transもオリジナルに戻す
                    for (int y = startY; y <= endY; y++)
                    {
                        foreach (UnitInfo unit in lstUnit)
                        {
                            if (unit.X - 1 == startX && unit.Y - 1 == y)
                            {
                                transform.Set3DPoints(0, (float)unit.CabinetPos.TopLeft.x * k, -(float)unit.CabinetPos.TopLeft.y * k, (float)unit.CabinetPos.TopLeft.z * k);        // y : -1倍して、左手座標系　→　右手座標系に変換    
                                transform.Set3DPoints(1, (float)unit.CabinetPos.TopRight.x * k, -(float)unit.CabinetPos.TopRight.y * k, (float)unit.CabinetPos.TopRight.z * k);        // y : -1倍して、左手座標系　→　右手座標系に変換    
                                transform.Set3DPoints(2, (float)unit.CabinetPos.BottomRight.x * k, -(float)unit.CabinetPos.BottomRight.y * k, (float)unit.CabinetPos.BottomRight.z * k);        // y : -1倍して、左手座標系　→　右手座標系に変換    
                                transform.Set3DPoints(3, (float)unit.CabinetPos.BottomLeft.x * k, -(float)unit.CabinetPos.BottomLeft.y * k, (float)unit.CabinetPos.BottomLeft.z * k);        // y : -1倍して、左手座標系　→　右手座標系に変換    

                                transform.SetTranslation(m_Transration[0], m_Transration[1], m_WorkDistance * k);
                                transform.SetRx(m_Rotate[0]);
                                transform.SetRy(m_Rotate[1]);
                                transform.SetRz(m_Rotate[2]);

                                transform.SetShiftToCameraCoordinate(m_ShiftToCameraCoordinate[0], m_ShiftToCameraCoordinate[1], m_ShiftToCameraCoordinate[2]);
                                transform.CameraParameter.Set(m_CameraParam.f, m_CameraParam.SensorSizeH, m_CameraParam.SensorSizeV, m_CameraParam.SensorResH, m_CameraParam.SensorResV);
                                transform.Calc();

                                sw.WriteLine(transform.ImagePoints[0].X.ToString() + "," + transform.ImagePoints[0].Y.ToString());
                                sw.WriteLine(transform.ImagePoints[3].X.ToString() + "," + transform.ImagePoints[3].Y.ToString());

                                if (y == startY)
                                    strOrg = transform.ImagePoints[0].X.ToString() + "," + transform.ImagePoints[0].Y.ToString();

                                break;
                            }
                        }
                    }

                    for (int x = startX; x <= endX; x++)
                    {
                        foreach (UnitInfo unit in lstUnit)
                        {
                            if (unit.X - 1 == x && unit.Y - 1 == endY)
                            {
                                transform.Set3DPoints(0, (float)unit.CabinetPos.TopLeft.x * k, -(float)unit.CabinetPos.TopLeft.y * k, (float)unit.CabinetPos.TopLeft.z * k);        // y : -1倍して、左手座標系　→　右手座標系に変換    
                                transform.Set3DPoints(1, (float)unit.CabinetPos.TopRight.x * k, -(float)unit.CabinetPos.TopRight.y * k, (float)unit.CabinetPos.TopRight.z * k);        // y : -1倍して、左手座標系　→　右手座標系に変換    
                                transform.Set3DPoints(2, (float)unit.CabinetPos.BottomRight.x * k, -(float)unit.CabinetPos.BottomRight.y * k, (float)unit.CabinetPos.BottomRight.z * k);        // y : -1倍して、左手座標系　→　右手座標系に変換    
                                transform.Set3DPoints(3, (float)unit.CabinetPos.BottomLeft.x * k, -(float)unit.CabinetPos.BottomLeft.y * k, (float)unit.CabinetPos.BottomLeft.z * k);        // y : -1倍して、左手座標系　→　右手座標系に変換    

                                transform.SetTranslation(m_Transration[0], m_Transration[1], m_WorkDistance * k);
                                transform.SetRx(m_Rotate[0]);
                                transform.SetRy(m_Rotate[1]);
                                transform.SetRz(m_Rotate[2]);

                                transform.SetShiftToCameraCoordinate(m_ShiftToCameraCoordinate[0], m_ShiftToCameraCoordinate[1], m_ShiftToCameraCoordinate[2]);
                                transform.CameraParameter.Set(m_CameraParam.f, m_CameraParam.SensorSizeH, m_CameraParam.SensorSizeV, m_CameraParam.SensorResH, m_CameraParam.SensorResV);
                                transform.Calc();

                                sw.WriteLine(transform.ImagePoints[3].X.ToString() + "," + transform.ImagePoints[3].Y.ToString());
                                sw.WriteLine(transform.ImagePoints[2].X.ToString() + "," + transform.ImagePoints[2].Y.ToString());
                                break;
                            }
                        }
                    }

                    for (int y = endY; y >= startY; y--)
                    {
                        foreach (UnitInfo unit in lstUnit)
                        {
                            if (unit.X - 1 == endX && unit.Y - 1 == y)
                            {
                                transform.Set3DPoints(0, (float)unit.CabinetPos.TopLeft.x * k, -(float)unit.CabinetPos.TopLeft.y * k, (float)unit.CabinetPos.TopLeft.z * k);        // y : -1倍して、左手座標系　→　右手座標系に変換    
                                transform.Set3DPoints(1, (float)unit.CabinetPos.TopRight.x * k, -(float)unit.CabinetPos.TopRight.y * k, (float)unit.CabinetPos.TopRight.z * k);        // y : -1倍して、左手座標系　→　右手座標系に変換    
                                transform.Set3DPoints(2, (float)unit.CabinetPos.BottomRight.x * k, -(float)unit.CabinetPos.BottomRight.y * k, (float)unit.CabinetPos.BottomRight.z * k);        // y : -1倍して、左手座標系　→　右手座標系に変換    
                                transform.Set3DPoints(3, (float)unit.CabinetPos.BottomLeft.x * k, -(float)unit.CabinetPos.BottomLeft.y * k, (float)unit.CabinetPos.BottomLeft.z * k);        // y : -1倍して、左手座標系　→　右手座標系に変換    

                                transform.SetTranslation(m_Transration[0], m_Transration[1], m_WorkDistance * k);
                                transform.SetRx(m_Rotate[0]);
                                transform.SetRy(m_Rotate[1]);
                                transform.SetRz(m_Rotate[2]);

                                transform.SetShiftToCameraCoordinate(m_ShiftToCameraCoordinate[0], m_ShiftToCameraCoordinate[1], m_ShiftToCameraCoordinate[2]);
                                transform.CameraParameter.Set(m_CameraParam.f, m_CameraParam.SensorSizeH, m_CameraParam.SensorSizeV, m_CameraParam.SensorResH, m_CameraParam.SensorResV);
                                transform.Calc();

                                sw.WriteLine(transform.ImagePoints[2].X.ToString() + "," + transform.ImagePoints[2].Y.ToString());
                                sw.WriteLine(transform.ImagePoints[1].X.ToString() + "," + transform.ImagePoints[1].Y.ToString());
                                break;
                            }
                        }
                    }

                    for (int x = endX; x >= startX; x--)
                    {
                        foreach (UnitInfo unit in lstUnit)
                        {
                            if (unit.X - 1 == x && unit.Y - 1 == startY)
                            {
                                transform.Set3DPoints(0, (float)unit.CabinetPos.TopLeft.x * k, -(float)unit.CabinetPos.TopLeft.y * k, (float)unit.CabinetPos.TopLeft.z * k);        // y : -1倍して、左手座標系　→　右手座標系に変換    
                                transform.Set3DPoints(1, (float)unit.CabinetPos.TopRight.x * k, -(float)unit.CabinetPos.TopRight.y * k, (float)unit.CabinetPos.TopRight.z * k);        // y : -1倍して、左手座標系　→　右手座標系に変換    
                                transform.Set3DPoints(2, (float)unit.CabinetPos.BottomRight.x * k, -(float)unit.CabinetPos.BottomRight.y * k, (float)unit.CabinetPos.BottomRight.z * k);        // y : -1倍して、左手座標系　→　右手座標系に変換    
                                transform.Set3DPoints(3, (float)unit.CabinetPos.BottomLeft.x * k, -(float)unit.CabinetPos.BottomLeft.y * k, (float)unit.CabinetPos.BottomLeft.z * k);        // y : -1倍して、左手座標系　→　右手座標系に変換    

                                transform.SetTranslation(m_Transration[0], m_Transration[1], m_WorkDistance * k);
                                transform.SetRx(m_Rotate[0]);
                                transform.SetRy(m_Rotate[1]);
                                transform.SetRz(m_Rotate[2]);

                                transform.SetShiftToCameraCoordinate(m_ShiftToCameraCoordinate[0], m_ShiftToCameraCoordinate[1], m_ShiftToCameraCoordinate[2]);
                                transform.CameraParameter.Set(m_CameraParam.f, m_CameraParam.SensorSizeH, m_CameraParam.SensorSizeV, m_CameraParam.SensorResH, m_CameraParam.SensorResV);
                                transform.Calc();

                                sw.WriteLine(transform.ImagePoints[1].X.ToString() + "," + transform.ImagePoints[1].Y.ToString());
                                sw.WriteLine(transform.ImagePoints[0].X.ToString() + "," + transform.ImagePoints[0].Y.ToString());
                                break;
                            }
                        }
                    }


                    sw.WriteLine(strOrg);

                }
#endif

                SetCabinetPos(m_lstCamPosUnits, m_WorkDistance);
                MoveCabinetPos(0, m_Rotate[0], 0, 0, -m_Transration[1] / k, 0);              // あおり撮影、Cabinetやスペックは、オリジナルのサイズなので、Transもオリジナルに戻す
                foreach (UnitInfo unit in lstUnit)
                {
                    double longZ = Math.Max(Math.Max(Math.Max(unit.CabinetPos.TopLeft.z, unit.CabinetPos.TopRight.z), unit.CabinetPos.BottomRight.z), unit.CabinetPos.BottomLeft.z);
                    if (longestZ < longZ)
                        longestZ = longZ;
                    double shortZ = Math.Min(Math.Min(Math.Min(unit.CabinetPos.TopLeft.z, unit.CabinetPos.TopRight.z), unit.CabinetPos.BottomRight.z), unit.CabinetPos.BottomLeft.z);
                    if (shortestZ > shortZ)
                        shortestZ = shortZ;
                }

                SetCabinetPos(m_lstCamPosUnits, /*m_WorkDistance*/0);
                //MoveCabinetPos(0, m_Rotate[0], 0, 0, -m_Transration[1] / k, 0);              // あおり撮影、Cabinetやスペックは、オリジナルのサイズなので、Transもオリジナルに戻す
                foreach (UnitInfo unit in lstUnit)
                {
                    transform.Set3DPoints(0, (float)unit.CabinetPos.TopLeft.x * k, -(float)unit.CabinetPos.TopLeft.y * k, (float)unit.CabinetPos.TopLeft.z * k);        // y : -1倍して、左手座標系　→　右手座標系に変換    
                    transform.Set3DPoints(1, (float)unit.CabinetPos.TopRight.x * k, -(float)unit.CabinetPos.TopRight.y * k, (float)unit.CabinetPos.TopRight.z * k);        // y : -1倍して、左手座標系　→　右手座標系に変換    
                    transform.Set3DPoints(2, (float)unit.CabinetPos.BottomRight.x * k, -(float)unit.CabinetPos.BottomRight.y * k, (float)unit.CabinetPos.BottomRight.z * k);        // y : -1倍して、左手座標系　→　右手座標系に変換    
                    transform.Set3DPoints(3, (float)unit.CabinetPos.BottomLeft.x * k, -(float)unit.CabinetPos.BottomLeft.y * k, (float)unit.CabinetPos.BottomLeft.z * k);        // y : -1倍して、左手座標系　→　右手座標系に変換    

                    transform.SetTranslation(m_Transration[0], m_Transration[1], m_WorkDistance * k);
                    transform.SetRx(m_Rotate[0]);
                    transform.SetRy(m_Rotate[1]);
                    transform.SetRz(m_Rotate[2]);

                    transform.SetShiftToCameraCoordinate(m_ShiftToCameraCoordinate[0], m_ShiftToCameraCoordinate[1], m_ShiftToCameraCoordinate[2]);
                    transform.CameraParameter.Set(m_CameraParam.f, m_CameraParam.SensorSizeH, m_CameraParam.SensorSizeV, m_CameraParam.SensorResH, m_CameraParam.SensorResV);
                    transform.Calc();

                    float imageLeft = Math.Min(transform.ImagePoints[0].X, transform.ImagePoints[3].X);
                    float imageRight = Math.Max(transform.ImagePoints[1].X, transform.ImagePoints[2].X);
                    float imageTop = Math.Min(transform.ImagePoints[0].Y, transform.ImagePoints[1].Y);
                    float imageBottom = Math.Max(transform.ImagePoints[2].Y, transform.ImagePoints[3].Y);

                    if (imageXMin > imageLeft)
                        imageXMin = imageLeft;
                    if (imageXMax < imageRight)
                        imageXMax = imageRight;
                    if (imageYMin > imageTop)
                        imageYMin = imageTop;
                    if (imageYMax < imageBottom)
                        imageYMax = imageBottom;
                }

                if (log == true)
                {
                    saveLog(string.Format("Longest Z distance to cabinet : {0:F1}", longestZ));
                    saveLog(string.Format("Shortest Z distance to cabinet : {0:F1}", shortestZ));
                    saveLog(string.Format("Image edge (Left/Right/Top/Bottom) : {0:F1}, {1:F1}, {2:F1}, {3:F1}", imageXMin, imageXMax, imageYMin, imageYMax));
                }

                // Z距離が遠すぎないことのチェック
                if (zDistanceSpec != 0)
                {
                    if (longestZ > zDistanceSpec)
                    {
                        throw (new Exception("The distance between the camera and Wall is too long. Please edit [Camera Height] in [Configuration] tab so that the distance to Wall is shorter."));
                    }
                }

                // Z距離が近すぎないことのチェック
                if (shortestZ < m_WorkDistance * 0.86)
                {
                    throw (new Exception("The distance between thee camera and Wall is too close. Please select less cabinet again or edit [Camera Height] in [Configuration] tab so that the distance to Wall is longer."));
                }

                // 撮影エリアからはみ出していないことのチェック
                if (imageXMin < tgtCamPos_canUse.TopLeft.X ||
                    imageYMin < tgtCamPos_canUse.TopLeft.Y ||
                    imageXMax > tgtCamPos_canUse.TopRight.X ||
                    imageYMin < tgtCamPos_canUse.TopRight.Y ||
                    imageXMax > tgtCamPos_canUse.BottomRight.X ||
                    imageYMax > tgtCamPos_canUse.BottomRight.Y ||
                    imageXMin < tgtCamPos_canUse.BottomLeft.X ||
                    imageYMax > tgtCamPos_canUse.BottomLeft.Y)
                {
                    throw (new Exception("The captured Wall image is out of range of camera. Please select less cabinet again or edit [Camera Height] in [Configuration] tab so that the distance to Wall is longer."));
                }
            }


        }


#else

                // added by Hotta 2022/06/22 for カメラ位置
                void SetCamPosTarget()
        {
            tgtCamPos = new MarkerPosition();
            tgtCamPos_canUse = new MarkerPosition();
            tgtCamPos_HorLineSpec = new float[2];
            tgtCamPos_VerLineSpec = new float[2];

            TransformImage.TransformImage transform = new TransformImage.TransformImage();

            if (m_lstCamPosUnits.Count <= 0)
            {
                tgtCamPos = null;
                return;
            }

            List<int> listX = new List<int>();
            List<int> listY = new List<int>();
            foreach (UnitInfo unit in m_lstCamPosUnits)
            {
                if (listX.Contains(unit.X) != true)
                { listX.Add(unit.X); }
                if (listY.Contains(unit.Y) != true)
                { listY.Add(unit.Y); }
            }
            // modified by Hotta 2022/07/29
            /*
            m_CabinetXNum = listX.Count;
            m_CabinetYNum = listY.Count;
            */
            m_CabinetXNum_CamPos = listX.Count;
            m_CabinetYNum_CamPos = listY.Count;

            if (allocInfo.LEDModel == ZRD_C12A 
                || allocInfo.LEDModel == ZRD_B12A 
                || allocInfo.LEDModel == ZRD_CH12D 
                || allocInfo.LEDModel == ZRD_BH12D
                || allocInfo.LEDModel == ZRD_CH12D_S3
                || allocInfo.LEDModel == ZRD_BH12D_S3)
            {
                m_LedPitch = (float)RealPitch_P12;

                m_CabinetDx = CabinetDxP12;
                m_CabinetDy = CabinetDyP12;

                if (allocInfo.LEDModel == ZRD_C12A
                    || allocInfo.LEDModel == ZRD_B12A)
                {
                    m_ModuleDx = ModuleDxP12_Mdoule4x2;
                    m_ModuleDy = ModuleDyP12_Module4x2;
                }
                else
                {
                    m_ModuleDx = ModuleDxP12_Module4x3;
                    m_ModuleDy = ModuleDyP12_Module4x3;
                }
            }
            else if (allocInfo.LEDModel == ZRD_C15A 
                || allocInfo.LEDModel == ZRD_B15A 
                || allocInfo.LEDModel == ZRD_CH15D 
                || allocInfo.LEDModel == ZRD_BH15D
                || allocInfo.LEDModel == ZRD_CH15D_S3
                || allocInfo.LEDModel == ZRD_BH15D_S3)
            {
                m_LedPitch = (float)RealPitch_P15;

                m_CabinetDx = CabinetDxP15;
                m_CabinetDy = CabinetDyP15;

                if (allocInfo.LEDModel == ZRD_C15A 
                    || allocInfo.LEDModel == ZRD_B15A)
                {
                    m_ModuleDx = ModuleDxP15_Module4x2;
                    m_ModuleDy = ModuleDyP15_Module4x2;
                }
                else
                {
                    m_ModuleDx = ModuleDxP15_Module4x3;
                    m_ModuleDy = ModuleDyP15_Module4x3;
                }
            }
            else
            { return; }

            m_ModuleXNum = m_CabinetDx / m_ModuleDx;
            m_ModuleYNum = m_CabinetDy / m_ModuleDy;

            // deleted by Hotta 2022/07/29
            /*
            m_PanelDx = m_CabinetDx * m_CabinetXNum;
            m_PanelDy = m_CabinetDy * m_CabinetYNum;
            */

            m_CameraParam = new CameraParameter(16.0f, 23.5f, 15.6f, 1024, 680);

            int notUse = (int)(m_CameraParam.SensorResH * 0.07 + 0.5);
            tgtCamPos_canUse.TopLeft.X = notUse;
            tgtCamPos_canUse.TopLeft.Y = notUse;
            tgtCamPos_canUse.TopRight.X = m_CameraParam.SensorResH - notUse;
            tgtCamPos_canUse.TopRight.Y = notUse;
            tgtCamPos_canUse.BottomRight.X = m_CameraParam.SensorResH - notUse;
            tgtCamPos_canUse.BottomRight.Y = m_CameraParam.SensorResV - notUse;
            tgtCamPos_canUse.BottomLeft.X = notUse;
            tgtCamPos_canUse.BottomLeft.Y = m_CameraParam.SensorResV - notUse;

#if NO_CONTROLLER
            // HP224
            float k;
            if (allocInfo.LEDModel == ZRD_C15A 
                || allocInfo.LEDModel == ZRD_B15A 
                || allocInfo.LEDModel == ZRD_CH15D 
                || allocInfo.LEDModel == ZRD_BH15D
                || allocInfo.LEDModel == ZRD_CH15D_S3
                || allocInfo.LEDModel == ZRD_BH15D_S3)
            {
                k = 476.0f / (m_LedPitch * m_CabinetDx * CabinetLength_P15);
            }
            else
            {
                k = 476.0f / (m_LedPitch * m_CabinetDx * CabinetLength_P12);
            }

            //k = 1;
#else
            k = 1;
#endif

            //float cabinetWidth = (m_PanelCorner[1].X - m_PanelCorner[0].X) / m_CabinetXNum;
            //float cabinetHeight = (m_PanelCorner[3].Y - m_PanelCorner[0].Y) / m_CabinetYNum;
            float cabinetWidth = m_LedPitch * m_CabinetDx;
            float cabinetHeight = m_LedPitch * m_CabinetDy;

#if NO_CONTROLLER
            // HP224
            cabinetWidth *= k;
            cabinetHeight *= k;
#endif

            float transZ = 4000.0f;

            /*
            float lenX = m_PanelDx * m_LedPitch;
            float lenY = m_PanelDy * m_LedPitch;

            m_PanelCorner = new Point3f[4];
            m_PanelCorner[0] = new Point3f(-lenX / 2, -lenY / 2, 0.0f);
            m_PanelCorner[1] = new Point3f(lenX / 2, -lenY / 2, 0.0f);
            m_PanelCorner[2] = new Point3f(lenX / 2, lenY / 2, 0.0f);
            m_PanelCorner[3] = new Point3f(-lenX / 2, lenY / 2, 0.0f);
            */

#if NO_CONTROLLER
            // HP P224
            transZ *= k;    // 393.5f;
                            //
#endif
            if (allocInfo.LEDModel == ZRD_C15A 
                || allocInfo.LEDModel == ZRD_B15A 
                || allocInfo.LEDModel == ZRD_CH15D 
                || allocInfo.LEDModel == ZRD_BH15D
                || allocInfo.LEDModel == ZRD_CH15D_S3 
                || allocInfo.LEDModel == ZRD_BH15D_S3)
            { 
                transZ *= (float)(RealPitch_P15 / RealPitch_P12);
            }
            m_Transration = new float[] { 0, 0, transZ };

            m_Rotate = new float[] { 0.0f, 0.0f, 0.0f };

            m_ShiftToCameraCoordinate = new float[] { 0, 0, 0 };

            m_CameraParam = new CameraParameter(16.0f, 23.5f, 15.6f, 1024, 680);

            //// Cabinetのサイズ*1/4に、タイル表示
            // フラット
            // modified by Hotta 2022/07/29
            /*
            transform.Set3DPoints(0, -cabinetWidth * ((float)m_CabinetXNum / 2) + cabinetWidth * (1.0f / 4), -cabinetHeight * ((float)m_CabinetYNum / 2) + cabinetHeight * (1.0f / 4), 0);
            transform.Set3DPoints(1, +cabinetWidth * ((float)m_CabinetXNum / 2) - cabinetWidth * (1.0f / 4), -cabinetHeight * ((float)m_CabinetYNum / 2) + cabinetHeight * (1.0f / 4), 0);
            transform.Set3DPoints(2, +cabinetWidth * ((float)m_CabinetXNum / 2) - cabinetWidth * (1.0f / 4), +cabinetHeight * ((float)m_CabinetYNum / 2) - cabinetHeight * (1.0f / 4), 0);
            transform.Set3DPoints(3, -cabinetWidth * ((float)m_CabinetXNum / 2) + cabinetWidth * (1.0f / 4), +cabinetHeight * ((float)m_CabinetYNum / 2) - cabinetHeight * (1.0f / 4), 0);
            */
            transform.Set3DPoints(0, -cabinetWidth * ((float)m_CabinetXNum_CamPos / 2) + cabinetWidth * (1.0f / 4), -cabinetHeight * ((float)m_CabinetYNum_CamPos / 2) + cabinetHeight * (1.0f / 4), 0);
            transform.Set3DPoints(1, +cabinetWidth * ((float)m_CabinetXNum_CamPos / 2) - cabinetWidth * (1.0f / 4), -cabinetHeight * ((float)m_CabinetYNum_CamPos / 2) + cabinetHeight * (1.0f / 4), 0);
            transform.Set3DPoints(2, +cabinetWidth * ((float)m_CabinetXNum_CamPos / 2) - cabinetWidth * (1.0f / 4), +cabinetHeight * ((float)m_CabinetYNum_CamPos / 2) - cabinetHeight * (1.0f / 4), 0);
            transform.Set3DPoints(3, -cabinetWidth * ((float)m_CabinetXNum_CamPos / 2) + cabinetWidth * (1.0f / 4), +cabinetHeight * ((float)m_CabinetYNum_CamPos / 2) - cabinetHeight * (1.0f / 4), 0);
            //

            transform.SetTranslation(m_Transration[0], m_Transration[1], m_Transration[2]);

            transform.SetRx(m_Rotate[0]);
            transform.SetRy(m_Rotate[1]);
            transform.SetRz(m_Rotate[2]);

            transform.SetShiftToCameraCoordinate(m_ShiftToCameraCoordinate[0], m_ShiftToCameraCoordinate[1], m_ShiftToCameraCoordinate[2]);


            transform.CameraParameter.Set(m_CameraParam.f, m_CameraParam.SensorSizeH, m_CameraParam.SensorSizeV, m_CameraParam.SensorResH, m_CameraParam.SensorResV);

            transform.Calc();

            tgtCamPos.TopLeft.X = transform.ImagePoints[0].X;
            tgtCamPos.TopLeft.Y = transform.ImagePoints[0].Y;
            tgtCamPos.TopRight.X = transform.ImagePoints[1].X;
            tgtCamPos.TopRight.Y = transform.ImagePoints[1].Y;
            tgtCamPos.BottomRight.X = transform.ImagePoints[2].X;
            tgtCamPos.BottomRight.Y = transform.ImagePoints[2].Y;
            tgtCamPos.BottomLeft.X = transform.ImagePoints[3].X;
            tgtCamPos.BottomLeft.Y = transform.ImagePoints[3].Y;

            // modified by Hotta 2022/08/01
            /*
            transZ = 4000.0f * 1.10f;
            */
            transZ = 4000.0f * (1.0f / (float)Settings.Ins.GapCam.CamPos_SizeMin);

#if NO_CONTROLLER
            // HP P224
            transZ *= k;    // 393.5f * 1.10f;
                            //
#endif
            if (allocInfo.LEDModel == ZRD_C15A 
                || allocInfo.LEDModel == ZRD_B15A 
                || allocInfo.LEDModel == ZRD_CH15D 
                || allocInfo.LEDModel == ZRD_BH15D
                || allocInfo.LEDModel == ZRD_CH15D_S3
                || allocInfo.LEDModel == ZRD_BH15D_S3)
            { 
                transZ *= (float)(RealPitch_P15 / RealPitch_P12);
            }

            transform.SetTranslation(m_Transration[0], m_Transration[1], transZ);

            transform.Calc();
            tgtCamPos_HorLineSpec[0] = transform.ImagePoints[1].X - transform.ImagePoints[0].X;
            tgtCamPos_VerLineSpec[0] = transform.ImagePoints[3].Y - transform.ImagePoints[0].Y;

            // modified by Hotta 2022/08/01
            /*
            transZ = 4000.0f * 0.95f;
            */
            transZ = 4000.0f * (1.0f / (float)Settings.Ins.GapCam.CamPos_SizeMax);

#if NO_CONTROLLER
            // HP P224
            transZ *= k;    // 393.5f * 0.95f;
                            //
#endif
            if (allocInfo.LEDModel == ZRD_C15A 
                || allocInfo.LEDModel == ZRD_B15A 
                || allocInfo.LEDModel == ZRD_CH15D 
                || allocInfo.LEDModel == ZRD_BH15D
                || allocInfo.LEDModel == ZRD_CH15D_S3
                || allocInfo.LEDModel == ZRD_BH15D_S3)
            { 
                transZ *= (float)(RealPitch_P15 / RealPitch_P12);
            }

            transform.SetTranslation(m_Transration[0], m_Transration[1], transZ);

            transform.Calc();

            tgtCamPos_HorLineSpec[1] = transform.ImagePoints[1].X - transform.ImagePoints[0].X;
            tgtCamPos_VerLineSpec[1] = transform.ImagePoints[3].Y - transform.ImagePoints[0].Y;
        }

#endif

        private void timerGapCam_Tick(object sender, EventArgs e)
        {
            // modified by Hotta 2022/05/24 for カメラ位置決め
#if CameraPosition
            try
            {
                // added by Hotta 2022/07/25
                timerGapCam.Enabled = false;
                AdjustCameraPosition((System.Windows.Forms.Timer)sender, imgGapCamCameraView, tbtnGapCamSetPos);
            }
#else
            try { SetPosMain((System.Windows.Forms.Timer)sender, aryUnitGapCam, imgGapCamCameraView, tbtnGapCamSetPos); }
#endif
            catch (Exception ex)
            {
                // added by Hotta 2023/04/28                    
                // ThroughMode設定を解除 1Step
                SetThroughMode(false);

                // ユーザー設定に書き戻し
                setUserSettingSetPos(userSetting);

                // 内部信号OFF
                stopIntSig();
                //

                timerGapCam.Enabled = false;
                tbtnGapCamSetPos.IsChecked = false;
                tbtnGapCamSetPos_Click(sender, new RoutedEventArgs());

                ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
            }
        }


        int m_MaxNumOfAdjustment;
        bool m_EvaluateAdjustmentResult;

        private async void btnGapCamMeasStart_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher dispatcher = Application.Current.Dispatcher;
            
            bool status = true;
            bool waitFlag = false;

            actionButton(sender, "Measurement Gap Start.");

            // Unitが選択されているか、矩形になっているか再度確認
            List<UnitInfo> lstTgtUnit;
            try
            {
                // modified by Hotta 2022/12/22
                /*
                CheckSelectedUnits(aryUnitGapCam, out lstTgtUnit);
                */
                CheckSelectedUnits(aryUnitGapCam, out lstTgtUnit, true, out m_lstCamPosUnits, true);
                //
            }
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                // タブの表示ページを切り替え
                tcGapCamView.SelectedIndex = 0;
                releaseButton(sender, "Measurement Gap Done.");
                //tcMain.IsEnabled = false;
                return;
            }

            // modified by Hotta 2024/12/26
            /*
            winProgress = new WindowProgress("Measurement Gap Progress");
            */
            winProgress = new WindowProgress("Measurement Gap Progress", 180, 400, WindowProgress.TAbortType.Measurement);
            //
            winProgress.Show();
            winProgress.ShowMessage("Start Measurement.");

            // added by Hotta 2022/07/26
            // AdjustCameraPosition()実行中に、ボタンが押されたときの対策　（このとき、timerGapCam.Enabled = falseで、下のif()はtrueにならない）
            if (tbtnGapCamSetPos.IsChecked == true)
            {
                timerGapCam.Enabled = false;
                tbtnGapCamSetPos.IsChecked = false;
                tbtnGapCamSetPos_Click(sender, new RoutedEventArgs());
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
                    tcGapCamView.SelectedIndex = 0;
                    releaseButton(sender, "Measurement Gap Done.");
                    return;
                }
                //
            }
            //

            if (timerGapCam.Enabled == true)
            {
                timerGapCam.Enabled = false;
                tbtnGapCamSetPos.IsChecked = false;
                tbtnGapCamSetPos_Click(sender, new RoutedEventArgs());
                waitFlag = true;
            }

            tcMain.IsEnabled = false;

            // added by Hotta 2022/09/01
            clearGapResult(DispType.Measure);
            //

            // タブの表示ページを切り替え
            tcGapCamView.SelectedIndex = 4;

            if (waitFlag == true)
            { Thread.Sleep(5000); } // 画像の取得が完全に終わるまで待つ

            // added by Hotta 2023/01/23
            m_lstUserSetting = null;
            //

            // modified by Hotta 2023/01/23
            /*
            try { await Task.Run(() => measureGapAsync(lstTgtUnit)); }
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                status = false;
            }
            */

            // moved by Tei 2024/12/24
#if NO_CAP
#else
            // フォルダの作成
            m_CamMeasPath = applicationPath + MeasDir + "Gap_" + DateTime.Now.ToString("yyyyMMddHHmm") + "\\";
#endif
            if (Directory.Exists(m_CamMeasPath) == false)
            { Directory.CreateDirectory(m_CamMeasPath); }

            saveLog("Start Measurement.");

            int processSec = initialGapCameraMeasurementProcessSec(lstTgtUnit.Count);
            winProgress.StartRemainTimer(processSec);
            winProgress.ShowMessage("Initial Settings.");

            try
            {
                try
                {
                    await Task.Run(() => measureGapAsync(lstTgtUnit));
                }
                finally
                {
                    if (m_lstUserSetting != null)
                    {
                        // modified by Hotta 2025/02/05
                        /*
                        // modified by Hotta 2023/04/28
                        //setNormalSetting();
                        SetThroughMode(false);
                        //
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
                }
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
                status = false;
            }
            //

            winProgress.StopRemainTimer();

            string msg = "";
            string caption = "";
            if (status == true)
            {
                msg = "Measurement Gap Complete!";
                caption = "Complete";
            }
            else
            {
                msg = "Failed in Measurement Gap.";
                caption = "Error";
                dispGapResult(true);
            }

            winProgress.Close();

            playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
            WindowMessage winMessage = new WindowMessage(msg, caption);
            winMessage.ShowDialog();

            releaseButton(sender, "Measurement Gap Done.");
            tcMain.IsEnabled = true;
        }


        // added by Hotta 2023/01/23
        List<UserSetting> m_lstUserSetting;
        //

        //
        private async void btnGapCamAdjStart_Click(object sender, RoutedEventArgs e)
        {
            bool status = true;
            bool waitFlag = false;

            actionButton(sender, "Adjustment Gap Start.");

            // Unitが選択されているか、矩形になっているか再度確認
            List<UnitInfo> lstTgtUnit;
            try
            {
                // modified by Hotta 2022/12/22
                /*
                CheckSelectedUnits(aryUnitGapCam, out lstTgtUnit);
                */
                CheckSelectedUnits(aryUnitGapCam, out lstTgtUnit, true, out m_lstCamPosUnits, true);
                //
            }
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                // タブの表示ページを切り替え
                tcGapCamView.SelectedIndex = 0;
                releaseButton(sender, "Adjustment Gap Done.");
                //tcMain.IsEnabled = true;
                return;
            }

            // modified by Hotta 2024/12/25
            /*
            winProgress = new WindowProgress("Adjustment Gap Progress");
            */
            winProgress = new WindowProgress("Adjustment Gap Progress", /*170*/180, 400, WindowProgress.TAbortType.Adjustment);

            winProgress.Show();
            winProgress.ShowMessage("Start Adjustment.");
            
            // moved by Tei 2024/12/24
#if NO_CAP
            m_CamMeasPath = m_CamMeasPath_0;
#else
            // フォルダの作成
            m_CamMeasPath = applicationPath + MeasDir + "Gap_" + DateTime.Now.ToString("yyyyMMddHHmm") + "\\";
#endif

            if (Directory.Exists(m_CamMeasPath) == false)
            { Directory.CreateDirectory(m_CamMeasPath); }

            saveLog("Start Adjustment Gap.");


            // added by Hotta 2022/07/26
            // AdjustCameraPosition()実行中に、ボタンが押されたときの対策　（このとき、timerGapCam.Enabled = falseで、下のif()はtrueにならない）
            if (tbtnGapCamSetPos.IsChecked == true)
            {
                timerGapCam.Enabled = false;
                tbtnGapCamSetPos.IsChecked = false;
                tbtnGapCamSetPos_Click(sender, new RoutedEventArgs());
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
                    tcGapCamView.SelectedIndex = 0;
                    releaseButton(sender, "Adjustment Gap Done.");
                    return;
                }
                //
            }
            //

            if (timerGapCam.Enabled == true)
            {
                timerGapCam.Enabled = false;
                tbtnGapCamSetPos.IsChecked = false;
                tbtnGapCamSetPos_Click(sender, new RoutedEventArgs());
                waitFlag = true;
            }

            tcMain.IsEnabled = false;

            // added by Hotta 2022/09/01
            clearGapResult(DispType.Before);
            clearGapResult(DispType.Result);
            //

            // タブの表示ページを切り替え
            tcGapCamView.SelectedIndex = 2;

            if (waitFlag == true)
            { Thread.Sleep(5000); } // 画像の取得が完全に終わるまで待つ

            // added by Hotta 2023/01/23
            m_lstUserSetting = null;
            //

            try
            {
                m_MaxNumOfAdjustment = int.Parse((string)comboBoxGapCameraNumOfAdjustment.SelectedItem);
                m_EvaluateAdjustmentResult = (bool)checkEnableAdjustmentResult.IsChecked;
                // modified by Hotta 2023/01/23
                /*
                await Task.Run(() => adjustGapRegAsync(lstTgtUnit));
                */

                int processSec = initialGapCameraAdjustmentProcessSec(lstTgtUnit.Count);
                winProgress.StartRemainTimer(processSec);

                try
                {
                    await Task.Run(() => adjustGapRegAsync(lstTgtUnit));
                }
                finally
                {
                    if (m_lstUserSetting != null)
                    {
                        // modified by Hotta 2025/02/05
                        /*
                        // modified by Hotta 2023/04/28
                        //setNormalSetting();
                        SetThroughMode(false);
                        //
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
                }
                //
            }
            // added by Hotta 2025/01/31
            catch(CameraCasUserAbortException ex)
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

            // added by Hotta 2022/11/24 for そのままWriteData実行
#if Auto_WriteData
            // added by Hotta 2022/12/21
            winProgress.StopRemainTimer();
            winProgress.Close();

            saveLog("Finish Adjustment Gap.");

            //
            string msg = "";
            string caption = "";

            if (status == true)
            {
                releaseButton(sender, "Adjustment Gap Done.");

                btnGapCamRomStart.IsEnabled = true;
                winProgress.Close();
                playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");

                bool? result;
                showMessageWindow(out result, "Adjustment Gap Complete!\r\nDo you want to continue Writing Data?", "Confirm", System.Drawing.SystemIcons.Question, 500, 200, "Yes", "No");

                // WriteData実行
                if (result == true)
                {
                    actionButton(sender, "Writing Gap correction value to ROM Start.");

                    winProgress = new WindowProgress("Writing Gap correction value to ROM Progress");
                    winProgress.Show();

                    int processSec = initialGapCameraROMWriteProcessSec();
                    winProgress.StartRemainTimer(processSec);

                    status = true;
                    try { await Task.Run(() => romSaveAsync(lstTgtUnit)); }
                    catch (Exception ex)
                    {
                        ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                        status = false;
                    }

                    winProgress.Close();

                    if (status == true)
                    {
                        releaseButton(sender, "Writing Gap correction value to ROM Done.");
                        //dispGapResult();
                        msg = "Writing Gap correction value to ROM Complete!";
                        caption = "Complete";
                    }
                    else
                    {
                        releaseButton(sender, "Failed in writing Gap corection value to ROM.");
                        dispGapResult(true);
                        msg = "Failed in writing Gap corection value to ROM.";
                        caption = "Error";
                    }

                    winProgress.StopRemainTimer();
                }
                // WriteData中止
                else
                {
                    //dispGapResult(true);
                    msg = "Abort to Writing Data.";
                    caption = "Abort";
                    releaseButton(sender, "Writing Gap correction value to ROM Done.");
                }
            }
            else
            {
                releaseButton(sender, "Failed in Adjustment Gap.");

                dispGapResult(true);
                msg = "Failed in Adjustment Gap.";
                caption = "Error";
            }

            playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
            WindowMessage winMessage = new WindowMessage(msg, caption);
            winMessage.ShowDialog();
#else
            if (status == true)
            {
                msg = "Adjustment Gap Complete!\r\nPlease check, and click \"Write Data\"";
                caption = "Complete";

                // modified by Hotta 2021/01/25
                /*
                // 調整が成功した場合、再度Measureを実行するまで再調整できないようにする
                btnGapCamAdjStart.IsEnabled = false;
                */
                btnGapCamRomStart.IsEnabled = true;
            }
            else
            {
                msg = "Failed in Adjustment Gap.";
                caption = "Error";
                dispGapResult(true);
            }

            winProgress.Close();

            playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
            WindowMessage winMessage = new WindowMessage(msg, caption);
            winMessage.ShowDialog();
#endif
            tcMain.IsEnabled = true;
        }

        private async void btnGapCamRomStart_Click(object sender, RoutedEventArgs e)
        {
            bool status = true;
            bool waitFlag = false;

            actionButton(sender, "Writing Gap correction value to ROM Start.");

            // moved by Hotta 2023/03/23
            /*
            winProgress = new WindowProgress("Writing Gap correction value to ROM Progress");
            winProgress.Show();
            */

            if (timerGapCam.Enabled == true)
            {
                timerGapCam.Enabled = false;
                tbtnGapCamSetPos.IsChecked = false;
                tbtnGapCamSetPos_Click(sender, new RoutedEventArgs());
                waitFlag = true;
            }
            tcMain.IsEnabled = false;

            if (waitFlag == true)
            { Thread.Sleep(5000); } // 画像の取得が完全に終わるまで待つ

            // Unitが選択されているか、矩形になっているか再度確認
            List<UnitInfo> lstTgtUnit;

            try
            { CheckSelectedUnits(aryUnitGapCam, out lstTgtUnit); }
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);

                // タブの表示ページを切り替え
                tcGapCamView.SelectedIndex = 0;
                releaseButton(sender, "Writing Gap correction value to ROM Done.");
                tcMain.IsEnabled = true;
                return;
            }

            // タブの表示ページを切り替え
            tcGapCamView.SelectedIndex = 3;

            // moved by Hotta 2023/03/23
            //
            winProgress = new WindowProgress("Writing Gap correction value to ROM Progress");
            winProgress.Show();
            //

            try { await Task.Run(() => romSaveAsync(lstTgtUnit)); }
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                status = false;
            }

            string msg = "";
            string caption = "";
            if (status == true)
            {
                msg = "Writing Gap correction value to ROM Complete!";
                caption = "Complete";
            }
            else
            {
                msg = "Failed in writing Gap corection value to ROM.";
                caption = "Error";
                dispGapResult(true);
            }

            winProgress.Close();

            playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
            WindowMessage winMessage = new WindowMessage(msg, caption);
            winMessage.ShowDialog();

            releaseButton(sender, "Writing Gap correction value to ROM Done.");

            tcMain.IsEnabled = true;
        }


        private void cmbxPatternGapCam_DropDownClosed(object sender, EventArgs e)
        {
            if (cmbxPatternGapCam != null)
            {
                int level = 0;
                actionButton(sender, "Set Plane Signal");
                if (cmbxPatternGapCam.SelectedIndex == 0)
                    level = (int)(Math.Pow(0.05, (1.0 / 2.2)) * 1023 + 0.5);
                else if (cmbxPatternGapCam.SelectedIndex == 1)
                    level = (int)(Math.Pow(0.10, (1.0 / 2.2)) * 1023 + 0.5);
                else if (cmbxPatternGapCam.SelectedIndex == 2)
                    level = (int)(Math.Pow(0.20, (1.0 / 2.2)) * 1023 + 0.5);
                else if (cmbxPatternGapCam.SelectedIndex == 3)
                    level = (int)(Math.Pow(0.50, (1.0 / 2.2)) * 1023 + 0.5);
                else if (cmbxPatternGapCam.SelectedIndex == 4)
                    level = (int)(Math.Pow(1.00, (1.0 / 2.2)) * 1023 + 0.5);

                outputIntSigFlat(level, level, level);
                releaseButton(sender);
            }
        }

        private void btnSelectAllGapCam_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            { actionButton(sender, "Select All"); }

            for (int i = 0; i < allocInfo.MaxX; i++)
            {
                for (int j = 0; j < allocInfo.MaxY; j++)
                {
                    if (aryUnitGapCam[i, j].UnitInfo != null)
                    { Dispatcher.Invoke(new Action(() => { aryUnitGapCam[i, j].IsChecked = true; })); }
                }
            }

            if (sender != null)
            {
                System.Threading.Thread.Sleep(600);
                releaseButton(sender);
            }
        }

        private void btnDeselectAllGapCam_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            { actionButton(sender, "Deselect All"); }

            for (int i = 0; i < allocInfo.MaxX; i++)
            {
                for (int j = 0; j < allocInfo.MaxY; j++)
                {
                    if (aryUnitGapCam[i, j].UnitInfo != null)
                    { Dispatcher.Invoke(new Action(() => { aryUnitGapCam[i, j].IsChecked = false; })); }
                }
            }

            if (sender != null)
            {
                System.Threading.Thread.Sleep(600);
                releaseButton(sender);
            }
        }




        enum CorrectPos { TopLeft, TopRight, RightTop, RightBottom };
        //m_GapContrast[CabinetX, CabinetY, ModuleX, ModuleY, TopLeft/TopRight/RightTop/RightBottom]
        double[,,,,] m_GapContrast;
        bool[,,,,] m_WarningCorrectValue;

        // added by Hotta 2022/12/13
#if Spec_by_Zdistance
        double[,,,,] m_GapContrastSpec;
#endif


        private void btnGapCamMeasResultOpen_Click(object sender, RoutedEventArgs e)
        {
            // modified by Hotta 2022/07/13
#if Coverity
            // ダイアログのインスタンスを生成
            using (var dialog = new OpenFileDialog())
            {
                dialog.InitialDirectory = "c:\\cas\\measure";
                dialog.FileName = "measure.bmp";
                dialog.Filter = "ビットマップファイル(*.bmp)|*.bmp";

                // ダイアログを表示する
                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                Bitmap canvas = (Bitmap)Bitmap.FromFile(dialog.FileName);

                System.Windows.Media.Imaging.BitmapSource bitmapSource;

                // MemoryStreamを利用した変換処理
                using (var ms = new System.IO.MemoryStream())
                {
                    // MemoryStreamに書き出す
                    canvas.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                    // MemoryStreamをシーク
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    // MemoryStreamからBitmapFrameを作成
                    // (BitmapFrameはBitmapSourceを継承しているのでそのまま渡せばOK)
                    bitmapSource =
                        System.Windows.Media.Imaging.BitmapFrame.Create(
                            ms,
                            System.Windows.Media.Imaging.BitmapCreateOptions.None,
                            System.Windows.Media.Imaging.BitmapCacheOption.OnLoad
                        );
                }
                imgGapCamBefore.Source = bitmapSource;
            }
#else
            // ダイアログのインスタンスを生成
            var dialog = new OpenFileDialog();

            dialog.InitialDirectory = "c:\\cas\\measure";
            dialog.FileName = "measure.bmp";
            dialog.Filter = "ビットマップファイル(*.bmp)|*.bmp";

            // ダイアログを表示する
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            Bitmap canvas = (Bitmap)Bitmap.FromFile(dialog.FileName);

            System.Windows.Media.Imaging.BitmapSource bitmapSource;

            // MemoryStreamを利用した変換処理
            using (var ms = new System.IO.MemoryStream())
            {
                // MemoryStreamに書き出す
                canvas.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                // MemoryStreamをシーク
                ms.Seek(0, System.IO.SeekOrigin.Begin);
                // MemoryStreamからBitmapFrameを作成
                // (BitmapFrameはBitmapSourceを継承しているのでそのまま渡せばOK)
                bitmapSource =
                    System.Windows.Media.Imaging.BitmapFrame.Create(
                        ms,
                        System.Windows.Media.Imaging.BitmapCreateOptions.None,
                        System.Windows.Media.Imaging.BitmapCacheOption.OnLoad
                    );
            }
            imgGapCamBefore.Source = bitmapSource;
#endif
        }

        private void btnGapCamAdjResultOpen_Click(object sender, RoutedEventArgs e)
        {

            // modified by Hotta 2022/07/13
#if Coverity
            // ダイアログのインスタンスを生成
            using (var dialog = new OpenFileDialog())
            {
                dialog.InitialDirectory = "c:\\cas\\measure";
                dialog.FileName = "adjust.bmp";
                dialog.Filter = "ビットマップファイル(*.bmp)|*.bmp";

                // ダイアログを表示する
                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                Bitmap canvas = (Bitmap)Bitmap.FromFile(dialog.FileName);

                System.Windows.Media.Imaging.BitmapSource bitmapSource;

                // MemoryStreamを利用した変換処理
                using (var ms = new System.IO.MemoryStream())
                {
                    // MemoryStreamに書き出す
                    canvas.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                    // MemoryStreamをシーク
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    // MemoryStreamからBitmapFrameを作成
                    // (BitmapFrameはBitmapSourceを継承しているのでそのまま渡せばOK)
                    bitmapSource =
                        System.Windows.Media.Imaging.BitmapFrame.Create(
                            ms,
                            System.Windows.Media.Imaging.BitmapCreateOptions.None,
                            System.Windows.Media.Imaging.BitmapCacheOption.OnLoad
                        );
                }
                imgGapCamResult.Source = bitmapSource;
            }
#else
            // ダイアログのインスタンスを生成
            var dialog = new OpenFileDialog();

            dialog.InitialDirectory = "c:\\cas\\measure";
            dialog.FileName = "adjust.bmp";
            dialog.Filter = "ビットマップファイル(*.bmp)|*.bmp";

            // ダイアログを表示する
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            Bitmap canvas = (Bitmap)Bitmap.FromFile(dialog.FileName);

            System.Windows.Media.Imaging.BitmapSource bitmapSource;

            // MemoryStreamを利用した変換処理
            using (var ms = new System.IO.MemoryStream())
            {
                // MemoryStreamに書き出す
                canvas.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                // MemoryStreamをシーク
                ms.Seek(0, System.IO.SeekOrigin.Begin);
                // MemoryStreamからBitmapFrameを作成
                // (BitmapFrameはBitmapSourceを継承しているのでそのまま渡せばOK)
                bitmapSource =
                    System.Windows.Media.Imaging.BitmapFrame.Create(
                        ms,
                        System.Windows.Media.Imaging.BitmapCreateOptions.None,
                        System.Windows.Media.Imaging.BitmapCacheOption.OnLoad
                    );
            }
            imgGapCamResult.Source = bitmapSource;
#endif
        }


        Bitmap makeResultImage()
        {
            int startX = 50;
            int startY = 50;

            int moduleX = 80;
            int moduleY = 90;

            //描画先とするImageオブジェクトを作成する
            Bitmap canvas = new Bitmap(startX * 2 + moduleX * m_ModuleXNum * m_CabinetXNum, startY * 2 + moduleY * m_ModuleYNum * m_CabinetYNum);


            // modified by Hotta 2022/07/13
#if Coverity
            //ImageオブジェクトのGraphicsオブジェクトを作成する
            using (Graphics g = Graphics.FromImage(canvas))
            {

                //全体をグレーで塗りつぶす
                // modified by Hotta 2022/07/13
#if Coverity
                System.Drawing.Color color = System.Drawing.Color.FromArgb(0x40, 0x40, 0x40);
                using (System.Drawing.Brush br = new SolidBrush(color))
                {
                    g.FillRectangle(br, g.VisibleClipBounds);
                }
#else
                g.FillRectangle(new SolidBrush(System.Drawing.Color.FromArgb(0x40, 0x40, 0x40)), g.VisibleClipBounds);
#endif

                // modified by Hotta 2022/07/13
#if Coverity
                // Module端を描く
                using (System.Drawing.Pen p = new Pen(Color.FromArgb(0x60, 0x60, 0x60), 1))
                {
                    for (int x = 0; x < m_CabinetXNum; x++)
                    {
                        for (int y = 0; y < m_CabinetYNum; y++)
                        {
                            for (int _x = 0; _x < m_ModuleXNum; _x++)
                            {
                                for (int _y = 0; _y < m_ModuleYNum; _y++)
                                {
                                    g.DrawRectangle(p, startX + moduleX * _x + (moduleX * m_ModuleXNum) * x, startY + moduleY * _y + (moduleY * m_ModuleYNum) * y, moduleX, moduleY);
                                }
                            }
                        }
                    }
                }

                // Cabinet端を描く
                using (System.Drawing.Pen p = new Pen(Color.FromArgb(0x80, 0x80, 0x80), 2))
                {
                    for (int x = 0; x < m_CabinetXNum; x++)
                    {
                        for (int y = 0; y < m_CabinetYNum; y++)
                        {
                            g.DrawRectangle(p, startX + (moduleX * m_ModuleXNum) * x, startY + (moduleY * m_ModuleYNum) * y, moduleX * m_ModuleXNum, moduleY * m_ModuleYNum);
                        }
                    }
                }
#else
            // Module端を描く
            System.Drawing.Pen p = new Pen(Color.FromArgb(0x60, 0x60, 0x60), 1);
            for (int x = 0; x < m_CabinetXNum; x++)
            {
                for (int y = 0; y < m_CabinetYNum; y++)
                {
                    for (int _x = 0; _x < m_ModuleXNum; _x++)
                    {
                        for (int _y = 0; _y < m_ModuleYNum; _y++)
                        {
                            g.DrawRectangle(p, startX + moduleX * _x + (moduleX * m_ModuleXNum) * x, startY + moduleY * _y + (moduleY * m_ModuleYNum) * y, moduleX, moduleY);
                        }
                    }
                }
            }

            // Cabinet端を描く
            p = new Pen(Color.FromArgb(0x80, 0x80, 0x80), 2);
            for (int x = 0; x < m_CabinetXNum; x++)
            {
                for (int y = 0; y < m_CabinetYNum; y++)
                {
                    g.DrawRectangle(p, startX + (moduleX * m_ModuleXNum) * x, startY + (moduleY * m_ModuleYNum) * y, moduleX * m_ModuleXNum, moduleY * m_ModuleYNum);
                }
            }
#endif

#region Cabinetのインデックスを書く
                //フォントオブジェクトの作成
                //Font fnt = new Font("Yu Gothic UI", 24);
                //for (int x = 0; x < m_CabinetXNum; x++)
                //{
                //    int originX = startX + (moduleX * m_ModuleXNum) * x + (moduleX * m_ModuleXNum / 2) - 20;
                //    int originY = startY - 50;
                //    g.DrawString(string.Format("{0}", x+1), fnt, Brushes.Cyan, originX, originY);
                //}
                //for (int y = 0; y < m_CabinetYNum; y++)
                //{
                //    int originX = startX - 50;
                //    int originY = startY + (moduleY * m_ModuleYNum) * y + (moduleY * m_ModuleYNum / 2);
                //    g.DrawString(string.Format("{0}", y+1), fnt, Brushes.Cyan, originX, originY - 20);
                //}
#endregion

#region Cabinet/Moduleのインデックスを書く
                //fnt = new Font("Yu Gothic UI", 10);
                //for (int x = 0; x < m_CabinetXNum; x++)
                //{
                //    for (int y = 0; y < m_CabinetYNum; y++)
                //    {
                //        for (int _x = 0; _x < m_ModuleXNum; _x++)
                //        {
                //            for (int _y = 0; _y < m_ModuleYNum; _y++)
                //            {
                //                int px = startX + moduleX * _x + (moduleX * m_ModuleXNum) * x + moduleX / 2 - 15;
                //                int py = startY + moduleY * _y + (moduleY * m_ModuleYNum) * y + moduleY / 2 - 10;
                //                g.DrawString((x+1).ToString() + "-" + (y+1).ToString() + "_" + (_y * m_ModuleXNum + _x + 1).ToString(), fnt, Brushes.Cyan, px, py);
                //            }
                //        }
                //    }
                //}
#endregion

#region コントラスト値を書く
                //フォントオブジェクトの作成
                //fnt = new Font("Yu Gothic UI", 8);
                //for (int x = 0; x < m_CabinetXNum; x++)
                //{
                //    for (int y = 0; y < m_CabinetYNum; y++)
                //    {
                //        for (int _x = 0; _x < m_ModuleXNum; _x++)
                //        {
                //            for (int _y = 0; _y < m_ModuleYNum; _y++)
                //            {
                //                int originX = startX + moduleX * _x + (moduleX * m_ModuleXNum) * x;
                //                int originY = startY + moduleY * _y + (moduleY * m_ModuleYNum) * y;
                //                for(CorrectPos pos = CorrectPos.TopLeft; pos <= CorrectPos.RightBottom; pos++)
                //                {
                //                    double value = m_GapContrast[x, y, _x, _y, (int)pos];

                //                    if (double.IsNaN(value) == true)
                //                        continue;

                //                    int px = 0, py = 0;
                //                    if(pos == CorrectPos.TopLeft)
                //                    {
                //                        px = originX;
                //                        py = originY + 2;
                //                    }
                //                    else if(pos == CorrectPos.TopRight)
                //                    {
                //                        px = originX + 40;
                //                        py = originY + 2;
                //                    }
                //                    else if(pos == CorrectPos.RightTop)
                //                    {
                //                        px = originX + 60;
                //                        py = originY + 20;
                //                    }
                //                    else if(pos == CorrectPos.RightBottom)
                //                    {
                //                        px = originX + 60;
                //                        py = originY + 60;
                //                    }

                //                    Brush br;
                //                    if (Math.Abs(value) > 0.016)
                //                    {
                //                        br = Brushes.Red;
                //                    }
                //                    else
                //                    {
                //                        if(m_WarningCorrectValue[x, y, _x, _y, (int)pos] == true)
                //                            br = Brushes.Yellow;
                //                        else
                //                            br = Brushes.Lime;
                //                    }

                //                    g.DrawString((value * 100).ToString("+0.00;-0.00;0.00"), fnt, br, px, py);

                //                }
                //            }
                //        }
                //    }
                //}
#endregion

                // コントラスト値に応じて、ライン色を変えて描画
                for (int x = 0; x < m_CabinetXNum; x++)
                {
                    for (int y = 0; y < m_CabinetYNum; y++)
                    {
                        for (int _x = 0; _x < m_ModuleXNum; _x++)
                        {
                            for (int _y = 0; _y < m_ModuleYNum; _y++)
                            {
                                int originX = startX + moduleX * _x + (moduleX * m_ModuleXNum) * x;
                                int originY = startY + moduleY * _y + (moduleY * m_ModuleYNum) * y;

                                // modified by Hotta 2022/07/07
#if CorrectTargetEdge
                                //public enum CorrectPosition { TopLeft, TopRight, RightTop, RightBottom, BottomLeft, BottomRight, LeftTop, LeftBottom };
                                for (CorrectPosition pos = CorrectPosition.TopLeft; pos <= CorrectPosition.LeftBottom; pos++)
                                {
                                    double value = m_GapContrast[x, y, _x, _y, (int)pos];

                                    // added by Hotta 2022/12/13
#if Spec_by_Zdistance
                                    double spec = m_GapContrastSpec[x, y, _x, _y, (int)pos];
#endif

                                    if (double.IsNaN(value) == true)
                                        continue;

                                    System.Drawing.Point p1 = new System.Drawing.Point(), p2 = new System.Drawing.Point();
                                    if (pos == CorrectPosition.TopLeft)
                                    {
                                        p1 = new System.Drawing.Point(originX, originY);
                                        p2 = new System.Drawing.Point(originX + moduleX / 2, originY);
                                    }
                                    else if (pos == CorrectPosition.TopRight)
                                    {
                                        p1 = new System.Drawing.Point(originX + moduleX / 2, originY);
                                        p2 = new System.Drawing.Point(originX + moduleX, originY);
                                    }
                                    else if (pos == CorrectPosition.RightTop)
                                    {
                                        p1 = new System.Drawing.Point(originX + moduleX, originY);
                                        p2 = new System.Drawing.Point(originX + moduleX, originY + moduleY / 2);
                                    }
                                    else if (pos == CorrectPosition.RightBottom)
                                    {
                                        p1 = new System.Drawing.Point(originX + moduleX, originY + moduleY / 2);
                                        p2 = new System.Drawing.Point(originX + moduleX, originY + moduleY);
                                    }
                                    else if (pos == CorrectPosition.BottomLeft)
                                    {
                                        p1 = new System.Drawing.Point(originX, originY + moduleY);
                                        p2 = new System.Drawing.Point(originX + moduleX / 2, originY + moduleY);
                                    }
                                    else if (pos == CorrectPosition.BottomRight)
                                    {
                                        p1 = new System.Drawing.Point(originX + moduleX / 2, originY + moduleY);
                                        p2 = new System.Drawing.Point(originX + moduleX, originY + moduleY);
                                    }
                                    else if (pos == CorrectPosition.LeftTop)
                                    {
                                        p1 = new System.Drawing.Point(originX, originY);
                                        p2 = new System.Drawing.Point(originX, originY + moduleY / 2);
                                    }
                                    else if (pos == CorrectPosition.LeftBottom)
                                    {
                                        p1 = new System.Drawing.Point(originX, originY + moduleY / 2);
                                        p2 = new System.Drawing.Point(originX, originY + moduleY);
                                    }
                                    // modified by Hotta 2022/12/13
#if Spec_by_Zdistance
                                    if (Math.Abs(value) > spec)
                                    {
                                        g.DrawLine(new Pen(Color.Red, 6), p1, p2);
                                    }
#else
                                    if (Math.Abs(value) > Settings.Ins.GapCam.AdjustSpec)
                                    {
                                        g.DrawLine(new Pen(Color.Red, 6), p1, p2);
                                    }
#endif
                                }
#else
                                    for (CorrectPos pos = CorrectPos.TopLeft; pos <= CorrectPos.RightBottom; pos++)
                                {
                                    double value = m_GapContrast[x, y, _x, _y, (int)pos];

                                    if (double.IsNaN(value) == true)
                                        continue;

                                    System.Drawing.Point p1 = new System.Drawing.Point(), p2 = new System.Drawing.Point();
                                    if (pos == CorrectPos.TopLeft)
                                    {
                                        p1 = new System.Drawing.Point(originX, originY);
                                        p2 = new System.Drawing.Point(originX + moduleX / 2, originY);
                                    }
                                    else if (pos == CorrectPos.TopRight)
                                    {
                                        p1 = new System.Drawing.Point(originX + moduleX / 2, originY);
                                        p2 = new System.Drawing.Point(originX + moduleX, originY);
                                    }
                                    else if (pos == CorrectPos.RightTop)
                                    {
                                        p1 = new System.Drawing.Point(originX + moduleX, originY);
                                        p2 = new System.Drawing.Point(originX + moduleX, originY + moduleY / 2);
                                    }
                                    else if (pos == CorrectPos.RightBottom)
                                    {
                                        p1 = new System.Drawing.Point(originX + moduleX, originY + moduleY / 2);
                                        p2 = new System.Drawing.Point(originX + moduleX, originY + moduleY);
                                    }
                                    // 緑色も黄色も、描かない
                                    /*
                                    System.Drawing.Pen pen;
                                    if (Math.Abs(value) > Settings.Ins.GapCam.AdjustSpec)
                                    {
                                        pen = new Pen(Color.Red, 6);
                                    }
                                    else
                                    {
                                        if (m_WarningCorrectValue[x, y, _x, _y, (int)pos] == true)
                                            pen = new Pen(Color.Yellow, 3);
                                        else
                                            pen = new Pen(Color.Lime, 3);
                                    }
                                    g.DrawLine(pen, p1, p2);
                                    */
                                    if (Math.Abs(value) > Settings.Ins.GapCam.AdjustSpec)
                                    {
                                        g.DrawLine(new Pen(Color.Red, 6), p1, p2);
                                    }
                                }
#endif
                            }
                        }
                    }
                }

                //リソースを解放する
                //fnt.Dispose();
            }

#else
            //ImageオブジェクトのGraphicsオブジェクトを作成する
            Graphics g = Graphics.FromImage(canvas);

            //全体をグレーで塗りつぶす
            // modified by Hotta 2022/07/13
#if Coverity
            System.Drawing.Color color = System.Drawing.Color.FromArgb(0x40, 0x40, 0x40);
            using(System.Drawing.Brush br  = new SolidBrush(color))
            {
                g.FillRectangle(br, g.VisibleClipBounds);
            }
#else
            g.FillRectangle(new SolidBrush(System.Drawing.Color.FromArgb(0x40, 0x40, 0x40)), g.VisibleClipBounds);
#endif

            // modified by Hotta 2022/07/13
#if Coverity
            // Module端を描く
            using (System.Drawing.Pen p = new Pen(Color.FromArgb(0x60, 0x60, 0x60), 1))
            {
                for (int x = 0; x < m_CabinetXNum; x++)
                {
                    for (int y = 0; y < m_CabinetYNum; y++)
                    {
                        for (int _x = 0; _x < m_ModuleXNum; _x++)
                        {
                            for (int _y = 0; _y < m_ModuleYNum; _y++)
                            {
                                g.DrawRectangle(p, startX + moduleX * _x + (moduleX * m_ModuleXNum) * x, startY + moduleY * _y + (moduleY * m_ModuleYNum) * y, moduleX, moduleY);
                            }
                        }
                    }
                }
            }

            // Cabinet端を描く
            using (System.Drawing.Pen p = new Pen(Color.FromArgb(0x80, 0x80, 0x80), 2))
            {
                for (int x = 0; x < m_CabinetXNum; x++)
                {
                    for (int y = 0; y < m_CabinetYNum; y++)
                    {
                        g.DrawRectangle(p, startX + (moduleX * m_ModuleXNum) * x, startY + (moduleY * m_ModuleYNum) * y, moduleX * m_ModuleXNum, moduleY * m_ModuleYNum);
                    }
                }
            }
#else
            // Module端を描く
            System.Drawing.Pen p = new Pen(Color.FromArgb(0x60, 0x60, 0x60), 1);
            for (int x = 0; x < m_CabinetXNum; x++)
            {
                for (int y = 0; y < m_CabinetYNum; y++)
                {
                    for (int _x = 0; _x < m_ModuleXNum; _x++)
                    {
                        for (int _y = 0; _y < m_ModuleYNum; _y++)
                        {
                            g.DrawRectangle(p, startX + moduleX * _x + (moduleX * m_ModuleXNum) * x, startY + moduleY * _y + (moduleY * m_ModuleYNum) * y, moduleX, moduleY);
                        }
                    }
                }
            }

            // Cabinet端を描く
            p = new Pen(Color.FromArgb(0x80, 0x80, 0x80), 2);
            for (int x = 0; x < m_CabinetXNum; x++)
            {
                for (int y = 0; y < m_CabinetYNum; y++)
                {
                    g.DrawRectangle(p, startX + (moduleX * m_ModuleXNum) * x, startY + (moduleY * m_ModuleYNum) * y, moduleX * m_ModuleXNum, moduleY * m_ModuleYNum);
                }
            }
#endif

#region Cabinetのインデックスを書く
            //フォントオブジェクトの作成
            //Font fnt = new Font("Yu Gothic UI", 24);
            //for (int x = 0; x < m_CabinetXNum; x++)
            //{
            //    int originX = startX + (moduleX * m_ModuleXNum) * x + (moduleX * m_ModuleXNum / 2) - 20;
            //    int originY = startY - 50;
            //    g.DrawString(string.Format("{0}", x+1), fnt, Brushes.Cyan, originX, originY);
            //}
            //for (int y = 0; y < m_CabinetYNum; y++)
            //{
            //    int originX = startX - 50;
            //    int originY = startY + (moduleY * m_ModuleYNum) * y + (moduleY * m_ModuleYNum / 2);
            //    g.DrawString(string.Format("{0}", y+1), fnt, Brushes.Cyan, originX, originY - 20);
            //}
#endregion

#region Cabinet/Moduleのインデックスを書く
            //fnt = new Font("Yu Gothic UI", 10);
            //for (int x = 0; x < m_CabinetXNum; x++)
            //{
            //    for (int y = 0; y < m_CabinetYNum; y++)
            //    {
            //        for (int _x = 0; _x < m_ModuleXNum; _x++)
            //        {
            //            for (int _y = 0; _y < m_ModuleYNum; _y++)
            //            {
            //                int px = startX + moduleX * _x + (moduleX * m_ModuleXNum) * x + moduleX / 2 - 15;
            //                int py = startY + moduleY * _y + (moduleY * m_ModuleYNum) * y + moduleY / 2 - 10;
            //                g.DrawString((x+1).ToString() + "-" + (y+1).ToString() + "_" + (_y * m_ModuleXNum + _x + 1).ToString(), fnt, Brushes.Cyan, px, py);
            //            }
            //        }
            //    }
            //}
#endregion

#region コントラスト値を書く
            //フォントオブジェクトの作成
            //fnt = new Font("Yu Gothic UI", 8);
            //for (int x = 0; x < m_CabinetXNum; x++)
            //{
            //    for (int y = 0; y < m_CabinetYNum; y++)
            //    {
            //        for (int _x = 0; _x < m_ModuleXNum; _x++)
            //        {
            //            for (int _y = 0; _y < m_ModuleYNum; _y++)
            //            {
            //                int originX = startX + moduleX * _x + (moduleX * m_ModuleXNum) * x;
            //                int originY = startY + moduleY * _y + (moduleY * m_ModuleYNum) * y;
            //                for(CorrectPos pos = CorrectPos.TopLeft; pos <= CorrectPos.RightBottom; pos++)
            //                {
            //                    double value = m_GapContrast[x, y, _x, _y, (int)pos];

            //                    if (double.IsNaN(value) == true)
            //                        continue;

            //                    int px = 0, py = 0;
            //                    if(pos == CorrectPos.TopLeft)
            //                    {
            //                        px = originX;
            //                        py = originY + 2;
            //                    }
            //                    else if(pos == CorrectPos.TopRight)
            //                    {
            //                        px = originX + 40;
            //                        py = originY + 2;
            //                    }
            //                    else if(pos == CorrectPos.RightTop)
            //                    {
            //                        px = originX + 60;
            //                        py = originY + 20;
            //                    }
            //                    else if(pos == CorrectPos.RightBottom)
            //                    {
            //                        px = originX + 60;
            //                        py = originY + 60;
            //                    }

            //                    Brush br;
            //                    if (Math.Abs(value) > 0.016)
            //                    {
            //                        br = Brushes.Red;
            //                    }
            //                    else
            //                    {
            //                        if(m_WarningCorrectValue[x, y, _x, _y, (int)pos] == true)
            //                            br = Brushes.Yellow;
            //                        else
            //                            br = Brushes.Lime;
            //                    }

            //                    g.DrawString((value * 100).ToString("+0.00;-0.00;0.00"), fnt, br, px, py);

            //                }
            //            }
            //        }
            //    }
            //}
#endregion

            // コントラスト値に応じて、ライン色を変えて描画
            for (int x = 0; x < m_CabinetXNum; x++)
            {
                for (int y = 0; y < m_CabinetYNum; y++)
                {
                    for (int _x = 0; _x < m_ModuleXNum; _x++)
                    {
                        for (int _y = 0; _y < m_ModuleYNum; _y++)
                        {
                            int originX = startX + moduleX * _x + (moduleX * m_ModuleXNum) * x;
                            int originY = startY + moduleY * _y + (moduleY * m_ModuleYNum) * y;

                            // modified by Hotta 2022/07/07
#if CorrectTargetEdge
                            //public enum CorrectPosition { TopLeft, TopRight, RightTop, RightBottom, BottomLeft, BottomRight, LeftTop, LeftBottom };
                            for (CorrectPosition pos = CorrectPosition.TopLeft; pos <= CorrectPosition.LeftBottom; pos++)
                            {
                                double value = m_GapContrast[x, y, _x, _y, (int)pos];

                                if (double.IsNaN(value) == true)
                                    continue;

                                System.Drawing.Point p1 = new System.Drawing.Point(), p2 = new System.Drawing.Point();
                                if (pos == CorrectPosition.TopLeft)
                                {
                                    p1 = new System.Drawing.Point(originX, originY);
                                    p2 = new System.Drawing.Point(originX + moduleX / 2, originY);
                                }
                                else if (pos == CorrectPosition.TopRight)
                                {
                                    p1 = new System.Drawing.Point(originX + moduleX / 2, originY);
                                    p2 = new System.Drawing.Point(originX + moduleX, originY);
                                }
                                else if (pos == CorrectPosition.RightTop)
                                {
                                    p1 = new System.Drawing.Point(originX + moduleX, originY);
                                    p2 = new System.Drawing.Point(originX + moduleX, originY + moduleY / 2);
                                }
                                else if (pos == CorrectPosition.RightBottom)
                                {
                                    p1 = new System.Drawing.Point(originX + moduleX, originY + moduleY / 2);
                                    p2 = new System.Drawing.Point(originX + moduleX, originY + moduleY);
                                }
                                else if (pos == CorrectPosition.BottomLeft)
                                {
                                    p1 = new System.Drawing.Point(originX, originY + moduleY);
                                    p2 = new System.Drawing.Point(originX + moduleX / 2, originY + moduleY);
                                }
                                else if (pos == CorrectPosition.BottomRight)
                                {
                                    p1 = new System.Drawing.Point(originX + moduleX / 2, originY + moduleY);
                                    p2 = new System.Drawing.Point(originX + moduleX, originY + moduleY);
                                }
                                else if (pos == CorrectPosition.LeftTop)
                                {
                                    p1 = new System.Drawing.Point(originX, originY);
                                    p2 = new System.Drawing.Point(originX, originY + moduleY / 2);
                                }
                                else if (pos == CorrectPosition.LeftBottom)
                                {
                                    p1 = new System.Drawing.Point(originX, originY + moduleY / 2);
                                    p2 = new System.Drawing.Point(originX, originY + moduleY);
                                }
                                if (Math.Abs(value) > Settings.Ins.GapCam.AdjustSpec)
                                {
                                    g.DrawLine(new Pen(Color.Red, 6), p1, p2);
                                }
                            }
#else
                            for (CorrectPos pos = CorrectPos.TopLeft; pos <= CorrectPos.RightBottom; pos++)
                            {
                                double value = m_GapContrast[x, y, _x, _y, (int)pos];

                                if (double.IsNaN(value) == true)
                                    continue;

                                System.Drawing.Point p1 = new System.Drawing.Point(), p2 = new System.Drawing.Point();
                                if (pos == CorrectPos.TopLeft)
                                {
                                    p1 = new System.Drawing.Point(originX, originY);
                                    p2 = new System.Drawing.Point(originX + moduleX / 2, originY);
                                }
                                else if (pos == CorrectPos.TopRight)
                                {
                                    p1 = new System.Drawing.Point(originX + moduleX / 2, originY);
                                    p2 = new System.Drawing.Point(originX + moduleX, originY);
                                }
                                else if (pos == CorrectPos.RightTop)
                                {
                                    p1 = new System.Drawing.Point(originX + moduleX, originY);
                                    p2 = new System.Drawing.Point(originX + moduleX, originY + moduleY / 2);
                                }
                                else if (pos == CorrectPos.RightBottom)
                                {
                                    p1 = new System.Drawing.Point(originX + moduleX, originY + moduleY / 2);
                                    p2 = new System.Drawing.Point(originX + moduleX, originY + moduleY);
                                }
                                // 緑色も黄色も、描かない
                                /*
                                System.Drawing.Pen pen;
                                if (Math.Abs(value) > Settings.Ins.GapCam.AdjustSpec)
                                {
                                    pen = new Pen(Color.Red, 6);
                                }
                                else
                                {
                                    if (m_WarningCorrectValue[x, y, _x, _y, (int)pos] == true)
                                        pen = new Pen(Color.Yellow, 3);
                                    else
                                        pen = new Pen(Color.Lime, 3);
                                }
                                g.DrawLine(pen, p1, p2);
                                */
                                if (Math.Abs(value) > Settings.Ins.GapCam.AdjustSpec)
                                {
                                    g.DrawLine(new Pen(Color.Red, 6), p1, p2);
                                }
                            }
#endif
                        }
                    }
                }
            }

            //リソースを解放する
            //fnt.Dispose();
            g.Dispose();
#endif

            return canvas;
        }

        Bitmap clearResultImage()
        {
            int startX = 50;
            int startY = 50;

            int moduleX = 80;
            int moduleY = 90;

            //描画先とするImageオブジェクトを作成する
            Bitmap canvas = new Bitmap(startX * 2 + moduleX * m_ModuleXNum * m_CabinetXNum, startY * 2 + moduleY * m_ModuleYNum * m_CabinetYNum);

            // modified by Hotta 2022/07/13
#if Coverity
            //ImageオブジェクトのGraphicsオブジェクトを作成する
            using (Graphics g = Graphics.FromImage(canvas))
            {

                //全体をグレーで塗りつぶす
                // modified by Hotta 2022/07/13
#if Coverity
                System.Drawing.Color color = System.Drawing.Color.FromArgb(0x40, 0x40, 0x40);
                using (System.Drawing.Brush br = new SolidBrush(color))
                {
                    g.FillRectangle(br, g.VisibleClipBounds);
                }
#else
                g.FillRectangle(new SolidBrush(System.Drawing.Color.FromArgb(0x40, 0x40, 0x40)), g.VisibleClipBounds);
#endif


            }
#else
            //ImageオブジェクトのGraphicsオブジェクトを作成する
            Graphics g = Graphics.FromImage(canvas);

            //全体をグレーで塗りつぶす
            g.FillRectangle(new SolidBrush(System.Drawing.Color.FromArgb(0x40, 0x40, 0x40)), g.VisibleClipBounds);

            //リソースを解放する
            g.Dispose();
#endif
            return canvas;
        }



        // MeasurementのDrag指定用
        private void utbGapCam_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            rectAreaGapCam.Visibility = System.Windows.Visibility.Visible;
            rectAreaGapCam.Margin = new Thickness(e.GetPosition(this).X - 2, e.GetPosition(this).Y - 48, 0, 0);

            startPos = new System.Windows.Point(rectAreaGapCam.Margin.Left, rectAreaGapCam.Margin.Top);
        }

        private void utbGapCam_PreviewMouseUp(object sender, MouseButtonEventArgs e)
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

            double offsetX = tcMain.Margin.Left + gdGapCam.Margin.Left + gdTcViewGapCam.Margin.Left + gdAllocGapCam.Margin.Left + gdAllocGapCamLayout.Margin.Left + cabinetAllocColumnHeaderWidth - svGapCam.HorizontalOffset;
            double offsetY = tcMain.Margin.Top + gdGapCam.Margin.Top + gdTcViewGapCam.Margin.Top + tbiGapCamUnitAlloc.Height + gdAllocGapCam.Margin.Top + gdAllocGapCamLayout.Margin.Top + cabinetAllocRowHeaderHeight - svGapCam.VerticalOffset;

            for (int y = 0; y < allocInfo.MaxY; y++)
            {
                for (int x = 0; x < allocInfo.MaxX; x++)
                {
                    if (aryUnitGapCam[x, y].IsEnabled == false)
                    { continue; }

                    if (isContainedDragArea(aryUnitGapCam[x, y], area, offsetX, offsetY) == true)
                    {
                        if (aryUnitGapCam[x, y].IsChecked == true)
                        { aryUnitGapCam[x, y].IsChecked = false; }
                        else
                        { aryUnitGapCam[x, y].IsChecked = true; }
                    }
                }
            }

            rectAreaGapCam.Visibility = System.Windows.Visibility.Hidden;
        }

        private void utbGapCam_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            double height = e.GetPosition(this).Y - 48 - startPos.Y;
            try
            {
                if (height >= 0)
                { rectAreaGapCam.Height = height; }
                else
                {
                    rectAreaGapCam.Margin = new Thickness(rectAreaGapCam.Margin.Left, startPos.Y + height, 0, 0);
                    rectAreaGapCam.Height = Math.Abs(height);
                }
            }
            catch { rectAreaGapCam.Height = 0; }

            double width = e.GetPosition(this).X - 2 - startPos.X;
            try
            {
                if (width >= 0)
                { rectAreaGapCam.Width = width; }
                else
                {
                    rectAreaGapCam.Margin = new Thickness(startPos.X + width, rectAreaGapCam.Margin.Top, 0, 0);
                    rectAreaGapCam.Width = Math.Abs(width);
                }
            }
            catch { rectAreaGapCam.Width = 0; }
        }



        //////////////////////////////////////////////////////
        /// added by Hotta 2022/01/14
        /// 目地補正　結果表示
        /// 
        /// 
        /// マウス押下中フラグ
        bool isMouseLeftButtonDown = false;

        /// マウスを押下した点を保存
        System.Windows.Point MouseDonwStartPoint = new System.Windows.Point(0, 0);

        /// マウスの現在地
        System.Windows.Point MouseCurrentPoint = new System.Windows.Point(0, 0);

        /// マウス押下
        private void imgGapCamBefore_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // クリックした位置を保存
            // 位置の基準にするControlはなんでもいいが、MouseMoveのほうの基準Controlと合わせること。
            MouseDonwStartPoint = e.GetPosition(this);

            isMouseLeftButtonDown = true;
        }

        /// マウス離す
        private void imgGapCamBefore_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            isMouseLeftButtonDown = false;
        }

        /// マウスがコントロールの上から外れた
        private void imgGapCamBefore_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            isMouseLeftButtonDown = false;
        }

        /// マウスがコントロールの上を移動
        private void imgGapCamBefore_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            imgGapCamBefore.Focus();

            if (isMouseLeftButtonDown == false) return;

            // マウスの現在位置座標を取得（ScrollViewerからの相対位置）
            // ここは、位置の基準にするControl(GetPositionの引数)はScrollViewrでもthis(Window自体)でもなんでもいい。
            // Start時とマウス移動時の差分がわかりさえすればよし。
            MouseCurrentPoint = e.GetPosition(this);

            // 移動開始点と現在位置の差から、MouseMoveイベント1回分の移動量を算出
            double offsetX = MouseCurrentPoint.X - MouseDonwStartPoint.X;
            double offsetY = MouseCurrentPoint.Y - MouseDonwStartPoint.Y;

            // 動かす対象の図形からMatrixオブジェクトを取得
            // このMatrixオブジェクトを用いて図形を描画上移動させる
            Matrix matrix = ((MatrixTransform)imgGapCamBefore.RenderTransform).Matrix;

            // TranslateメソッドにX方向とY方向の移動量を渡し、移動後の状態を計算
            matrix.Translate(offsetX, offsetY);

            // 移動後の状態を計算したMatrixオブジェクトを描画に反映する
            imgGapCamBefore.RenderTransform = new MatrixTransform(matrix);

            // 移動開始点を現在位置で更新する
            // （今回の現在位置が次回のMouseMoveイベントハンドラで使われる移動開始点となる）
            MouseDonwStartPoint = MouseCurrentPoint;
        }

        /// ホイールくるくる
        private void imgGapCamBefore_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var scale = 1.0;
            Matrix matrix = ((MatrixTransform)imgGapCamBefore.RenderTransform).Matrix;

            // ScaleAt()の拡大中心点(引数3,4個目)に渡すための座標をとるときの基準Controlは、拡大縮小をしたいものの一つ上のControlにすること。
            // ここでは拡大縮小するGridを包んでいるScrollViewerを基準にした。
            MouseCurrentPoint = e.GetPosition(this);

            // ホイール上に回す→拡大 / 下に回す→縮小
            if (e.Delta > 0) scale = 1.25;
            else scale = 1 / 1.25;

            // 拡大実施
            matrix.ScaleAt(scale, scale, MouseCurrentPoint.X, MouseCurrentPoint.Y);
            imgGapCamBefore.RenderTransform = new MatrixTransform(matrix);
        }

        private void imgGapCamBefore_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var scale = 1.0;
            Matrix matrix = ((MatrixTransform)imgGapCamBefore.RenderTransform).Matrix;

            // ScaleAt()の拡大中心点(引数3,4個目)に渡すための座標をとるときの基準Controlは、拡大縮小をしたいものの一つ上のControlにすること。
            // ここでは拡大縮小するGridを包んでいるScrollViewerを基準にした。
            //MouseCurrentPoint = e.GetPosition(this);
            // マウス座標から論理座標に変換
            System.Drawing.Point dp = System.Windows.Forms.Cursor.Position;
            System.Windows.Point wp = new System.Windows.Point(dp.X, dp.Y);
            PresentationSource src = PresentationSource.FromVisual(this);
            CompositionTarget ct = src.CompositionTarget;
            System.Windows.Point p = ct.TransformFromDevice.Transform(wp);
            MouseCurrentPoint = p;

            // 'u'→拡大 / 'd'→縮小
            if (e.Key == Key.U) scale = 1.25;
            else if (e.Key == Key.D) scale = 1 / 1.25;

            // 拡大実施
            matrix.ScaleAt(scale, scale, MouseCurrentPoint.X, MouseCurrentPoint.Y);
            imgGapCamBefore.RenderTransform = new MatrixTransform(matrix);
        }

        ///////////////////////////////

        /// マウス押下
        private void imgGapCamResult_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // クリックした位置を保存
            // 位置の基準にするControlはなんでもいいが、MouseMoveのほうの基準Controlと合わせること。
            MouseDonwStartPoint = e.GetPosition(this);

            isMouseLeftButtonDown = true;
        }

        /// マウス離す
        private void imgGapCamResult_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            isMouseLeftButtonDown = false;
        }

        /// マウスがコントロールの上から外れた
        private void imgGapCamResult_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            isMouseLeftButtonDown = false;
        }

        /// マウスがコントロールの上を移動
        private void imgGapCamResult_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            imgGapCamResult.Focus();

            if (isMouseLeftButtonDown == false) return;

            // マウスの現在位置座標を取得（ScrollViewerからの相対位置）
            // ここは、位置の基準にするControl(GetPositionの引数)はScrollViewrでもthis(Window自体)でもなんでもいい。
            // Start時とマウス移動時の差分がわかりさえすればよし。
            MouseCurrentPoint = e.GetPosition(this);

            // 移動開始点と現在位置の差から、MouseMoveイベント1回分の移動量を算出
            double offsetX = MouseCurrentPoint.X - MouseDonwStartPoint.X;
            double offsetY = MouseCurrentPoint.Y - MouseDonwStartPoint.Y;

            // 動かす対象の図形からMatrixオブジェクトを取得
            // このMatrixオブジェクトを用いて図形を描画上移動させる
            Matrix matrix = ((MatrixTransform)imgGapCamResult.RenderTransform).Matrix;

            // TranslateメソッドにX方向とY方向の移動量を渡し、移動後の状態を計算
            matrix.Translate(offsetX, offsetY);

            // 移動後の状態を計算したMatrixオブジェクトを描画に反映する
            imgGapCamResult.RenderTransform = new MatrixTransform(matrix);

            // 移動開始点を現在位置で更新する
            // （今回の現在位置が次回のMouseMoveイベントハンドラで使われる移動開始点となる）
            MouseDonwStartPoint = MouseCurrentPoint;
        }

        /// ホイールくるくる
        private void imgGapCamResult_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scale = 1.0;
            Matrix matrix = ((MatrixTransform)imgGapCamResult.RenderTransform).Matrix;

            // ScaleAt()の拡大中心点(引数3,4個目)に渡すための座標をとるときの基準Controlは、拡大縮小をしたいものの一つ上のControlにすること。
            // ここでは拡大縮小するGridを包んでいるScrollViewerを基準にした。
            MouseCurrentPoint = e.GetPosition(this);

            // ホイール上に回す→拡大 / 下に回す→縮小
            if (e.Delta > 0) scale = 1.25;
            else scale = 1 / 1.25;

            // 拡大実施
            matrix.ScaleAt(scale, scale, MouseCurrentPoint.X, MouseCurrentPoint.Y);
            imgGapCamResult.RenderTransform = new MatrixTransform(matrix);
        }

        private void imgGapCamResult_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var scale = 1.0;
            Matrix matrix = ((MatrixTransform)imgGapCamResult.RenderTransform).Matrix;

            // ScaleAt()の拡大中心点(引数3,4個目)に渡すための座標をとるときの基準Controlは、拡大縮小をしたいものの一つ上のControlにすること。
            // ここでは拡大縮小するGridを包んでいるScrollViewerを基準にした。
            //MouseCurrentPoint = e.GetPosition(this);
            // マウス座標から論理座標に変換
            System.Drawing.Point dp = System.Windows.Forms.Cursor.Position;
            System.Windows.Point wp = new System.Windows.Point(dp.X, dp.Y);
            PresentationSource src = PresentationSource.FromVisual(this);
            CompositionTarget ct = src.CompositionTarget;
            System.Windows.Point p = ct.TransformFromDevice.Transform(wp);
            MouseCurrentPoint = p;

            // 'u'→拡大 / 'd'→縮小
            if (e.Key == Key.U) scale = 1.25;
            else if (e.Key == Key.D) scale = 1 / 1.25;

            // 拡大実施
            matrix.ScaleAt(scale, scale, MouseCurrentPoint.X, MouseCurrentPoint.Y);
            imgGapCamResult.RenderTransform = new MatrixTransform(matrix);
        }

        //////////////////////////////////////////////////////

        ///////////////////////////////

        /// マウス押下
        private void imgGapCamMeasure_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // クリックした位置を保存
            // 位置の基準にするControlはなんでもいいが、MouseMoveのほうの基準Controlと合わせること。
            MouseDonwStartPoint = e.GetPosition(this);

            isMouseLeftButtonDown = true;
        }

        /// マウス離す
        private void imgGapCamMeasure_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            isMouseLeftButtonDown = false;
        }

        /// マウスがコントロールの上から外れた
        private void imgGapCamMeasure_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            isMouseLeftButtonDown = false;
        }

        /// マウスがコントロールの上を移動
        private void imgGapCamMeasure_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            imgGapCamMeasure.Focus();

            if (isMouseLeftButtonDown == false) return;

            // マウスの現在位置座標を取得（ScrollViewerからの相対位置）
            // ここは、位置の基準にするControl(GetPositionの引数)はScrollViewrでもthis(Window自体)でもなんでもいい。
            // Start時とマウス移動時の差分がわかりさえすればよし。
            MouseCurrentPoint = e.GetPosition(this);

            // 移動開始点と現在位置の差から、MouseMoveイベント1回分の移動量を算出
            double offsetX = MouseCurrentPoint.X - MouseDonwStartPoint.X;
            double offsetY = MouseCurrentPoint.Y - MouseDonwStartPoint.Y;

            // 動かす対象の図形からMatrixオブジェクトを取得
            // このMatrixオブジェクトを用いて図形を描画上移動させる
            Matrix matrix = ((MatrixTransform)imgGapCamMeasure.RenderTransform).Matrix;

            // TranslateメソッドにX方向とY方向の移動量を渡し、移動後の状態を計算
            matrix.Translate(offsetX, offsetY);

            // 移動後の状態を計算したMatrixオブジェクトを描画に反映する
            imgGapCamMeasure.RenderTransform = new MatrixTransform(matrix);

            // 移動開始点を現在位置で更新する
            // （今回の現在位置が次回のMouseMoveイベントハンドラで使われる移動開始点となる）
            MouseDonwStartPoint = MouseCurrentPoint;
        }

        /// ホイールくるくる
        private void imgGapCamMeasure_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scale = 1.0;
            Matrix matrix = ((MatrixTransform)imgGapCamMeasure.RenderTransform).Matrix;

            // ScaleAt()の拡大中心点(引数3,4個目)に渡すための座標をとるときの基準Controlは、拡大縮小をしたいものの一つ上のControlにすること。
            // ここでは拡大縮小するGridを包んでいるScrollViewerを基準にした。
            MouseCurrentPoint = e.GetPosition(this);

            // ホイール上に回す→拡大 / 下に回す→縮小
            if (e.Delta > 0) scale = 1.25;
            else scale = 1 / 1.25;

            // 拡大実施
            matrix.ScaleAt(scale, scale, MouseCurrentPoint.X, MouseCurrentPoint.Y);
            imgGapCamMeasure.RenderTransform = new MatrixTransform(matrix);
        }

        private void imgGapCamMeasure_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var scale = 1.0;
            Matrix matrix = ((MatrixTransform)imgGapCamResult.RenderTransform).Matrix;

            // ScaleAt()の拡大中心点(引数3,4個目)に渡すための座標をとるときの基準Controlは、拡大縮小をしたいものの一つ上のControlにすること。
            // ここでは拡大縮小するGridを包んでいるScrollViewerを基準にした。
            //MouseCurrentPoint = e.GetPosition(this);
            // マウス座標から論理座標に変換
            System.Drawing.Point dp = System.Windows.Forms.Cursor.Position;
            System.Windows.Point wp = new System.Windows.Point(dp.X, dp.Y);
            PresentationSource src = PresentationSource.FromVisual(this);
            CompositionTarget ct = src.CompositionTarget;
            System.Windows.Point p = ct.TransformFromDevice.Transform(wp);
            MouseCurrentPoint = p;

            // 'u'→拡大 / 'd'→縮小
            if (e.Key == Key.U) scale = 1.25;
            else if (e.Key == Key.D) scale = 1 / 1.25;

            // 拡大実施
            matrix.ScaleAt(scale, scale, MouseCurrentPoint.X, MouseCurrentPoint.Y);
            imgGapCamMeasure.RenderTransform = new MatrixTransform(matrix);
        }
        //////////////////////////////////////////////////////


#endregion Events

#region Private Methods

#region Backup(Gap)

        unsafe private void backupGapRegAsync(string path)
        {
            List<GapCamCorrectionValue> lstGapCv = new List<GapCamCorrectionValue>();

            // modified by Hotta 2022/11/25
            /*
            int step = countUnits() * 13; // 13 : Unit=1 + Cell=12
            */
            int step = 0;

#if DEBUG
            if (Settings.Ins.ExecLog == true)
            {
                MainWindow.SaveExecLog("Method   : " + System.Reflection.MethodBase.GetCurrentMethod().Name);
                MainWindow.SaveExecLog("LEDModel : " + allocInfo.LEDModel);
            }
#endif
            if (allocInfo.LEDModel == ZRD_C12A
                || allocInfo.LEDModel == ZRD_B12A
                || allocInfo.LEDModel == ZRD_C15A
                || allocInfo.LEDModel == ZRD_B15A)
            {
                step = countUnits() * 8;
            }
            else
            {
                step = countUnits() * 12;
            }
            //

            winProgress.SetWholeSteps(step);
            // added by Hotta 2022/06/17 for Cancun
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
                // deleted by Hotta 2023/03/03
                /*
                m_LedPitch = (float)RealPitch_P12;
                */
                // added by Hotta 2023/04/28
#if NO_CONTROLLER
                m_LedPitch = (float)RealPitch_P12;
#endif

                m_CabinetDx = CabinetDxP12;
                m_CabinetDy = CabinetDyP12;

                if (allocInfo.LEDModel == ZRD_C12A 
                    || allocInfo.LEDModel == ZRD_B12A)
                {
                    m_ModuleDx = ModuleDxP12_Mdoule4x2;
                    m_ModuleDy = ModuleDyP12_Module4x2;
                }
                else
                {
                    m_ModuleDx = ModuleDxP12_Module4x3;
                    m_ModuleDy = ModuleDyP12_Module4x3;
                }
            }
            else if (allocInfo.LEDModel == ZRD_C15A 
                || allocInfo.LEDModel == ZRD_B15A 
                || allocInfo.LEDModel == ZRD_CH15D 
                || allocInfo.LEDModel == ZRD_BH15D
                || allocInfo.LEDModel == ZRD_CH15D_S3
                || allocInfo.LEDModel == ZRD_BH15D_S3)
            {
#if (DEBUG || Release_log)
                if (Settings.Ins.ExecLog == true)
                {
                    MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                }
#endif
                // deleted  by Hotta 2023/03/03
                /*
                m_LedPitch = (float)RealPitch_P15;
                */
                // added by Hotta 2023/04/28
#if NO_CONTROLLER
                m_LedPitch = (float)RealPitch_P15;
#endif

                m_CabinetDx = CabinetDxP15;
                m_CabinetDy = CabinetDyP15;

                if (allocInfo.LEDModel == ZRD_C15A 
                    || allocInfo.LEDModel == ZRD_B15A)
                {
                    m_ModuleDx = ModuleDxP15_Module4x2;
                    m_ModuleDy = ModuleDyP15_Module4x2;
                }
                else
                {
                    m_ModuleDx = ModuleDxP15_Module4x3;
                    m_ModuleDy = ModuleDyP15_Module4x3;
                }
            }
            else
            {
#if (DEBUG || Release_log)
                if (Settings.Ins.ExecLog == true)
                {
                    MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                }
#endif
                return;
            }

            m_ModuleXNum = m_CabinetDx / m_ModuleDx;
            m_ModuleYNum = m_CabinetDy / m_ModuleDy;


            for (int y = 0; y < allocInfo.MaxY; y++)
            {
                for (int x = 0; x < allocInfo.MaxX; x++)
                {
                    if (allocInfo.lstUnits[x][y] == null)
                    { 
                        continue;
                    }

                    GapCamCorrectionValue cv = new GapCamCorrectionValue(m_ModuleXNum * m_ModuleYNum);
                    cv.Unit = allocInfo.lstUnits[x][y];

                    // added by Hotta 2022/11/10 for Cabinet補正値なし
#if No_CabinetCorrectionValue

#else
                    // Unit
                    winProgress.ShowMessage("Get Cabinet Correction Value.");
                    winProgress.PutForward1Step();
                    getGapCvUnit(cv.Unit, ref cv.CvUnit);
#endif
                    // Cell
                    for (int cell = 0; cell < moduleCount; cell++)
                    {
                        winProgress.ShowMessage("Get Module Correction Value (Module-" + (cell + 1) + ").");
                        winProgress.PutForward1Step();
                        getGapCvCell(cv.Unit, cell + 1, ref cv.AryCvCell[cell]);  // +1 : Cell番号指定は1ベース
                    }

                    lstGapCv.Add(cv);
                }
            }

            GapCamCorrectionValue.SaveToXmlFile(path, lstGapCv);
        }

        private void getGapCvUnit(UnitInfo unit, ref GapCellCorrectValue cv)
        {
            cv = new GapCellCorrectValue();

            if (unit == null)
            { return; }

            Byte[] cmd = new byte[SDCPClass.CmdGapCorrectValueGet.Length];
            Array.Copy(SDCPClass.CmdGapCorrectValueGet, cmd, SDCPClass.CmdGapCorrectValueGet.Length);

            // Target
            cmd[9] += (byte)(unit.PortNo << 4);
            cmd[9] += (byte)unit.UnitNo;

            // TopLeft
            cmd[8] = 0;
            sendSdcpCommand(cmd, out string gap, dicController[unit.ControllerID].IPAddress);
            cv.TopLeft = Convert.ToInt32(gap, 16);

            // TopRight
            cmd[8] = 1;
            sendSdcpCommand(cmd, out gap, dicController[unit.ControllerID].IPAddress);
            cv.TopRight = Convert.ToInt32(gap, 16);

            // BottomLeft
            cmd[8] = 2;
            sendSdcpCommand(cmd, out gap, dicController[unit.ControllerID].IPAddress);
            cv.BottomLeft = Convert.ToInt32(gap, 16);

            // BottomRight
            cmd[8] = 3;
            sendSdcpCommand(cmd, out gap, dicController[unit.ControllerID].IPAddress);
            cv.BottomRight = Convert.ToInt32(gap, 16);

            // LeftTop
            cmd[8] = 4;
            sendSdcpCommand(cmd, out gap, dicController[unit.ControllerID].IPAddress);
            cv.LeftTop = Convert.ToInt32(gap, 16);

            // LeftBottom
            cmd[8] = 5;
            sendSdcpCommand(cmd, out gap, dicController[unit.ControllerID].IPAddress);
            cv.LeftBottom = Convert.ToInt32(gap, 16);

            // RightTop
            cmd[8] = 6;
            sendSdcpCommand(cmd, out gap, dicController[unit.ControllerID].IPAddress);
            cv.RightTop = Convert.ToInt32(gap, 16);

            // RightBottom
            cmd[8] = 7;
            sendSdcpCommand(cmd, out gap, dicController[unit.ControllerID].IPAddress);
            cv.RightBottom = Convert.ToInt32(gap, 16);

            // 全周同時取得に対応
            //cmd[8] = 0xFF;

            //sendSdcpCommand(cmd, out string gap, dicController[unit.ControllerID].IPAddress);

            //// 値の格納
            //cv.TopLeft = Convert.ToInt32(gap.Substring(0, 2), 16);
            //cv.TopRight = Convert.ToInt32(gap.Substring(2, 2), 16);

            //cv.BottomLeft = Convert.ToInt32(gap.Substring(4, 2), 16);
            //cv.BottomRight = Convert.ToInt32(gap.Substring(6, 2), 16);

            //cv.LeftTop = Convert.ToInt32(gap.Substring(8, 2), 16);
            //cv.LeftBottom = Convert.ToInt32(gap.Substring(10, 2), 16);

            //cv.RightTop = Convert.ToInt32(gap.Substring(12, 2), 16);
            //cv.RightBottom = Convert.ToInt32(gap.Substring(14, 2), 16);
        }

        private void getGapCvCell(UnitInfo unit, int cell, ref GapCellCorrectValue cv)
        {
            cv = new GapCellCorrectValue();

#if NO_CONTROLLER
            // Top-Left
            cv.TopLeft = 128;

            // Top-Right
            cv.TopRight = 128;

            // Left-Top
            cv.LeftTop = 128;

            // Right-Top
            cv.RightTop = 128;

            // Left-Bottom
            cv.LeftBottom = 128;

            // Right-Bottom
            cv.RightBottom = 128;

            // Bottom-Left
            cv.BottomLeft = 128;

            // Bottom-Right
            cv.BottomRight = 128;
#else
            if (unit == null)
            { return; }

            Byte[] cmd = new byte[SDCPClass.CmdGapCellCorrectValueGet.Length];
            Array.Copy(SDCPClass.CmdGapCellCorrectValueGet, cmd, SDCPClass.CmdGapCellCorrectValueGet.Length);

            // Target
            cmd[9] += (byte)(unit.PortNo << 4);
            cmd[9] += (byte)unit.UnitNo;

            cmd[8] = (byte)cell;

            sendSdcpCommand(cmd, out string gap, dicController[unit.ControllerID].IPAddress);

            if (gap == "0180")
            { return; }

            try
            {
                // Top-Left
                string strValue = gap.Substring(0, 2);
                cv.TopLeft = Convert.ToInt32(strValue, 16);

                // Top-Right
                strValue = gap.Substring(2, 2);
                cv.TopRight = Convert.ToInt32(strValue, 16);

                // Left-Top
                strValue = gap.Substring(4, 2);
                cv.LeftTop = Convert.ToInt32(strValue, 16);

                // Right-Top
                strValue = gap.Substring(6, 2);
                cv.RightTop = Convert.ToInt32(strValue, 16);

                // Left-Bottom
                strValue = gap.Substring(8, 2);
                cv.LeftBottom = Convert.ToInt32(strValue, 16);

                // Right-Bottom
                strValue = gap.Substring(10, 2);
                cv.RightBottom = Convert.ToInt32(strValue, 16);

                // Bottom-Left
                strValue = gap.Substring(12, 2);
                cv.BottomLeft = Convert.ToInt32(strValue, 16);

                // Bottom-Right
                strValue = gap.Substring(14, 2);
                cv.BottomRight = Convert.ToInt32(strValue, 16);
            }
            catch { return; }
#endif
        }


        private void getGapCvCell_InitializedValue(UnitInfo unit, int cell, ref GapCellCorrectValue cv)
        {
            cv = new GapCellCorrectValue();

            // Top-Left
            cv.TopLeft = 128;

            // Top-Right
            cv.TopRight = 128;

            // Left-Top
            cv.LeftTop = 128;

            // Right-Top
            cv.RightTop = 128;

            // Left-Bottom
            cv.LeftBottom = 128;

            // Right-Bottom
            cv.RightBottom = 128;

            // Bottom-Left
            cv.BottomLeft = 128;

            // Bottom-Right
            cv.BottomRight = 128;
        }


        private void restoreGapRegAsync(string path)
        {
            GapCamCorrectionValue.LoadFromXmlFile(path, out List<GapCamCorrectionValue> lstGapCv);

            int step = countUnits() * 13; // 1:Unit + 12:Cell
            winProgress.SetWholeSteps(step);

            lstModifiedUnits = new List<UnitInfo>();

            foreach (GapCamCorrectionValue value in lstGapCv)
            {
                // added by Hotta 2022/11/10 for Cabinet補正値なし
#if No_CabinetCorrectionValue

#else
                // Unit
                winProgress.ShowMessage("Set Cabinet Correction Value.\r\n(Cabinet : " + value.Unit.ControllerID + "-" + value.Unit.PortNo + "-" + value.Unit.UnitNo + ")");
                winProgress.PutForward1Step();
                setGapCvUnit(value.Unit, value.CvUnit);
#endif
                // Cell
                for (int cell = 0; cell < moduleCount; cell++)
                {
                    winProgress.ShowMessage("Set Module Correction Value.\r\n(Cabinet : " + value.Unit.ControllerID + "-" + value.Unit.PortNo + "-" + value.Unit.UnitNo + ", Module-" + (cell + 1) + ")");
                    winProgress.PutForward1Step();
                    setGapCvCell(value.Unit, cell + 1, value.AryCvCell[cell]);  // +1 : Cell番号指定は1ベース
                }

                lstModifiedUnits.Add(value.Unit);
            }

            // Cellの書き込み
            // modified by Hotta 2022/03/01
            //writeGapCellCorrectionValue();
            writeGapCellCorrectionValueWithReconfig();
        }


        private void restoreBulkGapRegAsync(string path)
        {
            GapCamCorrectionValue.LoadFromXmlFile(path, out List<GapCamCorrectionValue> lstGapCv);

            // modified by Hotta 2022/11/25
            /*
            int step = countUnits() * 13; // 1:Unit + 12:Cell
            */
            int step = 0;
            if (allocInfo.LEDModel == ZRD_C12A || allocInfo.LEDModel == ZRD_B12A || allocInfo.LEDModel == ZRD_C15A || allocInfo.LEDModel == ZRD_B15A)
                step = countUnits() * 8;
            else
                step = countUnits() * 12;
            //

            winProgress.SetWholeSteps(step);

            lstModifiedUnits = new List<UnitInfo>();

            foreach (GapCamCorrectionValue value in lstGapCv)
            {
                // added by Hotta 2022/11/10 for Cabinet補正値なし
#if No_CabinetCorrectionValue

#else
                // Unit
                winProgress.ShowMessage("Set Cabinet Correction Value.\r\n(Cabinet : " + value.Unit.ControllerID + "-" + value.Unit.PortNo + "-" + value.Unit.UnitNo + ")");
                setGapCvUnit(value.Unit, value.CvUnit);
#endif
                // Cell
                for (int cell = 0; cell < moduleCount; cell++)
                {
                    winProgress.ShowMessage("Set Module Correction Value.\r\n(Cabinet : " + value.Unit.ControllerID + "-" + value.Unit.PortNo + "-" + value.Unit.UnitNo + ", Module-" + (cell + 1) + ")");
                    winProgress.PutForward1Step();
                    setGapCvCellBulk(value.Unit, cell + 1, value.AryCvCell[cell]);  // +1 : Cell番号指定は1ベース
                }

                lstModifiedUnits.Add(value.Unit);
            }

            // Cellの書き込み
            // modified by Hotta 2022/03/01
            //writeGapCellCorrectionValue();
            writeGapCellCorrectionValueWithReconfig();
        }

        private void setGapCvUnit(UnitInfo unit, GapCellCorrectValue cv)
        {
            if (unit == null)
            { return; }

            Byte[] cmd = new byte[SDCPClass.CmdGapCorrectValueSet.Length];
            Array.Copy(SDCPClass.CmdGapCorrectValueSet, cmd, SDCPClass.CmdGapCorrectValueSet.Length);

            // Target Unit
            cmd[9] += (byte)(unit.PortNo << 4);
            cmd[9] += (byte)unit.UnitNo;

            // TopLeft
            cmd[8] = 0;
            cmd[20] = (byte)cv.TopLeft;
            sendSdcpCommand(cmd, 100, dicController[unit.ControllerID].IPAddress);

            // TopRight
            cmd[8] = 1;
            cmd[20] = (byte)cv.TopRight;
            sendSdcpCommand(cmd, 100, dicController[unit.ControllerID].IPAddress);

            // BottomLeft
            cmd[8] = 2;
            cmd[20] = (byte)cv.BottomLeft;
            sendSdcpCommand(cmd, 100, dicController[unit.ControllerID].IPAddress);

            // BottomRight
            cmd[8] = 3;
            cmd[20] = (byte)cv.BottomRight;
            sendSdcpCommand(cmd, 100, dicController[unit.ControllerID].IPAddress);

            // LeftTop
            cmd[8] = 4;
            cmd[20] = (byte)cv.LeftTop;
            sendSdcpCommand(cmd, 100, dicController[unit.ControllerID].IPAddress);

            // LeftBottom
            cmd[8] = 5;
            cmd[20] = (byte)cv.LeftBottom;
            sendSdcpCommand(cmd, 100, dicController[unit.ControllerID].IPAddress);

            // RightTop
            cmd[8] = 6;
            cmd[20] = (byte)cv.RightTop;
            sendSdcpCommand(cmd, 100, dicController[unit.ControllerID].IPAddress);

            // RightBottom
            cmd[8] = 7;
            cmd[20] = (byte)cv.RightBottom;
            sendSdcpCommand(cmd, 100, dicController[unit.ControllerID].IPAddress);

            //// 全周同時書き込み
            //cmd[8] = 0xFF;

            //cmd[20] = (byte)cv.TopLeft;
            //cmd[21] = (byte)cv.TopRight;
            //cmd[22] = (byte)cv.BottomLeft;
            //cmd[23] = (byte)cv.BottomRight;
            //cmd[24] = (byte)cv.LeftTop;
            //cmd[25] = (byte)cv.LeftBottom;
            //cmd[26] = (byte)cv.RightTop;
            //cmd[27] = (byte)cv.RightBottom;

            //sendSdcpCommand(cmd, 100, dicController[unit.ControllerID].IPAddress);
        }

        private void setGapCvCell(UnitInfo unit, int cell, GapCellCorrectValue cv)
        {
            setGapCvCellEdge(unit, cell, EdgePosition.Edge_1, cv.TopLeft);
            setGapCvCellEdge(unit, cell, EdgePosition.Edge_2, cv.TopRight);
            setGapCvCellEdge(unit, cell, EdgePosition.Edge_3, cv.LeftTop);
            setGapCvCellEdge(unit, cell, EdgePosition.Edge_4, cv.RightTop);
            setGapCvCellEdge(unit, cell, EdgePosition.Edge_5, cv.LeftBottom);
            setGapCvCellEdge(unit, cell, EdgePosition.Edge_6, cv.RightBottom);
            setGapCvCellEdge(unit, cell, EdgePosition.Edge_7, cv.BottomLeft);
            setGapCvCellEdge(unit, cell, EdgePosition.Edge_8, cv.BottomRight);
        }

        private void setGapCvCellEdge(UnitInfo unit, int cell, EdgePosition targetEdge, int value)
        {
            if (unit == null)
            { return; }

            Byte[] cmd = new byte[SDCPClass.CmdGapCellCorrectValueSet.Length];
            Array.Copy(SDCPClass.CmdGapCellCorrectValueSet, cmd, SDCPClass.CmdGapCellCorrectValueSet.Length);

            cmd[9] += (byte)(unit.PortNo << 4);
            cmd[9] += (byte)unit.UnitNo;

            cmd[20] = (byte)cell;

            cmd[21] = (byte)targetEdge;

            cmd[22] = (byte)value;

            sendSdcpCommand(cmd, 100, dicController[unit.ControllerID].IPAddress);
        }


        private void setGapCvCellBulk(UnitInfo unit, int cell, GapCellCorrectValue cv)
        {
            if (unit == null)
            { return; }

            Byte[] cmd = new byte[SDCPClass.CmdGapCellCorrectValueSetBulk.Length];
            Array.Copy(SDCPClass.CmdGapCellCorrectValueSetBulk, cmd, SDCPClass.CmdGapCellCorrectValueSetBulk.Length);

            cmd[8] = (byte)cell;

            cmd[9] += (byte)(unit.PortNo << 4);
            cmd[9] += (byte)unit.UnitNo;

            cmd[20] = (byte)cv.TopLeft;
            cmd[21] = (byte)cv.TopRight;
            cmd[22] = (byte)cv.LeftTop;
            cmd[23] = (byte)cv.RightTop;
            cmd[24] = (byte)cv.LeftBottom;
            cmd[25] = (byte)cv.RightBottom;
            cmd[26] = (byte)cv.BottomLeft;
            cmd[27] = (byte)cv.BottomRight;

            sendSdcpCommand(cmd, 100, dicController[unit.ControllerID].IPAddress);
        }


#endregion Backup(Gap)

#region Set Pos

        private void outputGapCamTargetArea(List<UnitInfo> lstTgtUnit, bool white = false)
        {
            // modified by Hotta 2022/11/10 for 複数コントローラ
#if MultiController
            OutputTargetArea(lstTgtUnit, !white);
#else

            int startX = int.MaxValue, startY = int.MaxValue, maxX = 0, maxY = 0;
            int height = 0, width = 0;

            foreach (UnitInfo unit in lstTgtUnit)
            {
                if (unit.PixelX < startX)
                { startX = unit.PixelX; }

                if (unit.PixelY < startY)
                { startY = unit.PixelY; }

                if (unit.PixelX > maxX)
                { maxX = unit.PixelX; }

                if (unit.PixelY > maxY)
                { maxY = unit.PixelY; }
            }

            height = maxY - startY + cabiDy;
            width = maxX - startX + cabiDx;

#if NO_CONTROLLER
            return;
#endif

#if OutputOnlyGreen
            if(white != true)
                outputIntSigWindow(startX, startY, height, width, 0, m_MeasureLevel, 0);
            else
                outputIntSigWindow(startX, startY, height, width, m_MeasureLevel, m_MeasureLevel, m_MeasureLevel);
#else
            outputIntSigWindow(startX, startY, height, width, Settings.Ins.GapCam.MeasLevel, Settings.Ins.GapCam.MeasLevel, Settings.Ins.GapCam.MeasLevel);
#endif

#endif
        }

        // added by Hotta 2022/06/30
#if CorrectTargetEdge

        // modified by Hotta 2022/10/26 for 複数コントローラ
#if MultiController

        enum ExpandType
        {
            Both = 0, Top = 1, Right = 2
        }

        class WindowSigInfo
        {
            public int controllerID;
            public int start_x;
            public int start_y;
            public int end_x;
            public int end_y;
            public WindowSigInfo(int contID, int st_x, int st_y, int ed_x, int ed_y)
            {
                controllerID = contID;
                start_x = st_x;
                start_y = st_y;
                end_x = ed_x;
                end_y = ed_y;
            }
        }

        class HalfTileInfo
        {
            public int controllerID;
            public int X;
            public int Y;
            public HalfTileInfo(int contID, int x, int y)
            {
                controllerID = contID;
                X = x;
                Y = y;
            }
        }


        bool m_bottomHalfTile = false;
        bool m_rightHalfTile = false;

        int m_topTileNumX, m_topTileNumY;
        int m_rightTileNumX, m_rightTileNumY;

        bool m_ExpandTop, m_ExpandBottom;
        bool m_ExpandLeft, m_ExpandRight;

        //
        // lstTgtUnitのインデックス
        // w:5 × h:3 を選択した場合
        //  0  1  2  3  4
        //  5  6  7  8  9
        // 10 11 12 13 14 
        //
        //
        private void outputGapCamTargetArea_EdgeExpand(List<UnitInfo> lstTgtUnit, ExpandType type, bool white = false)
        {
            // 対象Cabinetで、コントローラごとのウインドウ情報を作る
            // 対象Cabinet自身のエリアを塗りつぶす
            List<WindowSigInfo> windowSigInfo = new List<WindowSigInfo>();
            foreach (UnitInfo unit in lstTgtUnit)
            {
                if (windowSigInfo.Count == 0)
                {
                    if (type == ExpandType.Top)
                        windowSigInfo.Add(new WindowSigInfo(unit.ControllerID, unit.PixelX, unit.PixelY + modDy / 2 - 1, unit.PixelX + cabiDx - 1, unit.PixelY + cabiDy - 1 - modDy / 2));
                    else if (type == ExpandType.Right)
                        windowSigInfo.Add(new WindowSigInfo(unit.ControllerID, unit.PixelX + modDx / 2 - 1, unit.PixelY, unit.PixelX + cabiDx - 1 - modDx / 2, unit.PixelY + cabiDy - 1));
                    else
                        windowSigInfo.Add(new WindowSigInfo(unit.ControllerID, unit.PixelX, unit.PixelY, unit.PixelX + cabiDx - 1, unit.PixelY + cabiDy - 1));
                }
                else
                {
                    bool existsFlag = false;
                    foreach (WindowSigInfo wsi in windowSigInfo)
                    {
                        if (wsi.controllerID == unit.ControllerID)
                        {
                            if (type == ExpandType.Top)
                            {
                                if (wsi.start_x > unit.PixelX)
                                    wsi.start_x = unit.PixelX;
                                if (wsi.start_y > unit.PixelY + modDy / 2 - 1)
                                    wsi.start_y = unit.PixelY + modDy / 2 - 1;
                                if (wsi.end_x < unit.PixelX + cabiDx - 1)
                                    wsi.end_x = unit.PixelX + cabiDx - 1;
                                if (wsi.end_y < unit.PixelY + cabiDy - 1 - modDy / 2)
                                    wsi.end_y = unit.PixelY + cabiDy - 1 - modDy / 2;
                                existsFlag = true;
                            }
                            else if (type == ExpandType.Right)
                            {
                                if (wsi.start_x > unit.PixelX + modDx / 2 - 1)
                                    wsi.start_x = unit.PixelX + modDx / 2 - 1;
                                if (wsi.start_y > unit.PixelY)
                                    wsi.start_y = unit.PixelY;
                                if (wsi.end_x < unit.PixelX + cabiDx - 1 - modDx / 2)
                                    wsi.end_x = unit.PixelX + cabiDx - 1 - modDx / 2;
                                if (wsi.end_y < unit.PixelY + cabiDy - 1)
                                    wsi.end_y = unit.PixelY + cabiDy - 1;
                                existsFlag = true;
                            }
                            else
                            {
                                if (wsi.start_x > unit.PixelX)
                                    wsi.start_x = unit.PixelX;
                                if (wsi.start_y > unit.PixelY)
                                    wsi.start_y = unit.PixelY;
                                if (wsi.end_x < unit.PixelX + cabiDx - 1)
                                    wsi.end_x = unit.PixelX + cabiDx - 1;
                                if (wsi.end_y < unit.PixelY + cabiDy - 1)
                                    wsi.end_y = unit.PixelY + cabiDy - 1;
                                existsFlag = true;
                            }
                        }
                    }
                    if (existsFlag != true)
                    {
                        if (type == ExpandType.Top)
                            windowSigInfo.Add(new WindowSigInfo(unit.ControllerID, unit.PixelX, unit.PixelY + modDy / 2 - 1, unit.PixelX + cabiDx - 1, unit.PixelY + cabiDy - 1 - modDy / 2));
                        else if (type == ExpandType.Right)
                            windowSigInfo.Add(new WindowSigInfo(unit.ControllerID, unit.PixelX + modDx / 2 - 1, unit.PixelY, unit.PixelX + cabiDx - 1 - modDx / 2, unit.PixelY + cabiDy - 1));
                        else
                            windowSigInfo.Add(new WindowSigInfo(unit.ControllerID, unit.PixelX, unit.PixelY, unit.PixelX + cabiDx - 1, unit.PixelY + cabiDy - 1));
                    }
                }
            }

            //// CabinetAllocationで、隣接Cabinetの存在を調べる
            //bool topEdge, bottomEdge, leftEdge, rightEdge;
            //checkWallEdge(lstTgtUnit, out topEdge, out bottomEdge, out leftEdge, out rightEdge);

            // CabinetAllocationで、対象Cabinetの端のインデックスを調べる
            int startX = int.MaxValue, startY = int.MaxValue, endX = int.MinValue, endY = int.MinValue;
            foreach (UnitInfo unit in lstTgtUnit)
            {
                if (unit.X < startX)
                { startX = unit.X; }

                if (unit.Y < startY)
                { startY = unit.Y; }

                if (unit.X > endX)
                { endX = unit.X; }

                if (unit.Y > endY)
                { endY = unit.Y; }
            }
            int tgtNumX = (endX - startX) + 1;
            int tgtNumY = (endY - startY) + 1;


            int modNumX, modNumY;
            if (allocInfo.LEDModel == ZRD_C12A || allocInfo.LEDModel == ZRD_C15A || allocInfo.LEDModel == ZRD_B12A || allocInfo.LEDModel == ZRD_B15A)
            {
                modNumX = 4;
                modNumY = 2;
            }
            else
            {
                modNumX = 4;
                modNumY = 3;
            }


            m_ExpandTop = m_ExpandBottom = m_ExpandLeft = m_ExpandRight = false;

            if (type == ExpandType.Both || type == ExpandType.Top)
            {
                // 端でないCabinetは、自身を塗りつぶす
                for (int i = 0; i < tgtNumX * tgtNumY; i++)
                {
                    // 上端でない
                    if (i >= tgtNumX)
                    {
                        // 自身のCabinet上端まで塗りつぶす
                        foreach (WindowSigInfo wsi in windowSigInfo)
                        {
                            if (wsi.controllerID == lstTgtUnit[i].ControllerID)
                            {
                                if (wsi.start_y > lstTgtUnit[i].PixelY)
                                    wsi.start_y = lstTgtUnit[i].PixelY;
                            }
                        }
                    }
                    // 下端でない
                    if (i < tgtNumX * (tgtNumY - 1))
                    {
                        // 自身のCabinet下端まで塗りつぶす
                        foreach (WindowSigInfo wsi in windowSigInfo)
                        {
                            if (wsi.controllerID == lstTgtUnit[i].ControllerID)
                            {
                                if (wsi.end_y < lstTgtUnit[i].PixelY + cabiDy - 1)
                                    wsi.end_y = lstTgtUnit[i].PixelY + cabiDy - 1;
                            }
                        }
                    }
                }


                ///////////////////
                // 選択されたCabinetの上端全てに隣接するCabinetがあるかどうか調べる
                int y0 = lstTgtUnit[0].Y;
                bool nextCabinet = true;
                if (y0 <= 1)
                {
                    nextCabinet = false;
                }
                else
                {
                    for (int x = 0; x < tgtNumX; x++)
                    {
                        if (lstTgtUnit[x].Y != y0)
                            break;

                        UnitInfo nextUnit = allocInfo.lstUnits[lstTgtUnit[x].X - 1][(lstTgtUnit[x].Y - 1) - 1]; // -1 してインデックスの基数を　1　→　0　に合わせる。2ケ目の -1 は、1ケ上を意味する。
                        if (nextUnit == null)
                        {
                            nextCabinet = false;
                            break;
                        }
                    }
                }

                // 選択されたCabinetの上端全てに、隣接するCabinetがあるなら拡張する
                if (nextCabinet == true)
                {
                    m_ExpandTop = true;

                    for (int x = 0; x < tgtNumX; x++)
                    {
                        // 自身のCabinet上端まで塗りつぶす
                        foreach (WindowSigInfo wsi in windowSigInfo)
                        {
                            if (wsi.controllerID == lstTgtUnit[x].ControllerID)
                            {
                                if (wsi.start_y > lstTgtUnit[x].PixelY)
                                    wsi.start_y = lstTgtUnit[x].PixelY;
                            }
                        }

                        UnitInfo nextUnit = allocInfo.lstUnits[lstTgtUnit[x].X - 1][(lstTgtUnit[x].Y - 1) - 1]; // -1 してインデックスの基数を　1　→　0　に合わせる。2ケ目の -1 は、1ケ上を意味する。
                        bool existsFlag = false;
                        foreach (WindowSigInfo wsi in windowSigInfo)
                        {
                            // 隣接するCabinetのウインドウ情報があるなら
                            if (wsi.controllerID == nextUnit.ControllerID)
                            {
                                if (wsi.start_x > nextUnit.PixelX)
                                    wsi.start_x = nextUnit.PixelX;
                                if (wsi.start_y > nextUnit.PixelY + cabiDy - 1 - modDy / 2)
                                    wsi.start_y = nextUnit.PixelY + cabiDy - 1 - modDy / 2;
                                if (wsi.end_x < nextUnit.PixelX + cabiDx - 1)
                                    wsi.end_x = nextUnit.PixelX + cabiDx - 1;
                                if (wsi.end_y < nextUnit.PixelY + cabiDy - 1)
                                    wsi.end_y = nextUnit.PixelY + cabiDy - 1;
                                existsFlag = true;
                            }
                        }
                        // 隣接するCabinetのウインドウ情報がないなら
                        if (existsFlag != true)
                            windowSigInfo.Add(new WindowSigInfo(nextUnit.ControllerID, nextUnit.PixelX, nextUnit.PixelY + cabiDy - 1 - modDy / 2, nextUnit.PixelX + cabiDx - 1, nextUnit.PixelY + cabiDy - 1));

                        /*
                        // 拡張したCabinetが別のControllerかつPixelY==0なら、半分タイルが必要
                        if (lstTgtUnit[x].ControllerID != nextUnit.ControllerID && (lstTgtUnit[x].PixelY == 0 || nextUnit.PixelY == 0))
                            m_bottomHalfTile = true;
                        */
                        // （拡張して）自身PixelY==0なら、半分タイルが必要
                        if (lstTgtUnit[x].PixelY == 0)
                            m_bottomHalfTile = true;
                    }
                }

                ///////////////////
                // 選択されたCabinetの下端全てに隣接するCabinetがあるかどうか調べる
                y0 = lstTgtUnit[tgtNumX * tgtNumY - 1].Y;
                nextCabinet = true;
                if (y0 >= allocInfo.MaxY)
                {
                    nextCabinet = false;
                }
                else
                {
                    for (int x = 0; x < tgtNumX; x++)
                    {
                        if (lstTgtUnit[(tgtNumX * tgtNumY - 1) - x].Y != y0)
                            break;

                        UnitInfo nextUnit = allocInfo.lstUnits[lstTgtUnit[(tgtNumX * tgtNumY - 1) - x].X - 1][(lstTgtUnit[(tgtNumX * tgtNumY - 1) - x].Y - 1) + 1]; // -1 してインデックスの基数を　1　→　0　に合わせる。2ケ目の +1 は、1ケ下を意味する。
                        if (nextUnit == null)
                        {
                            nextCabinet = false;
                            break;
                        }
                    }
                }

                // 選択されたCabinetの下端全てに、隣接するCabinetがあるなら拡張する
                if (nextCabinet == true)
                {
                    m_ExpandBottom = true;

                    for (int x = 0; x < tgtNumX; x++)
                    {
                        // 自身のCabinet下端まで塗りつぶす
                        foreach (WindowSigInfo wsi in windowSigInfo)
                        {
                            if (wsi.controllerID == lstTgtUnit[(tgtNumX * tgtNumY - 1) - x].ControllerID)
                            {
                                if (wsi.end_y < lstTgtUnit[(tgtNumX * tgtNumY - 1) - x].PixelY + cabiDy - 1)
                                    wsi.end_y = lstTgtUnit[(tgtNumX * tgtNumY - 1) - x].PixelY + cabiDy - 1;
                            }
                        }

                        UnitInfo nextUnit = allocInfo.lstUnits[lstTgtUnit[(tgtNumX * tgtNumY - 1) - x].X - 1][(lstTgtUnit[(tgtNumX * tgtNumY - 1) - x].Y - 1) + 1]; // -1 してインデックスの基数を　1　→　0　に合わせる。2ケ目の +1 は、1ケ下を意味する。
                        bool existsFlag = false;
                        foreach (WindowSigInfo wsi in windowSigInfo)
                        {
                            // 隣接するCabinetのウインドウ情報があるなら
                            if (wsi.controllerID == nextUnit.ControllerID)
                            {
                                if (wsi.start_x > nextUnit.PixelX)
                                    wsi.start_x = nextUnit.PixelX;
                                if (wsi.start_y > nextUnit.PixelY)
                                    wsi.start_y = nextUnit.PixelY;
                                if (wsi.end_x < nextUnit.PixelX + cabiDx - 1)
                                    wsi.end_x = nextUnit.PixelX + cabiDx - 1;
                                if (wsi.end_y < nextUnit.PixelY + modDy / 2 + 1)
                                    wsi.end_y = nextUnit.PixelY + modDy / 2 + 1;
                                existsFlag = true;
                            }
                        }
                        // 隣接するCabinetのウインドウ情報がないなら
                        if (existsFlag != true)
                            windowSigInfo.Add(new WindowSigInfo(nextUnit.ControllerID, nextUnit.PixelX, nextUnit.PixelY, nextUnit.PixelX + cabiDx - 1, nextUnit.PixelY + modDy / 2 - 1));

                        /*
                        // 拡張したCabinetが別のControllerかつPixelY==0なら、半分タイルが必要
                        if (lstTgtUnit[(tgtNumX * tgtNumY - 1) - x].ControllerID != nextUnit.ControllerID && (lstTgtUnit[(tgtNumX * tgtNumY - 1) - x].PixelY == 0 || nextUnit.PixelY == 0))
                            m_bottomHalfTile = true;
                        */
                        // （拡張して）拡張したCabinetのPixelY==0なら、半分タイルが必要
                        if (nextUnit.PixelY == 0)
                            m_bottomHalfTile = true;

                    }
                }
            }


            if (type == ExpandType.Both || type == ExpandType.Right)
            {
                // 端でないCabinetは、自身を塗りつぶす
                for (int i = 0; i < tgtNumX * tgtNumY; i++)
                {
                    // 左端でない
                    if (i % tgtNumX != 0)
                    {
                        // 自身のCabinet左端まで塗りつぶす
                        foreach (WindowSigInfo wsi in windowSigInfo)
                        {
                            if (wsi.controllerID == lstTgtUnit[i].ControllerID)
                            {
                                if (wsi.start_x > lstTgtUnit[i].PixelX)
                                    wsi.start_x = lstTgtUnit[i].PixelX;
                            }
                        }
                    }
                    // 右端でない
                    if ((i + 1) % tgtNumX != 0)
                    {
                        // 自身のCabinet右端まで塗りつぶす
                        foreach (WindowSigInfo wsi in windowSigInfo)
                        {
                            if (wsi.controllerID == lstTgtUnit[i].ControllerID)
                            {
                                if (wsi.end_x < lstTgtUnit[i].PixelX + cabiDx - 1)
                                    wsi.end_x = lstTgtUnit[i].PixelX + cabiDx - 1;
                            }
                        }
                    }
                }


                ///////////////////
                // 選択されたCabinetの左端全てに隣接するCabinetがあるかどうか調べる
                int x0 = lstTgtUnit[0].X;
                bool nextCabinet = true;
                if (x0 <= 1)
                {
                    nextCabinet = false;
                }
                else
                {
                    for (int y = 0; y < tgtNumY; y++)
                    {
                        if (lstTgtUnit[tgtNumX * y].X != x0)
                            break;

                        UnitInfo nextUnit = allocInfo.lstUnits[(lstTgtUnit[tgtNumX * y].X - 1) - 1][lstTgtUnit[tgtNumX * y].Y - 1]; // -1 してインデックスの基数を　1　→　0　に合わせる。2ケ目の -1 は、1ケ左を意味する。
                        if (nextUnit == null)
                        {
                            nextCabinet = false;
                            break;
                        }
                    }
                }

                // 選択されたCabinetの左端全てに、隣接するCabinetがあるなら拡張する
                if (nextCabinet == true)
                {
                    m_ExpandLeft = true;

                    for (int y = 0; y < tgtNumY; y++)
                    {
                        // 自身のCabinet左端まで塗りつぶす
                        foreach (WindowSigInfo wsi in windowSigInfo)
                        {
                            if (wsi.controllerID == lstTgtUnit[tgtNumX * y].ControllerID)
                            {
                                if (wsi.start_x > lstTgtUnit[tgtNumX * y].PixelX)
                                    wsi.start_x = lstTgtUnit[tgtNumX * y].PixelX;
                            }
                        }

                        UnitInfo nextUnit = allocInfo.lstUnits[(lstTgtUnit[tgtNumX * y].X - 1) - 1][lstTgtUnit[tgtNumX * y].Y - 1]; // -1 してインデックスの基数を　1　→　0　に合わせる。2ケ目の -1 は、1ケ左を意味する。
                        bool existsFlag = false;
                        foreach (WindowSigInfo wsi in windowSigInfo)
                        {
                            // 隣接するCabinetのウインドウ情報があるなら
                            if (wsi.controllerID == nextUnit.ControllerID)
                            {
                                if (wsi.start_x > nextUnit.PixelX + cabiDx - 1 - modDx / 2)
                                    wsi.start_x = nextUnit.PixelX + cabiDx - 1 - modDx / 2;
                                if (wsi.start_y > nextUnit.PixelY)
                                    wsi.start_y = nextUnit.PixelY;
                                if (wsi.end_x < nextUnit.PixelX + cabiDx - 1)
                                    wsi.end_x = nextUnit.PixelX + cabiDx - 1;
                                if (wsi.end_y < nextUnit.PixelY + cabiDy - 1)
                                    wsi.end_y = nextUnit.PixelY + cabiDy - 1;
                                existsFlag = true;
                            }
                        }
                        // 隣接するCabinetのウインドウ情報がないなら
                        if (existsFlag != true)
                            windowSigInfo.Add(new WindowSigInfo(nextUnit.ControllerID, nextUnit.PixelX + cabiDx - 1 - modDx / 2, nextUnit.PixelY, nextUnit.PixelX + cabiDx - 1, nextUnit.PixelY + cabiDy - 1));

                        /*
                        // 拡張したCabinetが別のControllerかつPixelY==0なら、半分タイルが必要
                        if (lstTgtUnit[y].ControllerID != nextUnit.ControllerID && (lstTgtUnit[tgtNumX * y].PixelX == 0 || nextUnit.PixelX == 0))
                            m_rightHalfTile = true;
                        */
                        // （拡張して）自身のPixelX==0なら、半分タイルが必要
                        if (lstTgtUnit[tgtNumX * y].PixelX == 0)
                            m_rightHalfTile = true;

                    }
                }


                ///////////////////
                // 選択されたCabinetの右端全てに隣接するCabinetがあるかどうか調べる
                x0 = lstTgtUnit[tgtNumX - 1].X;
                nextCabinet = true;
                if (x0 >= allocInfo.MaxX)
                {
                    nextCabinet = false;
                }
                else
                {
                    for (int y = 0; y < tgtNumY; y++)
                    {
                        if (lstTgtUnit[tgtNumX * (y + 1) - 1].X != x0)
                            break;

                        UnitInfo nextUnit = allocInfo.lstUnits[(lstTgtUnit[tgtNumX * (y + 1) - 1].X - 1) + 1][lstTgtUnit[tgtNumX * (y + 1) - 1].Y - 1]; // -1 してインデックスの基数を　1　→　0　に合わせる。2ケ目の +1 は、1ケ右を意味する。
                        if (nextUnit == null)
                        {
                            nextCabinet = false;
                            break;
                        }
                    }
                }

                // 選択されたCabinetの右端全てに、隣接するCabinetがあるなら拡張する
                if (nextCabinet == true)
                {
                    m_ExpandRight = true;

                    for (int y = 0; y < tgtNumY; y++)
                    {
                        // 自身のCabinet右端まで塗りつぶす
                        foreach (WindowSigInfo wsi in windowSigInfo)
                        {
                            if (wsi.controllerID == lstTgtUnit[tgtNumX * (y + 1) - 1].ControllerID)
                            {
                                if (wsi.end_x < lstTgtUnit[tgtNumX * (y + 1) - 1].PixelX + cabiDx - 1)
                                    wsi.end_x = lstTgtUnit[tgtNumX * (y + 1) - 1].PixelX + cabiDx - 1;
                            }
                        }

                        UnitInfo nextUnit = allocInfo.lstUnits[(lstTgtUnit[tgtNumX * (y + 1) - 1].X - 1) + 1][lstTgtUnit[tgtNumX * (y + 1) - 1].Y - 1]; // -1 してインデックスの基数を　1　→　0　に合わせる。2ケ目の +1 は、1ケ右を意味する。
                        bool existsFlag = false;
                        foreach (WindowSigInfo wsi in windowSigInfo)
                        {
                            // 隣接するCabinetのウインドウ情報があるなら
                            if (wsi.controllerID == nextUnit.ControllerID)
                            {
                                if (wsi.start_x > nextUnit.PixelX)
                                    wsi.start_x = nextUnit.PixelX;
                                if (wsi.start_y > nextUnit.PixelY)
                                    wsi.start_y = nextUnit.PixelY;
                                if (wsi.end_x < nextUnit.PixelX + modDx / 2 - 1)
                                    wsi.end_x = nextUnit.PixelX + modDx / 2 - 1;
                                if (wsi.end_y < nextUnit.PixelY + cabiDy - 1)
                                    wsi.end_y = nextUnit.PixelY + cabiDy - 1;
                                existsFlag = true;
                            }
                        }
                        // 隣接するCabinetのウインドウ情報がないなら
                        if (existsFlag != true)
                            windowSigInfo.Add(new WindowSigInfo(nextUnit.ControllerID, nextUnit.PixelX, nextUnit.PixelY, nextUnit.PixelX + modDx / 2 - 1, nextUnit.PixelY + cabiDy - 1));

                        /*
                        // 拡張したCabinetが別のControllerかつPixelY==0なら、半分タイルが必要
                        if (lstTgtUnit[tgtNumX * (y + 1) - 1].ControllerID != nextUnit.ControllerID && (lstTgtUnit[tgtNumX * (y + 1) - 1].PixelX == 0 || nextUnit.PixelX == 0))
                            m_rightHalfTile = true;
                        */
                        // （拡張して）拡張したCabinetのPixelX==0なら、半分タイルが必要
                        if (nextUnit.PixelX == 0)
                            m_rightHalfTile = true;


                    }
                }
            }


            // 半分タイルを表示する必要のあるCabinet
            // 対象Cabinetのうち、Pixel=0で、かつ、物理的に端でないもの
            // 1ケでも該当するならば、全Cabinetを撮影することになるので、Controller番号は関係なく、該当するしないだけを取得する
            foreach (UnitInfo unit in lstTgtUnit)
            {
                UnitInfo upUnit;
                UnitInfo leftUnit;

                if (type == ExpandType.Both || type == ExpandType.Top)
                {
                    // 1ケ上
                    if (unit.Y > 1)
                    {
                        upUnit = allocInfo.lstUnits[unit.X - 1][(unit.Y - 1) - 1]; // -1 してインデックスの基数を　1　→　0　に合わせる。2ケ目の -1 は、1ケ上を意味する。
                        if (upUnit != null && unit.PixelY == 0)
                            m_bottomHalfTile = true;
                    }
                }

                if (type == ExpandType.Both || type == ExpandType.Right)
                {
                    // 1ケ左
                    if (unit.X > 1)
                    {
                        leftUnit = allocInfo.lstUnits[(unit.X - 1) - 1][unit.Y - 1]; // -1 してインデックスの基数を　1　→　0　に合わせる。2ケ目の -1 は、1ケ左を意味する。
                        if (leftUnit != null && unit.PixelX == 0)
                            m_rightHalfTile = true;
                    }
                }
            }

            // タイルの数
            if (type == ExpandType.Top)
            {
                m_topTileNumX = tgtNumX * modNumX;
                m_topTileNumY = tgtNumY * modNumY - 1;
                if (m_ExpandTop == true)
                    m_topTileNumY++;
                if (m_ExpandBottom == true)
                    m_topTileNumY++;
#if TempFile
                using (StreamWriter sw = new StreamWriter(applicationPath + "\\Temp\\topTileNum.txt"))
                {
                    sw.WriteLine(m_topTileNumX.ToString() + "," + m_topTileNumY.ToString());
                }
#endif
            }

            if (type == ExpandType.Right)
            {
                m_rightTileNumX = tgtNumX * modNumX - 1;
                if (m_ExpandLeft == true)
                    m_rightTileNumX++;
                if (m_ExpandRight == true)
                    m_rightTileNumX++;
                m_rightTileNumY = tgtNumY * modNumY;
#if TempFile
                using (StreamWriter sw = new StreamWriter(applicationPath + "\\Temp\\rightTileNum.txt"))
                {
                    sw.WriteLine(m_rightTileNumX.ToString() + "," + m_rightTileNumY.ToString());
                }
#endif
            }



#if NO_CONTROLLER
#if TempFile
            int _k = 2;
            int _cabiDx = cabiDx / _k;
            int _cabiDy = cabiDy / _k;

            Mat[] sourceMat = new Mat[windowSigInfo.Count];

            Mat[] sourceWindowMat = new Mat[windowSigInfo.Count];
            Mat[] sourceTileMat = new Mat[windowSigInfo.Count];

            // ウィンドウ
            for (int i = 0; i < sourceMat.Length; i++)
            {
                sourceMat[i] = new Mat(new Size(1920, 1080), MatType.CV_8UC3);
                sourceMat[i].SetTo(0);
                sourceMat[i].Rectangle(new OpenCvSharp.Point(windowSigInfo[i].start_x / _k, windowSigInfo[i].start_y / _k), new OpenCvSharp.Point(windowSigInfo[i].end_x / _k, windowSigInfo[i].end_y / _k), new Scalar(127, 127, 127), -1);

                sourceWindowMat[i] = new Mat(new Size(1920, 1080), MatType.CV_8UC1);
                sourceWindowMat[i].SetTo(0);
                sourceWindowMat[i].Rectangle(new OpenCvSharp.Point(windowSigInfo[i].start_x / _k, windowSigInfo[i].start_y / _k), new OpenCvSharp.Point(windowSigInfo[i].end_x / _k, windowSigInfo[i].end_y / _k), new Scalar(255), -1);
            }

            // タイル
            for (int i = 0; i < sourceMat.Length; i++)
            {
                sourceTileMat[i] = new Mat(new Size(1920, 1080), MatType.CV_8UC1);
                sourceTileMat[i].SetTo(0);

                for (int y = 0; y < sourceMat[i].Height / _cabiDy; y++)
                {
                    for (int x = 0; x < sourceMat[i].Width / _cabiDx; x++)
                    {
                        for (int _y = 0; _y < modNumY; _y++)
                        {
                            for (int _x = 0; _x < modNumX; _x++)
                            {
                                if (type == ExpandType.Both || type == ExpandType.Top)
                                {
                                    // H目地用
                                    OpenCvSharp.Point pt1 = new OpenCvSharp.Point(_cabiDx * x + _cabiDx / modNumX * _x + _cabiDx / modNumX / 2, _cabiDy * y + _cabiDy / modNumY * (_y + 1));
                                    pt1.X -= (int)((double)Settings.Ins.GapCam.TrimmingSize / _k / 2 + 0.5);
                                    pt1.Y -= (int)((double)Settings.Ins.GapCam.TrimmingSize / _k / 2 + 0.5);
                                    OpenCvSharp.Point pt2 = new OpenCvSharp.Point(pt1.X, pt1.Y);
                                    pt2.X += (int)((double)Settings.Ins.GapCam.TrimmingSize / _k - 1 + 0.5);
                                    pt2.Y += (int)((double)Settings.Ins.GapCam.TrimmingSize / _k - 1 + 0.5);
                                    sourceMat[i].Rectangle(pt1, pt2, new Scalar(0, 255, 255), -1);

                                    sourceTileMat[i].Rectangle(pt1, pt2, new Scalar(255), -1);
                                }
                                if (type == ExpandType.Both || type == ExpandType.Right)
                                {
                                    // V目地用
                                    OpenCvSharp.Point pt1 = new OpenCvSharp.Point(_cabiDx * x + _cabiDx / modNumX * (_x + 1), _cabiDy * y + _cabiDy / modNumY * _y + _cabiDy / modNumY / 2);
                                    pt1.X -= (int)((double)Settings.Ins.GapCam.TrimmingSize / _k / 2 + 0.5);
                                    pt1.Y -= (int)((double)Settings.Ins.GapCam.TrimmingSize / _k / 2 + 0.5);
                                    OpenCvSharp.Point pt2 = new OpenCvSharp.Point(pt1.X, pt1.Y);
                                    pt2.X += (int)((double)Settings.Ins.GapCam.TrimmingSize / _k - 1 + 0.5);
                                    pt2.Y += (int)((double)Settings.Ins.GapCam.TrimmingSize / _k - 1 + 0.5);
                                    sourceMat[i].Rectangle(pt1, pt2, new Scalar(255, 255, 0), -1);

                                    sourceTileMat[i].Rectangle(pt1, pt2, new Scalar(255), -1);
                                }
                            }
                        }
                    }
                }
            }

            // 半分タイル
            for (int i = 0; i < sourceMat.Length; i++)
            {
                for (int y = 0; y < sourceMat[i].Height / _cabiDy; y++)
                {
                    for (int x = 0; x < sourceMat[i].Width / _cabiDx; x++)
                    {
                        for (int _y = 0; _y < modNumY; _y++)
                        {
                            for (int _x = 0; _x < modNumX; _x++)
                            {
                                if (type == ExpandType.Both || type == ExpandType.Top)
                                {
                                    // H目地用
                                    if (m_bottomHalfTile == true)
                                    {
                                        OpenCvSharp.Point pt1 = new OpenCvSharp.Point(_cabiDx * x + _cabiDx / modNumX * _x + _cabiDx / modNumX / 2, _cabiDy * y + _cabiDy / modNumY * _y);
                                        pt1.X -= (int)((double)Settings.Ins.GapCam.TrimmingSize / _k / 2 + 0.5);
                                        //pt1.Y -= Settings.Ins.GapCam.TrimmingSize / _k / 2;
                                        OpenCvSharp.Point pt2 = new OpenCvSharp.Point(pt1.X, pt1.Y);
                                        pt2.X += (int)((double)Settings.Ins.GapCam.TrimmingSize / _k - 1 + 0.5);
                                        pt2.Y += (int)((double)Settings.Ins.GapCam.TrimmingSize / _k / 2 - 1 + 0.5);
                                        sourceMat[i].Rectangle(pt1, pt2, new Scalar(0, 127, 255), -1);

                                        sourceTileMat[i].Rectangle(pt1, pt2, new Scalar(255), -1);
                                    }
                                }

                                if (type == ExpandType.Both || type == ExpandType.Right)
                                {
                                    // V目地用
                                    if (m_rightHalfTile == true)
                                    {
                                        OpenCvSharp.Point pt1 = new OpenCvSharp.Point(_cabiDx * x + _cabiDx / modNumX * _x, _cabiDy * y + _cabiDy / modNumY * _y + _cabiDy / modNumY / 2);
                                        //pt1.X -= Settings.Ins.GapCam.TrimmingSize / _k / 2;
                                        pt1.Y -= (int)((double)Settings.Ins.GapCam.TrimmingSize / _k / 2 + 0.5);
                                        OpenCvSharp.Point pt2 = new OpenCvSharp.Point(pt1.X, pt1.Y);
                                        pt2.X += (int)((double)Settings.Ins.GapCam.TrimmingSize / _k / 2 - 1 + 0.5);
                                        pt2.Y += (int)((double)Settings.Ins.GapCam.TrimmingSize / _k - 1 + 0.5);
                                        sourceMat[i].Rectangle(pt1, pt2, new Scalar(255, 0, 0), -1);

                                        sourceTileMat[i].Rectangle(pt1, pt2, new Scalar(255), -1);
                                    }
                                }
                            }
                        }
                    }
                }
            }



            startX = int.MaxValue; startY = int.MaxValue; endX = int.MinValue; endY = int.MinValue;
            foreach (List<UnitInfo> listUnit in allocInfo.lstUnits)
            {
                foreach (UnitInfo unit in listUnit)
                {
                    if (unit != null)
                    {
                        if (unit.X < startX)
                        { startX = unit.X; }

                        if (unit.Y < startY)
                        { startY = unit.Y; }

                        if (unit.X > endX)
                        { endX = unit.X; }

                        if (unit.Y > endY)
                        { endY = unit.Y; }
                    }
                }
            }


            Mat destMat = new Mat(new OpenCvSharp.Size(_cabiDx * endX, _cabiDy * endY), MatType.CV_8UC3);
            destMat.SetTo(0);

            Mat windowMat = new Mat(new OpenCvSharp.Size(_cabiDx * endX, _cabiDy * endY), MatType.CV_8UC1);
            windowMat.SetTo(0);
            Mat tileMat = new Mat(new OpenCvSharp.Size(_cabiDx * endX, _cabiDy * endY), MatType.CV_8UC1);
            tileMat.SetTo(0);

            windowMat.Line(new OpenCvSharp.Point(0, 0), new OpenCvSharp.Point(windowMat.Width - 1, 0), 255);
            for (int y = 0; y < endY; y++)
            {
                windowMat.Line(new OpenCvSharp.Point(0, _cabiDy * y), new OpenCvSharp.Point(windowMat.Width - 1, _cabiDy * y), 255);
            }



            // パターン表示領域
            foreach (List<UnitInfo> listUnit in allocInfo.lstUnits)
            {
                foreach (UnitInfo unit in listUnit)
                {
                    if (unit != null)
                    {
                        for (int i = 0; i < sourceMat.Length; i++)
                        {
                            if (windowSigInfo[i].controllerID == unit.ControllerID)
                            {
                                destMat[new OpenCvSharp.Rect((unit.X - 1) * _cabiDx, (unit.Y - 1) * _cabiDy, _cabiDx, _cabiDy)] =
                                    sourceMat[i][new OpenCvSharp.Rect(unit.PixelX / _k, unit.PixelY / _k, _cabiDx, _cabiDy)];

                                windowMat[new OpenCvSharp.Rect((unit.X - 1) * _cabiDx, (unit.Y - 1) * _cabiDy, _cabiDx, _cabiDy)] =
                                    sourceWindowMat[i][new OpenCvSharp.Rect(unit.PixelX / _k, unit.PixelY / _k, _cabiDx, _cabiDy)];

                                tileMat[new OpenCvSharp.Rect((unit.X - 1) * _cabiDx, (unit.Y - 1) * _cabiDy, _cabiDx, _cabiDy)] =
                                    sourceTileMat[i][new OpenCvSharp.Rect(unit.PixelX / _k, unit.PixelY / _k, _cabiDx, _cabiDy)];
                            }
                        }
                    }
                }
            }

            // 存在するCabinet
            foreach (List<UnitInfo> listUnit in allocInfo.lstUnits)
            {
                foreach (UnitInfo unit in listUnit)
                {
                    if (unit != null)
                    {
                        destMat.Rectangle(new OpenCvSharp.Point((unit.X - 1) * _cabiDx + 5, (unit.Y - 1) * _cabiDy + 5), new OpenCvSharp.Point(unit.X * _cabiDx - 6, unit.Y * _cabiDy - 6), new Scalar(255, 0, 255), 1);
                        destMat.PutText(unit.PixelX.ToString() + "," + unit.PixelY.ToString(), new OpenCvSharp.Point((unit.X - 1) * _cabiDx + 5, (unit.Y - 1) * _cabiDy + 20), HersheyFonts.HersheySimplex, 0.3, new Scalar(255, 0, 255));
                    }
                }
            }

            // Cabinet境界
            for (int y = 0; y < endY; y++)
                destMat.Line(new OpenCvSharp.Point(0, y * _cabiDy), new OpenCvSharp.Point(endX * _cabiDx - 1, y * _cabiDy), new Scalar(0, 255, 0));
            for (int x = 0; x < endX; x++)
                destMat.Line(new OpenCvSharp.Point(x * _cabiDx, 0), new OpenCvSharp.Point(x * _cabiDx, endY * _cabiDy - 1), new Scalar(0, 255, 0));


            // Controller境界
            List<WindowSigInfo> rectController = new List<WindowSigInfo>();
            foreach (List<UnitInfo> listUnit in allocInfo.lstUnits)
            {
                foreach (UnitInfo unit in listUnit)
                {
                    if (unit != null)
                    {
                        if (rectController.Count == 0)
                        {
                            rectController.Add(new WindowSigInfo(unit.ControllerID, unit.X - 1, unit.Y - 1, unit.X, unit.Y));
                        }
                        else
                        {
                            bool existsFlag = false;
                            foreach (WindowSigInfo wsi in rectController)
                            {
                                if (wsi.controllerID == unit.ControllerID)
                                {
                                    if (wsi.start_x > unit.X - 1)
                                        wsi.start_x = unit.X - 1;
                                    if (wsi.start_y > unit.Y - 1)
                                        wsi.start_y = unit.Y - 1;
                                    if (wsi.end_x < unit.X)
                                        wsi.end_x = unit.X;
                                    if (wsi.end_y < unit.Y)
                                        wsi.end_y = unit.Y;
                                    existsFlag = true;
                                }
                            }
                            if (existsFlag != true)
                            {
                                rectController.Add(new WindowSigInfo(unit.ControllerID, unit.X - 1, unit.Y - 1, unit.X, unit.Y));
                            }
                        }
                    }
                }
            }
            foreach (WindowSigInfo wsi in rectController)
            {
                destMat.Rectangle(new OpenCvSharp.Point(wsi.start_x * _cabiDx, wsi.start_y * _cabiDy), new OpenCvSharp.Point(wsi.end_x * _cabiDx - 1, wsi.end_y * _cabiDy - 1), new Scalar(0, 0, 255), 3);
            }

            if (type == ExpandType.Top)
                destMat.SaveImage(applicationPath + "\\Temp\\topPattern.jpg");
            else if (type == ExpandType.Right)
                destMat.SaveImage(applicationPath + "\\Temp\\rightPattern.jpg");
            else
                destMat.SaveImage(applicationPath + "\\Temp\\bothPattern.jpg");


            if (type == ExpandType.Top)
            {
                windowMat.SaveImage(applicationPath + "\\Temp\\topWindow.jpg");
                tileMat.SaveImage(applicationPath + "\\Temp\\topTile.jpg");
            }
            else if (type == ExpandType.Right)
            {
                windowMat.SaveImage(applicationPath + "\\Temp\\rightWindow.jpg");
                tileMat.SaveImage(applicationPath + "\\Temp\\rightTile.jpg");
            }


            destMat.Dispose();
            windowMat.Dispose();
            tileMat.Dispose();

            for (int i = 0; i < sourceMat.Length; i++)
            {
                sourceMat[i].Dispose();

                sourceWindowMat[i].Dispose();
                sourceTileMat[i].Dispose();
            }
#endif

#else
            // 一旦、前回の表示をクリアする
            outputIntSigFlat(0, 0, 0);
            Thread.Sleep(Settings.Ins.Camera.PatternWait);

            foreach (WindowSigInfo wsi in windowSigInfo)
            {
                foreach (ControllerInfo cont in dicController.Values)
                {
                    if(cont.ControllerID == wsi.controllerID)
                    {
                        if (white != true)
                            outputIntSigWindowByController(wsi.start_x, wsi.start_y, wsi.end_y - wsi.start_y + 1, wsi.end_x - wsi.start_x + 1, cont, 0, m_MeasureLevel, 0);
                        else
                            outputIntSigWindowByController(wsi.start_x, wsi.start_y, wsi.end_y - wsi.start_y + 1, wsi.end_x - wsi.start_x + 1, cont, m_MeasureLevel, m_MeasureLevel, m_MeasureLevel);
                    }
                }
            }
#endif

            // added by Hotta 2022/12/19
            foreach (ControllerInfo controller in dicController.Values)
            {
                foreach (WindowSigInfo wsi in windowSigInfo)
                {
                    if (wsi.controllerID == controller.ControllerID)
                    {
                        controller.Target = true;
                    }
                }
            }
            //
        }

        private void outputIntSigWindowByController(int startX, int startY, int height, int width, ControllerInfo cont, int R = 492, int G = 492, int B = 492)
        {
            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            setIntSigWindowCommand(ref cmd, startX, startY, height, width, R, G, B);

            sendSdcpCommand(cmd, 0, cont.IPAddress);

            Thread.Sleep(Settings.Ins.IntSignalWait);
        }

#else
        private void outputGapCamTargetArea_EdgeExpand(List<UnitInfo> lstTgtUnit, bool white = false)
        {
            int startX = int.MaxValue, startY = int.MaxValue, maxX = 0, maxY = 0;
            int height = 0, width = 0;

            foreach (UnitInfo unit in lstTgtUnit)
            {
                if (unit.PixelX < startX)
                { startX = unit.PixelX; }

                if (unit.PixelY < startY)
                { startY = unit.PixelY; }

                if (unit.PixelX > maxX)
                { maxX = unit.PixelX; }

                if (unit.PixelY > maxY)
                { maxY = unit.PixelY; }
            }
            maxX += cabiDx - 1;
            maxY += cabiDy - 1;

            bool topEdge, bottomEdge, leftEdge, rightEdge;
            checkWallEdge(lstTgtUnit, out topEdge, out bottomEdge, out leftEdge, out rightEdge);

            if (topEdge != true)
                startY -= m_ModuleDy;
            if (leftEdge != true)
                startX -= m_ModuleDx;

            if (bottomEdge != true)
                maxY += m_ModuleDy;
            if (rightEdge != true)
                maxX += m_ModuleDx;

            height = maxY - startY + 1;
            width = maxX - startX + 1;

#if NO_CONTROLLER
            return;
#endif
            if (white != true)
                outputIntSigWindow(startX, startY, height, width, 0, m_MeasureLevel, 0);
            else
                outputIntSigWindow(startX, startY, height, width, m_MeasureLevel, m_MeasureLevel, m_MeasureLevel);
        }
#endif
        // 

        void checkWallEdge(List<UnitInfo> lstTgtUnit, out bool topEdge, out bool bottomEdge, out bool leftEdge, out bool rightEdge)
        {
            // modified by Hotta 2022/08/29 離れ小島対策（1ケのProfileに、不連続なWalが複数存在する場合の対策

            /*
            int startX = int.MaxValue, startY = int.MaxValue, endX = 0, endY = 0;

            foreach (UnitInfo unit in lstTgtUnit)
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
            endX += cabiDx - 1;
            endY += cabiDy - 1;

            int wallMaxX = 0;
            int wallMaxY = 0;
            for (int y = 0; y < allocInfo.MaxY; y++)
            {
                for (int x = 0; x < allocInfo.MaxX; x++)
                {
                    if (allocInfo.lstUnits[x][y] == null)
                    { continue; }

                    if (wallMaxX < allocInfo.lstUnits[x][y].PixelX + m_CabinetDx - 1)
                        wallMaxX = allocInfo.lstUnits[x][y].PixelX + m_CabinetDx - 1;

                    if (wallMaxY < allocInfo.lstUnits[x][y].PixelY + m_CabinetDy - 1)
                        wallMaxY = allocInfo.lstUnits[x][y].PixelY + m_CabinetDy - 1;
                }
            }

            if (startX > 0)
                leftEdge = false;
            else
                leftEdge = true;

            if (startY > 0)
                topEdge = false;
            else
                topEdge = true;

            if (endX < wallMaxX)
                rightEdge = false;
            else
                rightEdge = true;

            if (endY < wallMaxY)
                bottomEdge = false;
            else
                bottomEdge = true;
            */

            // 選択されたCabinetを含む、物理的に連続したCabinetの範囲を取得する
            int startX = int.MaxValue, startY = int.MaxValue, endX = int.MinValue, endY = int.MinValue;

            foreach (UnitInfo unit in lstTgtUnit)
            {
                if (unit.X < startX)
                { startX = unit.X; }

                if (unit.Y < startY)
                { startY = unit.Y; }

                if (unit.X > endX)
                { endX = unit.X; }

                if (unit.Y > endY)
                { endY = unit.Y; }
            }
            // インデックスの開始値を、1→0に変更
            startX--;
            startY--;
            endX--;
            endY--;

            leftEdge = false;
            if (startX <= 0)
            {
                leftEdge = true;
            }
            else
            {
                for(int y = startY; y <= endY; y++)
                {
                    if (allocInfo.lstUnits[startX - 1][y] == null)
                    {
                        leftEdge = true;
                        break;
                    }
                }
            }

            rightEdge = false;
            if (endX >= allocInfo.lstUnits.Count - 1)
            {
                rightEdge = true;
            }
            else
            {
                for (int y = startY; y <= endY; y++)
                {
                    if (allocInfo.lstUnits[endX + 1][y] == null)
                    {
                        rightEdge = true;
                        break;
                    }
                }
            }

            topEdge = false;
            if (startY <= 0)
            {
                topEdge = true;
            }
            else
            {
                for (int x = startX; x <= endX; x++)
                {
                    if (allocInfo.lstUnits[x][startY - 1] == null)
                    {
                        topEdge = true;
                        break;
                    }
                }
            }

            bottomEdge = false;
            if(endY >= allocInfo.lstUnits[0].Count - 1) // allocInfo.lstUnits[?].Countは、インデックスに関係なく、同じ値のはず
            {
                bottomEdge = true;
            }
            else
            {
                for (int x = startX; x <= endX; x++)
                {
                    if (allocInfo.lstUnits[x][endY + 1] == null)
                    {
                        bottomEdge = true;
                        break;
                    }
                }
            }
        }
#endif

#endregion Set Pos

#region Measurement(Gap)

        //static string m_CamMeasPath_0 = @"C:\CAS\Measurement\Gap_202301201145\";
        static string m_CamMeasPath_0 = @"C:\CAS\Measurement\Gap_202501091100\";
        string m_CamMeasPath = m_CamMeasPath_0;

        unsafe private void measureGapAsync(List<UnitInfo> lstTgtUnit)
        {
            m_GapStatus = GapStatus.Measure;
            m_AdjustCount = 0;

            clearGapResult(DispType.Measure);

#if NO_CAP
            Thread.Sleep(1000);
#endif




            // added by Hotta 2022/12/15
            int startX = int.MaxValue;
            int endX = int.MinValue;
            int startY = int.MaxValue;
            int endY = int.MinValue;
            foreach (UnitInfo unit in lstTgtUnit)
            {
                if (startX > unit.X)
                    startX = unit.X;
                if (endX < unit.X)
                    endX = unit.X;
                if (startY > unit.Y)
                    startY = unit.Y;
                if (endY < unit.Y)
                    endY = unit.Y;
            }
            saveLog(string.Format("Target Cabinet X : {0} - {1}, Y : {2} - {3}", startX, endX, startY, endY));
            //

            // 全体のStep数を設定
            winProgress.SetWholeSteps(66);

            lstGapCamCp = new List<GapCamCorrectionValue>();

            // OpenCVSharpのDllがあるか確認する
            CheckOpenCvSharpDll();
            // 信号レベルの設定
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
                m_MeasureLevel = Settings.Ins.GapCam.MeasLevel_BModel;
            }

            // カメラパラメータの設定
            if (Settings.Ins.Camera.Name == "ILCE-6400")
            {
                m_ShootCondition = Settings.Ins.GapCam.Setting_A6400;
            }
            else
            {
                m_ShootCondition = Settings.Ins.GapCam.Setting_A7;
            }


            List<int> listX = new List<int>();
            List<int> listY = new List<int>();
            foreach (UnitInfo unit in lstTgtUnit)
            {
                if (listX.Contains(unit.X) != true)
                {
                    listX.Add(unit.X);
                }
                if (listY.Contains(unit.Y) != true)
                {
                    listY.Add(unit.Y);
                }
            }
            m_CabinetXNum = listX.Count;
            m_CabinetYNum = listY.Count;
            if (allocInfo.LEDModel == ZRD_C12A 
                || allocInfo.LEDModel == ZRD_B12A 
                || allocInfo.LEDModel == ZRD_CH12D 
                || allocInfo.LEDModel == ZRD_BH12D
                || allocInfo.LEDModel == ZRD_CH12D_S3
                || allocInfo.LEDModel == ZRD_BH12D_S3)
            {
                // deleted by Hotta 2023/03/03
                /*
                m_LedPitch = (float)RealPitch_P12;
                */
                // added by Hotta 2023/04/28
#if NO_CONTROLLER
                m_LedPitch = (float)RealPitch_P12;
#endif

                m_CabinetDx = CabinetDxP12;
                m_CabinetDy = CabinetDyP12;

                if (allocInfo.LEDModel == ZRD_C12A 
                    || allocInfo.LEDModel == ZRD_B12A)
                {
                    m_ModuleDx = ModuleDxP12_Mdoule4x2;
                    m_ModuleDy = ModuleDyP12_Module4x2;
                }
                else
                {
                    m_ModuleDx = ModuleDxP12_Module4x3;
                    m_ModuleDy = ModuleDyP12_Module4x3;
                }
            }
            else if (allocInfo.LEDModel == ZRD_C15A 
                || allocInfo.LEDModel == ZRD_B15A 
                || allocInfo.LEDModel == ZRD_CH15D 
                || allocInfo.LEDModel == ZRD_BH15D
                || allocInfo.LEDModel == ZRD_CH15D_S3
                || allocInfo.LEDModel == ZRD_BH15D_S3)
            {
                // deleted by Hotta 2023/03/03
                /*
                m_LedPitch = (float)RealPitch_P15;
                */
                // added by Hotta 2023/04/28
#if NO_CONTROLLER
                m_LedPitch = (float)RealPitch_P15;
#endif
                m_CabinetDx = CabinetDxP15;
                m_CabinetDy = CabinetDyP15;

                if (allocInfo.LEDModel == ZRD_C15A 
                    || allocInfo.LEDModel == ZRD_B15A)
                {
                    m_ModuleDx = ModuleDxP15_Module4x2;
                    m_ModuleDy = ModuleDyP15_Module4x2;
                }
                else
                {
                    m_ModuleDx = ModuleDxP15_Module4x3;
                    m_ModuleDy = ModuleDyP15_Module4x3;
                }
            }
            else
            { 
                return;
            }

            m_ModuleXNum = m_CabinetDx / m_ModuleDx;
            m_ModuleYNum = m_CabinetDy / m_ModuleDy;

            m_PanelDx = m_CabinetDx * m_CabinetXNum;
            m_PanelDy = m_CabinetDy * m_CabinetYNum;


#if NO_CONTROLLER
#else
            // added by Hotta 2022/12/19 for 対象コントローラの取得
            foreach(ControllerInfo controller in dicController.Values)
                controller.Target = false;
            outputGapCamTargetArea_EdgeExpand(lstTgtUnit, ExpandType.Both, true);
            // added by Hotta 2023/01/06
            // 画質設定するコントローラは、補正対象Cabinetだけでなく、カメラ位置合わせのCabinetに接続されているものも対象になる
            outputGapCamTargetArea_EdgeExpand(m_lstCamPosUnits, ExpandType.Both, true);
            //
            Thread.Sleep(Settings.Ins.Camera.PatternWait);
            //

            // Cabinet on
            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdUnitPowerOn, cont.IPAddress); }
            Thread.Sleep(5000);

            // カメラの目標位置を再設定
            //SetTargetCamPos(lstTgtUnit);

            // ユーザー設定を保存 1Step
            List<UserSetting> lstUserSettings;
            winProgress.ShowMessage("Store User Settings.");
            winProgress.PutForward1Step();
            saveLog("Store User Settings.");
            // modified by Hotta 2023/01/23
            /*
            getUserSetting(out lstUserSettings);
            */
            bool st = getUserSetting(out lstUserSettings);
            if (st == true)
                m_lstUserSetting = lstUserSettings;
            //

            // 調整用設定 1Step
            winProgress.ShowMessage("Set Adjust Settings.");
            winProgress.PutForward1Step();
            saveLog("Set Adjust Settings.");
            setAdjustSetting();

            // added by Hotta 2022/08/08
            // Layout info : off
            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdDispUnitAddrOff, 0, cont.IPAddress); }
#endif

            int processSec = calcGapCameraMeasurementProcessSec(1);
            winProgress.SetRemainTimer(processSec);

#if NO_CAP
#else
            // AutoFocus
            // AutoFocus
            winProgress.ShowMessage("Auto focus.");
            winProgress.PutForward1Step();
            saveLog("Auto focus.");
            outputIntSigChecker(0, 0, modDy, modDx, modDx, modDy, m_MeasureLevel, m_MeasureLevel, m_MeasureLevel); // 20IRE

            // added by Hotta 2022/07/26
            Thread.Sleep(Settings.Ins.Camera.PatternWait);
            //

            /*
            saveLog(string.Format("CameraSetting:{0},{1},{2},{3},{4},{5}",
                Settings.Ins.Camera.SetPosSetting.FNumber,
                Settings.Ins.Camera.SetPosSetting.Shutter,
                Settings.Ins.Camera.SetPosSetting.ISO,
                Settings.Ins.Camera.SetPosSetting.WB,
                Settings.Ins.Camera.SetPosSetting.CompressionType,
                Settings.Ins.Camera.SetPosSetting.ImageSize));
            AutoFocus(Settings.Ins.Camera.SetPosSetting);
            */
            saveLog(string.Format("CameraSetting:{0},{1},{2},{3},{4},{5}",
                m_ShootCondition.FNumber,
                m_ShootCondition.Shutter,
                m_ShootCondition.ISO,
                m_ShootCondition.WB,
                m_ShootCondition.CompressionType,
                m_ShootCondition.ImageSize));
            //AutoFocus(m_ShootCondition);
            AutoFocus(m_ShootCondition, new AfAreaSetting());

            // added by Hotta 2022/07/26
            AcqARW arw;
            Mat mat;
            int width, height;
            ushort* pMat;

            if (File.Exists(m_CamMeasPath + "AutoFocus.arw") == true)
            {
                if (checkFileSize(m_CamMeasPath + "AutoFocus.arw") != true)
                {
                    throw new Exception("Saving the " + m_CamMeasPath + "AutoFocus.arw" + " file does not completed.");
                }

                try
                {
                    loadArwFile(m_CamMeasPath + "AutoFocus.arw", out arw);
                }
                // added by Hotta 2025/01/31
                catch(CameraCasUserAbortException ex)
                {
                    throw ex;
                }
                //
                catch //(Exception ex)
                {
                    Thread.Sleep(1000);
                    loadArwFile(m_CamMeasPath + "AutoFocus.arw", out arw);
                }
                width = arw.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
                height = arw.RawMainIFD.ImageHeight / 2;
                mat = new Mat(new Size(width, height), MatType.CV_16UC1);
                pMat = (ushort*)(mat.Data);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        pMat[y * width + x] = arw.RawData[1][x, y];
                    }
                }
                try
                {
                    SaveMatBinary(mat, m_CamMeasPath + "AutoFocus");
                }
                catch //(Exception ex)
                {
                    Thread.Sleep(1000);
                    SaveMatBinary(mat, m_CamMeasPath + "AutoFocus");
                }
                mat.Dispose();
                arw = null;
            }
            //
#endif

            processSec = calcGapCameraMeasurementProcessSec(2);
            winProgress.SetRemainTimer(processSec);

            // 開始時のカメラ位置を保存 1Step
            winProgress.ShowMessage("Store Camera Position.");
            winProgress.PutForward1Step();
            saveLog("Store Camera Position.");

            // modified by Hotta 2023/01/30
            /*
            // modified by Hotta 2022/12/15
            /*
            // added by Hotta 2022/08/02
            // SetCamPosTarget();
            //
            if (allocInfo.LEDModel == ZRD_C15A || allocInfo.LEDModel == ZRD_B15A || allocInfo.LEDModel == ZRD_CH15D || allocInfo.LEDModel == ZRD_BH15D)
                SetCamPosTarget(ImageType_CamPos.LiveView, true, m_lstCamPosUnits, 8000.0);
            else
                SetCamPosTarget(ImageType_CamPos.LiveView, true, m_lstCamPosUnits, 8000.0 * (1.26 / 1.58));
            */
            if (allocInfo.LEDModel == ZRD_C15A
                || allocInfo.LEDModel == ZRD_B15A
                || allocInfo.LEDModel == ZRD_CH15D
                || allocInfo.LEDModel == ZRD_BH15D
                || allocInfo.LEDModel == ZRD_CH15D_S3
                || allocInfo.LEDModel == ZRD_BH15D_S3)
            {
                SetCamPosTarget(ImageType_CamPos.LiveView, true, lstTgtUnit, 8000.0);
            }
            else
            {
                SetCamPosTarget(ImageType_CamPos.LiveView, true, lstTgtUnit, 8000.0 * (1.26 / 1.58));
            }

            //

            bool status = true;
            // modified by Hotta 2022/06/21 for カメラ位置
            /*
            for(int retry=0;retry<3;retry++)
            {
                status = GetCameraPos(lstTgtUnit, out CameraPosition startCamPos);
                if (startCamPos != null)
                    saveLog(string.Format("X={0:F5},Y={1:F5},Z={2:F5},Pan={3:F5},Tilt={4:F5},Roll={5:F5}", startCamPos.X, startCamPos.Y, startCamPos.Z, startCamPos.Pan, startCamPos.Tilt, startCamPos.Roll));
                if (status == true)
                    break;
            }
            if (status != true)
            {
                saveLog("The camera position is inappropriate.");
                throw new Exception("The camera position is inappropriate.\r\nPlease align the camera position again.");
            }
            */
            for (int retry = 0; retry < 3; retry++)
            {
                status = GetCameraPosition(imgGapCamMeasure);
                saveLog(string.Format("X={0:F2},Y={1:F2},Z={2:F2},Pan={3:F2},Tilt={4:F2},Roll={5:F2}", m_CamPos_Tx, m_CamPos_Ty, m_CamPos_Tz, m_CamPos_Pan, m_CamPos_Tilt, m_CamPos_Roll));
                saveLog(string.Format("Top={0:F1},Bottom={1:F1},Left={2:F1},Right={3:F1}", m_CamPos_TopLen, m_CamPos_BottomLen, m_CamPos_LeftLen, m_CamPos_RightLen));
                if (status == true)
                    break;
            }
            if (status != true)
            {
                saveLog("The camera position is inappropriate.");
                if (appliMode != ApplicationMode.Developer)
                    throw new Exception("The camera position is inappropriate.\r\nPlease align the camera position again.");
            }

            processSec = calcGapCameraMeasurementProcessSec(3);
            winProgress.SetRemainTimer(processSec);

            // 撮影
            captureGapImages(m_CamMeasPath); // 11step

            // added by Hotta 2024/01/09
            // ESCキー押下による中断を無効化
            winProgress.AbortType = WindowProgress.TAbortType.None;
            //

            processSec = calcGapCameraMeasurementProcessSec(4);
            winProgress.SetRemainTimer(processSec);

            // 処理
            calcGapGain(lstTgtUnit, m_CamMeasPath); // 6step

            // 有用な情報がないので、保存しない
            /*
            // 補正値関係以外をクリアしたインスタンス作成
            List<GapCamCorrectionValue> _lstGapCamCp = new List<GapCamCorrectionValue>();
            for(int n=0;n<lstGapCamCp.Count;n++)
            {
                _lstGapCamCp.Add(new GapCamCorrectionValue());
                _lstGapCamCp[n].Unit = lstGapCamCp[n].Unit;
                _lstGapCamCp[n].CvUnit = lstGapCamCp[n].CvUnit;
                _lstGapCamCp[n].AryCvCell = new GapCellCorrectValue[lstGapCamCp[n].AryCvCell.Length];
                for(int i=0;i<lstGapCamCp[n].AryCvCell.Length;i++)
                {
                    _lstGapCamCp[n].AryCvCell[i] = lstGapCamCp[n].AryCvCell[i];
                }
            }

            // 測定結果を保存
            //GapCamCorrectionValue.SaveToXmlFile(m_GapCamMeasPath + "GapMeasResult.xml", lstGapCamCp); // 1step
            GapCamCorrectionValue.SaveToXmlFile(m_GapCamMeasPath + "GapMeasResult.xml", _lstGapCamCp); // 1step
            */

#if NO_CAP
#else
            // deleted by Hotta 2022/07/26
            // カメラ設定を変えないので、オートフォーカスは不要
            //// AutoFocus
            //winProgress.ShowMessage("Auto focus.");
            //winProgress.PutForward1Step();
            //saveLog("Auto focus.");
            //outputIntSigChecker(0, 0, modDy, modDx, modDx, modDy, m_MeasureLevel, m_MeasureLevel, m_MeasureLevel); // 20IRE
            // added by Hotta 2022/07/26
            //Thread.Sleep(Settings.Ins.Camera.PatternWait);
            //
            ///*
            //saveLog(string.Format("CameraSetting:{0},{1},{2},{3},{4},{5}",
            //    Settings.Ins.Camera.SetPosSetting.FNumber,
            //    Settings.Ins.Camera.SetPosSetting.Shutter,
            //    Settings.Ins.Camera.SetPosSetting.ISO,
            //    Settings.Ins.Camera.SetPosSetting.WB,
            //    Settings.Ins.Camera.SetPosSetting.CompressionType,
            //    Settings.Ins.Camera.SetPosSetting.ImageSize));
            //AutoFocus(Settings.Ins.Camera.SetPosSetting);
            //*/
            //saveLog(string.Format("CameraSetting:{0},{1},{2},{3},{4},{5}",
            //    m_ShootCondition.FNumber,
            //    m_ShootCondition.Shutter,
            //    m_ShootCondition.ISO,
            //    m_ShootCondition.WB,
            //    m_ShootCondition.CompressionType,
            //    m_ShootCondition.ImageSize));
            ////AutoFocus(m_ShootCondition);
            //AutoFocus(m_ShootCondition, new AfAreaSetting());


            processSec = calcGapCameraMeasurementProcessSec(5);
            winProgress.SetRemainTimer(processSec);

            // 終了時のカメラ位置を保存 1Step
            winProgress.ShowMessage("Store Camera Position.");
            winProgress.PutForward1Step();
            saveLog("Store Camera Position.");
            // modified by Hotta 2022/06/21 for カメラ位置
            /*
            for(int retry=0;retry<3;retry++)
            {
                status = GetCameraPos(lstTgtUnit, out CameraPosition endCamPos);
                if (endCamPos != null)
                    saveLog(string.Format("X={0:F5},Y={1:F5},Z={2:F5},Pan={3:F5},Tilt={4:F5},Roll={5:F5}", endCamPos.X, endCamPos.Y, endCamPos.Z, endCamPos.Pan, endCamPos.Tilt, endCamPos.Roll));
                if (status == true)
                    break;
            }
            */
            for (int retry = 0; retry < 3; retry++)
            {
                status = GetCameraPosition(imgGapCamMeasure);
                saveLog(string.Format("X={0:F2},Y={1:F2},Z={2:F2},Pan={3:F2},Tilt={4:F2},Roll={5:F2}", m_CamPos_Tx, m_CamPos_Ty, m_CamPos_Tz, m_CamPos_Pan, m_CamPos_Tilt, m_CamPos_Roll));
                saveLog(string.Format("Top={0:F1},Bottom={1:F1},Left={2:F1},Right={3:F1}", m_CamPos_TopLen, m_CamPos_BottomLen, m_CamPos_LeftLen, m_CamPos_RightLen));
                if (status == true)
                    break;
            }
            if (status != true)
            {
                saveLog("The camera position is inappropriate.");
                if (appliMode != ApplicationMode.Developer)
                    throw new Exception("The camera position is inappropriate.\r\nPlease align the camera position again.");
            }
#endif

#if NO_CONTROLLER
#else

            // 調整設定を解除 1Step
            winProgress.ShowMessage("Set Normal Settings.");
            winProgress.PutForward1Step();
            saveLog("Set Normal Settings.");
            // modified by Hotta 2023/04/28 for v1.06
            /*
            setNormalSetting();
            */
            SetThroughMode(false);

            // ユーザー設定に書き戻し 1Step
            winProgress.ShowMessage("Restore User Settings.");
            winProgress.PutForward1Step();
            saveLog("Restore User Settings.");
            setUserSetting(lstUserSettings);

            // added by Hotta 2023/01/23
            // 正常に設定を戻したので、m_lstUserSetting = nullにして、呼び出し側のfinally分をスキップ
            m_lstUserSetting = null;
            //
#endif
#if NO_CAP
#else
            if (status != true)
            {
                saveLog("The camera position is inappropriate.");
                throw new Exception("The camera position is inappropriate.\r\nPlease align the camera position again.");
            }
#endif

            // 表示
            dispGapResult();

#if NO_CONTROLLER
#else
            outputIntSigFlat(m_MeasureLevel, m_MeasureLevel, m_MeasureLevel);
            if (m_MeasureLevel == 492)
            {
                Dispatcher.Invoke(new Action(() => { cmbxPatternGapCam.SelectedIndex = 2; }));
            }
            else
            {
                Dispatcher.Invoke(new Action(() => { cmbxPatternGapCam.SelectedIndex = 1; }));
            }
#endif

            // Logの世代管理
            ManageLogGen(applicationPath + MeasDir, "Gap_");

            saveLog("Finish Measurement.");
        }

        unsafe private void captureGapImages(string measPath)
        {
            AcqARW arw;
            Mat mat;
            int width, height;
            ushort* pMat;

            string imgPath;
            List<UnitInfo> lstTgtUnit = new List<UnitInfo>();

            // added by Hotta 2022/11/10 for 複数コントローラ
            // 最初に1回、初期化しておく
#if MultiController
            if (m_GapStatus == GapStatus.Before || m_GapStatus == GapStatus.Measure)
                m_bottomHalfTile = m_rightHalfTile = false;
#endif


            // Unitが選択されているか、矩形になっているか確認
            // modified by Hotta 2022/12/22
            /*
            base.Dispatcher.Invoke(() => CheckSelectedUnits(aryUnitGapCam, out lstTgtUnit));
            */
            base.Dispatcher.Invoke(() => CheckSelectedUnits(aryUnitGapCam, out lstTgtUnit, true, out m_lstCamPosUnits, true));


#if NO_CONTROLLER
#else
            // modified by Hotta 2022/11/10 for 映り込み
#if Reflection

#else
            // カメラ位置と同じ絞りで実行したいため、このタイミングで行う。
            // added by Hotta 2022/02/01
            if (m_GapStatus == GapStatus.Before || m_GapStatus == GapStatus.Measure)
            {
                // 照明の映り込みをチェック
                // 目地補正のカメラ設定：F11, 1/8s
                // U/F調整のカメラ設定 ：F22, 0.5s
                // 絞りが4倍、SSが1/4倍の関係にあり、同じ明るさで撮影される。
                // そのため、映り込み検査のスペックは、U/F調整のものをそのまま流用する。
                winProgress.ShowMessage("Check Light Reflections.");
                winProgress.PutForward1Step();
                saveLog("Check Light Reflections.");
                CheckLightingReflection(lstTgtUnit, true);
            }
            //
#endif  // Reflection

#endif

            // modified by Hotta 2021/09/01
#if NO_CAP
            // deleted by Hotta 2022/02/18
            // いらないはず。コピペの失敗？
            /*
            winProgress.ShowMessage("Set Camera Settings. (TrimArea)");
            winProgress.PutForward1Step();

            imgPath = measPath + System.IO.Path.GetFileNameWithoutExtension(fn_AreaFile);

            // TrimmingArea
            captureGapTrimmingAreaImage(measPath); // 5step
            */
#else
            // deleted by Hotta 2022/06/21
            // カメラ設定を変更していないので、不要
            /*　
            // オートフォーカス
            if (m_GapStatus == GapStatus.Before || m_GapStatus == GapStatus.Measure)
            {
                winProgress.ShowMessage("Auto Focus");
                winProgress.PutForward1Step();
                saveLog("Auto focus.");

#if NO_CONTROLLER
#else
                // modified by Hotta 2022/02/28
                // outputIntSigHatchInv(1, 1, modDy - 2, modDx - 2, modDx, modDy, m_MeasureLevel, m_MeasureLevel, m_MeasureLevel);
                outputIntSigChecker(0, 0, modDy, modDx, modDx, modDy, m_MeasureLevel, m_MeasureLevel, m_MeasureLevel);
                // added by Hotta 2022/07/26
                //Thread.Sleep(Settings.Ins.Camera.PatternWait);
                //
#endif
                saveLog(string.Format("CameraSetting:{0},{1},{2},{3},{4},{5}",
                    m_ShootCondition.FNumber,
                    m_ShootCondition.Shutter,
                    m_ShootCondition.ISO,
                    m_ShootCondition.WB,
                    m_ShootCondition.CompressionType,
                    m_ShootCondition.ImageSize));
                AutoFocus(m_ShootCondition);
                if (File.Exists(measPath + fn_AutoFocus + ".arw") == true)
                {
                    if(checkFileSize(measPath + fn_AutoFocus + ".arw") != true)
                    {
                        throw new Exception("Saving the " + measPath + fn_AutoFocus + ".arw" + " file does not completed.");
                    }
                    try
                    {
                        loadArwFile(measPath + fn_AutoFocus + ".arw", out arw);
                    }
                    catch(Exception ex)
                    {
                        Thread.Sleep(1000);
                        loadArwFile(measPath + fn_AutoFocus + ".arw", out arw);
                    }
                    width = arw.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
                    height = arw.RawMainIFD.ImageHeight / 2;
                    mat = new Mat(new Size(width, height), MatType.CV_16UC1);
                    pMat = (ushort*)(mat.Data);

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            pMat[y * width + x] = arw.RawData[1][x, y];
                        }
                    }
                    try
                    {
                        SaveMatBinary(mat, measPath + fn_AutoFocus);
                    }
                    catch(Exception ex)
                    {
                        Thread.Sleep(1000);
                        SaveMatBinary(mat, measPath + fn_AutoFocus);
                    }
                    mat.Dispose();
                    arw = null;
                }


                //Thread.Sleep(5000);
            }
            */
#endif

            // 全黒信号出力
            if (m_GapStatus == GapStatus.Before || m_GapStatus == GapStatus.Measure)
            {
                winProgress.ShowMessage("Capture Black image");
                winProgress.PutForward1Step();
                saveLog("Capture Black image.");
#if NO_CONTROLLER

#else
                outputIntSigFlat(0, 0, 0);

                // added by Hotta 2022/07/26
                Thread.Sleep(Settings.Ins.Camera.PatternWait);
                //
#endif


                // added by Hotta 2022/10/11 for 映り込み
#if Reflection
                for (int cap = 0; cap < CaptureNum; cap++)
                {
                    imgPath = measPath + fn_BlackFile + "_" + cap.ToString();
#if NO_CAP

#else
                    saveLog(string.Format("CameraSetting:{0},{1},{2},{3},{4},{5}",
                    m_ShootCondition.FNumber,
                    m_ShootCondition.Shutter,
                    m_ShootCondition.ISO,
                    m_ShootCondition.WB,
                    m_ShootCondition.CompressionType,
                    m_ShootCondition.ImageSize));


                    CaptureImage(imgPath, m_ShootCondition);
#endif
                    if (File.Exists(imgPath + ".arw") == true)
                    {
                        if (checkFileSize(imgPath + ".arw") != true)
                        {
                            throw new Exception("Saving the " + imgPath + ".arw" + " file does not completed.");
                        }

                        try
                        {
                            loadArwFile(imgPath + ".arw", out arw);
                        }
                        // added by Hotta 2025/01/31
                        catch (CameraCasUserAbortException ex)
                        {
                            throw ex;
                        }
                        //
                        catch //(Exception ex)
                        {
                            Thread.Sleep(1000);
                            loadArwFile(imgPath + ".arw", out arw);
                        }
                        width = arw.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
                        height = arw.RawMainIFD.ImageHeight / 2;
                        mat = new Mat(new Size(width, height), MatType.CV_16UC1);
                        pMat = (ushort*)(mat.Data);

                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                pMat[y * width + x] = arw.RawData[1][x, y];
                            }
                        }
                        try
                        {
                            SaveMatBinary(mat,imgPath);
                        }
                        catch //(Exception ex)
                        {
                            Thread.Sleep(1000);
                            SaveMatBinary(mat, imgPath);
                        }
                        mat.Dispose();
                        arw = null;
                    }
                }
            }
#else


#if NO_CAP

#else
                saveLog(string.Format("CameraSetting:{0},{1},{2},{3},{4},{5}",
                m_ShootCondition.FNumber,
                m_ShootCondition.Shutter,
                m_ShootCondition.ISO,
                m_ShootCondition.WB,
                m_ShootCondition.CompressionType,
                m_ShootCondition.ImageSize));

                CaptureImage(measPath + fn_BlackFile, m_ShootCondition);
#endif
                if(File.Exists(measPath + fn_BlackFile + ".arw") == true)
                {
                    if (checkFileSize(measPath + fn_BlackFile + ".arw") != true)
                    {
                        throw new Exception("Saving the " + measPath + fn_BlackFile + ".arw" + " file does not completed.");
                    }

                    try
                    {
                        loadArwFile(measPath + fn_BlackFile + ".arw", out arw);
                    }
                    catch(Exception ex)
                    {
                        Thread.Sleep(1000);
                        loadArwFile(measPath + fn_BlackFile + ".arw", out arw);
                    }
                    width = arw.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
                    height = arw.RawMainIFD.ImageHeight / 2;
                    mat = new Mat(new Size(width, height), MatType.CV_16UC1);
                    pMat = (ushort*)(mat.Data);

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            pMat[y * width + x] = arw.RawData[1][x, y];
                        }
                    }
                    try
                    {
                        SaveMatBinary(mat, measPath + fn_BlackFile);
                    }
                    catch(Exception ex)
                    {
                        Thread.Sleep(1000);
                        SaveMatBinary(mat, measPath + fn_BlackFile);
                    }
                    mat.Dispose();
                    arw = null;
                }
            }
#endif

            // ラスター出力
            if (m_GapStatus == GapStatus.Before || m_GapStatus == GapStatus.Measure)
            {
                winProgress.ShowMessage("Capture Flat image");
                winProgress.PutForward1Step();
                saveLog("Capture Flat image.");
#if NO_CONTROLLER
                // modified by Hotta 2022/10/28
#if MultiController
                outputGapCamTargetArea_EdgeExpand(lstTgtUnit, ExpandType.Top);
                outputGapCamTargetArea_EdgeExpand(lstTgtUnit, ExpandType.Right);
#else
                outputGapCamTargetArea_EdgeExpand(lstTgtUnit);
#endif
#else

                // added by Hotta 2022/11/08 for 複数コントローラ
#if MultiController
                outputGapCamTargetArea_EdgeExpand(lstTgtUnit, ExpandType.Both);
#else
                // added by Hotta 2022/07/08
#if CorrectTargetEdge
                outputGapCamTargetArea_EdgeExpand(lstTgtUnit);
#else
                outputGapCamTargetArea(lstTgtUnit);
#endif

#endif
                // added by Hotta 2022/07/26
                Thread.Sleep(Settings.Ins.Camera.PatternWait);
                //

#endif

#if NO_CAP

#else
                saveLog(string.Format("CameraSetting:{0},{1},{2},{3},{4},{5}",
                    m_ShootCondition.FNumber,
                    m_ShootCondition.Shutter,
                    m_ShootCondition.ISO,
                    m_ShootCondition.WB,
                    m_ShootCondition.CompressionType,
                    m_ShootCondition.ImageSize));
                CaptureImage(measPath + fn_Flat, m_ShootCondition);
#endif
                if (File.Exists(measPath + fn_Flat + ".arw") == true)
                {
                    if (checkFileSize(measPath + fn_Flat + ".arw") != true)
                    {
                        throw new Exception("Saving the " + measPath + fn_Flat + ".arw" + " file does not completed.");
                    }

                    try
                    {
                        loadArwFile(measPath + fn_Flat + ".arw", out arw);
                    }
                    // added by Hotta 2025/01/31
                    catch (CameraCasUserAbortException ex)
                    {
                        throw ex;
                    }
                    //
                    catch //(Exception ex)
                    {
                        Thread.Sleep(1000);
                        loadArwFile(measPath + fn_Flat + ".arw", out arw);
                    }
                    width = arw.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
                    height = arw.RawMainIFD.ImageHeight / 2;
                    mat = new Mat(new Size(width, height), MatType.CV_16UC1);
                    pMat = (ushort*)(mat.Data);

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            pMat[y * width + x] = arw.RawData[1][x, y];
                        }
                    }
                    try
                    {
                        SaveMatBinary(mat, measPath + fn_Flat);
                    }
                    catch //(Exception ex)
                    {
                        Thread.Sleep(1000);
                        SaveMatBinary(mat, measPath + fn_Flat);
                    }
                    mat.Dispose();
                    arw = null;
                }
            }

            // 全白信号出力
            if (m_GapStatus == GapStatus.Before || m_GapStatus == GapStatus.Measure)
            {
                winProgress.ShowMessage("Capture White image");
                winProgress.PutForward1Step();
                saveLog("Capture White image.");

                string fname;
                if (m_GapStatus == GapStatus.Before)
                    fname = measPath + fn_WhiteBeforeFile;
                else
                    fname = measPath + fn_WhiteMeasureFile;
#if NO_CONTROLLER

#else
                // modified by Hotta 2022/07/08
#if CorrectTargetEdge

                // modified by Hotta 2022/11/08
#if MultiController
                outputGapCamTargetArea_EdgeExpand(lstTgtUnit, ExpandType.Both, true);
#else
                outputGapCamTargetArea_EdgeExpand(lstTgtUnit, true);
#endif
#else
                outputGapCamTargetArea(lstTgtUnit, true);
#endif
#endif
                // added by Hotta 2022/07/26
                Thread.Sleep(Settings.Ins.Camera.PatternWait);
                //
#if NO_CAP
#else
                saveLog(string.Format("CameraSetting:{0},{1},{2},{3},{4},{5}",
                    m_ShootCondition.FNumber,
                    m_ShootCondition.Shutter,
                    m_ShootCondition.ISO,
                    m_ShootCondition.WB,
                    m_ShootCondition.CompressionType,
                    m_ShootCondition.ImageSize));
                CaptureImage(fname, m_ShootCondition);
#endif
                if(File.Exists(fname + ".arw") == true)
                {
                    if (checkFileSize(fname + ".arw") != true)
                    {
                        throw new Exception("Saving the " + fname + ".arw" + " file does not completed.");
                    }

                    try
                    {
                        loadArwFile(fname + ".arw", out arw);
                    }
                    // added by Hotta 2025/01/31
                    catch (CameraCasUserAbortException ex)
                    {
                        throw ex;
                    }
                    //
                    catch //(Exception ex)
                    {
                        Thread.Sleep(1000);
                        loadArwFile(fname + ".arw", out arw);
                    }
                    width = arw.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
                    height = arw.RawMainIFD.ImageHeight / 2;
                    mat = new Mat(new Size(width, height), MatType.CV_16UC3);
                    int ch = mat.Channels();
                    int step = width * ch;
                    pMat = (ushort*)(mat.Data);

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            pMat[y * step + x * ch + 2] = arw.RawData[0][x, y];
                            pMat[y * step + x * ch + 1] = arw.RawData[1][x, y];
                            pMat[y * step + x * ch + 0] = arw.RawData[2][x, y];
                        }
                    }
                    try
                    {
                        SaveMatBinary(mat, fname);
                    }
                    catch //(Exception ex)
                    {
                        Thread.Sleep(1000);
                        SaveMatBinary(mat, fname);
                    }

                    mat.Dispose();
                    arw = null;
                }
            }



            // 位置合せ用信号出力
            if (m_GapStatus == GapStatus.Before || m_GapStatus == GapStatus.Measure)
            {
                winProgress.ShowMessage("Capture Target area image");
                winProgress.PutForward1Step();
                saveLog("Capture Target area image.");

                // modified by Hotta 2022/11/08 for 複数コントローラ
#if MultiController

                // 上下端用
#if NO_CONTROLLER
#else
                outputGapCamTargetArea_EdgeExpand(lstTgtUnit, ExpandType.Top);
#endif
                
                Thread.Sleep(Settings.Ins.Camera.PatternWait);
#if NO_CAP
#else
                saveLog(string.Format("CameraSetting:{0},{1},{2},{3},{4},{5}",
                    m_ShootCondition.FNumber,
                    m_ShootCondition.Shutter,
                    m_ShootCondition.ISO,
                    m_ShootCondition.WB,
                    m_ShootCondition.CompressionType,
                    m_ShootCondition.ImageSize));
                CaptureImage(measPath + _fn_AreaFile + "_Top", m_ShootCondition);
#endif
                if (File.Exists(measPath + _fn_AreaFile + "_Top.arw") == true)
                {
                    if (checkFileSize(measPath + _fn_AreaFile + "_Top.arw") != true)
                    {
                        throw new Exception("Saving the " + measPath + _fn_AreaFile + "_Top.arw" + " file does not completed.");
                    }

                    try
                    {
                        loadArwFile(measPath + _fn_AreaFile + "_Top.arw", out arw);
                    }
                    // added by Hotta 2025/01/31
                    catch (CameraCasUserAbortException ex)
                    {
                        throw ex;
                    }
                    //
                    catch //(Exception ex)
                    {
                        Thread.Sleep(1000);
                        loadArwFile(measPath + _fn_AreaFile + "_Top.arw", out arw);
                    }
                    width = arw.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
                    height = arw.RawMainIFD.ImageHeight / 2;
                    mat = new Mat(new Size(width, height), MatType.CV_16UC1);
                    pMat = (ushort*)(mat.Data);

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            pMat[y * width + x] = arw.RawData[1][x, y];
                        }
                    }
                    try
                    {
                        SaveMatBinary(mat, measPath + _fn_AreaFile + "_Top");
                    }
                    catch //(Exception ex)
                    {
                        Thread.Sleep(1000);
                        SaveMatBinary(mat, measPath + _fn_AreaFile + "_Top");
                    }
                    mat.Dispose();
                    arw = null;
                }

                // 左右端用
#if NO_CONTROLLER
#else
                outputGapCamTargetArea_EdgeExpand(lstTgtUnit, ExpandType.Right);
#endif
                Thread.Sleep(Settings.Ins.Camera.PatternWait);
#if NO_CAP
#else
                saveLog(string.Format("CameraSetting:{0},{1},{2},{3},{4},{5}",
                    m_ShootCondition.FNumber,
                    m_ShootCondition.Shutter,
                    m_ShootCondition.ISO,
                    m_ShootCondition.WB,
                    m_ShootCondition.CompressionType,
                    m_ShootCondition.ImageSize));
                CaptureImage(measPath + _fn_AreaFile + "_Right", m_ShootCondition);
#endif
                if (File.Exists(measPath + _fn_AreaFile + "_Right.arw") == true)
                {
                    if (checkFileSize(measPath + _fn_AreaFile + "_Right.arw") != true)
                    {
                        throw new Exception("Saving the " + measPath + _fn_AreaFile + "_Right.arw" + " file does not completed.");
                    }

                    try
                    {
                        loadArwFile(measPath + _fn_AreaFile + "_Right.arw", out arw);
                    }
                    // added by Hotta 2025/01/31
                    catch (CameraCasUserAbortException ex)
                    {
                        throw ex;
                    }
                    //
                    catch //(Exception ex)
                    {
                        Thread.Sleep(1000);
                        loadArwFile(measPath + _fn_AreaFile + "_Right.arw", out arw);
                    }
                    width = arw.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
                    height = arw.RawMainIFD.ImageHeight / 2;
                    mat = new Mat(new Size(width, height), MatType.CV_16UC1);
                    pMat = (ushort*)(mat.Data);

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            pMat[y * width + x] = arw.RawData[1][x, y];
                        }
                    }
                    try
                    {
                        SaveMatBinary(mat, measPath + _fn_AreaFile + "_Right");
                    }
                    catch //(Exception ex)
                    {
                        Thread.Sleep(1000);
                        SaveMatBinary(mat, measPath + _fn_AreaFile + "_Right");
                    }
                    mat.Dispose();
                    arw = null;
                }


#else   // MultiController

#if NO_CONTROLLER
#else
                // modified by Hotta 2022/07/05
#if CorrectTargetEdge
                outputGapCamTargetArea_EdgeExpand(lstTgtUnit);
#else
                outputGapCamTargetArea(lstTgtUnit);
#endif
#endif
                // added by Hotta 2022/07/26
                Thread.Sleep(Settings.Ins.Camera.PatternWait);
                //
#if NO_CAP
#else
                saveLog(string.Format("CameraSetting:{0},{1},{2},{3},{4},{5}",
                    m_ShootCondition.FNumber,
                    m_ShootCondition.Shutter,
                    m_ShootCondition.ISO,
                    m_ShootCondition.WB,
                    m_ShootCondition.CompressionType,
                    m_ShootCondition.ImageSize));
                CaptureImage(measPath + _fn_AreaFile, m_ShootCondition);
#endif
                if(File.Exists(measPath + _fn_AreaFile + ".arw") == true)
                {
                    if (checkFileSize(measPath + _fn_AreaFile + ".arw") != true)
                    {
                        throw new Exception("Saving the " + measPath + _fn_AreaFile + ".arw" + " file does not completed.");
                    }

                    try
                    {
                        loadArwFile(measPath + _fn_AreaFile + ".arw", out arw);
                    }
                    catch(Exception ex)
                    {
                        Thread.Sleep(1000);
                        loadArwFile(measPath + _fn_AreaFile + ".arw", out arw);
                    }
                    width = arw.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
                    height = arw.RawMainIFD.ImageHeight / 2;
                    mat = new Mat(new Size(width, height), MatType.CV_16UC1);
                    pMat = (ushort*)(mat.Data);

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            pMat[y * width + x] = arw.RawData[1][x, y];
                        }
                    }
                    try
                    {
                        SaveMatBinary(mat, measPath + _fn_AreaFile);
                    }
                    catch(Exception ex)
                    {
                        Thread.Sleep(1000);
                        SaveMatBinary(mat, measPath + _fn_AreaFile);
                    }
                    mat.Dispose();
                    arw = null;
                }
#endif  // MultiController
            }

            // モアレ検出
            if (m_GapStatus == GapStatus.Before || m_GapStatus == GapStatus.Measure)
            {
                // deleted by Hotta 2022/08/18
/*
#if NO_CONTROLLER

#else
*/
                winProgress.ShowMessage("Capture Moire area.");
                winProgress.PutForward1Step();
                saveLog("Capture Moire area.");

                List<Area> lstArea;
                // モアレ検査エリアの表示と撮影
#if NO_CONTROLLER
#else
#if OutputOnlyGreen
                outputIntSigHatch(modDx / 4 * 1, modDy / 4 * 1, modDy / 4 * 2, modDx / 4 * 2, modDx, modDy, 0, m_MeasureLevel, 0);
#else
                outputIntSigHatch(modDx / 4 * 1, modDy / 4 * 1, modDy / 4 * 2, modDx / 4 * 2, modDx, modDy, Settings.Ins.GapCam.MeasLevel, Settings.Ins.GapCam.MeasLevel, Settings.Ins.GapCam.MeasLevel);
#endif
#endif
                // added by Hotta 2022/07/26
                Thread.Sleep(Settings.Ins.Camera.PatternWait);
                //

#if NO_CAP
#else
                saveLog(string.Format("CameraSetting:{0},{1},{2},{3},{4},{5}",
                    m_ShootCondition.FNumber,
                    m_ShootCondition.Shutter,
                    m_ShootCondition.ISO,
                    m_ShootCondition.WB,
                    m_ShootCondition.CompressionType,
                    m_ShootCondition.ImageSize));
                CaptureImage(measPath + fn_MoireArea, m_ShootCondition);
#endif
                if(File.Exists(measPath + fn_MoireArea + ".arw") == true)
                {
                    if (checkFileSize(measPath + fn_MoireArea + ".arw") != true)
                    {
                        throw new Exception("Saving the " + measPath + fn_MoireArea + ".arw" + " file does not completed.");
                    }

                    try
                    {
                        loadArwFile(measPath + fn_MoireArea + ".arw", out arw);
                    }
                    // added by Hotta 2025/01/31
                    catch (CameraCasUserAbortException ex)
                    {
                        throw ex;
                    }
                    //
                    catch //(Exception ex)
                    {
                        Thread.Sleep(1000);
                        loadArwFile(measPath + fn_MoireArea + ".arw", out arw);
                    }
                    width = arw.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
                    height = arw.RawMainIFD.ImageHeight / 2;
                    mat = new Mat(new Size(width, height), MatType.CV_16UC1);
                    pMat = (ushort*)(mat.Data);

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            pMat[y * width + x] = arw.RawData[1][x, y];
                        }
                    }
                    try
                    {
                        SaveMatBinary(mat, measPath + fn_MoireArea);
                    }
                    catch //(Exception ex)
                    {
                        Thread.Sleep(1000);
                        SaveMatBinary(mat, measPath + fn_MoireArea);
                    }
                    mat.Dispose();
                    arw = null;
                }

                // モアレ検査エリアの特定
                // modified by Hotta 2022/10/11
#if Reflection
                calcMoireCheckArea(measPath + fn_MoireArea, measPath + fn_BlackFile + "_0", out lstArea);
#else
                if(File.Exists(measPath + fn_BlackFile + ".matbin") == true)
                    calcMoireCheckArea(measPath + fn_MoireArea, measPath + fn_BlackFile, out lstArea);
                else
                    calcMoireCheckArea(measPath + fn_MoireArea, measPath + fn_BlackFile + "_0", out lstArea);
#endif
                // モアレ検査
                winProgress.ShowMessage("Capture Moire checking image.");
                winProgress.PutForward1Step();
                saveLog("Capture Moire checking image.");

#if NO_CONTROLLER
#else
                //outputGapCamTargetArea(lstTgtUnit, true);
                outputIntSigFlat(m_MeasureLevel, m_MeasureLevel, m_MeasureLevel);
#endif
                // added by Hotta 2022/07/26
                Thread.Sleep(Settings.Ins.Camera.PatternWait);
                //

#if NO_CAP
#else
                saveLog(string.Format("CameraSetting:{0},{1},{2},{3},{4},{5}",
                    m_ShootCondition.FNumber,
                    m_ShootCondition.Shutter,
                    m_ShootCondition.ISO,
                    m_ShootCondition.WB,
                    m_ShootCondition.CompressionType,
                    m_ShootCondition.ImageSize));
                CaptureImage(measPath + fn_MoireCheck, m_ShootCondition);
#endif
                if(File.Exists(measPath + fn_MoireCheck + ".arw") == true)
                {
                    if (checkFileSize(measPath + fn_MoireCheck + ".arw") != true)
                    {
                        throw new Exception("Saving the " + measPath + fn_MoireCheck + ".arw" + " file does not completed.");
                    }

                    try
                    {
                        loadArwFile(measPath + fn_MoireCheck + ".arw", out arw);
                    }
                    // added by Hotta 2025/01/31
                    catch (CameraCasUserAbortException ex)
                    {
                        throw ex;
                    }
                    //
                    catch //(Exception ex)
                    {
                        Thread.Sleep(1000);
                        loadArwFile(measPath + fn_MoireCheck + ".arw", out arw);
                    }
                    width = arw.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
                    height = arw.RawMainIFD.ImageHeight / 2;
                    mat = new Mat(new Size(width, height), MatType.CV_16UC1);
                    pMat = (ushort*)(mat.Data);

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            pMat[y * width + x] = arw.RawData[1][x, y];
                        }
                    }
                    try
                    {
                        SaveMatBinary(mat, measPath + fn_MoireCheck);
                    }
                    catch //(Exception ex)
                    {
                        Thread.Sleep(1000);
                        SaveMatBinary(mat, measPath + fn_MoireCheck);
                    }
                    mat.Dispose();
                    arw = null;
                }
                // deleted by Hotta 2022/08/18
/*
#endif
*/
                checkMoire(measPath + fn_MoireCheck, lstArea);
            }

            // TrimmingArea
            if (m_GapStatus == GapStatus.Before || m_GapStatus == GapStatus.Measure)
            {
                //winProgress.ShowMessage("Capture Trimming Area image");
                //winProgress.PutForward1Step();
                //saveLog("Capture Trimming Area image.");

                captureGapTrimmingAreaImage(measPath);
            }

            // Flat
            winProgress.ShowMessage("Capture Gap image");
            winProgress.PutForward1Step();
            saveLog("Capture Gap image.");
            captureGapFlatImageSwing(measPath, lstTgtUnit); // 5step

#if NO_CONTROLLER
#else
            // modified by Hotta 2022/07/08
#if CorrectTargetEdge
            outputGapCamTargetArea_EdgeExpand(lstTgtUnit, ExpandType.Both);
#else
            outputGapCamTargetArea(lstTgtUnit);
#endif
#endif
        }

        int TrimAreaNum = 7;
        int[] TrimAreaTopPos;
        int[] TrimAreaRightPos;


        unsafe private void captureGapTrimmingAreaImage(string measPath)
        {
            AcqARW arw;
            Mat mat;
            int width, height;
            ushort* pMat;

            // UnitとCellの測定位置は重なっているのでCellの位置のみ取得する
#if NO_CONTROLLER
#else
#if OutputOnlyGreen
            outputIntSigHatchInv(1, 1, modDy - 2, modDx - 2, modDx, modDy, 0, m_MeasureLevel, 0);
#else
            outputIntSigHatchInv(1, 1,modDy - 2, modDx - 2,modDx, modDy, Settings.Ins.GapCam.MeasLevel, Settings.Ins.GapCam.MeasLevel, Settings.Ins.GapCam.MeasLevel);
#endif
#endif
            // added by Hotta 2022/07/26
            Thread.Sleep(Settings.Ins.Camera.PatternWait);
            //

            winProgress.ShowMessage("Capture Gap Position image.");
            winProgress.PutForward1Step();
            saveLog("Capture Gap Position image.");

            string imgPath;
            for (int n=0;n<CaptureNum;n++)
            {
                imgPath = measPath + "GapPos" + n.ToString();
#if NO_CAP
#else
                CaptureImage(imgPath);
#endif
                if(File.Exists(imgPath + ".arw") == true)
                {
                    if (checkFileSize(imgPath + ".arw") != true)
                    {
                        throw new Exception("Saving the " + imgPath + ".arw" + " file does not completed.");
                    }

                    try
                    {
                        loadArwFile(imgPath + ".arw", out arw);
                    }
                    // added by Hotta 2025/01/31
                    catch (CameraCasUserAbortException ex)
                    {
                        throw ex;
                    }
                    //
                    catch //(Exception ex)
                    {
                        Thread.Sleep(1000);
                        loadArwFile(imgPath + ".arw", out arw);
                    }
                    width = arw.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
                    height = arw.RawMainIFD.ImageHeight / 2;
                    mat = new Mat(new Size(width, height), MatType.CV_16UC1);
                    pMat = (ushort*)(mat.Data);

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            pMat[y * width + x] = arw.RawData[1][x, y];
                        }
                    }
                    try
                    {
                        SaveMatBinary(mat, imgPath);
                    }
                    catch //(Exception ex)
                    {
                        Thread.Sleep(1000);
                        SaveMatBinary(mat, imgPath);
                    }
                    mat.Dispose();
                    arw = null;
                }
            }

            // Top/Bottom
            // |------|-------|-------|-------|-------|-------|------|
            //  offset size    size    size    size    size    offset
            // modified by Hotta 2022/07/13
#if Coverity
            int step = (int)((float)(modDx - 2 * Settings.Ins.GapCam.TrimmingOffset - Settings.Ins.GapCam.TrimmingSize) / (TrimAreaNum - 1) + 0.5);
#else
            int step = (int)((modDx - 2 * Settings.Ins.GapCam.TrimmingOffset - Settings.Ins.GapCam.TrimmingSize) / (TrimAreaNum - 1) + 0.5);
#endif
            TrimAreaTopPos = new int[TrimAreaNum];
            for (int n=0;n<TrimAreaNum;n++)
            {
                winProgress.ShowMessage("Capture Top/Bottom Image (" + n.ToString() + ").");
                winProgress.PutForward1Step();
                saveLog("Capture Top/Bottom Image (" + n.ToString() + ").");

                // added by Hotta 2022/07/26
                int margin = 1;
                int tileSize = Settings.Ins.GapCam.TrimmingSize;
                while ((Settings.Ins.GapCam.TrimmingOffset + step * n) + tileSize - 1 > modDx - 1 - margin)
                {
                    tileSize--;
                }
                //
#if NO_CONTROLLER
#else
#if OutputOnlyGreen
                // modified by Hotta 2022/07/26
                /*
                outputIntSigHatch(Settings.Ins.GapCam.TrimmingOffset + step * n, modDy - (Settings.Ins.GapCam.TrimmingSize / 2), Settings.Ins.GapCam.TrimmingSize, Settings.Ins.GapCam.TrimmingSize, modDx, modDy, 0, m_MeasureLevel, 0);
                */
                outputIntSigHatch(Settings.Ins.GapCam.TrimmingOffset + step * n, modDy - (Settings.Ins.GapCam.TrimmingSize / 2), Settings.Ins.GapCam.TrimmingSize, tileSize, modDx, modDy, 0, m_MeasureLevel, 0);
                //
#else
                outputIntSigHatch(Settings.Ins.GapCam.TrimmingOffset + step * n, modDy - (Settings.Ins.GapCam.TrimmingSize / 2), Settings.Ins.GapCam.TrimmingSize, Settings.Ins.GapCam.TrimmingSize, modDx, modDy, Settings.Ins.GapCam.MeasLevel, Settings.Ins.GapCam.MeasLevel, Settings.Ins.GapCam.MeasLevel);
#endif
#endif
                // added by Hotta 2022/07/26
                Thread.Sleep(Settings.Ins.Camera.PatternWait);
                //

                imgPath = measPath + fn_Top + n.ToString();
#if NO_CAP
#else
                CaptureImage(imgPath);
#endif
                if(File.Exists(imgPath + ".arw") == true)
                {
                    if (checkFileSize(imgPath + ".arw") != true)
                    {
                        throw new Exception("Saving the " + imgPath + ".arw" + " file does not completed.");
                    }

                    try
                    {
                        loadArwFile(imgPath + ".arw", out arw);
                    }
                    // added by Hotta 2025/01/31
                    catch (CameraCasUserAbortException ex)
                    {
                        throw ex;
                    }
                    //
                    catch //(Exception ex)
                    {
                        Thread.Sleep(1000);
                        loadArwFile(imgPath + ".arw", out arw);
                    }
                    width = arw.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
                    height = arw.RawMainIFD.ImageHeight / 2;
                    mat = new Mat(new Size(width, height), MatType.CV_16UC1);
                    pMat = (ushort*)(mat.Data);

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            pMat[y * width + x] = arw.RawData[1][x, y];
                        }
                    }
                    try
                    {
                        SaveMatBinary(mat, imgPath);
                    }
                    catch //(Exception ex)
                    {
                        Thread.Sleep(1000);
                        SaveMatBinary(mat, imgPath);
                    }
                    mat.Dispose();
                    arw = null;
                }

                // added by Hotta 2022/11/08 for 複数コントローラ
#if MultiController
                if(m_bottomHalfTile == true)
                {
#if NO_CONTROLLER
#else
                    outputIntSigHatch(Settings.Ins.GapCam.TrimmingOffset + step * n, 0, Settings.Ins.GapCam.TrimmingSize / 2, tileSize, modDx, modDy, 0, m_MeasureLevel, 0);
#endif
                    Thread.Sleep(Settings.Ins.Camera.PatternWait);

                    imgPath = measPath + fn_Top + n.ToString() + "_Half";
#if NO_CAP
#else
                    CaptureImage(imgPath);
#endif
                    if (File.Exists(imgPath + ".arw") == true)
                    {
                        if (checkFileSize(imgPath + ".arw") != true)
                        {
                            throw new Exception("Saving the " + imgPath + ".arw" + " file does not completed.");
                        }

                        try
                        {
                            loadArwFile(imgPath + ".arw", out arw);
                        }
                        // added by Hotta 2025/01/31
                        catch (CameraCasUserAbortException ex)
                        {
                            throw ex;
                        }
                        //
                        catch //(Exception ex)
                        {
                            Thread.Sleep(1000);
                            loadArwFile(imgPath + ".arw", out arw);
                        }
                        width = arw.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
                        height = arw.RawMainIFD.ImageHeight / 2;
                        mat = new Mat(new Size(width, height), MatType.CV_16UC1);
                        pMat = (ushort*)(mat.Data);

                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                pMat[y * width + x] = arw.RawData[1][x, y];
                            }
                        }
                        try
                        {
                            SaveMatBinary(mat, imgPath);
                        }
                        catch //(Exception ex)
                        {
                            Thread.Sleep(1000);
                            SaveMatBinary(mat, imgPath);
                        }
                        mat.Dispose();
                        arw = null;
                    }
                }
#endif

                // modified by Hotta 2022/07/25
                /*
                TrimAreaTopPos[n] = Settings.Ins.GapCam.TrimmingOffset + step * n + Settings.Ins.GapCam.TrimmingSize / 2;
                */
                TrimAreaTopPos[n] = Settings.Ins.GapCam.TrimmingOffset + step * n + tileSize / 2;
            }

            // Right/Left
            // modified by Hotta 2022/07/13
#if Coverity
            step = (int)((float)(modDy - 2 * Settings.Ins.GapCam.TrimmingOffset - Settings.Ins.GapCam.TrimmingSize) / (TrimAreaNum - 1) + 0.5);
#else
            step = (int)((modDy - 2 * Settings.Ins.GapCam.TrimmingOffset - Settings.Ins.GapCam.TrimmingSize) / (TrimAreaNum - 1) + 0.5);
#endif
            TrimAreaRightPos = new int[TrimAreaNum];
            for (int n = 0; n < TrimAreaNum; n++)
            {
                winProgress.ShowMessage("Capture Right/Left Image(" + n.ToString() + ").");
                winProgress.PutForward1Step();
                saveLog("Capture Right/Left Image(" + n.ToString() + ").");

                // added by Hotta 2022/07/26
                int margin = 1;
                int tileSize = Settings.Ins.GapCam.TrimmingSize;
                while ((Settings.Ins.GapCam.TrimmingOffset + step * n) + tileSize - 1 > modDy - 1 - margin)
                {
                    tileSize--;
                }
                //

#if NO_CONTROLLER
#else
#if OutputOnlyGreen
                // modified by Hotta 2022/07/28
                /*
                outputIntSigHatch(modDx - (Settings.Ins.GapCam.TrimmingSize / 2), Settings.Ins.GapCam.TrimmingOffset + step * n, Settings.Ins.GapCam.TrimmingSize, Settings.Ins.GapCam.TrimmingSize, modDx, modDy, 0, m_MeasureLevel, 0);
                */
                outputIntSigHatch(modDx - (Settings.Ins.GapCam.TrimmingSize / 2), Settings.Ins.GapCam.TrimmingOffset + step * n, tileSize, Settings.Ins.GapCam.TrimmingSize, modDx, modDy, 0, m_MeasureLevel, 0);
#else
                outputIntSigHatch(modDx - (Settings.Ins.GapCam.TrimmingSize / 2), Settings.Ins.GapCam.TrimmingOffset + step * n, Settings.Ins.GapCam.TrimmingSize, Settings.Ins.GapCam.TrimmingSize, modDx, modDy, Settings.Ins.GapCam.MeasLevel, Settings.Ins.GapCam.MeasLevel, Settings.Ins.GapCam.MeasLevel);
#endif
#endif
                // added by Hotta 2022/07/26
                Thread.Sleep(Settings.Ins.Camera.PatternWait);
                //

                imgPath = measPath + fn_Right + n.ToString();
#if NO_CAP
#else
                CaptureImage(imgPath);
#endif
                if(File.Exists(imgPath + ".arw") == true)
                {
                    if (checkFileSize(imgPath + ".arw") != true)
                    {
                        throw new Exception("Saving the " + imgPath + ".arw" + " file does not completed.");
                    }

                    try
                    {
                        loadArwFile(imgPath + ".arw", out arw);
                    }
                    // added by Hotta 2025/01/31
                    catch (CameraCasUserAbortException ex)
                    {
                        throw ex;
                    }
                    //
                    catch //(Exception ex)
                    {
                        Thread.Sleep(1000);
                        loadArwFile(imgPath + ".arw", out arw);
                    }
                    width = arw.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
                    height = arw.RawMainIFD.ImageHeight / 2;
                    mat = new Mat(new Size(width, height), MatType.CV_16UC1);
                    pMat = (ushort*)(mat.Data);

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            pMat[y * width + x] = arw.RawData[1][x, y];
                        }
                    }
                    try
                    {
                        SaveMatBinary(mat, imgPath);
                    }
                    catch //(Exception ex)
                    {
                        Thread.Sleep(1000);
                        SaveMatBinary(mat, imgPath);
                    }
                    mat.Dispose();
                    arw = null;
                }

                // added by Hotta 2022/11/08 for 複数コントローラ
#if MultiController
                if(m_rightHalfTile == true)
                {
#if NO_CONTROLLER
#else
                    outputIntSigHatch(0, Settings.Ins.GapCam.TrimmingOffset + step * n, tileSize, Settings.Ins.GapCam.TrimmingSize / 2, modDx, modDy, 0, m_MeasureLevel, 0);
#endif
                    Thread.Sleep(Settings.Ins.Camera.PatternWait);

                    imgPath = measPath + fn_Right + n.ToString() + "_Half";
#if NO_CAP
#else
                    CaptureImage(imgPath);
#endif
                    if (File.Exists(imgPath + ".arw") == true)
                    {
                        if (checkFileSize(imgPath + ".arw") != true)
                        {
                            throw new Exception("Saving the " + imgPath + ".arw" + " file does not completed.");
                        }

                        try
                        {
                            loadArwFile(imgPath + ".arw", out arw);
                        }
                        // added by Hotta 2025/01/31
                        catch (CameraCasUserAbortException ex)
                        {
                            throw ex;
                        }
                        //
                        catch //(Exception ex)
                        {
                            Thread.Sleep(1000);
                            loadArwFile(imgPath + ".arw", out arw);
                        }
                        width = arw.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
                        height = arw.RawMainIFD.ImageHeight / 2;
                        mat = new Mat(new Size(width, height), MatType.CV_16UC1);
                        pMat = (ushort*)(mat.Data);

                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                pMat[y * width + x] = arw.RawData[1][x, y];
                            }
                        }
                        try
                        {
                            SaveMatBinary(mat, imgPath);
                        }
                        catch //(Exception ex)
                        {
                            Thread.Sleep(1000);
                            SaveMatBinary(mat, imgPath);
                        }
                        mat.Dispose();
                        arw = null;
                    }
                }
#endif

                // modified by Hotta 2022/07/26
                /*
                TrimAreaRightPos[n] = Settings.Ins.GapCam.TrimmingOffset + step * n + Settings.Ins.GapCam.TrimmingSize / 2;
                */
                TrimAreaRightPos[n] = Settings.Ins.GapCam.TrimmingOffset + step * n + tileSize / 2;
            }
        }

        unsafe private void captureGapFlatImageSwing(string measPath, List<UnitInfo> lstTgtUnit)
        {
            AcqARW arw;
            Mat mat;
            int width, height;
            ushort* pMat;

            /*
            // White
            winProgress.ShowMessage("Capture Flat image.");
            winProgress.PutForward1Step();
#if NO_CONTROLLER
#else
#if OutputOnlyGreen
            outputGapCamTargetArea(lstTgtUnit);
#else
            outputIntSigFlat(Settings.Ins.GapCam.MeasLevel, Settings.Ins.GapCam.MeasLevel, Settings.Ins.GapCam.MeasLevel brightness._20pc, brightness._20pc, brightness._20pc);
            outputGapCamTargetArea(lstTgtUnit, true);
#endif
#endif
            // added by Hotta 2022/07/26
            Thread.Sleep(Settings.Ins.Camera.PatternWait);
            //

            string imgPath = measPath + fn_Flat;

            if (m_GapStatus == GapStatus.Neutral)
                imgPath += "Neutral";
            else if(m_GapStatus == GapStatus.Adjust)
                imgPath += "Adjust";
            else if (m_GapStatus == GapStatus.Measure)
                imgPath += "Measure";

#if NO_CAP
#else
            CaptureImage(imgPath);
#endif
            if (File.Exists(imgPath + ".arw") == true)
            {
                loadArwFile(imgPath + ".arw", out arw);
                width = arw.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
                height = arw.RawMainIFD.ImageHeight / 2;
                mat = new Mat(new Size(width, height), MatType.CV_16UC1);
                pMat = (ushort*)(mat.Data);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        pMat[y * width + x] = arw.RawData[1][x, y];
                    }
                }
                SaveMatBinary(mat, imgPath);
                mat.Dispose();
                arw = null;
            }
            */

            string imgPath = "";


            int[] gapLevel;
            double[] levelRatio;

            if (m_GapStatus == GapStatus.Before || m_GapStatus == GapStatus.Measure)
            {
                levelRatio = new double[]
                {
                    // 5%刻み
                    0.80, 0.85, 0.90, 0.95,
                    1.00,
                    1.05, 1.10, 1.15, 1.20
                };
            }
            else
            {
                levelRatio = new double[]
                {
                    // 2%刻み
                    0.96, 0.98,
                    1.00,
                    1.02, 1.04
                };
            }

            gapLevel = new int[levelRatio.Length];
            for (int i = 0; i < levelRatio.Length; i++)
            {
                double v = Math.Pow((double)m_MeasureLevel / 1023, 2.2);
                v *= levelRatio[i];
                gapLevel[i] = (int)(Math.Pow(v, 1.0 / 2.2) * 1023 + 0.5);
            }

            for (int n = 0; n < gapLevel.Length; n++)
            {
                // moved by Hotta 2022/10/19
                winProgress.ShowMessage("Capture Gap image (" + gapLevel[n].ToString() + ").");
                winProgress.PutForward1Step();
                saveLog("Capture Gap image (" + gapLevel[n].ToString() + ").");

                // added by Hotta 2022/10/19
                // 全黒（映り込み）撮影
#if Reflection
                for (int cap = 0; cap < CaptureNum; cap++)
                {
#if NO_CONTROLLER
#else
                    outputIntSigFlat(0, 0, 0);
                    Thread.Sleep(Settings.Ins.Camera.PatternWait);
#endif
                    if (m_GapStatus == GapStatus.Before)
                        imgPath = measPath + "GapBefore_" + gapLevel[n].ToString() + "_Black" + "_" + cap.ToString();
                    else if (m_GapStatus == GapStatus.Result)
                        imgPath = measPath + "GapResult_" + gapLevel[n].ToString() + "_Black" + "_" + cap.ToString();
                    else if (m_GapStatus == GapStatus.Measure)
                        imgPath = measPath + "GapMeasure_" + gapLevel[n].ToString() + "_Black" + "_" + cap.ToString();
#if NO_CAP
#else
                    CaptureImage(imgPath);
                    Thread.Sleep(Settings.Ins.Camera.CameraWait);
#endif
                    if (File.Exists(imgPath + ".arw") == true)
                    {
                        if (checkFileSize(imgPath + ".arw") != true)
                        {
                            throw new Exception("Saving the " + imgPath + ".arw" + " file does not completed.");
                        }

                        try
                        {
                            loadArwFile(imgPath + ".arw", out arw);
                        }
                        // added by Hotta 2025/01/31
                        catch (CameraCasUserAbortException ex)
                        {
                            throw ex;
                        }
                        //
                        catch //(Exception ex)
                        {
                            Thread.Sleep(1000);
                            loadArwFile(imgPath + ".arw", out arw);
                        }
                        width = arw.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
                        height = arw.RawMainIFD.ImageHeight / 2;
                        mat = new Mat(new Size(width, height), MatType.CV_16UC1);
                        pMat = (ushort*)(mat.Data);

                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                pMat[y * width + x] = arw.RawData[1][x, y];
                            }
                        }
                        try
                        {
                            SaveMatBinary(mat, imgPath);
                        }
                        catch //(Exception ex)
                        {
                            Thread.Sleep(1000);
                            SaveMatBinary(mat, imgPath);
                        }
                        mat.Dispose();
                        arw = null;
                    }
                }
#endif


#if NO_CONTROLLER
#else
#if OutputOnlyGreen
                outputIntSigFlatGap(1, 1, modDy - 2, modDx - 2, modDx, modDy, 0, m_MeasureLevel, 0, 0, gapLevel[n], 0);
#else
                outputIntSigFlatGap(1, 1, modDy - 2, modDx - 2, modDx, modDy, Settings.Ins.GapCam.MeasLevel, Settings.Ins.GapCam.MeasLevel, Settings.Ins.GapCam.MeasLevel, gapLevel[n], gapLevel[n], gapLevel[n]);
#endif
#endif
                    // added by Hotta 2022/07/26
                    Thread.Sleep(Settings.Ins.Camera.PatternWait);
                //

                // moved by Hotta 2022/10/19
                /*
                winProgress.ShowMessage("Capture Gap image (" + gapLevel[n].ToString() + ").");
                winProgress.PutForward1Step();
                saveLog("Capture Gap image (" + gapLevel[n].ToString() + ").");
                */

                for (int cap = 0;cap<CaptureNum;cap++)
                {
                    if (m_GapStatus == GapStatus.Before)
                        imgPath = measPath + "GapBefore_" + gapLevel[n].ToString() + "_" + cap.ToString();
                    else if(m_GapStatus == GapStatus.Result)
                        imgPath = measPath + "GapResult_" + gapLevel[n].ToString() + "_" + cap.ToString();
                    else if(m_GapStatus == GapStatus.Measure)
                        imgPath = measPath + "GapMeasure_" + gapLevel[n].ToString() + "_" + cap.ToString();
#if NO_CAP
#else
                    CaptureImage(imgPath);
                    Thread.Sleep(Settings.Ins.Camera.CameraWait);
#endif
                    if (File.Exists(imgPath + ".arw") == true)
                    {
                        if (checkFileSize(imgPath + ".arw") != true)
                        {
                            throw new Exception("Saving the " + imgPath + ".arw" + " file does not completed.");
                        }

                        try
                        {
                            loadArwFile(imgPath + ".arw", out arw);
                        }
                        // added by Hotta 2025/01/31
                        catch (CameraCasUserAbortException ex)
                        {
                            throw ex;
                        }
                        //
                        catch //(Exception ex)
                        {
                            Thread.Sleep(1000);
                            loadArwFile(imgPath + ".arw", out arw);
                        }
                        width = arw.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
                        height = arw.RawMainIFD.ImageHeight / 2;
                        mat = new Mat(new Size(width, height), MatType.CV_16UC1);
                        pMat = (ushort*)(mat.Data);

                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                pMat[y * width + x] = arw.RawData[1][x, y];
                            }
                        }
                        try
                        {
                            SaveMatBinary(mat, imgPath);
                        }
                        catch //(Exception ex)
                        {
                            Thread.Sleep(1000);
                            SaveMatBinary(mat, imgPath);
                        }
                        mat.Dispose();
                        arw = null;
                    }
                }
            }
        }

        class GapSwing
        {
            // modified by Hotta 2022/11/18
            /*
            public int UnitNo;
            */
            public int UnitX;
            public int UnitY;
            //
            public int CellNo;
            public double X;
            public double Y;
            public AdjustDirection Dir;
            public CorrectPosition Pos;
            public List<GapMeas> Meas;
            public double Slope;
            public double Offset;
            public double TargetSigLevel;
            public double CurrentContrast;
            public double Gain;
            // modified by Hotta 2022/11/18
            // public GapSwing(int unitNo, int cellNo, double x, double y, AdjustDirection dir, CorrectPosition pos)
            public GapSwing(int unitX, int unitY, int cellNo, double x, double y, AdjustDirection dir, CorrectPosition pos)
            {
                // modified by Hotta 2022/11/18
                /*
                UnitNo = unitNo;
                */
                UnitX = unitX;
                UnitY = unitY;
                //
                CellNo = cellNo;
                X = x;
                Y = y;
                Dir = dir;
                Pos = pos;
                Meas = new List<GapMeas>();
            }
        }

        class GapMeas
        {
            public double SignalLevel;
            public double RatioLevel;
            public double GapBright;
            public double AroundBright;
            public double GapGain;
            public GapMeas(int level, double gapBright, double aroundBright, double gapGain)
            {
                SignalLevel = level;
                RatioLevel = Math.Pow((double)level / 1023, 2.2);
                GapBright = gapBright;
                AroundBright = aroundBright;
                GapGain = gapGain;
            }
        }


        string _fn_FlatWhite = /*"White"*/
            "Gap_483_4";

        unsafe private void calcGapGain(List<UnitInfo> lstTgtUnit, string measPath)
        {
            int idx;

            if(m_GapStatus == GapStatus.Before || m_GapStatus == GapStatus.Measure)
            {
                // 対象エリアのマスク画像を作成
                string file = measPath + System.IO.Path.GetFileNameWithoutExtension(_fn_AreaFile);
                // modified by Hotta 2022/10/11 for 映り込み
#if Reflection
                // modified by Hotta 2022/10/31
#if MultiController
                makeTargetArea(file + "_Top", measPath + fn_BlackFile + "_0", ExpandType.Top);
                makeTargetArea(file + "_Right", measPath + fn_BlackFile + "_0", ExpandType.Right);
#else
                makeTargetArea(file, measPath + fn_BlackFile + "_0");
#endif

#else
                // modified by Hotta 2022/10/18
                /*
                makeTargetArea(file, measPath + fn_BlackFile);
                */
                if (File.Exists(measPath + fn_BlackFile + ".matbin") == true)
                    makeTargetArea(file, measPath + fn_BlackFile);
                else
                    makeTargetArea(file, measPath + fn_BlackFile + "_0");
                //

                makeTargetArea(file, measPath + fn_BlackFile);
#endif
                GC.Collect();

                // 補正点を格納
                storeGapCp(lstTgtUnit, measPath); // 4step

                GC.Collect();
#if NoEncript
                string[] files_GapPos = Directory.GetFiles(measPath, fn_GapPos + "*.matbin", System.IO.SearchOption.TopDirectoryOnly);
#else
                string[] files_GapPos = Directory.GetFiles(measPath, fn_GapPos + "*.matbinx", System.IO.SearchOption.TopDirectoryOnly);
#endif
                Array.Sort(files_GapPos);

                saveLog("Calc Gap position.");

                winProgress.ShowMessage("Load GapPos image.");
                saveLog("Load Raw Picture. (GapPos)");

                Mat matAvg = new Mat();
                Mat mat = new Mat();    // Coverityチェック　例外パスだとリーク
                for (int n = 0; n < CaptureNum; n++)
                {
                    string fname = Path.GetDirectoryName(files_GapPos[n]) + "\\" + Path.GetFileNameWithoutExtension(files_GapPos[n]);

                    LoadMatBinary(fname, out mat);
                    if (n == 0)
                    {
                        mat.ConvertTo(matAvg, MatType.CV_32FC1);
                    }
                    else
                    {
                        // modified by Hotta 2022/07/13
#if Coverity
                        using (Mat _mat = new Mat())
                        {
                            mat.ConvertTo(_mat, MatType.CV_32FC1);
                            matAvg += _mat; // Coverityチェック　代入に失敗するとリーク。上書きされる、元のmatAvgがリーク。　
                        }
#else
                        Mat _mat = new Mat();
                        mat.ConvertTo(_mat, MatType.CV_32FC1);
                        matAvg += _mat;
                        _mat.Dispose();
#endif
                        //
                    }
                    mat.Dispose();
                }
                matAvg /= CaptureNum;   // Coverityチェック　代入に失敗するとリーク

                ///
                // 昔のログには、fn_Flatが存在しない。
                // ファイルを時系列でソートすると、Right6.arw と GapMeasure_???_0.arw の間に、White.arwがあるので、それをFlat.arwに変更する。
                ///

                string imgPath = measPath + fn_Flat;

                Mat flat;   // Coverityチェック　例外パスだとリーク
                LoadMatBinary(imgPath, out flat);

                calcGapPos(matAvg, flat, measPath);
                winProgress.PutForward1Step();

                matAvg.Dispose();
                flat.Dispose();

                GC.Collect();
            }

            ///
            saveLog("Calc Gap contrast.");

            string[] filenames = new string[] { "" };
#if NoEncript
            if (m_GapStatus == GapStatus.Before)
                filenames = Directory.GetFiles(measPath, "GapBefore_*.matbin", System.IO.SearchOption.TopDirectoryOnly);
            else if (m_GapStatus == GapStatus.Result)
                filenames = Directory.GetFiles(measPath, "GapResult_*.matbin", System.IO.SearchOption.TopDirectoryOnly);
            else if (m_GapStatus == GapStatus.Measure)
                filenames = Directory.GetFiles(measPath, "GapMeasure_*.matbin", System.IO.SearchOption.TopDirectoryOnly);
#else
            if (m_GapStatus == GapStatus.Before)
                filenames = Directory.GetFiles(measPath, "GapBefore_*.matbinx", System.IO.SearchOption.TopDirectoryOnly);
            else if (m_GapStatus == GapStatus.Result)
                filenames = Directory.GetFiles(measPath, "GapResult_*.matbinx", System.IO.SearchOption.TopDirectoryOnly);
            else if (m_GapStatus == GapStatus.Measure)
                filenames = Directory.GetFiles(measPath, "GapMeasure_*.matbinx", System.IO.SearchOption.TopDirectoryOnly);
#endif
            Array.Sort(filenames);

            List<int> listLevel = new List<int>();

            for(int n=0;n<filenames.Length;n++)
            {
                int level = int.Parse(System.IO.Path.GetFileNameWithoutExtension(filenames[n]).Split('_')[1]);
                if (listLevel.Contains(level) != true)
                    listLevel.Add(level);
            }
            listLevel.Sort();

            List<GapSwing> listGapSwing = new List<GapSwing>();

            for (int n = 0; n < listLevel.Count; n++)
            {
                if (m_GapStatus == GapStatus.Before)
                    _fn_FlatWhite = "GapBefore_" + listLevel[n].ToString();
                else if (m_GapStatus == GapStatus.Result)
                    _fn_FlatWhite = "GapResult_" + listLevel[n].ToString();
                else if (m_GapStatus == GapStatus.Measure)
                    _fn_FlatWhite = "GapMeasure_" + listLevel[n].ToString();

                calcCpGainRaw(measPath); // 1step

                // modified by Hotta 2022/02/18
                /*
                using (StreamWriter sw = new StreamWriter(measPath + _fn_FlatWhite + ".csv"))
                {
                    sw.WriteLine("CabiX,CabiY,ModuleNo,X,Y,Dir,Pos,GapGain,Slope,Offset,Gap,Around");
                    idx = 0;
                    foreach (GapCamCorrectionValue gapCv in lstGapCamCp)
                    {
                        string str = "";
                        // Cell
                        foreach (GapCamCp cp in gapCv.lstCellCp)
                        {
                            if (cp.Pos == CorrectPosition.TopLeft || cp.Pos == CorrectPosition.RightTop)
                            {
                                str = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}",
                                    gapCv.Unit.X, gapCv.Unit.Y, (int)cp.CellNo + 1,
                                    cp.CamArea[0].CenterPos.X, cp.CamArea[0].CenterPos.Y,
                                    cp.Direction, cp.Pos,
                                    cp.GapGain, cp.Slope, cp.Offset, cp.Gap, cp.Around);

                                sw.WriteLine(str);

                                if (n == 0)
                                {
                                    listGapSwing.Add(new GapSwing((gapCv.Unit.Y - 1) * allocInfo.MaxX + (gapCv.Unit.X - 1), (int)(cp.CellNo), cp.CamArea[0].CenterPos.X, cp.CamArea[0].CenterPos.Y, cp.Direction, cp.Pos));
                                }
                                listGapSwing[idx++].Meas.Add(new GapMeas(listLevel[n], cp.Slope, cp.Offset, cp.GapGain));
                            }
                            else
                            {
                                str = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}",
                                    gapCv.Unit.X, gapCv.Unit.Y, (int)cp.CellNo + 1,
                                    cp.CamArea[TrimAreaNum - 1].CenterPos.X, cp.CamArea[TrimAreaNum - 1].CenterPos.Y,
                                    cp.Direction, cp.Pos,
                                    cp.GapGain, cp.Slope, cp.Offset, cp.Gap, cp.Around);
                                sw.WriteLine(str);

                                if (n == 0)
                                {
                                    listGapSwing.Add(new GapSwing((gapCv.Unit.Y - 1) * allocInfo.MaxX + (gapCv.Unit.X - 1), (int)(cp.CellNo), cp.CamArea[TrimAreaNum - 1].CenterPos.X, cp.CamArea[TrimAreaNum - 1].CenterPos.Y, cp.Direction, cp.Pos));
                                }
                                listGapSwing[idx++].Meas.Add(new GapMeas(listLevel[n], cp.Slope, cp.Offset, cp.GapGain));
                            }
                        }
                    }
                }
                */
                idx = 0;
                foreach (GapCamCorrectionValue gapCv in lstGapCamCp)
                {
                    // Cell
                    foreach (GapCamCp cp in gapCv.lstCellCp)
                    {
                        // modified by Hotta 2022/07/07
#if CorrectTargetEdge
                        if (cp.Pos == CorrectPosition.TopLeft || cp.Pos == CorrectPosition.RightTop || cp.Pos == CorrectPosition.BottomLeft || cp.Pos == CorrectPosition.LeftTop)
#else
                        if (cp.Pos == CorrectPosition.TopLeft || cp.Pos == CorrectPosition.RightTop)
#endif
                        {
                            if (n == 0)
                            {
                                // modified by Hotta 2022/06/27
#if RotateRect
                                // modified by Hotta 2022/11/18
                                //listGapSwing.Add(new GapSwing((gapCv.Unit.Y - 1) * allocInfo.MaxX + (gapCv.Unit.X - 1), (int)(cp.CellNo), cp.CamArea[0].Center.X, cp.CamArea[0].Center.Y, cp.Direction, cp.Pos));
                                listGapSwing.Add(new GapSwing(gapCv.Unit.X, gapCv.Unit.Y, (int)(cp.CellNo), cp.CamArea[0].Center.X, cp.CamArea[0].Center.Y, cp.Direction, cp.Pos));
#else
                                listGapSwing.Add(new GapSwing((gapCv.Unit.Y - 1) * allocInfo.MaxX + (gapCv.Unit.X - 1), (int)(cp.CellNo), cp.CamArea[0].CenterPos.X, cp.CamArea[0].CenterPos.Y, cp.Direction, cp.Pos));
#endif
                            }
                            listGapSwing[idx++].Meas.Add(new GapMeas(listLevel[n], cp.Slope, cp.Offset, cp.GapGain));
                        }
                        else
                        {
                            if (n == 0)
                            {
                                // modified by Hotta 2022/06/27
#if RotateRect
                                // modified by Hotta 2022/11/18
                                //listGapSwing.Add(new GapSwing((gapCv.Unit.Y - 1) * allocInfo.MaxX + (gapCv.Unit.X - 1), (int)(cp.CellNo), cp.CamArea[TrimAreaNum - 1].Center.X, cp.CamArea[TrimAreaNum - 1].Center.Y, cp.Direction, cp.Pos));
                                listGapSwing.Add(new GapSwing(gapCv.Unit.X, gapCv.Unit.Y, (int)(cp.CellNo), cp.CamArea[TrimAreaNum - 1].Center.X, cp.CamArea[TrimAreaNum - 1].Center.Y, cp.Direction, cp.Pos));
#else
                                listGapSwing.Add(new GapSwing((gapCv.Unit.Y - 1) * allocInfo.MaxX + (gapCv.Unit.X - 1), (int)(cp.CellNo), cp.CamArea[TrimAreaNum - 1].CenterPos.X, cp.CamArea[TrimAreaNum - 1].CenterPos.Y, cp.Direction, cp.Pos));
#endif
                            }
                            listGapSwing[idx++].Meas.Add(new GapMeas(listLevel[n], cp.Slope, cp.Offset, cp.GapGain));
                        }
                    }
                }
            }
                ///

            foreach (GapSwing gapSwing in listGapSwing)
            {
                List<Point2f> points = new List<Point2f>();

                foreach(GapMeas meas in gapSwing.Meas)
                {
                    points.Add(new Point2f((float)meas.RatioLevel, (float)meas.GapGain));
                }
                Line2D line = Cv2.FitLine(points, DistanceTypes.L1, 0, 0.01, 0.01);
                double a = line.Vy / line.Vx;
                double b = -line.Vy / line.Vx * line.X1 + line.Y1;

                double currentSigLevel = Math.Pow((double)m_MeasureLevel / 1023, 2.2);
                // y = a * x + b
                // 1.0 = a * x + b
                // x = (1.0 - b) / a
                gapSwing.TargetSigLevel = (Settings.Ins.GapCam.TargetGain - b) / a;    // コントラスト＝ターゲットになる信号レベル（リニア空間）→　ターゲット信号レベル
                gapSwing.Slope = a;
                gapSwing.Offset = b;
                gapSwing.CurrentContrast = a * currentSigLevel + b; // 信号レベル=20%のときのコントラスト
                gapSwing.Gain = currentSigLevel / gapSwing.TargetSigLevel;  // ターゲット信号レベルに対する信号レベル=20%の比

                //
                // ターゲット信号レベル付近のデータで再計算
                //
                points.Clear();
                foreach (GapMeas meas in gapSwing.Meas)
                {
                    if (gapSwing.TargetSigLevel * 0.90 < meas.RatioLevel && meas.RatioLevel < gapSwing.TargetSigLevel * 1.10)
                        points.Add(new Point2f((float)meas.RatioLevel, (float)meas.GapGain));
                }
                if (points.Count > /*4*/2)
                {
                    line = Cv2.FitLine(points, DistanceTypes.L1, 0, 0.01, 0.01);
                    a = line.Vy / line.Vx;
                    b = -line.Vy / line.Vx * line.X1 + line.Y1;

                    currentSigLevel = Math.Pow((double)m_MeasureLevel / 1023, 2.2);
                    // y = a * x + b
                    // 1.0 = a * x + b
                    // x = (1.0 - b) / a
                    gapSwing.TargetSigLevel = (Settings.Ins.GapCam.TargetGain - b) / a;    // コントラスト＝ターゲットになる信号レベル（リニア空間）→　ターゲット信号レベル
                    gapSwing.Slope = a;
                    gapSwing.Offset = b;
                    gapSwing.CurrentContrast = a * currentSigLevel + b; // 信号レベル=20%のときのコントラスト
                    gapSwing.Gain = currentSigLevel / gapSwing.TargetSigLevel;  // ターゲット信号レベルに対する信号レベル=20%の比
                }
            }

            string fn;
            // modified by Hotta 2022/02/18
            /*
            if (m_Adjusted != true)
                fn = measPath + "GapNeutral.csv";
            else
            {
                if(m_AdjustCount == 0)
                    fn = measPath + "GapAdjust.csv";
                else
                    fn = measPath + "GapAdjust_" + (m_AdjustCount+1).ToString() + ".csv";
            }
            using (StreamWriter sw = new StreamWriter(fn))
            {
                string str = "";
                str = "CabiX,CabiY,ModuleNo,X,Y,Dir,Pos,";
                for (int n = 0; n < listGapSwing[0].Meas.Count; n++)
                    str += listGapSwing[0].Meas[n].SignalLevel.ToString() + ",";
                str += "TargetSigLevel,a,b,CurrentContrast,Gain,Diff";
                sw.WriteLine(str);
                str = ",,,,,,,";
                for (int n = 0; n < listGapSwing[0].Meas.Count; n++)
                    str += listGapSwing[0].Meas[n].RatioLevel.ToString() + ",";
                sw.WriteLine(str);

                foreach (GapSwing gapSwing in listGapSwing)
                {
                    str = string.Format("{0},{1},{2},{3},{4},{5},{6},", gapSwing.UnitNo % allocInfo.MaxX + 1, gapSwing.UnitNo / allocInfo.MaxY + 1, (int)gapSwing.CellNo + 1, gapSwing.X, gapSwing.Y, gapSwing.Dir, gapSwing.Pos);
                    for (int n = 0; n < listGapSwing[0].Meas.Count; n++)
                        str += gapSwing.Meas[n].GapGain.ToString() + ",";
                    str += string.Format("{0},{1},{2},{3},{4},{5}", gapSwing.TargetSigLevel, gapSwing.Slope, gapSwing.Offset, gapSwing.CurrentContrast, gapSwing.Gain, gapSwing.Gain - Settings.Ins.GapCam.TargetGain);
                    sw.WriteLine(str);
                }
            }
            */

#if NoEncript
            if (m_GapStatus == GapStatus.Before)
                fn = measPath + "GapBefore.csv";
            else if (m_GapStatus == GapStatus.Measure)
                fn = measPath + "GapMeasure.csv";
            else
            {
                if (m_AdjustCount == 0)
                    fn = measPath + "GapAdjust.csv";
                else
                    fn = measPath + "GapAdjust_" + (m_AdjustCount + 1).ToString() + ".csv";
            }
#else
            if (m_GapStatus == GapStatus.Before)
                fn = measPath + "GapBefore.csvx";
            else if (m_GapStatus == GapStatus.Measure)
                fn = measPath + "GapMeasure.csvx";
            else
            {
                if (m_AdjustCount == 0)
                    fn = measPath + "GapAdjust.csvx";
                else
                    fn = measPath + "GapAdjust_" + (m_AdjustCount + 1).ToString() + ".csvx";
            }
#endif
            string str = "";
            using (StreamWriter sw = new StreamWriter(fn))
            {
#if NO_CAP
                str = "CabiX,CabiY,ModuleNo,X,Y,Dir,Pos,";
                for (int n = 0; n < listGapSwing[0].Meas.Count; n++)
                    str += listGapSwing[0].Meas[n].SignalLevel.ToString() + ",";
                str += "TargetSigLevel,a,b,CurrentContrast,Gain,Diff\n";
                str += ",,,,,,,";
                for (int n = 0; n < listGapSwing[0].Meas.Count; n++)
                    str += listGapSwing[0].Meas[n].RatioLevel.ToString() + ",";
#if NoEncript
                sw.WriteLine(str);
#else
                string encryptStr;
                encryptStr = Encrypt(str, CalcKey(AES_IV), CalcKey(AES_Key));
                sw.WriteLine(encryptStr);
#endif
#endif
                foreach (GapSwing gapSwing in listGapSwing)
                {
                    // modified by Hotta 2022/11/18
                    /*
                    str = string.Format("{0},{1},{2},{3},{4},{5},{6},", gapSwing.UnitNo % allocInfo.MaxX + 1, gapSwing.UnitNo / allocInfo.MaxY + 1, (int)gapSwing.CellNo + 1, gapSwing.X, gapSwing.Y, gapSwing.Dir, gapSwing.Pos);
                    */
                    str = string.Format("{0},{1},{2},{3},{4},{5},{6},", gapSwing.UnitX, gapSwing.UnitY, (int)gapSwing.CellNo + 1, gapSwing.X, gapSwing.Y, gapSwing.Dir, gapSwing.Pos);

                    for (int n = 0; n < listGapSwing[0].Meas.Count; n++)
                        str += gapSwing.Meas[n].GapGain.ToString() + ",";
                    str += string.Format("{0},{1},{2},{3},{4},{5}", gapSwing.TargetSigLevel, gapSwing.Slope, gapSwing.Offset, gapSwing.CurrentContrast, gapSwing.Gain, gapSwing.Gain - Settings.Ins.GapCam.TargetGain);
#if NoEncript
                    sw.WriteLine(str);
#else
                    encryptStr = Encrypt(str, CalcKey(AES_IV), CalcKey(AES_Key));
                    sw.WriteLine(encryptStr);
#endif
                }
            }
#if NoEncript
#else
            /**/
            // 確認用
            using(StreamReader sr = new StreamReader(fn))
            {
                str = sr.ReadToEnd();
            }
            string[] lines = str.Split('\n');
            fn = fn.Substring(0, fn.Length - 1);
            using (StreamWriter sw = new StreamWriter(fn))
            {
                for (int n = 0; n < lines.Length-1; n++)    // 最後のlines[]は、""のため
                {
                    str = Decrypt(lines[n], CalcKey(AES_IV), CalcKey(AES_Key));
                    sw.WriteLine(str);
                }
            }
            /**/
#endif

            // level=492のときの比 = cp.GapGain
            // lstGapCamCp と listGapSwing は、同じ並びのはず
            idx = 0;
            for(int n=0;n<lstGapCamCp.Count;n++)
            {
                GapCamCorrectionValue gapCv = lstGapCamCp[n];
                // Unit
                for(int i=0;i<gapCv.lstUnitCp.Count;i++)
                {
                    GapCamCp cp = gapCv.lstUnitCp[i];
                    cp.GapGain = listGapSwing[idx++].Gain;
                }
                // Cell
                for(int i=0;i< gapCv.lstCellCp.Count;i++)
                {
                    GapCamCp cp = gapCv.lstCellCp[i];
                    cp.GapGain = listGapSwing[idx++].Gain;
                }
            }
        }

        // modified by Hotta 2022/10/31 for 複数コントローラ
#if MultiController
        Mat m_MatMaskTop;
        Mat m_MatMaskRight;
#else
        Mat m_MatMask;
#endif
        // modified by Hotta 2022/10/31 for 複数コントローラ
#if MultiController
        unsafe private void makeTargetArea(string file, string black, ExpandType type)
#else
        unsafe private void makeTargetArea(string file, string black)
#endif
        {
            winProgress.ShowMessage("Calc Target area");
            winProgress.PutForward1Step();
            saveLog("Calc Target area.");

            Mat matMask = new Mat();
            Mat matBlack = new Mat();

            // modified by Hotta 2022/08/18 for Coverity
            /*
            Mat mat = new Mat();
            */
            Mat mat = null;

            try
            {
                LoadMatBinary(Path.GetDirectoryName(file) + "\\" + Path.GetFileNameWithoutExtension(file), out matMask);

                // modified by Hotta 2022/10/18
                /*
                LoadMatBinary(Path.GetDirectoryName(black) + "\\" + Path.GetFileNameWithoutExtension(black), out matBlack);
                */
                if(File.Exists(black + ".matbin") == true)
                    LoadMatBinary(Path.GetDirectoryName(black) + "\\" + Path.GetFileNameWithoutExtension(black), out matBlack);
                else
                    LoadMatBinary(Path.GetDirectoryName(black) + "\\" + Path.GetFileNameWithoutExtension(black) + "_0", out matBlack);
                //

                int width = matMask.Width;
                int height = matMask.Height;
                mat = new Mat(new Size(width, height), MatType.CV_16UC1);
                ushort* pMat = (ushort*)(mat.Data);
                ushort* pMask = (ushort*)(matMask.Data);
                ushort* pBlack = (ushort*)(matBlack.Data);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int v = (int)pMask[y * width + x] - pBlack[y * width + x];
                        if (v < 0)
                            v = 0;
                        pMat[y * width + x] = (ushort)v;
                    }
                }
                matMask.Dispose();
                matBlack.Dispose();
            }
            catch(Exception ex)
            {
                if(mat != null)
                    mat.Dispose();
                matMask.Dispose();
                matBlack.Dispose();
                throw new Exception(ex.Message);
            }


            /*
            // テスト用
            // 画像の1/4を黒にする
            for (int y = 0; y < height/2; y++)
            {
                for (int x = 0; x < width/2; x++)
                {
                    pMat[y * width + x] = 0;
                }
            }
            //
            */


            // added by Hotta 2022/10/31 for 複数コントローラ　テスト
#if MultiControllerTest
            mat.Dispose();
            if (type == ExpandType.Top)
                mat = new Mat(applicationPath + "\\Temp\\topWindow.jpg");
            else if (type == ExpandType.Right)
                mat = new Mat(applicationPath + "\\Temp\\rightWindow.jpg");
            Cv2.CvtColor(mat, mat, ColorConversionCodes.BGR2GRAY);
#endif

            // modified by Hotta 2022/07/13
#if Coverity
            CvBlobs blobs;
            using (Mat gray = new Mat())
            {
                // added by Hotta 2022/10/31 for 複数コントローラ　テスト
#if MultiControllerTest
                mat.ConvertTo(gray, MatType.CV_8UC1, 1.0 / 1.0);
#else
                mat.ConvertTo(gray, MatType.CV_8UC1, 1.0 / 16);
#endif

#if TempFile
                if(type == ExpandType.Top)
                    gray.SaveImage(applicationPath + "\\Temp\\MeasArea_Top.jpg");
                else
                    gray.SaveImage(applicationPath + "\\Temp\\MeasArea_Right.jpg");
#endif

                Mat binary = gray.Threshold(0, 0xff, ThresholdTypes.Otsu);

#if TempFile
                if(type == ExpandType.Top)
                    Cv2.ImWrite(applicationPath + @"\Temp\MeasArea_TopBin.jpg", binary);
                else
                    Cv2.ImWrite(applicationPath + @"\Temp\MeasArea_RightBin.jpg", binary);
#endif

                /*CvBlobs*/
                blobs = new CvBlobs(binary);
                blobs.FilterByArea(GapTrimmingAreaMin, binary.Height * binary.Width);

                binary.Dispose();
                //gray.Dispose();
            }
#else
            Mat gray = new Mat();

            mat.ConvertTo(gray, MatType.CV_8UC1, 1.0 / 16);
#if TempFile
            gray.SaveImage(applicationPath + "\\Temp\\MeasArea.jpg");
#endif

            Mat binary = gray.Threshold(0, 0xff, ThresholdTypes.Otsu);

#if TempFile
            Cv2.ImWrite(applicationPath + @"\Temp\MeasAreaBin.jpg", binary);
#endif

            CvBlobs blobs = new CvBlobs(binary);
            blobs.FilterByArea(GapTrimmingAreaMin, binary.Height * binary.Width);

            binary.Dispose();
            gray.Dispose();
#endif



            // 最大のBlobを検索
            CvBlob measArea = blobs.LargestBlob();

            // deleted by Hotta 2022/06/24
            /*
            // 矩形チェック
            int area = measArea.Area;
            width = measArea.Rect.Width;
            height = measArea.Rect.Height;
            double ratio = (double)area / (width * height);
            // A6400-4Kのとき、ratio = 0.968
            if(ratio < 0.8 || ratio > 1.2)
            {
                mat.Dispose();
                saveLog(string.Format("The detected target area is not rectangle ({0:F2}).", ratio));
                throw new Exception(string.Format("The detected target area is not rectangle ({0:F2}).\r\nPlease check display status(Raster).", ratio));
            }
            */

            // modified by Hotta 2022/07/13
#if Coverity
            Mat[] split;
            using (Mat contour = new Mat(mat.Size(), MatType.CV_8UC3))
            using (Mat inv = new Mat())
            {
                measArea.Contour.Render(contour);
                contour.FloodFill(new OpenCvSharp.Point(0, 0), new Scalar(255, 255, 255));

                //Mat inv = new Mat();
                Cv2.BitwiseNot(contour, inv);

                // modified by Hotta 2022/10/31 for 複数コントローラ
#if MultiController
                if(type == ExpandType.Top)
                {
                    if (m_MatMaskTop != null)
                        m_MatMaskTop.Dispose();
                    m_MatMaskTop = new Mat();
                    split = inv.Split();
                    m_MatMaskTop = split[0].Clone();
                }
                else
                {
                    if (m_MatMaskRight != null)
                        m_MatMaskRight.Dispose();
                    m_MatMaskRight = new Mat();
                    split = inv.Split();
                    m_MatMaskRight = split[0].Clone();
                }
#else
                if (m_MatMask != null)
                    m_MatMask.Dispose();
                m_MatMask = new Mat();
                /*Mat[]*/ split = inv.Split();
                m_MatMask = split[0].Clone();
#endif
            }
#if TempFile

            // modified by Hotta 2022/10/31 for 複数コントローラ
#if MultiController
            if(type == ExpandType.Top)
                Cv2.ImWrite(applicationPath + @"\Temp\TargetAreaTop.jpg", m_MatMaskTop);
            else if (type == ExpandType.Right)
                Cv2.ImWrite(applicationPath + @"\Temp\TargetAreaRight.jpg", m_MatMaskRight);
#else
            Cv2.ImWrite(applicationPath + @"\Temp\TargetArea.jpg", m_MatMask);
#endif

#endif

#else
            Mat contour = new Mat(mat.Size(), MatType.CV_8UC3);
            measArea.Contour.Render(contour);
            contour.FloodFill(new OpenCvSharp.Point(0, 0), new Scalar(255, 255, 255));

            Mat inv = new Mat();
            Cv2.BitwiseNot(contour, inv);

            if (m_MatMask != null)
                m_MatMask.Dispose();
            m_MatMask = new Mat();
            Mat[] split = inv.Split();
            m_MatMask = split[0].Clone();
#if TempFile
            Cv2.ImWrite(applicationPath + @"\Temp\TargetArea.jpg", m_MatMask);
#endif
#endif

            /*
            // Mask.jpg
            string maskFile = System.IO.Path.GetDirectoryName(file) + "\\" + fn_MaskFile;
            ////
            ////Cv2.ImWrite(maskFile, m_MatMask);
            ////
            SaveMatBinary(m_MatMask, maskFile);
            */

            // サチリチェック
            Mat masked = new Mat(); // Coverityチェック　例外パスだとリーク
            // modified by Hotta 2022/10/31 for 複数コントローラ
#if MultiController
            if(type == ExpandType.Top)
                mat.CopyTo(masked, m_MatMaskTop);
            else
                mat.CopyTo(masked, m_MatMaskRight);

#else
            mat.CopyTo(masked, m_MatMask);
#endif

            // modified by Hotta 2022/07/13
#if Coverity
            CvBlob maxBlob;
            using (Mat matByte = new Mat())
            {
                masked.ConvertTo(matByte, MatType.CV_8UC1, 1.0 / 64);
#if TempFile
                Cv2.ImWrite(applicationPath + @"\Temp\Byte.jpg", matByte);
#endif
                using (Mat matBin = matByte.Threshold(0.9 * 255, 255, ThresholdTypes.Binary))
                {
#if TempFile
                    Cv2.ImWrite(applicationPath + @"\Temp\SaturationCheck.jpg", matBin);
#endif
                    blobs = new CvBlobs(matBin);
                    blobs.FilterByArea(100, matBin.Height * matBin.Width);

                    // 最大のBlobを検索
                    /*CvBlob*/
                    maxBlob = blobs.LargestBlob();

                    matBin.Dispose();
                }
            }
            masked.Dispose();
#else
            Mat matByte = new Mat();
            masked.ConvertTo(matByte, MatType.CV_8UC1, 1.0 / 64);
#if TempFile
            Cv2.ImWrite(applicationPath + @"\Temp\Byte.jpg", matByte);
#endif
            Mat matBin = new Mat();
            matBin = matByte.Threshold(0.9 * 255, 255, ThresholdTypes.Binary);
#if TempFile
            Cv2.ImWrite(applicationPath + @"\Temp\SaturationCheck.jpg", matBin);
#endif
            blobs = new CvBlobs(matBin);
            blobs.FilterByArea(100, matBin.Height * matBin.Width);

            // 最大のBlobを検索
            CvBlob maxBlob = blobs.LargestBlob();

            matBin.Dispose();
            matByte.Dispose();
            masked.Dispose();
#endif

            split[0].Dispose();
            split[1].Dispose();
            split[2].Dispose();
            // modified by Hotta 2022/07/13
#if Coverity

#else
            inv.Dispose();
            contour.Dispose();
#endif
            mat.Dispose();

            if (maxBlob != null)
            {
                saveLog("The captured image is too bright.");
                throw new Exception("The captured image is too bright.\r\nPlease check picture setting of Wall.");
            }
        }


        // added  by Hotta 2022/05/09
#if CaptureTilt
        class BlobIndexDist
        {
            public int index;
            public double dist;
            public BlobIndexDist()
            {
                index = -1;
                dist = double.MaxValue;
            }
            public void Set(int ind, double dst)
            {
                index = ind;
                dist = dst;
            }
        }
#endif


        // modified by Hotta 2022/05/09
#if CaptureTilt
        // modified by Hotta 2022/06/27
#if RotateRect
        unsafe private void getTrimmingAreaGap(string file, CorrectPosition pos, List<UnitInfo> lstTgtUnit, out RotatedRect[,] lstArea)
#else
                unsafe private void getTrimmingAreaGap(string file, CorrectPosition pos, List<UnitInfo> lstTgtUnit, out Area[,] lstArea)
#endif
#else
                unsafe private void getTrimmingAreaGap(string file, CorrectPosition pos, List<UnitInfo> lstTgtUnit, out List<Area> lstArea)
#endif
        {
            string maskFile = System.IO.Path.GetDirectoryName(file) + "\\" + fn_MaskFile;

            // modified by Hotta 2022/05/09
#if CaptureTilt
#else
                    lstArea = new List<Area>();
#endif

            Mat matTrim = new Mat();
            LoadMatBinary(Path.GetDirectoryName(file) + "\\" + Path.GetFileNameWithoutExtension(file), out matTrim);


            // added by Hotta 2022/11/08 for 複数コントローラ
#if MultiController
            Mat matHalf = new Mat();
            if((pos == CorrectPosition.TopLeft && m_bottomHalfTile == true) || (pos == CorrectPosition.RightTop && m_rightHalfTile == true))
            {
                LoadMatBinary(Path.GetDirectoryName(file) + "\\" + Path.GetFileNameWithoutExtension(file) + "_Half", out matHalf);
            }
            ushort* pHalf = (ushort*)matHalf.Data;
#endif

            // added by Hotta 2022/10/31 for 複数コントローラ　テスト
#if MultiControllerTest
            if (pos == CorrectPosition.TopLeft)
                matTrim = new Mat(applicationPath + "\\Temp\\topTile.jpg");
            else
                matTrim = new Mat(applicationPath + "\\Temp\\rightTile.jpg");
            Cv2.CvtColor(matTrim, matTrim, ColorConversionCodes.BGR2GRAY);

            matTrim.ConvertTo(matTrim, MatType.CV_16UC1, 1.0);
#endif
            int width = matTrim.Width;
            int height = matTrim.Height;
            ushort* pTrim = (ushort*)matTrim.Data;


            // added by Hotta 2022/10/04 for 映り込み
#if Reflection
            Mat matBlack = new Mat();

            LoadMatBinary(Path.GetDirectoryName(file) + "\\" + fn_BlackFile + "_0", out matBlack);

            ushort* pBlack = (ushort*)matBlack.Data;
#endif

            // modified by Hotta 2022/07/13
#if Coverity
            Mat gray = new Mat();   // Coverityチェック　例外パスだとリーク
            Mat _mask;  // Coverityチェック　例外パスだとリーク
            using (Mat mat = new Mat(new Size(width, height), MatType.CV_16UC1))
            {
                ushort* pMat = (ushort*)(mat.Data);

                // modified by Hotta 2022/10/31 for 複数コントローラ
#if MultiController
                if (pos == CorrectPosition.TopLeft)
                    _mask = m_MatMaskTop.Clone();
                else
                    _mask = m_MatMaskRight.Clone();
#else
                        // Mask.jpg
                        /*
                        Mat _mask = new Mat(maskFile);
                        */
                        /*Mat*/
                        _mask = m_MatMask.Clone();
#endif
                int chMask = _mask.Channels();
                int stepMask = chMask * width;
                byte* _pmask = (byte*)_mask.Data;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // added by Hotta 2022/10/31 for 複数コントローラ　テスト
#if MultiControllerTest
                        if (_pmask[y * width + x] != 0)
                        {
                            pMat[y * width + x] = pTrim[y * width + x];
                        }
#else

                        // modified by Hotta 2022/10/04 for 映り込み
#if Reflection
                        /**/
                        // modified by Hotta 2022/10/31
                        //if (_pmask[y * stepMask + x * chMask + 1] != 0)
                        if (_pmask[y * width + x] != 0)
                        {
                            int value = pTrim[y * width + x] - pBlack[y * width + x];

                            // added by Hotta 2022/11/08 for 複数コントローラ
#if MultiController
                            if ((pos == CorrectPosition.TopLeft && m_bottomHalfTile == true) || (pos == CorrectPosition.RightTop && m_rightHalfTile == true))
                            {
                                value += pHalf[y * width + x] - pBlack[y * width + x];
                            }
#endif
                            if (value < 0)
                                value = 0;
                            pMat[y * width + x] = (ushort)value;
                        }

                        /**/
                        /*
                        if (_pmask[y * stepMask + x * chMask + 1] != 0)
                            pMat[y * width + x] = pTrim[y * width + x];
                        */
#else
                        if (_pmask[y * stepMask + x * chMask + 1] != 0)
                            pMat[y * width + x] = pTrim[y * width + x];
#endif
#endif
                    }
                }

                /*
                // テスト用
                for (int y = 0; y < height/2; y++)
                {
                    for (int x = 0; x < width/2; x++)
                    {
                        pMat[y * width + x] = 0;
                    }
                }
                */

                // まだ使うので、あとでDispose()
                //_mask.Dispose();

                //Mat gray = new Mat();

                // added by Hotta 2022/10/31 for 複数コントローラ　テスト
#if MultiControllerTest
                mat.ConvertTo(gray, MatType.CV_8UC1, 1.0 / 1.0);
#else
                mat.ConvertTo(gray, MatType.CV_8UC1, 1.0 / 16);
#endif
            }

            // added by Hotta 2022/08//02 for Coverity
            matTrim.Dispose();

            // added by Hotta 2022/10/04 for 映り込み
#if Reflection
            matBlack.Dispose();
#endif

            // added by Hotta 2023/02/27
            matHalf.Dispose();
            //

#if TempFile
            gray.SaveImage(applicationPath + "\\Temp\\" + System.IO.Path.GetFileNameWithoutExtension(file) + "_.jpg");
#endif

#else
                    Mat mat = new Mat(new Size(width, height), MatType.CV_16UC1);
                    ushort* pMat = (ushort*)(mat.Data);

                    // Mask.jpg
                    /*
                    Mat _mask = new Mat(maskFile);
                    */
                    Mat _mask = m_MatMask.Clone();
                    int chMask = _mask.Channels();
                    int stepMask = chMask * width;
                    byte* _pmask = (byte*)_mask.Data;

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            if (_pmask[y * stepMask + x * chMask + 1] != 0)
                                pMat[y * width + x] = pTrim[y * width + x];
                        }
                    }

                    /*
                    // テスト用
                    for (int y = 0; y < height/2; y++)
                    {
                        for (int x = 0; x < width/2; x++)
                        {
                            pMat[y * width + x] = 0;
                        }
                    }
                    */

                    // まだ使うので、あとでDispose()
                    //_mask.Dispose();

                    Mat gray = new Mat();

                    mat.ConvertTo(gray, MatType.CV_8UC1, 1.0 / 16);
#if TempFile
                    gray.SaveImage(applicationPath + "\\Temp\\" + System.IO.Path.GetFileNameWithoutExtension(file) + "_.jpg");
#endif

#endif
            using (Mat binary = gray.Threshold(0, 0xff, ThresholdTypes.Otsu))
            //using (Mat mask = new Mat(maskFile, ImreadModes.Grayscale))
            using (Mat masked = new Mat())
            {
#if TempFile
                binary.SaveImage(applicationPath + "\\Temp\\" + "\\" + System.IO.Path.GetFileNameWithoutExtension(file) + "Bin.jpg");
#endif
                //binary.CopyTo(masked, mask);
                binary.CopyTo(masked, _mask);

                Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(10, 10));
                Mat closing = new Mat();
                Cv2.MorphologyEx(masked, closing, MorphTypes.Close, kernel);

                kernel.Dispose();

#if TempFile
                Cv2.ImWrite(applicationPath + "\\Temp\\" + "\\" + System.IO.Path.GetFileNameWithoutExtension(file) + "Bin2.jpg", closing);

                Mat mat = closing.Clone();
#endif
                CvBlobs blobs = new CvBlobs(closing);

                closing.Dispose();

                // added by Hotta 2022/05/09
#if CaptureTilt

                // added by Hotta 2022/11/01 for 複数コントローラ
#if MultiController

#else
                        List<int> listX = new List<int>();
                        List<int> listY = new List<int>();
                        foreach (UnitInfo unit in lstTgtUnit)
                        {
                            if (listX.Contains(unit.X) != true)
                                listX.Add(unit.X);
                            if (listY.Contains(unit.Y) != true)
                                listY.Add(unit.Y);
                        }

                        int hNum, vNum;

                        if (allocInfo.LEDModel == ZRD_C12A || allocInfo.LEDModel == ZRD_C15A || allocInfo.LEDModel == ZRD_B12A || allocInfo.LEDModel == ZRD_B15A)
                        {
                            hNum = 4 * listX.Count;
                            vNum = 2 * listY.Count;
                        }
                        else
                        {
                            hNum = 4 * listX.Count;
                            vNum = 3 * listY.Count;
                        }
#endif

                // added by Hotta 2022/07/06
#if CorrectTargetEdge

                // added by Hotta 2022/11/01 for 複数コントローラ
#if MultiController

#else
                        bool topEdge, bottomEdge, leftEdge, rightEdge;
                        checkWallEdge(lstTgtUnit, out topEdge, out bottomEdge, out leftEdge, out rightEdge);
                        if (pos == CorrectPosition.TopLeft || pos == CorrectPosition.TopRight || pos == CorrectPosition.BottomLeft || pos == CorrectPosition.BottomRight)
                        {
                            if (topEdge == true)    // Wall上端を含む場合、-1する。タイルが表示されない。
                                vNum -= 1;
                            else                    // Wall上端を含まない場合、+1する。対象Cabinetより1ケ上のModuleが点灯し、上側のタイルが半分含まれる。
                                vNum += 1;
                            vNum += 1;  // 一番下の分。無条件に+1する。
                            if (bottomEdge != true) // Wall下端を含まない場合、+1する。対象Cabinetより1ケ下のModuleが点灯し、下側のタイルが半分含まれる。
                                vNum += 1;

                            if (leftEdge != true) // Wall左端を含まない場合、+1する。対象Cabinetより1ケ左のModuleが点灯し、左側のタイルが含まれる。
                                hNum += 1;
                            if (rightEdge != true) // Wall右端を含まない場合、+1する。対象Cabinetより1ケ右のModuleが点灯し、右側のタイルが含まれる。
                                hNum += 1;
                        }
                        else
                        {
                            if(topEdge != true)       // Wall上端を含まない場合、+1する。対象Cabinetより1ケ上のModuleが点灯し、上側のタイルが含まれる。
                                vNum += 1;
                            if (bottomEdge != true)       // Wall下端を含まない場合、+1する。対象Cabinetより1ケ下のModuleが点灯し、上側のタイルが含まれる。
                                vNum += 1;

                            if (leftEdge == true)    // Wall左端を含む場合、-1する。タイルが表示されない。
                                hNum -= 1;
                            else                    // Wall左端を含まない場合、+1する。対象Cabinetより1ケ左のModuleが点灯し、左側のタイルが半分含まれる。
                                hNum += 1;
                            hNum += 1;  // 一番右の分。無条件に+1する。
                            if (rightEdge != true) // Wall右端を含まない場合、+1する。対象Cabinetより1ケ右のModuleが点灯し、右側のタイルが半分含まれる。
                                hNum += 1;
                        }
#endif

#endif

                // 形状や面積が異常なものは、リジェクトor検出失敗とする
                double area = 0;
                foreach (KeyValuePair<int, CvBlob> item in blobs)
                {
                    int labelValue = item.Key;
                    CvBlob blob = item.Value;
                    area += blob.Area;
                }
                area /= blobs.Count;
                blobs.FilterByArea((int)(area * 0.1), int.MaxValue);

                // added by Hotta 2022/11/01 for 複数コントローラ
#if MultiController
                CvBlob[,] aryBlob;
                if (pos == CorrectPosition.TopLeft)
                {
                    aryBlob = new CvBlob[m_topTileNumX, m_topTileNumY];
                    getTilePosition(blobs, m_topTileNumX, m_topTileNumY, out aryBlob);
                }
                else
                {
                    aryBlob = new CvBlob[m_rightTileNumX, m_rightTileNumY];
                    getTilePosition(blobs, m_rightTileNumX, m_rightTileNumY, out aryBlob);
                }
#else
                CvBlob[,] aryBlob = new CvBlob[hNum, vNum];
                getTilePosition(blobs, hNum, vNum, out aryBlob);
#endif

#if TempFile
                for (int y=0;y<aryBlob.GetLength(1);y++)
                {
                    for (int x = 0; x < aryBlob.GetLength(0); x++)
                    {
                        mat.Circle((int)(aryBlob[x, y].Centroid.X + 0.5), (int)(aryBlob[x, y].Centroid.Y + 0.5), 3, new Scalar(0));
                    }
                }
                Cv2.ImWrite(applicationPath + "\\Temp\\" + System.IO.Path.GetFileNameWithoutExtension(file) + "_Select.jpg", mat);
                mat.Dispose();
#endif



                // modified by Hotta 2022/11/02
#if MultiController
                if (pos == CorrectPosition.TopLeft || pos == CorrectPosition.TopRight || pos == CorrectPosition.BottomLeft || pos == CorrectPosition.BottomRight)
                {
                    lstArea = new RotatedRect[m_topTileNumX, m_topTileNumY];

                    for (int y = 0; y < m_topTileNumY; y++)
                    {
                        for (int x = 0; x < m_topTileNumX; x++)
                        {
                            int minX = aryBlob[x, y].MinX;
                            int maxX = aryBlob[x, y].MaxX + 1;
                            int minY = aryBlob[x, y].MinY;
                            int maxY = aryBlob[x, y].MaxY + 1;

                            using (Mat matTemp0 = new Mat(masked.Size(), MatType.CV_8UC3))
                            {
                                CvContourChainCode contourChainCode = aryBlob[x, y].Contour;
                                contourChainCode.Render(matTemp0);
                                //matTemp0.SaveImage("c:\\temp\\temp0.jpg");

                                using (Mat matTemp = matTemp0[minY, maxY, minX, maxX])
                                using (Mat matMono = new Mat(matTemp.Size(), MatType.CV_8UC1))
                                using (Mat matBin = new Mat(matTemp.Size(), MatType.CV_8UC1))
                                {
                                    //matTemp.SaveImage("c:\\temp\\temp.jpg");
                                    Cv2.CvtColor(matTemp, matMono, ColorConversionCodes.BGR2GRAY);
                                    OpenCvSharp.Point[][] contours;
                                    OpenCvSharp.HierarchyIndex[] hindex;
                                    Cv2.FindContours(matMono, out contours, out hindex, RetrievalModes.External, ContourApproximationModes.ApproxNone);
                                    lstArea[x, y] = Cv2.MinAreaRect(contours[0]);
                                    lstArea[x, y].Center.X += minX;
                                    lstArea[x, y].Center.Y += minY;
                                }
                            }
                        }
                    }
                }
                else
                {
                    lstArea = new RotatedRect[m_rightTileNumX, m_rightTileNumY];

                    // modified by Hotta 2022/07/06
                    for (int y = 0; y < m_rightTileNumY; y++)
                    {
                        for (int x = 0; x < m_rightTileNumX; x++)
                        {
                            int minX = aryBlob[x, y].MinX;
                            int maxX = aryBlob[x, y].MaxX + 1;
                            int minY = aryBlob[x, y].MinY;
                            int maxY = aryBlob[x, y].MaxY + 1;

                            using (Mat matTemp0 = new Mat(masked.Size(), MatType.CV_8UC3))
                            {
                                CvContourChainCode contourChainCode = aryBlob[x, y].Contour;
                                contourChainCode.Render(matTemp0);

                                using (Mat matTemp = matTemp0[minY, maxY, minX, maxX])
                                using (Mat matMono = new Mat(matTemp.Size(), MatType.CV_8UC1))
                                using (Mat matBin = new Mat(matTemp.Size(), MatType.CV_8UC1))
                                {
                                    Cv2.CvtColor(matTemp, matMono, ColorConversionCodes.BGR2GRAY);
                                    OpenCvSharp.Point[][] contours;
                                    OpenCvSharp.HierarchyIndex[] hindex;
                                    Cv2.FindContours(matMono, out contours, out hindex, RetrievalModes.External, ContourApproximationModes.ApproxNone);
                                    lstArea[x, y] = Cv2.MinAreaRect(contours[0]);
                                    lstArea[x, y].Center.X += minX;
                                    lstArea[x, y].Center.Y += minY;
                                }
                            }
                        }
                    }
                }

#else   // MultiController
                if (pos == CorrectPosition.TopLeft || pos == CorrectPosition.TopRight || pos == CorrectPosition.BottomLeft || pos == CorrectPosition.BottomRight)
                {
                    // modified by Hotta 2022/07/06
#if CorrectTargetEdge
                    int start_x, end_x, start_y, end_y;
                    if (topEdge == true)
                        start_y = 0;
                    else
                        start_y = 1;

                    end_y = vNum - 1;
                    if (bottomEdge == true) // Wallの端かどうかにかかわらず、-1する。
                        end_y -= 1;
                    else
                        end_y -= 1;

                    if (leftEdge == true)
                        start_x = 0;
                    else
                        start_x = 1;

                    end_x = hNum - 1;
                    if (rightEdge != true)
                        end_x -= 1;
#endif
                    // modified by Hotta 2022/06/27
#if RotateRect
                    // modified by Hotta 2022/07/06
#if CorrectTargetEdge
                    lstArea = new RotatedRect[end_x - start_x + 1, end_y - start_y + 1];
#else
                    lstArea = new RotatedRect[hNum, vNum - 1];
#endif
#else
                    lstArea = new Area[hNum, vNum - 1];
#endif

                    // modified by Hotta 2022/07/06
#if CorrectTargetEdge
                    for (int y = start_y; y <= end_y; y++)
                    {
                        for (int x = start_x; x <= end_x; x++)
                        {
#else
                    for (int y = 0; y < vNum - 1; y++)   // 下端は使わない
                    {
                        for (int x = 0; x < hNum; x++)
                        {
#endif
                            // modified by Hotta 2022/06/27
#if RotateRect
                            int minX = aryBlob[x, y].MinX;
                            int maxX = aryBlob[x, y].MaxX + 1;
                            int minY = aryBlob[x, y].MinY;
                            int maxY = aryBlob[x, y].MaxY + 1;

                            using (Mat matTemp0 = new Mat(masked.Size(), MatType.CV_8UC3))
                            {
                                CvContourChainCode contourChainCode = aryBlob[x, y].Contour;
                                contourChainCode.Render(matTemp0);
                                //matTemp0.SaveImage("c:\\temp\\temp0.jpg");

                                using (Mat matTemp = matTemp0[minY, maxY, minX, maxX])
                                using (Mat matMono = new Mat(matTemp.Size(), MatType.CV_8UC1))
                                using (Mat matBin = new Mat(matTemp.Size(), MatType.CV_8UC1))
                                {
                                    //matTemp.SaveImage("c:\\temp\\temp.jpg");
                                    Cv2.CvtColor(matTemp, matMono, ColorConversionCodes.BGR2GRAY);
                                    OpenCvSharp.Point[][] contours;
                                    OpenCvSharp.HierarchyIndex[] hindex;
                                    Cv2.FindContours(matMono, out contours, out hindex, RetrievalModes.External, ContourApproximationModes.ApproxNone);
                                    lstArea[x - start_x, y - start_y] = Cv2.MinAreaRect(contours[0]);
                                    lstArea[x - start_x, y - start_y].Center.X += minX;
                                    lstArea[x - start_x, y - start_y].Center.Y += minY;
                                }
                            }
#else
                            lstArea[x, y] = new Area(aryBlob[x, y].Rect.Left, aryBlob[x, y].Rect.Top, aryBlob[x, y].Rect.Height, aryBlob[x, y].Rect.Width);
#endif
                        }
                    }
                }
                else
                {
                    // modified by Hotta 2022/07/06
#if CorrectTargetEdge
                    int start_x, end_x, start_y, end_y;
                    if (leftEdge == true)
                        start_x = 0;
                    else
                        start_x = 1;

                    end_x = hNum - 1;
                    if (rightEdge == true) // Wallの端かどうかにかかわらず、-1する。    // Coverityチェック　rightEdgeの値に関係なくend_x-=1している　このままにしておく
                        end_x -= 1;
                    else
                        end_x -= 1;

                    if (topEdge == true)
                        start_y = 0;
                    else
                        start_y = 1;

                    end_y = vNum - 1;
                    if (bottomEdge != true)
                        end_y -= 1;
#endif

                    // modified by Hotta 2022/06/27
#if RotateRect
                    // modified by Hotta 2022/07/06
#if CorrectTargetEdge
                    lstArea = new RotatedRect[end_x - start_x + 1, end_y - start_y + 1];
#else
                    lstArea = new RotatedRect[hNum - 1, vNum];
#endif
#else
                    lstArea = new Area[hNum - 1, vNum];
#endif

                    // modified by Hotta 2022/07/06
#if CorrectTargetEdge
                    for (int y = start_y; y <= end_y; y++)
                    {
                        for (int x = start_x; x <= end_x; x++)
                        {
#else
                            for (int y = 0; y < vNum; y++)
                            {
                                for (int x = 0; x < hNum - 1; x++)   // 右端は使わない
                                {
#endif
                            // modified by Hotta 2022/06/27
#if RotateRect
                            //using (Mat matTemp = new Mat(masked.Size(), MatType.CV_8UC3))
                            //using (Mat matMono = new Mat(masked.Size(), MatType.CV_8UC1))
                            //using (Mat matBin = new Mat(masked.Size(), MatType.CV_8UC1))
                            //{
                            //    CvContourChainCode contourChainCode = aryBlob[x, y].Contour;
                            //    contourChainCode.Render(matTemp);
                            //    Cv2.CvtColor(matTemp, matMono, ColorConversionCodes.BGR2GRAY);
                            //    Cv2.Threshold(matMono, matBin, 0, 255, ThresholdTypes.Otsu);
                            //    OpenCvSharp.Point[][] contours;
                            //    OpenCvSharp.HierarchyIndex[] hindex;
                            //    Cv2.FindContours(matBin, out contours, out hindex, RetrievalModes.External, ContourApproximationModes.ApproxNone);
                            //    lstArea[x, y] = Cv2.MinAreaRect(contours[0]);
                            //}
                            int minX = aryBlob[x, y].MinX;
                            int maxX = aryBlob[x, y].MaxX + 1;
                            int minY = aryBlob[x, y].MinY;
                            int maxY = aryBlob[x, y].MaxY + 1;

                            using (Mat matTemp0 = new Mat(masked.Size(), MatType.CV_8UC3))
                            {
                                CvContourChainCode contourChainCode = aryBlob[x, y].Contour;
                                contourChainCode.Render(matTemp0);

                                using (Mat matTemp = matTemp0[minY, maxY, minX, maxX])
                                using (Mat matMono = new Mat(matTemp.Size(), MatType.CV_8UC1))
                                using (Mat matBin = new Mat(matTemp.Size(), MatType.CV_8UC1))
                                {
                                    Cv2.CvtColor(matTemp, matMono, ColorConversionCodes.BGR2GRAY);
                                    OpenCvSharp.Point[][] contours;
                                    OpenCvSharp.HierarchyIndex[] hindex;
                                    Cv2.FindContours(matMono, out contours, out hindex, RetrievalModes.External, ContourApproximationModes.ApproxNone);
                                    lstArea[x - start_x, y - start_y] = Cv2.MinAreaRect(contours[0]);
                                    lstArea[x - start_x, y - start_y].Center.X += minX;
                                    lstArea[x - start_x, y - start_y].Center.Y += minY;
                                }
                            }
#else
                            lstArea[x, y] = new Area(aryBlob[x, y].Rect.Left, aryBlob[x, y].Rect.Top, aryBlob[x, y].Rect.Height, aryBlob[x, y].Rect.Width);
#endif
                        }
                    }
                }

#endif  // MultiController

#else
                // 面積の大きい順に、必要な数を取得する
                int num = 0;

                List<int> listX = new List<int>();
                List<int> listY = new List<int>();
                foreach (UnitInfo unit in lstTgtUnit)
                {
                    if (listX.Contains(unit.X) != true)
                        listX.Add(unit.X);
                    if (listY.Contains(unit.Y) != true)
                        listY.Add(unit.Y);
                }
                if (pos == CorrectPosition.TopLeft || pos == CorrectPosition.TopRight || pos == CorrectPosition.BottomLeft || pos == CorrectPosition.BottomRight)
                {
                    num = (m_ModuleCountX * m_ModuleCountY) * (listX.Count * listY.Count) - (m_ModuleCountX * listX.Count);
                }
                else
                {
                    num = (m_ModuleCountX * m_ModuleCountY) * (listX.Count * listY.Count) - (m_ModuleCountY * listY.Count);
                }



                if (blobs.Count < num)
                {
                    saveLog(string.Format("The number of deteced trimming area is wrong. detected:{0} < specified:{1}", blobs.Count, num));
                    throw new Exception(string.Format("The number of deteced trimming area is wrong. detected:{0} < specified:{1}\r\nPlease check display status(Small Tile).", blobs.Count, num));
                }

                for(int n=0;n<num;n++)
                {
                    CvBlob blob = blobs.LargestBlob();
                    Area trim = new Area(blob.Rect.Left, blob.Rect.Top, blob.Rect.Height, blob.Rect.Width);
                    lstArea.Add(trim);
                    bool st = blobs.Remove(blob.Label);
                }
#endif
            }

            _mask.Dispose();
            gray.Dispose();
            // modified by Hotta 2022/07/13
#if Coverity
#else
                    mat.Dispose();
#endif
        }



        void getTilePosition(CvBlobs blobs, int hNum, int vNum, out CvBlob[,] aryBlob)
        {
            // added by Hotta 2022/09/30
#if ShowPattern_CamPos
            if (blobs == null)
                throw new Exception("No tiles found.");
#endif
            //

            aryBlob = new CvBlob[hNum, vNum];

            List<CvBlob> listBlobs = new List<CvBlob>();

            if (blobs.Count < hNum * vNum)
            {
                throw new Exception(string.Format("The number of found tiles is not correct. Found:[{0}], Spec:[{1}]", blobs.Count, hNum * vNum));
            }
            for (int n = 0; n < hNum * vNum; n++)
            {
                CvBlob blob = blobs.LargestBlob();
                listBlobs.Add(blob);
                blobs.Remove(blob.Label);
            }

            //foreach (CvBlob blob in blobs.Values)
            //{
            //    listBlobs.Add(blob);
            //}

            if (listBlobs.Count != hNum * vNum)
            {
                throw new Exception(string.Format("The number of found tiles is not correct. Found:[{0}], Spec:[{1}]", listBlobs.Count, hNum * vNum));
            }

            for (int x = 0; x < hNum; x++)
            {
                int index = 0;
                CvBlob[] aryClmBlob = new CvBlob[vNum];

                // 最も左にあるブロブ
                int min_index = -1;
                double min_x = double.MaxValue;
                for (int n = 0; n < listBlobs.Count; n++)
                {
                    if (listBlobs[n].Centroid.X < min_x)
                    {
                        min_x = listBlobs[n].Centroid.X;
                        min_index = n;
                    }
                }
                aryClmBlob[index++] = listBlobs[min_index];  // 登録
                Point2d u_ref = listBlobs[min_index].Centroid;  // 上検索用の起点ブロブ
                Point2d d_ref = listBlobs[min_index].Centroid;  // 下検索用の起点ブロブ
                listBlobs.RemoveAt(min_index);  // 検索対象から削除


                // added by Hotta 2022/11/11
                // 各列あたり1ケの場合の対応
                if(vNum == 1)
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
                    for (int n = 0; n < listBlobs.Count; n++)
                    {
                        double r = Math.Sqrt(Math.Pow(listBlobs[n].Centroid.X - u_ref.X, 2) + Math.Pow(listBlobs[n].Centroid.Y - u_ref.Y, 2));
                        double deltaY = listBlobs[n].Centroid.Y - u_ref.Y;  // 上にある場合、負の数になる
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
                    for (int n = 0; n < listBlobs.Count; n++)
                    {
                        double r = Math.Sqrt(Math.Pow(listBlobs[n].Centroid.X - d_ref.X, 2) + Math.Pow(listBlobs[n].Centroid.Y - d_ref.Y, 2));
                        double deltaY = listBlobs[n].Centroid.Y - d_ref.Y;  // 下にある場合、正の数になる
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
                        throw new Exception(string.Format("Failed to sort tiles. (Column={0})", x));
                    }
                    else if (dBlob.index == -1 || uBlob.dist <= dBlob.dist)
                    {
                        // 上だけ見つけた　または　上の方の距離が短い場合
                        aryClmBlob[index] = listBlobs[uBlob.index].Clone();  // 登録
                        u_ref = listBlobs[uBlob.index].Centroid;    // 上検索用起点ブロブ
                        listBlobs.RemoveAt(uBlob.index);    // 検索対象から削除
                    }
                    else if (uBlob.index == -1 || uBlob.dist > dBlob.dist)
                    {
                        // 下だけ見つけた　または　下の方の距離が短い場合
                        aryClmBlob[index] = listBlobs[dBlob.index].Clone();  // 登録
                        d_ref = listBlobs[dBlob.index].Centroid;    // 下検索用起点ブロブ
                        listBlobs.RemoveAt(dBlob.index);    // 検索対象から削除
                    }

                    index++;
                    // 一列分のブロブが登録されたら、
                    if (index == vNum)
                    {
                        // V位置でソート
                        var sortBlob = aryClmBlob.OrderBy(blob => blob.Centroid.Y);
                        int count = 0;
                        foreach (CvBlob blob in sortBlob)
                        {
                            aryBlob[x, count++] = blob;
                        }
                        break;
                    }
                }
            }
        }


        void getTilePos(CvBlobs blobs, int[] tileNum, out CvBlob[][] aryBlob)
        {
            aryBlob = new CvBlob[tileNum.Length][];
            for (int i = 0; i < tileNum.Length; i++)
                aryBlob[i] = new CvBlob[tileNum[i]];

            if (blobs == null)
                throw new Exception("No tiles found.");

            List<CvBlob> listBlobs = new List<CvBlob>();

            int num = 0;
            for (int i = 0; i < tileNum.Length; i++)
            {
                num += tileNum[i];
            }

            if (blobs.Count < num)
            {
                throw new Exception(string.Format("The number of found tiles is not correct. Found:[{0}], Spec:[{1}]", blobs.Count, num));
            }
            // ノイズを削除
            // あおりや歪みで小さくなったタイル　＞　ノイズ　のはず
            for (int n = 0; n < num; n++)
            {
                CvBlob blob = blobs.LargestBlob();
                listBlobs.Add(blob);
                blobs.Remove(blob.Label);
            }

            if (listBlobs.Count != num)
            {
                throw new Exception(string.Format("The number of found tiles is not correct. Found:[{0}], Spec:[{1}]", listBlobs.Count,num));
            }

            for (int x = 0; x < tileNum.Length; x++)
            {
                int index = 0;
                CvBlob[] aryClmBlob = new CvBlob[tileNum[x]];

                // 最も左にあるブロブ
                int min_index = -1;
                double min_x = double.MaxValue;
                for (int n = 0; n < listBlobs.Count; n++)
                {
                    if (listBlobs[n].Centroid.X < min_x)
                    {
                        min_x = listBlobs[n].Centroid.X;
                        min_index = n;
                    }
                }
                aryClmBlob[index++] = listBlobs[min_index];  // 登録
                Point2d u_ref = listBlobs[min_index].Centroid;  // 上検索用の起点ブロブ
                Point2d d_ref = listBlobs[min_index].Centroid;  // 下検索用の起点ブロブ
                listBlobs.RemoveAt(min_index);  // 検索対象から削除

                BlobIndexDist uBlob;
                BlobIndexDist dBlob;
                while (true)
                {
                    // 上検索　起点ブロブに対して、-45°～-135°以内（上方向）にあり、最も距離の短いブロブを見つける
                    uBlob = new BlobIndexDist();
                    double min = double.MaxValue;
                    for (int n = 0; n < listBlobs.Count; n++)
                    {
                        double r = Math.Sqrt(Math.Pow(listBlobs[n].Centroid.X - u_ref.X, 2) + Math.Pow(listBlobs[n].Centroid.Y - u_ref.Y, 2));
                        double deltaY = listBlobs[n].Centroid.Y - u_ref.Y;  // 上にある場合、負の数になる
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
                    for (int n = 0; n < listBlobs.Count; n++)
                    {
                        double r = Math.Sqrt(Math.Pow(listBlobs[n].Centroid.X - d_ref.X, 2) + Math.Pow(listBlobs[n].Centroid.Y - d_ref.Y, 2));
                        double deltaY = listBlobs[n].Centroid.Y - d_ref.Y;  // 下にある場合、正の数になる
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
                        aryClmBlob[index] = listBlobs[uBlob.index].Clone();  // 登録
                        u_ref = listBlobs[uBlob.index].Centroid;    // 上検索用起点ブロブ
                        listBlobs.RemoveAt(uBlob.index);    // 検索対象から削除
                    }
                    else if (uBlob.index == -1 || uBlob.dist > dBlob.dist)
                    {
                        // 下だけ見つけた　または　下の方の距離が短い場合
                        aryClmBlob[index] = listBlobs[dBlob.index].Clone();  // 登録
                        d_ref = listBlobs[dBlob.index].Centroid;    // 下検索用起点ブロブ
                        listBlobs.RemoveAt(dBlob.index);    // 検索対象から削除
                    }

                    index++;
                    // 一列分のブロブが登録されたら、
                    if (index == tileNum[x])
                    {
                        // V位置でソート
                        var sortBlob = aryClmBlob.OrderBy(blob => blob.Centroid.Y);
                        int count = 0;
                        foreach (CvBlob blob in sortBlob)
                        {
                            aryBlob[x][count++] = blob;
                        }
                        break;
                    }
                }
            }
        }



        /// <summary>
        /// TrimmingAreaの面積を計算する。撮影距離が任意なので面積も前後する。
        /// TrimmingAreaは最低でも12個（1Unit分）はあるはずということが前提になっている。
        /// </summary>
        /// <param name="blobs"></param>
        /// <returns></returns>
        private double calcArea(CvBlobs blobs)
        {
            double area = 0;
            List<double> lstArea = new List<double>();

            // 極端に大きいもの、小さいものを除外する
            blobs.FilterByArea(GapTrimmingAreaMin, TrimmingAreaMax);

            // 見つかった数が1個以下の場合はNG
            if (blobs.Count < 1)
            { return area; }

            foreach (KeyValuePair<int, CvBlob> pair in blobs)
            { lstArea.Add(pair.Value.Area); }

            // 面積順に並び替え
            lstArea.Sort();

            // 大きさで真ん中辺のTrimmingAreaの平均値を求める
            // →真ん中辺はダメみたいなので真ん中より大きい側で平均を求める（Unitが1列しかない場合などは大きいのとその半分のが半々になる）
            int count = 0;
            double sum = 0;

            int start = (int)((double)lstArea.Count * 0.55);
            int end = (int)((double)lstArea.Count * 0.8);

            for (int i = start; i < end; i++)
            {
                sum += lstArea[i];
                count++;
            }

            // modified by Hotta 2022/07/13
#if Coverity
            if(count > 0)
                area = sum / count;
            else
                area = 0;
#else
            area = sum / count;
#endif
            return area;
        }


        private void storeGapCp(List<UnitInfo> lstTgtUnit, string measPath)
        {
            // modified by Hotta 2022/05/09
#if CaptureTilt
            // modified by Hotta 2022/06/27
#if RotateRect
            RotatedRect[][,] lstAreaTop = new RotatedRect[TrimAreaNum][,];
            RotatedRect[][,] lstAreaRight = new RotatedRect[TrimAreaNum][,];
#else
            Area[][,] lstAreaTop = new Area[TrimAreaNum][,];
            Area[][,] lstAreaRight = new Area[TrimAreaNum][,];
#endif
#else
            List<Area>[] lstAreaTop = new List<Area>[TrimAreaNum];
            List<Area>[] lstAreaRight = new List<Area>[TrimAreaNum];
#endif
            saveLog("Calc Trimming area.");

            // TrimmingAreaを抽出
            // Top
            for (int n = 0; n < TrimAreaNum; n++)
            {
                winProgress.ShowMessage("Load Top image (" + n.ToString() + ").");
                winProgress.PutForward1Step();
                saveLog("Load Top image (" + n.ToString() + ").");

                string file = measPath + fn_Top + n.ToString();

                // modified by Hotta 2022/05/09
#if CaptureTilt
                getTrimmingAreaGap(file, CorrectPosition.TopLeft, lstTgtUnit, out lstAreaTop[n]);
#else
                // modified by Hotta 2022/02/14
                // getTrimmingAreaGap(file, CorrectPosition.TopLeft, out lstAreaTop[n]);
                getTrimmingAreaGap(file, CorrectPosition.TopLeft, lstTgtUnit, out lstAreaTop[n]);
#endif
            }

            // Right
            for (int n = 0; n < TrimAreaNum; n++)
            {
                winProgress.ShowMessage("Load Right image(" + n.ToString() + ").");
                winProgress.PutForward1Step();
                saveLog("Load Right image(" + n.ToString() + ").");

                string file = measPath + fn_Right + n.ToString();

                // modified by Hotta 2022/05/09
#if CaptureTilt
                getTrimmingAreaGap(file, CorrectPosition.RightTop, lstTgtUnit, out lstAreaRight[n]);
#else
                // modified by Hotta 2022/02/14
                //getTrimmingAreaGap(file, CorrectPosition.RightTop, out lstAreaRight[n]);
                getTrimmingAreaGap(file, CorrectPosition.RightTop, lstTgtUnit, out lstAreaRight[n]);
#endif
            }

            // 調整点の格納 ※矩形前提
            int StartUnitX = int.MaxValue, StartUnitY = int.MaxValue; // 対象Unitの左上のユニット位置（1ベース）
            int EndUnitX = 0, EndUnitY = 0;

            // Unitの位置を調査
            foreach (UnitInfo unit in lstTgtUnit)
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


            // added by Hotta 2022/07/06
#if CorrectTargetEdge
            bool topEdge, bottomEdge, leftEdge, rightEdge;
            checkWallEdge(lstTgtUnit, out topEdge, out bottomEdge, out leftEdge, out rightEdge);
#endif

            // added by Hotta 2022/11/04 for 複数コントローラ
#if MultiController
            bool topModuleCorrectionEnable = true;
            bool bottomModuleCorrectionEnable = true;
            bool leftModuleCorrectionEnable = true;
            bool rightModuleCorrectionEnable = true;

            // 選択されたCabinetの上端の上の行に、少なくとも1ケはCabinetがない　→　一番上のModuleの上端の目地補正を行わない
            if (topEdge == true)
                topModuleCorrectionEnable = false;

            // 選択されたCabinetの下端の下の行に、少なくとも1ケはCabinetがない　→　一番下のModuleの下端の目地補正を行わない
            if (bottomEdge == true)
                bottomModuleCorrectionEnable = false;

            // 選択されたCabinetの左端の左の列に、少なくとも1ケはCabinetがない　→　一番左のModuleの左端の目地補正を行わない
            if (leftEdge == true)
                leftModuleCorrectionEnable = false;

            // 選択されたCabinetの右端の右の列に、少なくとも1ケはCabinetがない　→　一番下のModuleの右端の目地補正を行わない
            if (rightEdge == true)
                rightModuleCorrectionEnable = false;
#endif

            // added by Hotta 2022/05/09 for あおり撮影
#if CaptureTilt
            for (int y = StartUnitY - 1; y <= EndUnitY - 1; y++)
            {
                for (int x = StartUnitX - 1; x <= EndUnitX - 1; x++)
                {
                    int cabinet_x_index = x - (StartUnitX - 1);
                    int cabinet_y_index = y - (StartUnitY - 1);

                    GapCamCorrectionValue gapCv = new GapCamCorrectionValue(m_ModuleXNum * m_ModuleYNum);

                    // 対象のUnitを探す
                    gapCv.Unit = searchUnit(lstTgtUnit, x + 1, y + 1);  // 1 : 基数

                    // Top-Left & Top-Right
                    for (int lr = 0; lr < 2; lr++)
                    {
                        // lr=0 : Top-Left
                        // lr=1 : Top-Right

                        // modified by Hotta 2022/06/17 for Cancun
#region Cancun
                        //// Cabinet間を含まない最上端のCabinet
                        //if (y == StartUnitY - 1)
                        //{
                        //    // Module:4,5,6,7
                        //    for (int cell = 0; cell < m_ModuleXNum; cell++)
                        //    {
                        //        GapCamCp cp = new GapCamCp();
                        //        cp.CamArea = new Area[TrimAreaNum];
                        //        for (int n = 0; n < TrimAreaNum; n++)
                        //        {
                        //            cp.CamArea[n] = lstAreaTop[n][cabinet_x_index * m_ModuleXNum + cell, 0];
                        //        }
                        //        cp.Direction = AdjustDirection.Horizontally;
                        //        cp.Pos = corPos;
                        //        cp.CellNo = (CellNum)Enum.ToObject(typeof(CellNum), cell + m_ModuleXNum);
                        //        gapCv.lstCellCp.Add(cp);
                        //    }
                        //}
                        //// Cabinet間を含む最上端ではないCabinet
                        //else
                        //{
                        //    // Module:0,1,2,3
                        //    for (int cell = 0; cell < m_ModuleXNum; cell++)
                        //    {
                        //        GapCamCp cp = new GapCamCp();
                        //        cp.CamArea = new Area[TrimAreaNum];
                        //        for (int n = 0; n < TrimAreaNum; n++)
                        //        {
                        //            cp.CamArea[n] = lstAreaTop[n][cabinet_x_index * m_ModuleXNum + cell, cabinet_y_index * 2 - 1];
                        //        }
                        //        cp.Direction = AdjustDirection.Horizontally;
                        //        cp.Pos = corPos;
                        //        cp.CellNo = (CellNum)Enum.ToObject(typeof(CellNum), cell + 0);
                        //        gapCv.lstCellCp.Add(cp);
                        //    }

                        //    // Module:4,5,6,7
                        //    for (int cell = 0; cell < m_ModuleXNum; cell++)
                        //    {
                        //        GapCamCp cp = new GapCamCp();
                        //        cp.CamArea = new Area[TrimAreaNum];
                        //        for (int n = 0; n < TrimAreaNum; n++)
                        //        {
                        //            cp.CamArea[n] = lstAreaTop[n][cabinet_x_index * m_ModuleXNum + cell, cabinet_y_index * 2  + 1 - 1];
                        //        }
                        //        cp.Direction = AdjustDirection.Horizontally;
                        //        cp.Pos = corPos;
                        //        cp.CellNo = (CellNum)Enum.ToObject(typeof(CellNum), cell + m_ModuleXNum);
                        //        gapCv.lstCellCp.Add(cp);
                        //    }
                        //}
#endregion

                        // modified by Hotta 2022/11/04 for 複数コントローラ
#if MultiController
                        GapCamCp cp = new GapCamCp();

                        for (int _y = 0; _y < m_ModuleYNum; _y++)
                        {
                            // 最上端のCabinetで、最上端のModule（特別）
                            if (y == StartUnitY - 1 && _y == 0)
                            {
                                // 上端を補正する
                                if (topModuleCorrectionEnable == true)
                                {
                                    for (int _x = 0; _x < m_ModuleXNum; _x++)
                                    {
                                        cp = new GapCamCp();
                                        cp.CamArea = new RotatedRect[TrimAreaNum];
                                        for (int n = 0; n < TrimAreaNum; n++)
                                            cp.CamArea[n] = lstAreaTop[n][cabinet_x_index * m_ModuleXNum + _x, 0];
                                        cp.Direction = AdjustDirection.Horizontally;
                                        if(lr == 0)
                                            cp.Pos = CorrectPosition.TopLeft;
                                        else
                                            cp.Pos = CorrectPosition.TopRight;
                                        cp.CellNo = (CellNum)Enum.ToObject(typeof(CellNum), 0 * m_ModuleXNum + _x);
                                        gapCv.lstCellCp.Add(cp);
                                    }
                                }
                                //// 補正しないなら、下のModuleへ
                                //else
                                //{
                                //    continue;
                                //}
                            }

                            // 最上端のCabinetで、最上端のModuleでない
                            // Moduleの上端を補正する（通常）
                            if (!(y == StartUnitY - 1 && _y == 0))
                            {
                                for (int _x = 0; _x < m_ModuleXNum; _x++)
                                {
                                    cp = new GapCamCp();
                                    cp.CamArea = new RotatedRect[TrimAreaNum];
                                    for (int n = 0; n < TrimAreaNum; n++)
                                    {
                                        if (topModuleCorrectionEnable == true)
                                            cp.CamArea[n] = lstAreaTop[n][cabinet_x_index * m_ModuleXNum + _x, cabinet_y_index * m_ModuleYNum + _y];
                                        else
                                            cp.CamArea[n] = lstAreaTop[n][cabinet_x_index * m_ModuleXNum + _x, cabinet_y_index * m_ModuleYNum + _y - 1];
                                    }
                                    cp.Direction = AdjustDirection.Horizontally;
                                    if (lr == 0)
                                        cp.Pos = CorrectPosition.TopLeft;
                                    else
                                        cp.Pos = CorrectPosition.TopRight;
                                    cp.CellNo = (CellNum)Enum.ToObject(typeof(CellNum), _y * m_ModuleXNum + _x);
                                    gapCv.lstCellCp.Add(cp);
                                }
                            }

                            // 最下端のCabinetで、最下端のModule（特別）
                            if (y == EndUnitY - 1 && _y == m_ModuleYNum - 1)
                            {
                                // 下端を補正する
                                if (bottomModuleCorrectionEnable == true)
                                {
                                    for (int _x = 0; _x < m_ModuleXNum; _x++)
                                    {
                                        cp = new GapCamCp();
                                        cp.CamArea = new RotatedRect[TrimAreaNum];
                                        for (int n = 0; n < TrimAreaNum; n++)
                                        {
                                            if (topModuleCorrectionEnable == true)
                                                cp.CamArea[n] = lstAreaTop[n][cabinet_x_index * m_ModuleXNum + _x, cabinet_y_index * m_ModuleYNum + (m_ModuleYNum - 1) + 1];
                                            else
                                                cp.CamArea[n] = lstAreaTop[n][cabinet_x_index * m_ModuleXNum + _x, cabinet_y_index * m_ModuleYNum + (m_ModuleYNum - 1)];
                                        }
                                        cp.Direction = AdjustDirection.Horizontally;
                                        if (lr == 0)
                                            cp.Pos = CorrectPosition.BottomLeft;
                                        else
                                            cp.Pos = CorrectPosition.BottomRight;
                                        cp.CellNo = (CellNum)Enum.ToObject(typeof(CellNum), _y * m_ModuleXNum + _x);
                                        gapCv.lstCellCp.Add(cp);
                                    }
                                }
                            }
                        }
#else   // // MultiController
                        for(int _y=0;_y<m_ModuleYNum;_y++)
                        {
                            // modified by Hotta 2022/07/06
#if CorrectTargetEdge
                            // 対象の最上端かつWallの最上端
                            if (y == StartUnitY - 1 && _y == 0 && topEdge == true)
                                continue;
#else
                            // Cabinet間を含まない最上端のCabinet
                            if (y == StartUnitY - 1 && _y == 0)
                                continue;
#endif
                            for (int _x = 0; _x < m_ModuleXNum; _x++)
                            {
                                GapCamCp cp = new GapCamCp();
                                // modified by Hotta 2022/06/27
#if RotateRect
                                cp.CamArea = new RotatedRect[TrimAreaNum];
#else
                                cp.CamArea = new Area[TrimAreaNum];
#endif
                                for (int n = 0; n < TrimAreaNum; n++)
                                {
                                    // modified by Hotta 2022/07/07
#if CorrectTargetEdge
                                    if(topEdge == true)
                                        cp.CamArea[n] = lstAreaTop[n][cabinet_x_index * m_ModuleXNum + _x, cabinet_y_index * m_ModuleYNum + _y - 1]; // -1は、1回スキップしているため
                                    else
                                        cp.CamArea[n] = lstAreaTop[n][cabinet_x_index * m_ModuleXNum + _x, cabinet_y_index * m_ModuleYNum + _y];
#else
                                    cp.CamArea[n] = lstAreaTop[n][cabinet_x_index * m_ModuleXNum + _x, cabinet_y_index * m_ModuleYNum + _y - 1]; // -1は、_y == 0 をスキップしているため
#endif
                                }
                                cp.Direction = AdjustDirection.Horizontally;
                                cp.Pos = corPos;
                                cp.CellNo = (CellNum)Enum.ToObject(typeof(CellNum),  _y * m_ModuleXNum + _x);
                                gapCv.lstCellCp.Add(cp);
                            }
                            // modified by Hotta 2022/07/06
#if CorrectTargetEdge
                            // 対象の最下端だが、Wallの最下端でない
                            if (y == EndUnitY - 1 && _y == m_ModuleYNum - 1 && bottomEdge != true)
                            {
                                for (int _x = 0; _x < m_ModuleXNum; _x++)
                                {
                                    GapCamCp cp = new GapCamCp();
                                    cp.CamArea = new RotatedRect[TrimAreaNum];
                                    for (int n = 0; n < TrimAreaNum; n++)
                                    {
                                        if (topEdge == true)
                                            cp.CamArea[n] = lstAreaTop[n][cabinet_x_index * m_ModuleXNum + _x, cabinet_y_index * m_ModuleYNum + (m_ModuleYNum - 1) + 1 - 1]; // -1は、1回スキップしているため
                                        else
                                            cp.CamArea[n] = lstAreaTop[n][cabinet_x_index * m_ModuleXNum + _x, cabinet_y_index * m_ModuleYNum + (m_ModuleYNum - 1) + 1];
                                    }
                                    cp.Direction = AdjustDirection.Horizontally;
                                    if (lr == 0)
                                        //corPos = CorrectPosition.TopLeft;
                                        corPos = CorrectPosition.BottomLeft;
                                    else
                                        //corPos = CorrectPosition.TopRight;
                                        corPos = CorrectPosition.BottomRight;
                                    cp.Pos = corPos;
                                    cp.CellNo = (CellNum)Enum.ToObject(typeof(CellNum), (m_ModuleYNum - 1) * m_ModuleXNum + _x);
                                    gapCv.lstCellCp.Add(cp);
                                }
                            }
#endif
                        }
#endif  // // MultiController
                    }


                    // Right-Top & Right-Bottom
                    for (int tb = 0; tb < 2; tb++)
                    {
                        // tb=0 : Right-Top
                        // tb=1 : Right-Bottom

                        // modified by Hotta 2022/06/17 for Cancun
#region Cancun
                        //// Module:0,1,2,3
                        //for (int cell = 0; cell < m_ModuleXNum; cell++)
                        //{
                        //    if ((x - (StartUnitX - 1)) == LenX - 1 && cell == m_ModuleXNum - 1)
                        //        continue;
                        //    GapCamCp cp = new GapCamCp();
                        //    cp.CamArea = new Area[TrimAreaNum];
                        //    for (int n = 0; n < TrimAreaNum; n++)
                        //    {
                        //        cp.CamArea[n] = lstAreaRight[n][cabinet_x_index * m_ModuleXNum + cell, cabinet_y_index * 2 + 0];
                        //    }
                        //    cp.Direction = AdjustDirection.Vertically;
                        //    cp.Pos = corPos;
                        //    cp.CellNo = (CellNum)Enum.ToObject(typeof(CellNum), cell + m_ModuleXNum * 0);
                        //    gapCv.lstCellCp.Add(cp);
                        //}

                        //// Module:4,5,6,7
                        //for (int cell = 0; cell < m_ModuleXNum; cell++)
                        //{
                        //    if ((x - (StartUnitX - 1)) == LenX - 1 && cell == m_ModuleXNum - 1)
                        //        continue;
                        //    GapCamCp cp = new GapCamCp();
                        //    cp.CamArea = new Area[TrimAreaNum];
                        //    for (int n = 0; n < TrimAreaNum; n++)
                        //    {
                        //        cp.CamArea[n] = lstAreaRight[n][cabinet_x_index * m_ModuleXNum + cell, cabinet_y_index * 2 + 1];
                        //    }
                        //    cp.Direction = AdjustDirection.Vertically;
                        //    cp.Pos = corPos;
                        //    cp.CellNo = (CellNum)Enum.ToObject(typeof(CellNum), cell + m_ModuleXNum * 1);
                        //    gapCv.lstCellCp.Add(cp);
                        //}
#endregion

                        // modified by Hotta 2022/11/04 for 複数コントローラ
#if MultiController
                        GapCamCp cp = new GapCamCp();

                        for (int _y = 0; _y < m_ModuleYNum; _y++)
                        {

                            for (int _x = 0; _x < m_ModuleXNum; _x++)
                            {
                                // 最左端のCabinetで、最左端のModule（特別）
                                if (x == StartUnitX - 1 && _x == 0)
                                {
                                    // 左端を補正する
                                    if (leftModuleCorrectionEnable == true)
                                    {
                                        cp = new GapCamCp();
                                        cp.CamArea = new RotatedRect[TrimAreaNum];
                                        for (int n = 0; n < TrimAreaNum; n++)
                                            cp.CamArea[n] = lstAreaRight[n][0, cabinet_y_index * m_ModuleYNum + _y];
                                        cp.Direction = AdjustDirection.Vertically;
                                        if (tb == 0)
                                            cp.Pos = CorrectPosition.LeftTop;
                                        else
                                            cp.Pos = CorrectPosition.LeftBottom;
                                        cp.CellNo = (CellNum)Enum.ToObject(typeof(CellNum), _y * m_ModuleXNum + 0);
                                        gapCv.lstCellCp.Add(cp);
                                    }
                                }
                                // 通常の右端を対象にするため、このifに対するelse は不要（else をつけると、右端が対象外になってしまう）

                                // 最右端のCabinetで、最右端のModule（特別）
                                if (x == EndUnitX - 1 && _x == m_ModuleXNum - 1)
                                {
                                    // 右端を補正する
                                    if (rightModuleCorrectionEnable == true)
                                    {
                                        cp = new GapCamCp();
                                        cp.CamArea = new RotatedRect[TrimAreaNum];
                                        for (int n = 0; n < TrimAreaNum; n++)
                                        {
                                            if (leftModuleCorrectionEnable == true)
                                                cp.CamArea[n] = lstAreaRight[n][cabinet_x_index * m_ModuleXNum + (m_ModuleXNum - 1) + 1, cabinet_y_index * m_ModuleYNum + _y];
                                            else
                                                cp.CamArea[n] = lstAreaRight[n][cabinet_x_index * m_ModuleXNum + (m_ModuleXNum - 1), cabinet_y_index * m_ModuleYNum + _y];
                                        }
                                        cp.Direction = AdjustDirection.Vertically;
                                        if (tb == 0)
                                            cp.Pos = CorrectPosition.RightTop;
                                        else
                                            cp.Pos = CorrectPosition.RightBottom;
                                        cp.CellNo = (CellNum)Enum.ToObject(typeof(CellNum), _y * m_ModuleXNum + _x);
                                        gapCv.lstCellCp.Add(cp);
                                    }
                                }
                                // 最右端のCabinetかつ最右端のModule以外（通常）
                                else
                                {
                                    cp = new GapCamCp();
                                    cp.CamArea = new RotatedRect[TrimAreaNum];
                                    for (int n = 0; n < TrimAreaNum; n++)
                                    {
                                        if (leftModuleCorrectionEnable == true)
                                            cp.CamArea[n] = lstAreaRight[n][cabinet_x_index * m_ModuleXNum + _x + 1, cabinet_y_index * m_ModuleYNum + _y];
                                        else
                                            cp.CamArea[n] = lstAreaRight[n][cabinet_x_index * m_ModuleXNum + _x, cabinet_y_index * m_ModuleYNum + _y];
                                    }
                                    cp.Direction = AdjustDirection.Vertically;
                                    if (tb == 0)
                                        cp.Pos = CorrectPosition.RightTop;
                                    else
                                        cp.Pos = CorrectPosition.RightBottom;
                                    cp.CellNo = (CellNum)Enum.ToObject(typeof(CellNum), _y * m_ModuleXNum + _x);
                                    gapCv.lstCellCp.Add(cp);
                                }
                            }
                        }

#else   // MultiController
                        for(int _y=0;_y<m_ModuleYNum;_y++)
                        {
                            for (int _x = 0; _x < m_ModuleXNum; _x++)
                            {
                                if ((x - (StartUnitX - 1)) == LenX - 1 && _x == m_ModuleXNum - 1 && rightEdge == true)
                                    continue;

                                GapCamCp cp = new GapCamCp();
                                // modified by Hotta 2022/06/27
#if RotateRect
                                cp.CamArea = new RotatedRect[TrimAreaNum];
#else
                                cp.CamArea = new Area[TrimAreaNum];
#endif
                                // added by Hotta 2022/07/07
#if CorrectTargetEdge
                                if(leftEdge == true)
                                {
                                    for (int n = 0; n < TrimAreaNum; n++)
                                    {
                                        cp.CamArea[n] = lstAreaRight[n][cabinet_x_index * m_ModuleXNum + _x, cabinet_y_index * m_ModuleYNum + _y];
                                    }
                                    cp.Direction = AdjustDirection.Vertically;
                                    cp.Pos = corPos;
                                    cp.CellNo = (CellNum)Enum.ToObject(typeof(CellNum), _y * m_ModuleXNum + _x);
                                    gapCv.lstCellCp.Add(cp);
                                }
                                else
                                {
                                    if(x == StartUnitX - 1 && _x == 0)
                                    {
                                        for (int n = 0; n < TrimAreaNum; n++)
                                        {
                                            cp.CamArea[n] = lstAreaRight[n][cabinet_x_index * m_ModuleXNum + _x, cabinet_y_index * m_ModuleYNum + _y];
                                        }
                                        cp.Direction = AdjustDirection.Vertically;
                                        if (corPos == CorrectPosition.RightTop)
                                            cp.Pos = CorrectPosition.LeftTop;
                                        else
                                            cp.Pos = CorrectPosition.LeftBottom;
                                        cp.CellNo = (CellNum)Enum.ToObject(typeof(CellNum), _y * m_ModuleXNum + _x);
                                        gapCv.lstCellCp.Add(cp);
                                    }

                                    cp = new GapCamCp();
                                    cp.CamArea = new RotatedRect[TrimAreaNum];

                                    int x_index_offset = 1;
                                    for (int n = 0; n < TrimAreaNum; n++)
                                    {
                                        cp.CamArea[n] = lstAreaRight[n][cabinet_x_index * m_ModuleXNum + _x + x_index_offset, cabinet_y_index * m_ModuleYNum + _y];
                                    }
                                    cp.Direction = AdjustDirection.Vertically;
                                    cp.Pos = corPos;
                                    cp.CellNo = (CellNum)Enum.ToObject(typeof(CellNum), _y * m_ModuleXNum + _x);
                                    gapCv.lstCellCp.Add(cp);
                                }
#else
                                for (int n = 0; n < TrimAreaNum; n++)
                                {
                                    cp.CamArea[n] = lstAreaRight[n][cabinet_x_index * m_ModuleXNum + _x, cabinet_y_index * m_ModuleYNum + _y];
                                }
                                cp.Direction = AdjustDirection.Vertically;
                                cp.Pos = corPos;
                                cp.CellNo = (CellNum)Enum.ToObject(typeof(CellNum), _y * m_ModuleXNum + _x);
                                gapCv.lstCellCp.Add(cp);
#endif
                            }
                        }

#endif  // MultiController
                    }
                    lstGapCamCp.Add(gapCv);
                }
            }
#else
            // Y座標順に並び替え
            for(int n=0;n<TrimAreaNum;n++)
            {
                var cy = new Comparison<Area>(CompareY);
                lstAreaTop[n].Sort(cy);
                lstAreaRight[n].Sort(cy);
            }

            var cx = new Comparison<Area>(CompareX);

            for (int y = StartUnitY - 1; y <= EndUnitY - 1; y++)
            {
                for (int x = StartUnitX - 1; x <= EndUnitX - 1; x++)
                {
                    GapCamCorrectionValue gapCv = new GapCamCorrectionValue();

                    // 対象のUnitを探す
                    gapCv.Unit = searchUnit(lstTgtUnit, x + 1, y + 1);  // 1 : 基数

                    // Cell補正点
                    List<Area> lstAreaLineCell;

                    // Top-Left & Top-Right
                    for (int lr = 0; lr < 2; lr++)
                    {
                        // lr=0 : Top-Left
                        // lr=1 : Top-Right
                        CorrectPosition corPos;
                        if (lr == 0)
                            corPos = CorrectPosition.TopLeft;
                        else
                            corPos = CorrectPosition.TopRight;

                        // Cabinet間を含まない最上端のCabinet
                        // modified by Hotta 2021/12/20
                        // if (y == 0)
                        if(y == StartUnitY - 1)
                        {
                            // 代入
                            for (int cell = 0; cell < m_ModuleCountX; cell++)
                            {
                                GapCamCp cp = new GapCamCp();
                                cp.CamArea = new Area[TrimAreaNum];
                                for(int n=0;n<TrimAreaNum;n++)
                                {
                                    List<Area> targetArea = lstAreaTop[n]; ;
                                    // 自身のCabinet内Module間のTrimmingArea（行方向）を抜き出す
                                    lstAreaLineCell = targetArea.GetRange(0, m_ModuleCountX * LenX);
                                    // X座標順に並び替え
                                    lstAreaLineCell.Sort(cx);
                                    cp.CamArea[n] = new Area();
                                    // modified by Hotta 2021/12/21
                                    // cp.CamArea[n] = lstAreaLineCell[x * m_ModuleCountX + cell];
                                    cp.CamArea[n] = lstAreaLineCell[(x - (StartUnitX - 1)) * m_ModuleCountX + cell];
                                }
                                cp.Direction = AdjustDirection.Horizontally;
                                cp.Pos = corPos;
                                cp.CellNo = (CellNum)Enum.ToObject(typeof(CellNum), cell + m_ModuleCountX);
                                gapCv.lstCellCp.Add(cp);
                            }
                        }
                        // Cabinet間を含む最上端ではないCabinet
                        else
                        {
                            // 代入
                            for (int cell = 0; cell < m_ModuleCountX; cell++)
                            {
                                GapCamCp cp = new GapCamCp();
                                cp.CamArea = new Area[TrimAreaNum];
                                for (int n = 0; n < TrimAreaNum; n++)
                                {
                                    List<Area> targetArea = lstAreaTop[n]; ;
                                    // 自身と1段上のModule間のTrimmingArea（行方向）を抜き出す
                                    // modified by Hotta 2021/12/20
                                    // lstAreaLineCell = targetArea.GetRange(m_ModuleCountX * LenX + (m_ModuleCountX * LenX * 2) * (y - 1), m_ModuleCountX * LenX);
                                    lstAreaLineCell = targetArea.GetRange(m_ModuleCountX * LenX + (m_ModuleCountX * LenX * 2) * ((y - (StartUnitY - 1)) - 1), m_ModuleCountX * LenX);
                                    // X座標順に並び替え
                                    lstAreaLineCell.Sort(cx);
                                    // modified by hotta 2021/12/21
                                    // cp.CamArea[n] = lstAreaLineCell[x * m_ModuleCountX + cell];
                                    cp.CamArea[n] = lstAreaLineCell[(x - (StartUnitX - 1)) * m_ModuleCountX + cell];
                                }
                                cp.Direction = AdjustDirection.Horizontally;
                                cp.Pos = corPos;
                                cp.CellNo = (CellNum)Enum.ToObject(typeof(CellNum), cell + 0);
                                gapCv.lstCellCp.Add(cp);
                            }

                            // 代入
                            for (int cell = 0; cell < m_ModuleCountX; cell++)
                            {
                                GapCamCp cp = new GapCamCp();
                                cp.CamArea = new Area[TrimAreaNum];
                                for (int n = 0; n < TrimAreaNum; n++)
                                {
                                    List<Area> targetArea = lstAreaTop[n]; ;
                                    // 自身のCabinet内Module間のTrimmingArea（行方向）を抜き出す
                                    // modified by Hotta 2021/12/20
                                    // lstAreaLineCell = targetArea.GetRange(m_ModuleCountX * LenX + (m_ModuleCountX * LenX * 2) * (y - 1) + m_ModuleCountX * LenX, m_ModuleCountX * LenX);
                                    lstAreaLineCell = targetArea.GetRange(m_ModuleCountX * LenX + (m_ModuleCountX * LenX * 2) * ((y - (StartUnitY - 1)) - 1) + m_ModuleCountX * LenX, m_ModuleCountX * LenX);
                                    // X座標順に並び替え
                                    lstAreaLineCell.Sort(cx);
                                    cp.CamArea[n] = lstAreaLineCell[(x - (StartUnitX - 1)) * m_ModuleCountX + cell];
                                }
                                cp.Direction = AdjustDirection.Horizontally;
                                cp.Pos = corPos;
                                cp.CellNo = (CellNum)Enum.ToObject(typeof(CellNum), cell + m_ModuleCountX);
                                gapCv.lstCellCp.Add(cp);
                            }
                        }
                    }


                    // Right-Top & Right-Bottom
                    for (int tb = 0; tb < 2; tb++)
                    {
                        // tb=0 : Right-Top
                        // tb=1 : Right-Bottom
                        List<Area> targetArea;
                        CorrectPosition corPos;
                        if (tb == 0)
                            corPos = CorrectPosition.RightTop;
                        else
                            corPos = CorrectPosition.RightBottom;

                        // 代入
                        for (int cell = 0; cell < m_ModuleCountX; cell++)
                        {
                            // modified by Hotta 2021/12/21
                            // if (x == LenX - 1 && cell == m_ModuleCountX - 1)
                            if ((x - (StartUnitX - 1)) == LenX - 1 && cell == m_ModuleCountX - 1)
                                continue;
                            GapCamCp cp = new GapCamCp();
                            cp.CamArea = new Area[TrimAreaNum];
                            for(int n=0;n<TrimAreaNum;n++)
                            {
                                targetArea = lstAreaRight[n];
                                // 自身のCabinet内1行目のTrimmingArea（行方向）を抜き出す
                                // modified by Hotta 2021/12/20
                                //lstAreaLineCell = targetArea.GetRange((((m_ModuleCountX * LenX) - 1) * m_ModuleCountY) * y, m_ModuleCountX * LenX - 1);
                                lstAreaLineCell = targetArea.GetRange((((m_ModuleCountX * LenX) - 1) * m_ModuleCountY) * (y - (StartUnitY - 1)), m_ModuleCountX * LenX - 1);
                                // X座標順に並び替え
                                lstAreaLineCell.Sort(cx);
                                // modified by Hotta 2021/12/21
                                // cp.CamArea[n] = lstAreaLineCell[m_ModuleCountX * x + cell];
                                cp.CamArea[n] = lstAreaLineCell[m_ModuleCountX * (x - (StartUnitX - 1)) + cell];
                            }
                            cp.Direction = AdjustDirection.Vertically;
                            cp.Pos = corPos;
                            cp.CellNo = (CellNum)Enum.ToObject(typeof(CellNum), cell + m_ModuleCountX * 0);
                            gapCv.lstCellCp.Add(cp);
                        }

                        // 代入
                        for (int cell = 0; cell < m_ModuleCountX; cell++)
                        {
                            // modified by Hotta 2021/12/21
                            //if (x == LenX - 1 && cell == m_ModuleCountX - 1)
                            if ((x - (StartUnitX - 1)) == LenX - 1 && cell == m_ModuleCountX - 1)
                                continue;
                            GapCamCp cp = new GapCamCp();
                            cp.CamArea = new Area[TrimAreaNum];
                            for(int n=0;n<TrimAreaNum;n++)
                            {
                                targetArea = lstAreaRight[n];
                                // 自身のCabinet内2行目のTrimmingArea（行方向）を抜き出す
                                // modified by Hotta 2021/12/20
                                //lstAreaLineCell = targetArea.GetRange((((m_ModuleCountX * LenX) - 1) * m_ModuleCountY) * y + (m_ModuleCountX * LenX - 1), m_ModuleCountX * LenX - 1);
                                lstAreaLineCell = targetArea.GetRange((((m_ModuleCountX * LenX) - 1) * m_ModuleCountY) * (y - (StartUnitY - 1)) + (m_ModuleCountX * LenX - 1), m_ModuleCountX * LenX - 1);
                                // X座標順に並び替え
                                lstAreaLineCell.Sort(cx);
                                // modified by Hotta 2021/12/21
                                // cp.CamArea[n] = lstAreaLineCell[m_ModuleCountX * x + cell];
                                cp.CamArea[n] = lstAreaLineCell[m_ModuleCountX * (x - (StartUnitX - 1)) + cell];
                            }
                            cp.Direction = AdjustDirection.Vertically;
                            cp.Pos = corPos;
                            cp.CellNo = (CellNum)Enum.ToObject(typeof(CellNum), cell + m_ModuleCountX * 1);
                            gapCv.lstCellCp.Add(cp);
                        }
                    }

                    lstGapCamCp.Add(gapCv);
                }
            }
#endif


#if MultiControllerTest
#if TempFile
            Mat mat = new Mat(applicationPath + "\\Temp\\topPattern.jpg");
            foreach(GapCamCorrectionValue gapCv in lstGapCamCp)
            {
                foreach(GapCamCp cp in gapCv.lstCellCp)
                {
                    if(cp.Direction == AdjustDirection.Horizontally)
                    {
                        string str = string.Format("{0},{1},{2}", gapCv.Unit.X, gapCv.Unit.Y, (int)cp.CellNo+1);
                        if (cp.Pos == CorrectPosition.TopLeft || cp.Pos == CorrectPosition.TopRight)
                            str += "T";
                        else
                            str += "B";
                        mat.PutText(str, new OpenCvSharp.Point(cp.CamArea[0].Center.X, cp.CamArea[0].Center.Y), HersheyFonts.HersheySimplex, 0.3, new Scalar(255, 255, 255));
                    }
                }
            }
            mat.SaveImage(applicationPath + "\\Temp\\topPattern.jpg");
            mat.Dispose();

            mat = new Mat(applicationPath + "\\Temp\\rightPattern.jpg");
            foreach (GapCamCorrectionValue gapCv in lstGapCamCp)
            {
                foreach (GapCamCp cp in gapCv.lstCellCp)
                {
                    if (cp.Direction == AdjustDirection.Vertically)
                    {
                        string str = string.Format("{0},{1},{2}", gapCv.Unit.X, gapCv.Unit.Y, (int)cp.CellNo+1);
                        if (cp.Pos == CorrectPosition.LeftTop || cp.Pos == CorrectPosition.LeftBottom)
                            str += "L";
                        else
                            str += "R";
                        mat.PutText(str, new OpenCvSharp.Point(cp.CamArea[0].Center.X, cp.CamArea[0].Center.Y), HersheyFonts.HersheySimplex, 0.3, new Scalar(255, 255, 255));
                    }
                }
            }
            mat.SaveImage(applicationPath + "\\Temp\\rightPattern.jpg");
            mat.Dispose();

#endif
#endif

        }

        unsafe private void calcCpGainRaw(string measPath)
        {
            Mat matAvg = new Mat();
            Mat mat = new Mat();    // Coverityチェック　例外パスだとリーク
            float* pMat = null;

            winProgress.ShowMessage("Load Gap image. " + _fn_FlatWhite);
            saveLog("Load Gap image. " + _fn_FlatWhite);

            for (int n = 0; n < CaptureNum; n++)
            {
                LoadMatBinary(measPath + _fn_FlatWhite + "_" + n.ToString(), out mat);
                if (n == 0)
                {
                    mat.ConvertTo(matAvg, MatType.CV_32FC1);
                }
                else
                {
                    // modified by Hotta 2022/07/13
#if Coverity
                    using (Mat _mat = new Mat())
                    {
                        mat.ConvertTo(_mat, MatType.CV_32FC1);
                        matAvg += _mat; // Coverityチェック　代入に失敗するとリーク
                    }
#else
                    Mat _mat = new Mat();
                    mat.ConvertTo(_mat, MatType.CV_32FC1);
                    matAvg += _mat;
                    _mat.Dispose();
#endif
                }
                mat.Dispose();
            }
            matAvg /= CaptureNum;   // Coverityチェック　上書きされた、元のmatAvgがリーク


            // added by Hotta 2022/10/04 for 映り込み
#if Reflection
            Mat matBlack = new Mat();

            // modified by Hotta 2022/10/19
            /*
            for (int n = 0; n < CaptureNum; n++)
            {
                LoadMatBinary(measPath + fn_BlackFile + "_" + n.ToString(), out mat);
                if (n == 0)
                {
                    mat.ConvertTo(matBlack, MatType.CV_32FC1);
                }
                else
                {
                    using (Mat _mat = new Mat())
                    {
                        mat.ConvertTo(_mat, MatType.CV_32FC1);
                        matBlack += _mat; // Coverityチェック　代入に失敗するとリーク
                    }
                }
                mat.Dispose();
            }
            matBlack /= CaptureNum;   // Coverityチェック　上書きされた、元のmatAvgがリーク
            */

            for (int n = 0; n < CaptureNum; n++)
            {
                LoadMatBinary(measPath + _fn_FlatWhite + "_Black_" + n.ToString(), out mat);
                if (n == 0)
                {
                    mat.ConvertTo(matBlack, MatType.CV_32FC1);
                }
                else
                {
                    using (Mat _mat = new Mat())
                    {
                        mat.ConvertTo(_mat, MatType.CV_32FC1);
                        matBlack += _mat; // Coverityチェック　代入に失敗するとリーク
                    }
                }
                mat.Dispose();
            }
            matBlack /= CaptureNum;   // Coverityチェック　上書きされた、元のmatAvgがリーク
            //


#if TempFile
            foreach (GapCamCorrectionValue gapCv in lstGapCamCp)
            {
                foreach (GapCamCp cp in gapCv.lstCellCp)
                {
                    if (gapCv.Unit.X == 2 && gapCv.Unit.Y == 2)
                    {
                        if (cp.CellNo == CellNum.Cell_2 && cp.Pos == CorrectPosition.TopLeft)
                        {
                            float* pB = (float*)matBlack.Data;
                            using (StreamWriter sw = new StreamWriter(applicationPath + @"\Temp\black.csv", true))
                            {
                                int tilePos = 0;
                                int startX = cp.CamArea[tilePos].BoundingRect().Left;
                                int startY = cp.CamArea[tilePos].BoundingRect().Top;
                                int endX = cp.CamArea[tilePos].BoundingRect().Right;
                                int endY = cp.CamArea[tilePos].BoundingRect().Bottom;
                                for (int y = startY; y <= endY; y++)
                                {
                                    string str = "";
                                    for (int x = startX; x <= endX; x++)
                                    {
                                        str += pB[y * matBlack.Width + x].ToString() + ",";
                                    }
                                    sw.WriteLine(str);
                                }
                                sw.WriteLine("");
                            }
                        }
                    }
                }
            }
#endif


#if TempFile
            mat = new Mat();
            matAvg.ConvertTo(mat, MatType.CV_8UC1, 1.0 / 16);
            mat.SaveImage(applicationPath + "\\Temp\\" + _fn_FlatWhite + "_Swing_.jpg");
            mat.Dispose();

            mat = new Mat();
            matBlack.ConvertTo(mat, MatType.CV_8UC1, 1.0 / 16);
            mat.SaveImage(applicationPath + "\\Temp\\" + _fn_FlatWhite + "_Black.jpg");
            mat.Dispose();
#endif

            // データとり用
            //matBlack.SetTo(0);
            //


            int width = matBlack.Width;
            int height = matBlack.Height;
            float* pAvg = (float*)matAvg.Data;
            float* pBlack = (float*)matBlack.Data;
            for(int y=0;y<height;y++)
            {
                for(int x=0;x<width;x++)
                {
                    float value = pAvg[y * width + x] - pBlack[y * width + x];
                    if (value < 0)
                        value = 0;
                    pAvg[y * width + x] = value;
                }
            }

            matBlack.Dispose();
#endif

#if TempFile
            mat = new Mat();
            matAvg.ConvertTo(mat, MatType.CV_8UC1, 1.0 / 16);
            mat.SaveImage(applicationPath + "\\Temp\\" + _fn_FlatWhite + "_Swing.jpg");
            mat.Dispose();
#endif

#if TempFile
            foreach (GapCamCorrectionValue gapCv in lstGapCamCp)
            {
                foreach (GapCamCp cp in gapCv.lstCellCp)
                {
                    if (gapCv.Unit.X == 2 && gapCv.Unit.Y == 2)
                    {
                        if (cp.CellNo == CellNum.Cell_2 && cp.Pos == CorrectPosition.TopLeft)
                        {
                            Mat gapPos = new Mat();
                            Cv2.Resize(m_MatGapPos, gapPos, new Size(m_MatGapPos.Width / 5, m_MatGapPos.Height / 5));
                            byte* pGapPos = (byte*)gapPos.Data;
                            using (StreamWriter sw = new StreamWriter(applicationPath + @"\Temp\gapPos.csv", true))
                            {
                                int tilePos = 0;
                                int startX = cp.CamArea[tilePos].BoundingRect().Left;
                                int startY = cp.CamArea[tilePos].BoundingRect().Top;
                                int endX = cp.CamArea[tilePos].BoundingRect().Right;
                                int endY = cp.CamArea[tilePos].BoundingRect().Bottom;
                                for (int y = startY; y <= endY; y++)
                                {
                                    string str = "";
                                    for (int x = startX; x <= endX; x++)
                                    {
                                        str += pGapPos[y * gapPos.Width + x].ToString() + ",";
                                    }
                                    sw.WriteLine(str);
                                }
                                sw.WriteLine("");
                            }
                            gapPos.Dispose();
                        }
                    }
                }
            }
#endif

            winProgress.PutForward1Step();

            winProgress.ShowMessage("Calc Gap gain. " + _fn_FlatWhite);
            saveLog("Calc Gap gain. " + _fn_FlatWhite);

            int sigLevel = int.Parse(_fn_FlatWhite.Split('_')[1]);

            foreach (GapCamCorrectionValue gapCv in lstGapCamCp)
            {
                // Cell
                foreach (GapCamCp cp in gapCv.lstCellCp)
                {
#if TempFile
                    if(gapCv.Unit.X == 7 && gapCv.Unit.Y == 11)
                    {
                        if(cp.CellNo == CellNum.Cell_4 && cp.Pos == CorrectPosition.RightTop)
                            calcGapGain(/*mat*/matAvg, cp, sigLevel, true);
                    }
                    calcGapGain(/*mat*/matAvg, cp, sigLevel);
#else
                    calcGapGain(/*mat*/matAvg, cp, sigLevel);
#endif
                }
            }

            matAvg.Dispose();

            GC.Collect();
        }


        Mat m_MatGapPos;
        
        unsafe private void calcGapPos(Mat mat, Mat flat, string measPath)
        {
            // added by Hotta 2022/11/09
#if MultiControllerTest
            return;
#endif

            int width = mat.Width;
            int height = mat.Height;
            float* pMat = (float*)(mat.Data);
            int ch = mat.Channels();
            int step = width * ch;


#if TempFile
            Mat gapOrg = new Mat();
            mat.ConvertTo(gapOrg, MatType.CV_8UC1, 1.0 / 16);
            gapOrg.SaveImage(applicationPath + "\\Temp\\GapPos_.jpg");
            gapOrg.Dispose();
#endif

            string maskFile = measPath + "\\" + fn_MaskFile;
            // Mask.jpg
            /*
            Mat mask = new Mat(maskFile, ImreadModes.Grayscale);
            */

            // modified by Hotta 2022/10/31 for 複数コントローラ
#if MultiController
            Mat mask = new Mat();
            //Cv2.BitwiseAnd(m_MatMaskTop, m_MatMaskRight, mask);
            Cv2.BitwiseOr(m_MatMaskTop, m_MatMaskRight, mask);
#else
            Mat mask = m_MatMask.Clone();
#endif
            byte* pMask = (byte*)mask.Data;

#if TempFile
            mask.SaveImage(applicationPath + "\\Temp\\Mask.jpg");
            Mat _flat = flat / 16;
            _flat.SaveImage(applicationPath + "\\Temp\\Flat.jpg");
            _flat.Dispose();
#endif
            ////////////////////////////////////
            // modified by Hotta 2022/10/12
            // シェーディング補正の順序を変更
            ////////////////////////////////////
            /*
            // シェーディング補正用
            ushort* pFlat = (ushort*)flat.Data;
            double avg = 0;
            int count = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (pMask[y * width + x] != 0)
                    {
                        count++;
                        avg += pFlat[y * width + x];
                    }
                }
            }
            // modified by Hotta 2022/07/13
#if Coverity
            if(count != 0)
                avg /= count;
#else
            avg /= count;
#endif
            Mat _mat = new Mat(new Size(width, height), MatType.CV_16UC1);
            ushort* _pMat = (ushort*)(_mat.Data);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (pMask[y * width + x] != 0)
                    {
                        int v = (int)pMat[y * width + x];
                        double gain = avg / pFlat[y * width + x];
                        v = (int)(gain * v + 0.5);
                        _pMat[y * width + x] = (ushort)v;
                    }
                }
            }

            // added by Hotta 2022/10/11 for 映り込み
            // 全黒分を引く
#if Reflection
            Mat matBlack = new Mat();
            LoadMatBinary(measPath + fn_BlackFile + "_0", out matBlack);
            ushort* pBlack = (ushort*)matBlack.Data;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (pMask[y * width + x] != 0)
                    {
                        int v = _pMat[y * width + x] - pBlack[y * width + x];
                        if (v < 0)
                            v = 0;
                        _pMat[y * width + x] = (ushort)v;
                    }
                }
            }
            matBlack.Dispose();
#endif
            */
            /**/
#if Reflection
            Mat matBlack = new Mat();

            LoadMatBinary(measPath + fn_BlackFile + "_0", out matBlack);

            ushort* pBlack = (ushort*)matBlack.Data;
#endif
            // シェーディング補正用
            ushort* pFlat = (ushort*)flat.Data;
            double avg = 0;
            int count = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (pMask[y * width + x] != 0)
                    {
                        count++;
#if Reflection
                        avg += pFlat[y * width + x] - pBlack[y* width + x];
#else
                        avg += pFlat[y * width + x];
#endif
                    }
                }
            }
            if (count != 0)
                avg /= count;

            Mat _mat = new Mat(new Size(width, height), MatType.CV_16UC1);
            ushort* _pMat = (ushort*)(_mat.Data);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (pMask[y * width + x] != 0)
                    {
#if Reflection
                        int v = (int)(pMat[y * width + x] - pBlack[y * width + x]);
                        if (v < 0)
                            v = 0;
                        int fl = pFlat[y * width + x] - pBlack[y * width + x];
                        if (fl < 0)
                            fl = 1;
                        double gain = avg / fl;
                        v = (int)(gain * v + 0.5);
                        _pMat[y * width + x] = (ushort)v;
#else
                        int v = (int)pMat[y * width + x];
                        double gain = avg / pFlat[y * width + x];
                        v = (int)(gain * v + 0.5);
                        _pMat[y * width + x] = (ushort)v;
#endif
                    }
                }
            }
#if Reflection

#if TempFile
            Mat _black = new Mat();
            matBlack.ConvertTo(_black, MatType.CV_8UC1, 1.0 / 16);
            _black.SaveImage(applicationPath + "\\Temp\\Black.jpg");
            _black.Dispose();
#endif

            matBlack.Dispose();
#endif
            /**/
            ////////////////////////////////////

            mask.Dispose();

            Mat gray = new Mat();   // Coverityチェック　例外パスだとリーク
            // 大きな数で割りすぎているが、現在、上手くいっており、変更のリスクが大きいので、このままにしておく
            // 大きな数で除算することで、ノイズが消去されている可能性ある
            // 本来、1/64で割るべき？(2^14 / 2^8 = 2^6)
             _mat.ConvertTo(gray, MatType.CV_8UC1, 1.0 / 16);
#if TempFile
            gray.SaveImage(applicationPath + "\\Temp\\GapPos.jpg");
#endif

            _mat.Dispose();


            // modified by Hotta 2022/07/13
#if Coverity
            using (Mat maskedGray = new Mat())
            {
                // added by Hotta 2022/10/31 for 複数コントローラ
#if MultiController
                mask = new Mat();
                //Cv2.BitwiseAnd(m_MatMaskTop, m_MatMaskRight, mask);
                Cv2.BitwiseOr(m_MatMaskTop, m_MatMaskRight, mask);
                gray.CopyTo(maskedGray, mask);
#else
                gray.CopyTo(maskedGray, m_MatMask);
#endif
                // 目地位置の大きさチェック
                // added by Hotta 2022/10/31 for 複数コントローラ
#if MultiController
                Mat maskBin = mask.Threshold(0, 255, ThresholdTypes.Otsu);
                mask.Dispose();
#else
                Mat maskBin = m_MatMask.Threshold(0, 255, ThresholdTypes.Otsu);
#endif
                CvBlobs blobs = new CvBlobs(maskBin);
                int maskWidth = blobs.LargestBlob().Rect.Width;
                int maskHeight = blobs.LargestBlob().Rect.Height;

                // modified by Hotta 2022/07/13
#if Coverity
                using (Mat maskGapPos = maskedGray.Threshold(0, 255, ThresholdTypes.Otsu))
                {
#if TempFile
                    maskGapPos.SaveImage(applicationPath + "\\Temp\\maskGapPos.jpg");
#endif
                    blobs = new CvBlobs(maskGapPos);
                    int gapPosWidth = blobs.LargestBlob().Rect.Width;
                    int gapPosHeight = blobs.LargestBlob().Rect.Height;

                    maskBin.Dispose();
                }
#else
                Mat maskGapPos = maskedGray.Threshold(0, 255, ThresholdTypes.Otsu);
#if TempFile
                maskGapPos.SaveImage(applicationPath + "\\Temp\\maskGapPos.jpg");
#endif
                blobs = new CvBlobs(maskGapPos);
                int gapPosWidth = blobs.LargestBlob().Rect.Width;
                int gapPosHeight = blobs.LargestBlob().Rect.Height;

                maskBin.Dispose();
                maskGapPos.Dispose();
#endif

                // added by Hotta 2022/05/09 for あおり撮影
#if CaptureTilt

#else
            if(Math.Abs(maskWidth - gapPosWidth) > 10 || Math.Abs(maskHeight - gapPosHeight) > 10)
            {
                saveLog(string.Format("The detected Gap area size is too small. Gap:{0}x{1} < Cabinet:{2}x{3}", gapPosWidth, gapPosHeight, maskWidth, maskHeight));
                throw new Exception(string.Format("The detected Gap area size is too small. Gap:{0}x{1} < Cabinet:{2}x{3}\r\nPlease check display(Hatch).", gapPosWidth, gapPosHeight, maskWidth, maskHeight));
            }
#endif

#if ImageScale_x5
                if (m_MatGapPos != null)
                    m_MatGapPos.Dispose();
                m_MatGapPos = new Mat();

                Mat _maskedGray = new Mat();
                Cv2.Resize(maskedGray, _maskedGray, new Size(maskedGray.Width * 5, maskedGray.Height * 5));

                if (m_MatGapPos != null)
                    m_MatGapPos.Dispose();

                m_MatGapPos = _maskedGray.Threshold(0, 255, ThresholdTypes.Otsu);

                _maskedGray.Dispose();
#else
            if (m_MatGapPos != null)
                m_MatGapPos.Dispose();
            m_MatGapPos = new Mat();
            m_MatGapPos = maskedGray.Threshold(0, 255, ThresholdTypes.Otsu);
#endif

                gray.Dispose();
            }
#else
                Mat maskedGray = new Mat();
            gray.CopyTo(maskedGray, m_MatMask);

            // 目地位置の大きさチェック
            Mat maskBin = m_MatMask.Threshold(0, 255, ThresholdTypes.Otsu);
            CvBlobs blobs = new CvBlobs(maskBin);
            int maskWidth = blobs.LargestBlob().Rect.Width;
            int maskHeight = blobs.LargestBlob().Rect.Height;

            Mat maskGapPos = maskedGray.Threshold(0, 255, ThresholdTypes.Otsu);
#if TempFile
            maskGapPos.SaveImage(applicationPath + "\\Temp\\maskGapPos.jpg");
#endif
            blobs = new CvBlobs(maskGapPos);
            int gapPosWidth = blobs.LargestBlob().Rect.Width;
            int gapPosHeight = blobs.LargestBlob().Rect.Height;

            maskBin.Dispose();
            maskGapPos.Dispose();
            // added by Hotta 2022/05/09 for あおり撮影
#if CaptureTilt

#else
            if(Math.Abs(maskWidth - gapPosWidth) > 10 || Math.Abs(maskHeight - gapPosHeight) > 10)
            {
                saveLog(string.Format("The detected Gap area size is too small. Gap:{0}x{1} < Cabinet:{2}x{3}", gapPosWidth, gapPosHeight, maskWidth, maskHeight));
                throw new Exception(string.Format("The detected Gap area size is too small. Gap:{0}x{1} < Cabinet:{2}x{3}\r\nPlease check display(Hatch).", gapPosWidth, gapPosHeight, maskWidth, maskHeight));
            }
#endif

#if ImageScale_x5
            if (m_MatGapPos != null)
                m_MatGapPos.Dispose();
            m_MatGapPos = new Mat();

            Mat _maskedGray = new Mat();
            Cv2.Resize(maskedGray, _maskedGray, new Size(maskedGray.Width * 5, maskedGray.Height * 5));

            if (m_MatGapPos != null)
                m_MatGapPos.Dispose();

            m_MatGapPos = _maskedGray.Threshold(0, 255, ThresholdTypes.Otsu);

            maskedGray.Dispose();
            _maskedGray.Dispose();
#else
            if (m_MatGapPos != null)
                m_MatGapPos.Dispose();
            m_MatGapPos = new Mat();
            m_MatGapPos = maskedGray.Threshold(0, 255, ThresholdTypes.Otsu);
#endif

            gray.Dispose();
            maskedGray.Dispose();
#endif

                //m_MatGapPos.SaveImage(measPath + @"GapPos.jpg");
            }


            // ある入力信号レベルにおいて、
            // 計測位置 vs 目地輝度比 を直線近似して、
            // モジュール角の目地輝度比を算出する
            unsafe private void calcGapGain(Mat mat, GapCamCp cp, int sigLevel, bool debug = false)
        {
            double[] gapContrast = new double[TrimAreaNum];
            double[] gapBright = new double[TrimAreaNum];
            double[] aroundBright = new double[TrimAreaNum];


            for (int n = 0; n < TrimAreaNum; n++)
            {
                // modified by Hotta 2022/06/27
#if RotateRect
                int startX = cp.CamArea[n].BoundingRect().Left;
                int startY = cp.CamArea[n].BoundingRect().Top;
                int endX = cp.CamArea[n].BoundingRect().Right;
                int endY = cp.CamArea[n].BoundingRect().Bottom;
#else
                int startX = (int)(cp.CamArea[n].StartPos.X + 0.5);
                int startY = (int)(cp.CamArea[n].StartPos.Y + 0.5);
                int endX = (int)(cp.CamArea[n].EndPos.X + 0.5);
                int endY = (int)(cp.CamArea[n].EndPos.Y + 0.5);
#endif
                // modified by Hotta 2022/07/13
                int width = endX - startX;
                int height = endY - startY;
                // こちらが正しいが、Ver1.1の実績および影響が大きいので、上の方で行う。
                //int width = endX - startX + 1;
                //int height = endY - startY + 1;


                // modified by Hotta 2022/07/14
                // メモリリークしているかもしれないので、＋１だけ余分に確保する
                //Mat flat = new Mat(new Size(width, height), MatType.CV_32FC3);
                // modified byb Hotta 2022/08/18 for Coverity
                /*
                Mat flat = new Mat(new Size(width + 1, height + 1), MatType.CV_32FC3);
                */
                Mat flat;


                //int ch = flat.Channels();
                //int step = width * ch;
                //int stepMat = mat.Width * ch;

                //float* pMat = (float*)mat.Data;
                flat = mat[startY, endY, startX, endX]; // Coverityチェック　上書きされた、元のflatがリーク

                //float* pFlat = (float*)flat.Data;

                /*
                using (StreamWriter sw = new StreamWriter(applicationPath + @"\Temp\flat.csv", true))
                {
                    for (int y = 0; y < height + 10; y++)
                    {
                        string str = "";
                        for (int x = 0; x < width + 10; x++)
                        {
                            str += pFlat[y * step + x * ch + 1].ToString() + ",";
                        }
                        sw.WriteLine(str);
                    }
                    sw.WriteLine("");
                }
                */
#region debug
                //if (debug == true)
                //{
                //    using (StreamWriter sw = new StreamWriter(applicationPath + @"\Temp\mat.csv", true))
                //    {
                //        sw.WriteLine(sigLevel.ToString() + "_" + n.ToString());
                //        for (int y = 0; y < height; y++)
                //        {
                //            string str = "";
                //            for (int x = 0; x < width; x++)
                //            {
                //                str += pMat[(startY + y) * stepMat + (startX + x) * ch + 1].ToString() + ",";
                //            }
                //            sw.WriteLine(str);
                //        }
                //        sw.WriteLine("");
                //    }
                //}
#endregion

                // deleted by Hotta 2022/02/16
                //Mat[] splitFlat = flat.Split();



#if TempFile
                if(debug == true)
                {
                    if(true/*sigLevel == 445*/)
                    {
                        if(n == 0)
                        {
                            /*
                            float* pFlat = (float*)flat.Data;
                            using (StreamWriter sw = new StreamWriter(applicationPath + @"\Temp\flat.csv", true))
                            {
                                for(int y = 0; y<flat.Height;y++)
                                {
                                    string str = "";
                                    for (int x = 0; x < flat.Width; x++)
                                    {
                                        str += pFlat[y * flat.Width + x].ToString() + ",";
                                    }
                                    sw.WriteLine(str);
                                }
                                sw.WriteLine("");
                            }
                            */
                            float* pMat = (float*)mat.Data;
                            using (StreamWriter sw = new StreamWriter(applicationPath + @"\Temp\flat.csv", true))
                            {
                                for (int y = startY; y <= endY; y++)
                                {
                                    string str = "";
                                    for (int x = startX; x <= endX; x++)
                                    {
                                        str += pMat[y * mat.Width + x].ToString() + ",";
                                    }
                                    sw.WriteLine(str);
                                }
                                sw.WriteLine("");
                            }

                        }
                    }
                }
#endif


#if ImageScale_x5
                // 矩形の範囲
                Mat maskBkg = new Mat(new Size(width * 5, height * 5), MatType.CV_8UC1);    // Coverityチェック　例外パスだとリーク
                maskBkg.SetTo(255); // Coverityチェック　戻り値がリーク

                // 対象画像をトリミングして、解像度x5倍にする
                Mat target = new Mat();
                // modified by Hotta 2022/02/16
                // Cv2.Resize(splitFlat[1], target, new Size(width * 5, height * 5));
                Cv2.Resize(flat, target, new Size(width * 5, height * 5));

                // deleted by Hotta 2022/02/16
                //float* pSplit = (float*)splitFlat[1].Data;
                float* pTarget = (float*)target.Data;

#region debug
                //float* pFlat0 = (float*)flat.Data;
                //if (debug == true)
                //{
                //    using (StreamWriter sw = new StreamWriter(applicationPath + @"\Temp\Flat0.csv", true))
                //    {
                //        sw.WriteLine(sigLevel.ToString() + "_" + n.ToString());
                //        for (int y = 0; y < height; y++)
                //        {
                //            string str = "";
                //            for (int x = 0; x < width; x++)
                //            {
                //                str += pFlat0[y * width + x].ToString() + ",";
                //            }
                //            sw.WriteLine(str);
                //        }
                //        sw.WriteLine("");
                //    }
                //    using (StreamWriter sw = new StreamWriter(applicationPath + @"\Temp\Flat.csv", true))
                //    {
                //        sw.WriteLine(sigLevel.ToString() + "_" + n.ToString());
                //        for (int y = 0; y < target.Height; y++)
                //        {
                //            string str = "";
                //            for (int x = 0; x < target.Width; x++)
                //            {
                //                str += pTarget[y * target.Width + x].ToString() + ",";
                //            }
                //            sw.WriteLine(str);
                //        }
                //        sw.WriteLine("");
                //    }
                //}
#endregion

                // ギャップの明るさ
                Mat maskGap = new Mat(new Size(width * 5, height * 5), MatType.CV_8UC1);    // Coverityチェック　例外パスだとリーク
                Mat localGapPos = m_MatGapPos[new OpenCvSharp.Rect(startX * 5, startY * 5, width * 5, height * 5)];
                Cv2.BitwiseAnd(maskBkg, localGapPos, maskGap);

                // added by Hotta 2022/06/27
#if RotateRect
                Mat matMaskRotate = new Mat(maskGap.Size(), MatType.CV_8UC1);   // Coverityチェック　例外パスだとリーク
                matMaskRotate.SetTo(0);
                OpenCvSharp.Point2f[] arrayVertices = cp.CamArea[n].Points();
                List<OpenCvSharp.Point> vertices = new List<OpenCvSharp.Point>();
                for (int i = 0; i < 4; i++)
                    vertices.Add(new OpenCvSharp.Point((arrayVertices[i].X - startX) * 5, (arrayVertices[i].Y - startY) * 5));
                matMaskRotate.FillConvexPoly(vertices, new Scalar(255));
                //matMaskRotate.SaveImage("c:\\temp\\maskRotate.jpg");

                Mat maskGap2 = new Mat(maskGap.Size(), maskGap.Type());
                Cv2.BitwiseAnd(maskGap, matMaskRotate, maskGap2);
                //maskGap.SaveImage("c:\\temp\\maskGap.jpg");
                //matMaskRotate.Dispose();  // 後で使う
#endif
                byte* pMaskGap = (byte*)maskGap.Data;
#region debug
                //if (debug == true)
                //{
                //    using (StreamWriter sw = new StreamWriter(applicationPath + @"\Temp\MaskGap.csv", true))
                //    {
                //        sw.WriteLine(sigLevel.ToString() + "_" + n.ToString());
                //        for (int y = 0; y < maskGap.Height; y++)
                //        {
                //            string str = "";
                //            for (int x = 0; x < maskGap.Width; x++)
                //            {
                //                str += pMaskGap[y * maskGap.Width + x].ToString() + ",";
                //            }
                //            sw.WriteLine(str);
                //        }
                //        sw.WriteLine("");
                //    }
                //}
#endregion

                Scalar gap = new Scalar();
                Scalar stddev;
                // added by Hotta 2022/06/27
#if RotateRect
                Cv2.MeanStdDev(target, out gap, out stddev, maskGap2);
#else
                Cv2.MeanStdDev(target, out gap, out stddev, maskGap);
#endif
                //

                // modified by Hotta 2022/07/13
#if Coverity
                Mat maskAround;
                // 周辺の明るさ
                using (Mat invGap = new Mat(new Size(width * 5, height * 5), MatType.CV_8UC1))
                using (Mat localGapDilate = new Mat())
                using (Mat elem = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3 * 5, 3 * 5)))
                {
                    Cv2.Dilate(localGapPos, localGapDilate, elem);
                    //elem.Dispose();

                    Cv2.BitwiseNot(/*localGapPos*/localGapDilate, invGap);
                    /*Mat*/ maskAround = new Mat(new Size(width * 5, height * 5), MatType.CV_8UC1);
                    Cv2.BitwiseAnd(maskBkg, invGap, maskAround);
                }
#else
                // 周辺の明るさ
                Mat invGap = new Mat(new Size(width * 5, height * 5), MatType.CV_8UC1);

                Mat localGapDilate = new Mat();
                Mat elem = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3 * 5, 3 * 5));
                Cv2.Dilate(localGapPos, localGapDilate, elem);
                elem.Dispose();

                Cv2.BitwiseNot(/*localGapPos*/localGapDilate, invGap);
                Mat maskAround = new Mat(new Size(width * 5, height * 5), MatType.CV_8UC1);
                Cv2.BitwiseAnd(maskBkg, invGap, maskAround);
#endif

                // added by Hotta 2022/06/27
#if RotateRect
                Mat maskAround2 = new Mat(maskAround.Size(), maskAround.Type());
                Cv2.BitwiseAnd(maskAround, matMaskRotate, maskAround2);
                //maskAround.SaveImage("c:\\temp\\maskAround.jpg");
                matMaskRotate.Dispose();
#endif

                byte* pMaskAround = (byte*)maskAround.Data;
#region debug
                //if (debug == true)
                //{
                //    using (StreamWriter sw = new StreamWriter(applicationPath + @"\Temp\MaskAround.csv", true))
                //    {
                //        sw.WriteLine(sigLevel.ToString() + "_" + n.ToString());
                //        for (int y = 0; y < maskAround.Height; y++)
                //        {
                //            string str = "";
                //            for (int x = 0; x < maskAround.Width; x++)
                //            {
                //                str += pMaskAround[y * maskAround.Width + x].ToString() + ",";
                //            }
                //            sw.WriteLine(str);
                //        }
                //        sw.WriteLine("");
                //    }
                //}
#endregion

                Scalar around = new Scalar();
                // added by Hotta 2022/06/27
#if RotateRect
                Cv2.MeanStdDev(target, out around, out stddev, maskAround2);
#else
                Cv2.MeanStdDev(target, out around, out stddev, maskAround);
#endif
                //
#else

#region debug
                if (debug == true)
                {
                    using (StreamWriter sw = new StreamWriter(applicationPath + @"\Temp\Flat.csv", true))
                    {
                        sw.WriteLine(sigLevel.ToString() + "_" + n.ToString());
                        for (int y = 0; y < height; y++)
                        {
                            string str = "";
                            for (int x = 0; x < width; x++)
                            {
                                str += pFlat[y * step + x * ch + 1].ToString() + ",";
                            }
                            sw.WriteLine(str);
                        }
                        sw.WriteLine("");
                    }
                }
#endregion

                // 矩形の範囲
                Mat maskBkg = new Mat(new Size(width, height), MatType.CV_8UC1);
                byte* pMaskBkg = (byte*)maskBkg.Data;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        pMaskBkg[y * width + x] = 255;
                    }
                }

                // ギャップの明るさ
                Mat maskGap = new Mat(new Size(width, height), MatType.CV_8UC1);
                Mat localGapPos = m_MatGapPos[new OpenCvSharp.Rect(startX, startY, width, height)];
                Cv2.BitwiseAnd(maskBkg, localGapPos, maskGap);
                byte* pMaskGap = (byte*)maskGap.Data;
#region debug
                if (debug == true)
                {
                    using (StreamWriter sw = new StreamWriter(applicationPath + @"\Temp\MaskGap.csv", true))
                    {
                        sw.WriteLine(sigLevel.ToString() + "_" + n.ToString());
                        for (int y = 0; y < height; y++)
                        {
                            string str = "";
                            for (int x = 0; x < width; x++)
                            {
                                str += pMaskGap[y * width + x].ToString() + ",";
                            }
                            sw.WriteLine(str);
                        }
                        sw.WriteLine("");
                    }
                }
#endregion
                ////
                ////maskGap.SaveImage(applicationPath + @"\Temp\MaskGap.jpg");
                //// 暗点を除去して、その平均明るさを求める
                Scalar gap = new Scalar();
                Scalar stddev;
                double minVal, maxVal;
                OpenCvSharp.Point minLoc, maxLoc;
                float* p = (float*)splitFlat[1].Data;
                for (int i = 0; i < 10; i++)
                {
                    Cv2.MeanStdDev(splitFlat[1], out gap, out stddev, maskGap);
                    Cv2.MinMaxLoc(splitFlat[1], out minVal, out maxVal, out minLoc, out maxLoc, maskGap);
                    //if (minVal < gap[0] - 2.0 * stddev[0])
                    if (minVal < gap[0] * 0.9)
                    {
                        p[minLoc.Y * splitFlat[1].Width + minLoc.X] = (float)gap[0];
                    }
                    else
                    {
                        break;
                    }
                }

                // ヒストグラムの中央付近の値で計算する
                /*
                p = (float*)splitFlat[1].Data;
                pMaskGap = (byte*)maskGap.Data;
                List<float> listValue = new List<float>();
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (pMaskBkg[y * width + x] != 0)
                            listValue.Add(p[y * width + x]);
                    }
                }

                listValue.Sort();

                int count = listValue.Count;
                listValue.RemoveRange(0, (int)(0.1 * count));
                listValue.Reverse();
                listValue.RemoveRange(0, (int)(0.1 * count));
                gap[0] = 0;
                for(int i=0;i<listValue.Count;i++)
                {
                    gap[0] += listValue[i];
                }
                gap[0] /= listValue.Count;
                */



                //int maskGapArea = Cv2.CountNonZero(maskGap);

                // 周辺の明るさ
                Mat invGap = new Mat(new Size(width, height), MatType.CV_8UC1);

                Mat localGapDilate = new Mat();
                Mat elem = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
                Cv2.Dilate(localGapPos, localGapDilate, elem);

                Cv2.BitwiseNot(/*localGapPos*/localGapDilate, invGap);
                Mat maskAround = new Mat(new Size(width, height), MatType.CV_8UC1);
                Cv2.BitwiseAnd(maskBkg, invGap, maskAround);

                byte* pMaskAround = (byte*)maskAround.Data;
#region debug
                if (debug == true)
                {
                    using (StreamWriter sw = new StreamWriter(applicationPath + @"\Temp\MaskAround.csv", true))
                    {
                        sw.WriteLine(sigLevel.ToString() + "_" + n.ToString());
                        for (int y = 0; y < height; y++)
                        {
                            string str = "";
                            for (int x = 0; x < width; x++)
                            {
                                str += pMaskAround[y * width + x].ToString() + ",";
                            }
                            sw.WriteLine(str);
                        }
                        sw.WriteLine("");
                    }
                }
#endregion

                // 周辺エリアから、暗点を除去する
                Scalar around = new Scalar();
                p = (float*)splitFlat[1].Data;
                for (int i = 0; i < 100; i++)
                {
                    Cv2.MeanStdDev(splitFlat[1], out around, out stddev, maskAround);
                    Cv2.MinMaxLoc(splitFlat[1], out minVal, out maxVal, out minLoc, out maxLoc, maskAround);
                    //if (minVal < around[0] - 2.0 * stddev[0])
                    if (minVal < around[0] * 0.9)
                    {
                        p[minLoc.Y * splitFlat[1].Width + minLoc.X] = (float)around[0];
                    }
                    else
                    {
                        break;
                    }
                }
                //
#endif

                gapContrast[n] = gap[0] / around[0];
                gapBright[n] = gap[0];
                aroundBright[n] = around[0];

                target.Dispose();
                // deleted by Hotta 2022/02/16
                /*
                splitFlat[0].Dispose();
                splitFlat[1].Dispose();
                splitFlat[2].Dispose();
                */
                maskAround.Dispose();
                // modified by Hotta 2022/07/13
#if Coverity
#else
                invGap.Dispose();
                localGapDilate.Dispose();
#endif
                maskGap.Dispose();
                maskBkg.Dispose();
                flat.Dispose();
                localGapPos.Dispose();

                // added by Hotta 2022/06/27
#if RotateRect
                maskGap2.Dispose();
                maskAround2.Dispose();
#endif
                //GC.Collect();



#if TempFile
                if(debug == true && n == 3)
                {
                    using (StreamWriter sw = new StreamWriter(applicationPath + @"\Temp\bright.csv", true))
                    {
                        sw.WriteLine("{0},{1},{2}", sigLevel, gap[0], around[0]);
                    }
                }
#endif

            }

#region debug
            if (debug == true)
            {
#if TempFile
                using (StreamWriter sw = new StreamWriter(applicationPath + @"\Temp\GapBright.csv", true))
                {
                    string str = "";
                    str = sigLevel.ToString() + ",";
                    for (int n = 0; n < TrimAreaNum; n++)
                    {
                        str += gapBright[n].ToString() + ",";
                    }
                    sw.WriteLine(str);
                }
                using (StreamWriter sw = new StreamWriter(applicationPath + @"\Temp\AroundBright.csv", true))
                {
                    string str = "";
                    str = sigLevel.ToString() + ",";
                    for (int n = 0; n < TrimAreaNum; n++)
                    {
                        str += aroundBright[n].ToString() + ",";
                    }
                    sw.WriteLine(str);
                }
                using (StreamWriter sw = new StreamWriter(applicationPath + @"\Temp\GapContrast.csv", true))
                {
                    string str = "";
                    str = sigLevel.ToString() + ",";
                    for (int n = 0; n < TrimAreaNum; n++)
                    {
                        str += gapContrast[n].ToString() + ",";
                    }
                    sw.WriteLine(str);
                }
#endif
            }
#endregion
            // modified by Hotta 2022/07/08
#if CorrectTargetEdge
            if (cp.Pos == CorrectPosition.TopLeft || cp.Pos == CorrectPosition.RightTop || cp.Pos == CorrectPosition.BottomLeft || cp.Pos == CorrectPosition.LeftTop)
#else
            if (cp.Pos == CorrectPosition.TopLeft || cp.Pos == CorrectPosition.RightTop)
#endif
            {
                cp.Gap = gapBright[0];
                cp.Around = aroundBright[0];
            }
            else
            {
                cp.Gap = gapBright[TrimAreaNum - 1];
                cp.Around = aroundBright[TrimAreaNum - 1];
            }


            // 算出したgapContrast[]から、Module端の値を算出
            // 直線近似
            double a, b;
            // modified by Hotta 2022/07/08
#if CorrectTargetEdge
            if (cp.Pos == CorrectPosition.TopLeft || cp.Pos == CorrectPosition.TopRight || cp.Pos == CorrectPosition.BottomLeft || cp.Pos == CorrectPosition.BottomRight)
#else
            if (cp.Pos == CorrectPosition.TopLeft || cp.Pos == CorrectPosition.TopRight)
#endif
            {
                List<Point2f> points = new List<Point2f>();

                for (int n = 0; n < TrimAreaNum; n++)
                {
                    points.Add(new Point2f(TrimAreaTopPos[n], (float)gapContrast[n]));
                }
                Line2D line = Cv2.FitLine(points, DistanceTypes.L1, 0, 0.01, 0.01);
                a = line.Vy / line.Vx;
                b = -line.Vy / line.Vx * line.X1 + line.Y1;
                // modified by Hotta 2022/07/08
#if CorrectTargetEdge
                if (cp.Pos == CorrectPosition.TopLeft || cp.Pos == CorrectPosition.BottomLeft)
#else
                if (cp.Pos == CorrectPosition.TopLeft)
#endif
                    cp.GapGain = a * 0 + b;
                else
                    cp.GapGain = a * (modDx - 1) + b;
            }
            else
            {
                List<Point2f> points = new List<Point2f>();

                for (int n = 0; n < TrimAreaNum; n++)
                {
                    points.Add(new Point2f(TrimAreaRightPos[n], (float)gapContrast[n]));
                }
                Line2D line = Cv2.FitLine(points, DistanceTypes.L1, 0, 0.01, 0.01);
                a = line.Vy / line.Vx;
                b = -line.Vy / line.Vx * line.X1 + line.Y1;
                // modified by Hotta 2022/07/08
#if CorrectTargetEdge
                if (cp.Pos == CorrectPosition.RightTop || cp.Pos == CorrectPosition.LeftTop)
#else
                if (cp.Pos == CorrectPosition.RightTop)
#endif
                    cp.GapGain = a * 0 + b;
                else
                    cp.GapGain = a * (modDy - 1) + b;

            }
            cp.Slope = a;
            cp.Offset = b;
        }


        unsafe void calcMoireCheckArea(string moireArea, string black, out List<Area> lstArea)
        {
            List<Area> lstAreaAll = new List<Area>();

            Mat matMoire = new Mat();
            LoadMatBinary(Path.GetDirectoryName(moireArea) + "\\" + Path.GetFileNameWithoutExtension(moireArea), out matMoire);

            Mat matBlack = new Mat();

            LoadMatBinary(Path.GetDirectoryName(black) + "\\" + Path.GetFileNameWithoutExtension(black), out matBlack);

            int width = matMoire.Width; // Coverityチェック　例外パスだとリーク
            int height = matMoire.Height;
            ushort* pMoire = (ushort*)matMoire.Data;
            ushort* pBlack = (ushort*)matBlack.Data;

            // modified byb Hotta 2022/07/13
#if Coverity
            using (Mat gray = new Mat())
            {
                using (Mat mat = new Mat(new Size(width, height), MatType.CV_16UC1))
                {
                    ushort* pMat = (ushort*)mat.Data;

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int v = (int)pMoire[y * width + x] - pBlack[y * width + x];
                            if (v < 0)
                                v = 0;
                            pMat[y * width + x] = (ushort)v;
                        }
                    }
                    matMoire.Dispose();
                    matBlack.Dispose();

                    mat.ConvertTo(gray, MatType.CV_8UC1, 1.0 / 16);
                }

                using (Mat binary = gray.Threshold(0, 0xff, ThresholdTypes.Otsu))
                {
#if TempFile
                Cv2.ImWrite(applicationPath + @"\Temp\" + System.IO.Path.GetFileNameWithoutExtension(moireArea) + "_.jpg", binary, (int[])null);
#endif
                    Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(10, 10));
                    Mat closing = new Mat();
                    Cv2.MorphologyEx(binary, closing, MorphTypes.Close, kernel);

#if TempFile
                Cv2.ImWrite(applicationPath + @"\Temp\" + System.IO.Path.GetFileNameWithoutExtension(moireArea) + ".jpg", closing);
#endif
                    CvBlobs blobs = new CvBlobs(closing);

                    double area = calcArea(blobs);

                    blobs.FilterByArea((int)(area * 0.6), (int)(area * 1.4));

                    foreach (KeyValuePair<int, CvBlob> pair in blobs)
                    {
                        Area trim = new Area(pair.Value.Rect.Left, pair.Value.Rect.Top, pair.Value.Rect.Height, pair.Value.Rect.Width);
                        lstAreaAll.Add(trim);
                    }
                    closing.Dispose();
                }
            }
#else
            Mat mat = new Mat(new Size(width, height), MatType.CV_16UC1);
            ushort* pMat = (ushort*)mat.Data;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int v = (int)pMoire[y * width + x] - pBlack[y * width + x];
                    if (v < 0)
                        v = 0;
                    pMat[y * width + x] = (ushort)v;
                }
            }
            matMoire.Dispose();
            matBlack.Dispose();


            Mat gray = new Mat();

            mat.ConvertTo(gray, MatType.CV_8UC1, 1.0 / 16);

            using (Mat binary = gray.Threshold(0, 0xff, ThresholdTypes.Otsu))
            {
#if TempFile
                Cv2.ImWrite(applicationPath + @"\Temp\" + System.IO.Path.GetFileNameWithoutExtension(moireArea) + "_.jpg", binary, (int[])null);
#endif
                Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(10, 10));
                Mat closing = new Mat();
                Cv2.MorphologyEx(binary, closing, MorphTypes.Close, kernel);

#if TempFile
                Cv2.ImWrite(applicationPath + @"\Temp\" + System.IO.Path.GetFileNameWithoutExtension(moireArea) + ".jpg", closing);
#endif
                CvBlobs blobs = new CvBlobs(closing);

                double area = calcArea(blobs);

                blobs.FilterByArea((int)(area * 0.6), (int)(area * 1.4));

                foreach (KeyValuePair<int, CvBlob> pair in blobs)
                {
                    Area trim = new Area(pair.Value.Rect.Left, pair.Value.Rect.Top, pair.Value.Rect.Height, pair.Value.Rect.Width);
                    lstAreaAll.Add(trim);
                }
                closing.Dispose();
            }

            gray.Dispose();


#endif


            // modified by Hotta 2022/07/13
#if Coverity

#else
            mat.Dispose();
#endif
            if (lstAreaAll.Count < 8)
            {
                saveLog("The number of area to check moire is too few.");
                throw new Exception("The number of area to check moire is too few.\r\nPlease check display status(Large Tile).");
            }

            // 画像中心に近い8ケを選択
            lstArea = new List<Area>();
            for(int i=0;i<8;i++)
            {
                Area nearArea = new Area();
                double min = double.MaxValue;
                int min_index = 0;
                for (int n = 0; n < lstAreaAll.Count; n++)
                {
                    double distance = (width / 2 - lstAreaAll[n].CenterPos.X) * (width / 2 - lstAreaAll[n].CenterPos.X);
                    distance += (height / 2 - lstAreaAll[n].CenterPos.Y) * (height / 2 - lstAreaAll[n].CenterPos.Y);
                    distance = Math.Sqrt(distance);

                    if (min > distance)
                    {
                        min_index = n;
                        nearArea = lstAreaAll[n];
                        min = distance;
                    }
                }
                lstArea.Add(nearArea);
                lstAreaAll.RemoveAt(min_index);
            }
        }

        unsafe private void checkMoire(string moireCheck, List<Area> lstArea)
        {
            int moireCheckNum = lstArea.Count;
            double[] moireValue = new double[moireCheckNum];

            //double moireThreshold = 0.56;
            double moireThreshold = Settings.Ins.GapCam.MoireSpec;

            Mat matMoire = new Mat();   // Coverityチェック　例外パスだとリーク
            LoadMatBinary(Path.GetDirectoryName(moireCheck) + "\\" + Path.GetFileNameWithoutExtension(moireCheck), out matMoire);
            ushort* pMoire = (ushort*)matMoire.Data;

            Dft dft = new Dft();
            OpenCvSharp.Rect moireRoi;

            for (int n=0;n<lstArea.Count;n++)
            {
                int imageWidth = matMoire.Width;
                int imageHeight = matMoire.Height;
                int size = 0;
                if (lstArea[n].Width < lstArea[n].Height)
                    size = lstArea[n].Width;
                else
                    size = lstArea[n].Height;

                // 最適なサイズに変更する
                int optimalSize;
                while(true)
                {
                    optimalSize = dft.GetOptimalSize(size);
                    if (optimalSize <= size)
                        break;
                    else
                        size--;
                    if(size == 1)
                    {
                        saveLog("The optimal size for DFT to detect moire cannot be calculated.");
                        throw new Exception("The optimal size for DFT to detect moire cannot be calculated.\r\nPlease check display status(Large Tile).");
                    }
                }
                size = optimalSize;


                Mat matLog = new Mat(new OpenCvSharp.Size(size * 3, size), MatType.CV_8UC1);    // Coverityチェック　例外パスだとリーク

                // ROIを正方形にしないと、意図したDFTが実行されない。
                moireRoi = new OpenCvSharp.Rect(
                    (int)(lstArea[n].StartPos.X + 0.5),
                    (int)(lstArea[n].StartPos.Y + 0.5),
                    size,
                    size
                    );

                //
                // modified by Hotta 2022/07/13
#if Coverity
                float* pMat;
                Mat mat;
                Scalar mean;
                using (Mat mat0 = new Mat(new OpenCvSharp.Size(moireRoi.Width, moireRoi.Height), MatType.CV_32FC1))
                {
                    /*float* */ pMat = (float*)(mat0.Data);
                    for (int y = 0; y < moireRoi.Height; y++)
                    {
                        for (int x = 0; x < moireRoi.Width; x++)
                        {
                            pMat[y * mat0.Width + x] = (float)(pMoire[(y + moireRoi.Y) * imageWidth + (x + moireRoi.X)]);
                        }
                    }

                    // 平均明るさで正規化
                    /*Scalar*/ mean = Cv2.Mean(mat0);
                    mat = new Mat(mat0.Size(), mat0.Type());
                    mat = (1.0 / mean[0] * (4096 / 2)) * mat0;  // Coverityチェック　上書きされた、元のmatがリーク。代入に失敗するとリーク
                }
#else
                Mat mat = new Mat(new OpenCvSharp.Size(moireRoi.Width, moireRoi.Height), MatType.CV_32FC1);
                float* pMat = (float*)(mat.Data);
                for (int y = 0; y < moireRoi.Height; y++)
                {
                    for (int x = 0; x < moireRoi.Width; x++)
                    {
                        pMat[y * mat.Width + x] = (float)(pMoire[(y + moireRoi.Y) * imageWidth + (x + moireRoi.X)]);
                    }
                }

                // 平均明るさで正規化
                Scalar mean = Cv2.Mean(mat);
                mat = (1.0 / mean[0] * (4096 / 2)) * mat;
#endif

                pMat = (float*)mat.Data;

                // modified by Hotta 2022/07/13
#if Coverity
                Mat src8;   // Coverityチェック　例外パスだとリーク
                byte* pSrc;
                using (Mat _mat = new Mat(mat.Size(), MatType.CV_8UC1))
                {
                    mat.ConvertTo(_mat, MatType.CV_8UC1, 1.0 / 16);
                    src8 = _mat.Clone();
                    pSrc = (byte*)src8.Data;
                }
#else
                Mat _mat = new Mat(mat.Size(), MatType.CV_8UC1);
                mat.ConvertTo(_mat, MatType.CV_8UC1, 1.0 / 16);
                Mat src8 = _mat.Clone();
                byte* pSrc = (byte*)src8.Data;
                _mat.Dispose();
#endif

                // シェーディング補正
                double avg = 0;
                Mat slope = new Mat(new OpenCvSharp.Size(mat.Width, 1), MatType.CV_32FC1);
                Cv2.Reduce(mat, slope, ReduceDimension.Row, ReduceTypes.Avg, MatType.CV_32FC1);
                float* pSlope = (float*)slope.Data;
                avg = 0;
                for (int x = 0; x < slope.Width; x++)
                    avg += pSlope[x];
                avg /= slope.Width;
                for (int y = 0; y < moireRoi.Height; y++)
                {
                    for (int x = 0; x < moireRoi.Width; x++)
                    {
                        pMat[y * mat.Width + x] *= (float)(avg / pSlope[x]);
                    }
                }
                slope.Dispose();

                slope = new Mat(new OpenCvSharp.Size(1, mat.Height), MatType.CV_32FC1);
                Cv2.Reduce(mat, slope, ReduceDimension.Column, ReduceTypes.Avg, MatType.CV_32FC1);
                pSlope = (float*)slope.Data;
                avg = 0;
                for (int y = 0; y < slope.Height; y++)
                    avg += pSlope[y];
                avg /= slope.Height;
                for (int y = 0; y < moireRoi.Height; y++)
                {
                    for (int x = 0; x < moireRoi.Width; x++)
                    {
                        pMat[y * mat.Width + x] *= (float)(avg / pSlope[y]);
                    }
                }
                //
                // modified by Hotta 2022/07/13
#if Coverity

#else
                Mat src = mat.Clone();
#endif

                // modified by Hotta 2022/07/13
#if Coverity
                Mat flat8;  // Coverityチェック　例外パスだとリーク
                using (Mat _mat = new Mat(mat.Size(), MatType.CV_8UC1))
                {
                    mat.ConvertTo(_mat, MatType.CV_8UC1, 1.0 / 16);
                    flat8 = _mat.Clone();
                }
#else
                _mat = new Mat(mat.Size(), MatType.CV_8UC1);
                mat.ConvertTo(_mat, MatType.CV_8UC1, 1.0 / 16);
                Mat flat8 = _mat.Clone();
                _mat.Dispose();

#endif

                // modified by Hotta 2022/07/13
#if Coverity
                double moireDft;

                // 元画像のDFT
                dft.SetSourceImage(mat);
                dft.TransForm();
                using (Mat work = dft.GetSpectrumImage())
                {
                    Mat dft8 = new Mat();
                    work.ConvertTo(dft8, MatType.CV_8UC1);
                    dft8 = dft8.ConvertScaleAbs(100.0, 0);  // Coverityチェック　上書きされた元のdft8がリーク
                    byte* pLog = (byte*)matLog.Data;
                    byte* pDft8 = (byte*)dft8.Data;
                    byte* pFlat = (byte*)flat8.Data;
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            pLog[y * matLog.Width + x] = pSrc[y * src8.Width + x];
                            pLog[y * matLog.Width + x + matLog.Width / 3 * 1] = pFlat[y * flat8.Width + x];
                            pLog[y * matLog.Width + x + matLog.Width / 3 * 2] = pDft8[y * dft8.Width + x];
                        }
                    }
                    dft8.Dispose();

                    Scalar stddev;
                    Cv2.MeanStdDev(work, out mean, out stddev);

                    // 元画像のDFTから、低周波成分を除く
                    float* p = (float*)work.Data;
                    p[work.Height / 2 * work.Width + work.Width / 2] = 0;

                    Scalar _mean;
                    Cv2.MeanStdDev(work, out _mean, out stddev);
                    /*double*/ moireDft = stddev[0] / mean[0];

                    //work.Dispose();
                }
                flat8.Dispose();
                mat.Dispose();
                src8.Dispose();
#else
                Mat work = new Mat();

                // 元画像のDFT
                dft.SetSourceImage(mat);
                dft.TransForm();
                work = dft.GetSpectrumImage();

                Mat dft8 = new Mat();
                work.ConvertTo(dft8, MatType.CV_8UC1);
                dft8 = dft8.ConvertScaleAbs(100.0, 0);
                byte* pLog = (byte*)matLog.Data;
                byte* pDft8 = (byte*)dft8.Data;
                byte* pFlat = (byte*)flat8.Data;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        pLog[y * matLog.Width + x] = pSrc[y * src8.Width + x];
                        pLog[y * matLog.Width + x + matLog.Width / 3 * 1] = pFlat[y * flat8.Width + x];
                        pLog[y * matLog.Width + x + matLog.Width / 3 * 2] = pDft8[y * dft8.Width + x];
                    }
                }
                flat8.Dispose();
                dft8.Dispose();
                mat.Dispose();

                Scalar stddev;
                Cv2.MeanStdDev(work, out mean, out stddev);

                // 元画像のDFTから、低周波成分を除く
                float* p = (float*)work.Data;
                p[work.Height / 2 * work.Width + work.Width / 2] = 0;

                Scalar _mean;
                Cv2.MeanStdDev(work, out _mean, out stddev);
                double moireDft = stddev[0] / mean[0];

                work.Dispose();
#endif

#if TempFile
                matLog.SaveImage(applicationPath + TempDir + "\\moire" + n.ToString() + ".bmp");
#endif
                    matLog.Dispose();

                moireValue[n] = moireDft;
            }
            matMoire.Dispose();

            double moire = 0;
            for (int n = 0; n < moireCheckNum; n++)
                moire += moireValue[n];
            moire /= moireCheckNum;


            saveLog(string.Format("Moire : {0:F2}.", moire));

            if (moire > moireThreshold)
            {
                saveLog(string.Format("Moire is detected ({0:F2} > spec:{1:F2}).", moire, moireThreshold));
                throw new Exception(string.Format("Moire is detected ({0:F2} > spec:{1:F2}).\r\nPlease check Camera condition(Iris, SS, WD).", moire, moireThreshold));
            }
        }


        enum DispType { Before, Result, Measure};

        async private void dispGapResult(bool error = false)
        {
            if(error == true)
            {
                await Dispatcher.BeginInvoke(new Action(() => {
                    textboxGapCamBeforeJudge.Text = "NG";
                    textboxGapCamBeforeJudge.Background = System.Windows.Media.Brushes.DarkRed;
                    textboxGapCamBeforeJudge.Foreground = System.Windows.Media.Brushes.Red;
                }));
                await Dispatcher.BeginInvoke(new Action(() => {
                    textboxGapCamResultJudge.Text = "NG";
                    textboxGapCamResultJudge.Background = System.Windows.Media.Brushes.DarkRed;
                    textboxGapCamResultJudge.Foreground = System.Windows.Media.Brushes.Red;
                }));
                await Dispatcher.BeginInvoke(new Action(() => {
                    textboxGapCamMeasureJudge.Text = "NG";
                    textboxGapCamMeasureJudge.Background = System.Windows.Media.Brushes.DarkRed;
                    textboxGapCamMeasureJudge.Foreground = System.Windows.Media.Brushes.Red;
                }));
                return;
            }

            /**/
            //m_CabinetXNum = allocInfo.MaxX;
            //m_CabinetYNum = allocInfo.MaxY;
            /**/
            /*
            List<int> listX = new List<int>();
            List<int> listY = new List<int>();
            foreach (GapCamCorrectionValue cv in lstGapCamCp)
            {
                if (listX.Contains(cv.Unit.X) != true)
                    listX.Add(cv.Unit.X);
                if (listY.Contains(cv.Unit.Y) != true)
                    listY.Add(cv.Unit.Y);
            }
            m_CabinetXNum = listX.Count;
            m_CabinetYNum = listY.Count;
            */

            // modified by Hotta 2022/07/07
#if CorrectTargetEdge
            m_GapContrast = new double[m_CabinetXNum, m_CabinetYNum, m_ModuleXNum, m_ModuleYNum, (int)(CorrectPosition.LeftBottom + 1)];
            m_WarningCorrectValue = new bool[m_CabinetXNum, m_CabinetYNum, m_ModuleXNum, m_ModuleYNum, (int)(CorrectPosition.LeftBottom + 1)];
#else
            m_GapContrast = new double[m_CabinetXNum, m_CabinetYNum, m_ModuleXNum, m_ModuleYNum, (int)(CorrectPos.RightBottom + 1)];
            m_WarningCorrectValue = new bool[m_CabinetXNum, m_CabinetYNum, m_ModuleXNum, m_ModuleYNum, (int)(CorrectPos.RightBottom + 1)];
#endif

            // added by Hotta 2022/12/13
#if Spec_by_Zdistance
            m_GapContrastSpec = new double[m_CabinetXNum, m_CabinetYNum, m_ModuleXNum, m_ModuleYNum, (int)(CorrectPosition.LeftBottom + 1)];
#endif

            for (int y=0;y< m_CabinetYNum;y++)
            {
                for (int x = 0; x < m_CabinetXNum; x++)
                {
                    for (int _y = 0; _y < m_ModuleYNum; _y++)
                    {
                        for (int _x = 0; _x < m_ModuleXNum; _x++)
                        {
                            for (int pos = (int)CorrectPos.TopLeft; pos <= (int)CorrectPos.RightBottom; pos++)
                            {
                                m_GapContrast[x, y, _x, _y, pos] = double.NaN;
                                m_WarningCorrectValue[x, y, _x, _y, pos] = true;
                            }
                        }
                    }
                }
            }


            double max = double.MinValue, min = double.MaxValue, ave = 0, stddev = 0;
            double sum1 = 0, sum2 = 0;
            int count = 0;
            int ng_count = 0;
            string strNGList = "";


            // added by Hotta 2022/07/05
            int minX = int.MaxValue, minY = int.MaxValue;
            foreach (GapCamCorrectionValue gapCv in lstGapCamCp)
            {
                if (gapCv.Unit.X - 1 < minX)
                    minX = gapCv.Unit.X - 1;
                if (gapCv.Unit.Y - 1 < minY)
                    minY = gapCv.Unit.Y - 1;
            }

            foreach (GapCamCorrectionValue gapCv in lstGapCamCp)
            {
                // Cell
                foreach (GapCamCp cp in gapCv.lstCellCp)
                {
                    int cabiX = gapCv.Unit.X - 1;
                    int cabiY = gapCv.Unit.Y - 1;

                    // modified by Hotta 2022/07/05
                    /*
                    //PCLとりあえず
                    if (cabiX >= 10)
                        cabiX -= 10;
                    */
                    cabiX -= minX;
                    cabiY -= minY;


                    // modified by Hotta 2022/06/17 for bugfix
                    /*
                    int modX = (int)cp.CellNo % m_ModuleXNum;
                    int modY = (int)cp.CellNo / m_ModuleYNum;
                    */
                    int modX = (int)cp.CellNo % m_ModuleXNum;
                    int modY = (int)cp.CellNo / m_ModuleXNum;

                    m_GapContrast[cabiX, cabiY, modX, modY, (int)cp.Pos] = cp.GapGain - Settings.Ins.GapCam.TargetGain;

                    // added by Hotta 2022/12/13
#if Spec_by_Zdistance
                    m_GapContrastSpec[cabiX, cabiY, modX, modY, (int)cp.Pos] = m_Spec_by_Zdistance[cabiX + minX][cabiY + minY];
#endif

                    double k = (1.248 - 0.75) / 255;
                    if (Math.Abs(cp.RegAdj - cp.RegOrg) * k < 0.1)
                        m_WarningCorrectValue[cabiX, cabiY, modX, modY, (int)cp.Pos] = false;

                    if (max < m_GapContrast[cabiX, cabiY, modX, modY, (int)cp.Pos])
                        max = m_GapContrast[cabiX, cabiY, modX, modY, (int)cp.Pos];
                    if (min > m_GapContrast[cabiX, cabiY, modX, modY, (int)cp.Pos])
                        min = m_GapContrast[cabiX, cabiY, modX, modY, (int)cp.Pos];

                    sum1 += m_GapContrast[cabiX, cabiY, modX, modY, (int)cp.Pos];
                    sum2 += m_GapContrast[cabiX, cabiY, modX, modY, (int)cp.Pos] * m_GapContrast[cabiX, cabiY, modX, modY, (int)cp.Pos];
                    count++;

                    // modified by Hotta 2022/12/13
#if Spec_by_Zdistance
                    if (Math.Abs(m_GapContrast[cabiX, cabiY, modX, modY, (int)cp.Pos]) > m_GapContrastSpec[cabiX, cabiY, modX, modY, (int)cp.Pos])
#else
                    if(Math.Abs(m_GapContrast[cabiX, cabiY, modX, modY, (int)cp.Pos]) > Settings.Ins.GapCam.AdjustSpec)
#endif
                    {
                        ng_count++;

                        strNGList += (cabiX + 1).ToString() + "-" + (cabiY + 1).ToString() + "_" + (modY * m_ModuleXNum + modX + 1).ToString() + "-";
                        if (cp.Pos == CorrectPosition.TopLeft)
                            strNGList += "TL : ";
                        else if (cp.Pos == CorrectPosition.TopRight)
                            strNGList += "TR : ";
                        else if (cp.Pos == CorrectPosition.RightTop)
                            strNGList += "RT : ";
                        else if (cp.Pos == CorrectPosition.RightBottom)
                            strNGList += "RB : ";
                        // added by Hotta 2022/07/08
#if CorrectTargetEdge
                        else if (cp.Pos == CorrectPosition.BottomLeft)
                            strNGList += "BL : ";
                        else if (cp.Pos == CorrectPosition.BottomRight)
                            strNGList += "BR : ";
                        else if (cp.Pos == CorrectPosition.LeftTop)
                            strNGList += "LT : ";
                        else if (cp.Pos == CorrectPosition.LeftBottom)
                            strNGList += "LB : ";
#endif

                        strNGList += (m_GapContrast[cabiX, cabiY, modX, modY, (int)cp.Pos] * 100).ToString("+0.00;-0.00;0.00") + "%";
                        strNGList += "\n";
                    }
                }
            }
            if (ng_count == 0)
                strNGList = "None";
            else
            {
                strNGList = "NG count : " + ng_count.ToString() + "\n" + strNGList;
            }

            // modified by Hotta 2022/07/13
#if Coverity
            if (count != 0)
                ave = sum1 / count;
            else
                ave = sum1;

            double d = count - ave * ave;
            if (d != 0)
                stddev = Math.Sqrt(sum2 / d);
            else
                stddev = 0;

#else
            ave = sum1 / count;
            stddev = Math.Sqrt(sum2 / count - ave * ave);
#endif
            if (m_GapStatus == GapStatus.Before)
            {
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamMaxBefore.Text = (max * 100).ToString("+0.00;-0.00;0.00") + "%"; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamMinBefore.Text = (min * 100).ToString("+0.00;-0.00;0.00") + "%"; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamPPBefore.Text = ((max - min) * 100).ToString("0.00") + "%"; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCam3sigmaBefore.Text = ((3.0 * stddev) * 100).ToString("+0.00;-0.00;0.00") + "%"; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamAveBefore.Text = (ave * 100).ToString("+0.00;-0.00;0.00") + "%"; }));
                if (ng_count == 0)
                {
                    await Dispatcher.BeginInvoke(new Action(() => {
                        textboxGapCamBeforeJudge.Text = "OK";
                        textboxGapCamBeforeJudge.Background = System.Windows.Media.Brushes.DarkGreen;
                        textboxGapCamBeforeJudge.Foreground = System.Windows.Media.Brushes.Lime;
                    }));
                }
                else
                {
                    await Dispatcher.BeginInvoke(new Action(() => {
                        textboxGapCamBeforeJudge.Text = "NG";
                        textboxGapCamBeforeJudge.Background = System.Windows.Media.Brushes.DarkRed;
                        textboxGapCamBeforeJudge.Foreground = System.Windows.Media.Brushes.Red;
                    }));
                }
            }
            else if(m_GapStatus == GapStatus.Result)
            {
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamMaxResult.Text = (max * 100).ToString("+0.00;-0.00;0.00") + "%"; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamMinResult.Text = (min * 100).ToString("+0.00;-0.00;0.00") + "%"; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamPPResult.Text = ((max - min) * 100).ToString("0.00") + "%"; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCam3sigmaResult.Text = ((3.0 * stddev) * 100).ToString("+0.00;-0.00;0.00") + "%"; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamAveResult.Text = (ave * 100).ToString("+0.00;-0.00;0.00") + "%"; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamNGlistResult.Text = strNGList; }));
                if (ng_count == 0)
                {
                    await Dispatcher.BeginInvoke(new Action(() => {
                        textboxGapCamResultJudge.Text = "OK";
                        textboxGapCamResultJudge.Background = System.Windows.Media.Brushes.DarkGreen;
                        textboxGapCamResultJudge.Foreground = System.Windows.Media.Brushes.Lime;
                    }));
                }
                else
                {
                    await Dispatcher.BeginInvoke(new Action(() => {
                        textboxGapCamResultJudge.Text = "NG";
                        textboxGapCamResultJudge.Background = System.Windows.Media.Brushes.DarkRed;
                        textboxGapCamResultJudge.Foreground = System.Windows.Media.Brushes.Red;
                    }));
                }
            }
            else
            {
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamMaxMeasure.Text = (max * 100).ToString("+0.00;-0.00;0.00") + "%"; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamMinMeasure.Text = (min * 100).ToString("+0.00;-0.00;0.00") + "%"; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamPPMeasure.Text = ((max - min) * 100).ToString("0.00") + "%"; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCam3sigmaMeasure.Text = ((3.0 * stddev) * 100).ToString("+0.00;-0.00;0.00") + "%"; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamAveMeasure.Text = (ave * 100).ToString("+0.00;-0.00;0.00") + "%"; }));
                if (ng_count == 0)
                {
                    await Dispatcher.BeginInvoke(new Action(() => {
                        textboxGapCamMeasureJudge.Text = "OK";
                        textboxGapCamMeasureJudge.Background = System.Windows.Media.Brushes.DarkGreen;
                        textboxGapCamMeasureJudge.Foreground = System.Windows.Media.Brushes.Lime;
                    }));
                }
                else
                {
                    await Dispatcher.BeginInvoke(new Action(() => {
                        textboxGapCamMeasureJudge.Text = "NG";
                        textboxGapCamMeasureJudge.Background = System.Windows.Media.Brushes.DarkRed;
                        textboxGapCamMeasureJudge.Foreground = System.Windows.Media.Brushes.Red;
                    }));
                }
            }


            Bitmap canvas = makeResultImage();
            // deleted by Hotta 2023/03/03
            /*
            Mat mat;
            */

            if (m_GapStatus == GapStatus.Before)
            {
                canvas.Save(m_CamMeasPath + "\\GapBefore.bmp");
#if NoEncript
#else
                mat = new Mat(m_CamMeasPath + "\\GapBefore.bmp");
                SaveMatBinary(mat, m_CamMeasPath + "\\GapBefore");
                mat.Dispose();
#endif
            }
            else if(m_GapStatus == GapStatus.Result)
            {
                canvas.Save(m_CamMeasPath + "\\GapResult.bmp");
#if NoEncript
#else
                mat = new Mat(m_CamMeasPath + "\\GapResult.bmp");
                SaveMatBinary(mat, m_CamMeasPath + "\\GapResult");
                mat.Dispose();
#endif
            }
            else if (m_GapStatus == GapStatus.Measure)
            {
                canvas.Save(m_CamMeasPath + "\\GapMeasure.bmp");
#if NoEncript
#else
                mat = new Mat(m_CamMeasPath + "\\GapMeasure.bmp");
                SaveMatBinary(mat, m_CamMeasPath + "\\GapMeasure");
                mat.Dispose();
#endif
            }

            System.Windows.Media.Imaging.BitmapSource bitmapSource;

            // MemoryStreamを利用した変換処理
            using (var ms = new System.IO.MemoryStream())
            {
                // MemoryStreamに書き出す
                canvas.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                // MemoryStreamをシーク
                ms.Seek(0, System.IO.SeekOrigin.Begin);
                // MemoryStreamからBitmapFrameを作成
                // (BitmapFrameはBitmapSourceを継承しているのでそのまま渡せばOK)
                bitmapSource =
                    System.Windows.Media.Imaging.BitmapFrame.Create(
                        ms,
                        System.Windows.Media.Imaging.BitmapCreateOptions.None,
                        System.Windows.Media.Imaging.BitmapCacheOption.OnLoad
                    );
            }

            if(m_GapStatus == GapStatus.Before)
                await Dispatcher.BeginInvoke(new Action(() => { imgGapCamBefore.Source = bitmapSource; }));
            else if(m_GapStatus == GapStatus.Result)
                await Dispatcher.BeginInvoke(new Action(() => { imgGapCamResult.Source = bitmapSource; }));
            else
                await Dispatcher.BeginInvoke(new Action(() => { imgGapCamMeasure.Source = bitmapSource; }));

        }


        async private void clearGapResult(DispType type)
        {
            if (type == DispType.Before)
            {
                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    textboxGapCamBeforeJudge.Text = "";
                    textboxGapCamBeforeJudge.Background = System.Windows.Media.Brushes.Gray;
                    textboxGapCamBeforeJudge.Foreground = System.Windows.Media.Brushes.Gray;
                }));
                    
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamMaxBefore.Text = ""; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamMinBefore.Text = ""; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamPPBefore.Text = ""; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCam3sigmaBefore.Text = ""; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamAveBefore.Text = ""; }));
            }
            else if(type == DispType.Result)
            {
                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    textboxGapCamResultJudge.Text = "";
                    textboxGapCamResultJudge.Background = System.Windows.Media.Brushes.Gray;
                    textboxGapCamResultJudge.Foreground = System.Windows.Media.Brushes.Gray;
                }));

                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamMaxResult.Text = ""; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamMinResult.Text = ""; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamPPResult.Text = ""; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCam3sigmaResult.Text = ""; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamAveResult.Text = ""; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamNGlistResult.Text = ""; }));
            }
            else
            {
                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    textboxGapCamMeasureJudge.Text = "";
                    textboxGapCamMeasureJudge.Background = System.Windows.Media.Brushes.Gray;
                    textboxGapCamMeasureJudge.Foreground = System.Windows.Media.Brushes.Gray;
                }));

                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamMaxMeasure.Text = ""; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamMinMeasure.Text = ""; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamPPMeasure.Text = ""; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCam3sigmaMeasure.Text = ""; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamAveMeasure.Text = ""; }));
                await Dispatcher.BeginInvoke(new Action(() => { txbGapCamNGlistMeasure.Text = ""; }));
            }

            Bitmap canvas = clearResultImage();

            System.Windows.Media.Imaging.BitmapSource bitmapSource;

            // MemoryStreamを利用した変換処理
            using (var ms = new System.IO.MemoryStream())
            {
                // MemoryStreamに書き出す
                canvas.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                // MemoryStreamをシーク
                ms.Seek(0, System.IO.SeekOrigin.Begin);
                // MemoryStreamからBitmapFrameを作成
                // (BitmapFrameはBitmapSourceを継承しているのでそのまま渡せばOK)
                bitmapSource =
                    System.Windows.Media.Imaging.BitmapFrame.Create(
                        ms,
                        System.Windows.Media.Imaging.BitmapCreateOptions.None,
                        System.Windows.Media.Imaging.BitmapCacheOption.OnLoad
                    );
            }

            if (type == DispType.Before)
                await Dispatcher.BeginInvoke(new Action(() => { imgGapCamBefore.Source = bitmapSource; }));
            else if(type == DispType.Result)
                await Dispatcher.BeginInvoke(new Action(() => { imgGapCamResult.Source = bitmapSource; }));
            else
                await Dispatcher.BeginInvoke(new Action(() => { imgGapCamMeasure.Source = bitmapSource; }));
        }


#endregion Measurement(Gap)

#region Adjust

        int m_AdjustCount;

        unsafe private void adjustGapRegAsync(List<UnitInfo> lstTgtUnit)
        {
            winProgress.ShowMessage("Start Adjustment Gap.");

            AcqARW arw;
            Mat mat;
            int width, height;
            ushort* pMat;

            bool status = true;

            m_GapStatus = GapStatus.Before;
            m_AdjustCount = 0;

            clearGapResult(DispType.Before);
            clearGapResult(DispType.Result);

#if NO_CAP
            Thread.Sleep(1000);
#endif

            // added by Hotta 2022/12/15
            int startX = int.MaxValue;
            int endX = int.MinValue;
            int startY = int.MaxValue;
            int endY = int.MinValue;
            foreach (UnitInfo unit in lstTgtUnit)
            {
                if (startX > unit.X)
                    startX = unit.X;
                if (endX < unit.X)
                    endX = unit.X;
                if (startY > unit.Y)
                    startY = unit.Y;
                if (endY < unit.Y)
                    endY = unit.Y;
            }
            saveLog(string.Format("Target Cabinet X : {0} - {1}, Y : {2} - {3}", startX, endX, startY, endY));
            //

            // 全体のStep数を設定
            winProgress.SetWholeSteps(64);

            // OpenCVSharpのDllがあるか確認する
            CheckOpenCvSharpDll();
            // 信号レベルの設定
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
                m_MeasureLevel = Settings.Ins.GapCam.MeasLevel_BModel;
            }

            // カメラパラメータの設定
            if (Settings.Ins.Camera.Name == "ILCE-6400")
                m_ShootCondition = Settings.Ins.GapCam.Setting_A6400;
            else
                m_ShootCondition = Settings.Ins.GapCam.Setting_A7;

            List<int> listX = new List<int>();
            List<int> listY = new List<int>();
            foreach (UnitInfo unit in lstTgtUnit)
            {
                if (listX.Contains(unit.X) != true)
                    listX.Add(unit.X);
                if (listY.Contains(unit.Y) != true)
                    listY.Add(unit.Y);
            }
            m_CabinetXNum = listX.Count;
            m_CabinetYNum = listY.Count;
            if (allocInfo.LEDModel == ZRD_C12A 
                || allocInfo.LEDModel == ZRD_B12A 
                || allocInfo.LEDModel == ZRD_CH12D 
                || allocInfo.LEDModel == ZRD_BH12D
                || allocInfo.LEDModel == ZRD_CH12D_S3
                || allocInfo.LEDModel == ZRD_BH12D_S3)
            {
                // deleted by Hotta 2023/03/03
                /*
                m_LedPitch = (float)RealPitch_P12;
                */
                // added by Hotta 2023/04/28
#if NO_CONTROLLER
                m_LedPitch = (float)RealPitch_P12;
#endif
                m_CabinetDx = CabinetDxP12;
                m_CabinetDy = CabinetDyP12;

                if (allocInfo.LEDModel == ZRD_C12A 
                    || allocInfo.LEDModel == ZRD_B12A)
                {
                    m_ModuleDx = ModuleDxP12_Mdoule4x2;
                    m_ModuleDy = ModuleDyP12_Module4x2;
                }
                else
                {
                    m_ModuleDx = ModuleDxP12_Module4x3;
                    m_ModuleDy = ModuleDyP12_Module4x3;
                }
            }
            else if (allocInfo.LEDModel == ZRD_C15A 
                || allocInfo.LEDModel == ZRD_B15A 
                || allocInfo.LEDModel == ZRD_CH15D 
                || allocInfo.LEDModel == ZRD_BH15D
                || allocInfo.LEDModel == ZRD_CH15D_S3
                || allocInfo.LEDModel == ZRD_BH15D_S3)
            {
                // deleted by Hotta 2023/03/03
                /*
                m_LedPitch = (float)RealPitch_P15;
                */
                // added by Hotta 2023/04/28
#if NO_CONTROLLER
                m_LedPitch = (float)RealPitch_P15;
#endif
                m_CabinetDx = CabinetDxP15;
                m_CabinetDy = CabinetDyP15;

                if (allocInfo.LEDModel == ZRD_C15A 
                    || allocInfo.LEDModel == ZRD_B15A)
                {
                    m_ModuleDx = ModuleDxP15_Module4x2;
                    m_ModuleDy = ModuleDyP15_Module4x2;
                }
                else
                {
                    m_ModuleDx = ModuleDxP15_Module4x3;
                    m_ModuleDy = ModuleDyP15_Module4x3;
                }
            }
            else
            { 
                return;
            }

            m_ModuleXNum = m_CabinetDx / m_ModuleDx;
            m_ModuleYNum = m_CabinetDy / m_ModuleDy;

            m_PanelDx = m_CabinetDx * m_CabinetXNum;
            m_PanelDy = m_CabinetDy * m_CabinetYNum;


#if NO_CONTROLLER

#else
            // added by Hotta 2022/12/19 for 対象コントローラの取得
            foreach (ControllerInfo controller in dicController.Values)
                controller.Target = false;
            outputGapCamTargetArea_EdgeExpand(lstTgtUnit, ExpandType.Both, true);
            // added by Hotta 2023/01/06
            // 画質設定するコントローラは、補正対象Cabinetだけでなく、カメラ位置合わせのCabinetに接続されているものも対象になる
            outputGapCamTargetArea_EdgeExpand(m_lstCamPosUnits, ExpandType.Both, true);
            //

            Thread.Sleep(Settings.Ins.Camera.PatternWait);
            //

            // added by Hotta 2024/12/25
            //CheckUserAbort();


            // Cabinet on
            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdUnitPowerOn, cont.IPAddress); }
            Thread.Sleep(5000);






            // カメラの目標位置を再設定
            //SetTargetCamPos(lstTgtUnit);

            // ユーザー設定を保存 1Step
            List<UserSetting> lstUserSettings;
            winProgress.ShowMessage("Store User Settings.");
            winProgress.PutForward1Step();
            saveLog("Store User Settings.");
            // modified by Hotta 2023/01/23
            /*
            getUserSetting(out lstUserSettings);
            */
            status = getUserSetting(out lstUserSettings);
            if(status == true)
                m_lstUserSetting = lstUserSettings;
            //

            // 調整用設定 1Step
            winProgress.ShowMessage("Set Adjust Settings.");
            winProgress.PutForward1Step();
            saveLog("Set Adjust Settings.");
            setAdjustSetting();

            // added by Hotta 2022/08/08
            // Layout info : off
            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdDispUnitAddrOff, 0, cont.IPAddress); }
#endif

            int processSec = calcGapCameraAdjustmentProcessSec(1);
            winProgress.SetRemainTimer(processSec);

#if NO_CAP
#else
            // AutoFocus
            winProgress.ShowMessage("Auto focus.");
            winProgress.PutForward1Step();
            saveLog("Auto focus.");
            outputIntSigChecker(0, 0, modDy, modDx, modDx, modDy, m_MeasureLevel, m_MeasureLevel, m_MeasureLevel); // 20IRE

            // added by Hotta 2022/07/26
            Thread.Sleep(Settings.Ins.Camera.PatternWait);
            //

            /*
            saveLog(string.Format("CameraSetting:{0},{1},{2},{3},{4},{5}",
                Settings.Ins.Camera.SetPosSetting.FNumber,
                Settings.Ins.Camera.SetPosSetting.Shutter,
                Settings.Ins.Camera.SetPosSetting.ISO,
                Settings.Ins.Camera.SetPosSetting.WB,
                Settings.Ins.Camera.SetPosSetting.CompressionType,
                Settings.Ins.Camera.SetPosSetting.ImageSize));
            AutoFocus(Settings.Ins.Camera.SetPosSetting);
            */
            saveLog(string.Format("CameraSetting:{0},{1},{2},{3},{4},{5}",
                m_ShootCondition.FNumber,
                m_ShootCondition.Shutter,
                m_ShootCondition.ISO,
                m_ShootCondition.WB,
                m_ShootCondition.CompressionType,
                m_ShootCondition.ImageSize));
            //AutoFocus(m_ShootCondition);
            AutoFocus(m_ShootCondition, new AfAreaSetting());

            // added by Hotta 2022/07/26
            if (File.Exists(m_CamMeasPath + "AutoFocus.arw") == true)
            {
                if (checkFileSize(m_CamMeasPath + "AutoFocus.arw") != true)
                {
                    throw new Exception("Saving the " + m_CamMeasPath + "AutoFocus.arw" + " file does not completed.");
                }

                try
                {
                    loadArwFile(m_CamMeasPath + "AutoFocus.arw", out arw);
                }
                // added by Hotta 2025/01/31
                catch (CameraCasUserAbortException ex)
                {
                    throw ex;
                }
                //
                catch //(Exception ex)
                {
                    Thread.Sleep(1000);
                    loadArwFile(m_CamMeasPath + "AutoFocus.arw", out arw);
                }
                width = arw.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
                height = arw.RawMainIFD.ImageHeight / 2;
                mat = new Mat(new Size(width, height), MatType.CV_16UC1);
                pMat = (ushort*)(mat.Data);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        pMat[y * width + x] = arw.RawData[1][x, y];
                    }
                }
                try
                {
                    SaveMatBinary(mat, m_CamMeasPath + "AutoFocus");
                }
                catch //(Exception ex)
                {
                    Thread.Sleep(1000);
                    SaveMatBinary(mat, m_CamMeasPath + "AutoFocus");
                }
                mat.Dispose();
                arw = null;
            }
            //
#endif

            processSec = calcGapCameraAdjustmentProcessSec(2);
            winProgress.SetRemainTimer(processSec);

            // 開始時のカメラ位置を保存 1Step
            winProgress.ShowMessage("Store Camera Position.");
            winProgress.PutForward1Step();
            saveLog("Store Camera Position.");

            // modified by Hotta 2023/01/30
            /*
            // modified by Hotta 2022/12/15
            // added by Hotta 2022/08/02
            // SetCamPosTarget(true);
            //
            if (allocInfo.LEDModel == ZRD_C15A || allocInfo.LEDModel == ZRD_B15A || allocInfo.LEDModel == ZRD_CH15D || allocInfo.LEDModel == ZRD_BH15D)
                SetCamPosTarget(ImageType_CamPos.LiveView, true, m_lstCamPosUnits, 8000.0);
            else
                SetCamPosTarget(ImageType_CamPos.LiveView, true, m_lstCamPosUnits, 8000.0 * (1.26 / 1.58));
            */
            if (allocInfo.LEDModel == ZRD_C15A
                || allocInfo.LEDModel == ZRD_B15A
                || allocInfo.LEDModel == ZRD_CH15D
                || allocInfo.LEDModel == ZRD_BH15D
                || allocInfo.LEDModel == ZRD_CH15D_S3
                || allocInfo.LEDModel == ZRD_BH15D_S3)
            {
                SetCamPosTarget(ImageType_CamPos.LiveView, true, lstTgtUnit, 8000.0);
            }
            else
            {
                SetCamPosTarget(ImageType_CamPos.LiveView, true, lstTgtUnit, 8000.0 * (1.26 / 1.58));
            }

            status = true;
            for (int retry = 0; retry < 3; retry++)
            {
                status = GetCameraPosition(imgGapCamMeasure);
                saveLog(string.Format("X={0:F2},Y={1:F2},Z={2:F2},Pan={3:F2},Tilt={4:F2},Roll={5:F2}", m_CamPos_Tx, m_CamPos_Ty, m_CamPos_Tz, m_CamPos_Pan, m_CamPos_Tilt, m_CamPos_Roll));
                saveLog(string.Format("Top={0:F1},Bottom={1:F1},Left={2:F1},Right={3:F1}", m_CamPos_TopLen, m_CamPos_BottomLen, m_CamPos_LeftLen, m_CamPos_RightLen));
                if (status == true)
                    break;
            }
            if (status != true)
            {
                saveLog("The camera position is inappropriate.");
                if (appliMode != ApplicationMode.Developer)
                    throw new Exception("The camera position is inappropriate.\r\nPlease align the camera position again.");
            }

            processSec = calcGapCameraAdjustmentProcessSec(3);
            winProgress.SetRemainTimer(processSec);

            winProgress.ShowMessage("Initialize correction data");
            winProgress.PutForward1Step();
            saveLog("Initialize correction data.");
#if NO_CONTROLLER
#else
            // 補正値の初期化
            outputIntSigFlat(m_MeasureLevel, m_MeasureLevel, m_MeasureLevel);

            // added by Hotta 2022/06/24
            // 目地補正の無効化
            setGapCorrection(false);

            // deleted by Hotta 2022/06/24
            // 無地補正スイッチを無効化したため、実際の補正の初期化をやめる
            /*
            // 全てのCabinetを選択
            if (allocInfo.MaxX * allocInfo.MaxY == lstTgtUnit.Count)
            {
                setAllGapCorrectValue(lstTgtUnit[0], 128);
            }
            // 一部のCabinetを選択
            else
            {
                // 調整点の格納 ※矩形前提
                int StartUnitX = int.MaxValue, StartUnitY = int.MaxValue; // 対象Unitの左上のユニット位置（1ベース）
                int EndUnitX = 0, EndUnitY = 0;

                // Unitの位置を調査
                foreach (UnitInfo unit in lstTgtUnit)
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

                //int LenX = EndUnitX - StartUnitX + 1; // 対象UnitのX方向Unit数
                //int LenY = EndUnitY - StartUnitY + 1; // 対象UnitのY方向Unit数

                // modified by Hotta 2022/06/16 for 分割補正
                // ±0プリセットは、端であるかどうかにかかわらず、全て行う。
                //
                //foreach (UnitInfo unit in lstTgtUnit)
                //{
                //    for (CellNum cell = CellNum.Cell_0; cell <= CellNum.Cell_7; cell++)
                //    {
                //        for (EdgePosition edge = EdgePosition.Edge_1; edge <= EdgePosition.Edge_8; edge++)
                //        {
                //            // 上端
                //            // EdgePosition.Edge_1 : cv.TopLeft
                //            // EdgePosition.Edge_2 : cv.TopRight
                //            if (unit.Y == StartUnitY)
                //            {
                //                if (CellNum.Cell_0 <= cell && cell <= CellNum.Cell_3)
                //                {
                //                    if (edge == EdgePosition.Edge_1 || edge == EdgePosition.Edge_2)
                //                        continue;
                //                }
                //            }
                //            // 下端
                //            // EdgePosition.Edge_7 : cv.BottomLeft
                //            // EdgePosition.Edge_8 : cv.BottomRight
                //            if (unit.Y == EndUnitY)
                //            {
                //                if (CellNum.Cell_4 <= cell && cell <= CellNum.Cell_7)
                //                {
                //                    if (edge == EdgePosition.Edge_7 || edge == EdgePosition.Edge_8)
                //                        continue;
                //                }
                //            }
                //            // 左端
                //            // EdgePosition.Edge_3 : cv.LeftTop
                //            // EdgePosition.Edge_5 : cv.LeftBottom
                //            if (unit.X == StartUnitX)
                //            {
                //                if (cell == CellNum.Cell_0 || cell == CellNum.Cell_4)
                //                {
                //                    if (edge == EdgePosition.Edge_3 || edge == EdgePosition.Edge_5)
                //                        continue;
                //                }
                //            }
                //            // 右端
                //            // EdgePosition.Edge_4 : cv.RightTop
                //            // EdgePosition.Edge_6 : cv.RightBottom
                //            if (unit.X == EndUnitX)
                //            {
                //                if (cell == CellNum.Cell_3 || cell == CellNum.Cell_7)
                //                {
                //                    if (edge == EdgePosition.Edge_4 || edge == EdgePosition.Edge_6)
                //                        continue;
                //                }
                //            }
                //            setGapCvCellEdge(unit, (int)(cell + 1), edge, 128); // cellは1ベース

                //        }
                //    }
                //}
                //
                foreach (UnitInfo unit in lstTgtUnit)
                {
                    // modified by Hootta 2022/06/17
                    //
                    //for (CellNum cell = CellNum.Cell_0; cell <= CellNum.Cell_7; cell++)
                    //{
                    //    for (EdgePosition edge = EdgePosition.Edge_1; edge <= EdgePosition.Edge_8; edge++)
                    //    {
                    //        setGapCvCellEdge(unit, (int)(cell + 1), edge, 128); // cellは1ベース
                    //    }
                    //}
                    //
                    for (int cell=0;cell<m_ModuleXNum * m_ModuleYNum;cell++)
                    {
                        for (EdgePosition edge = EdgePosition.Edge_1; edge <= EdgePosition.Edge_8; edge++)
                        {
                            setGapCvCellEdge(unit, cell + 1, edge, 128); // cellは1ベース
                        }
                    }
                }
                //
            }
            */
#endif

            // この時点で、全ての補正値は±0（128）が設定される
            lstGapCamCp = new List<GapCamCorrectionValue>();

            // 撮影
            captureGapImages(m_CamMeasPath); // 11step

            processSec = calcGapCameraAdjustmentProcessSec(4);
            winProgress.SetRemainTimer(processSec);

            // 処理
            calcGapGain(lstTgtUnit, m_CamMeasPath); // 6step

            // added by Hotta 2025/01/09
            // ESCキー押下による中断を無効化
            winProgress.AbortType = WindowProgress.TAbortType.None;
            //

            // 補正値関係以外をクリアしたインスタンス作成
            List<GapCamCorrectionValue> _lstGapCamCp = new List<GapCamCorrectionValue>();
            for (int n = 0; n < lstGapCamCp.Count; n++)
            {
                _lstGapCamCp.Add(new GapCamCorrectionValue(m_ModuleXNum * m_ModuleYNum));
                _lstGapCamCp[n].Unit = lstGapCamCp[n].Unit;
                // added by Hotta 2022/11/10 for Cabinet補正値なし
#if No_CabinetCorrectionValue

#else
                _lstGapCamCp[n].CvUnit = lstGapCamCp[n].CvUnit;
#endif
                _lstGapCamCp[n].AryCvCell = new GapCellCorrectValue[lstGapCamCp[n].AryCvCell.Length];
                for (int i = 0; i < lstGapCamCp[n].AryCvCell.Length; i++)
                {
                    _lstGapCamCp[n].AryCvCell[i] = lstGapCamCp[n].AryCvCell[i];
                }
            }

            // 測定結果を保存
            //GapCamCorrectionValue.SaveToXmlFile(measPath + "GapNeutralResult.xml", lstGapCamCp); // 1step
            GapCamCorrectionValue.SaveToXmlFile(m_CamMeasPath + "GapBeforeResult.xml", _lstGapCamCp); // 1step

            // added by Hotta 2022/08/30
            // 見栄えをよくするため、NGが多いMeasurement画像を表示する前に、タブを切り替えておく
            // タブの表示ページを切り替え
            Dispatcher.Invoke(new Action(() => { tcGapCamView.SelectedIndex = 3; }));
            //

            // 表示
            dispGapResult();

            // added by Hotta 2022/06/24
            // 目地補正の有効化
#if NO_CONTROLLER
            Thread.Sleep(1000);
#else
            setGapCorrection(true);
#endif

            processSec = calcGapCameraAdjustmentProcessSec(5);
            winProgress.SetRemainTimer(processSec);

            // タブの表示ページを切り替え
            Dispatcher.Invoke(new Action(() => { tcGapCamView.SelectedIndex = 3; }));

            // modified by Hotta 2022/07/08
#if CorrectTargetEdge
            // added by Hotta 2022/10/28
#if MultiController
            outputGapCamTargetArea_EdgeExpand(lstTgtUnit, ExpandType.Both, true);
#else
            outputGapCamTargetArea_EdgeExpand(lstTgtUnit, true);
#endif
#else
            outputGapCamTargetArea(lstTgtUnit, true);
#endif
            lstModifiedUnits = new List<UnitInfo>();

            bool completeFlag = true;

            for (m_AdjustCount = 0; m_AdjustCount < m_MaxNumOfAdjustment; m_AdjustCount++)
            {
                // modified by Hotta 2022/08/30
                /*
                winProgress.SetWholeSteps(lstGapCamCp.Count);
                */
                int stepNum = lstGapCamCp.Count;
                if (m_GapStatus == GapStatus.Before)
                    stepNum += lstGapCamCp.Count * 2;
                winProgress.SetWholeSteps(stepNum);

                completeFlag = true;

                foreach (GapCamCorrectionValue cv in lstGapCamCp)
                {
                    foreach (GapCamCp cp in cv.lstCellCp)
                    {
                        // modified by Hotta 2022/12/13
#if Spec_by_Zdistance
                        if (Math.Abs(cp.GapGain - 1.0) > m_Spec_by_Zdistance[cv.Unit.X - 1][cv.Unit.Y - 1])
                        {
                            completeFlag = false;
                            break;
                        }
#else
                        if (Math.Abs(cp.GapGain - 1.0) > Settings.Ins.GapCam.AdjustSpec)
                        {
                            completeFlag = false;
                            break;
                        }
#endif
                    }
                }

                if (m_AdjustCount != 0)
                {
                    if (completeFlag == true)
                    {
                        saveLog("All gap correction is in spec.");
                        break;
                    }

                    // added by Tei 2024/12/25
                    int outSpecCount = 0;
                    foreach (GapCamCorrectionValue cv in lstGapCamCp)
                    {
                        foreach (GapCamCp cp in cv.lstCellCp)
                        {
#if Spec_by_Zdistance
                            if (Math.Abs(cp.GapGain - 1.0) > m_Spec_by_Zdistance[cv.Unit.X - 1][cv.Unit.Y - 1] * 0.5)
                            {
                                outSpecCount++;

                                EdgePosition targetEdge = EdgePosition.NA;
                                if (cp.Pos == CorrectPosition.TopLeft)
                                { targetEdge = EdgePosition.Edge_1; }
                                else if (cp.Pos == CorrectPosition.TopRight)
                                { targetEdge = EdgePosition.Edge_2; }
                                else if (cp.Pos == CorrectPosition.RightTop)
                                { targetEdge = EdgePosition.Edge_4; }
                                else if (cp.Pos == CorrectPosition.RightBottom)
                                { targetEdge = EdgePosition.Edge_6; }
                                else if (cp.Pos == CorrectPosition.LeftTop)
                                { targetEdge = EdgePosition.Edge_3; }
                                else if (cp.Pos == CorrectPosition.LeftBottom)
                                { targetEdge = EdgePosition.Edge_5; }
                                else if (cp.Pos == CorrectPosition.BottomLeft)
                                { targetEdge = EdgePosition.Edge_7; }
                                else if (cp.Pos == CorrectPosition.BottomRight)
                                { targetEdge = EdgePosition.Edge_8; }

                                getNextCell(cv.Unit, (int)cp.CellNo + 1, targetEdge, out UnitInfo nextUnit, out _, out _);

                                if (nextUnit != null)
                                { outSpecCount++; }
                            }
#else
                            if (Math.Abs(cp.GapGain - 1.0) > Settings.Ins.GapCam.AdjustSpec * 0.5)
                            {
                                outSpecCount++;

                                EdgePosition targetEdge = EdgePosition.NA;
                                if (cp.Pos == CorrectPosition.TopLeft)
                                { targetEdge = EdgePosition.Edge_1; }
                                else if (cp.Pos == CorrectPosition.TopRight)
                                { targetEdge = EdgePosition.Edge_2; }
                                else if (cp.Pos == CorrectPosition.RightTop)
                                { targetEdge = EdgePosition.Edge_4; }
                                else if (cp.Pos == CorrectPosition.RightBottom)
                                { targetEdge = EdgePosition.Edge_6; }
                                else if (cp.Pos == CorrectPosition.LeftTop)
                                { targetEdge = EdgePosition.Edge_3; }
                                else if (cp.Pos == CorrectPosition.LeftBottom)
                                { targetEdge = EdgePosition.Edge_5; }
                                else if (cp.Pos == CorrectPosition.BottomLeft)
                                { targetEdge = EdgePosition.Edge_7; }
                                else if (cp.Pos == CorrectPosition.BottomRight)
                                { targetEdge = EdgePosition.Edge_8; }

                                getNextCell(cv.Unit, (int)cp.CellNo + 1, targetEdge, out UnitInfo nextUnit, out _, out _);

                                if (nextUnit != null)
                                { outSpecCount++; }
                            }
#endif
                        }
                    }

                    saveLog("OutSpecCount: " + outSpecCount);
                    processSec = calcGapCameraAdjustmentProcessSec(GAP_RE_CALC_STEP_5_PROCESS_SEC, outSpecCount);
                    winProgress.SetRemainTimer(processSec);
                }

                saveLog("Set Gap correction value.");

                //-----------------------------------------------------------------------------------------
                // moved by Hotta 2022/07/11
                // 先のループ回で設定した補正値を、ここでリセットしてしまうため、ループ前に移動
                // 1回目のAdjustのときは、補正値を読み出す。m_GapStatus:Before:1回目のAdjustと同意
                if (m_GapStatus == GapStatus.Before)
                {
                    // deleted by Hotta 2022/08/30
                    /*
                    winProgress.SetWholeSteps(lstGapCamCp.Count);
                    winProgress.SetMarquee();
                    */

                    foreach (GapCamCorrectionValue cv in lstGapCamCp)
                    {
                        string progress = "Get original value. (Cabinet : " + cv.Unit.ControllerID + "-" + cv.Unit.PortNo + "-" + cv.Unit.UnitNo + ")";
                        // 現在のReg値を取得
                        // Unit
                        winProgress.ShowMessage(progress + "\r\nGet Cabinet Correction Value.");
                        winProgress.PutForward1Step();
                        //saveLog(progress + "\r\nGet Cabinet Correction Value."); 
#if NO_CONTROLLER
#else
                        // added by Hotta 2022/11/10 for Cabinet補正値なし
#if No_CabinetCorrectionValue

#else
                        // xmlファイル作成のため、あえて実行する
                        getGapCvUnit(cv.Unit, ref cv.CvUnit);
#endif

#endif
                        // Module
                        for (int cell = 0; cell < moduleCount; cell++)
                        {
                            // ±0（128）を設定する
                            getGapCvCell_InitializedValue(cv.Unit, cell + 1, ref cv.AryCvCell[cell]);  // +1 : Cell番号指定は1ベース
                        }
                    }
                }
                //-----------------------------------------------------------------------------------------

                // deleted by Hotta 2022/08/30
                /*
                winProgress.SetWholeSteps(lstGapCamCp.Count);
                winProgress.SetMarquee();
                */

                foreach (GapCamCorrectionValue cv in lstGapCamCp)
                {
                    string progress = "Set(single) Gap Correction. (Cabinet : " + cv.Unit.ControllerID + "-" + cv.Unit.PortNo + "-" + cv.Unit.UnitNo + ")";
                    winProgress.ShowMessage(progress);
                    //winProgress.PutForward1Step();
                    saveLog(progress);

                    // deleted by Hotta 2023/03/03
                    /*
                    int step = 0;
                    */

                    // Unitの最大進捗数
                    //int unitSteps = 1 + 12 + cv.lstUnitCp.Count + cv.lstCellCp.Count;

                    //-----------------------------------------------------------------------------------------
                    // moved by Hotta 2022/07/11
                    // 先のループ回で設定した補正値を、ここでリセットしてしまうため、ループ前に移動
                    /*
                    // 現在のReg値を取得
                    // Unit
                    winProgress.ShowMessage(progress + "\r\nGet Cabinet Correction Value.");
                    winProgress.SetPartProgress(step++ * 100 / unitSteps);
                    //saveLog(progress + "\r\nGet Cabinet Correction Value."); 

                    // 1回目のAdjustのときは、補正値を読み出す。m_GapStatus:Before:1回目のAdjustと同意
                    if (m_GapStatus == GapStatus.Before)
                    {
#if NO_CONTROLLER
#else
                        // xmlファイル作成のため、あえて実行する
                        getGapCvUnit(cv.Unit, ref cv.CvUnit);
#endif
                        // Module
                        for (int cell = 0; cell < moduleCount; cell++)
                        {
                            winProgress.ShowMessage(progress + "\r\nGet Module Correction Value. (Module-" + (cell + 1) + ")");
                            winProgress.SetPartProgress(step++ * 100 / unitSteps);
                            //saveLog(progress + "\r\nGet Module Correction Value. (Module-" + (cell + 1) + ")");

                            // ±0（128）を設定する
                            getGapCvCell_InitializedValue(cv.Unit, cell + 1, ref cv.AryCvCell[cell]);  // +1 : Cell番号指定は1ベース
                        }
                    }
                    */
                    //-----------------------------------------------------------------------------------------


                    // deleted by Hotta 2021/11/17
                    // Unitは補正しない
                    /*
                    // Unit Gap
                    foreach (GapCamCp cp in cv.lstUnitCp)
                    {
                        winProgress.ShowMessage(progress + "\r\nSet Unit Correction Value. (" + cp.Pos + ")");
                        winProgress.SetPartProgress(step++ * 100 / unitSteps);

                        int curReg = getCv(cv, cp);

                        // 新しいReg値を算出
                        double ca = Settings.Ins.GapCam.TargetGain - cp.GapGain;

                        int newReg = calcNewRegUnit(curReg, ca);

                        // 新しいReg値を書き込み                    
                        setGapCorrectValue(cv.Unit, cp.Pos, newReg);
                    }
                    */

                    // Cell Gap
                    foreach (GapCamCp cp in cv.lstCellCp)
                    {
                        if (m_GapStatus == GapStatus.Result)
                        {
                            // modified by Hotta 2022/12/13
#if Spec_by_Zdistance
                            // 2回目以降で、それほどズレていなかったら、continue
                            if (Math.Abs(cp.GapGain - 1.0) < m_Spec_by_Zdistance[cv.Unit.X - 1][cv.Unit.Y - 1] * 0.5)
                            {
                                continue;
                            }
#else
                            // 2回目以降で、それほどズレていなかったら、continue
                            if (Math.Abs(cp.GapGain - 1.0) < Settings.Ins.GapCam.AdjustSpec * 0.5)
                            {
                                continue;
                            }
#endif
                        }

                        // modified by Hotta 2022/03/14
                        /*
                        winProgress.ShowMessage(progress + "\r\nSet Module Correction Value. (" + cp.CellNo + ", " + cp.Pos + ")");
                        */
                        string mes = "";
                        if (cp.CellNo == CellNum.Cell_0)
                            mes = progress + "\r\nSet Module1 Correction Value.";
                        else if (cp.CellNo == CellNum.Cell_1)
                            mes = progress + "\r\nSet Module2 Correction Value.";
                        else if (cp.CellNo == CellNum.Cell_2)
                            mes = progress + "\r\nSet Module3 Correction Value.";
                        else if (cp.CellNo == CellNum.Cell_3)
                            mes = progress + "\r\nSet Module4 Correction Value.";
                        else if (cp.CellNo == CellNum.Cell_4)
                            mes = progress + "\r\nSet Module5 Correction Value.";
                        else if (cp.CellNo == CellNum.Cell_5)
                            mes = progress + "\r\nSet Module6 Correction Value.";
                        else if (cp.CellNo == CellNum.Cell_6)
                            mes = progress + "\r\nSet Module7 Correction Value.";
                        else if (cp.CellNo == CellNum.Cell_7)
                            mes = progress + "\r\nSet Module8 Correction Value.";
                        // added by Hotta 2022/06/17 for Cancun
                        else if (cp.CellNo == CellNum.Cell_8)
                            mes = progress + "\r\nSet Module9 Correction Value.";
                        else if (cp.CellNo == CellNum.Cell_9)
                            mes = progress + "\r\nSet Module10 Correction Value.";
                        else if (cp.CellNo == CellNum.Cell_10)
                            mes = progress + "\r\nSet Module11 Correction Value.";
                        else if (cp.CellNo == CellNum.Cell_11)
                            mes = progress + "\r\nSet Module12 Correction Value.";
                        //

                        if (cp.Pos == CorrectPosition.BottomLeft)
                            mes += "BL)";
                        else if (cp.Pos == CorrectPosition.BottomRight)
                            mes += "BR)";
                        else if (cp.Pos == CorrectPosition.LeftBottom)
                            mes += "LB)";
                        else if (cp.Pos == CorrectPosition.LeftTop)
                            mes += "LT)";
                        else if (cp.Pos == CorrectPosition.RightBottom)
                            mes += "RB)";
                        else if (cp.Pos == CorrectPosition.RightTop)
                            mes += "RT)";
                        else if (cp.Pos == CorrectPosition.TopLeft)
                            mes += "TL)";
                        else if (cp.Pos == CorrectPosition.TopRight)
                            mes += "TR)";


                        winProgress.ShowMessage(mes);
                        //

                        //winProgress.SetPartProgress(step++ * 100 / unitSteps);
                        // deleted by Hotta 2022/08/30
                        /*
                        winProgress.PutForward1Step();
                        */
                        //saveLog(progress + "\r\nSet Module Correction Value. (" + cp.CellNo + ", " + cp.Pos + ")");

                        int curReg = 0;
                        if (m_GapStatus == GapStatus.Before)
                        {
                            // 1回目
                            curReg = getCv(cv, cp);
                        }
                        else
                        {
                            // 2回目以降
                            curReg = cp.RegAdj;
                        }

                        // 新しいReg値を算出
                        double ca = cp.GapGain;
                        int newReg = calcNewRegCell(curReg, ca);
                        cp.RegAdj = newReg;

                        // 新しいReg値を書き込み
                        EdgePosition edge = EdgePosition.NA;

                        if (cp.Pos == CorrectPosition.TopLeft)
                        { edge = EdgePosition.Edge_1; }
                        else if (cp.Pos == CorrectPosition.TopRight)
                        { edge = EdgePosition.Edge_2; }
                        else if (cp.Pos == CorrectPosition.RightTop)
                        { edge = EdgePosition.Edge_4; }
                        else if (cp.Pos == CorrectPosition.RightBottom)
                        { edge = EdgePosition.Edge_6; }
                        // added by Hotta 2022/07/08
#if CorrectTargetEdge
                        else if (cp.Pos == CorrectPosition.LeftTop)
                        { edge = EdgePosition.Edge_3; }
                        else if (cp.Pos == CorrectPosition.LeftBottom)
                        { edge = EdgePosition.Edge_5; }
                        else if (cp.Pos == CorrectPosition.BottomLeft)
                        { edge = EdgePosition.Edge_7; }
                        else if (cp.Pos == CorrectPosition.BottomRight)
                        { edge = EdgePosition.Edge_8; }
#endif

#if BulkSetCorrectValue
                        if(m_GapStatus == GapStatus.Before)
                            // 初回は、一括書き込み
                            setGapCellCorrectValue(cv.Unit, cp.CellNo, edge, newReg);
                        else
                            // 2回目以降は、即時書き込み
                            setGapCellCorrectValue_directly(cv.Unit, cp.CellNo, edge, newReg);
#else
                        setGapCellCorrectValue(cv.Unit, cp.CellNo, edge, newReg);
#endif
                    }
                    winProgress.PutForward1Step();
                }
                //winProgress.SetMarquee();

                // 初回は一括書き込み
                if(m_GapStatus == GapStatus.Before)
                {
                    // added by Hotta 2022/07/11
                    // Module一括書き込み
#if BulkSetCorrectValue
                    // deleted by Hotta 2022/08/30
                    /*
                    winProgress.SetWholeSteps(lstGapCamCp.Count * moduleCount);
                    winProgress.SetMarquee();
                    */

                    foreach (GapCamCorrectionValue cv in lstGapCamCp)
                    {
                        string progress = "Set(bulk) Gap Correction. (Cabinet : " + cv.Unit.ControllerID + "-" + cv.Unit.PortNo + "-" + cv.Unit.UnitNo + ")";
                        saveLog(progress);

                        for (int cell = 0; cell < moduleCount; cell++)
                        {
                            string mes = "";
                            if (cell == 0)
                                mes = progress + "\r\nSet Module1 Correction Value.";
                            else if (cell == 1)
                                mes = progress + "\r\nSet Module2 Correction Value.";
                            else if (cell == 2)
                                mes = progress + "\r\nSet Module3 Correction Value.";
                            else if (cell == 3)
                                mes = progress + "\r\nSet Module4 Correction Value.";
                            else if (cell == 4)
                                mes = progress + "\r\nSet Module5 Correction Value.";
                            else if (cell == 5)
                                mes = progress + "\r\nSet Module6 Correction Value.";
                            else if (cell == 6)
                                mes = progress + "\r\nSet Module7 Correction Value.";
                            else if (cell == 7)
                                mes = progress + "\r\nSet Module8 Correction Value.";
                            // added by Hotta 2022/06/17 for Cancun
                            else if (cell == 8)
                                mes = progress + "\r\nSet Module9 Correction Value.";
                            else if (cell == 9)
                                mes = progress + "\r\nSet Module10 Correction Value.";
                            else if (cell == 10)
                                mes = progress + "\r\nSet Module11 Correction Value.";
                            else if (cell == 11)
                                mes = progress + "\r\nSet Module12 Correction Value.";

                            winProgress.ShowMessage(mes);
                            // deleted by Hotta 2022/08/30
                            /*
                            winProgress.PutForward1Step();
                            */
#if NO_CONTROLLER
#else
                            setGapCvCellBulk(cv.Unit, cell + 1, cv.AryCvCell[cell]);  // +1 : Cell番号指定は1ベース
#endif
                        }
                        // added by Hotta 2022/08/30
                        winProgress.PutForward1Step();
                        //
                    }
#endif
                }

                foreach (GapCamCorrectionValue cv in lstGapCamCp)
                {
                    foreach (GapCamCp cp in cv.lstCellCp)
                    {
                        EdgePosition edge = EdgePosition.NA;

                        if (cp.Pos == CorrectPosition.TopLeft)
                        { edge = EdgePosition.Edge_1; }
                        else if (cp.Pos == CorrectPosition.TopRight)
                        { edge = EdgePosition.Edge_2; }
                        else if (cp.Pos == CorrectPosition.RightTop)
                        { edge = EdgePosition.Edge_4; }
                        else if (cp.Pos == CorrectPosition.RightBottom)
                        { edge = EdgePosition.Edge_6; }
                        // added by Hotta 2022/07/08
#if CorrectTargetEdge
                        else if (cp.Pos == CorrectPosition.BottomLeft)
                        { edge = EdgePosition.Edge_7; }
                        else if (cp.Pos == CorrectPosition.BottomRight)
                        { edge = EdgePosition.Edge_8; }
                        else if (cp.Pos == CorrectPosition.LeftTop)
                        { edge = EdgePosition.Edge_3; }
                        else if (cp.Pos == CorrectPosition.LeftBottom)
                        { edge = EdgePosition.Edge_5; }

#endif
                        setGapCellCorrectValueForXML(cv.Unit, cp.CellNo, edge, cp.RegAdj);
                    }
                }


#if NO_CAP
                Thread.Sleep(1000);
#endif

                m_GapStatus = GapStatus.Result;

                string fname;
                string str;

#if NoEncript
                if (m_AdjustCount == 0)
                    fname = m_CamMeasPath + "correctReg.csv";
                else
                    fname = m_CamMeasPath + "correctReg_" + (m_AdjustCount + 1).ToString() + ".csv";
#else
                if (m_AdjustCount == 0)
                    fname = m_CamMeasPath + "correctReg.csvx";
                else
                    fname = m_CamMeasPath + "correctReg_" + (m_AdjustCount + 1).ToString() + ".csvx";
#endif
                using (StreamWriter sw = new StreamWriter(fname))
                {
#if NO_CAP
                    //str = "UnitNo,CellNo,Dir,Pos,Gain,OrgReg,AdjReg";
                    str = "CabiX,CabiY,ModuleNo,Dir,Pos,Gain,OrgReg,AdjReg";
#if NoEncript
                    sw.WriteLine(str);
#else
                    string encryptStr;
                    encryptStr = Encrypt(str, CalcKey(AES_IV), CalcKey(AES_Key));
                    sw.WriteLine(encryptStr);
#endif
#endif
                    foreach (GapCamCorrectionValue cv in lstGapCamCp)
                    {
                        foreach (GapCamCp cp in cv.lstCellCp)
                        {
                            //str = string.Format("{0},{1},{2},{3},", (cv.Unit.Y - 1) * allocInfo.MaxX + (cv.Unit.X - 1), cp.CellNo, cp.Direction, cp.Pos);
                            str = string.Format("{0},{1},{2},{3},{4},", cv.Unit.X, cv.Unit.Y, cp.CellNo, cp.Direction, cp.Pos);
                            str += string.Format("{0},{1},{2}", cp.GapGain, cp.RegOrg, cp.RegAdj);
#if NoEncript
                            sw.WriteLine(str);
#else
                            encryptStr = Encrypt(str, CalcKey(AES_IV), CalcKey(AES_Key));
                            sw.WriteLine(encryptStr);
#endif
                        }
                    }
                }
#if NoEncript
#else
                /**/
                // 確認用
                using (StreamReader sr = new StreamReader(fname))
                {
                    str = sr.ReadToEnd();
                }
                string[] lines = str.Split('\n');
                fname = fname.Substring(0, fname.Length - 1);
                using (StreamWriter sw = new StreamWriter(fname))
                {
                    for (int n = 0; n < lines.Length - 1; n++)    // 最後のlines[]は、""のため
                    {
                        str = Decrypt(lines[n], CalcKey(AES_IV), CalcKey(AES_Key));
                        sw.WriteLine(str);
                    }
                }
                /**/
#endif
                // 計測しないで抜ける
                if (m_EvaluateAdjustmentResult != true)
                {
                    saveLog("Not measure adjustment result.");
                    break;
                }

                winProgress.SetWholeSteps(15);

                // added by Hotta 2024/01/09
                // ESCキー押下による中断を有効化
                winProgress.AbortType = WindowProgress.TAbortType.Adjustment;
                //

                // 撮影
                captureGapImages(m_CamMeasPath);

                // added by Hotta 2024/01/09
                // ESCキー押下による中断を無効化
                winProgress.AbortType = WindowProgress.TAbortType.None;
                //

                // 処理
                calcGapGain(lstTgtUnit, m_CamMeasPath);


                str = "";
#if NoEncript
                if (m_AdjustCount == 0)
                    fname = m_CamMeasPath + "result.csv";
                else
                    fname = m_CamMeasPath + "result_" + (m_AdjustCount + 1).ToString() + ".csv";
#else
                string EncryptStr;
                if (m_AdjustCount == 0)
                    fname = m_CamMeasPath + "result.csvx";
                else
                    fname = m_CamMeasPath + "result_" + (m_AdjustCount + 1).ToString() + ".csvx";
#endif
                using (StreamWriter sw = new StreamWriter(fname))
                {

                    double ave, stddev;
                    double sum1 = 0, sum2 = 0;
                    int count = 0;
                    foreach (GapCamCorrectionValue gapCv in lstGapCamCp)
                    {
                        // Cell
                        foreach (GapCamCp cp in gapCv.lstCellCp)
                        {
                            sum1 += cp.GapGain - Settings.Ins.GapCam.TargetGain;
                            sum2 += (cp.GapGain - Settings.Ins.GapCam.TargetGain) * (cp.GapGain - Settings.Ins.GapCam.TargetGain);
                            count++;
                        }
                    }
                    ave = sum1 / count;
                    stddev = Math.Sqrt(sum2 / count - ave * ave);

                    str = "avg," + (ave * 100).ToString() + "%";
#if NoEncript
                    sw.WriteLine(str);
#else
                    EncryptStr = Encrypt(str, CalcKey(AES_IV), CalcKey(AES_Key));
                    sw.WriteLine(EncryptStr);
#endif
                    str = "3*stddev," + (3.0 * stddev * 100).ToString() + "%";
#if NoEncript
                    sw.WriteLine(str);
#else
                    EncryptStr = Encrypt(str, CalcKey(AES_IV), CalcKey(AES_Key));
                    sw.WriteLine(EncryptStr);
#endif
                    // modified by Hotta 2022/12/15
#if Spec_by_Zdistance
                    str = "CabiX,CabiY,ModuleNo,Dir,Pos,Result,Spec";
#else
                    str = "CabiX,CabiY,ModuleNo,Dir,Pos,Result";
#endif

#if NoEncript
                    sw.WriteLine(str);
#else
                    EncryptStr = Encrypt(str, CalcKey(AES_IV), CalcKey(AES_Key));
                    sw.WriteLine(EncryptStr);
#endif
                    foreach (GapCamCorrectionValue cv in lstGapCamCp)
                    {
                        foreach (GapCamCp cp in cv.lstCellCp)
                        {
                            str = string.Format("{0},{1},{2},{3},{4},{5}%,", cv.Unit.X, cv.Unit.Y, (int)cp.CellNo + 1, cp.Direction, cp.Pos, (cp.GapGain - Settings.Ins.GapCam.TargetGain) * 100);
                            // added by Hotta 2022/12/15
#if Spec_by_Zdistance
                            str += string.Format("{0}%", m_Spec_by_Zdistance[cv.Unit.X - 1][cv.Unit.Y - 1] * 100);
#endif
#if NoEncript
                            sw.WriteLine(str);
#else
                            EncryptStr = Encrypt(str, CalcKey(AES_IV), CalcKey(AES_Key));
                            sw.WriteLine(EncryptStr);
#endif
                        }
                    }
                }
#if NoEncript
#else
                /**/
                // 確認用
                using (StreamReader sr = new StreamReader(fname))
                {
                    str = sr.ReadToEnd();
                }
                /*string[]*/ lines = str.Split('\n');
                fname = fname.Substring(0, fname.Length - 1);
                using (StreamWriter sw = new StreamWriter(fname))
                {
                    for (int n = 0; n < lines.Length - 1; n++)    // 最後のlines[]は、""のため
                    {
                        str = Decrypt(lines[n], CalcKey(AES_IV), CalcKey(AES_Key));
                        sw.WriteLine(str);
                    }
                }
                /**/
#endif
            }

            if(m_EvaluateAdjustmentResult == true)
            {
                if (m_MaxNumOfAdjustment == 1)
                {
                    completeFlag = true;

                    foreach (GapCamCorrectionValue cv in lstGapCamCp)
                    {
                        foreach (GapCamCp cp in cv.lstCellCp)
                        {
                            // modified by Hotta 2022/12/13
#if Spec_by_Zdistance
                            if (Math.Abs(cp.GapGain - 1.0) > m_Spec_by_Zdistance[cv.Unit.X - 1][cv.Unit.Y - 1])
                            {
                                completeFlag = false;
                                break;
                            }
#else
                            if (Math.Abs(cp.GapGain - 1.0) > Settings.Ins.GapCam.AdjustSpec)
                            {
                                completeFlag = false;
                                break;
                            }
#endif
                        }
                    }
                    if (completeFlag == true)
                    {
                        saveLog("All gap correction is in spec.");
                    }
                    else
                    {
                        saveLog("The gap correction is out spec, but the adjustment loop number is over specified number.");
                    }
                }
                else if (completeFlag != true && m_AdjustCount >= m_MaxNumOfAdjustment)
                {
                    saveLog("The gap correction is out spec, but the adjustment loop number is over specified number.");
                }
            }


            // added by Hotta 2022/03/14
            // ここまで完了したら、後の結果にかかわらず、ROM書き込みをできるようにする。
            Dispatcher.Invoke(new Action(() => { btnGapCamRomStart.IsEnabled = true; }));
            //

            processSec = calcGapCameraAdjustmentProcessSec(6);
            winProgress.SetRemainTimer(processSec);

            if (m_EvaluateAdjustmentResult == true)
            {
                // 全白撮影
                winProgress.ShowMessage("Capture White image.");
                saveLog("Capture White image.");
#if NO_CONTROLLER

#else
                // modified by Hotta 2022/07/08
#if CorrectTargetEdge
                outputGapCamTargetArea_EdgeExpand(lstTgtUnit, ExpandType.Both, true);
#else
                outputGapCamTargetArea(lstTgtUnit, true);
#endif
#endif
                // added by Hotta 2022/07/26
                Thread.Sleep(Settings.Ins.Camera.PatternWait);
                //

#if NO_CAP
#else
                saveLog(string.Format("CameraSetting:{0},{1},{2},{3},{4},{5}",
                    m_ShootCondition.FNumber,
                    m_ShootCondition.Shutter,
                    m_ShootCondition.ISO,
                    m_ShootCondition.WB,
                    m_ShootCondition.CompressionType,
                    m_ShootCondition.ImageSize));
                CaptureImage(m_CamMeasPath + fn_WhiteResultFile, m_ShootCondition);
#endif
                if (File.Exists(m_CamMeasPath + fn_WhiteResultFile + ".arw") == true)
                {
                    if (checkFileSize(m_CamMeasPath + fn_WhiteResultFile + ".arw") != true)
                    {
                        throw new Exception("Saving the " + m_CamMeasPath + fn_WhiteResultFile + ".arw" + " file does not completed.");
                    }

                    try
                    {
                        loadArwFile(m_CamMeasPath + fn_WhiteResultFile + ".arw", out arw);
                    }
                    // added by Hotta 2025/01/31
                    catch (CameraCasUserAbortException ex)
                    {
                        throw ex;
                    }
                    //
                    catch //(Exception ex)
                    {
                        Thread.Sleep(1000);
                        loadArwFile(m_CamMeasPath + fn_WhiteResultFile + ".arw", out arw);
                    }
                    width = arw.RawMainIFD.ImageWidth / 2;  // RAWデータは半分
                    height = arw.RawMainIFD.ImageHeight / 2;
                    mat = new Mat(new Size(width, height), MatType.CV_16UC3);
                    int ch = mat.Channels();
                    int step = width * ch;
                    pMat = (ushort*)(mat.Data);

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            pMat[y * step + x * ch + 2] = arw.RawData[0][x, y];
                            pMat[y * step + x * ch + 1] = arw.RawData[1][x, y];
                            pMat[y * step + x * ch + 0] = arw.RawData[2][x, y];
                        }
                    }
                    try
                    {
                        SaveMatBinary(mat, m_CamMeasPath + fn_WhiteResultFile);
                    }
                    catch //(Exception ex)
                    {
                        Thread.Sleep(1000);
                        SaveMatBinary(mat, m_CamMeasPath + fn_WhiteResultFile);
                    }
                    mat.Dispose();
                    arw = null;
                }
            }


            // 補正値関係以外をクリアしたインスタンス作成
            _lstGapCamCp = new List<GapCamCorrectionValue>();
            for (int n = 0; n < lstGapCamCp.Count; n++)
            {
                _lstGapCamCp.Add(new GapCamCorrectionValue(m_ModuleXNum * m_ModuleYNum));
                _lstGapCamCp[n].Unit = lstGapCamCp[n].Unit;
                // added by Hotta 2022/11/10 for Cabinet補正値なし
#if No_CabinetCorrectionValue

#else
                _lstGapCamCp[n].CvUnit = lstGapCamCp[n].CvUnit;
#endif
                _lstGapCamCp[n].AryCvCell = new GapCellCorrectValue[lstGapCamCp[n].AryCvCell.Length];
                for (int i = 0; i < lstGapCamCp[n].AryCvCell.Length; i++)
                {
                    _lstGapCamCp[n].AryCvCell[i] = lstGapCamCp[n].AryCvCell[i];
                }
            }

            // 測定結果を保存
            //GapCamCorrectionValue.SaveToXmlFile(measPath + "GapAdjustResult.xml", lstGapCamCp); // 1step
            GapCamCorrectionValue.SaveToXmlFile(m_CamMeasPath + "GapAdjustResult.xml", _lstGapCamCp); // 1step

#if NO_CAP
#else
            if(m_EvaluateAdjustmentResult == true)
            {
                // deleted by Hotta 2022/06/21
                // カメラ設定は変えないので、オートフォーカス不要
                /*
                // AutoFocus
                //winProgress.ShowMessage("Auto focus.");
                //winProgress.PutForward1Step();
                //saveLog("Auto focus.");
                //outputIntSigChecker(0, 0, modDy, modDx, modDx, modDy, m_MeasureLevel, m_MeasureLevel, m_MeasureLevel); // 20IRE
                // added by Hotta 2022/07/26
                //Thread.Sleep(Settings.Ins.Camera.PatternWait);
                //
                ///*
                //saveLog(string.Format("CameraSetting:{0},{1},{2},{3},{4},{5}",
                //    Settings.Ins.Camera.SetPosSetting.FNumber,
                //    Settings.Ins.Camera.SetPosSetting.Shutter,
                //    Settings.Ins.Camera.SetPosSetting.ISO,
                //    Settings.Ins.Camera.SetPosSetting.WB,
                //    Settings.Ins.Camera.SetPosSetting.CompressionType,
                //    Settings.Ins.Camera.SetPosSetting.ImageSize));
                //AutoFocus(Settings.Ins.Camera.SetPosSetting);
                //*/
                /*
                saveLog(string.Format("CameraSetting:{0},{1},{2},{3},{4},{5}",
                    m_ShootCondition.FNumber,
                    m_ShootCondition.Shutter,
                    m_ShootCondition.ISO,
                    m_ShootCondition.WB,
                    m_ShootCondition.CompressionType,
                    m_ShootCondition.ImageSize));
                AutoFocus(m_ShootCondition);
                */

                // 終了時のカメラ位置を保存 1Step
                winProgress.ShowMessage("Store Camera Position.");
                winProgress.PutForward1Step();
                saveLog("Store Camera Position.");
                for (int retry = 0; retry < 3; retry++)
                {
                    status = GetCameraPosition(imgGapCamMeasure);
                    saveLog(string.Format("X={0:F2},Y={1:F2},Z={2:F2},Pan={3:F2},Tilt={4:F2},Roll={5:F2}", m_CamPos_Tx, m_CamPos_Ty, m_CamPos_Tz, m_CamPos_Pan, m_CamPos_Tilt, m_CamPos_Roll));
                    saveLog(string.Format("Top={0:F1},Bottom={1:F1},Left={2:F1},Right={3:F1}", m_CamPos_TopLen, m_CamPos_BottomLen, m_CamPos_LeftLen, m_CamPos_RightLen));
                    if (status == true)
                        break;
                }
            }
#endif

            processSec = calcGapCameraAdjustmentProcessSec(7);
            winProgress.SetRemainTimer(processSec);

#if NO_CONTROLLER
#else

            // 調整設定を解除 1Step
            winProgress.ShowMessage("Set Normal Settings.");
            winProgress.PutForward1Step();
            saveLog("Set Normal Settings.");
            // modified by Hotta 2023/04/28 for v1.06
            /*
            setNormalSetting();
            */
            SetThroughMode(false);

            // ユーザー設定に書き戻し 1Step
            winProgress.ShowMessage("Restore User Settings.");
            winProgress.PutForward1Step();
            saveLog("Restore User Settings.");
            setUserSetting(lstUserSettings);

            // added by Hotta 2023/01/23
            // 正常に設定を戻したので、m_lstUserSetting = nullにして、呼び出し側のfinally分をスキップ
            m_lstUserSetting = null;
            //
#endif

#if NO_CAP
#else
            if (m_EvaluateAdjustmentResult == true)
            {
                if (status != true)
                {
                    saveLog("The camera position is inappropriate.");
                    if (appliMode != ApplicationMode.Developer)
                        throw new Exception("The camera position is inappropriate.\r\nPlease align the camera position again.");
                }
            }
#endif

            // 表示
            if (m_EvaluateAdjustmentResult == true)
                dispGapResult();

#if NO_CONTROLLER
#else
            outputIntSigFlat(m_MeasureLevel, m_MeasureLevel, m_MeasureLevel);
            if (m_MeasureLevel == 492)
            {
                Dispatcher.Invoke(new Action(() => { cmbxPatternGapCam.SelectedIndex = 2; }));
            }
            else
            {
                Dispatcher.Invoke(new Action(() => { cmbxPatternGapCam.SelectedIndex = 1; }));
            }
#endif

            // Logの世代管理
            ManageLogGen(applicationPath + MeasDir, "Gap_");

        }



        private void romSaveAsync(List<UnitInfo> lstTgtUnit)
        {
            saveLog("Start ROM writing.");
#if NO_CONTROLLER
#else
            // 書き込みの実行
            // modiefied by Hotta 2022/03/01
            //writeGapCellCorrectionValue();
            writeGapCellCorrectionValueWithReconfig();

            outputIntSigFlat(m_MeasureLevel, m_MeasureLevel, m_MeasureLevel);
            if (m_MeasureLevel == 492)
            {
                Dispatcher.Invoke(new Action(() => { cmbxPatternGapCam.SelectedIndex = 2; }));
            }
            else
            {
                Dispatcher.Invoke(new Action(() => { cmbxPatternGapCam.SelectedIndex = 1; }));
            }
#endif
            saveLog("Finish ROM writing.");
        }



        // added by Hotta 2022/03/01
        private bool writeGapCellCorrectionValueWithReconfig()
        {
            if (Settings.Ins.ExecLog == true)
            {
                SaveExecLog("");
                SaveExecLog("[0] writeGapCellCorrectionValue start");
            }

            // ●進捗の最大値を設定 [1]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[1] Set Step Count."); }
            winProgress.ShowMessage("Set Step Count.");

            int step = 4 + lstModifiedUnits.Count * 1;
            winProgress.SetWholeSteps(step);
            winProgress.PutForward1Step();

            // ●Panel OFF [2]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[2] Panel Off."); }
            winProgress.ShowMessage("Panel Off.");

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdUnitPowerOff, cont.IPAddress); }

            System.Threading.Thread.Sleep(10000);
            winProgress.PutForward1Step();

            foreach (UnitInfo unit in lstModifiedUnits)
            {
                // ●Write [*1]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*1] Write Correction Value."); }
                winProgress.ShowMessage("Write Correction Value.");

                Byte[] cmd = new byte[SDCPClass.CmdGapCellCorrectWrite.Length];
                Array.Copy(SDCPClass.CmdGapCellCorrectWrite, cmd, SDCPClass.CmdGapCellCorrectWrite.Length);

                cmd[9] += (byte)(unit.PortNo << 4);
                cmd[9] += (byte)unit.UnitNo;

                sendSdcpCommand(cmd, 0, dicController[unit.ControllerID].IPAddress);

                winProgress.PutForward1Step();
            }

            System.Threading.Thread.Sleep(10000);

            // ●Reconfig [3]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[3] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            // added by Hotta 2022/03/03
            foreach (ControllerInfo controller in dicController.Values)
            { controller.Target = true; }
            //

            sendReconfig();
            winProgress.PutForward1Step();

            // ●Panel ON [4]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[4] Panel On."); }
            winProgress.ShowMessage("Panel On.");

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdUnitPowerOn, cont.IPAddress); }

            winProgress.PutForward1Step();

            return true;
        }


        private int getCv(GapCamCorrectionValue cv, GapCamCp cp)
        {
            int reg = 0;

            if (cp.CellNo == CellNum.NA) // Unit
            {
                // added by Hotta 2022/11/10 for Cabinet補正値なし
#if No_CabinetCorrectionValue
                reg = 128;
#else
                if (cp.Pos == CorrectPosition.TopLeft)
                { reg = cv.CvUnit.TopLeft; }
                else if (cp.Pos == CorrectPosition.TopRight)
                { reg = cv.CvUnit.TopRight; }
                else if (cp.Pos == CorrectPosition.RightTop)
                { reg = cv.CvUnit.RightTop; }
                else if (cp.Pos == CorrectPosition.RightBottom)
                { reg = cv.CvUnit.RightBottom; }
#endif
            }
            else // Cell
            {
                GapCellCorrectValue cell = cv.AryCvCell[(int)cp.CellNo];

                if (cp.Pos == CorrectPosition.TopLeft)
                { reg = cell.TopLeft; }
                else if (cp.Pos == CorrectPosition.TopRight)
                { reg = cell.TopRight; }
                else if (cp.Pos == CorrectPosition.RightTop)
                { reg = cell.RightTop; }
                else if (cp.Pos == CorrectPosition.RightBottom)
                { reg = cell.RightBottom; }

                // added by Hotta 2022/07/08
#if CorrectTargetEdge
                else if (cp.Pos == CorrectPosition.BottomLeft)
                { reg = cell.BottomLeft; }
                else if (cp.Pos == CorrectPosition.BottomRight)
                { reg = cell.BottomRight; }
                else if (cp.Pos == CorrectPosition.LeftTop)
                { reg = cell.LeftTop; }
                else if (cp.Pos == CorrectPosition.LeftBottom)
                { reg = cell.LeftBottom; }
#endif
            }

            cp.RegOrg = reg;

            return reg;
        }

        /// <summary>
        /// 調整量（Gain）から補正量を計算する。Unit用、UnitとCellで感度や調整範囲が異なる
        /// </summary>
        /// <param name="curReg"></param>
        /// <param name="gapGain"></param>
        /// <returns></returns>
        private int calcNewRegUnit(int curReg, double gapGain)
        {
            return 0;
        }

        private int calcNewRegCell(int curReg, double gapGain)
        {
            // RS485_CA_RegiList_211008.xlsxブック　Reg_Detailシート　290行目より
            // LED Mod間目地補正
            // LED Mod最外周の1画素幅にのみ適用
            // LED Mod4辺の輝度を75.0 % -124.8 % の範囲で調整
            // LEDMOD_EDGE_FIT: 補正値 約0.2 % 刻み
            // Initial Value: 0x80(100 % 相当)

            double k = (1.248 - 0.75) / 255;
            
            // レジスタの現在値のゲイン
            double curGain = 1.0 + k * (curReg - 128);

            // 現在のゲインに、計測した目地コントラストの逆数をかける →　目地コントラスト=2.0なら、現在のゲインに1/2.0をかける
            double newGain = curGain * (1.0 / gapGain);

            // 求めたゲインをレジスタ値に変換
            int newReg = (int)((newGain - 1.0) / k + 128 + 0.5);

            // リミットを外す
            /*
            if (newReg < 80)
            { newReg = 80; }

            if (newReg > 176)
            { newReg = 176; }
            */
            // modified by Hotta 2022/03/09
            /*
            if (newReg < 0)
            { newReg = 0; }

            if (newReg > 200)
            { newReg = 200; }
            */
            // modified by Hotta 2022/08/08
            /*
            if (newReg < 56)
            { newReg = 56; }

            if (newReg > 200)
            { newReg = 200; }
            */
            if (newReg < correctValue_Min)
            { newReg = correctValue_Min; }

            if (newReg > correctValue_Max)
            { newReg = correctValue_Max; }


            return newReg;
        }

        private void setGapCorrectValue(UnitInfo unit, CorrectPosition pos, int value)
        {
            // deleted by Hotta 2023/03/03
            /*
            CursorDirection dir;
            */

            // modified by Hotta 2022/07/13
#if Coverity
            if (unit == null)
            { return; }

            int x = unit.X, y = unit.Y;
#else
            int x = unit.X, y = unit.Y;

            if (unit == null)
            { return; }
#endif

            // deleted by Hotta 2022/07/08
#if CorrectTargetEdge
#else
            if (pos == CorrectPosition.TopLeft || pos == CorrectPosition.TopRight)
            { dir = CursorDirection.X; }
            else
            { dir = CursorDirection.Y; }
#endif

            Byte[] cmd = new byte[SDCPClass.CmdGapCorrectValueSet.Length];
            Array.Copy(SDCPClass.CmdGapCorrectValueSet, cmd, SDCPClass.CmdGapCorrectValueSet.Length);

            // Target Unit
            cmd[9] += (byte)(unit.PortNo << 4);
            cmd[9] += (byte)unit.UnitNo;

            // modified by Hotta 2022/07/08
#if CorrectTargetEdge
            if (pos == CorrectPosition.TopLeft)
            { cmd[8] = 0; }
            else if (pos == CorrectPosition.TopRight)
            { cmd[8] = 1; }
            else if (pos == CorrectPosition.RightTop)
            { cmd[8] = 6; }
            else if(pos == CorrectPosition.RightBottom)
            { cmd[8] = 7; }
            else if (pos == CorrectPosition.BottomLeft)
            { cmd[8] = 2; }
            else if (pos == CorrectPosition.BottomRight)
            { cmd[8] = 3; }
            else if (pos == CorrectPosition.LeftTop)
            { cmd[8] = 4; }
            else if (pos == CorrectPosition.LeftBottom)
            { cmd[8] = 5; }
#else
            if (pos == CorrectPosition.TopLeft)
            { cmd[8] = 0; }
            else if (pos == CorrectPosition.TopRight)
            { cmd[8] = 1; }
            else if (pos == CorrectPosition.RightTop)
            { cmd[8] = 6; }
            else // RightBottom
            { cmd[8] = 7; }
#endif
            cmd[20] = (byte)value;

            sendSdcpCommand(cmd, 100, dicController[unit.ControllerID].IPAddress);

            // Next Unit
            int nextX, nextY;
            UnitInfo nextUnit = null;
            // modified by Hotta 2022/07/08
#if CorrectTargetEdge
            nextX = x;
            nextY = y;
            if(pos == CorrectPosition.TopLeft || pos == CorrectPosition.TopRight)
            {
                nextX = x;
                nextY = y - 1; // 1つ上のUnit
            }
            else if(pos == CorrectPosition.BottomLeft || pos == CorrectPosition.BottomRight)
            {
                nextX = x;
                nextY = y + 1; // 1つ下のUnit
            }
            else if(pos == CorrectPosition.RightTop || pos == CorrectPosition.RightBottom)
            {
                nextX = x + 1; // 1つ右のUnit
                nextY = y;
            }
            else if (pos == CorrectPosition.LeftTop || pos == CorrectPosition.LeftBottom)
            {
                nextX = x - 1; // 1つ左のUnit
                nextY = y;
            }
#else
            if (dir == CursorDirection.X)
            {
                nextX = x;
                nextY = y - 1; // 1つ上のUnit
            }
            else
            {
                nextX = x + 1;
                nextY = y;
            }
#endif
            try { nextUnit = aryUnitUf[nextX - 1, nextY - 1].UnitInfo; }
            catch { return; }

            if (nextUnit == null)
            { return; }

            // Unit Address
            cmd[9] = 0;
            cmd[9] += (byte)(nextUnit.PortNo << 4);
            cmd[9] += (byte)nextUnit.UnitNo;

            // modified by Hotta 2022/07/08
#if CorrectTargetEdge
            if (pos == CorrectPosition.TopLeft)
            { cmd[8] = 2; }
            else if (pos == CorrectPosition.TopRight)
            { cmd[8] = 3; }
            else if (pos == CorrectPosition.RightTop)
            { cmd[8] = 4; }
            else if (pos == CorrectPosition.RightBottom)
            { cmd[8] = 5; }
            else if (pos == CorrectPosition.BottomLeft)
            { cmd[8] = 0; }
            else if (pos == CorrectPosition.BottomRight)
            { cmd[8] = 1; }
            else if (pos == CorrectPosition.LeftTop)
            { cmd[8] = 6; }
            else if (pos == CorrectPosition.LeftBottom)
            { cmd[8] = 7; }
#else
            if (pos == CorrectPosition.TopLeft)
            { cmd[8] = 2; }
            else if (pos == CorrectPosition.TopRight)
            { cmd[8] = 3; }
            else if (pos == CorrectPosition.RightTop)
            { cmd[8] = 4; }
            else
            { cmd[8] = 5; }
#endif
            cmd[20] = (byte)value;

            sendSdcpCommand(cmd, 100, dicController[nextUnit.ControllerID].IPAddress);
        }

        private void setGapCellCorrectValue(UnitInfo unit, CellNum CellNo, EdgePosition targetEdge, int value)
        {
            UnitInfo targetUnit = unit;

            if (targetUnit == null)
            { return; }


            int targetCell = (int)CellNo + 1;
            Byte[] cmd;

#if NO_CONTROLLER
            ;
#else
            // modified by Hotta 2022/07/11
            // 対象Cabinetの補正データは、ここではセットしない。一旦、lstGapCamCpにストアしておいて、後でセットする。
#if BulkSetCorrectValue
            ;
#else
            /*Byte[]*/ cmd = new byte[SDCPClass.CmdGapCellCorrectValueSet.Length];
            Array.Copy(SDCPClass.CmdGapCellCorrectValueSet, cmd, SDCPClass.CmdGapCellCorrectValueSet.Length);

            cmd[9] += (byte)(targetUnit.PortNo << 4);
            cmd[9] += (byte)targetUnit.UnitNo;

            //int targetCell = (int)CellNo + 1;

            cmd[20] = (byte)targetCell;

            cmd[21] = (byte)targetEdge;

            cmd[22] = (byte)value;

            sendSdcpCommand(cmd, 100, dicController[targetUnit.ControllerID].IPAddress);
#endif

#endif

            for (int n=0;n<lstGapCamCp.Count;n++)
            {
                if(lstGapCamCp[n].Unit.X == unit.X && lstGapCamCp[n].Unit.Y == unit.Y)
                {
                    // AryCvCell[]のインデックス = CellNoのはず
                    if (targetEdge == EdgePosition.Edge_1)
                        lstGapCamCp[n].AryCvCell[(int)CellNo].TopLeft = value;
                    else if (targetEdge == EdgePosition.Edge_2)
                        lstGapCamCp[n].AryCvCell[(int)CellNo].TopRight = value;
                    else if (targetEdge == EdgePosition.Edge_3)
                        lstGapCamCp[n].AryCvCell[(int)CellNo].LeftTop = value;
                    else if (targetEdge == EdgePosition.Edge_4)
                        lstGapCamCp[n].AryCvCell[(int)CellNo].RightTop = value;
                    else if (targetEdge == EdgePosition.Edge_5)
                        lstGapCamCp[n].AryCvCell[(int)CellNo].LeftBottom = value;
                    else if (targetEdge == EdgePosition.Edge_6)
                        lstGapCamCp[n].AryCvCell[(int)CellNo].RightBottom = value;
                    else if (targetEdge == EdgePosition.Edge_7)
                        lstGapCamCp[n].AryCvCell[(int)CellNo].BottomLeft = value;
                    else if (targetEdge == EdgePosition.Edge_8)
                        lstGapCamCp[n].AryCvCell[(int)CellNo].BottomRight = value;
                    break;
                }
            }
            //

            if (lstModifiedUnits.Contains(targetUnit) == false)
            { lstModifiedUnits.Add(targetUnit); }

            // Next Unit
            UnitInfo nextUnit;
            int nextCell;
            EdgePosition nextEdge;

            getNextCell(targetUnit, targetCell, targetEdge, out nextUnit, out nextCell, out nextEdge);

            if (nextUnit == null)
            { return; }

#if NO_CONTROLLER

#else

#if BulkSetCorrectValue
            // 隣接データが、lstGapCamCpに含まれていなければ、ここでセットする。
            // 隣接データが、lstGapCamCpに含まれていれば、ストアしておいて、ここでセットしない。
            bool containUnit = false;
            for (int n = 0; n < lstGapCamCp.Count; n++)
            {
                if (lstGapCamCp[n].Unit.X == nextUnit.X && lstGapCamCp[n].Unit.Y == nextUnit.Y)
                {
                    containUnit = true;
                    break;
                }
            }
            if (containUnit != true)
            {
                cmd = new byte[SDCPClass.CmdGapCellCorrectValueSet.Length];
                Array.Copy(SDCPClass.CmdGapCellCorrectValueSet, cmd, SDCPClass.CmdGapCellCorrectValueSet.Length);

                cmd[9] += (byte)(nextUnit.PortNo << 4);
                cmd[9] += (byte)nextUnit.UnitNo;

                cmd[20] = (byte)nextCell;

                cmd[21] = (byte)nextEdge;

                cmd[22] = (byte)value;

                sendSdcpCommand(cmd, 100, dicController[nextUnit.ControllerID].IPAddress);
            }
#else
            cmd = new byte[SDCPClass.CmdGapCellCorrectValueSet.Length];
            Array.Copy(SDCPClass.CmdGapCellCorrectValueSet, cmd, SDCPClass.CmdGapCellCorrectValueSet.Length);

            cmd[9] += (byte)(nextUnit.PortNo << 4);
            cmd[9] += (byte)nextUnit.UnitNo;

            cmd[20] = (byte)nextCell;

            cmd[21] = (byte)nextEdge;

            cmd[22] = (byte)value;

            sendSdcpCommand(cmd, 100, dicController[nextUnit.ControllerID].IPAddress);
#endif

#endif

            for (int n = 0; n < lstGapCamCp.Count; n++)
            {
                if (lstGapCamCp[n].Unit.X == nextUnit.X && lstGapCamCp[n].Unit.Y == nextUnit.Y)
                {
                    // AryCvCell[]のインデックス = nextCell - 1のはず
                    if (nextEdge == EdgePosition.Edge_1)
                        lstGapCamCp[n].AryCvCell[nextCell - 1].TopLeft = value;
                    else if (nextEdge == EdgePosition.Edge_2)
                        lstGapCamCp[n].AryCvCell[nextCell - 1].TopRight = value;
                    else if (nextEdge == EdgePosition.Edge_3)
                        lstGapCamCp[n].AryCvCell[nextCell - 1].LeftTop = value;
                    else if (nextEdge == EdgePosition.Edge_4)
                        lstGapCamCp[n].AryCvCell[nextCell - 1].RightTop = value;
                    else if (nextEdge == EdgePosition.Edge_5)
                        lstGapCamCp[n].AryCvCell[nextCell - 1].LeftBottom = value;
                    else if (nextEdge == EdgePosition.Edge_6)
                        lstGapCamCp[n].AryCvCell[nextCell - 1].RightBottom = value;
                    else if (nextEdge == EdgePosition.Edge_7)
                        lstGapCamCp[n].AryCvCell[nextCell - 1].BottomLeft = value;
                    else if (nextEdge == EdgePosition.Edge_8)
                        lstGapCamCp[n].AryCvCell[nextCell - 1].BottomRight = value;
                    break;
                }
            }

            if (lstModifiedUnits.Contains(nextUnit) == false)
            { lstModifiedUnits.Add(nextUnit); }
        }

        // added by Hotta 2022/07/13
        // lstGapCamCpに関係なく、レジスタにセットする。
        private void setGapCellCorrectValue_directly(UnitInfo unit, CellNum CellNo, EdgePosition targetEdge, int value)
        {
            UnitInfo targetUnit = unit;

            if (targetUnit == null)
            { return; }


            int targetCell = (int)CellNo + 1;
            Byte[] cmd;

#if NO_CONTROLLER
            ;
#else
            /*Byte[]*/ cmd = new byte[SDCPClass.CmdGapCellCorrectValueSet.Length];
            Array.Copy(SDCPClass.CmdGapCellCorrectValueSet, cmd, SDCPClass.CmdGapCellCorrectValueSet.Length);

            cmd[9] += (byte)(targetUnit.PortNo << 4);
            cmd[9] += (byte)targetUnit.UnitNo;

            //int targetCell = (int)CellNo + 1;

            cmd[20] = (byte)targetCell;

            cmd[21] = (byte)targetEdge;

            cmd[22] = (byte)value;

            sendSdcpCommand(cmd, 100, dicController[targetUnit.ControllerID].IPAddress);
#endif

            for (int n = 0; n < lstGapCamCp.Count; n++)
            {
                if (lstGapCamCp[n].Unit.X == unit.X && lstGapCamCp[n].Unit.Y == unit.Y)
                {
                    // AryCvCell[]のインデックス = CellNoのはず
                    if (targetEdge == EdgePosition.Edge_1)
                        lstGapCamCp[n].AryCvCell[(int)CellNo].TopLeft = value;
                    else if (targetEdge == EdgePosition.Edge_2)
                        lstGapCamCp[n].AryCvCell[(int)CellNo].TopRight = value;
                    else if (targetEdge == EdgePosition.Edge_3)
                        lstGapCamCp[n].AryCvCell[(int)CellNo].LeftTop = value;
                    else if (targetEdge == EdgePosition.Edge_4)
                        lstGapCamCp[n].AryCvCell[(int)CellNo].RightTop = value;
                    else if (targetEdge == EdgePosition.Edge_5)
                        lstGapCamCp[n].AryCvCell[(int)CellNo].LeftBottom = value;
                    else if (targetEdge == EdgePosition.Edge_6)
                        lstGapCamCp[n].AryCvCell[(int)CellNo].RightBottom = value;
                    else if (targetEdge == EdgePosition.Edge_7)
                        lstGapCamCp[n].AryCvCell[(int)CellNo].BottomLeft = value;
                    else if (targetEdge == EdgePosition.Edge_8)
                        lstGapCamCp[n].AryCvCell[(int)CellNo].BottomRight = value;
                    break;
                }
            }
            //

            if (lstModifiedUnits.Contains(targetUnit) == false)
            { lstModifiedUnits.Add(targetUnit); }

            // Next Unit
            UnitInfo nextUnit;
            int nextCell;
            EdgePosition nextEdge;

            getNextCell(targetUnit, targetCell, targetEdge, out nextUnit, out nextCell, out nextEdge);

            if (nextUnit == null)
            { return; }

#if NO_CONTROLLER

#else
            cmd = new byte[SDCPClass.CmdGapCellCorrectValueSet.Length];
            Array.Copy(SDCPClass.CmdGapCellCorrectValueSet, cmd, SDCPClass.CmdGapCellCorrectValueSet.Length);

            cmd[9] += (byte)(nextUnit.PortNo << 4);
            cmd[9] += (byte)nextUnit.UnitNo;

            cmd[20] = (byte)nextCell;

            cmd[21] = (byte)nextEdge;

            cmd[22] = (byte)value;

            sendSdcpCommand(cmd, 100, dicController[nextUnit.ControllerID].IPAddress);
#endif

            for (int n = 0; n < lstGapCamCp.Count; n++)
            {
                if (lstGapCamCp[n].Unit.X == nextUnit.X && lstGapCamCp[n].Unit.Y == nextUnit.Y)
                {
                    // AryCvCell[]のインデックス = nextCell - 1のはず
                    if (nextEdge == EdgePosition.Edge_1)
                        lstGapCamCp[n].AryCvCell[nextCell - 1].TopLeft = value;
                    else if (nextEdge == EdgePosition.Edge_2)
                        lstGapCamCp[n].AryCvCell[nextCell - 1].TopRight = value;
                    else if (nextEdge == EdgePosition.Edge_3)
                        lstGapCamCp[n].AryCvCell[nextCell - 1].LeftTop = value;
                    else if (nextEdge == EdgePosition.Edge_4)
                        lstGapCamCp[n].AryCvCell[nextCell - 1].RightTop = value;
                    else if (nextEdge == EdgePosition.Edge_5)
                        lstGapCamCp[n].AryCvCell[nextCell - 1].LeftBottom = value;
                    else if (nextEdge == EdgePosition.Edge_6)
                        lstGapCamCp[n].AryCvCell[nextCell - 1].RightBottom = value;
                    else if (nextEdge == EdgePosition.Edge_7)
                        lstGapCamCp[n].AryCvCell[nextCell - 1].BottomLeft = value;
                    else if (nextEdge == EdgePosition.Edge_8)
                        lstGapCamCp[n].AryCvCell[nextCell - 1].BottomRight = value;
                    break;
                }
            }

            if (lstModifiedUnits.Contains(nextUnit) == false)
            { lstModifiedUnits.Add(nextUnit); }
        }



        private void setGapCellCorrectValueForXML(UnitInfo unit, CellNum CellNo, EdgePosition targetEdge, int value)
        {
            UnitInfo targetUnit = unit;

            if (targetUnit == null)
            { return; }

            int targetCell = (int)CellNo + 1;

            for (int n = 0; n < lstGapCamCp.Count; n++)
            {
                if (lstGapCamCp[n].Unit.X == unit.X && lstGapCamCp[n].Unit.Y == unit.Y)
                {
                    // AryCvCell[]のインデックス = CellNoのはず
                    if (targetEdge == EdgePosition.Edge_1)
                        lstGapCamCp[n].AryCvCell[(int)CellNo].TopLeft = value;
                    else if (targetEdge == EdgePosition.Edge_2)
                        lstGapCamCp[n].AryCvCell[(int)CellNo].TopRight = value;
                    else if (targetEdge == EdgePosition.Edge_3)
                        lstGapCamCp[n].AryCvCell[(int)CellNo].LeftTop = value;
                    else if (targetEdge == EdgePosition.Edge_4)
                        lstGapCamCp[n].AryCvCell[(int)CellNo].RightTop = value;
                    else if (targetEdge == EdgePosition.Edge_5)
                        lstGapCamCp[n].AryCvCell[(int)CellNo].LeftBottom = value;
                    else if (targetEdge == EdgePosition.Edge_6)
                        lstGapCamCp[n].AryCvCell[(int)CellNo].RightBottom = value;
                    else if (targetEdge == EdgePosition.Edge_7)
                        lstGapCamCp[n].AryCvCell[(int)CellNo].BottomLeft = value;
                    else if (targetEdge == EdgePosition.Edge_8)
                        lstGapCamCp[n].AryCvCell[(int)CellNo].BottomRight = value;
                    break;
                }
            }

            // Next Unit
            UnitInfo nextUnit;
            int nextCell;
            EdgePosition nextEdge;

            getNextCell(targetUnit, targetCell, targetEdge, out nextUnit, out nextCell, out nextEdge);

            if (nextUnit == null)
            { return; }

            for (int n = 0; n < lstGapCamCp.Count; n++)
            {
                if (lstGapCamCp[n].Unit.X == nextUnit.X && lstGapCamCp[n].Unit.Y == nextUnit.Y)
                {
                    // AryCvCell[]のインデックス = nextCell - 1のはず
                    if (nextEdge == EdgePosition.Edge_1)
                        lstGapCamCp[n].AryCvCell[nextCell - 1].TopLeft = value;
                    else if (nextEdge == EdgePosition.Edge_2)
                        lstGapCamCp[n].AryCvCell[nextCell - 1].TopRight = value;
                    else if (nextEdge == EdgePosition.Edge_3)
                        lstGapCamCp[n].AryCvCell[nextCell - 1].LeftTop = value;
                    else if (nextEdge == EdgePosition.Edge_4)
                        lstGapCamCp[n].AryCvCell[nextCell - 1].RightTop = value;
                    else if (nextEdge == EdgePosition.Edge_5)
                        lstGapCamCp[n].AryCvCell[nextCell - 1].LeftBottom = value;
                    else if (nextEdge == EdgePosition.Edge_6)
                        lstGapCamCp[n].AryCvCell[nextCell - 1].RightBottom = value;
                    else if (nextEdge == EdgePosition.Edge_7)
                        lstGapCamCp[n].AryCvCell[nextCell - 1].BottomLeft = value;
                    else if (nextEdge == EdgePosition.Edge_8)
                        lstGapCamCp[n].AryCvCell[nextCell - 1].BottomRight = value;
                    break;
                }
            }
        }


        private void setAllGapCorrectValue(UnitInfo unit, int value)
        {
            UnitInfo targetUnit = unit;

            if (targetUnit == null)
            { return; }

            int sdcpWait = 100;
            Byte[] cmd = new byte[SDCPClass.CmdGapCellCorrectValueSet.Length];
            Array.Copy(SDCPClass.CmdGapCellCorrectValueSet, cmd, SDCPClass.CmdGapCellCorrectValueSet.Length);


            cmd[9] = 0xff; // 全Cabinet

            cmd[22] = (byte)value;  // 補正値

            // modified by Hotta 2022/06/17 for Cancun
            /*
            for (int module=0;module<8;module++)
            */
            for (int module = 0; module < m_ModuleXNum * m_ModuleYNum; module++)
            {
                // Module
                cmd[20] = (byte)(module + 1);   // Module0=1, module1=2,..., module7=8;

                for (int edge = (int)EdgePosition.Edge_1; edge <= (int)EdgePosition.Edge_8; edge++)
                {
                    cmd[21] = (byte)edge;
                    // modified by Hotta 2022/03/03
                    /*
                    sendSdcpCommand(cmd, sdcpWait, dicController[targetUnit.ControllerID].IPAddress);
                    */
                    foreach(ControllerInfo controller in dicController.Values)
                    {
                        sendSdcpCommand(cmd, sdcpWait, controller.IPAddress);
                    }
                }
            }
        }
#endregion Adjust


        // added by Hotta 2022/05/24 for カメラ位置決め
#if CameraPosition

        bool m_Enable_Capture_MaskImage = true;

        double m_LastTx = 0, m_LastTy = 0, m_LastTz = 0;
        double m_LastPan = 0, m_LastTilt = 0, m_LastRoll = 0;

        double m_CamPos_Tx, m_CamPos_Ty, m_CamPos_Tz;
        double m_CamPos_Pan, m_CamPos_Tilt, m_CamPos_Roll;
        double m_CamPos_TopLen, m_CamPos_BottomLen, m_CamPos_LeftLen, m_CamPos_RightLen;
        OpenCvSharp.Point[] m_Max_contours;
        string m_CamPos_BlackImagePath, m_CamPos_RasterImagePath, m_CamPos_TileImagePath;


        private void AdjustCameraPosition(System.Windows.Forms.Timer timer, System.Windows.Controls.Image img, ToggleButton tbtn)
        {
            bool status = true;

            timer.Enabled = false;

            m_CamPos_BlackImagePath = tempPath + "black_CamPos";
            m_CamPos_RasterImagePath = tempPath + "raster_CamPos";
            m_CamPos_TileImagePath = tempPath + "tile_CamPos";

            CvBlobs blobs = null;
            CvBlobs blobSatArea = null;

            try
            {
#if NO_CAP
#else
                // modified by Hotta 2022/08/08
                //captureCamPos(img);
                captureCamPos(img, false);
#endif
                detectTileCamPos(out blobs);

                // added by Hotta 2022/12/19
                detectSatAreaCamPos(out blobSatArea);
                //

            }
            catch (Exception ex)
            { 
                // 継続しない
                ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                tbtnGapCamSetPos.IsChecked = false;
                tcGapCamView.SelectedIndex = 0;

                // added by Hotta 2023/05/01
#if NO_CONTROLLER
#else
                SetThroughMode(false);

                // ユーザー設定に書き戻し
                setUserSettingSetPos(userSetting);

                // 内部信号OFF
                stopIntSig();
#endif

                return;
            }

            GC.Collect();


            // added by Hotta 2022/07/25
            // 上のcaptureCamPos()実行中に、tbtnGapCamSetPosやbtnGapCamAdjStart等が押されたときの対策
            if (tbtnGapCamSetPos.IsChecked != true)
            {
                // added by Hotta 2023/02/06
                // ボタンが解除されている場合は終了処理                
#if NO_CONTROLLER
#else
                SetThroughMode(false);

                // added by Hotta 2023/04/28 for v1.06
                // ユーザー設定に書き戻し
                setUserSettingSetPos(userSetting);
                //

                // 内部信号OFF
                stopIntSig();
#endif
                //

                return;
            }
            //

            CvBlob[,] aryBlob = new CvBlob[m_CabinetXNum_CamPos * 2, m_CabinetYNum_CamPos * 2];
            //CvBlob[,] aryBlob = new CvBlob[m_CabinetXNum * 4, m_CabinetYNum * 4];

            status = true;
            try
            {
                getTilePosition(blobs, m_CabinetXNum_CamPos * 2, m_CabinetYNum_CamPos * 2, out aryBlob);
                //getTilePosition(blobs, m_CabinetXNum * 4, m_CabinetYNum * 4, out aryBlob);
            }
            catch //(Exception ex)
            {
                status = false;
            }

            try
            {
                bool canProgress = true; // 次のStep(Measure/Adjust)に進めるかどうか

                if (status == true)
                {
                    // 各辺の長さの算出
                    // modified by Hotta 2022/07/29
                    /*
                    m_CamPos_TopLen = Math.Sqrt(
                        Math.Pow(aryBlob[0, 0].Centroid.X - aryBlob[m_CabinetXNum * 2 - 1, 0].Centroid.X, 2) +
                        Math.Pow(aryBlob[0, 0].Centroid.Y - aryBlob[m_CabinetXNum * 2 - 1, 0].Centroid.Y, 2));
                    m_CamPos_BottomLen = Math.Sqrt(
                        Math.Pow(aryBlob[0, m_CabinetYNum * 2 - 1].Centroid.X - aryBlob[m_CabinetXNum * 2 - 1, m_CabinetYNum * 2 - 1].Centroid.X, 2) +
                        Math.Pow(aryBlob[0, m_CabinetYNum * 2 - 1].Centroid.Y - aryBlob[m_CabinetXNum * 2 - 1, m_CabinetYNum * 2 - 1].Centroid.Y, 2));
                    m_CamPos_LeftLen = Math.Sqrt(
                        Math.Pow(aryBlob[0, 0].Centroid.X - aryBlob[0, m_CabinetYNum * 2 - 1].Centroid.X, 2) +
                        Math.Pow(aryBlob[0, 0].Centroid.Y - aryBlob[0, m_CabinetYNum * 2 - 1].Centroid.Y, 2));
                    m_CamPos_RightLen = Math.Sqrt(
                        Math.Pow(aryBlob[m_CabinetXNum * 2 - 1, 0].Centroid.X - aryBlob[m_CabinetXNum * 2 - 1, m_CabinetYNum * 2 - 1].Centroid.X, 2) +
                        Math.Pow(aryBlob[m_CabinetXNum * 2 - 1, 0].Centroid.Y - aryBlob[m_CabinetXNum * 2 - 1, m_CabinetYNum * 2 - 1].Centroid.Y, 2));
                    */
                    m_CamPos_TopLen = Math.Sqrt(
                        Math.Pow(aryBlob[0, 0].Centroid.X - aryBlob[m_CabinetXNum_CamPos * 2 - 1, 0].Centroid.X, 2) +
                        Math.Pow(aryBlob[0, 0].Centroid.Y - aryBlob[m_CabinetXNum_CamPos * 2 - 1, 0].Centroid.Y, 2));
                    m_CamPos_BottomLen = Math.Sqrt(
                        Math.Pow(aryBlob[0, m_CabinetYNum_CamPos * 2 - 1].Centroid.X - aryBlob[m_CabinetXNum_CamPos * 2 - 1, m_CabinetYNum_CamPos * 2 - 1].Centroid.X, 2) +
                        Math.Pow(aryBlob[0, m_CabinetYNum_CamPos * 2 - 1].Centroid.Y - aryBlob[m_CabinetXNum_CamPos * 2 - 1, m_CabinetYNum_CamPos * 2 - 1].Centroid.Y, 2));
                    m_CamPos_LeftLen = Math.Sqrt(
                        Math.Pow(aryBlob[0, 0].Centroid.X - aryBlob[0, m_CabinetYNum_CamPos * 2 - 1].Centroid.X, 2) +
                        Math.Pow(aryBlob[0, 0].Centroid.Y - aryBlob[0, m_CabinetYNum_CamPos * 2 - 1].Centroid.Y, 2));
                    m_CamPos_RightLen = Math.Sqrt(
                        Math.Pow(aryBlob[m_CabinetXNum_CamPos * 2 - 1, 0].Centroid.X - aryBlob[m_CabinetXNum_CamPos * 2 - 1, m_CabinetYNum_CamPos * 2 - 1].Centroid.X, 2) +
                        Math.Pow(aryBlob[m_CabinetXNum_CamPos * 2 - 1, 0].Centroid.Y - aryBlob[m_CabinetXNum_CamPos * 2 - 1, m_CabinetYNum_CamPos * 2 - 1].Centroid.Y, 2));
                    //

                    //m_CamPos_TopLen = Math.Sqrt(
                    //    Math.Pow(aryBlob[0, 0].Centroid.X - aryBlob[m_CabinetXNum * 4 - 1, 0].Centroid.X, 2) +
                    //    Math.Pow(aryBlob[0, 0].Centroid.Y - aryBlob[m_CabinetXNum * 4 - 1, 0].Centroid.Y, 2));
                    //m_CamPos_BottomLen = Math.Sqrt(
                    //    Math.Pow(aryBlob[0, m_CabinetYNum * 4 - 1].Centroid.X - aryBlob[m_CabinetXNum * 4 - 1, m_CabinetYNum * 4 - 1].Centroid.X, 2) +
                    //    Math.Pow(aryBlob[0, m_CabinetYNum * 4 - 1].Centroid.Y - aryBlob[m_CabinetXNum * 4 - 1, m_CabinetYNum * 4 - 1].Centroid.Y, 2));
                    //m_CamPos_LeftLen = Math.Sqrt(
                    //    Math.Pow(aryBlob[0, 0].Centroid.X - aryBlob[0, m_CabinetYNum * 4 - 1].Centroid.X, 2) +
                    //    Math.Pow(aryBlob[0, 0].Centroid.Y - aryBlob[0, m_CabinetYNum * 4 - 1].Centroid.Y, 2));
                    //m_CamPos_RightLen = Math.Sqrt(
                    //    Math.Pow(aryBlob[m_CabinetXNum * 4 - 1, 0].Centroid.X - aryBlob[m_CabinetXNum * 4 - 1, m_CabinetYNum * 4 - 1].Centroid.X, 2) +
                    //    Math.Pow(aryBlob[m_CabinetXNum * 4 - 1, 0].Centroid.Y - aryBlob[m_CabinetXNum * 4 - 1, m_CabinetYNum * 4 - 1].Centroid.Y, 2));

                    // カメラ姿勢の推定
                    estimateCamPos(aryBlob);


                    /*
                    // コーナーの最外の3D座標を求める
                    TransformImage.TransformImage transform = new TransformImage.TransformImage();

                    // 左上
                    float xPos = (float)allocInfo.lstUnits[1][1].CabinetPos.TopLeft.x;
                    float yPos = -(float)allocInfo.lstUnits[1][1].CabinetPos.TopLeft.y;         // -1倍して、左手座標系　→　右手座標系に変換    
                    float zPos = (float)allocInfo.lstUnits[1][1].CabinetPos.TopLeft.z;
                    transform.Set3DPoints(0, xPos, yPos, zPos);

                    // 右上
                    xPos = (float)allocInfo.lstUnits[1][1].CabinetPos.TopRight.x;
                    yPos = -(float)allocInfo.lstUnits[1][1].CabinetPos.TopRight.y;         // -1倍して、左手座標系　→　右手座標系に変換    
                    zPos = (float)allocInfo.lstUnits[1][1].CabinetPos.TopRight.z;
                    transform.Set3DPoints(1, xPos, yPos, zPos);

                    // 右下
                    xPos = (float)allocInfo.lstUnits[1][1].CabinetPos.BottomRight.x;
                    yPos = -(float)allocInfo.lstUnits[1][1].CabinetPos.BottomRight.y;         // -1倍して、左手座標系　→　右手座標系に変換    
                    zPos = (float)allocInfo.lstUnits[1][1].CabinetPos.BottomRight.z;
                    transform.Set3DPoints(2, xPos, yPos, zPos);

                    // 左下
                    xPos = (float)allocInfo.lstUnits[1][1].CabinetPos.BottomLeft.x;
                    yPos = -(float)allocInfo.lstUnits[1][1].CabinetPos.BottomLeft.y;         // -1倍して、左手座標系　→　右手座標系に変換    
                    zPos = (float)allocInfo.lstUnits[1][1].CabinetPos.BottomLeft.z;
                    transform.Set3DPoints(3, xPos, yPos, zPos);

                    // 基準の撮影画像座標
                    //transform.SetTranslation(m_Transration[0], m_Transration[1], 0);
                    transform.SetTranslation(m_Transration[0], -1534, 0);
                    transform.SetRx(m_Rotate[0]);
                    transform.SetRy(m_Rotate[1]);
                    transform.SetRz(m_Rotate[2]);

                    transform.SetShiftToCameraCoordinate(m_ShiftToCameraCoordinate[0], m_ShiftToCameraCoordinate[1], m_ShiftToCameraCoordinate[2]);
                    transform.CameraParameter.Set(m_CameraParam.f, m_CameraParam.SensorSizeH, m_CameraParam.SensorSizeV, m_CameraParam.SensorResH, m_CameraParam.SensorResV);
                    transform.Calc();

                    float[] TopLeft = new float[3];
                    float[] TopRight = new float[3];
                    float[] BottomRight = new float[3];
                    float[] BottomLeft = new float[3];

                    transform.GetMoved3DPoints(0, out TopLeft[0], out TopLeft[1], out TopLeft[2]);
                    transform.GetMoved3DPoints(1, out TopRight[0], out TopRight[1], out TopRight[2]);
                    transform.GetMoved3DPoints(2, out BottomRight[0], out BottomRight[1], out BottomRight[2]);
                    transform.GetMoved3DPoints(3, out BottomLeft[0], out BottomLeft[1], out BottomLeft[2]);
                    */

                    // カメラ中心を原点とした、各Cabinet位置の計算
                    //estimateCamPos()で、WD含んだCabinet座標は計算済み
                    MoveCabinetPos(0, m_Rotate[0], 0, 0, -m_Transration[1], 0);              // あおり撮影
                    MoveCabinetPos(-m_CamPos_Pan, m_CamPos_Tilt, -m_CamPos_Roll, 0, 0, 0);  // カメラ回転
                    MoveCabinetPos(0, 0, 0, m_CamPos_Tx, -m_CamPos_Ty, m_CamPos_Tz);        // カメラ移動

                    // 各Cabinetごとの規格の計算
                    calc_Spec_by_Zdistance();


                    canProgress = true;

                    // タイルを検出できたので、次回、黒/ラスターを計測しない
                    m_Enable_Capture_MaskImage = false;

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

                        // added by Hotta 2022/12/19
                        foreach (KeyValuePair<int, CvBlob> item in blobSatArea)
                        {
                            CvBlob blob = item.Value;
                            CvContourChainCode cc = blob.Contour;
                            cc.Render(matImageProcess, new Scalar(0, 0, 255));
                        }
                        //

                        matImageProcess.SaveImage(tempPath + "imageProcess.jpg");
                        // 画像を表示
                        DispImageFileUnlock(tempPath + "imageProcess.jpg", img, null, true);
                        //DoEvents();
                    }

                    // 規格に入っているかチェック
                    status = true;
                    // modified by Hotta 2022/11/14 for 外部入力されたCabinet配置
#if FlexibleCabinetPosition
                    if (!(tgtCamPos_HorLineSpec[0][0] < m_CamPos_TopLen && m_CamPos_TopLen < tgtCamPos_HorLineSpec[0][1]) ||  // 上辺
                        !(tgtCamPos_HorLineSpec[1][0] < m_CamPos_BottomLen && m_CamPos_BottomLen < tgtCamPos_HorLineSpec[1][1]) || // 下辺
                        !(tgtCamPos_VerLineSpec[0][0] < m_CamPos_LeftLen && m_CamPos_LeftLen < tgtCamPos_VerLineSpec[0][1]) || // 左辺
                        !(tgtCamPos_VerLineSpec[1][0] < m_CamPos_RightLen && m_CamPos_RightLen < tgtCamPos_VerLineSpec[1][1])) // 右辺
#else
                    if (!(tgtCamPos_HorLineSpec[0] < m_CamPos_TopLen && m_CamPos_TopLen < tgtCamPos_HorLineSpec[1]) ||  // 上辺
                        !(tgtCamPos_HorLineSpec[0] < m_CamPos_BottomLen && m_CamPos_BottomLen < tgtCamPos_HorLineSpec[1]) || // 下辺
                        !(tgtCamPos_VerLineSpec[0] < m_CamPos_LeftLen && m_CamPos_LeftLen < tgtCamPos_VerLineSpec[1])  || // 左辺
                        !(tgtCamPos_VerLineSpec[0] < m_CamPos_RightLen && m_CamPos_RightLen < tgtCamPos_VerLineSpec[1])) // 右辺
#endif
                        status = false;

                    // modified by Hotta 2022/07/29
                    /*
                    if (aryBlob[0, 0].Centroid.X < tgtCamPos_canUse.TopLeft.X || aryBlob[0, 0].Centroid.Y < tgtCamPos_canUse.TopLeft.Y ||   // 左上
                        aryBlob[m_CabinetXNum * 2 - 1, 0].Centroid.X > tgtCamPos_canUse.TopRight.X || aryBlob[m_CabinetXNum * 2 - 1, 0].Centroid.Y < tgtCamPos_canUse.TopRight.Y || // 右上
                        aryBlob[m_CabinetXNum * 2 - 1, m_CabinetYNum * 2 - 1].Centroid.X > tgtCamPos_canUse.BottomRight.X || aryBlob[m_CabinetXNum * 2 - 1, m_CabinetYNum * 2 - 1].Centroid.Y > tgtCamPos_canUse.BottomRight.Y || // 右下
                        aryBlob[0, m_CabinetYNum * 2 - 1].Centroid.X < tgtCamPos_canUse.BottomLeft.X || aryBlob[0, m_CabinetYNum * 2 - 1].Centroid.Y > tgtCamPos_canUse.BottomLeft.Y)   // 左下
                        status = false;
                    */
                    if (aryBlob[0, 0].Centroid.X < tgtCamPos_canUse.TopLeft.X || aryBlob[0, 0].Centroid.Y < tgtCamPos_canUse.TopLeft.Y ||   // 左上
                        aryBlob[m_CabinetXNum_CamPos * 2 - 1, 0].Centroid.X > tgtCamPos_canUse.TopRight.X || aryBlob[m_CabinetXNum_CamPos * 2 - 1, 0].Centroid.Y < tgtCamPos_canUse.TopRight.Y || // 右上
                        aryBlob[m_CabinetXNum_CamPos * 2 - 1, m_CabinetYNum_CamPos * 2 - 1].Centroid.X > tgtCamPos_canUse.BottomRight.X || aryBlob[m_CabinetXNum_CamPos * 2 - 1, m_CabinetYNum_CamPos * 2 - 1].Centroid.Y > tgtCamPos_canUse.BottomRight.Y || // 右下
                        aryBlob[0, m_CabinetYNum_CamPos * 2 - 1].Centroid.X < tgtCamPos_canUse.BottomLeft.X || aryBlob[0, m_CabinetYNum_CamPos * 2 - 1].Centroid.Y > tgtCamPos_canUse.BottomLeft.Y)   // 左下
                        status = false;


                    // added by Hotta 2023/02/06
                    // タイルは検出したが、ラスターがはみ出していないかチェック
                    if (status == true && m_Max_contours != null)
                    {
                        for (int n = 0; n < m_Max_contours.Length; n++)
                        {
                            if (m_Max_contours[n].X <= tgtCamPos_canUse.TopLeft.X || m_Max_contours[n].X <= tgtCamPos_canUse.BottomLeft.X)
                            {
                                status = false;
                                break;
                            }
                            if (m_Max_contours[n].X >= tgtCamPos_canUse.TopRight.X || m_Max_contours[n].X >= tgtCamPos_canUse.BottomRight.X)
                            {
                                status = false;
                                break;
                            }
                            if (m_Max_contours[n].Y <= tgtCamPos_canUse.TopLeft.Y || m_Max_contours[n].Y <= tgtCamPos_canUse.TopRight.Y)
                            {
                                status = false;
                                break;
                            }
                            if (m_Max_contours[n].Y >= tgtCamPos_canUse.BottomLeft.Y || m_Max_contours[n].Y >= tgtCamPos_canUse.BottomRight.Y)
                            {
                                status = false;
                                break;
                            }
                        }
                    }
                    //


                    if (status == true)
                    {
                        canProgress = true;
                        textboxGapCamPos.Foreground = System.Windows.Media.Brushes.Lime;
                        textboxGapCamPos.Text = "OK";
                    }
                    else
                    {
                        canProgress = false;
                        textboxGapCamPos.Foreground = System.Windows.Media.Brushes.Red;
                        textboxGapCamPos.Text = "NG";
                    }

                    //double[] aryValue = new double[] { Math.Abs(panDeg / 0.74), Math.Abs(tiltDeg / 0.6), Math.Abs(rollDeg / 0.6), Math.Abs(distX / 58), Math.Abs(distY / 58), Math.Abs(distZ / 48) };
                    // rollは感度を落とす
                    double[] aryValue = new double[] { Math.Abs(m_CamPos_Pan), Math.Abs(m_CamPos_Tilt), Math.Abs(m_CamPos_Roll / 2), Math.Abs(m_CamPos_Tx / 3), Math.Abs(m_CamPos_Ty / 3), Math.Abs(m_CamPos_Tz / 3) };

                    double max_value = 0;
                    int max_index = 0;

                    // added by Hotta 2022/08/03
                    // 200mm以上ずれていたら、Zを赤字にする
                    if(Math.Abs(m_CamPos_Tz) > 200.0)
                    {
                        max_index = 5;
                    }
                    //
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
                    else
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


                    // added by Hotta 2022/12/16 for ハンチング防止
                    bool hanting_X_Pan = false;
                    // deleted by Hotta 2023/03/03
                    /*
                    double hanting_X_limit = 10;
                    */
                    double hanting_X_k = 0.25;
                    // deleted by Hotta 2023/03/03
                    /*
                    double hanting_Pan_limit = 1.0;
                    */
                    double hanting_Pan_k = 0.25;

                    bool hanting_Y_Tilt = false;
                    // deleted by Hotta 2023/03/03
                    /*
                    double hanting_Y_limit = 10;
                    */
                    double hanting_Y_k = 0.25;
                    // deleted by Hotta 2023/03/03
                    /*
                    double hanting_Tilt_limit = 1.0;
                    */
                    double hanting_Tilt_k = 0.25;

                    System.Windows.Media.Brush goodHantingBrush = new SolidColorBrush(Colors.Teal);
                    System.Windows.Media.Brush badHantingBrush = new SolidColorBrush(Colors.Tomato);

                    if (m_PreventionHanting == true)
                    {
                        // Tx * Pan < -5 は、互いの符号が異なることを意味する
                        if (m_CamPos_Tx * m_CamPos_Pan < -5)
                        {
                            hanting_X_Pan = true;
                        }
                        // Ty * Tilt > 5 は、互いの符号が同じであることを意味する
                        if (m_CamPos_Ty * m_CamPos_Tilt > 5)
                        {
                            hanting_Y_Tilt = true;
                        }
                    }
                    //

                    // ●Pan
                    if (canProgress != true && max_index == 0)
                    {
                        if (hanting_X_Pan != true)
                            brush = badBrush;
                        else
                            brush = badHantingBrush;
                    }
                    else
                    {
                        if (hanting_X_Pan != true)
                            brush = goodBrush;
                        else
                            brush = goodHantingBrush;
                    }
                    if (hanting_X_Pan != true)
                        setText(txbGapCamPan, m_CamPos_Pan.ToString("+0.0;-0.0"), brush);
                    else
                        setText(txbGapCamPan, (m_CamPos_Pan  * hanting_Pan_k).ToString("+0.0;-0.0"), brush);

                    if (m_CamPos_Pan >= 0)
                        imgGapCamPan.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_2.gif"));
                    else
                        imgGapCamPan.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_4.gif"));

                    // ●Tilt
                    if (canProgress != true && max_index == 1)
                    {
                        if (hanting_Y_Tilt != true)
                            brush = badBrush;
                        else
                            brush = badHantingBrush;
                    }
                    else
                    {
                        if (hanting_Y_Tilt != true)
                            brush = goodBrush;
                        else
                            brush = goodHantingBrush;
                    }
                    if (hanting_Y_Tilt != true)
                        setText(txbGapCamTilt, m_CamPos_Tilt.ToString("+0.0;-0.0"), brush);
                    else
                        setText(txbGapCamTilt, (m_CamPos_Tilt * hanting_Tilt_k).ToString("+0.0;-0.0"), brush);
                    //

                    if (m_CamPos_Tilt >= 0)
                        imgGapCamTilt.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_1.gif"));
                    else
                        imgGapCamTilt.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_3.gif"));

                    // ●Roll
                    if (canProgress != true && max_index == 2)
                        brush = badBrush;
                    else
                        brush = goodBrush;
                    setText(txbGapCamRoll, m_CamPos_Roll.ToString("+0.0;-0.0"), brush);

                    if (m_CamPos_Roll >= 0)
                        imgGapCamRoll.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/RightTurn.png"));
                    else
                        imgGapCamRoll.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/LeftTurn.png"));

                    // ●X
                    if (canProgress != true && max_index == 3)
                    {
                        if (hanting_X_Pan != true)
                            brush = badBrush;
                        else
                            brush = badHantingBrush;
                    }
                    else
                    {
                        if (hanting_X_Pan != true)
                            brush = goodBrush;
                        else
                            brush = goodHantingBrush;
                    }
                    if (hanting_X_Pan != true)
                        setText(txbGapCamPosX, m_CamPos_Tx.ToString("+0.0;-0.0"), brush);
                    else
                        setText(txbGapCamPosX, (m_CamPos_Tx * hanting_X_k).ToString("+0.0;-0.0"), brush);
                    //

                    if (m_CamPos_Tx >= 0)
                        imgGapCamX.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_2.gif"));
                    else
                        imgGapCamX.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_4.gif"));

                    // ●Y
                    if (canProgress != true && max_index == 4)
                    {
                        if (hanting_Y_Tilt != true)
                            brush = badBrush;
                        else
                            brush = badHantingBrush;
                    }
                    else
                    {
                        if (hanting_Y_Tilt != true)
                            brush = goodBrush;
                        else
                            brush = goodHantingBrush;
                    }
                    if (hanting_Y_Tilt != true)
                        setText(txbGapCamPosY, m_CamPos_Ty.ToString("+0.0;-0.0"), brush);
                    else
                    {
                        setText(txbGapCamPosY, (m_CamPos_Ty * hanting_Y_k).ToString("+0.0;-0.0"), brush);
                    }
                    //

                    if (m_CamPos_Ty >= 0)
                        imgGapCamY.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_3.gif"));
                    else
                        imgGapCamY.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_1.gif"));

                    // ●Z
                    if (canProgress != true && max_index == 5)
                        brush = badBrush;
                    else
                        brush = goodBrush;
                    setText(txbGapCamPosZ, m_CamPos_Tz.ToString("+0.0;-0.0"), brush);

                    if (m_CamPos_Tz >= 0)
                        imgGapCamZ.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_1.gif"));
                    else
                        imgGapCamZ.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_3.gif"));



                    //if(Math.Abs(panDeg - m_LastPan) > 1.0f || Math.Abs(tiltDeg - m_LastTilt) > 1.0f || Math.Abs(rollDeg - m_LastRoll) > 1.0f ||
                    //    Math.Abs(distX - m_LastTx) > 1.0f || Math.Abs(distY - m_LastTy) > 1.0f || Math.Abs(distZ - m_LastTz) > 1.0f)
                    if (Math.Abs(m_CamPos_Pan - m_LastPan) > 3.0f || Math.Abs(m_CamPos_Tilt - m_LastTilt) > 3.0f || Math.Abs(m_CamPos_Roll - m_LastRoll) > 3.0f ||
                        Math.Abs(m_CamPos_Tx - m_LastTx) > 3.0f || Math.Abs(m_CamPos_Ty - m_LastTy) > 3.0f || Math.Abs(m_CamPos_Tz - m_LastTz) > 3.0f)
                    {
                        m_Enable_Capture_MaskImage = true;
                    }
                    m_LastPan = m_CamPos_Pan;
                    m_LastTilt = m_CamPos_Tilt;
                    m_LastRoll = m_CamPos_Roll;
                    m_LastTx = m_CamPos_Tx;
                    m_LastTy = m_CamPos_Ty;
                    m_LastTz = m_CamPos_Tz;
                }
                else
                {
                    if (txbGapCamPosX.Text != "")
                        txbGapCamPosX.Text = "";
                    if (txbGapCamPosY.Text != "")
                        txbGapCamPosY.Text = "";
                    if (txbGapCamPosZ.Text != "")
                        txbGapCamPosZ.Text = "";
                    if (txbGapCamPan.Text != "")
                        txbGapCamPan.Text = "";
                    if (txbGapCamTilt.Text != "")
                        txbGapCamTilt.Text = "";
                    if (txbGapCamRoll.Text != "")
                        txbGapCamRoll.Text = "";

                    if (imgGapCamPan.Source != null)
                        imgGapCamPan.Source = null;
                    if (imgGapCamTilt.Source != null)
                        imgGapCamTilt.Source = null;
                    if (imgGapCamRoll.Source != null)
                        imgGapCamRoll.Source = null;
                    if (imgGapCamX.Source != null)
                        imgGapCamX.Source = null;
                    if (imgGapCamY.Source != null)
                        imgGapCamY.Source = null;
                    if (imgGapCamZ.Source != null)
                        imgGapCamZ.Source = null;

                    // タイルを検出できなかったので、処理を中断し、次回、黒/ラスターを計測する
                    m_Enable_Capture_MaskImage = true;

                    canProgress = false; // 次のStep(Measure/Adjust)に進めない

                    bool over_top = false;
                    bool over_bottom = false;
                    bool over_left = false;
                    bool over_right = false;
                    status = true;

                    // added by Hotta 2022/10/04
                    if(m_Max_contours != null)
                    {
                        //
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
                    }
                    int over_count = 0;
                    if (over_top == true)
                        over_count++;
                    if (over_bottom == true)
                        over_count++;
                    if (over_left == true)
                        over_count++;
                    if (over_right == true)
                        over_count++;


                    if (over_count >= 2)
                    {
                        imgGapCamZ.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_3.gif"));
                    }
                    else if (over_left == true)
                    {
                        imgGapCamX.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_4.gif"));
                        imgGapCamPan.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_4.gif"));
                    }
                    else if (over_right == true)
                    {
                        imgGapCamX.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_2.gif"));
                        imgGapCamPan.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_2.gif"));
                    }
                    else if (over_top == true)
                    {
                        imgGapCamY.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_1.gif"));
                        imgGapCamTilt.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_1.gif"));
                    }
                    else if (over_bottom == true)
                    {
                        imgGapCamY.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_3.gif"));
                        imgGapCamTilt.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Arrow_3.gif"));
                    }

                    textboxGapCamPos.Foreground = System.Windows.Media.Brushes.Red;
                    textboxGapCamPos.Text = "NG";
                }

                // OKなら次の工程に進める
                if (appliMode == ApplicationMode.Developer || canProgress == true)
                {
                    btnGapCamMeasStart.IsEnabled = true;
                    btnGapCamAdjStart.IsEnabled = true;
                }
                else
                {
                    btnGapCamMeasStart.IsEnabled = false;
                    btnGapCamAdjStart.IsEnabled = false;
                }
            }
            catch //(Exception ex)
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
                // deleted by Hotta 2022/02/06
                // timerGapCamは、下にあるtimerと同じ
                /*
                // Timer Stop
                timerGapCam.Enabled = false;
                */


                // added by Hotta 2023/02/06
                // ボタンが解除されている場合は終了処理                
#if NO_CONTROLLER

#else
                SetThroughMode(false);

// added by Hotta 2023/04/28 for v1.06
                // ユーザー設定に書き戻し
                setUserSettingSetPos(userSetting);
                //

                // 内部信号OFF
                stopIntSig();
#endif
                //

                timer.Enabled = false;
            }
            else
            {
                timer.Enabled = true;
            }
        }


        private bool GetCameraPosition(System.Windows.Controls.Image img)
        {
            bool status = true;

            m_CamPos_BlackImagePath = tempPath + "black_CamPos";
            m_CamPos_RasterImagePath = tempPath + "raster_CamPos";
            m_CamPos_TileImagePath = tempPath + "tile_CamPos";

            CvBlobs blobs = null;

            m_Enable_Capture_MaskImage = true;

            try
            {
#if NO_CAP
#else
                captureCamPos(img);
#endif
                // modified by Hotta 2022/08/08
                // detectTileCamPos(out blobs);
                detectTileCamPos(out blobs, true);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            // modified by Hotta 2022/07/29
            // CvBlob[,] aryBlob = new CvBlob[m_CabinetXNum * 2, m_CabinetYNum * 2];
            CvBlob[,] aryBlob = new CvBlob[m_CabinetXNum_CamPos * 2, m_CabinetYNum_CamPos * 2];
            try
            {
                // modified by Hotta 2022/07/29
                //getTilePosition(blobs, m_CabinetXNum * 2, m_CabinetYNum * 2, out aryBlob);
                getTilePosition(blobs, m_CabinetXNum_CamPos * 2, m_CabinetYNum_CamPos * 2, out aryBlob);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            try
            {
                // 各辺の長さの算出
                // modified by Hotta 2022/07/29
                /*
                m_CamPos_TopLen = Math.Sqrt(
                    Math.Pow(aryBlob[0, 0].Centroid.X - aryBlob[m_CabinetXNum * 2 - 1, 0].Centroid.X, 2) +
                    Math.Pow(aryBlob[0, 0].Centroid.Y - aryBlob[m_CabinetXNum * 2 - 1, 0].Centroid.Y, 2));
                m_CamPos_BottomLen = Math.Sqrt(
                    Math.Pow(aryBlob[0, m_CabinetYNum * 2 - 1].Centroid.X - aryBlob[m_CabinetXNum * 2 - 1, m_CabinetYNum * 2 - 1].Centroid.X, 2) +
                    Math.Pow(aryBlob[0, m_CabinetYNum * 2 - 1].Centroid.Y - aryBlob[m_CabinetXNum * 2 - 1, m_CabinetYNum * 2 - 1].Centroid.Y, 2));
                m_CamPos_LeftLen = Math.Sqrt(
                    Math.Pow(aryBlob[0, 0].Centroid.X - aryBlob[0, m_CabinetYNum * 2 - 1].Centroid.X, 2) +
                    Math.Pow(aryBlob[0, 0].Centroid.Y - aryBlob[0, m_CabinetYNum * 2 - 1].Centroid.Y, 2));
                m_CamPos_RightLen = Math.Sqrt(
                    Math.Pow(aryBlob[m_CabinetXNum * 2 - 1, 0].Centroid.X - aryBlob[m_CabinetXNum * 2 - 1, m_CabinetYNum * 2 - 1].Centroid.X, 2) +
                    Math.Pow(aryBlob[m_CabinetXNum * 2 - 1, 0].Centroid.Y - aryBlob[m_CabinetXNum * 2 - 1, m_CabinetYNum * 2 - 1].Centroid.Y, 2));
                */
                    m_CamPos_TopLen = Math.Sqrt(
                    Math.Pow(aryBlob[0, 0].Centroid.X - aryBlob[m_CabinetXNum_CamPos * 2 - 1, 0].Centroid.X, 2) +
                    Math.Pow(aryBlob[0, 0].Centroid.Y - aryBlob[m_CabinetXNum_CamPos * 2 - 1, 0].Centroid.Y, 2));
                m_CamPos_BottomLen = Math.Sqrt(
                    Math.Pow(aryBlob[0, m_CabinetYNum_CamPos * 2 - 1].Centroid.X - aryBlob[m_CabinetXNum_CamPos * 2 - 1, m_CabinetYNum_CamPos * 2 - 1].Centroid.X, 2) +
                    Math.Pow(aryBlob[0, m_CabinetYNum_CamPos * 2 - 1].Centroid.Y - aryBlob[m_CabinetXNum_CamPos * 2 - 1, m_CabinetYNum_CamPos * 2 - 1].Centroid.Y, 2));
                m_CamPos_LeftLen = Math.Sqrt(
                    Math.Pow(aryBlob[0, 0].Centroid.X - aryBlob[0, m_CabinetYNum_CamPos * 2 - 1].Centroid.X, 2) +
                    Math.Pow(aryBlob[0, 0].Centroid.Y - aryBlob[0, m_CabinetYNum_CamPos * 2 - 1].Centroid.Y, 2));
                m_CamPos_RightLen = Math.Sqrt(
                    Math.Pow(aryBlob[m_CabinetXNum_CamPos * 2 - 1, 0].Centroid.X - aryBlob[m_CabinetXNum_CamPos * 2 - 1, m_CabinetYNum_CamPos * 2 - 1].Centroid.X, 2) +
                    Math.Pow(aryBlob[m_CabinetXNum_CamPos * 2 - 1, 0].Centroid.Y - aryBlob[m_CabinetXNum_CamPos * 2 - 1, m_CabinetYNum_CamPos * 2 - 1].Centroid.Y, 2));


                // カメラ姿勢の推定
                estimateCamPos(aryBlob);

                // カメラ中心を原点とした、各Cabinet位置の計算
                //estimateCamPos()で、WD含んだCabinet座標は計算済み
                MoveCabinetPos(0, m_Rotate[0], 0, 0, -m_Transration[1], 0);              // あおり撮影
                MoveCabinetPos(-m_CamPos_Pan, m_CamPos_Tilt, -m_CamPos_Roll, 0, 0, 0);  // カメラ回転
                MoveCabinetPos(0, 0, 0, m_CamPos_Tx, -m_CamPos_Ty, m_CamPos_Tz);        // カメラ移動

                // 各Cabinetごとの規格の計算
                calc_Spec_by_Zdistance();

#if TempFile
                /*
                using (StreamWriter sw = new StreamWriter(applicationPath + "\\Temp\\CabinetPos.csv"))
                {
                    string str = "";

                    str = ",";
                    for (int x = 0; x < allocInfo.MaxX; x++)
                        str += (x + 1).ToString() + ",,,";
                    sw.WriteLine(str);

                    str = ",";
                    for (int x = 0; x < allocInfo.MaxX; x++)
                        str += "x,y,z,";
                    sw.WriteLine(str);

                    for (int y=0;y<allocInfo.MaxY;y++)
                    {
                        str = (y + 1).ToString() + ",";
                        for (int x = 0; x < allocInfo.MaxX; x++)
                        {
                            double _x, _y, _z;
                            _x = (allocInfo.lstUnits[x][y].CabinetPos.TopLeft.x + allocInfo.lstUnits[x][y].CabinetPos.TopRight.x + allocInfo.lstUnits[x][y].CabinetPos.BottomRight.x + allocInfo.lstUnits[x][y].CabinetPos.BottomLeft.x) / 4;
                            _y = (allocInfo.lstUnits[x][y].CabinetPos.TopLeft.y + allocInfo.lstUnits[x][y].CabinetPos.TopRight.y + allocInfo.lstUnits[x][y].CabinetPos.BottomRight.y + allocInfo.lstUnits[x][y].CabinetPos.BottomLeft.y) / 4;
                            _z = (allocInfo.lstUnits[x][y].CabinetPos.TopLeft.z + allocInfo.lstUnits[x][y].CabinetPos.TopRight.z + allocInfo.lstUnits[x][y].CabinetPos.BottomRight.z + allocInfo.lstUnits[x][y].CabinetPos.BottomLeft.z) / 4;
                            str += _x.ToString() + "," + _y.ToString() + "," + _z.ToString() + ",";
                        }
                        sw.WriteLine(str);
                    }
                }
                */
#endif



                // 規格に入っているかチェック
                status = true;
                // modified by Hotta 2022/11/14 for 外部入力されたCabinet配置
#if FlexibleCabinetPosition
                if (!(tgtCamPos_HorLineSpec[0][0] < m_CamPos_TopLen && m_CamPos_TopLen < tgtCamPos_HorLineSpec[0][1]) ||  // 上辺
                    !(tgtCamPos_HorLineSpec[1][0] < m_CamPos_BottomLen && m_CamPos_BottomLen < tgtCamPos_HorLineSpec[1][1]) || // 下辺
                    !(tgtCamPos_VerLineSpec[0][0] < m_CamPos_LeftLen && m_CamPos_LeftLen < tgtCamPos_VerLineSpec[0][1]) || // 左辺
                    !(tgtCamPos_VerLineSpec[1][0] < m_CamPos_RightLen && m_CamPos_RightLen < tgtCamPos_VerLineSpec[1][1])) // 右辺
#else
                if (!(tgtCamPos_HorLineSpec[0] < m_CamPos_TopLen && m_CamPos_TopLen < tgtCamPos_HorLineSpec[1]) ||  // 上辺
                    !(tgtCamPos_HorLineSpec[0] < m_CamPos_BottomLen && m_CamPos_BottomLen < tgtCamPos_HorLineSpec[1]) || // 下辺
                    !(tgtCamPos_VerLineSpec[0] < m_CamPos_LeftLen && m_CamPos_LeftLen < tgtCamPos_VerLineSpec[1]) || // 左辺
                    !(tgtCamPos_VerLineSpec[0] < m_CamPos_RightLen && m_CamPos_RightLen < tgtCamPos_VerLineSpec[1])) // 右辺
#endif
                    status = false;
                // modified by Hotta 2022/07/29
                /*
                if (aryBlob[0, 0].Centroid.X < tgtCamPos_canUse.TopLeft.X || aryBlob[0, 0].Centroid.Y < tgtCamPos_canUse.TopLeft.Y ||   // 左上
                    aryBlob[m_CabinetXNum * 2 - 1, 0].Centroid.X > tgtCamPos_canUse.TopRight.X || aryBlob[m_CabinetXNum * 2 - 1, 0].Centroid.Y < tgtCamPos_canUse.TopRight.Y || // 右上
                    aryBlob[m_CabinetXNum * 2 - 1, m_CabinetYNum * 2 - 1].Centroid.X > tgtCamPos_canUse.BottomRight.X || aryBlob[m_CabinetXNum * 2 - 1, m_CabinetYNum * 2 - 1].Centroid.Y > tgtCamPos_canUse.BottomRight.Y || // 右下
                    aryBlob[0, m_CabinetYNum * 2 - 1].Centroid.X < tgtCamPos_canUse.BottomLeft.X || aryBlob[0, m_CabinetYNum * 2 - 1].Centroid.Y > tgtCamPos_canUse.BottomLeft.Y)   // 左下
                    status = false;
                */
                if (aryBlob[0, 0].Centroid.X < tgtCamPos_canUse.TopLeft.X || aryBlob[0, 0].Centroid.Y < tgtCamPos_canUse.TopLeft.Y ||   // 左上
                    aryBlob[m_CabinetXNum_CamPos * 2 - 1, 0].Centroid.X > tgtCamPos_canUse.TopRight.X || aryBlob[m_CabinetXNum_CamPos * 2 - 1, 0].Centroid.Y < tgtCamPos_canUse.TopRight.Y || // 右上
                    aryBlob[m_CabinetXNum_CamPos * 2 - 1, m_CabinetYNum_CamPos * 2 - 1].Centroid.X > tgtCamPos_canUse.BottomRight.X || aryBlob[m_CabinetXNum_CamPos * 2 - 1, m_CabinetYNum_CamPos * 2 - 1].Centroid.Y > tgtCamPos_canUse.BottomRight.Y || // 右下
                    aryBlob[0, m_CabinetYNum_CamPos * 2 - 1].Centroid.X < tgtCamPos_canUse.BottomLeft.X || aryBlob[0, m_CabinetYNum_CamPos * 2 - 1].Centroid.Y > tgtCamPos_canUse.BottomLeft.Y)   // 左下
                    status = false;

                /*
                // added by Hotta 2022/12/06
                // Cabinet座標を再設定
                MoveCabinetPos(0, 0, 0, 0, 0, 0);
                */

            }
            catch //(Exception ex)
            {
                throw new Exception("Fail to estimate camera position. Please set camera again.");
            }
            return status;
        }


        /// <summary>
        /// 現在メッセージ待ち行列の中にある全てのUIメッセージを処理します。
        /// </summary>
        private void DoEvents()
        {
            DispatcherFrame frame = new DispatcherFrame();
            var callback = new DispatcherOperationCallback(obj =>
            {
                ((DispatcherFrame)obj).Continue = false;
                return null;
            });
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, callback, frame);
            Dispatcher.PushFrame(frame);
        }

        // added by Hotta 2022/11/28 for 自動リトライ
#if Retry_NotChangeLastImage
        Mat matTile_CamPos;
        Mat matBlack_CamPos;
        Mat matRaster_CamPos;
#endif


        // modified by Hotta 2022/08/08
        // void captureCamPos(System.Windows.Controls.Image img)
        void captureCamPos(System.Windows.Controls.Image img, bool patternWait = true)
        {
            // deleted by Hotta 2023/03/03
            /*
            int startX, startY;
            int maxX, maxY;
            int width, height;
            int pitchH, pitchV;
            */

            // added by Hotta 2023/04/28
#if NO_CONTROLLER
            int startX, startY;
            int maxX, maxY;
            int width, height;
            int pitchH, pitchV;
#endif


            ShootCondition condition = new ShootCondition();

            condition.CompressionType = 2;
            condition.FNumber = Settings.Ins.GapCam.Setting_A6400.FNumber;

            // modified by Hotta 2022/12/16
            // カメラ位置合わせの時はSサイズ固定
            //condition.ImageSize = Settings.Ins.GapCam.Setting_A6400.ImageSize;
            condition.ImageSize = 3;
            //
            condition.ISO = Settings.Ins.GapCam.Setting_A6400.ISO;
#if NO_CONTROLLER
            condition.ISO = "1600";
#endif
            condition.Shutter = Settings.Ins.GapCam.Setting_A6400.Shutter;
            condition.WB = Settings.Ins.GapCam.Setting_A6400.WB;

            // deleted by Hotta 2023/03/03
            /*
            m_CalcCameraPosition = false;
            */

            // added by Hotta 2022/11/28 for 自動リトライ
#if Retry_NotChangeLastImage
            if (matTile_CamPos != null)
            {
                matTile_CamPos.Dispose();
                matTile_CamPos = null;
            }
            if (matBlack_CamPos != null)
            {
                matBlack_CamPos.Dispose();
                matBlack_CamPos = null;
            }
            if (matRaster_CamPos != null)
            {
                matRaster_CamPos.Dispose();
                matRaster_CamPos = null;
            }
#endif

            // 撮影と計算
            // deleted by Hotta 2023/03/03
            /*
            bool status = true;
            */

            int tileSize = 32;

            // added by Hotta 2022/12/16
            int imageWidth = m_CameraParam.SensorResH;
            int imageHeight = m_CameraParam.SensorResV;
            //

#if Retry_NotChangeLastImage
            int retryNum = Settings.Ins.GapCam.CamPos_RetryNum;
#endif

            if (m_Enable_Capture_MaskImage == true)
            {
                // added by Hotta 2022/09/15
#if ShowPattern_CamPos
                if (Settings.Ins.ExecLog == true)
                    MainWindow.SaveExecLog("BLACK command will be sent.");
                Dispatcher.Invoke(new Action(() => { txtbStatus.Text = "BLACK command will be sent."; }));
                DoEvents();
#endif

#if NO_CONTROLLER
                outputMonitorWindow(0, 0, 1080, 1920, 0, 0, 0);
#else
                outputFlatPattern(0, 0, 0);
#endif

#if ShowPattern_CamPos
                if (Settings.Ins.ExecLog == true)
                    MainWindow.SaveExecLog("BLACK command sent.");
                Dispatcher.Invoke(new Action(() => { txtbStatus.Text = "BLACK command sent."; }));
                DoEvents();
#endif

                // modified by Hotta 2022/09/30
#if ShowPattern_CamPos
                Thread.Sleep(Settings.Ins.Camera.PatternWait_CamPos);
#else
                // modified by Hotta 2022/08/08
                //Thread.Sleep(100);
                Thread.Sleep(Settings.Ins.Camera.PatternWait / 10);
#endif
                // added by Hotta 2022/08/08
                if (patternWait == true)
                    Thread.Sleep(Settings.Ins.Camera.PatternWait);
                //

#if NO_CAP
#else
                // added by Hotta 2022/12/16
                if(m_ImageType_CamPos == ImageType_CamPos.LiveView)
                {
                    LiveView(m_CamPos_BlackImagePath, condition/*, true*/);
                }
                else
                {
                    CaptureImage(m_CamPos_BlackImagePath, condition);
                    Thread.Sleep(Settings.Ins.Camera.CameraWait);
                }
#endif

                // added by Hotta 2022/11/28 for 自動リトライ
                // タイル → 全黒になっていることを確認
#if Retry_NotChangeLastImage
                Mat matF;

                // タイル画像があったら取得
                if (File.Exists(m_CamPos_TileImagePath + ".jpg") == true)
                {
                    if(matTile_CamPos != null)
                    {
                        matTile_CamPos.Dispose();
                        matTile_CamPos = null;
                    }
                    matTile_CamPos = new Mat(m_CamPos_TileImagePath + ".jpg");
                    if (matTile_CamPos.Width == imageWidth && matTile_CamPos.Height == imageHeight)
                    {
                        matF = new Mat();
                        matTile_CamPos.ConvertTo(matF, MatType.CV_32FC3, 1.0 / 255);
                        matTile_CamPos.Dispose();
                        matTile_CamPos = new Mat();
                        Cv2.Pow(matF, 2.2, matF);
                        matF *= 255;
                        matF.ConvertTo(matTile_CamPos, MatType.CV_8UC3);
                        matF.Dispose();
                    }
                    else
                    {
                        matTile_CamPos.Dispose();
                        matTile_CamPos = null;
                    }
                }

                for (int retry = 0; retry < retryNum; retry++)
                {
                    // 黒画像の取得
                    for (int i = 0; i < 300; i++)
                    {
                        FileStream stream = null;
                        try { stream = new FileStream(m_CamPos_BlackImagePath + ".jpg", FileMode.Open, FileAccess.ReadWrite, FileShare.None); }
                        catch/*(Exception ex)*/ { Thread.Sleep(10); continue; }
                        finally { if(stream != null) { stream.Close(); } }

                        matBlack_CamPos = new Mat(m_CamPos_BlackImagePath + ".jpg");
                        if (matBlack_CamPos.Width == imageWidth && matBlack_CamPos.Height == imageHeight)
                            break;
                        else
                        {
                            matBlack_CamPos.Dispose();
                            matBlack_CamPos = null;
                            Thread.Sleep(10);
                        }
                    }

                    // 黒画像の処理
                    if(matBlack_CamPos != null)
                    {
                        matF = new Mat();
                        matBlack_CamPos.ConvertTo(matF, MatType.CV_32FC3, 1.0 / 255);
                        matBlack_CamPos.Dispose();
                        matBlack_CamPos = new Mat();
                        Cv2.Pow(matF, 2.2, matF);
                        matF *= 255;
                        matF.ConvertTo(matBlack_CamPos, MatType.CV_8UC3);
                        matF.Dispose();
                    }

                    // タイルと黒があったら、チェック
                    if (matBlack_CamPos != null && matTile_CamPos != null)
                    {
                        // タイル表示のままなら、ブロブは検出されない。
                        // 全黒表示になっていたら、ブロブを検出する。
                        Mat matDiff = matTile_CamPos - matBlack_CamPos;
                        Mat matMono = new Mat();
                        Cv2.CvtColor(matDiff, matMono, ColorConversionCodes.BGR2GRAY);
                        Mat matBin = new Mat();
                        //Cv2.Threshold(matMono, matBin, 0, 255, ThresholdTypes.Otsu);
                        Cv2.Threshold(matMono, matBin, 25, 255, ThresholdTypes.Binary); // Otsuだと、対象が全黒でも、少し明るいところを検出してしまうので
#if TempFile
                        matBin.SaveImage(applicationPath + "\\Temp\\Tile_Black_CamPos.jpg");
#endif
                        CvBlobs blobs = new CvBlobs(matBin);
                        blobs.FilterByArea(4, int.MaxValue);

                        matBin.Dispose();
                        matMono.Dispose();
                        matDiff.Dispose();
                        //matTile_CamPos.Dispose();
                        //matTile_CamPos = null;
                        // matBlackは、後で使うため、Dispose()しない。

                        if (blobs.Count >= m_lstCamPosUnits.Count * 4)
                            break;
                    }
                    // 動確用
                    //outputMonitorWindow(0, 0, 1080, 1920, 0, 0, 0);
                    //

                    Thread.Sleep(Settings.Ins.Camera.PatternWait_CamPos);
#if NO_CAP
#else
                    // added by Hotta 2022/12/16
                    if(m_ImageType_CamPos == ImageType_CamPos.LiveView)
                    {
                        LiveView(m_CamPos_BlackImagePath, condition/*, true*/);
                    }
                    else
                    {
                        CaptureImage(m_CamPos_BlackImagePath);
                        Thread.Sleep(Settings.Ins.Camera.CameraWait);
                    }
#endif
                }
#endif

                DispImageFileUnlock(m_CamPos_BlackImagePath + ".jpg", img, null, true);
                DoEvents();

#if ShowPattern_CamPos
                if (Settings.Ins.ExecLog == true)
                    MainWindow.SaveExecLog("RASTER command will be sent.");
                Dispatcher.Invoke(new Action(() => { txtbStatus.Text = "RASTER command will be sent."; }));
                DoEvents();
#endif

#if NO_CONTROLLER
                startX = int.MaxValue; startY = int.MaxValue; maxX = 0; maxY = 0;
                height = 0; width = 0;

                foreach (UnitInfo unit in m_lstCamPosUnits)
                {
                    if (unit.PixelX < startX)
                    { startX = unit.PixelX; }

                    if (unit.PixelY < startY)
                    { startY = unit.PixelY; }

                    if (unit.PixelX > maxX)
                    { maxX = unit.PixelX; }

                    if (unit.PixelY > maxY)
                    { maxY = unit.PixelY; }
                }

                height = (maxY - startY + cabiDy) * 1080 / 2160;
                width = (maxX - startX + cabiDx) * 1920 / 3840;

                // modified by Hotta 2022/09/13
                // outputMonitorWindow(0, 0, height, width, 0, 255, 0);
                outputMonitorWindow(0, 1080 - height, height, width, 0, 255, 0);
#else

                // modified by Hotta 2022/10/26 for 複数コントローラ
#if MultiController
                OutputTargetArea(m_lstCamPosUnits, true);
#else
                // modified by Hotta 2022/06/23 for カメラ位置
                //outputGapCamTargetArea(lstTgtUnit);
                outputGapCamTargetArea(m_lstCamPosUnits);
#endif

#endif

#if ShowPattern_CamPos
                if (Settings.Ins.ExecLog == true)
                    MainWindow.SaveExecLog("RASTER command sent.");
                Dispatcher.Invoke(new Action(() => { txtbStatus.Text = "RASTER command sent."; }));
                DoEvents();
#endif
                // modified by Hotta 2022/09/30
#if ShowPattern_CamPos
                Thread.Sleep(Settings.Ins.Camera.PatternWait_CamPos);
#else
                // modified by Hotta 2022/08/08
                //Thread.Sleep(100);
                Thread.Sleep(Settings.Ins.Camera.PatternWait / 10);
#endif
                // added by Hotta 2022/08/08
                if (patternWait == true)
                    Thread.Sleep(Settings.Ins.Camera.PatternWait);
                //

#if NO_CAP
#else
                // added by Hotta 2022/12/15
                if(m_ImageType_CamPos == ImageType_CamPos.LiveView)
                {
                    LiveView(m_CamPos_RasterImagePath, condition/*, true*/);
                }
                else
                {
                    CaptureImage(m_CamPos_RasterImagePath, condition);
                    Thread.Sleep(Settings.Ins.Camera.CameraWait);
                }
#endif

                // added by Hotta 2022/11/28 for 自動リトライ
                // 全黒 → ラスターになっていることを確認
#if Retry_NotChangeLastImage
                for (int retry = 0; retry < retryNum; retry++)
                {
                    for (int i = 0; i < 300; i++)
                    {
                        FileStream stream = null;
                        try { stream = new FileStream(m_CamPos_RasterImagePath + ".jpg", FileMode.Open, FileAccess.ReadWrite, FileShare.None); }
                        catch /*(Exception ex)*/ { Thread.Sleep(10); continue; }
                        finally { if (stream != null) { stream.Close(); } }

                        if (matRaster_CamPos != null)
                        {
                            matRaster_CamPos.Dispose();
                            matRaster_CamPos = null;
                        }
                        matRaster_CamPos = new Mat(m_CamPos_RasterImagePath + ".jpg");
                        if (matRaster_CamPos.Width == imageWidth && matRaster_CamPos.Height == imageHeight)
                            break;
                        else
                        {
                            matRaster_CamPos.Dispose();
                            matRaster_CamPos = null;
                            Thread.Sleep(10);
                        }
                    }

                    if(matRaster_CamPos != null)
                    {
                        matF = new Mat();
                        matRaster_CamPos.ConvertTo(matF, MatType.CV_32FC3, 1.0 / 255);
                        matRaster_CamPos.Dispose();
                        matRaster_CamPos = new Mat();
                        Cv2.Pow(matF, 2.2, matF);
                        matF *= 255;
                        matF.ConvertTo(matRaster_CamPos, MatType.CV_8UC3);
                        matF.Dispose();
                    }

                    if(matBlack_CamPos != null && matRaster_CamPos != null)
                    {
                        // 全黒表示のままなら、二値化した白の面積はほとんどゼロ。
                        // ラスターになっていたら、二値化した白の面積は一定量ある。
                        Mat matMask = matRaster_CamPos - matBlack_CamPos;
                        Mat matMono = new Mat();
                        Cv2.CvtColor(matMask, matMono, ColorConversionCodes.BGR2GRAY);
                        Mat matBin = new Mat();
                        //Cv2.Threshold(matMono, matBin, 0, 255, ThresholdTypes.Otsu);
                        Cv2.Threshold(matMono, matBin, 25, 255, ThresholdTypes.Binary); // Otsuだと、対象が全黒でも、少し明るいところを検出してしまうので
#if TempFile
                        matBin.SaveImage(applicationPath + "\\Temp\\Black_Raster_CamPos.jpg");
#endif
                        int whitePix = Cv2.CountNonZero(matBin);
                        int allPix = matBin.Width * matBin.Height;

                        matBin.Dispose();
                        matMono.Dispose();
                        matMask.Dispose();
                        //matBlack_CamPos.Dispose();   後で使う
                        //matRaster_CamPos.Dispose();   後で使う

                        if (whitePix >= 0.1 * allPix)
                            break;
                    }

                    // 動確用
                    //outputMonitorWindow(0, 1080 - height, height, width, 0, 255, 0);
                    //

                    Thread.Sleep(Settings.Ins.Camera.PatternWait_CamPos);
#if NO_CAP
#else
                    // added by Hotta 2022/12/15
                    if(m_ImageType_CamPos == ImageType_CamPos.LiveView)
                    {
                        LiveView(m_CamPos_RasterImagePath, condition/*, true*/);
                    }
                    else
                    {
                        CaptureImage(m_CamPos_RasterImagePath, condition);
                        Thread.Sleep(Settings.Ins.Camera.CameraWait);
                    }
#endif
                }
#endif

                DispImageFileUnlock(m_CamPos_RasterImagePath + ".jpg", img, null, true);
                DoEvents();
            }
#if ShowPattern_CamPos
            else
            {
                if (matBlack_CamPos != null)
                {
                    matBlack_CamPos.Dispose();
                    matBlack_CamPos = null;
                }
                matBlack_CamPos = new Mat(m_CamPos_BlackImagePath + ".jpg");
                Mat matF = new Mat();
                matBlack_CamPos.ConvertTo(matF, MatType.CV_32FC3, 1.0 / 255);
                matBlack_CamPos.Dispose();
                matBlack_CamPos = new Mat();
                Cv2.Pow(matF, 2.2, matF);
                matF *= 255;
                matF.ConvertTo(matBlack_CamPos, MatType.CV_8UC3);
                matF.Dispose();

                if (matRaster_CamPos != null)
                {
                    matRaster_CamPos.Dispose();
                    matRaster_CamPos = null;
                }
                matRaster_CamPos = new Mat(m_CamPos_RasterImagePath + ".jpg");
                matF = new Mat();
                matRaster_CamPos.ConvertTo(matF, MatType.CV_32FC3, 1.0 / 255);
                matRaster_CamPos.Dispose();
                matRaster_CamPos = new Mat();
                Cv2.Pow(matF, 2.2, matF);
                matF *= 255;
                matF.ConvertTo(matRaster_CamPos, MatType.CV_8UC3);
                matF.Dispose();
            }
#endif

#if ShowPattern_CamPos
            if (Settings.Ins.ExecLog == true)
                MainWindow.SaveExecLog("TILE command will be sent.");
            Dispatcher.Invoke(new Action(() => { txtbStatus.Text = "TILE command will be sent."; }));
            DoEvents();
#endif

#if NO_CONTROLLER
            //startX = cabiDx / 8 - tileSize / 2;
            //startY = cabiDy / 8 - tileSize / 2;
            //width = tileSize;
            //height = tileSize;
            //pitchH = cabiDx * 2 / 8;
            //pitchV = cabiDy * 2 / 8;

            startX = cabiDx / 4 - tileSize / 2;
            startY = cabiDy / 4 - tileSize / 2;
            width = tileSize;
            height = tileSize;
            pitchH = cabiDx * 2 / 4;
            pitchV = cabiDy * 2 / 4;

            startX /= 2;
            startY /= 2;
            width /= 2;
            height /= 2;
            pitchH /= 2;
            pitchV /= 2;

            outputMonitorTile(startX, startY, height, width, pitchH, pitchV, 0, 255, 0);
#else
            outputIntSigHatch(cabiDx / 4 - tileSize / 2, cabiDy / 4 - tileSize / 2, tileSize, tileSize, cabiDx * 2 / 4, cabiDy * 2 / 4, 0, m_MeasureLevel, 0);
            //outputIntSigHatch(cabiDx / 8 - tileSize / 2, cabiDy / 8 - tileSize / 2, tileSize, tileSize, cabiDx * 2 / 8, cabiDy * 2 / 8, 0, 255, 0);

#endif

#if ShowPattern_CamPos
            if (Settings.Ins.ExecLog == true)
                MainWindow.SaveExecLog("TILE command sent.");
            Dispatcher.Invoke(new Action(() => { txtbStatus.Text = "TILE command sent."; }));
            DoEvents();
#endif
            // modified by Hotta 2022/09/30
#if ShowPattern_CamPos
            Thread.Sleep(Settings.Ins.Camera.PatternWait_CamPos);
#else
                // modified by Hotta 2022/08/08
                //Thread.Sleep(100);
                Thread.Sleep(Settings.Ins.Camera.PatternWait / 10);
#endif
            // added by Hotta 2022/08/08
            if (patternWait == true)
                Thread.Sleep(Settings.Ins.Camera.PatternWait);
            //
#if NO_CAP
#else
            // added by Hotta 2022/12/15
            if(m_ImageType_CamPos == ImageType_CamPos.LiveView)
            {
                LiveView(m_CamPos_TileImagePath, condition/*, true*/);
            }
            else
            {
                CaptureImage(m_CamPos_TileImagePath);
                Thread.Sleep(Settings.Ins.Camera.CameraWait);
            }
#endif

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            // added by Hotta 2022/11/28 for 自動リトライ
            // ラスター → タイルになっていることを確認
#if Retry_NotChangeLastImage
            for (int retry = 0; retry < retryNum; retry++)
            {
                for(int i=0;i<300;i++)
                {
                    FileStream stream = null;
                    try { stream = new FileStream(m_CamPos_TileImagePath + ".jpg", FileMode.Open, FileAccess.ReadWrite, FileShare.None); }
                    catch /*(Exception ex)*/ { Thread.Sleep(10); continue; }
                    finally { if (stream != null) { stream.Close(); } }

                    if (matTile_CamPos != null)
                    {
                        matTile_CamPos.Dispose();
                        matTile_CamPos = null;
                    }
                    matTile_CamPos = new Mat(m_CamPos_TileImagePath + ".jpg");
                    if (matTile_CamPos.Width == imageWidth && matTile_CamPos.Height == imageHeight)
                        break;
                    else
                    {
                        matTile_CamPos.Dispose();
                        matTile_CamPos = null;
                        Thread.Sleep(10);
                    }
                }
                if(matTile_CamPos != null)
                {
                    Mat matF = new Mat();
                    matTile_CamPos.ConvertTo(matF, MatType.CV_32FC3, 1.0 / 255);
                    matTile_CamPos.Dispose();
                    matTile_CamPos = new Mat();
                    Cv2.Pow(matF, 2.2, matF);
                    matF *= 255;
                    matF.ConvertTo(matTile_CamPos, MatType.CV_8UC3);
                    matF.Dispose();
                }

                if(matBlack_CamPos != null && matRaster_CamPos != null && matTile_CamPos != null)
                {
                    // ラスター表示のままなら、ブロブの個数が不足する。
                    // タイルになっていたら、ブロブの個数>=Cabinet数 x 4 となる。
                    Mat matMask = matRaster_CamPos - matBlack_CamPos;
                    Mat matMaskMono = new Mat();
                    Cv2.CvtColor(matMask, matMaskMono, ColorConversionCodes.BGR2GRAY);
                    Mat matMaskBin = new Mat();
                    Cv2.Threshold(matMaskMono, matMaskBin, 0, 255, ThresholdTypes.Otsu);

                    Mat matTile2 = matTile_CamPos - matBlack_CamPos;
                    Mat matTileMono = new Mat();
                    Cv2.CvtColor(matTile2, matTileMono, ColorConversionCodes.BGR2GRAY);
                    Cv2.BitwiseAnd(matTileMono, matMaskBin, matTileMono);
                    Mat matTileBin = new Mat();
                    Cv2.Threshold(matTileMono, matTileBin, 0, 255, ThresholdTypes.Otsu);

#if TempFile
                    matTileBin.SaveImage(applicationPath + "\\Temp\\Raster_Tile_CamPos.jpg");
#endif
                    CvBlobs blobs = new CvBlobs(matTileBin);

                    matTileBin.Dispose();
                    matTileMono.Dispose();
                    matMaskBin.Dispose();
                    matMaskMono.Dispose();
                    matMask.Dispose();
                    matTile2.Dispose();

                    if (blobs.Count >= m_lstCamPosUnits.Count * 4)
                        break;
                }

                // 動確用
                //outputMonitorTile(startX, startY, height, width, pitchH, pitchV, 0, 255, 0);
                //

                Thread.Sleep(Settings.Ins.Camera.PatternWait_CamPos);
#if NO_CAP
#else

                // added by Hotta 2022/12/16
                if(m_ImageType_CamPos == ImageType_CamPos.LiveView)
                {
                    LiveView(m_CamPos_TileImagePath, condition/*, true*/);
                }
                else
                {
                    CaptureImage(m_CamPos_TileImagePath);
                    Thread.Sleep(Settings.Ins.Camera.CameraWait);
                }
#endif
            }

            /*
            for(int i=0;i<30;i++)
            {
                LiveView(m_CamPos_TileImagePath + "_" + i.ToString(), condition);
            }
            */
#endif

            sw.Stop();

            DispImageFileUnlock(m_CamPos_TileImagePath + ".jpg", img, null, true);
            DoEvents();

            // added by Hotta 2022/11/28 for 自動リトライ
#if Retry_NotChangeLastImage
            if (matTile_CamPos != null)
            {
                matTile_CamPos.Dispose();
                matTile_CamPos = null;
            }
            if (matBlack_CamPos != null)
            {
                matBlack_CamPos.Dispose();
                matBlack_CamPos = null;
            }
            if (matRaster_CamPos != null)
            {
                matRaster_CamPos.Dispose();
                matRaster_CamPos = null;
            }
#endif

        }

        // modified by Hotta 2022/08/08
        // void detectTileCamPos(out CvBlobs blobs)
        unsafe void detectTileCamPos(out CvBlobs blobs, bool saveLog = false)
        {
            Mat matBlack = null;// new Mat();
            Mat matRaster = null;// new Mat();
            Mat matTile = null;// new Mat();
            // modified by Hotta 2022/08/18 for Ceverity
            /*
            Mat matMask = new Mat();
            Mat matDiff = new Mat();
            */
            Mat matMask = null;
            Mat matDiff = null;
            //
            Mat matMono = null;// new Mat();
            Mat matMaskBin = null;// new Mat();
            Mat matMaskBin2 = null;// new Mat();
            Mat matTileMask = new Mat(); // Coverityチェック　例外パスだとリーク
            //Mat matTileMask2 = new Mat();
            Mat matBin = new Mat(); // Coverityチェック　例外パスだとリーク
            Mat matClose = null;// new Mat();

            blobs = null;

            matBlack = new Mat(m_CamPos_BlackImagePath + ".jpg");
            matRaster = new Mat(m_CamPos_RasterImagePath + ".jpg");
            matTile = new Mat(m_CamPos_TileImagePath + ".jpg");


            //Cv2.Resize(matBlack, matBlack, new OpenCvSharp.Size(3008, 2000));
            //Cv2.Resize(matRaster, matRaster, new OpenCvSharp.Size(3008, 2000));
            //Cv2.Resize(matTile, matTile, new OpenCvSharp.Size(3008, 2000));


            // added by Hotta 2022/11/22 for カメラのガンマをキャンセル
            Mat matBlackF = new Mat();
            matBlack.ConvertTo(matBlackF, MatType.CV_32FC3, 1.0 / 255);
            matBlack.Dispose();
            matBlack = new Mat();
            Cv2.Pow(matBlackF, 2.2, matBlackF);
            matBlackF *= 255;
            matBlackF.ConvertTo(matBlack, MatType.CV_8UC3);
            matBlackF.Dispose();
#if TempFile
            matBlack.SaveImage(applicationPath + "\\Temp\\black_CamPos_l.bmp");
#endif

            Mat matRasterF = new Mat();
            matRaster.ConvertTo(matRasterF, MatType.CV_32FC3, 1.0 / 255);
            matRaster.Dispose();
            matRaster = new Mat();
            Cv2.Pow(matRasterF, 2.2, matRasterF);
            matRasterF *= 255;
            matRasterF.ConvertTo(matRaster, MatType.CV_8UC3);
            matRasterF.Dispose();
#if TempFile
            matRaster.SaveImage(applicationPath + "\\Temp\\raster_CamPos_l.bmp");
#endif
            /**/
            Mat matTileF = new Mat();
            matTile.ConvertTo(matTileF, MatType.CV_32FC3, 1.0 / 255);
            matTile.Dispose();
            matTile = new Mat();
            Cv2.Pow(matTileF, 2.2, matTileF);
            matTileF *= 255;
            matTileF.ConvertTo(matTile, MatType.CV_8UC3);
            matTileF.Dispose();
            /**/

            /*
            Mat matTileF = new Mat();
            matTile.ConvertTo(matTileF, MatType.CV_32FC3, 1.0);
            matTile.Dispose();

            for (int i=0;i<30;i++)
            {
                Mat matTileF0 = new Mat();
                matTile = new Mat(m_CamPos_TileImagePath + "_" + i.ToString() + ".jpg");
                matTile.ConvertTo(matTileF0, MatType.CV_32FC3, 1.0);
                matTileF += matTileF0;
                matTile.Dispose();
                matTileF0.Dispose();
            }
            matTileF /= 30;

            matTileF /= 255;
            Cv2.Pow(matTileF, 2.2, matTileF);
            matTileF *= 255;
            matTile = new Mat();
            matTileF.ConvertTo(matTile, MatType.CV_8UC3);
            matTileF.Dispose();
            */



#if TempFile
            matTile.SaveImage(applicationPath + "\\Temp\\tile_CamPos_l.bmp");
#endif
            //


            matMask = matRaster - matBlack;   // Coverityチェック　上書きされた、元のmatMaskがリーク。代入に失敗するとリーク
            matDiff = matTile - matBlack;   // Coverityチェック　上書きされた、元のmatDiffがリーク

#if TempFile
            matDiff.SaveImage(applicationPath + "\\Temp\\diff0.jpg");
#endif

            /*
            for(int i=0;i<1000;i++)
            {
                Mat mat = new Mat();
                Cv2.GaussianBlur(matDiff, mat, new OpenCvSharp.Size(3, 3), 0);
                matDiff.Dispose();
                matDiff = mat.Clone();
                mat.Dispose();
            }
            */
            /*
            Mat mat = new Mat();
            Cv2.MedianBlur(matDiff, mat, 3);
            matDiff.Dispose();
            matDiff = mat.Clone();
            mat.Dispose();
            */
            try
            {
                // added by Hotta 2022/08/08
                if(saveLog == true)
                {
                    SaveMatBinary(matBlack, m_CamMeasPath + "\\CamPos_Black");
                    SaveMatBinary(matRaster, m_CamMeasPath + "\\CamPos_Raster");
                    SaveMatBinary(matTile, m_CamMeasPath + "\\CamPos_Tile");
                }
                //

                matBlack.Dispose();
                matBlack = null;
                matRaster.Dispose();
                matRaster = null;
                matTile.Dispose();
                matTile = null;



                //Cv2.Resize(matMask, matMask, new OpenCvSharp.Size(matMask.Width * 3, matMask.Height * 3));
                //Cv2.Resize(matDiff, matDiff, new OpenCvSharp.Size(matDiff.Width * 3, matDiff.Height * 3));


#if TempFile
                matMask.SaveImage(applicationPath + "\\Temp\\mask.jpg");
                matDiff.SaveImage(applicationPath + "\\Temp\\diff.jpg");
#endif

                matMono = new Mat();
                // modified by Hotta 2022/12/15
                //Cv2.CvtColor(matMask, matMono, ColorConversionCodes.BGR2GRAY);  // Coverityチェック　上書きされた、元のmatMonoがリーク
                Mat[] matSplit = new Mat[3];
                matSplit = matMask.Split();
                matMono = matSplit[1].Clone();
                for (int n = 0; n < matSplit.Length; n++)
                    matSplit[n].Dispose();


                // added by Hotta 2022/12/15
                matMask.Dispose();
                matMask = null;

                matMaskBin = new Mat();

                // modified by Hotta 2022/11/17 for Ver2
                // 閾値の変更

#if NO_CONTROLLER
                Cv2.Threshold(matMono, matMaskBin, 25, 255, ThresholdTypes.Binary); // Ver1.5
#else
                Cv2.Threshold(matMono, matMaskBin, 0, 255, ThresholdTypes.Otsu);
#endif
                matMono.Dispose();
                
#if TempFile
                matMaskBin.SaveImage(applicationPath + "\\Temp\\maskBin_CamPos.bmp");
#endif

                #region Wall光が周囲に反射している場合の対策テスト（不採用）
                // added by Hotta 2022/11/17
                /*
                Mat matEdge = new Mat();
                Cv2.Laplacian(matMono, matEdge, MatType.CV_8UC1, 3, 3);
#if TempFile
                matEdge.SaveImage(applicationPath + "\\Temp\\maskEdge_CamPos.bmp");
#endif
                Mat matEdgeBin = new Mat();
                Cv2.Threshold(matEdge, matEdgeBin, 0, 255, ThresholdTypes.Otsu);
#if TempFile
                matEdgeBin.SaveImage(applicationPath + "\\Temp\\maskEdgeBin_CamPos.bmp");
#endif
                Mat _kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(5, 5));
                Mat matClosing = new Mat();
                Cv2.MorphologyEx(matEdgeBin, matClosing, MorphTypes.Close, _kernel);
#if TempFile
                matClosing.SaveImage(applicationPath + "\\Temp\\maskEdgeClose_CamPos.bmp");
#endif

                OpenCvSharp.Point[][] _contours;
                OpenCvSharp.HierarchyIndex[] _hindex;
                Cv2.FindContours(matClosing, out _contours, out _hindex, RetrievalModes.List, ContourApproximationModes.ApproxNone);

                Mat matMaskEdge2 = new Mat(matClosing.Size(), MatType.CV_8UC1);
                for (int i=0;i< _contours.Length;i++)
                {
                    Cv2.DrawContours(matMaskEdge2, _contours, i, 255, -1);
                }
#if TempFile
                matMaskEdge2.SaveImage(applicationPath + "\\Temp\\maskEdge2_CamPos.bmp");
#endif
                matMaskEdge2.Dispose();
                _kernel.Dispose();
                matClosing.Dispose();
                matEdgeBin.Dispose();
                matEdge.Dispose();
                */
                #endregion


                OpenCvSharp.Point[][] contours;
                OpenCvSharp.HierarchyIndex[] hindex;
                Cv2.FindContours(matMaskBin, out contours, out hindex, RetrievalModes.List, ContourApproximationModes.ApproxNone);
                int width = matMaskBin.Width;
                int height = matMaskBin.Height;
                matMaskBin.Dispose();
                matMaskBin = null;

                // modified by Hotta 2022/09/30
                // 清澄白河3F　天井映り込み
#if ShowPattern_CamPos
                // 一番面積が大きい輪郭を選択する。下の方法だと、最も輪郭が長いブロブを選択している。
                double max_area = 0;
                int max_idx = -1;
                double _area = 0;
                for (int n = 0; n < contours.Length; n++)
                {
                    _area = Cv2.ContourArea(contours[n]);
                    if (_area > max_area)
                    {
                        // 端のブロブは採用しない
                        /*
                        RotatedRect rect = Cv2.MinAreaRect(contours[n]);
                        Point2f[] aryP = rect.Points();
                        float top = aryP[0].Y, bottom = aryP[0].Y, left = aryP[0].X, right = aryP[0].X;
                        for(int i=0;i<aryP.Length;i++)
                        {
                            if (top > aryP[i].Y)
                                top = aryP[i].Y;
                            if (bottom < aryP[i].Y)
                                bottom = aryP[i].Y;
                            if (left > aryP[i].X)
                                left = aryP[i].X;
                            if (right < aryP[i].X)
                                right = aryP[i].X;
                        }
                        if (top > 0.8 * matMaskBin.Height)
                            break;
                        if (bottom < 0.2 * matMaskBin.Height)
                            break;
                        if (left > 0.8 * matMaskBin.Width)
                            break;
                        if (right < 0.2 * matMaskBin.Width)
                            break;
                        */
                        Moments m = Cv2.Moments(contours[n]);
                        double cx = m.M10 / m.M00;
                        double cy = m.M01 / m.M00;

                        // modified by Hotta 2022/12/15
                        /*
                        if (0.2 * matMaskBin.Width < cx && cx < 0.8 * matMaskBin.Width &&
                            0.2 * matMaskBin.Height < cy && cy < 0.8 * matMaskBin.Height)
                        */
                        if (0.05 * width < cx && cx < 0.95 * width &&
                            0.05 * height < cy && cy < 0.95 * height)
                        {
                            max_area = _area;
                            max_idx = n;
                        }
                    }
                    else if(max_area != 0 && _area == max_area)
                    {
                        Moments m = Cv2.Moments(contours[n]);
                        double cx = m.M10 / m.M00;
                        double cy = m.M01 / m.M00;
                        double dist = (cx - width / 2) * (cx - width / 2) + (cy - height / 2) * (cy - height / 2);

                        Moments _m = Cv2.Moments(contours[max_idx]);
                        double _cx = _m.M10 / _m.M00;
                        double _cy = _m.M01 / _m.M00;
                        double _dist = (_cx - width / 2) * (_cx - width / 2) + (_cy - height / 2) * (_cy - height / 2);

                        if(dist < _dist)
                        {
                            max_area = _area;
                            max_idx = n;
                        }
                    }
                }

                if (max_idx == -1)
                {
                    //throw new Exception("Fail to detect raster.");

                    // deleted by Hotta 2023/02/27 for Coverity
                    // 到達しないコードを削除
                    /*
                    if (matMask != null)
                        matMask.Dispose();
                    */
                    if (matDiff != null)
                        matDiff.Dispose();
                    if (matMono != null)
                        matMono.Dispose();
                    // deleted by Hotta 2023/02/27 for Coverity
                    // 到達しないコードを削除
                    /*
                    if (matMaskBin != null)
                        matMaskBin.Dispose();
                    */
                    // deleted by Hotta 2022/10/06
                    /*
                    if (matMaskBin2 != null)
                        matMaskBin2.Dispose();
                    */
                    if (matTileMask != null)
                        matTileMask.Dispose();
                    if (matBin != null)
                        matBin.Dispose();
                    // deleted by Hotta 2022/10/06
                    /*
                    if (matClose != null)
                        matClose.Dispose();
                    */
                    blobs = null;

                    return;
                }

                m_Max_contours = new OpenCvSharp.Point[contours[max_idx].Length];

                for (int n = 0; n < m_Max_contours.Length; n++)
                    m_Max_contours[n] = contours[max_idx][n];

#else
                // 一番面積が大きい輪郭を選択する。
                int max_len = 0;
                int max_idx = -1;
                for (int n = 0; n < contours.Length; n++)
                {
                    if (contours[n].Length > max_len)
                    {
                        max_len = contours[n].Length;
                        max_idx = n;
                    }
                }
                m_Max_contours = new OpenCvSharp.Point[max_len];
                for (int n = 0; n < max_len; n++)
                    m_Max_contours[n] = contours[max_idx][n];
#endif
                //

                matMaskBin2 = new Mat(new OpenCvSharp.Size(width, height), MatType.CV_8UC1);    // Coverityチェック　上書きされた、元の元のmaMaskBin2がリーク
                Cv2.DrawContours(matMaskBin2, contours, max_idx, new Scalar(255), -1);



#if TempFile
                matMaskBin2.SaveImage(applicationPath + "\\Temp\\maskBin2_CamPos.bmp");
#endif


                #region 周囲の反射光キャンセル（不採用）
                //////////////////////////
                // 周囲の反射光キャンセル
                //////////////////////////

                // エッジ検出
                /*
                int edgeStep = 5;
                double edgeRatio = 0.7;

                Mat matMaskwRef = new Mat();
                matMono.CopyTo(matMaskwRef, matMaskBin2);

                Mat matMaskwoRef = matMaskwRef.Clone();

#if TempFile
                matMaskwRef.SaveImage(applicationPath + "\\Temp\\mask0_CamPos.bmp");
#endif
                byte* pMatMaskwR = (byte*)matMaskwRef.Data;
                byte* pMatMaskwoR = (byte*)matMaskwoRef.Data;
                for (int x=0;x< matMaskwRef.Width;x++)
                {
                    int top = -1;
                    int bottom = -1;
                    for (int y = 0; y < matMaskwRef.Height; y++)
                    {
                        if (pMatMaskwR[y * matMaskwoRef.Width + x] != 0)
                        {
                            top = y;
                            break;
                        }
                    }
                    for (int y = matMaskwRef.Height - 1; y >= 0; y--)
                    {
                        if (pMatMaskwR[y * matMaskwoRef.Width + x] != 0)
                        {
                            bottom = y;
                            break;
                        }
                    }
                    if(top != bottom)
                    {
                        int start = (top + bottom) / 2;

                        for(int y = start; y >= top; y--)
                        {
                            bool flag = false;

                            double out_avg = 0;
                            int out_count = 0;

                            double in_avg = 0;
                            int in_count = 0;

                            for (int i=0; i<edgeStep; i++)
                            {
                                if (y - i >= top)
                                {
                                    out_avg += pMatMaskwR[(y - i) * matMaskwoRef.Width + x];
                                    out_count++;
                                }
                                out_avg /= out_count;

                                if ((y + i) <= bottom)
                                {
                                    in_avg += pMatMaskwR[(y + i) * matMaskwoRef.Width + x];
                                    in_count++;
                                }
                                in_avg /= in_count;

                                if(out_avg < edgeRatio * in_avg)
                                    flag = true;
                                else
                                {
                                    flag = false;
                                    continue;
                                }
                            }
                            if(flag == true)
                            {
                                for(int _y = y; _y >= top; _y--)
                                {
                                    pMatMaskwoR[_y * matMaskwoRef.Width + x] = 0;
                                }
                            }
                        }
                    }
                }
#if TempFile

                matMaskwoRef.SaveImage(applicationPath + "\\Temp\\maskBin3.bmp");
#endif
                matMaskwoRef.Dispose();
                matMaskwRef.Dispose();
                */

                /*
                Mat matMaskwRef = new Mat();
                matMono.CopyTo(matMaskwRef, matMaskBin2);

                Mat matMaskwoRef = new Mat();
                Cv2.AdaptiveThreshold(matMaskwRef, matMaskwoRef, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.Binary, 101, 0);

#if TempFile
                matMaskwoRef.SaveImage(applicationPath + "\\Temp\\maskBin3.bmp");
#endif
                */


                #endregion

                // added by Hotta 2022/12/15
                matMono = new Mat();

                // modified by Hotta 2022/12/15
                //Cv2.CvtColor(matDiff, matMono, ColorConversionCodes.BGR2GRAY);
                matSplit = new Mat[3];
                matSplit = matDiff.Split();
                matMono = matSplit[1].Clone();
                for (int n = 0; n < matSplit.Length; n++)
                    matSplit[n].Dispose();

                //matDiff.SaveImage("c:\\temp\\diff.bmp");

                // added by Hotta 2022/12/15
                matDiff.Dispose();
                matDiff = null;

                Cv2.BitwiseAnd(matMono, matMaskBin2, matTileMask);
                matMono.Dispose();
                matMono = null;
                matMaskBin2.Dispose();
                matMaskBin2 = null;

#if NO_CONTROLLER
                Cv2.Threshold(matTileMask, matBin, 25, 255, ThresholdTypes.Binary);
#else
                Cv2.Threshold(matTileMask, matBin, 0, 255, ThresholdTypes.Otsu);
#endif
                matTileMask.Dispose();
                matTileMask = null;

#if TempFile
                matBin.SaveImage(applicationPath + "\\Temp\\tileBin_CamPos.bmp");
#endif

                // ブロブ内塗りつぶし
                // modified by Hotta 2022/11/24
                // カメラガンマをキャンセルしたので、大きめに処理する
                //Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
                //Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(11, 11));   // やりすぎると、あおり撮影の時、ブロブ同士がつながってしまう
                Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(5, 5));
                matClose = new Mat();
                Cv2.MorphologyEx(matBin, matClose, MorphTypes.Close, kernel);

                // added by Hotta 2022/12/15
                matBin.Dispose();
                matBin = null;

#if TempFile
                matClose.SaveImage(applicationPath + "\\Temp\\tileBinClose_CamPos.bmp");
#endif

                // タイルを検出
                /*CvBlobs*/
                blobs = new CvBlobs(matClose);

                // added by Hotta 2022/12/15
                matClose.Dispose();
                matClose = null;


                // 形状や面積が異常なものは、リジェクトor検出失敗とする
                double area = 0;
                foreach (KeyValuePair<int, CvBlob> item in blobs)
                {
                    int labelValue = item.Key;
                    CvBlob blob = item.Value;
                    area += blob.Area;
                }
                area /= blobs.Count;
                blobs.FilterByArea((int)(area * 0.1), int.MaxValue);

            }
            catch //(Exception ex)
            {
                if(matBlack != null)
                    matBlack.Dispose();
                if (matRaster != null)
                    matRaster.Dispose();
                if (matTile != null)
                    matTile.Dispose();

                // modified by Hotta 2022/08/18 for Ceverity
                /*
                matMask.Dispose();
                matDiff.Dispose();
                */
                if(matMask != null)
                    matMask.Dispose();
                if(matDiff != null)
                    matDiff.Dispose();
                //

                // modified by Hotta 2022/08/18 for Ceverity
                /*
                matMono.Dispose();
                */
                if (matMono != null)
                    matMono.Dispose();
                //

                // modified by Hotta 2022/09/06
                /*
                matMaskBin.Dispose();
                */
                if(matMaskBin != null)
                    matMaskBin.Dispose();

                // modified by Hotta 2022/08/18 for Ceverity
                /*
                matMaskBin2.Dispose();
                */
                if (matMaskBin2 != null)
                    matMaskBin2.Dispose();
                //
                if(matTileMask != null)
                    matTileMask.Dispose();
                if(matBin != null)
                    matBin.Dispose();

                // modified by Hotta 2022/09/06
                /*
                matClose.Dispose();
                */
                if(matClose != null)
                    matClose.Dispose();
                //
            }
        }

        // added by Hotta 2022/12/19
        unsafe void detectSatAreaCamPos(out CvBlobs blobs)
        {
            blobs = null;

            Mat mat = new Mat(m_CamPos_BlackImagePath + ".jpg");

            Mat matF = new Mat();
            mat.ConvertTo(matF, MatType.CV_32FC3, 1.0 / 255);
            mat.Dispose();
            mat = new Mat();
            Cv2.Pow(matF, 2.2, matF);
            matF *= 255;
            matF.ConvertTo(mat, MatType.CV_8UC3);
            matF.Dispose();

            Mat[] matSplit = new Mat[3];
            matSplit = mat.Split();
            mat.Dispose();

            Mat matBin = new Mat();
            Cv2.Threshold(matSplit[1], matBin, 230, 255, ThresholdTypes.Binary);
            matSplit[0].Dispose();
            matSplit[1].Dispose();
            matSplit[2].Dispose();

            blobs = new CvBlobs(matBin);

            matBin.Dispose();
        }


        // modified by Hotta 2022/11/14 for 外部入力されたCabinet配置
#if FlexibleCabinetPosition
        void estimateCamPos(CvBlob[,] aryBlob)
        {
            Point2f[] imagePoints = new Point2f[m_CabinetXNum_CamPos * m_CabinetYNum_CamPos * 4]; // 1cabinetあたり4ケのタイル
            for (int y = 0; y < m_CabinetYNum_CamPos * 2; y++)
            {
                for (int x = 0; x < m_CabinetXNum_CamPos * 2; x++)
                {
                    imagePoints[y * (m_CabinetXNum_CamPos * 2) + x] = new Point2f((float)aryBlob[x, y].Centroid.X, (float)aryBlob[x, y].Centroid.Y);
                }
            }

            /*X座標を直線近似*/
            /*
            for (int x = 0; x < m_CabinetXNum_CamPos * 2; x++)
            {
                List<Point2f> points = new List<Point2f>();
                for (int y = 0; y < m_CabinetYNum_CamPos * 2; y++)
                {
                    // ここで、X/Yを入れ替え
                    points.Add(new Point2f((float)aryBlob[x, y].Centroid.Y, (float)aryBlob[x, y].Centroid.X));
                }
                Line2D line = Cv2.FitLine(points, DistanceTypes.L1, 0, 0.01, 0.01);
                double a = line.Vy / line.Vx;
                double b = -line.Vy / line.Vx * line.X1 + line.Y1;
                // ここで、Y/Xを入れ替え
                for (int y = 0; y < m_CabinetYNum_CamPos * 2; y++)
                {
                    imagePoints[y * (m_CabinetXNum_CamPos * 2) + x] = new Point2f((float)(a * aryBlob[x, y].Centroid.Y + b), (float)aryBlob[x, y].Centroid.Y);
                }
            }
            */

            /*

            // X間の平均値
            float aveX = 0;
            for (int y = 0; y < m_CabinetYNum_CamPos * 2; y++)
            {
                for (int x = 0; x < m_CabinetXNum_CamPos * 2 - 1; x++)
                {
                    aveX += (float)aryBlob[x + 1, y].Centroid.X - (float)aryBlob[x, y].Centroid.X;
                }
            }
            aveX /= (m_CabinetYNum_CamPos * 2) * (m_CabinetXNum_CamPos * 2 - 1);

            // Y間の平均値
            float aveY = 0;
            for (int x = 0; x < m_CabinetXNum_CamPos * 2; x++)
            {
                for (int y = 0; y < m_CabinetYNum_CamPos * 2 - 1; y++)
                {
                    aveY += (float)aryBlob[x, y + 1].Centroid.Y - (float)aryBlob[x, y].Centroid.Y;
                }
            }
            aveY /= (m_CabinetXNum_CamPos * 2) * (m_CabinetYNum_CamPos * 2 - 1);

            // 座標の割り振り
            for (int y = 0; y < m_CabinetYNum_CamPos * 2; y++)
            {
                for (int x = 0; x < m_CabinetXNum_CamPos * 2; x++)
                {
                    imagePoints[y * (m_CabinetXNum_CamPos * 2) + x].X = (float)(aryBlob[0, 0].Centroid.X + aveX * x);
                    imagePoints[y * (m_CabinetXNum_CamPos * 2) + x].Y = (float)(aryBlob[0, 0].Centroid.Y + aveY * y);
                }
            }
            */

#if TempFile
            /**/
            Mat mat = new Mat(applicationPath + "\\Temp\\tileBinClose_CamPos.bmp");
            for (int x = 0; x < m_CabinetXNum_CamPos * 2; x++)
            {
                for (int y = 0; y < m_CabinetYNum_CamPos * 2 - 1; y++)
                {
                    //mat.Line(
                    //    (int)(imagePoints[y * (m_CabinetXNum_CamPos * 2) + x].X + 0.5), (int)(imagePoints[y * (m_CabinetXNum_CamPos * 2) + x].Y + 0.5),
                    //    (int)(imagePoints[(y + 1) * (m_CabinetXNum_CamPos * 2) + x].X + 0.5), (int)(imagePoints[(y + 1) * (m_CabinetXNum_CamPos * 2) + x].Y + 0.5),
                    //    new Scalar(0, 0, 255));
                    mat.Rectangle(
                        new OpenCvSharp.Point((int)(imagePoints[y * (m_CabinetXNum_CamPos * 2) + x].X + 0.5), (int)(imagePoints[y * (m_CabinetXNum_CamPos * 2) + x].Y + 0.5)),
                        new OpenCvSharp.Point((int)(imagePoints[y * (m_CabinetXNum_CamPos * 2) + x].X + 0.5), (int)(imagePoints[y * (m_CabinetXNum_CamPos * 2) + x].Y + 0.5)),
                        new Scalar(0, 0, 255));
                }
            }
            for (int y = 0; y < m_CabinetYNum_CamPos * 2; y++)
            {
                for (int x = 0; x < m_CabinetXNum_CamPos * 2 - 1; x++)
                {
                    //mat.Line(
                    //    (int)(imagePoints[y * (m_CabinetXNum_CamPos * 2) + x].X + 0.5), (int)(imagePoints[y * (m_CabinetXNum_CamPos * 2) + x].Y + 0.5),
                    //    (int)(imagePoints[y * (m_CabinetXNum_CamPos * 2) + x + 1].X + 0.5), (int)(imagePoints[y * (m_CabinetXNum_CamPos * 2) + x + 1].Y + 0.5),
                    //    new Scalar(0, 0, 255));
                    mat.Rectangle(
                        new OpenCvSharp.Point((int)(imagePoints[y * (m_CabinetXNum_CamPos * 2) + x].X + 0.5), (int)(imagePoints[y * (m_CabinetXNum_CamPos * 2) + x].Y + 0.5)),
                        new OpenCvSharp.Point((int)(imagePoints[y * (m_CabinetXNum_CamPos * 2) + x].X + 0.5), (int)(imagePoints[y * (m_CabinetXNum_CamPos * 2) + x].Y + 0.5)),
                        new Scalar(0, 0, 255));

                }
            }
            mat.SaveImage(applicationPath + "\\Temp\\tileBinClose_CamPos.bmp");
            mat.Dispose();
            /**/
#endif

            // カメラ位置の推定
            TransformImage.EstimateCameraPos estimate = new TransformImage.EstimateCameraPos();

            estimate.CameraParameter.Set(m_CameraParam.f, m_CameraParam.SensorSizeH, m_CameraParam.SensorSizeV, m_CameraParam.SensorResH, m_CameraParam.SensorResV);

            estimate.ImagePoints = imagePoints;

            // 3D座標
            Point3f[] objectPoints = new Point3f[m_CabinetXNum_CamPos * m_CabinetYNum_CamPos * 4];

            float wd = m_WorkDistance;
            //if (allocInfo.LEDModel == ZRD_C15A || allocInfo.LEDModel == ZRD_B15A || allocInfo.LEDModel == ZRD_CH15D || allocInfo.LEDModel == ZRD_BH15D)
            //{ wd *= (float)(RealPitch_P15 / RealPitch_P12); }

            float k = 1.0f;

#if NO_CONTROLLER
            // HP224
            if (allocInfo.LEDModel == ZRD_C15A 
                || allocInfo.LEDModel == ZRD_B15A 
                || allocInfo.LEDModel == ZRD_CH15D 
                || allocInfo.LEDModel == ZRD_BH15D
                || allocInfo.LEDModel == ZRD_CH15D_S3
                || allocInfo.LEDModel == ZRD_BH15D_S3)
            {
                k = 476.0f / (m_LedPitch * m_CabinetDx * CabinetLength_P15);
            }
            else
            {
                k = 476.0f / (m_LedPitch * m_CabinetDx * CabinetLength_P12);
            }
#endif

#if NO_CAP
            k = 1;
#endif

            SetCabinetPos(m_lstCamPosUnits, wd);


            TransformImage.TransformImage transform = new TransformImage.TransformImage();

            int startX = int.MaxValue;
            int endX = int.MinValue;
            int startY = int.MaxValue;
            int endY = int.MinValue;

            foreach (UnitInfo unit in m_lstCamPosUnits)
            {
                if (startX > unit.X)
                    startX = unit.X;
                if (endX < unit.X)
                    endX = unit.X;
                if (startY > unit.Y)
                    startY = unit.Y;
                if (endY < unit.Y)
                    endY = unit.Y;
            }
            startX--;
            endX--;
            startY--;
            endY--;

            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    float left = (float)allocInfo.lstUnits[x][y].CabinetPos.TopLeft.x;
                    float right = (float)allocInfo.lstUnits[x][y].CabinetPos.BottomRight.x;
                    float top = -(float)allocInfo.lstUnits[x][y].CabinetPos.TopLeft.y;
                    float bottom = -(float)allocInfo.lstUnits[x][y].CabinetPos.BottomLeft.y;
                    float front = (float)allocInfo.lstUnits[x][y].CabinetPos.TopLeft.z;
                    float back = (float)allocInfo.lstUnits[x][y].CabinetPos.BottomRight.z;

                    // 左上
                    float xPos = left + 1.0f / 4 * (right - left);
                    float yPos = top + 1.0f / 4 * (bottom - top);
                    float zPos = front + 1.0f / 4 * (back - front);
                    objectPoints[((y - startY) * 2 + 0) * (m_CabinetXNum_CamPos * 2) + (x - startX) * 2 + 0] = new Point3f(xPos * k, yPos * k, zPos * k);

                    // 右上
                    xPos = left + 3.0f / 4 * (right - left);
                    yPos = top + 1.0f / 4 * (bottom - top);
                    zPos = front + 3.0f / 4 * (back - front);
                    objectPoints[((y - startY) * 2 + 0) * (m_CabinetXNum_CamPos * 2) + (x - startX) * 2 + 1] = new Point3f(xPos * k, yPos * k, zPos * k);

                    // 左下
                    xPos = left + 1.0f / 4 * (right - left);
                    yPos = top + 3.0f / 4 * (bottom - top);
                    zPos = front + 1.0f / 4 * (back - front);
                    objectPoints[((y - startY) * 2 + 1) * (m_CabinetXNum_CamPos * 2) + (x - startX) * 2 + 0] = new Point3f(xPos * k, yPos * k, zPos * k);

                    // 右下
                    xPos = left + 3.0f / 4 * (right - left);
                    yPos = top + 3.0f / 4 * (bottom - top);
                    zPos = front + 3.0f / 4 * (back - front);
                    objectPoints[((y - startY) * 2 + 1) * (m_CabinetXNum_CamPos * 2) + (x - startX) * 2 + 1] = new Point3f(xPos * k, yPos * k, zPos * k);
                }
            }

            estimate.ObjectPoints = objectPoints;
            estimate.Estimate();

            m_CamPos_Pan = estimate.Rot[1];
            m_CamPos_Tilt = estimate.Rot[0] - m_Rotate[0];
            m_CamPos_Roll = estimate.Rot[2];
            m_CamPos_Tx = estimate.Trans[0];
            m_CamPos_Ty = estimate.Trans[1] - m_Transration[1];
            m_CamPos_Tz = estimate.Trans[2];

            /*
            // added by Hotta 2022/11/28 for カメラ位置推定の対象範囲が小さい場合の対策
#if CamPos_TargetArea_Small
            int image_magnification;
            int sensor_magnification = 1;
            for(image_magnification=50;image_magnification>=2;image_magnification--)
            {
                // 画像を拡大した時、センサーに収まる倍率（magnification）を探す 
                if ((aryBlob[0,                            0].Centroid.X                            - m_CameraParam.SensorResH / 2) * image_magnification + m_CameraParam.SensorResH / 2 * sensor_magnification > 0 &&
                    (aryBlob[m_CabinetXNum_CamPos * 2 - 1, 0].Centroid.X                            - m_CameraParam.SensorResH / 2) * image_magnification + m_CameraParam.SensorResH / 2 * sensor_magnification < m_CameraParam.SensorResH * sensor_magnification &&
                    (aryBlob[m_CabinetXNum_CamPos * 2 - 1, m_CabinetYNum_CamPos * 2 - 1].Centroid.X - m_CameraParam.SensorResH / 2) * image_magnification + m_CameraParam.SensorResH / 2 * sensor_magnification < m_CameraParam.SensorResH * sensor_magnification &&
                    (aryBlob[0,                            m_CabinetYNum_CamPos * 2 - 1].Centroid.X - m_CameraParam.SensorResH / 2) * image_magnification + m_CameraParam.SensorResH / 2 * sensor_magnification > 0 &&
                    (aryBlob[0, 　　　　　　　　　　　　   0].Centroid.Y                            - m_CameraParam.SensorResV / 2) * image_magnification + m_CameraParam.SensorResV / 2 * sensor_magnification > 0 &&
                    (aryBlob[m_CabinetXNum_CamPos * 2 - 1, 0].Centroid.Y                            - m_CameraParam.SensorResV / 2) * image_magnification + m_CameraParam.SensorResV / 2 * sensor_magnification > 0 &&
                    (aryBlob[m_CabinetXNum_CamPos * 2 - 1, m_CabinetYNum_CamPos * 2 - 1].Centroid.Y - m_CameraParam.SensorResV / 2) * image_magnification + m_CameraParam.SensorResV / 2 * sensor_magnification < m_CameraParam.SensorResV * sensor_magnification &&
                    (aryBlob[0,                            m_CabinetYNum_CamPos * 2 - 1].Centroid.Y - m_CameraParam.SensorResV / 2) * image_magnification + m_CameraParam.SensorResV / 2 * sensor_magnification < m_CameraParam.SensorResV * sensor_magnification
                    )
                    break;
            }
            // センサーに収まる倍率があるなら
            if(image_magnification >= 2)
            {
                // 画像をmagnification倍する
                imagePoints = new Point2f[m_CabinetXNum_CamPos * m_CabinetYNum_CamPos * 4]; // 1cabinetあたり4ケのタイル
                for (int y = 0; y < m_CabinetYNum_CamPos * 2; y++)
                {
                    for (int x = 0; x < m_CabinetXNum_CamPos * 2; x++)
                    {
                        imagePoints[y * (m_CabinetXNum_CamPos * 2) + x]
                           = new Point2f(
                               (float)(aryBlob[x, y].Centroid.X - m_CameraParam.SensorResH / 2) * image_magnification + m_CameraParam.SensorResH / 2 * sensor_magnification,
                               (float)(aryBlob[x, y].Centroid.Y - m_CameraParam.SensorResV / 2) * image_magnification + m_CameraParam.SensorResV / 2 * sensor_magnification
                               );
                    }
                }
                estimate.ImagePoints = imagePoints;

                // 拡大した分、焦点距離を長くする
                estimate.CameraParameter.Set(m_CameraParam.f * image_magnification, m_CameraParam.SensorSizeH * sensor_magnification, m_CameraParam.SensorSizeV * sensor_magnification, m_CameraParam.SensorResH * sensor_magnification, m_CameraParam.SensorResV * sensor_magnification);

                estimate.Estimate();

                m_CamPos_Pan = estimate.Rot[1];
                m_CamPos_Tilt = estimate.Rot[0] - m_Rotate[0];
                m_CamPos_Roll = estimate.Rot[2];
                m_CamPos_Tx = estimate.Trans[0];
                m_CamPos_Ty = (estimate.Trans[1] - m_Transration[1]);
                m_CamPos_Tz = estimate.Trans[2];
            }
#endif
            */
            /**/

#if TempFile
            using(StreamWriter sw = new StreamWriter(applicationPath + "\\Temp\\tilePos.csv"))
            {
                for (int y = 0; y < m_CabinetYNum_CamPos * 2; y++)
                {
                    string str = "";
                    for (int x = 0; x < m_CabinetXNum_CamPos * 2; x++)
                    {
                        str += aryBlob[x, y].Centroid.X.ToString() + "," + aryBlob[x, y].Centroid.Y.ToString() + ",";
                    }
                    sw.WriteLine(str);
                }
                sw.WriteLine("");

                for (int i = 0; i < imagePoints.Length;i++)
                {
                    sw.WriteLine(imagePoints[i].X.ToString() + "," + imagePoints[i].Y.ToString());
                }
                sw.WriteLine("");

                for (int i = 0; i < imagePoints.Length; i++)
                {
                    sw.WriteLine(objectPoints[i].X.ToString() + "," + objectPoints[i].Y.ToString() + "," + objectPoints[i].Z.ToString());
                }
                sw.WriteLine("");
            }

            using(StreamWriter sw = new StreamWriter(applicationPath + "\\Temp\\est.csv", true))
            {
                sw.WriteLine(m_CamPos_Pan.ToString() + "," + m_CamPos_Tilt.ToString() + "," + m_CamPos_Roll.ToString() + "," + m_CamPos_Tx.ToString() + "," + m_CamPos_Ty.ToString() + "," + m_CamPos_Tz.ToString());
            }
            /**/


            Point2f[] _imagePoints;
            Point3f[] _objectPoints;

            using (StreamWriter sw = new StreamWriter(applicationPath + "\\Temp\\estimate.csv"))
            {
                sw.WriteLine("Pan,Tilt,Roll,Tx,Ty,Tz");

                _imagePoints = new Point2f[imagePoints.Length];
                _objectPoints = new Point3f[objectPoints.Length];

                for (int i = 0; i < imagePoints.Length; i++)
                {
                    _imagePoints[i] = imagePoints[i];
                    _objectPoints[i] = objectPoints[i];
                }

                estimate.ImagePoints = _imagePoints;
                estimate.ObjectPoints = _objectPoints;

                estimate.Estimate();

                sw.WriteLine(estimate.Rot[1].ToString() + "," + (estimate.Rot[0] - m_Rotate[0]).ToString() + "," + estimate.Rot[2].ToString() + ","
                    + estimate.Trans[0].ToString() + "," + (estimate.Trans[1] - m_Transration[1]).ToString() + "," + estimate.Trans[2].ToString());


                _imagePoints = new Point2f[imagePoints.Length - 1];
                _objectPoints = new Point3f[objectPoints.Length - 1];

                for (int n = 0; n < imagePoints.Length; n++)
                {
                    int ptr = 0;
                    for (int i = 0; i < imagePoints.Length; i++)
                    {
                        if (n == i)
                            continue;

                        if(i == imagePoints.Length - 1)
                        {
                            _imagePoints[ptr].X = imagePoints[i].X;
                            _imagePoints[ptr].Y = imagePoints[i].Y;
                        }
                        else
                        {
                            _imagePoints[ptr] = imagePoints[i];
                        }
                        _objectPoints[ptr] = objectPoints[i];
                        ptr++;
                    }

                    estimate.ImagePoints = _imagePoints;
                    estimate.ObjectPoints = _objectPoints;

                    estimate.Estimate();

                    sw.WriteLine(estimate.Rot[1].ToString() + "," + (estimate.Rot[0] - m_Rotate[0]).ToString() + "," + estimate.Rot[2].ToString() + ","
                        + estimate.Trans[0].ToString() + "," + (estimate.Trans[1] - m_Transration[1]).ToString() + "," + estimate.Trans[2].ToString());
                }
            }
#endif
        }


#else
            void estimateCamPos(CvBlob[,] aryBlob)
        {
            // modified by Hotta 2022/07/29
            /*
            Point2f[] imagePoints = new Point2f[m_CabinetXNum * m_CabinetYNum * 4]; // 1cabinetあたり4ケのタイル
            for (int y = 0; y < m_CabinetYNum * 2; y++)
            {
                for (int x = 0; x < m_CabinetXNum * 2; x++)
                {
                    imagePoints[y * (m_CabinetXNum * 2) + x] = new Point2f((float)aryBlob[x, y].Centroid.X, (float)aryBlob[x, y].Centroid.Y);
                }
            }
            */
            Point2f[] imagePoints = new Point2f[m_CabinetXNum_CamPos * m_CabinetYNum_CamPos * 4]; // 1cabinetあたり4ケのタイル
            for (int y = 0; y < m_CabinetYNum_CamPos * 2; y++)
            {
                for (int x = 0; x < m_CabinetXNum_CamPos * 2; x++)
                {
                    imagePoints[y * (m_CabinetXNum_CamPos * 2) + x] = new Point2f((float)aryBlob[x, y].Centroid.X, (float)aryBlob[x, y].Centroid.Y);
                }
            }


            //Point2f[] imagePoints = new Point2f[m_CabinetXNum * m_CabinetYNum * 16]; // 1cabinetあたり16ケのタイル
            //for (int y = 0; y < m_CabinetYNum * 4; y++)
            //{
            //    for (int x = 0; x < m_CabinetXNum * 4; x++)
            //    {
            //        imagePoints[y * (m_CabinetXNum * 4) + x] = new Point2f((float)aryBlob[x, y].Centroid.X, (float)aryBlob[x, y].Centroid.Y);
            //    }
            //}


            // カメラ位置の推定
            TransformImage.EstimateCameraPos estimate = new TransformImage.EstimateCameraPos();

            float f = m_CameraParam.f;
            int imageWidth = m_CameraParam.SensorResH;
            int imageHeight = m_CameraParam.SensorResV;
            float sensorWidth = m_CameraParam.SensorSizeH;
            float sensorHeight = m_CameraParam.SensorSizeV;
            estimate.CameraParameter.Set(f, sensorWidth, sensorHeight, imageWidth, imageHeight);

            estimate.ImagePoints = imagePoints;

            double cabinetWidth;
            double cabinetHeight;

            if (allocInfo.LEDModel == ZRD_C12A 
                || allocInfo.LEDModel == ZRD_B12A 
                || allocInfo.LEDModel == ZRD_CH12D
                || allocInfo.LEDModel == ZRD_BH12D
                || allocInfo.LEDModel == ZRD_CH12D_S3
                || allocInfo.LEDModel == ZRD_BH12D_S3)
            {
                cabinetWidth = CabinetDxP12 * RealPitch_P12;
                cabinetHeight = CabinetDyP12 * RealPitch_P12;
                //cabinetWidth = (m_PanelCorner[1].X - m_PanelCorner[0].X) / m_CabinetXNum;
                //cabinetHeight = (m_PanelCorner[3].Y - m_PanelCorner[0].Y) / m_CabinetXNum;
            }
            else
            {
                cabinetWidth = CabinetDxP15 * RealPitch_P15;
                cabinetHeight = CabinetDyP15 * RealPitch_P15;
                //cabinetWidth = (m_PanelCorner[1].X - m_PanelCorner[0].X) / m_CabinetXNum;
                //cabinetHeight = (m_PanelCorner[3].Y - m_PanelCorner[0].Y) / m_CabinetXNum;
            }

            /**/
            // modified by Hotta 2022/07/29
            // Point3f[] objectPoints = new Point3f[m_CabinetXNum * m_CabinetYNum * 4];
            Point3f[] objectPoints = new Point3f[m_CabinetXNum_CamPos * m_CabinetYNum_CamPos * 4];

            //double offsetX = -(cabinetWidth * m_CabinetXNum) / 2 + cabinetWidth / 4;
            //double offsetY = -(cabinetHeight * m_CabinetYNum) / 2 + cabinetHeight / 4;
            //double pitchX = cabinetWidth / 2;
            //double pitchY = cabinetHeight / 2;

            float wd = 4000.0f;

            if (allocInfo.LEDModel == ZRD_C15A 
                || allocInfo.LEDModel == ZRD_B15A 
                || allocInfo.LEDModel == ZRD_CH15D 
                || allocInfo.LEDModel == ZRD_BH15D
                || allocInfo.LEDModel == ZRD_CH15D_S3 
                || allocInfo.LEDModel == ZRD_BH15D_S3)
            { 
                wd *= (float)(RealPitch_P15 / RealPitch_P12);
            }

            float k = 1.0f;

#if NO_CONTROLLER
            // HP224
            if (allocInfo.LEDModel == ZRD_C15A 
                || allocInfo.LEDModel == ZRD_B15A 
                || allocInfo.LEDModel == ZRD_CH15D 
                || allocInfo.LEDModel == ZRD_BH15D
                || allocInfo.LEDModel == ZRD_CH15D_S3
                || allocInfo.LEDModel == ZRD_BH15D_S3)
            {
                k = 476.0f / (m_LedPitch * m_CabinetDx * CabinetLength_P15);
            }
            else
            {
                k = 476.0f / (m_LedPitch * m_CabinetDx * CabinetLength_P12);
            }

            cabinetWidth *= k;
            cabinetHeight *= k;
            wd *= k;
#endif

            TransformImage.TransformImage transform = new TransformImage.TransformImage();
            float angle = 0.0f;//5.0f;

            // modified by Hotta 2022/07/29
            /*
            for (int y = 0; y < m_CabinetYNum; y++)
            {
                for (int x = 0; x < m_CabinetXNum; x++)
                {
            */
            for (int y = 0; y < m_CabinetYNum_CamPos; y++)
            {
                for (int x = 0; x < m_CabinetXNum_CamPos; x++)
                {
                    transform.Set3DPoints(0, (float)(-cabinetWidth / 2 + cabinetWidth / 4 + cabinetWidth / 2 * 0), (float)(-cabinetHeight / 2 + cabinetHeight / 4 + cabinetHeight / 2 * 0), 0);
                    transform.Set3DPoints(1, (float)(-cabinetWidth / 2 + cabinetWidth / 4 + cabinetWidth / 2 * 1), (float)(-cabinetHeight / 2 + cabinetHeight / 4 + cabinetHeight / 2 * 0), 0);
                    transform.Set3DPoints(2, (float)(-cabinetWidth / 2 + cabinetWidth / 4 + cabinetWidth / 2 * 1), (float)(-cabinetHeight / 2 + cabinetHeight / 4 + cabinetHeight / 2 * 1), 0);
                    transform.Set3DPoints(3, (float)(-cabinetWidth / 2 + cabinetWidth / 4 + cabinetWidth / 2 * 0), (float)(-cabinetHeight / 2 + cabinetHeight / 4 + cabinetHeight / 2 * 1), 0);

                    // フラット
                    // modified by Hotta 2022/07/29
                    // transform.SetTranslation((float)(-cabinetWidth * m_CabinetXNum / 2 + cabinetWidth / 2 + cabinetWidth * x), (float)(-cabinetHeight * m_CabinetYNum / 2 + cabinetHeight / 2 + cabinetHeight * y), 6926.1f * k);
                    transform.SetTranslation((float)(-cabinetWidth * m_CabinetXNum_CamPos / 2 + cabinetWidth / 2 + cabinetWidth * x), (float)(-cabinetHeight * m_CabinetYNum_CamPos / 2 + cabinetHeight / 2 + cabinetHeight * y), 6926.1f * k);
                    // ラウンド
                    //transform.SetTranslation(0, (float)(-cabinetHeight * m_CabinetYNum_CamPos / 2 + cabinetHeight / 2  + cabinetHeight * y), 6926.1f * k);

                    transform.SetRx(0);
                    // modified by Hotta 2022/07/29
                    // transform.SetRy(-angle / 2 - angle * ((float)m_CabinetXNum / 2 - 1) + angle * x); //  -17.5f + 5.0f * x;
                    transform.SetRy(-angle / 2 - angle * ((float)m_CabinetXNum_CamPos / 2 - 1) + angle * x); //  -17.5f + 5.0f * x;
                    transform.SetRz(0);

                    transform.SetShiftToCameraCoordinate(0, 0, wd - 6926.1f * k);

                    transform.CameraParameter.Set(m_CameraParam.f, m_CameraParam.SensorSizeH, m_CameraParam.SensorSizeV, m_CameraParam.SensorResH, m_CameraParam.SensorResV);

                    transform.Calc();

                    // modified by Hotta 2022/07/29
                    /*
                    float X, Y, Z;
                    transform.GetMoved3DPoints(0, out X, out Y, out Z);
                    objectPoints[(y * 2 + 0) * (m_CabinetXNum * 2) + x * 2 + 0] = new Point3f(X, Y, Z);
                    transform.GetMoved3DPoints(1, out X, out Y, out Z);
                    objectPoints[(y * 2 + 0) * (m_CabinetXNum * 2) + x * 2 + 1] = new Point3f(X, Y, Z);
                    transform.GetMoved3DPoints(3, out X, out Y, out Z);
                    objectPoints[(y * 2 + 1) * (m_CabinetXNum * 2) + x * 2 + 0] = new Point3f(X, Y, Z);
                    transform.GetMoved3DPoints(2, out X, out Y, out Z);
                    objectPoints[(y * 2 + 1) * (m_CabinetXNum * 2) + x * 2 + 1] = new Point3f(X, Y, Z);
                    */
                    float X, Y, Z;
                    transform.GetMoved3DPoints(0, out X, out Y, out Z);
                    objectPoints[(y * 2 + 0) * (m_CabinetXNum_CamPos * 2) + x * 2 + 0] = new Point3f(X, Y, Z);
                    transform.GetMoved3DPoints(1, out X, out Y, out Z);
                    objectPoints[(y * 2 + 0) * (m_CabinetXNum_CamPos * 2) + x * 2 + 1] = new Point3f(X, Y, Z);
                    transform.GetMoved3DPoints(3, out X, out Y, out Z);
                    objectPoints[(y * 2 + 1) * (m_CabinetXNum_CamPos * 2) + x * 2 + 0] = new Point3f(X, Y, Z);
                    transform.GetMoved3DPoints(2, out X, out Y, out Z);
                    objectPoints[(y * 2 + 1) * (m_CabinetXNum_CamPos * 2) + x * 2 + 1] = new Point3f(X, Y, Z);

                }
            }


            estimate.ObjectPoints = objectPoints;

            estimate.Estimate();


            //m_CamPos_Pan = estimate.Rot[1] - m_Rotate[1];
            //m_CamPos_Tilt = estimate.Rot[0] - m_Rotate[0];
            //m_CamPos_Roll = estimate.Rot[2] - m_Rotate[2];
            //m_CamPos_Tx = estimate.Trans[0] - m_Transration[0];
            //m_CamPos_Ty = estimate.Trans[1] - m_Transration[1];
            //m_CamPos_Tz = estimate.Trans[2] - m_Transration[2];
            m_CamPos_Pan = estimate.Rot[1];
            m_CamPos_Tilt = estimate.Rot[0];
            m_CamPos_Roll = estimate.Rot[2];
            m_CamPos_Tx = estimate.Trans[0];
            m_CamPos_Ty = estimate.Trans[1];
            m_CamPos_Tz = estimate.Trans[2];

        }

        bool DetectCorner(Mat mat, out PointF[] corner)
        {
            corner = new PointF[4];

            List<CvBlob> lstBlob = new List<CvBlob>();

            CvBlobs blobs = new CvBlobs(mat);


            if (blobs.Count < 4)
            {
                return false;
            }

            for (int n = 0; n < 4; n++)
            {
                CvBlob blob = blobs.LargestBlob();
                lstBlob.Add(blob);
                bool st = blobs.Remove(blob.Label);
            }

            // Xでソート
            lstBlob.Sort((a, b) => (int)(a.Centroid.X - b.Centroid.X));

            if (lstBlob[0].Centroid.Y < lstBlob[1].Centroid.Y)
            {
                // 左上
                corner[0].X = lstBlob[0].MinX;
                corner[0].Y = lstBlob[0].MinY;

                // 左下
                corner[3].X = lstBlob[1].MinX;
                corner[3].Y = lstBlob[1].MaxY;
            }
            else
            {
                // 左上
                corner[0].X = lstBlob[1].MinX;
                corner[0].Y = lstBlob[1].MinY;

                // 左下
                corner[3].X = lstBlob[0].MinX;
                corner[3].Y = lstBlob[0].MaxY;
            }

            if (lstBlob[2].Centroid.Y < lstBlob[3].Centroid.Y)
            {
                // 右上
                corner[1].X = lstBlob[2].MaxX;
                corner[1].Y = lstBlob[2].MinY;

                // 右下
                corner[2].X = lstBlob[3].MaxX;
                corner[2].Y = lstBlob[3].MaxY;
            }
            else
            {
                // 右上
                corner[1].X = lstBlob[3].MaxX;
                corner[1].Y = lstBlob[3].MinY;

                // 右下
                corner[2].X = lstBlob[2].MaxX;
                corner[2].Y = lstBlob[2].MaxY;
            }

            return true;
        }
#endif


        // added by Hotta 2022/12/13
#if Spec_by_Zdistance
        void calc_Spec_by_Zdistance()
        {
            m_Spec_by_Zdistance = new double[allocInfo.MaxX][];
            for(int x=0;x< allocInfo.MaxX;x++)
            {
                m_Spec_by_Zdistance[x] = new double[allocInfo.MaxY];
            }

            for (int x = 0; x < allocInfo.MaxX; x++)
            {
                for (int y = 0; y < allocInfo.MaxY; y++)
                {
                    // added by Hotta 2022/12/22
                    if (allocInfo.lstUnits[x][y] == null)
                    {
                        m_Spec_by_Zdistance[x][y] = 0;
                        continue;
                    }
                    //

                    double _x = (allocInfo.lstUnits[x][y].CabinetPos.TopLeft.x + allocInfo.lstUnits[x][y].CabinetPos.TopRight.x + allocInfo.lstUnits[x][y].CabinetPos.BottomRight.x + allocInfo.lstUnits[x][y].CabinetPos.BottomLeft.x) / 4;
                    double _y = (allocInfo.lstUnits[x][y].CabinetPos.TopLeft.y + allocInfo.lstUnits[x][y].CabinetPos.TopRight.y + allocInfo.lstUnits[x][y].CabinetPos.BottomRight.y + allocInfo.lstUnits[x][y].CabinetPos.BottomLeft.y) / 4;
                    double _z = (allocInfo.lstUnits[x][y].CabinetPos.TopLeft.z + allocInfo.lstUnits[x][y].CabinetPos.TopRight.z + allocInfo.lstUnits[x][y].CabinetPos.BottomRight.z + allocInfo.lstUnits[x][y].CabinetPos.BottomLeft.z) / 4;

                    if (allocInfo.LEDModel == ZRD_C15A
                        || allocInfo.LEDModel == ZRD_B15A
                        || allocInfo.LEDModel == ZRD_CH15D
                        || allocInfo.LEDModel == ZRD_BH15D
                        || allocInfo.LEDModel == ZRD_CH15D_S3
                        || allocInfo.LEDModel == ZRD_BH15D_S3)
                    {
#if (DEBUG || Release_log)
                        if (Settings.Ins.ExecLog == true)
                        {
                            MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                        }
#endif
                        m_Spec_by_Zdistance[x][y] = 0.003 * _z / 1000;
                    }
                    else
                    {
#if (DEBUG || Release_log)
                        if (Settings.Ins.ExecLog == true)
                        {
                            MainWindow.SaveExecFuncLog("LEDModel : " + allocInfo.LEDModel);
                        }
#endif
                        m_Spec_by_Zdistance[x][y] = 0.003 * _z / 1000 / (1.26 / 1.58);
                    }
                    if (m_Spec_by_Zdistance[x][y] < 0.015)
                    {
                        m_Spec_by_Zdistance[x][y] = 0.015;
                    }

                }
            }
        }
#endif

        bool DetectCorner(Mat matBin, Mat matMono, out PointF[] corner)
        {
            corner = new PointF[4];
            corner[0] = new PointF(0, 0);
            corner[1] = new PointF(0, 0);
            corner[2] = new PointF(0, 0);
            corner[3] = new PointF(0, 0);

            OpenCvSharp.Point[][] contours;
            OpenCvSharp.HierarchyIndex[] hindex;
            Cv2.FindContours(matBin, out contours, out hindex, RetrievalModes.External, ContourApproximationModes.ApproxNone);

            if(contours.Length == 0)
            {
                return false;
            }

            int max = 0;
            int max_index = 0;
            for(int n=0;n<contours.Length;n++)
            {
                if(contours[n].Length > max)
                {
                    max = contours[n].Length;
                    max_index = n;
                }
            }

            // 外接矩形から、はみ出していないかチェック
            OpenCvSharp.Rect boundingRect = Cv2.BoundingRect(contours[max_index]);
            if(boundingRect.Left <= 0 || boundingRect.Right >= matBin.Width - 1 || boundingRect.Top <= 0 || boundingRect.Bottom >= matBin.Height - 1)
            {
                return false;
            }

            RotatedRect rect = Cv2.MinAreaRect(contours[max_index]);
            Point2f[] aryP = rect.Points();
            // Xでソート
            Array.Sort(aryP, (a, b) => (int)(a.X - b.X));
            if(aryP[0].Y < aryP[1].Y)
            {
                corner[0].X = aryP[0].X;
                corner[0].Y = aryP[0].Y;

                corner[3].X = aryP[1].X;
                corner[3].Y = aryP[1].Y;
            }
            else
            {
                corner[0].X = aryP[1].X;
                corner[0].Y = aryP[1].Y;

                corner[3].X = aryP[0].X;
                corner[3].Y = aryP[0].Y;
            }
            if (aryP[2].Y < aryP[3].Y)
            {
                corner[1].X = aryP[2].X;
                corner[1].Y = aryP[2].Y;

                corner[2].X = aryP[3].X;
                corner[2].Y = aryP[3].Y;
            }
            else
            {
                corner[1].X = aryP[3].X;
                corner[1].Y = aryP[3].Y;

                corner[2].X = aryP[2].X;
                corner[2].Y = aryP[2].Y;
            }


            if (corner[0].X <= 0 || corner[0].X >= matBin.Width - 1 || corner[0].Y <= 0 || corner[0].Y >= matBin.Height - 1 ||
                corner[1].X <= 0 || corner[1].X >= matBin.Width - 1 || corner[1].Y <= 0 || corner[1].Y >= matBin.Height - 1 ||
                corner[2].X <= 0 || corner[2].X >= matBin.Width - 1 || corner[2].Y <= 0 || corner[2].Y >= matBin.Height - 1 ||
                corner[3].X <= 0 || corner[3].X >= matBin.Width - 1 || corner[3].Y <= 0 || corner[3].Y >= matBin.Height - 1)
            {
                return false;
            }


            for (int n = 0; n < 5; n++)
            {
                float a = 0, b = 0;
                float c = 0, d = 0;

                PointF[] corner0 = new PointF[4];
                PointF[,,] cornerLine = new PointF[4, 2, 2];  // 位置, H/V, start/end

                int margin = 500;
                int target = 500;
                corner0[0] = corner[0];
                corner0[1] = corner[1];
                corner0[2] = corner[2];
                corner0[3] = corner[3];

                Rectangle rectTarget = new Rectangle();
                List<Point2f> listFind;
                Line2D line;

                // 左上コーナー詳細
                // 上辺
                rectTarget.X = (int)corner0[0].X + margin;
                rectTarget.Y = (int)corner0[0].Y - margin;
                rectTarget.Width = target;
                rectTarget.Height = margin + target;
                FindPixelPos(matMono, rectTarget, out listFind, 0);
                line = Cv2.FitLine(listFind, DistanceTypes.L1, 0, 0.01, 0.01);
                a = (float)(line.Vy / line.Vx);
                b = (float)(-line.Vy / line.Vx * line.X1 + line.Y1);
                
                // 左辺
                rectTarget.X = (int)corner0[0].X - margin;
                rectTarget.Y = (int)corner0[0].Y + margin;
                rectTarget.Width = margin + target;
                rectTarget.Height = target;
                FindPixelPos(matMono, rectTarget, out listFind, 2);
                line = Cv2.FitLine(listFind, DistanceTypes.L1, 0, 0.01, 0.01);
                c = (float)(line.Vy / line.Vx);
                d = (float)(-line.Vy / line.Vx * line.X1 + line.Y1);

                // コーナー
                corner[0].X = (c * b + d) / (1.0F - c * a);
                corner[0].Y = a * corner[0].X + b;


                // 右上コーナー詳細
                // 上辺
                rectTarget.X = (int)corner0[1].X - margin - target;
                rectTarget.Y = (int)corner0[1].Y - margin;
                rectTarget.Width = target;
                rectTarget.Height = margin + target;
                FindPixelPos(matMono, rectTarget, out listFind, 0);
                line = Cv2.FitLine(listFind, DistanceTypes.L1, 0, 0.01, 0.01);
                a = (float)(line.Vy / line.Vx);
                b = (float)(-line.Vy / line.Vx * line.X1 + line.Y1);

                // 右辺
                rectTarget.X = (int)corner0[1].X - margin;
                rectTarget.Y = (int)corner0[1].Y + margin;
                rectTarget.Width = margin + target;
                rectTarget.Height = target;
                FindPixelPos(matMono, rectTarget, out listFind, 3);
                line = Cv2.FitLine(listFind, DistanceTypes.L1, 0, 0.01, 0.01);
                c = (float)(line.Vy / line.Vx);
                d = (float)(-line.Vy / line.Vx * line.X1 + line.Y1);

                // コーナー
                corner[1].X = (c * b + d) / (1.0F - c * a);
                corner[1].Y = a * corner[1].X + b;


                // 右下コーナー詳細
                // 下辺
                rectTarget.X = (int)corner0[2].X - margin - target;
                rectTarget.Y = (int)corner0[2].Y - margin;
                rectTarget.Width = target;
                rectTarget.Height = margin + target;
                FindPixelPos(matMono, rectTarget, out listFind, 1);
                line = Cv2.FitLine(listFind, DistanceTypes.L1, 0, 0.01, 0.01);
                a = (float)(line.Vy / line.Vx);
                b = (float)(-line.Vy / line.Vx * line.X1 + line.Y1);

                // 右辺
                rectTarget.X = (int)corner0[2].X - margin;
                rectTarget.Y = (int)corner0[2].Y - margin - target;
                rectTarget.Width = margin + target;
                rectTarget.Height = target;
                FindPixelPos(matMono, rectTarget, out listFind, 3);
                line = Cv2.FitLine(listFind, DistanceTypes.L1, 0, 0.01, 0.01);
                c = (float)(line.Vy / line.Vx);
                d = (float)(-line.Vy / line.Vx * line.X1 + line.Y1);

                // コーナー
                corner[2].X = (c * b + d) / (1.0F - c * a);
                corner[2].Y = a * corner[2].X + b;


                // 左下コーナー詳細
                // 下辺
                rectTarget.X = (int)corner0[3].X + margin;
                rectTarget.Y = (int)corner0[3].Y - margin;
                rectTarget.Width = target;
                rectTarget.Height = margin + target;
                FindPixelPos(matMono, rectTarget, out listFind, 1);
                line = Cv2.FitLine(listFind, DistanceTypes.L1, 0, 0.01, 0.01);
                a = (float)(line.Vy / line.Vx);
                b = (float)(-line.Vy / line.Vx * line.X1 + line.Y1);

                // 左辺
                rectTarget.X = (int)corner0[3].X - margin;
                rectTarget.Y = (int)corner0[3].Y - margin - target;
                rectTarget.Width = margin + target;
                rectTarget.Height = target;
                FindPixelPos(matMono, rectTarget, out listFind, 2);
                line = Cv2.FitLine(listFind, DistanceTypes.L1, 0, 0.01, 0.01);
                c = (float)(line.Vy / line.Vx);
                d = (float)(-line.Vy / line.Vx * line.X1 + line.Y1);

                // コーナー
                corner[3].X = (c * b + d) / (1.0F - c * a);
                corner[3].Y = a * corner[3].X + b;


                if (corner[0].X <= 0 || corner[0].X >= matMono.Width - 1 || corner[0].Y <= 0 || corner[0].Y >= matMono.Height - 1 ||
                    corner[1].X <= 0 || corner[1].X >= matMono.Width - 1 || corner[1].Y <= 0 || corner[1].Y >= matMono.Height - 1 ||
                    corner[2].X <= 0 || corner[2].X >= matMono.Width - 1 || corner[2].Y <= 0 || corner[2].Y >= matMono.Height - 1 ||
                    corner[3].X <= 0 || corner[3].X >= matMono.Width - 1 || corner[3].Y <= 0 || corner[3].Y >= matMono.Height - 1)
                {
                    return false;
                }

                if (float.IsNaN(corner[0].X) == true || float.IsNaN(corner[0].Y) == true ||
                    float.IsNaN(corner[1].X) == true || float.IsNaN(corner[1].Y) == true ||
                    float.IsNaN(corner[2].X) == true || float.IsNaN(corner[2].Y) == true ||
                    float.IsNaN(corner[3].X) == true || float.IsNaN(corner[3].Y) == true)
                {
                    return false;
                }
            }

            return true;
        }



        unsafe public void FindPixelPos(Mat mat, Rectangle rect, out List<Point2f> pos, int flag)
        {
            pos = new List<Point2f>();

            if (rect.X < 0)
                rect.X = 0;
            if (rect.X > mat.Width - 1)
                rect.X = mat.Width - 1;
            if (rect.X + rect.Width - 1 > mat.Width - 1)
                rect.Width = mat.Width - 1 - (rect.X - 1);

            if (rect.Y < 0)
                rect.Y = 0;
            if (rect.Y > mat.Height - 1)
                rect.Y = mat.Height - 1;
            if (rect.Y + rect.Height - 1 > mat.Height - 1)
                rect.Height = mat.Height - 1 - (rect.Y - 1);

            OpenCvSharp.Rect r = new OpenCvSharp.Rect(new OpenCvSharp.Point(rect.X, rect.Y), new OpenCvSharp.Size(rect.Width, rect.Height));
            Mat matRect = mat[r];
            Mat matBin = new Mat();
            Cv2.Threshold(matRect, matBin, 0, 255, ThresholdTypes.Otsu);

            //matBin.SaveImage("c:\\temp\\rectBin.bmp");

            byte* pBin = (byte*)matBin.Data;

            if (flag == 0 || flag == 1)
            {
                for (int x = 0; x < matBin.Width; x++)
                {
                    // 上辺
                    if (flag == 0)
                    {
                        for (int y = 0; y < matBin.Height; y++)
                        {
                            if (pBin[y * matBin.Width + x] != 0)
                            {
                                pos.Add(new Point2f(x + rect.X, y + rect.Y));
                                break;
                            }
                        }
                    }
                    // 下辺
                    else
                    {
                        for (int y = matBin.Height - 1; y >= 0; y--)
                        {
                            if (pBin[y * matBin.Width + x] != 0)
                            {
                                pos.Add(new Point2f(x + rect.X, y + rect.Y));
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                for (int y = 0; y < matBin.Height; y++)
                {
                    // 左辺
                    if (flag == 2)
                    {
                        for (int x = 0; x < matBin.Width; x++)
                        {
                            if (pBin[y * matBin.Width + x] != 0)
                            {
                                pos.Add(new Point2f(y + rect.Y, x + rect.X));
                                break;
                            }
                        }
                    }
                    // 右辺
                    else
                    {
                        for (int x = matBin.Width - 1; x >= 0; x--)
                        {
                            if (pBin[y * matBin.Width + x] != 0)
                            {
                                pos.Add(new Point2f(y + rect.Y, x + rect.X));
                                break;
                            }
                        }
                    }
                }
            }

        }


#endif


        void saveLog(string log)
        {
            string str;
#if NoEncript
            using (StreamWriter sw = new StreamWriter(m_CamMeasPath + "log.txt", true))
#else
            string encryptStr;
            using (StreamWriter sw = new StreamWriter(m_CamMeasPath + "log.txtx", true))
#endif
            {
                DateTime now = DateTime.Now;
                str = string.Format("{0:D4}/{1:D2}/{2:D2},{3:D2}:{4:D2}:{5:D2}.{6},{7}", now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Millisecond, log);
#if NoEncript
                sw.WriteLine(str);
#else
                encryptStr = Encrypt(str, CalcKey(AES_IV), CalcKey(AES_Key));
                sw.WriteLine(encryptStr);
#endif
            }

#if NoEncript
#else
            /**/
            // 確認用
            using (StreamReader sr = new StreamReader(m_CamMeasPath + "log.txtx"))
            {
                str = sr.ReadToEnd();
            }
            string[] lines = str.Split('\n');
            using (StreamWriter sw = new StreamWriter(m_CamMeasPath + "log.txt"))
            {
                for (int n = 0; n < lines.Length - 1; n++)    // 最後のlines[]は、""のため
                {
                    str = Decrypt(lines[n], CalcKey(AES_IV), CalcKey(AES_Key));
                    sw.WriteLine(str);
                }
            }
            /**/
#endif
        }


        unsafe bool SaveMatBinary(Mat mat, string filename)
        {
            filename += ".matbin";

            // modified by Hotta 2022/07/13
#if Coverity
            using (Mat destMat = new Mat())
            {
                //mat.ConvertTo(destMat, MatType.CV_16UC1);
                mat.ConvertTo(destMat, mat.Type());

                using (BinaryWriter bw = new BinaryWriter(new FileStream(filename, FileMode.Create)))
                {
                    long type = destMat.Type();
                    long width = destMat.Width;
                    long height = destMat.Height;
                    long ch = destMat.Channels();

                    bw.Write(type);
                    bw.Write(width);
                    bw.Write(height);
                    bw.Write(ch);

                    /*
                    byte[] array = new byte[mat.ElemSize() * mat.Total()];
                    Marshal.Copy(mat.Data, array, 0, array.Length);
                    bw.Write(array, 0, array.Length);
                    */

                    byte[] array = new byte[destMat.ElemSize() * width];
                    for (int y = 0; y < height; y++)
                    {
                        Marshal.Copy(destMat.Data + array.Length * y, array, 0, array.Length);
                        bw.Write(array, 0, array.Length);
                    }

                    array = null;
                    GC.Collect();
                }
                //destMat.Dispose();
            }
#else
            Mat destMat = new Mat();
            //mat.ConvertTo(destMat, MatType.CV_16UC1);
            mat.ConvertTo(destMat, mat.Type());

            using (BinaryWriter bw = new BinaryWriter(new FileStream(filename, FileMode.Create)))
            {
                long type = destMat.Type();
                long width = destMat.Width;
                long height = destMat.Height;
                long ch = destMat.Channels();

                bw.Write(type);
                bw.Write(width);
                bw.Write(height);
                bw.Write(ch);

                /*
                byte[] array = new byte[mat.ElemSize() * mat.Total()];
                Marshal.Copy(mat.Data, array, 0, array.Length);
                bw.Write(array, 0, array.Length);
                */

                byte[] array = new byte[destMat.ElemSize() * width];
                for (int y = 0; y < height; y++)
                {
                    Marshal.Copy(destMat.Data + array.Length * y, array, 0, array.Length);
                    bw.Write(array, 0, array.Length);
                }

                array = null;
                GC.Collect();
            }
            destMat.Dispose();
#endif

#if NoEncript
#else
            EncryptFile(filename, filename + "x", CalcKey(AES_Key), CalcKey(AES_IV));

            File.Delete(filename);
#endif
            string arwFile = Path.GetDirectoryName(filename) + "\\" + Path.GetFileNameWithoutExtension(filename) + ".arw";
            if (File.Exists(arwFile) == true)
                File.Delete(arwFile);

            /*
            string bmpFile = Path.GetDirectoryName(filename) + "\\" + Path.GetFileNameWithoutExtension(filename) + ".bmp";
            if (File.Exists(bmpFile) == true)
                File.Delete(bmpFile);
            */

            return true;
        }

        unsafe bool LoadMatBinary(string filename, out Mat mat)
        {
            // added by Hotta 2024/01/10
            CheckUserAbort();
            //


            filename += ".matbin";
#if NoEncript
#else
            DecryptFile(filename + "x", filename, CalcKey(AES_Key), CalcKey(AES_IV));
#endif
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                byte[] array = new byte[8];

                fs.Read(array, 0, array.Length);
                long type = BitConverter.ToInt64(array, 0);

                fs.Read(array, 0, array.Length);
                long width = BitConverter.ToInt64(array, 0);

                fs.Read(array, 0, array.Length);
                long height = BitConverter.ToInt64(array, 0);

                fs.Read(array, 0, array.Length);
                long ch = BitConverter.ToInt64(array, 0);

                mat = new Mat(new OpenCvSharp.Size(width, height), (MatType)type);

                //array = new byte[mat.ElemSize() * mat.Total()];
                //fs.Read(array, 0, array.Length);
                //Marshal.Copy(array, 0, mat.Data, (int)(mat.ElemSize() * mat.Total()));

                array = new byte[mat.ElemSize() * width];
                for (int y = 0; y < height; y++)
                {
                    fs.Read(array, 0, array.Length);
                    Marshal.Copy(array, 0, mat.Data + array.Length * y, array.Length);
                }

                array = null;
                GC.Collect();
            }
#if NoEncript
#else
            File.Delete(filename);
#endif
            return true;
        }

#if NoEncript
#else
        /// <summary>
        /// ファイルを暗号化する
        /// </summary>
        /// <param name="sourceFile">暗号化するファイルパス</param>
        /// <param name="destFile">暗号化されたデータを保存するファイルパス</param>
        /// <param name="key">暗号化に使用した共有キー</param>
        /// <param name="iv">暗号化に使用した初期化ベクタ</param>
        public static void EncryptFile(string sourceFile, string destFile, string key, string iv)
        {
            //RijndaelManagedオブジェクトを作成
            System.Security.Cryptography.RijndaelManaged rijndael =
                new System.Security.Cryptography.RijndaelManaged();

            //設定を変更するときは、変更する
            //rijndael.KeySize = 256;
            //rijndael.BlockSize = 128;
            //rijndael.FeedbackSize = 128;
            //rijndael.Mode = System.Security.Cryptography.CipherMode.CBC;
            //rijndael.Padding = System.Security.Cryptography.PaddingMode.PKCS7;

            //共有キーと初期化ベクタを作成
            //Key、IVプロパティがnullの時に呼びだすと、自動的に作成される
            //自分で作成するときは、GenerateKey、GenerateIVメソッドを使う
            //key = rijndael.Key;
            //iv = rijndael.IV;
            rijndael.IV = Encoding.UTF8.GetBytes(iv);
            rijndael.Key = Encoding.UTF8.GetBytes(key);


            //暗号化されたファイルを書き出すためのFileStream
            System.IO.FileStream outFs = new System.IO.FileStream(
                destFile, System.IO.FileMode.Create, System.IO.FileAccess.Write);
            //対称暗号化オブジェクトの作成
            System.Security.Cryptography.ICryptoTransform encryptor =
                rijndael.CreateEncryptor();
            //暗号化されたデータを書き出すためのCryptoStreamの作成
            System.Security.Cryptography.CryptoStream cryptStrm =
                new System.Security.Cryptography.CryptoStream(
                    outFs, encryptor,
                    System.Security.Cryptography.CryptoStreamMode.Write);

            //暗号化されたデータを書き出す
            System.IO.FileStream inFs = new System.IO.FileStream(
                sourceFile, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            byte[] bs = new byte[1024];
            int readLen;
            while ((readLen = inFs.Read(bs, 0, bs.Length)) > 0)
            {
                cryptStrm.Write(bs, 0, readLen);
            }

            //閉じる
            inFs.Close();
            cryptStrm.Close();
            encryptor.Dispose();
            outFs.Close();
        }

        /// <summary>
        /// ファイルを復号化する
        /// </summary>
        /// <param name="sourceFile">復号化するファイルパス</param>
        /// <param name="destFile">復号化されたデータを保存するファイルパス</param>
        /// <param name="key">暗号化に使用した共有キー</param>
        /// <param name="iv">暗号化に使用した初期化ベクタ</param>
        public static void DecryptFile(string sourceFile, string destFile, string key, string iv)
        {
            //RijndaelManagedオブジェクトの作成
            System.Security.Cryptography.RijndaelManaged rijndael =
                new System.Security.Cryptography.RijndaelManaged();

            //共有キーと初期化ベクタを設定
            rijndael.IV = Encoding.UTF8.GetBytes(iv);
            rijndael.Key = Encoding.UTF8.GetBytes(key);

            //暗号化されたファイルを読み込むためのFileStream
            System.IO.FileStream inFs = new System.IO.FileStream(
                sourceFile, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            //対称復号化オブジェクトの作成
            System.Security.Cryptography.ICryptoTransform decryptor =
                rijndael.CreateDecryptor();
            //暗号化されたデータを読み込むためのCryptoStreamの作成
            System.Security.Cryptography.CryptoStream cryptStrm =
                new System.Security.Cryptography.CryptoStream(
                    inFs, decryptor,
                    System.Security.Cryptography.CryptoStreamMode.Read);

            //復号化されたデータを書き出す
            System.IO.FileStream outFs = new System.IO.FileStream(
                destFile, System.IO.FileMode.Create, System.IO.FileAccess.Write);
            byte[] bs = new byte[1024];
            int readLen;
            //復号化に失敗すると例外CryptographicExceptionが発生
            while ((readLen = cryptStrm.Read(bs, 0, bs.Length)) > 0)
            {
                outFs.Write(bs, 0, readLen);
            }

            //閉じる
            outFs.Close();
            cryptStrm.Close();
            decryptor.Dispose();
            inFs.Close();
        }

        /// <summary>
        /// 対称鍵暗号を使って文字列を暗号化する
        /// </summary>
        /// <param name="text">暗号化する文字列</param>
        /// <param name="iv">対称アルゴリズムの初期ベクター</param>
        /// <param name="key">対称アルゴリズムの共有鍵</param>
        /// <returns>暗号化された文字列</returns>
        public static string Encrypt(string text, string iv, string key)
        {

            using (System.Security.Cryptography.RijndaelManaged rijndael = new System.Security.Cryptography.RijndaelManaged())
            {
                rijndael.BlockSize = 128;
                rijndael.KeySize = 128;
                rijndael.Mode = System.Security.Cryptography.CipherMode.CBC;
                rijndael.Padding = System.Security.Cryptography.PaddingMode.PKCS7;

                rijndael.IV = Encoding.UTF8.GetBytes(iv);
                rijndael.Key = Encoding.UTF8.GetBytes(key);

                System.Security.Cryptography.ICryptoTransform encryptor = rijndael.CreateEncryptor(rijndael.Key, rijndael.IV);

                byte[] encrypted;
                using (MemoryStream mStream = new MemoryStream())
                {
                    using (System.Security.Cryptography.CryptoStream ctStream = new System.Security.Cryptography.CryptoStream(mStream, encryptor, System.Security.Cryptography.CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(ctStream))
                        {
                            sw.Write(text);
                        }
                        encrypted = mStream.ToArray();
                    }
                }
                return (System.Convert.ToBase64String(encrypted));
            }
        }

        /// <summary>
        /// 対称鍵暗号を使って暗号文を復号する
        /// </summary>
        /// <param name="cipher">暗号化された文字列</param>
        /// <param name="iv">対称アルゴリズムの初期ベクター</param>
        /// <param name="key">対称アルゴリズムの共有鍵</param>
        /// <returns>復号された文字列</returns>
        public static string Decrypt(string cipher, string iv, string key)
        {
            using (System.Security.Cryptography.RijndaelManaged rijndael = new System.Security.Cryptography.RijndaelManaged())
            {
                rijndael.BlockSize = 128;
                rijndael.KeySize = 128;
                rijndael.Mode = System.Security.Cryptography.CipherMode.CBC;
                rijndael.Padding = System.Security.Cryptography.PaddingMode.PKCS7;

                rijndael.IV = Encoding.UTF8.GetBytes(iv);
                rijndael.Key = Encoding.UTF8.GetBytes(key);

                System.Security.Cryptography.ICryptoTransform decryptor = rijndael.CreateDecryptor(rijndael.Key, rijndael.IV);

                string plain = string.Empty;
                using (MemoryStream mStream = new MemoryStream(System.Convert.FromBase64String(cipher)))
                {
                    using (System.Security.Cryptography.CryptoStream ctStream = new System.Security.Cryptography.CryptoStream(mStream, decryptor, System.Security.Cryptography.CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(ctStream))
                        {
                            plain = sr.ReadToEnd();
                        }
                    }
                }
                return plain;
            }
        }

        // deleted by Hotta 2022/02/28
        // UfCamera.csの方を使用
        /*
        private static string CalcKey(string key)
        {
            string newKey = "";

            newKey = new string(key.Substring(8, 8).Reverse().ToArray()) + key.Substring(0, 8).Replace(key.Substring(3, 1), "@");

            return newKey;
        }
        */
#endif


        bool checkFileSize(string path)
        {
            bool status = false;

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Reset();
            sw.Start();
            while(true)
            {
                FileInfo file = new FileInfo(path);
                long size0 = file.Length;
                Thread.Sleep(100);
                file = new FileInfo(path);
                long size1 = file.Length;
                if (size0 == size1)
                {
                    status = true;
                    break;
                }
                if(sw.ElapsedMilliseconds > 10000)
                {
                    status = false;
                    break;
                }
                Thread.Sleep(100);
            }
            sw.Stop();

            if (status != true)
                return false;


            FileStream stream = null;
            sw.Reset();
            sw.Start();
            while(true)
            {
                try
                {
                    status = true;
                    stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                }
                catch //(Exception ex)
                {
                    status = false;
                    Thread.Sleep(100);
                    if (sw.ElapsedMilliseconds > 10000)
                    {
                        break;
                    }
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Close();
                    }
                }
                if(status == true)
                    break;
            }
            return status;
        }

        // added by Hotta 2025/01/31
        public class CameraCasUserAbortException : Exception
        {
            public CameraCasUserAbortException()
            {
            }

            public CameraCasUserAbortException(string message)
                : base(message)
            {
            }

            public CameraCasUserAbortException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }

        // added by Hotta 2024/12/25
        void CheckUserAbort()
        {
            System.Windows.Forms.Application.DoEvents();

            if (winProgress == null)
                return;

            UserOperation operation = winProgress.Operation;
            if (operation == UserOperation.Cancel)
            {
                saveLog("Detected ESC key pressed.");
                winProgress.PauseRemainTimer();

                bool? result;
                if(winProgress.AbortType == WindowProgress.TAbortType.Adjustment)
                    showMessageWindow(out result, "Detected ESC key pressed.\r\nDo you want to abort the adjustment process?", "Confirm", System.Drawing.SystemIcons.Question, 500, 200, "Yes", "No");
                else
                    showMessageWindow(out result, "Detected ESC key pressed.\r\nDo you want to abort the measurement process?", "Confirm", System.Drawing.SystemIcons.Question, 500, 200, "Yes", "No");

                if (result == true)
                {
                    if (winProgress.AbortType == WindowProgress.TAbortType.Adjustment)
                    {
                        winProgress.AbortType = WindowProgress.TAbortType.None;
                        winProgress.ShowMessage("Aborting the adjustment process.");
                        winProgress.PutForward1Step();
                        DoEvents();
                        saveLog("Aborting the adjustment process.");
                        // modified by Hotta 2025/01/31
                        /*
                        throw new Exception("Abort the adjustment process.");
                        */
                        throw new CameraCasUserAbortException("Abort the adjustment process.");
                    }
                    else
                    {
                        winProgress.AbortType = WindowProgress.TAbortType.None;
                        winProgress.ShowMessage("Aborting the measurement process.");
                        winProgress.PutForward1Step();
                        DoEvents();
                        saveLog("Aborting the measurement process.");
                        // modified by Hotta 2025/01/31
                        /*
                        throw new Exception("Abort the measurement process.");
                        */
                        throw new CameraCasUserAbortException("Abort the measurement process.");
                    }
                }
                else
                {
                    saveLog("Resume the adjustment/measurement process.");
                    winProgress.ResumeRemainTimer();
                }
            }
            winProgress.Operation = UserOperation.None;
        }
        //

        #endregion Private Methods

        #region UseMonitor

        Bitmap m_Bmp;
        Form m_Splash;

        void outputMonitorChecker(int startX, int startY, int height, int width, int pitchH, int pitchV, int R, int G, int B)
        {
            if (m_Bmp != null)
                m_Bmp.Dispose();

            m_Bmp = new Bitmap(1920, 1080, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Rectangle rect = new Rectangle(0, 0, 1920, 1080);
            System.Drawing.Imaging.BitmapData bmpData = m_Bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            IntPtr ptr = bmpData.Scan0;
            int stride = bmpData.Stride;

            int bytes = stride * m_Bmp.Height;
            byte[] rgbValues = new byte[bytes];

            int x_count, y_count;
            y_count = 0;
            for (int y = 0; y < m_Bmp.Height; y += pitchV)
            {
                x_count = 0;
                for (int x = 0; x < m_Bmp.Width; x += pitchH)
                {
                    if ((x_count % 2 == 0 && y_count % 2 == 0) || (x_count % 2 == 1 && y_count % 2 == 1))
                    {
                        int initX, ix;
                        for (int _y = 0; _y < pitchV; _y++)
                        {
                            if (_y + y >= m_Bmp.Height)
                                break;
                            initX = stride * (_y + y);
                            for (int _x = 0; _x < pitchH; _x++)
                            {
                                if (_x + x >= m_Bmp.Width)
                                {
                                    break;
                                }
                                ix = initX + (_x + x) * 3;
                                rgbValues[ix + 0] = (byte)B;
                                rgbValues[ix + 1] = (byte)G;
                                rgbValues[ix + 2] = (byte)R;
                            }
                        }
                    }
                    x_count++;
                }
                y_count++;
            }
            //// Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            m_Bmp.UnlockBits(bmpData);

            ShowImage();
        }


        void outputMonitorTile(int startX, int startY, int height, int width, int pitchH, int pitchV, int R, int G, int B)
        {
            if (m_Bmp != null)
                m_Bmp.Dispose();

            m_Bmp = new Bitmap(1920, 1080, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Rectangle rect = new Rectangle(0, 0, 1920, 1080);
            System.Drawing.Imaging.BitmapData bmpData = m_Bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            IntPtr ptr = bmpData.Scan0;
            int stride = bmpData.Stride;

            int bytes = stride * m_Bmp.Height;
            byte[] rgbValues = new byte[bytes];

            // フラット
            /**/
            for (int y = 0; y < m_Bmp.Height; y += pitchV)
            {
                for (int x = 0; x < m_Bmp.Width; x += pitchH)
                {
                    int initX, ix;
                    for (int _y = 0; _y < height; _y++)
                    {
                        if (startY + _y + y >= m_Bmp.Height)
                            break;
                        initX = stride * (startY + _y + y);
                        for (int _x = 0; _x < width; _x++)
                        {
                            if (startX + _x + x >= m_Bmp.Width)
                            {
                                break;
                            }
                            ix = initX + (startX + _x + x) * 3;
                            rgbValues[ix + 0] = (byte)B;
                            rgbValues[ix + 1] = (byte)G;
                            rgbValues[ix + 2] = (byte)R;
                        }
                    }
                }
            }
            /**/


            // ラウンド
            /*
            int cabiXNum = 10;
            int cabiYNum = 10;
            int cabiWidth = (int)(m_LedPitch * 1920 * 2 / cabiXNum + 0.5);
            int cabiHeight = (int)(m_LedPitch * 1080 * 2 / cabiYNum + 0.5);
            float angle = 5.0f;
            float wd = 5000.0f * 0.9f;

            TransformImage.TransformImage transform = new TransformImage.TransformImage();

            for (int y = 0; y < cabiYNum; y++)
            {
                for (int x = 0; x < cabiXNum; x++)
                {
                    transform.Set3DPoints(0, -cabiWidth / 2 + cabiWidth / 4 + cabiWidth / 2 * 0, -cabiHeight / 2 + cabiHeight / 4 + cabiHeight / 2 * 0, 0);
                    transform.Set3DPoints(1, -cabiWidth / 2 + cabiWidth / 4 + cabiWidth / 2 * 1, -cabiHeight / 2 + cabiHeight / 4 + cabiHeight / 2 * 0, 0);
                    transform.Set3DPoints(2, -cabiWidth / 2 + cabiWidth / 4 + cabiWidth / 2 * 1, -cabiHeight / 2 + cabiHeight / 4 + cabiHeight / 2 * 1, 0);
                    transform.Set3DPoints(3, -cabiWidth / 2 + cabiWidth / 4 + cabiWidth / 2 * 0, -cabiHeight / 2 + cabiHeight / 4 + cabiHeight / 2 * 1, 0);

                    transform.SetTranslation(0, -cabiHeight * cabiYNum / 2 + cabiHeight / 2 + cabiHeight * y, 6926.1f);

                    transform.SetRx(0);
                    transform.SetRy(-angle / 2 - angle * (cabiXNum / 2 - 1) + angle * x); //  -17.5f + 5.0f * x;
                    transform.SetRz(0);

                    transform.SetShiftToCameraCoordinate(0, 0, wd - 6926.1f);

                    transform.CameraParameter.Set(m_CameraParam.f, m_CameraParam.SensorSizeH, m_CameraParam.SensorSizeV, 1920, 1080);

                    transform.Calc();

                    for(int n=0;n<4;n++)
                    {
                        int px = (int)transform.ImagePoints[n].X;
                        int py = (int)transform.ImagePoints[n].Y;

                        int initX, ix;
                        for (int _y = -5; _y <= 5; _y++)
                        {
                            for (int _x = -5; _x <= 5; _x++)
                            {
                                initX = stride * (py + _y);
                                ix = initX + (px + _x) * 3;
                                rgbValues[ix + 0] = (byte)B;
                                rgbValues[ix + 1] = (byte)G;
                                rgbValues[ix + 2] = (byte)R;
                            }
                        }
                    }
                }
            }
            */
            //


            //// Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            m_Bmp.UnlockBits(bmpData);

            ShowImage();
        }


        void outputMonitorWindow(int startX, int startY, int height, int width, int R, int G, int B)
        {
            if (m_Bmp != null)
                m_Bmp.Dispose();

            m_Bmp = new Bitmap(1920, 1080, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Rectangle rect = new Rectangle(0, 0, 1920, 1080);
            System.Drawing.Imaging.BitmapData bmpData = m_Bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            IntPtr ptr = bmpData.Scan0;
            int stride = bmpData.Stride;

            int bytes = stride * m_Bmp.Height;
            byte[] rgbValues = new byte[bytes];

            int initX, ix;
            for (int y = startY; y < startY + height; y++)
            {
                if (y >= m_Bmp.Height)
                    break;
                initX = stride * y;
                for (int x = startX; x < startX + width; x++)
                {
                    if (x >= m_Bmp.Width)
                        break;

                    ix = initX + x * 3;
                    rgbValues[ix + 0] = (byte)B;
                    rgbValues[ix + 1] = (byte)G;
                    rgbValues[ix + 2] = (byte)R;
                }
            }
            //// Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            m_Bmp.UnlockBits(bmpData);

            ShowImage();
        }

        void ShowImage()
        {
            m_Splash = new Form();
            m_Splash.ShowInTaskbar = false;
            m_Splash.FormBorderStyle = FormBorderStyle.None;
            m_Splash.StartPosition = FormStartPosition.Manual;

            m_Splash.Width = 1920;
            m_Splash.Height = 1080;

            m_Splash.SetBounds(1920 * 2, 0, 0, 0, BoundsSpecified.Location);
            //m_Splash.SetBounds(1920 * 1, 0, 0, 0, BoundsSpecified.Location);

            m_Splash.BackgroundImage = m_Bmp;

            m_Splash.Show();
            m_Splash.Refresh();
        }



        #endregion UseMonitor


        #region Remain Time Fields

        int GAP_RE_CALC_STEP_5_PROCESS_SEC = 105;

        int GAP_INITIAL_SETTINGS_SEC = 8;

        // 撮影
        int GAP_CAPTURE_BLACK_IMAGE_SEC = 22;
        int GAP_CAPTURE_FLAT_IMAGE_SEC = 9;
        int GAP_CAPTURE_WHITE_IMAGE_SEC = 9;
        int GAP_CAPTURE_TARGET_AREA_IMAGE_SEC = 18;
        int GAP_CAPTURE_MOIRE_AREA_SEC = 8;
        int GAP_CAPTURE_MOIRE_CHEKING_IMAGE_SEC = 9;
        double GAP_CAPTURE_TOP_BOTTOM_IMAGE_SEC = 8.2;
        double GAP_CAPTURE_LEFT_RIGHT_IMAGE_SEC = 8.2;
        double GAP_CAPTURE_GAP_IMAGE_SEC = 49;

        double SET_BULK_GAP_CORRECTION_SEC = 0.47;
        double SET_SINGLE_GAP_CORRECTION_ST_SEC = 0.2;

        double GAP_LOAD_TOP_BOTTOM_IMAGE_SEC = 0.3;
        double GAP_LOAD_LEFT_RIGHT_IMAGE_SEC = 0.3;
        double GAP_LOAD_GAP_IMAGE_SEC = 0.25;
        double GAP_CALC_GAP_GAIN_SEC = 0.04; // Cabinet単位

        // ROM Writing
        int GAP_WRITE_PANEL_OFF = 1;
        int GAP_WRITE_WRITE_CORRECTION_VALUE = 1;
        int GAP_WRITE_THREAD_SLEEP = 10;
        int GAP_WRITE_PANEL_ON = 1;

        int GAP_CAPTURE_TOP_BOTTOM_IMAGE_COUNT = 7;
        int GAP_CAPTURE_LEFT_RIGHT_IMAGE_COUNT = 7;
        int GAP_CAPTURE_GAP_IMAGE_COUNT = 9;
        int GAP_LOAD_TOP_BOTTOM_IMAGE_COUNT = 7;
        int GAP_LOAD_LEFT_RIGHT_IMAGE_COUNT = 7;
        int GAP_LOAD_GAP_IMAGE_COUNT = 9;
        int GAP_CALC_GAP_GAIN_COUNT = 9;
        int GAP_RESULT_CAPTURE_GAP_IMAGE_COUNT = 5;
        int GAP_RESULT_LOAD_GAP_IMAGE_COUNT = 5;
        int GAP_RESULT_CALC_GAP_GAIN_COUNT = 5;

        int[] m_AryProcessSec;

        #endregion Remain Time Fields


        #region Remain Time Private Methods

        /// <summary>
        /// Measurement処理にかかる推定時間を算出
        /// <param name="cabinetCount">選択Cabinet数</param>
        /// </summary>
        private int initialGapCameraMeasurementProcessSec(int cabinetCount)
        {
            //process[0]  初期処理
            int _step0 = GAP_INITIAL_SETTINGS_SEC
                        + USER_SETTING_SEC
                        + ADJUST_SETTING_SEC;
            //process[1]  Auto Focus
            int _step1 = AUTO_FOCUS_SEC;
            //process[2]  開始時のカメラ位置を保存
            int _step2 = STORE_CAMERA_POSITION_SEC;
            //process[3]  撮影
            //process[3.1]  Capture Black image
            //process[3.2]  Capture Flat image
            //process[3.3]  Capture White image
            //process[3.4]  Capture Target area image
            //process[3.5]  Capture Moire area
            //process[3.6]  Capture Moire checking image
            //process[3.7]  Capture Trimming Area image(Capture Gap Position image / Capture Top/Bottom Image / Capture Right/Left Image)
            //process[3.8]  Capture Gap image
            int _step3 = GAP_CAPTURE_BLACK_IMAGE_SEC
                        + GAP_CAPTURE_FLAT_IMAGE_SEC
                        + GAP_CAPTURE_WHITE_IMAGE_SEC
                        + GAP_CAPTURE_TARGET_AREA_IMAGE_SEC
                        + GAP_CAPTURE_MOIRE_AREA_SEC
                        + GAP_CAPTURE_MOIRE_CHEKING_IMAGE_SEC
                        + (int)Math.Round(GAP_CAPTURE_TOP_BOTTOM_IMAGE_SEC * GAP_CAPTURE_TOP_BOTTOM_IMAGE_COUNT)
                        + (int)Math.Round(GAP_CAPTURE_LEFT_RIGHT_IMAGE_SEC * GAP_CAPTURE_LEFT_RIGHT_IMAGE_COUNT)
                        + (int)Math.Round(GAP_CAPTURE_GAP_IMAGE_SEC * GAP_CAPTURE_GAP_IMAGE_COUNT);
            //process[4]   処理
            int _step4 = (int)Math.Round(GAP_LOAD_TOP_BOTTOM_IMAGE_SEC * GAP_LOAD_TOP_BOTTOM_IMAGE_COUNT)
                        + (int)Math.Round(GAP_LOAD_LEFT_RIGHT_IMAGE_SEC * GAP_LOAD_LEFT_RIGHT_IMAGE_COUNT)
                        + (int)Math.Round(GAP_LOAD_GAP_IMAGE_SEC * GAP_LOAD_GAP_IMAGE_COUNT)
                        + (int)Math.Round(GAP_CALC_GAP_GAIN_SEC * cabinetCount * GAP_CALC_GAP_GAIN_COUNT);
            //process[5]   
            //process[5.1]  終了時のカメラ位置を保存
            //process[5.2]  調整用設定を解除
            //process[5.3]  ユーザー設定に書き戻し
            int _step5 = STORE_CAMERA_POSITION_SEC
                        + ADJUST_SETTING_SEC
                        + USER_SETTING_SEC;

            m_AryProcessSec = new int[6] { _step0, _step1, _step2, _step3, _step4, _step5 };
            int processSec = m_AryProcessSec.Sum();
            saveLog($"MeasurementProcessSec: process[0]:{_step0} process[1]:{_step1} process[2]:{_step2} process[3]:{_step3} process[4]:{_step4} process[5]:{_step5}");

            return processSec;
        }

        /// <summary>
        /// MeasurementのRemainTimeを算出
        /// </summary>
        /// <param name="step"></param>
        /// <returns></returns>
        private int calcGapCameraMeasurementProcessSec(int step)
        {
            int processSec = m_AryProcessSec.Skip(step).Sum();
            saveLog("GapCameraMeasurementProcessSec step:" + step + " stepSec:" + m_AryProcessSec[step] + " processSec:" + processSec);

            return processSec;
        }

        /// <summary>
        /// Adjustment処理にかかる推定時間を算出
        /// </summary>
        /// <param name="count">選択Cabinet数</param>
        /// <returns></returns>
        private int initialGapCameraAdjustmentProcessSec(int count)
        {
            //process[0]  初期処理
            int _step0 = GAP_INITIAL_SETTINGS_SEC
                        + USER_SETTING_SEC
                        + ADJUST_SETTING_SEC;
            //process[1]  Auto Focus
            int _step1 = AUTO_FOCUS_SEC;
            //process[2]  開始時のカメラ位置を保存
            int _step2 = STORE_CAMERA_POSITION_SEC;
            //process[3]  撮影
            int _step3 = GAP_CAPTURE_BLACK_IMAGE_SEC
                        + GAP_CAPTURE_FLAT_IMAGE_SEC
                        + GAP_CAPTURE_WHITE_IMAGE_SEC
                        + GAP_CAPTURE_TARGET_AREA_IMAGE_SEC
                        + GAP_CAPTURE_MOIRE_AREA_SEC
                        + GAP_CAPTURE_MOIRE_CHEKING_IMAGE_SEC
                        + (int)Math.Round(GAP_CAPTURE_TOP_BOTTOM_IMAGE_SEC * GAP_CAPTURE_TOP_BOTTOM_IMAGE_COUNT)
                        + (int)Math.Round(GAP_CAPTURE_LEFT_RIGHT_IMAGE_SEC * GAP_CAPTURE_LEFT_RIGHT_IMAGE_COUNT)
                        + (int)Math.Round(GAP_CAPTURE_GAP_IMAGE_SEC * GAP_CAPTURE_GAP_IMAGE_COUNT);
            //process[4]  処理
            int _step4 = (int)Math.Round(GAP_LOAD_TOP_BOTTOM_IMAGE_SEC * GAP_LOAD_TOP_BOTTOM_IMAGE_COUNT)
                        + (int)Math.Round(GAP_LOAD_LEFT_RIGHT_IMAGE_SEC * GAP_LOAD_LEFT_RIGHT_IMAGE_COUNT)
                        + (int)Math.Round(GAP_LOAD_GAP_IMAGE_SEC * GAP_LOAD_GAP_IMAGE_COUNT)
                        + (int)Math.Round(GAP_CALC_GAP_GAIN_SEC * count * GAP_CALC_GAP_GAIN_COUNT);
            //process[5]  Set Gap Correction Value + Adjustment Result
            int _step5 = m_EvaluateAdjustmentResult
                       ? (int)Math.Round(SET_BULK_GAP_CORRECTION_SEC * moduleCount * count)
                        + (int)Math.Round(GAP_CAPTURE_GAP_IMAGE_SEC * GAP_RESULT_CAPTURE_GAP_IMAGE_COUNT)
                        + (int)Math.Round(GAP_LOAD_GAP_IMAGE_SEC * GAP_RESULT_LOAD_GAP_IMAGE_COUNT)
                        + (int)Math.Round(GAP_CALC_GAP_GAIN_SEC * count * GAP_RESULT_CALC_GAP_GAIN_COUNT)
                       : (int)Math.Round(SET_BULK_GAP_CORRECTION_SEC * moduleCount * count);

            //process[6]  全白撮影
            int _step6 = m_EvaluateAdjustmentResult
                       ? GAP_CAPTURE_WHITE_IMAGE_SEC
                        + STORE_CAMERA_POSITION_SEC
                       : 0;
            //process[7]
            int _step7 = ADJUST_SETTING_SEC
                        + USER_SETTING_SEC;

            m_AryProcessSec = new int[8] { _step0, _step1, _step2, _step3, _step4, _step5, _step6, _step7 };
            int processSec = m_AryProcessSec.Sum();
            saveLog($"AdjustmentProcessSec: process[0]:{_step0} process[1]:{_step1} process[2]:{_step2} process[3]:{_step3} process[4]:{_step4} process[5]:{_step5} process[6]:{_step6} process[7]:{_step7}");

            return processSec;
        }

        /// <summary>
        /// AdjustmentのRemainTimeを算出
        /// </summary>
        /// <param name="step"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private int calcGapCameraAdjustmentProcessSec(int step, int count = 0)
        {
            // All gap correction is in specではない場合RemainTime再計算
            if (step == GAP_RE_CALC_STEP_5_PROCESS_SEC)
            {
                m_AryProcessSec[5] = (int)Math.Round(SET_SINGLE_GAP_CORRECTION_ST_SEC * count)
                                    + (int)Math.Round(GAP_CAPTURE_GAP_IMAGE_SEC * GAP_RESULT_CAPTURE_GAP_IMAGE_COUNT)
                                    + (int)Math.Round(GAP_LOAD_GAP_IMAGE_SEC * GAP_RESULT_LOAD_GAP_IMAGE_COUNT)
                                    + (int)Math.Round(GAP_CALC_GAP_GAIN_SEC * count * GAP_RESULT_CALC_GAP_GAIN_COUNT);
                step = 5; // 残り時間の算出ためのstep戻し
            }

            int processSec = m_AryProcessSec.Skip(step).Sum();
            saveLog("GapCameraAdjustmentProcessSec step:" + step + " stepSec:" + m_AryProcessSec[step] + " processSec:" + processSec);

            return processSec;
        }

        /// <summary>
        /// 書き込み処理実行時間
        /// </summary>
        /// <returns></returns>
        private int initialGapCameraROMWriteProcessSec()
        {
            //Process[0]  ROM writing
            m_AryProcessSec = new int[6] { GAP_WRITE_PANEL_OFF, GAP_WRITE_THREAD_SLEEP, GAP_WRITE_WRITE_CORRECTION_VALUE, GAP_WRITE_THREAD_SLEEP, UNIT_RECONFIG_SEC, GAP_WRITE_PANEL_ON };

            int processSec = m_AryProcessSec.Sum();
            saveLog("calcGapCameraROMWriteProcessSec:" + processSec);

            return processSec;
        }

        #endregion Remain Time Private Methods

    }
}
