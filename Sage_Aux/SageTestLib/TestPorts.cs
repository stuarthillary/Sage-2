/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.ItemBased.Connectors;
using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.SimCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Highpoint.Sage.ItemBased.Blocks
{


    [TestClass]
    public class PortsAndConnectorsTester
    {
        [TestMethod]
        public void TestPortBasics()
        {
            new PortTester().TestSimplePortNetwork();
        }

    }

    [TestClass]
    public class PortTester
    {
        private readonly Model _model;
        readonly int _nBlocks = 10;
        readonly SimplePassThroughPortOwner[] _blocks;
        //SimplePassThroughPortOwner m_finish;
        public PortTester()
        {
            _model = new Model();
            _blocks = new SimplePassThroughPortOwner[_nBlocks];
        }

        [TestMethod]
        public void TestSimplePortNetwork()
        {

            for (int i = 0; i < _nBlocks; i++)
            {
                _blocks[i] = new SimplePassThroughPortOwner(_model, "Block" + i, Guid.NewGuid());
                if (i > 0)
                    ConnectorFactory.Connect(_blocks[i - 1].Out, _blocks[i].In);
            }

            _blocks[0].In.Put("Random string");
            Debug.WriteLine(_blocks[_nBlocks - 1].Out.Peek(null));


            Debug.WriteLine(_blocks[_nBlocks - 1].Out.Take(null));
        }

        [TestMethod]
        public void TestProxyPorts()
        {

            SimpleProxyPortOwner sppo = new SimpleProxyPortOwner(_model, "Proxy", Guid.NewGuid());
            sppo.In.Put("Random string");
            Debug.WriteLine(sppo.Out.Peek(null));

            Debug.WriteLine(sppo.Out.Take(null));
        }
    }

    [TestClass]
    public class ManagementFacadeTester
    {

        public void TestManagementBasics()
        {

            //new ManagementFacadeTester().DoAbbreviatedFacadeTest();
            //new ManagementFacadeTester().DoAbbreviatedSimplePushTest();
            //new ManagementFacadeTester().DoAbbreviatedSimplePullTest();
            //new ManagementFacadeTester().DoAbbreviatedInputSideBufferedPullTest();
            //new ManagementFacadeTester().DoAbbreviatedOutputSideBufferedPullTest();
            //new ManagementFacadeTester().DoAbbreviatedSimplePushPullTestWithBuffering();
            //new ManagementFacadeTester().DoAbbreviatedSimplePushPullTestNoOutputBuffering();
            //new ManagementFacadeTester().OneActiveOnePassiveInputDeterminesOneBufferedPassiveOutputTest();
            //new ManagementFacadeTester().DoAbbreviatedForcedPullTest();
            new ManagementFacadeTester().TestOneInOneOutPushPullTransform();
            //new ManagementFacadeTester().TestOnePullValue();
        }

        private IInputPort _in0, _in1;
        private IOutputPort _out0, _out1;
        private SimpleOutputPort _entryPoint0, _entryPoint1;
        private InputPortManager _facadeIn0, _facadeIn1;
        private OutputPortManager _facadeOut0, _facadeOut1;

        [TestMethod]
        public void DoAbbreviatedFacadeTest()
        {

            PortManagementFacade pmf = Setup(out _in0, out _in1, out _out0, out _out1, out _facadeIn0, out _facadeIn1, out _facadeOut0, out _facadeOut1, out _entryPoint0, out _entryPoint1);

            _facadeOut0.ComputeFunction = new Action(() => { _facadeOut0.Buffer = _facadeIn0.Value.ToString() + " " + _facadeIn1.Value.ToString(); });
            _facadeOut1.ComputeFunction = new Action(() => { _facadeOut1.Buffer = _facadeIn1.Value.ToString() + " " + _facadeIn0.Value.ToString(); });

            Console.WriteLine(_out0.Take(null) + " taken.");
            Console.WriteLine(_out0.Take(null) + " taken.");
            Console.WriteLine(_out1.Take(null) + " taken.");
            Console.WriteLine(_out1.Take(null) + " taken.");

        }

        [TestMethod]
        public void DoAbbreviatedSimplePushTest()
        {

            PortManagementFacade pmf = Setup(out _in0, out _in1, out _out0, out _out1, out _facadeIn0, out _facadeIn1, out _facadeOut0, out _facadeOut1, out _entryPoint0, out _entryPoint1);

            _facadeIn0.WriteAction = InputPortManager.DataWriteAction.Push;
            _facadeIn0.ReadSource = InputPortManager.DataReadSource.BufferOrPull;

            _facadeOut0.ComputeFunction = new Action(() => { _facadeOut0.Buffer = _facadeIn0.Value; });
            _facadeIn0.SetDependents(_facadeOut0);

            _entryPoint0.OwnerPut("Data0");
            _entryPoint0.OwnerPut("Data1");
            //Console.WriteLine(out0.Take(null) + " taken.");
            //Console.WriteLine(out0.Take(null) + " taken.");
            //Console.WriteLine(out1.Take(null) + " taken.");
            //Console.WriteLine(out1.Take(null) + " taken.");

        }

        [TestMethod]
        public void DoAbbreviatedSimplePullTest()
        {

            PortManagementFacade pmf = Setup(out _in0, out _in1, out _out0, out _out1, out _facadeIn0, out _facadeIn1, out _facadeOut0, out _facadeOut1, out _entryPoint0, out _entryPoint1);

            _facadeIn0.WriteAction = InputPortManager.DataWriteAction.StoreAndInvalidate;
            _facadeIn0.ReadSource = InputPortManager.DataReadSource.BufferOrPull;
            _facadeIn0.DataBufferPersistence = PortManager.BufferPersistence.UntilRead;
            _facadeIn0.SetDependents(_facadeOut0);

            _facadeOut0.ComputeFunction = new Action(() => { _facadeOut0.Buffer = _facadeIn0.Value; });
            _facadeOut0.DataBufferPersistence = PortManager.BufferPersistence.UntilRead;

            Console.WriteLine(_out0.Take(null) + " taken.");
            Console.WriteLine(_out0.Take(null) + " taken.");

        }

        [TestMethod]
        public void DoAbbreviatedInputSideBufferedPullTest()
        {
            // Put a value into the input. Read the output several times. Replace the input, read the output twice more.

            PortManagementFacade pmf = Setup(out _in0, out _in1, out _out0, out _out1, out _facadeIn0, out _facadeIn1, out _facadeOut0, out _facadeOut1, out _entryPoint0, out _entryPoint1);

            _facadeIn0.WriteAction = InputPortManager.DataWriteAction.StoreAndInvalidate;
            _facadeIn0.ReadSource = InputPortManager.DataReadSource.BufferOrPull;
            _facadeIn0.DataBufferPersistence = PortManager.BufferPersistence.UntilWrite;
            _facadeIn0.SetDependents(_facadeOut0);

            _facadeOut0.ComputeFunction = new Action(() => { _facadeOut0.Buffer = _facadeIn0.Value; });
            _facadeOut0.DataBufferPersistence = PortManager.BufferPersistence.UntilRead;

            _entryPoint0.OwnerPut("PushData0");
            Console.WriteLine(_out0.Take(null) + " taken.");
            Console.WriteLine(_out0.Take(null) + " taken.");
            _entryPoint0.OwnerPut("PushData1");
            Console.WriteLine(_out0.Take(null) + " taken.");
            //Console.WriteLine(out1.Take(null) + " taken.");
            //Console.WriteLine(out1.Take(null) + " taken.");

        }

        [TestMethod]
        public void DoAbbreviatedOutputSideBufferedPullTest()
        {
            // Put a value into the input. Read the output several times. Replace the input, read the output twice more.

            PortManagementFacade pmf = Setup(out _in0, out _in1, out _out0, out _out1, out _facadeIn0, out _facadeIn1, out _facadeOut0, out _facadeOut1, out _entryPoint0, out _entryPoint1);

            _facadeIn0.WriteAction = InputPortManager.DataWriteAction.StoreAndInvalidate;
            _facadeIn0.ReadSource = InputPortManager.DataReadSource.Buffer;
            _facadeIn0.DataBufferPersistence = PortManager.BufferPersistence.None;
            _facadeIn0.SetDependents(_facadeOut0);

            _facadeOut0.ComputeFunction = new Action(() => { _facadeOut0.Buffer = _facadeIn0.Value; });
            _facadeOut0.DataBufferPersistence = PortManager.BufferPersistence.UntilWrite;

            _entryPoint0.OwnerPut("PushData0");
            Console.WriteLine(_out0.Take(null) + " taken.");
            Console.WriteLine(_out0.Take(null) + " taken.");
            _entryPoint0.OwnerPut("PushData1");
            Console.WriteLine(_out0.Take(null) + " taken.");
            //Console.WriteLine(out1.Take(null) + " taken.");
            //Console.WriteLine(out1.Take(null) + " taken.");

        }

        [TestMethod]
        public void DoAbbreviatedSimplePushPullTestWithOutputBuffering()
        {

            PortManagementFacade pmf = Setup(out _in0, out _in1, out _out0, out _out1, out _facadeIn0, out _facadeIn1, out _facadeOut0, out _facadeOut1, out _entryPoint0, out _entryPoint1);

            _facadeIn0.WriteAction = InputPortManager.DataWriteAction.Push;
            _facadeIn0.ReadSource = InputPortManager.DataReadSource.Pull;
            _facadeIn0.DataBufferPersistence = PortManager.BufferPersistence.None;
            _facadeIn0.SetDependents(_facadeOut0);

            _facadeOut0.ComputeFunction = new Action(() => { _facadeOut0.Buffer = _facadeIn0.Value; });
            _facadeOut0.DataBufferPersistence = PortManager.BufferPersistence.UntilWrite;

            _entryPoint0.OwnerPut("PushData0");
            _entryPoint0.OwnerPut("PushData1");
            Console.WriteLine(_out0.Take(null) + " taken.");
            Console.WriteLine(_out0.Take(null) + " taken.");
            //Console.WriteLine(out1.Take(null) + " taken.");
            //Console.WriteLine(out1.Take(null) + " taken.");

        }

        [TestMethod]
        public void DoAbbreviatedSimplePushPullTestNoOutputBuffering()
        {

            PortManagementFacade pmf = Setup(out _in0, out _in1, out _out0, out _out1, out _facadeIn0, out _facadeIn1, out _facadeOut0, out _facadeOut1, out _entryPoint0, out _entryPoint1);

            _facadeIn0.WriteAction = InputPortManager.DataWriteAction.Push;
            _facadeIn0.ReadSource = InputPortManager.DataReadSource.BufferOrPull;
            _facadeIn0.DataBufferPersistence = PortManager.BufferPersistence.UntilWrite;
            _facadeIn0.SetDependents(_facadeOut0);

            _facadeOut0.ComputeFunction = new Action(() => { _facadeOut0.Buffer = _facadeIn0.Value; });
            _facadeOut0.DataBufferPersistence = PortManager.BufferPersistence.UntilWrite;

            _entryPoint0.OwnerPut("PushData0");
            _entryPoint0.OwnerPut("PushData1");
            Console.WriteLine(_out0.Take(null) + " taken.");
            Console.WriteLine(_out0.Take(null) + " taken.");
            _entryPoint0.OwnerPut("PushData2");
            Console.WriteLine(_out0.Take(null) + " taken.");
            Console.WriteLine(_out0.Take(null) + " taken.");

        }

        [TestMethod]
        public void OneActiveOnePassiveInputDeterminesOneBufferedPassiveOutputTest()
        {

            PortManagementFacade pmf = Setup(out _in0, out _in1, out _out0, out _out1, out _facadeIn0, out _facadeIn1, out _facadeOut0, out _facadeOut1, out _entryPoint0, out _entryPoint1);

            // Active input. Writes to this port cause a value to be pushed out the output.
            _facadeIn0.WriteAction = InputPortManager.DataWriteAction.StoreAndInvalidate;
            _facadeIn0.ReadSource = InputPortManager.DataReadSource.BufferOrPull;
            _facadeIn0.DataBufferPersistence = PortManager.BufferPersistence.UntilRead;
            _facadeIn0.SetDependents(_facadeOut0);

            // Passive input. We pull a value if none is present, and store it in a buffer that remains valid until overwritten.
            _facadeIn1.WriteAction = InputPortManager.DataWriteAction.StoreAndInvalidate;
            _facadeIn1.ReadSource = InputPortManager.DataReadSource.BufferOrPull;
            _facadeIn1.DataBufferPersistence = PortManager.BufferPersistence.UntilWrite;
            _facadeIn1.SetDependents(_facadeOut0);

            // Pulls from the output cause the buffer value to be reused.
            _facadeOut0.ComputeFunction = new Action(() => { _facadeOut0.Buffer = ((string)_facadeIn0.Value) + ((string)_facadeIn1.Value); });
            _facadeOut0.DataBufferPersistence = PortManager.BufferPersistence.UntilWrite;

            _entryPoint0.OwnerPut("PushData0");
            Console.WriteLine("With data0 not yet provided, and data1 not yet provided, " + _out0.Take(null) + " taken.");
            Console.WriteLine("With data0 provided, and data1 not not yet provided, " + _out0.Take(null) + " taken.");
            _entryPoint0.OwnerPut("PushData1");
            _entryPoint0.OwnerPut("PushData2");
            _entryPoint1.OwnerPut("EntryPoint1(2)");
            Console.WriteLine(_out0.Take(null) + " taken.");

        }

        [TestMethod]
        public void TestOnePullValue()
        {
            OnePullValue opv = new OnePullValue(null, null, null, Guid.NewGuid());
            opv.ConstValue = 12.345;
            Console.WriteLine(opv.Ports.Outputs[0].Take(null));
            Console.WriteLine(opv.Ports.Outputs[0].Take(null));
            Console.WriteLine(opv.Ports.Outputs[0].Take(null));
        }

        [TestMethod]
        public void TestOneInOneOutPushPullTransform()
        {
            DLog dlog = new DLog(null, null, null, Guid.NewGuid());

            double i1 = 0.1;
            _entryPoint1 = new SimpleOutputPort(null, "", Guid.NewGuid(), null, delegate (IOutputPort iop, object selector)
            {
                return i1++;
            }, null);
            ConnectorFactory.Connect(_entryPoint1, dlog.Ports.Inputs[0]);

            dlog.Ports.Outputs[0].PortDataPresented += new PortDataEvent(delegate (object data, IPort where)
            {
                Console.WriteLine(string.Format("{0} presented at {1}.", data.ToString(), where.Name));
            });


            Console.WriteLine(String.Format("Pushing {0}", i1));
            _entryPoint1.OwnerPut(i1++);
            Console.WriteLine(String.Format("Pushing {0}", i1));
            _entryPoint1.OwnerPut(i1++);


            Console.WriteLine(dlog.Ports.Outputs[0].Take(null));
            Console.WriteLine(dlog.Ports.Outputs[0].Take(null));
            Console.WriteLine(dlog.Ports.Outputs[0].Take(null));
        }

        private PortManagementFacade Setup(out IInputPort in0, out IInputPort in1, out IOutputPort out0, out IOutputPort out1, out InputPortManager facadeIn0, out InputPortManager facadeIn1, out OutputPortManager facadeOut0, out OutputPortManager facadeOut1, out SimpleOutputPort entryPoint0, out SimpleOutputPort entryPoint1)
        {
            ManagementFacadeBlock mfb = new ManagementFacadeBlock();
            out0 = new SimpleOutputPort(null, "Out0", Guid.NewGuid(), mfb, null, null);
            out1 = new SimpleOutputPort(null, "Out1", Guid.NewGuid(), mfb, null, null);
            in0 = new SimpleInputPort(null, "In0", Guid.NewGuid(), mfb, null);
            in1 = new SimpleInputPort(null, "In1", Guid.NewGuid(), mfb, null);

            int i0 = 0;
            entryPoint0 = new SimpleOutputPort(null, "", Guid.NewGuid(), null, delegate (IOutputPort iop, object selector)
            {
                return string.Format("Src0 ({0})", i0++);
            }, null);
            ConnectorFactory.Connect(entryPoint0, in0);

            int i1 = 0;
            entryPoint1 = new SimpleOutputPort(null, "", Guid.NewGuid(), null, delegate (IOutputPort iop, object selector)
            {
                return string.Format("Src1 ({0})", i1++);
            }, null);
            ConnectorFactory.Connect(entryPoint1, in1);

            out0.PortDataPresented += new PortDataEvent(delegate (object data, IPort where)
            {
                Console.WriteLine(string.Format("{0} presented at {1}.", data.ToString(), where.Name));
            });
            out1.PortDataPresented += new PortDataEvent(delegate (object data, IPort where)
            {
                Console.WriteLine(string.Format("{0} presented at {1}.", data.ToString(), where.Name));
            });

            PortManagementFacade pmf = new PortManagementFacade(mfb);

            facadeIn0 = pmf.ManagerFor(in0);
            facadeIn1 = pmf.ManagerFor(in1);
            facadeOut0 = pmf.ManagerFor(out0);
            facadeOut1 = pmf.ManagerFor(out1);

            return pmf;
        }
    }

    internal class OnePullValue : ManagementFacadeBlock
    {

        private readonly SimpleOutputPort _output = null;
        private readonly OutputPortManager _opm = null;

        public OnePullValue(IModel model, string name, string description, Guid guid)
        {

            _output = new SimpleOutputPort(null, "Output", Guid.NewGuid(), this, null, null);

            PortManagementFacade pmf = new PortManagementFacade(this);

            _opm = pmf.ManagerFor(_output);

            _opm.DataBufferPersistence = PortManager.BufferPersistence.UntilWrite; // Output value is reusable. We never use the compute function.

        }

        public double ConstValue
        {
            get
            {
                return (double)_opm.Buffer;
            }
            set
            {
                _opm.Buffer = value;
            }
        }
    }

    /// <summary>
    /// This class assumes that each time a write is done to the input port, computation and a push to the output port
    /// is expected, and each time a pull is done from the output port, a pull from the input port and recomputation of
    /// the value to be presented on the output port is expected.
    /// </summary>
    internal abstract class OneInOneOutPushPullTransform : ManagementFacadeBlock
    {
        private readonly SimpleInputPort _input = null;
        private readonly SimpleOutputPort _output = null;

        private readonly InputPortManager _ipm = null;
        private readonly OutputPortManager _opm = null;

        public OneInOneOutPushPullTransform(IModel model, string name, string description, Guid guid)
        {

            _output = new SimpleOutputPort(null, "Output", Guid.NewGuid(), this, null, null);
            _input = new SimpleInputPort(null, "Input", Guid.NewGuid(), this, null);

            _input.PortDataPresented += new PortDataEvent(delegate (object data, IPort where)
            {
                Console.WriteLine(data.ToString() + " presented to " + where.Name);
            });

            PortManagementFacade pmf = new PortManagementFacade(this);

            _ipm = pmf.ManagerFor(_input);
            _opm = pmf.ManagerFor(_output);

            _ipm.WriteAction = InputPortManager.DataWriteAction.Push; // When a value is written into the input buffer, we push the resultant transform out the output port.
            _ipm.DataBufferPersistence = InputPortManager.BufferPersistence.None; // The input buffer is re-read with every pull.
            _ipm.ReadSource = InputPortManager.DataReadSource.Pull; // We'll always pull a new value.
            _ipm.SetDependents(_opm); // A new value written to ipm impacts opm.

            _opm.ComputeFunction = new Action(ComputeFuction);
            _opm.DataBufferPersistence = PortManager.BufferPersistence.None; // Output value is always recomputed.

        }

        public object Input
        {
            get
            {
                return _ipm.Value;
            }
        }
        public object Output
        {
            set
            {
                _opm.Buffer = value;
            }
        }

        protected abstract void ComputeFuction();
    }

    class DLog : OneInOneOutPushPullTransform
    {

        public DLog(IModel model, string name, string description, Guid guid) : base(model, name, description, guid) { }

        protected override void ComputeFuction()
        {
            Output = Math.Log((double)Input, Math.E);
        }
    }

    class SimplePassThroughPortOwner : IPortOwner, IHasName
    {
        public IInputPort In
        {
            get
            {
                return _in;
            }
        }
        private readonly SimpleInputPort _in;
        public IOutputPort Out
        {
            get
            {
                return _out;
            }
        }
        private readonly SimpleOutputPort _out;
        public string Name
        {
            get
            {
                return _name;
            }
        }
        private object _buffer;
        private readonly string _name;

        public SimplePassThroughPortOwner(IModel model, string name, Guid guid)
        {
            _name = name;
            _in = new SimpleInputPort(model, "In", Guid.NewGuid(), this, new DataArrivalHandler(OnDataArrived));
            _out = new SimpleOutputPort(model, "Out", Guid.NewGuid(), this, new DataProvisionHandler(OnDataRequested), null);
        }

        private bool OnDataArrived(object data, IInputPort ip)
        {
            Debug.Write(Name + " was just given data (" + data.ToString() + ")");
            if (In.Peer != null)
            {
                Debug.WriteLine(" by " + ((IHasName)In.Peer.Owner).Name);
            }
            else
            {
                Debug.WriteLine(" by some non-connected element.");
            }

            _buffer = data;

            if (_out.Peer != null)
            {
                return _out.OwnerPut(data);
            }
            else
            {
                return false;
            }
        }

        private object OnDataRequested(IOutputPort op, object selector)
        {

            Debug.Write(Name + " was just asked for data by ");
            if (Out.Peer != null)
            {
                Debug.WriteLine(((IHasName)Out.Peer.Owner).Name);
            }
            else
            {
                Debug.WriteLine("some non-connected element.");
            }

            if (_in.Peer != null)
            {
                Debug.WriteLine("I will ask " + ((IHasName)In.Peer.Owner).Name + "...");
                return _in.OwnerTake(null);
            }
            else
            {
                string data = RandomString();
                Debug.WriteLine("Since I have no predecessor, I'll make up some data. How about " + data + "...");
                return data;
            }
        }

        private readonly string _letters = "abcdefghijklmnopqrstuvwxyz";
        private readonly Random _random = new Random();
        private string RandomString()
        {
            return RandomString(10);
        }
        private string RandomString(int nChars)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(nChars);
            for (int i = 0; i < nChars; i++)
                sb.Append(_letters[_random.Next(_letters.Length)]);
            return sb.ToString();
        }


        #region IPortOwner Implementation
        /// <summary>
        /// The PortSet object to which this IPortOwner delegates.
        /// </summary>
        private readonly PortSet _ports = new PortSet();
        /// <summary>
        /// Registers a port with this IPortOwner
        /// </summary>
        /// <param name="key">The key by which this IPortOwner will know this port.</param>
        /// <param name="port">The port that this IPortOwner will know by this key.</param>
        public void AddPort(IPort port)
        {
            _ports.AddPort(port);
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

        /// <summary>
        /// Unregisters a port from this IPortOwner.
        /// </summary>
        /// <param name="key">The key by which the port being unregistered is known.</param>
        public void RemovePort(IPort port)
        {
            _ports.RemovePort(port);
        }

        /// <summary>
        /// Unregisters all ports that this IPortOwner knows to be its own.
        /// </summary>
        public void ClearPorts()
        {
            _ports.ClearPorts();
        }

        /// <summary>
        /// The public property that is the PortSet this IPortOwner owns.
        /// </summary>
        public IPortSet Ports
        {
            get
            {
                return _ports;
            }
        }
        #endregion
    }

    class SimpleProxyPortOwner : IPortOwner
    {
        private readonly string _name;
        private readonly SimplePassThroughPortOwner _sptpo;
        public IInputPort In
        {
            get
            {
                return _in;
            }
        }
        private readonly IInputPort _in;
        public IOutputPort Out
        {
            get
            {
                return _out;
            }
        }
        private readonly IOutputPort _out;

        public SimpleProxyPortOwner(IModel model, string name, Guid guid)
        {
            _name = name;
            _sptpo = new SimplePassThroughPortOwner(model, name + ".internal", Guid.NewGuid());

            _in = new InputPortProxy(model, "In", null, Guid.NewGuid(), this, _sptpo.In);
            _out = new OutputPortProxy(model, "Out", null, Guid.NewGuid(), this, _sptpo.Out);
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }


        #region IPortOwner Members

        public void AddPort(IPort port)
        {
            throw new Exception("The method or operation is not implemented.");
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
            throw new Exception("The method or operation is not implemented.");
        }

        public void ClearPorts()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public IPortSet Ports
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        #endregion
    }

    class ManagementFacadeBlock : IPortOwner
    {
        private readonly PortSet _ps = new PortSet();

        public void AddPort(IPort port)
        {
            _ps.AddPort(port);
        }

        public IPort AddPort(string channelTypeName)
        {
            throw new NotImplementedException();
        }

        public IPort AddPort(string channelTypeName, Guid guid)
        {
            throw new NotImplementedException();
        }

        public List<IPortChannelInfo> SupportedChannelInfo
        {
            get
            {
                return GeneralPortChannelInfo.StdInputAndOutput;
            }
        }

        public void RemovePort(IPort port)
        {
            _ps.RemovePort(port);
        }

        public void ClearPorts()
        {
            _ps.ClearPorts();
        }

        public IPortSet Ports
        {
            get
            {
                return _ps;
            }
        }
    }
}
