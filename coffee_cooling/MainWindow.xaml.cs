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
                    Title = it.Method.ToString(),
                    Values = new ChartValues<double>(it.Values),
                    LineSmoothness = 0,
                    PointGeometry = null,
                    Fill = new SolidColorBrush(),
                });
            };
            Labels.Clear();
            Labels.AddRange(result.ArgumentValues.ConvertAll(new Converter<double, string>((double x) => { return x.ToString(); })));
            //Data.Clear();
            //using (var e1 = (from i in Enumerable.Range(0, result.ArgumentValues.Count()) select i).GetEnumerator())
            //using (var e2 = result.ApproximationData.GetEnumerator())
            //{
            //    while (e1.MoveNext() && e2.MoveNext())
            //    {
            //        var item1 = e1.Current;
            //        var item2 = e2.Current;

            //        // use item1 and item2
            //    }
            //}
        }

        void OnCalculationCompleted(object sender, Model.Result result)
        {
            Dispatcher.Invoke(new UpdateDataDelegate(UpdateData), result);
        }

        public ObservableRangeCollection<string> Labels { get; set; } = new ObservableRangeCollection<string>();

        public SeriesCollection Series { get; set; } = new SeriesCollection();

        public ObservableRangeCollection<DataPoint> Data { get; set; } = new ObservableRangeCollection<DataPoint>();

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

namespace System.Collections.ObjectModel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;

    /// <summary> 
    /// Represents a dynamic data collection that provides notifications when items get added, removed, or when the whole list is refreshed. 
    /// </summary> 
    /// <typeparam name="T"></typeparam> 
    public class ObservableRangeCollection<T> : ObservableCollection<T>
    {

        private const string CountName = nameof(Count);
        private const string IndexerName = "Item[]";

        /// <summary> 
        /// Initializes a new instance of the System.Collections.ObjectModel.ObservableCollection(Of T) class. 
        /// </summary> 
        public ObservableRangeCollection()
            : base()
        {
        }

        /// <summary> 
        /// Initializes a new instance of the System.Collections.ObjectModel.ObservableCollection(Of T) class that contains elements copied from the specified collection. 
        /// </summary> 
        /// <param name="collection">collection: The collection from which the elements are copied.</param> 
        /// <exception cref="System.ArgumentNullException">The collection parameter cannot be null.</exception> 
        public ObservableRangeCollection(IEnumerable<T> collection)
            : base(collection)
        {
        }

        /// <summary> 
        /// Adds the elements of the specified collection to the end of the ObservableCollection(Of T). 
        /// </summary> 
        public void AddRange(IEnumerable<T> collection, NotifyCollectionChangedAction notificationMode = NotifyCollectionChangedAction.Add)
        {
            if (notificationMode != NotifyCollectionChangedAction.Add && notificationMode != NotifyCollectionChangedAction.Reset)
                throw new ArgumentException("Mode must be either Add or Reset for AddRange.", nameof(notificationMode));
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            if (collection is ICollection<T> list)
            {
                if (list.Count == 0) return;
            }
            else if (!collection.Any()) return;
            else list = new List<T>(collection);

            CheckReentrancy();

            int startIndex = Count;
            foreach (var i in collection)
                Items.Add(i);

            NotifyProperties();
            if (notificationMode == NotifyCollectionChangedAction.Reset)
                OnCollectionReset();
            else
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, list as IList ?? list.ToList(), startIndex));
        }

        /// <summary>
        /// Called by base class Collection&lt;T&gt; when the list is being cleared;
        /// raises a CollectionChanged event to any listeners.
        /// </summary>
        protected override void ClearItems()
        {
            if (Count > 0)
                base.ClearItems();
        }


        /// <summary> 
        /// Removes the first occurence of each item in the specified collection from ObservableCollection(Of T).
        /// </summary> 
        public virtual void RemoveRange(IEnumerable<T> collection, NotifyCollectionChangedAction notificationMode = NotifyCollectionChangedAction.Remove)
        {
            if (notificationMode != NotifyCollectionChangedAction.Remove && notificationMode != NotifyCollectionChangedAction.Reset)
                throw new ArgumentException("Mode must be either Remove or Reset for RemoveRange.", nameof(notificationMode));
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            if (Count == 0) return;
            if (collection is ICollection<T> list && list.Count == 0) return;
            else if (!collection.Any()) return;

            CheckReentrancy();
            if (notificationMode == NotifyCollectionChangedAction.Reset)
            {
                foreach (var i in collection)
                    Items.Remove(i);

                OnCollectionReset();
                NotifyProperties();
                return;
            }

            var removed = new Dictionary<int, List<T>>();
            var curSegmentIndex = -1;
            foreach (var item in collection)
            {
                var index = IndexOf(item);
                if (index < 0) continue;

                Items.RemoveAt(index);

                if (!removed.TryGetValue(index - 1, out var segment) && !removed.TryGetValue(index, out segment))
                {
                    curSegmentIndex = index;
                    removed[index] = new List<T> { item };
                }
                else
                    segment.Add(item);
            }

            if (Count == 0)
                OnCollectionReset();
            else
                foreach (var segment in removed)
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, segment.Value, segment.Key));

            NotifyProperties();
        }

        /// <summary> 
        /// Clears the current collection and replaces it with the specified item. 
        /// </summary> 
        public void Replace(T item) => ReplaceRange(new T[] { item });

        /// <summary> 
        /// Clears the current collection and replaces it with the specified collection. 
        /// </summary> 
        /// <param name="noDuplicates">
        /// Sets whether we should ignore items already in the collection when adding items.
        /// false (default) items already existing in the collection will be reused to increase performance.
        /// true - perform regular clear and add, and notify about a reset when done.
        /// </param>
        public void ReplaceRange(IEnumerable<T> collection, bool reset = false)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            if (collection is IList<T> list)
            {
                if (list.Count == 0)
                {
                    Clear();
                    return;
                }
            }
            else if (!collection.Any())
            {
                Clear();
                return;
            }
            else list = new List<T>(collection);

            CheckReentrancy();

            if (reset)
            {
                Items.Clear();
                AddRange(collection, NotifyCollectionChangedAction.Reset);
                return;
            }

            var oldCount = Count;
            var lCount = list.Count;

            for (int i = 0; i < Math.Max(Count, lCount); i++)
            {
                if (i < Count && i < lCount)
                {
                    T old = this[i], @new = list[i];
                    if (Equals(old, @new))
                        continue;
                    else
                    {
                        Items[i] = @new;
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, @new, @old, i));
                    }
                }
                else if (Count > lCount)
                {
                    var removed = new Stack<T>();
                    for (var j = Count - 1; j >= i; j--)
                    {
                        removed.Push(this[j]);
                        Items.RemoveAt(j);
                    }
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed.ToList(), i));
                    break;
                }
                else
                {
                    var added = new List<T>();
                    for (int j = i; j < list.Count; j++)
                    {
                        var @new = list[j];
                        Items.Add(@new);
                        added.Add(@new);
                    }
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, added, i));
                    break;
                }
            }

            NotifyProperties(Count != oldCount);
        }

        void OnCollectionReset() => OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

        void NotifyProperties(bool count = true)
        {
            if (count)
                OnPropertyChanged(new PropertyChangedEventArgs(CountName));
            OnPropertyChanged(new PropertyChangedEventArgs(IndexerName));
        }
    }
}