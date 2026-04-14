using System;
using System.Text;
using System.Collections.Generic;
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
using System.IO;
using Microsoft.VisualBasic.FileIO;

using MakeUFData;

namespace CAS
{
    using CabinetOffset = System.Drawing.Point;
    using WidthOffset = System.Drawing.Point;

    // UF Manual
    public partial class MainWindow : Window
    {
        #region Fields

        private int cursorUnitX = 0;
        private int cursorUnitY = 0;
        private int cursorCellX = 0;
        private int cursorCellY = 0;

        private const int cursorUnitX_Min = 0;
        private const int cursorUnitY_Min = 0;
        private const int cursorCellX_Min = 0;
        private const int cursorCellX_Max = 3;
        private const int cursorCellY_Min = 0;

        //private MeasurementMode ufManualMode;

        private List<UnitInfo> lstSelectedUnits = new List<UnitInfo>();

        private bool isFirstSelectedOn = true;
        private bool isUfSelectBtnSelected = false;

        private const int TabIndex_UfManual = 2;//Uniformity(Manual)のTabIndex

        private const int WRITE_MANUAL_GAIN_STEPS = 17;

        #endregion Fields

        #region Events

        #region Cursor

        private void tbtnCursorUfManual_Checked(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Cursor On");

            if (isFirstSelectedOn)
            { setUfManualCorrection(true); }

            isUfSelectBtnSelected = false; // Select解除

            outputCursorUfManual();
            gdUfManualCursor.IsEnabled = true;
            gdAdjLevel.IsEnabled = false;
            //gdWriteUfManual.IsEnabled = false;

            releaseButton(sender);
        }

        private void tbtnCursorUfManual_Unchecked(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Cursor Off");

            if (isFirstSelectedOn)
            { setUfManualCorrection(false); }

            if (cmbxPatternUfManual.SelectedIndex == -1)
            { cmbxPatternUfManual.SelectedIndex = 0; }

            cmbxPatternUfManual_DropDownClosed(sender, e);
            gdUfManualCursor.IsEnabled = false;

            isFirstSelectedOn = true;

            releaseButton(sender);
        }
        
        private void rbCursorUnitUfManual_Checked(object sender, RoutedEventArgs e)
        {
            if (gamePadViewModel.CursorUfManual && TabIndex_UfManual == gamePadViewModel.TabSelectIndex)
            {
                isFirstSelectedOn = false;
                tbtnCursorUfManual_Checked(sender, e);
                cursorCellX = 0;
                cursorCellY = 0;
            }
        }

        private void rbCursorCellUfManual_Checked(object sender, RoutedEventArgs e)
        {
            if (gamePadViewModel.CursorUfManual && TabIndex_UfManual == gamePadViewModel.TabSelectIndex)
            {
                isFirstSelectedOn = false;
                tbtnCursorUfManual_Checked(sender, e);
            }
        }
        
        private void btnCursorUpUfManual_Click(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Move Cursor");

            // Unit
            if (rbCursorUnitUfManual.IsChecked == true)
            {
                cursorUnitY--;
                if (cursorUnitY < 0)
                { cursorUnitY = 0; }
            }
            // Cell
            else
            {
                cursorCellY--;
                if (cursorCellY < cursorCellY_Min)
                {
                    cursorUnitY--;
                    if (cursorUnitY >= cursorUnitY_Min)
                    { cursorCellY = cursorCellY_Max; }
                    else
                    {
                        cursorUnitY = cursorUnitY_Min;
                        cursorCellY = cursorCellY_Min;
                    }
                }
            }

            outputCursorUfManual();

            releaseButton(sender);
        }

        private void btnCursorLeftUfManual_Click(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Move Cursor");

            // Unit
            if (rbCursorUnitUfManual.IsChecked == true)
            {
                cursorUnitX--;
                if (cursorUnitX < 0)
                { cursorUnitX = 0; }
            }
            // Cell
            else
            {
                cursorCellX--;
                if (cursorCellX < cursorCellX_Min)
                {
                    cursorUnitX--;
                    if (cursorUnitX >= cursorUnitX_Min)
                    { cursorCellX = cursorCellX_Max; }
                    else
                    {
                        cursorUnitX = cursorUnitX_Min;
                        cursorCellX = cursorCellX_Min;
                    }
                }
            }

            outputCursorUfManual();

            releaseButton(sender);
        }

        private void btnCursorRightUfManual_Click(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Move Cursor");
            
            // Unit
            if (rbCursorUnitUfManual.IsChecked == true)
            {
                cursorUnitX++;
                if (cursorUnitX >= allocInfo.MaxX)
                { cursorUnitX = allocInfo.MaxX - 1; }
            }
            // Cell
            else
            {
                cursorCellX++;
                if(cursorCellX > cursorCellX_Max)
                {
                    cursorUnitX++;
                    if (cursorUnitX >= allocInfo.MaxX)
                    {
                        cursorUnitX = allocInfo.MaxX - 1;
                        cursorCellX = cursorCellX_Max;
                    }
                    else
                    { cursorCellX = cursorCellX_Min; }
                }
            }

            outputCursorUfManual();

            releaseButton(sender);
        }

        private void btnCursorDownUfManual_Click(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Move Cursor");

            // Unit
            if (rbCursorUnitUfManual.IsChecked == true)
            {
                cursorUnitY++;
                if (cursorUnitY >= allocInfo.MaxY)
                { cursorUnitY = allocInfo.MaxY - 1; }
            }
            // Cell
            else
            {
                cursorCellY++;
                if (cursorCellY > cursorCellY_Max)
                {
                    cursorUnitY++;
                    if (cursorUnitY >= allocInfo.MaxY)
                    {
                        cursorUnitY = allocInfo.MaxY - 1;
                        cursorCellY = cursorCellY_Max;
                    }
                    else
                    { cursorCellY = cursorCellY_Min; }
                }
            }

            outputCursorUfManual();

            releaseButton(sender);
        }

        #endregion Cursor

        private void btnSelectUfManual_Click(object sender, RoutedEventArgs e)
        {
            if (aryUnitData[cursorUnitX, cursorUnitY].UnitInfo == null)
            {
                ShowMessageWindow("Failed to store Cabinet information.", "Error!", System.Drawing.SystemIcons.Error, 320, 180);
                return;
            }

            isFirstSelectedOn = false;
            tbtnCursorUfManual.IsChecked = false;
            isUfSelectBtnSelected = true;
            gdAdjLevel.IsEnabled = true;
            //gdWriteUfManual.IsEnabled = true;

            try
            {
                if (checkAlreadySelected(aryUnitData[cursorUnitX, cursorUnitY].UnitInfo) == false)
                { lstSelectedUnits.Add(aryUnitData[cursorUnitX, cursorUnitY].UnitInfo); }
            }
            catch
            {
                ShowMessageWindow("Failed to store Cabinet information.", "Error!", System.Drawing.SystemIcons.Error, 320, 180);
                return;
            }

            outputSignalUfManualAdjustArea();
            //getManualGain();
        }

        private async void btnExecuteUfManual_Click(object sender, RoutedEventArgs e)
        {
            bool status = false;

            tcMain.IsEnabled = false;
            actionButton(sender, "Write Manual Gain Start.");

            //string msg = "Start writing the manual gain.\r\nIt takes approximate each 1 minute per Display unit to complete data transfer. However you may not cancel after start.\r\n\r\nAre you sure?";
            string msg = "It takes approximate each 1 minute per Display cabinet. \r\n\r\nAre you sure ?";
            bool? result = false;
            showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 500, 210, "Yes");

            if (result == true)
            {
                winProgress = new WindowProgress("Write Progress");
                winProgress.Show();

                m_MakeUFData = new CMakeUFData(cabiDx, cabiDy);

                status = await Task.Run(() => writeManualGain());

                // ●Cabinet Power On
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("Cabinet Power On."); }
                foreach (ControllerInfo cont in dicController.Values)
                { sendSdcpCommand(SDCPClass.CmdUnitPowerOn, SDCP_COMMAND_WAIT_TIME, cont.IPAddress); }
                System.Threading.Thread.Sleep(5000);
                // ●Raster表示
                outputRaster(brightness._20pc); // 20%

                msg = "";
                string caption = "";
                if (status == true)
                {
                    msg = "Write Manual Gain complete!";
                    caption = "Complete";
                }
                else
                {
                    foreach (ControllerInfo controller in dicController.Values)
                    {
                        if (controller.Target == true)
                        {
                            if (sendSdcpCommand(SDCPClass.CmdFtpOff, controller.IPAddress) != true)
                            {
                                ShowMessageWindow("Failed to FTP Off.", "Error!", System.Drawing.SystemIcons.Error, 420, 180);
                            }
                        }
                    }

                    msg = "Failed to Write Manual Gain.";
                    caption = "Error";
                }

                // ●すべてのドライブをアンマウント
                unmountDirve();

                winProgress.Close();
                playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");

                WindowMessage winMessage = new WindowMessage(msg, caption);
                winMessage.ShowDialog();
                
                // UF Manual調整済みUnitのリストをリセット
                lstSelectedUnits.Clear();
            }

            releaseButton(sender, "Write Manual Gain Done.");
            tcMain.IsEnabled = true;
        }

        #region Signal
        
        private void cmbxLevelUfManual_DropDownClosed(object sender, EventArgs e)
        {
            if (tbtnCursorUfManual.IsChecked == false)
            {
                if (isUfSelectBtnSelected == true)
                { outputSignalUfManualAdjustArea(); }
                else
            	{ cmbxPatternUfManual_DropDownClosed(sender, e); }
            }
            else
            {
                actionButton(sender, "Change Cursor Level");
                outputCursorUfManual();
                releaseButton(sender);
            }
        }

        private void cmbxLevelUfManual_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabIndex_UfManual == gamePadViewModel.TabSelectIndex)
            {
                cmbxLevelUfManual_DropDownClosed(sender, e);
            }
        }

        private void cmbxColorUfManual_DropDownClosed(object sender, EventArgs e)
        {
            if (tbtnCursorUfManual.IsChecked == false)
            {
                if (isUfSelectBtnSelected == true)
                { outputSignalUfManualAdjustArea(); }
                else
            	{ cmbxPatternUfManual_DropDownClosed(sender, e); }
            }
            else
            {
                actionButton(sender, "Change Cursor Color");
                outputCursorUfManual();
                releaseButton(sender);
            }
        }
        private void cmbxColorlUfManual_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabIndex_UfManual == gamePadViewModel.TabSelectIndex)
            {
                cmbxColorUfManual_DropDownClosed(sender, e);
            }
        }

        private void cmbxPatternUfManual_DropDownClosed(object sender, EventArgs e)
        {
            if (cmbxPatternUfManual != null && tbtnCursorUfManual.IsChecked == false)
            {
                if (cmbxPatternUfManual.SelectedIndex == 0)
                {
                    actionButton(sender, "Set Plane Signal");
                    outputSignalUfManual();
                    releaseButton(sender);
                }
                else if (cmbxPatternUfManual.SelectedIndex == 1)
                {
                    actionButton(sender, "Set Hatch Cabinet");
                    outputSignalUfManual(TestPattern.Unit);
                    releaseButton(sender);
                }
                else if (cmbxPatternUfManual.SelectedIndex == 2)
                {
                    actionButton(sender, "Set Hatch Module");
                    outputSignalUfManual(TestPattern.Cell);
                    releaseButton(sender);
                }
                else if (cmbxPatternUfManual.SelectedIndex == 3)
                {
                    actionButton(sender, "Internal Signal Off");
                    foreach (ControllerInfo cont in dicController.Values)
                    { sendSdcpCommand(SDCPClass.CmdIntSignalOff, 0, cont.IPAddress); }
                    releaseButton(sender);
                }
            }
        }

        #endregion Signal

        #region Adjust

        private void btnResetUfManual_Click(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Reset");

            UnitInfo unit = aryUnitData[cursorUnitX, cursorUnitY].UnitInfo;

            MeasurementMode mode = getMeasurementMode();
            resetManualGain(unit, mode);

            releaseButton(sender);
        }

        private void btnRedUp_Click(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Red Gain Up");

            int gainR, gainG = 0, gainB = 0;

            gainR = (cmbxRedGainUfManual.SelectedIndex + 1) * 5;
            setManualGain(gainR, gainG, gainB);

            releaseButton(sender);
        }

        private void btnRedDown_Click(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Red Gain Down");

            int gainR, gainG = 0, gainB = 0;

            gainR = -1 * (cmbxRedGainUfManual.SelectedIndex + 1) * 5;
            setManualGain(gainR, gainG, gainB);

            releaseButton(sender);
        }

        private void btnGreenUp_Click(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Green Gain Up");

            int gainR = 0, gainG, gainB = 0;

            gainG = (cmbxGreenGainUfManual.SelectedIndex + 1) * 5;
            setManualGain(gainR, gainG, gainB);

            releaseButton(sender);
        }

        private void btnGreenDown_Click(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Green Gain Down");

            int gainR = 0, gainG, gainB = 0;

            gainG = -1 * (cmbxGreenGainUfManual.SelectedIndex + 1) * 5;
            setManualGain(gainR, gainG, gainB);

            releaseButton(sender);
        }

        private void btnBlueUp_Click(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Blue Gain Up");

            int gainR = 0, gainG = 0, gainB;

            gainB = (cmbxBlueGainUfManual.SelectedIndex + 1) * 5;
            setManualGain(gainR, gainG, gainB);

            releaseButton(sender);
        }

        private void btnBlueDown_Click(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Blue Gain Down");

            int gainR = 0, gainG = 0, gainB;

            gainB = -1 * (cmbxBlueGainUfManual.SelectedIndex + 1) * 5;
            setManualGain(gainR, gainG, gainB);

            releaseButton(sender);
        }

        private void btnWhiteUp_Click(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "White Gain Up");

            int gainR, gainG, gainB;

            gainR = (cmbxWhiteGainUfManual.SelectedIndex + 1) * 5;
            gainG = (cmbxWhiteGainUfManual.SelectedIndex + 1) * 5;
            gainB = (cmbxWhiteGainUfManual.SelectedIndex + 1) * 5;

            setManualGain(gainR, gainG, gainB);

            releaseButton(sender);
        }

        private void btnWhiteDown_Click(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "White Gain Down");

            int gainR, gainG, gainB;

            gainR = -1 * (cmbxWhiteGainUfManual.SelectedIndex + 1) * 5;
            gainG = -1 * (cmbxWhiteGainUfManual.SelectedIndex + 1) * 5;
            gainB = -1 * (cmbxWhiteGainUfManual.SelectedIndex + 1) * 5;

            setManualGain(gainR, gainG, gainB);

            releaseButton(sender);
        }

        #endregion

        #region Debug

        private void btnReadManualGain_Click(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Read");

            try
            {
                string value;
                string[] aryValues = new string[moduleCount]; // 各ModuleのAdjustment level調整データを格納
                UnitInfo unit = aryUnitData[cursorUnitX, cursorUnitY].UnitInfo;

                Byte[] cmd = new byte[SDCPClass.CmdUnitManualAdjGet.Length];
                Array.Copy(SDCPClass.CmdUnitManualAdjGet, cmd, SDCPClass.CmdUnitManualAdjGet.Length);

                cmd[9] += (byte)(unit.PortNo << 4);
                cmd[9] += (byte)unit.UnitNo;

                sendSdcpCommand(cmd, out value, dicController[unit.ControllerID].IPAddress);

                for (int i = 0; i < moduleCount; i++)
                { aryValues[i] = value.Substring(i * 12, 12); }

                if (rbCursorUnitUfManual.IsChecked == true)
                {
                    txbGainRed.Text = aryValues[0].Substring(0, 4);
                    txbGainGreen.Text = aryValues[0].Substring(4, 4);
                    txbGainBlue.Text = aryValues[0].Substring(8, 4);

                    txbGainRedDec.Text = calcComplement(txbGainRed.Text).ToString();
                    txbGainGreenDec.Text = calcComplement(txbGainGreen.Text).ToString();
                    txbGainBlueDec.Text = calcComplement(txbGainBlue.Text).ToString();
                }
                else
                {
                    txbGainRed.Text = aryValues[cursorCellX + cursorCellY * 4].Substring(0, 4);
                    txbGainGreen.Text = aryValues[cursorCellX + cursorCellY * 4].Substring(4, 4);
                    txbGainBlue.Text = aryValues[cursorCellX + cursorCellY * 4].Substring(8, 4);

                    txbGainRedDec.Text = calcComplement(txbGainRed.Text).ToString();
                    txbGainGreenDec.Text = calcComplement(txbGainGreen.Text).ToString();
                    txbGainBlueDec.Text = calcComplement(txbGainBlue.Text).ToString();
                }
            }
            catch { }

            releaseButton(sender);
        }

        private void btnWriteManualGain_Click(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Write");

            try
            {
                string value;
                UnitInfo unit = aryUnitData[cursorUnitX, cursorUnitY].UnitInfo;

                Byte[] cmd = new byte[SDCPClass.CmdUnitManualAdjSet.Length];
                Array.Copy(SDCPClass.CmdUnitManualAdjSet, cmd, SDCPClass.CmdUnitManualAdjSet.Length);

                cmd[9] += (byte)(unit.PortNo << 4);
                cmd[9] += (byte)unit.UnitNo;

                if (rbCursorUnitUfManual.IsChecked == true)
                { cmd[20] = 0; } // 全Cell
                else
                { cmd[20] = (byte)(cursorCellX + 1 + cursorCellY * 4); }

                cmd[21] = (byte)(Convert.ToInt32(txbWriteGainRed.Text) >> 8);
                cmd[22] = (byte)(Convert.ToInt32(txbWriteGainRed.Text) & 0xFF);
                cmd[23] = (byte)(Convert.ToInt32(txbWriteGainGreen.Text) >> 8);
                cmd[24] = (byte)(Convert.ToInt32(txbWriteGainGreen.Text) & 0xFF);
                cmd[25] = (byte)(Convert.ToInt32(txbWriteGainBlue.Text) >> 8);
                cmd[26] = (byte)(Convert.ToInt32(txbWriteGainBlue.Text) & 0xFF);

                sendSdcpCommand(cmd, out value, dicController[unit.ControllerID].IPAddress);
            }
            catch { }

            releaseButton(sender);
        }

        #endregion

        #endregion

        #region Private Methods

        private void outputCursorUfManual()
        {
            int foregroundLevel, backgroundLevel;
            MeasurementMode mode;

            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            if (cmbxLevelUfManual.SelectedIndex == 0) // 20%
            {
                backgroundLevel = brightness._20pc;
                foregroundLevel = brightness._24pc; // 24% 2.2 = 535
            }
            else if (cmbxLevelUfManual.SelectedIndex == 1) // 50%
            {
                backgroundLevel = brightness._50pc;
                foregroundLevel = brightness._60pc; // 60% 2.2 = 811
            }
            else // 100%
            {
                backgroundLevel = brightness._100pc;
                foregroundLevel = brightness._80pc; // 80% 2.2 = 924
            }

            try
            {
                // 色設定
                setSignalColor(ref cmd, foregroundLevel, backgroundLevel);

                if (rbCursorUnitUfManual.IsChecked == true)
                { mode = MeasurementMode.UnitUniformity; }
                else
                { mode = MeasurementMode.CellUniformity; }

                // 位置設定
                UnitInfo unit;
                setCmdPosition(ref cmd, mode, out unit);

                foreach (ControllerInfo cont in dicController.Values)
                {
                    if (cont.ControllerID == unit.ControllerID)
                    { sendSdcpCommand(cmd, 0, cont.IPAddress); }
                    else
                    { outputRasterUfManual(backgroundLevel, cont.IPAddress); }
                }
            }
            catch { }
        }

        private void outputSignalUfManualAdjustArea()
        {
            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            int backgroundLevel, foregroundLevel;

            if (cmbxLevelUfManual.SelectedIndex == 0) // 20%
            {
                backgroundLevel = brightness._15pc; // 15% = 432
                foregroundLevel = brightness._20pc;
            }
            else if (cmbxLevelUfManual.SelectedIndex == 1) // 50%
            {
                backgroundLevel = brightness._40pc;
                foregroundLevel = brightness._50pc;
            }
            else // 100%
            {
                backgroundLevel = brightness._80pc;
                foregroundLevel = brightness._100pc;
            }

            // 色設定
            setSignalColor(ref cmd, foregroundLevel, backgroundLevel);

            cmd[21] += 0x09; // Pattern: Window

            if (MeasurementMode.UnitUniformity == getMeasurementMode())
            { outputSignalUfManualCabinetAdjustArea(ref cmd); }
            else
            { outputSignalUfManualModuleAdjustArea(ref cmd); }
        }

        private void outputSignalUfManualCabinetAdjustArea(ref Byte[] cmd)
        {
            List<UnitInfo> selectedAllCabinets = new List<UnitInfo>();
            foreach (AdjustmentPosition position in Enum.GetValues(typeof(AdjustmentPosition)))
            {
                CabinetOffset offset = RelativePosition.GetCabinetOffset(position);
                int x = cursorUnitX + offset.X;
                int y = cursorUnitY + offset.Y;
                if ((x >= 0 && x < allocInfo.MaxX) && (y >= 0 && y < allocInfo.MaxY))
                {
                    UnitInfo cabinet = aryUnitData[x, y].UnitInfo;
                    if (cabinet != null)
                    { selectedAllCabinets.Add(cabinet); }
                }
            }

            List<UnitInfo> selectedControllerCabinets = new List<UnitInfo>();
            foreach (ControllerInfo controller in dicController.Values)
            {
                selectedControllerCabinets.Clear();
                foreach (UnitInfo cabinet in selectedAllCabinets)
                {
                    if (cabinet.ControllerID == controller.ControllerID)
                    { selectedControllerCabinets.Add(cabinet); }
                }

                int startPosX = 0;
                int startPosY = 0;
                int endPosX = 0;
                int endPosY = 0;
                foreach (UnitInfo cabinet in selectedControllerCabinets)
                {
                    if (endPosX == 0)
                    {
                        startPosX = cabinet.PixelX;
                        startPosY = cabinet.PixelY;
                        endPosX = startPosX + cabiDx;
                        endPosY = startPosY + cabiDy;
                        continue;
                    }

                    // 開始位置の補正
                    if (cabinet.PixelX < startPosX)
                    { startPosX = cabinet.PixelX; }

                    if (cabinet.PixelY < startPosY)
                    { startPosY = cabinet.PixelY; }

                    // 終了位置の補正
                    if (endPosX < cabinet.PixelX + cabiDx)
                    { endPosX = cabinet.PixelX + cabiDx; }

                    if (endPosY < cabinet.PixelY + cabiDy)
                    { endPosY = cabinet.PixelY + cabiDy; }
                }

                // 位置設定
                setCmdPosition(ref cmd, startPosX, startPosY, endPosX - startPosX, endPosY - startPosY);

                sendSdcpCommand(cmd, 0, controller.IPAddress);
            }
        }

        private void outputSignalUfManualModuleAdjustArea(ref Byte[] cmd)
        {
            CellNum selectModuleNo = (CellNum)(cursorCellX + (cursorCellY * 4));
            List<CellInfo> selectedAllModules = new List<CellInfo>();
            foreach (AdjustmentPosition position in Enum.GetValues(typeof(AdjustmentPosition)))
            {
                Tuple<CabinetOffset, WidthOffset> offset = RelativePosition.GetModuleOffset(selectModuleNo, position);
                int x = cursorUnitX + offset.Item1.X;
                int y = cursorUnitY + offset.Item1.Y;
                if ((x >= 0 && x < allocInfo.MaxX) && (y >= 0 && y < allocInfo.MaxY))
                {
                    UnitInfo cabinet = aryUnitData[x, y].UnitInfo;
                    if (cabinet == null)
                    { continue; }

                    int pixelX = cabinet.PixelX + (modDx * offset.Item2.X);
                    int pixelY = cabinet.PixelY + (modDy * offset.Item2.Y);
                    selectedAllModules.Add(new CellInfo(cabinet, pixelX, pixelY));
                }
            }

            List<CellInfo> selectedControllerModules = new List<CellInfo>();
            foreach (ControllerInfo controller in dicController.Values)
            {
                selectedControllerModules.Clear();
                foreach (CellInfo module in selectedAllModules)
                {
                    if (module.ControllerID == controller.ControllerID)
                    { selectedControllerModules.Add(module); }
                }

                int startPosX = 0;
                int startPosY = 0;
                int endPosX = 0;
                int endPosY = 0;
                foreach (CellInfo module in selectedControllerModules)
                {
                    if (endPosX == 0)
                    {
                        startPosX = module.PixelX;
                        startPosY = module.PixelY;
                        endPosX = startPosX + modDx;
                        endPosY = startPosY + modDy;
                        continue;
                    }

                    // 開始位置の補正
                    if (module.PixelX < startPosX)
                    { startPosX = module.PixelX; }

                    if (module.PixelY < startPosY)
                    { startPosY = module.PixelY; }

                    // 終了位置の補正
                    if (endPosX < module.PixelX + modDx)
                    { endPosX = module.PixelX + modDx; }

                    if (endPosY < module.PixelY + modDy)
                    { endPosY = module.PixelY + modDy; }
                }

                // 位置設定
                setCmdPosition(ref cmd, startPosX, startPosY, endPosX - startPosX, endPosY - startPosY);

                sendSdcpCommand(cmd, 0, controller.IPAddress);
            }
        }

        private void outputSignalUfManual(TestPattern pattern = TestPattern.Raster)
        {
            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            int level;
            CellColor color;

            if (cmbxLevelUfManual.SelectedIndex == 0) // 20%
            { level = brightness._20pc; }
            else if (cmbxLevelUfManual.SelectedIndex == 1) // 50%
            { level = brightness._50pc; }
            else // 100%
            { level = brightness._100pc; }

            if (cmbxColorUfManual.SelectedIndex == 1)
            { color = CellColor.Red; }
            else if (cmbxColorUfManual.SelectedIndex == 2)
            { color = CellColor.Green; }
            else if (cmbxColorUfManual.SelectedIndex == 3)
            { color = CellColor.Blue; }
            else
            { color = CellColor.White; }

            try { setPlaneSignal(ref cmd, level, color); }
            catch { return; }

            if (pattern == TestPattern.Unit)
            {
                cmd[21] += 0x01;

                // Start Position
                cmd[34] = 0;
                cmd[35] = 1;
                cmd[36] = 0;
                cmd[37] = 1;

                // H, V Width
                cmd[38] = (byte)(cabiDx - 2 >> 8);
                cmd[39] = (byte)(cabiDx - 2 & 0xff);
                cmd[40] = (byte)(cabiDy - 2 >> 8);
                cmd[41] = (byte)(cabiDy - 2 & 0xff);

                // H, V Pitch
                cmd[42] = (byte)(cabiDx >> 8);
                cmd[43] = (byte)(cabiDx & 0xff);
                cmd[44] = (byte)(cabiDy >> 8);
                cmd[45] = (byte)(cabiDy & 0xff);
            }
            else if (pattern == TestPattern.Cell)
            {
                cmd[21] += 0x01;

                // Start Position
                cmd[34] = 0;
                cmd[35] = 1;
                cmd[36] = 0;
                cmd[37] = 1;

                // H, V Width
                cmd[38] = (byte)(modDx - 2 >> 8);
                cmd[39] = (byte)(modDx - 2 & 0xff);
                cmd[40] = (byte)(modDy - 2 >> 8);
                cmd[41] = (byte)(modDy - 2 & 0xff);

                // H, V Pitch
                cmd[42] = (byte)(modDx >> 8);
                cmd[43] = (byte)(modDx & 0xff);
                cmd[44] = (byte)(modDy >> 8);
                cmd[45] = (byte)(modDy & 0xff);
            }

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(cmd, 0, cont.IPAddress); }
        }

        private void outputRasterUfManual(int level, string ip)
        {
            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            // Pattern Raster
            cmd[21] += 0x00; // Raster

            if (cmbxColorUfManual.SelectedIndex == 1)
            { cmd[21] = 0x40; }
            else if (cmbxColorUfManual.SelectedIndex == 2)
            { cmd[21] = 0x20; }
            else if (cmbxColorUfManual.SelectedIndex == 3)
            { cmd[21] = 0x10; }
            else
            { cmd[21] = 0x70; }

            // Foreground Color
            cmd[22] = (byte)(level >> 8); // Red
            cmd[23] = (byte)(level & 0xFF);
            cmd[24] = (byte)(level >> 8); // Green
            cmd[25] = (byte)(level & 0xFF);
            cmd[26] = (byte)(level >> 8); // Blue
            cmd[27] = (byte)(level & 0xFF);

            sendSdcpCommand(cmd, 0, ip);
        }

        private void setCmdForegroundLevel(ref Byte[] cmd, int level)
        {
            // Foreground Color
            cmd[22] = (byte)(level >> 8); // Red
            cmd[23] = (byte)(level & 0xFF);
            cmd[24] = (byte)(level >> 8); // Green
            cmd[25] = (byte)(level & 0xFF);
            cmd[26] = (byte)(level >> 8); // Blue
            cmd[27] = (byte)(level & 0xFF);
        }

        private void setSignalColor(ref Byte[] cmd, int foreground, int background)
        {
            // Pattern Raster
            cmd[21] = 0;

            if (cmbxColorUfManual.SelectedIndex == 1)
            { cmd[21] = 0x40; }
            else if (cmbxColorUfManual.SelectedIndex == 2)
            { cmd[21] = 0x20; }
            else if (cmbxColorUfManual.SelectedIndex == 3)
            { cmd[21] = 0x10; }
            else
            { cmd[21] = 0x70; }

            // Foreground Color
            cmd[22] = (byte)(foreground >> 8); // Red
            cmd[23] = (byte)(foreground & 0xFF);
            cmd[24] = (byte)(foreground >> 8); // Green
            cmd[25] = (byte)(foreground & 0xFF);
            cmd[26] = (byte)(foreground >> 8); // Blue
            cmd[27] = (byte)(foreground & 0xFF);

            // Background Color
            cmd[28] = (byte)(background >> 8); ; // Red
            cmd[29] = (byte)(background & 0xFF);
            cmd[30] = (byte)(background >> 8); ; // Green
            cmd[31] = (byte)(background & 0xFF);
            cmd[32] = (byte)(background >> 8); ; // Blue
            cmd[33] = (byte)(background & 0xFF);
        }

        private void setCmdPosition(ref Byte[] cmd, int startPosX, int startPosY, int widthH, int widthV)
        {
            // Start Position
            cmd[34] = (byte)(startPosX >> 8);
            cmd[35] = (byte)(startPosX & 0xFF);
            cmd[36] = (byte)(startPosY >> 8);
            cmd[37] = (byte)(startPosY & 0xFF);

            // H, V Width
            cmd[38] = (byte)(widthH >> 8);
            cmd[39] = (byte)(widthH & 0xFF);
            cmd[40] = (byte)(widthV >> 8);
            cmd[41] = (byte)(widthV & 0xFF);
        }

        private void setCmdPosition(ref Byte[] cmd, MeasurementMode mode, out UnitInfo unit)
        {
            int unitOffsetX, unitOffsetY;
            int cellOffsetX, cellOffsetY;

            unit = aryUnitData[cursorUnitX, cursorUnitY].UnitInfo;

            unitOffsetX = aryUnitData[cursorUnitX, cursorUnitY].UnitInfo.PixelX;
            unitOffsetY = aryUnitData[cursorUnitX, cursorUnitY].UnitInfo.PixelY;

            cellOffsetX = modDx * cursorCellX;
            cellOffsetY = modDy * cursorCellY;

            cmd[21] += 0x09;

            if (mode == MeasurementMode.UnitUniformity)
            {
                // Start Position
                cmd[34] = (byte)(unitOffsetX >> 8);
                cmd[35] = (byte)(unitOffsetX & 0xFF);
                cmd[36] = (byte)(unitOffsetY >> 8);
                cmd[37] = (byte)(unitOffsetY & 0xFF);

                // H, V Width
                cmd[38] = (byte)(cabiDx >> 8);
                cmd[39] = (byte)(cabiDx - 1 & 0xff);
                cmd[40] = (byte)(cabiDy >> 8);
                cmd[41] = (byte)(cabiDy - 1 & 0xff);
            }
            else
            {
                // Start Position
                cmd[34] = (byte)(unitOffsetX + cellOffsetX >> 8);
                cmd[35] = (byte)(unitOffsetX + cellOffsetX & 0xFF);
                cmd[36] = (byte)(unitOffsetY + cellOffsetY >> 8);
                cmd[37] = (byte)(unitOffsetY + cellOffsetY & 0xFF);

                // H, V Width
                cmd[38] = (byte)(modDx >> 8);
                cmd[39] = (byte)(modDx - 1 & 0xff);
                cmd[40] = (byte)(modDy >> 8);
                cmd[41] = (byte)(modDy - 1 & 0xff);
            }
        }

        private void setManualGain(int gainR, int gainG, int gainB)
        {
            string value;
            UnitInfo unit = aryUnitData[cursorUnitX, cursorUnitY].UnitInfo;

            Byte[] cmd = new byte[SDCPClass.CmdUnitManualAdjSet.Length];
            Array.Copy(SDCPClass.CmdUnitManualAdjSet, cmd, SDCPClass.CmdUnitManualAdjSet.Length);

            cmd[9] += (byte)(unit.PortNo << 4);
            cmd[9] += (byte)unit.UnitNo;

            MeasurementMode mode = getMeasurementMode();
            if (mode == MeasurementMode.UnitUniformity)
            { cmd[20] = 0; } // 全Module
            else
            { cmd[20] = (byte)(cursorCellX + 1 + cursorCellY * 4); }

            cmd[21] = (byte)(gainR >> 8);
            cmd[22] = (byte)(gainR & 0xFF);
            cmd[23] = (byte)(gainG >> 8);
            cmd[24] = (byte)(gainG & 0xFF);
            cmd[25] = (byte)(gainB >> 8);
            cmd[26] = (byte)(gainB & 0xFF);

            sendSdcpCommand(cmd, out value, dicController[unit.ControllerID].IPAddress);
        }

        private void resetManualGain(UnitInfo unit, MeasurementMode mode)
        {
            string value;

            Byte[] cmd = new byte[SDCPClass.CmdUnitManualAdjReset.Length];
            Array.Copy(SDCPClass.CmdUnitManualAdjReset, cmd, SDCPClass.CmdUnitManualAdjReset.Length);

            cmd[9] += (byte)(unit.PortNo << 4);
            cmd[9] += (byte)unit.UnitNo;

            if (mode == MeasurementMode.UnitUniformity)
            { cmd[20] = 0; } // 全Module
            else
            { cmd[20] = (byte)(cursorCellX + 1 + cursorCellY * 4); }

            sendSdcpCommand(cmd, out value, dicController[unit.ControllerID].IPAddress);
        }

        private MeasurementMode getMeasurementMode()
        {
            if (rbCursorUnitUfManual.IsChecked == true)
            { return MeasurementMode.UnitUniformity; }
            else
            { return MeasurementMode.CellUniformity; }
        }

        private bool writeManualGain()
        {
            if (Settings.Ins.ExecLog == true)
            {
                SaveExecLog("");
                SaveExecLog("[0] writeManualGain start");
            }

            List<UnitInfo> lstTargetUnit = new List<UnitInfo>();
            List<ControllerInfo> lstTargetController = new List<ControllerInfo>();
            List<MoveFile> lstMoveFile = new List<MoveFile>();
            bool status;
            FileDirectory baseFileDir;

            // ●進捗の最大値を設定 [1]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[1] Set Step Count."); }
            winProgress.ShowMessage("Set Step Count.");

            int step = WRITE_MANUAL_GAIN_STEPS;
            winProgress.SetWholeSteps(step);
            winProgress.PutForward1Step();

            // ●Power OnでないときはOnにする [2]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[2] Check Controller Power."); }
            winProgress.ShowMessage("Check Controller Power.");

            if (!setAllControllerPowerOn())
            { return false; }
            winProgress.PutForward1Step();

            // ●調整をするCabinetをListに格納 [3]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[3] Store Target Cabinet(s) Info."); }
            winProgress.ShowMessage("Store Target Cabinet(s) Info.");

            correctWrittenUnits(out lstTargetUnit);
            if (lstTargetUnit.Count == 0)
            {
                ShowMessageWindow("There are no cabinets with manual gain adjusted.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●対象Cabinetが接続されているController情報を保持する [4]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[4] Store Target Controller(s) Info."); }
            winProgress.ShowMessage("Store Target Controller(s) Info.");

            // 初期化
            foreach (ControllerInfo cont in dicController.Values)
            { cont.Target = false; }

            foreach (UnitInfo unit in lstTargetUnit)
            {
                if (lstTargetController.Contains(dicController[unit.ControllerID]) != true)
                {
                    dicController[unit.ControllerID].Target = true;
                    lstTargetController.Add(dicController[unit.ControllerID]);
                }
            }

            winProgress.PutForward1Step();

            // ●調整データのバックアップがすべてあるか確認 [5]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[5] Check Backup Data."); }
            winProgress.ShowMessage("Check Backup Data.");

            status = checkDataFile(lstTargetUnit, out baseFileDir);
            if (status != true)
            {
                ShowMessageWindow("There is not the module data file.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            #region Controller Settings

            foreach (ControllerInfo controller in lstTargetController)
            {
                // ●FTP ON [*1]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*1] FTP On."); }
                winProgress.ShowMessage("FTP On.");

                if (sendSdcpCommand(SDCPClass.CmdFtpOn, controller.IPAddress) != true)
                { return false; }

                // ●Model名取得 [*2]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*2] Get Model Name."); }
                winProgress.ShowMessage("Get Model Name.");

                if (string.IsNullOrWhiteSpace(controller.ModelName) == true)
                { controller.ModelName = getModelName(controller.IPAddress); }

                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("     Model Name : " + controller.ModelName); }

                // ●Serial取得 [*3]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*3] Get Serial No."); }
                winProgress.ShowMessage("Get Serial No.");

                if (string.IsNullOrWhiteSpace(controller.SerialNo) == true)
                { controller.SerialNo = getSerialNo(controller.IPAddress); }

                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("     Serial Num. : " + controller.SerialNo); }

                // ●Tempフォルダ内のファイルを削除 [*4]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*4] Delete Temp Files."); }
                winProgress.ShowMessage("Delete Temp Files.");

                string tempPath = applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo + "_Temp";

                if (Directory.Exists(tempPath) != true)
                { Directory.CreateDirectory(tempPath); }

                string[] files = Directory.GetFiles(tempPath, "*", System.IO.SearchOption.AllDirectories);

                for (int i = 0; i < files.Length; i++)
                {
                    try { File.Delete(files[i]); }
                    catch { return false; }
                }
            }
            winProgress.PutForward1Step();

            #endregion Controller Settings

            #region Make Corrected File

            foreach (UnitInfo unit in lstTargetUnit)
            {
                const int CELL_X_MAX = 4;
                const int CELL_Y_MAX_CHIRON = 2;
                const int CELL_Y_MAX_CANCUN = 3;

                // ●Cell Dataの補正データ抽出 [*1]
                winProgress.ShowMessage("Extract Correction Data.");
                string filePath = makeFilePath(unit, baseFileDir);
                status = m_MakeUFData.ExtractFmt(filePath, allocInfo.LEDModel);
                if (status != true)
                {
                    ShowMessageWindow("Failed in ExtractFmt.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }

                // ●抽出した補正データと調整目標値から逆算してその時のXYZの値を求める [*2]
                winProgress.ShowMessage("Calc XYZ.");
                status = m_MakeUFData.Fmt2XYZ(Defined.m_Target_xr, Defined.m_Target_yr, Defined.m_Target_xg, Defined.m_Target_yg, Defined.m_Target_xb, Defined.m_Target_yb, Defined.m_Target_Yw, Defined.m_Target_xw, Defined.m_Target_yw);
                if (status != true)
                {
                    ShowMessageWindow("Failed in Fmt2XYZ.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }

                // ●今回の計測値と前の計測値の差分を求めて、画素ごとのXYZデータを更新する [*3]
                winProgress.ShowMessage("Calc New XYZ Data.");

                int cellXMax, cellYMax;
                if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
                {
                    cellXMax = CELL_X_MAX;
                    cellYMax = CELL_Y_MAX_CHIRON;
                }
                else
                {
                    cellXMax = CELL_X_MAX;
                    cellYMax = CELL_Y_MAX_CANCUN;
                }

                for (int cellY = 0; cellY < cellYMax; cellY++)
                {
                    for (int cellX = 0; cellX < cellXMax; cellX++)
                    {
                        int startX = cellX * modDx;
                        int startY = cellY * modDy;

                        status = m_MakeUFData.ModifyXYZ_Multiply(startX, startY,
                            unit.ManualGain.lstGain[cellX + cellY * 4].GainR, unit.ManualGain.lstGain[cellX + cellY * 4].GainG, unit.ManualGain.lstGain[cellX + cellY * 4].GainB);
                        if (status != true)
                        {
                            ShowMessageWindow("Failed in ModifyXYZ_Multiply.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                            return false;
                        }
                    }
                }

                // ●補正データを作成する [*4]
                winProgress.ShowMessage("Make New Corrected Data.");
                double targetYw, targetYr, targetYg, targetYb;
                int ucr, ucg, ucb;
                //status = m_MakeUFData.Statistics(-1, out targetYw, out targetYr, out targetYg, out targetYb, out ucr, out ucg, out ucb);
                status = m_MakeUFData.CalcFmt(-1, -1, -1, -1, false, out targetYw, out targetYr, out targetYg, out targetYb, out ucr, out ucg, out ucb);
                if (status != true)
                {
                    ShowMessageWindow("Failed in Statistics.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }

                // ●ファイル保存 [*5]
                winProgress.ShowMessage("Save New Module Data.");
                string ajustedFile = makeFilePath(unit, FileDirectory.Temp);

                // フォルダがない場合、フォルダ作成
                if (System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(ajustedFile)) != true)
                { System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ajustedFile)); }

                try { status = m_MakeUFData.OverWritePixelData(ajustedFile, allocInfo.LEDModel); }
                catch { status = false; }
                if (status != true)
                {
                    ShowMessageWindow("Failed in OverWritePixelData.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }

                MoveFile move = new MoveFile();
                move.ControllerID = unit.ControllerID;
                move.FilePath = ajustedFile;

                lstMoveFile.Add(move);
            }

            #endregion

            // ●TestPattern OFF [6]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[6] Test Pattern Off."); }
            winProgress.ShowMessage("Test Pattern Off.");

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdIntSignalOff, 0, cont.IPAddress); }
            winProgress.PutForward1Step();

            // ●調整済みファイルの移動 [7]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[7] Move Adjusted Files."); }
            winProgress.ShowMessage("Move Adjusted Files.");

            foreach (MoveFile move in lstMoveFile)
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("\tMoved File = " + move.FilePath); }

                // ファイル移動
                try { status = putFileFtpRetry(dicController[move.ControllerID].IPAddress, move.FilePath); }
                catch { status = false; }

                if (status == false) // ファイルの移動が失敗するケースへの対応
                {
                    System.Threading.Thread.Sleep(5000);

                    try { status = putFileFtpRetry(dicController[move.ControllerID].IPAddress, move.FilePath); }
                    catch { status = false; }
                }

                if (status == false) // リトライ
                {
                    if (Settings.Ins.ExecLog == true)
                    { SaveExecLog("\t(Retry to move file.)"); }

                    System.Threading.Thread.Sleep(5000);

                    try
                    {
                        if (putFileFtpRetry(dicController[move.ControllerID].IPAddress, move.FilePath) != true)
                        {
                            ShowMessageWindow("Failed in moving adjusted module data file.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                            return false;
                        }
                    }
                    catch
                    {
                        ShowMessageWindow("Failed in moving adjusted module data file.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                        return false;
                    }
                }
            }
            winProgress.PutForward1Step();

            // ●Cabinet Power Off [8]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[8] Cabinet Power Off."); }
            winProgress.ShowMessage("Cabinet Power Off.");

            foreach (ControllerInfo cont in lstTargetController)
            { sendSdcpCommand(SDCPClass.CmdUnitPowerOff, cont.IPAddress); }
            System.Threading.Thread.Sleep(SLEEP_TIME_AFTER_PANEL_OFF);

            if (getUnitPowerStatus() != true)
            {
                string msg = "Failed to cabinet power off.";
                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }
            winProgress.PutForward1Step();

#if NO_WRITE
#else
            // ●Reconfig [9]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[9] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            sendReconfig();
            winProgress.PutForward1Step();

            // ●書き込みコマンド発行 [10]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[10] Send Write Command."); }
            winProgress.ShowMessage("Send Write Command.");

            foreach (ControllerInfo cont in lstTargetController)
            { sendSdcpCommand(SDCPClass.CmdDataWrite, cont.IPAddress); }
            winProgress.PutForward1Step();

            // ●書き込みComplete待ち [11]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[11] Waiting for the response."); }
            winProgress.ShowMessage("Waiting for the response.");
            winProgress.ElapsedTimer(true);

            try
            {
                bool[] complete = new bool[lstTargetController.Count];

                while (true)
                {
                    int id = 0;
                    foreach (ControllerInfo controller in lstTargetController)
                    {
                        complete[id] = checkCompleteFtp(controller.IPAddress, "write_complete");
                        id++;
                    }

                    bool allComplete = true;
                    for (int i = 0; i < complete.Length; i++)
                    {
                        if (complete[i] == false)
                        { allComplete = false; }
                    }

                    if (allComplete == true)
                    { break; }

                    winProgress.ShowMessage("Waiting for the process of controller.");

                    System.Threading.Thread.Sleep(1000);
                }
            }
            catch
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("Send Reconfig."); }
                winProgress.ShowMessage("Send Reconfig.");
                sendReconfig();
                return false;
            }

            winProgress.ElapsedTimer(false);
            winProgress.PutForward1Step();

            // ●Latest → Previousフォルダへコピー [12]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[12] Move Latest to Previous."); }
            winProgress.ShowMessage("Move Latest to Previous.");

            foreach (ControllerInfo controller in lstTargetController)
            { copyLatest2Previous(applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo); }
            winProgress.PutForward1Step();

            // ●Temp → Latestフォルダへコピー [13]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[13] Move Temp to Latest."); }
            winProgress.ShowMessage("Move Temp to Latest.");

            foreach (ControllerInfo controller in lstTargetController)
            { copyTemp2Latest(applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo); }
            winProgress.PutForward1Step();

            // ●Reconfig [14]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[14] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            sendReconfig();
            winProgress.PutForward1Step();
#endif

            // ●Tempフォルダのファイルを削除 [15]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[15] Delete Temp Files."); }
            winProgress.ShowMessage("Delete Temp Files.");

            foreach (ControllerInfo controller in lstTargetController)
            {
                string tempPath = applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo + "_Temp";

                if (Directory.Exists(tempPath) != true)
                { Directory.CreateDirectory(tempPath); }

                string[] files = Directory.GetFiles(tempPath, "*", System.IO.SearchOption.AllDirectories);

                for (int i = 0; i < files.Length; i++)
                {
                    try { File.Delete(files[i]); }
                    catch { return false; }
                }
            }
            winProgress.PutForward1Step();

            // ●すべてのManual Gainをリセット [16]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[16] Reset Manual Gain."); }
            winProgress.ShowMessage("Reset Manual Gain.");

            foreach (List<UnitInfo> lst in allocInfo.lstUnits)
            {
                foreach (UnitInfo unit in lst)
                {
                    if (unit != null)
                    { resetManualGain(unit, MeasurementMode.UnitUniformity); }
                }
            }
            winProgress.PutForward1Step();

            // ●FTP Off [17]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[17] FTP Off."); }
            winProgress.ShowMessage("FTP Off.");

            foreach (ControllerInfo controller in lstTargetController)
            { sendSdcpCommand(SDCPClass.CmdFtpOff, controller.IPAddress); }
            winProgress.PutForward1Step();

            return true;
        }

        private bool correctWrittenUnits(out List<UnitInfo> lstTargetUnit)
        {
            ManualGain gain;
            lstTargetUnit = new List<UnitInfo>();

            //foreach (List<UnitInfo> lst in allocInfo.lstUnits)
            //{
            //    foreach (UnitInfo unit in lst)
            //    {
            //        getManualGain(unit, out gain);

            //        if (gain != null)
            //        {
            //            unit.ManualGain = gain;
            //            lstTargetUnit.Add(unit);
            //        }
            //    }
            //}

            foreach (UnitInfo unit in lstSelectedUnits)
            {
                getManualGain(unit, out gain);

                if (gain != null)
                {
                    unit.ManualGain = gain;
                    lstTargetUnit.Add(unit);
                }
            }

            return true;
        }

        private void getManualGain(UnitInfo unit, out ManualGain manualGain)
        {
            string value;
            string[] aryValues = new string[moduleCount]; // 各ModuleのAdjustment level調整データを格納

            manualGain = null;

            try
            {
                Byte[] cmd = new byte[SDCPClass.CmdUnitManualAdjGet.Length];
                Array.Copy(SDCPClass.CmdUnitManualAdjGet, cmd, SDCPClass.CmdUnitManualAdjGet.Length);

                cmd[9] += (byte)(unit.PortNo << 4);
                cmd[9] += (byte)unit.UnitNo;

                sendSdcpCommand(cmd, out value, dicController[unit.ControllerID].IPAddress);

                if (value == SDCP_NAK_NOT_APPLICABLE || value == "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")
                { return; }

                manualGain = new ManualGain();

                for (int i = 0; i < moduleCount; i++)
                {
                    aryValues[i] = value.Substring(i * 12, 12); //12文字=6Byte(R/2Byte,G/2Byte,B/2Byte)

                    CellGain cell = new CellGain();
                    cell.GainR = calcComplement(aryValues[i].Substring(0, 4));
                    cell.GainG = calcComplement(aryValues[i].Substring(4, 4));
                    cell.GainB = calcComplement(aryValues[i].Substring(8, 4));

                    manualGain.lstGain.Add(cell);
                }
            }
            catch { }
        }

        private int calcComplement(string hex)
        {
            int compl = 0;
            int org = 0;

            org = Convert.ToInt32(hex, 16);

            if (org > 512)
            { compl = Convert.ToInt32("FFFF" + hex, 16); }
            else
            { compl = org; }

            return compl;
        }

        private bool checkAlreadySelected(UnitInfo unit)
        {
            foreach (UnitInfo selectedUnit in lstSelectedUnits)
            {
                if (selectedUnit.ControllerID == unit.ControllerID
                    && selectedUnit.PortNo == unit.PortNo
                    && selectedUnit.UnitNo == unit.UnitNo)
                { return true; }
            }

            return false;
        }

        private void setUfManualCorrection(bool setCorrectionMode)
        {
            byte[] getCorrectMode = new byte[SDCPClass.CmdCorrectModeGet.Length];
            Array.Copy(SDCPClass.CmdCorrectModeGet, getCorrectMode, SDCPClass.CmdCorrectModeGet.Length);

            byte[] cmd = new byte[SDCPClass.CmdCorrectModeSet.Length];
            Array.Copy(SDCPClass.CmdCorrectModeSet, cmd, SDCPClass.CmdCorrectModeSet.Length);

            foreach (ControllerInfo controller in dicController.Values)
            {
                string mode;
                sendSdcpCommand(getCorrectMode, out mode, controller.IPAddress);

                if (setCorrectionMode)
                {
                    if (mode == "00") // 目地補正:Off, 目補正:Off
                    { cmd[20] = 0x02; }
                    else if (mode == "01") // 目地補正:On, 目補正:Off
                    { cmd[20] = 0x03; }
                    else
                    { return; }
                }
                else
                {
                    if (mode == "02") // 目地補正:Off, 目補正:On
                    { cmd[20] = 0x00; }
                    else if (mode == "03") // 目地補正:On, 目補正:On
                    { cmd[20] = 0x01; }
                    else
                    { return; }
                }

                sendSdcpCommand(cmd, controller.IPAddress);
            }
        }

        #endregion
    }
}
