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
    /// WindowControllerInfo.xaml の相互作用ロジック
    /// </summary>
    public partial class WindowControllerInfo : Window
    {
        ControllerInfo cont;
        DispatcherTimer timer;

        public WindowControllerInfo(ControllerInfo controller)
        {
            InitializeComponent();

            cont = controller;

            lbController.Content = "Controller-" + cont.ControllerID.ToString();

            timer = new DispatcherTimer(DispatcherPriority.Normal);
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            timer.Tick -= timer_Tick;
            timer.Stop();
            this.Close();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            showControllerInfo();
        }

        private void showControllerInfo()
        {
            txbContSerial.Text = cont.SerialNo;
            txbContModelName.Text = cont.ModelName;
            txbContVersion.Text = cont.Version;

            txbCpuFirmVer.Text = cont.SoftwareVersion.CPU_Firm_Ver;

            txbDifFirmVer.Text = cont.SoftwareVersion.DIF_Firm_Ver;
            txbDifFpga1.Text = cont.SoftwareVersion.DIF_FPGA1_Ver;
            txbDifFpga2.Text = cont.SoftwareVersion.DIF_FPGA2_Ver;
            txbDifFpga3.Text = cont.SoftwareVersion.DIF_FPGA3_Ver;
            txbDifDp1Sp.Text = cont.SoftwareVersion.DIF_DP1_Splitter;
            txbDifDp2Sp.Text = cont.SoftwareVersion.DIF_DP2_Splitter;
            txbDifDp1Rec1.Text = cont.SoftwareVersion.DIF_DP1_Receiver1;
            txbDifDp1Rec2.Text = cont.SoftwareVersion.DIF_DP1_Receiver2;
            txbDifDp2Rec1.Text = cont.SoftwareVersion.DIF_DP2_Receiver1;
            txbDifDp2Rec2.Text = cont.SoftwareVersion.DIF_DP2_Receiver2;

            txbPifFpgaVer.Text = cont.SoftwareVersion.PIF_FPGA_Ver;

            //txbCpuBoardName.Text = cont.BoardInfo.CPU_Board_Name;
            //txbCpuPartsNum.Text = cont.BoardInfo.CPU_Parts_Number;
            //txbCpuMountA.Text = cont.BoardInfo.CPU_Mount_A;
            //txbCpuComplA.Text = cont.BoardInfo.CPU_Compl_A;

            //txbDifBoardName.Text = cont.BoardInfo.DIF_Board_Name;
            //txbDifPartsNum.Text = cont.BoardInfo.DIF_Parts_Number;
            //txbDifMountA.Text = cont.BoardInfo.DIF_Mount_A;
            //txbDifComplA.Text = cont.BoardInfo.DIF_Compl_A;

            //txbPifBoardName.Text = cont.BoardInfo.PIF_Board_Name;
            //txbPifPartsNum.Text = cont.BoardInfo.PIF_Parts_Number;
            //txbPifMountA.Text = cont.BoardInfo.PIF_Mount_A;
            //txbPifComplA.Text = cont.BoardInfo.PIF_Compl_A;
        }
    }
}
