using Highpoint.Sage.Mathematics;
using Highpoint.Sage.SystemDynamics.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.Linq;

// ReSharper disable ExpressionIsAlwaysNull
namespace Highpoint.Sage.SystemDynamics
{

    [TestClass]
    public class BoilingPointTester
    {
        public BoilingPointTester()
        {
            Init();
        }

        [TestInitialize]
        public void Init()
        {
        }

        [TestMethod]
        public void TestLynxHare()
        {

            string[] args = new string[] { };
            XElement parameters = null;
            string outputFileName = null;

            RunProgram<LynxHare3>.Run(
                args,
                Integrator.Euler,
                parameters,
                outputFileName,
                "Timeslice,Lynx,Hares",
                (TextWriter tw, LynxHare3 s) =>
                    tw.WriteLine("{2:0.000}, {0:0.0000000}, {1:0.000000}", s.Lynx, s.Hares, s.TimeSliceNdx * s.TimeStep));

            RunProgram<LynxHare3>.Run(
                args,
                Integrator.RK4,
                parameters,
                outputFileName,
                "Timeslice,Lynx,Hares",
                (TextWriter tw, LynxHare3 s) =>
                    tw.WriteLine("{2:0.000}, {0:0.0000000}, {1:0.000000}", s.Lynx, s.Hares, s.TimeSliceNdx * s.TimeStep));

        }

        public partial class LynxHare3
        {
            public override TimeSpan NominalPeriod { get; } = TimeSpan.FromDays(1.0);
            public override TimeSpan ActivePeriod { get; } = TimeSpan.FromDays(1.0);
            protected override void Initialize()
            {
            }
        }


        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public partial class LynxHare3 : StateBase<LynxHare3>
        {

            // Constant Auxiliaries
            private readonly double _hare_birth_fraction = 1.25;
            private readonly double _area = 1E3;
            private readonly double _lynx_birth_fraction = 0.25;
            private readonly double _size_of_1_time_lynx_harvest = 1;
            private readonly static IDoubleInterpolator lynx_death_fraction_idi = new LinearDoubleInterpolator();
            private readonly static IDoubleInterpolator hares_killed_per_lynx_idi = new LinearDoubleInterpolator();

            // ID Numbers for flows.
            private readonly static int hare_births = 0;
            private readonly static int hare_deaths = 1;
            private readonly static int lynx_births = 2;
            private readonly static int lynx_deaths = 3;
            private readonly static int one_time_lynx_harvest = 4;

            static LynxHare3()
            {   // Class-global set up.
                lynx_death_fraction_idi.SetData(new double[] { 0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 }, new double[] { 0.94, 0.66, 0.4, 0.35, 0.3, 0.25, 0.2, 0.15, 0.1, 0.07, 0.05 });
                hares_killed_per_lynx_idi.SetData(new double[] { 0, 50, 100, 150, 200, 250, 300, 350, 400, 450, 500 }, new double[] { 0, 50, 100, 150, 200, 250, 300, 350, 400, 450, 500 });
            }

            // Stock Names
            public override string[] StockNames()
                =>
                    new[]
                    {
                    "Hares",
                    "Lynx"
                    };

            // Flow Names
            public override string[] FlowNames()
                =>
                    new[]
                    {
                    "hare_births",
                    "hare_deaths",
                    "lynx_births",
                    "lynx_deaths",
                    "one_time_lynx_harvest"
                    };

            public LynxHare3() : this(false) { }

            public LynxHare3(bool fromCopy = false)
            {
                Start = 0;
                Finish = 60;
                TimeStep = 0.125;

                _hares = 5E4;
                _lynx = 1250;

                StockSetters = new Action<StateBase<LynxHare3>, double>[]
                {
                (state, d) => ((LynxHare3)state).Hares = d,
                (state, d) => ((LynxHare3)state).Lynx = d,
                };

                StockGetters = new Func<StateBase<LynxHare3>, double>[]
                {
                state => ((LynxHare3)state).Hares,
                state => ((LynxHare3)state).Lynx,
                };

                Flows = new List<Func<StateBase<LynxHare3>, double>>
            {
                /* hare_births */ state => ((LynxHare3)state).Hares * ((LynxHare3)state)._hare_birth_fraction,
                /* hare_deaths */ state => ((LynxHare3)state).Lynx * ((LynxHare3)state).hares_killed_per_lynx,
                /* lynx_births */ state => ((LynxHare3)state).Lynx * ((LynxHare3)state)._lynx_birth_fraction,
                /* lynx_deaths */ state => ((LynxHare3)state).Lynx * ((LynxHare3)state).lynx_death_fraction,
                /* one_time_lynx_harvest */ state => Pulse ( ((LynxHare3)state)._size_of_1_time_lynx_harvest , 4 , 1e3 )
            };

                StockInflows = new List<int[]> { new int[] { hare_births }, new int[] { lynx_births } };
                StockOutflows = new List<int[]> { new int[] { hare_deaths }, new int[] { lynx_deaths, one_time_lynx_harvest } };
            }

            public override StateBase<LynxHare3> Copy()
            {
                LynxHare3 retval = new LynxHare3(true);
                retval._hares = _hares;
                retval._lynx = _lynx;
                retval.TimeSliceNdx = TimeSliceNdx;
                return retval;
            }

            public override void Configure(XElement parameters = null)
            {

            }

            // Non-constant Auxiliaries
            public double hare_density => Hares / _area;
            public double lynx_death_fraction => lynx_death_fraction_idi.GetYValue(hare_density);
            public double hares_killed_per_lynx => hares_killed_per_lynx_idi.GetYValue(hare_density);

            // These predicates are applied to all values set directly into stocks.
            private List<Predicate<double>> Tests = null; // new List<Predicate<double>>() {double.IsNaN, d => d < 0.0};

            public double Hares
            {
                get
                {
                    return _hares;
                }
                set
                {
                    Tests?.ForEach(n => { if (n(value)) Debugger.Break(); });
                    _hares = value;
                }
            }
            public double Lynx
            {
                get
                {
                    return _lynx;
                }
                set
                {
                    Tests?.ForEach(n => { if (n(value)) Debugger.Break(); });
                    _lynx = value;
                }
            }


            // Stocks
            private double _hares;
            private double _lynx;

            //////////////////////////////////////////////////////////////
            // MACRO IMPLEMENTATIONS
            //////////////////////////////////////////////////////////////
            //////////////////////////////////////////////////////////////

        }
    }
}