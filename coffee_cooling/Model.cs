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
            private double Step;
            private double InitialTemperature;
            private double Count;

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
            }

            private List<double> Analytical()
            {
                List<double> result = new List<double>
                {
                    InitialTemperature
                };
                for (int i = 1; i < Count; ++i)
                    result.Add(TemperatureFunction(Step * i));
                return result;
            }

            private List<double> Euler()
            {
                List<double> result = new List<double>
                {
                    InitialTemperature
                };
                for (int i = 0; i < Count - 1; ++i)
                    result.Add(result[i] + Step * Function(result[i]));
                return result;
            }

            private List<double> MEuler()
            {
                List<double> result = new List<double>()
                {
                    InitialTemperature
                };
                for (int i = 0; i < Count - 1; ++i)
                {
                    double y = result[i] + Step * Function(result[i]);
                    result.Add(result[i] + Step * (Function(result[i]) + Function(y)) / 2.0);
                }
                return result;
            }

            private List<double> RK4()
            {
                List<double> result = new List<double>()
                {
                    InitialTemperature
                };
                for (int i = 0; i < Count - 1; ++i)
                {
                    double k1 = Function(result[i]);
                    double k2 = Function(result[i] + Step * k1 / 2.0);
                    double k3 = Function(result[i] + Step * k2 / 2.0);
                    double k4 = Function(result[i] + Step * k3);
                    result.Add(result[i] + Step * (k1 + 2.0 * k2 + 2.0 * k3 + k4) / 6.0);
                }
                return result;
            }

            public List<double> Calculate(Methods method)
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
                        return null;
                }
            }

        }

        public static event EventHandler<Result> CalculationCompleted;

        private static void Calculate(Parameters parameters)
        {
            Calculation calculation = new Calculation(parameters);
            List<double> argumentValues = 
                new List<double> (from i in Enumerable.Range(0, parameters.SegmentCount)
                               select parameters.TimeRange * i);
            List<ApproximationData> approximationData = new List<ApproximationData>();
            foreach (Methods method in parameters.Methods)
                approximationData.Add(
                    new ApproximationData()
                    {
                        Method = method,
                        Values = calculation.Calculate(method)
                    }
                );
            CalculationCompleted(null,
                new Result()
                {
                    ArgumentValues = argumentValues,
                    ApproximationData = approximationData
                });
        }

        public static void BeginCalculation(Parameters parameters)
        {
            //Thread thread = new Thread(() => Calculate(parameters));
            //thread.Start();
            Application.Current.Dispatcher.Invoke((Action)delegate {
                Calculate(parameters);
            });
        }

    }
}
