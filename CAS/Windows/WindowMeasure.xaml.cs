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
    /// WindowMeasure.xaml の相互作用ロジック
    /// </summary>
    public partial class WindowMeasure : Window
    {
        public UserOperation operation;
        private bool enableRewind = false;

        public WindowMeasure()
        {
            InitializeComponent();

#if DEBUG
            this.WindowState = System.Windows.WindowState.Normal;
#endif
        }

        public bool? ShowDialog(bool EnableRewind)
        {
            string msg = "Please Set Probe on Cross Point.\r\nMeasure : Hit Any Keys or Click Anywhere\r\n(ESC key : Cancel)";

            enableRewind = EnableRewind;

            //if (enableRewind == true)
            //{ msg = "Please Set Probe on Cross Point.\r\nMeasure : Hit Any Keys or Click Anywhere\r\n(ESC key : Cancel)\r\n(Shift + \"R\" : Rewind Unit)"; }
            //else
            //{ msg = "Please Set Probe on Cross Point.\r\nMeasure : Hit Any Keys or Click Anywhere\r\n(ESC key : Cancel)"; }

            lbMessage.Content = msg;

            return ShowDialog();
        }

        //public bool? ShowDialog(string Message)
        //{
        //    lbMessage.Content = Message;
        //    return ShowDialog();
        //}

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            ModifierKeys modifierKeys = Keyboard.Modifiers;

            if (e.Key == Key.Escape)
            {
                operation = UserOperation.Cancel;
                this.DialogResult = false;
            }
            else if (enableRewind == true && (e.Key == Key.RightShift || e.Key == Key.LeftShift))
            { } // Shiftキーが押されたときは無視
            else if (enableRewind == true && e.Key == Key.R)
            {
                if(modifierKeys == System.Windows.Input.ModifierKeys.Shift)
                {
                    operation = UserOperation.Rewind;
                    this.DialogResult = false;
                }
                else
                {
                    operation = UserOperation.OK;
                    this.DialogResult = true;
                }
            }
            else
            {
                operation = UserOperation.OK;
                this.DialogResult = true;
            }
        }

        private void Window_LostFocus(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
