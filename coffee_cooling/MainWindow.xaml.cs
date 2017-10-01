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
                    Title = MethodNames[it.Key],
                    Values = new ChartValues<double>(it.Value.Values),
                    LineSmoothness = 0,
                    PointGeometry = null,
                    Fill = new SolidColorBrush(),
                });
            };
            Labels.Clear();
            Labels.AddRange(result.ArgumentValues.ConvertAll(new Converter<double, string>((double x) => { return x.ToString(); })));
            Data.Clear();
            using (var n = (from i in Enumerable.Range(0, result.ArgumentValues.Count()) select i).GetEnumerator())
            using (var anlv = result.ApproximationData[Model.Methods.Analytical].Values.GetEnumerator())
            using (var eulv = result.ApproximationData[Model.Methods.Euler].Values.GetEnumerator())
            using (var eule = result.ApproximationData[Model.Methods.Euler].Error.GetEnumerator())
            using (var meulv = result.ApproximationData[Model.Methods.MEuler].Values.GetEnumerator())
            using (var meule = result.ApproximationData[Model.Methods.MEuler].Error.GetEnumerator())
            using (var rk4v = result.ApproximationData[Model.Methods.RK4].Values.GetEnumerator())
            using (var rk4e = result.ApproximationData[Model.Methods.RK4].Error.GetEnumerator())
            using (var time = result.ArgumentValues.GetEnumerator())
            {
                while (n.MoveNext() && anlv.MoveNext() && eulv.MoveNext() && eule.MoveNext() && meulv.MoveNext() && meule.MoveNext() && rk4v.MoveNext() && rk4e.MoveNext())
                {
                    Data.Add(new DataPoint
                    {
                        PointNumber = n.Current,
                        TimePoint = time.Current,
                        AnalyticalSolutionVal = anlv.Current,
                        EulerSolutionVal = eulv.Current,
                        EulerErrorVal = eule.Current,
                        MEulerSolutionVal = meulv.Current,
                        MEulerErrorVal = meule.Current,
                        RK4SolutionVal = rk4v.Current,
                        RK4ErrorVal = rk4e.Current,
                    });
                }
            }
            EulerDeviation = result.ApproximationData[Model.Methods.Euler].StandardDeviation;
        }

        void OnCalculationCompleted(object sender, Model.Result result)
        {
            Dispatcher.Invoke(new UpdateDataDelegate(UpdateData), result);
        }

        public Double EulerDeviation { get; set; }

        public List<string> Labels { get; set; } = new List<string>();

        public SeriesCollection Series { get; set; } = new SeriesCollection();

        public ObservableCollection<DataPoint> Data { get; set; } = new ObservableCollection<DataPoint>();

        public static readonly Dictionary<Model.Methods, string> MethodNames = new Dictionary<Model.Methods, string>()
        {
            {Model.Methods.Analytical, "Аналитический" }, {Model.Methods.Euler, "Эйлера" }, {Model.Methods.MEuler, "Мод. Эйлера" }, {Model.Methods.RK4, "Рунге-Кутты" }
        };

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
            if (String.IsNullOrEmpty(s))
                return "1";
            if (s == "-" || s == "+")
                return s + "1";
            return s;
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

    public class DataPoint
    {
        public int PointNumber { get; set; }
        public double TimePoint { get; set; }
        public double AnalyticalSolutionVal { get; set; }
        public double EulerSolutionVal { get; set; }
        public double EulerErrorVal { get; set; }
        public double MEulerSolutionVal { get; set; }
        public double MEulerErrorVal { get; set; }
        public double RK4SolutionVal { get; set; }
        public double RK4ErrorVal { get; set; }
    }
    
}