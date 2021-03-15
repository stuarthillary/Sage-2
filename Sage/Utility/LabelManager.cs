/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;
using System.Threading;

namespace Highpoint.Sage.Utility
{

    /// <summary>
    /// A manager to which a class that implements a Label can delegate.
    /// </summary>
    public class LabelManager : IHasLabel
    {

        private static readonly LocalDataStoreSlot _ldss;
        /// <summary>
        /// The name of the Thread-Local-Storage data slot in which the current context key is stored.
        /// </summary>
        public static readonly string CONTEXTSLOTNAME = "SageLabelContext";

        /// <summary>
        /// The name of the default contents of the Thread-Local-Storage data slot in which the current context key is stored.
        /// </summary>
        public static readonly string DEFAULT_CHANNEL = "SageDefaultLabelContext";

        private readonly Dictionary<string, string> _labels;

        /// <summary>
        /// Initializes the <see cref="T:LabelManager"/> class by creating a Thread-Local-Storage data slot for the key.
        /// </summary>
        static LabelManager()
        {
            _ldss = Thread.AllocateNamedDataSlot(CONTEXTSLOTNAME);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:LabelManager"/> class.
        /// </summary>
        public LabelManager()
        {
            _labels = new Dictionary<string, string> { [DEFAULT_CHANNEL] = String.Empty };
        }

        /// <summary>
        /// Sets the label context for all unspecified requests in this thread.
        /// </summary>
        /// <param name="context">The context.</param>
        public static void SetContext(string context)
        {
            LocalDataStoreSlot ldss = Thread.GetNamedDataSlot(CONTEXTSLOTNAME);
            Thread.SetData(ldss, context);
        }

        #region IHasLabel Members

        /// <summary>
        /// Gets or sets the label in the currently-selected context, or if none has been selected, then according to the default context.
        /// </summary>
        /// <value>The label.</value>
        public string Label
        {
            get
            {
                if (_labels.ContainsKey(Key))
                {
                    return _labels[Key];
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                if (_labels.ContainsKey(Key))
                {
                    _labels[Key] = value;
                }
                else
                {
                    _labels.Add(Key, value);
                }
            }
        }

        /// <summary>
        /// Sets the label in the context indicated by the provided context, or if null or String.Empty has been selected, then in the default context.
        /// </summary>
        /// <param name="label">The label.</param>
        /// <param name="context">The context - use null or string.Empty for the default context.</param>
        public void SetLabel(string label, string context)
        {
            if (context == null || context.Equals(string.Empty))
            {
                context = DEFAULT_CHANNEL;
            }
            _labels[context] = label;
        }

        /// <summary>
        /// Gets the label from the context indicated by the provided context, or if null or String.Empty has been selected, then from the default context.
        /// </summary>
        /// <param name="context">The context - use null or string.Empty for the default context.</param>
        /// <returns></returns>
        public string GetLabel(string context)
        {
            if (context == null || context.Equals(string.Empty))
            {
                context = DEFAULT_CHANNEL;
            }
            return _labels[context];
        }

        #endregion

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <value>The key.</value>
        private string Key => (string)Thread.GetData(_ldss) ?? DEFAULT_CHANNEL;
    }
}