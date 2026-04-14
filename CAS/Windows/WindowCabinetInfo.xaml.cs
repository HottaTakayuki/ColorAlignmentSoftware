using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CAS
{
    /// <summary>
    /// WindowCabinetDataSetting.xaml の相互作用ロジック
    /// </summary>
    public partial class WindowCabinetInfo : Window
    {
        private static readonly Regex _modelNameRegex = new Regex("^[a-zA-Z0-9/-]$");
        private static readonly Regex _serialNoRegex = new Regex("^[0-9]$");
        public WindowCabinetInfo()
        {
            InitializeComponent();
        }

        private void btnCabinetDataSettingOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        /// <summary>
        /// 製品名入力文字が、英数字か/以外の場合はキャンセルする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InputModelName_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !_modelNameRegex.IsMatch(e.Text);
        }

        /// <summary>
        /// SerialNoが数字以外の場合はキャンセルする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InputSerialNo_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !_serialNoRegex.IsMatch(e.Text);
        }
    }
}
