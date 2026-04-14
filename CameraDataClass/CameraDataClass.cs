using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Security.Cryptography;

namespace CameraDataClass
{
    #region Constants

    #endregion Constants

    #region Enum

    public enum UfCamAdjustType { Cabinet, EachModule, Radiator, Cabi_9pt } // Cabinet = Default

    public enum UfCamCorrectPos { Cabinet_Top, Cabinet_Bottom, Cabinet_Left, Cabinet_Right,
        Radiator_L_Top, Radiator_L_Bottom, Radiator_L_Left, Radiator_L_Right, Radiator_R_Top, Radiator_R_Bottom, Radiator_R_Left, Radiator_R_Right,
        Module_0, Module_1, Module_2, Module_3, Module_4, Module_5, Module_6, Module_7, Module_8, Module_9, Module_10, Module_11, Reference, unknown,
        _9pt_TopLeft, _9pt_TopRight, _9pt_Center, _9pt_BottomLeft, _9pt_BottomRight
    }

    #endregion

    #region Class

    public class ShootCondition
    {
        public int ImageSize = 1;
        public string FNumber = "";
        public string Shutter = "";
        public string ISO = "";
        public string WB = "";
        public uint CompressionType = 16;

        public ShootCondition()
        {

        }

        // added by Hotta 2022/06/087 for ライブビュー
        public ShootCondition(ShootCondition condition)
        {
            ImageSize = condition.ImageSize;
            FNumber = condition.FNumber;
            Shutter = condition.Shutter;
            ISO = condition.ISO;
            WB = condition.WB;
            CompressionType = condition.CompressionType;
        }

        public bool Equals(ShootCondition condition)
        {
            bool status = true;

            if (ImageSize != condition.ImageSize)
                status = false;
            if (FNumber != condition.FNumber)
                status = false;
            if (Shutter != condition.Shutter)
                status = false;
            if (ISO != condition.ISO)
                status = false;
            if (WB != condition.WB)
                status = false;
            if (CompressionType != condition.CompressionType)
                status = false;

            return status;
        }
        //
    }

    // added by Hotta 2022/05/18 for AFエリア
    public class AfAreaSetting
    {
        /// <summary>
        /// "Wide", "Zone", "Center", "FlexibleSpotS", "FlexibleSpotM", "FlexibleSpotL"
        /// </summary>
        public string focusAreaType = "Center";
        /// <summary>
        /// S : 65 to 574, M : 79 to 560, L : 94 to 545
        /// </summary>
        public ushort focusAreaX = 320;
        /// <summary>
        /// FlexibleSpotS : 53 to 374, FlexibleSpotM : 63 to 364, FlexibleSpotL : 73 to 354
        /// </summary>
        public ushort focusAreaY = 210;

        public AfAreaSetting()
        {
            focusAreaType = "Center";
        }

        public int FlexibleSpotXMin()
        {
            if (focusAreaType == "FlexibleSpotS")
                return 65;
            else if (focusAreaType == "FlexibleSpotM")
                return 79;
            else if (focusAreaType == "FlexibleSpotL")
                return 94;
            else
                return 0;
        }

        public int FlexibleSpotXMax()
        {
            if (focusAreaType == "FlexibleSpotS")
                return 574;
            else if (focusAreaType == "FlexibleSpotM")
                return 560;
            else if (focusAreaType == "FlexibleSpotL")
                return 545;
            else
                return 0;
        }

        public int FlexibleSpotYMin()
        {
            if (focusAreaType == "FlexibleSpotS")
                return 53;
            else if (focusAreaType == "FlexibleSpotM")
                return 63;
            else if (focusAreaType == "FlexibleSpotL")
                return 73;
            else
                return 0;
        }

        public int FlexibleSpotYMax()
        {
            if (focusAreaType == "FlexibleSpotS")
                return 374;
            else if (focusAreaType == "FlexibleSpotM")
                return 364;
            else if (focusAreaType == "FlexibleSpotL")
                return 354;
            else
                return 0;
        }
    }
    //

    public class ReferencePosition
    {
        public ReferenceMarkerPosition P1_2;
        public ReferenceMarkerPosition P1_5;

        public ReferencePosition()
        {
            P1_2 = new ReferenceMarkerPosition();
            P1_5 = new ReferenceMarkerPosition();
        }

        public static bool LoadFromXmlFile(string path, out ReferencePosition data)
        {
            data = null;

            try
            {
                using (StreamReader sr = new StreamReader(path, new UTF8Encoding(false)))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(ReferencePosition));

                    //読み込んで逆シリアル化する
                    object obj = xs.Deserialize(sr);
                    data = (ReferencePosition)obj;
                }
            }
            catch (Exception ex)
            { throw new Exception(ex.Message); }

            return true;
        }

        public static bool SaveToXmlFile(string path, ReferencePosition data)
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
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(ReferencePosition));

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

    public class ReferenceMarkerPosition
    {
        public MarkerPosition Quad_1;
        public MarkerPosition Quad_2;
        public MarkerPosition Quad_3;
        public MarkerPosition Quad_4;

        public ReferenceMarkerPosition()
        {
            Quad_1 = new MarkerPosition();
            Quad_2 = new MarkerPosition();
            Quad_3 = new MarkerPosition();
            Quad_4 = new MarkerPosition();
        }
    }

    public class MarkerPosition
    {
        public int X;
        public int Y;
        public Coordinate TopLeft;
        public Coordinate TopRight;
        public Coordinate BottomLeft;
        public Coordinate BottomRight;

        public MarkerPosition()
        {
            TopLeft = new Coordinate();
            TopRight = new Coordinate();
            BottomLeft = new Coordinate();
            BottomRight = new Coordinate();
        }

        public MarkerPosition(int x, int y)
        {
            X = x;
            Y = y;
            TopLeft = new Coordinate();
            TopRight = new Coordinate();
            BottomLeft = new Coordinate();
            BottomRight = new Coordinate();
        }
    }

    public class Coordinate
    {
        public double X;
        public double Y;

        public Coordinate() { }

        public Coordinate(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// Cabinetの空間座標[mm]
    /// </summary>
    public class CabinetCoordinate
    {
        public SpatialCoordinate BottomLeft; // Cabinetの原点
        public SpatialCoordinate BottomRight;
        public SpatialCoordinate TopLeft;
        public SpatialCoordinate TopRight
        {
            get { return new SpatialCoordinate(BottomRight.x + (TopLeft.x - BottomLeft.x), BottomRight.y + (TopLeft.y - BottomLeft.y), BottomRight.z + (TopLeft.z - BottomLeft.z)); }
        }

        public CabinetCoordinate()
        {
            BottomLeft = new SpatialCoordinate();
            BottomRight = new SpatialCoordinate();
            TopLeft = new SpatialCoordinate();
        }
    }

    /// <summary>
    /// 直交座標(空間座標) 単位[mm]
    /// カメラ画像の横方向がX(右が+)、縦方向がY(上が+)、奥行方向がZ(カメラから遠ざかる方向が+)
    /// </summary>
    public class SpatialCoordinate
    {
        public double x;
        public double y;
        public double z;

        public SpatialCoordinate()
        {

        }

        public SpatialCoordinate(SpatialCoordinate sc)
        {
            this.x = sc.x;
            this.y = sc.y;
            this.z = sc.z;
        }

        public SpatialCoordinate(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    /// <summary>
    /// 極座標 単位[deg]
    /// </summary>
    public class PolarCoordinate
    {
        public double Pan;
        public double Tilt;
        public double r; // 直線距離(多分使わない)

        public PolarCoordinate() { }

        public PolarCoordinate(double pan, double tilt, double r)
        {
            this.Pan = pan;
            this.Tilt = tilt;
            this.r = r;
        }
    }

    public class Area // 撮影範囲、パネル画素数で指定
    {
        public Coordinate StartPos;
        public int Width;
        public int Height;

        public Coordinate LeftBottom
        {
            get { return new Coordinate(StartPos.X, StartPos.Y + Height); }
        }

        public Coordinate RightTop
        {
            get { return new Coordinate(StartPos.X + Width, StartPos.Y); }
        }

        public Coordinate EndPos
        {
            get { return new Coordinate(StartPos.X + Width, StartPos.Y + Height); }
        }

        public Coordinate CenterPos
        {
            get { return new Coordinate(StartPos.X + (double)Width / 2, StartPos.Y + (double)Height / 2); }
        }

        public Area()
        {
            StartPos = new Coordinate();
        }

        public Area(int x, int y, int height, int width)
        {
            StartPos = new Coordinate(x, y);
            Height = height;
            Width = width;
        }
    }

    public class RectangleArea
    {
        public Coordinate TopLeft;
        public Coordinate TopRight;
        public Coordinate BottomLeft;
        public Coordinate BottomRight;

        public RectangleArea()
        {
            TopLeft = new Coordinate();
            TopRight = new Coordinate();
            BottomLeft = new Coordinate();
            BottomRight = new Coordinate();
        }
    }

    // カメラ視野角特性補正用データ
    public class CameraCorrectionData
    {
        public List<CameraCorrectionValue> lstCamCorrectValues;
        public string SHA256Hash = ""; // ファイルのSHA256ハッシュ

        public CameraCorrectionData()
        {
            lstCamCorrectValues = new List<CameraCorrectionValue>();
        }

        public static bool LoadFromXmlFile(string path, out CameraCorrectionData data)
        {
            data = null;

            try
            {
                using (StreamReader sr = new StreamReader(path, new UTF8Encoding(false)))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(CameraCorrectionData));

                    //読み込んで逆シリアル化する
                    object obj = xs.Deserialize(sr);
                    data = (CameraCorrectionData)obj;
                }
            }
            catch (Exception ex)
            { throw new Exception(ex.Message); }

            return true;
        }

        public static bool SaveToXmlFile(string path, CameraCorrectionData data)
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
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(CameraCorrectionData));

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

    public class CameraCorrectionValue
    {
        public Coordinate CenterCoordinate; // 補正エリアの中心座標
        //public PolarCoordinate PolarCoodinate; // 極座標(Pan, Tilt)
        public UfCamAdjustType Type; // 補正点のタイプ
        public UfCamCorrectPos CorrectionPoint; // 補正点の位置
        public int CabinetX; // Cabinet座標H 1-10(8)
        public int CabinetY; // Cabinet座標V 1-10(8)

        public double CvGainR = 1.0; // 視野角特性補正値 R
        public double CvGainG = 1.0; // 視野角特性補正値 G
        public double CvGainB = 1.0; // 視野角特性補正値 B

        public string CcdHash = "";
        public string LcdHash = "";

        public CameraCorrectionValue()
        {
            CenterCoordinate = new Coordinate();
            //PolarCoodinate = new PolarCoordinate();
            Type = UfCamAdjustType.Cabinet;
            CorrectionPoint = UfCamCorrectPos.unknown;
        }
    }

    public class UfCamCpAngle
    {
        public UfCamCorrectPos CorrectPos;
        public PolarCoordinate Angle;

        public UfCamCpAngle()
        {
            CorrectPos = UfCamCorrectPos.unknown;
            Angle = new PolarCoordinate();
        }
    }

    public class LedCorrectionData
    {
        public List<LedCorrectionValue> lstCamCorrectValues;
        public string SHA256Hash = "";

        public LedCorrectionData()
        {
            lstCamCorrectValues = new List<LedCorrectionValue>();
        }

        public static bool LoadFromXmlFile(string path, out LedCorrectionData data)
        {
            data = null;

            try
            {
                using (StreamReader sr = new StreamReader(path, new UTF8Encoding(false)))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(LedCorrectionData));

                    //読み込んで逆シリアル化する
                    object obj = xs.Deserialize(sr);
                    data = (LedCorrectionData)obj;
                }
            }
            catch (Exception ex)
            { throw new Exception(ex.Message); }

            return true;
        }

        public static bool SaveToXmlFile(string path, LedCorrectionData data)
        {
            try
            {
                XmlWriterSettings settings = new XmlWriterSettings();

                settings.Indent = true; // インデントを行う
                settings.IndentChars = "\t"; // インデントにタブ文字を用いる
                settings.NewLineOnAttributes = true; // 属性にも改行を行う

                using (XmlWriter writer = XmlWriter.Create(path, settings))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(LedCorrectionData));
                    xs.Serialize(writer, data);
                }
            }
            catch (Exception ex)
            { throw new Exception(ex.Message); }

            return true;
        }
    }

    public class LedCorrectionValue
    {
        public double PanAngle = 0.0;
        public double TiltAngle = 0.0; //
        public double CvGainR = 1.0; // 視野角特性補正値 R
        public double CvGainG = 1.0; // 視野角特性補正値 G
        public double CvGainB = 1.0; // 視野角特性補正値 B

        public LedCorrectionValue()
        {
        }
    }

    public class PixelValue
    {
        public double R;
        public double G;
        public double B;

        public PixelValue()
        {

        }

        public PixelValue(double r, double g, double b)
        {
            this.R = r;
            this.G = g;
            this.B = b;
        }
    }

    public class TrimmingArea
    {
        public string PositionName = "";
        public Area Area;
        public UfCamAdjustType Type = UfCamAdjustType.Cabinet; // 補正点のタイプ
        public UfCamCorrectPos CorrectionPoint = UfCamCorrectPos.unknown; // 補正点の位置
        public int CabinetX; // Cabinet座標H 1-10(8)
        public int CabinetY; // Cabinet座標V 1-10(8)

        public TrimmingArea()
        {
            Area = new Area();
        }

        public TrimmingArea(string posName, Area area, UfCamAdjustType type = UfCamAdjustType.Cabinet, UfCamCorrectPos pos = UfCamCorrectPos.unknown)
        {
            this.PositionName = posName;
            this.Area = area;
            this.Type = type;
            this.CorrectionPoint = pos;
        }
    }

    public class CameraControlData
    {
        //public const string CameraControlFile = "\\CamCont.xml";
        //private const string AES_IV = @"k49Z854QR8jDXVfW";
        //private const string AES_Key = @"SG7YpTYgcrd7P5Uh";

        public Comment comment;
        public ShootCondition Condition;
        public string ImgPath = "";
        public bool ShootFlag = false;
        public bool CloseFlag = false;
        // added by Hotta 2021/12/03
        public bool AutoFocusFlag = false;
        //

        // added by Hotta 2022/05/18 for AFエリア
        public AfAreaSetting AfArea;
        //

        // added by Hotta 2022/06/06 for ライブビュー
        /// <summary>
        /// 0 : Off, 1 : Single, 2 : Continuous
        /// </summary>
        public int LiveViewFlag = 0;
        //

        public CameraControlData()
        {
            comment = new Comment();
            Condition = new ShootCondition();

            // added by Hotta 2022/05/18 for AFエリア
            AfArea = new AfAreaSetting();

            // added by Hotta 2022/06/06 for ライブビュー
            LiveViewFlag = 0;
        }

        public CameraControlData(ShootCondition condition)
        {
            Condition = condition;
        }

        // added by Hotta 2022/05/18 for AFエリア
        public CameraControlData(ShootCondition condition, AfAreaSetting afArea)
        {
            Condition = condition;
            AfArea = afArea;
        }


        public static bool LoadFromXmlFile(string path, out CameraControlData data)
        {
            int count = 0;

            data = null;            

            while (true)
            {
                try
                {
                    using (StreamReader sr = new StreamReader(path, new UTF8Encoding(false)))
                    {
                        System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(CameraControlData));

                        //読み込んで逆シリアル化する
                        object obj = xs.Deserialize(sr);
                        data = (CameraControlData)obj;
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

        public static bool LoadFromEncryptFile(string path, out CameraControlData data, string iv, string key)
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

                        System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(CameraControlData));

                        using (StreamReader stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(text))))
                        {
                            //読み込んで逆シリアル化する
                            object obj = xs.Deserialize(stream);
                            data = (CameraControlData)obj;
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

        public static bool SaveToXmlFile(string path, CameraControlData data)
        {
            int count = 0;

            while (true)
            {
                try
                {
                    XmlWriterSettings settings = new XmlWriterSettings();

                    settings.Indent = true; // インデントを行う
                    settings.IndentChars = "\t"; // インデントにタブ文字を用いる
                    settings.NewLineOnAttributes = true; // 属性にも改行を行う

                    using (XmlWriter writer = XmlWriter.Create(path, settings))
                    {
                        System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(CameraControlData));

                        //シリアル化して書き込む
                        xs.Serialize(writer, data);
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

        public static bool SaveToEncryptFile(string path, CameraControlData data, string iv, string key)
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
                        System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(CameraControlData));

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

        //private static string CalcKey(string key)
        //{
        //    string newKey = "";

        //    newKey = new string(key.Substring(8, 8).Reverse().ToArray()) + key.Substring(0, 8).Replace(key.Substring(3, 1), "@");

        //    return newKey;
        //}

        /// <summary>
        /// 対称鍵暗号を使って文字列を暗号化する
        /// </summary>
        /// <param name="text">暗号化する文字列</param>
        /// <param name="iv">対称アルゴリズムの初期ベクター</param>
        /// <param name="key">対称アルゴリズムの共有鍵</param>
        /// <returns>暗号化された文字列</returns>
        public static string Encrypt(string text, string iv, string key)
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
        public static string Decrypt(string cipher, string iv, string key)
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

    #region AccSettings

    [Serializable()]
    public class AccSettings
    {
        public static string ErrorMessage;

        public Comment comment;
        public CommonParams Common;
        public string CameraControlFile = "";

        #region Methods

        public AccSettings()
        {
            comment = new Comment();
            Common = new CommonParams();
        }

        [NonSerialized()]
        private static AccSettings _instance;

        [System.Xml.Serialization.XmlIgnore]
        public static AccSettings Ins
        {
            get
            {
                if (_instance == null)
                    _instance = new AccSettings();
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
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(AccSettings));

                    //読み込んで逆シリアル化する
                    object obj = xs.Deserialize(sr);                 
                    Ins = (AccSettings)obj;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.ToString();
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
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(AccSettings));

                    //読み込んで逆シリアル化する
                    object obj = xs.Deserialize(sr);
                    Ins = (AccSettings)obj;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.ToString();
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
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(AccSettings));

                    //シリアル化して書き込む
                    xs.Serialize(writer, Ins);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.ToString();
                return false;
            }

            return true;
        }

        public static bool SaveToXmlFile(string path)
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
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(AccSettings));

                    //シリアル化して書き込む
                    xs.Serialize(writer, Ins);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.ToString();
                return false;
            }

            return true;
        }

        private static string GetSettingPath()
        {
            string currentDir = System.Environment.CurrentDirectory;
            string filePath = currentDir + "\\Components\\AccSettings.xml";

            return filePath;
        }

        #endregion
    }

    public class Comment // XMLの中にコメント書いても消えちゃうと思うのでパラメータにして一緒に格納しておく
    {
        public string Camera = "対応機種 : ILCE-7RM3, ILCE-6400";
        public string F_Number = "右記から選択して記入 ： F2.8, F3.2, F3.5, F4.0, F4.5, F5.0, F5.6, F6.3, F7.1, F8.0, F9.0, F10, F11, F13, F14, F16, F18, F20, F22";
        public string Shutter = "右記から選択して記入 ： 1/8000, 1/6400, 1/5000, 1/4000, 1/3200, 1/2500, 1/2000, 1/1600, 1/1250, 1/1000, 1/800, 1/640, 1/500, 1/400, 1/320, 1/250, 1/200, 1/160, 1/125, 1/100, 1/80, 1/60, 1/50, 1/40, 1/30, 1/25, 1/20, 1/15, 1/13, 1/10, 1/8, 1/6, 1/5, 1/4, 1/3, 0.4, 0.5, 0.6, 0.8, 1, 1.3, 1.6, 2, 2.5, 3.2, 4, 5, 6, 8, 10, 13, 15, 20, 25, 30, BULB";
        public string ISO = "右記から選択して記入 ： AUTO, 50, 64, 80, 100, 125, 160, 200, 250, 320, 400, 500, 640, 800, 1000, 1250, 1600, 2000, 2500, 3200, 4000, 5000, 6400, 8000, 10000, 12800, 16000, 20000, 25600, 32000, 40000, 51200, 64000, 80000, 102400";
    }

    public class CommonParams
    {
        public string CameraName = "";
        public int CameraWait = 0;
    }

    #endregion AccSettings   

    #endregion Class
}
