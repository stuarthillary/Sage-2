/* This source code licensed under the GNU Affero General Public License */


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
    /// <summary>
    /// Implemented by a method that will listen for changes in the form or
    /// contents of a memento.
    /// </summary>
    public delegate void MementoChangeEvent(ISupportsMementos rootChange);

    /// <summary>
    /// Implemented by an object that supports Mementos.
    /// </summary>
    public interface ISupportsMementos
    {

        /// <summary>
        /// Retrieves a memento from the object.
        /// </summary>
        IMemento Memento
        {
            get; set;
        }

        /// <summary>
        /// Reports whether the object has changed relative to its memento
        /// since the last memento was recorded.
        /// </summary>
        bool HasChanged
        {
            get;
        }

        /// <summary>
        /// Fired when the memento contents will have changed. This does not
        /// imply that the memento <i>has</i> changed, since the memento is
        /// recorded, typically, only on request. It <i>does</i> imply that if
        /// you ask for a memento, it will be in some way different from any
        /// memento you might have previously acquired.
        /// </summary>
        event MementoChangeEvent MementoChangeEvent;

        /// <summary>
        /// Indicates whether this object can report memento changes to its
        /// parent. (Mementos can contain other mementos.) 
        /// </summary>
        bool ReportsOwnChanges
        {
            get;
        }

        /// <summary>
        /// Returns true if the two mementos are semantically equal.
        /// </summary>
        /// <param name="otherGuy">The other memento implementer.</param>
        /// <returns>True if the two mementos are semantically equal.</returns>
        bool Equals(ISupportsMementos otherGuy);
    }
}