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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CAS
{
    /// <summary>
    /// WindowSelectController.xaml の相互作用ロジック
    /// </summary>
    public partial class WindowSelectController : Window
    {
        public WindowSelectController()
        {
            InitializeComponent();
        }

        private void btnControllerIPOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void tbxControllerIP_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            { DialogResult = true; }
        }
    }
}
