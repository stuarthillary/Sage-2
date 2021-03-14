/* This source code licensed under the GNU Affero General Public License */
using System.Collections;

namespace Highpoint.Sage.Materials.Chemistry.Emissions
{
    /// <summary>
    /// This model is a placeholder for operations that cause no emissions.
    /// </summary>
    public class NoEmissionModel : EmissionModel
    {

        /// <summary>
        /// One-size-fits-all API for all Emission Models, so that the Emissions Service can run
        /// an emission model that it has never seen before.
        /// <p></p>The NoEmissions model is included for completeness, and has no entries that are
        /// required to be present in the parameters hashtable.
        /// </summary>
        /// <param name="initial">The initial mixture on which the emission model is to run.</param>
        /// <param name="final">The final mixture that is delivered after the emission model has run.</param>
        /// <param name="emission">The mixture that is evolved in the process of the emission.</param>
        /// <param name="modifyInPlace">True if the initial mixture is to be modified by the service.</param>
        /// <param name="parameters">A hashtable of name/value pairs containing additional parameters.</param>
        public override void Process(
            Mixture initial,
            out Mixture final,
            out Mixture emission,
            bool modifyInPlace,
            Hashtable parameters)
        {

            NoEmission(initial, out final, out emission, modifyInPlace);

            ReportProcessCall(this, initial, final, emission, parameters);
        }

        #region >>> Usability Support <<<
        private static readonly string description = "This model is a placeholder for operations that cause no emissions.";
        private static readonly EmissionParam[] parameters = { };
        private static readonly string[] keys = { "No Emissions" };
        /// <summary>
        /// This is a description of what emissions mode this model computes (such as Air Dry, Gas Sweep, etc.)
        /// </summary>
        public override string ModelDescription => description;

        /// <summary>
        /// This is the list of parameters this model uses, and therefore expects as input.
        /// </summary>
        public override EmissionParam[] Parameters => parameters;

        /// <summary>
        /// The keys which, when fed to the Emissions Service's ProcessEmission method, determines
        /// that this model is to be called.
        /// </summary>
        public override string[] Keys => keys;

        #endregion

        /// <summary>
        /// This model is a placeholder for operations that cause no emissions.
        /// </summary>
        /// <param name="initial">The mixture as it exists before the emission.</param>
        /// <param name="final">The resultant mixture after the emission.</param>
        /// <param name="emission">The mixture emitted as a result of this model.</param>
        /// <param name="modifyInPlace">If true, then the initial mixture is returned in its final state after emission, otherwise, it is left as-is.</param>
        public void NoEmission(
            Mixture initial,
            out Mixture final,
            out Mixture emission,
            bool modifyInPlace
            )
        {
            final = modifyInPlace ? initial : (Mixture)initial.Clone();
            emission = new Mixture(initial.Name + " emissions") { Temperature = initial.Temperature };

        }
    }
}
