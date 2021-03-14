/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.ItemBased.Servers
{
    /// <summary>
    /// Optional interface for a service object, in case it wants to be notified of its
    /// stages of participation with, or processing by, a server.
    /// </summary>
    public interface IServiceObject
    {
        void OnServiceBeginning(IServer server);
        void OnServiceCompleting(IServer server);
    }
}