using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using System.Windows.Threading;

namespace CAS
{
    /// <summary>
    /// WindowUnitInfo.xaml の相互作用ロジック
    /// </summary>
    public partial class WindowUnitInfo : Window
    {
        UnitInfo unit;
        DispatcherTimer timer;
        bool showFirst;

        private const int WINDOW_UNITINFO_HEIGHT_CHIRON = 470;
        private const int GRID_MODULEINFO_HTIGHT_CHIRON = 325;
        private const int TEXTBOX_SERIAL_NUMBER_HTIGHT_CHIRON = 200;

        private const int WINDOW_UNITINFO_HEIGHT_CANCUN = 570;
        private const int GRID_MODULEINFO_HTIGHT_CANCUN = 425;
        private const int TEXTBOX_SERIAL_NUMBER_HTIGHT_CANCUN = 300;

        public WindowUnitInfo(UnitInfo unitInfo)
        {
            InitializeComponent();

            unit = unitInfo;

            lbUnit.Content = "(" + unit.X + ", " + unit.Y + ") C" + unit.ControllerID + "-" + unit.PortNo + "-" + unit.UnitNo;

            initModuleInfoGrid();

            timer = new DispatcherTimer(DispatcherPriority.Normal);
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();

            showFirst = true;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            timer.Tick -= timer_Tick;
            timer.Stop();
            this.Close();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            showUnitInfo();
        }

        private bool showUnitInfo()
        {
            txbUnitSerial.Text = unit.SerialNo;
            txbUnitModelName.Text = unit.ModelName;
            txbUnitVersion.Text = unit.Version;

            if (String.IsNullOrWhiteSpace(unit.UnitDataFile) != true && showFirst == true)
            {
                //loadUcBoardInfo();
                loadModuleInfo();
                showFirst = false;
            }

            return true;
        }

        private bool loadModuleInfo()
        {
            if (!File.Exists(unit.UnitDataFile))
            { return false; }

            List<CompositeConfigData> lstModuleInfo = CompositeConfigData.getCompositeConfigDataList(unit.UnitDataFile, MainWindow.dt_LedModuleInfoRead);
            foreach (CompositeConfigData moduleInfo in lstModuleInfo)
            {
                try
                {
                    int option1 = Convert.ToInt32(moduleInfo.header.Option1);
                    byte[] serialNumber = new byte[MainWindow.UdModuleSerialNoSize];
                    Array.Copy(moduleInfo.data, MainWindow.UdModuleSerialNoOffset, serialNumber, 0, MainWindow.UdModuleSerialNoSize);

                    switch (option1)
                    {
                        case 1:
                            sn1.Text = convSerialNumber(serialNumber);
                            break;
                        case 2:
                            sn2.Text = convSerialNumber(serialNumber);
                            break;
                        case 3:
                            sn3.Text = convSerialNumber(serialNumber);
                            break;
                        case 4:
                            sn4.Text = convSerialNumber(serialNumber);
                            break;
                        case 5:
                            sn5.Text = convSerialNumber(serialNumber);
                            break;
                        case 6:
                            sn6.Text = convSerialNumber(serialNumber);
                            break;
                        case 7:
                            sn7.Text = convSerialNumber(serialNumber);
                            break;
                        case 8:
                            sn8.Text = convSerialNumber(serialNumber);
                            break;
                        case 9:
                            sn9.Text = convSerialNumber(serialNumber);
                            break;
                        case 10:
                            sn10.Text = convSerialNumber(serialNumber);
                            break;
                        case 11:
                            sn11.Text = convSerialNumber(serialNumber);
                            break;
                        case 12:
                            sn12.Text = convSerialNumber(serialNumber);
                            break;
                        default:
                            break;
                    }
                }
                catch { }
            }
            return true;
        }

        private void initModuleInfoGrid()
        {
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            {
                tbText9.Visibility = Visibility.Collapsed;
                tbText10.Visibility = Visibility.Collapsed;
                tbText11.Visibility = Visibility.Collapsed;
                tbText12.Visibility = Visibility.Collapsed;
                sn9.Visibility = Visibility.Collapsed;
                sn10.Visibility = Visibility.Collapsed;
                sn11.Visibility = Visibility.Collapsed;
                sn12.Visibility = Visibility.Collapsed;
                tbSn.Height = TEXTBOX_SERIAL_NUMBER_HTIGHT_CHIRON;
                gdModuleInfo.Height = GRID_MODULEINFO_HTIGHT_CHIRON;
                this.Height = WINDOW_UNITINFO_HEIGHT_CHIRON;
            }
            else
            {
                tbText9.Visibility = Visibility.Visible;
                tbText10.Visibility = Visibility.Visible;
                tbText11.Visibility = Visibility.Visible;
                tbText12.Visibility = Visibility.Visible;
                sn9.Visibility = Visibility.Visible;
                sn10.Visibility = Visibility.Visible;
                sn11.Visibility = Visibility.Visible;
                sn12.Visibility = Visibility.Visible;
                tbSn.Height = TEXTBOX_SERIAL_NUMBER_HTIGHT_CANCUN;
                gdModuleInfo.Height = GRID_MODULEINFO_HTIGHT_CANCUN;
                this.Height = WINDOW_UNITINFO_HEIGHT_CANCUN;
            }
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

        //private bool loadUcBoardInfo()
        //{
        //    byte[] srcBytes;

        //    try { srcBytes = System.IO.File.ReadAllBytes(unit.UnitDataFile); }
        //    catch { return false; }

        //    Board Serial
        //     uc_board_data(512)->uc_board_info(448)->BS0(0)
        //    byte[] aryUcSerial = new byte[7];
        //    Array.Copy(srcBytes, 512 + 448, aryUcSerial, 0, 7);
        //    string serial = "";
        //    for (int i = 1; i <= aryUcSerial.Length; i++)
        //    { serial += aryUcSerial[aryUcSerial.Length - i].ToString("X1"); }
        //    txbUcSerial.Text = BitConverter.ToString(aryUcSerial).Replace("-", string.Empty);
        //    txbUcSerial.Text = serial;

        //    Board Name
        //     unit_info(8)->BN0(8)
        //    byte[] aryUcBoardName = new byte[8];
        //    Array.Copy(srcBytes, 8 + 8, aryUcBoardName, 0, 8);
        //    txbUcBoardName.Text = reverseStr(convertAscii(aryUcBoardName));

        //    Parts Number
        //     unit_info(8)->BPN0(32)
        //    byte[] aryUcPartsNum = new byte[4];
        //    Array.Copy(srcBytes, 8 + 32, aryUcPartsNum, 0, 4);
        //    string str = "1" + reverseStr(BitConverter.ToString(aryUcPartsNum).Replace("-", string.Empty));
        //    string str = "1";
        //    for (int i = aryUcPartsNum.Length - 1; i >= 0; i--)
        //    { str += aryUcPartsNum[i].ToString("X2"); }
        //    txbUcPartsNum.Text = str.Substring(0, 1) + "-" + str.Substring(1, 3) + "-" + str.Substring(4, 3) + "-" + str.Substring(7, 2);

        //    Mount - A
        //     unit_info(8)->MNA0(36)
        //    byte[] aryUcMountA = new byte[4];
        //    Array.Copy(srcBytes, 8 + 36, aryUcMountA, 0, 4);
        //    str = "A";// +reverseStr(BitConverter.ToString(aryUcMountA).Replace("-", string.Empty));
        //    for (int i = aryUcMountA.Length - 1; i >= 0; i--)
        //    { str += aryUcMountA[i].ToString("X2"); }
        //    str = convertLastChar(str);
        //    txbUcMountA.Text = str.Substring(0, 1) + "-" + str.Substring(1, 4) + "-" + str.Substring(5, 3) + "-" + str.Substring(8, 1);

        //    Compl - A
        //     unit_info(8)->CPA0(40)
        //    byte[] aryUcComplA = new byte[4];
        //    Array.Copy(srcBytes, 8 + 40, aryUcComplA, 0, 4);
        //    str = "A";// +reverseStr(BitConverter.ToString(aryUcComplA).Replace("-", string.Empty));
        //    for (int i = aryUcComplA.Length - 1; i >= 0; i--)
        //    { str += aryUcComplA[i].ToString("X2"); }
        //    str = convertLastChar(str);
        //    txbUcComplA.Text = str.Substring(0, 1) + "-" + str.Substring(1, 4) + "-" + str.Substring(5, 3) + "-" + str.Substring(8, 1);

        //    return true;
        //}

        //    private string convertAscii(byte[] code)
        //    {
        //        string text = "";

        //        for (int i = 0; i < code.Length; i++)
        //        {
        //            if(code[i] == 0)
        //            { continue; }

        //            char cha = (char)Convert.ToInt32(code[i]);
        //            text += cha;
        //        }

        //        return text.Trim();
        //    }

        //    private string convertLastChar(string str)
        //    {
        //        string lastChar = str.Substring(str.Length - 1, 1).ToLower();
        //        string code = "";

        //        if(lastChar == "0")
        //        { code = "A"; }
        //        else if (lastChar == "1")
        //        { code = "B"; }
        //        else if (lastChar == "2")
        //        { code = "C"; }
        //        else if (lastChar == "3")
        //        { code = "D"; }
        //        else if (lastChar == "4")
        //        { code = "E"; }
        //        else if (lastChar == "5")
        //        { code = "F"; }
        //        else if (lastChar == "6")
        //        { code = "G"; }
        //        else if (lastChar == "7")
        //        { code = "H"; }
        //        else if (lastChar == "8")
        //        { code = "I"; }
        //        else if (lastChar == "9")
        //        { code = "J"; }
        //        else if (lastChar == "a")
        //        { code = "K"; }
        //        else if (lastChar == "b")
        //        { code = "L"; }
        //        else if (lastChar == "c")
        //        { code = "M"; }
        //        else if (lastChar == "d")
        //        { code = "N"; }
        //        else if (lastChar == "e")
        //        { code = "O"; }
        //        else if (lastChar == "f")
        //        { code = "P"; }

        //        return str.Substring(0, str.Length - 1) + code;
        //    }
        //}
    }
}
