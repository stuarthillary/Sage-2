/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Persistence;
// ReSharper disable UnusedMember.Local
// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable VirtualMemberNeverOverriden.Global

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// A structure that is used in the creation of a hierarchical dictionary. Such
    /// a dictionary structure can contain smart property bags and other dictionaries
    /// as leaf nodes, as well as having IDictionaries implementing the tree-like
    /// structure of the root dictionary. This is used so that a node in a SmartPropertyBag
    /// can have an atomic meaning in a real-world sense (such as a temperature controller
    /// on a piece of equipment, but still be implemented as an IDictionary or even a
    /// SmartPropertyBag.
    /// </summary>
    public struct HierarchicalDictionaryEntry : IXmlPersistable
    {
        private object _key;
        private object _value;
        private bool _isLeaf;
        /// <summary>
        /// Creates a HierarchicalDictionaryEntry.
        /// </summary>
        /// <param name="key">The key by which the object is known in the dictionary.</param>
        /// <param name="val">The object value of the entry in the dictionary.</param>
        /// <param name="isLeaf">True if this is a semantic leaf-node.</param>
        public HierarchicalDictionaryEntry(object key, object val, bool isLeaf)
        {
            _key = key;
            _isLeaf = isLeaf;
            _value = val;
        }
        /// <summary>
        /// The key by which the object is known in the dictionary.
        /// </summary>
        public object Key => _key;

        /// <summary>
        /// The object value of the entry in the dictionary.
        /// </summary>
        public object Value => _value;

        /// <summary>
        /// True if this is a semantic leaf-node.
        /// </summary>
        public bool IsLeaf => _isLeaf;

        #region >>> IXmlPersistable Support

        /// <summary>
        /// Stores this object to the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
        public void SerializeTo(XmlSerializationContext xmlsc)
        {
            xmlsc.StoreObject("key", _key);
            xmlsc.StoreObject("Value", _value);
            xmlsc.StoreObject("IsLeaf", _isLeaf);
        }
        /// <summary>
        /// Reconstitutes this object from the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
		public void DeserializeFrom(XmlSerializationContext xmlsc)
        {
            _key = xmlsc.LoadObject("key");
            _value = xmlsc.LoadObject("Value");
            _isLeaf = (bool)xmlsc.LoadObject("IsLeaf");
        }


        #endregion
    }
}