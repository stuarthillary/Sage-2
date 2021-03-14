/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using Highpoint.Sage.Utility;
using System;
using System.Collections.Generic;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.ItemBased.Ports
{
    public class OutputPortManager : PortManager
    {

        #region Private fields
        private readonly SimpleOutputPort _sop;
        private object _buffer;
        private bool _bufferValid;
        private Action _valueComputeMethod;
        private readonly List<OutputPortManager> _peers;
        #endregion

        #region Constructors
        public OutputPortManager(SimpleOutputPort sop) : this(sop, BufferPersistence.UntilWrite) { }

        public OutputPortManager(SimpleOutputPort sop, BufferPersistence obp)
        {
            _sop = sop;
            _sop.PeekHandler = new DataProvisionHandler(OnPeek);
            _sop.TakeHandler = new DataProvisionHandler(OnTake);
            _buffer = null;
            _bufferValid = false;
            _valueComputeMethod = null;
            bufferPersistence = obp;
            _peers = new List<OutputPortManager>();
        }
        #endregion

        public Action ComputeFunction
        {
            get
            {
                if (_valueComputeMethod == null)
                    throw new ApplicationException(string.Format("Unspecified ComputeFunction on port {0} of {1}.", _sop.Name, _sop.Owner));
                return _valueComputeMethod;
            }
            set
            {
                _valueComputeMethod = value;
            }
        }

        public void Push(bool recompute = true)
        {
            if (recompute || !BufferValid)
            {
                ComputeFunction();
            }
            _sop.OwnerPut(Buffer);
        }

        public object OnPeek(IOutputPort iop, object data)
        {
            return _buffer;
        }

        public object OnTake(IOutputPort iop, object data)
        {
            return Buffer;
        }

        public bool BufferValid
        {
            get
            {
                return _bufferValid;
            }
            set
            {
                _bufferValid = value;
                if (!_bufferValid)
                    _buffer = null;
            }
        }

        public object Buffer
        {
            get
            {
                if (Diagnostics)
                    _Debug.WriteLine(string.Format("Block {0}, port {1} being asked to give its value - the buffer {2} valid.", ((IHasIdentity)_sop.Owner).Name, _sop.Name, BufferValid ? "is" : "is not"));
                if (!BufferValid)
                {
                    try
                    {
                        object oldValue = _buffer;
                        ComputeFunction();
                        object newValue = _buffer;
                        if (ValueHasChanged(oldValue, newValue))
                            PushAllBut(this);

                    }
                    catch (NullReferenceException nre)
                    {
                        string ownerName = _sop.Owner as IHasIdentity != null ? (_sop.Owner as IHasIdentity).Name : "<unknown block>";
                        List<string> problemPorts = new List<string>();
                        foreach (SimpleInputPort sip in _sop.Owner.Ports.Inputs)
                        {
                            if (sip.OwnerTake(null) == null)
                            {
                                string peerPortName = sip.Peer.Name;
                                string peerOwnerName = sip.Peer.Owner as IHasIdentity != null ? (sip.Peer.Owner as IHasIdentity).Name : "<unknown block>";
                                problemPorts.Add(string.Format("{0}, connected upstream to port \"{1}\" on block \"{2}\"", sip.Name, peerPortName, peerOwnerName));
                            }
                        }

                        string msg = string.Format("The block \"{0}\" was unable to complete its compute function, probably because an upstream source was unable (or unrequested) to deliver a value in response to a pull. Suspect ports are {1}.",
                            ownerName, StringOperations.ToCommasAndAndedList(problemPorts));

                        _sop.Model.AddError(new GenericModelError("Compute function failure", msg, _sop.Owner, StringOperations.ToCommasAndAndedList(problemPorts)));
                        throw new ApplicationException(msg, nre);
                    }
                }
                object retval = _buffer;
                if (Diagnostics)
                    _Debug.WriteLine(string.Format("Block {0}, port {1} provided value {2}", ((IHasIdentity)_sop.Owner).Name, _sop.Name, retval));
                switch (bufferPersistence)
                {
                    case BufferPersistence.None:
                    case BufferPersistence.UntilRead:
                        _buffer = null;
                        BufferValid = false;
                        break;
                    case BufferPersistence.UntilWrite:
                        break;
                    default:
                        break;
                }

                return retval;
            }
            set
            {
                _buffer = value;
                BufferValid = true;
            }
        }

        public override bool IsPortConnected
        {
            get
            {
                return _sop.Connector != null;
            }
        }

        internal void AddPeers(params OutputPortManager[] opms)
        {
            foreach (OutputPortManager opm in opms)
            {
                if (!_peers.Contains(opm) && opm != this)
                    _peers.Add(opm);
            }
        }

        private void PushAllBut(OutputPortManager instigator)
        {
            foreach (OutputPortManager peer in _peers)
            {
                if (peer != instigator)
                    peer.Push(false);
            }
        }

        private bool ValueHasChanged(object oldValue, object newValue)
        {
            if (oldValue == newValue)
                return false;
            if (oldValue == null && newValue != null)
                return true;
            if (newValue == null && oldValue != null)
                return true;
            return !oldValue.Equals(newValue);
        }

        public override void ClearBuffer()
        {
            _buffer = null;
            BufferValid = false;
        }

    }
}
