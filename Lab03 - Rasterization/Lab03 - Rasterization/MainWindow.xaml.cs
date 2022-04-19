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

// TODO check for out of bounds of canvas! maybe set canvas size to match panel size?

namespace Lab03___Rasterization
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isDrawingLine;
        private bool _isDrawingPolygon;
        private bool _isDrawingCircle;
        private List<Point> _currentPoints = new();
        private readonly List<IDrawable> _allShapes = new();
        private readonly WriteableBitmap _whiteWbm;
        private WriteableBitmap _wbm;
        private Polygon currentPolygon;
        private bool _isDraggingVertex;


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
        private void OnClick_ResetCanvas(object sender, RoutedEventArgs e)
        {
            _allShapes.Clear();
            ClearCanvas();
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

        private int _currentShapeIndex;
        private int _currentPointIndex;
        private int _currentLineIndex;
        private void TheCanvas_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var cursorPosition = e.GetPosition(TheCanvas);
            Debug.WriteLine("CLICKED!");
            Debug.WriteLine($"{cursorPosition.X}, {cursorPosition.Y}");

            if (_isDrawingLine) DrawLine(cursorPosition);
            else if (_isDrawingPolygon) DrawPolygon(cursorPosition);
            else
            {
                foreach (var shape in _allShapes)
                {
                    var shapeIndex = _allShapes.IndexOf(shape);
                    var points = shape.GetPoints();
                    foreach (var point in points)
                    {
                        var pointIndex = points.IndexOf(point);
                        if (calculateDistance(point, cursorPosition) < 10)
                        {
                            Debug.WriteLine("VERTEX!");
                            _isDraggingVertex = true;
                            _currentPoints = points;
                            _currentPointIndex = pointIndex;
                            _currentShapeIndex = shapeIndex;
                            return;
                        }
                    }
                }
            }
        }
        private void TheCanvas_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingVertex)
            {
                _isDraggingVertex = false;
                _currentPoints.Clear();
            }
        }

        private void TheCanvas_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ToggleAllOff();
        }

        private void TheCanvas_OnMouseMove(object sender, MouseEventArgs e)
        {
            Line currentLine;
            var cursorPosition = e.GetPosition(TheCanvas);

            // draw line preview
            if (_isDrawingLine && _currentPoints.Count > 0)
            {
                ClearCanvas();
                DrawAllShapes();
                currentLine = new Line(new List<Point> { _currentPoints[0], cursorPosition });
                currentLine.Draw(_wbm);
            }
            
            // draw polygon preview
            if (_isDrawingPolygon && _currentPoints.Count > 0)
            {
                ClearCanvas();
                DrawAllShapes();
                for (int i = 0; i < _currentPoints.Count - 1; i++)
                {
                    currentLine = new Line(new List<Point> { _currentPoints[i], _currentPoints[i+1] });
                    currentLine.Draw(_wbm);
                }

                currentLine = new Line(new List<Point> { _currentPoints[^1], cursorPosition });
                currentLine.Draw(_wbm);
            }

            if (_isDraggingVertex)
            {
                _currentPoints[_currentPointIndex] = cursorPosition;
                _allShapes[_currentShapeIndex].SetPoints(new List<Point>(_currentPoints));

                ClearCanvas();
                DrawAllShapes();
            }
        }


        private void ToggleAllOff()
        {
            if (_isDrawingLine) ToggleIsDrawingLine();
            else if (_isDrawingPolygon) ToggleIsDrawingPolygon();

            _currentPoints.Clear();
            ClearCanvas();
            DrawAllShapes();
        }

        private void DrawLine(Point clickPosition)
        {
            if (_currentPoints.Count == 0) // add startPoint
            {
                _currentPoints.Add(clickPosition);
                Debug.WriteLine($"Starting: {clickPosition.X}, {clickPosition.Y}");
            }
            else // add endPoint and add Line to _allShapes
            {
                _currentPoints.Add(clickPosition);
                Debug.WriteLine($"Ending: {clickPosition.X}, {clickPosition.Y}");
                _allShapes.Add(new Line(new List<Point>(_currentPoints)));
                
                // clear the points
                _currentPoints.Clear();
                
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
            ToggleAllOff();
            ToggleIsDrawingLine();
        }

        private void ToggleIsDrawingLine()
        {
            _isDrawingLine = !_isDrawingLine;
            LineButton.Background = _isDrawingLine ? Brushes.LightSalmon : Brushes.LightCyan;
        }

        

        private void PolygonButton_OnClick(object sender, RoutedEventArgs e)
        {
            ToggleAllOff();
            ToggleIsDrawingPolygon();
        }

        private void ToggleIsDrawingPolygon()
        {
            

            _isDrawingPolygon = !_isDrawingPolygon;
            PolygonButton.Background = _isDrawingPolygon ? Brushes.LightSalmon : Brushes.LightCyan;
        }

        private void DrawPolygon(Point clickPosition)
        {
            if (_currentPoints.Count == 0)
            {
                _currentPoints.Add(clickPosition);
                Debug.WriteLine($"Starting: {clickPosition.X}, {clickPosition.Y}");
            }
            else if (_currentPoints.Count < 2)
            {
                _currentPoints.Add(clickPosition);
                Debug.WriteLine($"PolygonPoint: {clickPosition.X}, {clickPosition.Y}");
            }
            else
            {
                if (calculateDistance(_currentPoints[0], clickPosition) < 10)
                {
                    Debug.WriteLine($"Ending: {clickPosition.X}, {clickPosition.Y}");
                    _allShapes.Add(new Polygon(new List<Point>(_currentPoints)));
                    _currentPoints.Clear(); // when the polygon is finished
                    Debug.WriteLine("Clearing _points");
                    ToggleIsDrawingPolygon();
                    ClearCanvas();
                    DrawAllShapes();
                }
                else
                {
                    _currentPoints.Add(clickPosition);
                    Debug.WriteLine($"PolygonPoint: {clickPosition.X}, {clickPosition.Y}");
                }
            }
        }

        private int calculateDistance(Point p1, Point p2)
        {
            return (int)Math.Round(Math.Sqrt(Math.Pow((p2.X - p1.X), 2) + Math.Pow((p2.Y - p1.Y), 2)));
        }


        
    }
}
