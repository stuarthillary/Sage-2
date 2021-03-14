/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// A basic implementation of IModelWarning. 
    /// </summary>
    public class GenericModelWarning : IModelWarning
    {
        private readonly object _target;
        private readonly string _name;
        private readonly string _narrative;
        private readonly object _subject = null;

        /// <summary>
        /// Creates an instance of a basic implementation of IModelWarning.
        /// </summary>
        /// <param name="name">The short name of the warning.</param>
        /// <param name="narrative">A longer narrative of the warning.</param>
        /// <param name="target">The target of the warning - where the warning happened.</param>
        /// <param name="subject">The subject of the warning - who probably caused it.</param>
        public GenericModelWarning(string name, string narrative, object target, object subject)
        {
            _name = name;
            _narrative = narrative;
            _target = target;
            _subject = subject;
        }

        #region Implementation of IModelWarning
        /// <summary>
        /// The short name of the warning.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }
        /// <summary>
        /// A longer narrative of the warning.
        /// </summary>
        public string Narrative
        {
            get
            {
                return _narrative;
            }
        }
        /// <summary>
        /// The target of the warning - where the warning happened.
        /// </summary>
        public object Target
        {
            get
            {
                return _target;
            }
        }
        /// <summary>
        /// The subject of the warning - who probably caused it.
        /// </summary>
        public object Subject
        {
            get
            {
                return _subject;
            }
        }
        #endregion

        public override string ToString()
        {
            string subjectString = _subject == null ? "<NoSubject>" : "Subject = " + _subject;
            return _name + ": " + _narrative + " (" + subjectString + ", Target = " + _target + ".";
        }

    }


}

