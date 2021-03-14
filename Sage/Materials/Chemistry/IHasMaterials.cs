/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Materials.Chemistry
{
    /// <summary>
    /// Interface IHasMaterials is implemented by any object that serves as a librarian for material types, which are held in a MaterialCatalog.
    /// </summary>
    public interface IHasMaterials
    {
        MaterialCatalog MyMaterialCatalog
        {
            get;
        }
        /// <summary>
        /// Registers the material catalog.
        /// </summary>
        /// <param name="mcat">The mcat.</param>
        void RegisterMaterialCatalog(MaterialCatalog mcat);
    }
}
