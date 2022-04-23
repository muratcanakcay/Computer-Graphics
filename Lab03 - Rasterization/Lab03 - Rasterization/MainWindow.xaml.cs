using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        private bool _isDraggingEdge;
        private bool _isDraggingVertex;
        private Point _initialCursorPosition;
        private Point _currentCursorPosition;
        private int _currentShapeIndex;
        private int _currentEdgeIndex;
        private int _currentPointIndex;
        private uint _currentShapeThickness = 1;
        private Color _currentShapeColor = Color.FromArgb(255, 0, 0, 0);
        private readonly List<Point> _currentPoints = new();
        private readonly List<IDrawable> _allShapes = new();
        private readonly WriteableBitmap _emptyWbm;
        private WriteableBitmap _wbm;
        
        public MainWindow()
        {
            InitializeComponent();
            (_emptyWbm, _wbm) = InitializeCanvas();
        }

        private (WriteableBitmap, WriteableBitmap) InitializeCanvas()
        {
            var emptyWbm = new WriteableBitmap((int)TheCanvas.Width,
                                                (int)TheCanvas.Height, 
                                                96, 
                                                96, 
                                                PixelFormats.Bgr32, 
                                                null);

            emptyWbm.Clear();            
            TheCanvas.Background = new ImageBrush { ImageSource = emptyWbm };
            return (emptyWbm, emptyWbm);
        }
        private void RedrawCanvas()
        {
            _wbm = _emptyWbm.Clone();
            DrawAllShapes(_wbm);
            TheCanvas.Background = new ImageBrush { ImageSource = _wbm };
        }
        
        private void TheCanvas_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _currentCursorPosition = e.GetPosition(TheCanvas);
            Debug.WriteLine("CLICKED!");
            Debug.WriteLine($"{_currentCursorPosition.X}, {_currentCursorPosition.Y}");

            if (e.ClickCount == 2)
            {
                Debug.Write("DOUBLECLICK!!");
            }
            else if (_isDrawingLine) DrawLine(_currentCursorPosition);
            else if (_isDrawingPolygon) DrawPolygon(_currentCursorPosition);
            else if (_isDrawingCircle) DrawCircle(_currentCursorPosition);
            else if (!_isDraggingVertex || !_isDraggingEdge)
            {
                // for each shape, check if a vertex or edge is clicked
                foreach (var shape in _allShapes)
                {
                    _currentPointIndex = -1;
                    _currentEdgeIndex = -1;
                    _currentShapeIndex = _allShapes.IndexOf(shape);
                    _currentPointIndex = shape.GetVertexIndexOf(_currentCursorPosition);
                    if (_currentPointIndex > -1)
                    {
                        Debug.WriteLine("VERTEX!");
                        _initialCursorPosition = _currentCursorPosition;
                        _isDraggingVertex = true;
                        return;
                    }
                    else // check for edge
                    {
                        _currentEdgeIndex = shape.GetEdgeIndexOf(_currentCursorPosition);
                        if (_currentEdgeIndex > -1)
                        {
                            Debug.WriteLine($"EDGE! {_currentEdgeIndex}");
                            _initialCursorPosition = _currentCursorPosition;
                            _isDraggingEdge= true;
                            return;
                        }
                        else
                        {
                            continue;  // to the next shape    
                        }
                    }
                }
            }
        }
        private void TheCanvas_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingVertex || _isDraggingEdge)
            {
                _isDraggingVertex = false;
                _isDraggingEdge = false;
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
            _currentCursorPosition = e.GetPosition(TheCanvas);

            // draw line preview
            if (_isDrawingLine && _currentPoints.Count > 0)
            {
                RedrawCanvas();
                currentLine = new Line(new List<Point> { _currentPoints[0], _currentCursorPosition }, _currentShapeThickness, _currentShapeColor);
                currentLine.Draw(_wbm);
            }
            
            // draw polygon preview
            if (_isDrawingPolygon && _currentPoints.Count > 0)
            {
                RedrawCanvas();
                for (var i = 0; i < _currentPoints.Count - 1; i++)
                {
                    currentLine = new Line(new List<Point> { _currentPoints[i], _currentPoints[i+1] }, _currentShapeThickness, _currentShapeColor);
                    currentLine.Draw(_wbm);
                }

                currentLine = new Line(new List<Point> { _currentPoints[^1], _currentCursorPosition }, _currentShapeThickness, _currentShapeColor);
                currentLine.Draw(_wbm);
            }

            // draw circle preview
            if (_isDrawingCircle && _currentPoints.Count > 0)
            {
                RedrawCanvas();
                var currentCircle = new Circle(new List<Point> { _currentPoints[0], _currentCursorPosition }, _currentShapeThickness, _currentShapeColor);
                currentCircle.Draw(_wbm);
            }

            if (_isDraggingVertex)
            {
                _allShapes[_currentShapeIndex].MoveVertex(_currentPointIndex, Point.Subtract(_currentCursorPosition, _initialCursorPosition));
                _initialCursorPosition = _currentCursorPosition;

                RedrawCanvas();
            }

            if (_isDraggingEdge)
            {
                _allShapes[_currentShapeIndex].MoveEdge(_currentEdgeIndex, Point.Subtract(_currentCursorPosition, _initialCursorPosition));
                _initialCursorPosition = _currentCursorPosition;

                RedrawCanvas();
            }
        }
        
        private void ToggleAllOff()
        {
            if (_isDrawingLine) ToggleIsDrawingLine();
            else if (_isDrawingPolygon) ToggleIsDrawingPolygon();
            else if (_isDrawingCircle) ToggleIsDrawingCircle();

            _currentPoints.Clear();
            RedrawCanvas();
        }
        private void DrawAllShapes(WriteableBitmap wbm)
        {
            foreach (var shape in _allShapes)
                shape.Draw(wbm);
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
                _allShapes.Add(new Line(new List<Point>(_currentPoints), _currentShapeThickness, _currentShapeColor));
                
                // clear the points
                _currentPoints.Clear();
                
                ToggleIsDrawingLine();
                RedrawCanvas();
            }
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
            else if (_currentPoints.Count < 2 || DistanceBetween(_currentPoints[0], clickPosition) > 10)
            {
                _currentPoints.Add(clickPosition);
                Debug.WriteLine($"PolygonPoint: {clickPosition.X}, {clickPosition.Y}");  
            }
            else
            {
                Debug.WriteLine($"Ending: {clickPosition.X}, {clickPosition.Y}");
                _allShapes.Add(new Polygon(new List<Point>(_currentPoints), _currentShapeThickness, _currentShapeColor));
                _currentPoints.Clear();
                ToggleIsDrawingPolygon();
                RedrawCanvas();
            }
        }
        private void CircleButton_OnClick(object sender, RoutedEventArgs e)
        {
            ToggleAllOff();
            ToggleIsDrawingCircle();
        }
        private void ToggleIsDrawingCircle()
        {
            _isDrawingCircle = !_isDrawingCircle;
            CircleButton.Background = _isDrawingCircle ? Brushes.LightSalmon : Brushes.LightCyan;
        }
        private void DrawCircle(Point clickPosition)
        {
            if (_currentPoints.Count == 0) // add center
            {
                _currentPoints.Add(clickPosition);
                Debug.WriteLine($"Center: {clickPosition.X}, {clickPosition.Y}");
            }
            else // add edgePoint and add Circle to _allShapes
            {
                _currentPoints.Add(clickPosition);
                Debug.WriteLine($"Edge: {clickPosition.X}, {clickPosition.Y}");
                _allShapes.Add(new Circle(new List<Point>(_currentPoints), _currentShapeThickness, _currentShapeColor));
                
                // clear the points
                _currentPoints.Clear();
                
                ToggleIsDrawingCircle();
                RedrawCanvas();
            }
        }
        private void OnClick_ResetCanvas(object sender, RoutedEventArgs e)
        {
            _allShapes.Clear();
            RedrawCanvas();
        }
        private void ShapeThickness_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (int.TryParse(e.Text, out int t) && t is > 0 and < 9)
            {
                _currentShapeThickness = (uint)t;
                Debug.WriteLine($"thickness changed to {_currentShapeThickness}");
                ShapeThickness.Text = "";
                e.Handled = false;
                return;
            }

            e.Handled = true;
        }
        private void ShapeColorButton_OnClick(object sender, RoutedEventArgs e)
        {
            using System.Windows.Forms.ColorDialog colorDialog = new System.Windows.Forms.ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ShapeColor.Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
                _currentShapeColor = Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B);
            }
        }
        private int DistanceBetween(Point p1, Point p2)
        {
            return (int)Math.Round(Math.Sqrt(Math.Pow((p2.X - p1.X), 2) + Math.Pow((p2.Y - p1.Y), 2)));
        }

        private void dummyCallBack(object sender, RoutedEventArgs e)
        {
            return;
        }
    }
}
