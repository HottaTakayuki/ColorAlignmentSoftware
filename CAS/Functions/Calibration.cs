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

using SONY.Modules;
using MakeUFData;

namespace CAS
{
    // Calibration
    public partial class MainWindow : Window
    {
        #region Fields

        private bool calibTabFirstSelect = true;

        // Calibration Custom
        private ChromCustom calibCustom = new ChromCustom(ColorPurpose.Calib);
        private ConfigChrom confirmChromType;

        #endregion

        #region Events

        private void showDefaultChromaticity()
        {
            ChromCustom defaultChrom = new ChromCustom(Settings.Ins.ConfigChromType);

            if (calibTabFirstSelect) // 最初Calib画面表示時
            {
                rbCalibDefault.Checked -= rbCalibDefault_Checked;
                rbCalibDefault.IsChecked = true;
                rbCalibDefault.Checked += rbCalibDefault_Checked;

                // Set Default
                txbCalibRed_x.Text = defaultChrom.Red.x.ToString();
                txbCalibRed_y.Text = defaultChrom.Red.y.ToString();
                txbCalibRed_Y.Text = defaultChrom.Red.Lv.ToString();
                txbCalibGreen_x.Text = defaultChrom.Green.x.ToString();
                txbCalibGreen_y.Text = defaultChrom.Green.y.ToString();
                txbCalibGreen_Y.Text = defaultChrom.Green.Lv.ToString();
                txbCalibBlue_x.Text = defaultChrom.Blue.x.ToString();
                txbCalibBlue_y.Text = defaultChrom.Blue.y.ToString();
                txbCalibBlue_Y.Text = defaultChrom.Blue.Lv.ToString();
                txbCalibWhite_x.Text = defaultChrom.White.x.ToString();
                txbCalibWhite_y.Text = defaultChrom.White.y.ToString();
                txbCalibWhite_Y.Text = defaultChrom.White.Lv.ToString();

                // Set Custom
                calibCustom.Red.x = defaultChrom.Red.x;
                calibCustom.Red.y = defaultChrom.Red.y;
                calibCustom.Red.Lv = defaultChrom.Red.Lv;
                calibCustom.Green.x = defaultChrom.Green.x;
                calibCustom.Green.y = defaultChrom.Green.y;
                calibCustom.Green.Lv = defaultChrom.Green.Lv;
                calibCustom.Blue.x = defaultChrom.Blue.x;
                calibCustom.Blue.y = defaultChrom.Blue.y;
                calibCustom.Blue.Lv = defaultChrom.Blue.Lv;
                calibCustom.White.x = defaultChrom.White.x;
                calibCustom.White.y = defaultChrom.White.y;
                calibCustom.White.Lv = defaultChrom.White.Lv;

                gdCalibValueTable.IsEnabled = false;
                calibTabFirstSelect = false;
            }

            if (confirmChromType != Settings.Ins.ConfigChromType) // モデル変更時
            {
                rbCalibDefault.Checked -= rbCalibDefault_Checked;
                rbCalibDefault.IsChecked = true;
                rbCalibDefault.Checked += rbCalibDefault_Checked;

                // Set Default
                txbCalibRed_x.Text = defaultChrom.Red.x.ToString();
                txbCalibRed_y.Text = defaultChrom.Red.y.ToString();
                txbCalibRed_Y.Text = defaultChrom.Red.Lv.ToString();
                txbCalibGreen_x.Text = defaultChrom.Green.x.ToString();
                txbCalibGreen_y.Text = defaultChrom.Green.y.ToString();
                txbCalibGreen_Y.Text = defaultChrom.Green.Lv.ToString();
                txbCalibBlue_x.Text = defaultChrom.Blue.x.ToString();
                txbCalibBlue_y.Text = defaultChrom.Blue.y.ToString();
                txbCalibBlue_Y.Text = defaultChrom.Blue.Lv.ToString();
                txbCalibWhite_x.Text = defaultChrom.White.x.ToString();
                txbCalibWhite_y.Text = defaultChrom.White.y.ToString();
                txbCalibWhite_Y.Text = defaultChrom.White.Lv.ToString();

                // Set Custom
                calibCustom.Red.x = defaultChrom.Red.x;
                calibCustom.Red.y = defaultChrom.Red.y;
                calibCustom.Red.Lv = defaultChrom.Red.Lv;
                calibCustom.Green.x = defaultChrom.Green.x;
                calibCustom.Green.y = defaultChrom.Green.y;
                calibCustom.Green.Lv = defaultChrom.Green.Lv;
                calibCustom.Blue.x = defaultChrom.Blue.x;
                calibCustom.Blue.y = defaultChrom.Blue.y;
                calibCustom.Blue.Lv = defaultChrom.Blue.Lv;
                calibCustom.White.x = defaultChrom.White.x;
                calibCustom.White.y = defaultChrom.White.y;
                calibCustom.White.Lv = defaultChrom.White.Lv;

                gdCalibValueTable.IsEnabled = false;
                confirmChromType = Settings.Ins.ConfigChromType;
            }
        }

        private void rbCalibDefault_Checked(object sender, RoutedEventArgs e)
        {
            ChromCustom defaultChrom = new ChromCustom(Settings.Ins.ConfigChromType);

            // Store Custom
            try { calibCustom.Red.x = Convert.ToDouble(txbCalibRed_x.Text); }
            catch { calibCustom.Red.x = 0; }
            try { calibCustom.Red.y = Convert.ToDouble(txbCalibRed_y.Text); }
            catch { calibCustom.Red.y = 0; }
            try { calibCustom.Red.Lv = Convert.ToDouble(txbCalibRed_Y.Text); }
            catch { calibCustom.Red.Lv = 0; }

            try { calibCustom.Green.x = Convert.ToDouble(txbCalibGreen_x.Text); }
            catch { calibCustom.Green.x = 0; }
            try { calibCustom.Green.y = Convert.ToDouble(txbCalibGreen_y.Text); }
            catch { calibCustom.Green.y = 0; }
            try { calibCustom.Green.Lv = Convert.ToDouble(txbCalibGreen_Y.Text); }
            catch { calibCustom.Green.Lv = 0; }

            try { calibCustom.Blue.x = Convert.ToDouble(txbCalibBlue_x.Text); }
            catch { calibCustom.Blue.x = 0; }
            try { calibCustom.Blue.y = Convert.ToDouble(txbCalibBlue_y.Text); }
            catch { calibCustom.Blue.y = 0; }
            try { calibCustom.Blue.Lv = Convert.ToDouble(txbCalibBlue_Y.Text); }
            catch { calibCustom.Blue.Lv = 0; }

            try { calibCustom.White.x = Convert.ToDouble(txbCalibWhite_x.Text); }
            catch { calibCustom.White.x = 0; }
            try { calibCustom.White.y = Convert.ToDouble(txbCalibWhite_y.Text); }
            catch { calibCustom.White.y = 0; }
            try { calibCustom.White.Lv = Convert.ToDouble(txbCalibWhite_Y.Text); }
            catch { calibCustom.White.Lv = 0; }

            // Set Default
            txbCalibRed_x.Text = defaultChrom.Red.x.ToString();
            txbCalibRed_y.Text = defaultChrom.Red.y.ToString();
            txbCalibRed_Y.Text = defaultChrom.Red.Lv.ToString();
            txbCalibGreen_x.Text = defaultChrom.Green.x.ToString();
            txbCalibGreen_y.Text = defaultChrom.Green.y.ToString();
            txbCalibGreen_Y.Text = defaultChrom.Green.Lv.ToString();
            txbCalibBlue_x.Text = defaultChrom.Blue.x.ToString();
            txbCalibBlue_y.Text = defaultChrom.Blue.y.ToString();
            txbCalibBlue_Y.Text = defaultChrom.Blue.Lv.ToString();
            txbCalibWhite_x.Text = defaultChrom.White.x.ToString();
            txbCalibWhite_y.Text = defaultChrom.White.y.ToString();
            txbCalibWhite_Y.Text = defaultChrom.White.Lv.ToString();

            gdCalibValueTable.IsEnabled = false;

        }

        private void rbCalibCustom_Checked(object sender, RoutedEventArgs e)
        {
            // Set Custom
            txbCalibRed_x.Text = calibCustom.Red.x.ToString();
            txbCalibRed_y.Text = calibCustom.Red.y.ToString();
            txbCalibRed_Y.Text = calibCustom.Red.Lv.ToString();
            txbCalibGreen_x.Text = calibCustom.Green.x.ToString();
            txbCalibGreen_y.Text = calibCustom.Green.y.ToString();
            txbCalibGreen_Y.Text = calibCustom.Green.Lv.ToString();
            txbCalibBlue_x.Text = calibCustom.Blue.x.ToString();
            txbCalibBlue_y.Text = calibCustom.Blue.y.ToString();
            txbCalibBlue_Y.Text = calibCustom.Blue.Lv.ToString();
            txbCalibWhite_x.Text = calibCustom.White.x.ToString();
            txbCalibWhite_y.Text = calibCustom.White.y.ToString();
            txbCalibWhite_Y.Text = calibCustom.White.Lv.ToString();

            gdCalibValueTable.IsEnabled = true;
        }

        private async void btnCalibration_Click(object sender, RoutedEventArgs e)
        {
            bool status = false;
            var dispatcher = Application.Current.Dispatcher;
            string msg = "";
            string caption = "";

            tcMain.IsEnabled = false;
            actionButton(sender, "Calibration Start.");

            winProgress = new WindowProgress("Calibration Progress");
            winProgress.ShowMessage("Calibration Start.");
            winProgress.Show();

            // Analyzerのラジオボタンを確認
            if (rbRemoteOff.IsChecked == true)
            {
                winProgress.Close();

                msg = "Analyzer is not selected.";
                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);

                playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
                releaseButton(sender, "UF Adjust Done.");
                tcMain.IsEnabled = true;
                return;
            }

            m_lstUserSetting = null;

            try { status = await Task.Run(() => calibration()); }
            catch (Exception ex)
            { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

            if (status == true)
            {
                msg = "Calibration Complete!";
                caption = "Complete";
            }
            else
            {
                setNormalSettingCalib();
                setUserSettingCalib(m_lstUserSetting);
                m_lstUserSetting = null;

                msg = "Failed in Calibration.";
                caption = "Error";
            }

            winProgress.Close();

            playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
            WindowMessage winMessage = new WindowMessage(msg, caption);
            winMessage.ShowDialog();

            releaseButton(sender, "Calibration Done.");
            tcMain.IsEnabled = true;
        }

        private void UnitCalibToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            // Unitを一つしか選択できないようにする処理
            clearSelectAllCalib();

            ((UnitToggleButton)sender).Checked -= UnitCalibToggleButton_Checked;
            ((UnitToggleButton)sender).IsChecked = true;
            ((UnitToggleButton)sender).Checked += UnitCalibToggleButton_Checked;
        }

        #endregion Events

        #region Private Methods

        private void clearSelectAllCalib()
        {
            for (int i = 0; i < allocInfo.MaxX; i++)
            {
                for (int j = 0; j < allocInfo.MaxY; j++)
                {
                    if (aryUnitCalib[i, j].UnitInfo != null)
                    { Dispatcher.Invoke(new Action(() => { aryUnitCalib[i, j].IsChecked = false; })); }
                }
            }
        }
        
        private bool calibration()
        {
            if (Settings.Ins.ExecLog == true)
            {
                SaveExecLog("");
                SaveExecLog("[0] calibration start");
            }

            var dispatcher = Application.Current.Dispatcher;

            List<UnitInfo> lstTargetUnit = new List<UnitInfo>();
            List<UserSetting> lstUserSetting;
            bool status;

            // ●進捗の最大値を設定
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[1] Set Step Count."); }
            winProgress.ShowMessage("Set Step Count.");

            int step = 13 + 1 * dicController.Count;
            winProgress.SetWholeSteps(step);

            // ●調整をするUnitをListに格納 [2]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[2] Store Target Cabinet Info."); }
            winProgress.ShowMessage("Store Target Cabinet Info.");

            dispatcher.Invoke(() => correctTargetUnit(out lstTargetUnit, allocInfo.MaxX, allocInfo.MaxY));
            if (lstTargetUnit.Count == 0)
            {
                ShowMessageWindow("Please select the target cabinet.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

#if NO_SET
#else
            // ●Power OnでないときはOnにする [3]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[3] Check Controller Power."); }
            winProgress.ShowMessage("Check Controller Power.");

            if (!setAllControllerPowerOn())
            { return false; }
            winProgress.PutForward1Step();
#endif

            CompositeConfigData compositeConfigData = null;
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
            {
                // ●調整データのバックアップがあるか確認 [*]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*] Check Backup Data."); }
                winProgress.ShowMessage("Check Backup Data.");
                string file = makeFilePath(lstTargetUnit[0], FileDirectory.Backup_Latest, DataType.HcData);
                if (System.IO.File.Exists(file))
                {
                    int upper4Bits = checkCrosstalkValidDataIndicator(file, out compositeConfigData);
                    if (upper4Bits == 2)
                    {
                        // Do nothing
                    }
                    else if (upper4Bits == 1)
                    {
                        string msg = "Crosstalk correction has already been performed in CAS.\r\nDo you want to continue calibration?";
                        showMessageWindow(out bool? result, msg, "Confirm", System.Drawing.SystemIcons.Question, 300, 180, "Continue", "Cancel");
                        if (result == false)
                        { return false; }
                    }
                    else
                    {
                        string msg = "Calibration cannot continue because crosstalk correction has not been performed at the factory.";
                        ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                        return false;
                    }
                }
                else
                {
                    ShowMessageWindow("There is not the hc.bin file.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }
            }

            // ●User設定保存 [4]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[4] Store User Settings."); }
            winProgress.ShowMessage("Store User Settings.");

            try
            {
                if (getUserSettingCalib(out lstUserSetting) != true)
                { return false; }
                m_lstUserSetting = lstUserSetting;
            }
            catch (Exception ex)
            {
                SaveErrorLog("[getUserSettingCalib] Source : " + ex.Source + "\r\nException Message : " + ex.Message);
                ShowMessageWindow(ex.Message, "Exception! (getUserSettingCalib())", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }
            winProgress.PutForward1Step();

            // ●調整用設定 [5]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[5] Set Calibration Settings."); }
            winProgress.ShowMessage("Set Calibration Settings.");

            setCalibrationSetting();
            winProgress.PutForward1Step();

            foreach (ControllerInfo controller in dicController.Values)
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*1] (Controller Loop) ControllerID : " + controller.ControllerID.ToString()); }

#if NO_SET
#else
                // ●Unit Power On [*1]
                winProgress.ShowMessage("Cabinet Power On.");
                sendSdcpCommand(SDCPClass.CmdUnitPowerOn, controller.IPAddress);
                winProgress.PutForward1Step();
#endif
            }

#if NO_MEASURE
#else
            // ●Open CA-410 [6]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[6] Open CA-410."); }
            winProgress.ShowMessage("Open CA-410.");

            status = openSubDispatch(1, Settings.Ins.NoOfProbes, Settings.Ins.PortVal);
            if (status != true)
            {
                ShowMessageWindow("Can not open CA-410.\r\nPlease check it status.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●CA-410設定 [7]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[7] Set CA-410 Settings."); }
            winProgress.ShowMessage("Set CA-410 Settings.");

            if (setCA410SettingDispatchCalib() != true)
            {
                ShowMessageWindow("Can not set CA-410 settings.\r\nPlease check its status.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();
#endif

            // ●Layout情報Off [8]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[8] Layout Info turn off."); }
            winProgress.ShowMessage("Layout Info turn off.");

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdDispUnitAddrOff, 0, cont.IPAddress); }
            winProgress.PutForward1Step();

            // ●基準色度取得 [9]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[9] Correct Target Color."); }
            winProgress.ShowMessage("Correct Target Color.");

            ChromCustom color = null;
            if (dispatcher.Invoke(() => correctTargetColor(out color)) != true)
            {
                MainWindow.ShowMessageWindow("Contains invalid value.\r\nPlease check again.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●クロストーク補正量取得 [*]
            CrossTalkCorrectionHighColor ctc = null;
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*] Get crosstalk."); }
                winProgress.ShowMessage("Get crosstalk..");

                if (!getCrosstalk(compositeConfigData, out ctc))
                {
                    ShowMessageWindow("Failed to get crosstalk.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }
            }

            // ●校正実行 [10]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[10] Execute Calibration."); }
            winProgress.ShowMessage("Execute Calibration.");

            if (dispatcher.Invoke(() => calibrateCA410(lstTargetUnit[0], color, ctc)) != true)
            {
                dispatcher.Invoke(() => resetLvxyCalModeSub());
                ShowMessageWindow("Can not calibrate CA-410.\r\nPlease check its status.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●内部信号を停止 [11]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[11] Int Pattern Off."); }
            winProgress.ShowMessage("Int Pattern Off.");

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdIntSignalOff, 0, cont.IPAddress); }
            winProgress.PutForward1Step();

            // ●User設定に戻す [12]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[12] Restore User Settings."); }
            winProgress.ShowMessage("Restore User Settings.");

            setNormalSettingCalib();

            setUserSettingCalib(m_lstUserSetting);
            m_lstUserSetting = null;    //User設定を戻したので呼び出し側の設定処理をスキップ

            winProgress.PutForward1Step();

            // ●UF/MeasureのChannelも同じにする [13]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[13] Set CA-410 Channel."); }
            winProgress.ShowMessage("Set CA-410 Channel.");

            dispatcher.Invoke(() => setCA410Channel());
            winProgress.PutForward1Step();

            return true;
        }

        private void correctTargetUnit(out List<UnitInfo> lstUnitList, int MaxX, int MaxY)
        {
            string error;
            List<UnitInfo> lstUnitTemp = new List<UnitInfo>();

            lstUnitList = new List<UnitInfo>();

            for (int x = 0; x < MaxX; x++)
            {
                for (int y = 0; y < MaxY; y++)
                {
                    try
                    {
                        if (aryUnitCalib[x, y].UnitInfo != null && aryUnitCalib[x, y].IsChecked == true)
                        { lstUnitList.Add(aryUnitCalib[x, y].UnitInfo); }
                    }
                    catch
                    {
                        error = "[correctTargetCabinet] (Case:1) x = " + x.ToString() + ", y = " + y.ToString()
                            + ", MaxX = " + MaxX.ToString() + ", MaxY = " + MaxY.ToString();
                        SaveErrorLog(error);
                        saveSelectionInfo();
                    }
                }
            }
        }

        private bool getUserSettingCalib(out List<UserSetting> lstUserSetting)
        {
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[0] getUserSettingCalib start ( Controller Count : " + dicController.Count.ToString() + " )"); }

            string buff;
            lstUserSetting = new List<UserSetting>();

            foreach (ControllerInfo controller in dicController.Values)
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[*1] (Controller Loop) ControllerID : " + controller.ControllerID.ToString()); }

                UserSetting user = new UserSetting();

                user.ControllerID = controller.ControllerID;

                // Low Brightness Mode
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[*2] Low Brightness Mode"); }

                System.Threading.Thread.Sleep(1000);
                if (sendSdcpCommand(SDCPClass.CmdLowBrightModeGet, out buff, controller.IPAddress) != true)
                { return false; }
                try { user.LowBrightnessMode = Convert.ToInt32(buff, 16); }
                catch (Exception ex)
                {
                    string errStr = "[getUserSetting(Low Brightness Mode)] Source : " + ex.Source + "\r\nException Messsage : " + ex.Message + "\r\nBuff = " + buff.ToString();
                    SaveErrorLog(errStr);
                    ShowMessageWindow(errStr, "Exception! (Low Brightness Mode)", System.Drawing.SystemIcons.Error, 500, 210);
                    return false;
                }

                // Temp Corection
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[*3] Temp Corection"); }

                System.Threading.Thread.Sleep(1000);
                if (sendSdcpCommand(SDCPClass.CmdTempCorrectGet, out buff, controller.IPAddress) != true)
                { return false; }
                try { user.TempCorrection = Convert.ToInt32(buff, 16); }
                catch (Exception ex)
                {
                    string errStr = "[getUserSetting(Temp Corection)] Source : " + ex.Source + "\r\nException Messsage : " + ex.Message + "\r\nBuff = " + buff.ToString();
                    SaveErrorLog(errStr);
                    ShowMessageWindow(errStr, "Exception! (Temp Corection)", System.Drawing.SystemIcons.Error, 500, 210);
                    return false;
                }

                // Burn-In Corection
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[*4] Burn-In Corection"); }

                System.Threading.Thread.Sleep(1000);
                if (sendSdcpCommand(SDCPClass.CmdBurnInCorrectGet, out buff, controller.IPAddress) != true)
                { return false; }
                try { user.BurnInCorrection = Convert.ToInt32(buff, 16); }
                catch (Exception ex)
                {
                    string errStr = "[getUserSetting(Burn-In Corection)] Source : " + ex.Source + "\r\nException Messsage : " + ex.Message + "\r\nBuff = " + buff.ToString();
                    SaveErrorLog(errStr);
                    ShowMessageWindow(errStr, "Exception! (Burn-In Corection)", System.Drawing.SystemIcons.Error, 500, 210);
                    return false;
                }

                // Signal Mode 2
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[*5] Signal Mode (2)"); }

                if (sendSdcpCommand(SDCPClass.CmdSigModeTwoGet, out buff, controller.IPAddress) != true)
                { return false; }
                try { user.SignalModeTwo = Convert.ToInt32(buff, 16); }
                catch (Exception ex)
                {
                    string errStr = "[getUserSetting(Signal Mode (2))] Source : " + ex.Source + "\r\nException Messsage : " + ex.Message + "\r\nBuff = " + buff.ToString();
                    SaveErrorLog(errStr);
                    ShowMessageWindow(errStr, "Exception! (Signal Mode (2))", System.Drawing.SystemIcons.Error, 500, 210);
                    return false;
                }

                try { lstUserSetting.Add(user); }
                catch (Exception ex)
                {
                    string errStr = "[getUserSetting(List Add)] Source : " + ex.Source + "\r\nException Messsage : " + ex.Message;
                    SaveErrorLog(errStr);
                    ShowMessageWindow(errStr, "Exception! (List Add)", System.Drawing.SystemIcons.Error, 500, 210);

                    errStr = "";
                    foreach (UserSetting setting in lstUserSetting)
                    { errStr += setting.ToString() + "\r\n"; }
                    SaveErrorLog(errStr);
                    return false;
                }
            }

            return true;
        }

        private bool setCalibrationSetting()
        {
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[0] setCalibSetting start ( Controller Count : " + dicController.Count.ToString() + " )"); }

            foreach (ControllerInfo cont in dicController.Values)
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[*1] (Controller Loop) ControllerID : " + cont.ControllerID.ToString()); }

                // Through Mode
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[*2] Through Mode"); }

                System.Threading.Thread.Sleep(1000);
                if (sendSdcpCommand(SDCPClass.CmdThroughModeOn, cont.IPAddress) != true)
                { return false; }

                // Low Brightness Mode
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[*3] Low Brightness Mode"); }

                System.Threading.Thread.Sleep(1000);
                if (sendSdcpCommand(SDCPClass.CmdLowBrightModeSet, cont.IPAddress) != true)
                { return false; }

                // Temp Corection
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[*4] Temp Corection"); }

                System.Threading.Thread.Sleep(1000);
                if (sendSdcpCommand(SDCPClass.CmdTempCorrectSet, cont.IPAddress) != true)
                { return false; }

                // Burn-In Correction
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[*5] Burn-In Correction"); }

                System.Threading.Thread.Sleep(1000);
                if (sendSdcpCommand(SDCPClass.CmdBurnInCorrectSet, cont.IPAddress) != true)
                { return false; }

                // Signal Mode 2
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[*6] Signal Mode (2)"); }

                int param = m_lstUserSetting.Find(x => x.ControllerID == cont.ControllerID).SignalModeTwo;
                if (checkSignalModeTwoVacStatus(param))
                {
                    param &= ~0x08; // VAC bitをoff
                    Byte[] cmd = new byte[SDCPClass.CmdSigModeTwoSet.Length];
                    Array.Copy(SDCPClass.CmdSigModeTwoSet, cmd, SDCPClass.CmdSigModeTwoSet.Length);

                    cmd[20] = (byte)param;
                    if (!sendSdcpCommand(cmd, cont.IPAddress))
                    { return false; }
                }
            }

            return true;
        }

        private void setUserSettingCalib(List<UserSetting> lstUserSetting)
        {
            if (lstUserSetting == null)
            { return; }

            foreach (UserSetting usr in lstUserSetting)
            {
                // Low Brightness Mode
                Byte[] cmd = new byte[SDCPClass.CmdLowBrightModeSet.Length];
                Array.Copy(SDCPClass.CmdLowBrightModeSet, cmd, SDCPClass.CmdLowBrightModeSet.Length);

                cmd[20] = (byte)usr.LowBrightnessMode;

                System.Threading.Thread.Sleep(1000);
                sendSdcpCommand(cmd, dicController[usr.ControllerID].IPAddress);

                // Temp Corection
                cmd = new byte[SDCPClass.CmdTempCorrectSet.Length];
                Array.Copy(SDCPClass.CmdTempCorrectSet, cmd, SDCPClass.CmdTempCorrectSet.Length);

                cmd[20] = (byte)usr.TempCorrection;

                System.Threading.Thread.Sleep(1000);
                sendSdcpCommand(cmd, dicController[usr.ControllerID].IPAddress);

                // Burn-In Corection
                cmd = new byte[SDCPClass.CmdBurnInCorrectSet.Length];
                Array.Copy(SDCPClass.CmdBurnInCorrectSet, cmd, SDCPClass.CmdBurnInCorrectSet.Length);

                cmd[20] = (byte)usr.BurnInCorrection;

                System.Threading.Thread.Sleep(1000);
                sendSdcpCommand(cmd, dicController[usr.ControllerID].IPAddress);

                // Signal Mode 2
                cmd = new byte[SDCPClass.CmdSigModeTwoSet.Length];
                Array.Copy(SDCPClass.CmdSigModeTwoSet, cmd, SDCPClass.CmdSigModeTwoSet.Length);

                cmd[20] = (byte)usr.SignalModeTwo;
                sendSdcpCommand(cmd, dicController[usr.ControllerID].IPAddress);
            }
        }
                
        private bool setCA410SettingDispatchCalib()
        {
            var dispatcher = Application.Current.Dispatcher;
            return dispatcher.Invoke(() => setCA410SettingCalib());
        }

        private bool setCA410SettingCalib() // CA-410の設定
        {
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[0] setCA410Setting start"); }

            // Channel
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[1] setChannelSub()"); }

            if (setChannelSub(cmbxCA410ChannelCalib.SelectedIndex + 1) != true)
            { return false; }

            // Sync Mode
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[2] setSyncModeSub()"); }

            if (setSyncModeSub((float)CaSdk.SyncMode.SyncUNIV) != true)
            { return false; }

            // Display Mode
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[3] setDisplayModeSub()"); }

            if (setDisplayModeSub(CaSdk.DisplayMode.DispModeLvxy) != true)
            { return false; }

            // Displya Digit
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[4] setDisplayDigitsSub()"); }

            if (setDisplayDigitsSub(CaSdk.DisplayDigits.DisplayDigit4) != true)
            { return false; }

            // Averaging Mode
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[5] setAveragingModeSub()"); }

            if (setAveragingModeSub(CaSdk.AveragingMode.AveragingFast) != true)
            { return false; }

            // Brightness Unit
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[6] setBrightnessUnitSub()"); }

            if (setBrightnessUnitSub(CaSdk.BrightnessUnit.BrightUnitcdm2) != true)
            { return false; }

            return true;
        }

        private bool correctTargetColor(out ChromCustom color)
        {
            color = new ChromCustom(ColorPurpose.Calib);

            // Store Value
            try
            {
                // Red
                color.Red.x = Convert.ToDouble(txbCalibRed_x.Text);
                color.Red.y = Convert.ToDouble(txbCalibRed_y.Text);
                color.Red.Lv = Convert.ToDouble(txbCalibRed_Y.Text);

                // Green
                color.Green.x = Convert.ToDouble(txbCalibGreen_x.Text);
                color.Green.y = Convert.ToDouble(txbCalibGreen_y.Text);
                color.Green.Lv = Convert.ToDouble(txbCalibGreen_Y.Text);

                // Blue
                color.Blue.x = Convert.ToDouble(txbCalibBlue_x.Text);
                color.Blue.y = Convert.ToDouble(txbCalibBlue_y.Text);
                color.Blue.Lv = Convert.ToDouble(txbCalibBlue_Y.Text);

                // White
                color.White.x = Convert.ToDouble(txbCalibWhite_x.Text);
                color.White.y = Convert.ToDouble(txbCalibWhite_y.Text);
                color.White.Lv = Convert.ToDouble(txbCalibWhite_Y.Text);

#if DEBUG
                Console.WriteLine($"StoreValue Red.Lv: {color.Red.Lv}, Green.Lv: {color.Green.Lv}, Blue.Lv: {color.Blue.Lv}");
#elif Release_log
                MainWindow.SaveExecLog($"      StoreValue Red.Lv: {color.Red.Lv}, Green.Lv: {color.Green.Lv}, Blue.Lv: {color.Blue.Lv}");
#endif

            }
            catch
            { return false; }

            return true;
        }

        private bool calibrateCA410(UnitInfo unit, ChromCustom color, CrossTalkCorrectionHighColor ctc)
        {
            bool status;
            double[] dummy;
            UserOperation operation = UserOperation.None;

            // dummyの測定を1回実施しないと、校正ができない。
            status = measureColor(null, out dummy, CaSdk.DisplayMode.DispModeXYZ, ref operation);
            if (status != true)
            { return false; }

            System.Threading.Thread.Sleep(500);

            // 校正実行modeにする
            status = setLvxyCalModeSub();
            if (status != true)
            { return false; }
            
            // 測定ポイントの十字を表示
            outputCross(unit, CrossPointPosition.Calib);

            // 測定画面表示
            status = Dispatcher.Invoke(() => showMeasureWindow());
            if (status != true)
            { return false; }

            CrossPointPosition crossPoint;
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            {
                crossPoint = CrossPointPosition.Calib;
            }
            else
            {
                crossPoint = CrossPointPosition.Module_5;
                color.Red.Lv *= 1 + ctc.Red;
                color.Green.Lv *= 1 + ctc.Green;
                color.Blue.Lv *= 1 + ctc.Blue;
            }

#if DEBUG
            Console.WriteLine($"SetValue Red.Lv: {color.Red.Lv}, Green.Lv: {color.Green.Lv}, Blue.Lv: {color.Blue.Lv}");
#elif Release_log
            MainWindow.SaveExecLog($"      SetValue Red.Lv: {color.Red.Lv}, Green.Lv: {color.Green.Lv}, Blue.Lv: {color.Blue.Lv}");
#endif

            // Red
            outputWindow(unit, crossPoint, CellColor.Red);
            System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait); // 画が表示されるまでWait

            status = measureSub();
            if (status != true)
            { return false; }

            System.Threading.Thread.Sleep(100);

            status = setLvxyCalDataSub(CaSdk.ColorNo.ColorRed, (float)color.Red.x, (float)color.Red.y, (float)color.Red.Lv);
            if (status != true)
            { return false; }

            playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

            // Green
            outputWindow(unit, crossPoint, CellColor.Green);
            System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait); // 画が表示されるまでWait

            status = measureSub();
            if (status != true)
            { return false; }
            
            System.Threading.Thread.Sleep(100);

            status = setLvxyCalDataSub(CaSdk.ColorNo.ColorGreen, (float)color.Green.x, (float)color.Green.y, (float)color.Green.Lv);
            if (status != true)
            { return false; }

            playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

            // Blue
            outputWindow(unit, crossPoint, CellColor.Blue);
            System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait); // 画が表示されるまでWait

            status = measureSub();
            if (status != true)
            { return false; }

            System.Threading.Thread.Sleep(100);

            status = setLvxyCalDataSub(CaSdk.ColorNo.ColorBlue, (float)color.Blue.x, (float)color.Blue.y, (float)color.Blue.Lv);
            if (status != true)
            { return false; }

            playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

            // White
            outputWindow(unit, crossPoint, CellColor.White);
            System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait); // 画が表示されるまでWait

            status = measureSub();
            if (status != true)
            { return false; }

            System.Threading.Thread.Sleep(100);

            status = setLvxyCalDataSub(CaSdk.ColorNo.ColorWhite, (float)color.White.x, (float)color.White.y, (float)color.White.Lv);
            if (status != true)
            { return false; }

            // 校正結果の書き込み
            status = enterSub();
            if (status != true)
            { return false; }

            playSound(applicationPath + "\\Components\\Sound\\button01b.mp3");

            return true;
        }

        private void setCA410Channel()
        {
            changCAChannel(cmbxCA410ChannelCalib.SelectedIndex + 1);

            cmbxCA410Channel.SelectedIndex = cmbxCA410ChannelCalib.SelectedIndex + 1;
            cmbxCA410ChannelMeas.SelectedIndex = cmbxCA410ChannelCalib.SelectedIndex + 1;
        }

        private void setNormalSettingCalib()
        {
            foreach (ControllerInfo controller in dicController.Values)
            {
                // Through Mode
                sendSdcpCommand(SDCPClass.CmdThroughModeOff, dicController[controller.ControllerID].IPAddress);
            }
        }

        /// <summary>
        /// クロストークのValid Data Indicatorの値を判定
        /// </summary>
        /// <param name="file">Latestフォルダー中の対象Cabinetのhc.bin</param>
        /// <param name="data">Module No.6のComposite Config Dataを格納</param>
        /// <returns></returns>
        private int checkCrosstalkValidDataIndicator(string file, out CompositeConfigData data)
        {
            data = new CompositeConfigData();
            int upper4Bits = -1;
            try
            {
                byte[] tempBytes = new byte[4];

                List<CompositeConfigData> lstConfigData = CompositeConfigData.getCompositeConfigDataList(file, dt_ColorCorrectionWrite);
                for (int i = 0; i < lstConfigData.Count; i++)
                {
                    if ((int)lstConfigData[i].header.Option1 == NO_6)
                    {
                        Array.Copy(lstConfigData[i].data, HcCcDataCtcDataValidIndicatorOffset, tempBytes, 0, tempBytes.Length);
                        int vdi = BitConverter.ToInt32(tempBytes, 0);
                        upper4Bits = vdi >> 4;
                        if (upper4Bits == 2 || upper4Bits == 1)
                        {
                            data = lstConfigData[i];
                        }

                        break;
                    }
                }
            }
            catch
            { return -1; }

            return upper4Bits;
        }

        /// <summary>
        /// 高諧調Crosstalk補正量R/G/Bを取得
        /// </summary>
        /// <param name="compositeConfigData"></param>
        /// <param name="ctc"></param>
        /// <returns></returns>
        private bool getCrosstalk(CompositeConfigData compositeConfigData, out CrossTalkCorrectionHighColor ctc)
        {
            ctc = new CrossTalkCorrectionHighColor();

            try
            {
                byte[] tempBytes = new byte[4];

                Array.Copy(compositeConfigData.data, HcCcDataCtcHighRedOffset, tempBytes, 0, tempBytes.Length);
                ctc.Red = BitConverter.ToSingle(tempBytes, 0);

                Array.Copy(compositeConfigData.data, HcCcDataCtcHighGreenOffset, tempBytes, 0, tempBytes.Length);
                ctc.Green = BitConverter.ToSingle(tempBytes, 0);

                Array.Copy(compositeConfigData.data, HcCcDataCtcHighBlueOffset, tempBytes, 0, tempBytes.Length);
                ctc.Blue = BitConverter.ToSingle(tempBytes, 0);

#if DEBUG
                Console.WriteLine($"Crosstalk RY: {ctc.Red}");
                Console.WriteLine($"Crosstalk GY: {ctc.Green}");
                Console.WriteLine($"Crosstalk BY: {ctc.Blue}");
#elif Release_log
                MainWindow.SaveExecLog($"      Crosstalk RY: {ctc.Red}");
                MainWindow.SaveExecLog($"      Crosstalk GY: {ctc.Green}");
                MainWindow.SaveExecLog($"      Crosstalk BY: {ctc.Blue}");
#endif

            }
            catch
            { return false; }

            return true;
        }

        #endregion Private Methods
    }
}
