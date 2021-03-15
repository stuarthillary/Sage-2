using System.Collections.Generic;

namespace Highpoint.Sage.SystemDynamics.Design
{
    public interface IModel
    {
        string Name
        {
            get;
        }
        List<IStock> Stocks
        {
            get;
        }
        List<IFlow> Flows
        {
            get;
        }
        List<IAux> Auxes
        {
            get;
        }

    }
}
