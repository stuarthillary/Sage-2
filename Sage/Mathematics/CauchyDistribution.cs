/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Randoms;
using Highpoint.Sage.SimCore;
using System;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// The Cauchy distribution is important
    /// as an example of a pathological case. Cauchy distributions look similar
    /// to a normal distribution. However, they have much heavier tails. When
    /// studying hypothesis tests that assume normality, seeing how the tests
    /// perform on data from a Cauchy distribution is a good indicator of how
    /// sensitive the tests are to heavy-tail departures from normality.
    /// Likewise, it is a good check for robust techniques that are designed to
    /// work well under a wide variety of distributional assumptions. 
    /// </summary>
    public class CauchyDistribution : IDoubleDistribution
    {

        #region Private Fields

        private CauchyCDF _cdf;
        private IRandomChannel _random;

        #endregion

        /// <summary>
        /// Creates a Cauchy Distribution with a specified location and shape.
        /// </summary>
        /// <param name="location">The location (center) of the distribution.</param>
        /// <param name="shape">The shape (extent) of the distribution.</param>
        public CauchyDistribution(double location, double shape)
            : this(null, "", Guid.Empty, location, shape) { }

        /// <summary>
        /// Creates a Cauchy Distribution with a specified location and shape.
        /// </summary>
        /// <param name="model">The model that owns this Cauchy Distribution.</param>
        /// <param name="name">The name of this Cauchy Distribution.</param>
        /// <param name="guid">The GUID of this Cauchy Distribution.</param>
        /// <param name="location">The location (center) of the distribution.</param>
        /// <param name="shape">The shape (extent) of the distribution.</param>
        public CauchyDistribution(IModel model, string name, Guid guid, double location, double shape)
        {
            _model = model;
            _name = name;
            _guid = guid;
            _random = (Model == null ? GlobalRandomServer.Instance : Model.RandomServer).GetRandomChannel();
            _cdf = new CauchyCDF(location, shape);
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
            double x = m_constrained ? (m_low == m_high ? m_low : _random.NextDouble(m_low, m_high)) : _random.NextDouble();
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
            m_low = low;
            m_high = high;
            m_constrained = (m_low != 0.0 && m_high != 1.0);
        }

        private bool m_constrained;
        private double m_low;
        private double m_high = 1.0;

        #endregion

        #region Initialization
        //TODO: 1.) Make sure that what happens in any other ctors also happens in the Initialize method.
        //TODO: 2.) Replace all NAME? and DESCRIPTION? tags with the appropriate text.
        /// <summary>
        /// Use this for initialization of the form 'new CauchyDistribution().Initialize( ... );'
        /// Note that this mechanism relies on the whole model performing initialization.
        /// </summary>
        public CauchyDistribution()
        {
        }

        /// <summary>
        /// Initializes this <see cref="T:Highpoint.Sage.Mathematics.CauchyDistribution"/> in the context of the specified model. Requires execution against the Sage intialization protocol. Guids specified are those of other objects in the model which this object must interact during initialization.
        /// </summary>
        /// <param name="model">The model that owns this CauchyDistribution and in whose context the initialization is being performed.</param>
        /// <param name="name">The name of this CauchyDistribution.</param>
        /// <param name="description">The description of this CauchyDistribution.</param>
        /// <param name="guid">The GUID of this CauchyDistribution.</param>
        /// <param name="location">The location of this CauchyDistribution.</param>
        /// <param name="shape">The shape of this CauchyDistribution.</param>
        [Initializer(InitializationType.PreRun)]
        public void Initialize(IModel model, string name, string description, Guid guid,
            [InitializerArg(0, "Location", RefType.Owned, typeof(double), "The location (center) of the distribution.")]
            double location,
            [InitializerArg(1, "Shape", RefType.Owned, typeof(double), "The shape (extent) of the distribution.")]
            double shape)
        {
            InitializeIdentity(model, name, description, guid);
            IMOHelper.RegisterWithModel(this);

            model.GetService<InitializationManager>().AddInitializationTask(_Initialize, location, shape);
        }

        /// <summary>
        /// Used by the <see cref="T:Highpoint.Sage.SimCore.InitializationManager"/> in the sequenced execution of an initialization protocol.
        /// </summary>
        /// <param name="model">The model into which this object is to be initialized.</param>
        /// <param name="p">The parameters that will be used to initialize this object.</param>
        public void _Initialize(IModel model, object[] p)
        {
            _random = null; // Allows the random channel to be obtained at run time, after model has properly initialized it.
            double location = (double)p[0];
            double shape = (double)p[1];
            _cdf = new CauchyCDF(location, shape);
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
        /// The user-friendly name for this Cauchy Distribution. Typically not required to be unique.
        /// </summary>
        /// <value></value>
        public string Name => _name;
        private string _description = "A Cauchy Distribution";
        /// <summary>
        /// A description of this Cauchy Distribution.
        /// </summary>
        public string Description => _description ?? _name;

        private Guid _guid = Guid.Empty;
        /// <summary>
        /// The Guid for this Cauchy Distribution. Typically required to be unique.
        /// </summary>
        /// <value></value>
        public Guid Guid => _guid;
        private IModel _model;
        /// <summary>
        /// The model that owns this object, or from which this object gets time, etc. data.
        /// </summary>
        /// <value>The model.</value>
        public IModel Model => _model;
        #endregion
    }

}