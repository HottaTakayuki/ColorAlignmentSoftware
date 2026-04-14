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

using SONY.Modules;
using MakeUFData;

namespace CAS
{
    // Debug
    public partial class MainWindow : Window
    {
        #region Fields
        #endregion Fields

        #region Events

        // ControlのアイテムはCurrent（選択中）のControllerにしかコマンドを送らない
        // Allの場合はすべてのControllerに対して送信する
        private void cmbxController_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ComboBox)sender).SelectedItem == null)
            { return; }

            string controller = ((ComboBox)sender).SelectedItem.ToString();

            if (controller == "All")
            {
                txbBurnInCorrect.Text = "-";
                txbTempCorrect.Text = "-";
                return;
            }

            foreach (ControllerInfo item in dicController.Values)
            {
                if (controller == "Controller_" + item.ControllerID)
                {
                    currentControllerIP = item.IPAddress;

                    if (tcMain.SelectedIndex == 3)
                    {
                        tcMain.IsEnabled = false;
                        doEvents();
                        getControllerSetting();
                        tcMain.IsEnabled = true;
                    }

                    break;
                }
            }
        }

        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            openSub(1, Settings.Ins.NoOfProbes, Settings.Ins.PortVal);
        }

        private void btnMeasure_Click(object sender, RoutedEventArgs e)
        {
            bool status = measureSub();
            if ((status == true) || (caSdk.getErrorNo() == 426))
            {
                Double[] data = new double[3];
                status = getDataSub(out data[0], out data[1], out data[2], 0);
                if (status != true)
                {
                    if (caSdk.getErrorNo() == 2)
                    {
                        if (ignoreTempError == false)
                        {
                            bool? dialogResult;
                            string msg = "[E2]Temp error has occurred.\r\n\r\nThere is a possibility that the measurement value is shifted.\r\nDo you want to continue the measurement?";

                            showMessageWindow(out dialogResult, msg, "[E2]Temp Error", System.Drawing.SystemIcons.Question, 500, 210, "Yes");

                            if (dialogResult == true)
                            {
                                ignoreTempError = true;
                                return;
                            }
                            else
                            { ShowMessageWindow("Can not measure color.", "Error!", System.Drawing.SystemIcons.Error, 300, 180); }
                        }
                        else
                        { return; }
                    }
                    else // Temp Error以外の場合
                    { ShowMessageWindow("Can not measure color.", "Error!", System.Drawing.SystemIcons.Error, 300, 180); }
                }
                else
                {
                    txbData1.Text = data[0].ToString();
                    txbData2.Text = data[1].ToString();
                    txbData3.Text = data[2].ToString();
                }
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            // CA-410 Close
            if (caSdk != null)
            {
                if (caSdk.isOpened() == true)
                { setRemoteModeSub(CaSdk.RemoteMode.RemoteOff); }
            }
        }

        #region COMBO BOX

        bool channelAutoChange = true;
        private void cmbxChannel_SelectionChanged(object sender, EventArgs e)
        {
            if (channelAutoChange == true)
            {
                txtbStatus.Text = " BUSY";
                //Application.DoEvents();
                if (setChannelSub(cmbxChannel.SelectedIndex) == true)
                    txtbStatus.Text = "DONE";
            }
            else
                channelAutoChange = true;
        }

        bool syncModeAutoChenge = true;
        private void cmbxSyncMode_SelectionChanged(object sender, EventArgs e)
        {
            if (syncModeAutoChenge == true)
            {
                txtbStatus.Text = "BUSY";
                //Application.DoEvents();
                if (setSyncModeSub(cmbxSyncMode.SelectedIndex) == true)
                    txtbStatus.Text = "DONE";
            }
            else
                syncModeAutoChenge = true;
        }

        bool displayModeAutoChange = true;
        private void cmbxDisplayMode_SelectionChanged(object sender, EventArgs e)
        {
            if (displayModeAutoChange == true)
            {
                txtbStatus.Text = "BUSY";
                //Application.DoEvents();
                if (setDisplayModeSub((CaSdk.DisplayMode)cmbxDisplayMode.SelectedIndex) == true)
                    txtbStatus.Text = "DONE";
            }
            else
                displayModeAutoChange = true;
        }

        bool displayDigitsAutoChange = true;
        private void cmbxDisplayDigits_SelectionChanged(object sender, EventArgs e)
        {
            if (displayDigitsAutoChange == true)
            {
                txtbStatus.Text = "BUSY";
                //Application.DoEvents();
                if (setDisplayDigitsSub((CaSdk.DisplayDigits)cmbxDisplayDigits.SelectedIndex) == true)
                    txtbStatus.Text = "DONE";
            }
            else
                displayDigitsAutoChange = true;
        }

        bool averagingModeAutoChange = true;
        private void cmbxAveragingMode_SelectionChanged(object sender, EventArgs e)
        {
            if (averagingModeAutoChange == true)
            {
                txtbStatus.Text = " BUSY";
                //Application.DoEvents();
                if (setAveragingModeSub((CaSdk.AveragingMode)cmbxAveragingMode.SelectedIndex) == true)
                    txtbStatus.Text = "DONE";
            }
            else
                averagingModeAutoChange = true;
        }

        bool brightnessUnitAutoChange = true;
        private void cmbxBrightnessUnit_SelectionChanged(object sender, EventArgs e)
        {
            if (brightnessUnitAutoChange == true)
            {
                txtbStatus.Text = "BUSY";
                //Application.DoEvents();
                if (setBrightnessUnitSub((CaSdk.BrightnessUnit)cmbxBrightnessUnit.SelectedIndex) == true)
                    txtbStatus.Text = "DONE";
            }
            else
                brightnessUnitAutoChange = true;
        }

        #endregion COMBO BOX

        private void btnThrough_Click(object sender, RoutedEventArgs e)
        {
            sendSdcpCommand(SDCPClass.CmdThroughModeOn);
        }

        private void btnTempCorrection_Click(object sender, RoutedEventArgs e)
        {
            sendSdcpCommand(SDCPClass.CmdTempCorrectSet);
        }

        private void btnBurnIn_Click(object sender, RoutedEventArgs e)
        {
            //sendSdcpCommand(SDCPClass.CmdBurnInCorrect);
        }

        private void btnLowBright_Click(object sender, RoutedEventArgs e)
        {
            sendSdcpCommand(SDCPClass.CmdLowBrightModeSet);
        }

        //private void btnFanMode_Click(object sender, RoutedEventArgs e)
        //{
        //    sendSdcpCommand(SDCPClass.CmdUnitFanModeSet);
        //}

        private void btnGapCorrectGet_Click(object sender, RoutedEventArgs e)
        {
            byte[] cmd = new byte[SDCPClass.CmdGapCorrectValueGet.Length];
            Array.Copy(SDCPClass.CmdGapCorrectValueGet, cmd, SDCPClass.CmdGapCorrectValueGet.Length);

            cmd[9] = Convert.ToByte("11", 16);

            string buff;
            sendSdcpCommand(cmd, out buff, "192.168.6.10");
        }

        private void btnGapCorrectSet_Click(object sender, RoutedEventArgs e)
        {
            sendSdcpCommand(SDCPClass.CmdGapCorrectSetOn);
        }

        private void btnSetSerial_Click(object sender, RoutedEventArgs e)
        {
            byte[] cmd = new byte[SDCPClass.CmdSerialNoSet.Length];
            Array.Copy(SDCPClass.CmdSerialNoSet, cmd, SDCPClass.CmdSerialNoSet.Length);

            try
            {
                int intSerial = int.Parse("0000001"); // 設定するSerial
                string strSerial = intSerial.ToString("X8");

                cmd[20] = (byte)Convert.ToInt32(strSerial.Substring(0, 2), 16);
                cmd[21] = (byte)Convert.ToInt32(strSerial.Substring(2, 2), 16);
                cmd[22] = (byte)Convert.ToInt32(strSerial.Substring(4, 2), 16);
                cmd[23] = (byte)Convert.ToInt32(strSerial.Substring(6, 2), 16);
            }
            catch { }

            sendSdcpCommand(cmd);
        }

        private void btnSetAddress_Click(object sender, RoutedEventArgs e)
        {
            byte[] cmd = new byte[SDCPClass.CmdUnitAllocationAddressSet.Length];
            Array.Copy(SDCPClass.CmdUnitAllocationAddressSet, cmd, SDCPClass.CmdUnitAllocationAddressSet.Length);

            cmd[9] = (byte)Convert.ToByte("11", 16);
            cmd[20] = 1;
            cmd[22] = 3;

            string buff;
            sendSdcpCommand(cmd, out buff, "192.168.6.10");
        }

        private void btnGetAddress_Click(object sender, RoutedEventArgs e)
        {
            byte[] cmd = new byte[SDCPClass.CmdUnitAllocationAddressGet.Length];
            Array.Copy(SDCPClass.CmdUnitAllocationAddressGet, cmd, SDCPClass.CmdUnitAllocationAddressGet.Length);

            cmd[9] = Convert.ToByte("16", 16);

            string buff;
            sendSdcpCommand(cmd, out buff, "192.168.6.10");
        }

        private void btnWriteAddress_Click(object sender, RoutedEventArgs e)
        {
            string buff;
            //int maxUnitNo;

            //if (allocInfo.LedPitch == Defined.ledPitch1_5)
            //{ maxUnitNo = Defined.Max_UnitNo_P15; }
            //else
            //{ maxUnitNo = Defined.Max_UnitNo_P12; }

            // ControllerがStandby状態でないとNG

            #region Initialize

            // ↓やらん方がよさそう
            //// 最初にすべて0にフォーマット
            //foreach (ControllerInfo controller in dicController.Values)
            //{
            //    for (int i = 0; i < 12; i++)
            //    {
            //        for (int j = 0; j < maxUnitNo; j++)
            //        {
            //            // Allocation Address
            //            byte[] cmd = new byte[SDCPClass.CmdUnitAllocationAddressSet.Length];
            //            Array.Copy(SDCPClass.CmdUnitAllocationAddressSet, cmd, SDCPClass.CmdUnitAllocationAddressSet.Length);

            //            string portNo = (i + 1).ToString("X1");
            //            string unitNo = (j + 1).ToString("X1");

            //            cmd[9] = Convert.ToByte(portNo + unitNo, 16);

            //            sendSdcpCommand(cmd, out buff, controller.IPAddress);

            //            // Picture Address
            //            cmd = new byte[SDCPClass.CmdUnitPictureAddressSet.Length];
            //            Array.Copy(SDCPClass.CmdUnitPictureAddressSet, cmd, SDCPClass.CmdUnitPictureAddressSet.Length);

            //            cmd[9] = Convert.ToByte(portNo + unitNo, 16);

            //            sendSdcpCommand(cmd, out buff, controller.IPAddress);
            //        }
            //    }
            //}

            #endregion Initialize

            // 実際のアドレスを書き込む
            foreach (List<UnitInfo> lstUnits in allocInfo.lstUnits)
            {
                foreach (UnitInfo unit in lstUnits)
                {
                    if (unit.ControllerID == 0)
                    { continue; }

                    // Allocation Address
                    byte[] cmd = new byte[SDCPClass.CmdUnitAllocationAddressSet.Length];
                    Array.Copy(SDCPClass.CmdUnitAllocationAddressSet, cmd, SDCPClass.CmdUnitAllocationAddressSet.Length);

                    string portNo = unit.PortNo.ToString("X1");
                    string unitNo = unit.UnitNo.ToString("X1");

                    cmd[9] = Convert.ToByte(portNo + unitNo, 16);
                    cmd[21] = (byte)unit.X;
                    cmd[23] = (byte)unit.Y;

                    sendSdcpCommand(cmd, out buff, dicController[unit.ControllerID].IPAddress);

                    // Picture Address
                    cmd = new byte[SDCPClass.CmdUnitPictureAddressSet.Length];
                    Array.Copy(SDCPClass.CmdUnitPictureAddressSet, cmd, SDCPClass.CmdUnitPictureAddressSet.Length);

                    string pixelX = unit.PixelX.ToString("X4");
                    string pixelY = unit.PixelY.ToString("X4");

                    cmd[9] = Convert.ToByte(portNo + unitNo, 16);
                    cmd[20] = Convert.ToByte(pixelX.Substring(0, 2), 16);
                    cmd[21] = Convert.ToByte(pixelX.Substring(2, 2), 16);
                    cmd[22] = Convert.ToByte(pixelY.Substring(0, 2), 16);
                    cmd[23] = Convert.ToByte(pixelY.Substring(2, 2), 16);

                    sendSdcpCommand(cmd, out buff, dicController[unit.ControllerID].IPAddress);
                }
            }

            MessageBox.Show("Write Address Complete!", "Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        private void btnNfsModeOn_Click(object sender, RoutedEventArgs e)
        {
            sendSdcpCommand(SDCPClass.CmdNfsOn);
        }

        private void btnNfsModeOff_Click(object sender, RoutedEventArgs e)
        {
            sendSdcpCommand(SDCPClass.CmdNfsOff);
        }

        private void btnReadUnitData_Click(object sender, RoutedEventArgs e)
        {
            sendSdcpCommand(SDCPClass.CmdDataRead_UnitData);
        }

        private void btnReadCellUniformity_Click(object sender, RoutedEventArgs e)
        {
            sendSdcpCommand(SDCPClass.CmdDataRead_CellUniformity);
        }

        private void btnReadCellIntTime_Click(object sender, RoutedEventArgs e)
        {
            sendSdcpCommand(SDCPClass.CmdDataRead_CellIntegralTime);
        }

        private void btnWrite_Click(object sender, RoutedEventArgs e)
        {
            sendSdcpCommand(SDCPClass.CmdDataWrite);
        }
        
        private void btnUnitReconfig_Click(object sender, RoutedEventArgs e)
        {
            string controller = cmbxControllerControl.SelectedItem.ToString();

            actionButton(sender, "Reconfig");

            if (controller == "All")
            {
                foreach (ControllerInfo cont in dicController.Values)
                { sendSdcpCommand(SDCPClass.CmdUnitReconfig, cont.IPAddress); }
            }
            else
            { sendSdcpCommand(SDCPClass.CmdUnitReconfig); }

            releaseButton(sender);
        }

        private void btnMasterSlave_Click(object sender, RoutedEventArgs e)
        {
            string buff;

            // Master / Slave
            sendSdcpCommand(SDCPClass.CmdControllerMasterSlaveGet, out buff);
        }
        
        private void btnSetColorTemp_Click(object sender, RoutedEventArgs e)
        {
            //byte[] cmd = new byte[SDCPClass.CmdColorTempSet.Length];
            //Array.Copy(SDCPClass.CmdColorTempSet, cmd, SDCPClass.CmdColorTempSet.Length);

            //cmd[20] = 0x00;
 
            //foreach (ControllerInfo cont in dicController.Values)
            //{ sendSdcpCommand(cmd, cont.IPAddress); }
        }
        
        private void btnCalcChecksumTest_Click(object sender, RoutedEventArgs e)
        {
            byte[] destBytes = File.ReadAllBytes(@"C:\Data\Documents\CLED SI\170518\u0106_ud_new.bin");

            // Unit Info CheckSum再計算
            byte[] unitInfo = new byte[128];
            Array.Copy(destBytes, 8, unitInfo, 0, 128);

            calcUnitInfoCheckSum(ref unitInfo);

            Array.Copy(unitInfo, 0, destBytes, 8, 128);

            File.WriteAllBytes(@"C:\Data\Documents\CLED SI\170518\test.bin", destBytes);
        }

        private void btnReadOneUnit_Click(object sender, RoutedEventArgs e)
        {
            unmountDirve();

            foreach (ControllerInfo controller in dicController.Values)
            {
                // ●Unit Power Off [*1]                
                if (sendSdcpCommand(SDCPClass.CmdUnitPowerOff, controller.IPAddress) != true)
                { return; }
                
                // ●NFS ON [*2]                
                if (sendSdcpCommand(SDCPClass.CmdNfsOn, 3000, controller.IPAddress) != true)
                { return; }

                System.Threading.Thread.Sleep(Settings.Ins.NfsWait);
                                                
                // ●NFSドライブをマウント [*6]
                // すべてのControllerを無条件にMount
                controller.NeedMount = true;
                controller.Drive = "";

                if (mountNfsDrive(controller) != true)
                { return; }                
            }

            // ●Backupコマンド発行
            byte[] cmd = new byte[SDCPClass.CmdDataRead_UnitData.Length];
            Array.Copy(SDCPClass.CmdDataRead_UnitData, cmd, SDCPClass.CmdDataRead_UnitData.Length);

            cmd[9] = 0x11;

            sendSdcpCommand(cmd, dicController[1].IPAddress);

            // ●Complete待ち [4]
            bool[] complete = new bool[dicController.Count];

            while (true)
            {
                int id = 0;
                foreach (ControllerInfo controller in dicController.Values)
                {
                    complete[id] = checkComplete(controller.Drive, "read_complete");
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

            cmd[9] = 0x12;
            sendSdcpCommand(cmd, dicController[1].IPAddress);

            // ●Complete待ち [4]
            while (true)
            {
                int id = 0;
                foreach (ControllerInfo controller in dicController.Values)
                {
                    complete[id] = checkComplete(controller.Drive, "read_complete");
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

            // ●File移動 [5]            
            foreach (ControllerInfo controller in dicController.Values)
            {
                string destDir = @"C:\CAS\Backup\Test\";
                
                // Directory作成
                if (Directory.Exists(destDir) != true)
                {
                    try { Directory.CreateDirectory(destDir); }
                    catch (Exception ex)
                    {
                        ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                        return;
                    }
                }

                string[] files = Directory.GetFiles(controller.Drive, "*", System.IO.SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    try
                    {
                        string destPath = destDir + System.IO.Path.GetFileName(files[i]);

                        if (File.Exists(destPath) == true)
                        {
                            try { File.Delete(destPath); }
                            catch { }
                        }

                        File.Copy(files[i], destPath, true);
                    }
                    catch { return; }
                }
            }
        }

        private void btnSendADCP_Click(object sender, RoutedEventArgs e)
        {
            //string res;

            //adcp = new SendADCP.SendADCP();
            //adcp.TargetIpAddress = "192.168.6.10";
            //adcp.Password = Defined.password;

            //adcp.SendADCPCommand("re_photo_mode ?", out res);
        }

        private void btnFtpModeOn_Click(object sender, RoutedEventArgs e)
        {
            sendSdcpCommand(SDCPClass.CmdFtpOn);
        }

        private void btnFtpModeOff_Click(object sender, RoutedEventArgs e)
        {
            sendSdcpCommand(SDCPClass.CmdFtpOff);
        }

        private void btnGetFtpFileList_Click(object sender, RoutedEventArgs e)
        {
            List<string> lstFiles;

            getFileListFtp(currentControllerIP, out lstFiles);
        }

        private void btnDbgResetUfManual_Click(object sender, RoutedEventArgs e)
        {
            string value;

            foreach (List<UnitInfo> lstUnit in allocInfo.lstUnits)
            {
                foreach (UnitInfo unit in lstUnit)
                {
                    Byte[] cmd = new byte[SDCPClass.CmdUnitManualAdjReset.Length];
                    Array.Copy(SDCPClass.CmdUnitManualAdjReset, cmd, SDCPClass.CmdUnitManualAdjReset.Length);

                    cmd[9] += (byte)(unit.PortNo << 4);
                    cmd[9] += (byte)unit.UnitNo;

                    cmd[20] = 0; // 全Cell

                    sendSdcpCommand(cmd, out value, dicController[unit.ControllerID].IPAddress);
                }
            }
        }

        #region Chiron

        private void btnConvCdbin_Click(object sender, RoutedEventArgs e)
        {
            string file = @"C:\CAS\Backup\Chiron\new-rCD.rbin";

            byte[] srcBytes = File.ReadAllBytes(file);
            byte[] convertedBytes;

            convertWriteData(srcBytes, out convertedBytes);

            string convertedFile = @"C:\CAS\Backup\Chiron\convertedCd.bin";
            File.WriteAllBytes(convertedFile, convertedBytes);
        }

        private void btnDebugCalcCrc_Click(object sender, RoutedEventArgs e)
        {
            string file = @"C:\CAS\Backup\ZRCT-200_0000000_Rbin\u0101_ud.rbin";

            byte[] srcBytes = File.ReadAllBytes(file);
            byte[] tempBytes = new byte[58368];
            byte[] crcBytes = new byte[4];

            Array.Copy(srcBytes, 32, tempBytes, 0, 58368);

            // tempBytesに切り出した部分から計算したCRC
            uint crc32 = new CRC32().Calc(tempBytes);
            crc32 = crc32 ^ 0xFFFFFFFF;
            byte[] crc = BitConverter.GetBytes(crc32);

            byte[] crc2;
            CalcCrc(tempBytes, out crc2);

            // もともと記載のCRC
            Array.Copy(srcBytes, 16, crcBytes, 0, 4);
        }

        private void btnDebugConvCdIR03_Click(object sender, RoutedEventArgs e)
        {
            string orgFile = @"C:\CAS\Backup\ZRCT-200_0000000_Rbin\u0101_cd.rbin";
            string destDir = @"C:\CAS\Backup\ZRCT-200_0000000_Initial";

            convertRbinToBin(orgFile, destDir);
        }

        private void btnDebugPat3_Click(object sender, RoutedEventArgs e)
        {
            string file = @"D:\Documents\Chiron\設計資料\データフォーマット仕様_20201209\Pat3CdData-No34&36\P1p2F-No34_cd.bin";

            byte[] srcBytes = File.ReadAllBytes(file);
            byte[] modBytes = new byte[hcModuleDataLength];
            byte[] pxlBytes = new byte[129600];
            byte[] data = new byte[8];
            double[] elements;

            Array.Copy(srcBytes, 320, modBytes, 0, hcModuleDataLength);
            Array.Copy(modBytes, 288, pxlBytes, 0, 129600);

            for (int i = 0; i < 10; i++)
            {
                Array.Copy(pxlBytes, i * 8, data, 0, 8);

                CMakeUFData.UnpackCcDataPat3(data, out elements);

                byte[] data2;
                CMakeUFData.PackCcDataPat3(elements, out data2);
            }
        }

        private void btnUfCamPackCc_Click(object sender, RoutedEventArgs e)
        {
            // 3*3の9個の成分を64bitのデータにPackingする
            byte[] data;
            double[] elements = new double[9] { 0.77770, 0.00000, 0.00000, 0.00000, 0.85862, 0.00000, 0.00000, 0.00000, 0.85229 };

            CMakeUFData.PackCcDataPat3(elements, out data);
        }

        private void btnUfCamUnpackCc_Click(object sender, RoutedEventArgs e)
        {
            // 64bitのデータを3*3の9個の成分にUnpackingする
            double[] elements;
            //byte[] data = new byte[8] { 0xc2, 0x40, 0x10, 0x48, 0x0c, 0x02, 0x41, 0xaf };
            byte[] data = new byte[8] { 0x94, 0x11, 0x04, 0x72, 0x04, 0x81, 0x20, 0x93 };
            //byte[] data = new byte[8] { 0x94, 0x10, 0x04, 0x72, 0x04, 0x81, 0x20, 0x93 };

            CMakeUFData.UnpackCcDataPat3(data, out elements);
        }

        private void btnDebugCCtoCsv_Click(object sender, RoutedEventArgs e)
        {
            string file = @"D:\Documents\Chiron\作成資料\角田さん作成データ\初期値_UNC01.rbin";

            // 目標色度設定
            setChromTarget();

            m_MakeUFData = new CMakeUFData(cabiDx, cabiDy);

            bool status = m_MakeUFData.ExtractFmt(file, allocInfo.LEDModel);

            //status = m_MakeUFData.Fmt2XYZ(ufTargetChrom.Red.x, ufTargetChrom.Red.y, ufTargetChrom.Green.x, ufTargetChrom.Green.y, ufTargetChrom.Blue.x, ufTargetChrom.Blue.y, ufTargetChrom.White.Lv, ufTargetChrom.White.x, ufTargetChrom.White.y);

            string csv = @"D:\Documents\Chiron\作成資料\角田さん作成データ\初期値_UNC01.csv";
            m_MakeUFData.OutputFmtCsv(csv);
        }

        #endregion Chiron

        #endregion Events

        #region Private Methods

        private void setDisplayModeText(CaSdk.DisplayMode displayMode)
        {
            switch (displayMode)
            {
                case CaSdk.DisplayMode.DispModeLvxy:
                    lbDataUnit1.Content = "x";
                    lbDataUnit2.Content = "y";
                    lbDataUnit3.Content = "Lv";
                    break;
                case CaSdk.DisplayMode.DispModeTdudv:
                    lbDataUnit1.Content = "T";
                    lbDataUnit2.Content = "duv";
                    lbDataUnit3.Content = "Lv";
                    break;
                case CaSdk.DisplayMode.DispModeANL:
                case CaSdk.DisplayMode.DispModeANLG:
                case CaSdk.DisplayMode.DispModeANLR:
                    lbDataUnit1.Content = "R";
                    lbDataUnit2.Content = "B";
                    lbDataUnit3.Content = "G";
                    break;
                case CaSdk.DisplayMode.DispModePUV:
                    lbDataUnit1.Content = "u\'";
                    lbDataUnit2.Content = "v\'";
                    lbDataUnit3.Content = "Lv";
                    break;
                case CaSdk.DisplayMode.DispModeFMA:
                    lbDataUnit1.Content = "";
                    lbDataUnit2.Content = "";
                    lbDataUnit3.Content = "FMA";
                    break;
                case CaSdk.DisplayMode.DispModeXYZ:
                    lbDataUnit1.Content = "X";
                    lbDataUnit2.Content = "Y";
                    lbDataUnit3.Content = "Z";
                    break;
                case CaSdk.DisplayMode.DispModeJEITA:
                    lbDataUnit1.Content = "";
                    lbDataUnit2.Content = "";
                    lbDataUnit3.Content = "JEITA";
                    break;
                default:
                    lbDataUnit1.Content = "";
                    lbDataUnit2.Content = "";
                    lbDataUnit3.Content = "";
                    break;
            }
        }

        #region Chiron

        private void convertWriteData(byte[] srcBytes, out byte[] convertedBytes)
        {
            convertedBytes = new byte[srcBytes.Length];
            Array.Copy(srcBytes, convertedBytes, srcBytes.Length);

            // DataTypeの変更
            // Product Info
            // DataType変更
            int intDataType = 0x00000801; // Dummy
            byte[] dataType = BitConverter.GetBytes(intDataType);

            Array.Copy(dataType, 0, convertedBytes, 40, 4);

            // CRC再計算
            byte[] tempBytes = new byte[28];
            Array.Copy(convertedBytes, 32, tempBytes, 0, 28);

            uint crc32 = new CRC32().Calc(tempBytes);
            crc32 = crc32 ^ 0xFFFFFFFF;
            byte[] crc = BitConverter.GetBytes(crc32);

            Array.Copy(crc, 0, convertedBytes, 60, 4);

            // Color Correction(Moduleごとに同じものが8個)
            for (int i = 0; i < 8; i++)
            {
                // DataType変更
                intDataType = 0x00001101; // Color Correction (Write)
                dataType = BitConverter.GetBytes(intDataType);

                Array.Copy(dataType, 0, convertedBytes, 328 + 129888 * i, 4); // 328 = 一番最初のColor CorrectionのDataTypeの位置, 129888 = 1ModuleあたりのByte数

                // CRC再計算
                // Data

                // Header
                tempBytes = new byte[28];
                Array.Copy(convertedBytes, 320 + 129888 * i, tempBytes, 0, 28);

                crc32 = new CRC32().Calc(tempBytes);
                crc32 = crc32 ^ 0xFFFFFFFF;
                crc = BitConverter.GetBytes(crc32);

                Array.Copy(crc, 0, convertedBytes, 348 + 129888 * i, 4);
            }
        }

        #endregion Chiron

        #endregion Private Methods
    }
}
