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
using System.Windows.Threading;

namespace CAS
{
    /// <summary>
    /// WindowControllerError.xaml の相互作用ロジック
    /// </summary>
    public partial class WindowControllerError : Window
    {
        ControllerInfo cont;
        DispatcherTimer timer;

        public WindowControllerError(ControllerInfo controller)
        {
            InitializeComponent();

            cont = controller;
                        
            lbController.Content = "Controller-" + cont.ControllerID.ToString();

            timer = new DispatcherTimer(DispatcherPriority.Normal);
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            showInfo();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            timer.Tick -= timer_Tick;
            timer.Stop();
            this.Close();
        }

        private void showInfo()
        {
            lbxError.Items.Clear();

            // Error / Warning
            UInt32 errorCode = Convert.ToUInt32(cont.Error, 16);

            if (errorCode != 0)
            {
                if ((errorCode & 0x01) != 0)
                { lbxError.Items.Add("Power Error (001-00-00)"); }

                if ((errorCode & 0x02) != 0)
                { lbxError.Items.Add("Power Error (002-00-00)"); }

                if ((errorCode & 0x04) != 0)
                { lbxError.Items.Add("Power Error (003-00-00)"); }

                if ((errorCode & 0x08) != 0)
                { lbxError.Items.Add("Temperature Error (012-00-00)"); }

                if ((errorCode & 0x10) != 0)
                { lbxError.Items.Add("Temperature Error (013-00-00)"); }

                if ((errorCode & 0x20) != 0)
                { lbxError.Items.Add("Board Error (021-00-00)"); }

                if ((errorCode & 0x40) != 0)
                { lbxError.Items.Add("Board Error (022-00-00)"); }

                if ((errorCode & 0x80) != 0)
                { lbxError.Items.Add("Internal Error (023-00-00)"); }

                if ((errorCode & 0x100) != 0)
                { lbxError.Items.Add("Board Error (032-00-00)"); }

                if ((errorCode & 0x200) != 0)
                { lbxError.Items.Add("Board Error (033-00-00)"); }

                if ((errorCode & 0x400) != 0)
                { lbxError.Items.Add("Internal Error (050-00-00)"); }

                if ((errorCode & 0x800) != 0)
                { lbxError.Items.Add("Temperature Error (017-00-00)"); }
            }

            UInt64 warningCode = Convert.ToUInt64(cont.Warning, 16);

            if (warningCode != 0)
            {
                if ((warningCode & 0x01) != 0)
                { lbxError.Items.Add("Cabinet Warning (230-00-00)"); }

                if ((warningCode & 0x02) != 0)
                { lbxError.Items.Add("Update Warning (240-00-11)"); }

                if ((warningCode & 0x04) != 0)
                { lbxError.Items.Add("Update Warning (240-00-21)"); }

                if ((warningCode & 0x08) != 0)
                { lbxError.Items.Add("Update Warning (240-00-22)"); }

                if ((warningCode & 0x10) != 0)
                { lbxError.Items.Add("Update Warning (240-00-23)"); }

                if ((warningCode & 0x20) != 0)
                { lbxError.Items.Add("Update Warning (240-00-24)"); }

                if ((warningCode & 0x40) != 0)
                { lbxError.Items.Add("Update Warning (240-00-31)"); }

                if ((warningCode & 0x400) != 0)
                { lbxError.Items.Add("Temperature Warning (212-00-00)"); }

                if ((warningCode & 0x8000) != 0)
                { lbxError.Items.Add("Temperature Warning (213-00-00)"); }

                if ((warningCode & 0x20000) != 0)
                { lbxError.Items.Add("Internal Warning (233-00-11)"); }

                if ((warningCode & 0x40000) != 0)
                { lbxError.Items.Add("Link Warning (233-00-21)"); }

                if ((warningCode & 0x80000) != 0)
                { lbxError.Items.Add("Link Warning (233-00-23)"); }

                if ((warningCode & 0x100000) != 0)
                { lbxError.Items.Add("Temperature Warning (214-00-00)"); }

                if ((warningCode & 0x200000) != 0)
                { lbxError.Items.Add("Fan Warning (215-00-11)"); }

                if ((warningCode & 0x400000) != 0)
                { lbxError.Items.Add("Fan Warning (215-00-12)"); }

                if ((warningCode & 0x800000) != 0)
                { lbxError.Items.Add("Fan Warning (216-00-11)"); }

                if ((warningCode & 0x1000000) != 0)
                { lbxError.Items.Add("Fan Warning (216-00-12)"); }

                if ((warningCode & 0x4000000) != 0)
                { lbxError.Items.Add("Temperature Sensor Warning (220-00-00)"); }

                if ((warningCode & 0x8000000) != 0)
                { lbxError.Items.Add("Temperature Sensor Warning (222-00-13)"); }

                if ((warningCode & 0x10000000) != 0)
                { lbxError.Items.Add("Temperature Sensor Warning (223-00-13)"); }

                if ((warningCode & 0x20000000) != 0)
                { lbxError.Items.Add("Temperature Sensor Warning (224-00-00)"); }

                if ((warningCode & 0x40000000) != 0)
                { lbxError.Items.Add("Update Warning (240-00-25)"); }

                if ((warningCode & 0x80000000) != 0)
                { lbxError.Items.Add("Update Warning (240-00-26)"); }

                if ((warningCode & 0x100000000) != 0)
                { lbxError.Items.Add("Standby Cause No Signal (401-00-00)"); }

                if ((warningCode & 0x200000000) != 0)
                { lbxError.Items.Add("LED Model Warning (430-00-00)"); }
            }

           for (int tahitiNo = 0; tahitiNo < cont.TahitiWarningList.Count / 2; tahitiNo++) //TX
           {
               UInt64 tahitiTxWarningCode;
               string tahitiNoText = (tahitiNo+1).ToString("X1");

               try { tahitiTxWarningCode = Convert.ToUInt64(cont.TahitiWarningList[tahitiNo], 16); }
               catch { tahitiTxWarningCode = 0; }

               if ((tahitiTxWarningCode & 0x01) != 0)
               { lbxError.Items.Add("Temperature Warning (511-1" + tahitiNoText + "-00)"); }

               if ((tahitiTxWarningCode & 0x02) != 0)
               { lbxError.Items.Add("Connection Warning (531-1" + tahitiNoText + "-00)"); }

               if ((tahitiTxWarningCode & 0x04) != 0)
               { lbxError.Items.Add("Connection Warning (531-1" + tahitiNoText + "-10)"); }

               if ((tahitiTxWarningCode & 0x08) != 0)
               { lbxError.Items.Add("Connection Warning (532-1" + tahitiNoText + "-10)"); }

           }

           for (int tahitiNo = 0; tahitiNo < cont.TahitiWarningList.Count / 2; tahitiNo++) //RX
           {
               UInt64 tahitiRxWarningCode;
               string tahitiNoText = (tahitiNo + 1).ToString("X1");

               try { tahitiRxWarningCode = Convert.ToUInt64(cont.TahitiWarningList[tahitiNo + cont.TahitiWarningList.Count / 2], 16); }
               catch { tahitiRxWarningCode = 0; }

               if ((tahitiRxWarningCode & 0x01) != 0)
               { lbxError.Items.Add("Temperature Warning (511-2" + tahitiNoText + "-00)"); }

               if ((tahitiRxWarningCode & 0x02) != 0)
               { lbxError.Items.Add("Connection Warning (531-2" + tahitiNoText + "-00)"); }

               if ((tahitiRxWarningCode & 0x04) != 0)
               { lbxError.Items.Add("Connection Warning (531-2" + tahitiNoText + "-10)"); }

               if ((tahitiRxWarningCode & 0x08) != 0)
               { lbxError.Items.Add("Connection Warning (532-2" + tahitiNoText + "-10)"); }
           }

            // Temp
            txbIfTemp.Text = cont.TempInfo.IF_Temp;
            txbPsTemp.Text = "-";//cont.TempInfo.PS_Temp;
            txbDifTemp.Text = cont.TempInfo.DIF_Temp;
            txbPifTemp.Text = cont.TempInfo.PIF_Temp;
        }
    }
}
