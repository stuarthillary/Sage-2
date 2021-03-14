/* This source code licensed under the GNU Affero General Public License */

using System.Collections.Specialized;
using System.Diagnostics;
using _Debug = System.Diagnostics.Debug;


namespace Highpoint.Sage.SimCore
{

    /// <summary>
    /// Class ModelConfig is a collection of initialization parameters that is intended to be available to the model in the app.config file.
    /// </summary>
    public class ModelConfig
    {
        readonly NameValueCollection _nvc;
        public ModelConfig() : this("Sage") { }

        public ModelConfig(string sectionName)
        {
            _nvc = (NameValueCollection)System.Configuration.ConfigurationManager.GetSection(sectionName);
            if (_nvc == null)
            {
                // TODO: Add this to an Errors & Warnings collection instead of dumping it to Trace.
                _Debug.WriteLine(string.Format("Warning - <{0}> section missing from config file for {1}.", sectionName, Process.GetCurrentProcess().ProcessName));
            }
        }

        public string GetSimpleParameter(string key)
        {
            string retval = null;
            if (_nvc != null)
                retval = _nvc[key];
            if (retval == null)
            {
                // TODO: Add this to an Errors & Warnings collection instead of dumping it to Trace.
                _Debug.WriteLine("Application requested unfound parameter associated with key " + key + " in the app.config file.");
            }
            return retval;
        }
    }

}