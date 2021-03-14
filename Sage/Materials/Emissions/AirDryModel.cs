/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using PN = Highpoint.Sage.Materials.Chemistry.Emissions.EmissionModel.ParamNames;

namespace Highpoint.Sage.Materials.Chemistry.Emissions
{
    /// <summary>
    /// This model is used to calculate the emissions associated with drying solid product in a dryer 
    /// with no emission control equipment.  Thus, the model assumes that the entire solvent content 
    /// of the wet product cake is emitted to the atmosphere.  The calculation of the emission is then 
    /// a simple calculation based on the expected, or measured, dry product weight and the expected, 
    /// or measured, wet cake LOD (loss on drying).
    /// </summary>
    public class AirDryModel : EmissionModel
    {

        /// <summary>
        /// One-size-fits-all API for all Emission Models, so that the Emissions Service can run
        /// an emission model that it has never seen before.
        /// <p></p>In order to successfully call the Air Dry model on this API, the parameters hashtable
        /// must include the following entries (see the AirDry(...) method for details):<p></p>
        /// &quot;MassOfDriedProductCake&quot;, &quot;ControlTemperature&quot;, &quot;MaterialGuidToVolumeFraction&quot;.
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

            PrepareToReadLateBoundParameters();

            //			double massOfDriedProductCake = (double)parameters[PN.MassOfDriedProductCake_Kg];
            //			double controlTemperature = (double)parameters[PN.ControlTemperature_K];
            //			Hashtable materialGuidToVolumeFraction = (Hashtable)parameters[PN.MaterialGuidToVolumeFraction];

            double massOfDriedProductCake = double.NaN;
            TryToRead(ref massOfDriedProductCake, PN.MassOfDriedProductCake_Kg, parameters);
            double controlTemperature = double.NaN;
            TryToRead(ref controlTemperature, PN.ControlTemperature_K, parameters);
            Hashtable materialGuidToVolumeFraction = null;
            TryToRead(ref materialGuidToVolumeFraction, PN.MaterialGuidToVolumeFraction, parameters);

            EvaluateSuccessOfParameterReads();

            AirDry(initial, out final, out emission, modifyInPlace, massOfDriedProductCake, controlTemperature, materialGuidToVolumeFraction);

            ReportProcessCall(this, initial, final, emission, parameters);

        }


        #region >>> Usability Support <<<
        private static readonly string description = "This model is used to calculate the emissions associated with drying solid product in a dryer with no emission control equipment.  Thus, the model assumes that the entire solvent content of the wet product cake is emitted to the atmosphere.  The calculation of the emission is then a simple calculation based on the expected, or measured, dry product weight and the expected, or measured, wet cake LOD (loss on drying).";
        private static readonly EmissionParam[] parameters =
            {
                                   new EmissionParam(PN.MassOfDriedProductCake_Kg,"Kilogram mass of the post-drying product cake."),
                                   new EmissionParam(PN.ControlTemperature_K,"Control (or condenser) temperature, in degrees Kelvin."),
                                   new EmissionParam(PN.MaterialGuidToVolumeFraction,"A hashtable with the guids of materialTypes as keys, and the volumeFraction for that material type as values. VolumeFraction is the percent [0.0 to 1.0] of that material type in the offgas.")
                               };

        private static readonly string[] s_keys = { "Air Dry" };
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
        public override string[] Keys => s_keys;

        #endregion

        /// <summary>
        /// This model is used to calculate the emissions associated with drying solid product in a dryer 
        /// with no emission control equipment.  Thus, the model assumes that the entire solvent content 
        /// of the wet product cake is emitted to the atmosphere.  The calculation of the emission is then 
        /// a simple calculation based on the expected, or measured, dry product weight and the expected, 
        /// or measured, wet cake LOD (loss on drying).
        /// </summary>
        /// <param name="initial">The initial mixture that is dried.</param>
        /// <param name="final">An out-param that provides the mixture that results.</param>
        /// <param name="emission">An out-param that provides the emitted mixture.</param>
        /// <param name="modifyInPlace">True if the initial mixture provided is to be modified as a result of this call.</param>
        /// <param name="massOfDriedProductCake">Kilogram mass of the post-drying product cake.</param>
        /// <param name="controlTemperature">Control (or condenser) temperature, in degrees Kelvin.</param>
        /// <param name="materialGuidToVolumeFraction">A hashtable with the guids of materialTypes as keys, and the volumeFraction for that material type as values. VolumeFraction is the percent [0.0 to 1.0] of that material type in the offgas.</param>
        public void AirDry(
            Mixture initial,
            out Mixture final,
            out Mixture emission,
            bool modifyInPlace,
            double massOfDriedProductCake,
            // ReSharper disable once UnusedParameter.Global
            double controlTemperature,
            Hashtable materialGuidToVolumeFraction
            )
        {

            emission = new Mixture(initial.Name + " Air Dry emissions");
            Mixture mixture = modifyInPlace ? initial : (Mixture)initial.Clone();

            if (initial.Mass > 0.0)
            {
                double lossOnDryingPct = 1.0 - (massOfDriedProductCake / initial.Mass);

                double aggDensity = 0.0;
                foreach (Substance substance in mixture.Constituents)
                {
                    MaterialType mt = substance.MaterialType;
                    double volFrac = (double)materialGuidToVolumeFraction[mt.Guid];
                    double density = substance.Density;
                    aggDensity += volFrac * density;
                }
                double kTerm = massOfDriedProductCake * (lossOnDryingPct / (1.0 - lossOnDryingPct)) / aggDensity;

                if (double.IsPositiveInfinity(kTerm))
                    kTerm = 0.0;
                ArrayList substances = new ArrayList(mixture.Constituents);
                foreach (Substance substance in substances)
                {
                    MaterialType mt = substance.MaterialType;
                    double massOfSubstance = kTerm * (double)materialGuidToVolumeFraction[mt.Guid] * substance.Density;

                    if (!PermitOverEmission)
                        massOfSubstance = Math.Min(substance.Mass, massOfSubstance);
                    if (!PermitUnderEmission)
                        massOfSubstance = Math.Max(0, massOfSubstance);


                    Substance emitted = (Substance)mt.CreateMass(massOfSubstance, substance.Temperature);
                    Substance.ApplyMaterialSpecs(emitted, substance);
                    emission.AddMaterial(emitted);
                }

                foreach (Substance s in emission.Constituents)
                {
                    mixture.RemoveMaterial(s.MaterialType, s.Mass);
                }
            }
            final = mixture;
        }
    }
}
