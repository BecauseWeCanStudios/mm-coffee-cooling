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
    
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Labels = new List<string>();
            Series = new SeriesCollection();
            Model.CalculationCompleted += OnCalculationCompleted;
            UpdatePlot();
            DataContext = this;
        }

        private delegate void UpdateDataDelegate(Model.Result result);

        void UpdateData(Model.Result result)
        {
            Series.Clear();
            foreach (var it in result.ApproximationData)
            {
                Series.Add(new LineSeries
                {
                    Title = it.Method.ToString(),
                    Values = new ChartValues<double>(it.Values),
                    LineSmoothness = 0,
                    PointGeometry = null,
                    Fill = new SolidColorBrush(),
                });
            };
            Labels.Clear();
            Labels.AddRange(result.ArgumentValues.ConvertAll(new Converter<double, string>((double x) => { return x.ToString(); })));
        }

        void OnCalculationCompleted(object sender, Model.Result result)
        {
            Dispatcher.Invoke(new UpdateDataDelegate(UpdateData), result);
        }

        public List<string> Labels { get; set; }
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
            System.Globalization.CultureInfo ci = System.Threading.Thread.CurrentThread.CurrentCulture;
            string decimalSeparator = ci.NumberFormat.CurrencyDecimalSeparator;
            var textBox = sender as TextBox;
            e.Handled = !Regex.IsMatch(textBox.Text + e.Text, @"^[-+]?[0-9]*" + decimalSeparator + @"?[0-9]*$");
        }

        private void TB_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.IsLoaded)
            {
                UpdatePlot();
            }
        }

        private void TB_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = e.Key == Key.Space;
        }

        private String PassDefaultIfEmpty(String s)
        {
            return String.IsNullOrEmpty(s) ? "1" : s;
        }

        private void UpdatePlot() => Model.BeginCalculation(new Model.Parameters()
        {
            InitialTemperature = Convert.ToDouble(PassDefaultIfEmpty(StartTempTB.Text)),
            CoolingCoefficient = Convert.ToDouble(PassDefaultIfEmpty(CoolingCoefficientTB.Text)),
            EnvironmentTemperature = Convert.ToDouble(PassDefaultIfEmpty(AmbientTempTB.Text)),
            SegmentCount = Convert.ToInt32(PassDefaultIfEmpty(SegmentCountTB.Text)),
            TimeRange = Convert.ToDouble(PassDefaultIfEmpty(TimeRangeTB.Text)),
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
    
}