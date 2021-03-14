/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Materials.Chemistry
{
    /// <summary>
    /// ISupportsReactions is an entity that keeps track of reactions and materials, and provides each
    /// a place to acquire references to the other. If you define a reaction with Potassium in it, and
    /// use the Potassium material type to create some potassium, you can be sure the potassium will be
    /// able to react if the material is made from the MaterialCatalog, and the reaction is stored in
    /// the ReactionProcessor of, the same instance of ISupportsReactions.
    /// </summary>
    public interface ISupportsReactions : IHasMaterials
    {
        ReactionProcessor MyReactionProcessor
        {
            get;
        }
        void RegisterReactionProcessor(ReactionProcessor rp);
    }
}
