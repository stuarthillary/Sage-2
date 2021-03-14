/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Mathematics;
using Highpoint.Sage.SimCore;
using System;
using System.Collections;

namespace Highpoint.Sage.ItemBased.Queues.DataCollectors
{
    /// <summary>
    /// Summary description for WaitingTime.
    /// </summary>
    public class WaitingTime : IModelObject
    {
        private readonly IQueue _hostQueue;
        private readonly ArrayList _data;
        private readonly Hashtable _occupants;
        private readonly int _nBins;
        private Histogram1D_TimeSpan _hist = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitingTime"/> class.
        /// </summary>
        /// <param name="model">The model in which this object runs.</param>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        /// <param name="hostQueue">The host queue.</param>
        /// <param name="nBins">The number of bins into which to divide the waiting time.</param>
		public WaitingTime(IModel model, string name, Guid guid, IQueue hostQueue, int nBins)
        {
            InitializeIdentity(model, name, null, guid);

            _nBins = nBins;
            _hostQueue = hostQueue;
            _hostQueue.ObjectEnqueued += new QueueOccupancyEvent(hostQueue_ObjectEnqueued);
            _hostQueue.ObjectDequeued += new QueueOccupancyEvent(hostQueue_ObjectDequeued);
            _data = new ArrayList();
            _occupants = new Hashtable();

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

        public void Reset()
        {
            _data.Clear();
            _hist = null;
        }

        public Histogram1D_TimeSpan Histogram
        {
            get
            {
                if (_data.Count == 0)
                    return new Histogram1D_TimeSpan(TimeSpan.MinValue, TimeSpan.MaxValue, 1);
                TimeSpan[] rawdata = new TimeSpan[_data.Count];
                TimeSpan min = TimeSpan.MaxValue;
                TimeSpan max = TimeSpan.MinValue;
                int ndx = 0;
                foreach (TimeSpan data in _data)
                {
                    rawdata[ndx++] = data;
                    if (data < min)
                        min = data;
                    if (data > max)
                        max = data;
                }
                if (min == max)
                    max += TimeSpan.FromMinutes(_nBins);
                _hist = new Histogram1D_TimeSpan(rawdata, min, max, _nBins, _name);
                _hist.Recalculate();
                return _hist;
            }
        }

        private void hostQueue_ObjectEnqueued(IQueue hostQueue, object serviceItem)
        {
            _occupants.Add(serviceItem, _model.Executive.Now);
            //_Debug.WriteLine(m_model.Executive.Now + " : " + this.Name + " enqueueing " + serviceItem + ". It currently has " + hostQueue.Count + " occupants.");
        }

        private void hostQueue_ObjectDequeued(IQueue hostQueue, object serviceItem)
        {
            DateTime entry = (DateTime)_occupants[serviceItem];
            _occupants.Remove(serviceItem);
            TimeSpan duration = _model.Executive.Now - entry;
            //_Debug.WriteLine(m_model.Executive.Now + " : " + this.Name + " dequeueing " + serviceItem + " after " + duration + ". It currently has " + hostQueue.Count + " occupants.");
            _data.Add(duration);
            _hist = null;
        }

        #region Implementation of IModelObject
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
        /// A description of this WaitingTime Histogram.
        /// </summary>
        public string Description
        {
            get
            {
                return _description ?? _name;
            }
        }
        private Guid _guid = Guid.Empty;
        public Guid Guid => _guid;
        private IModel _model;
        /// <summary>
        /// The model that owns this object, or from which this object gets time, etc. data.
        /// </summary>
        /// <value>The model.</value>
        public IModel Model => _model;
        #endregion

    }
}
