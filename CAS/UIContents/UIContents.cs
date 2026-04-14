namespace CAS
{
    /// <summary>
    /// UI上の文言定義クラス
    /// </summary>
    public class UIContents
    {
        private static UIContents _instance;
        public static UIContents Instance
        {
            get
            {
                if(_instance == null)
                {
                    _instance = new UIContents();
                }
                return _instance;
            }
        }

        // Binding対象の文言を以下に定義

        public string ZRD_C12A_Content
        {
            get
            {
                return MainWindow.ZRD_C12A;
            }
        }
        public string ZRD_C15A_Content
        {
            get
            {
                return MainWindow.ZRD_C15A;
            }
        }
        public string ZRD_B12A_Content
        {
            get
            {
                return MainWindow.ZRD_B12A;
            }
        }
        public string ZRD_B15A_Content
        {
            get
            {
                return MainWindow.ZRD_B15A;
            }
        }
        public string ZRD_CH12D_Content
        {
            get
            {
                return MainWindow.ZRD_CH12D;
            }
        }
        public string ZRD_CH15D_Content
        {
            get
            {
                return MainWindow.ZRD_CH15D;
            }
        }
        public string ZRD_BH12D_Content
        {
            get
            {
                return MainWindow.ZRD_BH12D;
            }
        }
        public string ZRD_BH15D_Content
        {
            get
            {
                return MainWindow.ZRD_BH15D;
            }
        }
        public string ZRD_CH12D_S3_Content
        {
            get
            {
                return MainWindow.ZRD_CH12D_S3;
            }
        }
        public string ZRD_CH15D_S3_Content
        {
            get
            {
                return MainWindow.ZRD_CH15D_S3;
            }
        }
        public string ZRD_BH12D_S3_Content
        {
            get
            {
                return MainWindow.ZRD_BH12D_S3;
            }
        }
        public string ZRD_BH15D_S3_Content
        {
            get
            {
                return MainWindow.ZRD_BH15D_S3;
            }
        }
        public string CUSTOM_Content
        {
            get
            {
                return MainWindow.CUSTOM;
            }
        }
    }
}
