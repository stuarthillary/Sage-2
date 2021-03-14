/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.Persistence;
using Highpoint.Sage.SimCore;
using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;

namespace Highpoint.Sage.ItemBased.Connectors
{
    public class BasicNonBufferedConnector : IConnector
    {

        #region Private Fields
        private IOutputPort _upstream;
        private IInputPort _downstream;
        private bool _inUse;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicNonBufferedConnector"/> class.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="name">The name.</param>
        /// <param name="description">The description.</param>
        /// <param name="guid">The GUID.</param>
        /// <param name="input">The input.</param>
        /// <param name="output">The output.</param>
        public BasicNonBufferedConnector(IModel model, string name, string description, Guid guid, IPort input, IPort output)
        {
            IMOHelper.Initialize(ref _model, model, ref _name, name, ref _description, description, ref _guid, guid);
            Connect(input, output);
            IMOHelper.RegisterWithModel(this);
            _inUse = false;
        }

        /// <summary>
        /// For prelude to deserialization only.
        /// </summary>
        public BasicNonBufferedConnector()
        {
            _inUse = false;
        }

        public BasicNonBufferedConnector(IPort input, IPort output)
        {
            Connect(input, output);
            _inUse = false;
        }

        public IInputPort Downstream
        {
            get
            {
                return _downstream;
            }
        }
        public IOutputPort Upstream
        {
            get
            {
                return _upstream;
            }
        }

        public void Connect(IPort p1, IPort p2)
        {
            if (_upstream != null || _downstream != null)
                throw new ApplicationException("Trying to connect an already-connected port.");
            if (p1 is IInputPort && p2 is IOutputPort)
            {
                Attach((IInputPort)p1, (IOutputPort)p2);
            }
            else if (p2 is IInputPort && p1 is IOutputPort)
            {
                Attach((IInputPort)p2, (IOutputPort)p1);
            }
            else
            {
                throw new ApplicationException("Trying to connect non-compatible ports " + p1.GetType() + " and " + p2.GetType());
            }
            //Console.WriteLine("Just connected " + ((IHasIdentity)m_upstream.Owner).Name + "." + m_upstream.Key + " to " + ((IHasIdentity)m_downstream.Owner).Name + "." + m_downstream.Key);
            //Console.WriteLine(Upstream.Connector + ", " + Downstream.Connector);
        }

        public void Disconnect()
        {
            Detach();
            _inUse = false;
        }

        /// <summary>
        /// Called by the PortOwner after it has put new data on the port. It indicates that
        /// data is newly available on this port. Since it is a multicast event, by the time
        /// a recipient receives it, the newly-arrived data may be gone already.
        /// </summary>
        public void NotifyDataAvailable()
        {
            _downstream.NotifyDataAvailable();
        }

        internal void Attach(IInputPort input, IOutputPort output)
        {
            _upstream = output;
            _downstream = input;
            input.Connector = this;
            output.Connector = this;
        }

        internal void Detach()
        {
            //			_Debug.WriteLine("Setting " + ((IHasIdentity)m_upstream.Owner).Name + "'s connector to null.");
            //			_Debug.WriteLine("Setting " + m_upstream.Owner + "'s connector to null.");
            if (_upstream != null)
                _upstream.Connector = null;
            //			_Debug.WriteLine("Setting " + ((IHasIdentity)m_downstream.Owner).Name + "'s connector to null.");
            //			_Debug.WriteLine("Setting " + m_downstream.Owner + "'s connector to null.");
            if (_downstream != null)
                _downstream.Connector = null;
            _upstream = null;
            _downstream = null;
        }

        public object GetOutOfBandData()
        {
            return _downstream.GetOutOfBandData();
        }
        public object GetOutOfBandData(object key)
        {
            return _downstream.GetOutOfBandData(key);
        }
        public bool IsPeekable
        {
            get
            {
                return _upstream.IsPeekable;
            }
        }
        public object Peek(object selector)
        {
            return _upstream.Peek(selector);
        }
        public object Take(object selector)
        {
            return _upstream.Take(selector);
        }
        public bool Put(object data)
        {
            return _downstream.Put(data);
        }

        #region Member Variables

        private IModel _model;
        private string _name = String.Empty;
        private string _description = String.Empty;
        private Guid _guid = Guid.Empty;
        private IPort _input = null;
        private IPort _output = null;
        #endregion Member Variables

        #region Initialization
        //TODO: Replace all DESCRIPTION? tags with the appropriate text.
        //TODO: If this class is derived from another that implements IModelObject, remove the m_model, m_name, and m_guid declarations.
        //TODO: Make sure that what happens in any other ctors also happens in the Initialize method.

        [Initializer(InitializationType.PreRun, "_Initialize")]
        public void Initialize(IModel model, string name, string description, Guid guid,
            [InitializerArg(0, "inputPortOwner", RefType.Owned, typeof(IPortOwner), "The upstream port owner attached to this connector")]
            Guid inputPortOwner,
           [InitializerArg(1, "inputPortName", RefType.Owned, typeof(string), "The name of the port on the upstream port owner")]
            string inputPortName,
        [InitializerArg(2, "outputPortOwner", RefType.Owned, typeof(IPort), "The downstream port attached to this connector")]
            Guid outputPortOwner,
        [InitializerArg(3, "outputPortName", RefType.Owned, typeof(string), "The downstream port attached to this connector")]
            string outputPortName)
        {

            InitializeIdentity(model, name, description, guid);

            // Put here: Things that are done in the full constructor, but don't operate
            // on the arguments passed into that ctor or this initialize method.

            IMOHelper.RegisterWithModel(this);

            model.GetService<InitializationManager>().AddInitializationTask(new Initializer(_Initialize), inputPortOwner, inputPortName, outputPortOwner, outputPortName);
        }

        /// <summary>
        /// Services needs in the first dependency-sequenced round of initialization.
        /// </summary>
        /// <param name="model">The model in which the initialization is taking place.</param>
        /// <param name="p">The array of objects that take part in this round of initialization.</param>
        public void _Initialize(IModel model, object[] p)
        {
            IPortOwner ipo = (IPortOwner)model.ModelObjects[p[0]];
            _input = ipo.Ports[(string)p[1]];
            IPortOwner opo = (IPortOwner)model.ModelObjects[p[2]];
            _output = ipo.Ports[(string)p[3]];
            Connect(_input, _output);
        }


        #endregion

        #region Implementation of IModelObject

        /// <summary>
        /// The model to which this BasicNonBufferedConnector belongs.
        /// </summary>
        /// <value>The BasicNonBufferedConnector's description.</value>
        public IModel Model
        {
            [DebuggerStepThrough]
            get
            {
                return _model;
            }
        }
        /// <summary>
        /// The Name for this BasicNonBufferedConnector. Typically used for human-readable representations.
        /// </summary>
        /// <value>The BasicNonBufferedConnector's name.</value>
        public string Name
        {
            [DebuggerStepThrough]
            get
            {
                return _name;
            }
        }
        /// <summary>
        /// The Guid of this BasicNonBufferedConnector.
        /// </summary>
        /// <value>The BasicNonBufferedConnector's Guid.</value>
        public Guid Guid
        {
            [DebuggerStepThrough]
            get
            {
                return _guid;
            }
        }
        /// <summary>
        /// The description for this BasicNonBufferedConnector. Typically used for human-readable representations.
        /// </summary>
        /// <value>The BasicNonBufferedConnector's description.</value>
        public string Description => (_description ?? ("No description for " + _name));

        /// <summary>
        /// Initializes the fields that feed the properties of this IModelObject identity.
        /// </summary>
        /// <param name="model">The BasicNonBufferedConnector's new model value.</param>
        /// <param name="name">The BasicNonBufferedConnector's new name value.</param>
        /// <param name="description">The BasicNonBufferedConnector's new description value.</param>
        /// <param name="guid">The BasicNonBufferedConnector's new GUID value.</param>
        public void InitializeIdentity(IModel model, string name, string description, Guid guid)
        {
            IMOHelper.Initialize(ref _model, model, ref _name, name, ref _description, description, ref _guid, guid);
        }

        #endregion

        #region IConnector Members


        public bool InUse
        {
            get
            {
                return _inUse;
            }
            set
            {
                if (_inUse != value)
                {
                    _inUse = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs("InUse"));
                    }
                }
            }
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        #endregion

        /// <summary>
        /// Prior to this call, you must have created the connector using the 
        /// </summary>
        /// <param name="self"></param>
        /// <param name="deserializationContext"></param>
        public void LoadFromXElement(XElement self, DeserializationContext deserializationContext)
        {

            IModel model = null;
            string connectorName = self.Attribute("connectorName").Value;
            string connectorDesc = self.Attribute("connectorDesc").Value;
            Guid connectorGuidWas = XmlConvert.ToGuid(self.Attribute("connectorGuid").Value);
            Guid connectorGuidIs = Guid.NewGuid();
            deserializationContext.SetNewGuidForOldGuid(connectorGuidWas, connectorGuidIs);
            IMOHelper.Initialize(ref _model, model, ref _name, connectorName, ref _description, connectorDesc, ref _guid, connectorGuidIs);
            IMOHelper.RegisterWithModel(this);

            XElement source = self.Element("Source");
            Guid upstreamOwnerGuidWas = XmlConvert.ToGuid(source.Attribute("guid").Value);
            Guid upstreamOwnerGuidIs = Guid.NewGuid();
            string upstreamPortName = source.Attribute("name").Value;
            IPortOwner usmb = (IPortOwner)deserializationContext.GetModelObjectThatHad(upstreamOwnerGuidWas);
            IOutputPort upstreamPort = (IOutputPort)usmb.Ports[upstreamPortName];

            XElement destination = self.Element("Destination");
            Guid downstreamOwnerGuidWas = XmlConvert.ToGuid(destination.Attribute("guid").Value);
            Guid downstreamOwnerGuidIs = Guid.NewGuid();
            string downstreamPortName = destination.Attribute("name").Value;
            IPortOwner dsmb = (IPortOwner)deserializationContext.GetModelObjectThatHad(downstreamOwnerGuidWas);
            IInputPort downstreamPort = (IInputPort)dsmb.Ports[downstreamPortName];

            Connect(upstreamPort, downstreamPort);
        }

        public XElement AsXElement(string name)
        {

            Guid upstreamOwnerGuid = Upstream.Owner != null ? Upstream.Owner is IModelObject ? ((IModelObject)Upstream.Owner).Guid : Guid.Empty : Guid.Empty;
            string upstreamPortName = Upstream != null ? Upstream.Name : string.Empty;

            Guid downstreamOwnerGuid = Downstream.Owner != null ? Downstream.Owner is IModelObject ? ((IModelObject)Downstream.Owner).Guid : Guid.Empty : Guid.Empty;
            string downstreamPortName = Downstream != null ? Downstream.Name : string.Empty;

            return new XElement(name,
                new XAttribute("connectorName", Name),
                new XAttribute("connectorDescription", Description),
                new XAttribute("connectorGuid", Guid.ToString()),

                new XElement("Source",
                    new XAttribute("guid", upstreamOwnerGuid),
                    new XAttribute("name", upstreamPortName)),

                new XElement("Destination",
                    new XAttribute("guid", downstreamOwnerGuid),
                    new XAttribute("name", downstreamPortName))
                );
        }
    }
}