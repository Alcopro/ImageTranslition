using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Drawing.Drawing2D;

namespace ImageTranslition
{

    public delegate void Interface(object ui);

    public partial class Form1 : Form
    {

        public int scale, scaledw, scaledh, GlobW, GlobH;
        public double koefh, koefw;

        public Form1()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Zoom(ResultPicture, 2);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Zoom(ResultPicture, 0.5);
        }

        bool validPicture;

        private void button4_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
            string fle = openFileDialog1.FileName;
            if (fle.Substring(fle.LastIndexOf('.') + 1, fle.Length - fle.LastIndexOf('.') - 1) == "jpg")
            {
                validPicture = true;
                textBox3.Text = openFileDialog1.FileName;
            }
            else
            {
                validPicture = false;
                MessageBox.Show("Invalid FileName!");
            }
        }

        private void DrawResultPicture(PictureBox FromPicture)
        {
            int NewPicWidth = ResultPicture.Width;
            double koef = (double)ResultPicture.Width / (double)FromPicture.Width;
            int NewPicHeight = (int)((double)FromPicture.Height * koef);

            ResultPicture.Width = NewPicWidth;
            ResultPicture.Height = NewPicHeight;

            Bitmap tempBmp = new Bitmap(NewPicWidth, NewPicHeight);
            Graphics grp = Graphics.FromImage(tempBmp);
            Bitmap bmp = new Bitmap(FromPicture.Image);
            Rectangle srcRect = new Rectangle(0, 0, FromPicture.Width, FromPicture.Height);
            Rectangle dstRect = new Rectangle(0, 0, NewPicWidth, NewPicHeight);
            grp.DrawImage(bmp, dstRect, srcRect, GraphicsUnit.Pixel);
            grp.Dispose();

            ResultPicture.Image = tempBmp;
            ResultPicture.Refresh();
            bmp.Dispose();
            tempBmp.Dispose();
            NewPicWidth = NewPicHeight = 0;
            koef = 0;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (!validPicture)
            {
                MessageBox.Show("Invalid FileName!");
                return;
            }
            /*WorkPicture.Dispose();
            SourcePicture.Dispose();
            FinalPicture.Dispose();*/
            WorkPicture.Image = Image.FromFile(textBox3.Text);
            SourcePicture.Image = Image.FromFile(textBox3.Text);
            FinalPicture.Image = Image.FromFile(textBox3.Text);

            button1.Enabled = true;
            textBox1.Enabled = true;
            textBox2.Enabled = true;

            DrawResultPicture(SourcePicture);
        }
       
        private void button6_Click(object sender, EventArgs e)
        {
            saveFileDialog1.ShowDialog();
            try
            {
                if (saveFileDialog1.FileName != "")
                    FinalPicture.Image.Save(saveFileDialog1.FileName);
                MessageBox.Show("Успешно!");
            }
            catch
            {
                MessageBox.Show("Не удалось сохранить файл!");
            }
            finally
            {
                
            }           
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            progressBar1.Minimum = 0;
            progressBar2.Minimum = 0;
            progressBar1.Step = 1;
            int minWorker, minIOC;
            ThreadPool.GetMinThreads(out minWorker, out minIOC);
            ThreadPool.SetMinThreads(4096, minIOC);
            /*int maxthr = minWorker;
            while (ThreadPool.SetMinThreads(maxthr, minIOC))
                maxthr++;
            maxthr--;
            ThreadPool.SetMinThreads(maxthr, minIOC);*/

            //ThreadPool.GetMinThreads(out minWorker, out minIOC);
            //MessageBox.Show(minWorker.ToString());
            var uiContext = SynchronizationContext.Current;

            Interface thr = new Interface(Work);
            thr.BeginInvoke(uiContext, null, null);
            //Work();
            

        }     

        private void Zoom(PictureBox Pic, double scale)
        {
            Bitmap bmp = new Bitmap(WorkPicture.Image);
            int newW = (int)(Pic.Width * scale);
            int newH = (int)(Pic.Height * scale);

            Bitmap tempBmp = new Bitmap(Pic.Image);
            Graphics grp = Graphics.FromImage(tempBmp);
            Rectangle srcRect = new Rectangle(0, 0, newW, newH);
            Rectangle dstRect = new Rectangle(0, 0, Pic.Width, Pic.Height);
            grp.DrawImage(bmp, dstRect, srcRect, GraphicsUnit.Pixel);
            grp.Dispose();

            Pic.Image = tempBmp;
            return;
        }


        //--------------------------------------------------------
        //--------------------------------------------------------
        public class Pixelization
        {
            public static int DPI = 300;
            private static int GlobH;
            private static int GlobW;
            private static int scaledh;
            private static int scaledw;
            private static double H;
            private static double W;
            private static Bitmap SrcBitmap;
            private static int scale;
            private static System.Object locking = new System.Object();
            static Form1 F1;

            class Pieces
            {
                public int Count;
                public Bitmap[] Piece;
            } 

            private static Pieces ResultBmp;

            public void Clear()
            {
                ResultBmp = new Pieces();
                TempBmp = new Pieces();

                GlobW = GlobH = scaledw = scaledh = scale = 0;
                H = W = 0;
                return;
            }

            public void Init(Form1 Form, double w, double h, Bitmap Src)
            {
                F1 = Form;
                scale = 1;
                H = h;
                W = w;
                if (h < 1 || w < 1)
                    scale = 10;
                if (h < 0.1 || w < 0.1)
                    scale = 100;
                if (h < 0.01 || w < 0.01)
                    scale = 1000;
                if (h < 0.001 || w < 0.001)
                    scale = 10000;

                scaledh = (int)(h * scale);
                scaledw = (int)(w * scale);

                SrcBitmap = new Bitmap(Src);
                SrcBitmap.SetResolution(DPI, DPI);
            }

            private static int min(int x, int y)
            {
                return (x < y ? x : y);
            }

            private static int max(int x, int y)
            {
                return (x > y ? x : y);
            }

            private static Bitmap Rescale(Bitmap Pic, double scaled)
            {
                int newW = (int)(Pic.Width * scaled);
                int newH = (int)(Pic.Height * scaled);
                Bitmap bmp = new Bitmap(Pic, newW, newH);
                return bmp;
            }

            private static Pieces TempBmp;

            private static void DrawCells(Bitmap IBmp, int scaledh, int scaledw)
            {
                int GlobW = IBmp.Width;
                int GlobH = IBmp.Height;

                int k = Convert.ToInt32(Thread.CurrentThread.Name);

                for (int j = 0; j < GlobW; j += scaledw)
                {
                    Graphics grp = Graphics.FromImage(IBmp);
                    int x = min(j, GlobW - 1);
                    int y = 0;
                    int x1 = min(j + scaledw - 1, GlobW - 1);
                    int y1 = scaledh - 1;
                    int AvRed = (IBmp.GetPixel(x, y).R + IBmp.GetPixel(x, y1).R + IBmp.GetPixel(x1, y).R + IBmp.GetPixel(x1, y1).R) / 4;
                    int AvGreen = (IBmp.GetPixel(x, y).G + IBmp.GetPixel(x, y1).G + IBmp.GetPixel(x1, y).G + IBmp.GetPixel(x1, y1).G) / 4;
                    int AvBlue = (IBmp.GetPixel(x, y).B + IBmp.GetPixel(x, y1).B + IBmp.GetPixel(x1, y).B + IBmp.GetPixel(x1, y1).B) / 4;

                    //var brush = new SolidBrush(Color.FromArgb(AvRed, AvGreen, AvBlue));
                    //var pen = new Pen(Color.Black);
                    //pen.Width = 0.1F;
                    var brush = new SolidBrush(Color.FromArgb(AvRed, AvGreen, AvBlue));
                    grp.FillRectangle(brush, new Rectangle(x, y, scaledw, scaledh));
                    //grp.DrawLine(pen, 1f * x, 1f * y, 1f * (x + scaledw), 1f * (y + scaledh));

                    grp.Dispose();
                    brush.Dispose();
                    //pen.Dispose();
                    x = y = x1 = y1 = AvRed = AvGreen = AvBlue = 0;
                }
                GlobW = GlobH = 0;
                TempBmp.Piece[k] = new Bitmap(IBmp);
                IBmp.Dispose();
                return;
            }

            delegate void GetMessage();

            public void Repaint(int ind, Bitmap Src, object ui)
            {
                int GlobH = Src.Height;
                int GlobW = Src.Width;
                SynchronizationContext context = (SynchronizationContext)ui;
                //int ind = Convert.ToInt32(Thread.CurrentThread.Name);
                Thread[] workers = new Thread[GlobH / scaledh + 2];

                TempBmp.Count = 0;
                TempBmp.Piece = new Bitmap[GlobH / scaledh + 2];

                for (int i = 0; i < GlobH; i += scaledh, TempBmp.Count++)
                {
                    int H = (i + scaledh <= GlobH ? scaledh : GlobH - i);
                    Bitmap tempBmp = new Bitmap(Src.Clone(new Rectangle(0, i, GlobW, H), Src.PixelFormat));
                    workers[TempBmp.Count] = new Thread(() => DrawCells(tempBmp, H, scaledw));
                    workers[TempBmp.Count].Name = TempBmp.Count.ToString();
                }

                SendOrPostCallback action = (object objct) =>
                {
                    F1.progressBar2.Maximum = TempBmp.Count * 2;
                    F1.progressBar2.Step = 1;
                };
                context.Send(action, null);    

                for (int i = 0; i < TempBmp.Count; i++)
                {
                    workers[i].Start();
                    action = (object objct) =>
                    {
                        F1.progressBar2.PerformStep();
                    };
                    context.Send(action, null);
                }

                Bitmap newBmp = new Bitmap(Src.Width, Src.Height);
                int pos = 0;
                for (int i = 0; i < TempBmp.Count; i++, pos += scaledh)
                {
                    Graphics grp = Graphics.FromImage(newBmp);
                    workers[i].Join();
                    Rectangle srcRect = new Rectangle(0, 0, GlobW, scaledh);
                    Rectangle dstRect = new Rectangle(0, pos, GlobW, scaledh);
                    lock (locking)
                    {
                        grp.DrawImage(TempBmp.Piece[i], dstRect, srcRect, GraphicsUnit.Pixel);
                    }
                    action = (object objct) =>
                    {
                        F1.progressBar2.PerformStep();
                    };
                    context.Send(action, null);
                    grp.Dispose();
                }
                TempBmp.Piece = null;
                TempBmp.Count = 0;
                ResultBmp.Piece[ind] = Rescale(newBmp, 1 / (double)scale);
            }
            

            private static int GetOptimalH(int W, int H)
            {
                long KoefToGb = 8589934592; //8*1024*1024*1024 PROBLEM
                long w = (long)(W);
                long h = (long)(H);
                long calculated = (long)(h * w * 8); //ColorDepth default = 8 for gray
                long oldH = h;
                while ((double)(calculated) / (double)(KoefToGb) < 0.2) 
                {
                    h += 100;
                    calculated = (long)(h * w * 8);
                    if (h >= SrcBitmap.Height * scale)
                        return -1;
                }

                while (!(SrcBitmap.Height % h == 0))
                {
                    h--;
                }

                oldH = 0;
                KoefToGb = 0;
                if (h == 0)
                    h = -1;
                return (int)(h);
            }

            public Bitmap DoWork(object ui)
            {
                SynchronizationContext context = (SynchronizationContext)ui;

                GlobW = SrcBitmap.Width /** scale*/;
                GlobH = SrcBitmap.Height /** scale*/;

                int OptimalH = GetOptimalH(GlobW * scale, scaledh);
                OptimalH = (OptimalH == -1 ? GlobH * scale : OptimalH);

                ResultBmp.Count = 0;
                ResultBmp.Piece = new Bitmap[(int)(SrcBitmap.Height * scale / OptimalH + 2)];

                Bitmap FinBmp = new Bitmap(SrcBitmap.Width, SrcBitmap.Height);
                Thread[] workers = new Thread[(int)(SrcBitmap.Height * scale / OptimalH + 2)];

                OptimalH /= scale;

                context.Send(Progress, OptimalH);

                for (int y = 0; y < SrcBitmap.Height; y += OptimalH, ResultBmp.Count++)
                {
                    Bitmap tempBmp = Rescale(new Bitmap(SrcBitmap.Clone(new Rectangle(0, y, GlobW, (y + OptimalH <= GlobH ? OptimalH : GlobH - y)), SrcBitmap.PixelFormat)), (double)scale);
                    SendOrPostCallback action = (object objct) =>
                    {
                        F1.progressBar2.Value = 1;
                        F1.progressBar1.PerformStep();
                    };
                    context.Send(action, null);
                    Repaint(ResultBmp.Count, tempBmp, ui);
                }

                int pos = 0;
                for (int i = 0; i < ResultBmp.Count; i++, pos += OptimalH)
                {
                    Graphics grp = Graphics.FromImage(FinBmp);
                    Rectangle srcRect = new Rectangle(0, 0, GlobW, OptimalH);
                    Rectangle dstRect = new Rectangle(0, pos, GlobW, OptimalH);
                    grp.DrawImage(ResultBmp.Piece[i], dstRect, srcRect, GraphicsUnit.Pixel);
                    grp.Dispose();
                }

                ResultBmp.Piece = null;
                ResultBmp.Count = 0;

                return FinBmp;
            }

            public void Progress(object i)
            {
                int OptimalH = (int)i;
                F1.label1.Text = "Обработка изображения...";
                F1.label4.Text = "Пикселизация...";
                F1.label1.Refresh();
                F1.label4.Refresh();
                F1.progressBar1.Value = 1;
                F1.progressBar1.Maximum = SrcBitmap.Height / OptimalH + 1;
                F1.progressBar1.Step = 1;
            }
        }

        //--------------------------------------------------------
        //--------------------------------------------------------

        private void Work(object ui)
        {
            //MessageBox.Show(textBox1.Text + " " + textBox2.Text);
            //MessageBox.Show(Convert.ToString(int.Parse(textBox2.Text)));
            double h, w;
            try
            {
                h = double.Parse(textBox1.Text);
                w = double.Parse(textBox2.Text);
            }
            catch
            {
                return;
            }
            finally
            {

            }
            SynchronizationContext context = (SynchronizationContext)ui;
            Pixelization Job;
            Job = new Pixelization();
            Job.Clear();
            Job.Init(this, w, h, new Bitmap(SourcePicture.Image));
            Bitmap ResultBitmap = new Bitmap(Job.DoWork(ui));
            FinalPicture.Height = ResultBitmap.Height;
            FinalPicture.Width = ResultBitmap.Width;

            SendOrPostCallback Done = (object objct) =>
            {
                FinalPicture.Image = new Bitmap(ResultBitmap);
                DrawResultPicture(FinalPicture);
                ResultBitmap.Dispose();
                MessageBox.Show("Готово!");
                progressBar1.Value = 0;
                progressBar2.Value = 0;
                label1.Text = "Ожидание...";
                label4.Text = "Ожидание...";
                label1.Refresh();
                label4.Refresh();
            };
            context.Send(Done, context);
        }

    }

}
