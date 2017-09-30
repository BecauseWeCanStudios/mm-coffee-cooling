using System;
using System.Collections.Generic;

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
            public double Step { get; private set; }
            public Tuple<double, double> TemperatureRange
            {
                get { return TemperatureRange; }
                set
                {
                    TemperatureRange = value;
                    Step = value.Item2 - value.Item1;
                }
            }
            public List<Methods> Methods;
        }

        public struct ApproximationData
        { 
            public Methods Method;
            public List<double> Values;
        }

        public struct Result
        {
            public List<double> ArgumentValues;
            public List<ApproximationData> ApproximationData;
        }

    }
}
