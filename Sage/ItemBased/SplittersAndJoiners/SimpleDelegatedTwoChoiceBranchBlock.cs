/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.SimCore;
using System;

namespace Highpoint.Sage.ItemBased.SplittersAndJoiners
{
    public class SimpleDelegatedTwoChoiceBranchBlock : SimpleTwoChoiceBranchBlock
    {
        public IOutputPort YesPort;
        public IOutputPort NoPort;
        private BooleanDecider _bd = null;
        public SimpleDelegatedTwoChoiceBranchBlock(IModel model, string name, Guid guid) : base(model, name, guid)
        {
            YesPort = Out0;
            NoPort = Out1;
        }

        public BooleanDecider BooleanDeciderDelegate
        {
            get
            {
                return _bd;
            }
            set
            {
                _bd = value;
            }
        }
        protected override IPort ChoosePort(object dataObject)
        {
            if (_bd != null)
            {
                if (_bd(dataObject))
                {
                    return YesPort;
                }
                else
                {
                    return NoPort;
                }
            }
            else
            {
                return null;
            }
        }
    }
}
