// added by Hotta 2024/08/28
#define ForCrosstalkCameraUF
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Windows.Forms;
using SONY.Modules;
using CAS;
using CameraDataClass;

namespace MakeUFData
{
    class CMakeUFData
    {
        public enum MeasureMode { FullCell = 12, Cross4Point = 4, Cell = 5, Unit = 8 };
        private enum E_mode { E_FMT, E_CONCEAL, E_DEFECTS, E_DEFECTS2 };

        #region Fields

        // PAT2
        // Offset, Step, Max, BitMask, Shift
        private static readonly MATPARAM[] matParamPat2 = new MATPARAM[9]{
            new MATPARAM(   0.09375, 0.00390625 , 1.08984375 , 0xFF, 56),
            new MATPARAM(  -0.15625, 0.004882813, 0.463867188, 0x7F, 49),
            new MATPARAM(  -0.1875 , 0.002929688, 0.184570313, 0x7F, 42),
            new MATPARAM(  -0.15625, 0.004882813, 0.151367188, 0x3F, 36),
            new MATPARAM(   0.09375, 0.00390625 , 1.08984375 , 0xFF, 28),
            new MATPARAM(  -0.1875 , 0.002929688, 0.184570313, 0x7F, 21),
            new MATPARAM(  -0.15625, 0.004882813, 0.151367188, 0x3F, 15),
            new MATPARAM(  -0.1875 , 0.002929688, 0.184570313, 0x7F,  8),
            new MATPARAM(   0.09375, 0.00390625 , 1.08984375 , 0xFF,  0)
        };

        private static readonly MATPARAM[] matParamPat3 = new MATPARAM[9]{
            new MATPARAM(   0.0    , 0.00585938 , 1.49414063 , 0xFF, 56),
            new MATPARAM(  -0.03125, 0.00195313 , 0.21679688 , 0x7F, 49),
            new MATPARAM(  -0.03125, 0.00097656 , 0.09277344 , 0x7F, 42),
            new MATPARAM(  -0.03125, 0.00097656 , 0.09277344 , 0x7F, 35),
            new MATPARAM(   0.0    , 0.00585938 , 1.49414063 , 0xFF, 27),
            new MATPARAM(  -0.03125, 0.00097656 , 0.09277344 , 0x7F, 20),
            new MATPARAM(  -0.03125, 0.00195313 , 0.09179688 , 0x3F, 14),
            new MATPARAM(  -0.03125, 0.00195313 , 0.09179688 , 0x3F,  8),
            new MATPARAM(   0.0    , 0.00585938 , 1.49414063 , 0xFF,  0)
        };

        const int m_DivideX = 4;
        const int m_DivideX_4P = 3;
        const int m_DivideY_Chiron = 2;
        const int m_DivideY_Cancun = 3;
        const int m_DivideY_4P = 3;

        private int moduleCount; // 1CabinetあたりのModule数 

        int m_DivideY;

        int m_UnitDx;//P1.2 = 480 / P1.5 = 384;
        int m_UnitDy;//P1.2 = 270 / P1.5 = 216;

        int m_CellDx;// = m_UnitDx / m_DivideX;
        int m_CellDy;// = m_UnitDy / m_DivideY;

        float[] m_pFmtOriginal; // 現在の補正値？
        float[] m_pFmtCreated; // 新しい補正値？
        float[] m_pFmtCell;
        float[] m_pXYZ; // XYZ
        float[] m_pXYZCell;

        double[] m_CellTemperature;

        double[] m_Target;  // xr, yr, gx, gy, bx, by, Yw, xw, yw

        string m_SourceCellDataFileName;

        string m_LastErrorMessage;

        bool m_IsCtcOverwrite;
        Dictionary<int, CrossTalkCorrectionHighColor> m_CorUncCrosstalk;
        CrossTalkCoef_UF m_CtcCoef;
        MtgtAddCrosstalk m_MtgtAddCrosstalk;

        // added by Hotta 2024/09/12
#if ForCrosstalkCameraUF
        public Dictionary<int, CrossTalkCorrectionHighColor> CorUncCrosstalk
        {
            get { return m_CorUncCrosstalk; }
        }
#endif

        #endregion

        #region Public Methods

        public CMakeUFData(int dx, int dy)
        {
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            {
                m_DivideY = m_DivideY_Chiron;
            }
            else
            {
                m_DivideY = m_DivideY_Cancun;
                m_IsCtcOverwrite = false;
                m_CorUncCrosstalk = new Dictionary<int, CrossTalkCorrectionHighColor>();
                m_CtcCoef = new CrossTalkCoef_UF();
            }

            moduleCount = m_DivideX * m_DivideY;

            m_UnitDx = dx;
            m_UnitDy = dy;
            m_CellDx = m_UnitDx / m_DivideX;
            m_CellDy = m_UnitDy / m_DivideY;

            m_pFmtOriginal = new float[m_UnitDx * m_UnitDy * 9];
            m_pFmtCell = new float[m_CellDx * m_CellDy * 9];
            m_pXYZ = new float[m_UnitDx * m_UnitDy * 3 * 3];
            m_pXYZCell = new float[m_CellDx * m_CellDy * 3 * 3];

            m_LastErrorMessage = "";

            m_CellTemperature = new double[m_DivideX * m_DivideY];

            m_Target = new double[] { 0.691, 0.308, 0.185, 0.722, 0.14, 0.058, 167.006, 0.292, 0.297 };
        }

        public string GetLastErrorMessage()
        {
            return m_LastErrorMessage;
        }

        /// <summary>
        /// 高諧調Crosstalk補正量R/G/Bを取得
        /// </summary>
        /// <param name="file">高諧調Crosstalk補正量R/G/Bを取得する対象ファイル</param>
        /// <param name="measureData">測定データが格納された配列</param>
        /// <returns></returns>
        public bool GetUncCrosstalk(string file, double[][][] measureData)
        {
            m_IsCtcOverwrite = false;
            m_CorUncCrosstalk = new Dictionary<int, CrossTalkCorrectionHighColor>();

            try
            {
                byte[] tempBytes = new byte[4];

                List<CompositeConfigData> lstConfigData = CompositeConfigData.getCompositeConfigDataList(file, MainWindow.dt_ColorCorrectionWrite);
                for (int i = 0; i < lstConfigData.Count; i++)
                {

#if DEBUG
                    Console.WriteLine($"Module No.:{(int)lstConfigData[i].header.Option1}");
#elif Release_log
                    MainWindow.SaveExecLog($"     Module No.:{(int)lstConfigData[i].header.Option1}");
#endif

                    Array.Copy(lstConfigData[i].data, MainWindow.HcCcDataCtcDataValidIndicatorOffset, tempBytes, 0, tempBytes.Length);
                    int vdi = BitConverter.ToInt32(tempBytes, 0);
                    int upper4Bits = vdi >> 4;
                    if (upper4Bits == 2 || upper4Bits == 1)
                    {
                        CrossTalkCorrectionHighColor _ctc = new CrossTalkCorrectionHighColor();

                        Array.Copy(lstConfigData[i].data, MainWindow.HcCcDataCtcHighRedOffset, tempBytes, 0, tempBytes.Length);
                        _ctc.Red = BitConverter.ToSingle(tempBytes, 0);

                        Array.Copy(lstConfigData[i].data, MainWindow.HcCcDataCtcHighGreenOffset, tempBytes, 0, tempBytes.Length);
                        _ctc.Green = BitConverter.ToSingle(tempBytes, 0);

                        Array.Copy(lstConfigData[i].data, MainWindow.HcCcDataCtcHighBlueOffset, tempBytes, 0, tempBytes.Length);
                        _ctc.Blue = BitConverter.ToSingle(tempBytes, 0);

                        m_CorUncCrosstalk.Add((int)lstConfigData[i].header.Option1, _ctc);

#if DEBUG
                        Console.WriteLine($"Crosstalk RY: {_ctc.Red}");
                        Console.WriteLine($"Crosstalk GY: {_ctc.Green}");
                        Console.WriteLine($"Crosstalk BY: {_ctc.Blue}");
#elif Release_log
                        MainWindow.SaveExecLog($"      Crosstalk RY: {_ctc.Red}");
                        MainWindow.SaveExecLog($"      Crosstalk GY: {_ctc.Green}");
                        MainWindow.SaveExecLog($"      Crosstalk BY: {_ctc.Blue}");
#endif
                    }
                    else if (measureData == null) //カメラCAS用に、VDI=0を通知する処理
                    {
                        CrossTalkCorrectionHighColor _ctc = null;
                        m_CorUncCrosstalk.Add((int)lstConfigData[i].header.Option1, _ctc);
                    }
                    else
                    {
                        CrossTalkCorrectionHighColor _ctc = new CrossTalkCorrectionHighColor();
                        List<CrossTalkCorrectionHighColor> _lstCtc = new List<CrossTalkCorrectionHighColor>();

                        for (int x = 0; x < 4; x++)
                        {
                            CalcUncCrosstalkRGB(measureData[x], out CrossTalkCorrectionHighColor ctc);
                            _lstCtc.Add(ctc);
                        }

                        for (int x = 0; x < _lstCtc.Count; x++)
                        {
                            _ctc.Red += _lstCtc[x].Red;
                            _ctc.Green += _lstCtc[x].Green;
                            _ctc.Blue += _lstCtc[x].Blue;
                        }

                        _ctc.Red /= _lstCtc.Count;
                        _ctc.Green /= _lstCtc.Count;
                        _ctc.Blue /= _lstCtc.Count;

                        m_CorUncCrosstalk.Add((int)lstConfigData[i].header.Option1, _ctc);

#if DEBUG
                        Console.WriteLine($"Crosstalk RY(Avg): {_ctc.Red}");
                        Console.WriteLine($"Crosstalk GY(Avg): {_ctc.Green}");
                        Console.WriteLine($"Crosstalk BY(Avg): {_ctc.Blue}");
#elif Release_log
                        MainWindow.SaveExecLog($"      Crosstalk RY(Avg): {_ctc.Red}");
                        MainWindow.SaveExecLog($"      Crosstalk GY(Avg): {_ctc.Green}");
                        MainWindow.SaveExecLog($"      Crosstalk BY(Avg): {_ctc.Blue}");
#endif

                    }
                }
            }
            catch
            { return false; }

            return true;
        }

        /// <summary>
        /// 算出したクロストーク補正量を保持
        /// </summary>
        /// <param name="lstTargetCell">対象Moduleの番号が格納されたリスト</param>
        /// <param name="measureData">測定データが格納された配列</param>
        /// <returns></returns>
        public bool CalcUncCrosstalk(List<int> lstTargetCell, double[][][] measureData)
        {
            m_IsCtcOverwrite = true;
            m_CorUncCrosstalk = new Dictionary<int, CrossTalkCorrectionHighColor>();
            try
            {
                for (int idx = 0; idx < lstTargetCell.Count; idx++)
                {

#if DEBUG
                    Console.WriteLine($"Module No.{lstTargetCell[idx] + 1}");
#elif Release_log
                    MainWindow.SaveExecLog($"     Module No.{lstTargetCell[idx] + 1}");
#endif

                    if (!CalcUncCrosstalkRGB(measureData[idx], out CrossTalkCorrectionHighColor ctc))
                    { return false; }

                    m_CorUncCrosstalk.Add(lstTargetCell[idx] + 1, ctc);
                }
            }
            catch
            { return false; }

            return true;
        }

        /// <summary>
        /// 算出したクロストーク補正量を測定データに加味
        /// </summary>
        /// <param name="measureData">測定データが格納された配列</param>
        /// <param name="idx">測定データを取り出すインデックス</param>
        /// <returns></returns>
        public bool CalcUncCrosstalkWithAddUncCrosstalk(double[][][] measureData, int idx)
        {
            double[] dData = new double[9];
            try
            {
#if DEBUG
                Console.WriteLine($"Measure Data Index:{idx}");
#elif Release_log
                MainWindow.SaveExecLog($"     MeasureData Index:{idx}");
#endif

                // Rx = RX / (RX + RY + RZ)
                double Rx = measureData[idx][0][0] / (measureData[idx][0][0] + measureData[idx][0][1] + measureData[idx][0][2]);
                // Ry = RY / (RX + RY + RZ)
                double Ry = measureData[idx][0][1] / (measureData[idx][0][0] + measureData[idx][0][1] + measureData[idx][0][2]);

                // Gx = GX / (GX + GY + GZ)
                double Gx = measureData[idx][1][0] / (measureData[idx][1][0] + measureData[idx][1][1] + measureData[idx][1][2]);
                // Gy = GY / (GX + GY + GZ)
                double Gy = measureData[idx][1][1] / (measureData[idx][1][0] + measureData[idx][1][1] + measureData[idx][1][2]);

                // Bx = BX / (BX + BY + BZ)
                double Bx = measureData[idx][2][0] / (measureData[idx][2][0] + measureData[idx][2][1] + measureData[idx][2][2]);
                // By = BY / (BX + BY + BZ)
                double By = measureData[idx][2][1] / (measureData[idx][2][0] + measureData[idx][2][1] + measureData[idx][2][2]);

                // Wx = WX / (WX + WY + WZ)
                double Wx = measureData[idx][3][0] / (measureData[idx][3][0] + measureData[idx][3][1] + measureData[idx][3][2]);
                // Wy = WY / (WX + WY + WZ)
                double Wy = measureData[idx][3][1] / (measureData[idx][3][0] + measureData[idx][3][1] + measureData[idx][3][2]);
                // WY = WY
                double WY = measureData[idx][3][1];

                double[] target = new double[] { Rx, Ry, Gx, Gy, Bx, By, WY, Wx, Wy };
                ColMtx Msig = new ColMtx(target, 1);
                Msig.GetElement(dData);

                measureData[idx][0][0] = dData[0];
                measureData[idx][0][1] = dData[1];
                measureData[idx][0][2] = dData[2];
                measureData[idx][1][0] = dData[3];
                measureData[idx][1][1] = dData[4];
                measureData[idx][1][2] = dData[5];
                measureData[idx][2][0] = dData[6];
                measureData[idx][2][1] = dData[7];
                measureData[idx][2][2] = dData[8];

#if DEBUG
                Console.WriteLine($"Msig_adjacent: {dData[0]}, {dData[3]}, {dData[6]}, {dData[1]}, {dData[4]}, {dData[7]}, {dData[2]}, {dData[5]}, {dData[8]}");
#elif Release_log
                MainWindow.SaveExecLog($"      Msig_adjacent: {dData[0]}, {dData[3]}, {dData[6]}, {dData[1]}, {dData[4]}, {dData[7]}, {dData[2]}, {dData[5]}, {dData[8]}");
#endif

            }
            catch
            { return false; }

            return true;
        }

        /// <summary>
        /// 測定データからR/G/B輝度単色クロストーク補正量算出
        /// </summary>
        /// <param name="measureData">対象測定データの[R/G/B/W][X/Y/Z]が格納されている</param>
        /// <param name="ctc">R/G/B輝度単色クロストーク補正量を格納する</param>
        /// <returns></returns>
        public bool CalcUncCrosstalkRGB(double[][] measureData, out CrossTalkCorrectionHighColor ctc)
        {
            ctc = new CrossTalkCorrectionHighColor();

            try
            {
                // RGBX = RX + GX + BX
                double RGBX = measureData[0][0] + measureData[1][0] + measureData[2][0];
                // RGBY = RY + GY + BY
                double RGBY = measureData[0][1] + measureData[1][1] + measureData[2][1];
                // RGBZ = RZ + GZ + BZ
                double RGBZ = measureData[0][2] + measureData[1][2] + measureData[2][2];
                // RGBx = RGBX / (RGBX + RGBY + RGBZ)
                double RGBx = RGBX / (RGBX + RGBY + RGBZ);
                // RGBy = RGBY / (RGBX + RGBY + RGBZ)
                double RGBy = RGBY / (RGBX + RGBY + RGBZ);

                // クロストーク算出
                // WY
                double WY = measureData[3][1];
                // Crosstalk Y(Yct) = WY / RGBY
                double Yct = WY / RGBY;
                // Wx
                double Wx = measureData[3][0] / (measureData[3][0] + measureData[3][1] + measureData[3][2]);
                // Crosstalk x(xct) = Wx / RGBx
                double xct = Wx - RGBx;
                // Wy
                double Wy = measureData[3][1] / (measureData[3][0] + measureData[3][1] + measureData[3][2]);
                // Crosstalk y(yct) = Wy / RGBy
                double yct = Wy - RGBy;

                // クロストーク補正量算出(CrossTalkCorrecction)
                // CTC_Y = 1 - Yct
                double CTC_Y = 1 - Yct;

#if DEBUG
                Console.WriteLine($"CTC_Y: {CTC_Y}");
#endif

                // CTC_x = -xct
                double CTC_x = -xct;

#if DEBUG
                Console.WriteLine($"CTC_x: {CTC_x}");
#endif

                // CTC_y = -yct
                double CTC_y = -yct;

#if DEBUG
                Console.WriteLine($"CTC_y: {CTC_y}");
#endif

                // R/G/B輝度単色クロストーク補正量
                // Crosstalk RY = CTC_Y + WxRY * CTC_x + WyRY * CTC_y
                double RY = CTC_Y + (m_CtcCoef.WxRY * CTC_x) + (m_CtcCoef.WyRY * CTC_y);

#if DEBUG
                Console.WriteLine($"Crosstalk RY: {RY}");
#endif

                // Crosstalk GY = CTC_Y + WxGY * CTC_x + WyGY * CTC_y
                double GY = CTC_Y + (m_CtcCoef.WxGY * CTC_x) + (m_CtcCoef.WyGY * CTC_y);

#if DEBUG
                Console.WriteLine($"Crosstalk GY: {GY}");
#endif

                // Crosstalk BY = CTC_Y + WxBY * CTC_x + WyBY * CTC_y
                double BY = CTC_Y + (m_CtcCoef.WxBY * CTC_x) + (m_CtcCoef.WyBY * CTC_y);

#if DEBUG
                Console.WriteLine($"Crosstalk BY: {BY}");
#endif
                ctc.Red = Math.Round(RY, 6);
                ctc.Green = Math.Round(GY, 6);
                ctc.Blue = Math.Round(BY, 6);

#if Release_log
                MainWindow.SaveExecLog($"      Crosstalk RY: {ctc.Red}");
                MainWindow.SaveExecLog($"      Crosstalk GY: {ctc.Green}");
                MainWindow.SaveExecLog($"      Crosstalk BY: {ctc.Blue}");
#endif
            }
            catch
            { return false; }

            return true;
        }

        /// <summary>
        /// 調整の目標値を設定する。
        /// </summary>
        /// <param name="xr"></param>
        /// <param name="yr"></param>
        /// <param name="xg"></param>
        /// <param name="yg"></param>
        /// <param name="xb"></param>
        /// <param name="yb"></param>
        /// <param name="Yw"></param>
        /// <param name="xw"></param>
        /// <param name="yw"></param>
        public void SetTargetValue(double xr, double yr, double xg, double yg, double xb, double yb, double Yw, double xw, double yw)
        {
            m_Target[0] = xr;
            m_Target[1] = yr;
            m_Target[2] = xg;
            m_Target[3] = yg;
            m_Target[4] = xb;
            m_Target[5] = yb;
            m_Target[6] = Yw;
            m_Target[7] = xw;
            m_Target[8] = yw;
        }

        /// <summary>
        /// 既存のCellDataから、ムラ補正データを取得する。とりあえず1.2Pのみ対応
        /// </summary>
        /// <param name="fileName">既存のCellDataのファイル名</param>
        /// <returns></returns>
        //public bool ExtractFmt(string fileName)
        //{
        //    const int size = 129792;    //1038336bit / 8
        //    byte[] buf = new byte[size];
        //    int[] fmb = new int[18];
        //    int i, j;
        //    int cells = 0;

        //    m_SourceCellDataFileName = fileName;

        //    FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        //    fs.Seek(0, SeekOrigin.Begin);
        //    for (int y0 = 0; y0 < m_UnitDy; y0 += m_CellDy)
        //    {
        //        for (int x0 = 0; x0 < m_UnitDx; x0 += m_CellDx)
        //        {
        //            j = 0;
        //            //fs.Seek(0x20000 * cells + (32 * 6 + 1856 + 8192 + 32 * 3 + 1440) / 8, SeekOrigin.Begin);
        //            fs.Seek(0x20000 * cells + (32 * 6 + 1856 + 8192 + 32) / 8, SeekOrigin.Begin);
        //            byte[] temp = new byte[4];
        //            fs.Read(temp, 0, 4);
        //            m_CellTemperature[cells] = (double)((long)(temp[3] << 24) + (long)(temp[2] << 16) + (long)(temp[1] << 8) + (long)temp[0]) * 0.0625;
        //            fs.Seek((1440 + 32) / 8, SeekOrigin.Current);

        //            fs.Read(buf, 0, size);
        //            for (int y = y0; y < y0 + m_CellDy; y++)
        //            {
        //                for (int x = x0; x < x0 + m_CellDx; x += 2)
        //                {
        //                    // unpacking
        //                    for(i = 0; i < 9 * 2; i += 2)
        //                    {
        //                        fmb[i + 0] = ((int)(buf[j + 1] & 0x0f) << 8) + buf[j];
        //                        fmb[i + 1] = ((int)buf[j + 2] << 4) + (buf[j + 1] >> 4);
        //                        j += 3;
        //                    }

        //                    // binary -> float
        //                    for (i = 0; i < 9 * 2; ++i)
        //                    {
        //                        // 2画素分で1周期
        //                        int m = (i > 8) ? i - 9 : i; // 対角を調べる目印
        //                        if (m % 4 != 0) fmb[i] -= 2048; // 比対角だけ-2048
        //                        m_pFmtOriginal[y * m_UnitDx * 9 + x * 9 + i] = fmb[i] / 2048.0f;
        //                    }
        //                }
        //            }
        //            cells++;
        //        }
        //    }
        //    fs.Close();

        //    return true;
        //}        

        public bool ExtractFmt(string fileName, string ledModel)
        {
            byte[] srcBytes = File.ReadAllBytes(fileName);
            int size = m_CellDx * m_CellDy * 8; // 1Pixel = 8Byte, P1.2 : 129600[Byte] / P1.5 : 82944[Byte]
            int cells = 0;
            int hcModuleDataLength;

            m_SourceCellDataFileName = fileName;

            switch (ledModel)
            {
                case MainWindow.ZRD_C15A:
                case MainWindow.ZRD_B15A:
                    hcModuleDataLength = MainWindow.HcModuleDataLengthP15_Module4x2;
                    break;
                case MainWindow.ZRD_CH12D:
                case MainWindow.ZRD_BH12D:
                case MainWindow.ZRD_CH12D_S3:
                case MainWindow.ZRD_BH12D_S3:
                    hcModuleDataLength = MainWindow.HcModuleDataLengthP12_Module4x3;
                    break;
                case MainWindow.ZRD_CH15D:
                case MainWindow.ZRD_BH15D:
                case MainWindow.ZRD_CH15D_S3:
                case MainWindow.ZRD_BH15D_S3:
                    hcModuleDataLength = MainWindow.HcModuleDataLengthP15_Module4x3;
                    break;
                default:
                    hcModuleDataLength = MainWindow.HcModuleDataLengthP12_Module4x2;
                    break;
            }

            // ModuleのLoop
            for (int y0 = 0; y0 < m_UnitDy; y0 += m_CellDy)
            {
                for (int x0 = 0; x0 < m_UnitDx; x0 += m_CellDx)
                {
                    byte[] moduleBytes = new byte[size];

                    // 補正時の温度 (単位/精度はTBD)
                    byte[] tempBytes = new byte[4];
                    Array.Copy(srcBytes, 0x164 + cells * hcModuleDataLength, tempBytes, 0, 4); // 356 = 352(最初のColor Correction Data) + 4(Color Correction Data内のAdj Tempの位置)

                    m_CellTemperature[cells] = BitConverter.ToInt32(tempBytes, 0);

                    // 補正値
                    // 1Module分を一時格納
                    Array.Copy(srcBytes, 0x260 + cells * hcModuleDataLength, moduleBytes, 0, size);

                    // Module内の画素のLoop
                    int pixel = 0;

#if ForCrosstalkCameraUF
                    float[] avg_Fmt = new float[9];
#endif

                    for (int y = y0; y < y0 + m_CellDy; y++)
                    {
                        for (int x = x0; x < x0 + m_CellDx; x++)
                        {
                            double[] elements;

                            byte[] pxlBytes = new byte[8]; // 8 : 1Pixelあたりのデータ長
                            Array.Copy(moduleBytes, pixel * 8, pxlBytes, 0, 8);

                            //CMakeUFData.UnpackCcDataPat2(pxlBytes, out elements);
                            CMakeUFData.UnpackCcDataPat3(pxlBytes, out elements);

                            // 格納
                            for (int i = 0; i < 9; i++)
                            { m_pFmtOriginal[y * m_UnitDx * 9 + x * 9 + i] = (float)elements[i]; }

#if DEBUG
                            if (CalcModuleNo(x, y, out int moduleNo))
                            {
                                // Excelで3x3を受け取るIndexルールに合わせため、出力並び順を変更
                                // elements[0],elements[1]...elements[8]
                                Console.WriteLine($"Module No.{moduleNo} m_pFmtOriginal: {string.Join(", ", elements)}");
                            }
#elif Release_log
                            if (CalcModuleNo(x, y, out int moduleNo))
                            { MainWindow.SaveExecLog($"      Module No.{moduleNo} m_pFmtOriginal: {string.Join(", ", elements)}"); }
#endif

                            pixel++;
                        }
                    }
                    cells++;
#if ForCrosstalkCameraUF
                    //for (int i = 0; i < 9; i++)
                    //    avg_Fmt[i] /= m_CellDx * m_CellDy;
                    //using (StreamWriter sw = new StreamWriter("c:\\temp\\read_fmt.csv", true))
                    //{
                    //    sw.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}", fileName, avg_Fmt[0], avg_Fmt[3], avg_Fmt[6], avg_Fmt[1], avg_Fmt[4], avg_Fmt[7], avg_Fmt[2], avg_Fmt[5], avg_Fmt[8]);
                    //}
#endif
                }
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool ExtractCellData(string fileName)
        {
            const int size = 129792;    //1038336bit / 8
            byte[] buf = new byte[size];
            int[] fmb = new int[18];
            int i, j;
            //int cells = 0;

            //m_SourceCellDataFileName = fileName;

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
	            fs.Seek(0, SeekOrigin.Begin);
	
	            j = 0;
	            fs.Seek((32 * 6 + 1856 + 8192 + 32) / 8, SeekOrigin.Begin);
	            byte[] temp = new byte[4];
	            fs.Read(temp, 0, 4);
	            //m_CellTemperature[cells] = (double)((long)(temp[3] << 24) + (long)(temp[2] << 16) + (long)(temp[1] << 8) + (long)temp[0]) * 0.0625;
	            fs.Seek((1440 + 32) / 8, SeekOrigin.Current);
	
	            fs.Read(buf, 0, size);
	            for (int y = 0; y < m_CellDy; y++)
	            {
	                for (int x = 0; x < m_CellDx; x += 2)
	                {
	                    // unpacking
	                    for (i = 0; i < 9 * 2; i += 2)
	                    {
	                        fmb[i + 0] = ((int)(buf[j + 1] & 0x0f) << 8) + buf[j];
	                        fmb[i + 1] = ((int)buf[j + 2] << 4) + (buf[j + 1] >> 4);
	                        j += 3;
	                    }
	
	                    // binary -> float
	                    for (i = 0; i < 9 * 2; ++i)
	                    {
	                        // 2画素分で1周期
	                        int m = (i > 8) ? i - 9 : i; // 対角を調べる目印
	                        if (m % 4 != 0) fmb[i] -= 2048; // 比対角だけ-2048
	                        m_pFmtCell[y * m_CellDx * 9 + x * 9 + i] = fmb[i] / 2048.0f;
	                    }
	                }
	            }
            }

            return true;
        }

        /// <summary>
        /// ムラ補正データとその時の補正ターゲットから、XYZイメージを算出する。
        /// </summary>
        /// <returns></returns>
        public bool Fmt2XYZ(double xr, double yr, double xg, double yg, double xb, double yb, double Yw, double xw, double yw)
        {
            // parameterの目標を設定
            double[] target = new double[] { xr, yr, xg, yg, xb, yb, Yw, xw, yw };
            ColMtx Msig = new ColMtx(target, 1);

#if DEBUG
            double[] dData = new double[9];

            Msig.GetElement(dData);
            Console.WriteLine($"Msig: {dData[0]}, {dData[3]}, {dData[6]}, {dData[1]}, {dData[4]}, {dData[7]}, {dData[2]}, {dData[5]}, {dData[8]}");
#elif Release_log
            double[] dData = new double[9];

            Msig.GetElement(dData);
            MainWindow.SaveExecLog($"      Msig: {dData[0]}, {dData[3]}, {dData[6]}, {dData[1]}, {dData[4]}, {dData[7]}, {dData[2]}, {dData[5]}, {dData[8]}");
#endif

            // 計算実行
            int i = 0;
            for (int y = 0; y < m_UnitDy; ++y)
            {
                for (int x = 0; x < m_UnitDx; ++x)
                {
                    ColMtx Mfmt = new ColMtx(m_pFmtOriginal, i); // fmtは順番がまず横なので転置必要
                    ColMtx Mxyz;
                    Mxyz = Msig * (Mfmt.Transpose().Inv());　// UnpackCcDataPat3関数処理で取得したMfmtの転置変換の並び変えが必要
                    Mxyz.GetElement(m_pXYZ, i); // m_pXYZに設定
                    i += 9;

#if DEBUG
                    if (CalcModuleNo(x, y, out int moduleNo))
                    {
                        Mxyz.GetElement(dData);
                        Console.WriteLine($"Module No.{moduleNo} Mfmt: {dData[0]}, {dData[3]}, {dData[6]}, {dData[1]}, {dData[4]}, {dData[7]}, {dData[2]}, {dData[5]}, {dData[8]}");
                    }
#elif Release_log

                    if (CalcModuleNo(x, y, out int moduleNo))
                    {
                        Mxyz.GetElement(dData);
                        MainWindow.SaveExecLog($"      Module No.{moduleNo} Mfmt: {dData[0]}, {dData[3]}, {dData[6]}, {dData[1]}, {dData[4]}, {dData[7]}, {dData[2]}, {dData[5]}, {dData[8]}");
                    }
#endif
                }
            }

            return true;
        }

        /// <summary>
        /// ムラ補正データとその時の補正ターゲットから、XYZイメージを算出する。Cell Data用。
        /// </summary>
        /// <param name="xr"></param>
        /// <param name="yr"></param>
        /// <param name="xg"></param>
        /// <param name="yg"></param>
        /// <param name="xb"></param>
        /// <param name="yb"></param>
        /// <param name="Yw"></param>
        /// <param name="xw"></param>
        /// <param name="yw"></param>
        /// <returns></returns>
        public bool Fmt2XYZ_Cell(double xr, double yr, double xg, double yg, double xb, double yb, double Yw, double xw, double yw)
        {
            // parameterの目標を設定
            double[] target = new double[] { xr, yr, xg, yg, xb, yb, Yw, xw, yw };
            ColMtx Msig = new ColMtx(target, 1);

            // 計算実行
            int i = 0;
            for (int y = 0; y < m_CellDy; ++y)
            {
                for (int x = 0; x < m_CellDx; ++x)
                {
                    ColMtx Mfmt = new ColMtx(m_pFmtCell, i); // fmtは順番がまず横なので転置必要
                    ColMtx Mxyz;
                    Mxyz = Msig * (Mfmt.Transpose().Inv());
                    Mxyz.GetElement(m_pXYZCell, i); // m_pXYZに設定
                    i += 9;
                }
            }
            return true;
        }
        

        /// <summary>
        /// ムラ補正データ　と、補正ターゲット×クロストーク量　から、XYZイメージを算出する。カメラU/Fで使う。
        /// </summary>
        /// <returns></returns>
// modified by Hotta 2024/09/11
#if ForCrosstalkCameraUF
        public bool Fmt2XYZ_Crosstalk(double xr, double yr, double xg, double yg, double xb, double yb, double Yw, double xw, double yw, float[][] crosstalk)
        {
            // parameterの目標を設定
            double[] target = new double[] { xr, yr, xg, yg, xb, yb, Yw, xw, yw };

            // 計算実行
            int i = 0;
            for (int y = 0; y < m_UnitDy; ++y)
            {
                for (int x = 0; x < m_UnitDx; ++x)
                {
                    // クロストーク補正量を含んだ、製造調整時のMsigを求める
                    ColMtx Msig = new ColMtx(target, 1);
                    int moduleNo = (y / m_CellDy) * 4 + (x / m_CellDx);
                    ColMtx gain = new ColMtx(
                        (1.0f + crosstalk[moduleNo][0]), 0, 0,
                        0, (1.0f + crosstalk[moduleNo][1]), 0,
                        0, 0, (1.0f + crosstalk[moduleNo][2])
                    );
                    Msig = Msig * gain;

                    ColMtx Mfmt = new ColMtx(m_pFmtOriginal, i); // fmtは順番がまず横なので転置必要
                    ColMtx Mxyz;
                    Mxyz = Msig * (Mfmt.Transpose().Inv());
                    Mxyz.GetElement(m_pXYZ, i); // m_pXYZに設定
                    i += 9;
                }
            }

            return true;
        }
#endif

        /// <summary>
        /// ベースになるCellDataと交換用Cellのデータをマージします。
        /// </summary>
        /// <param name="CellNo">Cell番号(0-11)</param>
        /// <returns></returns>
        public bool MergeCellData(int CellNo)
        {
            //int offset = m_CellDx * m_CellDy * 9 * CellNo;
            int x_start, x_end, y_start, y_end;
            int pos = 0;

            x_start = CellNo % 4 * m_CellDx;
            x_end = x_start + m_CellDx;
            y_start = CellNo / 4 * m_CellDy;
            y_end = y_start + m_CellDy;

            for (int i = 0; i < m_pXYZ.Length; i += 9)
            {
                int x = i / 9 % m_UnitDx;
                int y = i / 9 / m_UnitDx;

                if (x >= x_start && x < x_end && y >= y_start && y < y_end)
                {
                    m_pXYZ[i] = m_pXYZCell[pos];
                    m_pXYZ[i + 1] = m_pXYZCell[pos + 1];
                    m_pXYZ[i + 2] = m_pXYZCell[pos + 2];
                    m_pXYZ[i + 3] = m_pXYZCell[pos + 3];
                    m_pXYZ[i + 4] = m_pXYZCell[pos + 4];
                    m_pXYZ[i + 5] = m_pXYZCell[pos + 5];
                    m_pXYZ[i + 6] = m_pXYZCell[pos + 6];
                    m_pXYZ[i + 7] = m_pXYZCell[pos + 7];
                    m_pXYZ[i + 8] = m_pXYZCell[pos + 8];

                    pos += 9;
                }
            }
            
            return true;
        }

        /// <summary>
        /// 調整時のCellの温度を取得する。（セルデータに作成時に必要）
        /// </summary>
        /// <returns></returns>
        public bool GetCellTemperature()
        {
            for(int n = 0; n < m_DivideY * m_DivideY; n++)
            {
                m_CellTemperature[n] = 20.0 + 0.1 * n;
            }
            return true;
        }

        /// <summary>先に算出したXYZイメージに、今回の計測値から取得した誤差分を更新する。</summary>
        /// <remarks>パネル単位で一律補正</remarks>
        /// <param name="measureData">今回のXYZ計測値（セル単位）[point][r/g/b][X/Y/Z]
        /// point:X1Y1/X2Y1/...XdevideY1/X1Y2/.../XdevideYdevide
        /// </param>
        /// <param name="mode">全Cell(12点)測定・十字4点</param>
        /// <returns></returns>
        public bool Compensate_XYZ(double[][][] measureData, MeasureMode mode = MeasureMode.FullCell)
        {
            Double[] xyz = new Double[9];
            for (Int32 i = 0; i < 9; ++i)
            { xyz[i] = m_Target[i]; } // xyzは使い回し

            // Msig : 目標のWを満足するためのXr/Yr/Zr/Xg/Yg/Zg/Xb/Yb/Zbの行列式
            // これらの色が発光するための入力信号、という意味合い
            ColMtx Msig = new ColMtx(xyz, 1);
            ColMtx[] C = null;
            int index = 0;

            // Msig：Targetをもとに算出する行列
            // Mmes：測定値をもとに算出する行列
            // Mtgt：外周の測定値をもとに算出する行列

            // Strict Mode
            if (mode == MeasureMode.FullCell)
            {
                C = new ColMtx[m_DivideX * m_DivideY];

                for (int yi = 0; yi < m_DivideY; ++yi)
                {
                    for (int xi = 0; xi < m_DivideX; ++xi)
                    {
                        //int[] col = new int[3];
                        xyz = new Double[9];

                        for (Int32 c = 0; c < 3; ++c)
                        {
                            for (Int32 i = 0; i < 3; i++)
                            { xyz[c * 3 + i] = measureData[index][c][i]; }
                        }

                        // 補償matrix作成
                        // 実際の光出力:Mmes = 入力信号:Msig * 係数:C -> C = (Msig^-1) * Mmes
                        // 要するに、C : 実際の光出力(measureData) / 入力信号(m_Target)。 
                        // ALTAの誤差やLED特性。●●入れても、■■出てくる、という係数
                        ColMtx Mmes = new ColMtx(xyz);
                        C[index++] = Mmes * Msig.Inv();
                    }
                }
            }
            // Standard Mode(x端、y端４か所測定) or Unit単体（目標を外周に合わせる）
            else if(mode == MeasureMode.Cross4Point || mode == MeasureMode.Unit)
            {
                C = new ColMtx[m_DivideX_4P * m_DivideY_4P];

                // Standard
                if (mode == MeasureMode.Cross4Point)
                {
                    for (int i = 0; i < (int)MeasureMode.Cross4Point; i++)
                    {
                        //int[] col = new int[3];
                        xyz = new Double[9];
                        for (Int32 c = 0; c < 3; ++c)
                        {
                            for (Int32 j = 0; j < 3; j++)
                            { xyz[c * 3 + j] = measureData[index][c][j]; }
                        }

                        ColMtx Mmes = new ColMtx(xyz);
                        C[index++] = Mmes * Msig.Inv();
                    }
                }
                // Unit単体（目標を外周に合わせる）
                else
                {
                    m_MtgtAddCrosstalk = new MtgtAddCrosstalk();

                    for (int i = 0; i < (int)MeasureMode.Cross4Point; i++)
                    {
                        xyz = new Double[9];
                        for (Int32 c = 0; c < 3; ++c)
                        {
                            for (Int32 j = 0; j < 3; j++)
                            { xyz[c * 3 + j] = measureData[index][c][j]; }　// 対象Cabinetの測定値
                        }

                        ColMtx Mmes = new ColMtx(xyz);

                        ColMtx Mtgt;
                        if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
                        {
                            if (measureData[index + 4][0][0] > 0)
                            {
                                xyz = new Double[9];
                                for (Int32 c = 0; c < 3; ++c)
                                {
                                    for (Int32 j = 0; j < 3; j++)
                                    { xyz[c * 3 + j] = measureData[index + 4][c][j]; } // 隣接Cabinetの測定値
                                }

                                Mtgt = new ColMtx(xyz);
                                C[index++] = (Msig * Mtgt.Inv()) * Mmes * Msig.Inv(); // まず通常の補償(Mmes * Msig.Inv())をして、それにtargetのずらし分((Msig * Mtgt.Inv())を適用 
                            }
                            else
                            { C[index++] = Mmes * Msig.Inv(); }
                        }
                        else // LEDModuleConfigurations.Module_4x3
                        {
                            if (measureData[index + 4][0][0] < 0)
                            { measureData[index + 4] = measureData[index]; } // 隣接Cabinetの測定値がない場合、対象Cabinetの測定値を使う

                            if (m_MtgtAddCrosstalk.IsAddCtc)
                            {
                                if (!CalcUncCrosstalkWithAddUncCrosstalk(measureData, index + 4))
                                { return false; }
                            }

                            xyz = new Double[9];
                            for (Int32 c = 0; c < 3; ++c)
                            {
                                for (Int32 j = 0; j < 3; j++)
                                { xyz[c * 3 + j] = measureData[index + 4][c][j]; } // 隣接Cabinetの測定値
                            }

                            Mtgt = new ColMtx(xyz);
                            C[index++] = (Msig * Mtgt.Inv()) * Mmes * Msig.Inv(); // まず通常の補償(Mmes * Msig.Inv())をして、それにtargetのずらし分((Msig * Mtgt.Inv())を適用 
                        }
                    }
                }

                C[5] = C[1]; // Right-Center
                C[1] = C[0]; // Top-Center
                C[7] = C[2]; // Bottom-Center
                C[3] = C[3]; // Left-Center

                C[0] = (C[1] + C[3]) / 2; // LT
                C[2] = (C[1] + C[5]) / 2; // RT
                C[6] = (C[3] + C[7]) / 2; // LB
                C[8] = (C[5] + C[7]) / 2; // RB
                C[4] = (C[0] + C[1] + C[2] + C[3] + C[5] + C[6] + C[7] + C[8]) / 8; // CC
            }
            else { return false; }

            // 補償実行
            ColMtx Mcpst; // 補償後のXYZが入る

            Int32 pos = 0;
            Int32 matrixPosY = 0;

            int ix, iy; // 9pointで分割される4象限を表す。
            ColMtx[] Cp = new ColMtx[3]; // 直線近似で使用される左、中、右のC

            for (int y = 0; y < m_UnitDy; ++y)
            {
                if (mode == MeasureMode.FullCell)
                {
                    matrixPosY = y / m_CellDy * m_DivideX;
                }
                else // 4Point or Unit
                {
                    iy = (y < m_UnitDy / 2) ? 0 : 1;
                    Cp[0] = ((C[3 + 3 * iy] - C[0 + 3 * iy]) / ((m_UnitDy / 2.0) / (y - m_UnitDy / 2.0 * iy))) + C[0 + 3 * iy]; // 左
                    Cp[1] = ((C[4 + 3 * iy] - C[1 + 3 * iy]) / ((m_UnitDy / 2.0) / (y - m_UnitDy / 2.0 * iy))) + C[1 + 3 * iy]; // 中
                    Cp[2] = ((C[5 + 3 * iy] - C[2 + 3 * iy]) / ((m_UnitDy / 2.0) / (y - m_UnitDy / 2.0 * iy))) + C[2 + 3 * iy]; // 右
                }

                for (int x = 0; x < m_UnitDx; ++x)
                {
                    if (mode == MeasureMode.FullCell)
                    {
                        Int32 matrixPos = matrixPosY + x / m_CellDx;
                        ColMtx Morg = new ColMtx(m_pXYZ, 9 * pos);  // Morg : 画素ごとのXYZデータ
                        Mcpst = C[matrixPos] * Morg;  // XYZデータに、Cをかける。

#if DEBUG
                        if (CalcModuleNo(x, y, out int moduleNo))
                        {
                            double[] dData = new double[9];

                            C[matrixPos].GetElement(dData);
                            Console.WriteLine($"Module No.{moduleNo} C: {dData[0]}, {dData[3]}, {dData[6]}, {dData[1]}, {dData[4]}, {dData[7]}, {dData[2]}, {dData[5]}, {dData[8]}");

                            ColMtx Mmes = C[matrixPos] * Msig;
                            Mmes.GetElement(dData);
                            Console.WriteLine($"Module No.{moduleNo} Mmes: {dData[0]}, {dData[3]}, {dData[6]}, {dData[1]}, {dData[4]}, {dData[7]}, {dData[2]}, {dData[5]}, {dData[8]}");

                            Mcpst.GetElement(dData);
                            Console.WriteLine($"Module No.{moduleNo} Mcpst: {dData[0]}, {dData[3]}, {dData[6]}, {dData[1]}, {dData[4]}, {dData[7]}, {dData[2]}, {dData[5]}, {dData[8]}");
                        }
#elif Release_log
                        if (CalcModuleNo(x, y, out int moduleNo))
                        {
                            double[] dData = new double[9];

                            ColMtx Mmes = C[matrixPos] * Msig;
                            Mmes.GetElement(dData);
                            MainWindow.SaveExecLog($"      Module No.{moduleNo} Mmes: {dData[0]}, {dData[3]}, {dData[6]}, {dData[1]}, {dData[4]}, {dData[7]}, {dData[2]}, {dData[5]}, {dData[8]}");

                            C[matrixPos].GetElement(dData);
                            MainWindow.SaveExecLog($"      Module No.{moduleNo} C: {dData[0]}, {dData[3]}, {dData[6]}, {dData[1]}, {dData[4]}, {dData[7]}, {dData[2]}, {dData[5]}, {dData[8]}");

                            Mcpst.GetElement(dData);
                            MainWindow.SaveExecLog($"      Module No.{moduleNo} Mcpst: {dData[0]}, {dData[3]}, {dData[6]}, {dData[1]}, {dData[4]}, {dData[7]}, {dData[2]}, {dData[5]}, {dData[8]}");
                        }
#endif

                    }
                    else // 4Point or Unit
                    {
                        ix = (x < m_UnitDx / 2) ? 0 : 1;
                        ColMtx Morg = new ColMtx(m_pXYZ, 9 * pos);
                        Mcpst = (((Cp[1 + ix] - Cp[0 + ix]) / ((m_UnitDx / 2.0) / (x - m_UnitDx / 2.0 * ix))) + Cp[0 + ix]) * Morg;

#if DEBUG
                        if (CalcModuleNo(x, y, out int moduleNo))
                        {
                            double[] dData = new double[9];

                            //ColMtx Mmes = (((Cp[1 + ix] - Cp[0 + ix]) / (m_UnitDx / 2.0 / (x - m_UnitDx / 2.0 * ix))) + Cp[0 + ix]) * Msig;
                            //Mmes.GetElement(dData);
                            //Console.WriteLine($"Module No.{moduleNo} Mmes: {dData[0]}, {dData[3]}, {dData[6]}, {dData[1]}, {dData[4]}, {dData[7]}, {dData[2]}, {dData[5]}, {dData[8]}");

                            ColMtx TargetC = ((Cp[1 + ix] - Cp[0 + ix]) / (m_UnitDx / 2.0 / (x - m_UnitDx / 2.0 * ix))) + Cp[0 + ix];
                            TargetC.GetElement(dData);
                            Console.WriteLine($"Module No.{moduleNo} C: {dData[0]}, {dData[3]}, {dData[6]}, {dData[1]}, {dData[4]}, {dData[7]}, {dData[2]}, {dData[5]}, {dData[8]}");

                            Mcpst.GetElement(dData);
                            Console.WriteLine($"Module No.{moduleNo} Mcpst: {dData[0]}, {dData[3]}, {dData[6]}, {dData[1]}, {dData[4]}, {dData[7]}, {dData[2]}, {dData[5]}, {dData[8]}");
                        }
#elif Release_log
                        if (CalcModuleNo(x, y, out int moduleNo))
                        {
                            double[] dData = new double[9];

                            //ColMtx Mmes = (((Cp[1 + ix] - Cp[0 + ix]) / (m_UnitDx / 2.0 / (x - m_UnitDx / 2.0 * ix))) + Cp[0 + ix]) * Msig;
                            //Mmes.GetElement(dData);
                            //MainWindow.SaveExecLog($"      Module No.{moduleNo} Mmes: {dData[0]}, {dData[3]}, {dData[6]}, {dData[1]}, {dData[4]}, {dData[7]}, {dData[2]}, {dData[5]}, {dData[8]}");

                            ColMtx TargetC = ((Cp[1 + ix] - Cp[0 + ix]) / (m_UnitDx / 2.0 / (x - m_UnitDx / 2.0 * ix))) + Cp[0 + ix];
                            TargetC.GetElement(dData);
                            MainWindow.SaveExecLog($"      Module No.{moduleNo} C: {dData[0]}, {dData[3]}, {dData[6]}, {dData[1]}, {dData[4]}, {dData[7]}, {dData[2]}, {dData[5]}, {dData[8]}");

                            Mcpst.GetElement(dData);
                            MainWindow.SaveExecLog($"      Module No.{moduleNo} Mcpst: {dData[0]}, {dData[3]}, {dData[6]}, {dData[1]}, {dData[4]}, {dData[7]}, {dData[2]}, {dData[5]}, {dData[8]}");
                        }
#endif
                    }

                    Mcpst.GetElement(m_pXYZ, 9 * pos);  // m_pXYZ：誤差を含んだXYZデータ　⇒　これが目標XYZと同じになるような補正データを求める。
                    pos++;
                }
            }

            return true;
        }

        /// <summary>先に算出したXYZイメージに、今回の計測値から取得した誤差分を更新する。</summary>
        /// <remarks>パネル単位で一律補正</remarks>
        /// <param name="measureData">今回のXYZ計測値（セル単位）[point][r/g/b][X/Y/Z]
        /// point:X1Y1/X2Y1/...XdevideY1/X1Y2/.../XdevideYdevide
        /// </param>
        /// <param name="mode">全Cell(12点)測定・十字4点</param>
        /// <returns></returns>
        public bool Compensate_XYZ_CellReplace(double[][][] measureData, int CellNo)
        {
            Double[] xyz = new Double[9];
            for (Int32 i = 0; i < 9; ++i)
            { xyz[i] = m_Target[i]; } // xyzは使い回し

            // Msig : 目標のWを満足するためのXr/Yr/Zr/Xg/Yg/Zg/Xb/Yb/Zbの行列式
            // これらの色が発光するための入力信号、という意味合い
            ColMtx Msig = new ColMtx(xyz, 1);
            ColMtx[] C = null;

            // Msig：Targetをもとに算出する行列
            // Mmes：測定値をもとに算出する行列
            // Mtgt：外周の測定値をもとに算出する行列
                        
            C = new ColMtx[1]; // Cell交換ではCは一つだけ
            
            //int[] col = new int[3];
            for (Int32 c = 0; c < 3; ++c)
            {
                for (Int32 i = 0; i < 3; i++)
                { xyz[c * 3 + i] = measureData[0][c][i]; }
            }
            ColMtx Mmes = new ColMtx(xyz);

            m_MtgtAddCrosstalk = new MtgtAddCrosstalk();
            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x3 && m_MtgtAddCrosstalk.IsAddCtc)
            {
                for (int index = 1; index < 5; index++)
                {
                    if (measureData[index][0][0] > 0)
                    {
                        if (!CalcUncCrosstalkWithAddUncCrosstalk(measureData, index))
                        { return false; }
                    }
                }
            }

            // 外周の色度から最終目標への変換行列をMtgtを作成する
            calcColorAverage(measureData, out xyz);

            ColMtx Mtgt = new ColMtx(xyz);

            // 補償matrix作成
            // 実際の光出力:Mmes = 入力信号:Msig * 係数:C -> C = (Msig^-1) * Mmes
            // 要するに、C : 実際の光出力(measureData) / 入力信号(m_Target)。 
            // ALTAの誤差やLED特性。●●入れても、■■出てくる、という係数
            //C[0] = Mmes * Msig.Inv();
            C[0] = (Msig * Mtgt.Inv()) * Mmes * Msig.Inv(); // まず通常の補償(Mmes * Msig.Inv())をして、それにtargetのずらし分((Msig * Mtgt.Inv())を適用            

            // 補償実行
            ColMtx Mcpst; // 補償後のXYZが入る
            int x_start, x_end, y_start, y_end;           

            x_start = CellNo % 4 * m_CellDx;
            x_end = x_start + m_CellDx;
            y_start = CellNo / 4 * m_CellDy;
            y_end = y_start + m_CellDy;

            Int32 pos = 0;
            
            for (int y = 0; y < m_UnitDy; ++y)
            {
                for (int x = 0; x < m_UnitDx; ++x)
                {
                    if (x >= x_start && x < x_end && y >= y_start && y < y_end)
                    {
                        ColMtx Morg = new ColMtx(m_pXYZ, 9 * pos);  // Morg : 画素ごとのXYZデータ
                        Mcpst = C[0] * Morg;  // XYZデータに、Cをかける。                    

                        Mcpst.GetElement(m_pXYZ, 9 * pos);  // m_pXYZ：誤差を含んだXYZデータ　⇒　これが目標XYZと同じになるような補正データを求める。

#if DEBUG
                        if (CalcModuleNo(x, y, out int moduleNo))
                        {
                            double[] dData = new double[9];

                            Mmes.GetElement(dData);
                            Console.WriteLine($"Module No.{moduleNo} Mmes: {dData[0]}, {dData[3]}, {dData[6]}, {dData[1]}, {dData[4]}, {dData[7]}, {dData[2]}, {dData[5]}, {dData[8]}");

                            C[0].GetElement(dData);
                            Console.WriteLine($"Module No.{moduleNo} C: {dData[0]}, {dData[3]}, {dData[6]}, {dData[1]}, {dData[4]}, {dData[7]}, {dData[2]}, {dData[5]}, {dData[8]}");

                            Mcpst.GetElement(dData);
                            Console.WriteLine($"Module No.{moduleNo} Mcpst: {dData[0]}, {dData[3]}, {dData[6]}, {dData[1]}, {dData[4]}, {dData[7]}, {dData[2]}, {dData[5]}, {dData[8]}");
                        }
#elif Release_log
                        if (CalcModuleNo(x, y, out int moduleNo))
                        {
                            double[] dData = new double[9];

                            Mmes.GetElement(dData);
                            MainWindow.SaveExecLog($"      Module No.{moduleNo} Mmes: {dData[0]}, {dData[3]}, {dData[6]}, {dData[1]}, {dData[4]}, {dData[7]}, {dData[2]}, {dData[5]}, {dData[8]}");

                            C[0].GetElement(dData);
                            MainWindow.SaveExecLog($"      Module No.{moduleNo} C: {dData[0]}, {dData[3]}, {dData[6]}, {dData[1]}, {dData[4]}, {dData[7]}, {dData[2]}, {dData[5]}, {dData[8]}");

                            Mcpst.GetElement(dData);
                            MainWindow.SaveExecLog($"      Module No.{moduleNo} Mcpst: {dData[0]}, {dData[3]}, {dData[6]}, {dData[1]}, {dData[4]}, {dData[7]}, {dData[2]}, {dData[5]}, {dData[8]}");
                        }
#endif
                    }
                    pos++;
                }
            }

            return true;
        }

        public void CalcWhiteXYZ_To_RGBXYZ(double xr, double yr, double xg, double yg, double xb, double yb, double xw, double yw, double Yw,
            out double Xr, out double Yr, out double Zr, out double Xg, out double Yg, out double Zg, out double Xb, out double Yb, out double Zb)
        {
            double[] norXYZ_R = new double[3] { xr / yr, 1.0, (1.0 - xr - yr) / yr };
            double[] norXYZ_G = new double[3] { xg / yg, 1.0, (1.0 - xg - yg) / yg };
            double[] norXYZ_B = new double[3] { xb / yb, 1.0, (1.0 - xb - yb) / yb };

            ColMtx mat_XYZ_W = new ColMtx(xw / yw * Yw, Yw, (1.0 - xw - yw) / yw * Yw);

            ColMtx mat_norXYZ_RGB = new ColMtx(norXYZ_R[0], norXYZ_R[1], norXYZ_R[2], norXYZ_G[0], norXYZ_G[1], norXYZ_G[2], norXYZ_B[0], norXYZ_B[1], norXYZ_B[2]);

            ColMtx mat_Y_RGB = mat_norXYZ_RGB.Inv() * mat_XYZ_W;

            double[] Y_RGB = new double[3];
            mat_Y_RGB.GetElement(Y_RGB);

            Xr = xr / yr * Y_RGB[0];
            Yr = Y_RGB[0];
            Zr = (1.0 - xr - yr) / yr * Y_RGB[0];

            Xg = xg / yg * Y_RGB[1];
            Yg = Y_RGB[1];
            Zg = (1.0 - xg - yg) / yg * Y_RGB[1];

            Xb = xb / yb * Y_RGB[2];
            Yb = Y_RGB[2];
            Zb = (1.0 - xb - yb) / yb * Y_RGB[2];
        }

        /// <summary>
        /// この中で、“MODIFY_XYZ MULTIPLY 81 241 80 120 320 360 0.9 0.8 0.7”　というコマンドがありますが、これがxyzを修正しています。
        /// “MODIFY_XYZ MULTIPLY x y dx dy sx sy r g b”  コマンドの意味は、
        /// 　　x,y     ｽﾀｰﾄ座標で、Unitなら(1,1)、Cellの場合はﾎﾟｼﾞｼｮﾝにより異なる（xは1,81,161,241、ｙは1,121,241)
        /// 　　dx,dy Tileｻｲｽﾞで、Cell(80,120)、Unit(320,360)
        /// 　　sx､sy  Unitsize(320,360)で固定
        /// 　　r g bは各色補正率　=　1-(目補正レジスタ値)×0.0009766 
        /// 　　例）例えばRed-75,Green-35,Blue+10のレジスタ値の場合、
        ///     補正率は、r=1-(-75)×0.0009766=1.073
        ///             g=1-(-35)×0.0009766=1.034
        ///             b=1-(+10)×0.0009766=0.990
        /// </summary>
        /// <returns></returns>
        public bool ModifyXYZ_Multiply(int startX, int startY, /*int width, int height,*/ int gainR, int gainG, int gainB)
        {                                //arg[1]      arg[2]      arg[3]     arg[4]
            int sx = m_UnitDx; // Unit Size X arg[5] // P1.2 = 460 / P1.5 = 382;
            int sy = m_UnitDy; // Unit Size Y arg[6] // P1.2 = 270 / P1.5 = 214;
            int dx = m_UnitDx; // Panel(Cell) Size X
            int dy = m_UnitDy; // Panel(Cell) Size Y

            // Cellごとの計算しかしないので固定
            int width = m_CellDx;
            int height = m_CellDy;

            float r = (float)(1 - gainR * 0.0009766);
            float g = (float)(1 - gainG * 0.0009766);
            float b = (float)(1 - gainB * 0.0009766);

            for (int j0 = startY; j0 < dy; j0 += sy) // -1は0～座標に変換
            {
                for (int i0 = startX; i0 < dx; i0 += sx)
                {
                    for (int j = j0; j < j0 + height && j < dy; ++j)
                    {
                        for (int i = i0; i < i0 + width && i < dx; ++i)
                        {
                            m_pXYZ[dx * 9 * j + i * 9 + 0] *= r;
                            m_pXYZ[dx * 9 * j + i * 9 + 1] *= r;
                            m_pXYZ[dx * 9 * j + i * 9 + 2] *= r;
                            m_pXYZ[dx * 9 * j + i * 9 + 3] *= g;
                            m_pXYZ[dx * 9 * j + i * 9 + 4] *= g;
                            m_pXYZ[dx * 9 * j + i * 9 + 5] *= g;
                            m_pXYZ[dx * 9 * j + i * 9 + 6] *= b;
                            m_pXYZ[dx * 9 * j + i * 9 + 7] *= b;
                            m_pXYZ[dx * 9 * j + i * 9 + 8] *= b;
                        }
                    }
                }
            }

            return true;
        }


        // modified by Hotta 2024/08/28 for crosstalk
#if ForCrosstalkCameraUF
        float[,][] m_CorTarget;
#endif

// modified by Hotta 2024/09/27
#if ForCrosstalkCameraUF
        public bool ModifyXYZCam(UfCamAdjustType type, UfCamCorrectionPoint refPt, UfCamCabinetCpInfo targetUnit, bool enabledCrosstalk)
#else
        public bool ModifyXYZCam(UfCamAdjustType type, UfCamCorrectionPoint refPt, UfCamCabinetCpInfo targetUnit)
#endif
        {
            bool isRefCabinet = false;
            PixelValue[] rgbCpGain; // 補正点（測定点）の補正Gain、Cabinetだと9点、Radiatorだと18点、EachModeleだと8点

            // 基準Cabinetかどうかを確認
            if (refPt.Unit.ControllerID == targetUnit.Unit.ControllerID
                && refPt.Unit.PortNo == targetUnit.Unit.PortNo
                && refPt.Unit.UnitNo == targetUnit.Unit.UnitNo)
            { isRefCabinet = true; }

            // modified by Hotta 2024/08/28 for crosstalk
#if ForCrosstalkCameraUF
            m_CorTarget = new float[m_UnitDx, m_UnitDy][];
            for(int y=0;y<m_UnitDy;y++)
            {
                for(int x=0;x<m_UnitDx;x++)
                {
                    m_CorTarget[x, y] = new float[9];
                }
            }

            float[] crosstalk = new float[3];
#endif

            if (type == UfCamAdjustType.Cabinet)
            {
                rgbCpGain = new PixelValue[m_DivideX_4P * m_DivideY_4P]; // 田の字型9点の補正点を持つ

                // 各補正点に格納
                foreach (UfCamCorrectionPoint cp in targetUnit.lstCp)
                {
                    double gainR = refPt.PixelValueR / cp.PixelValueR;
                    double gainG = refPt.PixelValueG / cp.PixelValueG;
                    double gainB = refPt.PixelValueB / cp.PixelValueB;

                    if (cp.Pos == UfCamCorrectPos.Cabinet_Top)
                    { rgbCpGain[1] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Cabinet_Bottom)
                    { rgbCpGain[7] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Cabinet_Left)
                    { rgbCpGain[3] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Cabinet_Right)
                    { rgbCpGain[5] = new PixelValue(gainR, gainG, gainB); }
                }

                // 基準Cabinetの場合、基準点のGainは必ず1
                if (isRefCabinet == true)
                {
                    if (refPt.Quad == Quadrant.Quad_1)
                    { rgbCpGain[6] = new PixelValue(1, 1, 1); }
                    else if (refPt.Quad == Quadrant.Quad_2)
                    { rgbCpGain[8] = new PixelValue(1, 1, 1); }
                    else if (refPt.Quad == Quadrant.Quad_3)
                    { rgbCpGain[2] = new PixelValue(1, 1, 1); }
                    else if (refPt.Quad == Quadrant.Quad_4)
                    { rgbCpGain[0] = new PixelValue(1, 1, 1); }
                    else
                    { return false; }
                }

                // 測定点以外は平均値を使用
                if (rgbCpGain[0] == null)
                { rgbCpGain[0] = new PixelValue((rgbCpGain[1].R + rgbCpGain[3].R) / 2, (rgbCpGain[1].G + rgbCpGain[3].G) / 2, (rgbCpGain[1].B + rgbCpGain[3].B) / 2); }

                if (rgbCpGain[2] == null)
                { rgbCpGain[2] = new PixelValue((rgbCpGain[1].R + rgbCpGain[5].R) / 2, (rgbCpGain[1].G + rgbCpGain[5].G) / 2, (rgbCpGain[1].B + rgbCpGain[5].B) / 2); }

                if (rgbCpGain[6] == null)
                { rgbCpGain[6] = new PixelValue((rgbCpGain[3].R + rgbCpGain[7].R) / 2, (rgbCpGain[3].G + rgbCpGain[7].G) / 2, (rgbCpGain[3].B + rgbCpGain[7].B) / 2); }

                if (rgbCpGain[8] == null)
                { rgbCpGain[8] = new PixelValue((rgbCpGain[5].R + rgbCpGain[7].R) / 2, (rgbCpGain[5].G + rgbCpGain[7].G) / 2, (rgbCpGain[5].B + rgbCpGain[7].B) / 2); }

                rgbCpGain[4] = new PixelValue((rgbCpGain[1].R + rgbCpGain[3].R + rgbCpGain[5].R + rgbCpGain[7].R) / 4, (rgbCpGain[1].G + rgbCpGain[3].G + rgbCpGain[5].G + rgbCpGain[7].G) / 4, (rgbCpGain[1].B + rgbCpGain[3].B + rgbCpGain[5].B + rgbCpGain[7].B) / 4);

                // 新しい係数を作成
                string textR = "", textG = "", textB = "";
                string lineR = "", lineG = "", lineB = "";

                // ModuleのLoop
                for (int y = 0; y < m_UnitDy; y++)
                {
                    if (Settings.Ins.ExecLog == true)
                    {
                        lineR = "";
                        lineG = "";
                        lineB = "";
                    }

                    for (int x = 0; x < m_UnitDx; x++)
                    {
                        double gainR, gainG, gainB;
                        calcCamGain(type, rgbCpGain, x, y, out gainR, out gainG, out gainB);

                        // modified by Hotta 2024/08/28 for crosstalk
#if ForCrosstalkCameraUF
                        if(enabledCrosstalk == true)
                        {
                            foreach (MainWindow.CrosstalkInfo info in MainWindow.ListCrosstalkInfo)
                            {
                                if (info.ControllerID == targetUnit.Unit.ControllerID && info.X == targetUnit.Unit.X && info.Y == targetUnit.Unit.Y)
                                {
                                    crosstalk[0] = info.Crosstalk[y / m_CellDy * m_DivideX + x / m_CellDx][0];
                                    crosstalk[1] = info.Crosstalk[y / m_CellDy * m_DivideX + x / m_CellDx][1];
                                    crosstalk[2] = info.Crosstalk[y / m_CellDy * m_DivideX + x / m_CellDx][2];
                                    break;
                                }
                            }
                        }
                        else
                        {
                            crosstalk[0] = crosstalk[1] = crosstalk[2] = 0;
                        }

                        if (Settings.Ins.ExecLog == true)
                        {
                            lineR += gainR * (1.0 + crosstalk[0]) + ", ";
                            lineG += gainG * (1.0 + crosstalk[1]) + ", ";
                            lineB += gainB * (1.0 + crosstalk[2]) + ", ";
                        }

                        // R/G/BごとのオリジナルのX/Y/Zターゲット

                        double[] target = new double[] { m_Target[0], m_Target[1], m_Target[2], m_Target[3], m_Target[4], m_Target[5], m_Target[6], m_Target[7], m_Target[8] };
                        ColMtx Msig = new ColMtx(target, 1);

                        // 新しいターゲット
                        ColMtx gain = new ColMtx(
                            gainR * (1.0 + crosstalk[0]), 0, 0,
                            0, gainG * (1.0 + crosstalk[1]), 0,
                            0, 0, gainB * (1.0 + crosstalk[2])
                        );

                        ColMtx MsigNew = Msig * gain;
                        double[] newTarget = new double[9];
                        MsigNew.GetElement(newTarget);

                        m_CorTarget[x, y][0] = (float)(newTarget[0] / (newTarget[0] + newTarget[1] + newTarget[2]));   // xr = RX / (RX + RY + RZ)
                        m_CorTarget[x, y][1] = (float)(newTarget[1] / (newTarget[0] + newTarget[1] + newTarget[2]));   // yr = RY / (RX + RY + RZ)
                        m_CorTarget[x, y][2] = (float)(newTarget[3] / (newTarget[3] + newTarget[4] + newTarget[5]));   // xg = GX / (GX + GY + GZ)
                        m_CorTarget[x, y][3] = (float)(newTarget[4] / (newTarget[3] + newTarget[4] + newTarget[5]));   // yg = GY / (GX + GY + GZ)
                        m_CorTarget[x, y][4] = (float)(newTarget[6] / (newTarget[6] + newTarget[7] + newTarget[8]));   // xb = BX / (BX + BY + BZ)
                        m_CorTarget[x, y][5] = (float)(newTarget[7] / (newTarget[6] + newTarget[7] + newTarget[8]));   // yb = BY / (BX + BY + BZ)
                        m_CorTarget[x, y][6] = (float)(newTarget[1] + newTarget[4] + newTarget[7]);   // Yw = RY + GY + BY
                        m_CorTarget[x, y][7] = (float)((newTarget[0] + newTarget[3] + newTarget[6]) / (newTarget[0] + newTarget[3] + newTarget[6] + newTarget[1] + newTarget[4] + newTarget[7] + newTarget[2] + newTarget[5] + newTarget[8]));  // xw = X / (X + Y + Z)
                        m_CorTarget[x, y][8] = (float)((newTarget[1] + newTarget[4] + newTarget[7]) / (newTarget[0] + newTarget[3] + newTarget[6] + newTarget[1] + newTarget[4] + newTarget[7] + newTarget[2] + newTarget[5] + newTarget[8]));  // yw = Y / (X + Y + Z)

#else
                        float r = (float)(1 + (1 - gainR));
                        float g = (float)(1 + (1 - gainG));
                        float b = (float)(1 + (1 - gainB));

                        if (Settings.Ins.ExecLog == true)
                        {
                            lineR += gainR + ", ";
                            lineG += gainG + ", ";
                            lineB += gainB + ", ";
                        }

                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 0] *= (float)r;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 1] *= (float)r;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 2] *= (float)r;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 3] *= (float)g;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 4] *= (float)g;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 5] *= (float)g;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 6] *= (float)b;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 7] *= (float)b;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 8] *= (float)b;
#endif
                    }

                    if (Settings.Ins.ExecLog == true)
                    {
                        textR += lineR + "\r\n";
                        textG += lineG + "\r\n";
                        textB += lineB + "\r\n";
                    }
                }
            }
            else if (type == UfCamAdjustType.Radiator)
            {
                rgbCpGain = new PixelValue[m_DivideX_4P * m_DivideY_4P * 2]; // 田の字型9点×2の補正点を持つ

                // 各補正点に格納
                foreach (UfCamCorrectionPoint cp in targetUnit.lstCp)
                {
                    double gainR = refPt.PixelValueR / cp.PixelValueR;
                    double gainG = refPt.PixelValueG / cp.PixelValueG;
                    double gainB = refPt.PixelValueB / cp.PixelValueB;

                    if (cp.Pos == UfCamCorrectPos.Radiator_L_Top)
                    { rgbCpGain[1] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Radiator_L_Bottom)
                    { rgbCpGain[13] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Radiator_L_Left)
                    { rgbCpGain[6] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Radiator_L_Right)
                    { rgbCpGain[8] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Radiator_R_Top)
                    { rgbCpGain[4] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Radiator_R_Bottom)
                    { rgbCpGain[16] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Radiator_R_Left)
                    { rgbCpGain[9] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Radiator_R_Right)
                    { rgbCpGain[11] = new PixelValue(gainR, gainG, gainB); }
                }

                // 基準Cabinetの場合、基準点のGainは必ず1
                if (isRefCabinet == true)
                {
                    if (refPt.Quad == Quadrant.Quad_1)
                    { rgbCpGain[12] = new PixelValue(1, 1, 1); }
                    else if (refPt.Quad == Quadrant.Quad_2)
                    { rgbCpGain[17] = new PixelValue(1, 1, 1); }
                    else if (refPt.Quad == Quadrant.Quad_3)
                    { rgbCpGain[5] = new PixelValue(1, 1, 1); }
                    else if (refPt.Quad == Quadrant.Quad_4)
                    { rgbCpGain[0] = new PixelValue(1, 1, 1); }
                    else
                    { return false; }
                }

                // 測定点以外は平均値を使用
                if (rgbCpGain[0] == null)
                { rgbCpGain[0] = new PixelValue((rgbCpGain[1].R + rgbCpGain[6].R) / 2, (rgbCpGain[1].G + rgbCpGain[6].G) / 2, (rgbCpGain[1].B + rgbCpGain[6].B) / 2); }

                if (rgbCpGain[2] == null)
                { rgbCpGain[2] = new PixelValue((rgbCpGain[1].R + rgbCpGain[8].R) / 2, (rgbCpGain[1].G + rgbCpGain[8].G) / 2, (rgbCpGain[1].B + rgbCpGain[8].B) / 2); }

                if (rgbCpGain[3] == null)
                { rgbCpGain[3] = new PixelValue((rgbCpGain[4].R + rgbCpGain[9].R) / 2, (rgbCpGain[4].G + rgbCpGain[9].G) / 2, (rgbCpGain[4].B + rgbCpGain[9].B) / 2); }

                if (rgbCpGain[5] == null)
                { rgbCpGain[5] = new PixelValue((rgbCpGain[4].R + rgbCpGain[11].R) / 2, (rgbCpGain[4].G + rgbCpGain[11].G) / 2, (rgbCpGain[4].B + rgbCpGain[11].B) / 2); }

                if (rgbCpGain[12] == null)
                { rgbCpGain[12] = new PixelValue((rgbCpGain[6].R + rgbCpGain[13].R) / 2, (rgbCpGain[6].G + rgbCpGain[13].G) / 2, (rgbCpGain[6].B + rgbCpGain[13].B) / 2); }

                if (rgbCpGain[14] == null)
                { rgbCpGain[14] = new PixelValue((rgbCpGain[8].R + rgbCpGain[13].R) / 2, (rgbCpGain[8].G + rgbCpGain[13].G) / 2, (rgbCpGain[8].B + rgbCpGain[13].B) / 2); }

                if (rgbCpGain[15] == null)
                { rgbCpGain[15] = new PixelValue((rgbCpGain[9].R + rgbCpGain[16].R) / 2, (rgbCpGain[9].G + rgbCpGain[16].G) / 2, (rgbCpGain[9].B + rgbCpGain[16].B) / 2); }

                if (rgbCpGain[17] == null)
                { rgbCpGain[17] = new PixelValue((rgbCpGain[11].R + rgbCpGain[16].R) / 2, (rgbCpGain[11].G + rgbCpGain[16].G) / 2, (rgbCpGain[11].B + rgbCpGain[16].B) / 2); }

                // 中心
                rgbCpGain[7] = new PixelValue((rgbCpGain[1].R + rgbCpGain[6].R + rgbCpGain[8].R + rgbCpGain[13].R) / 4, (rgbCpGain[1].G + rgbCpGain[6].G + rgbCpGain[8].G + rgbCpGain[13].G) / 4, (rgbCpGain[1].B + rgbCpGain[6].B + rgbCpGain[8].B + rgbCpGain[13].B) / 4);
                rgbCpGain[10] = new PixelValue((rgbCpGain[4].R + rgbCpGain[9].R + rgbCpGain[11].R + rgbCpGain[16].R) / 4, (rgbCpGain[4].G + rgbCpGain[9].G + rgbCpGain[11].G + rgbCpGain[16].G) / 4, (rgbCpGain[4].B + rgbCpGain[9].B + rgbCpGain[11].B + rgbCpGain[16].B) / 4);

                // 新しい係数を作成
                string textR = "", textG = "", textB = "";
                string lineR = "", lineG = "", lineB = "";

                for (int y = 0; y < m_UnitDy; y++)
                {
                    if (Settings.Ins.ExecLog == true)
                    {
                        lineR = "";
                        lineG = "";
                        lineB = "";
                    }

                    for (int x = 0; x < m_UnitDx; x++)
                    {
                        double gainR, gainG, gainB;
                        calcCamGain(type, rgbCpGain, x, y, out gainR, out gainG, out gainB);

                        // modified by Hotta 2024/08/28 for crosstalk
#if ForCrosstalkCameraUF
                        if(enabledCrosstalk == true)
                        {
                            foreach (MainWindow.CrosstalkInfo info in MainWindow.ListCrosstalkInfo)
                            {
                                if (info.ControllerID == targetUnit.Unit.ControllerID && info.X == targetUnit.Unit.X && info.Y == targetUnit.Unit.Y)
                                {
                                    crosstalk[0] = info.Crosstalk[y / m_CellDy * m_DivideX + x / m_CellDx][0];
                                    crosstalk[1] = info.Crosstalk[y / m_CellDy * m_DivideX + x / m_CellDx][1];
                                    crosstalk[2] = info.Crosstalk[y / m_CellDy * m_DivideX + x / m_CellDx][2];
                                    break;
                                }
                            }
                        }
                        else
                        {
                            crosstalk[0] = crosstalk[1] = crosstalk[2] = 0;
                        }

                        if (Settings.Ins.ExecLog == true)
                        {
                            lineR += gainR * (1.0 + crosstalk[0]) + ", ";
                            lineG += gainG * (1.0 + crosstalk[1]) + ", ";
                            lineB += gainB * (1.0 + crosstalk[2]) + ", ";
                        }

                        // R/G/BごとのオリジナルのX/Y/Zターゲット
                        double[] target = new double[] { m_Target[0], m_Target[1], m_Target[2], m_Target[3], m_Target[4], m_Target[5], m_Target[6], m_Target[7], m_Target[8] };
                        ColMtx Msig = new ColMtx(target, 1);

                        // 新しいターゲット
                        ColMtx gain = new ColMtx(
                            gainR * (1.0 + crosstalk[0]), 0, 0,
                            0, gainG * (1.0 + crosstalk[1]), 0,
                            0, 0, gainB * (1.0 + crosstalk[2])
                        );

                        ColMtx MsigNew = Msig * gain;
                        double[] newTarget = new double[9];
                        MsigNew.GetElement(newTarget);

                        m_CorTarget[x, y][0] = (float)(newTarget[0] / (newTarget[0] + newTarget[1] + newTarget[2]));   // xr = RX / (RX + RY + RZ)
                        m_CorTarget[x, y][1] = (float)(newTarget[1] / (newTarget[0] + newTarget[1] + newTarget[2]));   // yr = RY / (RX + RY + RZ)
                        m_CorTarget[x, y][2] = (float)(newTarget[3] / (newTarget[3] + newTarget[4] + newTarget[5]));   // xg = GX / (GX + GY + GZ)
                        m_CorTarget[x, y][3] = (float)(newTarget[4] / (newTarget[3] + newTarget[4] + newTarget[5]));   // yg = GY / (GX + GY + GZ)
                        m_CorTarget[x, y][4] = (float)(newTarget[6] / (newTarget[6] + newTarget[7] + newTarget[8]));   // xb = BX / (BX + BY + BZ)
                        m_CorTarget[x, y][5] = (float)(newTarget[7] / (newTarget[6] + newTarget[7] + newTarget[8]));   // yb = BY / (BX + BY + BZ)
                        m_CorTarget[x, y][6] = (float)(newTarget[1] + newTarget[4] + newTarget[7]);   // Yw = RY + GY + BY
                        m_CorTarget[x, y][7] = (float)((newTarget[0] + newTarget[3] + newTarget[6]) / (newTarget[0] + newTarget[3] + newTarget[6] + newTarget[1] + newTarget[4] + newTarget[7] + newTarget[2] + newTarget[5] + newTarget[8]));  // xw = X / (X + Y + Z)
                        m_CorTarget[x, y][8] = (float)((newTarget[1] + newTarget[4] + newTarget[7]) / (newTarget[0] + newTarget[3] + newTarget[6] + newTarget[1] + newTarget[4] + newTarget[7] + newTarget[2] + newTarget[5] + newTarget[8]));  // yw = Y / (X + Y + Z)

#else
                        float r = (float)(1 + (1 - gainR)); // ここの処理が抜けてた？ 20211119
                        float g = (float)(1 + (1 - gainG));
                        float b = (float)(1 + (1 - gainB));

                        if (Settings.Ins.ExecLog == true)
                        {
                            lineR += gainR + ", ";
                            lineG += gainG + ", ";
                            lineB += gainB + ", ";
                        }

                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 0] *= (float)r;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 1] *= (float)r;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 2] *= (float)r;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 3] *= (float)g;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 4] *= (float)g;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 5] *= (float)g;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 6] *= (float)b;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 7] *= (float)b;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 8] *= (float)b;
#endif
                    }

                    if (Settings.Ins.ExecLog == true)
                    {
                        textR += lineR + "\r\n";
                        textG += lineG + "\r\n";
                        textB += lineB + "\r\n";
                    }
                }
            }
            else if (type == UfCamAdjustType.EachModule)
            {
                string textR = "", textG = "", textB = "";
                string lineR = "", lineG = "", lineB = "";

                rgbCpGain = new PixelValue[moduleCount]; // 各Moduleごとに持つ

                // 各補正点に格納
                foreach (UfCamCorrectionPoint cp in targetUnit.lstCp)
                {
                    double gainR = refPt.PixelValueR / cp.PixelValueR;
                    double gainG = refPt.PixelValueG / cp.PixelValueG;
                    double gainB = refPt.PixelValueB / cp.PixelValueB;

                    if (cp.Pos == UfCamCorrectPos.Module_0)
                    { rgbCpGain[0] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Module_1)
                    { rgbCpGain[1] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Module_2)
                    { rgbCpGain[2] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Module_3)
                    { rgbCpGain[3] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Module_4)
                    { rgbCpGain[4] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Module_5)
                    { rgbCpGain[5] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Module_6)
                    { rgbCpGain[6] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Module_7)
                    { rgbCpGain[7] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Module_8)
                    { rgbCpGain[8] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Module_9)
                    { rgbCpGain[9] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Module_10)
                    { rgbCpGain[10] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Module_11)
                    { rgbCpGain[11] = new PixelValue(gainR, gainG, gainB); }
                }

                // 基準Cabinetの場合、基準点のGainは必ず1
                if (isRefCabinet == true)
                {
                    if (moduleCount == m_DivideX * m_DivideY_Chiron) // 8Module = Chiron
                    {
                        if (refPt.Quad == Quadrant.Quad_1)
                        { rgbCpGain[4] = new PixelValue(1, 1, 1); }
                        else if (refPt.Quad == Quadrant.Quad_2)
                        { rgbCpGain[7] = new PixelValue(1, 1, 1); }
                        else if (refPt.Quad == Quadrant.Quad_3)
                        { rgbCpGain[3] = new PixelValue(1, 1, 1); }
                        else if (refPt.Quad == Quadrant.Quad_4)
                        { rgbCpGain[0] = new PixelValue(1, 1, 1); }
                        else
                        { return false; }
                    }
                    else // Cancun
                    {
                        if (refPt.Quad == Quadrant.Quad_1)
                        { rgbCpGain[8] = new PixelValue(1, 1, 1); }
                        else if (refPt.Quad == Quadrant.Quad_2)
                        { rgbCpGain[11] = new PixelValue(1, 1, 1); }
                        else if (refPt.Quad == Quadrant.Quad_3)
                        { rgbCpGain[3] = new PixelValue(1, 1, 1); }
                        else if (refPt.Quad == Quadrant.Quad_4)
                        { rgbCpGain[0] = new PixelValue(1, 1, 1); }
                        else
                        { return false; }
                    }
                }

                // ModuleのLoop
                for (int mod = 0; mod < moduleCount; mod++)
                {
                    int startX, startY; // 各Moduleの始点

                    startX = mod % 4 * m_CellDx;
                    startY = mod / 4 * m_CellDy;

                    // modified by Hotta 2024/08/28
#if ForCrosstalkCameraUF
                    if (enabledCrosstalk == true)
                    {
                        foreach (MainWindow.CrosstalkInfo info in MainWindow.ListCrosstalkInfo)
                        {
                            if (info.ControllerID == targetUnit.Unit.ControllerID && info.X == targetUnit.Unit.X && info.Y == targetUnit.Unit.Y)
                            {
                                crosstalk[0] = info.Crosstalk[mod][0];
                                crosstalk[1] = info.Crosstalk[mod][1];
                                crosstalk[2] = info.Crosstalk[mod][2];
                                break;
                            }
                        }
                    }
                    else
                    {
                        crosstalk[0] = crosstalk[1] = crosstalk[2] = 0;
                    }

                    for (int y = startY; y < startY + m_CellDy; y++)
                    {
                        if (Settings.Ins.ExecLog == true)
                        {
                            lineR = "";
                            lineG = "";
                            lineB = "";
                        }

                        for (int x = startX; x < startX + m_CellDx; x++)
                        {
                            if (Settings.Ins.ExecLog == true)
                            {
                                lineR += rgbCpGain[mod].R * (1.0 + crosstalk[0]) + ", ";
                                lineG += rgbCpGain[mod].G * (1.0 + crosstalk[1]) + ", ";
                                lineB += rgbCpGain[mod].B * (1.0 + crosstalk[2]) + ", ";
                            }

                            // R/G/BごとのオリジナルのX/Y/Zターゲット
                            double[] target = new double[] { m_Target[0], m_Target[1], m_Target[2], m_Target[3], m_Target[4], m_Target[5], m_Target[6], m_Target[7], m_Target[8] };
                            ColMtx Msig = new ColMtx(target, 1);

                            ColMtx gain = null;
                            // 基準Cabinetで基準点Moduleの場合、ターゲットは変更しない
                            if (isRefCabinet == true && mod == refPt.ModuleNo)
                            {
                                gain = new ColMtx(
                                    (1.0 + crosstalk[0]), 0, 0,
                                    0, (1.0 + crosstalk[1]), 0,
                                    0, 0, (1.0 + crosstalk[2])
                                );
                            }
                            else
                            {
                                // 新しいターゲット用の係数
                                // 基準Cabinetで基準点Moduleの場合、rgbCpGain[].R/G/B = 1.0 のはずだが、念のため。。。
                                gain = new ColMtx(
                                    rgbCpGain[mod].R * (1.0 + crosstalk[0]), 0, 0,
                                    0, rgbCpGain[mod].G * (1.0 + crosstalk[1]), 0,
                                    0, 0, rgbCpGain[mod].B * (1.0 + crosstalk[2])
                                );
                            }

                            ColMtx MsigNew = Msig * gain;
                            double[] newTarget = new double[9];
                            MsigNew.GetElement(newTarget);

                            m_CorTarget[x, y][0] = (float)(newTarget[0] / (newTarget[0] + newTarget[1] + newTarget[2]));   // xr = RX / (RX + RY + RZ)
                            m_CorTarget[x, y][1] = (float)(newTarget[1] / (newTarget[0] + newTarget[1] + newTarget[2]));   // yr = RY / (RX + RY + RZ)
                            m_CorTarget[x, y][2] = (float)(newTarget[3] / (newTarget[3] + newTarget[4] + newTarget[5]));   // xg = GX / (GX + GY + GZ)
                            m_CorTarget[x, y][3] = (float)(newTarget[4] / (newTarget[3] + newTarget[4] + newTarget[5]));   // yg = GY / (GX + GY + GZ)
                            m_CorTarget[x, y][4] = (float)(newTarget[6] / (newTarget[6] + newTarget[7] + newTarget[8]));   // xb = BX / (BX + BY + BZ)
                            m_CorTarget[x, y][5] = (float)(newTarget[7] / (newTarget[6] + newTarget[7] + newTarget[8]));   // yb = BY / (BX + BY + BZ)
                            m_CorTarget[x, y][6] = (float)(newTarget[1] + newTarget[4] + newTarget[7]);   // Yw = RY + GY + BY
                            m_CorTarget[x, y][7] = (float)((newTarget[0] + newTarget[3] + newTarget[6]) / (newTarget[0] + newTarget[3] + newTarget[6] + newTarget[1] + newTarget[4] + newTarget[7] + newTarget[2] + newTarget[5] + newTarget[8]));  // xw = X / (X + Y + Z)
                            m_CorTarget[x, y][8] = (float)((newTarget[1] + newTarget[4] + newTarget[7]) / (newTarget[0] + newTarget[3] + newTarget[6] + newTarget[1] + newTarget[4] + newTarget[7] + newTarget[2] + newTarget[5] + newTarget[8]));  // yw = Y / (X + Y + Z)
                        }

                        if (Settings.Ins.ExecLog == true)
                        {
                            textR += lineR + "\r\n";
                            textG += lineG + "\r\n";
                            textB += lineB + "\r\n";
                        }
                    }
#else
                    float r = (float)(1 + (1 - rgbCpGain[mod].R));
                    float g = (float)(1 + (1 - rgbCpGain[mod].G));
                    float b = (float)(1 + (1 - rgbCpGain[mod].B));

                    for (int y = startY; y < startY + m_CellDy; y++)
                    {
                        if (Settings.Ins.ExecLog == true)
                        {
                            lineR = "";
                            lineG = "";
                            lineB = "";
                        }

                        for (int x = startX; x < startX + m_CellDx; x++)
                        {
                            if (Settings.Ins.ExecLog == true)
                            {
                                lineR += r + ", ";
                                lineG += g + ", ";
                                lineB += b + ", ";
                            }

                            m_pXYZ[y * m_UnitDx * 9 + x * 9 + 0] *= (float)r;
                            m_pXYZ[y * m_UnitDx * 9 + x * 9 + 1] *= (float)r;
                            m_pXYZ[y * m_UnitDx * 9 + x * 9 + 2] *= (float)r;
                            m_pXYZ[y * m_UnitDx * 9 + x * 9 + 3] *= (float)g;
                            m_pXYZ[y * m_UnitDx * 9 + x * 9 + 4] *= (float)g;
                            m_pXYZ[y * m_UnitDx * 9 + x * 9 + 5] *= (float)g;
                            m_pXYZ[y * m_UnitDx * 9 + x * 9 + 6] *= (float)b;
                            m_pXYZ[y * m_UnitDx * 9 + x * 9 + 7] *= (float)b;
                            m_pXYZ[y * m_UnitDx * 9 + x * 9 + 8] *= (float)b;
                        }

                        if (Settings.Ins.ExecLog == true)
                        {
                            textR += lineR + "\r\n";
                            textG += lineG + "\r\n";
                            textB += lineB + "\r\n";
                        }
                    }
#endif
                }
            }
            else if (type == UfCamAdjustType.Cabi_9pt)
            {
                rgbCpGain = new PixelValue[m_DivideX_4P * m_DivideY_4P]; // 田の字型9点の補正点を持つ

                // 各補正点に格納
                foreach (UfCamCorrectionPoint cp in targetUnit.lstCp)
                {
                    double gainR = refPt.PixelValueR / cp.PixelValueR;
                    double gainG = refPt.PixelValueG / cp.PixelValueG;
                    double gainB = refPt.PixelValueB / cp.PixelValueB;

                    if(cp.Pos == UfCamCorrectPos._9pt_TopLeft)
                    { rgbCpGain[0] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Cabinet_Top)
                    { rgbCpGain[1] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos._9pt_TopRight)
                    { rgbCpGain[2] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Cabinet_Left)
                    { rgbCpGain[3] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos._9pt_Center)
                    { rgbCpGain[4] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Cabinet_Right)
                    { rgbCpGain[5] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos._9pt_BottomLeft)
                    { rgbCpGain[6] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Cabinet_Bottom)
                    { rgbCpGain[7] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos._9pt_BottomRight)
                    { rgbCpGain[8] = new PixelValue(gainR, gainG, gainB); }
                }

                // 基準Cabinetの場合、基準点のGainは必ず1
                if (isRefCabinet == true)
                {
                    if (refPt.Quad == Quadrant.Quad_1)
                    { rgbCpGain[6] = new PixelValue(1, 1, 1); }
                    else if (refPt.Quad == Quadrant.Quad_2)
                    { rgbCpGain[8] = new PixelValue(1, 1, 1); }
                    else if (refPt.Quad == Quadrant.Quad_3)
                    { rgbCpGain[2] = new PixelValue(1, 1, 1); }
                    else if (refPt.Quad == Quadrant.Quad_4)
                    { rgbCpGain[0] = new PixelValue(1, 1, 1); }
                    else
                    { return false; }
                }

                // 新しい係数を作成
                string textR = "", textG = "", textB = "";
                string lineR = "", lineG = "", lineB = "";

                // ModuleのLoop
                for (int y = 0; y < m_UnitDy; y++)
                {
                    if (Settings.Ins.ExecLog == true)
                    {
                        lineR = "";
                        lineG = "";
                        lineB = "";
                    }

                    for (int x = 0; x < m_UnitDx; x++)
                    {
                        double gainR, gainG, gainB;
                        calcCamGain(type, rgbCpGain, x, y, out gainR, out gainG, out gainB);

                        // modified by Hotta 2024/08/28 for crosstalk
#if ForCrosstalkCameraUF
                        if(enabledCrosstalk == true)
                        {
                            foreach (MainWindow.CrosstalkInfo info in MainWindow.ListCrosstalkInfo)
                            {
                                if (info.ControllerID == targetUnit.Unit.ControllerID && info.X == targetUnit.Unit.X && info.Y == targetUnit.Unit.Y)
                                {
                                    crosstalk[0] = info.Crosstalk[y / m_CellDy * m_DivideX + x / m_CellDx][0];
                                    crosstalk[1] = info.Crosstalk[y / m_CellDy * m_DivideX + x / m_CellDx][1];
                                    crosstalk[2] = info.Crosstalk[y / m_CellDy * m_DivideX + x / m_CellDx][2];
                                }
                            }
                        }
                        else
                        {
                            crosstalk[0] = crosstalk[1] = crosstalk[2] = 0;
                        }

                        if (Settings.Ins.ExecLog == true)
                        {
                            lineR += gainR * (1.0 + crosstalk[0]) + ", ";
                            lineG += gainG * (1.0 + crosstalk[1]) + ", ";
                            lineB += gainB * (1.0 + crosstalk[2]) + ", ";
                        }

                        // R/G/BごとのオリジナルのX/Y/Zターゲット
                        double[] target = new double[] { m_Target[0], m_Target[1], m_Target[2], m_Target[3], m_Target[4], m_Target[5], m_Target[6], m_Target[7], m_Target[8] };
                        ColMtx Msig = new ColMtx(target, 1);

                        // 新しいターゲット
                        ColMtx gain = new ColMtx(
                            gainR * (1.0 + crosstalk[0]), 0, 0,
                            0, gainG * (1.0 + crosstalk[1]), 0,
                            0, 0, gainB * (1.0 + crosstalk[2])
                        );

                        ColMtx MsigNew = Msig * gain;
                        double[] newTarget = new double[9];
                        MsigNew.GetElement(newTarget);

                        m_CorTarget[x, y][0] = (float)(newTarget[0] / (newTarget[0] + newTarget[1] + newTarget[2]));   // xr = RX / (RX + RY + RZ)
                        m_CorTarget[x, y][1] = (float)(newTarget[1] / (newTarget[0] + newTarget[1] + newTarget[2]));   // yr = RY / (RX + RY + RZ)
                        m_CorTarget[x, y][2] = (float)(newTarget[3] / (newTarget[3] + newTarget[4] + newTarget[5]));   // xg = GX / (GX + GY + GZ)
                        m_CorTarget[x, y][3] = (float)(newTarget[4] / (newTarget[3] + newTarget[4] + newTarget[5]));   // yg = GY / (GX + GY + GZ)
                        m_CorTarget[x, y][4] = (float)(newTarget[6] / (newTarget[6] + newTarget[7] + newTarget[8]));   // xb = BX / (BX + BY + BZ)
                        m_CorTarget[x, y][5] = (float)(newTarget[7] / (newTarget[6] + newTarget[7] + newTarget[8]));   // yb = BY / (BX + BY + BZ)
                        m_CorTarget[x, y][6] = (float)(newTarget[1] + newTarget[4] + newTarget[7]);   // Yw = RY + GY + BY
                        m_CorTarget[x, y][7] = (float)((newTarget[0] + newTarget[3] + newTarget[6]) / (newTarget[0] + newTarget[3] + newTarget[6] + newTarget[1] + newTarget[4] + newTarget[7] + newTarget[2] + newTarget[5] + newTarget[8]));  // xw = X / (X + Y + Z)
                        m_CorTarget[x, y][8] = (float)((newTarget[1] + newTarget[4] + newTarget[7]) / (newTarget[0] + newTarget[3] + newTarget[6] + newTarget[1] + newTarget[4] + newTarget[7] + newTarget[2] + newTarget[5] + newTarget[8]));  // yw = Y / (X + Y + Z)

#else

                        float r = (float)(1 + (1 - gainR));
                        float g = (float)(1 + (1 - gainG));
                        float b = (float)(1 + (1 - gainB));

                        if (Settings.Ins.ExecLog == true)
                        {
                            lineR += gainR + ", ";
                            lineG += gainG + ", ";
                            lineB += gainB + ", ";
                        }

                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 0] *= (float)r;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 1] *= (float)r;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 2] *= (float)r;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 3] *= (float)g;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 4] *= (float)g;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 5] *= (float)g;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 6] *= (float)b;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 7] *= (float)b;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 8] *= (float)b;
#endif
                    }

                    if (Settings.Ins.ExecLog == true)
                    {
                        textR += lineR + "\r\n";
                        textG += lineG + "\r\n";
                        textB += lineB + "\r\n";
                    }
                }
            }

            return true;
        }

        public bool ModifyXYZCamCommonLed(UfCamAdjustType type, UfCamCorrectionPoint refPt, UfCamCabinetCpInfo targetUnit)
        {
            bool isRefCabinet = false;
            PixelValue[] rgbCpGain; // 補正点（測定点）の補正Gain、Cabinetだと9点、Radiatorだと18点、EachModeleだと8点

            // 基準Cabinetかどうかを確認
            if (refPt.Unit.ControllerID == targetUnit.Unit.ControllerID
                && refPt.Unit.PortNo == targetUnit.Unit.PortNo
                && refPt.Unit.UnitNo == targetUnit.Unit.UnitNo)
            { isRefCabinet = true; }

            if (type == UfCamAdjustType.Cabinet)
            {
                rgbCpGain = new PixelValue[m_DivideX_4P * m_DivideY_4P]; // 田の字型9点の補正点を持つ

                // 各補正点に格納
                foreach (UfCamCorrectionPoint cp in targetUnit.lstCp)
                {
                    double gainR = refPt.PixelValueR / cp.PixelValueR;
                    double gainG = refPt.PixelValueG / cp.PixelValueG;
                    double gainB = refPt.PixelValueB / cp.PixelValueB;

                    if (cp.Pos == UfCamCorrectPos.Cabinet_Top)
                    { rgbCpGain[1] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Cabinet_Bottom)
                    { rgbCpGain[7] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Cabinet_Left)
                    { rgbCpGain[3] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Cabinet_Right)
                    { rgbCpGain[5] = new PixelValue(gainR, gainG, gainB); }
                }

                // 基準Cabinetの場合、基準点のGainは必ず1
                if (isRefCabinet == true)
                {
                    if (refPt.Quad == Quadrant.Quad_1)
                    { rgbCpGain[6] = new PixelValue(1, 1, 1); }
                    else if (refPt.Quad == Quadrant.Quad_2)
                    { rgbCpGain[8] = new PixelValue(1, 1, 1); }
                    else if (refPt.Quad == Quadrant.Quad_3)
                    { rgbCpGain[2] = new PixelValue(1, 1, 1); }
                    else if (refPt.Quad == Quadrant.Quad_4)
                    { rgbCpGain[0] = new PixelValue(1, 1, 1); }
                    else
                    { return false; }
                }

                // 測定点以外は平均値を使用
                if (rgbCpGain[0] == null)
                { rgbCpGain[0] = new PixelValue((rgbCpGain[1].R + rgbCpGain[3].R) / 2, (rgbCpGain[1].G + rgbCpGain[3].G) / 2, (rgbCpGain[1].B + rgbCpGain[3].B) / 2); }

                if (rgbCpGain[2] == null)
                { rgbCpGain[2] = new PixelValue((rgbCpGain[1].R + rgbCpGain[5].R) / 2, (rgbCpGain[1].G + rgbCpGain[5].G) / 2, (rgbCpGain[1].B + rgbCpGain[5].B) / 2); }

                if (rgbCpGain[6] == null)
                { rgbCpGain[6] = new PixelValue((rgbCpGain[3].R + rgbCpGain[7].R) / 2, (rgbCpGain[3].G + rgbCpGain[7].G) / 2, (rgbCpGain[3].B + rgbCpGain[7].B) / 2); }

                if (rgbCpGain[8] == null)
                { rgbCpGain[8] = new PixelValue((rgbCpGain[5].R + rgbCpGain[7].R) / 2, (rgbCpGain[5].G + rgbCpGain[7].G) / 2, (rgbCpGain[5].B + rgbCpGain[7].B) / 2); }

                rgbCpGain[4] = new PixelValue((rgbCpGain[1].R + rgbCpGain[3].R + rgbCpGain[5].R + rgbCpGain[7].R) / 4, (rgbCpGain[1].G + rgbCpGain[3].G + rgbCpGain[5].G + rgbCpGain[7].G) / 4, (rgbCpGain[1].B + rgbCpGain[3].B + rgbCpGain[5].B + rgbCpGain[7].B) / 4);

                // 新しい係数を作成
                string textR = "", textG = "", textB = "";
                string lineR = "", lineG = "", lineB = "";

                // ModuleのLoop
                for (int y = 0; y < m_UnitDy; y++)
                {
                    if (Settings.Ins.ExecLog == true)
                    {
                        lineR = "";
                        lineG = "";
                        lineB = "";
                    }

                    for (int x = 0; x < m_UnitDx; x++)
                    {
                        double gainR, gainG, gainB;
                        calcCamGain(type, rgbCpGain, x, y, out gainR, out gainG, out gainB);

                        float r = (float)(1 + (1 - gainR));
                        float g = (float)(1 + (1 - gainG));
                        float b = (float)(1 + (1 - gainB));

                        if (Settings.Ins.ExecLog == true)
                        {
                            lineR += gainR + ", ";
                            lineG += gainG + ", ";
                            lineB += gainB + ", ";
                        }

                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 0] *= (float)r;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 1] *= (float)g;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 2] *= (float)b;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 3] *= (float)r;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 4] *= (float)g;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 5] *= (float)b;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 6] *= (float)r;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 7] *= (float)g;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 8] *= (float)b;
                    }

                    if (Settings.Ins.ExecLog == true)
                    {
                        textR += lineR + "\r\n";
                        textG += lineG + "\r\n";
                        textB += lineB + "\r\n";
                    }
                }
            }
            else if (type == UfCamAdjustType.Radiator)
            {
                rgbCpGain = new PixelValue[m_DivideX_4P * m_DivideY_4P * 2]; // 田の字型9点×2の補正点を持つ

                // 各補正点に格納
                foreach (UfCamCorrectionPoint cp in targetUnit.lstCp)
                {
                    double gainR = refPt.PixelValueR / cp.PixelValueR;
                    double gainG = refPt.PixelValueG / cp.PixelValueG;
                    double gainB = refPt.PixelValueB / cp.PixelValueB;

                    if (cp.Pos == UfCamCorrectPos.Radiator_L_Top)
                    { rgbCpGain[1] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Radiator_L_Bottom)
                    { rgbCpGain[13] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Radiator_L_Left)
                    { rgbCpGain[6] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Radiator_L_Right)
                    { rgbCpGain[8] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Radiator_R_Top)
                    { rgbCpGain[4] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Radiator_R_Bottom)
                    { rgbCpGain[16] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Radiator_R_Left)
                    { rgbCpGain[9] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Radiator_R_Right)
                    { rgbCpGain[11] = new PixelValue(gainR, gainG, gainB); }
                }

                // 基準Cabinetの場合、基準点のGainは必ず1
                if (isRefCabinet == true)
                {
                    if (refPt.Quad == Quadrant.Quad_1)
                    { rgbCpGain[12] = new PixelValue(1, 1, 1); }
                    else if (refPt.Quad == Quadrant.Quad_2)
                    { rgbCpGain[17] = new PixelValue(1, 1, 1); }
                    else if (refPt.Quad == Quadrant.Quad_3)
                    { rgbCpGain[5] = new PixelValue(1, 1, 1); }
                    else if (refPt.Quad == Quadrant.Quad_4)
                    { rgbCpGain[0] = new PixelValue(1, 1, 1); }
                    else
                    { return false; }
                }

                // 測定点以外は平均値を使用
                if (rgbCpGain[0] == null)
                { rgbCpGain[0] = new PixelValue((rgbCpGain[1].R + rgbCpGain[6].R) / 2, (rgbCpGain[1].G + rgbCpGain[6].G) / 2, (rgbCpGain[1].B + rgbCpGain[6].B) / 2); }

                if (rgbCpGain[2] == null)
                { rgbCpGain[2] = new PixelValue((rgbCpGain[1].R + rgbCpGain[8].R) / 2, (rgbCpGain[1].G + rgbCpGain[8].G) / 2, (rgbCpGain[1].B + rgbCpGain[8].B) / 2); }

                if (rgbCpGain[3] == null)
                { rgbCpGain[3] = new PixelValue((rgbCpGain[4].R + rgbCpGain[9].R) / 2, (rgbCpGain[4].G + rgbCpGain[9].G) / 2, (rgbCpGain[4].B + rgbCpGain[9].B) / 2); }

                if (rgbCpGain[5] == null)
                { rgbCpGain[5] = new PixelValue((rgbCpGain[4].R + rgbCpGain[11].R) / 2, (rgbCpGain[4].G + rgbCpGain[11].G) / 2, (rgbCpGain[4].B + rgbCpGain[11].B) / 2); }

                if (rgbCpGain[12] == null)
                { rgbCpGain[12] = new PixelValue((rgbCpGain[6].R + rgbCpGain[13].R) / 2, (rgbCpGain[6].G + rgbCpGain[13].G) / 2, (rgbCpGain[6].B + rgbCpGain[13].B) / 2); }

                if (rgbCpGain[14] == null)
                { rgbCpGain[14] = new PixelValue((rgbCpGain[8].R + rgbCpGain[13].R) / 2, (rgbCpGain[8].G + rgbCpGain[13].G) / 2, (rgbCpGain[8].B + rgbCpGain[13].B) / 2); }

                if (rgbCpGain[15] == null)
                { rgbCpGain[15] = new PixelValue((rgbCpGain[9].R + rgbCpGain[16].R) / 2, (rgbCpGain[9].G + rgbCpGain[16].G) / 2, (rgbCpGain[9].B + rgbCpGain[16].B) / 2); }

                if (rgbCpGain[17] == null)
                { rgbCpGain[17] = new PixelValue((rgbCpGain[11].R + rgbCpGain[16].R) / 2, (rgbCpGain[11].G + rgbCpGain[16].G) / 2, (rgbCpGain[11].B + rgbCpGain[16].B) / 2); }

                // 中心
                rgbCpGain[7] = new PixelValue((rgbCpGain[1].R + rgbCpGain[6].R + rgbCpGain[8].R + rgbCpGain[13].R) / 4, (rgbCpGain[1].G + rgbCpGain[6].G + rgbCpGain[8].G + rgbCpGain[13].G) / 4, (rgbCpGain[1].B + rgbCpGain[6].B + rgbCpGain[8].B + rgbCpGain[13].B) / 4);
                rgbCpGain[10] = new PixelValue((rgbCpGain[4].R + rgbCpGain[9].R + rgbCpGain[11].R + rgbCpGain[16].R) / 4, (rgbCpGain[4].G + rgbCpGain[9].G + rgbCpGain[11].G + rgbCpGain[16].G) / 4, (rgbCpGain[4].B + rgbCpGain[9].B + rgbCpGain[11].B + rgbCpGain[16].B) / 4);

                // 新しい係数を作成
                string textR = "", textG = "", textB = "";
                string lineR = "", lineG = "", lineB = "";

                for (int y = 0; y < m_UnitDy; y++)
                {
                    if (Settings.Ins.ExecLog == true)
                    {
                        lineR = "";
                        lineG = "";
                        lineB = "";
                    }

                    for (int x = 0; x < m_UnitDx; x++)
                    {
                        double gainR, gainG, gainB;
                        calcCamGain(type, rgbCpGain, x, y, out gainR, out gainG, out gainB);

                        float r = (float)(1 + (1 - gainR));
                        float g = (float)(1 + (1 - gainG));
                        float b = (float)(1 + (1 - gainB));

                        if (Settings.Ins.ExecLog == true)
                        {
                            lineR += gainR + ", ";
                            lineG += gainG + ", ";
                            lineB += gainB + ", ";
                        }

                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 0] *= (float)r;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 1] *= (float)g;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 2] *= (float)b;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 3] *= (float)r;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 4] *= (float)g;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 5] *= (float)b;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 6] *= (float)r;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 7] *= (float)g;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 8] *= (float)b;
                    }

                    if (Settings.Ins.ExecLog == true)
                    {
                        textR += lineR + "\r\n";
                        textG += lineG + "\r\n";
                        textB += lineB + "\r\n";
                    }
                }
            }
            else if (type == UfCamAdjustType.EachModule)
            {
                string textR = "", textG = "", textB = "";
                string lineR = "", lineG = "", lineB = "";

                rgbCpGain = new PixelValue[moduleCount]; // 各Moduleごとに持つ

                // 各補正点に格納
                foreach (UfCamCorrectionPoint cp in targetUnit.lstCp)
                {
                    double gainR = refPt.PixelValueR / cp.PixelValueR;
                    double gainG = refPt.PixelValueG / cp.PixelValueG;
                    double gainB = refPt.PixelValueB / cp.PixelValueB;

                    if (cp.Pos == UfCamCorrectPos.Module_0)
                    { rgbCpGain[0] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Module_1)
                    { rgbCpGain[1] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Module_2)
                    { rgbCpGain[2] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Module_3)
                    { rgbCpGain[3] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Module_4)
                    { rgbCpGain[4] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Module_5)
                    { rgbCpGain[5] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Module_6)
                    { rgbCpGain[6] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Module_7)
                    { rgbCpGain[7] = new PixelValue(gainR, gainG, gainB); }
                }

                // ModuleのLoop
                for (int mod = 0; mod < moduleCount; mod++)
                {
                    int startX, startY; // 各Moduleの始点

                    startX = mod % 4 * m_CellDx;
                    startY = mod / 4 * m_CellDy;

                    float r = (float)(1 + (1 - rgbCpGain[mod].R));
                    float g = (float)(1 + (1 - rgbCpGain[mod].G));
                    float b = (float)(1 + (1 - rgbCpGain[mod].B));

                    for (int y = startY; y < startY + m_CellDy; y++)
                    {
                        if (Settings.Ins.ExecLog == true)
                        {
                            lineR = "";
                            lineG = "";
                            lineB = "";
                        }

                        for (int x = startX; x < startX + m_CellDx; x++)
                        {
                            if (Settings.Ins.ExecLog == true)
                            {
                                lineR += r + ", ";
                                lineG += g + ", ";
                                lineB += b + ", ";
                            }

                            m_pXYZ[y * m_UnitDx * 9 + x * 9 + 0] *= (float)r;
                            m_pXYZ[y * m_UnitDx * 9 + x * 9 + 1] *= (float)g;
                            m_pXYZ[y * m_UnitDx * 9 + x * 9 + 2] *= (float)b;
                            m_pXYZ[y * m_UnitDx * 9 + x * 9 + 3] *= (float)r;
                            m_pXYZ[y * m_UnitDx * 9 + x * 9 + 4] *= (float)g;
                            m_pXYZ[y * m_UnitDx * 9 + x * 9 + 5] *= (float)b;
                            m_pXYZ[y * m_UnitDx * 9 + x * 9 + 6] *= (float)r;
                            m_pXYZ[y * m_UnitDx * 9 + x * 9 + 7] *= (float)g;
                            m_pXYZ[y * m_UnitDx * 9 + x * 9 + 8] *= (float)b;

                            //m_pXYZ[y * m_UnitDx * 9 + x * 9 + 0] *= (float)rgbCpGain[mod].R;
                            //m_pXYZ[y * m_UnitDx * 9 + x * 9 + 1] *= (float)rgbCpGain[mod].G * (float)rgbCpGain[mod].R;
                            //m_pXYZ[y * m_UnitDx * 9 + x * 9 + 2] *= (float)rgbCpGain[mod].B * (float)rgbCpGain[mod].R;
                            //m_pXYZ[y * m_UnitDx * 9 + x * 9 + 3] *= (float)rgbCpGain[mod].R * (float)rgbCpGain[mod].G;
                            //m_pXYZ[y * m_UnitDx * 9 + x * 9 + 4] *= (float)rgbCpGain[mod].G;
                            //m_pXYZ[y * m_UnitDx * 9 + x * 9 + 5] *= (float)rgbCpGain[mod].B * (float)rgbCpGain[mod].G;
                            //m_pXYZ[y * m_UnitDx * 9 + x * 9 + 6] *= (float)rgbCpGain[mod].R * (float)rgbCpGain[mod].B;
                            //m_pXYZ[y * m_UnitDx * 9 + x * 9 + 7] *= (float)rgbCpGain[mod].G * (float)rgbCpGain[mod].B;
                            //m_pXYZ[y * m_UnitDx * 9 + x * 9 + 8] *= (float)rgbCpGain[mod].B;

                            //m_pXYZ[y * m_UnitDx * 9 + x * 9 + 0] *= (float)r;
                            //m_pXYZ[y * m_UnitDx * 9 + x * 9 + 1] *= (float)g * r;
                            //m_pXYZ[y * m_UnitDx * 9 + x * 9 + 2] *= (float)b * r;
                            //m_pXYZ[y * m_UnitDx * 9 + x * 9 + 3] *= (float)r * g;
                            //m_pXYZ[y * m_UnitDx * 9 + x * 9 + 4] *= (float)g;
                            //m_pXYZ[y * m_UnitDx * 9 + x * 9 + 5] *= (float)b * g;
                            //m_pXYZ[y * m_UnitDx * 9 + x * 9 + 6] *= (float)r * b;
                            //m_pXYZ[y * m_UnitDx * 9 + x * 9 + 7] *= (float)g * b;
                            //m_pXYZ[y * m_UnitDx * 9 + x * 9 + 8] *= (float)b;
                        }

                        if (Settings.Ins.ExecLog == true)
                        {
                            textR += lineR + "\r\n";
                            textG += lineG + "\r\n";
                            textB += lineB + "\r\n";
                        }
                    }
                }
            }
            else if (type == UfCamAdjustType.Cabi_9pt)
            {
                rgbCpGain = new PixelValue[m_DivideX_4P * m_DivideY_4P]; // 田の字型9点の補正点を持つ

                // 各補正点に格納
                foreach (UfCamCorrectionPoint cp in targetUnit.lstCp)
                {
                    double gainR = refPt.PixelValueR / cp.PixelValueR;
                    double gainG = refPt.PixelValueG / cp.PixelValueG;
                    double gainB = refPt.PixelValueB / cp.PixelValueB;

                    if (cp.Pos == UfCamCorrectPos._9pt_TopLeft)
                    { rgbCpGain[0] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Cabinet_Top)
                    { rgbCpGain[1] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos._9pt_TopRight)
                    { rgbCpGain[2] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Cabinet_Left)
                    { rgbCpGain[3] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos._9pt_Center)
                    { rgbCpGain[4] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Cabinet_Right)
                    { rgbCpGain[5] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos._9pt_BottomLeft)
                    { rgbCpGain[6] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos.Cabinet_Bottom)
                    { rgbCpGain[7] = new PixelValue(gainR, gainG, gainB); }
                    else if (cp.Pos == UfCamCorrectPos._9pt_BottomRight)
                    { rgbCpGain[8] = new PixelValue(gainR, gainG, gainB); }
                }

                // 基準Cabinetの場合、基準点のGainは必ず1
                if (isRefCabinet == true)
                {
                    if (refPt.Quad == Quadrant.Quad_1)
                    { rgbCpGain[6] = new PixelValue(1, 1, 1); }
                    else if (refPt.Quad == Quadrant.Quad_2)
                    { rgbCpGain[8] = new PixelValue(1, 1, 1); }
                    else if (refPt.Quad == Quadrant.Quad_3)
                    { rgbCpGain[2] = new PixelValue(1, 1, 1); }
                    else if (refPt.Quad == Quadrant.Quad_4)
                    { rgbCpGain[0] = new PixelValue(1, 1, 1); }
                    else
                    { return false; }
                }

                // 新しい係数を作成
                string textR = "", textG = "", textB = "";
                string lineR = "", lineG = "", lineB = "";

                // ModuleのLoop
                for (int y = 0; y < m_UnitDy; y++)
                {
                    if (Settings.Ins.ExecLog == true)
                    {
                        lineR = "";
                        lineG = "";
                        lineB = "";
                    }

                    for (int x = 0; x < m_UnitDx; x++)
                    {
                        double gainR, gainG, gainB;
                        calcCamGain(type, rgbCpGain, x, y, out gainR, out gainG, out gainB);

                        float r = (float)(1 + (1 - gainR));
                        float g = (float)(1 + (1 - gainG));
                        float b = (float)(1 + (1 - gainB));

                        if (Settings.Ins.ExecLog == true)
                        {
                            lineR += gainR + ", ";
                            lineG += gainG + ", ";
                            lineB += gainB + ", ";
                        }

                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 0] *= (float)r;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 1] *= (float)g;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 2] *= (float)b;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 3] *= (float)r;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 4] *= (float)g;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 5] *= (float)b;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 6] *= (float)r;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 7] *= (float)g;
                        m_pXYZ[y * m_UnitDx * 9 + x * 9 + 8] *= (float)b;
                    }

                    if (Settings.Ins.ExecLog == true)
                    {
                        textR += lineR + "\r\n";
                        textG += lineG + "\r\n";
                        textB += lineB + "\r\n";
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 補正データを作成する
        /// </summary>
        /// <param name="underCompensate"></param>
        /// <param name="targetYw"></param>
        /// <param name="targetYr"></param>
        /// <param name="targetYg"></param>
        /// <param name="targetYb"></param>
        /// <param name="ucr"></param>
        /// <param name="ucg"></param>
        /// <param name="ucb"></param>
        /// <returns></returns>
        public bool Statistics(double underCompensate, out double targetYw, out double targetYr, out double targetYg, out double targetYb, out int ucr, out int ucg, out int ucb)
        {
            bool status;
            ucr = ucg = ucb = 0;

            if (Settings.Ins.LedModuleConfiguration == LEDModuleConfigurations.Module_4x2)
            {
                status = CalcFmt(underCompensate, -1, -1, -1, false,
                    out targetYw, out targetYr, out targetYg, out targetYb, out ucr, out ucg, out ucb);
            }
            else
            {
                status = CalcFmtWithCrosstalk(out targetYw, out targetYr, out targetYg, out targetYb);
            }

            return status;
        }

        // added by Hotta 2024/09/12
#if ForCrosstalkCameraUF
        /// <summary>
        /// 補正データを作成する
        /// </summary>
        /// <param name="underCompensate"></param>
        /// <param name="targetYw"></param>
        /// <param name="targetYr"></param>
        /// <param name="targetYg"></param>
        /// <param name="targetYb"></param>
        /// <param name="ucr"></param>
        /// <param name="ucg"></param>
        /// <param name="ucb"></param>
        /// <returns></returns>
        public bool Statistics_CameraUF(UnitInfo unit, out double targetYw, out double targetYr, out double targetYg, out double targetYb)
        {
            bool status = CalcFmt_CameraUF(unit, out targetYw, out targetYr, out targetYg, out targetYb);

            return status;
        }


#endif

        /// <summary>
        ///既存celldataファイルのムラdata部分だけを内部メモリ上のfmtから作成したdataで上書きする 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        //public bool OverWritePixelData(string fileName)
        //{
        //    int i, cells = 0;
        //    byte[] buf = new byte[0x20000]; // celldataの1つ分のsize
        //    //const float *fmt = m_pPage1->GetCurrentFmt();
        //    int dx = m_UnitDx;
        //    int dy = m_UnitDy;

        //    FileStream fin = new FileStream(m_SourceCellDataFileName, FileMode.Open, FileAccess.Read);
        //    FileStream fout = new FileStream(fileName, FileMode.Create, FileAccess.Write);

        //    for (int y0 = 0; y0 < dy; y0 += m_CellDy)
        //    {
        //        for (int x0 = 0; x0 < dx; x0 += m_CellDx)
        //        {
        //            fin.Seek(0x20000 * cells, SeekOrigin.Begin); // skip
        //            fin.Read(buf, 0, 0x20000);

        //            // temperature
        //            long temperature = (long)(m_CellTemperature[cells] / 0.0625);
        //            buf[(32 * 6 + 1856 + 8192 + 32) / 8 + 0] = (byte)(temperature >> 0);
        //            buf[(32 * 6 + 1856 + 8192 + 32) / 8 + 1] = (byte)(temperature >> 8);
        //            buf[(32 * 6 + 1856 + 8192 + 32) / 8 + 2] = (byte)(temperature >> 16);
        //            buf[(32 * 6 + 1856 + 8192 + 32) / 8 + 3] = (byte)(temperature >> 24);

        //            // 補正data
        //            long index = (32 * 6 + 1856 + 8192 + 32 * 3 + 1440) / 8; // reserve,焼付補正パラメータ,ectをskip。

        //            for (int y = y0; y < y0 + m_CellDy; ++y)
        //            {
        //                for (int x = x0; x < x0 + m_CellDx; x += 2)
        //                {
        //                    int[] fmb = new int[18];
        //                    // binary化

        //                    for (i = 0; i < 9 * 2; ++i)
        //                    { // 2画素分で1周期

        //                        fmb[i] = (int)(m_pFmtCreated[y * m_UnitDx * 9 + x * 9 + i] * 2048 + 0.5);
        //                        //fmb[i] = (int)(m_pFmtOriginal[y * m_UnitDx  * 9 + x * 9 + i] * 2048);
        //                        if (Math.Abs((int)(m_pFmtCreated[y * m_UnitDx * 9 + x * 9 + i] * 2048 + 0.5) - (int)(m_pFmtOriginal[y * m_UnitDx * 9 + x * 9 + i] * 2048)) >= 1)
        //                        {
        //                            //MessageBox.Show("different.");
        //                        }

        //                        int m = (i > 8) ? i - 9 : i; // 対角を調べる目印
        //                        if (m % 4 != 0)
        //                            fmb[i] += 2048; // 比対角だけ+2048
        //                        if (fmb[i] < 0)
        //                            fmb[i] = 0; // clip処理

        //                        else if (fmb[i] > 4095)
        //                            fmb[i] = 4095;
        //                    }

        //                    // packing
        //                    for (i = 0; i < 9 * 2; i += 2)
        //                    {
        //                        buf[index++] = (byte)(fmb[i + 0] & 0xff);
        //                        buf[index++] = (byte)(((fmb[i + 1] << 4) & 0xf0) | ((fmb[i + 0] >> 8) & 0x0f));
        //                        buf[index++] = (byte)(fmb[i + 1] >> 4);
        //                    }
        //                }
        //            }

        //            // check sum
        //            uint sum = 0;
        //            index = (32 * 6 + 1856 + 8192) / 8; // pixel_data()の先頭

        //            for (i = 0; i < 1038336 / 32; ++i)
        //            {
        //                // DWORD(32bit)単位で足す。

        //                sum += (uint)(buf[index + i * 4 + 0] << 0) +
        //                        (uint)(buf[index + i * 4 + 1] << 8) +
        //                        (uint)(buf[index + i * 4 + 2] << 16) +
        //                        (uint)(buf[index + i * 4 + 3] << 24);
        //            }
        //            // check sumの場所(32bit)だけ除く

        //            sum -= (uint)(buf[index + (32 + 32 + 1440) / 8 + 0] << 0) +
        //                    (uint)(buf[index + (32 + 32 + 1440) / 8 + 1] << 8) +
        //                    (uint)(buf[index + (32 + 32 + 1440) / 8 + 2] << 16) +
        //                    (uint)(buf[index + (32 + 32 + 1440) / 8 + 3] << 24);

        //            buf[index + (32 + 32 + 1440) / 8 + 0] = (byte)(sum & 0x000000ff);
        //            buf[index + (32 + 32 + 1440) / 8 + 1] = (byte)((sum & 0x0000ff00) >> 8);
        //            buf[index + (32 + 32 + 1440) / 8 + 2] = (byte)((sum & 0x00ff0000) >> 16);
        //            buf[index + (32 + 32 + 1440) / 8 + 3] = (byte)((sum & 0xff000000) >> 24);

        //            fout.Seek(0x20000 * cells, SeekOrigin.Begin); // skip
        //            fout.Write(buf, 0, 0x20000);
        //            ++cells;
        //        }
        //    }
        //    fin.Close();
        //    fout.Close();

        //    return true;
        //}
        public bool OverWritePixelData(string fileName, string ledModel , bool allowCvLimit = false)
        {
            int cells = 0;
            int dx = m_UnitDx;
            int dy = m_UnitDy;
            int size = m_CellDx * m_CellDy * 8; // 1Pixel = 8Byte, P1.2 : 129600[Byte] / P1.5 : 82944[Byte]
            int hcDataLength, hcModuleDataLength, hcCcDataLength;
            byte[] crc;

            byte[] srcBytes = File.ReadAllBytes(m_SourceCellDataFileName);
            byte[] newBytes = new byte[srcBytes.Length];

            switch (ledModel)
            {
                case MainWindow.ZRD_C15A:
                case MainWindow.ZRD_B15A:
                    hcDataLength = MainWindow.HcDataLengthP15_Module4x2;
                    hcModuleDataLength = MainWindow.HcModuleDataLengthP15_Module4x2;
                    hcCcDataLength = MainWindow.HcCcDataLengthP15_Module4x2;
                    break;
                case MainWindow.ZRD_CH12D:
                case MainWindow.ZRD_BH12D:
                case MainWindow.ZRD_CH12D_S3:
                case MainWindow.ZRD_BH12D_S3:
                    hcDataLength = MainWindow.HcDataLengthP12_Module4x3;
                    hcModuleDataLength = MainWindow.HcModuleDataLengthP12_Module4x3;
                    hcCcDataLength = MainWindow.HcCcDataLengthP12_Module4x3;
                    break;
                case MainWindow.ZRD_CH15D:
                case MainWindow.ZRD_BH15D:
                case MainWindow.ZRD_CH15D_S3:
                case MainWindow.ZRD_BH15D_S3:
                    hcDataLength = MainWindow.HcDataLengthP15_Module4x3;
                    hcModuleDataLength = MainWindow.HcModuleDataLengthP15_Module4x3;
                    hcCcDataLength = MainWindow.HcCcDataLengthP15_Module4x3;
                    break;
                default:
                    hcDataLength = MainWindow.HcDataLengthP12_Module4x2;
                    hcModuleDataLength = MainWindow.HcModuleDataLengthP12_Module4x2;
                    hcCcDataLength = MainWindow.HcCcDataLengthP12_Module4x2;
                    break;
            }

            Array.Copy(srcBytes, 0, newBytes, 0, srcBytes.Length);

            // ModuleのLoop
            for (int y0 = 0; y0 < dy; y0 += m_CellDy)
            {
                for (int x0 = 0; x0 < dx; x0 += m_CellDx)
                {
                    byte[] moduleBytes = new byte[hcModuleDataLength];
                    byte[] pxlBytes = new byte[size];
                    byte[] data; // Packingされた補正値 8Byte
                    double[] elements = new double[9];

                    // 1Module分を一時格納
                    Array.Copy(srcBytes, 0x140 + cells * hcModuleDataLength, moduleBytes, 0, hcModuleDataLength);

                    // 画素補正値をコピー
                    Array.Copy(moduleBytes, 0x120, pxlBytes, 0, size);

                    // Module内の画素のLoop
                    int pixel = 0;
                    for (int y = y0; y < y0 + m_CellDy; y++)
                    {
                        for (int x = x0; x < x0 + m_CellDx; x++)
                        {
                            for (int i = 0; i < 9; i++)
                            { elements[i] = m_pFmtCreated[(y * m_UnitDx + x) * 9 + i]; }

                            //CMakeUFData.PackCcDataPat2(elements, out data);
                            CMakeUFData.PackCcDataPat3(elements, out data, allowCvLimit);

                            Array.Copy(data, 0, pxlBytes, pixel * 8, 8);
                            pixel++;
                        }
                    }

                    // 画素補正値を書き戻し
                    Array.Copy(pxlBytes, 0, moduleBytes, 0x120, size);

                    // CRC再計算
                    byte[] ccData = new byte[hcCcDataLength];
                    Array.Copy(moduleBytes, 0x20, ccData, 0, hcCcDataLength);

                    MainWindow.CalcCrc(ccData, out crc);
                    Array.Copy(crc, 0, moduleBytes, 0x10, 4);

                    // HeaderのCRC再計算
                    byte[] headerBytes = new byte[28];
                    Array.Copy(moduleBytes, 0, headerBytes, 0, 28);

                    MainWindow.CalcCrc(headerBytes, out crc);
                    Array.Copy(crc, 0, moduleBytes, 0x1C, 4);

                    // 新しいデータを書き戻し
                    Array.Copy(moduleBytes, 0, newBytes, 0x140 + cells * hcModuleDataLength, hcModuleDataLength);

                    ++cells;
                }
            }

            // 全体のCRC再計算
            byte[] dataBytes = new byte[hcDataLength];
            Array.Copy(newBytes, 0x20, dataBytes, 0, hcDataLength);

            MainWindow.CalcCrc(dataBytes, out crc);
            Array.Copy(crc, 0, newBytes, 0x10, 4);

            // HeaderのCRC再計算
            dataBytes = new byte[28];
            Array.Copy(newBytes, 0, dataBytes, 0, 28);

            MainWindow.CalcCrc(dataBytes, out crc);
            Array.Copy(crc, 0, newBytes, 0x1C, 4);

            File.WriteAllBytes(fileName, newBytes);

            return true;
        }

        public bool OverWritePixelDataWithCrosstalk(string fileName, string ledModel, bool allowCvLimit = false)
        {
            int cells = 0;
            int dx = m_UnitDx;
            int dy = m_UnitDy;
            int size = m_CellDx * m_CellDy * 8; // 1Pixel = 8Byte, P1.2 : 129600[Byte] / P1.5 : 82944[Byte]
            int hcDataLength, hcModuleDataLength, hcCcDataLength;
            byte[] crc;

            byte[] srcBytes = File.ReadAllBytes(m_SourceCellDataFileName);
            byte[] newBytes = new byte[srcBytes.Length];

            switch (ledModel)
            {
                case MainWindow.ZRD_C15A:
                case MainWindow.ZRD_B15A:
                    hcDataLength = MainWindow.HcDataLengthP15_Module4x2;
                    hcModuleDataLength = MainWindow.HcModuleDataLengthP15_Module4x2;
                    hcCcDataLength = MainWindow.HcCcDataLengthP15_Module4x2;
                    break;
                case MainWindow.ZRD_CH12D:
                case MainWindow.ZRD_BH12D:
                case MainWindow.ZRD_CH12D_S3:
                case MainWindow.ZRD_BH12D_S3:
                    hcDataLength = MainWindow.HcDataLengthP12_Module4x3;
                    hcModuleDataLength = MainWindow.HcModuleDataLengthP12_Module4x3;
                    hcCcDataLength = MainWindow.HcCcDataLengthP12_Module4x3;
                    break;
                case MainWindow.ZRD_CH15D:
                case MainWindow.ZRD_BH15D:
                case MainWindow.ZRD_CH15D_S3:
                case MainWindow.ZRD_BH15D_S3:
                    hcDataLength = MainWindow.HcDataLengthP15_Module4x3;
                    hcModuleDataLength = MainWindow.HcModuleDataLengthP15_Module4x3;
                    hcCcDataLength = MainWindow.HcCcDataLengthP15_Module4x3;
                    break;
                default:
                    hcDataLength = MainWindow.HcDataLengthP12_Module4x2;
                    hcModuleDataLength = MainWindow.HcModuleDataLengthP12_Module4x2;
                    hcCcDataLength = MainWindow.HcCcDataLengthP12_Module4x2;
                    break;
            }

            Array.Copy(srcBytes, 0, newBytes, 0, srcBytes.Length);

            // ModuleのLoop
            for (int y0 = 0; y0 < dy; y0 += m_CellDy)
            {
                for (int x0 = 0; x0 < dx; x0 += m_CellDx)
                {
                    byte[] moduleBytes = new byte[hcModuleDataLength];
                    byte[] pxlBytes = new byte[size];
                    byte[] data; // Packingされた補正値 8Byte
                    double[] elements = new double[9];
                    byte[] tempBytes = new byte[4];

                    // 1Module分を一時格納
                    Array.Copy(srcBytes, 0x140 + cells * hcModuleDataLength, moduleBytes, 0, hcModuleDataLength);

                    // 画素補正値をコピー
                    Array.Copy(moduleBytes, 0x120, pxlBytes, 0, size);

                    if (m_IsCtcOverwrite & m_CorUncCrosstalk.ContainsKey(cells + 1))
                    {
                        // Crosstalk Correction Data Valid Indicatorの値を1に設定[00 00 00 1X]
                        Array.Copy(moduleBytes, MainWindow.CommonHeaderLength + MainWindow.HcCcDataCtcDataValidIndicatorOffset, tempBytes, 0, tempBytes.Length);

                        tempBytes[0] = (byte)(tempBytes[0] & 0x0F | 0x01 << 4);
                        Array.Copy(tempBytes, 0, moduleBytes, MainWindow.CommonHeaderLength + MainWindow.HcCcDataCtcDataValidIndicatorOffset, tempBytes.Length);

                        // R/G/B輝度単色クロストーク補正量
                        float ctc = (float)m_CorUncCrosstalk[cells + 1].Red;
                        if (ctc < float.MinValue || ctc > float.MaxValue)
                        { return false; }

                        tempBytes = BitConverter.GetBytes(ctc);
                        Array.Copy(tempBytes, 0, moduleBytes, MainWindow.CommonHeaderLength + MainWindow.HcCcDataCtcHighRedOffset, MainWindow.HcCcDataCtcHighColorSize);

#if DEBUG
                        Console.WriteLine($"Module No.{cells + 1} CTC (H)R: {BitConverter.ToString(tempBytes)}");
#endif

                        ctc = (float)m_CorUncCrosstalk[cells + 1].Green;
                        if (ctc < float.MinValue || ctc > float.MaxValue)
                        { return false; }

                        tempBytes = BitConverter.GetBytes(ctc);
                        Array.Copy(tempBytes, 0, moduleBytes, MainWindow.CommonHeaderLength + MainWindow.HcCcDataCtcHighGreenOffset, MainWindow.HcCcDataCtcHighColorSize);

#if DEBUG
                        Console.WriteLine($"Module No.{cells + 1} CTC (H)G: {BitConverter.ToString(tempBytes)}");
#endif

                        ctc = (float)m_CorUncCrosstalk[cells + 1].Blue;
                        if (ctc < float.MinValue || ctc > float.MaxValue)
                        { return false; }

                        tempBytes = BitConverter.GetBytes(ctc);
                        Array.Copy(tempBytes, 0, moduleBytes, MainWindow.CommonHeaderLength + MainWindow.HcCcDataCtcHighBlueOffset, MainWindow.HcCcDataCtcHighColorSize);

#if DEBUG
                        Console.WriteLine($"Module No.{cells + 1} CTC (H)B: {BitConverter.ToString(tempBytes)}");
#endif

                    }

                    if (m_CorUncCrosstalk.ContainsKey(cells + 1))
                    {
                        // Module内の画素のLoop
                        int pixel = 0;
                        for (int y = y0; y < y0 + m_CellDy; y++)
                        {
                            for (int x = x0; x < x0 + m_CellDx; x++)
                            {
                                for (int i = 0; i < 9; i++)
                                { elements[i] = m_pFmtCreated[(y * m_UnitDx + x) * 9 + i]; }

                                //CMakeUFData.PackCcDataPat2(elements, out data);
                                CMakeUFData.PackCcDataPat3(elements, out data, allowCvLimit);

                                Array.Copy(data, 0, pxlBytes, pixel * 8, 8);
                                pixel++;
                            }
                        }
                    }

                    // 画素補正値を書き戻し
                    Array.Copy(pxlBytes, 0, moduleBytes, 0x120, size);

                    // CRC再計算
                    byte[] ccData = new byte[hcCcDataLength];
                    Array.Copy(moduleBytes, 0x20, ccData, 0, hcCcDataLength);

                    MainWindow.CalcCrc(ccData, out crc);
                    Array.Copy(crc, 0, moduleBytes, 0x10, 4);

                    // HeaderのCRC再計算
                    byte[] headerBytes = new byte[28];
                    Array.Copy(moduleBytes, 0, headerBytes, 0, 28);

                    MainWindow.CalcCrc(headerBytes, out crc);
                    Array.Copy(crc, 0, moduleBytes, 0x1C, 4);

                    // 新しいデータを書き戻し
                    Array.Copy(moduleBytes, 0, newBytes, 0x140 + cells * hcModuleDataLength, hcModuleDataLength);

                    ++cells;
                }
            }

            // 全体のCRC再計算
            byte[] dataBytes = new byte[hcDataLength];
            Array.Copy(newBytes, 0x20, dataBytes, 0, hcDataLength);

            MainWindow.CalcCrc(dataBytes, out crc);
            Array.Copy(crc, 0, newBytes, 0x10, 4);

            // HeaderのCRC再計算
            dataBytes = new byte[28];
            Array.Copy(newBytes, 0, dataBytes, 0, 28);

            MainWindow.CalcCrc(dataBytes, out crc);
            Array.Copy(crc, 0, newBytes, 0x1C, 4);

            File.WriteAllBytes(fileName, newBytes);

            return true;
        }

        /// <summary>
        /// 3×3のマトリックスからcd.bin用の1画素分8Byteのデータに変換する
        /// </summary>
        /// <param name="elements">elements[9] M00～M22までの3×3の要素</param>
        /// <param name="data">8Byte(64Bit)のデータ</param>
        public static void PackCcDataPat2(double[] elements, out byte[] data)
        {
            Int64 ullDat = 0;
            data = new byte[8];

            for (int cnt = 0; cnt < 9; cnt++)
            {
                // 最大, 最小値チェック
                if ((matParamPat2[cnt].Offset > elements[cnt]) || (matParamPat2[cnt].Max < elements[cnt]))
                { throw new Exception(); }

                ullDat |= ((Int64)(((int)(Math.Floor(((elements[cnt] - matParamPat2[cnt].Offset) / matParamPat2[cnt].Step) + 0.5)) & matParamPat2[cnt].BitMask))) << matParamPat2[cnt].Shift; // 0.5 = 四捨五入のため
            }

            data = BitConverter.GetBytes(ullDat);
        }

        /// <summary>
        /// 3×3の補償行列をcd.binの8Byteのデータに変換する
        /// </summary>
        /// <param name="elements">3×3の補償行列</param>
        /// <param name="data">cd.binの1画素分のデータ</param>
        /// <param name="allowCvLimit">要素が最小・最大値を超える場合に抑制するかどうか。抑制を実施すると色目が変わるため推奨しない。</param>
        public static void PackCcDataPat3(double[] elements, out byte[] data, bool allowCvLimit = false)
        {
            Int64 ullDat = 0;
            int over = 0, under = 0; 
            
            data = new byte[8];

            for (int cnt = 0; cnt < 9; cnt++)
            {
                if(allowCvLimit == true)
                {
                    if (matParamPat3[cnt].Offset > elements[cnt])
                    {
                        elements[cnt] = matParamPat3[cnt].Offset + matParamPat3[cnt].Step;
                        under++;
                    }

                    if (matParamPat3[cnt].Max < elements[cnt])
                    {
                        elements[cnt] = matParamPat3[cnt].Max - matParamPat3[cnt].Step;
                        over++;
                    }
                }

                // 最大, 最小値チェック
                if ((matParamPat3[cnt].Offset > elements[cnt]) || (matParamPat3[cnt].Max < elements[cnt]))
                { throw new Exception(); }

                ullDat |= ((Int64)(((int)(Math.Floor(((elements[cnt] - matParamPat3[cnt].Offset) / matParamPat3[cnt].Step) + 0.5)) & matParamPat3[cnt].BitMask))) << matParamPat3[cnt].Shift; // 0.5 = 四捨五入のため
            }

            data = BitConverter.GetBytes(ullDat);
        }

        /// <summary>
        /// cd.binの1画素分8Byteのデータから3×3のマトリックスの要素を抽出する
        /// </summary>
        /// <param name="data">8Byte(64Bit)のデータ</param>
        /// <param name="elements">elements[9] M00～M22までの3×3の要素</param>
        public static void UnpackCcDataPat2(byte[] data, out double[] elements)
        {
            elements = new double[9];
            Int64 ullDat = BitConverter.ToInt64(data, 0);

            for (int cnt = 0; cnt < 9; cnt++)
            {
                int val = (int)((ullDat >> matParamPat2[cnt].Shift) & matParamPat2[cnt].BitMask);
                elements[cnt] = (double)val * matParamPat2[cnt].Step + matParamPat2[cnt].Offset;
            }
        }

        public static void UnpackCcDataPat3(byte[] data, out double[] elements)
        {
            elements = new double[9];
            Int64 ullDat = BitConverter.ToInt64(data, 0);

            for (int cnt = 0; cnt < 9; cnt++)
            {
                int val = (int)((ullDat >> matParamPat3[cnt].Shift) & matParamPat3[cnt].BitMask);
                elements[cnt] = (double)val * matParamPat3[cnt].Step + matParamPat3[cnt].Offset;
            }
        }

        public bool OutputFmtCsv(string file)
        {
            string text = "";
            string line = "";

            for (int index = 0; index < m_pFmtOriginal.Length; index += 9)
            {
                line = "";
                for (int i = index; i < index + 9; i++)
                { line += m_pFmtOriginal[i].ToString() + ", "; }

                text += line + "\r\n";
            }

            using(StreamWriter sw = new StreamWriter(file))
            { sw.Write(text); }

            return true;
        }

#endregion Public Methods

#region Private Methods

        private bool calcColorAverage(double[][][] measureData, out Double[] xyz)
        {
            int count = 0;

            xyz = new Double[9];

            for (int i = 1; i < 5; i++)
            {
                if (measureData[i][0][0] < 0)
                { continue; }

                for (Int32 c = 0; c < 3; ++c)
                {
                    for (Int32 j = 0; j < 3; j++)
                    { xyz[c * 3 + j] += measureData[i][c][j]; }
                }

                //xyz[0] += measureData[i][0][0];
                //xyz[1] += measureData[i][0][1];
                //xyz[2] += measureData[i][0][2];
                //xyz[3] += measureData[i][1][0];
                //xyz[4] += measureData[i][1][1];
                //xyz[5] += measureData[i][1][2];
                //xyz[6] += measureData[i][2][0];
                //xyz[7] += measureData[i][2][1];
                //xyz[8] += measureData[i][3][2];

                count++;
            }

            if (count > 0)
            {
	            for (int i = 0; i < xyz.Length; i++)
	            { xyz[i] /= count; } // 平均を算出            
            }

            return true;
        }
        
        // メモリ上のXYZから、fmtを求める。補正targetのYwが返る。 
        // STATISTICSの場合は、mode=E_FMT。メモリ上のXYZを変更する。補正targetのYwが返る。 
        // 暗点補償の場合は、mode=E_CONCEAL。メモリ上のXYZを変更する。補正targetのYwが返る。 
        public bool CalcFmt(double underCompensate, int upper/* = -1 */, int strength/* = -1 */, int limit/* = -1 */, bool defect2,
                out double targetYw, out double targetYr, out double targetYg, out double targetYb, out int ucr, out int ucg, out int ucb)
        {
            int i, j;
            //CString str;
            double ratioYr, ratioYg, ratioYb; // LEDの実際のmax輝度 / 信号のWを出すときの各色の輝度、ratioYxのmin
            double LedYwr, LedYwg, LedYwb; // LEDの色度と信号Wの色度から計算されるLEDの輝度比。

            double xr = m_Target[0];
            double yr = m_Target[1];
            double xg = m_Target[2];
            double yg = m_Target[3];
            double xb = m_Target[4];
            double yb = m_Target[5];
            double Yw = m_Target[6];
            double xw = m_Target[7];
            double yw = m_Target[8];

            double det_inv;
            double Xr, Yr, Zr, Xg, Yg, Zg, Xb, Yb, Zb;
            double LiS11, LiS12, LiS13, LiS21, LiS22, LiS23, LiS31, LiS32, LiS33; // 補正matrix L^-1・M (double)
            double deltaY;
            double eLWr, eLWg, eLWb;
            int lastSign = -1; // 前回の結果がover補正の場合は-1、under補正の場合は+1
            int dx = m_UnitDx; // panel画素数x
            int dy = m_UnitDy;

            int col;

            targetYw = 0;
            targetYr = 0;
            targetYg = 0;
            targetYb = 0;
            ucr = ucg = ucb = 0;
            if (m_pXYZ == null)
            {
                m_LastErrorMessage = "XYZ情報がありません";
                return false;
            }

            // conceal用
            double[] darkLevel = new double[3];
            ColMtx Sig = new ColMtx(xr, yr, xg, yg, xb, yb, Yw, xw, yw, 1);
            float[] xyz2 = null;

            // DEFECT用
            int[] darkCnt = new int[8]; // 欠陥数counter -,R,G,RG,B,RB,GB,RGB
            for (i = 0; i < 8; i++)
                darkCnt[i] = 0;

            // mode設定
            E_mode mode; // E_CONCEAL, E_DEFECTS2, E_FMT
            if (upper != -1 && strength != -1 && limit != -1)
            {
                // CONCEAL
                mode = E_mode.E_CONCEAL;
                xyz2 = new float[dx * dy * 3 * 3];
                for (i = 0; i < 3 * 3 * dx * dy; i++)
                { xyz2[i] = m_pXYZ[i]; } // 現状XYZのcopy。後で変更部分だけを上書きする。 
            }
            else
            {
                // FMT ←CASはここしか使ってない
                mode = E_mode.E_FMT;
                m_pFmtCreated = new float[dx * dy * 9];
            }

            // added by Hotta 2015/07/28
            if (m_pFmtCreated == null)
            { m_pFmtCreated = new float[dx * dy * 9]; }
            //

            // 信号
            double SigYwr, SigYwg, SigYwb;
            double SigXr, SigZr, SigXg, SigZg, SigXb, SigZb, SigXw, SigZw;
            xyToXZ(xr, yr, out SigXr, out SigZr); // 信号の色空間

            xyToXZ(xg, yg, out SigXg, out SigZg);
            xyToXZ(xb, yb, out SigXb, out SigZb);
            xyToXZ(xw, yw, out SigXw, out SigZw);

            // 信号のwhite比。合計1nitになる値。 
            double s11, s12, s13, s21, s22, s23, s31, s32, s33, SigDet; // 補正matrix (Signal)'-1
            SigDet = (SigXr * SigZb + SigXg * SigZr + SigXb * SigZg - SigXr * SigZg - SigXg * SigZb - SigXb * SigZr); // 信号色空間の逆行列 S^-1作成
            s11 = (SigZb - SigZg) / SigDet;
            s12 = (SigXb * SigZg - SigXg * SigZb) / SigDet;
            s13 = (SigXg - SigXb) / SigDet;
            s21 = (SigZr - SigZb) / SigDet;
            s22 = (SigXr * SigZb - SigXb * SigZr) / SigDet;
            s23 = (SigXb - SigXr) / SigDet;
            s31 = (SigZg - SigZr) / SigDet;
            s32 = (SigXg * SigZr - SigXr * SigZg) / SigDet;
            s33 = (SigXr - SigXg) / SigDet;
            SigYwr = s11 * SigXw + s12 + s13 * SigZw; // white輝度比 
            SigYwg = s21 * SigXw + s22 + s23 * SigZw;
            SigYwb = s31 * SigXw + s32 + s33 * SigZw;

            // 補正後のWhite輝度の設定値
            deltaY = Yw; // targetYの探索変動幅の初期値
            targetYw = 0; // 最初に必ずdeltaYが足されるので0。(cell検)必ず小さ目の値、(unit検)target+deltaYが目標値 

            ////////////////////////////////////////////////////////////////
            // main
            if (underCompensate != -1) underCompensate = underCompensate * dx * dy / 100;

            while (lastSign != 0)
            {
                targetYw += deltaY;
                ucr = ucg = ucb = 0; // 補正残りの数(異常は未count)

                for (int y = 0; y < dy; ++y)
                {
                    for (int x = 0; x < dx; ++x)
                    {
                        i = (dx * y + x) * 9;

                        // m_pXとm_pZは、X,Zでなく、X/Y,Z/Yになっている。
                        Xr = m_pXYZ[i + 0] / m_pXYZ[i + 1];
                        Yr = m_pXYZ[i + 1];
                        Zr = m_pXYZ[i + 2] / m_pXYZ[i + 1];
                        Xg = m_pXYZ[i + 3] / m_pXYZ[i + 4];
                        Yg = m_pXYZ[i + 4];
                        Zg = m_pXYZ[i + 5] / m_pXYZ[i + 4];
                        Xb = m_pXYZ[i + 6] / m_pXYZ[i + 7];
                        Yb = m_pXYZ[i + 7];
                        Zb = m_pXYZ[i + 8] / m_pXYZ[i + 7];

                        if ((Yr != 0 && Yg != 0 && Yb != 0) || mode == E_mode.E_CONCEAL)
                        { // どれかが0だと、異常値になるので、skip。輝度のみ補正なら他の色関係ないので異常値にならない。Concealの場合はskipしない。 
                            // L^-1 * S
                            det_inv = 1 / (Xr * Zb + Xg * Zr + Xb * Zg - Xr * Zg - Xg * Zb - Xb * Zr);
                            LiS11 = ((Zb - Zg) * SigXr + (Xb * Zg - Xg * Zb) + (Xg - Xb) * SigZr) * det_inv;
                            LiS12 = ((Zb - Zg) * SigXg + (Xb * Zg - Xg * Zb) + (Xg - Xb) * SigZg) * det_inv;
                            LiS13 = ((Zb - Zg) * SigXb + (Xb * Zg - Xg * Zb) + (Xg - Xb) * SigZb) * det_inv;
                            LiS21 = ((Zr - Zb) * SigXr + (Xr * Zb - Xb * Zr) + (Xb - Xr) * SigZr) * det_inv;
                            LiS22 = ((Zr - Zb) * SigXg + (Xr * Zb - Xb * Zr) + (Xb - Xr) * SigZg) * det_inv;
                            LiS23 = ((Zr - Zb) * SigXb + (Xr * Zb - Xb * Zr) + (Xb - Xr) * SigZb) * det_inv;
                            LiS31 = ((Zg - Zr) * SigXr + (Xg * Zr - Xr * Zg) + (Xr - Xg) * SigZr) * det_inv;
                            LiS32 = ((Zg - Zr) * SigXg + (Xg * Zr - Xr * Zg) + (Xr - Xg) * SigZg) * det_inv;
                            LiS33 = ((Zg - Zr) * SigXb + (Xg * Zr - Xr * Zg) + (Xr - Xg) * SigZb) * det_inv;

                            // 信号の方のW輝度を1とした時の、信号のWを出すときの各色LEDの輝度 (L^-1 * XYZsig)。(LEDの色度から計算される値)。足して1なんだから必ず1以下。 
                            LedYwr = ((Zb - Zg) * SigXw + (Xb * Zg - Xg * Zb) + (Xg - Xb) * SigZw) * det_inv;
                            LedYwg = ((Zr - Zb) * SigXw + (Xr * Zb - Xb * Zr) + (Xb - Xr) * SigZw) * det_inv;
                            LedYwb = ((Zg - Zr) * SigXw + (Xg * Zr - Xr * Zg) + (Xr - Xg) * SigZw) * det_inv;

                            // (測定された現実のLED輝度) / (目標のLED輝度)
                            ratioYr = Yr / (LedYwr * targetYw);
                            ratioYg = Yg / (LedYwg * targetYw);
                            ratioYb = Yb / (LedYwb * targetYw);

                            if (mode == E_mode.E_CONCEAL)
                            {
                                //// Conceal
                                if (Yr == 0 || Yg == 0 || Yb == 0) ratioYr = ratioYg = ratioYb = 0; // fmtの方はall0にしてあるので、それに対応する。 
                                darkLevel[0] = (ratioYr < upper / 100.0) ? ratioYr : 1;
                                darkLevel[1] = (ratioYg < upper / 100.0) ? ratioYg : 1;
                                darkLevel[2] = (ratioYb < upper / 100.0) ? ratioYb : 1;

                                // dark dotの周辺を輝度up
                                if ((darkLevel[0] != 1 || darkLevel[1] != 1 || darkLevel[2] != 1))
                                { // Conceal && RGBどれか1つがdarkの場合。 
                                    ColMtx XYZ2;
                                    for (/*int*/ col = 0; col < 3; ++col)
                                    {
                                        darkLevel[col] = (1 + (1 - darkLevel[col]) * (strength / 100.0) / 8); // darkDotの輝度% → 周辺各画素の増加%に変換。 
                                        if (darkLevel[col] > limit / 100.0) darkLevel[col] = limit / 100.0;
                                    }

                                    ColMtx Dark = new ColMtx(darkLevel[0], 0.0, 0.0, 0.0, darkLevel[1], 0.0, 0.0, 0.0, darkLevel[2], 33);
                                    ColMtx SSDI = Sig * (Sig * Dark).Inv(); // 一旦、途中計算を受ける。 
                                    for (int oy = -1; oy < 2; ++oy)
                                    {
                                        for (int ox = -1; ox < 2; ++ox)
                                        {
                                            if (ox == 0 && oy == 0) continue;
                                            if (x + ox < 0 || x + ox >= dx) continue;
                                            if (y + oy < 0 || y + oy >= dy) continue;
                                            int i2 = (dx * (y + oy) + (x + ox)) * 9;
                                            ColMtx Led = new ColMtx(m_pXYZ, i2, 33);
                                            XYZ2 = SSDI * Led; // XYZ2 = Sig * (Sig * Dark).Inv() * Led
                                            double[] pXyz = new Double[9];
                                            XYZ2.GetElement(pXyz);
                                            for (j = 0; j < 9; ++j)
                                                xyz2[i2 + j] = (float)pXyz[j];
                                        }
                                    }
                                }
                            }
                            else if (mode == E_mode.E_FMT)
                            {
                                //// Make Fmt
                                // TRIO内バランス調整
                                if (ratioYr < 1) { ratioYr = 1; ++ucr; } // targetY以下のときは、そのままの値を使用することになる。 
                                if (ratioYg < 1) { ratioYg = 1; ++ucg; }
                                if (ratioYb < 1) { ratioYb = 1; ++ucb; }

                                // Matrix作成
                                eLWr = 1 / ratioYr / LedYwr;
                                eLWg = 1 / ratioYg / LedYwg;
                                eLWb = 1 / ratioYb / LedYwb;
                                m_pFmtCreated[i + 0] = (float)(eLWr * LiS11 * SigYwr); // T = e ・ (Lのwhite比)^-1 ・ (L^-1 ・ S) ・ (Sのwhite比)
                                m_pFmtCreated[i + 1] = (float)(eLWr * LiS12 * SigYwg);
                                m_pFmtCreated[i + 2] = (float)(eLWr * LiS13 * SigYwb);
                                m_pFmtCreated[i + 3] = (float)(eLWg * LiS21 * SigYwr);
                                m_pFmtCreated[i + 4] = (float)(eLWg * LiS22 * SigYwg);
                                m_pFmtCreated[i + 5] = (float)(eLWg * LiS23 * SigYwb);
                                m_pFmtCreated[i + 6] = (float)(eLWb * LiS31 * SigYwr);
                                m_pFmtCreated[i + 7] = (float)(eLWb * LiS32 * SigYwg);
                                m_pFmtCreated[i + 8] = (float)(eLWb * LiS33 * SigYwb);
                            }
                        }
                        else
                        {
                            // FMTの場合で、Y=0などで異常値の場合は、matrixを0にする
                            for (j = 0; j < 9; ++j) m_pFmtCreated[i + j] = 0;
                        }
                    }
                }

                if (underCompensate != -1)
                {
                    // 補正残り量補正
                    if (ucr > (int)(underCompensate) || ucg > (int)(underCompensate) || ucb > (int)(underCompensate))
                    {
                        // 今回under補正
                        if (lastSign == -1)
                        {
                            // 前回over補正
                            deltaY = Math.Abs(deltaY) / 2 * -1;
                        }
                        else
                        {
                            // 前回もunder補正
                            ; // 同じdeltaYでもう一回 
                        }
                        lastSign = 1;
                    }
                    else if (ucr < (int)(underCompensate - 1) && ucg < (int)(underCompensate - 1) && ucb < (int)(underCompensate - 1))
                    {
                        // 今回over補正
                        if (lastSign == -1)
                        {
                            // 前回もover補正
                            ; // 同じdeltaYでもう一回 
                        }
                        else
                        {
                            // 前回under補正
                            deltaY = Math.Abs(deltaY) / 2 * +1;
                        }
                        lastSign = -1;
                    }
                    else
                    {
                        // ねらい的中
                        lastSign = 0;
                    }
                }
                else
                {
                    // 指定輝度補正
                    lastSign = 0;
                }
            }

            if (mode == E_mode.E_CONCEAL)
            {
                for (i = 0; i < 3 * 3 * dx * dy; i++)
                    m_pXYZ[i] = xyz2[i]; // 現状XYZの上書き 
            }

            return true;
        }

        private bool CalcFmtWithCrosstalk(out double targetYw, out double targetYr, out double targetYg, out double targetYb)
        {
            int i, j;
            double ratioYr, ratioYg, ratioYb; // LEDの実際のmax輝度 / 信号のWを出すときの各色の輝度、ratioYxのmin
            double LedYwr, LedYwg, LedYwb; // LEDの色度と信号Wの色度から計算されるLEDの輝度比。

            double xr = m_Target[0];
            double yr = m_Target[1];
            double xg = m_Target[2];
            double yg = m_Target[3];
            double xb = m_Target[4];
            double yb = m_Target[5];
            double Yw = m_Target[6];    // ループの中で、クロストーク補正された値に更新される
            double xw = m_Target[7];    // ループの中で、クロストーク補正された値に更新される
            double yw = m_Target[8];    // ループの中で、クロストーク補正された値に更新される

            double det_inv;
            double Xr, Yr, Zr, Xg, Yg, Zg, Xb, Yb, Zb;
            double LiS11, LiS12, LiS13, LiS21, LiS22, LiS23, LiS31, LiS32, LiS33; // 補正matrix L^-1・M (double)
            double eLWr, eLWg, eLWb;
            int dx = m_UnitDx; // panel画素数x
            int dy = m_UnitDy;

            targetYw = 0;
            targetYr = 0;
            targetYg = 0;
            targetYb = 0;

            if (m_pXYZ == null)
            {
                m_LastErrorMessage = "XYZ情報がありません";
                return false;
            }

            m_pFmtCreated = new float[dx * dy * 9];

            // ターゲットの 単色輝度（Yr, Yg, Yb）および その和であるW色度輝度（xw, yw, Yw） を変更する。単色色度（xr, yr, xg, yg, xb, yb） は変更しない。
            double tgtXr, tgtYr, tgtZr, tgtXg, tgtYg, tgtZg, tgtXb, tgtYb, tgtZb;

            // ターゲット (xr,yr), (xg,yg), (xb,yb) および (xw,yw,Yw) を実現する、(Xr,Yr,Zr), (Xg, Yg, Zg), (Xb, Yb, Zb) を算出する
            CalcWhiteXYZ_To_RGBXYZ(xr, yr, xg, yg, xb, yb, xw, yw, Yw, out tgtXr, out tgtYr, out tgtZr, out tgtXg, out tgtYg, out tgtZg, out tgtXb, out tgtYb, out tgtZb);
           
#if DEBUG
            Console.WriteLine($"Target Yr: {tgtYr}\r\nTarget Yg: {tgtYg}\r\nTarget Yb: {tgtYb}");
#endif

            for (int y = 0; y < dy; ++y)
            {
                for (int x = 0; x < dx; ++x)
                {
                    double SigYwr, SigYwg, SigYwb;
                    double SigXr, SigZr, SigXg, SigZg, SigXb, SigZb, SigXw, SigZw;

                    double tgtCorYr = tgtYr;
                    double tgtCorYg = tgtYg;
                    double tgtCorYb = tgtYb;

                    // クロストーク補正
                    // 求めた Yr, Yg, Yb をクロストーク補正
                    int cell = (y / m_CellDy * m_DivideX) + (x / m_CellDx) + 1;
                    if (m_CorUncCrosstalk.ContainsKey(cell))
                    {
                        tgtCorYr = tgtYr * (1.0 + m_CorUncCrosstalk[cell].Red);
                        tgtCorYg = tgtYg * (1.0 + m_CorUncCrosstalk[cell].Green);
                        tgtCorYb = tgtYb * (1.0 + m_CorUncCrosstalk[cell].Blue);
                    }
                    else
                    { continue; }

                    // (xr,yr), (xg,yg), (xb,yb) かつ、クロストーク補正した Yr, Yg, Yb となる、(Xr,Yr,Zr), (Xg,Yg,Zg), (Xb,Yb,Zb) を求める。
                    double tgtCorXr = xr / yr * tgtCorYr;
                    double tgtCorZr = (1.0 - xr - yr) / yr * tgtCorYr;

                    double tgtCorXg = xg / yg * tgtCorYg;
                    double tgtCorZg = (1.0 - xg - yg) / yg * tgtCorYg;

                    double tgtCorXb = xb / yb * tgtCorYb;
                    double tgtCorZb = (1.0 - xb - yb) / yb * tgtCorYb;

                    // これらから、Wを求める
                    double tgtCorXw = tgtCorXr + tgtCorXg + tgtCorXb;
                    double tgtCorYw = tgtCorYr + tgtCorYg + tgtCorYb;
                    double tgtCorZw = tgtCorZr + tgtCorZg + tgtCorZb;

                    xw = tgtCorXw / (tgtCorXw + tgtCorYw + tgtCorZw);   // クロストーク補正した単色輝度で更新
                    yw = tgtCorYw / (tgtCorXw + tgtCorYw + tgtCorZw);   // クロストーク補正した単色輝度で更新
                    Yw = tgtCorYw;                                      // クロストーク補正した単色輝度で更新

                    // 信号
                    xyToXZ(xr, yr, out SigXr, out SigZr); // 信号の色空間
                    xyToXZ(xg, yg, out SigXg, out SigZg);
                    xyToXZ(xb, yb, out SigXb, out SigZb);
                    xyToXZ(xw, yw, out SigXw, out SigZw);

                    // 信号のwhite比。合計1nitになる値。 
                    double s11, s12, s13, s21, s22, s23, s31, s32, s33, SigDet; // 補正matrix (Signal)'-1
                    SigDet = SigXr * SigZb + SigXg * SigZr + SigXb * SigZg - SigXr * SigZg - SigXg * SigZb - SigXb * SigZr; // 信号色空間の逆行列 S^-1作成
                    s11 = (SigZb - SigZg) / SigDet;
                    s12 = (SigXb * SigZg - SigXg * SigZb) / SigDet;
                    s13 = (SigXg - SigXb) / SigDet;
                    s21 = (SigZr - SigZb) / SigDet;
                    s22 = (SigXr * SigZb - SigXb * SigZr) / SigDet;
                    s23 = (SigXb - SigXr) / SigDet;
                    s31 = (SigZg - SigZr) / SigDet;
                    s32 = (SigXg * SigZr - SigXr * SigZg) / SigDet;
                    s33 = (SigXr - SigXg) / SigDet;
                    SigYwr = s11 * SigXw + s12 + s13 * SigZw; // white輝度比 
                    SigYwg = s21 * SigXw + s22 + s23 * SigZw;
                    SigYwb = s31 * SigXw + s32 + s33 * SigZw;

                    // 補正後のWhite輝度の設定値
                    targetYw = Yw;

                    i = (dx * y + x) * 9;

                    // m_pXとm_pZは、X,Zでなく、X/Y,Z/Yになっている。
                    Xr = m_pXYZ[i + 0] / m_pXYZ[i + 1];
                    Yr = m_pXYZ[i + 1];
                    Zr = m_pXYZ[i + 2] / m_pXYZ[i + 1];
                    Xg = m_pXYZ[i + 3] / m_pXYZ[i + 4];
                    Yg = m_pXYZ[i + 4];
                    Zg = m_pXYZ[i + 5] / m_pXYZ[i + 4];
                    Xb = m_pXYZ[i + 6] / m_pXYZ[i + 7];
                    Yb = m_pXYZ[i + 7];
                    Zb = m_pXYZ[i + 8] / m_pXYZ[i + 7];

                    if (Yr != 0 && Yg != 0 && Yb != 0)
                    { // どれかが0だと、異常値になるので、skip。輝度のみ補正なら他の色関係ないので異常値にならない。
                        // L^-1 * S
                        det_inv = 1 / (Xr * Zb + Xg * Zr + Xb * Zg - Xr * Zg - Xg * Zb - Xb * Zr);
                        LiS11 = ((Zb - Zg) * SigXr + (Xb * Zg - Xg * Zb) + (Xg - Xb) * SigZr) * det_inv;
                        LiS12 = ((Zb - Zg) * SigXg + (Xb * Zg - Xg * Zb) + (Xg - Xb) * SigZg) * det_inv;
                        LiS13 = ((Zb - Zg) * SigXb + (Xb * Zg - Xg * Zb) + (Xg - Xb) * SigZb) * det_inv;
                        LiS21 = ((Zr - Zb) * SigXr + (Xr * Zb - Xb * Zr) + (Xb - Xr) * SigZr) * det_inv;
                        LiS22 = ((Zr - Zb) * SigXg + (Xr * Zb - Xb * Zr) + (Xb - Xr) * SigZg) * det_inv;
                        LiS23 = ((Zr - Zb) * SigXb + (Xr * Zb - Xb * Zr) + (Xb - Xr) * SigZb) * det_inv;
                        LiS31 = ((Zg - Zr) * SigXr + (Xg * Zr - Xr * Zg) + (Xr - Xg) * SigZr) * det_inv;
                        LiS32 = ((Zg - Zr) * SigXg + (Xg * Zr - Xr * Zg) + (Xr - Xg) * SigZg) * det_inv;
                        LiS33 = ((Zg - Zr) * SigXb + (Xg * Zr - Xr * Zg) + (Xr - Xg) * SigZb) * det_inv;

                        // 信号の方のW輝度を1とした時の、信号のWを出すときの各色LEDの輝度 (L^-1 * XYZsig)。(LEDの色度から計算される値)。足して1なんだから必ず1以下。 
                        LedYwr = ((Zb - Zg) * SigXw + (Xb * Zg - Xg * Zb) + (Xg - Xb) * SigZw) * det_inv;
                        LedYwg = ((Zr - Zb) * SigXw + (Xr * Zb - Xb * Zr) + (Xb - Xr) * SigZw) * det_inv;
                        LedYwb = ((Zg - Zr) * SigXw + (Xg * Zr - Xr * Zg) + (Xr - Xg) * SigZw) * det_inv;

                        // (測定された現実のLED輝度) / (目標のLED輝度)
                        ratioYr = Yr / (LedYwr * targetYw);
                        ratioYg = Yg / (LedYwg * targetYw);
                        ratioYb = Yb / (LedYwb * targetYw);

                        // Matrix作成
                        eLWr = 1 / ratioYr / LedYwr;
                        eLWg = 1 / ratioYg / LedYwg;
                        eLWb = 1 / ratioYb / LedYwb;

                        m_pFmtCreated[i + 0] = (float)(eLWr * LiS11 * SigYwr); // T = e ・ (Lのwhite比)^-1 ・ (L^-1 ・ S) ・ (Sのwhite比)
                        m_pFmtCreated[i + 1] = (float)(eLWr * LiS12 * SigYwg);
                        m_pFmtCreated[i + 2] = (float)(eLWr * LiS13 * SigYwb);
                        m_pFmtCreated[i + 3] = (float)(eLWg * LiS21 * SigYwr);
                        m_pFmtCreated[i + 4] = (float)(eLWg * LiS22 * SigYwg);
                        m_pFmtCreated[i + 5] = (float)(eLWg * LiS23 * SigYwb);
                        m_pFmtCreated[i + 6] = (float)(eLWb * LiS31 * SigYwr);
                        m_pFmtCreated[i + 7] = (float)(eLWb * LiS32 * SigYwg);
                        m_pFmtCreated[i + 8] = (float)(eLWb * LiS33 * SigYwb);


#if DEBUG
                        if (CalcModuleNo(x, y, out int moduleNo))
                        {
                            Console.WriteLine($"Module No.{moduleNo} MtgtCT: {tgtCorXr}, {tgtCorXg}, {tgtCorXb}, {tgtCorYr}, {tgtCorYg}, {tgtCorYb}, {tgtCorZr}, {tgtCorZg}, {tgtCorZb}");
                            Console.WriteLine($"Module No.{moduleNo} m_pFmtCreated: {m_pFmtCreated[i + 0]}, {m_pFmtCreated[i + 1]}, {m_pFmtCreated[i + 2]}, {m_pFmtCreated[i + 3]}, {m_pFmtCreated[i + 4]}, {m_pFmtCreated[i + 5]}, {m_pFmtCreated[i + 6]}, {m_pFmtCreated[i + 7]}, {m_pFmtCreated[i + 8]}"); // m_pFmtOriginalと並び一緒
                        }
#elif Release_log
                        if (CalcModuleNo(x, y, out int moduleNo))
                        {
                            MainWindow.SaveExecLog($"      Module No.{moduleNo} MtgtCT: {tgtCorXr}, {tgtCorXg}, {tgtCorXb}, {tgtCorYr}, {tgtCorYg}, {tgtCorYb}, {tgtCorZr}, {tgtCorZg}, {tgtCorZb}");
                            MainWindow.SaveExecLog($"      Module No.{moduleNo} m_pFmtCreated: {m_pFmtCreated[i + 0]}, {m_pFmtCreated[i + 1]}, {m_pFmtCreated[i + 2]}, {m_pFmtCreated[i + 3]}, {m_pFmtCreated[i + 4]}, {m_pFmtCreated[i + 5]}, {m_pFmtCreated[i + 6]}, {m_pFmtCreated[i + 7]}, {m_pFmtCreated[i + 8]}");
                        }
#endif

                    }
                    else
                    {
                        // FMTの場合で、Y=0などで異常値の場合は、matrixを0にする
                        for (j = 0; j < 9; ++j) m_pFmtCreated[i + j] = 0;
                    }
                }
            }

            return true;
        }


        // added by Hotta 2024/09/12
#if ForCrosstalkCameraUF
        // カメラU/F用
        // メモリ上のXYZから、fmtを求める。補正targetのYwが返る。 
        // STATISTICSの場合は、mode=E_FMT。メモリ上のXYZを変更する。補正targetのYwが返る。 
        // 暗点補償の場合は、mode=E_CONCEAL。メモリ上のXYZを変更する。補正targetのYwが返る。 
        private bool CalcFmt_CameraUF(UnitInfo unit, out double targetYw, out double targetYr, out double targetYg, out double targetYb)
        {
            int i, j;
            double ratioYr, ratioYg, ratioYb; // LEDの実際のmax輝度 / 信号のWを出すときの各色の輝度、ratioYxのmin
            double LedYwr, LedYwg, LedYwb; // LEDの色度と信号Wの色度から計算されるLEDの輝度比。
            double xr, yr, xg, yg, xb, yb, Yw, xw, yw;

            double det_inv;
            double Xr, Yr, Zr, Xg, Yg, Zg, Xb, Yb, Zb;
            double LiS11, LiS12, LiS13, LiS21, LiS22, LiS23, LiS31, LiS32, LiS33; // 補正matrix L^-1・M (double)
            double eLWr, eLWg, eLWb;
            int dx = m_UnitDx; // panel画素数x
            int dy = m_UnitDy;

            targetYw = 0;
            targetYr = 0;
            targetYg = 0;
            targetYb = 0;
            if (m_pXYZ == null)
            {
                m_LastErrorMessage = "XYZ情報がありません";
                return false;
            }

            m_pFmtCreated = new float[dx * dy * 9];

            // added by Hotta 2015/07/28
            if (m_pFmtCreated == null)
            { m_pFmtCreated = new float[dx * dy * 9]; }
            //

            // 信号
            double SigYwr, SigYwg, SigYwb;
            double SigXr, SigZr, SigXg, SigZg, SigXb, SigZb, SigXw, SigZw;

            //// 信号のwhite比。合計1nitになる値。 
            double s11, s12, s13, s21, s22, s23, s31, s32, s33, SigDet; // 補正matrix (Signal)'-1

            for (int y = 0; y < dy; ++y)
            {
                for (int x = 0; x < dx; ++x)
                {
                    xr = m_CorTarget[x, y][0];
                    yr = m_CorTarget[x, y][1];
                    xg = m_CorTarget[x, y][2];
                    yg = m_CorTarget[x, y][3];
                    xb = m_CorTarget[x, y][4];
                    yb = m_CorTarget[x, y][5];
                    Yw = m_CorTarget[x, y][6];
                    xw = m_CorTarget[x, y][7];
                    yw = m_CorTarget[x, y][8];

                    xyToXZ(xr, yr, out SigXr, out SigZr); // 信号の色空間
                    xyToXZ(xg, yg, out SigXg, out SigZg);
                    xyToXZ(xb, yb, out SigXb, out SigZb);
                    xyToXZ(xw, yw, out SigXw, out SigZw);

                    // 信号のwhite比。合計1nitになる値。 
                    //double s11, s12, s13, s21, s22, s23, s31, s32, s33, SigDet; // 補正matrix (Signal)'-1
                    SigDet = (SigXr * SigZb + SigXg * SigZr + SigXb * SigZg - SigXr * SigZg - SigXg * SigZb - SigXb * SigZr); // 信号色空間の逆行列 S^-1作成
                    s11 = (SigZb - SigZg) / SigDet;
                    s12 = (SigXb * SigZg - SigXg * SigZb) / SigDet;
                    s13 = (SigXg - SigXb) / SigDet;
                    s21 = (SigZr - SigZb) / SigDet;
                    s22 = (SigXr * SigZb - SigXb * SigZr) / SigDet;
                    s23 = (SigXb - SigXr) / SigDet;
                    s31 = (SigZg - SigZr) / SigDet;
                    s32 = (SigXg * SigZr - SigXr * SigZg) / SigDet;
                    s33 = (SigXr - SigXg) / SigDet;
                    SigYwr = s11 * SigXw + s12 + s13 * SigZw; // white輝度比 
                    SigYwg = s21 * SigXw + s22 + s23 * SigZw;
                    SigYwb = s31 * SigXw + s32 + s33 * SigZw;

                    targetYw = Yw;

                    i = (dx * y + x) * 9;

                    // m_pXとm_pZは、X,Zでなく、X/Y,Z/Yになっている。
                    Xr = m_pXYZ[i + 0] / m_pXYZ[i + 1];
                    Yr = m_pXYZ[i + 1];
                    Zr = m_pXYZ[i + 2] / m_pXYZ[i + 1];
                    Xg = m_pXYZ[i + 3] / m_pXYZ[i + 4];
                    Yg = m_pXYZ[i + 4];
                    Zg = m_pXYZ[i + 5] / m_pXYZ[i + 4];
                    Xb = m_pXYZ[i + 6] / m_pXYZ[i + 7];
                    Yb = m_pXYZ[i + 7];
                    Zb = m_pXYZ[i + 8] / m_pXYZ[i + 7];

                    if (Yr != 0 && Yg != 0 && Yb != 0)
                    { // どれかが0だと、異常値になるので、skip。輝度のみ補正なら他の色関係ないので異常値にならない。Concealの場合はskipしない。 
                        // L^-1 * S
                        det_inv = 1 / (Xr * Zb + Xg * Zr + Xb * Zg - Xr * Zg - Xg * Zb - Xb * Zr);
                        LiS11 = ((Zb - Zg) * SigXr + (Xb * Zg - Xg * Zb) + (Xg - Xb) * SigZr) * det_inv;
                        LiS12 = ((Zb - Zg) * SigXg + (Xb * Zg - Xg * Zb) + (Xg - Xb) * SigZg) * det_inv;
                        LiS13 = ((Zb - Zg) * SigXb + (Xb * Zg - Xg * Zb) + (Xg - Xb) * SigZb) * det_inv;
                        LiS21 = ((Zr - Zb) * SigXr + (Xr * Zb - Xb * Zr) + (Xb - Xr) * SigZr) * det_inv;
                        LiS22 = ((Zr - Zb) * SigXg + (Xr * Zb - Xb * Zr) + (Xb - Xr) * SigZg) * det_inv;
                        LiS23 = ((Zr - Zb) * SigXb + (Xr * Zb - Xb * Zr) + (Xb - Xr) * SigZb) * det_inv;
                        LiS31 = ((Zg - Zr) * SigXr + (Xg * Zr - Xr * Zg) + (Xr - Xg) * SigZr) * det_inv;
                        LiS32 = ((Zg - Zr) * SigXg + (Xg * Zr - Xr * Zg) + (Xr - Xg) * SigZg) * det_inv;
                        LiS33 = ((Zg - Zr) * SigXb + (Xg * Zr - Xr * Zg) + (Xr - Xg) * SigZb) * det_inv;

                        // 信号の方のW輝度を1とした時の、信号のWを出すときの各色LEDの輝度 (L^-1 * XYZsig)。(LEDの色度から計算される値)。足して1なんだから必ず1以下。 
                        LedYwr = ((Zb - Zg) * SigXw + (Xb * Zg - Xg * Zb) + (Xg - Xb) * SigZw) * det_inv;
                        LedYwg = ((Zr - Zb) * SigXw + (Xr * Zb - Xb * Zr) + (Xb - Xr) * SigZw) * det_inv;
                        LedYwb = ((Zg - Zr) * SigXw + (Xg * Zr - Xr * Zg) + (Xr - Xg) * SigZw) * det_inv;

                        // (測定された現実のLED輝度) / (目標のLED輝度)
                        ratioYr = Yr / (LedYwr * targetYw);
                        ratioYg = Yg / (LedYwg * targetYw);
                        ratioYb = Yb / (LedYwb * targetYw);

                        //// Make Fmt
                        // TRIO内バランス調整
                        // 製造調整と同じく、あえてリミットかけない
                        //if (ratioYr < 1) { ratioYr = 1; } // targetY以下のときは、そのままの値を使用することになる。 
                        //if (ratioYg < 1) { ratioYg = 1; }
                        //if (ratioYb < 1) { ratioYb = 1; }

                        // Matrix作成
                        eLWr = 1 / ratioYr / LedYwr;
                        eLWg = 1 / ratioYg / LedYwg;
                        eLWb = 1 / ratioYb / LedYwb;
                        m_pFmtCreated[i + 0] = (float)(eLWr * LiS11 * SigYwr); // T = e ・ (Lのwhite比)^-1 ・ (L^-1 ・ S) ・ (Sのwhite比)
                        m_pFmtCreated[i + 1] = (float)(eLWr * LiS12 * SigYwg);
                        m_pFmtCreated[i + 2] = (float)(eLWr * LiS13 * SigYwb);
                        m_pFmtCreated[i + 3] = (float)(eLWg * LiS21 * SigYwr);
                        m_pFmtCreated[i + 4] = (float)(eLWg * LiS22 * SigYwg);
                        m_pFmtCreated[i + 5] = (float)(eLWg * LiS23 * SigYwb);
                        m_pFmtCreated[i + 6] = (float)(eLWb * LiS31 * SigYwr);
                        m_pFmtCreated[i + 7] = (float)(eLWb * LiS32 * SigYwg);
                        m_pFmtCreated[i + 8] = (float)(eLWb * LiS33 * SigYwb);

                        /*
                        // Cabinetセンター 
                        if(unit.X == 1 && unit.Y == 1)
                        {
                            if (m_CellDx * 1 <= x && x < m_CellDx * 2 && m_CellDy * 1 <= y && y < m_CellDy * 2)
                            {
                                m_pFmtCreated[i + 0] = m_pFmtOriginal[i + 0] * 1.1f;
                                m_pFmtCreated[i + 3] = m_pFmtOriginal[i + 3] * 1.1f;
                                m_pFmtCreated[i + 6] = m_pFmtOriginal[i + 6] * 1.1f;
                                m_pFmtCreated[i + 1] = m_pFmtOriginal[i + 1] * 1.1f;
                                m_pFmtCreated[i + 4] = m_pFmtOriginal[i + 4] * 1.1f;
                                m_pFmtCreated[i + 7] = m_pFmtOriginal[i + 7] * 1.1f;
                                m_pFmtCreated[i + 2] = m_pFmtOriginal[i + 2] * 1.1f;
                                m_pFmtCreated[i + 5] = m_pFmtOriginal[i + 5] * 1.1f;
                                m_pFmtCreated[i + 8] = m_pFmtOriginal[i + 8] * 1.1f;
                            }
                            if (m_CellDx * 2 <= x && x < m_CellDx * 3 && m_CellDy * 1 <= y && y < m_CellDy * 2)
                            {
                                m_pFmtCreated[i + 0] = m_pFmtOriginal[i + 0] * 0.9f;
                                m_pFmtCreated[i + 3] = m_pFmtOriginal[i + 3] * 0.9f;
                                m_pFmtCreated[i + 6] = m_pFmtOriginal[i + 6] * 0.9f;
                                m_pFmtCreated[i + 1] = m_pFmtOriginal[i + 1] * 0.9f;
                                m_pFmtCreated[i + 4] = m_pFmtOriginal[i + 4] * 0.9f;
                                m_pFmtCreated[i + 7] = m_pFmtOriginal[i + 7] * 0.9f;
                                m_pFmtCreated[i + 2] = m_pFmtOriginal[i + 2] * 0.9f;
                                m_pFmtCreated[i + 5] = m_pFmtOriginal[i + 5] * 0.9f;
                                m_pFmtCreated[i + 8] = m_pFmtOriginal[i + 8] * 0.9f;
                            }
                        }
                        else if(unit.X == 2 && unit.Y == 1)
                        {
                            if (m_CellDx * 1 <= x && x < m_CellDx * 2 && m_CellDy * 1 <= y && y < m_CellDy * 2)
                            {
                                m_pFmtCreated[i + 0] = m_pFmtOriginal[i + 0] * 1.1f;
                                m_pFmtCreated[i + 3] = m_pFmtOriginal[i + 3] * 1.1f;
                                m_pFmtCreated[i + 6] = m_pFmtOriginal[i + 6] * 1.1f;
                                m_pFmtCreated[i + 1] = m_pFmtOriginal[i + 1];
                                m_pFmtCreated[i + 4] = m_pFmtOriginal[i + 4];
                                m_pFmtCreated[i + 7] = m_pFmtOriginal[i + 7];
                                m_pFmtCreated[i + 2] = m_pFmtOriginal[i + 2];
                                m_pFmtCreated[i + 5] = m_pFmtOriginal[i + 5];
                                m_pFmtCreated[i + 8] = m_pFmtOriginal[i + 8];
                            }
                            if (m_CellDx * 2 <= x && x < m_CellDx * 3 && m_CellDy * 1 <= y && y < m_CellDy * 2)
                            {
                                m_pFmtCreated[i + 0] = m_pFmtOriginal[i + 0] * 0.9f;
                                m_pFmtCreated[i + 3] = m_pFmtOriginal[i + 3] * 0.9f;
                                m_pFmtCreated[i + 6] = m_pFmtOriginal[i + 6] * 0.9f;
                                m_pFmtCreated[i + 1] = m_pFmtOriginal[i + 1];
                                m_pFmtCreated[i + 4] = m_pFmtOriginal[i + 4];
                                m_pFmtCreated[i + 7] = m_pFmtOriginal[i + 7];
                                m_pFmtCreated[i + 2] = m_pFmtOriginal[i + 2];
                                m_pFmtCreated[i + 5] = m_pFmtOriginal[i + 5];
                                m_pFmtCreated[i + 8] = m_pFmtOriginal[i + 8];
                            }
                        }
                        else if (unit.X == 1 && unit.Y == 2)
                        {
                            if (m_CellDx * 1 <= x && x < m_CellDx * 2 && m_CellDy * 1 <= y && y < m_CellDy * 2)
                            {
                                m_pFmtCreated[i + 0] = m_pFmtOriginal[i + 0];
                                m_pFmtCreated[i + 3] = m_pFmtOriginal[i + 3];
                                m_pFmtCreated[i + 6] = m_pFmtOriginal[i + 6];
                                m_pFmtCreated[i + 1] = m_pFmtOriginal[i + 1] * 1.1f;
                                m_pFmtCreated[i + 4] = m_pFmtOriginal[i + 4] * 1.1f;
                                m_pFmtCreated[i + 7] = m_pFmtOriginal[i + 7] * 1.1f;
                                m_pFmtCreated[i + 2] = m_pFmtOriginal[i + 2];
                                m_pFmtCreated[i + 5] = m_pFmtOriginal[i + 5];
                                m_pFmtCreated[i + 8] = m_pFmtOriginal[i + 8];
                            }
                            if (m_CellDx * 2 <= x && x < m_CellDx * 3 && m_CellDy * 1 <= y && y < m_CellDy * 2)
                            {
                                m_pFmtCreated[i + 0] = m_pFmtOriginal[i + 0];
                                m_pFmtCreated[i + 3] = m_pFmtOriginal[i + 3];
                                m_pFmtCreated[i + 6] = m_pFmtOriginal[i + 6];
                                m_pFmtCreated[i + 1] = m_pFmtOriginal[i + 1] * 0.9f;
                                m_pFmtCreated[i + 4] = m_pFmtOriginal[i + 4] * 0.9f;
                                m_pFmtCreated[i + 7] = m_pFmtOriginal[i + 7] * 0.9f;
                                m_pFmtCreated[i + 2] = m_pFmtOriginal[i + 2];
                                m_pFmtCreated[i + 5] = m_pFmtOriginal[i + 5];
                                m_pFmtCreated[i + 8] = m_pFmtOriginal[i + 8];
                            }
                        }
                        else if (unit.X == 2 && unit.Y == 2)
                        {
                            if (m_CellDx * 1 <= x && x < m_CellDx * 2 && m_CellDy * 1 <= y && y < m_CellDy * 2)
                            {
                                m_pFmtCreated[i + 0] = m_pFmtOriginal[i + 0];
                                m_pFmtCreated[i + 3] = m_pFmtOriginal[i + 3];
                                m_pFmtCreated[i + 6] = m_pFmtOriginal[i + 6];
                                m_pFmtCreated[i + 1] = m_pFmtOriginal[i + 1];
                                m_pFmtCreated[i + 4] = m_pFmtOriginal[i + 4];
                                m_pFmtCreated[i + 7] = m_pFmtOriginal[i + 7];
                                m_pFmtCreated[i + 2] = m_pFmtOriginal[i + 2] * 1.1f;
                                m_pFmtCreated[i + 5] = m_pFmtOriginal[i + 5] * 1.1f;
                                m_pFmtCreated[i + 8] = m_pFmtOriginal[i + 8] * 1.1f;
                            }
                            if (m_CellDx * 2 <= x && x < m_CellDx * 3 && m_CellDy * 1 <= y && y < m_CellDy * 2)
                            {
                                m_pFmtCreated[i + 0] = m_pFmtOriginal[i + 0];
                                m_pFmtCreated[i + 3] = m_pFmtOriginal[i + 3];
                                m_pFmtCreated[i + 6] = m_pFmtOriginal[i + 6];
                                m_pFmtCreated[i + 1] = m_pFmtOriginal[i + 1];
                                m_pFmtCreated[i + 4] = m_pFmtOriginal[i + 4];
                                m_pFmtCreated[i + 7] = m_pFmtOriginal[i + 7];
                                m_pFmtCreated[i + 2] = m_pFmtOriginal[i + 2];
                                m_pFmtCreated[i + 2] = m_pFmtOriginal[i + 2] * 0.9f;
                                m_pFmtCreated[i + 5] = m_pFmtOriginal[i + 5] * 0.9f;
                                m_pFmtCreated[i + 8] = m_pFmtOriginal[i + 8] * 0.9f;
                            }
                        }
                        */

                        /*
                        // Cabinet内Hランプ
                        if (unit.X == 2 && unit.Y == 1)
                        {
                            m_pFmtCreated[i + 0] = m_pFmtOriginal[i + 0] * (1.0f -0.2f / dx * x + 0.1f);
                            m_pFmtCreated[i + 3] = m_pFmtOriginal[i + 3] * (1.0f - 0.2f / dx * x + 0.1f);
                            m_pFmtCreated[i + 6] = m_pFmtOriginal[i + 6] * (1.0f - 0.2f / dx * x + 0.1f);
                            m_pFmtCreated[i + 1] = m_pFmtOriginal[i + 1];
                            m_pFmtCreated[i + 4] = m_pFmtOriginal[i + 4];
                            m_pFmtCreated[i + 7] = m_pFmtOriginal[i + 7];
                            m_pFmtCreated[i + 2] = m_pFmtOriginal[i + 2];
                            m_pFmtCreated[i + 5] = m_pFmtOriginal[i + 5];
                            m_pFmtCreated[i + 8] = m_pFmtOriginal[i + 8];
                        }
                        else if(unit.X == 1 && unit.Y == 2)
                        {
                            m_pFmtCreated[i + 0] = m_pFmtOriginal[i + 0];
                            m_pFmtCreated[i + 3] = m_pFmtOriginal[i + 3];
                            m_pFmtCreated[i + 6] = m_pFmtOriginal[i + 6];
                            m_pFmtCreated[i + 1] = m_pFmtOriginal[i + 1] * (1.0f - 0.2f / dx * x + 0.1f);
                            m_pFmtCreated[i + 4] = m_pFmtOriginal[i + 4] * (1.0f - 0.2f / dx * x + 0.1f);
                            m_pFmtCreated[i + 7] = m_pFmtOriginal[i + 7] * (1.0f - 0.2f / dx * x + 0.1f);
                            m_pFmtCreated[i + 2] = m_pFmtOriginal[i + 2];
                            m_pFmtCreated[i + 5] = m_pFmtOriginal[i + 5];
                            m_pFmtCreated[i + 8] = m_pFmtOriginal[i + 8];
                        }
                        else if (unit.X == 2 && unit.Y == 2)
                        {
                            m_pFmtCreated[i + 0] = m_pFmtOriginal[i + 0];
                            m_pFmtCreated[i + 3] = m_pFmtOriginal[i + 3];
                            m_pFmtCreated[i + 6] = m_pFmtOriginal[i + 6];
                            m_pFmtCreated[i + 1] = m_pFmtOriginal[i + 1];
                            m_pFmtCreated[i + 4] = m_pFmtOriginal[i + 4];
                            m_pFmtCreated[i + 7] = m_pFmtOriginal[i + 7];
                            m_pFmtCreated[i + 2] = m_pFmtOriginal[i + 2] * (1.0f - 0.2f / dx * x + 0.1f);
                            m_pFmtCreated[i + 5] = m_pFmtOriginal[i + 5] * (1.0f - 0.2f / dx * x + 0.1f);
                            m_pFmtCreated[i + 8] = m_pFmtOriginal[i + 8] * (1.0f - 0.2f / dx * x + 0.1f);
                        }
                        */

                        /*
                        // Cabinet内Vランプ
                        if (unit.X == 1 && unit.Y == 1)
                        {
                            m_pFmtCreated[i + 0] = m_pFmtOriginal[i + 0] * (1.0f - 0.2f / dy * y + 0.1f);
                            m_pFmtCreated[i + 3] = m_pFmtOriginal[i + 3] * (1.0f - 0.2f / dy * y + 0.1f);
                            m_pFmtCreated[i + 6] = m_pFmtOriginal[i + 6] * (1.0f - 0.2f / dy * y + 0.1f);
                            m_pFmtCreated[i + 1] = m_pFmtOriginal[i + 1];
                            m_pFmtCreated[i + 4] = m_pFmtOriginal[i + 4];
                            m_pFmtCreated[i + 7] = m_pFmtOriginal[i + 7];
                            m_pFmtCreated[i + 2] = m_pFmtOriginal[i + 2];
                            m_pFmtCreated[i + 5] = m_pFmtOriginal[i + 5];
                            m_pFmtCreated[i + 8] = m_pFmtOriginal[i + 8];
                        }
                        else if (unit.X == 2 && unit.Y == 1)
                        {
                            m_pFmtCreated[i + 0] = m_pFmtOriginal[i + 0];
                            m_pFmtCreated[i + 3] = m_pFmtOriginal[i + 3];
                            m_pFmtCreated[i + 6] = m_pFmtOriginal[i + 6];
                            m_pFmtCreated[i + 1] = m_pFmtOriginal[i + 1] * (1.0f - 0.2f / dy * y + 0.1f);
                            m_pFmtCreated[i + 4] = m_pFmtOriginal[i + 4] * (1.0f - 0.2f / dy * y + 0.1f);
                            m_pFmtCreated[i + 7] = m_pFmtOriginal[i + 7] * (1.0f - 0.2f / dy * y + 0.1f);
                            m_pFmtCreated[i + 2] = m_pFmtOriginal[i + 2];
                            m_pFmtCreated[i + 5] = m_pFmtOriginal[i + 5];
                            m_pFmtCreated[i + 8] = m_pFmtOriginal[i + 8];
                        }
                        else if (unit.X == 2 && unit.Y == 2)
                        {
                            m_pFmtCreated[i + 0] = m_pFmtOriginal[i + 0];
                            m_pFmtCreated[i + 3] = m_pFmtOriginal[i + 3];
                            m_pFmtCreated[i + 6] = m_pFmtOriginal[i + 6];
                            m_pFmtCreated[i + 1] = m_pFmtOriginal[i + 1];
                            m_pFmtCreated[i + 4] = m_pFmtOriginal[i + 4];
                            m_pFmtCreated[i + 7] = m_pFmtOriginal[i + 7];
                            m_pFmtCreated[i + 2] = m_pFmtOriginal[i + 2] * (1.0f - 0.2f / dy * y + 0.1f);
                            m_pFmtCreated[i + 5] = m_pFmtOriginal[i + 5] * (1.0f - 0.2f / dy * y + 0.1f);
                            m_pFmtCreated[i + 8] = m_pFmtOriginal[i + 8] * (1.0f - 0.2f / dy * y + 0.1f);
                        }
                        */

                        /*
                        // 評価用に、U/F調整データを固定的に変更
                        if(unit.X == 1)
                        {
                            for(int n=0;n<9;n++)
                                m_pFmtCreated[i + n] = (float)(m_pFmtOriginal[i + n] * 0.95);
                        }
                        else if (unit.X == 2)
                        {
                            for (int n = 0; n < 9; n++)
                                m_pFmtCreated[i + n] = (float)(m_pFmtOriginal[i + n] * 0.975);
                        }
                        else if (unit.X == 3)
                        {
                            for (int n = 0; n < 9; n++)
                                m_pFmtCreated[i + n] = (float)(m_pFmtOriginal[i + n] * 1);
                        }
                        else if (unit.X == 4)
                        {
                            for (int n = 0; n < 9; n++)
                                m_pFmtCreated[i + n] = (float)(m_pFmtOriginal[i + n] * 1.025);
                        }
                        else if (unit.X == 5)
                        {
                            for (int n = 0; n < 9; n++)
                                m_pFmtCreated[i + n] = (float)(m_pFmtOriginal[i + n] * 1.05);
                        }
                        */
                    }
                    else
                    {
                        // FMTの場合で、Y=0などで異常値の場合は、matrixを0にする
                        for (j = 0; j < 9; ++j) m_pFmtCreated[i + j] = 0;
                    }
                }
            }
            // added by Hotta 2024/10/02 for TEST
            /*
#if ForCrosstalkCameraUF
            storeFmt(unit.X - 1, unit.Y - 1);
#endif
            */
            //adjustFmtValue();

            return true;
        }

        /*
        private void adjustFmtValue()
        {
            float[] fmtPacked = new float[m_UnitDx * m_UnitDy * 9];
            double[] corK = new double[9];

            int i, j;
            int dx = m_UnitDx;
            int dy = m_UnitDy;

            double[] avgFmtCreated0;
            double[] avgFmtCreated;
            double[] avgFmtOriginal;
            double[] refRatio;
            double[] curRatio;


            for (int y0 = 0; y0 < dy; y0 += m_CellDy)
            {
                for (int x0 = 0; x0 < dx; x0 += m_CellDx)
                {
                    if (m_CellDy * 1 <= y0 && y0 < m_CellDy * 2 && m_CellDx * 1 <= x0 && x0 < m_CellDy * 3)
                    {
                        avgFmtOriginal = new double[9];
                        avgFmtCreated0 = new double[9];
                        for (int y = y0; y < y0 + m_CellDy; ++y)
                        {
                            for (int x = x0; x < x0 + m_CellDx; ++x)
                            {
                                i = (dx * y + x) * 9;
                                for (j = 0; j < 9; j++)
                                {
                                    avgFmtOriginal[j] += m_pFmtOriginal[i + j];
                                    avgFmtCreated0[j] += m_pFmtCreated[i + j];
                                }
                            }
                        }
                        for (j = 0; j < 9; j++)
                        {
                            avgFmtOriginal[j] /= m_CellDx * m_CellDy;
                            avgFmtCreated0[j] /= m_CellDx * m_CellDy;
                        }
                        using(StreamWriter sw = new StreamWriter("c:\\cas\\temp\\cor.csv", true))
                        {
                            string str = "";
                            for (j = 0; j < 9; j++)
                                str += avgFmtOriginal[j].ToString() + ",";
                            str += ",";
                            for (j = 0; j < 9; j++)
                                str += avgFmtCreated0[j].ToString() + ",";
                            sw.WriteLine(str);
                        }
                    }

                }
            }
            return;









                    for (int y0 = 0; y0 < dy; y0 += m_CellDy)
            {
                for (int x0 = 0; x0 < dx; x0 += m_CellDx)
                {

                    avgFmtOriginal = new double[9];
                    avgFmtCreated0 = new double[9];
                    for (int y = y0; y < y0 + m_CellDy; ++y)
                    {
                        for (int x = x0; x < x0 + m_CellDx; ++x)
                        {
                            i = (dx * y + x) * 9;
                            for (j = 0; j < 9; j++)
                            {
                                avgFmtOriginal[j] += m_pFmtOriginal[i + j];
                                avgFmtCreated0[j] += m_pFmtCreated[i + j];
                            }
                        }
                    }
                    for (j = 0; j < 9; j++)
                    {
                        avgFmtOriginal[j] /= m_CellDx * m_CellDy;
                        avgFmtCreated0[j] /= m_CellDx * m_CellDy;
                    }




                    byte[] pack_data; // Packingされた補正値 8Byte
                    double[] elements = new double[9];
                    int count = 0;

                    while (true)
                    {
                        bool exit_flag = true;
                        avgFmtCreated = new double[9];
                        for (int y = y0; y < y0 + m_CellDy; ++y)
                        {
                            for (int x = x0; x < x0 + m_CellDx; ++x)
                            {
                                i = (dx * y + x) * 9;
                                for (j = 0; j < 9; j++)
                                {
                                    elements[j] = m_pFmtCreated[(y * m_UnitDx + x) * 9 + j];
                                }

                                CMakeUFData.PackCcDataPat3(elements, out pack_data, true);
                                CMakeUFData.UnpackCcDataPat3(pack_data, out elements);

                                for (j = 0; j < 9; j++)
                                {
                                    fmtPacked[(y * m_UnitDx + x) * 9 + j] = (float)elements[j];
                                    avgFmtCreated[j] += fmtPacked[(y * m_UnitDx + x) * 9 + j];
                                }
                            }
                        }
                        for (j = 0; j < 9; j++)
                        {
                            avgFmtCreated[j] /= m_CellDx * m_CellDy;
                        }

                        for (j = 0; j < 9; j++)
                        {
                            if (j == 0 || j == 4 || j == 8)
                                continue;

                            if (Math.Abs(avgFmtCreated[j]) < 0.0001)
                                continue;

                            if (Math.Abs((avgFmtCreated[j] - avgFmtOriginal[j]) / avgFmtOriginal[j] - (avgFmtCreated[j % 3] - avgFmtOriginal[j % 3]) / avgFmtOriginal[j % 3]) > 0.005)
                            {
                                exit_flag = false;

                                if ((avgFmtCreated[j] - avgFmtOriginal[j]) / avgFmtOriginal[j] - (avgFmtCreated[j % 3] - avgFmtOriginal[j % 3]) / avgFmtOriginal[j % 3] > 0)
                                {
                                    for (int y = y0; y < y0 + m_CellDy; ++y)
                                    {
                                        for (int x = x0; x < x0 + m_CellDx; ++x)
                                        {
                                            if((y * m_UnitDx + x) % 100 == count)
                                                m_pFmtCreated[(y * m_UnitDx + x) * 9 + j] -= 0.001f;
                                        }
                                    }
                                }
                                else
                                {
                                    for (int y = y0; y < y0 + m_CellDy; ++y)
                                    {
                                        for (int x = x0; x < x0 + m_CellDx; ++x)
                                        {
                                            if ((y * m_UnitDx + x) % 100 == count)
                                                m_pFmtCreated[(y * m_UnitDx + x) * 9 + j] += 0.001f;
                                        }
                                    }
                                }
                            }
                        }
                        if (exit_flag == true)
                            break;

                        count++;
                        if (count >= 100)
                            count = 0;
                    }
                }
            }
        }
        */
#endif


        private void calcCamGain(UfCamAdjustType type, PixelValue[] rgbCpGain, int x, int y, out double gainR, out double gainG, out double gainB)
        {
            gainR = 1;
            gainG = 1;
            gainB = 1;

            if (type == UfCamAdjustType.Cabinet || type == UfCamAdjustType.Cabi_9pt)
            {
#region Red

                // x方向の近似式2つを求める y1 = ax1 + b, y2 = cx2 + d
                Coordinate start1, end1, start2, end2;
                double a, b, c, d;

                if (x >= m_UnitDx / 2 && y < m_UnitDy / 2) // 第1象限
                {
                    start1 = new Coordinate(m_UnitDx / 2, rgbCpGain[1].R);
                    end1 = new Coordinate(m_UnitDx, rgbCpGain[2].R);
                    start2 = new Coordinate(m_UnitDx / 2, rgbCpGain[4].R);
                    end2 = new Coordinate(m_UnitDx, rgbCpGain[5].R);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(0, a * x + b); // x, yを入れ替えて計算する
                    end1 = new Coordinate(m_UnitDy / 2, c * x + d);
                }
                else if (x < m_UnitDx / 2 && y < m_UnitDy / 2) // 第2象限
                {
                    start1 = new Coordinate(0, rgbCpGain[0].R);
                    end1 = new Coordinate(m_UnitDx / 2, rgbCpGain[1].R);
                    start2 = new Coordinate(0, rgbCpGain[3].R);
                    end2 = new Coordinate(m_UnitDx / 2, rgbCpGain[4].R);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(0, a * x + b);
                    end1 = new Coordinate(m_UnitDy / 2, c * x + d);
                }
                else if (x < m_UnitDx / 2 && y >= m_UnitDy / 2) // 第3象限
                {
                    start1 = new Coordinate(0, rgbCpGain[3].R);
                    end1 = new Coordinate(m_UnitDx / 2, rgbCpGain[4].R);
                    start2 = new Coordinate(0, rgbCpGain[6].R);
                    end2 = new Coordinate(m_UnitDx / 2, rgbCpGain[7].R);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(m_UnitDy / 2, a * x + b);
                    end1 = new Coordinate(m_UnitDy, c * x + d);
                }
                else if (x >= m_UnitDx / 2 && y >= m_UnitDy / 2) // 第4象限
                {
                    start1 = new Coordinate(m_UnitDx / 2, rgbCpGain[4].R);
                    end1 = new Coordinate(m_UnitDx, rgbCpGain[5].R);
                    start2 = new Coordinate(m_UnitDx / 2, rgbCpGain[7].R);
                    end2 = new Coordinate(m_UnitDx, rgbCpGain[8].R);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(m_UnitDy / 2, a * x + b);
                    end1 = new Coordinate(m_UnitDy, c * x + d);
                }
                else
                { throw new Exception("Failed in calcCamGain."); }

                // 2つの近似式からy方向の近似式を求める x = ey + f
                double e, f;
                calcApproxCoef(start1, end1, out e, out f);

                gainR = e * y + f;

#endregion Red

#region Green

                // x方向の近似式2つを求める y1 = ax1 + b, y2 = cx2 + d                
                if (x >= m_UnitDx / 2 && y < m_UnitDy / 2) // 第1象限
                {
                    start1 = new Coordinate(m_UnitDx / 2, rgbCpGain[1].G);
                    end1 = new Coordinate(m_UnitDx, rgbCpGain[2].G);
                    start2 = new Coordinate(m_UnitDx / 2, rgbCpGain[4].G);
                    end2 = new Coordinate(m_UnitDx, rgbCpGain[5].G);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(0, a * x + b); // x, yを入れ替えて計算する
                    end1 = new Coordinate(m_UnitDy / 2, c * x + d);
                }
                else if (x < m_UnitDx / 2 && y < m_UnitDy / 2) // 第2象限
                {
                    start1 = new Coordinate(0, rgbCpGain[0].G);
                    end1 = new Coordinate(m_UnitDx / 2, rgbCpGain[1].G);
                    start2 = new Coordinate(0, rgbCpGain[3].G);
                    end2 = new Coordinate(m_UnitDx / 2, rgbCpGain[4].G);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(0, a * x + b);
                    end1 = new Coordinate(m_UnitDy / 2, c * x + d);
                }
                else if (x < m_UnitDx / 2 && y >= m_UnitDy / 2) // 第3象限
                {
                    start1 = new Coordinate(0, rgbCpGain[3].G);
                    end1 = new Coordinate(m_UnitDx / 2, rgbCpGain[4].G);
                    start2 = new Coordinate(0, rgbCpGain[6].G);
                    end2 = new Coordinate(m_UnitDx / 2, rgbCpGain[7].G);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(m_UnitDy / 2, a * x + b);
                    end1 = new Coordinate(m_UnitDy, c * x + d);
                }
                else if (x >= m_UnitDx / 2 && y >= m_UnitDy / 2) // 第4象限
                {
                    start1 = new Coordinate(m_UnitDx / 2, rgbCpGain[4].G);
                    end1 = new Coordinate(m_UnitDx, rgbCpGain[5].G);
                    start2 = new Coordinate(m_UnitDx / 2, rgbCpGain[7].G);
                    end2 = new Coordinate(m_UnitDx, rgbCpGain[8].G);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(m_UnitDy / 2, a * x + b);
                    end1 = new Coordinate(m_UnitDy, c * x + d);
                }
                else
                { throw new Exception("Failed in calcCamGain."); }

                // 2つの近似式からy方向の近似式を求める x = ey + f                
                calcApproxCoef(start1, end1, out e, out f);

                gainG = e * y + f;

#endregion Green

#region Blue

                // x方向の近似式2つを求める y1 = ax1 + b, y2 = cx2 + d                
                if (x >= m_UnitDx / 2 && y < m_UnitDy / 2) // 第1象限
                {
                    start1 = new Coordinate(m_UnitDx / 2, rgbCpGain[1].B);
                    end1 = new Coordinate(m_UnitDx, rgbCpGain[2].B);
                    start2 = new Coordinate(m_UnitDx / 2, rgbCpGain[4].B);
                    end2 = new Coordinate(m_UnitDx, rgbCpGain[5].B);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(0, a * x + b); // x, yを入れ替えて計算する
                    end1 = new Coordinate(m_UnitDy / 2, c * x + d);
                }
                else if (x < m_UnitDx / 2 && y < m_UnitDy / 2) // 第2象限
                {
                    start1 = new Coordinate(0, rgbCpGain[0].B);
                    end1 = new Coordinate(m_UnitDx / 2, rgbCpGain[1].B);
                    start2 = new Coordinate(0, rgbCpGain[3].B);
                    end2 = new Coordinate(m_UnitDx / 2, rgbCpGain[4].B);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(0, a * x + b);
                    end1 = new Coordinate(m_UnitDy / 2, c * x + d);
                }
                else if (x < m_UnitDx / 2 && y >= m_UnitDy / 2) // 第3象限
                {
                    start1 = new Coordinate(0, rgbCpGain[3].B);
                    end1 = new Coordinate(m_UnitDx / 2, rgbCpGain[4].B);
                    start2 = new Coordinate(0, rgbCpGain[6].B);
                    end2 = new Coordinate(m_UnitDx / 2, rgbCpGain[7].B);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(m_UnitDy / 2, a * x + b);
                    end1 = new Coordinate(m_UnitDy, c * x + d);
                }
                else if (x >= m_UnitDx / 2 && y >= m_UnitDy / 2) // 第4象限
                {
                    start1 = new Coordinate(m_UnitDx / 2, rgbCpGain[4].B);
                    end1 = new Coordinate(m_UnitDx, rgbCpGain[5].B);
                    start2 = new Coordinate(m_UnitDx / 2, rgbCpGain[7].B);
                    end2 = new Coordinate(m_UnitDx, rgbCpGain[8].B);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(m_UnitDy / 2, a * x + b);
                    end1 = new Coordinate(m_UnitDy, c * x + d);
                }
                else
                { throw new Exception("Failed in calcCamGain."); }

                // 2つの近似式からy方向の近似式を求める x = ey + f                
                calcApproxCoef(start1, end1, out e, out f);

                gainB = e * y + f;

#endregion Blue
            }
            else if (type == UfCamAdjustType.Radiator)
            {
                // Module No.
                // -----------------
                // | 1 | 2 | 3 | 4 |
                // |---|---|---|---|
                // | 5 | 6 | 7 | 8 |
                // -----------------

                // rgbCpGain
                // [ 0] - [ 1] - [ 2] - [ 3] - [ 4] - [ 5]
                //  |      |      |      |      |      |
                // [ 6] - [ 7] - [ 8] - [ 9] - [10] - [11]
                //  |      |      |      |      |      |
                // [12] - [13] - [14] - [15] - [16] - [17]

                // x方向の近似式2つを求める y1 = ax1 + b, y2 = cx2 + d
                Coordinate start1, end1, start2, end2;
                double a, b, c, d;
                double e, f;

#region Red

                if (x >= 0 && x < m_UnitDx / 4 && y >= 0 && y < m_UnitDy / 2) // No.1
                {
                    start1 = new Coordinate(0, rgbCpGain[0].R);
                    end1 = new Coordinate(m_UnitDx / 4, rgbCpGain[1].R);
                    start2 = new Coordinate(0, rgbCpGain[6].R);
                    end2 = new Coordinate(m_UnitDx / 4, rgbCpGain[7].R);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(0, a * x + b); // x, yを入れ替えて計算する
                    end1 = new Coordinate(m_UnitDy / 2, c * x + d);
                }
                else if (x >= m_UnitDx / 4 && x < m_UnitDx / 2 && y >= 0 && y < m_UnitDy / 2) // No.2
                {
                    start1 = new Coordinate(m_UnitDx / 4, rgbCpGain[1].R);
                    end1 = new Coordinate(m_UnitDx / 2, rgbCpGain[2].R);
                    start2 = new Coordinate(m_UnitDx / 4, rgbCpGain[7].R);
                    end2 = new Coordinate(m_UnitDx / 2, rgbCpGain[8].R);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(0, a * x + b); // x, yを入れ替えて計算する
                    end1 = new Coordinate(m_UnitDy / 2, c * x + d);
                }
                else if (x >= m_UnitDx / 2 && x < m_UnitDx * 3 / 4 && y >= 0 && y < m_UnitDy / 2) // No.3
                {
                    start1 = new Coordinate(m_UnitDx / 2, rgbCpGain[3].R);
                    end1 = new Coordinate(m_UnitDx * 3 / 4, rgbCpGain[4].R);
                    start2 = new Coordinate(m_UnitDx / 2, rgbCpGain[9].R);
                    end2 = new Coordinate(m_UnitDx * 3 / 4, rgbCpGain[10].R);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(0, a * x + b); // x, yを入れ替えて計算する
                    end1 = new Coordinate(m_UnitDy / 2, c * x + d);
                }
                else if (x >= m_UnitDx * 3 / 4 && x < m_UnitDx && y >= 0 && y < m_UnitDy / 2) // No.4
                {
                    start1 = new Coordinate(m_UnitDx * 3 / 4, rgbCpGain[4].R);
                    end1 = new Coordinate(m_UnitDx, rgbCpGain[5].R);
                    start2 = new Coordinate(m_UnitDx * 3 / 4, rgbCpGain[10].R);
                    end2 = new Coordinate(m_UnitDx, rgbCpGain[11].R);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(0, a * x + b); // x, yを入れ替えて計算する
                    end1 = new Coordinate(m_UnitDy / 2, c * x + d);
                }
                else if (x >= 0 && x < m_UnitDx / 4 && y >= m_UnitDy / 2 && y < m_UnitDy) // No.5
                {
                    start1 = new Coordinate(0, rgbCpGain[6].R);
                    end1 = new Coordinate(m_UnitDx / 4, rgbCpGain[7].R);
                    start2 = new Coordinate(0, rgbCpGain[12].R);
                    end2 = new Coordinate(m_UnitDx / 4, rgbCpGain[13].R);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(m_UnitDy / 2, a * x + b);
                    end1 = new Coordinate(m_UnitDy, c * x + d);
                }
                else if (x >= m_UnitDx / 4 && x < m_UnitDx / 2 && y >= m_UnitDy / 2 && y < m_UnitDy) // No.6
                {
                    start1 = new Coordinate(m_UnitDx / 4, rgbCpGain[7].R);
                    end1 = new Coordinate(m_UnitDx / 2, rgbCpGain[8].R);
                    start2 = new Coordinate(m_UnitDx / 4, rgbCpGain[13].R);
                    end2 = new Coordinate(m_UnitDx / 2, rgbCpGain[14].R);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(m_UnitDy / 2, a * x + b); // x, yを入れ替えて計算する
                    end1 = new Coordinate(m_UnitDy, c * x + d);
                }
                else if (x >= m_UnitDx / 2 && x < m_UnitDx * 3 / 4 && y >= m_UnitDy / 2 && y < m_UnitDy) // No.7
                {
                    start1 = new Coordinate(m_UnitDx / 2, rgbCpGain[9].R);
                    end1 = new Coordinate(m_UnitDx * 3 / 4, rgbCpGain[10].R);
                    start2 = new Coordinate(m_UnitDx / 2, rgbCpGain[15].R);
                    end2 = new Coordinate(m_UnitDx * 3 / 4, rgbCpGain[16].R);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(m_UnitDy / 2, a * x + b); // x, yを入れ替えて計算する
                    end1 = new Coordinate(m_UnitDy, c * x + d);
                }
                else if (x >= m_UnitDx * 3 / 4 && x < m_UnitDx && y >= m_UnitDy / 2 && y < m_UnitDy) // No.8
                {
                    start1 = new Coordinate(m_UnitDx * 3 / 4, rgbCpGain[10].R);
                    end1 = new Coordinate(m_UnitDx, rgbCpGain[11].R);
                    start2 = new Coordinate(m_UnitDx * 3 / 4, rgbCpGain[16].R);
                    end2 = new Coordinate(m_UnitDx, rgbCpGain[17].R);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(m_UnitDy / 2, a * x + b); // x, yを入れ替えて計算する
                    end1 = new Coordinate(m_UnitDy, c * x + d);
                }
                else
                { throw new Exception("Failed in calcCamGain.(Radiator)"); }

                // 2つの近似式からy方向の近似式を求める x = ey + f
                calcApproxCoef(start1, end1, out e, out f);

                gainR = e * y + f;

#endregion Red

#region Green

                if (x >= 0 && x < m_UnitDx / 4 && y >= 0 && y < m_UnitDy / 2) // No.1
                {
                    start1 = new Coordinate(0, rgbCpGain[0].G);
                    end1 = new Coordinate(m_UnitDx / 4, rgbCpGain[1].G);
                    start2 = new Coordinate(0, rgbCpGain[6].G);
                    end2 = new Coordinate(m_UnitDx / 4, rgbCpGain[7].G);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(0, a * x + b); // x, yを入れ替えて計算する
                    end1 = new Coordinate(m_UnitDy / 2, c * x + d);
                }
                else if (x >= m_UnitDx / 4 && x < m_UnitDx / 2 && y >= 0 && y < m_UnitDy / 2) // No.2
                {
                    start1 = new Coordinate(m_UnitDx / 4, rgbCpGain[1].G);
                    end1 = new Coordinate(m_UnitDx / 2, rgbCpGain[2].G);
                    start2 = new Coordinate(m_UnitDx / 4, rgbCpGain[7].G);
                    end2 = new Coordinate(m_UnitDx / 2, rgbCpGain[8].G);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(0, a * x + b); // x, yを入れ替えて計算する
                    end1 = new Coordinate(m_UnitDy / 2, c * x + d);
                }
                else if (x >= m_UnitDx / 2 && x < m_UnitDx * 3 / 4 && y >= 0 && y < m_UnitDy / 2) // No.3
                {
                    start1 = new Coordinate(m_UnitDx / 2, rgbCpGain[3].G);
                    end1 = new Coordinate(m_UnitDx * 3 / 4, rgbCpGain[4].G);
                    start2 = new Coordinate(m_UnitDx / 2, rgbCpGain[9].G);
                    end2 = new Coordinate(m_UnitDx * 3 / 4, rgbCpGain[10].G);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(0, a * x + b); // x, yを入れ替えて計算する
                    end1 = new Coordinate(m_UnitDy / 2, c * x + d);
                }
                else if (x >= m_UnitDx * 3 / 4 && x < m_UnitDx && y >= 0 && y < m_UnitDy / 2) // No.4
                {
                    start1 = new Coordinate(m_UnitDx * 3 / 4, rgbCpGain[4].G);
                    end1 = new Coordinate(m_UnitDx, rgbCpGain[5].G);
                    start2 = new Coordinate(m_UnitDx * 3 / 4, rgbCpGain[10].G);
                    end2 = new Coordinate(m_UnitDx, rgbCpGain[11].G);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(0, a * x + b); // x, yを入れ替えて計算する
                    end1 = new Coordinate(m_UnitDy / 2, c * x + d);
                }
                else if (x >= 0 && x < m_UnitDx / 4 && y >= m_UnitDy / 2 && y < m_UnitDy) // No.5
                {
                    start1 = new Coordinate(0, rgbCpGain[6].G);
                    end1 = new Coordinate(m_UnitDx / 4, rgbCpGain[7].G);
                    start2 = new Coordinate(0, rgbCpGain[12].G);
                    end2 = new Coordinate(m_UnitDx / 4, rgbCpGain[13].G);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(m_UnitDy / 2, a * x + b);
                    end1 = new Coordinate(m_UnitDy, c * x + d);
                }
                else if (x >= m_UnitDx / 4 && x < m_UnitDx / 2 && y >= m_UnitDy / 2 && y < m_UnitDy) // No.6
                {
                    start1 = new Coordinate(m_UnitDx / 4, rgbCpGain[7].G);
                    end1 = new Coordinate(m_UnitDx / 2, rgbCpGain[8].G);
                    start2 = new Coordinate(m_UnitDx / 4, rgbCpGain[13].G);
                    end2 = new Coordinate(m_UnitDx / 2, rgbCpGain[14].G);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(m_UnitDy / 2, a * x + b); // x, yを入れ替えて計算する
                    end1 = new Coordinate(m_UnitDy, c * x + d);
                }
                else if (x >= m_UnitDx / 2 && x < m_UnitDx * 3 / 4 && y >= m_UnitDy / 2 && y < m_UnitDy) // No.7
                {
                    start1 = new Coordinate(m_UnitDx / 2, rgbCpGain[9].G);
                    end1 = new Coordinate(m_UnitDx * 3 / 4, rgbCpGain[10].G);
                    start2 = new Coordinate(m_UnitDx / 2, rgbCpGain[15].G);
                    end2 = new Coordinate(m_UnitDx * 3 / 4, rgbCpGain[16].G);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(m_UnitDy / 2, a * x + b); // x, yを入れ替えて計算する
                    end1 = new Coordinate(m_UnitDy, c * x + d);
                }
                else if (x >= m_UnitDx * 3 / 4 && x < m_UnitDx && y >= m_UnitDy / 2 && y < m_UnitDy) // No.8
                {
                    start1 = new Coordinate(m_UnitDx * 3 / 4, rgbCpGain[10].G);
                    end1 = new Coordinate(m_UnitDx, rgbCpGain[11].G);
                    start2 = new Coordinate(m_UnitDx * 3 / 4, rgbCpGain[16].G);
                    end2 = new Coordinate(m_UnitDx, rgbCpGain[17].G);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(m_UnitDy / 2, a * x + b); // x, yを入れ替えて計算する
                    end1 = new Coordinate(m_UnitDy, c * x + d);
                }
                else
                { throw new Exception("Failed in calcCamGain.(Radiator)"); }

                // 2つの近似式からy方向の近似式を求める x = ey + f
                calcApproxCoef(start1, end1, out e, out f);

                gainG = e * y + f;

#endregion Green

#region Blue

                if (x >= 0 && x < m_UnitDx / 4 && y >= 0 && y < m_UnitDy / 2) // No.1
                {
                    start1 = new Coordinate(0, rgbCpGain[0].B);
                    end1 = new Coordinate(m_UnitDx / 4, rgbCpGain[1].B);
                    start2 = new Coordinate(0, rgbCpGain[6].B);
                    end2 = new Coordinate(m_UnitDx / 4, rgbCpGain[7].B);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(0, a * x + b); // x, yを入れ替えて計算する
                    end1 = new Coordinate(m_UnitDy / 2, c * x + d);
                }
                else if (x >= m_UnitDx / 4 && x < m_UnitDx / 2 && y >= 0 && y < m_UnitDy / 2) // No.2
                {
                    start1 = new Coordinate(m_UnitDx / 4, rgbCpGain[1].B);
                    end1 = new Coordinate(m_UnitDx / 2, rgbCpGain[2].B);
                    start2 = new Coordinate(m_UnitDx / 4, rgbCpGain[7].B);
                    end2 = new Coordinate(m_UnitDx / 2, rgbCpGain[8].B);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(0, a * x + b); // x, yを入れ替えて計算する
                    end1 = new Coordinate(m_UnitDy / 2, c * x + d);
                }
                else if (x >= m_UnitDx / 2 && x < m_UnitDx * 3 / 4 && y >= 0 && y < m_UnitDy / 2) // No.3
                {
                    start1 = new Coordinate(m_UnitDx / 2, rgbCpGain[3].B);
                    end1 = new Coordinate(m_UnitDx * 3 / 4, rgbCpGain[4].B);
                    start2 = new Coordinate(m_UnitDx / 2, rgbCpGain[9].B);
                    end2 = new Coordinate(m_UnitDx * 3 / 4, rgbCpGain[10].B);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(0, a * x + b); // x, yを入れ替えて計算する
                    end1 = new Coordinate(m_UnitDy / 2, c * x + d);
                }
                else if (x >= m_UnitDx * 3 / 4 && x < m_UnitDx && y >= 0 && y < m_UnitDy / 2) // No.4
                {
                    start1 = new Coordinate(m_UnitDx * 3 / 4, rgbCpGain[4].B);
                    end1 = new Coordinate(m_UnitDx, rgbCpGain[5].B);
                    start2 = new Coordinate(m_UnitDx * 3 / 4, rgbCpGain[10].B);
                    end2 = new Coordinate(m_UnitDx, rgbCpGain[11].B);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(0, a * x + b); // x, yを入れ替えて計算する
                    end1 = new Coordinate(m_UnitDy / 2, c * x + d);
                }
                else if (x >= 0 && x < m_UnitDx / 4 && y >= m_UnitDy / 2 && y < m_UnitDy) // No.5
                {
                    start1 = new Coordinate(0, rgbCpGain[6].B);
                    end1 = new Coordinate(m_UnitDx / 4, rgbCpGain[7].B);
                    start2 = new Coordinate(0, rgbCpGain[12].B);
                    end2 = new Coordinate(m_UnitDx / 4, rgbCpGain[13].B);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(m_UnitDy / 2, a * x + b);
                    end1 = new Coordinate(m_UnitDy, c * x + d);
                }
                else if (x >= m_UnitDx / 4 && x < m_UnitDx / 2 && y >= m_UnitDy / 2 && y < m_UnitDy) // No.6
                {
                    start1 = new Coordinate(m_UnitDx / 4, rgbCpGain[7].B);
                    end1 = new Coordinate(m_UnitDx / 2, rgbCpGain[8].B);
                    start2 = new Coordinate(m_UnitDx / 4, rgbCpGain[13].B);
                    end2 = new Coordinate(m_UnitDx / 2, rgbCpGain[14].B);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(m_UnitDy / 2, a * x + b); // x, yを入れ替えて計算する
                    end1 = new Coordinate(m_UnitDy, c * x + d);
                }
                else if (x >= m_UnitDx / 2 && x < m_UnitDx * 3 / 4 && y >= m_UnitDy / 2 && y < m_UnitDy) // No.7
                {
                    start1 = new Coordinate(m_UnitDx / 2, rgbCpGain[9].B);
                    end1 = new Coordinate(m_UnitDx * 3 / 4, rgbCpGain[10].B);
                    start2 = new Coordinate(m_UnitDx / 2, rgbCpGain[15].B);
                    end2 = new Coordinate(m_UnitDx * 3 / 4, rgbCpGain[16].B);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(m_UnitDy / 2, a * x + b); // x, yを入れ替えて計算する
                    end1 = new Coordinate(m_UnitDy, c * x + d);
                }
                else if (x >= m_UnitDx * 3 / 4 && x < m_UnitDx && y >= m_UnitDy / 2 && y < m_UnitDy) // No.8
                {
                    start1 = new Coordinate(m_UnitDx * 3 / 4, rgbCpGain[10].B);
                    end1 = new Coordinate(m_UnitDx, rgbCpGain[11].B);
                    start2 = new Coordinate(m_UnitDx * 3 / 4, rgbCpGain[16].B);
                    end2 = new Coordinate(m_UnitDx, rgbCpGain[17].B);

                    calcApproxCoef(start1, end1, out a, out b);
                    calcApproxCoef(start2, end2, out c, out d);

                    start1 = new Coordinate(m_UnitDy / 2, a * x + b); // x, yを入れ替えて計算する
                    end1 = new Coordinate(m_UnitDy, c * x + d);
                }
                else
                { throw new Exception("Failed in calcCamGain.(Radiator)"); }

                // 2つの近似式からy方向の近似式を求める x = ey + f
                calcApproxCoef(start1, end1, out e, out f);

                gainB = e * y + f;

#endregion Blue
            }
            else // EachModule
            {
                // 使ってない
            }
        }

        private void calcApproxCoef(Coordinate start, Coordinate end, out double a, out double b)
        {
            a = (end.Y - start.Y) / (end.X - start.X);
            b = start.Y - a * start.X;
        }

        /// <summary>xy->XZ変換(Y=1として処理)</summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="X">X</param>
        /// <param name="Z">Z</param>
        private void xyToXZ(double x, double y, out double X, out double Z)
        {
            xyToXZ(x, y, out  X, out  Z, false);
        }

        /// <summary>xy ot u'v' -> XZ変換(Y=1として処理)</summary>
        /// <param name="x">x or u'</param>
        /// <param name="y">y or v'</param>
        /// <param name="X">X</param>
        /// <param name="Z">Z</param>
        /// <param name="bUV">true=u'/v'</param>
        private void xyToXZ(double x, double y, out double X, out double Z, bool bUV)
        {
            if (bUV == true)
            { // u'v' -> xy変換
                double u = x;
                double v = y;
                x = 9 * u / (6 * u - 16 * v + 12);
                y = 4 * v / (6 * u - 16 * v + 12);
            }
            // Y=1として処理 
            X = x / y;
            Z = (1 - x - y) / y;
        }

        /// <summary>
        /// ポジションx,y座標からModuleの番号確認
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="moduleNo"></param>
        /// <returns></returns>
        private bool CalcModuleNo(int x, int y, out int moduleNo)
        {
            moduleNo = -1;

            if (x == m_CellDx * 0.5 && y == m_CellDy * 0.5)
            { moduleNo = 1; }
            else if (x == m_CellDx * 1.5 && y == m_CellDy * 0.5)
            { moduleNo = 2; }
            else if (x == m_CellDx * 2.5 && y == m_CellDy * 0.5)
            { moduleNo = 3; }
            else if (x == m_CellDx * 3.5 && y == m_CellDy * 0.5)
            { moduleNo = 4; }
            else if (x == m_CellDx * 0.5 && y == m_CellDy * 1.5)
            { moduleNo = 5; }
            else if (x == m_CellDx * 1.5 && y == m_CellDy * 1.5)
            { moduleNo = 6; }
            else if (x == m_CellDx * 2.5 && y == m_CellDy * 1.5)
            { moduleNo = 7; }
            else if (x == m_CellDx * 3.5 && y == m_CellDy * 1.5)
            { moduleNo = 8; }
            else if (x == m_CellDx * 0.5 && y == m_CellDy * 2.5)
            { moduleNo = 9; }
            else if (x == m_CellDx * 1.5 && y == m_CellDy * 2.5)
            { moduleNo = 10; }
            else if (x == m_CellDx * 2.5 && y == m_CellDy * 2.5)
            { moduleNo = 11; }
            else if (x == m_CellDx * 3.5 && y == m_CellDy * 2.5)
            { moduleNo = 12; }
            else
            { return false; }

            return true;
        }

        // added by Hotta 2024/10/02
#if ForCrosstalkCameraUF

        double[,][,][] m_FmtOriginal = new double[100, 100][,][];
        double[,][,][] m_FmtCreated = new double[100, 100][,][];

        public void storeFmt(int cabiX, int cabiY)
        {
            m_FmtOriginal[cabiX, cabiY] = new double[4, 3][];
            m_FmtCreated[cabiX, cabiY] = new double[4, 3][];

            for (int moduleY=0;moduleY<3;moduleY++)
            {
                for (int moduleX = 0; moduleX < 4; moduleX++)
                {
                    m_FmtOriginal[cabiX, cabiY][moduleX, moduleY] = new double[9];
                    m_FmtCreated[cabiX, cabiY][moduleX, moduleY] = new double[9];
                }
            }

            for(int y=0;y<m_UnitDy;y++)
            {
                for (int x = 0; x < m_UnitDx; x++)
                {
                    for (int i = 0; i < 9; i++)
                    {
                        m_FmtOriginal[cabiX, cabiY][x / m_CellDx, y / m_CellDy][i] += m_pFmtOriginal[y * m_UnitDx * 9 + x * 9 + i];
                        m_FmtCreated[cabiX, cabiY][x / m_CellDx, y / m_CellDy][i] += m_pFmtCreated[y * m_UnitDx * 9 + x * 9 + i];
                    }
                }
            }

            for(int y=0;y<3;y++)
            {
                for(int x=0;x<4;x++)
                {
                    for (int i = 0; i < 9; i++)
                    {
                        m_FmtOriginal[cabiX, cabiY][x, y][i] /= m_CellDx * m_CellDx;
                        m_FmtCreated[cabiX, cabiY][x, y][i] /= m_CellDx * m_CellDx;
                    }
                }
            }
        }

        public void saveFmt()
        {
            string path = Directory.GetCurrentDirectory() + "\\Temp\\original.csv";
            string str = "";

            if (Directory.Exists(Path.GetDirectoryName(path)) != true)
                Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (StreamWriter sw = new StreamWriter(path))
            {
                for (int cabiY = 0; cabiY < 2; cabiY++)
                {
                    for (int modY = 0; modY < 3; modY++)
                    {
                        for (int idx = 0; idx < 9; idx+=3)
                        {
                            str = "";
                            for (int cabiX = 0; cabiX < 2; cabiX++)
                            {
                                for (int modX = 0; modX < 4; modX++)
                                {
                                    if(m_FmtOriginal[cabiX, cabiY] == null)
                                    {
                                        str += "-1,-1,-1,";
                                    }
                                    else
                                    {
                                        str += m_FmtOriginal[cabiX, cabiY][modX, modY][idx + 0].ToString() + ","
                                             + m_FmtOriginal[cabiX, cabiY][modX, modY][idx + 1].ToString() + ","
                                             + m_FmtOriginal[cabiX, cabiY][modX, modY][idx + 2].ToString() + ",";
                                    }
                                }
                            }
                            sw.WriteLine(str);
                        }
                    }
                }
            }

            path = Directory.GetCurrentDirectory() + "\\Temp\\created.csv";

            using (StreamWriter sw = new StreamWriter(path))
            {
                for (int cabiY = 0; cabiY < 2; cabiY++)
                {
                    for (int modY = 0; modY < 3; modY++)
                    {
                        for (int idx = 0; idx < 9; idx += 3)
                        {
                            str = "";
                            for (int cabiX = 0; cabiX < 2; cabiX++)
                            {
                                for (int modX = 0; modX < 4; modX++)
                                {
                                    if (m_FmtCreated[cabiX, cabiY] == null)
                                    {
                                        str += "-1,-1,-1,";
                                    }
                                    else
                                    {
                                        str += m_FmtCreated[cabiX, cabiY][modX, modY][idx + 0].ToString() + ","
                                         + m_FmtCreated[cabiX, cabiY][modX, modY][idx + 1].ToString() + ","
                                         + m_FmtCreated[cabiX, cabiY][modX, modY][idx + 2].ToString() + ",";
                                    }
                                }
                            }
                            sw.WriteLine(str);
                        }
                    }
                }
            }
        }
#endif

        #endregion Private Methods
    }
}