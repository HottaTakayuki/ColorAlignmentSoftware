using System;
using System.Collections.Generic;
using System.IO;
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

namespace CAS.Windows
{
    /// <summary>
    /// WindowEULA.xaml の相互作用ロジック
    /// </summary>
    public partial class WindowEULA : Window
    {
        public WindowEULA()
        {
            InitializeComponent();

            rbEulaNo.IsChecked = true;
            btnEulaOK.IsEnabled = false;

            loadEulaRtf();
        }

        private void rbEulaYes_Checked(object sender, RoutedEventArgs e)
        {
            btnEulaOK.IsEnabled = true;
        }

        private void rbEulaNo_Checked(object sender, RoutedEventArgs e)
        {
            btnEulaOK.IsEnabled = false;
        }

        private void btnEulaOK_Click(object sender, RoutedEventArgs e)
        {
            Settings.Ins.eulaAgree = true;
            DialogResult = true;
        }

        private void btnEulaCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void loadEulaRtf()
        {
            TextRange range;
            FileStream fStream;

            range = new TextRange(rtbEulaText.Document.ContentStart, rtbEulaText.Document.ContentEnd);
            fStream = new FileStream(Settings.Ins.Eulafile, FileMode.Open, FileAccess.Read);
            range.Load(fStream, DataFormats.Rtf);
            fStream.Close();
        }
    }
}
