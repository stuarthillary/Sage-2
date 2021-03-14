/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.Materials.Chemistry.Emissions
{
    /// <summary>
    /// Characterizes a parameter that is required by an emissions model.
    /// </summary>
    [Serializable]
    public class EmissionParam
    {
        private string _name;
        private string _description;
        /// <summary>
        /// Creates a new instance of the <see cref="T:EmissionParam"/> class for serialization purposes.
        /// </summary>
		protected EmissionParam()
        {
        } // for serialization.
        public EmissionParam(string name, string description)
        {
            _name = name;
            _description = description;
        }
        /// <summary>
        /// Gets or sets the name of the <see cref="T:EmissionParam"/>.
        /// </summary>
        /// <value>The name of the <see cref="T:EmissionParam"/>.</value>
		public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }
        /// <summary>
        /// Gets or sets the description of the <see cref="T:EmissionParam"/>.
        /// </summary>
        /// <value>The description of the <see cref="T:EmissionParam"/>.</value>
		public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                _description = value;
            }
        }
    }
}
