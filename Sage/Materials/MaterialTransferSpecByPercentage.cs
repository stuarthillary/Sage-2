/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Materials.Chemistry;
using Highpoint.Sage.SimCore;
using System;

namespace Highpoint.Sage.Materials
{
    /// <summary>
    /// Represents a request for material transfer, specified as a percentage of a particular
    /// material in the source mixture.  Stored in an arraylist keyed to the port on which it
    /// is to be effected, in the SOMTaskDetails.m_xferOutSpecLists hashtable.<p></p>
    /// <p></p>
    /// Note that a MaterialTransferSpecByPercentage is not scalable. It is, in effect, scaled
    /// by the amount of material in the source container.
    /// </summary>
    public class MaterialTransferSpecByPercentage : IMaterialTransferHelper
    {
        private readonly MaterialType _materialType;
        private readonly double _percentage;
        private TimeSpan _durationPerKilogram;
        private TimeSpan _duration;
        private double _actualMass;

        /// <summary>
        /// Creates a MaterialTransferSpecByMass that will transfer a specified mass of a
        /// specified type of material, over a specified duration. It is presumed that duration
        /// and mass do NOT scale, so if you want them to, you will need to add the
        /// appropriate scaling adapters.
        /// </summary>
        /// <param name="matlType">The material type to be transferred.</param>
        /// <param name="percentage">The percentage of the material of the specified type in the source container.</param>
        /// <param name="durationPerKilogram">The timespan required to transfer each kilogram of material.</param>
        public MaterialTransferSpecByPercentage(MaterialType matlType, double percentage, TimeSpan durationPerKilogram)
        {
            _materialType = matlType;
            _percentage = percentage;
            _duration = TimeSpan.Zero;
            _durationPerKilogram = durationPerKilogram;
        }

        /// <summary>
        /// The type of the material to be transferred.
        /// </summary>
		public virtual MaterialType MaterialType
        {
            get
            {
                return _materialType;
            }
        }

        /// <summary>
        /// The percentage of the material of the specified type that is found in the source container, that should be transferred.
        /// </summary>
        public virtual double Percentage
        {
            get
            {
                return _percentage;
            }
        }

        /// <summary>
        /// The total duration of the transfer. Note that since this is dependent upon the mass,
        /// which is dependent on how much of the type of material was found in the source container,
        /// it will not be known correctly until after <b>GetExtract</b> is called.
        /// </summary>
		public virtual TimeSpan Duration
        {
            get
            {
                return _duration;
            }
        }

        /// <summary>
        /// The total mass of the transfer. Note that since this is dependent upon how
        /// much of the type of material was found in the source container,
        /// it will not be known correctly until after <b>GetExtract</b> is called.
        /// </summary>
		public virtual double Mass
        {
            get
            {
                return _actualMass;
            }
        }

        /// <summary>
        /// The timespan to allot for each kilogram transferred.
        /// </summary>
        public TimeSpan DurationPerKilogram
        {
            get
            {
                return _durationPerKilogram;
            }
        }

        /// <summary>
        /// Gets the material to be transferred by extracting it from the source material.
        /// </summary>
        /// <param name="source">The material from which the transfer is to be made.</param>
        /// <returns>The material to be transferred.</returns>
        public virtual IMaterial GetExtract(IMaterial source)
        {
            IMaterial retval = null;
            if (_materialType == null)
            {
                double massToRemove = source.Mass * Percentage;
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
                        retval = s.Remove(s.Mass * _percentage);
                    }
                    else
                    {
                        retval = _materialType.CreateMass(0, 0);
                    }
                }
                else if (source is Mixture)
                {
                    Mixture m = ((Mixture)source);
                    retval = m.RemoveMaterial(_materialType, m.ContainedMassOf(_materialType) * _percentage);
                    if (retval == null)
                        retval = _materialType.CreateMass(0, 0);
                }
                else
                {
                    throw new ApplicationException("Attempt to remove an unknown implementer of IMaterial from a mixture!");
                }
            }

            _actualMass = retval.Mass;
            _duration = TimeSpan.FromTicks((long)(_durationPerKilogram.Ticks * retval.Mass));
            return retval;

        }

        /// <summary>
        /// Clone operation allows a MTSBP object to be reused, thereby eliminating the need to
        /// re-specify each time the transfer is to take place.
        /// </summary>
        /// <returns>A clone of this instance.</returns>
        public virtual object Clone()
        {
            MaterialTransferSpecByPercentage mtsp = new MaterialTransferSpecByPercentage(_materialType, _percentage, _durationPerKilogram);
            if (CloneEvent != null)
                CloneEvent(this, mtsp);
            return mtsp;
        }

        /// <summary>
        /// Fired after a clone operation has taken place.
        /// </summary>
		public event CloneHandler CloneEvent;

        /// <summary>
        /// Provides a human-readable description of the transfer mass, material, and duration, scaled as requested.
        /// </summary>
        /// <returns>A human-readable description of the transfer mass, material, and duration, scaled as requested.</returns>
        public override string ToString()
        {
            string material = (_materialType == null ? "Entire Mixture" : _materialType.Name);
            return (_percentage * 100).ToString("F2") + "% of " + material + ", which should take " + _duration;
        }

    }
}