using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace CAS
{
    /// <summary>
    /// WindowComplete.xaml の相互作用ロジック
    /// </summary>
    public partial class WindowMessage : Window
    {
        public WindowMessage(string Message, string Caption, string ButtonContent = "OK")
            : this(Message, Caption, SystemIcons.Information, ButtonContent)
        {
        }

        public WindowMessage(string Message, string Caption, Icon Icon, string ButtonContent = "OK")
        {
            InitializeComponent();

            if (Settings.Ins.ExecLog == true)
            { 
                MainWindow.SaveExecLog("WindowMessage : " + Message.Replace("\r", "").Replace("\n", "") + " Caption : " + Caption);
            }

            //システムのアイコンを表示する
            imIcon.Source = Imaging.CreateBitmapSourceFromHIcon(Icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            Icon.Dispose();

            this.Title = Caption;

            tbMessage.Text = Message;

            btnOK.Content = ButtonContent;

            //ChangeWindowSize();
        }

        public void ChangeWindowSize(int WindowWidth = 300, int WindowHeight = 180)
        {
            this.Width = WindowWidth;
            this.Height = WindowHeight;

            //this.Width = tbMessage.Width + tbMessage.Margin.Left + 15;
            //this.Height = tbMessage.Height + tbMessage.Margin.Top + 123;
        }

        public void AddButton()
        {
            btn2.Visibility = System.Windows.Visibility.Visible;

            Thickness margin = new Thickness();
            margin.Left = this.Width / 2 - btnOK.Width - 10;
            margin.Right = btnOK.Margin.Right;
            margin.Top = btnOK.Margin.Top;
            margin.Bottom = btnOK.Margin.Bottom;

            btnOK.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            btnOK.Margin = margin;

            margin.Left += btn2.Width + 10;

            btn2.Margin = margin;
        }

        public void AddButton(string Content)
        {
            AddButton();
            btn2.Content = Content;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            //this.Close();
        }

        private void btn2_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            //this.Close();
        }
    }
}
