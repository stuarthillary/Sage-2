/* This source code licensed under the GNU Affero General Public License */

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace Highpoint.Sage.Persistence
{
    /// <summary>
    /// This interface is implemented by any object that can be serialized to a custom XML
    /// stream. (It does not mean that, necessarily, the XmlSerializationContext has been
    /// provisioned with serializers suffient to perform that serialization, but just that
    /// the object implementing it, knows how to break down and stream, and subsequently to
    /// reclaim from the stream, its constituent parts.
    /// </summary>
    public interface IXmlPersistable
    {

        /// <summary>
        /// Stores this object to the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
        void SerializeTo(XmlSerializationContext xmlsc);

        /// <summary>
        /// Reconstitutes this object from the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
        void DeserializeFrom(XmlSerializationContext xmlsc); // After calling default ctor.
    }
}