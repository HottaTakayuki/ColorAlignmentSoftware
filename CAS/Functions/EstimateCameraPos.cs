using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TransformImage
{
    class EstimateCameraPos
    {
        public class CameraParam
        {
            double m_f;
            public double f
            {
                get { return m_f; }
                set { m_f = value; }
            }

            double m_SensorSizeH;
            public double SensorSizeH
            {
                get { return m_SensorSizeH; }
                set { m_SensorSizeH = value; }
            }

            double m_SensorSizeV;
            public double SensorSizeV
            {
                get { return m_SensorSizeV; }
                set { m_SensorSizeV = value; }
            }

            int m_SensorPxH;
            public int SensorPxH
            {
                get { return m_SensorPxH; }
                set { m_SensorPxH = value; }
            }

            int m_SensorPxV;
            public int SensorPxV
            {
                get { return m_SensorPxV; }
                set { m_SensorPxV = value; }
            }

            public CameraParam(double f, double SensorSizeH, double SensorSizeV, int SensorPxH, int SensorPxV)
            {
                m_f = f;
                m_SensorSizeH = SensorSizeH;
                m_SensorSizeV = SensorSizeV;
                m_SensorPxH = SensorPxH;
                m_SensorPxV = SensorPxV;
            }

            public void Set(double f, double SensorSizeH, double SensorSizeV, int SensorPxH, int SensorPxV)
            {
                m_f = f;
                m_SensorSizeH = SensorSizeH;
                m_SensorSizeV = SensorSizeV;
                m_SensorPxH = SensorPxH;
                m_SensorPxV = SensorPxV;
            }
        }
        CameraParam m_CameraParameter;
        public CameraParam CameraParameter
        {
            get { return m_CameraParameter; }
        }

        public EstimateCameraPos()
        {
            m_CameraParameter = new CameraParam(16, 23.5, 15.6, 3000, 2000);
        }

        Point2f[] m_ImagePoints;
        public Point2f[] ImagePoints
        {
            set { m_ImagePoints = value; }
        }

        Point3f[] m_ObjectPoints;
        public Point3f[] ObjectPoints
        {
            set { m_ObjectPoints = value; }
        }

        /*
        float[] m_Rot;
        public float[] Rot
        {
            get { return m_Rot; }
        }

        float[] m_Trans;
        public float[] Trans
        {
            get { return m_Trans; }
        }
        */
        /**/
        double[] m_Rot;
        public double[] Rot
        {
            get { return m_Rot; }
        }

        double[] m_Trans;
        public double[] Trans
        {
            get { return m_Trans; }
        }
        /**/

        public void Estimate()
        {
            // added by Hotta 2023/02/27 for Coverity
            using (Mat DIST_COEF = new Mat(4, 1, MatType.CV_32FC1, 0))
            using (Mat rvec = new Mat(new Size(1, 3), MatType.CV_64FC1, 0))
            using (Mat tvec = new Mat(new Size(1, 3), MatType.CV_64FC1, 0))
            using (Mat matImagePoints = new Mat(m_ImagePoints.Length, 1, MatType.CV_32FC2, m_ImagePoints))
            using (Mat matObjectPoints = new Mat(m_ObjectPoints.Length, 1, MatType.CV_32FC3, m_ObjectPoints))
            {
                Mat CAMERA_MATRIX = new Mat(3, 3, MatType.CV_32FC1);
                CAMERA_MATRIX.Set(0, 0, (float)(m_CameraParameter.f * m_CameraParameter.SensorPxH / m_CameraParameter.SensorSizeH));
                CAMERA_MATRIX.Set(0, 1, 0);
                CAMERA_MATRIX.Set(0, 2, (float)m_CameraParameter.SensorPxH / 2);
                CAMERA_MATRIX.Set(1, 0, 0);
                CAMERA_MATRIX.Set(1, 1, (float)(m_CameraParameter.f * m_CameraParameter.SensorPxV / m_CameraParameter.SensorSizeV));
                CAMERA_MATRIX.Set(1, 2, (float)m_CameraParameter.SensorPxV / 2);
                CAMERA_MATRIX.Set(2, 0, 0);
                CAMERA_MATRIX.Set(2, 1, 0);
                CAMERA_MATRIX.Set(2, 2, 1);

                // deleted by Hotta 2023/02/27 for Coverity
                // using()に変更
                /*
                Mat DIST_COEF = new Mat(4, 1, MatType.CV_32FC1, 0);
                Mat matImagePoints = new Mat(m_ImagePoints.Length, 1, MatType.CV_32FC2, m_ImagePoints);
                Mat matObjectPoints = new Mat(m_ObjectPoints.Length, 1, MatType.CV_32FC3, m_ObjectPoints);
                */

                // deleted by Hotta 2023/02/27 for Coverity
                // using()に変更
                /*
                //Mat rvec = new Mat();
                //Mat tvec = new Mat();
                Mat rvec = new Mat(new Size(1, 3), MatType.CV_64FC1);
                Mat tvec = new Mat(new Size(1, 3), MatType.CV_64FC1);
                rvec.SetTo(0);
                tvec.SetTo(0);
                */

                //Cv2.SolvePnP(matObjectPoints, matImagePoints, CAMERA_MATRIX, DIST_COEF, rvec, tvec);
                //Cv2.SolvePnPRansac(matObjectPoints, matImagePoints, CAMERA_MATRIX, DIST_COEF, rvec, tvec);                            // rvec, tvec：64FC1
                //Cv2.SolvePnP(matObjectPoints, matImagePoints, CAMERA_MATRIX, DIST_COEF, rvec, tvec, false, SolvePnPFlags.UPNP);   // rvec, tvec：64FC1
                //Cv2.SolvePnP(matObjectPoints, matImagePoints, CAMERA_MATRIX, DIST_COEF, rvec, tvec, true, SolvePnPFlags.UPNP);   // rvec, tvec：64FC1
                //Cv2.SolvePnP(matObjectPoints, matImagePoints, CAMERA_MATRIX, DIST_COEF, rvec, tvec, false, SolvePnPFlags.Iterative);   // rvec, tvec：64FC1
                Cv2.SolvePnP(matObjectPoints, matImagePoints, CAMERA_MATRIX, DIST_COEF, rvec, tvec, true, SolvePnPFlags.Iterative);   // rvec, tvec：64FC1


                //m_Rot = new float[3];
                m_Rot = new double[3];
                Marshal.Copy(rvec.Data, m_Rot, 0, 3);
                m_Rot[0] *= (float)(180.0 / Math.PI);
                m_Rot[1] *= (float)(180.0 / Math.PI);
                m_Rot[2] *= (float)(180.0 / Math.PI);

                Mat matRot = new Mat();         // 投影ベクトル
                Cv2.Rodrigues(rvec, matRot);    // 回転行列化

                //Mat matPos = -matRot.Inv() * tvec;    // - をコメントアウト
                Mat matPos = matRot.Inv() * tvec;    // - をコメントアウト  // Coverityチェック　代入に失敗するとリーク　代入に失敗するとリーク
                                                     //m_Trans = new float[3];
                m_Trans = new double[3];
                Marshal.Copy(matPos.Data, m_Trans, 0, 3);

                matPos.Dispose();
                matRot.Dispose();
                // deleted by Hotta 2023/02/27
                // using()に変更
                /*
                tvec.Dispose();
                rvec.Dispose();
                matObjectPoints.Dispose();
                matImagePoints.Dispose();
                */
                // deleted by Hotta 2023/02/27
                // using()に変更
                /*
                DIST_COEF.Dispose();
                */
                CAMERA_MATRIX.Dispose();
            }
        }
    }
}
