/* This source code licensed under the GNU Affero General Public License */

// ReSharper disable UnusedMemberInSuper.Global

namespace Highpoint.Sage.Materials.Chemistry.VaporPressure
{
    public interface IAntoinesCoefficients3 : IAntoinesCoefficients
    {
        double A
        {
            get;
        }
        double B
        {
            get;
        }
        double C
        {
            get;
        }
    }
}
