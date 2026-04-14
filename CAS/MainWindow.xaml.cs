using CAS.Windows;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using SONY.Modules;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;

namespace CAS
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Constants

        const string TempDir = @"\Temp\";
        const string MeasDir = @"\Measurement\";
        const string CompDir = @"\Components\";
        const string LogDir = @"\Log\";

        // CLED
        //const int cabiDx = 320;
        //const int cabiDy = 360;
        //const int modDx = 80;
        //const int modDy = 120;
        //public const int CellCount = 12;
        //public const int ModuleCount = 8;

        // Chiron & Cancun (Cabinet)
        // 1.2P
        const int CabinetDxP12 = 480;
        const int CabinetDyP12 = 270;

        // 1.5P
        const int CabinetDxP15 = 384;
        const int CabinetDyP15 = 216;

        // Chiron (Module)
        // 1.2P
        const int ModuleDxP12_Mdoule4x2 = 120;
        const int ModuleDyP12_Module4x2 = 135;
        // 1.5P
        const int ModuleDxP15_Module4x2 = 96;
        const int ModuleDyP15_Module4x2 = 108;

        // Cancun (Module)
        // 1.2P
        const int ModuleDxP12_Module4x3 = 120;
        const int ModuleDyP12_Module4x3 = 90;
        // 1.5P
        const int ModuleDxP15_Module4x3 = 96;
        const int ModuleDyP15_Module4x3 = 72;

        const int CabinetLength_P12 = 8; // 4Kサイズの1辺のCabinet数 今はH, V共通
        const int CabinetLength_P15 = 10; // 4Kサイズの1辺のCabinet数
        const int CabinetCount_P12 = 64; // 4KサイズのCabinet数
        const int CabinetCount_P15 = 100; // 4KサイズのCabinet数

        const double CabinetSizeH_Chiron = 608.0; // [mm]
        const double CabinetSizeV_Chiron = 342.0; // [mm]
        const double CabinetSizeH_Cancun = 609.89; // [mm]
        const double CabinetSizeV_Cancun = 343.06; // [mm]

        // データ構造
        // Common Header
        public const int CommonHeaderLength                     = 32; // 全体のHeaderデータ長
        public const int CommonHeaderAddress                    = 0;  // 全体のHeaderデータ開始アドレス
        public const int CommonHeaderDataTypeOffset             = 8;  // 全体のHeaderデータタイプの位置
        public const int CommonHeaderDataTypeSize               = 4;  // 全体のHeaderデータタイプのサイズ
        public const int CommonHeaderDataLengthOffset           = CommonHeaderAddress + 12; // 全体のHeaderデータの本体データ長開始アドレス
        public const int CommonHeaderDataLengthSize             = 4;
        public const int CommonHeaderDataCrcOffset              = CommonHeaderAddress + 16; // 全体のHeader Data CRCの位置
        public const int CommonHeaderDataCrcSize                = 4;  // 全体のData CRCのサイズ
        public const int CommonHeaderHeaderCrcRange             = 28; // 全体のHeader Header Data CRC計算範囲
        public const int CommonHeaderHeaderCrcOffset            = CommonHeaderAddress + 28; // 全体のHeader Header CRCの位置
        public const int CommonHeaderHeaderCrcSize              = 4;  // 全体のHeader Header CRCのサイズ

        // Product Info
        public const int ProductInfoLength                      = 288; // Product Infoデータ長
        public const int ProductInfoDataLength                  = 256; // Product Info Dataデータ長
        public const int ProductInfoAddress                     = CommonHeaderLength; // Product Infoデータ開始アドレス
        public const int ProductInfoDataTypeOffset              = ProductInfoAddress + 8;  // Product Info Data Typeの位置
        public const int ProductInfoDataTypeSize                = 4;  // Product Info Data Typeのサイズ
        public const int ProductInfoDataCrcOffset               = ProductInfoAddress + 16; // Product Info Data CRCの位置
        public const int ProductInfoDataCrcSize                 = 4;  // Product Info Data CRCのサイズ
        public const int ProductInfoHeaderCrcRange              = 28; // Product Info Header CRC計算範囲
        public const int ProductInfoHeaderCrcOffset             = ProductInfoAddress + 28; // Product Info Header CRCの位置
        public const int ProductInfoHeaderCrcSize               = 4;  // Product Info Header CRCのサイズ
        public const int ProductInfoDataAddress                 = ProductInfoAddress + 32; // Product Info Dataの開始アドレス
        public const int ProductInfoDataModelNameOffset         = ProductInfoAddress + 36; // Product Info Data Model Nameの位置
        public const int ProductInfoDataModelNameSize           = 32; // Product Info Data Model Nameのサイズ
        public const int ProductInfoDataSerialNoOffset          = ProductInfoAddress + 68; // Product Info Data Serial Numberの位置
        public const int ProductInfoDataSerialNoSize            = 4;  // Product Info Data Serial No.のサイズ

        // Unit Data関連
        // Chiron
        public const int ModuleCount_Module4x2                  = 8;  // 1Cabinetあたりの総Module数
        public const int UdDataLength_Module4x2                 = ProductInfoLength + UdInputGammaLength * InputGammaTableCount + UdOutputGammaLength * OutputGammaTableCount + UdModuleInfoLength * ModuleCount_Module4x2;  // Product Info以降のデータ長(95648-32)

        // Cancun
        public const int ModuleCount_Module4x3                  = 12; // 1Cabinetあたりの総Module数
        public const int UdDataLength_Module4x3                 = ProductInfoLength + UdInputGammaLength * InputGammaTableCount + UdOutputGammaLength * OutputGammaTableCount + UdModuleInfoLength * ModuleCount_Module4x3; // Product Info以降のデータ長(96800-32)

        public const int InputGammaTableCount                   = 3;  // Unit Data構造のInput Gamma Table数
        public const int OutputGammaTableCount                  = 6;  // Unit Data構造のOutput Gamma Table数

        public const int UdInputGammaLength                     = 12384; // Input Gammaデータ長、3つある
        public const int UdFirstInputGammaAddress               = CommonHeaderLength + ProductInfoLength; // Input Gammaデータ開始アドレス
        public const int UdFirstInputGammaDataTypeOffset        = UdFirstInputGammaAddress + 8;   // Input Gamma Data Typeの位置
        public const int UdInputGammaDataTypeSize               = 4;   // Input Gamma Data Typeのサイズ
        public const int UdInputGammaHeaderCrcRange             = 28;  // Input Gamma Header CRC計算範囲
        public const int UdFirstInputGammaHeaderCrcOffset       = UdFirstInputGammaAddress + 28;  // Input Gamma Header CRCの位置
        public const int UdInputGammaHeaderCrcSize              = 4;   // Input Gamma Header CRCのサイズ

        public const int UdOutputGammaLength                    = 9312;// Output Gammaデータ長、6つある
        public const int UdFirstOutputGammaAddress              = CommonHeaderLength + ProductInfoLength + UdInputGammaLength * InputGammaTableCount; // Output Gammaデータ開始アドレス
        public const int UdFirstOutputGammaDataTypeOffset       = UdFirstOutputGammaAddress + 8;   // Output Gamma Data Typeの位置
        public const int UdOutputGammaDataTypeSize              = 4;   // Output Gamma Data Typeのサイズ
        public const int UdOutputGammaHeaderCrcRange            = 28;  // Output Gamma Header CRC計算範囲
        public const int UdFirstOutputGammaHeaderCrcOffset      = UdFirstOutputGammaAddress + 28;  // Output Gamma Header CRCの位置
        public const int UdOutputGammaHeaderCrcSize             = 4;   // Output Gamma Header CRCのサイズ

        public const int UdModuleInfoLength                     = 288; // LED Module Infoデータ長、12個ある
        public const int UdFirstModuleInfoAddress               = CommonHeaderLength + ProductInfoLength + UdInputGammaLength * InputGammaTableCount + UdOutputGammaLength * OutputGammaTableCount; // Module Infoデータ開始アドレス
        public const int UdFirstModuleInfoDataTypeOffset        = UdFirstModuleInfoAddress + 8;   // Module Info Data Typeの位置
        public const int UdModuleInfoDataTypeSize               = 4;   // Module Info Data Typeのサイズ
        public const int UdModuleInfoHeaderCrcRange             = 28;  // Module Info Header CRC計算範囲
        public const int UdFirstModuleInfoHeaderCrcOffset       = UdFirstModuleInfoAddress + 28;  // Module Info Header CRCの位置
        public const int UdModuleInfoHeaderCrcSize              = 4;   // Module Info Header CRCのサイズ
        public const int UdFirstModuleSerialNoOffset            = 68;  // Moduleシリアル番号の位置
        public const int UdModuleSerialNoOffset                 = 36;  // Module Info Dataからシリアル番号の位置
        public const int UdModuleSerialNoSize                   = 4;   // Moduleシリアル番号のサイズ
        public const int UdModuleInfoDataLength                 = 256; // LED Module Info Dataデータ長

        // Uniformity High Data関連
        // Chiron 1.2P
        public const int HcDataLengthP12_Module4x2              = 1039392; //1039424 - 32 hc.binデータの中でProduct Info以降のデータ長
        public const int HcModuleDataLengthP12_Module4x2        = 129888; // 1Module分のデータ長、8つある
        public const int HcCcDataLengthP12_Module4x2            = 129856; // Color Correction Dataのデータ長

        // Chiron 1.5P
        public const int HcDataLengthP15_Module4x2              = 666144; //666176 - 32 hc.binデータの中でProduct Info以降のデータ長
        public const int HcModuleDataLengthP15_Module4x2        = 83232; // 1Module分のデータ長、8つある
        public const int HcCcDataLengthP15_Module4x2            = 83200; // Color Correction Dataのデータ長

        // Cancun 1.2P
        public const int HcDataLengthP12_Module4x3              = 1040544; //1040576 - 32 hc.binデータの中でProduct Info以降のデータ長
        public const int HcModuleDataLengthP12_Module4x3        = 86688; // 1Module分のデータ長、12個ある
        public const int HcCcDataLengthP12_Module4x3            = 86656; // Color Correction Dataのデータ長

        // Cancun 1.5P
        public const int HcDataLengthP15_Module4x3              = 667296; //667328 - 32 hc.binデータの中でProduct Info以降のデータ長
        public const int HcModuleDataLengthP15_Module4x3        = 55584; // 1Module分のデータ長、12個ある
        public const int HcCcDataLengthP15_Module4x3            = 55552; // Color Correction Dataのデータ長

        public const int HcFirstCcAddress                       = CommonHeaderLength + ProductInfoLength;  // Color Correctionデータ開始アドレス
        public const int HcFirstCcDataTypeOffset                = HcFirstCcAddress + 8;  // Color Correction Data Typeの位置
        public const int HcCcDataTypeOffset                     = 8;  // Color Correction Data Typeの位置
        public const int HcCcDataTypeSize                       = 4;  // Color Correction Data Typeのサイズ
        public const int HcFirstCcDataOffset                    = HcFirstCcAddress + 32; // Color Correction Dataの位置
        public const int HcFirstCcDataCrcOffset                 = HcFirstCcAddress + 16; // Color Correction DataのCRCの位置
        public const int HcCcDataCrcSize                        = 4;  // Color Correction DataのCRCのサイズ
        public const int HcCcDataOption2Offset                  = 24; // Color Correction DataのOption2のサイズ
        public const int HcCcDataHeaderCrcRange                 = 28; // Color Correction DataのCRC計算範囲
        public const int HcFirstCcDataHeaderCrcOffset           = HcFirstCcAddress + 28; // Color Correction Data HeaderのCRCの位置
        public const int HcCcDataHeaderCrcSize                  = 4;  // Color Correction DataのCRCサイズ 
        public const int HcCcDataCtcDataValidIndicatorOffset    = 48; // Color Correction DataからCrosstalk Correction Data Valid Indicatorの位置
        public const int HcCcDataCtcDataValidIndicatorSize      = 4;  // Crosstalk Correction Data Valid Indicatorのサイズ
        public const int HcCcDataCtcHighRedOffset               = 52; // 高諧調Crosstalk補正量(R)の位置
        public const int HcCcDataCtcHighGreenOffset             = 56; // 高諧調Crosstalk補正量(G)の位置
        public const int HcCcDataCtcHighBlueOffset              = 60; // 高諧調Crosstalk補正量(B)の位置
        public const int HcCcDataCtcHighColorSize               = 4;  // 高諧調Crosstalk補正量(R/G/B)のサイズ

        // Uniformity Low Data関連
        // Chiron 1.2P
        public const int LcDataLengthP12_Module4x2              = 390368; //390400 - 32 lc.binデータの中でProduct Info以降のデータ長
        public const int LcFcclModuleDataLengthP12_Module4x2    = 48760; // 1Module(FCCL)分のデータ長、8つある
        public const int LcFcclDataLengthP12_Module4x2          = 48728; // FCCL Dataのデータ長

        // Chiron 1.5P
        public const int LcDataLengthP15_Module4x2              = 250400; //250432 - 32 lc.binデータの中でProduct Info以降のデータ長
        public const int LcFcclModuleDataLengthP15_Module4x2    = 31264; // 1Module分のデータ長、8つある
        public const int LcFcclDataLengthP15_Module4x2          = 31232; // FCCL Dataのデータ長

        // Cancun 1.2P
        public const int LcDataLengthP12_Module4x3              = 110496; //110528 - 32 lc.binデータの中でProduct Info以降のデータ長
        public const int LcLcalVModuleDataLengthP12_Module4x3   = 1248; // 1Module(Low Calibration V)分のデータ長、12つある
        public const int LcLcalVDataLengthP12_Module4x3         = 1216; // Low Calibration Vのデータ長
        public const int LcLcalHModuleDataLengthP12_Module4x3   = 3168; // 1Module(Low Calibration H)分のデータ長、12つある
        public const int LcLcalHDataLengthP12_Module4x3         = 3136; // Low Calibration Hのデータ長
        public const int LcLmcalModuleDataLengthP12_Module4x3   = 4608; // 1Module(Lower-Mid Calibration)分のデータ長、12つある
        public const int LcLmcalDataLengthP12_Module4x3         = 4576; // Lower-Mid Calibrationのデータ長

        // Cancun 1.5P
        public const int LcDataLengthP15_Module4x3              = 84000; //84032 - 32 lc.binデータの中でProduct Info以降のデータ長
        public const int LcLcalVModuleDataLengthP15_Module4x3   = 1056; // 1Module(Low Calibration V)分のデータ長、12つある
        public const int LcLcalVDataLengthP15_Module4x3         = 1024; // Low Calibration Vのデータ長
        public const int LcLcalHModuleDataLengthP15_Module4x3   = 2016; // 1Module(Low Calibration H)分のデータ長、12つある
        public const int LcLcalHDataLengthP15_Module4x3         = 1984; // Low Calibration Hのデータ長
        public const int LcLmcalModuleDataLengthP15_Module4x3   = 3744; // 1Module(Lower-Mid Calibration)分のデータ長、12つある
        public const int LcLmcalDataLengthP15_Module4x3         = 3712; // Lower-Mid Calibrationのデータ長

        public const int LcMgamModuleDataLength                 = 160; // 1Module(LED Module Gamma)分のデータ長、12つある
        public const int LcMgamDataLength                       = 128; // LED Module Gammaのデータ長

        public const int LcFirstModuleAddress                   = CommonHeaderLength + ProductInfoLength;  // LC First Moduleデータ開始アドレス
        public const int LcFirstModuleDataTypeOffset            = LcFirstModuleAddress + 8;  // LC First Module Data Typeの位置
        public const int LcFirstModuleDataTypeSize              = 4;  // LC First Module Data Typeのサイズ
        public const int LcFirstModuleDataOffset                = LcFirstModuleAddress + 32; // LC First Module Dataの位置
        public const int LcFirstModuleDataCrcOffset             = LcFirstModuleAddress + 16; // LC First Module DataのCRCの位置
        public const int LcFirstModuleDataCrcSize               = 4;  // LC First Module DataのCRCのサイズ
        public const int LcFirstModuleDataHeaderCrcRange        = 28; // LC First Module DataのCRC計算範囲
        public const int LcFirstModuleDataHeaderCrcOffset       = LcFirstModuleAddress + 28; // LC First Module Data HeaderのCRCの位置
        public const int LcFirstModuleDataHeaderCrcSize         = 4;  // LC First Module DataのCRCサイズ

        public const int LcMgamModuleValidDataIndicatorOffset   = 12; // LED Module Gamma DataからValid Data Indicatorの位置
        public const int LcMgamModuleValidDataIndicatorSize     = 4;  // Valid Data Indicatorのサイズ

        // HF Data関連
        // Chiron 1.2P
        public const int HfDataLengthP12_Module4x2              = 1039392; //1039424 - 32 hc.binデータの中でProduct Info以降のデータ長

        // Cancun 1.2P
        public const int HfDataLengthP12_Module4x3              = 1040544; //1040576 - 32 hc.binデータの中でProduct Info以降のデータ長

        public const int HfModuleDataLengthP12                  = 129888; // 1Module分のデータ長、8つある
        public const int HfCcDataLengthP12                      = 129856; // Color Correction Dataのデータ長

        // Chiron 1.5P
        public const int HfDataLengthP15_Module4x2              = 666144; //666176 - 32 hc.binデータの中でProduct Info以降のデータ長

        // Cancun 1.5P
        public const int HfDataLengthP15_Module4x3              = 667296; //667328 - 32 hc.binデータの中でProduct Info以降のデータ長

        public const int HfModuleDataLengthP15                  = 83232; // 1Module分のデータ長、8つある
        public const int HfCcDataLengthP15                      = 83200; // Color Correction Dataのデータ長

        // Data Type
        public const uint dt_Module4x2                          = 0xFF000000;
        public const uint dt_Module4x3                          = 0xFF011000;
        public const uint dt_LedModuleInfoRead                  = 0x00001000;
        public const uint dt_ColorCorrectionRead                = 0x00001100;
        public const uint dt_MgamRead                           = 0x00001700;

        const int dt_DummyData                                  = 0x00000801;
        const int dt_ProductInfoWrite                           = 0x00000101;
        const int dt_InputGammaWrite                            = 0x00000201;
        const int dt_OutputGammaWrite                           = 0x00000301;
        public const uint dt_ColorCorrectionWrite               = 0x00001101;
        const int dt_AgingCorrectionnWrite                      = 0x00001201;
        const int dt_FcclWrite                                  = 0x00001301;
        const int dt_LcalVWrite                                 = 0x00001401;
        const int dt_LcalHWrite                                 = 0x00001501;
        const int dt_LmcalWrite                                 = 0x00001601;
        const int dt_MgamWrite                                  = 0x00001701;

        // Option2
        public const uint op2_Current                           = 0x00000001;

        // dllをロードするときに実行するアクション
        const uint LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800;

        // Logを何世代分まで保存するか
        const int logMaxGen = 30;

        #endregion Constants

        #region Fields

        private const string ftpUser = "udver01";
        private const string ctlFtpUser = "dbver01"; // サービス用基盤Backup&Restore時FTPログイン認証

        private const string customPassword = "SONYCLED";

        public const string ZRD_C12A = "ZRD-C12A";
        public const string ZRD_C15A = "ZRD-C15A";
        public const string ZRD_B12A = "ZRD-B12A";
        public const string ZRD_B15A = "ZRD-B15A";
        public const string ZRD_CH12D = "ZRD-CH12D";
        public const string ZRD_CH15D = "ZRD-CH15D";
        public const string ZRD_BH12D = "ZRD-BH12D";
        public const string ZRD_BH15D = "ZRD-BH15D";
        public const string ZRD_CH12D_S3 = "ZRD-CH12D/3";
        public const string ZRD_CH15D_S3 = "ZRD-CH15D/3";
        public const string ZRD_BH12D_S3 = "ZRD-BH12D/3";
        public const string ZRD_BH15D_S3 = "ZRD-BH15D/3";
        public const string CUSTOM = "Custom";
        public const string NA = "N.A.";

        private ApplicationMode appliMode = ApplicationMode.Normal;

        private bool settingSaveFlag = true;
        private bool loadSuccess = true;
        private static string applicationPath;
        private string tempPath;
        private string version;
        private string copyright;
        //private bool extendedDataDisable = true;
        //private bool? supportHdcpCmd = null;

        private UnitToggleButton[,] aryUnitUf, aryCellUf, aryUnitUfCam, aryUnitData, aryCellData, aryUnitInfo, aryUnitMeas, aryUnitCalib, aryUnitRelTgt, aryCellRelTgt, aryUnitGapCam, aryUnitGg, aryCellGg, arySmUnit;

        private Brush UnitDefaultBrush;

        // 構成情報ファイル
        private string profilePath;
        private bool profileLoaded = false; // 構成情報のファイルをロードしたかどうか
        
        // 内部用構成情報
        private Profile DCSProfileData;
        private ConfigurationInfo CASProfileData;
        private ProfileType profileType;
        private Dictionary<int, ControllerInfo> dicController;
        private AllocationInfo allocInfo;
        private ObservableCollection<ControllerInfo> contData; // 一覧表表示用
        //private string CabinetModel;        
        private ObservableCollection<ControllerHDCPInfo> hdcpData; // 一覧表表示用

        // MP3音源再生用
        [System.Runtime.InteropServices.DllImport("winmm.dll")]
        private static extern int mciSendString(String command, StringBuilder buffer, int bufferSize, IntPtr hwndCallback);
        private string aliasName = "MediaFile";

        //WindowBackupProgress winBackup;
        WindowProgress winProgress;

        // Temp Error(No.2)を無視するかどうか
        private bool ignoreTempError = false;

        // Chiorn
        //private ChironModel chironModel; // P1.2 or P1.5

        // 現在のモデルのCabinet、Moduleの画素数
        private int cabiDx = 0;
        private int cabiDy = 0;
        private int modDx = 0;
        private int modDy = 0;

        // Uniformity(Camera)
        private System.Windows.Forms.Timer timerUfCam;

        // Gap(Cam)
        private System.Windows.Forms.Timer timerGapCam;

        // Replace 一時的保持データ用
        private byte[] cabiModelName;
        private byte[] cabiSerialNo;
        private string targetPath;
        private bool changedCabiData = false;

        // Led Model切替時保持用
        private int moduleCount;
        private int udDataLength;
        private int hcDataLength;
        private int hcModuleDataLength;
        private int hcCcDataLength;
        private int lcDataLength;
        private int lcFcclModuleDataLength;
        private int lcFcclDataLength;
        private int lcLcalVModuleDataLength;
        private int lcLcalHModuleDataLength;
        private int lcLmcalModuleDataLength;
        private int lcMgamModuleDataLength;

        // Cabinetの物理的なサイズ
        private double cabinetSizeH; // [mm]
        private double cabinetSizeV; // [mm]
        private double camDist; // [mm] 撮影距離

        private int cursorCellY_Max;
        private int cursorGapCellCellY_Max;

        private bool shouldReferCasSetting = false;

        //HID Device
        private GamePadDevice gamePadDevice = null;
        private GamePadViewModel gamePadViewModel = null;
        private GamePadCmdFunc gamePadCmdFunc = null;

        //GamePad using Tab Index
        private int TabIndexUniformityManual = 2;
        private int TabIndexGapCorrectionModule = 5;

        // Cabinet Allocationの段/列ガイドの幅と高さ
        private int cabinetAllocRowHeaderHeight = 23;
        private int cabinetAllocColumnHeaderWidth = 23;
        public enum logKey
        {
            ExecLog,
            SDCPLog,
        }

        #endregion Fields

        public MainWindow()
        {
            InitializeComponent();

            SetDefaultDllDirectories(LOAD_LIBRARY_SEARCH_SYSTEM32); // システムディレクトリー以外からDLLをロードできない
            SetDllDirectory(""); // カレントディレクトリーが検索されない

            #region Licenseはやらないことに
            //if (LicenseManager.CheckLicense() != true)
            //{
            //    string msg = "License is invalid.\r\nPlease inquiry to a system administrator.";
            //    MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //    profileLoaded = true; // 構成情報の保存を聞かれないようにするため
            //    settingSaveFlag = false;
            //    this.Close();
            //    return;
            //}
            #endregion

            System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
            System.Version ver = asm.GetName().Version;

            //version = "Version " + verToString(ver);
            version = "Version " + ver;

            AssemblyCopyrightAttribute[] copyrightAttributes = (AssemblyCopyrightAttribute[])asm.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            copyright = copyrightAttributes[0].Copyright;

#if (Release|| Release_log)
            tiDebug.Visibility = System.Windows.Visibility.Collapsed;
            tiMeasurement.Visibility = System.Windows.Visibility.Collapsed;
            tiModuleGamma.Visibility = System.Windows.Visibility.Collapsed;
            //tiCalibration.Visibility = System.Windows.Visibility.Collapsed;
            btnReconfig.Visibility = System.Windows.Visibility.Collapsed;
            //btnPanelSerial.Visibility = System.Windows.Visibility.Collapsed;
            btnPanelLog.Visibility = System.Windows.Visibility.Collapsed;
            gdUfManualDebug.Visibility = System.Windows.Visibility.Collapsed;
            //tiConfiguration.Visibility = System.Windows.Visibility.Collapsed;
            gdPassword.Visibility = System.Windows.Visibility.Collapsed;
            gdChrom.Visibility = System.Windows.Visibility.Collapsed;
            rbChromCustom.Visibility = System.Windows.Visibility.Collapsed;
            //cbExtendedDisable.Visibility = System.Windows.Visibility.Collapsed;
            gdPhotoMode.Visibility = System.Windows.Visibility.Collapsed;
            //gdHdcp.Visibility = System.Windows.Visibility.Collapsed;
            gdLowLevelCutoff.Visibility = System.Windows.Visibility.Collapsed;
            //btnHdcpOnOff.Visibility = System.Windows.Visibility.Collapsed;
            gdMeasAlert.Visibility = System.Windows.Visibility.Collapsed;
            //gdUfCamMeasure.Visibility = System.Windows.Visibility.Collapsed;
            //btnMeasChiron.Visibility = System.Windows.Visibility.Collapsed;
            //tiUfCamera.Visibility = Visibility.Collapsed;
            //gdUfCamDebug.Visibility = Visibility.Collapsed;
            rbUfCamDefault.Visibility = Visibility.Collapsed;
            //rbUfCam9pt.Visibility = Visibility.Collapsed;
            gdMeasPoint.Visibility = Visibility.Collapsed;
            gdUfCamMeasSummary.Visibility = Visibility.Collapsed;
            //btnUfCamMeasTargetStart.Visibility = Visibility.Collapsed;
            //gdUfCamCorrection.Visibility = Visibility.Collapsed;
            cbUfCamMeasTgtOnly.Visibility = Visibility.Collapsed;
            tiGapCorrection.Visibility = Visibility.Collapsed;
            //cbUfCamHViewPt.Visibility = Visibility.Collapsed;
            tiSigMode.Visibility = Visibility.Collapsed;
#endif

#if DEBUG
            //this.Title = "CAS  Ver. " + verToString(ver);
            this.Title = "CAS  Ver. " + ver;

            this.Title += "  [DEBUG]";
            appliMode = ApplicationMode.Developer;

            txbSite.Text = "DEBUG";
            //pwbConfig.Password = customPassword;
            btnUfCamMeasStart.IsEnabled = true;
            btnUfCamAdjustStart.IsEnabled = true;
#endif

#if NO_MEASURE
            this.Title += "  [NO_MEASURE]";
#endif

#if NO_WRITE
            this.Title += "  [NO_WRITE]";
#endif

            // Service Mode            
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            { appliMode = ApplicationMode.Service; }

            if (appliMode == ApplicationMode.Service)
            {
                this.Title += "  [Service Mode]";//"  [Engineer Mode]";
                btnReconfig.Visibility = System.Windows.Visibility.Visible;
                //btnPanelSerial.Visibility = System.Windows.Visibility.Visible;
                btnPanelLog.Visibility = System.Windows.Visibility.Visible;
                tiMeasurement.Visibility = System.Windows.Visibility.Visible;
                tiConfiguration.Visibility = System.Windows.Visibility.Visible;
                gdPassword.Visibility = System.Windows.Visibility.Visible;
                gdChrom.Visibility = System.Windows.Visibility.Visible;
                rbChromCustom.Visibility = System.Windows.Visibility.Visible;
                //tiCalibration.Visibility = System.Windows.Visibility.Visible;
                //cbExtendedDisable.Visibility = System.Windows.Visibility.Visible;
                //gdPhotoMode.Visibility = System.Windows.Visibility.Visible;
                //gdHdcp.Visibility = System.Windows.Visibility.Visible;
                //gdLowLevelCutoff.Visibility = System.Windows.Visibility.Visible;
                gdMeasAlert.Visibility = System.Windows.Visibility.Visible;
                rbUfCam9pt.Visibility = Visibility.Visible;
                gdUfCamMeasSummary.Visibility = Visibility.Visible;
                //gdUfCamCorrection.Visibility = Visibility.Visible;
                //cbUfCamHViewPt.Visibility = Visibility.Visible;
                tiSigMode.Visibility = Visibility.Visible;
            }

            // Normal Mode
            if (appliMode == ApplicationMode.Normal)
            {
                rbChromZRDB12A.IsEnabled = false;
                rbChromZRDB15A.IsEnabled = false;
                rbChromZRDC12A.IsEnabled = false;
                rbChromZRDC15A.IsEnabled = false;
                rbChromZRDBH12D.IsEnabled = false;
                rbChromZRDBH15D.IsEnabled = false;
                rbChromZRDCH12D.IsEnabled = false;
                rbChromZRDCH15D.IsEnabled = false;
                rbChromZRDBH12D_S3.IsEnabled = false;
                rbChromZRDBH15D_S3.IsEnabled = false;
                rbChromZRDCH12D_S3.IsEnabled = false;
                rbChromZRDCH15D_S3.IsEnabled = false;
                rbChromCustom.IsEnabled = false;
            }

            // Applicationのパスを取得する
            applicationPath = System.IO.Path.GetDirectoryName(asm.Location);
            tempPath = applicationPath + TempDir;

            // 設定値Load
            if (Settings.LoadFromXmlFile() == false)
            {
                loadSuccess = false;

                string msg = "Failed to read the CasSetting.xml file.\r\nPlease check the contents of the file.\r\r\n" + Settings.ErrorMessage;
                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 420, 210);

                App.Current.Shutdown();
            }

#if (DEBUG || Release_log)
            Settings.Ins.ExecLog = true;
            Settings.Ins.SdcpLog = true;
#endif

            if (Settings.Ins.ExecLog == true)
            {
                MainWindow.SaveExecLog("");
                MainWindow.SaveExecLog("[[[ Start-up CAS Ver." + ver + " ]]]");
                MainWindow.SaveExecLog("(Setting) SDCP Wait Time : " + Settings.Ins.SdcpWaitTime.ToString() + "[msec]");
            }
            ManageLogGen(applicationPath + LogDir, logKey.ExecLog);
            ManageLogGen(applicationPath + LogDir, logKey.SDCPLog);

            // EUAL同意確認
            if (Settings.Ins.eulaAgree != true)
            {
                Settings.Ins.Eulafile = applicationPath + "\\Eula.rtf";
                if (File.Exists(Settings.Ins.Eulafile))
                {
                    WindowEULA winEula = new WindowEULA();
                    if (!winEula.ShowDialog() == true)
                    { this.Close(); }
                }
                else
                {
                    string msg = "The Eula.rtf file does not exist.\r\nStop starting the application.";
                    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);

                    this.Close();
                }
            }

            // ディスプレイの大きさを取得
            int dispW = 0, dispH = 0;
            foreach (System.Windows.Forms.Screen s in System.Windows.Forms.Screen.AllScreens)
            {
                if(s.Bounds.Right > dispW)
                { dispW = s.Bounds.Right; }

                if(s.Bounds.Bottom > dispH)
                { dispH = s.Bounds.Bottom; }
            }

            if (Settings.Ins.Window_X_Position > dispW)
            { this.Left = 0; }
            else
            { this.Left = Settings.Ins.Window_X_Position; }

            if (Settings.Ins.Window_Y_Position > dispH)
            { this.Top = 0; }
            else
            { this.Top = Settings.Ins.Window_Y_Position; }

            this.Height = Settings.Ins.Window_Height;
            this.Width = Settings.Ins.Window_Width;
                        
            UnitDefaultBrush = new ToggleButton().Background;
            tcMain.IsEnabled = false;

            rbCursorX.Checked += rbCursor_Checked;
            rbCursorY.Checked += rbCursor_Checked;

            cmbxCA410ChannelRelTgt.SelectionChanged -= cmbxCA410ChannelRelTgt_SelectionChanged; // ※cmbxCA410ChannelRelTgtだけなぜかi=10のItems.AddのときにSelectionChangedのイベントが発生する。.NETのバグ？

            // Channel
            for (Int32 i = 0; i < 100; i++)
            {
                cmbxCA410Channel.Items.Add("CH" + i.ToString("D2"));
                cmbxCA410ChannelMeas.Items.Add("CH" + i.ToString("D2"));

                if (i > 0) // CalibではCh00は選択しない
                { cmbxCA410ChannelCalib.Items.Add("CH" + i.ToString("D2")); }

                cmbxCA410ChannelRelTgt.Items.Add("CH" + i.ToString("D2"));
                cmbxCA410ChannelGain.Items.Add("CH" + i.ToString("D2"));
                cmbxChannel.Items.Add("CH" + i.ToString("D2")); //Debug
            }

            cmbxCA410ChannelRelTgt.SelectionChanged += cmbxCA410ChannelRelTgt_SelectionChanged;

            caSdk = new CaSdk();

            // CA-410(CA-SDK2)のインストールの有無を調べる
            if (File.Exists(Settings.Ins.ComToolPath) != true)
            {
                rbCA410RemoteOn.IsEnabled = false;
                rbCA410RemoteOnMeas.IsEnabled = false;
                rbCA410RemoteOnCalib.IsEnabled = false;
                rbCA410RemoteOnRelTgt.IsEnabled = false;
                rbCA410RemoteOnGain.IsEnabled = false;
            }

            // Brightness設定
            //if (appliMode == ApplicationMode.Service)
            { brightness = new Brightness(Settings.Ins.Gamma); }
            //else
            //{ brightness = new Brightness(Gamma.Gamma22); }

            // カメラ名称の設定
            for (int i = 0; i < CamNames.Length; i++)
            {
                cmbxUfCamCamera.Items.Add(CamNames[i]);
                cmbxGapCamCamera.Items.Add(CamNames[i]);

                if (Settings.Ins.Camera.Name == CamNames[i])
                { cmbxUfCamCamera.SelectedIndex = i; }
            }

            // UfCameraのLiveView用Timerの設定
            timerUfCam = new System.Windows.Forms.Timer();
            this.timerUfCam.Interval = 50;
            this.timerUfCam.Tick += new System.EventHandler(this.timerUfCam_Tick);

            // Gap(Cam)の位置合せ用Timerの設定
            timerGapCam = new System.Windows.Forms.Timer();
            this.timerGapCam.Interval = 50;
            this.timerGapCam.Tick += new System.EventHandler(this.timerGapCam_Tick);

            // カメラ制御用ファイルのパスを設定（IPC通信がダメだったのでやむなくファイルにする）
            CamContFile = applicationPath + TempDir + CameraControlFile;

            // Profile Load
            profilePath = Settings.Ins.LastProfile;//applicationPath + "\\Profile\\" + Settings.Ins.LastProfile;

            // 最初にControllerの構成をLoadする
            // Fileが存在しない、もしくはLoadに失敗した場合はファイルを選択するか手動で入力する
            if (System.IO.File.Exists(profilePath) != true || loadProfile(profilePath) != true)
            {
                while (true)
                {
                    WindowProfile winProfile = new WindowProfile();
                    winProfile.dele_sendAdcpCommand = ADCPClass.sendAdcpCommand;
                    winProfile.dele_sendSdcpCommand = sendSdcpCommand;
                    if (winProfile.ShowDialog() == true)
                    {
                        if (winProfile.Mode == ProfileMode.File)
                        {
                            if (openProfile(winProfile.ProfileFilePath) == true)
                            { break; }
                        }
                        else
                        {
                            dicController = winProfile.dicController;
                            allocInfo = winProfile.allocInfo;
                            
                            showConfiguration();

                            profileLoaded = false;
                            break;
                        }
                    }
                    else
                    {
                        this.Close();
                        return;
                    }             
                }
            }
            else
            { shouldReferCasSetting = true; } // 通常起動時

            // Alertの設定
            txbMeasThreshXy.Text = Settings.Ins.xyThresh.ToString("0.000");
            txbMeasThreshY.Text = Settings.Ins.YThresh.ToString();
            
            if (Settings.Ins.MeasAlert == true)
            {
                rbMeasAlertOn.IsChecked = true;
                txbMeasThreshXy.IsEnabled = true;
                txbMeasThreshY.IsEnabled = true;
            }
            else
            {
                rbMeasAlertOff.IsChecked = true;
                txbMeasThreshXy.IsEnabled = false;
                txbMeasThreshY.IsEnabled = false;
            }

            // Measurementの日付を入れる
            txbDate.Text = DateTime.Now.ToString("yyyy/MM/dd");

            // Configuration Wall形状ファイル
            if(Settings.Ins.Form == WallForm.Curve)
            { rbConfigWallFormCurve.IsChecked = true; }
            else
            { rbConfigWallFormFlat.IsChecked = true; }

            txbConfigWallForm.Text = Settings.Ins.WallFormFile;
                        
            txbConfigDist.Text = Settings.Ins.CameraDist.ToString();

            if(Settings.Ins.CameraInstPos == CameraInstallPos.Custom)
            { rbConfigCamPosCustom.IsChecked = true; }
            else
            { rbConfigCamPosDefault.IsChecked = true; }

            txbConfigWallHeight.Text = Settings.Ins.WallBottomH.ToString();
            txbConfigCamHeight.Text = Settings.Ins.CameraH.ToString();

            //if (Settings.Ins.PanelType == PanelType.Normal)
            //{ rbPanelTypeNormal.IsChecked = true; }
            //else if (Settings.Ins.PanelType == PanelType.DigitalCinema2017)
            //{ rbPanelTypeDigitalCinema.IsChecked = true; }
            //else if (Settings.Ins.PanelType == PanelType.DigitalCinema2020)
            //{ rbPanelTypeDigitalCinema2020.IsChecked = true; }

            //if (Settings.Ins.Gamma == Gamma.Gamma22)
            //{ rbGamma22.IsChecked = true; }
            //else if (Settings.Ins.Gamma == Gamma.Gamma26)
            //{ rbGamma26.IsChecked = true; }
            //else if (Settings.Ins.Gamma == Gamma.PQ)
            //{ rbGammaPQ.IsChecked = true; }

            // Lumi/Chromのモード表示
            //if (Settings.Ins.ConfigChromType == ConfigChrom.DigitalCinema2017)
            //{
            //    lbChromMode.Content = "Digital Cinema 2017";
            //    lbChromMode.Visibility = System.Windows.Visibility.Visible;
            //}
            //else if (Settings.Ins.ConfigChromType == ConfigChrom.DigitalCinema2020)
            //{
            //    lbChromMode.Content = "Digital Cinema 2020";
            //    lbChromMode.Visibility = System.Windows.Visibility.Visible;
            //}
            //else if (Settings.Ins.ConfigChromType == ConfigChrom.Custom)
            //{
            //    lbChromMode.Content = "Custom";
            //    lbChromMode.Visibility = System.Windows.Visibility.Visible;
            //}
            //else
            //{ lbChromMode.Visibility = System.Windows.Visibility.Collapsed; }

            // Panel Typeの表示
            //if (Settings.Ins.PanelType == PanelType.DigitalCinema2017)
            //{
            //    lbPanelType.Content = "Digital Cinema 2017";
            //    lbPanelType.Visibility = System.Windows.Visibility.Visible;
            //}
            //else if (Settings.Ins.PanelType == PanelType.DigitalCinema2020)
            //{
            //    lbPanelType.Content = "Digital Cinema 2020";
            //    lbPanelType.Visibility = System.Windows.Visibility.Visible;
            //}
            //else
            //{ lbPanelType.Visibility = System.Windows.Visibility.Collapsed; }

            // Gammaの表示
            //if(Settings.Ins.Gamma == Gamma.Gamma26)
            //{
            //    lbGamma.Content = "2.6";
            //    lbGamma.Visibility = System.Windows.Visibility.Visible;
            //}
            //else if (Settings.Ins.Gamma == Gamma.PQ)
            //{
            //    lbGamma.Content = "PQ";
            //    lbGamma.Visibility = System.Windows.Visibility.Visible;
            //}
            //else
            //{ lbGamma.Visibility = System.Windows.Visibility.Collapsed; }

            // ファイル転送方式
            if (Settings.Ins.TransType == TransType.NFS)
            { rbNfs.IsChecked = true; }
            else
            { rbFtp.IsChecked = true; }

            // 起動時に音を鳴らさない、例外が発生しないようにあとからイベントを追加する
            rbRemoteOff.Checked += rbRemoteOff_Checked;
            rbRemoteOffMeas.Checked += rbRemoteOff_Checked;
            rbRemoteOffCalib.Checked += rbRemoteOff_Checked;
            rbRemoteOffRelTgt.Checked += rbRemoteOff_Checked;
            rbCursorUnitUfManual.Checked += rbCursorUnitUfManual_Checked;
            rbCursorUnitGapCell.Checked += rbCursorUnitGapCell_Checked;
            //rbCalibDefault.Checked += rbCalibDefault_Checked;

            // Profileファイルのロードが完了していれば操作を有効にする
            //if(profileLoaded == true) // 無限ループでAlloc情報を持たずに開くことはない
            { tcMain.IsEnabled = true; }

            //GamePad対応処理
            //ViewModel バインド設定
            gamePadViewModel = new GamePadViewModel();
            DataContext = gamePadViewModel;
            try
            {
                //GamePadCmdFunc生成
                gamePadCmdFunc = new GamePadCmdFunc(gamePadViewModel, this);
                //GamePadDevice生成
                gamePadDevice = new GamePadDevice(gamePadCmdFunc.getGamePadProfilePath(), gamePadViewModel);
                //GmamePadコマンド初期設定
                gamePadCmdFunc.commandInit(gamePadDevice.GamePadProfileData);

                //デバイス検索と接続は対象のタブ選択時のみ接続を行うことになったので開始時では実施しないことにした
                //tcMain_SelectionChangedに移動
                //デバイス検索と接続
                //Task task = gamePadDevice.connectHIDInput();
                //デバイス監視開始
                //gamePadDevice.startHidDeviceTimer();

                //GamePad操作によるコマンド実行開始
                startGamePadCmdTimer();

            }
            catch //(Exception ex)
            {
                //現状ではプロファイルの読み込み失敗時のメッセージ表示はしない
                //ShowMessageWindow(ex.Message, "Warning", System.Drawing.SystemIcons.Warning, 500, 210);
            }
        }
        
        private void Window_Load(object sender, RoutedEventArgs e)
        {
            // LED Modelに応じて設定
            if (shouldReferCasSetting)
            {
                if ((appliMode == ApplicationMode.Normal && validConfigChromType(Settings.Ins.ConfigChromType) != true) ||
                    (appliMode == ApplicationMode.Developer && validConfigChromType(Settings.Ins.ConfigChromType) != true && Settings.Ins.ConfigChromType != ConfigChrom.Custom) ||
                    (appliMode == ApplicationMode.Service && validConfigChromType(Settings.Ins.ConfigChromType) != true && Settings.Ins.ConfigChromType != ConfigChrom.Custom))
                { initLedModel(); }
                else
                {
                    loadConfigurationOfLedModel(allocInfo.LEDModel);

                    ConfigChrom configChrom = Settings.Ins.ConfigChromType;
                    switch (configChrom)
                    {
                        case ConfigChrom.ZRD_B12A:
                            rbChromZRDB12A.IsChecked = true;
                            break;
                        case ConfigChrom.ZRD_B15A:
                            rbChromZRDB15A.IsChecked = true;
                            break;
                        case ConfigChrom.ZRD_C12A:
                            rbChromZRDC12A.IsChecked = true;
                            break;
                        case ConfigChrom.ZRD_C15A:
                            rbChromZRDC15A.IsChecked = true;
                            break;
                        case ConfigChrom.ZRD_BH12D:
                            rbChromZRDBH12D.IsChecked = true;
                            break;
                        case ConfigChrom.ZRD_BH15D:
                            rbChromZRDBH15D.IsChecked = true;
                            break;
                        case ConfigChrom.ZRD_CH12D:
                            rbChromZRDCH12D.IsChecked = true;
                            break;
                        case ConfigChrom.ZRD_CH15D:
                            rbChromZRDCH15D.IsChecked = true;
                            break;
                        case ConfigChrom.ZRD_BH12D_S3:
                            rbChromZRDBH12D_S3.IsChecked = true;
                            break;
                        case ConfigChrom.ZRD_BH15D_S3:
                            rbChromZRDBH15D_S3.IsChecked = true;
                            break;
                        case ConfigChrom.ZRD_CH12D_S3:
                            rbChromZRDCH12D_S3.IsChecked = true;
                            break;
                        case ConfigChrom.ZRD_CH15D_S3:
                            rbChromZRDCH15D_S3.IsChecked = true;
                            break;
                        case ConfigChrom.Custom:
                            rbChromCustom.IsChecked = true;
                            break;
                        default:
                            rbChromZRDB12A.IsChecked = true;
                            break;
                    }
                }
            }
            else
            { initLedModel(); }

            // Uniformity画面
            initUniformityAdjustmentMode();

            // UniformityCamera画面
            initUniformityCameraAdjustmentMode();

            // RelativeTarget画面
            initRelativeTarget();

            // ModuleGamma画面
            if (appliMode == ApplicationMode.Service && Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
            { initGammaGain(); }

            // SIGMODE画面
            if (appliMode == ApplicationMode.Service)
            {
                initSmCheckBoxVisibility();
                initSigMode();
            }

            // added by Hotta 2022/01/31
            imgGapCamBefore.Focusable = true;
            imgGapCamResult.Focusable = true;

            // added by Hotta 2022/02/25
            if (appliMode == ApplicationMode.Normal)
            {
                labelGapCamMaxBefore.Visibility = Visibility.Hidden;
                labelGapCamMinBefore.Visibility = Visibility.Hidden;
                labelGapCamPPBefore.Visibility = Visibility.Hidden;
                labelGapCam3sigmaBefore.Visibility = Visibility.Hidden;
                labelGapCamAveBefore.Visibility = Visibility.Hidden;

                txbGapCamMaxBefore.Visibility = Visibility.Hidden;
                txbGapCamMinBefore.Visibility = Visibility.Hidden;
                txbGapCamPPBefore.Visibility = Visibility.Hidden;
                txbGapCam3sigmaBefore.Visibility = Visibility.Hidden;
                txbGapCamAveBefore.Visibility = Visibility.Hidden;

                labelGapCamMaxResult.Visibility = Visibility.Hidden;
                labelGapCamMinResult.Visibility = Visibility.Hidden;
                labelGapCamPPResult.Visibility = Visibility.Hidden;
                labelGapCam3sigmaResult.Visibility = Visibility.Hidden;
                labelGapCamAveResult.Visibility = Visibility.Hidden;

                txbGapCamMaxResult.Visibility = Visibility.Hidden;
                txbGapCamMinResult.Visibility = Visibility.Hidden;
                txbGapCamPPResult.Visibility = Visibility.Hidden;
                txbGapCam3sigmaResult.Visibility = Visibility.Hidden;
                txbGapCamAveResult.Visibility = Visibility.Hidden;

                labelGapCamNGlistResult.Visibility = Visibility.Hidden;
                txbGapCamNGlistResult.Visibility = Visibility.Hidden;

                labelGapCamMaxMeasure.Visibility = Visibility.Hidden;
                labelGapCamMinMeasure.Visibility = Visibility.Hidden;
                labelGapCamPPMeasure.Visibility = Visibility.Hidden;
                labelGapCam3sigmaMeasure.Visibility = Visibility.Hidden;
                labelGapCamAveMeasure.Visibility = Visibility.Hidden;

                txbGapCamMaxMeasure.Visibility = Visibility.Hidden;
                txbGapCamMinMeasure.Visibility = Visibility.Hidden;
                txbGapCamPPMeasure.Visibility = Visibility.Hidden;
                txbGapCam3sigmaMeasure.Visibility = Visibility.Hidden;
                txbGapCamAveMeasure.Visibility = Visibility.Hidden;

                labelGapCamNGlistMeasure.Visibility = Visibility.Hidden;
                txbGapCamNGlistMeasure.Visibility = Visibility.Hidden;
            }

            // 起動時パスワード入力欄有効/無効
            if ((appliMode == ApplicationMode.Service || appliMode == ApplicationMode.Developer) && Settings.Ins.ConfigChromType == ConfigChrom.Custom)
            { pwbConfig.IsEnabled = true; }

            // added by Hotta 2022/02/28
            comboBoxGapCameraNumOfAdjustment.Items.Add("1");
            comboBoxGapCameraNumOfAdjustment.Items.Add("2");
            comboBoxGapCameraNumOfAdjustment.Items.Add("3");
            comboBoxGapCameraNumOfAdjustment.Items.Add("4");
            comboBoxGapCameraNumOfAdjustment.Items.Add("5");
            comboBoxGapCameraNumOfAdjustment.SelectedIndex = 0;

            string msg = "Disconnect or terminate DCS connected to the target Display Controller when adjusting with CAS.";
            ShowMessageWindow(msg, "Information", System.Drawing.SystemIcons.Information, 380, 210);
        }

        //--------------------------------------------------------------------------------------
        // GamePad用 
        // MainWindow以外でGUIボタン押下イベント発生させたい
        // しかしHIDデバイスのイベントスレッドからMainWindowスレッドにアクセスでないので
        // 仕方なくタイマーでHIDデバイスのボタン押下状態を取得する実装にした
        //--------------------------------------------------------------------------------------
        // タイマーのインスタンス
        private DispatcherTimer gamePadCmdTimer = null;
        // 間隔(ms)
        private const int gamePadPollingTime = 10;
 
        /// <summary>
        /// タイマー 
        /// </summary>
        private void startGamePadCmdTimer()
        {
            // タイマーのインスタンスを生成
            gamePadCmdTimer = new DispatcherTimer(); // 優先度はDispatcherPriority.Background
            // インターバルを設定
            gamePadCmdTimer.Interval = TimeSpan.FromMilliseconds(gamePadPollingTime);
            // タイマメソッドを設定
            gamePadCmdTimer.Tick += new EventHandler(gamePadCmd);
            // タイマを開始
            gamePadCmdTimer.Start();
        }

        /// <summary>
        /// インターバル経過時Call
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void gamePadCmd(object sender, EventArgs e)
        {
            gamePadCmdFunc.execCommand(gamePadDevice.GamePadKeyId);
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        static extern bool SetDefaultDllDirectories(uint directoryFlags);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern IntPtr SetDllDirectory(string lpFileName);

        public static void SaveErrorLog(string msg)
        {
            string logFile = applicationPath + "\\Log\\";

            if (Directory.Exists(logFile) != true)
            { Directory.CreateDirectory(logFile); }

            string date = DateTime.Now.ToString("_yyyyMMdd");

            logFile += "ErrorLog" + date + ".txt";

            try
            {
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(logFile, true))
                { sw.WriteLine(DateTime.Now.ToString() + " : " + msg); }
            }
            catch { }
        }

        /// <summary>
        /// 関数内If文条件分岐確認用Log(LED-8753対応)
        /// </summary>
        /// <param name="msg">ExecLogに記録するログ</param>
        /// <param name="lineNumber">呼出元行数</param>
        /// <param name="memberName">呼出元関数名</param>
        public static void SaveExecFuncLog(string msg, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "")
        {
            string funcMsg = msg + ", lineNumber = " + lineNumber.ToString() + ", memberName = " + memberName;
            SaveExecLog(funcMsg);
        }

        public static void SaveExecLog(string msg)
        {
            string logFile = applicationPath + "\\Log\\";

            if (Directory.Exists(logFile) != true)
            { Directory.CreateDirectory(logFile); }

            string date = DateTime.Now.ToString("_yyyyMMdd");

            logFile += "ExecLog" + date + ".txt";

            try
            {
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(logFile, true))
                { sw.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + " : " + msg); }
            }
            catch { }
        }

        /// <summary>
        /// 各ログの最大世代以上削除処理
        /// </summary>
        /// <param name="dir">世代管理するログの配置フルパス</param>
        /// <param name="key">対象となるログ種類</param>
        private void ManageLogGen(string dir, logKey key)
        {
            // yyyymmdd形式の日付を含むファイル名の正規表現パターン
            string pattern = @"^(.*)_(\d{8})\.txt$";

            // フォルダ内のファイルを取得し、パターンに合致するものだけ抽出
            var matchedFiles = Directory.GetFiles(dir)
                .Select(filePath => new
                {
                    FullPath = filePath,
                    FileName = Path.GetFileName(filePath),
                    Match = Regex.Match(Path.GetFileName(filePath), pattern)
                })
                .Where(x => x.Match.Success && x.FileName.StartsWith(key.ToString()))
                .Select(x => new
                {
                    x.FullPath,
                    x.FileName,
                    DateString = x.Match.Groups[2].Value,
                    // yyyymmddをDateTimeに変換。変換できない場合はMinValue
                    Date = DateTime.TryParseExact(x.Match.Groups[2].Value, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime dt) ? dt : DateTime.MinValue
                })
                // 変換失敗を除外
                .Where(x => x.Date != DateTime.MinValue)
                .OrderBy(x => x.Date)  // 古い順にソート
                .ToList();

            // 世代上限以上であれば削除
            while (matchedFiles.Count > logMaxGen)
            {
                var fileToDelete = matchedFiles[0];

                try
                {
                    File.Delete(fileToDelete.FullPath);
                    if (Settings.Ins.ExecLog == true)
                    {
                        MainWindow.SaveExecLog("delete : " + fileToDelete.FileName);
                    }
                }
                catch (Exception ex)
                {
                    if (Settings.Ins.ExecLog == true)
                    {
                        MainWindow.SaveExecLog("failed to delete : " + fileToDelete.FileName + " error : " + ex.Message);
                    }
                }

                matchedFiles.RemoveAt(0);  // リストからも削除
            }
        }

        #region Events

        public UIContents UIContents
        {
            get
            {
                return UIContents.Instance;
            }
        }
        
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            this.Activate();
            this.Topmost = false;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(!IsLoaded) // MainWindowの表示状況確認
            { return; }

            // 進捗ダイアログ表示中はMainWindowの閉じるボタンイベントをキャンセルにする
            if (winProgress != null && winProgress.IsVisible)
            {
                e.Cancel = true;
                return;
            }

            // CA-410 Close
            if (caSdk != null)
            {
                if (caSdk.isOpened() == true)
                { setRemoteModeSub(CaSdk.RemoteMode.RemoteOff); }
            }

            // 構成情報Save
            if (profileLoaded != true)
            {
                string msg = "Configuration information is not saved.\r\n\r\nDo you need to save it?";
                //MessageBoxResult result = MessageBox.Show(msg, "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);

                bool? result = false;
                showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 500, 210, "Yes");

                if (result == true)
                { saveConfiguration(); }
            }

            // 設定値Save
            if (settingSaveFlag == true && loadSuccess == true)
            {
                Settings.Ins.Window_X_Position = (int)this.Left;
                Settings.Ins.Window_Y_Position = (int)this.Top;
                Settings.Ins.Window_Height = (int)this.Height;
                Settings.Ins.Window_Width = (int)this.Width;

                Settings.SaveToXmlFile();
            }

            // タイマー停止
            if (gamePadCmdTimer != null)
            {
                    gamePadCmdTimer.Stop();
            }
            // HID Device Dispose
            if(gamePadDevice != null)
            {
                gamePadDevice.dispose();
            }

            //// Camera切断
            //if (ccLiveView != null && ccLiveView.IsCameraOpened == true)
            //{
            //    timerLiveView.Enabled = false;
            //    System.Threading.Thread.Sleep(100);

            //    try { ccLiveView.CloseCamera(); }
            //    catch(Exception ex)
            //    {
            //        WindowMessage winMessage = new WindowMessage(ex.Message, "Camera Color Analyzer Error!!");
            //        winMessage.ShowDialog();
            //    }
            //}

            // Camera制御ProcessのKill
            KillCcProcess();
        }

        private async void miNew_Click(object sender, RoutedEventArgs e)
        {
            bool status;
            string res;

            // ◆Controller情報
            List<ControllerInfo> lstControllerInfo = new List<ControllerInfo>();
            WindowControllerCount winContCount = new WindowControllerCount();

            bool? result = winContCount.ShowDialog();
            if (result == false)
            { return; }

            int count = winContCount.cmbxControllerCount.SelectedIndex + 1;
            for (int i = 0; i < count; i++)
            {
                ControllerInfo info = new ControllerInfo();

                while (true)
                {
                    // IPAdress
                    WindowControllerIP winIP = new WindowControllerIP();
                    winIP.txbIP.Focus();
                    if (winIP.ShowDialog() == false)
                    { return; }

                    // TODO: IPアドレスのフォーマットチェックも良いかも
                    info.IPAddress = winIP.txbIP.Text;

                    // Password
                    WindowInputPassword winPw = new WindowInputPassword();
                    winPw.txbPw.Focus();
                    if (winPw.ShowDialog() == false)
                    { return; }

                    // Password check
                    if (ADCPClass.sendAdcpCommand(ADCPClass.CmdControllerPowerStausGet, out res, winIP.txbIP.Text, winPw.txbPw.Password))
                    {
                        info.Password = winPw.txbPw.Password;
                        break;
                    }
                }

                // IPAdress重複チェック
                foreach (ControllerInfo controller in lstControllerInfo)
                {
                    if (controller.IPAddress == info.IPAddress)
                    {
                        string msg = "Failed to get Layout Info.\r\nBecause the IP address is duplicated.";
                        ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 380, 210);
                        return;
                    }
                }

                lstControllerInfo.Add(info);
            }

            winProgress = new WindowProgress("Get Allocation Info.");
            winProgress.Show();

            try
            {
                status = await Task.Run(() => getAllocationInfo(lstControllerInfo));
                if (status == true)
                {
                    showConfiguration();
                    profileLoaded = false;

                    initLedModel();

                    // ModuleGamma画面
                    if (appliMode == ApplicationMode.Service && Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
                    { initGammaGain(); }
                    else
                    { tiModuleGamma.Visibility = Visibility.Collapsed; }
                }
            }
            catch (Exception ex)
            {
                string msg = "Can not get the Allocation Info.\r\n" + ex.Message;
                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return;
            }

            winProgress.Close();
        }

        private void miOpen_Click(object sender, RoutedEventArgs e)
        {
            //bool status;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.FilterIndex = 1;
            openFileDialog.Filter = "Profile File(.xml)|*.xml|All Files (*.*)|*.*";
            
            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                if (openProfile(openFileDialog.FileName))
                {
                    initLedModel();

                    // ModuleGamma画面
                    if (appliMode == ApplicationMode.Service && Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3)
                    { initGammaGain(); }
                    else
                    { tiModuleGamma.Visibility = Visibility.Collapsed; }
                }

                //winProgress = new WindowProgress("Trun On Power.");
                //winProgress.Show();

                //try { status = await Task.Run(() => turnOnHDCP()); }
                //catch (Exception ex)
                //{
                //    string msg = "Can not be set to the power on state.\r\n" + ex.Message;
                //    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                //    return;
                //}

                //winProgress.Close();
            }
        }
        
        private void miSave_Click(object sender, RoutedEventArgs e)
        {
            saveConfiguration();
        }

        private void miAbout_Click(object sender, RoutedEventArgs e)
        {
            SingleWindowHelper.Show<WindowVersion>(() => new WindowVersion(version, copyright), this);
        }

        private async void tcMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var dispatcher = Application.Current.Dispatcher;

            if (tcMain.SelectedIndex >= 0)
            {
                if (gamePadDevice != null)
                {
                    // ゲームパッドを使用している場合
                    if (tcMain.SelectedIndex == TabIndexUniformityManual ||
                        tcMain.SelectedIndex == TabIndexGapCorrectionModule)
                    {
                        //デバイス検索と接続
                        Task task = gamePadDevice.connectHIDInput();
                        //デバイス監視開始
                        gamePadDevice.startHidDeviceTimer();
                    }
                    else
                    {
                        //デバイス切断と監視停止
                        gamePadDevice.dispose();
                    }
                }

                //if (tcMain.SelectedIndex == 0)
                //{
                //    if (Settings.Ins.ExecLog == true)
                //    { MainWindow.SaveExecLog("Move to Data Tab"); }
                //}
                //else if (tcMain.SelectedIndex == 1)
                //{
                //    if (Settings.Ins.ExecLog == true)
                //    { MainWindow.SaveExecLog("Move to Uniformity Tab"); }
                //} else
                // Uniformity
                if (((TabItem)tcMain.Items[tcMain.SelectedIndex]).Header.ToString() == "Uniformity" && uniformityTabFirstSelect == true)
                {
                    uniformityTabFirstSelect = false;
                    getUniformityTargetMode();
                }
                // Gap Correct
                else if (((TabItem)tcMain.Items[tcMain.SelectedIndex]).Header.ToString() == "Gap Correction" && gapCorrectTabFirstSelect == true)
                {
                    gapCorrectTabFirstSelect = false;
                    getGapCorrectionMode();
                }
                // Gap Correct(Module)
                else if (((TabItem)tcMain.Items[tcMain.SelectedIndex]).Header.ToString() == "Gap Correction(Module)" && gapCorrectModuleTabFirstSelect == true)
                {
                    gapCorrectModuleTabFirstSelect = false;
                    getGapCorrectionModuleMode();
                }
                // Control
                else if (((TabItem)tcMain.Items[tcMain.SelectedIndex]).Header.ToString() == "Control" && controlTabFirstSelect == true)
                {
                    controlTabFirstSelect = false;
                    //getControllerSetting();

                    try { await Task.Run(() => getControllerHdcpStatus()); }
                    catch (Exception ex)
                    { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

                    try { await Task.Run(() => getControllerSetting()); }
                    catch (Exception ex)
                    { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

                    cmbxPattern.SelectedIndex = 0;
                }
                // Information
                else if (((TabItem)tcMain.Items[tcMain.SelectedIndex]).Header.ToString() == "Information" && infoTabFirstSelect == true)
                {
                    infoTabFirstSelect = false;

                    //getControllerStatus();
                    try { await Task.Run(() => getControllerStatus()); }
                    catch (Exception ex)
                    { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

                    //getUnitStatus();
                    try { await Task.Run(() => getUnitStatus()); }
                    catch (Exception ex)
                    { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }

                    //checkUnitData();
                    try { await Task.Run(() => checkUnitData()); }
                    catch (Exception ex)
                    { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }
                }
                // Measurement
                //            else if (tcMain.SelectedIndex == 7 && measTabFirstSelect == true)
                //            {
                //                measTabFirstSelect = false;
                //#if NO_SET
                //#else
                //                try { getColorTemp(); }
                //                catch (Exception ex)
                //                { ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210); }
                //#endif
                //            }
                // Calibration
                else if (((TabItem)tcMain.Items[tcMain.SelectedIndex]).Header.ToString() == "Calibration")
                { showDefaultChromaticity(); }
                // Configuration
                else if (((TabItem)tcMain.Items[tcMain.SelectedIndex]).Header.ToString() == "Configuration" && configTabFirstSelect == true)
                {
                    configTabFirstSelect = false;

                    showTargetModelChromaticity();
                }
                // Relative Target
                else if (((TabItem)tcMain.Items[tcMain.SelectedIndex]).Header.ToString() == "Relative Target")
                { showTargetChromaticity(); }

                //// Camera Live Viewの表示
                //if (((TabItem)tcMain.Items[tcMain.SelectedIndex]).Header.ToString() == "Uniformity(Camera)")
                //{
                //    if (ccLiveView != null && ccLiveView.IsCameraOpened == true)
                //    { timerLiveView.Enabled = true; }
                //}
                //else
                //{ timerLiveView.Enabled = false; }
            }
        }
        
        private void tabItemToEnable(bool status)
        {
            tiData.IsEnabled = status;
            tiUniformity.IsEnabled = status;
            tiUniformityManual.IsEnabled = status;
            tiUfCamera.IsEnabled = status;
            tiGapCorrection.IsEnabled = status;
            tiGapCorrectionModule.IsEnabled = status;
            tiControl.IsEnabled = status;
            tiInformation.IsEnabled = status;
            tiMeasurement.IsEnabled = status;
            tiCalibration.IsEnabled = status;
            tiRelativeTarget.IsEnabled = status;
            tiDebug.IsEnabled = status;
        }

        #endregion Events

        #region Private Methods

        #region Common

        #region Communication

        private bool sendSdcpCommand(Byte[] aryCmd)
        {
            string dummy;
            return sendSdcpCommand(aryCmd, out dummy);
        }

        private bool sendSdcpCommand(Byte[] aryCmd, int waitTime)
        {
            string dummy;
            return sendSdcpCommand(aryCmd, out dummy, waitTime);
        }

        private bool sendSdcpCommand(Byte[] aryCmd, out string RecievedBuffer)
        {
            return sendSdcpCommand(aryCmd, out RecievedBuffer, Settings.Ins.SdcpWaitTime, currentControllerIP);
        }

        private bool sendSdcpCommand(Byte[] aryCmd, out string RecievedBuffer, int waitTime)
        {
            return sendSdcpCommand(aryCmd, out RecievedBuffer, waitTime, currentControllerIP);
        }

        private bool sendSdcpCommand(Byte[] aryCmd, string ip)
        {
            string dummy;
            return sendSdcpCommand(aryCmd, out dummy, ip);
        }

        private bool sendSdcpCommand(Byte[] aryCmd, int waitTime, string ip)
        {
            string dummy;
            return sendSdcpCommand(aryCmd, out dummy, waitTime, ip);
        }

        private bool sendSdcpCommand(Byte[] aryCmd, out string RecievedBuffer, string ip)
        {
            return sendSdcpCommand(aryCmd, out RecievedBuffer, Settings.Ins.SdcpWaitTime, ip);
        }

        private bool sendSdcpCommand(Byte[] aryCmd, out string RecievedBuffer, int waitTime, string ip)
        {
            Byte[] receivedBuffer;
            string receivedStr = "";

            showByteArray(CommunicationDirection.Send, aryCmd, ip);

            if (SDCPClass.SendCommand(aryCmd, out receivedBuffer, waitTime, ip) != true)
            {
                RecievedBuffer = "";
                return false;
            }

            if (receivedBuffer.Length > 18) // 本来は >= 20にすべき。18より大きいと、長さ＋データが追加されるので必ず20以上の長さがある
            {
                try
                {
                    for (int i = 19; i < receivedBuffer.Length; i++)
                    { receivedStr += receivedBuffer[i].ToString("X2"); }
                }
                catch(Exception ex)
                {
                    string errStr = "[sendSdcpCommand] Source : " + ex.Source + "\r\nException Message : " + ex.Message;
                    ShowMessageWindow(errStr, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                    RecievedBuffer = "";
                    return false;
                }
            }

            showByteArray(CommunicationDirection.Receive, receivedBuffer, ip);

            RecievedBuffer = receivedStr;

            return true;
        }

        private void showByteArray(CommunicationDirection dir, Byte[] aryByte, string ip)
        {
            string text = "";

            text += DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + " : ";

            text += "( " + ip + " ) ";

            if (dir == CommunicationDirection.Send)
            { text += ">> "; }
            else
            { text += "<< "; }

            if (aryByte.Length > 0)
            {
                for (int i = 0; i < aryByte.Length; i++)
                { text += aryByte[i].ToString("X2") + " "; }
            }

            // 別のスレッドからのコールに対応
            var dispatcher = Application.Current.Dispatcher;
            if (dispatcher.CheckAccess())
            { showSDCPLog(text); }
            else
            { dispatcher.Invoke(() => showSDCPLog(text)); }

            // SDCPの通信ログをファイルに残す
            if (Settings.Ins.SdcpLog == true)
            {
                string logFile = applicationPath + "\\Log\\";

                if(Directory.Exists(logFile) != true)
                { Directory.CreateDirectory(logFile); }


                string date = DateTime.Now.ToString("_yyyyMMdd");

                logFile += "SDCPLog" + date + ".txt";

                try
                {
                    using (System.IO.StreamWriter sw = new System.IO.StreamWriter(logFile, true))
                    { sw.WriteLine(text); }
                }
                catch { }
            }
        }

        private void showSDCPLog(string text)
        {
            lbSDCPLog.Items.Add(text);
        }

        #endregion Communication

        #region Display

        private void actionButton(object sender, string message, string sound = "button70.mp3")
        {
            ((Control)sender).IsEnabled = false;
            txtbStatus.Text = message;
            doEvents();

            playSound(applicationPath + "\\Components\\Sound\\" + sound);
        }

        private void releaseButton(object sender, string message = "Done")
        {
            ((Control)sender).IsEnabled = true;
            txtbStatus.Text = message;            
        }

        /// <summary>
        /// 現在メッセージ待ち行列の中にある全てのUIメッセージを処理します。
        /// </summary>
        private void doEvents()
        {
            DispatcherFrame frame = new DispatcherFrame();
            var callback = new DispatcherOperationCallback(obj =>
            {
                ((DispatcherFrame)obj).Continue = false;
                return null;
            });
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, callback, frame);
            Dispatcher.PushFrame(frame);
        }

        /// <summary>
        /// WAVEファイルを再生する
        /// </summary>
        /// <param name="waveFile"></param>
        private void playSound(string fileName)
        {
            string cmd;

            try
            {
                stopSound();

                //ファイルを開く
                cmd = "open \"" + fileName + "\" type mpegvideo alias " + aliasName;
                if (mciSendString(cmd, null, 0, IntPtr.Zero) != 0)
                { return; }

                //再生する
                cmd = "play " + aliasName;
                mciSendString(cmd, null, 0, IntPtr.Zero);
            }
            catch { }
        }

        /// <summary>
        /// 再生されている音を止める
        /// playした後stop（close）しないと次が再生できないみたい
        /// </summary>
        private void stopSound()
        {
            string cmd;

            try
            {
                //再生しているWAVEを停止する
                cmd = "stop " + aliasName;
                mciSendString(cmd, null, 0, IntPtr.Zero);

                //閉じる
                cmd = "close " + aliasName;
                mciSendString(cmd, null, 0, IntPtr.Zero);
            }
            catch { }
        }
        
        private void showStatusTextBlock(string text)
        {
            var dispatcher = Application.Current.Dispatcher;
            dispatcher.Invoke(() => showStatusText(text));
        }

        private void showStatusText(string text)
        {
            txtbStatus.Text = text;
        }

        private void setText(TextBox tb, string value)
        {
            base.Dispatcher.Invoke(() => tb.Text = value);
        }

        private void setText(TextBox tb, string value, Brush brush)
        {
            base.Dispatcher.Invoke(() => tb.Text = value);
            base.Dispatcher.Invoke(() => tb.Foreground = brush);
        }

        private void setImage(Image img, System.Drawing.Bitmap source)
        {
            IntPtr hbitmap = source.GetHbitmap();
            ImageSource imgSorce = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hbitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            imgSorce.Freeze();
            base.Dispatcher.Invoke(() => img.Source = imgSorce);
            DeleteObject(hbitmap);
        }

        #endregion Display

        #region Internal Signal

        private void outputIntSigFlat(int r = 492, int g = 492, int b = 492)
        {
            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            setIntSigFlat(ref cmd, r, g, b);

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(cmd, 0, cont.IPAddress); }

            Thread.Sleep(Settings.Ins.IntSignalWait);
        }

        private void setIntSigFlat(ref Byte[] cmd, int r, int g, int b)
        {
            try
            {
                // Foreground Color
                cmd[22] = (byte)(r >> 8); // Red
                cmd[23] = (byte)(r & 0xFF);
                cmd[24] = (byte)(g >> 8); // Green
                cmd[25] = (byte)(g & 0xFF);
                cmd[26] = (byte)(b >> 8); // Blue
                cmd[27] = (byte)(b & 0xFF);

                // Background Color
                cmd[28] = (byte)(0 >> 8); ; // Red
                cmd[29] = (byte)(0 & 0xFF);
                cmd[30] = (byte)(0 >> 8); ; // Green
                cmd[31] = (byte)(0 & 0xFF);
                cmd[32] = (byte)(0 >> 8); ; // Blue
                cmd[33] = (byte)(0 & 0xFF);

                cmd[34] = (byte)(0 >> 8);
                cmd[35] = (byte)(0 & 0xFF);
                cmd[36] = (byte)(0 >> 8);
                cmd[37] = (byte)(0 & 0xFF);

                // H, V Width
                cmd[38] = (byte)(0 >> 8);
                cmd[39] = (byte)(0 & 0xFF);
                cmd[40] = (byte)(0 >> 8);
                cmd[41] = (byte)(0 & 0xFF);

                // H, V Pitch
                cmd[42] = (byte)(0 >> 8);
                cmd[43] = (byte)(0 & 0xFF);
                cmd[44] = (byte)(0 >> 8);
                cmd[45] = (byte)(0 & 0xFF);
            }
            catch { } // 無視する

            // RGB
            cmd[21] = 0;
            cmd[21] += 0x40;
            cmd[21] += 0x20;
            cmd[21] += 0x10;

            cmd[21] += 0x00; // Flat            
        }

        private void outputIntSigWindow(int startX, int startY, int height, int width, int R = 492, int G = 492, int B = 492)
        {
            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            setIntSigWindowCommand(ref cmd, startX, startY, height, width, R, G, B);

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(cmd, 0, cont.IPAddress); }

            Thread.Sleep(Settings.Ins.IntSignalWait);
        }

        private void setIntSigWindowCommand(ref Byte[] cmd, int startX, int startY, int height, int width, int R, int G, int B)
        {
            try
            {
                // Foreground Color
                cmd[22] = (byte)(R >> 8); // Red
                cmd[23] = (byte)(R & 0xFF);
                cmd[24] = (byte)(G >> 8); // Green
                cmd[25] = (byte)(G & 0xFF);
                cmd[26] = (byte)(B >> 8); // Blue
                cmd[27] = (byte)(B & 0xFF);

                // Background Color
                cmd[28] = (byte)(0 >> 8); ; // Red
                cmd[29] = (byte)(0 & 0xFF);
                cmd[30] = (byte)(0 >> 8); ; // Green
                cmd[31] = (byte)(0 & 0xFF);
                cmd[32] = (byte)(0 >> 8); ; // Blue
                cmd[33] = (byte)(0 & 0xFF);

                // Start Position
                cmd[34] = (byte)(startX >> 8);
                cmd[35] = (byte)(startX & 0xFF);
                cmd[36] = (byte)(startY >> 8);
                cmd[37] = (byte)(startY & 0xFF);

                // H, V Width
                cmd[38] = (byte)(width >> 8);
                cmd[39] = (byte)(width & 0xFF);
                cmd[40] = (byte)(height >> 8);
                cmd[41] = (byte)(height & 0xFF);

                // H, V Pitch
                //cmd[42] = (byte)((pitchH) >> 8);
                //cmd[43] = (byte)((pitchH) & 0xFF);
                //cmd[44] = (byte)((pitchV) >> 8);
                //cmd[45] = (byte)((pitchV) & 0xFF);
            }
            catch { } // 無視する

            // RGB
            cmd[21] = 0;
            cmd[21] += 0x40;
            cmd[21] += 0x20;
            cmd[21] += 0x10;

            cmd[21] += 0x09; // Hatch            
        }

        private void outputIntSigHatch(int startX, int startY, int height, int width, int pitchH, int pitchV, int R = 492, int G = 492, int B = 492)
        {
            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            setIntSigHatchCommand(ref cmd, startX, startY, height, width, pitchH, pitchV, R, G, B);

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(cmd, 0, cont.IPAddress); }

            Thread.Sleep(Settings.Ins.IntSignalWait);
        }

        private void setIntSigHatchCommand(ref Byte[] cmd, int startX, int startY, int height, int width, int pitchH, int pitchV, int R, int G, int B)
        {
            try
            {
                // Foreground Color
                cmd[22] = (byte)(R >> 8); // Red
                cmd[23] = (byte)(R & 0xFF);
                cmd[24] = (byte)(G >> 8); // Green
                cmd[25] = (byte)(G & 0xFF);
                cmd[26] = (byte)(B >> 8); // Blue
                cmd[27] = (byte)(B & 0xFF);

                // Background Color
                cmd[28] = (byte)(0 >> 8); ; // Red
                cmd[29] = (byte)(0 & 0xFF);
                cmd[30] = (byte)(0 >> 8); ; // Green
                cmd[31] = (byte)(0 & 0xFF);
                cmd[32] = (byte)(0 >> 8); ; // Blue
                cmd[33] = (byte)(0 & 0xFF);

                // Start Position
                cmd[34] = (byte)(startX >> 8);
                cmd[35] = (byte)(startX & 0xFF);
                cmd[36] = (byte)(startY >> 8);
                cmd[37] = (byte)(startY & 0xFF);

                // H, V Width
                cmd[38] = (byte)(width >> 8);
                cmd[39] = (byte)(width & 0xFF);
                cmd[40] = (byte)(height >> 8);
                cmd[41] = (byte)(height & 0xFF);

                // H, V Pitch
                cmd[42] = (byte)((pitchH) >> 8);
                cmd[43] = (byte)((pitchH) & 0xFF);
                cmd[44] = (byte)((pitchV) >> 8);
                cmd[45] = (byte)((pitchV) & 0xFF);
            }
            catch { } // 無視する

            // RGB
            cmd[21] = 0;
            cmd[21] += 0x40;
            cmd[21] += 0x20;
            cmd[21] += 0x10;

            cmd[21] += 0x01; // Hatch            
        }

        // added by Hotta 2021/09/06
        private void outputIntSigHatchInv(int startX, int startY, int height, int width, int pitchH, int pitchV, int R = 492, int G = 492, int B = 492)
        {
            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            setIntSigHatchCommandInv(ref cmd, startX, startY, height, width, pitchH, pitchV, R, G, B);

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(cmd, 0, cont.IPAddress); }

            Thread.Sleep(Settings.Ins.IntSignalWait);
        }

        private void setIntSigHatchCommandInv(ref Byte[] cmd, int startX, int startY, int height, int width, int pitchH, int pitchV, int R, int G, int B)
        {
            try
            {
                // Foreground Color
                cmd[22] = (byte)(R >> 8); // Red
                cmd[23] = (byte)(R & 0xFF);
                cmd[24] = (byte)(G >> 8); // Green
                cmd[25] = (byte)(G & 0xFF);
                cmd[26] = (byte)(B >> 8); // Blue
                cmd[27] = (byte)(B & 0xFF);

                // Background Color
                cmd[28] = (byte)(0 >> 8); ; // Red
                cmd[29] = (byte)(0 & 0xFF);
                cmd[30] = (byte)(0 >> 8); ; // Green
                cmd[31] = (byte)(0 & 0xFF);
                cmd[32] = (byte)(0 >> 8); ; // Blue
                cmd[33] = (byte)(0 & 0xFF);

                // Start Position
                cmd[34] = (byte)(startX >> 8);
                cmd[35] = (byte)(startX & 0xFF);
                cmd[36] = (byte)(startY >> 8);
                cmd[37] = (byte)(startY & 0xFF);

                // H, V Width
                cmd[38] = (byte)(width >> 8);
                cmd[39] = (byte)(width & 0xFF);
                cmd[40] = (byte)(height >> 8);
                cmd[41] = (byte)(height & 0xFF);

                // H, V Pitch
                cmd[42] = (byte)((pitchH) >> 8);
                cmd[43] = (byte)((pitchH) & 0xFF);
                cmd[44] = (byte)((pitchV) >> 8);
                cmd[45] = (byte)((pitchV) & 0xFF);
            }
            catch { } // 無視する

            // RGB
            cmd[21] = 0;
            cmd[21] += 0x40;
            cmd[21] += 0x20;
            cmd[21] += 0x10;

            cmd[21] += 0x01; // Hatch            

            cmd[21] += 0x80;    // invert
        }
        //
        
        // added by Hotta 2021/09/07
        private void outputIntSigFlatGap(int startX, int startY, int height, int width, int pitchH, int pitchV, int FlatR, int FlatG, int FlatB, int GapR, int GapG, int GapB)
        {
            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            setIntSigFlatGapCommand(ref cmd, startX, startY, height, width, pitchH, pitchV, FlatR, FlatG, FlatB, GapR, GapG, GapB);

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(cmd, 0, cont.IPAddress); }

            Thread.Sleep(Settings.Ins.IntSignalWait);
        }
        
        private void setIntSigFlatGapCommand(ref Byte[] cmd, int startX, int startY, int height, int width, int pitchH, int pitchV, int FlatR, int FlatG, int FlatB, int GapR, int GapG, int GapB)
        {
            try
            {
                // Foreground Color
                cmd[22] = (byte)(GapR >> 8); // Red
                cmd[23] = (byte)(GapR & 0xFF);
                cmd[24] = (byte)(GapG >> 8); // Green
                cmd[25] = (byte)(GapG & 0xFF);
                cmd[26] = (byte)(GapB >> 8); // Blue
                cmd[27] = (byte)(GapB & 0xFF);

                // Background Color
                cmd[28] = (byte)(FlatR >> 8); ; // Red
                cmd[29] = (byte)(FlatR & 0xFF);
                cmd[30] = (byte)(FlatG >> 8); ; // Green
                cmd[31] = (byte)(FlatG & 0xFF);
                cmd[32] = (byte)(FlatB >> 8); ; // Blue
                cmd[33] = (byte)(FlatB & 0xFF);

                // Start Position
                cmd[34] = (byte)(startX >> 8);
                cmd[35] = (byte)(startX & 0xFF);
                cmd[36] = (byte)(startY >> 8);
                cmd[37] = (byte)(startY & 0xFF);

                // H, V Width
                cmd[38] = (byte)(width >> 8);
                cmd[39] = (byte)(width & 0xFF);
                cmd[40] = (byte)(height >> 8);
                cmd[41] = (byte)(height & 0xFF);

                // H, V Pitch
                cmd[42] = (byte)((pitchH) >> 8);
                cmd[43] = (byte)((pitchH) & 0xFF);
                cmd[44] = (byte)((pitchV) >> 8);
                cmd[45] = (byte)((pitchV) & 0xFF);
            }
            catch { } // 無視する

            // RGB
            cmd[21] = 0;
            cmd[21] += 0x40;
            cmd[21] += 0x20;
            cmd[21] += 0x10;

            cmd[21] += 0x01; // Hatch            

            cmd[21] += 0x80;    // invert
        }
        //

        private void outputIntSigChecker(int startX, int startY, int height, int width, int pitchH, int pitchV, int R = 492, int G = 492, int B = 492)
        {
            Byte[] cmd = new byte[SDCPClass.CmdIntSignalOn.Length];
            Array.Copy(SDCPClass.CmdIntSignalOn, cmd, SDCPClass.CmdIntSignalOn.Length);

            setIntSigChecker(ref cmd, startX, startY, height, width, pitchH, pitchV, R, G, B);

            foreach (ControllerInfo cont in dicController.Values)
            { sendSdcpCommand(cmd, 0, cont.IPAddress); }

            Thread.Sleep(Settings.Ins.IntSignalWait);
        }

        private void setIntSigChecker(ref Byte[] cmd, int startX, int startY, int height, int width, int pitchH, int pitchV, int R, int G, int B)
        {
            try
            {
                // Foreground Color
                cmd[22] = (byte)(R >> 8); // Red
                cmd[23] = (byte)(R & 0xFF);
                cmd[24] = (byte)(G >> 8); // Green
                cmd[25] = (byte)(G & 0xFF);
                cmd[26] = (byte)(B >> 8); // Blue
                cmd[27] = (byte)(B & 0xFF);

                // Background Color
                cmd[28] = (byte)(0 >> 8); ; // Red
                cmd[29] = (byte)(0 & 0xFF);
                cmd[30] = (byte)(0 >> 8); ; // Green
                cmd[31] = (byte)(0 & 0xFF);
                cmd[32] = (byte)(0 >> 8); ; // Blue
                cmd[33] = (byte)(0 & 0xFF);

                // Start Position
                cmd[34] = (byte)(startX >> 8);
                cmd[35] = (byte)(startX & 0xFF);
                cmd[36] = (byte)(startY >> 8);
                cmd[37] = (byte)(startY & 0xFF);

                // H, V Width
                cmd[38] = (byte)(width >> 8);
                cmd[39] = (byte)(width & 0xFF);
                cmd[40] = (byte)(height >> 8);
                cmd[41] = (byte)(height & 0xFF);

                // H, V Pitch
                cmd[42] = (byte)((pitchH) >> 8);
                cmd[43] = (byte)((pitchH) & 0xFF);
                cmd[44] = (byte)((pitchV) >> 8);
                cmd[45] = (byte)((pitchV) & 0xFF);
            }
            catch { } // 無視する

            // RGB
            cmd[21] = 0;
            cmd[21] += 0x40;
            cmd[21] += 0x20;
            cmd[21] += 0x10;

            cmd[21] += 0x02; // Hatch            

            cmd[21] += 0x80;    // invert
        }

        private void stopIntSig()
        {
            //foreach (ControllerInfo cont in dicController.Values)
            //{ sendSdcpCommand(SDCPClass.CmdIntSignalOff, 0, cont.IPAddress); }
            outputIntSigFlat(0, 0, 0); // 信号停止を全黒へ変更

            Thread.Sleep(Settings.Ins.IntSignalWait);
        }

        #endregion Internal Signal

        #region MISC

        private string verToString(System.Version ver)
        {
            string version = "";

            version = ver.Major + "." + ver.Minor + ".";

            if (ver.Build == 0)
            { version += "0."; }
            else
            { version += ver.Build.ToString("00") + "."; }

            if(ver.Revision == 0)
            { version += "0"; }
            else
            { version += ver.Revision.ToString("00"); }

            return version;
        }

        private bool openProfile(string path)
        {
            // モデル判定
            determineProfileType(path);

            // File Check
            if (!isValidProfile(path))
            {
                string msg = "Invalid profile.";
                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }

            profilePath = applicationPath + "\\Profile\\" + System.IO.Path.GetFileName(path);
            string profileDir = applicationPath + "\\Profile";

            // File Copy
            if (System.IO.Path.GetDirectoryName(path) != profileDir)
            {
                if (System.IO.File.Exists(profilePath) == true)
                {
                    string msg = "A file of the same name already exists.\r\n( " + profilePath + " )\r\n\r\nAre you sure you want to delete it?";
                    //MessageBox.Show(msg, "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Question)

                    bool? result = false;
                    showMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 500, 210, "Yes");
                    
                    if (result == true)
                    {
                        try { FileSystem.DeleteFile(profilePath, UIOption.OnlyErrorDialogs, RecycleOption.DeletePermanently); }
                        catch { }
                    }
                    else { return false; }
                }

                System.IO.File.Copy(path, profilePath);
            }

            // File Open
            if (loadProfile(profilePath, false) != true)
            { return false; }

            return true;
        }

        /// <summary>
        /// プロファイルを読み込む
        /// </summary>
        /// <param name="path">読み込むプロファイルのパス</param>
        /// <param name="isNormalBoot">通常起動:true、Profile読込む:false</param>
        private bool loadProfile(string path, bool isNormalBoot = true)
        {
            try
            {
                if (isNormalBoot)
                { determineProfileType(path); }

                if (profileType == ProfileType.DCS)
                {
                    if (DCSProfileData == null)
                    {
                        DCSProfileXmlToObjectConvert(path);

                        if (!isValidDCSProfile())
                        {
                            string msg = "Invalid profile.";
                            ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                            return false;
                        }
                    }

                    if (!loadDCSProfileXML())
                    {
                        string msg = "Invalid profile.";
                        ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                        return false;
                    }
                }
                else if (profileType == ProfileType.CAS)
                {
                    if (CASProfileData == null)
                    {
                        CASProfileXmlToObjectConvert(path);

                        if (!isValidCASProfile())
                        {
                            string msg = "Invalid profile.";
                            ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                            return false;
                        }
                    }

                    if (!loadCASProfileXML())
                    {
                        string msg = "Invalid profile.";
                        ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                        return false;
                    }
                }
                else
                {
                    string msg = "Invalid profile.";
                    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }

                showConfiguration();

                profileLoaded = true;
                Settings.Ins.LastProfile = path;
            }
            catch
            {
                string msg = "Invalid profile.";
                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }

            #region やっぱやめた
            //try
            //{
            //    List<LayoutItem> lstLayout;

            //    #region GIANT
            //    if (model == Model.GIANT)
            //    {
            //        System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(boost_serialization_GIANT));
            //        boost_serialization_GIANT profileData;

            //        using (System.IO.StreamReader sr = new System.IO.StreamReader(path, new System.Text.UTF8Encoding(false)))
            //        { profileData = (boost_serialization_GIANT)serializer.Deserialize(sr); }

            //        // Profileから内部データにController情報とUnit Allocation情報を格納する
            //        // ◆Controller情報
            //        dicController = new Dictionary<int, ControllerInfo>();
            //        foreach (ControllerItem_GIANT item in profileData.ProfileData.ConnectionSetting.item)
            //        {
            //            ControllerInfo info = new ControllerInfo();
            //            info.ControllerID = item.ControllerID;
            //            info.IPAddress = item.IPAddress;

            //            if (item.MasterSlaveMode == 1)
            //            { info.Master = true; }
            //            else
            //            { info.Master = false; }
                        
            //            info.Model = Model.GIANT;

            //            try { dicController.Add(info.ControllerID, info); }
            //            catch
            //            {
            //                string msg = "Controller ID is repeating.";
            //                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
            //                return false;
            //            }
            //        }

            //        lstLayout = profileData.ProfileData.LayoutSetting.item;
            //    }
            //    #endregion GIANT
            //    #region Monolith
            //    else if (model == Model.Monolith)
            //    {
            //        System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(boost_serialization_Monolith));
            //        boost_serialization_Monolith profileData;

            //        using (System.IO.StreamReader sr = new System.IO.StreamReader(path, new System.Text.UTF8Encoding(false)))
            //        { profileData = (boost_serialization_Monolith)serializer.Deserialize(sr); }

            //        // Profileから内部データにController情報とUnit Allocation情報を格納する
            //        // ◆Controller情報
            //        dicController = new Dictionary<int, ControllerInfo>();
            //        foreach (ControllerItem_Monolith item in profileData.ProfileData.ConnectionSetting.item)
            //        {
            //            ControllerInfo info = new ControllerInfo();
            //            info.ControllerID = item.ControllerID;
            //            info.IPAddress = item.IPAddress;

            //            if (item.MasterSlaveMode == 1)
            //            { info.Master = true; }
            //            else
            //            { info.Master = false; }

            //            info.Model = Model.Monolith;

            //            try { dicController.Add(info.ControllerID, info); }
            //            catch
            //            {
            //                string msg = "Controller ID is repeating.";
            //                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
            //                return false;
            //            }
            //        }
                    
            //        lstLayout = profileData.ProfileData.LayoutSetting.item;
            //    }
            //    #endregion Monolith
            //    // それ以外 (Configuration File)
            //    else
            //    { throw new System.Exception(); }

            //    // ◆Unit Allocation情報 (共通)
            //    int x = 0, y = 0;
            //    foreach (LayoutItem layoutItem in lstLayout)
            //    {
            //        // 各ControllerのUnit Count
            //        foreach (ControllerInfo info in dicController.Values)
            //        {
            //            if (info.ControllerID == layoutItem.ControllerID)
            //            {
            //                info.UnitCount = layoutItem.UnitLayoutSetting.count;
            //                break;
            //            }
            //        }

            //        // X, Yの最大値検索
            //        foreach (UnitItem item in layoutItem.UnitLayoutSetting.item)
            //        {
            //            if (item.Allocation.X > x)
            //            { x = item.Allocation.X; }

            //            if (item.Allocation.Y > y)
            //            { y = item.Allocation.Y; }
            //        }
            //    }

            //    allocInfo = new AllocationInfo();
            //    allocInfo.MaxX = x;
            //    allocInfo.MaxY = y;

            //    // Allocation情報を初期化
            //    for (int i = 0; i < allocInfo.MaxX; i++)
            //    {
            //        allocInfo.lstUnits.Add(new List<UnitInfo>());

            //        for (int j = 0; j < allocInfo.MaxY; j++)
            //        {
            //            UnitInfo unit = new UnitInfo();
            //            allocInfo.lstUnits[i].Add(unit);
            //        }
            //    }

            //    foreach (LayoutItem layoutItem in lstLayout)
            //    {
            //        foreach (UnitItem item in layoutItem.UnitLayoutSetting.item)
            //        {
            //            UnitInfo unit = new UnitInfo();

            //            unit.Enable = true;
            //            unit.ControllerID = layoutItem.ControllerID;
            //            unit.PortNo = item.PortNo;
            //            unit.UnitNo = item.UnitNo;
            //            unit.X = item.Allocation.X;
            //            unit.Y = item.Allocation.Y;
            //            unit.PixelX = item.Picture.PixelX;
            //            unit.PixelY = item.Picture.PixelY;
            //            unit.CellDataFile = "u" + unit.PortNo.ToString("D2") + unit.UnitNo.ToString("D2") + "_cd.bin";
            //            unit.AgingDataFile = "u" + unit.PortNo.ToString("D2") + unit.UnitNo.ToString("D2") + "_ad.bin";

            //            allocInfo.lstUnits[item.Allocation.X - 1][item.Allocation.Y - 1] = unit;
            //        }
            //    }

            //    showConfiguration();

            //    profileLoaded = true;
            //    Settings.Ins.LastProfile = path;
            //}
            //// Configuration File
            //catch
            //{
            //    if (loadConfigurationXML(path) != true)
            //    { return false; }

            //    showConfiguration();

            //    profileLoaded = true;
            //    Settings.Ins.LastProfile = path;
            //}
            #endregion

            // Debug用
            if (appliMode == ApplicationMode.Developer)
            {
                this.Title = this.Title.Replace("  (" + ProfileType.DCS.ToString() + ")", "");
                this.Title = this.Title.Replace("  (" + ProfileType.CAS.ToString() + ")", "");

                this.Title += "  (" + profileType.ToString() + ")";
            }

            return true;
        }

        private string validLedModel(string ledModel, string ledPitch)
        {
            if (!string.IsNullOrEmpty(ledModel))
            {
                return ledModel == ZRD_B12A ||
                       ledModel == ZRD_B15A ||
                       ledModel == ZRD_C12A ||
                       ledModel == ZRD_C15A ||
                       ledModel == ZRD_BH12D ||
                       ledModel == ZRD_BH15D ||
                       ledModel == ZRD_CH12D ||
                       ledModel == ZRD_CH15D ||
                       ledModel == ZRD_BH12D_S3 ||
                       ledModel == ZRD_BH15D_S3 ||
                       ledModel == ZRD_CH12D_S3 ||
                       ledModel == ZRD_CH15D_S3 ? ledModel : ZRD_B12A;
            }
            else
            {
                return ledPitch == "1.5" ? ZRD_B15A : ZRD_B12A;
        }
        }

        private bool validConfigChromType(ConfigChrom chrom)
        {
            switch (chrom)
            {
                case ConfigChrom.ZRD_B12A:
                case ConfigChrom.ZRD_B15A:
                case ConfigChrom.ZRD_C12A:
                case ConfigChrom.ZRD_C15A:
                case ConfigChrom.ZRD_BH12D:
                case ConfigChrom.ZRD_BH15D:
                case ConfigChrom.ZRD_CH12D:
                case ConfigChrom.ZRD_CH15D:
                case ConfigChrom.ZRD_BH12D_S3:
                case ConfigChrom.ZRD_BH15D_S3:
                case ConfigChrom.ZRD_CH12D_S3:
                case ConfigChrom.ZRD_CH15D_S3:
                    return true;
                default:
                    return false;
        }
        }

        private bool saveConfiguration()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FilterIndex = 1;
            sfd.Filter = "Configuration File(.xml)|*.xml|All Files (*.*)|*.*";
            if (sfd.ShowDialog() == true)
            {
                string path = sfd.FileName;
                if (saveConfigurationXML(path) != true)
                { return false; }

                Settings.Ins.LastProfile = path;
                profileLoaded = true;

                return true;
            }   

            return false;
        }

        private bool saveConfigurationXML(string path)
        {
            ConfigurationInfo conf = new ConfigurationInfo();
            conf.Model = profileType;
            conf.dicController = dicController;
            conf.allocInfo = allocInfo;

            try
            {
                //DataContractSerializerオブジェクトを作成
                //オブジェクトの型を指定する
                DataContractSerializer serializer = new DataContractSerializer(typeof(ConfigurationInfo));

                //BOMが付かないUTF-8で、書き込むファイルを開く
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Encoding = new System.Text.UTF8Encoding(false);

                settings.Indent = true; // インデントを行う
                settings.IndentChars = "\t"; // インデントにタブ文字を用いる
                settings.NewLineOnAttributes = true; // 属性にも改行を行う

                using (XmlWriter xw = XmlWriter.Create(path, settings))
                {
                    //シリアル化し、XMLファイルに保存する
                    serializer.WriteObject(xw, conf);
                }
            }
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }

            return true;
        }

        private bool loadConfigurationXML(string path)
        {
            try
            {
                //DataContractSerializerオブジェクトを作成
                DataContractSerializer serializer = new DataContractSerializer(typeof(ConfigurationInfo));

                //読み込むファイルを開く
                XmlReader xr = XmlReader.Create(path);

                //XMLファイルから読み込み、逆シリアル化する
                ConfigurationInfo obj = (ConfigurationInfo)serializer.ReadObject(xr);

                //ファイルを閉じる
                xr.Close();

                // Profileの妥当性チェック
                //if (isValidCASProfile(obj) != true)
                //{
                //    string msg = "Invalid profile.";
                //    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                //}

                profileType = obj.Model;
                dicController = obj.dicController;
                allocInfo = obj.allocInfo;

                allocInfo.LEDModel = validLedModel(allocInfo.LEDModel, allocInfo.LedPitch);
            }
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }

            return true;
        }
        
        private bool getAllocationInfo(List<ControllerInfo> lstControllerInfo)
        {
            var dispatcher = Application.Current.Dispatcher;

            Dictionary<int, ControllerInfo> newDicController = new Dictionary<int, ControllerInfo>();

            AllocationInfo newAllocInfo = new AllocationInfo();
            List<UnitInfo> lstUnitInfo = new List<UnitInfo>();

            // ●進捗の最大値を設定
            winProgress.ShowMessage("Set Step Count.");
            int step = lstControllerInfo.Count * 4 + 1; // Power On
            winProgress.SetWholeSteps(step);

            string msg, buff;
            foreach (ControllerInfo controller in lstControllerInfo)
            {
                winProgress.ShowMessage("Controller Power off.");

                // Controller Standby
                PowerStatus powerStatus;
                if (getPowerStatus(controller.IPAddress, out powerStatus) != true)
                { return false; }

                if (powerStatus != PowerStatus.Standby)
                {
                    if (sendSdcpCommand(SDCPClass.CmdStandby, out buff, controller.IPAddress) != true)
                    { return false; }

                    DateTime startTime = DateTime.Now;
                    while (true)
                    {
                        if (getPowerStatus(controller.IPAddress, out powerStatus) != true)
                        { return false; }

                        if (powerStatus == PowerStatus.Shutting_Down || powerStatus == PowerStatus.Standby)
                        { break; }

                        TimeSpan span = DateTime.Now - startTime;
                        if (span.TotalSeconds > CONTROLLER_POWER_STATUS_TIMEOUT)
                        {
                            msg = "Failed to standby.";
                            ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                            return false;
                        }

                        System.Threading.Thread.Sleep(1000);
                    }
                }
                winProgress.PutForward1Step();
            }

            foreach (ControllerInfo controller in lstControllerInfo)
            {
                winProgress.ShowMessage("Get Controller ID.");

                // Controller ID
                sendSdcpCommand(SDCPClass.CmdControllerIDGet, out buff, controller.IPAddress);
                int controllerCount = int.Parse(buff.Substring(2, 2));
                int controllerID = int.Parse(buff.Substring(0, 2));

                if (buff == SDCP_NAK_NOT_APPLICABLE || controllerCount == 0 || controllerID == 0)
                {
                    msg = "Failed to get ControllerID.";
                    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 380, 210);
                    return false;
                }
                else
                { controller.ControllerID = controllerID; }

                // Controller ID重複チェック
                if (newDicController.ContainsKey(controller.ControllerID))
                {
                    msg = "Failed to get Layout Info.\r\nBecause the ControllerID is duplicated.";
                    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 380, 210);
                    return false;
                }

                newDicController.Add(controller.ControllerID, controller);
                winProgress.PutForward1Step();
            }

            msg = "Failed to get Layout Info.\r\nBecause the cabinet model for each controller does not match.";
            foreach (ControllerInfo controller in newDicController.Values)
            {
                winProgress.ShowMessage("Controller : " + controller.ControllerID + "\r\nGet LED Model.");

                // Master / Slave
                sendSdcpCommand(SDCPClass.CmdControllerMasterSlaveGet, out buff, controller.IPAddress);
                if (buff == "01")
                { controller.Master = true; }
                else
                { controller.Master = false; }

                // Get LED Model
                if (getLedModel(controller.IPAddress, controller.Password, out string ledModel) != true)
                {
                    msg = "Failed to get LED Model.\r\nController ID: " + controller.ControllerID;
                    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 380, 210);
                    return false;
                }

                msg += "\r\nController " + controller.ControllerID + ": " + ledModel;
                if (newAllocInfo.LEDModel != null && newAllocInfo.LEDModel != ledModel)
                {
                    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 500, 210);
                    return false;
                }

                newAllocInfo.LEDModel = ledModel;

                winProgress.PutForward1Step();
            }

            foreach (ControllerInfo controller in newDicController.Values)
            {
                int maxUnitNo;

                if (newAllocInfo.LEDModel == ZRD_C15A
                    || newAllocInfo.LEDModel == ZRD_B15A 
                    || newAllocInfo.LEDModel == ZRD_CH15D 
                    || newAllocInfo.LEDModel == ZRD_BH15D
                    || newAllocInfo.LEDModel == ZRD_CH15D_S3
                    || newAllocInfo.LEDModel == ZRD_BH15D_S3)
                {
                    maxUnitNo = Defined.Max_UnitNo_P15;
                }
                else
                {
                    maxUnitNo = Defined.Max_UnitNo_P12;
                }

                // Get Allocation/Picture Info
                for (int i = 0; i < 12; i++)
                {
                    for (int j = 0; j < maxUnitNo; j++)
                    {
                        UnitInfo info = new UnitInfo();
                        info.Enable = true;
                        info.ControllerID = controller.ControllerID;
                        info.PortNo = i + 1;
                        info.UnitNo = j + 1;
                        info.CellDataFile = "u" + info.PortNo.ToString("D2") + info.UnitNo.ToString("D2") + "_cd.bin";
                        info.AgingDataFile = "u" + info.PortNo.ToString("D2") + info.UnitNo.ToString("D2") + "_ad.bin";

                        // Allocation Info
                        byte[] cmd = new byte[SDCPClass.CmdUnitAllocationAddressGet.Length];
                        Array.Copy(SDCPClass.CmdUnitAllocationAddressGet, cmd, SDCPClass.CmdUnitAllocationAddressGet.Length);

                        string port = (i + 1).ToString("X1");
                        string unit = (j + 1).ToString("X1");

                        cmd[9] = (byte)Convert.ToInt32(port + unit, 16);

                        sendSdcpCommand(cmd, out buff, controller.IPAddress);

                        if (buff.Length < 8)
                        { return false; }

                        string strX = buff.Substring(0, 4);
                        string strY = buff.Substring(4, 4);

                        info.X = Convert.ToInt32(strX, 16);
                        info.Y = Convert.ToInt32(strY, 16);

                        if (info.X == 0 && info.Y == 0)
                        { break; }

                        winProgress.ShowMessage("Controller : " + controller.ControllerID + "\r\nPort No. : " + (i + 1) + ", Cabinet No. : " + (j + 1) + " (Allocation Info.)");

                        // Picture Info
                        cmd = new byte[SDCPClass.CmdUnitPictureAddressGet.Length];
                        Array.Copy(SDCPClass.CmdUnitPictureAddressGet, cmd, SDCPClass.CmdUnitPictureAddressGet.Length);

                        cmd[9] = (byte)Convert.ToInt32(port + unit, 16);

                        sendSdcpCommand(cmd, out buff, controller.IPAddress);
                        
                        if (buff.Length < 8)
                        { return false; }

                        string strPixX = buff.Substring(0, 4);
                        string strPixY = buff.Substring(4, 4);

                        info.PixelX = Convert.ToInt32(strPixX, 16);
                        info.PixelY = Convert.ToInt32(strPixY, 16);
                        
                        winProgress.ShowMessage("Controller : " + controller.ControllerID + "\r\nPort No. : " + (i + 1) + ", Cabinet No. : " + (j + 1) + " (Picture Info.)");

                        lstUnitInfo.Add(info);
                        controller.UnitCount++;
                    }
                }
                winProgress.PutForward1Step();
            }

            // X, Yの最大値検索
            int maxX = 0, maxY = 0;
            foreach (UnitInfo info in lstUnitInfo)
            {
                if (info.X > maxX)
                { maxX = info.X; }

                if (info.Y > maxY)
                { maxY = info.Y; }
            }

            newAllocInfo.MaxX = maxX;
            newAllocInfo.MaxY = maxY;

            // Allocation情報を初期化
            for (int i = 0; i < newAllocInfo.MaxX; i++)
            {
                newAllocInfo.lstUnits.Add(new List<UnitInfo>());

                for (int j = 0; j < newAllocInfo.MaxY; j++)
                {
                    UnitInfo unit = null; //new UnitInfo();
                    newAllocInfo.lstUnits[i].Add(unit);
                }
            }

            foreach (UnitInfo info in lstUnitInfo)
            { newAllocInfo.lstUnits[info.X - 1][info.Y - 1] = info; }
            
            // ●Controller Power On
            winProgress.ShowMessage("Controller Power On.");

            if (!setAllControllerPowerOn(newDicController))
            { return false; }
            winProgress.PutForward1Step();

            // 最新データに更新
            dicController = newDicController;
            allocInfo = newAllocInfo;

            return true;
        }

        //private bool turnOnHDCP()
        //{
        //    var dispatcher = Application.Current.Dispatcher;

        //    // ●進捗の最大値を設定
        //    winProgress.ShowMessage("Set Step Count.");
        //    int step = dicController.Count * 2;
        //    winProgress.SetWholeSteps(step);
        //    winProgress.SetMarquee();

        //    // Power On
        //    PowerStatus power;
        //    bool powerUpWait = false;
        //    foreach (ControllerInfo cont in dicController.Values)
        //    {
        //        winProgress.ShowMessage("Controller Power On. (Controller : " + cont.ControllerID + ")");

        //        if (getPowerStatus(cont.IPAddress, out power) != true)
        //        { return false; }

        //        if (power != PowerStatus.Power_On)
        //        {
        //            if (sendSdcpCommand(SDCPClass.CmdPowerUp, cont.IPAddress) != true)
        //            { return false; }

        //            powerUpWait = true;
        //        }

        //        winProgress.PutForward1Step();
        //    }

        //    if (powerUpWait == true)
        //    { System.Threading.Thread.Sleep(Settings.Ins.PowerOnWait); }

        //    // HDCP On
        //    foreach (ControllerInfo cont in dicController.Values)
        //    {
        //        string buff;
                
        //        //winProgress.ShowMessage("Send HDCP On. (Controller : " + cont.ControllerID + ")");

        //        bool result = sendSdcpCommand(SDCPClass.CmdHDCPSet, out buff, cont.IPAddress);
        //        if (result == false)
        //        {
        //            // タイムアウトの場合は再送信
        //            System.Threading.Thread.Sleep(1000);
        //            sendSdcpCommand(SDCPClass.CmdHDCPSet, out buff, cont.IPAddress);
        //        }

        //        // コマンド未対応
        //        if (buff == "0101")
        //        {
        //            supportHdcpCmd = false;
        //            dispatcher.Invoke(() => (gdHdcp.IsEnabled = false));
        //        }

        //        winProgress.PutForward1Step();
        //    }

        //    return true;
        //}

        private void showConfiguration()
        {
            const int FIXED_WIDTH = 64;
            const int FIXED_HEIGHT = 36;

            // Controller一覧 (Backup)
            contData = new ObservableCollection<ControllerInfo>();

            foreach (ControllerInfo cont in dicController.Values)
            { contData.Add(cont); }

            dgController.ItemsSource = contData;

            if (dicController.Count > 0)
            {
                contData[0].Select = true;
                dgController.SelectedIndex = 0;
            }

            // Controller (Control)
            try { cmbxControllerControl.Items.Clear(); }
            catch { }

            cmbxControllerControl.Items.Add("All");

            foreach (ControllerInfo controller in dicController.Values)
            { cmbxControllerControl.Items.Add("Controller_" + controller.ControllerID); }

            if (cmbxControllerControl.Items.Count > 0)
            { cmbxControllerControl.SelectedIndex = 0; }

            // Cabinet Allocation段/列に番号設定
            CabinetAllocationHeaderViewModel allocaionheaderVm = new CabinetAllocationHeaderViewModel();
            allocaionheaderVm.ColumnMaxX = allocInfo.MaxX;
            allocaionheaderVm.ColumnMaxY = allocInfo.MaxY;
            gdAllocData.DataContext = allocaionheaderVm;
            gdAllocUf.DataContext = allocaionheaderVm;
            gdAllocInfo.DataContext = allocaionheaderVm;
            gdAllocMeasurement.DataContext = allocaionheaderVm;
            gdAllocCalibration.DataContext = allocaionheaderVm;
            gdAllocRelativeTarget.DataContext = allocaionheaderVm;
            gdUfCamAlloc.DataContext = allocaionheaderVm;
            gdAllocGapCam.DataContext = allocaionheaderVm;
            gdAllocGg.DataContext = allocaionheaderVm;
            gdAllocSig.DataContext = allocaionheaderVm;

            // ●Unit Allocation (Data)
            gdUnitArrayData.Children.Clear();
            gdUnitArrayData.Width = allocInfo.MaxX * FIXED_WIDTH; // 64:ToggleBottanの幅
            gdUnitArrayData.Height = allocInfo.MaxY * FIXED_HEIGHT; // 36:ToggleBottanの高さ
            gdUnitFrameData.Width = gdUnitArrayData.Width;
            gdUnitFrameData.Height = gdUnitArrayData.Height;

            aryUnitData = new UnitToggleButton[allocInfo.MaxX, allocInfo.MaxY];

            for (int i = 0; i < allocInfo.MaxX; i++)
            {
                for (int j = 0; j < allocInfo.MaxY; j++)
                {
                    UnitToggleButton utb = new UnitToggleButton();
                    utb.Height = FIXED_HEIGHT;
                    utb.Width = FIXED_WIDTH;
                    utb.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    utb.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    utb.Margin = new Thickness(i * FIXED_WIDTH, j * FIXED_HEIGHT, 0, 0);
                    utb.BorderThickness = new Thickness(1);
                    utb.IsEnabled = false;
                    utb.Checked += UnitDataToggleButton_Checked;
                    utb.PreviewMouseDown += utbData_PreviewMouseDown;
                    utb.PreviewMouseUp += utbData_PreviewMouseUp;
                    utb.PreviewMouseMove += utbData_PreviewMouseMove;
                    gdUnitArrayData.Children.Add(utb);

                    aryUnitData[i, j] = utb;
                }
            }

            for (int i = 0; i < allocInfo.lstUnits.Count; i++)
            {
                for (int j = 0; j < allocInfo.lstUnits[i].Count; j++)
                {
                    UnitInfo ui = allocInfo.lstUnits[i][j];

                    if (ui == null || ui.Enable == false)
                    { continue; }

                    aryUnitData[ui.X - 1, ui.Y - 1].Content = "C" + ui.ControllerID + "-" + ui.PortNo + "-" + ui.UnitNo;
                    aryUnitData[ui.X - 1, ui.Y - 1].IsEnabled = true;
                    aryUnitData[ui.X - 1, ui.Y - 1].UnitInfo = ui;
                }
            }

            // Cell (Data)
            gdCellArrayData.Children.Clear();
            aryCellData = new UnitToggleButton[4, 3];

            int cellNo = 1;
            for (int i = 0; i < 3; i++) // Y
            {
                for (int j = 0; j < 4; j++) // X
                {
                    UnitToggleButton utb = new UnitToggleButton();
                    utb.Content = "No." + cellNo++;
                    utb.Height = 45;
                    utb.Width = 40;
                    utb.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    utb.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    utb.Margin = new Thickness(j * 40, i * 45, 0, 0);
                    utb.BorderThickness = new Thickness(1);
                    utb.IsEnabled = true;
                    utb.Checked += CellDataToggleButton_Checked;
                    gdCellArrayData.Children.Add(utb);

                    aryCellData[j, i] = utb;
                }
            }

            // ●Unit Allocation (UF)
            gdUnitArray.Children.Clear();
            gdUnitArray.Width = allocInfo.MaxX * FIXED_WIDTH; // 64:ToggleBottanの幅
            gdUnitArray.Height = allocInfo.MaxY * FIXED_HEIGHT; // 36:ToggleBottanの高さ
            gdUnitFrame.Width = gdUnitArray.Width;
            gdUnitFrame.Height = gdUnitArray.Height;

            aryUnitUf = new UnitToggleButton[allocInfo.MaxX, allocInfo.MaxY];

            for (int i = 0; i < allocInfo.MaxX; i++)
            {
                for (int j = 0; j < allocInfo.MaxY; j++)
                {
                    UnitToggleButton utb = new UnitToggleButton();
                    utb.Height = FIXED_HEIGHT;
                    utb.Width = FIXED_WIDTH;
                    utb.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    utb.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    utb.Margin = new Thickness(i * FIXED_WIDTH, j * FIXED_HEIGHT, 0, 0);
                    utb.BorderThickness = new Thickness(1);
                    utb.IsEnabled = false;
                    utb.Checked += UnitToggleButton_Checked;
                    utb.PreviewMouseDown += utbUf_PreviewMouseDown;
                    utb.PreviewMouseUp += utbUf_PreviewMouseUp;
                    utb.PreviewMouseMove += utbUf_PreviewMouseMove;
                    gdUnitArray.Children.Add(utb);

                    aryUnitUf[i, j] = utb;
                }
            }

            for (int i = 0; i < allocInfo.lstUnits.Count; i++)
            {
                for (int j = 0; j < allocInfo.lstUnits[i].Count; j++)
                {
                    UnitInfo ui = allocInfo.lstUnits[i][j];

                    if (ui == null || ui.Enable == false)
                    { continue; }

                    aryUnitUf[ui.X - 1, ui.Y - 1].Content = "C" + ui.ControllerID + "-" + ui.PortNo + "-" + ui.UnitNo;
                    aryUnitUf[ui.X - 1, ui.Y - 1].IsEnabled = true;
                    aryUnitUf[ui.X - 1, ui.Y - 1].UnitInfo = ui;

                    UnitDefaultBrush = aryUnitUf[ui.X - 1, ui.Y - 1].Background;
                }
            }

            // Cell (UF)
            gdCellArray.Children.Clear();
            aryCellUf = new UnitToggleButton[4, 3];

            cellNo = 1;
            for (int i = 0; i < 3; i++) // Y
            {
                for (int j = 0; j < 4; j++) // X
                {
                    UnitToggleButton utb = new UnitToggleButton();
                    utb.Content = "No." + cellNo++;
                    utb.Height = 45;
                    utb.Width = 40;
                    utb.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    utb.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    utb.Margin = new Thickness(j * 40, i * 45, 0, 0);
                    utb.BorderThickness = new Thickness(1);
                    utb.IsEnabled = true;
                    utb.Checked += CellToggleButton_Checked;
                    gdCellArray.Children.Add(utb);

                    aryCellUf[j, i] = utb;
                }
            }

            // ●Unit Allocation (UF Cam)
            gdUfCamUnitArray.Width = allocInfo.MaxX * FIXED_WIDTH; // 64:ToggleBottanの幅
            gdUfCamUnitArray.Height = allocInfo.MaxY * FIXED_HEIGHT; // 36:ToggleBottanの高さ
            gdUfCamUnitFrame.Width = gdUfCamUnitArray.Width;
            gdUfCamUnitFrame.Height = gdUfCamUnitArray.Height;

            aryUnitUfCam = new UnitToggleButton[allocInfo.MaxX, allocInfo.MaxY];

            for (int i = 0; i < allocInfo.MaxX; i++)
            {
                for (int j = 0; j < allocInfo.MaxY; j++)
                {
                    UnitToggleButton utb = new UnitToggleButton();
                    utb.Height = FIXED_HEIGHT;
                    utb.Width = FIXED_WIDTH;
                    utb.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    utb.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    utb.Margin = new Thickness(i * FIXED_WIDTH, j * FIXED_HEIGHT, 0, 0);
                    utb.BorderThickness = new Thickness(1);
                    utb.IsEnabled = false;
                    utb.PreviewMouseDown += utbUfCam_PreviewMouseDown;
                    utb.PreviewMouseUp += utbUfCam_PreviewMouseUp;
                    utb.PreviewMouseMove += utbUfCam_PreviewMouseMove;
                    gdUfCamUnitArray.Children.Add(utb);

                    aryUnitUfCam[i, j] = utb;
                }
            }

            for (int i = 0; i < allocInfo.lstUnits.Count; i++)
            {
                for (int j = 0; j < allocInfo.lstUnits[i].Count; j++)
                {
                    UnitInfo ui = allocInfo.lstUnits[i][j];

                    if (ui == null || ui.Enable == false)
                    { continue; }

                    aryUnitUfCam[ui.X - 1, ui.Y - 1].Content = "C" + ui.ControllerID + "-" + ui.PortNo + "-" + ui.UnitNo;
                    aryUnitUfCam[ui.X - 1, ui.Y - 1].IsEnabled = true;
                    aryUnitUfCam[ui.X - 1, ui.Y - 1].UnitInfo = ui;
                }
            }

            // ●Unit Allocation (Gap Cam)
            gdUnitArrayGapCam.Width = allocInfo.MaxX * FIXED_WIDTH; // 64:ToggleBottanの幅
            gdUnitArrayGapCam.Height = allocInfo.MaxY * FIXED_HEIGHT; // 36:ToggleBottanの高さ
            gdUnitFrameGapCam.Width = gdUnitArrayGapCam.Width;
            gdUnitFrameGapCam.Height = gdUnitArrayGapCam.Height;

            aryUnitGapCam = new UnitToggleButton[allocInfo.MaxX, allocInfo.MaxY];

            for (int i = 0; i < allocInfo.MaxX; i++)
            {
                for (int j = 0; j < allocInfo.MaxY; j++)
                {
                    UnitToggleButton utb = new UnitToggleButton();
                    utb.Height = FIXED_HEIGHT;
                    utb.Width = FIXED_WIDTH;
                    utb.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    utb.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    utb.Margin = new Thickness(i * FIXED_WIDTH, j * FIXED_HEIGHT, 0, 0);
                    utb.BorderThickness = new Thickness(1);
                    utb.IsEnabled = false;
                    utb.PreviewMouseDown += utbGapCam_PreviewMouseDown;
                    utb.PreviewMouseUp += utbGapCam_PreviewMouseUp;
                    utb.PreviewMouseMove += utbGapCam_PreviewMouseMove;
                    gdUnitArrayGapCam.Children.Add(utb);

                    aryUnitGapCam[i, j] = utb;
                }
            }

            for (int i = 0; i < allocInfo.lstUnits.Count; i++)
            {
                for (int j = 0; j < allocInfo.lstUnits[i].Count; j++)
                {
                    UnitInfo ui = allocInfo.lstUnits[i][j];

                    if (ui == null || ui.Enable == false)
                    { continue; }

                    aryUnitGapCam[ui.X - 1, ui.Y - 1].Content = "C" + ui.ControllerID + "-" + ui.PortNo + "-" + ui.UnitNo;
                    aryUnitGapCam[ui.X - 1, ui.Y - 1].IsEnabled = true;
                    aryUnitGapCam[ui.X - 1, ui.Y - 1].UnitInfo = ui;
                }
            }

            // ●Unit Allocation (Info)
            gdUnitArrayInfo.Children.Clear();
            gdUnitArrayInfo.Width = allocInfo.MaxX * FIXED_WIDTH; // 64:ToggleBottanの幅
            gdUnitArrayInfo.Height = allocInfo.MaxY * FIXED_HEIGHT; // 36:ToggleBottanの高さ
            gdUnitFrameInfo.Width = gdUnitArrayInfo.Width;
            gdUnitFrameInfo.Height = gdUnitArrayInfo.Height;

            aryUnitInfo = new UnitToggleButton[allocInfo.MaxX, allocInfo.MaxY];

            for (int i = 0; i < allocInfo.MaxX; i++)
            {
                for (int j = 0; j < allocInfo.MaxY; j++)
                {
                    UnitToggleButton utb = new UnitToggleButton();
                    utb.Height = FIXED_HEIGHT;
                    utb.Width = FIXED_WIDTH;
                    utb.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    utb.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    utb.Margin = new Thickness(i * FIXED_WIDTH, j * FIXED_HEIGHT, 0, 0);
                    utb.BorderThickness = new Thickness(1);
                    utb.IsEnabled = false;
                    utb.Checked += UnitInfoToggleButton_Checked;
                    gdUnitArrayInfo.Children.Add(utb);

                    aryUnitInfo[i, j] = utb;
                }
            }

            for (int i = 0; i < allocInfo.lstUnits.Count; i++)
            {
                for (int j = 0; j < allocInfo.lstUnits[i].Count; j++)
                {
                    UnitInfo ui = allocInfo.lstUnits[i][j];

                    if (ui == null || ui.Enable == false)
                    { continue; }

                    aryUnitInfo[ui.X - 1, ui.Y - 1].Content = "C" + ui.ControllerID + "-" + ui.PortNo + "-" + ui.UnitNo;
                    aryUnitInfo[ui.X - 1, ui.Y - 1].IsEnabled = true;
                    aryUnitInfo[ui.X - 1, ui.Y - 1].UnitInfo = ui;
                }
            }

            // ●Unit Allocation (Measurement)
            gdUnitArrayMeasurement.Children.Clear();
            gdUnitArrayMeasurement.Width = allocInfo.MaxX * FIXED_WIDTH; // 64:ToggleBottanの幅
            gdUnitArrayMeasurement.Height = allocInfo.MaxY * FIXED_HEIGHT; // 36:ToggleBottanの高さ
            gdUnitFrameMeasurement.Width = gdUnitArrayMeasurement.Width;
            gdUnitFrameMeasurement.Height = gdUnitArrayMeasurement.Height;

            aryUnitMeas = new UnitToggleButton[allocInfo.MaxX, allocInfo.MaxY];

            for (int i = 0; i < allocInfo.MaxX; i++)
            {
                for (int j = 0; j < allocInfo.MaxY; j++)
                {
                    UnitToggleButton utb = new UnitToggleButton();
                    utb.Height = FIXED_HEIGHT;
                    utb.Width = FIXED_WIDTH;
                    utb.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    utb.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    utb.Margin = new Thickness(i * FIXED_WIDTH, j * FIXED_HEIGHT, 0, 0);
                    utb.BorderThickness = new Thickness(1);
                    utb.IsEnabled = false;
                    utb.PreviewMouseDown += utbMeas_PreviewMouseDown;
                    utb.PreviewMouseUp += utbMeas_PreviewMouseUp;
                    utb.PreviewMouseMove += utbMeas_PreviewMouseMove;
                    gdUnitArrayMeasurement.Children.Add(utb);

                    aryUnitMeas[i, j] = utb;
                }
            }

            for (int i = 0; i < allocInfo.lstUnits.Count; i++)
            {
                for (int j = 0; j < allocInfo.lstUnits[i].Count; j++)
                {
                    UnitInfo ui = allocInfo.lstUnits[i][j];

                    if (ui == null || ui.Enable == false)
                    { continue; }

                    aryUnitMeas[ui.X - 1, ui.Y - 1].Content = "C" + ui.ControllerID + "-" + ui.PortNo + "-" + ui.UnitNo;
                    aryUnitMeas[ui.X - 1, ui.Y - 1].IsEnabled = true;
                    aryUnitMeas[ui.X - 1, ui.Y - 1].UnitInfo = ui;
                }
            }

            // ●Unit Allocation (Calibration)
            gdUnitArrayCalibration.Children.Clear();
            gdUnitArrayCalibration.Width = allocInfo.MaxX * FIXED_WIDTH; // 64:ToggleBottanの幅
            gdUnitArrayCalibration.Height = allocInfo.MaxY * FIXED_HEIGHT; // 36:ToggleBottanの高さ
            gdUnitFrameCalibration.Width = gdUnitArrayCalibration.Width;
            gdUnitFrameCalibration.Height = gdUnitArrayCalibration.Height;

            aryUnitCalib = new UnitToggleButton[allocInfo.MaxX, allocInfo.MaxY];

            for (int i = 0; i < allocInfo.MaxX; i++)
            {
                for (int j = 0; j < allocInfo.MaxY; j++)
                {
                    UnitToggleButton utb = new UnitToggleButton();
                    utb.Height = FIXED_HEIGHT;
                    utb.Width = FIXED_WIDTH;
                    utb.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    utb.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    utb.Margin = new Thickness(i * FIXED_WIDTH, j * FIXED_HEIGHT, 0, 0);
                    utb.BorderThickness = new Thickness(1);
                    utb.IsEnabled = false;
                    utb.Checked += UnitCalibToggleButton_Checked;
                    gdUnitArrayCalibration.Children.Add(utb);

                    aryUnitCalib[i, j] = utb;
                }
            }

            for (int i = 0; i < allocInfo.lstUnits.Count; i++)
            {
                for (int j = 0; j < allocInfo.lstUnits[i].Count; j++)
                {
                    UnitInfo ui = allocInfo.lstUnits[i][j];

                    if (ui == null || ui.Enable == false)
                    { continue; }

                    aryUnitCalib[ui.X - 1, ui.Y - 1].Content = "C" + ui.ControllerID + "-" + ui.PortNo + "-" + ui.UnitNo;
                    aryUnitCalib[ui.X - 1, ui.Y - 1].IsEnabled = true;
                    aryUnitCalib[ui.X - 1, ui.Y - 1].UnitInfo = ui;
                }
            }

            // ●Unit Allocation (Relative Target)
            gdUnitArrayRelTgt.Children.Clear();
            gdUnitArrayRelTgt.Width = allocInfo.MaxX * FIXED_WIDTH; // 64:ToggleBottanの幅
            gdUnitArrayRelTgt.Height = allocInfo.MaxY * FIXED_HEIGHT; // 36:ToggleBottanの高さ
            gdUnitFrameRelTgt.Width = gdUnitArrayRelTgt.Width;
            gdUnitFrameRelTgt.Height = gdUnitArrayRelTgt.Height;

            aryUnitRelTgt = new UnitToggleButton[allocInfo.MaxX, allocInfo.MaxY];

            for (int i = 0; i < allocInfo.MaxX; i++)
            {
                for (int j = 0; j < allocInfo.MaxY; j++)
                {
                    UnitToggleButton utb = new UnitToggleButton();
                    utb.Height = FIXED_HEIGHT;
                    utb.Width = FIXED_WIDTH;
                    utb.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    utb.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    utb.Margin = new Thickness(i * FIXED_WIDTH, j * FIXED_HEIGHT, 0, 0);
                    utb.BorderThickness = new Thickness(1);
                    utb.IsEnabled = false;
                    utb.PreviewMouseDown += utbRelTgt_PreviewMouseDown;
                    utb.PreviewMouseUp += utbRelTgt_PreviewMouseUp;
                    utb.PreviewMouseMove += utbRelTgt_PreviewMouseMove;
                    gdUnitArrayRelTgt.Children.Add(utb);

                    aryUnitRelTgt[i, j] = utb;
                }
            }

            for (int i = 0; i < allocInfo.lstUnits.Count; i++)
            {
                for (int j = 0; j < allocInfo.lstUnits[i].Count; j++)
                {
                    UnitInfo ui = allocInfo.lstUnits[i][j];

                    if (ui == null || ui.Enable == false)
                    { continue; }

                    aryUnitRelTgt[ui.X - 1, ui.Y - 1].Content = "C" + ui.ControllerID + "-" + ui.PortNo + "-" + ui.UnitNo;
                    aryUnitRelTgt[ui.X - 1, ui.Y - 1].IsEnabled = true;
                    aryUnitRelTgt[ui.X - 1, ui.Y - 1].UnitInfo = ui;
                }
            }

            // Cell
            gdCellArrayRt.Children.Clear();
            aryCellRelTgt = new UnitToggleButton[4, 3];

            cellNo = 1;
            for (int i = 0; i < 3; i++) // Y
            {
                for (int j = 0; j < 4; j++) // X
                {
                    UnitToggleButton utb = new UnitToggleButton();
                    utb.Content = "No." + cellNo++;
                    utb.Height = 45;
                    utb.Width = 40;
                    utb.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    utb.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    utb.Margin = new Thickness(j * 40, i * 45, 0, 0);
                    utb.BorderThickness = new Thickness(1);
                    utb.IsEnabled = true;
                    utb.Checked += btnRtCellToggleButton_Checked;
                    gdCellArrayRt.Children.Add(utb);

                    aryCellRelTgt[j, i] = utb;
                }
            }

            // ●Module Gamma
            gdUnitArrayGg.Children.Clear();
            gdUnitArrayGg.Width = allocInfo.MaxX * FIXED_WIDTH; // 64:ToggleBottanの幅
            gdUnitArrayGg.Height = allocInfo.MaxY * FIXED_HEIGHT; // 36:ToggleBottanの高さ
            gdGgUnitFrame.Width = gdUnitArrayGg.Width;
            gdGgUnitFrame.Height = gdUnitArrayGg.Height;

            aryUnitGg = new UnitToggleButton[allocInfo.MaxX, allocInfo.MaxY];

            for (int i = 0; i < allocInfo.MaxX; i++)
            {
                for (int j = 0; j < allocInfo.MaxY; j++)
                {
                    UnitToggleButton utb = new UnitToggleButton();
                    utb.Height = FIXED_HEIGHT;
                    utb.Width = FIXED_WIDTH;
                    utb.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    utb.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    utb.Margin = new Thickness(i * FIXED_WIDTH, j * FIXED_HEIGHT, 0, 0);
                    utb.BorderThickness = new Thickness(1);
                    utb.IsEnabled = false;
                    utb.PreviewMouseDown += utbGg_PreviewMouseDown;
                    utb.PreviewMouseUp += utbGg_PreviewMouseUp;
                    utb.PreviewMouseMove += utbGg_PreviewMouseMove;
                    gdUnitArrayGg.Children.Add(utb);

                    aryUnitGg[i, j] = utb;
                }
            }

            for (int i = 0; i < allocInfo.lstUnits.Count; i++)
            {
                for (int j = 0; j < allocInfo.lstUnits[i].Count; j++)
                {
                    UnitInfo ui = allocInfo.lstUnits[i][j];

                    if (ui == null || ui.Enable == false)
                    { continue; }

                    aryUnitGg[ui.X - 1, ui.Y - 1].Content = "C" + ui.ControllerID + "-" + ui.PortNo + "-" + ui.UnitNo;
                    aryUnitGg[ui.X - 1, ui.Y - 1].IsEnabled = true;
                    aryUnitGg[ui.X - 1, ui.Y - 1].UnitInfo = ui;

                    UnitDefaultBrush = aryUnitGg[ui.X - 1, ui.Y - 1].Background;
                }
            }

            // Cell
            gdCellArrayGg.Children.Clear();
            aryCellGg = new UnitToggleButton[4, 3];

            cellNo = 1;
            for (int i = 0; i < 3; i++) // Y
            {
                for (int j = 0; j < 4; j++) // X
                {
                    UnitToggleButton utb = new UnitToggleButton();
                    utb.Content = "No." + cellNo++;
                    utb.Height = 45;
                    utb.Width = 40;
                    utb.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    utb.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    utb.Margin = new Thickness(j * 40, i * 45, 0, 0);
                    utb.BorderThickness = new Thickness(1);
                    utb.IsEnabled = true;
                    utb.Checked += btnCellToggleButton_Checked;
                    gdCellArrayGg.Children.Add(utb);

                    aryCellGg[j, i] = utb;
                }
            }

            // ●SIG MODE
            gdSmUnitArray.Children.Clear();
            gdSmUnitArray.Width = allocInfo.MaxX * FIXED_WIDTH; // 64:ToggleBottanの幅
            gdSmUnitArray.Height = allocInfo.MaxY * FIXED_HEIGHT; // 36:ToggleBottanの高さ
            gdSmUnitFrame.Width = gdSmUnitArray.Width;
            gdSmUnitFrame.Height = gdSmUnitArray.Height;

            arySmUnit = new UnitToggleButton[allocInfo.MaxX, allocInfo.MaxY];

            for (int i = 0; i < allocInfo.MaxX; i++)
            {
                for (int j = 0; j < allocInfo.MaxY; j++)
                {
                    UnitToggleButton utb = new UnitToggleButton();
                    utb.Height = FIXED_HEIGHT;
                    utb.Width = FIXED_WIDTH;
                    utb.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    utb.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    utb.Margin = new Thickness(i * FIXED_WIDTH, j * FIXED_HEIGHT, 0, 0);
                    utb.BorderThickness = new Thickness(1);
                    utb.IsEnabled = false;
                    utb.PreviewMouseDown += utbSm_PreviewMouseDown;
                    utb.PreviewMouseUp += utbSm_PreviewMouseUp;
                    utb.PreviewMouseMove += utbSm_PreviewMouseMove;
                    gdSmUnitArray.Children.Add(utb);

                    arySmUnit[i, j] = utb;
                }
            }

            for (int i = 0; i < allocInfo.lstUnits.Count; i++)
            {
                for (int j = 0; j < allocInfo.lstUnits[i].Count; j++)
                {
                    UnitInfo ui = allocInfo.lstUnits[i][j];

                    if (ui == null || ui.Enable == false)
                    { continue; }

                    arySmUnit[ui.X - 1, ui.Y - 1].Content = "C" + ui.ControllerID + "-" + ui.PortNo + "-" + ui.UnitNo;
                    arySmUnit[ui.X - 1, ui.Y - 1].IsEnabled = true;
                    arySmUnit[ui.X - 1, ui.Y - 1].UnitInfo = ui;

                    UnitDefaultBrush = arySmUnit[ui.X - 1, ui.Y - 1].Background;
                }
            }

            // ●Target Cainet (UF Camera)
            cmbxUfCamTgtCabi.Items.Clear();
            for (int y = 0; y < allocInfo.MaxY; y++)
            {
                for (int x = 0; x < allocInfo.MaxX; x++)
                {
                    try
                    {
                        if (allocInfo.lstUnits[x][y] != null)
                        {
                            UnitInfo unit = allocInfo.lstUnits[x][y];
                            cmbxUfCamTgtCabi.Items.Add("C" + unit.ControllerID + "-" + unit.PortNo + "-" + unit.UnitNo);

                        }
                    }
                    catch { } // 無視
                }
            }

            try { cmbxUfCamTgtCabi.SelectedIndex = 0; }
            catch { } // 無視

            // HDCP一覧
            hdcpData = new ObservableCollection<ControllerHDCPInfo>();

            foreach (ControllerInfo cont in dicController.Values)
            {
                var hdcpInfo = new ControllerHDCPInfo(cont.ControllerID, cont.IPAddress);
                hdcpInfo.SetSendSdcpCommand = sendSdcpCommand;
                hdcpInfo.GetSendSdcpCommand = sendSdcpCommand;

                hdcpData.Add(hdcpInfo);
            }

            dgHdcp.ItemsSource = hdcpData;
        }

        private bool getPowerStatus(string ip, out PowerStatus status)
        {
            string buff;

            if (sendSdcpCommand(SDCPClass.CmdControllerPowerStausGet, out buff, ip) != true)
            {
                status = PowerStatus.Unknown;
                return false;
            }

            if(buff == "00")
            { status = PowerStatus.Standby; }
            else if(buff == "01")
            { status = PowerStatus.Power_On; }
            else if (buff == "02")
            { status = PowerStatus.Update; }
            else if(buff == "03")
            { status = PowerStatus.Startup; }
            else if (buff == "04")
            { status = PowerStatus.Shutting_Down; }
            else if (buff == "05")
            { status = PowerStatus.Initializing; }
            else
            {
                status = PowerStatus.Unknown;
                return false;
            }

            return true;
        }

        /// <summary>
        /// コントローラーをPower Onに設定
        /// </summary>
        /// <returns></returns>
        private bool setAllControllerPowerOn(Dictionary<int, ControllerInfo> dic = null)
        {
            Dictionary<int, ControllerInfo> dicCon = dic ?? dicController;
            foreach (ControllerInfo controller in dicCon.Values)
            {
                if (!setControllerPowerOn(controller))
                { return false; }
            }

            foreach (ControllerInfo controller in dicCon.Values)
            {
                DateTime startTime = DateTime.Now;
                while (true)
                {
                    if (!getPowerStatus(controller.IPAddress, out PowerStatus power))
                    {
                        string msg = "Failed get controller power status.";
                        ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                        return false;
                    }

                    if (power == PowerStatus.Power_On)
                    { break; }

                    TimeSpan span = DateTime.Now - startTime;
                    if (span.TotalSeconds > 180) // 3分経過してもPower Onにならなかったら処理中止
                    {
                        string msg = "Failed to power on controller.";
                        ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                        return false;
                    }

                    System.Threading.Thread.Sleep(1000);
                }
            }

            return true;
        }

        /// <summary>
        /// コントローラーをPower Onに設定
        /// </summary>
        /// <param name="controller">対象コントローラー</param>
        /// <returns></returns>
        private bool setControllerPowerOn(ControllerInfo controller)
        {
            if (!getPowerStatus(controller.IPAddress, out PowerStatus power))
            {
                string msg = "Failed get controller power status.";
                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }

            if (power == PowerStatus.Standby)
            {
                if (!sendSdcpCommand(SDCPClass.CmdPowerUp, SDCP_COMMAND_WAIT_TIME, controller.IPAddress))
                {
                    string msg = "Failed to power on controller.";
                    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }
            }
            else if (power == PowerStatus.Shutting_Down)
            {
                DateTime startTime = DateTime.Now;
                while (true)
                {
                    if (!getPowerStatus(controller.IPAddress, out power))
                    {
                        string msg = "Failed get controller power status.";
                        ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                        return false;
                    }

                    if (power == PowerStatus.Standby)
                    {
                        if (!sendSdcpCommand(SDCPClass.CmdPowerUp, SDCP_COMMAND_WAIT_TIME, controller.IPAddress))
                        {
                            string msg = "Failed to power on controller.";
                            ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                            return false;
                        }

                        break;
                    }

                    TimeSpan span = DateTime.Now - startTime;
                    if (span.TotalSeconds > 180) // 3分経過してもStandbyにならなかったら処理中止
                    {
                        string msg = "Failed to power on controller.";
                        ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                        return false;
                    }

                    System.Threading.Thread.Sleep(1000);
                }
            }
            else if (power == PowerStatus.Update)
            {
                string msg = "Controller is updating.";
                ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                return false;
            }
            else // power == PowerStatus.Startup || Power == PowerStatus.Power_On
            {
                // Do nothing
            }

            return true;
        }

        private bool getLedModel(string ip, string pw, out string newLedModel)
        {
            string res;
            bool result = true;

            ADCPClass.sendAdcpCommand(ADCPClass.CmdLedModelGet, out res, ip, pw);

            if (res != ADCP_ERROR_COMMAND)
            {
                switch (res)
                {
                    case ADCPClass.ZRD_B12A:
                        newLedModel = ZRD_B12A;
                        break;
                    case ADCPClass.ZRD_B15A:
                        newLedModel = ZRD_B15A;
                        break;
                    case ADCPClass.ZRD_C12A:
                        newLedModel = ZRD_C12A;
                        break;
                    case ADCPClass.ZRD_C15A:
                        newLedModel = ZRD_C15A;
                        break;
                    case ADCPClass.ZRD_BH12D:
                        newLedModel = ZRD_BH12D;
                        break;
                    case ADCPClass.ZRD_BH15D:
                        newLedModel = ZRD_BH15D;
                        break;
                    case ADCPClass.ZRD_CH12D:
                        newLedModel = ZRD_CH12D;
                        break;
                    case ADCPClass.ZRD_CH15D:
                        newLedModel = ZRD_CH15D;
                        break;
                    case ADCPClass.ZRD_BH12D_S3:
                        newLedModel = ZRD_BH12D_S3;
                        break;
                    case ADCPClass.ZRD_BH15D_S3:
                        newLedModel = ZRD_BH15D_S3;
                        break;
                    case ADCPClass.ZRD_CH12D_S3:
                        newLedModel = ZRD_CH12D_S3;
                        break;
                    case ADCPClass.ZRD_CH15D_S3:
                        newLedModel = ZRD_CH15D_S3;
                        break;
                    default:
                    newLedModel = "-";
                    result = false;
                        break;
                }
            }
            else
            {
                ADCPClass.sendAdcpCommand(ADCPClass.CmdLedPitchGet, out res, ip, pw);

                if (res == ADCPClass.LED_PITCH1_2)
                { newLedModel = ZRD_B12A; }
                else if (res == ADCPClass.LED_PITCH1_5)
                { newLedModel = ZRD_B15A; }
                else
                {
                    newLedModel = "-";
                    result = false;
                }
            }

            return result;
        }

        private void initLedModel()
        {
            initConfiguration();
            loadConfigurationOfLedModel(allocInfo.LEDModel);
            initUniformity();
            initRelativeTarget();

            if (appliMode == ApplicationMode.Service)
            {
                initSmCheckBoxVisibility();
                initSigMode();
            }

            switch (allocInfo.LEDModel)
            {
                case ZRD_B12A:
                    rbChromZRDB12A.IsChecked = true;
                    break;
                case ZRD_B15A:
                    rbChromZRDB15A.IsChecked = true;
                    break;
                case ZRD_C12A:
                    rbChromZRDC12A.IsChecked = true;
                    break;
                case ZRD_C15A:
                    rbChromZRDC15A.IsChecked = true;
                    break;
                case ZRD_BH12D:
                    rbChromZRDBH12D.IsChecked = true;
                    break;
                case ZRD_BH15D:
                    rbChromZRDBH15D.IsChecked = true;
                    break;
                case ZRD_CH12D:
                    rbChromZRDCH12D.IsChecked = true;
                    break;
                case ZRD_CH15D:
                    rbChromZRDCH15D.IsChecked = true;
                    break;
                case ZRD_BH12D_S3:
                    rbChromZRDBH12D_S3.IsChecked = true;
                    break;
                case ZRD_BH15D_S3:
                    rbChromZRDBH15D_S3.IsChecked = true;
                    break;
                case ZRD_CH12D_S3:
                    rbChromZRDCH12D_S3.IsChecked = true;
                    break;
                case ZRD_CH15D_S3:
                    rbChromZRDCH15D_S3.IsChecked = true;
                    break;
            }

            txbConfigDist.Text = camDist.ToString();
            Settings.Ins.CameraDist = camDist;

            tiConfiguration.IsSelected = true;
        }

        private void initConfiguration()
        {
            Settings.Ins.ConfigChromType = ConfigChrom.NA;

            rbChromZRDB12A.IsChecked = false;
            rbChromZRDB15A.IsChecked = false;
            rbChromZRDC12A.IsChecked = false;
            rbChromZRDC15A.IsChecked = false;
            rbChromZRDBH12D.IsChecked = false;
            rbChromZRDBH15D.IsChecked = false;
            rbChromZRDCH12D.IsChecked = false;
            rbChromZRDCH15D.IsChecked = false;
            rbChromZRDBH12D_S3.IsChecked = false;
            rbChromZRDBH15D_S3.IsChecked = false;
            rbChromZRDCH12D_S3.IsChecked = false;
            rbChromZRDCH15D_S3.IsChecked = false;
        }

        private void initTestPatternParam()
        {
            lstTestPatternParam = new List<TestPatternParam>();

            TestPatternParam param = new TestPatternParam();

            // Raster
            param.R_ON = true;
            param.G_ON = true;
            param.B_ON = true;
            param.R_FG_LV = brightness._20pc;
            param.G_FG_LV = brightness._20pc;
            param.B_FG_LV = brightness._20pc;
            param.R_BG_LV = 0;
            param.G_BG_LV = 0;
            param.B_BG_LV = 0;
            param.H_ST_POS = 0;
            param.V_ST_POS = 0;
            param.H_WIDTH = 0;
            param.V_WIDTH = 0;
            param.H_PITCH = 0;
            param.V_PITCH = 0;
            param.INVERT = false;

            lstTestPatternParam.Add(param);

            // Tile
            param = new TestPatternParam();
            
            param.R_ON = true;
            param.G_ON = true;
            param.B_ON = true;
            param.R_FG_LV = brightness._50pc;
            param.G_FG_LV = brightness._50pc;
            param.B_FG_LV = brightness._50pc;
            param.R_BG_LV = 0;
            param.G_BG_LV = 0;
            param.B_BG_LV = 0;
            param.H_ST_POS = 60;
            param.V_ST_POS = 60;
            param.H_WIDTH = 120;
            param.V_WIDTH = 120;
            param.H_PITCH = 240;
            param.V_PITCH = 240;
            param.INVERT = false;
            
            lstTestPatternParam.Add(param);

            // Checker
            param = new TestPatternParam();

            param.R_ON = true;
            param.G_ON = true;
            param.B_ON = true;
            param.R_FG_LV = brightness._50pc;
            param.G_FG_LV = brightness._50pc;
            param.B_FG_LV = brightness._50pc;
            param.R_BG_LV = 0;
            param.G_BG_LV = 0;
            param.B_BG_LV = 0;
            param.H_ST_POS = 0;
            param.V_ST_POS = 0;
            param.H_WIDTH = 120;
            param.V_WIDTH = 120;
            param.H_PITCH = 0;
            param.V_PITCH = 0;
            param.INVERT = false;

            lstTestPatternParam.Add(param);

            // Cross
            param = new TestPatternParam();

            param.R_ON = true;
            param.G_ON = true;
            param.B_ON = true;
            param.R_FG_LV = brightness._50pc;
            param.G_FG_LV = brightness._50pc;
            param.B_FG_LV = brightness._50pc;
            param.R_BG_LV = 0;
            param.G_BG_LV = 0;
            param.B_BG_LV = 0;
            param.H_ST_POS = 1919;
            param.V_ST_POS = 1079;
            param.H_WIDTH = 1;
            param.V_WIDTH = 1;
            param.H_PITCH = 0;
            param.V_PITCH = 0;
            param.INVERT = false;

            lstTestPatternParam.Add(param);

            // H Ramp(INC)
            param = new TestPatternParam();

            param.R_ON = true;
            param.G_ON = true;
            param.B_ON = true;
            param.R_FG_LV = 0;
            param.G_FG_LV = 0;
            param.B_FG_LV = 0;
            param.R_BG_LV = 1;
            param.G_BG_LV = 1;
            param.B_BG_LV = 1;
            param.H_ST_POS = 0;
            param.V_ST_POS = 0;
            param.H_WIDTH = 4;
            param.V_WIDTH = 0;
            param.H_PITCH = 0;
            param.V_PITCH = 0;
            param.INVERT = false;

            lstTestPatternParam.Add(param);

            // H Ramp(DEC)
            param = new TestPatternParam();

            param.R_ON = true;
            param.G_ON = true;
            param.B_ON = true;
            param.R_FG_LV = 64;
            param.G_FG_LV = 64;
            param.B_FG_LV = 64;
            param.R_BG_LV = 1;
            param.G_BG_LV = 1;
            param.B_BG_LV = 1;
            param.H_ST_POS = 0;
            param.V_ST_POS = 0;
            param.H_WIDTH = 4;
            param.V_WIDTH = 0;
            param.H_PITCH = 0;
            param.V_PITCH = 0;
            param.INVERT = true;

            lstTestPatternParam.Add(param);

            // V Ramp(INC)
            param = new TestPatternParam();

            param.R_ON = true;
            param.G_ON = true;
            param.B_ON = true;
            param.R_FG_LV = 0;
            param.G_FG_LV = 0;
            param.B_FG_LV = 0;
            param.R_BG_LV = 1;
            param.G_BG_LV = 1;
            param.B_BG_LV = 1;
            param.H_ST_POS = 0;
            param.V_ST_POS = 0;
            param.H_WIDTH = 0;
            param.V_WIDTH = 2;
            param.H_PITCH = 0;
            param.V_PITCH = 0;
            param.INVERT = false;

            lstTestPatternParam.Add(param);

            // V Ramp(DEC)
            param = new TestPatternParam();

            param.R_ON = true;
            param.G_ON = true;
            param.B_ON = true;
            param.R_FG_LV = 0;
            param.G_FG_LV = 0;
            param.B_FG_LV = 0;
            param.R_BG_LV = 1;
            param.G_BG_LV = 1;
            param.B_BG_LV = 1;
            param.H_ST_POS = 0;
            param.V_ST_POS = 72;
            param.H_WIDTH = 0;
            param.V_WIDTH = 2;
            param.H_PITCH = 0;
            param.V_PITCH = 0;
            param.INVERT = true;

            lstTestPatternParam.Add(param);

            // Color Bar
            param = new TestPatternParam();

            param.R_ON = true;
            param.G_ON = true;
            param.B_ON = true;
            param.R_FG_LV = brightness._22pc;
            param.G_FG_LV = brightness._22pc;
            param.B_FG_LV = brightness._22pc;
            param.R_BG_LV = 0;
            param.G_BG_LV = 0;
            param.B_BG_LV = 0;
            param.H_ST_POS = 0;
            param.V_ST_POS = 0;
            param.H_WIDTH = 480;
            param.V_WIDTH = 0;
            param.H_PITCH = 0;
            param.V_PITCH = 0;
            param.INVERT = false;

            lstTestPatternParam.Add(param);

            // Window
            param = new TestPatternParam();

            param.R_ON = true;
            param.G_ON = true;
            param.B_ON = true;
            param.R_FG_LV = brightness._22pc;
            param.G_FG_LV = brightness._22pc;
            param.B_FG_LV = brightness._22pc;
            param.R_BG_LV = 0;
            param.G_BG_LV = 0;
            param.B_BG_LV = 0;
            param.H_ST_POS = 1280;
            param.V_ST_POS = 720;
            param.H_WIDTH = 1280;
            param.V_WIDTH = 720;
            param.H_PITCH = 0;
            param.V_PITCH = 0;
            param.INVERT = false;

            lstTestPatternParam.Add(param);

            // Hatch Cabinet
            param = new TestPatternParam();

            param.R_ON = true;
            param.G_ON = true;
            param.B_ON = true;
            param.R_FG_LV = brightness._20pc;
            param.G_FG_LV = brightness._20pc;
            param.B_FG_LV = brightness._20pc;
            param.R_BG_LV = 0;
            param.G_BG_LV = 0;
            param.B_BG_LV = 0;
            param.H_ST_POS = 1;
            param.V_ST_POS = 1;

            param.H_WIDTH = cabiDx - 2;
            param.V_WIDTH = cabiDy - 2;
            param.H_PITCH = cabiDx;
            param.V_PITCH = cabiDy;
            param.INVERT = false;

            lstTestPatternParam.Add(param);

            // Hatch Module
            param = new TestPatternParam();

            param.R_ON = true;
            param.G_ON = true;
            param.B_ON = true;
            param.R_FG_LV = brightness._20pc;
            param.G_FG_LV = brightness._20pc;
            param.B_FG_LV = brightness._20pc;
            param.R_BG_LV = 0;
            param.G_BG_LV = 0;
            param.B_BG_LV = 0;
            param.H_ST_POS = 1;
            param.V_ST_POS = 1;
            
            param.H_WIDTH = modDx - 2;
            param.V_WIDTH = modDy - 2;
            param.H_PITCH = modDx;
            param.V_PITCH = modDy;
            param.INVERT = false;

            lstTestPatternParam.Add(param);
        }

        private void initUniformity()
        {
            initUniformityTargetMode();
            initUniformityAdjustmentMode();
        }

        private void initUniformityAdjustmentMode()
        {
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            { rbStandard.IsChecked = true; }
            else
            { rbStrict.IsChecked = true; }
        }

        private void initUniformityTargetMode()
        {
            Settings.Ins.Uniformity.IsRelativeTarget = false;
            Settings.Ins.Uniformity.ModeOption = ModeOption.Relative;
            cmbxTargetMode.SelectedIndex = 1;
        }

        private void initUniformityCameraAdjustmentMode()
        {
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            { rbUfCam9pt.IsChecked = true; }
            else
            { rbUfCamEachMod.IsChecked = true; }
        }

        private void initRelativeTarget()
        {
            // GUIの初期化
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            {
                lbRtModuleAllocation.Visibility = Visibility.Collapsed;
                gdRtCellFrame.Visibility = Visibility.Collapsed;
                gdAllocRelativeTargetLayout.Margin = new Thickness(10, 26, 10, 12);
            }
            else
            {
                lbRtModuleAllocation.Visibility = Visibility.Visible;
                gdRtCellFrame.Visibility = Visibility.Visible;
                gdAllocRelativeTargetLayout.Margin = new Thickness(10, 26, 195, 12);
            }
        }

        private void initGammaGain()
        {
            tiModuleGamma.Visibility = Visibility.Visible;

            // Adjustment Mode
            if (Settings.Ins.GammaGain.GainAdjustmentMode == GainAdjustmentMode.EachModule)
            { rbGgEachModule.IsChecked = true; }
            else
            { rbGgAllModule.IsChecked = true; }

            // Data Writing Option
            if (Settings.Ins.GammaGain.GainDataWritingOption == GainDataWritingOption.NotAdjustedModules)
            { rbWriteNotAdjustedModules.IsChecked = true; }
            else
            { rbWriteAllModules.IsChecked = true; }

            // Reference Cell Position
            if (Settings.Ins.GammaGain.GainReferenceCellPosition == GainReferenceCellPosition.Bottom)
            { rbRefPostionBottom.IsChecked = true; }
            else if (Settings.Ins.GammaGain.GainReferenceCellPosition == GainReferenceCellPosition.Left)
            { rbRefPostionLeft.IsChecked = true; }
            else if (Settings.Ins.GammaGain.GainReferenceCellPosition == GainReferenceCellPosition.Right)
            { rbRefPostionRight.IsChecked = true; }
            else
            { rbRefPostionTop.IsChecked = true; }

            rbRemoteOffGain.Checked += rbRemoteOff_Checked;

            cbLcData.IsChecked = true; // Backup
            cbLcDataRestore.IsChecked = true; // Restore
        }

        private void initSigMode()
        {
            initSmCheckBoxIsChecked(false);
            initSmCheckBoxIsEnabled(false);
            initSmCheckBoxIsThreeState(true);
            initSmButtonIsEnabled(false);
        }

        private int countUnits()
        {
            int count = 0;

            foreach (ControllerInfo info in dicController.Values)
            { count += info.UnitCount; }

            return count;
        }

        private void initAllUnitToggleButton()
        {
            // Cabinet
            for (int i = 0; i < allocInfo.MaxX; i++)
            {
                for (int j = 0; j < allocInfo.MaxY; j++)
                {
                    aryUnitData[i, j].IsChecked = false;
                    aryUnitUf[i, j].IsChecked = false;
                    aryUnitInfo[i, j].IsChecked = false;
                    aryUnitMeas[i, j].IsChecked = false;
                    aryUnitCalib[i, j].IsChecked = false;
                    aryUnitRelTgt[i, j].IsChecked = false;
                }
            }

            // Module
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    aryCellData[i, j].IsChecked = false;
                    aryCellUf[i, j].IsChecked = false;
                }
            }
        }

        private void initCursorXYPostion()
        {
            // Uniformity(Manual)
            cursorUnitX = 0;
            cursorUnitY = 0;
            cursorCellX = 0;
            cursorCellY = 0;

            // Gap Correct(Module)
            cursorGapCellUnitX = 0;
            cursorGapCellUnitY = 0;
            cursorGapCellCellX = 0;
            cursorGapCellCellY = 0;
        }

        /// <summary>
        /// モデルを判定
        /// </summary>
        /// <param name="path">読み込むプロファイルのパス</param>
        private void determineProfileType(string path)
        {
            using (StreamReader sr = new StreamReader(path, new UTF8Encoding(false)))
            {
                string line1 = sr.ReadLine();
                string line2 = sr.ReadLine();
                string line3 = sr.ReadLine();
                string line4 = sr.ReadLine();

                if (!string.IsNullOrEmpty(line2) && line2.Contains("<Profile>"))
                { profileType = ProfileType.DCS; }
                else if (!string.IsNullOrEmpty(line2) && line2.Contains("<ConfigurationInfo xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/CAS\">"))
                { profileType = ProfileType.CAS; }
                else
                { profileType = ProfileType.NA; }
            }
        }

        /// <summary>
        /// プロファイルをチェック
        /// </summary>
        /// <param name="path">読み込むプロファイルのパス</param>
        private bool isValidProfile(string path)
        {
            if (profileType == ProfileType.DCS)
            {
                DCSProfileXmlToObjectConvert(path);

                if (!isValidDCSProfile())
                { return false; }
            }
            else if (profileType == ProfileType.CAS)
            {
                CASProfileXmlToObjectConvert(path);

                if (!isValidCASProfile())
                { return false; }
            }
            else
            { return false; }

            return true;
        }

        /// <summary>
        ///XMLデータをObjectデータに逆シリアル化
        /// </summary>
        /// <param name="path">読み込むプロファイルのパス</param>
        private void DCSProfileXmlToObjectConvert(string path)
        {
            //XmlSerializerオブジェクトを作成
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(Profile));

            //読み込むファイルを開く
            using (StreamReader sr = new StreamReader(path, new UTF8Encoding(false)))
            {
                //XMLファイルから読み込み、逆シリアル化する
                DCSProfileData = (Profile)serializer.Deserialize(sr);
            }
        }

        /// <summary>
        /// プロファイルの妥当性チェック
        /// </summary>
        private bool isValidDCSProfile()
        {
            if (DCSProfileData == null)
            { return false; }

            if (DCSProfileData.Controllers == null)
            { return false; }

            if (DCSProfileData.Controllers.Controller.Count == INT_ZERO)
            { return false; }

            foreach (Controller controller in DCSProfileData.Controllers.Controller)
            {
                if (string.IsNullOrEmpty(controller.Connection))
                { return false; }

                if (controller.Units == null)
                { return false; }

                if (string.IsNullOrEmpty(controller.Units.LedModel))
                {
                    if (string.IsNullOrEmpty(controller.Units.LedPitch))
                    { return false; }
                }

                //if (LedModelTypeUpperStringToEnumConverter.Convert(controller.Units.LedModel) == null)
                //{ return false; }
            }

            return true;
        }

        // added by Hotta 2024/01/10
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SetActiveWindow(IntPtr hWnd);

        private void Window_Activated(object sender, EventArgs e)
        {
            IntPtr hwd = IntPtr.Zero;

            Process p = Process.GetCurrentProcess();
            hwd = p.MainWindowHandle;

            if (hwd != IntPtr.Zero)
            {
                // アクティブに設定
                SetActiveWindow(hwd);
            }
        }

        //

        /// <summary>
        /// DCSのProfileから内部データにController情報とUnit Allocation情報を格納する
        /// </summary>
        private bool loadDCSProfileXML()
        {
            // ◆Controller情報
            Dictionary<int, ControllerInfo> _dicController = new Dictionary<int, ControllerInfo>();
            AllocationInfo _allocInfo = new AllocationInfo();
            foreach (Controller item in DCSProfileData.Controllers.Controller)
            {
                ControllerInfo info = new ControllerInfo();
                info.ControllerID = Convert.ToInt32(item.Id);
                info.IPAddress = item.Connection;

                if (item.IsMaster == "true")
                {
                    info.Master = true;
                    _allocInfo.LEDModel = validLedModel(item.Units.LedModel, item.Units.LedPitch);
                }
                else
                { info.Master = false; }

                try { _dicController.Add(info.ControllerID, info); }
                catch
                {
                    string msg = "Controller ID is repeating.";
                    ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 300, 180);
                    return false;
                }
            }

            // ◆Unit Allocation情報
            int x = 0, y = 0;
            foreach (Controller cont in DCSProfileData.Controllers.Controller)
            {
                // 各ControllerのUnit Count
                foreach (ControllerInfo info in _dicController.Values)
                {
                    if (info.ControllerID.ToString() == cont.Id)
                    {
                        info.UnitCount = cont.Units.Unit.Count;
                        break;
                    }
                }

                // X, Yの最大値検索
                foreach (Unit unit in cont.Units.Unit)
                {
                    if (Convert.ToInt32(unit.X) > x)
                    { x = Convert.ToInt32(unit.X); }

                    if (Convert.ToInt32(unit.Y) > y)
                    { y = Convert.ToInt32(unit.Y); }
                }
            }

            _allocInfo.MaxX = x;
            _allocInfo.MaxY = y;

            // Allocation情報を初期化
            for (int i = 0; i < _allocInfo.MaxX; i++)
            {
                _allocInfo.lstUnits.Add(new List<UnitInfo>());

                for (int j = 0; j < _allocInfo.MaxY; j++)
                {
                    UnitInfo unit = null; //new UnitInfo();
                    _allocInfo.lstUnits[i].Add(unit);
                }
            }

            foreach (Controller cont in DCSProfileData.Controllers.Controller)
            {
                foreach (Unit unit in cont.Units.Unit)
                {
                    UnitInfo info = new UnitInfo();

                    info.Enable = true;
                    info.ControllerID = Convert.ToInt32(cont.Id);
                    info.PortNo = Convert.ToInt32(unit.Port);
                    info.UnitNo = Convert.ToInt32(unit.UnitNo);
                    info.X = Convert.ToInt32(unit.X);
                    info.Y = Convert.ToInt32(unit.Y);
                    info.PixelX = Convert.ToInt32(unit.PixelX);
                    info.PixelY = Convert.ToInt32(unit.PixelY);
                    info.CellDataFile = "u" + info.PortNo.ToString("D2") + info.UnitNo.ToString("D2") + "_cd.bin";
                    info.AgingDataFile = "u" + info.PortNo.ToString("D2") + info.UnitNo.ToString("D2") + "_ad.bin";

                    _allocInfo.lstUnits[info.X - 1][info.Y - 1] = info;
                }
            }

            allocInfo = _allocInfo;
            dicController = _dicController;

            return true;
        }

        /// <summary>
        /// XMLデータをObjectデータに逆シリアル化
        /// </summary>
        /// <param name="path">読み込むプロファイルのパス</param>
        private void CASProfileXmlToObjectConvert(string path)
        {
            //DataContractSerializerオブジェクトを作成
            DataContractSerializer serializer = new DataContractSerializer(typeof(ConfigurationInfo));

            //読み込むファイルを開く
            using (XmlReader xr = XmlReader.Create(path))
            {
                //XMLファイルから読み込み、逆シリアル化する
                CASProfileData = (ConfigurationInfo)serializer.ReadObject(xr);
            }
        }

        /// <summary>
        /// CASプロファイルの妥当性チェック
        /// </summary>
        private bool isValidCASProfile()
        {
            if (CASProfileData == null)
            { return false; }

            if (CASProfileData.dicController == null)
            { return false; }

            if (CASProfileData.dicController.Count == INT_ZERO)
            { return false; }

            if (CASProfileData.allocInfo == null)
            { return false; }

            if (CASProfileData.allocInfo.lstUnits.Count == INT_ZERO)
            { return false; }

            if (string.IsNullOrEmpty(CASProfileData.allocInfo.LEDModel))
            { return false; }

            return true;
        }

        /// <summary>
        /// CASのProfileから内部データにController情報とUnit Allocation情報を格納する
        /// </summary>
        private bool loadCASProfileXML()
        {
            try
            {
                //TargetModel = CASProfileData.Model;
                dicController = CASProfileData.dicController;
                allocInfo = CASProfileData.allocInfo;

                allocInfo.LEDModel = validLedModel(allocInfo.LEDModel, allocInfo.LedPitch);
            }
            catch (Exception ex)
            {
                ShowMessageWindow(ex.Message, "Exception!", System.Drawing.SystemIcons.Error, 500, 210);
                return false;
            }

            return true;
        }

        private void ContextMenu_DeviceUniqueIDCopy_Click(object sender, RoutedEventArgs e)
        {
            // 選択中のデータを取得
            if (dgHdcp.SelectedCells.Count > 0)
            {
                var info = dgHdcp.SelectedCells[0];
                var item = info.Item as ControllerHDCPInfo;
                if (item != null)
                { Clipboard.SetText(item.DeviceUniqueID); }
            }
        } 
        
        static double standardDeviation(IEnumerable<double> sequence)
        {
            double result = 0;

            if (sequence.Any())
            {
                double average = sequence.Average();
                double sum = sequence.Sum(d => Math.Pow(d - average, 2));
                result = Math.Sqrt((sum) / (sequence.Count() - 1));
            }
            return result;
        }

        private string CalcSha256Hash(string file)
        {
            StringBuilder hashStr = new StringBuilder();

            using (FileStream filestream = new FileStream(file, FileMode.Open))
            {
                // SHA256ハッシュを計算
                using (var csp = new SHA256CryptoServiceProvider())
                {
                    var hashBytes = csp.ComputeHash(filestream);

                    // バイト配列を文字列に変換
                    foreach (var hashByte in hashBytes)
                    { hashStr.Append(hashByte.ToString("x2")); }
                }
            }

            return hashStr.ToString();
        }

        #endregion MISC

        #region MessageWindow Invoker

        //private bool showMessageWindow(out bool? result)
        //{
        //    WindowMessage winMessage = new WindowMessage("Please Change UC Board.", "UC Board Replace", "Done");
        //    result = winMessage.ShowDialog();

        //    return true;
        //}

        //private bool showMessageWindow(out bool? result, string msg, string caption)
        //{
        //    WindowMessage winMessage = new WindowMessage(msg, caption, "Done");
        //    result = winMessage.ShowDialog();

        //    return true;
        //}

        private bool showMessageWindow(out bool? result, string msg, string caption, string button)
        {
            var dispatcher = Application.Current.Dispatcher;
            bool? tempResult = false;

            dispatcher.Invoke(() =>
            {
                WindowMessage winMessage = new WindowMessage(msg, caption, button);
                tempResult = winMessage.ShowDialog();
            });

            result = tempResult;

            return true;
        }

        // Error表示用
        public static bool ShowMessageWindow(string msg, string caption, System.Drawing.Icon icon, int width, int height)
        {
            var dispatcher = Application.Current.Dispatcher;
            bool? tempResult = false;

            dispatcher.Invoke(() =>
            {
                WindowMessage winMessage = new WindowMessage(msg, caption, icon);
                winMessage.ChangeWindowSize(width, height);
                
                tempResult = winMessage.ShowDialog();
            });

            return true;
        }

        private static bool showMessageWindow(out bool? result, string msg, string caption, System.Drawing.Icon icon, int width, int height, string buttonContent)
        {
            var dispatcher = Application.Current.Dispatcher;
            bool? tempResult = false;

            dispatcher.Invoke(() =>
            {
                WindowMessage winMessage = new WindowMessage(msg, caption, icon, buttonContent);
                winMessage.ChangeWindowSize(width, height);
                winMessage.AddButton();

                tempResult = winMessage.ShowDialog();
            });

            result = tempResult;

            return true;
        }

        public static bool ShowMessageWindow(out bool? result, string msg, string caption, System.Drawing.Icon icon, int width, int height, string buttonContent)
        {
            return showMessageWindow(out result, msg, caption, icon, width, height, buttonContent);
        }

        public bool showMessageWindow(out bool? result, string msg, string caption, System.Drawing.Icon icon, int width, int height, string buttonContent, string button2Content)
        {
            var dispatcher = Application.Current.Dispatcher;
            bool? tempResult = false;

            dispatcher.Invoke(() =>
            {
                WindowMessage winMessage = new WindowMessage(msg, caption, icon, buttonContent);
                winMessage.ChangeWindowSize(width, height);
                winMessage.AddButton(button2Content);

                tempResult = winMessage.ShowDialog();
            });

            result = tempResult;

            return true;
        }

        #endregion MessageWindow Invoker

        #endregion Common

        #endregion Private Methods
    }
}
