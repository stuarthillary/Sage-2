/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.SimCore;
using System;

namespace Highpoint.Sage.ItemBased.SplittersAndJoiners
{
    public abstract class SimpleTwoChoiceBranchBlock : SimpleBranchBlock
    {
        protected IOutputPort Out0, Out1;
        public SimpleTwoChoiceBranchBlock(IModel model, string name, Guid guid) : base(model, name, guid) { }

        protected override void SetUpOutputPorts()
        {
            Out0 = new SimpleOutputPort(Model, "Out0", Guid.NewGuid(), this, null, null);
            // Ports.AddPort(m_out0); <-- Done in port's ctor.
            Out1 = new SimpleOutputPort(Model, "Out1", Guid.NewGuid(), this, null, null);
            // Ports.AddPort(m_out1); <-- Done in port's ctor.
            outputs = new IOutputPort[] { Out0, Out1 };
        }
    }
}
