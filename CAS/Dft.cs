//#define SaveLogFile 

// added by Hotta 2022/07/13
#define Coverity


using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DigitalFourierTransform
{
    class Dft
    {
        Mat m_matSource;
        Mat[] m_matDft;
        Mat m_DftImage;

        public Dft()
        {
            m_matDft = new Mat[3];
            for (int n = 0; n < 3; n++)
                m_matDft[n] = new Mat();
        }

        public bool SetSourceImage(string filename)
        {
            m_matSource = new Mat(filename);

            return true;
        }

        public bool SetSourceImage(Mat mat)
        {
            m_matSource = mat.Clone();

            return true;
        }

        public bool TransForm()
        {
            Mat[] src = new Mat[3];

            // modified by Hotta 2022/07/13
#if Coverity

#else
            Mat matBitmap = new Mat();
#endif

            // 入力画像を DFT 入力形式に変換
            // 入力画像のサイズをFFT処理に適切なサイズに拡張
            src = Cv2.Split(m_matSource);

            for (int ch = 0; ch < /*3*/ m_matSource.Channels(); ch++)
            {
                //②画像を最適なサイズに変形  
                Mat padded = new Mat();
                int optimalRows = Cv2.GetOptimalDFTSize(src[ch].Rows);
                int optimalCols = Cv2.GetOptimalDFTSize(src[ch].Cols);
                Cv2.CopyMakeBorder(src[ch], padded, 0, optimalRows - src[ch].Rows, 0, optimalCols - src[ch].Cols, BorderTypes.Constant, Scalar.All(0));
#if SaveLogFile
                padded.ConvertTo(matBitmap, MatType.CV_8UC1);
                matBitmap.SaveImage("c:\\temp\\padded.bmp");
#endif
                //③0で拡張された別の平面に移す  
                Mat paddedF32 = new Mat();
                padded.ConvertTo(paddedF32, MatType.CV_32F);
                Mat[] planes = { paddedF32, Mat.Zeros(padded.Size(), MatType.CV_32F) };
                Mat complex = new Mat();
                Cv2.Merge(planes, complex);


                //④dftを入力画像とフィットさせDFT（フーリエ変換）  
                //Cv2.Dft(complex, dft);
                Cv2.Dft(complex, m_matDft[ch], DftFlags.Scale);

                Mat[] dftPlanes = Cv2.Split(m_matDft[ch]);

                // deleted by Hotta 2023/03/03
                /*
                double minVal, maxVal;
                */
#if SaveLogFile

                Mat dft0 = new Mat(), dft1 = new Mat();
                Cv2.Normalize(dftPlanes[0], dft0, 0, 255, NormTypes.MinMax);
                Cv2.Normalize(dftPlanes[1], dft1, 0, 255, NormTypes.MinMax);
                dft0.ConvertTo(dft0, MatType.CV_8UC1);
                dft1.ConvertTo(dft1, MatType.CV_8UC1);
                dft0.SaveImage("c:\\temp\\dft0.bmp");
                dft1.SaveImage("c:\\temp\\dft1.bmp");
                Cv2.MinMaxLoc(dft0, out minVal, out maxVal);
#endif

                //⑤各画素の大きさを計算する  
                Mat magnitude = new Mat();
                Cv2.Magnitude(dftPlanes[0], dftPlanes[1], magnitude);

#if SaveLogFile
                //Cv2.MinMaxLoc(magnitude, out minVal, out maxVal);  
                Mat magntuideImg = new Mat();
                Cv2.Normalize(magnitude, magntuideImg, 0, 255, NormTypes.MinMax);
                magntuideImg.SaveImage("c:\\temp\\magnitudeImg.bmp");
#endif

                //⑥対数スケールに変更  
                magnitude += Scalar.All(1); // Coverityチェック　代入に失敗するとリーク
                Cv2.Log(magnitude, magnitude);

#if SaveLogFile
                Cv2.MinMaxLoc(magnitude, out minVal, out maxVal);
                Mat magnitudeLogImg = magnitude / maxVal * 255;
                magnitudeLogImg.ConvertTo(magnitudeLogImg, MatType.CV_8UC1);
                Cv2.MinMaxLoc(magnitudeLogImg, out minVal, out maxVal);
                magnitudeLogImg.SaveImage("c:\\temp\\magnitudeLogImg.bmp");
#endif

                //⑦奇数の行及び列を持つ場合、偶数にクロップする  
                Mat spectrum = magnitude[new Rect(0, 0, magnitude.Cols & -2, magnitude.Rows & -2)];


                //⑧原点が画像の中心になるように4象限を入れ替え  
                shiftImage(ref spectrum);

                m_DftImage = spectrum.Clone();

#if SaveLogFile
                Cv2.Normalize(spectrum, spectrum, 0, 255, NormTypes.MinMax);//0～255に正規化  
                spectrum.ConvertTo(spectrum, MatType.CV_8U);
                spectrum.SaveImage("c:\\temp\\spectrum.bmp");
#endif

                // added by Hotta 2022/07/13
                spectrum.Dispose();
                magnitude.Dispose();
                for (int n = 0; n < dftPlanes.Length; n++)
                    dftPlanes[n].Dispose();
                complex.Dispose();
                for (int n = 0; n < planes.Length; n++)
                    planes[n].Dispose();
                paddedF32.Dispose();
                padded.Dispose();
                //
            }

            for(int n=0;n<src.Length;n++)
                src[n].Dispose();

            return true;
        }


        public Mat GetSpectrumImage()
        {
            return m_DftImage;
        }

        public bool ModifySpectrum()
        {
            // deleted by Hotta 2022/08/02
            /*
            Mat[] dest = new Mat[3];
            */

            for (int ch=0;ch<3;ch++)
            {
                // modified by Hotta 2022/07/13
#if Coverity
                using (Mat mask = new Mat(m_matDft[ch].Size(), m_matDft[ch].Type(), Scalar.All(1)))
                {
                    if (ch < 3)
                    {
                        // 高周波成分を消す
                        mask.Rectangle(new Rect(285 - 15, 129 - 15, 30, 30), new Scalar(0), -1);
                        mask.Rectangle(new Rect(215 - 15, 171 - 15, 30, 30), new Scalar(0), -1);
                    }
                    //else if (ch == 2)
                    //{
                    //    /*
                    //    // 低周波成分を消す
                    //    mask.Rectangle(new Rect(250, 150, 10, 10), new Scalar(0), -1);
                    //    // オフセット分は残す
                    //    mask.Rectangle(new Rect(250, 150, 1, 1), new Scalar(1), -1);
                    //    */
                    //}

                    shiftImage(ref m_matDft[ch]);
                    m_matDft[ch] = m_matDft[ch].Mul(mask);  // Coverityチェック　代入に失敗するとリーク
                    shiftImage(ref m_matDft[ch]);
                }
#else
                Mat mask = new Mat(m_matDft[ch].Size(), m_matDft[ch].Type(), Scalar.All(1));

                if(ch < 3)
                {
                    // 高周波成分を消す
                    mask.Rectangle(new Rect(285 - 15, 129 - 15, 30, 30), new Scalar(0), -1);
                    mask.Rectangle(new Rect(215 - 15, 171 - 15, 30, 30), new Scalar(0), -1);
                }
                else if(ch == 2)
                {
                    /*
                    // 低周波成分を消す
                    mask.Rectangle(new Rect(250, 150, 10, 10), new Scalar(0), -1);
                    // オフセット分は残す
                    mask.Rectangle(new Rect(250, 150, 1, 1), new Scalar(1), -1);
                    */
                }

                shiftImage(ref m_matDft[ch]);
                m_matDft[ch] = m_matDft[ch].Mul(mask);
                shiftImage(ref m_matDft[ch]);
#endif
            }

            return true;
        }


        public bool InvertTransForm()
        {
            Mat[] dest = new Mat[3];
            Mat matDest;

            for(int ch=0;ch<3;ch++)
            {
                //逆フーリエ
                Mat inverseTransform = new Mat();
                Cv2.Dft(m_matDft[ch], inverseTransform, DftFlags.Inverse | DftFlags.RealOutput);
                //Cv2.Normalize(inverseTransform, inverseTransform, 0, 255, NormTypes.MinMax);
                dest[ch] = inverseTransform.Clone();
                inverseTransform.ConvertTo(inverseTransform, MatType.CV_8U);
                inverseTransform.SaveImage("c:\\temp\\inverseTransform.bmp");
            }

            Mat dest0 = new Mat();  // Coverityチェック　例外パスだとリーク
            Cv2.Merge(dest, dest0);
            matDest = dest0.Clone(new Rect(0, 0, m_matSource.Width, m_matSource.Height));

            matDest.SaveImage("c:\\temp\\dest.bmp");

            dest0.Dispose();
            matDest.Dispose();

            dest[2].Dispose();
            dest[1].Dispose();
            dest[0].Dispose();

            return true;
        }


        // 画像入れ替え関数
        void shiftImage(ref Mat img)
        {
            int cx = img.Cols / 2, cy = img.Rows / 2;//各象限の行数と列数を計算

            // modified by Hotta 2022/07/13
#if Coverity
            using (Mat q0 = new Mat(img, new Rect(0, 0, cx, cy)))//各象限に対応するMatを作成  
            using (Mat q1 = new Mat(img, new Rect(cx, 0, cx, cy)))
            using (Mat q2 = new Mat(img, new Rect(0, cy, cx, cy)))
            using (Mat q3 = new Mat(img, new Rect(cx, cy, cx, cy)))
            using (Mat tmp = new Mat())
            {
                q0.CopyTo(tmp);
                q3.CopyTo(q0);
                tmp.CopyTo(q3);

                q1.CopyTo(tmp);
                q2.CopyTo(q1);
                tmp.CopyTo(q2);
            }
#else
            Mat q0 = new Mat(img, new Rect(0, 0, cx, cy));//各象限に対応するMatを作成  
            Mat q1 = new Mat(img, new Rect(cx, 0, cx, cy));
            Mat q2 = new Mat(img, new Rect(0, cy, cx, cy));
            Mat q3 = new Mat(img, new Rect(cx, cy, cx, cy));
            Mat tmp = new Mat();
            q0.CopyTo(tmp);
            q3.CopyTo(q0);
            tmp.CopyTo(q3);

            q1.CopyTo(tmp);
            q2.CopyTo(q1);
            tmp.CopyTo(q2);
#endif
        }

        public int GetOptimalSize(int size)
        {
            return Cv2.GetOptimalDFTSize(size);
        }
    }
}
