/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
// ReSharper disable RedundantDefaultMemberInitializer

namespace Highpoint.Sage.SimCore
{
    internal delegate void FilterMethod( ref SortedList events );

    internal class ExecEventRemover
    {
        private readonly IExecEventSelector _ees = null;
        private readonly long _eventId;
        private readonly object _target = null;
        private readonly FilterMethod _filterMethod;

        public ExecEventRemover(IExecEventSelector ees)
        {
            _ees = ees;
            _filterMethod = new FilterMethod(FilterOnFullData);
        }
        public ExecEventRemover(long eventId)
        {
            _eventId = eventId;
            _filterMethod = new FilterMethod(FilterOnEventId);
        }

        public ExecEventRemover(Delegate target)
        {
            _target = target;
            _filterMethod = new FilterMethod(FilterOnDelegateAll);
        }

        public ExecEventRemover(object target)
        {
            _target = target;
            _filterMethod = new FilterMethod(FilterOnTargetAll);
        }

        public void Filter(ref SortedList events)
        {
            _filterMethod(ref events);
        }

        private void FilterOnFullData(ref SortedList events)
        {

            IList keyList = events.GetKeyList();
            ExecEvent ee;
            for (int i = keyList.Count - 1; i >= 0; i--)
            {
                ee = (ExecEvent)keyList[i];
                if (_ees.SelectThisEvent(ee.ExecEventReceiver, ee.When, ee.Priority, ee.UserData, ee.EventType))
                {
                    events.RemoveAt(i);
                }
            }
        }

        private void FilterOnEventId(ref SortedList events)
        {
            if (events.ContainsValue(_eventId))
            {
                // Need to remove the entry that has the value of m_eventID.
                events.RemoveAt(events.IndexOfValue(_eventId));
            }
            else
            {
                throw new ApplicationException("Attempted to remove an event from the executive by its event ID (" + _eventId + "), where that event ID was not in the event list.");
            }
        }

        private void FilterOnTarget(ref SortedList events)
        {

            object eventTarget = null;
            foreach (ExecEvent ee in events.Keys)
            {

                if (ee.ExecEventReceiver.Target is DetachableEvent)
                {
                    ExecEventReceiver eer = ((ExecEvent)((DetachableEvent)ee.ExecEventReceiver.Target).RootEvent).ExecEventReceiver;
                    eventTarget = eer.Target;
                }
                else
                {
                    eventTarget = ee.ExecEventReceiver.Target;
                }

                // We're comparing at the object level - we can't compare any higher, since we
                // have no control over what kinds of objects we may be comparing. To avoid an
                // invalid cast exception, we treat them both as objects.
                if (Equals(eventTarget, _target))
                {
                    //_Debug.WriteLine("Sure would like to remove " + ee.ToString());
                    int indexOfKey = events.IndexOfKey(ee);
                    events.RemoveAt(indexOfKey);
                    break;
                }
            }
        }

        private void FilterOnDelegate(ref SortedList events)
        {

            object eventTarget = null;
            foreach (ExecEvent ee in events.Keys)
            {

                if (ee.ExecEventReceiver.Target is DetachableEvent)
                {
                    ExecEventReceiver eer = ((ExecEvent)((DetachableEvent)ee.ExecEventReceiver.Target).RootEvent).ExecEventReceiver;
                    eventTarget = eer;
                }
                else
                {
                    eventTarget = ee.ExecEventReceiver;
                }

                if (((Delegate)eventTarget).Equals((Delegate)_target))
                {
                    //_Debug.WriteLine("Sure would like to remove " + ee.ToString());
                    int indexOfKey = events.IndexOfKey(ee);
                    events.RemoveAt(indexOfKey);
                    break;
                }
            }
        }

        private void FilterOnTargetAll(ref SortedList events)
        {

            ArrayList eventsToDelete = new ArrayList();																// AEL
            Type soughtTargetType = _target.GetType();
            Type eventTargetType = null;

            IList keyList = events.GetKeyList();
            for (int i = keyList.Count - 1; i >= 0; i--)
            {
                ExecEvent ee = (ExecEvent)keyList[i];

                if (ee.ExecEventReceiver.Target is DetachableEvent)
                {
                    ExecEventReceiver eer = ((ExecEvent)((DetachableEvent)ee.ExecEventReceiver.Target).RootEvent).ExecEventReceiver;
                    eventTargetType = eer.Target.GetType();
                }
                else
                {
                    // The callback could be static, so if it is, then we need the targetType a different way.
                    eventTargetType = ee.ExecEventReceiver.Target == null ? ee.ExecEventReceiver.Method.ReflectedType : ee.ExecEventReceiver.Target.GetType();
                }

                // We're comparing at the object level - we can't compare any higher, since we
                // have no control over what kinds of objects we may be comparing. To avoid an
                // invalid cast exception, we treat them both as objects.
                //if ( object.Equals(eventTarget,m_target) ) {
                if (eventTargetType.Equals(soughtTargetType))
                {
                    events.RemoveAt(i);
                }
            }
        }

        private void FilterOnDelegateAll(ref SortedList events)
        {

            object eventTarget = null;
            ExecEvent ee;
            DetachableEvent de;
            IList keyList = events.GetKeyList();
            for (int i = keyList.Count - 1; i >= 0; i--)
            {
                ee = (ExecEvent)keyList[i];
                de = ee.ExecEventReceiver.Target as DetachableEvent;
                if (de != null)
                {
                    eventTarget = de.RootEvent.ExecEventReceiver.Target;
                }
                else
                {
                    eventTarget = ee.ExecEventReceiver;
                }

                if (((Delegate)eventTarget).Equals((Delegate)_target))
                {
                    events.RemoveAt(i);
                }
            }
        }
    }
}
