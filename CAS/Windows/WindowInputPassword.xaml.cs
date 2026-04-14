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
    /// WindowSelectControllerIP.xaml の相互作用ロジック
    /// </summary>
    public partial class WindowInputPassword : Window
    {
        public WindowInputPassword()
        {
            InitializeComponent();
        }

        private void btnInputPasswordOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void inputPassword_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            { DialogResult = true; }
        }
    }
}