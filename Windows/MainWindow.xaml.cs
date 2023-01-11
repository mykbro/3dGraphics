﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using _3dGraphics.Graphics;
using DrawingGraphics = System.Drawing.Graphics;
using DrawingPoint = System.Drawing.Point;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingPen = System.Drawing.Pen;

namespace _3dGraphics.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private readonly DrawingPen RenderPen = new DrawingPen(System.Drawing.Brushes.White, 0.25f);        

        //translations
        private readonly Action _leftArrowPressedCmd;
        private readonly Action _leftArrowReleasedCmd;
        private readonly Action _rightArrowPressedCmd;
        private readonly Action _rightArrowReleasedCmd;

        private readonly Action _upArrowPressedCmd;
        private readonly Action _upArrowReleasedCmd;
        private readonly Action _downArrowPressedCmd;
        private readonly Action _downArrowReleasedCmd;

        private readonly Action _pgUpPressedCmd;
        private readonly Action _pgUpReleasedCmd;
        private readonly Action _pgDownPressedCmd;
        private readonly Action _pgDownReleasedCmd;
        //rotations
        private readonly Action _wPressedCmd;
        private readonly Action _wReleasedCmd;
        private readonly Action _sPressedCmd;
        private readonly Action _sReleasedCmd;

        private readonly Action _dPressedCmd;
        private readonly Action _dReleasedCmd;
        private readonly Action _aPressedCmd;
        private readonly Action _aReleasedCmd;

        private readonly Action _qPressedCmd;
        private readonly Action _qReleasedCmd;
        private readonly Action _ePressedCmd;
        private readonly Action _eReleasedCmd;
        //FOV
        private readonly Action _plusPressedCmd;
        private readonly Action _plusReleasedCmd;
        private readonly Action _minusPressedCmd;
        private readonly Action _minusReleasedCmd;        



        public MainWindow(Action leftArrowPressedCmd, Action leftArrowReleasedCmd, Action rightArrowPressedCmd, Action rightArrowReleasedCmd,
                            Action upArrowPressedCmd, Action upArrowReleasedCmd, Action downArrowPressedCmd, Action downArrowReleasedCmd,
                            Action pgUpPressedCmd, Action pgUpReleasedCmd, Action pgDownPressedCmd, Action pgDownReleasedCmd,
                            Action wPressedCmd, Action wReleasedCmd, Action sPressedCmd, Action sReleasedCmd,
                            Action dPressedCmd, Action dReleasedCmd, Action aPressedCmd, Action aReleasedCmd,
                            Action qPressedCmd, Action qReleasedCmd, Action ePressedCmd, Action eReleasedCmd,
                            Action plusPressedCmd, Action plusReleasedCmd, Action minusPressedCmd, Action minusReleasedCmd)
        {
            _leftArrowPressedCmd= leftArrowPressedCmd;
            _leftArrowReleasedCmd= leftArrowReleasedCmd;
            _rightArrowPressedCmd = rightArrowPressedCmd;
            _rightArrowReleasedCmd= rightArrowReleasedCmd;
            _upArrowPressedCmd=upArrowPressedCmd;
            _upArrowReleasedCmd=upArrowReleasedCmd;
            _downArrowPressedCmd=downArrowPressedCmd;
            _downArrowReleasedCmd=downArrowReleasedCmd;
            _pgUpPressedCmd = pgUpPressedCmd;
            _pgUpReleasedCmd = pgUpReleasedCmd;
            _pgDownPressedCmd = pgDownPressedCmd;
            _pgDownReleasedCmd= pgDownReleasedCmd;      
            
            _wPressedCmd= wPressedCmd;
            _wReleasedCmd= wReleasedCmd;
            _sPressedCmd= sPressedCmd;
            _sReleasedCmd= sReleasedCmd;
            _dPressedCmd= dPressedCmd;
            _dReleasedCmd= dReleasedCmd;
            _aPressedCmd= aPressedCmd;
            _aReleasedCmd= aReleasedCmd;
            _ePressedCmd= ePressedCmd;
            _eReleasedCmd= eReleasedCmd;
            _qPressedCmd= qPressedCmd;
            _qReleasedCmd= qReleasedCmd;

            _plusPressedCmd= plusPressedCmd;
            _plusReleasedCmd= plusReleasedCmd;
            _minusPressedCmd= minusPressedCmd;
            _minusReleasedCmd = minusReleasedCmd;
            
            InitializeComponent();            
        }

        public int ScreenWidth => (int) _canvas.Width;
        public int ScreenHeight => (int)_canvas.Height;        


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
           

        }

        public void DrawLine(Point p1, Point p2)
        {            
            //_canvas.Children.Add(new Line() { X1 = p1.X, Y1 = p1.Y, X2 = p2.X, Y2 = p2.Y, Stroke = Brushes.White });
        }

        public void DrawFragment(Fragment f)
        {   
            Polygon p = new Polygon() { Points = { f.P1, f.P2, f.P3 }, Stroke = Brushes.White, StrokeThickness = 0.25 };
            //_canvas.Children.Add(p);
        }
        

        private void DrawFragmentOnGraphics(Fragment f, DrawingGraphics g)
        {          
            g.DrawLine(RenderPen, (float) f.P1.X, (float) f.P1.Y, (float) f.P2.X, (float) f.P2.Y);
            g.DrawLine(RenderPen, (float) f.P2.X, (float) f.P2.Y, (float) f.P3.X, (float) f.P3.Y);
            g.DrawLine(RenderPen, (float) f.P3.X, (float) f.P3.Y, (float) f.P1.X, (float) f.P1.Y);
        }



        public void DrawFragments(IEnumerable<Fragment> fragments)
        {
            /*
            foreach(Fragment f in fragments)
                DrawFragment(f);
            */            
            PixelFormat pf = PixelFormats.Bgr24;         
            WriteableBitmap wBmp = new WriteableBitmap(ScreenWidth, ScreenHeight, 96.0, 96.0, pf, null);            

            wBmp.Lock();    //we need to Lock even in the rendering thread because of the backbuffer usage
            using(DrawingBitmap bmp = new DrawingBitmap(ScreenWidth, ScreenHeight, wBmp.BackBufferStride, System.Drawing.Imaging.PixelFormat.Format24bppRgb, wBmp.BackBuffer))
            {
                using(DrawingGraphics g = DrawingGraphics.FromImage(bmp))
                {
                    foreach(Fragment f in fragments) 
                    {
                        DrawFragmentOnGraphics(f, g);
                    }
                }
            }
            wBmp.AddDirtyRect(new Int32Rect(0,0, ScreenWidth, ScreenHeight));
            wBmp.Unlock();
            _canvas.Source = wBmp;  
            
        }

        public void ClearCanvas() 
        { 
            //_canvas.Children.Clear(); 
        } 

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
           
            Key pressedKey = e.Key;

            switch (pressedKey)
            {
                case Key.Left:
                    _leftArrowPressedCmd.Invoke();
                    break;
                case Key.Right:
                    _rightArrowPressedCmd.Invoke();
                    break;
                case Key.Up:
                    _upArrowPressedCmd.Invoke();
                    break;
                case Key.Down:
                    _downArrowPressedCmd.Invoke();
                    break;
                case Key.PageUp:
                    _pgUpPressedCmd.Invoke();
                    break;
                case Key.PageDown:
                    _pgDownPressedCmd.Invoke();
                    break;
                case Key.W:
                    _wPressedCmd.Invoke();
                    break;
                case Key.S:
                    _sPressedCmd.Invoke();
                    break;
                case Key.D:
                    _dPressedCmd.Invoke();
                    break;
                case Key.A:
                    _aPressedCmd.Invoke();
                    break;
                case Key.Q:
                    _qPressedCmd.Invoke();
                    break;
                case Key.E:
                    _ePressedCmd.Invoke();
                    break;
                case Key.Add:
                    _plusPressedCmd.Invoke();
                    break;
                case Key.Subtract:
                    _minusPressedCmd.Invoke();
                    break;
                default:
                    break;
            }
        }

        private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
           
            Key releasedKey = e.Key;

            switch (releasedKey)
            {
                case Key.Left:
                    _leftArrowReleasedCmd.Invoke();
                    break;
                case Key.Right:
                    _rightArrowReleasedCmd.Invoke();
                    break;
                case Key.Up:
                    _upArrowReleasedCmd.Invoke();
                    break;
                case Key.Down:
                    _downArrowReleasedCmd.Invoke();
                    break;
                case Key.PageUp:
                    _pgUpReleasedCmd.Invoke();
                    break;
                case Key.PageDown:
                    _pgDownReleasedCmd.Invoke();
                    break;
                case Key.W:
                    _wReleasedCmd.Invoke();
                    break;
                case Key.S:
                    _sReleasedCmd.Invoke();
                    break;
                case Key.D:
                    _dReleasedCmd.Invoke();
                    break;
                case Key.A:
                    _aReleasedCmd.Invoke();
                    break;
                case Key.Q:
                    _qReleasedCmd.Invoke();
                    break;
                case Key.E:
                    _eReleasedCmd.Invoke();
                    break;
                case Key.Add:
                    _plusReleasedCmd.Invoke();
                    break;
                case Key.Subtract:
                    _minusReleasedCmd.Invoke();
                    break;
                default:
                    break;
            }
            
            
        }
    }
}
