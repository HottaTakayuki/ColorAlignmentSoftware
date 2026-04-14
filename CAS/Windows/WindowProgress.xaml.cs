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
using System.Timers;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace CAS
{
    /// <summary>
    /// WindowProgress.xaml の相互作用ロジック
    /// </summary>
    public partial class WindowProgress : Window
    {
        [DllImport("User32.dll")]
        private static extern uint GetClassLong(IntPtr hwnd, int nIndex);

        [DllImport("User32.dll")]
        private static extern uint SetClassLong(IntPtr hwnd, int nIndex, uint dwNewLong);

        private const int GCL_STYLE = -26;
        private const uint CS_NOCLOSE = 0x0200;

        Timer elapsedTimer;
        Timer remainTimer;
        DateTime startTime;
        private int m_ProcessSec;

        // added by Hotta 2024/12/25
        public enum TAbortType { None, Adjustment, Measurement };
        TAbortType abortType = TAbortType.None;
        public TAbortType AbortType
        {
            set
            {
                abortType = value;
                operation = UserOperation.None;
            }
            get { return abortType; }
        }

        UserOperation operation = UserOperation.None;
        public UserOperation Operation
        {
            get
            {
                if (abortType == TAbortType.Adjustment || abortType == TAbortType.Measurement)
                    return operation;
                else
                    return UserOperation.None;
            }
            set { operation = value; }
        }
        //


        // modified by Hotta 2024/12/25
        /*
        public WindowProgress(string Caption = "Progress", int Height = 170, int Width = 300)
        */
        public WindowProgress(string Caption = "Progress", int Height = 180, int Width = 300, TAbortType keyAbortType = TAbortType.None)
        {
            InitializeComponent();

            this.Title = Caption;

            this.Height = Height;
            this.Width = Width;

            elapsedTimer = new Timer();
            elapsedTimer.Elapsed += new ElapsedEventHandler(OnElapsed_TimersTimer);
            elapsedTimer.Interval = 100;

            remainTimer = new Timer();
            remainTimer.Elapsed += new ElapsedEventHandler(OnRemain_TimersTimer);
            remainTimer.Interval = 100;

            // added by Hotta 2024/12/25
            if (keyAbortType == TAbortType.Adjustment || keyAbortType == TAbortType.Measurement)
            {
                this.KeyDown += new KeyEventHandler(Window_KeyDown);
                this.abortType = keyAbortType;
            }
            else
                this.abortType = TAbortType.None;

            operation = UserOperation.None;

            if (Height != 170)
                lbMessage.Height = lbMessage.Height * (double)Height / 170;

            //
        }

        public void ShowMessage(string Message)
        {
            // added by Hotta 2025/01/09
            if (abortType == TAbortType.Adjustment)
                Message += "\r\n*ESC key pressed, You can abort the adjustment process.\r\nIt may take some time before the process aborted.";
            else if(abortType == TAbortType.Measurement)
                Message += "\r\n*ESC key pressed, You can abort the measurement process.\r\nIt may take some time before the process aborted.";
            //

            Dispatcher.Invoke(new Action(() => { this.lbMessage.Content = Message; }));
        }

        public void SetWholeSteps(int StepCount)
        {
            Dispatcher.Invoke(new Action(() => { this.pbProgressWhole.Maximum = StepCount; }));
            Dispatcher.Invoke(new Action(() => { this.pbProgressWhole.Value = 0; }));
        }

        public void PutForward1Step()
        {
            Dispatcher.Invoke(new Action(() => { this.pbProgressWhole.Value += 1; }));
        }

        public void PutBackward1Step()
        {
            Dispatcher.Invoke(new Action(() => { this.pbProgressWhole.Value -= 1; }));
        }

        public void SetWholeProgress(int step)
        {
            Dispatcher.Invoke(new Action(() => { this.pbProgressWhole.Value = step; }));
        }

        public void GetWholeProgress(out int step)
        {
            double temp = 0;

            Dispatcher.Invoke(new Action(() => { temp = this.pbProgressWhole.Value; }));

            step = (int)temp;
        }

        public void ElapsedTimer(bool enable)
        {
            if (enable == true)
            {
                startTime = DateTime.Now;
                elapsedTimer.Start();
                Dispatcher.Invoke(new Action(() => { lbElapsedTime.Visibility = System.Windows.Visibility.Visible; }));
            }
            else
            {
                elapsedTimer.Stop();
                Dispatcher.Invoke(new Action(() => { lbElapsedTime.Visibility = System.Windows.Visibility.Collapsed; }));
            }
        }

        private void OnElapsed_TimersTimer(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                TimeSpan span = DateTime.Now - startTime;
                lbElapsedTime.Content = "( Elapsed Time  " + span.Hours.ToString("D2") + " : " + span.Minutes.ToString("D2") + " : " + span.Seconds.ToString("D2") + " )";
            }));
        }

        // added by Hotta 2024/12/25
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (abortType == TAbortType.None)
            {
                operation = UserOperation.None;
            }
            else
            {
                // added by Hotta 2025/01/31
                if (operation == UserOperation.Cancel)
                    return;
                //
                if (e.Key == Key.Escape)
                    operation = UserOperation.Cancel;
                else
                    operation = UserOperation.None;
            }
        }
        //

        public void StartRemainTimer(int sec)
        {
            if (Settings.Ins.ExecLog == true)
            { MainWindow.SaveExecLog("StartRemainTimer"); }

            startTime = DateTime.Now;
            m_ProcessSec = sec;
            remainTimer.Start();
            Dispatcher.Invoke(new Action(() => { lbRemainTime.Visibility = System.Windows.Visibility.Visible; }));
        }

        public void PauseRemainTimer()
        {
            TimeSpan ts = DateTime.Now - startTime;
            
            if (Settings.Ins.ExecLog == true)
            {
                MainWindow.SaveExecLog($"StartTime: {startTime}");
                MainWindow.SaveExecLog($"TimeSpan: {ts}");
                MainWindow.SaveExecLog($"Paused ProcessSec: {m_ProcessSec}");
            }

            int _elapsedSec = ts.Seconds + (ts.Minutes * 60) + (ts.Hours * 3600); // _elapsedSec: 処理Step開始から一時停止された時までの時間
            m_ProcessSec -= _elapsedSec;

            if (Settings.Ins.ExecLog == true)
            {
                MainWindow.SaveExecLog($"Resume ProcessSec: {m_ProcessSec}");
            }

            remainTimer.Stop();
        }

        public void ResumeRemainTimer()
        {
            startTime = DateTime.Now;
            remainTimer.Start();
        }

        public void StopRemainTimer()
        {
            if (Settings.Ins.ExecLog == true)
            { MainWindow.SaveExecLog("StopRemainTimer"); }

            remainTimer.Stop();
            Dispatcher.Invoke(new Action(() => { lbRemainTime.Visibility = System.Windows.Visibility.Collapsed; }));
        }

        public void SetRemainTimer(int sec)
        {
            if (Settings.Ins.ExecLog == true)
            { MainWindow.SaveExecLog("SetRemainTimer"); }

            startTime = DateTime.Now;
            m_ProcessSec = sec;
        }

        private void OnRemain_TimersTimer(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                TimeSpan span = DateTime.Now - startTime;
                int remainSec = m_ProcessSec - span.Seconds - span.Minutes * 60 - span.Hours * 3600;

                if (remainSec < 1)
                {
                    if (Settings.Ins.ExecLog == true)
                    { MainWindow.SaveExecLog("Time remaining is 0"); }

                    remainSec = 1;
                }

                if (remainSec < 60)
                {
                    lbRemainTime.Content = "Remaining Time (Estimated): " + remainSec.ToString("D2") + " s ";
                }
                else
                {
                    if (remainSec < 3600)
                    {
                        lbRemainTime.Content = "Remaining Time (Estimated): " + ((remainSec + 60 - 1) / 60).ToString("D2") + " m ";
                    }
                    else
                    {
                        if (remainSec % 10 == 0)
                        {
                            lbRemainTime.Content = "Remaining Time (Estimated): " + (remainSec / 3600).ToString("D2") + " h " + ((remainSec % 3600 + 60 - 1) / 60).ToString("D2") + " m ";
                        }
                    }
                }
            }));
        }


        private void WindowProgress_Load(object sender, RoutedEventArgs e)
        {
            //「X」閉じるメニュー無効化
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                uint style = GetClassLong(hwnd, GCL_STYLE);
                SetClassLong(hwnd, GCL_STYLE, style | CS_NOCLOSE);
            }
        }
    }
}
