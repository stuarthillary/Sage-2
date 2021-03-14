/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Persistence;
using System;


namespace Highpoint.Sage.Materials.Chemistry
{
    /// <summary>
    /// A BasicReactionSupporter is used for testing. It is the simplest class that can implement the
    /// ISupportsReactions interface (it also implements IXmlPersistable...)
    /// </summary>
    public class BasicReactionSupporter : ISupportsReactions, IXmlPersistable
    {
        private ReactionProcessor _reactionProcessor;
        private MaterialCatalog _materialCatalog;
        /// <summary>
		/// The simplest class that implements the ISupportsReactions interface (it also implements IXmlPersistable...)
        /// </summary>
		public BasicReactionSupporter()
        {
            RegisterReactionProcessor(new ReactionProcessor());
            RegisterMaterialCatalog(new MaterialCatalog());

        }
        public ReactionProcessor MyReactionProcessor => _reactionProcessor;
        public MaterialCatalog MyMaterialCatalog => _materialCatalog;

        public void RegisterReactionProcessor(ReactionProcessor rp)
        {
            if (_reactionProcessor == null)
                _reactionProcessor = rp;
            else
                throw new ApplicationException("Attempt to register a new reaction processor into a BasicReactionSupporter that already has one. This is prohibited.");
        }
        public void RegisterMaterialCatalog(MaterialCatalog materialCatalog)
        {
            if (_materialCatalog == null)
                _materialCatalog = materialCatalog;
            else
                throw new ApplicationException("Attempt to register a new material catalog into a BasicReactionSupporter that already has one. This is prohibited.");
        }
        #region IXmlPersistable Members

        /// <summary>
        /// Serializes this object to the specified XmlSerializatonContext.
        /// </summary>
        /// <param name="xmlsc">The XmlSerializatonContext into which this object is to be stored.</param>
        public void SerializeTo(XmlSerializationContext xmlsc)
        {
            if (xmlsc != null)
            {
                xmlsc.StoreObject("MaterialCatalog", _materialCatalog);
                xmlsc.StoreObject("ReactionProcessor", _reactionProcessor);
            }
            else
            {
                throw new ApplicationException("SerializeTo(...) called with a null XmlSerializationContext.");
            }
        }

        /// <summary>
        /// Deserializes this object from the specified XmlSerializatonContext.
        /// </summary>
        /// <param name="xmlsc">The XmlSerializatonContext from which this object is to be reconstituted.</param>
        public void DeserializeFrom(XmlSerializationContext xmlsc)
        {
            _materialCatalog = (MaterialCatalog)xmlsc.LoadObject("MaterialCatalog");
            _reactionProcessor = (ReactionProcessor)xmlsc.LoadObject("ReactionProcessor");
        }

        #endregion
    }
}
