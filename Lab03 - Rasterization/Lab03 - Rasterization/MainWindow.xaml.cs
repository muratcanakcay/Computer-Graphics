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
        private static readonly Point NullPoint = new(-1, -1);
        private Point _startingPoint = NullPoint;
        private Point _endingPoint = NullPoint;
        private List<Line> _allShapes = new();
        private WriteableBitmap _wbm;

        public MainWindow()
        {
            InitializeComponent();

            _wbm = new WriteableBitmap((int)TheCanvas.Width,
                                                    (int)TheCanvas.Height, 
                                                    96, 
                                                    96, 
                                                    PixelFormats.Bgr32, 
                                                    null);
            
            try
            {
                _wbm.Lock();

                for (int x = 0; x < _wbm.Width; x++)
                {
                    for (int y = 0; y < _wbm.Height; y++)
                    {
                        _wbm.SetPixelColor(x, y, Color.FromArgb(255, 255, 255, 255));
                    }
                
                }
            }
            finally
            {
                _wbm.Unlock();
            }
            
            

            var brush = new ImageBrush
            {
                ImageSource = _wbm
            };

            TheCanvas.Background = brush;
        }

        private void dummyCallBack(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void TheCanvas_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var clickPosition = e.GetPosition(TheCanvas);
            Debug.WriteLine("CLICKED!");
            Debug.WriteLine($"{clickPosition.X}, {clickPosition.Y}");

            if (_isDrawingLine) DrawLine(clickPosition);

        }

        private void DrawLine(Point clickPosition)
        {
            if (_startingPoint.Equals(NullPoint))
            {
                _startingPoint = clickPosition;
                Debug.WriteLine($"Starting: {clickPosition.X}, {clickPosition.Y}");
            }
            else
            {
                _endingPoint = clickPosition;
                Debug.WriteLine($"Ending: {clickPosition.X}, {clickPosition.Y}");
                _allShapes.Add(new Line(_startingPoint, _endingPoint));
                ToggleIsDrawingLine();

                foreach (var line in _allShapes)
                {
                    Debug.WriteLine(line);
                    line.Draw(_wbm);
                }

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
            if (_isDrawingLine == false) _startingPoint = NullPoint;
        }
    }
}
