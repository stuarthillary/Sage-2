using System.Collections.Generic;

namespace Highpoint.Sage.SystemDynamics.Design
{
    public interface IVariables
    {
        IEnumerable<IAux> Auxes
        {
            get;
        }
        IEnumerable<IFlow> Flows
        {
            get;
        }
        IEnumerable<IGraphicalFunction> GraphicalFunctions
        {
            get;
        }
        IEnumerable<IGroup> Groups
        {
            get;
        }
        IEnumerable<IModule> Modules
        {
            get;
        }
        IEnumerable<IStock> Stocks
        {
            get;
        }
    }
}
