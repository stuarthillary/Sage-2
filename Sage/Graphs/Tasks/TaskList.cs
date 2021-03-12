/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Diagnostics;
using Highpoint.Sage.Persistence;
using Highpoint.Sage.SimCore;
using System;
using System.Collections;
using System.Diagnostics;

namespace Highpoint.Sage.Graphs.Tasks
{
    public class TaskList : IXmlPersistable, IModelObject
    {

        private Task _masterTask;
        private readonly ArrayList _list;
        private readonly Hashtable _hashtable;

        public TaskList(IModel model, string name, Guid guid) : this(model, name, Guid.NewGuid(), new Task(model, name, Guid.NewGuid())) { }

        public TaskList(IModel model, string name, Guid guid, Task task)
        {
            Debug.Assert(model.Equals(task.Model), "TaskList being created for a model, but with a root task that is assigned to a different model.");
            InitializeIdentity(model, name, null, guid);

            _masterTask = task;
            _list = new ArrayList();
            _hashtable = new Hashtable();

            IMOHelper.RegisterWithModel(this);
        }

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

        public Task MasterTask
        {
            get
            {
                return _masterTask;
            }
        }

        public void AddTaskAfter(Task predecessor, Task subject)
        {
            int predIndex = _list.IndexOf(predecessor);
            if (predIndex == -1)
            {
                if (_list.Contains(subject) && !_list.Contains(predecessor))
                {
                    throw new ApplicationException("In \"AddTaskAfter\" operation, TaskList contains subject, but not predecessor - argument order swap?");
                }
                else
                {
                    throw new ApplicationException("In \"AddTaskAfter\" operation, TaskList does not contain the predecessor.");
                }
            }

            if (predIndex == _list.Count - 1)
            {
                //_Debug.WriteLine("Appending task " + subject.Name + " with Guid " + subject.Guid + " under task list for task " + MasterTask.Name + " which currently has " + m_hashtable.Count + " entries.");
                AppendTask(subject);
            }
            else
            { // actual insertion...
                Task pred = (Task)_list[predIndex];
                Task succ = (Task)_list[predIndex + 1];
                _list.Insert(predIndex + 1, subject);
                //_Debug.WriteLine("Appending task " + subject.Name + " with Guid " + subject.Guid + " under task list for task " + MasterTask.Name + " which currently has " + m_hashtable.Count + " entries.");
                _hashtable.Add(subject.Guid, subject);
                pred.RemoveSuccessor(succ);
                pred.AddSuccessor(subject);
                subject.AddSuccessor(succ);
                //succ.AddPredecessor(subject);
                _masterTask.AddChildEdge(subject);
            }
        }

        public void AddTaskBefore(Task successor, Task subject)
        {
            int succIndex = _list.IndexOf(successor);
            if (succIndex == -1)
            {
                if (_list.Contains(subject) && !_list.Contains(successor))
                {
                    throw new ApplicationException("In \"AddTaskBefore\" operation, the ChildTaskList for " + _masterTask.Name + " contains subject, but not successor - argument order swap?");
                }
                else
                {
                    throw new ApplicationException("In \"AddTaskBefore\" operation, the ChildTaskList for " + _masterTask.Name + " does not contain the successor, " + successor.Name + ", so the new task, " + subject.Name + " cannot be added after it.");
                }
            }
            Task succ = null;
            if (succIndex > 0)
            {
                Task pred = (Task)_list[succIndex - 1];
                pred.RemoveSuccessor(successor);
                pred.AddSuccessor(subject);
            }
            succ = (Task)_list[succIndex];
            succ.AddPredecessor(subject);
            _list.Insert(succIndex, subject);
            //_Debug.WriteLine("Appending task " + subject.Name + " with Guid " + subject.Guid + " under task list for task " + MasterTask.Name + " which currently has " + m_hashtable.Count + " entries.");
            _hashtable.Add(subject.Guid, subject);

            _masterTask.AddChildEdge(subject);
        }

        public void AppendTask(Task subject)
        {
            if (_list.Count > 0)
            {
                Task predecessor = (Task)_list[_list.Count - 1];
                _list.Add(subject);
                //_Debug.WriteLine("Appending task " + subject.Name + " with Guid " + subject.Guid + " under task list for task " + MasterTask.Name + " which currently has " + m_hashtable.Count + " entries.");
                _hashtable.Add(subject.Guid, subject);

                subject.AddPredecessor(predecessor);
            }
            else
            {
                //_Debug.WriteLine("Appending task " + subject.Name + " with Guid " + subject.Guid + " under task list for task " + MasterTask.Name + " which currently has " + m_hashtable.Count + " entries.");
                _list.Add(subject);
                _hashtable.Add(subject.Guid, subject);

            }

            _masterTask.AddChildEdge(subject);

        }

        public void RemoveTask(Task subject)
        {

            Task pred = null;
            Task succ = null;
            int subjNdx = _list.IndexOf(subject);

            if (subjNdx < _list.Count - 1)
                succ = (Task)_list[subjNdx + 1];
            if (subjNdx > 0)
                pred = (Task)_list[subjNdx - 1];

            //			_Debug.WriteLine("\r\n\r\n*************************************************************\r\nBefore RemoveTask\r\n");
            //			_Debug.WriteLine(DiagnosticAids.GraphToString(m_masterTask));
            //			_Debug.WriteLine("\r\n\r\n*************************************************************\r\nBefore RemoveChildEdge\r\n");
            _masterTask.RemoveChildEdge(subject);
            //			_Debug.WriteLine("\r\n\r\n*************************************************************\r\nBefore The rest of the stuff...\r\n");
            //			_Debug.WriteLine(DiagnosticAids.GraphToString(m_masterTask));
            _list.Remove(subject);
            _hashtable.Remove(subject.Guid);

            if (pred != null)
                pred.RemoveSuccessor(subject);
            if (succ != null)
                succ.RemovePredecessor(subject);

            // Must heal the list, now.
            if (pred != null && succ != null)
                pred.AddSuccessor(succ);
            if (pred == null && succ != null)
                MasterTask.AddCostart(succ);
            // Was, until 1/25/2004 : if ( pred != null && succ == null ) MasterTask.AddCofinish(pred);
            if (pred != null && succ == null)
                pred.AddCofinish(MasterTask);
            //			_Debug.WriteLine("\r\n\r\n*************************************************************\r\nAfter everything...\r\n");
            //			_Debug.WriteLine(DiagnosticAids.GraphToString(m_masterTask));
            subject.SelfValidState = false;
            //			foreach ( Highpoint.Sage.Graphs.Edge childEdge in subject.ChildEdges ) {
            //				if ( childEdge is Task ) ((Task)childEdge).SelfValidState = false;
            //			}
        }

        public Task this[int i]
        {
            get
            {
                return (Task)_list[i];
            }
        }

        public Task this[Guid guid]
        {
            get
            {
                return (Task)_hashtable[guid];
            }
        }

        public IList List
        {
            get
            {
                return _list;
            }
        }
        public IDictionary Hashtable
        {
            get
            {
                return _hashtable;
            }
        }

        public string ToStringDeep()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int n = 0;
            foreach (Task task in _list)
            {
                sb.Append("Entry # ");
                sb.Append(n++);
                sb.Append(" : \r\n");
                sb.Append(DiagnosticAids.GraphToString(task));
            }
            return sb.ToString();


        }

        #region IXmlPersistable Members
        /// <summary>
        /// Default constructor for serialization only.
        /// </summary>
        public TaskList()
        {
            _hashtable = new Hashtable();
            _list = new ArrayList();
        }
        public virtual void SerializeTo(XmlSerializationContext xmlsc)
        {
            xmlsc.StoreObject("MasterTask", _masterTask);
            xmlsc.StoreObject("ChildTasks", _list);
            // Don't need to store the hashtable.
        }

        public virtual void DeserializeFrom(XmlSerializationContext xmlsc)
        {
            _masterTask = (Task)xmlsc.LoadObject("MasterTask");

            ArrayList tmpList = (ArrayList)xmlsc.LoadObject("ChildTasks");

            foreach (Task task in tmpList)
                AppendTask(task);

        }

        #endregion

        #region IModelObject Members

        private IModel _model;
        /// <summary>
        /// The model that owns this object, or from which this object gets time, etc. data.
        /// </summary>
        /// <value>The model.</value>
        public IModel Model
        {
            get
            {
                return _model;
            }
        }

        private string _name = null;
        public string Name
        {
            get
            {
                return _name;
            }
        }

        private string _description = null;
        /// <summary>
        /// A description of this TaskList.
        /// </summary>
        public string Description
        {
            get
            {
                return _description ?? _name;
            }
        }


        private Guid _guid = Guid.Empty;
        public Guid Guid
        {
            get
            {
                return _guid;
            }
        }

        #endregion
    }
}
