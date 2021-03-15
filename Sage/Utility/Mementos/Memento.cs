/* This source code licensed under the GNU Affero General Public License */

using System.Collections;

#pragma warning disable 1587
/// <summary>
/// The Mementos namespace contains an implementation of a Memento pattern - that is, an object that implements ISupportsMememtos
/// is capable of generating and maintaining mementos, which are representations of internal state at a given time that can be
/// used to restore the object's internal state to a previous set of values. MementoHelper exists to simplify implementation of the
/// ISupportsMememtos interface.
/// </summary>
#pragma warning restore 1587
namespace Highpoint.Sage.Utility.Mementos
{

    public delegate void MementoEvent(IMemento memento);

    /// <summary>
    /// Implemented by any object that can act as a memento.
    /// </summary>
    public interface IMemento
    {

        /// <summary>
        /// Creates an empty copy of whatever object this memento can reconstitute. Some
        /// mementos are only able to reconstitute into their source objects (they can only
        /// be used to restore state in the same object), and these mementos will return a
        /// reference to that object.)
        /// </summary>
        ISupportsMementos CreateTarget();

        /// <summary>
        /// Loads the contents of this Memento into the provided object.
        /// </summary>
        /// <param name="ism">The object to receive the contents of the memento.</param>
        void Load(ISupportsMementos ism);

        /// <summary>
        /// Emits an IDictionary form of the memento that can be, for example, dumped to
        /// Trace.
        /// </summary>
        /// <returns>An IDictionary form of the memento.</returns>
        IDictionary GetDictionary();

        /// <summary>
        /// Returns true if the two mementos are semantically equal.
        /// </summary>
        /// <param name="otheOneMemento">The memento this one should compare itself to.</param>
        /// <returns>True if the mementos are semantically equal.</returns>
        bool Equals(IMemento otheOneMemento);

        /// <summary>
        /// This event is fired once this memento has completed its Load(ISupportsMementos ism) invocation.
        /// </summary>
        event MementoEvent OnLoadCompleted;

        /// <summary>
        /// This holds a reference to the memento, if any, that contains this memento.
        /// </summary>
        IMemento Parent
        {
            get; set;
        }
    }
}