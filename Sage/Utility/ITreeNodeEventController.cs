/* This source code licensed under the GNU Affero General Public License */
//#define PREANNOUNCE
// ReSharper disable NonReadonlyMemberInGetHashCode
// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Utility
{
    public interface ITreeNodeEventController<T>
    {

#if PREANNOUNCE
        void OnAboutToLoseParent(ITreeNode<T> parent);
        void OnAboutToGainParent(ITreeNode<T> parent);
        void OnAboutToLoseChild(ITreeNode<T> child);
        void OnAboutToGainChild(ITreeNode<T> child);
#endif
        void OnLostParent(ITreeNode<T> parent);
        void OnGainedParent(ITreeNode<T> parent);
        void OnLostChild(ITreeNode<T> child);
        void OnGainedChild(ITreeNode<T> child);
        void OnChildrenResorted(ITreeNode<T> self);
        void OnSubtreeChanged(SubtreeChangeType changeType, ITreeNode<T> where);
    }
}