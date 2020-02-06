using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Media;
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
    private int nFrameCounter;
    private int nScreenWidth;
    private int nScreenHeight;
    private float fpsTime;
    private int fps;
    public void Construct(Form thisform, int screenWidth, int screenHeight, bool fullscreen = false)
    {
        if (screenWidth < 100 || screenHeight < 10)
        {
            screenWidth = 640;
            screenHeight = 480;
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
            nScreenWidth = screenWidth;
            nScreenHeight = screenHeight;
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
        nFrameCounter++;
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
    public int FrameCount()
    {
        return nFrameCounter;
    }
    public Vector Middle()
    {
        return new Vector(nScreenWidth * 0.5f, nScreenHeight * 0.5f);
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
    public void SaveFrame(string locationpath, string name, string format = "bmp")
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
    private SoundPlayer sp;
    private bool bPlaying;
    public Sound(string filepath)
    {
        sp = new SoundPlayer(filepath);
        bPlaying = false;
    }
    ~Sound()
    {
        sp.Dispose();
    }
    public void PlayOnce()
    {
        if (!bPlaying)
        {
            sp.Play();
            bPlaying = true;
        }
    }
    public void Play()
    {
        sp.Play();
    }
    public void Stop()
    {
        if (bPlaying)
        {
            sp.Stop();
            bPlaying = false;
        }
    }
    public void PlayLoop()
    {
        if (!bPlaying) 
        {
            sp.PlayLooping();
            bPlaying = true;
        }
    }
}
static class Utility
{
    public static uint SeededRandom(uint seed)
    {
        seed += 0xe120fc15;
        ulong tmp = (ulong)seed * 0x4a39b70d;
        uint m1 = (uint)((tmp >> 32) ^ tmp);
        tmp = (ulong)m1 * 0x12fad5c9;
        uint m2 = (uint)((tmp >> 32) ^ tmp);
        return m2;
    }
    public static float[] SimpleNoise1D(float[] seed, int octaves = 8, float bias = 2f, bool wrap = false)
    {
        int nCount = seed.Length;
        float[] fOutput = new float[nCount];
        for (int x = 0; x < nCount; x++)
        {
            float fNoise = 0f;
            float fScaleAcc = 0f;
            float fScale = 1f;
            for (int o = 0; o < octaves; o++)
            {
                int nPitch = nCount >> o;
                int nSample1 = (x / nPitch) * nPitch;
                int ns = nSample1 + nPitch;
                int nSample2 = (ns < nCount) ? nSample1 + nPitch : (wrap ? ns % nCount : nCount - 1);
                float fBlend = (x - nSample1) / (float)nPitch;
                float fSample = (1.0f - fBlend) * seed[nSample1] + fBlend * seed[nSample2];
                fScaleAcc += fScale;
                fNoise += fSample * fScale;
                fScale /= bias;
            }
            fOutput[x] = fNoise / fScaleAcc;
        }
        return fOutput;
    }
    public static float[,] SimpleNoise2D(float[,] seed, int octaves = 8, float bias = 2f, bool wrap = false)
    {
        int nWidth = seed.GetLength(0);
        int nHeight = seed.GetLength(1);
        float[,] fOutput = new float[nWidth, nHeight];
        for (int x = 0; x < nWidth; x++)
            for (int y = 0; y < nHeight; y++)
            {
                float fNoise = 0.0f;
                float fScaleAcc = 0.0f;
                float fScale = 1.0f;
                for (int o = 0; o < octaves; o++)
                {
                    int nPitchX = nWidth >> o;
                    int nPitchY = nHeight >> o;
                    int nSampleX1 = (x / nPitchX) * nPitchX;
                    int nSampleY1 = (y / nPitchY) * nPitchY;
                    int nsx = nSampleX1 + nPitchX;
                    int nsy = nSampleY1 + nPitchY;
                    int nSampleX2 = (nsx < nWidth) ? nsx : (wrap ? nsx % nWidth : nWidth - 1);
                    int nSampleY2 = (nsy < nHeight) ? nsy : (wrap ? nsy % nHeight : nHeight - 1);
                    float fBlendX = (x - nSampleX1) / (float)nPitchX;
                    float fBlendY = (y - nSampleY1) / (float)nPitchY;
                    float fSampleT = (1.0f - fBlendX) * seed[nSampleX1, nSampleY1] + fBlendX * seed[nSampleX2, nSampleY1];
                    float fSampleB = (1.0f - fBlendX) * seed[nSampleX1, nSampleY2] + fBlendX * seed[nSampleX2, nSampleY2];
                    fScaleAcc += fScale;
                    fNoise += (fBlendY * (fSampleB - fSampleT) + fSampleT) * fScale;
                    fScale /= bias;
                }
                fOutput[x, y] = fNoise / fScaleAcc;
            }
        return fOutput;
    }
    public static float Map(float value, float valueLowLimit, float valueHighLimit, float mapLowLimit, float mapHighLimit)
    {
        if (value >= valueHighLimit) return mapHighLimit;
        if (value <= valueLowLimit) return mapLowLimit;
        float procent = (value - valueLowLimit) / (valueHighLimit - valueLowLimit);
        float limitlen = mapHighLimit - mapLowLimit;
        return mapLowLimit + procent * limitlen;
    }
    public static void Swap<T>(ref T a, ref T b)
    {
        T t = b;
        b = a;
        a = t;
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
    public static Vector CollisionSAT(Vector[] pa, Vector[] pb)
    {
        float overlap = float.MaxValue;
        Vector resolve = new Vector();
        for (int s = 0; s < 2; s++)
        {
            if (s == 1) Swap(ref pa, ref pb);
            for (int p = 0; p < pa.Length; p++)
            {
                Vector axp = new Vector(-(pa[(p + 1) % pa.Length].y - pa[p].y), pa[(p + 1) % pa.Length].x - pa[p].x).Normalize();
                float min_r1 = float.MaxValue;
                float max_r1 = float.MinValue;
                for (int pt = 0; pt < pa.Length; pt++)
                {
                    float dot = pa[pt].Dot(axp);
                    min_r1 = Math.Min(min_r1, dot);
                    max_r1 = Math.Max(max_r1, dot);
                }
                float min_r2 = float.MaxValue;
                float max_r2 = float.MinValue;
                for (int pt = 0; pt < pb.Length; pt++)
                {
                    float dot = pb[pt].Dot(axp);
                    min_r2 = Math.Min(min_r2, dot);
                    max_r2 = Math.Max(max_r2, dot);
                }
                float co = Math.Min(max_r1, max_r2) - Math.Max(min_r1, min_r2);
                if (co < overlap)
                {
                    if (co < 0f) return new Vector();
                    overlap = co;
                    resolve = axp;
                }
            }
        }
        Vector mida = new Vector();
        for (int i = 0; i < pa.Length; i++) mida += pa[i];
        mida /= pa.Length;
        Vector midb = new Vector();
        for (int i = 0; i < pb.Length; i++) midb += pb[i];
        midb /= pb.Length;
        Vector cd = (mida - midb).Normalize();
        if (cd.Dot(resolve) < 0f) resolve *= -1f;
        return resolve * -overlap;
    }
}
static class Time
{
    public static Stopwatch s;
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
    public Vector(SizeF s)
    {
        x = s.Width;
        y = s.Height;
        z = 0f;
    }
    public static Vector RandomVector()
    {
        return VectorFromAngle((float)(FormGameEngine.rnd.NextDouble() * Math.PI) * 2f);
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
    public Vector Rotate(float angle, Vector pivot = null)
    {
        if (pivot == null) pivot = new Vector();
        float x_ = (float)(Math.Cos(angle) * (x - pivot.x) - Math.Sin(angle) * (y - pivot.y)) + pivot.x;
        float y_ = (float)(Math.Cos(angle) * (y - pivot.y) + Math.Sin(angle) * (x - pivot.x)) + pivot.y;
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
    public bool InsideBounds(float x1, float y1, float x2, float y2)
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
        if (x >= x1 && y >= y1 && x <= x2 && y <= y2) return true;
        return false;
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
    public static PointF[] AsPoints(Vector[] v)
    {
        PointF[] p = new PointF[v.Length];
        for (int i = 0; i < p.Length; i++) p[i] = v[i].AsPoint();
        return p;
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
    public Vector Multiply(Vector v, bool vectorfirst = false)
    {
        if (vectorfirst)
        {
            Matrix m = new Matrix(new float[,] { { v.x, v.y, v.z } });
            Matrix mm = m.Multiply(this);
            return new Vector(mm.matrix[0, 0], mm.matrix[0, 1], mm.matrix[0, 2]);
        }
        else
        {
            Matrix m = new Matrix(new float[,] { { v.x }, { v.y }, { v.z } });
            Matrix mm = Multiply(m);
            return new Vector(mm.matrix[0, 0], mm.matrix[1, 0], mm.matrix[2, 0]);
        }
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
class Line
{
    public Vector sp;
    public Vector ep;
    public Line(float spx, float spy, float epx, float epy)
    {
        sp = new Vector(spx, spy);
        ep = new Vector(epx, epy);
    }
    public Line(Vector sp, Vector ep)
    {
        this.sp = sp.Clone();
        this.ep = ep.Clone();
    }
    public Line Clone()
    {
        return new Line(sp, ep);
    }
    public float DistPointToLine(Vector v)
    {
        float l2 = Length() * Length(); 
        if (l2 == 0f) return (v - sp).Magnitude();   
        float t = Math.Max(0f, Math.Min(1f, (v - sp).Dot(ep - sp) / l2));
        Vector projection = sp + t * (ep - sp);  
        return (v - projection).Magnitude();
    }
    public Vector ProjPointOnLine(Vector v)
    {
        float m = (sp.y - ep.y) / (sp.x - ep.x);
        float n = sp.y - m * sp.x;
        float x = (v.x + m * v.y - m * n) / (m * m + 1);
        float y = m * x + n;
        Vector rez = new Vector(x, y);
        if (DistPointToLine(rez) > 0.1f) 
        {
            if ((sp - rez).Magnitude() < (ep - rez).Magnitude()) rez = sp.Clone();
            else rez = ep.Clone();
        }
        return rez;
    }
    public Vector IntersectionPoint(Line l, bool direct = true)
    {
        float x1 = sp.x;
        float y1 = sp.y;
        float x2 = ep.x;
        float y2 = ep.y;
        float x3 = l.sp.x;
        float y3 = l.sp.y;
        float x4 = l.ep.x;
        float y4 = l.ep.y;
        float t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4));
        float u = -((x1 - x2) * (y1 - y3) - (y1 - y2) * (x1 - x3)) / ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4));
        if (t >= 0f && u >= 0f && u <= 1f)
        {
            if (direct)
            {
                if (t <= 1f)
                    return new Vector(x1 + t * (x2 - x1), y1 + t * (y2 - y1));
            }
            else return new Vector(x1 + t * (x2 - x1), y1 + t * (y2 - y1));
        }
        return null;
    }
    public float AngleLine(Line l)
    {
        float m1 = (sp.y - ep.y) / (sp.x - ep.x);
        float m2 = (l.sp.y - l.ep.y) / (l.sp.x - l.ep.x);
        return (float)Math.Atan((m1 - m2) / (1f + m1 * m2));
    }
    public float Length()
    {
        return (ep - sp).Magnitude();
    }
    public Vector Heading()
    {
        return (ep - sp).Normalize();
    }
    public override string ToString()
    {
        return $"SP({sp.x}, {sp.y}, {sp.z}), EP({ep.x}, {ep.y}, {ep.z})";
    }
}
static class FileRead
{
    private static List<char> ListDividers = new List<char>() { ' ', ',', '!', '?' };
    private static List<string> FileUnchanged = new List<string>();
    private static string FileChanged = "";
    private static int CurrentIndex = 0;
    public static void ProcessFile(string filepath)
    {
        FileUnchanged.Clear();
        FileChanged = "";
        CurrentIndex = 0;
        StringBuilder sb = new StringBuilder();
        using (var reader = new StreamReader(filepath))
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
    private static bool IsDivider(char value)
    {
        foreach (char c in ListDividers)
            if (c == value) return true;
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
        if (word.Length == 0) return null;
        return word;
    }
    public static bool EndOfFile()
    {
        int LastIndex = CurrentIndex;
        string word = GetNextAsText();
        if (word == null) return true;
        else
        {
            CurrentIndex = LastIndex;
            return false;
        }
    }
    public static void ReplaceDividers(char[] dividers)
    {
        ListDividers.Clear();
        for (int i = 0; i < dividers.Length; i++)
            ListDividers.Add(dividers[i]);
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
    public Animation(Bitmap spriteSheet, int framesCount)
    {
        nFramesCount = framesCount;
        fTimeBetweenFrames = new float[framesCount];
        ImageFrames = new Bitmap[framesCount];
        for (int i = 0; i < framesCount; i++)
        {
            fTimeBetweenFrames[i] = 100f;
            Rectangle area = new Rectangle(i * spriteSheet.Width / framesCount,
                                           0,
                                           spriteSheet.Width / framesCount,
                                           spriteSheet.Height);
            ImageFrames[i] = spriteSheet.Clone(area, spriteSheet.PixelFormat);
        }
        timer = new Stopwatch();
    }
    public Animation(Bitmap[] sprites)
    {
        nFramesCount = sprites.Length;
        fTimeBetweenFrames = new float[nFramesCount];
        ImageFrames = new Bitmap[nFramesCount];
        for (int i = 0; i < nFramesCount; i++)
        {
            fTimeBetweenFrames[i] = 100f;
            ImageFrames[i] = sprites[i];
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
    public void SetTimeBetweenFramesIntervals(float[] intervals)
    {
        if (intervals.Length == 1 && nFramesCount > 1)
        {
            for (int i = 0; i < nFramesCount; i++)
                fTimeBetweenFrames[i] = intervals[0];
        }
        else
        {
            for (int i = 0; i < intervals.Length; i++)
                fTimeBetweenFrames[i] = intervals[i];
        }
    }
    public void AnimationState(bool changeState)
    {
        if (changeState)
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
    private static bool FormCaptured;
    public static void LinkReferences(Form form, PictureBox canvas)
    {
        kb_prev = new Hashtable();
        kb_now = new Hashtable();
        mouse = new Vector();
        pmouse = new Vector();
        form.KeyPreview = true;
        form.KeyDown += EventKeyDown;
        form.KeyUp += EventKeyUp;
        form.MouseCaptureChanged += EventMouseCapture;
        canvas.MouseDown += EventMouseDown;
        canvas.MouseUp += EventMouseUp;
        canvas.MouseMove += EvenMouseMove;
        canvas.MouseWheel += EventMouseWheel;
    }
    private static void EventMouseCapture(object sender, EventArgs e)
    {
        Time.s.Stop();
        FormCaptured = true;
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
        return mouse.Clone();
    }
    public static Vector PMousePos()
    {
        return pmouse.Clone();
    }
    public static Vector PlaneMovement()
    {
        Vector v = new Vector();
        if (KeyHeld(Keys.W) || KeyHeld(Keys.Up)) v.y += -1;
        if (KeyHeld(Keys.D) || KeyHeld(Keys.Right)) v.x += 1;
        if (KeyHeld(Keys.S) || KeyHeld(Keys.Down)) v.y += 1;
        if (KeyHeld(Keys.A) || KeyHeld(Keys.Left)) v.x += -1;
        return v.Normalize();
    }
    public static Keys AsKey(string keyname)
    {
        return (Keys)Enum.Parse(typeof(Keys), keyname, true);
    }
    public static void UpdateKeys()
    {
        foreach (DictionaryEntry key in kb_now)
            kb_prev[key.Key] = (bool)kb_now[key.Key];
        pmouse = mouse.Clone();
        if (FormCaptured)
        {
            Time.s.Start();
            FormCaptured = false;
        }
    }
    private static void UpdateState(Keys key, bool state)
    {
        kb_now[key] = state;
        if (kb_prev[key] == null) kb_prev[key] = false;
    }
    private static void UpdateState(MouseButtons key, bool state)
    {
        kb_now[key] = state;
        if (kb_prev[key] == null) kb_prev[key] = false;
    }
}