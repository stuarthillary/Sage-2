/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Persistence;
using Highpoint.Sage.Utility.Mementos;
using System;
using System.Collections;
using System.Collections.Specialized;
using _Debug = System.Diagnostics.Debug;
// ReSharper disable UnusedMember.Local
// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable VirtualMemberNeverOverriden.Global

namespace Highpoint.Sage.SimCore
{

    /// <summary>
    /// NOTE: This is an older class whose utility may have been subsumed into other,
    /// newer .NET classes such as tuples and the mechanisms for lambda expressions.
    /// However, it is used in various places in the library, so it is retained.
    /// <para/>
    /// A SmartPropertyBag (SPB) is at its basic level, a collection of name/value pairs.
    /// The power of a smart property bag lies in the fact that entries in the bag
    /// can be any of a number of specialized types - 
    /// <para/><b>Simple data (Value) : </b>Any primitive that is convertible to a double, string or boolean.
    /// <para/><b>Expression : </b>An expression is a string such as "X + Y" that, when
    /// queried (assuming that X and Y are entries in the bag) is evaluated and returns 
    /// the result of the evaluation.
    /// <para/><b>Alias : </b>A name value pair that points to an entry in another SPB.
    /// <para/><b>Delegate : </b>A delegate to a method that returns a double. When this entry 
    /// is requested, the delegate is called and the resulting value is returned.
    /// <para/><b>SPB : </b>An entry in a SPB may be another SPB, which effectively
    /// becomes a child of this SPB. Thus, a SPB representing a truck may contain several
    /// other SPBs representing each load placed on that truck, and thereafter, the key
    /// "TruckA.LoadB.Customer" will retrieve, for example, a string containing the name or
    /// ID of the customer for whom load B is destined.<p></p>
    /// <para/><b>ISnapShottable : </b>Any arbitrary object can be stored in a SPB if it
    /// implements the ISnapshottable interface. The SPB enables storage of booleans, doubles,
    /// and strings through the use of internal classes SPBBooleanWrapper, SPBDoubleValueWrapper,
    /// and  SPBStringWrapper, respectively - each of which implements ISnapshottable.
    /// <hr></hr>
    /// A SmartPropertyBag can also be write-locked, allowing it temporarily or permanently
    /// to be read but not written.<p></p>
    /// A SmartPropertyBag also maintains a memento, an object that records the SPB's internal
    /// state and can restore that state at any time. This is useful for a model element
    /// that may need to be "rolled back" to a prior state. This memento is only recalculated
    /// when it is requested, and only the portions that have changed are re-recorded into the
    /// memento.<p></p>
    /// A SmartPropertyBag is useful when an application is required to support significant
    /// configurability at run time. An example might be a modeling tool that incorporates the
    /// concept of a vessel for chemical manufacturing, but does not know at the time of app
    /// design, all of the characteristics that will be of interest in that vessel, and all
    /// of the attachments that will be made available on that vessel at a later time or by
    /// the designer using the application.
    /// </summary>
    public class SmartPropertyBag : ISupportsMementos, IHasWriteLock, IEnumerable, ISPBTreeNode, IXmlPersistable
    {

        #region Private Fields
        private readonly MementoHelper _ssh;
        private static readonly bool _diagnostics = Diagnostics.DiagnosticAids.Diagnostics("SmartPropertyBag");
        private readonly WriteLock _writeLock = new WriteLock(true);
        private readonly IDictionary _dictionary = new Hashtable();
        private IMemento _memento;

        #endregion Private Fields

        #region Constructors
        /// <summary>
        /// Creates a SmartPropertyBag.
        /// </summary>
        public SmartPropertyBag()
        {
            _ssh = new MementoHelper(this, true);
        }

        #endregion Constructors

        /// <summary>
        /// Any item added to a SPB must implement this interface.
        /// </summary>
        public interface IHasValue : ISupportsMementos, IHasWriteLock
        {
            /// <summary>
            /// Retrieves the underlying value object contained in this entry.
            /// </summary>
            /// <returns>The underlying value object contained in this entry.</returns>
            object GetValue();
        }

        /// <summary>
        /// Fired whenever the memento maintained by this SPB has changed.
        /// </summary>
        public event MementoChangeEvent MementoChangeEvent
        {
            add
            {
                if (!_writeLock.IsWritable)
                    throw new WriteProtectionViolationException(this, _writeLock);
                _ssh.MementoChangeEvent += value;
            }
            remove
            {
                if (!_writeLock.IsWritable)
                    throw new WriteProtectionViolationException(this, _writeLock);
                _ssh.MementoChangeEvent -= value;
            }
        }

        /// <summary>
        /// Delegate that is implemented by a delegate that is being added to the SPB, returning a double.
        /// </summary>
        public delegate double SPBDoubleDelegate();

        /// <summary>
        /// Indicates if write operations on this equipment are permitted.
        /// </summary>
        public bool IsWritable => _writeLock.IsWritable;

        /// <summary>
        /// Indicates if this SPB is a leaf (whether it contains entries). Fulfills
        /// obligation incurred by implementing TreeNode.
        /// </summary>
        public bool IsLeaf => false;

        /// <summary>
        /// Allows the SPB to be treated as a writelock, to determine if it is write-protected.
        /// </summary>
        /// <param name="spb">The SPB whose writability is being queried.</param>
        /// <returns></returns>
        public static explicit operator WriteLock(SmartPropertyBag spb)
        {
            return spb._writeLock;
        }

        /// <summary>
        /// Determines whether this smart property bag contains the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        /// 	<c>true</c> if this smart property bag contains the specified key; otherwise, <c>false</c>.
        /// </returns>
		public bool Contains(object key)
        {
            string s = key as string;
            if (s != null)
            {
                return ExistsKey(s);
            }
            else
            {
                // ReSharper disable once TailRecursiveCall
                return Contains(key); // TODO: Get code coverage here for a test.
            }
        }

        #region SPB Contents' Inner Classes - all but SPBExpressionWrapper and DoubleDelegateWrapper are persistable.
        private class SPBAlias : IHasValue, ISPBTreeNode, IXmlPersistable
        {
            public event MementoChangeEvent MementoChangeEvent
            {
                add
                {
                    _ssh.MementoChangeEvent += value;
                }
                remove
                {
                    _ssh.MementoChangeEvent -= value;
                }
            }
            private MementoHelper _ssh;
            private SmartPropertyBag _spb;
            private IMemento _memento;
            private string _key;
            public SPBAlias(SmartPropertyBag whichBag, string key)
            {
                _ssh = new MementoHelper(this, true);
                _ssh.AddChild(whichBag.GetContentsOfKey(key));
                _spb = whichBag;
                _key = key;
                _memento = new SPBAliasMemento(this);
            }

            public string OtherKey => _key;
            public SmartPropertyBag OtherBag => _spb;

            public IMemento Memento
            {
                get
                {
                    return _memento;
                }
                set
                {
                    ((SPBAliasMemento)value).Load(this);
                }
            }

            public bool Equals(ISupportsMementos otherGuy)
            {
                if (!(otherGuy is SPBAlias))
                    return false;
                SPBAlias spba = (SPBAlias)otherGuy;
                if (_key.Equals(spba._key, StringComparison.Ordinal) && _spb.Equals(spba._spb))
                    return true;
                return false;
            }

            #region IHasValue Implementation
            public object GetValue()
            {
                object val = _spb[_key];
                if (val is IHasValue)
                    val = ((IHasValue)val).GetValue();
                return val;
            }
            public bool IsLeaf => true;
            public bool IsWritable => false;

            #endregion

            public bool HasChanged => _ssh.HasChanged;

            public bool ReportsOwnChanges => _ssh.ReportsOwnChanges;

            #region IXmlPersistable Members

            /// <summary>
            /// A default constructor, to be used for creating an empty object prior to reconstitution from a serializer.
            /// </summary>
            public SPBAlias()
            {
                _ssh = new MementoHelper(this, true);
                _memento = new SPBAliasMemento(this);
            }

            /// <summary>
            /// Serializes this object to the specified XmlSerializatonContext.
            /// </summary>
            /// <param name="xmlsc">The XmlSerializatonContext into which this object is to be stored.</param>
            public void SerializeTo(XmlSerializationContext xmlsc)
            {
                xmlsc.StoreObject("key", _key);
                xmlsc.StoreObject("Aliased_Bag", _spb);
            }

            /// <summary>
            /// Deserializes this object from the specified XmlSerializatonContext.
            /// </summary>
            /// <param name="xmlsc">The XmlSerializatonContext from which this object is to be reconstituted.</param>
            public void DeserializeFrom(XmlSerializationContext xmlsc)
            {
                _spb = (SmartPropertyBag)xmlsc.LoadObject("Aliased_Bag");
                _key = (string)xmlsc.LoadObject("key");
                _ssh.AddChild(_spb.GetContentsOfKey(_key));
            }

            #endregion

            private class SPBAliasMemento : IMemento
            {

                #region Private Fields
                private readonly SPBAlias _orig;
                private readonly object _value;

                #endregion

                public SPBAliasMemento(SPBAlias orig)
                {
                    _orig = orig;
                    _value = orig.GetValue();
                }
                public ISupportsMementos CreateTarget()
                {
                    return _orig;
                }

                public void Load(ISupportsMementos ism)
                {
                    SPBAlias alias = (SPBAlias)ism;
                    alias._key = _orig._key;
                    alias._spb = _orig._spb;
                    alias._ssh = new MementoHelper(alias, true);
                    alias._ssh.AddChild(alias._spb.GetContentsOfKey(alias._key));
                    alias._memento = new SPBAliasMemento(alias);

                    OnLoadCompleted?.Invoke(this);
                }

                public IDictionary GetDictionary()
                {
                    IDictionary retval = new ListDictionary();
                    retval.Add("Value", _value);
                    return retval;
                }
                public bool Equals(IMemento otheOneMemento)
                {
                    if (otheOneMemento == null)
                        return false;
                    if (this == otheOneMemento)
                        return true;
                    if (!(otheOneMemento is SPBAliasMemento))
                        return false;

                    SPBAliasMemento spbamOtherGuy = (SPBAliasMemento)otheOneMemento;
                    if (spbamOtherGuy._orig._spb != _orig._spb)
                        return false;
                    if (spbamOtherGuy._orig._key != _orig._key)
                        return false;
                    return spbamOtherGuy._value.Equals(_value);
                }

                /// <summary>
                /// This event is fired once this memento has completed its Load(ISupportsMementos ism) invocation.
                /// </summary>
                public event MementoEvent OnLoadCompleted;

                /// <summary>
                /// This holds a reference to the memento, if any, that contains this memento.
                /// </summary>
                /// <value></value>
                public IMemento Parent
                {
                    get; set;
                }
            }

        }

        private class SPBDoubleDelegateWrapper : IHasValue, ISPBTreeNode
        { // Not IXmlPersistable.
            public event MementoChangeEvent MementoChangeEvent
            {
                add
                {
                    _ssh.MementoChangeEvent += value;
                }
                remove
                {
                    _ssh.MementoChangeEvent -= value;
                }
            }
            SPBDoubleDelegate _del;
            MementoHelper _ssh;
            private object _lastValue = new object();
            public SPBDoubleDelegateWrapper(SPBDoubleDelegate del)
            {
                _ssh = new MementoHelper(this, false);
                _del = del;
            }

            public IMemento Memento
            {
                get
                {
                    _lastValue = GetValue();
                    return new SPBDoubleDelegateWrapperMemento(this);
                }
                set
                {
                    ((SPBDoubleDelegateWrapperMemento)value).Load(this);
                }
            }

            #region Implementation of IHasValue
            public object GetValue()
            {
                return _del();
            }
            public bool IsLeaf => true;
            public bool IsWritable => false;

            #endregion


            public bool Equals(ISupportsMementos otherGuy)
            {
                if (!(otherGuy is SPBDoubleDelegateWrapper))
                    return false;
                SPBDoubleDelegateWrapper spbdw = (SPBDoubleDelegateWrapper)otherGuy;
                if (_del == spbdw._del)
                    return true;
                return false;
            }

            public bool HasChanged => !_del().Equals(_lastValue);
            public bool ReportsOwnChanges => _ssh.ReportsOwnChanges;

            class SPBDoubleDelegateWrapperMemento : IMemento
            {

                #region Private Fields

                private readonly SPBDoubleDelegateWrapper _dw;
                private readonly double _value;
                #endregion

                public SPBDoubleDelegateWrapperMemento(SPBDoubleDelegateWrapper dw)
                {
                    _dw = dw;
                    _value = (double)dw.GetValue();
                }
                public ISupportsMementos CreateTarget()
                {
                    return _dw;
                }
                public void Load(ISupportsMementos ism)
                {
                    SPBDoubleDelegateWrapper spbdw = (SPBDoubleDelegateWrapper)ism;
                    spbdw._ssh = new MementoHelper(spbdw, false);
                    spbdw._del = _dw._del;

                    OnLoadCompleted?.Invoke(this);
                }
                public IDictionary GetDictionary()
                {
                    IDictionary retval = new ListDictionary();
                    retval.Add("Value", _value);
                    return retval;
                }

                public bool Equals(IMemento otheOneMemento)
                {
                    if (otheOneMemento == null)
                        return false;
                    if (this == otheOneMemento)
                        return true;
                    if (!(otheOneMemento is SPBDoubleDelegateWrapperMemento))
                        return false;

                    SPBDoubleDelegateWrapperMemento spbdwm = (SPBDoubleDelegateWrapperMemento)otheOneMemento;
                    if (spbdwm._dw.Equals(_dw) && spbdwm._value.Equals(_value))
                        return true;
                    return false;
                }

                /// <summary>
                /// This event is fired once this memento has completed its Load(ISupportsMementos ism) invocation.
                /// </summary>
                public event MementoEvent OnLoadCompleted;

                /// <summary>
                /// This holds a reference to the memento, if any, that contains this memento.
                /// </summary>
                /// <value></value>
                public IMemento Parent
                {
                    get; set;
                }
            }
        }

        private class SPBValueHolder : IHasValue, ISPBTreeNode, IXmlPersistable
        {
            public event MementoChangeEvent MementoChangeEvent
            {
                add
                {
                    _ssh.MementoChangeEvent += value;
                }
                remove
                {
                    _ssh.MementoChangeEvent -= value;
                }
            }
            private readonly MementoHelper _ssh;
            private double _value;
            public SPBValueHolder()
            {
                _ssh = new MementoHelper(this, true);
            }
            public IMemento Memento
            {
                get
                {
                    return new SPBValueHolderMemento(_value);
                }
                set
                {
                    _value = (double)((SPBValueHolderMemento)value).GetValue();
                }
            }

            public void SetValue(double val)
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (val != _value)
                {
                    _value = val;
                    _ssh.ReportChange();
                }
            }

            #region Implementation of IHasValue
            public object GetValue()
            {
                return _value;
            }
            public bool IsLeaf => true;
            public bool IsWritable => true;

            #endregion

            public bool HasChanged => _ssh.HasChanged;
            public bool ReportsOwnChanges => _ssh.ReportsOwnChanges;

            public static explicit operator double(SPBValueHolder spbvh)
            {
                return spbvh._value;
            }

            public bool Equals(ISupportsMementos otherGuy)
            {
                SPBValueHolder spbvh = otherGuy as SPBValueHolder;
                return _value == spbvh?._value;
            }

            #region >>> Serialization Support (incl. IXmlPersistable Members) <<<
            public void SerializeTo(XmlSerializationContext xmlsc)
            {
                //base.SerializeTo(node,xmlsc);
                xmlsc.StoreObject("Value", _value);
            }

            public void DeserializeFrom(XmlSerializationContext xmlsc)
            {
                //base.DeserializeFrom(xmlsc);
                _value = (double)xmlsc.LoadObject("Value");
            }
            #endregion

            class SPBValueHolderMemento : IMemento
            {

                #region Private Fields
                private readonly double _value;

                #endregion

                public SPBValueHolderMemento(double val)
                {
                    _value = val;
                }
                public ISupportsMementos CreateTarget()
                {
                    SPBValueHolder vh = new SPBValueHolder();
                    vh.SetValue(_value);
                    return vh;
                }
                public object GetValue()
                {
                    return _value;
                }
                public IDictionary GetDictionary()
                {
                    IDictionary retval = new ListDictionary();
                    retval.Add("Value", _value);
                    return retval;
                }

                public bool Equals(IMemento otheOneMemento)
                {
                    if (otheOneMemento == null)
                        return false;
                    if (this == otheOneMemento)
                        return true;

                    if (_value == (otheOneMemento as SPBValueHolderMemento)?._value)
                        return true;

                    return false;
                }

                public void Load(ISupportsMementos ism)
                {
                    ((SPBValueHolder)ism)._value = _value;

                    OnLoadCompleted?.Invoke(this);
                }

                /// <summary>
                /// This event is fired once this memento has completed its Load(ISupportsMementos ism) invocation.
                /// </summary>
                public event MementoEvent OnLoadCompleted;

                /// <summary>
                /// This holds a reference to the memento, if any, that contains this memento.
                /// </summary>
                /// <value></value>
                public IMemento Parent
                {
                    get; set;
                }
            }
        }

        private class SPBStringHolder : IHasValue, ISPBTreeNode, IXmlPersistable
        {
            public event MementoChangeEvent MementoChangeEvent
            {
                add
                {
                    _ssh.MementoChangeEvent += value;
                }
                remove
                {
                    _ssh.MementoChangeEvent -= value;
                }
            }
            private readonly MementoHelper _ssh;
            private string _value;
            public SPBStringHolder()
            {
                _ssh = new MementoHelper(this, true);
            }
            public IMemento Memento
            {
                get
                {
                    return new SPBStringHolderMemento(_value);
                }
                set
                {
                    _value = (string)((SPBStringHolderMemento)value).GetValue();
                }
            }

            public void SetValue(string val)
            {
                if (!val.Equals(_value, StringComparison.Ordinal))
                {
                    _value = val;
                    _ssh.ReportChange();
                }
            }

            #region Implementation of IHasValue
            public object GetValue()
            {
                return _value;
            }
            public bool IsLeaf => true;
            public bool IsWritable => true;

            #endregion

            public bool HasChanged => _ssh.HasChanged;
            public bool ReportsOwnChanges => _ssh.ReportsOwnChanges;

            public static explicit operator string(SPBStringHolder spbvh)
            {
                return spbvh._value;
            }

            public bool Equals(ISupportsMementos otherGuy)
            {
                if (!(otherGuy is SPBStringHolder))
                    return false;
                SPBStringHolder spbvh = (SPBStringHolder)otherGuy;
                return _value.Equals(spbvh._value, StringComparison.Ordinal);
            }

            #region >>> Serialization Support (incl. IXmlPersistable Members) <<<
            public void SerializeTo(XmlSerializationContext xmlsc)
            {
                //base.SerializeTo(node,xmlsc);
                xmlsc.StoreObject("Value", _value);
            }

            public void DeserializeFrom(XmlSerializationContext xmlsc)
            {
                //base.DeserializeFrom(xmlsc);
                _value = (string)xmlsc.LoadObject("Value");
            }
            #endregion

            class SPBStringHolderMemento : IMemento
            {

                #region Private Fields
                private readonly string _value;

                #endregion

                public SPBStringHolderMemento(string val)
                {
                    _value = val;
                }
                public ISupportsMementos CreateTarget()
                {
                    SPBStringHolder sh = new SPBStringHolder();
                    sh.SetValue(_value);
                    return sh;
                }
                public object GetValue()
                {
                    return _value;
                }
                public IDictionary GetDictionary()
                {
                    IDictionary retval = new ListDictionary();
                    retval.Add("Value", _value);
                    return retval;
                }

                public bool Equals(IMemento otheOneMemento)
                {
                    if (otheOneMemento == null)
                        return false;
                    if (this == otheOneMemento)
                        return true;
                    if (!(otheOneMemento is SPBStringHolderMemento))
                        return false;

                    if (_value.Equals(((SPBStringHolderMemento)otheOneMemento)._value, StringComparison.Ordinal))
                        return true;

                    return false;
                }

                public void Load(ISupportsMementos ism)
                {
                    ((SPBStringHolder)ism)._value = _value;

                    OnLoadCompleted?.Invoke(this);
                }

                /// <summary>
                /// This event is fired once this memento has completed its Load(ISupportsMementos ism) invocation.
                /// </summary>
                public event MementoEvent OnLoadCompleted;

                /// <summary>
                /// This holds a reference to the memento, if any, that contains this memento.
                /// </summary>
                /// <value></value>
                public IMemento Parent
                {
                    get; set;
                }
            }
        }

        private class SPBBooleanHolder : IHasValue, ISPBTreeNode, IXmlPersistable
        {
            public event MementoChangeEvent MementoChangeEvent
            {
                add
                {
                    _ssh.MementoChangeEvent += value;
                }
                remove
                {
                    _ssh.MementoChangeEvent -= value;
                }
            }
            private readonly MementoHelper _ssh;
            private bool _value;
            public SPBBooleanHolder()
            {
                _ssh = new MementoHelper(this, true);
            }
            public IMemento Memento
            {
                get
                {
                    return new SPBBooleanHolderMemento(_value);
                }
                set
                {
                    _value = (bool)((SPBBooleanHolderMemento)value).GetValue();
                }
            }

            public void SetValue(bool val)
            {
                if (!val.Equals(_value))
                {
                    _value = val;
                    _ssh.ReportChange();
                }
            }

            #region Implementation of IHasValue
            public object GetValue()
            {
                return _value;
            }
            public bool IsLeaf => true;
            public bool IsWritable => true;

            #endregion

            public bool HasChanged => _ssh.HasChanged;
            public bool ReportsOwnChanges => _ssh.ReportsOwnChanges;

            public static explicit operator bool(SPBBooleanHolder spbbh)
            {
                return spbbh._value;
            }

            public bool Equals(ISupportsMementos otherGuy)
            {
                if (!(otherGuy is SPBBooleanHolder))
                    return false;
                SPBBooleanHolder spbbh = (SPBBooleanHolder)otherGuy;
                return _value.Equals(spbbh._value);
            }

            #region >>> Serialization Support (incl. IXmlPersistable Members) <<<
            public void SerializeTo(XmlSerializationContext xmlsc)
            {
                //base.SerializeTo(node,xmlsc);
                xmlsc.StoreObject("Value", _value);
            }

            public void DeserializeFrom(XmlSerializationContext xmlsc)
            {
                //base.DeserializeFrom(xmlsc);
                _value = (bool)xmlsc.LoadObject("Value");
            }
            #endregion

            class SPBBooleanHolderMemento : IMemento
            {

                #region Private Fields
                private readonly bool _value;

                #endregion

                public SPBBooleanHolderMemento(bool val)
                {
                    _value = val;
                }
                public ISupportsMementos CreateTarget()
                {
                    SPBBooleanHolder bh = new SPBBooleanHolder();
                    bh.SetValue(_value);
                    return bh;
                }
                public object GetValue()
                {
                    return _value;
                }
                public IDictionary GetDictionary()
                {
                    IDictionary retval = new ListDictionary();
                    retval.Add("Value", _value);
                    return retval;
                }

                public bool Equals(IMemento otheOneMemento)
                {
                    if (otheOneMemento == null)
                        return false;
                    if (this == otheOneMemento)
                        return true;
                    if (!(otheOneMemento is SPBBooleanHolderMemento))
                        return false;

                    if (_value.Equals(((SPBBooleanHolderMemento)otheOneMemento)._value))
                        return true;

                    return false;
                }

                public void Load(ISupportsMementos ism)
                {
                    ((SPBBooleanHolder)ism)._value = _value;

                    OnLoadCompleted?.Invoke(this);
                }

                /// <summary>
                /// This event is fired once this memento has completed its Load(ISupportsMementos ism) invocation.
                /// </summary>
                public event MementoEvent OnLoadCompleted;

                /// <summary>
                /// This holds a reference to the memento, if any, that contains this memento.
                /// </summary>
                /// <value></value>
                public IMemento Parent
                {
                    get; set;
                }
            }
        }

        //		internal class SPBEnumerator : IEnumerator {
        //
        //			IEnumerator m_enumerator;
        //			public SPBEnumerator(SmartPropertyBag spb){
        //				m_enumerator = spb.m_dictionary.GetEnumerator();
        //			}
        //		
        //			#region Implementation of IEnumerator
        //			public void Reset() {
        //				m_enumerator.Reset();
        //			}
        //			public bool MoveNext() {
        //				return m_enumerator.MoveNext();
        //			}
        //			public object Current {
        //				get {
        //					return new SPBDictionaryEntry(m_enumerator.Current);
        //				}
        //			}
        //			#endregion
        //		}

        #endregion
        /// <summary>
        /// Retrieves an enumerator that cycles through all of the entries in this SPB.
        /// If the entry is not a leaf node, then it can have its enumerator invoked,
        /// allowing that entry's child list to be walked, and so forth.
        /// <p/>
        /// <code>
        /// private void DumpEnumerable( IEnumerable enumerable, int depth ) {
        ///		foreach ( HierarchicalDictionaryEntry hde in enumerable ) {
        ///			for ( int i = 0 ; i &lt; depth ; i++ ) Trace.Write("\t");
        ///			Trace.Write(hde.Key.ToString() + ", ");
        ///			Trace.Write(hde.Value.GetType() + ", ");
        ///			if ( hde.IsLeaf ) {
        ///				Trace.Write(hde.Value.ToString());
        ///				if ( hde.Value is double ) {
        ///		 			_Debug.WriteLine(" &lt;NOTE: this is a double.&gt;"); 
        ///				} else {
        ///					_Debug.WriteLine("");
        ///				}
        ///			} else {
        ///				_Debug.WriteLine("");
        ///				DumpEnumerable((IEnumerable)hde.Value,depth+1);
        ///			}
        ///		}
        /// </code>
        /// </summary>
        /// <returns>An enumerator that cycles through all of the entries in this SPB.</returns>
        public IEnumerator GetEnumerator()
        {
            ArrayList al = new ArrayList();
            foreach (DictionaryEntry de in _dictionary)
            {
                IHasValue value = de.Value as IHasValue;
                object val = (value != null ? value.GetValue() : de.Value);
                ISPBTreeNode node = de.Value as ISPBTreeNode;
                bool isLeaf = !(node != null && !node.IsLeaf);
                al.Add(new HierarchicalDictionaryEntry(de.Key, val, isLeaf));
            }
            return al.GetEnumerator();
        }

        #region Methods for adding things to, and removing them from, the SPB.

        /// <summary>
        /// Adds an alias to this SPB. An alias points to an entry in another SPB. The other SPB
        /// need not be a child of this SPB.
        /// </summary>
        /// <param name="key">The key in this SPB by which this alias will be known.</param>
        /// <param name="otherBag">The SPB to which this alias points.</param>
        /// <param name="otherKey">The key in the otherBag that holds the aliased object.</param>
        public void AddAlias(string key, SmartPropertyBag otherBag, string otherKey)
        {
            if (!_writeLock.IsWritable)
                throw new WriteProtectionViolationException(this, _writeLock);
            object obj = otherBag[otherKey];
            if (obj == null)
                throw new ApplicationException("SmartPropertyBag aliasing a key to a nonexistent 'other' key.");
            ISupportsMementos iss = new SPBAlias(otherBag, otherKey);
            AddSPBEntry(key, iss);
            _ssh.AddChild(iss);
        }

        /// <summary>
        /// Adds a child SPB to this SPB. A child SPB is one that is owned by this bag,
        /// and whose entries can be treated as sub-entries of this bag. For example, a
        /// if a bag, representing a pallet, were to contain another SPB under the key
        /// of "Crates", and that SPB contained one SPB for each crate (one of which was
        /// keyed as "123-45", and that SPB had a string keyed as "SKU" and another keyed
        /// as "Batch", then the following code would retrieve the SKU directly:
        /// <code>string theSKU = (string)myPallet["Crates.123-45.SKU"];</code>
        /// </summary>
        /// <param name="key">The key by which the child SPB is going to be known.</param>
        /// <param name="spb">The child SPB.</param>
        public void AddChildSPB(string key, SmartPropertyBag spb)
        {
            if (!_writeLock.IsWritable)
                throw new WriteProtectionViolationException(this, _writeLock);
            AddSPBEntry(key, spb);
            _ssh.AddChild(spb);
            _writeLock.AddChild((WriteLock)spb);
        }

        /// <summary>
        /// Adds a value (convertible to double) to the SPB under a specified key.
        /// </summary>
        /// <param name="key">The key by which the value will known and/or retrieved.</param>
        /// <param name="valConvertibleToDouble">An object that is convertible to a double.</param>
        public void AddValue(string key, object valConvertibleToDouble)
        {
            if (!_writeLock.IsWritable)
                throw new WriteProtectionViolationException(this, _writeLock);
            SPBValueHolder spbvh = new SPBValueHolder();
            spbvh.SetValue(Convert.ToDouble(valConvertibleToDouble));
            AddSPBEntry(key, spbvh);
            _ssh.AddChild(spbvh);
        }

        /// <summary>
        /// Adds a string value to the SPB under a specified key.
        /// </summary>
        /// <param name="key">The key by which the string value will known and/or retrieved.</param>
        /// <param name="val">The string that will be stored in the SPB.</param>
        public void AddString(string key, string val)
        {
            if (!_writeLock.IsWritable)
                throw new WriteProtectionViolationException(this, _writeLock);
            SPBStringHolder spbsh = new SPBStringHolder();
            spbsh.SetValue(val);
            AddSPBEntry(key, spbsh);
            _ssh.AddChild(spbsh);
        }

        /// <summary>
        /// Adds a boolean value to the SPB under a specified key.
        /// </summary>
        /// <param name="key">The key by which the boolean value will known and/or retrieved.</param>
        /// <param name="val">The boolean that will be stored in the SPB.</param>
        public void AddBoolean(string key, bool val)
        {
            if (!_writeLock.IsWritable)
                throw new WriteProtectionViolationException(this, _writeLock);
            SPBBooleanHolder spbbh = new SPBBooleanHolder();
            spbbh.SetValue(val);
            AddSPBEntry(key, spbbh);
            _ssh.AddChild(spbbh);
        }

        /// <summary>
        /// Any object can be stored in a SPB if it implements ISupportsMementos. This API
        /// performs such storage.
        /// </summary>
        /// <param name="key">The key under which the ISupportsMementos implementer is to 
        /// be known.</param>
        /// <param name="iss">the object that implements ISupportsMementos.</param>
        public void AddSnapshottable(string key, ISupportsMementos iss)
        {
            if (!_writeLock.IsWritable)
                throw new WriteProtectionViolationException(this, _writeLock);
            AddSPBEntry(key, iss);
            _ssh.AddChild(iss);
            // TODO: Get test coverage on the following code.
            // ReSharper disable once SuspiciousTypeConversion.Global
            WriteLock cwl = iss as WriteLock;
            if (cwl != null)
                _writeLock.AddChild(cwl);
        }

        /// <summary>
        /// Adds a delegate to the SPB under a specified key. When this entry is retrieved
        /// from the SPB, it will first be located by key, and then be evaluated by calling
        /// it, and the value returned from the delegate invocation will be returned to the
        /// entity calling into the SPB. Example:
        /// <code>
        /// SPBDoubleDelegate spbdd = new SPBDoubleDelegate(this.GetAValue);
        /// mySPB.AddDelegate("someValue",spbdd); // Add the delegate to the SPB.
        /// double theValue = mySPB["someValue"]; // calls into 'this.GetAValue()' and returns the answer.
        /// </code>
        /// </summary>
        /// <param name="key">The key by which the delegate's value will known and/or retrieved.</param>
        /// <param name="val">The delegate that will be stored in the SPB.</param>
        public void AddDelegate(string key, SPBDoubleDelegate val)
        {
            if (!_writeLock.IsWritable)
                throw new WriteProtectionViolationException(this, _writeLock);
            SPBDoubleDelegateWrapper spbdw = new SPBDoubleDelegateWrapper(val);
            AddSPBEntry(key, spbdw);
            _ssh.AddChild(spbdw);
        }

        /// <summary>
        /// Removes an object from this SPB.
        /// </summary>
        /// <param name="key">The key of the object that is being removed.</param>
        public void Remove(string key)
        {
            if (!_writeLock.IsWritable)
                throw new WriteProtectionViolationException(this, _writeLock);
            ISupportsMementos child = GetContentsOfKey(key);
            // TODO: Get test coverage on the following code.
            // ReSharper disable once SuspiciousTypeConversion.Global
            WriteLock cwl = child as WriteLock;
            if (cwl != null)
                _writeLock.RemoveChild(cwl);
            _ssh.RemoveChild(child);
            _dictionary.Remove(key);
        }

        /// <summary>
        /// Removes all objects from the SPB.
        /// </summary>
        public void Clear()
        {
            if (!_writeLock.IsWritable)
                throw new WriteProtectionViolationException(this, _writeLock);
            ArrayList keys = new ArrayList();
            foreach (DictionaryEntry de in _dictionary)
                keys.Add(de.Key);
            foreach (string key in keys)
            {
                WriteLock cwl = _dictionary[key] as WriteLock;
                if (cwl != null)
                    _writeLock.RemoveChild(cwl);
                Remove(key);
            }
            _ssh.ReportChange();
        }

        #endregion

        /// <summary>
        /// Retrieves an entry from this SPB. Compound keys may be specified if appropriate.
        /// For example, if a bag, representing a pallet, were to contain another SPB under
        /// the key of "Crates", and that SPB contained one SPB for each crate (one of which
        /// was keyed as "123-45", and that SPB had a string keyed as "SKU" and another keyed
        /// as "Batch", then the following code would retrieve the SKU directly:
        /// <code>string theSKU = (string)myPallet["Crates.123-45.SKU"];</code>
        /// </summary>
        public virtual object this[string key]
        {
            get
            {
                key = key.Trim();
                // NOTE: Do not use a key with a '.' in it.
                if (key.IndexOf('.', StringComparison.Ordinal) != -1)
                { // TODO: Speed this up. Checks all key values in all tables, currently.
                    string myKey = key.Substring(0, key.IndexOf('.', StringComparison.Ordinal));
                    string subsKey = key.Substring(key.IndexOf('.', StringComparison.Ordinal) + 1);
                    object subbag = GetContentsOfKey(myKey);
                    SmartPropertyBag bag = subbag as SmartPropertyBag;
                    if (bag != null)
                    {
                        return bag[subsKey];
                    }
                    else
                    {
                        string msg = "SmartPropertyBag called with key, \"" + key + "\", but the contents " +
                            "of key \"" + myKey + "\" was not a SmartPropertyBag, so the key \"" + subsKey +
                            "\" cannot be retrieved from it.";
                        throw new SmartPropertyBagContentsException(msg);
                    }
                }
                object retval = _dictionary[key];
                IHasValue value = retval as IHasValue;
                return value != null ? value.GetValue() : retval;
            }
            set
            {
                if (!_writeLock.IsWritable)
                    throw new WriteProtectionViolationException(this, _writeLock);
                key = key.Trim();
                if (key.IndexOf('.', StringComparison.Ordinal) != -1)
                {
                    // It's formatted to be a set from a subsidiary SPB.
                    string myKey = key.Substring(0, key.IndexOf('.', StringComparison.Ordinal));
                    string subsKey = key.Substring(key.IndexOf('.', StringComparison.Ordinal) + 1);
                    object subbag = GetContentsOfKey(myKey);
                    SmartPropertyBag bag = subbag as SmartPropertyBag;
                    if (bag != null)
                    {
                        bag[subsKey] = value;
                    }
                    else
                    {
                        string msg = "SmartPropertyBag called with key, \"" + key + "\", but the contents " +
                            "of key \"" + myKey + "\" was not a SmartPropertyBag, so the key \"" + subsKey +
                            "\" cannot be retrieved from it.";
                        throw new SmartPropertyBagContentsException(msg);
                    }
                }
                else
                { // The specified contents are located in this SPB
                    try
                    {
                        object val = GetContentsOfKey(key);
                        SPBValueHolder holder = val as SPBValueHolder;
                        if (holder != null)
                            holder.SetValue(Convert.ToDouble(value));
                        else if (val is SPBStringHolder)
                            ((SPBStringHolder)val).SetValue((string)value);
                        else
                        {
                            (val as SPBBooleanHolder)?.SetValue(Convert.ToBoolean(value));
                        }
                    }
                    catch (InvalidCastException)
                    {
                        throw new SmartPropertyBagContentsException("Error setting value into a SPB. Can set data only into a key " +
                            "that contains a data element convertible to System.Double.");
                    }
                    catch (FormatException)
                    {
                        throw new SmartPropertyBagContentsException("Error setting value into a SPB. Must be convertible to a double.");
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves the contents of a key known to exist in THIS SPB.
        /// Throws a SmartPropertyBagContentsException if the key does
        /// not exist in this bag.
        /// </summary>
        /// <param name="key">The key under which the lookup is to be
        /// performed. Compound keys are not permitted, here.</param>
        /// <returns>The contents of the key.</returns>
        protected ISupportsMementos GetContentsOfKey(string key)
        {
            ISupportsMementos contents = (ISupportsMementos)_dictionary[key];
            if (contents == null)
            {
                string msg = "Application code called SmartPropertyBag with a key, \"" + key +
                    "\", that does not exist in this SmartPropertyBag.";
                throw new SmartPropertyBagContentsException(msg);
            }
            return contents;
        }

        /// <summary>
        /// Returns true if this dictionary (or any dictionary below it)
        /// contains a value stored under this key.
        /// </summary>
        /// <param name="key">The key under which the lookup is to be
        /// performed. Compound keys are not permitted, here.</param>
        /// <returns>The contents of the key.</returns>
        protected bool ExistsKey(string key)
        {
            int firstDotNdx = key.IndexOf('.', StringComparison.Ordinal);
            if (firstDotNdx != -1)
            {
                string lclKey = key.Substring(0, firstDotNdx);
                string subKey = key.Substring(firstDotNdx + 1, key.Length - firstDotNdx - 1);
                ISupportsMementos ism = (ISupportsMementos)this[lclKey];
                SmartPropertyBag bag = ism as SmartPropertyBag;
                if (bag != null)
                {
                    return bag.ExistsKey(subKey);
                }
                else
                {
                    return false;
                }
            }
            return _dictionary.Contains(key);
        }

        private void AddSPBEntry(string key, object payload)
        {
            int firstDotNdx = key.IndexOf('.', StringComparison.Ordinal);
            if (firstDotNdx != -1)
            {
                string lclKey = key.Substring(0, firstDotNdx);
                string subKey = key.Substring(firstDotNdx + 1, key.Length - firstDotNdx - 1);
                ISupportsMementos ism = (ISupportsMementos)this[lclKey];
                if (ism == null)
                {
                    AddChildSPB(lclKey, new SmartPropertyBag());
                    ism = (ISupportsMementos)this[lclKey];
                }
                else if (!(ism is SmartPropertyBag))
                {
                    throw new ApplicationException("Attempt to add a key to SmartPropertyBag, " + key + ", but the key, " + lclKey + " already exists, and is a leaf node - no sub-bag can be created.");
                }
                else
                {
                    // It's an SPB that already exists, so we're golden.
                }
                ((SmartPropertyBag)ism).AddSPBEntry(subKey, payload);
            }
            else
            {
                // No 'dot', so it's a leaf-node add.
                _dictionary.Add(key, payload);
            }
        }


        /// <summary>
        /// Retrieves the memento of this SPB. Includes all state from this bag,
        /// other bags' aliased entries, and child bags, as well as the mementos
        /// from any entry that implements ISupportsMementos. Optimizations are
        /// applied such that a minimum of computation is required to perform the
        /// extraction.
        /// </summary>
        public IMemento Memento
        {
            get
            {
                if (_ssh.HasChanged)
                {
                    _memento = new SmartPropertyBagMemento(this);
                    _ssh.ReportSnapshot();
                }
                return _memento;
            }
            set
            {
                if (!_writeLock.IsWritable)
                    throw new WriteProtectionViolationException(this, _writeLock);
                if (!_ssh.HasChanged && _memento.Equals(value))
                    return;
                SmartPropertyBagMemento spbm = (SmartPropertyBagMemento)value;
                spbm.Load(this);
            }
        }

        /// <summary>
        /// True if this SPB has changed in any way since the last time it was
        /// snapshotted.
        /// </summary>
        public bool HasChanged => _ssh.HasChanged;

        /// <summary>
        /// True if this SPB is capable of reporting its own changes.
        /// </summary>
        public bool ReportsOwnChanges => _ssh.ReportsOwnChanges;

        /// <summary>
        /// Returns true if the two SPBs are semantically equal. (In other words,
        /// if both have the same entries, and each evaluates as being '.Equal()'
        /// to its opposite, then the two bags are equal.)
        /// </summary>
        /// <param name="otherGuy">The other SPB. If it is an ISupportsMementos that
        /// is not also a SPB, it will return false.</param>
        /// <returns>True if the two SPBs are semantically equal.</returns>
        public bool Equals(ISupportsMementos otherGuy)
        {
            SmartPropertyBag spb = otherGuy as SmartPropertyBag;
            if (_dictionary.Count != spb?._dictionary.Count)
                return false;
            foreach (DictionaryEntry de in spb._dictionary)
            {
                if (!_dictionary.Contains(de.Key))
                    return false;
                ISupportsMementos myValue = (ISupportsMementos)_dictionary[de.Key];

                if (!(((ISupportsMementos)de.Value).Equals(myValue)))
                    return false;
            }
            return true;
        }

        private static bool DictionariesAreEqual(IDictionary dict1, IDictionary dict2)
        {
            if (dict1 == null && dict2 == null)
            {
                //_Debug.WriteLine("Both are null.");
                return true;
            }
            if (dict1 == null || dict2 == null)
            {
                //_Debug.WriteLine("One or the other is null.");
                return false;
            }
            if (dict1.Count != dict2.Count)
            {
                if (_diagnostics)
                {
                    _Debug.WriteLine("Two dictionaries have a different item count.");
                    foreach (DictionaryEntry de in dict1)
                        _Debug.WriteLine(de.Key + ", " + de.Value);
                    foreach (DictionaryEntry de in dict2)
                        _Debug.WriteLine(de.Key + ", " + de.Value);
                }
                return false;
            }
            foreach (DictionaryEntry de in dict1)
            {
                //_Debug.WriteLine("Comparing " + de.Key.ToString());
                if (!dict2.Contains(de.Key))
                    return false;
                object val1 = de.Value;
                object val2 = dict2[de.Key];
                if (val1 == null && val2 == null)
                    continue;
                if (val1 == null || val2 == null)
                    return false;
                //_Debug.WriteLine("Both have it. One is " + val1.ToString() + ", and the other is " + val2.ToString());
                IDictionary d1 = val1 as IDictionary;
                IDictionary d2 = val2 as IDictionary;
                if (d1 != null && d2 != null)
                {
                    //_Debug.WriteLine("Performing dictionary comparison of " + val1 + " and " + val2 );
                    if (!DictionariesAreEqual(d1, d2))
                        return false;
                }
                else
                {
                    // it's an object.
                    //_Debug.WriteLine("Comparing non-dictionary items " + val1.ToString() + ", and " + val2.ToString());
                    if (!val1.Equals(val2))
                        return false;
                }
            }
            return true;
        }

        private class SmartPropertyBagMemento : IMemento
        {

            #region Private Fields
            private readonly IDictionary _mementoDict;
            private readonly SmartPropertyBag _spb;

            #endregion

            public SmartPropertyBagMemento(SmartPropertyBag spb)
            {
                _spb = spb;
                if (spb._dictionary.Count <= 10)
                {
                    _mementoDict = new ListDictionary();
                }
                else
                {
                    _mementoDict = new Hashtable();
                }
                foreach (DictionaryEntry de in spb._dictionary)
                {
                    ISupportsMementos value = de.Value as ISupportsMementos;
                    if (value != null)
                    {
                        ISupportsMementos val = value;
                        IMemento memento = val.Memento;
                        memento.Parent = this;
                        _mementoDict.Add(de.Key, memento);
                    }
                    else
                    {
                        throw new ApplicationException("Trying to snapshot a " + de.Value + ", which is not snapshottable.");
                    }
                }
            }

            public void Load(ISupportsMementos ism)
            {
                SmartPropertyBag spb = (SmartPropertyBag)ism;
                spb._dictionary.Clear();
                spb._ssh.Clear();

                ////// (FIXED)H_A_C_K: 20070220
                ////// The following is in place because: When reloading unit state, we can, depending on the
                ////// order in which keys appear in the memento dictionary, restore the mixture before restoring
                ////// the volume in which that mixture is contained. This would not be a problem, except that 
                ////// the mixture recalculates its characteristics based on the volume of the container in which
                ////// it is contained - which, if it has not yet been restored, can lead to a null reference. Thus, 
                ////// the h_a_c_k is that we force mixture to restore last, after volume, so that the restoration & 
                //////recalculation has everything it needs. Better solution will be worked on immediately to follow.
                ////ArrayList dictEntries = new ArrayList();
                ////foreach ( DictionaryEntry de in m_mementoDict ) {
                ////    if ( !de.Key.Equals("Mixture") ) {
                ////        dictEntries.Add(de);
                ////    }
                ////}
                ////foreach (DictionaryEntry de in m_mementoDict) {
                ////    if (de.Key.Equals("Mixture")) {
                ////        dictEntries.Add(de);
                ////    }
                ////}
                // FIX (20071124) : An ISupportsMementos implementer can be loaded from a properly-selected IMemento.
                // The problem comes in when that implementer's initialization requires access to another 
                // ISupportsMementos implementer that has not yet been reconstituted. So we are adding an
                // OnLoadCompleted event and IMemento Parent { get; } field to IMemento so that a load method that
                // needs to perform some activity that depends on something else in the deserialization train, can
                // register for a callback after deserialization completes.
                // 

                ArrayList dictEntries = new ArrayList();
                foreach (DictionaryEntry de in _mementoDict)
                {
                    dictEntries.Add(de);
                }

                foreach (DictionaryEntry de in dictEntries)
                {

                    if (_diagnostics)
                        _Debug.WriteLine("Reloading " + spb + " with " + de.Key + " = " + de.Value);
                    string key = (string)de.Key;
                    ISupportsMementos child = ((IMemento)de.Value).CreateTarget();
                    ((IMemento)de.Value).Load(child);

                    spb.AddSnapshottable(key, child);
                }
                spb._memento = this;
                spb._ssh.HasChanged = false;

                OnLoadCompleted?.Invoke(this);
            }

            public ISupportsMementos CreateTarget()
            {
                _spb._dictionary.Clear();
                foreach (DictionaryEntry de in _mementoDict)
                {
                    string key = (string)de.Key;
                    object val = ((IMemento)de.Value).CreateTarget();
                    _spb.AddSPBEntry(key, val);
                }
                return _spb;
            }

            public IDictionary GetDictionary()
            {
                Hashtable retval = new Hashtable();
                foreach (DictionaryEntry de in _mementoDict)
                {
                    retval.Add(de.Key, ((IMemento)de.Value).GetDictionary());
                }
                return retval;
            }

            public bool Equals(IMemento otheOneMemento)
            {
                if (otheOneMemento == null)
                    return false;
                return DictionariesAreEqual(GetDictionary(), otheOneMemento.GetDictionary());
            }

            /// <summary>
            /// This event is fired once this memento has completed its Load(ISupportsMementos ism) invocation.
            /// </summary>
            public event MementoEvent OnLoadCompleted;

            /// <summary>
            /// This holds a reference to the memento, if any, that contains this memento.
            /// </summary>
            /// <value></value>
            public IMemento Parent
            {
                get; set;
            }
        }
        #region >>> Serialization Support (incl. IXmlPersistable Members) <<<
        /// <summary>
        /// Stores this object to the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
        public virtual void SerializeTo(XmlSerializationContext xmlsc)
        {
            //base.SerializeTo(node,xmlsc);
            xmlsc.StoreObject("EntryCount", _dictionary.Count);

            int i = 0;
            foreach (DictionaryEntry de in _dictionary)
            {
                xmlsc.StoreObject("Entry_" + i, de);
                i++;
            }
        }

        /// <summary>
        /// Reconstitutes this object from the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
		public virtual void DeserializeFrom(XmlSerializationContext xmlsc)
        {
            //base.DeserializeFrom(xmlsc);
            int entryCount = (int)xmlsc.LoadObject("EntryCount");

            for (int i = 0; i < entryCount; i++)
            {
                DictionaryEntry de = (DictionaryEntry)xmlsc.LoadObject("Entry_" + i);
                AddSPBEntry((string)de.Key, de.Value);
            }
        }
        #endregion
    }
}