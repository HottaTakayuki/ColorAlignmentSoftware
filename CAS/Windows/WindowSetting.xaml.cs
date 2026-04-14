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
    /// WindowSetting.xaml の相互作用ロジック
    /// </summary>
    public partial class WindowSetting : Window
    {
        public WindowSetting()
        {
            InitializeComponent();

            // Channel
            try { cmbxChannel.Items.Clear(); }
            catch { }

            for (Int32 i = 0; i < 100; i++)
            { cmbxChannel.Items.Add("CH" + i.ToString("D2")); }

            //cmbxChannel.SelectedIndex = Settings.Ins.Channel;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            //Settings.Ins.Channel = cmbxChannel.SelectedIndex;
            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
