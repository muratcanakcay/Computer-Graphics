using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using Microsoft.Win32;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Drawing.Color;
using Point = System.Windows.Point;

namespace Lab04___Clipping_and_Filling
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public struct ShapeT
        {
            public string ClassName;
            public List<Point> Points;
            public int Thickness;
            public int Color;
            public int? FillColor;
            public string? FillImage;
            public bool IsClippingRectangle;
        };
        
        private bool _isDrawingLine;
        private bool _isDrawingPolygon;
        private bool _isDrawingRectangle;
        private bool _isDrawingCircle;
        private bool _isDrawingCircleArc;
        private bool _isMovingVertex;
        private bool _isMovingEdge;
        private bool _isMovingShape;
        private bool _isModifyingShape;
        private bool _isAntiAliased;
        private bool _isSuperSampled;
        private bool _isZooming;
        private Point _previousCursorPosition;
        private Point _currentCursorPosition;
        private Polygon? _selectedPolygon;
        private int _selectedRectangleIndex;
        private int _clippingRectangleIndex = -1;
        private int SSAA = 2; // TODO: make this modifiable from GUI
        private int _currentShapeIndex;
        private int _currentEdgeIndex;
        private int _currentVertexIndex;
        private int _currentZoomLevel = 1;
        private int _currentShapeThickness = 1;
        private Color _currentShapeColor = Color.FromKnownColor(KnownColor.Black);
        private Color _currentBorderFillColor = Color.FromKnownColor(KnownColor.Red);
        private Color _currentBorderFillBorderColor = Color.FromKnownColor(KnownColor.Black);
        private Color? _currentFillColor;
        private string? _currentFillImage;
        private readonly List<Point> _currentPoints = new();
        private readonly List<IDrawable> _allShapes = new();
        private readonly List<(Point, Color, Color, Boolean)> _allFills = new ();
        private WriteableBitmap? _emptyWbm;
        private WriteableBitmap? _emptyWbmSsaa;
        private WriteableBitmap _wbm;
        private readonly SolidColorBrush _activeButtonColor = Brushes.LightSalmon;
        private readonly SolidColorBrush _inactiveButtonColor = Brushes.LightCyan;
        private bool _isBorderFilling;
        private bool _useEightConnected;


        public MainWindow()
        {
            InitializeComponent();
            InitializeWbm(out _emptyWbm);

            _wbm = _emptyWbm.Clone();
            CanvasImage.Source = _wbm;
        }

        //---------- HELPER FUNCTIONS
        private void InitializeWbm(out WriteableBitmap wbm, int ssaa = 1)
        {
            wbm = new WriteableBitmap((int)TheCanvas.Width * ssaa,
                                    (int)TheCanvas.Height * ssaa, 
                                    96, 
                                    96, 
                                    PixelFormats.Bgr32, 
                                    null);

            wbm.Clear();
        }
        private void RedrawCanvas()
        {
            if (_isSuperSampled)
            {
                if (_emptyWbmSsaa == null || _emptyWbmSsaa.PixelHeight != (int)TheCanvas.Height * SSAA)
                    InitializeWbm(out _emptyWbmSsaa, SSAA);
                
                _wbm = _emptyWbmSsaa.Clone();
                DrawAllShapes(_wbm);
                _wbm = _wbm.DownSample(SSAA);
                CanvasImage.Source = _wbm;
            }
            else
            {
                if (_emptyWbm == null)
                    InitializeWbm(out _emptyWbm);
                
                _wbm = _emptyWbm.Clone();
                DrawAllShapes(_wbm);
                CanvasImage.Source = _wbm;
            }
        }
        private void ToggleAllOff()
        {
            if (_isDrawingLine) ToggleIsDrawingLine();
            else if (_isDrawingPolygon) ToggleIsDrawingPolygon();
            else if (_isDrawingRectangle) ToggleIsDrawingRectangle();
            else if (_isDrawingCircle) ToggleIsDrawingCircle();
            else if (_isDrawingCircleArc) ToggleIsDrawingCircleArc();

            DisableFillButtons();
            if (_clippingRectangleIndex == -1) ClipButton.IsEnabled = false;
            else ClipButton.Background = _activeButtonColor;
            _selectedRectangleIndex = -1;

            _currentPoints.Clear();
            RedrawCanvas();
        }
        private void DrawAllShapes(WriteableBitmap wbm)
        {
            foreach (var shape in _allShapes)
                shape.Draw(wbm, _isAntiAliased, _isSuperSampled, SSAA, _clippingRectangleIndex == -1 ? null : (Rectangle)_allShapes[_clippingRectangleIndex]);

            foreach (var fill in _allFills)
            {
                _currentBorderFillColor = fill.Item2;
                _currentBorderFillBorderColor = fill.Item3;
                _useEightConnected = fill.Item4;
                BorderFill(fill.Item1);
            }


        }
        private static int DistanceBetween(Point p1, Point p2)
        {
            return (int)Math.Round(Math.Sqrt(Math.Pow((p2.X - p1.X), 2) + Math.Pow((p2.Y - p1.Y), 2)));
        }
        
        //---------- MOUSE EVENTS
        private void TheCanvas_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isBorderFilling)
            {
                //BorderFill(_currentCursorPosition = e.GetPosition(TheCanvas));
                _allFills.Add((e.GetPosition(TheCanvas), _currentBorderFillColor, _currentBorderFillBorderColor, _useEightConnected));
                RedrawCanvas();
            }

            _isModifyingShape = false;
            DisableFillButtons();

            if (_clippingRectangleIndex == -1) ClipButton.IsEnabled = false;
            else ClipButton.Background = _activeButtonColor;
            _selectedRectangleIndex = -1;

            _currentCursorPosition = e.GetPosition(TheCanvas);
            Debug.WriteLine($"CLICKED ({_currentCursorPosition.X}, {_currentCursorPosition.Y})");

            if (e.ClickCount == 2)
            {
                Debug.WriteLine("DOUBLECLICK!!");
                ToggleAllOff();

                // for each shape, check if a vertex or edge is clicked
                foreach (var shape in _allShapes)
                {
                    _currentShapeIndex = -1;
                    if (shape.GetVertexIndexOf(_currentCursorPosition) > -1 ||
                        shape.GetEdgeIndexOf(_currentCursorPosition) > -1)
                    {
                        _currentShapeIndex = _allShapes.IndexOf(shape);
                        break;
                    }
                }

                if (_currentShapeIndex == -1) return; // did not click on a shape

                _isModifyingShape = true;
                _isMovingShape = true;
                ShapeThicknessTextBox.Text = _allShapes[_currentShapeIndex].Thickness.ToString();
                var shapeColor = _allShapes[_currentShapeIndex].Color;
                ShapeColorButton.Fill = new SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(shapeColor.A, shapeColor.R, shapeColor.G, shapeColor.B));

                if (_allShapes[_currentShapeIndex] is Polygon selectedPolygon)
                {
                    _selectedPolygon = selectedPolygon;
                    EnableFillButtons();
                }

                if (_allShapes[_currentShapeIndex] is Rectangle)
                {
                    _selectedRectangleIndex = _currentShapeIndex;
                    ClipButton.IsEnabled = true;
                    ClipButton.Background = _clippingRectangleIndex == _selectedRectangleIndex ? _activeButtonColor : _inactiveButtonColor;
                }

                Debug.WriteLine(_allShapes[_currentShapeIndex]);
            }
            else if (_isDrawingLine) DrawLine(_currentCursorPosition);
            else if (_isDrawingPolygon) DrawPolygon(_currentCursorPosition);
            else if (_isDrawingRectangle) DrawRectangle(_currentCursorPosition);
            else if (_isDrawingCircle) DrawCircle(_currentCursorPosition);
            else if (_isDrawingCircleArc) DrawCircleArc(_currentCursorPosition);
            else if (!_isMovingVertex || !_isMovingEdge)
            {
                // for each shape, check if a vertex or edge is clicked
                foreach (var shape in _allShapes)
                {
                    _currentVertexIndex = -1;
                    _currentEdgeIndex = -1;
                    _currentShapeIndex = _allShapes.IndexOf(shape);
                    
                    // check for vertex
                    _currentVertexIndex = shape.GetVertexIndexOf(_currentCursorPosition);
                    if (_currentVertexIndex > -1)
                    {
                        Debug.WriteLine($"VERTEX#{_currentVertexIndex}");
                        _previousCursorPosition = _currentCursorPosition;
                        _isMovingVertex = true;
                        return;
                    }
                    
                    // check for edge
                    _currentEdgeIndex = shape.GetEdgeIndexOf(_currentCursorPosition);
                    if (_currentEdgeIndex > -1)
                    {
                        Debug.WriteLine($"EDGE#{_currentEdgeIndex}");
                        _previousCursorPosition = _currentCursorPosition;
                        _isMovingEdge= true;
                        return;
                    }

                    _currentShapeIndex = -1;
                }
            }
        }
        private void TheCanvas_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isMovingShape = false;

            if (_isMovingVertex || _isMovingEdge)
            {
                _isMovingVertex = false;
                _isMovingEdge = false;
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
            if (!_isSuperSampled && _isDrawingLine && _currentPoints.Count > 0)
            {
                RedrawCanvas();
                currentLine = new Line(
                    new List<Point>
                    {
                        _currentPoints[0],
                        _currentCursorPosition
                    },
                    _currentShapeThickness,
                    _currentShapeColor);
                currentLine.Draw(_wbm, _isAntiAliased, _isSuperSampled);
            }
            
            // draw polygon preview
            if (_isDrawingPolygon && _currentPoints.Count > 0)
            {
                RedrawCanvas();
                for (var i = 0; i < _currentPoints.Count - 1; i++)
                {
                    currentLine = new Line(
                        new List<Point>
                        {
                            _currentPoints[i],
                            _currentPoints[i+1]
                        },
                        _currentShapeThickness,
                        _currentShapeColor);
                    currentLine.Draw(_wbm, _isAntiAliased, _isSuperSampled);
                }

                currentLine = new Line(new List<Point> { _currentPoints[^1], _currentCursorPosition }, _currentShapeThickness, _currentShapeColor);
                currentLine.Draw(_wbm, _isAntiAliased, _isSuperSampled);
            }

            // draw rectangle preview
            if (_isDrawingRectangle && _currentPoints.Count > 0)
            {
                RedrawCanvas();
                currentLine = new Line(new List<Point> { _currentPoints[0], new Point(_currentCursorPosition.X, _currentPoints[0].Y) }, _currentShapeThickness, _currentShapeColor);
                currentLine.Draw(_wbm, _isAntiAliased, _isSuperSampled);
                currentLine = new Line(new List<Point> { new Point(_currentCursorPosition.X, _currentPoints[0].Y), _currentCursorPosition }, _currentShapeThickness, _currentShapeColor);
                currentLine.Draw(_wbm, _isAntiAliased, _isSuperSampled);
                currentLine = new Line(new List<Point> { _currentCursorPosition, new Point(_currentPoints[0].X, _currentCursorPosition.Y) }, _currentShapeThickness, _currentShapeColor);
                currentLine.Draw(_wbm, _isAntiAliased, _isSuperSampled);
                currentLine = new Line(new List<Point> { new Point(_currentPoints[0].X, _currentCursorPosition.Y), _currentPoints[0] }, _currentShapeThickness, _currentShapeColor);
                currentLine.Draw(_wbm, _isAntiAliased, _isSuperSampled);
            }

            // draw circle preview
            if (_isDrawingCircle && _currentPoints.Count > 0)
            {
                RedrawCanvas();
                var currentCircle = new Circle(
                    new List<Point>
                    {
                        _currentPoints[0],
                        _currentCursorPosition
                    },
                    _currentShapeThickness,
                    _currentShapeColor);
                currentCircle.Draw(_wbm, _isAntiAliased, _isSuperSampled);
            }

            // draw circle arc preview
            if (_isDrawingCircleArc && _currentPoints.Count > 1)
            {
                RedrawCanvas();
                var currentCircleArc = new CircleArc(
                    new List<Point>
                    {
                        _currentPoints[0],
                        _currentPoints[1],
                        _currentCursorPosition 
                    },
                    _currentShapeThickness,
                    _currentShapeColor);
                currentCircleArc.Draw(_wbm, _isAntiAliased, _isSuperSampled);
            }

            if (_isMovingVertex)
            {
                _allShapes[_currentShapeIndex].MoveVertex(
                    _currentVertexIndex, Point.Subtract(_currentCursorPosition, _previousCursorPosition));
                _previousCursorPosition = _currentCursorPosition;

                RedrawCanvas();
            }

            if (_isMovingEdge)
            {
                _allShapes[_currentShapeIndex].MoveEdge(
                    _currentEdgeIndex, Point.Subtract(_currentCursorPosition, _previousCursorPosition));
                _previousCursorPosition = _currentCursorPosition;

                RedrawCanvas();
            }

            if (_isMovingShape)
            {
                _allShapes[_currentShapeIndex].MoveShape(
                    Point.Subtract(_currentCursorPosition, _previousCursorPosition));
                _previousCursorPosition = _currentCursorPosition;

                RedrawCanvas();
            }
        }
        private void TheCanvas_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_isZooming)
            {
                if (e.Delta > 0) _currentZoomLevel++;
                else if (_currentZoomLevel > 1) _currentZoomLevel--;

                TheCanvas.RenderTransform = new ScaleTransform(
                    _currentZoomLevel,
                    _currentZoomLevel,
                    _currentCursorPosition.X,
                    _currentCursorPosition.Y);
            }
        }

        //---------- KEYBOARD EVENTS
        private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (_isModifyingShape && e.Key == Key.Delete)
            {
                _allShapes.RemoveAt(_currentShapeIndex);
                _isModifyingShape = false;
                ToggleAllOff();
                RedrawCanvas();
            }

            if (!_isZooming && e.Key == Key.LeftCtrl)
            {
                _isZooming = true;
                Debug.WriteLine("Start zooming");
            }
        }
        private void MainWindow_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (_isZooming && e.Key == Key.LeftCtrl)
            {
                _isZooming = false;
                Debug.WriteLine("Stop zooming");
            }
        }

        //---------- LINE METHODS
        private void LineButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_isDrawingLine) ToggleAllOff();
            ToggleIsDrawingLine();
        }
        private void ToggleIsDrawingLine()
        {
            _isDrawingLine = !_isDrawingLine;
            LineButton.Background = _isDrawingLine ? _activeButtonColor : _inactiveButtonColor;
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
                Debug.WriteLine($"Ending: {clickPosition.X}, {clickPosition.Y}");
                _allShapes.Add(new Line(
                    new List<Point>{ _currentPoints[0], clickPosition }, _currentShapeThickness, _currentShapeColor));
                
                _currentPoints.Clear();
                ToggleIsDrawingLine();
                RedrawCanvas();
            }
        }
        
        //---------- POLYGON METHODS
        private void PolygonButton_OnClick(object sender, RoutedEventArgs e)
        {
            if(!_isDrawingPolygon) ToggleAllOff();
            ToggleIsDrawingPolygon();
        }
        private void ToggleIsDrawingPolygon()
        {
            _isDrawingPolygon = !_isDrawingPolygon;
            PolygonButton.Background = _isDrawingPolygon ? _activeButtonColor : _inactiveButtonColor;
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
                _allShapes.Add(new Polygon(
                    new List<Point>(_currentPoints), _currentShapeThickness, _currentShapeColor));

                _currentPoints.Clear();
                ToggleIsDrawingPolygon();
                RedrawCanvas();
            }
        }

        //---------- RECTANGLE METHODS
        private void RectangleButton_OnClick(object sender, RoutedEventArgs e)
        {
            if(!_isDrawingRectangle) ToggleAllOff();
            ToggleIsDrawingRectangle();
        }
        private void ToggleIsDrawingRectangle()
        {
            _isDrawingRectangle = !_isDrawingRectangle;
            RectangleButton.Background = _isDrawingRectangle ? _activeButtonColor : _inactiveButtonColor;
        }
        private void DrawRectangle(Point clickPosition)
        {
            if (_currentPoints.Count == 0)
            {
                _currentPoints.Add(clickPosition);
                Debug.WriteLine($"Starting: {clickPosition.X}, {clickPosition.Y}");
            }
            else
            {
                Debug.WriteLine($"Ending: {clickPosition.X}, {clickPosition.Y}");
                
                _currentPoints.Add(clickPosition);

                var rectangleVertices = new List<Point>
                {
                    _currentPoints[0],
                    new Point(_currentPoints[1].X, _currentPoints[0].Y),
                    _currentPoints[1],
                    new Point(_currentPoints[0].X, _currentPoints[1].Y),
                };

                _allShapes.Add(new Rectangle(rectangleVertices, _currentShapeThickness, _currentShapeColor));

                _currentPoints.Clear();
                ToggleIsDrawingRectangle();
                RedrawCanvas();
            }
        }
        
        //---------- CIRCLE METHODS
        private void CircleButton_OnClick(object sender, RoutedEventArgs e)
        {
            if(!_isDrawingCircle) ToggleAllOff();
            ToggleIsDrawingCircle();
        }
        private void ToggleIsDrawingCircle()
        {
            _isDrawingCircle = !_isDrawingCircle;
            CircleButton.Background = _isDrawingCircle ? _activeButtonColor : _inactiveButtonColor;
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
                _allShapes.Add(new Circle(
                    new List<Point>(_currentPoints), _currentShapeThickness, _currentShapeColor));
                
                // clear the points
                _currentPoints.Clear();
                
                ToggleIsDrawingCircle();
                RedrawCanvas();
            }
        }
        
        //---------- CIRCLE ARC
        private void CircleArcButton_OnClick(object sender, RoutedEventArgs e)
        {
            if(!_isDrawingCircleArc) ToggleAllOff();
            ToggleIsDrawingCircleArc();
        }
        private void ToggleIsDrawingCircleArc()
        {
            _isDrawingCircleArc = !_isDrawingCircleArc;
            CircleArcButton.Background = _isDrawingCircleArc ? _activeButtonColor : _inactiveButtonColor;
        }
        private void DrawCircleArc(Point clickPosition)
        {
            if (_currentPoints.Count == 0) // add center
            {
                _currentPoints.Add(clickPosition);
                Debug.WriteLine($"Center: {clickPosition.X}, {clickPosition.Y}");
            }
            else if (_currentPoints.Count == 1) // add starting point of arc
            {
                _currentPoints.Add(clickPosition);
                Debug.WriteLine($"Arc Start: {clickPosition.X}, {clickPosition.Y}");
            }
            else // add edgePoint and add Circle to _allShapes
            {
                _currentPoints.Add(clickPosition);
                Debug.WriteLine($"Arc End: {clickPosition.X}, {clickPosition.Y}");
                _allShapes.Add(new CircleArc(
                    new List<Point>(_currentPoints), _currentShapeThickness, _currentShapeColor));
                
                // clear the points
                _currentPoints.Clear();
                
                ToggleIsDrawingCircleArc();
                RedrawCanvas();
            }
        }

        //---------- THICKNESS, EDGE COLOR, ANTI-ALIASING
        private void ShapeThickness_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (int.TryParse(e.Text, out var inputNum) && inputNum is >= 1 and <= 8)
            {
                _currentShapeThickness = inputNum;
                Debug.WriteLine($"thickness changed to {_currentShapeThickness}");
                ShapeThicknessTextBox.Text = "";
                e.Handled = false;

                if (_isModifyingShape)
                {
                    _allShapes[_currentShapeIndex].Thickness = _currentShapeThickness;
                    RedrawCanvas();
                }

                return;
            }

            e.Handled = true;
        }
        private void ShapeColorButton_OnClick(object sender, RoutedEventArgs e)
        {
            using var colorDialog = new System.Windows.Forms.ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ShapeColorButton.Fill = new SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(
                        colorDialog.Color.A,
                        colorDialog.Color.R,
                        colorDialog.Color.G,
                        colorDialog.Color.B));

                _currentShapeColor = Color.FromArgb(
                    colorDialog.Color.A,
                    colorDialog.Color.R,
                    colorDialog.Color.G,
                    colorDialog.Color.B);

                if (_isModifyingShape)
                {
                    _allShapes[_currentShapeIndex].Color = _currentShapeColor;
                    RedrawCanvas();
                }
            }
        }
        private void AntiAliasButton_OnClick(object sender, RoutedEventArgs e)
        {
            ToggleIsAntiAliased();
            RedrawCanvas();
        }
        private void ToggleIsAntiAliased()
        {
            if (_isSuperSampled) ToggleIsSuperSampled();

            _isAntiAliased = !_isAntiAliased;
            AntiAliasButton.Background = _isAntiAliased ? _activeButtonColor : _inactiveButtonColor;
        }
        private void SuperSampleButton_OnClick(object sender, RoutedEventArgs e)
        {
            ToggleIsSuperSampled();
            RedrawCanvas();
            
            // free the memory in wbm
            if (_isSuperSampled) _emptyWbm = null;
            else _emptyWbmSsaa = null;
        }
        private void ToggleIsSuperSampled()
        {
            if (_isAntiAliased) ToggleIsAntiAliased();
            
            _isSuperSampled = !_isSuperSampled;
            SuperSampleButton.Background = _isSuperSampled ? _activeButtonColor : _inactiveButtonColor;
        }
        
        //---------- FILLING
        private void FillSolidButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_currentFillColor is null) return;
            ToggleFillSolid();
            RedrawCanvas();
        }
        private void ToggleFillSolid()
        {
            switch (_selectedPolygon!.FillColor)
            {
                case null:
                    _selectedPolygon.FillColor = _currentFillColor;
                    _selectedPolygon.FillImage = null;
                    break;
                default:
                    _selectedPolygon.FillColor = null;
                    break;
            }

            FillImageButton.Background = _inactiveButtonColor;
            FillSolidButton.Background = _selectedPolygon.FillColor is not null
                ? _activeButtonColor
                : _inactiveButtonColor;
        }
        private void SelectFillColorButton_OnClick(object sender, MouseButtonEventArgs e)
        {
            //if(!_isPolygonSelected) return;
            
            using var colorDialog = new System.Windows.Forms.ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FillColorButton.Fill = new SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(
                        colorDialog.Color.A,
                        colorDialog.Color.R,
                        colorDialog.Color.G,
                        colorDialog.Color.B));

                _currentFillColor = Color.FromArgb(
                    colorDialog.Color.A,
                    colorDialog.Color.R,
                    colorDialog.Color.G,
                    colorDialog.Color.B);

                if (_selectedPolygon?.FillColor != null) // (_selectedPolygon != null && _selectedPolygon.FillColor != null)
                {
                    _selectedPolygon.FillColor = _currentFillColor;
                    RedrawCanvas();
                }
            }
        }
        private void FillImageButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_currentFillImage is null) return;
            ToggleFillImage();
            RedrawCanvas();
        }
        private void ToggleFillImage()
        {
            switch (_selectedPolygon!.FillImage)
            {
                case null:
                    _selectedPolygon.FillImage = _currentFillImage;
                    _selectedPolygon.FillColor = null;
                    break;
                default:
                    _selectedPolygon.FillImage = null;
                    break;
            }

            FillSolidButton.Background = _inactiveButtonColor;
            FillImageButton.Background = _selectedPolygon.FillImage is not null
                ? _activeButtonColor
                : _inactiveButtonColor;
        }
        private void SelectFillImageButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new()
            {
                Filter = "Image files|*.jpg;*.png;*.bmp",
                RestoreDirectory = true
            };

            if (dlg.ShowDialog() != true) return;

            _currentFillImage = (new Uri(System.Reflection.Assembly.GetExecutingAssembly().Location))
                .MakeRelativeUri(new Uri(dlg.FileName)).ToString();

            SelectedFillImage.Text = _currentFillImage;


            if (_selectedPolygon?.FillImage != null) // (_selectedPolygon != null && _selectedPolygon.FillImage != null)
            {
                _selectedPolygon.FillImage = _currentFillImage;
                RedrawCanvas();
            }
        }
        private void DisableFillButtons()
        {
            FillSolidButton.IsEnabled = false;
            FillColorButton.Fill = new SolidColorBrush(Colors.Transparent);
            _currentFillColor = null;

            FillImageButton.IsEnabled = false;
            SelectedFillImage.Text = "No image selected";
            _currentFillImage = null;
        }
        private void EnableFillButtons()
        {
            EnableFillSolidButton();
            EnableFillImageButton();
        }
        private void EnableFillImageButton()
        {
            FillImageButton.IsEnabled = true;
            FillImageButton.Background = _selectedPolygon!.FillImage is not null
                ? _activeButtonColor
                : _inactiveButtonColor;

            var fillImage = _selectedPolygon!.FillImage;
            if (fillImage is not null)
            {
                SelectedFillImage.Text = fillImage;
                _currentFillImage = fillImage;
            }
            else
            {
                SelectedFillImage.Text = "No image selected";
                _currentFillImage = null;
            }
        }
        private void EnableFillSolidButton()
        {
            FillSolidButton.IsEnabled = true;
            FillSolidButton.Background = _selectedPolygon!.FillColor is not null
                ? _activeButtonColor
                : _inactiveButtonColor;

            var fillColor = _selectedPolygon.FillColor;
            if (fillColor is not null)
            {
                FillColorButton.Fill = new SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(
                        ((Color)fillColor).A,
                        ((Color)fillColor).R,
                        ((Color)fillColor).G,
                        ((Color)fillColor).B));
                _currentFillColor = fillColor;
            }
            else
            {
                FillColorButton.Fill = new SolidColorBrush(Colors.Transparent);
                _currentFillColor = null;
            }
        }

        //----------- CLIPPING (COHEN SUTHERLAND ALGORITHM)
        private void ClipButton_OnClick(object sender, RoutedEventArgs e)
        {
            ToggleClipping();
            RedrawCanvas(); 
        }
        private void ToggleClipping()
        {
            if (_clippingRectangleIndex == -1)
            {
                _clippingRectangleIndex = _selectedRectangleIndex;
                ((Rectangle)_allShapes[_clippingRectangleIndex]).IsClippingRectangle = true;
            }
            else if (_clippingRectangleIndex == _selectedRectangleIndex)
            {
                ((Rectangle)_allShapes[_clippingRectangleIndex]).IsClippingRectangle = false;
                _clippingRectangleIndex = -1;
            }
            else
            {
                ((Rectangle)_allShapes[_clippingRectangleIndex]).IsClippingRectangle = false;
                _clippingRectangleIndex = _selectedRectangleIndex;
                if (_clippingRectangleIndex != -1) 
                    ((Rectangle)_allShapes[_clippingRectangleIndex]).IsClippingRectangle = true;
            }

            ClipButton.Background = _clippingRectangleIndex == -1 ? _inactiveButtonColor : _activeButtonColor;
            if (_selectedRectangleIndex == -1) ClipButton.IsEnabled = false;
        }

        //----------- BORDER FILL
        private void BorderFill4Button_OnClick(object sender, RoutedEventArgs e)
        {
            _isBorderFilling = !_isBorderFilling;
            _useEightConnected = false;
            BorderFill4Button.Background = _isBorderFilling ? _activeButtonColor : _inactiveButtonColor;
        }
        private void BorderFill8Button_OnClick(object sender, RoutedEventArgs e)
        {
            _isBorderFilling = !_isBorderFilling;
            _useEightConnected = true;
            BorderFill8Button.Background = _isBorderFilling ? _activeButtonColor : _inactiveButtonColor; 
        }
        private void SelectBorderFillColorButton_OnClick(object sender, MouseButtonEventArgs e)
        {
            using var colorDialog = new System.Windows.Forms.ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                BorderFillColorButton.Fill = new SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(
                        colorDialog.Color.A,
                        colorDialog.Color.R,
                        colorDialog.Color.G,
                        colorDialog.Color.B));

                _currentBorderFillColor = Color.FromArgb(
                    colorDialog.Color.A,
                    colorDialog.Color.R,
                    colorDialog.Color.G,
                    colorDialog.Color.B);
            }
        }
        private void SelectBorderFillBorderColorButton_OnClick(object sender, MouseButtonEventArgs e)
        {
            using var colorDialog = new System.Windows.Forms.ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                BorderFillBorderColorButton.Fill = new SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(
                        colorDialog.Color.A,
                        colorDialog.Color.R,
                        colorDialog.Color.G,
                        colorDialog.Color.B));

                _currentBorderFillBorderColor = Color.FromArgb(
                    colorDialog.Color.A,
                    colorDialog.Color.R,
                    colorDialog.Color.G,
                    colorDialog.Color.B);
            }
        }
        private void BorderFill(Point currentCursorPosition)
        {
            Debug.WriteLine($"Border Fill 8-connected: {_useEightConnected}");
            
            var pointStack = new Stack<Point>();
            pointStack.Push(currentCursorPosition);
            _wbm.Lock();
            while (pointStack.Count > 0)
            {
                var currentPoint = pointStack.Pop();
                var x = (int)currentPoint.X;
                var y = (int)currentPoint.Y;
                if (x < 0 || x > CanvasImage.ActualWidth || y < 0 || y > CanvasImage.ActualHeight) continue;

                var c = _wbm.GetPixelColor(x, y);
                if (((c.R != _currentBorderFillColor.R) ||
                     (c.G != _currentBorderFillColor.G) ||
                     (c.B != _currentBorderFillColor.B)) 
                    &&
                    ((c.R != _currentBorderFillBorderColor.R) ||
                     (c.G != _currentBorderFillBorderColor.G) ||
                     (c.B != _currentBorderFillBorderColor.B)))
                {
                    _wbm.SetPixelColor(x, y, _currentBorderFillColor);
                    pointStack.Push(new Point(x+1, y+0));
                    pointStack.Push(new Point(x-1, y+0));
                    pointStack.Push(new Point(x+0, y+1));
                    pointStack.Push(new Point(x+0, y-1));

                    if (_useEightConnected)
                    {
                        pointStack.Push(new Point(x + 1, y + 1));
                        pointStack.Push(new Point(x - 1, y - 1));
                        pointStack.Push(new Point(x - 1, y + 1));
                        pointStack.Push(new Point(x + 1, y - 1));
                    }
                }
            }
            _wbm.Unlock();

            _isBorderFilling = false;
            BorderFill4Button.Background = _inactiveButtonColor;
            BorderFill8Button.Background = _inactiveButtonColor;
        }

        //---------- MENU ITEMS
        private void OnClick_ResetCanvas(object sender, RoutedEventArgs e)
        {
            _allShapes.Clear();
            _allFills.Clear();
            _isAntiAliased = true;
            _clippingRectangleIndex = -1;
            ToggleIsAntiAliased();
            ToggleAllOff();
            RedrawCanvas();
        }
        private void OnClick_SaveShapes(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new()
            {
                Filter = "XML files|*.xml",
                RestoreDirectory = true
            };

            if (dlg.ShowDialog() != true || dlg.FileName == string.Empty) return;

            List<ShapeT> shapesList = _allShapes
                                        .Select(shape => new ShapeT() {
                                            ClassName = shape.GetType().Name,
                                            Points = shape.Points,
                                            Thickness = shape.Thickness,
                                            Color = shape.Color.ToArgb(),
                                            FillColor = shape.FillColor?.ToArgb(),
                                            FillImage = shape.FillImage,
                                            IsClippingRectangle = shape.IsClippingRectangle
                                        })
                                        .ToList();

            var s = new XmlSerializer(typeof(List<ShapeT>));
            using var fs = new FileStream(dlg.FileName, FileMode.Create);
            s.Serialize(fs, shapesList);
        }
        private void OnClick_LoadShapes(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new()
            {
                Filter = "XML files|*.xml",
                RestoreDirectory = true
            };

            if (dlg.ShowDialog() != true || dlg.FileName.Equals("")) return;

            var s = new XmlSerializer(typeof(List<ShapeT>));
            using var fs = new FileStream($"{dlg.FileName}", FileMode.Open, FileAccess.Read);
            try
            {
                var loadedObject = s.Deserialize(fs);
                if (loadedObject != null)
                {
                    var loadedList = (List<ShapeT>)loadedObject;
                    _allShapes.Clear();
                    _clippingRectangleIndex = -1;

                    foreach (var shape in loadedList)
                    {
                        switch(shape.ClassName)
                        {
                            case "Line":
                                _allShapes.Add(new Line(
                                    shape.Points, 
                                    shape.Thickness, 
                                    Color.FromArgb(shape.Color)));
                                break;
                            case "Polygon":
                                _allShapes.Add(new Polygon(
                                    shape.Points, 
                                    shape.Thickness, 
                                    Color.FromArgb(shape.Color),
                                    shape.FillColor != null ? Color.FromArgb((int)shape.FillColor) : null,
                                    shape.FillImage));
                                break;
                            case "Rectangle":
                                _allShapes.Add(new Rectangle(
                                    shape.Points, 
                                    shape.Thickness, 
                                    Color.FromArgb(shape.Color),
                                    shape.FillColor is not null ? Color.FromArgb((int)shape.FillColor) : null,
                                    shape.FillImage,
                                    shape.IsClippingRectangle));
                                if (shape.IsClippingRectangle)
                                {
                                    _clippingRectangleIndex = _allShapes.Count - 1;
                                }
                                break;
                            case "Circle":
                                _allShapes.Add(new Circle(
                                    shape.Points,
                                    shape.Thickness,
                                    Color.FromArgb(shape.Color)));
                                break;
                            case "CircleArc":
                                _allShapes.Add(new CircleArc(
                                    shape.Points,
                                    shape.Thickness,
                                    Color.FromArgb(shape.Color)));
                                break;
                        }
                    }
                }

                _isAntiAliased = true;
                ToggleIsAntiAliased();
                RedrawCanvas();
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show("Error in file!", "Error!");
            }
        }
        private void OnClick_Exit(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                    "Are you sure you want to leave?", "Exit", MessageBoxButton.YesNo)
                == MessageBoxResult.Yes)
                Close();
        }
        private void OnClick_Help(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Click and hold on an edge or a vertex to move it.\n" +
                            "Double-click and hold on a shape to move the entire shape\n\n" +
                            "Double-click on a shape to select it. If a shape is selected you can then change " +
                            "its color/thickness or delete it with del key. If a rectangle is selected you can " +
                            "set it as the clipping rectangle.\n\n" +
                            "Ctrl+Mouse wheel to zoom in/out.", "Help", 
                            MessageBoxButton.OK);
        }
    }
}