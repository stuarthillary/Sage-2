/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Persistence;
using System;
using System.Collections;

namespace Highpoint.Sage.Materials.Chemistry
{

    /// <summary>
    /// Class MaterialCatalog stands alone, or serves as a base class for any object that manages instances of <see cref="MaterialType"/>. 
    /// In this case, to &quot;manage&quot; means to be a point of focus to supply a requester with a material type
    /// that is specified by name ur unique ID (Guid). This is often kept at the model level in a model that 
    /// represents or contains chemical reactions.
    /// </summary>
    /// <seealso cref="Highpoint.Sage.Persistence.IXmlPersistable" />
    public class MaterialCatalog : IXmlPersistable
    {

        #region Private Fields

        private Hashtable _materialTypesByName = new Hashtable();
        private Hashtable _materialTypesByGuid = new Hashtable();

        #endregion

        /// <summary>
        /// Adds the specified <see cref="MaterialType"/> to this MaterialCatalog.
        /// </summary>
        /// <param name="mt">The MaterialType.</param>
        /// <exception cref="System.ApplicationException">
        /// SiteScheduleModelBuilder reports creating  + mt + , when there is already a material type,  + mtPre +  of the same name in the model.
        /// or
        /// SiteScheduleModelBuilder reports creating  + mt + , when there is already a material type,  + mtPre +  of the same guid in the model.
        /// </exception>
        public void Add(MaterialType mt)
        {
            if (_materialTypesByName.ContainsKey(mt.Name))
            {
                MaterialType mtPre = (MaterialType)_materialTypesByName[mt.Name];
                throw new ApplicationException("SiteScheduleModelBuilder reports creating " + mt +
                                               ", when there is already a material type, " + mtPre +
                                               " of the same name in the model.");
            }
            else
            {
                _materialTypesByName.Add(mt.Name, mt);
            }

            if (_materialTypesByGuid.Contains(mt.Guid))
            {
                MaterialType mtPre = (MaterialType)_materialTypesByGuid[mt.Guid];
                throw new ApplicationException("SiteScheduleModelBuilder reports creating " + mt +
                                               ", when there is already a material type, " + mtPre +
                                               " of the same guid in the model.");
            }
            else
            {
                _materialTypesByGuid.Add(mt.Guid, mt);
            }
        }

        /// <summary>
        /// Determines whether this MaterialCatalog contains the specified MaterialType. 
        /// Different instances of Material Types are considered equal if they have the same name and guid.
        /// </summary>
        /// <param name="mt">The MaterialType instance.</param>
        /// <returns><c>true</c> if this MaterialCatalog contains the specified MaterialType; otherwise, <c>false</c>.</returns>
        public bool Contains(MaterialType mt)
        {
            return (_materialTypesByGuid.ContainsValue(mt) && _materialTypesByName.ContainsValue(mt));
        }

        /// <summary>
        /// Determines whether this MaterialCatalog contains a MaterialType with the specified name.
        /// </summary>
        /// <param name="mtName">Name of the MaterialType.</param>
        /// <returns><c>true</c> if this MaterialCatalog contains a MaterialType with the specified name; otherwise, <c>false</c>.</returns>
        public bool Contains(string mtName)
        {
            return (_materialTypesByName.ContainsKey(mtName));
        }

        /// <summary>
        /// Determines whether this MaterialCatalog contains a MaterialType with the specified Guid.
        /// </summary>
        /// <param name="mtGuid">The mt unique identifier.</param>
        /// <returns><c>true</c> if this MaterialCatalog contains a MaterialType with the specified Guid; otherwise, <c>false</c>.</returns>
        public bool Contains(Guid mtGuid)
        {
            return (_materialTypesByGuid.ContainsKey(mtGuid));
        }

        /// <summary>
        /// Gets the <see cref="MaterialType"/> with the specified unique identifier.
        /// </summary>
        /// <param name="guid">The unique identifier.</param>
        /// <returns>MaterialType.</returns>
        public MaterialType this[Guid guid] => (MaterialType)_materialTypesByGuid[guid];

        /// <summary>
        /// Gets the <see cref="MaterialType"/> with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>MaterialType.</returns>
        public MaterialType this[string name] => (MaterialType)_materialTypesByName[name];

        /// <summary>
        /// Gets the collection of material types contained in this MaterialCatalog.
        /// </summary>
        /// <value>The material types.</value>
        public ICollection MaterialTypes => _materialTypesByName.Values;

        /// <summary>
        /// Clears this instance - removes all contained MaterialTypes.
        /// </summary>
        public void Clear()
        {
            _materialTypesByGuid.Clear();
            _materialTypesByName.Clear();
        }

        /// <summary>
        /// Removes the material type with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        public void Remove(string name)
        {
            Guid guid = ((MaterialType)_materialTypesByName[name]).Guid;
            _materialTypesByGuid.Remove(guid);
            _materialTypesByName.Remove(name);
        }

        /// <summary>
        /// Removes the material type with the specified Guid.
        /// </summary>
        /// <param name="guid">The specified Guid.</param>
        public void Remove(Guid guid)
        {
            string name = ((MaterialType)_materialTypesByName[guid]).Name;
            _materialTypesByGuid.Remove(guid);
            _materialTypesByName.Remove(name);
        }

        #region >>> Serialization Support <<< 

        /// <summary>
        /// Stores this object to the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
        public void SerializeTo(XmlSerializationContext xmlsc)
        {
            xmlsc.StoreObject("MaterialTypesByName", _materialTypesByName);
            xmlsc.StoreObject("MaterialTypesByGuid", _materialTypesByGuid);
        }

        /// <summary>
        /// Reconstitutes this object from the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
        public void DeserializeFrom(XmlSerializationContext xmlsc)
        {
            _materialTypesByName = (Hashtable)xmlsc.LoadObject("MaterialTypesByName");
            _materialTypesByGuid = (Hashtable)xmlsc.LoadObject("MaterialTypesByGuid");
        }

        #endregion

    }
}