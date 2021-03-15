/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;
using System.Reflection;
using System.Xml;
using _Debug = System.Diagnostics.Debug;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace Highpoint.Sage.Persistence
{
    /// <summary>
    /// An ISerializer that knows how to store a wide range of objects into, and retrieve them
    /// from, an XML document.
    /// </summary>
    public class XmlSerializationContext : ISerializer
    {

        #region Private Fields
        private readonly ISerializer _delegateXmlSerializer;
        private readonly ISerializer _typeXmlSerializer;
        private readonly Hashtable _serializers;
        private XmlDocument _rootDoc;
        private XmlNode _archive;
        private XmlNode _typeCatalog;
        private Hashtable _objectsByKey;
        private Hashtable _keysByObject;
        private Hashtable _typesByIndex;
        private Hashtable _indexesByType;
        private ISerializer _enumXmlSerializer;
        private Stack _nodeCursor;
        private int _typeNum;
        private int _objectNum;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlSerializationContext"/> class.
        /// </summary>
        public XmlSerializationContext()
        {
            Reset();
            _serializers = new Hashtable
            {
                {typeof (string), new StringXmlSerializer(this)},
                {typeof (double), new DoubleXmlSerializer(this)},
                {typeof (sbyte), new SByteXmlSerializer(this)},
                {typeof (byte), new ByteXmlSerializer(this)},
                {typeof (short), new ShortXmlSerializer(this)},
                {typeof (ushort), new UShortXmlSerializer(this)},
                {typeof (int), new IntXmlSerializer(this)},
                {typeof (uint), new UintXmlSerializer(this)},
                {typeof (long), new LongXmlSerializer(this)},
                {typeof (bool), new BoolXmlSerializer(this)},
                {typeof (DateTime), new DateTimeXmlSerializer(this)},
                {typeof (TimeSpan), new TimeSpanXmlSerializer(this)},
                {typeof (Guid), new GuidXmlSerializer(this)},
                {typeof (Hashtable), new HashtableXmlSerializer(this)},
                {typeof (ArrayList), new ArrayListXmlSerializer(this)},
                {typeof (Type), new TypeXmlSerializer(this)},
                {typeof (DictionaryEntry), new DictionaryEntryXmlSerializer(this)}
            };

            //m_serializers.Add(typeof(Array),new ArrayXmlSerializer(this));

            _enumXmlSerializer = new EnumXmlSerializer(this);
            _delegateXmlSerializer = new DelegateXmlSerializer(this);
            _typeXmlSerializer = new TypeXmlSerializer(this);
        }

        /// <summary>
        /// Determines whether the XmlSerializationContext contains a serializer for the specified target type.
        /// </summary>
        /// <param name="targetType">Type of the target.</param>
        /// <returns><c>true</c> if the XmlSerializationContext contains a serializer for the specified target type; otherwise, <c>false</c>.</returns>
        public bool ContainsSerializer(Type targetType)
        {
            return _serializers.ContainsKey(targetType);
        }

        /// <summary>
        /// Adds a serializer to the XmlSerializationContext for the specified target type.
        /// </summary>
        /// <param name="targetType">Type of the target.</param>
        /// <param name="serializer">The serializer.</param>
        public void AddSerializer(Type targetType, ISerializer serializer)
        {
            _serializers.Add(targetType, serializer);
        }

        /// <summary>
        /// Removes the serializer from the XmlSerializationContext for the specified target type.
        /// </summary>
        /// <param name="targetType">Type of the target.</param>
        public void RemoveSerializer(Type targetType)
        {
            _serializers.Remove(targetType);
        }

        /// <summary>
        /// Pushes the XmlNode into the serializer.
        /// </summary>
        /// <param name="node">The node.</param>
        public void PushNode(XmlNode node)
        {
            _nodeCursor.Push(node);
        }

        /// <summary>
        /// Pops an XmlNode off the serializer.
        /// </summary>
        /// <returns>XmlNode.</returns>
        public XmlNode PopNode()
        {
            return (XmlNode)_nodeCursor.Pop();
        }

        /// <summary>
        /// Gets the current node.
        /// </summary>
        /// <value>The current node.</value>
        public XmlNode CurrentNode => (XmlNode)_nodeCursor.Peek();

        #region ISerializer Members
        /// <summary>
        /// Persists the object 'obj' to an XmlNode, and appends that node under
        /// the XmlSerializationContext's CurrentNode node.
        /// </summary>
        /// <param name="key">In it's ToString() form, this will be the name of the new node.</param>
        /// <param name="obj">This is the object that will be serialized to the new node.</param>
        /// <returns>The XmlNode that was created.</returns>
        public void StoreObject(object key, object obj)
        {
            XmlNode currentNode = CurrentNode;
            if (obj == null)
            { // ----------------------- It's null.
                XmlNode node = CreateNullNode(ref _rootDoc, key.ToString());
                currentNode.AppendChild(node);
            }
            else if (_keysByObject.Contains(obj))
            { // ---- We've seen it before.
                XmlNode node = CreateRefNode(key, _keysByObject[obj]);
                currentNode.AppendChild(node);
            }
            else if (_serializers.Contains(obj.GetType()))
            {
                ((ISerializer)_serializers[obj.GetType()]).StoreObject(key, obj);
            }
            else if (obj is Enum)
            {
                _enumXmlSerializer.StoreObject(key, obj);
            }
            else if (obj is Delegate)
            {
                _delegateXmlSerializer.StoreObject(key, obj);
            }
            else if (obj is Type)
            {
                _typeXmlSerializer.StoreObject(key, obj);
            }
            else
            { // ----------------------------------- It's a new object.
                object oid = GetOidForObject(obj);
                _keysByObject.Add(obj, oid);
                XmlNode node = CreateEmptyNode(key.ToString(), obj.GetType(), oid);
                currentNode.AppendChild(node);
                PushNode(node);
                try
                {
                    ((IXmlPersistable)obj).SerializeTo(this);
                }
                catch (InvalidCastException ice)
                {
                    _Debug.WriteLine(ice.Message);
                    throw new ApplicationException("Attempt to serialize an object of type " + obj.GetType() + " failed. It does not implement IXmlPersistable.");
                }
                PopNode();
                //return node;
            }
        }

        /// <summary>
        /// Loads (reconstitutes) an object from an archive object, and returns
        /// the object. If the object has already been reconstituted (i.e. the
        /// reference being deserialized is the second or later reference to an
        /// object, the original object is located and a reference to it is returned.
        /// </summary>
        /// <param name="key">The key whose node under 'archive' is to be deserialized.</param>
        /// <returns>The deserialized object.</returns>
        public object LoadObject(object key)
        {
            XmlNode node = CurrentNode.SelectSingleNode(key.ToString());
            object retval;

            // If the node contains null, return null.
            if (NodeIsNull(node))
                return null;

            if (NodeIsRef(node))
            {
                object oid = GetOidFromNode(node);
                if (_objectsByKey.Contains(oid))
                {
                    retval = _objectsByKey[oid];
                }
                else
                {
                    throw new ApplicationException("Couldn't find a referenced object");
                }
            }
            else
            {

                // If the node contains a referenced object, return that object.
                // There is stuff to be deserialized.
                Type type = GetTypeFromNode(node);
                if (_serializers.Contains(type))
                {
                    // If it's a type that has a custom deserializer, then we call it.
                    ISerializer serializer = (ISerializer)_serializers[type];
                    retval = serializer.LoadObject(key);
                }
                else if (type.IsEnum)
                {
                    retval = _enumXmlSerializer.LoadObject(key);
                }
                else if (typeof(Delegate).IsAssignableFrom(type))
                {
                    retval = _delegateXmlSerializer.LoadObject(key);
                }
                else
                {
                    // We will need to create the object using the default mechanism.
                    retval = CreateEmptyObject(node);
                    _objectsByKey.Add(GetOidFromNode(node), retval);
                    PushNode(node);
                    ((IXmlPersistable)retval).DeserializeFrom(this);
                    PopNode();
                }
            }
            return retval;
        }

        public Hashtable ContextEntities
        {
            get; private set;
        }

        /// <summary>
        /// Resets this context. Clears the document, object cache, node stack and hashtables.
        /// </summary>
        public void Reset()
        {
            _rootDoc = new XmlDocument();
            _archive = _rootDoc.AppendChild(_rootDoc.CreateElement(archive));
            _typeCatalog = _archive.AppendChild(_rootDoc.CreateElement(type_Catalog));
            _objectsByKey = new Hashtable();
            _keysByObject = new Hashtable();
            _typesByIndex = new Hashtable();
            _indexesByType = new Hashtable();
            ContextEntities = new Hashtable();
            _enumXmlSerializer = new EnumXmlSerializer(this);
            _nodeCursor = new Stack();
            _nodeCursor.Push(_archive);
            _typeNum = 0;
            _objectNum = 0;
        }

        #endregion

        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        /// <summary>
        /// Gets or sets a value indicating whether this context is using a type catalog. Using a type
        /// catalog makes for better compression of an XML file, at the cost of minimally slower performance.
        /// </summary>
        /// <value><c>true</c> if [use catalog]; otherwise, <c>false</c>.</value>
        public bool UseCatalog { get; set; } = true;

        /// <summary>
        /// Populates a new XmlSerializationContext from a specified filename.
        /// </summary>
        /// <param name="filename">The filename.</param>
        public void Load(string filename)
        {
            _rootDoc.Load(filename);
            _archive = _rootDoc.SelectSingleNode(archive);
            _objectsByKey.Clear();
            _keysByObject.Clear();
            _nodeCursor.Clear();
            _nodeCursor.Push(_archive);
            _typeNum = 0;
            _objectNum = 0;
            LoadTypeCatalog();
        }

        /// <summary>
        /// Saves the XmlSerializationContext from a specified filename.
        /// </summary>
        /// <param name="filename">The filename.</param>
        public void Save(string filename)
        {
            _rootDoc.Save(filename);
        }

        private XmlNode CreateEmptyNode(string name, Type type, object oid)
        {
            //_Debug.WriteLine("Creating a node of type " + type.ToString() + " to store " + name );

            XmlNode node;
            try
            {
                node = _rootDoc.CreateNode(XmlNodeType.Element, name, null);
            }
            catch (XmlException xmlex)
            {
                _Debug.WriteLine(xmlex.Message);
                throw;
            }
            _Debug.Assert(node.Attributes != null, "node.Attributes != null");

            XmlAttribute attr;
            if (oid != null)
            { // Primitives won't have an oid.
                attr = _rootDoc.CreateAttribute(ref_Id_Label);
                attr.InnerText = oid.ToString();
                node.Attributes.Append(attr);
            }

            if (UseCatalog)
            {
                if (!_indexesByType.Contains(type))
                    AddTypeToCatalog(type);
                object typeIndex = _indexesByType[type];

                attr = _rootDoc.CreateAttribute(typekey_Label);
                attr.InnerText = typeIndex.ToString();
                node.Attributes.Append(attr);
                attr = _rootDoc.CreateAttribute(assy_Label);
                attr.InnerText = type.Assembly.FullName;
                node.Attributes.Append(attr);
            }
            else
            {
                attr = _rootDoc.CreateAttribute(type_Label);
                attr.InnerText = type.FullName;
                node.Attributes.Append(attr);
                attr = _rootDoc.CreateAttribute(assy_Label);
                attr.InnerText = type.Assembly.FullName;
                node.Attributes.Append(attr);
            }
            return node;
        }

        private static XmlNode CreateNullNode(ref XmlDocument root, string name)
        {
            XmlNode node = root.CreateNode(XmlNodeType.Element, name, null);
            XmlAttribute attr = root.CreateAttribute(null_Label);
            attr.InnerText = "true";
            _Debug.Assert(node.Attributes != null, "node.Attributes != null");
            node.Attributes.Append(attr);
            return node;
        }

        private static bool NodeIsNull(XmlNode node)
        {
            _Debug.Assert(node.Attributes != null, "node.Attributes != null");
            XmlAttribute nullness = node.Attributes[null_Label];
            return nullness != null && nullness.InnerText.Equals("true");
        }

        private static bool NodeIsRef(XmlNode node)
        {
            _Debug.Assert(node.Attributes != null, "node.Attributes != null");
            XmlAttribute refness = node.Attributes[isref_Label];
            return refness != null && refness.InnerText.Equals("true");
        }

        /// <summary>
        /// Retrieves an object's type from that object's node. Goes through the
        /// type catalog if we're using one, or reads the node's type information
        /// directly, if we're not.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private Type GetTypeFromNode(XmlNode node)
        {
            _Debug.Assert(node.Attributes != null, "node.Attributes != null");

            if (UseCatalog)
            {
                string typeId = node.Attributes[typekey_Label].InnerText;
                //				if ( !m_typesByIndex.Contains(typeID) ) {
                //					LoadTypeCatalog();
                //				}
                return (Type)_typesByIndex[typeId];
            }
            else
            {
                Assembly assy = GetAssemblyFromNode(node);
                XmlAttribute typeAttr = node.Attributes[type_Label];
                if (typeAttr == null || assy == null)
                    return null;
                return assy.GetType(typeAttr.InnerText);
            }
        }

        private static Assembly GetAssemblyFromNode(XmlNode node)
        {
            _Debug.Assert(node.Attributes != null, "node.Attributes != null");
            XmlAttribute assyAttr = node.Attributes[assy_Label];
            if (assyAttr == null)
                return null;
            string assyName = assyAttr.InnerText;
            return Assembly.Load(assyName);
        }

        private void AddTypeToCatalog(Type type)
        {

            object oid = GetOidForType(type);
            _indexesByType.Add(type, oid);
            _typesByIndex.Add(oid, type);
            XmlNode typeNode = _rootDoc.CreateElement(oid.ToString());
            _typeCatalog.AppendChild(typeNode);
            XmlAttribute attr = _rootDoc.CreateAttribute(type_Label);
            attr.InnerText = type.FullName;
            _Debug.Assert(typeNode.Attributes != null, "typeNode.Attributes != null");
            typeNode.Attributes.Append(attr);
            attr = _rootDoc.CreateAttribute(assy_Label);
            attr.InnerText = type.Assembly.FullName;
            typeNode.Attributes.Append(attr);
        }

        private void LoadTypeCatalog()
        {
            _typeCatalog = _rootDoc.SelectSingleNode(archive + "/" + type_Catalog);
            _typesByIndex.Clear();
            _indexesByType.Clear();
            Type type = null;
            _Debug.Assert(_typeCatalog?.ChildNodes != null, "m_typeCatalog?.ChildNodes != null");
            foreach (XmlNode typeNode in _typeCatalog?.ChildNodes)
            {
                string typeKey = typeNode.Name;

                Assembly assy = GetAssemblyFromNode(typeNode);
                _Debug.Assert(typeNode.Attributes != null, "typeNode.Attributes != null");
                XmlAttribute typeAttr = typeNode.Attributes[type_Label];
                if (typeAttr != null && assy != null)
                    type = assy.GetType(typeAttr.InnerText);


                _typesByIndex.Add(typeKey, type);
                _Debug.Assert(type != null, "type != null");
                _indexesByType.Add(type, typeKey);
            }
        }

        private object CreateEmptyObject(XmlNode node)
        {
            Type type = GetTypeFromNode(node);

            ConstructorInfo ci = type.GetConstructor(new Type[] { });
            if (ci == null)
            {
                throw new ApplicationException("ConstructorInfo was null. (Does type " + type + " have a default constructor?)");
            }
            return ci.Invoke(BindingFlags.CreateInstance, null, null, null);
        }

        private XmlNode CreateRefNode(object key, object refid)
        {
            XmlNode node = _rootDoc.CreateNode(XmlNodeType.Element, key.ToString(), "");
            //node.InnerText = refid.ToString();
            XmlAttribute attr = _rootDoc.CreateAttribute(isref_Label);
            attr.InnerText = "true";
            _Debug.Assert(node.Attributes != null, "node.Attributes != null");
            node.Attributes.Append(attr);
            attr = _rootDoc.CreateAttribute(ref_Id_Label);
            attr.InnerText = refid.ToString();
            node.Attributes.Append(attr);

            return node;
        }


        #region >>> Labels for different types and instances of XML Nodes <<<
        private static readonly string null_Label = "isNull";
        private static readonly string isref_Label = "isRef";
        private static readonly string ref_Id_Label = "objKey";
        private static readonly string typekey_Label = "typeKey";
        private static readonly string type_Label = "type";
        private static readonly string assy_Label = "assembly";
        private static readonly string type_Catalog = "TypeCatalog";
        private static readonly string archive = "Archive";
        #endregion

        // ReSharper disable once UnusedParameter.Local
        private object GetOidForObject(object obj)
        {
            return "_" + (_objectNum++);
        }
        // ReSharper disable once UnusedParameter.Local
        private object GetOidForType(object obj)
        {
            return "_" + (_typeNum++);
        }
        private object GetOidFromNode(XmlNode node)
        {
            _Debug.Assert(node.Attributes != null, "node.Attributes != null");
            XmlAttribute attr = node.Attributes[ref_Id_Label];
            if (attr == null)
                return null;
            return node.Attributes[ref_Id_Label].InnerText;
        }

        #region >>> Canned Serializers for Basic Types <<< 
        abstract class PrimitiveXmlSerializer : ISerializer
        {
            private readonly XmlSerializationContext _xmlsc;
            private readonly Type _type;
            protected PrimitiveXmlSerializer(XmlSerializationContext xmlsc, Type type)
            {
                _xmlsc = xmlsc;
                _type = type;
            }

            #region ISerializer Members
            public void StoreObject(object key, object obj)
            {
                XmlNode node = _xmlsc.CreateEmptyNode(key.ToString(), _type, null);
                node.InnerText = StringFromObject(obj);
                _xmlsc.CurrentNode.AppendChild(node);
            }

            public object LoadObject(object key)
            {
                XmlNode node = _xmlsc.CurrentNode.SelectSingleNode(key.ToString());
                _Debug.Assert(node != null, "node != null");
                return ObjectFromString(node.InnerText);
            }
            public void Reset()
            {
            }
            public Hashtable ContextEntities => _xmlsc.ContextEntities;

            #endregion

            protected abstract object ObjectFromString(string str);
            // ReSharper disable once VirtualMemberNeverOverriden.Global
            protected virtual string StringFromObject(object obj)
            {
                return obj.ToString();
            }

        }

        private class StringXmlSerializer : PrimitiveXmlSerializer
        {
            public StringXmlSerializer(XmlSerializationContext xmlsc) : base(xmlsc, typeof(string)) { }
            protected override object ObjectFromString(string str)
            {
                return str;
            }
        }

        private class DoubleXmlSerializer : PrimitiveXmlSerializer
        {
            public DoubleXmlSerializer(XmlSerializationContext xmlsc) : base(xmlsc, typeof(double)) { }
            protected override object ObjectFromString(string str)
            {
                return double.Parse(str);
            }
        }
        private class LongXmlSerializer : PrimitiveXmlSerializer
        {
            public LongXmlSerializer(XmlSerializationContext xmlsc) : base(xmlsc, typeof(long)) { }
            protected override object ObjectFromString(string str)
            {
                return long.Parse(str);
            }
        }
        private class ShortXmlSerializer : PrimitiveXmlSerializer
        {
            public ShortXmlSerializer(XmlSerializationContext xmlsc) : base(xmlsc, typeof(short)) { }
            protected override object ObjectFromString(string str)
            {
                return short.Parse(str);
            }
        }

        private class UShortXmlSerializer : PrimitiveXmlSerializer
        {
            public UShortXmlSerializer(XmlSerializationContext xmlsc) : base(xmlsc, typeof(ushort)) { }
            protected override object ObjectFromString(string str)
            {
                return ushort.Parse(str);
            }
        }

        private class IntXmlSerializer : PrimitiveXmlSerializer
        {
            public IntXmlSerializer(XmlSerializationContext xmlsc) : base(xmlsc, typeof(int)) { }
            protected override object ObjectFromString(string str)
            {
                return int.Parse(str);
            }
        }

        private class UintXmlSerializer : PrimitiveXmlSerializer
        {
            public UintXmlSerializer(XmlSerializationContext xmlsc) : base(xmlsc, typeof(uint)) { }
            protected override object ObjectFromString(string str)
            {
                return uint.Parse(str);
            }
        }

        private class SByteXmlSerializer : PrimitiveXmlSerializer
        {
            public SByteXmlSerializer(XmlSerializationContext xmlsc) : base(xmlsc, typeof(sbyte)) { }
            protected override object ObjectFromString(string str)
            {
                return sbyte.Parse(str);
            }
        }

        private class ByteXmlSerializer : PrimitiveXmlSerializer
        {
            public ByteXmlSerializer(XmlSerializationContext xmlsc) : base(xmlsc, typeof(byte)) { }
            protected override object ObjectFromString(string str)
            {
                return byte.Parse(str);
            }
        }

        private class BoolXmlSerializer : PrimitiveXmlSerializer
        {
            public BoolXmlSerializer(XmlSerializationContext xmlsc) : base(xmlsc, typeof(bool)) { }
            protected override object ObjectFromString(string str)
            {
                return bool.Parse(str);
            }
        }

        private class GuidXmlSerializer : PrimitiveXmlSerializer
        {
            public GuidXmlSerializer(XmlSerializationContext xmlsc) : base(xmlsc, typeof(Guid)) { }
            //protected override string StringFromObject(object obj){ return ((TimeSpan)obj).ToString(; }
            protected override object ObjectFromString(string str)
            {
                return new Guid(str);
            }
        }

        private class TimeSpanXmlSerializer : PrimitiveXmlSerializer
        {
            public TimeSpanXmlSerializer(XmlSerializationContext xmlsc) : base(xmlsc, typeof(TimeSpan)) { }
            //protected override string StringFromObject(object obj){ return ((TimeSpan)obj).ToString(; }
            protected override object ObjectFromString(string str)
            {
                return TimeSpan.Parse(str);
            }
        }

        private class DateTimeXmlSerializer : PrimitiveXmlSerializer
        {
            public DateTimeXmlSerializer(XmlSerializationContext xmlsc) : base(xmlsc, typeof(DateTime)) { }
            //protected override string StringFromObject(object obj){ return obj.ToString(); }
            protected override object ObjectFromString(string str)
            {
                return DateTime.Parse(str);
            }
        }

        private class TypeXmlSerializer : PrimitiveXmlSerializer
        {
            public TypeXmlSerializer(XmlSerializationContext xmlsc) : base(xmlsc, typeof(Type)) { }
            //protected override string StringFromObject(object obj){ return obj.ToString(); }
            protected override object ObjectFromString(string str)
            {
                return Type.GetType(str);
            }
        }

        private class HashtableXmlSerializer : ISerializer
        {
            private readonly XmlSerializationContext _xmlsc;
            private readonly Type _type = typeof(Hashtable);
            public HashtableXmlSerializer(XmlSerializationContext xmlsc)
            {
                _xmlsc = xmlsc;
            }

            #region ISerializer Members

            public void StoreObject(object key, object obj)
            {
                object oid = _xmlsc.GetOidForObject(obj);
                XmlNode node = _xmlsc.CreateEmptyNode(key.ToString(), _type, oid);
                _xmlsc.CurrentNode.AppendChild(node);
                _xmlsc.PushNode(node);
                Hashtable ht = (Hashtable)obj;
                _xmlsc.StoreObject("NumEntries", ht.Count);
                int i = 0;
                foreach (DictionaryEntry de in ht)
                {
                    _xmlsc.StoreObject("Key_" + i, de.Key);
                    _xmlsc.StoreObject("Val_" + i, de.Value);
                    i++;
                }
                _xmlsc.PopNode();
            }

            public object LoadObject(object key)
            {
                XmlNode node = _xmlsc.CurrentNode.SelectSingleNode(key.ToString());
                _xmlsc.PushNode(node);
                Hashtable ht = new Hashtable();
                object tmpCount = _xmlsc.LoadObject("NumEntries");
                int count = (int)tmpCount;
                for (int i = 0; i < count; i++)
                {
                    object dekey = _xmlsc.LoadObject("Key_" + i);
                    object deval = _xmlsc.LoadObject("Val_" + i);
                    ht.Add(dekey, deval);
                }
                _xmlsc.PopNode();
                return ht;
            }

            public void Reset()
            {
                // TODO:  Add HashtableXmlSerializer.Reset implementation
            }

            public Hashtable ContextEntities => _xmlsc.ContextEntities;

            #endregion

        }

        private class ArrayListXmlSerializer : ISerializer
        {
            private readonly XmlSerializationContext _xmlsc;
            private readonly Type _type = typeof(ArrayList);
            public ArrayListXmlSerializer(XmlSerializationContext xmlsc)
            {
                _xmlsc = xmlsc;
            }

            #region ISerializer Members

            public void StoreObject(object key, object obj)
            {
                object oid = _xmlsc.GetOidForObject(obj);
                XmlNode node = _xmlsc.CreateEmptyNode(key.ToString(), _type, oid);
                _xmlsc.CurrentNode.AppendChild(node);
                _xmlsc.PushNode(node);
                ArrayList al = (ArrayList)obj;
                _xmlsc.StoreObject("NumEntries", al.Count);
                int i = 0;
                foreach (object entry in al)
                {
                    _xmlsc.StoreObject("Val_" + i, entry);
                    i++;
                }
                _xmlsc.PopNode();
            }

            public object LoadObject(object key)
            {
                XmlNode node = _xmlsc.CurrentNode.SelectSingleNode(key.ToString());
                _xmlsc.PushNode(node);
                ArrayList al = new ArrayList();
                object tmpCount = _xmlsc.LoadObject("NumEntries");
                int count = (int)tmpCount;
                for (int i = 0; i < count; i++)
                {
                    object alval = _xmlsc.LoadObject("Val_" + i);
                    al.Add(alval);
                }
                _xmlsc.PopNode();
                return al;
            }

            public void Reset()
            {
            }

            public Hashtable ContextEntities => _xmlsc.ContextEntities;

            #endregion

        }

        // Not currently in use - still a problem with an array of primitives being deserialized.
        // It seems that the empty array of primitives is being created, but then the first object,
        // read from the archive is peing pushed into the array - but since it's an object, and
        // the array takes primitives, we throw an exception. Run zTestPersistence's 
        // new DeepPersistenceTester().TestArrayPersistence(); to see the issue in action.
        // ReSharper disable once UnusedMember.Local
        private class ArrayXmlSerializer : ISerializer
        {
            private readonly XmlSerializationContext _xmlsc;
            private readonly Type _type = typeof(Array);
            public ArrayXmlSerializer(XmlSerializationContext xmlsc)
            {
                _xmlsc = xmlsc;
            }

            #region ISerializer Members

            public void StoreObject(object key, object obj)
            { // TODO: Array of primitives - more compact format.
                object oid = _xmlsc.GetOidForObject(obj);
                XmlNode node = _xmlsc.CreateEmptyNode(key.ToString(), _type, oid);
                _xmlsc.CurrentNode.AppendChild(node);
                _xmlsc.PushNode(node);

                Array array = (Array)obj;
                int rank = array.Rank;
                int[] lengths = new int[rank];
                for (int i = 0; i < rank; i++)
                    lengths[i] = array.GetLength(i);
                _xmlsc.StoreObject("ElementType", array.GetType());
                if (rank > 1)
                {
                    _xmlsc.StoreObject("Lengths", lengths);
                }
                else
                {
                    _xmlsc.StoreObject("Length", lengths[0]);
                }

                int[] indices = new int[rank];
                Array.Clear(indices, 0, rank);

                int ndx = 0;
                while (ndx < rank)
                {
                    ndx = 0;
                    string ndxStr = GetNdxString(indices);
                    _xmlsc.StoreObject(ndxStr, array.GetValue(indices));
                    while ((ndx < indices.Length) && (++indices[ndx]) == lengths[ndx])
                    {
                        indices[ndx] = 0;
                        ndx++;
                    }
                }

                _xmlsc.PopNode();
            }

            private string GetNdxString(int[] indices)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder("C_");
                for (int i = 0; i < indices.Length; i++)
                {
                    sb.Append(indices[i]);
                    if (i < (indices.Length - 1))
                        sb.Append("_");
                }
                return sb.ToString();
            }


            public object LoadObject(object key)
            {
                XmlNode node = _xmlsc.CurrentNode.SelectSingleNode(key.ToString());
                _xmlsc.PushNode(node);
                Type elementType = (Type)_xmlsc.LoadObject("ElementType");
                int[] lengths;
                _Debug.Assert(node != null, "node != null");
                if (node.SelectSingleNode("Lengths") != null)
                {
                    lengths = (int[])_xmlsc.LoadObject("Lengths");
                }
                else
                {
                    lengths = new int[1];
                    lengths[0] = (int)_xmlsc.LoadObject("Length");
                }
                int rank = lengths.Length;

                Array array = Array.CreateInstance(elementType, lengths);

                int[] indices = new int[rank];
                Array.Clear(array, 0, rank);

                int ndx = 0;
                while (ndx < rank)
                {
                    ndx = 0;
                    string ndxStr = GetNdxString(indices);
                    // The problem comes in in this next line. LoadObject gives an object, array might take a primitive value type.
                    array.SetValue(_xmlsc.LoadObject(ndxStr), indices);
                    while ((ndx < indices.Length) && (++indices[ndx]) == lengths[ndx])
                    {
                        indices[ndx] = 0;
                        ndx++;
                    }
                }

                _xmlsc.PopNode();
                return array;
            }

            public void Reset()
            {
            }

            public Hashtable ContextEntities => _xmlsc.ContextEntities;

            #endregion

        }

        private class DelegateXmlSerializer : ISerializer
        {
            private readonly XmlSerializationContext _xmlsc;
            public DelegateXmlSerializer(XmlSerializationContext xmlsc)
            {
                _xmlsc = xmlsc;
            }

            #region ISerializer Members

            public void StoreObject(object key, object obj)
            {
                object oid = _xmlsc.GetOidForObject(obj);
                XmlNode node = _xmlsc.CreateEmptyNode(key.ToString(), obj.GetType(), oid);
                _xmlsc.CurrentNode.AppendChild(node);
                _xmlsc.PushNode(node);
                Delegate del = (Delegate)obj;
                _xmlsc.StoreObject("Target", del.Target);
                _xmlsc.StoreObject("Method", del.Method.Name);
                _xmlsc.PopNode();
            }

            public object LoadObject(object key)
            {
                XmlNode node = _xmlsc.CurrentNode.SelectSingleNode(key.ToString());
                _xmlsc.PushNode(node);
                object target = _xmlsc.LoadObject("Target");
                string method = (string)_xmlsc.LoadObject("Method");
                Delegate del = Delegate.CreateDelegate(_xmlsc.GetTypeFromNode(node), target, method);
                _xmlsc.PopNode();
                return del;
            }

            public void Reset()
            {
            }

            public Hashtable ContextEntities => _xmlsc.ContextEntities;

            #endregion

        }
        private class DictionaryEntryXmlSerializer : ISerializer
        {
            private readonly XmlSerializationContext _xmlsc;
            private readonly Type _type = typeof(DictionaryEntry);
            public DictionaryEntryXmlSerializer(XmlSerializationContext xmlsc)
            {
                _xmlsc = xmlsc;
            }

            #region ISerializer Members

            public void StoreObject(object key, object obj)
            {
                object oid = _xmlsc.GetOidForObject(obj);
                XmlNode node = _xmlsc.CreateEmptyNode(key.ToString(), _type, oid);
                _xmlsc.CurrentNode.AppendChild(node);
                _xmlsc.PushNode(node);
                DictionaryEntry de = (DictionaryEntry)obj;
                _xmlsc.StoreObject("key", de.Key);
                _xmlsc.StoreObject("Value", de.Value);
                _xmlsc.PopNode();
            }

            public object LoadObject(object key)
            {
                XmlNode node = _xmlsc.CurrentNode.SelectSingleNode(key.ToString());
                _xmlsc.PushNode(node);
                object entryKey = _xmlsc.LoadObject("key");
                object entryVal = _xmlsc.LoadObject("Value");
                _xmlsc.PopNode();
                return new DictionaryEntry(entryKey, entryVal);
            }

            public void Reset()
            {
            }

            public Hashtable ContextEntities => _xmlsc.ContextEntities;

            #endregion

        }
        private class EnumXmlSerializer : ISerializer
        {
            private readonly XmlSerializationContext _xmlsc;
            public EnumXmlSerializer(XmlSerializationContext xmlsc)
            {
                _xmlsc = xmlsc;
            }

            #region ISerializer Members

            public void StoreObject(object key, object obj)
            {
                object oid = _xmlsc.GetOidForObject(obj);
                XmlNode node = _xmlsc.CreateEmptyNode(key.ToString(), obj.GetType(), oid);
                _xmlsc.CurrentNode.AppendChild(node);
                _xmlsc.PushNode(node);
                _xmlsc.StoreObject(key, obj.ToString());
                _xmlsc.PopNode();
            }

            public object LoadObject(object key)
            {
                XmlNode node = _xmlsc.CurrentNode.SelectSingleNode(key.ToString());
                _xmlsc.PushNode(node);
                Type type = _xmlsc.GetTypeFromNode(node);
                string enumText = (string)_xmlsc.LoadObject(key);
                _xmlsc.PopNode();
                return Enum.Parse(type, enumText, false);
            }

            public void Reset()
            {
            }

            public Hashtable ContextEntities => _xmlsc.ContextEntities;

            #endregion

        }

        #endregion
    }
}