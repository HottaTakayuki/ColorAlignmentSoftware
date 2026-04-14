using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SONY.Modules
{
	class ColMtx
	{
		Double[] m_ele = new Double[9];
		int m_type;

		public ColMtx()
		{
			m_type = -1;
			Array.Clear(m_ele, 0, m_ele.GetLength(0));
		}

		public ColMtx(ColMtx obj)
		{
			m_type = obj.m_type;
			for (Int32 i = 0; i < 9; i++)
				m_ele[i] = obj.m_ele[i];
		}

		public ColMtx(Double[] p)
		{
			newColMtx(p, 33);
		}

		public ColMtx(float[] p, Int32 offset)
		{
			double[] dp = new double[9];
			for (Int32 i = 0; i < 9; i++)
				dp[i] = p[i + offset];
			newColMtx(dp, 33);
		}

		public ColMtx(float[] p, int offset, int type)
		{
			double[] dp = new double[9];
			for (Int32 i = 0; i < 9; i++)
				dp[i] = p[i + offset];
			newColMtx(dp, type);
		}

		public ColMtx(Double[] p, int type)
		{
			newColMtx(p, type);
		}

		public ColMtx(Double e0, Double e1, Double e2, Double e3, Double e4, Double e5, Double e6, Double e7, Double e8)
		{
			Double[] tmp = new Double[9] { e0, e1, e2, e3, e4, e5, e6, e7, e8 };
			newColMtx(tmp, 33);
		}

		public ColMtx(Double e0, Double e1, Double e2, Double e3, Double e4, Double e5, Double e6, Double e7, Double e8, int type)
		{
			Double[] tmp = new Double[9] { e0, e1, e2, e3, e4, e5, e6, e7, e8 };
			newColMtx(tmp, type);
		}

		public ColMtx(Double e0, Double e1, Double e2)
		{
			Double[] tmp = new double[9] { e0, e1, e2, 0, 0, 0, 0, 0, 0 };
			newColMtx(tmp, 31);
		}

		void newColMtx(Double[] p, int type)
		{
			m_type = type;

			if (m_type == 33)
			{
				// 3x3
				for (int i = 0; i < 9; ++i)
				{ m_ele[i] = p[i]; } // floatなら暗黙のcast
			}
			else if (m_type == 31)
			{
				// 3x1
				for (int i = 0; i < 3; ++i)
				{ m_ele[i] = p[i]; } // floatなら暗黙のcast
			}
			else if (m_type == 1)
			{
				// xyR,xyG,xyB,YxyW
				ColMtx Mxy = new ColMtx(); // Y-1の色域matrix
				Mxy.m_type = 33;
				for (int i = 0; i < 3; ++i)
				{
					Mxy.m_ele[i * 3 + 0] = (p[i * 2 + 0] / p[i * 2 + 1]);
					Mxy.m_ele[i * 3 + 1] = 1.0;
					Mxy.m_ele[i * 3 + 2] = (1.0 - p[i * 2 + 0] - p[i * 2 + 1]) / p[i * 2 + 1];
				}

				ColMtx Mxyz = new ColMtx();
				Mxyz.m_type = 31;
				Mxyz.m_ele[0] = (p[7] / p[8]) * p[6];
				Mxyz.m_ele[1] = p[6];
				Mxyz.m_ele[2] = (1.0 - p[7] - p[8]) / p[8] * p[6];

				//
				// Ywht = Mxy * Mxyz
				//
				// Xw   Xr/Yr Xg/Yg Xb/Yb   Yr
				// Yw = Yr/Yr Yg/Yg Yb/Yb * Yg
				// Zw   Zr/Yr Zg/Yg Zb/Yb   Yb
				//

				ColMtx Ywht;
				Ywht = Mxy.Inv() * Mxyz;

				ColMtx Y = new ColMtx();
				Y.m_type = 33;
				Y.m_ele[0] = Ywht.m_ele[0];
				Y.m_ele[4] = Ywht.m_ele[1];
				Y.m_ele[8] = Ywht.m_ele[2];


				//
				// Xr/Yr Xg/Yg Xb/Yb   Yr 0 0   Xr Xg Xb
				// Yr/Yr Yg/Yg Yb/Yb * 0 Yg 0 = Xr Xg Xb
				// Zr/Yr Zg/Yg Zb/Yb   0 0 Yb   Xr Xg Xb
				//
				m_type = 33;
				ColMtx dummy = Mxy * Y;
				for (Int32 i = 0; i < 9; i++)
				{ m_ele[i] = dummy.m_ele[i]; }
			}
		}

		public void GetElement(float[] p, Int32 offset)
		{
			Double[] dp = new Double[9];
			GetElement(dp);
			for (Int32 i = 0; i < 9; i++)
				p[i + offset] = (float)dp[i];
		}

		public void GetElement(Double[] p)
		{
			if (m_type == 33)
			{
				// 3x3
				for (int i = 0; i < 9; ++i)
					p[i] = m_ele[i];
			}
			else if (m_type == 31)
			{
				// 3x1
				for (int i = 0; i < 3; ++i)
					p[i] = m_ele[i];
			}
			else
			{
				for (int i = 0; i < 3; ++i)
					p[i] = 0;
			}
		}

		public ColMtx Inv()
		{
			//  a0 a3 a6	-1
			// (a1 a4 a7)
			//  a2 a5 a8	
			double a11 = m_ele[0];
			double a21 = m_ele[1];
			double a31 = m_ele[2];
			double a12 = m_ele[3];
			double a22 = m_ele[4];
			double a32 = m_ele[5];
			double a13 = m_ele[6];
			double a23 = m_ele[7];
			double a33 = m_ele[8];

			double detA = a11 * a22 * a33 + a21 * a32 * a13 + a31 * a12 * a23 - a11 * a32 * a23 - a31 * a22 * a13 - a21 * a12 * a33;

			Double[] tmp_ele = new Double[9];
			tmp_ele[0] = (a22 * a33 - a23 * a32) / detA;
			tmp_ele[1] = (a23 * a31 - a21 * a33) / detA;
			tmp_ele[2] = (a21 * a32 - a22 * a31) / detA;
			tmp_ele[3] = (a13 * a32 - a12 * a33) / detA;
			tmp_ele[4] = (a11 * a33 - a13 * a31) / detA;
			tmp_ele[5] = (a12 * a31 - a11 * a32) / detA;
			tmp_ele[6] = (a12 * a23 - a13 * a22) / detA;
			tmp_ele[7] = (a13 * a21 - a11 * a23) / detA;
			tmp_ele[8] = (a11 * a22 - a12 * a21) / detA;

			ColMtx tmp = new ColMtx(tmp_ele);
			return tmp;
		}

		public ColMtx XYZtoYxy(bool bUV/* = false*/)
			//  X	    Y
			// (Y) -> (x)
			//  Z	    y
{
			// doubleで受けると型が決まっちゃうので、3,4,5をworkingとして使用。

			m_ele[3] = m_ele[0] + m_ele[1] + m_ele[2]; // X+Y+Z
			m_ele[4] = m_ele[0] / m_ele[3]; // x = X / (X+Y+Z)
			m_ele[5] = m_ele[1] / m_ele[3]; // y = Y / (X+Y+Z)

			m_ele[0] = m_ele[1]; // Y
			if (bUV) { // xy -> u'v'変換
				m_ele[1] = 4 * m_ele[4] / (-2 * m_ele[4] + 12 * m_ele[5] + 3); // u'
				m_ele[2] = 9 * m_ele[5] / (-2 * m_ele[4] + 12 * m_ele[5] + 3); // v'
			} else {
				m_ele[1] = m_ele[4]; // x
				m_ele[2] = m_ele[5]; // y
			}

			ColMtx tmp = new ColMtx(m_ele, 31);
			return tmp;
		}
        
		public static ColMtx operator *(ColMtx obj1, ColMtx obj2) {
			double[] tmp = new Double[9];

			if (obj1.m_type == 33 && obj2.m_type == 33) {
				// 3x3 * 3x3
				double a11 = obj1.m_ele[0];
				double a21 = obj1.m_ele[1];
				double a31 = obj1.m_ele[2];
				double a12 = obj1.m_ele[3];
				double a22 = obj1.m_ele[4];
				double a32 = obj1.m_ele[5];
				double a13 = obj1.m_ele[6];
				double a23 = obj1.m_ele[7];
				double a33 = obj1.m_ele[8];

				double b11 = obj2.m_ele[0];
				double b21 = obj2.m_ele[1];
				double b31 = obj2.m_ele[2];
				double b12 = obj2.m_ele[3];
				double b22 = obj2.m_ele[4];
				double b32 = obj2.m_ele[5];
				double b13 = obj2.m_ele[6];
				double b23 = obj2.m_ele[7];
				double b33 = obj2.m_ele[8];

				tmp[0] = a11 * b11 + a12 * b21 + a13 * b31;
				tmp[1] = a21 * b11 + a22 * b21 + a23 * b31;
				tmp[2] = a31 * b11 + a32 * b21 + a33 * b31;
				tmp[3] = a11 * b12 + a12 * b22 + a13 * b32;
				tmp[4] = a21 * b12 + a22 * b22 + a23 * b32;
				tmp[5] = a31 * b12 + a32 * b22 + a33 * b32;
				tmp[6] = a11 * b13 + a12 * b23 + a13 * b33;
				tmp[7] = a21 * b13 + a22 * b23 + a23 * b33;
				tmp[8] = a31 * b13 + a32 * b23 + a33 * b33;

				return new ColMtx(tmp, 33); ;
			} else if (obj1.m_type == 33 && obj2.m_type == 31) {
				// 3x3 * 3x1
				double a11 = obj1.m_ele[0];
				double a21 = obj1.m_ele[1];
				double a31 = obj1.m_ele[2];
				double a12 = obj1.m_ele[3];
				double a22 = obj1.m_ele[4];
				double a32 = obj1.m_ele[5];
				double a13 = obj1.m_ele[6];
				double a23 = obj1.m_ele[7];
				double a33 = obj1.m_ele[8];

				double b11 = obj2.m_ele[0];
				double b21 = obj2.m_ele[1];
				double b31 = obj2.m_ele[2];

				tmp[0] = a11 * b11 + a12 * b21 + a13 * b31;
				tmp[1] = a21 * b11 + a22 * b21 + a23 * b31;
				tmp[2] = a31 * b11 + a32 * b21 + a33 * b31;

				return new ColMtx(tmp, 31); ;
			} else {
				// error
				return new ColMtx(tmp, 33);
			}
		}

		public static ColMtx operator +(ColMtx obj1, ColMtx obj2) {
			double[] tmp = new double[9];

			if (obj1.m_type == 33 && obj2.m_type == 33) {
				// 3x3 + 3x3
				for (int i = 0; i < 9; ++i)
					tmp[i] = obj1.m_ele[i] + obj2.m_ele[i];
				return new ColMtx(tmp, 33);
			} else if (obj1.m_type == 31 && obj2.m_type == 31) {
				// 3x1 + 3x1
				for (int i = 0; i < 3; ++i)
					tmp[i] = obj1.m_ele[i] + obj2.m_ele[i];
				return new ColMtx(tmp, 31);
			} else {
				// error
				return new ColMtx(tmp, 33);
			}
		}

		public static ColMtx operator -(ColMtx obj1, ColMtx obj2) {
			double[] tmp = new double[9];

			if (obj1.m_type == 33 && obj2.m_type == 33) {
				// 3x3 + 3x3
				for (int i = 0; i < 9; ++i) tmp[i] = obj1.m_ele[i] - obj2.m_ele[i];
				return new ColMtx(tmp, 33);
			} else if (obj1.m_type == 31 && obj2.m_type == 31) {
				// 3x1 + 3x1
				for (int i = 0; i < 3; ++i)
					tmp[i] = obj1.m_ele[i] - obj2.m_ele[i];
				return new ColMtx(tmp, 31);
			} else {
				// error
				return new ColMtx(tmp, 33);
			}
		}

		public static ColMtx operator /(ColMtx obj, double a) {
			double[] tmp = new double[9];

			if (obj.m_type == 33) {
				// 3x3
				for (int i = 0; i < 9; ++i) tmp[i] = obj.m_ele[i] / a;
				return new ColMtx(tmp, 33);
			} else if (obj.m_type == 31) {
				// 3x1
				for (int i = 0; i < 3; ++i) tmp[i] = obj.m_ele[i] / a;
				return new ColMtx(tmp, 31);
			} else {
				// error
				return new ColMtx(tmp, 33);
			}
		}

        public ColMtx Transpose()
        {
            double[] a = new double[9];

            a[0] = m_ele[0];
            a[1] = m_ele[3];
            a[2] = m_ele[6];
            a[3] = m_ele[1];
            a[4] = m_ele[4];
            a[5] = m_ele[7];
            a[6] = m_ele[2];
            a[7] = m_ele[5];
            a[8] = m_ele[8];

            ColMtx tmp = new ColMtx(a);
            return tmp;
        }
	}
}
