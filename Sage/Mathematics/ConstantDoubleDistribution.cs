/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using System;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// A ConstantDoubleDistribution serves a constant value.
    /// </summary>
    public class ConstantDoubleDistribution : IDoubleDistribution
    {
        private double _value;

        /// <summary>
        /// Creates a ConstantDoubleDistribution with a specific value;
        /// </summary>
        /// <param name="val">The (double) value that this distribution always serves up.</param>
        public ConstantDoubleDistribution(double val)
        {
            _value = val;
        }

        /// <summary>
        /// Creates a ConstantDoubleDistribution with a specific value;
        /// </summary>
        /// <param name="model">the model of which this distribution is a part.</param>
        /// <param name="name">The name that this distribution goes by.</param>
        /// <param name="guid">The Guid of this distribution.</param>
        /// <param name="val">The (double) value that this distribution always serves up.</param>
        public ConstantDoubleDistribution(IModel model, string name, Guid guid, double val)
        {
            _value = val;
        }

        #region IDoubleDistribution Members

        /// <summary>
        /// Returns the next double in the distribution.
        /// </summary>
        /// <returns>The next double in the distribution.</returns>
        public double GetNext()
        {
            return _value;
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
            return _value;
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
            // no effect.
        }

        #endregion

        #region Initialization
        /// <summary>
        /// Use this for initialization of the form new ConstantDoubleDistribution().Initialize( ... );
        /// </summary>
        public ConstantDoubleDistribution()
        {
        }

        /// <summary>
        /// Initializes this <see cref="T:Highpoint.Sage.Mathematics.ConstantDoubleDistribution"/> in the context of the specified model. Requires execution against the Sage intialization protocol. Guids specified are those of other objects in the model which this object must interact during initialization.
        /// </summary>
        /// <param name="model">The model that owns this ConstantDoubleDistribution and in whose context the initialization is being performed.</param>
        /// <param name="name">The name of this ConstantDoubleDistribution.</param>
        /// <param name="description">The description of this ConstantDoubleDistribution.</param>
        /// <param name="guid">The GUID of this ConstantDoubleDistribution.</param>
        /// <param name="val">The value that this ConstantDoubleDistribution always returns.</param>
        [Initializer(InitializationType.PreRun)]
        public void Initialize(IModel model, string name, string description, Guid guid,
            [InitializerArg(0, "Value", RefType.Owned, typeof(double), "The constant value for this distribution.")]
            double val)
        {
            InitializeIdentity(model, name, description, guid);
            IMOHelper.RegisterWithModel(this);

            model.GetService<InitializationManager>().AddInitializationTask(_Initialize, val);
        }


        /// <summary>
        /// Used by the <see cref="T:Highpoint.Sage.SimCore.InitializationManager"/> in the sequenced execution of an initialization protocol.
        /// </summary>
        /// <param name="model">The model into which this object is to be initialized.</param>
        /// <param name="p">The parameters that will be used to initialize this object.</param>
        public void _Initialize(IModel model, object[] p)
        {
            _value = (double)p[0];
        }

        /// <summary>
        /// Performs the part of object initialization that pertains to the fields associated with this object's being an implementer of IModelObject.
        /// </summary>
        /// <param name="model">The model that owns this ConstantDoubleDistribution and in whose context the initialization is being performed.</param>
        /// <param name="name">The name of this ConstantDoubleDistribution.</param>
        /// <param name="description">The description of this ConstantDoubleDistribution.</param>
        /// <param name="guid">The GUID of this ConstantDoubleDistribution.</param>
        public void InitializeIdentity(IModel model, string name, string description, Guid guid)
        {
            IMOHelper.Initialize(ref _model, model, ref _name, name, ref _description, description, ref _guid, guid);
        }
        #endregion

        #region IModelObject Members
        private string _name;
        /// <summary>
        /// The user-friendly name for this Constant Double Distribution. Typically not required to be unique.
        /// </summary>
        /// <value></value>
        public string Name => _name;
        private string _description = "A Constant Double Distribution";
        /// <summary>
        /// A description of this Constant Double Distribution.
        /// </summary>
        public string Description => _description ?? _name;

        private Guid _guid = Guid.Empty;
        /// <summary>
        /// The Guid for this Constant Double Distribution. Required to be unique.
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