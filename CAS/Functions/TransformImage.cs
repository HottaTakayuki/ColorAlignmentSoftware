using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransformImage
{
    class TransformImage
    {
        Mat[] m_Mat3DPoints;

        Mat m_MatTranslation;
        Mat m_MatRy;
        Mat m_MatRx;
        Mat m_MatRz;

        Mat m_MatTranslation2;
        Mat m_MatRy2;
        Mat m_MatRx2;
        Mat m_MatRz2;


        Mat m_MatShiftToCameraCoordinate;

        Point2f[] m_ImagePoints;
        public Point2f[] ImagePoints
        {
            get { return m_ImagePoints; }
        }

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


        Mat[] m_MatMoved3DPoints;

        public TransformImage()
        {
            m_Mat3DPoints = new Mat[4];
            for(int n=0;n<4;n++)
                m_Mat3DPoints[n] = new Mat(4, 1, MatType.CV_32FC1);

            m_MatTranslation = new Mat(4, 4, MatType.CV_32FC1);

            m_MatRy = new Mat(4, 4, MatType.CV_32FC1);
            m_MatRx = new Mat(4, 4, MatType.CV_32FC1);
            m_MatRz = new Mat(4, 4, MatType.CV_32FC1);

            m_MatTranslation2 = new Mat(4, 4, MatType.CV_32FC1);

            m_MatRy2 = new Mat(4, 4, MatType.CV_32FC1);
            m_MatRx2 = new Mat(4, 4, MatType.CV_32FC1);
            m_MatRz2 = new Mat(4, 4, MatType.CV_32FC1);


            m_MatShiftToCameraCoordinate = new Mat(3, 1, MatType.CV_32FC1);
            m_CameraParameter = new CameraParam(16, 23.5, 15.6, 3000, 2000);

            m_ImagePoints = new Point2f[4];

            m_MatMoved3DPoints = new Mat[4];
            for (int n = 0; n < 4; n++)
                m_MatMoved3DPoints[n] = new Mat(3, 1, MatType.CV_32FC1);
        }

        public void Set3DPoints(int pos, float X, float Y, float Z)
        {
            m_Mat3DPoints[pos].Set(0, 0, X);
            m_Mat3DPoints[pos].Set(1, 0, Y);
            m_Mat3DPoints[pos].Set(2, 0, Z);
            m_Mat3DPoints[pos].Set(3, 0, 1.0f);
        }

        public void SetTranslation(float X, float Y, float Z)
        {
            m_MatTranslation.Set(0, 0, 1.0f);
            m_MatTranslation.Set(0, 1, 0.0f);
            m_MatTranslation.Set(0, 2, 0.0f);
            m_MatTranslation.Set(0, 3, X);

            m_MatTranslation.Set(1, 0, 0.0f);
            m_MatTranslation.Set(1, 1, 1.0f);
            m_MatTranslation.Set(1, 2, 0.0f);
            m_MatTranslation.Set(1, 3, Y);

            m_MatTranslation.Set(2, 0, 0.0f);
            m_MatTranslation.Set(2, 1, 0.0f);
            m_MatTranslation.Set(2, 2, 1.0f);
            m_MatTranslation.Set(2, 3, Z);

            m_MatTranslation.Set(3, 0, 0.0f);
            m_MatTranslation.Set(3, 1, 0.0f);
            m_MatTranslation.Set(3, 2, 0.0f);
            m_MatTranslation.Set(3, 3, 1.0f);
        }

        public void SetRy(double degrees)
        {
            m_MatRy.Set(0, 0, (float)Math.Cos(degrees/180* Math.PI));
            m_MatRy.Set(0, 1, 0.0f);
            m_MatRy.Set(0, 2, (float)Math.Sin(degrees / 180 * Math.PI));
            m_MatRy.Set(0, 3, 0.0f);

            m_MatRy.Set(1, 0, 0.0f);
            m_MatRy.Set(1, 1, 1.0f);
            m_MatRy.Set(1, 2, 0.0f);
            m_MatRy.Set(1, 3, 0.0f);

            m_MatRy.Set(2, 0, -(float)Math.Sin(degrees / 180 * Math.PI));
            m_MatRy.Set(2, 1, 0.0f);
            m_MatRy.Set(2, 2, (float)Math.Cos(degrees / 180 * Math.PI));
            m_MatRy.Set(2, 3, 0.0f);

            m_MatRy.Set(3, 0, 0.0f);
            m_MatRy.Set(3, 1, 0.0f);
            m_MatRy.Set(3, 2, 0.0f);
            m_MatRy.Set(3, 3, 1.0f);
        }

        public void SetRx(double degrees)
        {
            m_MatRx.Set(0, 0, 1.0f);
            m_MatRx.Set(0, 1, 0.0f);
            m_MatRx.Set(0, 2, 0.0f);
            m_MatRx.Set(0, 3, 0.0f);

            m_MatRx.Set(1, 0, 0.0f);
            m_MatRx.Set(1, 1, (float)Math.Cos(degrees / 180 * Math.PI));
            m_MatRx.Set(1, 2, -(float)Math.Sin(degrees / 180 * Math.PI));
            m_MatRx.Set(1, 3, 0.0f);

            m_MatRx.Set(2, 0, 0.0f);
            m_MatRx.Set(2, 1, (float)Math.Sin(degrees / 180 * Math.PI));
            m_MatRx.Set(2, 2, (float)Math.Cos(degrees / 180 * Math.PI));
            m_MatRx.Set(2, 3, 0.0f);

            m_MatRx.Set(3, 0, 0.0f);
            m_MatRx.Set(3, 1, 0.0f);
            m_MatRx.Set(3, 2, 0.0f);
            m_MatRx.Set(3, 3, 1.0f);
        }

        public void SetRz(double degrees)
        {
            m_MatRz.Set(0, 0, (float)Math.Cos(degrees / 180 * Math.PI));
            m_MatRz.Set(0, 1, -(float)Math.Sin(degrees / 180 * Math.PI));
            m_MatRz.Set(0, 2, 0.0f);
            m_MatRz.Set(0, 3, 0.0f);

            m_MatRz.Set(1, 0, (float)Math.Sin(degrees / 180 * Math.PI));
            m_MatRz.Set(1, 1, (float)Math.Cos(degrees / 180 * Math.PI));
            m_MatRz.Set(1, 2, 0.0f);
            m_MatRz.Set(1, 3, 0.0f);

            m_MatRz.Set(2, 0, 0.0f);
            m_MatRz.Set(2, 1, 0.0f);
            m_MatRz.Set(2, 2, 1.0f);
            m_MatRz.Set(2, 3, 0.0f);

            m_MatRz.Set(3, 0, 0.0f);
            m_MatRz.Set(3, 1, 0.0f);
            m_MatRz.Set(3, 2, 0.0f);
            m_MatRz.Set(3, 3, 1.0f);
        }

        public void SetTranslation2(float X, float Y, float Z)
        {
            m_MatTranslation2.Set(0, 0, 1.0f);
            m_MatTranslation2.Set(0, 1, 0.0f);
            m_MatTranslation2.Set(0, 2, 0.0f);
            m_MatTranslation2.Set(0, 3, X);

            m_MatTranslation2.Set(1, 0, 0.0f);
            m_MatTranslation2.Set(1, 1, 1.0f);
            m_MatTranslation2.Set(1, 2, 0.0f);
            m_MatTranslation2.Set(1, 3, Y);

            m_MatTranslation2.Set(2, 0, 0.0f);
            m_MatTranslation2.Set(2, 1, 0.0f);
            m_MatTranslation2.Set(2, 2, 1.0f);
            m_MatTranslation2.Set(2, 3, Z);

            m_MatTranslation2.Set(3, 0, 0.0f);
            m_MatTranslation2.Set(3, 1, 0.0f);
            m_MatTranslation2.Set(3, 2, 0.0f);
            m_MatTranslation2.Set(3, 3, 1.0f);
        }

        public void SetRy2(double degrees)
        {
            m_MatRy2.Set(0, 0, (float)Math.Cos(degrees / 180 * Math.PI));
            m_MatRy2.Set(0, 1, 0.0f);
            m_MatRy2.Set(0, 2, (float)Math.Sin(degrees / 180 * Math.PI));
            m_MatRy2.Set(0, 3, 0.0f);

            m_MatRy2.Set(1, 0, 0.0f);
            m_MatRy2.Set(1, 1, 1.0f);
            m_MatRy2.Set(1, 2, 0.0f);
            m_MatRy2.Set(1, 3, 0.0f);

            m_MatRy2.Set(2, 0, -(float)Math.Sin(degrees / 180 * Math.PI));
            m_MatRy2.Set(2, 1, 0.0f);
            m_MatRy2.Set(2, 2, (float)Math.Cos(degrees / 180 * Math.PI));
            m_MatRy2.Set(2, 3, 0.0f);

            m_MatRy2.Set(3, 0, 0.0f);
            m_MatRy2.Set(3, 1, 0.0f);
            m_MatRy2.Set(3, 2, 0.0f);
            m_MatRy2.Set(3, 3, 1.0f);
        }

        public void SetRx2(double degrees)
        {
            m_MatRx2.Set(0, 0, 1.0f);
            m_MatRx2.Set(0, 1, 0.0f);
            m_MatRx2.Set(0, 2, 0.0f);
            m_MatRx2.Set(0, 3, 0.0f);

            m_MatRx2.Set(1, 0, 0.0f);
            m_MatRx2.Set(1, 1, (float)Math.Cos(degrees / 180 * Math.PI));
            m_MatRx2.Set(1, 2, -(float)Math.Sin(degrees / 180 * Math.PI));
            m_MatRx2.Set(1, 3, 0.0f);

            m_MatRx2.Set(2, 0, 0.0f);
            m_MatRx2.Set(2, 1, (float)Math.Sin(degrees / 180 * Math.PI));
            m_MatRx2.Set(2, 2, (float)Math.Cos(degrees / 180 * Math.PI));
            m_MatRx2.Set(2, 3, 0.0f);

            m_MatRx2.Set(3, 0, 0.0f);
            m_MatRx2.Set(3, 1, 0.0f);
            m_MatRx2.Set(3, 2, 0.0f);
            m_MatRx2.Set(3, 3, 1.0f);
        }

        public void SetRz2(double degrees)
        {
            m_MatRz2.Set(0, 0, (float)Math.Cos(degrees / 180 * Math.PI));
            m_MatRz2.Set(0, 1, -(float)Math.Sin(degrees / 180 * Math.PI));
            m_MatRz2.Set(0, 2, 0.0f);
            m_MatRz2.Set(0, 3, 0.0f);

            m_MatRz2.Set(1, 0, (float)Math.Sin(degrees / 180 * Math.PI));
            m_MatRz2.Set(1, 1, (float)Math.Cos(degrees / 180 * Math.PI));
            m_MatRz2.Set(1, 2, 0.0f);
            m_MatRz2.Set(1, 3, 0.0f);

            m_MatRz2.Set(2, 0, 0.0f);
            m_MatRz2.Set(2, 1, 0.0f);
            m_MatRz2.Set(2, 2, 1.0f);
            m_MatRz2.Set(2, 3, 0.0f);

            m_MatRz2.Set(3, 0, 0.0f);
            m_MatRz2.Set(3, 1, 0.0f);
            m_MatRz2.Set(3, 2, 0.0f);
            m_MatRz2.Set(3, 3, 1.0f);
        }


        public void SetShiftToCameraCoordinate(float X, float Y, float Z)
        {
            m_MatShiftToCameraCoordinate.Set(0, 0, X);
            m_MatShiftToCameraCoordinate.Set(1, 0, Y);
            m_MatShiftToCameraCoordinate.Set(2, 0, Z);
        }

        public void Calc()
        {
            Mat matTranslation;  // Coverityチェック　例外パスだとリーク
            Mat matRotate;  // Coverityチェック　例外パスだとリーク
            Mat matMoved3DPoint;  // Coverityチェック　例外パスだとリーク
            Mat matPointCameraCoordinate;
            for (int n=0;n<4;n++)
            {
                matTranslation = m_MatTranslation * m_Mat3DPoints[n];   // Coverityチェック　代入に失敗するとリーク
                //matRotate = m_MatRz * (m_MatRx * (m_MatRy * matTranslation));
                matRotate = m_MatRz * (m_MatRy * (m_MatRx * matTranslation));

                /*
                //
                // あおった後に、
                // パネルセンターでRY回転
                Mat trans = new Mat(4, 4, MatType.CV_32FC1);

                trans.Set(0, 0, 1.0f);
                trans.Set(0, 1, 0.0f);
                trans.Set(0, 2, 0.0f);
                trans.Set(0, 3, 0.0f);

                trans.Set(1, 0, 0.0f);
                trans.Set(1, 1, 1.0f);
                trans.Set(1, 2, 0.0f);
                trans.Set(1, 3, 0.0f);

                trans.Set(2, 0, 0.0f);
                trans.Set(2, 1, 0.0f);
                trans.Set(2, 2, 1.0f);
                trans.Set(2, 3, -4000.0f);

                trans.Set(3, 0, 0.0f);
                trans.Set(3, 1, 0.0f);
                trans.Set(3, 2, 0.0f);
                trans.Set(3, 3, 1.0f);

                double degrees = 45;
                Mat ry = new Mat(4, 4, MatType.CV_32FC1);
                ry.Set(0, 0, (float)Math.Cos(degrees / 180 * Math.PI));
                ry.Set(0, 1, 0.0f);
                ry.Set(0, 2, (float)Math.Sin(degrees / 180 * Math.PI));
                ry.Set(0, 3, 0.0f);

                ry.Set(1, 0, 0.0f);
                ry.Set(1, 1, 1.0f);
                ry.Set(1, 2, 0.0f);
                ry.Set(1, 3, 0.0f);

                ry.Set(2, 0, -(float)Math.Sin(degrees / 180 * Math.PI));
                ry.Set(2, 1, 0.0f);
                ry.Set(2, 2, (float)Math.Cos(degrees / 180 * Math.PI));
                ry.Set(2, 3, 0.0f);

                ry.Set(3, 0, 0.0f);
                ry.Set(3, 1, 0.0f);
                ry.Set(3, 2, 0.0f);
                ry.Set(3, 3, 1.0f);
                matRotate = ry * (trans * matRotate);
                //
                */


                // 次数の変換（1x4 -> 1x3）
                matMoved3DPoint = new Mat(3, 1, MatType.CV_32FC1);
                matMoved3DPoint.Set(0, 0, matRotate.At<float>(0, 0));
                matMoved3DPoint.Set(1, 0, matRotate.At<float>(1, 0));
                matMoved3DPoint.Set(2, 0, matRotate.At<float>(2, 0));
                matPointCameraCoordinate = matMoved3DPoint + m_MatShiftToCameraCoordinate;  // Coverityチェック　代入に失敗するとリーク

                m_ImagePoints[n] = new Point2f();
                //m_ImagePoints[n].X = (float)(matPointCameraCoordinate.At<float>(0, 0) / matPointCameraCoordinate.At<float>(0, 2) * m_CameraParameter.SensorPxH / m_CameraParameter.SensorSizeH * m_CameraParameter.f + m_CameraParameter.SensorPxH / 2);
                //m_ImagePoints[n].Y = (float)(matPointCameraCoordinate.At<float>(1, 0) / matPointCameraCoordinate.At<float>(0, 2) * m_CameraParameter.SensorPxV / m_CameraParameter.SensorSizeV * m_CameraParameter.f + m_CameraParameter.SensorPxV / 2);
                m_ImagePoints[n].X = (float)(matPointCameraCoordinate.At<float>(0, 0) / matPointCameraCoordinate.At<float>(2, 0) * m_CameraParameter.SensorPxH / m_CameraParameter.SensorSizeH * m_CameraParameter.f + m_CameraParameter.SensorPxH / 2);
                m_ImagePoints[n].Y = (float)(matPointCameraCoordinate.At<float>(1, 0) / matPointCameraCoordinate.At<float>(2, 0) * m_CameraParameter.SensorPxV / m_CameraParameter.SensorSizeV * m_CameraParameter.f + m_CameraParameter.SensorPxV / 2);

                m_MatMoved3DPoints[n].Set(0, 0, matPointCameraCoordinate.At<float>(0, 0));
                m_MatMoved3DPoints[n].Set(1, 0, matPointCameraCoordinate.At<float>(1, 0));
                m_MatMoved3DPoints[n].Set(2, 0, matPointCameraCoordinate.At<float>(2, 0));

                matTranslation.Dispose();
                matRotate.Dispose();
                matMoved3DPoint.Dispose();
                matPointCameraCoordinate.Dispose();
            }
        }

        public void Calc2()
        {
            Mat matTranslation;  // Coverityチェック　例外パスだとリーク
            Mat matRotate;  // Coverityチェック　例外パスだとリーク
            Mat matMoved3DPoint;  // Coverityチェック　例外パスだとリーク
            Mat matPointCameraCoordinate;
            for (int n = 0; n < 4; n++)
            {
                matTranslation = m_MatTranslation * m_Mat3DPoints[n];
                matRotate = m_MatRz * (m_MatRy * (m_MatRx * matTranslation));

                matTranslation.Dispose();
                matTranslation = m_MatTranslation2 * matRotate;
                matRotate.Dispose();
                matRotate = m_MatRz2 * (m_MatRy2 * (m_MatRx2 * matTranslation));    // Coverityチェック　代入に失敗するとリーク

                // 次数の変換（1x4 -> 1x3）
                matMoved3DPoint = new Mat(3, 1, MatType.CV_32FC1);
                matMoved3DPoint.Set(0, 0, matRotate.At<float>(0, 0));
                matMoved3DPoint.Set(1, 0, matRotate.At<float>(1, 0));
                matMoved3DPoint.Set(2, 0, matRotate.At<float>(2, 0));
                matPointCameraCoordinate = matMoved3DPoint + m_MatShiftToCameraCoordinate;

                m_ImagePoints[n] = new Point2f();
                //m_ImagePoints[n].X = (float)(matPointCameraCoordinate.At<float>(0, 0) / matPointCameraCoordinate.At<float>(0, 2) * m_CameraParameter.SensorPxH / m_CameraParameter.SensorSizeH * m_CameraParameter.f + m_CameraParameter.SensorPxH / 2);
                //m_ImagePoints[n].Y = (float)(matPointCameraCoordinate.At<float>(1, 0) / matPointCameraCoordinate.At<float>(0, 2) * m_CameraParameter.SensorPxV / m_CameraParameter.SensorSizeV * m_CameraParameter.f + m_CameraParameter.SensorPxV / 2);
                m_ImagePoints[n].X = (float)(matPointCameraCoordinate.At<float>(0, 0) / matPointCameraCoordinate.At<float>(2, 0) * m_CameraParameter.SensorPxH / m_CameraParameter.SensorSizeH * m_CameraParameter.f + m_CameraParameter.SensorPxH / 2);
                m_ImagePoints[n].Y = (float)(matPointCameraCoordinate.At<float>(1, 0) / matPointCameraCoordinate.At<float>(2, 0) * m_CameraParameter.SensorPxV / m_CameraParameter.SensorSizeV * m_CameraParameter.f + m_CameraParameter.SensorPxV / 2);

                m_MatMoved3DPoints[n].Set(0, 0, matPointCameraCoordinate.At<float>(0, 0));
                m_MatMoved3DPoints[n].Set(1, 0, matPointCameraCoordinate.At<float>(1, 0));
                m_MatMoved3DPoints[n].Set(2, 0, matPointCameraCoordinate.At<float>(2, 0));

                matTranslation.Dispose();
                matRotate.Dispose();
                matMoved3DPoint.Dispose();
                matPointCameraCoordinate.Dispose();
            }
        }

        public void GetMoved3DPoints(int pos, out float X, out float Y, out float Z)
        {
            X = Y = Z = 0;
            X = m_MatMoved3DPoints[pos].At<float>(0, 0);
            Y = m_MatMoved3DPoints[pos].At<float>(1, 0);
            Z = m_MatMoved3DPoints[pos].At<float>(2, 0);
        }


    }
}
