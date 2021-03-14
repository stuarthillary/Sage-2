/* This source code licensed under the GNU Affero General Public License */
using System.Collections;

namespace Highpoint.Sage.Materials.Chemistry.Emissions
{
    /// <summary>
    /// This interface is implemented by a class that is capable of acting as an emissions model, computing the
    /// amount of material partitioned off as emissions, as a result of a specified situation.
    /// </summary>
    public interface IEmissionModel
    {
        /// <summary>
        /// Computes the effects of the emission.
        /// </summary>
        /// <param name="initial">The initial mixture.</param>
        /// <param name="final">The final mixture, after emissions are removed.</param>
        /// <param name="emission">The mixture that is emitted.</param>
        /// <param name="modifyInPlace">If this is true, then the emissions are removed from the initial mixture,
        /// and upon return from the call, the initial mixture will reflect the contents after the emission has taken place.</param>
        /// <param name="parameters">This is a hashtable of name/value pairs that represents all of the parameters necessary
        /// to describe this particular emission model, such as pressures, temperatures, gas sweep rates, etc.</param>
        void Process(
            Mixture initial,
            out Mixture final,
            out Mixture emission,
            bool modifyInPlace,
            Hashtable parameters);

        /// <summary>
        /// This is the list of names by which this emission model is specified, such as "Gas Sweep", "Vacuum Dry", etc.
        /// </summary>
        string[] Keys
        {
            get;
        }

        EmissionParam[] Parameters
        {
            get;
        }
        string ModelDescription
        {
            get;
        }

        bool PermitOverEmission
        {
            get; set;
        }
        bool PermitUnderEmission
        {
            get; set;
        }
    }
}
