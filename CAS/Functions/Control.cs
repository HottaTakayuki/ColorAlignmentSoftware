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

using SONY.Modules;
using MakeUFData;

namespace CAS
{
    // Control
    public partial class MainWindow : Window
    {
        #region Fields
        
        // Test Pattern Parameter
        private List<TestPatternParam> lstTestPatternParam;

        private string currentControllerIP = "0.0.0.0";
        private bool controlTabFirstSelect = true;

        private TestPattern lastPattern = TestPattern.Raster;

        #endregion Fields

        #region Events

        private void btnPowerOn_Click(object sender, RoutedEventArgs e)
        {
            string controller = cmbxControllerControl.SelectedItem.ToString();

            actionButton(sender, "Controller Power On");

            if (controller == "All")
            {
                foreach(ControllerInfo cont in dicController.Values)
                { sendSdcpCommand(SDCPClass.CmdPowerUp, cont.IPAddress); }
            }
            else
            { sendSdcpCommand(SDCPClass.CmdPowerUp); }

            releaseButton(sender);
        }

        private void btnStandby_Click(object sender, RoutedEventArgs e)
        {
            string controller = cmbxControllerControl.SelectedItem.ToString();

            actionButton(sender, "Controller Standby");
            
            if (controller == "All")
            {
                foreach (ControllerInfo cont in dicController.Values)
                { sendSdcpCommand(SDCPClass.CmdStandby, cont.IPAddress); }
            }
            else
            { sendSdcpCommand(SDCPClass.CmdStandby); }

            releaseButton(sender);
        }

        private void btnUnitOn_Click(object sender, RoutedEventArgs e)
        {
            string controller = cmbxControllerControl.SelectedItem.ToString();

            actionButton(sender, "Cabinet Power On");

            if (controller == "All")
            {
                foreach (ControllerInfo cont in dicController.Values)
                { sendSdcpCommand(SDCPClass.CmdUnitPowerOn, cont.IPAddress); }
            }
            else
            { sendSdcpCommand(SDCPClass.CmdUnitPowerOn); }

            releaseButton(sender);
        }

        private void btnUnitOff_Click(object sender, RoutedEventArgs e)
        {
            string controller = cmbxControllerControl.SelectedItem.ToString();

            actionButton(sender, "Cabinet Standby");

            if (controller == "All")
            {
                foreach (ControllerInfo cont in dicController.Values)
                { sendSdcpCommand(SDCPClass.CmdUnitPowerOff, cont.IPAddress); }
            }
            else
            { sendSdcpCommand(SDCPClass.CmdUnitPowerOff); }

            releaseButton(sender);
        }

        private async void btnReconfig_Click(object sender, RoutedEventArgs e)
        {
            bool status = false;

            tcMain.IsEnabled = false;
            actionButton(sender, "Reconfig Start.");

            winProgress = new WindowProgress("Reconfig Progress");
            winProgress.ShowMessage("Reconfig Start.");
            winProgress.Show();

            try
            {
                if (rbCellReplace.IsChecked == true)
                { status = await Task.Run(() => reconfig()); }
            }
            catch (Exception ex)
            { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

            string msg = "";
            string caption = "";
            if (status == true)
            {
                msg = "Reconfig Complete!";
                caption = "Complete";
            }
            else
            {
                msg = "Failed in Reconfig.";
                caption = "Error";
            }

            winProgress.Close();

            playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
            WindowMessage winMessage = new WindowMessage(msg, caption);
            winMessage.ShowDialog();

            releaseButton(sender, "Reconfig Done.");
            tcMain.IsEnabled = true;
        }

        private void btnReloadCont_Click(object sender, RoutedEventArgs e)
        {
            gdControllerStatus.IsEnabled = false;
            actionButton(sender, "Reload");

            getControllerSetting();
                        
            gdControllerStatus.IsEnabled = true;
            releaseButton(sender);
        }
        
        private void btnSetBurnIn_Click(object sender, RoutedEventArgs e)
        {
            string controller = cmbxControllerControl.SelectedItem.ToString();

            actionButton(sender, "Set Burn-In Correction");

            Byte[] cmd = new byte[SDCPClass.CmdBurnInCorrectSet.Length];
            Array.Copy(SDCPClass.CmdBurnInCorrectSet, cmd, SDCPClass.CmdBurnInCorrectSet.Length);

            try { cmd[20] = Convert.ToByte(txbBurnInCorrect.Text); }
            catch
            {
                ShowMessageWindow("Parameter is invalid.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                //return;
            }

            if (controller == "All")
            {
                foreach (ControllerInfo cont in dicController.Values)
                { sendSdcpCommand(cmd, cont.IPAddress); }
            }
            else
            { sendSdcpCommand(cmd); }

            releaseButton(sender);
        }

        private void rbPhotoModeOff_Checked(object sender, RoutedEventArgs e)
        {
            //string res;

            tcMain.IsEnabled = false;
            actionButton(sender, "Set Re-Photo Mode Off.");

            //foreach (ControllerInfo cont in dicController.Values)
            //{ sendAdcpCommand("re_photo_mode \"off\"", out res, cont.IPAddress); }

            releaseButton(sender);
            tcMain.IsEnabled = true;
        }

        private void rbPhotoModeMode1_Checked(object sender, RoutedEventArgs e)
        {
            //string res;

            tcMain.IsEnabled = false;
            actionButton(sender, "Set Re-Photo Mode Mode-1.");

            //foreach (ControllerInfo cont in dicController.Values)
            //{ sendAdcpCommand("re_photo_mode \"mode1\"", out res, cont.IPAddress); }

            releaseButton(sender);
            tcMain.IsEnabled = true;
        }

        private void rbPhotoModeMode2_Checked(object sender, RoutedEventArgs e)
        {
            //string res;

            tcMain.IsEnabled = false;
            actionButton(sender, "Set Re-Photo Mode Mode-2.");

            //foreach (ControllerInfo cont in dicController.Values)
            //{ sendAdcpCommand("re_photo_mode \"mode2\"", out res, cont.IPAddress); }

            releaseButton(sender);
            tcMain.IsEnabled = true;
        }

        private bool showFirst_cmbxPattern = true;
        private void cmbxPattern_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TestPatternParam param = new TestPatternParam();

            // Store
            if (showFirst_cmbxPattern != true)
            {
                param.R_ON = (bool)cbRed.IsChecked;
                param.G_ON = (bool)cbGreen.IsChecked;
                param.B_ON = (bool)cbBlue.IsChecked;
                try { param.R_FG_LV = Convert.ToInt32(txbForeColorR.Text); }
                catch { param.R_FG_LV = 0; }
                try { param.G_FG_LV = Convert.ToInt32(txbForeColorG.Text); }
                catch { param.G_FG_LV = 0; }
                try { param.B_FG_LV = Convert.ToInt32(txbForeColorB.Text); }
                catch { param.B_FG_LV = 0; }
                try { param.R_BG_LV = Convert.ToInt32(txbBackColorR.Text); }
                catch { param.R_BG_LV = 0; }
                try { param.G_BG_LV = Convert.ToInt32(txbBackColorG.Text); }
                catch { param.G_BG_LV = 0; }
                try { param.B_BG_LV = Convert.ToInt32(txbBackColorB.Text); }
                catch { param.B_BG_LV = 0; }
                try { param.H_ST_POS = Convert.ToInt32(txbStartPosX.Text); }
                catch { param.H_ST_POS = 0; }
                try { param.V_ST_POS = Convert.ToInt32(txbStartPosY.Text); }
                catch { param.V_ST_POS = 0; }
                try { param.H_WIDTH = Convert.ToInt32(txbWidthX.Text); }
                catch { param.H_WIDTH = 0; }
                try { param.V_WIDTH = Convert.ToInt32(txbWidthY.Text); }
                catch { param.V_WIDTH = 0; }
                try { param.H_PITCH = Convert.ToInt32(txbPitchX.Text); }
                catch { param.H_PITCH = 0; }
                try { param.V_PITCH = Convert.ToInt32(txbPitchY.Text); }
                catch { param.V_PITCH = 0; }
                param.INVERT = (bool)cbInvert.IsChecked;

                lstTestPatternParam[(int)lastPattern] = param;
            }

            // Restore
            if (lstTestPatternParam != null)
            {
                setPatternTextBox();
            }
        }

        private void btnSetTemp_Click(object sender, RoutedEventArgs e)
        {
            string controller = cmbxControllerControl.SelectedItem.ToString();

            actionButton(sender, "Set Temp. Correction");

            Byte[] cmd = new byte[SDCPClass.CmdTempCorrectSet.Length];
            Array.Copy(SDCPClass.CmdTempCorrectSet, cmd, SDCPClass.CmdTempCorrectSet.Length);

            try { cmd[20] = Convert.ToByte(txbTempCorrect.Text); }
            catch
            {
                ShowMessageWindow("Parameter is invalid.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return;
            }

            if (controller == "All")
            {
                foreach (ControllerInfo cont in dicController.Values)
                { sendSdcpCommand(cmd, cont.IPAddress); }
            }
            else
            { sendSdcpCommand(cmd); }

            releaseButton(sender);
        }
     
        private void btnIntSigOn_Click(object sender, RoutedEventArgs e)
        {
            string controller = cmbxControllerControl.SelectedItem.ToString();

            actionButton(sender, "Internal Signal On");

            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            setInternalSignalCommand(ref cmd);

            if (controller == "All")
            {
                foreach (ControllerInfo cont in dicController.Values)
                { sendSdcpCommand(cmd, 0, cont.IPAddress); }
            }
            else
            { sendSdcpCommand(cmd, 0); }

            //System.Threading.Thread.Sleep(500);
            releaseButton(sender);
        }

        private void btnIntSigOff_Click(object sender, RoutedEventArgs e)
        {
            string controller = cmbxControllerControl.SelectedItem.ToString();

            actionButton(sender, "Internal Signal Off");

            if (controller == "All")
            {
                foreach (ControllerInfo cont in dicController.Values)
                { sendSdcpCommand(SDCPClass.CmdIntSignalOff, 0, cont.IPAddress); }
            }
            else
            { sendSdcpCommand(SDCPClass.CmdIntSignalOff, 0); }

            System.Threading.Thread.Sleep(500);
            releaseButton(sender);
        }

        private void btnLayoutInfoOn_Click(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Disp Layout Info On");

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdDispUnitAddrOn, 0, cont.IPAddress); }

            releaseButton(sender);
        }

        private void btnLayoutInfoOff_Click(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Disp Layout Info Off");

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdDispUnitAddrOff, 0, cont.IPAddress); }

            releaseButton(sender);
        }

        private void txb_GotFocus(object sender, RoutedEventArgs e)
        {
            // TextBoxがフォーカスを受け取ったら、Textすべてを選択状態にする
            Action action = ((TextBox)sender).SelectAll; //txbForeColorR.SelectAll;
            Dispatcher.BeginInvoke(action);
        }
        
        //private void rbHDCPOn_Checked(object sender, RoutedEventArgs e)
        //{
        //    string ret;

        //    tcMain.IsEnabled = false;
        //    actionButton(sender, "Set HDCP On.");

        //    Byte[] cmd = new byte[SDCPClass.CmdHDCPSet.Length];
        //    Array.Copy(SDCPClass.CmdHDCPSet, cmd, SDCPClass.CmdHDCPSet.Length);

        //    cmd[20] = 0x01;

        //    foreach (ControllerInfo cont in dicController.Values)
        //    { sendSdcpCommand(cmd, out ret, cont.IPAddress); }

        //    releaseButton(sender);
        //    tcMain.IsEnabled = true;
        //}

        //private void rbHDCPOff_Checked(object sender, RoutedEventArgs e)
        //{
        //    tcMain.IsEnabled = false;
        //    actionButton(sender, "Set HDCP Off.");

        //    Byte[] cmd = new byte[SDCPClass.CmdHDCPSet.Length];
        //    Array.Copy(SDCPClass.CmdHDCPSet, cmd, SDCPClass.CmdHDCPSet.Length);

        //    cmd[20] = 0x00;

        //    foreach (ControllerInfo cont in dicController.Values)
        //    { sendSdcpCommand(cmd, cont.IPAddress); }

        //    releaseButton(sender);
        //    tcMain.IsEnabled = true;
        //}
        
        private void rbLowLevelOn_Checked(object sender, RoutedEventArgs e)
        {
            //string res;

            tcMain.IsEnabled = false;
            actionButton(sender, "Set Low Level Cutoff On.");

            //foreach (ControllerInfo cont in dicController.Values)
            //{ sendAdcpCommand("unit_bk_level_sw \"on\"", out res, cont.IPAddress); }

            releaseButton(sender);
            tcMain.IsEnabled = true;
        }

        private void rbLowLevelOff_Checked(object sender, RoutedEventArgs e)
        {
            //string res;

            tcMain.IsEnabled = false;
            actionButton(sender, "Set Low Level Cutoff Off.");

            //foreach (ControllerInfo cont in dicController.Values)
            //{ sendAdcpCommand("unit_bk_level_sw \"off\"", out res, cont.IPAddress); }

            releaseButton(sender);
            tcMain.IsEnabled = true;
        }
        
        //private void btnHdcpOnOff_Click(object sender, RoutedEventArgs e)
        //{
        //    if (gdHdcp.IsEnabled == true)
        //    {
        //        //lbHdcp.IsEnabled = false;
        //        gdHdcp.IsEnabled = false;
        //    }
        //    else
        //    {
        //        //lbHdcp.IsEnabled = true;
        //        gdHdcp.IsEnabled = true;
        //    }
        //}

        private void rbNfs_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Ins.TransType = TransType.NFS;
        }

        private void rbFtp_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Ins.TransType = TransType.FTP;
        }

        #endregion Events

        #region Private Methods

        private void setPatternTextBox()
        {
            TestPatternParam param = new TestPatternParam();

            try
            {
                param = lstTestPatternParam[cmbxPattern.SelectedIndex];

                cbRed.IsChecked = param.R_ON;
                cbGreen.IsChecked = param.G_ON;
                cbBlue.IsChecked = param.B_ON;
                txbForeColorR.Text = param.R_FG_LV.ToString();
                txbForeColorG.Text = param.G_FG_LV.ToString();
                txbForeColorB.Text = param.B_FG_LV.ToString();
                txbBackColorR.Text = param.R_BG_LV.ToString();
                txbBackColorG.Text = param.G_BG_LV.ToString();
                txbBackColorB.Text = param.B_BG_LV.ToString();
                txbStartPosX.Text = param.H_ST_POS.ToString();
                txbStartPosY.Text = param.V_ST_POS.ToString();
                txbWidthX.Text = param.H_WIDTH.ToString();
                txbWidthY.Text = param.V_WIDTH.ToString();
                txbPitchX.Text = param.H_PITCH.ToString();
                txbPitchY.Text = param.V_PITCH.ToString();
                cbInvert.IsChecked = param.INVERT;

                lastPattern = (TestPattern)cmbxPattern.SelectedIndex;

                showFirst_cmbxPattern = false;
            }
            catch { }
        }

        private void setInternalSignalCommand(ref Byte[] cmd)
        {
            try
            {
                // Foreground Color
                cmd[22] = (byte)(Convert.ToInt32(txbForeColorR.Text) >> 8); // Red
                cmd[23] = (byte)(Convert.ToInt32(txbForeColorR.Text) & 0xFF);
                cmd[24] = (byte)(Convert.ToInt32(txbForeColorG.Text) >> 8); // Green
                cmd[25] = (byte)(Convert.ToInt32(txbForeColorG.Text) & 0xFF);
                cmd[26] = (byte)(Convert.ToInt32(txbForeColorB.Text) >> 8); // Blue
                cmd[27] = (byte)(Convert.ToInt32(txbForeColorB.Text) & 0xFF);

                // Background Color
                cmd[28] = (byte)(Convert.ToInt32(txbBackColorR.Text) >> 8); ; // Red
                cmd[29] = (byte)(Convert.ToInt32(txbBackColorR.Text) & 0xFF);
                cmd[30] = (byte)(Convert.ToInt32(txbBackColorG.Text) >> 8); ; // Green
                cmd[31] = (byte)(Convert.ToInt32(txbBackColorG.Text) & 0xFF);
                cmd[32] = (byte)(Convert.ToInt32(txbBackColorB.Text) >> 8); ; // Blue
                cmd[33] = (byte)(Convert.ToInt32(txbBackColorB.Text) & 0xFF);

                // Start Position
                cmd[34] = (byte)(Convert.ToInt32(txbStartPosX.Text) >> 8);
                cmd[35] = (byte)(Convert.ToInt32(txbStartPosX.Text) & 0xFF);
                cmd[36] = (byte)(Convert.ToInt32(txbStartPosY.Text) >> 8);
                cmd[37] = (byte)(Convert.ToInt32(txbStartPosY.Text) & 0xFF);

                // H, V Width
                cmd[38] = (byte)(Convert.ToInt32(txbWidthX.Text) >> 8);
                cmd[39] = (byte)(Convert.ToInt32(txbWidthX.Text) & 0xFF);
                cmd[40] = (byte)(Convert.ToInt32(txbWidthY.Text) >> 8);
                cmd[41] = (byte)(Convert.ToInt32(txbWidthY.Text) & 0xFF);

                // H, V Pitch
                cmd[42] = (byte)(Convert.ToInt32(txbPitchX.Text) >> 8);
                cmd[43] = (byte)(Convert.ToInt32(txbPitchX.Text) & 0xFF);
                cmd[44] = (byte)(Convert.ToInt32(txbPitchY.Text) >> 8);
                cmd[45] = (byte)(Convert.ToInt32(txbPitchY.Text) & 0xFF);
            }
            catch { } // 無視する

            cmd[21] = 0;
            if (cbRed.IsChecked == true)
            { cmd[21] += 0x40; }
            if (cbGreen.IsChecked == true)
            { cmd[21] += 0x20; }
            if (cbBlue.IsChecked == true)
            { cmd[21] += 0x10; }

            switch ((TestPattern)cmbxPattern.SelectedIndex)
            {
                case TestPattern.Tile:
                    cmd[21] += 0x01;
                    break;
                case TestPattern.Checker:
                    cmd[21] += 0x02;
                    break;
                case TestPattern.Cross:
                    cmd[21] += 0x03;
                    break;
                case TestPattern.HRampInc:
                case TestPattern.HRampDec:
                    cmd[21] += 0x04;
                    break;
                case TestPattern.VRampInc:
                case TestPattern.VRampDec:
                    cmd[21] += 0x05;
                    break;
                case TestPattern.ColorBar:
                    cmd[21] += 0x06;
                    break;
                case TestPattern.Window:
                    cmd[21] += 0x09;
                    break;
                case TestPattern.Unit:// Hatch Cabinet
                case TestPattern.Cell:// Hatch Module
                    cmd[21] += 0x01;
                    break;
                case TestPattern.Raster:
                default:
                    cmd[21] += 0x00;
                    break;

            }

            if (cbInvert.IsChecked == true)
            { cmd[21] += 0x80; }
        }

        private bool getControllerSetting()
        {
            var dispatcher = Application.Current.Dispatcher;
            string buff;

            //string controller = (dispatcher.Invoke(() => cmbxControllerControl.SelectedItem.ToString()));
            
            if (dicController.Values.Count <= 0)
            { return true; }

            KeyValuePair<int, ControllerInfo> firstController = dicController.FirstOrDefault();
            ControllerInfo controller = firstController.Value;
            
            // Burn-In Correction
            sendSdcpCommand(SDCPClass.CmdBurnInCorrectGet, out buff, controller.IPAddress);
            try
            {
                int param = Convert.ToInt32(buff, 16);

                if (param >= 0 && param <= 10)
                { dispatcher.Invoke(() => (txbBurnInCorrect.Text = param.ToString())); }
                else
                { dispatcher.Invoke(() => (txbBurnInCorrect.Text = "-")); }
            }
            catch { dispatcher.Invoke(() => (txbBurnInCorrect.Text = "")); }

            // Temp. Correction
            sendSdcpCommand(SDCPClass.CmdTempCorrectGet, out buff, controller.IPAddress);
            try
            {
                int param = Convert.ToInt32(buff, 16);

                if (param >= 0 && param <= 10)
                { dispatcher.Invoke(() => (txbTempCorrect.Text = param.ToString())); }
                else
                { dispatcher.Invoke(() => (txbTempCorrect.Text = "-")); }
            }
            catch { dispatcher.Invoke(() => (txbTempCorrect.Text = "")); }

            return true;
        }

        private void btnUpdateHDCPCont_Click(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "HDCP Info update");

            getControllerHdcpStatus();

            releaseButton(sender);
        }

        public bool getControllerHdcpStatus()
        {
            var dispatcher = Application.Current.Dispatcher;

            dispatcher.Invoke(() => (gdHdcp.IsEnabled = false));

            try
            {
                foreach (ControllerHDCPInfo controllerHdcp in hdcpData)
                {
                    // Controller Power Status Check
                    PowerStatus power;
                    getPowerStatus(controllerHdcp.IPAddress, out power);

                    if (power != PowerStatus.Power_On)
                    { controllerHdcp.initHdcpInfo(); }
                    else
                    { controllerHdcp.UpdateHdcpInfo(); }
                }
            }
            catch
            { dispatcher.Invoke(() => (gdHdcp.IsEnabled = true)); }

            dispatcher.Invoke(() => (gdHdcp.IsEnabled = true));
            return true;
        }

        private bool reconfig()
        {
            if (Settings.Ins.ExecLog == true)
            {
                SaveExecLog("");
                SaveExecLog("[0] reconfig start");
            }

            // ●進捗の最大値を設定
            winProgress.ShowMessage("Set Step Count.");
            int step = 2;
            winProgress.SetWholeSteps(step);

            //// ●Power OnでないときはOnにする [1] 
            //if (Settings.Ins.ExecLog == true)
            //{ SaveExecLog("[1] Check Controller Power."); }
            //winProgress.ShowMessage("Check Controller Power.");

            //PowerStatus power;
            //bool powerUpWait = false;
            //foreach (ControllerInfo cont in dicController.Values)
            //{
            //    if (getPowerStatus(cont.IPAddress, out power) != true)
            //    { return false; }

            //    if (power != PowerStatus.Power_On)
            //    {
            //        winProgress.ShowMessage("Controller Power On. (Wait 60 sec.)");

            //        if (sendSdcpCommand(SDCPClass.CmdPowerUp, cont.IPAddress) != true)
            //        { return false; }

            //        powerUpWait = true;
            //    }
            //}

            //if (powerUpWait == true)
            //{ System.Threading.Thread.Sleep(Settings.Ins.PowerOnWait); }
            //winProgress.PutForward1Step();
            
            // ●Unit Power Off [1]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[1] Cabinet Power Off."); }
            winProgress.ShowMessage("Cabinet Power Off.");

            foreach (ControllerInfo controller in dicController.Values)
            {
            	controller.Target = true;
            	
                if (sendSdcpCommand(SDCPClass.CmdUnitPowerOff, controller.IPAddress) != true)
                { return false; }
            }

            System.Threading.Thread.Sleep(5000);
            winProgress.PutForward1Step();
                        
#if NO_WRITE
#else
            // ●Reconfig [2]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[2] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            sendReconfig();
            winProgress.PutForward1Step();
#endif

            //// ●Unit Power On [4]
            //if (Settings.Ins.ExecLog == true)
            //{ SaveExecLog("[2] Unit Power On."); }
            //winProgress.ShowMessage("Unit Power On.");

            //foreach (ControllerInfo cont in dicController.Values)
            //{ sendSdcpCommand(SDCPClass.CmdUnitPowerOn, cont.IPAddress); }
            //winProgress.PutForward1Step();

            return true;
        }

        #endregion Private Methods
    }
}
