/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Persistence;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Utility.Mementos;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Debug = System.Diagnostics.Debug;
using K = Highpoint.Sage.Materials.Chemistry.Constants;

// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable RedundantDefaultMemberInitializer

namespace Highpoint.Sage.Materials.Chemistry
{
    /// <summary>
    /// A substance is a homogeneous mixture - one constituent. Typically, this is a single element or compound. Temperatures externally are always presented in degrees celsius.
    /// </summary>
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global (Can be inherited in user code.)
    public class Substance : IMaterial, IHasName
    {

        #region Private Fields

        private static readonly bool breakOnIsNaNTemp = Diagnostics.DiagnosticAids.Diagnostics("TemperatureIsNaNBreak");
        private static readonly ArrayList _emptyList = ArrayList.ReadOnly(new ArrayList());
        private MaterialType _type;
        private Hashtable _materialSpecs;
        private double _mass = 0.0;       // Kilograms
        private readonly WriteLock _writeLock = new WriteLock(true);
        private readonly MementoHelper _ssh;
        private IMemento _memento;
        private Mixture.MaterialChangeDistiller _eventDistiller;
        #endregion 

        internal double Temp = 0.0;       // degrees K
        /// <summary>
        /// The default mass format string. This can be changed by client code to default to lesser or greater format precision.
        /// </summary>
        public static string DefaultMassFormatString = "F2";

        /// <summary>
        /// Indicates if write operations on this equipment are permitted.
        /// </summary>
        public bool IsWritable => _writeLock.IsWritable;

        /// <summary>
        /// Performs an explicit conversion from <see cref="Substance"/> to <see cref="WriteLock"/>.
        /// </summary>
        /// <param name="substance">The substance.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator WriteLock(Substance substance)
        {
            return substance._writeLock;
        }

        /// <summary>
        /// Fired after a material has changed its mass, constituents or temperature.
        /// </summary>
        public event MaterialChangeListener MaterialChanged;


        /// <summary>
        /// Initializes a new instance of the <see cref="Substance"/> class.
        /// </summary>
        /// <param name="type">The MaterialType of the substance.</param>
        /// <param name="mass">The mass of substance to be created.</param>
        /// <param name="tempInCelsius">The temperature of the substance in celsius.</param>
        /// <param name="state">The material state of the substance.</param>
        public Substance(MaterialType type, double mass, double tempInCelsius, MaterialState state = MaterialState.Unknown)
        {
            if (mass > float.MaxValue)
                mass = float.MaxValue;
            _ssh = new MementoHelper(this, true);
            _type = type;
            _mass = mass;
            Temperature = tempInCelsius;
            State = state;
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>IMaterial.</returns>
        public IMaterial Clone()
        {
            Substance retval = (Substance)_type.CreateMass(Mass, Temperature);
            retval.State = State;
            if (_materialSpecs != null)
            {
                retval._materialSpecs = new Hashtable();
                foreach (DictionaryEntry de in _materialSpecs)
                    retval._materialSpecs.Add(de.Key, de.Value);
            }
            return retval;
        }

        /// <summary>
        /// Ascertains equality between this one and the other one.
        /// </summary>
        /// <param name="otherOne">The other one.</param>
        /// <returns><c>true</c> if this one and the other one are equal, <c>false</c> otherwise.</returns>
        public bool Equals(ISupportsMementos otherOne)
        {
            Substance otherSubstance = otherOne as Substance;
            if (otherSubstance == null)
                return false;
            if (_materialSpecs != null && otherSubstance._materialSpecs == null)
                return false;
            if (_materialSpecs == null && otherSubstance._materialSpecs != null)
                return false;
            if (_materialSpecs != null)
            {
                if (otherSubstance._materialSpecs != null && _materialSpecs.Count != otherSubstance._materialSpecs.Count)
                    return false;
                foreach (DictionaryEntry de in _materialSpecs)
                {
                    if (otherSubstance._materialSpecs != null && !otherSubstance._materialSpecs.Contains(de.Key))
                        return false;
                    if (otherSubstance._materialSpecs != null && !de.Value.Equals(otherSubstance._materialSpecs[de.Key]))
                        return false;
                }
            }
            else if (otherSubstance._materialSpecs != null)
                return false;

            return (_mass.Equals(otherSubstance._mass) && Temp.Equals(otherSubstance.Temp) && _type.Equals(otherSubstance._type));
        }

        /// <summary>
        /// The user-friendly name for this object.
        /// </summary>
        /// <value>The name.</value>
        public string Name => _type.Name;

        /// <summary>
        /// Gets the mass of the material in kilograms.
        /// </summary>
        /// <value>The mass of the material in kilograms.</value>
        public double Mass => _mass;

        /// <summary>
        /// Gets the number of moles of the substance. This requires that Molecular Weight be set in the appropriate material type.
        /// </summary>
        /// <value>The moles.</value>
        public double Moles => (_mass * 1000) / _type.MolecularWeight; // grams per 'grams-per-mole' = moles.

        /// <summary>
        /// Gets the density of the material in kilograms per liter.
        /// </summary>
        /// <value>The density.</value>
        public double Density => Mass / Volume;

        /// <summary>
        /// Gets or sets the material state of the substance.
        /// </summary>
        /// <value>The state.</value>
        public MaterialState State { get; set; } = MaterialState.Unknown;

        /// <summary>
        /// Sets the material spec of the substance. Call this only once on any given substance. 
        /// See the tech note on Material Specifications. If you have multiple specifications 
        /// to set, call SetMaterialSpecs.
        /// </summary>
        /// <param name="identity">The material specification.</param>
        /// <param name="amount">The amount. Any remaining material is presumed to have no spec.</param>
        /// <exception cref="ApplicationException">Substance had a material specification set to it more than once.</exception>
        public void SetMaterialSpec(Guid identity, double amount)
        {
            if (identity.Equals(Guid.Empty))
                return;
            if (_materialSpecs == null)
            {
                _materialSpecs = new Hashtable { { identity, amount } };
            }
            else
            {
                throw new ApplicationException("Substance had a material specification set to it more than once.");
            }
        }

        /// <summary>
        /// Estimates a boiling point for the substance.
        /// </summary>
        /// <param name="atPressureInPascals">The pressure at which the boiling point is desired.</param>
        /// <returns>The estimated boiling point, in degrees celsius.</returns>
        public double GetEstimatedBoilingPoint(double atPressureInPascals)
        {
            return VaporPressure.VaporPressureCalculator.ComputeBoilingPoint(this, atPressureInPascals);
        }

        /// <summary>
        /// A non-'empty' guid in the collection applies that guid to the whole mass of this substance.
        /// A dictionaryEntry with a guid for a key and a double for a value assumes the double to be a
        /// mass, and the guid to be a spec, and assigns that spec to the specfied quantiy (mass) of
        /// material.
        /// </summary>
        /// <param name="specs"></param>
        public void SetMaterialSpecs(ICollection specs)
        {
            _materialSpecs = new Hashtable();
            foreach (object obj in specs)
            {
                if (obj is Guid)
                {
                    if (!Guid.Empty.Equals(obj))
                    {
                        if (_materialSpecs.ContainsKey((Guid)obj))
                            DuplicateSpec((Guid)obj);
                        _materialSpecs.Add((Guid)obj, _mass);
                    }
                }
                else if (obj is DictionaryEntry)
                {
                    DictionaryEntry de = (DictionaryEntry)obj;
                    if (!Guid.Empty.Equals(de.Key))
                    {
                        if (_materialSpecs.ContainsKey(de.Key))
                            DuplicateSpec(de.Key);
                        _materialSpecs.Add(de.Key, de.Value);
                    }
                }
                else
                {
                    throw new ApplicationException("Attempt to specify a material specification by "
                        + "something other than a collection of [DictionaryEntries with Guids as "
                        + "Keys] or [Guids] - these are the only two constructs that can be used. They "
                        + " are intended to represent MaterialSpecifications or their guids.");
                }
            }
        }

        private void DuplicateSpec(object key)
        {
            throw new ApplicationException("The guid " + key + " was used more than once as a material spec to be applied to " + ToString() + ". This is an error.");
        }

        /// <summary>
        /// Gets the material specification collection.
        /// </summary>
        /// <returns>ICollection.</returns>
        public ICollection GetMaterialSpecs()
        {
            if (_materialSpecs == null)
                return _emptyList;
            ArrayList al = new ArrayList(_materialSpecs);
            return al;
        }

        /// <summary>
        /// Gets the mass, from this substance, of the provided material specification.
        /// </summary>
        /// <param name="identity">The material specification.</param>
        /// <returns>The mass, from this substance, of the provided material specification.</returns>
        public double GetMaterialSpec(Guid identity)
        {
            if (_materialSpecs == null)
                return 0.0;
            if (!_materialSpecs.Contains(identity))
                return 0.0;
            return (double)_materialSpecs[identity];
        }

        /// <summary>
        /// Clears the material specification collection.
        /// </summary>
        public void ClearMaterialSpecs()
        {
            _materialSpecs = null;
        }

        /// <summary>
        /// Converts the entire portion of a substance that is one material spec to another material spec.
        /// </summary>
        /// <param name="fromWhichMs">From which material spec.</param>
        /// <param name="toWhichMs">To which material spec.</param>
        public void ConvertMaterialSpec(Guid fromWhichMs, Guid toWhichMs)
        {
            if (_materialSpecs == null)
                return;
            if (!_materialSpecs.Contains(fromWhichMs))
                return;
            double fromValue = (double)_materialSpecs[fromWhichMs];

            double toValue = (_materialSpecs.Contains(toWhichMs) ? (double)_materialSpecs[toWhichMs] : 0.0);
            _materialSpecs.Remove(fromWhichMs);
            _materialSpecs.Remove(toWhichMs);
            if (!Guid.Empty.Equals(toWhichMs))
                _materialSpecs.Add(toWhichMs, fromValue + toValue);
        }

        /// <summary>
        /// Applies the material specs.
        /// </summary>
        /// <param name="emitted">The emitted.</param>
        /// <param name="original">The original.</param>
		public static void ApplyMaterialSpecs(Substance emitted, Substance original)
        {
            _Debug.Assert(emitted.MaterialType.Equals(original.MaterialType));

            ArrayList emittedSpecs = new ArrayList();
            foreach (DictionaryEntry de in original.GetMaterialSpecs())
            {
                Guid specGuid = (Guid)de.Key;
                double specMass = (double)de.Value;
                double origPctg = specMass / original.Mass;

                double emittedMass = emitted.Mass * origPctg;

                emittedSpecs.Add(new DictionaryEntry(specGuid, emittedMass));
            }
            emitted.SetMaterialSpecs(emittedSpecs);
        }

        /// <summary>
        /// Gets or sets the temperature in degrees Celsius (internally, temperatures are stored in degrees Kelvin.)
        /// </summary>
        /// <value>The temperature.</value>
		public double Temperature
        {
            get
            {
                return Temp + K.KELVIN_TO_CELSIUS;
            }
            set
            {
                double temp = Temp;
                if (!_writeLock.IsWritable)
                    throw new WriteProtectionViolationException(this, _writeLock);
                Temp = value + K.CELSIUS_TO_KELVIN;
                if (temp != Temp)
                {
#if DEBUG
                    if (breakOnIsNaNTemp && double.IsNaN(temp))
                        System.Diagnostics.Debugger.Break();
#endif
                    _ssh.ReportChange();
                    MaterialChanged?.Invoke(this, MaterialChangeType.Temperature);
                }
            }
        }
        /// <summary>
        /// Gets the specific heat of the mixture, in Joules per kilogram degree-K.
        /// </summary>
        /// <value>The specific heat.</value>
        public double SpecificHeat => _type.SpecificHeat;

        /// <summary>
        /// Latent heat of vaporization - the heat energy required to vaporize one kilogram of this material. (J/kg)
        /// </summary>
        /// <value></value>
        public double LatentHeatOfVaporization => _type.LatentHeatOfVaporization;

        /// <summary>
        /// Gets the type of the material.
        /// </summary>
        /// <value>The type of the material.</value>
        public MaterialType MaterialType => _type;

        /// <summary>
        /// Gets the volume of the material in liters.
        /// </summary>
        /// <value>The volume.</value>
        public double Volume => _mass / _type.SpecificGravity;

        internal void SetTempInKelvin(double kelvin)
        {
            double temp = Temp;
            if (!_writeLock.IsWritable)
                throw new WriteProtectionViolationException(this, _writeLock);
            Temp = kelvin;
            if (temp != Temp)
            {
#if DEBUG
                if (breakOnIsNaNTemp && double.IsNaN(Temp))
                    System.Diagnostics.Debugger.Break();
#endif

                _ssh.ReportChange();
                MaterialChanged?.Invoke(this, MaterialChangeType.Temperature);
            }
        }

        /// <summary>
        /// Adds the specified number of joules of energy to the mixture.
        /// </summary>
        /// <param name="joules">The joules to add to the mixture.</param>
        /// <exception cref="WriteProtectionViolationException">Fired if there is a write lock on the substance.</exception>
        public void AddEnergy(double joules)
        {
            if (!_writeLock.IsWritable)
                throw new WriteProtectionViolationException(this, _writeLock);
            Energy += joules;
        }

        /// <summary>
        /// Gets or sets the amount of thermal energy in the substance.
        /// </summary>
        /// <value>The energy.</value>
        /// <exception cref="WriteProtectionViolationException">Fired if there is a write lock on the substance.</exception>
        internal double Energy
        {
            get
            {
                return _type.SpecificHeat * _mass * Temp;
            }
            set
            {
                double temp = Temp;
                if (!_writeLock.IsWritable)
                    throw new WriteProtectionViolationException(this, _writeLock);
                if (_mass == 0.0)
                {
                    Temp = 0.0;
                }
                else
                {
                    Temp = value / (_type.SpecificHeat * _mass);
                }
                if (temp != Temp)
                {
#if DEBUG
                    if (breakOnIsNaNTemp && double.IsNaN(Temp))
                        System.Diagnostics.Debugger.Break();
#endif
                    _ssh.ReportChange();
                    MaterialChanged?.Invoke(this, MaterialChangeType.Temperature);
                }
            }
        }

        /// <summary>
        /// Suspends the issuance of change events. When change events are resumed, one change event will be fired if
        /// the material has changed. This prevents a cascade of change events that would be issued, for example during
        /// the processing of a reaction. See <see cref="Mixture.MaterialChangeDistiller"/>.
        /// </summary>
        public void SuspendChangeEvents()
        {
            if (_eventDistiller == null)
            {
                _eventDistiller = new Mixture.MaterialChangeDistiller();
            }
            _eventDistiller.Hold(ref MaterialChanged);
        }

        /// <summary>
        /// Resumes the change events. When change events are resumed, one change event will be fired if
        /// the material has changed. This prevents a cascade of change events that would be issued, for example during
        /// the processing of a reaction.
        /// </summary>
        /// <param name="issueSummaryEvents">if set to <c>true</c>, issues a summarizing event for each change type that has occurred.</param>
        public void ResumeChangeEvents(bool issueSummaryEvents)
        {
            if (_eventDistiller == null)
            {
                _eventDistiller = new Mixture.MaterialChangeDistiller();
            }
            _eventDistiller.Release(ref MaterialChanged, issueSummaryEvents);
        }


        /// <summary>
        /// Adds the specified substance to this substance. Both substances must be of the same material type, or an exception will fire.
        /// </summary>
        /// <param name="substance">The substance.</param>
        /// <exception cref="WriteProtectionViolationException">Fired if there is a write lock on the substance.</exception>
        /// <exception cref="ApplicationException">Fired if the substances are not of the same material type.</exception>
        public void Add(Substance substance)
        {
            //if ( substance.Name.Equals("Water") ) System.Diagnostics.Debugger.Break();
            if (!_writeLock.IsWritable)
                throw new WriteProtectionViolationException(this, _writeLock);
            if (!_type.Equals(substance._type))
                throw new ApplicationException("Substance " + Name + " being added to substance " + substance.Name + "!");
            double oldEnergy = Energy;
            _mass += substance.Mass; // Have to set Mass before setting energy, since energy->temp requires mass.
            Energy = oldEnergy + substance.Energy;

            if (substance._materialSpecs != null)
            {

                // If the incoming has materialSpecs and we don't, then we must create them.
                if (_materialSpecs == null)
                    _materialSpecs = new Hashtable();
                foreach (DictionaryEntry de in substance._materialSpecs)
                {
                    if (!_materialSpecs.ContainsKey(de.Key))
                        _materialSpecs.Add(de.Key, 0.0);
                }

                foreach (DictionaryEntry de in substance._materialSpecs)
                {
                    if (_materialSpecs.Contains(de.Key))
                    {
                        double old = (double)_materialSpecs[de.Key];
                        _materialSpecs.Remove(de.Key);
                        _materialSpecs.Add(de.Key, (old + (double)de.Value));
                    }
                    else
                    {
                        if (!Guid.Empty.Equals(de.Key))
                            _materialSpecs.Add(de.Key, de.Value);
                    }
                }
            }

            _ssh.ReportChange();
            MaterialChanged?.Invoke(this, MaterialChangeType.Contents);
        }

        /// <summary>
        /// Removes the specified substance from this substance. Both substances must be of the same material type, or an exception will fire..
        /// </summary>
        /// <param name="substance">The substance.</param>
        /// <returns>Substance.</returns>
        /// <exception cref="ApplicationException">Fired if the substances are not of the same material type.</exception>
        /// <exception cref="WriteProtectionViolationException">Fired if there is a write lock on the substance.</exception>
        public virtual Substance Remove(Substance substance)
        {
            if (!_type.Equals(substance._type))
                throw new ApplicationException("Substance " + Name + " being removed from substance " + substance.Name + "!");
            if (!_writeLock.IsWritable)
                throw new WriteProtectionViolationException(this, _writeLock);
            // Trace.Write("Removing " + substance.Mass + " kg of " + substance.Name + " from an existing " + m_mass + " kg, ");
            // TODO: Assert we are not removing more than is there.
            double mass = Math.Min(substance.Mass, _mass);
            Substance s = Remove(mass);
            // _Debug.WriteLine("leaving " + m_mass + " kg.");
            _ssh.ReportChange();
            MaterialChanged?.Invoke(this, MaterialChangeType.Contents);
            return s;
        }

        /// <summary>
        /// Removes the specified mass from this substance.
        /// </summary>
        /// <param name="mass">The mass.</param>
        /// <returns>Substance.</returns>
        /// <exception cref="WriteProtectionViolationException">Fired if there is a write lock on the substance.</exception>
        public virtual Substance Remove(double mass)
        {
            if (!_writeLock.IsWritable)
                throw new WriteProtectionViolationException(this, _writeLock);

            Substance s = (Substance)MaterialType.CreateMass(mass, Temperature);

            if (_mass == float.MaxValue)
            {
                if (_materialSpecs != null)
                {
                    // First figure out the total mass...
                    double totalMass = _materialSpecs.Cast<DictionaryEntry>().Sum(de => (double)de.Value);

                    if (totalMass > 0.0)
                    {
                        foreach (DictionaryEntry de in _materialSpecs)
                        {
                            if (s._materialSpecs == null)
                                s._materialSpecs = new Hashtable();
                            s._materialSpecs.Add(de.Key, mass * ((double)de.Value) / totalMass);
                        }
                    }
                }
                return s;
            }
            else
            {
                double pctRemoved = mass / _mass;
                Hashtable ht = new Hashtable();
                if (_materialSpecs != null)
                {
                    foreach (DictionaryEntry de in _materialSpecs)
                    {
                        if (s._materialSpecs == null)
                            s._materialSpecs = new Hashtable();
                        if (s._materialSpecs.ContainsKey(de.Key))
                        {
                            s._materialSpecs[de.Key] = pctRemoved * ((double)de.Value);
                        }
                        else
                        {
                            s._materialSpecs.Add(de.Key, pctRemoved * ((double)de.Value));
                        }
                        if (pctRemoved < 1.0)
                            ht.Add(de.Key, (1.0 - pctRemoved) * ((double)de.Value));
                    }
                }
                _materialSpecs = ht;
            }

            double have = Energy;
            double minus = s.Energy;
            _mass -= s.Mass; // Have to set Mass before setting energy, since energy->temp requires mass.
            Energy = (have - minus);

            _ssh.ReportChange();
            MaterialChanged?.Invoke(this, MaterialChangeType.Contents);
            return s;
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:Highpoint.Sage.Materials.Chemistry.Mixture"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:Highpoint.Sage.Materials.Chemistry.Mixture"></see>.
        /// </returns>
        public override string ToString()
        {
            return ToString("F1", "F2");
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:Highpoint.Sage.Materials.Chemistry.IMaterial"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:Highpoint.Sage.Materials.Chemistry.IMaterial"></see>.
        /// </returns>
        public string ToStringWithoutTemperature()
        {
            return ToStringWithoutTemperature("F2");
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:Highpoint.Sage.Materials.Chemistry.IMaterial"></see>.
        /// </summary>
        /// <param name="massFmt">The mass format string. For example, &quot;F2&quot; will display to two decimals.</param>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:Highpoint.Sage.Materials.Chemistry.IMaterial"></see>.
        /// </returns>
        public string ToStringWithoutTemperature(string massFmt)
        {
            string fmtString = "{0:" + massFmt + "} kg of {1}";
            return string.Format(fmtString, Mass, Name);
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:Highpoint.Sage.Materials.Chemistry.IMaterial"></see>.
        /// Uses caller-supplied format strings in forming the numbers representing mass and temperature.
        /// </summary>
        /// <param name="tempFmt">The temperature's numerical format string.</param>
        /// <param name="massFmt">The mass's numerical format string.</param>
        /// <returns></returns>
        public string ToString(string tempFmt, string massFmt)
        {
            return "Substance (" + Temperature.ToString(tempFmt) + " deg C) of " + ToStringWithoutTemperature(massFmt);
        }


#if NOT_DEFINED
		public override string ToString()
        {
			return this.ToString(DEFAULT_MASS_FORMAT_STRING);
		}

		public string ToString(string massFmt)
        {
			string result = _mass.ToString(massFmt) + " kg of " + Name;
			if ( _materialSpecs != null )
            {
				foreach ( DictionaryEntry de in _materialSpecs )
                {
					result += ("[" + de.Key + ":" + de.Value + "]");
				}
			}
			return result;
		}
#endif

        /// <summary>
        /// Retrieves a memento from the substance, or reconstitutes it from a memento.
        /// </summary>
        /// <value>The memento.</value>
        /// <exception cref="WriteProtectionViolationException">Fired if this substance has an active write lock, and a reconstitution is requested.</exception>
        public IMemento Memento
        {
            get
            {
                if (!_ssh.HasChanged && _memento != null)
                    return _memento;
                _memento = new SubstanceMemento(this);
                _ssh.ReportSnapshot();
                return _memento;
            }
            set
            {
                if (!_writeLock.IsWritable)
                    throw new WriteProtectionViolationException(this, _writeLock);
                ((SubstanceMemento)value).Load(this);
                _ssh.HasChanged = false;
            }
        }

        /// <summary>
        /// This event is fired when the memento contents will have changed. This does not
        /// imply that the memento <i>has</i> changed, since the memento is
        /// recorded, typically, only on request. It <i>does</i> imply that if
        /// you ask for a memento, it might be in some way different from any
        /// memento you might have previously acquired.
        /// </summary>
        /// <exception cref="WriteProtectionViolationException">
        /// </exception>
        public event MementoChangeEvent MementoChangeEvent
        {
            add
            {
                if (!_writeLock.IsWritable)
                    throw new WriteProtectionViolationException(this, _writeLock);
                _ssh.MementoChangeEvent += value;
            }
            remove
            {
                if (!_writeLock.IsWritable)
                    throw new WriteProtectionViolationException(this, _writeLock);
                _ssh.MementoChangeEvent -= value;
            }
        }

        /// <summary>
        /// Reports whether the object has changed relative to its memento
        /// since the last memento was recorded.
        /// </summary>
        /// <value><c>true</c> if this instance has changed; otherwise, <c>false</c>.</value>
        public bool HasChanged => _ssh.HasChanged;

        /// <summary>
        /// Indicates whether this object can report memento changes to its
        /// parent. (Mementos can contain other mementos.)
        /// </summary>
        /// <value><c>true</c> if [reports own changes]; otherwise, <c>false</c>.</value>
        public bool ReportsOwnChanges => _ssh.ReportsOwnChanges;

        /// <summary>
        /// Class SubstanceMemento creates a moment-in-time snapshot (see Memento design pattern) of a substance.
        /// </summary>
        /// <seealso cref="Highpoint.Sage.Utility.Mementos.IMemento" />
        public class SubstanceMemento : IMemento
        {

            #region Private Fields
            /// <summary>
            /// The material type
            /// </summary>
            private readonly MaterialType _materialType;
            /// <summary>
            /// The temperature
            /// </summary>
            private readonly double _temperature;
            /// <summary>
            /// The mass
            /// </summary>
            private readonly double _mass;
            /// <summary>
            /// The material specs
            /// </summary>
            private readonly Hashtable _matlSpecs;

            #endregion

            /// <summary>
            /// Initializes a new instance of the <see cref="SubstanceMemento"/> class.
            /// </summary>
            /// <param name="substance">The substance.</param>
            public SubstanceMemento(Substance substance)
            {
                _materialType = substance._type;
                _temperature = substance.Temperature;
                _mass = substance._mass;

                if (substance._materialSpecs != null && substance._materialSpecs.Count > 0)
                {
                    _matlSpecs = new Hashtable();
                    foreach (DictionaryEntry de in substance._materialSpecs)
                    {
                        _matlSpecs.Add(de.Key, de.Value);
                    }
                }
            }

            /// <summary>
            /// Creates an empty copy of whatever object this memento can reconstitute. Some
            /// mementos are only able to reconstitute into their source objects (they can only
            /// be used to restore state in the same object), and these mementos will return a
            /// reference to that object.)
            /// </summary>
            /// <returns>ISupportsMementos.</returns>
            public ISupportsMementos CreateTarget()
            {
                Substance substance = (Substance)_materialType.CreateMass(_mass, _temperature);
                if (_matlSpecs != null)
                {
                    substance.SetMaterialSpecs(_matlSpecs);
                }
                return substance;
            }

            /// <summary>
            /// Loads the contents of this Memento into the provided object.
            /// </summary>
            /// <param name="ism">The object to receive the contents of the memento.</param>
            public void Load(ISupportsMementos ism)
            {
                Substance substance = (Substance)ism;
                substance._mass = _mass;
                substance.Temperature = _temperature;
                substance._type = _materialType;

                if (_matlSpecs != null)
                {
                    substance.SetMaterialSpecs(_matlSpecs);
                }
                else
                {
                    substance.ClearMaterialSpecs();
                }

                OnLoadCompleted?.Invoke(this);
            }

            /// <summary>
            /// Emits an IDictionary form of the memento that can be, for example, dumped to
            /// Trace.
            /// </summary>
            /// <returns>An IDictionary form of the memento.</returns>
            public IDictionary GetDictionary()
            {
                IDictionary retval = new System.Collections.Specialized.ListDictionary();
                retval.Add("Name", _materialType.Name);
                retval.Add("Mass", _mass);
                retval.Add("Volume", _mass / _materialType.SpecificGravity);
                retval.Add("Temp", _temperature);
                if (_matlSpecs != null)
                {
                    int i = 0;
                    foreach (DictionaryEntry de in _matlSpecs)
                    {
                        retval.Add("MatlSpec_" + (i++), "Guid:" + de.Key + ", Amt:" + de.Value);
                    }
                }
                return retval;
            }

            /// <summary>
            /// Ascertains equality between this one and the other one.
            /// </summary>
            /// <param name="otherOne">The other one.</param>
            /// <returns><c>true</c> if this one and the other one are equal, <c>false</c> otherwise.</returns>
            public bool Equals(IMemento otherOne)
            {
                if (otherOne == null)
                    return false;
                if (this == otherOne)
                    return true;
                if (!(otherOne is SubstanceMemento))
                    return false;

                SubstanceMemento smog = (SubstanceMemento)otherOne;

                bool eq = (_mass == smog._mass && _temperature == smog._temperature && _materialType == smog._materialType);

                if (!eq)
                    return false;

                if (_matlSpecs != null)
                {
                    foreach (DictionaryEntry de in _matlSpecs)
                    {
                        if (smog._matlSpecs == null)
                            return false;
                        if (!smog._matlSpecs.Contains(de.Key))
                            return false;
                        if (!smog._matlSpecs[de.Key].Equals(de.Value))
                            return false;
                    }
                }

                return true;
            }

            /// <summary>
            /// This event is fired once this memento has completed its Load(ISupportsMementos ism) invocation.
            /// </summary>
            public event MementoEvent OnLoadCompleted;

            /// <summary>
            /// This holds a reference to the memento, if any, that contains this memento.
            /// </summary>
            /// <value>The parent.</value>
            public IMemento Parent
            {
                get; set;
            }
        }


        /// <summary>
        /// Gets or sets the tag, which is a user-supplied data element.
        /// </summary>
        /// <value>The tag.</value>
        public object Tag
        {
            get; set;
        }

        #region >>> Serialization Support <<< 
        /// <summary>
        /// Initializes a new instance of the <see cref="Substance"/> class.
        /// </summary>
        public Substance()
        {
            _ssh = new MementoHelper(this, true);
        }
        /// <summary>
        /// Stores this object to the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
        public void SerializeTo(XmlSerializationContext xmlsc)
        {
            xmlsc.StoreObject("Mass", _mass);
            xmlsc.StoreObject("Temp", Temp);
            xmlsc.StoreObject("Type", _type);
        }
        /// <summary>
        /// Reconstitutes this object from the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
        public void DeserializeFrom(XmlSerializationContext xmlsc)
        {
            _mass = (double)xmlsc.LoadObject("Mass");
            Temp = (double)xmlsc.LoadObject("Temp");
            _type = (MaterialType)xmlsc.LoadObject("Type");
            _ssh.ReportChange();
        }
        #endregion


        /// <summary>
        /// A comparer that compares this substance with another substance only by mass. Useful for sorting mixture contents.
        /// </summary>
        /// <value>The comparer.</value>
        public static IComparer<Substance> ByMass { get; } = new ByMassComparer();

        /// <summary>
        /// A comparer that compares this substance with another substance by mass and then name. Useful for sorting mixture contents.
        /// </summary>
        /// <value>The comparer.</value>
        public static IComparer<Substance> ByMassThenName { get; } = new ByMassThenNameComparer();


        private class ByMassComparer : IComparer<Substance>
        {

            #region IComparer<Substance> Members

            public int Compare(Substance x, Substance y)
            {
                return Comparer.Default.Compare(x.Mass, y.Mass);
            }

            #endregion
        }

        private class ByMassThenNameComparer : IComparer<Substance>
        {

            #region IComparer<Substance> Members

            public int Compare(Substance x, Substance y)
            {
                int retval = Comparer.Default.Compare(x.Mass, y.Mass);
                if (retval != 0)
                    return retval;
                return Comparer.Default.Compare(x.Name, y.Name);
            }

            #endregion
        }
    }
}
