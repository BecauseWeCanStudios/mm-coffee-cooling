using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;

namespace coffee_cooling
{
    public static class Model
    {

        public enum Methods { Analytical, Euler, MEuler, RK4 }

        public struct Parameters
        {
            public double InitialTemperature;
            public double CoolingCoefficient;
            public double EnvironmentTemperature;
            public int SegmentCount;
            public double TimeRange;
            public List<Methods> Methods;
        }

        public struct ApproximationData
        { 
            public Methods Method;
            public List<double> Values;
            public List<double> Error;
        }

        public class Result : EventArgs
        {
            public List<double> ArgumentValues;
            public List<ApproximationData> ApproximationData;
        }

        private class Calculation
        {
            private Func<double, double> Function;
            private Func<double, double> TemperatureFunction;
            private List<double> AnaliticalValues;
            private double Step;
            private double InitialTemperature;
            private int Count;

            public Calculation(Parameters parameters)
            {
                Function = (double y) =>
                {
                    return -parameters.CoolingCoefficient * (y - parameters.EnvironmentTemperature);
                };
                TemperatureFunction = (double t) =>
                {
                    return parameters.EnvironmentTemperature + Math.Exp(parameters.CoolingCoefficient * (-t)) * 
                        (parameters.InitialTemperature - parameters.EnvironmentTemperature);
                };
                Step = parameters.TimeRange / parameters.SegmentCount;
                InitialTemperature = parameters.InitialTemperature;
                Count = parameters.SegmentCount;
                AnaliticalValues = new List<double>(from i in Enumerable.Range(0, Count) select TemperatureFunction(Step * i));
            }

            private ApproximationData Analytical()
            {
                return new ApproximationData()
                {
                    Method = Methods.Analytical,
                    Values = AnaliticalValues,
                    Error = null
                };
            }

            private ApproximationData GetInitialData(Methods method)
            {
                return new ApproximationData()
                {
                    Method = method,
                    Values = new List<double>() { InitialTemperature },
                    Error = new List<double>() { 0 }
                };
            }

            private ApproximationData Euler()
            {
                ApproximationData data = GetInitialData(Methods.Euler);
                for (int i = 0; i < Count - 1; ++i)
                {
                    data.Values.Add(data.Values[i] + Step * Function(data.Values[i]));
                    data.Error.Add(Math.Abs(data.Values[i + 1] - AnaliticalValues[i + 1]));
                }
                return data;
            }

            private ApproximationData MEuler()
            {
                ApproximationData data = GetInitialData(Methods.MEuler);
                for (int i = 0; i < Count - 1; ++i)
                {
                    double y = data.Values[i] + Step * Function(data.Values[i]);
                    data.Values.Add(data.Values[i] + Step * (Function(data.Values[i]) + Function(y)) / 2.0);
                    data.Error.Add(Math.Abs(data.Values[i + 1] - AnaliticalValues[i + 1]));
                }
                return data;
            }

            private ApproximationData RK4()
            {
                ApproximationData data = GetInitialData(Methods.RK4);
                for (int i = 0; i < Count - 1; ++i)
                {
                    double k1 = Function(data.Values[i]);
                    double k2 = Function(data.Values[i] + Step * k1 / 2.0);
                    double k3 = Function(data.Values[i] + Step * k2 / 2.0);
                    double k4 = Function(data.Values[i] + Step * k3);
                    data.Values.Add(data.Values[i] + Step * (k1 + 2.0 * k2 + 2.0 * k3 + k4) / 6.0);
                    data.Error.Add(Math.Abs(data.Values[i + 1] - AnaliticalValues[i + 1]));
                }
                return data;
            }

            public ApproximationData Calculate(Methods method)
            {
                switch (method)
                {
                    case Methods.Analytical:
                        return Analytical();
                    case Methods.Euler:
                        return Euler();
                    case Methods.MEuler:
                        return MEuler();
                    case Methods.RK4:
                        return RK4();
                    default:
                        return new ApproximationData();
                }
            }

        }

        public static event EventHandler<Result> CalculationCompleted;

        private static void Calculate(Parameters parameters)
        {
            Calculation calculation = new Calculation(parameters);
            double step = parameters.TimeRange / parameters.SegmentCount;
            List<double> argumentValues = 
                new List<double> (from i in Enumerable.Range(0, parameters.SegmentCount)
                               select step * i);
            List<ApproximationData> approximationData = new List<ApproximationData>();
            foreach (Methods method in parameters.Methods)
                approximationData.Add(calculation.Calculate(method));
            CalculationCompleted(null,
                new Result()
                {
                    ArgumentValues = argumentValues,
                    ApproximationData = approximationData
                });
        }

        public static void BeginCalculation(Parameters parameters)
        {
            Thread thred = new Thread(() => Calculate(parameters));
            thred.Start();
        }

    }
}
