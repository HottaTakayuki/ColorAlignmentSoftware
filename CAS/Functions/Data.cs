using MakeUFData;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace CAS
{
    // Backup
    public partial class MainWindow : Window
    {
        #region Fields

        private const int BACKUP_STEPS                = 11;
        private const int RESTORE_STEPS               = 16;
        private const int REPLACE_MODULE_STEPS        = 15;
        private const int REPLACE_RCS_BOARD_STEPS     = 22;
        private const int RECOVERY_CABINET_STEPS      = 16;
        private const int RECOVERY_MODULE_STEPS       = 19;
        private const int CONTROLLER_BACKUP_STEPS     = 6;
        private const int CONTROLLER_RESTORE_STEPS    = 7;

        private const string CLED_CONTROLLER_PERMANENT_DB = "cled_controller_permanent_db.json";
        private const string VIDEO_PROCESSOR_PERMANENT_DB = "video_processor_permanent_db.json";
        private const string CHANNEL_MEMORY_PERMANENT_DB  = "channel_memory_permanent_db.json";
        private const string PRODUCTINFOPERMANENT         = "ProductInfoPermanent.json";

        private const int FTP_RETRY_COUNT = 30;
        private const int FTP_RETRY_SLEEP_TIME = 3000;

        public const int SDCP_COMMAND_WAIT_TIME       = 0;
        public const int SLEEP_TIME_AFTER_PANEL_OFF   = 1000;

        public const int CONTROLLER_POWER_STATUS_TIMEOUT = 30; //sec
        public const string SDCP_NAK_NOT_APPLICABLE = "0180";
        public const string STR_HYPHEN = "-";

        public const string ADCP_ERROR_COMMAND = "err_cmd";

        //係数　データ移動時間
        public const double RESTORE_UD_FILE_MOVE_SEC = 0.06;
        public const double RESTORE_HC_FILE_MOVE_SEC = 0.16;
        public const double RESTORE_LC_FILE_MOVE_SEC = 0.26;
        //係数　レスポンス時間
        public const double RESTORE_UD_FILE_RESPONSE_SEC = 0.49;    //u2:各モデル一台当たりのudファイルの処理時間
        public const double RESTORE_HC_FILE_RESPONSE_SEC = 5.27;    //h2:各モデル一台当たりのhc,hfファイルの処理時間
        public const double RESTORE_LC_FILE_RESPONSE_SEC = 1.97;    //l2:各モデル一台当たりのlcファイルの処理時間
        public const double RESTORE_UD_FILE_RESPONSE_FIX_SEC = 0.53;    //u3:キャビネット台数に依存しないudファイルの処理時間
        public const double RESTORE_HC_FILE_RESPONSE_FIX_SEC = 0.85;    //h3:キャビネット台数に依存しないhc,hfファイルの処理時間
        public const double RESTORE_LC_FILE_RESPONSE_FIX_SEC = 0.78;    //l3:キャビネット台数に依存しないlcファイルの処理時間
        public const double RESTORE_FILE_RESPONSE_FIX_SEC = 21.4;   //ファイルの種類の選択に関わらず発生する処理時間
        //補正係数　レスポンス時間
        public const double RESTORE_FILE_SUPPLEMENT_SEC = 0.018;


        #endregion Fields

        #region Events

        // Backup
        private async void btnBackupStart_Click(object sender, RoutedEventArgs e)
        {
            bool status = false;
            var dispatcher = Application.Current.Dispatcher;

            tcMain.IsEnabled = false;
            actionButton(sender, "Backup Start.");

            string msg = "It takes approximate each 10 minutes per Controller to complete data transfer. However you may not cancel after start.\r\n\r\nAre you sure?";
            bool? result = false;
            showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 500, 210, "Yes");

            if (result == true)
            {
                winProgress = new WindowProgress("Backup Progress");
                winProgress.Show();
                
                try { status = await Task.Run(() => backup()); }
                catch (Exception ex)
                { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

                // ●Cabinet Power On [10]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[10] Cabinet Power On."); }
                winProgress.ShowMessage("Cabinet Power On.");
                foreach (ControllerInfo cont in dicController.Values)
                { sendSdcpCommand(SDCPClass.CmdUnitPowerOn, SDCP_COMMAND_WAIT_TIME, cont.IPAddress); }

                msg = "";
                string caption = "";
                if (status == true)
                {
                    winProgress.PutForward1Step();

                    msg = "Backup Complete!";
                    caption = "Complete";
                }
                else
                {
                    foreach (ControllerInfo controller in dicController.Values)
                    {
                        if (sendSdcpCommand(SDCPClass.CmdFtpOff, controller.IPAddress) != true)
                        {
                            ShowMessageWindow("Failed to FTP Off.", "Error!", System.Drawing.SystemIcons.Error, 420, 180);
                        }
                    }

                    msg = "Failed in Backup.";
                    caption = "Error";
                }

                winProgress.StopRemainTimer();
                winProgress.Close();

                playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
                WindowMessage winMessage = new WindowMessage(msg, caption);                
                winMessage.ShowDialog();
            }

            releaseButton(sender, "Backup Done.");
            tcMain.IsEnabled = true;
        }
        
        // Restore
        private async void btnRestoreStart_Click(object sender, RoutedEventArgs e)
        {
            bool status = false;
            var dispatcher = Application.Current.Dispatcher;

            tcMain.IsEnabled = false;
            actionButton(sender, "Restore Start.");

            string msg = "It takes approximate each 1 minute per Display cabinet to complete data transfer. However you may not cancel after start.\r\n\r\nAre you sure?";            
            bool? result = false;
            showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 500, 210, "Yes");

            if (result == true)
            {
                winProgress = new WindowProgress("Restore Progress");
                winProgress.ShowMessage("Restore Start.");
                winProgress.Show();

                if (Settings.Ins.ExecLog == true)
                {
                    SaveExecLog("cmbxRestoreTarget.SelectedIndex : " + cmbxRestoreTarget.SelectedIndex);
                }

                try { status = await Task.Run(() => restore()); }
                catch (Exception ex)
                { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 300, 180); }
                
                msg = "";
                string caption = "";
                if (status == true)
                {
                    msg = "Restore Complete!";
                    caption = "Complete";
                }
            else
            {
                foreach (ControllerInfo controller in dicController.Values)
                {
                    if (controller.Target == true)
                    {
                        if (sendSdcpCommand(SDCPClass.CmdFtpOff, controller.IPAddress) != true)
                        {
                            ShowMessageWindow("Failed to FTP Off.", "Error!", System.Drawing.SystemIcons.Error, 420, 180);
                        }
                    }
                }
                msg = "Failed in Restore.";
                caption = "Error";
            }

                winProgress.StopRemainTimer();
                winProgress.Close();

                playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
                WindowMessage winMessage = new WindowMessage(msg, caption);
                winMessage.ShowDialog();
            }

            releaseButton(sender, "Restore Done.");
            tcMain.IsEnabled = true;
        }
        
        // Replace
        private void rbCellReplace_Checked(object sender, RoutedEventArgs e)
        {
            if (aryUnitData != null)
            { diselectAllDataUnits(); }

            //if (gdCellFrameData != null)
            //{ gdCellFrameData.IsEnabled = true; }
        }

        private void rbUCBoard_Checked(object sender, RoutedEventArgs e)
        {
            //if (gdCellFrameData != null)
            //{ gdCellFrameData.IsEnabled = false; }
        }

        private async void btnReplace_Click(object sender, RoutedEventArgs e)
        {
            bool status = false;

            if (rbCellReplace.IsChecked == true || rbUCBoard.IsChecked == true)
            {
                tcMain.IsEnabled = false;
                actionButton(sender, "Replace Start.");

                winProgress = new WindowProgress("Replace Progress");
                winProgress.ShowMessage("Replace Start.");
                winProgress.Show();

                m_MakeUFData = new CMakeUFData(cabiDx, cabiDy);

                try
                {
                    if (rbCellReplace.IsChecked == true)
                    { status = await Task.Run(() => replaceCell()); }    // Replace Module
                    else if (rbUCBoard.IsChecked == true)
                    { status = await Task.Run(() => replaceUcBoard()); } // Replace RCS Board
                }
                catch (Exception ex)
                { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

                // ●Cabinet Power On
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("Cabinet Power On."); }
                foreach (ControllerInfo cont in dicController.Values)
                { sendSdcpCommand(SDCPClass.CmdUnitPowerOn, SDCP_COMMAND_WAIT_TIME, cont.IPAddress); }
                System.Threading.Thread.Sleep(5000);
                // ●Raster表示
                outputRaster(brightness._20pc); // 20%

                string msg = "";
                string caption = "";
                if (status == true)
                {
                    msg = "Replace Complete!";
                    caption = "Complete";
                }
                else
                {
                    foreach (ControllerInfo controller in dicController.Values)
                    {
                        if (controller.Target == true)
                        {
                            if (sendSdcpCommand(SDCPClass.CmdFtpOff, controller.IPAddress) != true)
                            {
                                ShowMessageWindow("Failed to FTP Off.", "Error!", System.Drawing.SystemIcons.Error, 420, 180);
                            }
                        }
                    }

                    msg = "Failed in Replace.";
                    caption = "Error";
                }

                // ●すべてのドライブをアンマウント
                unmountDirve();
                
                winProgress.Close();

                playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
                WindowMessage winMessage = new WindowMessage(msg, caption);
                winMessage.ShowDialog();

                releaseButton(sender, "Replace Done.");
                tcMain.IsEnabled = true;
            }
            else if (rbCpuReplace.IsChecked == true)
            {
                // Replace CPU
                tcMain.IsEnabled = false;
                actionButton(sender, "Replace Start.");

                WindowCpuReplaceInputIP winInputIp = new WindowCpuReplaceInputIP();

                bool? result = winInputIp.ShowDialog();
                if (result != true)
                {
                    releaseButton(sender, "Replace Cancel.");
                    tcMain.IsEnabled = true;
                    return;
                }

                WindowInputSerial winInputSerial = new WindowInputSerial();
                
                result = winInputSerial.ShowDialog();
                if (result == true)
                {
                    byte[] cmd = new byte[SDCPClass.CmdSerialNoSet.Length];
                    Array.Copy(SDCPClass.CmdSerialNoSet, cmd, SDCPClass.CmdSerialNoSet.Length);

                    try
                    {
                        int intSerial = int.Parse(winInputSerial.txbSerial.Text); // 設定するSerial
                        string strSerial = intSerial.ToString("X8");

                        cmd[20] = (byte)Convert.ToInt32(strSerial.Substring(0, 2), 16);
                        cmd[21] = (byte)Convert.ToInt32(strSerial.Substring(2, 2), 16);
                        cmd[22] = (byte)Convert.ToInt32(strSerial.Substring(4, 2), 16);
                        cmd[23] = (byte)Convert.ToInt32(strSerial.Substring(6, 2), 16);
                    }
                    catch { }

                    sendSdcpCommand(cmd, winInputIp.txbIP.Text);
                }

                playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
                WindowMessage winMessage = new WindowMessage("CPU Replace Done.", "Complete");
                winMessage.ShowDialog();

                releaseButton(sender, "Replace Done.");
                tcMain.IsEnabled = true;
            }
        }

        // Recovery
        private void rbCabinetRecovery_Checked(object sender, RoutedEventArgs e)
        {
            //if (gdCellFrameData != null)
            //{ gdCellFrameData.IsEnabled = false; }
        }

        private void rbModuleRecovery_Checked(object sender, RoutedEventArgs e)
        {
            //if(aryCellData != null)
            //{ diselectAllDataCells(); }

            if (aryUnitData != null)
            { diselectAllDataUnits(); }

            //if (gdCellFrameData != null)
            //{ gdCellFrameData.IsEnabled = true; }
        }

        private async void btnRecovery_Click(object sender, RoutedEventArgs e)
        {
            bool status = false;

            if (rbCabinetRecovery.IsChecked == true || rbModuleRecovery.IsChecked == true)
            {
                tcMain.IsEnabled = false;
                actionButton(sender, "Recovery Start.");

                winProgress = new WindowProgress("Recovery Progress");
                winProgress.ShowMessage("Recovery Start.");
                winProgress.Show();

                try
                {
                    if (rbCabinetRecovery.IsChecked == true)
                    { status = await Task.Run(() => recoveryCabinet()); }    // Recovery Cabinet
                    else if (rbModuleRecovery.IsChecked == true)
                    { status = await Task.Run(() => recoveryModule()); }     // Recovery Module
                }
                catch (Exception ex)
                { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

                string msg = "";
                string caption = "";
                if (status == true)
                {
                    msg = "Recovery Complete!";
                    caption = "Complete";
                }
                else
                {
                    foreach (ControllerInfo controller in dicController.Values)
                    {
                        if (controller.Target == true)
                        {
                            if (sendSdcpCommand(SDCPClass.CmdFtpOff, controller.IPAddress) != true)
                            {
                                ShowMessageWindow("Failed to FTP Off.", "Error!", System.Drawing.SystemIcons.Error, 420, 180);
                            }
                        }
                    }

                    msg = "Failed in Recovery.";
                    caption = "Error";
                }
                winProgress.Close();

                playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
                WindowMessage winMessage = new WindowMessage(msg, caption);
                winMessage.ShowDialog();

                releaseButton(sender, "Replace Done.");
                tcMain.IsEnabled = true;
            }
        }

        // Controller Data Backup
        private async void btnCDBackupStart_Click(object sender, RoutedEventArgs e)
        {
            var dispatcher = Application.Current.Dispatcher;
            tcMain.IsEnabled = false;
            actionButton(sender, "Controller Data Backup Start.");

            bool status = false;
            string contIP;

            WindowSelectController wscIP = new WindowSelectController();
            wscIP.Title = "Backup";
            wscIP.titleMsg.Text = "Please input backup controller IP address.";
            wscIP.tbxControllerIP.Focus();
            if (wscIP.ShowDialog() == true)
            {
                string msg = "Do you want to back up controller data?";
                bool? result = false;
                showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 360, 180, "Yes");
                if (result == true)
                {
                    winProgress = new WindowProgress("Backup Progress");
                    winProgress.ShowMessage("Controller data backup start.");
                    winProgress.Show();

                    contIP = wscIP.tbxControllerIP.Text;

                    try { status = await Task.Run(() => controllerDBBackup(contIP)); }
                    catch (Exception ex)
                    { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

                    msg = "";
                    string caption = "";
                    if (status == true)
                    {
                        msg = "Controller Data Backup Complete!";
                        caption = "Backup Complete";
                    }
                    else
                    {
                        if (sendSdcpCommand(SDCPClass.CmdFtpOff, contIP) != true)
                        {
                           ShowMessageWindow("Failed to FTP Off.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                        }

                        msg = "Failed in Controller Data Backup.";
                        caption = "Backup Error";
                    }

                    winProgress.Close();
                    ShowMessageWindow(msg, caption, System.Drawing.SystemIcons.Information, 360, 180);
                }
            }
            releaseButton(sender, "Controller Data Backup done.");
            tcMain.IsEnabled = true;
        }

        // Controller Data Restore
        private async void btnCDRestoreStart_Click(object sender, RoutedEventArgs e)
        {
            var dispatcher = Application.Current.Dispatcher;
            tcMain.IsEnabled = false;
            actionButton(sender, "Controller Data Restore Start.");

            bool status = false;
            string contIP;

            WindowSelectController wscIP = new WindowSelectController();
            wscIP.Title = "Restore";
            wscIP.titleMsg.Text = "Please input restore controller IP address.";
            wscIP.tbxControllerIP.Focus();
            if (wscIP.ShowDialog() == true)
            {
                string msg = "Do you want to restore controller data?";
                bool? result = false;
                showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 360, 180, "Yes");
                if (result == true)
                {
                    winProgress = new WindowProgress("Restore Progress");
                    winProgress.ShowMessage("Controller data restore start.");
                    winProgress.Show();

                    contIP = wscIP.tbxControllerIP.Text;

                    try { status = await Task.Run(() => controllerDBRestore(contIP)); }
                    catch (Exception ex)
                    { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

                    msg = "";
                    string caption = "";
                    if (status == true)
                    {
                        msg = "Controller Data Restore Complete!";
                        caption = "Restore Complete";
                    }
                    else
                    {
                        if (sendSdcpCommand(SDCPClass.CmdFtpOff, contIP) != true)
                        {
                            ShowMessageWindow("Failed to FTP Off.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                        }

                        msg = "Failed in Controller Data Restore.";
                        caption = "Restore Error";
                    }

                    winProgress.Close();
                    ShowMessageWindow(msg, caption, System.Drawing.SystemIcons.Information, 360, 180);
                }
            }
            releaseButton(sender, "Controller Data Restore done.");
            tcMain.IsEnabled = true;
        }

        private void UnitDataToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            // Restoreの選択に影響してしまうのでSkip
            // Unitを一つしか選択できないようにする処理
            //if (rbCell.IsChecked == true)
            //{
            //    diselectAllDataUnits();

            //    ((UnitToggleButton)sender).Checked -= UnitDataToggleButton_Checked;
            //    ((UnitToggleButton)sender).IsChecked = true;
            //    ((UnitToggleButton)sender).Checked += UnitDataToggleButton_Checked;
            //}
        }

        private void CellDataToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            //diselectAllDataCells();

            ((UnitToggleButton)sender).Checked -= CellDataToggleButton_Checked;
            ((UnitToggleButton)sender).IsChecked = true;
            ((UnitToggleButton)sender).Checked += CellDataToggleButton_Checked;
        }

        // UnitのDrag指定用
        private void utbData_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            rectAreaData.Visibility = System.Windows.Visibility.Visible;
            rectAreaData.Margin = new Thickness(e.GetPosition(this).X - 2, e.GetPosition(this).Y - 48, 0, 0);

            startPos = new Point(rectAreaData.Margin.Left, rectAreaData.Margin.Top);
        }

        private void utbData_PreviewMouseUp(object sender, MouseButtonEventArgs e)
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

            double offsetX = tcMain.Margin.Left + gdData.Margin.Left + gdAllocData.Margin.Left + gdAllocDataLayout.Margin.Left + cabinetAllocColumnHeaderWidth - svData.HorizontalOffset;
            double offsetY = tcMain.Margin.Top + gdData.Margin.Top + gdAllocData.Margin.Top + gdAllocDataLayout.Margin.Top + cabinetAllocRowHeaderHeight - svData.VerticalOffset;

            for (int y = 0; y < allocInfo.MaxY; y++)
            {
                for (int x = 0; x < allocInfo.MaxX; x++)
                {
                    if (aryUnitData[x, y].IsEnabled == false)
                    { continue; }

                    if (isContainedDragArea(aryUnitData[x, y], area, offsetX, offsetY) == true)
                    {
                        if (aryUnitData[x, y].IsChecked == true)
                        { aryUnitData[x, y].IsChecked = false; }
                        else
                        { aryUnitData[x, y].IsChecked = true; }
                    }
                }
            }

            rectAreaData.Visibility = System.Windows.Visibility.Hidden;
        }

        private void utbData_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            double height = e.GetPosition(this).Y - 48 - startPos.Y;
            try
            {
                if (height >= 0)
                { rectAreaData.Height = height; }
                else
                {
                    rectAreaData.Margin = new Thickness(rectAreaData.Margin.Left, startPos.Y + height, 0, 0);
                    rectAreaData.Height = Math.Abs(height);
                }
            }
            catch { rectAreaData.Height = 0; }

            double width = e.GetPosition(this).X - 2 - startPos.X;
            try
            {
                if (width >= 0)
                { rectAreaData.Width = width; }
                else
                {
                    rectAreaData.Margin = new Thickness(startPos.X + width, rectAreaData.Margin.Top, 0, 0);
                    rectAreaData.Width = Math.Abs(width);
                }
            }
            catch { rectAreaData.Width = 0; }
        }

        // Extended Data Disable
        private void cbExtendedDisable_Checked(object sender, RoutedEventArgs e)
        {
            //extendedDataDisable = true;
        }

        private void cbExtendedDisable_Unchecked(object sender, RoutedEventArgs e)
        {
            //extendedDataDisable = false;
        }

        #endregion Events

        #region Private Methods

        #region Common

        private bool isErrorResponseValue(string value)
        {
            bool isError = false;
            switch (value)
            {
                case "0101":    // Invalid Item
                case "0102":    // Invalid Item Request
                case "0103":    // Invalid Length
                case "0104":    // Invalid Data
                case "0111":    // Short Data
                case "0180":    // Not Applicable Item
                case "0201":    // Different Community
                case "1001":    // Invalid Version
                case "1002":    // Invalid Category
                case "1003":    // Invalid Request
                case "1011":    // Short Header
                case "1012":    // Short Community
                case "1013":    // Short Command
                case "2001":    // Timeout(Network Error)
                case "F001":    // Timeout(Comm Error)
                case "F010":    // Check Sum Error
                case "F020":    // Framing Error
                case "F030":    // Parity Error
                case "F040":    // Over Run Error
                case "F050":    // Other Comm Error
                case "F0F0":    // Unknown Response
                case "F110":    // Read Error
                case "F120":    // Write Error
                    isError = true;
                    break;
                default:
                    break;
            }

            return isError;
        }

        private string getModelName(string IpAddress)
        {
            string buff, model;

            sendSdcpCommand(SDCPClass.CmdModelName, out buff, IpAddress);
            model = convertAscii(buff);

            return model;
        }

        private string getSerialNo(string IpAddress)
        {
            string buff, serial;

            sendSdcpCommand(SDCPClass.CmdSerialNo, out buff, IpAddress);
            serial = convertSerial(buff);

            if (serial == "0000000")
            {
                System.Threading.Thread.Sleep(1000);

                sendSdcpCommand(SDCPClass.CmdSerialNo, out buff, IpAddress);
                serial = convertSerial(buff);
            }

            return serial;
        }

        private string getVersion(string ip)
        {
            string buff, version;

            sendSdcpCommand(SDCPClass.CmdControllerVersionGet, out buff, ip);
            version = convertVersion(buff);

            return version;
        }

        private string convertAscii(string code)
        {
            if (isErrorResponseValue(code))
            { return STR_HYPHEN; }

            string text = "";
            try
            {
                for (int i = 0; i < code.Length; i = i + 2)
                {
                    string ascii = code.Substring(i, 2);
                    char cha = (char)Convert.ToInt32(ascii, 16);
                    text += cha;
                }
            }
            catch
            { text = ""; }

            return text.Trim();
        }

        private string convertVersion(string strVersion)
        {
            if (isErrorResponseValue(strVersion))
            { return STR_HYPHEN; }

            string version;
            try
            { version = strVersion.Substring(0, 2) + "." + strVersion.Substring(2, 2) + "." + strVersion.Substring(4, 2); }
            catch
            { version = "00.00.00"; }

            return version;
        }

        private string convertSerial(string strSerial)
        {
            if (isErrorResponseValue(strSerial))
            { return STR_HYPHEN; }

            string ser;
            try
            {
                int serial = Convert.ToInt32(strSerial, 16);
                ser = serial.ToString("D7");
            }
            catch { ser = "0000000"; }

            return ser;
        }

        private bool mountNfsDrive(ControllerInfo controller)
        {
            bool mounted = false;

            // Mountする必要のないControllerはMountしない
            if(controller.NeedMount == false)
            { return true; }

            if (mountDrive(controller) != true)
            { return false; }

            System.Threading.Thread.Sleep(Settings.Ins.NfsWait);

            // NFSマウントを確認
            mounted = checkNfsMount(controller.Drive);
            if (mounted != true)
            {
                string msg = "Failed in mounting NFS Drive.";
                msg += "\r\n\r\nIf the network drive is mounted and it is currently not connected, the NFS mount fails.\r\nPlease temporarily unmount the network drive.";                
                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 500, 210);

                return false;
            }

            return true;
        }

        private bool mountDrive(ControllerInfo controller)
        {
            // 空いているドライブを検索する
            string drive = getBrankDrive();
            controller.Drive = drive;

            // Batファイルを作成する
            if (makeMountBatFile(controller.IPAddress, controller.Drive) != true)
            { return false; }

            // Batファイルを実行する
            callBatch("nfs_mount.bat");
            
            return true;
        }

        private string getBrankDrive()
        {
            string brankLetter = "";
            List<string> lstDrive = new List<string>();

            //string[] drives = Directory.GetLogicalDrives();

            //for (int i = 0; i < drives.Length; i++)
            //{ lstDrive.Add(drives[i]); }

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            { lstDrive.Add(drive.Name); }

            for (int code = 90; code > 0; code--)
            {
                string driveLetter = ((char)code).ToString() + ":\\";

                if (lstDrive.Contains(driveLetter) == false)
                {
                    brankLetter = driveLetter;
                    break;
                }
            }

            return brankLetter;
        }

        private bool makeMountBatFile(string ipAddress, string drive)
        {
            string cmd = @"C:\Windows\System32\mount.exe -o nolock \\" + ipAddress + "/usr/local/app/NativeDisplay/ufdata " + drive;
            string filePath = applicationPath + "\\Components\\nfs_mount.bat";

            try
            {
                using (StreamWriter sw = new StreamWriter(filePath))
                { sw.Write(cmd); }
            }
            catch
            {
                ShowMessageWindow("Faild in makeing nfs bat file.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }

            return true;
        }

        private int callBatch(string batFile)
        {
        /* Chiron1では使用しないため削除 */
        /*
            string batPath = applicationPath + "\\Components\\" + batFile;

            var startInfo = new ProcessStartInfo();
            startInfo.FileName = applicationPath + @"\Components\BatchExecute.exe"; //System.Environment.GetEnvironmentVariable("ComSpec");//@"C:\WINDOWS\system32\cmd.exe";
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            //startInfo.Verb = "RunAs";
            startInfo.Arguments = batPath;//batFile;
            Process process = Process.Start(startInfo);
            process.WaitForExit();
            return process.ExitCode;
        */
        	return 0;
        }

        private bool checkNfsMount(string driveLetter)
        {
            bool mounted = false;
            List<string> lstDrive = new List<string>();

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            { lstDrive.Add(drive.Name); }

            if (lstDrive.Contains(driveLetter) == true)
            {
                string[] files;

                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        files = Directory.GetFiles(driveLetter);
                        mounted = true;
                    }
                    catch //(Exception ex)
                    {
                        // NFSドライブのMountが確認できない場合、5000msec待機
                        if (Settings.Ins.ExecLog == true)
                        { MainWindow.SaveExecLog("*** NFS Mount Wait 5000[msec] (Drive : " + driveLetter + ") ***"); }

                        System.Threading.Thread.Sleep(5000);
                    }

                    if (mounted == true)
                    { break; }
                }
            }

            return mounted;
        }

        private bool deleteNfsFiles()
        {
            foreach (ControllerInfo controller in dicController.Values)
            {
                string[] files;
                try { files = Directory.GetFiles(controller.Drive, "*", System.IO.SearchOption.AllDirectories); }
                catch { continue; }

                for (int i = 0; i < files.Length; i++)
                {
                    try { FileSystem.DeleteFile(files[i], UIOption.OnlyErrorDialogs, RecycleOption.DeletePermanently); }
                    catch { }
                }
            }

            return true;
        }

        private bool deleteFtpFiles()
        {
            foreach (ControllerInfo controller in dicController.Values)
            {
                List<string> lstFiles;

                if (getFileListFtp(controller.IPAddress, out lstFiles) != true)
                { return false; }

                foreach (string file in lstFiles)
                {
                    string pass = makeFtpPassword(controller.IPAddress);
                    Uri u = new Uri("ftp://" + controller.IPAddress + ":21/" + file);

                    System.Net.FtpWebRequest ftpReq = (System.Net.FtpWebRequest)System.Net.WebRequest.Create(u);
                    ftpReq.Credentials = new System.Net.NetworkCredential(ftpUser, pass);
                    ftpReq.Method = System.Net.WebRequestMethods.Ftp.DeleteFile;
                    ftpReq.Proxy = null;

                    using (System.Net.FtpWebResponse ftpRes = (System.Net.FtpWebResponse)ftpReq.GetResponse())
                    {
                        if (ftpRes.StatusCode != System.Net.FtpStatusCode.FileActionOK)
                        { return false; }
                    }
                }
            }

            return true;
        }

        private bool deleteFtpFile(ControllerInfo controller)
        {
            List<string> lstFiles;

            if (getFileListFtp(controller.IPAddress, out lstFiles) != true)
            { return false; }

            foreach (string file in lstFiles)
            {
                string pass = makeFtpPassword(controller.IPAddress);
                Uri u = new Uri("ftp://" + controller.IPAddress + ":21/" + file);

                System.Net.FtpWebRequest ftpReq = (System.Net.FtpWebRequest)System.Net.WebRequest.Create(u);
                ftpReq.Credentials = new System.Net.NetworkCredential(ftpUser, pass);
                ftpReq.Method = System.Net.WebRequestMethods.Ftp.DeleteFile;
                ftpReq.Proxy = null;

                using (System.Net.FtpWebResponse ftpRes = (System.Net.FtpWebResponse)ftpReq.GetResponse())
                {
                    if (ftpRes.StatusCode != System.Net.FtpStatusCode.FileActionOK)
                    { return false; }
                }
            }

            return true;
        }

        private bool unmountDirve()
        {
            if(Settings.Ins.TransType == TransType.FTP)
            { return true; }

            // Batファイルを実行する
            callBatch("nfs_umount.bat");

            System.Threading.Thread.Sleep(3000);

            // NFS Off
            foreach (ControllerInfo controller in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdNfsOff, 0, controller.IPAddress); }

            return true;
        }

        private bool getFileListFtp(string ip, out List<string> lstFiles)
        {
            string pass = makeFtpPassword(ip);
            Uri u = new Uri("ftp://" + ip + ":21/");

            lstFiles = new List<string>();

            for (int i = 0; i < FTP_RETRY_COUNT; i++)
            {
                System.Net.FtpWebRequest ftpReq = (System.Net.FtpWebRequest)System.Net.WebRequest.Create(u);

                ftpReq.Credentials = new System.Net.NetworkCredential(ftpUser, pass);
                ftpReq.Method = System.Net.WebRequestMethods.Ftp.ListDirectory;
                ftpReq.KeepAlive = false;
                ftpReq.UsePassive = true;
                ftpReq.Proxy = null;
                ftpReq.Timeout = 30000;

                try
                {
                    using (System.Net.FtpWebResponse ftpRes = (System.Net.FtpWebResponse)ftpReq.GetResponse())
                    {
                        string res;
                        using (System.IO.StreamReader sr = new System.IO.StreamReader(ftpRes.GetResponseStream()))
                        { res = sr.ReadToEnd(); }

                        string[] lines = res.Split(new string[] { "\r", "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                        //Console.WriteLine("{0}: {1}", ftpRes.StatusCode, ftpRes.StatusDescription);

                        foreach (string line in lines)
                        { lstFiles.Add(Path.GetFileName(line.Trim())); }

                        return true;
                    }
                }
                catch (System.Net.WebException e)
                {
                    if (Settings.Ins.ExecLog == true)
                    {
                        SaveExecLog("        ListDirectory failed");
                        SaveExecLog("        IP: " + ip);
                        SaveExecLog("        ErrMessage: " + e.Message);
                        SaveExecLog("        ErrStatus: " + e.Status);
                    }

                    System.Threading.Thread.Sleep(FTP_RETRY_SLEEP_TIME);
                }
            }

            return false;
        }

        private bool getFilesFtpRetry(string ip, string destDir, List<string> lstTargetFiles, out List<string> lstSkippedFiles)
        {
            bool status;
            bool ret = true;

            lstSkippedFiles = new List<string>();

            foreach (string file in lstTargetFiles)
            {
                string destPath = destDir + file;

                int count = 0;
                while (true)
                {
                    if (count > Settings.Ins.FtpRetryCount)
                    {
                        lstSkippedFiles.Add(file);
                        SaveErrorLog("[getFilesFtp(" + count +")]Failed in getting the file. ( " + file + " )");
                        ret = false;
                        break;
                    }

                    // ファイル転送
                    if (Settings.Ins.ExecLog == true)
                    { SaveExecLog("\t\tGET start    " + file); }

                    System.Threading.Thread.Sleep(50);

                    status = getFileFtp(ip, destPath);
                    if (status == false)
                    {
                        System.Threading.Thread.Sleep(1000);

                        count++;
                        continue;
                    }

                    if (Settings.Ins.ExecLog == true)
                    { SaveExecLog("\t\tGET complete " + file); }

                    System.Threading.Thread.Sleep(50);

                    // ファイルがちゃんと転送されたか確認
                    if (File.Exists(destPath) == true)
                    { break; }

                    count++;
                }
            }

            return ret;
        }

        private bool getFileFtp(string ip, string destPath)
        {
            string pass = makeFtpPassword(ip);
            Uri u = new Uri("ftp://" + ip + ":21/" + System.IO.Path.GetFileName(destPath));

            for (int i = 0; i < FTP_RETRY_COUNT; i++)
            {
                System.Net.FtpWebRequest ftpReq = (System.Net.FtpWebRequest)System.Net.WebRequest.Create(u);

                ftpReq.Credentials = new System.Net.NetworkCredential(ftpUser, pass);
                ftpReq.Method = System.Net.WebRequestMethods.Ftp.DownloadFile;
                ftpReq.KeepAlive = false;
                ftpReq.UseBinary = true;
                ftpReq.UsePassive = true;
                ftpReq.Proxy = null;
                ftpReq.Timeout = 30000;

                try
                {
                    using (System.Net.FtpWebResponse ftpRes = (System.Net.FtpWebResponse)ftpReq.GetResponse())
                    {
                        using (System.IO.Stream resStrm = ftpRes.GetResponseStream())
                        {
                            using (System.IO.FileStream fs = new System.IO.FileStream(destPath, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                            {
                                byte[] buffer = new byte[1024];
                                while (true)
                                {
                                    int readSize = resStrm.Read(buffer, 0, buffer.Length);
                                    if (readSize == 0)
                                    { break; }

                                    fs.Write(buffer, 0, readSize);
                                }

                                return true;
                            }
                        }
                    }
                }
                catch (System.Net.WebException e)
                {
                    if (Settings.Ins.ExecLog == true)
                    {
                        SaveExecLog("        Download file failed");
                        SaveExecLog("        IP: " + ip);
                        SaveExecLog("        ErrMessage: " + e.Message);
                        SaveExecLog("        ErrStatus: " + e.Status);
                    }

                    System.Threading.Thread.Sleep(FTP_RETRY_SLEEP_TIME);
                }
            }

            return false;
        }

        private bool putFileFtpRetry(string ip, string srcPath)
        {
            bool status;
            List<string> lstDestFiles;
            string srcFile = System.IO.Path.GetFileName(srcPath);

            // 総データ長の再チェック && CRCの再チェック && DataType再チェック
            if (checkAllDataLength(srcPath) != true || confirmChecksum(srcPath) != true || checkCommonHeaderDataType(srcPath) != true)
            {
                string msg = "The following files could not be read correctly.\r\nPlease check the backup file.\r\n\r\n" + srcFile;

                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 340, 200);
                return false;
            }

            int count = 0;
            while (true)
            {
                if (count > Settings.Ins.FtpRetryCount)
                {
                    SaveErrorLog("[putFilesFtp(" + count + ")]Failed in putting the file. ( " + srcFile + " )");
                    return false;
                }

                // ファイル転送
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\t\tPUT start    " + srcFile); }

                System.Threading.Thread.Sleep(50);

                status = putFileFtp(ip, srcPath);
                if (status == false)
                {
                    System.Threading.Thread.Sleep(1000);

                    count++;
                    continue;
                }

                System.Threading.Thread.Sleep(1000);

                // ファイルがちゃんと転送されたか確認
                getFileListFtp(ip, out lstDestFiles);

                foreach(string file in lstDestFiles)
                {
                    if (file == srcFile)
                    {
                        if (Settings.Ins.ExecLog == true)
                        { SaveExecLog("\t\tPUT complete " + srcFile); }

                        System.Threading.Thread.Sleep(50);

                        return true;
                    }
                }

                count++;
            }
        }

        private bool putFileFtp(string ip, string srcPath)
        {
            string pass = makeFtpPassword(ip);
            Uri u = new Uri("ftp://" + ip + ":21/" + System.IO.Path.GetFileName(srcPath));

            for (int i = 0; i < FTP_RETRY_COUNT; i++)
            {
                System.Net.FtpWebRequest ftpReq = (System.Net.FtpWebRequest)System.Net.WebRequest.Create(u);

                ftpReq.Credentials = new System.Net.NetworkCredential(ftpUser, pass);
                ftpReq.Method = System.Net.WebRequestMethods.Ftp.UploadFile;
                ftpReq.KeepAlive = false;
                ftpReq.UseBinary = true;
                ftpReq.UsePassive = true;
                ftpReq.Proxy = null;
                ftpReq.Timeout = 30000;

                try
                {
                    using (System.IO.Stream reqStrm = ftpReq.GetRequestStream())
                    {
                        using (System.IO.FileStream fs = new System.IO.FileStream(srcPath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                        {
                            //アップロードStreamに書き込む
                            byte[] buffer = new byte[1024];
                            while (true)
                            {
                                int readSize = fs.Read(buffer, 0, buffer.Length);
                                if (readSize == 0)
                                { break; }
                                reqStrm.Write(buffer, 0, readSize);
                            }

                            return true;
                            //using (System.Net.FtpWebResponse ftpRes = (System.Net.FtpWebResponse)ftpReq.GetResponse())
                            //{
                            //    string res = string.Format("{0}: {1}", ftpRes.StatusCode, ftpRes.StatusDescription);                           
                            //}
                        }
                    }
                }
                catch (System.Net.WebException e)
                {
                    if (Settings.Ins.ExecLog == true)
                    {
                        SaveExecLog("        Upload file failed");
                        SaveExecLog("        IP: " + ip);
                        SaveExecLog("        ErrMessage: " + e.Message);
                        SaveExecLog("        ErrStatus: " + e.Status);
                    }

                    System.Threading.Thread.Sleep(FTP_RETRY_SLEEP_TIME);
                }
            }

            return false;
        }

        private string makeFtpPassword(string ip)
        {
            string pass = "";

            string[] octets = ip.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);

            pass = ((Convert.ToInt32(octets[0]) + Convert.ToInt32(octets[3])) * 999 + Convert.ToInt32(octets[1]) + Convert.ToInt32(octets[2]) + 3550).ToString();

            return pass;
        }

        private string makeNewFtpPassword(string ip)
        {
            // Contoroller data backup/restore用パスワード
            string pass = "";

            string[] octets = ip.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);

            pass = ((Convert.ToInt32(octets[0]) + Convert.ToInt32(octets[3])) * 998 + Convert.ToInt32(octets[1]) + Convert.ToInt32(octets[2]) + 3550).ToString();

            return pass;
        }

        private bool getUnitPowerStatus()
        {
            string buff = "";
            bool done = true;

            if (winProgress != null)
            { winProgress.ShowMessage("Waiting for Cabinet Power Off.\r\nMonitor Cabinet Power Status..."); }

            foreach (ControllerInfo controller in dicController.Values)
            {
                if (controller.Target == true)
                {
                    DateTime startTime = DateTime.Now;
                    
                    while (true)
                    {
                        if (sendSdcpCommand(SDCPClass.CmdUnitPowerStatusGet, out buff, controller.IPAddress) != true)
                        { return false; }

                        if (buff == "00") // PowerOff完了
                        { break; }
                        else if (buff == "0101") // SDCPコマンド未対応
                        {
                            done = false;
                            break;
                        }

                        TimeSpan span = DateTime.Now - startTime;
                        if (span.TotalSeconds > 300)
                        { return false; }

                        System.Threading.Thread.Sleep(1000);
                    }
                }
            }

            if (done == false)
            { System.Threading.Thread.Sleep(10000); }

            return true;
        }

        private bool sendReconfig()
        {
            string buff = "";

            // Reconfigコマンド送信
            foreach (ControllerInfo cont in dicController.Values)
            {
                if (cont.Target == true)
                { sendSdcpCommand(SDCPClass.CmdUnitReconfig, cont.IPAddress); }
            }

            System.Threading.Thread.Sleep(3000);

            DateTime startTime = DateTime.Now;

            if (winProgress != null)
            { winProgress.ShowMessage("Waiting for Reconfig.\r\nMonitor Reconfig Status..."); }

            for (int i = 0; i < 100; i++)
            {
                bool done = true;

                // Status取得コマンド送信
                foreach (ControllerInfo cont in dicController.Values)
                {
                    if (cont.Target == true)
                    {
                        sendSdcpCommand(SDCPClass.CmdUnitReconfigStatusGet, out buff, cont.IPAddress);
                        if (buff != "00")
                        {
                            // Reconfig中、もしくは旧モデルが1台でもあるとダメ
                            done = false;
                            break;
                        }
                    }
                }

                if (done == true)
                { break; }

                TimeSpan span = DateTime.Now - startTime;
                if (span.TotalSeconds > 100)
                {
                    winProgress.ShowMessage("Reconfig Timeout");
                    System.Threading.Thread.Sleep(3000);
                    break;
                }

                System.Threading.Thread.Sleep(1000);
            }

            return true;
        }

        #endregion Common

        #region Backup

        private bool backup(BackupMode backupMode = BackupMode.Normal, List<UnitInfo> lstTargetUnits = null, int dataMode = 4) // Data Mode 4 = Aging Data, 1 = Unit Data
        {
            if (Settings.Ins.ExecLog == true)
            {
                SaveExecLog("");
                SaveExecLog("[0] backup start");
            }

            var dispatcher = Application.Current.Dispatcher;
            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmm"); // Unit Log用
            List<string> lstBrokenFiles = new List<string>(); // データが破損しているFile
            List<string> lstSkippedFiles = new List<string>(); // FTPで転送できなかったファイル
            bool status;

            if (backupMode == BackupMode.Individual && (lstTargetUnits == null || lstTargetUnits.Count <= 0))
            { return false; }

            // ●進捗の最大値を設定
            winProgress.ShowMessage("Set Step Count.");
            int step = BACKUP_STEPS;

            // Individualの場合、1Unitずつコマンド発行とComplete待ちを繰り返す
            if(backupMode == BackupMode.Individual)
            { step += 2 * lstTargetUnits.Count - 1; }

            winProgress.SetWholeSteps(step);

            bool unitData = false, hcData = false, lcData = false;
            dispatcher.Invoke(() => getCheckBoxStatusBackup(out unitData, out hcData, out lcData));
            if (unitData == false && hcData == false && lcData == false)
            {
                ShowMessageWindow("Please select target data.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }

            //推定処理時間
            int processSec = 0;

            int currentStep = 0;
            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));

            int cabinetCount = 0;
            dispatcher.Invoke(() => getTotalCabinets(out cabinetCount, allocInfo.MaxX, allocInfo.MaxY));
            if (cabinetCount == 0)
            {
                ShowMessageWindow("Not found cabinets.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
            }

            PowerStatus power;
            bool powerUpWait = false;   //poweronでないコントローラーが存在するかどうか

            foreach (ControllerInfo cont in dicController.Values)
            {
                if (getPowerStatus(cont.IPAddress, out power) != true)
                { return false; }

                if (power != PowerStatus.Power_On)
                {
                    powerUpWait = true;
                    break;
                }
            }
            
            int maxConnectCabinet = 0;
            foreach (ControllerInfo cont in dicController.Values)
            {
                if (maxConnectCabinet < cont.UnitCount)
                { maxConnectCabinet = cont.UnitCount; }
            }

            int dataMoveSec = calcBackupDataMoveSec(cabinetCount, unitData, hcData, lcData);
            int responseSec = calcBackupResponseSec(maxConnectCabinet, unitData, hcData, lcData);

            processSec = calcBackupProcessSec(currentStep,powerUpWait, responseSec, dataMoveSec);


            winProgress.StartRemainTimer(processSec);

            // ●すでにBackupフォルダがある場合は退避する [1]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[1] Move Old Backup Folder."); }
            winProgress.ShowMessage("Move Old Backup Folder.");

            if (backupMode == BackupMode.Normal && Directory.Exists(applicationPath + "\\Backup") == true)
            {
                string destPath = applicationPath + "\\Backup_" + DateTime.Now.ToString("yyyyMMddHHmm");
                try { Directory.Move(applicationPath + "\\Backup", destPath); }
                catch(Exception ex)
                {
                    ShowMessageWindow("Failed to save backup folder.\r\nException : " + ex.Message, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }
            }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcBackupProcessSec(currentStep,powerUpWait, responseSec, dataMoveSec);
            winProgress.SetRemainTimer(processSec);

            // ●Power OnでないときはOnにする [2]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[2] Check Controller Power."); }
            winProgress.ShowMessage("Check Controller Power.");

            if (!setAllControllerPowerOn())
            { return false; }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcBackupProcessSec(currentStep,powerUpWait, responseSec, dataMoveSec);
            winProgress.SetRemainTimer(processSec);

            // ●すべてのControllerが対象
            foreach (ControllerInfo controller in dicController.Values)
            { controller.Target = true; }

            // Controllerごとの処理
            foreach (ControllerInfo controller in dicController.Values)
            {
                // ●FTP ON [*1]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*1] FTP On."); }
                winProgress.ShowMessage("FTP On.");

                if (sendSdcpCommand(SDCPClass.CmdFtpOn, controller.IPAddress) != true)
                { return false; }

                // ●Model名取得 [*2]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*2] Get Model Name."); }
                winProgress.ShowMessage("Get Model Name.");

                if (string.IsNullOrWhiteSpace(controller.ModelName) == true)
                { controller.ModelName = getModelName(controller.IPAddress); }

                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("     Model Name : " + controller.ModelName); }

                // ●Serial取得 [*3]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*3] Get Serial No."); }
                winProgress.ShowMessage("Get Serial No.");

                if (string.IsNullOrWhiteSpace(controller.SerialNo) == true)
                { controller.SerialNo = getSerialNo(controller.IPAddress); }

                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("     Serial Num. : " + controller.SerialNo); }

                // ●HDCP On
                //if (Settings.Ins.ExecLog == true)
                //{ SaveExecLog("[*6] HDCP On."); }
                //winProgress.ShowMessage("HDCP On.");

                //string buff;
                //bool result = sendSdcpCommand(SDCPClass.CmdHDCPSet, out buff, controller.IPAddress);
                //if (result == false)
                //{
                //    // タイムアウトの場合は再送信
                //    System.Threading.Thread.Sleep(1000);
                //    sendSdcpCommand(SDCPClass.CmdHDCPSet, out buff, controller.IPAddress);
                //}

                //// コマンド未対応
                //if (buff == "0101")
                //{
                //    supportHdcpCmd = false;
                //    dispatcher.Invoke(() => (gdHdcp.IsEnabled = false));
                //}
            }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcBackupProcessSec(currentStep, powerUpWait, responseSec, dataMoveSec);
            winProgress.SetRemainTimer(processSec);

            #region Individual以外
            if (backupMode != BackupMode.Individual)
            {
                // ●Cabinet Power Off [3]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[3] Cabinet Power Off"); }
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
                winProgress.PutForward1Step();

                dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
                processSec = calcBackupProcessSec(currentStep,powerUpWait, responseSec, dataMoveSec);
                winProgress.SetRemainTimer(processSec);

                // ●Reconfig [4]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[4] Send Reconfig."); }
                winProgress.ShowMessage("Send Reconfig.");

                sendReconfig();
                winProgress.PutForward1Step();

                dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
                processSec = calcBackupProcessSec(currentStep,powerUpWait, responseSec, dataMoveSec);
                winProgress.SetRemainTimer(processSec);

                // ●Backupコマンド発行 [5]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[5] Send Backup Command."); }
                winProgress.ShowMessage("Send Backup Command.");

                byte[] cmd = new byte[SDCPClass.CmdDataRead_UnitData.Length];
                Array.Copy(SDCPClass.CmdDataRead_UnitData, cmd, SDCPClass.CmdDataRead_UnitData.Length);

                cmd[cmd.Length - 1] = 0; // 初期化

                if (backupMode == BackupMode.Normal)
                {
                    if (unitData == true)
                    {
                        cmd[cmd.Length - 1] += 0x01;
                        //if (extendedDataDisable != true)
                        //{ cmd[cmd.Length - 1] += 8; } // Extended Data
                    }

                    if (hcData == true)
                    {
                        cmd[cmd.Length - 1] += 0x02;
                        cmd[cmd.Length - 1] += 0x40;
                    }

                    //if (agingData == true)
                    //{ cmd[cmd.Length - 1] += 0x04; }

                    if(lcData == true)
                    { cmd[cmd.Length - 1] += 0x20; }
                }
                else if (backupMode == BackupMode.UnitDataOnly)
                { cmd[cmd.Length - 1] += 0x01; } // UC Board Replaceの場合はUnit Dataのみデータをとる
                //else if (backupMode == BackupMode.AgingDataOnly)
                //{ cmd[cmd.Length - 1] += 0x04; }
                else if (backupMode == BackupMode.UnitLog)
                { cmd[cmd.Length - 1] += 0x10; }

                foreach (ControllerInfo controller in dicController.Values)
                { sendSdcpCommand(cmd, controller.IPAddress); }
                winProgress.PutForward1Step();

                dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
                processSec = calcBackupProcessSec(currentStep,powerUpWait, responseSec, dataMoveSec);
                winProgress.SetRemainTimer(processSec);

                // ●Complete待ち [6]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[6] Waiting for the response."); }
                winProgress.ShowMessage("Waiting for the response.");

                try
                {
                    bool[] complete = new bool[dicController.Count];

                    while (true)
                    {
                        int id = 0;
                        foreach (ControllerInfo controller in dicController.Values)
                        {
                            complete[id] = checkCompleteFtp(controller.IPAddress, "read_complete");
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
                processSec = calcBackupProcessSec(currentStep,powerUpWait, responseSec, dataMoveSec);
                winProgress.SetRemainTimer(processSec);

                // ●Reconfig [7]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[7] Send Reconfig."); }
                winProgress.ShowMessage("Send Reconfig.");

                sendReconfig();
                winProgress.PutForward1Step();

                dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
                processSec = calcBackupProcessSec(currentStep,powerUpWait, responseSec, dataMoveSec);
                winProgress.SetRemainTimer(processSec);

            }
            #endregion Individual以外
            #region Individual
            else
            {
                // ●Unit Power Off [4]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[4] Cabinet Power Off"); }
                winProgress.ShowMessage("Cabinet Power Off.");

                foreach (ControllerInfo cont in dicController.Values)
                { sendSdcpCommand(SDCPClass.CmdUnitPowerOff, cont.IPAddress); }

                System.Threading.Thread.Sleep(10000);
                winProgress.PutForward1Step();

                dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
                processSec = calcBackupProcessSec(currentStep,powerUpWait, responseSec, dataMoveSec);
                winProgress.SetRemainTimer(processSec);

                // ●Reconfig [5]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[5] Send Reconfig."); }
                winProgress.ShowMessage("Send Reconfig.");

                sendReconfig();
                winProgress.PutForward1Step();

                dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
                processSec = calcBackupProcessSec(currentStep,powerUpWait, responseSec, dataMoveSec);
                winProgress.SetRemainTimer(processSec);

                foreach (UnitInfo unit in lstTargetUnits)
                {
                    // ●Backupコマンド発行 [6]
                    if (Settings.Ins.ExecLog == true)
                    { SaveExecLog("[6] Send Backup Command."); }
                    winProgress.ShowMessage("Send Backup Command.");

                    byte[] cmd = new byte[SDCPClass.CmdDataRead_UnitData.Length];
                    Array.Copy(SDCPClass.CmdDataRead_UnitData, cmd, SDCPClass.CmdDataRead_UnitData.Length);

                    cmd[cmd.Length - 1] = (byte)dataMode;

                    string strPort = Convert.ToString(unit.PortNo, 16);
                    string strUnit = Convert.ToString(unit.UnitNo, 16);
                    int num16 = Convert.ToInt32(strPort + strUnit, 16);

                    cmd[9] = (byte)num16;

                    sendSdcpCommand(cmd, dicController[unit.ControllerID].IPAddress);

                    System.Threading.Thread.Sleep(5000);
                    winProgress.PutForward1Step();

                    dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
                    processSec = calcBackupProcessSec(currentStep,powerUpWait, responseSec, dataMoveSec);
                    winProgress.SetRemainTimer(processSec);

                    // ●Complete待ち [7]
                    if (Settings.Ins.ExecLog == true)
                    { SaveExecLog("[7] Waiting for the response."); }
                    winProgress.ShowMessage("Waiting for the response.");

                    while (true)
                    {
                        if (Settings.Ins.TransType == TransType.FTP)
                        {
                        if (checkCompleteFtp(dicController[unit.ControllerID].IPAddress, "read_complete") == true)
                        { break; }
                        }
                        else
                        {
                            if (checkComplete(dicController[unit.ControllerID].Drive, "read_complete") == true)
                            { break; }
                        }

                        winProgress.ShowMessage("Waiting for the process of controller.");

                        System.Threading.Thread.Sleep(1000);
                    }

                    winProgress.PutForward1Step();

                    dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
                    processSec = calcBackupProcessSec(currentStep,powerUpWait, responseSec, dataMoveSec);
                    winProgress.SetRemainTimer(processSec);

                }

                // ●Reconfig [8]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[8] Send Reconfig."); }
                winProgress.ShowMessage("Send Reconfig.");

                sendReconfig();
                winProgress.PutForward1Step();

                dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
                processSec = calcBackupProcessSec(currentStep,powerUpWait, responseSec, dataMoveSec);
                winProgress.SetRemainTimer(processSec);

            }
            #endregion Individual

            // ●File移動 [8]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[8] Move Files."); }
            winProgress.ShowMessage("Move Files.");

            foreach (ControllerInfo controller in dicController.Values)
            {
                string destDir;
                bool isInitial = false; // Initialかどうか。Initialがないと他があってもInitial扱い
				List<string> lstFtpFiles;

                if(Settings.Ins.TransType == TransType.NFS && controller.NeedMount == false)
                { continue; }

                // コピー先のフォルダを決定
                if (backupMode == BackupMode.Normal) // Backup実行時、Backupフォルダは退避する仕様になったので必ずInitialになる
                {
                    destDir = applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo + "_Rbin\\";
                    isInitial = true;
                }
                else if (backupMode == BackupMode.UnitDataOnly)
                { destDir = applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo + "_Rbin\\"; }
                else if (backupMode == BackupMode.Individual)
                { destDir = applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo + "_Latest\\"; }
                else if (backupMode == BackupMode.UnitLog)
                { destDir = applicationPath + "\\Log\\Cabinet\\" + timeStamp + "\\" + controller.ModelName + "_" + controller.SerialNo + "\\"; }
                else
                { return false; }

                // Directory作成
                if (Directory.Exists(destDir) != true)
                {
                    try { Directory.CreateDirectory(destDir); }
                    catch (Exception ex)
                    {
                        ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                        return false;
                    }
                }

                winProgress.ShowMessage("Move Files.");

                // 移動
                // 対象ファイルのリストを取得
                if(getFileListFtp(controller.IPAddress, out lstFtpFiles) != true)
                { return false; }

                // ファイルのコピー
                try
                {
                    status = getFilesFtpRetry(controller.IPAddress, destDir, lstFtpFiles, out lstSkippedFiles);
                    if(status == false)
                    { return false; }
                }
                catch { return false; }

                // ファイルの破損チェック
                foreach (string file in lstFtpFiles)
                {
                    string destPath = destDir + file;

                    if (confirmChecksum(destPath) != true)
                    { lstBrokenFiles.Add(destPath); }
                }

                // ●破損ファイルがある場合は再読み込み
                if (appliMode != ApplicationMode.Developer && Settings.Ins.ReadIndividualUnits == true && lstBrokenFiles.Count > 0)
                {
                    if (Settings.Ins.ExecLog == true)
                    { SaveExecLog("[9] Reload Files."); }
                    winProgress.ShowMessage("Reload Files.");

                    if (reloadDataFile(controller, lstBrokenFiles, destDir) != true)
                    {
                        string msg = "Failed to reload the file.\r\nThe backup file may be damaged.\r\nPlease check the backup file before doing other work.";                        
                        ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 500, 210);
                        return false;
                    }
                }

                // Initial時はPrevious, Latestにも同時にコピー
                if (isInitial == true)
                {
                    if (copyBackup(applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo, lstBrokenFiles) != true)
                    { return false; }
                }
            }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcBackupProcessSec(currentStep,powerUpWait, responseSec, dataMoveSec);
            winProgress.SetRemainTimer(processSec);

            if (Settings.Ins.ReadIndividualUnits == false && lstBrokenFiles.Count > 0)
            {
                string msg = "The following files could not be read correctly.\r\nPlease check the backup file before doing other work.\r\n\r\n";

                foreach (string str in lstBrokenFiles)
                { msg += str + "\r\n"; }

                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 500, 300);
                return false;
            }

            // ●FTP OFF [9]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[9] FTP Off."); }
            winProgress.ShowMessage("FTP Off.");

            foreach (ControllerInfo controller in dicController.Values)
            {
                if (sendSdcpCommand(SDCPClass.CmdFtpOff, controller.IPAddress) != true)
                {
                    ShowMessageWindow("Failed to FTP Off.", "Error!", System.Drawing.SystemIcons.Error, 420, 180);
                    return false;
                }
            }

            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcBackupProcessSec(currentStep,powerUpWait, responseSec, dataMoveSec);
            winProgress.SetRemainTimer(processSec);

            // Skipされたファイルがある場合は警告を表示
            if (Settings.Ins.ReadIndividualUnits == false && lstSkippedFiles.Count > 0)
            {
                string msg = "The following files is skipped.\r\nPlease check the backup file before doing other work.\r\n\r\n";

                foreach (string str in lstSkippedFiles)
                { msg += str + "\r\n"; }

                ShowMessageWindow(msg, "Caution!", System.Drawing.SystemIcons.Error, 500, 300);
            }

            return true;
        }

        private void getCheckBoxStatusBackup(out bool unitData, out bool hcData, out bool lcData/*, out bool agingData*/)
        {
            unitData = (bool)cbUnitData.IsChecked;
            hcData = (bool)cbHcData.IsChecked;
            lcData = (bool)cbLcData.IsChecked;
            //agingData = (bool)cbAgingData.IsChecked;
        }

        private bool checkComplete(string path, string fileName)
        {
            bool status = false;

            string[] files = Directory.GetFiles(path, "*", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].Contains(fileName) == true)
                {
                    status = true;
                    break;
                }
            }

            return status;
        }

        private bool checkCompleteFtp(string ip, string fileName)
        {
            List<string> lstFiles;

            if (getFileListFtp(ip, out lstFiles) != true)
            { throw new Exception("ListDirectory failed"); }

            foreach(string file in lstFiles)
            {
                if(file == fileName)
                { return true; }
            }

            return false;
        }

        //private string makeDestPath(string baseDir, out bool isInitial)
        //{            
        //    string destPath = baseDir + "_Initial\\";

        //    isInitial = false;

        //    if (Directory.Exists(destPath) != true)
        //    {
        //        isInitial = true;
        //        return destPath;
        //    }

        //    destPath = baseDir + "_Previous\\";

        //    if (Directory.Exists(destPath) != true)
        //    { return destPath; }

        //    // _Initial, _Previousが存在する場合は自動的に_Latest（_Latestが存在する場合も含む）
        //    destPath = baseDir + "_Latest\\";

        //    return destPath;
        //}

        // InitialフォルダをPrevious, Latestへコピーする
        private bool copyBackup(string targetDir, List<string> lstBrokenFiles)
        {
            string sourceDir = targetDir + "_Rbin";            

            // ●Initial
            string initDir = targetDir + "_Initial\\";

            // フォルダがある場合は削除
            if (Directory.Exists(initDir) == true)
            { Directory.Delete(initDir, true); }

            try { Directory.CreateDirectory(initDir); }
            catch { return false; }

            string[] files = Directory.GetFiles(sourceDir, "*", System.IO.SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                string name = System.IO.Path.GetFileName(files[i]);
                string destPath = initDir + name;

                if (name.Contains("_ud.rbin") || name.Contains("_hc.rbin") || name.Contains("_lc.rbin"))
                {
                    if (checkAllDataLength(files[i]) == true && checkCommonHeaderDataType(files[i]) == true)
                    {
                        try
                        { convertRbinToBin(files[i], initDir); }
                        catch
                        {
                            if (lstBrokenFiles.Contains(files[i]) == false)
                            { lstBrokenFiles.Add(files[i]); }
                        }
                    }
                    else
                    {
                        if (lstBrokenFiles.Contains(files[i]) == false)
                        { lstBrokenFiles.Add(files[i]); }
                    }
                }
                else if (name.Contains("_hf.rbin"))
                { 
                    if (checkAllDataLength(files[i]) != true || checkCommonHeaderDataType(files[i]) != true)
                    {
                        if (lstBrokenFiles.Contains(files[i]) == false)
                        { lstBrokenFiles.Add(files[i]); }
                    }
                }
                //else
                //{ File.Copy(files[i], destPath, true); }
            }

            // ●Previous
            string destDir = targetDir + "_Previous\\";

            // フォルダがある場合は削除
            if (Directory.Exists(destDir) == true)
            { Directory.Delete(destDir, true); }

            try { Directory.CreateDirectory(destDir); }
            catch { return false; }

            files = Directory.GetFiles(initDir, "*", System.IO.SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    string destPath = destDir + System.IO.Path.GetFileName(files[i]);
                    File.Copy(files[i], destPath, true);
                }
                catch { return false; }
            }

            // ●Latest
            destDir = targetDir + "_Latest\\";

            // フォルダがある場合は削除
            if (Directory.Exists(destDir) == true)
            { Directory.Delete(destDir, true); }

            try { Directory.CreateDirectory(destDir); }
            catch { return false; }

            files = Directory.GetFiles(initDir, "*", System.IO.SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                try
                {                    
                    string destPath = destDir + System.IO.Path.GetFileName(files[i]);
                    File.Copy(files[i], destPath, true);
                }
                catch { return false; }
            }

            // ●Temp Tempフォルダも一緒に作っておく
            destDir = targetDir + "_Temp\\";

            if (Directory.Exists(destDir) == false)
            {
                try { Directory.CreateDirectory(destDir); }
                catch{ return false; }
            }

            return true;
        }

        private bool checkAllDataLength(string file)
        {
            byte[] srcBytes = File.ReadAllBytes(file);

            if (allocInfo.LEDModel == ZRD_C12A || allocInfo.LEDModel == ZRD_C15A || allocInfo.LEDModel == ZRD_B12A || allocInfo.LEDModel == ZRD_B15A)
            {
                if (file.Contains("_ud.rbin") || file.Contains("_ud.bin"))
                {
                    if (srcBytes.Length == CommonHeaderLength + UdDataLength_Module4x2)
                    { return true; }
                    else
                    { return false; }
                }

                if (allocInfo.LEDModel == ZRD_C12A || allocInfo.LEDModel == ZRD_B12A)
                {
                    if (file.Contains("_hc.rbin") || file.Contains("_hc.bin"))
                    {
                        if (srcBytes.Length == CommonHeaderLength + HcDataLengthP12_Module4x2)
                        { return true; }
                        else
                        { return false; }
                    }

                    if (file.Contains("_lc.rbin") || file.Contains("_lc.bin"))
                    {
                        if (srcBytes.Length == CommonHeaderLength + LcDataLengthP12_Module4x2)
                        { return true; }
                        else
                        { return false; }
                    }

                    if (file.Contains("_hf.rbin"))
                    {
                        if (srcBytes.Length == CommonHeaderLength + HfDataLengthP12_Module4x2)
                        { return true; }
                        else
                        { return false; }
                    }
                }
                else // ZRD-C15A, ZRD-B15A
                {
                    if (file.Contains("_hc.rbin") || file.Contains("_hc.bin"))
                    {
                        if (srcBytes.Length == CommonHeaderLength + HcDataLengthP15_Module4x2)
                        { return true; }
                        else
                        { return false; }
                    }

                    if (file.Contains("_lc.rbin") || file.Contains("_lc.bin"))
                    {
                        if (srcBytes.Length == CommonHeaderLength + LcDataLengthP15_Module4x2)
                        { return true; }
                        else
                        { return false; }
                    }

                    if (file.Contains("_hf.rbin"))
                    {
                        if (srcBytes.Length == CommonHeaderLength + HfDataLengthP15_Module4x2)
                        { return true; }
                        else
                        { return false; }
                    }
                }
            }
            else // ZRD-CH12D, ZRD-CH15D, ZRD-BH12D, ZRD-BH15D, ZRD-CH12D/3, ZRD-CH15D/3, ZRD-BH12D/3, ZRD-BH15D/3
            {
                if (file.Contains("_ud.rbin") || file.Contains("_ud.bin"))
                {
                    if (srcBytes.Length == CommonHeaderLength + UdDataLength_Module4x3)
                    { return true; }
                    else
                    { return false; }
                }

                if (allocInfo.LEDModel == ZRD_CH12D
                    || allocInfo.LEDModel == ZRD_BH12D
                    || allocInfo.LEDModel == ZRD_CH12D_S3
                    || allocInfo.LEDModel == ZRD_BH12D_S3)
                {
                    if (file.Contains("_hc.rbin") || file.Contains("_hc.bin"))
                    {
                        if (srcBytes.Length == CommonHeaderLength + HcDataLengthP12_Module4x3)
                        { return true; }
                        else
                        { return false; }
                    }

                    if (file.Contains("_lc.rbin") || file.Contains("_lc.bin"))
                    {
                        if (srcBytes.Length == CommonHeaderLength + LcDataLengthP12_Module4x3)
                        { return true; }
                        else
                        { return false; }
                    }

                    if (file.Contains("_hf.rbin"))
                    {
                        if (srcBytes.Length == CommonHeaderLength + HfDataLengthP12_Module4x3)
                        { return true; }
                        else
                        { return false; }
                    }
                }
                else // ZRD-CH15D, ZRD-BH15D, ZRD-CH15D/3, ZRD-BH15D/3
                {
                    if (file.Contains("_hc.rbin") || file.Contains("_hc.bin"))
                    {
                        if (srcBytes.Length == CommonHeaderLength + HcDataLengthP15_Module4x3)
                        { return true; }
                        else
                        { return false; }
                    }

                    if (file.Contains("_lc.rbin") || file.Contains("_lc.bin"))
                    {
                        if (srcBytes.Length == CommonHeaderLength + LcDataLengthP15_Module4x3)
                        { return true; }
                        else
                        { return false; }
                    }

                    if (file.Contains("_hf.rbin"))
                    {
                        if (srcBytes.Length == CommonHeaderLength + HfDataLengthP15_Module4x3)
                        { return true; }
                        else
                        { return false; }
                    }
                }
            }

            return false;
        }

        private bool checkCommonHeaderDataType(string file)
        {
            byte[] srcBytes = File.ReadAllBytes(file);

            if (srcBytes.Length < CommonHeaderLength)
            { return false; }

            byte[] tempBytes = new byte[CommonHeaderDataTypeSize];

            Array.Copy(srcBytes, CommonHeaderDataTypeOffset, tempBytes, 0, CommonHeaderDataTypeSize);

            uint dataType = BitConverter.ToUInt32(tempBytes, 0);

            if ((allocInfo.LEDModel == ZRD_B12A
                || allocInfo.LEDModel == ZRD_B15A
                || allocInfo.LEDModel == ZRD_C12A
                || allocInfo.LEDModel == ZRD_C15A) && dataType == dt_Module4x2)
            {
                return true;
            }
            else if ((allocInfo.LEDModel == ZRD_BH12D
                || allocInfo.LEDModel == ZRD_BH15D
                || allocInfo.LEDModel == ZRD_CH12D
                || allocInfo.LEDModel == ZRD_CH15D
                || allocInfo.LEDModel == ZRD_BH12D_S3
                || allocInfo.LEDModel == ZRD_BH15D_S3
                || allocInfo.LEDModel == ZRD_CH12D_S3
                || allocInfo.LEDModel == ZRD_CH15D_S3) && dataType == dt_Module4x3)
            {
                return true;
            }
            else
            { 
                return false;
            }
        }

        private bool confirmChecksum(string filePath)
        {
            //if (System.IO.Path.GetFileName(filePath).Contains("_ud.bin") == true || System.IO.Path.GetFileName(filePath).Contains("_ud.rbin"))
            //{ return checkUnitDataChiron(filePath); }
            ////else if (System.IO.Path.GetFileName(filePath).Contains("_ed.bin") == true)
            ////{ return checkExtendedData(filePath); }
            //else if (System.IO.Path.GetFileName(filePath).Contains("_cd.bin") == true || System.IO.Path.GetFileName(filePath).Contains("_cd.rbin") == true)
            //{ return checkCellDataChiron(filePath); }
            //else if (System.IO.Path.GetFileName(filePath).Contains("_ad.bin") == true || System.IO.Path.GetFileName(filePath).Contains("_ad.rbin") == true)
            //{ return checkAgingDataChiron(filePath); }

            // HeaderのCRCを使う場合ファイルの種類で差がない
            if (System.IO.Path.GetFileName(filePath).Contains("_ud.bin") == true || System.IO.Path.GetFileName(filePath).Contains("_ud.rbin")
                || System.IO.Path.GetFileName(filePath).Contains("_hc.bin") == true || System.IO.Path.GetFileName(filePath).Contains("_hc.rbin") == true
                || System.IO.Path.GetFileName(filePath).Contains("_lc.bin") == true || System.IO.Path.GetFileName(filePath).Contains("_lc.rbin") == true)
            //|| System.IO.Path.GetFileName(filePath).Contains("_ad.bin") == true || System.IO.Path.GetFileName(filePath).Contains("_ad.rbin") == true
            //|| System.IO.Path.GetFileName(filePath).Contains("_hf.bin") == true)
            { return checkDataChiron(filePath); }

            return true;
        }

        private bool checkDataChiron(string filePath)
        {
            byte[] srcBytes = File.ReadAllBytes(filePath);

            if (srcBytes.Length < CommonHeaderLength)
            { return false; }

            byte[] tempBytes;
            byte[] crcBytes = new byte[CommonHeaderDataCrcSize];

            // Product Info以降のデータ長を調べる
            byte[] lenBytes = new byte[CommonHeaderDataLengthSize];
            Array.Copy(srcBytes, CommonHeaderDataLengthOffset, lenBytes, 0, CommonHeaderDataLengthSize);
            int len = BitConverter.ToInt32(lenBytes, 0);

            if (len > srcBytes.Length - CommonHeaderLength)
            { return false; }

            tempBytes = new byte[len];
            Array.Copy(srcBytes, ProductInfoAddress, tempBytes, 0, len);

            // tempBytesに切り出した部分から計算したCRC            
            byte[] newCrcBytes;
            CalcCrc(tempBytes, out newCrcBytes);
            int newCrc = BitConverter.ToInt32(newCrcBytes, 0);

            // もともと記載のCRC
            Array.Copy(srcBytes, CommonHeaderDataCrcOffset, crcBytes, 0, CommonHeaderDataCrcSize);
            int orgCrc = BitConverter.ToInt32(crcBytes, 0);

            if (newCrc == orgCrc)
            { return true; }
            else
            { return false; }
        }

        //private bool checkUnitData(string filePath)
        //{
        //    byte[] srcBytes = File.ReadAllBytes(filePath);

        //    // unit_info
        //    byte[] tempBytes = new byte[128];
        //    Array.Copy(srcBytes, 8, tempBytes, 0, 128);

        //    if(compareChecksum(tempBytes, 124) != true)
        //    { return false; }

        //    // uc_board_data
        //    tempBytes = new byte[512];
        //    Array.Copy(srcBytes, 512, tempBytes, 0, 512);

        //    if (compareChecksum(tempBytes, 508) != true)
        //    { return false; }

        //    // input_gamma_table
        //    tempBytes = new byte[12416];
        //    Array.Copy(srcBytes, 1024, tempBytes, 0, 12416);

        //    if (compareChecksum(tempBytes, 124) != true)
        //    { return false; }

        //    // vsaw_table
        //    for (int i = 0; i < 7; i++)
        //    {
        //        tempBytes = new byte[8320];
        //        Array.Copy(srcBytes, 27472 + i * 8320, tempBytes, 0, 8320);

        //        if (compareChecksum(tempBytes, 124) != true)
        //        { return false; }
        //    }

        //    // panel_gamma_table
        //    for (int i = 0; i < 7; i++)
        //    {
        //        tempBytes = new byte[4544];
        //        Array.Copy(srcBytes, 94032 + i * 4544, tempBytes, 0, 4544);

        //        if (compareChecksum(tempBytes, 124) != true)
        //        { return false; }
        //    }

        //    return true;
        //}

        //private bool checkUnitDataChiron(string filePath)
        //{
        //    byte[] srcBytes = File.ReadAllBytes(filePath);
        //    byte[] tempBytes;
        //    byte[] crcBytes = new byte[4];

        //    // Product Info以降のデータ長を調べる
        //    byte[] lenBytes = new byte[4];
        //    Array.Copy(srcBytes, 0x0c, lenBytes, 0, 4);
        //    int len = BitConverter.ToInt32(lenBytes, 0);

        //    tempBytes = new byte[len];
        //    Array.Copy(srcBytes, 32, tempBytes, 0, len);

        //    // tempBytesに切り出した部分から計算したCRC            
        //    byte[] newCrcBytes;
        //    CalcCrc(tempBytes, out newCrcBytes);
        //    int newCrc = BitConverter.ToInt32(newCrcBytes, 0);

        //    // もともと記載のCRC
        //    Array.Copy(srcBytes, 16, crcBytes, 0, 4);
        //    int orgCrc = BitConverter.ToInt32(crcBytes, 0);

        //    if(newCrc == orgCrc)
        //    { return true; }
        //    else
        //    { return false; }
        //}

        //private bool checkExtendedData(string filePath)
        //{
        //    byte[] srcBytes = File.ReadAllBytes(filePath);
        //    byte[] tempBytes;

        //    //// unit_info // ExtendedのUnit_InfoはすべてFFがデフォルトでCheckSumが機能していない
        //    //byte[] tempBytes = new byte[128];
        //    //Array.Copy(srcBytes, 8, tempBytes, 0, 128);

        //    //if (compareChecksum(tempBytes, 124) != true)
        //    //{ return false; }

        //    // vsaw_table(1)
        //    for (int i = 0; i < 7; i++)
        //    {
        //        tempBytes = new byte[8320]; // 66,560 bit
        //        Array.Copy(srcBytes, 27472 + i * 8320, tempBytes, 0, 8320);

        //        if (compareChecksum(tempBytes, 124) != true)
        //        { return false; }
        //    }

        //    // panel_gamma_table(1)
        //    for (int i = 0; i < 7; i++)
        //    {
        //        tempBytes = new byte[4544]; // 36,352 bit
        //        Array.Copy(srcBytes, 94032 + i * 4544, tempBytes, 0, 4544);

        //        if (compareChecksum(tempBytes, 124) != true)
        //        { return false; }
        //    }

        //    // vsaw_table(2)
        //    for (int i = 0; i < 7; i++)
        //    {
        //        tempBytes = new byte[8320]; // 66,560 bit
        //        Array.Copy(srcBytes, 158544 + i * 8320, tempBytes, 0, 8320);

        //        if (compareChecksum(tempBytes, 124) != true)
        //        { return false; }
        //    }

        //    // panel_gamma_table(2)
        //    for (int i = 0; i < 7; i++)
        //    {
        //        tempBytes = new byte[4544]; // 36,352 bit
        //        Array.Copy(srcBytes, 225104 + i * 4544, tempBytes, 0, 4544);

        //        if (compareChecksum(tempBytes, 124) != true)
        //        { return false; }
        //    }

        //    // vsaw_table(3)
        //    for (int i = 0; i < 7; i++)
        //    {
        //        tempBytes = new byte[8320]; // 66,560 bit
        //        Array.Copy(srcBytes, 289616 + i * 8320, tempBytes, 0, 8320);

        //        if (compareChecksum(tempBytes, 124) != true)
        //        { return false; }
        //    }

        //    // panel_gamma_table(3)
        //    for (int i = 0; i < 7; i++)
        //    {
        //        tempBytes = new byte[4544]; // 36,352 bit
        //        Array.Copy(srcBytes, 356176 + i * 4544, tempBytes, 0, 4544);

        //        if (compareChecksum(tempBytes, 124) != true)
        //        { return false; }
        //    }

        //    // vsaw_table(4)
        //    for (int i = 0; i < 7; i++)
        //    {
        //        tempBytes = new byte[8320]; // 66,560 bit
        //        Array.Copy(srcBytes, 420688 + i * 8320, tempBytes, 0, 8320);

        //        if (compareChecksum(tempBytes, 124) != true)
        //        { return false; }
        //    }

        //    // panel_gamma_table(4)
        //    for (int i = 0; i < 7; i++)
        //    {
        //        tempBytes = new byte[4544]; // 36,352 bit
        //        Array.Copy(srcBytes, 487248 + i * 4544, tempBytes, 0, 4544);

        //        if (compareChecksum(tempBytes, 124) != true)
        //        { return false; }
        //    }

        //    return true;
        //}

        //private bool checkCellData(string filePath)
        //{
        //    byte[] srcBytes = File.ReadAllBytes(filePath);

        //    // 下記以外
        //    byte[] tempBytes = new byte[256];
        //    Array.Copy(srcBytes, 0, tempBytes, 0, 256);

        //    if (compareChecksum(tempBytes, 252) != true)
        //    { return false; }

        //    // aging_parameters
        //    tempBytes = new byte[1024]; // 8,192 bit
        //    Array.Copy(srcBytes, 256, tempBytes, 0, 1024);

        //    if (compareChecksum(tempBytes, 1020) != true)
        //    { return false; }

        //    // pixel_data
        //    tempBytes = new byte[129792]; // 1,038,336 bit
        //    Array.Copy(srcBytes, 1280, tempBytes, 0, 129792);

        //    if (compareChecksum(tempBytes, 188) != true)
        //    { return false; }

        //    return true;
        //}

        //private bool checkCellDataChiron(string filePath)
        //{
        //    byte[] srcBytes = File.ReadAllBytes(filePath);
        //    byte[] tempBytes;
        //    byte[] crcBytes = new byte[4];

        //    // Product Info以降のデータ長を調べる
        //    byte[] lenBytes = new byte[4];
        //    Array.Copy(srcBytes, 0x0c, lenBytes, 0, 4);
        //    int len = BitConverter.ToInt32(lenBytes, 0);

        //    tempBytes = new byte[len];
        //    Array.Copy(srcBytes, 0x20, tempBytes, 0, len);

        //    byte[] newCrcBytes;
        //    CalcCrc(tempBytes, out newCrcBytes);
        //    int newCrc = BitConverter.ToInt32(newCrcBytes, 0);

        //    // もともと記載のCRC
        //    Array.Copy(srcBytes, 0x10, crcBytes, 0, 4);
        //    int orgCrc = BitConverter.ToInt32(crcBytes, 0);

        //    if (newCrc == orgCrc)
        //    { return true; }
        //    else
        //    { return false; }
        //}

        //private bool checkAgingData(string filePath)
        //{
        //    byte[] srcBytes = File.ReadAllBytes(filePath);

        //    byte[] tempBytes = new byte[115712];
        //    Array.Copy(srcBytes, 0, tempBytes, 0, 115712);

        //    if (compareChecksum(tempBytes, 508) != true)
        //    { return false; }

        //    return true;
        //}

        //private bool checkAgingDataChiron(string filePath)
        //{
        //    byte[] srcBytes = File.ReadAllBytes(filePath);
        //    byte[] tempBytes;
        //    byte[] crcBytes = new byte[4];

        //    // Product Info以降のデータ長を調べる
        //    byte[] lenBytes = new byte[4];
        //    Array.Copy(srcBytes, 0x0c, lenBytes, 0, 4);
        //    int len = BitConverter.ToInt32(lenBytes, 0);

        //    tempBytes = new byte[len];
        //    Array.Copy(srcBytes, 0x20, tempBytes, 0, len);

        //    byte[] newCrcBytes;
        //    CalcCrc(tempBytes, out newCrcBytes);
        //    int newCrc = BitConverter.ToInt32(newCrcBytes, 0);

        //    // もともと記載のCRC
        //    Array.Copy(srcBytes, 0x10, crcBytes, 0, 4);
        //    int orgCrc = BitConverter.ToInt32(crcBytes, 0);

        //    if (newCrc == orgCrc)
        //    { return true; }
        //    else
        //    { return false; }
        //}

        private bool compareChecksum(byte[] dataBytes, int checkSumPos)
        {
            int orgCheckSum = BitConverter.ToInt32(dataBytes, checkSumPos);
            int calcCheckSum = 0;

            for (int i = 0; i < dataBytes.Length; i += 4)
            {
                // CheckSumの部分はSkip
                if(i == checkSumPos)
                { continue; }

                calcCheckSum += BitConverter.ToInt32(dataBytes, i);
            }

            if(orgCheckSum != calcCheckSum)
            { return false; }

            return true;
        }

        private bool reloadDataFile(ControllerInfo cont, List<string> lstFiles, string destDir)
        {
            // NFSドライブのファイル削除
            if (Settings.Ins.TransType == TransType.NFS)
            {
                try
                {
                    string[] deleteFiles = System.IO.Directory.GetFiles(cont.Drive, "*", System.IO.SearchOption.AllDirectories);
                    for (int i = 0; i < deleteFiles.Length; i++)
                    {
                        try { FileSystem.DeleteFile(deleteFiles[i], UIOption.OnlyErrorDialogs, RecycleOption.DeletePermanently); }
                        catch { }
                    }
                }
                catch { }
            }

            // ●Unit Power Off [11]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[11] Cabinet Power Off"); }
            winProgress.ShowMessage("Cabinet Power Off.");

            sendSdcpCommand(SDCPClass.CmdUnitPowerOff, cont.IPAddress);
            System.Threading.Thread.Sleep(10000);
            winProgress.PutForward1Step();

            // ●Reconfig [12]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[12] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            sendReconfig();
            winProgress.PutForward1Step();

            int cnt = 1;
            foreach(string file in lstFiles)
            {
                winProgress.ShowMessage("Reload Files. ( " + cnt + " / " + lstFiles.Count + " )");

                // ●Backupコマンド発行
                byte[] cmd = new byte[SDCPClass.CmdDataRead_UnitData.Length];
                Array.Copy(SDCPClass.CmdDataRead_UnitData, cmd, SDCPClass.CmdDataRead_UnitData.Length);

                if (System.IO.Path.GetFileName(file).Contains("_ud.bin") == true)
                { cmd[cmd.Length - 1] = 1; }
                else if (System.IO.Path.GetFileName(file).Contains("_cd.bin") == true)
                { cmd[cmd.Length - 1] = 2; }
                else if (System.IO.Path.GetFileName(file).Contains("_ad.bin") == true)
                { cmd[cmd.Length - 1] = 4; }
                //else if (System.IO.Path.GetFileName(file).Contains("_ed.bin") == true)
                //{ cmd[cmd.Length - 1] = 8; }

                string strPort = System.IO.Path.GetFileName(file).Substring(1, 2);
                string strUnit = System.IO.Path.GetFileName(file).Substring(3, 2);

                int intPort = Convert.ToInt32(strPort);
                int intUnit = Convert.ToInt32(strUnit);

                strPort = Convert.ToString(intPort, 16);
                strUnit = Convert.ToString(intUnit, 16);

                int num16 = Convert.ToInt32(strPort + strUnit, 16);

                cmd[9] = (byte)num16;

                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[13] Send Backup Command."); }
                sendSdcpCommand(cmd, cont.IPAddress);

                System.Threading.Thread.Sleep(5000);
                
                // ●Complete待ち [5]
                while (true)
                {
                    if (Settings.Ins.TransType == TransType.FTP)
                    {
                    if (checkCompleteFtp(cont.IPAddress, "read_complete") == true)
                    { break; }
                    }
                    else
                    {
                        if (checkComplete(cont.Drive, "read_complete") == true)
                        { break; }
                    }

                    System.Threading.Thread.Sleep(1000);
                }

                cnt++;
            }

            // ●Reconfig [12]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[12] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            sendReconfig();
            winProgress.PutForward1Step();

            // 移動
            if (Settings.Ins.TransType == TransType.FTP)
            {
                List<string> lstFtpFiles;

                if (getFileListFtp(cont.IPAddress, out lstFtpFiles) != true)
                { return false; }

                foreach (string file in lstFtpFiles)
                {
                    string destPath = destDir + file;
                    try
                    {
                        if (File.Exists(destPath) == true)
                        {
                            try { File.Delete(destPath); }
                            catch { }
                        }

                        getFileFtp(cont.IPAddress, destPath);

                        if (confirmChecksum(destPath) != true)
                        { return false; }
                    }
                    catch { return false; }
                }
            }
            else
            {
                string[] files = Directory.GetFiles(cont.Drive, "*", System.IO.SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    try
                    {
                        string destPath = destDir + System.IO.Path.GetFileName(files[i]);

                        if (File.Exists(destPath) == true)
                        {
                            try { File.Delete(destPath); }
                            catch { }
                        }

                        File.Copy(files[i], destPath, true);

                        if (confirmChecksum(destPath) != true)
                        { return false; }
                    }
                    catch { return false; }
                }
            }
            
            return true;
        }

        private void convertRbinToBin(string orgFile, string destDir, bool deleteOrgFile = false, bool isReplaceMode = false) // isReplaceMode = true: ReplaceのRCS Board交換時のみ
        {
            string fileName = Path.GetFileName(orgFile);
            string destFile = destDir + "\\" + Path.GetFileNameWithoutExtension(orgFile) + ".bin";

            byte[] srcBytes = File.ReadAllBytes(orgFile);
            byte[] convBytes = null;

            if (fileName.Contains("_ud.rbin") == true)
            { convWriteDataUnitData(srcBytes, out convBytes); }
            else if (fileName.Contains("_hc.rbin") == true)
            { convWriteDataHcData(srcBytes, out convBytes); }
            else if (fileName.Contains("_lc.rbin") == true)
            { convWriteDataLcData(srcBytes, out convBytes); }
            //else if (fileName.Contains("_ad.rbin") == true)
            //{ convWriteDataAgingData(srcBytes, out convBytes); }
            else
            { throw new Exception("The indicated file is inappropriate."); }
            
            if(isReplaceMode)
            {
                convUnitProductInfoData(convBytes, out convBytes);
                destFile = destDir + "\\" + Path.GetFileNameWithoutExtension(targetPath) + ".bin";
            }
            
            File.WriteAllBytes(destFile, convBytes);

            // OrgFile削除
            if(deleteOrgFile == true)
            { File.Delete(orgFile); }
        }

        private void convWriteDataUnitData(byte[] srcBytes, out byte[] convBytes)
        {
            byte[] crc;

            convBytes = new byte[srcBytes.Length];
            Array.Copy(srcBytes, convBytes, srcBytes.Length);

            // DataTypeの変更
            // Product Info
            // DataType変更
            int intDataType = dt_DummyData; // Dummy
            byte[] dataType = BitConverter.GetBytes(intDataType);
            Array.Copy(dataType, 0, convBytes, ProductInfoDataTypeOffset, ProductInfoDataTypeSize);

            // CRC再計算
            byte[] tempBytes = new byte[ProductInfoHeaderCrcRange];
            Array.Copy(convBytes, ProductInfoAddress, tempBytes, 0, ProductInfoHeaderCrcRange);

            CalcCrc(tempBytes, out crc);
            Array.Copy(crc, 0, convBytes, ProductInfoHeaderCrcOffset, ProductInfoHeaderCrcSize);

            // Input Gamma Table(ID1, 2, 3)
            for (int i = 0; i < InputGammaTableCount; i++)
            {
                // DataType変更
                intDataType = dt_InputGammaWrite;
                dataType = BitConverter.GetBytes(intDataType);
                Array.Copy(dataType, 0, convBytes, UdFirstInputGammaDataTypeOffset + UdInputGammaLength * i, UdInputGammaDataTypeSize);

                // CRC再計算
                tempBytes = new byte[UdInputGammaHeaderCrcRange];
                Array.Copy(convBytes, UdFirstInputGammaAddress + UdInputGammaLength * i, tempBytes, 0, UdInputGammaHeaderCrcRange);

                CalcCrc(tempBytes, out crc);
                Array.Copy(crc, 0, convBytes, UdFirstInputGammaHeaderCrcOffset + UdInputGammaLength * i, UdInputGammaHeaderCrcSize);
            }

            // Output Gamma(ID1, 2, 3, 4, 5, 6)
            for (int i = 0; i < OutputGammaTableCount; i++)
            {
                // DataType変更
                intDataType = dt_OutputGammaWrite;
                dataType = BitConverter.GetBytes(intDataType);
                Array.Copy(dataType, 0, convBytes, UdFirstOutputGammaDataTypeOffset + UdOutputGammaLength * i, UdOutputGammaDataTypeSize); // Output Gammaの最初のData Type

                // CRC再計算
                tempBytes = new byte[UdOutputGammaHeaderCrcRange];
                Array.Copy(convBytes, UdFirstOutputGammaAddress + UdOutputGammaLength * i, tempBytes, 0, UdOutputGammaHeaderCrcRange); // Output Gammaの最初のアドレス

                CalcCrc(tempBytes, out crc);
                Array.Copy(crc, 0, convBytes, UdFirstOutputGammaHeaderCrcOffset + UdOutputGammaLength * i, UdOutputGammaHeaderCrcSize); // Output Gammaの最初のCRC
            }

            // LED Modue Info
            for (int i = 0; i < moduleCount; i++)
            {
                // DataType変更
                intDataType = dt_DummyData;
                dataType = BitConverter.GetBytes(intDataType);
                Array.Copy(dataType, 0, convBytes, UdFirstModuleInfoDataTypeOffset + UdModuleInfoLength * i, UdModuleInfoDataTypeSize); // 一番最初のLED Module InfoのDataTypeの位置

                // CRC再計算
                // Dataは変更しないので割愛

                // Header
                tempBytes = new byte[UdModuleInfoHeaderCrcRange];
                Array.Copy(convBytes, UdFirstModuleInfoAddress + UdModuleInfoLength * i, tempBytes, 0, UdModuleInfoHeaderCrcRange); // 一番最初のLED Module Infoのアドレス

                CalcCrc(tempBytes, out crc);
                Array.Copy(crc, 0, convBytes, UdFirstModuleInfoHeaderCrcOffset + UdModuleInfoLength * i, UdModuleInfoHeaderCrcSize);
            }

            // 全体のCRC再計算
            byte[] dataBytes = new byte[udDataLength];
            Array.Copy(convBytes, ProductInfoAddress, dataBytes, 0, udDataLength);

            CalcCrc(dataBytes, out crc);
            Array.Copy(crc, 0, convBytes, CommonHeaderDataCrcOffset, CommonHeaderDataCrcSize);

            // HeaderのCRC再計算
            dataBytes = new byte[CommonHeaderHeaderCrcRange];
            Array.Copy(convBytes, 0, dataBytes, 0, CommonHeaderHeaderCrcRange);

            CalcCrc(dataBytes, out crc);
            Array.Copy(crc, 0, convBytes, CommonHeaderHeaderCrcOffset, CommonHeaderHeaderCrcSize);
        }

        private void convWriteDataHcData(byte[] srcBytes, out byte[] convBytes)
        {
            byte[] crc;

            convBytes = new byte[srcBytes.Length];
            Array.Copy(srcBytes, convBytes, srcBytes.Length);

            // DataTypeの変更
            // Product Info
            // DataType変更
            uint uintDataType = dt_DummyData; // Dummy
            byte[] dataType = BitConverter.GetBytes(uintDataType);
            Array.Copy(dataType, 0, convBytes, ProductInfoDataTypeOffset, ProductInfoDataTypeSize);

            // Data Sizeを変更
            //byte[] sizeBytes = BitConverter.GetBytes(CdDataLength);
            //Array.Copy(sizeBytes, 0, convBytes, 12, 4);

            // CRC再計算
            byte[] tempBytes = new byte[ProductInfoHeaderCrcRange];
            Array.Copy(convBytes, ProductInfoAddress, tempBytes, 0, ProductInfoHeaderCrcRange);

            CalcCrc(tempBytes, out crc);
            Array.Copy(crc, 0, convBytes, ProductInfoHeaderCrcOffset, ProductInfoHeaderCrcSize);

            // Color Correction
            for (int i = 0; i < moduleCount; i++)
            {
                // DataType変更
                uintDataType = dt_ColorCorrectionWrite; // Color Correction (Write)
                dataType = BitConverter.GetBytes(uintDataType);
                Array.Copy(dataType, 0, convBytes, HcFirstCcDataTypeOffset + hcModuleDataLength * i, HcCcDataTypeSize); // 一番最初のColor CorrectionのDataTypeの位置

                // CRC再計算
                // Data
                tempBytes = new byte[hcCcDataLength];
                Array.Copy(convBytes, HcFirstCcDataOffset + hcModuleDataLength * i, tempBytes, 0, hcCcDataLength);

                CalcCrc(tempBytes, out crc);
                Array.Copy(crc, 0, convBytes, HcFirstCcDataCrcOffset + hcModuleDataLength * i, HcCcDataCrcSize);

                // Header
                tempBytes = new byte[HcCcDataHeaderCrcRange];
                Array.Copy(convBytes, HcFirstCcAddress + hcModuleDataLength * i, tempBytes, 0, HcCcDataHeaderCrcRange);

                CalcCrc(tempBytes, out crc);
                Array.Copy(crc, 0, convBytes, HcFirstCcDataHeaderCrcOffset + hcModuleDataLength * i, HcCcDataHeaderCrcSize);
            }

            // 全体のCRC再計算
            byte[] dataBytes = new byte[hcDataLength];
            Array.Copy(convBytes, ProductInfoAddress, dataBytes, 0, hcDataLength);

            CalcCrc(dataBytes, out crc);
            Array.Copy(crc, 0, convBytes, CommonHeaderDataCrcOffset, CommonHeaderDataCrcSize);

            // HeaderのCRC再計算
            dataBytes = new byte[CommonHeaderHeaderCrcRange];
            Array.Copy(convBytes, 0, dataBytes, 0, CommonHeaderHeaderCrcRange);

            CalcCrc(dataBytes, out crc);
            Array.Copy(crc, 0, convBytes, CommonHeaderHeaderCrcOffset, CommonHeaderHeaderCrcSize);
           
        }

        private void convWriteDataLcData(byte[] srcBytes, out byte[] convBytes)
        {
            byte[] crc;

            convBytes = new byte[srcBytes.Length];
            Array.Copy(srcBytes, convBytes, srcBytes.Length);
                        
            // DataTypeの変更
            // Product Info
            // DataType変更
            int intDataType = dt_DummyData; // Dummy
            byte[] dataType = BitConverter.GetBytes(intDataType);
            Array.Copy(dataType, 0, convBytes, ProductInfoDataTypeOffset, ProductInfoDataTypeSize);

            // Data Sizeを変更
            //byte[] sizeBytes = BitConverter.GetBytes(CdDataLength);
            //Array.Copy(sizeBytes, 0, convBytes, 12, 4);

            // CRC再計算
            byte[] tempBytes = new byte[ProductInfoHeaderCrcRange];
            Array.Copy(convBytes, ProductInfoAddress, tempBytes, 0, ProductInfoHeaderCrcRange);

            CalcCrc(tempBytes, out crc);
            Array.Copy(crc, 0, convBytes, ProductInfoHeaderCrcOffset, ProductInfoHeaderCrcSize);
           
            string ledModel = allocInfo.LEDModel;
            if (ledModel == ZRD_B12A || ledModel == ZRD_B15A || ledModel == ZRD_C12A || ledModel == ZRD_C15A)
            {
                for (int i = 0; i < moduleCount; i++)
                {
                    // FCCL(Normal)
                    // DataType変更
                    intDataType = dt_FcclWrite; // FCCL (Write)
                    dataType = BitConverter.GetBytes(intDataType);
                    Array.Copy(dataType, 0, convBytes, LcFirstModuleDataTypeOffset + lcFcclModuleDataLength * i, LcFirstModuleDataTypeSize); // 328 = 一番最初のFCCLのDataTypeの位置

                    // CRC再計算
                    // Data
                    //tempBytes = new byte[lcFcclDataLength];
                    //Array.Copy(convBytes, LcFirstModuleDataOffset + lcFcclModuleDataLength * i, tempBytes, 0, lcFcclDataLength);

                    //CalcCrc(tempBytes, out crc);
                    //Array.Copy(crc, 0, convBytes, LcFirstModuleDataCrcOffset + lcFcclModuleDataLength * i, LcFirstModuleDataCrcSize);

                    // Header CRC
                    tempBytes = new byte[LcFirstModuleDataHeaderCrcRange]; // 28 : Signature～Option 2までの長さ
                    Array.Copy(convBytes, LcFirstModuleAddress + lcFcclModuleDataLength * i, tempBytes, 0, LcFirstModuleDataHeaderCrcRange);

                    CalcCrc(tempBytes, out crc);
                    Array.Copy(crc, 0, convBytes, LcFirstModuleDataHeaderCrcOffset + lcFcclModuleDataLength * i, LcFirstModuleDataHeaderCrcSize);
                }

            }
            else // ZRD-BH12D, ZRD-BH15D, ZRD-CH12D, ZRD-CH15D, ZRD-BH12D_S3, ZRD-BH15D_S3, ZRD-CH12D_S3, ZRD-CH15D_S3
            {
                int lcFirstLcalVModuleDataTypeOffset = LcFirstModuleAddress + 8;
                int lcFirstLcalHModuleDataTypeOffset = LcFirstModuleAddress + (lcLcalVModuleDataLength * moduleCount) + 8;
                int lcFirstLmcalModuleDataTypeOffset = LcFirstModuleAddress + (lcLcalVModuleDataLength * moduleCount) + (lcLcalHModuleDataLength * moduleCount) + 8;
                int lcFirstMgamModuleDataTypeOffset = LcFirstModuleAddress + (lcLcalVModuleDataLength * moduleCount) + (lcLcalHModuleDataLength * moduleCount) + (lcLmcalModuleDataLength * moduleCount) + 8;
                int lcFirstLcalHModuleDataHeaderCrcOffset = LcFirstModuleAddress + (lcLcalVModuleDataLength * moduleCount) + 28;
                int lcFirstLmcalModuleDataHeaderCrcOffset = LcFirstModuleAddress + (lcLcalVModuleDataLength * moduleCount) + (lcLcalHModuleDataLength * moduleCount) + 28;
                int lcFirstMgamModuleDataHeaderCrcOffset = LcFirstModuleAddress + (lcLcalVModuleDataLength * moduleCount) + (lcLcalHModuleDataLength * moduleCount) + (lcLmcalModuleDataLength * moduleCount) + 28;
                int lcHeaderCrcRange = 28;
                int dataLength = 4;

                for (int i = 0; i < moduleCount; i++)
                {
                    // Low Calibration V
                    // DataType変更
                    intDataType = dt_LcalVWrite;
                    dataType = BitConverter.GetBytes(intDataType);
                    Array.Copy(dataType, 0, convBytes, lcFirstLcalVModuleDataTypeOffset + (lcLcalVModuleDataLength * i), dataLength); // 328 = 一番最初のLCVのDataTypeの位置

                    // Header CRC
                    tempBytes = new byte[lcHeaderCrcRange]; // 28 : Signature～Option 2までの長さ
                    Array.Copy(convBytes, LcFirstModuleAddress + (lcLcalVModuleDataLength * i), tempBytes, 0, lcHeaderCrcRange);

                    CalcCrc(tempBytes, out crc);
                    Array.Copy(crc, 0, convBytes, LcFirstModuleDataHeaderCrcOffset + (lcLcalVModuleDataLength * i), dataLength);

                    // Low Calibration H
                    // DataType変更
                    intDataType = dt_LcalHWrite;
                    dataType = BitConverter.GetBytes(intDataType);
                    Array.Copy(dataType, 0, convBytes, lcFirstLcalHModuleDataTypeOffset + (lcLcalHModuleDataLength * i), dataLength); // 一番最初のLCHのDataTypeの位置

                    // Header CRC
                    tempBytes = new byte[lcHeaderCrcRange]; // 28 : Signature～Option 2までの長さ
                    Array.Copy(convBytes, LcFirstModuleAddress + (lcLcalVModuleDataLength * moduleCount) + (lcLcalHModuleDataLength * i), tempBytes, 0, lcHeaderCrcRange);

                    CalcCrc(tempBytes, out crc);
                    Array.Copy(crc, 0, convBytes, lcFirstLcalHModuleDataHeaderCrcOffset + (lcLcalHModuleDataLength * i), dataLength);

                    // Lower-Mid Calibration
                    // DataType変更
                    intDataType = dt_LmcalWrite;
                    dataType = BitConverter.GetBytes(intDataType);
                    Array.Copy(dataType, 0, convBytes, lcFirstLmcalModuleDataTypeOffset + (lcLmcalModuleDataLength * i), dataLength);

                    // Header CRC
                    tempBytes = new byte[lcHeaderCrcRange];
                    Array.Copy(convBytes, LcFirstModuleAddress + (lcLcalVModuleDataLength * moduleCount) + (lcLcalHModuleDataLength * moduleCount) + (lcLmcalModuleDataLength * i), tempBytes, 0, lcHeaderCrcRange);

                    CalcCrc(tempBytes, out crc);
                    Array.Copy(crc, 0, convBytes, lcFirstLmcalModuleDataHeaderCrcOffset + (lcLmcalModuleDataLength * i), dataLength);

                    // LED Module Gamma
                    // DataType変更
                    intDataType = dt_MgamWrite;
                    dataType = BitConverter.GetBytes(intDataType);
                    Array.Copy(dataType, 0, convBytes, lcFirstMgamModuleDataTypeOffset + (lcMgamModuleDataLength * i), dataLength);

                    // Header CRC
                    tempBytes = new byte[lcHeaderCrcRange];
                    Array.Copy(convBytes, LcFirstModuleAddress + (lcLcalVModuleDataLength * moduleCount) + (lcLcalHModuleDataLength * moduleCount) + (lcLmcalModuleDataLength * moduleCount) + (lcMgamModuleDataLength * i), tempBytes, 0, lcHeaderCrcRange);

                    CalcCrc(tempBytes, out crc);
                    Array.Copy(crc, 0, convBytes, lcFirstMgamModuleDataHeaderCrcOffset + (lcMgamModuleDataLength * i), dataLength);
                }
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
        }

        private void convWriteDataAgingData(byte[] srcBytes, out byte[] convBytes)
        {
            convBytes = new byte[srcBytes.Length];
            Array.Copy(srcBytes, convBytes, srcBytes.Length);

            // DataTypeの変更
            // Product Info
            // DataType変更
            int intDataType = dt_DummyData; // Dummy
            byte[] dataType = BitConverter.GetBytes(intDataType);

            Array.Copy(dataType, 0, convBytes, 40, 4);

            // CRC再計算
            byte[] tempBytes = new byte[28];
            Array.Copy(convBytes, 32, tempBytes, 0, 28);

            uint crc32 = new CRC32().Calc(tempBytes);
            crc32 = crc32 ^ 0xFFFFFFFF;
            byte[] crc = BitConverter.GetBytes(crc32);

            Array.Copy(crc, 0, convBytes, 60, 4);

            //// Color Correction(Moduleごとに同じものが8個)
            //for (int i = 0; i < 8; i++)
            //{
            //    // DataType変更
            //    intDataType = 0x00001101; // Color Correction (Write)
            //    dataType = BitConverter.GetBytes(intDataType);

            //    Array.Copy(dataType, 0, convertedBytes, 328 + 129888 * i, 4); // 328 = 一番最初のColor CorrectionのDataTypeの位置, 129888 = 1ModuleあたりのByte数

            //    // CRC再計算
            //    // Data

            //    // Header
            //    tempBytes = new byte[28];
            //    Array.Copy(convertedBytes, 320 + 129888 * i, tempBytes, 0, 28);

            //    crc32 = new CRC32().Calc(tempBytes);
            //    crc32 = crc32 ^ 0xFFFFFFFF;
            //    crc = BitConverter.GetBytes(crc32);

            //    Array.Copy(crc, 0, convertedBytes, 348 + 129888 * i, 4);
            //}

            //// 全体のCRC再計算
            //byte[] dataBytes = new byte[LcDataLength];
            //Array.Copy(convBytes, 0x20, dataBytes, 0, LcDataLength);

            //CalcCrc(dataBytes, out crc);
            //Array.Copy(crc, 0, convBytes, 0x10, 4);

            //// HeaderのCRC再計算
            //dataBytes = new byte[28];
            //Array.Copy(convBytes, 0, dataBytes, 0, 28);

            //CalcCrc(dataBytes, out crc);
            //Array.Copy(crc, 0, convBytes, 0x1C, 4);
        }

        private void convWriteNewHcData(List<CompositeConfigData> newHcData, byte[] srcBytes, out byte[] convBytes, List<int> lstTargetModule = null)
        {
            byte[] headerBytes, bodyBytes, dataType, crc;
            uint uintDataType;

            convBytes = new byte[srcBytes.Length];
            Array.Copy(srcBytes, convBytes, srcBytes.Length);

            foreach (int i in lstTargetModule)
            {
                // Header構造体(struct)をバイト配列(byte[])に変換 [Signature~Option 2]
                headerBytes = new byte[HcCcDataHeaderCrcRange];
                IntPtr ptr = Marshal.AllocHGlobal(HcCcDataHeaderCrcRange);
                Marshal.StructureToPtr(newHcData[i].header, ptr, false);
                Marshal.Copy(ptr, headerBytes, 0, HcCcDataHeaderCrcRange);
                Marshal.FreeHGlobal(ptr);

                bodyBytes = newHcData[i].data;

                // DataType変更
                uintDataType = dt_ColorCorrectionWrite; // Color Correction (Write)
                dataType = BitConverter.GetBytes(uintDataType);
                Array.Copy(dataType, 0, headerBytes, HcCcDataTypeOffset, dataType.Length);

                // Option2変更
                uint uintOpCurrent = op2_Current; // Option2
                byte[] opCurrent = BitConverter.GetBytes(uintOpCurrent);
                Array.Copy(opCurrent, 0, headerBytes, HcCcDataOption2Offset, opCurrent.Length);

                // Data CRC再計算
                Array.Copy(bodyBytes, 0, convBytes, HcFirstCcDataOffset + (hcModuleDataLength * i), bodyBytes.Length);

                CalcCrc(bodyBytes, out crc);
                Array.Copy(crc, 0, convBytes, HcFirstCcDataCrcOffset + (hcModuleDataLength * i), crc.Length);

                // HeaderのCRC再計算
                Array.Copy(headerBytes, 0, convBytes, HcFirstCcAddress + (hcModuleDataLength * i), headerBytes.Length);

                CalcCrc(headerBytes, out crc);
                Array.Copy(crc, 0, convBytes, HcFirstCcDataHeaderCrcOffset + (hcModuleDataLength * i), crc.Length);
            }

            // 全体のCRC再計算
            byte[] dataBytes = new byte[hcDataLength];
            Array.Copy(convBytes, ProductInfoAddress, dataBytes, 0, hcDataLength);

            CalcCrc(dataBytes, out crc);
            Array.Copy(crc, 0, convBytes, CommonHeaderDataCrcOffset, CommonHeaderDataCrcSize);

            // HeaderのCRC再計算
            dataBytes = new byte[CommonHeaderHeaderCrcRange];
            Array.Copy(convBytes, 0, dataBytes, 0, CommonHeaderHeaderCrcRange);

            CalcCrc(dataBytes, out crc);
            Array.Copy(crc, 0, convBytes, CommonHeaderHeaderCrcOffset, CommonHeaderHeaderCrcSize);
        }

        public static void CalcCrc(byte[] data, out byte[] crc)
        {
            uint crc32 = new CRC32().Calc(data);
            crc32 = crc32 ^ 0xFFFFFFFF;
            crc = BitConverter.GetBytes(crc32);
        }

        private void convUnitProductInfoData(byte[] convOrgBytes, out byte[] convBytes)
        {
            byte[] crc, tempBytes;

            convBytes = new byte[convOrgBytes.Length];
            Array.Copy(convOrgBytes, convBytes, convOrgBytes.Length);

            if(changedCabiData)
            {
                // Model Name
                Array.Copy(cabiModelName, 0, convBytes, ProductInfoDataModelNameOffset, ProductInfoDataModelNameSize);

                // Serial Number
                Array.Copy(cabiSerialNo, 0, convBytes, ProductInfoDataSerialNoOffset, ProductInfoDataSerialNoSize);

                // Product Info DataのCRC再計算
                tempBytes = new byte[ProductInfoDataLength];
                Array.Copy(convBytes, ProductInfoDataAddress, tempBytes, 0, ProductInfoDataLength);

                CalcCrc(tempBytes, out crc);
                Array.Copy(crc, 0, convBytes, ProductInfoDataCrcOffset, ProductInfoDataCrcSize);
            }

            // DataType変更
            int intDataType = dt_ProductInfoWrite; // Write
            byte[] dataType = BitConverter.GetBytes(intDataType);
            Array.Copy(dataType, 0, convBytes, ProductInfoDataTypeOffset, ProductInfoDataTypeSize);

            // HeaderのCRC再計算
            tempBytes = new byte[ProductInfoHeaderCrcRange];
            Array.Copy(convBytes, ProductInfoAddress, tempBytes, 0, ProductInfoHeaderCrcRange);

            CalcCrc(tempBytes, out crc);
            Array.Copy(crc, 0, convBytes, ProductInfoHeaderCrcOffset, ProductInfoHeaderCrcSize);

            // 全体のCRC再計算
            byte[] dataBytes = new byte[udDataLength];
            Array.Copy(convBytes, ProductInfoAddress, dataBytes, 0, udDataLength);

            CalcCrc(dataBytes, out crc);
            Array.Copy(crc, 0, convBytes, CommonHeaderDataCrcOffset, CommonHeaderDataCrcSize);

            // HeaderのCRC再計算
            dataBytes = new byte[CommonHeaderHeaderCrcRange];
            Array.Copy(convBytes, 0, dataBytes, 0, CommonHeaderHeaderCrcRange);

            CalcCrc(dataBytes, out crc);
            Array.Copy(crc, 0, convBytes, CommonHeaderHeaderCrcOffset, CommonHeaderHeaderCrcSize);
        }

        private int calcBackupDataMoveSec(int cabinetCount, bool unitData, bool hcData, bool lcData)
        {
            int dataMoveSec;

            //係数　データ移動時間
            double u3 = 0.17;
            double h3 = 0.52;
            double l3 = 0.19;

            dataMoveSec = (int)(Convert.ToDouble(unitData) * (cabinetCount * u3)
                + Convert.ToDouble(hcData) * (cabinetCount * h3) + Convert.ToDouble(lcData) * (cabinetCount * l3));

            return dataMoveSec;
        }

        private int calcBackupResponseSec(int maxConnectCabinet, bool unitData, bool hcData, bool lcData)
        {
            int responseSec;

            //係数　レスポンス時間 ファイルの種類ごとに定義
            double u1 = 1.46;
            double h1 = 12.69;
            double l1 = 2.89;
            double u2 = 3.31;
            double h2 = 2.54;
            double l2 = 0.50;

            //補正係数　キャビネット台数によって増加する1ファイルの処理時間を補正
            double s1 = 0.018;

            //係数　ファイルの種類と無関係なレスポンス時間
            double t2 = 20.73;


            responseSec = (int)(Convert.ToDouble(unitData) * (maxConnectCabinet * (u1 + maxConnectCabinet * s1) + u2)
                + Convert.ToDouble(hcData) * (maxConnectCabinet * (h1 + maxConnectCabinet * s1 * 2) + h2)   //hc,hf2ファイルなので補正量2倍
                +Convert.ToDouble(lcData) * (maxConnectCabinet * (l1 + +maxConnectCabinet * s1) + l2)
                + t2);

            return responseSec;
        }
        private int calcBackupProcessSec(int step,bool powerWait,int responseSec,int dataMoveSec)
        {
            int processSec = 0;
            //係数　データ移動,レスポンス処理,コントローラー台数に影響する時間を除いた合計処理時間
            //commonA[0]  [0] backup start
            //commonA[1]  [1] Move Old Backup Folder.
            //commonA[2]  [2] Check Controller Power.
            //commonA[3]  [*1] FTP On [*2] Get Model Name. [*3] Get Serial No.
            //commonA[4]  [3] Cabinet Power Off
            //commonA[5]  [4] Send Reconfig.
            //commonA[6]  [5] Send Backup Command.
            //            [6] Waiting for the response.	
            //commonA[7]  [7] Send Reconfig.
            //            [8] Move Files.
            //commonA[8] [9] FTP Off.
            //commonA[9] [10] Cabinet Power On.
            int[] commonA = new int[10] { 0, 0, 1, 0, 1, 12, 0, 13, 0, 0 };

            //係数　コントローラー台数に影響のある各種処理時間
            //contB[0]  [0] backup start
            //contB[1]  [1] Move Old Backup Folder.
            //contB[2]  [2] Check Controller Power.
            //contB[3]  [*1] FTP On [*2] Get Model Name. [*3] Get Serial No.
            //contB[4]  [3] Cabinet Power Off
            //contB[5]  [4] Send Reconfig.
            //contB[6]  [5] Send Backup Command.
            //          [6] Waiting for the response.	
            //contB[7]  [7] Send Reconfig.
            //          [8] Move Files.
            //contB[8] [9] FTP Off.
            //contB[9] [10] Cabinet Power On.
            int[] contB = new int[10] { 0, 0, 1, 3, 3, 2, 1, 3, 1, 0 };

            switch (step)
            {
                case 0: //[1] Move Old Backup Folder. 以下の処理時間全体を計算
                case 1: //[2] Check Controller Power.
                    processSec = commonA.Skip(step + 1).Sum() + dicController.Count * contB.Skip(step + 1).Sum()
                        + (Convert.ToInt32(powerWait) * 60) + responseSec + dataMoveSec;
                    break;
                case 2: //[*1] FTP On [*2] Get Model Name. [*3] Get Serial No.
                case 3: //[3] Cabinet Power Off
                case 4: //[4] Send Reconfig.
                case 5: //[5] Send Backup Command.
                case 6: //[6] Waiting for the response.	
                    processSec = commonA.Skip(step + 1).Sum() + dicController.Count * contB.Skip(step + 1).Sum()
                        + responseSec + dataMoveSec;
                    break;
                case 7: //[7] Send Reconfig.
                case 8: //[8] Move Files.
                    processSec = commonA.Skip(step + 1 - 1).Sum() + dicController.Count * contB.Skip(step + 1 - 1).Sum()
                        + dataMoveSec;
                    break;
                case 9: //[9] FTP Off.
                case 10: //[10] Cabinet Power On.
                    processSec = commonA.Skip(step + 1 - 2).Sum() + dicController.Count * contB.Skip(step + 1 - 2).Sum();
                    break;
                default:
                    break;
            }

            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("calcBackupProcessSec step:" + step + " powerWait:" + powerWait + " responseSec:" + responseSec +  " dataMoveSec:" + dataMoveSec + " processSec:" + processSec); }
            return processSec;
        }
        #endregion Backup

        #region Restore

        private bool restore()
        {
            if (Settings.Ins.ExecLog == true)
            {
                SaveExecLog("");
                SaveExecLog("[0] restore start");
            }

            bool status;
            List<ControllerInfo> lstTargetController = new List<ControllerInfo>();
            List<UnitInfo> lstTargetUnit = new List<UnitInfo>();
            List<string> lstDataSizeOkFiles = new List<string>();
            List<string> lstDataSizeNgFiles = new List<string>();
            List<string> lstUploadedFiles = new List<string>();
            List<string> lstSkippedFiles = new List<string>();
            FileDirectory baseFileDir = FileDirectory.NA;
            bool unitData = false, hcData = false, lcData = false;
            var dispatcher = Application.Current.Dispatcher;

            // ●進捗の最大値を設定
            winProgress.ShowMessage("Set Step Count.");
            int step = RESTORE_STEPS;
            winProgress.SetWholeSteps(step);

            // ターゲットフォルダを格納
            dispatcher.Invoke(() => storeBaseDir(out baseFileDir));

            int selectedCabinetCount = 0;
            if (baseFileDir != FileDirectory.Temp) // Tempの場合はTempフォルダ内のファイルすべてでRestoreする
            {
                // ●調整をするCabinetをListに格納 [1]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[1] Store Target Cabinet(s) Info."); }
                winProgress.ShowMessage("Store Target Cabinet(s) Info.");

                dispatcher.Invoke(() => correctDataUnit(out lstTargetUnit, allocInfo.MaxX, allocInfo.MaxY));
                if (lstTargetUnit.Count == 0)
                {
                    ShowMessageWindow("Please select cabinets.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }
                selectedCabinetCount = lstTargetUnit.Count;
                winProgress.PutForward1Step();

                // ●調整をするCabinetが接続されているController格納 [2]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[2] Store Target Controller(s) Info."); }
                winProgress.ShowMessage("Store Target Controller(s) Info.");

                // 初期化
                foreach (ControllerInfo controller in dicController.Values)
                { controller.Target = false; }

                foreach (UnitInfo unit in lstTargetUnit)
                {
                    if (lstTargetController.Contains(dicController[unit.ControllerID]) != true)
                    {
                        dicController[unit.ControllerID].Target = true;
                        lstTargetController.Add(dicController[unit.ControllerID]);
                    }
                }
                winProgress.PutForward1Step();

                // ●調整データのバックアップがすべてあるか確認 [3]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[3] Check Backup Data."); }
                winProgress.ShowMessage("Check Backup Data.");

                dispatcher.Invoke(() => storeCheckBoxStatusRestore(out unitData, out hcData, out lcData));
                if (unitData == false && hcData == false && lcData == false)
                {
                    ShowMessageWindow("Please select target data.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }

                status = checkDataFile(lstTargetUnit, baseFileDir, unitData, hcData, lcData);
                if (status != true)
                {
                    ShowMessageWindow("There is not the data file(s).", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }
                winProgress.PutForward1Step();
            }
            else
            {
                // Tempの場合はすべてのControllerがTargetになる
                foreach (ControllerInfo controller in dicController.Values)
                {
                    controller.Target = true;
                    lstTargetController.Add(controller);
                }
                //Temp以外を選択した場合と同ステップ数になるように調整
                winProgress.PutForward1Step();
                winProgress.PutForward1Step();
                winProgress.PutForward1Step();
            }

            //推定処理時間
            int processSec = 0;

            int currentStep = 0;
            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));

            PowerStatus power;
            bool powerUpWait = false;   //poweronでないコントローラーが存在するかどうか

            foreach (ControllerInfo cont in dicController.Values)
            {
                if (getPowerStatus(cont.IPAddress, out power) != true)
                { return false; }

                if (power != PowerStatus.Power_On)
                {
                    powerUpWait = true;
                    break;
                }
            }

            int dataMoveSec = 0;
            int responseSec = 0;
            int maxConnectCabinet = 0;

            if (baseFileDir == FileDirectory.Temp)
            {

                foreach (ControllerInfo cont in dicController.Values)
                {
                    if (maxConnectCabinet < cont.UnitCount)
                    { maxConnectCabinet = cont.UnitCount; }
                }

                foreach (ControllerInfo controller in dicController.Values)
                {
                    int hcFileCount = 0;
                    int lcFileCount = 0;
                    int udFileCount = 0;
                    List<int> maxResponseSec = new List<int>();
                    List<string> lstCabinet = new List<string>();

                    // ●Model名取得
                    if (string.IsNullOrWhiteSpace(controller.ModelName) == true)
                    { controller.ModelName = getModelName(controller.IPAddress); }

                    // ●Serial取得
                    if (string.IsNullOrWhiteSpace(controller.SerialNo) == true)
                    { controller.SerialNo = getSerialNo(controller.IPAddress); }

                    string tempPath = applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo + "_Temp";

                    if (Directory.Exists(tempPath) != true)
                    { continue; }

                    string[] files = Directory.GetFiles(tempPath, "*", System.IO.SearchOption.AllDirectories);
                    for (int i = 0; i < files.Length; i++)
                    {
                        string file = files[i];
                        if (file.Contains("ud.bin") || file.Contains("hc.bin") || file.Contains("lc.bin"))
                        {
                            string fileName = Path.GetFileNameWithoutExtension(file).Substring(0, 5);
                            if (lstCabinet.Contains(fileName) != true)
                            { lstCabinet.Add(fileName); }
                        }

                        if (file.Contains("ud.bin"))
                        { udFileCount++; }

                        if (file.Contains("hc.bin"))
                        { hcFileCount++; }

                        if (file.Contains("lc.bin"))
                        { lcFileCount++; }
                    }

                    dataMoveSec += calcRestoreDataMoveSecForTemp(udFileCount, hcFileCount, lcFileCount);
                    int targetControllerResponseSec = calcRestoreResponseSecForTemp(maxConnectCabinet, udFileCount, hcFileCount, lcFileCount);
                    if (responseSec < targetControllerResponseSec)
                    { responseSec = targetControllerResponseSec; }
                }
            }
            else // Tempフォルダ以外
            {
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

                dataMoveSec = calcRestoreDataMoveSec(selectedCabinetCount, unitData, hcData, lcData);
                responseSec = calcRestoreResponseSec(maxConnectCabinet, unitData, hcData, lcData);
            }
            
            processSec = calcRestoreProcessSec(currentStep, lstTargetController.Count, powerUpWait, dataMoveSec, responseSec);

            winProgress.StartRemainTimer(processSec);

            // ●Power OnでないときはOnにする [4]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[4] Check Controller Power."); }
            winProgress.ShowMessage("Check Controller Power.");

            if (!setAllControllerPowerOn())
            { return false; }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcRestoreProcessSec(currentStep, lstTargetController.Count, powerUpWait, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            foreach (ControllerInfo controller in lstTargetController)
            {
                // ●FTP ON [*1]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*1] FTP On."); }
                winProgress.ShowMessage("FTP On.");

                if (sendSdcpCommand(SDCPClass.CmdFtpOn, controller.IPAddress) != true)
                { return false; }

                // ●Model名取得 [*2]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*2] Get Model Name."); }
                winProgress.ShowMessage("Get Model Name.");

                if (string.IsNullOrWhiteSpace(controller.ModelName) == true)
                { controller.ModelName = getModelName(controller.IPAddress); }

                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("     Model Name : " + controller.ModelName); }

                // ●Serial取得 [*3]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*3] Get Serial No."); }
                winProgress.ShowMessage("Get Serial No.");

                if (string.IsNullOrWhiteSpace(controller.SerialNo) == true)
                { controller.SerialNo = getSerialNo(controller.IPAddress); }

                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("     Serial Num. : " + controller.SerialNo); }

                // ●Tempフォルダ内のファイルを削除 [*4]
                if (baseFileDir == FileDirectory.Backup_Previous || baseFileDir == FileDirectory.Backup_Initial)
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
                        catch { return false; }
                    }
                }
            }

            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcRestoreProcessSec(currentStep, lstTargetController.Count, powerUpWait, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            // ●ファイル移動 [5]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[5] Move File(s)."); }
            winProgress.ShowMessage("Move File(s).");

            if (baseFileDir == FileDirectory.Temp)
            {
                // Tempの場合は無条件にすべてのファイルを移動する
                foreach (ControllerInfo controller in lstTargetController)
                {
                    string tempDirPath = applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo + "_Temp\\";

                    // Tempフォルダがない場合作成する
                    if (Directory.Exists(tempDirPath) != true)
                    {
                        try
                        {
                            controller.Target = false;
                            Directory.CreateDirectory(tempDirPath);
                        }
                        catch
                        {
                            string msg = "Failed to create directory.";
                            ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 210);
                        }
                    }
                    else
                    {
                        string[] files = Directory.GetFiles(tempDirPath);
                        for (int i = 0; i < files.Length; i++)
                        {
                            string fileName = System.IO.Path.GetFileName(files[i]);
                            if (fileName.Contains("_ud.bin") || fileName.Contains("_hc.bin") || fileName.Contains("_lc.bin"))
                            {
                                if (checkAllDataLength(files[i]))
                                { lstDataSizeOkFiles.Add(files[i]); }
                                else
                                { lstDataSizeNgFiles.Add(files[i]); }
                            }
                        }
                    }
                }

                if (lstDataSizeNgFiles.Count > 0)
                {
                    string msg = "The following files are different in size.\r\nDo you want to restore these files?\r\n";

                    foreach (string str in lstDataSizeNgFiles)
                    { msg += str + "\r\n"; }

                    bool? result = false;
                    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 400, 210, "Yes", "No");
                    if (result == true)
                    {
                        lstDataSizeNgFiles.Clear();
                        restoreFtpUploadFileSelection(lstTargetController, lstDataSizeOkFiles, out lstUploadedFiles, out lstSkippedFiles, false);
                    }
                    else
                    { restoreFtpUploadFileSelection(lstTargetController, lstDataSizeOkFiles, out lstUploadedFiles, out lstSkippedFiles); }
                }
                else
                { restoreFtpUploadFileSelection(lstTargetController, lstDataSizeOkFiles, out lstUploadedFiles, out lstSkippedFiles); }

                // Restore処理行う対象Controllerがない場合、処理中止
                if (lstTargetController.Count(controller => controller.Target) == 0)
                {
                    string msg;

                    if ((lstDataSizeNgFiles.Count + lstSkippedFiles.Count) > 0)
                    { msg = "Failed to restore " + (lstDataSizeNgFiles.Count + lstSkippedFiles.Count) + " data(s).\r\nYou will find the data files which are failed to be restored, in each '*Temp' folder under the 'Backup' folder.\r\n\r\n"; }
                    else
                    { msg = "The data to be restored was not found."; }
                    
                    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 210);
                    return false;
                }

                // Restore処理行う対象Controllerのリストから対象外のControllerを削除
                for (int i = 0; i < lstTargetController.Count; i++)
                {
                    if (lstTargetController[i].Target != true)
                    { lstTargetController.Remove(lstTargetController[i]); }
                }
            }
            else
            {
                foreach (UnitInfo unit in lstTargetUnit)
                {
                    string srcFilePath = applicationPath + "\\Backup\\" + dicController[unit.ControllerID].ModelName + "_" + dicController[unit.ControllerID].SerialNo;
                    string tempFilePath = applicationPath + "\\Backup\\" + dicController[unit.ControllerID].ModelName + "_" + dicController[unit.ControllerID].SerialNo + "_Temp\\";

                    if (baseFileDir == FileDirectory.Backup_Initial)
                    { srcFilePath += "_Initial\\"; }
                    else if (baseFileDir == FileDirectory.Backup_Previous)
                    { srcFilePath += "_Previous\\"; }
                    else
                    { srcFilePath += "_Temp\\"; }

                    // ファイル移動
                    if (unitData == true)
                    {
                        string fileName = "u" + unit.PortNo.ToString("D2") + unit.UnitNo.ToString("D2") + "_ud.bin";

                        try
                        {
                            if (putFileFtpRetry(dicController[unit.ControllerID].IPAddress, srcFilePath + fileName) != true)
                            {
                                ShowMessageWindow("Failed in moving cabinet data file.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                                return false;
                            }

                            // Previous, InitialからのRestore時はTempフォルダにもコピー
                            if (baseFileDir == FileDirectory.Backup_Previous || baseFileDir == FileDirectory.Backup_Initial)
                            { System.IO.File.Copy(srcFilePath + fileName, tempFilePath + fileName); }
                        }
                        catch
                        {
                            ShowMessageWindow("Failed in moving cabinet data file.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                            return false;
                        }
                    }

                    if (hcData == true)
                    {
                        string fileName = "u" + unit.PortNo.ToString("D2") + unit.UnitNo.ToString("D2") + "_hc.bin";

                        try
                        {
                            if (putFileFtpRetry(dicController[unit.ControllerID].IPAddress, srcFilePath + fileName) != true)
                            {
                                ShowMessageWindow("Failed in moving uniformity high data file.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                                return false;
                            }

                            // Previous, InitialからのRestore時はTempフォルダにもコピー
                            if (baseFileDir == FileDirectory.Backup_Previous || baseFileDir == FileDirectory.Backup_Initial)
                            { System.IO.File.Copy(srcFilePath + fileName, tempFilePath + fileName); }
                        }
                        catch
                        {
                            ShowMessageWindow("Failed in moving uniformity high data file.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                            return false;
                        }
                    }

                    if (lcData == true)
                    {
                        string fileName = "u" + unit.PortNo.ToString("D2") + unit.UnitNo.ToString("D2") + "_lc.bin";
                       
                        try
                        {
                            if (putFileFtpRetry(dicController[unit.ControllerID].IPAddress, srcFilePath + fileName) != true)
                            {
                                ShowMessageWindow("Failed in moving uniformity low data file.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                                return false;
                            }

                            // Previous, InitialからのRestore時はTempフォルダにもコピー
                            if (baseFileDir == FileDirectory.Backup_Previous || baseFileDir == FileDirectory.Backup_Initial)
                            { System.IO.File.Copy(srcFilePath + fileName, tempFilePath + fileName); }
                        }
                        catch
                        {
                            ShowMessageWindow("Failed in moving uniformity low data file.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                            return false;
                        }
                    }
                }
            }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcRestoreProcessSec(currentStep, lstTargetController.Count, powerUpWait, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);


#if NO_WRITE
#else
            // ●Cabinet Power Off [6]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[6] Cabinet Power Off."); }
            winProgress.ShowMessage("Cabinet Power Off.");

            foreach (ControllerInfo controller in lstTargetController)
            { sendSdcpCommand(SDCPClass.CmdUnitPowerOff, controller.IPAddress); }
            System.Threading.Thread.Sleep(SLEEP_TIME_AFTER_PANEL_OFF);

            if (getUnitPowerStatus() != true)
            {
                string msg = "Failed to cabinet power off.";
                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcRestoreProcessSec(currentStep, lstTargetController.Count, powerUpWait, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            // ●Reconfig [7]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[7] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            sendReconfig();
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcRestoreProcessSec(currentStep, lstTargetController.Count, powerUpWait, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);


            // ●書き込みコマンド発行 [8]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[8] Send Write Command."); }
            winProgress.ShowMessage("Send Write Command.");

            foreach (ControllerInfo controller in lstTargetController)
            { sendSdcpCommand(SDCPClass.CmdDataWrite, controller.IPAddress); }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcRestoreProcessSec(currentStep, lstTargetController.Count, powerUpWait, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);


            // ●Complete待ち [9]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[9] Waiting for the response."); }
            winProgress.ShowMessage("Waiting for the response.");

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
            { return false; }

            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcRestoreProcessSec(currentStep, lstTargetController.Count, powerUpWait, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            // ●Latest → Previousフォルダへコピー [10]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[10] Move File(s). (Latest -> Previous)."); }
            winProgress.ShowMessage("Move File(s). (Latest -> Previous)");

            foreach (ControllerInfo controller in lstTargetController)
            {
                if (baseFileDir == FileDirectory.Temp)
                { restoreCopyLatest2Previous(applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo, lstUploadedFiles, lstDataSizeOkFiles); }
                else
                { copyLatest2Previous(applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo); }
                
            }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcRestoreProcessSec(currentStep, lstTargetController.Count, powerUpWait, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            // ●Temp → Latestフォルダへコピー [11]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[11] Move File(s). (Temp -> Latest).."); }
            winProgress.ShowMessage("Move File(s). (Temp -> Latest).");

            foreach (ControllerInfo controller in lstTargetController)
            {
                if (baseFileDir == FileDirectory.Temp)
                { restoreCopyTemp2Latest(applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo, lstUploadedFiles, lstDataSizeOkFiles); }
                else
                { copyTemp2Latest(applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo); }
            }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcRestoreProcessSec(currentStep, lstTargetController.Count, powerUpWait, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            // ●Reconfig [12]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[12] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            sendReconfig();
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcRestoreProcessSec(currentStep, lstTargetController.Count, powerUpWait, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

#endif
            // ●Tempフォルダのファイルを削除 [13]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[13] Delete Temp File(s)."); }
            winProgress.ShowMessage("Delete Temp File(s).");

            foreach (ControllerInfo controller in lstTargetController)
            {
                if (baseFileDir == FileDirectory.Temp)
                {
                    foreach (string file in lstUploadedFiles)
                    {
                        try
                        {
                            if (Settings.Ins.ExecLog == true)
                            {
                                SaveExecLog("Delete : " + file);
                            }
                            File.Delete(file);
                        }
                        catch
                        {
                            string msg = "Failed to delete.\r\n" + file;
                            ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 210);
                        }
                    }
                }
                else
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
                        catch { return false; }
                    }
                }
            }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcRestoreProcessSec(currentStep, lstTargetController.Count, powerUpWait, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            // ●FTP Off [14]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[14] FTP Off."); }
            winProgress.ShowMessage("FTP Off.");

            foreach (ControllerInfo controller in lstTargetController)
            {
                if (sendSdcpCommand(SDCPClass.CmdFtpOff, controller.IPAddress) != true)
                {
                    ShowMessageWindow("Failed to FTP Off.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }
            }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcRestoreProcessSec(currentStep, lstTargetController.Count, powerUpWait, dataMoveSec, responseSec);
            winProgress.SetRemainTimer(processSec);

            // ●Cabinet Power On [15]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[15] Cabinet Power On."); }
            winProgress.ShowMessage("Cabinet Power On.");

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdUnitPowerOn, SDCP_COMMAND_WAIT_TIME, cont.IPAddress); }
            winProgress.PutForward1Step();

            // Skipされたファイルがある場合は表示
            if ((lstDataSizeNgFiles.Count + lstSkippedFiles.Count) > 0)
            {
                string msg = "Failed to restore " + (lstDataSizeNgFiles.Count + lstSkippedFiles.Count) + " data(s).\r\nYou will find the data files which are failed to be restored, in each '*Temp' folder under the 'Backup' folder.\r\n\r\n";
                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            else
            { return true; }
        }

        private void correctDataUnit(out List<UnitInfo> lstUnitList, int MaxX, int MaxY)
        {
            lstUnitList = new List<UnitInfo>();

            for (int j = MaxY - 1; j >= 0; j--)
            {
                for (int i = 0; i < MaxX; i++)
                {
                    if (aryUnitData[i, j].UnitInfo != null && aryUnitData[i, j].IsChecked == true)
                    {
                        lstUnitList.Add(aryUnitData[i, j].UnitInfo);
                    }
                }
            }
        }
        private void getTotalCabinets(out int count, int MaxX, int MaxY)
        {
            count = 0;
            for (int j = MaxY - 1; j >= 0; j--)
            {
                for (int i = 0; i < MaxX; i++)
                {
                    if (aryUnitData[i, j].UnitInfo != null)
                    {
                        count++;
                    }
                }
            }
        }

        private void searchAdjacentUnitData(out List<UnitInfo> lstUnitList, int MaxX, int MaxY)
        {
            lstUnitList = new List<UnitInfo>();

            for (int y = MaxY - 1; y >= 0; y--)
            {
                for (int x = 0; x < MaxX; x++)
                {
                    if (aryUnitData[x, y].UnitInfo != null && aryUnitData[x, y].IsChecked == true)
                    {
                        if (x + 1 < MaxX && aryUnitData[x + 1, y].UnitInfo != null)
                        {
                            lstUnitList.Add(aryUnitData[x + 1, y].UnitInfo);
                        }
                        if (x - 1 >= 0 && aryUnitData[x - 1, y].UnitInfo != null)
                        {
                            lstUnitList.Add(aryUnitData[x - 1, y].UnitInfo);
                        }
                        if (y + 1 < MaxY && aryUnitData[x, y + 1].UnitInfo != null)
                        {
                            lstUnitList.Add(aryUnitData[x, y + 1].UnitInfo);
                        }
                        if (y - 1 >= 0 && aryUnitData[x, y - 1].UnitInfo != null)
                        {
                            lstUnitList.Add(aryUnitData[x, y - 1].UnitInfo);
                        }
                    }
                }
            }
        }

        private void storeCheckBoxStatusRestore(out bool unitData, out bool hcData, out bool lcData/*, out bool agingData*/)
        {
            unitData = (bool)cbUnitDataRestore.IsChecked;
            hcData = (bool)cbHcDataRestore.IsChecked;
            lcData = (bool)cbLcDataRestore.IsChecked;
            //agingData = (bool)cbAgingDataRestore.IsChecked;
        }

        private bool storeBaseDir(out FileDirectory baseFileDir)
        {
            if (cmbxRestoreTarget.SelectedIndex == 0)
            { baseFileDir = FileDirectory.Backup_Previous; }
            else if (cmbxRestoreTarget.SelectedIndex == 1)
            { baseFileDir = FileDirectory.Backup_Initial; }
            else if (cmbxRestoreTarget.SelectedIndex == 2)
            { baseFileDir = FileDirectory.Temp; }
            else
            {
                baseFileDir = FileDirectory.NA;
                return false;
            }

            return true;
        }

        private bool checkDataFile(List<UnitInfo> lstUnit, FileDirectory targetDir, bool unitData, bool hcData, bool lcData/*, bool agingData*/)
        {
            foreach (UnitInfo unit in lstUnit)
            {
                if (unitData == true)
                {
                    string filePath = makeFilePath(unit, targetDir, DataType.UnitData);

                    if (System.IO.File.Exists(filePath) != true)
                    { return false; }
                }

                if (hcData == true)
                {
                    string filePath = makeFilePath(unit, targetDir, DataType.HcData);

                    if (System.IO.File.Exists(filePath) != true)
                    { return false; }
                }

                if (lcData == true)
                {
                    string filePath = makeFilePath(unit, targetDir, DataType.LcData);

                    if (System.IO.File.Exists(filePath) != true)
                    { return false; }
                }

                //if (agingData == true)
                //{
                //    string filePath = makeFilePath(unit, targetDir, DataType.AgingData);

                //    if (System.IO.File.Exists(filePath) != true)
                //    { return false; }
                //}

                //if (extendedData == true)
                //{
                //    string filePath = makeFilePath(unit, targetDir, DataType.ExtendedData);

                //    if (System.IO.File.Exists(filePath) != true)
                //    { return false; }
                //}
            }

            return true;
        }
        
        // LatestフォルダのファイルをPreviousフォルダに上書き
        private bool copyLatest2Previous(string targetDir)
        {
            string[] files;

            string sourceDir = targetDir + "_Latest";
            string destDir = targetDir + "_Previous\\";

            if (Directory.Exists(destDir) != true)
            {
                try { Directory.CreateDirectory(destDir); }
                catch (Exception ex)
                {
                    ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                    return false;
                }
            }

            files = Directory.GetFiles(destDir, "*", System.IO.SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                try { File.Delete(files[i]); }
                catch { return false; }
            }

            files = Directory.GetFiles(sourceDir, "*", System.IO.SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    string destPath = destDir + System.IO.Path.GetFileName(files[i]);
                    File.Copy(files[i], destPath, true);
                }
                catch (Exception ex)
                {
                    ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                    return false;
                }
            }

            return true;
        }

        // Tempフォルダの全ファイルをLatestフォルダに上書き
        private bool copyTemp2Latest(string targetDir)
        {
            string sourceDir = targetDir + "_Temp";
            string destDir = targetDir + "_Latest\\";

            try { Directory.CreateDirectory(destDir); }
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }

            string[] files = Directory.GetFiles(sourceDir, "*", System.IO.SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    string destPath = destDir + System.IO.Path.GetFileName(files[i]);

                    if (File.Exists(destPath) == true)
                    { File.Delete(destPath); }

                    File.Copy(files[i], destPath, true);
                }
                catch (Exception ex)
                {
                    ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                    return false;
                }
            }

            return true;
        }

        private void restoreFtpUploadFileSelection(List<ControllerInfo> lstTargetController, List<string> lstDataSizeOkFiles, out List<string> lstUploadedFiles, out List<string> lstSkippedFiles, bool isFileSizeCheck = true)
        {
            lstUploadedFiles = new List<string>();
            lstSkippedFiles = new List<string>();

            foreach (ControllerInfo controller in lstTargetController)
            {
                string tempDirPath = applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo + "_Temp\\";

                bool isUploaded = false;
                string[] files = Directory.GetFiles(tempDirPath);
                for (int i = 0; i < files.Length; i++)
                {
                    string fileName = System.IO.Path.GetFileName(files[i]);
                    if (fileName.Contains("_ud.bin") || fileName.Contains("_hc.bin") || fileName.Contains("_lc.bin"))
                    {
                        if (isFileSizeCheck) // ファイルサイズが正しいもののみFTPにアップロードしたい場合
                        {
                            if (lstDataSizeOkFiles.Contains(files[i]))
                            {
                                if (confirmChecksum(files[i]) && checkCommonHeaderDataType(files[i]))
                                {
                                    if (putFileFtp(controller.IPAddress, files[i]))
                                    {
                                        lstUploadedFiles.Add(files[i]);
                                        isUploaded = true;
                                    }
                                    else
                                    { lstSkippedFiles.Add(files[i]); }
                                }
                                else
                                { lstSkippedFiles.Add(files[i]); }
                            }
                        }
                        else
                        {
                            if (confirmChecksum(files[i]) && checkCommonHeaderDataType(files[i]))
                            {
                                if (putFileFtp(controller.IPAddress, files[i]))
                                {
                                    lstUploadedFiles.Add(files[i]);
                                    isUploaded = true;
                                }
                                else
                                { lstSkippedFiles.Add(files[i]); }
                            }
                            else
                            { lstSkippedFiles.Add(files[i]); }
                        }
                    }
                    else
                    { lstSkippedFiles.Add(files[i]); }
                }

                if (!isUploaded)
                { controller.Target = false; }
            }
        }


        // アップロードに成功したファイルのみPreviousフォルダに上書き
        private bool restoreCopyLatest2Previous(string targetDir, List<string> lstUploadedFiles, List<string> lstDataSizeOkFiles)
        {
            string[] files;
            List<string> lstTargetControllerUploadedFiles = new List<string>();

            string tempDir = targetDir + "_Temp";
            string latestDir = targetDir + "_Latest";
            string previousDir = targetDir + "_Previous";

            // アップロードに成功した対象コントローラのファイルを抽出
            for (int i = 0; i < lstUploadedFiles.Count; i++)
            {
                if (lstUploadedFiles[i].Contains(targetDir))
                { lstTargetControllerUploadedFiles.Add(lstUploadedFiles[i]); }
            }
            
            // Previous内のファイル削除
            files = Directory.GetFiles(previousDir, "*", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    string fileName = Path.GetFileName(files[i]);
                    string targetFile = tempDir + "\\" + fileName;
                    if (lstTargetControllerUploadedFiles.Contains(targetFile) && lstDataSizeOkFiles.Contains(targetFile))
                    { File.Delete(files[i]); }
                }
                catch
                {
                    string msg = "Failed to delete.\r\n" + files[i];
                    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 210);
                }
            }

            // Latest → Previousにファイルコピー
            files = Directory.GetFiles(latestDir, "*", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    string fileName = Path.GetFileName(files[i]);
                    string targetFile = tempDir + "\\" + fileName;
                    if (lstTargetControllerUploadedFiles.Contains(targetFile) && lstDataSizeOkFiles.Contains(targetFile))
                    {
                        string destPath = previousDir + "\\" + fileName;
                        File.Copy(files[i], destPath, true);
                    }
                }
                catch
                {
                    string msg = "Failed to copy.\r\n" + files[i];
                    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 210);
                }
            }

            return true;
        }

        // アップロードに成功したファイルのみLatestフォルダに上書き
        private bool restoreCopyTemp2Latest(string targetDir, List<string> lstUploadedFiles, List<string> lstDataSizeOkFiles)
        {
            string[] files;
            List<string> lstTargetControllerUploadedFiles = new List<string>();

            string tempDir = targetDir + "_Temp";
            string latestDir = targetDir + "_Latest";

            // アップロードに成功した対象コントローラのファイルを抽出
            for (int i = 0; i < lstUploadedFiles.Count; i++)
            {
                if (lstUploadedFiles[i].Contains(targetDir))
                { lstTargetControllerUploadedFiles.Add(lstUploadedFiles[i]); }
            }

            // Latest内のファイル削除
            files = Directory.GetFiles(latestDir, "*", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    string fileName = Path.GetFileName(files[i]);
                    string targetFile = tempDir + "\\" + fileName;
                    if (lstTargetControllerUploadedFiles.Contains(targetFile) && lstDataSizeOkFiles.Contains(targetFile))
                    { File.Delete(files[i]); }
                }
                catch
                {
                    string msg = "Failed to delete.\r\n" + files[i];
                    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 210);
                }
            }

            // Temp → Latestにファイルコピー
            files = Directory.GetFiles(tempDir, "*", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    string fileName = Path.GetFileName(files[i]);
                    string targetFile = tempDir + "\\" + fileName;
                    if (lstTargetControllerUploadedFiles.Contains(targetFile) && lstDataSizeOkFiles.Contains(targetFile))
                    {
                        string destPath = latestDir + "\\" + fileName;
                        File.Copy(files[i], destPath, true);
                    }
                }
                catch
                {
                    string msg = "Failed to copy.\r\n" + files[i];
                    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 210);
                }
            }

            return true;
        }

        // Re-Write時、書き込んだTempファイルをLatestフォルダに上書き
        private bool overwriteLatest(List<UnitInfo> lstTargetUnit, bool unitData, bool cellData, bool agingData)
        {
            foreach (UnitInfo unit in lstTargetUnit)
            {
                string srcFilePath = applicationPath + "\\Backup\\" + dicController[unit.ControllerID].ModelName + "_" + dicController[unit.ControllerID].SerialNo + "_Temp\\";
                string destFilePath = applicationPath + "\\Backup\\" + dicController[unit.ControllerID].ModelName + "_" + dicController[unit.ControllerID].SerialNo + "_Latest\\";

                if (unitData == true)
                {
                    string fileName = "u" + unit.PortNo.ToString("D2") + unit.UnitNo.ToString("D2") + "_ud.bin";
                    try
                    {
                        if (File.Exists(destFilePath + fileName) == true)
                        {
                            try { File.Delete(destFilePath + fileName); }
                            catch { }
                        }

                        try { File.Copy(srcFilePath + fileName, destFilePath + fileName, true); }
                        catch { }
                    }
                    catch
                    {
                        ShowMessageWindow("Failed in moving cabinet data file.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                        return false;
                    }
                }

                if (cellData == true)
                {
                    string fileName = "u" + unit.PortNo.ToString("D2") + unit.UnitNo.ToString("D2") + "_cd.bin";
                    try
                    {
                        if (File.Exists(destFilePath + fileName) == true)
                        {
                            try { File.Delete(destFilePath + fileName); }
                            catch { }
                        }

                        try { File.Copy(srcFilePath + fileName, destFilePath + fileName, true); }
                        catch { }
                    }
                    catch
                    {
                        ShowMessageWindow("Failed in moving module data file.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                        return false;
                    }
                }

                if (agingData == true)
                {
                    string fileName = "u" + unit.PortNo.ToString("D2") + unit.UnitNo.ToString("D2") + "_ad.bin";
                    try
                    {
                        if (File.Exists(destFilePath + fileName) == true)
                        {
                            try { File.Delete(destFilePath + fileName); }
                            catch { }
                        }

                        try { File.Copy(srcFilePath + fileName, destFilePath + fileName, true); }
                        catch { }
                    }
                    catch
                    {
                        ShowMessageWindow("Failed in moving aging data file.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                        return false;
                    }
                }
            }

            return true;
        }
        private int calcRestoreDataMoveSec(int cabinetCount, bool unitData, bool hcData, bool lcData)
        {
            int dataMoveSec;

            dataMoveSec = (int)((Convert.ToDouble(unitData) * (cabinetCount * RESTORE_UD_FILE_MOVE_SEC))
                + (Convert.ToDouble(hcData) * (cabinetCount * RESTORE_HC_FILE_MOVE_SEC))
                + (Convert.ToDouble(lcData) * (cabinetCount * RESTORE_LC_FILE_MOVE_SEC)));

            return dataMoveSec;
        }

        private int calcRestoreDataMoveSecForTemp(int udFileCount, int hcFileCount, int lcFileCount)
        {
            int dataMoveSec;

            dataMoveSec = (int)(udFileCount * RESTORE_UD_FILE_MOVE_SEC + hcFileCount * RESTORE_HC_FILE_MOVE_SEC + lcFileCount * RESTORE_LC_FILE_MOVE_SEC);

            return dataMoveSec;
        }

        private int calcRestoreResponseSec(int maxConnectCabinet, bool unitData, bool hcData, bool lcData)
        {
            int responseSec;

            responseSec = (int)((Convert.ToDouble(unitData) * (maxConnectCabinet * (RESTORE_UD_FILE_RESPONSE_SEC + maxConnectCabinet * RESTORE_FILE_SUPPLEMENT_SEC) + RESTORE_UD_FILE_RESPONSE_FIX_SEC))
                + (Convert.ToDouble(hcData) * (maxConnectCabinet * (RESTORE_HC_FILE_RESPONSE_SEC + maxConnectCabinet * RESTORE_FILE_SUPPLEMENT_SEC)+ RESTORE_HC_FILE_RESPONSE_FIX_SEC))
                + (Convert.ToDouble(lcData) * (maxConnectCabinet * (RESTORE_LC_FILE_RESPONSE_SEC + maxConnectCabinet * RESTORE_FILE_SUPPLEMENT_SEC) + RESTORE_LC_FILE_RESPONSE_FIX_SEC))
                + RESTORE_FILE_RESPONSE_FIX_SEC);

            return responseSec;
        }

        private int calcRestoreResponseSecForTemp(int cabinetCount,int udFileCount, int hcFileCount, int lcFileCount)
        {
            int responseSec;
            int udFlag = 0;
            int hcFlag = 0;
            int lcFlag = 0;

            if(udFileCount > 0) 
            { udFlag = 1;}

            if (hcFileCount > 0)
            { hcFlag = 1; }

            if (lcFileCount > 0)
            { lcFlag = 1; }

            responseSec = (int)(udFlag * (udFileCount * (RESTORE_UD_FILE_RESPONSE_SEC + cabinetCount * RESTORE_FILE_SUPPLEMENT_SEC) + RESTORE_UD_FILE_RESPONSE_FIX_SEC)
                + (hcFlag * (hcFileCount * (RESTORE_HC_FILE_RESPONSE_SEC + cabinetCount * RESTORE_FILE_SUPPLEMENT_SEC) + RESTORE_HC_FILE_RESPONSE_FIX_SEC))
                + (lcFlag * (lcFileCount * (RESTORE_LC_FILE_RESPONSE_SEC + cabinetCount * RESTORE_FILE_SUPPLEMENT_SEC) + RESTORE_LC_FILE_RESPONSE_FIX_SEC))
                + RESTORE_FILE_RESPONSE_FIX_SEC);

            return responseSec;
        }

        private int calcRestoreProcessSec(int step, int contCount, bool powerWait, int dataMoveSec, int responseSec)
        {
            int processSec = 0;
            //係数　データ移動,レスポンス処理,コントローラー台数に影響する時間を除いた合計処理時間
            //commonA[0] [0] restore start
            //commonA[1] [1] Store Target Cabinet(s) Info
            //commonA[2] [2] Store Target Controller(s) Info.
            //commonA[3] [3] Check Backup Data.
            //commonA[4] [4] Check Controller Power.
            //commonA[5] [*1] FTP On [*2] Get Model Name. [*3] Get Serial No.
            //           [5] Move File(s).
            //commonA[6] [6] Cabinet Power Off.
            //commonA[7] [7] Send Reconfig.
            //commonA[8] [8] Send Write Command.
            //            [9] Waiting for the response.
            //commonA[10] [10] Move File(s). (Latest->Previous).
            //commonA[11] [11] Move File(s). (Temp->Latest)..
            //commonA[12] [12] Send Reconfig.
            //commonA[13] [13] Delete Temp File(s).
            //commonA[14] [14] FTP Off.
            //commonA[15] [15] Cabinet Power On.
            int[] commonA = new int[15] { 0, 0, 0, 0, 0, 0, 4, 13, 1, 0, 0, 16, 0, 0, 0};

            //係数　コントローラー台数に影響のある各種処理時間
            //contB[0] [0] restore start
            //contB[1] [1] Store Target Cabinet(s) Info
            //contB[2] [2] Store Target Controller(s) Info.
            //contB[3] [3] Check Backup Data.
            //contB[4] [4] Check Controller Power.
            //contB[5] [*1] FTP On [*2] Get Model Name. [*3] Get Serial No.
            //           [5] Move File(s).
            //contB[6] [6] Cabinet Power Off.
            //contB[7] [7] Send Reconfig.
            //contB[8] [8] Send Write Command.
            //            [9] Waiting for the response.
            //contB[9] [10] Move File(s). (Latest->Previous).
            //contB[10] [11] Move File(s). (Temp->Latest)..
            //contB[11] [12] Send Reconfig.
            //contB[12] [13] Delete Temp File(s).
            //contB[13] [14] FTP Off.
            //contB[14] [15] Cabinet Power On.
            int[] contB = new int[15] { 0, 0, 0, 0, 1, 3, 1, 2, 0, 0, 0, 1, 0, 1, 0};

            switch (step)
            {
                case 3: //[4] Check Controller Power. 以下の処理時間全体を計算
                    processSec = commonA.Skip(step + 1).Sum() + contCount * contB.Skip(step + 1).Sum()
                        + (Convert.ToInt32(powerWait) * 60) + responseSec + dataMoveSec;
                    break;
                case 4: //[*1] FTP On [*2] Get Model Name. [*3] Get Serial No.
                case 5: //[5] Move File(s).
                    processSec = commonA.Skip(step + 1 ).Sum() + contCount * contB.Skip(step + 1).Sum()
                            + responseSec + dataMoveSec;
                    break;
                case 6: //[6] Cabinet Power Off.
                case 7: //[7] Send Reconfig.
                case 8: //[8] Send Write Command.
                case 9: //[9] Waiting for the response.
                    processSec = commonA.Skip(step + 1 - 1).Sum() + contCount * contB.Skip(step + 1 - 1).Sum()
                            + responseSec;
                    break;
                case 10: //[10] Move File(s). (Latest->Previous).
                case 11: //[11] Move File(s). (Temp->Latest)..
                case 12: //[12] Send Reconfig.
                case 13: //[13] Delete Temp File(s).
                case 14: //[14] FTP Off.
                case 15: //[15] Cabinet Power On.
                    processSec = commonA.Skip(step + 1 - 2).Sum() + contCount * contB.Skip(step + 1 - 2).Sum();
                    break;
                default:
                    break;
            }

            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("calcRestoreProcessSec step:" + step + " contCount:" + contCount + " powerWait:" + powerWait + " responseSec:" + responseSec + " dataMoveSec:" + dataMoveSec + " processSec:" + processSec); }
            return processSec;
        }
        #endregion Restore

        #region Replace

        private void diselectAllDataUnits()
        {
            for (int i = 0; i < allocInfo.MaxX; i++)
            {
                for (int j = 0; j < allocInfo.MaxY; j++)
                {
                    Dispatcher.Invoke(new Action(() => { aryUnitData[i, j].IsChecked = false; }));
                }
            }
        }

        private void diselectAllDataCells()
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    Dispatcher.Invoke(new Action(() => { aryCellData[i, j].IsChecked = false; }));
                }
            }
        }

        // Module交換用アルゴリズム
        private bool replaceCell()
        {
            if (Settings.Ins.ExecLog == true)
            {
                SaveExecLog("");
                SaveExecLog("[0] replaceModule start");
            }

            var dispatcher = Application.Current.Dispatcher;
            List<ControllerInfo> lstTargetController = new List<ControllerInfo>();
            ControllerInfo controller;
            List<UnitInfo> lstTargetUnit = new List<UnitInfo>();
            UnitInfo targetUnit;

            // ●進捗の最大値を設定
            winProgress.ShowMessage("Set Step Count.");
            int step = REPLACE_MODULE_STEPS;
            winProgress.SetWholeSteps(step);

            // ●調整をするCabinetをListに格納 [1]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[1] Store Target Cabinet(s) Info."); }
            winProgress.ShowMessage("Store Target Cabinet(s) Info.");

            dispatcher.Invoke(() => correctDataUnit(out lstTargetUnit, allocInfo.MaxX, allocInfo.MaxY));
            if (lstTargetUnit.Count == 0)
            {
                ShowMessageWindow("Please select cabinet.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            else if (lstTargetUnit.Count > 1)
            {
                ShowMessageWindow("Plural cabinets are selected.\r\nPlease select only one cabinet in Module Replace.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            targetUnit = lstTargetUnit[0];
            winProgress.PutForward1Step();

            // ●調整をするCabinetが接続されているControllerを格納 [2]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[1] Store Target Cabinet(s) Info."); }
            winProgress.ShowMessage("Store Target Cabinet(s) Info.");

            // 初期化
            foreach (ControllerInfo cont in dicController.Values)
            { cont.Target = false; }

            dicController[targetUnit.ControllerID].Target = true;

            controller = dicController[targetUnit.ControllerID];    // 対象Controller

            winProgress.PutForward1Step();

            // ●Power OnでないときはOnにする [3]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[2] Check Controller Power."); }
            winProgress.ShowMessage("Check Controller Power.");

            if (!setAllControllerPowerOn())
            { return false; }
            winProgress.PutForward1Step();

            // FTP ON [*1]
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

            // ●調整データをバックアップ [*4]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*4] Backup Target Cabinet Data."); }
            winProgress.ShowMessage("Backup Target Cabinet Data.");

            if (backupReplaceModuleData(controller, targetUnit) != true)
            { return false; }

            // ●FTP Off [5]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[5] FTP Off."); }
            winProgress.ShowMessage("FTP Off.");

            if (sendSdcpCommand(SDCPClass.CmdFtpOff, controller.IPAddress) != true)
            {
                ShowMessageWindow("Failed to FTP Off.", "Error!", System.Drawing.SystemIcons.Error, 420, 180);
                return false;
            }
            winProgress.PutForward1Step();

            return true;
        }

        private bool backupReplaceModuleData(ControllerInfo controller, UnitInfo unit)
        {
            List<string> lstNonExistentFiles = new List<string>(); // 出力されなていないファイル
            List<string> lstBrokenFiles = new List<string>(); // データが破損しているFile
            List<int> lstMissingModuleNo = new List<int>(); ; // 欠落されているモジュール番号
            List<string> lstFtpFiles;

            // ●Rbinフォルダ中にファイルがある場合は退避する [*4-1]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[*4-1] Target Cabinet Data File Rename."); }
            winProgress.ShowMessage("Target Cabinet Data File Rename.");

            string baseDir = applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo;
            string rbinDir = baseDir + "_Rbin\\";

            // Rbinフォルダ存在しない場合作成
            if (!Directory.Exists(rbinDir))
            {
                try { Directory.CreateDirectory(rbinDir); }
                catch (Exception ex)
                {
                    ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                    return false;
                }
            }

            string searchInfo = "u" + unit.PortNo.ToString("D2") + unit.UnitNo.ToString("D2") + "*.rbin";
            string[] sourceDirFiles = Directory.GetFiles(rbinDir, searchInfo, System.IO.SearchOption.AllDirectories);

            for (int i = 0; i < sourceDirFiles.Length; i++)
            {
                string name = System.IO.Path.GetFileName(sourceDirFiles[i]);

                if (name.Contains("_ud.rbin") || name.Contains("_hc.rbin") || name.Contains("_hf.rbin") || name.Contains("_lc.rbin"))
                {
                    FileInfo fileInfo = new FileInfo(rbinDir + name);
                    string destDirFile = rbinDir + name + "_" + DateTime.Now.ToString("yyyyMMddHHmm");
                    fileInfo.MoveTo(destDirFile);
                }
            }
            winProgress.PutForward1Step();

            // ●Cabinet Power Off [*4-2]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[*4-2] Cabinet Power Off."); }
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

            // ●Reconfig [*4-3]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[*4-3] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            sendReconfig();
            winProgress.PutForward1Step();

            // ●Backupコマンド発行 [*4-4]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[*4-4] Send Backup Command."); }
            winProgress.ShowMessage("Send Backup Command.");

            byte[] cmd = new byte[SDCPClass.CmdDataRead_UnitData.Length];
            Array.Copy(SDCPClass.CmdDataRead_UnitData, cmd, SDCPClass.CmdDataRead_UnitData.Length);

            cmd[9] += (byte)(unit.PortNo << 4);
            cmd[9] += (byte)unit.UnitNo;
            cmd[21] = 0x63;  // ud:0x01, hc:0x02, lc:0x20, hf:0x40

            sendSdcpCommand(cmd, controller.IPAddress);
            winProgress.PutForward1Step();

            // ●Complete待ち [*4-5]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[*4-5] Waiting for the response."); }
            winProgress.ShowMessage("Waiting for the response.");
            winProgress.ElapsedTimer(true);

            try
            {
                while (true)
                {
                    if (checkCompleteFtp(controller.IPAddress, "read_complete") == true)
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
            
            winProgress.ElapsedTimer(false);
            winProgress.PutForward1Step();

            // ●Reconfig [*4-6]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[*4-6] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            sendReconfig();
            winProgress.PutForward1Step();

            // ●Move Files [*4-7]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("\t[*4-7] Move Files."); }
            winProgress.ShowMessage("Move Files.");

            try
            { getFileListFtp(controller.IPAddress, out lstFtpFiles); }
            catch
            { return false; }

            try
            { getFilesFtpRetry(controller.IPAddress, rbinDir, lstFtpFiles, out lstNonExistentFiles); }
            catch
            { return false; }

            // ファイルが足りない場合
            if (lstNonExistentFiles.Count > 0)
            {
                string msg = "File does not exist.\r\nPlease check the backup file before doing other work.\r\n\r\n";

                foreach (string str in lstNonExistentFiles)
                { msg += str + "\r\n"; }

                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // データ欠損がある場合
            sourceDirFiles = Directory.GetFiles(rbinDir, searchInfo, System.IO.SearchOption.AllDirectories);
			string hfFile = "";
            for (int i = 0; i < sourceDirFiles.Length; i++)
            {
                string name = System.IO.Path.GetFileName(sourceDirFiles[i]);
                if (name.Contains("_ud.rbin") || name.Contains("_hc.rbin") || name.Contains("_lc.rbin"))
                {
                    if (checkAllDataLength(sourceDirFiles[i]) != true || checkCommonHeaderDataType(sourceDirFiles[i]) != true)
                    { lstBrokenFiles.Add(name); }
                }
                else if (name.Contains("_hf.rbin"))
                {
                    if (checkAllDataLength(sourceDirFiles[i]) != true && checkCommonHeaderDataType(sourceDirFiles[i]) == true)
                    {
                        hfFile = sourceDirFiles[i];
                        List<CompositeConfigData> lstHfData = CompositeConfigData.getCompositeConfigDataList(sourceDirFiles[i], dt_ColorCorrectionRead);
                        for (int ModuleNo = 1; ModuleNo <= moduleCount; ModuleNo++)
                        {
                            if (lstHfData.FirstOrDefault(data => data.header.Option1 == ModuleNo) == null)
                            { lstMissingModuleNo.Add(ModuleNo); }
                        }
                    }
                }
            }

            if (lstBrokenFiles.Count > 0)
            {
                string msg = "The data size is incorrect.\r\n";
                foreach (string str in lstBrokenFiles)
                { msg += str + "\r\n"; }

                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }

            // ●データをコンバート [*4-8]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*4-8] Data Convert."); }
            winProgress.ShowMessage("Data Convert.");

            string initialDir = baseDir + "_Initial\\";
            string latestDir = baseDir + "_Latest\\";
            string previousDir = baseDir + "_Previous\\";

            // Initialフォルダ存在しない場合作成
            if (!Directory.Exists(initialDir))
            {
                try { Directory.CreateDirectory(initialDir); }
                catch (Exception ex)
            	{
	                ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
	                return false;
            	}
            }

            // Latestフォルダ存在しない場合作成
            if (!Directory.Exists(latestDir))
            {
                try { Directory.CreateDirectory(latestDir); }
                catch (Exception ex)
                {
                    ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                    return false;
                }
            }

            // Previousフォルダ存在しない場合作成
            if (!Directory.Exists(previousDir))
        	{
                try { Directory.CreateDirectory(previousDir); }
                catch (Exception ex)
                {
                    ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                    return false;
                }
            }

            // Initial・Latest・Previousフォルダへ格納(rbin -> bin)
            for (int i = 0; i < lstFtpFiles.Count; i++)
            {
                string name = Path.GetFileName(lstFtpFiles[i]);
                if (name.Contains("_ud.rbin") || name.Contains("_hc.rbin") || name.Contains("_lc.rbin"))
                {
                    string rbinFile = rbinDir + name;
                    convertRbinToBin(rbinFile, initialDir);

                    string newName = Path.GetFileNameWithoutExtension(name) + ".bin";
                    string initialFile = initialDir + newName;
                    string latestFile = latestDir + newName;
                    string previousFile = previousDir + newName;
                    File.Copy(@initialFile, @latestFile, true);
                    File.Copy(@initialFile, @previousFile, true);
                }
            }
            winProgress.PutForward1Step();
            
            if (lstMissingModuleNo.Count > 0)
            {
                int count = 0;
                string msg = "There are lacking some module data in the following files.\r\n" + hfFile + "\r\n*the lacking of data for following module position(s)\r\n";
                for (int i = 0; i < lstMissingModuleNo.Count; i++)
                {
                    msg += lstMissingModuleNo[i];

                    count++;
                    if (lstMissingModuleNo.Count != count)
                    { msg += ", "; }
                }

                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
            }

            return true;
        }

        private void checkTargetDataCell(out List<int> lstTargetCell)
        {
            lstTargetCell = new List<int>();

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (aryCellData[i, j].IsChecked == true)
                    {
                        lstTargetCell.Add(4 * j + i);
                    }
                }
            }
        }

        private bool resetAgingData(string agingFile, string tempFile, int targetCellNo)
        {
            byte[] srcBytes = File.ReadAllBytes(agingFile);
            byte[] tempBytes = new byte[0x20000];
            
            for (int i = 0; i < 12; i++)
            {
                Array.Copy(srcBytes, 0x20000 * i, tempBytes, 0, 0x20000);

                // 目標とするCellかどうか確認
                if (tempBytes[0] == 0xA5 && tempBytes[4] == targetCellNo + 1)
                {
                    // PP1セットだとヘッダがFFになっているのをついでに初期化
                    for (int pos = 24; pos < 508; pos++)
                    { tempBytes[pos] = 0; }

                    // 積算発光時間を初期化
                    for (int pos = 512; pos < tempBytes.Length; pos++)
                    { tempBytes[pos] = 0; }

                    // CheckSum再計算
                    calcAgingDataCheckSum(ref tempBytes);

                    // srcBytesに書き戻し
                    Array.Copy(tempBytes, 0, srcBytes, 0x20000 * i, 0x20000);

                    break;
                }
            }
            
            File.WriteAllBytes(tempFile, srcBytes);

            return true;
        }

        private bool calcAgingDataCheckSum(ref byte[] agingData)
        {
            uint checkSum = 0;

            for (int i = 0; i < agingData.Length; i += 4) // すべてのデータが4Byteサイズなので
            {
                // CheckSumの部分はSkip
                if (i == 508)
                { continue; }

                checkSum += (uint)((agingData[i + 3] << 24) | (agingData[i + 2] << 16) | (agingData[i + 1] << 8) | (agingData[i] << 0));
            }
         
            byte[] arySum = BitConverter.GetBytes(checkSum);

            Array.Copy(arySum, 0, agingData, 508, 4);

            return true;
        }

        // RCS基板交換用アルゴリズム
        private bool replaceUcBoard()
        {
            if (Settings.Ins.ExecLog == true)
            {
                SaveExecLog("");
                SaveExecLog("[0] replaceRcsBoard start");
            }

            bool status;
            ControllerInfo controller;
            List<UnitInfo> lstTargetUnit = new List<UnitInfo>();
            UnitInfo targetUnit, srcUnit;
            var dispatcher = Application.Current.Dispatcher;

            // ●進捗の最大値を設定
            winProgress.ShowMessage("Set Step Count.");
            int step = REPLACE_RCS_BOARD_STEPS;
            winProgress.SetWholeSteps(step);

            // ●調整をするCabinetをListに格納 [1]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[1] Store Target Cabinet Info."); }
            winProgress.ShowMessage("Store Target Cabinet Info.");

            dispatcher.Invoke(() => correctDataUnit(out lstTargetUnit, allocInfo.MaxX, allocInfo.MaxY));
            if (lstTargetUnit.Count == 0)
            {
                ShowMessageWindow("Please select cabinet.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            else if (lstTargetUnit.Count > 1)
            {
                ShowMessageWindow("Plural cabinets are selected.\r\nPlease select only one cabinet in RCS Board Replace.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            targetUnit = lstTargetUnit[0];
            winProgress.PutForward1Step();

            // ●調整をするCabinetが接続されているController格納 [2]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[2] Store Target Controller(s) Info."); }
            winProgress.ShowMessage("Store Target Controller(s) Info.");

            // 初期化
            foreach (ControllerInfo cont in dicController.Values)
            { cont.Target = false; }

            dicController[targetUnit.ControllerID].Target = true;

            controller = dicController[targetUnit.ControllerID];    // 対象Controller
            winProgress.PutForward1Step();

            // ●調整データがあるか確認 [3]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[3] Check Backup Data."); }
            winProgress.ShowMessage("Check Backup Data.");

            // 対象Cabinetのデータが存在しない、または異常の場合、隣接Cabinet(データ正常)が代入される
            srcUnit = checkBackupData(targetUnit);
            if (srcUnit == null)
            {
                ShowMessageWindow("Backup data does not exist or data configuration is missing.", "Error!", System.Drawing.SystemIcons.Error, 420, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●Power OnでないときはOnにする [4]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[4] Check Controller Power."); }
            winProgress.ShowMessage("Check Controller Power.");

            if (!setAllControllerPowerOn())
            { return false; }
            winProgress.PutForward1Step();

            #region Controller Settings

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

            // ●Tempフォルダ内のファイルを削除 [*4]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*4] Delete Temp Files."); }
            winProgress.ShowMessage("Delete Temporary Files.");

            string tempPath = applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo + "_Temp";

            if (Directory.Exists(tempPath) != true)
            { Directory.CreateDirectory(tempPath); }

            string[] files = Directory.GetFiles(tempPath, "*", System.IO.SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                try { File.Delete(files[i]); }
                catch { return false; }
            }
            winProgress.PutForward1Step();

            #endregion Controller Settings

            // ●基板交換 [4]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[4] Change RCS Board."); }
            winProgress.ShowMessage("Change RCS Board.");

            bool? result = false;
            showMessageWindow(out result, "Please Change RCS Board.", "RCS Board Replace", "Done");
            if (result != true)
            { return false; }
            winProgress.PutForward1Step();

            // ●Cabinet Data生成[5]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[5] Create Cabinet Data."); }
            winProgress.ShowMessage("Create Cabinet Data.");
            status = dispatcher.Invoke(() => createUnitDataWriteData(srcUnit, tempPath));
            if (status != true)
            { return false; }
            winProgress.PutForward1Step();

            // ●FTPにファイルをアップロード [6]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[6] Upload File to FTP."); }
            winProgress.ShowMessage("Upload File to FTP.");
                        
            if (uploadFile2Ftp(controller, targetUnit) != true)
            { return false; }
            winProgress.PutForward1Step();

#if NO_WRITE
#else
            // ●Cabinet Power Off [7]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[7] Cabinet Power Off"); }
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

            // ●Reconfig [8]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[8] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            sendReconfig();
            winProgress.PutForward1Step();

            // ●書き込みコマンド発行 [9]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[9] Send Write Command."); }
            winProgress.ShowMessage("Send Write Command.");

            sendSdcpCommand(SDCPClass.CmdDataWrite, controller.IPAddress);
            winProgress.PutForward1Step();

            // ●書き込みComplete待ち [10]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[10] Waiting for the response."); }
            winProgress.ShowMessage("Waiting for the response.");
            winProgress.ElapsedTimer(true);

            try
            {
                while (true)
                {
                    if (checkCompleteFtp(controller.IPAddress, "write_complete") == true)
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

            winProgress.ElapsedTimer(false);
            winProgress.PutForward1Step();

            // ●Latest → Previousフォルダへコピー [11]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[11] Move Latest to Previous."); }
            winProgress.ShowMessage("Move Latest to Previous.");

            copyLatest2Previous(applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo);
            winProgress.PutForward1Step();

            // ●Temp → Latestフォルダへコピー [12]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[12] Move Temp to Latest."); }
            winProgress.ShowMessage("Move Temp to Latest.");

            copyTemp2Latest(applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo);
            winProgress.PutForward1Step();

            // ●Reconfig [13]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[13] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            sendReconfig();
            winProgress.PutForward1Step();
#endif
            // ●Tempフォルダのファイルを削除 [14]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[14] Delete Temp Files."); }
            winProgress.ShowMessage("Delete Temp Files.");

            files = Directory.GetFiles(tempPath, "*", System.IO.SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                try { File.Delete(files[i]); }
                catch { return false; }
            }
            winProgress.PutForward1Step();

            // ●FTP Off [15]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[15] FTP Off."); }
            winProgress.ShowMessage("FTP Off.");

            if (sendSdcpCommand(SDCPClass.CmdFtpOff, controller.IPAddress) != true)
            {
                ShowMessageWindow("Failed to FTP Off.", "Error!", System.Drawing.SystemIcons.Error, 420, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●Power OffでないときはOffにする [16]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[16] Controller Power Off."); }
            winProgress.ShowMessage("Controller Power Off.");

            if (getPowerStatus(controller.IPAddress, out PowerStatus power) != true)
            { return false; }

            if (power != PowerStatus.Standby)
            {
                winProgress.ShowMessage("Controller Power Off.");
                if (sendSdcpCommand(SDCPClass.CmdStandby, controller.IPAddress) != true)
                { return false; }
            }
            System.Threading.Thread.Sleep(10000);

            DateTime startTime = DateTime.Now;
            while (true)
            {
                if (getPowerStatus(controller.IPAddress, out power) != true)
                { return false; }

                if (power == PowerStatus.Standby)
                { break; }

                TimeSpan span = DateTime.Now - startTime;
                if (span.TotalSeconds > 100)
                {
                    string msg = "Failed to standby controller.";
                    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }

                System.Threading.Thread.Sleep(1000);
            }
            winProgress.PutForward1Step();

            // ●Power OnでないときはOnにする [17]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[17] Controller Power On."); }
            winProgress.ShowMessage("Controller Power On.");

            if (!setControllerPowerOn(controller))
            { return false; }    
            winProgress.PutForward1Step();

            return true;
        }

        private bool checkAgingDataFile(UnitInfo unit)
        {
            // Aging Data
            string filePath = makeFilePath(unit, FileDirectory.Backup_Latest, DataType.AgingData);

            if (System.IO.File.Exists(filePath) != true)
            { return false; }

            return true;
        }

        private UnitInfo checkBackupData(UnitInfo unit)
        {
            var dispatcher = Application.Current.Dispatcher;
            List<UnitInfo> lstTargetUnit = new List<UnitInfo>();

            targetPath = makeFilePath(unit, FileDirectory.Backup_Rbin, DataType.UnitData);
            string filePath = targetPath;
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    if (checkAllDataLength(filePath) && checkCommonHeaderDataType(filePath) && confirmChecksum(filePath))
                    { return unit; }
                    else
                    { unit = null; }
                }
                catch
                { unit = null; }
            }
            else
            { unit = null; }

            // 右隣>左隣>下隣>上隣順でデータを存在するか確認する
            dispatcher.Invoke(() => searchAdjacentUnitData(out lstTargetUnit, allocInfo.MaxX, allocInfo.MaxY));
            for (int i = 0; i < lstTargetUnit.Count; i++)
            {
                filePath = makeFilePath(lstTargetUnit[i], FileDirectory.Backup_Rbin, DataType.UnitData);
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        if (checkAllDataLength(filePath) && checkCommonHeaderDataType(filePath) && confirmChecksum(filePath))
                        {
                            unit = lstTargetUnit[i];
                            return unit;
                        }
                    }
                    catch
                    { unit = null; }
                }
                else
                { unit = null; }
            }

            return unit;
        }

        private bool createUnitDataWriteData(UnitInfo unit, string destDir)
        {
            string orgModelName, orgSerialNo;
            string newModelName, newSerialNo;
            
            string srcFile = makeFilePath(unit, FileDirectory.Backup_Rbin, DataType.UnitData);
            byte[] srcBytes = System.IO.File.ReadAllBytes(srcFile);

            while (true)
            {
                WindowCabinetInfo winCds = new WindowCabinetInfo();
                orgModelName = convertStrModelName(srcBytes, ProductInfoDataModelNameOffset);
                orgSerialNo = convertStrSerialNo(srcBytes, ProductInfoDataSerialNoOffset);
                winCds.txbModelName.Text = orgModelName;
                winCds.txbSerialNo.Text = orgSerialNo;

                if (winCds.ShowDialog() == false)
                { return false; }

                newModelName = winCds.txbModelName.Text;
                newSerialNo = winCds.txbSerialNo.Text;

                try
                {
                    // モデル名の有効性チェック
                    if (newModelName.Length == 0 || Regex.IsMatch(newModelName, @"[^a-zA-Z0-9\- /]"))
                    {
                        ShowMessageWindow("Format does not match. Please type it again.", "Error!", System.Drawing.SystemIcons.Error, 420, 180);
                        continue;
                    }

                    // シリアル番号の有効性チェック
                    if (newSerialNo.Length == 0 || Regex.IsMatch(newSerialNo, @"[^0-9]"))
                    {
                        ShowMessageWindow("Format does not match. Please type it again.", "Error!", System.Drawing.SystemIcons.Error, 420, 180);
                        continue;
                    }
                }
                catch
                {
                    ShowMessageWindow("Format does not match. Please type it again.", "Error!", System.Drawing.SystemIcons.Error, 420, 180);
                    continue;
                }

                // モデル名とシリアル番号が変更された場合
                if (orgModelName != newModelName || orgSerialNo != newSerialNo)
                {
                    cabiModelName = convertBytModelName(winCds.txbModelName.Text);
                    cabiSerialNo = convertByteSerialNo(winCds.txbSerialNo.Text);
                    changedCabiData = true;
                }
                else
                { changedCabiData = false; }

                break;
            }

            convertRbinToBin(srcFile, destDir, false, true);

            return true;
        }

        private string convertStrModelName(byte[] srcBytes, int startIndex)
        {
            byte[] destBytes = new byte[32];

            Array.Copy(srcBytes, startIndex, destBytes, 0, 32);

            return System.Text.Encoding.GetEncoding("shift_jis").GetString(destBytes).Trim('\0');
        }

        private byte[] convertBytModelName(string modelName)
        {
            byte[] srcBytes = new byte[32];
            byte[] destBytes = new byte[32];

            srcBytes = System.Text.Encoding.GetEncoding("shift_jis").GetBytes(modelName);

            Array.Copy(srcBytes, 0, destBytes, 0, srcBytes.Length);

            return destBytes;
        }

        private string convertStrSerialNo(byte[] srcBytes, int startIndex)
        {
            int newData;
            int minNum = 0;
            int maxNum = 9999999;
            byte[] destBytes = new byte[4];

            Array.Copy(srcBytes, startIndex, destBytes, 0, 4);

            newData = BitConverter.ToInt32(destBytes, 0);
            if (newData < minNum || newData > maxNum)
            { return ""; }
            else
            { return newData.ToString().PadLeft(7, '0'); }
        }

        private byte[] convertByteSerialNo(string serialNo)
        {
            byte[] newData = new byte[4];

            newData = BitConverter.GetBytes(int.Parse(serialNo));

            return newData;
        }

        private bool mergeUnitData(UnitInfo unit)
        {
            byte[] tempBytes;

            string srcFile = makeFilePath(unit, FileDirectory.Backup_Initial, DataType.UnitData);
            string destFile = makeFilePath(unit, FileDirectory.Backup_Latest, DataType.UnitData);

            byte[] srcBytes = File.ReadAllBytes(srcFile);
            byte[] destBytes = File.ReadAllBytes(destFile);

            // 製品シリアル
            Array.Copy(srcBytes, 8, destBytes, 8, 7);

            // モデルネーム
            Array.Copy(srcBytes, 24, destBytes, 24, 16);

            // 再撮モード選択フラグ, 入力ガンマ情報, 色域調整情報
            Array.Copy(srcBytes, 56, destBytes, 56, 3);

            // Unit Info CheckSum再計算
            byte[] unitInfo = new byte[128];
            Array.Copy(destBytes, 8, unitInfo, 0, 128);

            calcUnitInfoCheckSum(ref unitInfo);

            Array.Copy(unitInfo, 0, destBytes, 8, 128);

            // panel_gamma_table(0)～(7)
            Array.Copy(srcBytes, 94032, destBytes, 94032, 36352);

            // Chech panel_gamma_table checksum
            for (int i = 0; i < 7; i++)
            {
                tempBytes = new byte[4544];
                Array.Copy(destBytes, 94032 + i * 4544, tempBytes, 0, 4544);

                if (compareChecksum(tempBytes, 124) != true)
                { return false; }
            }

            // Digital Cinema対応
            if (appliMode == ApplicationMode.Service
                && (Settings.Ins.PanelType == PanelType.DigitalCinema2017 || Settings.Ins.PanelType == PanelType.DigitalCinema2020))
            { replaceDigitalCinemaData(ref destBytes, srcBytes); }
            
            string tempFile = makeFilePath(unit, FileDirectory.Temp, DataType.UnitData);
            File.WriteAllBytes(tempFile, destBytes);

            return true;
        }

        private bool replaceDigitalCinemaData(ref byte[] destBytes, byte[] srcBytes)
        {
            //string srcFile;
            //byte[] srcBytes;
            byte[] tempBytes;

            //// input_gammma_table_26.bin
            //srcFile = applicationPath + "\\Components\\input_gammma_table_26.bin";
            //srcBytes = File.ReadAllBytes(srcFile);

            //Array.Copy(srcBytes, 0, destBytes, 0x0400, 12416);        

            // input_gammma_table
            Array.Copy(srcBytes, 1024, destBytes, 1024, 12416);

            // Chech input_gammma_table checksum
            tempBytes = new byte[12416];
            Array.Copy(destBytes, 1024, tempBytes, 0, 12416);
            
            if (compareChecksum(tempBytes, 124) != true)
            { return false; }            

            //// Mode3_vsawtable_1-1_120_80.bin
            //srcFile = applicationPath + "\\Components\\Mode3_vsawtable_1-1_120_80.bin";
            //srcBytes = File.ReadAllBytes(srcFile);

            //Array.Copy(srcBytes, 0, destBytes, 0xAC50, 8320);

            //// Mode7_vsawtable_1-1_100_80.bin
            //srcFile = applicationPath + "\\Components\\Mode7_vsawtable_1-1_100_80.bin";
            //srcBytes = File.ReadAllBytes(srcFile);

            //Array.Copy(srcBytes, 0, destBytes, 0x12E50, 8320);

            // vsaw_table
            Array.Copy(srcBytes, 27472, destBytes, 27472, 66560);

            // Check vsaw_table checksum
            for (int i = 0; i < 7; i++)
            {
                tempBytes = new byte[8320];
                Array.Copy(destBytes, 27472 + i * 8320, tempBytes, 0, 8320);

                if (compareChecksum(tempBytes, 124) != true)
                { return false; }
            }   

            // Iref0-11
            int value;
            
            for (int i = 0; i < 12; i++)
            {
                int addr = 512 + 12 + i * 4;
                value = BitConverter.ToInt32(destBytes, addr);

                if (Settings.Ins.PanelType == PanelType.DigitalCinema2017)
                { value += 26; }
                else if (Settings.Ins.PanelType == PanelType.DigitalCinema2020)
                { value += 21; }

                byte[] iref = BitConverter.GetBytes(value);
                Array.Copy(iref, 0, destBytes, addr, 4);
            }

            // Calc uc_board_data CheckSum
            tempBytes = new byte[512];
            Array.Copy(destBytes, 512, tempBytes, 0, 512);

            if (calcUcBoardDataCheckSum(ref tempBytes) != true)
            { return false; }

            Array.Copy(tempBytes, 0, destBytes, 512, 512);

            // Check uc_board_data checksum
            tempBytes = new byte[512];
            Array.Copy(destBytes, 512, tempBytes, 0, 512);

            if (compareChecksum(tempBytes, 508) != true)
            { return false; }

            return true;
        }

        private bool calcUcBoardDataCheckSum(ref byte[] ucBoardData)
        {
            int calcCheckSum = 0;

            for (int i = 0; i < ucBoardData.Length; i += 4)
            {
                // CheckSumの部分はSkip
                if (i == 508)
                { continue; }

                calcCheckSum += BitConverter.ToInt32(ucBoardData, i);
            }

            byte[] checksum = BitConverter.GetBytes(calcCheckSum);
            Array.Copy(checksum, 0, ucBoardData, 508, 4);

            return true;
        }

        private bool uploadFile2Ftp(ControllerInfo controller, UnitInfo unit)
        {
            string srcFile = makeFilePath(unit, FileDirectory.Temp, DataType.UnitData);
            
            try
            {
                if (putFileFtpRetry(controller.IPAddress, srcFile) != true)
                {
                    ShowMessageWindow("Failed Upload File to FTP", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }
            }
            catch
            {
                ShowMessageWindow("Failed Upload File to FTP", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }

            return true;
        }

        private bool uploadFile2Ftp(List<UnitInfo> lstTargetUnit, DataType dataType = DataType.UnitData)
        {
            foreach (UnitInfo unit in lstTargetUnit)
            {
                if (dataType == DataType.UnitData)
                {
                    string srcFile = makeFilePath(unit, FileDirectory.Temp, dataType);
                   
                    try
                    {
                        if (putFileFtpRetry(dicController[unit.ControllerID].IPAddress, srcFile) != true)
                        {
                            ShowMessageWindow("Failed Upload File to FTP", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                            return false;
                        }
                    }
                    catch
                    {
                        ShowMessageWindow("Failed Upload File to FTP", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                        return false;
                    }
                }
                else if (dataType == DataType.HcData)
                {
                    string srcFile = makeFilePath(unit, FileDirectory.Temp, dataType);
                   
                    try
                    {
                        if (putFileFtpRetry(dicController[unit.ControllerID].IPAddress, srcFile) != true)
                        {
                            ShowMessageWindow("Failed Upload File to FTP", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                            return false;
                        }
                    }
                    catch
                    {
                        ShowMessageWindow("Failed Upload File to FTP", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                        return false;
                    }
                }
            }

            return true;
        }

        private void btnCalcCheckSum_Click(object sender, RoutedEventArgs e)
        {
            //string srcFile = @"C:\CAS\Backup\CLF101AKA_0000001_Initial\u0103_ud.bin";

            //byte[] srcBytes = File.ReadAllBytes(srcFile);
            //byte[] unitInfo = new byte[128];

            //Array.Copy(srcBytes, 8, unitInfo, 0, 128);

            //calcUnitInfoCheckSum(ref unitInfo);

            //Array.Copy(unitInfo, 0, srcBytes, 8, 128);

            return;
        }

        private bool calcUnitInfoCheckSum(ref byte[] unitInfo)
        {
            uint bpn, mna, cpa, spi;
            uint checkSum = 0;

            // unit_infoのチェックサム
            checkSum = (uint)((unitInfo[3] << 24) | (unitInfo[2] << 16) | (unitInfo[1] << 8) | (unitInfo[0] << 0));
            checkSum += (uint)((unitInfo[7] << 24) | (unitInfo[6] << 16) | (unitInfo[5] << 8) | (unitInfo[4] << 0));
            checkSum += (uint)((unitInfo[11] << 24) | (unitInfo[10] << 16) | (unitInfo[9] << 8) | (unitInfo[8] << 0));
            checkSum += (uint)((unitInfo[15] << 24) | (unitInfo[14] << 16) | (unitInfo[13] << 8) | (unitInfo[12] << 0));
            checkSum += (uint)((unitInfo[19] << 24) | (unitInfo[18] << 16) | (unitInfo[17] << 8) | (unitInfo[16] << 0));
            checkSum += (uint)((unitInfo[23] << 24) | (unitInfo[22] << 16) | (unitInfo[21] << 8) | (unitInfo[20] << 0));
            checkSum += (uint)((unitInfo[27] << 24) | (unitInfo[26] << 16) | (unitInfo[25] << 8) | (unitInfo[24] << 0));
            checkSum += (uint)((unitInfo[31] << 24) | (unitInfo[30] << 16) | (unitInfo[29] << 8) | (unitInfo[28] << 0));

            bpn = BitConverter.ToUInt32(unitInfo, 32);
            checkSum += bpn;

            mna = BitConverter.ToUInt32(unitInfo, 36);
            checkSum += mna;

            cpa = BitConverter.ToUInt32(unitInfo, 40);
            checkSum += cpa;

            spi = BitConverter.ToUInt32(unitInfo, 44);
            checkSum += spi;

            // photo_mode_flag, input_gamma_info, color_matrix_info
            checkSum += BitConverter.ToUInt32(unitInfo, 48);

            byte[] arySum = BitConverter.GetBytes(checkSum);

            Array.Copy(arySum, 0, unitInfo, 124, 4);

            return true;
        }

        /// <summary>
        /// 対象UnitのAging Dataがない場合、周辺UnitのAging Dataの平均値をとり
        /// Aging Dataを作成する。
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        private bool makeAgingData(UnitInfo targetUnit)
        {
            try
            {
                List<UnitInfo> lstPeripheralUnits;
                UInt32 aveR = 0, aveG = 0, aveB = 0;
                UInt64 sumR = 0, sumG = 0, sumB = 0;

                // 周辺Unitを探索
                searchPeripheralUnits(targetUnit, out lstPeripheralUnits);

                if (lstPeripheralUnits.Count == 0)
                { return false; }
                
                if (Settings.Ins.ReadIndividualUnits == true)
                { backup(BackupMode.Individual, lstPeripheralUnits); }

                // RGBの積算発光時間平均値を計算
                for (int i = 0; i < lstPeripheralUnits.Count; i++)
                {
                    UInt64 red, green, blue;
                    if(calcAverage(lstPeripheralUnits[i], out red, out green, out blue) != true)
                    { return false; }

                    sumR += red;
                    sumG += green;
                    sumB += blue;
                }

                aveR = (UInt32)(sumR / (UInt64)lstPeripheralUnits.Count);
                aveG = (UInt32)(sumG / (UInt64)lstPeripheralUnits.Count);
                aveB = (UInt32)(sumB / (UInt64)lstPeripheralUnits.Count);

                // ファイル作成
                string orgFile = makeFilePath(lstPeripheralUnits[0], FileDirectory.Backup_Latest, DataType.AgingData);
                string tempFile = makeFilePath(targetUnit, FileDirectory.Temp, DataType.AgingData);
                writeAgingData(orgFile, tempFile, aveR, aveG, aveB);
            }
            catch
            { return false; }

            return true;
        }

        private bool searchPeripheralUnits(UnitInfo targetUnit, out List<UnitInfo> lstPeripheralUnits)
        {
            lstPeripheralUnits = new List<UnitInfo>();

            #region Phase 1

            try
            {
                if (allocInfo.lstUnits[targetUnit.X - 1][targetUnit.Y - 2] != null &&
                    checkAgingDataFile(allocInfo.lstUnits[targetUnit.X - 1][targetUnit.Y - 2]) == true)
                { lstPeripheralUnits.Add(allocInfo.lstUnits[targetUnit.X - 1][targetUnit.Y - 2]); }
            }
            catch { }

            try
            {
                if (allocInfo.lstUnits[targetUnit.X][targetUnit.Y - 1] != null &&
                    checkAgingDataFile(allocInfo.lstUnits[targetUnit.X][targetUnit.Y - 1]) == true)
                { lstPeripheralUnits.Add(allocInfo.lstUnits[targetUnit.X][targetUnit.Y - 1]); }
            }
            catch { }

            try
            {
                if (allocInfo.lstUnits[targetUnit.X - 1][targetUnit.Y] != null &&
                    checkAgingDataFile(allocInfo.lstUnits[targetUnit.X - 1][targetUnit.Y]) == true)
                { lstPeripheralUnits.Add(allocInfo.lstUnits[targetUnit.X - 1][targetUnit.Y]); }
            }
            catch { }

            try
            {
                if (allocInfo.lstUnits[targetUnit.X - 2][targetUnit.Y - 1] != null &&
                    checkAgingDataFile(allocInfo.lstUnits[targetUnit.X - 2][targetUnit.Y - 1]) == true)
                { lstPeripheralUnits.Add(allocInfo.lstUnits[targetUnit.X - 2][targetUnit.Y - 1]); }
            }
            catch { }

            if(lstPeripheralUnits.Count != 0)
            { return true; }

            #endregion Phase 1

            #region Phase 2

            try
            {
                if (allocInfo.lstUnits[targetUnit.X][targetUnit.Y - 2] != null &&
                    checkAgingDataFile(allocInfo.lstUnits[targetUnit.X][targetUnit.Y - 2]) == true)
                { lstPeripheralUnits.Add(allocInfo.lstUnits[targetUnit.X][targetUnit.Y - 2]); }
            }
            catch { }

            try
            {
                if (allocInfo.lstUnits[targetUnit.X][targetUnit.Y] != null &&
                    checkAgingDataFile(allocInfo.lstUnits[targetUnit.X][targetUnit.Y]) == true)
                { lstPeripheralUnits.Add(allocInfo.lstUnits[targetUnit.X][targetUnit.Y]); }
            }
            catch { }

            try
            {
                if (allocInfo.lstUnits[targetUnit.X - 2][targetUnit.Y] != null &&
                    checkAgingDataFile(allocInfo.lstUnits[targetUnit.X - 2][targetUnit.Y]) == true)
                { lstPeripheralUnits.Add(allocInfo.lstUnits[targetUnit.X - 2][targetUnit.Y]); }
            }
            catch { }

            try
            {
                if (allocInfo.lstUnits[targetUnit.X - 2][targetUnit.Y - 2] != null &&
                    checkAgingDataFile(allocInfo.lstUnits[targetUnit.X - 2][targetUnit.Y - 2]) == true)
                { lstPeripheralUnits.Add(allocInfo.lstUnits[targetUnit.X - 2][targetUnit.Y - 2]); }
            }
            catch { }

            if (lstPeripheralUnits.Count != 0)
            { return true; }

            #endregion Phase 2

            if (Settings.Ins.ReadIndividualUnits == false)
            {
                #region Phase 3

                try
                {
                    if (allocInfo.lstUnits[targetUnit.X - 1][targetUnit.Y - 3] != null &&
                        checkAgingDataFile(allocInfo.lstUnits[targetUnit.X - 1][targetUnit.Y - 3]) == true)
                    { lstPeripheralUnits.Add(allocInfo.lstUnits[targetUnit.X - 1][targetUnit.Y - 3]); }
                }
                catch { }

                try
                {
                    if (allocInfo.lstUnits[targetUnit.X + 1][targetUnit.Y - 1] != null &&
                        checkAgingDataFile(allocInfo.lstUnits[targetUnit.X + 1][targetUnit.Y - 1]) == true)
                    { lstPeripheralUnits.Add(allocInfo.lstUnits[targetUnit.X + 1][targetUnit.Y - 1]); }
                }
                catch { }

                try
                {
                    if (allocInfo.lstUnits[targetUnit.X - 1][targetUnit.Y + 1] != null &&
                        checkAgingDataFile(allocInfo.lstUnits[targetUnit.X - 1][targetUnit.Y + 1]) == true)
                    { lstPeripheralUnits.Add(allocInfo.lstUnits[targetUnit.X - 1][targetUnit.Y + 1]); }
                }
                catch { }

                try
                {
                    if (allocInfo.lstUnits[targetUnit.X - 3][targetUnit.Y - 1] != null &&
                        checkAgingDataFile(allocInfo.lstUnits[targetUnit.X - 3][targetUnit.Y - 1]) == true)
                    { lstPeripheralUnits.Add(allocInfo.lstUnits[targetUnit.X - 3][targetUnit.Y - 1]); }
                }
                catch { }

                if (lstPeripheralUnits.Count != 0)
                { return true; }

                #endregion Phase 3

                #region Phase 4

                try
                {
                    if (allocInfo.lstUnits[targetUnit.X + 1][targetUnit.Y - 3] != null &&
                        checkAgingDataFile(allocInfo.lstUnits[targetUnit.X + 1][targetUnit.Y - 3]) == true)
                    { lstPeripheralUnits.Add(allocInfo.lstUnits[targetUnit.X + 1][targetUnit.Y - 3]); }
                }
                catch { }

                try
                {
                    if (allocInfo.lstUnits[targetUnit.X + 1][targetUnit.Y + 1] != null &&
                        checkAgingDataFile(allocInfo.lstUnits[targetUnit.X + 1][targetUnit.Y + 1]) == true)
                    { lstPeripheralUnits.Add(allocInfo.lstUnits[targetUnit.X + 1][targetUnit.Y + 1]); }
                }
                catch { }

                try
                {
                    if (allocInfo.lstUnits[targetUnit.X - 3][targetUnit.Y + 1] != null &&
                        checkAgingDataFile(allocInfo.lstUnits[targetUnit.X - 3][targetUnit.Y + 1]) == true)
                    { lstPeripheralUnits.Add(allocInfo.lstUnits[targetUnit.X - 3][targetUnit.Y + 1]); }
                }
                catch { }

                try
                {
                    if (allocInfo.lstUnits[targetUnit.X - 3][targetUnit.Y - 3] != null &&
                        checkAgingDataFile(allocInfo.lstUnits[targetUnit.X - 3][targetUnit.Y - 3]) == true)
                    { lstPeripheralUnits.Add(allocInfo.lstUnits[targetUnit.X - 3][targetUnit.Y - 3]); }
                }
                catch { }

                if (lstPeripheralUnits.Count != 0)
                { return true; }

                #endregion Phase 4
            }

            return true;
        }

        // UnitのRGBの積算発光時間平均値を計算
        private bool calcAverage(UnitInfo unit, out UInt64 aveR, out UInt64 aveG, out UInt64 aveB)
        {
            UInt64 sumR = 0, sumG = 0, sumB = 0;
            string agingFile = makeFilePath(unit, FileDirectory.Backup_Latest, DataType.AgingData);

            aveR = 0;
            aveG = 0;
            aveB = 0;

            if (File.Exists(agingFile) != true)
            { return false; }

            byte[] srcBytes = File.ReadAllBytes(agingFile);
            byte[] tempBytes = new byte[0x20000];

            // CellごとにLoop
            for (int i = 0; i < 12; i++)
            {
                Array.Copy(srcBytes, 0x20000 * i, tempBytes, 0, 0x20000);

                calcAgingTime(tempBytes, out sumR, out sumG, out sumB);

                aveR += sumR;
                aveG += sumG;
                aveB += sumB;
            }

            aveR = aveR / 115200; // 320 * 360 Cellの全画素数
            aveG = aveG / 115200;
            aveB = aveB / 115200;

            return true;
        }

        private bool calcAgingTime(byte[] agingData, out UInt64 sumR, out UInt64 sumG, out UInt64 sumB)
        {
            sumR = 0;
            sumG = 0;
            sumB = 0;

            for (int i = 0x200; i < 115712; i += 12) // RGB合計で12Byte, 115712 = stuffing_bits開始アドレス
            {
                sumR += (uint)((agingData[i + 3] << 24) | (agingData[i + 2] << 16) | (agingData[i + 1] << 8) | (agingData[i] << 0));
                sumG += (uint)((agingData[i + 7] << 24) | (agingData[i + 6] << 16) | (agingData[i + 5] << 8) | (agingData[i + 4] << 0));
                sumB += (uint)((agingData[i + 11] << 24) | (agingData[i + 10] << 16) | (agingData[i + 9] << 8) | (agingData[i + 8] << 0));
            }

            return true;
        }

        private bool writeAgingData(string srcFile, string tempFile, UInt32 red, UInt32 green, UInt32 blue)
        {
            byte[] srcBytes = File.ReadAllBytes(srcFile);
            byte[] tempBytes = new byte[0x20000];

            for (int i = 0; i < 12; i++)
            {
                Array.Copy(srcBytes, 0x20000 * i, tempBytes, 0, 0x20000);
                
                // 積算発光時間を設定
                for (int pos = 512; pos < 115712; pos += 12)
                {
                    // Red
                    byte[] aryAve = BitConverter.GetBytes(red);
                    Array.Copy(aryAve, 0, tempBytes, pos, 4);

                    // Green
                    aryAve = BitConverter.GetBytes(green);
                    Array.Copy(aryAve, 0, tempBytes, pos + 4, 4);

                    // Blue
                    aryAve = BitConverter.GetBytes(blue);
                    Array.Copy(aryAve, 0, tempBytes, pos + 8, 4);
                }

                // CheckSum再計算
                calcAgingDataCheckSum(ref tempBytes);

                // srcBytesに書き戻し
                Array.Copy(tempBytes, 0, srcBytes, 0x20000 * i, 0x20000);
            }

            File.WriteAllBytes(tempFile, srcBytes);

            return true;
        }

        #endregion Replace

        #region Recovery

        private bool recoveryCabinet()
        {
            if (Settings.Ins.ExecLog == true)
            {
                SaveExecLog("");
                SaveExecLog("[0] recoveryCabinet start");
            }

            var dispatcher = Application.Current.Dispatcher;
            List<ControllerInfo> lstTargetController = new List<ControllerInfo>();
            List<UnitInfo> lstTargetUnit = new List<UnitInfo>();
            List<string> problemFile= new List<string>();
            List<string> allProblemFile = new List<string>();
            List<string> problemModule = new List<string>();
            List<string> allProblemModule = new List<string>();

            // ●進捗の最大値を設定
            winProgress.ShowMessage("Set Step Count.");
            int step = RECOVERY_CABINET_STEPS;
            winProgress.SetWholeSteps(step);

            // ●調整をするCabinetをListに格納 [1]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[1] Store Target Cabinet(s) Info."); }
            winProgress.ShowMessage("Store Target Cabinet(s) Info.");

            dispatcher.Invoke(() => correctDataUnit(out lstTargetUnit, allocInfo.MaxX, allocInfo.MaxY));
            if (lstTargetUnit.Count == 0)
            {
                ShowMessageWindow("Please select cabinet.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●Power OnでないときはOnにする [2]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[2] Check Controller Power."); }
            winProgress.ShowMessage("Check Controller Power.");

            if (!setAllControllerPowerOn())
            { return false; }
            winProgress.PutForward1Step();

            // ●hc.binファイル生成 [3]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[3] Create Uniformity High Data."); }
            winProgress.ShowMessage("Create Uniformity High Data.");

            int lstCount = lstTargetUnit.Count;
            for (int i = 0; i < lstTargetUnit.Count; i++)
            {
                if (createWriteHcData(lstTargetUnit[i], out problemFile, out problemModule, true) != true)
                { return false; }

                if (problemFile.Count > 0)
                {
                    // 選択されたCabinetのhf/hcファイルがない場合、lstTargetUnitリストから削除する
                    lstTargetUnit.Remove(lstTargetUnit[i]);

                    foreach (string pf in problemFile)
                    { allProblemFile.Add(pf); }
                }

                if (problemModule.Count > 0)
                {
                    foreach (string pm in problemModule)
                    { allProblemModule.Add(pm); }
                }
            }

            if (allProblemFile.Count == lstCount)
            {
                string msg = "The following files doesn't exist.\r\n";
                foreach (string str in allProblemFile)
                { msg += str + "\r\n"; }

                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
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

            #region Controller Settings

            foreach (ControllerInfo controller in lstTargetController)
            {
                // ●FTP ON [*1]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*1] FTP On."); }
                winProgress.ShowMessage("FTP On.");

                if (sendSdcpCommand(SDCPClass.CmdFtpOn, controller.IPAddress) != true)
                { return false; }

                // ●Model名取得 [*2]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*2] Get Model Name."); }
                winProgress.ShowMessage("Get Model Name.");

                if (string.IsNullOrWhiteSpace(controller.ModelName) == true)
                { controller.ModelName = getModelName(controller.IPAddress); }

                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("     Model Name : " + controller.ModelName); }

                // ●Serial取得 [*3]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*3] Get Serial No."); }
                winProgress.ShowMessage("Get Serial No.");

                if (string.IsNullOrWhiteSpace(controller.SerialNo) == true)
                { controller.SerialNo = getSerialNo(controller.IPAddress); }

                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("     Serial Num. : " + controller.SerialNo); }
            }
            winProgress.PutForward1Step();

            #endregion Controller Settings

            // ●TempフォルダからFTPへコピー [5]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[5] Upload Files to FTP."); }
            winProgress.ShowMessage("Upload Files to FTP.");

            if (uploadFile2Ftp(lstTargetUnit, DataType.HcData) != true)
            { return false; }
            winProgress.PutForward1Step();

#if NO_WRITE
#else
            // ●Cabinet Power Off [6]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[6] Cabinet Power Off."); }
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

            // ●Reconfig [7]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[7] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            sendReconfig();
            winProgress.PutForward1Step();

            // ●書き込みコマンド発行 [8]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[8] Send Write Command."); }
            winProgress.ShowMessage("Send Write Command.");

            foreach (ControllerInfo cont in lstTargetController)
            { sendSdcpCommand(SDCPClass.CmdDataWrite, cont.IPAddress); }
            winProgress.PutForward1Step();

            // ●Complete待ち [9]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[9] Waiting for the response."); }
            winProgress.ShowMessage("Waiting for the response.");
            winProgress.ElapsedTimer(true);

            bool[] complete = new bool[lstTargetController.Count];

            try
            {
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
            { return false; }

            winProgress.ElapsedTimer(false);
            winProgress.PutForward1Step();

            // ●Latest → Previousフォルダへコピー [10]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[10] Move File(s). (Latest -> Previous)."); }
            winProgress.ShowMessage("Move File(s). (Latest -> Previous)");

            foreach (ControllerInfo controller in lstTargetController)
            { copyLatest2Previous(applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo); }
            winProgress.PutForward1Step();

            // ●Temp → Latestフォルダへコピー [11]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[11] Move File(s). (Temp -> Latest)."); }
            winProgress.ShowMessage("Move File(s). (Temp -> Latest).");

            foreach (ControllerInfo controller in lstTargetController)
            { copyTemp2Latest(applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo); }
            winProgress.PutForward1Step();

            // ●Reconfig [12]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[12] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            sendReconfig();
            winProgress.PutForward1Step();
#endif

            // ●Tempフォルダのファイルを削除 [13]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[13] Delete Temp File(s)."); }
            winProgress.ShowMessage("Delete Temp File(s).");

            foreach (ControllerInfo controller in lstTargetController)
            {
                string tempPath = applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo + "_Temp";

                string[] files = Directory.GetFiles(tempPath, "*", System.IO.SearchOption.AllDirectories);

                for (int i = 0; i < files.Length; i++)
                {
                    try { File.Delete(files[i]); }
                    catch { return false; }
                }
            }
            winProgress.PutForward1Step();

            // ●FTP Off [14]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[14] FTP Off."); }
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

            // ●Cabinet Power On [15]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[15] Cabinet Power On."); }
            winProgress.ShowMessage("Cabinet Power On.");

            foreach (ControllerInfo controller in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdUnitPowerOn, SDCP_COMMAND_WAIT_TIME, controller.IPAddress); }
            winProgress.PutForward1Step();

            // ファイルがない、又はデータ欠落された場合
            if (allProblemFile.Count > 0 || allProblemModule.Count > 0)
            {
                string problemFileMsg = "The following files doesn't exist.\r\n";
                string problemModuleMsg = "The following Cabinet Module Uniformity Data could not be recovered.\r\n";
                string msg = "";

                if (allProblemFile.Count > 0)
                {
                    msg += problemFileMsg;
                    foreach (string str in allProblemFile)
                    { msg += str + "\r\n"; }

                    msg += "\r\n";
                }

                if (allProblemModule.Count > 0)
                {
                    msg += problemModuleMsg;
                    foreach (string str in allProblemModule)
                    { msg += str + "\r\n"; }
                }

                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
            }

            return true;
        }

        private bool recoveryModule()
        {
            if (Settings.Ins.ExecLog == true)
            {
                SaveExecLog("");
                SaveExecLog("[0] recoveryModule start");
            }

            var dispatcher = Application.Current.Dispatcher;
            ControllerInfo controller;
            List<UnitInfo> lstTargetUnit = new List<UnitInfo>();
            UnitInfo targetUnit;
            List<int> lstTargetModule = new List<int>();
            List<string> problemFile = new List<string>();
            List<string> problemModule = new List<string>();

            // ●進捗の最大値を設定
            winProgress.ShowMessage("Set Step Count.");
            int step = RECOVERY_MODULE_STEPS;
            winProgress.SetWholeSteps(step);

            // ●調整をするCabinetをListに格納 [1]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[1] Store Target Cabinet Info."); }
            winProgress.ShowMessage("Store Target Cabinet Info.");

            dispatcher.Invoke(() => correctDataUnit(out lstTargetUnit, allocInfo.MaxX, allocInfo.MaxY)); // Target Cabinet
            if (lstTargetUnit.Count == 0)
            {
                ShowMessageWindow("Please select cabinet.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            else if (lstTargetUnit.Count > 1)
            {
                ShowMessageWindow("Plural cabinets are selected.\r\nPlease select only one cabinet in Module Replace.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }

            dispatcher.Invoke(() => checkTargetDataCell(out lstTargetModule)); // Target Module
            if (lstTargetModule.Count == 0)
            {
                ShowMessageWindow("Please select target module.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            targetUnit = lstTargetUnit[0];
            winProgress.PutForward1Step();

            // ●調整をするCabinetが接続されているControllerを格納 [2]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[2] Store Target Controller Info."); }
            winProgress.ShowMessage("Store Target Controller Info.");

            // 初期化
            foreach (ControllerInfo cont in dicController.Values)
            { cont.Target = false; }

            dicController[targetUnit.ControllerID].Target = true;
            controller = dicController[targetUnit.ControllerID];    // 対象Controller

            winProgress.PutForward1Step();

            // ●Power OnでないときはOnにする [3]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[3] Check Controller Power."); }
            winProgress.ShowMessage("Check Controller Power.");

            if (!setAllControllerPowerOn())
            { return false; }
            winProgress.PutForward1Step();

            #region Controller Settings

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

            // ●Tempフォルダ内のファイルを削除 [*4]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*4] Delete Temp Files."); }
            winProgress.ShowMessage("Delete Temporary Files.");

            string tempPath = applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo + "_Temp";

            if (Directory.Exists(tempPath) != true)
            { Directory.CreateDirectory(tempPath); }

            string[] files = Directory.GetFiles(tempPath, "*", System.IO.SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                try { File.Delete(files[i]); }
                catch { return false; }
            }
            winProgress.PutForward1Step();

            #endregion Controller Settings

            // ●hc.binファイル生成 [4]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[4] Create Uniformity High Data."); }
            winProgress.ShowMessage("[4] Create Uniformity High Data.");
            if (createWriteHcData(targetUnit, out problemFile, out problemModule, false, lstTargetModule) != true)
            { return false; }

            if (problemFile.Count > 0)
            {
                string msg = "The following files doesn't exist.\r\n";
                foreach (string str in problemFile)
                { msg += str + "\r\n"; }

                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }

            if (problemModule.Count == lstTargetModule.Count)
            {
                string msg = "The following Cabinet Module Uniformity Data could not be recovered.\r\n";
                foreach (string str in problemModule)
                { msg += str + "\r\n"; }

                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●TempフォルダからFTPへコピー [5]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[5] Upload Files to FTP."); }
            winProgress.ShowMessage("Upload Files to FTP.");

            if (uploadFile2Ftp(lstTargetUnit, DataType.HcData) != true)
            { return false; }
            winProgress.PutForward1Step();

#if NO_WRITE
#else
            // ●Cabinet Power Off [6]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[6] Cabinet Power Off."); }
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

            // ●Reconfig [7]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[7] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            sendReconfig();
            winProgress.PutForward1Step();

            // ●書き込みコマンド発行 [8]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[8] Send Write Command."); }
            winProgress.ShowMessage("Send Write Command.");

            sendSdcpCommand(SDCPClass.CmdDataWrite, controller.IPAddress);
            winProgress.PutForward1Step();

            // ●Complete待ち [9]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[9] Waiting for the response."); }
            winProgress.ShowMessage("Waiting for the response.");
            winProgress.ElapsedTimer(true);
            try
            {
                while (true)
                {
                    if (checkCompleteFtp(controller.IPAddress, "write_complete") == true)
                    { break; }

                    winProgress.ShowMessage("Waiting for the process of controller.");

                    System.Threading.Thread.Sleep(1000);
                }
            }
            catch
            { return false; }
            
            winProgress.ElapsedTimer(false);
            winProgress.PutForward1Step();

            // ●Latest → Previousフォルダへコピー [10]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[10] Move File(s). (Latest -> Previous)."); }
            winProgress.ShowMessage("Move File(s). (Latest -> Previous)");

            copyLatest2Previous(applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo);
            winProgress.PutForward1Step();

            // ●Temp → Latestフォルダへコピー [11]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[11] Move File(s). (Temp -> Latest)."); }
            winProgress.ShowMessage("Move File(s). (Temp -> Latest).");

            copyTemp2Latest(applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo);
            winProgress.PutForward1Step();

            // ●Reconfig [12]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[12] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            sendReconfig();
            winProgress.PutForward1Step();
#endif
            // ●Tempフォルダのファイルを削除 [13]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[13] Delete Temp File(s)."); }
            winProgress.ShowMessage("Delete Temp File(s).");

            files = Directory.GetFiles(tempPath, "*", System.IO.SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                try { File.Delete(files[i]); }
                catch { return false; }
            }
            winProgress.PutForward1Step();

            // ●FTP Off [14]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[14] FTP Off."); }
            winProgress.ShowMessage("FTP Off.");

            if (sendSdcpCommand(SDCPClass.CmdFtpOff, controller.IPAddress) != true)
            {
                ShowMessageWindow("Failed to FTP Off.", "Error!", System.Drawing.SystemIcons.Error, 420, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●Cabinet Power On[15]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[15] Cabinet Power On."); }
            winProgress.ShowMessage("Cabinet Power On.");

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdUnitPowerOn, SDCP_COMMAND_WAIT_TIME, cont.IPAddress); }
            winProgress.PutForward1Step();

            // データ欠落された場合
            if (problemModule.Count > 0)
            {
                string problemModuleMsg = "The following Cabinet Module Uniformity Data could not be recovered.\r\n";
                foreach (string str in problemModule)
                { problemModuleMsg += str + "\r\n"; }

                ShowMessageWindow(problemModuleMsg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
            }

            return true;
        }

        private bool createWriteHcData(UnitInfo unit, out List<string> problemFile, out List<string> problemModule, bool isRecoveryCabinetMode, List<int> lstTargetModule = null)
        {
            byte[] srcBytes;
            string hfFilePath, hcFilePath, destPath;
            problemFile = new List<string>();
            problemModule = new List<string>();
            List<string> problemModuleData = new List<string>();

            try
            {
                hfFilePath = makeFilePath(unit, FileDirectory.Backup_Rbin, DataType.HfData);
                if (File.Exists(hfFilePath))
                {
                    hcFilePath = makeFilePath(unit, FileDirectory.Backup_Latest, DataType.HcData);
                    if (File.Exists(hcFilePath))
                    {
                        srcBytes = File.ReadAllBytes(hcFilePath);
                        destPath = makeFilePath(unit, FileDirectory.Temp, DataType.HcData);

                        if (isRecoveryCabinetMode) // Recovery Cabinet
                        {
                            lstTargetModule = new List<int>();
                            for(int i = 0; i < moduleCount; i++)
                            { lstTargetModule.Add(i); }

                            recoveryCabinetDataMerge(unit, hfFilePath, hcFilePath, out List<CompositeConfigData> newHcData, out problemModuleData);
                            convWriteNewHcData(newHcData, srcBytes, out byte[] destBytes, lstTargetModule);
                            File.WriteAllBytes(destPath, destBytes);
                        }
                        else // Recovery Module
                        {
                            recoveryModuleDataMerge(unit, hfFilePath, hcFilePath, lstTargetModule, out List<CompositeConfigData> newHcData, out problemModuleData);
                            convWriteNewHcData(newHcData, srcBytes, out byte[] destBytes, lstTargetModule);
                            File.WriteAllBytes(destPath, destBytes);
                        }
                    }
                    else
                    { problemFile.Add(hcFilePath); }
                }
                else
                { problemFile.Add(hfFilePath); }

                problemModule = problemModuleData;
            }
            catch
            { return false; }

            return true;
        }

        private void recoveryCabinetDataMerge(UnitInfo unit, string hfFilePath, string hcFilePath, out List<CompositeConfigData> newHcData, out List<string> problemModuleData)
        {
            problemModuleData = new List<string>();

            List<CompositeConfigData> lstHfData = CompositeConfigData.getCompositeConfigDataList(hfFilePath, dt_ColorCorrectionRead);
            List<CompositeConfigData> lstHcData = CompositeConfigData.getCompositeConfigDataList(hcFilePath, dt_ColorCorrectionWrite);

            for (int x = 0; x < lstHcData.Count; x++)
            {
                int count = 0;
                for (int y = 0; y < lstHfData.Count; y++)
                {
                    if (lstHcData[x].header.Option1 == lstHfData[y].header.Option1)
                    {
                        if (lstHcData[x].header.DataLength == lstHfData[y].header.DataLength)
                        {
                            lstHcData[x].header = lstHfData[y].header;
                            lstHcData[x].data = lstHfData[y].data;
                            break;
                        }
                        else
                        {
                            problemModuleData.Add("C" + unit.ControllerID + "-" + unit.PortNo + "-" + unit.UnitNo + " No." + lstHcData[x].header.Option1);
                            break;
                        }
                    }
                    count++;

                    if (lstHfData.Count == count)
                    { problemModuleData.Add("C" + unit.ControllerID + "-" +unit.PortNo + "-" + unit.UnitNo + " No." + lstHcData[x].header.Option1); }
                }
            }

            newHcData = lstHcData;
        }

        private void recoveryModuleDataMerge(UnitInfo unit, string hfFilePath, string hcFilePath, List<int> lstTargetModule, out List<CompositeConfigData> newHcData, out List<string> problemModuleData)
        {
            problemModuleData = new List<string>();

            List<CompositeConfigData> lstHfData = CompositeConfigData.getCompositeConfigDataList(hfFilePath, dt_ColorCorrectionRead);
            List<CompositeConfigData> lstHcData = CompositeConfigData.getCompositeConfigDataList(hcFilePath, dt_ColorCorrectionWrite);
            List<CompositeConfigData> targetData = new List<CompositeConfigData>();

            // 選択されたModuleがhf.rbinに存在するかチェックする
            // 存在したらtargetDataリストにコピーする
            for (int x = 0; x < lstTargetModule.Count; x++)
            {
                int count = 0;
                for (int y = 0; y < lstHfData.Count; y++)
                {
                    if ((lstTargetModule[x] + 1) == lstHfData[y].header.Option1)
                    {
                        targetData.Add(lstHfData[y]);
                        break;
                    }
                    count++;

                    if (lstHfData.Count == count)
                    { problemModuleData.Add("C" + unit.ControllerID + "-" + unit.PortNo + "-" + unit.UnitNo + " No." + (lstTargetModule[x] + 1)); }

                }
            }

            // hc.binと同PositionのModuleと比較する
            for (int x = 0; x < targetData.Count; x++)
            {
                for (int y = 0; y < lstHcData.Count; y++)
                {
                    if (targetData[x].header.Option1 == lstHcData[y].header.Option1)
                    {
                        if (targetData[x].header.DataLength == lstHcData[y].header.DataLength)
                        {
                            lstHcData[y].header = targetData[x].header;
                            lstHcData[y].data = targetData[x].data;
                            break;
                        }
                        else
                        {
                            problemModuleData.Add("C" + unit.ControllerID + "-" + unit.PortNo + "-" + unit.UnitNo + " No." + (lstTargetModule[x] + 1));
                            break;
                        }
                    }
                }
            }

            newHcData = lstHcData;
        }


        #endregion Recovery

        #region ConrollerDBBackup

        private bool controllerDBBackup(string ip)
        {
            var dispatcher = Application.Current.Dispatcher;
            List<string> lstFtpFiles;
            string destDir;
            int StandbyWait = 0;
            int step = CONTROLLER_BACKUP_STEPS;
            winProgress.SetWholeSteps(step);

            if (Settings.Ins.ExecLog == true)
            {
                SaveExecLog("");
                SaveExecLog("[0] Controller data backup start");
            }

            // [1]StandbyでないときはStandbyにする
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[1] Controller to standby"); }
            winProgress.ShowMessage("Controller to standby.");

            PowerStatus status;
            if (getPowerStatus(ip, out status) != true)
            { return false; }

            if (status != PowerStatus.Standby)
            {
                if (sendSdcpCommand(SDCPClass.CmdStandby, ip) != true)
                { return false; }
            }

            while (StandbyWait < 90)
            {
                if (getPowerStatus(ip, out status) != true)
                { return false; }

                if (status == PowerStatus.Standby)
                { break; }
                StandbyWait++;
                System.Threading.Thread.Sleep(1000);
            }

            if (status != PowerStatus.Standby)
            { return false; }
            winProgress.PutForward1Step();

            // [2]FTPモード起動
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[2] FTP On"); }
            winProgress.ShowMessage("FTP On.");

            if (sendSdcpCommand(SDCPClass.CmdFtpOn, ip) != true)
            { return false; }
            winProgress.PutForward1Step();

            // [3]FTPからファイルリスト取得
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[3] Get ListDirectory"); }
            winProgress.ShowMessage("Get ListDirectory.");

            if (getControllerFileListFtp(ip, out lstFtpFiles) != true)
            { return false; }
            winProgress.PutForward1Step();

            // [4]Directory作成
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[4] Creating directory"); }
            winProgress.ShowMessage("Creating directory.");

            destDir = applicationPath + "\\ControllerDataBackup\\" + getModelName(ip) + "_" + getSerialNo(ip);
            if (Directory.Exists(destDir) != true)
            { Directory.CreateDirectory(destDir); }
            else
            {
                // 存在時削除してから作成
                string[] files = Directory.GetFiles(destDir);
                foreach (string file in files)
                { File.Delete(file); }

                Directory.CreateDirectory(destDir);
            }
            winProgress.PutForward1Step();

            // [5]データバックアップ
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[5] Backup controller data files"); }
            winProgress.ShowMessage("Backup controller data files.");
            foreach (string file in lstFtpFiles)
            {
                if (file == CLED_CONTROLLER_PERMANENT_DB ||
                    file == VIDEO_PROCESSOR_PERMANENT_DB ||
                    file == CHANNEL_MEMORY_PERMANENT_DB ||
                    file == PRODUCTINFOPERMANENT)
                {
                    if (downControllerFileFtp(ip, file, destDir) != true)
                    { return false; }
                }
            }
            winProgress.PutForward1Step();

            // [6]FTPモード終了
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[6] FTP Off"); }
            winProgress.ShowMessage("FTP Off.");

            if (sendSdcpCommand(SDCPClass.CmdFtpOff, ip) != true)
            { return false; }
            winProgress.PutForward1Step();

            return true;
        }

        private bool getControllerFileListFtp(string ip, out List<string> lstFiles)
        {
            string pass = makeNewFtpPassword(ip);
            Uri url = new Uri("ftp://" + ip + ":21/");

            lstFiles = new List<string>();

            for (int i = 0; i < FTP_RETRY_COUNT; i++)
            {
                System.Net.FtpWebRequest ftpReq = (System.Net.FtpWebRequest)System.Net.WebRequest.Create(url);

                ftpReq.Credentials = new System.Net.NetworkCredential(ctlFtpUser, pass);
                ftpReq.Method = System.Net.WebRequestMethods.Ftp.ListDirectory;
                ftpReq.KeepAlive = false;
                ftpReq.UsePassive = true;
                ftpReq.Proxy = null;
                ftpReq.Timeout = 30000;

                try
                {
                    using (System.Net.FtpWebResponse ftpRes = (System.Net.FtpWebResponse)ftpReq.GetResponse())
                    {
                        string res;
                        using (System.IO.StreamReader sr = new System.IO.StreamReader(ftpRes.GetResponseStream()))
                        { res = sr.ReadToEnd(); }

                        string[] lines = res.Split(new string[] { "\r", "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string line in lines)
                        { lstFiles.Add(Path.GetFileName(line.Trim())); }

                        return true;
                    }
                }
                catch (System.Net.WebException e)
                {
                    if (Settings.Ins.ExecLog == true)
                    {
                        SaveExecLog("        ListDirectory failed");
                        SaveExecLog("        IP: " + ip);
                        SaveExecLog("        ErrMessage: " + e.Message);
                        SaveExecLog("        ErrStatus: " + e.Status);
                    }

                    System.Threading.Thread.Sleep(FTP_RETRY_SLEEP_TIME);
                }
            }
            
            return false;
        }

        private bool downControllerFileFtp(string ip, string file, string destDir)
        {
            string pass = makeNewFtpPassword(ip);
            Uri url = new Uri("ftp://" + ip + ":21/");

            for (int i = 0; i < FTP_RETRY_COUNT; i++)
            {
                try
                {
                    using (System.Net.WebClient wc = new System.Net.WebClient())
                    {
	                    wc.Credentials = new System.Net.NetworkCredential(ctlFtpUser, pass);
	                    wc.DownloadFile(url + file, destDir + "\\" + file + ".rstr");
                    }

                    return true;
                }
                catch (System.Net.WebException e)
                {
                    if (Settings.Ins.ExecLog == true)
                    {
                        SaveExecLog("        Download file failed");
                        SaveExecLog("        IP: " + ip);
                        SaveExecLog("        ErrMessage: " + e.Message);
                        SaveExecLog("        ErrStatus: " + e.Status);
                    }

                    System.Threading.Thread.Sleep(FTP_RETRY_SLEEP_TIME);
                }
            }

            return false;
        }

        #endregion ConrollerDBBackup

        #region ConrollerDBRestore

        private bool controllerDBRestore(string ip)
        {
            var dispatcher = Application.Current.Dispatcher;
            int StandbyWait = 0;
            string checkDir;
            string destDir;
            string[] backFiles;
            int step = CONTROLLER_RESTORE_STEPS;
            List<string> restoreFiles = new List<string>();
            winProgress.SetWholeSteps(step);

            if (Settings.Ins.ExecLog == true)
            {
                SaveExecLog("");
                SaveExecLog("[0] Controller data restore start");
            }

            // [1]ControllerDataBackupディレクトリあるかチェック
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[1] Check backup controller data."); }
            winProgress.ShowMessage("Check backup controller data.");

            checkDir = applicationPath + "\\ControllerDataBackup\\";
            destDir = applicationPath + "\\ControllerDataBackup\\" + getModelName(ip) + "_" + getSerialNo(ip);
            if (!Directory.Exists(checkDir) || !Directory.Exists(destDir))
            { return false; }

            backFiles = Directory.GetFiles(destDir);

            foreach (string backFile in backFiles)
            {
                string filename = Path.GetFileName(backFile);
                if (filename == CLED_CONTROLLER_PERMANENT_DB + ".rstr" ||
                    filename == VIDEO_PROCESSOR_PERMANENT_DB + ".rstr" ||
                    filename == CHANNEL_MEMORY_PERMANENT_DB + ".rstr" ||
                    filename == PRODUCTINFOPERMANENT + ".rstr")
                {
                    restoreFiles.Add(filename);
                }
            }

            if (restoreFiles.Count == 0)
            { return false; }
            winProgress.PutForward1Step();

            // [2]StandbyでないときはStandbyにする
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[2] Controller to standby."); }
            winProgress.ShowMessage("Controller to standby.");
            PowerStatus status;
            if (getPowerStatus(ip, out status) != true)
            { return false; }

            if (status != PowerStatus.Standby)
            {
                if (sendSdcpCommand(SDCPClass.CmdStandby, ip) != true)
                { return false; }
            }

            while (StandbyWait < 90)
            {
                if (getPowerStatus(ip, out status) != true)
                { return false; }

                if (status == PowerStatus.Standby)
                { break; }
                StandbyWait++;
                System.Threading.Thread.Sleep(1000);
            }

            if (status != PowerStatus.Standby)
            { return false; }
            winProgress.PutForward1Step();

            // [3]FTPモード起動
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[3] FTP On."); }
            winProgress.ShowMessage("FTP On.");

            if (sendSdcpCommand(SDCPClass.CmdFtpOn, ip) != true)
            { return false; }
            winProgress.PutForward1Step();

            // [4]バックアップデータをアップロード
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[4] Backup controller data upload."); }
            winProgress.ShowMessage("Backup controller data upload.");

            foreach (string restoreFile in restoreFiles)
            {
                string file = Path.GetFileName(restoreFile);
                if (uploadControllerFileFtp(ip, file, destDir + "\\" + file) != true)
                { return false; }
            }
            winProgress.PutForward1Step();

            // [5]リストア処理を実行するコマンド送信
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[5] Send restore command."); }
            winProgress.ShowMessage("Send restore command.");
            if (sendSdcpCommand(SDCPClass.CmdDataRestore, ip) != true)
            { return false; }
            winProgress.PutForward1Step();

            // [6]CPU側のデータマージ処理終了待ち
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[6] Waiting for the response."); }
            winProgress.ShowMessage("Waiting for the response.");

            string fileName = "";
            int count = 0;
            while (count < 60)
            {
                winProgress.ShowMessage("Waiting for the process of controller.");

                fileName = checkFileExist(ip);
                if (fileName == "restore_complete" || fileName == "restore_fail")
                {
                    break;
                }
                count++;
                System.Threading.Thread.Sleep(1000);
            }
            winProgress.PutForward1Step();

            // [7]FTPモード終了
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[7] FTP Off."); }
            winProgress.ShowMessage("FTP Off.");

            if (sendSdcpCommand(SDCPClass.CmdFtpOff, ip) != true)
            { return false; }
            winProgress.PutForward1Step();

            if (fileName == "" || fileName == "restore_fail")
            { return false; }

            return true;
        }

        private bool uploadControllerFileFtp(string ip, string file, string uploadFile)
        {
            string pass = makeNewFtpPassword(ip);

            Uri url = new Uri("ftp://" + ip + ":21/");

            for (int i = 0; i < FTP_RETRY_COUNT; i++)
            {
                try
                {
                    using (System.Net.WebClient wc = new System.Net.WebClient())
                    {
	                    wc.Credentials = new System.Net.NetworkCredential(ctlFtpUser, pass);
	                    wc.UploadFile(url + file, uploadFile);
                    }

                    return true;
                }
                catch (System.Net.WebException e)
                {
                    if (Settings.Ins.ExecLog == true)
                    {
                        SaveExecLog("        Upload file failed");
                        SaveExecLog("        IP: " + ip);
                        SaveExecLog("        ErrMessage: " + e.Message);
                        SaveExecLog("        ErrStatus: " + e.Status);
                    }

                    System.Threading.Thread.Sleep(FTP_RETRY_SLEEP_TIME);
                }
            }

            return false;
        }

        private string checkFileExist(string ip)
        {
            List<string> lstFiles;
            string filename = "";

            if (getControllerFileListFtp(ip, out lstFiles) != true)
            { return ""; }

            foreach (string file in lstFiles)
            {
                if (file == "restore_complete")
                {
                    filename = file;
                }
                else if (file == "restore_fail")
                {
                    filename = file;
                }
            }
            return filename;
        }



        #endregion ConrollerDBRestore

        #endregion Private Methods
    }
}
