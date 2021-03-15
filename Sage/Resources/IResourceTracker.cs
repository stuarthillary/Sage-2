/* This source code licensed under the GNU Affero General Public License */

using System.Collections;
// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Resources
{
    /// <summary>
    /// Implemented by anything that gathers ResourceEventRecords on a specific resource or resources.
    /// </summary>
    public interface IResourceTracker : IEnumerable
    {

        /// <summary>
        /// Clears all ResourceEventRecords.
        /// </summary>
        void Clear();

        /// <summary>
        /// Turns on tracking for this ResourceTracker. This defaults to 'true', and
        /// allEnabled must also be true, in order for a ResourceTracker to track.
        /// </summary>
        bool Enabled
        {
            get; set;
        }

        /// <summary>
        /// Allows for the setting of the active filter on the records
        /// </summary>
        ResourceEventRecordFilter Filter
        {
            set;
        }

        /// <summary>
        /// Returns all records that have been collected
        /// </summary>
        ICollection EventRecords
        {
            get;
        }

        /// <summary>
        /// The initial value(s) of all resources that are being tracked
        /// </summary>
        double InitialAvailable
        {
            get;
        }
    }
}