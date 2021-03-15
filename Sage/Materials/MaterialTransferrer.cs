/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Materials.Chemistry;
using Highpoint.Sage.SimCore;
using System;
using System.Collections.Generic;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Materials
{

    /// <summary>
    /// Performs transferral of material from one mixture to another.
    /// </summary>
    public class MaterialTransferrer : IUpdater
    {

        public class TypeSpec
        {

            #region Private Fields
            private readonly MaterialType _mt;
            private readonly double _mass;
            #endregion Private Fields

            public TypeSpec(MaterialType mtype, double mass)
            {
                _mass = mass;
                _mt = mtype;
            }

            public MaterialType MaterialType
            {
                get
                {
                    return _mt;
                }
            }
            public double Mass
            {
                get
                {
                    return _mass;
                }
            }

            public static List<TypeSpec> FromMixture(Mixture exemplar)
            {
                List<TypeSpec> typeSpecs = new List<TypeSpec>();
                foreach (Substance s in exemplar.Constituents)
                {
                    typeSpecs.Add(new TypeSpec(s.MaterialType, s.Mass));
                }
                return typeSpecs;
            }
        }

        #region Private Fields
        private readonly Mixture _from;
        private readonly Mixture _to;
        private readonly List<TypeSpec> _what;
        private TimeSpan _duration;
        private readonly IModel _model;
        private long _completionKey = long.MinValue;
        private long _startTicks = long.MinValue;
        private long _endTicks = long.MinValue;
        private long _lastUpdateTicks = long.MinValue;
        private double _lastFraction = 0.0;
        private bool _inProcess = false;
        private readonly List<IDetachableEventController> _startWaiters;
        private readonly List<IDetachableEventController> _endWaiters;
        #endregion Private Fields

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialTransferrer"/> class.
        /// </summary>
        /// <param name="model">The model in which the update is to be run.</param>
        /// <param name="from">The source mixture.</param>
        /// <param name="to">The destination mixture.</param>
        /// <param name="what">The exemplar representing what is to be transferred.</param>
        /// <param name="duration">The transfer duration.</param>
        public MaterialTransferrer(IModel model, ref Mixture from, ref Mixture to, Mixture what, TimeSpan duration)
            : this(model, ref from, ref to, TypeSpec.FromMixture(what), duration) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialTransferrer"/> class.
        /// </summary>
        /// <param name="model">The model in which the update is to be run.</param>
        /// <param name="from">The source mixture.</param>
        /// <param name="to">The destination mixture.</param>
        /// <param name="typespecs">The list of typespecs representing what is to be transferred.</param>
        /// <param name="duration">The transfer duration.</param>
        public MaterialTransferrer(IModel model, ref Mixture from, ref Mixture to, List<TypeSpec> typespecs, TimeSpan duration)
        {
            _startWaiters = new List<IDetachableEventController>();
            _endWaiters = new List<IDetachableEventController>();
            _model = model;
            _from = from;
            _to = to;
            _what = typespecs;
        }

        /// <summary>
        /// Starts the transfer that this MaterialTransferrer represents.
        /// </summary>
        public void Start()
        {

            if (_completionKey != long.MinValue)
            {
                throw new ApplicationException("An already used MaterialTransferrer was asked to start a second time. This is an error.");
            }

            _startWaiters.ForEach(delegate (IDetachableEventController waiter)
            {
                waiter.Resume();
            });
            _startWaiters.Clear();
            _from.Updater = this;
            _to.Updater = this;
            _startTicks = _model.Executive.Now.Ticks;
            _lastUpdateTicks = _startTicks;
            DateTime end = _model.Executive.Now + _duration;
            _endTicks = end.Ticks;
            _completionKey = _model.Executive.RequestEvent(new ExecEventReceiver(_Update), end, 0.0, null);

        }

        /// <summary>
        /// Blocks the caller's detachable event thread until this transfer has started.
        /// </summary>
        public void BlockTilStart()
        {
            _Debug.Assert(_model.Executive.CurrentEventType == ExecEventType.Detachable);
            if (_completionKey == long.MinValue/*i.e. it has not started*/)
            {
                IDetachableEventController waiter = _model.Executive.CurrentEventController;
                _startWaiters.Add(waiter);
                waiter.Suspend();
            }
        }

        /// <summary>
        /// Blocks the caller's detachable event thread until this transfer has finished.
        /// </summary>
        public void BlockTilDone()
        {
            _Debug.Assert(_model.Executive.CurrentEventType == ExecEventType.Detachable);
            if (_completionKey > _model.Executive.Now.Ticks)
            {
                _endWaiters.Add(_model.Executive.CurrentEventController);
                _model.Executive.CurrentEventController.Suspend();
            }
        }

        private void _Update(IExecutive exec, object userData)
        {
            if (!_inProcess)
            {
                _inProcess = true;
                double thisFraction = ((double)(_model.Executive.Now.Ticks - _startTicks)) / ((double)_duration.Ticks);
                double transferFraction = thisFraction - _lastFraction;
                if (transferFraction > 0)
                {
                    foreach (TypeSpec ts in _what)
                    {
                        if (ts.Mass > 0)
                        {
                            IMaterial extract = _from.RemoveMaterial(ts.MaterialType, (ts.Mass * transferFraction));
                            _to.AddMaterial(extract);
                        }
                    }
                }

                if (_model.Executive.Now.Ticks >= _endTicks)
                {
                    _endWaiters.ForEach(delegate (IDetachableEventController waiter)
                    {
                        waiter.Resume();
                    });
                    _endWaiters.Clear();
                }

                _lastFraction = thisFraction;
                _inProcess = false;
            }
        }

        private void ReleaseWaiters(List<IDetachableEventController> waiters)
        {
            waiters.ForEach(delegate (IDetachableEventController waiter)
            {
                waiter.Resume();
            });
            waiters.Clear();
        }

        #region IUpdater Members

        /// <summary>
        /// Performs the update operation that this implementer performs.
        /// </summary>
        /// <param name="initiator">The initiator.</param>
        public void DoUpdate(IMaterial initiator)
        {
            _Update(null, null);
        }

        /// <summary>
        /// Causes this updater no longer to perform alterations on the targeted mixture. This is not implemented in this class, and will throw an exception.
        /// </summary>
        /// <param name="detachee">The detachee.</param>
        public void Detach(IMaterial detachee)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}