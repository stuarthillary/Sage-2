/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.SimCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Diagnostics;

namespace Highpoint.Sage.Graphs.Tasks
{

    [TestClass]
    public class TaskTester
    {

        private readonly Random _random = new Random();

        public TaskTester()
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
        [Highpoint.Sage.Utility.FieldDescription("This test runs a parent task, which spans over the length of the five child tasks running in a sequence.")]
        public void TestChildSequencing()
        {
            Model model = new Model();
            model.AddService<ITaskManagementService>(new TaskManagementService());

            TestTask parent = new TestTask(model, "Parent");

            TaskProcessor tp = new TaskProcessor(model, "TP", parent) { KeepGraphContexts = true };
            model.GetService<ITaskManagementService>().AddTaskProcessor(tp);


            TestTask[] children = new TestTask[5];
            for (int i = 0; i < children.Length; i++)
            {
                children[i] = new TestTask(model, "Child" + i, TimeSpan.FromHours(i));
                if (i > 0)
                    children[i].AddPredecessor(children[i - 1]);
                parent.AddChildEdge(children[i]);
            }

            model.Start();

            IDictionary gc = (IDictionary)tp.GraphContexts[0];
            Assert.AreEqual(new DateTime(1, 1, 1, 0, 0, 0), parent.GetStartTime(gc), "Parent task did't start at the correct time.");
            Assert.AreEqual(new DateTime(1, 1, 1, 0, 0, 0), children[0].GetStartTime(gc), "Child task 1 did't start at the correct time.");
            Assert.AreEqual(new DateTime(1, 1, 1, 0, 0, 0), children[1].GetStartTime(gc), "Child task 2 did't start at the correct time.");
            Assert.AreEqual(new DateTime(1, 1, 1, 1, 0, 0), children[2].GetStartTime(gc), "Child task 3 did't start at the correct time.");
            Assert.AreEqual(new DateTime(1, 1, 1, 3, 0, 0), children[3].GetStartTime(gc), "Child task 4 did't start at the correct time.");
            Assert.AreEqual(new DateTime(1, 1, 1, 6, 0, 0), children[4].GetStartTime(gc), "Child task 5 did't start at the correct time.");
            Assert.AreEqual(new DateTime(1, 1, 1, 0, 0, 0), children[0].GetFinishTime(gc), "Child task 1 did't finish at the correct time.");
            Assert.AreEqual(new DateTime(1, 1, 1, 1, 0, 0), children[1].GetFinishTime(gc), "Child task 2 did't finish at the correct time.");
            Assert.AreEqual(new DateTime(1, 1, 1, 3, 0, 0), children[2].GetFinishTime(gc), "Child task 3 did't finish at the correct time.");
            Assert.AreEqual(new DateTime(1, 1, 1, 6, 0, 0), children[3].GetFinishTime(gc), "Child task 4 did't finish at the correct time.");
            Assert.AreEqual(new DateTime(1, 1, 1, 10, 0, 0), children[4].GetFinishTime(gc), "Child task 5 did't finish at the correct time.");
            Assert.AreEqual(new DateTime(1, 1, 1, 10, 0, 0), parent.GetFinishTime(gc), "Parent task did't finish at the correct time.");

        }

        // Ta(4 hr) -> Tb(1 hr)
        // Tc(1 hr) -> Td(1 hr) Check to see that Td starts at T=1.
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks to see that Td starts at T=1")]
        public void TestPlainGraph()
        {

            TestGraph1 tg1 = new TestGraph1();

            tg1.Model.Start();

            Assert.IsTrue(tg1.Ta.GetStartTime(tg1.GraphContext).Equals(new DateTime(1, 1, 1, 0, 0, 0)), "Task A did not start at 12AM 1/1/1");
            Assert.IsTrue(tg1.Ta.GetFinishTime(tg1.GraphContext).Equals(new DateTime(1, 1, 1, 4, 0, 0)), "Task A did not finish at 4AM 1/1/1");

            Assert.IsTrue(tg1.Tb.GetStartTime(tg1.GraphContext).Equals(new DateTime(1, 1, 1, 4, 0, 0)), "Task B did not start at 4AM 1/1/1");
            Assert.IsTrue(tg1.Tb.GetFinishTime(tg1.GraphContext).Equals(new DateTime(1, 1, 1, 5, 0, 0)), "Task B did not finish at 5AM 1/1/1");

            Assert.IsTrue(tg1.Tc.GetStartTime(tg1.GraphContext).Equals(new DateTime(1, 1, 1, 0, 0, 0)), "Task C did not start at 12AM 1/1/1");
            Assert.IsTrue(tg1.Tc.GetFinishTime(tg1.GraphContext).Equals(new DateTime(1, 1, 1, 1, 0, 0)), "Task C did not finish at 1AM 1/1/1");

            Assert.IsTrue(tg1.Td.GetStartTime(tg1.GraphContext).Equals(new DateTime(1, 1, 1, 1, 0, 0)), "Task D did not start at 1AM 1/1/1");
            Assert.IsTrue(tg1.Td.GetFinishTime(tg1.GraphContext).Equals(new DateTime(1, 1, 1, 2, 0, 0)), "Task D did not start at 1AM 1/1/1");

            Assert.IsTrue(tg1.Parent.GetStartTime(tg1.GraphContext).Equals(new DateTime(1, 1, 1, 0, 0, 0)), "Task parent did not start at 12AM 1/1/1");
            Assert.IsTrue(tg1.Parent.GetFinishTime(tg1.GraphContext).Equals(new DateTime(1, 1, 1, 5, 0, 0)), "Task parent did not finish at 5AM 1/1/1");

            Assert.IsTrue(tg1.Follow.GetStartTime(tg1.GraphContext).Equals(new DateTime(1, 1, 1, 5, 0, 0)), "Task follow did not start at 5AM 1/1/1");
            Assert.IsTrue(tg1.Follow.GetFinishTime(tg1.GraphContext).Equals(new DateTime(1, 1, 1, 5, 0, 0)), "Task follow did not finish at 5AM 1/1/1");

        }

        // Ta(4 hr) -> Tb(1 hr)
        // Tc(1 hr) -> Td(1 hr) (Td costart-slaved to Tb, so it starts at t=4, not t=1.)
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks Td costart-slaved to Tb, so it starts at t=4, not t=1.")]
        public void TestCoStart()
        {

            TestGraph1 tg1 = new TestGraph1();
            tg1.Tb.AddCostart(tg1.Td);

            tg1.Model.Start();

            Assert.IsTrue(tg1.Tb.GetStartTime(tg1.GraphContext).Equals(new DateTime(1, 1, 1, 4, 0, 0)), "Task B did not start at 4AM 1/1/1");

            Assert.IsTrue(tg1.Td.GetStartTime(tg1.GraphContext).Equals(new DateTime(1, 1, 1, 4, 0, 0)), "Task D did not start at 4AM 1/1/1");

        }

        // Ta(4 hr) -> Tb(1 hr)
        // Tc(1 hr) -> Td(1 hr) (Tc cofinish-slaved to Ta, so it ends at t=4, not t=1.)
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks Tc cofinish-slaved to Ta, so it ends at t=4, not t=1.")]
        public void TestCoFinish()
        {

            TestGraph1 tg1 = new TestGraph1();
            tg1.Ta.AddCofinish(tg1.Tc);

            tg1.Model.Start();

            Assert.IsTrue(tg1.Ta.GetFinishTime(tg1.GraphContext).Equals(new DateTime(1, 1, 1, 4, 0, 0)), "Task A did not finish at 4AM 1/1/1");

            Assert.IsTrue(tg1.Tc.GetFinishTime(tg1.GraphContext).Equals(new DateTime(1, 1, 1, 4, 0, 0)), "Task C did not finish at 4AM 1/1/1");

        }

        // Ta(4 hr) -> Tb(1 hr)
        // Tc(1 hr) -> Td(1 hr) (Tc.finish synched to Ta.finish, so tb and td start at t=4.)
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Checks if two tasks can be synchronized to start at the the same time.")]
        public void TestSynchroStart()
        {

            TestGraph1 tg1 = new TestGraph1();
            TestGraph1 tg2 = new TestGraph1();
            // Synchronize tb and td
            VertexSynchronizer vs1 = new VertexSynchronizer(tg1.Model.Executive, new Vertex[] { tg1.Tb.PreVertex, tg1.Td.PreVertex }, ExecEventType.Detachable);
            // Synchronize tb and tc
            VertexSynchronizer vs2 = new VertexSynchronizer(tg2.Model.Executive, new Vertex[] { tg2.Tb.PreVertex, tg2.Tc.PreVertex }, ExecEventType.Detachable);

            tg1.Model.Start();
            Debug.WriteLine("Test 2");
            tg2.Model.Start();

            // Test graph 1
            Assert.IsTrue(tg1.Tb.GetStartTime(tg1.GraphContext).Equals(new DateTime(1, 1, 1, 4, 0, 0)), "Task B did not start at 4AM 1/1/1");

            Assert.IsTrue(tg1.Td.GetStartTime(tg1.GraphContext).Equals(new DateTime(1, 1, 1, 4, 0, 0)), "Task D did not start at 4AM 1/1/1");

            Assert.IsTrue(tg1.Parent.GetFinishTime(tg1.GraphContext).Equals(new DateTime(1, 1, 1, 5, 0, 0)), "Task parent did not finish at 5AM 1/1/1");

            // Test graph 2
            Assert.IsTrue(tg2.Tb.GetStartTime(tg2.GraphContext).Equals(new DateTime(1, 1, 1, 4, 0, 0)), "Task B did not start at 4AM 1/1/1");

            Assert.IsTrue(tg2.Tc.GetStartTime(tg2.GraphContext).Equals(new DateTime(1, 1, 1, 4, 0, 0)), "Task C did not start at 4AM 1/1/1");

            Assert.IsTrue(tg2.Parent.GetFinishTime(tg2.GraphContext).Equals(new DateTime(1, 1, 1, 6, 0, 0)), "Task parent did not finish at 6AM 1/1/1");

        }

        /*
        // Ta(4 hr) -> Tb(1 hr)
        // Tc(1 hr) -> Td(1 hr) (Tc.finish synched to Ta.finish, so tb and td start at t=4.)
        [TestMethod] 
		[Highpoint.Sage.Utility.Description("Checks if two tasks can be synchronized to end at the the same time.")]
		[Ignore("This is a future feature - not yet implemented in code.")]
		public void TestSynchroFinish(){

			TestGraph1 tg1 = new TestGraph1();
			TestGraph1 tg2 = new TestGraph1();
			VertexSynchronizer vs1 = new VertexSynchronizer(tg1.model.Executive,new Vertex[]{tg1.ta.PostVertex,tg1.tc.PostVertex},ExecEventType.Detachable);
			VertexSynchronizer vs2 = new VertexSynchronizer(tg2.model.Executive,new Vertex[]{tg2.tc.PostVertex,tg2.ta.PostVertex},ExecEventType.Detachable);

            tg1.model.Start();
			Debug.WriteLine("Test 2");
			tg2.model.Start();

			// Test graph 1
			System.Diagnostics.Debug.Assert(tg1.ta.GetFinishTime(tg1.GraphContext).Equals(new DateTime(1,1,1,4,0,0)),"Task A did not finish at 4AM 1/1/1");

			System.Diagnostics.Debug.Assert(tg1.tc.GetFinishTime(tg1.GraphContext).Equals(new DateTime(1,1,1,4,0,0)),"Task C did not finish at 4AM 1/1/1");

			System.Diagnostics.Debug.Assert(tg1.td.GetStartTime(tg1.GraphContext).Equals(new DateTime(1,1,1,4,0,0)),"Task D did not start at 4AM 1/1/1");

			// Test graph 2
			System.Diagnostics.Debug.Assert(tg2.ta.GetFinishTime(tg2.GraphContext).Equals(new DateTime(1,1,1,4,0,0)),"Task A did not finish at 4AM 1/1/1");

			System.Diagnostics.Debug.Assert(tg2.tc.GetFinishTime(tg2.GraphContext).Equals(new DateTime(1,1,1,1,0,0)),"Task C did not finish at 1AM 1/1/1");

			System.Diagnostics.Debug.Assert(tg2.td.GetStartTime(tg2.GraphContext).Equals(new DateTime(1,1,1,1,0,0)),"Task D did not start at 1AM 1/1/1");

        }*/


        class TestGraph1
        {
            public TestTask Ta, Tb, Tc, Td;
            public TestTask Parent, Follow;
            public TaskProcessor Tp;
            public Model Model;
            public TestGraph1()
            {
                Model = new Model();
                Model.AddService<ITaskManagementService>(new TaskManagementService());


                Parent = new TestTask(Model, "Parent");
                Follow = new TestTask(Model, "Follow");

                Tp = new TaskProcessor(Model, "TP", Parent);
                Tp.KeepGraphContexts = true;

                Ta = new TestTask(Model, "TaskA", TimeSpan.FromHours(4));
                Tb = new TestTask(Model, "TaskB", TimeSpan.FromHours(1));
                Tc = new TestTask(Model, "TaskC", TimeSpan.FromHours(1));
                Td = new TestTask(Model, "TaskD", TimeSpan.FromHours(1));

                Parent.AddChildEdge(Ta);
                Parent.AddChildEdge(Tb);
                Parent.AddChildEdge(Tc);
                Parent.AddChildEdge(Td);
                Ta.AddSuccessor(Tb);
                Tc.AddSuccessor(Td);
                Parent.AddSuccessor(Follow);
            }

            public IDictionary GraphContext
            {
                get
                {
                    return (IDictionary)Tp.GraphContexts[0];
                }
            }
        }


        /*********************************************************************************/
        /*                   S  U  P  P  O  R  T     M  E  T  H  O  D  S                 */
        /*********************************************************************************/
        IList CreateSubGraph(IModel model, int howManyTasks, string nameRoot)
        {
            ArrayList edges = new ArrayList();
            for (int i = 0; i < howManyTasks; i++)
            {
                TestTask task = new TestTask(model, nameRoot + i);
                Debug.WriteLine("Creating task " + task.Name);
                edges.Add(task);
            }

            while (true)
            {

                // Select 2 tasks, and connect them.
                TestTask taskA = (TestTask)((Edge)edges[_random.Next(edges.Count)]);
                TestTask taskB = (TestTask)((Edge)edges[_random.Next(edges.Count)]);

                if (taskA == taskB)
                    continue;

                Debug.WriteLine(String.Format("Considering a connection between {0} and {1}.", taskA.Name, taskB.Name));

                int forward = Graphs.Analysis.PathLength.ShortestPathLength(taskA, taskB);
                int backward = Graphs.Analysis.PathLength.ShortestPathLength(taskB, taskA);

                Debug.WriteLine(String.Format("Forward path length is {0}, and reverse path length is {1}.", forward, backward));

                if ((forward == int.MaxValue) && (backward == int.MaxValue))
                {
                    taskA.AddSuccessor(taskB);
                    Debug.WriteLine(String.Format("{0} will follow {1}.", taskB.Name, taskA.Name));
                }
                else if ((forward != int.MaxValue) && (backward == int.MaxValue))
                {
                    taskA.AddSuccessor(taskB);
                    Debug.WriteLine(String.Format("{0} will follow {1}.", taskB.Name, taskA.Name));
                }
                else if ((forward == int.MaxValue) && (backward != int.MaxValue))
                {
                    taskB.AddSuccessor(taskA);
                    Debug.WriteLine(String.Format("{1} will follow {0}.", taskB.Name, taskA.Name));
                }
                else
                {
                    throw new ApplicationException("Cycle exists between " + taskA.Name + " and " + taskB.Name + ".");
                }

                // Once all tasks are connected to something, we're done constructing the test.
                bool allTasksAreConnected = true;
                foreach (Edge edge in edges)
                {
                    Task task = (Task)edge;
                    if ((edge.PredecessorEdges.Count == 0) && (edge.SuccessorEdges.Count == 0))
                    {
                        allTasksAreConnected = false;
                        break;
                    }
                }
                if (allTasksAreConnected)
                    break;
            }

            return edges;
        }


        class TestTask : Highpoint.Sage.Graphs.Tasks.Task
        {
            private TimeSpan _delay = TimeSpan.Zero;
            private bool _svs = true;
            public TestTask(IModel model, string name) : this(model, name, TimeSpan.Zero) { }
            public TestTask(IModel model, string name, TimeSpan delay) : base(model, name, Guid.NewGuid())
            {
                _delay = delay;
                this.EdgeExecutionStartingEvent += new EdgeEvent(OnTaskBeginning);
                this.EdgeExecutionFinishingEvent += new EdgeEvent(OnTaskCompleting);
            }

            protected override void DoTask(IDictionary graphContext)
            {
                SelfValidState = _svs;
                if (_delay.Equals(TimeSpan.Zero))
                {
                    SignalTaskCompletion(graphContext);
                }
                else
                {
                    Debug.WriteLine(Model.Executive.Now + " : " + Name + " is commencing a sleep for " + _delay + ".");
                    Model.Executive.RequestEvent(new ExecEventReceiver(DoneDelaying), Model.Executive.Now + _delay, 0.0, graphContext);
                }
            }

            private void DoneDelaying(IExecutive exec, object graphContext)
            {
                SignalTaskCompletion((IDictionary)graphContext);
                Debug.WriteLine(Model.Executive.Now + " : " + Name + " is done.");
            }

            private void OnTaskBeginning(IDictionary graphContext, Edge edge)
            {
                Debug.WriteLine(Model.Executive.Now + " : " + Name + " is beginning.");
            }

            private void OnTaskCompleting(IDictionary graphContext, Edge edge)
            {
                Debug.WriteLine(Model.Executive.Now + " : " + Name + " is completing.");
            }
        }
    }
}

