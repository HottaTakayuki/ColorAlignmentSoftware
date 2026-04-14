using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.IO;

namespace CAS
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        [STAThread]
        public static void Main()
        {
            try { CheckOpenCvSharpDll(); }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "CAS Error!!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            System.Windows.Media.FontFamily font = new System.Windows.Media.FontFamily("Yu Gothic UI, Gothic UI");
            Style style = new Style(typeof(Window));
            style.Setters.Add(new Setter(System.Windows.Controls.Control.FontFamilyProperty, font));
            style.Setters.Add(new Setter(System.Windows.Controls.Control.FontSizeProperty, 12.0));

            FrameworkElement.StyleProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata(style));

            App app = new App();
            app.InitializeComponent();
            app.Run();
        }

        private static void CheckOpenCvSharpDll()
        {
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();

            // Applicationのパスを取得する
            string applicationPath = System.IO.Path.GetDirectoryName(asm.Location);

            string OpenCvInstDir = @"C:\CAS_OSS\";
            string dllPath = applicationPath + "\\Components\\";

            // 全てのファイルがあるときはチェックしない
            if (File.Exists(dllPath + @"dll\x86\OpenCvSharpExtern.dll") == true &&
               File.Exists(dllPath + "OpenCvSharp.dll") == true &&
               File.Exists(dllPath + "OpenCvSharp.Blob.dll") == true)
            { return; }

            // OpenCvSharpがインストール済みか確認
            if (File.Exists(OpenCvInstDir + @"OpenCvSharpExtern.dll") == false ||
                File.Exists(OpenCvInstDir + @"OpenCvSharp.dll") == false ||
                File.Exists(OpenCvInstDir + @"OpenCvSharp.Blob.dll") == false)
            { throw new Exception("This Software requires the OpenCVSharp.\r\nPlease install the OpenCVSharp using included \"Download_OpenCVSharp\",  and try again."); }

            if (File.Exists(dllPath + @"dll\x86\OpenCvSharpExtern.dll") == false)
            {
                Directory.CreateDirectory(dllPath + "dll\\x86\\");
                File.Copy(OpenCvInstDir + @"OpenCvSharpExtern.dll", dllPath + @"dll\x86\OpenCvSharpExtern.dll");
            }

            if (File.Exists(dllPath + "OpenCvSharp.dll") == false)
            { File.Copy(OpenCvInstDir + @"OpenCvSharp.dll", dllPath + "OpenCvSharp.dll"); }

            if (File.Exists(dllPath + "OpenCvSharp.Blob.dll") == false)
            { File.Copy(OpenCvInstDir + @"OpenCvSharp.Blob.dll", dllPath + "OpenCvSharp.Blob.dll"); }
        }
    }
}
