/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.Materials.Chemistry;
using Highpoint.Sage.Materials.Chemistry.VaporPressure;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Highpoint.Sage.Materials
{

    [TestClass]
    public class MaterialServiceTester
    {

        public MaterialServiceTester()
        {
            Init();
        }

        [TestInitialize]
        public void Init()
        {
        }
        [TestCleanup]
        public void destroy()
        {
            Debug.WriteLine("Done.");
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test to remove material from a mixture")]
        public void TestChargeWithSufficientSupply()
        {
            Model model = new Model("Test Model", Guid.NewGuid());

            MaterialCatalog mcat = LoadSampleCatalog();

            MaterialService source = new MaterialService(model, "Sorc", Guid.NewGuid(), 3, 100, mcat, 37.0);
            source.AddCompartment(model, mcat["Ammonia"].Guid, 1500, 17, 2000, Guid.NewGuid());

            MaterialService sink = new MaterialService(model, "Sink", Guid.NewGuid(), 3, 100, mcat, 37.0);
            sink.AutocreateMaterialCompartments = true;

            TestClient tc = new TestClient(model, source, sink, mcat["Ammonia"], 1000, mcat["Ammonia"], 750);

            model.Executive.RequestEvent(
                new ExecEventReceiver(tc.Run),
                new DateTime(2009, 2, 16, 12, 59, 28), 0.0, new Hashtable(), ExecEventType.Detachable);

            model.Start();
        }

        #region Private Support Goo

        /// <summary>
        /// Loads the material catalog in the model with sample materials.
        /// </summary>
        /// <param name="mcat">the Material Catalog to be initialized.</param>
        private static MaterialCatalog LoadSampleCatalog()
        {
            MaterialCatalog mcat = new MaterialCatalog();
            //Console.WriteLine("Warning: Specification is creating a MaterialType without propagating its molecular weight, emissions classifications or VaporPressure constants.");
            mcat.Add(new MaterialType(null, "Ethyl Acetate", Guid.NewGuid(), 0.9020, 1.9230, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Hydrochloric Acid", Guid.NewGuid(), 1.1890, 2.5500, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Ethanol", Guid.NewGuid(), 0.8110, 3.0000, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Hexane", Guid.NewGuid(), 0.6700, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Triethylamine", Guid.NewGuid(), 0.7290, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "MTBE", Guid.NewGuid(), 0.7400, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Isopropyl Alcohol", Guid.NewGuid(), 0.7860, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Acetone", Guid.NewGuid(), 0.7899, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Propyl Alcohol", Guid.NewGuid(), 0.8035, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Butanol", Guid.NewGuid(), 0.8100, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Aluminum Hydroxide", Guid.NewGuid(), 1.0000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Ammonia", Guid.NewGuid(), 1.0000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Ammonium Hydroxide", Guid.NewGuid(), 1.0000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Carbon Dioxide", Guid.NewGuid(), 1.0000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Manganese Sulfate", Guid.NewGuid(), 1.0000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Nitrous Acid", Guid.NewGuid(), 1.0000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Potassium Phosphate", Guid.NewGuid(), 1.0000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Potassium Sulfate", Guid.NewGuid(), 1.0000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Sodium Sulfate", Guid.NewGuid(), 1.0000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Manganese Dioxide", Guid.NewGuid(), 1.3000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Potassium Hydroxide", Guid.NewGuid(), 1.3000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Sodium Bromide", Guid.NewGuid(), 1.3000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Sodium Bisulfite", Guid.NewGuid(), 1.4800, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Titanium Dioxide", Guid.NewGuid(), 1.5000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Titanium Tetrachloride", Guid.NewGuid(), 1.5000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Sodium Nitrate", Guid.NewGuid(), 1.6000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Phosphoric Acid", Guid.NewGuid(), 1.6850, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Sodium Sulfide", Guid.NewGuid(), 1.8580, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Caustic Soda", Guid.NewGuid(), 2.0000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Sodium Chloride", Guid.NewGuid(), 2.1650, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Sodium Bicarbonate", Guid.NewGuid(), 2.2000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Sodium Nitrite", Guid.NewGuid(), 2.3800, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Potassium Nitrite", Guid.NewGuid(), 1.9150, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Sodium Carbonate", Guid.NewGuid(), 2.5330, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Pot. Permanganate", Guid.NewGuid(), 2.7000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Bromine", Guid.NewGuid(), 3.1200, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Sulfuric Acid", Guid.NewGuid(), 1.8420, 4.1800, MaterialState.Liquid));

            mcat.Add(new MaterialType(null, "Methanol", Guid.NewGuid(), 0.7920, 4.1800, MaterialState.Liquid, 32));
            mcat.Add(new MaterialType(null, "Methylene Chloride", Guid.NewGuid(), 2.15, 4.1800, MaterialState.Liquid, 85));
            mcat.Add(new MaterialType(null, "Water", Guid.NewGuid(), 1.0000, 4.1800, MaterialState.Liquid));

            mcat["Methanol"].SetAntoinesCoefficients3(7.879, 1473.1, 230, PressureUnits.mmHg, TemperatureUnits.Celsius);
            mcat["Methylene Chloride"].SetAntoinesCoefficients3(7.263, 1222.7, 238.4, PressureUnits.mmHg, TemperatureUnits.Celsius);
            mcat["Water"].SetAntoinesCoefficients3(8.040, 1715.1, 232.4, PressureUnits.mmHg, TemperatureUnits.Celsius);

            Debug.WriteLine(" ... sample substances loaded.");
            return mcat;
        }

        class TestClient : IPortOwner, IHasIdentity
        {

            public IInputPort Input;
            public IOutputPort Output;

            #region Private Fields
            private readonly PortSet _portSet;
            private readonly SimpleInputPort _input;
            private readonly IOutputPort _output;
            private readonly IModel _model;
            private readonly MaterialService _source;

            private readonly MaterialType _getThis;
            private readonly double _getHowMuch;

            private readonly MaterialType _sendThat;
            private readonly double _sendHowMuch;
            private readonly MaterialService _sink;
            private Guid _guid = Guid.NewGuid();

            private readonly Mixture _mixture;
            #endregion Private Fields

            public TestClient(IModel model, MaterialService source, MaterialService sink, MaterialType getThis, double getHowMuch, MaterialType sendThat, double sendHowMuch)
            {

                _portSet = new PortSet();

                _input = new SimpleInputPort(source.Model, "In", Guid.NewGuid(), this, null);
                Input = _input;
                _source = source;
                _getThis = getThis;
                _getHowMuch = getHowMuch;

                _output = new SimpleOutputPort(source.Model, "Out", Guid.NewGuid(), this, null, null);
                Output = _output;
                _sink = sink;
                _sendThat = sendThat;
                _sendHowMuch = sendHowMuch;

                _model = model;
                _mixture = new Mixture(model, "Test client's mixture");

            }

            public void Run(IExecutive exec, object userData)
            {

                Dump("At start, ", exec);
                IDictionary graphContext = (IDictionary)userData;

                object transferInKey = _source.Setup(_getThis.CreateMass(_getHowMuch, 17.0), 25.0, Input, true);
                object transferOutKey = _sink.Setup(_sendThat.CreateMass(_sendHowMuch, 17.0), 40.0, Output, true);

                Dump("After setup, ", exec);

                _source.Execute(graphContext, transferInKey);
                MaterialTransfer mtSrc = (MaterialTransfer)_source.GetTransferTable(graphContext)[Input.Connector];
                foreach (IMaterial imat in mtSrc.Mixture.Constituents)
                {
                    _mixture.AddMaterial(imat);
                }

                Dump("After source executes, ", exec);

                Mixture export = (Mixture)_mixture.RemoveMaterial(_sendHowMuch);
                MaterialTransfer mtSnk = new MaterialTransfer(export, TimeSpan.FromMinutes(_sendHowMuch / 40.0));
                _sink.GetTransferTable(graphContext).Add(Output.Connector, mtSnk);
                _sink.Execute(graphContext, transferOutKey);

                Dump("After sink executes, ", exec);

                exec.CurrentEventController.SuspendFor(
                    TimeSpanOperations.Max(
                    TimeSpanOperations.Max(mtSrc.SourceDuration, mtSrc.DestinationDuration),
                    TimeSpanOperations.Max(mtSnk.SourceDuration, mtSnk.DestinationDuration)
                    ));

                _source.Teardown(transferInKey, true);
                _sink.Teardown(transferOutKey, true);

                Dump("After teardown, ", exec);

            }

            private void Dump(string context, IExecutive exec)
            {
                Console.WriteLine("{4}{0}\r\n\tSource:\r\n\t{1}\r\n\tClient has {2}\r\n\tSink:\r\n\t{3}",
                    exec.Now,
                    Dump(_source),
                    _mixture.ToString(),
                    Dump(_sink),
                    context);
            }

            private string Dump(MaterialService matlSvc)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append(matlSvc.Name);
                List<string> compartments = new List<string>();
                foreach (MaterialResourceItem mri in matlSvc.Compartments)
                {
                    compartments.Add(string.Format("{0} kg of {1}", mri.Available, mri.MaterialType.Name));
                }
                sb.Append(string.Format(" has {0} compartment{1} with {2}. ",
                    compartments.Count, (compartments.Count == 1 ? "" : "s"),
                    StringOperations.ToCommasAndAndedList(compartments)));

                foreach (IPort port in matlSvc.Ports)
                {
                    sb.Append(string.Format(" Port {0} is connected to {1}. ", port.Name, (port.Peer == null ? "<null>" : ((IHasName)port.Peer.Owner).Name)));
                }
                return sb.ToString();
            }

            #region IPortOwner Members
            public IPortSet Ports
            {
                get
                {
                    return _portSet;
                }
            }

            public void AddPort(IPort port)
            {
                _portSet.AddPort(port);
            }

            /// <summary>
            /// Adds a port to this object's port set in the specified role or channel.
            /// </summary>
            /// <param name="channel">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
            /// <returns>The newly-created port. Can return null if this is not supported.</returns>
            public IPort AddPort(string channel)
            {
                return null; /*Implement AddPort(string channel); */
            }

            /// <summary>
            /// Adds a port to this object's port set in the specified role or channel.
            /// </summary>
            /// <param name="channelTypeName">The channel - usually "Input" or "Output", sometimes "Control", "Kanban", etc.</param>
            /// <param name="guid">The GUID to be assigned to the new port.</param>
            /// <returns>The newly-created port. Can return null if this is not supported.</returns>
            public IPort AddPort(string channelTypeName, Guid guid)
            {
                return null; /*Implement AddPort(string channel); */
            }

            /// <summary>
            /// Gets the names of supported port channels.
            /// </summary>
            /// <value>The supported channels.</value>
            public List<Highpoint.Sage.ItemBased.Ports.IPortChannelInfo> SupportedChannelInfo
            {
                get
                {
                    return GeneralPortChannelInfo.StdInputAndOutput;
                }
            }

            public void RemovePort(IPort port)
            {
                _portSet.RemovePort(port);
            }

            public void ClearPorts()
            {
                _portSet.ClearPorts();
            }

            #endregion

            #region IHasIdentity Members

            public Guid Guid
            {
                get
                {
                    return _guid;
                }
            }

            public string Name
            {
                get
                {
                    return "TestClient";
                }
            }
            private readonly string _description = null;
            /// <summary>
            /// A description of this Test Client.
            /// </summary>
            public string Description
            {
                get
                {
                    return _description ?? Name;
                }
            }

            #endregion
        }

        #endregion
    }
}
