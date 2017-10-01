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
            public List<double> Values;
            public List<double> Error;
            public double StandardDeviation;
        }

        public class Result : EventArgs
        {
            public List<double> ArgumentValues;
            public Dictionary<Methods, ApproximationData> ApproximationData;
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
                AnaliticalValues = new List<double>(from i in Enumerable.Range(0, Count + 1) select TemperatureFunction(Step * i));
            }

            private ApproximationData Analytical()
            {
                return new ApproximationData()
                {
                    Values = AnaliticalValues,
                    Error = null,
                    StandardDeviation = 0
                };
            }

            private double Euler(double y)
            {
                return y + Step * Function(y);
            }

            private double MEuler(double y)
            {
                return y + Step * (Function(y) + Function(y + Step * Function(y))) / 2.0;
            }

            private double RK4(double y)
            {
                double k1 = Function(y);
                double k2 = Function(y + Step * k1 / 2.0);
                double k3 = Function(y + Step * k2 / 2.0);
                double k4 = Function(y + Step * k3);
                return y + Step * (k1 + 2.0 * k2 + 2.0 * k3 + k4) / 6.0;
            }

            public ApproximationData Calculate(Methods method)
            {
                Func<double, double> methodFunction = null;
                switch (method)
                {
                    case Methods.Analytical:
                        return Analytical();
                    case Methods.Euler:
                        methodFunction = Euler;
                        break;
                    case Methods.MEuler:
                        methodFunction = MEuler;
                        break;
                    case Methods.RK4:
                        methodFunction = RK4;
                        break;
                    default:
                        return new ApproximationData();
                }
                ApproximationData data = new ApproximationData()
                {
                    Values = new List<double>() { InitialTemperature },
                    Error = new List<double>() { 0 },
                    StandardDeviation = 0
                };
                for (int i = 0; i < Count; ++i)
                {
                    data.Values.Add(methodFunction(data.Values[i]));
                    data.Error.Add(Math.Abs(data.Values[i + 1] - AnaliticalValues[i + 1]));
                    data.StandardDeviation += data.Error[i + 1] * data.Error[i + 1];
                }
                data.StandardDeviation /= Count;
                return data;
            }

        }

        public static event EventHandler<Result> CalculationCompleted;

        private static void Calculate(Parameters parameters)
        {
            Calculation calculation = new Calculation(parameters);
            double step = parameters.TimeRange / parameters.SegmentCount;
            List<double> argumentValues = 
                new List<double> (from i in Enumerable.Range(0, parameters.SegmentCount + 1)
                               select step * i);
            Dictionary<Methods, ApproximationData> approximationData = new Dictionary<Methods, ApproximationData>();
            foreach (Methods method in parameters.Methods)
                approximationData[method] = calculation.Calculate(method);
            CalculationCompleted(null,
                new Result()
                {
                    ArgumentValues = argumentValues,
                    ApproximationData = approximationData
                }
            );
        }

        public static void BeginCalculation(Parameters parameters)
        {
            Thread thred = new Thread(() => Calculate(parameters));
            thred.Start();
        }

    }
}
