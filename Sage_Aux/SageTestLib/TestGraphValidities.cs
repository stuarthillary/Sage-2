/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Graphs;
using Highpoint.Sage.Graphs.Tasks;
using Highpoint.Sage.SimCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Diagnostics;

namespace Highpoint.Sage.Tasks
{

    [TestClass]
    public class GraphValidityTester
    {

        private Random _random = new Random();
        private Model _model;
        private Task _t, _t1, _t2, _t3, _t11, _t12, _t13, _t21, _t22, _t23, _t31, _t32, _t33;
        private TaskList _tL1, _tL2, _tL3, _tLnew;
        private TaskProcessor _tp;
        private static readonly bool VERBOSE = true;

        public GraphValidityTester()
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

        //		[TestMethod]
        //		[Highpoint.Sage.Utility.Description("This test initializes a model and runs a validation")]
        ////		public void TestTaskEnumerators() {
        //			InitializeModel("t,t1,t2,t3,t11,t12,t13,t21,t22,t23,t31,t32,t33");
        //
        //			Synchronize(t1,t2,t3);
        //			Synchronize(t11,t21,t31);
        //
        //			Task[] targets = new Task[]{t,t1,t2,t3,t11,t12,t13,t21,t22,t23,t31,t32,t33};
        //
        //			foreach ( TaskEnumerator.Direction direction in new TaskEnumerator.Direction[]{TaskEnumerator.Direction.Forward,TaskEnumerator.Direction.Backward}){
        //				foreach ( Task target in targets ) {
        //					Console.Write(target.Name + " (" + direction.ToString() + ") contains ");
        //					foreach ( Task task in new TaskEnumerator(target,direction) ) {
        //						Console.Write(task.Name + ", ");
        //					}
        //					Console.WriteLine();
        //				}
        //			}
        //		}

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test initializes a model and runs a validation")]
        public void TestBasicValidation()
        {
            InitializeModel("t,t1,t2,t3,t12,t13,t21,t23,t31,t32");

            Console.WriteLine(_t.ValidationService.StatusReport());

            Validate("Basic Validation");

            Console.WriteLine(_t.ValidationService.StatusReport());

            Assert.IsTrue(_t.ValidityState, "Freshly initialized model is not valid.");

        }

        /// <summary>
        /// - Adding a task in front of another task sets both task invalid
        /// - Adding a downstream task to an upstream task leaves the upstream task in its state, but sets the downstream task invalid
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test adds tasks before and behind another task")]
        public void TestAddTasks()
        {
            InitializeModel("t,t1,t2,t3,t12,t21,t23,t32");

            Assert.IsTrue(!_t12.ValidityState, "Task 12 is valid when it should be invalid");
            Assert.IsTrue(!_t21.ValidityState, "Task 21 is valid when it should be invalid");
            Assert.IsTrue(!_t23.ValidityState, "Task 23 is valid when it should be invalid");
            Assert.IsTrue(!_t32.ValidityState, "Task 32 is valid when it should be invalid");

            _model.Start();

            Assert.IsTrue(_t12.ValidityState, "Task 12 is invalid when it should be valid");
            Assert.IsTrue(_t21.ValidityState, "Task 21 is invalid when it should be valid");
            Assert.IsTrue(_t23.ValidityState, "Task 23 is invalid when it should be valid");
            Assert.IsTrue(_t32.ValidityState, "Task 32 is invalid when it should be valid");

            // Statement: All tasks that are now in the graph are valid!

            Assert.IsTrue(!_t11.ValidityState, "Task 11 is valid when it should be invalid");
            Assert.IsTrue(_t12.ValidityState, "Task 12 is invalid when it should be valid");
            _tL1.AddTaskBefore(_t12, _t11);
            Assert.IsTrue(!_t11.ValidityState, "Task 11 is valid when it should be invalid");
            Assert.IsTrue(!_t12.ValidityState, "Task 12 is valid when it should be invalid");


            // Add a task after a valid one.
            _tL2.AddTaskAfter(_t21, _t22);
            Assert.IsTrue(_t21.ValidityState, "Task 21 is invalid when it should be valid");
            Assert.IsTrue(!_t22.ValidityState, "Task 22 is valid when it should be invalid");

            // Add a task before a valid one.
            _t31.SelfValidState = false;
            _tL3.AddTaskBefore(_t32, _t31);     // this forces t32 to become invalid
            Assert.IsTrue(!_t32.ValidityState, "Task 32 is valid when it should be invalid");
            Assert.IsTrue(!_t31.ValidityState, "Task 31 is valid when it should be invalid");

            // Add a task after an invalid one
            _tL3.AddTaskAfter(_t32, _t33);
            Assert.IsTrue(!_t32.ValidityState, "Task 32 is valid when it should be invalid");
            Assert.IsTrue(!_t33.ValidityState, "Task 33 is valid when it should be invalid");

            Validate("Test 1");

            Assert.IsTrue(_t11.ValidityState, "Task 11 is invalid when it should be valid");
            Assert.IsTrue(_t12.ValidityState, "Task 12 is invalid when it should be valid");
            Assert.IsTrue(_t21.ValidityState, "Task 21 is invalid when it should be valid");
            Assert.IsTrue(_t22.ValidityState, "Task 22 is invalid when it should be valid");
            Assert.IsTrue(_t32.ValidityState, "Task 32 is invalid when it should be valid");
            Assert.IsTrue(_t33.ValidityState, "Task 33 is invalid when it should be valid");

        }

        /// <summary>
        /// - Adding a task in front of another task sets both task invalid
        /// - Adding a downstream task to an upstream task leaves the upstream task in its state, but sets the downstream task invalid
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test (used to cause) a resume failure. It is here to ensure that bug does not return.")]
        public void TestToCauseResumeFailure()
        {
            InitializeModel("t,t1,t2,t3,t11,t12,t13,t21,t22,t23,t31,t32");

            // Add a task after...

            _tL3.AddTaskAfter(_t32, _t33);

            Validate("Resume Failure Test");

            Assert.IsTrue(_t11.ValidityState, "Task 11 is invalid when it should be valid");
            Assert.IsTrue(_t12.ValidityState, "Task 12 is invalid when it should be valid");
            Assert.IsTrue(_t21.ValidityState, "Task 21 is invalid when it should be valid");
            Assert.IsTrue(_t22.ValidityState, "Task 22 is invalid when it should be valid");
            Assert.IsTrue(_t32.ValidityState, "Task 32 is invalid when it should be valid");
            Assert.IsTrue(_t33.ValidityState, "Task 33 is invalid when it should be valid");

        }

        /// <summary>
        /// - Synchronize two valid tasks sets the downstream task invalid
        /// - Synchronize an existing valid task with a new task, leaves the existing task valid and the new task invalid
        /// - Synchronize a new task with an existing valid task, leaves the new task invalid and sets the existing task invalid
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test synchronizes two task in various combinations and validates the model")]
        public void TestSynchronizeTasks()
        {
            InitializeModel("t,t1,t2,t3,t11,t12,t13,t21,t22,t23,t31,t32");

            Assert.IsTrue(!_t11.ValidityState, "Task 11 is valid when it should be invalid");
            Assert.IsTrue(!_t12.ValidityState, "Task 12 is valid when it should be invalid");
            Assert.IsTrue(!_t13.ValidityState, "Task 13 is valid when it should be invalid");
            Assert.IsTrue(!_t21.ValidityState, "Task 21 is valid when it should be invalid");
            Assert.IsTrue(!_t22.ValidityState, "Task 22 is valid when it should be invalid");
            Assert.IsTrue(!_t23.ValidityState, "Task 23 is valid when it should be invalid");
            Assert.IsTrue(!_t31.ValidityState, "Task 31 is valid when it should be invalid");
            Assert.IsTrue(!_t32.ValidityState, "Task 32 is valid when it should be invalid");

            _model.Start();

            Assert.IsTrue(_t11.ValidityState, "Task 11 is invalid when it should be valid");
            Assert.IsTrue(_t12.ValidityState, "Task 12 is invalid when it should be valid");
            Assert.IsTrue(_t13.ValidityState, "Task 13 is invalid when it should be valid");
            Assert.IsTrue(_t21.ValidityState, "Task 21 is invalid when it should be valid");
            Assert.IsTrue(_t22.ValidityState, "Task 22 is invalid when it should be valid");
            Assert.IsTrue(_t23.ValidityState, "Task 23 is invalid when it should be valid");
            Assert.IsTrue(_t31.ValidityState, "Task 31 is invalid when it should be valid");
            Assert.IsTrue(_t32.ValidityState, "Task 32 is invalid when it should be valid");


            // Synchronize two valid tasks from two different task lists
            Synchronize(_t12, _t32);
            Assert.IsTrue(!_t12.ValidityState, "Task 12 is still valid even after being synchronized with Task 32");
            Assert.IsTrue(!_t32.ValidityState, "Task 32 is still valid even after being synchronized with Task 12");

            // Create new tasks with task list
            TestTask nt = new TestTask(_model, "New Task");
            _t.AddChildEdge(nt);
            TestTask nt1 = new TestTask(_model, "New Task 1");
            TestTask nt2 = new TestTask(_model, "New Task 2");
            TestTask nt3 = new TestTask(_model, "New Task 3");
            _tLnew = new TaskList(_model, "Task new 1 Children", Guid.NewGuid(), nt);
            _tLnew.AppendTask(nt1);
            _tLnew.AppendTask(nt2);
            _tLnew.AppendTask(nt3);


            // Sychnronize a new task before a validated task - it should invalidate the following task.
            Assert.IsTrue(!nt3.ValidityState, "New Task is valid when it should be invalid");
            Assert.IsTrue(_t21.ValidityState, "Task 21 is invalid when it should be valid");
            Synchronize(nt3, _t21);
            Assert.IsTrue(!nt3.ValidityState, "New Task is valid when it should be invalid");
            Assert.IsTrue(!_t21.ValidityState, "Task 21 is valid when it should be invalid");

            // Sychnronize a validated task with a new task
            Assert.IsTrue(_t11.ValidityState, "Task 11 is invalid when it should be valid");
            Assert.IsTrue(!nt2.ValidityState, "New Task is valid when it should be invalid");
            Synchronize(_t11, nt2);
            Assert.IsTrue(!_t11.ValidityState, "Task 11 is valid when it should be invalid");
            Assert.IsTrue(!nt2.ValidityState, "New Task is valid when it should be invalid");

            // Sychnronize a new task after a validated task - it should NOT invalidate the preceding task.
            Assert.IsTrue(!nt1.ValidityState, "New Task is valid when it should be invalid");
            Assert.IsTrue(_t31.ValidityState, "Task 31 is invalid when it should be valid");
            Synchronize(_t31, nt1);
            Assert.IsTrue(!nt1.ValidityState, "New Task is valid when it should be invalid");
            Assert.IsTrue(!_t31.ValidityState, "Task 31 is valid when it should be invalid");

            Validate("Test 2");

            Assert.IsTrue(_t12.ValidityState, "Task 12 is not valid after being validated");
            Assert.IsTrue(_t13.ValidityState, "Task 13 is not valid after being validated");
            Assert.IsTrue(_t21.ValidityState, "Task 21 is not valid after being validated");
            Assert.IsTrue(_t22.ValidityState, "Task 22 is not valid after being validated");
            Assert.IsTrue(_t23.ValidityState, "Task 23 is not valid after being validated");
            Assert.IsTrue(_t32.ValidityState, "Task 32 is not valid after being validated");
            Assert.IsTrue(nt.ValidityState, "New Task is not valid after being validated");
            Assert.IsTrue(nt1.ValidityState, "New Task 1 is not valid after being validated");
            Assert.IsTrue(nt2.ValidityState, "New Task 2 is not valid after being validated");

        }


        /// <summary>
        /// - Synchronize two parents and two first children under them.
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test synchronizes two task in two levels and validates the model")]
        public void TestSynchronizeTasksInTwoLevels()
        {
            InitializeModel("t,t1,t2,t3,t11,t12,t21,t22,t31,t32");

            Assert.IsTrue(!_t11.ValidityState, "Task 11 is valid when it should be invalid");
            Assert.IsTrue(!_t12.ValidityState, "Task 12 is valid when it should be invalid");
            Assert.IsTrue(!_t21.ValidityState, "Task 21 is valid when it should be invalid");
            Assert.IsTrue(!_t22.ValidityState, "Task 22 is valid when it should be invalid");
            Assert.IsTrue(!_t31.ValidityState, "Task 31 is valid when it should be invalid");
            Assert.IsTrue(!_t32.ValidityState, "Task 32 is valid when it should be invalid");

            _model.Start();

            Assert.IsTrue(_t11.ValidityState, "Task 11 is invalid when it should be valid");
            Assert.IsTrue(_t12.ValidityState, "Task 12 is invalid when it should be valid");
            Assert.IsTrue(_t21.ValidityState, "Task 21 is invalid when it should be valid");
            Assert.IsTrue(_t22.ValidityState, "Task 22 is invalid when it should be valid");
            Assert.IsTrue(_t31.ValidityState, "Task 31 is invalid when it should be valid");
            Assert.IsTrue(_t32.ValidityState, "Task 32 is invalid when it should be valid");


            Synchronize(_t1, _t2, _t3);
            Console.WriteLine(Diagnostics.DiagnosticAids.ReportOnTaskValidity(_t, true));

            Synchronize(_t11, _t21, _t31);
            Console.WriteLine(Diagnostics.DiagnosticAids.ReportOnTaskValidity(_t, true));

            Validate("Test 4");

            Assert.IsTrue(_t11.ValidityState, "Task 11 is not valid after being validated");
            Assert.IsTrue(_t12.ValidityState, "Task 12 is not valid after being validated");
            Assert.IsTrue(_t21.ValidityState, "Task 21 is not valid after being validated");
            Assert.IsTrue(_t22.ValidityState, "Task 22 is not valid after being validated");
            Assert.IsTrue(_t31.ValidityState, "Task 31 is not valid after being validated");
            Assert.IsTrue(_t32.ValidityState, "Task 32 is not valid after being validated");

        }


        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test synchronizes two task and adds tasks to the synchronized tasks.")]
        public void TestSynchronizeAndAddTasks()
        {
            InitializeModel("t,t1,t2,t3,t12,t21,t23,t31,t32");

            Assert.IsTrue(!_t12.ValidityState, "Task 12 is valid when it should be invalid");
            Assert.IsTrue(!_t21.ValidityState, "Task 21 is valid when it should be invalid");
            Assert.IsTrue(!_t23.ValidityState, "Task 23 is valid when it should be invalid");
            Assert.IsTrue(!_t31.ValidityState, "Task 31 is valid when it should be invalid");
            Assert.IsTrue(!_t32.ValidityState, "Task 32 is valid when it should be invalid");

            _model.Start();

            Assert.IsTrue(_t12.ValidityState, "Task 12 is invalid when it should be valid");
            Assert.IsTrue(_t21.ValidityState, "Task 21 is invalid when it should be valid");
            Assert.IsTrue(_t23.ValidityState, "Task 23 is invalid when it should be valid");
            Assert.IsTrue(_t31.ValidityState, "Task 31 is invalid when it should be valid");
            Assert.IsTrue(_t32.ValidityState, "Task 32 is invalid when it should be valid");

            Synchronize(_t12, _t32);

            Assert.IsTrue(!_t12.ValidityState, "Task 12 is valid even after being synchronized with Task 32");
            Assert.IsTrue(!_t32.ValidityState, "Task 32 is valid even after being synchronized with Task 12");


            Validate("Test 3a");

            // Add task before; invalidates both task
            // in addition; because task t12 and t32 are synchronized t32 turns invalid as well
            _tL1.AddTaskBefore(_t12, _t11);
            Assert.IsTrue(!_t12.ValidityState, "Task 12 is valid when it should be invalid");
            Assert.IsTrue(!_t11.ValidityState, "Task 11 is valid when it should be invalid");
            // t32 turns invalid, because t12 turns invalid and t32 is synchronized with t12
            Assert.IsTrue(!_t32.ValidityState, "Task 32 is valid when it should be invalid");

            Validate("Test 3b");
            _t13.SelfValidState = true;

            // Add task after; leaves upstream task valid and sets downstream task invalid
            Assert.IsTrue(_t12.ValidityState, "Task 12 is valid when it should be invalid");
            Assert.IsTrue(_t13.ValidityState, "Task 13 is invalid after being validated");
            _tL1.AddTaskAfter(_t12, _t13);

            Assert.IsTrue(_t12.ValidityState, "Task 12 is invalid when it should be valid");
            Assert.IsTrue(!_t13.ValidityState, "Task 13 is valid when it should be invalid");

            Validate("Test 3c");
            _t22.SelfValidState = true;

            // Add task after; leaves upstream taks valid, but sets downstream task invalid
            Assert.IsTrue(_t21.ValidityState, "Task 21 is invalid after being validated");
            Assert.IsTrue(_t22.ValidityState, "Task 22 is invalid after being validated");
            _tL2.AddTaskAfter(_t21, _t22);
            Assert.IsTrue(_t21.ValidityState, "Task 21 is invalid when it should be valid");
            Assert.IsTrue(!_t22.ValidityState, "Task 22 is valid when it should be invalid");


            Validate("Test 3d");

            Assert.IsTrue(_t11.ValidityState, "Task 11 is not valid after being validated");
            Assert.IsTrue(_t12.ValidityState, "Task 12 is not valid after being validated");
            Assert.IsTrue(_t13.ValidityState, "Task 13 is not valid after being validated");
            Assert.IsTrue(_t21.ValidityState, "Task 21 is not valid after being validated");
            Assert.IsTrue(_t22.ValidityState, "Task 22 is not valid after being validated");
            Assert.IsTrue(_t32.ValidityState, "Task 32 is not valid after being validated");

        }

        /// <summary>
        /// - Remove a downstream task in a task list leaves all upstream tasks unchanged
        /// - Remove an upstream task in a task list and all downstream tasks will be set invalid
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test removes upstream and downstream tasks in a task list")]
        public void TestRemoveTasks()
        {
            InitializeModel("t,t1,t2,t3,t11,t12,t13,t21,t22,t23,t32");

            Assert.IsTrue(!_t11.ValidityState, "Task 11 is valid when it should be invalid");
            Assert.IsTrue(!_t12.ValidityState, "Task 12 is valid when it should be invalid");
            Assert.IsTrue(!_t13.ValidityState, "Task 13 is valid when it should be invalid");
            Assert.IsTrue(!_t21.ValidityState, "Task 21 is valid when it should be invalid");
            Assert.IsTrue(!_t23.ValidityState, "Task 23 is valid when it should be invalid");

            _model.Start();

            Assert.IsTrue(_t11.ValidityState, "Task 11 is invalid when it should be valid");
            Assert.IsTrue(_t12.ValidityState, "Task 12 is invalid when it should be valid");
            Assert.IsTrue(_t13.ValidityState, "Task 13 is invalid when it should be valid");
            Assert.IsTrue(_t21.ValidityState, "Task 21 is invalid when it should be valid");
            Assert.IsTrue(_t23.ValidityState, "Task 23 is invalid when it should be valid");

            // Remove upstream task in a task list, sets all downstream task invalid
            _tL1.RemoveTask(_t11);
            Assert.IsTrue(!_t12.ValidityState, "Task 12 is valid when it should be invalid");
            Assert.IsTrue(!_t13.ValidityState, "Task 13 is valid when it should be invalid");


            // Remove downstream task, leaves all upstream tasks unchanged
            _tL2.RemoveTask(_t23);
            Assert.IsTrue(_t21.ValidityState, "Task 21 is invalid when it should be valid");
            Assert.IsTrue(_t22.ValidityState, "Task 22 is invalid when it should be valid");
            Assert.IsTrue(!_t23.ValidityState, "Task 23 is valid when it should be invalid");
            Assert.IsTrue(_t32.ValidityState, "Task 32 is invalid when it should be valid");


            Validate("Test 1");

            Assert.IsTrue(!_t11.ValidityState, "Task 11 is valid when it should be invalid");
            Assert.IsTrue(_t12.ValidityState, "Task 12 is invalid when it should be valid");
            Assert.IsTrue(_t13.ValidityState, "Task 13 is invalid when it should be valid");
            Assert.IsTrue(_t21.ValidityState, "Task 21 is invalid when it should be valid");
            Assert.IsTrue(_t22.ValidityState, "Task 22 is invalid when it should be valid");
            Assert.IsTrue(!_t23.ValidityState, "Task 23 is valid when it should be invalid");
            Assert.IsTrue(_t32.ValidityState, "Task 32 is invalid when it should be valid");

        }

        /// <summary>
        /// - Remove a downstream task in a task list leaves all upstream tasks unchanged
        /// - Remove an upstream task in a task list and all downstream tasks will be set invalid
        /// </summary>
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("This test rmoves upstream and downstrem tasks in a task list")]
        public void TestSynchronizeAndRemoveTasks()
        {
            InitializeModel("t,t1,t2,t3,t11,t12,t13,t21,t22,t23,t32");

            Assert.IsTrue(!_t11.ValidityState, "Task 11 is valid when it should be invalid");
            Assert.IsTrue(!_t12.ValidityState, "Task 12 is valid when it should be invalid");
            Assert.IsTrue(!_t13.ValidityState, "Task 13 is valid when it should be invalid");
            Assert.IsTrue(!_t21.ValidityState, "Task 21 is valid when it should be invalid");
            Assert.IsTrue(!_t22.ValidityState, "Task 22 is valid when it should be invalid");
            Assert.IsTrue(!_t32.ValidityState, "Task 32 is valid when it should be invalid");

            _model.Start();

            Assert.IsTrue(_t11.ValidityState, "Task 11 is invalid when it should be valid");
            Assert.IsTrue(_t21.ValidityState, "Task 21 is invalid when it should be valid");
            Assert.IsTrue(_t22.ValidityState, "Task 22 is invalid when it should be valid");
            Assert.IsTrue(_t32.ValidityState, "Task 32 is invalid when it should be valid");

            // Remove a task invalidates all downstream tasks in the same task list 
            //   and also all tasks synchronized with a downstream task.
            Synchronize(_t11, _t21);
            Synchronize(_t22, _t32);
            Validate("Test 1");         // validate all tasks
            _tL2.RemoveTask(_t21);
            Assert.IsTrue(_t11.ValidityState, "Task 11 is invalid when it should be valid");
            // AEL, following is a bug. It validates to true when it should be invalid
            //System.Diagnostics.Debug.Assert(!t21.ValidityState, "Task 21 is valid when it should be invalid");
            Assert.IsTrue(!_t22.ValidityState, "Task 22 is valid when it should be invalid");
            // AEL, following is a bug. It validates to true when it should be invalid
            //System.Diagnostics.Debug.Assert(!t32.ValidityState, "Task 32 is valid when it should be invalid");


            Validate("Test 1");         // validate all tasks

            // Remove the upstream task of a synchronization, the downstream task will be set invalid
            Synchronize(_t12, _t23);
            _tL1.RemoveTask(_t12);
            Assert.IsTrue(!_t12.ValidityState, "Task 12 is valid when it should be invalid");
            Assert.IsTrue(!_t13.ValidityState, "Task 13 is valid when it should be invalid");
            // AEL, following is a bug. It validates to true when it should be invalid
            //System.Diagnostics.Debug.Assert(!t23.ValidityState, "Task 23 is valid when it should be invalid");
            Assert.IsTrue(_t32.ValidityState, "Task 32 is invalid when it should be valid");

            Validate("Test 1");

            Assert.IsTrue(_t11.ValidityState, "Task 11 is invalid when it should be valid");
            Assert.IsTrue(!_t12.ValidityState, "Task 12 is valid when it should be invalid");
            Assert.IsTrue(_t13.ValidityState, "Task 13 is invalid when it should be valid");
            Assert.IsTrue(!_t21.ValidityState, "Task 21 is valid when it should be invalid");
            Assert.IsTrue(_t22.ValidityState, "Task 22 is invalid when it should be valid");
            Assert.IsTrue(_t23.ValidityState, "Task 23 is invalid when it should be valid");
            Assert.IsTrue(_t32.ValidityState, "Task 32 is invalid when it should be valid");

        }

        // Obsolete tests ???
        //		public void TestValidationWithReconfiguration(){
        //
        //			InitializeModel("t,t1,t2,t3,t11,t12,t13,t21,t22,t23,t31,t32,t33");
        //			Synchronize(t11,t21);
        //			Synchronize(t22,t32);
        //			tL1.RemoveTask(t11);
        //			Validate("Test 6");
        //
        //			InitializeModel("t,t1,t2,t3,t11,t12,t13,t21,t22,t23,t31,t32,t33");
        //			Synchronize(t11,t21);
        //			Synchronize(t22,t32);
        //			tL2.RemoveTask(t22);
        //			Validate("Test 7");
        //
        //			InitializeModel("t,t1,t2,t3,t11,t12,t13,t21,t22,t23,t31,t32,t33");
        //			Synchronize(t11,t21);
        //			Synchronize(t22,t32);
        //			tL3.RemoveTask(t33);
        //			Validate("Test 8");
        //
        //
        //
        //		}

        //		private void PerformRandomTest(){
        //			// 1. Create an arbitrary graph
        //
        //			// 2. Establish synchronizers.
        //
        //			// 3. Validate.
        //
        //			// 4. Remove 0..all synchronizers.
        //
        //			// 5. Remove 0..all tasks.
        //
        //			// 6. Add 0..N tasks.
        //
        //			// 7. Add 0..N Synchronizers.
        //
        //
        //		}


        // Not sure if we still need this test since I have done extensive testing above
        //		[TestMethod] public void TestValidation(){
        //
        //			InitializeModel("t,t1,t2,t3,t12,t13,t21,t23,t31,t32");												// AEL
        //
        //			Debug.WriteLine(Diagnostics.DiagnosticAids.GraphToString(t));
        //
        //			new TaskProcessor(model,"Main",t); // Adds it into the model.
        //
        //			/*			Debug.WriteLine("Running the model - press enter to continue...");Trace.ReadLine();
        //						model.Start();
        //						Debug.WriteLine(Diagnostics.DiagnosticAids.ReportOnTaskValidity(t,true));
        //
        //						Debug.WriteLine("Invalidating Task 1.1 - press enter to continue...");Trace.ReadLine();
        //						t11.SelfValidState = false;
        //						Debug.WriteLine(Diagnostics.DiagnosticAids.ReportOnTaskValidity(t,true));
        //
        //						Debug.WriteLine("Revalidating Task 1.1 - press enter to continue...");Trace.ReadLine();
        //						t11.SelfValidState = true;
        //						Debug.WriteLine(Diagnostics.DiagnosticAids.ReportOnTaskValidity(t,true));
        //			*/
        //			//Debug.WriteLine("Adding third child task & vs - press enter to continue...");Trace.ReadLine();
        //			t.AddChildEdge(t3);
        //
        //			new VertexSynchronizer(model.Executive,new Vertex[]{t22.PreVertex,t31.PreVertex},ExecEventType.Detachable);
        //			model.Start();
        //			Debug.WriteLine(Diagnostics.DiagnosticAids.ReportOnTaskValidity(t,true));
        //
        //			//Debug.WriteLine("Invalidating Task t21 - press enter to continue...");Trace.ReadLine();
        //			t21.SelfValidState = false;
        //			Debug.WriteLine(Diagnostics.DiagnosticAids.ReportOnTaskValidity(t,true));
        //
        //			//Debug.WriteLine("Revalidating Task t21 - press enter to continue...");Trace.ReadLine();
        //			t21.SelfValidState = true;
        //			Debug.WriteLine(Diagnostics.DiagnosticAids.ReportOnTaskValidity(t,true));
        //
        //			//Debug.WriteLine("Adding Tasks t13 and t23 - press enter to continue...");Trace.ReadLine();
        //			tL1.AppendTask(t13);
        //			tL2.AppendTask(t23);
        //			Debug.WriteLine(Diagnostics.DiagnosticAids.ReportOnTaskValidity(t,true));
        //			model.Start();
        //
        //			Debug.WriteLine("Removing Task t21 - press enter to continue...");Trace.ReadLine();
        //			tL2.RemoveTask(t21);
        //			//t21.SelfValidState = true;
        //			Debug.WriteLine(Diagnostics.DiagnosticAids.ReportOnTaskValidity(t,true));
        //
        //
        //			Debug.WriteLine("Running the model - press enter to continue...");Trace.ReadLine();
        //			model.Start();
        //			Debug.WriteLine(Diagnostics.DiagnosticAids.ReportOnTaskValidity(t,true));
        //
        //		}


        private void InitializeModel()
        {
            InitializeModel("t,t1,t2,t3,t11,t12,t13,t21,t22,t23,t31,t32,t33");
        }
        private void InitializeModel(string includees)
        {
            _model = new Model("Test Model");
            _model.AddService<ITaskManagementService>(new TaskManagementService());
            _t = new TestTask(_model, "Top Task");
            _t1 = new TestTask(_model, "Task 1");
            _t2 = new TestTask(_model, "Task 2");
            _t3 = new TestTask(_model, "Task 3");
            _t11 = new TestTask(_model, "Task 1.1");
            _t12 = new TestTask(_model, "Task 1.2");
            _t13 = new TestTask(_model, "Task 1.3");
            _t21 = new TestTask(_model, "Task 2.1");
            _t22 = new TestTask(_model, "Task 2.2");
            _t23 = new TestTask(_model, "Task 2.3");
            _t31 = new TestTask(_model, "Task 3.1");
            _t32 = new TestTask(_model, "Task 3.2");
            _t33 = new TestTask(_model, "Task 3.3");

            _tp = new TaskProcessor(_model, "TP1", Guid.NewGuid(), _t);

            if (include("t1", includees))
                _t.AddChildEdge(_t1);
            if (include("t2", includees))
                _t.AddChildEdge(_t2);
            if (include("t3", includees))
                _t.AddChildEdge(_t3);

            _tL1 = new TaskList(_model, "Task 1 Children", Guid.NewGuid(), _t1);
            _tL2 = new TaskList(_model, "Task 2 Children", Guid.NewGuid(), _t2);
            _tL3 = new TaskList(_model, "Task 3 Children", Guid.NewGuid(), _t3);

            if (include("t11", includees))
                _tL1.AppendTask(_t11);
            if (include("t12", includees))
                _tL1.AppendTask(_t12);
            if (include("t13", includees))
                _tL1.AppendTask(_t13);
            if (include("t21", includees))
                _tL2.AppendTask(_t21);
            if (include("t22", includees))
                _tL2.AppendTask(_t22);
            if (include("t23", includees))
                _tL2.AppendTask(_t23);
            if (include("t31", includees))
                _tL3.AppendTask(_t31);
            if (include("t32", includees))
                _tL3.AppendTask(_t32);
            if (include("t33", includees))
                _tL3.AppendTask(_t33);

            Highpoint.Sage.Graphs.Validity.ValidationService vm = new Highpoint.Sage.Graphs.Validity.ValidationService(_t);

            //			model.Start();  AEL: I want control in the tests over when the model is started/validated.

        }

        private void Synchronize(Task t1, Task t2)
        {
            new VertexSynchronizer(_model.Executive, new Vertex[] { t1.PreVertex, t2.PreVertex }, ExecEventType.Synchronous);
        }

        private void Synchronize(Task t1, Task t2, Task t3)
        {
            new VertexSynchronizer(_model.Executive, new Vertex[] { t1.PreVertex, t2.PreVertex, t3.PreVertex }, ExecEventType.Synchronous);
        }

        private void Validate(string testName)
        {
            // AEL, following two lines are just to fix the problem temporarily
            _model.StateMachine.DoTransition(_model.GetIdleEnum());
            _model.Executive.Reset();

            if (VERBOSE)
                Debug.WriteLine(testName + " pre-execution state");
            if (VERBOSE)
                Debug.WriteLine(Diagnostics.DiagnosticAids.ReportOnTaskValidity(_t, true));
            _model.Start();
            if (VERBOSE)
                Debug.WriteLine(testName + " post-execution state");
            if (VERBOSE)
                Debug.WriteLine(Diagnostics.DiagnosticAids.ReportOnTaskValidity(_t, true));
            Assert.IsTrue(_t.ValidityState, testName + " failed.");
        }

        private bool include(string candidate, string includees)
        {
            string[] includeeArray = includees.Split(',');
            foreach (string x in includeeArray)
                if (x.Equals(candidate, StringComparison.Ordinal))
                    return true;
            return false;
        }

        class TestTask : Highpoint.Sage.Graphs.Tasks.Task
        {
            private bool _svs = true;
            public TestTask(IModel model, string name) : base(model, name, Guid.NewGuid())
            {
                this.EdgeExecutionStartingEvent += new EdgeEvent(OnTaskBeginning);
                this.EdgeExecutionFinishingEvent += new EdgeEvent(OnTaskCompleting);
            }

            protected override void DoTask(IDictionary graphContext)
            {
                SelfValidState = _svs;
                SignalTaskCompletion(graphContext);
            }

            private void OnTaskBeginning(IDictionary graphContext, Edge edge)
            {
                if (VERBOSE)
                    Debug.WriteLine(Model.Executive.Now + " : " + Name + " is beginning.");
            }

            private void OnTaskCompleting(IDictionary graphContext, Edge edge)
            {
                if (VERBOSE)
                    Debug.WriteLine(Model.Executive.Now + " : " + Name + " is completing.");
            }
        }
    }
}

