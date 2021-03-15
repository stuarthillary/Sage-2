/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Scheduling
{
    /// <summary>
    /// Implemented in a method that is to be called after an observable.
    /// Do not respond to this notification by changing the whoChanged object, and be aware that
    /// it is not legal to update U/I elements on any but the thread on which they were created.
    /// </summary>
    public delegate void ObservableChangeHandler(object whoChanged, object whatChanged, object howChanged);

    /// <summary>
    /// IObservable is implemented by an object that is capable of notifying others of its changes.
    /// </summary>
    public interface IObservable
    {
        /// <summary>
        /// ObservableChangeHandler is an event that is fired after an object changes.
        /// </summary>
        event ObservableChangeHandler ChangeEvent;
    }
}
