using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Engine
{
    public partial class Demolition : Form
    {
        public Demolition()
        {
            InitializeComponent();
            Demo demo = new Demo();
            demo.Construct(this, 1200, 800);
        }
        class Demo : FormGameEngine
        {
            float xoff;
            float yoff;
            float zoff;
            Vector[] grid;
            int cellsize;
            int w;
            int h;
            float noisegap;
            Vector pos;
            Vector vel;
            float size;
            float speed;
            public Demo()
            {
                AppName = "MyDemo";
            }
            public override void OnUserCreate()
            {
                pos = new Vector(100, 100);
                vel = new Vector();
                size = 20;
                speed = 300;

                Noise.Seed(rnd.Next(int.MaxValue));
                noisegap = 0.1f;
                cellsize = 20;
                w = ScreenWidth() / cellsize;
                h = ScreenHeight() / cellsize;
                grid = new Vector[w * h];
                for (int i = 0; i < grid.Length; i++) grid[i] = Vector.VectorFromAngle(0f);
            }
            public override void OnUserUpdate(float fElapsedTime)
            {
                gCanvas.Clear(Color.Black);
                yoff = 0f;
                for (int y = 0; y < h; y++)
                {
                    xoff = 0f;
                    for (int x = 0; x < w; x++)
                    {
                        int index = x + y * w;
                        Vector v1 = new Vector(x * cellsize + cellsize * 0.5f, y * cellsize + cellsize * 0.5f);
                        grid[index] = Vector.VectorFromAngle((float)(Noise.Eval(xoff, yoff, zoff) * Math.PI * 2f));
                        Vector v2 = v1 + grid[index] * cellsize * 0.5f;
                        gCanvas.DrawLine(Pens.White, v1.AsPoint(), v2.AsPoint());
                        //int g = (int)Utility.Map(Noise.Eval(xoff, yoff, zoff), 0f, 1f, 0, 255);
                        //SolidBrush sb = new SolidBrush(Color.FromArgb(g, g, g));
                        //gCanvas.FillRectangle(sb, x * cellsize, y * cellsize, cellsize, cellsize);
                        //sb.Dispose();
                        xoff += noisegap;
                    }
                    yoff += noisegap;
                }
                zoff += fElapsedTime;
                int place = (int)(pos.x / cellsize) + (int)(pos.y / cellsize) * w;
                vel = grid[place].Clone();
                Debug.WriteLine(place);
                pos += vel * speed * fElapsedTime;
                pos.KeepInBounds(0, 0, ScreenWidth() - 1, ScreenHeight() - 1, true);
                gCanvas.FillEllipse(Brushes.Red, pos.x - size * 0.5f, pos.y - size * 0.5f, size, size);
            }
        }
    }
}
