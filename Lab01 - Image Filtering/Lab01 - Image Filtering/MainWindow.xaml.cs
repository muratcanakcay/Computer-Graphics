using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Microsoft.Win32;

namespace Lab01___Image_Filtering
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WriteableBitmap? _originalImage;

        public List<IFilter> AppliedFilters = new List<IFilter>(); 

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new()
            {
                Filter = "Image files|*.jpg;*.png;*.bmp|All Files (*.*)|*.*",
                RestoreDirectory = true
            };

            if (dlg.ShowDialog() != true) return;

            _originalImage = new WriteableBitmap(new BitmapImage(new Uri(dlg.FileName)));
            OriginalImageCanvas.Source = _originalImage;
            FilteredImageCanvas.Source = _originalImage;
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to leave?", "Exit", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                Close();
        }

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

        private void ApplyFilters()
        {
            if (AppliedFilters.Count == 0 || _originalImage == null)
            {
                FilteredImageCanvas.Source = _originalImage;
                return;
            }

            var filteredImage = _originalImage;

            foreach (var filter in AppliedFilters)
            {
                filteredImage = filteredImage.ApplyFilter(filter);
            }

            FilteredImageCanvas.Source = filteredImage;
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
    }
}
