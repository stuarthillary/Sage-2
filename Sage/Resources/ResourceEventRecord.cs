/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using System;
using System.Collections;
// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Resources
{
    /// <summary>
    /// A record that represents the details of a transaction involving a resource. These include the various <see cref="Highpoint.Sage.Resources.ResourceAction"/>s.
    /// </summary>
    public class ResourceEventRecord
    {

        #region Private Fields

        private readonly IResource _resource;
        private double _quantityDesired;
        private double _quantityObtained;
        private double _capacity;
        private object _tag;
        private Guid _tagGuid;
        private IEditor _myEditor;

        #endregion

        /// <summary>
        /// Constructs a record of a resource transaction.
        /// </summary>
        /// <param name="when">The time (model time) that the transaction took place.</param>
        /// <param name="resource">The resource against which this transaction took place.</param>
        /// <param name="irr">The resource request that initiated this transaction.</param>
        /// <param name="action">The type of <see cref="Highpoint.Sage.Resources.ResourceAction"/> that took place.</param>
        public ResourceEventRecord(DateTime when, IResource resource, IResourceRequest irr, ResourceAction action)
        {
            _resource = resource;
            ResourceGuid = resource.Guid;
            When = when;
            _quantityDesired = irr.QuantityDesired;
            _quantityObtained = irr.QuantityObtained;
            _capacity = resource.Capacity;
            Available = resource.Available;
            Requester = irr.Requester;
            RequesterGuid = irr.Requester?.Guid ?? Guid.Empty;
            Action = action;
            _tag = null;
            _tagGuid = Guid.Empty;
            SerialNumber = Utility.SerialNumberService.GetNext();
        }

        /// <summary>
        /// Constructs a record of a resource transaction.
        /// </summary>
        /// <param name="when">The time (model time) that the transaction took place.</param>
        /// <param name="resourceGuid">The GUID of the resource against which this transaction took place.</param>
        /// <param name="desired">The quantity that was desired of the specified resource.</param>
        /// <param name="obtained">The quantity that was obtained of the specified resource.</param>
        /// <param name="capacity">The capacity of the specified resource after this transaction took place.</param>
        /// <param name="available">The amount available of the specified resource after this transaction took place.</param>
        /// <param name="requesterGuid">The GUID of the requester.</param>
        /// <param name="action">The type of <see cref="Highpoint.Sage.Resources.ResourceAction"/> that took place.</param>
		public ResourceEventRecord(DateTime when, Guid resourceGuid, double desired, double obtained, double capacity, double available, Guid requesterGuid, ResourceAction action)
        {
            When = when;
            _resource = null;
            ResourceGuid = resourceGuid;
            _quantityDesired = desired;
            _quantityObtained = obtained;
            _capacity = capacity;
            Available = available;
            Requester = null;
            RequesterGuid = requesterGuid;
            Action = action;
            _tag = null;
            _tagGuid = Guid.Empty;
            SerialNumber = Utility.SerialNumberService.GetNext();
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="source">Source RER to use as as a base</param>
        /// <param name="replacementResource">The resource that replaces the resource from the source RER in the newly constructed ResourceEventRecord.</param>
		public ResourceEventRecord(ResourceEventRecord source, IResource replacementResource)
        {
            When = source.When;
            ResourceGuid = source.ResourceGuid;
            _quantityDesired = source.QuantityDesired;
            _quantityObtained = source.QuantityObtained;
            _capacity = source.Capacity;
            Available = source.Available;
            Requester = source.Requester;
            RequesterGuid = source.RequesterGuid;
            Action = source.Action;
            _tag = source.Tag;
            _tagGuid = source.TagGuid;
            SerialNumber = source.SerialNumber;

            _resource = replacementResource;
            ResourceGuid = replacementResource.Guid;
        }

        /// <summary>
        /// Ancillary data for consumption by client code.
        /// </summary>
        /// <value>The tag GUID.</value>
		public Guid TagGuid
        {
            get
            {
                return _tagGuid;
            }
            set
            {
                _tag = null;
                _tagGuid = value;
            }
        }

        /// <summary>
        /// Ancillary data for consumption by client code.
        /// </summary>
        /// <value>The tag.</value>
		public object Tag
        {
            get
            {
                return _tag;
            }
            set
            {
                _tag = value;
                IHasIdentity tag = _tag as IHasIdentity;
                if (tag != null)
                    _tagGuid = tag.Guid;
            }
        }

        /// <summary>
        /// The resource against which this event transpired.
        /// </summary>
        public IResource Resource => _resource;

        /// <summary>
		/// The guid of the resource against which this event transpired.
		/// </summary>
		public Guid ResourceGuid
        {
            get;
        }

        /// <summary>
		/// The time that the event transpired.
		/// </summary>
		public DateTime When
        {
            get;
        }

        /// <summary>
		/// The quantity of resource that was desired by the resource request.
		/// </summary>
		public double QuantityDesired => _quantityDesired;

        /// <summary>
		/// The amount of resource granted to the requester.
		/// </summary>
		public double QuantityObtained => _quantityObtained;

        /// <summary>
		/// The capacity of the resource at the time of the request.
		/// </summary>
		public double Capacity => _capacity;

        /// <summary>
		/// The amount of the resource that was available AFTER the request was handled.
		/// </summary>
		public double Available
        {
            get; private set;
        }

        /// <summary>
		/// The identity of the entity that requested the resource.
		/// </summary>
		public IHasIdentity Requester
        {
            get;
        }

        /// <summary>
		/// The identity of the entity that requested the resource.
		/// </summary>
        [Obsolete("Use \"Requester\" instead.", false)]
        public IHasIdentity ByWhom => Requester;

        /// <summary>
		/// The identity of the entity that requested the resource.
		/// </summary>
		public Guid RequesterGuid
        {
            get;
        }

        /// <summary>
		/// The type of resource action that took place (Request, Reserved, Unreserved, Acquired, Released).
		/// </summary>
		public ResourceAction Action
        {
            get;
        }

        /// <summary>
		/// The serial number of this Resource Event Record.
		/// </summary>
		public long SerialNumber
        {
            get;
        }

        /// <summary>
		/// Returns a string representation of this transaction.
		/// </summary>
		/// <returns>A string representation of this transaction.</returns>
		public override string ToString()
        {
            return When + " : " + _resource.Name + ", " + _quantityObtained
                + ", " + _quantityDesired + ", " + _capacity + ", " + Available + ", "
                + (Requester == null ? "<unknown>" : Requester.Name) + ", " + Action;
        }

        /// <summary>
        /// Returns a detailed string representation of this transaction.
        /// </summary>
        /// <returns>A detailed string representation of this transaction.</returns>
        public string Detail()
        {
            string tagString = "";
            IHasIdentity tag = _tag as IHasIdentity;
            if (tag != null)
            {
                tagString = ", " + tag.Name + "(" + tag.Guid + ")";
            }

            return When + " : " + (_resource == null ? "<unknown>" : _resource.Name) + ", " + _quantityObtained
                + ", " + _quantityDesired + ", " + _capacity + ", " + Available + ", "
                + (Requester == null ? "<unknown>" : Requester.Name) + ", " + Action + tagString;
        }

        /// <summary>
        /// Returns a string representation of a header for a table of ResourceEventRecords, identifying the columns.
        /// </summary>
        /// <returns>A string representation of a header for a table of ResourceEventRecords, identifying the columns.</returns>
        public static string ToStringHeader()
        {
            return "When\tName\tObtained\tDesired \tCapacity\tAvailable\tByWhom\tTransactType";
        }

        /// <summary>
        /// Gets the object that provides editing capability into this RER.
        /// </summary>
        /// <value>The editor.</value>
        public IEditor Editor => _myEditor ?? (_myEditor = new RerEditor(this));

        /// <summary>
        /// Returns a comparer that can be used, for example, to sort ResourceEventRecords by their Resource Names.
        /// </summary>
        /// <param name="reverse">if set to <c>true</c>, will result in a reverse-order sort.</param>
        /// <returns>The comparer</returns>
		public static IComparer ByResourceName(bool reverse)
        {
            return new SortByResourceName(reverse);
        }

        /// <summary>
        /// Returns a comparer that can be used, for example, to sort ResourceEventRecords by their times of occurrence.
        /// </summary>
        /// <param name="reverse">if set to <c>true</c>, will result in a reverse-order sort.</param>
        /// <returns>The comparer</returns>
        public static IComparer ByTime(bool reverse)
        {
            return new SortByTime(reverse);
        }

        /// <summary>
        /// Returns a comparer that can be used, for example, to sort ResourceEventRecords by their Action types.
        /// </summary>
        /// <param name="reverse">if set to <c>true</c>, will result in a reverse-order sort.</param>
        /// <returns>The comparer</returns>
        public static IComparer ByAction(bool reverse)
        {
            return new SortByAction(reverse);
        }

        /// <summary>
        /// Returns a comparer that can be used, for example, to sort ResourceEventRecords by their serial numbers.
        /// </summary>
        /// <param name="reverse">if set to <c>true</c>, will result in a reverse-order sort.</param>
        /// <returns>The comparer</returns>
        public static IComparer BySerialNumber(bool reverse)
        {
            return new SortBySerialNumber(reverse);
        }

        /// <summary>
        /// An abstract class from which all Resource Event Record Comparers inherit.
        /// </summary>
        public abstract class RerComparer : IComparer
        {
            private readonly int _reverse;
            /// <summary>
            /// Creates a new instance of the <see cref="T:RERComparer"/> class.
            /// </summary>
            /// <param name="reverse">if set to <c>true</c>, will result in a reverse-order comparison.</param>
            protected RerComparer(bool reverse)
            {
                _reverse = reverse ? -1 : 1;
            }
            #region IComparer Members
            /// <summary>
            /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
            /// </summary>
            /// <param name="x">The first object to compare.</param>
            /// <param name="y">The second object to compare.</param>
            /// <returns>
            /// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
            /// </returns>
            /// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
            public abstract int Compare(object x, object y);
            #endregion
            protected int Flip(int i)
            {
                return i * _reverse;
            }
        }

        /// <summary>
        /// A comparer that can be used, for example, to sort ResourceEventRecords by their Resource Names.
        /// </summary>
        public class SortByResourceName : RerComparer
        {
            /// <summary>
            /// Creates a new instance of the <see cref="T:_SortByResourceName"/> class.
            /// </summary>
			public SortByResourceName() : base(false) { }
            /// <summary>
            /// Creates a new instance of the <see cref="T:_SortByResourceName"/> class.
            /// </summary>
            /// <param name="reverse">if set to <c>true</c>, will result in a reverse-order comparison.</param>
			public SortByResourceName(bool reverse) : base(reverse) { }
            #region IComparer Members
            /// <summary>
            /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
            /// </summary>
            /// <param name="x">The first object to compare.</param>
            /// <param name="y">The second object to compare.</param>
            /// <returns>
            /// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
            /// </returns>
            /// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
            public override int Compare(object x, object y)
            {
                return Flip(Comparer.Default.Compare(((ResourceEventRecord)x).Resource.Name, ((ResourceEventRecord)y).Resource.Name));
            }
            #endregion
        }

        /// <summary>
        /// A comparer that can be used, for example, to sort ResourceEventRecords by their times of occurrence.
        /// </summary>
        public class SortByTime : RerComparer
        {
            /// <summary>
            /// Creates a new instance of the <see cref="T:_SortByTime"/> class.
            /// </summary>
			public SortByTime() : base(false) { }
            /// <summary>
            /// Creates a new instance of the <see cref="T:_SortByTime"/> class.
            /// </summary>
            /// <param name="reverse">if set to <c>true</c>, will result in a reverse-order comparison.</param>
			public SortByTime(bool reverse) : base(reverse) { }
            #region IComparer Members
            /// <summary>
            /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
            /// </summary>
            /// <param name="x">The first object to compare.</param>
            /// <param name="y">The second object to compare.</param>
            /// <returns>
            /// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
            /// </returns>
            /// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
            public override int Compare(object x, object y)
            {
                return Flip(Comparer.Default.Compare(((ResourceEventRecord)x).When, ((ResourceEventRecord)y).When));
            }

            #endregion
        }

        /// <summary>
        /// A comparer that can be used, for example, to sort ResourceEventRecords by their serial numbers.
        /// </summary>
        public class SortBySerialNumber : RerComparer
        {
            /// <summary>
            /// Creates a new instance of the <see cref="T:_SortBySerialNumber"/> class.
            /// </summary>
			public SortBySerialNumber() : base(false) { }
            /// <summary>
            /// Creates a new instance of the <see cref="T:_SortBySerialNumber"/> class.
            /// </summary>
            /// <param name="reverse">if set to <c>true</c>, will result in a reverse-order comparison.</param>
			public SortBySerialNumber(bool reverse) : base(reverse) { }
            #region IComparer Members
            /// <summary>
            /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
            /// </summary>
            /// <param name="x">The first object to compare.</param>
            /// <param name="y">The second object to compare.</param>
            /// <returns>
            /// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
            /// </returns>
            /// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
            public override int Compare(object x, object y)
            {
                return Flip(Comparer.Default.Compare(((ResourceEventRecord)x).SerialNumber, ((ResourceEventRecord)y).SerialNumber));
            }

            #endregion
        }

        /// <summary>
        /// A comparer that can be used, for example, to sort ResourceEventRecords by their Action types.
        /// </summary>
        public class SortByAction : RerComparer
        {
            /// <summary>
            /// Creates a new instance of the <see cref="T:_SortByAction"/> class.
            /// </summary>
			public SortByAction() : base(false) { }
            /// <summary>
            /// Creates a new instance of the <see cref="T:_SortByAction"/> class.
            /// </summary>
            /// <param name="reverse">if set to <c>true</c>, will result in a reverse-order comparison.</param>
			public SortByAction(bool reverse) : base(reverse) { }
            #region IComparer Members
            /// <summary>
            /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
            /// </summary>
            /// <param name="x">The first object to compare.</param>
            /// <param name="y">The second object to compare.</param>
            /// <returns>
            /// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
            /// </returns>
            /// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
            public override int Compare(object x, object y)
            {
                return Flip(Comparer.Default.Compare(((ResourceEventRecord)x).Action, ((ResourceEventRecord)y).Action));
            }

            #endregion
        }

        /// <summary>
        /// Implemented by an object that can set the values of a ResourceEventRecord. Typically granted by the ResourceEventRecord itself, so that the RER can control who is able to modify it.
        /// </summary>
		public interface IEditor
        {
            /// <summary>
            /// Sets the available quantity of the desired resource, as recorded in the ResourceEventRecord.
            /// </summary>
            /// <param name="newValue">The new value.</param>
			void SetAvailable(double newValue);
            /// <summary>
            /// Sets the capacity of the desired resource, as recorded in the ResourceEventRecord.
            /// </summary>
            /// <param name="newValue">The new value.</param>
			void SetCapacity(double newValue);
            /// <summary>
            /// Sets the quantity desired of the desired resource, as recorded in the ResourceEventRecord.
            /// </summary>
            /// <param name="newValue">The new value.</param>
			void SetQuantityDesired(double newValue);
            /// <summary>
            /// Sets the quantity obtained of the desired resource, as recorded in the ResourceEventRecord.
            /// </summary>
            /// <param name="newValue">The new value.</param>
			void SetQuantityObtained(double newValue);
        }

        /// <summary>
        /// This ResourceEventRecord implementation's internal implementation of IEditor.
        /// </summary>
		internal class RerEditor : IEditor
        {
            private readonly ResourceEventRecord _rer;
            /// <summary>
            /// Creates a new instance of the <see cref="T:RerEditor"/> class.
            /// </summary>
            /// <param name="rer">The rer.</param>
			internal RerEditor(ResourceEventRecord rer)
            {
                _rer = rer;
            }
            /// <summary>
            /// Sets the available quantity of the desired resource, as recorded in the ResourceEventRecord.
            /// </summary>
            /// <param name="newValue">The new value.</param>
			public void SetAvailable(double newValue)
            {
                _rer.Available = newValue;
            }
            /// <summary>
            /// Sets the capacity of the desired resource, as recorded in the ResourceEventRecord.
            /// </summary>
            /// <param name="newValue">The new value.</param>
			public void SetCapacity(double newValue)
            {
                _rer._capacity = newValue;
            }
            /// <summary>
            /// Sets the quantity desired of the desired resource, as recorded in the ResourceEventRecord.
            /// </summary>
            /// <param name="newValue">The new value.</param>
			public void SetQuantityDesired(double newValue)
            {
                _rer._quantityDesired = newValue;
            }
            /// <summary>
            /// Sets the quantity obtained of the desired resource, as recorded in the ResourceEventRecord.
            /// </summary>
            /// <param name="newValue">The new value.</param>
			public void SetQuantityObtained(double newValue)
            {
                _rer._quantityObtained = newValue;
            }

        }
    }
}