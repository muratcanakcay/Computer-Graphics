using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Numerics;
using System.Windows.Input;
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
        private int _sx = 750;
        private int _sy = 450;
        private int _cPosX = 0;
        private int _cPosY = 0;
        private int _cPosZ = 400;
        private double lightX = 0;
        private double lightY = 25;
        private double lightZ = 0;
        private bool _isLightOn = false;
        private Vector3 Light;
        private Color _modelColor = Colors.Gray;
        private IMeshable _model = new Cylinder(15, 50, 20);
        

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
            
            model.ClearVertices();
            model.Vertices = projectedPoints;
        }
        public void DrawModel(IMeshable model)
        {
            TransformAndProject(model);
            var lightAttributes = CalculateLightAttributes();
            model.Draw(_wbm, _texture, lightAttributes);
        }

        // ---------------- HELPER FUNCTIONS --------------------

        private Phong CalculateLightAttributes()
        {
            return new Phong
            {
                IsIlluminated = _isLightOn,
                Camera = new Vector3(_cPosX, _cPosY, _cPosZ),
                Light = Light,
                ModelColor = _modelColor
            };
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
            _wbm = new WriteableBitmap((int)CanvasImage.Width, (int)CanvasImage.Height, 96, 96, PixelFormats.Bgra32,
                BitmapPalettes.Halftone256);
            CanvasImage.Source = _wbm;
        }
        private void ResetTransformations()
        {
            AngleXSlider.Value = 0;
            AngleYSlider.Value = 0;
            AngleZSlider.Value = 0;
            CamXslider.Value = 0;
            CamYslider.Value = 0;
            CamZslider.Value = 400;
            SxSlider.Value = 750;
            SySlider.Value = 450;
        }


        // ---------------- BUTTONS --------------------

        private void CylinderButton_OnClick(object sender, RoutedEventArgs e)
        {
            ModelName.Text = "Cylinder";
            ModelProperties.RowDefinitions[0].Height = new GridLength(0);
            ModelProperties.RowDefinitions[1].Height = new GridLength(26);
            ModelProperties.RowDefinitions[2].Height = new GridLength(0);
            ModelProperties.RowDefinitions[3].Height = new GridLength(26);
            ModelProperties.RowDefinitions[4].Height = new GridLength(0);
            ModelProperties.RowDefinitions[5].Height = new GridLength(26);
            _model = new Cylinder(15, 50, 30);
            Nslider.Value = 15;
            HeightSlider.Value = 50;
            RadiusSlider.Value = 30;
            CylinderButton.Visibility = Visibility.Hidden;
            SphereButton.Visibility = Visibility.Visible;
            SphereButton2.Visibility = Visibility.Hidden;
            CuboidButton.Visibility = Visibility.Visible;
            ResetTransformations();
            RefreshCanvasAndModel();
        }
        private void SphereButton_OnClick(object sender, RoutedEventArgs e)
        {
            ModelName.Text = "Sphere";
            ModelProperties.RowDefinitions[0].Height = new GridLength(26);
            ModelProperties.RowDefinitions[1].Height = new GridLength(26);
            ModelProperties.RowDefinitions[2].Height = new GridLength(0);
            ModelProperties.RowDefinitions[3].Height = new GridLength(00);
            ModelProperties.RowDefinitions[4].Height = new GridLength(0);
            ModelProperties.RowDefinitions[5].Height = new GridLength(26);
            _model = new Sphere(15, 15, 50);
            Mslider.Value = 15;
            Nslider.Value = 15;
            RadiusSlider.Value = 50;
            CylinderButton.Visibility = Visibility.Visible;
            SphereButton.Visibility = Visibility.Hidden;
            SphereButton2.Visibility = Visibility.Hidden;
            CuboidButton.Visibility = Visibility.Visible;
            ResetTransformations();
            RefreshCanvasAndModel();
        }
        private void CuboidButton_OnClick(object sender, RoutedEventArgs e)
        {
            ModelName.Text = "Cuboid";
            ModelProperties.RowDefinitions[0].Height = new GridLength(0);
            ModelProperties.RowDefinitions[1].Height = new GridLength(0);
            ModelProperties.RowDefinitions[2].Height = new GridLength(26);
            ModelProperties.RowDefinitions[3].Height = new GridLength(26);
            ModelProperties.RowDefinitions[4].Height = new GridLength(26);
            ModelProperties.RowDefinitions[5].Height = new GridLength(0);
            _model = new Cuboid(50, 50, -50);
            HeightSlider.Value = 50;
            DepthSlider.Value = 50;
            WidthSlider.Value = 50;
            CylinderButton.Visibility = Visibility.Visible;
            SphereButton.Visibility = Visibility.Hidden;
            SphereButton2.Visibility = Visibility.Visible;
            CuboidButton.Visibility = Visibility.Hidden;
            ResetTransformations();
            RefreshCanvasAndModel();
        }
        private void SelectModelColorButton_OnClick(object sender, MouseButtonEventArgs e)
        {
            using var colorDialog = new System.Windows.Forms.ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var modelColor = Color.FromArgb(
                    colorDialog.Color.A,
                    colorDialog.Color.R,
                    colorDialog.Color.G,
                    colorDialog.Color.B);
                
                ModelColorButton.Fill = new SolidColorBrush(modelColor);
                _modelColor = modelColor;
            }
            
            RefreshCanvasAndModel();
        }
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
        private void LightOnButton_OnClick(object sender, RoutedEventArgs e)
        {
            _isLightOn = true;
            LightOnButton.Visibility = Visibility.Hidden;
            LightOffButton.Visibility = Visibility.Visible;
            RefreshCanvasAndModel();
        }
        private void LightOffButton_OnClick(object sender, RoutedEventArgs e)
        {
            _isLightOn = false;
            LightOnButton.Visibility = Visibility.Visible;
            LightOffButton.Visibility = Visibility.Hidden;
            RefreshCanvasAndModel();
        }
        private void OnClick_Exit(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                    "Are you sure you want to leave?", "Exit", MessageBoxButton.YesNo)
                == MessageBoxResult.Yes)
                Close();
        }

        // ---------------- SLIDERS --------------------

        private void Mslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _model.M = (int)Mslider.Value;
            RefreshCanvasAndModel();
        }
        private void Nslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _model.N = (int)Nslider.Value;
            RefreshCanvasAndModel();
        }
        private void WidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _model.Width = (int)WidthSlider.Value;
            RefreshCanvasAndModel();
        }
        private void HeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _model.Height = (int)HeightSlider.Value;
            RefreshCanvasAndModel();
        }
        private void DepthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _model.Depth = -(int)DepthSlider.Value;
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

        // Light
        private void LightXslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lightX = -LightXslider.Value;
            Light = new Vector3((float)lightX, (float)lightY, (float)lightZ);
            RefreshCanvasAndModel();
        }

        private void LightYslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lightY = LightYslider.Value;
            Light = new Vector3((float)lightX, (float)lightY, (float)lightZ);
            RefreshCanvasAndModel();
        }

        private void LightZslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lightZ = LightZslider.Value;
            Light = new Vector3((float)lightX, (float)lightY, (float)lightZ);
            RefreshCanvasAndModel();
        }
    }
}