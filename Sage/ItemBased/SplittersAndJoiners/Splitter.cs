/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.SimCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Highpoint.Sage.ItemBased.SplittersAndJoiners
{

    /// <summary>
    /// Receives an object on its input port, and sends it out one or more output ports, as defined
    /// in a derived class. If it gets a pull from any output port, it pulls from its one input port.
    /// Notification of data available proceeds according to a derived class' logic.
    /// </summary>
    public abstract class Splitter : IPortOwner, ISplitter
    {

        private string _name = null;
        private Guid _guid = Guid.Empty;
        private IModel _model;
        private string _description = null;

        public IInputPort Input;
        protected SimpleInputPort m_input;
        public IOutputPort[] Outputs;
        protected SimpleOutputPort[] m_outputs;

        public Splitter(IModel model, string name, Guid guid, int nOuts)
        {
            IMOHelper.Initialize(ref _model, model, ref _name, name, ref _description, null, ref _guid, guid);
            _ports = new PortSet();
            m_input = new SimpleInputPort(model, "Input", Guid.NewGuid(), this, GetDataArrivalHandler());
            //AddPort(m_input); <-- Done in SIP's ctor.
            Input = m_input;
            Outputs = new IOutputPort[nOuts];
            m_outputs = new SimpleOutputPort[nOuts];
            for (int i = 0; i < nOuts; i++)
            {
                m_outputs[i] = new SimpleOutputPort(model, "Output" + i, Guid.NewGuid(), this, GetDataProvisionHandler(i), GetPeekHandler(i));
                Outputs[i] = m_outputs[i];
                //AddPort(m_outputs[i]); <-- Done in SOP's ctor.
            }
            IMOHelper.RegisterWithModel(this);
        }
        protected abstract DataArrivalHandler GetDataArrivalHandler();
        protected abstract DataProvisionHandler GetPeekHandler(int i);
        protected abstract DataProvisionHandler GetDataProvisionHandler(int i);

        #region IPortOwner Implementation
        /// <summary>
        /// The PortSet object to which this IPortOwner delegates.
        /// </summary>
        private readonly PortSet _ports = new PortSet();
        /// <summary>
        /// Registers a port with this IPortOwner
        /// </summary>
        /// <param name="port">The port that is to be added to this IPortOwner.</param>
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
        /// <param name="port">The port that is to be removed from this IPortOwner.</param>
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

        #region ISplitter Members

        IInputPort ISplitter.Input
        {
            get
            {
                return m_input;
            }
        }

        IOutputPort[] ISplitter.Outputs
        {
            get
            {
                return m_outputs;
            }
        }

        #endregion

        #region Implementation of IModelObject

        /// <summary>
        /// The IModel to which this object belongs.
        /// </summary>
        /// <value>The object's Model.</value>
        public IModel Model
        {
            [DebuggerStepThrough]
            get
            {
                return _model;
            }
        }

        /// <summary>
        /// The name by which this object is known. Typically not required to be unique in a pan-model context.
        /// </summary>
        /// <value>The object's name.</value>
        public string Name
        {
            [DebuggerStepThrough]
            get
            {
                return _name;
            }
        }

        /// <summary>
        /// The description for this object. Typically used for human-readable representations.
        /// </summary>
        /// <value>The object's description.</value>
        public string Description
        {
            [DebuggerStepThrough]
            get
            {
                return (_description ?? "No description for " + _name);
            }
        }

        /// <summary>
        /// The Guid for this object. Typically required to be unique in a pan-model context.
        /// </summary>
        /// <value>The object's Guid.</value>
        public Guid Guid
        {
            [DebuggerStepThrough]
            get
            {
                return _guid;
            }
        }

        /// <summary>
        /// Initializes the fields that feed the properties of this IModelObject identity.
        /// </summary>
        /// <param name="model">The IModelObject's new model value.</param>
        /// <param name="name">The IModelObject's new name value.</param>
        /// <param name="description">The IModelObject's new description value.</param>
        /// <param name="guid">The IModelObject's new GUID value.</param>
        public void InitializeIdentity(IModel model, string name, string description, Guid guid)
        {
            IMOHelper.Initialize(ref _model, model, ref _name, name, ref _description, description, ref _guid, guid);
        }

        #endregion
    }
}