/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Graphs.Tasks;
using Highpoint.Sage.SimCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Diagnostics;


namespace Highpoint.Sage.Graphs
{

    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public class GraphLoopingTester
    {

        #region MSTest Goo
        [TestInitialize]
        public void Init()
        {
        }
        [TestCleanup]
        public void destroy()
        {
            Debug.WriteLine("Done.");
        }
        #endregion

        private System.Text.StringBuilder _out;
        private readonly string _loopResult = "Edge Sub1 is running.Edge Sub2 is running.Edge Sub2 is running.Edge Sub2 is running.Edge Sub2 is running.Edge Sub2 is running.Edge Sub2 is running.Edge Sub3 is running.";
        private readonly string _branchResult = "Edge Sub1 is running.Edge Sub2 is running.Edge Sub1 is running.Edge Sub2 is running.Edge Sub1 is running.Edge Sub3 is running.";

        public GraphLoopingTester()
        {

        }

        [TestMethod]
        public void TestBasicLooping()
        {

            _out = new System.Text.StringBuilder();

            Model model = new Model("Model");
            model.AddService<ITaskManagementService>(new TaskManagementService());

            Task root = new Task(model, "Root");
            new TaskProcessor(model, "taskProcessor", root); // It is added into the model automatically.

            Edge sub1 = new MyEdge("Sub1", _out);
            Edge sub2 = new MyEdge("Sub2", _out);
            Edge sub3 = new MyEdge("Sub3", _out);

            CreateLoopback(model, sub2.PostVertex, sub2.PreVertex, "LoopbackChannelMarker", 5);

            ArrayList children = new ArrayList();
            children.Add(sub1);
            children.Add(sub2);
            children.Add(sub3);
            root.AddChainOfChildren(children);

            model.Start();

            Assert.IsTrue(_loopResult.Equals(_out.ToString()), "LoopingTester Results", "Looping tester failed to match expected results.");

        }

        [TestMethod]
        public void TestBasicBranching()
        {
            _out = new System.Text.StringBuilder();
            Model model = new Model("Model");
            model.AddService<ITaskManagementService>(new TaskManagementService());
            object branchChannelMarker = "BranchChannelMarker";

            Task root = new Task(model, "Root");
            new TaskProcessor(model, "taskProcessor", root); // It is added into the model automatically.

            Edge sub1 = new MyEdge("Sub1", _out);
            Edge sub2 = new MyEdge("Sub2", _out);
            Edge sub3 = new MyEdge("Sub3", _out);

            Edge.Connect(root.PreVertex, sub1.PreVertex);
            Edge.Connect(sub1.PostVertex, sub2.PreVertex).Channel = branchChannelMarker;
            Edge.Connect(sub2.PostVertex, sub1.PreVertex).Channel = branchChannelMarker;
            Edge.Connect(sub1.PostVertex, sub3.PreVertex); //Accept default channel marker.
            Edge.Connect(sub3.PostVertex, root.PostVertex);

            sub2.Channel = "AlternateChannelMarker";
            sub1.PostVertex.EdgeFiringManager = new CountedBranchManager(model, new object[] { branchChannelMarker, Edge.NULL_CHANNEL_MARKER }, new int[] { 2, 1 });
            sub1.PreVertex.EdgeReceiptManager = new MultiChannelEdgeReceiptManager(sub1.PreVertex);
            sub2.PreVertex.EdgeReceiptManager = new MultiChannelEdgeReceiptManager(sub2.PreVertex);
            sub3.PreVertex.EdgeReceiptManager = new MultiChannelEdgeReceiptManager(sub3.PreVertex);

            model.Start();

            Assert.IsTrue(_branchResult.Equals(_out.ToString()), "BranchingTester Results", "Branching tester failed to match expected results.");
        }

        private void CreateLoopback(IModel model, Vertex from, Vertex to, object channelMarker, int howManyTimes)
        {
            Edge loopback = Edge.Connect(from, to);
            loopback.Channel = channelMarker;
            from.EdgeFiringManager = new CountedBranchManager(model, new object[] { channelMarker, Edge.NULL_CHANNEL_MARKER }, new int[] { howManyTimes, 1 });
            to.EdgeReceiptManager = new MultiChannelEdgeReceiptManager(to);
        }

        class MyEdge : Edge
        {
            private System.Text.StringBuilder _out;
            public MyEdge(string name, System.Text.StringBuilder _out) : base(name)
            {
                this._out = _out;
                this.EdgeStartingEvent += new EdgeEvent(EdgeStarting);
            }
            private void EdgeStarting(IDictionary graphContext, Edge theEdge)
            {
                _out.Append("Edge " + theEdge.Name + " is running.");
            }
        }


    }
}
