/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using System;
using System.Collections.Generic;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace Highpoint.Sage.Persistence
{
    /// <summary>
    /// Class DeserializationContext tracks objects that have been deserialized from an Xml document, and performs
    /// GUID translation so that there are no Guid uniqueness constraints violated. This is useful if objects are 
    /// being deserialized into a model multiple times (such as in a copy/paste operation.)
    /// </summary>
    public class DeserializationContext
    {

        #region Private Fields

        private readonly Dictionary<Guid, Guid> _oldGuidToNewGuidMap;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="DeserializationContext"/> class for managing deserialization from/into the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        public DeserializationContext(IModel model)
        {
            Model = model;
            _oldGuidToNewGuidMap = new Dictionary<Guid, Guid>();
        }

        /// <summary>
        /// Gets the model in which the serialization and deserialization is being done.
        /// </summary>
        /// <value>The model.</value>
        public IModel Model
        {
            get;
        }

        /// <summary>
        /// Sets a new unique identifier to be used for a copy of the object that exists under the old unique identifier.
        /// </summary>
        /// <param name="oldGuid">The old unique identifier.</param>
        /// <param name="newGuid">The new unique identifier.</param>
        public void SetNewGuidForOldGuid(Guid oldGuid, Guid newGuid)
        {
            _oldGuidToNewGuidMap.Add(oldGuid, newGuid);
        }

        /// <summary>
        /// Gets the unique identifier to be used for a copy of the object that exists under the old unique identifier.
        /// </summary>
        /// <param name="oldGuid">The old unique identifier.</param>
        /// <returns>Guid.</returns>
        public Guid GetNewGuidForOldGuid(Guid oldGuid)
        {
            return _oldGuidToNewGuidMap[oldGuid];
        }

        /// <summary>
        /// Gets the model object that had the old unique identifier.
        /// </summary>
        /// <param name="oldGuid">The old unique identifier.</param>
        /// <returns>IModelObject.</returns>
        public IModelObject GetModelObjectThatHad(Guid oldGuid)
        {
            return Model.ModelObjects[oldGuid];
        }
    }
}