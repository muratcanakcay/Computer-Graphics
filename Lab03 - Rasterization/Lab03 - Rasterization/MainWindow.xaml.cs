using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Color = System.Drawing.Color;


namespace Lab03___Rasterization
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isDrawingLine;
        private bool _isDrawingPolygon;
        private static readonly Point NullPoint = new(-1, -1);
        private readonly List<Point> _points = new();
        private readonly List<IDrawable> _allShapes = new();
        private readonly WriteableBitmap _whiteWbm;
        private WriteableBitmap _wbm;
        private Polygon currentPolygon;
        

        public MainWindow()
        {
            InitializeComponent();
            _whiteWbm = InitializeCanvas(ref _wbm);
        }

        private WriteableBitmap InitializeCanvas(ref WriteableBitmap _wbm)
        {
            var whiteWbm = new WriteableBitmap((int)TheCanvas.Width,
                (int)TheCanvas.Height, 
                96, 
                96, 
                PixelFormats.Bgr32, 
                null);

            try
            {
                whiteWbm.Lock();
                for (int x = 0; x < whiteWbm.Width; x++)
                    for (int y = 0; y < whiteWbm.Height; y++)
                        whiteWbm.SetPixelColor(x, y, Color.FromArgb(255, 255, 255, 255));
            }
            finally
            {
                whiteWbm.Unlock();
            }

            _wbm = whiteWbm.Clone();
            var brush = new ImageBrush { ImageSource = _wbm };
            TheCanvas.Background = brush;

            return whiteWbm;
        }

        private void dummyCallBack(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ClearCanvas()
        {
            _wbm = _whiteWbm.Clone();
            var brush = new ImageBrush
            {
                ImageSource = _wbm
            };

            TheCanvas.Background = brush;
        }


        private void TheCanvas_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var clickPosition = e.GetPosition(TheCanvas);
            Debug.WriteLine("CLICKED!");
            Debug.WriteLine($"{clickPosition.X}, {clickPosition.Y}");

            if (_isDrawingLine) DrawLine(clickPosition);
            if (_isDrawingPolygon) DrawPolygon(clickPosition);
        }

        private void DrawLine(Point clickPosition)
        {
            if (_points.Count == 0)
            {
                _points.Add(clickPosition);
                Debug.WriteLine($"Starting: {clickPosition.X}, {clickPosition.Y}");
            }
            else
            {
                _points.Add(clickPosition);
                Debug.WriteLine($"Ending: {clickPosition.X}, {clickPosition.Y}");
                _allShapes.Add(new Line(new List<Point>(_points)));
                _points.Clear();
                
                ToggleIsDrawingLine();
                ClearCanvas();
                DrawAllShapes();
            }
        }

        private void DrawAllShapes()
        {
            foreach (var shape in _allShapes)
            {
                shape.Draw(_wbm);
            }
        }

        private void LineButton_OnClick(object sender, RoutedEventArgs e)
        {
            ToggleIsDrawingLine();
        }

        private void ToggleIsDrawingLine()
        {
            //TODO toggle All "isDrawing"s off first!!
            _isDrawingLine = !_isDrawingLine;
            LineButton.Background = _isDrawingLine ? Brushes.LightSalmon : Brushes.LightCyan;
        }

        private void TheCanvas_OnMouseMove(object sender, MouseEventArgs e)
        {
            Line currentLine;
            var cursorPosition = e.GetPosition(TheCanvas);

            // draw line preview
            if (_isDrawingLine && _points.Count > 0)
            {
                ClearCanvas();
                DrawAllShapes();
                currentLine = new Line(new List<Point> { _points[0], cursorPosition });
                currentLine.Draw(_wbm);
            }
            if (_isDrawingPolygon && _points.Count > 0)
            {
                ClearCanvas();
                DrawAllShapes();
                for (int i = 0; i < _points.Count - 1; i++)
                {
                    currentLine = new Line(new List<Point> { _points[i], _points[i+1] });
                    currentLine.Draw(_wbm);
                }

                currentLine = new Line(new List<Point> { _points[^1], cursorPosition });
                currentLine.Draw(_wbm);
            }
        }

        private void PolygonButton_OnClick(object sender, RoutedEventArgs e)
        {
            ToggleIsDrawingPolygon();
        }

        private void ToggleIsDrawingPolygon()
        {
            //TODO toggle All "isDrawing"s off first!!
            _isDrawingPolygon = !_isDrawingPolygon;
            PolygonButton.Background = _isDrawingPolygon ? Brushes.LightSalmon : Brushes.LightCyan;
        }

        private void DrawPolygon(Point clickPosition)
        {
            if (_points.Count == 0)
            {
                _points.Add(clickPosition);
                Debug.WriteLine($"Starting: {clickPosition.X}, {clickPosition.Y}");
            }
            else if (_points.Count < 2)
            {
                _points.Add(clickPosition);
                Debug.WriteLine($"PolygonPoint: {clickPosition.X}, {clickPosition.Y}");
            }
            else
            {
                if (calculateDistance(_points[0], clickPosition) < 10)
                {
                    Debug.WriteLine($"Ending: {clickPosition.X}, {clickPosition.Y}");
                    _allShapes.Add(new Polygon(new List<Point>(_points)));
                    _points.Clear(); // when the polygon is finished
                    Debug.WriteLine("Clearing _points");
                    ToggleIsDrawingPolygon();
                    ClearCanvas();
                    DrawAllShapes();
                }
                else
                {
                    _points.Add(clickPosition);
                    Debug.WriteLine($"PolygonPoint: {clickPosition.X}, {clickPosition.Y}");
                }
            }
        }

        private int calculateDistance(Point p1, Point p2)
        {
            return (int)Math.Round(Math.Sqrt(Math.Pow((p2.X - p1.X), 2) + Math.Pow((p2.Y - p1.Y), 2)));
        }

        private void OnClick_ClearCanvas(object sender, RoutedEventArgs e)
        {
            _allShapes.Clear();
            ClearCanvas();
        }
    }
}
