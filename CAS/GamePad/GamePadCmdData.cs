using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace CAS
{
    [XmlRoot("GamePadCmd")]
    public class GamePadCmdData
    {
        /// <summary>
        /// 選択中のGamePad プロファイル
        /// </summary>
        [XmlElement("SelectProFileIndex")]
        public uint SelectProFileIndex { get; set; }

        /// <summary>
        /// 選択対象のプロファイル
        /// </summary>
        [XmlArray("ProFiles")]
        [XmlArrayItem("ProFile")]
        public List<ProFile> ProFiles { get; set; }

        /// <summary>
        /// コマンドを実行する対象タブ
        /// </summary>
        [XmlArray("CommandTabs")]
        [XmlArrayItem("CommandTab")]
        public List<CommandTab> CommandTabs { get; set; }
    }

    public partial class ProFile
    {
        /// <summary>
        /// GamePadのプロファイル名
        /// </summary>
        [XmlElement("GamePadProfile")]
        public string FileName { get; set; }

        /// <summary>
        /// GamePadのプロファイル選択リスト名
        /// </summary>
        [XmlAttribute("name")]
        public string name { get; set; }
    }

    public partial class CommandTab
    {
        /// <summary>
        /// TAB Index
        /// </summary>
        [XmlAttribute("index")]
        public uint index { get; set; }

        /// <summary>
        /// TAB Name
        /// </summary>
        [XmlAttribute("name")]
        public string name { get; set; }

        /// <summary>
        /// コマンド
        /// </summary>
        [XmlArray("Commands")]
        [XmlArrayItem("Command")]
        public List<Command> Commands { get; set; }
    }

    public partial class Command
    {
        /// <summary>
        /// コマンドを実行するボタン（複数押下 区切り文字'+'）
        /// </summary>
        [XmlElement("Button")]
        public string Button { get; set; }

        /// <summary>
        /// 長押し確定時間（イベント回数：SP4で100＝1s程度）
        /// </summary>
        [XmlElement("isLongPush")]
        public string isLongPush { get; set; }

        /// <summary>
        /// 連続実行間隔（イベント回数：SP4で100＝1s程度）
        /// </summary>
        [XmlElement("isContinuePush")]
        public string isContinuePush { get; set; }

        /// <summary>
        /// コマンドID
        /// </summary>
        [XmlAttribute("id")]
        public uint id { get; set; }

        /// <summary>
        /// コマンド名
        /// </summary>
        [XmlAttribute("name")]
        public string name { get; set; }

        /// <summary>
        /// ButtonのKeyID：読み込み時にGamePadプロファイル情報から設定される
        /// </summary>
        public uint KeyId;
    }
}
