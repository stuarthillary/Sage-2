/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Randoms;
using Highpoint.Sage.SimCore;
using System;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// For both theoretical and practical reasons, the normal distribution is 
    /// probably the most important distribution in statistics. For example, 
    /// <p></p>Many classical statistical tests are based on the assumption 
    /// that the data follow a normal distribution. This assumption should be
    /// tested before applying these tests. 
    /// <p></p>In modeling applications, such as linear and non-linear regression,
    /// the error term is often assumed to follow a normal distribution with fixed
    /// location and scale. 
    /// <p></p>The normal distribution is used to find significance levels in many
    /// hypothesis tests and confidence intervals. 
    /// <p></p> The mathematics for this distribution come from 
    /// http://home.online.no/~pjacklam/notes/invnorm/impl/misra/normsinv.html
    /// ...derived from http://www.netlib.org/specfun/erf
    /// </summary>
    public class NormalDistribution : IDoubleDistribution
    {

        #region Private Fields

        // Coefficients in rational approximations
        private static readonly double[] a = new double[]{-3.969683028665376e+01, 2.209460984245205e+02,
                                                             -2.759285104469687e+02, 1.383577518672690e+02,
                                                             -3.066479806614716e+01, 2.506628277459239e+00};
        private static readonly double[] b = new double[]{-5.447609879822406e+01, 1.615858368580409e+02,
                                                             -1.556989798598866e+02, 6.680131188771972e+01,
                                                             -1.328068155288572e+01};
        private static readonly double[] c = new double[]{-7.784894002430293e-03, -3.223964580411365e-01,
                                                             -2.400758277161838e+00, -2.549732539343734e+00,
                                                             4.374664141464968e+00, 2.938163982698783e+00};
        private static readonly double[] d = new double[]{7.784695709041462e-03, 3.224671290700398e-01,
                                                             2.445134137142996e+00, 3.754408661907416e+00};
        private static readonly double pLow = 0.02425;
        private static readonly double pHigh = 1.0 - pLow;

        private IRandomChannel _random;
        private double _mean;
        private double _stdev;

        #endregion

        /// <summary>
        /// Creates a new instance of the <see cref="T:NormalDistribution"/> class.
        /// </summary>
        /// <param name="mean">The mean.</param>
        /// <param name="stdev">The stdev.</param>
        public NormalDistribution(double mean, double stdev)
            : this(null, "", Guid.Empty, mean, stdev) { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:NormalDistribution"/> class.
        /// </summary>
        /// <param name="model">The model in which this object runs.</param>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        /// <param name="mean">The mean.</param>
        /// <param name="stdev">The stdev.</param>
        public NormalDistribution(IModel model, string name, Guid guid, double mean, double stdev)
        {
            _model = model;
            _name = name;
            _guid = guid;
            _random = (Model == null ? GlobalRandomServer.Instance : Model.RandomServer).GetRandomChannel();
            _mean = mean;
            _stdev = stdev;
            // ReSharper disable once InvertIf
            if (Model != null)
            {
                Model.ModelObjects.Remove(guid);
                Model.ModelObjects.Add(guid, this);
            }
        }

        #region IDistribution Members

        public double Mean
        {
            get
            {
                return _mean;
            }
            set
            {
                _mean = value;
            }
        }
        public double StandardDeviation
        {
            get
            {
                return _stdev;
            }
            set
            {
                _stdev = value;
            }
        }

        /// <summary>
        /// Returns the next double in the distribution.
        /// </summary>
        /// <returns>The next double in the distribution.</returns>
        public virtual double GetNext()
        {
            if (_random == null)
                _random = _model.RandomServer.GetRandomChannel(); // If _Initialize() was called, this is necessary.

            double x = _constrained ? (_low == _high ? _low : _random.NextDouble(_low, _high)) : _random.NextDouble();
            return GetValueWithCumulativeProbability(x);
        }

        /// <summary>
        /// Gets the Y (distribution) value with the specified X (cumulative probability) value. For example,
        /// if the caller wishes to know what Y value will, with 90% certainty, always be greater than or equal
        /// to a value returned from the distribution, he would ask for GetValueWithCumulativeProbability(0.90);
        /// <para>Note: The median value of the distribution will be GetValueWithCumulativeProbability(0.50);</para>
        /// </summary>
        /// <param name="p">The probability.</param>
        /// <returns></returns>
        public virtual double GetValueWithCumulativeProbability(double p)
        {
            double retval, q;
            if (p < pLow)
            {
                // Rational approximation for lower region:
                q = Math.Sqrt(-2 * Math.Log(p, Math.E));
                retval = (((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5]) / ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1);
            }
            else if (pHigh < p)
            {
                // Rational approximation for upper region:
                q = Math.Sqrt(-2 * Math.Log(1 - p, Math.E));
                retval = -(((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5]) / ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1);
            }
            else
            {
                // Rational approximation for central region:
                q = p - 0.5;
                double r = q * q;
                retval = (((((a[0] * r + a[1]) * r + a[2]) * r + a[3]) * r + a[4]) * r + a[5]) * q / (((((b[0] * r + b[1]) * r + b[2]) * r + b[3]) * r + b[4]) * r + 1);
            }
            //			return retval;
            //		}
            //		protected double ScaleAndOffSet(double baseValue){
            //			double retval = baseValue;
            retval *= _stdev;
            retval += _mean;
            return retval;
        }

        /// <summary>
        /// Sets the interval on which the CDF is queried for X. The RNG internally generates a number 'x' on (0..1), and CDF(x) is the output
        /// random number on the distribution. For the purpose of generating a schedule, running monte-carlo simulations
        /// of schedules and other related tasks, we may want to generate randoms on just a portion of that CDF, say
        /// instead of (0..1), perhaps (0..0.95), or if we want the mean value, we would use an interval of (0.5,0.5).
        /// </summary>
        /// <param name="low">The low bound (inclusive).</param>
        /// <param name="high">The high bound (exclusive, unless low and high are equal).</param>
        public void SetCDFInterval(double low, double high)
        {
            _Debug.Assert(low >= 0 && high <= 1 && low <= high);
            _low = low;
            _high = high;
            _constrained = (_low != 0.0 && _high != 1.0);
        }

        private bool _constrained;
        private double _low;
        private double _high = 1.0;

        #endregion

        #region Initialization
        /// <summary>
        /// Use this for initialization of the form new NormalDistribution().Initialize( ... );
        /// </summary>
        public NormalDistribution()
        {
        }

        /// <summary>
        /// Initializes this <see cref="T:Highpoint.Sage.Mathematics.NormalDistribution"/> in the context of the specified model. Requires execution against the Sage intialization protocol. Guids specified are those of other objects in the model which this object must interact during initialization.
        /// </summary>
        /// <param name="model">The model that owns this object and in whose context the initialization is being performed.</param>
        /// <param name="name">The name of this NormalDistribution.</param>
        /// <param name="description">The description of this NormalDistribution.</param>
        /// <param name="guid">The GUID of this NormalDistribution.</param>
        /// <param name="mean">The mean of this NormalDistribution.</param>
        /// <param name="stdev">The standard deviation of this NormalDistribution.</param>
        [Initializer(InitializationType.PreRun)]
        public void Initialize(IModel model, string name, string description, Guid guid,
            [InitializerArg(0, "Mean", RefType.Owned, typeof(double), "Mean value for this distribution.")]
            double mean,
            [InitializerArg(1, "StdDev", RefType.Owned, typeof(double), "Standard Deviation for this distribution.")]
            double stdev)
        {
            InitializeIdentity(model, name, description, guid);
            IMOHelper.RegisterWithModel(this);

            model.GetService<InitializationManager>().AddInitializationTask(_Initialize, mean, stdev);
        }


        /// <summary>
        /// Used by the <see cref="T:Highpoint.Sage.SimCore.InitializationManager"/> in the sequenced execution of an initialization protocol.
        /// </summary>
        /// <param name="model">The model into which this object is to be initialized.</param>
        /// <param name="p">The parameters that will be used to initialize this object.</param>
        public void _Initialize(IModel model, object[] p)
        {
            _random = null; // Allows the random channel to be obtained at run time, after model has properly initialized it.
            _mean = (double)p[0];
            _stdev = (double)p[1];
        }

        /// <summary>
        /// Performs the part of object initialization that pertains to the fields associated with this object's being an implementer of IModelObject.
        /// </summary>
        /// <param name="model">The model in which this object runs.</param>
        /// <param name="name">The object's name.</param>
        /// <param name="description">The object's description.</param>
        /// <param name="guid">The object's GUID.</param>
        public void InitializeIdentity(IModel model, string name, string description, Guid guid)
        {
            IMOHelper.Initialize(ref _model, model, ref _name, name, ref _description, description, ref _guid, guid);
        }
        #endregion

        #region IModelObject Members
        private string _name;
        /// <summary>
        /// The user-friendly name for this NormalDistribution. Typically not required to be unique.
        /// </summary>
        /// <value></value>
        public string Name => _name;
        private string _description = "A Normal Distribution";
        /// <summary>
        /// A description of this Normal Distribution.
        /// </summary>
        public string Description => _description ?? _name;

        private Guid _guid = Guid.Empty;
        /// <summary>
        /// The Guid for this NormalDistribution. Typically required to be unique.
        /// </summary>
        /// <value></value>
        public Guid Guid => _guid;
        private IModel _model;
        /// <summary>
        /// The model that owns this NormalDistribution, or from which this object gets time, etc. data.
        /// </summary>
        /// <value>The model.</value>
        public IModel Model => _model;
        #endregion
    }

}