using System;
using System.Collections.Generic;
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
using MahApps.Metro.Controls;
using LiveCharts;
using LiveCharts.Wpf;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Text.RegularExpressions;

namespace coffee_cooling
{
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Series = new SeriesCollection();
            Model.CalculationCompleted += OnCalculationCompleted;
            UpdatePlot();
            DataContext = this;
        }

        void OnCalculationCompleted(object sender, Model.Result result)
        {
            Series.Clear();
            foreach (var it in result.ApproximationData)
            {
                Series.Add(new LineSeries
                {
                    Title = it.Method.ToString(),
                    Values = new ChartValues<double>(it.Values),
                    LineSmoothness = 0,
                });
            };
            Labels = new List<double>(result.ArgumentValues);
            Plot.InvalidateVisual();
        }

        //public double[] Labels { get; set; }
        public List<double> Labels { get; set; }
        public Func<double, string> YFormatter { get; set; }

        public SeriesCollection Series { get; set; }

        private void ListBox_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var item = ItemsControl.ContainerFromElement(ListBox, (DependencyObject)e.OriginalSource) as ListBoxItem;
            if (item == null) return;

            var series = (LineSeries)item.Content;
            series.Visibility = series.Visibility == Visibility.Visible
                ? Visibility.Hidden
                : Visibility.Visible;
        }

        private void DoubleTBPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            e.Handled = !Regex.IsMatch(textBox.Text + e.Text, @"^[-+]?[0-9]*[\.,]?[0-9]*$");
        }

        private void TB_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (((TextBox)sender).Text == "")
            {
                ((TextBox)sender).Text = "1";
            }
            if (this.IsLoaded)
            {
                UpdatePlot();
            }
        }

        private void UpdatePlot() => Model.BeginCalculation(new Model.Parameters()
        {
            InitialTemperature = Convert.ToDouble(StartTempTB.Text),
            CoolingCoefficient = Convert.ToDouble(CoolingCoefficientTB.Text),
            EnvironmentTemperature = Convert.ToDouble(AmbientTempTB.Text),
            SegmentCount = Convert.ToInt32(SegmentCountTB.Text),
            TimeRange = Convert.ToDouble(TimeRangeTB.Text),
            Methods = new List<Model.Methods>()
                {
                    Model.Methods.Analytical, Model.Methods.Euler, Model.Methods.MEuler, Model.Methods.RK4
                }
        });

        private void IntTB_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            e.Handled = !Regex.IsMatch(textBox.Text + e.Text, @"^[-+]?[0-9]*$");
        }
    }

    public class OpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (Visibility)value == Visibility.Visible
                ? 1d
                : .2d;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ReverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((SeriesCollection)value).Reverse();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringEmptyConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return string.IsNullOrEmpty((string)value) ? parameter : value;
        }

        public object ConvertBack(
              object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }
}