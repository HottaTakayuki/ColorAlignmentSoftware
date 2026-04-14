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
    using CabinetOffset = System.Drawing.Point;
    using WidthOffset = System.Drawing.Point;

    // Gap Correct Cell
    public partial class MainWindow : Window
    {
        private bool gapCorrectModuleTabFirstSelect = true;

        private readonly int moduleGapStep = 6;
        private readonly int moduleGapBase = 200; // = 128 + moduleGapStep * 12;
        private int cursorGapCellUnitX = 0;
        private int cursorGapCellUnitY = 0;
        private int cursorGapCellCellX = 0;
        private int cursorGapCellCellY = 0;

        private const int cursorGapCellUnitX_Min = 0;
        private const int cursorGapCellUnitY_Min = 0;
        private const int cursorGapCellCellX_Min = 0;
        private const int cursorGapCellCellX_Max = 3;
        private const int cursorGapCellCellY_Min = 0;

        private const int correctValue_Min = 56;
        private const int correctValue_Max = 200;
        
        private const int NO_1  = 1;
        private const int NO_2  = 2;
        private const int NO_3  = 3;
        private const int NO_4  = 4;
        private const int NO_5  = 5;
        private const int NO_6  = 6;
        private const int NO_7  = 7;
        private const int NO_8  = 8;
        private const int NO_9  = 9;
        private const int NO_10 = 10;
        private const int NO_11 = 11;
        private const int NO_12 = 12;

        //private bool gapCorrectCellTabFirstSelect = true;
        private List<UnitInfo> lstModifiedUnits = new List<UnitInfo>();
        private bool isGcSelectBtnSelected = false;

        private const int TabIndex_GapCell = 5;//Gap Correction(Module)

        private const int inside_PartCount_Module4x2 = 40; // 1CabinetにおけるCabinet内の調整箇所数
        private const int inside_PartCount_Module4x3 = 68;
        private const int outside_PartCount_Module4x2 = 24; // 1CabinetにおけるCabinet間の調整箇所数
        private const int outside_PartCount_Module4x3 = 28;

        #region Events

        #region Mode

        private void rbGapCorrectModuleOn_Checked(object sender, RoutedEventArgs e)
        {
            gdGapCorrectModuleMode.IsEnabled = false;
            actionButton(sender, "Gap Correct On");

            setGapCorrection(true);

            // Gap Correct画面のModeラジオボタンを同期化
            rbGapCorrectOn.Checked -= rbGapCorrectOn_Checked;
            rbGapCorrectOn.IsChecked = true;
            rbGapCorrectOn.Checked += rbGapCorrectOn_Checked;

            releaseButton(sender);
            gdGapCorrectModuleMode.IsEnabled = true;
        }

        private void rbGapCorrectModuleOff_Checked(object sender, RoutedEventArgs e)
        {
            gdGapCorrectModuleMode.IsEnabled = false;
            actionButton(sender, "Gap Correct Off");

            setGapCorrection(false);

            // Gap Correct画面のModeラジオボタンを同期化
            rbGapCorrectOff.Checked -= rbGapCorrectOff_Checked;
            rbGapCorrectOff.IsChecked = true;
            rbGapCorrectOff.Checked += rbGapCorrectOff_Checked;

            releaseButton(sender);
            gdGapCorrectModuleMode.IsEnabled = true;
        }

        #endregion Mode

        #region Cursor

        private void tbtnCursorGapCell_Checked(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Cursor On");

            isGcSelectBtnSelected = false;
            outputCursorGapCell();
            gdGapCorrectCellCursor.IsEnabled = true;

            if (rbCursorUnitGapCell.IsChecked == true)
            { btnSelectGapCell.IsEnabled = false; }
            else
            { btnSelectGapCell.IsEnabled = true; }

            gdGapCellCorrectValue.IsEnabled = false;

            releaseButton(sender);
        }

        private void tbtnCursorGapCell_Unchecked(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Cursor Off");

            if (cmbxPatternGapCell.SelectedIndex == -1)
            { cmbxPatternGapCell.SelectedIndex = 0; }

            cmbxPatternUfManual_DropDownClosed(sender, e);
            gdGapCorrectCellCursor.IsEnabled = false;

            releaseButton(sender);
        }

        private void rbCursorUnitGapCell_Checked(object sender, RoutedEventArgs e)
        {
            if (gamePadViewModel.CursorGapCell && TabIndex_GapCell == gamePadViewModel.TabSelectIndex)
            {
                tbtnCursorGapCell_Checked(sender, e);
                cursorGapCellCellX = 0;
                cursorGapCellCellY = 0;
                btnSelectGapCell.IsEnabled = false;
            }
        }

        private void rbCursorCellGapCell_Checked(object sender, RoutedEventArgs e)
        {
            if (gamePadViewModel.CursorGapCell && TabIndex_GapCell == gamePadViewModel.TabSelectIndex)
            {
                tbtnCursorGapCell_Checked(sender, e);
                btnSelectGapCell.IsEnabled = true;
            }
        }
        
        private void btnCursorUpGapCell_Click(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Move Cursor");

            // Unit
            if (rbCursorUnitGapCell.IsChecked == true)
            {
                cursorGapCellUnitY--;
                if (cursorGapCellUnitY < 0)
                { cursorGapCellUnitY = 0; }
            }
            // Cell
            else
            {
                cursorGapCellCellY--;
                if (cursorGapCellCellY < cursorGapCellCellY_Min)
                {
                    cursorGapCellUnitY--;
                    if (cursorGapCellUnitY >= cursorGapCellUnitY_Min)
                    { cursorGapCellCellY = cursorGapCellCellY_Max; }
                    else
                    {
                        cursorGapCellUnitY = cursorGapCellUnitY_Min;
                        cursorGapCellCellY = cursorGapCellCellY_Min;
                    }
                }
            }

            outputCursorGapCell();

            releaseButton(sender);
        }

        private void btnCursorLeftGapCell_Click(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Move Cursor");

            // Unit
            if (rbCursorUnitGapCell.IsChecked == true)
            {
                cursorGapCellUnitX--;
                if (cursorGapCellUnitX < 0)
                { cursorGapCellUnitX = 0; }
            }
            // Cell
            else
            {
                cursorGapCellCellX--;
                if (cursorGapCellCellX < cursorGapCellCellX_Min)
                {
                    cursorGapCellUnitX--;
                    if (cursorGapCellUnitX >= cursorGapCellUnitX_Min)
                    { cursorGapCellCellX = cursorGapCellCellX_Max; }
                    else
                    {
                        cursorGapCellUnitX = cursorGapCellUnitX_Min;
                        cursorGapCellCellX = cursorGapCellCellX_Min;
                    }
                }
            }

            outputCursorGapCell();

            releaseButton(sender);
        }

        private void btnCursorRightGapCell_Click(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Move Cursor");

            // Unit
            if (rbCursorUnitGapCell.IsChecked == true)
            {
                cursorGapCellUnitX++;
                if (cursorGapCellUnitX >= allocInfo.MaxX)
                { cursorGapCellUnitX = allocInfo.MaxX - 1; }
            }
            // Cell
            else
            {
                cursorGapCellCellX++;
                if (cursorGapCellCellX > cursorGapCellCellX_Max)
                {
                    cursorGapCellUnitX++;
                    if (cursorGapCellUnitX >= allocInfo.MaxX)
                    {
                        cursorGapCellUnitX = allocInfo.MaxX - 1;
                        cursorGapCellCellX = cursorGapCellCellX_Max;
                    }
                    else
                    { cursorGapCellCellX = cursorGapCellCellX_Min; }
                }
            }

            outputCursorGapCell();

            releaseButton(sender);
        }

        private void btnCursorDownGapCell_Click(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Move Cursor");

            // Unit
            if (rbCursorUnitGapCell.IsChecked == true)
            {
                cursorGapCellUnitY++;
                if (cursorGapCellUnitY >= allocInfo.MaxY)
                { cursorGapCellUnitY = allocInfo.MaxY - 1; }
            }
            // Cell
            else
            {
                cursorGapCellCellY++;
                if (cursorGapCellCellY > cursorGapCellCellY_Max)
                {
                    cursorGapCellUnitY++;
                    if (cursorGapCellUnitY >= allocInfo.MaxY)
                    {
                        cursorGapCellUnitY = allocInfo.MaxY - 1;
                        cursorGapCellCellY = cursorGapCellCellY_Max;
                    }
                    else
                    { cursorGapCellCellY = cursorGapCellCellY_Min; }
                }
            }

            outputCursorGapCell();

            releaseButton(sender);
        }

        private void btnSelectGapCell_Click(object sender, RoutedEventArgs e)
        {
            GapCellCorrectValue correctValue;

            if (btnSelectGapCell.IsEnabled)
            {
                if (aryUnitData[cursorGapCellUnitX, cursorGapCellUnitY].UnitInfo == null)
                {
                    ShowMessageWindow("Failed to store Cabinet information.", "Error!", System.Drawing.SystemIcons.Error, 320, 180);
                    return;
                }

                isGcSelectBtnSelected = true;
                tbtnCursorGapCell.IsChecked = false;
                gdGapCellCorrectValue.IsEnabled = true;

                outputSignalGapCellAdjustArea();

                getGapCellCorrectValue(out correctValue);
                showGapCellCorrectValue(correctValue);

            }
        }

        #endregion Cursor

        #region Cv Module

        private void rbGapModCv_Click(object sender, RoutedEventArgs e)
        {
            actionButton(sender, "Select Mode", "ListeningEarcon.wav");

            if (rbGapModCvModule.IsChecked == true)
            {
                gdGapCorrectValueCell.IsEnabled = true;
                gdGapModCvCabinet.IsEnabled = false;
                btnGapModCvCabiSet.IsEnabled = false;
            }
            else
            {
                gdGapCorrectValueCell.IsEnabled = false;
                gdGapModCvCabinet.IsEnabled = true;
                btnGapModCvCabiSet.IsEnabled = true;
            }

            releaseButton(sender);
        }

        #region ComboBox

        private void cmbxGapTopLeftCell_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            actionButton(sender, "Set Correct Value");
            setGapCellCorrectValue(EdgePosition.Edge_1, moduleGapBase - cmbxGapTopLeftCell.SelectedIndex * moduleGapStep); // SelectedIndex = 12 - (value - 128) / 4
            releaseButton(sender);
        }

        private void cmbxGapTopRightCell_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            actionButton(sender, "Set Correct Value");
            setGapCellCorrectValue(EdgePosition.Edge_2, moduleGapBase - cmbxGapTopRightCell.SelectedIndex * moduleGapStep); // SelectedIndex = 12 - (value - 128) / 4
            releaseButton(sender);
        }

        private void cmbxGapLeftTopCell_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            actionButton(sender, "Set Correct Value");
            setGapCellCorrectValue(EdgePosition.Edge_3, moduleGapBase - cmbxGapLeftTopCell.SelectedIndex * moduleGapStep); // SelectedIndex = 12 - (value - 128) / 4
            releaseButton(sender);
        }

        private void cmbxGapLeftBottomCell_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            actionButton(sender, "Set Correct Value");
            setGapCellCorrectValue(EdgePosition.Edge_5, moduleGapBase - cmbxGapLeftBottomCell.SelectedIndex * moduleGapStep); // SelectedIndex = 12 - (value - 128) / 4
            releaseButton(sender);
        }

        private void cmbxGapRightTopCell_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            actionButton(sender, "Set Correct Value");
            setGapCellCorrectValue(EdgePosition.Edge_4, moduleGapBase - cmbxGapRightTopCell.SelectedIndex * moduleGapStep); // SelectedIndex = 12 - (value - 128) / 4
            releaseButton(sender);
        }

        private void cmbxGapRightBottomCell_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            actionButton(sender, "Set Correct Value");
            setGapCellCorrectValue(EdgePosition.Edge_6, moduleGapBase - cmbxGapRightBottomCell.SelectedIndex * moduleGapStep); // SelectedIndex = 12 - (value - 128) / 4
            releaseButton(sender);
        }

        private void cmbxGapBottomLeftCell_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            actionButton(sender, "Set Correct Value");
            setGapCellCorrectValue(EdgePosition.Edge_7, moduleGapBase - cmbxGapBottomLeftCell.SelectedIndex * moduleGapStep); // SelectedIndex = 12 - (value - 128) / 4
            releaseButton(sender);
        }

        private void cmbxGapBottomRightCell_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            actionButton(sender, "Set Correct Value");
            setGapCellCorrectValue(EdgePosition.Edge_8, moduleGapBase - cmbxGapBottomRightCell.SelectedIndex * moduleGapStep); // SelectedIndex = 12 - (value - 128) / 4
            releaseButton(sender);
        }

        #endregion ComboBox

        #region Up & Down Button

        private void btnGapTopLeftUpCell_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapTopLeftCell.SelectedIndex;

            if (index <= 0)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapTopLeftCell.SelectionChanged -= cmbxGapTopLeftCell_SelectionChanged;
            cmbxGapTopLeftCell.SelectedIndex = index - 1;
            cmbxGapTopLeftCell.SelectionChanged += cmbxGapTopLeftCell_SelectionChanged;

            setGapCellCorrectValue(EdgePosition.Edge_1, moduleGapBase - cmbxGapTopLeftCell.SelectedIndex * moduleGapStep);

            releaseButton(sender);
        }

        private void btnGapTopLeftDownCell_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapTopLeftCell.SelectedIndex;

            if (index >= 24)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapTopLeftCell.SelectionChanged -= cmbxGapTopLeftCell_SelectionChanged;
            cmbxGapTopLeftCell.SelectedIndex = index + 1;
            cmbxGapTopLeftCell.SelectionChanged += cmbxGapTopLeftCell_SelectionChanged;

            setGapCellCorrectValue(EdgePosition.Edge_1, moduleGapBase - cmbxGapTopLeftCell.SelectedIndex * moduleGapStep);

            releaseButton(sender);
        }

        private void btnGapTopRightUpCell_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapTopRightCell.SelectedIndex;

            if (index <= 0)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapTopRightCell.SelectionChanged -= cmbxGapTopRightCell_SelectionChanged;
            cmbxGapTopRightCell.SelectedIndex = index - 1;
            cmbxGapTopRightCell.SelectionChanged += cmbxGapTopRightCell_SelectionChanged;

            setGapCellCorrectValue(EdgePosition.Edge_2, moduleGapBase - cmbxGapTopRightCell.SelectedIndex * moduleGapStep);

            releaseButton(sender);
        }

        private void btnGapTopRightDownCell_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapTopRightCell.SelectedIndex;

            if (index >= 24)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapTopRightCell.SelectionChanged -= cmbxGapTopRightCell_SelectionChanged;
            cmbxGapTopRightCell.SelectedIndex = index + 1;
            cmbxGapTopRightCell.SelectionChanged += cmbxGapTopRightCell_SelectionChanged;

            setGapCellCorrectValue(EdgePosition.Edge_2, moduleGapBase - cmbxGapTopRightCell.SelectedIndex * moduleGapStep);

            releaseButton(sender);
        }

        private void btnGapLeftTopUpCell_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapLeftTopCell.SelectedIndex;

            if (index <= 0)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapLeftTopCell.SelectionChanged -= cmbxGapLeftTopCell_SelectionChanged;
            cmbxGapLeftTopCell.SelectedIndex = index - 1;
            cmbxGapLeftTopCell.SelectionChanged += cmbxGapLeftTopCell_SelectionChanged;

            setGapCellCorrectValue(EdgePosition.Edge_3, moduleGapBase - cmbxGapLeftTopCell.SelectedIndex * moduleGapStep);

            releaseButton(sender);
        }

        private void btnGapLeftTopDownCell_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapLeftTopCell.SelectedIndex;

            if (index >= 24)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapLeftTopCell.SelectionChanged -= cmbxGapLeftTopCell_SelectionChanged;
            cmbxGapLeftTopCell.SelectedIndex = index + 1;
            cmbxGapLeftTopCell.SelectionChanged += cmbxGapLeftTopCell_SelectionChanged;

            setGapCellCorrectValue(EdgePosition.Edge_3, moduleGapBase - cmbxGapLeftTopCell.SelectedIndex * moduleGapStep);

            releaseButton(sender);
        }

        private void btnGapLeftBottomUpCell_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapLeftBottomCell.SelectedIndex;

            if (index <= 0)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapLeftBottomCell.SelectionChanged -= cmbxGapLeftBottomCell_SelectionChanged;
            cmbxGapLeftBottomCell.SelectedIndex = index - 1;
            cmbxGapLeftBottomCell.SelectionChanged += cmbxGapLeftBottomCell_SelectionChanged;

            setGapCellCorrectValue(EdgePosition.Edge_5, moduleGapBase - cmbxGapLeftBottomCell.SelectedIndex * moduleGapStep);

            releaseButton(sender);
        }

        private void btnGapLeftBottomDownCell_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapLeftBottomCell.SelectedIndex;

            if (index >= 24)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapLeftBottomCell.SelectionChanged -= cmbxGapLeftBottomCell_SelectionChanged;
            cmbxGapLeftBottomCell.SelectedIndex = index + 1;
            cmbxGapLeftBottomCell.SelectionChanged += cmbxGapLeftBottomCell_SelectionChanged;

            setGapCellCorrectValue(EdgePosition.Edge_5, moduleGapBase - cmbxGapLeftBottomCell.SelectedIndex * moduleGapStep);

            releaseButton(sender);
        }

        private void btnGapRightTopUpCell_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapRightTopCell.SelectedIndex;

            if (index <= 0)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapRightTopCell.SelectionChanged -= cmbxGapRightTopCell_SelectionChanged;
            cmbxGapRightTopCell.SelectedIndex = index - 1;
            cmbxGapRightTopCell.SelectionChanged += cmbxGapRightTopCell_SelectionChanged;

            setGapCellCorrectValue(EdgePosition.Edge_4, moduleGapBase - cmbxGapRightTopCell.SelectedIndex * moduleGapStep);

            releaseButton(sender);
        }

        private void btnGapRightTopDownCell_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapRightTopCell.SelectedIndex;

            if (index >= 24)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapRightTopCell.SelectionChanged -= cmbxGapRightTopCell_SelectionChanged;
            cmbxGapRightTopCell.SelectedIndex = index + 1;
            cmbxGapRightTopCell.SelectionChanged += cmbxGapRightTopCell_SelectionChanged;

            setGapCellCorrectValue(EdgePosition.Edge_4, moduleGapBase - cmbxGapRightTopCell.SelectedIndex * moduleGapStep);

            releaseButton(sender);
        }

        private void btnGapRightBottomUpCell_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapRightBottomCell.SelectedIndex;

            if (index <= 0)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapRightBottomCell.SelectionChanged -= cmbxGapRightBottomCell_SelectionChanged;
            cmbxGapRightBottomCell.SelectedIndex = index - 1;
            cmbxGapRightBottomCell.SelectionChanged += cmbxGapRightBottomCell_SelectionChanged;

            setGapCellCorrectValue(EdgePosition.Edge_6, moduleGapBase - cmbxGapRightBottomCell.SelectedIndex * moduleGapStep);

            releaseButton(sender);
        }

        private void btnGapRightBottomDownCell_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapRightBottomCell.SelectedIndex;

            if (index >= 24)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapRightBottomCell.SelectionChanged -= cmbxGapRightBottomCell_SelectionChanged;
            cmbxGapRightBottomCell.SelectedIndex = index + 1;
            cmbxGapRightBottomCell.SelectionChanged += cmbxGapRightBottomCell_SelectionChanged;

            setGapCellCorrectValue(EdgePosition.Edge_6, moduleGapBase - cmbxGapRightBottomCell.SelectedIndex * moduleGapStep);

            releaseButton(sender);
        }

        private void btnGapBottomLeftUpCell_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapBottomLeftCell.SelectedIndex;

            if (index <= 0)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapBottomLeftCell.SelectionChanged -= cmbxGapBottomLeftCell_SelectionChanged;
            cmbxGapBottomLeftCell.SelectedIndex = index - 1;
            cmbxGapBottomLeftCell.SelectionChanged += cmbxGapBottomLeftCell_SelectionChanged;

            setGapCellCorrectValue(EdgePosition.Edge_7, moduleGapBase - cmbxGapBottomLeftCell.SelectedIndex * moduleGapStep);

            releaseButton(sender);
        }

        private void btnGapBottomLeftDownCell_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapBottomLeftCell.SelectedIndex;

            if (index >= 24)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapBottomLeftCell.SelectionChanged -= cmbxGapBottomLeftCell_SelectionChanged;
            cmbxGapBottomLeftCell.SelectedIndex = index + 1;
            cmbxGapBottomLeftCell.SelectionChanged += cmbxGapBottomLeftCell_SelectionChanged;

            setGapCellCorrectValue(EdgePosition.Edge_7, moduleGapBase - cmbxGapBottomLeftCell.SelectedIndex * moduleGapStep);

            releaseButton(sender);
        }

        private void btnGapBottomRightUpCell_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapBottomRightCell.SelectedIndex;

            if (index <= 0)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapBottomRightCell.SelectionChanged -= cmbxGapBottomRightCell_SelectionChanged;
            cmbxGapBottomRightCell.SelectedIndex = index - 1;
            cmbxGapBottomRightCell.SelectionChanged += cmbxGapBottomRightCell_SelectionChanged;

            setGapCellCorrectValue(EdgePosition.Edge_8, moduleGapBase - cmbxGapBottomRightCell.SelectedIndex * moduleGapStep);

            releaseButton(sender);
        }

        private void btnGapBottomRightDownCell_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapBottomRightCell.SelectedIndex;

            if (index >= 24)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapBottomRightCell.SelectionChanged -= cmbxGapBottomRightCell_SelectionChanged;
            cmbxGapBottomRightCell.SelectedIndex = index + 1;
            cmbxGapBottomRightCell.SelectionChanged += cmbxGapBottomRightCell_SelectionChanged;

            setGapCellCorrectValue(EdgePosition.Edge_8, moduleGapBase - cmbxGapBottomRightCell.SelectedIndex * moduleGapStep);

            releaseButton(sender);
        }

        #endregion Up & Down Button

        #endregion Cv Module

        #region Cv Cabinet

        private async void btnGapModSingleCabiSet_Click(object sender, RoutedEventArgs e)
        {
            int valueH, valueV;

            tcMain.IsEnabled = false;
            actionButton(sender, "Set Gap Module Correction Value.");

            valueH = moduleGapBase - cmbxGapModCabiH.SelectedIndex * moduleGapStep;
            valueV = moduleGapBase - cmbxGapModCabiV.SelectedIndex * moduleGapStep;

            winProgress = new WindowProgress("Set Gap Module Correction Values Progress");
            winProgress.Show();

            try { await Task.Run(() => correctGapModSingleCabi(valueH, valueV, 100)); }
            catch (Exception ex)
            { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

            winProgress.Close();

            string msg = "Set Gap Module Correction Values Complete!";
            string caption = "Complete";

            playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
            WindowMessage winMessage = new WindowMessage(msg, caption);
            winMessage.ShowDialog();

            releaseButton(sender, "Set Correction Value Done.");
            tcMain.IsEnabled = true;
        }

        private void btnGapModCvCabiUpH_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapModCabiH.SelectedIndex;

            if (index <= 0)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapModCabiH.SelectedIndex = index - 1;

            releaseButton(sender);
        }

        private void btnGapModCvCabiDownH_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapModCabiH.SelectedIndex;

            if (index >= cmbxGapModCabiH.Items.Count - 1)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapModCabiH.SelectedIndex = index + 1;

            releaseButton(sender);
        }

        private void btnGapModCvCabiUpV_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapModCabiV.SelectedIndex;

            if (index <= 0)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapModCabiV.SelectedIndex = index - 1;

            releaseButton(sender);
        }

        private void btnGapModCvCabiDownV_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapModCabiV.SelectedIndex;

            if (index >= cmbxGapModCabiV.Items.Count - 1)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapModCabiV.SelectedIndex = index + 1;

            releaseButton(sender);
        }

        #endregion Cv Cabinet

        #region All Cabinets Inside

        private async void btnGapModAllCabiInsideSet_Click(object sender, RoutedEventArgs e)
        {
            bool status = false;
            int valueH, valueV;

            tcMain.IsEnabled = false;
            actionButton(sender, "Set Gap Module Correction Value Writing Start.");

            valueH = moduleGapBase - cmbxGapModAllCabiInsideH.SelectedIndex * moduleGapStep;
            valueV = moduleGapBase - cmbxGapModAllCabiInsideV.SelectedIndex * moduleGapStep;

            winProgress = new WindowProgress("Set All Correction Values Progress");
            winProgress.Show();

            try { status = await Task.Run(() => correctGapModAllCabiInside(valueH, valueV)); }
            catch (Exception ex)
            { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

            string msg = "";
            string caption = "";
            if (status == true)
            {
                msg = "Set All Correction Values Complete!";
                caption = "Complete";
            }
            else
            {
                msg = "Failed in Write Correction Value.";
                caption = "Error";
            }

            winProgress.Close();

            playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
            WindowMessage winMessage = new WindowMessage(msg, caption);
            winMessage.ShowDialog();

            releaseButton(sender, "Set Correction Value Done.");
            tcMain.IsEnabled = true;
        }

        private void btnGapModAllCabiInsideUpH_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapModAllCabiInsideH.SelectedIndex;

            if (index <= 0)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapModAllCabiInsideH.SelectedIndex = index - 1;

            releaseButton(sender);
        }

        private void btnGapModAllCabiInsideDownH_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapModAllCabiInsideH.SelectedIndex;

            if (index >= cmbxGapModAllCabiInsideH.Items.Count - 1)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapModAllCabiInsideH.SelectedIndex = index + 1;

            releaseButton(sender);
        }

        private void btnGapModAllCabiInsideUpV_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapModAllCabiInsideV.SelectedIndex;

            if (index <= 0)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapModAllCabiInsideV.SelectedIndex = index - 1;

            releaseButton(sender);
        }

        private void btnGapModAllCabiInsideDownV_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapModAllCabiInsideV.SelectedIndex;

            if (index >= cmbxGapModAllCabiInsideV.Items.Count - 1)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapModAllCabiInsideV.SelectedIndex = index + 1;

            releaseButton(sender);
        }

        #endregion All Cabinets Inside

        #region Both All

        private async void btnGapModBothAll_Click(object sender, RoutedEventArgs e)
        {
            bool status = false;
            int valueInsideH, valueInsideV, valueOutsideH, valueOutsideV;

            tcMain.IsEnabled = false;
            actionButton(sender, "Set Gap Module Correction Value Writing Start.");

            valueInsideH = moduleGapBase - cmbxGapModAllCabiInsideH.SelectedIndex * moduleGapStep;
            valueInsideV = moduleGapBase - cmbxGapModAllCabiInsideV.SelectedIndex * moduleGapStep;
            valueOutsideH = moduleGapBase - cmbxGapModAllCabiOutsideH.SelectedIndex * moduleGapStep;
            valueOutsideV = moduleGapBase - cmbxGapModAllCabiOutsideV.SelectedIndex * moduleGapStep;

            winProgress = new WindowProgress("Set All Correction Values Progress");
            winProgress.Show();

            try { status = await Task.Run(() => correctGapModBothAll(valueInsideH, valueInsideV, valueOutsideH, valueOutsideV)); }
            catch (Exception ex)
            { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

            string msg = "";
            string caption = "";
            if (status == true)
            {
                msg = "Set All Correction Values Complete!";
                caption = "Complete";
            }
            else
            {
                msg = "Failed in Write Correction Value.";
                caption = "Error";
            }

            winProgress.Close();

            playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
            WindowMessage winMessage = new WindowMessage(msg, caption);
            winMessage.ShowDialog();

            releaseButton(sender, "Set Correction Value Done.");
            tcMain.IsEnabled = true;
        }

        #endregion Both All

        #region All Cabinets Outside

        private async void btnGapModAllCabiOutsideSet_Click(object sender, RoutedEventArgs e)
        {
            bool status = false;
            int valueH, valueV;

            tcMain.IsEnabled = false;
            actionButton(sender, "Set Gap Module Correction Value Writing Start.");

            valueH = moduleGapBase - cmbxGapModAllCabiOutsideH.SelectedIndex * moduleGapStep;
            valueV = moduleGapBase - cmbxGapModAllCabiOutsideV.SelectedIndex * moduleGapStep;

            winProgress = new WindowProgress("Set All Correction Values Progress");
            winProgress.Show();

            try { status = await Task.Run(() => correctGapModAllCabiOutside(valueH, valueV)); }
            catch (Exception ex)
            { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

            string msg = "";
            string caption = "";
            if (status == true)
            {
                msg = "Set All Correction Values Complete!";
                caption = "Complete";
            }
            else
            {
                msg = "Failed in Write Correction Value.";
                caption = "Error";
            }

            winProgress.Close();

            playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
            WindowMessage winMessage = new WindowMessage(msg, caption);
            winMessage.ShowDialog();

            releaseButton(sender, "Set Correction Value Done.");
            tcMain.IsEnabled = true;
        }

        private void btnGapModAllCabiOutsideUpH_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapModAllCabiOutsideH.SelectedIndex;

            if (index <= 0)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapModAllCabiOutsideH.SelectedIndex = index - 1;

            releaseButton(sender);
        }

        private void btnGapModAllCabiOutsideDownH_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapModAllCabiOutsideH.SelectedIndex;

            if (index >= cmbxGapModAllCabiOutsideH.Items.Count - 1)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapModAllCabiOutsideH.SelectedIndex = index + 1;

            releaseButton(sender);
        }

        private void btnGapModAllCabiOutsideUpV_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapModAllCabiOutsideV.SelectedIndex;

            if (index <= 0)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapModAllCabiOutsideV.SelectedIndex = index - 1;

            releaseButton(sender);
        }

        private void btnGapModAllCabiOutsideDownV_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbxGapModAllCabiOutsideV.SelectedIndex;

            if (index >= cmbxGapModAllCabiOutsideV.Items.Count - 1)
            { return; }

            actionButton(sender, "Set Correct Value");

            cmbxGapModAllCabiOutsideV.SelectedIndex = index + 1;

            releaseButton(sender);
        }

        #endregion All Cabinets Inside

        #region Data Write

        private async void btnExecuteGapCell_Click(object sender, RoutedEventArgs e)
        {
            bool status = false;
            var dispatcher = Application.Current.Dispatcher;

            tcMain.IsEnabled = false;
            actionButton(sender, "Writing Gap Module Correction Value Start.");

            string msg = "";

            winProgress = new WindowProgress("Write Correction Value Progress");
            winProgress.Show();

            try { status = await Task.Run(() => writeGapCellCorrectionValue()); }
            catch (Exception ex)
            { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

            msg = "";
            string caption = "";
            if (status == true)
            {
                // Cabinet Power Onの直後のため
                System.Threading.Thread.Sleep(5000);

                // ●Raster表示
                outputRaster(brightness._20pc); // 20%

                msg = "Write Correction Value Complete!";
                caption = "Complete";
            }
            else
            {
                msg = "Failed in Write Correction Value.";
                caption = "Error";
            }

            winProgress.Close();

            playSound(applicationPath + "\\Components\\Sound\\crrect_answer2.mp3");
            WindowMessage winMessage = new WindowMessage(msg, caption);
            winMessage.ShowDialog();

            releaseButton(sender, "Write Correction Value Done.");
            tcMain.IsEnabled = true;
        }

        #endregion Data Write

        #region Internal Pattern

        private void cmbxLevelGapCell_DropDownClosed(object sender, EventArgs e)
        {
            if (tbtnCursorGapCell.IsChecked == false)
            {
                if (isGcSelectBtnSelected == true)
                { outputSignalGapCellAdjustArea(); }
                else
            	{ cmbxPatternGapCell_DropDownClosed(sender, e); }
            }
            else
            {
                actionButton(sender, "Change Cursor Level");
                outputCursorGapCell();
                releaseButton(sender);
            }
        }

        private void cmbxLevelGapCell_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabIndex_GapCell == gamePadViewModel.TabSelectIndex)
            {
                cmbxLevelGapCell_DropDownClosed(sender, e);
            }
        }

        private void cmbxPatternGapCell_DropDownClosed(object sender, EventArgs e)
        {
            if (cmbxPatternGapCell != null && tbtnCursorGapCell.IsChecked == false)
            {
                if (cmbxPatternGapCell.SelectedIndex == 0)
                {
                    actionButton(sender, "Set Plane Signal");
                    outputSignalGapCell();
                    releaseButton(sender);
                }
                else if (cmbxPatternGapCell.SelectedIndex == 1)
                {
                    actionButton(sender, "Set Hatch Cabinet");
                    outputSignalGapCell(TestPattern.Unit);
                    releaseButton(sender);
                }
                else if (cmbxPatternGapCell.SelectedIndex == 2)
                {
                    actionButton(sender, "Set Hatch Module");
                    outputSignalGapCell(TestPattern.Cell);
                    releaseButton(sender);
                }
                else if (cmbxPatternGapCell.SelectedIndex == 3)
                {
                    actionButton(sender, "Internal Signal Off");
                    foreach (ControllerInfo cont in dicController.Values)
                    { sendSdcpCommand(SDCPClass.CmdIntSignalOff, 0, cont.IPAddress); }
                    releaseButton(sender);
                }
            }
        }

        #endregion Internal Pattern

        #endregion Events

        #region Private Methods

        private bool getGapCorrectionModuleMode()
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
                rbGapCorrectModuleOff.Checked -= rbGapCorrectModuleOff_Checked;
                rbGapCorrectModuleOff.IsChecked = true;
                rbGapCorrectModuleOff.Checked += rbGapCorrectModuleOff_Checked;
            }
            else // On
            {
                rbGapCorrectModuleOn.Checked -= rbGapCorrectModuleOn_Checked;
                rbGapCorrectModuleOn.IsChecked = true;
                rbGapCorrectModuleOn.Checked += rbGapCorrectModuleOn_Checked;
            }

            return true;
        }

        private void outputCursorGapCell()
        {
            int foregroundLevel, backgroundLevel;
            MeasurementMode mode;

            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            if (cmbxLevelGapCell.SelectedIndex == 0) // 20%
            {
                backgroundLevel = brightness._20pc;
                foregroundLevel = brightness._24pc; // 24% 2.2 = 535
            }
            else if (cmbxLevelGapCell.SelectedIndex == 1) // 50%
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
                setSignalColor(ref cmd, foregroundLevel, backgroundLevel);

                if (rbCursorUnitGapCell.IsChecked == true)
                { mode = MeasurementMode.UnitUniformity; }
                else
                { mode = MeasurementMode.CellUniformity; }

                UnitInfo unit;
                setCmdPositionGapCell(ref cmd, mode, out unit);

                //foreach (ControllerInfo cont in dicController.Values)
                //{ sendSdcpCommand(cmd, 0, cont.IPAddress); }

                foreach (ControllerInfo cont in dicController.Values)
                {
                    if (cont.ControllerID == unit.ControllerID)
                    { sendSdcpCommand(cmd, 0, cont.IPAddress); }
                    else
                    { outputRaster(backgroundLevel, cont.ControllerID); }
                }
            }
            catch { }
        }

        private void setCmdPositionGapCell(ref Byte[] cmd, MeasurementMode mode, out UnitInfo unit)
        {
            int unitOffsetX, unitOffsetY;
            int cellOffsetX, cellOffsetY;

            unit = aryUnitData[cursorGapCellUnitX, cursorGapCellUnitY].UnitInfo;

            unitOffsetX = aryUnitData[cursorGapCellUnitX, cursorGapCellUnitY].UnitInfo.PixelX;
            unitOffsetY = aryUnitData[cursorGapCellUnitX, cursorGapCellUnitY].UnitInfo.PixelY;

            cellOffsetX = modDx * cursorGapCellCellX;
            cellOffsetY = modDy * cursorGapCellCellY;

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

        private void outputSignalGapCellAdjustArea()
        {
            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            int backgroundLevel, foregroundLevel;

            if (cmbxLevelGapCell.SelectedIndex == 0) // 20%
            {
                backgroundLevel = brightness._15pc;
                foregroundLevel = brightness._20pc;
            }
            else if (cmbxLevelGapCell.SelectedIndex == 1) // 50%
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
            setSignalColorGapCell(ref cmd, foregroundLevel, backgroundLevel);

            cmd[21] += 0x09; // Pattern: Window

            outputSignalGapCorrectModuleAdjustArea(ref cmd);
        }

        private void outputSignalGapCorrectModuleAdjustArea(ref Byte[] cmd)
        {
            CellNum selectModuleNo = (CellNum)(cursorGapCellCellX + (cursorGapCellCellY * 4));
            List<CellInfo> selectedAllModules = new List<CellInfo>();
            foreach (AdjustmentPosition position in Enum.GetValues(typeof(AdjustmentPosition)))
            {
                Tuple<CabinetOffset, WidthOffset> offset = RelativePosition.GetModuleOffset(selectModuleNo, position);
                int x = cursorGapCellUnitX + offset.Item1.X;
                int y = cursorGapCellUnitY + offset.Item1.Y;
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
                setCmdPositionGapCell(ref cmd, startPosX, startPosY, endPosX - startPosX, endPosY - startPosY);

                sendSdcpCommand(cmd, 0, controller.IPAddress);
            }
        }

        private void outputSignalGapCell(TestPattern pattern = TestPattern.Raster)
        {
            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            int level;
            CellColor color = CellColor.White;

            if (cmbxLevelGapCell.SelectedIndex == 0) // 20%
            { level = brightness._20pc; }
            else if (cmbxLevelGapCell.SelectedIndex == 1) // 50%
            { level = brightness._50pc; }
            else // 100%
            { level = brightness._100pc; }
            
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

        private void getGapCellCorrectValue(out GapCellCorrectValue value)
        {
            string gap;
            UnitInfo unit = null;

            value = new GapCellCorrectValue();

            try
            { unit = aryUnitUf[cursorGapCellUnitX, cursorGapCellUnitY].UnitInfo; }
            catch
            { return; }

            if (unit == null)
            { return; }

            Byte[] cmd = new byte[SDCPClass.CmdGapCellCorrectValueGet.Length];
            Array.Copy(SDCPClass.CmdGapCellCorrectValueGet, cmd, SDCPClass.CmdGapCellCorrectValueGet.Length);

            // Target
            cmd[9] += (byte)(unit.PortNo << 4);
            cmd[9] += (byte)unit.UnitNo;

            int cell = cursorGapCellCellX + cursorGapCellCellY * 4 + 1;

            cmd[8] = (byte)cell;

            sendSdcpCommand(cmd, out gap, dicController[unit.ControllerID].IPAddress);

            if(gap == "0180")
            { return; }

            try
            {
                int correctValue;
                // Top-Left
                string strValue = gap.Substring(0, 2);
                checkCorrectValue(Convert.ToInt32(strValue, 16), out correctValue);
                value.TopLeft = correctValue;

                // Top-Right
                strValue = gap.Substring(2, 2);
                checkCorrectValue(Convert.ToInt32(strValue, 16), out correctValue);
                value.TopRight = correctValue;

                // Left-Top
                strValue = gap.Substring(4, 2);
                checkCorrectValue(Convert.ToInt32(strValue, 16), out correctValue);
                value.LeftTop = correctValue;

                // Right-Top
                strValue = gap.Substring(6, 2);
                checkCorrectValue(Convert.ToInt32(strValue, 16), out correctValue);
                value.RightTop = correctValue;

                // Left-Bottom
                strValue = gap.Substring(8, 2);
                checkCorrectValue(Convert.ToInt32(strValue, 16), out correctValue);
                value.LeftBottom = correctValue;

                // Right-Bottom
                strValue = gap.Substring(10, 2);
                checkCorrectValue(Convert.ToInt32(strValue, 16), out correctValue);
                value.RightBottom = correctValue;
                
                // Bottom-Left
                strValue = gap.Substring(12, 2);
                checkCorrectValue(Convert.ToInt32(strValue, 16), out correctValue);
                value.BottomLeft = correctValue;

                // Bottom-Right
                strValue = gap.Substring(14, 2);
                checkCorrectValue(Convert.ToInt32(strValue, 16), out correctValue);
                value.BottomRight = correctValue;
            }
            catch { return; }
        }

        private void showGapCellCorrectValue(GapCellCorrectValue value)
        {
            // Top-Left
            cmbxGapTopLeftCell.SelectionChanged -= cmbxGapTopLeftCell_SelectionChanged;
            cmbxGapTopLeftCell.SelectedIndex = 12 - (value.TopLeft - 128) / moduleGapStep;
            cmbxGapTopLeftCell.SelectionChanged += cmbxGapTopLeftCell_SelectionChanged;

            // Top-Right
            cmbxGapTopRightCell.SelectionChanged -= cmbxGapTopRightCell_SelectionChanged;
            cmbxGapTopRightCell.SelectedIndex = 12 - (value.TopRight - 128) / moduleGapStep;
            cmbxGapTopRightCell.SelectionChanged += cmbxGapTopRightCell_SelectionChanged;

            // Left-Top
            cmbxGapLeftTopCell.SelectionChanged -= cmbxGapLeftTopCell_SelectionChanged;
            cmbxGapLeftTopCell.SelectedIndex = 12 - (value.LeftTop - 128) / moduleGapStep;
            cmbxGapLeftTopCell.SelectionChanged += cmbxGapLeftTopCell_SelectionChanged;

            // Left-Bottom
            cmbxGapLeftBottomCell.SelectionChanged -= cmbxGapLeftBottomCell_SelectionChanged;
            cmbxGapLeftBottomCell.SelectedIndex = 12 - (value.LeftBottom - 128) / moduleGapStep;
            cmbxGapLeftBottomCell.SelectionChanged += cmbxGapLeftBottomCell_SelectionChanged;

            // Right-Top
            cmbxGapRightTopCell.SelectionChanged -= cmbxGapRightTopCell_SelectionChanged;
            cmbxGapRightTopCell.SelectedIndex = 12 - (value.RightTop - 128) / moduleGapStep;
            cmbxGapRightTopCell.SelectionChanged += cmbxGapRightTopCell_SelectionChanged;

            // Right-Bottom
            cmbxGapRightBottomCell.SelectionChanged -= cmbxGapRightBottomCell_SelectionChanged;
            cmbxGapRightBottomCell.SelectedIndex = 12 - (value.RightBottom - 128) / moduleGapStep;
            cmbxGapRightBottomCell.SelectionChanged += cmbxGapRightBottomCell_SelectionChanged;

            // Bottom-Left
            cmbxGapBottomLeftCell.SelectionChanged -= cmbxGapBottomLeftCell_SelectionChanged;
            cmbxGapBottomLeftCell.SelectedIndex = 12 - (value.BottomLeft - 128) / moduleGapStep;
            cmbxGapBottomLeftCell.SelectionChanged += cmbxGapBottomLeftCell_SelectionChanged;

            // Bottom-Right
            cmbxGapBottomRightCell.SelectionChanged -= cmbxGapBottomRightCell_SelectionChanged;
            cmbxGapBottomRightCell.SelectedIndex = 12 - (value.BottomRight - 128) / moduleGapStep;
            cmbxGapBottomRightCell.SelectionChanged += cmbxGapBottomRightCell_SelectionChanged;
        }
        
        private void setGapCellCorrectValue(EdgePosition targetEdge, int value)
        {
            UnitInfo targetUnit = null;

            // Target Unit
            try
            { targetUnit = aryUnitUf[cursorGapCellUnitX, cursorGapCellUnitY].UnitInfo; }
            catch
            { return; }

            if (targetUnit == null)
            { return; }

            Byte[] cmd = new byte[SDCPClass.CmdGapCellCorrectValueSet.Length];
            Array.Copy(SDCPClass.CmdGapCellCorrectValueSet, cmd, SDCPClass.CmdGapCellCorrectValueSet.Length);

            cmd[9] += (byte)(targetUnit.PortNo << 4);
            cmd[9] += (byte)targetUnit.UnitNo;

            int targetCell = cursorGapCellCellX + cursorGapCellCellY * 4 + 1;

            cmd[20] = (byte)targetCell;

            cmd[21] = (byte)targetEdge;

            cmd[22] = (byte)value;

            sendSdcpCommand(cmd, 0, dicController[targetUnit.ControllerID].IPAddress);

            if(lstModifiedUnits.Contains(targetUnit) == false)
            { lstModifiedUnits.Add(targetUnit); }

            // Next Unit
            UnitInfo nextUnit;
            int nextCell;
            EdgePosition nextEdge;

            getNextCell(targetUnit, targetCell, targetEdge, out nextUnit, out nextCell, out nextEdge);

            if(nextUnit == null)
            { return; }

            cmd = new byte[SDCPClass.CmdGapCellCorrectValueSet.Length];
            Array.Copy(SDCPClass.CmdGapCellCorrectValueSet, cmd, SDCPClass.CmdGapCellCorrectValueSet.Length);

            cmd[9] += (byte)(nextUnit.PortNo << 4);
            cmd[9] += (byte)nextUnit.UnitNo;

            cmd[20] = (byte)nextCell;

            cmd[21] = (byte)nextEdge;

            cmd[22] = (byte)value;

            sendSdcpCommand(cmd, 0, dicController[nextUnit.ControllerID].IPAddress);

            if (lstModifiedUnits.Contains(nextUnit) == false)
            { lstModifiedUnits.Add(nextUnit); }
        }

        private void getNextCell(UnitInfo targetUnit, int targetCell, EdgePosition targetEdge, out UnitInfo nextUnit, out int nextCell, out EdgePosition nextEdge)
        {
            nextUnit = null;
            nextCell = -1;
            nextEdge = EdgePosition.NA;

            #region Cell 1
            if (targetCell == 1)
            {
                if(targetEdge == EdgePosition.Edge_1 || targetEdge == EdgePosition.Edge_2) // 上
                {
                    // Next Unit(上)
                    nextUnit = getUpwardUnit(targetUnit);
                    if (nextUnit == null)
                    { return; }

                    // Next Cell
                    nextCell = getUpwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_1)
                    { nextEdge = EdgePosition.Edge_7; }
                    else
                    { nextEdge = EdgePosition.Edge_8; }
                }
                else if(targetEdge == EdgePosition.Edge_3 || targetEdge == EdgePosition.Edge_5) // 左
                {
                    // Next Unit(左)
                    nextUnit = getLeftwardUnit(targetUnit);
                    if (nextUnit == null)
                    { return; }

                    // Next Cell
                    nextCell = getLeftwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_3)
                    { nextEdge = EdgePosition.Edge_4; }
                    else
                    { nextEdge = EdgePosition.Edge_6; }
                }
                else if(targetEdge == EdgePosition.Edge_4 || targetEdge == EdgePosition.Edge_6) // 右
                {
                    // Next Unit
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getRightwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_4)
                    { nextEdge = EdgePosition.Edge_3; }
                    else
                    { nextEdge = EdgePosition.Edge_5; }
                }
                else if (targetEdge == EdgePosition.Edge_7 || targetEdge == EdgePosition.Edge_8) // 下
                {
                    // Next Unit
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getDownwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_7)
                    { nextEdge = EdgePosition.Edge_1; }
                    else
                    { nextEdge = EdgePosition.Edge_2; }
                }
            }
            #endregion Cell 1
            #region Cell 2
            else if(targetCell == 2)
            {
                if (targetEdge == EdgePosition.Edge_1 || targetEdge == EdgePosition.Edge_2) // 上
                {
                    // Next Unit(上)
                    nextUnit = getUpwardUnit(targetUnit);
                    if (nextUnit == null)
                    { return; }

                    // Next Cell
                    nextCell = getUpwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_1)
                    { nextEdge = EdgePosition.Edge_7; }
                    else
                    { nextEdge = EdgePosition.Edge_8; }
                }
                else if (targetEdge == EdgePosition.Edge_3 || targetEdge == EdgePosition.Edge_5) // 左
                {
                    // Next Unit
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getLeftwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_3)
                    { nextEdge = EdgePosition.Edge_4; }
                    else
                    { nextEdge = EdgePosition.Edge_6; }
                }
                else if (targetEdge == EdgePosition.Edge_4 || targetEdge == EdgePosition.Edge_6) // 右
                {
                    // Next Unit
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getRightwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_4)
                    { nextEdge = EdgePosition.Edge_3; }
                    else
                    { nextEdge = EdgePosition.Edge_5; }
                }
                else if (targetEdge == EdgePosition.Edge_7 || targetEdge == EdgePosition.Edge_8) // 下
                {
                    // Next Unit
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getDownwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_7)
                    { nextEdge = EdgePosition.Edge_1; }
                    else
                    { nextEdge = EdgePosition.Edge_2; }
                }
            }
            #endregion Cell 2
            #region Cell 3
            else if (targetCell == 3)
            {
                if (targetEdge == EdgePosition.Edge_1 || targetEdge == EdgePosition.Edge_2) // 上
                {
                    // Next Unit(上)
                    nextUnit = getUpwardUnit(targetUnit);
                    if (nextUnit == null)
                    { return; }

                    // Next Cell
                    nextCell = getUpwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_1)
                    { nextEdge = EdgePosition.Edge_7; }
                    else
                    { nextEdge = EdgePosition.Edge_8; }
                }
                else if (targetEdge == EdgePosition.Edge_3 || targetEdge == EdgePosition.Edge_5) // 左
                {
                    // Next Unit(左)
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getLeftwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_3)
                    { nextEdge = EdgePosition.Edge_4; }
                    else
                    { nextEdge = EdgePosition.Edge_6; }
                }
                else if (targetEdge == EdgePosition.Edge_4 || targetEdge == EdgePosition.Edge_6) // 右
                {
                    // Next Unit(右)
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getRightwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_4)
                    { nextEdge = EdgePosition.Edge_3; }
                    else
                    { nextEdge = EdgePosition.Edge_5; }
                }
                else if (targetEdge == EdgePosition.Edge_7 || targetEdge == EdgePosition.Edge_8) // 下
                {
                    // Next Unit(下)
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getDownwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_7)
                    { nextEdge = EdgePosition.Edge_1; }
                    else
                    { nextEdge = EdgePosition.Edge_2; }
                }
            }
            #endregion Cell 3
            #region Cell 4
            else if (targetCell == 4)
            {
                if (targetEdge == EdgePosition.Edge_1 || targetEdge == EdgePosition.Edge_2) // 上
                {
                    // Next Unit(上)
                    nextUnit = getUpwardUnit(targetUnit);
                    if (nextUnit == null)
                    { return; }

                    // Next Cell
                    nextCell = getUpwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_1)
                    { nextEdge = EdgePosition.Edge_7; }
                    else
                    { nextEdge = EdgePosition.Edge_8; }
                }
                else if (targetEdge == EdgePosition.Edge_3 || targetEdge == EdgePosition.Edge_5) // 左
                {
                    // Next Unit(左)
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getLeftwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_3)
                    { nextEdge = EdgePosition.Edge_4; }
                    else
                    { nextEdge = EdgePosition.Edge_6; }
                }
                else if (targetEdge == EdgePosition.Edge_4 || targetEdge == EdgePosition.Edge_6) // 右
                {
                    // Next Unit(右)
                    nextUnit = getRightwardUnit(targetUnit);
                    if (nextUnit == null)
                    { return; }

                    // Next Cell
                    nextCell = getRightwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_4)
                    { nextEdge = EdgePosition.Edge_3; }
                    else
                    { nextEdge = EdgePosition.Edge_5; }
                }
                else if (targetEdge == EdgePosition.Edge_7 || targetEdge == EdgePosition.Edge_8) // 下
                {
                    // Next Unit(下)
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getDownwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_7)
                    { nextEdge = EdgePosition.Edge_1; }
                    else
                    { nextEdge = EdgePosition.Edge_2; }
                }
            }
            #endregion Cell 4
            #region Cell 5
            else if (targetCell == 5)
            {
                if (targetEdge == EdgePosition.Edge_1 || targetEdge == EdgePosition.Edge_2) // 上
                {
                    // Next Unit(上)
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getUpwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_1)
                    { nextEdge = EdgePosition.Edge_7; }
                    else
                    { nextEdge = EdgePosition.Edge_8; }
                }
                else if (targetEdge == EdgePosition.Edge_3 || targetEdge == EdgePosition.Edge_5) // 左
                {
                    // Next Unit(左)
                    nextUnit = getLeftwardUnit(targetUnit);
                    if (nextUnit == null)
                    { return; }

                    // Next Cell
                    nextCell = getLeftwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_3)
                    { nextEdge = EdgePosition.Edge_4; }
                    else
                    { nextEdge = EdgePosition.Edge_6; }
                }
                else if (targetEdge == EdgePosition.Edge_4 || targetEdge == EdgePosition.Edge_6) // 右
                {
                    // Next Unit(右)
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getRightwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_4)
                    { nextEdge = EdgePosition.Edge_3; }
                    else
                    { nextEdge = EdgePosition.Edge_5; }
                }
                else if (targetEdge == EdgePosition.Edge_7 || targetEdge == EdgePosition.Edge_8) // 下
                {
                    // Next Unit(下)
                    nextUnit = getDownwardUnit(targetUnit, targetCell);
                    if (nextUnit == null)
                    { return; }

                    // Next Cell
                    nextCell = getDownwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_7)
                    { nextEdge = EdgePosition.Edge_1; }
                    else
                    { nextEdge = EdgePosition.Edge_2; }
                }
            }
            #endregion Cell 5
            #region Cell 6
            else if (targetCell == 6)
            {
               if (targetEdge == EdgePosition.Edge_1 || targetEdge == EdgePosition.Edge_2) // 上
                {
                    // Next Unit(上)
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getUpwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_1)
                    { nextEdge = EdgePosition.Edge_7; }
                    else
                    { nextEdge = EdgePosition.Edge_8; }
                }
                else if (targetEdge == EdgePosition.Edge_3 || targetEdge == EdgePosition.Edge_5) // 左
                {
                    // Next Unit(左)
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getLeftwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_3)
                    { nextEdge = EdgePosition.Edge_4; }
                    else
                    { nextEdge = EdgePosition.Edge_6; }
                }
                else if (targetEdge == EdgePosition.Edge_4 || targetEdge == EdgePosition.Edge_6) // 右
                {
                    // Next Unit(右)
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getRightwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_4)
                    { nextEdge = EdgePosition.Edge_3; }
                    else
                    { nextEdge = EdgePosition.Edge_5; }
                }
                else if (targetEdge == EdgePosition.Edge_7 || targetEdge == EdgePosition.Edge_8) // 下
                {
                    // Next Unit(下)
                    nextUnit = getDownwardUnit(targetUnit, targetCell);
                    if (nextUnit == null)
                    { return; }

                    // Next Cell
                    nextCell = getDownwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_7)
                    { nextEdge = EdgePosition.Edge_1; }
                    else
                    { nextEdge = EdgePosition.Edge_2; }
                }
            }
            #endregion Cell 6
            #region Cell 7
            else if (targetCell == 7)
            {
               if (targetEdge == EdgePosition.Edge_1 || targetEdge == EdgePosition.Edge_2) // 上
                {
                    // Next Unit(上)
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getUpwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_1)
                    { nextEdge = EdgePosition.Edge_7; }
                    else
                    { nextEdge = EdgePosition.Edge_8; }
                }
                else if (targetEdge == EdgePosition.Edge_3 || targetEdge == EdgePosition.Edge_5) // 左
                {
                    // Next Unit(左)
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getLeftwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_3)
                    { nextEdge = EdgePosition.Edge_4; }
                    else
                    { nextEdge = EdgePosition.Edge_6; }
                }
                else if (targetEdge == EdgePosition.Edge_4 || targetEdge == EdgePosition.Edge_6) // 右
                {
                    // Next Unit(右)
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getRightwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_4)
                    { nextEdge = EdgePosition.Edge_3; }
                    else
                    { nextEdge = EdgePosition.Edge_5; }
                }
                else if (targetEdge == EdgePosition.Edge_7 || targetEdge == EdgePosition.Edge_8) // 下
                {
                    // Next Unit(下)
                    nextUnit = getDownwardUnit(targetUnit, targetCell);
                    if (nextUnit == null)
                    { return; }

                    // Next Cell
                    nextCell = getDownwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_7)
                    { nextEdge = EdgePosition.Edge_1; }
                    else
                    { nextEdge = EdgePosition.Edge_2; }
                }
            }
            #endregion Cell 7
            #region Cell 8
            else if (targetCell == 8)
            {
               if (targetEdge == EdgePosition.Edge_1 || targetEdge == EdgePosition.Edge_2) // 上
                {
                    // Next Unit(上)
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getUpwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_1)
                    { nextEdge = EdgePosition.Edge_7; }
                    else
                    { nextEdge = EdgePosition.Edge_8; }
                }
                else if (targetEdge == EdgePosition.Edge_3 || targetEdge == EdgePosition.Edge_5) // 左
                {
                    // Next Unit(左)
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getLeftwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_3)
                    { nextEdge = EdgePosition.Edge_4; }
                    else
                    { nextEdge = EdgePosition.Edge_6; }
                }
                else if (targetEdge == EdgePosition.Edge_4 || targetEdge == EdgePosition.Edge_6) // 右
                {
                    // Next Unit(右)
                    nextUnit = getRightwardUnit(targetUnit);
                    if (nextUnit == null)
                    { return; }

                    // Next Cell
                    nextCell = getRightwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_4)
                    { nextEdge = EdgePosition.Edge_3; }
                    else
                    { nextEdge = EdgePosition.Edge_5; }
                }
                else if (targetEdge == EdgePosition.Edge_7 || targetEdge == EdgePosition.Edge_8) // 下
                {
                    // Next Unit(下)
                    nextUnit = getDownwardUnit(targetUnit, targetCell);
                    if (nextUnit == null)
                    { return; }

                    // Next Cell
                    nextCell = getDownwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_7)
                    { nextEdge = EdgePosition.Edge_1; }
                    else
                    { nextEdge = EdgePosition.Edge_2; }
                }
            }
            #endregion Cell 8
            #region Cell 9
            else if (targetCell == 9)
            {
                if (targetEdge == EdgePosition.Edge_1 || targetEdge == EdgePosition.Edge_2) // 上
                {
                    // Next Unit(上)
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getUpwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_1)
                    { nextEdge = EdgePosition.Edge_7; }
                    else
                    { nextEdge = EdgePosition.Edge_8; }
                }
                else if (targetEdge == EdgePosition.Edge_3 || targetEdge == EdgePosition.Edge_5) // 左
                {
                    // Next Unit(左)
                    nextUnit = getLeftwardUnit(targetUnit);
                    if (nextUnit == null)
                    { return; }

                    // Next Cell
                    nextCell = getLeftwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_3)
                    { nextEdge = EdgePosition.Edge_4; }
                    else
                    { nextEdge = EdgePosition.Edge_6; }
                }
                else if (targetEdge == EdgePosition.Edge_4 || targetEdge == EdgePosition.Edge_6) // 右
                {
                    // Next Unit(右)
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getRightwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_4)
                    { nextEdge = EdgePosition.Edge_3; }
                    else
                    { nextEdge = EdgePosition.Edge_5; }
                }
                else if (targetEdge == EdgePosition.Edge_7 || targetEdge == EdgePosition.Edge_8) // 下
                {
                    // Next Unit(下)
                    nextUnit = getDownwardUnit(targetUnit, targetCell);
                    if (nextUnit == null)
                    { return; }

                    // Next Cell
                    nextCell = getDownwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_7)
                    { nextEdge = EdgePosition.Edge_1; }
                    else
                    { nextEdge = EdgePosition.Edge_2; }
                }
            }
            #endregion Cell 9
            #region Cell 10
            else if (targetCell == 10)
            {
                if (targetEdge == EdgePosition.Edge_1 || targetEdge == EdgePosition.Edge_2) // 上
                {
                    // Next Unit(上)
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getUpwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_1)
                    { nextEdge = EdgePosition.Edge_7; }
                    else
                    { nextEdge = EdgePosition.Edge_8; }
                }
                else if (targetEdge == EdgePosition.Edge_3 || targetEdge == EdgePosition.Edge_5) // 左
                {
                    // Next Unit(左)
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getLeftwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_3)
                    { nextEdge = EdgePosition.Edge_4; }
                    else
                    { nextEdge = EdgePosition.Edge_6; }
                }
                else if (targetEdge == EdgePosition.Edge_4 || targetEdge == EdgePosition.Edge_6) // 右
                {
                    // Next Unit(右)
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getRightwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_4)
                    { nextEdge = EdgePosition.Edge_3; }
                    else
                    { nextEdge = EdgePosition.Edge_5; }
                }
                else if (targetEdge == EdgePosition.Edge_7 || targetEdge == EdgePosition.Edge_8) // 下
                {
                    // Next Unit(下)
                    nextUnit = getDownwardUnit(targetUnit, targetCell);
                    if (nextUnit == null)
                    { return; }

                    // Next Cell
                    nextCell = getDownwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_7)
                    { nextEdge = EdgePosition.Edge_1; }
                    else
                    { nextEdge = EdgePosition.Edge_2; }
                }
            }
            #endregion Cell 10
            #region Cell 11
            else if (targetCell == 11)
            {
                if (targetEdge == EdgePosition.Edge_1 || targetEdge == EdgePosition.Edge_2) // 上
                {
                    // Next Unit(上)
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getUpwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_1)
                    { nextEdge = EdgePosition.Edge_7; }
                    else
                    { nextEdge = EdgePosition.Edge_8; }
                }
                else if (targetEdge == EdgePosition.Edge_3 || targetEdge == EdgePosition.Edge_5) // 左
                {
                    // Next Unit(左)
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getLeftwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_3)
                    { nextEdge = EdgePosition.Edge_4; }
                    else
                    { nextEdge = EdgePosition.Edge_6; }
                }
                else if (targetEdge == EdgePosition.Edge_4 || targetEdge == EdgePosition.Edge_6) // 右
                {
                    // Next Unit(右)
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getRightwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_4)
                    { nextEdge = EdgePosition.Edge_3; }
                    else
                    { nextEdge = EdgePosition.Edge_5; }
                }
                else if (targetEdge == EdgePosition.Edge_7 || targetEdge == EdgePosition.Edge_8) // 下
                {
                    // Next Unit(下)
                    nextUnit = getDownwardUnit(targetUnit, targetCell);
                    if (nextUnit == null)
                    { return; }

                    // Next Cell
                    nextCell = getDownwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_7)
                    { nextEdge = EdgePosition.Edge_1; }
                    else
                    { nextEdge = EdgePosition.Edge_2; }
                }
            }
            #endregion Cell 11
            #region Cell 12
            else if (targetCell == 12)
            {
                if (targetEdge == EdgePosition.Edge_1 || targetEdge == EdgePosition.Edge_2) // 上
                {
                    // Next Unit(上)
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getUpwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_1)
                    { nextEdge = EdgePosition.Edge_7; }
                    else
                    { nextEdge = EdgePosition.Edge_8; }
                }
                else if (targetEdge == EdgePosition.Edge_3 || targetEdge == EdgePosition.Edge_5) // 左
                {
                    // Next Unit(左)
                    nextUnit = targetUnit;

                    // Next Cell
                    nextCell = getLeftwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_3)
                    { nextEdge = EdgePosition.Edge_4; }
                    else
                    { nextEdge = EdgePosition.Edge_6; }
                }
                else if (targetEdge == EdgePosition.Edge_4 || targetEdge == EdgePosition.Edge_6) // 右
                {
                    // Next Unit(右)
                    nextUnit = getRightwardUnit(targetUnit);
                    if (nextUnit == null)
                    { return; }

                    // Next Cell
                    nextCell = getRightwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_4)
                    { nextEdge = EdgePosition.Edge_3; }
                    else
                    { nextEdge = EdgePosition.Edge_5; }
                }
                else if (targetEdge == EdgePosition.Edge_7 || targetEdge == EdgePosition.Edge_8) // 下
                {
                    // Next Unit(下)
                    nextUnit = getDownwardUnit(targetUnit, targetCell);
                    if (nextUnit == null)
                    { return; }

                    // Next Cell
                    nextCell = getDownwardCell(targetCell);

                    // Next Edge
                    if (targetEdge == EdgePosition.Edge_7)
                    { nextEdge = EdgePosition.Edge_1; }
                    else
                    { nextEdge = EdgePosition.Edge_2; }
                }
            }
            #endregion Cell 12
        }

        private UnitInfo getUpwardUnit(UnitInfo targetUnit)
        {
            UnitInfo unit;

            if ((targetUnit.Y - 2) < 0)
            { unit = null; }
            else
            { unit = aryUnitUf[targetUnit.X - 1, targetUnit.Y - 2].UnitInfo; }

            return unit;
        }

        private UnitInfo getLeftwardUnit(UnitInfo targetUnit)
        {
            UnitInfo unit;

            if ((targetUnit.X - 2) < 0)
            { unit = null; }
            else
            { unit = aryUnitUf[targetUnit.X - 2, targetUnit.Y - 1].UnitInfo; } 

            return unit;
        }

        private UnitInfo getRightwardUnit(UnitInfo targetUnit)
        {
            UnitInfo unit;

            if (targetUnit.X >= aryUnitUf.GetLength(0))
            { unit = null; }
            else
            { unit = aryUnitUf[targetUnit.X, targetUnit.Y - 1].UnitInfo; }

            return unit;
        }

        private UnitInfo getDownwardUnit(UnitInfo targetUnit, int targetCell)
        {
            UnitInfo unit;
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            {
                if (targetUnit.Y >= aryUnitUf.GetLength(1))
                { unit = null; }
                else
                { unit = aryUnitUf[targetUnit.X - 1, targetUnit.Y].UnitInfo; }
            }
            else
            {
                if (targetCell == NO_5 || targetCell == NO_6 || targetCell == NO_7 || targetCell == NO_8)
                { unit = targetUnit; }
                else
                {
                    if (targetUnit.Y >= aryUnitUf.GetLength(1))
                    { unit = null; }
                    else
                    { unit = aryUnitUf[targetUnit.X - 1, targetUnit.Y].UnitInfo; }
                }
            }

            return unit;
        }

        private int getUpwardCell(int targetCell)
        {
            int upperCell;
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            {
                if (targetCell == NO_1)
                { upperCell = NO_5; }
                else if (targetCell == NO_2)
                { upperCell = NO_6; }
                else if (targetCell == NO_3)
                { upperCell = NO_7; }
                else if (targetCell == NO_4)
                { upperCell = NO_8; }
                else if (targetCell == NO_5)
                { upperCell = NO_1; }
                else if (targetCell == NO_6)
                { upperCell = NO_2; }
                else if (targetCell == NO_7)
                { upperCell = NO_3; }
                else
                { upperCell = NO_4; }
            }
            else
            {
                if (targetCell == NO_1)
                { upperCell = NO_9; }
                else if (targetCell == NO_2)
                { upperCell = NO_10; }
                else if (targetCell == NO_3)
                { upperCell = NO_11; }
                else if (targetCell == NO_4)
                { upperCell = NO_12; }
                else if (targetCell == NO_5)
                { upperCell = NO_1; }
                else if (targetCell == NO_6)
                { upperCell = NO_2; }
                else if (targetCell == NO_7)
                { upperCell = NO_3; }
                else if (targetCell == NO_8)
                { upperCell = NO_4; }
                else if (targetCell == NO_9)
                { upperCell = NO_5; }
                else if (targetCell == NO_10)
                { upperCell = NO_6; }
                else if (targetCell == NO_11)
                { upperCell = NO_7; }
                else
                { upperCell = NO_8; }
            }

            return upperCell;    
        }

        private int getLeftwardCell(int targetCell)
        {
            int upperCell;
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            {
                if (targetCell == NO_1)
                { upperCell = NO_4; }
                else if (targetCell == NO_2)
                { upperCell = NO_1; }
                else if (targetCell == NO_3)
                { upperCell = NO_2; }
                else if (targetCell == NO_4)
                { upperCell = NO_3; }
                else if (targetCell == NO_5)
                { upperCell = NO_8; }
                else if (targetCell == NO_6)
                { upperCell = NO_5; }
                else if (targetCell == NO_7)
                { upperCell = NO_6; }
                else
                { upperCell = NO_7; }
            }
            else
            {
                if (targetCell == NO_1)
                { upperCell = NO_4; }
                else if (targetCell == NO_2)
                { upperCell = NO_1; }
                else if (targetCell == NO_3)
                { upperCell = NO_2; }
                else if (targetCell == NO_4)
                { upperCell = NO_3; }
                else if (targetCell == NO_5)
                { upperCell = NO_8; }
                else if (targetCell == NO_6)
                { upperCell = NO_5; }
                else if (targetCell == NO_7)
                { upperCell = NO_6; }
                else if (targetCell == NO_8)
                { upperCell = NO_7; }
                else if (targetCell == NO_9)
                { upperCell = NO_12; }
                else if (targetCell == NO_10)
                { upperCell = NO_9; }
                else if (targetCell == NO_11)
                { upperCell = NO_10; }
                else
                { upperCell = NO_11; }
            }

            return upperCell;
        }

        private int getRightwardCell(int targetCell)
        {
            int upperCell;
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            {
                if (targetCell == NO_1)
                { upperCell = NO_2; }
                else if (targetCell == NO_2)
                { upperCell = NO_3; }
                else if (targetCell == NO_3)
                { upperCell = NO_4; }
                else if (targetCell == NO_4)
                { upperCell = NO_1; }
                else if (targetCell == NO_5)
                { upperCell = NO_6; }
                else if (targetCell == NO_6)
                { upperCell = NO_7; }
                else if (targetCell == NO_7)
                { upperCell = NO_8; }
                else
                { upperCell = NO_5; }
            }
            else
            {
                if (targetCell == NO_1)
                { upperCell = NO_2; }
                else if (targetCell == NO_2)
                { upperCell = NO_3; }
                else if (targetCell == NO_3)
                { upperCell = NO_4; }
                else if (targetCell == NO_4)
                { upperCell = NO_1; }
                else if (targetCell == NO_5)
                { upperCell = NO_6; }
                else if (targetCell == NO_6)
                { upperCell = NO_7; }
                else if (targetCell == NO_7)
                { upperCell = NO_8; }
                else if (targetCell == NO_8)
                { upperCell = NO_5; }
                else if (targetCell == NO_9)
                { upperCell = NO_10; }
                else if (targetCell == NO_10)
                { upperCell = NO_11; }
                else if (targetCell == NO_11)
                { upperCell = NO_12; }
                else
                { upperCell = NO_9; }
            }

            return upperCell;
        }

        private int getDownwardCell(int targetCell)
        {
            int upperCell;
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            {
                if (targetCell == NO_1)
                { upperCell = NO_5; }
                else if (targetCell == NO_2)
                { upperCell = NO_6; }
                else if (targetCell == NO_3)
                { upperCell = NO_7; }
                else if (targetCell == NO_4)
                { upperCell = NO_8; }
                else if (targetCell == NO_5)
                { upperCell = NO_1; }
                else if (targetCell == NO_6)
                { upperCell = NO_2; }
                else if (targetCell == NO_7)
                { upperCell = NO_3; }
                else
                { upperCell = NO_4; }
            }
            else
            {
                if (targetCell == NO_1)
                { upperCell = NO_5; }
                else if (targetCell == NO_2)
                { upperCell = NO_6; }
                else if (targetCell == NO_3)
                { upperCell = NO_7; }
                else if (targetCell == NO_4)
                { upperCell = NO_8; }
                else if (targetCell == NO_5)
                { upperCell = NO_9; }
                else if (targetCell == NO_6)
                { upperCell = NO_10; }
                else if (targetCell == NO_7)
                { upperCell = NO_11; }
                else if (targetCell == NO_8)
                { upperCell = NO_12; }
                else if (targetCell == NO_9)
                { upperCell = NO_1; }
                else if (targetCell == NO_10)
                { upperCell = NO_2; }
                else if (targetCell == NO_11)
                { upperCell = NO_3; }
                else
                { upperCell = NO_4; }
            }

            return upperCell;
        }

        private void setSignalColorGapCell(ref Byte[] cmd, int foreground, int background)
        {
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

        private void setCmdPositionGapCell(ref Byte[] cmd, int startPosX, int startPosY, int widthH, int widthV)
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

        private bool writeGapCellCorrectionValue()
        {
            if (Settings.Ins.ExecLog == true)
            {
                SaveExecLog("");
                SaveExecLog("[0] writeGapCellCorrectionValue start");
            }

            // ●進捗の最大値を設定 [1]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[1] Set Step Count."); }
            winProgress.ShowMessage("Set Step Count.");

            int step = lstModifiedUnits.Count;            
            winProgress.SetWholeSteps(step);
            winProgress.PutForward1Step();

            // ●Write [2]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[2] Write Correction Value."); }
            winProgress.ShowMessage("Write Correction Value.");

            foreach (UnitInfo unit in lstModifiedUnits)
            {
                Byte[] cmd = new byte[SDCPClass.CmdGapCellCorrectWrite.Length];
                Array.Copy(SDCPClass.CmdGapCellCorrectWrite, cmd, SDCPClass.CmdGapCellCorrectWrite.Length);

                cmd[9] += (byte)(unit.PortNo << 4);
                cmd[9] += (byte)unit.UnitNo;

                sendSdcpCommand(cmd, 0, dicController[unit.ControllerID].IPAddress);

                winProgress.PutForward1Step();
            }
            
            return true;
        }

        private bool correctGapModAllCabiInside(int valueH, int valueV)
        {
            // ●進捗の最大値を設定
            winProgress.ShowMessage("Set Step Count.");
            int step;

            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            { step = inside_PartCount_Module4x2 * dicController.Values.Count; }
            else
            { step = inside_PartCount_Module4x3 * dicController.Values.Count; }

            winProgress.SetWholeSteps(step);

            foreach (ControllerInfo controller in dicController.Values)
            {
                setGapModCvCabinetInside(valueH, valueV, 100, controller.ControllerID);
            }

            cursorGapCellUnitX = 0;
            cursorGapCellUnitY = 0;
            cursorGapCellCellX = 0;
            cursorGapCellCellY = 0;

            return true;
        }

        private bool correctGapModSingleCabi(int valueH, int valueV, int sdcpWait)
        {
            // ●進捗の最大値を設定
            winProgress.ShowMessage("Set Step Count.");
            int step;

            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            { step = inside_PartCount_Module4x2; }
            else
            { step = inside_PartCount_Module4x3; }

            winProgress.SetWholeSteps(step);

            setGapModCvCabinetInside(valueH, valueV, 100);

            return true;
        }

        private bool correctGapModBothAll(int valueInsideH, int valueInsideV, int valueOutsideH, int valueOutsideV)
        {
            // ●進捗の最大値を設定
            winProgress.ShowMessage("Set Step Count.");
            int step;

            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            { step = inside_PartCount_Module4x2 * dicController.Values.Count + outside_PartCount_Module4x2 * dicController.Values.Count; }
            else
            { step = inside_PartCount_Module4x3 * dicController.Values.Count + outside_PartCount_Module4x3 * dicController.Values.Count; }

            winProgress.SetWholeSteps(step);

            foreach (ControllerInfo controller in dicController.Values)
            {
                setGapModCvCabinetInside(valueInsideH, valueInsideV, 100, controller.ControllerID);
            }

            foreach (ControllerInfo controller in dicController.Values)
            {
                setGapModCvCabinetOutside(valueOutsideH, valueOutsideV, 100, controller.ControllerID);
            }

            cursorGapCellUnitX = 0;
            cursorGapCellUnitY = 0;
            cursorGapCellCellX = 0;
            cursorGapCellCellY = 0;

            return true;
        }



        private void setGapModCvCabinetInside(int valueH, int valueV, int sdcpWait, int controllerID = 0)
        {
            UnitInfo targetUnit = null;
            string message = "Set Inside Correction Value.\r\n";

            Byte[] cmd = new byte[SDCPClass.CmdGapCellCorrectValueSet.Length];
            Array.Copy(SDCPClass.CmdGapCellCorrectValueSet, cmd, SDCPClass.CmdGapCellCorrectValueSet.Length);

            if (controllerID == 0)
            {
                // Target Cabinet
                try
                { targetUnit = aryUnitUf[cursorGapCellUnitX, cursorGapCellUnitY].UnitInfo; }
                catch
                { return; }

                if (targetUnit == null)
                { return; }

                cmd[9] += (byte)(targetUnit.PortNo << 4);
                cmd[9] += (byte)targetUnit.UnitNo;

                controllerID = targetUnit.ControllerID;
                message += "[ C" + targetUnit.ControllerID + "-" + targetUnit.PortNo + "-" + targetUnit.UnitNo + " ]";

                if (lstModifiedUnits.Contains(targetUnit) == false)
                { lstModifiedUnits.Add(targetUnit); }
            }
            else
            {
                cmd[9] = 0xff; // 全Cabinet
                message += "[ Controller ID:" + controllerID + " ]";

                foreach (var unit in aryUnitUf)
                {
                    if (unit.UnitInfo != null)
                    {
                        if (lstModifiedUnits.Contains(unit.UnitInfo) == false)
                        { lstModifiedUnits.Add(unit.UnitInfo); }
                    }
                }
            }

            // Module No.1
            cmd[20] = (byte)1;

            // H
            cmd[22] = (byte)valueH;
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 1, Edge - 7");
            }
            cmd[21] = (byte)EdgePosition.Edge_7;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 1, Edge - 8");
            }
            cmd[21] = (byte)EdgePosition.Edge_8;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }

            // V
            cmd[22] = (byte)valueV;
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 1, Edge - 4");
            }
            cmd[21] = (byte)EdgePosition.Edge_4;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 1, Edge - 6");
            }
            cmd[21] = (byte)EdgePosition.Edge_6;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }

            // Module No.2
            cmd[20] = (byte)2;

            // H
            cmd[22] = (byte)valueH;
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 2, Edge - 7");
            }
            cmd[21] = (byte)EdgePosition.Edge_7;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 2, Edge - 8");
            }
            cmd[21] = (byte)EdgePosition.Edge_8;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }

            // V
            cmd[22] = (byte)valueV;
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 2, Edge - 3");
            }
            cmd[21] = (byte)EdgePosition.Edge_3;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 2, Edge - 5");
            }
            cmd[21] = (byte)EdgePosition.Edge_5;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 2, Edge - 4");
            }
            cmd[21] = (byte)EdgePosition.Edge_4;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 2, Edge - 6");
            }
            cmd[21] = (byte)EdgePosition.Edge_6;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }

            // Module No.3
            cmd[20] = (byte)3;

            // H
            cmd[22] = (byte)valueH;
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 3, Edge - 7");
            }
            cmd[21] = (byte)EdgePosition.Edge_7;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 3, Edge - 8");
            }
            cmd[21] = (byte)EdgePosition.Edge_8;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }

            // V
            cmd[22] = (byte)valueV;
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 3, Edge - 3");
            }
            cmd[21] = (byte)EdgePosition.Edge_3;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 3, Edge - 5");
            }
            cmd[21] = (byte)EdgePosition.Edge_5;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 3, Edge - 4");
            }
            cmd[21] = (byte)EdgePosition.Edge_4;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 3, Edge - 6");
            }
            cmd[21] = (byte)EdgePosition.Edge_6;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }

            // Module No.4
            cmd[20] = (byte)4;

            // H
            cmd[22] = (byte)valueH;
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 4, Edge - 7");
            }
            cmd[21] = (byte)EdgePosition.Edge_7;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 4, Edge - 8");
            }
            cmd[21] = (byte)EdgePosition.Edge_8;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }

            // V
            cmd[22] = (byte)valueV;
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 4, Edge - 3");
            }
            cmd[21] = (byte)EdgePosition.Edge_3;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 4, Edge - 5");
            }
            cmd[21] = (byte)EdgePosition.Edge_5;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }

            // Module No.5
            cmd[20] = (byte)5;

            // H
            cmd[22] = (byte)valueH;
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 5, Edge - 1");
            }
            cmd[21] = (byte)EdgePosition.Edge_1;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 5, Edge - 2");
            }
            cmd[21] = (byte)EdgePosition.Edge_2;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }

            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
            {
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 5, Edge - 7");
                }
                cmd[21] = (byte)EdgePosition.Edge_7;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 5, Edge - 8");
                }
                cmd[21] = (byte)EdgePosition.Edge_8;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
            }

            // V
            cmd[22] = (byte)valueV;
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 5, Edge - 4");
            }
            cmd[21] = (byte)EdgePosition.Edge_4;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 5, Edge - 6");
            }
            cmd[21] = (byte)EdgePosition.Edge_6;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }

            // Module No.6
            cmd[20] = (byte)6;

            // H
            cmd[22] = (byte)valueH;
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 6, Edge - 1");
            }
            cmd[21] = (byte)EdgePosition.Edge_1;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 6, Edge - 2");
            }
            cmd[21] = (byte)EdgePosition.Edge_2;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }

            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
            {
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 6, Edge - 7");
                }
                cmd[21] = (byte)EdgePosition.Edge_7;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 6, Edge - 8");
                }
                cmd[21] = (byte)EdgePosition.Edge_8;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
            }

            // V
            cmd[22] = (byte)valueV;
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 6, Edge - 3");
            }
            cmd[21] = (byte)EdgePosition.Edge_3;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 6, Edge - 5");
            }
            cmd[21] = (byte)EdgePosition.Edge_5;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 6, Edge - 4");
            }
            cmd[21] = (byte)EdgePosition.Edge_4;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 6, Edge - 6");
            }
            cmd[21] = (byte)EdgePosition.Edge_6;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }

            // Module No.7
            cmd[20] = (byte)7;

            // H
            cmd[22] = (byte)valueH;
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 7, Edge - 1");
            }
            cmd[21] = (byte)EdgePosition.Edge_1;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 7, Edge - 2");
            }
            cmd[21] = (byte)EdgePosition.Edge_2;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }

            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
            {
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 7, Edge - 7");
                }
                cmd[21] = (byte)EdgePosition.Edge_7;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 7, Edge - 8");
                }
                cmd[21] = (byte)EdgePosition.Edge_8;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
            }

            // V
            cmd[22] = (byte)valueV;
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 7, Edge - 3");
            }
            cmd[21] = (byte)EdgePosition.Edge_3;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 7, Edge - 5");
            }
            cmd[21] = (byte)EdgePosition.Edge_5;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 7, Edge - 4");
            }
            cmd[21] = (byte)EdgePosition.Edge_4;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 7, Edge - 6");
            }
            cmd[21] = (byte)EdgePosition.Edge_6;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }

            // Module No.8
            cmd[20] = (byte)8;

            // H
            cmd[22] = (byte)valueH;
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 8, Edge - 1");
            }
            cmd[21] = (byte)EdgePosition.Edge_1;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 8, Edge - 2");
            }
            cmd[21] = (byte)EdgePosition.Edge_2;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }

            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
            {
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 8, Edge - 7");
                }
                cmd[21] = (byte)EdgePosition.Edge_7;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 8, Edge - 8");
                }
                cmd[21] = (byte)EdgePosition.Edge_8;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
            }

            // V
            cmd[22] = (byte)valueV;
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 8, Edge - 3");
            }
            cmd[21] = (byte)EdgePosition.Edge_3;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 8, Edge - 5");
            }
            cmd[21] = (byte)EdgePosition.Edge_5;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }

            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
            {
                // Module No.9
                cmd[20] = (byte)9;

                // H
                cmd[22] = (byte)valueH;
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 9, Edge - 1");
                }
                cmd[21] = (byte)EdgePosition.Edge_1;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 9, Edge - 2");
                }
                cmd[21] = (byte)EdgePosition.Edge_2;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }

                // V
                cmd[22] = (byte)valueV;
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 9, Edge - 4");
                }
                cmd[21] = (byte)EdgePosition.Edge_4;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 9, Edge - 6");
                }
                cmd[21] = (byte)EdgePosition.Edge_6;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }

                // Module No.10
                cmd[20] = (byte)10;

                // H
                cmd[22] = (byte)valueH;
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 10, Edge - 1");
                }
                cmd[21] = (byte)EdgePosition.Edge_1;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 10, Edge - 2");
                }
                cmd[21] = (byte)EdgePosition.Edge_2;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }

                // V
                cmd[22] = (byte)valueV;
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 10, Edge - 3");
                }
                cmd[21] = (byte)EdgePosition.Edge_3;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 10, Edge - 5");
                }
                cmd[21] = (byte)EdgePosition.Edge_5;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 10, Edge - 4");
                }
                cmd[21] = (byte)EdgePosition.Edge_4;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 10, Edge - 6");
                }
                cmd[21] = (byte)EdgePosition.Edge_6;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }

                // Module No.11
                cmd[20] = (byte)11;

                // H
                cmd[22] = (byte)valueH;
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 11, Edge - 1");
                }
                cmd[21] = (byte)EdgePosition.Edge_1;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 11, Edge - 2");
                }
                cmd[21] = (byte)EdgePosition.Edge_2;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }

                // V
                cmd[22] = (byte)valueV;
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 11, Edge - 3");
                }
                cmd[21] = (byte)EdgePosition.Edge_3;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 11, Edge - 5");
                }
                cmd[21] = (byte)EdgePosition.Edge_5;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 11, Edge - 4");
                }
                cmd[21] = (byte)EdgePosition.Edge_4;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 11, Edge - 6");
                }
                cmd[21] = (byte)EdgePosition.Edge_6;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }

                // Module No.12
                cmd[20] = (byte)12;

                // H
                cmd[22] = (byte)valueH;
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 12, Edge - 1");
                }
                cmd[21] = (byte)EdgePosition.Edge_1;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 12, Edge - 2");
                }
                cmd[21] = (byte)EdgePosition.Edge_2;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }

                // V
                cmd[22] = (byte)valueV;
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 12, Edge - 3");
                }
                cmd[21] = (byte)EdgePosition.Edge_3;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 12, Edge - 5");
                }
                cmd[21] = (byte)EdgePosition.Edge_5;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
            }

        }

        private bool correctGapModAllCabiOutside(int valueH, int valueV)
        {
            // ●進捗の最大値を設定
            winProgress.ShowMessage("Set Step Count.");
            int step;

            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            { step = outside_PartCount_Module4x2 * dicController.Values.Count; }
            else
            { step = outside_PartCount_Module4x3 * dicController.Values.Count; }

            winProgress.SetWholeSteps(step);

            foreach (ControllerInfo controller in dicController.Values)
            {
                setGapModCvCabinetOutside(valueH, valueV, 100, controller.ControllerID);
            }

            cursorGapCellUnitX = 0;
            cursorGapCellUnitY = 0;
            cursorGapCellCellX = 0;
            cursorGapCellCellY = 0;

            return true;
        }

        private void setGapModCvCabinetOutside(int valueH, int valueV, int sdcpWait, int controllerID = 0)
        {
            UnitInfo targetUnit = null;
            string message = "Set Outside Correction Value.\r\n";

            Byte[] cmd = new byte[SDCPClass.CmdGapCellCorrectValueSet.Length];
            Array.Copy(SDCPClass.CmdGapCellCorrectValueSet, cmd, SDCPClass.CmdGapCellCorrectValueSet.Length);

            if (controllerID == 0)
            {
                // Target Cabinet
                try
                { targetUnit = aryUnitUf[cursorGapCellUnitX, cursorGapCellUnitY].UnitInfo; }
                catch
                { return; }

                if (targetUnit == null)
                { return; }

                cmd[9] += (byte)(targetUnit.PortNo << 4);
                cmd[9] += (byte)targetUnit.UnitNo;

                controllerID = targetUnit.ControllerID;
                message += "[ C" + targetUnit.ControllerID + "-" + targetUnit.PortNo + "-" + targetUnit.UnitNo + " ]";

                if (lstModifiedUnits.Contains(targetUnit) == false)
                { lstModifiedUnits.Add(targetUnit); }
            }
            else
            {
                cmd[9] = 0xff; // 全Cabinet
                message += "[ Controller ID:" + controllerID + " ]";

                foreach (var unit in aryUnitUf)
                {
                    if (unit.UnitInfo != null)
                    {
                        if (lstModifiedUnits.Contains(unit.UnitInfo) == false)
                        { lstModifiedUnits.Add(unit.UnitInfo); }
                    }
                }
            }

            // Module No.1
            cmd[20] = (byte)1;

            // H
            cmd[22] = (byte)valueH;
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 1, Edge - 1");
            }
            cmd[21] = (byte)EdgePosition.Edge_1;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 1, Edge - 2");
            }
            cmd[21] = (byte)EdgePosition.Edge_2;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }

            // V
            cmd[22] = (byte)valueV;
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 1, Edge - 3");
            }
            cmd[21] = (byte)EdgePosition.Edge_3;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 1, Edge - 5");
            }
            cmd[21] = (byte)EdgePosition.Edge_5;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }

            // Module No.2
            cmd[20] = (byte)2;

            // H
            cmd[22] = (byte)valueH;
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 2, Edge - 1");
            }
            cmd[21] = (byte)EdgePosition.Edge_1;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 2, Edge - 2");
            }
            cmd[21] = (byte)EdgePosition.Edge_2;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }

            // Module No.3
            cmd[20] = (byte)3;

            // H
            cmd[22] = (byte)valueH;
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 3, Edge - 1");
            }
            cmd[21] = (byte)EdgePosition.Edge_1;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 3, Edge - 2");
            }
            cmd[21] = (byte)EdgePosition.Edge_2;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }

            // Module No.4
            cmd[20] = (byte)4;

            // H
            cmd[22] = (byte)valueH;
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 4, Edge - 1");
            }
            cmd[21] = (byte)EdgePosition.Edge_1;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 4, Edge - 2");
            }
            cmd[21] = (byte)EdgePosition.Edge_2;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }

            // V
            cmd[22] = (byte)valueV;
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 4, Edge - 4");
            }
            cmd[21] = (byte)EdgePosition.Edge_4;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 4, Edge - 6");
            }
            cmd[21] = (byte)EdgePosition.Edge_6;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }

            // Module No.5
            cmd[20] = (byte)5;

            // H
            cmd[22] = (byte)valueH;

            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            {
	            if (winProgress != null)
	            {
	                winProgress.ShowMessage(message + " Module - 5, Edge - 7");
	            }
	            cmd[21] = (byte)EdgePosition.Edge_7;
	            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
	            if (winProgress != null)
	            {
	                winProgress.ShowMessage(message + " Module - 5, Edge - 8");
	            }
	            cmd[21] = (byte)EdgePosition.Edge_8;
	            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
            }

            // V
            cmd[22] = (byte)valueV;
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 5, Edge - 3");
            }
            cmd[21] = (byte)EdgePosition.Edge_3;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 5, Edge - 5");
            }
            cmd[21] = (byte)EdgePosition.Edge_5;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }

            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            {
                // Module No.6
	            cmd[20] = (byte)6;
	
	            // H
	            cmd[22] = (byte)valueH;
	            if (winProgress != null)
	            {
	                winProgress.ShowMessage(message + " Module - 6, Edge - 7");
	            }
	            cmd[21] = (byte)EdgePosition.Edge_7;
	            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
	            if (winProgress != null)
	            {
	                winProgress.ShowMessage(message + " Module - 6, Edge - 8");
	            }
	            cmd[21] = (byte)EdgePosition.Edge_8;
	            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }

                // Module No.7
	            cmd[20] = (byte)7;
	
	            // H
	            cmd[22] = (byte)valueH;
	            if (winProgress != null)
	            {
	                winProgress.ShowMessage(message + " Module - 7, Edge - 7");
	            }
	            cmd[21] = (byte)EdgePosition.Edge_7;
	            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
	            if (winProgress != null)
	            {
	                winProgress.ShowMessage(message + " Module - 7, Edge - 8");
	            }
	            cmd[21] = (byte)EdgePosition.Edge_8;
	            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
            }

            // Module No.8
            cmd[20] = (byte)8;

            // H
            cmd[22] = (byte)valueH;
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            {
	            if (winProgress != null)
	            {
	                winProgress.ShowMessage(message + " Module - 8, Edge - 7");
	            }
	            cmd[21] = (byte)EdgePosition.Edge_7;
	            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
	            if (winProgress != null)
	            {
	                winProgress.ShowMessage(message + " Module - 8, Edge - 8");
	            }
	            cmd[21] = (byte)EdgePosition.Edge_8;
	            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
            }

            // V
            cmd[22] = (byte)valueV;
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 8, Edge - 4");
            }
            cmd[21] = (byte)EdgePosition.Edge_4;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }
            if (winProgress != null)
            {
                winProgress.ShowMessage(message + " Module - 8, Edge - 6");
            }
            cmd[21] = (byte)EdgePosition.Edge_6;
            sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
            if (winProgress != null)
            {
                winProgress.PutForward1Step();
            }

            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
            {
                // Module No.9
                cmd[20] = (byte)9;

                // H
                cmd[22] = (byte)valueH;
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 9, Edge - 7");
                }
                cmd[21] = (byte)EdgePosition.Edge_7;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 9, Edge - 8");
                }
                cmd[21] = (byte)EdgePosition.Edge_8;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }

                // V
                cmd[22] = (byte)valueV;
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 9, Edge - 3");
                }
                cmd[21] = (byte)EdgePosition.Edge_3;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 9, Edge - 5");
                }
                cmd[21] = (byte)EdgePosition.Edge_5;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }

                // Module No.10
                cmd[20] = (byte)10;

                // H
                cmd[22] = (byte)valueH;
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 10, Edge - 7");
                }
                cmd[21] = (byte)EdgePosition.Edge_7;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 10, Edge - 8");
                }
                cmd[21] = (byte)EdgePosition.Edge_8;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }

                // Module No.11
                cmd[20] = (byte)11;

                // H
                cmd[22] = (byte)valueH;
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 11, Edge - 7");
                }
                cmd[21] = (byte)EdgePosition.Edge_7;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 11, Edge - 8");
                }
                cmd[21] = (byte)EdgePosition.Edge_8;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }

                // Module No.12
                cmd[20] = (byte)12;

                // H
                cmd[22] = (byte)valueH;
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 12, Edge - 7");
                }
                cmd[21] = (byte)EdgePosition.Edge_7;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 12, Edge - 8");
                }
                cmd[21] = (byte)EdgePosition.Edge_8;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }

                // V
                cmd[22] = (byte)valueV;
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 12, Edge - 4");
                }
                cmd[21] = (byte)EdgePosition.Edge_4;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
                if (winProgress != null)
                {
                    winProgress.ShowMessage(message + " Module - 12, Edge - 6");
                }
                cmd[21] = (byte)EdgePosition.Edge_6;
                sendSdcpCommand(cmd, sdcpWait, dicController[controllerID].IPAddress);
                if (winProgress != null)
                {
                    winProgress.PutForward1Step();
                }
            }

        }
        
        private void checkCorrectValue(int targetValue, out int correctValue)
        {
            if (targetValue < correctValue_Min)
            { correctValue = correctValue_Min; }
            else if (targetValue > correctValue_Max)
            { correctValue = correctValue_Max; }
            else
            { correctValue = targetValue; }
        }

        #endregion Private Methods
    }
}
