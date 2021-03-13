/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Graphs.PFC
{
    public class PfcUnitInfo : IPfcUnitInfo
    {

        #region Private Fields
        private string _name;
        private int _sequenceNumber;
        #endregion 

        #region Constructors
        public PfcUnitInfo(string name, int sequenceNumber)
        {
            _name = name;
            _sequenceNumber = sequenceNumber;
        }
        #endregion 

        #region IPfcUnit Members

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

        public int SequenceNumber
        {
            get
            {
                return _sequenceNumber;
            }
            set
            {
                _sequenceNumber = value;
            }
        }

        #endregion
    }
}
