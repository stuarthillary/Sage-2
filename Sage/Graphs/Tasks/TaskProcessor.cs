/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Persistence;
using Highpoint.Sage.SimCore;
using System;
using System.Collections;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Graphs.Tasks
{

    /// <summary>
    /// TaskProcessor encapsulates a task, and is responsible for scheduling its
    /// execution. It must be run by an external entity, often at the model's
    /// start. This external entity must call Activate in order to cause the
    /// task to be scheduled, and subsequently run - the default Model implementation
    /// does this automatically in the Running state method.
    /// </summary>
    public class TaskProcessor : IModelObject, IXmlPersistable
    {

        #region Private Fields
        private Task _masterTask;

        private bool _startConditionsSpecified = false;
        private DateTime _when;
        private double _priority;
        private ExecEventType _eet;
        private IModel _model;
        private string _description = null;
        private Guid _guid = Guid.Empty;
        private string _name = null;
        private bool _keepGraphContexts = false;

        private static readonly bool _diagnostics = Diagnostics.DiagnosticAids.Diagnostics("TaskProcessor");
        #endregion

        #region Protected Fields
        protected IDictionary GraphContext;
        protected ArrayList _graphContexts = new ArrayList();
        #endregion

        #region Constructors
        public TaskProcessor(IModel model, string name, Task task) : this(model, name, Guid.NewGuid(), task) { }

        public TaskProcessor(IModel model, string name, Guid guid, Task task)
        {
            InitializeIdentity(model, name, null, guid);

            _masterTask = task;
            _priority = 0.0;
            _when = DateTime.MinValue;
            _eet = ExecEventType.Synchronous;
            Model.GetService<ITaskManagementService>().AddTaskProcessor(this);

            IMOHelper.RegisterWithModel(this);
        }
        #endregion

        /// <summary>
        /// Initialize the identity of this model object, once.
        /// </summary>
        /// <param name="model">The model this component runs in.</param>
        /// <param name="name">The name of this component.</param>
        /// <param name="description">The description for this component.</param>
        /// <param name="guid">The GUID of this component.</param>
        public void InitializeIdentity(IModel model, string name, string description, Guid guid)
        {
            IMOHelper.Initialize(ref _model, model, ref _name, name, ref _description, description, ref _guid, guid);
        }

        /// <summary>
        /// Gets the master task for this Task Processor - the task that holds the root level of the task graph to be performed.
        /// </summary>
        /// <value>The master task.</value>
        public Task MasterTask
        {
            get
            {
                return _masterTask;
            }
        }

        public void SetConfigData(DateTime when, ExecEventType eet, double priority)
        {
            SetStartTime(when);
            SetStartEventType(eet);
            SetStartEventPriority(priority);
        }

        public void SetStartTime(DateTime when)
        {
            _startConditionsSpecified = true;
            _when = when;
        }

        public DateTime StartTime
        {
            get
            {
                return _when;
            }
        }

        public void SetStartEventType(ExecEventType eet)
        {
            _eet = eet;
        }

        public void SetStartEventPriority(double priority)
        {
            _priority = priority;
        }

        public virtual void Activate()
        {
            //_Debug.WriteLine("Activating " + m_name );
            if (!_startConditionsSpecified)
            {
                _when = _model.Executive.Now;
                _priority = 0.0;
            }
            if (GraphContext == null)
            {
                GraphContext = new Hashtable();
            }
            else
            {
                _model.Executive.ClearVolatiles(GraphContext);
            }
            if (_keepGraphContexts)
                _graphContexts.Add(GraphContext);
            _model.Executive.RequestEvent(new ExecEventReceiver(BeginExecution), _when, _priority, GraphContext, _eet);
        }

        private void BeginExecution(IExecutive exec, object userData)
        {
            if (_diagnostics)
            {
                _Debug.WriteLine("Task processor " + Name + " beginning execution instance of graph " + _masterTask.Name);
            }
            _masterTask.Start((IDictionary)userData);
        }

        public bool KeepGraphContexts
        {
            get
            {
                return _keepGraphContexts;
            }
            set
            {
                _keepGraphContexts = value;
            }
        }
        public ArrayList GraphContexts
        {
            get
            {
                return ArrayList.ReadOnly(_graphContexts);
            }
        }
        public IDictionary CurrentGraphContext
        {
            get
            {
                return GraphContext;
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }
        /// <summary>
        /// A description of this TaskProcessor.
        /// </summary>
        public string Description
        {
            get
            {
                return _description ?? _name;
            }
        }
        public Guid Guid
        {
            get
            {
                return _guid;
            }
            set
            {
                if (_model != null)
                {
                    _model.ModelObjects.Remove(_guid);
                    _model.ModelObjects.Add(value, this);
                }
                _guid = value;
            }
        }
        /// <summary>
        /// The model that owns this object, or from which this object gets time, etc. data.
        /// </summary>
        /// <value>The model.</value>
        public IModel Model => _model;

        #region >>> Implementation of IXmlPersistable <<<
        /// <summary>
        /// Default constructor for serialization only.
        /// </summary>
        public TaskProcessor()
        {
        }

        public virtual void SerializeTo(XmlSerializationContext xmlsc)
        {
            xmlsc.StoreObject("ExecEventType", _eet);
            xmlsc.StoreObject("Guid", _guid);
            xmlsc.StoreObject("KeepGCs", _keepGraphContexts);
            xmlsc.StoreObject("MasterTask", _masterTask);
            xmlsc.StoreObject("Name", _name);
            xmlsc.StoreObject("StartCondSpec", _startConditionsSpecified);
            xmlsc.StoreObject("When", _when);
        }

        public virtual void DeserializeFrom(XmlSerializationContext xmlsc)
        {
            _model = (Model)xmlsc.ContextEntities["Model"];
            _eet = (ExecEventType)xmlsc.LoadObject("ExecEventType");
            _guid = (Guid)xmlsc.LoadObject("Guid");
            _keepGraphContexts = (bool)xmlsc.LoadObject("KeepGCs");
            _masterTask = (Task)xmlsc.LoadObject("MasterTask");
            _name = (string)xmlsc.LoadObject("Name");
            _startConditionsSpecified = (bool)xmlsc.LoadObject("StartCondSpec");
            _when = (DateTime)xmlsc.LoadObject("When");

        }

        #endregion
    }
}
