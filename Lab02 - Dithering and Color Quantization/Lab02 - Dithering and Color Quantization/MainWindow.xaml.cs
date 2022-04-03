using System;
using System.Collections.Generic;
using System.IO;
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
using System.Xml.Serialization;
using Lab01___Image_Filtering;
using Microsoft.Win32;

namespace Lab02___Dithering_and_Color_Quantization
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly List<WriteableBitmap> _history = new();
        private int _stackPosition = -1;

        public List<IFilter> AppliedFilters = new(); 

        public MainWindow()
        {
            InitializeComponent();
        }

        //---------------------------

        private void Reset_OnClick(object sender, RoutedEventArgs e)
        {
            ClearAllFilters();

            WriteableBitmap? originalImage = null;
            
            if (_history.Count > 0)
            {
               originalImage = _history[0];
            }

            _history.Clear();
            _stackPosition = 0;
            if (originalImage != null) _history.Add(originalImage);
            FilteredImageCanvas.Source = originalImage;
            FilterChainTextBlock.Text = "In > Out";
            BackButton.IsEnabled = false;
            ForwardButton.IsEnabled = false;
            FlattenButton.IsEnabled = false;
        }

        private void ClearAllFilters()
        {
            InvertCheckbox.IsChecked = false;
            BrightnessCheckbox.IsChecked = false;
            ContrastCheckbox.IsChecked = false;
            GammaCheckbox.IsChecked = false;
            BlurCheckbox.IsChecked = false;
            GaussianBlurCheckbox.IsChecked = false;
            SharpenCheckbox.IsChecked = false;
            EdgeDetectionCheckbox.IsChecked = false;
            EmbossCheckbox.IsChecked = false;
            MedianCheckbox.IsChecked = false;
            CustomFunctionCheckbox.IsChecked = false;
            
            BrightnessSlider.Value = 0;
            ContrastSlider.Value = 1;
            GammaSlider.Value = 1;
            FunctionPolyline.Points = new PointCollection() { new(0, 255), new(255, 0) };

            AppliedFilters.Clear();
        }

        private void LoadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new()
            {
                Filter = "Image files|*.jpg;*.png;*.bmp",
                RestoreDirectory = true
            };

            if (dlg.ShowDialog() != true) return;

            WriteableBitmap? originalImage = null;

            try
            {
                originalImage = new WriteableBitmap(new BitmapImage(new Uri(dlg.FileName)));
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                return;
            }
            
            ClearAllFilters();
            _history.Clear();
            OriginalImageCanvas.Source = originalImage;
            _history.Add(originalImage);
            _stackPosition = 0;
            FilteredImageCanvas.Source = _history[_stackPosition];
            FilterChainTextBlock.Text = "In > Out";
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            if (FilteredImageCanvas.Source == null)
            {
                MessageBox.Show("No image to save!", "No image to save");
                return;
            }
            
            SaveFileDialog dlg = new()
            {
                Filter = "Image files|*.jpg;*.png;*.bmp",
                RestoreDirectory = true
            };

            if (dlg.ShowDialog() != true || dlg.FileName == string.Empty) return;

            BitmapEncoder encoder;
            if (dlg.FileName.EndsWith("png"))
            {
                encoder = new PngBitmapEncoder();
            }
            else if (dlg.FileName.EndsWith("bmp"))
            {
                encoder = new BmpBitmapEncoder();
            }
            else
            {
                encoder = new JpegBitmapEncoder();
            }

            using var fs = new FileStream(dlg.FileName, FileMode.OpenOrCreate);
            encoder.Frames.Add(BitmapFrame.Create((WriteableBitmap)FilteredImageCanvas.Source));
            encoder.Save(fs);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to leave?", "Exit", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                Close();
        }

        //---------------------------

        private void InvertCheckbox_OnChecked(object sender, RoutedEventArgs e)
        {
            AppliedFilters.Add(new Inversion());
            ApplyFilters();
        }

        private void InvertCheckbox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            AppliedFilters.RemoveAll((filter) => filter is Inversion);
            ApplyFilters();
        }

        private void BrightnessCheckbox_OnChecked(object sender, RoutedEventArgs e)
        {
            AppliedFilters.Add(new Brightness((int)Math.Floor(BrightnessSlider.Value)));
            ApplyFilters();
        }

        private void BrightnessCheckbox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            AppliedFilters.RemoveAll((filter) => filter is Brightness);
            ApplyFilters();
        }

        private void BrightnessSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (BrightnessCheckbox.IsChecked == false) return;

            var brightnessFilter = (Brightness?)AppliedFilters.Find((filter) => filter is Brightness);

            if (brightnessFilter == null) throw new NullReferenceException();

            brightnessFilter.Coefficient = (int)Math.Floor(BrightnessSlider.Value);
            
            ApplyFilters();
        }

        private void ContrastCheckbox_OnChecked(object sender, RoutedEventArgs e)
        {
            AppliedFilters.Add(new Contrast(ContrastSlider.Value));
            ApplyFilters();
        }

        private void ContrastCheckbox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            AppliedFilters.RemoveAll((filter) => filter is Contrast);
            ApplyFilters();
        }

        private void ContrastSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ContrastCheckbox.IsChecked == false) return;

            var contrastFilter = (Contrast?)AppliedFilters.Find((filter) => filter is Contrast);

            if (contrastFilter == null) throw new NullReferenceException();

            contrastFilter.Coefficient = ContrastSlider.Value;
            
            ApplyFilters();
        }

        private void GammaCheckbox_OnChecked(object sender, RoutedEventArgs e)
        {
            AppliedFilters.Add(new Gamma(GammaSlider.Value));
            ApplyFilters();
        }

        private void GammaCheckbox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            AppliedFilters.RemoveAll((filter) => filter is Gamma);
            ApplyFilters();
        }

        private void GammaSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (GammaCheckbox.IsChecked == false) return;

            var gammaFilter = (Gamma?)AppliedFilters.Find((filter) => filter is Gamma);

            if (gammaFilter == null) throw new NullReferenceException();

            gammaFilter.Coefficient = GammaSlider.Value;
            
            ApplyFilters();
        }

        //---------------------------

        private void BlurCheckbox_OnChecked(object sender, RoutedEventArgs e)
        {
            AppliedFilters.Add(new Blur());
            ApplyFilters();
        }

        private void BlurCheckbox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            AppliedFilters.RemoveAll((filter) => filter is Blur);
            ApplyFilters();
        }

        private void GaussianBlurCheckbox_OnChecked(object sender, RoutedEventArgs e)
        {
            AppliedFilters.Add(new GaussianBlur());
            ApplyFilters();
        }

        private void GaussianBlurCheckbox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            AppliedFilters.RemoveAll((filter) => filter is GaussianBlur);
            ApplyFilters();
        }

        private void SharpenCheckbox_OnChecked(object sender, RoutedEventArgs e)
        {
            AppliedFilters.Add(new Sharpen());
            ApplyFilters();
        }

        private void SharpenCheckbox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            AppliedFilters.RemoveAll((filter) => filter is Sharpen);
            ApplyFilters();
        }

        private void EdgeDetectionCheckbox_OnChecked(object sender, RoutedEventArgs e)
        {
            AppliedFilters.Add(new EdgeDetection());
            ApplyFilters();
        }

        private void EdgeDetectionCheckbox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            AppliedFilters.RemoveAll((filter) => filter is EdgeDetection);
            ApplyFilters();
        }

        private void EmbossCheckbox_OnChecked(object sender, RoutedEventArgs e)
        {
            AppliedFilters.Add(new Emboss());
            ApplyFilters();
        }

        private void EmbossCheckbox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            AppliedFilters.RemoveAll((filter) => filter is Emboss);
            ApplyFilters();
        }

        //---------------------------

        private int? _capturedNodeIndex;
        private Point? CapturedNode => _capturedNodeIndex is int i ? FunctionPolyline.Points[i] : null;

        private void PolyLineNode_LeftMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source is not Line polylineNode) return;

            var clickPosition = e.GetPosition(CustomFunctionFilterCanvas);
            
            foreach (var p in FunctionPolyline.Points)
            {
                if (calculateDistance(clickPosition, p) >= 20) continue;
                
                _capturedNodeIndex = FunctionPolyline.Points.IndexOf(p);
                return;
            }
        }

        private void PolyLineNode_LeftMouseUp(object sender, MouseButtonEventArgs e)
        {
            _capturedNodeIndex = null;
            RearrangeNodes();
        }

        private void PolyLineNode_RightMouseUp(object sender, MouseButtonEventArgs e)
        {
            var clickPosition = e.GetPosition(CustomFunctionFilterCanvas);

            for (var i = 1; i < FunctionPolyline.Points.Count-1; i++)
            {
                var p1 = FunctionPolyline.Points[i];

                if (calculateDistance(clickPosition, p1) < 10)
                {
                    FunctionPolyline.Points.Remove(p1);
                    RearrangeNodes();
                    return;
                }
            }
        }

        private void CustomFunctionFilterCanvas_LeftMouseDown(object sender, MouseButtonEventArgs e)
        {
            var clickPosition = e.GetPosition(CustomFunctionFilterCanvas);

            if (calculateDistance(clickPosition, FunctionPolyline.Points.Last()) < 10) return;

            for (var i = 0; i < FunctionPolyline.Points.Count-1; i++)
            {
                var p1 = FunctionPolyline.Points[i];
                var p2 = FunctionPolyline.Points[i+1];
                
                if (calculateDistance(clickPosition, p1) < 20 || calculateDistance(clickPosition, p2) < 20 ) return;

                if (calculateDistance(p1, clickPosition) + calculateDistance(clickPosition, p2) ==
                    calculateDistance(p1, p2))
                {
                    FunctionPolyline.Points.Add(clickPosition);
                    RearrangeNodes();
                    return;
                }
            }
        }

        private void CustomFunctionFilterCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            // if a node is captured move it
            if (CapturedNode is Point initialPosition && _capturedNodeIndex is int nodeIndex)
            {
                var mousePosition = e.GetPosition(CustomFunctionFilterCanvas);
                var displacement = mousePosition - initialPosition;
                var newPosition = initialPosition + displacement;

                // check for edges and neighbor nodes
                if (nodeIndex == 0)
                {
                    newPosition.X = 0;
                }
                else if (nodeIndex == FunctionPolyline.Points.Count - 1)
                {
                    newPosition.X = 255;
                }
                else if (newPosition.X <= FunctionPolyline.Points[nodeIndex - 1].X)
                {
                    newPosition.X = FunctionPolyline.Points[nodeIndex-1].X + 1;
                }
                else if (newPosition.X >= FunctionPolyline.Points[nodeIndex + 1].X)
                {
                    newPosition.X = FunctionPolyline.Points[nodeIndex+1].X - 1;
                }

                if (newPosition.Y < 0)
                {
                    newPosition.Y = 0;
                }
                else if (newPosition.Y > 255)
                {
                    newPosition.Y = 255;
                }

                FunctionPolyline.Points[nodeIndex] = newPosition;
                FunctionPolyline.Points = new(FunctionPolyline.Points);
            }
        }

        private void CustomFunctionFilterCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            _capturedNodeIndex = null;
            RearrangeNodes();
        }

        private void PolylineTemplate_Inverse(object sender, RoutedEventArgs e)
        {
            FunctionPolyline.Points = new PointCollection() { new(0, 0), new(255, 255) };
            FunctionPolyline_OnPolyLineChanged();
        }

        private void PolylineTemplate_Brightness(object sender, RoutedEventArgs e)
        {
            FunctionPolyline.Points = new PointCollection() { new(0, 150), new(150, 0), new(255, 0) };
            FunctionPolyline_OnPolyLineChanged();
        }

        private void PolylineTemplate_Contrast(object sender, RoutedEventArgs e)
        {
            FunctionPolyline.Points = new PointCollection() { new(0, 255), new(75, 255), new(180, 0), new(255, 0) };
            FunctionPolyline_OnPolyLineChanged();
        }

        private void Polyline_ResetClick(object sender, RoutedEventArgs e)
        {
            FunctionPolyline.Points = new PointCollection() { new(0, 255), new(255, 0) };
            FunctionPolyline_OnPolyLineChanged();
        }
        private void RearrangeNodes()
        {
            var polylineNodes = new List<Point>(FunctionPolyline.Points);
            polylineNodes.Sort((p1, p2) => p1.X.CompareTo(p2.X));
            
            FunctionPolyline.Points = new(polylineNodes);            
            FunctionPolyline_OnPolyLineChanged();
        }
        private int calculateDistance(Point p1, Point p2)
        {
            return (int)Math.Round(Math.Sqrt(Math.Pow((p2.X - p1.X), 2) + Math.Pow((p2.Y - p1.Y), 2)));
        }
        
        //---------------------------

        private void CustomFunctionCheckbox_OnChecked(object sender, RoutedEventArgs e)
        {
            AppliedFilters.Add(new CustomFunction(FunctionPolyline.Points.ToArray()));
            ApplyFilters();
        }

        private void CustomFunctionCheckbox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            AppliedFilters.RemoveAll((filter) => filter is CustomFunction);
            ApplyFilters();
        }

        private void FunctionPolyline_OnPolyLineChanged()
        {
            if (CustomFunctionCheckbox.IsChecked == false) return;

            var customFunctionFilter = (CustomFunction?)AppliedFilters.Find((filter) => filter is CustomFunction);

            if (customFunctionFilter == null) throw new NullReferenceException();

            customFunctionFilter.NodeList = FunctionPolyline.Points.ToArray();
            
            ApplyFilters();
        }

        private void ExportButton_OnClick(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "XML Save File|*.xml";

            if (saveFileDialog.ShowDialog() != true || saveFileDialog.FileName.Equals("")) return;

            using var fileStream = new FileStream($"{saveFileDialog.FileName}", FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            XmlSerializer serializer = new XmlSerializer(typeof(PointCollection));
            serializer.Serialize(fileStream, FunctionPolyline.Points);
        }

        private void ImportButton_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML Save File|*.xml";

            if (openFileDialog.ShowDialog() != true || openFileDialog.FileName.Equals("")) return;
            
            using var fileStream = new FileStream($"{openFileDialog.FileName}", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            try
            {
                XmlSerializer deserializer = new XmlSerializer(typeof(PointCollection));
                var loadedPointCollection = (PointCollection)deserializer.Deserialize(fileStream);
                FunctionPolyline.Points = new(loadedPointCollection);
            }
            catch (Exception)
            {
                MessageBox.Show("There's an error in the file!", "Error!");
                return;
            }

            if (FunctionPolyline.Points.Count == 0)
                MessageBox.Show("No data loaded. There might be an error in the file!", "Warning!");
        }

        //---------------------------

        private void MedianCheckbox_OnChecked(object sender, RoutedEventArgs e)
        {
            AppliedFilters.Add(new Median());
            ApplyFilters();
        }

        private void MedianCheckbox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            AppliedFilters.RemoveAll((filter) => filter is Median);
            ApplyFilters();
        }

        //---------------------------

        private void ApplyFilters()
        {
            if (AppliedFilters.Count == 0)
            {
                FlattenButton.IsEnabled = false;

                FilteredImageCanvas.Source = _history.Count == 0 ? null : _history[_stackPosition];
                FilterChainTextBlock.Text = _history.Count == 1 ? "In > Out" : _stackPosition == 0 ? "In > Out" : $"In > Flattened ({_stackPosition}/{_history.Count-1}) > Out";
                return;
            }

            FlattenButton.IsEnabled = true;

            var filteredImage = _history[_stackPosition];
            var sb = new StringBuilder();
            
            sb.Append("In >");
            if (_history.Count > 1)
            {
                sb.Append( _stackPosition == 0 ? "In > Out" : $" Flattened ({_stackPosition}/{_history.Count-1}) >");
            }

            foreach (var filter in AppliedFilters)
            {
                filteredImage = filteredImage.ApplyFilter(filter);
                sb.Append(" " + filter + " >");
            }

            sb.Append(" Out");

            FilteredImageCanvas.Source = filteredImage;
            FilterChainTextBlock.Text = sb.ToString();
        }

        //------------------------

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            _stackPosition--;
            ForwardButton.IsEnabled = true;
            
            if (_stackPosition == 0)
            {
                BackButton.IsEnabled = false;
            }
            
            FilteredImageCanvas.Source = _history[_stackPosition];
            ClearAllFilters();
            ApplyFilters();
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            _stackPosition++;
            BackButton.IsEnabled = true;
            if (_stackPosition == _history.Count - 1)
            {
                ForwardButton.IsEnabled = false;
            }

            FilteredImageCanvas.Source = _history[_stackPosition];
            ClearAllFilters();
            ApplyFilters();
        }

        private void FlattenButton_Click(object sender, RoutedEventArgs e)
        {
            if (_stackPosition < _history.Count - 1)
            {
                _history.RemoveRange(_stackPosition + 1, _history.Count - _stackPosition -1);
                ForwardButton.IsEnabled = false;
            }

            _history.Add((WriteableBitmap)FilteredImageCanvas.Source.Clone());
            _stackPosition++;
            BackButton.IsEnabled = true;
            FilteredImageCanvas.Source = _history[_stackPosition];
            ClearAllFilters();
            ApplyFilters();
        }

    }
}