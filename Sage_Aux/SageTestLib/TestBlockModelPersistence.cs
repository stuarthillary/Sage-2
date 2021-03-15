/* This source code licensed under the GNU Affero General Public License */


using Highpoint.Sage.SimCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace Highpoint.Sage.ItemBased.Blocks
{

    /// <summary>BlockModelPersistence.
    /// </summary>
    [TestClass]
    public class BlockModelTester
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

        public BlockModelTester()
        {
        }

        [TestMethod]
        public void TestBlockModelPersistence()
        {

            Model model = new Model();

            model.RandomServer = new Randoms.RandomServer(12345, 100);



        }
    }
}
