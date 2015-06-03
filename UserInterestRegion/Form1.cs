using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace UserInterestRegion
{
    public partial class Form1 : Form
    {
        private List<Region> _regions = new List<Region>();
        private int _rectCount = 100;
        private RectangleF[] _rects;
        private int _width = 1000;
        private int _heigh = 600;
        private int _w = 10;
        private int _h = 10;
        private int _wOffset = 100;
        private int _hOffset = 75;


        public Form1()
        {
            InitializeComponent();
        }

        private void CreateRegion()
        {
            Random ran = new Random();
            _rects = new RectangleF[_rectCount];
            for (int i = 0; i < _rectCount - 20; i++)
            {
                float[] x = new float[2] { (float)(ran.NextDouble() * (_width - 2 * _w) + _wOffset), (float)(ran.NextDouble() * _w + _w) };
                float[] y = new float[2] { (float)(ran.NextDouble() * (_heigh - 2 * _h) + _hOffset), (float)(ran.NextDouble() * _h + _h) };
                //Array.Sort(x);
                //Array.Sort(y);
                Region reg = new Region();
                reg.LeftTop = new PointF(x[0] - _wOffset, y[0] - _hOffset);
                reg.RightBottom = new PointF(x[0] + x[1] - _wOffset, y[0] + y[1] - _hOffset);
                _rects[i] = new RectangleF(x[0], y[0], x[1], y[1]);
                _regions.Add(reg);
            }
            for (int i = 0; i < 20; i++)
            {
                float[] x = new float[2] { (float)(ran.NextDouble() * (100 - 2 * _w) + 400), (float)(ran.NextDouble() * _w + _w) };
                float[] y = new float[2] { (float)(ran.NextDouble() * (50 - 2 * _h) + 275), (float)(ran.NextDouble() * _h + _h) };
                //Array.Sort(x);
                //Array.Sort(y);
                Region reg = new Region();
                reg.LeftTop = new PointF(x[0] - _wOffset, y[0] - _hOffset);
                reg.RightBottom = new PointF(x[0] + x[1] - _wOffset, y[0] + y[1] - _hOffset);
                _rects[i] = new RectangleF(x[0], y[0], x[1], y[1]);
                _regions.Add(reg);
            }
        }

        private RectangleF InterestRegion(List<Region> regions, int control = 1, int count = 5)
        {
            RegionAnalyse analyse = new RegionAnalyse();
            //感兴趣区域提取
            //基本变量的设定
            int xs = control / 2 + control % 2;
            int ys = control / 2;
            int x = (int)Math.Pow(2, xs);
            int y = (int)Math.Pow(2, ys);
            float xstep = (float)_width / (float)x;
            float ystep = (float)_heigh / (float)y;
            analyse.Array = new double[x, y];
            analyse.MeshRegions = new MeshRegion[x, y];
            int xLPos = 0, yLPos = 0, xRPos = 0, yRPos = 0;
            analyse.MostObservePos = new List<Pos>(count);
            for (int i = 0; i < count; i++)
            {
                Pos p = new Pos();
                analyse.MostObservePos.Add(p);
            }
            //统计
            for (int i = 0; i < regions.Count; i++)
            {
                Region reg = regions[i];
                xLPos = (int)Math.Floor((double)reg.LeftTop.X / (double)xstep);
                yLPos = (int)Math.Floor((double)reg.LeftTop.Y / (double)ystep);
                xRPos = (int)Math.Floor((double)reg.RightBottom.X / (double)xstep);
                yRPos = (int)Math.Floor((double)reg.RightBottom.Y / (double)ystep);
                switch (ConfirmQuadrant(xLPos, yLPos, xRPos, yRPos))
                {
                    case 1:
                        analyse.Array[xLPos, yLPos]++;
                        if (analyse.Array[xLPos, yLPos] > analyse.Array[analyse.XMax, analyse.YMax])
                        {
                            analyse.XMax = xLPos;
                            analyse.YMax = yLPos;
                        }

                        ProcessRegion(ref analyse, xLPos, yLPos, reg, count);
                        break;
                    case 2:
                        for (int index = yLPos; index <= yRPos; index++)
                        {
                            analyse.Array[xLPos, index]++;
                            if (analyse.Array[xLPos, index] > analyse.Array[analyse.XMax, analyse.YMax])
                            {
                                analyse.XMax = xLPos;
                                analyse.YMax = index;
                            }

                            ProcessRegion(ref analyse, xLPos, index, reg, count);
                        }
                        break;
                    case 3:
                        for (int xi = xLPos; xi <= xRPos; xi++)
                            for (int yi = yLPos; yi <= yRPos; yi++)
                            {
                                analyse.Array[xi, yi]++;
                                if (analyse.Array[xi, yi] > analyse.Array[analyse.XMax, analyse.YMax])
                                {
                                    analyse.XMax = xi;
                                    analyse.YMax = yi;
                                }

                                ProcessRegion(ref analyse, xi, yi, reg, count);
                            }
                        break;
                    case 4:
                        for (int index = xLPos; index <= xRPos; index++)
                        {
                            analyse.Array[index, yLPos]++;
                            if (analyse.Array[index, yLPos] > analyse.Array[analyse.XMax, analyse.YMax])
                            {
                                analyse.XMax = index;
                                analyse.YMax = yLPos;
                            }

                            ProcessRegion(ref analyse, index, yLPos, reg, count);
                        }
                        break;
                    default:
                        break;
                }
            }
            //对统计结果分析
            RectangleF rect = new RectangleF(xstep * analyse.XMax, ystep * analyse.YMax, xstep, ystep);
            return rect;
        }

        private void ProcessRegion(ref RegionAnalyse analyse, int x, int y, Region reg, int count)
        {
            //矩形区所落网格区值自增一，并将矩形区记录到网格区的相应队列中
            if (analyse.MeshRegions[x, y] == null)
            {
                analyse.MeshRegions[x, y] = new MeshRegion();
            }
            analyse.MeshRegions[x, y].Value++;
            analyse.MeshRegions[x, y].Regions.Add(reg);
            //记录矩形区域出现频率较高的网格区
            Pos pp = new Pos(x, y);
            for (int j = 0; j < analyse.MostObservePos.Count; j++)
            {
                Pos P = analyse.MostObservePos[j];
                if (analyse.MeshRegions[P.X, P.Y] == null)
                {
                    analyse.MostObservePos.RemoveAt(count - 1);
                    analyse.MostObservePos.Insert(j, new Pos(x, y));
                    break;
                }
                if (analyse.MeshRegions[x, y].Value > analyse.MeshRegions[P.X, P.Y].Value)
                {
                    if (analyse.MostObservePos.Contains(pp))
                    {
                        analyse.MostObservePos.Remove(pp);
                        analyse.MostObservePos.Insert(j, pp);
                        break;
                    }
                    analyse.MostObservePos.RemoveAt(count - 1);
                    analyse.MostObservePos.Insert(j, pp);
                    break;
                }
            }
        }

        //确定矩形所属象限
        private int ConfirmQuadrant(int xL, int yL, int xR, int yR)
        {
            if (xL == xR)
            {
                if (yL == yR)
                    return 1;
                else if (yR > yL)
                    return 2;
            }
            if (xL < xR)
            {
                if (yL == yR)
                    return 4;
                else if (yR > yL)
                    return 3;
            }
            return 0;
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle validRect = new Rectangle(100, 75, 1000, 600);
            g.DrawRectangle(Pens.Black, validRect);
            CreateRegion();
            g.DrawRectangles(Pens.Red, _rects);
            RectangleF rec = InterestRegion(_regions, 8);
            g.DrawRectangle(Pens.Yellow, rec.X + _wOffset, rec.Y + _hOffset, rec.Width, rec.Height);
        }
    }

    public class RegionAnalyse
    {
        public int XMax = 0;
        public int YMax = 0;
        public double[,] Array;
        public MeshRegion[,] MeshRegions;
        public List<Pos> MostObservePos;
        public List<MeshRegion> MeshRegionList;
        public Dictionary<Pos, MeshRegion> MostObserveRegion;
    }

    public class Pos : IEquatable<Pos>
    {
        public Pos()
        { }

        public Pos(int x, int y)
        {
            X = x; Y = y;
        }

        public int X = 0;
        public int Y = 0;

        public bool Equals(Pos other)
        {
            if (other == null)
                return false;
            return (this.X == other.X && this.Y == other.Y);
        }
    }

    public class MeshRegion
    {
        public int X;
        public int Y;
        public int Value = 0;
        public List<Region> Regions = new List<Region>();
    }

    public class Region
    {
        public PointF LeftTop;
        public PointF RightBottom;
    }
}
