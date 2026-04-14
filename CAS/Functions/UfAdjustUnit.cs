using MakeUFData;
using Microsoft.Win32;
using SONY.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CAS
{
    using CabinetOffset = System.Drawing.Point;

    // UF Adjust
    public partial class MainWindow : Window
    {
        #region Fields

        private bool uniformityTabFirstSelect = true;

        private CaSdk caSdk;
        private CMakeUFData m_MakeUFData;

        //// UF Target Default
        //public const double m_Target_xr = 0.687;
        //public const double m_Target_yr = 0.306;
        //public const double m_Target_xg = 0.218;
        //public const double m_Target_yg = 0.666;
        //public const double m_Target_xb = 0.145;//0.151 TS // 0.145 PP以降
        //public const double m_Target_yb = 0.054;//0.041 TS // 0.054 PP以降
        //public const double m_Target_Yw = 204.0;
        //public const double m_Target_xw = 0.285;
        //public const double m_Target_yw = 0.294;

        private ChromCustom ufTargetChrom;

        private Brightness brightness;

        private ColorAnalyzerModel analyzer = ColorAnalyzerModel.NA;

        private CursorType curType = CursorType.WhiteCross;

        private const int AJUSTMENT_UNIFORMITY_STEPS = 23;
        private const int AJUSTMENT_CABINET_STEPS = 31;
        private const int AJUSTMENT_MODULE_STEPS = 34;

        #endregion Fields

        #region Events

        private void getUniformityTargetMode()
        {
            if (Settings.Ins.Uniformity.ModeOption == ModeOption.Normal)
            { cmbxTargetMode.SelectedIndex = 0; }
            else
            { cmbxTargetMode.SelectedIndex = 1; }
        }

        private void cmbxTargetMode_DropDownClosed(object sender, EventArgs e)
        {
            if (((ComboBox)sender).SelectedIndex == 0)
            { Settings.Ins.Uniformity.ModeOption = ModeOption.Normal; }
            else
            { Settings.Ins.Uniformity.ModeOption = ModeOption.Relative; }
        }

        // Adjust
        private void UfAdjustRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (gdCellFrame != null)
            { gdCellFrame.IsEnabled = false; }

            if (rbUnitUFAdjust != null && rbUnitUFAdjust.IsChecked == true)
            { btnDeselectAll_Click(null, null); }

            if ((rbStandard != null && rbStandard.IsChecked == true)
                || (rbMeasureOnly != null && rbMeasureOnly.IsChecked == true)
                || (rbStrict != null && rbStrict.IsChecked == true))
            { cmbxTargetMode.IsEnabled = true; }
            else
            { cmbxTargetMode.IsEnabled = false; }
        }

        private void rbCellUFAdjust_Checked(object sender, RoutedEventArgs e)
        {
            btnDeselectAll_Click(null, null);

            gdCellFrame.IsEnabled = true;
            cmbxTargetMode.IsEnabled = false;
        }

        private async void btnAdjustStart_Click(object sender, RoutedEventArgs e)
        {
            bool status = false;
            string msg = "", caption = "";
            bool? result = true;

            tcMain.IsEnabled = false;
            actionButton(sender, "UF Adjust Start.");

            // Analyzerのラジオボタンを確認
            if (rbRemoteOff.IsChecked == true)
            {
                msg = "Analyzer is not selected.";
                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);

                playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
                releaseButton(sender, "UF Adjust Done.");
                tcMain.IsEnabled = true;
                return;
            }

            // Set Target Chromaticity / Show Confirm Window
            //if (Settings.Ins.ConfigChromType == ConfigChrom.DigitalCinema2017)
            //{
            //    msg = "Luminance / Chromaticity is Digital Cinema 2017!";
            //    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Asterisk, 400, 180, "Yes");
            //}
            //else if (Settings.Ins.ConfigChromType == ConfigChrom.DigitalCinema2020)
            //{
            //    msg = "Luminance / Chromaticity is Digital Cinema 2020!";
            //    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Asterisk, 400, 180, "Yes");
            //}
            switch (Settings.Ins.ConfigChromType)
            {
                case ConfigChrom.ZRD_C12A:
                msg = "Luminance / Chromaticity is ZRD-C12A!";
                    break;
                case ConfigChrom.ZRD_C15A:
                msg = "Luminance / Chromaticity is ZRD-C15A!";
                    break;
                case ConfigChrom.ZRD_B12A:
                msg = "Luminance / Chromaticity is ZRD-B12A!";
                    break;
                case ConfigChrom.ZRD_B15A:
                msg = "Luminance / Chromaticity is ZRD-B15A!";
                    break;
                case ConfigChrom.ZRD_CH12D:
                msg = "Luminance / Chromaticity is ZRD-CH12D!";
                    break;
                case ConfigChrom.ZRD_CH15D:
                msg = "Luminance / Chromaticity is ZRD-CH15D!";
                    break;
                case ConfigChrom.ZRD_BH12D:
                msg = "Luminance / Chromaticity is ZRD-BH12D!";
                    break;
                case ConfigChrom.ZRD_BH15D:
                msg = "Luminance / Chromaticity is ZRD-BH15D!";
                    break;
                case ConfigChrom.ZRD_CH12D_S3:
                    msg = "Luminance / Chromaticity is ZRD-CH12D/3!";
                    break;
                case ConfigChrom.ZRD_CH15D_S3:
                    msg = "Luminance / Chromaticity is ZRD-CH15D/3!";
                    break;
                case ConfigChrom.ZRD_BH12D_S3:
                    msg = "Luminance / Chromaticity is ZRD-BH12D/3!";
                    break;
                case ConfigChrom.ZRD_BH15D_S3:
                    msg = "Luminance / Chromaticity is ZRD-BH15D/3!";
                    break;
                case ConfigChrom.Custom:
                    msg = "Luminance / Chromaticity is Custom!";
                    break;
            }
            if (!String.IsNullOrEmpty(msg))
            {
                showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Asterisk, 400, 180, "Yes");
            }

            if (result != true)
            {
                playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
                releaseButton(sender, "UF Adjust Done.");
                tcMain.IsEnabled = true;
                return;
            }

            setChromTarget();

            // Relative Target選択時は目標色度を更新
            if ((rbStandard.IsChecked == true || rbMeasureOnly.IsChecked == true || rbStrict.IsChecked == true) && cmbxTargetMode.SelectedIndex == 1)
            {
                if (Settings.Ins.Uniformity.IsRelativeTarget == true)
                { ufTargetChrom = new ChromCustom(ColorPurpose.Relative); }
                else
                {
                    msg = "The [Relative Target] measurement has not been performed. Do you want to continue?";
                    result = false;
                    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 400, 210, "Continue", "Cancel");
                    if (result != true)
                    {
                        playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
                        releaseButton(sender, "UF Adjust Done.");
                        tcMain.IsEnabled = true;
                        return;
                    }
                }
            }

            winProgress = new WindowProgress("Adjust Progress");
            winProgress.Show();

            m_MakeUFData = new CMakeUFData(cabiDx, cabiDy);

            m_lstUserSetting = null;

            //if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
            //{ m_ctc = new CrossTalkCorrectionHighColor(allocInfo.LEDModel); }

            if (rbStandard.IsChecked == true)
            {
                //status = adjustUf(UfAdjustMode.Std);
                status = await Task.Run(() => adjustUf(UfAdjustMode.Std));
            }
            else if (rbMeasureOnly.IsChecked == true)
            {
                status = await Task.Run(() => adjustUf(UfAdjustMode.MeasureOnly));
            }
            else if (rbStrict.IsChecked == true)
            {
                status = await Task.Run(() => adjustUf(UfAdjustMode.EachCell));
            }
            else if (rbUnitUFAdjust.IsChecked == true)
            {
                status = await Task.Run(() => adjustUfUnit());
            }
            else if (rbCellUFAdjust.IsChecked == true)
            {
                status = await Task.Run(() => adjustUfCell());
            }

            if (rbMeasureOnly.IsChecked != true)
            {
                // ●Cabinet Power On
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("Cabinet Power On."); }
                foreach (ControllerInfo cont in dicController.Values)
                { sendSdcpCommand(SDCPClass.CmdUnitPowerOn, SDCP_COMMAND_WAIT_TIME, cont.IPAddress); }
                System.Threading.Thread.Sleep(5000);
                // ●Raster表示
                outputRaster(brightness._20pc); // 20%
            }
            else
            {
                // TestPattern OFF
                foreach (ControllerInfo cont in dicController.Values)
                { sendSdcpCommand(SDCPClass.CmdIntSignalOff, 0, cont.IPAddress); }
            }

            if (status == true)
            {
                msg = "Uniformity adjustment complete !";
                caption = "Complete";
            }
            else
            {
                setNormalSetting();
                setUserSetting(m_lstUserSetting);
                m_lstUserSetting = null;
                setFtpOff();

                msg = "Failed in UF Adjust.";
                caption = "Error";
            }

            winProgress.StopRemainTimer();
            winProgress.Close();

            playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");

            WindowMessage winMessage = new WindowMessage(msg, caption);
            winMessage.ShowDialog();

            releaseButton(sender, "UF Adjust Done.");
            tcMain.IsEnabled = true;
        }
        
        private void rbUfCursor1_Checked(object sender, RoutedEventArgs e)
        {
            if (curType != CursorType.WhiteCross)
            {
                curType = CursorType.WhiteCross;

                rbUfCursor1.IsChecked = true;
                rbMeasCursor1.IsChecked = true;
                rbCalibCursor1.IsChecked = true;
                rbRelTgtCursor1.IsChecked = true;
                rbGgCursor1.IsChecked = true;
            }
        }

        private void rbUfCursor2_Checked(object sender, RoutedEventArgs e)
        {
            if (curType != CursorType.RedSquare)
            {
                curType = CursorType.RedSquare;

                rbUfCursor2.IsChecked = true;
                rbMeasCursor2.IsChecked = true;
                rbCalibCursor2.IsChecked = true;
                rbRelTgtCursor2.IsChecked = true;
                rbGgCursor2.IsChecked = true;
            }
        }

        // CA-410
        private void cmbxChannel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // CA-410 Channel
            changCAChannel(cmbxCA410Channel.SelectedIndex);

            cmbxCA410ChannelMeas.SelectedIndex = cmbxCA410Channel.SelectedIndex;
            //cmbxCA410ChannelCalib.SelectedIndex = cmbxCA410Channel.SelectedIndex;
            cmbxCA410ChannelRelTgt.SelectedIndex = cmbxCA410Channel.SelectedIndex;

            if (appliMode == ApplicationMode.Service && Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
            { cmbxCA410ChannelGain.SelectedIndex = cmbxCA410Channel.SelectedIndex; }
        }
        
        private void rbCA410RemoteOn_Checked(object sender, RoutedEventArgs e)
        {
            var dispatcher = Application.Current.Dispatcher;

            tcMain.IsEnabled = false;

            System.Windows.Controls.RadioButton rb = (System.Windows.Controls.RadioButton)sender;

            if (rb.Content.ToString() == "CA-410")
            { analyzer = ColorAnalyzerModel.CA410; }
            else
            { analyzer = ColorAnalyzerModel.NA; }

            actionButton(sender, "Open " + rb.Content + ".");

            // レジストリキーチェック
            if (isCurrentValueCaSdkTwo())
            {
                // CA-410接続
                if (openSub(1, Settings.Ins.NoOfProbes, Settings.Ins.PortVal))
                {
                    System.Threading.Thread.Sleep(500);

                    if (((RadioButton)sender).Name.Contains("Calib"))
                    { setCA410SettingCalib(); }
                    else
                    { setCA410Setting(); }

                    releaseButton(sender, "Open " + rb.Content + " Done.");
                }
                else
                {
                    DoSetOffCA410RadioButton();

                    string msg = "Failed to connect to CA-410. \r\nCheck if CA-410 is connected correctly. Or, \r\nSelect CA-SDK2 in the Com Registration Tool.";
                    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    releaseButton(sender, "Failed to Open " + rb.Content + ".");
                }
            }
            else
            {
                DoSetOffCA410RadioButton();

                string msg = "Failed to connect to CA-410. \r\nCheck if CA-410 is connected correctly. Or, \r\nSelect CA-SDK2 in the Com Registration Tool.";
                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                releaseButton(sender, "Failed to Open " + rb.Content + ".");
            }

            tcMain.IsEnabled = true;
        }
        
        private void rbRemoteOff_Checked(object sender, RoutedEventArgs e)
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

        // Test Pattern
        private void cmbxUfPattern_DropDownClosed(object sender, EventArgs e)
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

        // Alert
        private void rbMeasAlertOn_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Ins.MeasAlert = true;

            txbMeasThreshXy.IsEnabled = true;
            txbMeasThreshY.IsEnabled = true;
        }

        private void rbMeasAlertOff_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Ins.MeasAlert = false;

            txbMeasThreshXy.IsEnabled = false;
            txbMeasThreshY.IsEnabled = false;
        }

        private void txbMeasThreshXy_TextChanged(object sender, TextChangedEventArgs e)
        {
            try { Settings.Ins.xyThresh = Convert.ToDouble(txbMeasThreshXy.Text); }
            catch
            {
                string msg = "Failed to convert the entered value.\r\nPlease enter a number.";
                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 350, 180);
            }
        }

        private void txbMeasThreshY_TextChanged(object sender, TextChangedEventArgs e)
        {
            try { Settings.Ins.YThresh = Convert.ToDouble(txbMeasThreshY.Text); }
            catch
            {
                string msg = "Failed to convert the entered value.\r\nPlease enter a number.";
                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 350, 180);
            }
        }

        // Allocation
        private void btnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            { actionButton(sender, "Select All"); }

            for (int i = 0; i < allocInfo.MaxX; i++)
            {
                for (int j = 0; j < allocInfo.MaxY; j++)
                {
                    if (aryUnitUf[i, j].UnitInfo != null)
                    { Dispatcher.Invoke(new Action(() => { aryUnitUf[i, j].IsChecked = true; })); }
                }
            }

            if (sender != null)
            {
                System.Threading.Thread.Sleep(600);
                releaseButton(sender);
            }
        }

        private void btnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            { actionButton(sender, "Deselect All"); }

            for (int i = 0; i < allocInfo.MaxX; i++)
            {
                for (int j = 0; j < allocInfo.MaxY; j++)
                {
                    if (aryUnitUf[i, j].UnitInfo != null)
                    { Dispatcher.Invoke(new Action(() => { aryUnitUf[i, j].IsChecked = false; })); }
                }
            }

            if (sender != null)
            {
                System.Threading.Thread.Sleep(600);
                releaseButton(sender);
            }
        }
        
        //private void btnResetResult_Click(object sender, RoutedEventArgs e)
        //{
        //    if (sender != null)
        //    { actionButton(sender, "Reset Result"); }            

        //    for (int i = 0; i < allocInfo.MaxX; i++)
        //    {
        //        for (int j = 0; j < allocInfo.MaxY; j++)
        //        {
        //            Dispatcher.Invoke(new Action(() => { aryUnitUf[i, j].Background = UnitDefaultBrush; }));
        //        }
        //    }

        //    if (sender != null)
        //    {
        //        System.Threading.Thread.Sleep(600);
        //        releaseButton(sender);
        //    }
        //}

        private void UnitToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            // Unitを一つしか選択できないようにする処理
            if (rbUnitUFAdjust.IsChecked == true || rbCellUFAdjust.IsChecked == true)// || rbCellReplace.IsChecked == true)
            {
                //btnResetResult_Click(null, null);
                btnDeselectAll_Click(null, null);

                ((UnitToggleButton)sender).Checked -= UnitToggleButton_Checked;
                ((UnitToggleButton)sender).IsChecked = true;
                ((UnitToggleButton)sender).Checked += UnitToggleButton_Checked;
            }
        }

        private void CellToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            diselectAllCells();

            ((UnitToggleButton)sender).Checked -= CellToggleButton_Checked;
            ((UnitToggleButton)sender).IsChecked = true;
            ((UnitToggleButton)sender).Checked += CellToggleButton_Checked;
        }
        
        private Point startPos, endPos;

        private void utbUf_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            rectAreaUf.Visibility = System.Windows.Visibility.Visible;
            rectAreaUf.Margin = new Thickness(e.GetPosition(this).X - 2, e.GetPosition(this).Y - 48, 0, 0);

            startPos = new Point(rectAreaUf.Margin.Left, rectAreaUf.Margin.Top);
        }

        private void utbUf_PreviewMouseUp(object sender, MouseButtonEventArgs e)
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

            double offsetX = tcMain.Margin.Left + gdUniformity.Margin.Left + gdAllocUf.Margin.Left + gdAllocUfLayout.Margin.Left + cabinetAllocColumnHeaderWidth - svUniformity.HorizontalOffset;
            double offsetY = tcMain.Margin.Top + gdUniformity.Margin.Top + gdAllocUf.Margin.Top + gdAllocUfLayout.Margin.Top + cabinetAllocRowHeaderHeight - svUniformity.VerticalOffset;

            for (int y = 0; y < allocInfo.MaxY; y++)
            {
                for (int x = 0; x < allocInfo.MaxX; x++)
                {
                    if(aryUnitUf[x, y].IsEnabled == false)
                    { continue; }

                    if (isContainedDragArea(aryUnitUf[x, y], area, offsetX, offsetY) == true)
                    {
                        if(aryUnitUf[x, y].IsChecked == true)
                        { aryUnitUf[x, y].IsChecked = false; }
                        else
                        { aryUnitUf[x, y].IsChecked = true; }
                    }
                }
            }

            rectAreaUf.Visibility = System.Windows.Visibility.Hidden;
        }

        private void utbUf_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            double height = e.GetPosition(this).Y - 48 - startPos.Y;
            try
            {
                if (height >= 0)
                { rectAreaUf.Height = height; }
                else
                {
                    rectAreaUf.Margin = new Thickness(rectAreaUf.Margin.Left, startPos.Y + height, 0, 0);
                    rectAreaUf.Height = Math.Abs(height);
                }
            }
            catch { rectAreaUf.Height = 0; }

            double width = e.GetPosition(this).X - 2 - startPos.X;
            try
            {
                if (width >= 0)
                { rectAreaUf.Width = width; }
                else
                {
                    rectAreaUf.Margin = new Thickness(startPos.X + width, rectAreaUf.Margin.Top, 0, 0);
                    rectAreaUf.Width = Math.Abs(width);
                }
            }
            catch { rectAreaUf.Width = 0; }
        }

        #endregion Events

        #region Common

        #endregion Common

        #region Private Methods

        #region CA-410 Control Sub Function

        #region OPEN/CLOSE

        bool openSubDispatch(Int32 id, Int32 noOfProbes, Int32 portVal)
        {            
            var dispatcher = Application.Current.Dispatcher;
            return dispatcher.Invoke(() => openSub(id, noOfProbes, portVal));             
        }

        /// <summary>
        /// CA-410のラジオボタンを全てOn表示にする
        /// </summary>
        void DoSetOnCA410RadioButton()
        {
            rbCA410RemoteOn.Checked -= rbCA410RemoteOn_Checked;
            rbCA410RemoteOn.IsChecked = true;
            rbCA410RemoteOn.Checked += rbCA410RemoteOn_Checked;

            rbCA410RemoteOnMeas.Checked -= rbCA410RemoteOn_Checked;
            rbCA410RemoteOnMeas.IsChecked = true;
            rbCA410RemoteOnMeas.Checked += rbCA410RemoteOn_Checked;

            rbCA410RemoteOnCalib.Checked -= rbCA410RemoteOn_Checked;
            rbCA410RemoteOnCalib.IsChecked = true;
            rbCA410RemoteOnCalib.Checked += rbCA410RemoteOn_Checked;

            rbCA410RemoteOnRelTgt.Checked -= rbCA410RemoteOn_Checked;
            rbCA410RemoteOnRelTgt.IsChecked = true;
            rbCA410RemoteOnRelTgt.Checked += rbCA410RemoteOn_Checked;

            rbCA410RemoteOnGain.Checked -= rbCA410RemoteOn_Checked;
            rbCA410RemoteOnGain.IsChecked = true;
            rbCA410RemoteOnGain.Checked += rbCA410RemoteOn_Checked;
        }

        /// <summary>
        /// CA-410のラジオボタンを全てOff表示にする
        /// </summary>
        void DoSetOffCA410RadioButton()
        {
            rbRemoteOff.Checked -= rbRemoteOff_Checked;
            rbRemoteOff.IsChecked = true;
            rbRemoteOff.Checked += rbRemoteOff_Checked;

            rbRemoteOffMeas.Checked -= rbRemoteOff_Checked;
            rbRemoteOffMeas.IsChecked = true;
            rbRemoteOffMeas.Checked += rbRemoteOff_Checked;

            rbRemoteOffCalib.Checked -= rbRemoteOff_Checked;
            rbRemoteOffCalib.IsChecked = true;
            rbRemoteOffCalib.Checked += rbRemoteOff_Checked;

            rbRemoteOffRelTgt.Checked -= rbRemoteOff_Checked;
            rbRemoteOffRelTgt.IsChecked = true;
            rbRemoteOffRelTgt.Checked += rbRemoteOff_Checked;

            rbRemoteOffGain.Checked -= rbRemoteOff_Checked;
            rbRemoteOffGain.IsChecked = true;
            rbRemoteOffGain.Checked += rbRemoteOff_Checked;
        }

        bool openSub(Int32 id, Int32 noOfProbes, Int32 portVal)
        {
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[0] openSub start"); }

            if (caSdk.isOpened() == true)
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[1] caSdk.isOpened == true"); }

                if (setRemoteModeSub(CaSdk.RemoteMode.RemoteLock) != true)
                {
                    DoSetOffCA410RadioButton();

                    return false;
                }
                else
                {
                    DoSetOnCA410RadioButton();

                    showStatusTextBlock("DONE");
                    return true;
                }
            }

            showStatusTextBlock("CA-SDK OPEN");
            doEvents();

            // Open
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[1] caSdk.open()"); }

            bool status = caSdk.open(id, noOfProbes, Settings.Ins.InitialRetry, portVal);

            if (status == true)
            {
                DoSetOnCA410RadioButton();
            }
            else
            {
                DoSetOffCA410RadioButton();

                return false;
            }

            // 0 Cal
            if (analyzer == ColorAnalyzerModel.CA410)
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[CA-410] caSdk.calZero()"); }

                status = caSdk.calZero();
            }

            // Probe No.
            if (status == true)
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[2] caSdk.getNumberOfProbes()"); }

                Int32 nOfProbes;
                showStatusTextBlock("GET number of Probes");
                status = caSdk.getNumberOfProbes(out nOfProbes);
                if (status == true)
                {
                    cmbxProbeNo.Items.Clear();
                    for (Int32 i = 0; i < nOfProbes; i++)
                        cmbxProbeNo.Items.Add((i + 1).ToString());
                    cmbxProbeNo.SelectedIndex = 0;
                }
            }

            // Sync Mode
            if (status == true)
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[3] getSyncModeSub()"); }

                float syncMode;
                showStatusTextBlock("GET SYNC MODE");
                //Application.DoEvents();
                status = getSyncModeSub(out syncMode);
                if (status == true)
                {
                    syncModeAutoChenge = false;
                    cmbxSyncMode.SelectedIndex = (Int32)syncMode;
                    syncModeAutoChenge = true;
                }
            }

            // Display Mode
            if (status == true)
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[4] getDisplayModeSub()"); }

                CaSdk.DisplayMode displayMode;
                showStatusTextBlock("GET DISPALY MODE");
                //Application.DoEvents();
                status = getDisplayModeSub(out displayMode);
                if (status == true)
                {
                    displayModeAutoChange = false;
                    cmbxDisplayMode.SelectedIndex = (Int32)displayMode;
                    setDisplayModeText(displayMode);
                    displayModeAutoChange = true;
                }
            }

            // Display Digits
            if (status == true)
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[5] getDisplayDigitsSub()"); }

                CaSdk.DisplayDigits displayDigits;
                showStatusTextBlock("GET DISPALY DIGITS");
                //Application.DoEvents();
                status = getDisplayDigitsSub(out displayDigits);
                if (status == true)
                {
                    displayDigitsAutoChange = false;
                    cmbxDisplayDigits.SelectedIndex = (Int32)displayDigits;
                    displayDigitsAutoChange = true;
                }
            }

            // Averaging Mode
            if (status == true)
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[6] getAveragingModeSub()"); }

                CaSdk.AveragingMode averagingMode;
                showStatusTextBlock("GET AVERAGING MODE");
                //Application.DoEvents();
                status = getAveragingModeSub(out averagingMode);
                if (status == true)
                {
                    averagingModeAutoChange = false;
                    cmbxAveragingMode.SelectedIndex = (Int32)averagingMode;
                    averagingModeAutoChange = true;
                }
            }

            // Brightness Unit
            if (status == true)
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[7] getBrightnessCabinetSub()"); }

                CaSdk.BrightnessUnit brightnessUnit;
                showStatusTextBlock("GET BRIGHTNESS CABINET");
                //Application.DoEvents();
                status = getBrightnessUnitSub(out brightnessUnit);
                if (status == true)
                {
                    brightnessUnitAutoChange = false;
                    cmbxBrightnessUnit.SelectedIndex = (Int32)brightnessUnit;
                    brightnessUnitAutoChange = true;
                }
            }

            // Channel
            if (status == true)
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[8] getChannelSub()"); }

                Int32 channel;
                showStatusTextBlock("GET CHANNEL");
                status = getChannelSub(out channel);
                if (status == true)
                {
                    channelAutoChange = false;
                    cmbxChannel.SelectedIndex = channel;
                    channelAutoChange = true;
                }
            }

            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }
            else
            { showStatusTextBlock("DONE"); }

            return status;
        }

        bool closeSub()
        {
            if (caSdk != null)
            { caSdk.close(); }

            DoSetOffCA410RadioButton();

            return true;
        }

        #endregion OPEN/CLOSE

        #region Ca object

        bool setSyncModeSub(float syncMode)
        {
            if (caSdk.isOpened() != true)
            {
                //txtbStatus.Text = "Device is not opened";
                return false;
            }

            bool status = caSdk.setSyncMode(syncMode);
            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }

            return status;
        }

        bool getSyncModeSub(out float syncMode)
        {
            if (caSdk.isOpened() != true)
            {
                //txtbStatus.Text = "Device is not opened";
                syncMode = 0;
                return false;
            }
            bool status = caSdk.getSyncMode(out syncMode);
            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }

            return status;
        }

        bool setDisplayModeSub(CaSdk.DisplayMode displayMode)
        {
            if (caSdk.isOpened() != true)
            {
                showStatusTextBlock("Device is not opened");
                return false;
            }
            bool status = caSdk.setDisplayMode(displayMode);
            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }
            else
            { setDisplayModeText(displayMode); }

            return status;
        }

        bool getDisplayModeSub(out CaSdk.DisplayMode displayMode)
        {
            if (caSdk.isOpened() != true)
            {
                showStatusTextBlock("Device is not opened");
                displayMode = 0;
                return false;
            }

            bool status = caSdk.getDisplayMode(out displayMode);
            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }
            else
            { setDisplayModeText(displayMode); }

            return status;
        }

        bool setDisplayDigitsSub(CaSdk.DisplayDigits displayDigits)
        {
            if (caSdk.isOpened() != true)
            {
                showStatusTextBlock("Device is not opened");
                return false;
            }

            bool status = caSdk.setDisplayDigits(displayDigits);
            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }

            return status;
        }

        bool getDisplayDigitsSub(out CaSdk.DisplayDigits displayDigits)
        {
            if (caSdk.isOpened() != true)
            {
                showStatusTextBlock("Device is not opened");
                displayDigits = 0;
                return false;
            }

            bool status = caSdk.getDisplayDigits(out displayDigits);
            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }

            return status;
        }

        bool setAveragingModeSub(CaSdk.AveragingMode averagingMode)
        {
            if (caSdk.isOpened() != true)
            {
                showStatusTextBlock("Device is not opened");
                return false;
            }

            bool status = caSdk.setAveragingMode(averagingMode);
            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }

            return status;
        }

        bool getAveragingModeSub(out CaSdk.AveragingMode averagingMode)
        {
            if (caSdk.isOpened() != true)
            {
                showStatusTextBlock("Device is not opened");
                averagingMode = 0;
                return false;
            }
            bool status = caSdk.getAveragingMode(out averagingMode);
            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }

            return status;
        }

        bool setBrightnessUnitSub(CaSdk.BrightnessUnit brightnessUnit)
        {
            if (caSdk.isOpened() != true)
            {
                showStatusTextBlock("Device is not opened");
                return false;
            }
            bool status = caSdk.setBrightnessUnit(brightnessUnit);
            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }

            return status;
        }

        bool getBrightnessUnitSub(out CaSdk.BrightnessUnit brightnessUnit)
        {
            if (caSdk.isOpened() != true)
            {
                showStatusTextBlock("Device is not opened");
                brightnessUnit = 0;
                return false;
            }
            bool status = caSdk.getBrightnessUnit(out brightnessUnit);
            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }

            return status;
        }

        bool setRemoteModeSub(CaSdk.RemoteMode remoteMode)
        {
            bool status = true;
            try
            {
                status = caSdk.setRemoteMode(remoteMode);
                if (status != true)
                { showStatusTextBlock(caSdk.getErrorMessage()); }
            }
            catch (Exception ex)
            {
                showStatusTextBlock(ex.Message);
                status = false;
            }
            return status;
        }

        //bool zeroCalibrationSub()
        //{
        //    bool status;
        //    while (true)
        //    {
        //        status = true;
        //        ZeroCalibrationInfo info = new ZeroCalibrationInfo();
        //        info.Owner = this;
        //        if (info.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
        //        {
        //            txtbStatus.Text = "ABORT";
        //            return false;
        //        }
        //        status = setRemoteModeSub(CaSdk.RemoteMode.RemoteLock);
        //        if (status != true)
        //        {
        //            txtbStatus.Text = caSdk.getErrorMessage();
        //            return false;
        //        }
        //        else
        //        {
        //            status = caSdk.calZero();
        //            if (status != true)
        //                txtbStatus.Text = caSdk.getErrorMessage();
        //        }
        //        if (status == true)
        //            break;
        //    }
        //    return status;
        //}

        bool measureSub()
        {
            if (caSdk.isOpened() != true)
            {
                showStatusTextBlock("Device is not opened");
                return false;
            }

            bool status = caSdk.measure();
            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }
            else
            { showStatusTextBlock(""); }

            return status;
        }

        bool enterSub()
        {
            if (caSdk.isOpened() != true)
            { return false; }

            bool status = caSdk.enter();
            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }

            return status;
        }

        bool setLvxyCalModeSub()
        {
            if (caSdk.isOpened() != true)
            { return false; }

            bool status = caSdk.setLvxyCalMode();
            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }

            return status;
        }

        bool resetLvxyCalModeSub()
        {
            if (caSdk.isOpened() != true)
            { return false; }

            bool status = caSdk.resetLvxyCalMode();
            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }

            return status;
        }

        bool setLvxyCalDataSub(CaSdk.ColorNo color, float x, float y, float Lv)
        {
            if (caSdk.isOpened() != true)
            { return false; }

            bool status = caSdk.setLvxyCalData(color, x, y, Lv);
            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }

            return status;
        }

        bool setDisplayProbeSub(Int32 probeNo)
        {
            if (caSdk.isOpened() != true)
            {
                showStatusTextBlock("Device is not opened");
                return false;
            }

            bool status = caSdk.setDisplayProbe(probeNo + 1);
            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }

            return status;
        }

        #endregion Ca object

        #region Momory object

        bool setChannelSub(Int32 channel)
        {
            if (caSdk.isOpened() != true)
            {
                showStatusTextBlock("Device is not opened");
                return false;
            }

            bool status = caSdk.setChannelNo(channel);
            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }

            return status;
        }

        bool getChannelSub(out Int32 channel)
        {
            if (caSdk.isOpened() != true)
            {
                showStatusTextBlock("Device is not opened");
                channel = 0;
                return false;
            }

            bool status = caSdk.getChannelNo(out channel);
            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }

            return status;
        }

        bool checkCalDataSub(Int32 probeNo, String fileName)
        {
            if (caSdk.isOpened() != true)
            {
                showStatusTextBlock("Device is not opened");
                return false;
            }

            bool status = caSdk.checkCalData(probeNo + 1, fileName);
            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }

            return status;
        }

        bool copyToFileSub(Int32 probeNo, String fileName)
        {
            if (caSdk.isOpened() != true)
            {
                showStatusTextBlock("Device is not opened");
                return false;
            }

            bool status = caSdk.copyToFile(probeNo + 1, fileName);
            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }

            return status;
        }

        bool copyFromFileSub(Int32 probeNo, String fileName)
        {
            if (caSdk.isOpened() != true)
            {
                showStatusTextBlock("Device is not opened");
                return false;
            }

            bool status = caSdk.copyFromFile(probeNo + 1, fileName);
            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }

            return status;
        }

        #endregion Momory object

        #region Probes correction

        bool getNumberOfProbesSub(out Int32 noOfProbes)
        {
            if (caSdk.isOpened() != true)
            {
                showStatusTextBlock("Device is not opened");
                noOfProbes = 0;
                return false;
            }

            bool status = caSdk.getNumberOfProbes(out noOfProbes);
            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }

            return status;
        }

        #endregion Probes correction

        #region Probe Object

        bool getDataSub(out Double data1, out Double data2, out Double data3, Int32 probeNo)
        {
            if (caSdk.isOpened() != true)
            {
                showStatusTextBlock("Device is not opened");
                data1 = 0;
                data2 = 0;
                data3 = 0;
                return false;
            }

            bool status = caSdk.getData(out data1, out data2, out data3, probeNo);
            //if (cmbxChannel.SelectedIndex == 0)
            //{
            //    if (caSdk.getErrorNo() == 1)
            //        status = true;
            //}
            if (status != true)
            {
                if(caSdk.getErrorNo() == 1)
                { status = true; }
            }

            // added by Hotta 2015/07/10
            if (Settings.Ins.DarkErrorEnable == 0 && caSdk.getErrorNo() == 5)
            { status = true; }
            //

            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }

            //double data_1 = data1, data_2 = data2, data_3 = data3;
            //Dispatcher.Invoke(new Action(() => { txbData1.Text = data_1.ToString(); }));
            //Dispatcher.Invoke(new Action(() => { txbData2.Text = data_2.ToString(); }));
            //Dispatcher.Invoke(new Action(() => { txbData3.Text = data_3.ToString(); }));

            return status;
        }

        bool getProbeSerialSub(out String serial, Int32 probeNo)
        {
            if (caSdk.isOpened() != true)
            {
                showStatusTextBlock("Device is not opened");
                serial = "";
                return false;
            }

            bool status = caSdk.getProbeSerial(out serial, probeNo);
            if (status != true)
            { showStatusTextBlock(caSdk.getErrorMessage()); }

            return status;
        }

        #endregion Probe Object
        
        #region Change Color Analyzer

        #region Fields

        static bool regUpdated = false;
        static bool foundRegUpdated = false;
        //static bool comChangeComplete = false;
        //System.IntPtr hDevice;   // デバイスハンドル

        public const int WM_LBUTTONDOWN = 0x201;
        public const int WM_LBUTTONUP = 0x202;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int MK_LBUTTON = 0x0001;
        public static int GWL_STYLE = -16;
        public const int BM_GETCHECK = 0x00F0;
        
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, uint Msg, uint wParam, uint lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr hWnd, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32")]
        private static extern bool EnumWindows(WNDENUMPROC lpEnumFunc, IntPtr lParam);

        // EnumWindowsから呼び出されるコールバック関数WNDENUMPROCのデリゲート
        private delegate bool WNDENUMPROC(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32")]
        private static extern bool IsWindowVisible(IntPtr hWnd);
                
        [DllImport("user32")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        #endregion

        private void changeComInterface(string filePath, ColorAnalyzerModel model)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = filePath; //@"D:\Documents\CLED\CLED Controller\IR09.1\SW\CPU\PKG_011\mfgtools_011\MfgTool2.exe";
            proc.Start();

            System.Threading.Thread.Sleep(2000);
            proc.WaitForInputIdle();

            closeCompleteWindowAsync(proc);
            //Task task = new Task(() => { closeCompleteWindowAsync(proc); });
            //task.Start();
                        
            proc.Close();
            proc.Dispose();
                        
            return;
        }

        private static IntPtr FindTargetRadioButton(ChangeComWindow top, string title)
        {
            IntPtr ptr = new IntPtr();

            var all = GetAllChildWindows(top, new List<ChangeComWindow>());
            foreach (var window in all)
            {
                if (window.ClassName.Contains("BUTTON") && window.Title == title)
                {
                    ptr = window.hWnd;
                    break;
                }
            }

            return ptr;
        }

        private static IntPtr FindTargetButton(ChangeComWindow top, string title)
        {
            IntPtr ptr = new IntPtr();

            var all = GetAllChildWindows(top, new List<ChangeComWindow>());
            foreach (var window in all)
            {
                if (window.ClassName.Contains("BUTTON") && window.Title == title)
                {
                    ptr = window.hWnd;
                    break;
                }
            }

            return ptr;
        }

        private static WriteStatus checkStatus(ChangeComWindow top)
        {
            var all = GetAllChildWindows(top, new List<ChangeComWindow>());

            if (all[4].ClassName == "Static" && all[4].Title == "1" && all[6].Title == "0")
            { return WriteStatus.Complete; }
            else if (all[6].ClassName == "Static" && all[6].Title == "1")
            { return WriteStatus.Fail; }

            return WriteStatus.Writing;
        }

        private static void closeComToolWindow()
        {
            try
            {
                Process[] allProcesses = Process.GetProcesses();

                foreach (Process process in allProcesses)
                {
                    var all = GetAllChildWindows(GetWindow(process.MainWindowHandle), new List<ChangeComWindow>());

                    for (int i = 0; i < all.Count; i++)
                    {
                        if (all[i].Title != null && all[i].Title.Contains("COM Registration Tool") == true)
                        {
                            if (process.CloseMainWindow() != true)
                            {
                                process.Kill();
                                break;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void closeCompleteWindowAsync(Process proc)
        {
            //System.Threading.Thread.Sleep(1000);

            regUpdated = false;
            //comChangeComplete = false;

            while(true) // 終了条件を満たすまで無限ループ
            {
                System.Threading.Thread.Sleep(300);

                // Close Complete Window
                if (closeCompleteWindow(proc) == true)
                { break; }
            }
        }

        private bool closeCompleteWindow(Process proc)
        {
            try
            {
                // COM Registration Toolが閉じられている場合
                bool foundComRegTool = false;
                Process[] allProcesses = Process.GetProcesses();

                foreach (Process process in allProcesses)
                {
                    if (process.Id == proc.Id)
                    {
                        foundComRegTool = true;
                        break;
                    }
                }

                if(foundComRegTool == false)
                { return true; }

                // Registry Updatedが表示されている場合
                foundRegUpdated = false;
                EnumWindows(EnumerateWindow, IntPtr.Zero);

                if (regUpdated == true && foundRegUpdated == false)
                { return true; }
            }
            catch { }

            return false;
        }

        // ウィンドウを列挙するためのコールバックメソッド
        private static bool EnumerateWindow(IntPtr hWnd, IntPtr lParam)
        {
            // ウィンドウが可視かどうか調べる
            if (IsWindowVisible(hWnd))
            { checkCaptionAndProcess(hWnd); }

            return true;
        }

        private static void checkCaptionAndProcess(IntPtr hWnd)
        {
            // ウィンドウのキャプションを取得・表示
            StringBuilder caption = new StringBuilder(0x1000);

            GetWindowText(hWnd, caption, caption.Capacity);
            
            // ウィンドウハンドルからプロセスIDを取得
            int processId;

            GetWindowThreadProcessId(hWnd, out processId);

            // プロセスIDからProcessクラスのインスタンスを取得
            Process p = Process.GetProcessById(processId);

            if (caption.ToString() == "" && p.ProcessName == "COM_Registration_Tool")
            {
                foundRegUpdated = true;
                regUpdated = true;
            }
        }

        //private void tmrCloseComplete_Tick(object sender, EventArgs e)
        //{
        //    // Close Complete Window
        //    if (closeCompleteWindow() == true)
        //    { comChangeComplete = true; }
        //}

        // 指定したウィンドウの全ての子孫ウィンドウを取得し、リストに追加する
        private static List<ChangeComWindow> GetAllChildWindows(ChangeComWindow parent, List<ChangeComWindow> dest)
        {
            dest.Add(parent);
            EnumChildWindows(parent.hWnd).ToList().ForEach(x => GetAllChildWindows(x, dest));
            return dest;
        }

        // 与えた親ウィンドウの直下にある子ウィンドウを列挙する（孫ウィンドウは見つけてくれない）
        private static IEnumerable<ChangeComWindow> EnumChildWindows(IntPtr hParentWindow)
        {
            IntPtr hWnd = IntPtr.Zero;
            while ((hWnd = FindWindowEx(hParentWindow, hWnd, null, null)) != IntPtr.Zero) { yield return GetWindow(hWnd); }
        }

        // ウィンドウハンドルを渡すと、ウィンドウテキスト（ラベルなど）、クラス、スタイルを取得してWindowsクラスに格納して返す
        private static ChangeComWindow GetWindow(IntPtr hWnd)
        {
            int textLen = GetWindowTextLength(hWnd);
            string windowText = null;
            if (0 < textLen)
            {
                //ウィンドウのタイトルを取得する
                StringBuilder windowTextBuffer = new StringBuilder(textLen + 1);
                GetWindowText(hWnd, windowTextBuffer, windowTextBuffer.Capacity);
                windowText = windowTextBuffer.ToString();
            }

            //ウィンドウのクラス名を取得する
            StringBuilder classNameBuffer = new StringBuilder(256);
            GetClassName(hWnd, classNameBuffer, classNameBuffer.Capacity);

            // スタイルを取得する
            int style = GetWindowLong(hWnd, GWL_STYLE);
            return new ChangeComWindow() { hWnd = hWnd, Title = windowText, ClassName = classNameBuffer.ToString(), Style = style };
        }

        #endregion

        #endregion CA-410 Control Sub Function

        // Standard/Strict用アルゴリズム
        private bool adjustUf(UfAdjustMode mode)
        {
            if (Settings.Ins.ExecLog == true)
            {
                SaveExecLog("");
                SaveExecLog("[0] adjustUf start (" + mode.ToString() + ")");
            }

            List<ControllerInfo> lstTargetController = new List<ControllerInfo>();
            List<UnitInfo> lstTargetUnit = new List<UnitInfo>();
            double[][][] measureData = null;
            bool status;
            List<UserSetting> lstUserSetting;
            List<MoveFile> lstMoveFile = new List<MoveFile>();
            Dictionary<int, int> dicFileCount = new Dictionary<int, int>();
            FileDirectory baseFileDir;

            // ●進捗の最大値を設定 [1]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[1] Set Step Count."); }
            winProgress.ShowMessage("Set Step Count.");

            int step = AJUSTMENT_UNIFORMITY_STEPS + dicController.Count;
            winProgress.SetWholeSteps(step);

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

            var dispatcher = Application.Current.Dispatcher;
            dispatcher.Invoke(() => correctAdjustUnit(out lstTargetUnit, allocInfo.MaxX, allocInfo.MaxY));
            if (lstTargetUnit.Count == 0)
            {
                ShowMessageWindow("Please select cabinets.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●対象Cabinetが接続されているController情報を保持する [4]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[4] Store Target Controller(s) Info."); }
            winProgress.ShowMessage("Store Target Controller(s) Info.");

            // 初期化
            foreach (ControllerInfo cont in dicController.Values)
            { cont.Target = false; }

            foreach (UnitInfo unit in lstTargetUnit)
            {
                if (lstTargetController.Contains(dicController[unit.ControllerID]) != true)
                {
                    dicController[unit.ControllerID].Target = true;
                    lstTargetController.Add(dicController[unit.ControllerID]);
                }
            }
            winProgress.PutForward1Step();

            // ●Controllerごとのファイル数をリセット [5]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[5] Reset File Count."); }
            winProgress.ShowMessage("Reset File Count.");

            foreach (ControllerInfo cont in dicController.Values)
            { dicFileCount.Add(cont.ControllerID, 0); }
            winProgress.PutForward1Step();

            // ●調整データのバックアップがすべてあるか確認 [6]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[6] Check Backup Data."); }
            winProgress.ShowMessage("Check Backup Data.");

            status = checkDataFile(lstTargetUnit, out baseFileDir);
            if (status != true)
            {
                ShowMessageWindow("There is not the module data file.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            foreach (ControllerInfo controller in lstTargetController)
            {
                // ●Controller ID [*1]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*1] (Controller Loop) ControllerID : " + controller.ControllerID.ToString()); }
                winProgress.ShowMessage("ControllerID : " + controller.ControllerID.ToString());

                // ●FTP ON [*2]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*2] FTP On."); }
                winProgress.ShowMessage("FTP On.");

                if (sendSdcpCommand(SDCPClass.CmdFtpOn, controller.IPAddress) != true)
                { return false; }

                // ●Model名取得 [*3]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*3] Get Model Name."); }
                winProgress.ShowMessage("Get Model Name.");

                if (string.IsNullOrWhiteSpace(controller.ModelName) == true)
                { controller.ModelName = getModelName(controller.IPAddress); }

                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("     Model Name : " + controller.ModelName); }

                // ●Serial取得 [*4]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*4] Get Serial No."); }
                winProgress.ShowMessage("Get Serial No.");

                if (string.IsNullOrWhiteSpace(controller.SerialNo) == true)
                { controller.SerialNo = getSerialNo(controller.IPAddress); }

                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("     Serial Num. : " + controller.SerialNo); }

                // Measure Onlyの場合はTempフォルダのファイルを削除しない
                if (mode != UfAdjustMode.MeasureOnly)
                {
                    // Tempフォルダ内のファイルを削除
                    string tempPath = applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo + "_Temp";

                    if (Directory.Exists(tempPath) != true)
                    { Directory.CreateDirectory(tempPath); }

                    string[] files = Directory.GetFiles(tempPath, "*", System.IO.SearchOption.AllDirectories);

                    for (int i = 0; i < files.Length; i++)
                    {
                        try
                        {
                            if (Settings.Ins.ExecLog == true)
                            {
                                SaveExecLog("Delete : " + files[i]);
                            }
                            File.Delete(files[i]);
                        }
                        catch { return false; }
                    }
                }
            }
            winProgress.PutForward1Step();

#if NO_MEASURE
#else
            // ●Open CA-410 [7]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[7] Open CA-410."); }
            winProgress.ShowMessage("Open CA-410.");

            status = openSubDispatch(1, Settings.Ins.NoOfProbes, Settings.Ins.PortVal);
            if (status != true)
            {
                ShowMessageWindow("Can not open CA-410.\r\nPlease check it status.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●CA-410設定 [8]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[8] Set CA-410 Settings."); }
            winProgress.ShowMessage("Set CA-410 Settings.");

            if (setCA410SettingDispatch() != true)
            {
                ShowMessageWindow("Can not set CA-410 settings.\r\nPlease check it status.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();
#endif

            // ●User設定保存 [9]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[9] Store User Settings."); }
            winProgress.ShowMessage("Store User Settings.");

            try
            {
                if (getUserSetting(out lstUserSetting) != true)
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

            // ●調整用設定 [10]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[10] Set Adjust Settings."); }
            winProgress.ShowMessage("Set Adjust Settings.");

            setAdjustSetting();
            winProgress.PutForward1Step();

            // ●Layout情報Off
            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdDispUnitAddrOff, 0, cont.IPAddress); }

            // ●Cabinet調整
            UserOperation operation = UserOperation.None;

            //foreach (UnitInfo unit in lstTargetUnit)
            for (int i = 0; i < lstTargetUnit.Count; i++)
            {
                UnitInfo unit = lstTargetUnit[i];

                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*1] (Cabinet Loop) PortNo. : " + unit.PortNo.ToString() + ", CabinetNo. : " + unit.UnitNo.ToString()); }

                if (unit == null)
                { continue; }

                double targetYw, targetYr, targetYg, targetYb;
                int ucr, ucg, ucb;

                // ●色度測定 [*1]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*2] Measure Cabinet Color."); }
                winProgress.ShowMessage("Measure Cabinet Color." + "    (Cabinets: " + (i + 1) + "/" + lstTargetUnit.Count + ")");

                while (true)
                {
                    if (mode == UfAdjustMode.Std || mode == UfAdjustMode.MeasureOnly)
                    { status = measure4Points(unit, out measureData, out operation); }
                    else if (mode == UfAdjustMode.EachCell)
                    { status = measureAllCells(unit, out measureData, out operation); }
                    else
                    { status = false; }
                    
                    if (status == true || operation == UserOperation.Rewind || operation == UserOperation.Repeat)
                    { break; }
                    else if(operation == UserOperation.Cancel)
                    { return false; }
                    else
                    {
                        bool? result;
                        string msg = "Color measurement failed.\r\nDo you want to retry the measurement or skip ?";                        

                        showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 400, 180, "Retry", "Skip");

                        if (result == true)
                        { continue; }
                        else
                        {
                            operation = UserOperation.Skip;
                            break;
                        }
                    }
                }

                // Rewindを選択した場合は後の処理をスキップして前のUnitに戻る
                if (operation == UserOperation.Rewind)
                {
                    if (i == 0)
                    { i -= 1; }
                    else
                    {
                        lstMoveFile.RemoveAt(lstMoveFile.Count - 1); // 最後に追加した移動ファイルを削除
                        i -= 2;
                    }

                    continue;
                }
                else if (operation == UserOperation.Repeat)
                {
                    i -= 1;
                    continue;
                }
                else if (operation == UserOperation.Skip)
                { continue; }

                string filePath = makeFilePath(unit, baseFileDir);
                // ●クロストーク補正量算出 [*]
                if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
                {
                    if (Settings.Ins.ExecLog == true)
                    { SaveExecLog("[*] Calc Crosstalk Correction."); }
                    winProgress.ShowMessage("Calc Crosstalk Correction.");

                    if (mode == UfAdjustMode.Std || mode == UfAdjustMode.MeasureOnly)
                    {
                        status = m_MakeUFData.GetUncCrosstalk(filePath, measureData);
                    }
                    else // UfAdjustMode.EachCell
                    {
                        List<int> lstTargetCell = Enumerable.Range(0, measureData.Length).ToList();
                        status = m_MakeUFData.CalcUncCrosstalk(lstTargetCell, measureData);
                    }

                    if (!status)
                    {
                        ShowMessageWindow("Failed in crosstalk correction calc.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                        return false;
                    }
                }

                // ●目標値設定 [*3]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*3] Set Target Color."); }
                winProgress.ShowMessage("Set Target Color.");

                m_MakeUFData.SetTargetValue(ufTargetChrom.Red.x, ufTargetChrom.Red.y, ufTargetChrom.Green.x, ufTargetChrom.Green.y, ufTargetChrom.Blue.x, ufTargetChrom.Blue.y, ufTargetChrom.White.Lv, ufTargetChrom.White.x, ufTargetChrom.White.y);
                //m_MakeUFData.SetTargetValue(0.695, 0.304, 0.173, 0.740, 0.137, 0.053, 901.0 / 5, 0.324, 0.324);

                // ●Cell Dataの補正データ抽出 [*4]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*4] Extract Correction Data."); }
                winProgress.ShowMessage("Extract Correction Data.");

                status = m_MakeUFData.ExtractFmt(filePath, allocInfo.LEDModel);
                if (status != true)
                {
                    ShowMessageWindow("Failed in ExtractFmt.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }

                // ●抽出した補正データと調整目標値から逆算してその時のXYZの値を求める [*4]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*5] Calc XYZ."); }
                winProgress.ShowMessage("Calc XYZ.");

                //status = m_MakeUFData.Fmt2XYZ(m_Target_xr, m_Target_yr, m_Target_xg, m_Target_yg, m_Target_xb, m_Target_yb, m_Target_Yw, m_Target_xw, m_Target_yw);
                status = m_MakeUFData.Fmt2XYZ(ufTargetChrom.Red.x, ufTargetChrom.Red.y, ufTargetChrom.Green.x, ufTargetChrom.Green.y, ufTargetChrom.Blue.x, ufTargetChrom.Blue.y, ufTargetChrom.White.Lv, ufTargetChrom.White.x, ufTargetChrom.White.y);
                //status = m_MakeUFData.Fmt2XYZ(0.695, 0.304, 0.173, 0.740, 0.137, 0.053, 901.0 / 5, 0.324, 0.324);
                if (status != true)
                {
                    ShowMessageWindow("Failed in Fmt2XYZ.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }

                // ●補正したときの温度データを取得
                // Setする場合は、m_CellTemperatureに値を代入する。
                // 仮に、温度データは、オリジナルを流用する。
                //if(status == true)
                //{ status = m_MakeUFData.GetCellTemperature(); }

                // ●今回の計測値と前の計測値の差分を求めて、画素ごとのXYZデータを更新する [*5]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*6] Calc New XYZ Data."); }
                winProgress.ShowMessage("Calc New XYZ Data.");

                if (mode == UfAdjustMode.Std || mode == UfAdjustMode.MeasureOnly)
                { status = m_MakeUFData.Compensate_XYZ(measureData, CMakeUFData.MeasureMode.Cross4Point); }
                else if (mode == UfAdjustMode.EachCell)
                { status = m_MakeUFData.Compensate_XYZ(measureData); }
                else
                { status = false; }

                if (status != true)
                {
                    ShowMessageWindow("Failed in Compensate_XYZ.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }

                // 目標値を再設定
                //m_MakeUFData.SetTargetValue(ufTargetChrom.Red.x, ufTargetChrom.Red.y, ufTargetChrom.Green.x, ufTargetChrom.Green.y, ufTargetChrom.Blue.x, ufTargetChrom.Blue.y, ufTargetChrom.White.Lv, ufTargetChrom.White.x, ufTargetChrom.White.y);

                // ●補正データを作成する [*6]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*7] Make New Correction Data."); }
                winProgress.ShowMessage("Make New Correction Data.");

                status = m_MakeUFData.Statistics(-1, out targetYw, out targetYr, out targetYg, out targetYb, out ucr, out ucg, out ucb);
                if (status != true)
                {
                    ShowMessageWindow("Failed in Statistics.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }

                // ●ファイル保存 [*7]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*8] Save New Module Data."); }
                winProgress.ShowMessage("Save New Module Data.");

                string adjustedFile = makeFilePath(unit, FileDirectory.Temp);

                // フォルダがない場合、フォルダ作成
                if (System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(adjustedFile)) != true)
                { System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(adjustedFile)); }

                // ファイルがある場合消去
                if (System.IO.File.Exists(adjustedFile) == true)
                {
                    try { System.IO.File.Delete(adjustedFile); }
                    catch { } // とりあえず無視
                }

                try
                {
                    status = Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2
                        ? m_MakeUFData.OverWritePixelData(adjustedFile, allocInfo.LEDModel, true)
                        : m_MakeUFData.OverWritePixelDataWithCrosstalk(adjustedFile, allocInfo.LEDModel, true);

                    if (status != true)
                    {
                        ShowMessageWindow("Failed in OverWritePixelData.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                        return false;
                    }
                }
                catch
                {
                    ShowMessageWindow("Failed in OverWritePixelData.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }

                // ●移動するファイルのリストへ格納 [*8]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*9] Add New Data to List."); }
                winProgress.ShowMessage("Add New Data to List.");

                MoveFile move = new MoveFile();
                move.ControllerID = unit.ControllerID;
                move.FilePath = adjustedFile;

                lstMoveFile.Add(move);
            }
            winProgress.PutForward1Step();

            //推定処理時間
            int processSec = 0;

            int currentStep = 0;
            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));

            int maxConnectCabinet = 0;
            foreach (ControllerInfo cont in lstTargetController)
            {
            int cabinetCount = 0;
                foreach (UnitInfo unit in lstTargetUnit)
                {
                    if (cont.ControllerID == unit.ControllerID)
                    { cabinetCount++; }
                }

                if (maxConnectCabinet < cabinetCount)
                { maxConnectCabinet = cabinetCount; }
            }

            int dataMoveSec = calcAdjustUfDataMoveSec(lstTargetUnit.Count);
            int responseSec = calcAdjustUfResponseSec(maxConnectCabinet);
            processSec = calcAdjustUfProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.StartRemainTimer(processSec);

            // Measure Onlyの場合は書き込み周りの処理を行わない
            if (mode != UfAdjustMode.MeasureOnly)
            {
                // TestPattern OFF
                foreach (ControllerInfo cont in dicController.Values)
                { sendSdcpCommand(SDCPClass.CmdIntSignalOff, 0, cont.IPAddress); }

                // ●調整済みファイルの移動 [11]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[11] Move Adjusted Files."); }
                winProgress.ShowMessage("Move Adjusted Files.");

                foreach (MoveFile move in lstMoveFile)
                {
                    if (Settings.Ins.ExecLog == true)
                    { SaveExecLog("\tMoved File = " + move.FilePath); }

                    // ファイル移動
                    try { status = putFileFtpRetry(dicController[move.ControllerID].IPAddress, move.FilePath); }
                    catch { status = false; }

                    if (status == false) // ファイルの移動が失敗するケースへの対応
                    {
                        System.Threading.Thread.Sleep(5000);

                        try { status = putFileFtpRetry(dicController[move.ControllerID].IPAddress, move.FilePath); }
                        catch { status = false; }
                    }

                    if (status == false) // リトライ
                    {
                        if (Settings.Ins.ExecLog == true)
                        { SaveExecLog("\t(Retry to move file.)"); }

                        System.Threading.Thread.Sleep(5000);

                        try
                        {
                            if (putFileFtpRetry(dicController[move.ControllerID].IPAddress, move.FilePath) != true)
                            {
                                ShowMessageWindow("Failed in moving adjusted module data file.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                                return false;
                            }
                        }
                        catch
                        {
                            ShowMessageWindow("Failed in moving adjusted module data file.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                            return false;
                        }
                    }

                    // Controllerごとのファイル数カウント
                    dicFileCount[move.ControllerID]++;
                }
                winProgress.PutForward1Step();

                dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
                processSec = calcAdjustUfProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
                winProgress.SetRemainTimer(processSec);

                // ●Cabinet Power Off [12]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[12] Cabinet Power Off."); }
                winProgress.ShowMessage("Cabinet Power Off.");

                foreach (ControllerInfo cont in lstTargetController)
                { sendSdcpCommand(SDCPClass.CmdUnitPowerOff, cont.IPAddress); }
                System.Threading.Thread.Sleep(SLEEP_TIME_AFTER_PANEL_OFF);

                if (getUnitPowerStatus() != true)
                {
                    string msg = "Failed to cabinet power off.";
                    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 500, 210);
                    return false;
                }
                winProgress.PutForward1Step();

                dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
                processSec = calcAdjustUfProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
                winProgress.SetRemainTimer(processSec);

#if NO_WRITE
#else
                // ●Reconfig [13]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[13] Send Reconfig."); }
                winProgress.ShowMessage("Send Reconfig.");

                sendReconfig();
                winProgress.PutForward1Step();

                dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
                processSec = calcAdjustUfProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
                winProgress.SetRemainTimer(processSec);

                // ●書き込みコマンド発行 [14]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[14] Send Write Command."); }
                winProgress.ShowMessage("Send Write Command.");

                foreach (ControllerInfo cont in lstTargetController)
                { sendSdcpCommand(SDCPClass.CmdDataWrite, cont.IPAddress); }
                winProgress.PutForward1Step();

                dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
                processSec = calcAdjustUfProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
                winProgress.SetRemainTimer(processSec);

                // ●書き込みComplete待ち [15]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[15] Waiting for the process of controller."); }
                winProgress.ShowMessage("Waiting for the process of controller.");

                //int checkedController = 0;
                //int maxFileCount = 0;
                //foreach (KeyValuePair<int, int> pair in dicFileCount)
                //{
                //    if (pair.Value > maxFileCount)
                //    {
                //        checkedController = pair.Key;
                //        maxFileCount = pair.Value;
                //    }
                //}

                //while (true)
                //{
                //    // 書き込みが一番長いと思われるControllerのみ確認
                //    if (checkCompleteFtp(dicController[checkedController].IPAddress, "write_complete") == true)
                //    { break; }

                //    System.Threading.Thread.Sleep(1000);
                //}

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

                dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
                processSec = calcAdjustUfProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
                winProgress.SetRemainTimer(processSec);

#endif
            }

            // ●調整設定解除 [16]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[16] Set Normal Setting."); }
            winProgress.ShowMessage("Set Normal Setting.");

            setNormalSetting();
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            // ●User設定に戻す [17]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[17] Restore User Settings."); }
            winProgress.ShowMessage("Restore User Settings.");
            
            //if (result == MessageBoxResult.Yes)
            setUserSetting(m_lstUserSetting); 
            m_lstUserSetting = null;    //User設定を戻したので呼び出し側の設定処理をスキップ

            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            if (mode != UfAdjustMode.MeasureOnly)
            {
#if NO_WRITE
#else
                // ●Latest → Previousフォルダへコピー
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[18] Move Latest to Previous."); }

                foreach (ControllerInfo controller in lstTargetController)
                { copyLatest2Previous(applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo); }

                winProgress.PutForward1Step();

                dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
                processSec = calcAdjustUfProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
                winProgress.SetRemainTimer(processSec);

                // ●Temp → Latestフォルダへコピー
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[19] Move Temp to Latest."); }

                foreach (ControllerInfo controller in lstTargetController)
                { copyTemp2Latest(applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo); }

                winProgress.PutForward1Step();

                dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
                processSec = calcAdjustUfProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
                winProgress.SetRemainTimer(processSec);

                // ●Reconfig [20]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[20] Send Reconfig."); }
                winProgress.ShowMessage("Send Reconfig.");

                sendReconfig();
                winProgress.PutForward1Step();

                dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
                processSec = calcAdjustUfProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
                winProgress.SetRemainTimer(processSec);
#endif

                // ●Tempフォルダのファイルを削除
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[21] Delete Temp Files."); }

                foreach (ControllerInfo controller in lstTargetController)
                {
                    string tempPath = applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo + "_Temp";

                    if (Directory.Exists(tempPath) != true)
                    { Directory.CreateDirectory(tempPath); }

                    string[] files = Directory.GetFiles(tempPath, "*", System.IO.SearchOption.AllDirectories);

                    for (int i = 0; i < files.Length; i++)
                    {
                        try
                        {
                            if (Settings.Ins.ExecLog == true)
                            {
                                SaveExecLog("Delete : " + files[i]);
                            }
                            File.Delete(files[i]);
                        }
                        catch
                        {
                            return false;
                        }
                    }
                }

                // ●調整完了したUnitの背景色を変更 [19]
                //winProgress.ShowMessage("Set Complete Back Color.");
                //changeCompleteColor(lstTargetUnit);
                //winProgress.PutForward1Step();

                winProgress.PutForward1Step();

                dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
                processSec = calcAdjustUfProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
                winProgress.SetRemainTimer(processSec);
            }

            // ●選択を解除 [22]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[22] Unselect All Cabinets."); }
            winProgress.ShowMessage("Unselect All Cabinets.");

            btnDeselectAll_Click(null, null);
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfProcessSec(currentStep, lstTargetController.Count, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            // ●FTP Off [23]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[23] FTP Off."); }
            winProgress.ShowMessage("FTP Off.");

            foreach (ControllerInfo controller in lstTargetController)
            {
                if (sendSdcpCommand(SDCPClass.CmdFtpOff, controller.IPAddress) != true)
                {
                    ShowMessageWindow("Failed to FTP Off.", "Error!", System.Drawing.SystemIcons.Error, 420, 180);
                    return false;
                }
            }
            winProgress.PutForward1Step();

            return true;
        }

        // Unit個別調整用アルゴリズム
        private bool adjustUfUnit()
        {
            if (Settings.Ins.ExecLog == true)
            {
                SaveExecLog("");
                SaveExecLog("[0] adjustUfCabinet start");
            }

            ControllerInfo controller;
            List<UnitInfo> lstTargetUnit = new List<UnitInfo>();
            UnitInfo targetUnit;
            double[][][] measureData;
            bool status;
            List<UserSetting> lstUserSetting;
            FileDirectory baseFileDir;

            // ●進捗の最大値を設定 [1]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[1] Set Step Count."); }
            winProgress.ShowMessage("Set Step Count.");

            int step = AJUSTMENT_CABINET_STEPS;
            winProgress.SetWholeSteps(step);

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

            var dispatcher = Application.Current.Dispatcher;
            dispatcher.Invoke(() => correctAdjustUnit(out lstTargetUnit, allocInfo.MaxX, allocInfo.MaxY));
            if (lstTargetUnit.Count == 0)
            {
                ShowMessageWindow("Please select cabinets.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            targetUnit = lstTargetUnit[0];
            winProgress.PutForward1Step();

            // ●調整をするControllerをListに格納 [4]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[4] Store Target Controller(s) Info."); }
            winProgress.ShowMessage("Store Target Controller(s) Info.");

            foreach (ControllerInfo cont in dicController.Values)
            { cont.Target = false; }

            dicController[targetUnit.ControllerID].Target = true;

            controller = dicController[targetUnit.ControllerID];    // 対象Controller

            // 隣接のCabinetが接続されているControllerが異なる場合、対象Controllerに設定
            setAdjacentUnitControllerToTarget(targetUnit);
            winProgress.PutForward1Step();

            // ●調整データのバックアップがすべてあるか確認 [5]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[5] Check Backup Data."); }
            winProgress.ShowMessage("Check Backup Data.");

            status = checkDataFile(lstTargetUnit, out baseFileDir);
            if (status != true)
            {
                ShowMessageWindow("There is not the module data file.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●FTP ON [*1]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*1] FTP On."); }
            winProgress.ShowMessage("FTP On.");

            if (sendSdcpCommand(SDCPClass.CmdFtpOn, controller.IPAddress) != true)
            { return false; }
            winProgress.PutForward1Step();

            // ●Model名取得 [*2]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*2] Get Model Name."); }
            winProgress.ShowMessage("Get Model Name.");

            if (string.IsNullOrWhiteSpace(controller.ModelName) == true)
            { controller.ModelName = getModelName(controller.IPAddress); }

            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("     Model Name : " + controller.ModelName); }
            winProgress.PutForward1Step();

            // ●Serial取得 [*3]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*3] Get Serial No."); }
            winProgress.ShowMessage("Get Serial No.");

            if (string.IsNullOrWhiteSpace(controller.SerialNo) == true)
            { controller.SerialNo = getSerialNo(controller.IPAddress); }

            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("     Serial Num. : " + controller.SerialNo); }
            winProgress.PutForward1Step();

            // Tempフォルダ内のファイルを削除 [*4]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*4] Delete Temporary Files."); }
            winProgress.ShowMessage("Delete Temporary Files.");

            string tempPath = applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo + "_Temp";

            if (Directory.Exists(tempPath) != true)
            { Directory.CreateDirectory(tempPath); }

            string[] files = Directory.GetFiles(tempPath, "*", System.IO.SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    if (Settings.Ins.ExecLog == true)
                    {
                        SaveExecLog("Delete : " + files[i]);
                    }
                    File.Delete(files[i]);
                }
                catch { return false; }
            }
            winProgress.PutForward1Step();

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

            if (setCA410SettingDispatch() != true)
            {
                ShowMessageWindow("Can not set CA-410 settings.\r\nPlease check it status.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●User設定保存 [8]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[8] Store User Settings."); }
            winProgress.ShowMessage("Store User Settings.");

            try
            {
                if (getUserSetting(out lstUserSetting) != true)
                { return false; }
                m_lstUserSetting = lstUserSetting;
            }
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }
            winProgress.PutForward1Step();

            // ●調整用設定 [9]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[9] Set Adjust Settings."); }
            winProgress.ShowMessage("Set Adjust Settings.");

            setAdjustSetting();
            winProgress.PutForward1Step();

            // ●Layout情報Off [10]
            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdDispUnitAddrOff, 0, cont.IPAddress); }

            // ●色度測定 [*1]
            UserOperation operation = UserOperation.None;

            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*1] Measure Cabinet Color."); }
            winProgress.ShowMessage("Measure Cabinet Color.");

            while (true)
            {
                status = measure8Points(targetUnit, out measureData, out operation);
                
                if (status == true)
                { break; }
                else if(operation == UserOperation.Cancel)
                { return false; }
                else if (operation == UserOperation.Repeat)
                { continue; }
                else
                {
                    bool? result;
                    string msg = "Color measurement failed.\r\nDo you want to continue the measurement?";

                    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                    if (result != true)
                    { return false; }
                }
            }
            winProgress.PutForward1Step();

            //推定処理時間
            int processSec = 0;
            int currentStep = 0;

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfUnitProcessSec(currentStep);
            winProgress.StartRemainTimer(processSec);

            string filePath = makeFilePath(targetUnit, baseFileDir);
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
            {
                // ●クロストーク補正量算出 [*]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*2] Calc Crosstalk Correction."); }
                winProgress.ShowMessage("Calc Crosstalk Correction.");

                status = m_MakeUFData.GetUncCrosstalk(filePath, measureData);
                if (!status)
                {
                    ShowMessageWindow("Failed in crosstalk correction calc.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }
            }

            // ●目標値設定 [*3]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*3] Set Target Color."); }
            winProgress.ShowMessage("Set Target Color.");

            //m_MakeUFData.SetTargetValue(m_Target_xr, m_Target_yr, m_Target_xg, m_Target_yg, m_Target_xb, m_Target_yb, m_Target_Yw, m_Target_xw, m_Target_yw);
            m_MakeUFData.SetTargetValue(ufTargetChrom.Red.x, ufTargetChrom.Red.y, ufTargetChrom.Green.x, ufTargetChrom.Green.y, ufTargetChrom.Blue.x, ufTargetChrom.Blue.y, ufTargetChrom.White.Lv, ufTargetChrom.White.x, ufTargetChrom.White.y);
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfUnitProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // ●Cell Dataの補正データ抽出 [*4]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*4] Extract Correction Data."); }
            winProgress.ShowMessage("Extract Correction Data.");

            status = m_MakeUFData.ExtractFmt(filePath, allocInfo.LEDModel);
            if (status != true)
            {
                ShowMessageWindow("Failed in ExtractFmt.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfUnitProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // ●抽出した補正データと調整目標値から逆算してその時のXYZの値を求める [*5]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*5] Calc XYZ."); }
            winProgress.ShowMessage("Calc XYZ.");

            //status = m_MakeUFData.Fmt2XYZ(m_Target_xr, m_Target_yr, m_Target_xg, m_Target_yg, m_Target_xb, m_Target_yb, m_Target_Yw, m_Target_xw, m_Target_yw);
            status = m_MakeUFData.Fmt2XYZ(ufTargetChrom.Red.x, ufTargetChrom.Red.y, ufTargetChrom.Green.x, ufTargetChrom.Green.y, ufTargetChrom.Blue.x, ufTargetChrom.Blue.y, ufTargetChrom.White.Lv, ufTargetChrom.White.x, ufTargetChrom.White.y);
            if (status != true)
            {
                ShowMessageWindow("Failed in Fmt2XYZ.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfUnitProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // ●今回の計測値と前の計測値の差分を求めて、画素ごとのXYZデータを更新する [*6]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*6] Calc New XYZ Data."); }
            winProgress.ShowMessage("Calc New XYZ Data.");

            status = m_MakeUFData.Compensate_XYZ(measureData, CMakeUFData.MeasureMode.Unit);
            if (status != true)
            {
                ShowMessageWindow("Failed in Compensate_XYZ.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfUnitProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // ●補正データを作成する [*7]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*7] Make New Correction Data."); }
            winProgress.ShowMessage("Make New Correction Data.");

            double targetYw, targetYr, targetYg, targetYb;
            int ucr, ucg, ucb;
            status = m_MakeUFData.Statistics(-1, out targetYw, out targetYr, out targetYg, out targetYb, out ucr, out ucg, out ucb);
            if (status != true)
            {
                ShowMessageWindow("Failed in Statistics.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfUnitProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // ●ファイル保存 [*8]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*8] Save New Module Data."); }
            winProgress.ShowMessage("Save New Module Data.");

            string adjustedFile = makeFilePath(targetUnit, FileDirectory.Temp);

            // フォルダがない場合、フォルダ作成
            if (System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(adjustedFile)) != true)
            { System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(adjustedFile)); }

            try
            {
                status = Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2
                    ? m_MakeUFData.OverWritePixelData(adjustedFile, allocInfo.LEDModel)
                    : m_MakeUFData.OverWritePixelDataWithCrosstalk(adjustedFile, allocInfo.LEDModel);

                if (status != true)
                {
                    ShowMessageWindow("Failed in OverWritePixelData.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }
            }
            catch
            {
                ShowMessageWindow("Failed in OverWritePixelData.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfUnitProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // TestPattern OFF [11]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[11] Test Pattern Off."); }
            winProgress.ShowMessage("Test Pattern Off.");

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdIntSignalOff, 0, cont.IPAddress); }

            // ●調整済みファイルの移動 [12]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[12] Move Adjusted Files."); }
            winProgress.ShowMessage("Move Adjusted Files.");

            try
            {
                if (putFileFtpRetry(controller.IPAddress, adjustedFile) != true)
                {
                    ShowMessageWindow("Failed in moving adjusted module data file.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }
            }
            catch
            {
                ShowMessageWindow("Failed in moving adjusted module data file.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfUnitProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // ●Cabinet Power Off [13]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[13] Cabinet Power Off."); }
            winProgress.ShowMessage("Cabinet Power Off.");

            sendSdcpCommand(SDCPClass.CmdUnitPowerOff, controller.IPAddress);
            System.Threading.Thread.Sleep(SLEEP_TIME_AFTER_PANEL_OFF);

            if (getUnitPowerStatus() != true)
            {
                string msg = "Failed to cabinet power off.";
                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfUnitProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

#if NO_WRITE
#else
            // ●Reconfig [14]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[14] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            sendReconfig();
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfUnitProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // ●書き込みコマンド発行 [15]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[15] Send Write Command."); }
            winProgress.ShowMessage("Send Write Command.");

            sendSdcpCommand(SDCPClass.CmdDataWrite, controller.IPAddress);
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfUnitProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // ●書き込みComplete待ち [16]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[16] Waiting for the process of controller."); }
            winProgress.ShowMessage("Waiting for the process of controller.");

            try
            {
                while (true)
                {
                    if (checkCompleteFtp(controller.IPAddress, "write_complete") == true)
                    { break; }

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

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfUnitProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

#endif

            // ●調整設定解除 [17]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[17] Set Normal Setting."); }
            winProgress.ShowMessage("Set Normal Setting.");

            setNormalSetting();
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfUnitProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // ●User設定に戻す [18]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[18] Restore User Settings."); }
            winProgress.ShowMessage("Restore User Settings.");

            setUserSetting(m_lstUserSetting);
            m_lstUserSetting = null;    //User設定を戻したので呼び出し側の設定処理をスキップ

            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfUnitProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

#if NO_WRITE
#else       
            // ●Latest → Previousフォルダへコピー [19]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[19] Copy Latest to Previous."); }
            winProgress.ShowMessage("Copy Latest to Previous.");

            copyLatest2Previous(applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo);
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfUnitProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // ●Temp → Latestフォルダへコピー [20]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[20] Copy Temp to Latest."); }
            winProgress.ShowMessage("Copy Temp to Latest.");

            copyTemp2Latest(applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo);
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfUnitProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // ●Reconfig [21]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[21] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            sendReconfig();
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfUnitProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

#endif

            // ●Tempフォルダのファイルを削除 [22]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[22] Delete Temporary Files."); }
            winProgress.ShowMessage("Delete Temporary Files.");

            files = Directory.GetFiles(tempPath, "*", System.IO.SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    if (Settings.Ins.ExecLog == true)
                    {
                        SaveExecLog("Delete : " + files[i]);
                    }
                    File.Delete(files[i]);
                }
                catch { return false; }
            }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfUnitProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // ●FTP Off [23]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[23] FTP Off."); }
            winProgress.ShowMessage("FTP Off.");

            if (sendSdcpCommand(SDCPClass.CmdFtpOff, controller.IPAddress) != true)
            {
                ShowMessageWindow("Failed to FTP Off.", "Error!", System.Drawing.SystemIcons.Error, 420, 180);
                return false;
            }
            winProgress.PutForward1Step();

            return true;
        }

        private void correctAdjustUnit(out List<UnitInfo> lstUnitList, int MaxX, int MaxY)
        {
            int size;
            AdjustDirection dir;
            string error;
            List<UnitInfo> lstUnitTemp = new List<UnitInfo>();

            lstUnitList = new List<UnitInfo>();

            size = cmbxBlockSize.SelectedIndex + 1;

            if (rbHDir.IsChecked == true)
            { dir = AdjustDirection.Horizontally; }
            else
            { dir = AdjustDirection.Vertically; }

            // Modeを保存
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\tSize : " + size + ", Direction : " + dir.ToString()); }

            #region Size_1
            if (size == 1 && dir == AdjustDirection.Horizontally)
            {
                for (int y = MaxY - 1; y >= 0; y -= 2)
                {
                    for (int x = 0; x < MaxX; x++)
                    {
                        try
                        {
                            if (aryUnitUf[x, y].UnitInfo != null && aryUnitUf[x, y].IsChecked == true)
                            { lstUnitList.Add(aryUnitUf[x, y].UnitInfo); }
                        }
                        catch
                        {
                            error = "[correctAdjustCabinet] (Case:1) x = " + x.ToString() + ", y = " + y.ToString()
                                + ", MaxX = " + MaxX.ToString() + ", MaxY = " + MaxY.ToString();
                            SaveErrorLog(error);
                            saveSelectionInfo();
                        }
                    }

                    if (y - 1 < 0)
                    { continue; }

                    for (int x = MaxX - 1; x >= 0; x--)
                    {
                        try
                        {
                            if (aryUnitUf[x, y - 1].UnitInfo != null && aryUnitUf[x, y - 1].IsChecked == true)
                            { lstUnitList.Add(aryUnitUf[x, y - 1].UnitInfo); }
                        }
                        catch
                        {
                            error = "[correctAdjustCabinet] (Case:2) x = " + x.ToString() + ", y = " + y.ToString()
                                + ", MaxX = " + MaxX.ToString() + ", MaxY = " + MaxY.ToString();
                            SaveErrorLog(error);
                            saveSelectionInfo();
                        }
                    }
                }
            }
            else if (size == 1 && dir == AdjustDirection.Vertically)
            {
                for (int x = 0; x <= MaxX - 1; x += 2)
                {
                    for (int y = MaxY - 1; y >= 0; y--)
                    {
                        try
                        {
                            if (aryUnitUf[x, y].UnitInfo != null && aryUnitUf[x, y].IsChecked == true)
                            { lstUnitList.Add(aryUnitUf[x, y].UnitInfo); }
                        }
                        catch
                        {
                            error = "[correctAdjustCabinet] (Case:3) x = " + x.ToString() + ", y = " + y.ToString()
                                + ", MaxX = " + MaxX.ToString() + ", MaxY = " + MaxY.ToString();
                            SaveErrorLog(error);
                            saveSelectionInfo();
                        }
                    }

                    if (x + 1 >= MaxX)
                    { continue; }

                    for (int y = 0; y < MaxY; y++)
                    {
                        try
                        {
                            if (aryUnitUf[x + 1, y].UnitInfo != null && aryUnitUf[x + 1, y].IsChecked == true)
                            { lstUnitList.Add(aryUnitUf[x + 1, y].UnitInfo); }
                        }
                        catch
                        {
                            error = "[correctAdjustCabinet] (Case:4) x = " + x.ToString() + ", y = " + y.ToString()
                                + ", MaxX = " + MaxX.ToString() + ", MaxY = " + MaxY.ToString();
                            SaveErrorLog(error);
                            saveSelectionInfo();
                        }
                    }
                }
            }
            #endregion
            #region Size_2
            else if (size == 2 && dir == AdjustDirection.Horizontally)
            {
                for (int y = MaxY - 2; y >= -1; y -= 4)
                {
                    int x;

                    // Right方向
                    for (x = 0; x < MaxX; x += 2)
                    {
                        for (int yd = 0; yd < 2; yd++)
                        {
                            for (int xd = 0; xd < 2; xd++)
                            {
                                if(y + yd < 0 || x + xd >= MaxX)
                                { continue; }

                                try
                                {
                                    if (aryUnitUf[x + xd, y + yd].UnitInfo != null && aryUnitUf[x + xd, y + yd].IsChecked == true)
                                    { lstUnitList.Add(aryUnitUf[x + xd, y + yd].UnitInfo); }
                                }
                                catch
                                {
                                    error = "[correctAdjustCabinet] (Case:5) x = " + x.ToString() + ", xd = " + xd.ToString() + ", y = " + y.ToString() + ", yd = " + yd.ToString()
                                        + ", MaxX = " + MaxX.ToString() + ", MaxY = " + MaxY.ToString();
                                    SaveErrorLog(error);
                                    saveSelectionInfo();
                                }
                            }
                        }
                    }

                    // Left方向
                    for (x -= 2; x > -1; x -= 2)
                    {
                        for (int yd = 0; yd < 2; yd++)
                        {
                            for (int xd = 1; xd >= 0; xd--)
                            {
                                if (y - 2 + yd < 0 || x + xd < 0 || x + xd >= MaxX)
                                { continue; }

                                try
                                {
                                    if (aryUnitUf[x + xd, y - 2 + yd].UnitInfo != null && aryUnitUf[x + xd, y - 2 + yd].IsChecked == true)
                                    { lstUnitList.Add(aryUnitUf[x + xd, y - 2 + yd].UnitInfo); }
                                }
                                catch
                                {
                                    error = "[correctAdjustCabinet] (Case:6) x = " + x.ToString() + ", xd = " + xd.ToString() + ", y = " + y.ToString() + ", yd = " + yd.ToString()
                                        + ", MaxX = " + MaxX.ToString() + ", MaxY = " + MaxY.ToString();
                                    SaveErrorLog(error);
                                    saveSelectionInfo();
                                }
                            }
                        }
                    }
                }
            }
            else if (size == 2 && dir == AdjustDirection.Vertically)
            {
                for (int x = 0; x < MaxX; x += 4)
                {
                    int y;

                    // Up方向
                    for (y = MaxY - 2; y >= -1; y -= 2)
                    {
                        lstUnitTemp.Clear();

                        for (int yd = 0; yd < 2; yd++)
                        {
                            for (int xd = 0; xd < 2; xd++)
                            {
                                if (y + yd < 0 || x + xd >= MaxX)
                                { continue; }

                                try
                                {
                                    if (aryUnitUf[x + xd, y + yd].UnitInfo != null && aryUnitUf[x + xd, y + yd].IsChecked == true)
                                    //{ lstUnitList.Add(aryUnitUf[x + xd, y + yd].UnitInfo); }
                                    { lstUnitTemp.Add(aryUnitUf[x + xd, y + yd].UnitInfo); }
                                }
                                catch
                                {
                                    error = "[correctAdjustCabinet] (Case:7) x = " + x.ToString() + ", xd = " + xd.ToString() + ", y = " + y.ToString() + ", yd = " + yd.ToString()
                                        + ", MaxX = " + MaxX.ToString() + ", MaxY = " + MaxY.ToString();
                                    SaveErrorLog(error);
                                    saveSelectionInfo();
                                }
                            }
                        }

                        restoreUnitInfo(ref lstUnitList, lstUnitTemp);
                    }


                    // Down方向
                    for (y += 2; y < MaxY; y += 2)
                    {
                        for (int yd = 0; yd < 2; yd++)
                        {
                            for (int xd = 0; xd < 2; xd++)
                            {
                                if (y + yd < 0 || x + 2 + xd >= MaxX)
                                { continue; }

                                try
                                {
                                    if (aryUnitUf[x + 2 + xd, y + yd].UnitInfo != null && aryUnitUf[x + 2 + xd, y + yd].IsChecked == true)
                                    { lstUnitList.Add(aryUnitUf[x + 2 + xd, y + yd].UnitInfo); }
                                }
                                catch
                                {
                                    error = "[correctAdjustCabinet] (Case:8) x = " + x.ToString() + ", xd = " + xd.ToString() + ", y = " + y.ToString() + ", yd = " + yd.ToString()
                                        + ", MaxX = " + MaxX.ToString() + ", MaxY = " + MaxY.ToString();
                                    SaveErrorLog(error);
                                    saveSelectionInfo();
                                }
                            }
                        }
                    }
                }
            }
            #endregion
            #region Size_3
            else if (size == 3 && dir == AdjustDirection.Horizontally)
            {
                for (int y = MaxY - 3; y >= -2; y -= 6)
                {
                    int x;

                    // Right方向
                    for (x = 0; x < MaxX; x += 3)
                    {
                        for (int yd = 0; yd < 3; yd++)
                        {
                            for (int xd = 0; xd < 3; xd++)
                            {
                                if (y + yd < 0 || x + xd >= MaxX)
                                { continue; }

                                try
                                {
                                    if (aryUnitUf[x + xd, y + yd].UnitInfo != null && aryUnitUf[x + xd, y + yd].IsChecked == true)
                                    { lstUnitList.Add(aryUnitUf[x + xd, y + yd].UnitInfo); }
                                }
                                catch
                                {
                                    error = "[correctAdjustCabinet] (Case:9) x = " + x.ToString() + ", xd = " + xd.ToString() + ", y = " + y.ToString() + ", yd = " + yd.ToString()
                                        + ", MaxX = " + MaxX.ToString() + ", MaxY = " + MaxY.ToString();
                                    SaveErrorLog(error);
                                    saveSelectionInfo();
                                }
                            }
                        }
                    }

                    // Left方向
                    for (x -= 3; x > -2; x -= 3)
                    {
                        for (int yd = 0; yd < 3; yd++)
                        {
                            for (int xd = 2; xd >= 0; xd--)
                            {
                                if (y - 3 + yd < 0 || x + xd < 0 || x + xd >= MaxX)
                                { continue; }

                                try
                                {
                                    if (aryUnitUf[x + xd, y - 3 + yd].UnitInfo != null && aryUnitUf[x + xd, y - 3 + yd].IsChecked == true)
                                    { lstUnitList.Add(aryUnitUf[x + xd, y - 3 + yd].UnitInfo); }
                                }
                                catch
                                {
                                    error = "[correctAdjustCabinet] (Case:10) x = " + x.ToString() + ", xd = " + xd.ToString() + ", y = " + y.ToString() + ", yd = " + yd.ToString()
                                        + ", MaxX = " + MaxX.ToString() + ", MaxY = " + MaxY.ToString();
                                    SaveErrorLog(error);
                                    saveSelectionInfo();
                                }
                            }
                        }
                    }
                }
            }
            else if (size == 3 && dir == AdjustDirection.Vertically)
            {
                for (int x = 0; x < MaxX; x += 6)
                {
                    int y;

                    // Up方向
                    for (y = MaxY - 3; y >= -2; y -= 3)
                    {
                        lstUnitTemp.Clear();

                        for (int yd = 0; yd < 3; yd++)
                        {
                            for (int xd = 0; xd < 3; xd++)
                            {
                                if (y + yd < 0 || x + xd >= MaxX)
                                { continue; }

                                try
                                {
                                    if (aryUnitUf[x + xd, y + yd].UnitInfo != null && aryUnitUf[x + xd, y + yd].IsChecked == true)
                                    //{ lstUnitList.Add(aryUnitUf[x + xd, y + yd].UnitInfo); }
                                    { lstUnitTemp.Add(aryUnitUf[x + xd, y + yd].UnitInfo); }
                                }
                                catch
                                {
                                    error = "[correctAdjustCabinet] (Case:11) x = " + x.ToString() + ", xd = " + xd.ToString() + ", y = " + y.ToString() + ", yd = " + yd.ToString()
                                        + ", MaxX = " + MaxX.ToString() + ", MaxY = " + MaxY.ToString();
                                    SaveErrorLog(error);
                                    saveSelectionInfo();
                                }
                            }
                        }

                        restoreUnitInfo(ref lstUnitList, lstUnitTemp);
                    }

                    // Down方向
                    for (y += 3; y < MaxY; y += 3)
                    {
                        for (int yd = 0; yd < 3; yd++)
                        {
                            for (int xd = 0; xd < 3; xd++)
                            {
                                if (y + yd < 0 || x + 3 + xd >= MaxX)
                                { continue; }

                                try
                                {
                                    if (aryUnitUf[x + 3 + xd, y + yd].UnitInfo != null && aryUnitUf[x + 3 + xd, y + yd].IsChecked == true)
                                    { lstUnitList.Add(aryUnitUf[x + 3 + xd, y + yd].UnitInfo); }
                                }
                                catch
                                {
                                    error = "[correctAdjustCabinet] (Case:12) x = " + x.ToString() + ", xd = " + xd.ToString() + ", y = " + y.ToString() + ", yd = " + yd.ToString()
                                        + ", MaxX = " + MaxX.ToString() + ", MaxY = " + MaxY.ToString();
                                    SaveErrorLog(error);
                                    saveSelectionInfo();
                                }
                            }
                        }
                    }
                }
            }
            #endregion
            #region Size_4
            else if (size == 4 && dir == AdjustDirection.Horizontally)
            {
                for (int y = MaxY - 3; y >= -2; y -= 6)
                {
                    int x;
                    
                    // Right方向
                    for (x = 0; x < MaxX; x += 4)
                    {
                        for (int yd = 0; yd < 3; yd++)
                        {
                            for (int xd = 0; xd < 4; xd++)
                            {
                                if (y + yd < 0 || x + xd >= MaxX)
                                { continue; }

                                try
                                {
                                    if (aryUnitUf[x + xd, y + yd].UnitInfo != null && aryUnitUf[x + xd, y + yd].IsChecked == true)
                                    { lstUnitList.Add(aryUnitUf[x + xd, y + yd].UnitInfo); }
                                }
                                catch
                                {
                                    error = "[correctAdjustCabinet] (Case:13) x = " + x.ToString() + ", xd = " + xd.ToString() + ", y = " + y.ToString() + ", yd = " + yd.ToString()
                                        + ", MaxX = " + MaxX.ToString() + ", MaxY = " + MaxY.ToString();
                                    SaveErrorLog(error);
                                    saveSelectionInfo();
                                }
                            }
                        }
                    }

                    // Left方向
                    for (x -= 4; x > -3; x -= 4)
                    {
                        for (int yd = 0; yd < 3; yd++)
                        {
                            for (int xd = 3; xd >= 0; xd--)
                            {
                                if (y - 3 + yd < 0 || x + xd < 0 || x + xd >= MaxX)
                                { continue; }

                                try
                                {
                                    if (aryUnitUf[x + xd, y - 3 + yd].UnitInfo != null && aryUnitUf[x + xd, y - 3 + yd].IsChecked == true)
                                    { lstUnitList.Add(aryUnitUf[x + xd, y - 3 + yd].UnitInfo); }
                                }
                                catch
                                {
                                    error = "[correctAdjustCabinet] (Case:14) x = " + x.ToString() + ", xd = " + xd.ToString() + ", y = " + y.ToString() + ", yd = " + yd.ToString()
                                        + ", MaxX = " + MaxX.ToString() + ", MaxY = " + MaxY.ToString();
                                    SaveErrorLog(error);
                                    saveSelectionInfo();
                                }
                            }
                        }
                    }
                }
            }
            else if (size == 4 && dir == AdjustDirection.Vertically)
            {
                for (int x = 0; x < MaxX; x += 8)
                {
                    int y;

                    // Up方向
                    for (y = MaxY - 3; y >= -2; y -= 3)
                    {
                        lstUnitTemp.Clear();

                        for (int yd = 0; yd < 3; yd++)
                        {
                            for (int xd = 0; xd < 4; xd++)
                            {
                                if (y + yd < 0 || x + xd >= MaxX)
                                { continue; }

                                try
                                {
                                    if (aryUnitUf[x + xd, y + yd].UnitInfo != null && aryUnitUf[x + xd, y + yd].IsChecked == true)
                                    //{ lstUnitList.Add(aryUnitUf[x + xd, y + yd].UnitInfo); }
                                    { lstUnitTemp.Add(aryUnitUf[x + xd, y + yd].UnitInfo); }
                                }
                                catch
                                {
                                    error = "[correctAdjustCabinet] (Case:15) x = " + x.ToString() + ", xd = " + xd.ToString() + ", y = " + y.ToString() + ", yd = " + yd.ToString()
                                        + ", MaxX = " + MaxX.ToString() + ", MaxY = " + MaxY.ToString();
                                    SaveErrorLog(error);
                                    saveSelectionInfo();
                                }
                            }
                        }

                        restoreUnitInfo(ref lstUnitList, lstUnitTemp);
                    }

                    // Down方向
                    for (y += 3; y < MaxY; y += 3)
                    {
                        for (int yd = 0; yd < 3; yd++)
                        {
                            for (int xd = 0; xd < 4; xd++)
                            {
                                if (y + yd < 0 || x + 4 + xd >= MaxX)
                                { continue; }

                                try
                                {
                                    if (aryUnitUf[x + 4 + xd, y + yd].UnitInfo != null && aryUnitUf[x + 4 + xd, y + yd].IsChecked == true)
                                    { lstUnitList.Add(aryUnitUf[x + 4 + xd, y + yd].UnitInfo); }
                                }
                                catch
                                {
                                    error = "[correctAdjustCabinet] (Case:16) x = " + x.ToString() + ", xd = " + xd.ToString() + ", y = " + y.ToString() + ", yd = " + yd.ToString()
                                        + ", MaxX = " + MaxX.ToString() + ", MaxY = " + MaxY.ToString();
                                    SaveErrorLog(error);
                                    saveSelectionInfo();
                                }
                            }
                        }
                    }
                }
            }
            #endregion
        }

        private void restoreUnitInfo(ref List<UnitInfo> lstUnitList, List<UnitInfo> lstUnitTemp)
        {
            for (int Y = 100; Y > 0; Y--)
            {
                for (int X = 0; X < 100; X++)
                {
                    foreach (UnitInfo info in lstUnitTemp)
                    {
                        if (info.X == X && info.Y == Y)
                        {
                            lstUnitList.Add(info);
                            break;
                        }
                    }
                }
            }

            #region Old
            //if (lstUnitTemp.Count == 1)
            //{
            //    lstUnitList.Add(lstUnitTemp[0]);
            //}
            //else if (lstUnitTemp.Count == 2)
            //{
            //    // X=2, Y=1
            //    lstUnitList.Add(lstUnitTemp[0]);
            //    lstUnitList.Add(lstUnitTemp[1]);
            //}
            //else if (lstUnitTemp.Count == 3)
            //{
            //    // X=2, Y=1
            //    lstUnitList.Add(lstUnitTemp[0]);
            //    lstUnitList.Add(lstUnitTemp[1]);
            //    lstUnitList.Add(lstUnitTemp[2]);
            //}
            //else if (lstUnitTemp.Count == 4)
            //{
            //    if (size == 2)
            //    {
            //        lstUnitList.Add(lstUnitTemp[2]);
            //        lstUnitList.Add(lstUnitTemp[3]);
            //        lstUnitList.Add(lstUnitTemp[0]);
            //        lstUnitList.Add(lstUnitTemp[1]);
            //    }
            //    else
            //    {
            //        lstUnitList.Add(lstUnitTemp[0]);
            //        lstUnitList.Add(lstUnitTemp[1]);
            //        lstUnitList.Add(lstUnitTemp[2]);
            //        lstUnitList.Add(lstUnitTemp[3]);
            //    }
            //}
            //else if (lstUnitTemp.Count == 5) // Size = 3 / 4
            //{
            //    lstUnitList.Add(lstUnitTemp[2]);
            //    lstUnitList.Add(lstUnitTemp[3]);
            //    lstUnitList.Add(lstUnitTemp[4]);

            //    lstUnitList.Add(lstUnitTemp[0]);
            //    lstUnitList.Add(lstUnitTemp[1]);
            //}
            //else if (lstUnitTemp.Count == 6) // Size = 3 / 4
            //{
            //    // X=3, Y=2
            //    lstUnitList.Add(lstUnitTemp[3]);
            //    lstUnitList.Add(lstUnitTemp[4]);
            //    lstUnitList.Add(lstUnitTemp[5]);

            //    lstUnitList.Add(lstUnitTemp[0]);
            //    lstUnitList.Add(lstUnitTemp[1]);
            //    lstUnitList.Add(lstUnitTemp[2]);
            //}
            //else if (lstUnitTemp.Count == 7) // Size = 3 / 4
            //{
            //    lstUnitList.Add(lstUnitTemp[4]);
            //    lstUnitList.Add(lstUnitTemp[5]);
            //    lstUnitList.Add(lstUnitTemp[6]);

            //    lstUnitList.Add(lstUnitTemp[1]);
            //    lstUnitList.Add(lstUnitTemp[2]);
            //    lstUnitList.Add(lstUnitTemp[3]);

            //    lstUnitList.Add(lstUnitTemp[0]);
            //}
            //else if (lstUnitTemp.Count == 8) // Size = 3 / 4
            //{
            //    if (size == 4)
            //    {
            //        lstUnitList.Add(lstUnitTemp[4]);
            //        lstUnitList.Add(lstUnitTemp[5]);
            //        lstUnitList.Add(lstUnitTemp[6]);
            //        lstUnitList.Add(lstUnitTemp[7]);

            //        lstUnitList.Add(lstUnitTemp[0]);
            //        lstUnitList.Add(lstUnitTemp[1]);
            //        lstUnitList.Add(lstUnitTemp[2]);
            //        lstUnitList.Add(lstUnitTemp[3]);
            //    }
            //    else
            //    {
            //        lstUnitList.Add(lstUnitTemp[5]);
            //        lstUnitList.Add(lstUnitTemp[6]);
            //        lstUnitList.Add(lstUnitTemp[7]);

            //        lstUnitList.Add(lstUnitTemp[2]);
            //        lstUnitList.Add(lstUnitTemp[3]);
            //        lstUnitList.Add(lstUnitTemp[4]);

            //        lstUnitList.Add(lstUnitTemp[0]);
            //        lstUnitList.Add(lstUnitTemp[1]);
            //    }
            //}
            //else if (lstUnitTemp.Count == 9) // Size = 3 / 4
            //{
            //    lstUnitList.Add(lstUnitTemp[6]);
            //    lstUnitList.Add(lstUnitTemp[7]);
            //    lstUnitList.Add(lstUnitTemp[8]);

            //    lstUnitList.Add(lstUnitTemp[3]);
            //    lstUnitList.Add(lstUnitTemp[4]);
            //    lstUnitList.Add(lstUnitTemp[5]);

            //    lstUnitList.Add(lstUnitTemp[0]);
            //    lstUnitList.Add(lstUnitTemp[1]);
            //    lstUnitList.Add(lstUnitTemp[2]);
            //}
            //else if (lstUnitTemp.Count == 10) // Size = 4 のみ
            //{
            //    lstUnitList.Add(lstUnitTemp[6]);
            //    lstUnitList.Add(lstUnitTemp[7]);
            //    lstUnitList.Add(lstUnitTemp[8]);
            //    lstUnitList.Add(lstUnitTemp[9]);

            //    lstUnitList.Add(lstUnitTemp[2]);
            //    lstUnitList.Add(lstUnitTemp[3]);
            //    lstUnitList.Add(lstUnitTemp[4]);
            //    lstUnitList.Add(lstUnitTemp[5]);

            //    lstUnitList.Add(lstUnitTemp[0]);
            //    lstUnitList.Add(lstUnitTemp[1]);
            //}
            //else if (lstUnitTemp.Count == 11) // Size = 4 のみ
            //{
            //    lstUnitList.Add(lstUnitTemp[7]);
            //    lstUnitList.Add(lstUnitTemp[8]);
            //    lstUnitList.Add(lstUnitTemp[9]);
            //    lstUnitList.Add(lstUnitTemp[10]);

            //    lstUnitList.Add(lstUnitTemp[3]);
            //    lstUnitList.Add(lstUnitTemp[4]);
            //    lstUnitList.Add(lstUnitTemp[5]);
            //    lstUnitList.Add(lstUnitTemp[6]);

            //    lstUnitList.Add(lstUnitTemp[0]);
            //    lstUnitList.Add(lstUnitTemp[1]);
            //    lstUnitList.Add(lstUnitTemp[2]);
            //}
            //else if (lstUnitTemp.Count == 12) // Size = 4 のみ
            //{
            //    lstUnitList.Add(lstUnitTemp[8]);
            //    lstUnitList.Add(lstUnitTemp[9]);
            //    lstUnitList.Add(lstUnitTemp[10]);
            //    lstUnitList.Add(lstUnitTemp[11]);

            //    lstUnitList.Add(lstUnitTemp[4]);
            //    lstUnitList.Add(lstUnitTemp[5]);
            //    lstUnitList.Add(lstUnitTemp[6]);
            //    lstUnitList.Add(lstUnitTemp[7]);

            //    lstUnitList.Add(lstUnitTemp[0]);
            //    lstUnitList.Add(lstUnitTemp[1]);
            //    lstUnitList.Add(lstUnitTemp[2]);
            //    lstUnitList.Add(lstUnitTemp[3]);
            //}
            #endregion
        }

        private void setAdjacentUnitControllerToTarget(UnitInfo targetUnit)
        {
            UnitInfo adjacentUnit;

            // Top Cabinet
            storeAdjacentUnit(targetUnit, AdjustmentPosition.Top, out adjacentUnit);

            if (adjacentUnit != null && targetUnit.ControllerID != adjacentUnit.ControllerID)
            { dicController[adjacentUnit.ControllerID].Target = true; }

            // Right Cabinet
            storeAdjacentUnit(targetUnit, AdjustmentPosition.Right, out adjacentUnit);

            if (adjacentUnit != null && targetUnit.ControllerID != adjacentUnit.ControllerID)
            { dicController[adjacentUnit.ControllerID].Target = true; }

            // Bottom Cabinet
            storeAdjacentUnit(targetUnit, AdjustmentPosition.Bottom, out adjacentUnit);

            if (adjacentUnit != null && targetUnit.ControllerID != adjacentUnit.ControllerID)
            { dicController[adjacentUnit.ControllerID].Target = true; }

            // Left Cabinet
            storeAdjacentUnit(targetUnit, AdjustmentPosition.Left, out adjacentUnit);

            if (adjacentUnit != null && targetUnit.ControllerID != adjacentUnit.ControllerID)
            { dicController[adjacentUnit.ControllerID].Target = true; }
        }

        private void storeAdjacentUnit(UnitInfo targetUnit, AdjustmentPosition position, out UnitInfo adjacentUnit)
        {
            adjacentUnit = null;
            CabinetOffset offset;

            offset = RelativePosition.GetCabinetOffset(position);
            int x = targetUnit.X - 1 + offset.X;
            int y = targetUnit.Y - 1 + offset.Y;

            if (x >= INT_ZERO && x < allocInfo.MaxX && y >= INT_ZERO && y < allocInfo.MaxY)
            {
                adjacentUnit = aryUnitUf[x, y].UnitInfo;
            }
        }

        private void saveSelectionInfo()
        {
            string filePath = applicationPath + "\\Log\\";
            string text = "[" + DateTime.Now.ToString() + "]\r\n";
            string date = DateTime.Now.ToString("_yyyyMMdd");

            filePath += "ErrorSelectionInfo" + date + ".txt";

            for (int x = 0; x < allocInfo.MaxX; x++)
            {
                for (int y = 0; y < allocInfo.MaxY; y++)
                {
                    try { text += "x = " + x.ToString() + ", y = " + y.ToString() + ", Selection = " + aryUnitUf[x, y].IsChecked.ToString() + "\r\n"; }
                    catch { }
                }
            }

            try
            {
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(filePath, true))
                { sw.WriteLine(text); }
            }
            catch { }

            // Currect Profileを保存
            date = DateTime.Now.ToString("_yyyyMMddHHmm");
            filePath = applicationPath + "\\Log\\" + System.IO.Path.GetFileNameWithoutExtension(Settings.Ins.LastProfile) + date + ".xml";
            try
            { File.Copy(Settings.Ins.LastProfile, filePath); }
            catch { }
        }

        /// <summary>
        /// 指定キャビネット、データタイプのバックアップが取得されているかを確認
        /// 全てのフォルダになかった場合のみfalseを返し、存在するフォルダがあった場合はtargetDirに格納してtrueを返す
        /// </summary>
        /// <param name="lstUnit">チェックするキャビネットリスト</param>
        /// <param name="targetDir">指定ファイルが存在するディレクトリ</param>
        /// <param name="dataType">チェックするデータタイプ</param>
        /// <returns></returns>
        private bool checkDataFile(List<UnitInfo> lstUnit, out FileDirectory targetDir, DataType dataType = DataType.HcData)
        {
            if (Settings.Ins.ExecLog == true)
            {
                SaveExecLog("Start checkDataFile >>");
            }
            bool status = true;

            // Backup_Latest
            foreach (UnitInfo unit in lstUnit)
            {
                string filePath = makeFilePath(unit, FileDirectory.Backup_Latest, dataType);

                if (System.IO.File.Exists(filePath) != true)
                {
                    status = false;
                    if (Settings.Ins.ExecLog == true)
                    {
                        SaveExecLog(filePath + " File not found.");
                    }
                    break;
                }
            }

            if (status == true)
            {
                targetDir = FileDirectory.Backup_Latest;
                return true;
            }
            else { status = true; }

            // Backup_Previous
            foreach (UnitInfo unit in lstUnit)
            {
                string filePath = makeFilePath(unit, FileDirectory.Backup_Previous, dataType);

                if (System.IO.File.Exists(filePath) != true)
                {
                    status = false;
                    if (Settings.Ins.ExecLog == true)
                    {
                        SaveExecLog(filePath + " File not found.");
                    }
                    break;
                }
            }

            if (status == true)
            {
                targetDir = FileDirectory.Backup_Previous;
                return true;
            }
            else { status = true; }

            // Backup_Initial
            foreach (UnitInfo unit in lstUnit)
            {
                string filePath = makeFilePath(unit, FileDirectory.Backup_Initial, dataType);

                if (System.IO.File.Exists(filePath) != true)
                {
                    status = false;
                    if (Settings.Ins.ExecLog == true)
                    {
                        SaveExecLog(filePath + " File not found.");
                    }
                    break;
                }
            }

            if (status == true)
            {
                targetDir = FileDirectory.Backup_Initial;
                return true;
            }

            targetDir = FileDirectory.Backup_Initial;

            return false;
        }

        #region Settings

        private bool setCA410SettingDispatch(CaSdk.DisplayMode mode = CaSdk.DisplayMode.DispModeXYZ)
        {
            var dispatcher = Application.Current.Dispatcher;
            return dispatcher.Invoke(() => setCA410Setting(mode));
        }

        private bool setCA410Setting(CaSdk.DisplayMode mode = CaSdk.DisplayMode.DispModeXYZ) // CA-410の設定
        {
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[0] setCA410Setting start"); }

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

            if (setAveragingModeSub(CaSdk.AveragingMode.AveragingFast) != true)
            { return false; }

            // Brightness Unit
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[6] setBrightnessCabinetSub()"); }

            if (setBrightnessUnitSub(CaSdk.BrightnessUnit.BrightUnitcdm2) != true)
            { return false; }

            return true;
        }

        private bool getUserSetting(out List<UserSetting> lstUserSetting)
        {
            if (Settings.Ins.ExecLog == true)
            {
                SaveExecLog("\t[0] getUserSetting start ( Controller Count : " + dicController.Count.ToString() + " )");
                //saveExecLog(dicController.ToString());
            }

            string buff;
            lstUserSetting = new List<UserSetting>();

            foreach (ControllerInfo controller in dicController.Values)
            {
                if (controller.Target == true)
                {
                    if (Settings.Ins.ExecLog == true)
                    { SaveExecLog("\t[*1] (Controller Loop) ControllerID : " + controller.ControllerID.ToString()); }

                    UserSetting user = new UserSetting();

                    user.ControllerID = controller.ControllerID;

                    // Temp Corection
                    if (Settings.Ins.ExecLog == true)
                    { SaveExecLog("\t[*2] Temp Corection"); }

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

                    // Low Brightness Mode
                    if (Settings.Ins.ExecLog == true)
                    { SaveExecLog("\t[*3] Low Brightness Mode"); }

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

                    // Signal Mode 2
                    if (Settings.Ins.ExecLog == true)
                    { SaveExecLog("\t[*4] Signal Mode (2)"); }

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

                    // Fan Mode
                    //if (Settings.Ins.ExecLog == true)
                    //{ SaveExecLog("\t[*4] Fan Mode"); }

                    //System.Threading.Thread.Sleep(1000);
                    //if (sendSdcpCommand(SDCPClass.CmdUnitFanModeGet, out buff, controller.IPAddress) != true)
                    //{ return false; }
                    //try { user.UnitFanMode = Convert.ToInt32(buff, 16); }
                    //catch (Exception ex)
                    //{
                    //    string errStr = "[getUserSetting(Fan Mode)] Source : " + ex.Source + "\r\nException Messsage : " + ex.Message + "\r\nBuff = " + buff.ToString();
                    //    SaveErrorLog(errStr);
                    //    ShowMessageWindow(errStr, "Exception! (Fan Mode)", System.Drawing.SystemIcons.Error, 500, 210);
                    //    return false;
                    //}

                    //// Color Temp
                    //if (Settings.Ins.ExecLog == true)
                    //{ SaveExecLog("\t[*5] Color Temp"); }

                    //System.Threading.Thread.Sleep(1000);
                    //if (sendSdcpCommand(SDCPClass.CmdColorTempGet, out buff, controller.IPAddress) != true)
                    //{ return false; }
                    //try { user.ColorTemp = Convert.ToInt32(buff, 16); }
                    //catch (Exception ex)
                    //{
                    //    string errStr = "[getUserSetting(Color Temp)] Source : " + ex.Source + "\r\nException Messsage : " + ex.Message + "\r\nBuff = " + buff.ToString();
                    //    SaveErrorLog(errStr);
                    //    ShowMessageWindow(errStr, "Exception! (Color Temp)", System.Drawing.SystemIcons.Error, 500, 210);
                    //    return false;
                    //}

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
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isNormal">呼び出し元を判定するフラグ true:Module Gamma画面以外 false:Module Gamma画面から</param>
        /// <returns></returns>
        private bool setAdjustSetting(bool isNormal = true)
        {
            foreach (ControllerInfo cont in dicController.Values)
            {
                if (cont.Target == true)
                {
                    // Through Mode
                    if (sendSdcpCommand(SDCPClass.CmdThroughModeOn, cont.IPAddress) != true)
                    { return false; }

                    // Temp Corection
                    if (sendSdcpCommand(SDCPClass.CmdTempCorrectSet, cont.IPAddress) != true)
                    { return false; }

                    // Burn-In Correction
                    //sendSdcpCommand(SDCPClass.CmdBurnInCorrect, cont.IPAddress);

                    // Low Brightness Mode
                    if (sendSdcpCommand(SDCPClass.CmdLowBrightModeSet, cont.IPAddress) != true)
                    { return false; }

                    if (isNormal)
                    {
                        // Module Gamma画面ではSignal Mode 2の固定パラメータ設定する箇所があるので、ここでは処理しない
                        // Signal Mode 2
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

                    // Fan Mode
                    //System.Threading.Thread.Sleep(1000);
                    //if (sendSdcpCommand(SDCPClass.CmdUnitFanModeSet, cont.IPAddress) != true)
                    //{ return false; }
                }
            }

            return true;
        }

        private void setUserSetting(List<UserSetting> lstUserSetting)
        {
            if(lstUserSetting == null)
            { return; }

            foreach (UserSetting usr in lstUserSetting)
            {
                // Temp Corection
                Byte[] cmd = new byte[SDCPClass.CmdTempCorrectSet.Length];
                Array.Copy(SDCPClass.CmdTempCorrectSet, cmd, SDCPClass.CmdTempCorrectSet.Length);

                cmd[20] = (byte)usr.TempCorrection;
                sendSdcpCommand(cmd, dicController[usr.ControllerID].IPAddress);

                // Low Brightness Mode
                cmd = new byte[SDCPClass.CmdLowBrightModeSet.Length];
                Array.Copy(SDCPClass.CmdLowBrightModeSet, cmd, SDCPClass.CmdLowBrightModeSet.Length);

                cmd[20] = (byte)usr.LowBrightnessMode;
                sendSdcpCommand(cmd, dicController[usr.ControllerID].IPAddress);

                // Signal Mode 2
                cmd = new byte[SDCPClass.CmdSigModeTwoSet.Length];
                Array.Copy(SDCPClass.CmdSigModeTwoSet, cmd, SDCPClass.CmdSigModeTwoSet.Length);

                cmd[20] = (byte)usr.SignalModeTwo;
                sendSdcpCommand(cmd, dicController[usr.ControllerID].IPAddress);

                // Fan Mode
                //cmd = new byte[SDCPClass.CmdUnitFanModeSet.Length];
                //Array.Copy(SDCPClass.CmdUnitFanModeSet, cmd, SDCPClass.CmdUnitFanModeSet.Length);

                //cmd[20] = (byte)usr.UnitFanMode;

                //System.Threading.Thread.Sleep(1000);
                //sendSdcpCommand(cmd, dicController[usr.ControllerID].IPAddress);

                //// Color Temp
                //cmd = new byte[SDCPClass.CmdColorTempSet.Length];
                //Array.Copy(SDCPClass.CmdColorTempSet, cmd, SDCPClass.CmdColorTempSet.Length);

                //cmd[20] = (byte)usr.ColorTemp;

                //System.Threading.Thread.Sleep(1000);
                //sendSdcpCommand(cmd, dicController[usr.ControllerID].IPAddress);
            }
        }

        private void setNormalSetting()
        {
            foreach (ControllerInfo cont in dicController.Values)
            {
                if (cont.Target == true)
                {
                    // Through Mode
                    sendSdcpCommand(SDCPClass.CmdThroughModeOff, cont.IPAddress);

                    // User Settingに戻す仕様に変更
                    // Color Temp
                    //System.Threading.Thread.Sleep(1000);
                    //sendSdcpCommand(SDCPClass.CmdColorTempSet, cont.IPAddress); // D93
                }
            }
        }

        #endregion Settings

        #region Measure

        private bool measure4Points(UnitInfo unit, out double[][][] measureData, out UserOperation operation)
        {
            bool status;
            double[][][] pointData = new double[4][][];

            measureData = new double[0][][];

#if DEBUG
            Console.WriteLine($"Target Cabinet: C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}");
            SaveExecLog($"     Target Cabinet: C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}");
#elif Release_log
            SaveExecLog($"     Target Cabinet: C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}");
#endif

            // Top
            while (true)
            {

#if DEBUG
                Console.WriteLine($"CrossPointPosition: Top");
                SaveExecLog($"     CrossPointPosition: Top");
#elif Release_log
                SaveExecLog($"     CrossPointPosition: Top");
#endif

                status = measurePoint(unit, CrossPointPosition.Top, out pointData[0], out operation, true);

                if (status == true)
                { break; }
                else if(operation == UserOperation.Cancel)
                {
                    bool? result;
                    string msg = "Do you want to finish the measurement ?";

                    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                    if (result != true)
                    { continue; }

                    return false;
                }
                else { return false; }
            }

            // Right
            while (true)
            {

#if DEBUG
                Console.WriteLine($"CrossPointPosition: Right");
                SaveExecLog($"     CrossPointPosition: Right");
#elif Release_log
                SaveExecLog($"     CrossPointPosition: Right");
#endif

                status = measurePoint(unit, CrossPointPosition.Right, out pointData[1], out operation, true);

                if (status == true)
                { break; }
                else if (operation == UserOperation.Cancel)
                {
                    bool? result;
                    string msg = "Do you want to finish the measurement ?";

                    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                    if (result != true)
                    { continue; }

                    return false;
                }
                else if (operation == UserOperation.Rewind)
                {
                    operation = UserOperation.Repeat;
                    return false;
                }
                else
                { return false; }
            }

            // Bottom
            while (true)
            {

#if DEBUG
                Console.WriteLine($"CrossPointPosition: Bottom");
                SaveExecLog($"     CrossPointPosition: Bottom");
#elif Release_log
                SaveExecLog($"     CrossPointPosition: Bottom");
#endif

                status = measurePoint(unit, CrossPointPosition.Bottom, out pointData[2], out operation, true);

                if (status == true)
                { break; }
                else if (operation == UserOperation.Cancel)
                {
                    bool? result;
                    string msg = "Do you want to finish the measurement ?";

                    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                    if (result != true)
                    { continue; }

                    return false;
                }
                else if (operation == UserOperation.Rewind)
                {
                    operation = UserOperation.Repeat;
                    return false;
                }
                else
                { return false; }
            }

            // Left
            while (true)
            {

#if DEBUG
                Console.WriteLine($"CrossPointPosition: Left");
                SaveExecLog($"     CrossPointPosition: Left");
#elif Release_log
                SaveExecLog($"     CrossPointPosition: Left");
#endif

                status = measurePoint(unit, CrossPointPosition.Left, out pointData[3], out operation, true);

                if (status == true)
                { break; }
                else if (operation == UserOperation.Cancel)
                {
                    bool? result;
                    string msg = "Do you want to finish the measurement ?";

                    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                    if (result != true)
                    { continue; }

                    return false;
                }
                else if (operation == UserOperation.Rewind)
                {
                    operation = UserOperation.Repeat;
                    return false;
                }
                else
                { return false; }
            }

            // Calc Cell Color Data
            copyColorData(pointData, out measureData);

            // TestPattern OFF
            //foreach (ControllerInfo cont in dicController.Values)
            //{ sendSdcpCommand(SDCPClass.CmdIntSignalOff, cont.IPAddress); }

            return true;
        }

        private bool measure8Points(UnitInfo unit, out double[][][] measureData, out UserOperation operation)
        {
            bool status;
            double[][][] pointData = new double[8][][];
            NeighboringCells neighboringCells;

            measureData = new double[0][][];
            operation = UserOperation.None;

            // 隣接するCellを確認
            status = checkNeighboringCells(unit, -1, out neighboringCells); // 第二引数はCell番号、この場合、無効
            if (status != true)
            {
                ShowMessageWindow("Failed in checkNeighboringModules().", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }

#if DEBUG
            Console.WriteLine($"Target Cabinet: C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}");
            SaveExecLog($"     Target Cabinet: C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}");
#elif Release_log
            SaveExecLog($"     Target Cabinet: C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}");
#endif

            // ◆Target Unit
            // Top
            while (true)
            {

#if DEBUG
                Console.WriteLine($"CrossPointPosition: Top");
                SaveExecLog($"     CrossPointPosition: Top");
#elif Release_log
                SaveExecLog($"     CrossPointPosition: Top");
#endif

                status = measurePoint(unit, CrossPointPosition.Top, out pointData[0], out operation);

                if (status == true)
                { break; }
                else if (operation == UserOperation.Cancel)
                {
                    bool? result;
                    string msg = "Do you want to finish the measurement ?";

                    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                    if (result != true)
                    { continue; }
                    else
                    { return false; }
                }
                else { return false; }
            }

            // Right
            while (true)
            {

#if DEBUG
                Console.WriteLine($"CrossPointPosition: Right");
                SaveExecLog($"     CrossPointPosition: Right");
#elif Release_log
                SaveExecLog($"     CrossPointPosition: Right");
#endif

                status = measurePoint(unit, CrossPointPosition.Right, out pointData[1], out operation);

                if (status == true)
                { break; }
                else if (operation == UserOperation.Cancel)
                {
                    bool? result;
                    string msg = "Do you want to finish the measurement ?";

                    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                    if (result != true)
                    { continue; }
                    else
                    { return false; }
                }
                else { return false; }
            }

            // Bottom
            while (true)
            {

#if DEBUG
                Console.WriteLine($"CrossPointPosition: Bottom");
                SaveExecLog($"     CrossPointPosition: Bottom");
#elif Release_log
                SaveExecLog($"     CrossPointPosition: Bottom");
#endif

                status = measurePoint(unit, CrossPointPosition.Bottom, out pointData[2], out operation);

                if (status == true)
                { break; }
                else if (operation == UserOperation.Cancel)
                {
                    bool? result;
                    string msg = "Do you want to finish the measurement ?";

                    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                    if (result != true)
                    { continue; }
                    else
                    { return false; }
                }
                else { return false; }
            }

            // Left
            while (true)
            {

#if DEBUG
                Console.WriteLine($"CrossPointPosition: Left");
                SaveExecLog($"     CrossPointPosition: Left");
#elif Release_log
                SaveExecLog($"     CrossPointPosition: Left");
#endif

                status = measurePoint(unit, CrossPointPosition.Left, out pointData[3], out operation);

                if (status == true)
                { break; }
                else if (operation == UserOperation.Cancel)
                {
                    bool? result;
                    string msg = "Do you want to finish the measurement ?";

                    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                    if (result != true)
                    { continue; }
                    else
                    { return false; }
                }
                else { return false; }
            }

            // ◆ Neighboring Unit
            // Top
            if (neighboringCells.UpperCell.UnitInfo != null)
            {

#if DEBUG
                Console.WriteLine($"Upper Cabinet: C{neighboringCells.UpperCell.UnitInfo.ControllerID}-{neighboringCells.UpperCell.UnitInfo.PortNo}-{neighboringCells.UpperCell.UnitInfo.UnitNo}");
                SaveExecLog($"     Upper Cabinet: C{neighboringCells.UpperCell.UnitInfo.ControllerID}-{neighboringCells.UpperCell.UnitInfo.PortNo}-{neighboringCells.UpperCell.UnitInfo.UnitNo}");
#elif Release_log
                SaveExecLog($"     Upper Cabinet: C{neighboringCells.UpperCell.UnitInfo.ControllerID}-{neighboringCells.UpperCell.UnitInfo.PortNo}-{neighboringCells.UpperCell.UnitInfo.UnitNo}");
#endif

                while (true)
                {

#if DEBUG
                    Console.WriteLine($"CrossPointPosition: Bottom");
                    SaveExecLog($"     CrossPointPosition: Bottom");
#elif Release_log
                    SaveExecLog($"     CrossPointPosition: Bottom");
#endif

                    status = measurePoint(neighboringCells.UpperCell.UnitInfo, CrossPointPosition.Bottom, out pointData[4], out operation);

                    if (status == true)
                    { break; }
                    else if (operation == UserOperation.Cancel)
                    {
                        bool? result;
                        string msg = "Do you want to finish the measurement ?";

                        showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                        if (result != true)
                        { continue; }
                        else
                        { return false; }
                    }
                    else { return false; }
                }
            }
            else { pointData[4] = null; }

            // Right
            if (neighboringCells.RightCell.UnitInfo != null)
            {

#if DEBUG
                Console.WriteLine($"Right Cabinet: C{neighboringCells.RightCell.UnitInfo.ControllerID}-{neighboringCells.RightCell.UnitInfo.PortNo}-{neighboringCells.RightCell.UnitInfo.UnitNo}");
                SaveExecLog($"     Right Cabinet: C{neighboringCells.RightCell.UnitInfo.ControllerID}-{neighboringCells.RightCell.UnitInfo.PortNo}-{neighboringCells.RightCell.UnitInfo.UnitNo}");
#elif Release_log
                SaveExecLog($"     Right Cabinet: C{neighboringCells.RightCell.UnitInfo.ControllerID}-{neighboringCells.RightCell.UnitInfo.PortNo}-{neighboringCells.RightCell.UnitInfo.UnitNo}");
#endif

                while (true)
                {

#if DEBUG
                    Console.WriteLine($"CrossPointPosition: Left");
                    SaveExecLog($"     CrossPointPosition: Left");
#elif Release_log
                    SaveExecLog($"     CrossPointPosition: Left");
#endif

                    status = measurePoint(neighboringCells.RightCell.UnitInfo, CrossPointPosition.Left, out pointData[5], out operation);

                    if (status == true)
                    { break; }
                    else if (operation == UserOperation.Cancel)
                    {
                        bool? result;
                        string msg = "Do you want to finish the measurement ?";

                        showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                        if (result != true)
                        { continue; }
                        else
                        { return false; }
                    }
                    else { return false; }
                }
            }
            else { pointData[5] = null; }

            // Bottom
            if (neighboringCells.DownwardCell.UnitInfo != null)
            {

#if DEBUG
                Console.WriteLine($"Downward Cabinet: C{neighboringCells.DownwardCell.UnitInfo.ControllerID}-{neighboringCells.DownwardCell.UnitInfo.PortNo}-{neighboringCells.DownwardCell.UnitInfo.UnitNo}");
                SaveExecLog($"     Downward Cabinet: C{neighboringCells.DownwardCell.UnitInfo.ControllerID}-{neighboringCells.DownwardCell.UnitInfo.PortNo}-{neighboringCells.DownwardCell.UnitInfo.UnitNo}");
#elif Release_log
                SaveExecLog($"     Downward Cabinet: C{neighboringCells.DownwardCell.UnitInfo.ControllerID}-{neighboringCells.DownwardCell.UnitInfo.PortNo}-{neighboringCells.DownwardCell.UnitInfo.UnitNo}");
#endif

                while (true)
                {

#if DEBUG
                    Console.WriteLine($"CrossPointPosition: Top");
                    SaveExecLog($"     CrossPointPosition: Top");
#elif Release_log
                    SaveExecLog($"     CrossPointPosition: Top");
#endif

                    status = measurePoint(neighboringCells.DownwardCell.UnitInfo, CrossPointPosition.Top, out pointData[6], out operation);

                    if (status == true)
                    { break; }
                    else if (operation == UserOperation.Cancel)
                    {
                        bool? result;
                        string msg = "Do you want to finish the measurement ?";

                        showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                        if (result != true)
                        { continue; }
                        else
                        { return false; }
                    }
                    else { return false; }
                }
            }
            else { pointData[6] = null; }

            // Left
            if (neighboringCells.LeftCell.UnitInfo != null)
            {

#if DEBUG
                Console.WriteLine($"Left Cabinet: C{neighboringCells.LeftCell.UnitInfo.ControllerID}-{neighboringCells.LeftCell.UnitInfo.PortNo}-{neighboringCells.LeftCell.UnitInfo.UnitNo}");
                SaveExecLog($"     Left Cabinet: C{neighboringCells.LeftCell.UnitInfo.ControllerID}-{neighboringCells.LeftCell.UnitInfo.PortNo}-{neighboringCells.LeftCell.UnitInfo.UnitNo}");
#elif Release_log
                SaveExecLog($"     Left Cabinet: C{neighboringCells.LeftCell.UnitInfo.ControllerID}-{neighboringCells.LeftCell.UnitInfo.PortNo}-{neighboringCells.LeftCell.UnitInfo.UnitNo}");
#endif


                while (true)
                {

#if DEBUG
                    Console.WriteLine($"CrossPointPosition: Right");
                    SaveExecLog($"     CrossPointPosition: Right");
#elif Release_log
                    SaveExecLog($"     CrossPointPosition: Right");
#endif

                    status = measurePoint(neighboringCells.LeftCell.UnitInfo, CrossPointPosition.Right, out pointData[7], out operation);

                    if (status == true)
                    { break; }
                    else if (operation == UserOperation.Cancel)
                    {
                        bool? result;
                        string msg = "Do you want to finish the measurement ?";

                        showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                        if (result != true)
                        { continue; }
                        else
                        { return false; }
                    }
                    else { return false; }
                }
            }
            else { pointData[7] = null; }

            // Copy Color Data
            copyColorData(pointData, out measureData);

            // TestPattern OFF
            //foreach (ControllerInfo cont in dicController.Values)
            //{ sendSdcpCommand(SDCPClass.CmdIntSignalOff, cont.IPAddress); }
            
            return true;
        }

        private bool measure9Points(UnitInfo unit, out double[][][] measureData, out UserOperation operation)
        {
            bool status;
            double[][][] pointData = new double[9][][];

            measureData = new double[0][][];

            // Top-Left
            while (true)
            {
                status = measurePoint(unit, CrossPointPosition.TopLeft, out pointData[0], out operation, true);

                if (status == true)
                { break; }
                else if (operation == UserOperation.Cancel)
                {
                    bool? result;
                    string msg = "Do you want to finish the measurement ?";

                    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                    if (result != true)
                    { continue; }

                    return false;
                }
                else { return false; }
            }

            // Top
            while (true)
            {
                status = measurePoint(unit, CrossPointPosition.Top, out pointData[1], out operation, true);

                if (status == true)
                { break; }
                else if (operation == UserOperation.Cancel)
                {
                    bool? result;
                    string msg = "Do you want to finish the measurement ?";

                    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                    if (result != true)
                    { continue; }

                    return false;
                }
                else { return false; }
            }

            // Top-Right
            while (true)
            {
                status = measurePoint(unit, CrossPointPosition.TopRight, out pointData[2], out operation, true);

                if (status == true)
                { break; }
                else if (operation == UserOperation.Cancel)
                {
                    bool? result;
                    string msg = "Do you want to finish the measurement ?";

                    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                    if (result != true)
                    { continue; }

                    return false;
                }
                else { return false; }
            }

            // Left
            while (true)
            {
                status = measurePoint(unit, CrossPointPosition.Left, out pointData[3], out operation, true);

                if (status == true)
                { break; }
                else if (operation == UserOperation.Cancel)
                {
                    bool? result;
                    string msg = "Do you want to finish the measurement ?";

                    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                    if (result != true)
                    { continue; }

                    return false;
                }
                else if (operation == UserOperation.Rewind)
                {
                    operation = UserOperation.Repeat;
                    return false;
                }
                else
                { return false; }
            }

            // Center
            while (true)
            {
                status = measurePoint(unit, CrossPointPosition.Center, out pointData[4], out operation, true);

                if (status == true)
                { break; }
                else if (operation == UserOperation.Cancel)
                {
                    bool? result;
                    string msg = "Do you want to finish the measurement ?";

                    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                    if (result != true)
                    { continue; }

                    return false;
                }
                else if (operation == UserOperation.Rewind)
                {
                    operation = UserOperation.Repeat;
                    return false;
                }
                else
                { return false; }
            }

            // Right
            while (true)
            {
                status = measurePoint(unit, CrossPointPosition.Right, out pointData[5], out operation, true);

                if (status == true)
                { break; }
                else if (operation == UserOperation.Cancel)
                {
                    bool? result;
                    string msg = "Do you want to finish the measurement ?";

                    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                    if (result != true)
                    { continue; }

                    return false;
                }
                else if (operation == UserOperation.Rewind)
                {
                    operation = UserOperation.Repeat;
                    return false;
                }
                else
                { return false; }
            }

            // Bottom-Left
            while (true)
            {
                status = measurePoint(unit, CrossPointPosition.BottomLeft, out pointData[6], out operation, true);

                if (status == true)
                { break; }
                else if (operation == UserOperation.Cancel)
                {
                    bool? result;
                    string msg = "Do you want to finish the measurement ?";

                    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                    if (result != true)
                    { continue; }

                    return false;
                }
                else if (operation == UserOperation.Rewind)
                {
                    operation = UserOperation.Repeat;
                    return false;
                }
                else
                { return false; }
            }

            // Bottom
            while (true)
            {
                status = measurePoint(unit, CrossPointPosition.Bottom, out pointData[7], out operation, true);

                if (status == true)
                { break; }
                else if (operation == UserOperation.Cancel)
                {
                    bool? result;
                    string msg = "Do you want to finish the measurement ?";

                    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                    if (result != true)
                    { continue; }

                    return false;
                }
                else if (operation == UserOperation.Rewind)
                {
                    operation = UserOperation.Repeat;
                    return false;
                }
                else
                { return false; }
            }

            // Bottom-Right
            while (true)
            {
                status = measurePoint(unit, CrossPointPosition.BottomRight, out pointData[8], out operation, true);

                if (status == true)
                { break; }
                else if (operation == UserOperation.Cancel)
                {
                    bool? result;
                    string msg = "Do you want to finish the measurement ?";

                    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                    if (result != true)
                    { continue; }

                    return false;
                }
                else if (operation == UserOperation.Rewind)
                {
                    operation = UserOperation.Repeat;
                    return false;
                }
                else
                { return false; }
            }

            // Calc Cell Color Data
            copyColorData(pointData, out measureData);

            // TestPattern OFF
            //foreach (ControllerInfo cont in dicController.Values)
            //{ sendSdcpCommand(SDCPClass.CmdIntSignalOff, cont.IPAddress); }

            return true;
        }

        //private bool measurePoint(UnitInfo unit, CrossPointPosition point, out double[][] measureData)
        //{
        //    UserOperation ope = UserOperation.None;

        //    return measurePoint(unit, point, out measureData, out ope, false);
        //}

        private bool measurePoint(UnitInfo unit, CrossPointPosition point, out double[][] measureData, out UserOperation operation, bool enableRewind = false, bool singleMeasure = false)
        {
            bool status;
            UserOperation ope = UserOperation.None;

            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            {
                measureData = new double[3][];
                for (int rgb = 0; rgb < 3; rgb++)
                { measureData[rgb] = new double[3]; }
            }
            else
            {
                measureData = new double[4][];
                for (int rgbw = 0; rgbw < 4; rgbw++)
                { measureData[rgbw] = new double[3]; }
            }

            // 測定ポイントの十字を表示
            outputCross(unit, point);

            status = Dispatcher.Invoke(() => showMeasureWindow(out ope, enableRewind));
            operation = ope;

            if(status != true) // OK以外はfalseが返る
            { return false; }

            // Red
            outputWindow(unit, point, CellColor.Red);
            System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait); // 画が表示されるまでWait

            status = measureColor(ufTargetChrom.Red, out measureData[0], CaSdk.DisplayMode.DispModeXYZ, ref operation);
            if (status != true)
            { return false; }

#if DEBUG
            Console.WriteLine($"R(X): {measureData[RED][XYZ_X]}\r\nR(Y): {measureData[RED][XYZ_Y]}\r\nR(Z): {measureData[RED][XYZ_Z]}");
            SaveExecLog($"      R(X): {measureData[RED][XYZ_X]}");
            SaveExecLog($"      R(Y): {measureData[RED][XYZ_Y]}");
            SaveExecLog($"      R(Z): {measureData[RED][XYZ_Z]}");
#elif Release_log
            SaveExecLog($"      R(X): {measureData[RED][XYZ_X]}");
            SaveExecLog($"      R(Y): {measureData[RED][XYZ_Y]}");
            SaveExecLog($"      R(Z): {measureData[RED][XYZ_Z]}");
#endif

            playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

            // Green
            outputWindow(unit, point, CellColor.Green);
            System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

            status = measureColor(ufTargetChrom.Green, out measureData[1], CaSdk.DisplayMode.DispModeXYZ, ref operation);
            if (status != true)
            { return false; }

#if DEBUG
            Console.WriteLine($"G(X): {measureData[GREEN][XYZ_X]}\r\nG(Y): {measureData[GREEN][XYZ_Y]}\r\nG(Z): {measureData[GREEN][XYZ_Z]}");
            SaveExecLog($"      G(X): {measureData[GREEN][XYZ_X]}");
            SaveExecLog($"      G(Y): {measureData[GREEN][XYZ_Y]}");
            SaveExecLog($"      G(Z): {measureData[GREEN][XYZ_Z]}");
#elif Release_log
            SaveExecLog($"      G(X): {measureData[GREEN][XYZ_X]}");
            SaveExecLog($"      G(Y): {measureData[GREEN][XYZ_Y]}");
            SaveExecLog($"      G(Z): {measureData[GREEN][XYZ_Z]}");
#endif

            playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

            // Blue
            outputWindow(unit, point, CellColor.Blue);
            System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

            status = measureColor(ufTargetChrom.Blue, out measureData[2], CaSdk.DisplayMode.DispModeXYZ, ref operation);
            if (status != true)
            { return false; }

#if DEBUG
            Console.WriteLine($"B(X): {measureData[BLUE][XYZ_X]}\r\nB(Y): {measureData[BLUE][XYZ_Y]}\r\nB(Z): {measureData[BLUE][XYZ_Z]}");
            SaveExecLog($"      B(X): {measureData[BLUE][XYZ_X]}");
            SaveExecLog($"      B(Y): {measureData[BLUE][XYZ_Y]}");
            SaveExecLog($"      B(Z): {measureData[BLUE][XYZ_Z]}");
#elif Release_log
            SaveExecLog($"      B(X): {measureData[BLUE][XYZ_X]}");
            SaveExecLog($"      B(Y): {measureData[BLUE][XYZ_Y]}");
            SaveExecLog($"      B(Z): {measureData[BLUE][XYZ_Z]}");
#endif

            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
            {
                playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

                // White
                outputWindow(unit, point, CellColor.White);
                System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

                status = measureColor(ufTargetChrom.White, out measureData[3], CaSdk.DisplayMode.DispModeXYZ, ref operation);
                if (status != true)
                { return false; }

#if DEBUG
                Console.WriteLine($"W(X): {measureData[WHITE][XYZ_X]}\r\nW(Y): {measureData[WHITE][XYZ_Y]}\r\nW(Z): {measureData[WHITE][XYZ_Z]}");
                SaveExecLog($"      W(X): {measureData[WHITE][XYZ_X]}");
                SaveExecLog($"      W(Y): {measureData[WHITE][XYZ_Y]}");
                SaveExecLog($"      W(Z): {measureData[WHITE][XYZ_Z]}");
#elif Release_log
                SaveExecLog($"      W(X): {measureData[WHITE][XYZ_X]}");
                SaveExecLog($"      W(Y): {measureData[WHITE][XYZ_Y]}");
                SaveExecLog($"      W(Z): {measureData[WHITE][XYZ_Z]}");
#endif

            }

            playSound(applicationPath + "\\Components\\Sound\\button01b.mp3");

            return true;
        }

        private bool measureAllCells(UnitInfo unit, out double[][][] measureData, out UserOperation operation)
        {
            UserOperation ope = UserOperation.Cancel;

            // 初期化
            measureData = new double[12][][];
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            {
                for (int cell = 0; cell < 12; cell++)
                {
                    measureData[cell] = new double[3][];
                    for (int rgb = 0; rgb < 3; rgb++)
                    { measureData[cell][rgb] = new double[3]; }
                }
            }
            else
            {
                for (int cell = 0; cell < 12; cell++)
                {
                    measureData[cell] = new double[4][];
                    for (int rgbw = 0; rgbw < 4; rgbw++)
                    { measureData[cell][rgbw] = new double[3]; }
                }
            }

            operation = ope;

            // Cell分ループ
            for (int i = 0; i < moduleCount; i++)
            {
                bool status;
                double[] data;

                // Center
                outputCross(unit, i, CrossPointPosition.Center);
                
                status = Dispatcher.Invoke(() => showMeasureWindow(out ope, true));
                operation = ope;

                if(status == true)
                { } // 何もしない
                else if (operation == UserOperation.Rewind && i > 0)
                {
                    operation = UserOperation.Repeat;
                    return false;
                }
                else if (operation == UserOperation.Cancel)
                {
                    bool? result;
                    string msg = "Do you want to finish the measurement ?";

                    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                    if (result == true)
                    { return false; }
                    else
                    {
                        i -= 1;
                        continue;
                    }
                }
                else
                { return false; }

#if DEBUG
                Console.WriteLine($"Target Cabinet: C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}  Module No.{i + 1}");
                SaveExecLog($"     Target Cabinet: C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}  Module No.{i + 1}");
#elif Release_log
                SaveExecLog($"     Target Cabinet: C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}  Module No.{i + 1}");
#endif

                // Red
                outputCell(unit, CellColor.Red, (CellNum)i);
                System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

                status = measureColor(ufTargetChrom.Red, out data, CaSdk.DisplayMode.DispModeXYZ, ref operation);
                if (status == true)
                {
                    measureData[i][0][0] = data[0];
                    measureData[i][0][1] = data[1];
                    measureData[i][0][2] = data[2];
                }
                else { return false; }

#if DEBUG
                Console.WriteLine($"R(X): {measureData[i][RED][XYZ_X]}\r\nR(Y): {measureData[i][RED][XYZ_Y]}\r\nR(Z): {measureData[i][RED][XYZ_Z]}");
                SaveExecLog($"      R(X): {measureData[i][RED][XYZ_X]}");
                SaveExecLog($"      R(Y): {measureData[i][RED][XYZ_Y]}");
                SaveExecLog($"      R(Z): {measureData[i][RED][XYZ_Z]}");
#elif Release_log
                SaveExecLog($"      R(X): {measureData[i][RED][XYZ_X]}");
                SaveExecLog($"      R(Y): {measureData[i][RED][XYZ_Y]}");
                SaveExecLog($"      R(Z): {measureData[i][RED][XYZ_Z]}");
#endif

                playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

                // Green
                outputCell(unit, CellColor.Green, (CellNum)i);
                System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

                status = measureColor(ufTargetChrom.Green, out data, CaSdk.DisplayMode.DispModeXYZ, ref operation);
                if (status == true)
                {
                    measureData[i][1][0] = data[0];
                    measureData[i][1][1] = data[1];
                    measureData[i][1][2] = data[2];
                }
                else { return false; }

#if DEBUG
                Console.WriteLine($"G(X): {measureData[i][GREEN][XYZ_X]}\r\nG(Y): {measureData[i][GREEN][XYZ_Y]}\r\nG(Z): {measureData[i][GREEN][XYZ_Z]}");
                SaveExecLog($"      G(X): {measureData[i][GREEN][XYZ_X]}");
                SaveExecLog($"      G(Y): {measureData[i][GREEN][XYZ_Y]}");
                SaveExecLog($"      G(Z): {measureData[i][GREEN][XYZ_Z]}");
#elif Release_log
                SaveExecLog($"      G(X): {measureData[i][GREEN][XYZ_X]}");
                SaveExecLog($"      G(Y): {measureData[i][GREEN][XYZ_Y]}");
                SaveExecLog($"      G(Z): {measureData[i][GREEN][XYZ_Z]}");
#endif

                playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

                // Blue
                outputCell(unit, CellColor.Blue, (CellNum)i);
                System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

                status = measureColor(ufTargetChrom.Blue, out data, CaSdk.DisplayMode.DispModeXYZ, ref operation);
                if (status == true)
                {
                    measureData[i][2][0] = data[0];
                    measureData[i][2][1] = data[1];
                    measureData[i][2][2] = data[2];
                }
                else { return false; }

#if DEBUG
                Console.WriteLine($"B(X): {measureData[i][BLUE][XYZ_X]}\r\nB(Y): {measureData[i][BLUE][XYZ_Y]}\r\nB(Z): {measureData[i][BLUE][XYZ_Z]}");
                SaveExecLog($"      B(X): {measureData[i][BLUE][XYZ_X]}");
                SaveExecLog($"      B(Y): {measureData[i][BLUE][XYZ_Y]}");
                SaveExecLog($"      B(Z): {measureData[i][BLUE][XYZ_Z]}");
#elif Release_log
                SaveExecLog($"      B(X): {measureData[i][BLUE][XYZ_X]}");
                SaveExecLog($"      B(Y): {measureData[i][BLUE][XYZ_Y]}");
                SaveExecLog($"      B(Z): {measureData[i][BLUE][XYZ_Z]}");
#endif

                if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
                {
                    playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

                    // White
                    outputCell(unit, CellColor.White, (CellNum)i);
                    System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

                    status = measureColor(ufTargetChrom.White, out data, CaSdk.DisplayMode.DispModeXYZ, ref operation);
                    if (status == true)
                    {
                        measureData[i][3][0] = data[0];
                        measureData[i][3][1] = data[1];
                        measureData[i][3][2] = data[2];
                    }
                    else { return false; }

#if DEBUG
                    Console.WriteLine($"W(X): {measureData[i][WHITE][XYZ_X]}\r\nW(Y): {measureData[i][WHITE][XYZ_Y]}\r\nW(Z): {measureData[i][WHITE][XYZ_Z]}");
                    SaveExecLog($"      W(X): {measureData[i][WHITE][XYZ_X]}");
                    SaveExecLog($"      W(Y): {measureData[i][WHITE][XYZ_Y]}");
                    SaveExecLog($"      W(Z): {measureData[i][WHITE][XYZ_Z]}");
#elif Release_log
                    SaveExecLog($"      W(X): {measureData[i][WHITE][XYZ_X]}");
                    SaveExecLog($"      W(Y): {measureData[i][WHITE][XYZ_Y]}");
                    SaveExecLog($"      W(Z): {measureData[i][WHITE][XYZ_Z]}");
#endif
                }

                playSound(applicationPath + "\\Components\\Sound\\button01b.mp3");
            }

            //foreach (ControllerInfo cont in dicController.Values)
            //{ sendSdcpCommand(SDCPClass.CmdIntSignalOff, cont.IPAddress); }

            return true;
        }

        private bool measureColor(Chromaticity targetChrom, out double[] data, CaSdk.DisplayMode mode, ref UserOperation operation)
        {
            data = new double[3];

#if NO_MEASURE
            bool status = true;
#else
            while (true)
            {
                bool status = measureSub();
#endif

                if (status == true)// || (caSdk.getErrorNo() == 426))
                {
#if NO_MEASURE
#else
                    status = getDataSub(out data[0], out data[1], out data[2], 0);
#endif
                    if (status == true)
                    {
                        if (Settings.Ins.MeasAlert == true && targetChrom != null)
                        {
                            double x = 0, y = 0, Lv = 0;

                            if (mode == CaSdk.DisplayMode.DispModeXYZ)
                            {
                                x = data[0] / (data[0] + data[1] + data[2]);
                                y = data[1] / (data[0] + data[1] + data[2]);
                                Lv = data[1];
                            }
                            else if (mode == CaSdk.DisplayMode.DispModeLvxy)
                            {
                                x = data[0];
                                y = data[1];
                                Lv = data[2];
                            }

                            bool specError = false;

                            // x, y
                            if (x < targetChrom.x - Settings.Ins.xyThresh || x > targetChrom.x + Settings.Ins.xyThresh
                                || y < targetChrom.y - Settings.Ins.xyThresh || y > targetChrom.y + Settings.Ins.xyThresh)
                            { specError = true; }

                            // Y (Whiteのみ)
                            if (targetChrom.Lv != 0)
                            {
                                if (Lv < targetChrom.Lv * (1 - Settings.Ins.YThresh / 100) || Lv > targetChrom.Lv * (1 + Settings.Ins.YThresh / 100))
                                { specError = true; }
                            }

                            if (specError == true)
                            {
                                bool? result;
                                string msg = "The measured value is above the threshold.\r\nDo you want to measure again?";

                                showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 400, 200, "OK", "Cancel");

                                if (result == true)
                                {
                                    operation = UserOperation.Repeat;
                                    return false;
                                }
                                else
                                { return true; }
                            }
                        }

                        return true;
                    }
                    else
                    {
                        if (caSdk.getErrorNo() == 2) // Temp Errorの場合無視するかどうか確認する
                        {
                            if (ignoreTempError == false)
                            {
                                bool? dialogResult;
                                string msg = "[E2]Temp error has occurred.\r\n\r\nThere is a possibility that the measurement value is shifted.\r\nDo you want to continue the measurement?";

                                showMessageWindow(out dialogResult, msg, "Confirm", System.Drawing.SystemIcons.Question, 500, 210, "Yes");

                                if (dialogResult == true)
                                {
                                    ignoreTempError = true;
                                    return true;
                                }
                                else
                                {
                                    msg = "Can not get data.\r\n(Error No." + caSdk.getErrorNo() + ") " + caSdk.getErrorMessage();
                                    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 500, 210);
                                    return false;
                                }
                            }
                            else
                            { return true; }
                        }
                        else // Temp Error以外の場合
                        {
                            string msg = "Can not get data.\r\n(Error No." + caSdk.getErrorNo() + ") " + caSdk.getErrorMessage();
                            ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 500, 210);
                            return false;
                        }
                    }
                }
                else
                {
                    string msg = "Can not measure color.\r\n(Error No." + caSdk.getErrorNo() + ") " + caSdk.getErrorMessage();
                    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 500, 210);
                    return false;
                }
            }
        }

        private bool showMeasureWindow()
        {
            WindowMeasure winMeas = new WindowMeasure();

            if (winMeas.ShowDialog(false) != true)
            { return false; }

            return true;
        }

        private bool showMeasureWindow(out UserOperation ope, bool enableRewind)
        {
            WindowMeasure winMeas = new WindowMeasure();

            if (winMeas.ShowDialog(enableRewind) != true)
            {
                ope = winMeas.operation;
                return false;
            }
            else
            { ope = winMeas.operation; }

            return true;
        }

        //private bool showMeasureWindow(string Message, out UserOperation ope, bool enableRewind = false)
        //{
        //    WindowMeasure winMeas = new WindowMeasure(enableRewind);

        //    if (winMeas.ShowDialog(Message) != true)
        //    {
        //        ope = winMeas.operation;
        //        return false;
        //    }
        //    else
        //    { ope = winMeas.operation; }

        //    return true;
        //}

        #endregion Measure

        #region Signal

        private void outputRaster(int level, int targetController = -1)
        {
            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            // Pattern Raster
            cmd[21] += 0x00; // Raster

            // Foreground Color
            cmd[22] = (byte)(level >> 8); // Red
            cmd[23] = (byte)(level & 0xFF);
            cmd[24] = (byte)(level >> 8); // Green
            cmd[25] = (byte)(level & 0xFF);
            cmd[26] = (byte)(level >> 8); // Blue
            cmd[27] = (byte)(level & 0xFF);
            
            foreach (ControllerInfo cont in dicController.Values)
            {
                // TargetControllerが指定されていない(-1)場合はすべてのControllerに対して送信する
                if (targetController == -1 || targetController == cont.ControllerID)
                { sendSdcpCommand(cmd, 0, cont.IPAddress); }
            }            
        }

        private void outputCross(UnitInfo unit, CrossPointPosition point)
        {
            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            int startH = 0, startV = 0;

            // Unit Offset
            startH = unit.PixelX;
            startV = unit.PixelY;

            if (curType == CursorType.WhiteCross)
            {
                switch (point)
                {
                    case CrossPointPosition.Top:
                        //startH += 160; // 1/2 Unit
                        startH += cabiDx / 2;
                        startV += 20; // 20画素(カーソルサイズの半分)
                        break;
                    case CrossPointPosition.Bottom:
                        //startH += 160; // 1/2 Unit
                        //startV += 360 - 20; // 20画素
                        startH += cabiDx / 2;
                        startV += cabiDy - 20;
                        break;

                    case CrossPointPosition.Right:
                        //startH += 320 - 20; // 20画素
                        //startV += 180; // 1/2 Unit
                        startH += cabiDx - 20;
                        startV += cabiDy / 2;
                        break;
                    case CrossPointPosition.Left:
                        startH += 20; // 20画素
                        //startV += 180; // 1/2 Unit
                        startV += cabiDy / 2;
                        break;
                    case CrossPointPosition.Calib:
                        //startH += 140;
                        //startV += 135;
                        // 中心付近でModule境界に掛からない位置
                        startH += cabiDx / 2 - 20;
                        startV += cabiDy / 2 - 20;
                        break;
                    case CrossPointPosition.Center:
                        //startH += 160; // 1/2 Unit
                        //startV += 180; // 1/2 Unit
                        startH += cabiDx / 2;
                        startV += cabiDy / 2;
                        break;
                    case CrossPointPosition.TopLeft:
                        startH += 20;
                        startV += 20;
                        break;
                    case CrossPointPosition.TopRight:
                        startH += cabiDx - 20;
                        startV += 20;
                        break;
                    case CrossPointPosition.BottomLeft:
                        startH += 20;
                        startV += cabiDy - 20;
                        break;
                    case CrossPointPosition.BottomRight:
                        startH += cabiDx - 20;
                        startV += cabiDy - 20;
                        break;
                    case CrossPointPosition.RadiatorL_Top:
                        startH += cabiDx / 4;
                        startV += 20;
                        break;
                    case CrossPointPosition.RadiatorR_Top:
                        startH += cabiDx * 3 / 4;
                        startV += 20;
                        break;
                    case CrossPointPosition.RadiatorL_Right:
                        startH += cabiDx / 2 - 20;
                        startV += cabiDy / 2;
                        break;
                    case CrossPointPosition.RadiatorR_Left:
                        startH += cabiDx / 2 + 20;
                        startV += cabiDy / 2;
                        break;
                    case CrossPointPosition.RadiatorL_Bottom:
                        startH += cabiDx / 4;
                        startV += cabiDy - 20;
                        break;
                    case CrossPointPosition.RadiatorR_Bottom:
                        startH += cabiDx * 3 / 4;
                        startV += cabiDy - 20;
                        break;
                    case CrossPointPosition.Module_0:
                        startH += modDx / 2;
                        startV += modDy / 2;
                        break;
                    case CrossPointPosition.Module_1:
                        startH += modDx + modDx / 2;
                        startV += modDy / 2;
                        break;
                    case CrossPointPosition.Module_2:
                        startH += modDx * 2 + modDx / 2;
                        startV += modDy / 2;
                        break;
                    case CrossPointPosition.Module_3:
                        startH += modDx * 3 + modDx / 2;
                        startV += modDy / 2;
                        break;
                    case CrossPointPosition.Module_4:
                        startH += modDx / 2;
                        startV += modDy + modDy / 2;
                        break;
                    case CrossPointPosition.Module_5:
                        startH += modDx + modDx / 2;
                        startV += modDy + modDy / 2;
                        break;
                    case CrossPointPosition.Module_6:
                        startH += modDx * 2 + modDx / 2;
                        startV += modDy + modDy / 2;
                        break;
                    case CrossPointPosition.Module_7:
                        startH += modDx * 3 + modDx / 2;
                        startV += modDy + modDy / 2;
                        break;
                    case CrossPointPosition.Module_8:
                        startH += modDx / 2;
                        startV += modDy * 2 + modDy / 2;
                        break;
                    case CrossPointPosition.Module_9:
                        startH += modDx + modDx / 2;
                        startV += modDy * 2 + modDy / 2;
                        break;
                    case CrossPointPosition.Module_10:
                        startH += modDx * 2 + modDx / 2;
                        startV += modDy * 2 + modDy / 2;
                        break;
                    case CrossPointPosition.Module_11:
                        startH += modDx * 3 + modDx / 2;
                        startV += modDy * 2 + modDy / 2;
                        break;
                    default:
                        throw new Exception("The position is not specified properly.");
                }

                // Pattern Cross
                cmd[21] += 0x03;

                // Foreground Color
                cmd[22] = (byte)(brightness.UF_20pc >> 8); // Red
                cmd[23] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[24] = (byte)(brightness.UF_20pc >> 8); // Green
                cmd[25] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[26] = (byte)(brightness.UF_20pc >> 8); // Blue
                cmd[27] = (byte)(brightness.UF_20pc & 0xFF);

                // Start Position
                cmd[34] = (byte)(startH >> 8);
                cmd[35] = (byte)(startH & 0xFF);
                cmd[36] = (byte)(startV >> 8);
                cmd[37] = (byte)(startV & 0xFF);

                // H, V Width
                cmd[38] = 0x00;
                cmd[39] = 0x02;
                cmd[40] = 0x00;
                cmd[41] = 0x02;

                foreach (ControllerInfo cont in dicController.Values)
                {
                    if (cont.ControllerID == unit.ControllerID)
                    { sendSdcpCommand(cmd, 0, cont.IPAddress); }
                    else
                    { outputRaster(0, cont.ControllerID); }
                }
            }
            else // Red Square
            {
                switch (point)
                {
                    // Point Offset
                    case CrossPointPosition.Top:
                        //startH += 160 - 15; // 1/2 Unit, 15 = Windowの半分
                        startH += cabiDx / 2 - 15; // 1/2 Unit, 15 = Windowの半分
                        startV += 5; // 30x30 Windowの中心がCrossと同じところにくるように
                        break;
                    case CrossPointPosition.Bottom:
                        //startH += 160 - 15; // 1/2 Unit, 15 = Windowの半分
                        //startV += 360 - 35; // 30x30 Windowの中心がCrossと同じところにくるように
                        startH += cabiDx / 2 - 15; // 1/2 Unit, 15 = Windowの半分
                        startV += cabiDy - 35; // 30x30 Windowの中心がCrossと同じところにくるように
                        break;
                    case CrossPointPosition.Right:
                        //startH += 320 - 35; // 30x30 Windowの中心がCrossと同じところにくるように
                        //startV += 180 - 15; // 1/2 Unit, 15 = Windowの半分
                        startH += cabiDx - 35; // 30x30 Windowの中心がCrossと同じところにくるように
                        startV += cabiDy / 2 - 15; // 1/2 Unit, 15 = Windowの半分
                        break;
                    case CrossPointPosition.Left:
                        startH += 5; // 30x30 Windowの中心がCrossと同じところにくるように
                        //startV += 180 - 15; // 1/2 Unit, 15 = Windowの半分
                        startV += cabiDy / 2 - 15; // 1/2 Unit, 15 = Windowの半分
                        break;
                    case CrossPointPosition.Calib:
                        //startH += 140 - 15; // 15 = Windowの半分
                        //startV += 135 - 15; // 15 = Windowの半分
                        startH += cabiDx / 2 - 30; // 15 = Windowの半分
                        startV += cabiDy / 2 - 30; // 15 = Windowの半分
                        break;
                    case CrossPointPosition.Center:
                        //startH += 160 - 15; // 1/2 Unit, 15 = Windowの半分
                        //startV += 180 - 15; // 1/2 Unit, 15 = Windowの半分
                        startH += cabiDx / 2 - 15; // 1/2 Unit, 15 = Windowの半分
                        startV += cabiDy / 2 - 15; // 1/2 Unit, 15 = Windowの半分
                        break;
                    case CrossPointPosition.TopLeft:
                        startH += 5;
                        startV += 5;
                        break;
                    case CrossPointPosition.TopRight:
                        startH += cabiDx - 35;
                        startV += 5;
                        break;
                    case CrossPointPosition.BottomLeft:
                        startH += 5;
                        startV += cabiDy - 35;
                        break;
                    case CrossPointPosition.BottomRight:
                        startH += cabiDx - 35;
                        startV += cabiDy - 35;
                        break;
                    case CrossPointPosition.RadiatorL_Top:
                        startH += cabiDx / 4 - 15;
                        startV += 5;
                        break;
                    case CrossPointPosition.RadiatorR_Top:
                        startH += cabiDx * 3 / 4 - 15;
                        startV += 5;
                        break;
                    case CrossPointPosition.RadiatorL_Right:
                        startH += cabiDx / 2 - 35;
                        startV += cabiDy / 2 - 15;
                        break;
                    case CrossPointPosition.RadiatorR_Left:
                        startH += cabiDx / 2 + 5;
                        startV += cabiDy / 2 - 15;
                        break;
                    case CrossPointPosition.RadiatorL_Bottom:
                        startH += cabiDx / 4 - 15;
                        startV += cabiDy - 35;
                        break;
                    case CrossPointPosition.RadiatorR_Bottom:
                        startH += cabiDx * 3 / 4 - 15;
                        startV += cabiDy - 35;
                        break;
                    case CrossPointPosition.Module_0:
                        startH += modDx / 2 - 15;
                        startV += modDy / 2 - 15;
                        break;
                    case CrossPointPosition.Module_1:
                        startH += modDx + modDx / 2 - 15;
                        startV += modDy / 2 - 15;
                        break;
                    case CrossPointPosition.Module_2:
                        startH += modDx * 2 + modDx / 2 - 15;
                        startV += modDy / 2 - 15;
                        break;
                    case CrossPointPosition.Module_3:
                        startH += modDx * 3 + modDx / 2 - 15;
                        startV += modDy / 2 - 15;
                        break;
                    case CrossPointPosition.Module_4:
                        startH += modDx / 2 - 15;
                        startV += modDy + modDy / 2 - 15;
                        break;
                    case CrossPointPosition.Module_5:
                        startH += modDx + modDx / 2 - 15;
                        startV += modDy + modDy / 2 - 15;
                        break;
                    case CrossPointPosition.Module_6:
                        startH += modDx * 2 + modDx / 2 - 15;
                        startV += modDy + modDy / 2 - 15;
                        break;
                    case CrossPointPosition.Module_7:
                        startH += modDx * 3 + modDx / 2 - 15;
                        startV += modDy + modDy / 2 - 15;
                        break;
                    case CrossPointPosition.Module_8:
                        startH += modDx / 2 - 15;
                        startV += modDy * 2 + modDy / 2 - 15;
                        break;
                    case CrossPointPosition.Module_9:
                        startH += modDx + modDx / 2 - 15;
                        startV += modDy * 2 + modDy / 2 - 15;
                        break;
                    case CrossPointPosition.Module_10:
                        startH += modDx * 2 + modDx / 2 - 15;
                        startV += modDy * 2 + modDy / 2 - 15;
                        break;
                    case CrossPointPosition.Module_11:
                        startH += modDx * 3 + modDx / 2 - 15;
                        startV += modDy * 2 + modDy / 2 - 15;
                        break;
                    default:
                        throw new Exception("The position is not specified properly.");
                }

                // Pattern Cross
                cmd[21] += 0x09;

                // Foreground Color(Red)
                cmd[22] = (byte)(brightness.UF_20pc >> 8); // Red
                cmd[23] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[24] = 0; // Green
                cmd[25] = 0;
                cmd[26] = 0; // Blue
                cmd[27] = 0;

                // Background Color(White)
                cmd[28] = (byte)(brightness.UF_20pc >> 8); ; // Red
                cmd[29] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[30] = (byte)(brightness.UF_20pc >> 8); ; // Green
                cmd[31] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[32] = (byte)(brightness.UF_20pc >> 8); ; // Blue
                cmd[33] = (byte)(brightness.UF_20pc & 0xFF);

                // Start Position
                cmd[34] = (byte)(startH >> 8);
                cmd[35] = (byte)(startH & 0xFF);
                cmd[36] = (byte)(startV >> 8);
                cmd[37] = (byte)(startV & 0xFF);

                // H, V Width
                cmd[38] = 0x00;
                cmd[39] = 0x1E; // 30
                cmd[40] = 0x00;
                cmd[41] = 0x1E; // 30

                foreach (ControllerInfo cont in dicController.Values)
                {
                    if (cont.ControllerID == unit.ControllerID)
                    { sendSdcpCommand(cmd, 0, cont.IPAddress); }
                    else
                    { outputRaster(brightness._20pc, cont.ControllerID); }
                }
            }
        }

        private void outputCross(UnitInfo unit, int offsetH, int offsetV)
        {
            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            int startH = 0, startV = 0;

            // Unit Offset
            startH = unit.PixelX;
            startV = unit.PixelY;

            if (curType == CursorType.WhiteCross)
            {
                // Point Offset
                startH += offsetH - 1; // -1:線の太さの半分
                startV += offsetV - 1;

                // Pattern Cross
                cmd[21] += 0x03;

                // Foreground Color
                cmd[22] = (byte)(brightness.UF_20pc >> 8); // Red
                cmd[23] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[24] = (byte)(brightness.UF_20pc >> 8); // Green
                cmd[25] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[26] = (byte)(brightness.UF_20pc >> 8); // Blue
                cmd[27] = (byte)(brightness.UF_20pc & 0xFF);

                // Start Position
                cmd[34] = (byte)(startH >> 8);
                cmd[35] = (byte)(startH & 0xFF);
                cmd[36] = (byte)(startV >> 8);
                cmd[37] = (byte)(startV & 0xFF);

                // H, V Width
                cmd[38] = 0x00;
                cmd[39] = 0x02;
                cmd[40] = 0x00;
                cmd[41] = 0x02;

                foreach (ControllerInfo cont in dicController.Values)
                {
                    if (cont.ControllerID == unit.ControllerID)
                    { sendSdcpCommand(cmd, 0, cont.IPAddress); }
                    else
                    { outputRaster(0, cont.ControllerID); }
                }
            }
            else // Red Square
            {
                // Point Offset
                startH += offsetH - 1; // -1:線の太さの半分
                startV += offsetV - 1;

                // Pattern Cross
                cmd[21] += 0x09;

                // Foreground Color(Red)
                cmd[22] = (byte)(brightness.UF_20pc >> 8); // Red
                cmd[23] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[24] = 0; // Green
                cmd[25] = 0;
                cmd[26] = 0; // Blue
                cmd[27] = 0;

                // Background Color(White)
                cmd[28] = (byte)(brightness.UF_20pc >> 8); ; // Red
                cmd[29] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[30] = (byte)(brightness.UF_20pc >> 8); ; // Green
                cmd[31] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[32] = (byte)(brightness.UF_20pc >> 8); ; // Blue
                cmd[33] = (byte)(brightness.UF_20pc & 0xFF);

                // Start Position
                cmd[34] = (byte)(startH >> 8);
                cmd[35] = (byte)(startH & 0xFF);
                cmd[36] = (byte)(startV >> 8);
                cmd[37] = (byte)(startV & 0xFF);

                // H, V Width
                cmd[38] = 0x00;
                cmd[39] = 0x1E; // 30
                cmd[40] = 0x00;
                cmd[41] = 0x1E; // 30

                foreach (ControllerInfo cont in dicController.Values)
                {
                    if (cont.ControllerID == unit.ControllerID)
                    { sendSdcpCommand(cmd, 0, cont.IPAddress); }
                    else
                    { outputRaster(brightness._20pc, cont.ControllerID); }
                }
            }
        }

        private void outputWindow(UnitInfo unit, CrossPointPosition point, CellColor color)
        {
            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            int startH = 0, startV = 0;

            // Unit Offset
            startH = unit.PixelX; // += 320 * (unitCol - 1);
            startV = unit.PixelY; // += 360 * (6 - unitRow);

            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            {
                // Point Offset
                if (point == CrossPointPosition.Top)
                {
                    //startH += 160; // 1/2 Unit
                    startH += cabiDx / 2; // 1/2 Cabinet
                    startV += 20; // 20画素
                }
                else if (point == CrossPointPosition.Bottom)
                {
                    //startH += 160; // 1/2 Unit
                    //startV += 360 - 20; // 20画素
                    startH += cabiDx / 2; // 1/2 Unit
                    startV += cabiDy - 20; // 20画素
                }
                else if (point == CrossPointPosition.Right)
                {
                    //startH += 320 - 20; // 20画素
                    //startV += 180; // 1/2 Unit
                    startH += cabiDx - 20; // 20画素
                    startV += cabiDy / 2; // 1/2 Unit
                }
                else if (point == CrossPointPosition.Left)
                {
                    startH += 20; // 20画素
                                  //startV += 180; // 1/2 Unit
                    startV += cabiDy / 2; // 1/2 Unit
                }
                else if (point == CrossPointPosition.Calib)
                {
                    //startH += 140;
                    //startV += 135;
                    startH += cabiDx / 2 - 20;
                    startV += cabiDy / 2 - 20;
                }
                else if (point == CrossPointPosition.Center)
                {
                    //startH += 160; // 1/2 Unit
                    //startV += 180; // 1/2 Unit
                    startH += cabiDx / 2; // 1/2 Unit
                    startV += cabiDy / 2; // 1/2 Unit
                }
                else if (point == CrossPointPosition.TopLeft)
                {
                    startH += 20;
                    startV += 20;
                }
                else if (point == CrossPointPosition.TopRight)
                {
                    startH += cabiDx - 20;
                    startV += 20;
                }
                else if (point == CrossPointPosition.BottomLeft)
                {
                    startH += 20;
                    startV += cabiDy - 20;
                }
                else if (point == CrossPointPosition.BottomRight)
                {
                    startH += cabiDx - 20;
                    startV += cabiDy - 20;
                }
                else if (point == CrossPointPosition.RadiatorL_Top)
                {
                    startH += cabiDx / 4;
                    startV += 20;
                }
                else if (point == CrossPointPosition.RadiatorR_Top)
                {
                    startH += cabiDx * 3 / 4;
                    startV += 20;
                }
                else if (point == CrossPointPosition.RadiatorL_Right)
                {
                    startH += cabiDx / 2 - 20;
                    startV += cabiDy / 2;
                }
                else if (point == CrossPointPosition.RadiatorR_Left)
                {
                    startH += cabiDx / 2 + 20;
                    startV += cabiDy / 2;
                }
                else if (point == CrossPointPosition.RadiatorL_Bottom)
                {
                    startH += cabiDx / 4;
                    startV += cabiDy - 20;
                }
                else if (point == CrossPointPosition.RadiatorR_Bottom)
                {
                    startH += cabiDx * 3 / 4;
                    startV += cabiDy - 20;
                }
                else if (point == CrossPointPosition.Module_0)
                {
                    startH += modDx / 2;
                    startV += modDy / 2;
                }
                else if (point == CrossPointPosition.Module_1)
                {
                    startH += modDx + modDx / 2;
                    startV += modDy / 2;
                }
                else if (point == CrossPointPosition.Module_2)
                {
                    startH += modDx * 2 + modDx / 2;
                    startV += modDy / 2;
                }
                else if (point == CrossPointPosition.Module_3)
                {
                    startH += modDx * 3 + modDx / 2;
                    startV += modDy / 2;
                }
                else if (point == CrossPointPosition.Module_4)
                {
                    startH += modDx / 2;
                    startV += modDy + modDy / 2;
                }
                else if (point == CrossPointPosition.Module_5)
                {
                    startH += modDx + modDx / 2;
                    startV += modDy + modDy / 2;
                }
                else if (point == CrossPointPosition.Module_6)
                {
                    startH += modDx * 2 + modDx / 2;
                    startV += modDy + modDy / 2;
                }
                else if (point == CrossPointPosition.Module_7)
                {
                    startH += modDx * 3 + modDx / 2;
                    startV += modDy + modDy / 2;
                }
                else if (point == CrossPointPosition.Module_8)
                {
                    startH += modDx / 2;
                    startV += modDy * 2 + modDy / 2;
                }
                else if (point == CrossPointPosition.Module_9)
                {
                    startH += modDx + modDx / 2;
                    startV += modDy * 2 + modDy / 2;
                }
                else if (point == CrossPointPosition.Module_10)
                {
                    startH += modDx * 2 + modDx / 2;
                    startV += modDy * 2 + modDy / 2;
                }
                else if (point == CrossPointPosition.Module_11)
                {
                    startH += modDx * 3 + modDx / 2;
                    startV += modDy * 2 + modDy / 2;
                }
                else { throw new Exception("The position is not specified properly."); }

                startH -= 25;
                if (startH < 0)
                { startH = 0; }

                startV -= 25;
                if (startV < 0)
                { startV = 0; }

                // H, V Width
                cmd[38] = 0x00;
                cmd[39] = 0x32;
                cmd[40] = 0x00;
                cmd[41] = 0x32;
            }
            else // LEDModuleConfigurations.Module_4x3
            {
                // Point Offset
                if (point == CrossPointPosition.Top)
                {
                    startH += modDx;
                    startV += 0;
                    cmd[38] = 0x00;
                    cmd[39] = (byte)(modDx * 2);
                    cmd[40] = 0x00;
                    cmd[41] = (byte)modDy;
                }
                else if (point == CrossPointPosition.Bottom)
                {
                    startH += modDx;
                    startV += modDy * 2;
                    cmd[38] = 0x00;
                    cmd[39] = (byte)(modDx * 2);
                    cmd[40] = 0x00;
                    cmd[41] = (byte)modDy;
                }
                else if (point == CrossPointPosition.Right)
                {
                    startH += modDx * 3;
                    startV += modDy;
                    cmd[38] = 0x00;
                    cmd[39] = (byte)modDx;
                    cmd[40] = 0x00;
                    cmd[41] = (byte)modDy;
                }
                else if (point == CrossPointPosition.Left)
                {
                    startH += 0;
                    startV += modDy;
                    cmd[38] = 0x00;
                    cmd[39] = (byte)modDx;
                    cmd[40] = 0x00;
                    cmd[41] = (byte)modDy;
                }
                else if (point == CrossPointPosition.Calib)
                {
                    startH += modDx;
                    startV += modDy;
                    cmd[38] = 0x00;
                    cmd[39] = (byte)modDx;
                    cmd[40] = 0x00;
                    cmd[41] = (byte)modDy;
                }
                else if (point == CrossPointPosition.Center)
                {
                    startH += modDx;
                    startV += modDy;
                    cmd[38] = 0x00;
                    cmd[39] = (byte)(modDx * 2);
                    cmd[40] = 0x00;
                    cmd[41] = (byte)modDy;
                }
                else if (point == CrossPointPosition.TopLeft)
                {
                    startH += 20;
                    startV += 20;
                    cmd[38] = 0x00;
                    cmd[39] = 0x32;
                    cmd[40] = 0x00;
                    cmd[41] = 0x32;
                }
                else if (point == CrossPointPosition.TopRight)
                {
                    startH += cabiDx - 20;
                    startV += 20;
                    cmd[38] = 0x00;
                    cmd[39] = 0x32;
                    cmd[40] = 0x00;
                    cmd[41] = 0x32;
                }
                else if (point == CrossPointPosition.BottomLeft)
                {
                    startH += 20;
                    startV += cabiDy - 20;
                    cmd[38] = 0x00;
                    cmd[39] = 0x32;
                    cmd[40] = 0x00;
                    cmd[41] = 0x32;
                }
                else if (point == CrossPointPosition.BottomRight)
                {
                    startH += cabiDx - 20;
                    startV += cabiDy - 20;
                    cmd[38] = 0x00;
                    cmd[39] = 0x32;
                    cmd[40] = 0x00;
                    cmd[41] = 0x32;
                }
                else if (point == CrossPointPosition.RadiatorL_Top)
                {
                    startH += cabiDx / 4;
                    startV += 20;
                    cmd[38] = 0x00;
                    cmd[39] = 0x32;
                    cmd[40] = 0x00;
                    cmd[41] = 0x32;
                }
                else if (point == CrossPointPosition.RadiatorR_Top)
                {
                    startH += cabiDx * 3 / 4;
                    startV += 20;
                    cmd[38] = 0x00;
                    cmd[39] = 0x32;
                    cmd[40] = 0x00;
                    cmd[41] = 0x32;
                }
                else if (point == CrossPointPosition.RadiatorL_Right)
                {
                    startH += cabiDx / 2 - 20;
                    startV += cabiDy / 2;
                    cmd[38] = 0x00;
                    cmd[39] = 0x32;
                    cmd[40] = 0x00;
                    cmd[41] = 0x32;
                }
                else if (point == CrossPointPosition.RadiatorR_Left)
                {
                    startH += cabiDx / 2 + 20;
                    startV += cabiDy / 2;
                    cmd[38] = 0x00;
                    cmd[39] = 0x32;
                    cmd[40] = 0x00;
                    cmd[41] = 0x32;
                }
                else if (point == CrossPointPosition.RadiatorL_Bottom)
                {
                    startH += cabiDx / 4;
                    startV += cabiDy - 20;
                    cmd[38] = 0x00;
                    cmd[39] = 0x32;
                    cmd[40] = 0x00;
                    cmd[41] = 0x32;
                }
                else if (point == CrossPointPosition.RadiatorR_Bottom)
                {
                    startH += cabiDx * 3 / 4;
                    startV += cabiDy - 20;
                    cmd[38] = 0x00;
                    cmd[39] = 0x32;
                    cmd[40] = 0x00;
                    cmd[41] = 0x32;
                }
                else if (point == CrossPointPosition.Module_0)
                {
                    startH += 0;
                    startV += 0;
                    cmd[38] = 0x00;
                    cmd[39] = (byte)modDx;
                    cmd[40] = 0x00;
                    cmd[41] = (byte)modDy;
                }
                else if (point == CrossPointPosition.Module_1)
                {
                    startH += modDx;
                    startV += 0;
                    cmd[38] = 0x00;
                    cmd[39] = (byte)modDx;
                    cmd[40] = 0x00;
                    cmd[41] = (byte)modDy;
                }
                else if (point == CrossPointPosition.Module_2)
                {
                    startH += modDx * 2;
                    startV += 0;
                    cmd[38] = 0x00;
                    cmd[39] = (byte)modDx;
                    cmd[40] = 0x00;
                    cmd[41] = (byte)modDy;
                }
                else if (point == CrossPointPosition.Module_3)
                {
                    startH += modDx * 3;
                    startV += 0;
                    cmd[38] = 0x00;
                    cmd[39] = (byte)modDx;
                    cmd[40] = 0x00;
                    cmd[41] = (byte)modDy;
                }
                else if (point == CrossPointPosition.Module_4)
                {
                    startH += 0;
                    startV += modDy;
                    cmd[38] = 0x00;
                    cmd[39] = (byte)modDx;
                    cmd[40] = 0x00;
                    cmd[41] = (byte)modDy;
                }
                else if (point == CrossPointPosition.Module_5)
                {
                    startH += modDx;
                    startV += modDy;
                    cmd[38] = 0x00;
                    cmd[39] = (byte)modDx;
                    cmd[40] = 0x00;
                    cmd[41] = (byte)modDy;
                }
                else if (point == CrossPointPosition.Module_6)
                {
                    startH += modDx * 2;
                    startV += modDy;
                    cmd[38] = 0x00;
                    cmd[39] = (byte)modDx;
                    cmd[40] = 0x00;
                    cmd[41] = (byte)modDy;
                }
                else if (point == CrossPointPosition.Module_7)
                {
                    startH += modDx * 3;
                    startV += modDy;
                    cmd[38] = 0x00;
                    cmd[39] = (byte)modDx;
                    cmd[40] = 0x00;
                    cmd[41] = (byte)modDy;
                }
                else if (point == CrossPointPosition.Module_8)
                {
                    startH += 0;
                    startV += modDy * 2;
                    cmd[38] = 0x00;
                    cmd[39] = (byte)modDx;
                    cmd[40] = 0x00;
                    cmd[41] = (byte)modDy;
                }
                else if (point == CrossPointPosition.Module_9)
                {
                    startH += modDx;
                    startV += modDy * 2;
                    cmd[38] = 0x00;
                    cmd[39] = (byte)modDx;
                    cmd[40] = 0x00;
                    cmd[41] = (byte)modDy;
                }
                else if (point == CrossPointPosition.Module_10)
                {
                    startH += modDx * 2;
                    startV += modDy * 2;
                    cmd[38] = 0x00;
                    cmd[39] = (byte)modDx;
                    cmd[40] = 0x00;
                    cmd[41] = (byte)modDy;
                }
                else if (point == CrossPointPosition.Module_11)
                {
                    startH += modDx * 3;
                    startV += modDy * 2;
                    cmd[38] = 0x00;
                    cmd[39] = (byte)modDx;
                    cmd[40] = 0x00;
                    cmd[41] = (byte)modDy;
                }
                else { throw new Exception("The position is not specified properly."); }
            }

            // Pattern Window
            cmd[21] += 0x09;

            // Foreground Color           
            if (color == CellColor.Red)
            {
                cmd[22] = (byte)(brightness.UF_20pc >> 8); // Red
                cmd[23] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[24] = 0x00;
                cmd[25] = 0x00;
                cmd[26] = 0x00;
                cmd[27] = 0x00;
            }
            else if (color == CellColor.Green)
            {
                cmd[22] = 0x00;
                cmd[23] = 0x00;
                cmd[24] = (byte)(brightness.UF_20pc >> 8); // Green
                cmd[25] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[26] = 0x00;
                cmd[27] = 0x00;
            }
            else if (color == CellColor.Blue)
            {
                cmd[22] = 0x00;
                cmd[23] = 0x00;
                cmd[24] = 0x00;
                cmd[25] = 0x00;
                cmd[26] = (byte)(brightness.UF_20pc >> 8); // Blue
                cmd[27] = (byte)(brightness.UF_20pc & 0xFF);
            }
            else // White
            {
                cmd[22] = (byte)(brightness.UF_20pc >> 8); // Red
                cmd[23] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[24] = (byte)(brightness.UF_20pc >> 8); // Green
                cmd[25] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[26] = (byte)(brightness.UF_20pc >> 8); // Blue
                cmd[27] = (byte)(brightness.UF_20pc & 0xFF);
            }

            // Start Position
            cmd[34] = (byte)(startH >> 8);
            cmd[35] = (byte)(startH & 0xFF);
            cmd[36] = (byte)(startV >> 8);
            cmd[37] = (byte)(startV & 0xFF);

            if (curType == CursorType.RedSquare)
            {
                // Background Color(White)
                cmd[28] = (byte)(brightness.UF_20pc >> 8); ; // Red
                cmd[29] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[30] = (byte)(brightness.UF_20pc >> 8); ; // Green
                cmd[31] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[32] = (byte)(brightness.UF_20pc >> 8); ; // Blue
                cmd[33] = (byte)(brightness.UF_20pc & 0xFF);
            }

            sendSdcpCommand(cmd, 500, dicController[unit.ControllerID].IPAddress);
        }

        private void outputWindow(UnitInfo unit, int offsetH, int offsetV, CellColor color)
        {
            int winSize; // 表示Windowサイズ

            switch (allocInfo.LEDModel)
            {
                case ZRD_C12A:
                case ZRD_B12A:
                case ZRD_CH12D:
                case ZRD_BH12D:
                case ZRD_CH12D_S3:
                case ZRD_BH12D_S3:
                    winSize = 50;
                    break;
                default:
                    winSize = 40;
                    break;
            }

            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            int startH = 0, startV = 0;

            // Unit Offset
            startH = unit.PixelX; // += 320 * (unitCol - 1);
            startV = unit.PixelY; // += 360 * (6 - unitRow);

            // Point Offset
            startH += offsetH;
            startV += offsetV;

            startH -= winSize / 2;
            if (startH < 0)
            { startH = 0; }

            startV -= winSize / 2;
            if (startV < 0)
            { startV = 0; }

            // Pattern Window
            cmd[21] += 0x09;

            // Foreground Color           
            if (color == CellColor.Red)
            {
                cmd[22] = (byte)(brightness.UF_20pc >> 8); // Red
                cmd[23] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[24] = 0x00;
                cmd[25] = 0x00;
                cmd[26] = 0x00;
                cmd[27] = 0x00;
            }
            else if (color == CellColor.Green)
            {
                cmd[22] = 0x00;
                cmd[23] = 0x00;
                cmd[24] = (byte)(brightness.UF_20pc >> 8); // Green
                cmd[25] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[26] = 0x00;
                cmd[27] = 0x00;
            }
            else if (color == CellColor.Blue)
            {
                cmd[22] = 0x00;
                cmd[23] = 0x00;
                cmd[24] = 0x00;
                cmd[25] = 0x00;
                cmd[26] = (byte)(brightness.UF_20pc >> 8); // Blue
                cmd[27] = (byte)(brightness.UF_20pc & 0xFF);
            }
            else // White
            {
                cmd[22] = (byte)(brightness.UF_20pc >> 8); // Red
                cmd[23] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[24] = (byte)(brightness.UF_20pc >> 8); // Green
                cmd[25] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[26] = (byte)(brightness.UF_20pc >> 8); // Blue
                cmd[27] = (byte)(brightness.UF_20pc & 0xFF);
            }

            // Start Position
            cmd[34] = (byte)(startH >> 8);
            cmd[35] = (byte)(startH & 0xFF);
            cmd[36] = (byte)(startV >> 8);
            cmd[37] = (byte)(startV & 0xFF);

            // H, V Width
            cmd[38] = 0x00;
            cmd[39] = (byte)winSize;
            cmd[40] = 0x00;
            cmd[41] = (byte)winSize;

            if (curType == CursorType.RedSquare)
            {
                // Background Color(White)
                cmd[28] = (byte)(brightness.UF_20pc >> 8); ; // Red
                cmd[29] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[30] = (byte)(brightness.UF_20pc >> 8); ; // Green
                cmd[31] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[32] = (byte)(brightness.UF_20pc >> 8); ; // Blue
                cmd[33] = (byte)(brightness.UF_20pc & 0xFF);
            }

            sendSdcpCommand(cmd, 500, dicController[unit.ControllerID].IPAddress);
        }

        private void outputCell(UnitInfo unit, CellColor color, CellNum num)
        {
            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            int startH = 0, startV = 0;

            // Unit Offset
            startH = unit.PixelX; //+= 320 * (unitCol - 1);
            startV = unit.PixelY; //+= 360 * (6 - unitRow);

            // Cell Offset
            startH += modDx * ((int)num % 4);
            startV += modDy * ((int)num / 4);

            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            {
                startH += modDx / 2 - 25;
                startV += modDy / 2 - 25;
            }

            // Pattern Window
            cmd[21] += 0x09;

            // Foreground Color
            if (color == CellColor.Red)
            {
                cmd[22] = (byte)(brightness.UF_20pc >> 8); // Red
                cmd[23] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[24] = 0x00;
                cmd[25] = 0x00;
                cmd[26] = 0x00;
                cmd[27] = 0x00;
            }
            else if (color == CellColor.Green)
            {
                cmd[22] = 0x00;
                cmd[23] = 0x00;
                cmd[24] = (byte)(brightness.UF_20pc >> 8); // Green
                cmd[25] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[26] = 0x00;
                cmd[27] = 0x00;
            }
            else if (color == CellColor.Blue)
            {
                cmd[22] = 0x00;
                cmd[23] = 0x00;
                cmd[24] = 0x00;
                cmd[25] = 0x00;
                cmd[26] = (byte)(brightness.UF_20pc >> 8); // Blue
                cmd[27] = (byte)(brightness.UF_20pc & 0xFF);
            }
            else if (color == CellColor.White)
            {
                cmd[22] = (byte)(brightness.UF_20pc >> 8); // Red
                cmd[23] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[24] = (byte)(brightness.UF_20pc >> 8); // Green
                cmd[25] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[26] = (byte)(brightness.UF_20pc >> 8); // Blue
                cmd[27] = (byte)(brightness.UF_20pc & 0xFF);
            }

            // Start Position
            cmd[34] = (byte)(startH >> 8);
            cmd[35] = (byte)(startH & 0xFF);
            cmd[36] = (byte)(startV >> 8);
            cmd[37] = (byte)(startV & 0xFF);

            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            {
                // H, V Width
                cmd[38] = 0x00;
                cmd[39] = 0x32; //0x50;
                cmd[40] = 0x00;
                cmd[41] = 0x32; //0x78;
            }
            else
            {
                // H, V Width
                cmd[38] = 0x00;
                cmd[39] = (byte)modDx;
                cmd[40] = 0x00;
                cmd[41] = (byte)modDy;
            }

            if (curType == CursorType.RedSquare)
            {
                // Background Color(White)
                cmd[28] = (byte)(brightness.UF_20pc >> 8); ; // Red
                cmd[29] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[30] = (byte)(brightness.UF_20pc >> 8); ; // Green
                cmd[31] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[32] = (byte)(brightness.UF_20pc >> 8); ; // Blue
                cmd[33] = (byte)(brightness.UF_20pc & 0xFF);
            }

            if (curType == CursorType.WhiteCross)
            { sendSdcpCommand(cmd, 500, dicController[unit.ControllerID].IPAddress); }
            else
            {
                foreach (ControllerInfo cont in dicController.Values)
                {
                    if (cont.ControllerID == unit.ControllerID)
                    { sendSdcpCommand(cmd, 500, cont.IPAddress); }
                    else
                    { outputRaster(brightness.UF_20pc, cont.ControllerID); }
                }
            }
        }

        #endregion Signal

        private bool copyColorData(double[][][] pointData, out double[][][] measureData)
        {
            double[,] centerEst = new double[3, 3]; // [rgb, XYZ]
            //double[,] centerEst_V = new double[3, 3];

            // 初期化
            measureData = new double[12][][];
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            {
                for (int cell = 0; cell < 12; cell++)
                {
                    measureData[cell] = new double[3][];
                    for (int rgb = 0; rgb < 3; rgb++)
                    { measureData[cell][rgb] = new double[3]; }
                }
            }
            else
            {
                for (int cell = 0; cell < 12; cell++)
                {
                    measureData[cell] = new double[4][];
                    for (int rgbw = 0; rgbw < 4; rgbw++)
                    { measureData[cell][rgbw] = new double[3]; }
                }
            }

            for (int i = 0; i < pointData.Length; i++)
            {
                if (pointData[i] == null)
                {
                    // Red
                    measureData[i][0][0] = -1;
                    measureData[i][0][1] = -1;
                    measureData[i][0][2] = -1;

                    // Green
                    measureData[i][1][0] = -1;
                    measureData[i][1][1] = -1;
                    measureData[i][1][2] = -1;

                    // Blue
                    measureData[i][2][0] = -1;
                    measureData[i][2][1] = -1;
                    measureData[i][2][2] = -1;

                    // White
                    if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
                    {
                        measureData[i][3][0] = -1;
                        measureData[i][3][1] = -1;
                        measureData[i][3][2] = -1;
                    }

                    continue;
                }

                // Red
                measureData[i][0][0] = pointData[i][0][0];
                measureData[i][0][1] = pointData[i][0][1];
                measureData[i][0][2] = pointData[i][0][2];

                // Green
                measureData[i][1][0] = pointData[i][1][0];
                measureData[i][1][1] = pointData[i][1][1];
                measureData[i][1][2] = pointData[i][1][2];

                // Blue
                measureData[i][2][0] = pointData[i][2][0];
                measureData[i][2][1] = pointData[i][2][1];
                measureData[i][2][2] = pointData[i][2][2];

                // White
                if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
                {
                    measureData[i][3][0] = pointData[i][3][0];
                    measureData[i][3][1] = pointData[i][3][1];
                    measureData[i][3][2] = pointData[i][3][2];
                }
            }

            return true;
        }

        private string makeFilePath(UnitInfo unit, FileDirectory dir, DataType type = DataType.HcData)
        {
            string fileName;

            if (type == DataType.UnitData && dir != FileDirectory.Backup_Rbin)
            { fileName = "u" + unit.PortNo.ToString("D2") + unit.UnitNo.ToString("D2") + "_ud.bin"; }
            else if (type == DataType.UnitData && dir == FileDirectory.Backup_Rbin)
            { fileName = "u" + unit.PortNo.ToString("D2") + unit.UnitNo.ToString("D2") + "_ud.rbin"; }            
            else if (type == DataType.HcData)
            { fileName = "u" + unit.PortNo.ToString("D2") + unit.UnitNo.ToString("D2") + "_hc.bin"; }
            else if (type == DataType.LcData)
            { fileName = "u" + unit.PortNo.ToString("D2") + unit.UnitNo.ToString("D2") + "_lc.bin"; }
            else if(type == DataType.AgingData)
            { fileName = "u" + unit.PortNo.ToString("D2") + unit.UnitNo.ToString("D2") + "_ad.bin"; }
            else if (type == DataType.ExtendedData)
            { fileName = "u" + unit.PortNo.ToString("D2") + unit.UnitNo.ToString("D2") + "_ed.bin"; }
            else if (type == DataType.HfData)
            { fileName = "u" + unit.PortNo.ToString("D2") + unit.UnitNo.ToString("D2") + "_hf.rbin"; }
            else
            { return string.Empty; }

            // Model名取得
            string model = dicController[unit.ControllerID].ModelName;
            if (string.IsNullOrWhiteSpace(model) == true)
            {
                model = getModelName(dicController[unit.ControllerID].IPAddress);
                dicController[unit.ControllerID].ModelName = model;
            }

            // Serial No.取得
            string serial = dicController[unit.ControllerID].SerialNo;
            if (string.IsNullOrWhiteSpace(serial) == true)
            {
                serial = getSerialNo(dicController[unit.ControllerID].IPAddress);
                dicController[unit.ControllerID].SerialNo = serial;
            }

            string dirName = "";
            if (dir == FileDirectory.Backup_Initial || dir == FileDirectory.Backup_Previous || dir == FileDirectory.Backup_Latest || dir == FileDirectory.Backup_Rbin)
            { dirName = "Backup"; }
            else if (dir == FileDirectory.Temp)
            //{ dirName = "AdjustedData"; }
            { dirName = "Backup"; } // 調整済みデータはBackupフォルダのTempフォルダに格納するように変更
            else if(dir == FileDirectory.CellReplace)
            { dirName = "CellReplace"; }

            string filePath = applicationPath + "\\" + dirName + "\\" + model + "_" + serial;

            if (dir == FileDirectory.Backup_Initial)
            { filePath += "_Initial"; }
            else if(dir == FileDirectory.Backup_Previous)
            { filePath += "_Previous"; }
            else if(dir == FileDirectory.Backup_Latest)
            { filePath += "_Latest"; }
            else if (dir == FileDirectory.Backup_Rbin)
            { filePath += "_Rbin"; }
            else if(dir == FileDirectory.Temp)
            { filePath += "_Temp"; }

            filePath += "\\" + fileName;

            return filePath;
        }
        
        private void changeCompleteColor(List<UnitInfo> lstUnit)
        {
            foreach (UnitInfo unit in lstUnit)
            {
                Dispatcher.Invoke(new Action(() => { aryUnitUf[unit.X - 1, unit.Y - 1].Background = new SolidColorBrush(Colors.Aqua); }));
            }
        }

        private bool isContainedDragArea(UnitToggleButton utb, DragArea area, double offsetX, double offsetY)
        {
            // UnitToggleButtonの一部がArea内に含まれているかどうか
            // 左上
            if (utb.Margin.Left + offsetX >= area.StartX && utb.Margin.Left + offsetX <= area.EndX
                && utb.Margin.Top + offsetY >= area.StartY && utb.Margin.Top + offsetY <= area.EndY)
            { return true; }

            // 右上
            if (utb.Margin.Left + utb.Width + offsetX >= area.StartX && utb.Margin.Left + utb.Width + offsetX <= area.EndX
                && utb.Margin.Top + offsetY >= area.StartY && utb.Margin.Top + offsetY <= area.EndY)
            { return true; }

            // 左下
            if (utb.Margin.Left + offsetX >= area.StartX && utb.Margin.Left + offsetX <= area.EndX
                && utb.Margin.Top + utb.Height + offsetY >= area.StartY && utb.Margin.Top + utb.Height + offsetY <= area.EndY)
            { return true; }

            // 右下
            if (utb.Margin.Left + utb.Width + offsetX >= area.StartX && utb.Margin.Left + utb.Width + offsetX <= area.EndX
                && utb.Margin.Top + utb.Height + offsetY >= area.StartY && utb.Margin.Top + utb.Height + offsetY <= area.EndY)
            { return true; }
            
            // Areaが１つのUnitToggleButton内に含まれる場合
            if (area.StartX >= utb.Margin.Left + offsetX && area.EndX <= utb.Margin.Left + utb.Width + offsetX
                && area.StartY >= utb.Margin.Top + offsetY && area.EndY <= utb.Margin.Top + utb.Height + offsetY)
            { return false; }

            // AreaがUnitToggleButtonに含まれているかどうか
            // X方向
            if (area.StartY >= utb.Margin.Top + offsetY && area.EndY <= utb.Margin.Top + utb.Height + offsetY
                && area.StartX >= utb.Margin.Left + offsetX && area.StartX <= utb.Margin.Left + utb.Width + offsetX)
            { return true; }

            if (area.StartY >= utb.Margin.Top + offsetY && area.EndY <= utb.Margin.Top + utb.Height + offsetY
                && area.StartX <= utb.Margin.Left + offsetX && area.EndX >= utb.Margin.Left + utb.Width + offsetX)
            { return true; }

            if (area.StartY >= utb.Margin.Top + offsetY && area.EndY <= utb.Margin.Top + utb.Height + offsetY
                && area.EndX >= utb.Margin.Left + offsetX && area.EndX <= utb.Margin.Left + utb.Width + offsetX)
            { return true; }

            // Y方向
            if (area.StartX >= utb.Margin.Left + offsetX && area.EndX <= utb.Margin.Left + utb.Width + offsetX
                && area.StartY >= utb.Margin.Top + offsetY && area.StartY <= utb.Margin.Top + utb.Height + offsetY)
            { return true; }

            if (area.StartX >= utb.Margin.Left + offsetX && area.EndX <= utb.Margin.Left + utb.Width + offsetX
                && area.StartY <= utb.Margin.Top + offsetY && area.EndY >= utb.Margin.Top + utb.Height + offsetY)
            { return true; }

            if (area.StartX >= utb.Margin.Left + offsetX && area.EndX <= utb.Margin.Left + utb.Width + offsetX
                && area.EndY >= utb.Margin.Top + offsetY && area.EndY <= utb.Margin.Top + utb.Height + offsetY)
            { return true; }

            return false;
        }

        private void setChromTarget()
        {
            switch (Settings.Ins.ConfigChromType)
            {
                case ConfigChrom.Custom:
                ufTargetChrom = new ChromCustom(ColorPurpose.ConfigCustom);
                    break;
                case ConfigChrom.ZRD_BH15D:
                ufTargetChrom = new ChromCustom(ColorPurpose.ZRD_BH15D);
                    break;
                case ConfigChrom.ZRD_BH12D:
                ufTargetChrom = new ChromCustom(ColorPurpose.ZRD_BH12D);
                    break;
                case ConfigChrom.ZRD_CH15D:
                ufTargetChrom = new ChromCustom(ColorPurpose.ZRD_CH15D);
                    break;
                case ConfigChrom.ZRD_CH12D:
                ufTargetChrom = new ChromCustom(ColorPurpose.ZRD_CH12D);
                    break;
                case ConfigChrom.ZRD_BH15D_S3:
                    ufTargetChrom = new ChromCustom(ColorPurpose.ZRD_BH15D_S3);
                    break;
                case ConfigChrom.ZRD_BH12D_S3:
                    ufTargetChrom = new ChromCustom(ColorPurpose.ZRD_BH12D_S3);
                    break;
                case ConfigChrom.ZRD_CH15D_S3:
                    ufTargetChrom = new ChromCustom(ColorPurpose.ZRD_CH15D_S3);
                    break;
                case ConfigChrom.ZRD_CH12D_S3:
                    ufTargetChrom = new ChromCustom(ColorPurpose.ZRD_CH12D_S3);
                    break;
                case ConfigChrom.ZRD_B15A:
                ufTargetChrom = new ChromCustom(ColorPurpose.ZRD_B15A);
                    break;
                case ConfigChrom.ZRD_B12A:
                ufTargetChrom = new ChromCustom(ColorPurpose.ZRD_B12A);
                    break;
                case ConfigChrom.ZRD_C15A:
                ufTargetChrom = new ChromCustom(ColorPurpose.ZRD_C15A);
                    break;
                case ConfigChrom.ZRD_C12A:
                ufTargetChrom = new ChromCustom(ColorPurpose.ZRD_C12A);
                    break;
                default:
                    break;
            }
        }

        private bool isCurrentValueCaSdkTwo()
        {
            string currentValue = getRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Classes\CLSID\{006B0650-AF9A-4EE1-B18F-B5740004D7CE}\InprocServer32\");
            return currentValue == null ? false : currentValue.Contains("CA-SDK2");
        }

        private string getRegistryValue(string path)
        {
            string key = System.IO.Path.GetDirectoryName(path);
            string valuename = System.IO.Path.GetFileName(path);
            return Registry.GetValue(key, valuename, null)?.ToString();
        }

        private int calcAdjustUfDataMoveSec(int cabinetCount)
        {
            int dataMoveSec;

            //係数　データ移動時間
            double h1 = 0.16;

            dataMoveSec = (int)(cabinetCount * h1);

            return dataMoveSec;
        }

        private int calcAdjustUfResponseSec(int maxConnectCabinet)
        {
            int responseSec;

            //係数　レスポンス時間 ファイルの種類ごとに定義
            double h2 = 5.27;
            double h3 = 0.85;

            //補正係数　キャビネット台数によって増加する1ファイルの処理時間を補正
            double supplementNum = 0.018;

            //係数　ファイルの種類と無関係なレスポンス時間
            double t3 = 21.40;

            responseSec = (int)(maxConnectCabinet * (h2 + maxConnectCabinet * supplementNum) + h3 + t3);

            return responseSec;
        }
        private int calcAdjustUfProcessSec(int step, int controllerCount, int dataMoveSec, int responseSec)
        {
            int processSec = 0;
            //係数　データ移動,レスポンス処理,コントローラー台数に影響する時間を除いた合計処理時間
            //          [11] Move Adjusted Files.
            //commonA[0] [12] Cabinet Power Off.
            //commonA[1] [13] Send Reconfig.
            //commonA[2] [14] Send Write Command.
            //          [15] Waiting for the process of controller.
            //commonA[3] [16] Set Normal Setting.
            //commonA[4] [17] Restore User Settings.
            //commonA[5] [18] Move Latest to Previous.
            //commonA[6] [19] Move Temp to Latest.
            //commonA[7] [20] Send Reconfig.
            //commonA[8] [21] Delete Temp Files.
            //commonA[9] [22] Unselect All Cabinets.
            //commonA[10] [23] FTP Off.
            int[] commonA = new int[11] { 5, 16, 1, 1, 2, 0, 0, 14, 0, 0, 1};

            //係数　コントローラー台数に影響のある各種処理時間
            //          [11] Move Adjusted Files.
            //contB[0] [12] Cabinet Power Off.
            //contB[1] [13] Send Reconfig.
            //contB[2] [14] Send Write Command.
            //          [15] Waiting for the process of controller.
            //contB[3] [16] Set Normal Setting.
            //contB[4] [17] Restore User Settings.
            //contB[5] [18] Move Latest to Previous.
            //contB[6] [19] Move Temp to Latest.
            //contB[7] [20] Send Reconfig.
            //contB[8] [21] Delete Temp Files.
            //contB[9] [22] Unselect All Cabinets.
            //contB[10] [23] FTP Off.
            int[] contB = new int[11] { 2, 2, 1, 1, 2, 0, 0, 2, 0, 0, 1};

            switch (step)
            {
                case 11: //[11] Move Adjusted Files. 以下の処理時間全体を計算
                    processSec = commonA.Skip(step - 11).Sum() + controllerCount * contB.Skip(step - 11).Sum()
                            + responseSec + dataMoveSec;
                    break;
                case 12: //[12] Cabinet Power Off.
                case 13: //[13] Send Reconfig.
                case 14: //[14] Send Write Command.
                case 15: //[15] Waiting for the process of controller.
                    processSec = commonA.Skip(step - 11 -  1).Sum() + controllerCount * contB.Skip(step - 11 -  1).Sum()
                            + responseSec;
                    break;
                case 16: //[16] Set Normal Setting.
                case 17: //[17] Restore User Settings.
                case 18: //[18] Move Latest to Previous.
                case 19: //[19] Move Temp to Latest.
                case 20: //[20] Send Reconfig.
                case 21: //[21] Delete Temp Files.
                case 22: //[22] Unselect All Cabinets.
                case 23: //[23] FTP Off.
                    processSec = commonA.Skip(step - 11 -  2).Sum() + controllerCount * contB.Skip(step - 11 -  2).Sum();
                    break;
                default:
                    break;
            }

            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("calcAdjustUfProcessSec step:" + step + " controllerCount:" + controllerCount + " responseSec:" + responseSec + " dataMoveSec:" + dataMoveSec + " processSec:" + processSec); }
            return processSec;
        }
        private int calcAdjustUfUnitProcessSec(int step)
        {
            int processSec = 0;
            //係数　データ移動,レスポンス処理,コントローラー台数に影響する時間を除いた合計処理時間
            //commonA[0] [10] Set Target Color.
            //commonA[1] [11] Extract Correction Data.
            //commonA[2] [12] Calc XYZ.
            //commonA[3] [13] Calc New XYZ Data.
            //commonA[4] [14] Make New Correction Data.
            //commonA[5] [15] Save New Module Data.
            //commonA[6] [16] Move Adjusted Files.
            //commonA[7] [17] Cabinet Power Off.
            //commonA[8] [18] Send Reconfig.
            //commonA[9] [19] Send Write Command.
            //commonA[10] [20] Waiting for the process of controller.
            //commonA[11] [21] Set Normal Setting.
            //commonA[12] [22] Restore User Settings.
            //commonA[13] [23] Copy Latest to Previous.
            //commonA[14] [24] Copy Temp to Latest.
            //commonA[15] [25] Send Reconfig.
            //commonA[16] [26] Delete Temporary Files.
            //commonA[17] [27] FTP Off.
            int[] commonA = new int[18] { 0, 0, 0, 0, 0, 0, 1, 5, 15, 1, 38, 1, 2, 0, 0, 14, 0, 1};

            processSec = commonA.Skip(step - 13).Sum();

            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("calcAdjustUfUnitProcessSec step:" + step + " processSec:" + processSec); }

            return processSec;
        }
        #endregion Private Methods
    }
}
