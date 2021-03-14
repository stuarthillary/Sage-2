/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// Class InitializerAttribute decorates any method intended to be called by an initializationManager. It declares
    /// whether the method is to be called during model setup, or during the model's run.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    public class InitializerAttribute : Attribute
    {
        public static readonly string DEFAULT_NAME = "_Initialize";
        /// <summary>
        /// This enumeration describes when in the lifecycle of a model, the initializer is called.
        /// </summary>
        public enum InitializationType
        {
            /// <summary>
            /// The initializer is called during model setup, in the transition from Dirty to initialized.
            /// </summary>
            PreRun,
            /// <summary>
            /// The initializer is called during model run, while the model is in the running state.
            /// </summary>
            RunTime
        }

        readonly InitializationType _type;
        readonly string _secondaryInitializerName = null;
        public InitializerAttribute(InitializationType type) : this(type, DEFAULT_NAME) { }

        public InitializerAttribute(InitializationType type, string secondaryInitializerName)
        {
            _type = type;
            _secondaryInitializerName = secondaryInitializerName;
        }
        public InitializationType Type
        {
            get
            {
                return _type;
            }
        }
        public string SecondaryInitializerName
        {
            get
            {
                if (_secondaryInitializerName == null)
                    return "_Initialize";
                return _secondaryInitializerName;
            }
        }
    }
}
