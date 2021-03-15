/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Utility;
using System;

namespace Highpoint.Sage.Materials.Chemistry
{
    public class Vessel : IContainer, IModelObject, IResettable
    {

        #region Private Fields
        private static readonly Guid mixture_Guidmask = new Guid("4500F94B-DAE7-4399-B5E3-DC2EAD036ECD");
        private bool _autoReset;
        private readonly Mixture _mixture;
        private readonly double _capacity;
        private readonly double _initialPressure;
        private double _pressure;
        private readonly DoubleTracker _mixtureVolume;
        private readonly DoubleTracker _mixtureMass;
        #endregion

        public Vessel(IModel model, string name, string description, Guid guid, double capacity, double pressure, bool autoReset)
        {
            InitializeIdentity(model, name, description, guid);
            _mixture = new Mixture(model, name + ".Mixture", GuidOps.XOR(guid, mixture_Guidmask));
            _mixtureMass = new DoubleTracker();
            _mixtureVolume = new DoubleTracker();
            _mixture.MaterialChanged += new MaterialChangeListener(m_mixture_MaterialChanged);
            _pressure = _initialPressure = pressure;
            _capacity = capacity;
            _autoReset = autoReset;
            _model.Starting += new ModelEvent(m_model_Starting);
        }

        /// <summary>
        /// Gets a double tracker that records the initial, minimum, maximum, and final mixture mass.
        /// </summary>
        /// <value>The mixture mass.</value>
        public DoubleTracker MixtureMass
        {
            get
            {
                return _mixtureMass;
            }
        }

        /// <summary>
        /// Gets a double tracker that records the initial, minimum, maximum, and final mixture volume.
        /// </summary>
        /// <value>The mixture volume.</value>
        public DoubleTracker MixtureVolume
        {
            get
            {
                return _mixtureVolume;
            }
        }

        #region IContainer Members

        public Mixture Mixture
        {
            get
            {
                return _mixture;
            }
        }

        public double Capacity
        {
            get
            {
                return _capacity;
            }
        }

        public double Pressure
        {
            get
            {
                return _pressure;
            }
        }

        public bool AutoReset
        {
            get
            {
                return _autoReset;
            }
            set
            {
                _autoReset = value;
            }
        }

        #endregion

        #region IResettable Members

        /// <summary>
        /// Performs a reset operation on this instance.
        /// </summary>
        public void Reset()
        {
            _mixture.Clear();
            _pressure = _initialPressure;
            _mixtureVolume.Reset();
            _mixtureMass.Reset();
            _mixtureMass.Register(_mixture.Mass);
            _mixtureVolume.Register(_mixture.Volume);
        }

        #endregion

        #region Implementation of IModelObject
        private string _name = null;
        private Guid _guid = Guid.Empty;
        private IModel _model;
        private string _description = null;

        /// <summary>
        /// The IModel to which this object belongs.
        /// </summary>
        /// <value>The object's Model.</value>
        public IModel Model
        {
            [System.Diagnostics.DebuggerStepThrough]
            get
            {
                return _model;
            }
        }

        /// <summary>
        /// The name by which this object is known. Typically not required to be unique in a pan-model context.
        /// </summary>
        /// <value>The object's name.</value>
        public string Name
        {
            [System.Diagnostics.DebuggerStepThrough]
            get
            {
                return _name;
            }
        }

        /// <summary>
        /// The description for this object. Typically used for human-readable representations.
        /// </summary>
        /// <value>The object's description.</value>
        public string Description
        {
            [System.Diagnostics.DebuggerStepThrough]
            get
            {
                return _description ?? "No description for " + _name;
            }
        }

        /// <summary>
        /// The Guid for this object. Typically required to be unique in a pan-model context.
        /// </summary>
        /// <value>The object's Guid.</value>
        public Guid Guid
        {
            [System.Diagnostics.DebuggerStepThrough]
            get
            {
                return _guid;
            }
        }

        /// <summary>
        /// Initializes the fields that feed the properties of this IModelObject identity.
        /// </summary>
        /// <param name="model">The IModelObject's new model value.</param>
        /// <param name="name">The IModelObject's new name value.</param>
        /// <param name="description">The IModelObject's new description value.</param>
        /// <param name="guid">The IModelObject's new GUID value.</param>
        public void InitializeIdentity(IModel model, string name, string description, Guid guid)
        {
            IMOHelper.Initialize(ref _model, model, ref _name, name, ref _description, description, ref _guid, guid);
        }
        #endregion

        private void m_mixture_MaterialChanged(IMaterial material, MaterialChangeType type)
        {
            _mixtureMass.Register(_mixture.Mass);
            _mixtureVolume.Register(_mixture.Volume);
        }

        private void m_model_Starting(IModel theModel)
        {
            if (_autoReset)
            {
                Reset();
            }
        }
    }
}
