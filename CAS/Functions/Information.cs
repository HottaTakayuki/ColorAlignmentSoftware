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
using System.IO;

namespace CAS
{
    // Information
    public partial class MainWindow : Window
    {
        #region Fields

        private bool infoTabFirstSelect = true;
        private const int TXRX_TOTAL_COUNT = 24;
        private const int OPT_TX = 0;
        private const int OPT_RX = 1;
        private const int UNIT_INFO_ITEM = 4;


        #endregion Fields

        #region Events

        private async void btnContInfo_Click(object sender, RoutedEventArgs e)
        {
            bool status;

            if (dgController.SelectedItem != null)
            {
                //int id = Convert.ToInt32(((TextBlock)dgController.Columns[1].GetCellContent(dgController.SelectedItem)).Text);

                ControllerInfo targetCont = null;

                foreach (ControllerInfo cont in dicController.Values)
                {
                    if (cont.Select == true)
                    {
                        targetCont = cont;
                        break;
                    }
                }

                if (targetCont == null)
                { return; }

                WindowControllerInfo winContInfo = new WindowControllerInfo(targetCont);
                winContInfo.Owner = this;
                winContInfo.Show();

                tcMain.IsEnabled = false;

                try { status = await Task.Run(() => getControllerInfo(targetCont)); }
                catch (Exception ex)
                { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

                tcMain.IsEnabled = true;
            }
        }

        private async void btnContError_Click(object sender, RoutedEventArgs e)
        {
            bool status;

            if (dgController.SelectedItem != null)
            {
                ControllerInfo targetCont = null;

                foreach (ControllerInfo cont in dicController.Values)
                {
                    if (cont.Select == true)
                    {
                        targetCont = cont;
                        break;
                    }
                }

                if (targetCont == null)
                { return; }

                WindowControllerError winContError = new WindowControllerError(targetCont);
                winContError.Owner = this;
                winContError.Show();

                tcMain.IsEnabled = false;

                try { status = await Task.Run(() => getControllerError(targetCont)); }
                catch (Exception ex)
                { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

                tcMain.IsEnabled = true;
            }
        }

        private void btnReloadInfo_Click(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Reload");

            getControllerStatus();

            releaseButton(sender);
        }

        private async void btnPanelInfo_Click(object sender, RoutedEventArgs e)
        {
            bool status = false;
            UnitInfo targetUnit;

            correctTargetUnitInfo(out targetUnit);

            if (targetUnit == null)
            {
                ShowMessageWindow("Please select target cabinet.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return;
            }

            winProgress = new WindowProgress("Panel Information Progress");
            winProgress.Show();

            WindowUnitInfo winUnitInfo = new WindowUnitInfo(targetUnit);
            winUnitInfo.Owner = this;
            winUnitInfo.Show();

            tcMain.IsEnabled = false;

            try { status = await Task.Run(() => getUnitInfo(targetUnit)); }
            catch (Exception ex)
            { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

            if (status != true)
            { winUnitInfo.Close(); }

            winProgress.Close();
            tcMain.IsEnabled = true;
        }

        private async void btnPanelError_Click(object sender, RoutedEventArgs e)
        {
            bool status = false;
            UnitInfo targetUnit;

            correctTargetUnitInfo(out targetUnit);

            if (targetUnit == null)
            {
                ShowMessageWindow("Please select target cabinet.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return;
            }

            WindowUnitError winUnitError = new WindowUnitError(targetUnit);
            winUnitError.Owner = this;
            winUnitError.Show();

            tcMain.IsEnabled = false;

            try { status = await Task.Run(() => getUnitError(targetUnit)); }
            catch (Exception ex)
            { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

            tcMain.IsEnabled = true;
        }

        private async void btnPanelAlloc_Click(object sender, RoutedEventArgs e)
        {
            bool status = false;
            var dispatcher = Application.Current.Dispatcher;

            tcMain.IsEnabled = false;
            actionButton(sender, "Ser. No. Acquisition Start.");

            string msg = "";

            winProgress = new WindowProgress("Ser. No. Acquisition Progress");
            winProgress.Show();

            try { status = await Task.Run(() => outputAllocInfo()); }
            catch (Exception ex)
            { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

            msg = "";
            string caption = "";
            if (status == true)
            {
                msg = "Serial No. Acquisition Complete!!";
                caption = "Complete";
            }
            else
            {
                // ●Cabinet Power On [2]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[2] Cabinet Power On."); }
                winProgress.ShowMessage("Cabinet Power On.");
                foreach (ControllerInfo cont in dicController.Values)
                { sendSdcpCommand(SDCPClass.CmdUnitPowerOn, cont.IPAddress); }

                foreach (ControllerInfo controller in dicController.Values)
                {
                    if (sendSdcpCommand(SDCPClass.CmdFtpOff, controller.IPAddress) != true)
                    {
                        ShowMessageWindow("Failed to FTP Off.", "Error!", System.Drawing.SystemIcons.Error, 420, 180);
                    }
                }

                msg = "Failed in Serial No. Acquisition.";
                caption = "Error";
            }

            winProgress.StopRemainTimer();
            winProgress.Close();

            playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
            WindowMessage winMessage = new WindowMessage(msg, caption);
            winMessage.ShowDialog();

            releaseButton(sender, "Ser. No. Acquisition Done.");
            tcMain.IsEnabled = true;
        }

        private async void btnPanelLog_Click(object sender, RoutedEventArgs e)
        {
            bool status = false;
            var dispatcher = Application.Current.Dispatcher;

            tcMain.IsEnabled = false;
            actionButton(sender, "Log Acquisition Start.");

            string msg = "It takes approximate each 2 minutes per Controller to complete data transfer. However you may not cancel after start.\r\n\r\nAre you sure?";
            bool? result = false;
            showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 500, 210, "Yes");

            if (result == true)
            {
                winProgress = new WindowProgress("Log Acquisition Progress");
                winProgress.Show();

                try { status = await Task.Run(() => backup(BackupMode.UnitLog)); }
                catch (Exception ex)
                { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

                // ●Unit Power On [6]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[6] Cabinet Power On."); }
                winProgress.ShowMessage("Cabinet Power On.");
                foreach (ControllerInfo cont in dicController.Values)
                { sendSdcpCommand(SDCPClass.CmdUnitPowerOn, cont.IPAddress); }

                msg = "";
                string caption = "";
                if (status == true)
                {
                    winProgress.PutForward1Step();

                    msg = "Log Acquisition Complete!";
                    caption = "Complete";
                }
                else
                {
                    msg = "Failed in Log Acquisition.";
                    caption = "Error";
                }

                // ●すべてのドライブをアンマウント [7]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[7] Unmount NFS Drive."); }
                winProgress.ShowMessage("Unmount NFS Drive.");

                unmountDirve();
                winProgress.PutForward1Step();

                winProgress.Close();

                playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
                WindowMessage winMessage = new WindowMessage(msg, caption);
                winMessage.ShowDialog();
            }

            releaseButton(sender, "Log Acquisition Done.");
            tcMain.IsEnabled = true;
        }

        private void cbReloadInfo_Checked(object sender, RoutedEventArgs e)
        {
            btnPanelInfo.IsEnabled = true;
        }

        private void cbReloadInfo_Unchecked(object sender, RoutedEventArgs e)
        {
            checkUnitData();
        }

        private void UnitInfoToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            // Unitを一つしか選択できないようにする処理
            clearSelectInfo();

            ((UnitToggleButton)sender).Checked -= UnitInfoToggleButton_Checked;
            ((UnitToggleButton)sender).IsChecked = true;
            ((UnitToggleButton)sender).Checked += UnitInfoToggleButton_Checked;
        }

        private void btnReloadPanel_Click(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Reload");

            getUnitStatus();
            checkUnitData();

            releaseButton(sender);
        }

        #endregion Events

        #region Private Method

        private bool getControllerStatus()
        {
            var dispatcher = Application.Current.Dispatcher;

            dispatcher.Invoke(() => (dgController.ItemsSource = null));

            foreach (ControllerInfo cont in dicController.Values)
            {
                // Power Staus
                PowerStatus status;
                getPowerStatus(cont.IPAddress, out status);

                if (status == PowerStatus.Power_On)
                { cont.PowerStatus = "On"; }
                else if (status == PowerStatus.Standby)
                { cont.PowerStatus = "Standby"; }
                else if (status == PowerStatus.Update)
                { cont.PowerStatus = "-"; }
                else if (status == PowerStatus.Startup)
                { cont.PowerStatus = "Starting up"; }
                else if (status == PowerStatus.Shutting_Down)
                { cont.PowerStatus = "Shutting down"; }
                else if (status == PowerStatus.Initializing)
                { cont.PowerStatus = "Initializing"; }
                else
                { cont.PowerStatus = "-"; }

                // Error / Warning
                cont.ErrorWarningStr = "";

                string error;
                getControllerError(cont.IPAddress, out error);
                cont.Error = error;

                Int64 errorCode;
                try { errorCode = Convert.ToInt64(error, 16); }
                catch { errorCode = 0; }
                if (errorCode != 0)
                { cont.ErrorWarningStr += "Error"; }

                string warning;
                getControllerWarning(cont.IPAddress, out warning);
                cont.Warning = warning;

                Int64 warningCode;
                try { warningCode = Convert.ToInt64(warning, 16); }
                catch { warningCode = 0; }
                if (warningCode != 0)
                {
                    if (String.IsNullOrWhiteSpace(cont.ErrorWarningStr) != true)
                    { cont.ErrorWarningStr += " / "; }

                    cont.ErrorWarningStr += "Warning";
                }

                if (cont.ErrorWarningStr.Contains("Warning") == false)
                {
                    string txRxStatus;
                    getControllerTxRxStatusError(cont.IPAddress, out txRxStatus);

                    List<string> tahitiTxRxStatusList = new List<string>();

                    for (int i = 0; i < txRxStatus.Length; i = i + 2)
                    { tahitiTxRxStatusList.Add(txRxStatus.Substring(i, 2)); }

                    foreach (String tahitiStatus in tahitiTxRxStatusList)
                    {
                        UInt64 tahitiwarningCode = Convert.ToUInt64(tahitiStatus, 16);

                        if ((tahitiwarningCode & 0x02) != 0)   //ワーニングが発生している場合
                        {
                            if (String.IsNullOrWhiteSpace(cont.ErrorWarningStr) != true)
                            { cont.ErrorWarningStr += " / "; }

                            cont.ErrorWarningStr += "Warning";  //Warningを格納
                            break;
                        }
                    }
                }

                if (String.IsNullOrWhiteSpace(cont.ErrorWarningStr) == true)
                { cont.ErrorWarningStr = "None"; }
            }

            dispatcher.Invoke(() => (dgController.ItemsSource = contData));

            if (dicController.Count > 0)
            {
                contData[0].Select = true;
                dispatcher.Invoke(() => (dgController.SelectedIndex = 0));
            }

            return true;
        }

        private bool getControllerError(string ip, out string error)
        {
            string buff;

            if (sendSdcpCommand(SDCPClass.CmdControllerStatusErrorGet, out buff, ip) != true)
            {
                error = "";
                return false;
            }

            error = buff;

            return true;
        }

        private bool getControllerWarning(string ip, out string warning)
        {
            string buff;

            if (sendSdcpCommand(SDCPClass.CmdControllerStatusWarningGet, out buff, ip) != true)
            {
                warning = "";
                return false;
            }

            warning = buff;

            return true;
        }

        private bool getControllerTxRxStatusError(string ip, out string txrxstatus)
        {
            string buff;

            if (sendSdcpCommand(SDCPClass.CmdOptTxRxStatusErrorGet, out buff, ip) != true)
            {
                txrxstatus = "";
                return false;
            }

            txrxstatus = buff;

            return true;
        }

        private bool getControllerInfo(ControllerInfo cont)
        {
            string buff;

            // Initialize
            cont.SerialNo = "";
            cont.ModelName = "";
            cont.Version = "";
            cont.SoftwareVersion = new SoftwareVersionInfo();
            cont.BoardInfo = new BoardInformation();

            // <Version>
            // Serial Number
            cont.SerialNo = getSerialNo(cont.IPAddress);

            // Model Name
            cont.ModelName = getModelName(cont.IPAddress);

            // Version
            cont.Version = getVersion(cont.IPAddress);

            // ◆CPU
            // Firmware Ver.
            sendSdcpCommand(SDCPClass.CmdCpuFirmVersionGet, out buff, cont.IPAddress);
            try { cont.SoftwareVersion.CPU_Firm_Ver = buff.Substring(0, 2) + "." + buff.Substring(2, 2) + "." + buff.Substring(4, 2); }
            catch { cont.SoftwareVersion.CPU_Firm_Ver = "-"; }

            // ◆DIF
            // Firmware Ver.
            sendSdcpCommand(SDCPClass.CmdDifFirmVersionGet, out buff, cont.IPAddress);
            try { cont.SoftwareVersion.DIF_Firm_Ver = buff.Substring(0, 2) + "." + buff.Substring(2, 2) + "." + buff.Substring(4, 2); }
            catch { cont.SoftwareVersion.DIF_Firm_Ver = "-"; }

            // FPGA IC1 Ver.
            sendSdcpCommand(SDCPClass.CmdDifFpga1VersionGet, out buff, cont.IPAddress);
            try { cont.SoftwareVersion.DIF_FPGA1_Ver = buff.Substring(0, 2) + "." + buff.Substring(2, 2) + "." + buff.Substring(4, 2); }
            catch { cont.SoftwareVersion.DIF_FPGA1_Ver = "-"; }

            // FPGA IC2 Ver.
            sendSdcpCommand(SDCPClass.CmdDifFpga2VersionGet, out buff, cont.IPAddress);
            try { cont.SoftwareVersion.DIF_FPGA2_Ver = buff.Substring(0, 2) + "." + buff.Substring(2, 2) + "." + buff.Substring(4, 2); }
            catch { cont.SoftwareVersion.DIF_FPGA2_Ver = "-"; }

            // FPGA IC3 Ver.
            sendSdcpCommand(SDCPClass.CmdDifFpga3VersionGet, out buff, cont.IPAddress);
            try { cont.SoftwareVersion.DIF_FPGA3_Ver = buff.Substring(0, 2) + "." + buff.Substring(2, 2) + "." + buff.Substring(4, 2); }
            catch { cont.SoftwareVersion.DIF_FPGA3_Ver = "-"; }

            // DP1 Splitter
            sendSdcpCommand(SDCPClass.CmdDifDp1SplitterGet, out buff, cont.IPAddress);
            try { cont.SoftwareVersion.DIF_DP1_Splitter = buff.Substring(0, 2) + "." + buff.Substring(2, 2) + "." + buff.Substring(4, 2); }
            catch { cont.SoftwareVersion.DIF_DP1_Splitter = "-"; }

            // DP2 Splitter
            sendSdcpCommand(SDCPClass.CmdDifDp2SplitterGet, out buff, cont.IPAddress);
            try { cont.SoftwareVersion.DIF_DP2_Splitter = buff.Substring(0, 2) + "." + buff.Substring(2, 2) + "." + buff.Substring(4, 2); }
            catch { cont.SoftwareVersion.DIF_DP2_Splitter = "-"; }

            // DP1 Receiver1
            sendSdcpCommand(SDCPClass.CmdDifDp1Receiver1Get, out buff, cont.IPAddress);
            try { cont.SoftwareVersion.DIF_DP1_Receiver1 = buff.Substring(0, 2) + "." + buff.Substring(2, 2) + "." + buff.Substring(4, 2); }
            catch { cont.SoftwareVersion.DIF_DP1_Receiver1 = "-"; }

            // DP1 Receiver2
            sendSdcpCommand(SDCPClass.CmdDifDp1Receiver2Get, out buff, cont.IPAddress);
            try { cont.SoftwareVersion.DIF_DP1_Receiver2 = buff.Substring(0, 2) + "." + buff.Substring(2, 2) + "." + buff.Substring(4, 2); }
            catch { cont.SoftwareVersion.DIF_DP1_Receiver2 = "-"; }

            // DP2 Receiver1
            sendSdcpCommand(SDCPClass.CmdDifDp2Receiver1Get, out buff, cont.IPAddress);
            try { cont.SoftwareVersion.DIF_DP2_Receiver1 = buff.Substring(0, 2) + "." + buff.Substring(2, 2) + "." + buff.Substring(4, 2); }
            catch { cont.SoftwareVersion.DIF_DP2_Receiver1 = "-"; }

            // DP2 Receiver2
            sendSdcpCommand(SDCPClass.CmdDifDp2Receiver2Get, out buff, cont.IPAddress);
            try { cont.SoftwareVersion.DIF_DP2_Receiver2 = buff.Substring(0, 2) + "." + buff.Substring(2, 2) + "." + buff.Substring(4, 2); }
            catch { cont.SoftwareVersion.DIF_DP2_Receiver2 = "-"; }

            // ◆PIF
            // FPGA Ver.
            sendSdcpCommand(SDCPClass.CmdPifFpgaVersionGet, out buff, cont.IPAddress);
            try { cont.SoftwareVersion.PIF_FPGA_Ver = buff.Substring(0, 2) + "." + buff.Substring(2, 2) + "." + buff.Substring(4, 2); }
            catch { cont.SoftwareVersion.PIF_FPGA_Ver = "-"; }

            // <Board Information>
            // ◆CPU
            // Board Name
            //sendSdcpCommand(SDCPClass.CmdCpuBoardNameGet, out buff, cont.IPAddress);
            //try
            //{
            //    cont.BoardInfo.CPU_Board_Name = convertAscii(buff);
            //    if (string.IsNullOrWhiteSpace(cont.BoardInfo.CPU_Board_Name) == true)
            //    { cont.BoardInfo.CPU_Board_Name = "-"; }
            //}
            //catch { cont.BoardInfo.CPU_Board_Name = "-"; }

            // Parts Number
            //sendSdcpCommand(SDCPClass.CmdCpuPartsNumberGet, out buff, cont.IPAddress);
            //try
            //{
            //    cont.BoardInfo.CPU_Parts_Number = convertAscii(buff);
            //    if (string.IsNullOrWhiteSpace(cont.BoardInfo.CPU_Parts_Number) == true)
            //    { cont.BoardInfo.CPU_Parts_Number = "-"; }
            //}
            //catch { cont.BoardInfo.CPU_Parts_Number = "-"; }

            // Mount-A
            //sendSdcpCommand(SDCPClass.CmdCpuMountAGet, out buff, cont.IPAddress);
            //try
            //{
            //    cont.BoardInfo.CPU_Mount_A = convertAscii(buff);
            //    if (string.IsNullOrWhiteSpace(cont.BoardInfo.CPU_Mount_A) == true)
            //    { cont.BoardInfo.CPU_Mount_A = "-"; }
            //}
            //catch { cont.BoardInfo.CPU_Mount_A = "-"; }

            // Compl-A
            //sendSdcpCommand(SDCPClass.CmdCpuComplAGet, out buff, cont.IPAddress);
            //try
            //{
            //    cont.BoardInfo.CPU_Compl_A = convertAscii(buff);
            //    if (string.IsNullOrWhiteSpace(cont.BoardInfo.CPU_Compl_A) == true)
            //    { cont.BoardInfo.CPU_Compl_A = "-"; }
            //}
            //catch { cont.BoardInfo.CPU_Compl_A = "-"; }

            // ◆DIF
            // Board Name
            //sendSdcpCommand(SDCPClass.CmdDifBoardNameGet, out buff, cont.IPAddress);
            //try
            //{
            //    cont.BoardInfo.DIF_Board_Name = convertAscii(buff);
            //    if (string.IsNullOrWhiteSpace(cont.BoardInfo.DIF_Board_Name) == true)
            //    { cont.BoardInfo.DIF_Board_Name = "-"; }
            //}
            //catch { cont.BoardInfo.DIF_Board_Name = "-"; }

            //// Parts Number
            //sendSdcpCommand(SDCPClass.CmdDifPartsNumberGet, out buff, cont.IPAddress);
            //try
            //{
            //    cont.BoardInfo.DIF_Parts_Number = convertAscii(buff);
            //    if (string.IsNullOrWhiteSpace(cont.BoardInfo.DIF_Parts_Number) == true)
            //    { cont.BoardInfo.DIF_Parts_Number = "-"; }
            //}
            //catch { cont.BoardInfo.DIF_Parts_Number = "-"; }

            // Mount-A
            //sendSdcpCommand(SDCPClass.CmdDifMountAGet, out buff, cont.IPAddress);
            //try
            //{
            //    cont.BoardInfo.DIF_Mount_A = convertAscii(buff);
            //    if (string.IsNullOrWhiteSpace(cont.BoardInfo.DIF_Mount_A) == true)
            //    { cont.BoardInfo.DIF_Mount_A = "-"; }
            //}
            //catch { cont.BoardInfo.DIF_Mount_A = "-"; }

            // Compl-A
            //sendSdcpCommand(SDCPClass.CmdDifComplAGet, out buff, cont.IPAddress);
            //try
            //{
            //    cont.BoardInfo.DIF_Compl_A = convertAscii(buff);
            //    if (string.IsNullOrWhiteSpace(cont.BoardInfo.DIF_Compl_A) == true)
            //    { cont.BoardInfo.DIF_Compl_A = "-"; }
            //}
            //catch { cont.BoardInfo.DIF_Compl_A = "-"; }

            // ◆PIF
            // Board Name
            //sendSdcpCommand(SDCPClass.CmdPifBoardNameGet, out buff, cont.IPAddress);
            //try
            //{
            //    cont.BoardInfo.PIF_Board_Name = convertAscii(buff);
            //    if (string.IsNullOrWhiteSpace(cont.BoardInfo.PIF_Board_Name) == true)
            //    { cont.BoardInfo.PIF_Board_Name = "-"; }
            //}
            //catch { cont.BoardInfo.PIF_Board_Name = "-"; }

            // Parts Number
            //sendSdcpCommand(SDCPClass.CmdPifPartsNumberGet, out buff, cont.IPAddress);
            //try
            //{
            //    cont.BoardInfo.PIF_Parts_Number = convertAscii(buff);
            //    if (string.IsNullOrWhiteSpace(cont.BoardInfo.PIF_Parts_Number) == true)
            //    { cont.BoardInfo.PIF_Parts_Number = "-"; }
            //}
            //catch { cont.BoardInfo.PIF_Parts_Number = "-"; }

            // Mount-A
            //sendSdcpCommand(SDCPClass.CmdPifMountAGet, out buff, cont.IPAddress);
            //try
            //{
            //    cont.BoardInfo.PIF_Mount_A = convertAscii(buff);
            //    if (string.IsNullOrWhiteSpace(cont.BoardInfo.PIF_Mount_A) == true)
            //    { cont.BoardInfo.PIF_Mount_A = "-"; }
            //}
            //catch { cont.BoardInfo.PIF_Mount_A = "-"; }

            // Compl-A
            //sendSdcpCommand(SDCPClass.CmdPifComplAGet, out buff, cont.IPAddress);
            //try
            //{
            //    cont.BoardInfo.PIF_Compl_A = convertAscii(buff);
            //    if (string.IsNullOrWhiteSpace(cont.BoardInfo.PIF_Compl_A) == true)
            //    { cont.BoardInfo.PIF_Compl_A = "-"; }
            //}
            //catch { cont.BoardInfo.PIF_Compl_A = "-"; }

            return true;
        }

        private bool getControllerError(ControllerInfo cont)
        {
            string buff;

            // Initialize
            cont.TempInfo = new TemperatureInfo();

            // Error / Warning
            cont.ErrorWarningStr = "";

            if (cont.TahitiWarningList.Count == 0)
            {
                for (int i = 0; i < TXRX_TOTAL_COUNT; i++)
                { cont.TahitiWarningList.Add("000000000000"); }
            }
            else
            {
                for (int i = 0; i < TXRX_TOTAL_COUNT; i++)
                { cont.TahitiWarningList[i] = "000000000000"; }
            }

            string error;
            getControllerError(cont.IPAddress, out error);
            cont.Error = error;

            Int64 errorCode = Convert.ToInt64(error, 16);
            if (errorCode != 0)
            { cont.ErrorWarningStr += "Error"; }

            string warning;
            getControllerWarning(cont.IPAddress, out warning);
            cont.Warning = warning;

            Int64 warningCode = Convert.ToInt64(warning, 16);
            if (warningCode != 0)
            {
                if (String.IsNullOrWhiteSpace(cont.ErrorWarningStr) != true)
                { cont.ErrorWarningStr += " / "; }

                cont.ErrorWarningStr += "Warning";
            }

            string txRxStatus;
            getControllerTxRxStatusError(cont.IPAddress, out txRxStatus);

            List<string> tahitiTxRxStatusList = new List<string>();

            if (isErrorResponseValue(txRxStatus))
            { tahitiTxRxStatusList.Add("00"); }
            else
            {
                for (int i = 0; i < txRxStatus.Length; i = i + 2)
                { tahitiTxRxStatusList.Add(txRxStatus.Substring(i, 2)); }
            }

            if (cont.ErrorWarningStr.Contains("Warning") == false)
            {
                foreach (String tahitiStatus in tahitiTxRxStatusList)
                {
                    UInt64 tahitiwarningCode = Convert.ToUInt64(tahitiStatus, 16);

                    if ((tahitiwarningCode & 0x02) != 0)
                    {
                        if (String.IsNullOrWhiteSpace(cont.ErrorWarningStr) != true)
                        { cont.ErrorWarningStr += " / "; }

                        cont.ErrorWarningStr += "Warning";
                        break;
                    }
                }
            }

            for (int TahitiNo = 0; TahitiNo < tahitiTxRxStatusList.Count / 2; TahitiNo++)       //TX
            {
                UInt64 tahitiwarningCode = Convert.ToUInt64(tahitiTxRxStatusList[TahitiNo], 16);

                if ((tahitiwarningCode & 0x02) == 0)
                {
                    continue; 
                }
                string tahitiTxWarning;

                Byte[] cmd = new byte[SDCPClass.CmdOptTxRxWarningGet.Length];
                Array.Copy(SDCPClass.CmdOptTxRxWarningGet, cmd, SDCPClass.CmdOptTxRxWarningGet.Length);

                cmd[9] = (byte)((OPT_TX << 4) + TahitiNo);
                sendSdcpCommand(cmd, out tahitiTxWarning, cont.IPAddress);
                if(isErrorResponseValue(tahitiTxWarning) == false)
                { cont.TahitiWarningList[TahitiNo] = tahitiTxWarning; }
            }

            for (int TahitiNo = 0; TahitiNo < tahitiTxRxStatusList.Count / 2; TahitiNo++)       //RX
            {
                UInt64 tahitiwarningCode = Convert.ToUInt64(tahitiTxRxStatusList[TahitiNo+ tahitiTxRxStatusList.Count / 2], 16);

                if ((tahitiwarningCode & 0x02) == 0)
                {
                    continue;
                }

                string tahitiRxWarning;
                
                Byte[] cmd = new byte[SDCPClass.CmdOptTxRxWarningGet.Length];
                Array.Copy(SDCPClass.CmdOptTxRxWarningGet, cmd, SDCPClass.CmdOptTxRxWarningGet.Length);

                cmd[9] = (byte)((OPT_RX << 4) + TahitiNo);
                sendSdcpCommand(cmd, out tahitiRxWarning, cont.IPAddress);
                if (isErrorResponseValue(tahitiRxWarning) == false)
                { cont.TahitiWarningList[TahitiNo + tahitiTxRxStatusList.Count / 2] = tahitiRxWarning; }
            }

            if (String.IsNullOrWhiteSpace(cont.ErrorWarningStr) == true)
            { cont.ErrorWarningStr = "None"; }

            // Temp
            // IF
            sendSdcpCommand(SDCPClass.CmdIfTempGet, out buff, cont.IPAddress);
            try
            {
                if (buff == "0180")
                { cont.TempInfo.DIF_Temp = "0"; }
                else
                { cont.TempInfo.IF_Temp = Convert.ToInt32(buff, 16).ToString(); }
            }
            catch { cont.TempInfo.IF_Temp = "0"; }

            // PS
            sendSdcpCommand(SDCPClass.CmdPsTempGet, out buff, cont.IPAddress);
            try
            {
                if (buff == "0180")
                { cont.TempInfo.DIF_Temp = "0"; }
                else
                { cont.TempInfo.PS_Temp = Convert.ToInt32(buff, 16).ToString(); }
            }
            catch { cont.TempInfo.PS_Temp = "0"; }

            // DIF
            sendSdcpCommand(SDCPClass.CmdDifTempGet, out buff, cont.IPAddress);
            try
            {
                if (buff == "0180")
                { cont.TempInfo.DIF_Temp = "0"; }
                else
                { cont.TempInfo.DIF_Temp = Convert.ToInt32(buff, 16).ToString(); }
            }
            catch { cont.TempInfo.DIF_Temp = "0"; }

            // PIF
            sendSdcpCommand(SDCPClass.CmdPifTempGet, out buff, cont.IPAddress);
            try
            {
                if (buff == "0180")
                { cont.TempInfo.DIF_Temp = "0"; }
                else
                { cont.TempInfo.PIF_Temp = Convert.ToInt32(buff, 16).ToString(); }
            }
            catch { cont.TempInfo.PIF_Temp = "0"; }

            return true;
        }

        private void clearSelectInfo()
        {
            for (int i = 0; i < allocInfo.MaxX; i++)
            {
                for (int j = 0; j < allocInfo.MaxY; j++)
                {
                    if (aryUnitInfo[i, j].UnitInfo != null)
                    { Dispatcher.Invoke(new Action(() => { aryUnitInfo[i, j].IsChecked = false; })); }
                }
            }
        }

        private bool getUnitStatus()
        {
            string buff;
            int maxUnitNo;

            if (allocInfo.LEDModel == ZRD_B15A
                || allocInfo.LEDModel == ZRD_C15A
                || allocInfo.LEDModel == ZRD_BH15D
                || allocInfo.LEDModel == ZRD_CH15D
                || allocInfo.LEDModel == ZRD_BH15D_S3
                || allocInfo.LEDModel == ZRD_CH15D_S3)
            { 
                maxUnitNo = Defined.Max_UnitNo_P15;
            }
            else
            { 
                maxUnitNo = Defined.Max_UnitNo_P12;
            }

            foreach (ControllerInfo cont in dicController.Values)
            {
                byte[,] aryUnitStatus;
                sendSdcpCommand(SDCPClass.CmdUnitStatusGet, out buff, cont.IPAddress);

                if (buff == "0180")
                { continue; }

                convertErrorCode(buff, out aryUnitStatus);

                for (int port = 0; port < 12; port++)
                {
                    for (int unitNo = 0; unitNo < maxUnitNo; unitNo++)
                    {
                        int x, y;

                        if (aryUnitStatus[port, unitNo] != 0)
                        {
                            // Error
                            if ((aryUnitStatus[port, unitNo] & 0x01) != 0)
                            {
                                searchUnitAddress(cont.ControllerID, port + 1, unitNo + 1, out x, out y);
                                changeUnitColor(x, y, AlertLevel.Error);
                            }
                            // Warning
                            else if ((aryUnitStatus[port, unitNo] & 0x02) != 0)
                            {
                                searchUnitAddress(cont.ControllerID, port + 1, unitNo + 1, out x, out y);
                                changeUnitColor(x, y, AlertLevel.Warning);
                            }
                        }
                        else
                        {
                            searchUnitAddress(cont.ControllerID, port + 1, unitNo + 1, out x, out y);
                            changeUnitColor(x, y, AlertLevel.Normal);
                        }
                    }
                }
            }

            return true;
        }

        private bool convertErrorCode(string code, out byte[,] aryStatus)
        {
            aryStatus = new byte[12, 9];

            for (int port = 0; port < 12; port++)
            {
                for (int unitNo = 0; unitNo < 9; unitNo++)
                {
                    if (unitNo >= 9)
                    { continue; }

                    try { aryStatus[port, unitNo] = Convert.ToByte(code.Substring(port * 9 * 2 + unitNo * 2, 2), 16); }
                    catch { }
                }
            }

            return true;
        }

        private bool searchUnitAddress(int controllerId, int portNo, int unitNo, out int x, out int y)
        {
            x = -1;
            y = -1;

            for (int col = 0; col < allocInfo.MaxX; col++)
            {
                for (int row = 0; row < allocInfo.MaxY; row++)
                {
                    if (aryUnitInfo[col, row].UnitInfo == null)
                    { continue; }

                    if (aryUnitInfo[col, row].UnitInfo.ControllerID == controllerId
                    && aryUnitInfo[col, row].UnitInfo.PortNo == portNo
                    && aryUnitInfo[col, row].UnitInfo.UnitNo == unitNo)
                    {
                        x = aryUnitInfo[col, row].UnitInfo.X;
                        y = aryUnitInfo[col, row].UnitInfo.Y;
                        return true;
                    }
                }
            }

            return true;
        }

        private bool changeUnitColor(int x, int y, AlertLevel level)
        {
            if (x < 0 || y < 0)
            { return false; }

            if (level == AlertLevel.Error)
            { Dispatcher.Invoke(new Action(() => { aryUnitInfo[x - 1, y - 1].Background = new SolidColorBrush(Colors.LightPink); })); }
            else if (level == AlertLevel.Warning)
            { Dispatcher.Invoke(new Action(() => { aryUnitInfo[x - 1, y - 1].Background = new SolidColorBrush(Colors.LemonChiffon); })); }
            else
            {
                try { Dispatcher.Invoke(new Action(() => { aryUnitInfo[x - 1, y - 1].Background = new SolidColorBrush(SystemColors.ControlLightBrush.Color); })); }
                catch { } // 無視
            }

            return true;
        }

        private bool checkUnitData()
        {
            var dispatcher = Application.Current.Dispatcher;
            bool reload = false;

            for (int x = 0; x < allocInfo.MaxX; x++)
            {
                for (int y = 0; y < allocInfo.MaxY; y++)
                {
                    if (aryUnitInfo[x, y].UnitInfo == null)
                    { continue; }

                    string filePath = makeFilePath(aryUnitInfo[x, y].UnitInfo, FileDirectory.Backup_Latest, DataType.UnitData);

                    if (System.IO.File.Exists(filePath) != true)
                    {
                        reload = true;
                        break;
                    }
                }

                if (reload == true)
                { break; }
            }

            //if(reload == true)
            //{
            //    dispatcher.Invoke(() => (btnPanelInfo.IsEnabled = false));
            //}

            return true;
        }

        private void correctTargetUnitInfo(out UnitInfo targetUnit)
        {
            targetUnit = null;

            for (int y = allocInfo.MaxY - 1; y >= 0; y--)
            {
                for (int x = 0; x < allocInfo.MaxX; x++)
                {
                    if (aryUnitInfo[x, y].UnitInfo != null && aryUnitInfo[x, y].IsChecked == true)
                    {
                        targetUnit = aryUnitInfo[x, y].UnitInfo;
                    }
                }
            }
        }

        private bool getUnitInfo(UnitInfo unit)
        {
            bool status;
            bool reload = false;

            int step = UNIT_INFO_ITEM;
            winProgress.SetWholeSteps(step);

            var dispatcher = Application.Current.Dispatcher;
            dispatcher.Invoke(() => getCheckBoxStatus(out reload));

            if (reload == true)
            {
                status = reloadUnitData(unit);
                if (status != true)
                { return false; }
            }

            string filePath = makeFilePath(unit, FileDirectory.Backup_Rbin, DataType.UnitData);

            // Unit Data File
            unit.UnitDataFile = filePath;
            winProgress.PutForward1Step();

            // Serial No
            unit.SerialNo = getCabinetSerialNumber(unit);
            winProgress.PutForward1Step();

            // Model Name
            unit.ModelName = getCabinetModelName(unit);
            winProgress.PutForward1Step();

            // Version
            unit.Version = getCabinetVersion(unit);
            winProgress.PutForward1Step();

            return true;
        }

        private bool getUnitError(UnitInfo unit)
        {
            string buff;

            // Unit Error
            Byte[] cmd = new byte[SDCPClass.CmdUnitErrorGet.Length];
            Array.Copy(SDCPClass.CmdUnitErrorGet, cmd, SDCPClass.CmdUnitErrorGet.Length);

            cmd[9] += (byte)(unit.PortNo << 4);
            cmd[9] += (byte)unit.UnitNo;

            sendSdcpCommand(cmd, out buff, dicController[unit.ControllerID].IPAddress);
            if (!isErrorResponseValue(buff))
            { unit.Error = buff; }
            else
            { unit.Error = "00"; }

            // Unit Warning
            cmd = new byte[SDCPClass.CmdUnitWarningGet.Length];
            Array.Copy(SDCPClass.CmdUnitWarningGet, cmd, SDCPClass.CmdUnitWarningGet.Length);

            cmd[9] += (byte)(unit.PortNo << 4);
            cmd[9] += (byte)unit.UnitNo;

            sendSdcpCommand(cmd, out buff, dicController[unit.ControllerID].IPAddress);
            if (!isErrorResponseValue(buff))
            { unit.Warning = buff; }
            else
            { unit.Warning = "00"; }

            return true;
        }

        private void getCheckBoxStatus(out bool reload)
        {
            reload = (bool)cbReloadInfo.IsChecked;
        }

        private bool reloadUnitData(UnitInfo unit)
        {
            bool status = false;

            string msg = "It is necessary for backup for up to dozens of minutes.\r\nAnd you can not cancel it.\r\n\r\nAre you sure?";
            //MessageBoxResult result = MessageBox.Show(msg, "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);

            bool? result = false;
            showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 500, 210, "Yes");

            if (result == true)
            {
                try { status = backup(BackupMode.UnitDataOnly); }
                catch (Exception ex)
                { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

                // ●すべてのドライブをアンマウント [6]
                //winProgress.ShowMessage("Unmount NFS Drive.");
                //unmountDirve();

                // ターゲットUnitのDataのみ [6]
                if (convWriteDataOnlyUnitData(unit) != true)
                {
                    msg = "The following files could not be read correctly.\r\nPlease check the backup file before doing other work.";
                    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 500, 300);
                    return false;
                }
                winProgress.PutForward1Step();
            }
            else
            { return false; }

            return true;
        }

        private bool outputAllocInfo()
        {
            foreach (ControllerInfo controller in dicController.Values)
            {
                // Model Name
                if (string.IsNullOrWhiteSpace(controller.ModelName))
                { controller.ModelName = getModelName(controller.IPAddress); }

                // Controller Serial
                if (string.IsNullOrWhiteSpace(controller.SerialNo))
                { controller.SerialNo = getSerialNo(controller.IPAddress); }

                string srcDir = applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo + "_Rbin";
                if (!Directory.Exists(srcDir))
                {
                    string msg = "In order to obtain the serial information, the backup data of the cabinet is required. Although Cabinet backup data does not exist, do you want to continue obtaining serial information?";
                    showMessageWindow(out bool? result, msg, "Confirm", System.Drawing.SystemIcons.Question, 400, 210, "Yes", "No");
                    if (result == false)
                    { return false; } // Noボタン
                    else
                    { break; } // Yesボタン
                }
            }

            List<ControllerInfo> lstTargetController = new List<ControllerInfo>();

            // ●進捗の最大値を設定
            winProgress.ShowMessage("Set Step Count.");

            var dispatcher = Application.Current.Dispatcher;

            int cabinetCount = 0;
            dispatcher.Invoke(() => getTotalCabinets(out cabinetCount, allocInfo.MaxX, allocInfo.MaxY));
            if (cabinetCount == 0)
            {
                ShowMessageWindow("Not found cabinets.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
            }

            int step = 3 + cabinetCount;

            winProgress.SetWholeSteps(step);

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

            int nomatchUdfileCabinetCount = 0;
            int nomatchUdfileControllerCount = 0;

            foreach (List<UnitInfo> lst in allocInfo.lstUnits)
            {
                foreach (UnitInfo unit in lst)
                {
                    if (unit == null)
                    { continue; }

                    ControllerInfo controller = dicController[unit.ControllerID];  // Cabinetが接続されているController
                    string sourceDir = applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo + "_Rbin\\"; // Rbinフォルダ
                    string udFile = sourceDir + "\\u" + unit.PortNo.ToString("D2") + unit.UnitNo.ToString("D2") + "_ud.rbin";
                    if (System.IO.File.Exists(udFile) != true || checkCommonHeaderDataType(udFile) != true)
                    {
                        nomatchUdfileCabinetCount++;

                        if (lstTargetController.Contains(controller) != true)
                        {
                            lstTargetController.Add(controller);
                            nomatchUdfileControllerCount++;
                        }
                    }
                }

            }
            lstTargetController.Clear();

            int completeFTPonNomatchUdfileControllerCount = 0;  //適合するudファイルがないキャビネットのコントローラのうち、FTPonの処理が完了している台数
            int processedCabi = 0;
            int processedNomatchUdfileCabi = 0;

            processSec = calcOutputAllocInfoProcessSec(currentStep, cabinetCount,powerUpWait,nomatchUdfileCabinetCount, nomatchUdfileControllerCount, completeFTPonNomatchUdfileControllerCount, processedCabi, processedNomatchUdfileCabi);

            winProgress.StartRemainTimer(processSec);

            // ●Power OnでないときはOnにする [1]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[1] Check Controller Power."); }
            winProgress.ShowMessage("Check Controller Power.");

            if (!setAllControllerPowerOn())
            { return false; }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcOutputAllocInfoProcessSec(currentStep, cabinetCount,powerUpWait,nomatchUdfileCabinetCount, nomatchUdfileControllerCount, completeFTPonNomatchUdfileControllerCount, processedCabi, processedNomatchUdfileCabi);
            winProgress.SetRemainTimer(processSec);

            // 初期化
            foreach (ControllerInfo cont in dicController.Values)
            { cont.Target = false; }

            string controllerInfo = "Controller ID, Model, Controller Serial, Controller Version,\r\n";

            // Controller Loop
            foreach (ControllerInfo controller in dicController.Values)
            {
                string msg = "Getting Controller Info...\r\nController ID : " + controller.ControllerID.ToString();
                winProgress.ShowMessage(msg);

                string line = "";

                // Controller ID
                line += controller.ControllerID + ", ";

                // Model Name
                if (string.IsNullOrWhiteSpace(controller.ModelName) == true)
                { controller.ModelName = getModelName(controller.IPAddress); }

                line += controller.ModelName + ", ";

                // Controller Serial
                if (string.IsNullOrWhiteSpace(controller.SerialNo) == true)
                { controller.SerialNo = getSerialNo(controller.IPAddress); }

                line += controller.SerialNo + ", ";
                
                // Controller Version
                if (string.IsNullOrWhiteSpace(controller.Version) == true)
                { controller.Version = getVersion(controller.IPAddress); }

                line += controller.Version + ", ";

                controllerInfo += line + "\r\n";
            }

            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcOutputAllocInfoProcessSec(currentStep, cabinetCount, powerUpWait, nomatchUdfileCabinetCount, nomatchUdfileControllerCount, completeFTPonNomatchUdfileControllerCount, processedCabi, processedNomatchUdfileCabi);
            winProgress.SetRemainTimer(processSec);

            string cabinetInfo;

            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            { cabinetInfo = "Controller ID, Port No., Cabinet No., x, y, Cabinet Model, Cabinet Serial, Cabinet Version, LED Elapsed Time[sec], Module Serial1, Module Serial2, Module Serial3, Module Serial4, Module Serial5, Module Serial6, Module Serial7, Module Serial8,\r\n"; }
            else
            {
                cabinetInfo = "Controller ID, Port No., Cabinet No., x, y, Cabinet Model, Cabinet Serial, Cabinet Version, LED Elapsed Time[sec], Module Serial1, Module Serial2, Module Serial3, Module Serial4, Module Serial5, Module Serial6, Module Serial7, Module Serial8, Module Serial9, Module Serial10, Module Serial11, Module Serial12, Mod1 CTC VDI, Mod2 CTC VDI, Mod3 CTC VDI, Mod4 CTC VDI, Mod5 CTC VDI, Mod6 CTC VDI, Mod7 CTC VDI, Mod8 CTC VDI, Mod9 CTC VDI, Mod10 CTC VDI, Mod11 CTC VDI, Mod12 CTC VDI,\r\n";
                if (appliMode == ApplicationMode.Service)
                { cabinetInfo = "Controller ID, Port No., Cabinet No., x, y, Cabinet Model, Cabinet Serial, Cabinet Version, LED Elapsed Time[sec], Module Serial1, Module Serial2, Module Serial3, Module Serial4, Module Serial5, Module Serial6, Module Serial7, Module Serial8, Module Serial9, Module Serial10, Module Serial11, Module Serial12, Mod1 CTC VDI, Mod2 CTC VDI, Mod3 CTC VDI, Mod4 CTC VDI, Mod5 CTC VDI, Mod6 CTC VDI, Mod7 CTC VDI, Mod8 CTC VDI, Mod9 CTC VDI, Mod10 CTC VDI, Mod11 CTC VDI, Mod12 CTC VDI, Mod1 MGAM VDI, Mod2 MGAM VDI, Mod3 MGAM VDI, Mod4 MGAM VDI, Mod5 MGAM VDI, Mod6 MGAM VDI, Mod7 MGAM VDI, Mod8 MGAM VDI, Mod9 MGAM VDI, Mod10 MGAM VDI, Mod11 MGAM VDI, Mod12 MGAM VDI,\r\n"; }
            }

            // Cabinet Loop
            foreach (List<UnitInfo> lst in allocInfo.lstUnits)
            {
                foreach (UnitInfo unit in lst)
                {
                    if (unit == null)
                    {
                        continue;
                    }

                    ControllerInfo controller = dicController[unit.ControllerID];  // Cabinetが接続されているController

                    string line = "";
                    string msg = "Getting Cabinet Info...\r\nController ID : " + unit.ControllerID.ToString() + ", Port No. : " + unit.PortNo + ", Cabinet No. : " + unit.UnitNo;
                    winProgress.ShowMessage(msg);

                    // Controller ID
                    line += unit.ControllerID + ", ";

                    // Port No.
                    line += unit.PortNo + ", ";

                    // Unit No.
                    line += unit.UnitNo + ", ";

                    // X
                    line += unit.X + ", ";

                    // Y
                    line += unit.Y + ", ";

                    // Cabinet Model
                    unit.ModelName = getCabinetModelName(unit);

                    line += unit.ModelName + ", ";

                    // Cabinet Serial Number
                    unit.SerialNo = getCabinetSerialNumber(unit);

                    line += unit.SerialNo + ", ";

                    // Cabinet Version
                    unit.Version = getCabinetVersion(unit);

                    line += unit.Version + ", ";

                    System.Threading.Thread.Sleep(100); // 通信エラー対策

                    // LED Lighting Elapsed Time
                    unit.LedElapsedTime = getLedElapsedTime(unit);

                    line += unit.LedElapsedTime + ",";

                    // unit.LedElapsedTimeにハイフンが格納されている場合は、直前のSDCPコマンド（LED LIGHTING ELAPSED TIME）でCabinetから情報を取得できなかったことを意味する
                    // この場合はそのCabinetが未接続であるとみなし、Module Serial No.取得処理をSkipする（未接続のCabinetからはud.rbinを取得できないため）
                    if (unit.LedElapsedTime != STR_HYPHEN)
                    {
                        // Module Serial No.
                        bool isSelectedContinue = false;

                        string sourceDir = applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo + "_Rbin\\"; // Rbinフォルダ
                        string udFile = sourceDir + "\\u" + unit.PortNo.ToString("D2") + unit.UnitNo.ToString("D2") + "_ud.rbin";

                        // データファイルを確認する
                        if (System.IO.File.Exists(udFile) != true || checkCommonHeaderDataType(udFile) != true)
                        {
                            controller.Target = true;

                            if (lstTargetController.Contains(controller) != true) // 対象Controllerに対する1回目の処理
                            {
                                lstTargetController.Add(controller);

                                // FTP ON
                                winProgress.ShowMessage("FTP On.");
                                if (sendSdcpCommand(SDCPClass.CmdFtpOn, controller.IPAddress) != true)
                                { return false; }

                                // Cabinet Power Off
                                winProgress.ShowMessage("Cabinet Power Off.");
                                sendSdcpCommand(SDCPClass.CmdUnitPowerOff, controller.IPAddress);
                                System.Threading.Thread.Sleep(SLEEP_TIME_AFTER_PANEL_OFF);

                                if (getUnitPowerStatus() != true)
                                {
                                    msg = "Failed to cabinet power off.";
                                    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 500, 210);
                                    return false;
                                }
                                completeFTPonNomatchUdfileControllerCount++;
                            }
                            else // 対象Controllerに対する2回目以降の処理
                            {
                                try
                                {
                                    // 前回の処理で生成されたread_completeファイルを削除する
                                    // read_completeファイルが残っているとデータ読み出し完了と判定してしまうため
                                    if (deleteFtpFile(controller) != true)
                                    {
                                        msg = "Failed to delete ftp files.";
                                        ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 500, 210);
                                        return false;
                                    }
                                }
                                catch
                                {
                                    msg = "Failed to delete ftp files.";
                                    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 500, 210);
                                    return false;
                                }
                            }

                            while (true)
                            {
                                // ud.rbinバックアップ
                                if (backupTargetUnitData(controller, unit) != true)
                                {
                                    string subMsg = "Backup data does not exist or data may be damaged.\r\n\r\nu" + unit.PortNo.ToString("D2") + unit.UnitNo.ToString("D2") + "_ud.rbin";
                                    bool? result = false;
                                    showMessageWindow(out result, subMsg, "Confirm", System.Drawing.SystemIcons.Question, 400, 210, "Retry", "Continue");
                                    if (result != false)
                                    { continue; }
                                    else
                                    {
                                        isSelectedContinue = true;
                                        break;
                                    }
                                }
                                else
                                { break; }
                            }

                            // 初期化
                            controller.Target = false;
                            processedNomatchUdfileCabi++;
                        }

                        if (isSelectedContinue != true)
                        {
                            List<CompositeConfigData> lstModuleInfo = CompositeConfigData.getCompositeConfigDataList(udFile, dt_LedModuleInfoRead);
                            line += getSpecifiedRcsData(lstModuleInfo, TargetData.UD_SerialNumber, UdModuleSerialNoOffset, UdModuleSerialNoSize);

                            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
                            {
                                string latestDir = $"{applicationPath}\\Backup\\{controller.ModelName}_{controller.SerialNo}_Latest\\"; // Latestフォルダ
                                string hcFile = $"{latestDir}\\u{unit.PortNo:D2}{unit.UnitNo:D2}_hc.bin";
                                if (System.IO.File.Exists(hcFile))
                                {
                                    lstModuleInfo = CompositeConfigData.getCompositeConfigDataList(hcFile, dt_ColorCorrectionWrite);
                                    line += getSpecifiedRcsData(lstModuleInfo, TargetData.HC_ValidDataIndicator, HcCcDataCtcDataValidIndicatorOffset, HcCcDataCtcDataValidIndicatorSize);
                                }
                                else
                                { line += ",,,,,,,,,,,,"; } // 埋め込み

                                if (appliMode == ApplicationMode.Service)
                                {
                                    string lcFile = $"{latestDir}\\u{unit.PortNo:D2}{unit.UnitNo:D2}_lc.bin";
                                    if (System.IO.File.Exists(lcFile))
                                    {
                                        lstModuleInfo = CompositeConfigData.getCompositeConfigDataList(lcFile, dt_MgamWrite);
                                        line += getSpecifiedRcsData(lstModuleInfo, TargetData.LC_ValidDataIndicator, LcMgamModuleValidDataIndicatorOffset, LcMgamModuleValidDataIndicatorSize);
                                    }
                                }
                            }
                        }
                    }

                    cabinetInfo += line + "\r\n";

                    System.Threading.Thread.Sleep(100); // 通信エラー対策

                    winProgress.PutForward1Step();

                    dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
                    processedCabi++;
                    processSec = calcOutputAllocInfoProcessSec(currentStep, cabinetCount,powerUpWait,nomatchUdfileCabinetCount, nomatchUdfileControllerCount, completeFTPonNomatchUdfileControllerCount, processedCabi, processedNomatchUdfileCabi);
                    winProgress.SetRemainTimer(processSec);

                }
            }

            // File出力
            string filePath = applicationPath + "\\Profile\\Serial No.\\";

            if(System.IO.Directory.Exists(filePath) != true)
            { System.IO.Directory.CreateDirectory(filePath); }

            filePath += DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv";

            try
            {
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(filePath))
                { sw.Write(controllerInfo + cabinetInfo); }
            }
            catch { return false; }

            // FTP OFF
            winProgress.ShowMessage("FTP Off.");
            foreach (ControllerInfo cont in lstTargetController)
            { sendSdcpCommand(SDCPClass.CmdFtpOff, cont.IPAddress); }

            // ●Cabinet Power On [2]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[2] Cabinet Power On."); }
            winProgress.ShowMessage("Cabinet Power On.");

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdUnitPowerOn, cont.IPAddress); }
            winProgress.PutForward1Step();

            return true;
        }

        private bool backupTargetUnitData(ControllerInfo controller, UnitInfo unit)
        {
            List<string> lstNonExistentFiles = new List<string>(); // 出力されていないファイル
            List<string> lstFtpFiles;

            // Reconfig
            winProgress.ShowMessage("Send Reconfig.");
            sendReconfig();

            // Backupコマンド発行
            winProgress.ShowMessage("Send Backup Command.");

            byte[] cmd = new byte[SDCPClass.CmdDataRead_UnitData.Length];
            Array.Copy(SDCPClass.CmdDataRead_UnitData, cmd, SDCPClass.CmdDataRead_UnitData.Length);

            cmd[9] += (byte)(unit.PortNo << 4);
            cmd[9] += (byte)unit.UnitNo;
            cmd[21] = 0x01;  // ud:0x01, hc:0x02, lc:0x20, hf:0x40

            sendSdcpCommand(cmd, controller.IPAddress);

            // Complete待ち
            winProgress.ShowMessage("Waiting for the response.");

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
            
            // Reconfig
            winProgress.ShowMessage("Send Reconfig.");
            sendReconfig();

            // Move File
            winProgress.ShowMessage("Move File.");

            string baseDir = applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo;
            string rbinDir = baseDir + "_Rbin\\"; // Rbinのフォルダ
            string latestDir = baseDir + "_Latest\\"; // Latestのフォルダ
            // Directory作成
            if (System.IO.Directory.Exists(rbinDir) != true)
            {
                try { System.IO.Directory.CreateDirectory(rbinDir); }
                catch (Exception ex)
                {
                    ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                    return false;
                }
            }

            try
            { getFileListFtp(controller.IPAddress, out lstFtpFiles); }
            catch
            { return false; }

            try
            { getFilesFtpRetry(controller.IPAddress, rbinDir, lstFtpFiles, out lstNonExistentFiles); }
            catch
            { return false; }

            // Check Data
            winProgress.ShowMessage("Check Cabinet Data.");

            string udFile = rbinDir + "u" + unit.PortNo.ToString("D2") + unit.UnitNo.ToString("D2") + "_ud.rbin";

            if (System.IO.File.Exists(udFile) != true || checkAllDataLength(udFile) != true || checkCommonHeaderDataType(udFile) != true)
            { return false; }

            // ●Data Convert
            winProgress.ShowMessage("Data Convert.");

            // Directory作成
            if (System.IO.Directory.Exists(latestDir) != true)
            {
                try { System.IO.Directory.CreateDirectory(latestDir); }
                catch (Exception ex)
                {
                    ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                    return false;
                }
            }

            convertRbinToBin(udFile, latestDir);

            return true;
        }

        private bool convWriteDataOnlyUnitData(UnitInfo unit)
        {
            string fileName;
            string initialDir, latestDir, previousDir, tempDir;
            string baseDir = applicationPath + "\\Backup\\" + dicController[unit.ControllerID].ModelName + "_" + dicController[unit.ControllerID].SerialNo;
            initialDir = baseDir + "_Initial";
            latestDir = baseDir + "_Latest";
            previousDir = baseDir + "_Previous";
            tempDir = baseDir + "_Temp";

            // Initialフォルダ存在しない場合作成
            if (System.IO.Directory.Exists(initialDir) != true)
            { System.IO.Directory.CreateDirectory(initialDir); }

            // Previousフォルダ存在しない場合作成
            if (System.IO.Directory.Exists(previousDir) != true)
            { System.IO.Directory.CreateDirectory(previousDir); }

            // Latestフォルダ存在しない場合作成
            if (System.IO.Directory.Exists(latestDir) != true)
            { System.IO.Directory.CreateDirectory(latestDir); }

            // Tempフォルダ存在しない場合作成
            if (System.IO.Directory.Exists(tempDir) != true)
            { System.IO.Directory.CreateDirectory(tempDir); }

            string orgFile = makeFilePath(unit, FileDirectory.Backup_Rbin, DataType.UnitData);
            if (checkAllDataLength(orgFile) == true && checkCommonHeaderDataType(orgFile) == true && confirmChecksum(orgFile) == true)
            {
                // LatestフォルダからPreviousフォルダにコピー
                string[] files = System.IO.Directory.GetFiles(latestDir, "*", System.IO.SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    fileName = "u" + unit.PortNo.ToString("D2") + unit.UnitNo.ToString("D2") + "_ud.bin";
                    if (files[i].Contains(fileName))
                    {
                        System.IO.File.Copy(files[i], previousDir + "\\" + fileName, true);
                        break;
                    }
                }

                // RbinフォルダからInitialフォルダとLatestフォルダ内にデータ変換して格納
                byte[] srcBytes = System.IO.File.ReadAllBytes(orgFile);
                byte[] convBytes = null;

                fileName = System.IO.Path.GetFileName(orgFile);
                initialDir = initialDir + "\\" + System.IO.Path.GetFileNameWithoutExtension(fileName) + ".bin";
                latestDir = latestDir + "\\" + System.IO.Path.GetFileNameWithoutExtension(fileName) + ".bin";

                convWriteDataUnitData(srcBytes, out convBytes);

                System.IO.File.WriteAllBytes(initialDir, convBytes);
                System.IO.File.WriteAllBytes(latestDir, convBytes);

                return true;
            }
            else
            { return false; }
        }

        private string getCabinetModelName(UnitInfo unit)
        {
            byte[] cmd;
            string buff, model;

            cmd = new byte[SDCPClass.CmdUnitModelNameGet.Length];
            Array.Copy(SDCPClass.CmdUnitModelNameGet, cmd, SDCPClass.CmdUnitModelNameGet.Length);

            cmd[9] += (byte)(unit.PortNo << 4);
            cmd[9] += (byte)unit.UnitNo;

            sendSdcpCommand(cmd, out buff, dicController[unit.ControllerID].IPAddress);
            model = convertAscii(buff);

            return model;
        }

        private string getCabinetSerialNumber(UnitInfo unit)
        {
            byte[] cmd;
            string buff, serial;

            cmd = new byte[SDCPClass.CmdUnitSerialGet.Length];
            Array.Copy(SDCPClass.CmdUnitSerialGet, cmd, SDCPClass.CmdUnitSerialGet.Length);

            cmd[9] += (byte)(unit.PortNo << 4);
            cmd[9] += (byte)unit.UnitNo;

            sendSdcpCommand(cmd, out buff, dicController[unit.ControllerID].IPAddress);
            serial = convertCabinetSerialNumber(buff);

            return serial;
        }

        private string getCabinetVersion(UnitInfo unit)
        {
            byte[] cmd;
            string buff, version;

            cmd = new byte[SDCPClass.CmdUnitFpgaVerGet.Length];
            Array.Copy(SDCPClass.CmdUnitFpgaVerGet, cmd, SDCPClass.CmdUnitFpgaVerGet.Length);

            cmd[9] += (byte)(unit.PortNo << 4);
            cmd[9] += (byte)unit.UnitNo;

            sendSdcpCommand(cmd, out buff, dicController[unit.ControllerID].IPAddress);
            version = convertVersion(buff);

            return version;
        }

        private string getLedElapsedTime(UnitInfo unit)
        {
            byte[] cmd;
            string buff, time;

            cmd = new byte[SDCPClass.CmdLedLightingElapsedTimeGet.Length];
            Array.Copy(SDCPClass.CmdLedLightingElapsedTimeGet, cmd, SDCPClass.CmdLedLightingElapsedTimeGet.Length);

            cmd[9] += (byte)(unit.PortNo << 4);
            cmd[9] += (byte)unit.UnitNo;

            sendSdcpCommand(cmd, out buff, dicController[unit.ControllerID].IPAddress);
            time = convertElapsedTime(buff);

            return time;
        }

        /// <summary>
        /// 指定項目のデータを取得
        /// </summary>
        /// <param name="lstConfigData">モジュールデータリスト</param>
        /// <param name="targetData">取得データ</param>
        /// <param name="offset">取得データの開始位置</param>
        /// <param name="dataLength">取得データのサイズ</param>
        /// <returns></returns>
        private string getSpecifiedRcsData(List<CompositeConfigData> lstConfigData, TargetData targetData, int offset, int dataLength)
        {
            string sData = "";
            byte[] tempByte = new byte[dataLength];

            for (int moduleNum = 1; moduleNum <= moduleCount; moduleNum++)
            {
                int count = 0;
                for (int idx = 0; idx < lstConfigData.Count; idx++)
                {
                    try
                    {
                        int option1 = Convert.ToInt32(lstConfigData[idx].header.Option1);
                        if (option1 == moduleNum)
                        {
                            Array.Copy(lstConfigData[idx].data, offset, tempByte, 0, tempByte.Length);

                            if (targetData == TargetData.UD_SerialNumber)
                            {
                                sData += convSerialNumber(tempByte) + ", ";
                            }
                            else if (targetData == TargetData.LC_ValidDataIndicator || targetData == TargetData.HC_ValidDataIndicator)
                            {
                                sData += convValidDataIndicator(tempByte) + ", ";
                            }
                            
                            lstConfigData.RemoveAt(idx);
                            break;
                        }
                        else
                        {
                            // Do nothing
                        }
                    }
                    catch
                    {
                        sData += ", ";
                        lstConfigData.RemoveAt(idx);
                        break;
                    }

                    count++;

                    // 検索対象のmoduleNumがlstConfigDataの中に無かった場合カンマのみを出力
                    if (lstConfigData.Count == count)
                    { sData += ", "; }
                }
            }

            return sData;
        }

        private string convertCabinetSerialNumber(string strSerialNumber)
        {
            if (isErrorResponseValue(strSerialNumber))
            { return STR_HYPHEN; }

            string strSerial;
            int intSerial;
            try
            {
                intSerial = Convert.ToInt32(strSerialNumber, 16);
                strSerial = intSerial.ToString("D7");
            }
            catch
            { strSerial = "000000000"; }

            return strSerial;
        }

        private string convertElapsedTime(string strElapsed)
        {
            if (isErrorResponseValue(strElapsed))
            { return STR_HYPHEN; }

            string time;
            try
            {
                int intTime = Convert.ToInt32(strElapsed, 16);
                time = intTime.ToString();
            }
            catch
            { time = "0000000"; }

            return time;
        }

        private string convSerialNumber(byte[] tempBytes)
        {
            int maxNum = 999999999;

            int serialNo = BitConverter.ToInt32(tempBytes, 0);

            if (serialNo < 0 || serialNo > maxNum)
            { return ""; }
            else
            { return serialNo.ToString().PadLeft(9, '0'); }
        }

        private string convValidDataIndicator(byte[] tempBytes)
        {
            string sValidIndicator;
            try
            { sValidIndicator = BitConverter.ToInt32(tempBytes, 0).ToString("X"); }
            catch
            { sValidIndicator = ""; }

            return sValidIndicator;
        }

        private int calcOutputAllocInfoProcessSec(int step, int cabinetCount, bool powerWait, int nomatchUdfileCabinetCount, int nomatchUdfileControllerCount, int completeFTPonNomatchUdfileControllerCount, int processedCabi, int processedNomatchUdfileCabi)
        {
            int processSec = 0;
            //係数　データ移動,レスポンス処理,コントローラー台数に影響する時間を除いた合計処理時間
            //commonA[0] [1] Check Controller Power.
            //           Controller loop
            //           Cabinet loop
            //commonA[1] [2] Cabinet Power On.
            int[] commonA = new int[2] { 0, 1 };

            //係数　コントローラー台数に影響のある各種処理時間
            //contB[0] [1] Check Controller Power.
            //contB[1] Controller loop
            //          Cabinet loop
            //contB[3] [2] Cabinet Power On.
            int[] contB = new int[3] { 1, 0, 0 };

            //係数　キャビネット台数に比例する処理時間
            int cabiSec = 2;

            //係数　対応するudファイルがないキャビネット台数に比例する処理時間
            int NomatchUdfileCabiSec = 40;

            //補正係数　キャビネット台数によって増加する個別Backup処理の処理時間を補正
            double supplementNum = 0.306;

            //係数　対応するudファイルがないコントローラー台数に比例する処理時間
            int NomatchUdfileContSec = 3;


            switch (step)
            {
                case int s when s <= 0: //[1] Check Controller Power. 以下の処理時間全体を計算
                    processSec = commonA.Sum() + dicController.Count * contB.Sum() + (Convert.ToInt32(powerWait) * 60)
                        + cabinetCount * cabiSec + (int)(nomatchUdfileCabinetCount * (NomatchUdfileCabiSec + cabinetCount * supplementNum)) + nomatchUdfileControllerCount * NomatchUdfileContSec;
                    break;
                case int s when s <= 1://Controller loop 以下の処理時間全体を計算
                    processSec = commonA.Skip(1).Sum() + dicController.Count * contB.Skip(1).Sum()
                        + cabinetCount * cabiSec + (int)(nomatchUdfileCabinetCount * (NomatchUdfileCabiSec + cabinetCount * supplementNum)) + nomatchUdfileControllerCount * NomatchUdfileContSec;
                    break;
                case int s when s <= 1 + cabinetCount://Cabinet loop のうち処理済みのキャビネットを除いた、以下の処理時間全体を計算
                    processSec = commonA.Skip(1).Sum() + dicController.Count * contB.Skip(1).Sum()
                        + cabinetCount * cabiSec + (int)(nomatchUdfileCabinetCount * (NomatchUdfileCabiSec + cabinetCount * supplementNum)) + nomatchUdfileControllerCount * NomatchUdfileContSec;
                    processSec -= completeFTPonNomatchUdfileControllerCount * NomatchUdfileContSec + processedCabi * cabiSec + (int)(processedNomatchUdfileCabi * (NomatchUdfileCabiSec + cabinetCount * supplementNum));
                    break;
                case int s when s <= 2 + cabinetCount://[2] Cabinet Power On.
                    processSec = commonA.Skip(1).Sum() + dicController.Count * contB.Skip(2).Sum();
                    break;
                default:
                    break;
            }

            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("calcOutputAllocInfoProcessSec step:" + step + " cabinetCount:" + cabinetCount + " powerWait:" + powerWait + " nomatchUdfileCabinetCount:" + nomatchUdfileCabinetCount + " nomatchUdfileControllerCount:" + nomatchUdfileControllerCount + " completeFTPonNomatchUdfileControllerCount:" + completeFTPonNomatchUdfileControllerCount + " processedCabi:" + processedCabi + " processedNomatchUdfileCabi:" + processedNomatchUdfileCabi + " processSec:" + processSec); }

            return processSec;
        }
        #endregion
    }
}
