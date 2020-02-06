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
            Sound enginesound;
            Bitmap spriteship;
            Vector pos;
            Vector vel;
            float maxspeed;
            float maxsteer;
            public Demo()
            {
                AppName = "MyDemo";
            }
            public override void OnUserCreate()
            {
                enginesound = new Sound("../../enginesound.wav");
                spriteship = new Bitmap("../../spaceship.png");
                pos = Middle();
                vel = new Vector();
                maxspeed = 200f;
                maxsteer = 0.01f;
            }
            public override void OnUserUpdate(float fElapsedTime)
            {
                gCanvas.Clear(Color.Black);
                if (Input.KeyPressed(MouseButtons.Left)) maxspeed *= 3f;
                if (Input.KeyReleased(MouseButtons.Left)) maxspeed /= 3f;
                Vector desire = (Input.MousePos() - pos).Normalize();
                Vector steering = desire - vel;
                vel += steering.Limit(maxsteer);
                float distance = (Input.MousePos() - pos).Magnitude();
                float speed = Utility.Map(distance, 1f, 100f, 0f, maxspeed);
                pos += vel * speed * fElapsedTime;
                float angle = vel.AngleFromVector();
                Bitmap pspriteship = Utility.RotateImage(spriteship, Utility.Degrees(angle));
                gCanvas.DrawImage(pspriteship, pos.x - pspriteship.Width * 0.5f, pos.y - pspriteship.Height * 0.5f);
                pspriteship.Dispose();
                if (speed > 10f && Focused()) enginesound.PlayLoop();
                else enginesound.Stop();
            }
        }
    }
}
