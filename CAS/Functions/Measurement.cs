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
using System.Runtime.Serialization;
using System.Xml;

using SONY.Modules;
using CAS.Windows;
using ClosedXML.Excel;

namespace CAS
{
    // Measurement
    public partial class MainWindow : Window
    {
        #region Fields

        //private bool measTabFirstSelect = true;

        #endregion Fields

        #region Events
        
        private void cmbxCA410ChannelMeas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // CA-410 Channel
            changCAChannel(cmbxCA410ChannelMeas.SelectedIndex);

            cmbxCA410Channel.SelectedIndex = cmbxCA410ChannelMeas.SelectedIndex;
            //cmbxCA410ChannelCalib.SelectedIndex = cmbxCA410ChannelMeas.SelectedIndex;
            cmbxCA410ChannelRelTgt.SelectedIndex = cmbxCA410ChannelMeas.SelectedIndex;

            if (appliMode == ApplicationMode.Service && Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
            { cmbxCA410ChannelGain.SelectedIndex = cmbxCA410ChannelMeas.SelectedIndex; }
        }
        
        //private void cmbxColorTempMeasurement_DropDownClosed(object sender, EventArgs e)
        //{
        //    //byte[] cmd = new byte[SDCPClass.CmdColorTempSet.Length];
        //    //Array.Copy(SDCPClass.CmdColorTempSet, cmd, SDCPClass.CmdColorTempSet.Length);

        //    //if (((ComboBox)sender).SelectedIndex == 0)
        //    //{ cmd[20] = 0x00; }
        //    //else if (((ComboBox)sender).SelectedIndex == 1)
        //    //{ cmd[20] = 0x01; }
        //    //else if (((ComboBox)sender).SelectedIndex == 2)
        //    //{ cmd[20] = 0x02; }
        //    //else
        //    //{ return; }

        //    //foreach (ControllerInfo controller in dicController.Values)
        //    //{ sendSdcpCommand(cmd, controller.IPAddress); }

        //    string cmd = "", res;

        //    if (((ComboBox)sender).SelectedIndex == 0)
        //    { cmd = "color_temp \"d93\""; }
        //    else if (((ComboBox)sender).SelectedIndex == 1)
        //    { cmd = "color_temp \"d65\""; }
        //    else if (((ComboBox)sender).SelectedIndex == 2)
        //    { cmd = "color_temp \"d50\""; }
        //    else
        //    { return; }

        //    foreach (ControllerInfo controller in dicController.Values)
        //    { sendAdcpCommand(cmd, out res, controller.IPAddress); }
        //}

        private async void btnMeasurementStart_Click(object sender, RoutedEventArgs e)
        {
            bool status = false;            
            var dispatcher = Application.Current.Dispatcher;
            string msg = "";
            string caption = "";

            tcMain.IsEnabled = false;
            actionButton(sender, "Measurement Start.");

            winProgress = new WindowProgress("Measurement Progress");
            winProgress.ShowMessage("Measurement Start.");
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

            // Set Target Chromaticity / Show Confirm Window
            setChromTarget();

            m_lstUserSetting = null;

            try { status = await Task.Run(() => measurement()); }
            catch (Exception ex)
            { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

            if (status == true)
            {
                msg = "Measurement Complete!";
                caption = "Complete";
            }
            else
            {
                setNormalSettingMeasure();
                setUserSettingMeasure(m_lstUserSetting);
                m_lstUserSetting = null;

                msg = "Failed in Measurement.";
                caption = "Error";
            }

            winProgress.Close();

            playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
            WindowMessage winMessage = new WindowMessage(msg, caption);
            winMessage.ShowDialog();

            releaseButton(sender, "Measurement Done.");
            tcMain.IsEnabled = true;
        }

        // MeasurementのDrag指定用
        private void utbMeas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            rectAreaMeas.Visibility = System.Windows.Visibility.Visible;
            rectAreaMeas.Margin = new Thickness(e.GetPosition(this).X - 2, e.GetPosition(this).Y - 48, 0, 0);

            startPos = new Point(rectAreaMeas.Margin.Left, rectAreaMeas.Margin.Top);
        }

        private void utbMeas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
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

            double offsetX = tcMain.Margin.Left + gdMeasurement.Margin.Left + gdAllocMeasurement.Margin.Left + gdAllocMeasurLayout.Margin.Left + cabinetAllocColumnHeaderWidth - svMeasurement.HorizontalOffset;
            double offsetY = tcMain.Margin.Top + gdMeasurement.Margin.Top + gdAllocMeasurement.Margin.Top + gdAllocMeasurLayout.Margin.Top + cabinetAllocRowHeaderHeight - svMeasurement.VerticalOffset;

            for (int y = 0; y < allocInfo.MaxY; y++)
            {
                for (int x = 0; x < allocInfo.MaxX; x++)
                {
                    if (aryUnitMeas[x, y].IsEnabled == false)
                    { continue; }

                    if (isContainedDragArea(aryUnitMeas[x, y], area, offsetX, offsetY) == true)
                    {
                        if (aryUnitMeas[x, y].IsChecked == true)
                        { aryUnitMeas[x, y].IsChecked = false; }
                        else
                        { aryUnitMeas[x, y].IsChecked = true; }
                    }
                }
            }

            rectAreaMeas.Visibility = System.Windows.Visibility.Hidden;
        }

        private void utbMeas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            double height = e.GetPosition(this).Y - 48 - startPos.Y;
            try
            {
                if (height >= 0)
                { rectAreaMeas.Height = height; }
                else
                {
                    rectAreaMeas.Margin = new Thickness(rectAreaMeas.Margin.Left, startPos.Y + height, 0, 0);
                    rectAreaMeas.Height = Math.Abs(height);
                }
            }
            catch { rectAreaMeas.Height = 0; }

            double width = e.GetPosition(this).X - 2 - startPos.X;
            try
            {
                if (width >= 0)
                { rectAreaMeas.Width = width; }
                else
                {
                    rectAreaMeas.Margin = new Thickness(startPos.X + width, rectAreaMeas.Margin.Top, 0, 0);
                    rectAreaMeas.Width = Math.Abs(width);
                }
            }
            catch { rectAreaMeas.Width = 0; }
        }

        #endregion Events

        #region Private Methods

        //private void getColorTemp()
        //{
        //    foreach (ControllerInfo cont in dicController.Values)
        //    {
        //        if (cont.Master == true)
        //        {
        //            string res;
        //            //sendSdcpCommand(SDCPClass.CmdColorTempGet, out buff, cont.IPAddress);

        //            //if(buff == "00") //D93
        //            //{ cmbxColorTempMeasurement.SelectedIndex = 0; }
        //            //else if (buff == "01") // D65
        //            //{ cmbxColorTempMeasurement.SelectedIndex = 1; }
        //            //else if (buff == "02") // D50
        //            //{ cmbxColorTempMeasurement.SelectedIndex = 2; }
        //            sendAdcpCommand("color_temp ?", out res, cont.IPAddress);

        //            if (res.Contains("d93") == true) //D93
        //            { cmbxColorTempMeasurement.SelectedIndex = 0; }
        //            else if (res.Contains("d65") == true) // D65
        //            { cmbxColorTempMeasurement.SelectedIndex = 1; }
        //            else if (res.Contains("d50") == true) // D50
        //            { cmbxColorTempMeasurement.SelectedIndex = 2; }
        //        }
        //    }
        //}

        private bool measurement()
        {
            if (Settings.Ins.ExecLog == true)
            {
                SaveExecLog("");
                SaveExecLog("[0] measurement start");
            }

            var dispatcher = Application.Current.Dispatcher;
            
            List<UnitInfo> lstTargetUnit = new List<UnitInfo>();
            List<UserSetting> lstUserSetting;
            bool status;
            string site = "";
            //ColorTemp colorTemp = ColorTemp.D93;
            MeasurementMode mode = MeasurementMode.UnitUniformity;

            // ●進捗の最大値を設定
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[1] Set Step Count."); }
            winProgress.ShowMessage("Set Step Count.");

            int step = 12 + 8 * dicController.Count;
            winProgress.SetWholeSteps(step);

            // ●調整をするUnitをListに格納 [1]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[1] Store Target Cabinet(s) Info."); }
            winProgress.ShowMessage("Store Target Cabinet(s) Info.");

            dispatcher.Invoke(() => correctMeasureUnit(out lstTargetUnit, allocInfo.MaxX, allocInfo.MaxY));
            if (lstTargetUnit.Count == 0)
            {
                ShowMessageWindow("Please select cabinets.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

#if NO_SET
#else
            // ●Power OnでないときはOnにする [2]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[2] Check Controller Power."); }
            winProgress.ShowMessage("Check Controller Power.");

            if (!setAllControllerPowerOn())
            { return false; }
            winProgress.PutForward1Step();
#endif

            // ●User設定保存 [3]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[3] Store User Settings."); }
            winProgress.ShowMessage("Store User Settings.");

            try
            {
                if (getUserSettingMeasure(out lstUserSetting) != true)
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

            // ●調整用設定 [4]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[4] Set Adjust Settings."); }
            winProgress.ShowMessage("Set Adjust Settings.");

            setAdjustSettingMeasure();
            winProgress.PutForward1Step();

            // ●Color Temp設定取得 [5]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[5] Get Color Temp. Setting."); }
            winProgress.ShowMessage("Get Color Temp. Setting.");

            dispatcher.Invoke(() => getMeasureSetting(out site,/* out colorTemp,*/ out mode));
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

                //// ●Set Color Temp [*2]
                //winProgress.ShowMessage("Set Color Temp.");
                ////byte[] cmd = new byte[SDCPClass.CmdColorTempSet.Length];
                ////Array.Copy(SDCPClass.CmdColorTempSet, cmd, SDCPClass.CmdColorTempSet.Length);

                ////if (colorTemp == ColorTemp.D93)
                ////{ cmd[20] = 0x00; }
                ////else if (colorTemp == ColorTemp.D65)
                ////{ cmd[20] = 0x01; }
                ////else if (colorTemp == ColorTemp.D50)
                ////{ cmd[20] = 0x02; }

                ////sendSdcpCommand(cmd, controller.IPAddress);

                //string cmd = "", res;

                //if (colorTemp == ColorTemp.D93)
                //{ cmd = "color_temp \"d93\""; }
                //else if (colorTemp == ColorTemp.D65)
                //{ cmd = "color_temp \"d65\""; }
                //else if (colorTemp == ColorTemp.D50)
                //{ cmd = "color_temp \"d50\""; }

                //sendAdcpCommand(cmd, out res, controller.IPAddress);
                //if (res != "ok")
                //{
                //    ShowMessageWindow("ADCP command was not accepted.\r\nPlease check Controller status.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                //    return false;
                //}

                // ●Set Through Mode On [*2]
                winProgress.ShowMessage("Set Through Mode On.");

                System.Threading.Thread.Sleep(1000);
                if (sendSdcpCommand(SDCPClass.CmdThroughModeOn, controller.IPAddress) != true)
                { return false; }

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

            if (setCA410SettingDispatch(CaSdk.DisplayMode.DispModeLvxy) != true)
            {
                ShowMessageWindow("Can not set CA-410 settings.\r\nPlease check it status.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();
#endif

            // ●Layout情報Off
            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdDispUnitAddrOff, 0, cont.IPAddress); }

            // Measurement Modeで分岐
            if (appliMode == ApplicationMode.Developer)
            {
                bool cabi = false, mod = false, rad = false, _72pt = false;

                Dispatcher.Invoke(() => cabi = (bool)cbMeasCabinet.IsChecked);
                Dispatcher.Invoke(() => mod = (bool)cbMeasModule.IsChecked);
                Dispatcher.Invoke(() => rad = (bool)cbMeasRadiator.IsChecked);
                Dispatcher.Invoke(() => _72pt = (bool)cbMeas72pt.IsChecked);

                if (measureChironCustom(site, lstTargetUnit, cabi, mod, rad, _72pt) != true)
                { return false; }
            }
            else
            {
                if (mode == MeasurementMode.UnitUniformity)
                {
                    if (measureUnitUniformity(site, /*colorTemp,*/ lstTargetUnit) != true)
                    { return false; }
                }
                else
                {
                    if (measureCellUniformity(site, /*colorTemp,*/ lstTargetUnit) != true)
                    { return false; }
                }
            }

            // ●Set Through Mode Off
            winProgress.ShowMessage("Set Through Mode Off.");
            setNormalSettingMeasure();

            // ●User設定に戻す [12]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[12] Restore User Settings."); }
            winProgress.ShowMessage("Restore User Settings.");

            setUserSettingMeasure(m_lstUserSetting);
            m_lstUserSetting = null;    //User設定を戻したので呼び出し側の設定処理をスキップ

            winProgress.PutForward1Step();

            return true;
        }

        private void getMeasureSetting(out string site, /*out ColorTemp colorTemp,*/ out MeasurementMode mode)
        {
            // Site
            site = txbSite.Text;

            //// ColorTemp
            //if (cmbxColorTempMeasurement.SelectedIndex == 1)
            //{ colorTemp = ColorTemp.D65; }
            //else if (cmbxColorTempMeasurement.SelectedIndex == 2)
            //{ colorTemp = ColorTemp.D50; }
            //else
            //{ colorTemp = ColorTemp.D93; }

            // Measurement Mode
            if (rbMeasurementModeUnit.IsChecked == true)
            { mode = MeasurementMode.UnitUniformity; }
            else
            { mode = MeasurementMode.CellUniformity; }

            return;
        }

        private void correctMeasureUnit(out List<UnitInfo> lstUnitList, int MaxX, int MaxY)
        {
            string error;
            List<UnitInfo> lstUnitTemp = new List<UnitInfo>();

            lstUnitList = new List<UnitInfo>();
            
            #region Size_1
            for (int x = 0; x <= MaxX - 1; x += 2)
            {
                for (int y = 0; y < MaxY; y++)
                {
                    try
                    {
                        if (aryUnitMeas[x, y].UnitInfo != null && aryUnitMeas[x, y].IsChecked == true)
                        { lstUnitList.Add(aryUnitMeas[x, y].UnitInfo); }
                    }
                    catch
                    {
                        error = "[correctMeasureUnit] (Case:1) x = " + x.ToString() + ", y = " + y.ToString()
                            + ", MaxX = " + MaxX.ToString() + ", MaxY = " + MaxY.ToString();
                        SaveErrorLog(error);
                        saveSelectionInfo();
                    }
                }

                if (x + 1 >= MaxX)
                { continue; }
                
                for (int y = MaxY - 1; y >= 0; y--)
                {
                    try
                    {
                        if (aryUnitMeas[x + 1, y].UnitInfo != null && aryUnitMeas[x + 1, y].IsChecked == true)
                        { lstUnitList.Add(aryUnitMeas[x + 1, y].UnitInfo); }
                    }
                    catch
                    {
                        error = "[correctMeasureCabinet] (Case:2) x = " + x.ToString() + ", y = " + y.ToString()
                            + ", MaxX = " + MaxX.ToString() + ", MaxY = " + MaxY.ToString();
                        SaveErrorLog(error);
                        saveSelectionInfo();
                    }
                }
            }            
            #endregion
        }

        #region Settings

        private bool getUserSettingMeasure(out List<UserSetting> lstUserSetting)
        {
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[0] getUserSetting start ( Controller Count : " + dicController.Count.ToString() + " )"); }

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

                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t\tLow Brightness Mode : " + user.LowBrightnessMode); }
                winProgress.PutForward1Step();

                //// Color Space
                //if (Settings.Ins.ExecLog == true)
                //{ SaveExecLog("\t[*3] Color Space"); }

                //System.Threading.Thread.Sleep(1000);
                //if (sendSdcpCommand(SDCPClass.CmdColorSpaceGet, out buff, controller.IPAddress) != true)
                //{ return false; }
                //try { user.ColorSpace = Convert.ToInt32(buff, 16); }
                //catch (Exception ex)
                //{
                //    string errStr = "[getUserSetting(Color Space)] Source : " + ex.Source + "\r\nException Messsage : " + ex.Message + "\r\nBuff = " + buff.ToString();
                //    SaveErrorLog(errStr);
                //    ShowMessageWindow(errStr, "Exception! (Color Space)", System.Drawing.SystemIcons.Error, 500, 210);
                //    return false;
                //}

                //if (Settings.Ins.ExecLog == true)
                //{ SaveExecLog("\t\tColor Space : " + user.ColorSpace); }
                //winProgress.PutForward1Step();

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

        private bool setAdjustSettingMeasure()
        {
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[0] setAdjustSettingMeasure start ( Controller Count : " + dicController.Count.ToString() + " )"); }

            foreach (ControllerInfo cont in dicController.Values)
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[*1] (Controller Loop) ControllerID : " + cont.ControllerID.ToString()); }

                // Low Brightness Mode
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[*2] Low Brightness Mode"); }

                System.Threading.Thread.Sleep(1000);
                if (sendSdcpCommand(SDCPClass.CmdLowBrightModeSet, cont.IPAddress) != true)
                { return false; }
                winProgress.PutForward1Step();

                //// Color Space
                //if (Settings.Ins.ExecLog == true)
                //{ SaveExecLog("\t[*3] Color Space"); }

                //System.Threading.Thread.Sleep(1000);
                //if (sendSdcpCommand(SDCPClass.CmdColorSpaceSet, cont.IPAddress) != true)
                //{ return false; }
                //winProgress.PutForward1Step();
            }

            return true;
        }

        private void setUserSettingMeasure(List<UserSetting> lstUserSetting)
        {
            if (lstUserSetting == null)
            { return; }

            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[0] setUserSettingMeasure start ( Controller Count : " + dicController.Count.ToString() + " )"); }

            foreach (UserSetting usr in lstUserSetting)
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[*1] (Controller Loop) ControllerID : " + usr.ControllerID.ToString()); }

                // Low Brightness Mode
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t[*2] Low Brightness Mode : " + usr.LowBrightnessMode); }

                Byte[] cmd = new byte[SDCPClass.CmdLowBrightModeSet.Length];
                Array.Copy(SDCPClass.CmdLowBrightModeSet, cmd, SDCPClass.CmdLowBrightModeSet.Length);

                cmd[20] = (byte)usr.LowBrightnessMode;

                System.Threading.Thread.Sleep(1000);
                sendSdcpCommand(cmd, dicController[usr.ControllerID].IPAddress);
                winProgress.PutForward1Step();

                //// Color Space
                //if (Settings.Ins.ExecLog == true)
                //{ SaveExecLog("\t[*3] Color Space : " + usr.ColorSpace); }

                //cmd = new byte[SDCPClass.CmdColorSpaceSet.Length];
                //Array.Copy(SDCPClass.CmdColorSpaceSet, cmd, SDCPClass.CmdColorSpaceSet.Length);

                //cmd[20] = (byte)usr.ColorSpace;

                //System.Threading.Thread.Sleep(1000);
                //sendSdcpCommand(cmd, dicController[usr.ControllerID].IPAddress);
                //winProgress.PutForward1Step();
            }
        }

        #endregion Settings

        private bool measureUnitUniformity(string site, /*ColorTemp colorTemp,*/ List<UnitInfo> lstTargetUnit)
        {
            bool status;
            int measuredCount = 0;

            List<UnitColor> lstUnitColor = new List<UnitColor>();

            HeaderInfo header = new HeaderInfo();
            header.Date = DateTime.Now;
            header.Model = site;
            //header.ColorTemp = colorTemp;

            // ●Unit測定
#if NO_MEASURE
            string path = @"C:\CAS\Measurement\20170327_DEBUG_D93_UnitUniformity.xml";
            loadMeasuredData(path, out lstUnitColor);
#else
            foreach (UnitInfo unit in lstTargetUnit)
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*1] (Cabinet Loop) PortNo. : " + unit.PortNo.ToString() + ", CabinetNo. : " + unit.UnitNo.ToString()); }
                
                // ●色度測定 [*1]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*2] Measure Cabinet Color."); }
                winProgress.ShowMessage("Measure Cabinet Color." + "    (Cabinets: " + (++measuredCount) + "/" + lstTargetUnit.Count + ")");

                UnitColor unitColor;
                status = measure5Points(unit, out unitColor);
                if(status != true)
                { return false; }

                lstUnitColor.Add(unitColor);
            }
            winProgress.PutForward1Step();

            // ●TestPattern OFF [8]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[8] Test Pattern Off."); }
            winProgress.ShowMessage("Test Pattern Off.");

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdIntSignalOff, 0, cont.IPAddress); }

            // ●測定生データをファイルに保存 [9]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[9] Save Raw Data."); }
            winProgress.ShowMessage("Save Raw Data.");
            saveMeasuredData(header, MeasurementMode.UnitUniformity, lstUnitColor);
#endif

            // ●結果ファイル作成 [10]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[10] Make Result Data."); }
            winProgress.ShowMessage("Make Result Data.");

            string filePath;
            makeResultFileUnit(header, lstUnitColor, out filePath);

            // ●Windowに表示 [11]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[12] Show Result."); }
            winProgress.ShowMessage("Show Result.");

            //showResultWindowUnit(header, filePath, lstUnitColor.Count);
            System.Diagnostics.Process.Start(filePath);

            return true;
        }

        private bool measureCellUniformity(string site, /*ColorTemp colorTemp,*/ List<UnitInfo> lstTargetUnit)
        {
            bool status;
            int measuredCount = 0;

            List<UnitColor> lstUnitColor = new List<UnitColor>();

            HeaderInfo header = new HeaderInfo();
            header.Date = DateTime.Now;
            header.Model = site;
            //header.ColorTemp = colorTemp;

            // ●Unit測定
#if NO_MEASURE
            string path = @"C:\CAS\Measurement\20170328_DEBUG_D93_CellUniformity.xml";
            loadMeasuredData(path, out lstUnitColor);
#else
            foreach (UnitInfo unit in lstTargetUnit)
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*1] (Cabinet Loop) PortNo. : " + unit.PortNo.ToString() + ", CabinetNo. : " + unit.UnitNo.ToString()); }

                // ●色度測定 [*1]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*2] Measure Cabinet Color."); }
                winProgress.ShowMessage("Measure Cabinet Color." + "    (Cabinets: " + (++measuredCount) + "/" + lstTargetUnit.Count + ")");

                UnitColor unitColor;
                status = measureAllCells(unit, out unitColor);
                if (status != true)
                { return false; }

                lstUnitColor.Add(unitColor);
            }
            winProgress.PutForward1Step();

            // ●TestPattern OFF [8]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[8] Test Pattern Off."); }
            winProgress.ShowMessage("Test Pattern Off.");

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdIntSignalOff, 0, cont.IPAddress); }

            // ●測定生データをファイルに保存 [9]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[9] Save Raw Data."); }
            winProgress.ShowMessage("Save Raw Data.");
            saveMeasuredData(header, MeasurementMode.CellUniformity, lstUnitColor);
#endif

            // ●結果ファイル作成 [10]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[10] Make Result Data."); }
            winProgress.ShowMessage("Make Result Data.");

            string filePath;
            makeResultFileCell(header, lstUnitColor, out filePath);

            // ●Windowに表示 [11]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[11] Show Result."); }
            winProgress.ShowMessage("Show Result.");

            System.Diagnostics.Process.Start(filePath);

            return true;
        }
        
        private bool measureChironCustom(string site, List<UnitInfo> lstTargetUnit, bool cabinet, bool module, bool radiator, bool _72pt)
        {
            bool status;
            int measuredCount = 0;

            List<UnitColorMultiPos> lstUnitColor = new List<UnitColorMultiPos>();

            HeaderInfo header = new HeaderInfo();
            header.Date = DateTime.Now;
            header.Model = site;

            // ●Unit測定
            foreach (UnitInfo unit in lstTargetUnit)
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*1] (Cabinet Loop) PortNo. : " + unit.PortNo.ToString() + ", CabinetNo. : " + unit.UnitNo.ToString()); }

                // ●色度測定 [*1]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*2] Measure Cabinet Color."); }
                winProgress.ShowMessage("Measure Cabinet Color." + "    (Cabinets: " + (++measuredCount) + "/" + lstTargetUnit.Count + ")");

                UnitColorMultiPos unitColor;
                status = measureMultiPoints(unit, out unitColor, true, cabinet, module, radiator, _72pt, false);
                //status = measureMultiPoints(unit, true, false, out unitColor);
                if (status != true)
                { return false; }

                lstUnitColor.Add(unitColor);
            }
            winProgress.PutForward1Step();

            // ●TestPattern OFF [8]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[8] Test Pattern Off."); }
            winProgress.ShowMessage("Test Pattern Off.");

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdIntSignalOff, 0, cont.IPAddress); }

            // ●結果ファイル作成 [10]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[10] Make Result Data."); }
            winProgress.ShowMessage("Make Result Data.");

            string filePath = tempPath + @"\Measurement_" + DateTime.Now.ToString("yyyyMMddHHmm") + ".xml";
            UnitColorMultiPos.SaveToXmlFile(filePath, lstUnitColor);

            return true;
        }

        #region Measure

        private bool measure5Points(UnitInfo unit, out UnitColor unitColor)
        {
            bool status;
            List<Chromaticity> lstColor;

            unitColor = new UnitColor();
            unitColor.UnitName = "C" + unit.ControllerID + "-" + unit.PortNo + "-" + unit.UnitNo;

            // Top
            status = measurePoint(unit, CrossPointPosition.Top, out lstColor);
            if (status != true)
            { return false; }

            unitColor.Top = lstColor[0];

            // Right
            status = measurePoint(unit, CrossPointPosition.Right, out lstColor);
            if (status != true)
            { return false; }
            
            unitColor.Right = lstColor[0];

            // Bottom
            status = measurePoint(unit, CrossPointPosition.Bottom, out lstColor);
            if (status != true)
            { return false; }

            unitColor.Bottom = lstColor[0];

            // Left
            status = measurePoint(unit, CrossPointPosition.Left, out lstColor);
            if (status != true)
            { return false; }
            
            unitColor.Left = lstColor[0];

            // Center
            status = measurePoint(unit, CrossPointPosition.Center, out lstColor);
            if (status != true)
            { return false; }

            unitColor.lstCenter = lstColor;

            return true;
        }

        private bool measureMultiPoints(UnitInfo unit, out UnitColorMultiPos unitColor, bool singleMeasure, bool cabinet, bool module, bool radiator, bool _72pt, bool _9pt)
        {
            bool status;
            ChromWRGB chrom;

            unitColor = new UnitColorMultiPos();
            unitColor.UnitName = "C" + unit.ControllerID + "-" + unit.PortNo + "-" + unit.UnitNo;

            if (cabinet == true)
            {
                // Top
                status = measurePoint(unit, CrossPointPosition.Top, out chrom, 0, singleMeasure);
                if (status != true)
                { return false; }

                unitColor.lstChrom.Add(chrom);

                // Left
                status = measurePoint(unit, CrossPointPosition.Left, out chrom, 0, singleMeasure);
                if (status != true)
                { return false; }

                unitColor.lstChrom.Add(chrom);

                // Right
                status = measurePoint(unit, CrossPointPosition.Right, out chrom, 0, singleMeasure);
                if (status != true)
                { return false; }

                unitColor.lstChrom.Add(chrom);

                // Bottom
                status = measurePoint(unit, CrossPointPosition.Bottom, out chrom, 0, singleMeasure);
                if (status != true)
                { return false; }

                unitColor.lstChrom.Add(chrom);
            }

            if (module == true)
            {
                // Module
                for (int i = 0; i < moduleCount; i++)
                {
                    int pos = 18;
                    status = measurePoint(unit, (CrossPointPosition)Enum.ToObject(typeof(CrossPointPosition), pos + i), out chrom, 0, singleMeasure);
                    if (status != true)
                    { return false; }

                    unitColor.lstChrom.Add(chrom);
                }
            }

            if (radiator == true)
            {
                // RadiatorL - Top
                status = measurePoint(unit, CrossPointPosition.RadiatorL_Top, out chrom, 0, singleMeasure);
                if (status != true)
                { return false; }

                unitColor.lstChrom.Add(chrom);

                // RadiatorR - Top
                status = measurePoint(unit, CrossPointPosition.RadiatorR_Top, out chrom, 0, singleMeasure);
                if (status != true)
                { return false; }

                unitColor.lstChrom.Add(chrom);

                // RadiatorL - Left
                status = measurePoint(unit, CrossPointPosition.Left, out chrom, 0, singleMeasure);
                if (status != true)
                { return false; }

                unitColor.lstChrom.Add(chrom);

                // RadiatorL - Right
                status = measurePoint(unit, CrossPointPosition.RadiatorL_Right, out chrom, 0, singleMeasure);
                if (status != true)
                { return false; }

                unitColor.lstChrom.Add(chrom);

                // RadiatorR - Left
                status = measurePoint(unit, CrossPointPosition.RadiatorR_Left, out chrom, 0, singleMeasure);
                if (status != true)
                { return false; }

                unitColor.lstChrom.Add(chrom);

                // RadiatorR - Right
                status = measurePoint(unit, CrossPointPosition.Right, out chrom, 0, singleMeasure);
                if (status != true)
                { return false; }

                unitColor.lstChrom.Add(chrom);

                // RadiatorL - Bottom
                status = measurePoint(unit, CrossPointPosition.RadiatorL_Bottom, out chrom, 0, singleMeasure);
                if (status != true)
                { return false; }

                unitColor.lstChrom.Add(chrom);

                // RadiatorR - Bottom
                status = measurePoint(unit, CrossPointPosition.RadiatorR_Bottom, out chrom, 0, singleMeasure);
                if (status != true)
                { return false; }

                unitColor.lstChrom.Add(chrom);
            }

            if (_72pt == true) // 1Moduleを3×3に分割、Chironだと1Cabi12×6ポジ
            {
                int pitchH = cabiDx / 12;
                int pitchV = cabiDy / 6;

                for (int yp = 0; yp < 6; yp++)
                {
                    for (int xp = 0; xp < 12; xp++)
                    {
                        status = measurePoint(unit, pitchH * xp, pitchV * yp, out chrom, 0, singleMeasure);
                        if (status != true)
                        { return false; }

                        unitColor.lstChrom.Add(chrom);
                    }
                }
            }

            if (_9pt == true)
            {
                // Top-Left
                status = measurePoint(unit, CrossPointPosition.TopLeft, out chrom, 0, singleMeasure);
                if (status != true)
                { return false; }

                unitColor.lstChrom.Add(chrom);

                // Top
                status = measurePoint(unit, CrossPointPosition.Top, out chrom, 0, singleMeasure);
                if (status != true)
                { return false; }

                unitColor.lstChrom.Add(chrom);

                // Top-Right
                status = measurePoint(unit, CrossPointPosition.TopRight, out chrom, 0, singleMeasure);
                if (status != true)
                { return false; }

                unitColor.lstChrom.Add(chrom);

                // Left
                status = measurePoint(unit, CrossPointPosition.Left, out chrom, 0, singleMeasure);
                if (status != true)
                { return false; }

                unitColor.lstChrom.Add(chrom);

                // Center
                status = measurePoint(unit, CrossPointPosition.Center, out chrom, 0, singleMeasure);
                if (status != true)
                { return false; }

                unitColor.lstChrom.Add(chrom);

                // Right
                status = measurePoint(unit, CrossPointPosition.Right, out chrom, 0, singleMeasure);
                if (status != true)
                { return false; }

                unitColor.lstChrom.Add(chrom);

                // Bottom-Left
                status = measurePoint(unit, CrossPointPosition.BottomLeft, out chrom, 0, singleMeasure);
                if (status != true)
                { return false; }

                unitColor.lstChrom.Add(chrom);

                // Bottom
                status = measurePoint(unit, CrossPointPosition.Bottom, out chrom, 0, singleMeasure);
                if (status != true)
                { return false; }

                unitColor.lstChrom.Add(chrom);

                // Bottom-Right
                status = measurePoint(unit, CrossPointPosition.BottomRight, out chrom, 0, singleMeasure);
                if (status != true)
                { return false; }

                unitColor.lstChrom.Add(chrom);
            }

            return true;
        }

        private bool measurePoint(UnitInfo unit, CrossPointPosition point, out List<Chromaticity> lstColor)
        {
            bool status;
            double[] measureData;
            Chromaticity tempColor;
            UserOperation operation = UserOperation.None;

            lstColor = new List<Chromaticity>();

            // 測定ポイントの十字を表示
            outputCross(unit, point);

            // 測定画面表示
            status = Dispatcher.Invoke(() => showMeasureWindow());
            if (status != true)
            { return false; }

            if (point == CrossPointPosition.Center)
            {
                // Red
                outputWindow(unit, point, CellColor.Red);
                System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait); // 画が表示されるまでWait

                status = measureColor(ufTargetChrom.Red, out measureData, CaSdk.DisplayMode.DispModeLvxy, ref operation);
                if (status != true)
                { return false; }

                tempColor = new Chromaticity();
                tempColor.x = measureData[0];
                tempColor.y = measureData[1];
                tempColor.Lv = measureData[2];
                lstColor.Add(tempColor);

                playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

                // Green
                outputWindow(unit, point, CellColor.Green);
                System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

                status = measureColor(ufTargetChrom.Green, out measureData, CaSdk.DisplayMode.DispModeLvxy, ref operation);
                if (status != true)
                { return false; }

                tempColor = new Chromaticity();
                tempColor.x = measureData[0];
                tempColor.y = measureData[1];
                tempColor.Lv = measureData[2];
                lstColor.Add(tempColor);

                playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

                // Blue
                outputWindow(unit, point, CellColor.Blue);
                System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

                status = measureColor(ufTargetChrom.Blue, out measureData, CaSdk.DisplayMode.DispModeLvxy, ref operation);
                if (status != true)
                { return false; }

                tempColor = new Chromaticity();
                tempColor.x = measureData[0];
                tempColor.y = measureData[1];
                tempColor.Lv = measureData[2];
                lstColor.Add(tempColor);

                playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");
            }

            // White
            outputWindow(unit, point, CellColor.White);
            System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

            status = measureColor(ufTargetChrom.White, out measureData, CaSdk.DisplayMode.DispModeLvxy, ref operation);
            if (status != true)
            { return false; }

            tempColor = new Chromaticity();
            tempColor.x = measureData[0];
            tempColor.y = measureData[1];
            tempColor.Lv = measureData[2];
            lstColor.Add(tempColor);

            playSound(applicationPath + "\\Components\\Sound\\button01b.mp3");

            return true;
        }
        
        private bool measureAllCells(UnitInfo unit, out UnitColor unitColor)
        {
            bool status;
            List<Chromaticity> lstColor = new List<Chromaticity>();
            UserOperation operation = UserOperation.None;

            unitColor = new UnitColor();
            unitColor.UnitName = "C" + unit.ControllerID + "-" + unit.PortNo + "-" + unit.UnitNo;

            // Cell分ループ
            for (int i = 0; i < moduleCount; i++)
            {
                double[] data;
                Chromaticity color = new Chromaticity();

                // Center
                outputCross(unit, i, CrossPointPosition.Center);
                status = Dispatcher.Invoke(() => showMeasureWindow());
                if (status != true)
                { return false; }

                // White
                outputCell(unit, CellColor.White, (CellNum)i);
                System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

                status = measureColor(ufTargetChrom.White, out data, CaSdk.DisplayMode.DispModeLvxy, ref operation);
                if (status == true)
                {
                    color.x = data[0];
                    color.y = data[1];
                    color.Lv = data[2];
                }
                else { return false; }

                lstColor.Add(color);

                playSound(applicationPath + "\\Components\\Sound\\button01b.mp3");
            }

            unitColor.lstCenter = lstColor;

            return true;
        }

        private bool measurePoint(UnitInfo unit, CrossPointPosition point, out ChromWRGB chrom, int wait, bool singleMeasure = false)
        {
            bool status;
            double[] measureData;
            Chromaticity tempColor, prevColor = new Chromaticity();
            UserOperation operation = UserOperation.None;

            chrom = new ChromWRGB();
            chrom.Position = point.ToString();

            // 測定ポイントの十字を表示
            outputCross(unit, point);

            // 測定画面表示
            status = Dispatcher.Invoke(() => showMeasureWindow());
            if (status != true)
            { return false; }

            // Red
            outputWindow(unit, point, CellColor.Red);
            System.Threading.Thread.Sleep(wait); // 画が表示されるまでWait

            if (singleMeasure == true)
            {
                status = measureColor(ufTargetChrom.Red, out measureData, CaSdk.DisplayMode.DispModeLvxy, ref operation);
                if (status != true)
                { return false; }
            }
            else
            {
                int count = 0;
                while (true)
                {
                    status = measureColor(ufTargetChrom.Red, out measureData, CaSdk.DisplayMode.DispModeLvxy, ref operation);
                    if (status != true)
                    { return false; }

                    status = checkChrom(ufTargetChrom.Red, prevColor, measureData);
                    if (status == true)
                    { break; }

                    prevColor.x = measureData[0];
                    prevColor.y = measureData[1];
                    prevColor.Lv = measureData[2];

                    if (count > 15)
                    { return false; }

                    System.Threading.Thread.Sleep(200);

                    count++;
                }
            }

            tempColor = new Chromaticity();
            tempColor.x = measureData[0];
            tempColor.y = measureData[1];
            tempColor.Lv = measureData[2];
            chrom.Red = tempColor;

            playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

            // Green
            outputWindow(unit, point, CellColor.Green);
            System.Threading.Thread.Sleep(wait); // 画が表示されるまでWait

            if (singleMeasure == true)
            {
                status = measureColor(ufTargetChrom.Green, out measureData, CaSdk.DisplayMode.DispModeLvxy, ref operation);
                if (status != true)
                { return false; }
            }
            else
            {
                int count = 0;
                while (true)
                {
                    status = measureColor(ufTargetChrom.Green, out measureData, CaSdk.DisplayMode.DispModeLvxy, ref operation);
                    if (status != true)
                    { return false; }

                    status = checkChrom(ufTargetChrom.Green, prevColor, measureData);
                    if (status == true)
                    { break; }

                    prevColor.x = measureData[0];
                    prevColor.y = measureData[1];
                    prevColor.Lv = measureData[2];

                    if (count > 15)
                    { return false; }

                    System.Threading.Thread.Sleep(200);

                    count++;
                }
            }

            tempColor = new Chromaticity();
            tempColor.x = measureData[0];
            tempColor.y = measureData[1];
            tempColor.Lv = measureData[2];
            chrom.Green = tempColor;

            playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

            // Blue
            outputWindow(unit, point, CellColor.Blue);
            System.Threading.Thread.Sleep(wait); // 画が表示されるまでWait

            if (singleMeasure == true)
            {
                status = measureColor(ufTargetChrom.Blue, out measureData, CaSdk.DisplayMode.DispModeLvxy, ref operation);
                if (status != true)
                { return false; }
            }
            else
            {
                int count = 0;
                while (true)
                {
                    status = measureColor(ufTargetChrom.Blue, out measureData, CaSdk.DisplayMode.DispModeLvxy, ref operation);
                    if (status != true)
                    { return false; }

                    status = checkChrom(ufTargetChrom.Blue, prevColor, measureData);
                    if (status == true)
                    { break; }

                    prevColor.x = measureData[0];
                    prevColor.y = measureData[1];
                    prevColor.Lv = measureData[2];

                    if (count > 15)
                    { return false; }

                    System.Threading.Thread.Sleep(200);

                    count++;
                }
            }

            tempColor = new Chromaticity();
            tempColor.x = measureData[0];
            tempColor.y = measureData[1];
            tempColor.Lv = measureData[2];
            chrom.Blue = tempColor;

            playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

            // White
            outputWindow(unit, point, CellColor.White);
            System.Threading.Thread.Sleep(wait); // 画が表示されるまでWait

            if (singleMeasure == true)
            {
                status = measureColor(ufTargetChrom.White, out measureData, CaSdk.DisplayMode.DispModeLvxy, ref operation);
                if (status != true)
                { return false; }
            }
            else
            {
                int count  = 0;
                while (true)
                {
                    status = measureColor(ufTargetChrom.White, out measureData, CaSdk.DisplayMode.DispModeLvxy, ref operation);
                    if (status != true)
                    { return false; }

                    status = checkChrom(ufTargetChrom.White, prevColor, measureData);
                    if (status == true)
                    { break; }

                    prevColor.x = measureData[0];
                    prevColor.y = measureData[1];
                    prevColor.Lv = measureData[2];

                    if (count > 15)
                    { return false; }

                    System.Threading.Thread.Sleep(200);

                    count++;
                }
            }

            tempColor = new Chromaticity();
            tempColor.x = measureData[0];
            tempColor.y = measureData[1];
            tempColor.Lv = measureData[2];
            chrom.White = tempColor;

            playSound(applicationPath + "\\Components\\Sound\\button01b.mp3");

            return true;
        }

        private bool measurePoint(UnitInfo unit, int offsetH, int offsetV, out ChromWRGB chrom, int wait, bool singleMeasure = false)
        {
            bool status;
            double[] measureData;
            Chromaticity tempColor, prevColor = new Chromaticity();
            UserOperation operation = UserOperation.None;

            chrom = new ChromWRGB();
            chrom.Position = "H=" + offsetH + " V=" + offsetV;

            // 測定ポイントの十字を表示
            outputCross(unit, modDx / 6 + offsetH, modDy / 6 + offsetV);

            // 測定画面表示
            status = Dispatcher.Invoke(() => showMeasureWindow());
            if (status != true)
            { return false; }

            // Red
            outputWindow(unit, modDx / 6 + offsetH, modDy / 6 + offsetV, CellColor.Red);
            System.Threading.Thread.Sleep(wait); // 画が表示されるまでWait

            if (singleMeasure == true)
            {
                status = measureColor(ufTargetChrom.Red, out measureData, CaSdk.DisplayMode.DispModeLvxy, ref operation);
                if (status != true)
                { return false; }
            }
            else
            {
                int count = 0;
                while (true)
                {
                    status = measureColor(ufTargetChrom.Red, out measureData, CaSdk.DisplayMode.DispModeLvxy, ref operation);
                    if (status != true)
                    { return false; }

                    status = checkChrom(ufTargetChrom.Red, prevColor, measureData);
                    if (status == true)
                    { break; }

                    prevColor.x = measureData[0];
                    prevColor.y = measureData[1];
                    prevColor.Lv = measureData[2];

                    if (count > 15)
                    { return false; }

                    System.Threading.Thread.Sleep(30);

                    count++;
                }
            }

            tempColor = new Chromaticity();
            tempColor.x = measureData[0];
            tempColor.y = measureData[1];
            tempColor.Lv = measureData[2];
            chrom.Red = tempColor;

            playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

            // Green
            outputWindow(unit, modDx / 6 + offsetH, modDy / 6 + offsetV, CellColor.Green);
            System.Threading.Thread.Sleep(wait); // 画が表示されるまでWait

            if (singleMeasure == true)
            {
                status = measureColor(ufTargetChrom.Green, out measureData, CaSdk.DisplayMode.DispModeLvxy, ref operation);
                if (status != true)
                { return false; }
            }
            else
            {
                int count = 0;
                while (true)
                {
                    status = measureColor(ufTargetChrom.Green, out measureData, CaSdk.DisplayMode.DispModeLvxy, ref operation);
                    if (status != true)
                    { return false; }

                    status = checkChrom(ufTargetChrom.Green, prevColor, measureData);
                    if (status == true)
                    { break; }

                    prevColor.x = measureData[0];
                    prevColor.y = measureData[1];
                    prevColor.Lv = measureData[2];

                    if (count > 15)
                    { return false; }

                    System.Threading.Thread.Sleep(30);

                    count++;
                }
            }

            tempColor = new Chromaticity();
            tempColor.x = measureData[0];
            tempColor.y = measureData[1];
            tempColor.Lv = measureData[2];
            chrom.Green = tempColor;

            playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

            // Blue
            outputWindow(unit, modDx / 6 + offsetH, modDy / 6 + offsetV, CellColor.Blue);
            System.Threading.Thread.Sleep(wait); // 画が表示されるまでWait

            if (singleMeasure == true)
            {
                status = measureColor(ufTargetChrom.Blue, out measureData, CaSdk.DisplayMode.DispModeLvxy, ref operation);
                if (status != true)
                { return false; }
            }
            else
            {
                int count = 0;
                while (true)
                {
                    status = measureColor(ufTargetChrom.Blue, out measureData, CaSdk.DisplayMode.DispModeLvxy, ref operation);
                    if (status != true)
                    { return false; }

                    status = checkChrom(ufTargetChrom.Blue, prevColor, measureData);
                    if (status == true)
                    { break; }

                    prevColor.x = measureData[0];
                    prevColor.y = measureData[1];
                    prevColor.Lv = measureData[2];

                    if (count > 15)
                    { return false; }

                    System.Threading.Thread.Sleep(30);

                    count++;
                }
            }

            tempColor = new Chromaticity();
            tempColor.x = measureData[0];
            tempColor.y = measureData[1];
            tempColor.Lv = measureData[2];
            chrom.Blue = tempColor;

            playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

            // White
            outputWindow(unit, modDx / 6 + offsetH, modDy / 6 + offsetV, CellColor.White);
            System.Threading.Thread.Sleep(wait); // 画が表示されるまでWait

            if (singleMeasure == true)
            {
                status = measureColor(ufTargetChrom.White, out measureData, CaSdk.DisplayMode.DispModeLvxy, ref operation);
                if (status != true)
                { return false; }
            }
            else
            {
                int count = 0;
                while (true)
                {
                    status = measureColor(ufTargetChrom.White, out measureData, CaSdk.DisplayMode.DispModeLvxy, ref operation);
                    if (status != true)
                    { return false; }

                    status = checkChrom(ufTargetChrom.White, prevColor, measureData);
                    if (status == true)
                    { break; }

                    prevColor.x = measureData[0];
                    prevColor.y = measureData[1];
                    prevColor.Lv = measureData[2];

                    if (count > 15)
                    { return false; }

                    System.Threading.Thread.Sleep(30);

                    count++;
                }
            }

            tempColor = new Chromaticity();
            tempColor.x = measureData[0];
            tempColor.y = measureData[1];
            tempColor.Lv = measureData[2];
            chrom.White = tempColor;

            playSound(applicationPath + "\\Components\\Sound\\button01b.mp3");

            return true;
        }

        private bool checkChrom(Chromaticity target, Chromaticity prev, double[] data)
        {
            if (data[0] > target.x * 0.8 && data[0] < target.x * 1.2
                && data[1] > target.y * 0.8 && data[1] < target.y * 1.2
                && data[0] > prev.x * 0.95 && data[0] < prev.x * 1.05
                && data[1] > prev.y * 0.95 && data[1] < prev.y * 1.05)
            { return true; }

            return false;
        }

        #endregion Measure

        private bool makeResultFileUnit(HeaderInfo header, List<UnitColor> lstUnitColor, out string filePath)
        {
            string sourceFile = applicationPath + "\\Components\\Template_Cabinet.xlsx";
            filePath = applicationPath + "\\Measurement\\" + header.Date.ToString("yyyyMMdd") + "_" + header.Model + /*"_" + header.ColorTemp.ToString() +*/ "_UnitUniformity.xlsx";

            try
            {
                if(System.IO.File.Exists(filePath) == true)
                { filePath = makeNewFilePath(filePath); }

                System.IO.File.Copy(sourceFile, filePath);
            }
            catch (Exception ex)
            { throw new Exception("Failed to copy the template file.\r\n(" + ex.Message + ")"); }

            using (var workbook = new XLWorkbook(filePath))
            {
	            var worksheet = workbook.Worksheet(1);
	
	            int row = 2;
	            foreach (UnitColor color in lstUnitColor)
	            {
	                // Cabinet Name
	                worksheet.Cell(row, 1).Value = color.UnitName;
	
	                // W1
	                worksheet.Cell(row, 2).Value = color.Top.Lv;
	                worksheet.Cell(row, 3).Value = color.Top.x;
	                worksheet.Cell(row, 4).Value = color.Top.y;
	
	                // W2
	                worksheet.Cell(row, 5).Value = color.Right.Lv;
	                worksheet.Cell(row, 6).Value = color.Right.x;
	                worksheet.Cell(row, 7).Value = color.Right.y;
	
	                // W3
	                worksheet.Cell(row, 8).Value = color.Bottom.Lv;
	                worksheet.Cell(row, 9).Value = color.Bottom.x;
	                worksheet.Cell(row, 10).Value = color.Bottom.y;
	
	                // W4
	                worksheet.Cell(row, 11).Value = color.Left.Lv;
	                worksheet.Cell(row, 12).Value = color.Left.x;
	                worksheet.Cell(row, 13).Value = color.Left.y;
	
	                // R5
	                worksheet.Cell(row, 14).Value = color.lstCenter[0].Lv;
	                worksheet.Cell(row, 15).Value = color.lstCenter[0].x;
	                worksheet.Cell(row, 16).Value = color.lstCenter[0].y;
	
	                // G5
	                worksheet.Cell(row, 17).Value = color.lstCenter[1].Lv;
	                worksheet.Cell(row, 18).Value = color.lstCenter[1].x;
	                worksheet.Cell(row, 19).Value = color.lstCenter[1].y;
	
	                // B5
	                worksheet.Cell(row, 20).Value = color.lstCenter[2].Lv;
	                worksheet.Cell(row, 21).Value = color.lstCenter[2].x;
	                worksheet.Cell(row, 22).Value = color.lstCenter[2].y;
	
	                // W5
	                worksheet.Cell(row, 23).Value = color.lstCenter[3].Lv;
	                worksheet.Cell(row, 24).Value = color.lstCenter[3].x;
	                worksheet.Cell(row, 25).Value = color.lstCenter[3].y;
	
	                row++;
	            }

	            // 余白をクリア
	            worksheet = workbook.Worksheet(2);
                for (int i = row + 5; i <= 205; i++)
	            {
	                for (int j = 44; j <= 51; j++)
	                { worksheet.Cell(i, j).Value = ""; }
	            }

	            // Header情報書き込み
	            worksheet = workbook.Worksheet(3);
	
	            worksheet.Cell(2, 2).Value = "Date : " + header.Date.ToString("MMM/dd/yyyy", new System.Globalization.CultureInfo("en-US"));
	            worksheet.Cell(3, 2).Value = "Model : " + header.Model;

                switch (Settings.Ins.ConfigChromType)
                {

                    case ConfigChrom.ZRD_C12A:
                        worksheet.Cell(18, 5).Value = Settings.Ins.ZRDC12ATarget.White.Lv;
                        worksheet.Cell(19, 5).Value = Settings.Ins.ZRDC12ATarget.Red.x;
                        worksheet.Cell(20, 5).Value = Settings.Ins.ZRDC12ATarget.Red.y;
                        worksheet.Cell(21, 5).Value = Settings.Ins.ZRDC12ATarget.Green.x;
                        worksheet.Cell(22, 5).Value = Settings.Ins.ZRDC12ATarget.Green.y;
                        worksheet.Cell(23, 5).Value = Settings.Ins.ZRDC12ATarget.Blue.x;
                        worksheet.Cell(24, 5).Value = Settings.Ins.ZRDC12ATarget.Blue.y;
                        worksheet.Cell(25, 5).Value = Settings.Ins.ZRDC12ATarget.White.x;
                        worksheet.Cell(26, 5).Value = Settings.Ins.ZRDC12ATarget.White.y;
                        break;
                    case ConfigChrom.ZRD_C15A:
                        worksheet.Cell(18, 5).Value = Settings.Ins.ZRDC15ATarget.White.Lv;
                        worksheet.Cell(19, 5).Value = Settings.Ins.ZRDC15ATarget.Red.x;
                        worksheet.Cell(20, 5).Value = Settings.Ins.ZRDC15ATarget.Red.y;
                        worksheet.Cell(21, 5).Value = Settings.Ins.ZRDC15ATarget.Green.x;
                        worksheet.Cell(22, 5).Value = Settings.Ins.ZRDC15ATarget.Green.y;
                        worksheet.Cell(23, 5).Value = Settings.Ins.ZRDC15ATarget.Blue.x;
                        worksheet.Cell(24, 5).Value = Settings.Ins.ZRDC15ATarget.Blue.y;
                        worksheet.Cell(25, 5).Value = Settings.Ins.ZRDC15ATarget.White.x;
                        worksheet.Cell(26, 5).Value = Settings.Ins.ZRDC15ATarget.White.y;
                        break;
                    case ConfigChrom.ZRD_B12A:
                        worksheet.Cell(18, 5).Value = Settings.Ins.ZRDB12ATarget.White.Lv;
                        worksheet.Cell(19, 5).Value = Settings.Ins.ZRDB12ATarget.Red.x;
                        worksheet.Cell(20, 5).Value = Settings.Ins.ZRDB12ATarget.Red.y;
                        worksheet.Cell(21, 5).Value = Settings.Ins.ZRDB12ATarget.Green.x;
                        worksheet.Cell(22, 5).Value = Settings.Ins.ZRDB12ATarget.Green.y;
                        worksheet.Cell(23, 5).Value = Settings.Ins.ZRDB12ATarget.Blue.x;
                        worksheet.Cell(24, 5).Value = Settings.Ins.ZRDB12ATarget.Blue.y;
                        worksheet.Cell(25, 5).Value = Settings.Ins.ZRDB12ATarget.White.x;
                        worksheet.Cell(26, 5).Value = Settings.Ins.ZRDB12ATarget.White.y;
                        break;
                    case ConfigChrom.ZRD_B15A:
                        worksheet.Cell(18, 5).Value = Settings.Ins.ZRDB15ATarget.White.Lv;
                        worksheet.Cell(19, 5).Value = Settings.Ins.ZRDB15ATarget.Red.x;
                        worksheet.Cell(20, 5).Value = Settings.Ins.ZRDB15ATarget.Red.y;
                        worksheet.Cell(21, 5).Value = Settings.Ins.ZRDB15ATarget.Green.x;
                        worksheet.Cell(22, 5).Value = Settings.Ins.ZRDB15ATarget.Green.y;
                        worksheet.Cell(23, 5).Value = Settings.Ins.ZRDB15ATarget.Blue.x;
                        worksheet.Cell(24, 5).Value = Settings.Ins.ZRDB15ATarget.Blue.y;
                        worksheet.Cell(25, 5).Value = Settings.Ins.ZRDB15ATarget.White.x;
                        worksheet.Cell(26, 5).Value = Settings.Ins.ZRDB15ATarget.White.y;
                        break;
                    case ConfigChrom.ZRD_CH12D:
                        worksheet.Cell(18, 5).Value = Settings.Ins.ZRDCH12DTarget.White.Lv;
                        worksheet.Cell(19, 5).Value = Settings.Ins.ZRDCH12DTarget.Red.x;
                        worksheet.Cell(20, 5).Value = Settings.Ins.ZRDCH12DTarget.Red.y;
                        worksheet.Cell(21, 5).Value = Settings.Ins.ZRDCH12DTarget.Green.x;
                        worksheet.Cell(22, 5).Value = Settings.Ins.ZRDCH12DTarget.Green.y;
                        worksheet.Cell(23, 5).Value = Settings.Ins.ZRDCH12DTarget.Blue.x;
                        worksheet.Cell(24, 5).Value = Settings.Ins.ZRDCH12DTarget.Blue.y;
                        worksheet.Cell(25, 5).Value = Settings.Ins.ZRDCH12DTarget.White.x;
                        worksheet.Cell(26, 5).Value = Settings.Ins.ZRDCH12DTarget.White.y;
                        break;
                    case ConfigChrom.ZRD_CH15D:
                        worksheet.Cell(18, 5).Value = Settings.Ins.ZRDCH15DTarget.White.Lv;
                        worksheet.Cell(19, 5).Value = Settings.Ins.ZRDCH15DTarget.Red.x;
                        worksheet.Cell(20, 5).Value = Settings.Ins.ZRDCH15DTarget.Red.y;
                        worksheet.Cell(21, 5).Value = Settings.Ins.ZRDCH15DTarget.Green.x;
                        worksheet.Cell(22, 5).Value = Settings.Ins.ZRDCH15DTarget.Green.y;
                        worksheet.Cell(23, 5).Value = Settings.Ins.ZRDCH15DTarget.Blue.x;
                        worksheet.Cell(24, 5).Value = Settings.Ins.ZRDCH15DTarget.Blue.y;
                        worksheet.Cell(25, 5).Value = Settings.Ins.ZRDCH15DTarget.White.x;
                        worksheet.Cell(26, 5).Value = Settings.Ins.ZRDCH15DTarget.White.y;
                        break;
                    case ConfigChrom.ZRD_BH12D:
                        worksheet.Cell(18, 5).Value = Settings.Ins.ZRDBH12DTarget.White.Lv;
                        worksheet.Cell(19, 5).Value = Settings.Ins.ZRDBH12DTarget.Red.x;
                        worksheet.Cell(20, 5).Value = Settings.Ins.ZRDBH12DTarget.Red.y;
                        worksheet.Cell(21, 5).Value = Settings.Ins.ZRDBH12DTarget.Green.x;
                        worksheet.Cell(22, 5).Value = Settings.Ins.ZRDBH12DTarget.Green.y;
                        worksheet.Cell(23, 5).Value = Settings.Ins.ZRDBH12DTarget.Blue.x;
                        worksheet.Cell(24, 5).Value = Settings.Ins.ZRDBH12DTarget.Blue.y;
                        worksheet.Cell(25, 5).Value = Settings.Ins.ZRDBH12DTarget.White.x;
                        worksheet.Cell(26, 5).Value = Settings.Ins.ZRDBH12DTarget.White.y;
                        break;
                    case ConfigChrom.ZRD_BH15D:
                        worksheet.Cell(18, 5).Value = Settings.Ins.ZRDBH15DTarget.White.Lv;
                        worksheet.Cell(19, 5).Value = Settings.Ins.ZRDBH15DTarget.Red.x;
                        worksheet.Cell(20, 5).Value = Settings.Ins.ZRDBH15DTarget.Red.y;
                        worksheet.Cell(21, 5).Value = Settings.Ins.ZRDBH15DTarget.Green.x;
                        worksheet.Cell(22, 5).Value = Settings.Ins.ZRDBH15DTarget.Green.y;
                        worksheet.Cell(23, 5).Value = Settings.Ins.ZRDBH15DTarget.Blue.x;
                        worksheet.Cell(24, 5).Value = Settings.Ins.ZRDBH15DTarget.Blue.y;
                        worksheet.Cell(25, 5).Value = Settings.Ins.ZRDBH15DTarget.White.x;
                        worksheet.Cell(26, 5).Value = Settings.Ins.ZRDBH15DTarget.White.y;
                        break;
                    case ConfigChrom.ZRD_CH12D_S3:
                        worksheet.Cell(18, 5).Value = Settings.Ins.ZRDCH12DS3Target.White.Lv;
                        worksheet.Cell(19, 5).Value = Settings.Ins.ZRDCH12DS3Target.Red.x;
                        worksheet.Cell(20, 5).Value = Settings.Ins.ZRDCH12DS3Target.Red.y;
                        worksheet.Cell(21, 5).Value = Settings.Ins.ZRDCH12DS3Target.Green.x;
                        worksheet.Cell(22, 5).Value = Settings.Ins.ZRDCH12DS3Target.Green.y;
                        worksheet.Cell(23, 5).Value = Settings.Ins.ZRDCH12DS3Target.Blue.x;
                        worksheet.Cell(24, 5).Value = Settings.Ins.ZRDCH12DS3Target.Blue.y;
                        worksheet.Cell(25, 5).Value = Settings.Ins.ZRDCH12DS3Target.White.x;
                        worksheet.Cell(26, 5).Value = Settings.Ins.ZRDCH12DS3Target.White.y;
                        break;
                    case ConfigChrom.ZRD_CH15D_S3:
                        worksheet.Cell(18, 5).Value = Settings.Ins.ZRDCH15DS3Target.White.Lv;
                        worksheet.Cell(19, 5).Value = Settings.Ins.ZRDCH15DS3Target.Red.x;
                        worksheet.Cell(20, 5).Value = Settings.Ins.ZRDCH15DS3Target.Red.y;
                        worksheet.Cell(21, 5).Value = Settings.Ins.ZRDCH15DS3Target.Green.x;
                        worksheet.Cell(22, 5).Value = Settings.Ins.ZRDCH15DS3Target.Green.y;
                        worksheet.Cell(23, 5).Value = Settings.Ins.ZRDCH15DS3Target.Blue.x;
                        worksheet.Cell(24, 5).Value = Settings.Ins.ZRDCH15DS3Target.Blue.y;
                        worksheet.Cell(25, 5).Value = Settings.Ins.ZRDCH15DS3Target.White.x;
                        worksheet.Cell(26, 5).Value = Settings.Ins.ZRDCH15DS3Target.White.y;
                        break;
                    case ConfigChrom.ZRD_BH12D_S3:
                        worksheet.Cell(18, 5).Value = Settings.Ins.ZRDBH12DS3Target.White.Lv;
                        worksheet.Cell(19, 5).Value = Settings.Ins.ZRDBH12DS3Target.Red.x;
                        worksheet.Cell(20, 5).Value = Settings.Ins.ZRDBH12DS3Target.Red.y;
                        worksheet.Cell(21, 5).Value = Settings.Ins.ZRDBH12DS3Target.Green.x;
                        worksheet.Cell(22, 5).Value = Settings.Ins.ZRDBH12DS3Target.Green.y;
                        worksheet.Cell(23, 5).Value = Settings.Ins.ZRDBH12DS3Target.Blue.x;
                        worksheet.Cell(24, 5).Value = Settings.Ins.ZRDBH12DS3Target.Blue.y;
                        worksheet.Cell(25, 5).Value = Settings.Ins.ZRDBH12DS3Target.White.x;
                        worksheet.Cell(26, 5).Value = Settings.Ins.ZRDBH12DS3Target.White.y;
                        break;
                    case ConfigChrom.ZRD_BH15D_S3:
                        worksheet.Cell(18, 5).Value = Settings.Ins.ZRDBH15DS3Target.White.Lv;
                        worksheet.Cell(19, 5).Value = Settings.Ins.ZRDBH15DS3Target.Red.x;
                        worksheet.Cell(20, 5).Value = Settings.Ins.ZRDBH15DS3Target.Red.y;
                        worksheet.Cell(21, 5).Value = Settings.Ins.ZRDBH15DS3Target.Green.x;
                        worksheet.Cell(22, 5).Value = Settings.Ins.ZRDBH15DS3Target.Green.y;
                        worksheet.Cell(23, 5).Value = Settings.Ins.ZRDBH15DS3Target.Blue.x;
                        worksheet.Cell(24, 5).Value = Settings.Ins.ZRDBH15DS3Target.Blue.y;
                        worksheet.Cell(25, 5).Value = Settings.Ins.ZRDBH15DS3Target.White.x;
                        worksheet.Cell(26, 5).Value = Settings.Ins.ZRDBH15DS3Target.White.y;
                        break;
                    case ConfigChrom.Custom:
                        worksheet.Cell(18, 5).Value = Settings.Ins.CustomTarget.White.Lv;
                        worksheet.Cell(19, 5).Value = Settings.Ins.CustomTarget.Red.x;
                        worksheet.Cell(20, 5).Value = Settings.Ins.CustomTarget.Red.y;
                        worksheet.Cell(21, 5).Value = Settings.Ins.CustomTarget.Green.x;
                        worksheet.Cell(22, 5).Value = Settings.Ins.CustomTarget.Green.y;
                        worksheet.Cell(23, 5).Value = Settings.Ins.CustomTarget.Blue.x;
                        worksheet.Cell(24, 5).Value = Settings.Ins.CustomTarget.Blue.y;
                        worksheet.Cell(25, 5).Value = Settings.Ins.CustomTarget.White.x;
                        worksheet.Cell(26, 5).Value = Settings.Ins.CustomTarget.White.y;
                        break;
                }
	
	            // Cinema対応
	            //if (Settings.Ins.ConfigChromType == ConfigChrom.DigitalCinema2020)
	            //{
	            //    worksheet.Cell(18, 5).Value = 218;
	            //    worksheet.Cell(21, 5).Value = 0.202;
	            //    worksheet.Cell(22, 5).Value = 0.714;
	            //}
	
	            // Target
	            //if (header.ColorTemp == ColorTemp.D93)
	            //{
	            //    worksheet.Cell(18, 5).Value = 201;
	            //    worksheet.Cell(19, 3).Value = "Color\r\n(D93)";
	            //    //worksheet.Cell(4, 2).Value = "Color Temperature : D93";
	            //    worksheet.Cell(25, 5).Value = 0.283;
	            //    worksheet.Cell(26, 5).Value = 0.297;
	            //}
	            //else if (header.ColorTemp == ColorTemp.D65)
	            //{
	            //    worksheet.Cell(18, 5).Value = 188;
	            //    worksheet.Cell(19, 3).Value = "Color\r\n(D65)";
	            //    //worksheet.Cell(4, 2).Value = "Color Temperature : D65";
	            //    worksheet.Cell(25, 5).Value = 0.313;
	            //    worksheet.Cell(26, 5).Value = 0.329;
	            //}
	            //else
	            //{
	            //    worksheet.Cell(18, 5).Value = 168;
	            //    worksheet.Cell(19, 3).Value = "Color\r\n(D50)";
	            //    //worksheet.Cell(4, 2).Value = "Color Temperature : D50";
	            //    worksheet.Cell(25, 5).Value = 0.346;
	            //    worksheet.Cell(26, 5).Value = 0.359;
	            //}
	
	            // Header情報書き込み
	            worksheet = workbook.Worksheet(4);
	
	            worksheet.Cell(2, 2).Value = "Date : " + header.Date.ToString("MMM/dd/yyyy", new System.Globalization.CultureInfo("en-US"));
	            worksheet.Cell(3, 2).Value = "Model : " + header.Model;
	
	            workbook.Save();
            }

            return true;
        }
        
        private bool showResultWindowUnit(HeaderInfo header, string filePath, int unitCount)
        {
            var dispatcher = Application.Current.Dispatcher;

            dispatcher.Invoke(() =>
            {
                WindowMeasureResultUnit winMeasureResult = new WindowMeasureResultUnit(header, filePath, unitCount);
                winMeasureResult.Show();
            });

            return true;
        }
        
        private bool makeResultFileCell(HeaderInfo header, List<UnitColor> lstUnitColor, out string filePath)
        {
            string sourceFile = applicationPath + "\\Components\\Template_Module.xlsx";
            filePath = applicationPath + "\\Measurement\\" + header.Date.ToString("yyyyMMdd") + "_" + header.Model + /*"_" + header.ColorTemp.ToString() +*/ "_CellUniformity.xlsx";

            try
            {
                if (System.IO.File.Exists(filePath) == true)
                { filePath = makeNewFilePath(filePath); }

                System.IO.File.Copy(sourceFile, filePath);
            }
            catch (Exception ex)
            { throw new Exception("Failed to copy the template file.\r\n(" + ex.Message + ")"); }

            using (var workbook = new XLWorkbook(filePath))
            {
	            var worksheet = workbook.Worksheet(1);
	
	            int row = 2;
	            foreach (UnitColor color in lstUnitColor)
	            {
	                // Unit Name
	                worksheet.Cell(row, 1).Value = color.UnitName;
	
	                int cell = 0;
	                foreach (Chromaticity chrom in color.lstCenter)
	                {
	                    worksheet.Cell(row, cell * 3 + 2).Value = chrom.Lv;
	                    worksheet.Cell(row, cell * 3 + 3).Value = chrom.x;
	                    worksheet.Cell(row, cell * 3 + 4).Value = chrom.y;
	                    cell++;
	                }
	
	                row++;
	            }
	            
	            // Header情報書き込み
	            worksheet = workbook.Worksheet(2);
	
	            worksheet.Cell(2, 39).Value = "Date : " + header.Date.ToString("MMM/dd/yyyy", new System.Globalization.CultureInfo("en-US"));
	            worksheet.Cell(3, 39).Value = "Model : " + header.Model;
	
	            //// Target
	            //if (header.ColorTemp == ColorTemp.D93)
	            //{ worksheet.Cell(4, 39).Value = "Color Temperature : D93"; }
	            //else if (header.ColorTemp == ColorTemp.D65)
	            //{ worksheet.Cell(4, 39).Value = "Color Temperature : D65"; }
	            //else
	            //{ worksheet.Cell(4, 39).Value = "Color Temperature : D50"; }
	
	            workbook.Save();
            }

            return true;
        }

        private bool saveMeasuredData(HeaderInfo header, MeasurementMode mode, List<UnitColor> lstUnitColor)
        {
            try
            {
                string filePath = applicationPath + "\\Measurement\\" + header.Date.ToString("yyyyMMdd") + "_" + header.Model /*+ "_" + header.ColorTemp.ToString()*/;

                if (mode == MeasurementMode.UnitUniformity)
                { filePath += "_UnitUniformity.xml"; }
                else
                { filePath += "_CellUniformity.xml"; }

                //DataContractSerializerオブジェクトを作成
                //オブジェクトの型を指定する
                DataContractSerializer serializer = new DataContractSerializer(typeof(List<UnitColor>));

                //BOMが付かないUTF-8で、書き込むファイルを開く
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Encoding = new System.Text.UTF8Encoding(false);

                settings.Indent = true; // インデントを行う
                settings.IndentChars = "\t"; // インデントにタブ文字を用いる
                settings.NewLineOnAttributes = true; // 属性にも改行を行う

                using (XmlWriter xw = XmlWriter.Create(filePath, settings))
                {
                    //シリアル化し、XMLファイルに保存する
                    serializer.WriteObject(xw, lstUnitColor);
                }
            }
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }

            return true;
        }

        private bool loadMeasuredData(string path, out List<UnitColor> lstUnitColor)
        {
            lstUnitColor = new List<UnitColor>();

            try
            {
                //DataContractSerializerオブジェクトを作成
                DataContractSerializer serializer = new DataContractSerializer(typeof(List<UnitColor>));

                //読み込むファイルを開く
                XmlReader xr = XmlReader.Create(path);

                //XMLファイルから読み込み、逆シリアル化する
                lstUnitColor = (List<UnitColor>)serializer.ReadObject(xr);

                //ファイルを閉じる
                xr.Close();
            }
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }

            return true;
        }

        private string makeNewFilePath(string path)
        {
            string newPath = "";

            for (int i = 1; i < 100; i++)
            {
                newPath = System.IO.Path.GetDirectoryName(path) + "\\" + System.IO.Path.GetFileNameWithoutExtension(path) + "_" + i + ".xlsx";

                if (System.IO.File.Exists(newPath) != true)
                { break; }
            }

            return newPath;
        }

        private void setNormalSettingMeasure()
        {
            foreach (ControllerInfo controller in dicController.Values)
            {
                // Through Mode
                sendSdcpCommand(SDCPClass.CmdThroughModeOff, controller.IPAddress);
            }
        }

        #endregion Private Methods
    }
}
