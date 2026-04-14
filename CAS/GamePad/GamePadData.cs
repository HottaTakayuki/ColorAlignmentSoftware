using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CAS
{
    [XmlRoot("GamePad")]
    public class GamePadData
    {
        /// <summary>
        /// Game Pad 情報
        /// </summary>
        [XmlElement("GamePadInfo")]
        public GamePadInfo GamePadInfo { get; set; }

        /// <summary>
        /// Game Pad の パラメーター
        /// </summary>
        [XmlElement("GamePadPara")]
        public GamePadPara GamePadPara { get; set; }

        /// <summary>
        /// Game Pad ボタン情報
        /// </summary>
        [XmlArray("Buttons")]
        [XmlArrayItem("Button")]
        public List<ButtonData> Buttons { get; set; }
    }

    public partial class GamePadInfo
    {
        /// <summary>
        /// VenderID HIDデバイスで取得できるVenderID（VID_～）
        /// </summary>
        [XmlElement("VendorId")]
        public string VendorId { get; set; }

        /// <summary>
        /// ProductID HIDデバイスで取得できるProductID（PID_～）
        /// </summary>
        [XmlArray("ProductIds")]
        [XmlArrayItem("ProductId")]
        public List<string> ProductIds { get; set; }

        /// <summary>
        /// Model Nsme
        /// </summary>
        [XmlElement("ModelName")]
        public string ModelName { get; set; }
    }
    public partial class GamePadPara
    {
        /// <summary>
        /// 長押し確定時間（イベント回数：SP4で100＝1s程度）
        /// </summary>
        [XmlElement("LongPushTime")]
        public uint LongPushTime { get; set; }

        /// <summary>
        /// 連続実行間隔（イベント回数：SP4で100＝1s程度）
        /// </summary>
        [XmlElement("ContinuePushTime")]
        public uint ContinuePushTime { get; set; }

        /// <summary>
        /// ボタン確定時間（ボタンが敏感に反応すると複数押下時に誤動作する可能性があるのでその調整用）
        /// </summary>
        [XmlElement("ValidPushTime")]
        public uint ValidPushTime { get; set; }
    }

    public partial class ButtonData
    {
        /// <summary>
        /// HIDデバイスのボタン情報(Data[])
        /// </summary>
        [XmlArray("Datas")]
        [XmlArrayItem("Data")]
        public List<Data> Datas { get; set; }

        /// <summary>
        /// ボタンに対応するKeyID（コード側で識別するため）
        /// </summary>
        [XmlAttribute("keyid")]
        public string keyid { get; set; }

        /// <summary>
        /// ボタン名
        /// </summary>
        [XmlAttribute("name")]
        public string name { get; set; }
    }

    /// <remarks/>
    public partial class Data
    {
        /// <summary>
        /// ボタン判定に必要なHIDデバイスのData[]の配列の添字
        /// </summary>
        [XmlElement("Number")]
        public byte Number { get; set; }

        /// <summary>
        /// ボタン判定の値
        /// </summary>
        [XmlElement("Value")]
        public string Value { get; set; }

        /// <summary>
        /// 判定値にMaskしてボタン押下判定をする場合に設定される
        /// </summary>
        [XmlElement("Mask")]
        public string Mask { get; set; }
    }
}