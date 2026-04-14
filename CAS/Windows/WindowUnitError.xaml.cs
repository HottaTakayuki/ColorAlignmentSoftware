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
    /// WindowUnitError.xaml の相互作用ロジック
    /// </summary>
    public partial class WindowUnitError : Window
    {
        UnitInfo unit;
        DispatcherTimer timer;

        public WindowUnitError(UnitInfo unitInfo)
        {
            InitializeComponent();

            unit = unitInfo;

            lbUnit.Content = "(" + unit.X + ", " + unit.Y + ") C" + unit.ControllerID + "-" + unit.PortNo + "-" + unit.UnitNo;

            timer = new DispatcherTimer(DispatcherPriority.Normal);
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            timer.Tick -= timer_Tick;
            timer.Stop();
            this.Close();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            showInfo();
        }

        private void showInfo()
        {
            lbxError.Items.Clear();

            if (string.IsNullOrEmpty(unit.Error) || string.IsNullOrEmpty(unit.Warning))
            { return; }

            string pn = unit.PortNo.ToString("X1") + unit.UnitNo.ToString("X1");

            // Error / Warning
            UInt64 errorCode;
            try { errorCode = Convert.ToUInt64(unit.Error, 16); }
            catch { errorCode = 0; }

            if (errorCode != 0)
            {
                if ((errorCode & 0x04) != 0)
                { lbxError.Items.Add("Power Error (100-" + pn + "-00)"); }

                if ((errorCode & 0x08) != 0)
                { lbxError.Items.Add("Temperature Error (111-" + pn + "-00)"); }

                if ((errorCode & 0x20) != 0)
                { lbxError.Items.Add("Temperature Error (113-" + pn + "-11)"); }

                if ((errorCode & 0x40) != 0)
                { lbxError.Items.Add("Temperature Error (113-" + pn + "-12)"); }

                if ((errorCode & 0x80) != 0)
                { lbxError.Items.Add("Temperature Error (113-" + pn + "-13)"); }

                if ((errorCode & 0x100) != 0)
                { lbxError.Items.Add("Temperature Error (113-" + pn + "-14)"); }

                if ((errorCode & 0x200) != 0)
                { lbxError.Items.Add("Temperature Error (113-" + pn + "-21)"); }

                if ((errorCode & 0x400) != 0)
                { lbxError.Items.Add("Temperature Error (113-" + pn + "-22)"); }

                if ((errorCode & 0x800) != 0)
                { lbxError.Items.Add("Temperature Error (113-" + pn + "-23)"); }
                
                if ((errorCode & 0x1000) != 0)
                { lbxError.Items.Add("Temperature Error (113-" + pn + "-24)"); }

                if ((errorCode & 0x2000) != 0)
                { lbxError.Items.Add("Temperature Error (113-" + pn + "-31)"); }

                if ((errorCode & 0x4000) != 0)
                { lbxError.Items.Add("Temperature Error (113-" + pn + "-32)"); }

                if ((errorCode & 0x8000) != 0)
                { lbxError.Items.Add("Temperature Error (113-" + pn + "-33)"); }

                if ((errorCode & 0x10000) != 0)
                { lbxError.Items.Add("Temperature Error (113-" + pn + "-34)"); }

                if ((errorCode & 0x4000000000) != 0)
                { lbxError.Items.Add("Update Error (140-" + pn + "-00)"); }

                if ((errorCode & 0x8000000000) != 0)
                { lbxError.Items.Add("Update Error (150-" + pn + "-00)"); }
            }

            UInt64 warningCode;
            try { warningCode = Convert.ToUInt64(unit.Warning, 16); }
            catch { warningCode = 0; }

            if (warningCode != 0)
            {
                if ((warningCode & 0x04) != 0)
                { lbxError.Items.Add("Temperature Warning (311-" + pn + "-00)"); }

                if ((warningCode & 0x10) != 0)
                { lbxError.Items.Add("Temperature Warning (313-" + pn + "-11)"); }

                if ((warningCode & 0x20) != 0)
                { lbxError.Items.Add("Temperature Warning (313-" + pn + "-12)"); }

                if ((warningCode & 0x40) != 0)
                { lbxError.Items.Add("Temperature Warning (313-" + pn + "-13)"); }

                if ((warningCode & 0x80) != 0)
                { lbxError.Items.Add("Temperature Warning (313-" + pn + "-14)"); }

                if ((warningCode & 0x100) != 0)
                { lbxError.Items.Add("Temperature Warning (313-" + pn + "-21)"); }

                if ((warningCode & 0x200) != 0)
                { lbxError.Items.Add("Temperature Warning (313-" + pn + "-22)"); }

                if ((warningCode & 0x400) != 0)
                { lbxError.Items.Add("Temperature Warning (313-" + pn + "-23)"); }

                if ((warningCode & 0x800) != 0)
                { lbxError.Items.Add("Temperature Warning (313-" + pn + "-24)"); }

                if ((warningCode & 0x1000) != 0)
                { lbxError.Items.Add("Temperature Warning (313-" + pn + "-31)"); }

                if ((warningCode & 0x2000) != 0)
                { lbxError.Items.Add("Temperature Warning (313-" + pn + "-32)"); }

                if ((warningCode & 0x4000) != 0)
                { lbxError.Items.Add("Temperature Warning (313-" + pn + "-33)"); }

                if ((warningCode & 0x8000) != 0)
                { lbxError.Items.Add("Temperature Warning (313-" + pn + "-34)"); }

                if ((warningCode & 0x2000000) != 0)
                { lbxError.Items.Add("Connection Warning (331-" + pn + "-00)"); }

                if ((warningCode & 0x4000000) != 0)
                { lbxError.Items.Add("Connection Warning (331-" + pn + "-10)"); }

                if ((warningCode & 0x8000000) != 0)
                { lbxError.Items.Add("Internal Warning (320-" + pn + "-00)"); }

                if ((warningCode & 0x10000000) != 0)
                { lbxError.Items.Add("Internal Warning (320-" + pn + "-10)"); }

                if ((warningCode & 0x20000000) != 0)
                { lbxError.Items.Add("Internal Warning (321-" + pn + "-00)"); }

                if ((warningCode & 0x40000000) != 0)
                { lbxError.Items.Add("Connection Warning (330-" + pn + "-00)"); }

                if ((warningCode & 0x80000000) != 0)
                { lbxError.Items.Add("Connection Warning (330-" + pn + "-11)"); }

                if ((warningCode & 0x100000000) != 0)
                { lbxError.Items.Add("Connection Warning (330-" + pn + "-12)"); }

                if ((warningCode & 0x200000000) != 0)
                { lbxError.Items.Add("Connection Warning (330-" + pn + "-13)"); }

                if ((warningCode & 0x400000000) != 0)
                { lbxError.Items.Add("Connection Warning (330-" + pn + "-14)"); }

                if ((warningCode & 0x800000000) != 0)
                { lbxError.Items.Add("Connection Warning (330-" + pn + "-21)"); }

                if ((warningCode & 0x1000000000) != 0)
                { lbxError.Items.Add("Connection Warning (330-" + pn + "-22)"); }

                if ((warningCode & 0x2000000000) != 0)
                { lbxError.Items.Add("Connection Warning (330-" + pn + "-23)"); }

                if ((warningCode & 0x4000000000) != 0)
                { lbxError.Items.Add("Connection Warning (330-" + pn + "-24)"); }

                if ((warningCode & 0x8000000000) != 0)
                { lbxError.Items.Add("Connection Warning (330-" + pn + "-31)"); }

                if ((warningCode & 0x10000000000) != 0)
                { lbxError.Items.Add("Connection Warning (330-" + pn + "-32)"); }

                if ((warningCode & 0x20000000000) != 0)
                { lbxError.Items.Add("Connection Warning (330-" + pn + "-33)"); }

                if ((warningCode & 0x40000000000) != 0)
                { lbxError.Items.Add("Connection Warning (330-" + pn + "-34)"); }
            }
        }
    }
}
