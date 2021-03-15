/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using System;
using System.Globalization;

namespace Highpoint.Sage.Diagnostics
{
    /// <summary>
    /// Creates an event logger to store events from a particular executive into a file.
    /// </summary>
    public class ExecEventLogger : IDisposable
    {

        private System.IO.TextWriter _logFile;

        /// <summary>
        /// Creates a new instance of the <see cref="T:EventLogger"/> class.
        /// </summary>
        /// <param name="exec">The executive to be logged.</param>
        /// <param name="filename">The filename into which to write the logs.</param>
        public ExecEventLogger(IExecutive exec, string filename)
        {
            _logFile = new System.IO.StreamWriter(filename, false);
            exec.EventAboutToFire += Executive_EventAboutToFire;
            _logFile.WriteLine("Time,Pri,TargetObject,MethodName,UserData,EventType");
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="T:Highpoint.Sage.Diagnostics.EventLogger"/> is reclaimed by garbage collection.
        /// </summary>
        ~ExecEventLogger()
        {
            Dispose();
        }
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
		public void Dispose()
        {
            if (_logFile == null)
                return;
            try
            {
                _logFile.Flush();
                _logFile.Close();
                _logFile = null;
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void Executive_EventAboutToFire(long key, ExecEventReceiver eer, double priority, DateTime when, object userData, ExecEventType eventType)
        {
            string method = eer.Method.ToString();
            method = method.Replace(",", ":");
            _logFile.WriteLine(when.ToString(CultureInfo.InvariantCulture) + ", " + priority + ", " + eer.Target + ", " + method + ", " +
                (userData?.ToString() ?? "<null>") + ", " + eventType);
        }
    }
}