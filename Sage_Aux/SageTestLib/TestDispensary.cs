/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Materials.Chemistry;
using Highpoint.Sage.Randoms;
using Highpoint.Sage.SimCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;


namespace Highpoint.Sage.Materials
{

    /// <summary>
    /// Summary description for zTestDispensary.
    /// </summary>
    [TestClass]
    public class DispensaryTester
    {

        #region Private Fields
        private static readonly double AMBIENT_TEMPERATURE = 27.0;
        private IModel _model;
        Dispensary _dispensary;
        private MaterialType _mt1;
        private MaterialType _mt2;
        #endregion Private Fields

        [TestInitialize]
        public void Init()
        {
            _model = new Model();
            _dispensary = new Dispensary(_model.Executive);
            _mt1 = new MaterialType(_model, "Ethanol", Guid.NewGuid(), 1.5000, 3.2500, MaterialState.Liquid);
            _mt2 = new MaterialType(_model, "Cyclohexane", Guid.NewGuid(), 1.0000, 4.1800, MaterialState.Liquid);

        }
        [TestCleanup]
        public void destroy()
        {
            Debug.WriteLine("Done.");
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Simple set of puts and takes.")]
        public void TestBaseFunctionality()
        {
            _model.Executive.ExecutiveStarted_SingleShot += new ExecutiveEvent(Executive_ExecutiveStarted_SingleShot1);
            _model.Start();
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Simple set of puts and takes.")]
        public void TestRandomFunctionality()
        {
            _model.Executive.ExecutiveStarted_SingleShot += new ExecutiveEvent(Executive_ExecutiveStarted_SingleShot2);
            _model.Start();
        }

        void Executive_ExecutiveStarted_SingleShot1(IExecutive exec)
        {
            AddSomeMaterial(new DateTime(2008, 08, 01, 12, 00, 00), _mt1.CreateMass(10, AMBIENT_TEMPERATURE));
            RequestMaterial(new DateTime(2008, 08, 01, 13, 00, 00), 10);
            ConfirmTotlMass(new DateTime(2008, 08, 01, 14, 00, 00), 0);
            AddSomeMaterial(new DateTime(2008, 08, 01, 15, 00, 00), _mt1.CreateMass(10, AMBIENT_TEMPERATURE));
            RequestMaterial(new DateTime(2008, 08, 01, 16, 00, 00), 20);
            ConfirmTotlMass(new DateTime(2008, 08, 01, 17, 00, 00), 10);
            AddSomeMaterial(new DateTime(2008, 08, 01, 18, 00, 00), _mt1.CreateMass(8, AMBIENT_TEMPERATURE));
            AddSomeMaterial(new DateTime(2008, 08, 01, 19, 00, 00), _mt1.CreateMass(10, AMBIENT_TEMPERATURE));
            ConfirmTotlMass(new DateTime(2008, 08, 01, 20, 00, 00), 8);
            RequestMaterial(new DateTime(2008, 08, 01, 20, 00, 00), 10);
            RequestMaterial(new DateTime(2008, 08, 01, 20, 00, 00), 10);
            RequestMaterial(new DateTime(2008, 08, 01, 20, 00, 00), 10);
            RequestMaterial(new DateTime(2008, 08, 01, 20, 00, 00), 10);
            AddSomeMaterial(new DateTime(2008, 08, 01, 21, 00, 00), _mt1.CreateMass(33, AMBIENT_TEMPERATURE));
            ConfirmTotlMass(new DateTime(2008, 08, 01, 22, 00, 00), 1);
        }

        private double _howMuchPut;
        private double _howMuchRetrieved;
        void Executive_ExecutiveStarted_SingleShot2(IExecutive exec)
        {
            RandomServer r = new RandomServer(12345, 1000);
            Randoms.IRandomChannel rc = r.GetRandomChannel(98765, 1000);

            double howMuch = 0.0;
            DateTime when = new DateTime(2008, 08, 01, 12, 00, 00);
            for (int i = 0; i < 1000; i++)
            {
                int key = rc.Next(0, 2);
                int deltaT = rc.Next(0, 2);
                howMuch = rc.NextDouble() * 100.0;
                switch (key)
                {
                    case 0:
                        _howMuchPut += howMuch;
                        Console.WriteLine("{0} : Add {1} kg.", when, howMuch);
                        AddSomeMaterial(when, _mt1.CreateMass(howMuch, AMBIENT_TEMPERATURE));
                        break;
                    case 1:
                        _howMuchRetrieved += howMuch;
                        Console.WriteLine("{0} : Try to remove {1} kg.", when, howMuch);
                        RequestMaterial(when, howMuch);
                        break;
                    case 2:
                        Console.WriteLine("{0} : Confirm bookkeeping.", when);
                        ConfirmTotlMass(when);
                        break;
                    default:
                        break;
                }
                when += TimeSpan.FromMinutes(deltaT);
            }

            // Now, if there are outstanding requests, satisfy them.
            howMuch = _howMuchRetrieved - _howMuchPut;
            if (howMuch > 0)
            {
                Console.WriteLine("{0} : Add {1} kg.", when, howMuch);
                AddSomeMaterial(when, _mt1.CreateMass(howMuch, AMBIENT_TEMPERATURE));
            }

            _howMuchPut = 0.0;
            _howMuchRetrieved = 0.0;

            Console.WriteLine("Starting Test...");
        }

        private void ConfirmTotlMass(DateTime dateTime)
        {
            _model.Executive.RequestEvent(new ExecEventReceiver(delegate (IExecutive exec, object userData)
            {
                double expectedMass = _howMuchPut - _howMuchRetrieved;
                Console.WriteLine("{0} : Expect mass = {1} kg. in dispensary - now contains {2} kg.", exec.Now, expectedMass, _dispensary.PeekMixture.Mass);
                Assert.AreEqual(expectedMass, _dispensary.PeekMixture.Mass);
            }), dateTime, 0.0, null, ExecEventType.Detachable);
        }

        private void AddSomeMaterial(DateTime dateTime, IMaterial iMaterial)
        {
            _model.Executive.RequestEvent(new ExecEventReceiver(delegate (IExecutive exec, object userData)
            {
                _howMuchPut += iMaterial.Mass;
                _dispensary.Put(iMaterial);
                Console.WriteLine("{0} : Added {1} kg. to dispensary - now contains {2}.", exec.Now, iMaterial.ToString(), _dispensary.PeekMixture.ToString());
            }), dateTime, 0.0, null, ExecEventType.Detachable);
        }

        private void RequestMaterial(DateTime dateTime, double mass)
        {
            _model.Executive.RequestEvent(new ExecEventReceiver(delegate (IExecutive exec, object userData)
            {
                Console.WriteLine("{0} : Requested {1} kg. from dispensary - now contains {2}.", exec.Now, mass, _dispensary.PeekMixture.ToString());
                Mixture m = _dispensary.Get(mass);
                _howMuchRetrieved += mass;
                Console.WriteLine("{0} : Received {1} kg. from dispensary - now contains {2}.", exec.Now, mass, _dispensary.PeekMixture.ToString());
            }), dateTime, 0.0, null, ExecEventType.Detachable);
        }

        private void ConfirmTotlMass(DateTime dateTime, double expectedMass)
        {
            _model.Executive.RequestEvent(new ExecEventReceiver(delegate (IExecutive exec, object userData)
            {
                Console.WriteLine("{0} : Expect mass = {1} kg. in dispensary - now contains {2} kg.", exec.Now, expectedMass, _dispensary.PeekMixture.Mass);
                Assert.AreEqual(expectedMass, _dispensary.PeekMixture.Mass);
            }), dateTime, 0.0, null, ExecEventType.Detachable);
        }
    }
}