using System;
using System.Xml.Serialization;

namespace CAS
{
    public static class GamePadProfile
    {
        /// <summary>
        /// Game Pad用のプロファイル(XML)読み込み
        /// </summary>
        /// <param name="Filename"></param>
        /// <returns></returns>
         public static GamePadData GetGamePadProfile(string Filename)
        {
            GamePadData profiledata = null;

            try
            {
                using (System.IO.FileStream fs = new System.IO.FileStream(Filename, System.IO.FileMode.Open))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(GamePadData));
                    profiledata = (GamePadData)serializer.Deserialize(fs);
                }
            }
            catch (Exception)
            {
                throw new Exception("Game Pad Profile Read Error.");
            }
            return profiledata;
        }

        /// <summary>
        /// Game Pad コマンド用のプロファイル(XML)読み込み
        /// </summary>
        /// <param name="Filename"></param>
        /// <returns></returns>
        public static GamePadCmdData GetGamePadCmdProfile(string Filename)
        {
            GamePadCmdData profiledata = null;

            try
            {
                using (System.IO.FileStream fs = new System.IO.FileStream(Filename, System.IO.FileMode.Open))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(GamePadCmdData));
                    profiledata = (GamePadCmdData)serializer.Deserialize(fs);
                }
            }
            catch (Exception)
            {
                throw new Exception("Game Pad Command Profile Read Error.");
            }
            return profiledata;
        }
        public static　void SetSelecGamePadtProfileIndex(string Filename, uint Index)
        {
            try 
            {
                System.Xml.XmlDocument xmlDoc = new System.Xml.XmlDocument();
                // XMLファイル読込
                xmlDoc.Load(Filename);
                //ルート要素取得
                System.Xml.XmlElement rootElement = xmlDoc.DocumentElement;
                //ルート要素の子要素"SelectProFileIndex"取得
                System.Xml.XmlNodeList nodelist = rootElement.GetElementsByTagName("SelectProFileIndex");
                //指定したタグが存在するか？
                if (nodelist.Count > 0)
                {
                    nodelist.Item(0).InnerText = Index.ToString();
                    //ファイルに保存する
                    xmlDoc.Save(Filename);
                }
            }
            catch (Exception)
            {
                throw new Exception("Game Pad Command Profile Update Error.");
            }
        }
    }
}
