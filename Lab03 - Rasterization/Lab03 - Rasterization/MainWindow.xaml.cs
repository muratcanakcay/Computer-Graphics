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

        public MainWindow()
        {
            InitializeComponent();
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
                ToggleIsDrawingLine();
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
