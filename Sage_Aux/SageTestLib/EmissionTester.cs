/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using K = Highpoint.Sage.Materials.Chemistry.Emissions.Tester.Constants;
using PN = Highpoint.Sage.Materials.Chemistry.Emissions.EmissionModel.ParamNames;


namespace Highpoint.Sage.Materials.Chemistry.Emissions
{

    public class Tester
    {
        #region Private Fields
        private bool _lateBound;
        private double _initialMixtureTemperature;
        private BasicReactionSupporter _brs;
        private Mixture _initialMixture;
        private Mixture _currentMixture;
        private Mixture _lastEmission;
        private Mixture _aggregateEmissions;
        private double _condensorOrControlTemp;
        private double _tempOfTankLiquidInitial;
        private double _tempOfTankLiquidFinal;
        private double _fillVolume;
        private double _freeSpaceVolume;
        private double _initialVesselPressure;
        private double _finalVesselPressure;
        private double _batchCycleTimeForSweep;
        private double _gasSweepRateInSCFM;
        private double _numberOfMolesOfGasEvolved;
        private double _leakRateOfAirIntoSystem;
        private double _batchCycleTimeForVacuumOperation;
        private double _vacuumSystemPressureIn;
        #endregion

        public class Constants : Highpoint.Sage.Materials.Chemistry.Constants
        {
            public static double kgPerPound = 0.453592;
            public static double pascalsPer_mmHg = 133.322;
            public static double pascalsPerAtmosphere = 101325.0;
            public static double cubicFtPerGallon = 0.134;
            public static double litersPerGallon = 3.7854118;
            public static double cubicFtPerCubicMeter = 35.314667;
        }

        public Tester(BasicReactionSupporter brs, bool lateBound)
        {
            _brs = brs;
            _lateBound = lateBound;
            Reset();
        }


        public void Reset()
        {
            _initialMixture = new Mixture("Test mixture");
            _aggregateEmissions = new Mixture("Aggregate emissions");
            // Defaults from WebEmit.
            SetParams(35.0, 40.0, 40.0, 35.0, 200000, 581, 790, 760, 1.0, 0.1, 5.0, 1.0, 1.0, 500);
        }


        public void SetParams(
            double condensorOrControlTemp,
            double initialMixtureTemperature,
            double tempOfTankLiquidInitial,
            double tempOfTankLiquidFinal,
            double fillVolume,
            double freeSpaceVolume,
            double initialVesselPressure,
            double finalVesselPressure,
            double batchCycleTimeForSweep,
            double gasSweepRateInSCFM,
            double numberOfMolesOfGasEvolved,
            double leakRateOfAirIntoSystem,
            double batchCycleTimeForVacuumOperation,
            double vacuumSystemPressureIn)
        {
            _condensorOrControlTemp = condensorOrControlTemp;
            _initialMixtureTemperature = initialMixtureTemperature;
            _tempOfTankLiquidInitial = tempOfTankLiquidInitial;
            _tempOfTankLiquidFinal = tempOfTankLiquidFinal;
            _fillVolume = fillVolume;
            _freeSpaceVolume = freeSpaceVolume;
            _initialVesselPressure = initialVesselPressure;
            _finalVesselPressure = initialVesselPressure;
            _batchCycleTimeForSweep = batchCycleTimeForSweep;
            _gasSweepRateInSCFM = gasSweepRateInSCFM;
            _numberOfMolesOfGasEvolved = numberOfMolesOfGasEvolved;
            _leakRateOfAirIntoSystem = leakRateOfAirIntoSystem;
            _batchCycleTimeForVacuumOperation = batchCycleTimeForVacuumOperation;
            _vacuumSystemPressureIn = vacuumSystemPressureIn;

        }


        public void AddGallons(string name, double numGallons)
        {
            double liters = K.litersPerGallon * numGallons;
            MaterialType mt = _brs.MyMaterialCatalog[name];
            double kg = liters * mt.SpecificGravity;

            _initialMixture.AddMaterial(mt.CreateMass(kg, _tempOfTankLiquidInitial));
            _initialMixture.Temperature = _initialMixtureTemperature;
            _currentMixture = (Mixture)_initialMixture.Clone();
        }

        public void DoAirDry(Hashtable materialGuidToVolumeFraction, double massOfDriedProductCake, double controlTemperature)
        {
            Console.WriteLine("Air Dry Testing" + (_lateBound ? ", late bound." : ", early bound."));
            Console.WriteLine("Mixture is      : " + _currentMixture.ToString());
            if (_lateBound)
            {
                Hashtable paramTable = new Hashtable();
                string modelKey = "Air Dry";
                paramTable.Add(PN.MaterialGuidToVolumeFraction, materialGuidToVolumeFraction);
                paramTable.Add(PN.MassOfDriedProductCake_Kg, massOfDriedProductCake);
                paramTable.Add(PN.ControlTemperature_K, controlTemperature);
                EmissionsService.Instance.ProcessEmissions(_currentMixture, out _currentMixture, out _lastEmission, true, modelKey, paramTable);
            }
            else
            {
                new Highpoint.Sage.Materials.Chemistry.Emissions.AirDryModel().AirDry(_currentMixture, out _currentMixture, out _lastEmission, true, massOfDriedProductCake, controlTemperature, materialGuidToVolumeFraction);
            }
            Console.WriteLine("Mixture becomes : " + _currentMixture.ToString());
            Console.WriteLine("Emissions are   : " + _lastEmission.ToString("F1", "F4"));

            _aggregateEmissions.AddMaterial(_lastEmission);
        }


        public void DoFill(Mixture materialToAdd, double controlTemp)
        {
            Console.WriteLine("Fill Testing" + (_lateBound ? ", late bound." : ", early bound."));
            Console.WriteLine("Mixture is      : " + _currentMixture.ToString());
            if (_lateBound)
            {
                Hashtable paramTable = new Hashtable();
                paramTable.Add(PN.MaterialToAdd, materialToAdd);
                paramTable.Add(PN.ControlTemperature_K, controlTemp);
                string modelKey = "Fill";
                EmissionsService.Instance.ProcessEmissions(_currentMixture, out _currentMixture, out _lastEmission, true, modelKey, paramTable);
            }
            else
            {
                new FillModel().Fill(_currentMixture, out _currentMixture, out _lastEmission, true, materialToAdd, controlTemp);
            }
            Console.WriteLine("Mixture becomes : " + _currentMixture.ToString());
            Console.WriteLine("Emissions are   : " + _lastEmission.ToString("F2", "F8"));

            _aggregateEmissions.AddMaterial(_lastEmission);
        }


        public void DoEvacuation(double initialVesselPressure, double finalVesselPressure, double controlTemperature, double vesselVolume)
        {
            _initialVesselPressure = initialVesselPressure;
            _finalVesselPressure = finalVesselPressure;

            Console.WriteLine("Evacuation Testing" + (_lateBound ? ", late bound." : ", early bound."));
            Console.WriteLine("Mixture is      : " + _currentMixture.ToString());
            if (_lateBound)
            {
                Hashtable paramTable = new Hashtable();
                paramTable.Add(PN.InitialPressure_P, initialVesselPressure);
                paramTable.Add(PN.FinalPressure_P, finalVesselPressure);
                paramTable.Add(PN.ControlTemperature_K, controlTemperature);
                paramTable.Add(PN.VesselVolume_M3, vesselVolume);
                string modelKey = "Evacuate";
                EmissionsService.Instance.ProcessEmissions(_currentMixture, out _currentMixture, out _lastEmission, true, modelKey, paramTable);
            }
            else
            {
                new EvacuateModel().Evacuate(_currentMixture, out _currentMixture, out _lastEmission, true, initialVesselPressure, finalVesselPressure, controlTemperature, vesselVolume);
            }
            Console.WriteLine("Mixture becomes : " + _currentMixture.ToString());
            Console.WriteLine("Emissions are   : " + _lastEmission.ToString("F1", "F4"));

            _aggregateEmissions.AddMaterial(_lastEmission);
        }


        public void DoGasEvolution(double nMolesEvolved, double controlTemperature, double systemPressure)
        {
            Console.WriteLine("Gas Evolution Testing" + (_lateBound ? ", late bound." : ", early bound."));
            Console.WriteLine("Mixture is      : " + _currentMixture.ToString());
            if (_lateBound)
            {
                Hashtable paramTable = new Hashtable();
                paramTable.Add(PN.MolesOfGasEvolved, nMolesEvolved);
                paramTable.Add(PN.ControlTemperature_K, controlTemperature);
                paramTable.Add(PN.SystemPressure_P, systemPressure);
                string modelKey = "Gas Evolution";
                EmissionsService.Instance.ProcessEmissions(_currentMixture, out _currentMixture, out _lastEmission, true, modelKey, paramTable);
            }
            else
            {
                new GasEvolutionModel().GasEvolution(_currentMixture, out _currentMixture, out _lastEmission, true, nMolesEvolved, controlTemperature, systemPressure);
            }
            Console.WriteLine("Mixture becomes : " + _currentMixture.ToString());
            Console.WriteLine("Emissions are   : " + _lastEmission.ToString("F1", "F4"));

            _aggregateEmissions.AddMaterial(_lastEmission);
        }


        public void DoGasSweep(double controlTemperature, double systemPressure, double sweepRate, double sweepDuration)
        {
            Console.WriteLine("Gas Sweep Testing" + (_lateBound ? ", late bound." : ", early bound."));
            Console.WriteLine("Mixture is      : " + _currentMixture.ToString());
            if (_lateBound)
            {
                Hashtable paramTable = new Hashtable();
                paramTable.Add(PN.ControlTemperature_K, controlTemperature);
                paramTable.Add(PN.SystemPressure_P, systemPressure);
                paramTable.Add(PN.GasSweepRate_M3PerMin, sweepRate);
                paramTable.Add(PN.GasSweepDuration_Min, sweepDuration);
                string modelKey = "Gas Sweep";
                EmissionsService.Instance.ProcessEmissions(_currentMixture, out _currentMixture, out _lastEmission, true, modelKey, paramTable);
            }
            else
            {
                new GasSweepModel().GasSweep(_currentMixture, out _currentMixture, out _lastEmission, true, sweepRate, sweepDuration, controlTemperature, systemPressure);
            }
            Console.WriteLine("Mixture becomes : " + _currentMixture.ToString());
            Console.WriteLine("Emissions are   : " + _lastEmission.ToString("F1", "F4"));

            _aggregateEmissions.AddMaterial(_lastEmission);
        }


        public void DoHeat(double controlTemperature, double initialTemperature, double finalTemperature, double systemPressure, double vesselVolume)
        {
            Console.WriteLine("Heat Testing" + (_lateBound ? ", late bound." : ", early bound."));
            Console.WriteLine("Mixture is      : " + _currentMixture.ToString());
            if (_lateBound)
            {
                Hashtable paramTable = new Hashtable();
                paramTable.Add(PN.ControlTemperature_K, controlTemperature);
                paramTable.Add(PN.InitialTemperature_K, initialTemperature);
                paramTable.Add(PN.FinalTemperature_K, finalTemperature);
                paramTable.Add(PN.SystemPressure_P, systemPressure);
                paramTable.Add(PN.VesselVolume_M3, vesselVolume);
                string modelKey = "Heat";
                EmissionsService.Instance.ProcessEmissions(_currentMixture, out _currentMixture, out _lastEmission, true, modelKey, paramTable);
            }
            else
            {
                new HeatModel().Heat(_currentMixture, out _currentMixture, out _lastEmission, true, controlTemperature, initialTemperature, finalTemperature, systemPressure, vesselVolume);
            }
            Console.WriteLine("Mixture becomes : " + _currentMixture.ToString());
            Console.WriteLine("Emissions are   : " + _lastEmission.ToString("F1", "F4"));

            _aggregateEmissions.AddMaterial(_lastEmission);
        }


        public void DoMassBalance(Mixture desiredEmission)
        {
            Console.WriteLine("Mass Balance Testing" + (_lateBound ? ", late bound." : ", early bound."));
            Console.WriteLine("Mixture is      : " + _currentMixture.ToString());
            if (_lateBound)
            {
                Hashtable paramTable = new Hashtable();
                paramTable.Add(PN.DesiredEmission, desiredEmission);
                string modelKey = "Mass Balance";
                EmissionsService.Instance.ProcessEmissions(_currentMixture, out _currentMixture, out _lastEmission, true, modelKey, paramTable);
            }
            else
            {
                new MassBalanceModel().MassBalance(_currentMixture, out _currentMixture, out _lastEmission, true, desiredEmission);
            }
            Console.WriteLine("Mixture becomes : " + _currentMixture.ToString());
            Console.WriteLine("Emissions are   : " + _lastEmission.ToString("F1", "F4"));

            _aggregateEmissions.AddMaterial(_lastEmission);
        }


        public void DoNoEmissions()
        {
            Console.WriteLine("No Emissions Testing" + (_lateBound ? ", late bound." : ", early bound."));
            Console.WriteLine("Mixture is      : " + _currentMixture.ToString());
            if (_lateBound)
            {
                Hashtable paramTable = new Hashtable();
                string modelKey = "No Emissions";
                EmissionsService.Instance.ProcessEmissions(_currentMixture, out _currentMixture, out _lastEmission, true, modelKey, paramTable);
            }
            else
            {
                new NoEmissionModel().NoEmission(_currentMixture, out _currentMixture, out _lastEmission, true);
            }
            Console.WriteLine("Mixture becomes : " + _currentMixture.ToString());
            Console.WriteLine("Emissions are   : " + _lastEmission.ToString("F1", "F4"));

            _aggregateEmissions.AddMaterial(_lastEmission);
        }


        public void DoVacuumDistillation(double controlTemperature, double systemPressure, double airLeakRate, double airLeakDuration)
        {
            Console.WriteLine("Vacuum Distillation Testing" + (_lateBound ? ", late bound." : ", early bound."));
            Console.WriteLine("Mixture is      : " + _currentMixture.ToString());
            if (_lateBound)
            {
                Hashtable paramTable = new Hashtable();
                paramTable.Add(PN.ControlTemperature_K, controlTemperature);
                paramTable.Add(PN.VacuumSystemPressure_P, systemPressure);
                paramTable.Add(PN.AirLeakRate_KgPerMin, airLeakRate);
                paramTable.Add(PN.AirLeakDuration_Min, airLeakDuration);
                string modelKey = "Vacuum Distillation";
                EmissionsService.Instance.ProcessEmissions(_currentMixture, out _currentMixture, out _lastEmission, true, modelKey, paramTable);
            }
            else
            {
                new VacuumDistillationModel().VacuumDistillation(_currentMixture, out _currentMixture, out _lastEmission, true, controlTemperature, systemPressure, airLeakRate, airLeakDuration);
            }
            Console.WriteLine("Mixture becomes : " + _currentMixture.ToString());
            Console.WriteLine("Emissions are   : " + _lastEmission.ToString("F1", "F4"));

            _aggregateEmissions.AddMaterial(_lastEmission);
        }


        public void DoVacuumDistillationWScrubber(double controlTemperature, double systemPressure, double airLeakRate, double airLeakDuration)
        {
            Console.WriteLine("Vacuum Distillation w/ Scrubber Testing" + (_lateBound ? ", late bound." : ", early bound."));
            Console.WriteLine("Mixture is      : " + _currentMixture.ToString());
            if (_lateBound)
            {
                Hashtable paramTable = new Hashtable();
                paramTable.Add(PN.ControlTemperature_K, controlTemperature);
                paramTable.Add(PN.SystemPressure_P, systemPressure);
                paramTable.Add(PN.AirLeakRate_KgPerMin, airLeakRate);
                paramTable.Add(PN.AirLeakDuration_Min, airLeakDuration);
                string modelKey = "Vacuum Distillation w/ Scrubber";
                EmissionsService.Instance.ProcessEmissions(_currentMixture, out _currentMixture, out _lastEmission, true, modelKey, paramTable);
            }
            else
            {
                new VacuumDistillationWScrubberModel().VacuumDistillationWScrubber(_currentMixture, out _currentMixture, out _lastEmission, true, controlTemperature, systemPressure, airLeakRate, airLeakDuration);
            }
            Console.WriteLine("Mixture becomes : " + _currentMixture.ToString());
            Console.WriteLine("Emissions are   : " + _lastEmission.ToString("F1", "F4"));

            _aggregateEmissions.AddMaterial(_lastEmission);
        }


        public void DoVacuumDry(double controlTemperature, double systemPressure, double airLeakRate, double airLeakDuration, Hashtable materialGuidToVolumeFraction, double massOfDriedProductCake)
        {
            Console.WriteLine("Vacuum Dry Testing" + (_lateBound ? ", late bound." : ", early bound."));
            Console.WriteLine("Mixture is      : " + _currentMixture.ToString());
            if (_lateBound)
            {
                Hashtable paramTable = new Hashtable();
                paramTable.Add(PN.ControlTemperature_K, controlTemperature);
                paramTable.Add(PN.SystemPressure_P, systemPressure);
                paramTable.Add(PN.AirLeakRate_KgPerMin, airLeakRate);
                paramTable.Add(PN.AirLeakDuration_Min, airLeakDuration);
                paramTable.Add(PN.MaterialGuidToVolumeFraction, materialGuidToVolumeFraction);
                paramTable.Add(PN.MassOfDriedProductCake_Kg, massOfDriedProductCake);
                string modelKey = "Vacuum Dry";
                EmissionsService.Instance.ProcessEmissions(_currentMixture, out _currentMixture, out _lastEmission, true, modelKey, paramTable);
            }
            else
            {
                new VacuumDryModel().VacuumDry(_currentMixture, out _currentMixture, out _lastEmission, true, controlTemperature, systemPressure, airLeakRate, airLeakDuration, materialGuidToVolumeFraction, massOfDriedProductCake);
            }
            Console.WriteLine("Mixture becomes : " + _currentMixture.ToString());
            Console.WriteLine("Emissions are   : " + _lastEmission.ToString("F1", "F4"));

            _aggregateEmissions.AddMaterial(_lastEmission);
        }


        public void DoPressureTransfer(Mixture materialToAdd, double controlTemperature)
        {
            Console.WriteLine("Pressure Transfer Testing" + (_lateBound ? ", late bound." : ", early bound."));
            Console.WriteLine("Mixture is      : " + _currentMixture.ToString());
            if (_lateBound)
            {
                Hashtable paramTable = new Hashtable();
                paramTable.Add(PN.ControlTemperature_K, controlTemperature);
                paramTable.Add(PN.MaterialToAdd, materialToAdd);
                string modelKey = "Pressure Transfer";
                EmissionsService.Instance.ProcessEmissions(_currentMixture, out _currentMixture, out _lastEmission, true, modelKey, paramTable);
            }
            else
            {
                new PressureTransferModel().PressureTransfer(_currentMixture, out _currentMixture, out _lastEmission, true, materialToAdd, controlTemperature);
            }
            Console.WriteLine("Mixture becomes : " + _currentMixture.ToString());
            Console.WriteLine("Emissions are   : " + _lastEmission.ToString());

            _aggregateEmissions.AddMaterial(_lastEmission);
        }


        public void SetInitialTemperature(double temperature)
        {
            _initialMixtureTemperature = temperature;
            _initialMixture.Temperature = temperature;
        }
        public Mixture InitialMixture
        {
            get
            {
                return _initialMixture;
            }
        }
        public Mixture CurrentMixture
        {
            get
            {
                return _currentMixture;
            }
        }
        public Mixture LastEmission
        {
            get
            {
                return _lastEmission;
            }
        }
        public Mixture AggregateEmissions
        {
            get
            {
                return _aggregateEmissions;
            }
        }
    }
}