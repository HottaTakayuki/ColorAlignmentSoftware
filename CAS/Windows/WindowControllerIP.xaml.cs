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
    /// WindowControllerIP.xaml の相互作用ロジック
    /// </summary>
    public partial class WindowControllerIP : Window
    {
        public WindowControllerIP()
        {
            InitializeComponent();
        }

        public bool? ShowDialog(int Num)
        {
            txbMesssage.Text = "Please input IP address. ( Controller_" + Num + " )";
            return this.ShowDialog();
        }

        private void btnControllerIPOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void txbIP_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            { DialogResult = true; }
        }
    }
}
