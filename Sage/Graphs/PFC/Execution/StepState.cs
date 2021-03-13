/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.Graphs.PFC.Execution
{
    public enum StepState
    {
        Idle,
        Running,
        Complete, 
        Aborting, 
        Aborted,
        Stopping,
        Stopped, 
        Pausing,
        Paused,
        Holding,
        Held,
        Restarting
    }
}
