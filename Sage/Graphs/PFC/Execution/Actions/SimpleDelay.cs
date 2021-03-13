/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Mathematics;
using Highpoint.Sage.SimCore;
using System;

namespace Highpoint.Sage.Graphs.PFC.Execution.Actions
{
    public class SimpleDelay : PfcActor
    {

        private readonly ITimeSpanDistribution _tsd;

        public SimpleDelay(IModel model, string name, Guid guid, IPfcStepNode myStepNode, ITimeSpanDistribution tsd)
        : base(model, name, guid, myStepNode)
        {
            _tsd = tsd;
        }

        public override void GetPermissionToStart(PfcExecutionContext myPfcec, StepStateMachine ssm)
        {
        }

        public override void Run(PfcExecutionContext pfcec, StepStateMachine ssm)
        {
            IExecutive exec = Model.Executive;
            exec.CurrentEventController.SuspendUntil(exec.Now + _tsd.GetNext());
        }


        public override void SetStochasticMode(StochasticMode mode)
        {
            switch (mode)
            {
                case StochasticMode.Full:
                    _tsd.SetCDFInterval(0.0, 1.0);
                    break;
                case StochasticMode.Schedule:
                    _tsd.SetCDFInterval(0.5, 0.5);
                    break;
                default:
                    throw new ApplicationException("Unknown stochastic mode " + mode);
#pragma warning disable 0162 // Unreachable Code Detected
                    // Pragma is because if everything is okay, this code *will* be unreachable.
                    break;
#pragma warning disable 0162
            }
        }
    }
}
