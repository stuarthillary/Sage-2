/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.Randoms;
using Highpoint.Sage.SimCore;
using System;

namespace Highpoint.Sage.ItemBased.SplittersAndJoiners
{
    public class SimpleStochasticTwoChoiceBranchBlock : SimpleTwoChoiceBranchBlock
    {
        private readonly double _percentageOut0;
        private readonly IRandomChannel _randomChannel;
        public SimpleStochasticTwoChoiceBranchBlock(IModel model, string name, Guid guid, double percentageOut0) : base(model, name, guid)
        {
            _percentageOut0 = percentageOut0;
            _randomChannel = model.RandomServer.GetRandomChannel();
        }
        protected override IPort ChoosePort(object dataObject)
        {
            if (_randomChannel.NextDouble() <= _percentageOut0)
                return outputs[0];
            return outputs[1];
        }

    }
}
