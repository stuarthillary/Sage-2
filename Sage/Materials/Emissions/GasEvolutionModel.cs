/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Materials.Chemistry.VaporPressure;
using System;
using System.Collections;
using PN = Highpoint.Sage.Materials.Chemistry.Emissions.EmissionModel.ParamNames;
using VPC = Highpoint.Sage.Materials.Chemistry.VaporPressure.VaporPressureCalculator;

namespace Highpoint.Sage.Materials.Chemistry.Emissions
{
    /// <summary>
    /// This model is used to calculate the emissions associated with the generation
    /// of a non-condensable gas as the result of a chemical reaction.  The model assumes
    /// that the gas is exposed to the VOC, becomes saturated with the VOC vapor at the
    /// exit temperature, and leaves the system.  The model also assumes that the system
    /// pressure is 760 mmHg, atmospheric pressure.  This model is identical to the Gas
    /// Sweep model, except that the non-condensable sweep gas (usually nitrogen) is replaced
    /// in this model by a non-condensable gas generated in situ. It is important to note that
    /// if the generated gas is itself a VOS, non-VOS or TVOS, then the emission of this gas
    /// must be accounted for by a separate model, usually the Mass Balance model.
    /// <p>For example, if n-butyllithium is used in a chemical reaction and generates butane
    /// gas as a byproduct, the evolution of butane gas causes emissions of the VOC present in
    /// the system.  These emissions can be modeled by the Gas Evolution model (to account for
    /// the emission of the VOC vapor which saturates the butane gas) and the Mass Balance
    /// model (to account for the emission of the VOC butane).</p>
    /// </summary>
    public class GasEvolutionModel : EmissionModel
    {

        /// <summary>
        /// One-size-fits-all API for all Emission Models, so that the Emissions Service can run
        /// an emission model that it has never seen before.
        /// <p></p>In order to successfully call the Gas Evolution model on this API, the parameters hashtable
        /// must include the following entries (see the GasEvolution(...) method for details):<p></p>
        /// &quot;MolesOfGasEvolved&quot;, &quot;SystemPressure&quot; and &quot;ControlTemperature&quot;. If there
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

            //			double nMolesEvolved = (double)parameters[PN.MolesOfGasEvolved];
            //			double controlTemperature = (double)parameters[PN.ControlTemperature_K];
            //			double systemPressure = GetSystemPressure(parameters);

            double nMolesEvolved = double.NaN;
            TryToRead(ref nMolesEvolved, PN.MolesOfGasEvolved, parameters);
            double controlTemperature = double.NaN;
            TryToRead(ref controlTemperature, PN.ControlTemperature_K, parameters);
            double systemPressure = GetSystemPressure(parameters);

            EvaluateSuccessOfParameterReads();

            GasEvolution(initial, out final, out emission, modifyInPlace, nMolesEvolved, controlTemperature, systemPressure);

            ReportProcessCall(this, initial, final, emission, parameters);

        }

        #region >>> Usability Support <<<
        private static readonly string description = "This model is used to calculate the emissions associated with the generation of a non-condensable gas as the result of a chemical reaction.  The model assumes that the gas is exposed to the VOC, becomes saturated with the VOC vapor at the exit temperature, and leaves the system.  The model also assumes that the system pressure is 760 mmHg, atmospheric pressure.  This model is identical to the Gas Sweep model, except that the non-condensable sweep gas (usually nitrogen) is replaced in this model by a non-condensable gas generated in situ.\r\n\r\nIt is important to note that if the generated gas is itself a VOS, non-VOS or TVOS, then the emission of this gas must be accounted for by a separate model, usually the Mass Balance model. For example, if n-butyllithium is used in a chemical reaction and generates butane gas as a byproduct, the evolution of butane gas causes emissions of the VOC present in the system.  These emissions can be modeled by the Gas Evolution model (to account for the emission of the VOC vapor which saturates the butane gas) and the Mass Balance model (to account for the emission of the VOC butane).";
        private static readonly EmissionParam[] parameters =
            {
                                   new EmissionParam(PN.MolesOfGasEvolved,"The number of moles of gas evolved."),
                                   new EmissionParam(PN.ControlTemperature_K,"The control or condenser temperature, in degrees kelvin."),
                                   new EmissionParam(PN.SystemPressure_P,"The pressure of the system during the emission operation, in Pascals. This parameter can also be called \"Final Pressure\".")
                               };

        private static readonly string[] keys = { "Gas Evolution" };
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
        /// This model is used to calculate the emissions associated with the generation
        /// of a non-condensable gas as the result of a chemical reaction.  The model assumes
        /// that the gas is exposed to the VOC, becomes saturated with the VOC vapor at the
        /// exit temperature, and leaves the system.  The model also assumes that the system
        /// pressure is 760 mmHg, atmospheric pressure.  This model is identical to the Gas
        /// Sweep model, except that the non-condensable sweep gas (usually nitrogen) is replaced
        /// in this model by a non-condensable gas generated in situ. It is important to note that
        /// if the generated gas is itself a VOS, non-VOS or TVOS, then the emission of this gas
        /// must be accounted for by a separate model, usually the Mass Balance model.
        /// <p>For example, if n-butyllithium is used in a chemical reaction and generates butane
        /// gas as a byproduct, the evolution of butane gas causes emissions of the VOC present in
        /// the system.  These emissions can be modeled by the Gas Evolution model (to account for
        /// the emission of the VOC vapor which saturates the butane gas) and the Mass Balance
        /// model (to account for the emission of the VOC butane).</p>
        /// </summary>
        /// <param name="initial">The mixture as it exists before the emission.</param>
        /// <param name="final">The resultant mixture after the emission.</param>
        /// <param name="emission">The mixture emitted as a result of this model.</param>
        /// <param name="modifyInPlace">If true, then the initial mixture is returned in its final state after emission, otherwise, it is left as-is.</param>
        /// <param name="nMolesEvolved">The number of moles of gas evolved.</param>
        /// <param name="controlTemperature">The control or condenser temperature, in degrees kelvin.</param>
        /// <param name="systemPressure">The pressure of the system (or vessel) in Pascals.</param>
        public void GasEvolution(
            Mixture initial,
            out Mixture final,
            out Mixture emission,
            bool modifyInPlace,
            double nMolesEvolved,
            double controlTemperature,
            double systemPressure
            )
        {
            Mixture mixture = modifyInPlace ? initial : (Mixture)initial.Clone();
            emission = new Mixture(initial.Name + " GasEvolution emissions");

            double spp = VPC.SumOfPartialPressures(mixture, controlTemperature);

            foreach (Substance substance in mixture.Constituents)
            {
                MaterialType mt = substance.MaterialType;
                double molWt = mt.MolecularWeight;
                double molFrac = mixture.GetMoleFraction(mt, MaterialType.FilterAcceptLiquidOnly);
                double vaporPressure = VaporPressureCalculator.ComputeVaporPressure(mt, controlTemperature, TemperatureUnits.Kelvin, PressureUnits.Pascals);
                double massOfSubstance = nMolesEvolved * molWt * molFrac * vaporPressure / (systemPressure - spp);
                // At this point, massOfSubstance is in grams (since molWt is in grams per mole)
                massOfSubstance /= 1000;
                // now, mass is in kg.

                if (!PermitOverEmission)
                    massOfSubstance = Math.Min(substance.Mass, massOfSubstance);
                if (!PermitUnderEmission)
                    massOfSubstance = Math.Max(0, massOfSubstance);

                Substance emitted = (Substance)mt.CreateMass(massOfSubstance, controlTemperature + Chemistry.Constants.KELVIN_TO_CELSIUS);
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
