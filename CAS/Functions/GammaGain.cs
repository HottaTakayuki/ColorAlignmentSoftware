using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SONY.Modules;

namespace CAS
{
    using CabinetOffset = System.Drawing.Point;

    public partial class MainWindow : Window
    {
        #region Fields

        private const int INT_ZERO = 0;
        private const int INT_ONE  = 1;
        private const int INT_FF = 255;

        private const int SIG_MODE_ONE_DEFAULT_DATA = 0x3F;
        private const int SIG_MODE_ONE_ADJUST_DATA = 0x39;
        private const int SIG_MODE_TWO_DEFAULT_DATA = 0x17;
        private const int SIG_MODE_TWO_ADJUST_DATA = 0x07;

        private const double COR_GAIN_MIN_VALUE = -12.5;
        private const double COR_GAIN_MAX_VALUE = 12.4;

        private const int REGULAR = 0;
        private const int HIGH = 1;
        private const int RED = 0;
        private const int GREEN = 1;
        private const int BLUE = 2;
        private const int WHITE = 3;

        private const int XYZ_X = 0;
        private const int XYZ_Y = 1;
        private const int XYZ_Z = 2;
        //private const int XYLV_X = 3;
        //private const int XYLV_Y = 4;
        //private const int XYLV_LV = 5;

        private List<byte[]> lstLowCorGain0, lstHightCorGain1, lstHightCorGain2, lstHightCorGain3;
        private float[][] CrosstalkCorrections;
        private GammaGain GammaGain;
        private CrossTalk CrossTalk;

        private byte[] _gammaGainThreshold = new byte[60]; // TH0~TH4

        private List<int> lstAdjustedModule;

        #endregion Fields

        #region Events

        private void rbGgAllModule_Checked(object sender, RoutedEventArgs e)
        {
            if (gdGgCellFrame != null)
            { gdGgCellFrame.IsEnabled = false; }

            if (rbGgAllModule != null && rbGgAllModule.IsChecked == true)
            { btnGgDeselectAllUnit_Click(null, null); }

            DiselectGgAllCells();

            gdCellPosition.IsEnabled = false;
            gdDataWritingOption.IsEnabled = true;
            btnGgSelectAllUnit.IsEnabled = true;
        }

        private void rbGgEachModule_Checked(object sender, RoutedEventArgs e)
        {
            if (gdGgCellFrame != null)
            { gdGgCellFrame.IsEnabled = true; }

            if (rbGgEachModule != null && rbGgEachModule.IsChecked == true)
            { btnGgDeselectAllUnit_Click(null, null); }

            DiselectGgAllCells();

            gdCellPosition.IsEnabled = true;
            gdDataWritingOption.IsEnabled = false;
            btnGgSelectAllUnit.IsEnabled = false;
        }

        private void btnCellToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            DiselectGgAllCells();

            ((UnitToggleButton)sender).Checked -= btnCellToggleButton_Checked;
            ((UnitToggleButton)sender).IsChecked = true;
            ((UnitToggleButton)sender).Checked += btnCellToggleButton_Checked;
        }

        private void btnGgSelectAllUnit_Click(object sender, RoutedEventArgs s)
        {
            if (sender != null)
            { actionButton(sender, "Select All"); }

            for (int i = 0; i < allocInfo.MaxX; i++)
            {
                for (int j = 0; j < allocInfo.MaxY; j++)
                {
                    if (aryUnitGg[i, j].UnitInfo != null)
                    { Dispatcher.Invoke(new Action(() => { aryUnitGg[i, j].IsChecked = true; })); }
                }
            }

            if (sender != null)
            {
                System.Threading.Thread.Sleep(600);
                releaseButton(sender);
            }
        }

        private void btnGgDeselectAllUnit_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            { actionButton(sender, "Deselect All"); }

            for (int i = 0; i < allocInfo.MaxX; i++)
            {
                for (int j = 0; j < allocInfo.MaxY; j++)
                {
                    if (aryUnitGg[i, j].UnitInfo != null)
                    { Dispatcher.Invoke(new Action(() => { aryUnitGg[i, j].IsChecked = false; })); }
                }
            }

            if (sender != null)
            {
                System.Threading.Thread.Sleep(600);
                releaseButton(sender);
            }
        }

        private void rbRemoteOffGain_Checked(object sender, RoutedEventArgs e)
        {
            tcMain.IsEnabled = false;
            actionButton(sender, "Close " + analyzer.DisplayName() + ".");

            // CA-410 Close
            if (caSdk != null)
            {
                if (caSdk.isOpened() == true)
                { setRemoteModeSub(CaSdk.RemoteMode.RemoteOff); }

                closeSub();
            }

            releaseButton(sender, "Close " + analyzer.DisplayName() + " Done.");

            analyzer = ColorAnalyzerModel.NA;
            tcMain.IsEnabled = true;
        }

        private void cmbxChannelGain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // CA-410 Channel
            changCAChannel(cmbxCA410ChannelGain.SelectedIndex);

            cmbxCA410Channel.SelectedIndex = cmbxCA410ChannelGain.SelectedIndex;
            cmbxCA410ChannelMeas.SelectedIndex = cmbxCA410ChannelGain.SelectedIndex;
            //cmbxCA410ChannelCalib.SelectedIndex = cmbxCA410ChannelGain.SelectedIndex;
            cmbxCA410ChannelRelTgt.SelectedIndex = cmbxCA410ChannelGain.SelectedIndex;
        }

        private void cmbxGgPattern_DropDownClosed(object sender, EventArgs e)
        {
            if (((ComboBox)sender).SelectedIndex == 0)
            {
                cmbxPattern.SelectedIndex = (int)TestPattern.Raster;
                btnIntSigOn_Click(sender, null);
            }
            else if (((ComboBox)sender).SelectedIndex == 1)
            {
                cmbxPattern.SelectedIndex = (int)TestPattern.Unit;
                btnIntSigOn_Click(sender, null);
            }
            else if (((ComboBox)sender).SelectedIndex == 2)
            {
                cmbxPattern.SelectedIndex = (int)TestPattern.Cell;
                btnIntSigOn_Click(sender, null);
            }
            else if (((ComboBox)sender).SelectedIndex == 3)
            {
                btnIntSigOff_Click(sender, null);
            }
        }

        private void utbGg_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            rectAreaGg.Visibility = System.Windows.Visibility.Visible;
            rectAreaGg.Margin = new Thickness(e.GetPosition(this).X - 2, e.GetPosition(this).Y - 48, 0, 0);

            startPos = new Point(rectAreaGg.Margin.Left, rectAreaGg.Margin.Top);
        }

        private void utbGg_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            endPos = new Point(e.GetPosition(this).X - 2, e.GetPosition(this).Y - 48);

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

            double offsetX = tcMain.Margin.Left + gdGammaGain.Margin.Left + gdAllocGg.Margin.Left + gdAllocGgLayout.Margin.Left + cabinetAllocColumnHeaderWidth - svGammaGain.HorizontalOffset;
            double offsetY = tcMain.Margin.Top + gdGammaGain.Margin.Top + gdAllocGg.Margin.Top + gdAllocGgLayout.Margin.Top + cabinetAllocRowHeaderHeight - svGammaGain.VerticalOffset;

            for (int y = 0; y < allocInfo.MaxY; y++)
            {
                for (int x = 0; x < allocInfo.MaxX; x++)
                {
                    if (aryUnitGg[x, y].IsEnabled == false)
                    { continue; }

                    if (isContainedDragArea(aryUnitGg[x, y], area, offsetX, offsetY) == true)
                    {
                        if (aryUnitGg[x, y].IsChecked == true)
                        { aryUnitGg[x, y].IsChecked = false; }
                        else
                        { aryUnitGg[x, y].IsChecked = true; }
                    }
                }
            }

            rectAreaGg.Visibility = System.Windows.Visibility.Hidden;
        }

        private void utbGg_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            double height = e.GetPosition(this).Y - 48 - startPos.Y;
            try
            {
                if (height >= 0)
                { rectAreaGg.Height = height; }
                else
                {
                    rectAreaGg.Margin = new Thickness(rectAreaGg.Margin.Left, startPos.Y + height, 0, 0);
                    rectAreaGg.Height = Math.Abs(height);
                }
            }
            catch { rectAreaGg.Height = 0; }

            double width = e.GetPosition(this).X - 2 - startPos.X;
            try
            {
                if (width >= 0)
                { rectAreaGg.Width = width; }
                else
                {
                    rectAreaGg.Margin = new Thickness(startPos.X + width, rectAreaGg.Margin.Top, 0, 0);
                    rectAreaGg.Width = Math.Abs(width);
                }
            }
            catch { rectAreaGg.Width = 0; }
        }

        private async void btnGgAdjustStart_Click(object sender, RoutedEventArgs e)
        {
            tcMain.IsEnabled = false;
            actionButton(sender, "Gamma Gain Adjust Start.");

            string msg, caption;
            // Analyzerのラジオボタンを確認
            if (rbRemoteOff.IsChecked == true)
            {
                msg = "Analyzer is not selected.";
                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);

                playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
                releaseButton(sender, "Gamma Gain Adjust Done.");
                tcMain.IsEnabled = true;
                return;
            }

            // MGAMパラメーター算出用初期化設定
            GammaGain = new GammaGain(allocInfo.LEDModel);

            // CrossTalkCorrection算出用初期化設定
            CrossTalk = new CrossTalk(allocInfo.LEDModel);

            winProgress = new WindowProgress("Adjust Progress");
            winProgress.Show();

            bool status;
            if (rbGgAllModule.IsChecked == true)
            {
                status = await Task.Run(() => adjustUnitGammaGain()); // All Modules/Cabinet
            }
            else
            {
                status = await Task.Run(() => adjustCellGammaGain()); // Each Module
            }

            // ●Cabinet Power On
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("Cabinet Power On."); }
            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdUnitPowerOn, SDCP_COMMAND_WAIT_TIME, cont.IPAddress); }
            System.Threading.Thread.Sleep(5000);
            // ●Raster表示
            outputRaster(brightness._20pc); // 20%

            if (status)
            {
                msg = "Gamma Gain adjustment complete !";
                caption = "Complete";
            }
            else
            {
                if (!(m_lstUserSetting == null))
                {
                    setNormalSetting();
                    setUserSetting(m_lstUserSetting);
                    m_lstUserSetting = null;
                    setFtpOff();
                    setDefaultSignalModeOne();
                }

                msg = "Failed in Gamma Gain Adjust.";
                caption = "Error";
            }

            winProgress.StopRemainTimer();
            winProgress.Close();

            playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");

            WindowMessage winMessage = new WindowMessage(msg, caption);
            winMessage.ShowDialog();

            releaseButton(sender, "Gamma Gain Adjust Done.");
            tcMain.IsEnabled = true;
        }

        #endregion Events

        #region Private Methods

        /// <summary>
        /// Adjustment Mode - All Modules/Cabinet
        /// </summary>
        /// <returns></returns>
        private bool adjustUnitGammaGain()
        {
            List<ControllerInfo> lstTargetController = new List<ControllerInfo>();
            List<UnitInfo> lstTargetUnit = new List<UnitInfo>();
            List<int> lstTargetCell = new List<int>();
            List<UnitInfo> lstDataOkTargetUnit = new List<UnitInfo>();

            if (Settings.Ins.ExecLog == true)
            {
                SaveExecLog("");
                SaveExecLog("[0] adjust All Modules/Cabinet start.");
            }

            // ●進捗の最大値を設定 [1]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[1] Set Step Count."); }
            winProgress.ShowMessage("Set Step Count.");

            int step = 29;
            winProgress.SetWholeSteps(step);
            winProgress.PutForward1Step();

            // ●Power OnでないときはOnにする [2]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[2] Check Controller Power."); }
            winProgress.ShowMessage("Check Controller Power.");

            if (!setAllControllerPowerOn())
            { return false; }
            winProgress.PutForward1Step();

            // ●調整をするCabinetをListに格納 [3]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[3] Store Target Cabinet(s) Info."); }
            winProgress.ShowMessage("Store Target Cabinet(s) Info.");

            Dispatcher.Invoke(() => storeSelectedUnit(out lstTargetUnit, allocInfo.MaxX, allocInfo.MaxY));
            if (lstTargetUnit.Count == INT_ZERO)
            {
                ShowMessageWindow("Please select cabinet(s).", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●調整をするModuleをListに格納 [4]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[4] Store Target Module(s) Info."); }
            winProgress.ShowMessage("Store Target Module(s) Info.");

            lstTargetCell = Enumerable.Range(0, 12).ToList();
            winProgress.PutForward1Step();

            // ●調整データのバックアップファイルがあるか確認 [5]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[5] Check Backup Data."); }
            winProgress.ShowMessage("Check Backup Data.");

            if (!checkLcFileExists(lstTargetUnit, out lstDataOkTargetUnit, out string _msg))
            {
                if (lstDataOkTargetUnit.Count == INT_ZERO)
                {
                    _msg = $"The data file is missing or incorrect for following cabinets.\r\n\r\n{_msg}";
                    ShowMessageWindow(_msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }

                string _msg2 = $"The data file is missing or incorrect for following cabinets.\r\nDo you want to continue with the adjustment?\r\n\r\n{_msg}";
                showMessageWindow(out bool? result, _msg2, "Confirm", System.Drawing.SystemIcons.Question, 300, 180, "Continue", "Cancel");
                if (result != true)
                {
                    _msg = $"The adjustment processing has been canceled.\r\nThe data file is missing or incorrect for the following cabinets.\r\n\r\n{_msg}";
                    ShowMessageWindow(_msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false; ;
                }
            }
            winProgress.PutForward1Step();

            // ●対象Cabinetが接続されているController情報を保持する [6]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[6] Store Target Controller(s) Info."); }
            winProgress.ShowMessage("Store Target Controller(s) Info.");

            // 初期化
            foreach (ControllerInfo controller in dicController.Values)
            { controller.Target = false; }

            foreach (UnitInfo unit in lstDataOkTargetUnit)
            {
                if (!lstTargetController.Contains(dicController[unit.ControllerID]))
                {
                    dicController[unit.ControllerID].Target = true;
                    lstTargetController.Add(dicController[unit.ControllerID]);
                }
            }
            winProgress.PutForward1Step();

            // ●Controllerごと[7]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[7] Get Controller(s) Info."); }
            winProgress.ShowMessage("Get Controller(s) Info.");

            foreach (ControllerInfo controller in lstTargetController)
            {
                // Controller ID [*1]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog($"\t[*1] (Controller) ControllerID : {controller.ControllerID}"); }
                winProgress.ShowMessage($"ControllerID : {controller.ControllerID}");

                // FTP ON [*2]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[*2] FTP On."); }
                winProgress.ShowMessage("FTP On.");

                if (!sendSdcpCommand(SDCPClass.CmdFtpOn, controller.IPAddress))
                {
                    ShowMessageWindow("Failed to Set FTP On.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }

                // Model名取得 [*3]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[*3] Get Model Name."); }
                winProgress.ShowMessage("Get Model Name.");

                if (string.IsNullOrWhiteSpace(controller.ModelName))
                { controller.ModelName = getModelName(controller.IPAddress); }

                if (Settings.Ins.ExecLog == true)
                { SaveExecLog($"       Model Name : {controller.ModelName}"); }

                // Serial取得 [*4]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[*4] Get Serial No."); }
                winProgress.ShowMessage("Get Serial No.");

                if (string.IsNullOrWhiteSpace(controller.SerialNo))
                { controller.SerialNo = getSerialNo(controller.IPAddress); }

                if (Settings.Ins.ExecLog == true)
                { SaveExecLog($"       Serial Num. : {controller.SerialNo}"); }

                // Tempフォルダ内のファイルを削除 [*5]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[*5] Delete Temporary Files."); }
                winProgress.ShowMessage("Delete Temporary Files.");

                string tempPath = $"{applicationPath}\\Backup\\{controller.ModelName}_{controller.SerialNo}_Temp";
                if (!System.IO.Directory.Exists(tempPath))
                { System.IO.Directory.CreateDirectory(tempPath); }
                else
                {
                    string[] files = System.IO.Directory.GetFiles(tempPath, "*", System.IO.SearchOption.AllDirectories);
                    for (int i = 0; i < files.Length; i++)
                    {
                        try { System.IO.File.Delete(files[i]); }
                        catch { return false; }
                    }
                }
            }
            winProgress.PutForward1Step();

            // ●Open CA-410 [8]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[8] Open CA-410."); }
            winProgress.ShowMessage("Open CA-410.");

            if (!openSubDispatch(1, Settings.Ins.NoOfProbes, Settings.Ins.PortVal))
            {
                ShowMessageWindow("Can not open CA-410.\r\nPlease check it status.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●CA-410設定 [9]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[9] Set CA-410 Settings."); }
            winProgress.ShowMessage("Set CA-410 Settings.");

            if (!setCA410SettingDispatchModuleGamma())
            {
                ShowMessageWindow("Can not set CA-410 settings.\r\nPlease check it status.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●User設定保存 [10]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[10] Store User Settings."); }
            winProgress.ShowMessage("Store User Settings.");

            try
            {
                if (getUserSetting(out List<UserSetting> lstUserSetting) != true)
                { return false; }
                m_lstUserSetting = lstUserSetting;
            }
            catch (Exception ex)
            {
                SaveErrorLog($"[getUserSetting] Source : {ex.Source}\r\nException Message : {ex.Message}");
                ShowMessageWindow(ex.Message, "Exception! (getUserSetting())", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }
            winProgress.PutForward1Step();

            // ●調整用設定 [11]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[11] Set Adjust Settings."); }
            winProgress.ShowMessage("Set Adjust Settings.");

            setAdjustSetting(false);
            winProgress.PutForward1Step();

            // ●Signal Mode設定 [12]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[12] Set Signal Mode Settings."); }
            winProgress.ShowMessage("Set Signal Mode Settings.");

            foreach (ControllerInfo controller in lstTargetController)
            {
                if (!setAdjustSigModeSettings(controller, true))
                {
                    ShowMessageWindow("Failed to set adjust signal mode.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }
            }
            winProgress.PutForward1Step();

            // ●Layout情報Off [13]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[13] Layout Off."); }
            winProgress.ShowMessage("Layout Off.");

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdDispUnitAddrOff, 0, cont.IPAddress); }
            winProgress.PutForward1Step();

            // ●色度測定[14]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[14] Measure Cabinet Color."); }
            winProgress.ShowMessage("Measure Cabinet Color.");

            for (int i = 0; i < lstDataOkTargetUnit.Count; i++)
            {
                // 初期化
                initMgamParameter();

                CrosstalkCorrections = Enumerable.Repeat(new float[3], 12).ToArray(); // crosstalkCorrections = [CellNo.][CTC R/G/B]
                for (int cell = 0; cell < 12; cell++)
                { CrosstalkCorrections[cell] = new float[3]; }

                UnitInfo unit = lstDataOkTargetUnit[i];

                if (Settings.Ins.ExecLog == true)
                { SaveExecLog($" [*1] (Cabinet) C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}"); }

                if (unit == null)
                { continue; }

                if (Settings.Ins.ExecLog == true)
                { SaveExecLog($" [*2] Make Threshold Parameter."); }
                winProgress.ShowMessage("Make Threshold Parameter.");

                // Threshold Levelパラメーター作成
                try { makeThresholdLevel(); }
                catch (Exception ex)
                {
                    SaveErrorLog($"[makeThresholdLevel] Source : {ex.Source}\r\nException Message : {ex.Message}");
                    ShowMessageWindow($"Failed in make threshold parameter.\r\n{ex.Message}", "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                    return false;
                }

                UserOperation operation = UserOperation.None;
                for (int idx = 0; idx < lstTargetCell.Count; idx++)
                {
                    if (Settings.Ins.ExecLog == true)
                    { SaveExecLog($" [*3] Measure Module No.{idx + 1} Color."); }
                    winProgress.ShowMessage($"Measure Module No.{idx + 1} Color.");

                    while (true)
                    {
                        // 色測定
                        bool status = measurementCell(unit, lstTargetCell[idx], true, out double[][] measureHighData, out double[][] measureLowData, out operation);
                        if (operation == UserOperation.Cancel)
                        { return false; }
                        else if (operation == UserOperation.Rewind)
                        { break; }
                        else // UserOperation.OK
                        {
                            // Do nothing
                        }

                        if (!status) // 測定結果NG
                        { return false; }

                        // MGAMパラメーター計算
                        if (Settings.Ins.ExecLog == true)
                        { SaveExecLog($" [*4] Calc Module No.{idx + 1} MGAM Parameter."); }
                        winProgress.ShowMessage($"Calc Module No.{idx + 1} MGAM Parameter.");

                        try
                        {
                            if (!calcMgamParameter(idx, measureHighData, measureLowData))
                            {
                                bool? result;
                                string msg = "Do you want to retry the measurement or finish ?";

                                showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 400, 180, "Retry", "Finish");

                                if (result == true)
                                { continue; }
                                else
                                { return false; }
                            }
                            else
                            { break; }
                        }
                        catch (Exception ex)
                        {
                            SaveErrorLog($"[calcMgamParameter] Source : {ex.Source}\r\nException Message : {ex.Message}");
                            ShowMessageWindow($"Failed in calc mgam parameter.\r\n{ex.Message}", "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                            return false;
                        }
                    }

                    if (operation == UserOperation.Rewind)
                    { break; }
                }

                if (operation == UserOperation.Rewind)
                {
                    i -= 1;
                    continue;
                }

                lstAdjustedModule = new List<int>(); // 初期化

                // LCファイルに書き出し
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog(" [*5] Save New Lc Data."); }
                winProgress.ShowMessage("Save New Lc Data.");

                try { overwriteMgamData(unit, lstTargetCell); }
                catch (Exception ex)
                {
                    SaveErrorLog($"[overwriteMgamData] Source : {ex.Source}\r\nException Message : {ex.Message}");
                    ShowMessageWindow($"Failed in overwrite mgam data.\r\n{ex.Message}", "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                    return false;
                }

                // HCファイルに書き出し
                if (Settings.Ins.GammaGain.GainMeasurementMode == GainMeasurementMode.Full)
                {
                    if (Settings.Ins.ExecLog == true)
                    { SaveExecLog(" [*6] Save New Hc Data."); }
                    winProgress.ShowMessage("Save New Hc Data.");

                    try { overwriteColorCorrectionData(unit, lstTargetCell); }
                    catch (Exception ex)
                    {
                        SaveErrorLog($"[overwriteColorCorrectionData] Source : {ex.Source}\r\nException Message : {ex.Message}");
                        ShowMessageWindow($"Failed in overwrite color correction data.\r\n{ex.Message}", "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                        return false;
                    }
                }
            }
            winProgress.PutForward1Step();

            // ●TestPattern OFF[15]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[15] TestPattern Off."); }
            winProgress.ShowMessage("TestPattern Off.");

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdIntSignalOff, 0, cont.IPAddress); }
            winProgress.PutForward1Step();

            // 推定処理時間
            int processSec = 0;
            int currentStep = 0;
            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));

            int dataMoveSec = calcAdjustGainMoveSec(lstDataOkTargetUnit.Count);
            int responseSec = calcAdjustGainResponseSec(lstDataOkTargetUnit.Count);
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.StartRemainTimer(processSec);

            // ●Move Files[16]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[16] Move Adjusted Files."); }
            winProgress.ShowMessage("Move Adjusted Files.");

            foreach (ControllerInfo controller in lstTargetController)
            {
                string tempPath = $"{applicationPath}\\Backup\\{controller.ModelName}_{controller.SerialNo}_Temp";
                string[] files = System.IO.Directory.GetFiles(tempPath, "*", System.IO.SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    if (!putFileFtpRetry(controller.IPAddress, files[i]))
                    {
                        ShowMessageWindow("Failed to upload file to ftp.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                        return false;
                    }
                }
            }
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            // ●Cabinet Power Off [17]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[17] Cabinet Power Off."); }
            winProgress.ShowMessage("Cabinet Power Off.");

            foreach (ControllerInfo controller in lstTargetController)
            { sendSdcpCommand(SDCPClass.CmdUnitPowerOff, controller.IPAddress); }
            System.Threading.Thread.Sleep(SLEEP_TIME_AFTER_PANEL_OFF);

            if (getUnitPowerStatus() != true)
            {
                string msg = "Failed to cabinet power off.";
                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            // ●Reconfig [18]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[18] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            sendReconfig();
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            // ●書き込みコマンド発行 [19]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[19] Send Write Command."); }
            winProgress.ShowMessage("Send Write Command.");

            foreach (ControllerInfo controller in lstTargetController)
            { sendSdcpCommand(SDCPClass.CmdDataWrite, controller.IPAddress); }
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            // ●書き込みComplete待ち [20]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[20] Waiting for the process of controller."); }
            winProgress.ShowMessage("Waiting for the process of controller.");

            try
            {
                bool[] complete = new bool[lstTargetController.Count];

                while (true)
                {
                    int id = 0;
                    foreach (ControllerInfo controller in lstTargetController)
                    {
                        complete[id] = checkCompleteFtp(controller.IPAddress, "write_complete");
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

                    //winProgress.ShowMessage("Waiting for the process of controller.");

                    System.Threading.Thread.Sleep(1000);
                }
            }
            catch
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("Send Reconfig."); }
                winProgress.ShowMessage("Send Reconfig.");
                sendReconfig(); 
                return false;
            }
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            // ●調整設定解除 [21]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[21] Set Normal Setting."); }
            winProgress.ShowMessage("Set Normal Setting.");

            setNormalSetting();
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            // ●User設定に戻す [22]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[22] Restore User Settings."); }
            winProgress.ShowMessage("Restore User Settings.");

            setUserSetting(m_lstUserSetting);
            m_lstUserSetting = null;    //User設定を戻したので呼び出し側の設定処理をスキップ
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            // ●Latest → Previousフォルダへコピー [23]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[23] Move Latest to Previous."); }
            winProgress.ShowMessage("[22] Move Latest to Previous.");

            foreach (ControllerInfo controller in lstTargetController)
            { copyLatest2Previous($"{applicationPath}\\Backup\\{controller.ModelName}_{controller.SerialNo}"); }
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            // ●Temp → Latestフォルダへコピー [24]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[24] Move Temp to Latest."); }
            winProgress.ShowMessage("[23] Move Temp to Latest.");

            foreach (ControllerInfo controller in lstTargetController)
            { copyTemp2Latest($"{applicationPath}\\Backup\\{controller.ModelName}_{controller.SerialNo}"); }
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            // ●Reconfig [25]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[25] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            sendReconfig();
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            // ●Tempフォルダ内のファイルを削除 [26]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[26] Delete Temporary Files."); }
            winProgress.ShowMessage("Delete Temporary Files.");

            foreach (ControllerInfo controller in lstTargetController)
            {
                string tempPath = $"{applicationPath}\\Backup\\{controller.ModelName}_{controller.SerialNo}_Temp";
                if (!System.IO.Directory.Exists(tempPath))
                { System.IO.Directory.CreateDirectory(tempPath); }
                else
                {
                    string[] files = System.IO.Directory.GetFiles(tempPath, "*", System.IO.SearchOption.AllDirectories);
                    for (int i = 0; i < files.Length; i++)
                    {
                        try { System.IO.File.Delete(files[i]); }
                        catch { return false; }
                    }
                }
            }
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            // ●選択を解除 [27]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[27] Unselect All Cabinets."); }
            winProgress.ShowMessage("Unselect All Cabinets.");

            btnGgDeselectAllUnit_Click(null, null);
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            // ●FTP Off [28]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[28] FTP Off."); }
            winProgress.ShowMessage("FTP Off.");

            if (!setFtpOff())
            {
                ShowMessageWindow("Failed to set ftp off.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            // ●Signal Mode戻し [29]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[29] Set Signal Mode One."); }
            winProgress.ShowMessage("Set Signal Mode One.");

            if (!setDefaultSignalModeOne())
            {
                ShowMessageWindow("Failed to set signal mode one.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // 問題ありCabinet/Module情報再表示
            if (!string.IsNullOrEmpty(_msg))
            {
                _msg = $"The follwing cabinets/modules were not adjusted.\r\n\r\n{_msg}";
                ShowMessageWindow(_msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
            }

            return true;
        }

        /// <summary>
        /// Adjustment Mode - Each Module
        /// </summary>
        /// <returns></returns>
        private bool adjustCellGammaGain()
        {
            List<ControllerInfo> lstTargetController = new List<ControllerInfo>();
            List<UnitInfo> lstTargetUnit = new List<UnitInfo>();
            List<UnitInfo> lstDataOkTargetUnit = new List<UnitInfo>();
            List<int> lstTargetCell = new List<int>();
            ControllerInfo targetController;
            UnitInfo targetUnit;
            UnitInfo referenceUnit;
            int referenceCell;
            int targetCell;
            string tempPath;
            string[] files;
            GainReferenceCellPosition position = GainReferenceCellPosition.Top;

            if (Settings.Ins.ExecLog == true)
            {
                SaveExecLog("");
                SaveExecLog("[0] adjust Each Module start.");
            }

            // ●進捗の最大値を設定 [1]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[1] Set Step Count."); }
            winProgress.ShowMessage("Set Step Count.");

            int step = 29;
            winProgress.SetWholeSteps(step);
            winProgress.PutForward1Step();

            // ●Power OnでないときはOnにする [2]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[2] Check Controller Power."); }
            winProgress.ShowMessage("Check Controller Power.");

            if (!setAllControllerPowerOn())
            { return false; }
            winProgress.PutForward1Step();

            // ●調整をするCabinetをListに格納 [3]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[3] Store Target Cabinet Info."); }
            winProgress.ShowMessage("Store Target Cabinet Info.");

            Dispatcher.Invoke(() => storeSelectedUnit(out lstTargetUnit, allocInfo.MaxX, allocInfo.MaxY));
            if (lstTargetUnit.Count == INT_ZERO)
            {
                ShowMessageWindow("Please select cabinet.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            else if (lstTargetUnit.Count > INT_ONE)
            {
                ShowMessageWindow("Plural cabinets are selected.\r\nPlease select only one cabinet.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            targetUnit = lstTargetUnit[0];
            winProgress.PutForward1Step();

            // ●調整をするModuleをListに格納 [4]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[4] Store Target Module Info."); }
            winProgress.ShowMessage("Store Target Module Info.");

            Dispatcher.Invoke(() => storeSelectedCell(out lstTargetCell));
            if (lstTargetCell.Count == INT_ZERO)
            {
                ShowMessageWindow("Please select module.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            targetCell = lstTargetCell[0];
            winProgress.PutForward1Step();

            // ●調整データのバックアップがすべてあるか確認 [5]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[5] Check Backup Data."); }
            winProgress.ShowMessage("Check Backup Data.");

            if (!checkLcFileExists(lstTargetUnit, out _, out string _msg))
            {
                _msg = $"The adjustment processing has been canceled.\r\nThe data file is missing or incorrect for the following cabinets.\r\n\r\n{_msg}";
                ShowMessageWindow(_msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●調整をするModuleのReference Cabinet/Moduleを格納 [6]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[6] Store Reference Module Info."); }
            winProgress.ShowMessage("Store Reference Module Info.");

            Dispatcher.Invoke(() => storeReferenceCellPosition(out position));
            if (!storeReferenceCell(targetUnit, targetCell, position, out referenceUnit, out referenceCell))
            {
                ShowMessageWindow("Reference adjacent module not exists.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●対象Cabinetが接続されているController情報を保持する [7]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[7] Store Target Controller Info."); }
            winProgress.ShowMessage("Store Target Controller Info.");

            // 初期化
            foreach (ControllerInfo controller in dicController.Values)
            { controller.Target = false; }

            targetController = dicController[targetUnit.ControllerID];
            dicController[targetUnit.ControllerID].Target = true;
            lstTargetController.Add(dicController[targetUnit.ControllerID]);

            // 対象キャビネットと参照キャビネットに繋がっているコントローラーが異なる場合
            if (targetUnit.ControllerID != referenceUnit.ControllerID)
            {
                dicController[referenceUnit.ControllerID].Target = true;
                lstTargetController.Add(dicController[referenceUnit.ControllerID]);
            }
            winProgress.PutForward1Step();

            // ●対象Controller[8]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[8] Get Controller Info."); }
            winProgress.ShowMessage("Get Controller Info.");

            // Controller ID [*1]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog($"\t[*1] (Controller) ControllerID : {targetController.ControllerID}"); }
            winProgress.ShowMessage($"ControllerID : {targetController.ControllerID}");

            // FTP ON [*2]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[*2] FTP On."); }
            winProgress.ShowMessage("FTP On.");

            if (!sendSdcpCommand(SDCPClass.CmdFtpOn, targetController.IPAddress))
            {
                ShowMessageWindow("Failed to Set FTP On.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }

            // Model名取得 [*3]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[*3] Get Model Name."); }
            winProgress.ShowMessage("Get Model Name.");

            if (string.IsNullOrWhiteSpace(targetController.ModelName))
            { targetController.ModelName = getModelName(targetController.IPAddress); }

            if (Settings.Ins.ExecLog)
            { SaveExecLog($"     Model Name : {targetController.ModelName}"); }

            // Serial取得 [*4]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[*4] Get Serial No."); }
            winProgress.ShowMessage("Get Serial No.");

            if (string.IsNullOrWhiteSpace(targetController.SerialNo))
            { targetController.SerialNo = getSerialNo(targetController.IPAddress); }

            if (Settings.Ins.ExecLog == true)
            { SaveExecLog($"     Serial Num. : {targetController.SerialNo}"); }

            // Tempフォルダ内のファイルを削除 [*5]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[*5] Delete Temporary Files."); }
            winProgress.ShowMessage("Delete Temporary Files.");

            tempPath = $"{applicationPath}\\Backup\\{targetController.ModelName}_{targetController.SerialNo}_Temp";
            if (!System.IO.Directory.Exists(tempPath))
            {
                System.IO.Directory.CreateDirectory(tempPath);
            }
            else
            {
                files = System.IO.Directory.GetFiles(tempPath, "*", System.IO.SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    try { System.IO.File.Delete(files[i]); }
                    catch { return false; }
                }
            }
            winProgress.PutForward1Step();

            // ●Open CA-410 [9]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[9] Open CA-410."); }
            winProgress.ShowMessage("Open CA-410.");

            if (!openSubDispatch(1, Settings.Ins.NoOfProbes, Settings.Ins.PortVal))
            {
                ShowMessageWindow("Can not open CA-410.\r\nPlease check it status.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●CA-410設定 [10]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[10] Set CA-410 Settings."); }
            winProgress.ShowMessage("Set CA-410 Settings.");

            if (!setCA410SettingDispatchModuleGamma())
            {
                ShowMessageWindow("Can not set CA-410 settings.\r\nPlease check it status.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●User設定保存 [11]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[11] Store User Settings."); }
            winProgress.ShowMessage("Store User Settings.");

            try
            {
                if (getUserSetting(out List<UserSetting> lstUserSetting) != true)
                { return false; }
                m_lstUserSetting = lstUserSetting;
            }
            catch (Exception ex)
            {
                SaveErrorLog("[getUserSetting] Source : " + ex.Source + "\r\nException Message : " + ex.Message);
                ShowMessageWindow(ex.Message, "Exception! (getUserSetting())", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }
            winProgress.PutForward1Step();

            // ●調整用設定 [12]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[12] Set Adjust Settings."); }
            winProgress.ShowMessage("Set Adjust Settings.");

            setAdjustSetting(false);
            winProgress.PutForward1Step();

            // ●Layout情報Off [13]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[13] Layout Off."); }
            winProgress.ShowMessage("Layout Off.");

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdDispUnitAddrOff, 0, cont.IPAddress); }
            winProgress.PutForward1Step();

            // ●色度測定[14]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[14] Measurement Color Start."); }
            winProgress.ShowMessage("Measurement Color.");

            // 初期化
            initMgamParameter();

            CrosstalkCorrections = Enumerable.Repeat(new float[3], 12).ToArray(); // crosstalkCorrections = [CellNo.][CTC R/G/B]
            for (int cell = 0; cell < 12; cell++)
            { CrosstalkCorrections[cell] = new float[3]; }

            // 調整対象
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog($" [*1] (Target) C{targetUnit.ControllerID}-{targetUnit.PortNo}-{targetUnit.UnitNo}"); }

            // Signal Mode設定
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog(" [*2] Set Adjust Signal Mode."); }
            winProgress.ShowMessage("Set Adjust Signal Mode.");

            if (!setAdjustSigModeSettings(targetController, true))
            {
                ShowMessageWindow("Failed to set adjust signal mode.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }

            if (Settings.Ins.ExecLog == true)
            { SaveExecLog(" [*3] Measurement Color."); }
            winProgress.ShowMessage("Measurement Color.");

            double[][] refUnitMeasureHighData;
            double[][] refUnitMeasureLowData;
            double[][] targetUnitMeasureHighData;
            double[][] targetUnitMeasureLowData;
            while (true)
            {
                bool status = measurementCell(targetUnit, targetCell, false, out refUnitMeasureHighData, out refUnitMeasureLowData, out UserOperation operation);
                if (status)
                {
                    // 調整対象キャビネットの測定データを保持
                    targetUnitMeasureHighData = refUnitMeasureHighData;
                    targetUnitMeasureLowData = refUnitMeasureLowData;
                    break;
                }
                else
                { return false; }
            }

            // 参照対象
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog($" [*1] (Reference) C{referenceUnit.ControllerID}-{referenceUnit.PortNo}-{referenceUnit.UnitNo}"); }

            if(Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[*2] Set Adjust Signal Mode."); }
            winProgress.ShowMessage("Set Adjust Signal Mode.");

            if (!setAdjustSigModeSettings(dicController[referenceUnit.ControllerID], false))
            {
                ShowMessageWindow("Failed to set signal mode.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }

            if (Settings.Ins.ExecLog == true)
            { SaveExecLog(" [*3] Measurement Color."); }
            winProgress.ShowMessage("Measurement Color.");

            while (true)
            {
                bool status = measurementCell(referenceUnit, referenceCell, false, out refUnitMeasureHighData, out refUnitMeasureLowData, out UserOperation operation);
                if (status)
                { break; }
                else
                { return false; }
            }

            if (Settings.Ins.ExecLog == true)
            { SaveExecLog($" [*4] Make Threshold Parameter."); }
            winProgress.ShowMessage("Make Threshold Parameter.");

            // Threshold Levelパラメーター作成
            try { makeThresholdLevel(); }
            catch (Exception ex)
            {
                SaveErrorLog($"[makeThresholdLevel] Source : {ex.Source}\r\nException Message : {ex.Message}");
                ShowMessageWindow($"Failed in make threshold parameter.\r\n{ex.Message}", "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }

            // Target値算出
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog($" [*5] Calc Target."); }
            winProgress.ShowMessage("Calc Target.");

            try { calcTargetParameter(refUnitMeasureHighData, refUnitMeasureLowData); }
            catch (Exception ex)
            {
                SaveErrorLog($"[calcTargetParameter] Source : {ex.Source}\r\nException Message : {ex.Message}");
                ShowMessageWindow($"Failed in calc target parameter.\r\n{ex.Message}", "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }

            // MGAMパラメーター計算
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog($" [*6] Calc Module No.{targetCell + 1} MGAM Parameter."); }
            winProgress.ShowMessage($"Calc Module No.{targetCell + 1} MGAM Parameter.");

            try
            {
                if (!calcMgamParameter(targetCell, targetUnitMeasureHighData, targetUnitMeasureLowData))
                { return false; }
            }
            catch (Exception ex)
            {
                SaveErrorLog($"[calcMgamParameter] Source : {ex.Source}\r\nException Message : {ex.Message}");
                ShowMessageWindow($"Failed in calc mgam parameter.\r\n{ex.Message}", "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }

            // LCファイルに書き出し
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog(" [*7] Save New Lc Data."); }
            winProgress.ShowMessage("Save New Lc Data.");

            try { overwriteMgamData(targetUnit, lstTargetCell); }
            catch (Exception ex)
            {
                SaveErrorLog($"[overwriteMgamData] Source : {ex.Source}\r\nException Message : {ex.Message}");
                ShowMessageWindow($"Failed in overwrite mgam data.\r\n{ex.Message}", "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }
            winProgress.PutForward1Step();

            // HCファイルに書き出し
            if (Settings.Ins.GammaGain.GainMeasurementMode == GainMeasurementMode.Full)
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog(" [*8] Save New Hc Data."); }
                winProgress.ShowMessage("Save New Hc Data.");

                try { overwriteColorCorrectionData(targetUnit, lstTargetCell); }
                catch (Exception ex)
                {
                    SaveErrorLog($"[overwriteColorCorrectionData] Source : {ex.Source}\r\nException Message : {ex.Message}");
                    ShowMessageWindow($"Failed in overwrite color correction data.\r\n{ex.Message}", "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                    return false;
                }
            }

            // ●TestPattern OFF[15]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[15] TestPattern Off."); }
            winProgress.ShowMessage("TestPattern Off.");

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdIntSignalOff, 0, cont.IPAddress); }
            winProgress.PutForward1Step();

            // 推定処理時間
            int processSec = 0;
            int currentStep = 0;
            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));

            int dataMoveSec = calcAdjustGainMoveSec(lstTargetUnit.Count);
            int responseSec = calcAdjustGainResponseSec(lstTargetController.Count);
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.StartRemainTimer(processSec);

            // ●Move Files[16]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[16] Move Adjusted Files."); }
            winProgress.ShowMessage("Move Adjusted Files.");
            
            tempPath = $"{applicationPath}\\Backup\\{targetController.ModelName}_{targetController.SerialNo}_Temp";
            files = System.IO.Directory.GetFiles(tempPath, "*", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                if (!putFileFtpRetry(targetController.IPAddress, files[i]))
                {
                    ShowMessageWindow("Failed to upload file to ftp.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }
            }
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.StartRemainTimer(processSec);

            // ●Cabinet Power Off [17]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[17] Cabinet Power Off."); }
            winProgress.ShowMessage("Cabinet Power Off.");

            foreach (ControllerInfo controller in lstTargetController)
            { sendSdcpCommand(SDCPClass.CmdUnitPowerOff, controller.IPAddress); }
            System.Threading.Thread.Sleep(SLEEP_TIME_AFTER_PANEL_OFF);

            if (getUnitPowerStatus() != true)
            {
                string msg = "Failed to cabinet power off.";
                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.StartRemainTimer(processSec);

            // ●Reconfig [18]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[18] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            sendReconfig();
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.StartRemainTimer(processSec);

            // ●書き込みコマンド発行 [19]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[19] Send Write Command."); }
            winProgress.ShowMessage("Send Write Command.");

            sendSdcpCommand(SDCPClass.CmdDataWrite, targetController.IPAddress);
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.StartRemainTimer(processSec);

            // ●書き込みComplete待ち [20]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[20] Waiting for the process of controller."); }
            winProgress.ShowMessage("Waiting for the process of controller.");

            try
            {
                while (true)
                {
                    if (checkCompleteFtp(targetController.IPAddress, "write_complete") == true)
                    { break; }

                    winProgress.ShowMessage("Waiting for the process of controller.");

                    System.Threading.Thread.Sleep(1000);
                }
            }
            catch
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("Send Reconfig."); }
                winProgress.ShowMessage("Send Reconfig.");
                sendReconfig();
                return false;
            }
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.StartRemainTimer(processSec);

            // ●調整設定解除 [21]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[21] Set Normal Setting."); }
            winProgress.ShowMessage("Set Normal Setting.");

            setNormalSetting();
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.StartRemainTimer(processSec);

            // ●User設定に戻す [22]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[22] Restore User Settings."); }
            winProgress.ShowMessage("Restore User Settings.");

            setUserSetting(m_lstUserSetting);
            m_lstUserSetting = null;    //User設定を戻したので呼び出し側の設定処理をスキップ
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.StartRemainTimer(processSec);

            // ●Latest → Previousフォルダへコピー [23]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[23] Move Latest to Previous."); }
            winProgress.ShowMessage("[22] Move Latest to Previous.");
            
            copyLatest2Previous($"{applicationPath}\\Backup\\{targetController.ModelName}_{targetController.SerialNo}");
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.StartRemainTimer(processSec);

            // ●Temp → Latestフォルダへコピー [24]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[24] Move Temp to Latest."); }
            winProgress.ShowMessage("[23] Move Temp to Latest.");

            copyTemp2Latest($"{applicationPath}\\Backup\\{targetController.ModelName}_{targetController.SerialNo}");
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.StartRemainTimer(processSec);

            // ●Reconfig [25]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[25] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            sendReconfig();
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.StartRemainTimer(processSec);

            // ●Tempフォルダ内のファイルを削除 [26]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[26] Delete Temporary Files."); }
            winProgress.ShowMessage("Delete Temporary Files.");
            
            tempPath = $"{applicationPath}\\Backup\\{targetController.ModelName}_{targetController.SerialNo}_Temp";
            if (!System.IO.Directory.Exists(tempPath))
            { System.IO.Directory.CreateDirectory(tempPath); }
            else
            {
                files = System.IO.Directory.GetFiles(tempPath, "*", System.IO.SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    try { System.IO.File.Delete(files[i]); }
                    catch { return false; }
                }
            }
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.StartRemainTimer(processSec);

            // ●選択を解除 [27]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[27] Unselect Cabinets and Module."); }
            winProgress.ShowMessage("Unselect Cabinet and Module.");

            btnGgDeselectAllUnit_Click(null, null);
            DiselectGgAllCells();
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            // ●FTP Off [28]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[28] FTP Off."); }
            winProgress.ShowMessage("FTP Off.");

            if (!setFtpOff())
            {
                ShowMessageWindow("Failed to Set FTP Off.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            Dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustGainProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            // ●Signal Mode戻し [29]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[29] Set Signal Mode One."); }
            winProgress.ShowMessage("Set Signal Mode One.");

            if (!setDefaultSignalModeOne())
            {
                ShowMessageWindow("Failed to set signal mode one.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            return true;
        }

        /// <summary>
        /// モジュールアロケーションの選択状態を初期化
        /// </summary>
        private void DiselectGgAllCells()
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Dispatcher.Invoke(new Action(() => { aryCellGg[i, j].IsChecked = false; }));
                }
            }
        }

        /// <summary>
        /// 選択したキャビネットを取得
        /// </summary>
        /// <param name="lstUnitList">選択したキャビネットを格納するリスト</param>
        /// <param name="MaxX">キャビネットアロケーションの横最大値</param>
        /// <param name="MaxY">キャビネットアロケーションの縦最大値</param>
        private void storeSelectedUnit(out List<UnitInfo> lstUnitList, int MaxX, int MaxY)
        {
            lstUnitList = new List<UnitInfo>();

            for (int y = 0; y < MaxY; y++)
            {
                for (int x = 0; x < MaxX; x++)
                {
                    if (aryUnitGg[x, y].UnitInfo != null && aryUnitGg[x, y].IsChecked == true)
                    {
                        lstUnitList.Add(aryUnitGg[x, y].UnitInfo);
                    }
                }
            }
        }

        /// <summary>
        /// 選択したモジュールを取得
        /// </summary>
        /// <param name="lstTargetCell">選択したモジュールを格納するリスト</param>
        private void storeSelectedCell(out List<int> lstTargetCell)
        {
            lstTargetCell = new List<int>();

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (aryCellGg[i, j].IsChecked == true)
                    {
                        lstTargetCell.Add(4 * j + i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 参照するモジュールの位置を取得
        /// </summary>
        /// <param name="position"></param>
        private void storeReferenceCellPosition(out GainReferenceCellPosition position)
        {
            if (rbRefPostionBottom.IsChecked == true)
            {
                position = GainReferenceCellPosition.Bottom;
            }
            else if (rbRefPostionLeft.IsChecked == true)
            {
                position = GainReferenceCellPosition.Left;
            }
            else if (rbRefPostionRight.IsChecked == true)
            {
                position = GainReferenceCellPosition.Right;
            }
            else
            {
                position = GainReferenceCellPosition.Top;
            }
        }

        /// <summary>
        /// 参照するモジュール番号を取得
        /// </summary>
        /// <param name="targetUnit">対象キャビネット</param>
        /// <param name="targetCell">調整モジュール</param>
        /// <param name="position">調整モジュールから参照するモジュールの位置</param>
        /// <param name="referenceUnit"></param>
        /// <param name="referenceCell"></param>
        private bool storeReferenceCell(UnitInfo targetUnit, int targetCell, GainReferenceCellPosition position, out UnitInfo referenceUnit, out int referenceCell)
        {
            referenceUnit = null;
            referenceCell = -1;

            CellNum cell = (CellNum)targetCell;
            Tuple<CabinetOffset, int> offset = ReferencePosition.GetModuleOffset(cell, position);
            int x = targetUnit.X - 1 + offset.Item1.X;
            int y = targetUnit.Y - 1 + offset.Item1.Y;
            if (x >= INT_ZERO && x < allocInfo.MaxX && y >= INT_ZERO && y < allocInfo.MaxY)
            {
                UnitInfo unit = aryUnitGg[x, y].UnitInfo;
                if (unit == null)
                { return false; }

                referenceUnit = unit;
                referenceCell = offset.Item2; // CellNo.
            }
            else
            { return false; }

            return true;
        }

        

        /// <summary>
        /// 全対象キャビネットのlc.binファイルがあるか確認
        /// </summary>
        /// <param name="lstUnit"></param>
        private bool checkLcFileExists(List<UnitInfo> lstUnit, out List<UnitInfo> lstDataOkUnit, out string msg)
        {
            lstDataOkUnit = new List<UnitInfo>();
            string msg1 = "";
            string msg2 = "";

            foreach (UnitInfo unit in lstUnit)
            {
                string filePath = makeFilePath(unit, FileDirectory.Backup_Latest, DataType.LcData);
                if (!System.IO.File.Exists(filePath) || !checkAllDataLength(filePath) || !confirmChecksum(filePath))
                {
                    if (!string.IsNullOrEmpty(msg1))
                    { msg1 += "\r\n"; }

                    msg1 += $"C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}:Invalid lc.bin.";
                    continue;
                }

                // MGAMデータの存在チェック
                if (!checkMGAMDataExists(unit, filePath, out string notExistsCellNum))
                {
                    if (!string.IsNullOrEmpty(msg2))
                    { msg2 += "\r\n"; }

                    msg2 += notExistsCellNum;
                    continue;
                }

                lstDataOkUnit.Add(unit);
            }

            msg = "";
            if (string.IsNullOrEmpty(msg1))
            {
                if (!string.IsNullOrEmpty(msg2))
                { msg += "\r\n" + msg2; }
            }
            else
            {
                msg += "\r\n" + msg1;
                if (!string.IsNullOrEmpty(msg2))
                { msg += "\r\n" + msg2; }
            }

            if (!string.IsNullOrEmpty(msg))
            { return false; }

            return true;
        }

        /// <summary>
        /// LED Module Gamma存在確認
        /// </summary>
        /// <param name="unit">対象キャビネット</param>
        /// <param name="file">対象キャビネットのlc.binファイル</param>
        /// <param name="notExistsCellNum">Gammaデータが存在しないモジュール</param>
        /// <returns></returns>
        private bool checkMGAMDataExists(UnitInfo unit, string file, out string notExistsCellNum)
        {
            notExistsCellNum = "";
            List<int> lstNotExistsCellInfo = new List<int>();

            List<CompositeConfigData> lstGammaData = CompositeConfigData.getCompositeConfigDataList(file, dt_MgamWrite);
            if (lstGammaData.Count != moduleCount)
            {
                for (int i = 0; i < moduleCount; i++)
                {
                    if (!lstGammaData.Any(x => x.header.Option1 == i + 1))
                    { lstNotExistsCellInfo.Add(i + 1); }
                }

                notExistsCellNum = $"C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}:Lack of MGAM data [";
                for (int i = 0; i < lstNotExistsCellInfo.Count; i++)
                {
                    notExistsCellNum += lstNotExistsCellInfo[i];
                    if (lstNotExistsCellInfo.Count == i + 1)
                    {
                        notExistsCellNum += "]";
                    }
                    else
                    {
                        notExistsCellNum += ",";
                    }
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// CA-410設定
        /// </summary>
        /// <param name="mode">DisplayMode</param>
        /// <returns></returns>
        private bool setCA410SettingDispatchModuleGamma(CaSdk.DisplayMode mode = CaSdk.DisplayMode.DispModeXYZ)
        {
            var dispatcher = Application.Current.Dispatcher;
            return dispatcher.Invoke(() => setCA410SettingModuleGamma(mode));
        }

        /// <summary>
        /// CA-410の設定項目
        /// </summary>
        /// <param name="mode">DisplayMode</param>
        /// <returns></returns>
        private bool setCA410SettingModuleGamma(CaSdk.DisplayMode mode = CaSdk.DisplayMode.DispModeXYZ) // CA-410の設定
        {
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[0] setCA410SettingModuleGamma start"); }

            // Channel
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[1] setChannelSub()"); }

            if (setChannelSub(getCAChannel()) != true)
            { return false; }

            // Sync Mode
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[2] setSyncModeSub()"); }

            if (setSyncModeSub((float)CaSdk.SyncMode.SyncUNIV) != true)
            { return false; }

            // Display Mode
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[3] setDisplayModeSub()"); }

            if (setDisplayModeSub(mode) != true)
            { return false; }

            // Displya Digit
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[4] setDisplayDigitsSub()"); }

            if (setDisplayDigitsSub(CaSdk.DisplayDigits.DisplayDigit4) != true)
            { return false; }

            // Averaging Mode
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[5] setAveragingModeSub()"); }

            if (setAveragingModeSub(CaSdk.AveragingMode.AveragingAuto) != true)
            { return false; }

            // Brightness Unit
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[6] setBrightnessCabinetSub()"); }

            if (setBrightnessUnitSub(CaSdk.BrightnessUnit.BrightUnitcdm2) != true)
            { return false; }

            return true;
        }

        /// <summary>
        /// 調整用Signal Mode 1&2の設定
        /// </summary>
        /// <param name="controller">対象コントローラー</param>
        /// <param name="isTargetUnit">true: 調整対象キャビネット、false: 参照キャビネット</param>
        private bool setAdjustSigModeSettings(ControllerInfo controller, bool isTargetUnit)
        {
            // Signal Mode 1
            byte[] cmd = new byte[SDCPClass.CmdSigModeOneSet.Length];
            Array.Copy(SDCPClass.CmdSigModeOneSet, cmd, SDCPClass.CmdSigModeOneSet.Length);

            cmd[20] = SIG_MODE_ONE_ADJUST_DATA; // Reserved,Reserved,TMP On,AGC On,SBM On,OGAM Off,UNC Off,LGAM On
            if (!sendSdcpCommand(cmd, out string buff, controller.IPAddress))
            { return false; }

            if (isErrorResponseValue(buff))
            { return false; }

            // Signal Mode 2
            cmd = new byte[SDCPClass.CmdSigModeTwoSet.Length];
            Array.Copy(SDCPClass.CmdSigModeTwoSet, cmd, SDCPClass.CmdSigModeTwoSet.Length);

            if (isTargetUnit)
            {
                cmd[20] = SIG_MODE_TWO_ADJUST_DATA; // Reserved,Reserved,Reserved,MGAM Off,Reserved,LMCAL On,LCALH On,LCALV On
                if (!sendSdcpCommand(cmd, out buff, controller.IPAddress))
                { return false; }
            }
            else
            {
                cmd[20] = SIG_MODE_TWO_DEFAULT_DATA; // Reserved,Reserved,Reserved,MGAM On,Reserved,LMCAL On,LCALH On,LCALV On
                if (!sendSdcpCommand(cmd, out buff, controller.IPAddress))
                { return false; }
            }

            if (isErrorResponseValue(buff))
            { return false; }

            return true;
        }

        /// <summary>
        /// MGAMパラメーター格納するリストの新しいインスタンス作成
        /// </summary>
        private void initMgamParameter()
        {
            lstLowCorGain0 = new List<byte[]>();
            lstHightCorGain1 = new List<byte[]>();
            lstHightCorGain2 = new List<byte[]>();
            lstHightCorGain3 = new List<byte[]>();
        }

        /// <summary>
        /// テストパータン表示
        /// </summary>
        /// <param name="unit">対象キャビネット</param>
        /// <param name="cellNum">対象モジュール番号</param>
        /// <param name="enableRewind">測定のリトライ機能有効無効するフラグ true:有効 false:無効</param>
        /// <param name="measureHighData">測定データを格納する配列[5%/50%][r/g/b (Lv)]</param>
        /// <param name="measureLowData">測定データを格納する配列[(0.05%) r/g/b/w][X/Y/Z]</param>
        /// <param name="operation">操作オペレーションの結果</param>
        private bool measurementCell(UnitInfo unit, int cellNum, bool enableRewind, out double[][] measureHighData, out double[][] measureLowData, out UserOperation operation)
        {
            measureHighData = Enumerable.Repeat(new double[3], 2).ToArray(); // measureHighData = [5%/50%][r/g/b (Lv)]
            for (int rgb = 0; rgb < 2; rgb++)
            { measureHighData[rgb] = new double[3]; }

            measureLowData = Enumerable.Repeat(new double[3], 4).ToArray(); // measureLowData = [(0.05%) r/g/b/w][X/Y/Z]
            for (int xyz = 0; xyz < 4; xyz++)
            { measureLowData[xyz] = new double[3]; }

            UserOperation ope = UserOperation.None;

            while (true)
            {
                outputCross(unit, cellNum, CrossPointPosition.Center);

                Dispatcher.Invoke(() => showMeasureWindow(out ope, enableRewind));
                operation = ope;
                if (operation == UserOperation.Cancel)
                {
                    string msg = "Do you want to finish the measurement ?";
                    showMessageWindow(out bool? result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");
                    if (result == true)
                    { return false; }

                    continue;
                }
                else if (operation == UserOperation.Rewind)
                { return false; }
                else // operation == UserOperation.OK
                { break; }
            }

#if DEBUG
            Console.WriteLine($"C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo} Module No.{cellNum + 1}");
            SaveExecLog($"       C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo} Module No.{cellNum + 1}");
#elif Release_log
            SaveExecLog($"       C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo} Module No.{cellNum + 1}");
#endif

            // Red 5%
            outputSquareCellSize(unit, CellColor.Red, (CellNum)cellNum, brightness._5pc);
            System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

            bool status = measurementColor(out double highData);
            if (status)
            { measureHighData[REGULAR][RED] = highData; }
            else { return false; }

#if DEBUG
            Console.WriteLine($"R(R)Lv: {measureHighData[REGULAR][RED]}");
            SaveExecLog($"       R(R)Lv: {measureHighData[REGULAR][RED]}");
#elif Release_log
            SaveExecLog($"       R(R)Lv: {measureHighData[REGULAR][RED]}");
#endif

            playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

            // Green 5%
            outputSquareCellSize(unit, CellColor.Green, (CellNum)cellNum, brightness._5pc);
            System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

            status = measurementColor(out highData);
            if (status)
            { measureHighData[REGULAR][GREEN] = highData; }
            else { return false; }

#if DEBUG
            Console.WriteLine($"G(R)Lv: {measureHighData[REGULAR][GREEN]}");
            SaveExecLog($"       G(R)Lv: {measureHighData[REGULAR][GREEN]}");
#elif Release_log
            SaveExecLog($"       G(R)Lv: {measureHighData[REGULAR][GREEN]}");
#endif

            playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

            // Blue 5%
            outputSquareCellSize(unit, CellColor.Blue, (CellNum)cellNum, brightness._5pc);
            System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

            status = measurementColor(out highData);
            if (status)
            { measureHighData[REGULAR][BLUE] = highData; }
            else { return false; }

#if DEBUG
            Console.WriteLine($"B(R)Lv: {measureHighData[REGULAR][BLUE]}");
            SaveExecLog($"       B(R)Lv: {measureHighData[REGULAR][BLUE]}");
#elif Release_log
            SaveExecLog($"       B(R)Lv: {measureHighData[REGULAR][BLUE]}");
#endif

            playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

            // Red 50%
            outputSquareCellSize(unit, CellColor.Red, (CellNum)cellNum, brightness._50pc);
            System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

            status = measurementColor(out highData);
            if (status)
            { measureHighData[HIGH][RED] = highData; }
            else { return false; }

#if DEBUG
            Console.WriteLine($"R(H)Lv: {measureHighData[HIGH][RED]}");
            SaveExecLog($"       R(H)Lv: {measureHighData[HIGH][RED]}");
#elif Release_log
            SaveExecLog($"       R(H)Lv: {measureHighData[HIGH][RED]}");
#endif

            playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

            // Green 50%
            outputSquareCellSize(unit, CellColor.Green, (CellNum)cellNum, brightness._50pc);
            System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

            status = measurementColor(out highData);
            if (status)
            { measureHighData[HIGH][GREEN] = highData; }
            else { return false; }

#if DEBUG
            Console.WriteLine($"G(H)Lv: {measureHighData[HIGH][GREEN]}");
            SaveExecLog($"       G(H)Lv: {measureHighData[HIGH][GREEN]}");
#elif Release_log
            SaveExecLog($"       G(H)Lv: {measureHighData[HIGH][GREEN]}");
#endif

            playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

            // Blue 50%
            outputSquareCellSize(unit, CellColor.Blue, (CellNum)cellNum, brightness._50pc);
            System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

            status = measurementColor(out highData);
            if (status)
            { measureHighData[HIGH][BLUE] = highData; }
            else { return false; }

#if DEBUG
            Console.WriteLine($"B(H)Lv: {measureHighData[HIGH][BLUE]}");
            SaveExecLog($"       B(H)Lv: {measureHighData[HIGH][BLUE]}");
#elif Release_log
            SaveExecLog($"       B(H)Lv: {measureHighData[HIGH][BLUE]}");
#endif

            if (Settings.Ins.GammaGain.GainMeasurementMode == GainMeasurementMode.Full)
            {
                playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

                // Red 0.05%
                outputSquareCellSize(unit, CellColor.Red, (CellNum)cellNum, brightness._005pc);
                System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

                status = measurementColor(out double[] lowData);
                if (status)
                {
                    // X
                    measureLowData[RED][XYZ_X] = lowData[0];
                    // Y
                    measureLowData[RED][XYZ_Y] = lowData[1];
                    // Z
                    measureLowData[RED][XYZ_Z] = lowData[2];
                }
                else { return false; }

#if DEBUG
                Console.WriteLine($"R(L)X: {measureLowData[RED][XYZ_X]}\r\nR(L)Y: {measureLowData[RED][XYZ_Y]}\r\nR(L)Z: {measureLowData[RED][XYZ_Z]}");
                SaveExecLog($"       R(L)X: {measureLowData[RED][XYZ_X]}");
                SaveExecLog($"       R(L)Y: {measureLowData[RED][XYZ_Y]}");
                SaveExecLog($"       R(L)Z: {measureLowData[RED][XYZ_Z]}");
#elif Release_log
                SaveExecLog($"       R(L)X: {measureLowData[RED][XYZ_X]}");
                SaveExecLog($"       R(L)Y: {measureLowData[RED][XYZ_Y]}");
                SaveExecLog($"       R(L)Z: {measureLowData[RED][XYZ_Z]}");
#endif

                playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

                // Green 0.05%
                outputSquareCellSize(unit, CellColor.Green, (CellNum)cellNum, brightness._005pc);
                System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

                status = measurementColor(out lowData);
                if (status)
                {
                    // X
                    measureLowData[GREEN][XYZ_X] = lowData[0];
                    // Y
                    measureLowData[GREEN][XYZ_Y] = lowData[1];
                    // Z
                    measureLowData[GREEN][XYZ_Z] = lowData[2];
                }
                else { return false; }

#if DEBUG
                Console.WriteLine($"G(L)X: {measureLowData[GREEN][XYZ_X]}\r\nG(L)Y: {measureLowData[GREEN][XYZ_Y]}\r\nG(L)Z: {measureLowData[GREEN][XYZ_Z]}");
                SaveExecLog($"       G(L)X: {measureLowData[GREEN][XYZ_X]}");
                SaveExecLog($"       G(L)Y: {measureLowData[GREEN][XYZ_Y]}");
                SaveExecLog($"       G(L)Z: {measureLowData[GREEN][XYZ_Z]}");
#elif Release_log
                SaveExecLog($"       G(L)X: {measureLowData[GREEN][XYZ_X]}");
                SaveExecLog($"       G(L)Y: {measureLowData[GREEN][XYZ_Y]}");
                SaveExecLog($"       G(L)Z: {measureLowData[GREEN][XYZ_Z]}");
#endif

                playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

                // Blue 0.05%
                outputSquareCellSize(unit, CellColor.Blue, (CellNum)cellNum, brightness._005pc);
                System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

                status = measurementColor(out lowData);
                if (status)
                {
                    // X
                    measureLowData[BLUE][XYZ_X] = lowData[0];
                    // Y
                    measureLowData[BLUE][XYZ_Y] = lowData[1];
                    // Z
                    measureLowData[BLUE][XYZ_Z] = lowData[2];
                }
                else { return false; }

#if DEBUG
                Console.WriteLine($"B(L)X: {measureLowData[BLUE][XYZ_X]}\r\nB(L)Y: {measureLowData[BLUE][XYZ_Y]}\r\nB(L)Z: {measureLowData[BLUE][XYZ_Z]}");
                SaveExecLog($"       B(L)X: {measureLowData[BLUE][XYZ_X]}");
                SaveExecLog($"       B(L)Y: {measureLowData[BLUE][XYZ_Y]}");
                SaveExecLog($"       B(L)Z: {measureLowData[BLUE][XYZ_Z]}");
#elif Release_log
                SaveExecLog($"       B(L)X: {measureLowData[BLUE][XYZ_X]}");
                SaveExecLog($"       B(L)Y: {measureLowData[BLUE][XYZ_Y]}");
                SaveExecLog($"       B(L)Z: {measureLowData[BLUE][XYZ_Z]}");
#endif

                playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

                // White 0.05%
                outputSquareCellSize(unit, CellColor.White, (CellNum)cellNum, brightness._005pc);
                System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

                status = measurementColor(out lowData);
                if (status)
                {
                    // X
                    measureLowData[WHITE][XYZ_X] = lowData[0];
                    // Y
                    measureLowData[WHITE][XYZ_Y] = lowData[1];
                    // Z
                    measureLowData[WHITE][XYZ_Z] = lowData[2];
                }
                else { return false; }

#if DEBUG
                Console.WriteLine($"W(L)X: {measureLowData[WHITE][XYZ_X]}\r\nW(L)Y: {measureLowData[WHITE][XYZ_Y]}\r\nW(L)Z: {measureLowData[WHITE][XYZ_Z]}");
                SaveExecLog($"       W(L)X: {measureLowData[WHITE][XYZ_X]}");
                SaveExecLog($"       W(L)Y: {measureLowData[WHITE][XYZ_Y]}");
                SaveExecLog($"       W(L)Z: {measureLowData[WHITE][XYZ_Z]}");
#elif Release_log
                SaveExecLog($"       W(L)X: {measureLowData[WHITE][XYZ_X]}");
                SaveExecLog($"       W(L)Y: {measureLowData[WHITE][XYZ_Y]}");
                SaveExecLog($"       W(L)Z: {measureLowData[WHITE][XYZ_Z]}");
#endif

            }

            playSound(applicationPath + "\\Components\\Sound\\button01b.mp3");

            return true;
        }

        /// <summary>
        /// モジュール単位のテストパータン
        /// </summary>
        /// <param name="unit">対象キャビネット</param>
        /// <param name="color">テストパータンの色</param>
        /// <param name="cellNum">対象モジュール番号</param>
        /// <param name="lv">表示する輝度</param>
        private void outputSquareCellSize(UnitInfo unit, CellColor color, CellNum cellNum, int lv)
        {
            byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            int startH, startV;

            // Unit Offset
            startH = unit.PixelX;
            startV = unit.PixelY;

            // Cell Offset
            startH += modDx * ((int)cellNum % 4);
            startV += modDy * ((int)cellNum / 4);

            // Pattern Window
            cmd[21] += 0x09;

            // Foreground Color
            if (color == CellColor.Red)
            {
                cmd[22] = (byte)(lv >> 8); // Red
                cmd[23] = (byte)(lv & 0xFF);
                cmd[24] = 0x00;
                cmd[25] = 0x00;
                cmd[26] = 0x00;
                cmd[27] = 0x00;
            }
            else if (color == CellColor.Green)
            {
                cmd[22] = 0x00;
                cmd[23] = 0x00;
                cmd[24] = (byte)(lv >> 8); // Green
                cmd[25] = (byte)(lv & 0xFF);
                cmd[26] = 0x00;
                cmd[27] = 0x00;
            }
            else if (color == CellColor.Blue)
            {
                cmd[22] = 0x00;
                cmd[23] = 0x00;
                cmd[24] = 0x00;
                cmd[25] = 0x00;
                cmd[26] = (byte)(lv >> 8); // Blue
                cmd[27] = (byte)(lv & 0xFF);
            }
            else if (color == CellColor.White)
            {
                cmd[22] = (byte)(lv >> 8); // Red
                cmd[23] = (byte)(lv & 0xFF);
                cmd[24] = (byte)(lv >> 8); // Green
                cmd[25] = (byte)(lv & 0xFF);
                cmd[26] = (byte)(lv >> 8); // Blue
                cmd[27] = (byte)(lv & 0xFF);
            }

            // Start Position
            cmd[34] = (byte)(startH >> 8);
            cmd[35] = (byte)(startH & 0xFF);
            cmd[36] = (byte)(startV >> 8);
            cmd[37] = (byte)(startV & 0xFF);

            // H, V Width
            cmd[38] = 0x00;
            cmd[39] = (byte)modDx;
            cmd[40] = 0x00;
            cmd[41] = (byte)modDy;

            if (curType == CursorType.RedSquare)
            {
                // Background Color(White)
                cmd[28] = (byte)(brightness.UF_20pc >> 8); ; // Red
                cmd[29] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[30] = (byte)(brightness.UF_20pc >> 8); ; // Green
                cmd[31] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[32] = (byte)(brightness.UF_20pc >> 8); ; // Blue
                cmd[33] = (byte)(brightness.UF_20pc & 0xFF);

                foreach (ControllerInfo cont in dicController.Values)
                {
                    if (cont.ControllerID == unit.ControllerID)
                    { sendSdcpCommand(cmd, 500, cont.IPAddress); }
                    else
                    { outputRaster(brightness.UF_20pc, cont.ControllerID); }
                }
            }
            else
            {
                foreach (ControllerInfo cont in dicController.Values)
                {
                    if (cont.ControllerID == unit.ControllerID)
                    { sendSdcpCommand(cmd, 500, cont.IPAddress); }
                    else
                    { outputRaster(0, cont.ControllerID); }
                }
            }
        }

        /// <summary>
        /// 測定データを取り出し
        /// </summary>
        /// <param name="Lv">取得対象になる測定データの種類[Lv]</param>
        private bool measurementColor(out double Lv)
        {
            Lv = double.NaN;

            bool status = measureSub();
            if (status)
            {
                status = getProbeLvSub(out double probeLv, 0);
                if (status)
                {
                    Lv = probeLv;
                }
                else
                {
                    return false;
                }
            }
            else
            { return false; }

            return true;
        }

        /// <summary>
        /// 測定データを取り出し
        /// </summary>
        /// <param name="data">取得対象になるデータ配列[X/Y/Z]</param>
        /// <returns></returns>
        private bool measurementColor(out double[] data)
        {
            data = new double[3];

            bool status = measureSub();
            if (status)
            {
                status = getDataSub(out data[0], out data[1], out data[2], 0);
                if (!status)
                { return false; }
            }
            else
            { return false; }

            return true;
        }

        /// <summary>
        /// TargetHパラメーター算出
        /// </summary>
        /// <param name="measureHighData">測定データが格納された配列[5%/50%][(Lv) r/g/b]</param>
        /// <param name="measureLowData">測定データが格納された配列[(0.05%) r/g/b/w][X/Y/Z]</param>
        private void calcTargetParameter(double[][] measureHighData, double[][] measureLowData)
        {
            if (Settings.Ins.GammaGain.GainMeasurementMode == GainMeasurementMode.Full)
            {
                // Rlt = R(L)Lv / R(R)Lv
                GammaGain._GainAdjustmentCoef.TargetL.Rlt = measureLowData[RED][XYZ_Y] / measureHighData[REGULAR][RED];
#if DEBUG
                Console.WriteLine($"TargetL Rlt:{GammaGain._GainAdjustmentCoef.TargetL.Rlt} = R(L)Lv:{measureLowData[RED][XYZ_Y]} / R(R)Lv:{measureHighData[REGULAR][RED]}");
#endif

                // Glt = G(L)Lv / G(R)Lv
                GammaGain._GainAdjustmentCoef.TargetL.Glt = measureLowData[GREEN][XYZ_Y] / measureHighData[REGULAR][GREEN];
#if DEBUG
                Console.WriteLine($"TargetL Glt:{GammaGain._GainAdjustmentCoef.TargetL.Glt} = G(L)Lv:{measureLowData[GREEN][XYZ_Y]} / G(R)Lv:{measureHighData[REGULAR][GREEN]}");
#endif

                // Blt = B(L)Lv / B(R)Lv
                GammaGain._GainAdjustmentCoef.TargetL.Blt = measureLowData[BLUE][XYZ_Y] / measureHighData[REGULAR][BLUE];
#if DEBUG
                Console.WriteLine($"TargetL Blt:{GammaGain._GainAdjustmentCoef.TargetL.Blt} = B(L)Lv:{measureLowData[BLUE][XYZ_Y]} / B(R)Lv:{measureHighData[REGULAR][BLUE]}");
#endif
            }

            // Rht = R(H)Lv / R(R)Lv
            GammaGain._GainAdjustmentCoef.TargetH.Rht = measureHighData[HIGH][RED] / measureHighData[REGULAR][RED];
#if DEBUG
            Console.WriteLine($"TargetH Rht:{GammaGain._GainAdjustmentCoef.TargetH.Rht} = R(H)Lv:{measureHighData[HIGH][RED]} / R(R)Lv:{measureHighData[REGULAR][RED]}");
#endif

            // Ght = G(H)Lv / G(R)Lv
            GammaGain._GainAdjustmentCoef.TargetH.Ght = measureHighData[HIGH][GREEN] / measureHighData[REGULAR][GREEN];
#if DEBUG
            Console.WriteLine($"TargetH Ght:{GammaGain._GainAdjustmentCoef.TargetH.Ght} = G(H)Lv:{measureHighData[HIGH][GREEN]} / G(R)Lv:{measureHighData[REGULAR][GREEN]}");
#endif

            // Bht = B(H)Lv / B(R)Lv
            GammaGain._GainAdjustmentCoef.TargetH.Bht = measureHighData[HIGH][BLUE] / measureHighData[REGULAR][BLUE];
#if DEBUG
            Console.WriteLine($"TargetH Bht:{GammaGain._GainAdjustmentCoef.TargetH.Bht} = B(H)Lv:{measureHighData[HIGH][BLUE]} / B(R)Lv:{measureHighData[REGULAR][BLUE]}");
#endif

        }

        /// <summary>
        /// MGAMパラメーター算出
        /// </summary>
        /// <param name="targetCell">対象Module</param>
        /// <param name="measureHighData">測定データが格納された配列[5%/50%][(Lv) r/g/b]</param>
        /// <param name="measureLowData">測定データが格納された配列[(0.05%) r/g/b/w][X/Y/Z]</param>
        /// <returns></returns>
        private bool calcMgamParameter(int targetCell, double[][] measureHighData, double[][] measureLowData)
        {
            byte[] bCorGain;
            double cl_r, cl_g, cl_b, ch1_r, ch1_g, ch1_b, ch2_r, ch2_g, ch2_b;
            double crosstalkCorrection;

            if (Settings.Ins.GammaGain.GainMeasurementMode == GainMeasurementMode.Full)
            {
                // Crosstalk補正量算出　CTC_Y/CTC_x/CTC_y
                if (!calcMgamCrosstalk(measureLowData, out double CTC_Y, out double CTC_x, out double CTC_y))
                {
                    ShowMessageWindow("Calc crosstalk correction value faild.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }

                // Crosstalk_RY = CTC_Y + WxRY * CTC_x + WyRY * CTC_y
                crosstalkCorrection = CTC_Y + (CrossTalk._CrossTalkCoef_MG.WxRY * CTC_x) + (CrossTalk._CrossTalkCoef_MG.WyRY * CTC_y);
#if DEBUG
                Console.WriteLine($"Crosstalk RY:{crosstalkCorrection} = CTC_Y:{CTC_Y} + WxRY:{CrossTalk._CrossTalkCoef_MG.WxRY} * CTC_x:{CTC_x} + WyRY:{CrossTalk._CrossTalkCoef_MG.WyRY} * CTC_y:{CTC_y}");
#endif

                CrosstalkCorrections[targetCell][RED] = (float)crosstalkCorrection;

#if DEBUG
                Console.WriteLine($"Crosstalk RY: {BitConverter.ToString(BitConverter.GetBytes(CrosstalkCorrections[targetCell][RED]))}");
#endif

                // CL0_R = (1- ( R(L)Lv / R(R)Lv / Rlt)) * Kr
                cl_r = (1 - (measureLowData[RED][XYZ_Y] / measureHighData[REGULAR][RED] / GammaGain._GainAdjustmentCoef.TargetL.Rlt)) * GammaGain._GainAdjustmentCoef.CorrectionFactorK.Kr;
#if DEBUG
                Console.WriteLine($"CL0_R:{cl_r} = ( 1 - R(L)Lv:{measureLowData[RED][XYZ_Y]} / R(R)Lv:{measureHighData[REGULAR][RED]} / Rlt:{GammaGain._GainAdjustmentCoef.TargetL.Rlt} ) * Kr:{GammaGain._GainAdjustmentCoef.CorrectionFactorK.Kr}");
                SaveExecLog($"       CL0_R: {cl_r}");
#elif Release_log
                SaveExecLog($"       CL0_R: {cl_r}");
#endif

                // CL0_R' = CL0_R + Crosstalk_RY
                cl_r += crosstalkCorrection;
#if DEBUG
                Console.WriteLine($"CL0_R':{cl_r} += Crosstalk_RY:{crosstalkCorrection}");
                SaveExecLog($"       CL0_R': {cl_r}");
#elif Release_log
                SaveExecLog($"       CL0_R': {cl_r}");
#endif

                // Crosstalk_GY = CTC_Y + WxGY * CTC_x + WyGY * CTC_y
                crosstalkCorrection = CTC_Y + (CrossTalk._CrossTalkCoef_MG.WxGY * CTC_x) + (CrossTalk._CrossTalkCoef_MG.WyGY * CTC_y);
#if DEBUG
                Console.WriteLine($"Crosstalk GY:{crosstalkCorrection} = CTC_Y:{CTC_Y} + WxRY:{CrossTalk._CrossTalkCoef_MG.WxGY} * CTC_x:{CTC_x} + WyRY:{CrossTalk._CrossTalkCoef_MG.WyGY} * CTC_y:{CTC_y}");
#endif

                CrosstalkCorrections[targetCell][GREEN] = (float)crosstalkCorrection;

#if DEBUG
                Console.WriteLine($"Crosstalk GY:{BitConverter.ToString(BitConverter.GetBytes(CrosstalkCorrections[targetCell][GREEN]))}");
#endif

                // CL0_G = (1- ( G(L)Lv / G(R)Lv / Glt)) * Kg
                cl_g = (1 - (measureLowData[GREEN][XYZ_Y] / measureHighData[REGULAR][GREEN] / GammaGain._GainAdjustmentCoef.TargetL.Glt)) * GammaGain._GainAdjustmentCoef.CorrectionFactorK.Kg;
#if DEBUG
                Console.WriteLine($"CL0_G:{cl_g} = ( 1 - G(L)Lv:{measureLowData[GREEN][XYZ_Y]} / G(R)Lv:{measureHighData[REGULAR][GREEN]} / Glt:{GammaGain._GainAdjustmentCoef.TargetL.Glt} ) * Kg:{GammaGain._GainAdjustmentCoef.CorrectionFactorK.Kg}");
                SaveExecLog($"       CL0_G: {cl_g}");
#elif Release_log
                SaveExecLog($"       CL0_G: {cl_g}");
#endif

                // CL0_G' = CL0_G + Crosstalk_GY
                cl_g += crosstalkCorrection;
#if DEBUG
                Console.WriteLine($"CL0_G':{cl_g} += Crosstalk_GY:{crosstalkCorrection}");
                SaveExecLog($"       CL0_G': {cl_g}");
#elif Release_log
                SaveExecLog($"       CL0_G': {cl_g}");
#endif

                // Crosstalk_BY = CTC_Y + WxBY * CTC_x + WyBY * CTC_y
                crosstalkCorrection = CTC_Y + (CrossTalk._CrossTalkCoef_MG.WxBY * CTC_x) + (CrossTalk._CrossTalkCoef_MG.WyBY * CTC_y);
#if DEBUG
                Console.WriteLine($"Crosstalk BY:{crosstalkCorrection} = CTC_Y:{CTC_Y} + WxBY:{CrossTalk._CrossTalkCoef_MG.WxBY} * CTC_x:{CTC_x} + WyBY:{CrossTalk._CrossTalkCoef_MG.WyBY} * CTC_y:{CTC_y}");
#endif

                CrosstalkCorrections[targetCell][BLUE] = (float)crosstalkCorrection;

#if DEBUG
                Console.WriteLine($"Crosstalk BY:{BitConverter.ToString(BitConverter.GetBytes(CrosstalkCorrections[targetCell][BLUE]))}");
#endif

                // CL0_B = (1- ( B(L)Lv / B(R)Lv / Blt)) * Kb
                cl_b = (1 - (measureLowData[BLUE][XYZ_Y] / measureHighData[REGULAR][BLUE] / GammaGain._GainAdjustmentCoef.TargetL.Blt)) * GammaGain._GainAdjustmentCoef.CorrectionFactorK.Kb;
#if DEBUG
                Console.WriteLine($"CL0_B:{cl_b} = ( 1 - B(L)Lv:{measureLowData[BLUE][XYZ_Y]} / B(R)Lv:{measureHighData[REGULAR][BLUE]} / Blt:{GammaGain._GainAdjustmentCoef.TargetL.Blt} ) * Kb:{GammaGain._GainAdjustmentCoef.CorrectionFactorK.Kb}");
                SaveExecLog($"       CL0_B: {cl_b}");
#elif Release_log
                SaveExecLog($"       CL0_B: {cl_b}");
#endif

                // CL0_B' = CL0_B + Crosstalk_BY
                cl_b += crosstalkCorrection;
#if DEBUG
                Console.WriteLine($"CL0_B':{cl_b} += Crosstalk_BY:{crosstalkCorrection}");
                SaveExecLog($"       CL0_B': {cl_b}");
#elif Release_log
                SaveExecLog($"       CL0_B': {cl_b}");
#endif

                if (!makeCorGain(cl_r, cl_g, cl_b, out bCorGain))
                {
                    ShowMessageWindow($"Low Cor Gain0 value is out of valid range.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }

                lstLowCorGain0.Add(bCorGain);
#if DEBUG
                Console.WriteLine($"Low Cor Gain0: {BitConverter.ToString(bCorGain)}");
                SaveExecLog($"       Low Cor Gain0: {BitConverter.ToString(bCorGain)}");
#elif Release_log
                SaveExecLog($"       Low Cor Gain0: {BitConverter.ToString(bCorGain)}");
#endif
            }

            // CH1_R = (1- ( R(H)Lv / R(R)Lv / Rht)) * Rr
            ch1_r = (1 - (measureHighData[HIGH][RED] / measureHighData[REGULAR][RED] / GammaGain._GainAdjustmentCoef.TargetH.Rht)) * GammaGain._GainAdjustmentCoef.CorrectionFactorR.Rr;
#if DEBUG
            Console.WriteLine($"CH1_R:{ch1_r} = ( 1 - R(H)Lv:{measureHighData[HIGH][RED]} / R(R)Lv:{measureHighData[REGULAR][RED]} / Rht:{GammaGain._GainAdjustmentCoef.TargetH.Rht} ) * Rr:{GammaGain._GainAdjustmentCoef.CorrectionFactorR.Rr}");
            SaveExecLog($"       CH1_R: {ch1_r}");
#elif Release_log
            SaveExecLog($"       CH1_R: {ch1_r}");
#endif

            // CH1_G = (1- ( G(H)Lv / G(R)Lv / Ght)) * Rg
            ch1_g = (1 - (measureHighData[HIGH][GREEN] / measureHighData[REGULAR][GREEN] / GammaGain._GainAdjustmentCoef.TargetH.Ght)) * GammaGain._GainAdjustmentCoef.CorrectionFactorR.Rg;
#if DEBUG
            Console.WriteLine($"CH1_G:{ch1_g} = ( 1 - G(H)Lv:{measureHighData[HIGH][GREEN]} / G(R)Lv:{measureHighData[REGULAR][GREEN]} / Ght:{GammaGain._GainAdjustmentCoef.TargetH.Ght} ) * Rg:{GammaGain._GainAdjustmentCoef.CorrectionFactorR.Rg}");
            SaveExecLog($"       CH1_G: {ch1_g}");
#elif Release_log
            SaveExecLog($"       CH1_G: {ch1_g}");
#endif

            // CH1_B = (1- ( B(H)Lv / B(R)Lv / Bht)) * Rb
            ch1_b = (1 - (measureHighData[HIGH][BLUE] / measureHighData[REGULAR][BLUE] / GammaGain._GainAdjustmentCoef.TargetH.Bht)) * GammaGain._GainAdjustmentCoef.CorrectionFactorR.Rb;
#if DEBUG
            Console.WriteLine($"CH1_B:{ch1_b} = ( 1 - B(H)Lv:{measureHighData[HIGH][BLUE]} / B(R)Lv:{measureHighData[REGULAR][BLUE]} / Bht:{GammaGain._GainAdjustmentCoef.TargetH.Bht} ) * Rb:{GammaGain._GainAdjustmentCoef.CorrectionFactorR.Rb}");
            SaveExecLog($"       CH1_B: {ch1_b}");
#elif Release_log
            SaveExecLog($"       CH1_B: {ch1_b}");
#endif

            if (!makeCorGain(ch1_r, ch1_g, ch1_b, out bCorGain))
            {
                ShowMessageWindow("High Cor Gain1 value is out of valid range.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }

            lstHightCorGain1.Add(bCorGain);
#if DEBUG
            Console.WriteLine($"High Cor Gain1: {BitConverter.ToString(bCorGain)}");
            SaveExecLog($"       High Cor Gain1: {BitConverter.ToString(bCorGain)}");
#elif Release_log
            SaveExecLog($"       High Cor Gain1: {BitConverter.ToString(bCorGain)}");
#endif

            // CH2_R = (1- ( R(H)Lv / R(R)Lv / Rht)) * (1 - Rr)
            ch2_r = (1 - (measureHighData[HIGH][RED] / measureHighData[REGULAR][RED] / GammaGain._GainAdjustmentCoef.TargetH.Rht)) * (1 - GammaGain._GainAdjustmentCoef.CorrectionFactorR.Rr);
#if DEBUG
            Console.WriteLine($"CH2_R:{ch2_r} = ( 1 - R(H)Lv:{measureHighData[HIGH][RED]} / R(R)Lv:{measureHighData[REGULAR][RED]} / Rht:{GammaGain._GainAdjustmentCoef.TargetH.Rht} ) * ( 1 - Rr:{GammaGain._GainAdjustmentCoef.CorrectionFactorR.Rr} )");
            SaveExecLog($"       CH2_R: {ch2_r}");
#elif Release_log
            SaveExecLog($"       CH2_R: {ch2_r}");
#endif

            // CH2_G = (1- ( G(H)Lv / G(R)Lv / Ght)) * (1 - Rg)
            ch2_g = (1 - (measureHighData[HIGH][GREEN] / measureHighData[REGULAR][GREEN] / GammaGain._GainAdjustmentCoef.TargetH.Ght)) * (1 - GammaGain._GainAdjustmentCoef.CorrectionFactorR.Rg);
#if DEBUG
            Console.WriteLine($"CH2_G:{ch2_g} = ( 1 - G(H)Lv:{measureHighData[HIGH][GREEN]} / G(R)Lv:{measureHighData[REGULAR][GREEN]} / Ght:{GammaGain._GainAdjustmentCoef.TargetH.Ght} ) * ( 1 - Rg:{GammaGain._GainAdjustmentCoef.CorrectionFactorR.Rg} )");
            SaveExecLog($"       CH2_G: {ch2_g}");
#elif Release_log
            SaveExecLog($"       CH2_G: {ch2_g}");
#endif

            // CH2_B = (1- ( B(H)Lv / B(R)Lv / Bht)) * (1 - Rb)
            ch2_b = (1 - (measureHighData[HIGH][BLUE] / measureHighData[REGULAR][BLUE] / GammaGain._GainAdjustmentCoef.TargetH.Bht)) * (1 - GammaGain._GainAdjustmentCoef.CorrectionFactorR.Rb);
#if DEBUG
            Console.WriteLine($"CH2_B:{ch2_b} = ( 1 - B(H)Lv:{measureHighData[HIGH][BLUE]} / B(R)Lv:{measureHighData[REGULAR][BLUE]} / Bht:{GammaGain._GainAdjustmentCoef.TargetH.Bht} ) * ( 1 - Rb:{GammaGain._GainAdjustmentCoef.CorrectionFactorR.Rb} )");
            SaveExecLog($"       CH2_B: {ch2_b}");
#elif Release_log
            SaveExecLog($"       CH2_B: {ch2_b}");
#endif

            if (!makeCorGain(ch2_r, ch2_g, ch2_b, out bCorGain))
            {
                ShowMessageWindow("High Cor Gain2 value is out of valid range.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }

            lstHightCorGain2.Add(bCorGain);
#if DEBUG
            Console.WriteLine($"High Cor Gain2: {BitConverter.ToString(bCorGain)}");
            SaveExecLog($"       High Cor Gain2: {BitConverter.ToString(bCorGain)}");
#elif Release_log
            SaveExecLog($"       High Cor Gain2: {BitConverter.ToString(bCorGain)}");
#endif

            if (!makeCorGain(0, 0, 0, out bCorGain))
            {
                ShowMessageWindow("High Cor Gain3 value is out of valid range.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }

            lstHightCorGain3.Add(bCorGain);

            return true;
        }

        /// <summary>
        /// クロストーク補正量算出
        /// </summary>
        /// <param name="measureLowData">測定データが格納された配列[(0.05%) r/g/b/w][X/Y/Z]</param>
        /// <param name="CTC_Y"></param>
        /// <param name="CTC_x"></param>
        /// <param name="CTC_y"></param>
        /// <returns></returns>
        private bool calcMgamCrosstalk(double[][] measureLowData, out double CTC_Y, out double CTC_x, out double CTC_y)
        {
            CTC_Y = double.NaN;
            CTC_x = double.NaN;
            CTC_y = double.NaN;

            try
            {
                // X/Y/Z/x/y値を取得
                // RGBX = RX + GX + BX
                double RGBX = measureLowData[RED][XYZ_X] + measureLowData[GREEN][XYZ_X] + measureLowData[BLUE][XYZ_X];
#if DEBUG
                Console.WriteLine($"RGBX:{RGBX} = RX:{measureLowData[RED][XYZ_X]} + GX:{measureLowData[GREEN][XYZ_X]} + BX:{measureLowData[BLUE][XYZ_X]}");
#endif

                // RGBY = RY + GY + BY
                double RGBY = measureLowData[RED][XYZ_Y] + measureLowData[GREEN][XYZ_Y] + measureLowData[BLUE][XYZ_Y];
#if DEBUG
                Console.WriteLine($"RGBY:{RGBY} = RY:{measureLowData[RED][XYZ_Y]} + GY:{measureLowData[GREEN][XYZ_Y]} + BY:{measureLowData[BLUE][XYZ_Y]}");
#endif

                // RGBZ = RZ + GZ + BZ
                double RGBZ = measureLowData[RED][XYZ_Z] + measureLowData[GREEN][XYZ_Z] + measureLowData[BLUE][XYZ_Z];
#if DEBUG
                Console.WriteLine($"RGBZ:{RGBZ} = RZ:{measureLowData[RED][XYZ_Z]} + GZ:{measureLowData[GREEN][XYZ_Z]} + BZ:{measureLowData[BLUE][XYZ_Z]}");
#endif

                // RGBx = RGBX / (RGBX + RGBY + RGBZ)
                double RGBx = RGBX / (RGBX + RGBY + RGBZ);
#if DEBUG
                Console.WriteLine($"RGBx:{RGBx} = RGBX:{RGBX} / ( RGBX:{RGBX} + RGBY:{RGBY} + RGBZ:{RGBZ} )");
#endif

                // RGBy = RGBY / (RGBX + RGBY + RGBZ)
                double RGBy = RGBY / (RGBX + RGBY + RGBZ);
#if DEBUG
                Console.WriteLine($"RGBy:{RGBy} = RGBY:{RGBY} / ( RGBX:{RGBX} + RGBY:{RGBY} + RGBZ:{RGBZ} )");
#endif

                // クロストーク算出
                // CrosstalkY(Yct) = WY / RGBY
                double Yct = measureLowData[WHITE][XYZ_Y] / RGBY;
#if DEBUG
                Console.WriteLine($"Yct:{Yct} = WY:{measureLowData[WHITE][XYZ_Y]} / RGBY:{RGBY}");
#endif

                // Wx = WX / (WX + WY + WZ)
                double Wx = measureLowData[WHITE][XYZ_X] / (measureLowData[WHITE][XYZ_X] + measureLowData[WHITE][XYZ_Y] + measureLowData[WHITE][XYZ_Z]);
#if DEBUG
                Console.WriteLine($"Wx:{Wx} = WX:{measureLowData[WHITE][XYZ_X]} / ( WX:{measureLowData[WHITE][XYZ_X]} + WY:{measureLowData[WHITE][XYZ_Y]} + WZ:{measureLowData[WHITE][XYZ_Z]} )");
#endif

                // Crosstalkx(xct) = Wx - RGBx
                double xct = Wx - RGBx;
#if DEBUG
                Console.WriteLine($"xct:{xct} = Wx:{Wx} - RGBx:{RGBx}");
#endif

                // Wy = WY / (WX + WY + WZ)
                double Wy = measureLowData[WHITE][XYZ_Y] / (measureLowData[WHITE][XYZ_X] + measureLowData[WHITE][XYZ_Y] + measureLowData[WHITE][XYZ_Z]);
#if DEBUG
                Console.WriteLine($"Wy:{Wy} = WY:{measureLowData[WHITE][XYZ_Y]} / ( WX:{measureLowData[WHITE][XYZ_X]} + WY:{measureLowData[WHITE][XYZ_Y]} + WZ:{measureLowData[WHITE][XYZ_Z]} )");
#endif

                // Crosstalky(yct) = Wy - RGBy
                double yct = Wy - RGBy;
#if DEBUG
                Console.WriteLine($"yct:{yct} = Wy:{Wy} - RGBy:{RGBy}");
#endif

                // クロストーク補正量算出(CrossTalkCorrecction)
                // CTC_Y = Yctt - Yct
                CTC_Y = CrossTalk._CrossTalkTarget.Yctt - Yct;
#if DEBUG
                Console.WriteLine($"CTC_Y:{CTC_Y} = Yctt:{CrossTalk._CrossTalkTarget.Yctt} - Yct:{Yct}");
#endif

                // CTC_x = xctt - xct
                CTC_x = CrossTalk._CrossTalkTarget.xctt - xct;
#if DEBUG
                Console.WriteLine($"CTC_x:{CTC_x} = xctt:{CrossTalk._CrossTalkTarget.xctt} - xct:{xct}");
#endif
                // CTC_y = yctt - yct
                CTC_y = CrossTalk._CrossTalkTarget.yctt - yct;
#if DEBUG
                Console.WriteLine($"CTC_y:{CTC_y} = yctt:{CrossTalk._CrossTalkTarget.yctt} - yct:{yct}");
#endif
            }
            catch
            { return false; }

            return true;
        }

        /// <summary>
        /// Cor Gainデータを作成
        /// </summary>
        /// <param name="dGainR">Redの算出値</param>
        /// <param name="dGainG">Greenの算出値</param>
        /// <param name="dGainB">Blueの算出値</param>
        /// <returns>R/G/BのDoubleデータを1つのByte配列に圧縮されたデータ</returns>
        private bool makeCorGain(double dGainR, double dGainG, double dGainB, out byte[] bCorGain)
        {
            int iGainR, iGainG, iGainB;
            byte[] bGainR, bGainG, bGainB;
            bCorGain = new byte[12];

            if (!calcCorGain(dGainR * 100, out iGainR)) // 単位をパーセンテージに変更するため100倍する
            {
#if DEBUG
                Console.WriteLine($"Value is out of valid range.\r\nCor R: {dGainR * 100}");
#endif
                return false;
            }

            if (!calcCorGain(dGainG * 100, out iGainG))
            {
#if DEBUG
                Console.WriteLine($"Value is out of valid range.\r\nCor G: {dGainG * 100}");
#endif
                return false;
            }

            if (!calcCorGain(dGainB * 100, out iGainB))
            {
#if DEBUG
                Console.WriteLine($"Value is out of valid range.\r\nCor B: {dGainB * 100}");
#endif
                return false;
            }

            bGainR = BitConverter.GetBytes(iGainR);
            Array.Copy(bGainR, 0, bCorGain, 0, 4);

            bGainG = BitConverter.GetBytes(iGainG);
            Array.Copy(bGainG, 0, bCorGain, 4, 4);

            bGainB = BitConverter.GetBytes(iGainB);
            Array.Copy(bGainB, 0, bCorGain, 8, 4);

            return true;
        }

        /// <summary>
        /// 測定データから算出(計算式はMGAM_コマンド_240329.xlsm参照)
        /// </summary>
        /// <param name="value">算出値</param>
        /// <param name="_value">Int型変換値</returns>
        private bool calcCorGain(double value, out int _value)
        {
            _value = 0;

            if (value < COR_GAIN_MIN_VALUE || value > COR_GAIN_MAX_VALUE)
            {
                return false;
            }

            double result = Math.Round(value / 100 * 1024);
            if (result < INT_ZERO)
            {
                _value = 256 + Convert.ToInt32("FFFFFF00", 16) + Convert.ToInt32(result);
            }
            else
            {
                _value = Convert.ToInt32(result);
            }

            return true;
        }

        /// <summary>
        /// Threshold Levelパラメーターを作成
        /// </summary>
        private void makeThresholdLevel()
        {
            byte[] bThreshold;
            int dataSize = 4;
            int idx = 0;

            // Low Threshold 0 Level R/G/B
            bThreshold = BitConverter.GetBytes(GammaGain._Threshold.LowThresholdLevel0.Red);
            Array.Copy(bThreshold, 0, _gammaGainThreshold, idx, dataSize);
            bThreshold = BitConverter.GetBytes(GammaGain._Threshold.LowThresholdLevel0.Green);
            Array.Copy(bThreshold, 0, _gammaGainThreshold, idx += dataSize, dataSize);
            bThreshold = BitConverter.GetBytes(GammaGain._Threshold.LowThresholdLevel0.Blue);
            Array.Copy(bThreshold, 0, _gammaGainThreshold, idx += dataSize, dataSize);
#if DEBUG
            Console.WriteLine($"Low Threshold R:{GammaGain._Threshold.LowThresholdLevel0.Red}");
            Console.WriteLine($"Low Threshold G:{GammaGain._Threshold.LowThresholdLevel0.Green}");
            Console.WriteLine($"Low Threshold B:{GammaGain._Threshold.LowThresholdLevel0.Blue}");
#endif

            // High Threshold 1 Level R/G/B
            bThreshold = BitConverter.GetBytes(GammaGain._Threshold.HighThresholdLevel1.Red);
            Array.Copy(bThreshold, 0, _gammaGainThreshold, idx += dataSize, dataSize);
            bThreshold = BitConverter.GetBytes(GammaGain._Threshold.HighThresholdLevel1.Green);
            Array.Copy(bThreshold, 0, _gammaGainThreshold, idx += dataSize, dataSize);
            bThreshold = BitConverter.GetBytes(GammaGain._Threshold.HighThresholdLevel1.Blue);
            Array.Copy(bThreshold, 0, _gammaGainThreshold, idx += dataSize, dataSize);
#if DEBUG
            Console.WriteLine($"High Threshold R:{GammaGain._Threshold.HighThresholdLevel1.Red}");
            Console.WriteLine($"High Threshold G:{GammaGain._Threshold.HighThresholdLevel1.Green}");
            Console.WriteLine($"High Threshold B:{GammaGain._Threshold.HighThresholdLevel1.Blue}");
#endif

            // High Threshold 2 Level R/G/B
            bThreshold = BitConverter.GetBytes(GammaGain._Threshold.HighThresholdLevel2.Red);
            Array.Copy(bThreshold, 0, _gammaGainThreshold, idx += dataSize, dataSize);
            bThreshold = BitConverter.GetBytes(GammaGain._Threshold.HighThresholdLevel2.Green);
            Array.Copy(bThreshold, 0, _gammaGainThreshold, idx += dataSize, dataSize);
            bThreshold = BitConverter.GetBytes(GammaGain._Threshold.HighThresholdLevel2.Blue);
            Array.Copy(bThreshold, 0, _gammaGainThreshold, idx += dataSize, dataSize);
#if DEBUG
            Console.WriteLine($"High Threshold R:{GammaGain._Threshold.HighThresholdLevel2.Red}");
            Console.WriteLine($"High Threshold G:{GammaGain._Threshold.HighThresholdLevel2.Green}");
            Console.WriteLine($"High Threshold B:{GammaGain._Threshold.HighThresholdLevel2.Blue}");
#endif

            // High Threshold 3 Level R/G/B
            bThreshold = BitConverter.GetBytes(GammaGain._Threshold.HighThresholdLevel3.Red);
            Array.Copy(bThreshold, 0, _gammaGainThreshold, idx += dataSize, dataSize);
            bThreshold = BitConverter.GetBytes(GammaGain._Threshold.HighThresholdLevel3.Green);
            Array.Copy(bThreshold, 0, _gammaGainThreshold, idx += dataSize, dataSize);
            bThreshold = BitConverter.GetBytes(GammaGain._Threshold.HighThresholdLevel3.Blue);
            Array.Copy(bThreshold, 0, _gammaGainThreshold, idx += dataSize, dataSize);
#if DEBUG
            Console.WriteLine($"High Threshold R:{GammaGain._Threshold.HighThresholdLevel3.Red}");
            Console.WriteLine($"High Threshold G:{GammaGain._Threshold.HighThresholdLevel3.Green}");
            Console.WriteLine($"High Threshold B:{GammaGain._Threshold.HighThresholdLevel3.Blue}");
#endif

            // High Threshold 4 Level R/G/B
            bThreshold = BitConverter.GetBytes(GammaGain._Threshold.HighThresholdLevel4.Red);
            Array.Copy(bThreshold, 0, _gammaGainThreshold, idx += dataSize, dataSize);
            bThreshold = BitConverter.GetBytes(GammaGain._Threshold.HighThresholdLevel4.Green);
            Array.Copy(bThreshold, 0, _gammaGainThreshold, idx += dataSize, dataSize);
            bThreshold = BitConverter.GetBytes(GammaGain._Threshold.HighThresholdLevel4.Blue);
            Array.Copy(bThreshold, 0, _gammaGainThreshold, idx += dataSize, dataSize);
#if DEBUG
            Console.WriteLine($"High Threshold R:{GammaGain._Threshold.HighThresholdLevel4.Red}");
            Console.WriteLine($"High Threshold G:{GammaGain._Threshold.HighThresholdLevel4.Green}");
            Console.WriteLine($"High Threshold B:{GammaGain._Threshold.HighThresholdLevel4.Blue}");
#endif
        }

        /// <summary>
        /// New MGAMパラメーターを設定したlc.binファイルを生成
        /// </summary>
        /// <param name="unit">対象キャビネット</param>
        /// <param name="lstTargetCell">対象モジュール番号が格納されたリスト</param>
        private void overwriteMgamData(UnitInfo unit, List<int> lstTargetCell)
        {
            int lcFirstLmgamModuleDataAddress;
            int lcLmgamDataCrcOffset = 16;
            int lcLmgamHeaderCrcOffset = 28;
            int lcLmgamDataOffset = 32;
            int lcLmgamValidDataIndicatorOffset = 44;
            int lcLmgamLowThreshold0Offset = 48;
            int lcLmgamLowCorGain0Offset = 108;
            int lcLmgamHighCorGain1Offset = 120;
            int lcLmgamHighCorGain2Offset = 132;
            int lcLmgamHighCorGain3Offset = 144;
            int commonDataLength = 4;
            int lcCorGainDataLength = 12;
            int lcLmgamHeaderCrcDataLength = 28;
            int lcLmgamDataLength = 128;

            if (allocInfo.LEDModel == ZRD_BH12D
                || allocInfo.LEDModel == ZRD_CH12D
                || allocInfo.LEDModel == ZRD_BH12D_S3
                || allocInfo.LEDModel == ZRD_CH12D_S3)
            {
                lcFirstLmgamModuleDataAddress = CommonHeaderLength + ProductInfoLength 
                    + (LcLcalVModuleDataLengthP12_Module4x3 * moduleCount)
                    + (LcLcalHModuleDataLengthP12_Module4x3 * moduleCount)
                    + (LcLmcalModuleDataLengthP12_Module4x3 * moduleCount);
            }
            else
            { 
                lcFirstLmgamModuleDataAddress = CommonHeaderLength + ProductInfoLength 
                    + (LcLcalVModuleDataLengthP15_Module4x3 * moduleCount) 
                    + (LcLcalHModuleDataLengthP15_Module4x3 * moduleCount) 
                    + (LcLmcalModuleDataLengthP15_Module4x3 * moduleCount); 
            }

            int lcFirstLmgamHeaderCrcAddress = lcFirstLmgamModuleDataAddress + lcLmgamHeaderCrcOffset;
            int lcFirstLmgamDataCrcAddress = lcFirstLmgamModuleDataAddress + lcLmgamDataCrcOffset;
            int lcFirstLmgamDataAddress = lcFirstLmgamModuleDataAddress + lcLmgamDataOffset;
            int lcFirstLmgamValidDataIndicatorAddress = lcFirstLmgamModuleDataAddress + lcLmgamValidDataIndicatorOffset;
            int lcFirstLmgamThreshold0Address = lcFirstLmgamModuleDataAddress + lcLmgamLowThreshold0Offset;
            int lcFirstLmgamLowCorGain0Address = lcFirstLmgamModuleDataAddress + lcLmgamLowCorGain0Offset;
            int lcFirstLmgamHighCorGain1Address = lcFirstLmgamModuleDataAddress + lcLmgamHighCorGain1Offset;
            int lcFirstLmgamHighCorGain2Address = lcFirstLmgamModuleDataAddress + lcLmgamHighCorGain2Offset;
            int lcFirstLmgamHighCorGain3Address = lcFirstLmgamModuleDataAddress + lcLmgamHighCorGain3Offset;

            string srcfile = makeFilePath(unit, FileDirectory.Backup_Latest, DataType.LcData);

            byte[] crc, tempBytes;
            byte[] srcBytes = System.IO.File.ReadAllBytes(srcfile);
            byte[] convBytes = new byte[srcBytes.Length];
            Array.Copy(srcBytes, convBytes, srcBytes.Length);

            for (int i = 0; i < lstTargetCell.Count; i++)
            {
                if (Dispatcher.Invoke(() => rbGgAllModule.IsChecked) == true) // Adjustment Mode
                {
                    if (Dispatcher.Invoke(() => rbWriteNotAdjustedModules.IsChecked) == true) // Data Writing Option
                    {
                        // Check Valid Data Indicator
                        tempBytes = new byte[commonDataLength];
                        Array.Copy(convBytes, lcFirstLmgamValidDataIndicatorAddress + (lcMgamModuleDataLength * lstTargetCell[i]), tempBytes, 0, commonDataLength);

                        int vdi = BitConverter.ToInt32(tempBytes, 0);
                        if (vdi == INT_ZERO || vdi == INT_FF) // INT_ZERO: Blank Data
                        {
                            // Do noting
                        }
                        else
                        {
                            lstAdjustedModule.Add(i);
                            continue;
                        }
                    }
                }

                // Valid Data Indicator
                byte[] dataIndicator = BitConverter.GetBytes(INT_ONE); // INT_ONE: Valid Data
                Array.Copy(dataIndicator, 0, convBytes, lcFirstLmgamValidDataIndicatorAddress + (lcMgamModuleDataLength * lstTargetCell[i]), commonDataLength);

                // Threshold 0~4 Level R/G/B
                Array.Copy(_gammaGainThreshold, 0, convBytes, lcFirstLmgamThreshold0Address + (lcMgamModuleDataLength * lstTargetCell[i]), _gammaGainThreshold.Length);

                if (Settings.Ins.GammaGain.GainMeasurementMode == GainMeasurementMode.Full)
                {
                    // Low-Cor Gain0 R/G/B
                    Array.Copy(lstLowCorGain0[i], 0, convBytes, lcFirstLmgamLowCorGain0Address + (lcMgamModuleDataLength * lstTargetCell[i]), lcCorGainDataLength);
                }

                // High-Cor Gain1 R/G/B
                Array.Copy(lstHightCorGain1[i], 0, convBytes, lcFirstLmgamHighCorGain1Address + (lcMgamModuleDataLength * lstTargetCell[i]), lcCorGainDataLength);

                // High-Cor Gain2 R/G/B
                Array.Copy(lstHightCorGain2[i], 0, convBytes, lcFirstLmgamHighCorGain2Address + (lcMgamModuleDataLength * lstTargetCell[i]), lcCorGainDataLength);

                // High-Cor Gain3 R/G/B
                Array.Copy(lstHightCorGain3[i], 0, convBytes, lcFirstLmgamHighCorGain3Address + (lcMgamModuleDataLength * lstTargetCell[i]), lcCorGainDataLength);

                // Data CRC
                tempBytes = new byte[lcLmgamDataLength];
                Array.Copy(convBytes, lcFirstLmgamDataAddress + (lcMgamModuleDataLength * lstTargetCell[i]), tempBytes, 0, lcLmgamDataLength);

                CalcCrc(tempBytes, out crc);
                Array.Copy(crc, 0, convBytes, lcFirstLmgamDataCrcAddress + (lcMgamModuleDataLength * lstTargetCell[i]), commonDataLength);

                // Header CRC
                tempBytes = new byte[lcLmgamHeaderCrcDataLength];
                Array.Copy(convBytes, lcFirstLmgamModuleDataAddress + (lcMgamModuleDataLength * lstTargetCell[i]), tempBytes, 0, lcLmgamHeaderCrcDataLength);

                CalcCrc(tempBytes, out crc);
                Array.Copy(crc, 0, convBytes, lcFirstLmgamHeaderCrcAddress + (lcMgamModuleDataLength * lstTargetCell[i]), commonDataLength);
            }

            // 全体のCRC再計算
            byte[] dataBytes = new byte[lcDataLength];
            Array.Copy(convBytes, ProductInfoAddress, dataBytes, 0, lcDataLength);

            CalcCrc(dataBytes, out crc);
            Array.Copy(crc, 0, convBytes, CommonHeaderDataCrcOffset, 4);

            // HeaderのCRC再計算
            dataBytes = new byte[CommonHeaderHeaderCrcRange];
            Array.Copy(convBytes, 0, dataBytes, 0, CommonHeaderHeaderCrcRange);

            CalcCrc(dataBytes, out crc);
            Array.Copy(crc, 0, convBytes, CommonHeaderHeaderCrcOffset, CommonHeaderHeaderCrcSize);

            // lc.binファイルを書き出し
            string destFile = makeFilePath(unit, FileDirectory.Temp, DataType.LcData);
            System.IO.File.WriteAllBytes(destFile, convBytes);
        }

        /// <summary>
        /// New CTC(Low) R/G/B補正量を設定したhc.binファイルを生成
        /// </summary>
        /// <param name="unit">対象キャビネット</param>
        /// <param name="lstTargetCell">対象モジュール番号が格納されたリスト</param>
        private void overwriteColorCorrectionData(UnitInfo unit, List<int> lstTargetCell)
        {
            int commonLength = 4;
            int hcCcDataCrcOffset = 16;
            int hcHeaderCrcLength = 28;
            int hcHeaderCrcOffset = 28;
            int hcCcDataOffset = 32;
            int hcCtcVdiOffset = 80;
            int hcCtcLowRedOffset = 96;
            int hcCtcLowGreenOffset = 100;
            int hcCtcLowBlueOffset = 104;

            int hcFirstCcAddress = CommonHeaderLength + ProductInfoLength;

            int hcFirstCcHeaderCrcAddress = hcFirstCcAddress + hcHeaderCrcOffset;
            int hcFirstCcDataCrcAddress = hcFirstCcAddress + hcCcDataCrcOffset;
            int hcFirstCcDataAddress = hcFirstCcAddress + hcCcDataOffset;
            int hcFirstCtcVdiAddress = hcFirstCcAddress + hcCtcVdiOffset;
            int hcFirstCtcLowRedAddress = hcFirstCcAddress + hcCtcLowRedOffset;
            int hcFirstCtcLowGreenAddress = hcFirstCcAddress + hcCtcLowGreenOffset;
            int hcFirstCtcLowBlueAddress = hcFirstCcAddress + hcCtcLowBlueOffset;

            byte[] crc, tempBytes;

            string srcFile = makeFilePath(unit, FileDirectory.Backup_Latest, DataType.HcData);
            byte[] srcBytes = System.IO.File.ReadAllBytes(srcFile);
            byte[] convBytes = new byte[srcBytes.Length];
            Array.Copy(srcBytes, convBytes, srcBytes.Length);

            for (int i = 0; i < lstTargetCell.Count; i++)
            {
                if (Dispatcher.Invoke(() => rbGgAllModule.IsChecked) == true) // Adjustment Mode
                {
                    if (Dispatcher.Invoke(() => rbWriteNotAdjustedModules.IsChecked) == true) // Data Writing Option
                    {
                        if (lstAdjustedModule.Contains(i))
                        { continue; }
                    }
                }

                // Crosstalk Correction Data Valid Indicator
                tempBytes = new byte[4];
                Array.Copy(convBytes, hcFirstCtcVdiAddress + (hcModuleDataLength * lstTargetCell[i]), tempBytes, 0, tempBytes.Length);

                tempBytes[0] = (byte)((tempBytes[0] & ~(tempBytes[0] & 0x0F)) | 0x01 >> 0); // 下位4bitを1に設定
                Array.Copy(tempBytes, 0, convBytes, hcFirstCtcVdiAddress + (hcModuleDataLength * lstTargetCell[i]), tempBytes.Length);

                // Crosstalk Correction (Low) R
                tempBytes = new byte[commonLength];
                tempBytes = BitConverter.GetBytes(CrosstalkCorrections[lstTargetCell[i]][RED]);
                Array.Copy(tempBytes, 0, convBytes, hcFirstCtcLowRedAddress + (hcModuleDataLength * lstTargetCell[i]), tempBytes.Length);

                // Crosstalk Correction (Low) G
                tempBytes = BitConverter.GetBytes(CrosstalkCorrections[lstTargetCell[i]][GREEN]);
                Array.Copy(tempBytes, 0, convBytes, hcFirstCtcLowGreenAddress + (hcModuleDataLength * lstTargetCell[i]), tempBytes.Length);

                // Crosstalk Correction (Low) B
                tempBytes = BitConverter.GetBytes(CrosstalkCorrections[lstTargetCell[i]][BLUE]);
                Array.Copy(tempBytes, 0, convBytes, hcFirstCtcLowBlueAddress + (hcModuleDataLength * lstTargetCell[i]), tempBytes.Length);

                // Data CRC
                tempBytes = new byte[hcCcDataLength];
                Array.Copy(convBytes, hcFirstCcDataAddress + (hcModuleDataLength * lstTargetCell[i]), tempBytes, 0, tempBytes.Length);

                CalcCrc(tempBytes, out crc);
                Array.Copy(crc, 0, convBytes, hcFirstCcDataCrcAddress + (hcModuleDataLength * lstTargetCell[i]), crc.Length);

                // Header CRC
                tempBytes = new byte[hcHeaderCrcLength];
                Array.Copy(convBytes, hcFirstCcAddress + (hcModuleDataLength * lstTargetCell[i]), tempBytes, 0, tempBytes.Length);

                CalcCrc(tempBytes, out crc);
                Array.Copy(crc, 0, convBytes, hcFirstCcHeaderCrcAddress + (hcModuleDataLength * lstTargetCell[i]), crc.Length);
            }

            // 全体のCRC再計算
            tempBytes = new byte[hcDataLength];
            Array.Copy(convBytes, ProductInfoAddress, tempBytes, 0, tempBytes.Length);

            CalcCrc(tempBytes, out crc);
            Array.Copy(crc, 0, convBytes, CommonHeaderDataCrcOffset, crc.Length);

            // HeaderのCRC再計算
            tempBytes = new byte[hcHeaderCrcLength];
            Array.Copy(convBytes, 0, tempBytes, 0, tempBytes.Length);

            CalcCrc(tempBytes, out crc);
            Array.Copy(crc, 0, convBytes, CommonHeaderHeaderCrcOffset, crc.Length);

            // hc.binファイルを書き出し
            string destFile = makeFilePath(unit, FileDirectory.Temp, DataType.HcData);
            System.IO.File.WriteAllBytes(destFile, convBytes);
        }

        /// <summary>
        /// CA-410から測定した輝度値取得
        /// </summary>
        /// <param name="lv"></param>
        /// <param name="probeNo"></param>
        /// <returns></returns>
        private bool getProbeLvSub(out Double lv, Int32 probeNo)
        {
            if (caSdk.isOpened() != true)
            {
                showStatusTextBlock("Device is not opened");
                lv = double.NaN;
                return false;
            }

            bool status = caSdk.getProbeLv(out lv, probeNo);
            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }

            return status;
        }

        /// <summary>
        /// FTPの設定をOFF
        /// </summary>
        private bool setFtpOff()
        {
            foreach (ControllerInfo controller in dicController.Values)
            {
                if (controller.Target)
                {
                    if (!sendSdcpCommand(SDCPClass.CmdFtpOff, controller.IPAddress))
                    { return false; }
                }
            }
            return true;
        }

        /// <summary>
        /// Signal Mode 1の設定戻す
        /// </summary>
        private bool setDefaultSignalModeOne()
        {
            foreach (ControllerInfo controller in dicController.Values)
            {
                if (controller.Target)
                { setSignalModeOne(controller); }
            }

            return true;
        }

        /// <summary>
        /// Controller単位設定：Signal Mode 1
        /// </summary>
        private bool setSignalModeOne(ControllerInfo controller)
        {
            byte[] cmd = new byte[SDCPClass.CmdSigModeOneSet.Length];
            Array.Copy(SDCPClass.CmdSigModeOneSet, cmd, SDCPClass.CmdSigModeOneSet.Length);

            cmd[20] = SIG_MODE_ONE_DEFAULT_DATA;
            if (!sendSdcpCommand(cmd, out string buff, controller.IPAddress))
            { return false; }

            return true;
        }

        /// <summary>
        /// コントローラーにアップロードに必要な時間
        /// </summary>
        /// <param name="cabinetCount"></param>
        /// <returns></returns>
        private int calcAdjustGainMoveSec(int cabinetCount)
        {
            return (int)(cabinetCount * RESTORE_LC_FILE_MOVE_SEC);
        }

        /// <summary>
        /// コントローラーに書き込み完了まで必要な時間
        /// </summary>
        /// <param name="lcFileCount"></param>
        /// <returns></returns>
        private int calcAdjustGainResponseSec(int lcFileCount)
        {
            return (int)((lcFileCount * (RESTORE_LC_FILE_RESPONSE_SEC + (lcFileCount * RESTORE_FILE_SUPPLEMENT_SEC))) + RESTORE_LC_FILE_RESPONSE_FIX_SEC + RESTORE_FILE_RESPONSE_FIX_SEC);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="step"></param>
        /// <param name="controllerCount"></param>
        /// <param name="dataMoveSec"></param>
        /// <param name="responseSec"></param>
        /// <returns></returns>
        private int calcAdjustGainProcessSec(int step, int controllerCount, int dataMoveSec, int responseSec)
        {
            int processSec = 0;
            //係数　データ移動,レスポンス処理,コントローラー台数に影響する時間を除いた合計処理時間
            //commonA[0] [15] Test Patterm Off.
            //           [16] Move Adjusted Files.
            //commonA[1] [17] Cabinet PowerPower Off.
            //commonA[2] [18] Send Reconfig.
            //commonA[3] [19] Send Write Command.
            //           [20] Waiting for the process of controller.
            //commonA[4] [21] Set Normal Setting.
            //commonA[5] [22] Restore User Settings.
            //commonA[6] [23] Move Latest to Previous.
            //commonA[7] [24] Move Temp to Latest.
            //commonA[8] [25] Send Reconfig.
            //commonA[9] [26] Delete Temp Files.
            //commonA[10][27] Unselect All Cabinets.
            //commonA[11][28] FTP Off.
            //commonA[12][29] Default Signal Mode.
            int[] commonA = new int[13] { 0, 5, 16, 1, 1, 2, 0, 0, 14, 0, 0, 1, 2 };

            //係数　コントローラー台数に影響のある各種処理時間
            //contB[0] [15] Test Patterm Off.
            //         [16] Move Adjusted Files.
            //contB[1] [17] Cabinet PowerPower Off.
            //contB[2] [18] Send Reconfig.
            //contB[3] [19] Send Write Command.
            //         [20] Waiting for the process ofoller.
            //contB[4] [21] Set Normal Setting.
            //contB[5] [22] Restore User Settings.
            //contB[6] [23] Move Latest to Previous.
            //contB[7] [24] Move Temp to Latest.
            //contB[8] [25] Send Reconfig.
            //contB[9] [26] Delete Temp Files.
            //contB[10][27] Unselect All Cabinets.
            //contB[11][28] FTP Off.
            //contB[12][29] Default Signal Mode.
            int[] contB = new int[13] { 0, 2, 2, 1, 1, 2, 0, 0, 2, 0, 0, 1, 2 };

            switch (step)
            {
                case 15:
                    processSec = commonA.Skip(step - 14).Sum() + (controllerCount * contB.Skip(step - 14).Sum()) + responseSec + dataMoveSec;
                    break;
                case 16: //[16] Move Adjusted Files. 以下の処理時間全体を計算
                case 17: //[17] Cabinet Power Off.
                case 18: //[18] Send Reconfig.
                case 19: //[19] Send Write Command.
                    processSec = commonA.Skip(step - 15).Sum() + (controllerCount * contB.Skip(step - 15).Sum()) + responseSec;
                    break;
                case 20: //[20] Waiting for the process of controller.
                    processSec = commonA.Skip(step - 15).Sum() + (controllerCount * contB.Skip(step - 15).Sum());
                    break;
                case 21: //[21] Set Normal Setting.
                case 22: //[22] Restore User Settings.
                case 23: //[23] Move Latest to Previous.
                case 24: //[24] Move Temp to Latest.
                case 25: //[25] Send Reconfig.
                case 26: //[26] Delete Temp Files.
                case 27: //[27] Unselect All Cabinets.
                case 28: //[28] FTP Off.
                    processSec = commonA.Skip(step - 16).Sum() + (controllerCount * contB.Skip(step - 16).Sum());
                    break;
                default:
                    break;
            }

            if (Settings.Ins.ExecLog == true)
            { SaveExecLog($"CalcGainAllModulesProcessSec step:{step} controllerCount:{controllerCount} responseSec:{responseSec} dataMoveSec:{dataMoveSec} processSec:{processSec}"); }

            return processSec;
        }

#endregion Private Methods

    }
}
