/* This source code licensed under the GNU Affero General Public License */
using System;
using _Debug = System.Diagnostics.Debug;
using System.Collections;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.ItemBased.Ports;
using System.Collections.Generic;

namespace Highpoint.Sage.ItemBased.Channels
{
	/// <summary>
	/// Summary description for FixedRateChannel.
	/// </summary>
	public class FixedRateChannel : IPortOwner {
		
		#region >>> Private Variables <<<
		private readonly SimpleInputPort _entry;
		private readonly SimpleOutputPort _exit;
		private readonly IExecutive _exec;
		private readonly double _capacity;
		private DateTime _lastExitArrivalTime;
		private DateTime _lastEntryAcceptanceTime;
		private TimeSpan _entryPeriod;
		private TimeSpan _transitPeriod;
		private readonly Queue _queue;
		private readonly ExecEventReceiver _dequeueEventHandler;
		#endregion

        /// <summary>
        /// Creates a channel for which the transit rate is fixed, and which can hold a specified
        /// capacity of payload.
        /// </summary>
        /// <param name="model">The model in which this FixedRateChannel exists.</param>
        /// <param name="name">The name of this FixedRateChannel.</param>
        /// <param name="guid">The GUID of this FixedRateChannel.</param>
        /// <param name="exec">The executive that controls this channel.</param>
        /// <param name="transitPeriod">How long it takes an object to transit the channel.</param>
        /// <param name="capacity">How many objects the channel can hold.</param>
		public FixedRateChannel(IModel model, string name, Guid guid, IExecutive exec, TimeSpan transitPeriod, double capacity) {
			_exec = exec;
			_transitPeriod = transitPeriod;
			_capacity = capacity;
			_entryPeriod = TimeSpan.FromTicks((long)((double)_transitPeriod.Ticks/_capacity));
			_queue = new Queue();
			_entry = new SimpleInputPort(model, "Entry", Guid.NewGuid(), this, new DataArrivalHandler(OnEntryAttempted));
            _exit = new SimpleOutputPort(model, "Exit", Guid.NewGuid(), this, new DataProvisionHandler(CantTakeFromChannel), new DataProvisionHandler(CantPeekFromChannel));
            //m_ports.AddPort(m_entry); <-- Done in port's ctor.
            //m_ports.AddPort(m_exit); <-- Done in port's ctor.
			_dequeueEventHandler = new ExecEventReceiver(DequeueEventHandler);
		}

		private bool OnEntryAttempted(object obj, IInputPort ip){
			if ( _queue.Count == 0 ) return AcceptEntry(obj);
			if ( _exec.Now-_lastEntryAcceptanceTime >= _entryPeriod ) return AcceptEntry(obj);
			return false;			
		}
		private bool AcceptEntry(object obj){
			TimeSpan forwardBuffer;
			Bin bin;
			if ( _queue.Count == 0 ) {
				forwardBuffer  = _transitPeriod;
				bin = new Bin(obj,1.0,forwardBuffer);
				ScheduleDequeueEvent(bin);
			} else {
				forwardBuffer = TimeSpan.FromTicks(Math.Max(_transitPeriod.Ticks,(_exec.Now.Ticks - _lastEntryAcceptanceTime.Ticks)));
				bin = new Bin(obj,1.0,forwardBuffer);
			}
			_lastEntryAcceptanceTime = _exec.Now;
			return true;
		}
		private void ScheduleDequeueEvent(Bin bin){
			_exec.RequestEvent(_dequeueEventHandler,_exec.Now+bin.ForwardBuffer,0.0,bin);
		}
		private void DequeueEventHandler(IExecutive exec, object bin){
			_lastExitArrivalTime = exec.Now;
			_exit.OwnerPut(((Bin)bin).Payload);
		}
		private object CantTakeFromChannel(IOutputPort op, object selector){return null;}
        private object CantPeekFromChannel(IOutputPort op, object selector){ return null;}
		
		/// <summary>
		/// The input port (i.e. the on-ramp).
		/// </summary>
		public IInputPort Entry { get { return _entry; } }
		/// <summary>
		/// The output port (i.e. the off-ramp).
		/// </summary>
		public IOutputPort Exit { get { return _exit;  } }

		#region IPortOwner Implementation
		/// <summary>
		/// The PortSet object to which this IPortOwner delegates.
		/// </summary>
		private readonly PortSet _ports = new PortSet();

		/// <summary>
		/// Registers a port with this IPortOwner
		/// </summary>
		/// <param name="port">The port that is to be added to this IPortOwner.</param>
		public void AddPort(IPort port) {_ports.AddPort(port);}

        /// <summary>
        /// Adds a port to this object's port set in the specified role or channel.
        /// </summary>
        /// <param name="channel">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
        /// <returns>The newly-created port. Can return null if this is not supported.</returns>
        public IPort AddPort(string channel) { return null; /*Implement AddPort(string channel); */}

        /// <summary>
        /// Adds a port to this object's port set in the specified role or channel.
        /// </summary>
        /// <param name="channelTypeName">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
        /// <param name="guid">The GUID to be assigned to the new port.</param>
        /// <returns>The newly-created port. Can return null if this is not supported.</returns>
        public IPort AddPort(string channelTypeName, Guid guid) { return null; /*Implement AddPort(string channel); */}

        /// <summary>
        /// Gets the names of supported port channels.
        /// </summary>
        /// <value>The supported channels.</value>
        public List<IPortChannelInfo> SupportedChannelInfo { get { return GeneralPortChannelInfo.StdInputAndOutput; } }

        /// <summary>
        /// Unregisters a port from this IPortOwner.
        /// </summary>
        /// <param name="port">The port to be removed.</param>
		public void RemovePort(IPort port){ _ports.RemovePort(port); }
		/// <summary>
		/// Unregisters all ports that this IPortOwner knows to be its own.
		/// </summary>
		public void ClearPorts(){_ports.ClearPorts();}
		/// <summary>
		/// The public property that is the PortSet this IPortOwner owns.
		/// </summary>
		public IPortSet Ports { get { return _ports; } }
		#endregion

		/// <summary>
		/// A channel contains a series of bins. 
		/// </summary>
		private struct Bin {
			private readonly object _payload;
			private readonly double _capacity;
			private TimeSpan _forwardBuffer;
			public Bin(object payload, double capacity, TimeSpan forwardBuffer){
				_payload = payload;
				_capacity = capacity;
				_forwardBuffer = forwardBuffer;
			}
			public object Payload { get { return _payload; } }
			public double Capacity { get { return _capacity; } }
			public TimeSpan ForwardBuffer { get { return _forwardBuffer; } }
		}
	}
}
