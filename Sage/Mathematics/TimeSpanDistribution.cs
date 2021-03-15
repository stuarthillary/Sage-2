/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using System;
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable InvertIf

// ReSharper disable UnusedParameter.Local
// ReSharper disable ConvertToAutoPropertyWhenPossible
// ReSharper disable UnusedMember.Local
// ReSharper disable CompareOfFloatsByEqualityOperator // TODO: This needs to be addressed.

namespace Highpoint.Sage.Mathematics
{
    /// <summary>
    /// A distribution that uses an underlying <see cref="Highpoint.Sage.Mathematics.IDoubleDistribution"/> to
    /// generate a distribution of TimeSpans.
    /// </summary>
    public class TimeSpanDistribution : ITimeSpanDistribution
    {

        /// <summary>
        /// The units of a TimeSpanDistribution. The values are according to the underlying IDoubleDistribution, and 
        /// the units are according to the selected element of this enumeration.
        /// </summary>
        public enum Units
        {
            /// <summary>
            /// Seconds 
            /// </summary>
            Seconds,
            /// <summary>
            /// Minutes
            /// </summary>
            [DefaultValue]
            Minutes,
            /// <summary>
            /// Hours
            /// </summary>
            Hours,
            /// <summary>
            /// Days
            /// </summary>
            Days
        };

        #region Private Fields
        private Units _units;
        private IDoubleDistribution _baseDistribution;

        #endregion

        /// <summary>
        /// Creates a new instance of the <see cref="T:TimeSpanDistribution"/> class.
        /// </summary>
        /// <param name="distribution">The underlying IDoubleDistribution that will generate the values in this TimeSpanDistribution.</param>
        /// <param name="units">The units that will be applied to the values out of the underlying IDoubleDistribution.</param>
        public TimeSpanDistribution(IDoubleDistribution distribution, Units units)
            : this(null, "", Guid.Empty, distribution, units)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="T:TimeSpanDistribution"/> class.
        /// </summary>
        /// <param name="model">The model in which this TimeSpanDistribution will exist.</param>
        /// <param name="name">The name of this TimeSpanDistribution.</param>
        /// <param name="guid">The GUID of this TimeSpanDistribution.</param>
        /// <param name="distribution">The underlying IDoubleDistribution that will generate the values in this TimeSpanDistribution.</param>
        /// <param name="units">The units that will be applied to the values out of the underlying IDoubleDistribution.</param>
        public TimeSpanDistribution(IModel model, string name, Guid guid, IDoubleDistribution distribution, Units units)
        {
            _baseDistribution = distribution;
            _units = units;
            // ReSharper disable once InvertIf
            if (Model != null)
            {
                Model.ModelObjects.Remove(Guid);
                Model.ModelObjects.Add(Guid, this);
            }
        }

        /// <summary>
        /// Gets or sets the base distribution.
        /// </summary>
        /// <value>The base distribution.</value>
        IDoubleDistribution BaseDistribution
        {
            get
            {
                return _baseDistribution;
            }
            set
            {
                _baseDistribution = value;
            }
        }

        #region Initialization
        /// <summary>
        /// Use this for initialization of the form 'new TimeSpanDistribution().Initialize( ... );'
        /// Note that this mechanism relies on the whole model performing initialization.
        /// </summary>
        public TimeSpanDistribution()
        {
        }

        /// <summary>
        /// Initializes this <see cref="T:Highpoint.Sage.Mathematics.TimeSpanDistribution"/> in the context of the specified model. Requires execution against the Sage intialization protocol. Guids specified are those of other objects in the model which this object must interact during initialization.
        /// </summary>
        /// <param name="model">The model that owns this TimeSpanDistribution and in whose context the initialization is being performed.</param>
        /// <param name="name">The name of this TimeSpanDistribution.</param>
        /// <param name="description">The description of this TimeSpanDistribution.</param>
        /// <param name="guid">The GUID of this TimeSpanDistribution.</param>
        /// <param name="distribution">The GUID of the underlying double distribution which drives this TimeSpanDistribution.</param>
        /// <param name="units">The units (minutes, seconds, etc) that are applied to the underlying double distribution in deriving this TimeSpanDistribution's timespans.</param>
        [Initializer(InitializationType.PreRun)]
        public void Initialize(IModel model, string name, string description, Guid guid,
            [InitializerArg(0, "Base distribution", RefType.Owned, typeof(IDoubleDistribution), "The double distribution that provides the profile of this TimeSpan distribution.")]
            Guid distribution,
            [InitializerArg(1, "TimeUnits", RefType.Owned, typeof(Units), "An enumeration - Seconds, Minutes, Hours, or Days.")]
            Units units)
        {
            InitializeIdentity(model, name, description, guid);
            IMOHelper.RegisterWithModel(this);

            model.GetService<InitializationManager>().AddInitializationTask(_Initialize, distribution, units.ToString());
        }

        /// <summary>
        /// Used by the <see cref="T:Highpoint.Sage.SimCore.InitializationManager"/> in the sequenced execution of an initialization protocol.
        /// </summary>
        /// <param name="model">The model into which this object is to be initialized.</param>
        /// <param name="p">The parameters that will be used to initialize this object.</param>
        public void _Initialize(IModel model, object[] p)
        {
            _baseDistribution = (IDoubleDistribution)_model.ModelObjects[p[0]];
            _units = (Units)Enum.Parse(typeof(Units), (string)p[1]);
        }

        /// <summary>
        /// Performs the part of object initialization that pertains to the fields associated with this object's being an implementer of IModelObject.
        /// </summary>
        /// <param name="model">The model to which this distribution belongs.</param>
        /// <param name="name">The name of the distribution.</param>
        /// <param name="description">The description of the distribution.</param>
        /// <param name="guid">The GUID of the distribution.</param>
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
        /// <value></value>
        public string Name => _name;

        private string _description = "A Timespan Distribution";
        /// <summary>
        /// A description of this Timespan Distribution.
        /// </summary>
        public string Description => _description ?? _name;

        private Guid _guid = Guid.Empty;
        /// <summary>
        /// The Guid for this object. Typically required to be unique.
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

        #region ITimeSpanDistribution Members

        /// <summary>
        /// Gets the next value in this distribution.
        /// </summary>
        /// <returns>The next value in this distribution.</returns>
        public TimeSpan GetNext()
        {
            double d = _baseDistribution.GetNext();
            return GetTimeSpanFor(d);
        }

        /// <summary>
        /// Gets the Y (distribution) value with the specified X (cumulative probability) value. For example,
        /// if the caller wishes to know what duration will, with 90% certainty, always be greater than or equal
        /// to a duration returned from the distribution, he would ask for GetValueWithCumulativeProbability(0.90);
        /// <para>Note: The median value of the distribution will be GetValueWithCumulativeProbability(0.50);</para>
        /// </summary>
        /// <param name="probability">The probability.</param>
        /// <returns></returns>
        public TimeSpan GetValueWithCumulativeProbability(double probability)
        {
            return GetTimeSpanFor(_baseDistribution.GetValueWithCumulativeProbability(probability));
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
            _baseDistribution.SetCDFInterval(low, high);
        }


        #endregion

        private TimeSpan GetTimeSpanFor(double randomVar)
        {
            switch (_units)
            {
                case Units.Minutes:
                    randomVar *= 60.0;
                    break;
                case Units.Hours:
                    randomVar *= 3600.0;
                    break;
                case Units.Days:
                    randomVar *= 8640.0;
                    break;
            }
            if (randomVar < 0)
                randomVar = 0;
            return TimeSpan.FromSeconds(randomVar);
        }
    }

}