/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Randoms;
using Highpoint.Sage.SimCore;
using System;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// An Empirical distribution is a distribution that is formed from a Probability Density Function (PDF) that
    /// is provided by an external entity. The PDF is provided as a pair of x-value and y-value arrays. Like-indexed
    /// elements in these arrays are assumed to correspond to each other to form an (x,y) value that is a point on
    /// the PDF being described. Additionally, an Interpolator may be specified to smooth the otherwise piecewise
    /// linear PDF "curve."
    /// </summary>
    public class UniversalDistribution : IDoubleDistribution
    {

        #region Private Fields
        private ICDF _cdf;
        private IRandomChannel _random;
        #endregion

        /// <summary>
        /// A universal distribution relies on the caller to have implemented an arbitrary Cumulative Density Function.
        /// </summary>
        /// <param name="cumulativeDensityFunction">An implementer of ICDF that this distribution will use to ascertain values.</param>
        public UniversalDistribution(ICDF cumulativeDensityFunction)
            : this(null, "", Guid.Empty, cumulativeDensityFunction) { }

        /// <summary>
        /// A universal distribution relies on the caller to have implemented an arbitrary Cumulative Density Function.
        /// </summary>
        /// <param name="model">The model in which this distribution participates.</param>
        /// <param name="name">The name assigned to this distribution.</param>
        /// <param name="guid">The guid that identifies this distribution.</param>
        /// <param name="cumulativeDensityFunction">An implementer of ICDF that this distribution will use to ascertain values.</param>
        public UniversalDistribution(IModel model, string name, Guid guid, ICDF cumulativeDensityFunction)
        {
            InitializeIdentity(model, name, null, guid);
            _random = (Model == null ? GlobalRandomServer.Instance : Model.RandomServer).GetRandomChannel();
            _cdf = cumulativeDensityFunction;
            IMOHelper.RegisterWithModel(this);
        }

        #region IDoubleDistribution Members
        /// <summary>
        /// Returns the next double in the distribution.
        /// </summary>
        /// <returns>The next double in the distribution.</returns>
        public double GetNext()
        {
            if (_random == null)
            {
                _random = _model.RandomServer.GetRandomChannel(); // If _Initialize() was called, this is necessary.
            }
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
        /// Use this for initialization of the form 'new UniversalDistribution().Initialize( ... );'
        /// Note that this mechanism relies on the whole model performing initialization.
        /// </summary>
        public UniversalDistribution()
        {
        }

        /// <summary>
        /// Initializes this <see cref="T:Highpoint.Sage.Mathematics.Highpoint.Sage.Mathematics.EmpiricalDistribution"/> in the context of the specified model. Requires execution against the Sage intialization protocol. Guids specified are those of other objects in the model which this object must interact during initialization.
        /// </summary>
        /// <param name="model">The model that owns this UniversalDistribution and in whose context the initialization is being performed.</param>
        /// <param name="name">The name of this UniversalDistribution.</param>
        /// <param name="description">The description of this UniversalDistribution.</param>
        /// <param name="guid">The GUID of this UniversalDistribution.</param>
        /// <param name="cdfGuid">The Cumulative Density Function of this UniversalDistribution.</param>
        [Initializer(InitializationType.PreRun)]
        public void Initialize(IModel model, string name, string description, Guid guid,
            [InitializerArg(0, "CumulativeDensityFunction", RefType.Owned, typeof(ICDF), "The Cumulative Density Function that will drive this Universal Distribution.")]
      Guid cdfGuid)
        {
            InitializeIdentity(model, name, description, guid);
            IMOHelper.RegisterWithModel(this);

            model.GetService<InitializationManager>().AddInitializationTask(_Initialize, cdfGuid);
        }

        /// <summary>
        /// Used by the <see cref="T:Highpoint.Sage.SimCore.InitializationManager"/> in the sequenced execution of an initialization protocol.
        /// </summary>
        /// <param name="model">The model into which this object is to be initialized.</param>
        /// <param name="p">The parameters that will be used to initialize this object.</param>
        public void _Initialize(IModel model, object[] p)
        {
            _random = null; // Allows the random channel to be obtained at run time, after model has properly initialized it.
            _cdf = (ICDF)_model.ModelObjects[p[0]];

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
        /// The user-friendly name for this Empirical Distribution. Typically not required to be unique.
        /// </summary>
        /// <value></value>
        public string Name => _name;
        private string _description = "An Empirical Distribution";
        /// <summary>
        /// A description of this Empirical Distribution.
        /// </summary>
        public string Description => _description ?? _name;

        private Guid _guid = Guid.Empty;
        /// <summary>
        /// The Guid for this Empirical Distribution. Required to be unique.
        /// </summary>
        /// <value></value>
        public Guid Guid => _guid;
        private IModel _model;
        /// <summary>
        /// The model that owns this Empirical Distribution, or from which this object gets time, etc. data.
        /// </summary>
        /// <value>The model.</value>
        public IModel Model => _model;
        #endregion
    }

}