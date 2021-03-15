/* This source code licensed under the GNU Affero General Public License */

using System;
using _Debug = System.Diagnostics.Debug;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Randoms;

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// Creates a Uniform Distribution with a specified minimum and maximum. 
    /// The uniform distribution defines equal probability over a given range
    /// for a continuous distribution. For this reason, it is important as a
    /// reference distribution. <p></p>
    /// One of the most important applications of the uniform distribution
    /// is in the generation of random numbers. That is, almost all random
    /// number generators generate random numbers on the (0,1) interval. For
    /// other distributions, some transformation is applied to the uniform
    /// random numbers. 
    /// </summary>
    public class UniformDistribution : IDoubleDistribution {

        #region Private Fields

        private UniformCDF _cdf;
        private IRandomChannel _random;
        private double _minimum;
        private double _maximum;

        #endregion

        /// <summary>
        /// Creates a Uniform Distribution with a specified minimum and maximum.
        /// </summary>
        /// <param name="minimum">The minimum of the distribution.</param>
        /// <param name="maximum">The maximum of the distribution.</param>
        public UniformDistribution(double minimum, double maximum)
            : this(null, "", Guid.Empty, minimum, maximum) {
        }

        /// <summary>
        /// Creates a Uniform Distribution with a specified minimum and maximum.
        /// </summary>
        /// <param name="model">The model that owns this Uniform Distribution.</param>
        /// <param name="name">The name of this Uniform Distribution.</param>
        /// <param name="guid">The GUID of this Uniform Distribution.</param>
        /// <param name="minimum">The minimum of the distribution.</param>
        /// <param name="maximum">The maximum of the distribution.</param>
        public UniformDistribution(IModel model, string name, Guid guid, double minimum, double maximum)
        {
            _model = model;
            _name = name;
            _guid = guid;
            _minimum = minimum;
            _maximum = maximum;

            _random = (Model == null ? GlobalRandomServer.Instance : Model.RandomServer).GetRandomChannel();

            _cdf = new UniformCDF(_minimum, _maximum);
            if (Model != null)
            {
                Model.ModelObjects.Remove(guid);
                Model.ModelObjects.Add(guid, this);
            }
        }

        public double Minimum => _minimum;

        public double Maximum => _maximum;

        public void SetBounds(double minimum, double maximum) {
            _minimum = minimum;
            _maximum = maximum;
            _cdf = new UniformCDF(_minimum, _maximum);
        }

        #region IDistribution Members
        /// <summary>
        /// Serves up the next double in the distribution.
        /// </summary>
        /// <returns>The next double in the distribution.</returns>
        public double GetNext() {
            if (_random == null)
                _random = _model.RandomServer.GetRandomChannel(); // If _Initialize() was called, this is necessary.
            double x = m_constrained ? ( m_low == m_high ? m_low : _random.NextDouble(m_low, m_high) ) : _random.NextDouble();
            return _cdf.GetVariate(x);
        }

        /// <summary>
        /// Gets the Y (distribution) value with the specified X (cumulative probability) value. For example,
        /// if the caller wishes to know what Y value will, with 90% certainty, always be greater than or equal
        /// to a value returned from the distribution, he would ask for GetValueWithCumulativeProbability(0.90);
        /// <para>Note: The median value of the distribution will be GetValueWithCumulativeProbability(0.50);</para>
        /// </summary>
        /// <param name="probability">The probability.</param>
        /// <returns></returns>
        public double GetValueWithCumulativeProbability(double probability) {
            return _cdf.GetVariate(probability);
        }

        /// <summary>
        /// Sets the interval on which the CDF is queried for X. The RNG internally generates a number 'x' on (0..1), and CDF(x) is the output
        /// random number on the distribution. For the purpose of generating a schedule, running monte-carlo simulations
        /// of schedules and other related tasks, we may want to generate randoms on just a portion of that CDF, say
        /// instead of (0..1), perhaps (0..0.95), or if we want the mean value, we would use an interval of (0.5,0.5).
        /// </summary>
        /// <param name="low">The low bound (inclusive).</param>
        /// <param name="high">The high bound (exclusive, unless low and high are equal).</param>
        public void SetCDFInterval(double low, double high) {
            _Debug.Assert(low >= 0 && high <= 1 && low <= high);
            m_low = low;
            m_high = high;
            m_constrained = ( m_low != 0.0 && m_high != 1.0 );
        }

        private bool m_constrained;
        private double m_low;
        private double m_high = 1.0;

        #endregion

        #region Initialization
        /// <summary>
        /// Use this for initialization of the form 'new UniformDistribution().Initialize( ... );'
        /// Note that this mechanism relies on the whole model performing initialization.
        /// </summary>
        public UniformDistribution() { }

        /// <summary>
        /// Initializes this <see cref="T:Highpoint.Sage.Mathematics.UniformDistribution"/> in the context of the specified model. Requires execution against the Sage intialization protocol. Guids specified are those of other objects in the model which this object must interact during initialization.
        /// </summary>
        /// <param name="model">The model that owns this UniformDistribution and in whose context the initialization is being performed.</param>
        /// <param name="name">The name of this UniformDistribution.</param>
        /// <param name="description">The description of this UniformDistribution.</param>
        /// <param name="guid">The GUID of this UniformDistribution.</param>
        /// <param name="minimum">The minimum value in this UniformDistribution.</param>
        /// <param name="maximum">The maximum value in this UniformDistribution.</param>
        [Initializer(InitializationType.PreRun)]
        public void Initialize(IModel model, string name, string description, Guid guid,
            [InitializerArg(0, "Minimum", RefType.Owned, typeof(double), "The minimum of the distribution.")]
			double minimum,
            [InitializerArg(1, "Maximum", RefType.Owned, typeof(double), "The maximum of the distribution.")]
			double maximum) {
            InitializeIdentity(model, name, description, guid);
            IMOHelper.RegisterWithModel(this);

            SetBounds(minimum, maximum);

            model.GetService<InitializationManager>().AddInitializationTask(_Initialize, minimum, maximum);
        }

        /// <summary>
        /// Used by the <see cref="T:Highpoint.Sage.SimCore.InitializationManager"/> in the sequenced execution of an initialization protocol.
        /// </summary>
        /// <param name="model">The model into which this object is to be initialized.</param>
        /// <param name="p">The parameters that will be used to initialize this object.</param>
        public void _Initialize(IModel model, object[] p) {
            _random = null; // Allows the random channel to be obtained at run time, after model has properly initialized it.
            double minimum = (double)p[0];
            double maximum = (double)p[1];
            SetBounds(minimum, maximum);
        }

        /// <summary>
        /// Performs the part of object initialization that pertains to the fields associated with this object's being an implementer of IModelObject.
        /// </summary>
        /// <param name="model">The model in which this object runs.</param>
        /// <param name="name">The object's name.</param>
        /// <param name="description">The object's description.</param>
        /// <param name="guid">The object's GUID.</param>
        public void InitializeIdentity(IModel model, string name, string description, Guid guid) {
            IMOHelper.Initialize(ref _model, model, ref _name, name, ref _description, description, ref _guid, guid);
        }

        #endregion

        #region IModelObject Members
        private string _name;
        /// <summary>
        /// The user-friendly name for this Uniform Distribution. Typically not required to be unique.
        /// </summary>
        /// <value></value>
        public string Name => _name;
        private string _description = "A Uniform Distribution";
        /// <summary>
        /// A description of this Uniform Distribution.
        /// </summary>
        public string Description => _description ?? _name;

        private Guid _guid = Guid.Empty;
        /// <summary>
        /// The Guid for this Uniform Distribution. Typically required to be unique.
        /// </summary>
        /// <value></value>
        public Guid Guid => _guid;
        private IModel _model;
        /// <summary>
        /// The model that owns this Uniform Distribution, or from which this object gets time, etc. data.
        /// </summary>
        /// <value>The model.</value>
        public IModel Model => _model;
        #endregion
    }

}