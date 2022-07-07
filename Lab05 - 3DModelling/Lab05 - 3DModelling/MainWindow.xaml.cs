using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
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
        private int _cPosZ = -400;
        private double _lightX = 33;
        private double _lightY = 33;
        private double _lightZ = -80;
        private float _kA = 0.2f;
        private float _kD = 0.65f;
        private float _kS = 0.85f;
        private int _n = 20;
        private bool _isLightOn = false;
        private bool _isMeshOn = true;
        private Vector3 _lightPos;
        private Matrix4x4 _invTransformCameraMatrix;
        private Color _modelColor = Colors.DarkGreen;
        private IMeshable _model = new Sphere(15, 15, 50);

        public MainWindow()
        {
            InitializeComponent();
            DrawModel(_model);
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
            var M = Matrix4x4.Multiply(P, Matrix4x4.Multiply(C, Matrix4x4.Multiply(Rx, Matrix4x4.Multiply(Ry, Rz))));

            // inverse transform matrix for camera to be used in Phong illumination
            Matrix4x4.Invert(Matrix4x4.Multiply(C, Matrix4x4.Multiply(Rx, Matrix4x4.Multiply(Ry, Rz))), out _invTransformCameraMatrix);

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

            var newCanvas = new WriteableBitmap((int)CanvasImage.Width, (int)CanvasImage.Height, 96, 96, PixelFormats.Bgra32,
                BitmapPalettes.Halftone256);
            model.Draw(newCanvas, _texture, lightAttributes);
            _wbm = newCanvas;
            CanvasImage.Source = _wbm;
        }

        // ---------------- HELPER FUNCTIONS --------------------

        private Phong CalculateLightAttributes()
        {
            var invTransformedCamera = Vector3.Transform(new Vector3(0, 0, 0), _invTransformCameraMatrix);
            return new Phong ( _isMeshOn, _isLightOn, invTransformedCamera, _lightPos, _modelColor, _kA, _kD, _kS, _n );
        }
        private void RefreshModel()
        {
            _model.ClearVertices();
            _model.CalculateVertices();
        }
        private void ResetTransformations()
        {
            AngleXSlider.Value = 0;
            AngleYSlider.Value = 0;
            AngleZSlider.Value = 0;
            CamXslider.Value = 0;
            CamYslider.Value = 0;
            CamZslider.Value = -400;
            kDslider.Value = 0.65f;
            kSslider.Value = 0.85f;
            LightXslider.Value = 33;
            LightYslider.Value = 33;
            LightZslider.Value = -80;
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
            RefreshModel();
            ResetTransformations();
            DrawModel(_model);
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
            RefreshModel();
            ResetTransformations();
            DrawModel(_model);
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
            _model = new Cuboid(50, 50, 50);
            HeightSlider.Value = 50;
            DepthSlider.Value = 50;
            WidthSlider.Value = 50;
            CylinderButton.Visibility = Visibility.Visible;
            SphereButton.Visibility = Visibility.Hidden;
            SphereButton2.Visibility = Visibility.Visible;
            CuboidButton.Visibility = Visibility.Hidden;
            RefreshModel();
            ResetTransformations();
            DrawModel(_model);
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
                ModelColorButton.Stroke = new SolidColorBrush(modelColor);
                _modelColor = modelColor;
            }
            
            DrawModel(_model);
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

            DrawModel(_model);
        }
        private void ClearTextureButton_OnClick(object sender, RoutedEventArgs e)
        {
            _texture = null;
            DrawModel(_model);
        }
        private void LightOnButton_OnClick(object sender, RoutedEventArgs e)
        {
            _isLightOn = true;
            LightOnButton.Visibility = Visibility.Hidden;
            LightOffButton.Visibility = Visibility.Visible;
            HideMesh();
        }
        private void LightOffButton_OnClick(object sender, RoutedEventArgs e)
        {
            _isLightOn = false;
            LightOnButton.Visibility = Visibility.Visible;
            LightOffButton.Visibility = Visibility.Hidden;
            ShowMesh();
        }
        private void HideMeshButton_OnClick(object sender, RoutedEventArgs e)
        {
            HideMesh();
        }
        private void HideMesh()
        {
            _isMeshOn = false;
            HideMeshButton.Visibility = Visibility.Hidden;
            ShowMeshButton.Visibility = Visibility.Visible;
            DrawModel(_model);
        }
        private void ShowMeshButton_OnClick(object sender, RoutedEventArgs e)
        {
            ShowMesh();
        }
        private void ShowMesh()
        {
            _isMeshOn = true;
            ShowMeshButton.Visibility = Visibility.Hidden;
            HideMeshButton.Visibility = Visibility.Visible;
            DrawModel(_model);
        } private void OnClick_Exit(object sender, RoutedEventArgs e)
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
            if (MText is not null) MText.Text = ((int)Mslider.Value).ToString();
            RefreshModel();
            DrawModel(_model);
        }
        private void Nslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _model.N = (int)Nslider.Value;
            if (NText is not null) NText.Text = ((int)Nslider.Value).ToString();
            RefreshModel();
            DrawModel(_model);
        }
        private void WidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _model.Width = (int)WidthSlider.Value;
            if (WidthText is not null) WidthText.Text = ((int)WidthSlider.Value).ToString();
            RefreshModel();
            DrawModel(_model);
        }
        private void HeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _model.Height = (int)HeightSlider.Value;
            if (HeightText is not null) HeightText.Text = ((int)HeightSlider.Value).ToString();
            RefreshModel();
            DrawModel(_model);
        }
        private void DepthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _model.Depth = (int)DepthSlider.Value;
            if (DepthText is not null) DepthText.Text = ((int)DepthSlider.Value).ToString();
            RefreshModel();
            DrawModel(_model);
        }
        private void RadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _model.Radius = (int)RadiusSlider.Value;
            if (RadiusText is not null) RadiusText.Text = ((int)RadiusSlider.Value).ToString();
            RefreshModel();
            DrawModel(_model);
        }

        //Rotation
        private void AngleXSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _angleX = AngleXSlider.Value;
            if (AngleXText is not null) AngleXText.Text = ((int)AngleXSlider.Value).ToString();
            DrawModel(_model);
        }
        private void AngleYSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _angleY = AngleYSlider.Value;
            if (AngleYText is not null) AngleYText.Text = ((int)AngleYSlider.Value).ToString();
            DrawModel(_model);
        }
        private void AngleZSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _angleZ = AngleZSlider.Value;
            if (AngleZText is not null) AngleZText.Text = ((int)AngleZSlider.Value).ToString();
            DrawModel(_model);
        }

        // Camera
        private void CamXslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _cPosX = -(int)CamXslider.Value;
            if (CamXText is not null) CamXText.Text = ((int)CamXslider.Value).ToString();
            DrawModel(_model);
        }
        private void CamYslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _cPosY = -(int)CamYslider.Value;
            if (CamYText is not null) CamYText.Text = ((int)CamYslider.Value).ToString();
            //RefreshModel();
            DrawModel(_model);
        }
        private void CamZslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _cPosZ = -(int)CamZslider.Value;
            if (CamZText is not null) CamZText.Text = ((int)CamZslider.Value).ToString();
            DrawModel(_model);
        }

        // Light
        private void LightXslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _lightX = LightXslider.Value;
            _lightPos = new Vector3((float)_lightX, (float)_lightY, (float)_lightZ);
            if (LightXText is not null) LightXText.Text = ((int)LightXslider.Value).ToString();
            DrawModel(_model);
        }
        private void LightYslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _lightY = LightYslider.Value;
            _lightPos = new Vector3((float)_lightX, (float)_lightY, (float)_lightZ);
            if (LightYText is not null) LightYText.Text = ((int)LightYslider.Value).ToString();
            DrawModel(_model);
        }
        private void LightZslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _lightZ = LightZslider.Value;
            _lightPos = new Vector3((float)_lightX, (float)_lightY, (float)_lightZ);
            if (LightZText is not null) LightZText.Text = ((int)LightZslider.Value).ToString();
            DrawModel(_model);
        }
        private void kDslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            
            _kD = (float)kDslider.Value;
            if (kDText is not null) kDText.Text = ((float)kDslider.Value).ToString();
            DrawModel(_model);
        }
        private void kSslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _kS = (float)kSslider.Value;
            if (kSText is not null) kSText.Text = ((float)kSslider.Value).ToString();
            DrawModel(_model);
        }
        private void kAslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _kA = (float)kAslider.Value;
            if (kAText is not null) kAText.Text = ((float)kAslider.Value).ToString();
            DrawModel(_model);
        }
        private void n_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _n = (int)nslider.Value;
            if (nText is not null) nText.Text = ((int)nslider.Value).ToString();
            DrawModel(_model);
        }
    }
}