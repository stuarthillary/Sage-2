/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.ItemBased.Ports
{
    public class InputPortManager : PortManager
    {

        #region Behavioral Enumerations
        public enum DataReadSource
        {
            Buffer, 
            BufferOrPull,
            Pull
        }
        public enum DataWriteAction
        {
            Ignore,
            Store,
            StoreAndInvalidate,
            Push
        }
        #endregion

        #region Private fields
        private readonly SimpleInputPort _sip;
        private DataReadSource _readSource;
        private DataWriteAction _writeAction;
        private List<OutputPortManager> _dependents = null;
        private object _buffer = null;
        #endregion

        #region Constructors
        public InputPortManager(SimpleInputPort sip)
            : this(sip, DataWriteAction.StoreAndInvalidate, BufferPersistence.UntilWrite, DataReadSource.BufferOrPull) { }

        public InputPortManager(SimpleInputPort sip, DataWriteAction writeResponse, BufferPersistence bufferPersistence, DataReadSource readSource)
        {
            base.bufferPersistence = bufferPersistence;
            _sip = sip;
            _readSource = readSource;
            _writeAction = writeResponse;
            _sip.PutHandler = new DataArrivalHandler(PutHandler);
        }
        #endregion

        public DataReadSource ReadSource
        {
            get
            {
                return _readSource;
            }
            set
            {
                _readSource = value;
            }
        }

        public DataWriteAction WriteAction
        {
            get
            {
                return _writeAction;
            }
            set
            {
                _writeAction = value;
            }
        }

        public void SetDependents(params OutputPortManager[] dependents)
        {
            if (!(dependents.Length == 0 || _dependents == null || _dependents.Count == 0))
            {
                string ownerBlock = _sip.Owner is IHasIdentity ? ((IHasIdentity)_sip.Owner).Name : "a block";
                string msg = string.Format("Calling SetDependents on {1} clears {0} existent dependents. Call SetDependents(); first without dependents to indicate intentional clearance.",
                _dependents.Count, ownerBlock);
                Debug.Assert(false, msg);
            }
            _dependents = new List<OutputPortManager>(dependents);
            foreach (OutputPortManager opm in _dependents)
            {
                opm.AddPeers(_dependents.ToArray());
            }
        }

        public object Value
        {
            get
            {
                if (Diagnostics)
                    _Debug.WriteLine(string.Format("Block {0}, port {1} being asked to give its value.", ((IHasIdentity)_sip.Owner).Name, _sip.Name));
                object retval;
                switch (_readSource)
                {
                    case DataReadSource.Buffer:
                        retval = _buffer;
                        break;
                    case DataReadSource.BufferOrPull:
                        if (_buffer != null)
                        {
                            retval = _buffer;
                        }
                        else
                        {
                            // Pull into the buffer.
                            _buffer = _sip.OwnerTake(null);
                            retval = _buffer;
                        }
                        break;
                    case DataReadSource.Pull:
                        _buffer = _sip.OwnerTake(null);
                        retval = _buffer;
                        break;
                    default:
                        throw new ApplicationException(String.Format("Unhandled value {0} of InputSource enumeration in an InputPortManager.", _readSource));
                }

                switch (bufferPersistence)
                {
                    case BufferPersistence.None:
                        _buffer = null;
                        break;
                    case BufferPersistence.UntilRead:
                        _buffer = null;
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
                switch (_writeAction)
                {
                    case DataWriteAction.Ignore:
                        break;
                    case DataWriteAction.Store:
                        _buffer = value;
                        break;
                    case DataWriteAction.StoreAndInvalidate:
                        _buffer = value;
                        if (_dependents == null)
                        {
                            // TODO: Make this universal. (I.E. Require developer always to set values.)
                            throw new ApplicationException(string.Format("Block type {0} forgot to set dependents for input port {1}.", _sip.Owner.GetType().Name, _sip.Name));
                        }
                        _dependents.ForEach(n => n.BufferValid = false);
                        break;
                    case DataWriteAction.Push:
                        _buffer = value;
                        if (_dependents == null)
                        {
                            throw new ApplicationException(string.Format("Push-on-write specified on a port with no dependents. Specify dependents for {0}", _sip.Name));
                        }
                        else
                        {
                            _dependents.ForEach(n => n.Push());
                        }
                        break;
                    default:
                        break;
                }

            }
        }

        public override void ClearBuffer()
        {
            _buffer = null;
        }

        public override bool IsPortConnected
        {
            get
            {
                return _sip.Connector != null;
            }
        }

        private bool PutHandler(object data, IInputPort port)
        {
            Value = data;
            return true;
        }
    }
}
