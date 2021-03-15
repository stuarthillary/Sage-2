/* This source code licensed under the GNU Affero General Public License */

// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Resources
{
    /// <summary>
    /// A static holder for some static and stateless <see cref="Highpoint.Sage.Resources.ResourceEventRecordFilter"/>s.
    /// </summary>
    public static class ResourceEventRecordFilters
    {

        /// <summary>
        /// A filter that filters out requests, allowing the actual Acquire &amp; Release events to pass.
        /// </summary>
        /// <value>The filter.</value>
        public static ResourceEventRecordFilter FilterOutRequests => filterOutRequests;

        /// <summary>
        /// A filter that gets the acquire and release events only.
        /// </summary>
        /// <value>The acquire and release events only.</value>
		public static ResourceEventRecordFilter AcquireAndReleaseOnly => acquireAndReleaseOnly;

        /// <summary>
        /// Gets all events.
        /// </summary>
        /// <value>All events.</value>
		public static ResourceEventRecordFilter AllEvents => allEvents;

        #region Private Members
        private static bool acquireAndReleaseOnly(ResourceEventRecord candidate)
        {
            return (candidate.Action == ResourceAction.Acquired || candidate.Action == ResourceAction.Released);
        }
        private static bool filterOutRequests(ResourceEventRecord candidate)
        {
            return (candidate.Action != ResourceAction.Request);
        }
        private static bool allEvents(ResourceEventRecord candidate)
        {
            return true;
        }

        #endregion

    }
}