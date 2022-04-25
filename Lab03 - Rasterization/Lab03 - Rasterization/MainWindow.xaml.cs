using System;
using System.Collections.Generic;
using System.Diagnostics;
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

// TODO check for out of bounds of canvas! maybe set canvas size to match panel size?

namespace Lab03___Rasterization
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public struct ShapeT
        {
            public string ClassName;
            public List<Point> Points;
            public uint Thickness;
            public int Color;
        };
        
        private bool _isDrawingLine;
        private bool _isDrawingPolygon;
        private bool _isDrawingCircle;
        private bool _isDrawingCircleArc;
        private bool _isMovingVertex;
        private bool _isMovingEdge;
        private bool _isMovingShape;
        private bool _isModifyingShape;
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

        //---------- HELPER FUNCTIONS
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
        private void ToggleAllOff()
        {
            _isModifyingShape = false;
            if (_isDrawingLine) ToggleIsDrawingLine();
            else if (_isDrawingPolygon) ToggleIsDrawingPolygon();
            else if (_isDrawingCircle) ToggleIsDrawingCircle();
            else if (_isDrawingCircleArc) ToggleIsDrawingCircleArc();

            _currentPoints.Clear();
            RedrawCanvas();
        }
        private void DrawAllShapes(WriteableBitmap wbm)
        {
            foreach (var shape in _allShapes)
                shape.Draw(wbm);
        }
        private static int DistanceBetween(Point p1, Point p2)
        {
            return (int)Math.Round(Math.Sqrt(Math.Pow((p2.X - p1.X), 2) + Math.Pow((p2.Y - p1.Y), 2)));
        }
        
        //---------- MOUSE EVENTS
        private void TheCanvas_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isModifyingShape = false;
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
                    if (shape.GetVertexIndexOf(_currentCursorPosition) > -1 || shape.GetEdgeIndexOf(_currentCursorPosition) > -1)
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
                ShapeColorButton.Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(shapeColor.A, shapeColor.R, shapeColor.G, shapeColor.B));
            }
            else if (_isDrawingLine) DrawLine(_currentCursorPosition);
            else if (_isDrawingPolygon) DrawPolygon(_currentCursorPosition);
            else if (_isDrawingCircle) DrawCircle(_currentCursorPosition);
            else if (_isDrawingCircleArc) DrawCircleArc(_currentCursorPosition);
            else if (!_isMovingVertex || !_isMovingEdge)
            {
                // for each shape, check if a vertex or edge is clicked
                foreach (var shape in _allShapes)
                {
                    _currentPointIndex = -1;
                    _currentEdgeIndex = -1;
                    _currentShapeIndex = _allShapes.IndexOf(shape);
                    
                    // check for vertex
                    _currentPointIndex = shape.GetVertexIndexOf(_currentCursorPosition);
                    if (_currentPointIndex > -1)
                    {
                        Debug.WriteLine("VERTEX!");
                        _initialCursorPosition = _currentCursorPosition;
                        _isMovingVertex = true;
                        return;
                    }
                    
                    // check for edge
                    _currentEdgeIndex = shape.GetEdgeIndexOf(_currentCursorPosition);
                    if (_currentEdgeIndex > -1)
                    {
                        Debug.WriteLine($"EDGE! {_currentEdgeIndex}");
                        _initialCursorPosition = _currentCursorPosition;
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

            // draw circle preview
            if (_isDrawingCircleArc && _currentPoints.Count > 1)
            {
                RedrawCanvas();
                var currentCircleArc = new CircleArc(new List<Point> { _currentPoints[0], _currentPoints[1], _currentCursorPosition }, _currentShapeThickness, _currentShapeColor);
                currentCircleArc.Draw(_wbm);
            }

            if (_isMovingVertex)
            {
                _allShapes[_currentShapeIndex].MoveVertex(_currentPointIndex, Point.Subtract(_currentCursorPosition, _initialCursorPosition));
                _initialCursorPosition = _currentCursorPosition;

                RedrawCanvas();
            }

            if (_isMovingEdge)
            {
                _allShapes[_currentShapeIndex].MoveEdge(_currentEdgeIndex, Point.Subtract(_currentCursorPosition, _initialCursorPosition));
                _initialCursorPosition = _currentCursorPosition;

                RedrawCanvas();
            }

            if (_isMovingShape)
            {
                _allShapes[_currentShapeIndex].MoveShape(Point.Subtract(_currentCursorPosition, _initialCursorPosition));
                _initialCursorPosition = _currentCursorPosition;

                RedrawCanvas();
            }
        }

        //---------- KEYBOARD EVENTS
        private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (_isModifyingShape && e.Key == Key.Delete)
            {
                _allShapes.RemoveAt(_currentShapeIndex);
            }

            ToggleAllOff();
            RedrawCanvas();
        }
        
        //---------- LINE METHODS
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
                Debug.WriteLine($"Ending: {clickPosition.X}, {clickPosition.Y}");
                _allShapes.Add(new Line(new List<Point> { _currentPoints[0], clickPosition }, _currentShapeThickness, _currentShapeColor));
                
                // clear the points
                _currentPoints.Clear();
                
                ToggleIsDrawingLine();
                RedrawCanvas();
            }
        }
        
        //---------- POLYGON METHODS
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
        
        //---------- CIRCLE METHODS
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
        
        //---------- CIRCLE ARC
        private void CircleArcButton_OnClick(object sender, RoutedEventArgs e)
        {
            ToggleAllOff();
            ToggleIsDrawingCircleArc();
        }
        private void ToggleIsDrawingCircleArc()
        {
            _isDrawingCircleArc = !_isDrawingCircleArc;
            CircleArcButton.Background = _isDrawingCircleArc ? Brushes.LightSalmon : Brushes.LightCyan;
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
                _allShapes.Add(new CircleArc(new List<Point>(_currentPoints), _currentShapeThickness, _currentShapeColor));
                
                // clear the points
                _currentPoints.Clear();
                
                ToggleIsDrawingCircleArc();
                RedrawCanvas();
            }
        }

        //---------- THICKNESS AND COLOR
        private void ShapeThickness_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (int.TryParse(e.Text, out var inputNum) && inputNum is > 0 and < 9)
            {
                _currentShapeThickness = (uint)inputNum;
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
            using System.Windows.Forms.ColorDialog colorDialog = new System.Windows.Forms.ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ShapeColorButton.Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
                _currentShapeColor = Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B);

                if (_isModifyingShape)
                {
                    _allShapes[_currentShapeIndex].Color = _currentShapeColor;
                    RedrawCanvas();
                }
            }
        }
        
        //---------- MENU ITEMS
        private void OnClick_ResetCanvas(object sender, RoutedEventArgs e)
        {
            _allShapes.Clear();
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
        
            using var fs = new FileStream(dlg.FileName, FileMode.Create);
            var s = new XmlSerializer(typeof(List<ShapeT>));

            List<ShapeT> shapesList = _allShapes
                .Select(shape => new ShapeT() {
                    ClassName = shape.GetType().Name,
                    Points = shape.Points,
                    Thickness = shape.Thickness,
                    Color = shape.Color.ToArgb() })
                .ToList();

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
            using var fs = new FileStream($"{dlg.FileName}", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            try
            {
                var loadedObject = s.Deserialize(fs);
                if (loadedObject != null)
                {
                    var loadedList = (List<ShapeT>)loadedObject;
                    _allShapes.Clear();

                    foreach (var shape in loadedList)
                    {
                        switch(shape.ClassName)
                        {
                            case "Line":
                                _allShapes.Add(new Line(shape.Points, shape.Thickness, Color.FromArgb(shape.Color)));
                                break;
                            case "Polygon":
                                _allShapes.Add(new Polygon(shape.Points, shape.Thickness, Color.FromArgb(shape.Color)));
                                break;
                            case "Circle":
                                _allShapes.Add(new Circle(shape.Points, shape.Thickness, Color.FromArgb(shape.Color)));
                                break;
                            case "CircleArc":
                                _allShapes.Add(new CircleArc(shape.Points, shape.Thickness, Color.FromArgb(shape.Color)));
                                break;
                        }
                    }
                }

                RedrawCanvas();
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show("Error in file!", "Error!");
            }
        }
        
        //-----------
        private void DummyCallBack(object sender, RoutedEventArgs e)
        { }

    }

}