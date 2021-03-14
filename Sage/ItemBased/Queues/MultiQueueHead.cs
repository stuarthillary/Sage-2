/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.ItemBased.Connectors;
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.SimCore;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased.Queues
{
    /// <summary>
    /// An object that has multiple inputs and one output. When a pull occurs on the output,
    /// a selection strategy is used to 
    /// </summary>
    public class MultiQueueHead : IPortOwner
    {

        public IOutputPort[] Outputs;
        public IInputPort Input
        {
            get
            {
                return _input;
            }
        }
        private readonly SimpleInputPort _input;
        private readonly ISelectionStrategy _selStrategy = null;
        

        public MultiQueueHead(IModel model, string name, Guid guid, ArrayList queues, ISelectionStrategy selStrategy)
        { // TODO: Want to add/remove queues eventually, and use an IQueueSelectionStrategy.
            _selStrategy = selStrategy;
            selStrategy.Candidates = queues;

            _input = new SimpleInputPort(model, name, guid, this, new DataArrivalHandler(OnDataPushIn));

            Outputs = new IOutputPort[queues.Count];
            for (int i = 0; i < Outputs.GetLength(0); i++)
            {
                string portName = name + "#" + i;
                Outputs[i] = new SimpleOutputPort(model, portName, Guid.NewGuid(), this, new DataProvisionHandler(OnDataPullOut), null);
                ConnectorFactory.Connect(Outputs[i], ((Queue)queues[i]).Input);
            }
        }

        private object OnDataPullOut(IOutputPort op, object selector)
        {
            return _input.OwnerTake(selector);
        } // Forces an upstream read.
        private bool OnDataPushIn(object data, IInputPort ip)
        {
            if (data != null)
            {
                Queue queue = (Queue)_selStrategy.GetNext(null);
                IOutputPort outPort = (IOutputPort)queue.Input.Peer;
                //_Debug.WriteLine("Arbitrarily putting data to " + queue.ToString());
                return ((SimpleOutputPort)outPort).OwnerPut(data); // Cast is okay - it's my port.
            }
            else
            {
                return false;
            }
        }

        #region Add/Removal of Queues. Currently OOC.
        /*
         
        private ArrayList _queues = new ArrayList();

		public void AddQueue( Queue queue ) {
			if ( m_queues.Count < Outputs.GetLength(0) ) {
				m_queues.Add(queue);
				for ( int i = 0 ; i < Outputs.GetLength(0) ; i++ ) {
					if ( Outputs[i].Connector == null ) {
						m_connFactory.Connect(Outputs[i],queue.Input);
						if ( QueueAddedEvent != null ) QueueAddedEvent(queue);
						break;
					}
				}
			} else {
				throw new ApplicationException("Tried to add too many ports (" + m_queues.Count+1 + ") to a MultiQueueHead.");
			}
		}

		public ArrayList Queues { get { return ArrayList.ReadOnly(m_queues); } }

		public ISelectionStrategy SelectionStrategy {
			get { return m_selStrategy; }
			set {
				if ( m_selStrategy != null ) m_selStrategy.Unregister(this);
				m_selStrategy = value;
				m_selStrategy.Register(this);
			}
		}*/
        #endregion

        #region IPortOwner Implementation
        /// <summary>
        /// The PortSet object to which this IPortOwner delegates.
        /// </summary>
        private readonly PortSet _ports = new PortSet();
        /// <summary>
        /// Registers a port with this IPortOwner
        /// </summary>
        /// <param name="port">The port that this IPortOwner add.</param>
        public void AddPort(IPort port)
        {
            _ports.AddPort(port);
        }

        /// <summary>
        /// Adds a port to this object's port set in the specified role or channel.
        /// </summary>
        /// <param name="channel">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
        /// <returns>The newly-created port. Can return null if this is not supported.</returns>
        public IPort AddPort(string channel)
        {
            return null; /*Implement AddPort(string channel); */
        }

        /// <summary>
        /// Adds a port to this object's port set in the specified role or channel.
        /// </summary>
        /// <param name="channelTypeName">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
        /// <param name="guid">The GUID to be assigned to the new port.</param>
        /// <returns>The newly-created port. Can return null if this is not supported.</returns>
        public IPort AddPort(string channelTypeName, Guid guid)
        {
            return null; /*Implement AddPort(string channel); */
        }

        /// <summary>
        /// Gets the names of supported port channels.
        /// </summary>
        /// <value>The supported channels.</value>
        public List<IPortChannelInfo> SupportedChannelInfo
        {
            get
            {
                return GeneralPortChannelInfo.StdInputAndOutput;
            }
        }

        /// <summary>
        /// Unregisters a port from this IPortOwner.
        /// </summary>
        /// <param name="port">The port being removed.</param>
        public void RemovePort(IPort port)
        {
            _ports.RemovePort(port);
        }
        /// <summary>
        /// Unregisters all ports that this IPortOwner knows to be its own.
        /// </summary>
        public void ClearPorts()
        {
            _ports.ClearPorts();
        }
        /// <summary>
        /// The public property that is the PortSet this IPortOwner owns.
        /// </summary>
        public IPortSet Ports
        {
            get
            {
                return _ports;
            }
        }
        #endregion

    }
}