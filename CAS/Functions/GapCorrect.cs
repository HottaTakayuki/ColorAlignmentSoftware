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
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

using SONY.Modules;
using MakeUFData;

namespace CAS
{
    // Gap Correct
    public partial class MainWindow : Window
    {
        private bool gapCorrectTabFirstSelect = true;

        #region Events

        #region Mode

        //private void btnGapCorrectOn_Click(object sender, RoutedEventArgs e)
        //{
        //    string controller = cmbxControllerControl.SelectedItem.ToString();

        //    actionButton(sender, "Gap Correct On");

        //    if (controller == "All")
        //    {
        //        foreach (ControllerInfo cont in dicController.Values)
        //        { sendSdcpCommand(SDCPClass.CmdGapCorrectSetOn, cont.IPAddress); }
        //    }
        //    else
        //    { sendSdcpCommand(SDCPClass.CmdGapCorrectSetOn); }

        //    releaseButton(sender);
        //}

        //private void btnGapCorrectOff_Click(object sender, RoutedEventArgs e)
        //{
        //    string controller = cmbxControllerControl.SelectedItem.ToString();

        //    actionButton(sender, "Gap Correct Off");

        //    if (controller == "All")
        //    {
        //        foreach (ControllerInfo cont in dicController.Values)
        //        { sendSdcpCommand(SDCPClass.CmdGapCorrectSetOff, cont.IPAddress); }
        //    }
        //    else
        //    { sendSdcpCommand(SDCPClass.CmdGapCorrectSetOff); }

        //    releaseButton(sender);
        //}

        private void rbGapCorrectOn_Checked(object sender, RoutedEventArgs e)
        {
            gdGapCorrectMode.IsEnabled = false;
            actionButton(sender, "Gap Correct On");

            setGapCorrection(true);

            // Gap Correct(Module)画面のModeラジオボタンを同期化
            rbGapCorrectModuleOn.Checked -= rbGapCorrectModuleOn_Checked;
            rbGapCorrectModuleOn.IsChecked = true;
            rbGapCorrectModuleOn.Checked += rbGapCorrectModuleOn_Checked;

            releaseButton(sender);
            gdGapCorrectMode.IsEnabled = true;
        }

        private void rbGapCorrectOff_Checked(object sender, RoutedEventArgs e)
        {
            gdGapCorrectMode.IsEnabled = false;
            actionButton(sender, "Gap Correct Off");

            setGapCorrection(false);

            // Gap Correct(Module)画面のModeラジオボタンを同期化
            rbGapCorrectModuleOff.Checked -= rbGapCorrectModuleOff_Checked;
            rbGapCorrectModuleOff.IsChecked = true;
            rbGapCorrectModuleOff.Checked += rbGapCorrectModuleOff_Checked;

            releaseButton(sender); 
            gdGapCorrectMode.IsEnabled = true;
        }

        #endregion Mode

        #region Signal

        //private void btnPlane_Click(object sender, RoutedEventArgs e)
        //{
        //    actionButton(sender, "Set Plane Signal");
        //    outputGapPlane();
        //    System.Threading.Thread.Sleep(500);
        //    releaseButton(sender);
        //}

        //private void slPlaneLevel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        //{
        //    try
        //    {
        //        int level = (int)slPlaneLevel.Value;
        //        txbPlaneLevel.Text = level.ToString();
        //    }
        //    catch { }
        //}

        //private void txbPlaneLevel_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    try { slPlaneLevel.Value = Convert.ToInt32(txbPlaneLevel.Text); }
        //    catch { }
        //}

        //private void txbPlaneLevel_KeyDown(object sender, KeyEventArgs e)
        //{
        //    if (e.Key == Key.Enter)
        //    {
        //        actionButton(sender, "Set Plane Signal");
        //        outputGapPlane();
        //        System.Threading.Thread.Sleep(500);
        //        releaseButton(sender);
        //    }
        //}
        
        private void tbtnCursor_Checked(object sender, RoutedEventArgs e)
        {            
            actionButton(sender, "Set Cursor");
            outputGapCursor();
            System.Threading.Thread.Sleep(500);
            releaseButton(sender);

            gdGapCorrectCursor.IsEnabled = true;
            tbtnCorrect.IsChecked = false;

            // cmbx初期化
            cmbxGapTopLeft.SelectionChanged -= cmbxGapTopLeft_SelectionChanged;
            cmbxGapTopLeft.SelectedIndex = -1;
            cmbxGapTopLeft.SelectionChanged += cmbxGapTopLeft_SelectionChanged;

            cmbxGapTopRight.SelectionChanged -= cmbxGapTopRight_SelectionChanged;
            cmbxGapTopRight.SelectedIndex = -1;
            cmbxGapTopRight.SelectionChanged += cmbxGapTopRight_SelectionChanged;

            cmbxGapRightTop.SelectionChanged -= cmbxGapRightTop_SelectionChanged;
            cmbxGapRightTop.SelectedIndex = -1;
            cmbxGapRightTop.SelectionChanged += cmbxGapRightTop_SelectionChanged;

            cmbxGapRightBottom.SelectionChanged -= cmbxGapRightBottom_SelectionChanged;
            cmbxGapRightBottom.SelectedIndex = -1;
            cmbxGapRightBottom.SelectionChanged += cmbxGapRightBottom_SelectionChanged;
        }

        private void tbtnCursor_Unchecked(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Set Cursor");
            gdGapCorrectCursor.IsEnabled = false;
            outputGapPlane();
            System.Threading.Thread.Sleep(500);
            releaseButton(sender);
        }

        private void rbCursor_Checked(object sender, RoutedEventArgs e)
        {
            if (tbtnCursor.IsChecked == true)
            {
                actionButton(sender, "Change Cursor Axis", "ListeningEarcon.wav");
                outputGapCursor();
                System.Threading.Thread.Sleep(300);
                releaseButton(sender);
            }
        }

        private void txbCursorX_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                actionButton(sender, "Set Cursor");
                outputGapCursor();
                System.Threading.Thread.Sleep(500);
                releaseButton(sender);
            }
        }

        private void txbCursorY_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                actionButton(sender, "Set Cursor");
                outputGapCursor();
                System.Threading.Thread.Sleep(500);
                releaseButton(sender);
            }
        }

        private void btnCursorUp_Click(object sender, RoutedEventArgs e)
        {
            int y;

            actionButton(sender, "Set Cursor");

            try { y = Convert.ToInt32(txbCursorY.Text); }
            catch
            {
                releaseButton(sender);
                return;
            }

            if (y <= 1)
            {
                releaseButton(sender);
                return;
            }

            txbCursorY.Text = (--y).ToString();

            if (tbtnCursor.IsChecked == true)
            { outputGapCursor(); }
            
            System.Threading.Thread.Sleep(500);
            releaseButton(sender);
        }

        private void btnCursorDown_Click(object sender, RoutedEventArgs e)
        {
            int y;

            actionButton(sender, "Set Cursor");

            try { y = Convert.ToInt32(txbCursorY.Text); }
            catch
            {
                releaseButton(sender);
                return;
            }

            if (y >= allocInfo.MaxY)
            {
                releaseButton(sender); 
                return;
            }

            txbCursorY.Text = (++y).ToString();

            if (tbtnCursor.IsChecked == true)
            { outputGapCursor(); }

            System.Threading.Thread.Sleep(500);
            releaseButton(sender);
        }

        private void btnCursorLeft_Click(object sender, RoutedEventArgs e)
        {
            int x;

            actionButton(sender, "Set Cursor");

            try { x = Convert.ToInt32(txbCursorX.Text); }
            catch
            {
                releaseButton(sender);
                return;
            }

            if (x <= 1)
            {
                releaseButton(sender);
                return;
            }

            txbCursorX.Text = (--x).ToString();

            if (tbtnCursor.IsChecked == true)
            { outputGapCursor(); }

            System.Threading.Thread.Sleep(500);
            releaseButton(sender);
        }

        private void btnCursorRight_Click(object sender, RoutedEventArgs e)
        {
            int x;

            actionButton(sender, "Set Cursor");

            try { x = Convert.ToInt32(txbCursorX.Text); }
            catch
            {
                releaseButton(sender); 
                return;
            }

            if (x >= allocInfo.MaxX)
            {
                releaseButton(sender); 
                return;
            }

            txbCursorX.Text = (++x).ToString();

            if (tbtnCursor.IsChecked == true)
            { outputGapCursor(); }

            System.Threading.Thread.Sleep(500);
            releaseButton(sender);
        }
        
        private void cmbxGapCorrectLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cmbxGapCorrectPattern_DropDownClosed(sender, null);
        }

        private void cmbxGapCorrectPattern_DropDownClosed(object sender, EventArgs e)
        {
            if (cmbxGapCorrectPattern != null)
            {
                if (cmbxGapCorrectPattern.SelectedIndex == 0)
                {
                    actionButton(sender, "Set Plane Signal");
                    outputGapPlane();
                    System.Threading.Thread.Sleep(500);
                    releaseButton(sender);
                }
                else if (cmbxGapCorrectPattern.SelectedIndex == 1)
                {
                    actionButton(sender, "Internal Signal Off");
                    foreach (ControllerInfo cont in dicController.Values)
                    { sendSdcpCommand(SDCPClass.CmdIntSignalOff, 0, cont.IPAddress); }
                    System.Threading.Thread.Sleep(500);
                    releaseButton(sender);
                }
            }
        }
        
        // Reset
        private async void btnResetExecute_Click(object sender, RoutedEventArgs e)
        {
            bool status = false;

            WindowMessage winMessage = new WindowMessage("Correction data will be lost.\r\nAre you sure ?", "Confirm");
            bool? result = winMessage.ShowDialog();

            if (result == true)
            {
                winProgress = new WindowProgress("Reset Gap Correct Value");                
                winProgress.Show();

                try { status = await Task.Run(() => resetGapCorrect()); }
                catch (Exception ex)
                { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

                string msg = "";
                string caption = "";
                if (status == true)
                {
                    msg = "Reset Complete!";
                    caption = "Complete";
                }
                else
                {
                    msg = "Failed in Reset.";
                    caption = "Error";
                }

                winProgress.Close();

                playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
                winMessage = new WindowMessage(msg, caption);
                winMessage.ShowDialog();
            }
        }

        #endregion Signal

        #region Correct

        private void tbtnCorrect_Checked(object sender, RoutedEventArgs e)
        {
            CorrectionValue targetValue;
            
            actionButton(sender, "Get Correct Value");

            //gdGapCorrectSignal.IsEnabled = false;

            gdGapCorrectValue.IsEnabled = true;

            if (rbCursorX.IsChecked == true)
            {
                cmbxGapTopLeft.IsEnabled = true;
                btnGapTopLeftUp.IsEnabled = true;
                btnGapTopLeftDown.IsEnabled = true;

                cmbxGapTopRight.IsEnabled = true;
                btnGapTopRightUp.IsEnabled = true;
                btnGapTopRightDown.IsEnabled = true;

                cmbxGapRightTop.IsEnabled = false;
                btnGapRightTopUp.IsEnabled = false;
                btnGapRightTopDown.IsEnabled = false;

                cmbxGapRightBottom.IsEnabled = false;
                btnGapRightBottomUp.IsEnabled = false;
                btnGapRightBottomDown.IsEnabled = false;
            }
            else
            {
                cmbxGapTopLeft.IsEnabled = false;
                btnGapTopLeftUp.IsEnabled = false;
                btnGapTopLeftDown.IsEnabled = false;

                cmbxGapTopRight.IsEnabled = false;
                btnGapTopRightUp.IsEnabled = false;
                btnGapTopRightDown.IsEnabled = false;

                cmbxGapRightTop.IsEnabled = true;
                btnGapRightTopUp.IsEnabled = true;
                btnGapRightTopDown.IsEnabled = true;

                cmbxGapRightBottom.IsEnabled = true;
                btnGapRightBottomUp.IsEnabled = true;
                btnGapRightBottomDown.IsEnabled = true;
            }

            // 内部信号をPlaneに変更
            outputGapPlane();

            // 目地補正ON
            //foreach (ControllerInfo cont in dicController.Values)
            //{ sendSdcpCommand(SDCPClass.CmdGapCorrectSetOn, cont.IPAddress); }
            setGapCorrection(true);

            // RadioBox変更
            rbGapCorrectOn.Checked -= rbGapCorrectOn_Checked;
            rbGapCorrectOn.IsChecked = true;
            rbGapCorrectOn.Checked += rbGapCorrectOn_Checked;

            // 現在の補正値を取得
            getGapCorrectValue(out targetValue);//, out nextValue);

            if (rbCursorX.IsChecked == true)
            {
                cmbxGapTopLeft.SelectionChanged -= cmbxGapTopLeft_SelectionChanged;
                cmbxGapTopLeft.SelectedIndex = 24 - targetValue.Value1 / 10;
                cmbxGapTopLeft.SelectionChanged += cmbxGapTopLeft_SelectionChanged;

                cmbxGapTopRight.SelectionChanged -= cmbxGapTopRight_SelectionChanged;
                cmbxGapTopRight.SelectedIndex = 24 - targetValue.Value2 / 10;
                cmbxGapTopRight.SelectionChanged += cmbxGapTopRight_SelectionChanged;
            }
            else
            {
                cmbxGapRightTop.SelectionChanged -= cmbxGapRightTop_SelectionChanged;
                cmbxGapRightTop.SelectedIndex = 24 - targetValue.Value1 / 10;
                cmbxGapRightTop.SelectionChanged += cmbxGapRightTop_SelectionChanged;

                cmbxGapRightBottom.SelectionChanged -= cmbxGapRightBottom_SelectionChanged;
                cmbxGapRightBottom.SelectedIndex = 24 - targetValue.Value2 / 10;
                cmbxGapRightBottom.SelectionChanged += cmbxGapRightBottom_SelectionChanged;
            }

            releaseButton(sender);
        }

        private void tbtnCorrect_Unchecked(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Get Correct Value");

            //gdGapCorrectSignal.IsEnabled = true;

            gdGapCorrectValue.IsEnabled = false;

            //try { txbGapTopLeft.TextChanged -= txbGapTopLeft_TextChanged; }
            //catch { }
            //try { txbGapTopRight.TextChanged -= txbGapTopRight_TextChanged; }
            //catch { }
            //try { txbGapRightTop.TextChanged -= txbGapRightTop_TextChanged; }
            //catch { }
            //try { txbGapRightBottom.TextChanged -= txbGapRightBottom_TextChanged; }
            //catch { }
            
            System.Threading.Thread.Sleep(500);
            releaseButton(sender);
        }

        // 非表示のボタンのイベント
        private void btnGetCorrectValue_Click(object sender, RoutedEventArgs e)
        {
            CorrectionValue targetValue;//, nextValue;

            getGapCorrectValue(out targetValue);//, out nextValue);
        }
                
        private void cmbxGapTopLeft_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            actionButton(sender, "Set Correct Value");
            setGapCorrectValue(CorrectPosition.TopLeft, (24 - cmbxGapTopLeft.SelectedIndex) * 10 + 8);
            releaseButton(sender);
        }

        private void cmbxGapTopRight_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            actionButton(sender, "Set Correct Value");
            setGapCorrectValue(CorrectPosition.TopRight, (24 - cmbxGapTopRight.SelectedIndex) * 10 + 8);
            releaseButton(sender);
        }

        private void cmbxGapRightTop_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            actionButton(sender, "Set Correct Value");
            setGapCorrectValue(CorrectPosition.RightTop, (24 - cmbxGapRightTop.SelectedIndex) * 10 + 8);
            releaseButton(sender);
        }

        private void cmbxGapRightBottom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            actionButton(sender, "Set Correct Value");
            setGapCorrectValue(CorrectPosition.RightBottom, (24 - cmbxGapRightBottom.SelectedIndex) * 10 + 8);
            releaseButton(sender);
        }
        
        #region Value Up & Down

        private void btnGapTopLeftUp_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapTopLeft.SelectedIndex;

            if (index <= 0)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapTopLeft.SelectionChanged -= cmbxGapTopLeft_SelectionChanged;
            cmbxGapTopLeft.SelectedIndex = index - 1;
            cmbxGapTopLeft.SelectionChanged += cmbxGapTopLeft_SelectionChanged;

            setGapCorrectValue(CorrectPosition.TopLeft, (24 - cmbxGapTopLeft.SelectedIndex) * 10 + 8);
            
            releaseButton(sender);
        }

        private void btnGapTopLeftDown_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapTopLeft.SelectedIndex;
            
            if (index >= 24)
            { return; }
            
            actionButton(sender, "Set Correct Value");

            cmbxGapTopLeft.SelectionChanged -= cmbxGapTopLeft_SelectionChanged;
            cmbxGapTopLeft.SelectedIndex = index + 1;
            cmbxGapTopLeft.SelectionChanged += cmbxGapTopLeft_SelectionChanged;

            setGapCorrectValue(CorrectPosition.TopLeft, (24 - cmbxGapTopLeft.SelectedIndex) * 10 + 8);
            
            releaseButton(sender);
        }

        private void btnGapTopRightUp_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapTopRight.SelectedIndex;

            if (index <= 0)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapTopRight.SelectionChanged -= cmbxGapTopRight_SelectionChanged;
            cmbxGapTopRight.SelectedIndex = index - 1;
            cmbxGapTopRight.SelectionChanged += cmbxGapTopRight_SelectionChanged;

            setGapCorrectValue(CorrectPosition.TopRight, (24 - cmbxGapTopRight.SelectedIndex) * 10 + 8);

            releaseButton(sender);
        }

        private void btnGapTopRightDown_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapTopRight.SelectedIndex;

            if (index >= 24)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapTopRight.SelectionChanged -= cmbxGapTopRight_SelectionChanged;
            cmbxGapTopRight.SelectedIndex = index + 1;
            cmbxGapTopRight.SelectionChanged += cmbxGapTopRight_SelectionChanged;
            
            setGapCorrectValue(CorrectPosition.TopRight, (24 - cmbxGapTopRight.SelectedIndex) * 10 + 8);

            releaseButton(sender);
        }

        private void btnGapRightTopUp_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapRightTop.SelectedIndex;

            if (index <= 0)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapRightTop.SelectionChanged -= cmbxGapRightTop_SelectionChanged;
            cmbxGapRightTop.SelectedIndex = index - 1;
            cmbxGapRightTop.SelectionChanged += cmbxGapRightTop_SelectionChanged;
            
            setGapCorrectValue(CorrectPosition.RightTop, (24 - cmbxGapRightTop.SelectedIndex) * 10 + 8);

            releaseButton(sender);
        }

        private void btnGapRightTopDown_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapRightTop.SelectedIndex;

            if (index >= 24)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapRightTop.SelectionChanged -= cmbxGapRightTop_SelectionChanged;
            cmbxGapRightTop.SelectedIndex = index + 1;
            cmbxGapRightTop.SelectionChanged += cmbxGapRightTop_SelectionChanged;

            setGapCorrectValue(CorrectPosition.RightTop, (24 - cmbxGapRightTop.SelectedIndex) * 10 + 8);

            releaseButton(sender);
        }

        private void btnGapRightBottomUp_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapRightBottom.SelectedIndex;

            if (index <= 0)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapRightBottom.SelectionChanged -= cmbxGapRightBottom_SelectionChanged;
            cmbxGapRightBottom.SelectedIndex = index - 1;
            cmbxGapRightBottom.SelectionChanged += cmbxGapRightBottom_SelectionChanged;

            setGapCorrectValue(CorrectPosition.RightBottom, (24 - cmbxGapRightBottom.SelectedIndex) * 10 + 8);

            releaseButton(sender);
        }

        private void btnGapRightBottomDown_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapRightBottom.SelectedIndex;

            if (index >= 24)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapRightBottom.SelectionChanged -= cmbxGapRightBottom_SelectionChanged;
            cmbxGapRightBottom.SelectedIndex = index + 1;
            cmbxGapRightBottom.SelectionChanged += cmbxGapRightBottom_SelectionChanged;

            setGapCorrectValue(CorrectPosition.RightBottom, (24 - cmbxGapRightBottom.SelectedIndex) * 10 + 8);

            releaseButton(sender);
        }

        #endregion Value Up & Down

        #endregion Correct

        #endregion Events
        
        #region Private Methods

        private bool getGapCorrectionMode()
        {
            if (dicController.Values.Count <= 0)
            { return true; }

            KeyValuePair<int, ControllerInfo> firstController = dicController.FirstOrDefault();
            ControllerInfo controller = firstController.Value;

            Byte[] cmd = new byte[SDCPClass.CmdGapCorrectGet.Length];
            Array.Copy(SDCPClass.CmdGapCorrectGet, cmd, SDCPClass.CmdGapCorrectGet.Length);

            sendSdcpCommand(cmd, out string mode, controller.IPAddress);

            if (mode == "00" || mode == "02") // "00" 目補正:Off,目地補正:Off | "02" 目補正:On,目地補正:Off
            {
                rbGapCorrectOff.Checked -= rbGapCorrectOff_Checked;
                rbGapCorrectOff.IsChecked = true;
                rbGapCorrectOff.Checked += rbGapCorrectOff_Checked;
            }
            else // On
            {
                rbGapCorrectOn.Checked -= rbGapCorrectOn_Checked;
                rbGapCorrectOn.IsChecked = true;
                rbGapCorrectOn.Checked += rbGapCorrectOn_Checked;
            }

            return true;
        }

        private void outputGapPlane()
        {
            tbtnCursor.IsChecked = false;

            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            int level;

            if(cmbxGapCorrectLevel.SelectedIndex == 0)
            { level = brightness._5pc; }
            else if(cmbxGapCorrectLevel.SelectedIndex == 2)
            { level = brightness._50pc; }
            else
            { level = brightness._20pc; }

            try { setPlaneSignal(ref cmd, level); }
            catch { return; }

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(cmd, 0, cont.IPAddress); }
        }

        private void setPlaneSignal(ref Byte[] cmd, int level, CellColor color = CellColor.White)
        {
            try
            {
                if (color == CellColor.White)
                {
                    cmd[21] = 0x70;

                    // Foreground Color
                    cmd[22] = (byte)(level >> 8); // Red
                    cmd[23] = (byte)(level & 0xFF);
                    cmd[24] = (byte)(level >> 8); // Green
                    cmd[25] = (byte)(level & 0xFF);
                    cmd[26] = (byte)(level >> 8); // Blue
                    cmd[27] = (byte)(level & 0xFF);
                }
                else if (color == CellColor.Red)
                {
                    cmd[21] = 0x40;

                    // Foreground Color
                    cmd[22] = (byte)(level >> 8); // Red
                    cmd[23] = (byte)(level & 0xFF);
                    cmd[24] = 0; // Green
                    cmd[25] = 0;
                    cmd[26] = 0; // Blue
                    cmd[27] = 0;
                }
                else if (color == CellColor.Green)
                {
                    cmd[21] = 0x20;

                    // Foreground Color
                    cmd[22] = 0; // Red
                    cmd[23] = 0;
                    cmd[24] = (byte)(level >> 8); // Green
                    cmd[25] = (byte)(level & 0xFF);
                    cmd[26] = 0; // Blue
                    cmd[27] = 0;
                }
                else if (color == CellColor.Blue)
                {
                    cmd[21] = 0x10;

                    // Foreground Color
                    cmd[22] = 0; // Red
                    cmd[23] = 0;
                    cmd[24] = 0; // Green
                    cmd[25] = 0;
                    cmd[26] = (byte)(level >> 8); // Blue
                    cmd[27] = (byte)(level & 0xFF);
                }
            }
            catch { } // 無視する
        }

        private void outputGapCursor()
        {
            CursorDirection dir;
            int x, y;
            UnitInfo unit;
            int level;

            try
            {
                x = Convert.ToInt32(txbCursorX.Text);
                y = Convert.ToInt32(txbCursorY.Text);
            }
            catch { return; }

            if (rbCursorX.IsChecked == true)
            { dir = CursorDirection.X; }
            else
            { dir = CursorDirection.Y; }

            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            setGapCursorCmd(ref cmd, dir, x, y, out unit, out level);

            //foreach (ControllerInfo cont in dicController.Values)
            //{ sendSdcpCommand(cmd, 0, cont.IPAddress); }

            foreach (ControllerInfo cont in dicController.Values)
            {
                if (unit != null && cont.ControllerID == unit.ControllerID)
                { sendSdcpCommand(cmd, 0, cont.IPAddress); }
                else
                { outputRaster(level, cont.ControllerID); }
            }
        }

        private void setGapCursorCmd(ref Byte[] cmd, CursorDirection dir, int x, int y, out UnitInfo unit, out int level)
        {
            if (cmbxGapCorrectLevel.SelectedIndex == 0)
            { level = brightness._5pc; }
            else if (cmbxGapCorrectLevel.SelectedIndex == 2)
            { level = brightness._50pc; }
            else
            { level = brightness._20pc; }

            int height, width;
            int startH = 0, startV = 0;            

            if (dir == CursorDirection.X)
            {
                height = 2;
                width = cabiDx; //320;
            }
            else
            {
                height = cabiDy; //360;
                width = 2;
            }

            // Unit Offset
            try { unit = aryUnitUf[x - 1, y - 1].UnitInfo; }
            catch
            {
                unit = null;
                return;
            }
            if(unit == null)
            { return; }

            //if (dir == CursorDirection.X)
            //{ startH += 320 * (x - 1); }
            //else
            //{ startH += 320 * x; }

            //startV += 360 * (6 - y);            
            if (dir == CursorDirection.X)
            { startH = unit.PixelX; }
            else
            { startH = unit.PixelX + cabiDx; } // 320; }

            startV = unit.PixelY;
            
            // Cursorの線が中央に来るようにOffset
            if (dir == CursorDirection.X)
            {
                startV--;
                if (startV < 0)
                { startV = 0; }

                //if (y > 1 && y < 6)
                //{ startV--; }
                //else if (y == 6)
                //{ startV = startV - 2; }
            }
            else
            {
                startH--;
                if (startH < 0)
                { startH = 0; }
                //if (x > 1 && x < 12)
                //{ startH--; }
                //else if (x == 12)
                //{ startH = startH - 2; }
            }

            cmd[21] = 0x79;

            try
            {
                // Foreground Color　//747 492
                int fore;
                if (cmbxGapCorrectLevel.SelectedIndex == 0 || cmbxGapCorrectLevel.SelectedIndex == 1)
                { fore = brightness._50pc; }
                else
                { fore = brightness._100pc; }

                cmd[22] = (byte)(fore >> 8); // Red //1024
                cmd[23] = (byte)(fore & 0xFF);
                cmd[24] = (byte)(fore >> 8); // Green
                cmd[25] = (byte)(fore & 0xFF);
                cmd[26] = (byte)(fore >> 8); // Blue
                cmd[27] = (byte)(fore & 0xFF);

                // Background Color
                cmd[28] = (byte)(level >> 8); // Red
                cmd[29] = (byte)(level & 0xFF);
                cmd[30] = (byte)(level >> 8); // Green
                cmd[31] = (byte)(level & 0xFF);
                cmd[32] = (byte)(level >> 8); // Blue
                cmd[33] = (byte)(level & 0xFF);

                // Start Position
                cmd[34] = (byte)(startH >> 8);
                cmd[35] = (byte)(startH & 0xFF);
                cmd[36] = (byte)(startV >> 8);
                cmd[37] = (byte)(startV & 0xFF);

                // H, V Width
                cmd[38] = (byte)(width >> 8);
                cmd[39] = (byte)(width & 0xFF);
                cmd[40] = (byte)(height >> 8);
                cmd[41] = (byte)(height & 0xFF);
            }
            catch { }
        }

        private void getGapCorrectValue(out CorrectionValue target)//, out CorrectionValue next)
        {
            CursorDirection dir;
            int x, y;
            string gap;
            UnitInfo unit = null;

            target = new CorrectionValue();
            //next = new CorrectionValue();

            try
            {
                x = Convert.ToInt32(txbCursorX.Text);
                y = Convert.ToInt32(txbCursorY.Text);

                unit = aryUnitUf[x - 1, y - 1].UnitInfo;
            }
            catch { return; }

            if(unit == null)
            { return; }

            if (rbCursorX.IsChecked == true)
            { dir = CursorDirection.X; }
            else
            { dir = CursorDirection.Y; }

            Byte[] cmd = new byte[SDCPClass.CmdGapCorrectValueGet.Length];
            Array.Copy(SDCPClass.CmdGapCorrectValueGet, cmd, SDCPClass.CmdGapCorrectValueGet.Length);

            // Target
            //cmd[9] += (byte)(x << 4);
            //cmd[9] += (byte)y;
            cmd[9] += (byte)(unit.PortNo << 4);
            cmd[9] += (byte)unit.UnitNo;
            
            //if (dir == CursorDirection.X)
            //{ cmd[8] = 0; }
            //else
            //{ cmd[8] = 6; }

            //sendSdcpCommand(cmd, out gap, dicController[unit.ControllerID].IPAddress);

            //try { target.Value1 = Convert.ToInt32(gap, 16); }
            //catch { return; }

            //if (dir == CursorDirection.X)
            //{ cmd[8] = 1; }
            //else
            //{ cmd[8] = 7; }

            //sendSdcpCommand(cmd, out gap, dicController[unit.ControllerID].IPAddress);

            //try { target.Value2 = Convert.ToInt32(gap, 16); }
            //catch { return; }

            // 全周同時取得に対応
            cmd[8] = 0xFF;

            sendSdcpCommand(cmd, out gap, dicController[unit.ControllerID].IPAddress);

            if(dir == CursorDirection.X)
            {
                try
                {
                    target.Value1 = Convert.ToInt32(gap.Substring(0, 2), 16);
                    target.Value2 = Convert.ToInt32(gap.Substring(2, 2), 16);
                }
                catch { return; }
            }
            else
            {
                try
                {
                    target.Value1 = Convert.ToInt32(gap.Substring(12, 2), 16);
                    target.Value2 = Convert.ToInt32(gap.Substring(14, 2), 16);
                }
                catch { return; }
            }

            //// Next //隣接Unitは無視
            //int nextX, nextY;

            //if (dir == PlaneDirection.X)
            //{
            //    nextX = x;
            //    nextY = y + 1;
            //}
            //else
            //{
            //    nextX = x + 1;
            //    nextY = y;
            //}

            //cmd[9] = 0;
            //cmd[9] += (byte)(nextX << 4);
            //cmd[9] += (byte)nextY;

            //if (dir == PlaneDirection.X)
            //{ cmd[8] = 2; }
            //else
            //{ cmd[8] = 4; }

            //ret = sendSdcpCommand(cmd);

            //try { next.Value1 = Convert.ToInt32(ret); }
            //catch { return; }

            //if (dir == PlaneDirection.X)
            //{ cmd[8] = 3; }
            //else
            //{ cmd[8] = 5; }

            //ret = sendSdcpCommand(cmd);

            //try { next.Value2 = Convert.ToInt32(ret); }
            //catch { return; }
        }

        private void setGapCorrectValue(CorrectPosition pos, int value)
        {
            CursorDirection dir;
            int x, y;
            UnitInfo unit = null;

            try
            {
                x = Convert.ToInt32(txbCursorX.Text);
                y = Convert.ToInt32(txbCursorY.Text);

                unit = aryUnitUf[x - 1, y - 1].UnitInfo;
            }
            catch { return; }

            if (unit == null)
            { return; }

            if (rbCursorX.IsChecked == true)
            { dir = CursorDirection.X; }
            else
            { dir = CursorDirection.Y; }

            Byte[] cmd = new byte[SDCPClass.CmdGapCorrectValueSet.Length];
            Array.Copy(SDCPClass.CmdGapCorrectValueSet, cmd, SDCPClass.CmdGapCorrectValueSet.Length);

            // Target Unit
            //cmd[9] += (byte)(x << 4);
            //cmd[9] += (byte)y;
            cmd[9] += (byte)(unit.PortNo << 4);
            cmd[9] += (byte)unit.UnitNo;

            if (pos == CorrectPosition.TopLeft)
            { cmd[8] = 0; }
            else if (pos == CorrectPosition.TopRight)
            { cmd[8] = 1; }
            else if (pos == CorrectPosition.RightTop)
            { cmd[8] = 6; }
            else
            { cmd[8] = 7; }

            cmd[20] = (byte)value;

            sendSdcpCommand(cmd, 0, dicController[unit.ControllerID].IPAddress);

            // Next Unit
            int nextX, nextY;
            UnitInfo nextUnit = null;

            if (dir == CursorDirection.X)
            {
                nextX = x;
                nextY = y - 1; // 1つ上のUnit
            }
            else
            {
                nextX = x + 1;
                nextY = y;
            }

            try { nextUnit = aryUnitUf[nextX - 1, nextY - 1].UnitInfo; }
            catch { return; }

            if(nextUnit == null)
            { return; }
            
            // Unit Address
            cmd[9] = 0;
            cmd[9] += (byte)(nextUnit.PortNo << 4);
            cmd[9] += (byte)nextUnit.UnitNo;

            if (pos == CorrectPosition.TopLeft)
            { cmd[8] = 2; }
            else if (pos == CorrectPosition.TopRight)
            { cmd[8] = 3; }
            else if (pos == CorrectPosition.RightTop)
            { cmd[8] = 4; }
            else
            { cmd[8] = 5; }

            cmd[20] = (byte)value;

            sendSdcpCommand(cmd, 500, dicController[nextUnit.ControllerID].IPAddress);
        }

        private bool resetGapCorrect()
        {
            // ●進捗の最大値を設定
            int unitCount = 0;
            foreach (ControllerInfo cont in dicController.Values)
            { unitCount += cont.UnitCount; }

            winProgress.ShowMessage("Set Step Count.");
            winProgress.SetWholeSteps(unitCount);

            foreach (List<UnitInfo> lst in allocInfo.lstUnits)
            {
                foreach (UnitInfo unit in lst)
                {
                    if(unit == null)
                    { continue; }

                    winProgress.ShowMessage("Cabinet : Port No. = " + unit.PortNo + "\r\nCabinet No. = " + unit.UnitNo + " (TopLeft)");
                    if (sendGapCorrectValue(unit, CorrectPosition.TopLeft, 128, Settings.Ins.SdcpWaitTime) != true)
                    { return false; }

                    winProgress.ShowMessage("Cabinet : Port No. = " + unit.PortNo + "\r\nCabinet No. = " + unit.UnitNo + " (TopRight)");
                    if (sendGapCorrectValue(unit, CorrectPosition.TopRight, 128, Settings.Ins.SdcpWaitTime) != true)
                    { return false; }

                    winProgress.ShowMessage("Cabinet : Port No. = " + unit.PortNo + "\r\nCabinet No. = " + unit.UnitNo + " (BottomLeft)");
                    if (sendGapCorrectValue(unit, CorrectPosition.BottomLeft, 128, Settings.Ins.SdcpWaitTime) != true)
                    { return false; }

                    winProgress.ShowMessage("Cabinet : Port No. = " + unit.PortNo + "\r\nCabinet No. = " + unit.UnitNo + " (BottomRight)");
                    if (sendGapCorrectValue(unit, CorrectPosition.BottomRight, 128, Settings.Ins.SdcpWaitTime) != true)
                    { return false; }

                    winProgress.ShowMessage("Cabinet : Port No. = " + unit.PortNo + "\r\nCabinet No. = " + unit.UnitNo + " (LeftTop)");
                    if (sendGapCorrectValue(unit, CorrectPosition.LeftTop, 128, Settings.Ins.SdcpWaitTime) != true)
                    { return false; }

                    winProgress.ShowMessage("Cabinet : Port No. = " + unit.PortNo + "\r\nCabinet No. = " + unit.UnitNo + " (LeftBottom)");
                    if (sendGapCorrectValue(unit, CorrectPosition.LeftBottom, 128, Settings.Ins.SdcpWaitTime) != true)
                    { return false; }

                    winProgress.ShowMessage("Cabinet : Port No. = " + unit.PortNo + "\r\nCabinet No. = " + unit.UnitNo + " (RightTop)");
                    if (sendGapCorrectValue(unit, CorrectPosition.RightTop, 128, Settings.Ins.SdcpWaitTime) != true)
                    { return false; }

                    winProgress.ShowMessage("Cabinet : Port No. = " + unit.PortNo + "\r\nCabinet No. = " + unit.UnitNo + " (RightBottom)");
                    if (sendGapCorrectValue(unit, CorrectPosition.RightBottom, 128, Settings.Ins.SdcpWaitTime) != true)
                    { return false; }

                    winProgress.PutForward1Step();
                }
            }

            return true;
        }

        private bool sendGapCorrectValue(UnitInfo unit, CorrectPosition pos, int value, int wait = 0)
        {
            Byte[] cmd = new byte[SDCPClass.CmdGapCorrectValueSet.Length];
            Array.Copy(SDCPClass.CmdGapCorrectValueSet, cmd, SDCPClass.CmdGapCorrectValueSet.Length);

            // Target Unit
            cmd[9] += (byte)(unit.PortNo << 4);
            cmd[9] += (byte)unit.UnitNo;

            if (pos == CorrectPosition.TopLeft)
            { cmd[8] = 0; }
            else if (pos == CorrectPosition.TopRight)
            { cmd[8] = 1; }
            else if(pos == CorrectPosition.BottomLeft)
            { cmd[8] = 2; }
            else if(pos == CorrectPosition.BottomRight)
            { cmd[8] = 3; }
            else if (pos == CorrectPosition.LeftTop)
            { cmd[8] = 4; }
            else if (pos == CorrectPosition.LeftBottom)
            { cmd[8] = 5; }
            else if (pos == CorrectPosition.RightTop)
            { cmd[8] = 6; }
            else if (pos == CorrectPosition.RightBottom)
            { cmd[8] = 7; }

            cmd[20] = (byte)value;

            try { sendSdcpCommand(cmd, wait, dicController[unit.ControllerID].IPAddress); }
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }

            return true;
        }

        private void setGapCorrection(bool setCorrectionMode)
        {
            byte[] getCorrectMode = new byte[SDCPClass.CmdGapCorrectGet.Length];
            Array.Copy(SDCPClass.CmdGapCorrectGet, getCorrectMode, SDCPClass.CmdGapCorrectGet.Length);

            byte[] cmd = new byte[SDCPClass.CmdCorrectModeSet.Length];
            Array.Copy(SDCPClass.CmdCorrectModeSet, cmd, SDCPClass.CmdCorrectModeSet.Length);

            foreach (ControllerInfo controller in dicController.Values)
            {
                string mode;
                sendSdcpCommand(getCorrectMode, out mode, controller.IPAddress);

                if (setCorrectionMode)
                {
                    if (mode == "00") // 目補正:Off, 目地補正:Off
                    { cmd[20] = 0x01; }
                    else if (mode == "02") // 目補正:On, 目地補正:Off
                    { cmd[20] = 0x03; }
                    else
                    { return; }
                }
                else
                {
                    if (mode == "01") // 目補正:Off, 目地補正:On
                    { cmd[20] = 0x00; }
                    else if (mode == "03") // 目補正:On, 目地補正:On
                    { cmd[20] = 0x02; }
                    else
                    { return; }
                }

                sendSdcpCommand(cmd, controller.IPAddress);
            }
        }

        #endregion Private Methods
    }
}
