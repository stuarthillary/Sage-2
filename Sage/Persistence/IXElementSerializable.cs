/* This source code licensed under the GNU Affero General Public License */

using System.Xml.Linq;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace Highpoint.Sage.Persistence
{

    /// <summary>
    /// This interface is implemented by objects that will be serialized and deserialized via LINQ to XML.
    /// </summary>
    public interface IXElementSerializable
    {

        /// <summary>
        /// Loads and reconstitutes an object's internal state from the element 'self', according to the deserialization context.
        /// </summary>
        /// <param name="self">The self.</param>
        /// <param name="deserializationContext">The deserialization context.</param>
        void LoadFromXElement(XElement self, DeserializationContext deserializationContext);

        /// <summary>
        /// Represents an object's internal state as an XElement with the provided Name..
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>XElement.</returns>
        XElement AsXElement(string name);

    }
}