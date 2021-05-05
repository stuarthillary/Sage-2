/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.SimCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

namespace Highpoint.Sage.Resources
{

    [TestClass]
    public class ResourceTesterExt
    {

        public ResourceTesterExt()
        {
            Init();
        }

        // TODO: Acquire and Reserve methods that take just the boolean arg.
        // TODO: Be more performance-sensitive when adding resource requests to
        //       a resource manager, in assuming that the collection is now dirty.

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

        private PriRscReqTester _prt;
        private static string _resultString;


        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("First functional test of Resource Tester Infrastructural Class")]
        public void TestBasicFuctionality()
        {
            #region Expected Result
            string expected = @"[User_0,Req,01/01/0001 12:05:00 AM,0]
[User_1,Req,01/01/0001 12:05:00 AM,0]
[User_2,Req,01/01/0001 12:05:00 AM,0]
[User_3,Req,01/01/0001 12:05:00 AM,0]
[User_4,Req,01/01/0001 12:05:00 AM,0]
[User_0,Acq,01/01/0001 12:10:00 AM]
[User_0,Rls,01/01/0001 12:15:00 AM]
[User_1,Acq,01/01/0001 12:15:00 AM]
[User_1,Rls,01/01/0001 12:20:00 AM]
[User_2,Acq,01/01/0001 12:20:00 AM]
[User_2,Rls,01/01/0001 12:25:00 AM]
[User_3,Acq,01/01/0001 12:25:00 AM]
[User_3,Rls,01/01/0001 12:30:00 AM]
[User_4,Acq,01/01/0001 12:30:00 AM]
[User_4,Rls,01/01/0001 12:35:00 AM]";
            #endregion

            _resultString = "";
            new PriRscReqTester(5).Start();
            //Console.WriteLine(m_resultString);

            Assert.IsTrue(StripCRLF(_resultString).Equals(StripCRLF(expected), StringComparison.Ordinal), "TestPrioritizedResourceRequestWRemoval_2", "Results didn't match!");
        }


        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("First functional test of Resource Tester Infrastructural Class")]
        public void TestPrioritizedResourceRequestHandling()
        {
            #region Expected Result
            string expected = @"[User_0,Req,01/01/0001 12:05:00 AM,0]
[User_1,Req,01/01/0001 12:05:00 AM,1]
[User_2,Req,01/01/0001 12:05:00 AM,2]
[User_3,Req,01/01/0001 12:05:00 AM,3]
[User_4,Req,01/01/0001 12:05:00 AM,4]
[User_4,Acq,01/01/0001 12:10:00 AM]
[User_4,Rls,01/01/0001 12:15:00 AM]
[User_3,Acq,01/01/0001 12:15:00 AM]
[User_3,Rls,01/01/0001 12:20:00 AM]
[User_2,Acq,01/01/0001 12:20:00 AM]
[User_2,Rls,01/01/0001 12:25:00 AM]
[User_1,Acq,01/01/0001 12:25:00 AM]
[User_1,Rls,01/01/0001 12:30:00 AM]
[User_0,Acq,01/01/0001 12:30:00 AM]
[User_0,Rls,01/01/0001 12:35:00 AM]
";
            #endregion

            _prt = new PriRscReqTester(5);
            for (int i = 0; i < 5; i++)
            {
                ResourceUser ru = _prt.RscUsers[i];
                Console.WriteLine("Changing " + ru.Name + "'s priority to " + i);
                ru.ResourceRequest.Priority = i;
            }
            _resultString = "";
            _prt.Start();
            //Console.WriteLine(m_resultString);

            Assert.IsTrue(StripCRLF(_resultString).Equals(StripCRLF(expected), StringComparison.Ordinal), "TestPrioritizedResourceRequestWRemoval_2", "Results didn't match!");
        }
        private string StripCRLF(string structureString) => structureString.Replace("\r", "", StringComparison.Ordinal).Replace("\n", "", StringComparison.Ordinal);

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("First functional test of Resource Tester Infrastructural Class")]
        public void TestPrioritizedResourceRequestWRemoval_1()
        {
            #region Expected Result
            string expected = @"[User_0,Req,01/01/0001 12:05:00 AM,0]
[User_1,Req,01/01/0001 12:05:00 AM,1]
[User_2,Req,01/01/0001 12:05:00 AM,2]
[User_3,Req,01/01/0001 12:05:00 AM,3]
[User_4,Req,01/01/0001 12:05:00 AM,4]
[User_4,Acq,01/01/0001 12:10:00 AM]
[User_4,Rls,01/01/0001 12:15:00 AM]
[User_2,Acq,01/01/0001 12:15:00 AM]
[User_2,Rls,01/01/0001 12:20:00 AM]
[User_3,Acq,01/01/0001 12:20:00 AM]
[User_3,Rls,01/01/0001 12:25:00 AM]
[User_1,Acq,01/01/0001 12:25:00 AM]
[User_1,Rls,01/01/0001 12:30:00 AM]
[User_0,Acq,01/01/0001 12:30:00 AM]
[User_0,Rls,01/01/0001 12:35:00 AM]
";
            #endregion
            _prt = new PriRscReqTester(5);
            for (int i = 0; i < 5; i++)
            {
                ResourceUser ru = _prt.RscUsers[i];
                Console.WriteLine("Changing " + ru.Name + "'s priority to " + i);
                ru.ResourceRequest.Priority = i;
            }
            _prt.Model.Starting += new ModelEvent(Model_Starting);
            _resultString = "";
            _prt.Start();
            //Console.WriteLine(m_resultString);

            Assert.IsTrue(StripCRLF(_resultString).Equals(StripCRLF(expected), StringComparison.Ordinal), "TestPrioritizedResourceRequestWRemoval_2", "Results didn't match!");
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("First functional test of Resource Tester Infrastructural Class")]
        public void TestPrioritizedResourceRequestWRemoval_2()
        {
            #region Expected Result
            string expected = @"[User_0,Req,01/01/0001 12:05:00 AM,0]
[User_1,Req,01/01/0001 12:05:00 AM,0]
[User_2,Req,01/01/0001 12:05:00 AM,0]
[User_3,Req,01/01/0001 12:05:00 AM,0]
[User_4,Req,01/01/0001 12:05:00 AM,0]
[User_0,Acq,01/01/0001 12:10:00 AM]
[User_0,Rls,01/01/0001 12:15:00 AM]
[User_2,Acq,01/01/0001 12:15:00 AM]
[User_2,Rls,01/01/0001 12:20:00 AM]
[User_1,Acq,01/01/0001 12:20:00 AM]
[User_1,Rls,01/01/0001 12:25:00 AM]
[User_3,Acq,01/01/0001 12:25:00 AM]
[User_3,Rls,01/01/0001 12:30:00 AM]
[User_4,Acq,01/01/0001 12:30:00 AM]
[User_4,Rls,01/01/0001 12:35:00 AM]
";
            #endregion
            _prt = new PriRscReqTester(5);
            _prt.Model.Starting += new ModelEvent(Model_Starting);
            _resultString = "";
            _prt.Start();
            //Console.WriteLine(m_resultString);
            Assert.IsTrue(StripCRLF(_resultString).Equals(StripCRLF(expected), StringComparison.Ordinal), "TestPrioritizedResourceRequestWRemoval_2", "Results didn't match!");
        }

        private void Model_Starting(IModel theModel)
        {
            ExecEventReceiver eer = new ExecEventReceiver(AdjustPriority);
            DateTime when = theModel.Executive.Now + TimeSpan.FromMinutes(15);
            theModel.Executive.RequestEvent(eer, when, 0.0, null, ExecEventType.Synchronous);
        }
        private void AdjustPriority(IExecutive exec, object userData)
        {
            double newPri = 12.0;
            ResourceUser ru = _prt.RscUsers[2];
            Console.WriteLine(exec.Now + " : *** Adjusting priority of " + ru.Name + " to " + newPri + ".");
            ru.ResourceRequest.Priority = newPri;
        }

        #region Support Classes 
        class PriRscReqTester
        {
            private readonly ResourceUser[] _users;
            private readonly SelfManagingResource _smr;
            private readonly IModel _model = null;
            private IResourceRequest _rscReq;

            public PriRscReqTester(int nUsers)
            {

                _model = new Model("Resource Testing Model...");

                _smr = new SelfManagingResource(_model, "SMR", Guid.NewGuid(), 1.0, 1.0, true, true, true, true);

                _users = new ResourceUser[nUsers];

                for (int i = 0; i < nUsers; i++)
                {
                    _users[i] = new ResourceUser(_model, "User_" + i, Guid.NewGuid(), _smr);
                }
                _model.Starting += new ModelEvent(AcqireResource);
            }

            public void Start()
            {
                _model.Start();
            }

            #region Member Accessors
            public ResourceUser[] RscUsers
            {
                get
                {
                    return _users;
                }
            }
            public SelfManagingResource SMR
            {
                get
                {
                    return _smr;
                }
            }
            public IModel Model
            {
                get
                {
                    return _model;
                }
            }
            #endregion

            private void AcqireResource(IModel theModel)
            {
                Console.WriteLine("Acquiring the resource at " + theModel.Executive.Now);
                _rscReq = new SimpleResourceRequest(1.0, _smr);
                _smr.Acquire(_rscReq, false);

                ExecEventReceiver eer = new ExecEventReceiver(ReleaseResource);
                DateTime when = theModel.Executive.Now + TimeSpan.FromMinutes(10.0);
                double priority = 0.0;
                ExecEventType eet = ExecEventType.Synchronous;
                theModel.Executive.RequestEvent(eer, when, priority, null, eet);
            }

            private void ReleaseResource(IExecutive exec, object userData)
            {
                Console.WriteLine("Releasing the resource at " + getExecNow(exec));
                _rscReq.Release();
            }

            private string getExecNow(IExecutive exec)
            {
                return exec.Now.ToString("dd/MM/yyyy HH:mm:ss tt");
            }
        }

        class ResourceUser : IModelObject
        {
            private readonly IResourceRequest _irr;
            public ResourceUser(IModel model, string name, Guid guid, SelfManagingResource smr)
            {
                InitializeIdentity(model, name, null, guid);

                _irr = new SimpleResourceRequest(1.0, smr);
                _model.Starting += new ModelEvent(ScheduleMyResourceAction);

                IMOHelper.RegisterWithModel(this);
            }

            public IResourceRequest ResourceRequest
            {
                get
                {
                    return _irr;
                }
            }

            private void ScheduleMyResourceAction(IModel theModel)
            {
                ExecEventReceiver eer = new ExecEventReceiver(DoResourceAction);
                DateTime when = theModel.Executive.Now + TimeSpan.FromMinutes(5.0);
                double priority = 0.0;
                ExecEventType eet = ExecEventType.Detachable;
                theModel.Executive.RequestEvent(eer, when, priority, null, eet);
            }

            private void DoResourceAction(IExecutive exec, object obj)
            {
                _resultString += ("[" + this.Name + ",Req," + getExecNow(exec) + "," + _irr.Priority + "]\r\n");
                Console.WriteLine("At time " + getExecNow(exec) + ", " + _name + " trying to acquire with a priority of " + _irr.Priority);
                _irr.Acquire(_irr.DefaultResourceManager, true);
                _resultString += ("[" + this.Name + ",Acq," + getExecNow(exec) + "]\r\n");
                Console.WriteLine("At time " + getExecNow(exec) + ", " + _name + " acquired...");
                exec.CurrentEventController.SuspendUntil(exec.Now + TimeSpan.FromMinutes(5.0));
                Console.WriteLine("At time " + getExecNow(exec) + ", " + _name + " releasing...");
                _resultString += ("[" + this.Name + ",Rls," + getExecNow(exec) + "]\r\n");
                _irr.Release();
                Console.WriteLine("...and release is done.");

            }

            private string getExecNow(IExecutive exec)
            {
                return exec.Now.ToString("dd/MM/yyyy hh:mm:ss tt");
            }

            #region Implementation of IModelObject
            private string _name = null;
            public string Name
            {
                get
                {
                    return _name;
                }
            }
            private string _description = null;
            /// <summary>
            /// A description of this Resource User.
            /// </summary>
            public string Description
            {
                get
                {
                    return _description ?? _name;
                }
            }
            private Guid _guid = Guid.Empty;
            public Guid Guid
            {
                get
                {
                    return _guid;
                }
            }
            private IModel _model = null;
            public IModel Model
            {
                get
                {
                    return _model;
                }
            }
            /// <summary>
            /// Initializes the fields that feed the properties of this IModelObject identity.
            /// </summary>
            /// <param name="model">The IModelObject's new model value.</param>
            /// <param name="name">The IModelObject's new name value.</param>
            /// <param name="description">The IModelObject's new description value.</param>
            /// <param name="guid">The IModelObject's new GUID value.</param>
            public void InitializeIdentity(IModel model, string name, string description, Guid guid)
            {
                IMOHelper.Initialize(ref _model, model, ref _name, name, ref _description, description, ref _guid, guid);
            }

            #endregion
        }

        #endregion

    }
}
