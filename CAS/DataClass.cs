using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using System.Management;

using CameraDataClass;
using System.Xml.XPath;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using OpenCvSharp.Blob;
using System.Runtime.InteropServices;

namespace CAS
{
    using CabinetOffset = System.Drawing.Point;
    using WidthOffset = System.Drawing.Point;

    #region Class

    public static class Defined
    {
        // UF Target Default(CLED)
        //public const double m_Target_xr = 0.687;
        //public const double m_Target_yr = 0.306;
        //public const double m_Target_xg = 0.218;
        //public const double m_Target_yg = 0.666;
        //public const double m_Target_xb = 0.145;//0.151 TS // 0.145 PP以降
        //public const double m_Target_yb = 0.054;//0.041 TS // 0.054 PP以降
        //public const double m_Target_Yw = 204.0;
        //public const double m_Target_xw = 0.285;
        //public const double m_Target_yw = 0.294;

        // Chiron DVT
        public const double m_Target_xr = 0.691;
        public const double m_Target_yr = 0.308;
        public const double m_Target_xg = 0.185;
        public const double m_Target_yg = 0.722;
        public const double m_Target_xb = 0.140;
        public const double m_Target_yb = 0.058;
        public const double m_Target_Yw = 167.006; // DVT用、167.006が量産のターゲット
        public const double m_Target_xw = 0.292;
        public const double m_Target_yw = 0.297;

        // Chiron用
        public const int Max_UnitNo_P12 = 6;
        public const int Max_UnitNo_P15 = 9;
        public const int Max_Unit_Count_P12 = 72;
        public const int Max_Unit_Count_P15 = 108;

        //public const string ledPitch1_2 = "1.2";
        //public const string ledPitch1_5 = "1.5";
        public const string password = "Adm1nS0ny";
    }

    [Serializable()]
    public class Settings
    {
        public static string ErrorMessage;

        /// <summary>
        /// Window Start Position
        /// </summary>
        public int Window_X_Position;
        public int Window_Y_Position;
        public int Window_Height;
        public int Window_Width;

        /// <summary>
        /// CA-410 Setting
        /// </summary>
        public int Channel_ModelA;
        public int Channel_ModelD;
        public int Channel_Custom;
        public int InitialRetry;
        public int NoOfProbes;
        public int PortVal;
        public int DarkErrorEnable;
        public string ComToolPath;

        /// <summary>
        /// Exec Settings
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        public string Eulafile;
        [System.Xml.Serialization.XmlIgnore]
        public bool eulaAgree;

        public string LastProfile;
        public int SdcpWaitTime;
        [System.Xml.Serialization.XmlIgnore]
        public bool SdcpLog;
        [System.Xml.Serialization.XmlIgnore]
        public bool ExecLog;
        public int IntSignalWait;
        public int NfsWait;
        public int PowerOnWait;
        public int FtpRetryCount;

        public MtgtAddCtc MtgtAddCtc;

        [System.Xml.Serialization.XmlElement("LedModuleConfiguration")]
        public String LEDModuleConfigurationProperty
        {
            get { return LedModuleConfiguration.ToString(); }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    try
                    { LedModuleConfiguration = (LEDModuleConfigurations)Enum.Parse(typeof(LEDModuleConfigurations), value); }
                    catch { }
                }
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public LEDModuleConfigurations LedModuleConfiguration;

        [System.Xml.Serialization.XmlElement("ConfigChromType")]
        public String ConfigChromTypeProperty
        {
            get { return ConfigChromType.ToString(); }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    try
                    { ConfigChromType = (ConfigChrom)Enum.Parse(typeof(ConfigChrom), value); }
                    catch { }
                }
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public ConfigChrom ConfigChromType;

        public ChromCustom ZRDB12ATarget = null;
        public ChromCustom ZRDB15ATarget = null;
        public ChromCustom ZRDC12ATarget = null;
        public ChromCustom ZRDC15ATarget = null;
        public ChromCustom ZRDBH12DTarget = null;
        public ChromCustom ZRDBH15DTarget = null;
        public ChromCustom ZRDCH12DTarget = null;
        public ChromCustom ZRDCH15DTarget = null;
        public ChromCustom ZRDBH12DS3Target = null;
        public ChromCustom ZRDBH15DS3Target = null;
        public ChromCustom ZRDCH12DS3Target = null;
        public ChromCustom ZRDCH15DS3Target = null;
        public ChromCustom CustomTarget = null;
        public PanelType PanelType;
        public Gamma Gamma;
        public bool ReadIndividualUnits;

        public TransType TransType;

        public GammaGain GammaGain;

        // Relative Targetの設定
        public ChromCustom RelativeTarget;
        public Uniformity Uniformity;

        public DefaultCtCHighColor DefaultCtCHighColor;

        // Meas Alertの設定
        public bool MeasAlert;
        public double xyThresh;
        public double YThresh;

        // Wall形状ファイル(CSV)
        public WallForm Form; // Cabinet Wall形状
        public string WallFormFile;
        public double CameraDist = 4000;
        public CameraInstallPos CameraInstPos; // カメラ設置位置
        public double WallBottomH = 0;
        public double CameraH = 1710;

        // UfCamera
        public UfCameraParams Camera;

        // Gap Camera
        public GapCameraParams GapCam;

        [System.Xml.Serialization.XmlElement("EulaAgree")]
        public String EulaAgree
        {
            get { return eulaAgree.ToString().ToLowerInvariant(); }
            set
            {
                if (value != "true")
                { eulaAgree = false; }
                else
                { eulaAgree = true; }
            }
        }

        public Settings()
        {
            Window_X_Position = 0;
            Window_Y_Position = 0;
            Window_Height = 500;
            Window_Width = 600;

            // CA-410
            Channel_ModelA = 21;
            Channel_ModelD = 31;
            Channel_Custom = 31;
            InitialRetry = 5;
            NoOfProbes = 1;
            PortVal = 0;
            DarkErrorEnable = 0;
            ComToolPath = @"C:\Program Files (x86)\KONICA MINOLTA\CA-S40\CA-SDK2\COM_Registration_Tool.exe";

            Eulafile = "C:\\CAS\\Eula.rtf";
            LastProfile = "C:\\CAS\\Profile\\Profile.xml";
            SdcpWaitTime = 1000;
            SdcpLog = false;
            ExecLog = true;
            IntSignalWait = 700;
            NfsWait = 5000;
            PowerOnWait = 90000;
            FtpRetryCount = 2;

            //MtgtAddCtc = true;

            //ConfigChromType = ConfigChrom.ZRD_C12A;
            //CustomTarget = new ChromCustom(ColorPurpose.UF);
            PanelType = PanelType.Normal;
            Gamma = Gamma.Gamma22;
            ReadIndividualUnits = false;

            TransType = TransType.FTP;

            GammaGain = new GammaGain();
            RelativeTarget = new ChromCustom(true);
            Uniformity = new Uniformity();

            //DefaultCtCHighColor = new DefaultCtCHighColor();

            MeasAlert = true;
            xyThresh = 0;
            YThresh = 0;

            Camera = new UfCameraParams();
            GapCam = new GapCameraParams();

            WallFormFile = "";
        }

        //Settingsクラスのただ一つのインスタンス
        [NonSerialized()]
        private static Settings _instance;

        [System.Xml.Serialization.XmlIgnore]
        public static Settings Ins
        {
            get
            {
                if (_instance == null)
                    _instance = new Settings();
                return _instance;
            }
            set { _instance = value; }
        }

        /// <summary>
        /// 設定をXMLファイルから読み込み復元する
        /// </summary>
        public static bool LoadFromXmlFile()
        {
            string path = GetSettingPath();

            try
            {
                using (StreamReader sr = new StreamReader(path, new UTF8Encoding(false)))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(Settings));

                    //読み込んで逆シリアル化する
                    object obj = xs.Deserialize(sr);

                    Ins = (Settings)obj;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }

            return true;
        }

        public static bool LoadFromXmlFile(string path)
        {
            try
            {
                using (StreamReader sr = new StreamReader(path, new UTF8Encoding(false)))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(Settings));

                    //読み込んで逆シリアル化する
                    object obj = xs.Deserialize(sr);
                    Ins = (Settings)obj;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 現在の設定をXMLファイルに保存する
        /// </summary>
        public static bool SaveToXmlFile()
        {
            string path = GetSettingPath();

            try
            {
                XmlWriterSettings settings = new XmlWriterSettings();

                settings.Indent = true; // インデントを行う
                settings.IndentChars = "\t"; // インデントにタブ文字を用いる
                settings.NewLineOnAttributes = true; // 属性にも改行を行う

                using (XmlWriter writer = XmlWriter.Create(path, settings))
                //using (StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(false)))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(Settings));

                    //シリアル化して書き込む
                    xs.Serialize(writer, Ins);
                }
                //sw.Close();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }

            return true;
        }

        private static string GetSettingPath()
        {
            string currentDir = System.Environment.CurrentDirectory;
            string filePath = currentDir + "\\Components\\CasSettings.xml";

            return filePath;
        }
    }

    #region Profile GIANT

    [System.Xml.Serialization.XmlRoot("boost_serialization")]
    public class boost_serialization //_GIANT
    {
        public ProfileData ProfileData;
    }

    public class ProfileData //_GIANT
    {
        public ConnectionSetting ConnectionSetting;
        public LayoutSetting LayoutSetting;
    }

    public class ConnectionSetting //_GIANT
    {
        public int count;
        public int item_version;

        [System.Xml.Serialization.XmlElement("item")]
        public List<ControllerItem> item;
    }

    public class ControllerItem //_GIANT
    {
        public int ControllerID;
        public int MasterSlaveMode;
        public int CommunicationMode;
        public string IPAddress;
        public string ComPortName;
    }

    public class LayoutSetting
    {
        public int count;
        public int item_version;

        [System.Xml.Serialization.XmlElement("item")]
        public List<LayoutItem> item;
    }

    public class LayoutItem
    {
        public int ControllerID;
        public UnitLayoutSetting UnitLayoutSetting;
    }

    public class UnitLayoutSetting
    {
        public int count;
        public int item_version;

        [System.Xml.Serialization.XmlElement("item")]
        public List<UnitItem> item;
    }

    public class UnitItem
    {
        public int PortNo;
        public int UnitNo;
        public Allocation Allocation;
        public Picture Picture;
    }

    public class Allocation
    {
        public int X;
        public int Y;
    }

    public class Picture
    {
        public int SettingStatus;
        public int PixelX;
        public int PixelY;
    }

    #endregion Profile GIANT

    #region Profile Monolith2

    [XmlRoot("Profile")]
    public class Profile
    {
        [XmlElement("Controllers")]
        public Controllers Controllers;
    }

    [XmlRoot("Controllers")]
    public class Controllers
    {
        [XmlElement("Controller")]
        public List<Controller> Controller;
    }

    public class Controller
    {
        [XmlAttribute("Id")]
        public String Id { get; set; }

        [XmlAttribute("IsMaster")]
        public String IsMaster { get; set; }

        [XmlAttribute("IsEthernet")]
        public String IsEthernet { get; set; }

        [XmlAttribute("Connection")]
        public String Connection { get; set; }

        [XmlAttribute("Authenticate")]
        public String Authenticate { get; set; }

        [XmlElement("Units")]
        public Units Units;
    }

    [XmlRoot("Units")]
    public class Units
    {
        [XmlAttribute("LEDModel")]
        public String LedModel;

        [XmlAttribute("LedPitch")] //旧バージョンの対応為追加
        public String LedPitch;

        [XmlElement("Unit")]
        public List<Unit> Unit;
    }

    public class Unit
    {
        [XmlAttribute("Port")]
        public String Port { get; set; }

        [XmlAttribute("Unit")]
        public String UnitNo { get; set; }

        [XmlAttribute("X")]
        public String X { get; set; }

        [XmlAttribute("Y")]
        public String Y { get; set; }

        [XmlAttribute("IsPictureAssigned")]
        public String IsPictureAssigned { get; set; }

        [XmlAttribute("PixelX")]
        public String PixelX { get; set; }

        [XmlAttribute("PixelY")]
        public String PixelY { get; set; }
    }

    #endregion Profile Monolith2

    //#region Profile Monolith

    //[System.Xml.Serialization.XmlRoot("boost_serialization")]
    //public class boost_serialization_Monolith
    //{
    //    public ProfileData_Monolith ProfileData;
    //}

    //public class ProfileData_Monolith
    //{
    //    public VersionId VersionId;
    //    //public int RememberPassword;
    //    public ConnectionSetting_Monolith ConnectionSetting;
    //    public LayoutSetting LayoutSetting;
    //    //public Backup Backup;
    //}

    //public class VersionId
    //{
    //    public int initialized;
    //    public int item_version;
    //    public int value;
    //}

    //public class ConnectionSetting_Monolith
    //{
    //    public int count;
    //    public int item_version;

    //    [System.Xml.Serialization.XmlElement("item")]
    //    public List<ControllerItem_Monolith> item;
    //}

    //public class ControllerItem_Monolith
    //{
    //    public int ControllerID;
    //    public int MasterSlaveMode;
    //    public int CommunicationMode;
    //    public string IPAddress;
    //    public string ComPortName;
    //}

    //public class Backup
    //{
    //    public int count;
    //    public int item_version;

    //    [System.Xml.Serialization.XmlElement("item")]
    //    public List<BackupItem> item;
    //}

    //public class BackupItem
    //{
    //    public VersionId VersionId;
    //}

    //#endregion Profile Monolith

    #region Configuration Data

    [DataContract]
    public class ConfigurationInfo
    {
        [DataMember]
        public Dictionary<int, ControllerInfo> dicController;
        [DataMember]
        public AllocationInfo allocInfo;
        [DataMember]
        public ProfileType Model;

        //public ConfigurationInfo()
        //{
        //    dicController = new Dictionary<int, ControllerInfo>();
        //    allocInfo = new AllocationInfo();
        //}
    }

    public class ControllerInfo : INotifyPropertyChanged
    {
        // Select
        bool select;
        public bool Select
        {
            get { return select; }
            set { select = value; }
        }

        // ID
        public int ControllerID { get; set; }

        // Master
        public bool Master { get; set; }

        // Power Status
        public string PowerStatus { get; set; }

        // IP Address
        public string IPAddress { get; set; }

        [IgnoreDataMember]
        public string Password { get; set; }

        public string ModelName { get; set; }
        public string SerialNo { get; set; }
        public string Drive { get; set; }

        // Driving Units
        public int UnitCount { get; set; }

        // Error / Warning
        public string ErrorWarningStr { get; set; }
        public string Error { get; set; }
        public string Warning { get; set; }
        public List<string> TahitiWarningList { get; set; }
        // Version
        public string Version { get; set; }

        // Soft Version
        public SoftwareVersionInfo SoftwareVersion { get; set; }

        // Board Info
        public BoardInformation BoardInfo { get; set; }

        // Temperature Info
        public TemperatureInfo TempInfo { get; set; }

        // NFS need to mount
        public bool NeedMount { get; set; }

        [IgnoreDataMember]
        public bool Target { get; set; }

        public ControllerInfo()
        {
            Select = false;
            ControllerID = 0;
            Master = false;
            PowerStatus = "";
            IPAddress = "";
            ModelName = "";
            SerialNo = "";
            Drive = "";
            UnitCount = 0;
            ErrorWarningStr = "None";
            Version = "";
            Password = "";

            SoftwareVersion = new SoftwareVersionInfo();
            BoardInfo = new BoardInformation();
            TempInfo = new TemperatureInfo();
            TahitiWarningList = new List<string>();
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class ControllerHDCPInfo : INotifyPropertyChanged
    {
        public delegate bool DelegateSetSendSdcpCommand(Byte[] aryCmd, string ip);
        public delegate bool DelegateGetSendSdcpCommand(Byte[] aryCmd, out string RecievedBuffer, string ip);

        public DelegateSetSendSdcpCommand SetSendSdcpCommand { get; set; }
        public DelegateGetSendSdcpCommand GetSendSdcpCommand { get; set; }

        private ICommand actionCommand;

        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
           => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private string deviceUniqueID = "-----";
        private string activationKey = "";
        private bool activationKeyIsEnable = true;
        private string actionBtnContent = "Activate";

        private const string HDCP_ON = "01";
        private const string HDCP_OFF = "00";
        private const string HDCP_UNKNOWN = "";

        public int ControllerID { get; set; }

        public string IPAddress { get; set; }

        private HdcpStatusTypes _hdcpStatus;

        public HdcpStatusTypes HdcpStatus
        {
            get
            {
                return _hdcpStatus;
            }

            set
            {
                _hdcpStatus = value;

                if (_hdcpStatus == HdcpStatusTypes.Actived)
                {
                    ActivationKey = "";
                    ActivationKeyIsEnable = false;
                    ActionBtnContent = "Deactivate";
                }
                else if (_hdcpStatus == HdcpStatusTypes.Inactived)
                {
                    ActivationKeyIsEnable = true;
                    ActionBtnContent = "Activate";
                }
                else
                {
                    DeviceUniqueID = "-----";
                    ActivationKeyIsEnable = true;
                    ActionBtnContent = "Activate";
                }

                RaisePropertyChanged();
            }
        }

        public string DeviceUniqueID
        {
            get { return deviceUniqueID; }
            set
            {
                if (deviceUniqueID != value)
                {
                    deviceUniqueID = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string ActivationKey
        {
            get { return activationKey; }
            set
            {
                if (activationKey != value)
                {
                    activationKey = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool ActivationKeyIsEnable
        {
            get { return activationKeyIsEnable; }
            set
            {
                if (activationKeyIsEnable != value)
                {
                    activationKeyIsEnable = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string ActionBtnContent
        {
            get { return actionBtnContent; }
            set
            {
                if (actionBtnContent != value)
                {
                    actionBtnContent = value;
                    RaisePropertyChanged();
                }
            }
        }

        public ControllerHDCPInfo(int controllerID, string ipAddress)
        {
            this.ControllerID = controllerID;
            this.IPAddress = ipAddress;

            actionCommand = new DelegateCommand(
                () =>
                {
                    // Action Button 処理
                    if (HdcpStatus == HdcpStatusTypes.Actived)
                    { hdcpDeactivate(); }
                    else
                    { hdcpActivate(); }
                },
                () =>
                {
                    if (_hdcpStatus == HdcpStatusTypes.Actived)
                    { return true; }
                    else if (_hdcpStatus == HdcpStatusTypes.Inactived && !string.IsNullOrEmpty(ActivationKey))
                    { return true; }
                    else
                    { return false; }
                }
            );
        }

        public ICommand ActionCommand => actionCommand;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool hdcpActivate()
        {
            // HDCP ON
            string msg, caption, response;

            byte[] cmd = new byte[SDCPClass.CmdActivationKeySet.Length];
            Array.Copy(SDCPClass.CmdActivationKeySet, cmd, SDCPClass.CmdActivationKeySet.Length);

            // Activation Key処理
            if (createActivationKey(cmd, out cmd) != true)
            { return false; }

            msg = "HDCP function could not be activated.";
            caption = "Error!";
            if (SetSendSdcpCommand(cmd, this.IPAddress) != true)
            {
                updateStatus(HDCP_UNKNOWN);
                MainWindow.ShowMessageWindow(msg, caption, System.Drawing.SystemIcons.Error, 420, 180);
                return false;
            }

            // HDCP Status確認
            GetSendSdcpCommand(SDCPClass.CmdHDCPGet, out response, this.IPAddress);
            if (response == HDCP_ON)  // 00=Off, 01=On, ""=Unknow
            { updateStatus(response); }
            else
            {
                MainWindow.ShowMessageWindow(msg, caption, System.Drawing.SystemIcons.Error, 420, 180);

                if (response != HDCP_OFF)
                { updateStatus(HDCP_UNKNOWN); }

                return false;
            }

            return true;
        }

        public bool hdcpDeactivate()
        {
            // HDCP OFF
            string msg, caption, response;
            bool? result;

            msg = "Are you sure you want to deactivate HDCP function?";
            MainWindow.ShowMessageWindow(out result, msg, "Confirm", System.Drawing.SystemIcons.Question, 420, 180, "Yes");
            if (result == true)
            {
                msg = "HDCP function could not be deactivated.";
                caption = "Error!";
                if (SetSendSdcpCommand(SDCPClass.CmdHDCPOffSet, this.IPAddress) != true)
                {
                    updateStatus(HDCP_UNKNOWN);
                    MainWindow.ShowMessageWindow(msg, caption, System.Drawing.SystemIcons.Error, 420, 180);
                    return false;
                }

                // HDCP Status確認
                GetSendSdcpCommand(SDCPClass.CmdHDCPGet, out response, this.IPAddress);
                if (response == HDCP_OFF)  // 00=Off, 01=On, ""=Unknow
                { updateStatus(response); }
                else
                {
                    MainWindow.ShowMessageWindow(msg, caption, System.Drawing.SystemIcons.Error, 420, 180);

                    if (response != HDCP_ON)
                    { updateStatus(HDCP_UNKNOWN); }

                    return false;
                }
            }

            return true;
        }

        public void initHdcpInfo()
        {
            this.HdcpStatus = HdcpStatusTypes.None;
        }

        public bool UpdateHdcpInfo()
        {
            string response, msg;

            // Get HDCP Status 
            if (GetSendSdcpCommand(SDCPClass.CmdHDCPGet, out response, this.IPAddress) != true)
            {
                updateStatus(response);

                msg = "Failed to get HDCP Staus.";
                MainWindow.ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 420, 180);
                return false;
            }

            // Update HDCP Status
            updateStatus(response);

            // Get Device Unique ID 
            if (GetSendSdcpCommand(SDCPClass.CmdDeviceUniqueIDGet, out response, this.IPAddress) != true)
            {
                this.DeviceUniqueID = "-----";

                msg = "Failed to get Device Unique ID.";
                MainWindow.ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 420, 180);
                return false;
            }

            // Update Device Unique ID
            this.DeviceUniqueID = convertAscii(response);

            return true;
        }

        private void updateStatus(string response)
        {
            if (response == HDCP_ON)
            { HdcpStatus = HdcpStatusTypes.Actived; }
            else if (response == HDCP_OFF)
            { HdcpStatus = HdcpStatusTypes.Inactived; }
            else
            { HdcpStatus = HdcpStatusTypes.None; }
        }

        private bool createActivationKey(byte[] srcCmd, out byte[] cmd)
        {
            byte[] tempBytes;

            cmd = new byte[srcCmd.Length];
            Array.Copy(srcCmd, cmd, srcCmd.Length);

            if (ActivationKey.Length != 16 || Regex.IsMatch(ActivationKey, @"[^0-9a-fA-F]"))
            {
                string msg = "Input activation key is incorrect.";
                MainWindow.ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 420, 180);
                return false;
            }

            ASCIIEncoding ascii = new ASCIIEncoding();
            tempBytes = ascii.GetBytes(ActivationKey);

            int i = 20;
            foreach (byte tempByte in tempBytes)
            {
                cmd[i] = tempByte;
                i++;
            }

            return true;
        }

        private string convertAscii(string response)
        {
            if (response == "0180")
            { return "-----"; }

            string text = "";

            for (int i = 0; i < response.Length; i = i + 2)
            {
                string ascii = response.Substring(i, 2);
                char cha = (char)Convert.ToInt32(ascii, 16);
                text += cha;
            }
            return text.Trim();
        }
    }

    public class SoftwareVersionInfo
    {
        // CPU
        public string CPU_Firm_Ver { get; set; }

        // DIF
        public string DIF_Firm_Ver { get; set; }
        public string DIF_FPGA1_Ver { get; set; }
        public string DIF_FPGA2_Ver { get; set; }
        public string DIF_FPGA3_Ver { get; set; }
        public string DIF_DP1_Splitter { get; set; }
        public string DIF_DP2_Splitter { get; set; }
        public string DIF_DP1_Receiver1 { get; set; }
        public string DIF_DP1_Receiver2 { get; set; }
        public string DIF_DP2_Receiver1 { get; set; }
        public string DIF_DP2_Receiver2 { get; set; }

        // PIF
        public string PIF_FPGA_Ver { get; set; }

        public SoftwareVersionInfo()
        {
            CPU_Firm_Ver = "";
            DIF_Firm_Ver = "";
            DIF_FPGA1_Ver = "";
            DIF_FPGA2_Ver = "";
            DIF_FPGA3_Ver = "";
            DIF_DP1_Splitter = "";
            DIF_DP2_Splitter = "";
            DIF_DP1_Receiver1 = "";
            DIF_DP1_Receiver2 = "";
            DIF_DP2_Receiver1 = "";
            DIF_DP2_Receiver2 = "";
            PIF_FPGA_Ver = "";
        }
    }

    public class BoardInformation
    {
        // CPU
        public string CPU_Board_Name { get; set; }
        public string CPU_Parts_Number { get; set; }
        public string CPU_Mount_A { get; set; }
        public string CPU_Compl_A { get; set; }

        // DIF
        public string DIF_Board_Name { get; set; }
        public string DIF_Parts_Number { get; set; }
        public string DIF_Mount_A { get; set; }
        public string DIF_Compl_A { get; set; }

        // PIF
        public string PIF_Board_Name { get; set; }
        public string PIF_Parts_Number { get; set; }
        public string PIF_Mount_A { get; set; }
        public string PIF_Compl_A { get; set; }

        public BoardInformation()
        {
            CPU_Board_Name = "";
            CPU_Parts_Number = "";
            CPU_Mount_A = "";
            CPU_Compl_A = "";

            DIF_Board_Name = "";
            DIF_Parts_Number = "";
            DIF_Mount_A = "";
            DIF_Compl_A = "";

            PIF_Board_Name = "";
            PIF_Parts_Number = "";
            PIF_Mount_A = "";
            PIF_Compl_A = "";
        }
    }

    public class TemperatureInfo
    {
        public string IF_Temp { get; set; }
        public string PS_Temp { get; set; }
        public string DIF_Temp { get; set; }
        public string PIF_Temp { get; set; }

        public TemperatureInfo()
        {
            IF_Temp = "";
            PS_Temp = "";
            DIF_Temp = "";
            PIF_Temp = "";
        }
    }

    public class AllocationInfo
    {
        public int MaxX;
        public int MaxY;
        public string LEDModel;
        public string LedPitch; //旧バージョン対応のため復活
        public List<List<UnitInfo>> lstUnits;

        public AllocationInfo()
        {
            lstUnits = new List<List<UnitInfo>>();
        }
    }

    public class CabinetAllocationHeaderViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
           => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private int columnMaxX = 0;
        private int columnMaxY = 0;

        public int ColumnMaxX
        {
            get => columnMaxX;
            set
            {
                if (columnMaxX != value)
                {
                    columnMaxX = value;
                    RaisePropertyChanged();
                }
            }
        }

        public int ColumnMaxY
        {
            get => columnMaxY;
            set
            {
                if (columnMaxY != value)
                {
                    columnMaxY = value;
                    RaisePropertyChanged();
                }
            }
        }

        public IEnumerable<int> Columns => Enumerable.Range(1, columnMaxX);

        public IEnumerable<int> Rows => Enumerable.Range(1, columnMaxY);
    }

    public class UnitInfo : IEquatable<UnitInfo>
    {
        public bool Enable;
        public int ControllerID;
        public int PortNo;
        public int UnitNo;
        public int X;
        public int Y;
        public int PixelX;
        public int PixelY;
        public string CellDataFile;
        public string AgingDataFile;

        public string ModelName { get; set; }
        public string SerialNo { get; set; }
        public string Version { get; set; }
        public string UnitDataFile { get; set; }
        public string LedElapsedTime { get; set; }

        public string Error { get; set; }
        public string Warning { get; set; }

        public ManualGain ManualGain;
        public CameraDataClass.CabinetCoordinate CabinetPos; // Cabinetの空間座標(カメラの光学中心が原点、Cabinetの左下の座標)
        public List<UfCamCpAngle> lstCpAngle; // 各補正点のPan, Tilt角

        public UnitInfo()
        {
            Enable = false;
            ControllerID = 0;
            PortNo = 0;
            UnitNo = 0;
            X = 0;
            Y = 0;
            PixelX = 0;
            PixelY = 0;
            CellDataFile = "";
            AgingDataFile = "";

            ModelName = "";
            SerialNo = "";
            Version = "";
            UnitDataFile = "";
            LedElapsedTime = "";

            Error = "";
            Warning = "";

            ManualGain = new ManualGain();
            CabinetPos = new CabinetCoordinate();
            lstCpAngle = new List<UfCamCpAngle>();
        }

        #region Equality Operators

        public override bool Equals(object obj)
        {
            return Equals(obj as UnitInfo);
        }

        public bool Equals(UnitInfo other)
        {
            return other != null &&
                   ControllerID == other.ControllerID &&
                   PortNo == other.PortNo &&
                   UnitNo == other.UnitNo;
        }

        public override int GetHashCode()
        {
            int hashCode = -752653635;
            hashCode = hashCode * -1521134295 + ControllerID.GetHashCode();
            hashCode = hashCode * -1521134295 + PortNo.GetHashCode();
            hashCode = hashCode * -1521134295 + UnitNo.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(UnitInfo left, UnitInfo right)
        {
            return EqualityComparer<UnitInfo>.Default.Equals(left, right);
        }

        public static bool operator !=(UnitInfo left, UnitInfo right)
        {
            return !(left == right);
        }

        #endregion Equality Operators
    }

    public class CellInfo
    {
        public int ControllerID;
        public int PortNo;
        public int UnitNo;
        public int X;
        public int Y;
        public int PixelX;
        public int PixelY;

        public CellInfo(UnitInfo unit, int pixelX, int pixelY)
        {
            this.ControllerID = unit.ControllerID;
            this.PortNo = unit.PortNo;
            this.UnitNo = unit.UnitNo;
            this.X = unit.X;
            this.Y = unit.Y;
            this.PixelX = pixelX;
            this.PixelY = pixelY;
        }
    }

    public class UserSetting
    {
        public int ControllerID;
        public int LowBrightnessMode;
        public int UnitFanMode;
        public int TempCorrection;
        public int ColorTemp;
        public int ColorSpace;
        public int BurnInCorrection;
        public int SignalModeTwo;
    }

    public class UnitToggleButton : ToggleButton
    {
        public UnitInfo UnitInfo;

        public UnitToggleButton()
        {
            UnitInfo = null; //new UnitInfo();
        }
    }

    public class CorrectionValue
    {
        public int Value1; // Left/Top
        public int Value2; // Right/Bottom
    }

    public class GapCellCorrectValue
    {
        public int TopLeft;
        public int TopRight;
        public int LeftTop;
        public int RightTop;
        public int LeftBottom;
        public int RightBottom;
        public int BottomLeft;
        public int BottomRight;
    }

    public class MoveFile
    {
        public int ControllerID;
        public string FilePath;
    }

    public class NeighboringCells
    {
        public NeighboringCell UpperCell;
        public NeighboringCell DownwardCell;
        public NeighboringCell RightCell;
        public NeighboringCell LeftCell;

        public NeighboringCells()
        {
            UpperCell = new NeighboringCell();
            DownwardCell = new NeighboringCell();
            RightCell = new NeighboringCell();
            LeftCell = new NeighboringCell();
        }
    }

    public class NeighboringCell
    {
        public UnitInfo UnitInfo;
        public int CellNo;
    }

    public class DragArea
    {
        public double StartX;
        public double StartY;
        public double EndX;
        public double EndY;
    }

    public class TestPatternParam
    {
        public bool R_ON;
        public bool G_ON;
        public bool B_ON;
        public int R_FG_LV;
        public int G_FG_LV;
        public int B_FG_LV;
        public int R_BG_LV;
        public int G_BG_LV;
        public int B_BG_LV;
        public int H_ST_POS;
        public int V_ST_POS;
        public int H_WIDTH;
        public int V_WIDTH;
        public int H_PITCH;
        public int V_PITCH;
        public bool INVERT;
    }

    public static class BmpConverter
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        public static BitmapSource ToWPFBitmap(this System.Drawing.Bitmap bitmap)
        {
            var hBitmap = bitmap.GetHbitmap();

            BitmapSource source;
            try
            {
                source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap, IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }
            return source;
        }
    }

    public class CompositeConfigData
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct HEADER
        {
            public uint Signature;
            public uint Version;
            public uint DataType;
            public uint DataLength;
            public uint DateCRC;
            public uint Option1;
            public uint Option2;
            public uint HeaderCRC;
        }

        public HEADER header;
        public byte[] data;

        public static List<CompositeConfigData> getCompositeConfigDataList(string fileName, uint dataType)
        {
            List<CompositeConfigData> lstCompositeConfigData = new List<CompositeConfigData>();
            int size = Marshal.SizeOf(typeof(HEADER));
            byte[] buf = new byte[size];
            GCHandle handle = GCHandle.Alloc(buf, GCHandleType.Pinned); //メモリロック
            int index = 0; //ブロックIndex

            using (Stream stream = File.Open(fileName, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(stream, Encoding.ASCII, false))
                {
                    int len;
                    while ((len = reader.Read(buf, 0, size)) >= size)
                    {
                        HEADER h = (HEADER)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(HEADER)); //メモリブロックからオブジェクトにデータをマーシャリング

                        if (h.DataType == MainWindow.dt_Module4x2 || h.DataType == MainWindow.dt_Module4x3) // Chiron&Cancun
                        { continue; }

                        if (h.DataType == dataType)
                        {
                            try
                            {
                                CompositeConfigData compositeConfigData = new CompositeConfigData();
                                compositeConfigData.data = new byte[h.DataLength];

                                if (compositeConfigData.data.Length > 0)
                                { reader.Read(compositeConfigData.data, 0, compositeConfigData.data.Length); } //データを読込

                                compositeConfigData.header = h;
                                lstCompositeConfigData.Add(compositeConfigData);
                            }
                            catch
                            {
                                string msg = "The module information is invalid and cannot be read data.\r\nDataLength = " + h.DataLength + "\r\nOption1 = " + h.Option1;
                                MainWindow.ShowMessageWindow(msg, "Error!", System.Drawing.SystemIcons.Error, 500, 210);
                                break;
                            }
                        }
                        else if (h.DataLength > 0)
                        { reader.BaseStream.Seek(h.DataLength, SeekOrigin.Current); } //データをスキップ

                        index++;
                    }
                }
            }

            if (handle != null) //メモリを開放
            { handle.Free(); }

            return lstCompositeConfigData;
        }
    }

    public class SignalMode
    {
        public SignalModeOne _SignalModeOne;
        public SignalModeTwo _SignalModeTwo;

        public SignalMode()
        {
            _SignalModeOne = new SignalModeOne();
            _SignalModeTwo = new SignalModeTwo();
        }
    }

    public class SignalModeOne
    {
        public bool Ingam;
        public bool Unc;
        public bool Ogam;
        public bool Sbm;
        public bool Agc;
        public bool Tmp;

        public SignalModeOne()
        {
        }
    }

    public class SignalModeTwo
    {
        public bool LcalV;
        public bool LcalH;
        public bool Lmcal;
        public bool Vac;
        public bool Mgam;

        public SignalModeTwo()
        {
        }
    }

    #endregion Configuration Data

    #region Measurement Data

    public class HeaderInfo
    {
        public DateTime Date;
        public string Model;
        //public ColorTemp ColorTemp;
    }

    public class UnitColor
    {
        public string UnitName;
        public Chromaticity Top;
        public Chromaticity Right;
        public Chromaticity Bottom;
        public Chromaticity Left;
        public List<Chromaticity> lstCenter; //0=Red, 1=Green, 2=Blue, 3=White

        public UnitColor()
        {
            lstCenter = new List<Chromaticity>();
        }
    }

    public class UnitColorMultiPos
    {
        public string UnitName;
        public List<ChromWRGB> lstChrom;

        public UnitColorMultiPos()
        {
            lstChrom = new List<ChromWRGB>();
        }

        public static bool LoadFromXmlFile(string path, out List<UnitColorMultiPos> data)
        {
            data = null;

            try
            {
                using (StreamReader sr = new StreamReader(path, new UTF8Encoding(false)))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(List<UnitColorMultiPos>));

                    //読み込んで逆シリアル化する
                    object obj = xs.Deserialize(sr);

                    data = (List<UnitColorMultiPos>)obj;
                }
            }
            catch (Exception ex)
            { throw new Exception(ex.Message); }

            return true;
        }

        public static bool SaveToXmlFile(string path, List<UnitColorMultiPos> data)
        {
            try
            {
                XmlWriterSettings settings = new XmlWriterSettings();

                settings.Indent = true; // インデントを行う
                settings.IndentChars = "\t"; // インデントにタブ文字を用いる
                settings.NewLineOnAttributes = true; // 属性にも改行を行う

                using (XmlWriter writer = XmlWriter.Create(path, settings))
                //using (StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(false)))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(List<UnitColorMultiPos>));

                    //シリアル化して書き込む
                    xs.Serialize(writer, data);
                }
                //sw.Close();
            }
            catch (Exception ex)
            { throw new Exception(ex.Message); }

            return true;
        }
    }

    public class TargetUnitColor
    {
        public string UnitName;
        public List<Chromaticity> lstTop; //0=Red, 1=Green, 2=Blue, 3=White
        public List<Chromaticity> lstRight;
        public List<Chromaticity> lstBottom;
        public List<Chromaticity> lstLeft;

        public TargetUnitColor()
        {
            lstTop = new List<Chromaticity>();
            lstRight = new List<Chromaticity>();
            lstBottom = new List<Chromaticity>();
            lstLeft = new List<Chromaticity>();
        }
    }

    public class ChromWRGB
    {
        public string Position;
        public Chromaticity White;
        public Chromaticity Red;
        public Chromaticity Green;
        public Chromaticity Blue;

        public ChromWRGB()
        {
            White = new Chromaticity();
            Red = new Chromaticity();
            Green = new Chromaticity();
            Blue = new Chromaticity();
        }
    }

    [Serializable()]
    public class Chromaticity
    {
        public double x;
        public double y;
        public double Lv;

        public Chromaticity()
        {
        }

        public Chromaticity(double x, double y, double Lv)
        {
            this.x = x;
            this.y = y;
            this.Lv = Lv;
        }
    }

    public static class CalibDefault
    {
        public const double Red_x = 0.687;
        public const double Red_y = 0.306;
        public const double Red_Y = 46.82;
        public const double Green_x = 0.218;
        public const double Green_y = 0.666;
        public const double Green_Y = 138.85;
        public const double Blue_x = 0.145;
        public const double Blue_y = 0.054;
        public const double Blue_Y = 18.34;
        public const double White_x = 0.285;
        public const double White_y = 0.291;
        public const double White_Y = 204;
    }

    public class Uniformity
    {
        [System.Xml.Serialization.XmlIgnore]
        public bool IsRelativeTarget;
        [System.Xml.Serialization.XmlIgnore]
        public ModeOption ModeOption;

        [System.Xml.Serialization.XmlElement("IsRelativeTarget")]
        public String IsRelativeTargetProperty
        {
            get { return IsRelativeTarget.ToString().ToLowerInvariant(); }
            set
            {
                if (value == "true")
                { IsRelativeTarget = true; }
                else
                { IsRelativeTarget = false; }
            }
        }

        [System.Xml.Serialization.XmlElement("ModeOption")]
        public String ModeOptionProperty
        {
            get { return ModeOption.ToString(); }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    try
                    { ModeOption = (ModeOption)Enum.Parse(typeof(ModeOption), value); }
                    catch { }
                }
            }
        }

        public Uniformity()
        {
            ModeOption = ModeOption.Relative;
            IsRelativeTarget = false;
        }
    }

    [Serializable()]
    public class ChromCustom
    {
        public Chromaticity Red;
        public Chromaticity Green;
        public Chromaticity Blue;
        public Chromaticity White;

        public ChromCustom()
        {
            Red = new Chromaticity(Defined.m_Target_xr, Defined.m_Target_yr, 0);
            Green = new Chromaticity(Defined.m_Target_xg, Defined.m_Target_yg, 0);
            Blue = new Chromaticity(Defined.m_Target_xb, Defined.m_Target_yb, 0);
            White = new Chromaticity(Defined.m_Target_xw, Defined.m_Target_yw, Defined.m_Target_Yw);
        }

        public ChromCustom(bool defaultChrom)
        {
            if (defaultChrom == true)
            {
                Red = new Chromaticity(Defined.m_Target_xr, Defined.m_Target_yr, 0);
                Green = new Chromaticity(Defined.m_Target_xg, Defined.m_Target_yg, 0);
                Blue = new Chromaticity(Defined.m_Target_xb, Defined.m_Target_yb, 0);
                White = new Chromaticity(Defined.m_Target_xw, Defined.m_Target_yw, Defined.m_Target_Yw);
            }
            else
            {
                Red = new Chromaticity(0, 0, 0);
                Green = new Chromaticity(0, 0, 0);
                Blue = new Chromaticity(0, 0, 0);
                White = new Chromaticity(0, 0, 0);
            }
        }

        public ChromCustom(ConfigChrom chrom)
        {
            switch (chrom)
            {
                case ConfigChrom.ZRD_B12A:
                    setChrom(ColorPurpose.ZRD_B12A);
                    break;
                case ConfigChrom.ZRD_B15A:
                    setChrom(ColorPurpose.ZRD_B15A);
                    break;
                case ConfigChrom.ZRD_C12A:
                    setChrom(ColorPurpose.ZRD_C12A);
                    break;
                case ConfigChrom.ZRD_C15A:
                    setChrom(ColorPurpose.ZRD_C15A);
                    break;
                case ConfigChrom.ZRD_BH12D:
                    setChrom(ColorPurpose.ZRD_BH12D);
                    break;
                case ConfigChrom.ZRD_BH15D:
                    setChrom(ColorPurpose.ZRD_BH15D);
                    break;
                case ConfigChrom.ZRD_CH12D:
                    setChrom(ColorPurpose.ZRD_CH12D);
                    break;
                case ConfigChrom.ZRD_CH15D:
                    setChrom(ColorPurpose.ZRD_CH15D);
                    break;
                case ConfigChrom.ZRD_BH12D_S3:
                    setChrom(ColorPurpose.ZRD_BH12D_S3);
                    break;
                case ConfigChrom.ZRD_BH15D_S3:
                    setChrom(ColorPurpose.ZRD_BH15D_S3);
                    break;
                case ConfigChrom.ZRD_CH12D_S3:
                    setChrom(ColorPurpose.ZRD_CH12D_S3);
                    break;
                case ConfigChrom.ZRD_CH15D_S3:
                    setChrom(ColorPurpose.ZRD_CH15D_S3);
                    break;
                case ConfigChrom.Custom:
                    setChrom(ColorPurpose.ConfigCustom);
                    break;
                case ConfigChrom.NA:
                    setChrom(ColorPurpose.NA);
                    break;
                default:
                    break;
            }
        }

        public ChromCustom(ColorPurpose purpose)
        {
            setChrom(purpose);
        }

        public void setChrom(ColorPurpose purpose)
        {
            switch (purpose)
            {
                case ColorPurpose.ZRD_B12A:
                    Red = new Chromaticity(Settings.Ins.ZRDB12ATarget.Red.x, Settings.Ins.ZRDB12ATarget.Red.y, Settings.Ins.ZRDB12ATarget.Red.Lv);
                    Green = new Chromaticity(Settings.Ins.ZRDB12ATarget.Green.x, Settings.Ins.ZRDB12ATarget.Green.y, Settings.Ins.ZRDB12ATarget.Green.Lv);
                    Blue = new Chromaticity(Settings.Ins.ZRDB12ATarget.Blue.x, Settings.Ins.ZRDB12ATarget.Blue.y, Settings.Ins.ZRDB12ATarget.Blue.Lv);
                    White = new Chromaticity(Settings.Ins.ZRDB12ATarget.White.x, Settings.Ins.ZRDB12ATarget.White.y, Settings.Ins.ZRDB12ATarget.White.Lv);
                    break;
                case ColorPurpose.ZRD_B15A:
                    Red = new Chromaticity(Settings.Ins.ZRDB15ATarget.Red.x, Settings.Ins.ZRDB15ATarget.Red.y, Settings.Ins.ZRDB15ATarget.Red.Lv);
                    Green = new Chromaticity(Settings.Ins.ZRDB15ATarget.Green.x, Settings.Ins.ZRDB15ATarget.Green.y, Settings.Ins.ZRDB15ATarget.Green.Lv);
                    Blue = new Chromaticity(Settings.Ins.ZRDB15ATarget.Blue.x, Settings.Ins.ZRDB15ATarget.Blue.y, Settings.Ins.ZRDB15ATarget.Blue.Lv);
                    White = new Chromaticity(Settings.Ins.ZRDB15ATarget.White.x, Settings.Ins.ZRDB15ATarget.White.y, Settings.Ins.ZRDB15ATarget.White.Lv);
                    break;
                case ColorPurpose.ZRD_C12A:
                    Red = new Chromaticity(Settings.Ins.ZRDC12ATarget.Red.x, Settings.Ins.ZRDC12ATarget.Red.y, Settings.Ins.ZRDC12ATarget.Red.Lv);
                    Green = new Chromaticity(Settings.Ins.ZRDC12ATarget.Green.x, Settings.Ins.ZRDC12ATarget.Green.y, Settings.Ins.ZRDC12ATarget.Green.Lv);
                    Blue = new Chromaticity(Settings.Ins.ZRDC12ATarget.Blue.x, Settings.Ins.ZRDC12ATarget.Blue.y, Settings.Ins.ZRDC12ATarget.Blue.Lv);
                    White = new Chromaticity(Settings.Ins.ZRDC12ATarget.White.x, Settings.Ins.ZRDC12ATarget.White.y, Settings.Ins.ZRDC12ATarget.White.Lv);
                    break;
                case ColorPurpose.ZRD_C15A:
                    Red = new Chromaticity(Settings.Ins.ZRDC15ATarget.Red.x, Settings.Ins.ZRDC15ATarget.Red.y, Settings.Ins.ZRDC15ATarget.Red.Lv);
                    Green = new Chromaticity(Settings.Ins.ZRDC15ATarget.Green.x, Settings.Ins.ZRDC15ATarget.Green.y, Settings.Ins.ZRDC15ATarget.Green.Lv);
                    Blue = new Chromaticity(Settings.Ins.ZRDC15ATarget.Blue.x, Settings.Ins.ZRDC15ATarget.Blue.y, Settings.Ins.ZRDC15ATarget.Blue.Lv);
                    White = new Chromaticity(Settings.Ins.ZRDC15ATarget.White.x, Settings.Ins.ZRDC15ATarget.White.y, Settings.Ins.ZRDC15ATarget.White.Lv);
                    break;
                case ColorPurpose.ZRD_BH12D:
                    Red = new Chromaticity(Settings.Ins.ZRDBH12DTarget.Red.x, Settings.Ins.ZRDBH12DTarget.Red.y, Settings.Ins.ZRDBH12DTarget.Red.Lv);
                    Green = new Chromaticity(Settings.Ins.ZRDBH12DTarget.Green.x, Settings.Ins.ZRDBH12DTarget.Green.y, Settings.Ins.ZRDBH12DTarget.Green.Lv);
                    Blue = new Chromaticity(Settings.Ins.ZRDBH12DTarget.Blue.x, Settings.Ins.ZRDBH12DTarget.Blue.y, Settings.Ins.ZRDBH12DTarget.Blue.Lv);
                    White = new Chromaticity(Settings.Ins.ZRDBH12DTarget.White.x, Settings.Ins.ZRDBH12DTarget.White.y, Settings.Ins.ZRDBH12DTarget.White.Lv);
                    break;
                case ColorPurpose.ZRD_BH15D:
                    Red = new Chromaticity(Settings.Ins.ZRDBH15DTarget.Red.x, Settings.Ins.ZRDBH15DTarget.Red.y, Settings.Ins.ZRDBH15DTarget.Red.Lv);
                    Green = new Chromaticity(Settings.Ins.ZRDBH15DTarget.Green.x, Settings.Ins.ZRDBH15DTarget.Green.y, Settings.Ins.ZRDBH15DTarget.Green.Lv);
                    Blue = new Chromaticity(Settings.Ins.ZRDBH15DTarget.Blue.x, Settings.Ins.ZRDBH15DTarget.Blue.y, Settings.Ins.ZRDBH15DTarget.Blue.Lv);
                    White = new Chromaticity(Settings.Ins.ZRDBH15DTarget.White.x, Settings.Ins.ZRDBH15DTarget.White.y, Settings.Ins.ZRDBH15DTarget.White.Lv);
                    break;
                case ColorPurpose.ZRD_CH12D:
                    Red = new Chromaticity(Settings.Ins.ZRDCH12DTarget.Red.x, Settings.Ins.ZRDCH12DTarget.Red.y, Settings.Ins.ZRDCH12DTarget.Red.Lv);
                    Green = new Chromaticity(Settings.Ins.ZRDCH12DTarget.Green.x, Settings.Ins.ZRDCH12DTarget.Green.y, Settings.Ins.ZRDCH12DTarget.Green.Lv);
                    Blue = new Chromaticity(Settings.Ins.ZRDCH12DTarget.Blue.x, Settings.Ins.ZRDCH12DTarget.Blue.y, Settings.Ins.ZRDCH12DTarget.Blue.Lv);
                    White = new Chromaticity(Settings.Ins.ZRDCH12DTarget.White.x, Settings.Ins.ZRDCH12DTarget.White.y, Settings.Ins.ZRDCH12DTarget.White.Lv);
                    break;
                case ColorPurpose.ZRD_CH15D:
                    Red = new Chromaticity(Settings.Ins.ZRDCH15DTarget.Red.x, Settings.Ins.ZRDCH15DTarget.Red.y, Settings.Ins.ZRDCH15DTarget.Red.Lv);
                    Green = new Chromaticity(Settings.Ins.ZRDCH15DTarget.Green.x, Settings.Ins.ZRDCH15DTarget.Green.y, Settings.Ins.ZRDCH15DTarget.Green.Lv);
                    Blue = new Chromaticity(Settings.Ins.ZRDCH15DTarget.Blue.x, Settings.Ins.ZRDCH15DTarget.Blue.y, Settings.Ins.ZRDCH15DTarget.Blue.Lv);
                    White = new Chromaticity(Settings.Ins.ZRDCH15DTarget.White.x, Settings.Ins.ZRDCH15DTarget.White.y, Settings.Ins.ZRDCH15DTarget.White.Lv);
                    break;
                case ColorPurpose.ZRD_BH12D_S3:
                    Red = new Chromaticity(Settings.Ins.ZRDBH12DS3Target.Red.x, Settings.Ins.ZRDBH12DS3Target.Red.y, Settings.Ins.ZRDBH12DS3Target.Red.Lv);
                    Green = new Chromaticity(Settings.Ins.ZRDBH12DS3Target.Green.x, Settings.Ins.ZRDBH12DS3Target.Green.y, Settings.Ins.ZRDBH12DS3Target.Green.Lv);
                    Blue = new Chromaticity(Settings.Ins.ZRDBH12DS3Target.Blue.x, Settings.Ins.ZRDBH12DS3Target.Blue.y, Settings.Ins.ZRDBH12DS3Target.Blue.Lv);
                    White = new Chromaticity(Settings.Ins.ZRDBH12DS3Target.White.x, Settings.Ins.ZRDBH12DS3Target.White.y, Settings.Ins.ZRDBH12DS3Target.White.Lv);
                    break;
                case ColorPurpose.ZRD_BH15D_S3:
                    Red = new Chromaticity(Settings.Ins.ZRDBH15DS3Target.Red.x, Settings.Ins.ZRDBH15DS3Target.Red.y, Settings.Ins.ZRDBH15DS3Target.Red.Lv);
                    Green = new Chromaticity(Settings.Ins.ZRDBH15DS3Target.Green.x, Settings.Ins.ZRDBH15DS3Target.Green.y, Settings.Ins.ZRDBH15DS3Target.Green.Lv);
                    Blue = new Chromaticity(Settings.Ins.ZRDBH15DS3Target.Blue.x, Settings.Ins.ZRDBH15DS3Target.Blue.y, Settings.Ins.ZRDBH15DS3Target.Blue.Lv);
                    White = new Chromaticity(Settings.Ins.ZRDBH15DS3Target.White.x, Settings.Ins.ZRDBH15DS3Target.White.y, Settings.Ins.ZRDBH15DS3Target.White.Lv);
                    break;
                case ColorPurpose.ZRD_CH12D_S3:
                    Red = new Chromaticity(Settings.Ins.ZRDCH12DS3Target.Red.x, Settings.Ins.ZRDCH12DS3Target.Red.y, Settings.Ins.ZRDCH12DS3Target.Red.Lv);
                    Green = new Chromaticity(Settings.Ins.ZRDCH12DS3Target.Green.x, Settings.Ins.ZRDCH12DS3Target.Green.y, Settings.Ins.ZRDCH12DS3Target.Green.Lv);
                    Blue = new Chromaticity(Settings.Ins.ZRDCH12DS3Target.Blue.x, Settings.Ins.ZRDCH12DS3Target.Blue.y, Settings.Ins.ZRDCH12DS3Target.Blue.Lv);
                    White = new Chromaticity(Settings.Ins.ZRDCH12DS3Target.White.x, Settings.Ins.ZRDCH12DS3Target.White.y, Settings.Ins.ZRDCH12DS3Target.White.Lv);
                    break;
                case ColorPurpose.ZRD_CH15D_S3:
                    Red = new Chromaticity(Settings.Ins.ZRDCH15DS3Target.Red.x, Settings.Ins.ZRDCH15DS3Target.Red.y, Settings.Ins.ZRDCH15DS3Target.Red.Lv);
                    Green = new Chromaticity(Settings.Ins.ZRDCH15DS3Target.Green.x, Settings.Ins.ZRDCH15DS3Target.Green.y, Settings.Ins.ZRDCH15DS3Target.Green.Lv);
                    Blue = new Chromaticity(Settings.Ins.ZRDCH15DS3Target.Blue.x, Settings.Ins.ZRDCH15DS3Target.Blue.y, Settings.Ins.ZRDCH15DS3Target.Blue.Lv);
                    White = new Chromaticity(Settings.Ins.ZRDCH15DS3Target.White.x, Settings.Ins.ZRDCH15DS3Target.White.y, Settings.Ins.ZRDCH15DS3Target.White.Lv);
                    break;
                case ColorPurpose.Calib:
                    Red = new Chromaticity(0.687, 0.306, 46.82);
                    Green = new Chromaticity(0.218, 0.666, 138.85);
                    Blue = new Chromaticity(0.145, 0.054, 18.34);
                    White = new Chromaticity(0.285, 0.291, 204);
                    break;
                case ColorPurpose.ConfigCustom:
                    Red = new Chromaticity(Settings.Ins.CustomTarget.Red.x, Settings.Ins.CustomTarget.Red.y, Settings.Ins.CustomTarget.Red.Lv);
                    Green = new Chromaticity(Settings.Ins.CustomTarget.Green.x, Settings.Ins.CustomTarget.Green.y, Settings.Ins.CustomTarget.Green.Lv);
                    Blue = new Chromaticity(Settings.Ins.CustomTarget.Blue.x, Settings.Ins.CustomTarget.Blue.y, Settings.Ins.CustomTarget.Blue.Lv);
                    White = new Chromaticity(Settings.Ins.CustomTarget.White.x, Settings.Ins.CustomTarget.White.y, Settings.Ins.CustomTarget.White.Lv);
                    break;
                case ColorPurpose.Relative:
                    Red = new Chromaticity(Settings.Ins.RelativeTarget.Red.x, Settings.Ins.RelativeTarget.Red.y, Settings.Ins.RelativeTarget.Red.Lv);
                    Green = new Chromaticity(Settings.Ins.RelativeTarget.Green.x, Settings.Ins.RelativeTarget.Green.y, Settings.Ins.RelativeTarget.Green.Lv);
                    Blue = new Chromaticity(Settings.Ins.RelativeTarget.Blue.x, Settings.Ins.RelativeTarget.Blue.y, Settings.Ins.RelativeTarget.Blue.Lv);
                    White = new Chromaticity(Settings.Ins.RelativeTarget.White.x, Settings.Ins.RelativeTarget.White.y, Settings.Ins.RelativeTarget.White.Lv);
                    break;
                case ColorPurpose.NA:
                    Red = new Chromaticity(0, 0, 0);
                    Green = new Chromaticity(0, 0, 0);
                    Blue = new Chromaticity(0, 0, 0);
                    White = new Chromaticity(0, 0, 0);
                    break;
            }
        }
    }

    public class Brightness
    {
        public int _005pc;
        public int _5pc;
        public int _15pc;
        public int _20pc;
        public int _22pc;
        public int _24pc;
        public int _40pc;
        public int _50pc;
        public int _60pc;
        public int _80pc;
        public int _100pc;
        public int UF_20pc;

        public Brightness(Gamma gamma)
        {
            if (gamma == Gamma.Gamma22)
            {
                _005pc = 32; // 0.05%
                _5pc = 262; // 1023*(pc/100)^(1/2.2)
                _15pc = 432;
                _20pc = 492;
                _22pc = 512;
                _24pc = 535;
                _40pc = 675;
                _50pc = 747;
                _60pc = 811;
                _80pc = 924;
                _100pc = 1023;
                UF_20pc = 492;
            }
            else if (gamma == Gamma.Gamma26)
            {
                _5pc = 323;
                _20pc = 551;
                _22pc = 570;
                _24pc = 591;
                _50pc = 784;
                _60pc = 841;
                _80pc = 940;
                _100pc = 1023;
                UF_20pc = 551;
            }
            else if (gamma == Gamma.PQ)
            {
                _5pc = 262;
                _20pc = 492;
                _22pc = 512;
                _24pc = 535;
                _50pc = 747;
                _60pc = 811;
                _80pc = 924;
                _100pc = 1023;
                UF_20pc = 846;
            }
        }
    }

    #endregion Measurement Data

    #region Gain Data

    public class ManualGain
    {
        public List<CellGain> lstGain;

        public ManualGain()
        {
            lstGain = new List<CellGain>();
        }
    }

    public class CellGain
    {
        public int GainR;
        public int GainG;
        public int GainB;
    }

    [Serializable()]
    public class GammaGain
    {
        [System.Xml.Serialization.XmlElement("GainMeasurementMode")]
        public String GainMeasurementModeProperty
        {
            get => GainMeasurementMode.ToString();
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    try
                    {
                        if (value == "Simple" || value == "Full" || value == "0" || value == "1")
                        { GainMeasurementMode = (GainMeasurementMode)Enum.Parse(typeof(GainMeasurementMode), value); }
                    }
                    catch { }
                }
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public GainMeasurementMode GainMeasurementMode;

        [System.Xml.Serialization.XmlElement("GainAdjustmentMode")]
        public String GainAdjustmentModeProperty
        {
            get => GainAdjustmentMode.ToString();
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    try
                    {
                        if (value == "AllModules" || value == "EachModule" || value == "0" || value == "1")
                        { GainAdjustmentMode = (GainAdjustmentMode)Enum.Parse(typeof(GainAdjustmentMode), value); }
                    }
                    catch { }
                }
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public GainAdjustmentMode GainAdjustmentMode;

        [System.Xml.Serialization.XmlElement("GainDataWritingOption")]
        public String GainDataWritingOptionProperty
        {
            get => GainDataWritingOption.ToString();
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    try
                    {
                        if (value == "AllModules" || value == "NotAdjustedModules" || value == "0" || value == "1")
                        { GainDataWritingOption = (GainDataWritingOption)Enum.Parse(typeof(GainDataWritingOption), value); }
                    }
                    catch { }
                }
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public GainDataWritingOption GainDataWritingOption;

        [System.Xml.Serialization.XmlElement("GainReferenceCellPosition")]
        public String GainReferenceCellPositionProperty
        {
            get => GainReferenceCellPosition.ToString();
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    try
                    {
                        if (value == "Top" || value == "Left" || value == "Right" || value == "Bottom" || value == "0" || value == "1" || value == "2" || value == "3")
                        { GainReferenceCellPosition = (GainReferenceCellPosition)Enum.Parse(typeof(GainReferenceCellPosition), value); }
                    }
                    catch { }
                }
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public GainReferenceCellPosition GainReferenceCellPosition;

        [System.Xml.Serialization.XmlIgnore]
        public GainAdjustmentCoef _GainAdjustmentCoef;

        public GainAdjustmentCoefP12 GainAdjustmentCoefP12;
        public GainAdjustmentCoefP15 GainAdjustmentCoefP15;

        [System.Xml.Serialization.XmlIgnore]
        public Threshold _Threshold;

        public GammaGain()
        {
            _GainAdjustmentCoef = new GainAdjustmentCoef();
            GainAdjustmentCoefP12 = new GainAdjustmentCoefP12();
            GainAdjustmentCoefP15 = new GainAdjustmentCoefP15();
        }

        public GammaGain(string ledModel)
        {
            _GainAdjustmentCoef = new GainAdjustmentCoef(ledModel);
            _Threshold = new Threshold();
        }
    }

    public class GainAdjustmentCoef
    {
        public CorrectionFactorR CorrectionFactorR;
        public CorrectionFactorK CorrectionFactorK;
        public TargetH TargetH;
        public TargetL TargetL;

        public GainAdjustmentCoef()
        {
        }

        public GainAdjustmentCoef(string ledModel)
        {
            switch (ledModel)
            {
                case MainWindow.ZRD_CH12D:
                case MainWindow.ZRD_BH12D:
                case MainWindow.ZRD_CH12D_S3:
                case MainWindow.ZRD_BH12D_S3:
                    CorrectionFactorR = new CorrectionFactorR(
                        Settings.Ins.GammaGain.GainAdjustmentCoefP12.CorrectionFactorR.Rr,
                        Settings.Ins.GammaGain.GainAdjustmentCoefP12.CorrectionFactorR.Rg,
                        Settings.Ins.GammaGain.GainAdjustmentCoefP12.CorrectionFactorR.Rb);
                    CorrectionFactorK = new CorrectionFactorK(
                        Settings.Ins.GammaGain.GainAdjustmentCoefP12.CorrectionFactorK.Kr,
                        Settings.Ins.GammaGain.GainAdjustmentCoefP12.CorrectionFactorK.Kg,
                        Settings.Ins.GammaGain.GainAdjustmentCoefP12.CorrectionFactorK.Kb);
                    TargetH = new TargetH(
                        Settings.Ins.GammaGain.GainAdjustmentCoefP12.TargetH.Rht,
                        Settings.Ins.GammaGain.GainAdjustmentCoefP12.TargetH.Ght,
                        Settings.Ins.GammaGain.GainAdjustmentCoefP12.TargetH.Bht);
                    TargetL = new TargetL(
                        Settings.Ins.GammaGain.GainAdjustmentCoefP12.TargetL.Rlt,
                        Settings.Ins.GammaGain.GainAdjustmentCoefP12.TargetL.Glt,
                        Settings.Ins.GammaGain.GainAdjustmentCoefP12.TargetL.Blt);
                    break;
                case MainWindow.ZRD_CH15D:
                case MainWindow.ZRD_BH15D:
                case MainWindow.ZRD_CH15D_S3:
                case MainWindow.ZRD_BH15D_S3:
                    CorrectionFactorR = new CorrectionFactorR(
                        Settings.Ins.GammaGain.GainAdjustmentCoefP15.CorrectionFactorR.Rr,
                        Settings.Ins.GammaGain.GainAdjustmentCoefP15.CorrectionFactorR.Rg,
                        Settings.Ins.GammaGain.GainAdjustmentCoefP15.CorrectionFactorR.Rb);
                    CorrectionFactorK = new CorrectionFactorK(
                        Settings.Ins.GammaGain.GainAdjustmentCoefP15.CorrectionFactorK.Kr,
                        Settings.Ins.GammaGain.GainAdjustmentCoefP15.CorrectionFactorK.Kg,
                        Settings.Ins.GammaGain.GainAdjustmentCoefP15.CorrectionFactorK.Kb);
                    TargetH = new TargetH(
                        Settings.Ins.GammaGain.GainAdjustmentCoefP15.TargetH.Rht,
                        Settings.Ins.GammaGain.GainAdjustmentCoefP15.TargetH.Ght,
                        Settings.Ins.GammaGain.GainAdjustmentCoefP15.TargetH.Bht);
                    TargetL = new TargetL(
                        Settings.Ins.GammaGain.GainAdjustmentCoefP15.TargetL.Rlt,
                        Settings.Ins.GammaGain.GainAdjustmentCoefP15.TargetL.Glt,
                        Settings.Ins.GammaGain.GainAdjustmentCoefP15.TargetL.Blt);
                    break;
                default:
                    // Do nothing
                    break;
            }
        }
    }

    [Serializable()]
    public class GainAdjustmentCoefP12
    {
        public CorrectionFactorR CorrectionFactorR;
        public CorrectionFactorK CorrectionFactorK;
        public TargetH TargetH;
        public TargetL TargetL;

        public GainAdjustmentCoefP12()
        {
            CorrectionFactorR = new CorrectionFactorR();
            CorrectionFactorK = new CorrectionFactorK();
            TargetH = new TargetH();
            TargetL = new TargetL();
        }
    }

    [Serializable()]
    public class GainAdjustmentCoefP15
    {
        public CorrectionFactorR CorrectionFactorR;
        public CorrectionFactorK CorrectionFactorK;
        public TargetH TargetH;
        public TargetL TargetL;

        public GainAdjustmentCoefP15()
        {
            CorrectionFactorR = new CorrectionFactorR();
            CorrectionFactorK = new CorrectionFactorK();
            TargetH = new TargetH();
            TargetL = new TargetL();
        }
    }

    [Serializable()]
    public class CorrectionFactorR
    {
        public double Rr;
        public double Rg;
        public double Rb;

        public CorrectionFactorR()
        {
        }

        public CorrectionFactorR(double rr, double rg, double rb)
        {
            this.Rr = rr;
            this.Rg = rg;
            this.Rb = rb;
        }
    }

    [Serializable()]
    public class CorrectionFactorK
    {
        public double Kr;
        public double Kg;
        public double Kb;

        public CorrectionFactorK()
        {
        }

        public CorrectionFactorK(double kr, double kg, double kb)
        {
            this.Kr = kr;
            this.Kg = kg;
            this.Kb = kb;
        }
    }

    [Serializable()]
    public class TargetH
    {
        public double Rht;
        public double Ght;
        public double Bht;

        public TargetH()
        {
        }

        public TargetH(double rht, double ght, double bht)
        {
            this.Rht = rht;
            this.Ght = ght;
            this.Bht = bht;
        }
    }

    [Serializable()]
    public class TargetL
    {
        public double Rlt;
        public double Glt;
        public double Blt;

        public TargetL()
        {
        }

        public TargetL(double rlt, double glt, double blt)
        {
            this.Rlt = rlt;
            this.Glt = glt;
            this.Blt = blt;
        }
    }

    public class Threshold
    {
        public RGB LowThresholdLevel0;
        public RGB HighThresholdLevel1;
        public RGB HighThresholdLevel2;
        public RGB HighThresholdLevel3;
        public RGB HighThresholdLevel4;

        public Threshold()
        {
            LowThresholdLevel0 = new RGB(0x0831, 0x0831, 0x0831);
            HighThresholdLevel1 = new RGB(0x0D, 0x0D, 0x0D);
            HighThresholdLevel2 = new RGB(0x33, 0x33, 0x33);
            HighThresholdLevel3 = new RGB(0x80, 0x80, 0x80);
            HighThresholdLevel4 = new RGB(0xCD, 0xCD, 0xCD);
        }
    }

    public class RGB
    {
        public int Red;
        public int Green;
        public int Blue;

        public RGB()
        {
        }

        public RGB(int r, int g, int b)
        {
            this.Red = r;
            this.Green = g;
            this.Blue = b;
        }
    }

    public class CrossTalk
    {
        public CrossTalkTarget _CrossTalkTarget;
        public CrossTalkCoef_MG _CrossTalkCoef_MG;

        public CrossTalk()
        {
            _CrossTalkTarget = new CrossTalkTarget();
            _CrossTalkCoef_MG = new CrossTalkCoef_MG();
        }

        public CrossTalk(string ledModel)
        {
            _CrossTalkTarget = new CrossTalkTarget(ledModel);
            _CrossTalkCoef_MG = new CrossTalkCoef_MG();
        }
    }

    public class CrossTalkTarget
    {
        public double Yctt;
        public double xctt;
        public double yctt;

        public CrossTalkTarget()
        {
        }

        public CrossTalkTarget(string ledModel)
        {
            switch (ledModel)
            {
                case MainWindow.ZRD_CH12D:
                case MainWindow.ZRD_BH12D:
                case MainWindow.ZRD_CH12D_S3:
                case MainWindow.ZRD_BH12D_S3:
                    Yctt = 1.038;
                    xctt = -0.011;
                    yctt = -0.0264;
                    break;
                case MainWindow.ZRD_CH15D:
                case MainWindow.ZRD_BH15D:
                case MainWindow.ZRD_CH15D_S3:
                case MainWindow.ZRD_BH15D_S3:
                    Yctt = 0.998;
                    xctt = -0.0056;
                    yctt = -0.0135;
                    break;
                default:
                    // Do nothing
                    break;
            }
        }
    }

    public class CrossTalkCoef_MG
    {
        public double WxRY;
        public double WxGY;
        public double WxBY;
        public double WyRY;
        public double WyGY;
        public double WyBY;

        public CrossTalkCoef_MG()
        {
            WxRY = 8.087;
            WxGY = -2.179;
            WxBY = -2.667;
            WyRY = -3.888;
            WyGY = 1.679;
            WyBY = -6.336;
        }
    }

    public class CrossTalkCoef_UF
    {
        public double WxRY;
        public double WxGY;
        public double WxBY;
        public double WyRY;
        public double WyGY;
        public double WyBY;

        public CrossTalkCoef_UF()
        {
            WxRY = 7.31;
            WxGY = -2.68;
            WxBY = -2.43;
            WyRY = -3.88;
            WyGY = 2.39;
            WyBY = -6.67;
        }
    }

    public class CrossTalkCorrectionHighColor
    {
        public double Red;
        public double Green;
        public double Blue;

        public CrossTalkCorrectionHighColor()
        {
        }

        public CrossTalkCorrectionHighColor(string ledModel)
        {
            switch (ledModel)
            {
                case MainWindow.ZRD_CH12D:
                case MainWindow.ZRD_BH12D:
                case MainWindow.ZRD_CH12D_S3:
                case MainWindow.ZRD_BH12D_S3:
                    if (Settings.Ins.DefaultCtCHighColor == null)
                    {
                        Red = 0.026698;
                        Green = 0.011866;
                        Blue = -0.023411;
                    }
                    else
                    {
                        Red = Settings.Ins.DefaultCtCHighColor.Red;
                        Green = Settings.Ins.DefaultCtCHighColor.Green;
                        Blue = Settings.Ins.DefaultCtCHighColor.Blue;
                    }
                    break;
                case MainWindow.ZRD_CH15D:
                case MainWindow.ZRD_BH15D:
                case MainWindow.ZRD_CH15D_S3:
                case MainWindow.ZRD_BH15D_S3:
                    if (Settings.Ins.DefaultCtCHighColor == null)
                    {
                        Red = 0.003424;
                        Green = -0.000283;
                        Blue = -0.009103;
                    }
                    else
                    {
                        Red = Settings.Ins.DefaultCtCHighColor.Red;
                        Green = Settings.Ins.DefaultCtCHighColor.Green;
                        Blue = Settings.Ins.DefaultCtCHighColor.Blue;
                    }
                    break;
                default:
                    // Do nothing
                    break;
            }
        }
    }

    [Serializable()]
    public class DefaultCtCHighColor
    {
        public double Red;
        public double Green;
        public double Blue;

        public DefaultCtCHighColor()
        {
        }
    }

    public class MtgtAddCrosstalk
    {
        public bool IsAddCtc;

        public MtgtAddCrosstalk()
        {
            if (Settings.Ins.MtgtAddCtc == null)
            { IsAddCtc = true; }
            else
            { IsAddCtc = Settings.Ins.MtgtAddCtc.IsAddCtc; }
        }
    }

    [Serializable()]
    public class MtgtAddCtc
    {
        public bool IsAddCtc;

        public MtgtAddCtc()
        {
        }
    }

    #endregion Gain Data

    #region Change Analyzer

    public class ChangeComWindow
    {
        public string ClassName;
        public string Title;
        public IntPtr hWnd;
        public int Style;
    }

    #endregion

    #region UfCamera

    public class UfCameraParams
    {
        public bool SaveIntImage = false;
        public string Name = ""; // カメラ名称
        public ShootCondition SetPosSetting; // 位置合せ用パラメータ
        public ShootCondition TrimAreaSetting; // TrimmingArea取得用パラメータ
        public ShootCondition MeasAreaSetting; // 色測定用パラメータ

        //public int MakerSize = 30; // 位置合わせ用マーカーのサイズ
        //public int MakerThresh = 200; // 位置合わせ画像（Jpeg）用
        //public int MakerCamArea = 400; // マーカーのカメラLiveView画像上での面積

        public int PatternWait = 1000;
        // added by Hotta 2022/09/30
        public int PatternWait_CamPos = 400;    // カメラ位置合わせの時に使用する、パターン表示コマンド発行してから、カメラ撮影するまでの待機時間
        //
        public int CameraWait = 3000; // カメラ制御プロセス分離に伴い数秒は必要
        public int CaptureTimeout = 10000;

        public int MaxLoopCount = 30;
        public int GcWait = 1000;

        public int TrimmingSize = 50; // TrimminAreaの表示サイズ
        //public int Thresh = 100; // 2値化をOtsuに変更したためMask画像専用に変更
        //public int MaskThresh = 100; // ARWマスク画像作成用
        //public int TrimmingArea = 4000;

        public int MeasLevelR = 492; // 20IRE
        public int MeasLevelG = 492;
        public int MeasLevelB = 492;

        public double Blanking = 0.25; // トリミングエリアの削り量(割合) 0.5(50％)を超えると正常に動作しない
        //public int ColorOffset = 0;
        //public double SpecLimitR = 10; // 画素値の許容量 Targetからこれ以内なら合格
        //public double SpecLimitG = 10;
        //public double SpecLimitB = 10;
        //public int InsensLevel = 5; // 無感度領域、調整量が小さい（すでに十分規格に入っている）場合は調整しない

        public double MeasAreaBlanking = 0.1; // UF Measurmentの測定エリア（Moduleの9分割）の削り量(割合) 0.5(50％)を超えると正常に動作しない

        public double SpecSetPosPan = 2.0;
        public double SpecSetPosTilt = 2.0;
        public double SpecSetPosRoll = 2.0;
        public double SpecSetPosX = 200;
        public double SpecSetPosY = 200;
        public double SpecSetPosZ = 200;

        public int MaxRawValue = 8000; // Raw画像の最大画素値、この値以上だとサチっていると判断
        public int MinRawValue = 500;

        public double MeasureViewGain = 1.0; //5.0; // あまり目立たないように変更

        public double UfSpecInside = 2.5;
        public double UfSpecOutside = 3.5;

        public double UfGapSpecW = 2.5; // 隣接モジュール間の画素値差のスペック[%]
        //public double UfGapSpecR = 100; // White以外は確認せず
        //public double UfGapSpecG = 100;
        //public double UfGapSpecB = 100;

        //public int SpecReflection = 10; // 照明映り込みの閾値
        //public int SpecRefArea = 9; // 照明映り込みの面積

        public bool ShowDiagonal = false; // 位置合わせ時の対角線表示

        public int LogMaxGen = 5;

        public int SaturationLimit = 10000; // 飽和画素数の上限
        public int BlackDiffLimit = 100000; // 測定前後の黒画像比較（3%以上の差があるエリアの面積）

        public UfCameraParams()
        {
            SetPosSetting = new ShootCondition();
            TrimAreaSetting = new ShootCondition();
            MeasAreaSetting = new ShootCondition();
        }
    }

    public class UfCamMeasLog
    {
        public double WallCamDistance;
        public double WallHeight;
        public double CamHeight;
        public CameraPosition StartCamPos; // 開始時カメラ位置
        public CameraPosition EndCamPos; // 終了時カメラ位置

        public List<UfCamMeasValue> lstUfCamMeas;

        public UfCamMeasLog()
        {
            lstUfCamMeas = new List<UfCamMeasValue>();
        }

        public static bool LoadFromXmlFile(string path, out UfCamMeasLog data)
        {
            data = null;

            try
            {
                using (StreamReader sr = new StreamReader(path, new UTF8Encoding(false)))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(UfCamMeasLog));

                    //読み込んで逆シリアル化する
                    object obj = xs.Deserialize(sr);
                    data = (UfCamMeasLog)obj;
                }
            }
            catch (Exception ex)
            { throw new Exception(ex.Message); }

            return true;
        }

        public static bool LoadFromEncryptFile(string path, out UfCamMeasLog data, string iv, string key)
        {
            int count = 0;

            data = null;

            while (true)
            {
                try
                {
                    using (StreamReader sr = new StreamReader(path, new UTF8Encoding(false)))
                    {
                        string cipher = sr.ReadToEnd();
                        string text = Decrypt(cipher, iv, key);

                        System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(UfCamMeasLog));

                        using (StreamReader stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(text))))
                        {
                            //読み込んで逆シリアル化する
                            object obj = xs.Deserialize(stream);
                            data = (UfCamMeasLog)obj;
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    if (count > 10)
                    { throw new Exception(ex.Message); }

                    count++;
                }

                System.Threading.Thread.Sleep(100);
            }
        }

        public static bool SaveToXmlFile(string path, UfCamMeasLog data)
        {
            try
            {
                XmlWriterSettings settings = new XmlWriterSettings();

                settings.Indent = true; // インデントを行う
                settings.IndentChars = "\t"; // インデントにタブ文字を用いる
                settings.NewLineOnAttributes = true; // 属性にも改行を行う

                using (XmlWriter writer = XmlWriter.Create(path, settings))                
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(UfCamMeasLog));

                    //シリアル化して書き込む
                    xs.Serialize(writer, data);                    
                }
            }
            catch (Exception ex)
            { throw new Exception(ex.Message); }

            return true;
        }

        public static bool SaveToEncryptFile(string path, UfCamMeasLog data, string iv, string key)
        {
            int count = 0;

            while (true)
            {
                try
                {
                    string text;
                    XmlWriterSettings settings = new XmlWriterSettings();

                    // 暗号化して書き込む
                    using (StringWriter writer = new StringWriter())
                    {
                        System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(UfCamMeasLog));

                        //シリアル化して書き込む
                        xs.Serialize(writer, data);

                        text = writer.ToString();
                    }

                    string cipher = Encrypt(text, iv, key);

                    using (StreamWriter sw = new StreamWriter(path))
                    { sw.Write(cipher); }

                    return true;
                }
                catch (Exception ex)
                {
                    if (count > 10)
                    { throw new Exception(ex.Message); }

                    count++;
                }

                System.Threading.Thread.Sleep(100);
            }
        }

        /// <summary>
        /// 対称鍵暗号を使って文字列を暗号化する
        /// </summary>
        /// <param name="text">暗号化する文字列</param>
        /// <param name="iv">対称アルゴリズムの初期ベクター</param>
        /// <param name="key">対称アルゴリズムの共有鍵</param>
        /// <returns>暗号化された文字列</returns>
        private static string Encrypt(string text, string iv, string key)
        {

            using (RijndaelManaged rijndael = new RijndaelManaged())
            {
                rijndael.BlockSize = 128;
                rijndael.KeySize = 128;
                rijndael.Mode = CipherMode.CBC;
                rijndael.Padding = PaddingMode.PKCS7;

                rijndael.IV = Encoding.UTF8.GetBytes(iv);
                rijndael.Key = Encoding.UTF8.GetBytes(key);

                ICryptoTransform encryptor = rijndael.CreateEncryptor(rijndael.Key, rijndael.IV);

                byte[] encrypted;
                using (MemoryStream mStream = new MemoryStream())
                {
                    using (CryptoStream ctStream = new CryptoStream(mStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(ctStream))
                        {
                            sw.Write(text);
                        }
                        encrypted = mStream.ToArray();
                    }
                }
                return (System.Convert.ToBase64String(encrypted));
            }
        }

        /// <summary>
        /// 対称鍵暗号を使って暗号文を復号する
        /// </summary>
        /// <param name="cipher">暗号化された文字列</param>
        /// <param name="iv">対称アルゴリズムの初期ベクター</param>
        /// <param name="key">対称アルゴリズムの共有鍵</param>
        /// <returns>復号された文字列</returns>
        private static string Decrypt(string cipher, string iv, string key)
        {
            using (RijndaelManaged rijndael = new RijndaelManaged())
            {
                rijndael.BlockSize = 128;
                rijndael.KeySize = 128;
                rijndael.Mode = CipherMode.CBC;
                rijndael.Padding = PaddingMode.PKCS7;

                rijndael.IV = Encoding.UTF8.GetBytes(iv);
                rijndael.Key = Encoding.UTF8.GetBytes(key);

                ICryptoTransform decryptor = rijndael.CreateDecryptor(rijndael.Key, rijndael.IV);

                string plain = string.Empty;
                using (MemoryStream mStream = new MemoryStream(System.Convert.FromBase64String(cipher)))
                {
                    using (CryptoStream ctStream = new CryptoStream(mStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(ctStream))
                        {
                            plain = sr.ReadToEnd();
                        }
                    }
                }
                return plain;
            }
        }
    }

    /// <summary>
    /// UFの測定情報（Unit単位）
    /// </summary>
    public class UfCamMeasValue
    {
        public UnitInfo Unit; // Unit情報

        public UfCamModule[] aryUfCamModules;

        public UfCamMeasValue()
        {
            int moduleCount = 12;

            aryUfCamModules = new UfCamModule[moduleCount];

            for (int cell = 0; cell < moduleCount; cell++)
            {
                aryUfCamModules[cell] = new UfCamModule();
            }
        }

        public UfCamMeasValue(int moduleCount)
        {
            aryUfCamModules = new UfCamModule[moduleCount];

            for (int cell = 0; cell < moduleCount; cell++)
            {
                aryUfCamModules[cell] = new UfCamModule();
            }
        }

        public static bool LoadFromXmlFile(string path, out List<UfCamMeasValue> data)
        {
            data = null;

            try
            {
                using (StreamReader sr = new StreamReader(path, new UTF8Encoding(false)))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(List<UfCamMeasValue>));

                    //読み込んで逆シリアル化する
                    object obj = xs.Deserialize(sr);
                    data = (List<UfCamMeasValue>)obj;
                }
            }
            catch (Exception ex)
            { throw new Exception(ex.Message); }

            return true;
        }

        public static bool SaveToXmlFile(string path, List<UfCamMeasValue> data)
        {
            try
            {
                XmlWriterSettings settings = new XmlWriterSettings();

                settings.Indent = true; // インデントを行う
                settings.IndentChars = "\t"; // インデントにタブ文字を用いる
                settings.NewLineOnAttributes = true; // 属性にも改行を行う

                using (XmlWriter writer = XmlWriter.Create(path, settings))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(List<UfCamMeasValue>));

                    //シリアル化して書き込む
                    xs.Serialize(writer, data);
                }
            }
            catch (Exception ex)
            { throw new Exception(ex.Message); }

            return true;
        }
    }

    public class UfCamModule
    {
        public UfCamMp[] aryUfCamMp;

        // 画素値が隣接測定点に比べて何％ズレているか 隣接点間の差なので右と下のみ計算する
        public double PcPvGapRightR;
        public double PcPvGapRightG;
        public double PcPvGapRightB;
        public double PcPvGapRightW;

        public double PcPvGapBottomR;
        public double PcPvGapBottomG;
        public double PcPvGapBottomB;
        public double PcPvGapBottomW;

        public UfCamModule()
        {
            aryUfCamMp = new UfCamMp[MainWindow.UfMeasSplitCount]; // 9：Cellの分割数（3×3）
        }
    }

    public class UfCamMp // Mp = Measurement Point(測定点)
    {
        public CellNum CellNo; // Cell番号
        public int PosNo; // Cell内を9分割しているので0～8の番号が振られる。左上から右へ順番に振られる
        public Area CamArea; // 画像上の座標

        public double PixelValueR; // Area内のR画素値平均
        public double PixelValueG; // Area内のG画素値平均
        public double PixelValueB; // Area内のB画素値平均
        public double PixelValueW; // Area内のRGB画素値平均

        // 校正テーブル適応前の生値
        public double PvRawR;
        public double PvRawG;
        public double PvRawB;
        public double PvRawW;

        // 測定画面全体の平均から±何％ズレているか Pc = パーセント
        public double PcPvR;
        public double PcPvG;
        public double PcPvB;
        public double PcPvW;

        // 色度バランス Whiteに対しての各画素値の割合
        public double ChromBalanceR; // PvR / PvW
        public double ChromBalanceG; // PvG / PvW
        public double ChromBalanceB; // PvB / PvW

        // 色度バランスが測定画面全体の平均から±何％ズレているか Pc = パーセント
        public double PcChromR;
        public double PcChromG;
        public double PcChromB;

        public CameraCorrectionValue CCV; // カメラ(レンズ)補正値情報
        public LedCorrectionValue LCV; // LED補正値
    }

    public class UfCamCorrectionPoint
    {
        [XmlIgnore]
        public UnitInfo Unit; // 補正点が含まれるUnit/Cabinetの情報
        public UfCamCorrectPos Pos;
        public Area CameraArea; // 撮影画像の画素領域
        public Quadrant Quad; // 象限

        public double PixelValueR; // Area内のR画素刺激値平均
        public double PixelValueG; // Area内のG画素刺激値平均
        public double PixelValueB; // Area内のB画素刺激値平均

        public CameraCorrectionValue CCV; // カメラ(レンズ)補正値情報
        public LedCorrectionValue LCV; // LED補正値

        // added by Hotta 2024/09/09 for crosstalk
        public int ModuleNo = -1;

        public static bool LoadFromXmlFile(string path, out List<UfCamCorrectionPoint> data)
        {
            data = null;

            try
            {
                using (StreamReader sr = new StreamReader(path, new UTF8Encoding(false)))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(List<UfCamCorrectionPoint>));

                    //読み込んで逆シリアル化する
                    object obj = xs.Deserialize(sr);
                    data = (List<UfCamCorrectionPoint>)obj;
                }
            }
            catch (Exception ex)
            { throw new Exception(ex.Message); }

            return true;
        }

        public static bool SaveToXmlFile(string path, List<UfCamCorrectionPoint> data)
        {
            try
            {
                XmlWriterSettings settings = new XmlWriterSettings();

                settings.Indent = true; // インデントを行う
                settings.IndentChars = "\t"; // インデントにタブ文字を用いる
                settings.NewLineOnAttributes = true; // 属性にも改行を行う

                using (XmlWriter writer = XmlWriter.Create(path, settings))
                //using (StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(false)))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(List<UfCamCorrectionPoint>));

                    //シリアル化して書き込む
                    xs.Serialize(writer, data);
                }
            }
            catch (Exception ex)
            { throw new Exception(ex.Message); }

            return true;
        }

        public static bool LoadFromXmlFile(string path, out UfCamCorrectionPoint data)
        {
            data = null;

            try
            {
                using (StreamReader sr = new StreamReader(path, new UTF8Encoding(false)))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(UfCamCorrectionPoint));

                    //読み込んで逆シリアル化する
                    object obj = xs.Deserialize(sr);
                    data = (UfCamCorrectionPoint)obj;
                }
            }
            catch (Exception ex)
            { throw new Exception(ex.Message); }

            return true;
        }

        public static bool SaveToXmlFile(string path, UfCamCorrectionPoint data)
        {
            try
            {
                XmlWriterSettings settings = new XmlWriterSettings();

                settings.Indent = true; // インデントを行う
                settings.IndentChars = "\t"; // インデントにタブ文字を用いる
                settings.NewLineOnAttributes = true; // 属性にも改行を行う

                using (XmlWriter writer = XmlWriter.Create(path, settings))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(UfCamCorrectionPoint));

                    //シリアル化して書き込む
                    xs.Serialize(writer, data);
                }
            }
            catch (Exception ex)
            { throw new Exception(ex.Message); }

            return true;
        }

        public static bool LoadFromEncryptFile(string path, out UfCamCorrectionPoint data, string iv, string key)
        {
            int count = 0;

            data = null;

            while (true)
            {
                try
                {
                    using (StreamReader sr = new StreamReader(path, new UTF8Encoding(false)))
                    {

                        string cipher = sr.ReadToEnd();
                        string text = Decrypt(cipher, iv, key);

                        System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(UfCamCorrectionPoint));

                        using (StreamReader stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(text))))
                        {
                            //読み込んで逆シリアル化する
                            object obj = xs.Deserialize(stream);                         
                            data = (UfCamCorrectionPoint)obj;
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    if (count > 10)
                    { throw new Exception(ex.Message); }

                    count++;
                }

                System.Threading.Thread.Sleep(100);
            }
        }

        public static bool SaveToEncryptFile(string path, UfCamCorrectionPoint data, string iv, string key)
        {
            int count = 0;

            while (true)
            {
                try
                {
                    string text;
                    XmlWriterSettings settings = new XmlWriterSettings();

                    // 暗号化して書き込む
                    using (StringWriter writer = new StringWriter())
                    {
                        System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(UfCamCorrectionPoint));

                        //シリアル化して書き込む
                        xs.Serialize(writer, data);

                        text = writer.ToString();
                    }

                    string cipher = Encrypt(text, iv, key);

                    using (StreamWriter sw = new StreamWriter(path))
                    { sw.Write(cipher); }
                    return true;
                }
                catch (Exception ex)
                {
                    if (count > 10)
                    { throw new Exception(ex.Message); }

                    count++;
                }

                System.Threading.Thread.Sleep(100);
            }
        }

        /// <summary>
        /// 対称鍵暗号を使って文字列を暗号化する
        /// </summary>
        /// <param name="text">暗号化する文字列</param>
        /// <param name="iv">対称アルゴリズムの初期ベクター</param>
        /// <param name="key">対称アルゴリズムの共有鍵</param>
        /// <returns>暗号化された文字列</returns>
        private static string Encrypt(string text, string iv, string key)
        {

            using (RijndaelManaged rijndael = new RijndaelManaged())
            {
                rijndael.BlockSize = 128;
                rijndael.KeySize = 128;
                rijndael.Mode = CipherMode.CBC;
                rijndael.Padding = PaddingMode.PKCS7;

                rijndael.IV = Encoding.UTF8.GetBytes(iv);
                rijndael.Key = Encoding.UTF8.GetBytes(key);

                ICryptoTransform encryptor = rijndael.CreateEncryptor(rijndael.Key, rijndael.IV);

                byte[] encrypted;
                using (MemoryStream mStream = new MemoryStream())
                {
                    using (CryptoStream ctStream = new CryptoStream(mStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(ctStream))
                        {
                            sw.Write(text);
                        }
                        encrypted = mStream.ToArray();
                    }
                }
                return (System.Convert.ToBase64String(encrypted));
            }
        }

        /// <summary>
        /// 対称鍵暗号を使って暗号文を復号する
        /// </summary>
        /// <param name="cipher">暗号化された文字列</param>
        /// <param name="iv">対称アルゴリズムの初期ベクター</param>
        /// <param name="key">対称アルゴリズムの共有鍵</param>
        /// <returns>復号された文字列</returns>
        private static string Decrypt(string cipher, string iv, string key)
        {
            using (RijndaelManaged rijndael = new RijndaelManaged())
            {
                rijndael.BlockSize = 128;
                rijndael.KeySize = 128;
                rijndael.Mode = CipherMode.CBC;
                rijndael.Padding = PaddingMode.PKCS7;

                rijndael.IV = Encoding.UTF8.GetBytes(iv);
                rijndael.Key = Encoding.UTF8.GetBytes(key);

                ICryptoTransform decryptor = rijndael.CreateDecryptor(rijndael.Key, rijndael.IV);

                string plain = string.Empty;
                using (MemoryStream mStream = new MemoryStream(System.Convert.FromBase64String(cipher)))
                {
                    using (CryptoStream ctStream = new CryptoStream(mStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(ctStream))
                        {
                            plain = sr.ReadToEnd();
                        }
                    }
                }
                return plain;
            }
        }
    }

    public class UfCamAdjLog
    {
        public double WallCamDistance;
        public double WallHeight;
        public double CamHeight;
        public CameraPosition StartCamPos; // 開始時カメラ位置
        public CameraPosition EndCamPos; // 終了時カメラ位置

        public List<UfCamCabinetCpInfo> lstUnitCpInfo;

        public UfCamAdjLog()
        {
            lstUnitCpInfo = new List<UfCamCabinetCpInfo>();
        }

        public static bool LoadFromXmlFile(string path, out UfCamAdjLog data)
        {
            data = null;

            try
            {
                using (StreamReader sr = new StreamReader(path, new UTF8Encoding(false)))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(UfCamAdjLog));

                    //読み込んで逆シリアル化する
                    object obj = xs.Deserialize(sr);
                    data = (UfCamAdjLog)obj;
                }
            }
            catch (Exception ex)
            { throw new Exception(ex.Message); }

            return true;
        }

        public static bool LoadFromEncryptFile(string path, out UfCamAdjLog data, string iv, string key)
        {
            int count = 0;

            data = null;

            while (true)
            {
                try
                {
                    using (StreamReader sr = new StreamReader(path, new UTF8Encoding(false)))
                    {
                        string cipher = sr.ReadToEnd();
                        string text = Decrypt(cipher, iv, key);

                        System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(UfCamAdjLog));

                        using (StreamReader stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(text))))
                        {
                            //読み込んで逆シリアル化する
                            object obj = xs.Deserialize(stream);
                            data = (UfCamAdjLog)obj;
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    if (count > 10)
                    { throw new Exception(ex.Message); }

                    count++;
                }

                System.Threading.Thread.Sleep(100);
            }
        }

        public static bool SaveToXmlFile(string path, UfCamAdjLog data)
        {
            try
            {
                XmlWriterSettings settings = new XmlWriterSettings();

                settings.Indent = true; // インデントを行う
                settings.IndentChars = "\t"; // インデントにタブ文字を用いる
                settings.NewLineOnAttributes = true; // 属性にも改行を行う

                using (XmlWriter writer = XmlWriter.Create(path, settings))
                //using (StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(false)))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(UfCamAdjLog));

                    //シリアル化して書き込む
                    xs.Serialize(writer, data);
                }
                //sw.Close();
            }
            catch (Exception ex)
            { throw new Exception(ex.Message); }

            return true;
        }

        public static bool SaveToEncryptFile(string path, UfCamAdjLog data, string iv, string key)
        {
            int count = 0;

            while (true)
            {
                try
                {
                    string text;
                    XmlWriterSettings settings = new XmlWriterSettings();

                    // 暗号化して書き込む
                    using (StringWriter writer = new StringWriter())
                    {
                        System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(UfCamAdjLog));

                        //シリアル化して書き込む
                        xs.Serialize(writer, data);

                        text = writer.ToString();
                    }

                    string cipher = Encrypt(text, iv, key);

                    using (StreamWriter sw = new StreamWriter(path))
                    { sw.Write(cipher); }
                    return true;
                }
                catch (Exception ex)
                {
                    if (count > 10)
                    { throw new Exception(ex.Message); }

                    count++;
                }

                System.Threading.Thread.Sleep(100);
            }
        }

        /// <summary>
        /// 対称鍵暗号を使って文字列を暗号化する
        /// </summary>
        /// <param name="text">暗号化する文字列</param>
        /// <param name="iv">対称アルゴリズムの初期ベクター</param>
        /// <param name="key">対称アルゴリズムの共有鍵</param>
        /// <returns>暗号化された文字列</returns>
        private static string Encrypt(string text, string iv, string key)
        {

            using (RijndaelManaged rijndael = new RijndaelManaged())
            {
                rijndael.BlockSize = 128;
                rijndael.KeySize = 128;
                rijndael.Mode = CipherMode.CBC;
                rijndael.Padding = PaddingMode.PKCS7;

                rijndael.IV = Encoding.UTF8.GetBytes(iv);
                rijndael.Key = Encoding.UTF8.GetBytes(key);

                ICryptoTransform encryptor = rijndael.CreateEncryptor(rijndael.Key, rijndael.IV);

                byte[] encrypted;
                using (MemoryStream mStream = new MemoryStream())
                {
                    using (CryptoStream ctStream = new CryptoStream(mStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(ctStream))
                        {
                            sw.Write(text);
                        }
                        encrypted = mStream.ToArray();
                    }
                }
                return (System.Convert.ToBase64String(encrypted));
            }
        }

        /// <summary>
        /// 対称鍵暗号を使って暗号文を復号する
        /// </summary>
        /// <param name="cipher">暗号化された文字列</param>
        /// <param name="iv">対称アルゴリズムの初期ベクター</param>
        /// <param name="key">対称アルゴリズムの共有鍵</param>
        /// <returns>復号された文字列</returns>
        private static string Decrypt(string cipher, string iv, string key)
        {
            using (RijndaelManaged rijndael = new RijndaelManaged())
            {
                rijndael.BlockSize = 128;
                rijndael.KeySize = 128;
                rijndael.Mode = CipherMode.CBC;
                rijndael.Padding = PaddingMode.PKCS7;

                rijndael.IV = Encoding.UTF8.GetBytes(iv);
                rijndael.Key = Encoding.UTF8.GetBytes(key);

                ICryptoTransform decryptor = rijndael.CreateDecryptor(rijndael.Key, rijndael.IV);

                string plain = string.Empty;
                using (MemoryStream mStream = new MemoryStream(System.Convert.FromBase64String(cipher)))
                {
                    using (CryptoStream ctStream = new CryptoStream(mStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(ctStream))
                        {
                            plain = sr.ReadToEnd();
                        }
                    }
                }
                return plain;
            }
        }
    }

    public class UfCamCabinetCpInfo
    {
        public UnitInfo Unit;

        public List<UfCamCorrectionPoint> lstCp;

        public UfCamCabinetCpInfo()
        {
            lstCp = new List<UfCamCorrectionPoint>();
        }

        public static bool LoadFromXmlFile(string path, out List<UfCamCabinetCpInfo> data)
        {
            data = null;

            try
            {
                using (StreamReader sr = new StreamReader(path, new UTF8Encoding(false)))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(List<UfCamCabinetCpInfo>));

                    //読み込んで逆シリアル化する
                    object obj = xs.Deserialize(sr);
                    data = (List<UfCamCabinetCpInfo>)obj;
                }
            }
            catch (Exception ex)
            { throw new Exception(ex.Message); }

            return true;
        }

        public static bool SaveToXmlFile(string path, List<UfCamCabinetCpInfo> data)
        {
            try
            {
                XmlWriterSettings settings = new XmlWriterSettings();

                settings.Indent = true; // インデントを行う
                settings.IndentChars = "\t"; // インデントにタブ文字を用いる
                settings.NewLineOnAttributes = true; // 属性にも改行を行う

                using (XmlWriter writer = XmlWriter.Create(path, settings))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(List<UfCamCabinetCpInfo>));

                    //シリアル化して書き込む
                    xs.Serialize(writer, data);
                }
            }
            catch (Exception ex)
            { throw new Exception(ex.Message); }

            return true;
        }
    }

    public class MATPARAM
    {
        public double Offset;// offset と 最小値は等しいのでoffsetのみ記載
        public double Step;  // stepサイズ
        public double Max;   // 設定可能な最大値
        public int BitMask;  // 対象データのbitサイズに応じたbitMask
        public int Shift;    // 64bitデータにした場合の左シフト数

        public MATPARAM()
        { }

        public MATPARAM(double offset, double step, double max, int bitMask, int shift)
        {
            Offset = offset;
            Step = step;
            Max = max;
            BitMask = bitMask;
            Shift = shift;
        }
    }

    public class CabinetPosition
    {
        public ReferenceCabinetPosition P1_2;
        public ReferenceCabinetPosition P1_5;

        public CabinetPosition()
        {
            P1_2 = new ReferenceCabinetPosition();
            P1_5 = new ReferenceCabinetPosition();
        }

        public static bool LoadFromXmlFile(string path, out CabinetPosition data)
        {
            data = null;

            try
            {
                using (StreamReader sr = new StreamReader(path, new UTF8Encoding(false)))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(CabinetPosition));

                    //読み込んで逆シリアル化する
                    object obj = xs.Deserialize(sr);
                    data = (CabinetPosition)obj;
                }
            }
            catch (Exception ex)
            { throw new Exception(ex.Message); }

            return true;
        }

        public static bool SaveToXmlFile(string path, CabinetPosition data)
        {
            try
            {
                XmlWriterSettings settings = new XmlWriterSettings();

                settings.Indent = true; // インデントを行う
                settings.IndentChars = "\t"; // インデントにタブ文字を用いる
                settings.NewLineOnAttributes = true; // 属性にも改行を行う

                using (XmlWriter writer = XmlWriter.Create(path, settings))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(CabinetPosition));

                    //シリアル化して書き込む
                    xs.Serialize(writer, data);
                }
            }
            catch (Exception ex)
            { throw new Exception(ex.Message); }

            return true;
        }
    }

    public class ReferenceCabinetPosition
    {
        public List<List<MarkerPosition>> lstCabinetPosition;

        public ReferenceCabinetPosition()
        {
            lstCabinetPosition = new List<List<MarkerPosition>>();
        }
    }

    public class CameraPosition
    {
        public double X;
        public double Y;
        public double Z;
        public double Pan;
        public double Tilt;
        public double Roll;

        public CameraPosition()
        {

        }

        public CameraPosition(double x, double y, double z, double pan, double tilt, double roll)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.Pan = pan;
            this.Tilt = tilt;
            this.Roll = roll;
        }
    }

    public class ObjectiveLine
    {
        public bool Top = false;
        public bool Bottom = false;
        public bool Left = false;
        public bool Right = false;

        public ObjectiveLine(bool? top, bool? bottom, bool? left, bool? right)
        {
            this.Top = (bool)top;
            this.Bottom = (bool)bottom;
            this.Left = (bool)left;
            this.Right = (bool)right;
        }
    }

    public class UfCamMeasArea
    {
        public Coordinate Centroid;
        public RectangleArea RectArea;

        public UfCamMeasArea()
        {
            Centroid = new Coordinate();
            RectArea = new RectangleArea();
        }

        public UfCamMeasArea(Coordinate cent, RectangleArea rectArea)
        {
            Centroid = cent;
            RectArea = rectArea;
        }

        public UfCamMeasArea Clone()
        {
            return new UfCamMeasArea(this.Centroid, this.RectArea);
        }
    }

    public class ViewPoint
    {
        public bool Vertical;
        public bool Horizontal;
        public double RefPan;
        public double RefTilt;

        public ViewPoint()
        {
            Vertical = false;
            Horizontal = false;
            RefPan = double.NaN;
            RefTilt = double.NaN;
        }
    }

    #endregion

    #region GapCamera

    public class GapCameraParams
    {
        public string LastBackupFile = "";
        public int MinTargetArea = 0;
        public double MinTargetAreaPercent = 0;

        public int TrimmingSize = 30;
        
        // deleted by Hotta 2022/01/26
        // public int Blanking = 2;

        public double TargetGain = 1.0;

        //added by Hotta 2021/11/19
        public int TrimmingOffset = 2;

        // added by Hotta 2021/12/09
        public int MeasLevel_CModel = 492;
        public int MeasLevel_BModel = 359;

        // added by Hotta 2022/01/26
        public ShootCondition Setting_A7;
        public ShootCondition Setting_A6400;

        public double AdjustSpec = 0.015;

        // added by Hotta 2022/02/10
        public double MoireSpec = 0.56;

        // added by Hotta 2022/08/01
        public double CamPos_SizeMin = 0.91;
        public double CamPos_SizeMax = 1.05;
        //

        // added by Hotta 2022/12/02
        public int CamPos_RetryNum = 3;
        //

        public GapCameraParams()
        {
            Setting_A7 = new ShootCondition();
            Setting_A6400 = new ShootCondition();
        }


    }

    /// <summary>
    /// 目地の補正情報（Unit単位）
    /// </summary>
    public class GapCamCorrectionValue
    {
        public UnitInfo Unit; // Unit情報

        // deleted by Hotta 2022/11/10
        // Cabinetの補正値の取り扱いを中止
        /*
        public GapCellCorrectValue CvUnit; // Gap(Unit)の補正値
        */
        public GapCellCorrectValue[] AryCvCell; // Gap(Cell)の補正値

        public List<GapCamCp> lstUnitCp; // UnitのGap補正点（0/2/4個のはず）
        public List<GapCamCp> lstCellCp; // CellのGap補正点（34個のはず）

        // added by Hotta 2022/06/17 for Cancun
        private GapCamCorrectionValue()
        {

        }

        // modified by Hotta 2022/06/17 for Cancun
        public GapCamCorrectionValue(int moduleCount)
        {
            //AryCvCell = new GapCellCorrectValue[12];

            // modified by Hotta 2022/06/17 for Cancun
            //AryCvCell = new GapCellCorrectValue[8];
            AryCvCell = new GapCellCorrectValue[moduleCount];

            // added by Hotta 2022/01/21
            for (int n=0;n<AryCvCell.Length;n++)
            {
                if (AryCvCell[n] == null)
                    AryCvCell[n] = new GapCellCorrectValue();
                AryCvCell[n].TopLeft = 128;
                AryCvCell[n].TopRight = 128;
                AryCvCell[n].BottomLeft = 128;
                AryCvCell[n].BottomRight = 128;
                AryCvCell[n].RightTop = 128;
                AryCvCell[n].RightBottom = 128;
                AryCvCell[n].LeftTop = 128;
                AryCvCell[n].LeftBottom = 128;
            }
            //


            lstUnitCp = new List<GapCamCp>();
            lstCellCp = new List<GapCamCp>();
        }

        public static bool LoadFromXmlFile(string path, out List<GapCamCorrectionValue> data)
        {
            data = null;

            try
            {
                using (StreamReader sr = new StreamReader(path, new UTF8Encoding(false)))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(List<GapCamCorrectionValue>));

                    //読み込んで逆シリアル化する
                    object obj = xs.Deserialize(sr);                    
                    data = (List<GapCamCorrectionValue>)obj;
                }
            }
            catch (Exception ex)
            { throw new Exception(ex.Message); }

            return true;
        }

        public static bool SaveToXmlFile(string path, List<GapCamCorrectionValue> data)
        {
            try
            {
                XmlWriterSettings settings = new XmlWriterSettings();

                settings.Indent = true; // インデントを行う
                settings.IndentChars = "\t"; // インデントにタブ文字を用いる
                settings.NewLineOnAttributes = true; // 属性にも改行を行う

                using (XmlWriter writer = XmlWriter.Create(path, settings))
                //using (StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(false)))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(List<GapCamCorrectionValue>));

                    //シリアル化して書き込む
                    xs.Serialize(writer, data);
                }
                //sw.Close();
            }
            catch (Exception ex)
            { throw new Exception(ex.Message); }

            return true;
        }
    }

    public class GapCamCp // Cp = Correction Point(調整点)
    {
        public CellNum CellNo; // Cell番号
        public CorrectPosition Pos; // 調整点の位置（隣接している境界線は同じ値を持たなくてはならないため上辺と右辺のみ調整対象）
        // modified by Hotta 2022/06/27
        //public Area[] CamArea; // 画像上の座標
        public OpenCvSharp.RotatedRect[] CamArea; // 画像上の座標
        //
        public AdjustDirection Direction; // 水平線or垂直線（調整点の位置で一意に決まる）
        public int RegOrg; // 目地調整レジスタ値（初期値）
        public int RegAdj; // 目地調整レジスタ値（調整値）
        public double GapGain; // 調整ゲイン（目地が周辺と比べ何倍の明るさになっているか）
        public double Slope;
        public double Offset;
        public double Gap;
        public double Around;
        // deleted by Hotta 2022/02/10
        // public List<double> adjResult;
    }

    public class GapArea
    {
        public Area Area;
        public AdjustDirection Direction;
        public double[] PvR; // V/H方向に積算して求めた画素平均値R
        public double[] PvG;
        public double[] PvB;
    }

    #endregion GapCamera

    #region AdjustmentLocationData

    public static class RelativePosition
    {
        private static CabinetOffset[] relativePositionCabinet = {
            new CabinetOffset(-1, -1), // 左上
            new CabinetOffset( 0, -1), // 上
            new CabinetOffset( 1, -1), // 右上
            new CabinetOffset(-1,  0), // 左
            new CabinetOffset( 0,  0), // 中央
            new CabinetOffset( 1,  0), // 右
            new CabinetOffset(-1,  1), // 左下
            new CabinetOffset( 0,  1), // 下
            new CabinetOffset( 1,  1), // 右下
        };

        private static Tuple<CabinetOffset, WidthOffset>[] relativePositionModule1_Chiron = {
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset(-1, -1), new WidthOffset(3, 1)),   // 左上 (Module No8)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0, -1), new WidthOffset(0, 1)),   // 上   (Module No5)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0, -1), new WidthOffset(1, 1)),   // 右上 (Module No6)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset(-1,  0), new WidthOffset(3, 0)),   // 左   (Module No4)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(0, 0)),   // 中央 (Module No1)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 0)),   // 右   (Module No2)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset(-1,  0), new WidthOffset(3, 1)),   // 左下 (Module No8)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(0, 1)),   // 下   (Module No5)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 1)),   // 右下 (Module No6)
        };

        private static Tuple<CabinetOffset, WidthOffset>[] relativePositionModule2_Chiron = {
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0, -1), new WidthOffset(0, 1)),   // 左上 (Module No5)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0, -1), new WidthOffset(1, 1)),   // 上   (Module No6)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0, -1), new WidthOffset(2, 1)),   // 右上 (Module No7)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(0, 0)),   // 左   (Module No1)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 0)),   // 中央 (Module No2)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 0)),   // 右   (Module No3)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(0, 1)),   // 左下 (Module No5)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 1)),   // 下   (Module No6)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 1)),   // 右下 (Module No7)
        };

        private static Tuple<CabinetOffset, WidthOffset>[] relativePositionModule3_Chiron = {
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0, -1), new WidthOffset(1, 1)),   // 左上 (Module No6)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0, -1), new WidthOffset(2, 1)),   // 上   (Module No7)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0, -1), new WidthOffset(3, 1)),   // 右上 (Module No8)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 0)),   // 左   (Module No2)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 0)),   // 中央 (Module No3)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(3, 0)),   // 右   (Module No4)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 1)),   // 左下 (Module No6)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 1)),   // 下   (Module No7)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(3, 1)),   // 右下 (Module No8)
        };

        private static Tuple<CabinetOffset, WidthOffset>[] relativePositionModule4_Chiron = {
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0, -1), new WidthOffset(2, 1)),   // 左上 (Module No7)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0, -1), new WidthOffset(3, 1)),   // 上   (Module No8)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 1, -1), new WidthOffset(0, 1)),   // 右上 (Module No5)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 0)),   // 左   (Module No3)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(3, 0)),   // 中央 (Module No4)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 1,  0), new WidthOffset(0, 0)),   // 右   (Module No1)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 1)),   // 左下 (Module No7)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(3, 1)),   // 下   (Module No8)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 1,  0), new WidthOffset(0, 1)),   // 右下 (Module No5)
        };

        private static Tuple<CabinetOffset, WidthOffset>[] relativePositionModule5_Chiron = {
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset(-1,  0), new WidthOffset(3, 0)),   // 左上 (Module No4)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(0, 0)),   // 上   (Module No1)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 0)),   // 右上 (Module No2)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset(-1,  0), new WidthOffset(3, 1)),   // 左   (Module No8)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(0, 1)),   // 中央 (Module No5)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 1)),   // 右   (Module No6)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset(-1,  1), new WidthOffset(3, 0)),   // 左下 (Module No4)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  1), new WidthOffset(0, 0)),   // 下   (Module No1)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  1), new WidthOffset(1, 0)),   // 右下 (Module No2)
        };

        private static Tuple<CabinetOffset, WidthOffset>[] relativePositionModule6_Chiron = {
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(0, 0)),   // 左上 (Module No1)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 0)),   // 上   (Module No2)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 0)),   // 右上 (Module No3)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(0, 1)),   // 左   (Module No5)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 1)),   // 中央 (Module No6)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 1)),   // 右   (Module No7)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  1), new WidthOffset(0, 0)),   // 左下 (Module No1)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  1), new WidthOffset(1, 0)),   // 下   (Module No2)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  1), new WidthOffset(2, 0)),   // 右下 (Module No3)
        };

        private static Tuple<CabinetOffset, WidthOffset>[] relativePositionModule7_Chiron = {
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 0)),   // 左上 (Module No2)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 0)),   // 上   (Module No3)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(3, 0)),   // 右上 (Module No4)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 1)),   // 左   (Module No6)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 1)),   // 中央 (Module No7)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(3, 1)),   // 右   (Module No8)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  1), new WidthOffset(1, 0)),   // 左下 (Module No2)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  1), new WidthOffset(2, 0)),   // 下   (Module No3)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  1), new WidthOffset(3, 0)),   // 右下 (Module No4)
        };

        private static Tuple<CabinetOffset, WidthOffset>[] relativePositionModule8_Chiron = {
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 0)),   // 左上 (Module No3)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(3, 0)),   // 上   (Module No4)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 1,  0), new WidthOffset(0, 0)),   // 右上 (Module No1)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 1)),   // 左   (Module No7)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(3, 1)),   // 中央 (Module No8)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 1,  0), new WidthOffset(0, 1)),   // 右   (Module No5)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  1), new WidthOffset(2, 0)),   // 左下 (Module No3)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  1), new WidthOffset(3, 0)),   // 下   (Module No4)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 1,  1), new WidthOffset(0, 0)),   // 右下 (Module No1)
        };

        private static Tuple<CabinetOffset, WidthOffset>[] relativePositionModule1_Cancun = {
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset(-1, -1), new WidthOffset(3, 2)),   // 左上 (Module No12)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0, -1), new WidthOffset(0, 2)),   // 上   (Module No9)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0, -1), new WidthOffset(1, 2)),   // 右上 (Module No10)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset(-1,  0), new WidthOffset(3, 0)),   // 左   (Module No4)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(0, 0)),   // 中央 (Module No1)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 0)),   // 右   (Module No2)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset(-1,  0), new WidthOffset(3, 1)),   // 左下 (Module No8)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(0, 1)),   // 下   (Module No5)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 1)),   // 右下 (Module No6)
        };

        private static Tuple<CabinetOffset, WidthOffset>[] relativePositionModule2_Cancun = {
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0, -1), new WidthOffset(0, 2)),   // 左上 (Module No9)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0, -1), new WidthOffset(1, 2)),   // 上   (Module No10)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0, -1), new WidthOffset(2, 2)),   // 右上 (Module No11)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(0, 0)),   // 左   (Module No1)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 0)),   // 中央 (Module No2)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 0)),   // 右   (Module No3)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(0, 1)),   // 左下 (Module No5)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 1)),   // 下   (Module No6)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 1)),   // 右下 (Module No7)
        };

        private static Tuple<CabinetOffset, WidthOffset>[] relativePositionModule3_Cancun = {
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0, -1), new WidthOffset(1, 2)),   // 左上 (Module No10)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0, -1), new WidthOffset(2, 2)),   // 上   (Module No11)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0, -1), new WidthOffset(3, 2)),   // 右上 (Module No12)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 0)),   // 左   (Module No2)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 0)),   // 中央 (Module No3)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(3, 0)),   // 右   (Module No4)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 1)),   // 左下 (Module No6)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 1)),   // 下   (Module No7)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(3, 1)),   // 右下 (Module No8)
        };

        private static Tuple<CabinetOffset, WidthOffset>[] relativePositionModule4_Cancun = {
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0, -1), new WidthOffset(2, 2)),   // 左上 (Module No11)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0, -1), new WidthOffset(3, 2)),   // 上   (Module No12)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 1, -1), new WidthOffset(0, 2)),   // 右上 (Module No9)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 0)),   // 左   (Module No3)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(3, 0)),   // 中央 (Module No4)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 1,  0), new WidthOffset(0, 0)),   // 右   (Module No1)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 1)),   // 左下 (Module No7)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(3, 1)),   // 下   (Module No8)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 1,  0), new WidthOffset(0, 1)),   // 右下 (Module No5)
        };

        private static Tuple<CabinetOffset, WidthOffset>[] relativePositionModule5_Cancun = {
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset(-1,  0), new WidthOffset(3, 0)),   // 左上 (Module No4)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(0, 0)),   // 上   (Module No1)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 0)),   // 右上 (Module No2)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset(-1,  0), new WidthOffset(3, 1)),   // 左   (Module No8)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(0, 1)),   // 中央 (Module No5)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 1)),   // 右   (Module No6)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset(-1,  0), new WidthOffset(3, 2)),   // 左下 (Module No12)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(0, 2)),   // 下   (Module No9)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 2)),   // 右下 (Module No10)
        };

        private static Tuple<CabinetOffset, WidthOffset>[] relativePositionModule6_Cancun = {
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(0, 0)),   // 左上 (Module No1)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 0)),   // 上   (Module No2)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 0)),   // 右上 (Module No3)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(0, 1)),   // 左   (Module No5)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 1)),   // 中央 (Module No6)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 1)),   // 右   (Module No7)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(0, 2)),   // 左下 (Module No9)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 2)),   // 下   (Module No10)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 2)),   // 右下 (Module No11)
        };

        private static Tuple<CabinetOffset, WidthOffset>[] relativePositionModule7_Cancun = {
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 0)),   // 左上 (Module No2)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 0)),   // 上   (Module No3)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(3, 0)),   // 右上 (Module No4)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 1)),   // 左   (Module No6)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 1)),   // 中央 (Module No7)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(3, 1)),   // 右   (Module No8)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 2)),   // 左下 (Module No10)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 2)),   // 下   (Module No11)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(3, 2)),   // 右下 (Module No12)
        };

        private static Tuple<CabinetOffset, WidthOffset>[] relativePositionModule8_Cancun = {
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 0)),   // 左上 (Module No3)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(3, 0)),   // 上   (Module No4)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 1,  0), new WidthOffset(0, 0)),   // 右上 (Module No1)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 1)),   // 左   (Module No7)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(3, 1)),   // 中央 (Module No8)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 1,  0), new WidthOffset(0, 1)),   // 右   (Module No5)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 2)),   // 左下 (Module No11)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(3, 2)),   // 下   (Module No12)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 1,  0), new WidthOffset(0, 2)),   // 右下 (Module No9)
        };

        private static Tuple<CabinetOffset, WidthOffset>[] relativePositionModule9_Cancun = {
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset(-1,  0), new WidthOffset(3, 1)),   // 左上 (Module No8)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(0, 1)),   // 上   (Module No5)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 1)),   // 右上 (Module No6)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset(-1,  0), new WidthOffset(3, 2)),   // 左   (Module No12)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(0, 2)),   // 中央 (Module No9)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 2)),   // 右   (Module No10)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset(-1,  1), new WidthOffset(3, 0)),   // 左下 (Module No4)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  1), new WidthOffset(0, 0)),   // 下   (Module No1)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  1), new WidthOffset(1, 0)),   // 右下 (Module No2)
        };

        private static Tuple<CabinetOffset, WidthOffset>[] relativePositionModule10_Cancun = {
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(0, 1)),   // 左上 (Module No5)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 1)),   // 上   (Module No6)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 1)),   // 右上 (Module No7)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(0, 2)),   // 左   (Module No9)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 2)),   // 中央 (Module No10)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 2)),   // 右   (Module No11)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  1), new WidthOffset(0, 0)),   // 左下 (Module No1)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  1), new WidthOffset(1, 0)),   // 下   (Module No2)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  1), new WidthOffset(2, 0)),   // 右下 (Module No3)
        };

        private static Tuple<CabinetOffset, WidthOffset>[] relativePositionModule11_Cancun = {
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 1)),   // 左上 (Module No6)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 1)),   // 上   (Module No7)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(3, 1)),   // 右上 (Module No8)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(1, 2)),   // 左   (Module No10)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 2)),   // 中央 (Module No11)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(3, 2)),   // 右   (Module No12)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  1), new WidthOffset(1, 0)),   // 左下 (Module No2)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  1), new WidthOffset(2, 0)),   // 下   (Module No3)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  1), new WidthOffset(3, 0)),   // 右下 (Module No4)
        };

        private static Tuple<CabinetOffset, WidthOffset>[] relativePositionModule12_Cancun = {
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 1)),   // 左上 (Module No7)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(3, 1)),   // 上   (Module No8)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 1,  0), new WidthOffset(0, 1)),   // 右上 (Module No5)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(2, 2)),   // 左   (Module No11)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  0), new WidthOffset(3, 2)),   // 中央 (Module No12)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 1,  0), new WidthOffset(0, 2)),   // 右   (Module No9)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  1), new WidthOffset(2, 0)),   // 左下 (Module No3)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 0,  1), new WidthOffset(3, 0)),   // 下   (Module No4)
            new Tuple<CabinetOffset, WidthOffset>(new CabinetOffset( 1,  1), new WidthOffset(0, 0)),   // 右下 (Module No1)
        };

        public static CabinetOffset GetCabinetOffset(AdjustmentPosition position)
        {
            return relativePositionCabinet[(int)position];
        }

        public static Tuple<CabinetOffset, WidthOffset> GetModuleOffset(CellNum cellNum, AdjustmentPosition position)
        {
            Tuple<CabinetOffset, WidthOffset> offset;
            
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            { offset = GetModuleOffset_Chiron(cellNum, position); }
            else
            { offset = GetModuleOffset_Cancun(cellNum, position); }

            return offset;
        }

        public static Tuple<CabinetOffset, WidthOffset> GetModuleOffset_Chiron(CellNum cellNum, AdjustmentPosition position)
        {
            Tuple<CabinetOffset, WidthOffset> offset;
            switch (cellNum)
            {
                case CellNum.Cell_0:
                    offset = relativePositionModule1_Chiron[(int)position];
                    break;
                case CellNum.Cell_1:
                    offset = relativePositionModule2_Chiron[(int)position];
                    break;
                case CellNum.Cell_2:
                    offset = relativePositionModule3_Chiron[(int)position];
                    break;
                case CellNum.Cell_3:
                    offset = relativePositionModule4_Chiron[(int)position];
                    break;
                case CellNum.Cell_4:
                    offset = relativePositionModule5_Chiron[(int)position];
                    break;
                case CellNum.Cell_5:
                    offset = relativePositionModule6_Chiron[(int)position];
                    break;
                case CellNum.Cell_6:
                    offset = relativePositionModule7_Chiron[(int)position];
                    break;
                case CellNum.Cell_7:
                    offset = relativePositionModule8_Chiron[(int)position];
                    break;
                default:
                    offset = relativePositionModule1_Chiron[(int)position];
                    break;
            }

            return offset;
        }

        public static Tuple<CabinetOffset, WidthOffset> GetModuleOffset_Cancun(CellNum cellNum, AdjustmentPosition position)
        {
            Tuple<CabinetOffset, WidthOffset> offset;
            switch (cellNum)
            {
                case CellNum.Cell_0:
                    offset = relativePositionModule1_Cancun[(int)position];
                    break;
                case CellNum.Cell_1:
                    offset = relativePositionModule2_Cancun[(int)position];
                    break;
                case CellNum.Cell_2:
                    offset = relativePositionModule3_Cancun[(int)position];
                    break;
                case CellNum.Cell_3:
                    offset = relativePositionModule4_Cancun[(int)position];
                    break;
                case CellNum.Cell_4:
                    offset = relativePositionModule5_Cancun[(int)position];
                    break;
                case CellNum.Cell_5:
                    offset = relativePositionModule6_Cancun[(int)position];
                    break;
                case CellNum.Cell_6:
                    offset = relativePositionModule7_Cancun[(int)position];
                    break;
                case CellNum.Cell_7:
                    offset = relativePositionModule8_Cancun[(int)position];
                    break;
                case CellNum.Cell_8:
                    offset = relativePositionModule9_Cancun[(int)position];
                    break;
                case CellNum.Cell_9:
                    offset = relativePositionModule10_Cancun[(int)position];
                    break;
                case CellNum.Cell_10:
                    offset = relativePositionModule11_Cancun[(int)position];
                    break;
                case CellNum.Cell_11:
                    offset = relativePositionModule12_Cancun[(int)position];
                    break;
                default:
                    offset = relativePositionModule1_Cancun[(int)position];
                    break;
            }

            return offset;
        }
    }

    #endregion AdjustmentLocatonData

    #region ReferenceModuleLocationData

    public static class ReferencePosition
    {
        private static Tuple<CabinetOffset, int>[] selectedModule1 = {
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0, -1), 8),   // 上   (Module No9)
            new Tuple<CabinetOffset, int>(new CabinetOffset(-1,  0), 3),   // 左   (Module No4)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 1),   // 右   (Module No2)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 4),   // 下   (Module No5)
        };

        private static Tuple<CabinetOffset, int>[] selectedModule2 = {
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0, -1), 9),   // 上   (Module No10)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 0),   // 左   (Module No1)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 2),   // 右   (Module No3)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 5),   // 下   (Module No6)
        };

        private static Tuple<CabinetOffset, int>[] selectedModule3 = {
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0, -1), 10),  // 上   (Module No11)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 1),   // 左   (Module No2)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 3),   // 右   (Module No4)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 6),   // 下   (Module No7)
        };

        private static Tuple<CabinetOffset, int>[] selectedModule4 = {
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0, -1), 11),  // 上   (Module No12)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 2),   // 左   (Module No3)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 1,  0), 0),   // 右   (Module No1)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 7),   // 下   (Module No8)
        };

        private static Tuple<CabinetOffset, int>[] selectedModule5 = {
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 0),   // 上   (Module No1)
            new Tuple<CabinetOffset, int>(new CabinetOffset(-1,  0), 7),   // 左   (Module No8)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 5),   // 右   (Module No6)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 8),   // 下   (Module No9)
        };

        private static Tuple<CabinetOffset, int>[] selectedModule6 = {
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 1),   // 上   (Module No2)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 4),   // 左   (Module No5)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 6),   // 右   (Module No7)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 9),   // 下   (Module No10)
        };

        private static Tuple<CabinetOffset, int>[] selectedModule7 = {
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 2),   // 上   (Module No3)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 5),   // 左   (Module No6)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 7),   // 右   (Module No8)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 10),  // 下   (Module No11)
        };

        private static Tuple<CabinetOffset, int>[] selectedModule8 = {
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 3),   // 上   (Module No4)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 6),   // 左   (Module No7)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 1,  0), 4),   // 右   (Module No5)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 11),  // 下   (Module No12)
        };

        private static Tuple<CabinetOffset, int>[] selectedModule9 = {
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 4),   // 上   (Module No5)
            new Tuple<CabinetOffset, int>(new CabinetOffset(-1,  0), 11),  // 左   (Module No12)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 9),   // 右   (Module No10)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  1), 0),   // 下   (Module No1)
        };

        private static Tuple<CabinetOffset, int>[] selectedModule10 = {
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 5),   // 上   (Module No6)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 8),   // 左   (Module No9)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 10),  // 右   (Module No11)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  1), 1),   // 下   (Module No2)
        };

        private static Tuple<CabinetOffset, int>[] selectedModule11 = {
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 6),   // 上   (Module No7)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 9),   // 左   (Module No10)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 11),  // 右   (Module No12)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  1), 2),   // 下   (Module No3)
        };

        private static Tuple<CabinetOffset, int>[] selectedModule12 = {
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 7),   // 上   (Module No8)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  0), 10),  // 左   (Module No11)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 1,  0), 8),   // 右   (Module No9)
            new Tuple<CabinetOffset, int>(new CabinetOffset( 0,  1), 3),   // 下   (Module No4)
        };

        public static Tuple<CabinetOffset, int> GetModuleOffset(CellNum cellNum, GainReferenceCellPosition position)
        {
            Tuple<CabinetOffset, int> offset;
            switch (cellNum)
            {
                case CellNum.Cell_0:
                    offset = selectedModule1[(int)position];
                    break;
                case CellNum.Cell_1:
                    offset = selectedModule2[(int)position];
                    break;
                case CellNum.Cell_2:
                    offset = selectedModule3[(int)position];
                    break;
                case CellNum.Cell_3:
                    offset = selectedModule4[(int)position];
                    break;
                case CellNum.Cell_4:
                    offset = selectedModule5[(int)position];
                    break;
                case CellNum.Cell_5:
                    offset = selectedModule6[(int)position];
                    break;
                case CellNum.Cell_6:
                    offset = selectedModule7[(int)position];
                    break;
                case CellNum.Cell_7:
                    offset = selectedModule8[(int)position];
                    break;
                case CellNum.Cell_8:
                    offset = selectedModule9[(int)position];
                    break;
                case CellNum.Cell_9:
                    offset = selectedModule10[(int)position];
                    break;
                case CellNum.Cell_10:
                    offset = selectedModule11[(int)position];
                    break;
                case CellNum.Cell_11:
                    offset = selectedModule12[(int)position];
                    break;
                default:
                    offset = selectedModule1[(int)position];
                    break;
            }
            return offset;
        }
    }

    #endregion ReferenceModuleLocationData

    #endregion Class

    #region Enum

    public enum CommunicationDirection { Send = 0, Receive };

    public enum CellColor { Red, Green, Blue, White };

    public enum CellNum { Cell_0, Cell_1, Cell_2, Cell_3, Cell_4, Cell_5, Cell_6, Cell_7, Cell_8, Cell_9, Cell_10, Cell_11, NA };

    public enum UfAdjustMode { Std, EachCell, MeasureOnly };

    public enum CrossPointPosition { Top, Bottom, Right, Left, Center, Calib, 
        TopLeft, TopRight, BottomLeft, BottomRight,
        RadiatorL_Top, RadiatorR_Top, RadiatorL_Bottom, RadiatorR_Bottom, RadiatorL_Left, RadiatorR_Left, RadiatorL_Right, RadiatorR_Right,
        Module_0, Module_1, Module_2, Module_3, Module_4, Module_5, Module_6, Module_7, Module_8, Module_9, Module_10, Module_11 };

    public enum CursorDirection { X, Y };

    public enum CursorType { WhiteCross, RedSquare };

    public enum CorrectPosition { TopLeft, TopRight, RightTop, RightBottom, BottomLeft, BottomRight, LeftTop, LeftBottom };

    public enum ProfileMode { File, Manual };

    public enum FileDirectory { Backup_Initial, Backup_Previous, Backup_Latest, Backup_Rbin, Temp, CellReplace, NA };

    public enum PowerStatus { Standby, Power_On, Update, Startup, Shutting_Down, Initializing, Unknown };

    public enum BackupMode { Normal, UnitDataOnly, AgingDataOnly, UnitLog, Individual };

    public enum DataType { UnitData, HcData, LcData, AgingData, ExtendedData, HfData };

    public enum AdjustDirection { Horizontally, Vertically };

    public enum AlertLevel { Error, Warning, Normal };

    public enum TestPattern { Raster, Tile, Checker, Cross, HRampInc, HRampDec, VRampInc, VRampDec, ColorBar, Window, Unit, Cell };

    public enum ApplicationMode { Normal, Service, Developer };

    public enum ProfileType { DCS, CAS, NA };

    public enum ColorTemp { D93, D65, D50 };

    public enum MeasurementMode { UnitUniformity, CellUniformity };

    public enum EdgePosition { Edge_1 = 1, Edge_2 = 2, Edge_3 = 3, Edge_4 = 4, Edge_5 = 5, Edge_6 = 6, Edge_7 = 7, Edge_8 = 8, NA = -1 };

    public enum PanelType { Normal, DigitalCinema2017, DigitalCinema2020, NA };

    public enum Gamma { Gamma22, Gamma26, PQ, NA };

    public enum ColorPurpose { ZRD_C12A, ZRD_C15A, ZRD_B12A, ZRD_B15A, ZRD_CH12D, ZRD_CH15D, ZRD_BH12D, ZRD_BH15D, ZRD_CH12D_S3, ZRD_CH15D_S3, ZRD_BH12D_S3, ZRD_BH15D_S3, ConfigCustom, Calib, Relative, NA };

    public enum ConfigChrom { ZRD_C12A = 1, ZRD_C15A, ZRD_B12A, ZRD_B15A, ZRD_CH12D, ZRD_CH15D, ZRD_BH12D, ZRD_BH15D, ZRD_CH12D_S3, ZRD_CH15D_S3, ZRD_BH12D_S3, ZRD_BH15D_S3, Custom, NA };

    public enum ColorAnalyzerModel { CA410, NA };
    
    public enum WriteStatus { Complete = 0, Fail, Writing }

    public enum UserOperation { OK, Cancel, Rewind, Repeat, Skip, None }

    public enum TransType { NFS, FTP }

    //public enum LEDPitchTypes { P1_2, P1_5 }

    public enum LEDModuleConfigurations { Module_4x2, Module_4x3}
    
    public enum Quadrant { Quad_1, Quad_2, Quad_3, Quad_4, NA }

    public enum UfAdjAlgorithm { CommonColor, CommonLed } // カメラUFの調整アルゴリズム、Gainのかけ方が出力色で共通（主LED、補LEDで共通）かLEDで共通か

    public enum HdcpStatusTypes { None, Actived, Inactived }

    public enum ModeOption { Normal = 1, Relative }

    public enum AdjustmentPosition
    {
        TopLeft = 0,
        Top,
        TopRight,
        Left,
        Center,
        Right,
        BottomLeft,
        Bottom,
        BottomRight
    }

    public enum WallForm { Flat = 0, Curve }

    public enum CameraInstallPos { Default, Custom }

    public enum ObjectiveEdge { Top, Bottom, Left, Right }

    public enum GainMeasurementMode { Simple, Full }

    public enum GainAdjustmentMode { AllModules, EachModule }

    public enum GainDataWritingOption { AllModules, NotAdjustedModules }

    public enum GainReferenceCellPosition { Top, Left, Right, Bottom } // 上左右下の順でマトリックス作成したので、変更不可

    public enum TargetData { UD_SerialNumber, LC_ValidDataIndicator, HC_ValidDataIndicator }

    public enum SignalModeTypes { SigOne, SigTwo }

    public enum SignalModeSetTypes { Set, Reset }

    #endregion Enum

    static class ColorAnalyzerModelExt
    {
        // Gender に対する拡張メソッドの定義
        public static string DisplayName(this ColorAnalyzerModel model)
        {
            string[] names = { "CA-410", "N/A" };
            return names[(int)model];
        }
    }
}