using System;
using System.Drawing;
using System.Windows.Forms;

namespace BackSplash
{
    public partial class BackSplash : Form
    {
        Timer t1 = new Timer();

        private void Splash_Load(object sender, EventArgs e)
        {
            Opacity = 0;      //first the opacity is 0

            t1.Interval = 10;  //we'll increase the opacity every 10ms
            t1.Tick += new EventHandler(fadeIn);  //this calls the function that changes opacity 
            t1.Start();
        }

        private void Splash_Unload(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;    //cancel the event so the form won't be closed

            t1.Tick += new EventHandler(fadeOut);  //this calls the fade out function
            t1.Start();

            if (Opacity == 0)  //if the form is completly transparent
                e.Cancel = false;   //resume the event - the program can be closed

        }

        void fadeOut(object sender, EventArgs e)
        {
            if (Opacity <= 0)     //check if opacity is 0
            {
                t1.Stop();    //if it is, we stop the timer
                Close();   //and we try to close the form
            }
            else
                Opacity -= 0.05;
        }

        void fadeIn(object sender, EventArgs e)
        {
            if (Opacity >= 1)
                t1.Stop();   //this stops the timer if the form is completely displayed
            else
                Opacity += 0.05;
        }

        public void InitSplash(String splash_path)
        {

            this.FormBorderStyle = FormBorderStyle.None;
            this.SetBounds(0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            this.BackColor = Color.FromArgb(0, 0, 0);

            if (splash_path != "")
            {
                this.BackgroundImageLayout = ImageLayout.Stretch;
                this.BackgroundImage = Image.FromFile(splash_path);
            }
        }

        public void Disable()
        {
            this.Hide();
        }

        public void Enable(String app_path)
        {
            
            InitSplash(app_path);
            this.Show();
            this.Refresh();
        }
        public BackSplash()
        {
            InitializeComponent();
        }
    }
}
