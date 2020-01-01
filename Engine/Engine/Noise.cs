using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
static class Noise
{
	private static float STRETCH_CONSTANT_2D = -0.2113248f;
	private static float SQUISH_CONSTANT_2D = 0.3660254f;
	private static float STRETCH_CONSTANT_3D = -1f / 6f;
	private static float SQUISH_CONSTANT_3D = 1f / 3f;
	private static float NORM_CONSTANT_2D = 47f;
	private static float NORM_CONSTANT_3D = 103f;
	private static short[] perm;
	private static short[] permGradIndex3D;
	private static sbyte[] gradients2D = new sbyte[] {
			 5,  2,    2,  5,
			-5,  2,   -2,  5,
			 5, -2,    2, -5,
			-5, -2,   -2, -5,
		};
	private static sbyte[] gradients3D = new sbyte[] {
			-11,  4,  4,     -4,  11,  4,    -4,  4,  11,
			 11,  4,  4,      4,  11,  4,     4,  4,  11,
			-11, -4,  4,     -4, -11,  4,    -4, -4,  11,
			 11, -4,  4,      4, -11,  4,     4, -4,  11,
			-11,  4, -4,     -4,  11, -4,    -4,  4, -11,
			 11,  4, -4,      4,  11, -4,     4,  4, -11,
			-11, -4, -4,     -4, -11, -4,    -4, -4, -11,
			 11, -4, -4,      4, -11, -4,     4, -4, -11,
		};
	public static void Seed(long seed = 0)
	{
		perm = new short[256];
		permGradIndex3D = new short[256];
		short[] source = new short[256];
		for (short i = 0; i < 256; i++)
			source[i] = i;
		seed = seed * 6364136223846793005 + 1442695040888963407;
		seed = seed * 6364136223846793005 + 1442695040888963407;
		seed = seed * 6364136223846793005 + 1442695040888963407;
		for (int i = 255; i >= 0; i--)
		{
			seed = seed * 6364136223846793005 + 1442695040888963407;
			int r = (int)((seed + 31) % (i + 1));
			if (r < 0) r += (i + 1);
			perm[i] = source[r];
			permGradIndex3D[i] = (short)((perm[i] % (gradients3D.Length / 3)) * 3);
			source[r] = source[i];
		}
	}
	public static float Eval(float x, float y)
	{
		float stretchOffset = (x + y) * STRETCH_CONSTANT_2D;
		float xs = x + stretchOffset;
		float ys = y + stretchOffset;
		int xsb = FastFloor(xs);
		int ysb = FastFloor(ys);
		float squishOffset = (xsb + ysb) * SQUISH_CONSTANT_2D;
		float xb = xsb + squishOffset;
		float yb = ysb + squishOffset;
		float xins = xs - xsb;
		float yins = ys - ysb;
		float inSum = xins + yins;
		float dx0 = x - xb;
		float dy0 = y - yb;
		float dx_ext, dy_ext;
		int xsv_ext, ysv_ext;
		float value = 0;
		float dx1 = dx0 - 1 - SQUISH_CONSTANT_2D;
		float dy1 = dy0 - 0 - SQUISH_CONSTANT_2D;
		float attn1 = 2 - dx1 * dx1 - dy1 * dy1;
		if (attn1 > 0)
		{
			attn1 *= attn1;
			value += attn1 * attn1 * Extrapolate(xsb + 1, ysb + 0, dx1, dy1);
		}
		float dx2 = dx0 - 0 - SQUISH_CONSTANT_2D;
		float dy2 = dy0 - 1 - SQUISH_CONSTANT_2D;
		float attn2 = 2 - dx2 * dx2 - dy2 * dy2;
		if (attn2 > 0)
		{
			attn2 *= attn2;
			value += attn2 * attn2 * Extrapolate(xsb + 0, ysb + 1, dx2, dy2);
		}
		if (inSum <= 1)
		{
			float zins = 1 - inSum;
			if (zins > xins || zins > yins)
			{
				if (xins > yins)
				{
					xsv_ext = xsb + 1;
					ysv_ext = ysb - 1;
					dx_ext = dx0 - 1;
					dy_ext = dy0 + 1;
				}
				else
				{
					xsv_ext = xsb - 1;
					ysv_ext = ysb + 1;
					dx_ext = dx0 + 1;
					dy_ext = dy0 - 1;
				}
			}
			else
			{
				xsv_ext = xsb + 1;
				ysv_ext = ysb + 1;
				dx_ext = dx0 - 1 - 2 * SQUISH_CONSTANT_2D;
				dy_ext = dy0 - 1 - 2 * SQUISH_CONSTANT_2D;
			}
		}
		else
		{
			float zins = 2 - inSum;
			if (zins < xins || zins < yins)
			{
				if (xins > yins)
				{
					xsv_ext = xsb + 2;
					ysv_ext = ysb + 0;
					dx_ext = dx0 - 2 - 2 * SQUISH_CONSTANT_2D;
					dy_ext = dy0 + 0 - 2 * SQUISH_CONSTANT_2D;
				}
				else
				{
					xsv_ext = xsb + 0;
					ysv_ext = ysb + 2;
					dx_ext = dx0 + 0 - 2 * SQUISH_CONSTANT_2D;
					dy_ext = dy0 - 2 - 2 * SQUISH_CONSTANT_2D;
				}
			}
			else
			{
				dx_ext = dx0;
				dy_ext = dy0;
				xsv_ext = xsb;
				ysv_ext = ysb;
			}
			xsb += 1;
			ysb += 1;
			dx0 = dx0 - 1 - 2 * SQUISH_CONSTANT_2D;
			dy0 = dy0 - 1 - 2 * SQUISH_CONSTANT_2D;
		}
		float attn0 = 2 - dx0 * dx0 - dy0 * dy0;
		if (attn0 > 0)
		{
			attn0 *= attn0;
			value += attn0 * attn0 * Extrapolate(xsb, ysb, dx0, dy0);
		}
		float attn_ext = 2 - dx_ext * dx_ext - dy_ext * dy_ext;
		if (attn_ext > 0)
		{
			attn_ext *= attn_ext;
			value += attn_ext * attn_ext * Extrapolate(xsv_ext, ysv_ext, dx_ext, dy_ext);
		}
		return ((value / NORM_CONSTANT_2D) + 1f) * 0.5f;
	}
	public static float Eval(float x, float y, float z)
	{
		float stretchOffset = (x + y + z) * STRETCH_CONSTANT_3D;
		float xs = x + stretchOffset;
		float ys = y + stretchOffset;
		float zs = z + stretchOffset;
		int xsb = FastFloor(xs);
		int ysb = FastFloor(ys);
		int zsb = FastFloor(zs);
		float squishOffset = (xsb + ysb + zsb) * SQUISH_CONSTANT_3D;
		float xb = xsb + squishOffset;
		float yb = ysb + squishOffset;
		float zb = zsb + squishOffset;
		float xins = xs - xsb;
		float yins = ys - ysb;
		float zins = zs - zsb;
		float inSum = xins + yins + zins;
		float dx0 = x - xb;
		float dy0 = y - yb;
		float dz0 = z - zb;
		float dx_ext0, dy_ext0, dz_ext0;
		float dx_ext1, dy_ext1, dz_ext1;
		int xsv_ext0, ysv_ext0, zsv_ext0;
		int xsv_ext1, ysv_ext1, zsv_ext1;

		float value = 0;
		if (inSum <= 1)
		{
			byte aPoint = 0x01;
			float aScore = xins;
			byte bPoint = 0x02;
			float bScore = yins;
			if (aScore >= bScore && zins > bScore)
			{
				bScore = zins;
				bPoint = 0x04;
			}
			else if (aScore < bScore && zins > aScore)
			{
				aScore = zins;
				aPoint = 0x04;
			}
			float wins = 1 - inSum;
			if (wins > aScore || wins > bScore)
			{
				byte c = (bScore > aScore ? bPoint : aPoint);
				if ((c & 0x01) == 0)
				{
					xsv_ext0 = xsb - 1;
					xsv_ext1 = xsb;
					dx_ext0 = dx0 + 1;
					dx_ext1 = dx0;
				}
				else
				{
					xsv_ext0 = xsv_ext1 = xsb + 1;
					dx_ext0 = dx_ext1 = dx0 - 1;
				}
				if ((c & 0x02) == 0)
				{
					ysv_ext0 = ysv_ext1 = ysb;
					dy_ext0 = dy_ext1 = dy0;
					if ((c & 0x01) == 0)
					{
						ysv_ext1 -= 1;
						dy_ext1 += 1;
					}
					else
					{
						ysv_ext0 -= 1;
						dy_ext0 += 1;
					}
				}
				else
				{
					ysv_ext0 = ysv_ext1 = ysb + 1;
					dy_ext0 = dy_ext1 = dy0 - 1;
				}
				if ((c & 0x04) == 0)
				{
					zsv_ext0 = zsb;
					zsv_ext1 = zsb - 1;
					dz_ext0 = dz0;
					dz_ext1 = dz0 + 1;
				}
				else
				{
					zsv_ext0 = zsv_ext1 = zsb + 1;
					dz_ext0 = dz_ext1 = dz0 - 1;
				}
			}
			else
			{
				byte c = (byte)(aPoint | bPoint);
				if ((c & 0x01) == 0)
				{
					xsv_ext0 = xsb;
					xsv_ext1 = xsb - 1;
					dx_ext0 = dx0 - 2 * SQUISH_CONSTANT_3D;
					dx_ext1 = dx0 + 1 - SQUISH_CONSTANT_3D;
				}
				else
				{
					xsv_ext0 = xsv_ext1 = xsb + 1;
					dx_ext0 = dx0 - 1 - 2 * SQUISH_CONSTANT_3D;
					dx_ext1 = dx0 - 1 - SQUISH_CONSTANT_3D;
				}
				if ((c & 0x02) == 0)
				{
					ysv_ext0 = ysb;
					ysv_ext1 = ysb - 1;
					dy_ext0 = dy0 - 2 * SQUISH_CONSTANT_3D;
					dy_ext1 = dy0 + 1 - SQUISH_CONSTANT_3D;
				}
				else
				{
					ysv_ext0 = ysv_ext1 = ysb + 1;
					dy_ext0 = dy0 - 1 - 2 * SQUISH_CONSTANT_3D;
					dy_ext1 = dy0 - 1 - SQUISH_CONSTANT_3D;
				}
				if ((c & 0x04) == 0)
				{
					zsv_ext0 = zsb;
					zsv_ext1 = zsb - 1;
					dz_ext0 = dz0 - 2 * SQUISH_CONSTANT_3D;
					dz_ext1 = dz0 + 1 - SQUISH_CONSTANT_3D;
				}
				else
				{
					zsv_ext0 = zsv_ext1 = zsb + 1;
					dz_ext0 = dz0 - 1 - 2 * SQUISH_CONSTANT_3D;
					dz_ext1 = dz0 - 1 - SQUISH_CONSTANT_3D;
				}
			}
			float attn0 = 2 - dx0 * dx0 - dy0 * dy0 - dz0 * dz0;
			if (attn0 > 0)
			{
				attn0 *= attn0;
				value += attn0 * attn0 * Extrapolate(xsb + 0, ysb + 0, zsb + 0, dx0, dy0, dz0);
			}
			float dx1 = dx0 - 1 - SQUISH_CONSTANT_3D;
			float dy1 = dy0 - 0 - SQUISH_CONSTANT_3D;
			float dz1 = dz0 - 0 - SQUISH_CONSTANT_3D;
			float attn1 = 2 - dx1 * dx1 - dy1 * dy1 - dz1 * dz1;
			if (attn1 > 0)
			{
				attn1 *= attn1;
				value += attn1 * attn1 * Extrapolate(xsb + 1, ysb + 0, zsb + 0, dx1, dy1, dz1);
			}
			float dx2 = dx0 - 0 - SQUISH_CONSTANT_3D;
			float dy2 = dy0 - 1 - SQUISH_CONSTANT_3D;
			float dz2 = dz1;
			float attn2 = 2 - dx2 * dx2 - dy2 * dy2 - dz2 * dz2;
			if (attn2 > 0)
			{
				attn2 *= attn2;
				value += attn2 * attn2 * Extrapolate(xsb + 0, ysb + 1, zsb + 0, dx2, dy2, dz2);
			}
			float dx3 = dx2;
			float dy3 = dy1;
			float dz3 = dz0 - 1 - SQUISH_CONSTANT_3D;
			float attn3 = 2 - dx3 * dx3 - dy3 * dy3 - dz3 * dz3;
			if (attn3 > 0)
			{
				attn3 *= attn3;
				value += attn3 * attn3 * Extrapolate(xsb + 0, ysb + 0, zsb + 1, dx3, dy3, dz3);
			}
		}
		else if (inSum >= 2)
		{
			byte aPoint = 0x06;
			float aScore = xins;
			byte bPoint = 0x05;
			float bScore = yins;
			if (aScore <= bScore && zins < bScore)
			{
				bScore = zins;
				bPoint = 0x03;
			}
			else if (aScore > bScore && zins < aScore)
			{
				aScore = zins;
				aPoint = 0x03;
			}
			float wins = 3 - inSum;
			if (wins < aScore || wins < bScore)
			{
				byte c = (bScore < aScore ? bPoint : aPoint);
				if ((c & 0x01) != 0)
				{
					xsv_ext0 = xsb + 2;
					xsv_ext1 = xsb + 1;
					dx_ext0 = dx0 - 2 - 3 * SQUISH_CONSTANT_3D;
					dx_ext1 = dx0 - 1 - 3 * SQUISH_CONSTANT_3D;
				}
				else
				{
					xsv_ext0 = xsv_ext1 = xsb;
					dx_ext0 = dx_ext1 = dx0 - 3 * SQUISH_CONSTANT_3D;
				}
				if ((c & 0x02) != 0)
				{
					ysv_ext0 = ysv_ext1 = ysb + 1;
					dy_ext0 = dy_ext1 = dy0 - 1 - 3 * SQUISH_CONSTANT_3D;
					if ((c & 0x01) != 0)
					{
						ysv_ext1 += 1;
						dy_ext1 -= 1;
					}
					else
					{
						ysv_ext0 += 1;
						dy_ext0 -= 1;
					}
				}
				else
				{
					ysv_ext0 = ysv_ext1 = ysb;
					dy_ext0 = dy_ext1 = dy0 - 3 * SQUISH_CONSTANT_3D;
				}
				if ((c & 0x04) != 0)
				{
					zsv_ext0 = zsb + 1;
					zsv_ext1 = zsb + 2;
					dz_ext0 = dz0 - 1 - 3 * SQUISH_CONSTANT_3D;
					dz_ext1 = dz0 - 2 - 3 * SQUISH_CONSTANT_3D;
				}
				else
				{
					zsv_ext0 = zsv_ext1 = zsb;
					dz_ext0 = dz_ext1 = dz0 - 3 * SQUISH_CONSTANT_3D;
				}
			}
			else
			{
				byte c = (byte)(aPoint & bPoint);

				if ((c & 0x01) != 0)
				{
					xsv_ext0 = xsb + 1;
					xsv_ext1 = xsb + 2;
					dx_ext0 = dx0 - 1 - SQUISH_CONSTANT_3D;
					dx_ext1 = dx0 - 2 - 2 * SQUISH_CONSTANT_3D;
				}
				else
				{
					xsv_ext0 = xsv_ext1 = xsb;
					dx_ext0 = dx0 - SQUISH_CONSTANT_3D;
					dx_ext1 = dx0 - 2 * SQUISH_CONSTANT_3D;
				}
				if ((c & 0x02) != 0)
				{
					ysv_ext0 = ysb + 1;
					ysv_ext1 = ysb + 2;
					dy_ext0 = dy0 - 1 - SQUISH_CONSTANT_3D;
					dy_ext1 = dy0 - 2 - 2 * SQUISH_CONSTANT_3D;
				}
				else
				{
					ysv_ext0 = ysv_ext1 = ysb;
					dy_ext0 = dy0 - SQUISH_CONSTANT_3D;
					dy_ext1 = dy0 - 2 * SQUISH_CONSTANT_3D;
				}
				if ((c & 0x04) != 0)
				{
					zsv_ext0 = zsb + 1;
					zsv_ext1 = zsb + 2;
					dz_ext0 = dz0 - 1 - SQUISH_CONSTANT_3D;
					dz_ext1 = dz0 - 2 - 2 * SQUISH_CONSTANT_3D;
				}
				else
				{
					zsv_ext0 = zsv_ext1 = zsb;
					dz_ext0 = dz0 - SQUISH_CONSTANT_3D;
					dz_ext1 = dz0 - 2 * SQUISH_CONSTANT_3D;
				}
			}
			float dx3 = dx0 - 1 - 2 * SQUISH_CONSTANT_3D;
			float dy3 = dy0 - 1 - 2 * SQUISH_CONSTANT_3D;
			float dz3 = dz0 - 0 - 2 * SQUISH_CONSTANT_3D;
			float attn3 = 2 - dx3 * dx3 - dy3 * dy3 - dz3 * dz3;
			if (attn3 > 0)
			{
				attn3 *= attn3;
				value += attn3 * attn3 * Extrapolate(xsb + 1, ysb + 1, zsb + 0, dx3, dy3, dz3);
			}
			float dx2 = dx3;
			float dy2 = dy0 - 0 - 2 * SQUISH_CONSTANT_3D;
			float dz2 = dz0 - 1 - 2 * SQUISH_CONSTANT_3D;
			float attn2 = 2 - dx2 * dx2 - dy2 * dy2 - dz2 * dz2;
			if (attn2 > 0)
			{
				attn2 *= attn2;
				value += attn2 * attn2 * Extrapolate(xsb + 1, ysb + 0, zsb + 1, dx2, dy2, dz2);
			}
			float dx1 = dx0 - 0 - 2 * SQUISH_CONSTANT_3D;
			float dy1 = dy3;
			float dz1 = dz2;
			float attn1 = 2 - dx1 * dx1 - dy1 * dy1 - dz1 * dz1;
			if (attn1 > 0)
			{
				attn1 *= attn1;
				value += attn1 * attn1 * Extrapolate(xsb + 0, ysb + 1, zsb + 1, dx1, dy1, dz1);
			}
			dx0 = dx0 - 1 - 3 * SQUISH_CONSTANT_3D;
			dy0 = dy0 - 1 - 3 * SQUISH_CONSTANT_3D;
			dz0 = dz0 - 1 - 3 * SQUISH_CONSTANT_3D;
			float attn0 = 2 - dx0 * dx0 - dy0 * dy0 - dz0 * dz0;
			if (attn0 > 0)
			{
				attn0 *= attn0;
				value += attn0 * attn0 * Extrapolate(xsb + 1, ysb + 1, zsb + 1, dx0, dy0, dz0);
			}
		}
		else
		{
			float aScore;
			byte aPoint;
			bool aIsFurtherSide;
			float bScore;
			byte bPoint;
			bool bIsFurtherSide;
			float p1 = xins + yins;
			if (p1 > 1)
			{
				aScore = p1 - 1;
				aPoint = 0x03;
				aIsFurtherSide = true;
			}
			else
			{
				aScore = 1 - p1;
				aPoint = 0x04;
				aIsFurtherSide = false;
			}
			float p2 = xins + zins;
			if (p2 > 1)
			{
				bScore = p2 - 1;
				bPoint = 0x05;
				bIsFurtherSide = true;
			}
			else
			{
				bScore = 1 - p2;
				bPoint = 0x02;
				bIsFurtherSide = false;
			}
			float p3 = yins + zins;
			if (p3 > 1)
			{
				float score = p3 - 1;
				if (aScore <= bScore && aScore < score)
				{
					aScore = score;
					aPoint = 0x06;
					aIsFurtherSide = true;
				}
				else if (aScore > bScore && bScore < score)
				{
					bScore = score;
					bPoint = 0x06;
					bIsFurtherSide = true;
				}
			}
			else
			{
				float score = 1 - p3;
				if (aScore <= bScore && aScore < score)
				{
					aScore = score;
					aPoint = 0x01;
					aIsFurtherSide = false;
				}
				else if (aScore > bScore && bScore < score)
				{
					bScore = score;
					bPoint = 0x01;
					bIsFurtherSide = false;
				}
			}
			if (aIsFurtherSide == bIsFurtherSide)
			{
				if (aIsFurtherSide)
				{
					dx_ext0 = dx0 - 1 - 3 * SQUISH_CONSTANT_3D;
					dy_ext0 = dy0 - 1 - 3 * SQUISH_CONSTANT_3D;
					dz_ext0 = dz0 - 1 - 3 * SQUISH_CONSTANT_3D;
					xsv_ext0 = xsb + 1;
					ysv_ext0 = ysb + 1;
					zsv_ext0 = zsb + 1;

					byte c = (byte)(aPoint & bPoint);
					if ((c & 0x01) != 0)
					{
						dx_ext1 = dx0 - 2 - 2 * SQUISH_CONSTANT_3D;
						dy_ext1 = dy0 - 2 * SQUISH_CONSTANT_3D;
						dz_ext1 = dz0 - 2 * SQUISH_CONSTANT_3D;
						xsv_ext1 = xsb + 2;
						ysv_ext1 = ysb;
						zsv_ext1 = zsb;
					}
					else if ((c & 0x02) != 0)
					{
						dx_ext1 = dx0 - 2 * SQUISH_CONSTANT_3D;
						dy_ext1 = dy0 - 2 - 2 * SQUISH_CONSTANT_3D;
						dz_ext1 = dz0 - 2 * SQUISH_CONSTANT_3D;
						xsv_ext1 = xsb;
						ysv_ext1 = ysb + 2;
						zsv_ext1 = zsb;
					}
					else
					{
						dx_ext1 = dx0 - 2 * SQUISH_CONSTANT_3D;
						dy_ext1 = dy0 - 2 * SQUISH_CONSTANT_3D;
						dz_ext1 = dz0 - 2 - 2 * SQUISH_CONSTANT_3D;
						xsv_ext1 = xsb;
						ysv_ext1 = ysb;
						zsv_ext1 = zsb + 2;
					}
				}
				else
				{
					dx_ext0 = dx0;
					dy_ext0 = dy0;
					dz_ext0 = dz0;
					xsv_ext0 = xsb;
					ysv_ext0 = ysb;
					zsv_ext0 = zsb;
					byte c = (byte)(aPoint | bPoint);
					if ((c & 0x01) == 0)
					{
						dx_ext1 = dx0 + 1 - SQUISH_CONSTANT_3D;
						dy_ext1 = dy0 - 1 - SQUISH_CONSTANT_3D;
						dz_ext1 = dz0 - 1 - SQUISH_CONSTANT_3D;
						xsv_ext1 = xsb - 1;
						ysv_ext1 = ysb + 1;
						zsv_ext1 = zsb + 1;
					}
					else if ((c & 0x02) == 0)
					{
						dx_ext1 = dx0 - 1 - SQUISH_CONSTANT_3D;
						dy_ext1 = dy0 + 1 - SQUISH_CONSTANT_3D;
						dz_ext1 = dz0 - 1 - SQUISH_CONSTANT_3D;
						xsv_ext1 = xsb + 1;
						ysv_ext1 = ysb - 1;
						zsv_ext1 = zsb + 1;
					}
					else
					{
						dx_ext1 = dx0 - 1 - SQUISH_CONSTANT_3D;
						dy_ext1 = dy0 - 1 - SQUISH_CONSTANT_3D;
						dz_ext1 = dz0 + 1 - SQUISH_CONSTANT_3D;
						xsv_ext1 = xsb + 1;
						ysv_ext1 = ysb + 1;
						zsv_ext1 = zsb - 1;
					}
				}
			}
			else
			{
				byte c1, c2;
				if (aIsFurtherSide)
				{
					c1 = aPoint;
					c2 = bPoint;
				}
				else
				{
					c1 = bPoint;
					c2 = aPoint;
				}
				if ((c1 & 0x01) == 0)
				{
					dx_ext0 = dx0 + 1 - SQUISH_CONSTANT_3D;
					dy_ext0 = dy0 - 1 - SQUISH_CONSTANT_3D;
					dz_ext0 = dz0 - 1 - SQUISH_CONSTANT_3D;
					xsv_ext0 = xsb - 1;
					ysv_ext0 = ysb + 1;
					zsv_ext0 = zsb + 1;
				}
				else if ((c1 & 0x02) == 0)
				{
					dx_ext0 = dx0 - 1 - SQUISH_CONSTANT_3D;
					dy_ext0 = dy0 + 1 - SQUISH_CONSTANT_3D;
					dz_ext0 = dz0 - 1 - SQUISH_CONSTANT_3D;
					xsv_ext0 = xsb + 1;
					ysv_ext0 = ysb - 1;
					zsv_ext0 = zsb + 1;
				}
				else
				{
					dx_ext0 = dx0 - 1 - SQUISH_CONSTANT_3D;
					dy_ext0 = dy0 - 1 - SQUISH_CONSTANT_3D;
					dz_ext0 = dz0 + 1 - SQUISH_CONSTANT_3D;
					xsv_ext0 = xsb + 1;
					ysv_ext0 = ysb + 1;
					zsv_ext0 = zsb - 1;
				}
				dx_ext1 = dx0 - 2 * SQUISH_CONSTANT_3D;
				dy_ext1 = dy0 - 2 * SQUISH_CONSTANT_3D;
				dz_ext1 = dz0 - 2 * SQUISH_CONSTANT_3D;
				xsv_ext1 = xsb;
				ysv_ext1 = ysb;
				zsv_ext1 = zsb;
				if ((c2 & 0x01) != 0)
				{
					dx_ext1 -= 2;
					xsv_ext1 += 2;
				}
				else if ((c2 & 0x02) != 0)
				{
					dy_ext1 -= 2;
					ysv_ext1 += 2;
				}
				else
				{
					dz_ext1 -= 2;
					zsv_ext1 += 2;
				}
			}
			float dx1 = dx0 - 1 - SQUISH_CONSTANT_3D;
			float dy1 = dy0 - 0 - SQUISH_CONSTANT_3D;
			float dz1 = dz0 - 0 - SQUISH_CONSTANT_3D;
			float attn1 = 2 - dx1 * dx1 - dy1 * dy1 - dz1 * dz1;
			if (attn1 > 0)
			{
				attn1 *= attn1;
				value += attn1 * attn1 * Extrapolate(xsb + 1, ysb + 0, zsb + 0, dx1, dy1, dz1);
			}
			float dx2 = dx0 - 0 - SQUISH_CONSTANT_3D;
			float dy2 = dy0 - 1 - SQUISH_CONSTANT_3D;
			float dz2 = dz1;
			float attn2 = 2 - dx2 * dx2 - dy2 * dy2 - dz2 * dz2;
			if (attn2 > 0)
			{
				attn2 *= attn2;
				value += attn2 * attn2 * Extrapolate(xsb + 0, ysb + 1, zsb + 0, dx2, dy2, dz2);
			}
			float dx3 = dx2;
			float dy3 = dy1;
			float dz3 = dz0 - 1 - SQUISH_CONSTANT_3D;
			float attn3 = 2 - dx3 * dx3 - dy3 * dy3 - dz3 * dz3;
			if (attn3 > 0)
			{
				attn3 *= attn3;
				value += attn3 * attn3 * Extrapolate(xsb + 0, ysb + 0, zsb + 1, dx3, dy3, dz3);
			}
			float dx4 = dx0 - 1 - 2 * SQUISH_CONSTANT_3D;
			float dy4 = dy0 - 1 - 2 * SQUISH_CONSTANT_3D;
			float dz4 = dz0 - 0 - 2 * SQUISH_CONSTANT_3D;
			float attn4 = 2 - dx4 * dx4 - dy4 * dy4 - dz4 * dz4;
			if (attn4 > 0)
			{
				attn4 *= attn4;
				value += attn4 * attn4 * Extrapolate(xsb + 1, ysb + 1, zsb + 0, dx4, dy4, dz4);
			}
			float dx5 = dx4;
			float dy5 = dy0 - 0 - 2 * SQUISH_CONSTANT_3D;
			float dz5 = dz0 - 1 - 2 * SQUISH_CONSTANT_3D;
			float attn5 = 2 - dx5 * dx5 - dy5 * dy5 - dz5 * dz5;
			if (attn5 > 0)
			{
				attn5 *= attn5;
				value += attn5 * attn5 * Extrapolate(xsb + 1, ysb + 0, zsb + 1, dx5, dy5, dz5);
			}
			float dx6 = dx0 - 0 - 2 * SQUISH_CONSTANT_3D;
			float dy6 = dy4;
			float dz6 = dz5;
			float attn6 = 2 - dx6 * dx6 - dy6 * dy6 - dz6 * dz6;
			if (attn6 > 0)
			{
				attn6 *= attn6;
				value += attn6 * attn6 * Extrapolate(xsb + 0, ysb + 1, zsb + 1, dx6, dy6, dz6);
			}
		}
		float attn_ext0 = 2 - dx_ext0 * dx_ext0 - dy_ext0 * dy_ext0 - dz_ext0 * dz_ext0;
		if (attn_ext0 > 0)
		{
			attn_ext0 *= attn_ext0;
			value += attn_ext0 * attn_ext0 * Extrapolate(xsv_ext0, ysv_ext0, zsv_ext0, dx_ext0, dy_ext0, dz_ext0);
		}
		float attn_ext1 = 2 - dx_ext1 * dx_ext1 - dy_ext1 * dy_ext1 - dz_ext1 * dz_ext1;
		if (attn_ext1 > 0)
		{
			attn_ext1 *= attn_ext1;
			value += attn_ext1 * attn_ext1 * Extrapolate(xsv_ext1, ysv_ext1, zsv_ext1, dx_ext1, dy_ext1, dz_ext1);
		}
		return ((value / NORM_CONSTANT_3D) + 1f) * 0.5f;
	}
	private static float Extrapolate(int xsb, int ysb, float dx, float dy)
	{
		int index = perm[(perm[xsb & 0xFF] + ysb) & 0xFF] & 0x0E;
		return gradients2D[index] * dx + gradients2D[index + 1] * dy;
	}
	private static float Extrapolate(int xsb, int ysb, int zsb, float dx, float dy, float dz)
	{
		int index = permGradIndex3D[(perm[(perm[xsb & 0xFF] + ysb) & 0xFF] + zsb) & 0xFF];
		return gradients3D[index] * dx + gradients3D[index + 1] * dy + gradients3D[index + 2] * dz;
	}
	private static int FastFloor(float x)
	{
		int xi = (int)x;
		return x < xi ? xi - 1 : xi;
	}
}
