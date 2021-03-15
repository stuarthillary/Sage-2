/* This source code licensed under the GNU Affero General Public License */

using System.Collections;

#pragma warning disable 1587
/// <summary>
/// The Mementos namespace contains an implementation of a Memento pattern - that is, an object that implements ISupportsMememtos
/// is capable of generating and maintaining mementos, which are representations of internal state at a given time that can be
/// used to restore the object's internal state to a previous set of values. MementoHelper exists to simplify implementation of the
/// ISupportsMememtos interface.
/// </summary>
#pragma warning restore 1587
namespace Highpoint.Sage.Utility.Mementos
{
    /// <summary>
    /// A class that will perform much of the bookkeeping required to implement
    /// the ISupportsMementos interface, including child management, change tracking
    /// and memento generation.
    /// </summary>
    public class MementoHelper
    {

        #region private fields
        private readonly ISupportsMementos _iss;
        private readonly MementoChangeEvent _childChangeHandler;
        private readonly bool _wrappeeReportsOwnChanges;
        private ArrayList _children;        // children who report their own changes.
        private ArrayList _problemChildren; // children who can't report their own changes.
        private bool _hasChanged = true;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="MementoHelper"/> class.
        /// </summary>
        /// <param name="iss">The memento supporter that wraps this helper.</param>
        /// <param name="wrappeeReportsOwnChanges">if set to <c>true</c> the memento supporter is able to report its own changes.</param>
        public MementoHelper(ISupportsMementos iss, bool wrappeeReportsOwnChanges)
        {
            _wrappeeReportsOwnChanges = wrappeeReportsOwnChanges;
            _iss = iss;
            _childChangeHandler = ReportChange;
        }

        /// <summary>
        /// Clears the state of this helper.
        /// </summary>
        public void Clear()
        {

            if (_children != null)
            {
                foreach (ISupportsMementos child in _children)
                    child.MementoChangeEvent -= _childChangeHandler;
                _children.Clear();
            }
            _problemChildren?.Clear();


            _hasChanged = true; // Forces the parent to regather a memento.
        }

        /// <summary>
        /// Informs the helper that the memento supporter that wraps this helper has gained a child.
        /// </summary>
        /// <param name="child">The child.</param>
        public void AddChild(ISupportsMementos child)
        {
            if (child.ReportsOwnChanges)
            {
                if (_children == null)
                    _children = new ArrayList();
                if (!_children.Contains(child))
                {
                    _children.Add(child);
                    child.MementoChangeEvent += _childChangeHandler;
                }
            }
            else
            {
                if (_problemChildren == null)
                    _problemChildren = new ArrayList();
                if (!_problemChildren.Contains(child))
                {
                    _problemChildren.Add(child);
                }
            }
            ReportChange();
        }

        /// <summary>
        /// Informs the helper that the memento supporter that wraps this helper has lost a child.
        /// </summary>
        /// <param name="child">The child.</param>
        public void RemoveChild(ISupportsMementos child)
        {
            if (_children.Contains(child))
                child.MementoChangeEvent -= _childChangeHandler;
            _children?.Remove(child);
            _problemChildren?.Remove(child);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the memento supporter that wraps this helper has changed.
        /// </summary>
        /// <value><c>true</c> if the memento supporter that wraps this helper has changed; otherwise, <c>false</c>.</value>
        public bool HasChanged
        {
            get
            {
                if (_problemChildren != null)
                {
                    foreach (ISupportsMementos child in _problemChildren)
                    {
                        _hasChanged |= child.HasChanged;
                    }
                }
                return _hasChanged;
            }
            set
            {
                _hasChanged = value;
            }
        }

        /// <summary>
        /// Called by the memento supporter that wraps this helper, to let it know that a change has occurred in its internal state.
        /// </summary>
        /// <param name="iss">The memento supporter which has changed.</param>
        private void ReportChange(ISupportsMementos iss)
        {
            _hasChanged = true;
            MementoChangeEvent?.Invoke(iss);
        }

        /// <summary>
        /// Called by the memento supporter that wraps this helper, to let it know that a change has occurred in its internal state.
        /// </summary>
        public void ReportChange()
        {
            _hasChanged = true;
            MementoChangeEvent?.Invoke(_iss);
        }

        /// <summary>
        /// Occurs when the memento supporter that wraps this helper has reported a change in its internal state.
        /// </summary>
        public event MementoChangeEvent MementoChangeEvent;

        /// <summary>
        /// Called by the memento supporter that wraps this helper, to let it know that a snapsot (a memento) has just been generated.
        /// </summary>
        public void ReportSnapshot()
        {
            _hasChanged = false;
        }

        /// <summary>
        /// Gets a value indicating whether the memento supporter that wraps this helper reports its own changes.
        /// </summary>
        /// <value><c>true</c> if [reports own changes]; otherwise, <c>false</c>.</value>
        public bool ReportsOwnChanges => (_wrappeeReportsOwnChanges && _problemChildren == null);
    }
}