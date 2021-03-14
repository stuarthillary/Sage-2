/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Materials.Chemistry;
using Highpoint.Sage.SimCore;
using System;
using System.Collections.Generic;

namespace Highpoint.Sage.Materials
{

    /// <summary>
    /// A Dispensary holds a Mixture that supports infusion of materials (i.e. mixtures or substances) and
    /// getters that specify the quantity they want. If there is not enough, then the getter is suspended,
    /// and resumed when there is enough material there to satisfy the getter's request. The purpose is to
    /// allow suppliers to dump various quantities at varying times, of materials into it, and to support 
    /// consumers that say, "Give me 100 kg of substance X." If there are 100 kg of X already, then it is
    /// dispensed to the consumer. If there are not, then the consumer is blocked, and then (potentially)
    /// unblocked later when there *are* 100 kg. If there are never 100 kg, though, the consumer will never
    /// unblock.
    /// </summary>
    public class Dispensary
    {


        /// <summary>
        /// This class exists solely so that the Dispensary can assume there is always a getter or a putter waiting. 
        /// The dummy idec, though, acts as though (if there isn't actually a real one) the waiting getter or putter
        /// has, or needs, zero kilograms.
        /// </summary>
        private class DummyIdec : IDetachableEventController
        {

            #region IDetachableEventController Members

            public void Suspend()
            {
            }

            public void Resume()
            {
            }

            public void Resume(double overridePriority)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            public void SuspendUntil(DateTime when)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            public System.Diagnostics.StackTrace SuspendedStackTrace
            {
                get
                {
                    throw new Exception("The method or operation is not implemented.");
                }
            }

            public bool IsWaiting()
            {
                throw new Exception("The method or operation is not implemented.");
            }

            public IExecEvent RootEvent
            {
                get
                {
                    throw new Exception("The method or operation is not implemented.");
                }
            }

            public void SetAbortHandler(DetachableEventAbortHandler handler, params object[] args)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            public void ClearAbortHandler()
            {
                throw new Exception("The method or operation is not implemented.");
            }

            public void FireAbortHandler()
            {
                throw new Exception("The method or operation is not implemented.");
            }

            public void SuspendFor(TimeSpan howLong)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            #endregion
        }

        #region Private Fields
        private static readonly IDetachableEventController dummyIdec = new DummyIdec();
        private IDetachableEventController _getProcessor;
        private readonly List<IDetachableEventController> _waiters;
        private readonly IExecutive _executive;
        private long _getFlushEventId = -1;
        #endregion Private Fields

        public Dispensary(IExecutive executive) : this(executive, new Mixture()) { }

        public Dispensary(IExecutive executive, Mixture mixture)
        {
            _executive = executive;
            _getProcessor = dummyIdec;
            _waiters = new List<IDetachableEventController>();
            PeekMixture = mixture;
            executive.ExecutiveStarted += delegate
            {
                _waiters.Clear();
                PeekMixture.Clear();
            };
        }

        public void Put(IMaterial material)
        {
            PeekMixture.AddMaterial(material);
            //m_executive.CurrentEventController.SuspendUntil(m_executive.Now + duration);
            if (_waiters.Count > 0)
            {
                ScheduleProcessingOfGetters();
            }
        }

        private void ScheduleProcessingOfGetters()
        {
            if (_getFlushEventId == -1)
            {
                _getFlushEventId = _executive.RequestEvent(ProcessGetters, _executive.Now, 0.0, null, ExecEventType.Detachable);
            }
        }

        private void ProcessGetters(IExecutive exec, object userData)
        {
            _getProcessor = exec.CurrentEventController;
            IDetachableEventController waiter = null;
            while (_waiters.Count > 0 && _waiters[0] != waiter)
            {
                waiter = _waiters[0];
                waiter.Resume();
                _getProcessor.Suspend();
            }
            _getFlushEventId = -1;
            _getProcessor = dummyIdec;
        }

        public Mixture Get(double kilograms)
        {
            if (_waiters.Count > 0 || PeekMixture.Mass < kilograms)
            {
                _waiters.Add(_executive.CurrentEventController);
                do
                {
                    _getProcessor.Resume();
                    _executive.CurrentEventController.Suspend();
                } while (PeekMixture.Mass < kilograms);
                _waiters.RemoveAt(0);
                _getProcessor.Resume();
            }
            return (Mixture)PeekMixture.RemoveMaterial(kilograms);
        }

        public Mixture PeekMixture
        {
            get;
        }
    }
}
