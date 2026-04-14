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
using System.IO;
using System.Diagnostics;
using Microsoft.VisualBasic.FileIO;

using SONY.Modules;
using MakeUFData;


namespace CAS

{
    // Relative Target
    public partial class MainWindow : Window
    {
        #region Fields

        private const int RELATIVE_TARGET_STEPS = 14;

        private ChromCustom rtTargetChrom;

        #endregion Fields

        #region Events

        private void cmbxCA410ChannelRelTgt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // CA-410 Channel
            changCAChannel(cmbxCA410ChannelRelTgt.SelectedIndex);

            cmbxCA410Channel.SelectedIndex = cmbxCA410ChannelRelTgt.SelectedIndex;
            cmbxCA410ChannelMeas.SelectedIndex = cmbxCA410ChannelRelTgt.SelectedIndex;
            //cmbxCA410ChannelCalib.SelectedIndex = cmbxCA410ChannelMeas.SelectedIndex;

            if (appliMode == ApplicationMode.Service && Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
            { cmbxCA410ChannelGain.SelectedIndex = cmbxCA410ChannelRelTgt.SelectedIndex; }
        }

        private void btnRtCellToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            //DiselectRtAllCells();

            ((UnitToggleButton)sender).Checked -= btnRtCellToggleButton_Checked;
            ((UnitToggleButton)sender).IsChecked = true;
            ((UnitToggleButton)sender).Checked += btnRtCellToggleButton_Checked;
        }

        private async void btnRelTgtStart_Click(object sender, RoutedEventArgs e)
        {
            bool status = false;
            var dispatcher = Application.Current.Dispatcher;
            string msg = "";
            string caption = "";

            tcMain.IsEnabled = false;
            actionButton(sender, "Relative Target Setting Start.");

            winProgress = new WindowProgress("Relative Target Progress");
            winProgress.ShowMessage("Relative Target Start.");
            winProgress.Show();

            // Analyzerのラジオボタンを確認
            if (rbRemoteOff.IsChecked == true)
            {
                winProgress.Close();

                msg = "Analyzer is not selected.";
                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);

                playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
                releaseButton(sender, "UF Adjust Done.");
                tcMain.IsEnabled = true;
                return;
            }

            m_MakeUFData = new CMakeUFData(cabiDx, cabiDy);

            m_lstUserSetting = null;

            try { status = await Task.Run(() => setRelativeTarget()); }
            catch (Exception ex)
            { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

            if (status == true)
            {
                msg = "Relative Target Setting Complete!";
                caption = "Complete";
            }
            else
            {
                setNormalSetting();
                setUserSetting(m_lstUserSetting);
                m_lstUserSetting = null;

                msg = "Failed in Relative Target Setting.";
                caption = "Error";
            }

            // 表示を更新
            showTargetChromaticity();

            winProgress.Close();

            playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
            WindowMessage winMessage = new WindowMessage(msg, caption);
            winMessage.ShowDialog();

            releaseButton(sender, "Relative Target Setting Done.");
            tcMain.IsEnabled = true;
        }

        // MeasurementのDrag指定用
        private void utbRelTgt_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            rectAreaRelTgt.Visibility = System.Windows.Visibility.Visible;
            rectAreaRelTgt.Margin = new Thickness(e.GetPosition(this).X - 2, e.GetPosition(this).Y - 48, 0, 0);

            startPos = new Point(rectAreaRelTgt.Margin.Left, rectAreaRelTgt.Margin.Top);
        }

        private void utbRelTgt_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            endPos = new Point(e.GetPosition(this).X - 2, e.GetPosition(this).Y - 48);

            DragArea area = new DragArea();

            if (startPos.X <= endPos.X)
            {
                area.StartX = startPos.X;
                area.EndX = endPos.X;
            }
            else
            {
                area.StartX = endPos.X;
                area.EndX = startPos.X;
            }

            if (startPos.Y <= endPos.Y)
            {
                area.StartY = startPos.Y;
                area.EndY = endPos.Y;
            }
            else
            {
                area.StartY = endPos.Y;
                area.EndY = startPos.Y;
            }

            double offsetX = tcMain.Margin.Left + gdRelatveTarget.Margin.Left + gdAllocRelativeTarget.Margin.Left + gdAllocRelativeTargetLayout.Margin.Left + cabinetAllocColumnHeaderWidth - svRelTgt.HorizontalOffset;
            double offsetY = tcMain.Margin.Top + gdRelatveTarget.Margin.Top + gdAllocRelativeTarget.Margin.Top + gdAllocRelativeTargetLayout.Margin.Top + cabinetAllocRowHeaderHeight - svRelTgt.VerticalOffset;

            for (int y = 0; y < allocInfo.MaxY; y++)
            {
                for (int x = 0; x < allocInfo.MaxX; x++)
                {
                    if (aryUnitRelTgt[x, y].IsEnabled == false)
                    { continue; }

                    if (isContainedDragArea(aryUnitRelTgt[x, y], area, offsetX, offsetY) == true)
                    {
                        if (aryUnitRelTgt[x, y].IsChecked == true)
                        { aryUnitRelTgt[x, y].IsChecked = false; }
                        else
                        { aryUnitRelTgt[x, y].IsChecked = true; }
                    }
                }
            }

            rectAreaRelTgt.Visibility = System.Windows.Visibility.Hidden;
        }

        private void utbRelTgt_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            double height = e.GetPosition(this).Y - 48 - startPos.Y;
            try
            {
                if (height >= 0)
                { rectAreaRelTgt.Height = height; }
                else
                {
                    rectAreaRelTgt.Margin = new Thickness(rectAreaRelTgt.Margin.Left, startPos.Y + height, 0, 0);
                    rectAreaRelTgt.Height = Math.Abs(height);
                }
            }
            catch { rectAreaRelTgt.Height = 0; }

            double width = e.GetPosition(this).X - 2 - startPos.X;
            try
            {
                if (width >= 0)
                { rectAreaRelTgt.Width = width; }
                else
                {
                    rectAreaRelTgt.Margin = new Thickness(startPos.X + width, rectAreaRelTgt.Margin.Top, 0, 0);
                    rectAreaRelTgt.Width = Math.Abs(width);
                }
            }
            catch { rectAreaRelTgt.Width = 0; }
        }

        #endregion Events

        #region Private Methods

        private void DiselectRtAllCells()
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Dispatcher.Invoke(new Action(() => { aryCellRelTgt[i, j].IsChecked = false; }));
                }
            }
        }

        private void showTargetChromaticity()
        {
            txbRelTgtRed_x.Text = Settings.Ins.RelativeTarget.Red.x.ToString("0.000");
            txbRelTgtRed_y.Text = Settings.Ins.RelativeTarget.Red.y.ToString("0.000");
            txbRelTgtRed_Y.Text = Settings.Ins.RelativeTarget.Red.Lv.ToString("0");
            txbRelTgtGreen_x.Text = Settings.Ins.RelativeTarget.Green.x.ToString("0.000");
            txbRelTgtGreen_y.Text = Settings.Ins.RelativeTarget.Green.y.ToString("0.000");
            txbRelTgtGreen_Y.Text = Settings.Ins.RelativeTarget.Green.Lv.ToString("0");
            txbRelTgtBlue_x.Text = Settings.Ins.RelativeTarget.Blue.x.ToString("0.000");
            txbRelTgtBlue_y.Text = Settings.Ins.RelativeTarget.Blue.y.ToString("0.000");
            txbRelTgtBlue_Y.Text = Settings.Ins.RelativeTarget.Blue.Lv.ToString("0");
            txbRelTgtWhite_x.Text = Settings.Ins.RelativeTarget.White.x.ToString("0.000");
            txbRelTgtWhite_y.Text = Settings.Ins.RelativeTarget.White.y.ToString("0.000");
            txbRelTgtWhite_Y.Text = Settings.Ins.RelativeTarget.White.Lv.ToString("0");
        }

        private bool setRelativeTarget()
        {
            if (Settings.Ins.ExecLog == true)
            {
                SaveExecLog("");
                SaveExecLog("[0] Relative Target Setting start");
            }

            var dispatcher = Application.Current.Dispatcher;

            List<UnitInfo> lstTargetUnit = new List<UnitInfo>();
            List<int> lstTargetCell = new List<int>();
            List<UserSetting> lstUserSetting;
            bool status;

            // ●進捗の最大値を設定
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[1] Set Step Count."); }
            winProgress.ShowMessage("Set Step Count.");

            int step = RELATIVE_TARGET_STEPS;
            winProgress.SetWholeSteps(step);

            // ●調整をするUnitをListに格納 [2]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[2] Store Target Cabinet(s) Info."); }
            winProgress.ShowMessage("Store Target Cabinet(s) Info.");

            dispatcher.Invoke(() => correctRelTgtUnit(out lstTargetUnit, allocInfo.MaxX, allocInfo.MaxY));
            if (lstTargetUnit.Count == 0)
            {
                ShowMessageWindow("Please select cabinets.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●対象Cabinetが接続されているController [3]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[3] Store Target Controller(s) Info."); }
            winProgress.ShowMessage("Store Target Controller(s) Info.");

            // 初期化
            foreach (ControllerInfo cont in dicController.Values)
            { cont.Target = false; }

            foreach (UnitInfo unit in lstTargetUnit)
            { dicController[unit.ControllerID].Target = true; }
            winProgress.PutForward1Step();

            // ●調整をするModuleをListに格納 [*]
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*] Store Target Module(s) Info."); }
                winProgress.ShowMessage("Store Target Module(s) Info.");

                dispatcher.Invoke(() => correctRelTgtCell(out lstTargetCell));
                if (lstTargetCell.Count == 0)
                {
                    ShowMessageWindow("Please select modules.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }
            }

#if NO_SET
#else
            // ●Power OnでないときはOnにする [4]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[4] Check Controller Power."); }
            winProgress.ShowMessage("Check Controller Power.");

            if (!setAllControllerPowerOn())
            { return false; }
            winProgress.PutForward1Step();
#endif

            // ●User設定保存 [5]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[5] Store User Settings."); }
            winProgress.ShowMessage("Store User Settings.");

            try
            {
                if (getUserSetting(out lstUserSetting) != true)
                { return false; }
                m_lstUserSetting = lstUserSetting;
            }
            catch (Exception ex)
            {
                SaveErrorLog("[getUserSetting] Source : " + ex.Source + "\r\nException Message : " + ex.Message);
                ShowMessageWindow(ex.Message, "Exception! (getUserSetting())", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }
            winProgress.PutForward1Step();

            // ●調整用設定 [6]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[6] Set Adjust Settings."); }
            winProgress.ShowMessage("Set Adjust Settings.");

            setAdjustSetting();
            winProgress.PutForward1Step();

#if NO_MEASURE
#else
            // ●Open CA-410 [7]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[7] Open CA-410."); }
            winProgress.ShowMessage("Open CA-410.");

            status = openSubDispatch(1, Settings.Ins.NoOfProbes, Settings.Ins.PortVal);
            if (status != true)
            {
                ShowMessageWindow("Can not open CA-410.\r\nPlease check it status.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●CA-410設定 [8]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[8] Set CA-410 Settings."); }
            winProgress.ShowMessage("Set CA-410 Settings.");

            if (setCA410SettingDispatch(CaSdk.DisplayMode.DispModeLvxy) != true)
            {
                ShowMessageWindow("Can not set CA-410 settings.\r\nPlease check it status.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();
#endif

            // ●Layout情報Off [9]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[9] Layout info off."); }

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdDispUnitAddrOff, 0, cont.IPAddress); }
            winProgress.PutForward1Step();

            // ●測定
            if (measureTargetUnits(lstTargetUnit, lstTargetCell) != true)
            { return false; }

            // ●調整設定解除 [13]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[13] Set Normal Setting."); }
            winProgress.ShowMessage("Set Normal Setting.");

            setNormalSetting();
            winProgress.PutForward1Step();

            // ●User設定に戻す [14]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[14] Restore User Settings."); }
            winProgress.ShowMessage("Restore User Settings.");

            setUserSetting(m_lstUserSetting);
            m_lstUserSetting = null;    //User設定を戻したので呼び出し側の設定処理をスキップ

            winProgress.PutForward1Step();

            return true;
        }

        private void correctRelTgtUnit(out List<UnitInfo> lstUnitList, int MaxX, int MaxY)
        {
            string error;
            List<UnitInfo> lstUnitTemp = new List<UnitInfo>();

            lstUnitList = new List<UnitInfo>();

            #region Size_1
            for (int x = 0; x <= MaxX - 1; x += 2)
            {
                for (int y = 0; y < MaxY; y++)
                {
                    try
                    {
                        if (aryUnitRelTgt[x, y].UnitInfo != null && aryUnitRelTgt[x, y].IsChecked == true)
                        { lstUnitList.Add(aryUnitRelTgt[x, y].UnitInfo); }
                    }
                    catch
                    {
                        error = "[correctRelTgtCabinet] (Case:1) x = " + x.ToString() + ", y = " + y.ToString()
                            + ", MaxX = " + MaxX.ToString() + ", MaxY = " + MaxY.ToString();
                        SaveErrorLog(error);
                        saveSelectionInfo();
                    }
                }

                if (x + 1 >= MaxX)
                { continue; }

                for (int y = MaxY - 1; y >= 0; y--)
                {
                    try
                    {
                        if (aryUnitRelTgt[x + 1, y].UnitInfo != null && aryUnitRelTgt[x + 1, y].IsChecked == true)
                        { lstUnitList.Add(aryUnitRelTgt[x + 1, y].UnitInfo); }
                    }
                    catch
                    {
                        error = "[correctRelTgtCabinet] (Case:2) x = " + x.ToString() + ", y = " + y.ToString()
                            + ", MaxX = " + MaxX.ToString() + ", MaxY = " + MaxY.ToString();
                        SaveErrorLog(error);
                        saveSelectionInfo();
                    }
                }
            }
            #endregion
        }

        private void correctRelTgtCell(out List<int> lstCell)
        {
            lstCell = new List<int>();

            for (int x = 0; x < 3; x++) // Moduleの縦の数
            {
                for (int y = 0; y < 4; y++) // Moduleの横の数
                {
                    if (aryCellRelTgt[y, x].IsChecked == true)
                    {
                        lstCell.Add((4 * x) + y);
                    }
                }
            }
        }

        private bool measureTargetUnits(List<UnitInfo> lstTargetUnit, List<int> lstTargetCell)
        {
            int measuredCount = 0;

            List<TargetUnitColor> lstUnitColor = new List<TargetUnitColor>();
            List<List<Chromaticity>> lstTargetUnitAvgChrom = new List<List<Chromaticity>>();
            List<List<List<Chromaticity>>> lstAllUnitChrom = new List<List<List<Chromaticity>>>();

            // ●Unit測定[10]
#if NO_MEASURE
            string path = @"C:\CAS\Measurement\20170327_DEBUG_D93_UnitUniformity.xml";
            loadMeasuredData(path, out lstUnitColor);
#else
            foreach (UnitInfo unit in lstTargetUnit)
            {
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*1] (Cabinet Loop) PortNo. : " + unit.PortNo.ToString() + ", CabinetNo. : " + unit.UnitNo.ToString()); }

                // ●色度測定 [*1]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*2] Measure Cabinet Color."); }
                winProgress.ShowMessage("Measure Cabinet Color." + "    (Cabinets: " + (++measuredCount) + "/" + lstTargetUnit.Count + ")");

                if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
                {
                    if (measure4PointsRelTgt(unit, out TargetUnitColor unitColor))
                    { lstUnitColor.Add(unitColor); }
                    else
                    { return false; }
                }
                else
                {
                    if (measureCellPointsRelTgt(unit, lstTargetCell, out lstTargetUnitAvgChrom))
                    { lstAllUnitChrom.Add(lstTargetUnitAvgChrom); }
                    else
                    { return false; }
                }
            }
            winProgress.PutForward1Step();

            // ●TestPattern OFF [11]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[11] Test Pattern Off."); }
            winProgress.ShowMessage("Test Pattern Off.");

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdIntSignalOff, 0, cont.IPAddress); }
            winProgress.PutForward1Step();

            // ●測定データからRelative Targetを算出、設定 [12]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[12] Save Relative Target."); }
            winProgress.ShowMessage("Save Relative Target.");

            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            { calcTargetChrom(lstUnitColor); }
            else
            { calcNewTargetChrom(lstAllUnitChrom); }
            winProgress.PutForward1Step();
#endif

            return true;
        }

        private bool measure4PointsRelTgt(UnitInfo unit, out TargetUnitColor unitColor)
        {
            bool status;
            List<Chromaticity> lstColor;

            unitColor = new TargetUnitColor();
            unitColor.UnitName = "C" + unit.ControllerID + "-" + unit.PortNo + "-" + unit.UnitNo;

            // Top
            status = measurePointRelTgt(unit, CrossPointPosition.Top, out lstColor);
            if (status != true)
            { return false; }

            unitColor.lstTop = lstColor;

            // Right
            status = measurePointRelTgt(unit, CrossPointPosition.Right, out lstColor);
            if (status != true)
            { return false; }

            unitColor.lstRight = lstColor;

            // Bottom
            status = measurePointRelTgt(unit, CrossPointPosition.Bottom, out lstColor);
            if (status != true)
            { return false; }

            unitColor.lstBottom = lstColor;

            // Left
            status = measurePointRelTgt(unit, CrossPointPosition.Left, out lstColor);
            if (status != true)
            { return false; }

            unitColor.lstLeft = lstColor;

            return true;
        }

        /// <summary>
        /// 対象Cabinetの対象モジュールの色度を測定する
        /// </summary>
        /// <param name="unit">測定対象キャビネット</param>
        /// <param name="lstTargetCell">測定対象モジュール番号が格納されたリスト</param>
        /// <param name="lstTargetUnit">キャビネットで測定したr/g/b/wのx,y,Lv測定値を格納するリスト</param>
        /// <returns></returns>
        private bool measureCellPointsRelTgt(UnitInfo unit, List<int> lstTargetCell, out List<List<Chromaticity>> lstTargetUnit)
        {
            lstTargetUnit = new List<List<Chromaticity>>();
            for (int i = 0; i < lstTargetCell.Count; i++)
            {
                if (measurePointRelTgt(unit, (CrossPointPosition)(lstTargetCell[i] + 18), out List<Chromaticity> lstTargetUnitAvgChrom))
                { lstTargetUnit.Add(lstTargetUnitAvgChrom); }
                else
                { return false; }
            }

            return true;
        }

        private bool measurePointRelTgt(UnitInfo unit, CrossPointPosition point, out List<Chromaticity> lstColor)
        {
            bool status;
            double[] measureData;
            Chromaticity tempColor;
            UserOperation operation = UserOperation.None;

            lstColor = new List<Chromaticity>();

            // 測定ポイントの十字を表示
            outputCross(unit, point);

            // 測定画面表示
            status = Dispatcher.Invoke(() => showMeasureWindow());
            if (status != true)
            { return false; }

#if DEBUG
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            {
                string cpp;
                if (point == CrossPointPosition.Top)
                { cpp = "Top"; }
                else if (point == CrossPointPosition.Right)
                { cpp = "Right"; }
                else if (point == CrossPointPosition.Bottom)
                { cpp = "Bottom"; }
                else if (point == CrossPointPosition.Left)
                { cpp = "Left"; }
                else
                { cpp = string.Empty; }

                Console.WriteLine($"Target Cabinet: C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}\r\nCrossPointPosition: {cpp}");
                SaveExecLog($"     Target Cabinet: C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}  CrossPointPosition: {cpp}");
            }
            else
            {
                Console.WriteLine($"C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}\r\nModule No.{(int)point - 17}");
                SaveExecLog($"     Target Cabinet: C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}  Module No.{(int)point - 17}");
            }
#elif Release_log
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            {
                string cpp;
                if (point == CrossPointPosition.Top)
                { cpp = "Top"; }
                else if (point == CrossPointPosition.Right)
                { cpp = "Right"; }
                else if (point == CrossPointPosition.Bottom)
                { cpp = "Bottom"; }
                else if (point == CrossPointPosition.Left)
                { cpp = "Left"; }
                else
                { cpp = string.Empty; }

                SaveExecLog($"     C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}  CrossPointPosition: {cpp}");
            }
            else
            { SaveExecLog($"     C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}  Module No.{(int)point - 17}"); }
#endif

            // Red
            outputWindow(unit, point, CellColor.Red);
            System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait); // 画が表示されるまでWait

            status = measureColor(null, out measureData, CaSdk.DisplayMode.DispModeLvxy, ref operation);
            if (status != true)
            { return false; }

            tempColor = new Chromaticity();
            tempColor.x = measureData[0];
            tempColor.y = measureData[1];
            tempColor.Lv = measureData[2];
            lstColor.Add(tempColor);

#if DEBUG
            Console.WriteLine($"R(x): {measureData[0]}\r\nR(y): {measureData[1]}\r\nR(Lv): {measureData[2]}");
            SaveExecLog($"      R(x): {measureData[0]}");
            SaveExecLog($"      R(y): {measureData[1]}");
            SaveExecLog($"      R(Lv): {measureData[2]}");
#elif Release_log
            SaveExecLog($"      R(x): {measureData[0]}");
            SaveExecLog($"      R(y): {measureData[1]}");
            SaveExecLog($"      R(Lv): {measureData[2]}");
#endif

            playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

            // Green
            outputWindow(unit, point, CellColor.Green);
            System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

            status = measureColor(null, out measureData, CaSdk.DisplayMode.DispModeLvxy, ref operation);
            if (status != true)
            { return false; }

            tempColor = new Chromaticity();
            tempColor.x = measureData[0];
            tempColor.y = measureData[1];
            tempColor.Lv = measureData[2];
            lstColor.Add(tempColor);

#if DEBUG
            Console.WriteLine($"G(x): {measureData[0]}\r\nG(y): {measureData[1]}\r\nG(Lv): {measureData[2]}");
            SaveExecLog($"      G(x): {measureData[0]}");
            SaveExecLog($"      G(y): {measureData[1]}");
            SaveExecLog($"      G(Lv): {measureData[2]}");
#elif Release_log
            SaveExecLog($"      G(x): {measureData[0]}");
            SaveExecLog($"      G(y): {measureData[1]}");
            SaveExecLog($"      G(Lv): {measureData[2]}");
#endif

            playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

            // Blue
            outputWindow(unit, point, CellColor.Blue);
            System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

            status = measureColor(null, out measureData, CaSdk.DisplayMode.DispModeLvxy, ref operation);
            if (status != true)
            { return false; }

            tempColor = new Chromaticity();
            tempColor.x = measureData[0];
            tempColor.y = measureData[1];
            tempColor.Lv = measureData[2];
            lstColor.Add(tempColor);

#if DEBUG
            Console.WriteLine($"B(x): {measureData[0]}\r\nB(y): {measureData[1]}\r\nB(Lv): {measureData[2]}");
            SaveExecLog($"      B(x): {measureData[0]}");
            SaveExecLog($"      B(y): {measureData[1]}");
            SaveExecLog($"      B(Lv): {measureData[2]}");
#elif Release_log
            SaveExecLog($"      B(x): {measureData[0]}");
            SaveExecLog($"      B(y): {measureData[1]}");
            SaveExecLog($"      B(Lv): {measureData[2]}");
#endif

            playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

            // White
            outputWindow(unit, point, CellColor.White);
            System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

            status = measureColor(null, out measureData, CaSdk.DisplayMode.DispModeLvxy, ref operation);
            if (status != true)
            { return false; }

            tempColor = new Chromaticity();
            tempColor.x = measureData[0];
            tempColor.y = measureData[1];
            tempColor.Lv = measureData[2];
            lstColor.Add(tempColor);

#if DEBUG
            Console.WriteLine($"W(x): {measureData[0]}\r\nW(y): {measureData[1]}\r\nW(Lv): {measureData[2]}");
            SaveExecLog($"      W(x): {measureData[0]}");
            SaveExecLog($"      W(y): {measureData[1]}");
            SaveExecLog($"      W(Lv): {measureData[2]}");
#elif Release_log
            SaveExecLog($"      W(x): {measureData[0]}");
            SaveExecLog($"      W(y): {measureData[1]}");
            SaveExecLog($"      W(Lv): {measureData[2]}");
#endif

            playSound(applicationPath + "\\Components\\Sound\\button01b.mp3");

            return true;
        }

        private bool calcTargetChrom(List<TargetUnitColor> lstUnitColor)
        {
            List<ChromCustom> lstUnitChrom = new List<ChromCustom>(); // Unitごとの平均色度（Target）
            ChromCustom targetChrom = new ChromCustom(ColorPurpose.ZRD_C12A);

            // Unitごとに算出
            foreach (TargetUnitColor target in lstUnitColor)
            {
                ChromCustom unitTgt = new ChromCustom(false);

                unitTgt.Red.x = (target.lstTop[0].x + target.lstBottom[0].x + target.lstLeft[0].x + target.lstRight[0].x) / 4;
                unitTgt.Red.y = (target.lstTop[0].y + target.lstBottom[0].y + target.lstLeft[0].y + target.lstRight[0].y) / 4;
                unitTgt.Red.Lv = (target.lstTop[0].Lv + target.lstBottom[0].Lv + target.lstLeft[0].Lv + target.lstRight[0].Lv) / 4;

                unitTgt.Green.x = (target.lstTop[1].x + target.lstBottom[1].x + target.lstLeft[1].x + target.lstRight[1].x) / 4;
                unitTgt.Green.y = (target.lstTop[1].y + target.lstBottom[1].y + target.lstLeft[1].y + target.lstRight[1].y) / 4;
                unitTgt.Green.Lv = (target.lstTop[1].Lv + target.lstBottom[1].Lv + target.lstLeft[1].Lv + target.lstRight[1].Lv) / 4;

                unitTgt.Blue.x = (target.lstTop[2].x + target.lstBottom[2].x + target.lstLeft[2].x + target.lstRight[2].x) / 4;
                unitTgt.Blue.y = (target.lstTop[2].y + target.lstBottom[2].y + target.lstLeft[2].y + target.lstRight[2].y) / 4;
                unitTgt.Blue.Lv = (target.lstTop[2].Lv + target.lstBottom[2].Lv + target.lstLeft[2].Lv + target.lstRight[2].Lv) / 4;

                unitTgt.White.x = (target.lstTop[3].x + target.lstBottom[3].x + target.lstLeft[3].x + target.lstRight[3].x) / 4;
                unitTgt.White.y = (target.lstTop[3].y + target.lstBottom[3].y + target.lstLeft[3].y + target.lstRight[3].y) / 4;
                unitTgt.White.Lv = (target.lstTop[3].Lv + target.lstBottom[3].Lv + target.lstLeft[3].Lv + target.lstRight[3].Lv) / 4;

                lstUnitChrom.Add(unitTgt);
            }

            // 平均値を算出
            ChromCustom aveChrom = new ChromCustom(false);

            foreach (ChromCustom chrom in lstUnitChrom)
            {
                aveChrom.Red.x += chrom.Red.x;
                aveChrom.Red.y += chrom.Red.y;
                aveChrom.Red.Lv += chrom.Red.Lv;
                aveChrom.Green.x += chrom.Green.x;
                aveChrom.Green.y += chrom.Green.y;
                aveChrom.Green.Lv += chrom.Green.Lv;
                aveChrom.Blue.x += chrom.Blue.x;
                aveChrom.Blue.y += chrom.Blue.y;
                aveChrom.Blue.Lv += chrom.Blue.Lv;
                aveChrom.White.x += chrom.White.x;
                aveChrom.White.y += chrom.White.y;
                aveChrom.White.Lv += chrom.White.Lv;
            }

            targetChrom.Red.x = aveChrom.Red.x / lstUnitChrom.Count;
            targetChrom.Red.y = aveChrom.Red.y / lstUnitChrom.Count;
            targetChrom.Red.Lv = aveChrom.Red.Lv / lstUnitChrom.Count;
            targetChrom.Green.x = aveChrom.Green.x / lstUnitChrom.Count;
            targetChrom.Green.y = aveChrom.Green.y / lstUnitChrom.Count;
            targetChrom.Green.Lv = aveChrom.Green.Lv / lstUnitChrom.Count;
            targetChrom.Blue.x = aveChrom.Blue.x / lstUnitChrom.Count;
            targetChrom.Blue.y = aveChrom.Blue.y / lstUnitChrom.Count;
            targetChrom.Blue.Lv = aveChrom.Blue.Lv / lstUnitChrom.Count;
            targetChrom.White.x = aveChrom.White.x / lstUnitChrom.Count;
            targetChrom.White.y = aveChrom.White.y / lstUnitChrom.Count;
            targetChrom.White.Lv = aveChrom.White.Lv / lstUnitChrom.Count;

            // 設定ファイルに保存
            Settings.Ins.RelativeTarget = targetChrom;
            Settings.Ins.Uniformity.IsRelativeTarget = true;

            // ファイルに保存
            Settings.SaveToXmlFile();

            return true;
        }

        /// <summary>
        /// 全体Cabinetから平均値算出後、Relative Target値を算出しファイルに保存
        /// </summary>
        /// <param name="lstAllUnitColor">全対象Cabinetのr/g/b/wのx,y,Lv測定したデータが格納されたリスト</param>
        /// <returns></returns>
        private bool calcNewTargetChrom(List<List<List<Chromaticity>>> lstAllUnitColor)
        {
            List<List<Chromaticity>> lstUnitColor = new List<List<Chromaticity>>();
            List<Chromaticity> avgColor = new List<Chromaticity>();

#if DEBUG
            Console.WriteLine($"Average value of the target modules in one cabinet:");
#elif Release_log
            SaveExecLog($"     Average value of the target modules in one cabinet:");
#endif
            foreach (List<List<Chromaticity>> unitColor in lstAllUnitColor)
            {
                try
                {
                    if (calcAvgTargetColor(unitColor, out List<Chromaticity> lstUnitAvgColor)) // lstUnitAvgChrom: 1Unitの平均値
                    { lstUnitColor.Add(lstUnitAvgColor); }
                }
                catch (Exception ex)
                {
                    SaveErrorLog($"[calcAvgTargetColor] Source : {ex.Source}\r\nException Message : {ex.Message}");
                    ShowMessageWindow($"Failed in measure data average calc.\r\n{ex.Message}", "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                    return false;
                }
            }

            if (lstUnitColor.Count > 1) // 複数Cabinet選択時
            {
#if DEBUG
                Console.WriteLine($"Average value of the all cabinet:");
#elif Release_log
                SaveExecLog($"     Average value of the all cabinet:");
#endif

                try
                { calcAvgTargetColor(lstUnitColor, out avgColor); } // avgChrom: All Unitの平均値
                catch (Exception ex)
                {
                    SaveErrorLog($"[calcAvgTargetColor] Source : {ex.Source}\r\nException Message : {ex.Message}");
                    ShowMessageWindow($"Failed in measure data average calc.\r\n{ex.Message}", "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                    return false;
                }
            }
            else
            { avgColor = lstUnitColor[0]; }

            // 最終値を算出
            try
            { calcRelativeTarget(avgColor); }
            catch (Exception ex)
            {
                SaveErrorLog($"[calcRelativeTarget] Source : {ex.Source}\r\nException Message : {ex.Message}");
                ShowMessageWindow($"Failed in measure data average calc.\r\n{ex.Message}", "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }

            // 設定ファイルに保存
            Settings.Ins.RelativeTarget = rtTargetChrom;
            Settings.Ins.Uniformity.IsRelativeTarget = true;

            // ファイルに保存
            Settings.SaveToXmlFile();

            return true;
        }

        /// <summary>
        /// 平均値を算出
        /// </summary>
        /// <param name="lstTarget">Module/Cabinetのr/g/b/wのx,y,Lv測定データが格納されたリスト</param>
        /// <param name="avgChrom">Module/Cabinetのr/g/b/wのx,y,Lv測定データから算出した平均値を格納するリスト</param>
        /// <returns></returns>
        private bool calcAvgTargetColor(List<List<Chromaticity>> lstTarget, out List<Chromaticity> avgChrom)
        {
            avgChrom = new List<Chromaticity>();
            for (int i = 0; i < 4; i++)
            { avgChrom.Add(new Chromaticity()); }

            foreach (List<Chromaticity> target in lstTarget)
            {
                avgChrom[RED].x += target[RED].x;
                avgChrom[RED].y += target[RED].y;
                avgChrom[RED].Lv += target[RED].Lv;

                avgChrom[GREEN].x += target[GREEN].x;
                avgChrom[GREEN].y += target[GREEN].y;
                avgChrom[GREEN].Lv += target[GREEN].Lv;

                avgChrom[BLUE].x += target[BLUE].x;
                avgChrom[BLUE].y += target[BLUE].y;
                avgChrom[BLUE].Lv += target[BLUE].Lv;

                avgChrom[WHITE].x += target[WHITE].x;
                avgChrom[WHITE].y += target[WHITE].y;
                avgChrom[WHITE].Lv += target[WHITE].Lv;
            }

            avgChrom[RED].x = avgChrom[RED].x / lstTarget.Count;
            avgChrom[RED].y = avgChrom[RED].y / lstTarget.Count;
            avgChrom[RED].Lv = avgChrom[RED].Lv / lstTarget.Count;

#if DEBUG
            Console.WriteLine($"R(x): {avgChrom[RED].x}\r\nR(y): {avgChrom[RED].y}\r\nR(Lv): {avgChrom[RED].Lv}");
            SaveExecLog($"      R(x): {avgChrom[RED].x}");
            SaveExecLog($"      R(y): {avgChrom[RED].y}");
            SaveExecLog($"      R(Lv): {avgChrom[RED].Lv}");
#elif Release_log
            SaveExecLog($"      R(x): {avgChrom[RED].x}");
            SaveExecLog($"      R(y): {avgChrom[RED].y}");
            SaveExecLog($"      R(Lv): {avgChrom[RED].Lv}");
#endif

            avgChrom[GREEN].x = avgChrom[GREEN].x / lstTarget.Count;
            avgChrom[GREEN].y = avgChrom[GREEN].y / lstTarget.Count;
            avgChrom[GREEN].Lv = avgChrom[GREEN].Lv / lstTarget.Count;

#if DEBUG
            Console.WriteLine($"G(x): {avgChrom[GREEN].x}\r\nG(y): {avgChrom[GREEN].y}\r\nG(Lv): {avgChrom[GREEN].Lv}");
            SaveExecLog($"      G(x): {avgChrom[GREEN].x}");
            SaveExecLog($"      G(y): {avgChrom[GREEN].y}");
            SaveExecLog($"      G(Lv): {avgChrom[GREEN].Lv}");
#elif Release_log
            SaveExecLog($"      G(x): {avgChrom[GREEN].x}");
            SaveExecLog($"      G(y): {avgChrom[GREEN].y}");
            SaveExecLog($"      G(Lv): {avgChrom[GREEN].Lv}");
#endif

            avgChrom[BLUE].x = avgChrom[BLUE].x / lstTarget.Count;
            avgChrom[BLUE].y = avgChrom[BLUE].y / lstTarget.Count;
            avgChrom[BLUE].Lv = avgChrom[BLUE].Lv / lstTarget.Count;

#if DEBUG
            Console.WriteLine($"B(x): {avgChrom[BLUE].x}\r\nB(y): {avgChrom[BLUE].y}\r\nB(Lv): {avgChrom[BLUE].Lv}");
            SaveExecLog($"      B(x): {avgChrom[BLUE].x}");
            SaveExecLog($"      B(y): {avgChrom[BLUE].y}");
            SaveExecLog($"      B(Lv): {avgChrom[BLUE].Lv}");
#elif Release_log
            SaveExecLog($"      B(x): {avgChrom[BLUE].x}");
            SaveExecLog($"      B(y): {avgChrom[BLUE].y}");
            SaveExecLog($"      B(Lv): {avgChrom[BLUE].Lv}");
#endif

            avgChrom[WHITE].x = avgChrom[WHITE].x / lstTarget.Count;
            avgChrom[WHITE].y = avgChrom[WHITE].y / lstTarget.Count;
            avgChrom[WHITE].Lv = avgChrom[WHITE].Lv / lstTarget.Count;

#if DEBUG
            Console.WriteLine($"W(x): {avgChrom[WHITE].x}\r\nW(y): {avgChrom[WHITE].y}\r\nW(Lv): {avgChrom[WHITE].Lv}");
            SaveExecLog($"      W(x): {avgChrom[WHITE].x}");
            SaveExecLog($"      W(y): {avgChrom[WHITE].y}");
            SaveExecLog($"      W(Lv): {avgChrom[WHITE].Lv}");
#elif Release_log
            SaveExecLog($"      W(x): {avgChrom[WHITE].x}");
            SaveExecLog($"      W(y): {avgChrom[WHITE].y}");
            SaveExecLog($"      W(Lv): {avgChrom[WHITE].Lv}");
#endif

            return true;
        }

        /// <summary>
        /// Relative Target値を保存
        /// </summary>
        /// <param name="chrom">r/g/b/wのx,y,Lv値を格納されたリスト</param>
        /// <returns></returns>
        private bool calcRelativeTarget(List<Chromaticity> chrom)
        {
            double xr = chrom[RED].x;
            double yr = chrom[RED].y;
            double Yr = chrom[RED].Lv;

            double xg = chrom[GREEN].x;
            double yg = chrom[GREEN].y;
            double Yg = chrom[GREEN].Lv;

            double xb = chrom[BLUE].x;
            double yb = chrom[BLUE].y;
            double Yb = chrom[BLUE].Lv;

            double xw = chrom[WHITE].x;
            double yw = chrom[WHITE].y;
            double Yw = chrom[WHITE].Lv;

            // ターゲット (xr,yr), (xg,yg), (xb,yb) および (xw,yw,Yw) から Yr, Yg, Ybを算出
            //m_MakeUFData.CalcWhiteXYZ_To_RGBXYZ(xr, yr, xg, yg, xb, yb, xw, yw, Yw, out _, out Yr, out _, out _, out Yg, out _, out _, out Yb, out _);

            setRtChromTarget();

            rtTargetChrom.Red.x = Math.Round(xr, 3);
            rtTargetChrom.Red.y = Math.Round(yr, 3);
            rtTargetChrom.Red.Lv = Math.Round(Yr, 4);

#if DEBUG
            Console.WriteLine($"Relative Target Value:\r\nR(x): {rtTargetChrom.Red.x}\r\nR(y): {rtTargetChrom.Red.y}\r\nR(Lv): {rtTargetChrom.Red.Lv}");
            SaveExecLog($"      R(x): {rtTargetChrom.Red.x}");
            SaveExecLog($"      R(y): {rtTargetChrom.Red.y}");
            SaveExecLog($"      R(Lv): {rtTargetChrom.Red.Lv}");
#elif Release_log
            SaveExecLog($"     Relative Target Value:");
            SaveExecLog($"      R(x): {rtTargetChrom.Red.x}");
            SaveExecLog($"      R(y): {rtTargetChrom.Red.y}");
            SaveExecLog($"      R(Lv): {rtTargetChrom.Red.Lv}");
#endif

            rtTargetChrom.Green.x = Math.Round(xg, 3);
            rtTargetChrom.Green.y = Math.Round(yg, 3);
            rtTargetChrom.Green.Lv = Math.Round(Yg, 4);

#if DEBUG
            Console.WriteLine($"G(x): {rtTargetChrom.Green.x}\r\nG(y): {rtTargetChrom.Green.y}\r\nG(Lv): {rtTargetChrom.Green.Lv}");
            SaveExecLog($"      G(x): {rtTargetChrom.Green.x}");
            SaveExecLog($"      G(y): {rtTargetChrom.Green.y}");
            SaveExecLog($"      G(Lv): {rtTargetChrom.Green.Lv}");
#elif Release_log
            SaveExecLog($"      G(x): {rtTargetChrom.Green.x}");
            SaveExecLog($"      G(y): {rtTargetChrom.Green.y}");
            SaveExecLog($"      G(Lv): {rtTargetChrom.Green.Lv}");
#endif

            rtTargetChrom.Blue.x = Math.Round(xb, 3);
            rtTargetChrom.Blue.y = Math.Round(yb, 3);
            rtTargetChrom.Blue.Lv = Math.Round(Yb, 4);

#if DEBUG
            Console.WriteLine($"B(x): {rtTargetChrom.Blue.x}\r\nB(y): {rtTargetChrom.Blue.y}\r\nB(Lv): {rtTargetChrom.Blue.Lv}");
            SaveExecLog($"      B(x): {rtTargetChrom.Blue.x}");
            SaveExecLog($"      B(y): {rtTargetChrom.Blue.y}");
            SaveExecLog($"      B(Lv): {rtTargetChrom.Blue.Lv}");
#elif Release_log
            SaveExecLog($"      B(x): {rtTargetChrom.Blue.x}");
            SaveExecLog($"      B(y): {rtTargetChrom.Blue.y}");
            SaveExecLog($"      B(Lv): {rtTargetChrom.Blue.Lv}");
#endif

            rtTargetChrom.White.x = Math.Round(xw, 3);
            rtTargetChrom.White.y = Math.Round(yw, 3);
            rtTargetChrom.White.Lv = Math.Round(Yw, 4);

#if DEBUG
            Console.WriteLine($"W(x): {rtTargetChrom.White.x}\r\nW(y): {rtTargetChrom.White.y}\r\nW(Lv): {rtTargetChrom.White.Lv}");
            SaveExecLog($"      W(x): {rtTargetChrom.White.x}");
            SaveExecLog($"      W(y): {rtTargetChrom.White.y}");
            SaveExecLog($"      W(Lv): {rtTargetChrom.White.Lv}");
#elif Release_log
            SaveExecLog($"      W(x): {rtTargetChrom.White.x}");
            SaveExecLog($"      W(y): {rtTargetChrom.White.y}");
            SaveExecLog($"      W(Lv): {rtTargetChrom.White.Lv}");
#endif

            return true;
        }

        /// <summary>
        /// キャビネットのChromaticityを設定
        /// </summary>
        private void setRtChromTarget()
        {
            switch (Settings.Ins.ConfigChromType)
            {
                case ConfigChrom.ZRD_BH12D:
                    rtTargetChrom = new ChromCustom(ColorPurpose.ZRD_BH12D);
                    break;
                case ConfigChrom.ZRD_BH15D:
                    rtTargetChrom = new ChromCustom(ColorPurpose.ZRD_BH15D);
                    break;
                case ConfigChrom.ZRD_CH12D:
                    rtTargetChrom = new ChromCustom(ConfigChrom.ZRD_CH12D);
                    break;
                case ConfigChrom.ZRD_CH15D:
                    rtTargetChrom = new ChromCustom(ConfigChrom.ZRD_CH15D);
                    break;
                case ConfigChrom.ZRD_BH12D_S3:
                    rtTargetChrom = new ChromCustom(ColorPurpose.ZRD_BH12D_S3);
                    break;
                case ConfigChrom.ZRD_BH15D_S3:
                    rtTargetChrom = new ChromCustom(ColorPurpose.ZRD_BH15D_S3);
                    break;
                case ConfigChrom.ZRD_CH12D_S3:
                    rtTargetChrom = new ChromCustom(ConfigChrom.ZRD_CH12D_S3);
                    break;
                case ConfigChrom.ZRD_CH15D_S3:
                    rtTargetChrom = new ChromCustom(ConfigChrom.ZRD_CH15D_S3);
                    break;
                default:
                    // Do nothing
                    break;
            }
        }

        #endregion Private Methods
    }
}
