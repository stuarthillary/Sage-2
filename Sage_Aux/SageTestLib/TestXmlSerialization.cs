/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Materials.Chemistry;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Diagnostics;


namespace Highpoint.Sage.Persistence
{

    [TestClass]
    public class PersistenceTester
    {

        public PersistenceTester()
        {
        }

        [TestMethod]
        public void TestPersistenceBasics()
        {

            XmlSerializationContext xsc = new XmlSerializationContext();

            MyTestObject mto2 = new MyTestObject("Gary", 3.1, null);

            MyTestObject mto = new MyTestObject("Bill", 6.2,
                new MyTestObject("Bob", 12.4,
                new MyTestObject("Steve", 24.8,
                new MyTestObject("Dave", 48.1,
                new MyTestObject("Sally", 96.2,
                new MyTestObject("Rufus", 186.9,
                null))))));

            Debug.WriteLine("Setting " + mto.Child1.Child1.Name + "'s child2 to " + mto2.Name);
            mto.Child1.Child1.Child2 = mto2;
            Debug.WriteLine("Setting " + mto.Child1.Name + "'s child2 to " + mto2.Name);
            mto.Child1.Child2 = mto2;

            xsc.StoreObject("MTO", mto);

            xsc.Save(Highpoint.Sage.Utility.DirectoryOperations.GetAppDataDir() + "foo.xml");

            xsc.Reset();

            xsc.Load(Highpoint.Sage.Utility.DirectoryOperations.GetAppDataDir() + "foo.xml");

            MyTestObject mto3 = (MyTestObject)xsc.LoadObject("MTO");

            xsc = new XmlSerializationContext();
            xsc.StoreObject("MTO", mto3);
            xsc.Save(Highpoint.Sage.Utility.DirectoryOperations.GetAppDataDir() + "foo2.xml");


        }

        [TestMethod]
        public void TestPersistenceWaterStorage()
        {

            MaterialType mt = new MaterialType(null, "Water", Guid.NewGuid(), 1.234, 4.05, MaterialState.Liquid, 18.0, 1034);

            BasicReactionSupporter brs = new BasicReactionSupporter();
            brs.MyMaterialCatalog.Add(mt);

            IMaterial water = mt.CreateMass(1500, 35);

            XmlSerializationContext xsc = new XmlSerializationContext();

            xsc.StoreObject("Water", water);

            xsc.Save(Highpoint.Sage.Utility.DirectoryOperations.GetAppDataDir() + "water.xml");

            xsc.Reset();

            xsc.StoreObject("MC", brs.MyMaterialCatalog);

            xsc.Save(Highpoint.Sage.Utility.DirectoryOperations.GetAppDataDir() + "mc.xml");

        }

        [TestMethod]
        public void TestPersistenceWaterRestoration()
        {

            XmlSerializationContext xsc = new XmlSerializationContext();
            xsc.Load(Highpoint.Sage.Utility.DirectoryOperations.GetAppDataDir() + "water.xml");
            IMaterial water = (IMaterial)xsc.LoadObject("Water");

            Debug.WriteLine(water);

        }


        [TestMethod]
        public void TestPersistenceChemistryStorage()
        {
            BasicReactionSupporter brs = new BasicReactionSupporter();
            Initialize(brs);
            XmlSerializationContext xsc = new XmlSerializationContext();
            xsc.StoreObject("Chemistry", brs);

            xsc.Save(Highpoint.Sage.Utility.DirectoryOperations.GetAppDataDir() + "chemistry.xml");
        }

        private void Initialize(BasicReactionSupporter brs)
        {

            MaterialCatalog mcat = brs.MyMaterialCatalog;
            ReactionProcessor rp = brs.MyReactionProcessor;

            mcat.Add(new MaterialType(null, "Water", Guid.NewGuid(), 1.0000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Hydrochloric Acid", Guid.NewGuid(), 1.1890, 2.5500, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Caustic Soda", Guid.NewGuid(), 2.0000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Sodium Chloride", Guid.NewGuid(), 2.1650, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Sulfuric Acid 98%", Guid.NewGuid(), 1.8420, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Potassium Hydroxide", Guid.NewGuid(), 1.3000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Potassium Sulfate", Guid.NewGuid(), 1.0000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Nitrous Acid", Guid.NewGuid(), 1.0000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Sodium Nitrite", Guid.NewGuid(), 2.3800, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Potassium Nitrite", Guid.NewGuid(), 1.9150, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Aluminum Hydroxide", Guid.NewGuid(), 1.0000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Ammonia", Guid.NewGuid(), 1.0000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Ammonium Hydroxide", Guid.NewGuid(), 1.0000, 4.1800, MaterialState.Liquid));
            mcat.Add(new MaterialType(null, "Bromine", Guid.NewGuid(), 3.1200, 4.1800, MaterialState.Liquid));

            Reaction r1 = new Reaction(null, "Reaction 1", Guid.NewGuid());
            r1.AddReactant(mcat["Caustic Soda"], 0.5231);
            r1.AddReactant(mcat["Hydrochloric Acid"], 0.4769);
            r1.AddProduct(mcat["Water"], 0.2356);
            r1.AddProduct(mcat["Sodium Chloride"], 0.7644);
            rp.AddReaction(r1);

            Reaction r2 = new Reaction(null, "Reaction 2", Guid.NewGuid());
            r2.AddReactant(mcat["Sulfuric Acid 98%"], 0.533622);
            r2.AddReactant(mcat["Potassium Hydroxide"], 0.466378);
            r2.AddProduct(mcat["Water"], 0.171333);
            r2.AddProduct(mcat["Potassium Sulfate"], 0.828667);
            rp.AddReaction(r2);

            Reaction r3 = new Reaction(null, "Reaction 3", Guid.NewGuid());
            r3.AddReactant(mcat["Caustic Soda"], 0.459681368);
            r3.AddReactant(mcat["Nitrous Acid"], 0.540318632);
            r3.AddProduct(mcat["Water"], 0.207047552);
            r3.AddProduct(mcat["Sodium Nitrite"], 0.792952448);
            rp.AddReaction(r3);

            Reaction r4 = new Reaction(null, "Reaction 4", Guid.NewGuid());
            r4.AddReactant(mcat["Potassium Hydroxide"], 0.544102);
            r4.AddReactant(mcat["Nitrous Acid"], 0.455898);
            r4.AddProduct(mcat["Water"], 0.174698);
            r4.AddProduct(mcat["Potassium Nitrite"], 0.825302);
            rp.AddReaction(r4);

        }


    }

    class MyTestObject : IXmlPersistable
    {
        private MyTestObject _child1;
        private MyTestObject _child2;
        private string _name;
        private double _age;
        private bool _married;
        private DateTime _birthday;
        private TimeSpan _ts;
        Hashtable _ht = new Hashtable();
        ArrayList _al = new ArrayList();
        public MyTestObject(string name, double age, MyTestObject child)
        {
            _name = name;
            _age = age;
            _child1 = child;
            Random random = new Random();
            _married = random.NextDouble() < 0.5;
            _birthday = DateTime.Now - TimeSpan.FromTicks((long)(random.NextDouble() * TimeSpan.FromDays(20).Ticks));
            _ts = TimeSpan.FromTicks((long)(random.NextDouble() * TimeSpan.FromDays(20).Ticks));
            _ht = new Hashtable();
            _ht.Add("Age", _age);
            _ht.Add("Birthday", _birthday);
            _al.Add("Dog");
            _al.Add("Cat");
            _al.Add("Cheetah");
            _al.Add("Banana");
            _al.Add("Which one of these is not like the others?");
        }
        public string Name
        {
            get
            {
                return _name;
            }
        }
        public MyTestObject Child1
        {
            get
            {
                return _child1;
            }
        }
        public MyTestObject Child2
        {
            get
            {
                return _child2;
            }
            set
            {
                _child2 = value;
            }
        }

        public MyTestObject()
        {
        }
        public void SerializeTo(XmlSerializationContext xmlsc)
        {
            xmlsc.StoreObject("Child1", _child1);
            xmlsc.StoreObject("Child2", _child2);
            xmlsc.StoreObject("Name", _name);
            xmlsc.StoreObject("Age", _age);
            xmlsc.StoreObject("Married", _married);
            xmlsc.StoreObject("Birthday", _birthday);
            xmlsc.StoreObject("TimeSpan", _ts);
            xmlsc.StoreObject("Hashtable", _ht);
            xmlsc.StoreObject("ArrayList", _al);
        }
        public void DeserializeFrom(XmlSerializationContext xmlsc)
        {
            _child1 = (MyTestObject)xmlsc.LoadObject("Child1");
            _child2 = (MyTestObject)xmlsc.LoadObject("Child2");
            _name = (string)xmlsc.LoadObject("Name");
            _age = (double)xmlsc.LoadObject("Age");
            _married = (bool)xmlsc.LoadObject("Married");
            _birthday = (DateTime)xmlsc.LoadObject("Birthday");
            _ts = (TimeSpan)xmlsc.LoadObject("TimeSpan");
            _ht = (Hashtable)xmlsc.LoadObject("Hashtable");
            _al = (ArrayList)xmlsc.LoadObject("ArrayList");
        }
    }
}