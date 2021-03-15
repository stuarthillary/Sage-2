/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using System;

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// According to http://www.itl.nist.gov/div898/handbook/eda/section3/eda3669.htm, 
    /// the lognormal distribution is used extensively in reliability applications to 
    /// model failure times. The lognormal and Weibull distributions are probably the 
    /// most commonly used distributions in reliability applications.
    /// </summary>
    public class LognormalDistribution : NormalDistribution
    {

        /// <summary>
        /// Creates a new instance of the <see cref="T:LognormalDistribution"/> class.
        /// </summary>
        /// <param name="mean">The mean value of this LognormalDistribution.</param>
        /// <param name="stdev">The standard deviation of this LognormalDistribution.</param>
        public LognormalDistribution(double mean, double stdev)
            : base(null, "", Guid.Empty, mean, stdev) { }

        /// <summary>
        /// Creates a new instance of the <see cref="T:LognormalDistribution"/> class.
        /// </summary>
        /// <param name="model">The model that owns this LognormalDistribution.</param>
        /// <param name="name">The name of this LognormalDistribution.</param>
        /// <param name="guid">The GUID of this LognormalDistribution.</param>
        /// <param name="mean">The mean value of this LognormalDistribution.</param>
        /// <param name="stdev">The standard deviation of this LognormalDistribution.</param>
        public LognormalDistribution(IModel model, string name, Guid guid, double mean, double stdev)
            : base(model, name, guid, mean, stdev) { }

        #region IDistribution Members
        /// <summary>
        /// Returns the next double in the distribution.
        /// </summary>
        /// <returns>The next double in the distribution.</returns>
        public override double GetNext()
        {
            return Math.Exp(base.GetNext());
        }

        /// <summary>
        /// Gets the Y (distribution) value with the specified X (cumulative probability) value. For example,
        /// if the caller wishes to know what Y value will, with 90% certainty, always be greater than or equal
        /// to a value returned from the distribution, he would ask for GetValueWithCumulativeProbability(0.90);
        /// <para>Note: The median value of the distribution will be GetValueWithCumulativeProbability(0.50);</para>
        /// </summary>
        /// <param name="probability">The probability.</param>
        /// <returns></returns>
        public override double GetValueWithCumulativeProbability(double probability)
        {
            return Math.Exp(base.GetValueWithCumulativeProbability(probability));
        }

        #endregion

        #region Initialization
        /// <summary>
        /// Use this for initialization of the form 'new LognormalDistribution().Initialize( ... );'
        /// Note that this mechanism relies on the whole model performing initialization.
        /// </summary>
        public LognormalDistribution()
        {
        }

        /// <summary>
        /// Initializes this <see cref="T:Highpoint.Sage.Mathematics.NormalDistribution"/> in the context of the specified model. Requires execution against the Sage intialization protocol. Guids specified are those of other objects in the model which this object must interact during initialization.
        /// </summary>
        /// <param name="model">The model that owns this object and in whose context the initialization is being performed.</param>
        /// <param name="name">The name of this LognormalDistribution.</param>
        /// <param name="description">The description of this LognormalDistribution.</param>
        /// <param name="guid">The GUID of this LognormalDistribution.</param>
        /// <param name="mean">The mean of this LognormalDistribution.</param>
        /// <param name="stdev">The standard deviation of this LognormalDistribution.</param>
        [Initializer(InitializationType.PreRun, "_Initialize_LogNormal")]
        public new void Initialize(IModel model, string name, string description, Guid guid,
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
        public void _Initialize_LogNormal(IModel model, object[] p)
        {
            _Initialize(model, p);
        }

        #endregion
    }

}