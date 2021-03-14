/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Materials.Chemistry.VaporPressure;
using System;
using System.Collections;
using PN = Highpoint.Sage.Materials.Chemistry.Emissions.EmissionModel.ParamNames;
using VPC = Highpoint.Sage.Materials.Chemistry.VaporPressure.VaporPressureCalculator;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable RedundantAssignment
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable InconsistentNaming

namespace Highpoint.Sage.Materials.Chemistry.Emissions
{
    /// <summary>
    /// This model is used to calculate the emissions associated with sweeping or purging
    /// a vessel or other piece of equipment with a non-condensable gas (nitrogen).
    /// The model assumes that the sweep gas enters the system at 25°C, becomes saturated
    /// with the VOC vapor at the exit temperature, and leaves the system.
    /// </summary>
    public class GasSweepModel : EmissionModel
    {
        /// <summary>
        /// One-size-fits-all API for all Emission Models, so that the Emissions Service can run
        /// an emission model that it has never seen before.
        /// <p></p>In order to successfully call the Gas Sweep model on this API, the parameters hashtable
        /// must include the following entries (see the GasSweep(...) method for details):<p></p>
        /// &quot;GasSweepRate&quot;, &quot;GasSweepDuration&quot;, &quot;SystemPressure&quot; and &quot;ControlTemperature&quot;. If there
        /// is no entry under &quot;SystemPressure&quot;, then this method looks for entries under &quot;InitialPressure&quot; 
        /// and &quot;FinalPressure&quot; and uses their average.
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

            //			double gasSweepRate = (double)parameters["GasSweepRate"];
            //			double gasSweepDuration = (double)parameters["GasSweepDuration"];
            //			double controlTemperature = (double)parameters["ControlTemperature"];
            //			double systemPressure = GetSystemPressure(parameters);

            double gasSweepRate = double.NaN;
            TryToRead(ref gasSweepRate, PN.GasSweepRate_M3PerMin, parameters);
            double gasSweepDuration = double.NaN;
            TryToRead(ref gasSweepDuration, PN.GasSweepDuration_Min, parameters);
            double controlTemperature = double.NaN;
            TryToRead(ref controlTemperature, PN.ControlTemperature_K, parameters);
            double systemPressure = GetSystemPressure(parameters);

            EvaluateSuccessOfParameterReads();

            GasSweep(initial, out final, out emission, modifyInPlace, gasSweepRate, gasSweepDuration, controlTemperature, systemPressure);

            ReportProcessCall(this, initial, final, emission, parameters);

        }


        #region >>> Usability Support <<<
        private static readonly string description = "This model is used to calculate the emissions associated with sweeping or purging a vessel or other piece of equipment with a non-condensable gas (nitrogen). The model assumes that the sweep gas enters the system at 25°C, becomes saturated with the VOC vapor at the exit temperature, and leaves the system.";
        private static readonly EmissionParam[] parameters =
            {
                                   new EmissionParam(PN.GasSweepRate_M3PerMin,"The gas sweep rate, in cubic meters per time unit."),
                                   new EmissionParam(PN.GasSweepDuration_Min,"The gas sweep duration, in minutes."),
                                   new EmissionParam(PN.ControlTemperature_K,"The control or condenser temperature, in degrees Kelvin."),
                                   new EmissionParam(PN.SystemPressure_P,"The pressure of the system during the emission operation, in Pascals. This parameter can also be called \"Final Pressure\".")
                               };

        private static readonly string[] keys = { "Gas Sweep" };
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
        /// This model is used to calculate the emissions associated with sweeping or purging
        /// a vessel or other piece of equipment with a non-condensable gas (nitrogen).
        /// The model assumes that the sweep gas enters the system at 25°C, becomes saturated
        /// with the VOC vapor at the exit temperature, and leaves the system.
        /// </summary>
        /// <param name="initial">The mixture as it exists before the emission.</param>
        /// <param name="final">The resultant mixture after the emission.</param>
        /// <param name="emission">The mixture emitted as a result of this model.</param>
        /// <param name="modifyInPlace">If true, then the initial mixture is returned in its final state after emission, otherwise, it is left as-is.</param>
        /// <param name="gasSweepRate">The gas sweep rate, in cubic meters per time unit.</param>
        /// <param name="gasSweepDuration">The gas sweep duration, in matching time units.</param>
        /// <param name="controlTemperature">The control or condenser temperature, in degrees Kelvin.</param>
        /// <param name="systemPressure">The system (vessel) pressure, in Pascals.</param>
        public void GasSweep(
            Mixture initial,
            out Mixture final,
            out Mixture emission,
            bool modifyInPlace,
            double gasSweepRate, /*meters^3 per minute*/
            double gasSweepDuration, /*minutes*/
            double controlTemperature,
            double systemPressure
            )
        {
            Mixture mixture = modifyInPlace ? initial : (Mixture)initial.Clone();
            emission = new Mixture(initial.Name + " GasSweep emissions");

            double spp = VPC.SumOfPartialPressures(mixture, controlTemperature);
            /* double gasVolume = gasSweepDuration * gasSweepRate;
			gas volume is now in cubic meters*/
            double constantPart = (systemPressure * gasSweepDuration * gasSweepRate) / (Chemistry.Constants.MolarGasConstant * (systemPressure - spp) * controlTemperature);

            foreach (Substance substance in mixture.Constituents)
            {
                MaterialType mt = substance.MaterialType;
                double molWt = mt.MolecularWeight;
                double molFrac = mixture.GetMoleFraction(mt, MaterialType.FilterAcceptLiquidOnly);
                double vaporPressure = VaporPressureCalculator.ComputeVaporPressure(mt, controlTemperature, TemperatureUnits.Kelvin, PressureUnits.Pascals);
                double massOfSubstance = molWt * molFrac * vaporPressure * constantPart;
                // At this point, massOfSubstance is in grams (since molWt is in grams per mole)
                massOfSubstance /= 1000;
                // now, massOfSubstance is in kg.

                if (!PermitOverEmission)
                    massOfSubstance = Math.Min(substance.Mass, massOfSubstance);
                if (!PermitUnderEmission)
                    massOfSubstance = Math.Max(0, massOfSubstance);

                Substance emitted = (Substance)mt.CreateMass(massOfSubstance, controlTemperature + Chemistry.Constants.KELVIN_TO_CELSIUS);
                Substance.ApplyMaterialSpecs(emitted, substance);
                emission.AddMaterial(emitted);
            }

            double saturation = 1.00;
            if (ActiveEquationSet.Equals(EquationSet.MACT))
            {
                if (gasSweepRate > /*100 scfm = */2.831685 /* cubic meters per minute */ )
                    saturation *= 0.25;
            }


            foreach (Substance s in emission.Constituents)
            {
                mixture.RemoveMaterial(s.MaterialType, s.Mass * saturation);
            }
            final = mixture;
        }
    }
}
