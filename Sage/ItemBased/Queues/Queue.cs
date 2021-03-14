/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.SimCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Highpoint.Sage.ItemBased.Queues
{

    /// <summary>
    ///
    /// </summary>
	public class Queue : IQueue
    {

        #region Member Variables
        private System.Collections.Queue _queue;
        private SimpleInputPort _input;
        private SimpleOutputPort _output;
        private int _max;
        private IModel _model;
        private string _name = String.Empty;
        private string _description = String.Empty;
        private Guid _guid = Guid.Empty;
        #endregion Member Variables

        /// <summary>
        /// Initializes a new instance of the <see cref="Queue"/> class.
        /// </summary>
        /// <param name="model">The model in which this queue exists.</param>
        /// <param name="name">The name of this queue.</param>
        /// <param name="guid">The GUID of this queue.</param>
		public Queue(IModel model, string name, Guid guid) : this(model, name, guid, int.MaxValue) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Queue"/> class.
        /// </summary>
        /// <param name="model">The model in which this queue exists.</param>
        /// <param name="name">The name of this queue.</param>
        /// <param name="guid">The GUID of this queue.</param>
        /// <param name="max">The maximum number of items that can be held in this queue.</param>
		public Queue(IModel model, string name, Guid guid, int max)
        {
            InitializeIdentity(model, name, "", guid);
            _max = max;

            _queue = new System.Collections.Queue();

            Guid inGuid = Utility.GuidOps.Increment(guid);
            Guid outGuid = Utility.GuidOps.Increment(inGuid);

            _output = new SimpleOutputPort(model, "Output", outGuid, this, new DataProvisionHandler(ProvideData), new DataProvisionHandler(PeekData));
            _output.PortDataAccepted += new PortDataEvent(OnOutputPortDataAccepted);
            _input = new SimpleInputPort(model, "Input", inGuid, this, new DataArrivalHandler(OnDataArrived));

            LevelChangedEvent += new QueueLevelChangeEvent(OnQueueLevelChanged);

            IMOHelper.RegisterWithModel(this);
        }

        /// <summary>
        /// Gets the input port for this queue.
        /// </summary>
        /// <value>The input port.</value>
		public IInputPort Input
        {
            get
            {
                return _input;
            }
        }

        /// <summary>
        /// Gets the output port for this queue.
        /// </summary>
        /// <value>The output.</value>
		public IOutputPort Output
        {
            get
            {
                return _output;
            }
        }

        #region Initialization
        //TODO: 1.) Make sure that what happens in any other ctors also happens in the Initialize method.
        //TODO: 2.) Replace all DESCRIPTION? tags with the appropriate text.
        //TODO: 3.) If this class is derived from another that implements IModelObject, remove the m_model, m_name, and m_guid declarations.
        /// <summary>
        /// Use this for initialization of the form 'new Queue().Initialize( ... );'
        /// Note that this mechanism relies on the whole model performing initialization.
        /// </summary>
        public Queue()
        {
        }

        /// <summary>
        /// The Initialize(...) method is designed to be used explicitly with the 'new ObjectQueue().Initialize(...);'
        /// idiom, and then implicitly upon loading of the model from an XML document.
        /// </summary>
        /// <param name="model">The model in which this queue exists.</param>
        /// <param name="name">The name of this queue.</param>
        /// <param name="description">The description of this queue.</param>
        /// <param name="guid">The GUID of this queue.</param>
        /// <param name="max">The maximum number of items that can be held in this queue.</param>
        [Initializer(InitializationType.PreRun, "_Initialize")]
        public void Initialize(IModel model, string name, string description, Guid guid,
            [InitializerArg(0, "max", RefType.Owned, typeof(int), "The largest number of objects the queue can hold.")]
            int max)
        {

            InitializeIdentity(model, name, description, guid);

            IMOHelper.RegisterWithModel(this);
            model.GetService<InitializationManager>().AddInitializationTask(_Initialize, max);
        }


        /// <summary>
        /// First-round follow-on to the <see cref="Initialize"/> call.
        /// </summary>
        /// <param name="model">The model in which this queue exists.</param>
        /// <param name="p">The array of passed-in arguments.</param>
        public void _Initialize(IModel model, object[] p)
        {

            Guid inGuid = Utility.GuidOps.Increment(Guid);
            Guid outGuid = Utility.GuidOps.Increment(inGuid);

            _output = new SimpleOutputPort(model, "Output", outGuid, this, new DataProvisionHandler(ProvideData), new DataProvisionHandler(PeekData));
            _output.PortDataAccepted += new PortDataEvent(OnOutputPortDataAccepted);
            _input = new SimpleInputPort(model, "Input", inGuid, this, new DataArrivalHandler(OnDataArrived));

            //Ports.AddPort(m_output); <-- Done in port's ctor.
            //Ports.AddPort(m_input);  <-- Done in port's ctor.

            LevelChangedEvent += new QueueLevelChangeEvent(OnQueueLevelChanged);

            _max = (int)p[0];
            _queue = new System.Collections.Queue(_max);


        }

        #endregion

        /// <summary>
        /// Gets the max depth of this queue.
        /// </summary>
        /// <value>The max depth.</value>
        public int MaxDepth
        {
            [DebuggerStepThrough]
            get
            {
                return _max;
            }
        }

        private bool OnDataArrived(object data, IInputPort ip)
        {
            if (data != null)
            {
                _queue.Enqueue(data);
                if (ObjectEnqueued != null)
                    ObjectEnqueued(this, data);
                LevelChangedEvent(Count - 1, Count, this);
                _output.NotifyDataAvailable();
            }
            return true;
        }

        /// <summary>
        /// Called when the queue level changes.
        /// </summary>
        /// <param name="previous">The previous level.</param>
        /// <param name="current">The current level.</param>
        /// <param name="queue">The queue on which the change occurred.</param>
        public void OnQueueLevelChanged(int previous, int current, IQueue queue)
        {
            if (current == 0 && QueueEmptyEvent != null)
                QueueEmptyEvent(this);
            if (current == _max && QueueFullEvent != null)
                QueueFullEvent(this);
        }

        private void OnOutputPortDataAccepted(object data, IPort where)
        {
            if (ObjectDequeued != null)
                ObjectDequeued(this, data);
        }

        private object ProvideData(IOutputPort op, object selector)
        {
            if (_queue.Count > 0)
            {
                object data = _queue.Dequeue();
                LevelChangedEvent(Count + 1, Count, this);
                // Commented out the following because this functionality is now provided
                // (as it was before, also) through OnPortDataAccepted. Since the Queue
                // functionality assumes that if ProvideData is called, the data will be
                // accepted, then these two avenues are equivalent.
                //if ( ObjectDequeued != null ) ObjectDequeued(this,data);
                return data;
            }
            else
            {
                return null;
            }
        }

        private object PeekData(IOutputPort op, object selector)
        {
            if (_queue.Count > 0)
            {
                object data = _queue.Peek();
                return data;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the number of items currently in this queue.
        /// </summary>
        /// <value>The count.</value>
		public int Count
        {
            [DebuggerStepThrough]
            get
            {
                return _queue.Count;
            }
        }

        public event QueueMilestoneEvent QueueFullEvent;
        public event QueueMilestoneEvent QueueEmptyEvent;
        public event QueueLevelChangeEvent LevelChangedEvent;
        public event QueueOccupancyEvent ObjectEnqueued;
        public event QueueOccupancyEvent ObjectDequeued;

        #region IPortOwner Implementation
        /// <summary>
        /// The PortSet object to which this IPortOwner delegates.
        /// </summary>
        private readonly PortSet _ports = new PortSet();
        /// <summary>
        /// Registers a port with this IPortOwner
        /// </summary>
        /// <param name="port">The port that this IPortOwner will add.</param>
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
        /// <param name="port">The port that this IPortOwner will remove.</param>
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

        #region Implementation of IModelObject

        /// <summary>
        /// The model to which this Queue belongs.
        /// </summary>
        /// <value>The Queue's description.</value>
        public IModel Model
        {
            [DebuggerStepThrough]
            get
            {
                return _model;
            }
        }

        /// <summary>
        /// The Name for this Queue. Typically used for human-readable representations.
        /// </summary>
        /// <value>The Queue's name.</value>
        public string Name
        {
            [DebuggerStepThrough]
            get
            {
                return _name;
            }
        }

        /// <summary>
        /// The Guid of this Queue.
        /// </summary>
        /// <value>The Queue's Guid.</value>
        public Guid Guid
        {
            [DebuggerStepThrough]
            get
            {
                return _guid;
            }
        }

        /// <summary>
        /// The description for this Queue. Typically used for human-readable representations.
        /// </summary>
        /// <value>The Queue's description.</value>
        public string Description => (_description ?? ("No description for " + _name));

        /// <summary>
        /// Initializes the fields that feed the properties of this IModelObject identity.
        /// </summary>
        /// <param name="model">The Queue's new model value.</param>
        /// <param name="name">The Queue's new name value.</param>
        /// <param name="description">The Queue's new description value.</param>
        /// <param name="guid">The Queue's new GUID value.</param>
        public void InitializeIdentity(IModel model, string name, string description, Guid guid)
        {
            IMOHelper.Initialize(ref _model, model, ref _name, name, ref _description, description, ref _guid, guid);
        }

        #endregion

    }
}