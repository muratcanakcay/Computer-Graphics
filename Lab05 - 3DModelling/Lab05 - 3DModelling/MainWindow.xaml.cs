using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Numerics;
using Microsoft.Win32;

namespace Lab05___3DModelling
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WriteableBitmap _wbm;
        private WriteableBitmap? _texture = null;
        private double _angleX = 0;
        private double _angleY = 0;
        private double _angleZ = 0;
        private int _sx = 300;
        private int _sy = 300;
        private int _cPosX = 0;
        private int _cPosY = 0;
        private int _cPosZ = 200;
        //private readonly Cylinder _model = new(15, 50, 20);
        private readonly Sphere _model = new(15, 15, 20);

        public MainWindow()
        {
            InitializeComponent();
            RefreshCanvasAndModel();
        }

        //----------------- PROJECTION -------------------

        public void TransformAndProject(IMeshable model)
        {
            // does not transform the normal vector, just the coordinates
            var vertices = model.Vertices;
            var projectedPoints = new List<Point3d>();

            var alphaX = Math.PI * _angleX / 180.0;
            var alphaY = Math.PI * _angleY / 180.0;
            var alphaZ = Math.PI * _angleZ / 180.0;
            const double theta = Math.PI / 8;
            
            var Rx = new Matrix4x4(1, 0, 0, 0,
                                         0, (float)Math.Cos(alphaX), -(float)Math.Sin(alphaX), 0,
                                         0, (float)Math.Sin(alphaX), (float)Math.Cos(alphaX), 0,
                                         0, 0, 0, 1);

            var Ry = new Matrix4x4((float)Math.Cos(alphaY), 0, (float)Math.Sin(alphaY), 0,
                                         0, 1, 0, 0,
                                         -(float)Math.Sin(alphaY), 0, (float)Math.Cos(alphaY), 0,
                                         0, 0, 0, 1);

            var Rz = new Matrix4x4((float)Math.Cos(alphaZ), -(float)Math.Sin(alphaZ), 0, 0,
                                         (float)Math.Sin(alphaZ), (float)Math.Cos(alphaZ), 0, 0,
                                         0, 0, 1, 0,
                                         0, 0, 0, 1);

            

            var P = new Matrix4x4(-_sx / (float)Math.Tan(-theta), 0, _sx, 0,
                0, _sx / (float)Math.Tan(-theta), _sy, 0,
                0, 0, 0, 1,
                0, 0, 1, 0);
            
            var cameraPos = new Vector3(_cPosX, _cPosY, _cPosZ);
            var cameraTarget = new Vector3(0, 0, 0);
            var cameraUp = new Vector3(0, 1, 0);

            var cZ = Vector3.Normalize(Vector3.Subtract(cameraPos, cameraTarget));
            var cX = Vector3.Normalize(Vector3.Cross(cameraUp, cZ));
            var cY = Vector3.Normalize(Vector3.Cross(cZ, cX));

            var C = new Matrix4x4(cX.X, cX.Y, cX.Z, Vector3.Dot(cX, cameraPos),
                cY.X, cY.Y, cY.Z, Vector3.Dot(cY, cameraPos),
                cZ.X, cZ.Y, cZ.Z, Vector3.Dot(cZ, cameraPos),
                0, 0, 0, 1);

            
            // final transformation matrix    
            Matrix4x4 M = Matrix4x4.Multiply(P, Matrix4x4.Multiply(C, Matrix4x4.Multiply(Rx, Matrix4x4.Multiply(Ry, Rz))));

            foreach (var v in vertices)
            {

                var x = M.M11 * v.Global.X + M.M12 * v.Global.Y + M.M13 * v.Global.Z + M.M14 * v.Global.W;
                var y = M.M21 * v.Global.X + M.M22 * v.Global.Y + M.M23 * v.Global.Z + M.M24 * v.Global.W;
                var z = M.M31 * v.Global.X + M.M32 * v.Global.Y + M.M33 * v.Global.Z + M.M34 * v.Global.W;
                var w = M.M41 * v.Global.X + M.M42 * v.Global.Y + M.M43 * v.Global.Z + M.M44 * v.Global.W;

                x /= w;
                y /= w;
                z /= w;
                w = 1;

                projectedPoints.Add(new Point3d
                {
                    Projected = new Point4(x, y, z, w),
                    Global = v.Global,
                    Normal = v.Normal,
                    TextureMap = v.TextureMap
                });
            }

            model.Vertices = projectedPoints;
        }
        public void DrawModel(IMeshable model)
        {
            TransformAndProject(model);
            model.Draw(_wbm, _texture);
        }

        // ---------------- HELPER FUNCTIONS --------------------

        private void SelectTextureButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog()
            {
                Filter = "Image files|*.jpg;*.png;*.bmp",
                RestoreDirectory = true
            };

            if (dlg.ShowDialog() != true)
                return;

            try
            {
                _texture = new WriteableBitmap(new BitmapImage(new Uri(dlg.FileName)));
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

            RefreshCanvasAndModel();
        }
        private void ClearTextureButton_OnClick(object sender, RoutedEventArgs e)
        {
            _texture = null;
            RefreshCanvasAndModel();
        }
        private void RefreshCanvasAndModel()
        {
            ClearCanvas();
            _model.ClearVertices();
            _model.CalculateVertices();
            DrawModel(_model);
        }
        private void ClearCanvas()
        {
            _wbm = new WriteableBitmap((int)Canvas.Width, (int)Canvas.Height, 96, 96, PixelFormats.Bgra32,
                BitmapPalettes.Halftone256);
            Canvas.Source = _wbm;
        }


        // ---------------- SLIDERS --------------------

        private void Nslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _model.N = (int)Nslider.Value;
            RefreshCanvasAndModel();
        }
        private void Mslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _model.M = (int)Mslider.Value;
            RefreshCanvasAndModel();
        }
        private void HeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _model.Height = (int)HeightSlider.Value;
            RefreshCanvasAndModel();
        }
        private void RadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _model.Radius = (int)RadiusSlider.Value;
            RefreshCanvasAndModel();
        }

        //Rotation
        private void AngleXSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _angleX = AngleXSlider.Value;
            RefreshCanvasAndModel();
        }
        private void AngleYSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _angleY = AngleYSlider.Value;
            RefreshCanvasAndModel();
        }
        private void AngleZSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _angleZ = AngleZSlider.Value;
            RefreshCanvasAndModel();
        }

        //Translation
        private void SxSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _sx = (int)SxSlider.Value;
            RefreshCanvasAndModel();
        }
        private void SySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _sy = (int)SySlider.Value;
            RefreshCanvasAndModel();
        }

        // Camera
        private void CamXslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _cPosX = (int)CamXslider.Value;
            RefreshCanvasAndModel();
        }
        private void CamYslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _cPosY = (int)CamYslider.Value;
            RefreshCanvasAndModel();
        }
        private void CamZslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _cPosZ = (int)CamZslider.Value;
            RefreshCanvasAndModel();
        }

      
    }
}