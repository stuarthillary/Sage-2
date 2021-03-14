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
