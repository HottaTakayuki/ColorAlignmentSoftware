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
    /// WindowVersion.xaml の相互作用ロジック
    /// </summary>
    public partial class WindowVersion : Window
    {
        public WindowVersion(string version, string copyright)
        {
            InitializeComponent();

            txbVersion.Content = version;
            lbCopyright.Content = copyright;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
