namespace DefTest
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class Writer : Base
    {
        // A lot of the Writer functionality is tested via BehaviorMode.Rewritten in other tests, so these tests mostly handle the Create/Delete/Rename functions.

        public class SomeDefs : Def.Def
        {
            public SomeValues values;
            public SomeDefs defs;
        }

        public class SomeValues : Def.Def
        {
            public int number;
        }

        [Test]
        public void Creation([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SomeDefs), typeof(SomeValues) } };

            Def.Database.Create<SomeValues>("Hello").number = 10;
            Def.Database.Create<SomeValues>("Goodbye").number = 42;

            DoBehavior(mode);

            Assert.AreEqual(10, Def.Database<SomeValues>.Get("Hello").number);
            Assert.AreEqual(42, Def.Database<SomeValues>.Get("Goodbye").number);
        }

        [Test]
        public void MultiCreation([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SomeDefs), typeof(SomeValues) } };

            Def.Database.Create<SomeDefs>("Defs");
            Def.Database.Create<SomeValues>("Values");

            DoBehavior(mode);

            Assert.IsNotNull(Def.Database<SomeDefs>.Get("Defs"));
            Assert.IsNull(Def.Database<SomeValues>.Get("Defs"));

            Assert.IsNull(Def.Database<SomeDefs>.Get("Values"));
            Assert.IsNotNull(Def.Database<SomeValues>.Get("Values"));
        }

        [Test]
        public void Databases()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SomeDefs), typeof(SomeValues) } };

            var selfRef = Def.Database.Create<SomeDefs>("SelfRef");
            var otherRef = Def.Database.Create<SomeDefs>("OtherRef");
            var values = Def.Database.Create<SomeValues>("Values");

            Assert.AreSame(selfRef, Def.Database.Get(typeof(SomeDefs), "SelfRef"));
            Assert.AreSame(otherRef, Def.Database.Get(typeof(SomeDefs), "OtherRef"));
            Assert.AreSame(values, Def.Database.Get(typeof(SomeValues), "Values"));

            Assert.AreSame(selfRef, Def.Database<SomeDefs>.Get("SelfRef"));
            Assert.AreSame(otherRef, Def.Database<SomeDefs>.Get("OtherRef"));
            Assert.AreSame(values, Def.Database<SomeValues>.Get("Values"));
        }

        [Test]
        public void References([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SomeDefs), typeof(SomeValues) } };

            var selfRef = Def.Database.Create<SomeDefs>("SelfRef");
            var otherRef = Def.Database.Create<SomeDefs>("OtherRef");
            var values = Def.Database.Create<SomeValues>("Values");

            selfRef.defs = selfRef;
            selfRef.values = values;
            otherRef.defs = selfRef;
            otherRef.values = values;

            DoBehavior(mode);

            Assert.AreSame(Def.Database<SomeDefs>.Get("SelfRef"), Def.Database<SomeDefs>.Get("SelfRef").defs);
            Assert.AreSame(Def.Database<SomeValues>.Get("Values"), Def.Database<SomeDefs>.Get("SelfRef").values);
            Assert.AreSame(Def.Database<SomeDefs>.Get("SelfRef"), Def.Database<SomeDefs>.Get("OtherRef").defs);
            Assert.AreSame(Def.Database<SomeValues>.Get("Values"), Def.Database<SomeDefs>.Get("OtherRef").values);
        }

        public class IntDef : Def.Def
        {
            public int value = 4;
        }

        [Test]
        public void Delete([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <IntDef defName=""One""><value>1</value></IntDef>
                    <IntDef defName=""Two""><value>2</value></IntDef>
                    <IntDef defName=""Three""><value>3</value></IntDef>
                </Defs>");
            parser.Finish();

            Def.Database.Delete(Def.Database<IntDef>.Get("Two"));

            DoBehavior(mode);

            Assert.AreEqual(1, Def.Database<IntDef>.Get("One").value);
            Assert.IsNull(Def.Database<IntDef>.Get("Two"));
            Assert.AreEqual(3, Def.Database<IntDef>.Get("Three").value);
        }

        [Test]
        public void DoubleDelete([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <IntDef defName=""One""><value>1</value></IntDef>
                    <IntDef defName=""Two""><value>2</value></IntDef>
                    <IntDef defName=""Three""><value>3</value></IntDef>
                </Defs>");
            parser.Finish();

            var one = Def.Database<IntDef>.Get("One");
            Def.Database.Delete(one);
            ExpectErrors(() => Def.Database.Delete(one));
            Def.Database.Delete(Def.Database<IntDef>.Get("Three"));

            DoBehavior(mode);

            Assert.IsNull(Def.Database<IntDef>.Get("One"));
            Assert.AreEqual(2, Def.Database<IntDef>.Get("Two").value);
            Assert.IsNull(Def.Database<IntDef>.Get("Three"));
        }

        [Test]
        public void Rename([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <IntDef defName=""One""><value>1</value></IntDef>
                    <IntDef defName=""Two""><value>2</value></IntDef>
                    <IntDef defName=""Three""><value>3</value></IntDef>
                </Defs>");
            parser.Finish();

            Def.Database.Rename(Def.Database<IntDef>.Get("One"), "OneBeta");
            Def.Database.Rename(Def.Database<IntDef>.Get("OneBeta"), "OneGamma");

            // yes okay this is confusing
            Def.Database.Rename(Def.Database<IntDef>.Get("Two"), "One");

            DoBehavior(mode);

            Assert.AreEqual(1, Def.Database<IntDef>.Get("OneGamma").value);
            Assert.AreEqual(2, Def.Database<IntDef>.Get("One").value);
            Assert.AreEqual(3, Def.Database<IntDef>.Get("Three").value);
        }

        [Test]
        public void RenameDeleted([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <IntDef defName=""One""><value>1</value></IntDef>
                    <IntDef defName=""Two""><value>2</value></IntDef>
                    <IntDef defName=""Three""><value>3</value></IntDef>
                </Defs>");
            parser.Finish();

            var three = Def.Database<IntDef>.Get("Three");
            Def.Database.Delete(three);
            ExpectErrors(() => Def.Database.Rename(three, "ThreePhoenix"));

            DoBehavior(mode);

            Assert.AreEqual(1, Def.Database<IntDef>.Get("One").value);
            Assert.AreEqual(2, Def.Database<IntDef>.Get("Two").value);
            Assert.IsNull(Def.Database<IntDef>.Get("Three"));
            Assert.IsNull(Def.Database<IntDef>.Get("ThreePhoenix"));
        }

        // def reference to a def that no longer officially exists
            // also test this in Recorder, now that it's a thing that can happen
        // make sure to explore the create and delete and rename error pathways
    }
}
