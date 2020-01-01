﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
class FormGameEngine
{
    public static Random rnd = new Random();
    public string AppName;
    public Bitmap IconImage;
    public Graphics gCanvas;
    private Bitmap bCanvas;
    private Timer timer;
    private PictureBox Canvas;
    private Form form;
    private int nScreenWidth;
    private int nScreenHeight;
    private float fpsTime;
    private int fps;
    public void Construct(Form thisform, int ScreenWidth, int ScreenHeight, bool fullscreen = false)
    {
        if (ScreenWidth < 100 || ScreenHeight < 10)
        {
            ScreenWidth = 640;
            ScreenHeight = 480;
        }

        form = thisform;
        form.Text = "";
        form.StartPosition = FormStartPosition.CenterScreen;
        if (fullscreen)
        {
            form.FormBorderStyle = FormBorderStyle.None;
            form.WindowState = FormWindowState.Maximized;
            form.Bounds = Screen.PrimaryScreen.Bounds;
            nScreenWidth = form.Width;
            nScreenHeight = form.Height;
        }
        else
        {
            nScreenWidth = ScreenWidth;
            nScreenHeight = ScreenHeight;
            form.Width = nScreenWidth + 16;
            form.Height = nScreenHeight + 39;
            form.MaximumSize = new Size(form.Width, form.Height);
            form.MinimumSize = new Size(form.Width, form.Height);
        }

        Canvas = new PictureBox();
        Canvas.BackColor = Color.Black;
        Canvas.Dock = DockStyle.Fill;
        form.Controls.Add(Canvas);

        bCanvas = new Bitmap(Canvas.Width, Canvas.Height);
        gCanvas = Graphics.FromImage(bCanvas);
        Input.LinkReferences(form, Canvas);
        Time.Start();

        timer = new Timer();
        timer.Interval = 1;
        timer.Start();
        //timer.Tick += Update;
        Application.Idle += Update;
        if (IconImage != null) form.Icon = Icon.FromHandle(IconImage.GetHicon());
        OnUserCreate();
    }
    private void Update(object sender, EventArgs e)
    {
        Time.Calculate();
        fpsTime += Time.fElapsedTime;
        if (fpsTime >= 0.5f)
        {
            fpsTime = 0f;
            fps = (int)(1f / Time.fElapsedTime);
        }
        form.Text = AppName + " : " + fps;
        OnUserUpdate(Time.fElapsedTime);
        Canvas.Image = bCanvas;
        Input.UpdateKeys();
    }
    public virtual void OnUserCreate() { }
    public virtual void OnUserUpdate(float fElapsedTime) { }
    public int ScreenWidth()
    {
        return nScreenWidth;
    }
    public int ScreenHeight()
    {
        return nScreenHeight;
    }
    public bool Focused()
    {
        return form.Focused;
    }
    public void Exit()
    {
        Application.Exit();
    }
    //public unsafe void UpdatePixels(byte[] pixels)
    //{
    //    BitmapData bd = bCanvas.LockBits(new Rectangle(0, 0, nScreenWidth, nScreenHeight), ImageLockMode.ReadWrite, bCanvas.PixelFormat);
    //    byte* ptrFirstPixel = (byte*)bd.Scan0;
    //    for (int y = 0; y < nScreenHeight; y++)
    //    {
    //        byte* currentLine = ptrFirstPixel + (y * bd.Stride);
    //        for (int x = 0; x < nScreenWidth * 4; x += 4)
    //        {
    //            currentLine[x + 0] = pixels[y * nScreenWidth * 4 + x + 0];
    //            currentLine[x + 1] = pixels[y * nScreenWidth * 4 + x + 1];
    //            currentLine[x + 2] = pixels[y * nScreenWidth * 4 + x + 2];
    //            currentLine[x + 3] = pixels[y * nScreenWidth * 4 + x + 3];
    //        }
    //    }
    //    bCanvas.UnlockBits(bd);
    //}
    public void SaveFrame(string locationpath, string name, string format)
    {
        ImageFormat imf;
        format.ToLower();
        switch (format)
        {
            case "png": imf = ImageFormat.Png; break;
            case "bmp": imf = ImageFormat.Bmp; break;
            case "gif": imf = ImageFormat.Gif; break;
            case "jpeg": imf = ImageFormat.Jpeg; break;
            case "tiff": imf = ImageFormat.Tiff; break;
            default: imf = ImageFormat.Bmp; format = "bmp"; break;
        }
        bCanvas.Save($"{locationpath}/{name}.{format}", imf);
    }
}
class Sound
{
    [DllImport("winmm.dll")]
    private static extern Int32 mciSendString(string command, StringBuilder buffer, int bufferSize, IntPtr hwndCallback);
    private string SoundFile;
    public Sound(string filepath)
    {
        SoundFile = filepath;
    }
    public void Play()
    {
        mciSendString("stop " + SoundFile, new StringBuilder(), 0, IntPtr.Zero);
        mciSendString("play " + SoundFile, new StringBuilder(), 0, IntPtr.Zero);
    }
    public void Stop()
    {
        mciSendString("stop " + SoundFile, new StringBuilder(), 0, IntPtr.Zero);
    }
}
static class Utility
{
    public static float[] SimpleNoise1D(float[] Seed, int Octaves = 8, float Bias = 2f, bool Wrap = false)
    {
        int nCount = Seed.Length;
        float[] fOutput = new float[nCount];
        for (int x = 0; x < nCount; x++)
        {
            float fNoise = 0f;
            float fScaleAcc = 0f;
            float fScale = 1f;
            for (int o = 0; o < Octaves; o++)
            {
                int nPitch = nCount >> o;
                int nSample1 = (x / nPitch) * nPitch;
                int ns = nSample1 + nPitch;
                int nSample2 = (ns < nCount) ? nSample1 + nPitch : (Wrap ? ns % nCount : nCount - 1);
                float fBlend = (x - nSample1) / (float)nPitch;
                float fSample = (1.0f - fBlend) * Seed[nSample1] + fBlend * Seed[nSample2];
                fScaleAcc += fScale;
                fNoise += fSample * fScale;
                fScale /= Bias;
            }
            fOutput[x] = fNoise / fScaleAcc;
        }
        return fOutput;
    }
    public static float[,] SimpleNoise2D(float[,] Seed, int Octaves = 8, float Bias = 2f, bool Wrap = false)
    {
        int nWidth = Seed.GetLength(0);
        int nHeight = Seed.GetLength(1);
        float[,] fOutput = new float[nWidth, nHeight];
        for (int x = 0; x < nWidth; x++)
            for (int y = 0; y < nHeight; y++)
            {
                float fNoise = 0.0f;
                float fScaleAcc = 0.0f;
                float fScale = 1.0f;
                for (int o = 0; o < Octaves; o++)
                {
                    int nPitchX = nWidth >> o;
                    int nPitchY = nHeight >> o;
                    int nSampleX1 = (x / nPitchX) * nPitchX;
                    int nSampleY1 = (y / nPitchY) * nPitchY;
                    int nsx = nSampleX1 + nPitchX;
                    int nsy = nSampleY1 + nPitchY;
                    int nSampleX2 = (nsx < nWidth) ? nsx : (Wrap ? nsx % nWidth : nWidth - 1);
                    int nSampleY2 = (nsy < nHeight) ? nsy : (Wrap ? nsy % nHeight : nHeight - 1);
                    float fBlendX = (x - nSampleX1) / (float)nPitchX;
                    float fBlendY = (y - nSampleY1) / (float)nPitchY;
                    float fSampleT = (1.0f - fBlendX) * Seed[nSampleX1, nSampleY1] + fBlendX * Seed[nSampleX2, nSampleY1];
                    float fSampleB = (1.0f - fBlendX) * Seed[nSampleX1, nSampleY2] + fBlendX * Seed[nSampleX2, nSampleY2];
                    fScaleAcc += fScale;
                    fNoise += (fBlendY * (fSampleB - fSampleT) + fSampleT) * fScale;
                    fScale /= Bias;
                }
                fOutput[x, y] = fNoise / fScaleAcc;
            }
        return fOutput;
    }
    public static float Map(float Value, float Valuelowlimit, float Valuehighlimit, float Maplowlimit, float Maphighlimit)
    {
        if (Value >= Valuehighlimit) return Maphighlimit;
        if (Value <= Valuelowlimit) return Maplowlimit;
        float procent = (Value - Valuelowlimit) / (Valuehighlimit - Valuelowlimit);
        float limitlen = Maphighlimit - Maplowlimit;
        return Maplowlimit + procent * limitlen;
    }
    public static void Swap<T>(ref T x, ref T y)
    {
        T t = y;
        y = x;
        x = t;
    }
    public static float Degrees(float radians)
    {
        return (float)(180f / Math.PI * radians);
    }
    public static float Radians(float degrees)
    {
        return (float)(degrees * Math.PI / 180f);
    }
    public static Bitmap RotateImage(Bitmap image, float angle)
    {
        int w = image.Width;
        int h = image.Height;
        Bitmap tempImg = new Bitmap(w, h);
        Graphics g = Graphics.FromImage(tempImg);
        g.Clear(Color.Transparent);
        g.DrawImageUnscaled(image, 0, 0);
        g.Dispose();
        GraphicsPath path = new GraphicsPath();
        path.AddRectangle(new RectangleF(0f, 0f, w, h));
        System.Drawing.Drawing2D.Matrix mtrx = new System.Drawing.Drawing2D.Matrix();
        mtrx.Rotate(angle);
        RectangleF rct = path.GetBounds(mtrx);
        Bitmap newImg = new Bitmap((int)rct.Width, (int)rct.Height);
        g = Graphics.FromImage(newImg);
        g.Clear(Color.Transparent);
        g.TranslateTransform(-rct.X, -rct.Y);
        g.RotateTransform(angle);
        g.InterpolationMode = InterpolationMode.HighQualityBilinear;
        g.DrawImageUnscaled(tempImg, 0, 0);
        g.Dispose();
        tempImg.Dispose();
        return newImg;
    }
}
static class Time
{
    private static Stopwatch s;
    private static TimeSpan t;
    private static double t1;
    private static double t2;
    public static float fElapsedTime;
    public static float fTotalTime;
    public static void Start()
    {
        t1 = 0;
        t2 = 0;
        s = new Stopwatch();
        s.Start();
        Calculate();
    }
    public static void Calculate()
    {
        t = s.Elapsed;
        t1 = t.TotalSeconds;
        fElapsedTime = (float)(t1 - t2);
        t2 = t1;
        fTotalTime += fElapsedTime;
    }
}
class Vector
{
    public float x;
    public float y;
    public float z;
    public Vector(float _x = 0f, float _y = 0f, float _z = 0f)
    {
        x = _x;
        y = _y;
        z = _z;
    }
    public Vector(PointF p)
    {
        x = p.X;
        y = p.Y;
        z = 0f;
    }
    public static Vector RandomVector()
    {
        return VectorFromAngle((float)(FormGameEngine.rnd.NextDouble() * Math.PI * 2f));
    }
    public Vector Clone()
    {
        return new Vector(x, y, z);
    }
    public float Dot(Vector v)
    {
        return (x * v.x + y * v.y + z * v.z);
    }
    public Vector Cross(Vector v)
    {
        return new Vector(y * v.z - z * v.y, z * v.x - x * v.z, x * v.y - y * v.x);
    }
    public float Magnitude()
    {
        return (float)Math.Sqrt(x * x + y * y + z * z);
    }
    public Vector Normalize()
    {
        float d = Magnitude();
        if (d != 0f)
        {
            x /= d;
            y /= d;
            z /= d;
        }
        return Clone();
    }
    public Vector Limit(float value)
    {
        if (Magnitude() > value)
        {
            Vector v = Normalize() * value;
            x = v.x;
            y = v.y;
            z = v.z;
        }
        return Clone();
    }
    public Vector Rotate(float angle)
    {
        float x_ = (float)(Math.Cos(angle) * x - Math.Sin(angle) * y);
        float y_ = (float)(Math.Sin(angle) * x + Math.Sin(angle) * y);
        x = x_;
        y = y_;
        return Clone();
    }
    public Vector Lerp(Vector v)
    {
        return (this + v) * 0.5f;
    }
    public static Vector VectorFromAngle(float angle)
    {
        return new Vector((float)Math.Cos(angle), (float)Math.Sin(angle));
    }
    public float AngleFromVector()
    {
        return (float)Math.Atan2(y, x);
    }
    public bool OutsideBounds(float x1, float y1, float x2, float y2)
    {
        if (x1 > x2)
        {
            float t = x1;
            x1 = x2;
            x2 = t;
        }
        if (y1 > y2)
        {
            float t = y1;
            y1 = y2;
            y2 = t;
        }
        if (x >= x1 && y >= y1 && x <= x2 && y <= y2) return false;
        return true;
    }
    public void KeepInBounds(float x1, float y1, float x2, float y2, bool warp = false)
    {
        if (x1 > x2)
        {
            float t = x1;
            x1 = x2;
            x2 = t;
        }
        if (y1 > y2)
        {
            float t = y1;
            y1 = y2;
            y2 = t;
        }
        if (warp)
        {
            if (x < x1) x = x2;
            if (y < y1) y = y2;
            if (x > x2) x = x1;
            if (y > y2) y = y1;
        }
        else
        {
            if (x < x1) x = x1;
            if (y < y1) y = y1;
            if (x > x2) x = x2;
            if (y > y2) y = y2;
        }
    }
    public PointF AsPoint()
    {
        return new PointF(x, y);
    }
    public override string ToString()
    {
        return $"x: {x}, y: {y}, z: {z}";
    }
    public static Vector operator *(Vector v1, float l)
    {
        return new Vector(v1.x * l, v1.y * l, v1.z * l);
    }
    public static Vector operator *(float l, Vector v1)
    {
        return new Vector(v1.x * l, v1.y * l, v1.z * l);
    }
    public static Vector operator /(Vector v1, float l)
    {
        return new Vector(v1.x / l, v1.y / l, v1.z / l);
    }
    public static Vector operator /(float l, Vector v1)
    {
        return new Vector(v1.x / l, v1.y / l, v1.z / l);
    }
    public static Vector operator +(Vector v1, Vector v2)
    {
        return new Vector(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
    }
    public static Vector operator -(Vector v1, Vector v2)
    {
        return new Vector(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
    }
}
class Matrix
{
    public float[,] matrix;
    public int rows;
    public int cols;
    public Matrix(int n, int m)
    {
        rows = n;
        cols = m;
        matrix = new float[rows, cols];
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                matrix[i, j] = 0f;
    }
    public Matrix(float[,] sample)
    {
        rows = sample.GetLength(0);
        cols = sample.GetLength(1);
        matrix = new float[rows, cols];
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                matrix[i, j] = sample[i, j];
    }
    public static Matrix Identity(int n, int m)
    {
        Matrix mat = new Matrix(n, m);
        for (int i = 0; i < n; i++)
            mat.matrix[i, i] = 1f;
        return mat;
    }
    public static Matrix Incremented(int n, int m, int startvalue = 1)
    {
        Matrix mat = new Matrix(n, m);
        for (int i = 0; i < n; i++)
            for (int j = 0; j < m; j++)
                mat.matrix[i, j] = startvalue++;
        return mat;
    }
    public static Matrix RotationX(float a)
    {
        Matrix mat = new Matrix(3, 3);
        mat.matrix[0, 0] = 1f;
        mat.matrix[1, 1] = (float)Math.Cos(a);
        mat.matrix[1, 2] = -(float)Math.Sin(a);
        mat.matrix[2, 1] = (float)Math.Sin(a);
        mat.matrix[2, 2] = (float)Math.Cos(a);
        return mat;
    }
    public static Matrix RotationY(float a)
    {
        Matrix mat = new Matrix(3, 3);
        mat.matrix[0, 0] = (float)Math.Cos(a);
        mat.matrix[0, 2] = (float)Math.Sin(a);
        mat.matrix[1, 1] = 1f;
        mat.matrix[2, 0] = -(float)Math.Sin(a);
        mat.matrix[2, 2] = (float)Math.Cos(a);
        return mat;
    }
    public static Matrix RotationZ(float a)
    {
        Matrix mat = new Matrix(3, 3);
        mat.matrix[0, 0] = (float)Math.Cos(a);
        mat.matrix[0, 1] = -(float)Math.Sin(a);
        mat.matrix[1, 0] = (float)Math.Sin(a);
        mat.matrix[1, 1] = (float)Math.Cos(a);
        mat.matrix[2, 2] = 1f;
        return mat;
    }
    public static Matrix RotationXYZ(float ax, float ay, float az)
    {
        Matrix mat = new Matrix(3, 3);
        mat.matrix[0, 0] = (float)(Math.Cos(ay) * Math.Cos(az));
        mat.matrix[0, 1] = (float)(-Math.Cos(ay) * Math.Sin(az));
        mat.matrix[0, 2] = (float)Math.Sin(ay);
        mat.matrix[1, 0] = (float)(Math.Cos(ax) * Math.Sin(az) + Math.Sin(ax) * Math.Sin(ay) * Math.Cos(az));
        mat.matrix[1, 1] = (float)(Math.Cos(ax) * Math.Cos(az) - Math.Sin(ax) * Math.Sin(ay) * Math.Sin(az));
        mat.matrix[1, 2] = (float)(-Math.Sin(ax) * Math.Cos(ay));
        mat.matrix[2, 0] = (float)(Math.Sin(ax) * Math.Sin(az) - Math.Cos(ax) * Math.Sin(ay) * Math.Cos(az));
        mat.matrix[2, 1] = (float)(Math.Sin(ax) * Math.Cos(az) - Math.Cos(ax) * Math.Sin(ay) * Math.Sin(az));
        mat.matrix[2, 2] = (float)(Math.Cos(ax) * Math.Cos(ay));
        return mat;
    }
    public static Matrix RotationGivenAxis(Vector u, float a)
    {
        u.Normalize();
        Matrix mat = new Matrix(3, 3);
        mat.matrix[0, 0] = (float)(Math.Cos(a) + u.x * u.x * (1 - Math.Cos(a)));
        mat.matrix[0, 1] = (float)(u.x * u.y * (1 - Math.Cos(a)) - u.z * Math.Sin(a));
        mat.matrix[0, 2] = (float)(u.x * u.z * (1 - Math.Cos(a)) + u.y * Math.Sin(a));
        mat.matrix[1, 0] = (float)(u.y * u.x * (1 - Math.Cos(a)) + u.z * Math.Sin(a));
        mat.matrix[1, 1] = (float)(Math.Cos(a) + u.y * u.y * (1 - Math.Cos(a)));
        mat.matrix[1, 2] = (float)(u.y * u.z * (1 - Math.Cos(a)) - u.x * Math.Sin(a));
        mat.matrix[2, 0] = (float)(u.z * u.x * (1 - Math.Cos(a)) - u.y * Math.Sin(a));
        mat.matrix[2, 1] = (float)(u.z * u.y * (1 - Math.Cos(a)) + u.x * Math.Sin(a));
        mat.matrix[2, 2] = (float)(Math.Cos(a) + u.z * u.z * (1 - Math.Cos(a)));
        return mat;
    }
    public static Matrix RandomMatrix(int rows, int cols, int minval = 0, int maxval = 9)
    {
        Matrix mat = new Matrix(rows, cols);
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                mat.matrix[i, j] = FormGameEngine.rnd.Next(minval, maxval + 1);
        return mat;
    }
    public Matrix Clone()
    {
        Matrix mat = new Matrix(rows, cols);
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                mat.matrix[i, j] = matrix[i, j];
        return mat;
    }
    public Matrix Transpuse()
    {
        Matrix mat = new Matrix(rows, cols);
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                mat.matrix[j, i] = matrix[i, j];
        return mat;
    }
    public Matrix RemoveLines(int row, int col)
    {
        Matrix mat = new Matrix(row >= 0 && row < rows ? rows - 1 : rows, 
                                col >= 0 && col < cols ? cols - 1 : cols);
        for (int i = 0, j = 0; i < rows; i++)
        {
            if (i == row) continue;
            for (int k = 0, u = 0; k < cols; k++)
            {
                if (k == col) continue;
                mat.matrix[j, u] = matrix[i, k];
                u++;
            }
            j++;
        }
        return mat;
    }
    public Matrix Multiply(Matrix m)
    {
        if (cols == m.rows)
        {
            Matrix mat = new Matrix(rows, m.cols);
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < m.cols; j++)
                {
                    float sum = 0f;
                    for (int k = 0; k < cols; k++)
                        sum += matrix[i, k] * m.matrix[k, j];
                    mat.matrix[i, j] = sum;
                }
            return mat;
        }
        else
            throw new Exception("Can not mulitiply those");
    }
    public Vector Multiply(Vector v)
    {
        Matrix m = new Matrix(new float[,] { { v.x }, { v.y }, { v.z } });
        Matrix mm = Multiply(m);
        return new Vector(mm.matrix[0, 0], mm.matrix[1, 0], mm.matrix[2, 0]);
    }
    public float Determinant()
    {
        if (cols == rows)
        {
            int n = rows;
            if (n == 1) 
                return matrix[0, 0];
            if (n == 2)
                return matrix[0, 0] * matrix[1, 1] - matrix[1, 0] * matrix[0, 1];
            if (n == 3) 
            {
                return matrix[0, 0] * matrix[1, 1] * matrix[2, 2] +
                       matrix[1, 0] * matrix[2, 1] * matrix[0, 2] +
                       matrix[0, 1] * matrix[1, 2] * matrix[2, 0] -
                       matrix[2, 0] * matrix[1, 1] * matrix[0, 2] -
                       matrix[1, 0] * matrix[0, 1] * matrix[2, 2] -
                       matrix[2, 1] * matrix[1, 2] * matrix[0, 0];
            }
            else
            {
                float sum = 0;
                for (int i = 0; i < n; i++)
                    sum += (i % 2 == 0 ? matrix[0, i] : -matrix[0, i]) * RemoveLines(0, i).Determinant();
                return sum;
            }
        }
        else throw new Exception("Matrix needs to be square");
    }
    public Matrix Inverse()
    {
        float det = Determinant();
        if (det != 0f)
        {
            Matrix tr = Transpuse();
            Matrix mat = new Matrix(rows, cols);
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                {
                    float val = (i + j) % 2 == 0 ? 1 : -1;
                    mat.matrix[i, j] = val * tr.RemoveLines(i, j).Determinant();
                }
            return mat / det;
        }
        else throw new Exception("Matrix is not inverible");
    }
    public override string ToString()
    {
        string value = "";
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
                value += matrix[i, j] + " ";
            value += "\n";
        }
        return value;
    }
    public static Matrix operator /(Matrix m, float l)
    {
        Matrix mat = new Matrix(m.rows, m.cols);
        for (int i = 0; i < m.rows; i++)
            for (int j = 0; j < m.cols; j++)
                mat.matrix[i, j] = m.matrix[i, j] / l;
        return mat;
    }
    public static Matrix operator /(float l, Matrix m)
    {
        Matrix mat = new Matrix(m.rows, m.cols);
        for (int i = 0; i < m.rows; i++)
            for (int j = 0; j < m.cols; j++)
                mat.matrix[i, j] = m.matrix[i, j] / l;
        return mat;
    }
    public static Matrix operator *(Matrix m, float l)
    {
        Matrix mat = new Matrix(m.rows, m.cols);
        for (int i = 0; i < m.rows; i++)
            for (int j = 0; j < m.cols; j++)
                mat.matrix[i, j] = m.matrix[i, j] * l;
        return mat;
    }
    public static Matrix operator *(float l, Matrix m)
    {
        Matrix mat = new Matrix(m.rows, m.cols);
        for (int i = 0; i < m.rows; i++)
            for (int j = 0; j < m.cols; j++)
                mat.matrix[i, j] = m.matrix[i, j] * l;
        return mat;
    }
    public static Matrix operator +(Matrix m1, Matrix m2)
    {
        if (m1.cols == m2.cols && m1.rows == m2.rows)
        {
            Matrix mat = new Matrix(m1.rows, m1.cols);
            for (int i = 0; i < m1.rows; i++)
                for (int j = 0; j < m1.cols; j++)
                    mat.matrix[i, j] = m1.matrix[i, j] + m2.matrix[i, j];
            return mat;
        }
        else
            throw new Exception("The matrices do not have similar sizes");
    }
    public static Matrix operator -(Matrix m1, Matrix m2)
    {
        if (m1.cols == m2.cols && m1.rows == m2.rows)
        {
            Matrix mat = new Matrix(m1.rows, m1.cols);
            for (int i = 0; i < m1.rows; i++)
                for (int j = 0; j < m1.cols; j++)
                    mat.matrix[i, j] = m1.matrix[i, j] - m2.matrix[i, j];
            return mat;
        }
        else
            throw new Exception("The matrices do not have similar sizes");
    }
}
static class FileRead
{
    private static List<char> ListDividers = new List<char>() { ' ', ',', '!', '?' };
    private static List<string> FileUnchanged = new List<string>();
    private static string FileChanged = "";
    private static int CurrentIndex = 0;
    public static void ProcessFile(string FilePath)
    {
        FileUnchanged.Clear();
        FileChanged = "";
        CurrentIndex = 0;
        StringBuilder sb = new StringBuilder();
        using (var reader = new StreamReader(FilePath))
        {
            string buffer;
            while ((buffer = reader.ReadLine()) != null)
            {
                FileUnchanged.Add(buffer);
                sb.Append(buffer);
                sb.Append(" ");
            }
        }
        FileChanged = sb.ToString();
    }
    private static bool IsDivider(char Value)
    {
        foreach (char c in ListDividers)
            if (c == Value) return true;
        return false;
    }
    public static string GetNextAsText()
    {
        string word = "";
        bool Working = true;
        while (CurrentIndex < FileChanged.Length && Working)
        {
            if (!IsDivider(FileChanged[CurrentIndex]))
                word += FileChanged[CurrentIndex];
            else
                if (word.Length > 0)
                Working = false;
            CurrentIndex++;
        }
        return word;
    }
    public static dynamic GetNextAsValue()
    {
        string word = GetNextAsText();
        if (int.TryParse(word, out int valueint))
            return valueint;
        if (float.TryParse(word, out float valuefloat))
            return valuefloat;
        if (word.Length == 1) return word[0];
        if (word.Length == 0) throw new Exception("Reached the end of file");
        return word;
    }
    public static bool EndOfFile()
    {
        int LastIndex = CurrentIndex;
        string word = GetNextAsText();
        if (word == "") return true;
        else
        {
            CurrentIndex = LastIndex;
            return false;
        }
    }
    public static void ReplaceDividers(char[] Dividers)
    {
        ListDividers.Clear();
        for (int i = 0; i < Dividers.Length; i++)
            ListDividers.Add(Dividers[i]);
    }
    public static string ViewFile(int line = -1)
    {
        string ret;
        if (line >= 0)
            ret = FileUnchanged[line < FileUnchanged.Count ? line : FileUnchanged.Count - 1];
        else
        {
            StringBuilder sb = new StringBuilder();
            foreach (string s in FileUnchanged) sb.Append(s);
            ret = sb.ToString();
        }
        return ret;
    }
}
class Animation
{
    private Bitmap[] ImageFrames;
    private int nFramesCount;
    private int nCurrentFrame;
    private float[] fTimeBetweenFrames;
    private bool bAnimating;
    private Stopwatch timer;
    public Animation(Bitmap SpriteSheet, int FramesCount)
    {
        nFramesCount = FramesCount;
        fTimeBetweenFrames = new float[FramesCount];
        ImageFrames = new Bitmap[FramesCount];
        for (int i = 0; i < FramesCount; i++)
        {
            fTimeBetweenFrames[i] = 100f;
            Rectangle area = new Rectangle(i * SpriteSheet.Width / FramesCount,
                                           0,
                                           SpriteSheet.Width / FramesCount,
                                           SpriteSheet.Height);
            ImageFrames[i] = SpriteSheet.Clone(area, SpriteSheet.PixelFormat);
        }
        timer = new Stopwatch();
    }
    public Animation(Bitmap[] Sprites)
    {
        nFramesCount = Sprites.Length;
        fTimeBetweenFrames = new float[nFramesCount];
        ImageFrames = new Bitmap[nFramesCount];
        for (int i = 0; i < nFramesCount; i++)
        {
            fTimeBetweenFrames[i] = 100f;
            ImageFrames[i] = Sprites[i];
        }
        timer = new Stopwatch();
    }
    public void RescaleFrames(float procent)
    {
        for (int i = 0; i < nFramesCount; i++)
        {
            int w = ImageFrames[i].Width;
            int h = ImageFrames[i].Height;
            ImageFrames[i] = new Bitmap(ImageFrames[i], (int)(w * procent), (int)(h * procent));
        }
    }
    public void SetTimeBetweenFramesIntervals(float[] Intervals)
    {
        if (Intervals.Length == 1 && nFramesCount > 1)
        {
            for (int i = 0; i < nFramesCount; i++)
                fTimeBetweenFrames[i] = Intervals[0];
        }
        else
        {
            for (int i = 0; i < Intervals.Length; i++)
                fTimeBetweenFrames[i] = Intervals[i];
        }
    }
    public void AnimationState(bool ChangeState)
    {
        if (ChangeState)
        {
            if (!bAnimating)
            {
                nCurrentFrame = 0;
                bAnimating = true;
                timer.Start();
            }
        }
        else
        {
            if (bAnimating)
            {
                bAnimating = false;
                timer.Reset();
            }
        }
    }
    public void Animate(Graphics g, float px, float py)
    {
        if (bAnimating)
        {
            if (timer.ElapsedMilliseconds >= fTimeBetweenFrames[nCurrentFrame])
            {
                timer.Restart();
                nCurrentFrame++;
                if (nCurrentFrame >= nFramesCount) nCurrentFrame = 0;
            }
            g.DrawImage(ImageFrames[nCurrentFrame], px - ImageFrames[nCurrentFrame].Width * 0.5f,
                                                    py - ImageFrames[nCurrentFrame].Height * 0.5f);
        }
    }
}
static class Input
{
    private static Hashtable kb_prev;
    private static Hashtable kb_now;
    private static bool ScrollUp;
    private static bool ScrollDown;
    private static Vector mouse;
    private static Vector pmouse;
    public static void LinkReferences(Form form, PictureBox canvas)
    {
        kb_prev = new Hashtable();
        kb_now = new Hashtable();
        mouse = new Vector();
        pmouse = new Vector();
        form.KeyPreview = true;
        form.KeyDown += EventKeyDown;
        form.KeyUp += EventKeyUp;
        canvas.MouseDown += EventMouseDown;
        canvas.MouseUp += EventMouseUp;
        canvas.MouseMove += EvenMouseMove;
        canvas.MouseWheel += EventMouseWheel;
    }
    private static void EventMouseWheel(object sender, MouseEventArgs e)
    {
        if (e.Delta > 0)
        {
            ScrollUp = true;
            ScrollDown = false;
        }
        else if (e.Delta < 0)
        {
            ScrollUp = false;
            ScrollDown = true;
        }
    }
    private static void EvenMouseMove(object sender, MouseEventArgs e)
    {
        mouse.x = e.X;
        mouse.y = e.Y;
    }
    private static void EventMouseDown(object sender, MouseEventArgs e)
    {
        UpdateState(e.Button, true);
    }
    private static void EventMouseUp(object sender, MouseEventArgs e)
    {
        UpdateState(e.Button, false);
    }
    private static void EventKeyDown(object sender, KeyEventArgs e)
    {
        UpdateState(e.KeyCode, true);
    }
    private static void EventKeyUp(object sender, KeyEventArgs e)
    {
        UpdateState(e.KeyCode, false);
    }
    public static bool WheelScrollUp()
    {
        bool state = ScrollUp;
        ScrollUp = false;
        return state;
    }
    public static bool WheelScrollDown()
    {
        bool state = ScrollDown;
        ScrollDown = false;
        return state;
    }
    public static bool KeyPressed(MouseButtons key)
    {
        if (kb_now[key] != null)
        {
            if (!(bool)kb_prev[key])
                return (bool)kb_now[key];
            else
                return false;
        }
        else return false;
    }
    public static bool KeyReleased(MouseButtons key)
    {
        if (kb_now[key] != null)
        {
            if ((bool)kb_prev[key])
                return !(bool)kb_now[key];
            else
                return false;
        }
        else return false;
    }
    public static bool KeyHeld(MouseButtons key)
    {
        if (kb_now[key] == null) return false;
        return (bool)kb_now[key];
    }
    public static bool KeyPressed(Keys key)
    {
        if (kb_now[key] != null)
        {
            if (!(bool)kb_prev[key])
                return (bool)kb_now[key];
            else
                return false;
        }
        else return false;
    }
    public static bool KeyReleased(Keys key)
    {
        if (kb_now[key] != null)
        {
            if ((bool)kb_prev[key])
                return !(bool)kb_now[key];
            else
                return false;
        }
        else return false;
    }
    public static bool KeyHeld(Keys key)
    {
        if (kb_now[key] == null) return false;
        return (bool)kb_now[key];
    }
    public static Vector MousePos()
    {
        return mouse;
    }
    public static Vector PMousePos()
    {
        return pmouse;
    }
    public static void UpdateKeys()
    {
        foreach (DictionaryEntry key in kb_now)
            kb_prev[key.Key] = (bool)kb_now[key.Key];
        pmouse = mouse.Clone();
    }
    public static void UpdateState(Keys key, bool state)
    {
        kb_now[key] = state;
        if (kb_prev[key] == null) kb_prev[key] = false;
    }
    public static void UpdateState(MouseButtons key, bool state)
    {
        kb_now[key] = state;
        if (kb_prev[key] == null) kb_prev[key] = false;
    }
}