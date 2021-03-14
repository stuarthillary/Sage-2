/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Materials.Chemistry.VaporPressure;
using System;
using System.Collections;
using PN = Highpoint.Sage.Materials.Chemistry.Emissions.EmissionModel.ParamNames;

namespace Highpoint.Sage.Materials.Chemistry.Emissions
{
    /// <summary>
    /// This model is used to calculate emissions from the evacuation (or depressurizing)
    /// of the vessel containing a VOC and a “noncondensable” or “inert” gas.  The model
    /// assumes that the pressure in the vessel decreases linearly with time and that there
    /// is no air leakage into the vessel.  Further, the assumptions are made that the
    /// composition of the VOC mixture does not change during the evacuation and that there
    /// is no temperature change (isothermal expansion).  Finally, the vapor displaced from
    /// the vessel is saturated with the VOC vapor at exit temperature.
    /// </summary>
    public class EvacuateModel : EmissionModel
    {

        /// <summary>
        /// One-size-fits-all API for all Emission Models, so that the Emissions Service can run
        /// an emission model that it has never seen before.
        /// <p></p>In order to successfully call the Evacuate model on this API, the parameters hashtable
        /// must include the following entries (see the Evacuate(...) method for details):<p></p>
        /// &quot;InitialPressure&quot;, &quot;FinalPressure&quot;, &quot;ControlTemperature&quot;, &quot;VesselVolume&quot;.
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

            //			double initialPressure = (double)parameters[PN.InitialPressure_P];
            //			double finalPressure = (double)parameters[PN.FinalPressure_P];
            //			double controlTemperature = (double)parameters[PN.ControlTemperature_K];
            //			double vesselVolume = (double)parameters[PN.VesselVolume_M3];

            double initialPressure = double.NaN;
            TryToRead(ref initialPressure, PN.InitialPressure_P, parameters);
            double finalPressure = double.NaN;
            TryToRead(ref finalPressure, PN.FinalPressure_P, parameters);
            double controlTemperature = double.NaN;
            TryToRead(ref controlTemperature, PN.ControlTemperature_K, parameters);
            double vesselVolume = double.NaN;
            TryToRead(ref vesselVolume, PN.VesselVolume_M3, parameters);

            EvaluateSuccessOfParameterReads();
            Evacuate(initial, out final, out emission, modifyInPlace, initialPressure, finalPressure, controlTemperature, vesselVolume);

            ReportProcessCall(this, initial, final, emission, parameters);

        }

        #region >>> Usability Support <<<
        private static readonly string description = "This model is used to calculate emissions from the evacuation (or depressurizing) of the vessel containing a VOC and a “noncondensable” or “inert” gas.  The model assumes that the pressure in the vessel decreases linearly with time and that there is no air leakage into the vessel.  Further, the assumptions are made that the composition of the VOC mixture does not change during the evacuation and that there is no temperature change (isothermal expansion).  Finally, the vapor displaced from the vessel is saturated with the VOC vapor at exit temperature.";
        private static readonly EmissionParam[] parameters
            = {
                                     new EmissionParam(PN.InitialPressure_P,"The initial pressure of the system, in Pascals."),
                                     new EmissionParam(PN.FinalPressure_P,"The final pressure of the system, in Pascals."),
                                     new EmissionParam(PN.ControlTemperature_K,"The control, or condenser temperature, in degrees Kelvin."),
                                     new EmissionParam(PN.VesselVolume_M3,"The volume of the vessel, in cubic meters.")
                                 };

        private static readonly string[] keys = { "Evacuate" };
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
        /// 
        /// </summary>
        /// <param name="initial">The mixture as it exists before the emission.</param>
        /// <param name="final">The resultant mixture after the emission.</param>
        /// <param name="emission">The mixture emitted as a result of this model.</param>
        /// <param name="modifyInPlace">If true, then the initial mixture is returned in its final state after emission, otherwise, it is left as-is.</param>
        /// <param name="initialPressure">The initial pressure of the system, in Pascals.</param>
        /// <param name="finalPressure">The final pressure of the system, in Pascals.</param>
        /// <param name="controlTemperature">The control, or condenser temperature, in degrees Kelvin.</param>
        /// <param name="vesselVolume">The volume of the vessel, in cubic meters.</param>
        public void Evacuate(
            Mixture initial,
            out Mixture final,
            out Mixture emission,
            bool modifyInPlace,
            double initialPressure,
            double finalPressure,
            double controlTemperature,
            double vesselVolume
            )
        {
            double vesselFreeSpace = vesselVolume - (initial.Volume * 0.001 /*convert liters to cubic meters*/);
            Mixture mixture = modifyInPlace ? initial : (Mixture)initial.Clone();
            emission = new Mixture(initial.Name + " Evacuation emissions");

            double denom = 0.0;
            ArrayList substances = new ArrayList(mixture.Constituents);
            foreach (Substance substance in substances)
            {
                MaterialType mt = substance.MaterialType;
                double moleFraction = mixture.GetMoleFraction(mt, MaterialType.FilterAcceptLiquidOnly);
                double vaporPressure = VaporPressureCalculator.ComputeVaporPressure(mt, controlTemperature, TemperatureUnits.Kelvin, PressureUnits.Pascals);
                denom += moleFraction * vaporPressure;
            }
            denom *= -2;
            denom += finalPressure;
            denom += initialPressure;

            double kTerm = (vesselFreeSpace * 2 * (initialPressure - finalPressure)) / (Chemistry.Constants.MolarGasConstant * controlTemperature * denom);

            substances = new ArrayList(mixture.Constituents);
            foreach (Substance substance in substances)
            {
                MaterialType mt = substance.MaterialType;
                double molWt = mt.MolecularWeight;
                double molFrac = mixture.GetMoleFraction(mt, MaterialType.FilterAcceptLiquidOnly);
                double vaporPressure = VaporPressureCalculator.ComputeVaporPressure(mt, controlTemperature, TemperatureUnits.Kelvin, PressureUnits.Pascals);
                double massOfSubstance = molWt * molFrac * vaporPressure * kTerm; // grams, since molWt = grams per mole.
                massOfSubstance *= .001; // kilograms per gram.

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
            final = mixture;
        }

    }
}
