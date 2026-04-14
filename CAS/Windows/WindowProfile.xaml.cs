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
using System.Windows.Shapes;
using Microsoft.Win32;
using CAS.Converters;

namespace CAS
{
    public delegate bool Dele_sendSdcpCommand(Byte[] aryCmd, out string RecievedBuffer, string ip);
    public delegate bool Dele_sendAdcpCommand(string cmd, out string RecievedBuffer, string ip, string pw);

    /// <summary>
    /// WindowProfile.xaml の相互作用ロジック
    /// </summary>
    public partial class WindowProfile : Window
    {
        public ProfileMode Mode;
        public Dele_sendSdcpCommand dele_sendSdcpCommand;
        public Dele_sendAdcpCommand dele_sendAdcpCommand;

        public string ProfileFilePath;
        public Dictionary<int, ControllerInfo> dicController;
        public AllocationInfo allocInfo;

        private WindowProgress winProgress;

        public WindowProfile()
        {
            InitializeComponent();
        }

        private void btnOpenProfile_Click(object sender, RoutedEventArgs e)
        {
            Mode = ProfileMode.File;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.FilterIndex = 1;
            openFileDialog.Filter = "Profile File(.xml)|*.xml|All Files (*.*)|*.*";
            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                ProfileFilePath = openFileDialog.FileName;
                DialogResult = true;
            }
            else
            { DialogResult = false; }
        }

        private async void btnInputLayout_Click(object sender, RoutedEventArgs e)
        {
            Mode = ProfileMode.Manual;
            string res;

            List<ControllerInfo> lstControllerInfo = new List<ControllerInfo>();
            WindowControllerCount winContCount = new WindowControllerCount();

            bool? result = winContCount.ShowDialog();
            if (result == false)
            { return; }

            int count = winContCount.cmbxControllerCount.SelectedIndex + 1;
            for (int i = 0; i < count; i++)
            {
                ControllerInfo info = new ControllerInfo();
                while (true)
                {
                    // IPAdress
                    WindowControllerIP winIP = new WindowControllerIP();
                    winIP.txbIP.Focus();
                    if (winIP.ShowDialog() == false)
                    { return; }

                    // TODO: IPアドレスのフォーマットチェックも良いかも
                    info.IPAddress = winIP.txbIP.Text;

                    // Password
                    WindowInputPassword winPw = new WindowInputPassword();
                    winPw.txbPw.Focus();
                    if (winPw.ShowDialog() == false)
                    { return; }

                    // Password check
                    if (dele_sendAdcpCommand(ADCPClass.CmdControllerPowerStausGet, out res, winIP.txbIP.Text, winPw.txbPw.Password))
                    {
                        info.Password = winPw.txbPw.Password;
                        break;
                    }
                }

                // IPAdress重複チェック
                foreach (ControllerInfo controller in lstControllerInfo)
                {
                    if (controller.IPAddress == info.IPAddress)
                    {
                        string msg = "Failed to get Layout Info.\r\nBecause the IP address is duplicated.";
                        ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 380, 210);
                        return;
                    }
                }

                lstControllerInfo.Add(info);
            }

            // ◆Unit Allocation情報
            winProgress = new WindowProgress("Get Allocation Info.");
            winProgress.Show();

            try
            {
                result = await Task.Run(() => getAllocationInfo(lstControllerInfo));
                if (result == true)
                { DialogResult = true; }
            }
            catch (Exception ex)
            {
                string msg = "Can not get the Allocation Info.\r\n" + ex.Message;
                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return;
            }

            winProgress.Close();
        }

        private bool getAllocationInfo(List<ControllerInfo> lstControllerInfo)
        {
            // ◆Controller情報
            dicController = new Dictionary<int, ControllerInfo>();
            // ◆Allocation情報
            allocInfo = new AllocationInfo();
            List<UnitInfo> lstUnitInfo = new List<UnitInfo>();

            // ●進捗の最大値を設定
            winProgress.ShowMessage("Set Step Count.");
            int step = lstControllerInfo.Count * 4;
            winProgress.SetWholeSteps(step);

            string msg, buff;
            foreach (ControllerInfo controller in lstControllerInfo)
            {
                winProgress.ShowMessage("Controller Power off.");

                // Controller Standby
                PowerStatus powerStatus;
                if (getPowerStatus(controller.IPAddress, out powerStatus) != true)
                { return false; }

                if (powerStatus != PowerStatus.Standby)
                {
                    if (dele_sendSdcpCommand(SDCPClass.CmdStandby, out buff, controller.IPAddress) != true)
                    { return false; }

                    DateTime startTime = DateTime.Now;
                    while (true)
                    {
                        if (getPowerStatus(controller.IPAddress, out powerStatus) != true)
                        { return false; }

                        if (powerStatus == PowerStatus.Shutting_Down || powerStatus == PowerStatus.Standby)
                        { break; }

                        TimeSpan span = DateTime.Now - startTime;
                        if (span.TotalSeconds > MainWindow.CONTROLLER_POWER_STATUS_TIMEOUT)
                        {
                            msg = "Failed to standby.";
                            ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                            return false;
                        }

                        System.Threading.Thread.Sleep(1000);
                    }
                }
                winProgress.PutForward1Step();
            }

            foreach (ControllerInfo controller in lstControllerInfo)
            {
                winProgress.ShowMessage("Get Controller ID.");

                //Controller ID
                dele_sendSdcpCommand(SDCPClass.CmdControllerIDGet, out buff, controller.IPAddress);
                int controllerCount = int.Parse(buff.Substring(2, 2));
                int controllerID = int.Parse(buff.Substring(0, 2));

                if (buff == MainWindow.SDCP_NAK_NOT_APPLICABLE || controllerCount == 0 || controllerID == 0)
                {
                    msg = "Failed to get ControllerID.";
                    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 380, 210);
                    return false;
                }
                else
                { controller.ControllerID = controllerID; }

                // Controller ID重複チェック
                if (dicController.ContainsKey(controller.ControllerID))
                {
                    msg = "Failed to get Layout Info.\r\nBecause the ControllerID is duplicated.";
                    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 380, 210);
                    return false;
                }

                dicController.Add(controller.ControllerID, controller);
                winProgress.PutForward1Step();
            }
                
            msg = "Failed to get Layout Info.\r\nBecause the cabinet model for each controller does not match.";
            foreach (ControllerInfo controller in dicController.Values)
            {
                string ledModel;
                
                winProgress.ShowMessage("Controller : " + controller.ControllerID + "\r\nGet LED Model.");

                // Master / Slave
                dele_sendSdcpCommand(SDCPClass.CmdControllerMasterSlaveGet, out buff, controller.IPAddress);
                if (buff == "01")
                { controller.Master = true; }
                else
                { controller.Master = false; }

                // Get LED Model
                ledModel = allocInfo.LEDModel;

                if (getLedModel(controller.IPAddress, controller.Password) != true)
                {
                    msg = "Failed to get LED Model.\r\nController ID: " + controller.ControllerID;
                    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 380, 210);
                    return false;
                }
                
                msg += "\r\nController " + controller.ControllerID + ": " + allocInfo.LEDModel;
                if (ledModel != null && ledModel != allocInfo.LEDModel)
                {
                    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 500, 210);
                    return false;
                }

                setConfigChromType(allocInfo.LEDModel);

                winProgress.PutForward1Step();
            }

            foreach (ControllerInfo controller in dicController.Values)
            {
                int maxUnitNo;

                if (allocInfo.LEDModel == MainWindow.ZRD_B15A
                    || allocInfo.LEDModel == MainWindow.ZRD_C15A
                    || allocInfo.LEDModel == MainWindow.ZRD_BH15D 
                    || allocInfo.LEDModel == MainWindow.ZRD_CH15D
                    || allocInfo.LEDModel == MainWindow.ZRD_BH15D_S3
                    || allocInfo.LEDModel == MainWindow.ZRD_CH15D_S3)
                {
                    maxUnitNo = Defined.Max_UnitNo_P15;
                }
                else
                {
                    maxUnitNo = Defined.Max_UnitNo_P12;
                }

                // Get Allocation/Picture Info
                for (int i = 0; i < 12; i++)
                {
                    for (int j = 0; j < maxUnitNo; j++)
                    {
                        UnitInfo info = new UnitInfo();
                        info.Enable = true;
                        info.ControllerID = controller.ControllerID;
                        info.PortNo = i + 1;
                        info.UnitNo = j + 1;
                        info.CellDataFile = "u" + info.PortNo.ToString("D2") + info.UnitNo.ToString("D2") + "_cd.bin";
                        info.AgingDataFile = "u" + info.PortNo.ToString("D2") + info.UnitNo.ToString("D2") + "_ad.bin";

                        // Allocation Info
                        byte[] cmd = new byte[SDCPClass.CmdUnitAllocationAddressGet.Length];
                        Array.Copy(SDCPClass.CmdUnitAllocationAddressGet, cmd, SDCPClass.CmdUnitAllocationAddressGet.Length);

                        string port = (i + 1).ToString("X1");
                        string unit = (j + 1).ToString("X1");

                        cmd[9] = (byte)Convert.ToInt32(port + unit, 16);

                        dele_sendSdcpCommand(cmd, out buff, controller.IPAddress);

                        string strX = buff.Substring(0, 4);
                        string strY = buff.Substring(4, 4);

                        info.X = Convert.ToInt32(strX, 16);
                        info.Y = Convert.ToInt32(strY, 16);

                        if (info.X == 0 && info.Y == 0)
                        { break; }

                        winProgress.ShowMessage("Controller : " + controller.ControllerID + "\r\nPort No. : " + (i + 1) + ", Cabinet No. : " + (j + 1) + " (Allocation Info.)");

                        // Picture Info
                        cmd = new byte[SDCPClass.CmdUnitPictureAddressGet.Length];
                        Array.Copy(SDCPClass.CmdUnitPictureAddressGet, cmd, SDCPClass.CmdUnitPictureAddressGet.Length);

                        cmd[9] = (byte)Convert.ToInt32(port + unit, 16);

                        dele_sendSdcpCommand(cmd, out buff, controller.IPAddress);

                        string strPixX = buff.Substring(0, 4);
                        string strPixY = buff.Substring(4, 4);

                        info.PixelX = Convert.ToInt32(strPixX, 16);
                        info.PixelY = Convert.ToInt32(strPixY, 16);
                        
                        winProgress.ShowMessage("Controller : " + controller.ControllerID + "\r\nPort No. : " + (i + 1) + ", Cabinet No. : " + (j + 1) + " (Picture Info.)");

                        lstUnitInfo.Add(info);
                        controller.UnitCount++;
                    }
                }
                winProgress.PutForward1Step();
            }

            // X, Yの最大値検索
            int maxX = 0, maxY = 0;
            foreach (UnitInfo info in lstUnitInfo)
            {
                if (info.X > maxX)
                { maxX = info.X; }

                if (info.Y > maxY)
                { maxY = info.Y; }
            }

            allocInfo.MaxX = maxX;
            allocInfo.MaxY = maxY;

            // Allocation情報を初期化
            for (int i = 0; i < allocInfo.MaxX; i++)
            {
                allocInfo.lstUnits.Add(new List<UnitInfo>());

                for (int j = 0; j < allocInfo.MaxY; j++)
                {
                    UnitInfo unit = null; //new UnitInfo();
                    allocInfo.lstUnits[i].Add(unit);
                }
            }

            foreach (UnitInfo info in lstUnitInfo)
            { allocInfo.lstUnits[info.X - 1][info.Y - 1] = info; }

            return true;
        }

        private bool getLedModel(string ip, string pw)
        {
            string res;
            bool result = true;

            dele_sendAdcpCommand(ADCPClass.CmdLedModelGet, out res, ip, pw);

            if (res != MainWindow.ADCP_ERROR_COMMAND)
            {
                switch (res)
                {
                    case ADCPClass.ZRD_B12A:
                        allocInfo.LEDModel = MainWindow.ZRD_B12A;
                        break;
                    case ADCPClass.ZRD_B15A:
                        allocInfo.LEDModel = MainWindow.ZRD_B15A;
                        break;
                    case ADCPClass.ZRD_C12A:
                        allocInfo.LEDModel = MainWindow.ZRD_C12A;
                        break;
                    case ADCPClass.ZRD_C15A:
                        allocInfo.LEDModel = MainWindow.ZRD_C15A;
                        break;
                    case ADCPClass.ZRD_BH12D:
                        allocInfo.LEDModel = MainWindow.ZRD_BH12D;
                        break;
                    case ADCPClass.ZRD_BH15D:
                        allocInfo.LEDModel = MainWindow.ZRD_BH15D;
                        break;
                    case ADCPClass.ZRD_CH12D:
                        allocInfo.LEDModel = MainWindow.ZRD_CH12D;
                        break;
                    case ADCPClass.ZRD_CH15D:
                        allocInfo.LEDModel = MainWindow.ZRD_CH15D;
                        break;
                    case ADCPClass.ZRD_BH12D_S3:
                        allocInfo.LEDModel = MainWindow.ZRD_BH12D_S3;
                        break;
                    case ADCPClass.ZRD_BH15D_S3:
                        allocInfo.LEDModel = MainWindow.ZRD_BH15D_S3;
                        break;
                    case ADCPClass.ZRD_CH12D_S3:
                        allocInfo.LEDModel = MainWindow.ZRD_CH12D_S3;
                        break;
                    case ADCPClass.ZRD_CH15D_S3:
                        allocInfo.LEDModel = MainWindow.ZRD_CH15D_S3;
                        break;
                    default:
                        allocInfo.LEDModel = "-";
                        result = false;
                        break;
                }
            }
            else
            {
                dele_sendAdcpCommand(ADCPClass.CmdLedPitchGet, out res, ip, pw);

                if (res == ADCPClass.LED_PITCH1_2)
                { allocInfo.LEDModel = MainWindow.ZRD_B12A; }
                else if (res == ADCPClass.LED_PITCH1_5)
                { allocInfo.LEDModel = MainWindow.ZRD_B15A; }
                else
                {
                    allocInfo.LEDModel = "-";
                    result = false;
                }
            }

            return result;
        }

        public void setConfigChromType(string ledModel)
        {
            switch (ledModel)
            {
                case MainWindow.ZRD_B12A:
                    Settings.Ins.ConfigChromType = ConfigChrom.ZRD_B12A;
                    break;
                case MainWindow.ZRD_B15A:
                    Settings.Ins.ConfigChromType = ConfigChrom.ZRD_B15A;
                    break;
                case MainWindow.ZRD_C12A:
                    Settings.Ins.ConfigChromType = ConfigChrom.ZRD_C12A;
                    break;
                case MainWindow.ZRD_C15A:
                    Settings.Ins.ConfigChromType = ConfigChrom.ZRD_C15A;
                    break;
                case MainWindow.ZRD_BH12D:
                    Settings.Ins.ConfigChromType = ConfigChrom.ZRD_BH12D;
                    break;
                case MainWindow.ZRD_BH15D:
                    Settings.Ins.ConfigChromType = ConfigChrom.ZRD_BH15D;
                    break;
                case MainWindow.ZRD_CH12D:
                    Settings.Ins.ConfigChromType = ConfigChrom.ZRD_CH12D;
                    break;
                case MainWindow.ZRD_CH15D:
                    Settings.Ins.ConfigChromType = ConfigChrom.ZRD_CH15D;
                    break;
                case MainWindow.ZRD_BH12D_S3:
                    Settings.Ins.ConfigChromType = ConfigChrom.ZRD_BH12D_S3;
                    break;
                case MainWindow.ZRD_BH15D_S3:
                    Settings.Ins.ConfigChromType = ConfigChrom.ZRD_BH15D_S3;
                    break;
                case MainWindow.ZRD_CH12D_S3:
                    Settings.Ins.ConfigChromType = ConfigChrom.ZRD_CH12D_S3;
                    break;
                case MainWindow.ZRD_CH15D_S3:
                    Settings.Ins.ConfigChromType = ConfigChrom.ZRD_CH15D_S3;
                    break;
            }
        }

        private bool getPowerStatus(string ip, out PowerStatus status)
        {
            string buff;

            if (dele_sendSdcpCommand(SDCPClass.CmdControllerPowerStausGet, out buff, ip) != true)
            {
                status = PowerStatus.Unknown;
                return false;
            }

            if (buff == "00")
            { status = PowerStatus.Standby; }
            else if (buff == "01")
            { status = PowerStatus.Power_On; }
            else if (buff == "02")
            { status = PowerStatus.Update; }
            else if (buff == "03")
            { status = PowerStatus.Startup; }
            else if (buff == "04")
            { status = PowerStatus.Shutting_Down; }
            else if (buff == "05")
            { status = PowerStatus.Initializing; }
            else
            {
                status = PowerStatus.Unknown;
                return false;
            }

            return true;
        }

        public static bool ShowMessageWindow(string msg, string caption, System.Drawing.Icon icon, int width, int height)
        {
            var dispatcher = Application.Current.Dispatcher;
            bool? tempResult = false;

            dispatcher.Invoke(() =>
            {
                WindowMessage winMessage = new WindowMessage(msg, caption, icon);
                winMessage.ChangeWindowSize(width, height);

                tempResult = winMessage.ShowDialog();
            });

            return true;
        }
    }
}
