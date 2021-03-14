/* This source code licensed under the GNU Affero General Public License */
using System.Collections;
using _Debug = System.Diagnostics.Debug;
using K = Highpoint.Sage.Materials.Chemistry.Emissions.EmissionModel.Constants;
using PN = Highpoint.Sage.Materials.Chemistry.Emissions.EmissionModel.ParamNames;

namespace Highpoint.Sage.Materials.Chemistry.Emissions
{
    /// <summary>
    /// Base class for emission models, with a few helpful auxiliary methods &amp; constant values. It is not required that an
    /// Emission Model derive from this class.
    /// </summary>
    public abstract class EmissionModel : IEmissionModel
    {

        /// <summary>
        /// Contains constant strings that are to be used as keys for storing emissions parameters into 
        /// the parameters hashtable that holds the data pertinent to an emissions calculation.
        /// </summary>
		public class ParamNames
        {
            /// <summary>
            /// The key that identifies the Air Leak Duration.
            /// </summary>
            public static string AirLeakDuration_Min = "AirLeakDuration";
            /// <summary>
            /// The key that identifies the Air Leak Rate.
            /// </summary>
            public static string AirLeakRate_KgPerMin = "AirLeakRate";
            /// <summary>
            /// The key that identifies whether the Condenser is enabled.
            /// </summary>
            public static string CondenserEnabled = "CondenserEnabled";
            /// <summary>
            /// The key that identifies the Condenser Temperature.
            /// </summary>
            public static string CondenserTemperature_K = "CondenserTemperature";
            /// <summary>
            /// The key that identifies the ControlTemperature.
            /// </summary>
            public static string ControlTemperature_K = "ControlTemperature";
            /// <summary>
            /// The key that identifies the DesiredEmission.
            /// </summary>
            public static string DesiredEmission = "DesiredEmission";
            /// <summary>
            /// The key that identifies the Material Type Guid To Emit.
            /// </summary>
            public static string MaterialTypeGuidToEmit = "MaterialTypeGuidToEmit";
            /// <summary>
            /// The key that identifies the Material Spec Guid To Emit.
            /// </summary>
            public static string MaterialSpecGuidToEmit = "MaterialSpecGuidToEmit";
            /// <summary>
            /// The key that identifies the Material Fraction To Emit.
            /// </summary>
            public static string MaterialFractionToEmit = "MaterialFractionToEmit";
            /// <summary>
            /// The key that identifies the Material Mass To Emit.
            /// </summary>
            public static string MaterialMassToEmit = "MaterialMassToEmit";
            /// <summary>
            /// The key that identifies the Final Pressure.
            /// </summary>
            public static string FinalPressure_P = "FinalPressure";
            /// <summary>
            /// The key that identifies the Final Temperature.
            /// </summary>
            public static string FinalTemperature_K = "FinalTemperature";
            /// <summary>
            /// The key that identifies the Gas Sweep Duration.
            /// </summary>
            public static string GasSweepDuration_Min = "GasSweepDuration";
            /// <summary>
            /// The key that identifies the Gas Sweep Rate.
            /// </summary>
            public static string GasSweepRate_M3PerMin = "GasSweepRate";
            /// <summary>
            /// The key that identifies the Initial Pressure.
            /// </summary>
            public static string InitialPressure_P = "InitialPressure";
            /// <summary>
            /// The key that identifies the Initial Temperature.
            /// </summary>
            public static string InitialTemperature_K = "InitialTemperature";
            /// <summary>
            /// The key that identifies the Mass Of Dried Product Cake.
            /// </summary>
            public static string MassOfDriedProductCake_Kg = "MassOfDriedProductCake";
            /// <summary>
            /// The key that identifies the Material Guid To Volume Fraction.
            /// </summary>
            public static string MaterialGuidToVolumeFraction = "MaterialGuidToVolumeFraction";
            /// <summary>
            /// The key that identifies the Material To Add.
            /// </summary>
            public static string MaterialToAdd = "MaterialToAdd";
            /// <summary>
            /// The key that identifies the Moles Of Gas Evolved.
            /// </summary>
            public static string MolesOfGasEvolved = "MolesOfGasEvolved";
            /// <summary>
            /// The key that identifies the System Pressure.
            /// </summary>
            public static string SystemPressure_P = "SystemPressure";
            /// <summary>
            /// The key that identifies the Vacuum System Pressure.
            /// </summary>
            public static string VacuumSystemPressure_P = "VacuumSystemPressure";
            /// <summary>
            /// The key that identifies the Vessel Volume.
            /// </summary>
            public static string VesselVolume_M3 = "VesselVolume";
            /// <summary>
            /// The key that identifies the Fill Volume.
            /// </summary>
            public static string FillVolume_M3 = "FillVolume";
        }

        /// <summary>
        /// Determines which equation set is used by the emissions model. Default is CTG.
        /// </summary>
        public enum EquationSet
        {
            /// <summary>
            /// 1978 Control Technique Guidelines
            /// </summary>
            CTG,
            /// <summary>
            /// 1998 Pharmaceutical Maximum Achievable Control Technique guidelines.
            /// </summary>
            MACT
        }


        /// <summary>
        /// Useful constants for emission model computations.
        /// </summary>
        public class Constants : Chemistry.Constants
        {
            /// <summary>
            /// Multiply a double representing the number of pounds (avoirdupois) of a substance by this, to get kilograms.
            /// </summary>
            public static double KgPerPound = 0.453592;
            /// <summary>
            /// Multiply a double representing the number of mm of mercury of pressure, to get pascals.
            /// </summary>
            public static double PascalsPerMmHg = 133.322;
            /// <summary>
            /// Multiply a double representing the number of Bar absolute of pressure, to get pascals.
            /// </summary>
            public static double PascalsPerBar = 100000.0;
            /// <summary>
            /// Multiply a double representing the number of gallons of volume, to get cubic feet.
            /// </summary>
            public static double CubicFtPerGallon = 0.134;
            /// <summary>
            /// Multiply a double representing the number of gallons of volume, to get liters.
            /// </summary>
            public static double LitersPerGallon = 3.7854118;
            /// <summary>
            /// Multiply a double representing the number of cubic feet of volume, to get cubic meters.
            /// </summary>
            public static double CubicFtPerCubicMeter = 35.314667;
            /// <summary>
            /// Multiply a double representing the number of liters of volume, to get cubic meters.
            /// </summary>
            public static double CubicMetersPerLiter = 0.001;
            /// <summary>
            /// Multiply a double representing the number of cubic meters of air at STP to get kg of air.
            /// This is derived from the facts that air's molecular weight (mass-weighted-average) is 28.97 grams per mole
            /// (see http://www.engineeringtoolbox.com/8_679.html) and that air occupies 22.4 liters per mole at STP
            /// (see http://www.epa.gov/nerlesd1/chemistry/ppcp/prefix.htm).<p></p>
            /// ((28.97 g/mole)/(1000 g/kg)) / ((22.4 liters/mole)*1000 liters/m^3) = 1.293304 kg/m^3
            /// </summary>
            public static double AirKgPerCubicMeterStp = 1.293304;

            /// <summary>
            /// The mass-weighted average molecular weight of air. See http://www.engineeringtoolbox.com/8_679.html
            /// </summary>
            public static double MolecularWeightOfAir = 28.97;
            #region Explanation of MolWtOfAir
            //http://www.engineeringtoolbox.com/8_679.html
            //Components in Dry Air
            //The two most dominant components in dry air are Oxygen and Nitrogen.
            //Oxygen has an 16 atomic unit mass and Nitrogen has a 14 atomic units mass.
            //Since both these elements are diatomic in air - O2 and N2, the molecular 
            //mass of Oxygen is 32 and the molecular mass of Nitrogen is 28.
            //
            //Since air is a mixture of gases the total mass can be estimated by adding
            //the weight of all major components as shown below: 
            //
            //Components in Dry Air Volume Ratio compared to Dry Air  Molecular Mass - M(kg/kmol)  Molecular Mass in Air  
            //Oxygen 0.2095 32.00 6.704 
            //Nitrogen 0.7809 28.02 21.88 
            //Carbon Dioxide 0.0003 44.01 0.013 
            //Hydrogen 0.0000005  2.02 0 
            //Argon 0.00933 39.94 0.373 
            //Neon 0.000018 20.18 0 
            //Helium 0.000005 4.00 0 
            //Krypton 0.000001 83.8 0 
            //Xenon 0.09 10-6 131.29 0 
            //Total Molecular Mass of Air 28.97 
            #endregion

        }


        #region Private Fields
        private static readonly bool diagnostics = Diagnostics.DiagnosticAids.Diagnostics("Emissions.ModelParameterDumps");
        private readonly ArrayList _errMsgs = new ArrayList();
        protected ArrayList ErrorMessages => _errMsgs;

        #endregion

        /// <summary>
        /// Determines whether the engine uses 	CTG (1978 Control Technology Guidelines) or MACT (1998 Pharmaceutical Maximum Achievable Control Technology guidelines) equations for its computation.
        /// </summary>
        public static EquationSet ActiveEquationSet { get; set; } = EquationSet.CTG;

        /// <summary>
        /// Performs initial bookkeeping in support of determining if an error occurred while reading late-bound parameters for an emissions model.
        /// </summary>
        protected void PrepareToReadLateBoundParameters()
        {
            _errMsgs.Clear();
        }

        /// <summary>
        /// Attempts to read a parameter by name from the late-bound parameters list, providing an error message if the parameter is missing.
        /// </summary>
        /// <param name="variable">The double into which the read value is to be placed.</param>
        /// <param name="paramName">The string name of the parameter. Should be one of the EmissionModel.ParamNames entries.</param>
        /// <param name="parameters">The late-bound hashtable.</param>
        protected void TryToRead(ref double variable, string paramName, Hashtable parameters)
        {
            if (parameters.Contains(paramName))
            {
                variable = (double)parameters[paramName];
            }
            else
            {
                variable = double.NaN;
                _errMsgs.Add("Attempt to read missing parameter, \"" + paramName + "\" from supplied parameters.\r\n");
            }
        }

        /// <summary>
        /// Attempts to read a parameter by name from the late-bound parameters list, providing an error message if the parameter is missing.
        /// </summary>
        /// <param name="variable">The double into which the read value is to be placed.</param>
        /// <param name="paramName">The string name of the parameter. Should be one of the EmissionModel.ParamNames entries.</param>
        /// <param name="parameters">The late-bound hashtable.</param>
        protected void TryToRead(ref Hashtable variable, string paramName, Hashtable parameters)
        {
            if (parameters.Contains(paramName))
            {
                variable = (Hashtable)parameters[paramName];
            }
            else
            {
                variable = null;
                _errMsgs.Add("Attempt to read missing parameter, \"" + paramName + "\" from supplied parameters.\r\n");
            }
        }

        /// <summary>
        /// Attempts to read a parameter by name from the late-bound parameters list, providing an error message if the parameter is missing.
        /// </summary>
        /// <param name="variable">The double into which the read value is to be placed.</param>
        /// <param name="paramName">The string name of the parameter. Should be one of the EmissionModel.ParamNames entries.</param>
        /// <param name="parameters">The late-bound hashtable.</param>
        protected void TryToRead(ref Mixture variable, string paramName, Hashtable parameters)
        {
            if (parameters.Contains(paramName))
            {
                variable = (Mixture)parameters[paramName];
            }
            else
            {
                variable = null;
                _errMsgs.Add("Attempt to read missing parameter, \"" + paramName + "\" from supplied parameters.\r\n");
            }
        }

        /// <summary>
        /// This is called after all parameter reads are done, in a late bound model execution. It forms and throws a
        /// MissingParameterException with an appropriate messasge if any of the parameter reads failed.
        /// </summary>
        protected void EvaluateSuccessOfParameterReads()
        {
            if (_errMsgs.Count == 0)
                return;

            string errMsg = "There was an error reading emissions parameters for emissions operation " + GetType().Name + ". The following issues were encountered:\r\n";
            foreach (string err in _errMsgs)
                errMsg += err;

            throw new Utility.MissingParameterException(errMsg);
        }

        /// <summary>
        /// Creates a string that reports the process call, logging it to Trace.
        /// </summary>
        /// <param name="subject">The subject.</param>
        /// <param name="initial">The initial.</param>
        /// <param name="final">The final.</param>
        /// <param name="emission">The emission.</param>
        /// <param name="parameters">The parameters.</param>
        protected void ReportProcessCall(EmissionModel subject, Mixture initial, Mixture final, Mixture emission, Hashtable parameters)
        {
            if (diagnostics)
            {
                string modelName = subject.Keys[0];
                string opStepName = (string)parameters["SomOpStepName"];
                parameters.Remove("SomOpStepName");
                _Debug.WriteLine("\r\n> > > > > > > >  " + opStepName + " [" + modelName + "]");
                _Debug.WriteLine("Initial : " + initial.Volume + " liters ( " + initial.Volume / K.LitersPerGallon + " Gallons ) , " + initial.Mass + " kg ( " + (initial.Mass / K.KgPerPound) + " lbm ).");
                foreach (Substance s in initial.Constituents)
                {
                    _Debug.WriteLine("\t\t" + s.MaterialType + " : " + s.Volume + " liters ( " + s.Volume / K.LitersPerGallon + " Gallons ) , " + s.Mass + " kg ( " + (s.Mass / K.KgPerPound) + " lbm ).");
                }
                _Debug.WriteLine("Final   : " + final.Volume + " liters ( " + final.Volume / K.LitersPerGallon + " Gallons ) , " + final.Mass + " kg ( " + (final.Mass / K.KgPerPound) + " lbm ).");
                foreach (Substance s in final.Constituents)
                {
                    _Debug.WriteLine("\t\t" + s.MaterialType + " : " + s.Volume + " liters ( " + s.Volume / K.LitersPerGallon + " Gallons ) , " + s.Mass + " kg ( " + (s.Mass / K.KgPerPound) + " lbm ).");
                }
                _Debug.WriteLine("Emission: " + emission.Volume + " liters ( " + emission.Volume / K.LitersPerGallon + " Gallons ) , " + emission.Mass + " kg ( " + (emission.Mass / K.KgPerPound) + " lbm ).");
                foreach (Substance s in emission.Constituents)
                {
                    _Debug.WriteLine("\t\t" + s.MaterialType + " : " + s.Volume + " liters ( " + s.Volume / K.LitersPerGallon + " Gallons ) , " + s.Mass + " kg ( " + (s.Mass / K.KgPerPound) + " lbm ).");
                }

                int longestKey = 0;
                foreach (DictionaryEntry de in parameters)
                    if (de.Key.ToString().Length > longestKey)
                        longestKey = de.Key.ToString().Length;

                foreach (DictionaryEntry de in parameters)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    string label = de.Key.ToString();
                    sb.Append(label);
                    for (int i = label.Length; i < longestKey + 3; i++)
                        sb.Append(" ");
                    sb.Append(": " + de.Value + " ( " + Convert(de) + " ) ");
                    _Debug.WriteLine(sb.ToString());
                }
            }
        }

        private string Convert(DictionaryEntry de)
        {
            double d;
            if (!double.TryParse(de.Value.ToString(), System.Globalization.NumberStyles.Any, null, out d))
            {
                //Console.WriteLine("Couldn't parse " + de.Value.ToString() + " ( key was " + de.Key + ".)" );
                return "";
            }
            switch (de.Key.ToString())
            {
                case "VacuumSystemPressure":
                    {
                        return "" + (d / K.PascalsPerMmHg) + " mmHg";
                    }
                case "InitialPressure":
                    {
                        return "" + (d / K.PascalsPerMmHg) + " mmHg";
                    }
                case "FinalPressure":
                    {
                        return "" + (d / K.PascalsPerMmHg) + " mmHg";
                    }
                case "SystemPressure":
                    {
                        return "" + (d / K.PascalsPerMmHg) + " mmHg";
                    }
                case "FillVolume":
                    {
                        return "" + (d / K.LitersPerGallon) + " Gallons";
                    }
                case "VesselVolume":
                    {
                        return "" + (d / (K.CubicMetersPerLiter * K.LitersPerGallon)) + " Gallons";
                    }
                case "CondenserTemperature":
                    {
                        return "" + (d + Chemistry.Constants.KELVIN_TO_CELSIUS) + " deg C";
                    }
                case "FinalTemperature":
                    {
                        return "" + (d + Chemistry.Constants.KELVIN_TO_CELSIUS) + " deg C";
                    }
                case "InitialTemperature":
                    {
                        return "" + (d + Chemistry.Constants.KELVIN_TO_CELSIUS) + " deg C";
                    }
                case "ControlTemperature":
                    {
                        return "" + (d + Chemistry.Constants.KELVIN_TO_CELSIUS) + " deg C";
                    }
                case "GasSweepDuration":
                    {
                        return "" + (d / 60.0) + " hours";
                    }
                case "AirLeakDuration":
                    {
                        return "" + (d / 60.0) + " hours";
                    }
                case "GasSweepRate":
                    {
                        return "" + (d * K.CubicFtPerCubicMeter) + " SCFM";
                    }
                case "AirLeakRate":
                    {
                        return "" + (d * (60.0 / K.KgPerPound)) + " lbm per hour";
                    }
                default:
                    {
                        return "";
                    }
            }
        }

        private static bool _permitOverEmission;
        private static bool _permitUnderEmission;

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
        public abstract void Process(
            Mixture initial,
            out Mixture final,
            out Mixture emission,
            bool modifyInPlace,
            Hashtable parameters);

        /// <summary>
        /// This is the list of names by which this emission model is specified, such as "Gas Sweep", "Vacuum Dry", etc.
        /// </summary>
        public abstract string[] Keys
        {
            get;
        }
        /// <summary>
        /// This is the list of parameters this model uses, and therefore expects as input.
        /// </summary>
        public abstract EmissionParam[] Parameters
        {
            get;
        }
        /// <summary>
        /// This is a description of what emissions mode this model computes (such as Air Dry, Gas Sweep, etc.)
        /// </summary>
        public abstract string ModelDescription
        {
            get;
        }

        /// <summary>
        /// Gets the system pressure from the parameters hashtable. It may be stored under the PN.SystemPressure_P or failing that, the PN.FinalPressure_P key.
        /// </summary>
        /// <param name="parameters">The parameter hashtable.</param>
        /// <returns></returns>
		protected double GetSystemPressure(Hashtable parameters)
        {
            double systemPressure = double.NaN;
            if (parameters.Contains(PN.SystemPressure_P))
            {
                systemPressure = (double)parameters[PN.SystemPressure_P];
            }
            else if (parameters.Contains(PN.FinalPressure_P))
            {
                //double initPressure = (double)parameters[PN.InitialPressure];
                double finalPressure = (double)parameters[PN.FinalPressure_P];
                systemPressure = finalPressure; //(finalPressure+initPressure)/2.0;
            }
            else
            {
                _errMsgs.Add("Attempt to read missing parameters, either \"" + PN.SystemPressure_P + "\" or \"" + PN.FinalPressure_P + "\" from supplied parameters.\r\n");
            }
            return systemPressure;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this emission model permits over emission - that is, emission of a quantity of material that is greater than what is present in the mixture, if the calculations dictate it.
        /// </summary>
        /// <value><c>true</c> if [permit over emission]; otherwise, <c>false</c>.</value>
		public bool PermitOverEmission
        {
            get
            {
                return _permitOverEmission;
            }
            set
            {
                _permitOverEmission = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this emission model permits under emission - that is, emission of a negative quantity, if the calculations dictate it.
        /// </summary>
        /// <value><c>true</c> if [permit under emission]; otherwise, <c>false</c>.</value>
        public bool PermitUnderEmission
        {
            get
            {
                return _permitUnderEmission;
            }
            set
            {
                _permitUnderEmission = value;
            }
        }


    }
}
