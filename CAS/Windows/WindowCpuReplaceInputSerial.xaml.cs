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

namespace CAS
{
    /// <summary>
    /// WindowInputSerial.xaml の相互作用ロジック
    /// </summary>
    public partial class WindowInputSerial : Window
    {
        public WindowInputSerial()
        {
            InitializeComponent();

            txbSerial.Focus();
        }

        private void txbSerial_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            { btnControllerIPOK_Click(sender, e); }
        }

        private void btnControllerIPOK_Click(object sender, RoutedEventArgs e)
        {
            string serial = txbSerial.Text;

            if(serial.Length > 7)
            {
                MainWindow.ShowMessageWindow("Input number is too long.\r\nSerial number must be 7 digits or less.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return;
            }

            try { int intSerial = Convert.ToInt32(serial); }
            catch
            {
                MainWindow.ShowMessageWindow("Invalid input.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return;
            }

            this.DialogResult = true;
        }
    }
}
