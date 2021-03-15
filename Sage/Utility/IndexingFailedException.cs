/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.Utility
{
    /// <summary>
    /// Class IndexingFailedException - thrown when indexing has failed.
    /// </summary>
    /// <seealso cref="System.ApplicationException" />
    public class IndexingFailedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexingFailedException"/> class.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        public IndexingFailedException(string msg) : base(msg) { }
    }
}
