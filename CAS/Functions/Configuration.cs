using System;
using System.Collections.Generic;

using System.Windows;
using System.Windows.Controls;

using System.Windows.Forms;

namespace CAS
{
    // Configuration
    public partial class MainWindow : Window
    {
        #region Fields

        private bool configTabFirstSelect = true;
        private const int cursorCellYMax_Module4x2 = 1;
        private const int cursorCellYMax_Module4x3 = 2;

        #endregion

        #region Data Class

        class PixelSize
        {
            public int CabinetDx;
            public int CabinetDy;
            public int ModuleDx;
            public int ModuleDy;

            public PixelSize(int cabinetDx, int cabinetDy, int moduleDx, int moduleDy)
            {
                this.CabinetDx = cabinetDx;
                this.CabinetDy = cabinetDy;
                this.ModuleDx = moduleDx;
                this.ModuleDy = moduleDy;
            }
        }

        class DataSize
        {
            public int ModuleCnt;
            public int UdDataLength;
            public int HcDataLength;
            public int HcModuleDataLength;
            public int HcCcDataLength;
            public int LcDataLength;
            public int LcFcclModuleDataLength;
            public int LcFcclDataLength;
            public int LcLcalVModuleDataLength;
            public int LcLcalHModuleDataLength;
            public int LcLmcalModuleDataLength;
            public int LcMgamModuleDataLength;

            public DataSize(
                int moduleCnt,
                int udDataLength,
                int hcDataLength, int hcModuleDataLength, int hcCcDataLength,
                int lcDataLength, int lcFcclModuleDataLength, int lcFcclDataLength, int lcLcalVModuleDataLength, int lcLcalHModuleDataLength, int lcLmcalModuleDataLength, int lcMgamModuleDataLength)
            {
                this.ModuleCnt = moduleCnt;
                this.UdDataLength = udDataLength;
                this.HcDataLength = hcDataLength;
                this.HcModuleDataLength = hcModuleDataLength;
                this.HcCcDataLength = hcCcDataLength;
                this.LcDataLength = lcDataLength;
                this.LcFcclModuleDataLength = lcFcclModuleDataLength;
                this.LcFcclDataLength = lcFcclDataLength;
                this.LcLcalVModuleDataLength = lcLcalVModuleDataLength;
                this.LcLcalHModuleDataLength = lcLcalHModuleDataLength;
                this.LcLmcalModuleDataLength = lcLmcalModuleDataLength;
                this.LcMgamModuleDataLength = lcMgamModuleDataLength;
            }
        }

        class ConfigurationOfLedModel
        {
            public PixelSize PixelSize;
            public DataSize DataSize;
            public ColorPurpose ColorPurpose;
            public LEDModuleConfigurations LEDModuleConfiguration;
            public Visibility ModulePanel12Visibility;
            public int ModuleYMaxIdx;
            public CameraDataClass.Coordinate CabinetSize;
            public double CamDist;

            public ConfigurationOfLedModel(
                PixelSize pixelSize, DataSize dataSize, 
                ColorPurpose colorPurpose, LEDModuleConfigurations ledModuleConfiguration, Visibility modulePanel12Visibility,
                int moduleYMaxIdx, CameraDataClass.Coordinate cabinetSize, double camDist)
            {
                this.PixelSize = pixelSize;
                this.DataSize = dataSize;
                this.ColorPurpose = colorPurpose;
                this.LEDModuleConfiguration = ledModuleConfiguration;
                this.ModulePanel12Visibility = modulePanel12Visibility;
                this.ModuleYMaxIdx = moduleYMaxIdx;
                this.CabinetSize = cabinetSize;
                this.CamDist = camDist;
            }
        }

        #endregion Data Class

        #region Events

        #region Luminance / Chromaticity

        private void showTargetModelChromaticity()
        {
            ChromCustom targetChrom = new ChromCustom(Settings.Ins.ConfigChromType);

            txbConfigRed_x.Text   = targetChrom.Red.x.ToString("0.000");
            txbConfigRed_y.Text   = targetChrom.Red.y.ToString("0.000");
            txbConfigGreen_x.Text = targetChrom.Green.x.ToString("0.000");
            txbConfigGreen_y.Text = targetChrom.Green.y.ToString("0.000");
            txbConfigBlue_x.Text  = targetChrom.Blue.x.ToString("0.000");
            txbConfigBlue_y.Text  = targetChrom.Blue.y.ToString("0.000");
            txbConfigWhite_x.Text = targetChrom.White.x.ToString("0.000");
            txbConfigWhite_y.Text = targetChrom.White.y.ToString("0.000");
            txbConfigWhite_Y.Text = targetChrom.White.Lv.ToString("0.00");
        }

        private void pwbConfig_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (pwbConfig.Password == customPassword)
            { gdChrom.IsEnabled = true; }
            else
            { gdChrom.IsEnabled = false; }
        }

        private Dictionary<string, ConfigurationOfLedModel> configurationOfLedModelList = new Dictionary<string, ConfigurationOfLedModel>()
        {
            { ZRD_B12A,
                new ConfigurationOfLedModel(
                    new PixelSize(CabinetDxP12, CabinetDyP12, ModuleDxP12_Mdoule4x2, ModuleDyP12_Module4x2), 
                    new DataSize(ModuleCount_Module4x2, UdDataLength_Module4x2, HcDataLengthP12_Module4x2, HcModuleDataLengthP12_Module4x2, HcCcDataLengthP12_Module4x2, LcDataLengthP12_Module4x2, LcFcclModuleDataLengthP12_Module4x2, LcFcclDataLengthP12_Module4x2, 0, 0, 0, 0),
                    ColorPurpose.ZRD_B12A,
                    LEDModuleConfigurations.Module_4x2,
                    Visibility.Collapsed,
                    cursorCellYMax_Module4x2,
                    new CameraDataClass.Coordinate(CabinetSizeH_Chiron, CabinetSizeV_Chiron),
                    CapDistance_P12)
            },
            { ZRD_B15A,
                new ConfigurationOfLedModel(
                    new PixelSize(CabinetDxP15, CabinetDyP15, ModuleDxP15_Module4x2, ModuleDyP15_Module4x2), 
                    new DataSize(ModuleCount_Module4x2, UdDataLength_Module4x2, HcDataLengthP15_Module4x2, HcModuleDataLengthP15_Module4x2, HcCcDataLengthP15_Module4x2, LcDataLengthP15_Module4x2, LcFcclModuleDataLengthP15_Module4x2, LcFcclDataLengthP15_Module4x2, 0, 0, 0, 0),
                    ColorPurpose.ZRD_B15A,
                    LEDModuleConfigurations.Module_4x2,
                    Visibility.Collapsed,
                    cursorCellYMax_Module4x2,
                    new CameraDataClass.Coordinate(CabinetSizeH_Chiron, CabinetSizeV_Chiron),
                    CapDistance_P15)
            },
            { ZRD_C12A,
                new ConfigurationOfLedModel(
                    new PixelSize(CabinetDxP12, CabinetDyP12, ModuleDxP12_Mdoule4x2, ModuleDyP12_Module4x2),
                    new DataSize(ModuleCount_Module4x2, UdDataLength_Module4x2, HcDataLengthP12_Module4x2, HcModuleDataLengthP12_Module4x2, HcCcDataLengthP12_Module4x2, LcDataLengthP12_Module4x2, LcFcclModuleDataLengthP12_Module4x2, LcFcclDataLengthP12_Module4x2, 0, 0, 0, 0),
                    ColorPurpose.ZRD_C12A,
                    LEDModuleConfigurations.Module_4x2,
                    Visibility.Collapsed,
                    cursorCellYMax_Module4x2,
                    new CameraDataClass.Coordinate(CabinetSizeH_Chiron, CabinetSizeV_Chiron),
                    CapDistance_P12)
            },
            { ZRD_C15A,
                new ConfigurationOfLedModel(
                    new PixelSize(CabinetDxP15, CabinetDyP15, ModuleDxP15_Module4x2, ModuleDyP15_Module4x2), 
                    new DataSize(ModuleCount_Module4x2, UdDataLength_Module4x2, HcDataLengthP15_Module4x2, HcModuleDataLengthP15_Module4x2, HcCcDataLengthP15_Module4x2, LcDataLengthP15_Module4x2, LcFcclModuleDataLengthP15_Module4x2, LcFcclDataLengthP15_Module4x2, 0, 0, 0, 0),
                    ColorPurpose.ZRD_C15A,
                    LEDModuleConfigurations.Module_4x2,
                    Visibility.Collapsed,
                    cursorCellYMax_Module4x2,
                    new CameraDataClass.Coordinate(CabinetSizeH_Chiron, CabinetSizeV_Chiron),
                    CapDistance_P15)
            },
            { ZRD_BH12D,
                new ConfigurationOfLedModel(
                    new PixelSize(CabinetDxP12, CabinetDyP12, ModuleDxP12_Module4x3, ModuleDyP12_Module4x3), 
                    new DataSize(ModuleCount_Module4x3, UdDataLength_Module4x3, HcDataLengthP12_Module4x3, HcModuleDataLengthP12_Module4x3, HcCcDataLengthP12_Module4x3, LcDataLengthP12_Module4x3, 0, 0, LcLcalVModuleDataLengthP12_Module4x3, LcLcalHModuleDataLengthP12_Module4x3, LcLmcalModuleDataLengthP12_Module4x3, LcMgamModuleDataLength),
                    ColorPurpose.ZRD_BH12D,
                    LEDModuleConfigurations.Module_4x3,
                    Visibility.Visible,
                    cursorCellYMax_Module4x3,
                    new CameraDataClass.Coordinate(CabinetSizeH_Cancun, CabinetSizeV_Cancun),
                    CapDistance_P12)
            },
            { ZRD_BH15D,
                new ConfigurationOfLedModel(
                    new PixelSize(CabinetDxP15, CabinetDyP15, ModuleDxP15_Module4x3, ModuleDyP15_Module4x3), 
                    new DataSize(ModuleCount_Module4x3, UdDataLength_Module4x3, HcDataLengthP15_Module4x3, HcModuleDataLengthP15_Module4x3, HcCcDataLengthP15_Module4x3, LcDataLengthP15_Module4x3, 0, 0, LcLcalVModuleDataLengthP15_Module4x3, LcLcalHModuleDataLengthP15_Module4x3, LcLmcalModuleDataLengthP15_Module4x3, LcMgamModuleDataLength),
                    ColorPurpose.ZRD_BH15D,
                    LEDModuleConfigurations.Module_4x3,
                    Visibility.Visible,
                    cursorCellYMax_Module4x3,
                    new CameraDataClass.Coordinate(CabinetSizeH_Cancun, CabinetSizeV_Cancun),
                    CapDistance_P15)
            },
            { ZRD_CH12D,
                new ConfigurationOfLedModel(
                    new PixelSize(CabinetDxP12, CabinetDyP12, ModuleDxP12_Module4x3, ModuleDyP12_Module4x3), 
                    new DataSize(ModuleCount_Module4x3, UdDataLength_Module4x3, HcDataLengthP12_Module4x3, HcModuleDataLengthP12_Module4x3, HcCcDataLengthP12_Module4x3, LcDataLengthP12_Module4x3, 0, 0, LcLcalVModuleDataLengthP12_Module4x3, LcLcalHModuleDataLengthP12_Module4x3, LcLmcalModuleDataLengthP12_Module4x3, LcMgamModuleDataLength),
                    ColorPurpose.ZRD_CH12D,
                    LEDModuleConfigurations.Module_4x3,
                    Visibility.Visible,
                    cursorCellYMax_Module4x3,
                    new CameraDataClass.Coordinate(CabinetSizeH_Cancun, CabinetSizeV_Cancun),
                    CapDistance_P12)
            },
            { ZRD_CH15D,
                new ConfigurationOfLedModel(
                    new PixelSize(CabinetDxP15, CabinetDyP15, ModuleDxP15_Module4x3, ModuleDyP15_Module4x3), 
                    new DataSize(ModuleCount_Module4x3, UdDataLength_Module4x3, HcDataLengthP15_Module4x3, HcModuleDataLengthP15_Module4x3, HcCcDataLengthP15_Module4x3, LcDataLengthP15_Module4x3, 0, 0, LcLcalVModuleDataLengthP15_Module4x3, LcLcalHModuleDataLengthP15_Module4x3, LcLmcalModuleDataLengthP15_Module4x3, LcMgamModuleDataLength),
                    ColorPurpose.ZRD_CH15D,
                    LEDModuleConfigurations.Module_4x3,
                    Visibility.Visible,
                    cursorCellYMax_Module4x3,
                    new CameraDataClass.Coordinate(CabinetSizeH_Cancun, CabinetSizeV_Cancun),
                    CapDistance_P15)
            },
            { ZRD_BH12D_S3,
                new ConfigurationOfLedModel(
                    new PixelSize(CabinetDxP12, CabinetDyP12, ModuleDxP12_Module4x3, ModuleDyP12_Module4x3),
                    new DataSize(ModuleCount_Module4x3, UdDataLength_Module4x3, HcDataLengthP12_Module4x3, HcModuleDataLengthP12_Module4x3, HcCcDataLengthP12_Module4x3, LcDataLengthP12_Module4x3, 0, 0, LcLcalVModuleDataLengthP12_Module4x3, LcLcalHModuleDataLengthP12_Module4x3, LcLmcalModuleDataLengthP12_Module4x3, LcMgamModuleDataLength),
                    ColorPurpose.ZRD_BH12D_S3,
                    LEDModuleConfigurations.Module_4x3,
                    Visibility.Visible,
                    cursorCellYMax_Module4x3,
                    new CameraDataClass.Coordinate(CabinetSizeH_Cancun, CabinetSizeV_Cancun),
                    CapDistance_P12)
            },
            { ZRD_BH15D_S3,
                new ConfigurationOfLedModel(
                    new PixelSize(CabinetDxP15, CabinetDyP15, ModuleDxP15_Module4x3, ModuleDyP15_Module4x3),
                    new DataSize(ModuleCount_Module4x3, UdDataLength_Module4x3, HcDataLengthP15_Module4x3, HcModuleDataLengthP15_Module4x3, HcCcDataLengthP15_Module4x3, LcDataLengthP15_Module4x3, 0, 0, LcLcalVModuleDataLengthP15_Module4x3, LcLcalHModuleDataLengthP15_Module4x3, LcLmcalModuleDataLengthP15_Module4x3, LcMgamModuleDataLength),
                    ColorPurpose.ZRD_BH15D_S3,
                    LEDModuleConfigurations.Module_4x3,
                    Visibility.Visible,
                    cursorCellYMax_Module4x3,
                    new CameraDataClass.Coordinate(CabinetSizeH_Cancun, CabinetSizeV_Cancun),
                    CapDistance_P15)
            },
            { ZRD_CH12D_S3,
                new ConfigurationOfLedModel(
                    new PixelSize(CabinetDxP12, CabinetDyP12, ModuleDxP12_Module4x3, ModuleDyP12_Module4x3),
                    new DataSize(ModuleCount_Module4x3, UdDataLength_Module4x3, HcDataLengthP12_Module4x3, HcModuleDataLengthP12_Module4x3, HcCcDataLengthP12_Module4x3, LcDataLengthP12_Module4x3, 0, 0, LcLcalVModuleDataLengthP12_Module4x3, LcLcalHModuleDataLengthP12_Module4x3, LcLmcalModuleDataLengthP12_Module4x3, LcMgamModuleDataLength),
                    ColorPurpose.ZRD_CH12D_S3,
                    LEDModuleConfigurations.Module_4x3,
                    Visibility.Visible,
                    cursorCellYMax_Module4x3,
                    new CameraDataClass.Coordinate(CabinetSizeH_Cancun, CabinetSizeV_Cancun),
                    CapDistance_P12)
            },
            { ZRD_CH15D_S3,
                new ConfigurationOfLedModel(
                    new PixelSize(CabinetDxP15, CabinetDyP15, ModuleDxP15_Module4x3, ModuleDyP15_Module4x3),
                    new DataSize(ModuleCount_Module4x3, UdDataLength_Module4x3, HcDataLengthP15_Module4x3, HcModuleDataLengthP15_Module4x3, HcCcDataLengthP15_Module4x3, LcDataLengthP15_Module4x3, 0, 0, LcLcalVModuleDataLengthP15_Module4x3, LcLcalHModuleDataLengthP15_Module4x3, LcLmcalModuleDataLengthP15_Module4x3, LcMgamModuleDataLength),
                    ColorPurpose.ZRD_CH15D_S3,
                    LEDModuleConfigurations.Module_4x3,
                    Visibility.Visible,
                    cursorCellYMax_Module4x3,
                    new CameraDataClass.Coordinate(CabinetSizeH_Cancun, CabinetSizeV_Cancun),
                    CapDistance_P15)
            },
        };

        private void loadConfigurationOfLedModel(string ledModel)
        {
            var settings = configurationOfLedModelList[ledModel];

            // ラベル
            //lbChromMode.Content = settings.LedModelName;

            // LED ModelのPixel Size初期化
            cabiDx = settings.PixelSize.CabinetDx;
            cabiDy = settings.PixelSize.CabinetDy;
            modDx  = settings.PixelSize.ModuleDx;
            modDy  = settings.PixelSize.ModuleDy;

            // Data関連参照変数を初期化
            moduleCount  = settings.DataSize.ModuleCnt;
            udDataLength = settings.DataSize.UdDataLength;
            hcDataLength = settings.DataSize.HcDataLength;
            hcModuleDataLength = settings.DataSize.HcModuleDataLength;
            hcCcDataLength = settings.DataSize.HcCcDataLength;
            lcDataLength = settings.DataSize.LcDataLength;
            lcFcclModuleDataLength = settings.DataSize.LcFcclModuleDataLength;
            lcFcclDataLength = settings.DataSize.LcFcclDataLength;
            lcLcalVModuleDataLength = settings.DataSize.LcLcalVModuleDataLength;
            lcLcalHModuleDataLength = settings.DataSize.LcLcalHModuleDataLength;
            lcLmcalModuleDataLength = settings.DataSize.LcLmcalModuleDataLength;
            lcMgamModuleDataLength = settings.DataSize.LcMgamModuleDataLength;
            cabinetSizeH = settings.CabinetSize.X;
            cabinetSizeV = settings.CabinetSize.Y;
            camDist = settings.CamDist;

            // Module Allocation初期化
            aryCellData[0, 2].Visibility = settings.ModulePanel12Visibility;
            aryCellData[1, 2].Visibility = settings.ModulePanel12Visibility;
            aryCellData[2, 2].Visibility = settings.ModulePanel12Visibility;
            aryCellData[3, 2].Visibility = settings.ModulePanel12Visibility;

            aryCellUf[0, 2].Visibility = settings.ModulePanel12Visibility;
            aryCellUf[1, 2].Visibility = settings.ModulePanel12Visibility;
            aryCellUf[2, 2].Visibility = settings.ModulePanel12Visibility;
            aryCellUf[3, 2].Visibility = settings.ModulePanel12Visibility;

            // カーソル移動範囲初期化
            cursorCellY_Max = settings.ModuleYMaxIdx;
            cursorGapCellCellY_Max = settings.ModuleYMaxIdx;

            // Test Pattern Param初期化
            initTestPatternParam();

            // 全TabのCabinet&Module選択された状態を初期化
            initAllUnitToggleButton();

            // Uniformity(Manual)&GapCorrect(Module)のカーソル位置を初期化
            initCursorXYPostion();

            Settings.Ins.LedModuleConfiguration = settings.LEDModuleConfiguration;
            Settings.SaveToXmlFile();
        }

        private void loadConfigurationOfTargetValue(ConfigChrom configChrom, string ledModelName)
        {
            //if (Settings.Ins.ConfigChromType == ConfigChrom.NA)
            //{ tabItemToEnable(true); }

            // ラベル
            lbChromMode.Content = ledModelName;

            if (configChrom == ConfigChrom.Custom)
            {
                pwbConfig.IsEnabled = true;
                if (pwbConfig.Password == customPassword)
                { gdChrom.IsEnabled = true; }
                else
                { gdChrom.IsEnabled = false; }
            }
            else
            {
                pwbConfig.IsEnabled = false;
                gdChrom.IsEnabled = false;
            }

            ChromCustom targetChrom = new ChromCustom(configChrom);

            if (Settings.Ins.ConfigChromType != configChrom)
            {
                Settings.Ins.RelativeTarget = targetChrom;
                Settings.Ins.ConfigChromType = configChrom;
                Settings.SaveToXmlFile();
            }

            txbConfigRed_x.Text = targetChrom.Red.x.ToString("0.000");
            txbConfigRed_y.Text = targetChrom.Red.y.ToString("0.000");
            txbConfigGreen_x.Text = targetChrom.Green.x.ToString("0.000");
            txbConfigGreen_y.Text = targetChrom.Green.y.ToString("0.000");
            txbConfigBlue_x.Text = targetChrom.Blue.x.ToString("0.000");
            txbConfigBlue_y.Text = targetChrom.Blue.y.ToString("0.000");
            txbConfigWhite_x.Text = targetChrom.White.x.ToString("0.000");
            txbConfigWhite_y.Text = targetChrom.White.y.ToString("0.000");
            txbConfigWhite_Y.Text = targetChrom.White.Lv.ToString();
        }


        private void rbChrom_Checked(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as System.Windows.Controls.RadioButton;
            var chrom = ConfigChrom.NA;

            if (radioButton == rbChromZRDB12A)
            {
                chrom = ConfigChrom.ZRD_B12A;
            }
            else if(radioButton == rbChromZRDB15A)
            {
                chrom=ConfigChrom.ZRD_B15A;
            }
            else if(radioButton == rbChromZRDC12A)
            {
                chrom=ConfigChrom.ZRD_C12A;
            }
            else if (radioButton == rbChromZRDC15A)
            {
                chrom=ConfigChrom.ZRD_C15A;
            }
            else if (radioButton == rbChromZRDBH12D)
            {
                chrom = ConfigChrom.ZRD_BH12D;
            }
            else if (radioButton == rbChromZRDBH15D)
            {
                chrom = ConfigChrom.ZRD_BH15D;
            }
            else if (radioButton == rbChromZRDCH12D)
            {
                chrom=ConfigChrom.ZRD_CH12D;
            }
            else if (radioButton == rbChromZRDCH15D)
            {
                chrom = ConfigChrom.ZRD_CH15D;
            }
            else if (radioButton == rbChromZRDBH12D_S3)
            {
                chrom = ConfigChrom.ZRD_BH12D_S3;
            }
            else if (radioButton == rbChromZRDBH15D_S3)
            {
                chrom = ConfigChrom.ZRD_BH15D_S3;
            }
            else if (radioButton == rbChromZRDCH12D_S3)
            {
                chrom = ConfigChrom.ZRD_CH12D_S3;
            }
            else if (radioButton == rbChromZRDCH15D_S3)
            {
                chrom = ConfigChrom.ZRD_CH15D_S3;
            }
            else if (radioButton == rbChromCustom)
            {
                chrom = ConfigChrom.Custom;
            }
            if(chrom != ConfigChrom.NA)
            {
                loadConfigurationOfTargetValue(chrom, (string)radioButton.Content);
                setCAChannel(chrom);
            }
        }

        private void txbConfigRed_x_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (rbChromCustom.IsChecked == true)
            {
                try
                {
                    Settings.Ins.CustomTarget.Red.x = Convert.ToDouble(txbConfigRed_x.Text);
                    Settings.Ins.RelativeTarget = new ChromCustom(ColorPurpose.ConfigCustom);
                    Settings.SaveToXmlFile();
                }
                catch (Exception ex)
                { ShowMessageWindow("Input invalid value.\r\n\r\n" + ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }
            }
        }

        private void txbConfigRed_y_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (rbChromCustom.IsChecked == true)
            {
                try
                {
                    Settings.Ins.CustomTarget.Red.y = Convert.ToDouble(txbConfigRed_y.Text);
                    Settings.Ins.RelativeTarget = new ChromCustom(ColorPurpose.ConfigCustom);
                    Settings.SaveToXmlFile();
                }
                catch (Exception ex)
                { ShowMessageWindow("Input invalid value.\r\n\r\n" + ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }
            }
        }

        private void txbConfigGreen_x_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (rbChromCustom.IsChecked == true)
            {
                try
                {
                    Settings.Ins.CustomTarget.Green.x = Convert.ToDouble(txbConfigGreen_x.Text);
                    Settings.Ins.RelativeTarget = new ChromCustom(ColorPurpose.ConfigCustom);
                    Settings.SaveToXmlFile();
                }
                catch (Exception ex)
                { ShowMessageWindow("Input invalid value.\r\n\r\n" + ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }
            }
        }

        private void txbConfigGreen_y_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (rbChromCustom.IsChecked == true)
            {
                try
                {
                    Settings.Ins.CustomTarget.Green.y = Convert.ToDouble(txbConfigGreen_y.Text);
                    Settings.Ins.RelativeTarget = new ChromCustom(ColorPurpose.ConfigCustom);
                    Settings.SaveToXmlFile();
                }
                catch (Exception ex)
                { ShowMessageWindow("Input invalid value.\r\n\r\n" + ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }
            }
        }

        private void txbConfigBlue_x_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (rbChromCustom.IsChecked == true)
            {
                try
                {
                    Settings.Ins.CustomTarget.Blue.x = Convert.ToDouble(txbConfigBlue_x.Text);
                    Settings.Ins.RelativeTarget = new ChromCustom(ColorPurpose.ConfigCustom);
                    Settings.SaveToXmlFile();
                }
                catch (Exception ex)
                { ShowMessageWindow("Input invalid value.\r\n\r\n" + ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }
            }
        }

        private void txbConfigBlue_y_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (rbChromCustom.IsChecked == true)
            {
                try
                {
                    Settings.Ins.CustomTarget.Blue.y = Convert.ToDouble(txbConfigBlue_y.Text);
                    Settings.Ins.RelativeTarget = new ChromCustom(ColorPurpose.ConfigCustom);
                    Settings.SaveToXmlFile();
                }
                catch (Exception ex)
                { ShowMessageWindow("Input invalid value.\r\n\r\n" + ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }
            }
        }

        private void txbConfigWhite_x_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (rbChromCustom.IsChecked == true)
            {
                try
                {
                    Settings.Ins.CustomTarget.White.x = Convert.ToDouble(txbConfigWhite_x.Text);
                    Settings.Ins.RelativeTarget = new ChromCustom(ColorPurpose.ConfigCustom);
                    Settings.SaveToXmlFile();
                }
                catch (Exception ex)
                { ShowMessageWindow("Input invalid value.\r\n\r\n" + ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }
            }
        }

        private void txbConfigWhite_y_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (rbChromCustom.IsChecked == true)
            {
                try
                {
                    Settings.Ins.CustomTarget.White.y = Convert.ToDouble(txbConfigWhite_y.Text);
                    Settings.Ins.RelativeTarget = new ChromCustom(ColorPurpose.ConfigCustom);
                    Settings.SaveToXmlFile();
                }
                catch (Exception ex)
                { ShowMessageWindow("Input invalid value.\r\n\r\n" + ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }
            }
        }

        private void txbConfigWhite_Y_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (rbChromCustom.IsChecked == true)
            {
                try
                {
                    Settings.Ins.CustomTarget.White.Lv = Convert.ToDouble(txbConfigWhite_Y.Text);
                    Settings.Ins.RelativeTarget = new ChromCustom(ColorPurpose.ConfigCustom);
                    Settings.SaveToXmlFile();
                }
                catch (Exception ex)
                { ShowMessageWindow("Input invalid value.\r\n\r\n" + ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }
            }    
        }

        #endregion Luminance / Chromaticity

        #region Uniformity(Camera) / Gap Correction(Camera)

        private void rbConfigWallFormFlat_Checked(object sender, RoutedEventArgs e)
        {
            if (txbConfigWallForm != null)
            { txbConfigWallForm.IsEnabled = false; }
            if (btnConfigWallForm != null)
            { btnConfigWallForm.IsEnabled = false; }

            Settings.Ins.Form = WallForm.Flat;
        }

        private void rbConfigWallFormCurve_Checked(object sender, RoutedEventArgs e)
        {
            if (txbConfigWallForm != null)
            { txbConfigWallForm.IsEnabled = true; }
            if (btnConfigWallForm != null)
            { btnConfigWallForm.IsEnabled = true; }

            Settings.Ins.Form = WallForm.Curve;
        }

        private void txbConfigDist_TextChanged(object sender, TextChangedEventArgs e)
        {
            try { Settings.Ins.CameraDist = double.Parse(txbConfigDist.Text); }
            catch
            {
                Settings.Ins.CameraDist = 0;
                txbConfigDist.Text = Settings.Ins.CameraDist.ToString();
            }
        }

        private void txbConfigWallForm_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.Ins.WallFormFile = txbConfigWallForm.Text;
        }

        private void btnConfigWallForm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Wall形状ファイル選択
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.FileName = "";
                    if (string.IsNullOrWhiteSpace(Settings.Ins.WallFormFile) == false)
                    { ofd.InitialDirectory = System.IO.Path.GetDirectoryName(Settings.Ins.WallFormFile); }
                    else
                    { ofd.InitialDirectory = "C:\\"; }
                    ofd.Filter = "PRN File(*.prn)|*.prn|All File(*.*)|*.*";
                    ofd.FilterIndex = 1;
                    ofd.Title = "Please select a Wall Form File.";
                    ofd.RestoreDirectory = true;
                    ofd.CheckFileExists = true;
                    ofd.CheckPathExists = true;

                    if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        Settings.Ins.WallFormFile = ofd.FileName;
                        txbConfigWallForm.Text = ofd.FileName;

                        // Wall Formファイルの内容とProfileの整合性をチェック
                        try { LoadWallFormFile(out double[] rotateAngle); }
                        catch (Exception ex)
                        { ShowMessageWindow(ex.Message, "Caution!", System.Drawing.SystemIcons.Information, 300, 200); }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "CAS Error!", System.Drawing.SystemIcons.Error, 500, 210);
                return;
            }
        }

        private void txbConfigWallHeight_TextChanged(object sender, TextChangedEventArgs e)
        {
            try { Settings.Ins.WallBottomH = double.Parse(txbConfigWallHeight.Text); }
            catch
            {
                Settings.Ins.WallBottomH = 0;
                txbConfigWallHeight.Text = Settings.Ins.WallBottomH.ToString();
            }
        }

        private void txbConfigCamHeight_TextChanged(object sender, TextChangedEventArgs e)
        {
            try { Settings.Ins.CameraH = double.Parse(txbConfigCamHeight.Text); }
            catch
            {
                Settings.Ins.CameraH = 0;
                txbConfigCamHeight.Text = Settings.Ins.CameraH.ToString();
            }
        }

        private void rbConfigCamPosDefault_Checked(object sender, RoutedEventArgs e)
        {
            if (lbConfigCamPosWallBtm != null)
            { lbConfigCamPosWallBtm.IsEnabled = false; }
            if (txbConfigWallHeight != null)
            { txbConfigWallHeight.IsEnabled = false; }
            if (lbConfigCamPosWallBtmUnit != null)
            { lbConfigCamPosWallBtmUnit.IsEnabled = false; }
            if (lbConfigCamPosCamH != null)
            { lbConfigCamPosCamH.IsEnabled = false; }
            if (txbConfigCamHeight != null)
            { txbConfigCamHeight.IsEnabled = false; }
            if (lbConfigCamPosCamHUnit != null)
            { lbConfigCamPosCamHUnit.IsEnabled = false; }
            if(lbConfigRecommend != null)
            { lbConfigRecommend.IsEnabled = false; }
            if (lbConfigWallHeight != null)
            { lbConfigWallHeight.IsEnabled = false; }
            if (lbConfigWallVSize != null)
            { lbConfigWallVSize.IsEnabled = false; }

            Settings.Ins.CameraInstPos = CameraInstallPos.Default;
        }

        private void rbConfigCamPosCustom_Checked(object sender, RoutedEventArgs e)
        {
            if (lbConfigCamPosWallBtm != null)
            { lbConfigCamPosWallBtm.IsEnabled = true; }
            if (txbConfigWallHeight != null)
            { txbConfigWallHeight.IsEnabled = true; }
            if (lbConfigCamPosWallBtmUnit != null)
            { lbConfigCamPosWallBtmUnit.IsEnabled = true; }
            if (lbConfigCamPosCamH != null)
            { lbConfigCamPosCamH.IsEnabled = true; }
            if (txbConfigCamHeight != null)
            { txbConfigCamHeight.IsEnabled = true; }
            if (lbConfigCamPosCamHUnit != null)
            { lbConfigCamPosCamHUnit.IsEnabled = true; }
            if (lbConfigRecommend != null)
            { lbConfigRecommend.IsEnabled = true; }
            if (lbConfigWallHeight != null)
            { lbConfigWallHeight.IsEnabled = true; }
            if (lbConfigWallVSize != null)
            { lbConfigWallVSize.IsEnabled = true; }

            Settings.Ins.CameraInstPos = CameraInstallPos.Custom;
        }

        #endregion Uniformity(Camera) / Gap Correction(Camera)

        //private void rbPanelTypeNormal_Checked(object sender, RoutedEventArgs e)
        //{
        //    Settings.Ins.PanelType = PanelType.Normal;
        //    Settings.Ins.RelativeTarget = new ChromCustom(Settings.Ins.ConfigChromType);
        //    Settings.SaveToXmlFile();

        //    lbPanelType.Visibility = System.Windows.Visibility.Collapsed;
        //}

        //private void rbPanelTypeDigitalCinema_Checked(object sender, RoutedEventArgs e)
        //{
        //    Settings.Ins.PanelType = PanelType.DigitalCinema2017;
        //    ////Settings.Ins.RelativeTarget = new ChromCustom(Settings.Ins.ConfigChromType);
        //    ////Settings.SaveToXmlFile();

        //    ////lbPanelType.Content = "Digital Cinema 2017";
        //    ////lbPanelType.Visibility = System.Windows.Visibility.Visible;
        //}

        //private void rbPanelTypeDigitalCinema2020_Checked(object sender, RoutedEventArgs e)
        //{
        //    Settings.Ins.PanelType = PanelType.DigitalCinema2020;
        //    ////Settings.Ins.RelativeTarget = new ChromCustom(Settings.Ins.ConfigChromType);
        //    ////Settings.SaveToXmlFile();

        //    ////lbPanelType.Content = "Digital Cinema 2020";
        //    ////lbPanelType.Visibility = System.Windows.Visibility.Visible;
        //}

        //private void rbGamma22_Checked(object sender, RoutedEventArgs e)
        //{
        //    Settings.Ins.Gamma = Gamma.Gamma22;
        //    brightness = new Brightness(Settings.Ins.Gamma);
        //    initTestPatternParam();
        //    setPatternTextBox();
        //    Settings.Ins.RelativeTarget = new ChromCustom(Settings.Ins.ConfigChromType);
        //    Settings.SaveToXmlFile();

        //    lbGamma.Visibility = System.Windows.Visibility.Collapsed;
        //}

        //private void rbGamma26_Checked(object sender, RoutedEventArgs e)
        //{
        //    Settings.Ins.Gamma = Gamma.Gamma26;
        //    brightness = new Brightness(Settings.Ins.Gamma);
        //    initTestPatternParam();
        //    setPatternTextBox();
        //    Settings.Ins.RelativeTarget = new ChromCustom(Settings.Ins.ConfigChromType);
        //    Settings.SaveToXmlFile();

        //    lbGamma.Content = "2.6";
        //    lbGamma.Visibility = System.Windows.Visibility.Visible;
        //}

        //private void rbGammaPQ_Checked(object sender, RoutedEventArgs e)
        //{
        //    Settings.Ins.Gamma = Gamma.PQ;
        //    brightness = new Brightness(Settings.Ins.Gamma);
        //    initTestPatternParam();
        //    setPatternTextBox();
        //    Settings.Ins.RelativeTarget = new ChromCustom(Settings.Ins.ConfigChromType);
        //    Settings.SaveToXmlFile();

        //    lbGamma.Content = "PQ";
        //    lbGamma.Visibility = System.Windows.Visibility.Visible;
        //}

        #endregion Events

        #region Private Methods

        private void setCAChannel(ConfigChrom configChrom)
        {
            switch (configChrom)
            {
                case ConfigChrom.ZRD_B12A:
                case ConfigChrom.ZRD_B15A:
                case ConfigChrom.ZRD_C12A:
                case ConfigChrom.ZRD_C15A:
                    cmbxCA410Channel.SelectedIndex = Settings.Ins.Channel_ModelA;
                    cmbxCA410ChannelMeas.SelectedIndex = Settings.Ins.Channel_ModelA;
                    cmbxCA410ChannelCalib.SelectedIndex = Settings.Ins.Channel_ModelA - 1; // CalibrationではIndex 0: Ch01
                    cmbxCA410ChannelRelTgt.SelectedIndex = Settings.Ins.Channel_ModelA;
                    cmbxCA410ChannelGain.SelectedIndex = Settings.Ins.Channel_ModelA;
                    break;
                case ConfigChrom.ZRD_BH12D:
                case ConfigChrom.ZRD_BH15D:
                case ConfigChrom.ZRD_CH12D:
                case ConfigChrom.ZRD_CH15D:
                case ConfigChrom.ZRD_BH12D_S3:
                case ConfigChrom.ZRD_BH15D_S3:
                case ConfigChrom.ZRD_CH12D_S3:
                case ConfigChrom.ZRD_CH15D_S3:
                    cmbxCA410Channel.SelectedIndex = Settings.Ins.Channel_ModelD;
                    cmbxCA410ChannelMeas.SelectedIndex = Settings.Ins.Channel_ModelD;
                    cmbxCA410ChannelCalib.SelectedIndex = Settings.Ins.Channel_ModelD - 1; // CalibrationではIndex 0: Ch01
                    cmbxCA410ChannelRelTgt.SelectedIndex = Settings.Ins.Channel_ModelD;
                    cmbxCA410ChannelGain.SelectedIndex = Settings.Ins.Channel_ModelD;
                    break;
                default:
                    cmbxCA410Channel.SelectedIndex       = Settings.Ins.Channel_Custom;
                    cmbxCA410ChannelMeas.SelectedIndex   = Settings.Ins.Channel_Custom;
                    cmbxCA410ChannelCalib.SelectedIndex  = Settings.Ins.Channel_Custom - 1; // CalibrationではIndex 0: Ch01
                    cmbxCA410ChannelRelTgt.SelectedIndex = Settings.Ins.Channel_Custom;
                    cmbxCA410ChannelGain.SelectedIndex   = Settings.Ins.Channel_Custom;
                    break;
            }
        }

        private int getCAChannel()
        {
            ConfigChrom configChrom = Settings.Ins.ConfigChromType;

            switch (configChrom)
            {
                case ConfigChrom.ZRD_B12A:
                case ConfigChrom.ZRD_B15A:
                case ConfigChrom.ZRD_C12A:
                case ConfigChrom.ZRD_C15A:
                    return Settings.Ins.Channel_ModelA;
                case ConfigChrom.ZRD_BH12D:
                case ConfigChrom.ZRD_BH15D:
                case ConfigChrom.ZRD_CH12D:
                case ConfigChrom.ZRD_CH15D:
                case ConfigChrom.ZRD_BH12D_S3:
                case ConfigChrom.ZRD_BH15D_S3:
                case ConfigChrom.ZRD_CH12D_S3:
                case ConfigChrom.ZRD_CH15D_S3:
                    return Settings.Ins.Channel_ModelD;
                default:
                    return Settings.Ins.Channel_Custom;
            }
        }

        private void changCAChannel(int targetChannel)
        {
            ConfigChrom configChrom = Settings.Ins.ConfigChromType;

            switch (configChrom)
            {
                case ConfigChrom.ZRD_B12A:
                case ConfigChrom.ZRD_B15A:
                case ConfigChrom.ZRD_C12A:
                case ConfigChrom.ZRD_C15A:
                    Settings.Ins.Channel_ModelA = targetChannel;
                    break;
                case ConfigChrom.ZRD_BH12D:
                case ConfigChrom.ZRD_BH15D:
                case ConfigChrom.ZRD_CH12D:
                case ConfigChrom.ZRD_CH15D:
                case ConfigChrom.ZRD_BH12D_S3:
                case ConfigChrom.ZRD_BH15D_S3:
                case ConfigChrom.ZRD_CH12D_S3:
                case ConfigChrom.ZRD_CH15D_S3:
                    Settings.Ins.Channel_ModelD = targetChannel;
                    break;
                default:
                    Settings.Ins.Channel_Custom = targetChannel;
                    break;
            }
        }

        private void cmbxGamePadProfile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                uint selectedIndex = gamePadViewModel.SelectGamePadProfile;

                if (gamePadCmdFunc.GamePadCmdProfileData.SelectProFileIndex != selectedIndex)
                {
                    //GamePadProfileの選択情報を更新
                    gamePadCmdFunc.setGamePadProfileIndex(selectedIndex);
                    //GamePadDevice生成
                    gamePadDevice = new GamePadDevice(gamePadCmdFunc.getGamePadProfilePath(), gamePadViewModel);
                    //GmamePadコマンド初期設定
                    gamePadCmdFunc.commandInit(gamePadDevice.GamePadProfileData);
                }
            }
            catch
            {
                // エラーメッセージなど特に何もしない
            }
        }

        #endregion Private Methods
    }
}
