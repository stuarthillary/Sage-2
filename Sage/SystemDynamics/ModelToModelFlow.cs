using System;

namespace Highpoint.Sage.SystemDynamics
{
    public struct ModelToModelFlow<TModelTypeFrom, TModelTypeTo>
        where TModelTypeFrom : StateBase<TModelTypeFrom>
        where TModelTypeTo : StateBase<TModelTypeTo>
    {
        public ModelToModelFlow(TModelTypeFrom flowFrom, TModelTypeTo flowTo, Func<double> flow)
        {

        }
    }
}
