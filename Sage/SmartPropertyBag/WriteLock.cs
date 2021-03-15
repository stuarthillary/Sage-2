/* This source code licensed under the GNU Affero General Public License */

using System.Collections;
using System.Diagnostics;
// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.SimCore
{

    /// <summary>
    /// Fires when an object that can be made read-only, changes its writability status.
    /// </summary>
    /// <param name="newWritableState">true if the object is now writable.</param>
    public delegate void WritabilityChangeEvent(bool newWritableState);

    /// <summary>
    /// A class that manages the details of nestable write locking - that is, a parent that is write-locked implies that its children are thereby also write-locked.
    /// </summary>
    public class WriteLock : IHasWriteLock
    {

        #region Private Fields

        private bool _writable;
        private readonly ArrayList _children;
        private string _whereApplied;
        private static readonly bool _locationTracingEnabled = Diagnostics.DiagnosticAids.Diagnostics("WriteLockTracing");

        #endregion 

        /// <summary>
        /// Creates a new instance of the <see cref="T:WriteLock"/> class.
        /// </summary>
        /// <param name="initiallyWritable">if set to <c>true</c> [initially writable].</param>
        public WriteLock(bool initiallyWritable)
        {
            _writable = initiallyWritable;
            _children = new ArrayList();
            if (!_locationTracingEnabled)
                _whereApplied = _tracing_Off_Msg;
        }

        /// <summary>
        /// Fires when the object that this lock is overseeing, changes its writability status.
        /// </summary>
        public event WritabilityChangeEvent WritabilityChanged;

        /// <summary>
        /// Gets a value indicating whether this instance is currently writable.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is writable; otherwise, <c>false</c>.
        /// </value>
        public bool IsWritable => _writable;

        /// <summary>
        /// Sets the value indicating whether this instance is currently writable.
        /// </summary>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        public void SetWritable(bool writable)
        {
            if (writable == _writable)
                return;
            if (_locationTracingEnabled)
            {
                if (writable)
                {
                    _whereApplied = null;
                }
                else
                {
                    StackTrace st = new StackTrace(true);
                    _whereApplied = "";
                    for (int i = st.FrameCount - 1; i > 0; i--)
                    {
                        _whereApplied += st.GetFrame(i).ToString();
                    }
                }
            }
            _writable = writable;
            WritabilityChanged?.Invoke(_writable);
            foreach (var obj in _children)
            {
                ((WriteLock)obj).SetWritable(writable);
            }
        }
        /// <summary>
        /// Gets the location in a hierarchy of write-locked objects where the write-lock was applied.
        /// </summary>
        /// <value>The where applied.</value>
        public string WhereApplied => _whereApplied;

        /// <summary>
        /// Adds a dependent child object to this WriteLock.
        /// </summary>
        /// <param name="child">The child.</param>
        public void AddChild(WriteLock child)
        {
            _children.Add(child);
        }
        /// <summary>
        /// Removes a dependent child object from this WriteLock.
        /// </summary>
        /// <param name="child">The child.</param>
        public void RemoveChild(WriteLock child)
        {
            _children.Remove(child);
        }
        /// <summary>
        /// Clears the children from this WriteLock.
        /// </summary>
        public void ClearChildren()
        {
            _children.Clear();
        }

        private static readonly string _tracing_Off_Msg =
            @"[WriteLock tracing is off. Turn it on with an entry in the AppConfig file diagnostics section that looks like\r\n		<add key=""WriteLockTracing""					value=""true"" />";

    }
}