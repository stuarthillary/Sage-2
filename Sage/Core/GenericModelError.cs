/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// A basic implementation of IModelError. 
    /// </summary>
    public class GenericModelError : IModelError
    {
        private readonly object _target;
        private readonly string _name;
        private readonly string _narrative;
        private readonly object _subject = null;
        private readonly Exception _innerException;
        private readonly bool _autoClear = false;

        /// <summary>
        /// Creates an instance of a basic implementation of IModelError.
        /// </summary>
        /// <param name="name">The short name of the error.</param>
        /// <param name="narrative">A longer narrative of the error.</param>
        /// <param name="target">The target of the error - where the error happened.</param>
        /// <param name="subject">The subject of the error - who probably caused it.</param>
        public GenericModelError(string name, string narrative, object target, object subject)
        : this(name, narrative, target, subject, null) { }

        /// <summary>
        /// Creates an instance of a basic implementation of IModelError.
        /// </summary>
        /// <param name="name">The short name of the error.</param>
        /// <param name="narrative">A longer narrative of the error.</param>
        /// <param name="target">The target of the error - where the error happened.</param>
        /// <param name="subject">The subject of the error - who probably caused it.</param>
        /// <param name="innerException">An exception that may have been caught in the detection of this error.</param>
        public GenericModelError(string name, string narrative, object target, object subject, Exception innerException)
        {
            _name = name;
            _narrative = narrative;
            _target = target;
            _subject = subject;
            _innerException = innerException;
        }

        #region Implementation of IModelError
        /// <summary>
        /// The short name of the error.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }
        /// <summary>
        /// A longer narrative of the error.
        /// </summary>
        public string Narrative
        {
            get
            {
                return _narrative;
            }
        }
        /// <summary>
        /// The target of the error - where the error happened.
        /// </summary>
        public object Target
        {
            get
            {
                return _target;
            }
        }
        /// <summary>
        /// The subject of the error - who probably caused it.
        /// </summary>
        public object Subject
        {
            get
            {
                return _subject;
            }
        }
        /// <summary>
        /// The exception, if any, that generated this ModelError.
        /// </summary>
        public Exception InnerException
        {
            get
            {
                return _innerException;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this error should be automatically cleared at the start of a simulation.
        /// </summary>
        /// <value><c>true</c> if [auto clear]; otherwise, <c>false</c>.</value>
        public bool AutoClear
        {
            get
            {
                return _autoClear;
            }
        }

        #endregion

        public override string ToString()
        {
            string innerExString = (_innerException == null ? "" : " Inner exception = " + _innerException + ".");
            string subjectString = _subject == null ? "<NoSubject>" : "Subject = " + _subject;
            return _name + ": " + _narrative + " (" + subjectString + ", Target = " + _target + "." + innerExString;
        }

    }


}

