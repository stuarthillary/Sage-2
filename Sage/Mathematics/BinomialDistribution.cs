/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Randoms;
using Highpoint.Sage.SimCore;
using System;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// A Binomial distribution gives the discrete probability distribution P_p(n|N) of obtaining
    /// exactly n successes out of N Bernoulli trials (where the result of each Bernoulli trial
    /// is true with probability p and false with probability q==1-p).
    /// </summary>
    public class BinomialDistribution : IDoubleDistribution
    {

        #region Private Fields

        private IRandomChannel _random;
        private ICDF _bcdf;

        #endregion

        /// <summary>
        /// Create a binomial distribution.
        /// </summary>
        /// <param name="probability">The probability of success in any one Bernoulli trial.</param>
        /// <param name="numberOfOpps">The number of Bernoulli trials in the experiment.</param>
        public BinomialDistribution(double probability, int numberOfOpps)
            : this(null, "", Guid.Empty, probability, numberOfOpps)
        {
        }

        /// <summary>
        /// Create a binomial distribution.
        /// </summary>
        /// <param name="model">The model that owns this BinomialDistribution.</param>
        /// <param name="name">The name of this BinomialDistribution.</param>
        /// <param name="guid">The GUID of this BinomialDistribution.</param>
        /// <param name="probability">The probability of success in any one Bernoulli trial.</param>
        /// <param name="numberOfOpps">The number of Bernoulli trials in the experiment.</param>
        public BinomialDistribution(IModel model, string name, Guid guid, double probability, int numberOfOpps)
        {
            _model = model;
            _name = name;
            _guid = guid;
            _random = (Model == null ? GlobalRandomServer.Instance : Model.RandomServer).GetRandomChannel();
            _bcdf = new BinomialCDF(probability, numberOfOpps);
            if (Model != null)
            {
                Model.ModelObjects.Remove(guid);
                Model.ModelObjects.Add(guid, this);
            }
        }

        #region IDistribution Members

        /// <summary>
        /// Returns a value representing the number of successes experienced in a new experiment.
        /// </summary>
        /// <returns>The number of successes experienced in a new experiment. Returned as an integral value.</returns>
        public double GetNext()
        {
            if (_random == null)
                _random = _model.RandomServer.GetRandomChannel(); // If _Initialize() was called, this is necessary.
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            double x = _constrained ? (_low == _high ? _low : _random.NextDouble(_low, _high)) : _random.NextDouble();
            return (int)Math.Round(_bcdf.GetVariate(x), 0);
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
            return _bcdf.GetVariate(probability);
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
        /// Use this for initialization of the form 'new BinomialDistribution().Initialize( ... );'
        /// Note that this mechanism relies on the whole model performing initialization.
        /// </summary>
        public BinomialDistribution()
        {
        }

        /// <summary>
        /// Initializes this <see cref="T:Highpoint.Sage.Mathematics.BinomialDistribution"/> in the context of the specified model. Requires execution against the Sage intialization protocol. Guids specified are those of other objects in the model which this object must interact during initialization.
        /// </summary>
        /// <param name="model">The model that owns this BinomialDistribution and in whose context the initialization is being performed.</param>
        /// <param name="name">The name of this BinomialDistribution.</param>
        /// <param name="description">The description of this BinomialDistribution.</param>
        /// <param name="guid">The GUID of this BinomialDistribution.</param>
        /// <param name="probability">The probability of any one opportunity embodied by this BinomialDistribution.</param>
        /// <param name="numberOfOpps">The number of opportunities in a trial.</param>
        [Initializer(InitializationType.PreRun)]
        public void Initialize(IModel model, string name, string description, Guid guid,
            [InitializerArg(0, "Probability", RefType.Owned, typeof(double), "The probability of success in any one opportunity.")]
            double probability,
            [InitializerArg(1, "Number Of Opportunities", RefType.Owned, typeof(int), "The number of opportunities in any given trial.")]
            int numberOfOpps)
        {
            InitializeIdentity(model, name, description, guid);
            IMOHelper.RegisterWithModel(this);

            model.GetService<InitializationManager>().AddInitializationTask(_Initialize, probability, numberOfOpps);
        }

        /// <summary>
        /// Used by the <see cref="T:Highpoint.Sage.SimCore.InitializationManager"/> in the sequenced execution of an initialization protocol.
        /// </summary>
        /// <param name="model">The model into which this object is to be initialized.</param>
        /// <param name="p">The parameters that will be used to initialize this object.</param>
        public void _Initialize(IModel model, object[] p)
        {
            _random = null; // Allows the random channel to be obtained at run time, after model has properly initialized it.
            double probability = (double)p[0];
            int numberOfOpps = (int)p[1];
            _bcdf = new BinomialCDF(probability, numberOfOpps);
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
        /// The user-friendly name for this object. Typically not required to be unique.
        /// </summary>
        /// <value>The user-friendly name for this object.</value>
        public string Name => _name;
        private string _description = "A Binomial Distribution";
        /// <summary>
        /// A description of this Binomial Distribution.
        /// </summary>
        public string Description => _description ?? _name;

        private Guid _guid = Guid.Empty;
        /// <summary>
        /// The Guid for this Binomial Distribution. Typically required to be unique.
        /// </summary>
        /// <value>The Guid for this Binomial Distribution.</value>
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