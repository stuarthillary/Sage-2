/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.SimCore
{
    public interface IInitializationManager
    {
        void AddInitializationTask(Initializer initializer, params object[] parameters);
    }
}
