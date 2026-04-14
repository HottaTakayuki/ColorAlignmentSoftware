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
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using System.IO;

using SONY.Modules;

namespace CAS
{
    // Cell Replace
    public partial class MainWindow : Window
    {
        #region Fields

        private const int CELL_NO_1 = 0;
        private const int CELL_NO_2 = 1;
        private const int CELL_NO_3 = 2;
        private const int CELL_NO_4 = 3;
        private const int CELL_NO_5 = 4;
        private const int CELL_NO_6 = 5;
        private const int CELL_NO_7 = 6;
        private const int CELL_NO_8 = 7;
        private const int CELL_NO_9 = 8;
        private const int CELL_NO_10 = 9;
        private const int CELL_NO_11 = 10;
        private const int CELL_NO_12 = 11;

        #endregion Fields

        #region Events

        #endregion Events

        #region Private Method

        private void diselectAllCells()
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Dispatcher.Invoke(new Action(() => { aryCellUf[i, j].IsChecked = false; }));
                }
            }
        }

        // Cell個別調整用アルゴリズム
        private bool adjustUfCell()
        {
            if (Settings.Ins.ExecLog == true)
            {
                SaveExecLog("");
                SaveExecLog("[0] adjustUfCell start");
            }

            bool status;
            ControllerInfo controller;
            UnitInfo targetUnit;
            int targetCell = -1;
            List<UnitInfo> lstTargetUnit = new List<UnitInfo>();
            FileDirectory baseFileDir;

            // ●進捗の最大値を設定
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[1] Set Step Count."); }
            winProgress.ShowMessage("Set Step Count.");

            int step = AJUSTMENT_MODULE_STEPS;
            winProgress.SetWholeSteps(step);

            // ●Power OnでないときはOnにする [2]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[2] Check Controller Power."); }
            winProgress.ShowMessage("Check Controller Power.");

            if (!setAllControllerPowerOn())
            { return false; }
            winProgress.PutForward1Step();

            // ●調整をするCabinetをListに格納 [3]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[3] Store Target Cabinet Info."); }
            winProgress.ShowMessage("Store Target Cabinet Info.");

            var dispatcher = Application.Current.Dispatcher;
            dispatcher.Invoke(() => correctAdjustUnit(out lstTargetUnit, allocInfo.MaxX, allocInfo.MaxY));
            if (lstTargetUnit.Count == 0)
            {
                ShowMessageWindow("Please select cabinets.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            targetUnit = lstTargetUnit[0];
            winProgress.PutForward1Step();

            // ●Target（交換）Moduleを確認 [4]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[4] Store Target Module Info."); }
            winProgress.ShowMessage("Store Target Module Info.");

            dispatcher.Invoke(() => checkTargetCell(out targetCell));
            if (targetCell < 0)
            {
                ShowMessageWindow("Please select target module.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●対象Cabinetが接続されているController情報を保持する [5]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[5] Store Target Controller Info."); }
            winProgress.ShowMessage("Store Target Controller Info.");

            // 初期化
            foreach (ControllerInfo cont in dicController.Values)
            { cont.Target = false; }

            dicController[targetUnit.ControllerID].Target = true;

            controller = dicController[targetUnit.ControllerID];    // 対象Controller

            setAdjacentCellControllerToTarget(targetUnit, targetCell);
            winProgress.PutForward1Step();

            // ●調整データのバックアップがすべてあるか確認 [6]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[6] Check Backup Data."); }
            winProgress.ShowMessage("Check Backup Data.");

            status = checkDataFile(lstTargetUnit, out baseFileDir);
            if (status != true)
            {
                ShowMessageWindow("There is not the module data file.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

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

            if (setCA410SettingDispatch() != true)
            {
                ShowMessageWindow("Can not set CA-410 settings.\r\nPlease check it status.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●調整
            return adjustUfCellReplace_SecondPeriod(controller, targetUnit, targetCell, baseFileDir, false);
        }
        
        private bool adjustUfCellReplace_SecondPeriod(ControllerInfo controller, UnitInfo targetUnit, int targetCell, FileDirectory baseFileDir, bool cellReplace = true)
        {
            bool status;
            List<UserSetting> lstUserSetting;
            double[][][] measureData;

#if NO_WRITE
#else
            // ●Backupを取得 [9]
            winProgress.ShowMessage("Make Backup.");
            if (cellReplace == true) // Cell Replaceの時はバックアップを取得する
            {
                //if (backup(BackupMode.CellReplace) != true) // Cell Dataのみを取得するフラグ
                if(copyData(targetUnit) != true)
                { return false; }
            }
            winProgress.PutForward1Step();
#endif

            // ●調整データのバックアップがあるか確認 [10]
            winProgress.ShowMessage("Check Backup Data.");
            status = checkCellDataFile(targetUnit, cellReplace, baseFileDir);
            if (status != true)
            {
                ShowMessageWindow("There is not the module data file.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            // ●FTP ON [*1]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*1] FTP On."); }
            winProgress.ShowMessage("FTP On.");

            if (sendSdcpCommand(SDCPClass.CmdFtpOn, controller.IPAddress) != true)
            { return false; }
            winProgress.PutForward1Step();

            // ●Model名取得 [*2]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*2] Get Model Name."); }
            winProgress.ShowMessage("Get Model Name.");

            if (string.IsNullOrWhiteSpace(controller.ModelName) == true)
            { controller.ModelName = getModelName(controller.IPAddress); }

            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("     Model Name : " + controller.ModelName); }
            winProgress.PutForward1Step();

            // ●Serial取得 [*3]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*3] Get Serial No."); }
            winProgress.ShowMessage("Get Serial No.");

            if (string.IsNullOrWhiteSpace(controller.SerialNo) == true)
            { controller.SerialNo = getSerialNo(controller.IPAddress); }

            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("     Serial Num. : " + controller.SerialNo); }
            winProgress.PutForward1Step();

            // ●Tempフォルダ内のファイルを削除 [*4]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*4] Delete Temporary Files."); }
            winProgress.ShowMessage("Delete Temporary Files.");

            string tempPath = applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo + "_Temp";

            if (Directory.Exists(tempPath) != true)
            { Directory.CreateDirectory(tempPath); }

            string[] files = Directory.GetFiles(tempPath, "*", System.IO.SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                try { File.Delete(files[i]); }
                catch { return false; }
            }
            winProgress.PutForward1Step();

            // ●User設定保存 [9]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[9] Store User Settings."); }
            winProgress.ShowMessage("Store User Settings.");

            try
            {
                if (getUserSetting(out lstUserSetting) != true)
                { return false; }
                m_lstUserSetting = lstUserSetting;
            }
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }
            winProgress.PutForward1Step();

            // ●調整用設定 [10]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[10] Set Adjust Settings."); }
            winProgress.ShowMessage("Set Adjust Settings.");

            setAdjustSetting();
            winProgress.PutForward1Step();

            // ●Layout情報Off [11]
            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdDispUnitAddrOff, 0, cont.IPAddress); }

            // ●色度測定 [*1]
            UserOperation operation = UserOperation.None;

            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*1] Measure Module Color."); }
            winProgress.ShowMessage("Measure Module Color.");

            while (true)
            {
                status = measureCell(targetUnit, targetCell, out measureData, out operation);

                if (status == true)
                { break; }
                else if(operation == UserOperation.Cancel)
                { return false; }
                else if (operation == UserOperation.Repeat)
                { continue; }
                else
                {
                    bool? result;
                    string msg = "Color measurement failed.\r\nDo you want to continue the measurement?";

                    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                    if (result != true)
                    { return false; }
                }
            }

            winProgress.PutForward1Step();

            //推定処理時間
            int processSec = 0;
            int currentStep = 0;
            var dispatcher = Application.Current.Dispatcher;

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfCellProcessSec(currentStep);
            winProgress.StartRemainTimer(processSec);

            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
            {
                // クロストーク補正量算出 [*2]
                if (Settings.Ins.ExecLog == true)
                { SaveExecLog("[*2] Calc Crosstalk Correction."); }
                winProgress.ShowMessage("Calc Crosstalk Correction.");

                List<int> lstTargetCell = Enumerable.Range(0, 0).ToList();
                lstTargetCell.Add(targetCell);

                status = m_MakeUFData.CalcUncCrosstalk(lstTargetCell, measureData);
                if (!status)
                {
                    ShowMessageWindow("Failed in calc crosstalk correction.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }
            }

            // ●目標値設定 [*3]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*3] Set Target Color."); }
            winProgress.ShowMessage("Set Target Color.");

            //m_MakeUFData.SetTargetValue(m_Target_xr, m_Target_yr, m_Target_xg, m_Target_yg, m_Target_xb, m_Target_yb, m_Target_Yw, m_Target_xw, m_Target_yw);
            m_MakeUFData.SetTargetValue(ufTargetChrom.Red.x, ufTargetChrom.Red.y, ufTargetChrom.Green.x, ufTargetChrom.Green.y, ufTargetChrom.Blue.x, ufTargetChrom.Blue.y, ufTargetChrom.White.Lv, ufTargetChrom.White.x, ufTargetChrom.White.y);
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfCellProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // ●Cell Dataの補正データ抽出 [*4]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*4] Extract Correction Data."); }
            winProgress.ShowMessage("Extract Correction Data.");

            string filePath;
            if (cellReplace == true)
            { filePath = makeFilePath(targetUnit, FileDirectory.CellReplace); }
            else
            { filePath = makeFilePath(targetUnit, baseFileDir); }

            status = m_MakeUFData.ExtractFmt(filePath, allocInfo.LEDModel);
            if (status != true)
            {
                ShowMessageWindow("Failed in ExtractFmt.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfCellProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // ●抽出した補正データと調整目標値から逆算してその時のXYZの値を求める [*5]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*5] Calc XYZ."); }
            winProgress.ShowMessage("Calc XYZ.");

            //status = m_MakeUFData.Fmt2XYZ(m_Target_xr, m_Target_yr, m_Target_xg, m_Target_yg, m_Target_xb, m_Target_yb, m_Target_Yw, m_Target_xw, m_Target_yw);
            status = m_MakeUFData.Fmt2XYZ(ufTargetChrom.Red.x, ufTargetChrom.Red.y, ufTargetChrom.Green.x, ufTargetChrom.Green.y, ufTargetChrom.Blue.x, ufTargetChrom.Blue.y, ufTargetChrom.White.Lv, ufTargetChrom.White.x, ufTargetChrom.White.y);
            if (status != true)
            {
                ShowMessageWindow("Failed in Fmt2XYZ.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfCellProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // ●今回の計測値と前の計測値の差分を求めて、画素ごとのXYZデータを更新する [*6]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*6] Calc New XYZ Data."); }
            winProgress.ShowMessage("Calc New XYZ Data.");

            status = m_MakeUFData.Compensate_XYZ_CellReplace(measureData, targetCell);
            if (status != true)
            {
                ShowMessageWindow("Failed in Compensate_XYZ.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfCellProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // ●補正データを作成する [*7]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*7] Make New Correction Data."); }
            winProgress.ShowMessage("Make New Correction Data.");

            double targetYw, targetYr, targetYg, targetYb;
            int ucr, ucg, ucb;
            status = m_MakeUFData.Statistics(-1, out targetYw, out targetYr, out targetYg, out targetYb, out ucr, out ucg, out ucb);
            if (status != true)
            {
                ShowMessageWindow("Failed in Statistics.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfCellProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // ●ファイル保存 [*8]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[*8] Save New Module Data."); }
            winProgress.ShowMessage("Save New Module Data.");

            string adjustedFile = makeFilePath(targetUnit, FileDirectory.Temp);

            // フォルダがない場合、フォルダ作成
            if (System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(adjustedFile)) != true)
            { System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(adjustedFile)); }

            try
            {
                status = Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2
                    ? m_MakeUFData.OverWritePixelData(adjustedFile, allocInfo.LEDModel)
                    : m_MakeUFData.OverWritePixelDataWithCrosstalk(adjustedFile, allocInfo.LEDModel);

                if (status != true)
                {
                    ShowMessageWindow("Failed in OverWritePixelData.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }
            }
            catch
            {
                ShowMessageWindow("Failed in OverWritePixelData.", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfCellProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // TestPattern OFF [12]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[12] Test Pattern Off."); }
            winProgress.ShowMessage(" Test Pattern Off.");

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(SDCPClass.CmdIntSignalOff, 0, cont.IPAddress); }

            // ●調整済みファイルの移動 [13]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[13] Move Adjusted Files."); }
            winProgress.ShowMessage("Move Adjusted Files.");

            try
            {
                if (putFileFtpRetry(controller.IPAddress, adjustedFile) != true)
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
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfCellProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // ●Cabinet Power Off [14]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[14] Cabinet Power Off."); }
            winProgress.ShowMessage("Cabinet Power Off.");

            sendSdcpCommand(SDCPClass.CmdUnitPowerOff, controller.IPAddress);
            System.Threading.Thread.Sleep(SLEEP_TIME_AFTER_PANEL_OFF);

            if (getUnitPowerStatus() != true)
            {
                string msg = "Failed to cabinet power off.";
                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfCellProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);


#if NO_WRITE
#else
            // ●Reconfig [15]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[15] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            sendReconfig();
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfCellProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // ●書き込みコマンド発行 [16]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[16] Send Write Command."); }
            winProgress.ShowMessage("Send Write Command.");

            sendSdcpCommand(SDCPClass.CmdDataWrite, controller.IPAddress);
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfCellProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // ●書き込みComplete待ち [17]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[17] Waiting for the process of controller."); }
            winProgress.ShowMessage("Waiting for the process of controller.");

            try
            {
                while (true)
                {
                    if (checkCompleteFtp(controller.IPAddress, "write_complete") == true)
                    { break; }

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
            
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfCellProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

#endif

            // ●調整設定解除 [18]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[18] Set Normal Setting."); }
            winProgress.ShowMessage("Set Normal Setting.");

            setNormalSetting();
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfCellProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // ●User設定に戻す [19]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[19] Restore User Settings."); }
            winProgress.ShowMessage("Restore User Settings.");

            setUserSetting(m_lstUserSetting);
            m_lstUserSetting = null;    //User設定を戻したので呼び出し側の設定処理をスキップ
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfCellProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

#if NO_WRITE
#else
            // ●Latest → Previousフォルダへコピー [20]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[20] Copy Latest Files."); }
            winProgress.ShowMessage("Copy Latest Files.");

            copyLatest2Previous(applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo);
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfCellProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // ●Temp → Latestフォルダへコピー [21]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[21] Copy Temporary Files."); }
            winProgress.ShowMessage("Copy Temporary Files.");

            copyTemp2Latest(applicationPath + "\\Backup\\" + controller.ModelName + "_" + controller.SerialNo);
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfCellProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // ●Reconfig [22]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[22] Send Reconfig."); }
            winProgress.ShowMessage("Send Reconfig.");

            sendReconfig();
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfCellProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

#endif

            // ●Tempフォルダのファイルを削除 [23]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[23] Delete Temporary Files."); }
            winProgress.ShowMessage("Delete Temporary Files.");

            files = Directory.GetFiles(tempPath, "*", System.IO.SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                try 
                {
                    if (Settings.Ins.ExecLog == true)
                    {
                        SaveExecLog("Delete : " + files[i]);
                    }
                    File.Delete(files[i]);
                }
                catch { return false; }
            }
            winProgress.PutForward1Step();

            dispatcher.Invoke(() => winProgress.GetWholeProgress(out currentStep));
            processSec = calcAdjustUfCellProcessSec(currentStep);
            winProgress.SetRemainTimer(processSec);

            // ●FTP Off [24]
            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("[24] FTP Off."); }
            winProgress.ShowMessage("FTP Off.");

            if (sendSdcpCommand(SDCPClass.CmdFtpOff, controller.IPAddress) != true)
            {
                ShowMessageWindow("Failed to FTP Off.", "Error!", System.Drawing.SystemIcons.Error, 420, 180);
                return false;
            }
            winProgress.PutForward1Step();

            return true;
        }

        private bool openCellFile(out string FilePath)
        {
            FilePath = "";

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.FilterIndex = 1;
            openFileDialog.Filter = "Module File(.bin)|*.bin|All Files (*.*)|*.*";

            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                FilePath = openFileDialog.FileName;
            }
            else { return false; }

            return true;
        }

        private bool checkTargetCell(out int targetCellNo)
        {
            targetCellNo = -1;

            for (int i = 0; i < 4; i++) // Moduleの横の数
            {
                for (int j = 0; j < 3; j++) // Moduleの縦の数
                {
                    if (aryCellUf[i, j].IsChecked == true)
                    {
                        targetCellNo = 4 * j + i;
                        break;
                    }
                }

                if(targetCellNo >= 0)
                { break; }
            }

            return true;
        }

        private void setAdjacentCellControllerToTarget(UnitInfo targetUnit, int targetCell)
        {
            UnitInfo adjacentUnit;

            // Top Module
            storeReferenceCell(targetUnit, targetCell, GainReferenceCellPosition.Top, out adjacentUnit, out _);

            if (adjacentUnit != null && targetUnit.ControllerID != adjacentUnit.ControllerID)
            { dicController[adjacentUnit.ControllerID].Target = true; }

            // Right Module
            storeReferenceCell(targetUnit, targetCell, GainReferenceCellPosition.Right, out adjacentUnit, out _);

            if (adjacentUnit != null && targetUnit.ControllerID != adjacentUnit.ControllerID)
            { dicController[adjacentUnit.ControllerID].Target = true; }

            // Bottom Module
            storeReferenceCell(targetUnit, targetCell, GainReferenceCellPosition.Bottom, out adjacentUnit, out _);

            if (adjacentUnit != null && targetUnit.ControllerID != adjacentUnit.ControllerID)
            { dicController[adjacentUnit.ControllerID].Target = true; }

            // Left Module
            storeReferenceCell(targetUnit, targetCell, GainReferenceCellPosition.Left, out adjacentUnit, out _);

            if (adjacentUnit != null && targetUnit.ControllerID != adjacentUnit.ControllerID)
            { dicController[adjacentUnit.ControllerID].Target = true; }
        }

        private bool checkCellDataFile(UnitInfo unit, bool cellReplace, FileDirectory baseFileDir)
        {
            string filePath;

            if (cellReplace == true)
            { filePath = makeFilePath(unit, FileDirectory.CellReplace); }
            else
            { filePath = makeFilePath(unit, baseFileDir); }

            if (System.IO.File.Exists(filePath) != true)
            { return false; }

            return true;
        }

        private bool copyData(UnitInfo unit)
        {
            string backupDirCell = System.IO.Path.GetDirectoryName(makeFilePath(unit, FileDirectory.CellReplace, DataType.HcData));
            //applicationPath + "\\CellReplace\\" + dicController[unit.ControllerID].ModelName + "_" + dicController[unit.ControllerID].SerialNo;
            
            // ファイル削除
            if (Directory.Exists(backupDirCell) == true)
            {
                string[] files = Directory.GetFiles(backupDirCell, "*", System.IO.SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    try { FileSystem.DeleteFile(files[i], UIOption.OnlyErrorDialogs, RecycleOption.DeletePermanently); }
                    catch { }
                }
            }
            else
            { Directory.CreateDirectory(backupDirCell); }

            string srcFile = makeFilePath(unit, FileDirectory.Temp, DataType.HcData);
            string destFile = makeFilePath(unit, FileDirectory.CellReplace, DataType.HcData);

            try { File.Copy(srcFile, destFile); }
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }

            return true;
        }

        private bool measureCell(UnitInfo unit, int targetCell, out double[][][] measureData, out UserOperation operation)
        {
            bool status;
            double[][][] pointData = new double[5][][];
            NeighboringCells neighboringCells;

            measureData = new double[0][][];
            operation = UserOperation.None;

#if DEBUG
            Console.WriteLine($"Target Cabinet: C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}");
            SaveExecLog($"     Target Cabinet: C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}");
#elif Release_log
            SaveExecLog($"     Target Cabinet: C{unit.ControllerID}-{unit.PortNo}-{unit.UnitNo}");
#endif

            // 隣接するCellを確認
            status = checkNeighboringCells(unit, targetCell, out neighboringCells);
            if (status != true)
            {
                ShowMessageWindow("Failed in checkNeighboringModules().", "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }

            // Center
            while (true)
            {

#if DEBUG
                Console.WriteLine($"Target Module No.{targetCell + 1}");
                SaveExecLog($"     Target Module No.{targetCell + 1}");
#elif Release_log
                SaveExecLog($"     Target Module No.{targetCell + 1}");
#endif

                status = measurePoint(unit, targetCell, CrossPointPosition.Center, out pointData[0], out operation);

                if (status == true)
                { break; }
                else if (operation == UserOperation.Cancel)
                {
                    bool? result;
                    string msg = "Do you want to finish the measurement ?";

                    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                    if (result != true)
                    { continue; }

                    return false;
                }
                else { return false; }
            }

            // Top
            if (neighboringCells.UpperCell.UnitInfo != null)
            {
                while (true)
                {

#if DEBUG
                    Console.WriteLine($"Top Module No.{neighboringCells.UpperCell.CellNo + 1}");
                    SaveExecLog($"     Top Module No.{neighboringCells.UpperCell.CellNo + 1}");
#elif Release_log
                    SaveExecLog($"     Top Module No.{neighboringCells.UpperCell.CellNo + 1}");
#endif

                    status = measurePoint(neighboringCells.UpperCell.UnitInfo, neighboringCells.UpperCell.CellNo, CrossPointPosition.Bottom, out pointData[1], out operation);

                    if (status == true)
                    { break; }
                    else if (operation == UserOperation.Cancel)
                    {
                        bool? result;
                        string msg = "Do you want to finish the measurement ?";

                        showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                        if (result != true)
                        { continue; }

                        return false;
                    }
                    else { return false; }
                }
            }
            else { pointData[1] = null; }

            // Right
            if (neighboringCells.RightCell.UnitInfo != null)
            {
                while (true)
                {

#if DEBUG
                    Console.WriteLine($"Right Module No.{neighboringCells.RightCell.CellNo + 1}");
                    SaveExecLog($"     Right Module No.{neighboringCells.RightCell.CellNo + 1}");
#elif Release_log
                    SaveExecLog($"     Right Module No.{neighboringCells.RightCell.CellNo + 1}");
#endif

                    status = measurePoint(neighboringCells.RightCell.UnitInfo, neighboringCells.RightCell.CellNo, CrossPointPosition.Left, out pointData[2], out operation);

                    if (status == true)
                    { break; }
                    else if (operation == UserOperation.Cancel)
                    {
                        bool? result;
                        string msg = "Do you want to finish the measurement ?";

                        showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                        if (result != true)
                        { continue; }

                        return false;
                    }
                    else { return false; }
                }
            }
            else { pointData[2] = null; }

            // Bottom
            if (neighboringCells.DownwardCell.UnitInfo != null)
            {
                while (true)
                {

#if DEBUG
                    Console.WriteLine($"Bottom Module No.{neighboringCells.DownwardCell.CellNo + 1}");
                    SaveExecLog($"     Bottom Module No.{neighboringCells.DownwardCell.CellNo + 1}");
#elif Release_log
                    SaveExecLog($"     Bottom Module No.{neighboringCells.DownwardCell.CellNo + 1}");
#endif

                    status = measurePoint(neighboringCells.DownwardCell.UnitInfo, neighboringCells.DownwardCell.CellNo, CrossPointPosition.Top, out pointData[3], out operation);

                    if (status == true)
                    { break; }
                    else if (operation == UserOperation.Cancel)
                    {
                        bool? result;
                        string msg = "Do you want to finish the measurement ?";

                        showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                        if (result != true)
                        { continue; }

                        return false;
                    }
                    else { return false; }
                }
            }
            else { pointData[3] = null; }

            // Left
            if (neighboringCells.LeftCell.UnitInfo != null)
            {
                while (true)
                {

#if DEBUG
                    Console.WriteLine($"Left Module No.{neighboringCells.LeftCell.CellNo + 1}");
                    SaveExecLog($"     Left Module No.{neighboringCells.LeftCell.CellNo + 1}");
#elif Release_log
                    SaveExecLog($"     Left Module No.{neighboringCells.LeftCell.CellNo + 1}");
#endif

                    status = measurePoint(neighboringCells.LeftCell.UnitInfo, neighboringCells.LeftCell.CellNo, CrossPointPosition.Right, out pointData[4], out operation);

                    if (status == true)
                    { break; }
                    else if (operation == UserOperation.Cancel)
                    {
                        bool? result;
                        string msg = "Do you want to finish the measurement ?";

                        showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 350, 180, "Yes");

                        if (result != true)
                        { continue; }

                        return false;
                    }
                    else { return false; }
                }
            }
            else { pointData[4] = null; }

            // Calc Cell Color Data
            copyColorData(pointData, out measureData);

            // TestPattern OFF
            //foreach (ControllerInfo cont in dicController.Values)
            //{ sendSdcpCommand(SDCPClass.CmdIntSignalOff, cont.IPAddress); }

            return true;
        }

        private bool checkNeighboringCells(UnitInfo unit, int targetCell, out NeighboringCells neighboringCells)
        {
            neighboringCells = new NeighboringCells();

            UnitInfo upperUnit, downwardUnit, rightUnit, leftUnit;

            // Upper
            try { upperUnit = aryUnitUf[unit.X - 1, unit.Y - 2].UnitInfo; }
            catch { upperUnit = null; }

            // Downward
            try { downwardUnit = aryUnitUf[unit.X - 1, unit.Y].UnitInfo; }
            catch { downwardUnit = null; }

            // Right
            try { rightUnit = aryUnitUf[unit.X, unit.Y - 1].UnitInfo; }
            catch { rightUnit = null; }

            // Left
            try { leftUnit = aryUnitUf[unit.X - 2, unit.Y - 1].UnitInfo; }
            catch { leftUnit = null; }

            if (targetCell == -1)
            {
                neighboringCells.UpperCell.UnitInfo = upperUnit;
                neighboringCells.UpperCell.CellNo = -1;
                neighboringCells.DownwardCell.UnitInfo = downwardUnit;
                neighboringCells.DownwardCell.CellNo = -1;
                neighboringCells.RightCell.UnitInfo = rightUnit;
                neighboringCells.RightCell.CellNo = -1;
                neighboringCells.LeftCell.UnitInfo = leftUnit;
                neighboringCells.LeftCell.CellNo = -1;
            }
            else
            {
                if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
                {
                    switch (targetCell)
                    {
                        case 0:
                            neighboringCells.UpperCell.UnitInfo = upperUnit;
                            neighboringCells.UpperCell.CellNo = CELL_NO_5;
                            neighboringCells.DownwardCell.UnitInfo = unit;
                            neighboringCells.DownwardCell.CellNo = CELL_NO_5;
                            neighboringCells.RightCell.UnitInfo = unit;
                            neighboringCells.RightCell.CellNo = CELL_NO_2;
                            neighboringCells.LeftCell.UnitInfo = leftUnit;
                            neighboringCells.LeftCell.CellNo = CELL_NO_4;
                            break;
                        case 1:
                            neighboringCells.UpperCell.UnitInfo = upperUnit;
                            neighboringCells.UpperCell.CellNo = CELL_NO_6;
                            neighboringCells.DownwardCell.UnitInfo = unit;
                            neighboringCells.DownwardCell.CellNo = CELL_NO_6;
                            neighboringCells.RightCell.UnitInfo = unit;
                            neighboringCells.RightCell.CellNo = CELL_NO_3;
                            neighboringCells.LeftCell.UnitInfo = unit;
                            neighboringCells.LeftCell.CellNo = CELL_NO_1;
                            break;
                        case 2:
                            neighboringCells.UpperCell.UnitInfo = upperUnit;
                            neighboringCells.UpperCell.CellNo = CELL_NO_7;
                            neighboringCells.DownwardCell.UnitInfo = unit;
                            neighboringCells.DownwardCell.CellNo = CELL_NO_7;
                            neighboringCells.RightCell.UnitInfo = unit;
                            neighboringCells.RightCell.CellNo = CELL_NO_4;
                            neighboringCells.LeftCell.UnitInfo = unit;
                            neighboringCells.LeftCell.CellNo = CELL_NO_2;
                            break;
                        case 3:
                            neighboringCells.UpperCell.UnitInfo = upperUnit;
                            neighboringCells.UpperCell.CellNo = CELL_NO_8;
                            neighboringCells.DownwardCell.UnitInfo = unit;
                            neighboringCells.DownwardCell.CellNo = CELL_NO_8;
                            neighboringCells.RightCell.UnitInfo = rightUnit;
                            neighboringCells.RightCell.CellNo = CELL_NO_1;
                            neighboringCells.LeftCell.UnitInfo = unit;
                            neighboringCells.LeftCell.CellNo = CELL_NO_3;
                            break;
                        case 4:
                            neighboringCells.UpperCell.UnitInfo = unit;
                            neighboringCells.UpperCell.CellNo = CELL_NO_1;
                            neighboringCells.DownwardCell.UnitInfo = downwardUnit;
                            neighboringCells.DownwardCell.CellNo = CELL_NO_1;
                            neighboringCells.RightCell.UnitInfo = unit;
                            neighboringCells.RightCell.CellNo = CELL_NO_6;
                            neighboringCells.LeftCell.UnitInfo = leftUnit;
                            neighboringCells.LeftCell.CellNo = CELL_NO_8;
                            break;
                        case 5:
                            neighboringCells.UpperCell.UnitInfo = unit;
                            neighboringCells.UpperCell.CellNo = CELL_NO_2;
                            neighboringCells.DownwardCell.UnitInfo = downwardUnit;
                            neighboringCells.DownwardCell.CellNo = CELL_NO_2;
                            neighboringCells.RightCell.UnitInfo = unit;
                            neighboringCells.RightCell.CellNo = CELL_NO_7;
                            neighboringCells.LeftCell.UnitInfo = unit;
                            neighboringCells.LeftCell.CellNo = CELL_NO_5;
                            break;
                        case 6:
                            neighboringCells.UpperCell.UnitInfo = unit;
                            neighboringCells.UpperCell.CellNo = CELL_NO_3;
                            neighboringCells.DownwardCell.UnitInfo = downwardUnit;
                            neighboringCells.DownwardCell.CellNo = CELL_NO_3;
                            neighboringCells.RightCell.UnitInfo = unit;
                            neighboringCells.RightCell.CellNo = CELL_NO_8;
                            neighboringCells.LeftCell.UnitInfo = unit;
                            neighboringCells.LeftCell.CellNo = CELL_NO_6;
                            break;
                        case 7:
                            neighboringCells.UpperCell.UnitInfo = unit;
                            neighboringCells.UpperCell.CellNo = CELL_NO_4;
                            neighboringCells.DownwardCell.UnitInfo = downwardUnit;
                            neighboringCells.DownwardCell.CellNo = CELL_NO_4;
                            neighboringCells.RightCell.UnitInfo = rightUnit;
                            neighboringCells.RightCell.CellNo = CELL_NO_5;
                            neighboringCells.LeftCell.UnitInfo = unit;
                            neighboringCells.LeftCell.CellNo = CELL_NO_7;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    switch (targetCell)
                    {
                        case 0:
                            neighboringCells.UpperCell.UnitInfo = upperUnit;
                            neighboringCells.UpperCell.CellNo = CELL_NO_9;
                            neighboringCells.DownwardCell.UnitInfo = unit;
                            neighboringCells.DownwardCell.CellNo = CELL_NO_5;
                            neighboringCells.RightCell.UnitInfo = unit;
                            neighboringCells.RightCell.CellNo = CELL_NO_2;
                            neighboringCells.LeftCell.UnitInfo = leftUnit;
                            neighboringCells.LeftCell.CellNo = CELL_NO_4;
                            break;
                        case 1:
                            neighboringCells.UpperCell.UnitInfo = upperUnit;
                            neighboringCells.UpperCell.CellNo = CELL_NO_10;
                            neighboringCells.DownwardCell.UnitInfo = unit;
                            neighboringCells.DownwardCell.CellNo = CELL_NO_6;
                            neighboringCells.RightCell.UnitInfo = unit;
                            neighboringCells.RightCell.CellNo = CELL_NO_3;
                            neighboringCells.LeftCell.UnitInfo = unit;
                            neighboringCells.LeftCell.CellNo = CELL_NO_1;
                            break;
                        case 2:
                            neighboringCells.UpperCell.UnitInfo = upperUnit;
                            neighboringCells.UpperCell.CellNo = CELL_NO_11;
                            neighboringCells.DownwardCell.UnitInfo = unit;
                            neighboringCells.DownwardCell.CellNo = CELL_NO_7;
                            neighboringCells.RightCell.UnitInfo = unit;
                            neighboringCells.RightCell.CellNo = CELL_NO_4;
                            neighboringCells.LeftCell.UnitInfo = unit;
                            neighboringCells.LeftCell.CellNo = CELL_NO_2;
                            break;
                        case 3:
                            neighboringCells.UpperCell.UnitInfo = upperUnit;
                            neighboringCells.UpperCell.CellNo = CELL_NO_12;
                            neighboringCells.DownwardCell.UnitInfo = unit;
                            neighboringCells.DownwardCell.CellNo = CELL_NO_8;
                            neighboringCells.RightCell.UnitInfo = rightUnit;
                            neighboringCells.RightCell.CellNo = CELL_NO_1;
                            neighboringCells.LeftCell.UnitInfo = unit;
                            neighboringCells.LeftCell.CellNo = CELL_NO_3;
                            break;
                        case 4:
                            neighboringCells.UpperCell.UnitInfo = unit;
                            neighboringCells.UpperCell.CellNo = CELL_NO_1;
                            neighboringCells.DownwardCell.UnitInfo = unit;
                            neighboringCells.DownwardCell.CellNo = CELL_NO_9;
                            neighboringCells.RightCell.UnitInfo = unit;
                            neighboringCells.RightCell.CellNo = CELL_NO_6;
                            neighboringCells.LeftCell.UnitInfo = leftUnit;
                            neighboringCells.LeftCell.CellNo = CELL_NO_8;
                            break;
                        case 5:
                            neighboringCells.UpperCell.UnitInfo = unit;
                            neighboringCells.UpperCell.CellNo = CELL_NO_2;
                            neighboringCells.DownwardCell.UnitInfo = unit;
                            neighboringCells.DownwardCell.CellNo = CELL_NO_10;
                            neighboringCells.RightCell.UnitInfo = unit;
                            neighboringCells.RightCell.CellNo = CELL_NO_7;
                            neighboringCells.LeftCell.UnitInfo = unit;
                            neighboringCells.LeftCell.CellNo = CELL_NO_5;
                            break;
                        case 6:
                            neighboringCells.UpperCell.UnitInfo = unit;
                            neighboringCells.UpperCell.CellNo = CELL_NO_3;
                            neighboringCells.DownwardCell.UnitInfo = unit;
                            neighboringCells.DownwardCell.CellNo = CELL_NO_11;
                            neighboringCells.RightCell.UnitInfo = unit;
                            neighboringCells.RightCell.CellNo = CELL_NO_8;
                            neighboringCells.LeftCell.UnitInfo = unit;
                            neighboringCells.LeftCell.CellNo = CELL_NO_6;
                            break;
                        case 7:
                            neighboringCells.UpperCell.UnitInfo = unit;
                            neighboringCells.UpperCell.CellNo = CELL_NO_4;
                            neighboringCells.DownwardCell.UnitInfo = unit;
                            neighboringCells.DownwardCell.CellNo = CELL_NO_12;
                            neighboringCells.RightCell.UnitInfo = rightUnit;
                            neighboringCells.RightCell.CellNo = CELL_NO_5;
                            neighboringCells.LeftCell.UnitInfo = unit;
                            neighboringCells.LeftCell.CellNo = CELL_NO_7;
                            break;
                        case 8:
                            neighboringCells.UpperCell.UnitInfo = unit;
                            neighboringCells.UpperCell.CellNo = CELL_NO_5;
                            neighboringCells.DownwardCell.UnitInfo = downwardUnit;
                            neighboringCells.DownwardCell.CellNo = CELL_NO_1;
                            neighboringCells.RightCell.UnitInfo = unit;
                            neighboringCells.RightCell.CellNo = CELL_NO_10;
                            neighboringCells.LeftCell.UnitInfo = leftUnit;
                            neighboringCells.LeftCell.CellNo = CELL_NO_12;
                            break;
                        case 9:
                            neighboringCells.UpperCell.UnitInfo = unit;
                            neighboringCells.UpperCell.CellNo = CELL_NO_6;
                            neighboringCells.DownwardCell.UnitInfo = downwardUnit;
                            neighboringCells.DownwardCell.CellNo = CELL_NO_2;
                            neighboringCells.RightCell.UnitInfo = unit;
                            neighboringCells.RightCell.CellNo = CELL_NO_11;
                            neighboringCells.LeftCell.UnitInfo = unit;
                            neighboringCells.LeftCell.CellNo = CELL_NO_9;
                            break;
                        case 10:
                            neighboringCells.UpperCell.UnitInfo = unit;
                            neighboringCells.UpperCell.CellNo = CELL_NO_7;
                            neighboringCells.DownwardCell.UnitInfo = downwardUnit;
                            neighboringCells.DownwardCell.CellNo = CELL_NO_3;
                            neighboringCells.RightCell.UnitInfo = unit;
                            neighboringCells.RightCell.CellNo = CELL_NO_12;
                            neighboringCells.LeftCell.UnitInfo = unit;
                            neighboringCells.LeftCell.CellNo = CELL_NO_10;
                            break;
                        case 11:
                            neighboringCells.UpperCell.UnitInfo = unit;
                            neighboringCells.UpperCell.CellNo = CELL_NO_8;
                            neighboringCells.DownwardCell.UnitInfo = downwardUnit;
                            neighboringCells.DownwardCell.CellNo = CELL_NO_4;
                            neighboringCells.RightCell.UnitInfo = rightUnit;
                            neighboringCells.RightCell.CellNo = CELL_NO_9;
                            neighboringCells.LeftCell.UnitInfo = unit;
                            neighboringCells.LeftCell.CellNo = CELL_NO_11;
                            break;
                        default:
                            break;
                    }
                }
            }

            return true;
        }

        private bool measurePoint(UnitInfo unit, int targetCell, CrossPointPosition point, out double[][] measureData, out UserOperation operation)
        {
            bool status;
            UserOperation ope = UserOperation.None;

            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            {
                measureData = new double[3][];
                for (int rgb = 0; rgb < 3; rgb++)
                { measureData[rgb] = new double[3]; }
            }
            else
            {
                measureData = new double[4][];
                for (int rgbw = 0; rgbw < 4; rgbw++)
                { measureData[rgbw] = new double[3]; }
            }

            outputCross(unit, targetCell, point);

            status = Dispatcher.Invoke(() => showMeasureWindow(out ope, false));
            operation = ope;

            if (status != true)
            { return false; }

            // Red
            outputWindow(unit, targetCell, point, CellColor.Red);
            System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

            status = measureColor(ufTargetChrom.Red, out measureData[0], CaSdk.DisplayMode.DispModeXYZ, ref operation);
            if (status != true)
            { return false; }

#if DEBUG
            Console.WriteLine($"R(X): {measureData[RED][XYZ_X]}\r\nR(Y): {measureData[RED][XYZ_Y]}\r\nR(Z): {measureData[RED][XYZ_Z]}");
            SaveExecLog($"      R(X): {measureData[RED][XYZ_X]}");
            SaveExecLog($"      R(Y): {measureData[RED][XYZ_Y]}");
            SaveExecLog($"      R(Z): {measureData[RED][XYZ_Z]}");
#elif Release_log
            SaveExecLog($"      R(X): {measureData[RED][XYZ_X]}");
            SaveExecLog($"      R(Y): {measureData[RED][XYZ_Y]}");
            SaveExecLog($"      R(Z): {measureData[RED][XYZ_Z]}");
#endif

            playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

            // Green
            outputWindow(unit, targetCell, point, CellColor.Green);
            System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

            status = measureColor(ufTargetChrom.Green, out measureData[1], CaSdk.DisplayMode.DispModeXYZ, ref operation);
            if (status != true)
            { return false; }

#if DEBUG
            Console.WriteLine($"G(X): {measureData[GREEN][XYZ_X]}\r\nG(Y): {measureData[GREEN][XYZ_Y]}\r\nG(Z): {measureData[GREEN][XYZ_Z]}");
            SaveExecLog($"      G(X): {measureData[GREEN][XYZ_X]}");
            SaveExecLog($"      G(Y): {measureData[GREEN][XYZ_Y]}");
            SaveExecLog($"      G(Z): {measureData[GREEN][XYZ_Z]}");
#elif Release_log
            SaveExecLog($"      G(X): {measureData[GREEN][XYZ_X]}");
            SaveExecLog($"      G(Y): {measureData[GREEN][XYZ_Y]}");
            SaveExecLog($"      G(Z): {measureData[GREEN][XYZ_Z]}");
#endif

            playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

            // Blue
            outputWindow(unit, targetCell, point, CellColor.Blue);
            System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

            status = measureColor(ufTargetChrom.Blue, out measureData[2], CaSdk.DisplayMode.DispModeXYZ, ref operation);
            if (status != true)
            { return false; }

#if DEBUG
            Console.WriteLine($"B(X): {measureData[BLUE][XYZ_X]}\r\nB(Y): {measureData[BLUE][XYZ_Y]}\r\nB(Z): {measureData[BLUE][XYZ_Z]}");
            SaveExecLog($"      B(X): {measureData[BLUE][XYZ_X]}");
            SaveExecLog($"      B(Y): {measureData[BLUE][XYZ_Y]}");
            SaveExecLog($"      B(Z): {measureData[BLUE][XYZ_Z]}");
#elif Release_log
            SaveExecLog($"      B(X): {measureData[BLUE][XYZ_X]}");
            SaveExecLog($"      B(Y): {measureData[BLUE][XYZ_Y]}");
            SaveExecLog($"      B(Z): {measureData[BLUE][XYZ_Z]}");
#endif

            // White
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
            {
                playSound(applicationPath + "\\Components\\Sound\\button01a.mp3");

                outputWindow(unit, targetCell, point, CellColor.White);
                System.Threading.Thread.Sleep(Settings.Ins.IntSignalWait);

                status = measureColor(ufTargetChrom.White, out measureData[3], CaSdk.DisplayMode.DispModeXYZ, ref operation);
                if (status != true)
                { return false; }

#if DEBUG
                Console.WriteLine($"W(X): {measureData[WHITE][XYZ_X]}\r\nW(Y): {measureData[WHITE][XYZ_Y]}\r\nW(Z): {measureData[WHITE][XYZ_Z]}");
                SaveExecLog($"      W(X): {measureData[WHITE][XYZ_X]}");
                SaveExecLog($"      W(Y): {measureData[WHITE][XYZ_Y]}");
                SaveExecLog($"      W(Z): {measureData[WHITE][XYZ_Z]}");
#elif Release_log
                SaveExecLog($"      W(X): {measureData[WHITE][XYZ_X]}");
                SaveExecLog($"      W(Y): {measureData[WHITE][XYZ_Y]}");
                SaveExecLog($"      W(Z): {measureData[WHITE][XYZ_Z]}");
#endif
            }

            playSound(applicationPath + "\\Components\\Sound\\button01b.mp3");

            return true;
        }

        private int calcAdjustUfCellProcessSec(int step)
        {
            int processSec = 0;
            //係数　データ移動,レスポンス処理,コントローラー台数に影響する時間を除いた合計処理時間
            //commonA[0] [13] Set Target Color.
            //commonA[1] [14] Extract Correction Data.
            //commonA[2] [15] Calc XYZ.
            //commonA[3] [16] Calc New XYZ Data.
            //commonA[4] [17] Make New Correction Data.
            //commonA[5] [18] Save New Module Data.
            //commonA[6] [19] Move Adjusted Files.
            //commonA[7] [20] Cabinet Power Off.
            //commonA[8] [21] Send Reconfig.
            //commonA[9] [22] Send Write Command.
            //commonA[10] [23] Waiting for the process of controller.
            //commonA[11] [24] Set Normal Setting.
            //commonA[12] [25] Restore User Settings.
            //commonA[13] [26] Copy Latest Files.
            //commonA[14] [27] Copy Temporary Files.
            //commonA[15] [28] Send Reconfig.
            //commonA[16] [29] Delete Temporary Files.
            //commonA[17] [30] FTP Off.
            int[] commonA = new int[18] { 0, 0, 0, 0, 0, 0, 1, 4, 14, 1, 38, 1, 2, 0, 0, 14, 0, 1};

            processSec = commonA.Skip(step - 16).Sum();

            if (Settings.Ins.ExecLog == true)
            { SaveExecLog("calcAdjustUfCellProcessSec step:" + step + " processSec:" + processSec); }

            return processSec;
        }
        #region Signal

        private void outputCross(UnitInfo unit, int targetCell, CrossPointPosition point)
        {
            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            int startH = 0, startV = 0;

            // Unit Offset
            startH = unit.PixelX;
            startV = unit.PixelY;

            if (curType == CursorType.WhiteCross)
            {
                // Cell Offset
                startH += (targetCell % 4) * modDx - 1; // -1 : 線幅/2分引く
                startV += (targetCell / 4) * modDy - 1; // -1 : 線幅/2分引く

                // Point Offset
                if (point == CrossPointPosition.Center)
                {
                    startH += modDx / 2; // 1/2 Cell
                    startV += modDy / 2; // 1/2 Cell
                }
                else if (point == CrossPointPosition.Top)
                {
                    startH += modDx / 2; // 1/2 Cell
                    startV += 20; // 20画素
                }
                else if (point == CrossPointPosition.Bottom)
                {
                    startH += modDx / 2; // 1/2 Cell
                    startV += modDy - 20; // 20画素
                }
                else if (point == CrossPointPosition.Right)
                {
                    startH += modDx - 20; // 20画素
                    startV += modDy / 2; // 1/2 Cell
                }
                else
                {
                    startH += 20; // 20画素
                    startV += modDy / 2; // 1/2 Cell
                }

                // マイナスになるのを回避
                if (startH < 0)
                { startH = 0; }

                if (startV < 0)
                { startV = 0; }

                // Pattern Cross
                cmd[21] += 0x03;

                // Foreground Color
                cmd[22] = (byte)(brightness.UF_20pc >> 8); // Red //1024
                cmd[23] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[24] = (byte)(brightness.UF_20pc >> 8); // Green
                cmd[25] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[26] = (byte)(brightness.UF_20pc >> 8); // Blue
                cmd[27] = (byte)(brightness.UF_20pc & 0xFF);

                // Start Position
                cmd[34] = (byte)(startH >> 8);
                cmd[35] = (byte)(startH & 0xFF);
                cmd[36] = (byte)(startV >> 8);
                cmd[37] = (byte)(startV & 0xFF);

                // H, V Width
                cmd[38] = 0x00;
                cmd[39] = 0x02;
                cmd[40] = 0x00;
                cmd[41] = 0x02;

                foreach (ControllerInfo cont in dicController.Values)
                {
                    if (cont.ControllerID == unit.ControllerID)
                    { sendSdcpCommand(cmd, 0, cont.IPAddress); }
                    else
                    { outputRaster(0, cont.ControllerID); }
                }
            }
            else // Red Square
            {
                // Cell Offset
                startH += (targetCell % 4) * modDx;
                startV += (targetCell / 4) * modDy;

                // Point Offset
                if (point == CrossPointPosition.Center)
                {
                    startH += modDx / 2 - 15; // 1/2 Cell
                    startV += modDy / 2 - 15; // 1/2 Cell
                }
                else if (point == CrossPointPosition.Top)
                {
                    startH += modDx / 2 - 15; // 1/2 Cell
                    startV += 20 - 15; // 20画素
                }
                else if (point == CrossPointPosition.Bottom)
                {
                    startH += modDx / 2 - 15; // 1/2 Cell
                    startV += modDy - 20 - 15; // 20画素
                }
                else if (point == CrossPointPosition.Right)
                {
                    startH += modDx - 20 - 15; // 20画素
                    startV += modDy / 2 - 15; // 1/2 Cell
                }
                else
                {
                    startH += 20 - 15; // 20画素
                    startV += modDy / 2 - 15; // 1/2 Cell
                }

                // マイナスになるのを回避
                if (startH < 0)
                { startH = 0; }

                if (startV < 0)
                { startV = 0; }

                // Pattern Cross
                cmd[21] += 0x09;

                // Foreground Color(Red)
                cmd[22] = (byte)(brightness.UF_20pc >> 8); // Red
                cmd[23] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[24] = 0; // Green
                cmd[25] = 0;
                cmd[26] = 0; // Blue
                cmd[27] = 0;

                // Background Color(White)
                cmd[28] = (byte)(brightness.UF_20pc >> 8); ; // Red
                cmd[29] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[30] = (byte)(brightness.UF_20pc >> 8); ; // Green
                cmd[31] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[32] = (byte)(brightness.UF_20pc >> 8); ; // Blue
                cmd[33] = (byte)(brightness.UF_20pc & 0xFF);

                // Start Position
                cmd[34] = (byte)(startH >> 8);
                cmd[35] = (byte)(startH & 0xFF);
                cmd[36] = (byte)(startV >> 8);
                cmd[37] = (byte)(startV & 0xFF);

                // H, V Width
                cmd[38] = 0x00;
                cmd[39] = 0x1E;
                cmd[40] = 0x00;
                cmd[41] = 0x1E;

                foreach (ControllerInfo cont in dicController.Values)
                {
                    if (cont.ControllerID == unit.ControllerID)
                    { sendSdcpCommand(cmd, 0, cont.IPAddress); }
                    else
                    { outputRaster(brightness._20pc, cont.ControllerID); }
                }
            }
        }

        private void outputWindow(UnitInfo unit, int targetCell, CrossPointPosition point, CellColor color)
        {
            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            int startH = 0, startV = 0;

            // Unit Offset
            startH = unit.PixelX; // += 320 * (unitCol - 1);
            startV = unit.PixelY; // += 360 * (6 - unitRow);

            // Cell Offset
            startH += (targetCell % 4) * modDx; // 線幅/2分引く
            startV += (targetCell / 4) * modDy; // 線幅/2分引く

            // Point Offset
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            {
                if (point == CrossPointPosition.Center)
                {
                    startH += modDx / 2; // 1/2 Cell
                    startV += modDy / 2; // 1/2 Cell
                }
                else if (point == CrossPointPosition.Top)
                {
                    startH += modDx / 2; // 1/2 Cell
                    startV += 20; // 20画素
                }
                else if (point == CrossPointPosition.Bottom)
                {
                    startH += modDx / 2; // 1/2 Cell
                    startV += modDy - 20; // 20画素
                }
                else if (point == CrossPointPosition.Right)
                {
                    startH += modDx - 20; // 20画素
                    startV += modDy / 2; // 1/2 Cell
                }
                else
                {
                    startH += 20; // 20画素
                    startV += modDy / 2; // 1/2 Cell
                }

                startH -= 25;
                if (startH < 0)
                { startH = 0; }

                startV -= 25;
                if (startV < 0)
                { startV = 0; }
            }

            // Pattern Window
            cmd[21] += 0x09;

            // Foreground Color           
            if (color == CellColor.Red)
            {
                cmd[22] = (byte)(brightness.UF_20pc >> 8); // Red
                cmd[23] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[24] = 0x00;
                cmd[25] = 0x00;
                cmd[26] = 0x00;
                cmd[27] = 0x00;
            }
            else if (color == CellColor.Green)
            {
                cmd[22] = 0x00;
                cmd[23] = 0x00;
                cmd[24] = (byte)(brightness.UF_20pc >> 8); // Green
                cmd[25] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[26] = 0x00;
                cmd[27] = 0x00;
            }
            else if (color == CellColor.Blue)
            {
                cmd[22] = 0x00;
                cmd[23] = 0x00;
                cmd[24] = 0x00;
                cmd[25] = 0x00;
                cmd[26] = (byte)(brightness.UF_20pc >> 8); // Blue
                cmd[27] = (byte)(brightness.UF_20pc & 0xFF);
            }

            // Start Position
            cmd[34] = (byte)(startH >> 8);
            cmd[35] = (byte)(startH & 0xFF);
            cmd[36] = (byte)(startV >> 8);
            cmd[37] = (byte)(startV & 0xFF);

            // H, V Width
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            {
                cmd[38] = 0x00;
                cmd[39] = 0x32;
                cmd[40] = 0x00;
                cmd[41] = 0x32;
            }
            else
            {
                cmd[38] = 0x00;
                cmd[39] = (byte)modDx;
                cmd[40] = 0x00;
                cmd[41] = (byte)modDy;
            }

            if (curType == CursorType.RedSquare)
            {
                // Background Color(White)
                cmd[28] = (byte)(brightness.UF_20pc >> 8); ; // Red
                cmd[29] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[30] = (byte)(brightness.UF_20pc >> 8); ; // Green
                cmd[31] = (byte)(brightness.UF_20pc & 0xFF);
                cmd[32] = (byte)(brightness.UF_20pc >> 8); ; // Blue
                cmd[33] = (byte)(brightness.UF_20pc & 0xFF);
            }

            sendSdcpCommand(cmd, 500, dicController[unit.ControllerID].IPAddress);
        }

        #endregion Signal

        #endregion Private Method
    }
}
