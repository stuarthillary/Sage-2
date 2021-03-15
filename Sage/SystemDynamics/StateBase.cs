using Highpoint.Sage.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Highpoint.Sage.SystemDynamics
{
    public abstract class StateBase<T> : StateBase
    {
        public static bool Debug = false;
        public static bool DEFAULT_NEGATIVE_STOCK_MANAGEMENT_SETTING = true;

        public double Start = 0;
        public double Finish = 60;
        public double TimeStep = 0.125;
        public int TimeSliceNdx;
        public bool ManageStocksToStayPositive { get; set; } = DEFAULT_NEGATIVE_STOCK_MANAGEMENT_SETTING;

        public abstract string[] StockNames();
        public abstract string[] FlowNames();

        public List<Func<StateBase<T>, double>> Flows;

        public Func<StateBase<T>, double>[] StockGetters;

        public Action<StateBase<T>, double>[] StockSetters;

        public List<int[]> StockInflows;
        public List<int[]> StockOutflows;

        public abstract StateBase<T> Copy();

        public abstract void Configure(XElement parameters = null);

        // Library functions go here.

        #region Mathematical Functions (3.5.1)

        protected double INF => double.PositiveInfinity;
        protected double PI => Math.PI;

        #endregion

        #region Statistical Fuctions (3.5.2)

        private Dictionary<string, IDoubleDistribution> _distros = new Dictionary<string, IDoubleDistribution>();

        private IDoubleDistribution getDistro(string key, Func<IDoubleDistribution> creator)
        {
            IDoubleDistribution retval;
            if (!_distros.TryGetValue(key, out retval))
            {
                retval = creator();
                _distros.Add(key, retval);
            }
            return retval;
        }

        protected double ExponentialDist(double mean, long seed = long.MaxValue)
        {
            return getDistro(string.Format("Exponential{0}", seed),
                () => new ExponentialDistribution(mean, 1)).GetNext();
        }

        protected double LogNormalDist(double mean, double stdev, long seed = long.MaxValue)
        {
            return getDistro(string.Format("Exponential{0}", seed),
                () => new LognormalDistribution(mean, stdev)).GetNext();
        }

        protected double NormalDist(double mean, double stdev, long seed = long.MaxValue)
        {
            return getDistro(string.Format("Exponential{0}", seed),
                () => new NormalDistribution(mean, stdev)).GetNext();
        }

        protected double PoissonDist(double mean, long seed = long.MaxValue)
        {
            return getDistro(string.Format("Exponential{0}", seed),
                () => new PoissonDistribution(mean)).GetNext();
        }

        protected double UniformDist(double min, double max, long seed = long.MaxValue)
        {
            return getDistro(string.Format("Exponential{0}", seed),
                () => new UniformDistribution(min, max)).GetNext();
        }

        #endregion

        // Delay Functions (3.5.3) in base class.

        #region Test Input Functions (3.5.4)

        protected double Pulse(double magnitude, double firstTime, double interval)
        {
            double tolerance = TimeStep / 1000;
            double timeTilPulse = firstTime - ((TimeSliceNdx * TimeStep) % interval);
            if (Math.Abs(timeTilPulse) < tolerance)
            {
                return magnitude / TimeStep;
            }
            return 0.0;
        }

        protected double Ramp(double slope, double startTime)
        {
            double nIntervals = (TimeSliceNdx * TimeStep) - startTime;
            if (nIntervals > 0)
                return nIntervals * slope / TimeStep;
            return 0.0;
        }

        protected double Step(double height, double startTime)
        {
            double nIntervals = (TimeSliceNdx * TimeStep) - startTime;
            if (nIntervals > 0)
                return height / TimeStep; // It's going to get multiplied in the aggregation stage.
            return 0.0;
        }

        #endregion

        #region Miscellaneous Functions (3.5.6) [NOT COMPLETE]

        protected double InitialValue(int variableIndex)
        {
            throw new NotImplementedException();
        }

        protected double PreviousValue(int variableIndex)
        {
            throw new NotImplementedException();
        }

        // TODO: Figure out what to do about "SELF"... 

        #endregion

        #region Time Functions (3.5.5)

        protected double DeltaT => TimeStep;
        protected double StartTime => Start;
        protected double StopTime => Finish;
        protected double CurrentTime => TimeSliceNdx * TimeStep; // TODO: Compute this once each timestep.

        #endregion

        protected static double Constrain(double min, double max, double val)
        {
            return Math.Max(min, Math.Min(max, val));
        }

        protected virtual void ProcessChildModelsAsEuler(StateBase<T> state)
        {
        }

        public StateBase<T> RunOneTimesliceAsEuler(StateBase<T> state)
        {
            StateBase<T> newState = Copy();
            newState.TimeSliceNdx++;

            double[] deltas = GetDeltaForEachStock(state);

            int nStocks = state.StockNames().Length;
            for (int i = 0; i < nStocks; i++)
            {
                double @is = state.StockGetters[i](state);
                double delta = deltas[i];

                if (double.IsNaN(@is))
                {
                    Console.WriteLine($"Stock {state.StockNames()[i]} is {@is}.");
                }
                if (@is + delta < 0)
                {
                    Console.WriteLine($"Stock {state.StockNames()[i]} is {@is}, and is about to change by {delta}.");
                }
            }

            ProcessChildModelsAsEuler(newState);

            ApplyDeltasToState(newState, deltas);

            return newState;
        }

        public StateBase<T> RunOneTimeSliceAsRK4(StateBase<T> state)
        {
            // Get slopes at ti
            StateBase<T> newState = state.Copy();
            newState.TimeStep /= 2;
            newState.TimeSliceNdx *= 2;
            double[] k1 = GetDeltaForEachStock(newState); // delta from t0
            //Console.Write("Computing {2:0.0} : dY/dt[{0:0.0}] = {1:0.00000}, ", newState.TimeSliceNdx * newState.TimeStep, k1[1] / newState.TimeStep, state.TimeSliceNdx * state.TimeStep);
            Console.Write("{2:0.0},{0:0.0},{1:0.00000}, ", newState.TimeSliceNdx * newState.TimeStep, k1[1] / newState.TimeStep, state.TimeSliceNdx * state.TimeStep);

            // Get slopes at Ti+.5

            newState.TimeSliceNdx++;
            ApplyDeltasToState(newState, k1);
            double[] k2 = GetDeltaForEachStock(newState); // first delta from t0+1/2
            //Console.Write("dY/dt[{0:0.0}] = {1:0.00000}, ", newState.TimeSliceNdx * newState.TimeStep, k2[1] / newState.TimeStep);
            Console.Write("{0:0.0},{1:0.00000}, ", newState.TimeSliceNdx * newState.TimeStep, k2[1] / newState.TimeStep);

            // Apply slopes at Ti+.5 to state at Ti, 
            StateBase<T> newState2 = state.Copy();
            newState2.TimeStep /= 2;
            newState2.TimeSliceNdx *= 2;
            newState2.TimeSliceNdx++;
            ApplyDeltasToState(newState2, k2);
            double[] k3 = GetDeltaForEachStock(newState2); // second delta from t0+1/2
            //Console.Write("dY/dt[{0:0.0}] = {1:0.00000}, ", newState2.TimeSliceNdx * newState2.TimeStep, k3[1] / newState2.TimeStep);
            Console.Write("{0:0.0},{1:0.00000}, ", newState2.TimeSliceNdx * newState2.TimeStep, k3[1] / newState2.TimeStep);

            StateBase<T> newState3 = state.Copy();
            newState3.TimeSliceNdx++;
            newState3.TimeStep /= 2;
            newState3.TimeSliceNdx *= 2;
            ApplyDeltasToState(newState3, k3); // takes initial to halfway using second slope.
            ApplyDeltasToState(newState3, k3); // takes halfway to all the way using second slope.

            double[] k4 = GetDeltaForEachStock(newState3); // delta from t1
            //Console.WriteLine("dY/dt[{0:0.0}] = {1:0.00000}", newState3.TimeSliceNdx * newState3.TimeStep, k4[1] / newState3.TimeStep);
            Console.WriteLine("{0:0.0},{1:0.00000}", newState3.TimeSliceNdx * newState3.TimeStep, k4[1] / newState3.TimeStep);

            double[] finalK = new double[k4.Length];
            for (int i = 0; i < finalK.Length; i++)
            {
                finalK[i] = 2.0 * (k1[i] + k2[i] + k2[i] + k3[i] + k3[i] + k4[i]) / 6.0;
            }

            StateBase<T> finalState = state.Copy();
            finalState.TimeSliceNdx++;
            ApplyDeltasToState(finalState, finalK);

            return finalState;
        }

        private static void ApplyDeltasToState(StateBase<T> state, double[] deltas)
        {
            for (int i = 0; i < state.StockGetters.Length; i++)
            {
                double stockLevel = state.StockGetters[i](state);
                state.StockSetters[i](state, stockLevel + deltas[i]);
            }
        }

        private static double[] GetDeltaForEachStock(StateBase<T> state)
        {
            double[] retval = new double[state.StockGetters.Length];

            // For each stock.
            if (Debug)
                Console.WriteLine($"TIMESLICE {state.TimeSliceNdx} = {state.CurrentTime}");
            for (int i = 0; i < state.StockGetters.Length; i++)
            {
                if (Debug)
                    Console.WriteLine("\t{0}\r\n\t\t  {1:F2}", state.StockNames()[i], state.StockGetters[i](state));
                double increase = 0;
                double decrease = 0;
                // accumulate inflows
                for (int j = 0; j < state.StockInflows[i].Length; j++)
                {
                    int whichFlow = state.StockInflows[i][j];
                    if (Debug)
                        Console.WriteLine("\t\t+ {0:F2} ({1}).", state.Flows[whichFlow](state), state.FlowNames()[whichFlow]);
                    increase += state.Flows[whichFlow](state);
                }
                increase *= state.TimeStep;


                // accumulate outflows
                for (int j = 0; j < state.StockOutflows[i].Length; j++)
                {
                    int whichFlow = state.StockOutflows[i][j];
                    if (Debug)
                        Console.WriteLine("\t\t- {0:F2} ({1}).", state.Flows[whichFlow](state), state.FlowNames()[whichFlow]);
                    decrease += state.Flows[whichFlow](state);
                }
                decrease *= state.TimeStep;

                double current = state.StockGetters[i](state);
                if (state.ManageStocksToStayPositive)
                {
                    if (current + increase - decrease < 0.0)
                    {
                        decrease = current + increase;
                    }
                }
                retval[i] = increase - decrease;
                if (current + retval[i] < 0)
                {
                    Console.WriteLine($"Stock {state.StockNames()[i]} goes negative to {current + retval[i]} in step {i}.");
                }
            }

            return retval;
        }

    }

    public abstract class StateBase
    {
        public abstract TimeSpan NominalPeriod
        {
            get;
        }
        public abstract TimeSpan ActivePeriod
        {
            get;
        }
        protected abstract void Initialize();

        protected double PeriodAdjust(double val)
        {
            return val * NominalPeriod.TotalSeconds / ActivePeriod.TotalSeconds;
        }

        protected double FractionAdjust(double val)
        {
            return Math.Max(0.0, Math.Min(1.0, val * NominalPeriod.TotalSeconds / ActivePeriod.TotalSeconds));
        }

        #region Delay Functions (3.5.3)

        public interface IFunction
        {
            double Process(double stimulus);
        }

        public class Delay : IFunction
        {
            private readonly Queue<double> _queue = new Queue<double>();
            private readonly string _myString;
            private readonly bool _hasInitialValue;
            private readonly int _nBins;

            public Delay(double dt, double delay, double initVal = Double.NegativeInfinity)
            {
                _myString = String.Format("Delay({0},{1},{2});", delay, dt, initVal);
                _hasInitialValue = !Double.IsNegativeInfinity(initVal);
                _nBins = (int)(delay / dt);
                if (!_hasInitialValue)
                {
                    for (int i = 0; i < _nBins; i++)
                        _queue.Enqueue(initVal);
                }
            }

            public double Process(double stimulus)
            {
                if (!_hasInitialValue)
                {
                    for (int i = 0; i < _nBins - 1; i++)
                        _queue.Enqueue(stimulus);
                }
                _queue.Enqueue(stimulus);
                return _queue.Dequeue();
            }

            public override string ToString()
            {
                return _myString;
            }
        }

        public class Delay1 : IFunction
        {
            private double _hold;
            private readonly double _delay;
            private readonly double _dt;
            private bool _hasInitialValue;
            private readonly string _myString;

            public Delay1(double dt, double delay, double initVal = Double.NegativeInfinity)
            {
                _myString = String.Format("Delay1({0},{1},{2});", delay, dt, initVal);
                _delay = delay;
                _dt = dt;
                _hasInitialValue = !Double.IsNegativeInfinity(initVal);
                _hold = _hasInitialValue ? initVal * delay : 0;
            }

            public double Process(double stimulus)
            {
                if (!_hasInitialValue)
                {
                    _hold = stimulus * _delay;
                    _hasInitialValue = true;
                }

                double retval = _hold / _delay;
                _hold -= (retval * _dt);
                _hold += (stimulus * _dt);
                return retval;
            }

            public override string ToString()
            {
                return _myString;
            }
        }

        public class Delay3 : IFunction
        {
            private readonly double _delay;
            private double _hold1;
            private double _hold2;
            private double _hold3;
            private readonly double _dt;
            private bool _hasInitialValue;
            private readonly string _myString;

            public Delay3(double dt, double delay, double initVal = Double.NegativeInfinity)
            {
                _myString = String.Format("Delay3({0},{1},{2});", delay, dt, initVal);
                _dt = dt;
                _delay = delay;
                _hasInitialValue = !Double.IsNegativeInfinity(initVal);
                _hold1 = _hold2 = _hold3 = _hasInitialValue ? (initVal * (_delay / 3)) : 0;
            }

            public double Process(double stimulus)
            {
                if (!_hasInitialValue)
                {
                    _hold1 = _hold2 = _hold3 = stimulus * (_delay / 3);
                    _hasInitialValue = true;
                }
                double from3 = (_hold3 / (_delay / 3)) * _dt;
                _hold3 -= from3;

                double from2 = (_hold2 / (_delay / 3)) * _dt;
                _hold2 -= from2;
                _hold3 += from2;

                double from1 = (_hold1 / (_delay / 3)) * _dt;
                _hold1 += stimulus * _dt;
                _hold1 -= from1;
                _hold2 += from1;

                return from3 / _dt;
            }

            public override string ToString()
            {
                return _myString;
            }
        }

        public class DelayN : IFunction
        {
            private readonly double _delay;
            private readonly double _dt;
            private readonly int _nStages;
            private double[] _hold;
            private bool _hasInitialValue;
            private readonly string _myString;

            public DelayN(double dt, double delay, int nStages, double initVal = Double.NegativeInfinity)
            {
                _myString = String.Format("DelayN({0},{1},{2},{3});", delay, dt, nStages, initVal);
                _dt = dt;
                _delay = delay;
                _nStages = nStages;
                _hasInitialValue = !Double.IsNegativeInfinity(initVal);
                if (_hasInitialValue)
                {
                    _hold = Enumerable.Repeat(initVal * (_delay / _nStages), _nStages).ToArray();
                }
            }

            public double Process(double stimulus)
            {
                if (!_hasInitialValue)
                {
                    _hold = Enumerable.Repeat(stimulus * (_delay / _nStages), _nStages).ToArray();
                    _hasInitialValue = true;
                }

                double[] xfer = new double[_nStages + 1];
                xfer[0] = stimulus * _dt;
                for (int i = 1; i <= _nStages; i++)
                {
                    xfer[i] = (_hold[i - 1] / (_delay / _nStages)) * _dt;
                }

                for (int i = 0; i <= _nStages; i++)
                {
                    if (i > 0)
                        _hold[i - 1] -= xfer[i];
                    if (i < _nStages)
                        _hold[i] += xfer[i];
                }

                //Console.WriteLine("{0:F2}->[{1:F2}]-{2:F2}->[{3:F2}]-{4:F2}->[{5:F2}]-{6:F2}->",
                //    xfer[0], m_hold[0], xfer[1], m_hold[1], xfer[2], m_hold[2], xfer[3]);

                return xfer[_nStages] / _dt;

            }

            public override string ToString()
            {
                return _myString;
            }

        }

        public class Smooth1 : IFunction
        {
            private double _hold;
            private readonly double _averagingTime;
            private bool _hasInitialValue;
            private readonly string _myString;

            public Smooth1(double averagingTime, double initVal = Double.NegativeInfinity)
            {
                _myString = String.Format("Smooth1({0},{1});", averagingTime, initVal);
                _averagingTime = averagingTime;
                _hasInitialValue = !Double.IsNegativeInfinity(initVal);
                _hold = _hasInitialValue ? initVal * averagingTime : 0;
            }

            public double Process(double stimulus)
            {
                if (!_hasInitialValue)
                {
                    _hold = stimulus * _averagingTime;
                    _hasInitialValue = true;
                }

                double retval = _hold / _averagingTime;
                _hold -= retval;
                _hold += stimulus;
                return retval;
            }

            public override string ToString()
            {
                return _myString;
            }
        }

        public class Smooth3 : IFunction
        {
            private readonly double _averagingTime;
            private double _hold1;
            private double _hold2;
            private double _hold3;
            private bool _hasInitialValue;
            private readonly string _myString;

            public Smooth3(double averagingTime, double initVal = Double.NegativeInfinity)
            {
                _myString = String.Format("Smooth3({0},{1});", averagingTime, initVal);
                _averagingTime = averagingTime;
                _hasInitialValue = !Double.IsNegativeInfinity(initVal);
                _hold1 = _hold2 = _hold3 = _hasInitialValue ? (initVal * (_averagingTime / 3)) : 0;
            }

            public double Process(double stimulus)
            {
                if (!_hasInitialValue)
                {
                    _hold1 = _hold2 = _hold3 = stimulus * (_averagingTime / 3);
                    _hasInitialValue = true;
                }
                double from3 = (_hold3 / (_averagingTime / 3));
                _hold3 -= from3;

                double from2 = (_hold2 / (_averagingTime / 3));
                _hold2 -= from2;
                _hold3 += from2;

                double from1 = (_hold1 / (_averagingTime / 3));
                _hold1 += stimulus;
                _hold1 -= from1;
                _hold2 += from1;

                return from3;
            }

            public override string ToString()
            {
                return _myString;
            }
        }

        public class SmoothN : IFunction
        {
            private readonly double _averagingTime;
            private readonly int _nStages;
            private double[] _hold;
            private bool _hasInitialValue;
            private readonly string _myString;

            public SmoothN(double averagingTime, int nStages, double initVal = Double.NegativeInfinity)
            {
                _myString = String.Format("SmoothN({0},{1},{2});", averagingTime, nStages, initVal);
                _averagingTime = averagingTime;
                _nStages = nStages;
                _hasInitialValue = !Double.IsNegativeInfinity(initVal);
                if (_hasInitialValue)
                {
                    _hold = Enumerable.Repeat(initVal * (_averagingTime / _nStages), _nStages).ToArray();
                }
            }

            public double Process(double stimulus)
            {
                if (!_hasInitialValue)
                {
                    _hold = Enumerable.Repeat(stimulus * (_averagingTime / _nStages), _nStages).ToArray();
                    _hasInitialValue = true;
                }

                double[] xfer = new double[_nStages + 1];
                xfer[0] = stimulus;
                for (int i = 1; i <= _nStages; i++)
                {
                    xfer[i] = (_hold[i - 1] / (_averagingTime / _nStages));
                }

                for (int i = 0; i <= _nStages; i++)
                {
                    if (i > 0)
                        _hold[i - 1] -= xfer[i];
                    if (i < _nStages)
                        _hold[i] += xfer[i];
                }

                //Console.WriteLine("{0:F2}->[{1:F2}]-{2:F2}->[{3:F2}]-{4:F2}->[{5:F2}]-{6:F2}->",
                //    xfer[0], m_hold[0], xfer[1], m_hold[1], xfer[2], m_hold[2], xfer[3]);

                return xfer[_nStages];

            }

            public override string ToString()
            {
                return _myString;
            }

        }

        public class Trend : IFunction
        {
            private double _averageInput;
            private readonly double _averagingTime;
            private readonly double _dt;
            private readonly string _myString;

            public Trend(double dt, double averagingTime, double initVal = 0)
            {
                _myString = String.Format("Trend({0}, {1}, {2});", averagingTime, dt, initVal);
                _averagingTime = averagingTime;
                _dt = dt;
                _averageInput = initVal;
            }

            public double Process(double input)
            {
                double changeInAverage = (input - _averageInput) / _averagingTime;
                double trend = (input - _averageInput) / (_averageInput * _averagingTime * _dt);
                //Console.WriteLine("{0:F2}->[{1:F2}] = {2:F2}, Trend = {3}", input, m_averageInput, changeInAverage, trend);
                _averageInput += _dt * changeInAverage;

                return trend;
            }

            public double AverageInput => _averageInput;

            public override string ToString()
            {
                return _myString;
            }
        }

        public class Forecast : IFunction
        {
            private readonly Trend _trend;
            private readonly double _horizon;
            private readonly string _myString;

            public Forecast(double dt, double averagingTime, double horizon, double initVal = 0)
            {
                _myString = string.Format("Forecast({0}, {1}, {2}, {3});", averagingTime, dt, horizon, initVal);
                _trend = new Trend(averagingTime, dt, initVal);
                _horizon = horizon;
            }

            public double Process(double input)
            {
                double trend = _trend.Process(input);
                return _trend.AverageInput + (trend * _horizon);
            }

            public override string ToString()
            {
                return _myString;
            }
        }

        #endregion
    }
}
