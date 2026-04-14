using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CAS
{
    public partial class MainWindow : Window
    {
        #region Fields

        #endregion Fields

        #region Events

        private void btnSmSelectAllUnit_Click(object sender, RoutedEventArgs s)
        {
            if (sender != null)
            { actionButton(sender, "Select All"); }

            initSigMode();

            for (int i = 0; i < allocInfo.MaxX; i++)
            {
                for (int j = 0; j < allocInfo.MaxY; j++)
                {
                    if (arySmUnit[i, j].UnitInfo != null)
                    { Dispatcher.Invoke(new Action(() => { arySmUnit[i, j].IsChecked = true; })); }
                }
            }

            if (sender != null)
            {
                System.Threading.Thread.Sleep(600);
                releaseButton(sender);
            }
        }

        private void btnSmDeselectAllUnit_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            { actionButton(sender, "Deselect All"); }

            initSigMode();

            for (int i = 0; i < allocInfo.MaxX; i++)
            {
                for (int j = 0; j < allocInfo.MaxY; j++)
                {
                    if (arySmUnit[i, j].UnitInfo != null)
                    { Dispatcher.Invoke(new Action(() => { arySmUnit[i, j].IsChecked = false; })); }
                }
            }

            if (sender != null)
            {
                System.Threading.Thread.Sleep(600);
                releaseButton(sender);
            }
        }

        private void utbSm_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            rectSmArea.Visibility = System.Windows.Visibility.Visible;
            rectSmArea.Margin = new Thickness(e.GetPosition(this).X - 2, e.GetPosition(this).Y - 48, 0, 0);

            startPos = new Point(rectSmArea.Margin.Left, rectSmArea.Margin.Top);
        }

        private void utbSm_PreviewMouseUp(object sender, MouseButtonEventArgs e)
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

            double offsetX = tcMain.Margin.Left + gdSigMode.Margin.Left + gdAllocSig.Margin.Left + gdAllocSmLayout.Margin.Left + cabinetAllocColumnHeaderWidth - svSignalMode.HorizontalOffset;
            double offsetY = tcMain.Margin.Top + gdSigMode.Margin.Top + gdAllocSig.Margin.Top + gdAllocSmLayout.Margin.Top + cabinetAllocRowHeaderHeight - svSignalMode.VerticalOffset;

            for (int y = 0; y < allocInfo.MaxY; y++)
            {
                for (int x = 0; x < allocInfo.MaxX; x++)
                {
                    if (arySmUnit[x, y].IsEnabled == false)
                    { continue; }

                    if (isContainedDragArea(arySmUnit[x, y], area, offsetX, offsetY) == true)
                    {
                        if (arySmUnit[x, y].IsChecked == true)
                        { arySmUnit[x, y].IsChecked = false; }
                        else
                        { arySmUnit[x, y].IsChecked = true; }
                    }
                }
            }

            rectSmArea.Visibility = System.Windows.Visibility.Hidden;

            initSigMode();
        }

        private void utbSm_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            double height = e.GetPosition(this).Y - 48 - startPos.Y;
            try
            {
                if (height >= 0)
                { rectSmArea.Height = height; }
                else
                {
                    rectSmArea.Margin = new Thickness(rectSmArea.Margin.Left, startPos.Y + height, 0, 0);
                    rectSmArea.Height = Math.Abs(height);
                }
            }
            catch { rectSmArea.Height = 0; }

            double width = e.GetPosition(this).X - 2 - startPos.X;
            try
            {
                if (width >= 0)
                { rectSmArea.Width = width; }
                else
                {
                    rectSmArea.Margin = new Thickness(startPos.X + width, rectSmArea.Margin.Top, 0, 0);
                    rectSmArea.Width = Math.Abs(width);
                }
            }
            catch { rectSmArea.Width = 0; }
        }

        private async void btnSmGetData_Click(object sender, RoutedEventArgs e)
        {
            tcMain.IsEnabled = false;
            actionButton(sender, "Get Signal Mode.");

            bool status = false;
            string msg, caption;

            winProgress = new WindowProgress("Get Signal mode Progress");
            winProgress.Show();

            try { status = await Task.Run(() => getSignalMode()); }
            catch (Exception ex)
            { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

            if (status)
            {
                initSmCheckBoxIsEnabled(true);
                initSmCheckBoxIsThreeState(false);
                initSmButtonIsEnabled(true);

                msg = "Get Data Complete!";
                caption = "Complete";
            }
            else
            {
                initSigMode();

                msg = "Failed in Get Data.";
                caption = "Error";
            }

            winProgress.Close();

            playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
            WindowMessage winMessage = new WindowMessage(msg, caption);
            winMessage.ShowDialog();

            releaseButton(sender, "Get Signal Mode Done.");
            tcMain.IsEnabled = true;
        }

        private async void btnSmSetData_Click(object sender, RoutedEventArgs e)
        {
            tcMain.IsEnabled = false;
            actionButton(sender, "Set Signal Mode.");

            bool status = false;
            string msg, caption;

            winProgress = new WindowProgress("Set Signal Mode Progress");
            winProgress.Show();

            try { status = await Task.Run(() => setSignalMode(SignalModeSetTypes.Set)); }
            catch (Exception ex)
            { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

            if (status)
            {
                msg = "Set Data Complete!";
                caption = "Complete";
            }
            else
            {
                msg = "Failed in Set Data.";
                caption = "Error";
            }

            winProgress.Close();

            playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
            WindowMessage winMessage = new WindowMessage(msg, caption);
            winMessage.ShowDialog();

            releaseButton(sender, "Set Signal Mode Done.");
            tcMain.IsEnabled = true;
        }

        private async void btnSmResetData_Click(object sender, RoutedEventArgs e)
        {
            tcMain.IsEnabled = false;
            actionButton(sender, "Reset Signal Mode.");

            bool status = false;
            string msg, caption;

            winProgress = new WindowProgress("Reset Signal Mode Progress");
            winProgress.Show();

            try { status = await Task.Run(() => setSignalMode(SignalModeSetTypes.Reset)); }
            catch(Exception ex)
            { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

            if (status)
            {
                msg = "Reset Data Complete!";
                caption = "Complete";
            }
            else
            {
                msg = "Failed in Reset Data.";
                caption = "Error";
            }

            winProgress.Close();

            playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
            WindowMessage winMessage = new WindowMessage(msg, caption);
            winMessage.ShowDialog();

            releaseButton(sender, "Set Signal Mode Done.");
            tcMain.IsEnabled = true;
        }

        #endregion Events

        #region Private Methods

        /// <summary>
        /// Get Dataボタンイベント関数
        /// </summary>
        /// <returns></returns>
        private bool getSignalMode()
        {
            System.Windows.Threading.Dispatcher dispatcher = Application.Current.Dispatcher;

            List<UnitInfo> lstTargetUnit = new List<UnitInfo>();
            List<string> lstNGTargetUnit = new List<string>();

            if (Settings.Ins.ExecLog == true)
            {
                SaveExecLog("");
                SaveExecLog("[0] Get Signal Mode Start.");
            }

            // ●進捗の最大値を設定 [1]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[1] Set Step Count."); }
            winProgress.ShowMessage("Set Step Count.");

            int step = 6;
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

            Dispatcher.Invoke(() => storeSmSelectedUnit(out lstTargetUnit));
            if (lstTargetUnit.Count == INT_ZERO)
            {
                ShowMessageWindow("Please select cabinet(s).", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●SigModeを取得 [4]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[4] Get Signal Mode."); }
            winProgress.ShowMessage("Get Signal Mode.");

            List<SignalMode> lstSigMode = new List<SignalMode>();
            foreach (UnitInfo unit in lstTargetUnit)
            {
                SignalMode sigMode = new SignalMode();

                winProgress.ShowMessage($"Get Target Cabinet: C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}");
                if (!getSignalModeStatus(unit, SignalModeTypes.SigOne, out int value))
                {
                    if (!lstNGTargetUnit.Contains($"C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}"))
                    { lstNGTargetUnit.Add($"C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}"); }

                    continue;
                }
                else
                {
                    if (!storeSignalModeStatus(SignalModeTypes.SigOne, sigMode, value))
                    {
                        if (!lstNGTargetUnit.Contains($"C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}"))
                        { lstNGTargetUnit.Add($"C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}"); }

                        continue;
                    }
                }

                if (!getSignalModeStatus(unit, SignalModeTypes.SigTwo, out value))
                {
                    if (!lstNGTargetUnit.Contains($"C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}"))
                    { lstNGTargetUnit.Add($"C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}"); }

                    continue;
                }
                else
                {
                    if (!storeSignalModeStatus(SignalModeTypes.SigTwo, sigMode, value))
                    {
                        if (!lstNGTargetUnit.Contains($"C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}"))
                        { lstNGTargetUnit.Add($"C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}"); }

                        continue;
                    }
                }

                lstSigMode.Add(sigMode);
            }

            if (lstNGTargetUnit.Count > 0)
            {
                string msg = "Communication with the following cabinets is not possible or the response value is an error.";
                foreach (string unit in lstNGTargetUnit)
                { msg += $"\r\n{unit}"; }

                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);

                if (lstTargetUnit.Count == lstNGTargetUnit.Count)
                { return false; }
            }
            winProgress.PutForward1Step();

            // ●CheckBoxを初期化 [5]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[5] Init CheckBoxs Check Status."); }
            winProgress.ShowMessage("init CheckBoxs Check Status.");

            dispatcher.Invoke(() => initSmCheckBoxIsChecked(false));
            winProgress.PutForward1Step();

            // ●Signal Modeステータスを反映 [6]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[6] Update Signal Mode."); }
            winProgress.ShowMessage("Update Signal Mode.");

            if (!updateSignalModeStatus(lstSigMode))
            {
                ShowMessageWindow("Failed update signal mode", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            return true;
        }

        /// <summary>
        /// Set Data ボタンイベント関数
        /// </summary>
        /// <param name="setTypes">Set DataかResetかを判定する</param>
        /// <returns></returns>
        private bool setSignalMode(SignalModeSetTypes setTypes)
        {
            List<UnitInfo> lstTargetUnit = new List<UnitInfo>();
            List<string> lstNGTargetUnit = new List<string>();

            if (Settings.Ins.ExecLog == true)
            {
                SaveExecLog("");
                if (setTypes == SignalModeSetTypes.Set)
                { SaveExecLog("[0] Set Signal Mode Start."); }
                else
                { SaveExecLog("[0] Reset Signal Mode Start."); }
            }

            // ●進捗の最大値を設定 [1]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[1] Set Step Count."); }
            winProgress.ShowMessage("Set Step Count.");

            int step = 5;
            winProgress.SetWholeSteps(step);
            winProgress.PutForward1Step();

            // ●Power OnでないときはOnにする [2]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[2] Check Controller Power."); }
            winProgress.ShowMessage("Check Controller Power.");

            if (setTypes == SignalModeSetTypes.Reset)
            {
                foreach (ControllerInfo controller in dicController.Values)
                {
                    if (!getPowerStatus(controller.IPAddress, out PowerStatus power))
                    {
                        string msg = "Failed get controller power status.";
                        ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                        return false;
                    }

                    if (power != PowerStatus.Power_On)
                    {
                        string msg = "Controller is not power on.\r\nNo need to reset.";
                        ShowMessageWindow(msg, "Info!", System.Drawing.SystemIcons.Information, 300, 180);
                        return true;
                    }
                }
            }
            else //setTypes == SignalModeSetTypes.Set
            {
                if (!setAllControllerPowerOn())
                { return false; }
            }
            winProgress.PutForward1Step();

            // ●調整をするCabinetをListに格納 [3]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[3] Store Target Cabinet(s) Info."); }
            winProgress.ShowMessage("Store Target Cabinet(s) Info.");

            Dispatcher.Invoke(() => storeSmSelectedUnit(out lstTargetUnit));
            if (lstTargetUnit.Count == INT_ZERO)
            {
                ShowMessageWindow("Please select cabinet(s).", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●SigMode設定パラメータを作成 [4]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[4] Make Signal Mode Parameter."); }
            winProgress.ShowMessage("Make Signal Mode Parameter.");

            int sigModeOneParameter, sigModeTwoParameter;
            try
            {
                if (!makeSignalModeParameter(setTypes, out sigModeOneParameter, out sigModeTwoParameter))
                {
                    ShowMessageWindow("Checkbox has an Indeterminate state.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }
            }
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }
            winProgress.PutForward1Step();

            // ●SigMode設定 [5]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[5] Set Signal Mode."); }
            winProgress.ShowMessage("Set Signal Mode.");

            foreach (UnitInfo unit in lstTargetUnit)
            {
                winProgress.ShowMessage($"Set Target Cabinet: C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}");
                if (!setSignalModeStatus(unit, sigModeOneParameter, sigModeTwoParameter))
                { lstNGTargetUnit.Add($"C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}"); }
            }

            if (lstNGTargetUnit.Count > 0)
            {
                string msg = "Communication with the following cabinets is not possible or the response value is an error.";
                foreach (string unit in lstNGTargetUnit)
                { msg += $"\r\n{unit}"; }

                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);

                if (lstTargetUnit.Count == lstNGTargetUnit.Count)
                { return false; }
            }
            winProgress.PutForward1Step();

            if (setTypes == SignalModeSetTypes.Reset)
            {
                // ●Signal Modeステータスを反映 [6]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[6] Update Signal Mode."); }
                winProgress.ShowMessage("Update Signal Mode.");

                if (!updateSignalModeDefalutStatus())
                {
                    ShowMessageWindow("Failed update signal mode", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }
                winProgress.PutForward1Step();
            }

            return true;
        }

        /// <summary>
        /// ボタンの有効/無効化管理
        /// </summary>
        /// <param name="status">状態</param>
        private void initSmButtonIsEnabled(bool status)
        {
            btnSmSetData.IsEnabled = status;
            btnSmResetData.IsEnabled = status;
        }

        private void initSmCheckBoxVisibility()
        {
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            {
                // Signal Mode 2
                cbLcalV.Visibility = Visibility.Collapsed;
                cbLcalH.Visibility = Visibility.Collapsed;
                cbLmcal.Visibility = Visibility.Collapsed;
                cbMgam.Visibility = Visibility.Collapsed;

                cbVac.Margin = new Thickness(30, 0, 0, 40);
            }
            else
            {
                cbLcalV.Visibility = Visibility.Visible;
                cbLcalH.Visibility = Visibility.Visible;
                cbLmcal.Visibility = Visibility.Visible;
                cbMgam.Visibility = Visibility.Visible;

                cbVac.Margin = new Thickness(330, 0, 0, 40);
            }
        }

        /// <summary>
        /// チェックボックスのチェック状態管理
        /// </summary>
        /// <param name="status">状態</param>
        private void initSmCheckBoxIsChecked(bool status)
        {
            // Signal Mode 1
            cbIngam.IsChecked = status;
            cbUnc.IsChecked = status;
            cbOgam.IsChecked = status;
            cbSbm.IsChecked = status;
            cbAgc.IsChecked = status;
            cbTmp.IsChecked = status;

            // Signal Mode 2
            cbVac.IsChecked = status;
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
            {
                cbLcalV.IsChecked = status;
                cbLcalH.IsChecked = status;
                cbLmcal.IsChecked = status;
                cbMgam.IsChecked = status;
            }
        }

        /// <summary>
        /// チェックボックスの有効無効状態管理
        /// </summary>
        /// <param name="status">状態</param>
        private void initSmCheckBoxIsEnabled(bool status)
        {
            // Signal Mode 1
            cbIngam.IsEnabled = status;
            cbUnc.IsEnabled = status;
            cbOgam.IsEnabled = status;
            cbSbm.IsEnabled = status;
            cbAgc.IsEnabled = status;
            cbTmp.IsEnabled = status;

            // Signal Mode 2
            cbVac.IsEnabled = status;
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
            {
                cbLcalV.IsEnabled = status;
                cbLcalH.IsEnabled = status;
                cbLmcal.IsEnabled = status;
                cbMgam.IsEnabled = status;
            }
        }

        /// <summary>
        /// チェックボックスのサポート種類管理
        /// </summary>
        /// <param name="status">状態</param>
        private void initSmCheckBoxIsThreeState(bool status)
        {
            // Signal Mode 1
            cbIngam.IsThreeState = status;
            cbUnc.IsThreeState = status;
            cbOgam.IsThreeState = status;
            cbSbm.IsThreeState = status;
            cbAgc.IsThreeState = status;
            cbTmp.IsThreeState = status;

            // Signal Mode 2
            cbVac.IsThreeState = status;
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
            {
                cbLcalV.IsThreeState = status;
                cbLcalH.IsThreeState = status;
                cbLmcal.IsThreeState = status;
                cbMgam.IsThreeState = status;
            }
        }

        /// <summary>
        /// 対象にキャビネットを取得
        /// </summary>
        /// <param name="lstUnitList">選択されたキャビネットが格納されるリスト</param>
        private void storeSmSelectedUnit(out List<UnitInfo> lstUnitList)
        {
            lstUnitList = new List<UnitInfo>();

            for (int y = 0; y < allocInfo.MaxY; y++)
            {
                for (int x = 0; x < allocInfo.MaxX; x++)
                {
                    if (arySmUnit[x, y].UnitInfo != null && arySmUnit[x, y].IsChecked == true)
                    {
                        lstUnitList.Add(arySmUnit[x, y].UnitInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Signal Mode 1&2を取得
        /// </summary>
        /// <param name="unit">対象キャビネット</param>
        /// <param name="sigModeType">Signal Mode 1||2を判定する</param>
        /// <param name="sigValue"></param>
        /// <returns></returns>
        private bool getSignalModeStatus(UnitInfo unit, SignalModeTypes sigModeType, out int sigValue)
        {
            sigValue = -1;

            byte[] cmd;
            if (sigModeType == SignalModeTypes.SigOne)
            {
                cmd = new byte[SDCPClass.CmdSigModeOneGet.Length];
                Array.Copy(SDCPClass.CmdSigModeOneGet, cmd, SDCPClass.CmdSigModeOneGet.Length);
            }
            else
            {
                cmd = new byte[SDCPClass.CmdSigModeTwoGet.Length];
                Array.Copy(SDCPClass.CmdSigModeTwoGet, cmd, SDCPClass.CmdSigModeTwoGet.Length);
            }

            cmd[9] = 0x00;
            cmd[9] += (byte)(unit.PortNo << 4);
            cmd[9] += (byte)unit.UnitNo;

            if (!sendSdcpCommand(cmd, out string buff, dicController[unit.ControllerID].IPAddress))
            { return false; }

            if (isErrorResponseValue(buff))
            { return false; }

            sigValue = Convert.ToInt32(buff, 16);

            return true;
        }

        /// <summary>
        /// Signal Mode 2のVACを確認
        /// </summary>
        /// <param name="value">対象コントローラーから取得したパラメータ</param>
        /// <returns></returns>
        private bool checkSignalModeTwoVacStatus(int value)
        {
            return (value & 0x08) != 0;
        }

        /// <summary>
        /// チェックボックスの状態をGUI上から取得
        /// </summary>
        /// <param name="sigMode">Signal Mode 1&2の状態を保持</param>
        /// <returns></returns>
        //private bool storeCheckBoxStatus(out SignalMode sigMode)
        //{
        //    System.Windows.Threading.Dispatcher dispatcher = Application.Current.Dispatcher;

        //    sigMode = new SignalMode();
        //    try
        //    {
        //        // Signal Mode 1
        //        sigMode._SignalModeOne.Ingam = dispatcher.Invoke(() => cbIngam.IsChecked);
        //        sigMode._SignalModeOne.Unc = dispatcher.Invoke(() => cbUnc.IsChecked);
        //        sigMode._SignalModeOne.Ogam = dispatcher.Invoke(() => cbOgam.IsChecked);
        //        sigMode._SignalModeOne.Sbm = dispatcher.Invoke(() => cbSbm.IsChecked);
        //        sigMode._SignalModeOne.Agc = dispatcher.Invoke(() => cbAgc.IsChecked);
        //        sigMode._SignalModeOne.Tmp = dispatcher.Invoke(() => cbTmp.IsChecked);

        //        // Signal Mode 2
        //        sigMode._SignalModeTwo.Vac = dispatcher.Invoke(() => cbVac.IsChecked);
        //        if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
        //        {
        //            sigMode._SignalModeTwo.LcalV = dispatcher.Invoke(() => cbLcalV.IsChecked);
        //            sigMode._SignalModeTwo.LcalH = dispatcher.Invoke(() => cbLcalH.IsChecked);
        //            sigMode._SignalModeTwo.Lmcal = dispatcher.Invoke(() => cbLmcal.IsChecked);
        //            sigMode._SignalModeTwo.Mgam = dispatcher.Invoke(() => cbMgam.IsChecked);
        //        }
        //    }
        //    catch
        //    { return false; }

        //    return true;
        //}

        /// <summary>
        /// GUI上の選択状態に応じてSignal Mode 1&2に設定するコマンドパラメータを生成
        /// </summary>
        /// <param name="setTypes">Set DataかResetかを判定する</param>
        /// <param name="sigModeOneValue">Signal Mode 1の設定パラメータ</param>
        /// <param name="sigModeTwoValue">Signal Mode 2の設定パラメータ</param>
        /// <returns></returns>
        private bool makeSignalModeParameter(SignalModeSetTypes setTypes, out int sigModeOneValue, out int sigModeTwoValue)
        {
            System.Windows.Threading.Dispatcher dispatcher = Application.Current.Dispatcher;
            sigModeOneValue = sigModeTwoValue = -1;
            bool?[] sigModeStatus;

            sigModeStatus = new bool?[] { false, false, true, true, true, true, true, true }; // { BIT7:Reserved, BIT6:Reserved, BIT5:TMP, BIT4:AGC, BIT3:SBM, BIT2:OGAM, BIT1:UNC, BIT0:INGAM }
            if (setTypes == SignalModeSetTypes.Set)
            {
                sigModeStatus[7] = dispatcher.Invoke(() => cbIngam.IsChecked);
                sigModeStatus[6] = dispatcher.Invoke(() => cbUnc.IsChecked);
                sigModeStatus[5] = dispatcher.Invoke(() => cbOgam.IsChecked);
                sigModeStatus[4] = dispatcher.Invoke(() => cbSbm.IsChecked);
                sigModeStatus[3] = dispatcher.Invoke(() => cbAgc.IsChecked);
                sigModeStatus[2] = dispatcher.Invoke(() => cbTmp.IsChecked);
            }

            if (sigModeStatus.Contains(null)) // null = indeterminate
            { return false; }

            string binaryStr = string.Join("", sigModeStatus.Select(s => s == true ? "1" : "0"));
            sigModeOneValue = Convert.ToInt32(binaryStr, 2);

            sigModeStatus = new bool?[] { false, false, false, true, false, true, true, true }; // { BIT7:Reserved, BIT6:Reserved, BIT5:Reserved, BIT4:MGAM, BIT4:VAC, BIT3:LMCAL, BIT1:LCAL-H, BIT0:LCAL-V }
            if (setTypes == SignalModeSetTypes.Set)
            {
                sigModeStatus[7] = dispatcher.Invoke(() => cbLcalV.IsChecked);
                sigModeStatus[6] = dispatcher.Invoke(() => cbLcalH.IsChecked);
                sigModeStatus[5] = dispatcher.Invoke(() => cbLmcal.IsChecked);
                sigModeStatus[4] = dispatcher.Invoke(() => cbVac.IsChecked);
                sigModeStatus[3] = dispatcher.Invoke(() => cbMgam.IsChecked);
            }

            if (sigModeStatus.Contains(null))
            { return false; }

            binaryStr = string.Join("", sigModeStatus.Select(s => s == true ? "1" : "0"));
            sigModeTwoValue = Convert.ToInt32(binaryStr, 2);

            return true;
        }

        /// <summary>
        /// Signal Mode 1&2を設定
        /// </summary>
        /// <param name="unit">対象キャビネット</param>
        /// <param name="sigModeOneParam">Signal Mode 1に設定するパラメータ</param>
        /// <param name="sigModeTwoParam">Signal Mode 2に設定するパラメータ</param>
        /// <returns></returns>
        private bool setSignalModeStatus(UnitInfo unit, int sigModeOneParam, int sigModeTwoParam)
        {
            byte[] cmd;

            // Signal Mode 1
            cmd = new byte[SDCPClass.CmdSigModeOneSet.Length];
            Array.Copy(SDCPClass.CmdSigModeOneSet, cmd, SDCPClass.CmdSigModeOneSet.Length);

            cmd[9] = 0x00;
            cmd[9] += (byte)(unit.PortNo << 4);
            cmd[9] += (byte)unit.UnitNo;

            cmd[20] = Convert.ToByte(sigModeOneParam);

            if (!sendSdcpCommand(cmd, out string buff, dicController[unit.ControllerID].IPAddress))
            { return false; }

            if (isErrorResponseValue(buff))
            { return false; }

            // Signal Mode 2
            cmd = new byte[SDCPClass.CmdSigModeTwoSet.Length];
            Array.Copy(SDCPClass.CmdSigModeTwoSet, cmd, SDCPClass.CmdSigModeTwoSet.Length);

            cmd[9] = 0x00;
            cmd[9] += (byte)(unit.PortNo << 4);
            cmd[9] += (byte)unit.UnitNo;

            cmd[20] = Convert.ToByte(sigModeTwoParam);

            if (!sendSdcpCommand(cmd, out buff, dicController[unit.ControllerID].IPAddress))
            { return false; }

            if (isErrorResponseValue(buff))
            { return false; }

            return true;
        }

        /// <summary>
        /// Signal Modeの状態を保持
        /// </summary>
        /// <param name="sigModeType">Signal Mode 1||2を判定する</param>
        /// <param name="sigMode">Signal Mode 1&2の状態が格納されている</param>
        /// <param name="value">Signal Mode 1||2から取得した状態</param>
        /// <returns></returns>
        private bool storeSignalModeStatus(SignalModeTypes sigModeType, SignalMode sigMode, int value)
        {
            try
            {
                if (sigModeType == SignalModeTypes.SigOne)
                {
                    _ = (value & (1 << 0)) != 0
                        ? sigMode._SignalModeOne.Ingam = true
                        : sigMode._SignalModeOne.Ingam = false;

                    _ = (value & (1 << 1)) != 0
                        ? sigMode._SignalModeOne.Unc = true
                        : sigMode._SignalModeOne.Unc = false;

                    _ = (value & (1 << 2)) != 0
                        ? sigMode._SignalModeOne.Ogam = true
                        : sigMode._SignalModeOne.Ogam = false;

                    _ = (value & (1 << 3)) != 0
                        ? sigMode._SignalModeOne.Sbm = true
                        : sigMode._SignalModeOne.Sbm = false;

                    _ = (value & (1 << 4)) != 0
                        ? sigMode._SignalModeOne.Agc = true
                        : sigMode._SignalModeOne.Agc = false;

                    _ = (value & (1 << 5)) != 0
                        ? sigMode._SignalModeOne.Tmp = true
                        : sigMode._SignalModeOne.Tmp = false;
                }
                else
                {
                    _ = (value & (1 << 0)) != 0
                        ? sigMode._SignalModeTwo.LcalV = true
                        : sigMode._SignalModeTwo.LcalV = false;

                    _ = (value & (1 << 1)) != 0
                        ? sigMode._SignalModeTwo.LcalH = true
                        : sigMode._SignalModeTwo.LcalH = false;

                    _ = (value & (1 << 2)) != 0
                        ? sigMode._SignalModeTwo.Lmcal = true
                        : sigMode._SignalModeTwo.Lmcal = false;

                    _ = (value & (1 << 3)) != 0
                        ? sigMode._SignalModeTwo.Vac = true
                        : sigMode._SignalModeTwo.Vac = false;

                    _ = (value & (1 << 4)) != 0
                        ? sigMode._SignalModeTwo.Mgam = true
                        : sigMode._SignalModeTwo.Mgam = false;
                }
            }
            catch
            { return false; }

            return true;
        }

        /// <summary>
        /// Signal Mode ディフォルト状態をGUI上に反映
        /// </summary>
        /// <returns></returns>
        private bool updateSignalModeDefalutStatus()
        {
            System.Windows.Threading.Dispatcher dispatcher = Application.Current.Dispatcher;

            try
            {
                //Signal Mode 1
                // INGAM
                _ = dispatcher.Invoke(() => cbIngam.IsChecked = true);

                // UNC
                _ = dispatcher.Invoke(() => cbUnc.IsChecked = true);

                // OGAM
                _ = dispatcher.Invoke(() => cbOgam.IsChecked = true);

                // SBM
                _ = dispatcher.Invoke(() => cbSbm.IsChecked = true);

                // AGC
                _ = dispatcher.Invoke(() => cbAgc.IsChecked = true);

                // TMP
                _ = dispatcher.Invoke(() => cbTmp.IsChecked = true);


                // Signal Mode 2
                // LCAL-V
                _ = dispatcher.Invoke(() => cbLcalV.IsChecked = true);

                // LCAL-H
                _ = dispatcher.Invoke(() => cbLcalH.IsChecked = true);

                // LMCAL
                _ = dispatcher.Invoke(() => cbLmcal.IsChecked = true);

                // VAC
                _ = dispatcher.Invoke(() => cbVac.IsChecked = false);

                // MGAM
                _ = dispatcher.Invoke(() => cbMgam.IsChecked = true);
            }
            catch
            { return false; }

            return true;
        }

        /// <summary>
        /// Signal Mode状態をGUI上に反映
        /// </summary>
        /// <param name="lstSigMode">全キャビネットのSignal Mode1&2の状態が格納されているリスト</param>
        /// <returns></returns>
        private bool updateSignalModeStatus(List<SignalMode> lstSigMode)
        {
            System.Windows.Threading.Dispatcher dispatcher = Application.Current.Dispatcher;

            try
            {
                // Signal Mode 1
                // INGAM
                if (lstSigMode.All(x => x._SignalModeOne.Ingam))
                { _ = dispatcher.Invoke(() => cbIngam.IsChecked = true); }
                else if (lstSigMode.All(x => !x._SignalModeOne.Ingam))
                { _ = dispatcher.Invoke(() => cbIngam.IsChecked = false); }
                else
                { _ = dispatcher.Invoke(() => cbIngam.IsChecked = null); }

                // UNC
                if (lstSigMode.All(x => x._SignalModeOne.Unc))
                { _ = dispatcher.Invoke(() => cbUnc.IsChecked = true); }
                else if (lstSigMode.All(x => !x._SignalModeOne.Unc))
                { _ = dispatcher.Invoke(() => cbUnc.IsChecked = false); }
                else
                { _ = dispatcher.Invoke(() => cbUnc.IsChecked = null); }

                // OGAM
                if (lstSigMode.All(x => x._SignalModeOne.Ogam))
                { _ = dispatcher.Invoke(() => cbOgam.IsChecked = true); }
                else if (lstSigMode.All(x => !x._SignalModeOne.Ogam))
                { _ = dispatcher.Invoke(() => cbOgam.IsChecked = false); }
                else
                { _ = dispatcher.Invoke(() => cbOgam.IsChecked = null); }

                // SBM
                if (lstSigMode.All(x => x._SignalModeOne.Sbm))
                { _ = dispatcher.Invoke(() => cbSbm.IsChecked = true); }
                else if (lstSigMode.All(x => !x._SignalModeOne.Sbm))
                { _ = dispatcher.Invoke(() => cbSbm.IsChecked = false); }
                else
                { _ = dispatcher.Invoke(() => cbSbm.IsChecked = null); }

                // AGC
                if (lstSigMode.All(x => x._SignalModeOne.Agc))
                { _ = dispatcher.Invoke(() => cbAgc.IsChecked = true); }
                else if (lstSigMode.All(x => !x._SignalModeOne.Agc))
                { _ = dispatcher.Invoke(() => cbAgc.IsChecked = false); }
                else
                { _ = dispatcher.Invoke(() => cbAgc.IsChecked = null); }

                // TMP
                if (lstSigMode.All(x => x._SignalModeOne.Tmp))
                { _ = dispatcher.Invoke(() => cbTmp.IsChecked = true); }
                else if (lstSigMode.All(x => !x._SignalModeOne.Tmp))
                { _ = dispatcher.Invoke(() => cbTmp.IsChecked = false); }
                else
                { _ = dispatcher.Invoke(() => cbTmp.IsChecked = null); }

                // Signal Mode 2
                // VAC
                if (lstSigMode.All(x => x._SignalModeTwo.Vac))
                { _ = dispatcher.Invoke(() => cbVac.IsChecked = true); }
                else if (lstSigMode.All(x => !x._SignalModeTwo.Vac))
                { _ = dispatcher.Invoke(() => cbVac.IsChecked = false); }
                else
                { _ = dispatcher.Invoke(() => cbVac.IsChecked = null); }

                // Chironの場合、下記LCAL-V/LCAL-H/LMCAL/MGAMチェックボックスは非表示のため
                // 複数CabinetからSignalMode2を取得するとIndeterminate状態になる可能性があり
                // Indeterminate状態で[Set Data]機能実行すると処理失敗になる(Set Dataの仕様)ため
                // Indeterminate状態が存在し強制trueに設定するよう仕様決定
                if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
                {
                    // LCAL-V
                    _ = lstSigMode.All(x => !x._SignalModeTwo.LcalV)
                        ? dispatcher.Invoke(() => cbLcalV.IsChecked = false)
                        : dispatcher.Invoke(() => cbLcalV.IsChecked = true);

                    // LCAL-H
                    _ = lstSigMode.All(x => !x._SignalModeTwo.LcalH)
                        ? dispatcher.Invoke(() => cbLcalH.IsChecked = false)
                        : dispatcher.Invoke(() => cbLcalH.IsChecked = true);

                    // LMCAL
                    _ = lstSigMode.All(x => !x._SignalModeTwo.Lmcal)
                        ? dispatcher.Invoke(() => cbLmcal.IsChecked = false)
                        : dispatcher.Invoke(() => cbLmcal.IsChecked = true);

                    // MGAM
                    _ = lstSigMode.All(x => !x._SignalModeTwo.Mgam)
                        ? dispatcher.Invoke(() => cbMgam.IsChecked = false)
                        : dispatcher.Invoke(() => cbMgam.IsChecked = true);
                }
                else
                {
                    // LCAL-V
                    if (lstSigMode.All(x => x._SignalModeTwo.LcalV))
                    { _ = dispatcher.Invoke(() => cbLcalV.IsChecked = true); }
                    else if (lstSigMode.All(x => !x._SignalModeTwo.LcalV))
                    { _ = dispatcher.Invoke(() => cbLcalV.IsChecked = false); }
                    else
                    { _ = dispatcher.Invoke(() => cbLcalV.IsChecked = null); }

                    // LCAL-H
                    if (lstSigMode.All(x => x._SignalModeTwo.LcalH))
                    { _ = dispatcher.Invoke(() => cbLcalH.IsChecked = true); }
                    else if (lstSigMode.All(x => !x._SignalModeTwo.LcalH))
                    { _ = dispatcher.Invoke(() => cbLcalH.IsChecked = false); }
                    else
                    { _ = dispatcher.Invoke(() => cbLcalH.IsChecked = null); }

                    // LMCAL
                    if (lstSigMode.All(x => x._SignalModeTwo.Lmcal))
                    { _ = dispatcher.Invoke(() => cbLmcal.IsChecked = true); }
                    else if (lstSigMode.All(x => !x._SignalModeTwo.Lmcal))
                    { _ = dispatcher.Invoke(() => cbLmcal.IsChecked = false); }
                    else
                    { _ = dispatcher.Invoke(() => cbLmcal.IsChecked = null); }

                    // MGAM
                    if (lstSigMode.All(x => x._SignalModeTwo.Mgam))
                    { _ = dispatcher.Invoke(() => cbMgam.IsChecked = true); }
                    else if (lstSigMode.All(x => !x._SignalModeTwo.Mgam))
                    { _ = dispatcher.Invoke(() => cbMgam.IsChecked = false); }
                    else
                    { _ = dispatcher.Invoke(() => cbMgam.IsChecked = null); }
                }
            }
            catch
            { return false; }

            return true;
        }

        #endregion Private Methods
    }
}
