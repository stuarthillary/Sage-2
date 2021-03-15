/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Randoms;
using Highpoint.Sage.SimCore;
using System;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// The Weibull distribution is used extensively in reliability applications to model failure times. 
    /// </summary>
    public class WeibullDistribution : IDoubleDistribution
    {

        #region Private Fields

        //private WeibullCDF m_cdf = null;
        private IRandomChannel _random;
        private double _location;
        private double _scale;
        private double _invGamma;

        #endregion

        /// <summary>
        /// The Weibull distribution is used extensively in reliability applications to model failure times.
        /// </summary>
        /// <param name="shape">The shape parameter. Must be &gt; 0. &lt; 1 looks like an L, ~1 looks like a '/', and &gt; 1 looks like a '/\_'</param>
        /// <param name="location">The location parameter. Where the distribution is, on the X axis.</param>
        /// <param name="scale">The scale parameter.</param>
        public WeibullDistribution(double shape, double location, double scale)
            : this(null, "", Guid.Empty, shape, location, scale) { }

        /// <summary>
        /// The Weibull distribution is used extensively in reliability applications to model failure times.
        /// </summary>
        /// <param name="model">The model to which this Weibull Distribution belongs.</param>
        /// <param name="name">The name of this Weibull Distribution.</param>
        /// <param name="guid">The GUID of this Weibull Distribution.</param>
        /// <param name="shape">The shape parameter. Must be &gt; 0. &lt; 1 looks like an L, ~1 looks like a '/', and &gt; 1 looks like a '/\_'</param>
        /// <param name="location">The location parameter. Where the distribution is, on the X axis.</param>
        /// <param name="scale">The scale parameter.</param>
        public WeibullDistribution(IModel model, string name, Guid guid, double shape, double location, double scale)
        {
            if (shape <= 0 || scale <= 0)
            {
                throw new ArgumentException("Shape and scale parameters in a Weibull Distribution must be greater than zero.");
            }
            _model = model;
            _name = name;
            _guid = guid;
            _random = (Model == null ? GlobalRandomServer.Instance : Model.RandomServer).GetRandomChannel();

            _location = location;
            _scale = scale;
            //m_cdf = new WeibullCDF(gamma,100);
            _invGamma = 1.0 / shape;
            if (Model != null)
            {
                Model.ModelObjects.Remove(guid);
                Model.ModelObjects.Add(guid, this);
            }

        }

        #region IDistribution Members
        /// <summary>
        /// Serves up the next double in the distribution.
        /// </summary>
        /// <returns>The next double in the distribution.</returns>
        public double GetNext()
        {
            if (_random == null)
                _random = _model.RandomServer.GetRandomChannel(); // If _Initialize() was called, this is necessary.
            //return (m_location +(m_scale * m_cdf.GetVariate(m_random.NextDouble())));
            double x = _constrained ? (_low == _high ? _low : _random.NextDouble(_low, _high)) : _random.NextDouble();
            return GetValueWithCumulativeProbability(x);
        }

        /// <summary>
        /// Gets the Y (distribution) value with the specified X (cumulative probability) value. For example,
        /// if the caller wishes to know what Y value will, with 90% certainty, always be greater than or equal
        /// to a value returned from the distribution, he would ask for GetValueWithCumulativeProbability(0.90);
        /// <para>Note: The median value of the distribution will be GetValueWithCumulativeProbability(0.50);</para>
        /// </summary>
        /// <param name="probability">The probability.</param>
        /// <returns></returns>
        public double GetValueWithCumulativeProbability(double probability)
        {
            double x = Math.Pow(-Math.Log(1 - probability, Math.E), _invGamma);
            return (_location + (_scale * x));
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
        /// Use this for initialization of the form 'new WeibullDistribution().Initialize( ... );'
        /// Note that this mechanism relies on the whole model performing initialization.
        /// </summary>
        public WeibullDistribution()
        {
        }

        /// <summary>
        /// Initializes this <see cref="T:Highpoint.Sage.Mathematics.WeibullDistribution"/> in the context of the specified model. Requires execution against the Sage intialization protocol. Guids specified are those of other objects in the model which this object must interact during initialization.
        /// </summary>
        /// <param name="model">The model that owns this WeibullDistribution and in whose context the initialization is being performed.</param>
        /// <param name="name">The name of this WeibullDistribution.</param>
        /// <param name="description">The description of this WeibullDistribution.</param>
        /// <param name="guid">The GUID of this WeibullDistribution.</param>
        /// <param name="shape">The shape of this WeibullDistribution.</param>
        /// <param name="location">The location of this WeibullDistribution.</param>
        /// <param name="scale">The scale of this WeibullDistribution.</param>
        [Initializer(InitializationType.PreRun)]
        public void Initialize(IModel model, string name, string description, Guid guid,
            [InitializerArg(0, "Shape", RefType.Owned, typeof(double), "The shape parameter. Must be > 0. <1 looks like an L, ~1 looks like a '/', and > 1 looks like a '/\\_'")]
            double shape,
            [InitializerArg(1, "Location", RefType.Owned, typeof(double), "The location parameter. Where the distribution is, on the X axis.")]
            double location,
            [InitializerArg(2, "Scale", RefType.Owned, typeof(double), "The scale parameter - the extent of the distribution")]
            double scale)
        {

            if (shape <= 0 || scale <= 0)
            {
                throw new ArgumentException("Shape parameter in a Weibull Distribution must be greater than zero.");
            }

            InitializeIdentity(model, name, description, guid);
            IMOHelper.RegisterWithModel(this);

            model.GetService<InitializationManager>().AddInitializationTask(_Initialize, shape, location, scale);
        }

        /// <summary>
        /// Used by the <see cref="T:Highpoint.Sage.SimCore.InitializationManager"/> in the sequenced execution of an initialization protocol.
        /// </summary>
        /// <param name="model">The model into which this object is to be initialized.</param>
        /// <param name="p">The parameters that will be used to initialize this object.</param>
        public void _Initialize(IModel model, object[] p)
        {
            _random = null; // Allows the random channel to be obtained at run time, after model has properly initialized it.
            double shape = (double)p[0];
            _location = (double)p[1];
            _scale = (double)p[2];

            //m_cdf = new WeibullCDF(gamma,100);
            _invGamma = 1.0 / shape;

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
        /// The user-friendly name for this Weibull Distribution. Typically not required to be unique.
        /// </summary>
        /// <value></value>
        public string Name => _name;
        private string _description = "A Weibull Distribution";
        /// <summary>
        /// A description of this Weibull Distribution.
        /// </summary>
        public string Description => _description ?? _name;

        private Guid _guid = Guid.Empty;
        /// <summary>
        /// The Guid for this Weibull Distribution. Typically required to be unique.
        /// </summary>
        /// <value></value>
        public Guid Guid => _guid;
        private IModel _model;
        /// <summary>
        /// The model that owns this Weibull Distribution, or from which this object gets time, etc. data.
        /// </summary>
        /// <value>The model.</value>
        public IModel Model => _model;
        #endregion
    }

}