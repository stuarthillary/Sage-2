/* This source code licensed under the GNU Affero General Public License */

using System.Collections;

namespace Highpoint.Sage.Resources
{
    /// <summary>
    /// A SimpleAccessManager is made a part of the resource acquisition protocol that is
    /// embodied in all resource managers. When a resource manager is aware of an access
    /// manager, it asks that access manager if any resource request is grantable
    /// before it even allows the resource request to score the available resources. Therefore,
    /// an access manager uses Access Regulators to prevent resource requests from being granted
    /// in certain cases.
    /// <para></para>
    /// A SimpleAccessManager manages a single AccessRegulator that it applies across all
    /// resources that are presented to it, or it manages a stack of AccessRegulators that
    /// are applied to specified resources.
    /// <para></para>
    /// NOTE: If an AccessManager has a default regulator as well as resource-specific ones, the
    /// resource-specific ones take precedence.
    /// </summary>
    public class SimpleAccessManager : IAccessManager
    {

        #region Private Fields

        private readonly Hashtable _monitoredObjects;
        private readonly bool _autoDeleteEmptyStacks;
        private Stack _defaultAccessRegulators;

        #endregion

        /// <summary>
        /// Creates an access manager that removes resource-specific stacks of regulators once they
        /// are empty.
        /// </summary>
        public SimpleAccessManager() : this(true) { }
        /// <summary>
        /// See the default ctor - this ctor allows the developer to decide if they want to remove any
        /// stack that is assigned to a specific resource once it is empty. One might set this arg to
        /// false if there will be many adds &amp; removes of regulators, and it is expected that the stack
        /// will empty and refill often.
        /// </summary>
        /// <param name="autoDeleteEmptyStacks">True if you want the SimpleAccessManager to perform clean up.</param>
        public SimpleAccessManager(bool autoDeleteEmptyStacks)
        {
            _autoDeleteEmptyStacks = autoDeleteEmptyStacks;
            _monitoredObjects = new Hashtable();
            _defaultAccessRegulators = new Stack();
        }

        /// <summary>
        /// Pushes an access regulator onto the stack that is associated with a particular resource, or
        /// the default stack, if no resource is specified.
        /// </summary>
        /// <param name="accReg">Access Regulator to be pushed.</param>
        /// <param name="subject">The resource to which this regulator is to apply, or null, if it applies to all of them.</param>
        public void PushAccessRegulator(IAccessRegulator accReg, IResource subject)
        {
            if (subject == null)
            {
                if (_defaultAccessRegulators == null)
                    _defaultAccessRegulators = new Stack();
                _defaultAccessRegulators.Push(accReg);
            }
            else
            {
                Stack stack = (Stack)_monitoredObjects[subject];
                if (stack == null)
                {
                    stack = new Stack();
                    _monitoredObjects.Add(subject, stack);
                }
                stack.Push(accReg);
            }
        }

        /// <summary>
        /// Pops the top access regulator from the stack associated with the specified resource, or from the
        /// default stack if subject is set as null.
        /// </summary>
        /// <param name="subject">The resource to be regulated, or null if all are to be regulated.</param>
        /// <returns>The AccessRegulator being popped, or null, if the stack was empty.</returns>
        public IAccessRegulator PopAccessRegulator(IResource subject)
        {
            IAccessRegulator retval = null;
            if (subject == null)
            {
                retval = (IAccessRegulator)_defaultAccessRegulators.Pop();
                if (_defaultAccessRegulators.Count == 0 && _autoDeleteEmptyStacks)
                    _defaultAccessRegulators = null;
            }
            else
            {
                Stack stack = (Stack)_monitoredObjects[subject];
                if (stack != null)
                {
                    retval = (IAccessRegulator)stack.Pop();
                    if (_autoDeleteEmptyStacks && stack.Count == 0)
                        _monitoredObjects.Remove(subject);
                }
            }
            return retval;
        }

        /// <summary>
        /// Returns true if the given subject can be acquired using the presented key.
        /// </summary>
        /// <param name="subject">The resource whose acquisition is being queried.</param>
        /// <param name="usingKey">The key that is to be presented by the prospective acquirer.</param>
        /// <returns>True if the acquire will be allowed, false if not.</returns>
        public bool CanAcquire(object subject, object usingKey)
        {
            Stack myStack = (Stack)_monitoredObjects[subject];
            if (myStack != null)
            {
                IAccessRegulator iar = (IAccessRegulator)myStack.Peek();
                return (iar == null || iar.CanAcquire(subject, usingKey));
            }
            else
            {
                if (_defaultAccessRegulators == null || _defaultAccessRegulators.Count == 0)
                    return true;
                IAccessRegulator iar = (IAccessRegulator)_defaultAccessRegulators.Peek();
                return iar.CanAcquire(subject, usingKey);
            }
        }
    }
}
