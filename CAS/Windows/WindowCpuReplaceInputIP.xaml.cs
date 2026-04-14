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
    /// WindowCpuReplaceInputIP.xaml の相互作用ロジック
    /// </summary>
    public partial class WindowCpuReplaceInputIP : Window
    {
        public WindowCpuReplaceInputIP()
        {
            InitializeComponent();
        }

        private void txbSerial_KeyUp(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Enter)
            { btnOK_Click(sender, e); }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
