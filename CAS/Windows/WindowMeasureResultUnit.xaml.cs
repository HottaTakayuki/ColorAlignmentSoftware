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
using System.Windows.Shapes;
using ClosedXML.Excel;
using System.Data;


namespace CAS.Windows
{
    /// <summary>
    /// WindowMeasureResultUnit.xaml の相互作用ロジック
    /// </summary>
    public partial class WindowMeasureResultUnit : Window
    {
        private HeaderInfo header;
        private string path;
        private int count;
            
        private Brush excelentColor;
        private Brush goodColor;
        private Brush noGoodColor;

        // データテーブル
        private DataTable m_dt;

        public WindowMeasureResultUnit(HeaderInfo headerInfo, string filePath, int unitCount)
        {
            InitializeComponent();

            excelentColor = txbExcelent.Background;
            goodColor = txbGood.Background;
            noGoodColor =txbNoGood.Background;

            header = headerInfo;
            path = filePath;
            count = unitCount;

            lbDate.Content = "Date : " + header.Date.ToString("yyyy/MM/dd");
            lbModel.Content = "Model : " + header.Model;
            //lbColorTemp.Content = "Color Temperature : " + header.ColorTemp.ToString();

            string text = "Color";
            //string text = "Color     (";
            //if (header.ColorTemp == ColorTemp.D93)
            //{
            //    text += "D93)";
            //    txbTargetWhiteX.Text = "0.283";
            //    txbTargetWhiteY.Text = "0.297";
            //}
            //else if (header.ColorTemp == ColorTemp.D65)
            //{
            //    text += "D65)";
            //    txbTargetWhiteX.Text = "0.313";
            //    txbTargetWhiteY.Text = "0.329";
            //}
            //else
            //{
            //    text += "D50)";
            //    txbTargetWhiteX.Text = "0.346";
            //    txbTargetWhiteY.Text = "0.359";
            //}

            txbColor.Text = text;

            try { loadResultFile(); }
            catch { }
        }

        private void loadResultFile()
        {            
            using (var workbook = new XLWorkbook(path))
            {

	            // (All)
	            var worksheet = workbook.Worksheet(3);

#if NO_SET
#else
	            // Ave.
	            txbAve_0.Text = (Convert.ToDouble(worksheet.Cell(15, 8).Value.ToString())).ToString("F1");
	            txbAve_1.Text = (Convert.ToDouble(worksheet.Cell(16, 8).Value.ToString())).ToString("F1");
	            txbAve_2.Text = (Convert.ToDouble(worksheet.Cell(17, 8).Value.ToString())).ToString("F1");
	            txbAve_3.Text = (Convert.ToDouble(worksheet.Cell(18, 8).Value.ToString())).ToString("F1");
	            txbAve_4.Text = (Convert.ToDouble(worksheet.Cell(19, 8).Value.ToString())).ToString("F3");
	            txbAve_5.Text = (Convert.ToDouble(worksheet.Cell(20, 8).Value.ToString())).ToString("F3");
	            txbAve_6.Text = (Convert.ToDouble(worksheet.Cell(21, 8).Value.ToString())).ToString("F3");
	            txbAve_7.Text = (Convert.ToDouble(worksheet.Cell(22, 8).Value.ToString())).ToString("F3");
	            txbAve_8.Text = (Convert.ToDouble(worksheet.Cell(23, 8).Value.ToString())).ToString("F3");
	            txbAve_9.Text = (Convert.ToDouble(worksheet.Cell(24, 8).Value.ToString())).ToString("F3");
	            txbAve_10.Text = (Convert.ToDouble(worksheet.Cell(25, 8).Value.ToString())).ToString("F3");
	            txbAve_11.Text = (Convert.ToDouble(worksheet.Cell(26, 8).Value.ToString())).ToString("F3");
	
	            // Sigma
	            txbSigma_0.Text = (Convert.ToDouble(worksheet.Cell(13, 9).Value.ToString())).ToString("F2") + "%";
	            txbSigma_1.Text = (Convert.ToDouble(worksheet.Cell(14, 9).Value.ToString())).ToString("F4");
	            txbSigma_2.Text = (Convert.ToDouble(worksheet.Cell(15, 9).Value.ToString())).ToString("F2") + "%";
	            txbSigma_3.Text = (Convert.ToDouble(worksheet.Cell(16, 9).Value.ToString())).ToString("F2") + "%";
	            txbSigma_4.Text = (Convert.ToDouble(worksheet.Cell(17, 9).Value.ToString())).ToString("F2") + "%";
	            txbSigma_5.Text = (Convert.ToDouble(worksheet.Cell(18, 9).Value.ToString())).ToString("F2") + "%";
	            txbSigma_6.Text = (Convert.ToDouble(worksheet.Cell(19, 9).Value.ToString())).ToString("F4");
	            txbSigma_7.Text = (Convert.ToDouble(worksheet.Cell(20, 9).Value.ToString())).ToString("F4");
	            txbSigma_8.Text = (Convert.ToDouble(worksheet.Cell(21, 9).Value.ToString())).ToString("F4");
	            txbSigma_9.Text = (Convert.ToDouble(worksheet.Cell(22, 9).Value.ToString())).ToString("F4");
	            txbSigma_10.Text = (Convert.ToDouble(worksheet.Cell(23, 9).Value.ToString())).ToString("F4");
	            txbSigma_11.Text = (Convert.ToDouble(worksheet.Cell(24, 9).Value.ToString())).ToString("F4");
	            txbSigma_12.Text = (Convert.ToDouble(worksheet.Cell(25, 9).Value.ToString())).ToString("F4");
	            txbSigma_13.Text = (Convert.ToDouble(worksheet.Cell(26, 9).Value.ToString())).ToString("F4");
	
	            // Score (Variation)
	            //txbVariation_0.Text = ((int)(Convert.ToDouble(worksheet.Cell("L13").Value.ToString()))).ToString();
	            //txbVariation_1.Text = ((int)(Convert.ToDouble(worksheet.Cell("L14").Value.ToString()))).ToString();
	            //txbVariation_2.Text = ((int)(Convert.ToDouble(worksheet.Cell("L18").Value.ToString()))).ToString();
	            //txbVariation_3.Text = ((int)(Convert.ToDouble(worksheet.Cell("L19").Value.ToString()))).ToString();
	            //txbVariation_4.Text = ((int)(Convert.ToDouble(worksheet.Cell("L20").Value.ToString()))).ToString();
	            //txbVariation_5.Text = ((int)(Convert.ToDouble(worksheet.Cell("L21").Value.ToString()))).ToString();
	            //txbVariation_6.Text = ((int)(Convert.ToDouble(worksheet.Cell("L22").Value.ToString()))).ToString();
	            //txbVariation_7.Text = ((int)(Convert.ToDouble(worksheet.Cell("L23").Value.ToString()))).ToString();
	            //txbVariation_8.Text = ((int)(Convert.ToDouble(worksheet.Cell("L24").Value.ToString()))).ToString();
	            //txbVariation_9.Text = ((int)(Convert.ToDouble(worksheet.Cell("L25").Value.ToString()))).ToString();
	            //txbVariation_10.Text = ((int)(Convert.ToDouble(worksheet.Cell("L26").Value.ToString()))).ToString();
	
	            setValueWithColor(txbVariation_0, Convert.ToDouble(worksheet.Cell("L13").Value.ToString()));
	            setValueWithColor(txbVariation_1, Convert.ToDouble(worksheet.Cell("L14").Value.ToString()));
	            setValueWithColor(txbVariation_2, Convert.ToDouble(worksheet.Cell("L18").Value.ToString()));
	            setValueWithColor(txbVariation_3, Convert.ToDouble(worksheet.Cell("L19").Value.ToString()));
	            setValueWithColor(txbVariation_4, Convert.ToDouble(worksheet.Cell("L20").Value.ToString()));
	            setValueWithColor(txbVariation_5, Convert.ToDouble(worksheet.Cell("L21").Value.ToString()));
	            setValueWithColor(txbVariation_6, Convert.ToDouble(worksheet.Cell("L22").Value.ToString()));
	            setValueWithColor(txbVariation_7, Convert.ToDouble(worksheet.Cell("L23").Value.ToString()));
	            setValueWithColor(txbVariation_8, Convert.ToDouble(worksheet.Cell("L24").Value.ToString()));
	            setValueWithColor(txbVariation_9, Convert.ToDouble(worksheet.Cell("L25").Value.ToString()));
	            setValueWithColor(txbVariation_10, Convert.ToDouble(worksheet.Cell("L26").Value.ToString()));
	
	            // Score (Value)
	            //txbValue_0.Text = ((int)(Convert.ToDouble(worksheet.Cell("M18").Value.ToString()))).ToString();
	            //txbValue_1.Text = ((int)(Convert.ToDouble(worksheet.Cell("M19").Value.ToString()))).ToString();
	            //txbValue_2.Text = ((int)(Convert.ToDouble(worksheet.Cell("M20").Value.ToString()))).ToString();
	            //txbValue_3.Text = ((int)(Convert.ToDouble(worksheet.Cell("M21").Value.ToString()))).ToString();
	            //txbValue_4.Text = ((int)(Convert.ToDouble(worksheet.Cell("M22").Value.ToString()))).ToString();
	            //txbValue_5.Text = ((int)(Convert.ToDouble(worksheet.Cell("M23").Value.ToString()))).ToString();
	            //txbValue_6.Text = ((int)(Convert.ToDouble(worksheet.Cell("M24").Value.ToString()))).ToString();
	            //txbValue_7.Text = ((int)(Convert.ToDouble(worksheet.Cell("M25").Value.ToString()))).ToString();
	            //txbValue_8.Text = ((int)(Convert.ToDouble(worksheet.Cell("M26").Value.ToString()))).ToString();
	
	            setValueWithColor(txbValue_0, Convert.ToDouble(worksheet.Cell("M18").Value.ToString()));
	            setValueWithColor(txbValue_1, Convert.ToDouble(worksheet.Cell("M19").Value.ToString()));
	            setValueWithColor(txbValue_2, Convert.ToDouble(worksheet.Cell("M20").Value.ToString()));
	            setValueWithColor(txbValue_3, Convert.ToDouble(worksheet.Cell("M21").Value.ToString()));
	            setValueWithColor(txbValue_4, Convert.ToDouble(worksheet.Cell("M22").Value.ToString()));
	            setValueWithColor(txbValue_5, Convert.ToDouble(worksheet.Cell("M23").Value.ToString()));
	            setValueWithColor(txbValue_6, Convert.ToDouble(worksheet.Cell("M24").Value.ToString()));
	            setValueWithColor(txbValue_7, Convert.ToDouble(worksheet.Cell("M25").Value.ToString()));
	            setValueWithColor(txbValue_8, Convert.ToDouble(worksheet.Cell("M26").Value.ToString()));
#endif

	            // (Individual)
	            worksheet = workbook.Worksheet(4);
	
	            setDataGrid(worksheet);
	    	}
        }

        private void setValueWithColor(TextBox txb, double value)
        {
            if (value < 50)
            { txb.Background = noGoodColor; }
            else if (value >= 50 && value < 75)
            { txb.Background = goodColor; }
            else
            { txb.Background = excelentColor; }

            txb.Text = ((int)(value + 0.5)).ToString();
        }

        private void setDataGrid(ClosedXML.Excel.IXLWorksheet worksheet)
        {                        
            dgUnitColor.Columns.Add(new DataGridTextColumn() { Header = "Unit Position", Binding = new Binding("unit_pos"), Width = 60, CanUserResize = false, CanUserSort = false, CanUserReorder = false, IsReadOnly = true });
            
            dgUnitColor.Columns.Add(new DataGridTextColumn() { Header = "⊿%", Binding = new Binding("uf_bright_per"), Width = 60, CanUserResize = false, CanUserSort = false, CanUserReorder = false, IsReadOnly = true });
            dgUnitColor.Columns.Add(new DataGridTextColumn() { Header = "Score", Binding = new Binding("uf_bright_score"), Width = 40, CanUserResize = false, CanUserSort = false, CanUserReorder = false, IsReadOnly = true });
            dgUnitColor.Columns.Add(new DataGridTextColumn() { Header = "⊿u'v'", Binding = new Binding("uf_uv_per"), Width = 60, CanUserResize = false, CanUserSort = false, CanUserReorder = false, IsReadOnly = true });
            dgUnitColor.Columns.Add(new DataGridTextColumn() { Header = "Score", Binding = new Binding("uf_uv_score"), Width = 40, CanUserResize = false, CanUserSort = false, CanUserReorder = false, IsReadOnly = true });
            
            dgUnitColor.Columns.Add(new DataGridTextColumn() { Header = "x", Binding = new Binding("red_x"), Width = 60, CanUserResize = false, CanUserSort = false, CanUserReorder = false, IsReadOnly = true });            
            dgUnitColor.Columns.Add(new DataGridTextColumn() { Header = "y", Binding = new Binding("red_y"), Width = 60, CanUserResize = false, CanUserSort = false, CanUserReorder = false, IsReadOnly = true });
            dgUnitColor.Columns.Add(new DataGridTextColumn() { Header = "Score", Binding = new Binding("red_score"), Width = 40, CanUserResize = false, CanUserSort = false, CanUserReorder = false, IsReadOnly = true });

            dgUnitColor.Columns.Add(new DataGridTextColumn() { Header = "x", Binding = new Binding("green_x"), Width = 60, CanUserResize = false, CanUserSort = false, CanUserReorder = false, IsReadOnly = true });
            dgUnitColor.Columns.Add(new DataGridTextColumn() { Header = "y", Binding = new Binding("green_y"), Width = 60, CanUserResize = false, CanUserSort = false, CanUserReorder = false, IsReadOnly = true });
            dgUnitColor.Columns.Add(new DataGridTextColumn() { Header = "Score", Binding = new Binding("green_score"), Width = 40, CanUserResize = false, CanUserSort = false, CanUserReorder = false, IsReadOnly = true });

            dgUnitColor.Columns.Add(new DataGridTextColumn() { Header = "x", Binding = new Binding("blue_x"), Width = 60, CanUserResize = false, CanUserSort = false, CanUserReorder = false, IsReadOnly = true });
            dgUnitColor.Columns.Add(new DataGridTextColumn() { Header = "y", Binding = new Binding("blue_y"), Width = 60, CanUserResize = false, CanUserSort = false, CanUserReorder = false, IsReadOnly = true });
            dgUnitColor.Columns.Add(new DataGridTextColumn() { Header = "Score", Binding = new Binding("blue_score"), Width = 40, CanUserResize = false, CanUserSort = false, CanUserReorder = false, IsReadOnly = true });

            dgUnitColor.Columns.Add(new DataGridTextColumn() { Header = "x", Binding = new Binding("white_x"), Width = 60, CanUserResize = false, CanUserSort = false, CanUserReorder = false, IsReadOnly = true });
            dgUnitColor.Columns.Add(new DataGridTextColumn() { Header = "y", Binding = new Binding("white_y"), Width = 60, CanUserResize = false, CanUserSort = false, CanUserReorder = false, IsReadOnly = true });
            dgUnitColor.Columns.Add(new DataGridTextColumn() { Header = "Score", Binding = new Binding("white_score"), Width = 40, CanUserResize = false, CanUserSort = false, CanUserReorder = false, IsReadOnly = true });

            dgUnitColor.Columns.Add(new DataGridTextColumn() { Header = "Y", Binding = new Binding("white_bright_Y"), Width = 60, CanUserResize = false, CanUserSort = false, CanUserReorder = false, IsReadOnly = true });
            dgUnitColor.Columns.Add(new DataGridTextColumn() { Header = "Score", Binding = new Binding("white_bright_score"), Width = 40, CanUserResize = false, CanUserSort = false, CanUserReorder = false, IsReadOnly = true });
            
            // 表示用Data Table作成
            m_dt = new DataTable("UF");
            m_dt.Columns.Add(new DataColumn("unit_pos", typeof(string)));// 文字列

            m_dt.Columns.Add(new DataColumn("uf_bright_per", typeof(string)));
            m_dt.Columns.Add(new DataColumn("uf_bright_score", typeof(string)));
            m_dt.Columns.Add(new DataColumn("uf_uv_per", typeof(string)));
            m_dt.Columns.Add(new DataColumn("uf_uv_score", typeof(string)));

            m_dt.Columns.Add(new DataColumn("red_x", typeof(string)));
            m_dt.Columns.Add(new DataColumn("red_y", typeof(string)));
            m_dt.Columns.Add(new DataColumn("red_score", typeof(string)));

            m_dt.Columns.Add(new DataColumn("green_x", typeof(string)));
            m_dt.Columns.Add(new DataColumn("green_y", typeof(string)));
            m_dt.Columns.Add(new DataColumn("green_score", typeof(string)));

            m_dt.Columns.Add(new DataColumn("blue_x", typeof(string)));
            m_dt.Columns.Add(new DataColumn("blue_y", typeof(string)));
            m_dt.Columns.Add(new DataColumn("blue_score", typeof(string)));

            m_dt.Columns.Add(new DataColumn("white_x", typeof(string)));
            m_dt.Columns.Add(new DataColumn("white_y", typeof(string)));
            m_dt.Columns.Add(new DataColumn("white_score", typeof(string)));

            m_dt.Columns.Add(new DataColumn("white_bright_Y", typeof(string)));
            m_dt.Columns.Add(new DataColumn("white_bright_score", typeof(string)));

            DataRow newRowItem;
            for (int row = 13; row < 207; row++)
            {
                if (string.IsNullOrWhiteSpace(worksheet.Cell(row, 3).Value.ToString()) != true)
                {
                    newRowItem = m_dt.NewRow();
                    newRowItem["unit_pos"] = worksheet.Cell(row, 3).Value.ToString();

                    newRowItem["uf_bright_per"] = getPercent(worksheet.Cell(row, 4));
                    newRowItem["uf_bright_score"] = getScore(worksheet.Cell(row, 5));
                    newRowItem["uf_uv_per"] = getPercent(worksheet.Cell(row, 6));
                    newRowItem["uf_uv_score"] = getScore(worksheet.Cell(row, 7));

                    newRowItem["red_x"] = getColor(worksheet.Cell(row, 8));
                    newRowItem["red_y"] = getColor(worksheet.Cell(row, 9));
                    newRowItem["red_score"] = getScore(worksheet.Cell(row, 12));

                    newRowItem["green_x"] = getColor(worksheet.Cell(row, 13));
                    newRowItem["green_y"] = getColor(worksheet.Cell(row, 14));
                    newRowItem["green_score"] = getScore(worksheet.Cell(row, 17));

                    newRowItem["blue_x"] = getColor(worksheet.Cell(row, 18));
                    newRowItem["blue_y"] = getColor(worksheet.Cell(row, 19));
                    newRowItem["blue_score"] = getScore(worksheet.Cell(row, 22));

                    newRowItem["white_x"] = getColor(worksheet.Cell(row, 23));
                    newRowItem["white_y"] = getColor(worksheet.Cell(row, 24));
                    newRowItem["white_score"] = getScore(worksheet.Cell(row, 27));

                    newRowItem["white_bright_Y"] = getScore(worksheet.Cell(row, 28));
                    newRowItem["white_bright_score"] = getScore(worksheet.Cell(row, 29));

                    m_dt.Rows.Add(newRowItem);
                }
                else
                { break; }
            }

            dgUnitColor.DataContext = m_dt;
        }

        private string getPercent(ClosedXML.Excel.IXLCell cell)
        {
            string temp, ret = "";
            double valueDouble;

            try
            {
                temp = cell.Value.ToString();
                valueDouble = Double.Parse(temp);
                valueDouble *= 100;
                valueDouble = Math.Round(valueDouble, 1);

                ret = valueDouble.ToString() + " %";
            }
            catch
            { return "0%"; }
            
            return ret;
        }

        private string getScore(ClosedXML.Excel.IXLCell cell)
        {
            string temp, ret = "";
            double valueDouble;

            try
            {
                temp = cell.Value.ToString();
                valueDouble = Double.Parse(temp);
                valueDouble = Math.Round(valueDouble);
                ret = valueDouble.ToString();
            }
            catch
            { return "0"; }

            return ret;
        }

        private string getColor(ClosedXML.Excel.IXLCell cell)
        {
            string temp, ret = "";
            double valueDouble;

            try
            {
                temp = cell.Value.ToString();
                valueDouble = Double.Parse(temp);
                valueDouble = Math.Round(valueDouble, 3);
                ret = valueDouble.ToString("F3");
            }
            catch
            { return "0"; }

            return ret;

        }
    }
}
