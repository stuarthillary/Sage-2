/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Materials.Chemistry;
using Highpoint.Sage.Mathematics.Scaling;
using Highpoint.Sage.SimCore;
using System;

namespace Highpoint.Sage.Materials
{
    /// <summary>
    /// Represents a request for material transfer, specified as mass of a particular substance. 
    /// </summary>
    public class MaterialTransferSpecByMass : IMaterialTransferHelper, IScalable
    {
        private readonly MaterialType _materialType;
        private double _mass;
        private TimeSpan _duration;

        private IDoubleScalingAdapter _massScaler = null;
        private ITimeSpanScalingAdapter _durationScaler = null;

        /// <summary>
        /// Gets the mass scaler associated with this MaterialTransferSpecByMass.
        /// </summary>
        /// <value>The mass scaler.</value>
		public IDoubleScalingAdapter MassScaler
        {
            get
            {
                return _massScaler;
            }
        }
        /// <summary>
        /// Gets the duration scaler associated with this MaterialTransferSpecByMass.
        /// </summary>
        /// <value>The duration scaler.</value>
		public ITimeSpanScalingAdapter DurationScaler
        {
            get
            {
                return _durationScaler;
            }
        }

        /// <summary>
        /// Creates a MaterialTransferSpecByMass that will transfer a specified mass of a
        /// specified type of material, over a specified duration. It is presumed that duration
        /// and mass do NOT scale, so if you want them to, you will need to add the
        /// appropriate scaling adapters.
        /// </summary>
        /// <param name="matlType">The material type to be transferred.</param>
        /// <param name="mass">The base amount (before scaling) of the material to transfer.</param>
        /// <param name="duration">The base duration (before scaling) of the transfer.</param>
        public MaterialTransferSpecByMass(MaterialType matlType, double mass, TimeSpan duration)
        {
            _materialType = matlType;
            _mass = mass;
            _duration = duration;
        }

        /// <summary>
        /// Provides this transferSpec with a scaling adapter that will scale the transfer duration.<p></p>
        /// As an example, adding a TimeSpanLinearScalingAdapter with a linearity of 1.0 will cause
        /// duration to scale precisely in proportion to the aggregate scale provided in the Rescale operation.
        /// </summary>
        /// <param name="tsa">The ITimeSpanScalingAdapter that will provide timespan scaling for this transfer.</param>
        public void SetDurationScalingAdapter(ITimeSpanScalingAdapter tsa)
        {
            _durationScaler = tsa;
        }

        /// <summary>
        /// Provides this transferSpec with a scaling adapter that will scale the mass to be transferred.<p></p>
        /// As an example, adding a DoubleLinearScalingAdapter with a linearity of 1.0 will cause
        /// mass to scale precisely in proportion to the aggregate scale provided in the Rescale operation.
        /// </summary>
        /// <param name="dsa">The scaling adapter that will perform mass scaling for this transfer spec.</param>
        public void SetMassScalingAdapter(IDoubleScalingAdapter dsa)
        {
            _massScaler = dsa;
        }

        /// <summary>
        /// The material type to be transferred.
        /// </summary>
		public MaterialType MaterialType
        {
            get
            {
                return _materialType;
            }
        }

        /// <summary>
        /// The mass to be transferred. This value will reflect any scaling operations that have been done.
        /// </summary>
		public double Mass
        {
            get
            {
                return _mass;
            }
        }

        /// <summary>
        /// The duration of the transfer. This value will reflect any scaling operations that have been done.
        /// </summary>
		public TimeSpan Duration
        {
            get
            {
                return _duration;
            }
        }

        /// <summary>
        /// Commands a rescale of the transfer spec's mass and duration to a scale factor of the originally
        /// defined size.
        /// </summary>
        /// <param name="aggregateScale">The scaling to be applied to the initally-defined values.</param>
        public void Rescale(double aggregateScale)
        {
            if (_massScaler != null)
            {
                _massScaler.Rescale(aggregateScale);
                _mass = _massScaler.CurrentValue;
            }
            if (_durationScaler != null)
            {
                _durationScaler.Rescale(aggregateScale);
                _duration = _durationScaler.CurrentValue;
            }
        }

        /// <summary>
        /// Gets the material to be transferred by extracting it from the source material.
        /// </summary>
        /// <param name="source">The material from which the transfer is to be made.</param>
        /// <returns>The material to be transferred.</returns>
        public virtual IMaterial GetExtract(IMaterial source)
        {

            double massToRemove = Mass;
            IMaterial retval = null;

            if (_materialType == null)
            {
                // We're taking a mass of the whole substance or mixture.
                if (source is Substance)
                {
                    retval = ((Substance)source).Remove(massToRemove);
                }
                else
                {
                    retval = ((Mixture)source).RemoveMaterial(massToRemove);
                }
            }
            else
            {
                if (source is Substance)
                {
                    Substance s = ((Substance)source);
                    if (s.MaterialType.Equals(_materialType))
                    {
                        retval = s.Remove(massToRemove);
                    }
                    else
                    {
                        return _materialType.CreateMass(0, 0);
                    }
                }
                else if (source is Mixture)
                {
                    Mixture m = ((Mixture)source);
                    retval = m.RemoveMaterial(_materialType, massToRemove);
                    if (retval == null)
                    {
                        retval = _materialType.CreateMass(0, 0);
                    }
                }
                else
                {
                    throw new ApplicationException("Attempt to remove an unknown implementer of IMaterial from a mixture!");
                }
            }

            if (retval == null)
            {
                throw new ApplicationException("Unable to get extract from " + source);
            }

            //BUG: If the extract didn't get all it wanted, and time is scaled AND super- or sub-linear, the reported duration will be wrong.
            if (retval.Mass != massToRemove && _durationScaler != null)
            {
                // We didn't get all the mass we wanted, so we will reset mass and duration to the amount we got.
                double currentScale = (double)_durationScaler.CurrentValue.Ticks / (double)_durationScaler.FullScaleValue.Ticks;
                double factor = retval.Mass / massToRemove;
                _duration = TimeSpan.FromTicks((long)(_duration.Ticks * factor));
            }
            return retval;
        }

        /// <summary>
        /// Clone operation allows a MTSBM object to be reused, thereby eliminating the need to
        /// re-specify each time the transfer is to take place.
        /// </summary>
        /// <returns>A clone of this instance.</returns>
        public virtual object Clone()
        {
            MaterialTransferSpecByMass mtsm = new MaterialTransferSpecByMass(_materialType, _mass, _duration);
            if (_durationScaler != null)
                mtsm.SetDurationScalingAdapter(_durationScaler.Clone());
            if (_massScaler != null)
                mtsm.SetMassScalingAdapter(_massScaler.Clone());
            if (CloneEvent != null)
                CloneEvent(this, mtsm);
            return mtsm;
        }

        /// <summary>
        /// Fired after a cloning operation has taken place.
        /// </summary>
		public event CloneHandler CloneEvent;

        /// <summary>
        /// Provides a human-readable description of the transfer mass, material, and duration, scaled as requested.
        /// </summary>
        /// <returns>A human-readable description of the transfer mass, material, and duration, scaled as requested.</returns>
        public override string ToString()
        {
            string material = (_materialType == null ? "Entire Mixture" : _materialType.Name);
            return _mass.ToString("F2") + " kg of " + material + ", which should take " + _duration;
        }
    }
}