/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.SimCore;
using System;

namespace Highpoint.Sage.ItemBased.SplittersAndJoiners
{
    /// <summary>
    /// This splitter places anything that appears on its input port, simultaneously
    /// onto all of its output ports. If any output port cannot accept it, that output
    /// port is ignored <b>REJECTION OF PUSHES IS NOT SUPPORTED.</b>. Pulls and Peeks are not permitted.
    /// </summary>
    public class SimultaneousPushSplitter : Splitter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimultaneousPushSplitter"/> class.
        /// </summary>
        /// <param name="model">The model in which this <see cref="T:SimultaneousPushSplitter"/> will run.</param>
        /// <param name="name">The name of the new <see cref="T:SimultaneousPushSplitter"/>.</param>
        /// <param name="guid">The GUID of the new <see cref="T:SimultaneousPushSplitter"/>.</param>
        /// <param name="nOuts">The number of outputs this splitter will start with.</param>
		public SimultaneousPushSplitter(IModel model, string name, Guid guid, int nOuts) : base(model, name, guid, nOuts) { }
        protected override DataArrivalHandler GetDataArrivalHandler()
        {
            return new DataArrivalHandler(OnDataArrived);
        }

        protected override DataProvisionHandler GetDataProvisionHandler(int i)
        {
            return null;
        }
        protected override DataProvisionHandler GetPeekHandler(int i)
        {
            return null;
        }
        protected bool OnDataArrived(object data, IInputPort ip)
        {
            foreach (SimpleOutputPort op in m_outputs)
                op.OwnerPut(data);
            return true;
        }
    }
}