/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.SimCore
{
    #region Sample Implementation of IModelObject
#if NOT_DEFINED
// A recommended implementation to ^C^V.
    #region Implementation of IModelObject
        private string m_name = null;
        private Guid m_guid = Guid.Empty;
        private IModel m_model;
		private string m_description = null;
        
        /// <summary>
        /// The IModel to which this object belongs.
        /// </summary>
        /// <value>The object's Model.</value>
        public IModel Model { [System.Diagnostics.DebuggerStepThrough] get { return m_model; } }
       
        /// <summary>
        /// The name by which this object is known. Typically not required to be unique in a pan-model context.
        /// </summary>
        /// <value>The object's name.</value>
        public string Name { [System.Diagnostics.DebuggerStepThrough]get { return m_name; } }
        
        /// <summary>
        /// The description for this object. Typically used for human-readable representations.
        /// </summary>
        /// <value>The object's description.</value>
		public string Description { [System.Diagnostics.DebuggerStepThrough] get { return ((m_description==null)?("No description for " + m_name):m_description); } }
        
        /// <summary>
        /// The Guid for this object. Typically required to be unique in a pan-model context.
        /// </summary>
        /// <value>The object's Guid.</value>
        public Guid Guid { [System.Diagnostics.DebuggerStepThrough] get { return m_guid; } }

        /// <summary>
        /// Initializes the fields that feed the properties of this IModelObject identity.
        /// </summary>
        /// <param name="model">The IModelObject's new model value.</param>
        /// <param name="name">The IModelObject's new name value.</param>
        /// <param name="description">The IModelObject's new description value.</param>
        /// <param name="guid">The IModelObject's new GUID value.</param>
        public void InitializeIdentity(IModel model, string name, string description, Guid guid) {
            IMOHelper.Initialize(ref m_model, model, ref m_name, name, ref m_description, description, ref m_guid, guid);
        }
    #endregion

#endif

    #endregion

    /// <summary>
    /// A helper class that contains logic for initializing and registering ModelObjects.
    /// </summary>
    public static class IMOHelper
    {

        /// <summary>
        /// Initializes the specified m_model.
        /// </summary>
        /// <param name="m_model">The m_model field in the IModelObject.</param>
        /// <param name="model">The model to initialize the IModelObject's field with.</param>
        /// <param name="m_name">The m_name field in the IModelObject.</param>
        /// <param name="name">The name to initialize the IModelObject's field with.</param>
        /// <param name="m_description">The m_description field in the IModelObject.</param>
        /// <param name="description">The description to initialize the IModelObject's field with.</param>
        /// <param name="m_guid">The m_guid field in the IModelObject.</param>
        /// <param name="guid">The GUID to initialize the IModelObject's field with.</param>
        public static void Initialize(ref IModel m_model, IModel model, ref string m_name, string name, ref string m_description, string description, ref Guid m_guid, Guid guid)
        {

            if (m_model == null && m_guid.Equals(Guid.Empty))
            {
                m_model = model;
                m_name = name;
                if (description == null || description.Equals(""))
                {
                    m_description = name;
                }
                else
                {
                    m_description = description;
                }
                m_guid = guid;
            }
            else
            {
                string identity = "Model=" + (m_model == null ? "<null>" : (m_model.Name == null ? m_model.Guid.ToString() : m_model.Name)) +
                    ", Name=" + (m_name == null ? "<null>" : m_name) + ", Description=" + (m_description == null ? "<null>" : m_description) +
                    ", Guid=" + m_guid;

                throw new ApplicationException("Cannot call InitializeIdentity(...) on an IModelObject that is already initialized. " +
                    "The IModelobject's Identity is:\r\n[" + identity + "].");
            }
        }

        /// <summary>
        /// Registers the IModelObject with the model by adding the IModelObject to the IModel's ModelObjectDictionary.
        /// </summary>
        /// <param name="imo">The IModelObject.</param>
        public static void RegisterWithModel(IModelObject imo)
        {
            if (!(imo.Guid.Equals(Guid.Empty)) && (imo.Model != null))
            {
                imo.Model.AddModelObject(imo);
            }
        }

        /// <summary>
        /// Registers the provided IModelObject, keyed on its Guid, with the model, replacing any existing one with the new one, if so indicated.
        /// </summary>
        /// <param name="imo">The imo.</param>
        /// <param name="replaceOk">if set to <c>true</c> [replace OK].</param>
        public static void RegisterWithModel(IModelObject imo, bool replaceOk)
        {
            if (!(imo.Guid.Equals(Guid.Empty)) && (imo.Model != null))
            {
                if (imo.Model.ModelObjects.Contains(imo.Guid))
                {
                    if (!imo.Model.ModelObjects[imo.Guid].Equals(imo))
                    {
                        imo.Model.ModelObjects.Remove(imo.Guid);
                        imo.Model.AddModelObject(imo);
                    }
                }
                else
                {
                    imo.Model.AddModelObject(imo);
                }
            }
        }
    }
}
