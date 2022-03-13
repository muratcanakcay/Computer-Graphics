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
            Debug.WriteLine(AppliedFilters.Count);
            ApplyFilters();
        }

        private void InvertCheckbox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            AppliedFilters.RemoveAll((filter) => filter is Inversion);
            Debug.WriteLine(AppliedFilters.Count);
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
    }
}
