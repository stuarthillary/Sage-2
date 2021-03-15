/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Materials.Chemistry;
using System;

namespace Highpoint.Sage.Materials
{
    public class MaterialTransfer
    {

        #region Private Fields
        private readonly Mixture _mixture;
        private TimeSpan _sourceDuration;
        private TimeSpan _destinationDuration;
        #endregion Private Fields

        /// <summary>
        /// Creates a MaterialTransfer object.
        /// </summary>
        /// <param name="mixture">The mixture being transferred.</param>
        /// <param name="duration">The duration of the transfer.</param>
        public MaterialTransfer(Mixture mixture, TimeSpan duration)
        {
            _mixture = mixture;
            SourceDuration = duration;
            DestinationDuration = duration;
        }

        /// <summary>
        /// The mixture being transferred.
        /// </summary>
        public Mixture Mixture
        {
            get
            {
                return _mixture;
            }
        }

        /// <summary>
        /// The amount of time it takes for the source to output the mixture represented in this Transfer.
        /// </summary>
        public TimeSpan SourceDuration
        {
            set
            {
                _sourceDuration = value;
            }
            get
            {
                return _sourceDuration;
            }
        }
        /// <summary>
        /// The amount of time it takes for the sink to receive the mixture represented in this Transfer.
        /// </summary>
        public TimeSpan DestinationDuration
        {
            set
            {
                _destinationDuration = value;
            }
            get
            {
                return _destinationDuration;
            }
        }
    }
}

