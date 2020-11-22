namespace DefTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [TestFixture]
    public class Index : Base
    {
        public class IndexBaseDef : Def.Def
        {
            [Def.Index] public int index;
        }

        public class IndexDerivedDef : IndexBaseDef
        {
            [Def.Index] public new int index;
        }

        public class IndexLeafDef : StubDef
        {
            [Def.Index] public int index;
        }

        [Test]
        public void IndexBaseList([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IndexBaseDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <IndexBaseDef defName=""TestDefA"" />
                    <IndexBaseDef defName=""TestDefB"" />
                    <IndexBaseDef defName=""TestDefC"" />
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            Assert.AreSame(Def.Database<IndexBaseDef>.Get("TestDefA"), Def.Index<IndexBaseDef>.Get(Def.Database<IndexBaseDef>.Get("TestDefA").index));
            Assert.AreSame(Def.Database<IndexBaseDef>.Get("TestDefB"), Def.Index<IndexBaseDef>.Get(Def.Database<IndexBaseDef>.Get("TestDefB").index));
            Assert.AreSame(Def.Database<IndexBaseDef>.Get("TestDefC"), Def.Index<IndexBaseDef>.Get(Def.Database<IndexBaseDef>.Get("TestDefC").index));

            Assert.AreEqual(3, Def.Index<IndexBaseDef>.Count);
        }

        [Test]
        public void IndexDerivedList([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IndexBaseDef), typeof(IndexDerivedDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <IndexDerivedDef defName=""TestDefA"" />
                    <IndexBaseDef defName=""TestDefB"" />
                    <IndexDerivedDef defName=""TestDefC"" />
                    <IndexBaseDef defName=""TestDefD"" />
                    <IndexDerivedDef defName=""TestDefE"" />
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            Assert.AreSame(Def.Database<IndexBaseDef>.Get("TestDefA"), Def.Index<IndexBaseDef>.Get(Def.Database<IndexBaseDef>.Get("TestDefA").index));
            Assert.AreSame(Def.Database<IndexBaseDef>.Get("TestDefB"), Def.Index<IndexBaseDef>.Get(Def.Database<IndexBaseDef>.Get("TestDefB").index));
            Assert.AreSame(Def.Database<IndexBaseDef>.Get("TestDefC"), Def.Index<IndexBaseDef>.Get(Def.Database<IndexBaseDef>.Get("TestDefC").index));
            Assert.AreSame(Def.Database<IndexBaseDef>.Get("TestDefD"), Def.Index<IndexBaseDef>.Get(Def.Database<IndexBaseDef>.Get("TestDefD").index));
            Assert.AreSame(Def.Database<IndexBaseDef>.Get("TestDefE"), Def.Index<IndexBaseDef>.Get(Def.Database<IndexBaseDef>.Get("TestDefE").index));

            Assert.AreSame(Def.Database<IndexDerivedDef>.Get("TestDefA"), Def.Index<IndexDerivedDef>.Get(Def.Database<IndexDerivedDef>.Get("TestDefA").index));
            Assert.AreSame(Def.Database<IndexDerivedDef>.Get("TestDefC"), Def.Index<IndexDerivedDef>.Get(Def.Database<IndexDerivedDef>.Get("TestDefC").index));
            Assert.AreSame(Def.Database<IndexDerivedDef>.Get("TestDefE"), Def.Index<IndexDerivedDef>.Get(Def.Database<IndexDerivedDef>.Get("TestDefE").index));

            Assert.AreEqual(5, Def.Index<IndexBaseDef>.Count);
            Assert.AreEqual(3, Def.Index<IndexDerivedDef>.Count);
        }

        [Test]
        public void IndexLeafList([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IndexLeafDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <IndexLeafDef defName=""TestDefA"" />
                    <IndexLeafDef defName=""TestDefB"" />
                    <IndexLeafDef defName=""TestDefC"" />
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            Assert.AreSame(Def.Database<IndexLeafDef>.Get("TestDefA"), Def.Index<IndexLeafDef>.Get(Def.Database<IndexLeafDef>.Get("TestDefA").index));
            Assert.AreSame(Def.Database<IndexLeafDef>.Get("TestDefB"), Def.Index<IndexLeafDef>.Get(Def.Database<IndexLeafDef>.Get("TestDefB").index));
            Assert.AreSame(Def.Database<IndexLeafDef>.Get("TestDefC"), Def.Index<IndexLeafDef>.Get(Def.Database<IndexLeafDef>.Get("TestDefC").index));
            
            Assert.AreEqual(3, Def.Index<IndexLeafDef>.Count);
        }

        public class IndependentIndexClass
        {
            [Def.Index] public int index;
        }

        public struct IndependentIndexStruct
        {
            [Def.Index] public int index;

            public int value;
        }

        public class IndependentIndexDef : Def.Def
        {
            public IndependentIndexClass iicPrefilled = new IndependentIndexClass();
            public IndependentIndexClass iicUnfilled;
            public IndependentIndexStruct iis = new IndependentIndexStruct();
        }

        [Test]
        public void IndependentIndex([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IndependentIndexDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <IndependentIndexDef defName=""TestDefA"">
                        <iicPrefilled />
                        <iicUnfilled />
                        <iis><value>7</value></iis>
                    </IndependentIndexDef>
                    <IndependentIndexDef defName=""TestDefB"">
                        <iicPrefilled />
                        <iis><value>8</value></iis>
                    </IndependentIndexDef>
                    <IndependentIndexDef defName=""TestDefC"">
                        <iicPrefilled />
                        <iicUnfilled />
                        <iis><value>9</value></iis>
                    </IndependentIndexDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            // At the moment, the expected behavior is that classes which are explicitly mentioned get indices, classes which default to null and aren't mentioned don't get indices.
            // Classes which default to objects and aren't explicitly mentioned are in a gray area where we currently don't guarantee anything.
            // This needs to be fixed, but I'm not sure how to do it; I've got a few ideas but nothing I really like.

            // Structs, also, are required to be explicitly created.

            // *addendum*: I know how to fix this, it's just going to be a pain.

            Assert.AreSame(Def.Database<IndependentIndexDef>.Get("TestDefA").iicPrefilled, Def.Index<IndependentIndexClass>.Get(Def.Database<IndependentIndexDef>.Get("TestDefA").iicPrefilled.index));
            Assert.AreSame(Def.Database<IndependentIndexDef>.Get("TestDefB").iicPrefilled, Def.Index<IndependentIndexClass>.Get(Def.Database<IndependentIndexDef>.Get("TestDefB").iicPrefilled.index));
            Assert.AreSame(Def.Database<IndependentIndexDef>.Get("TestDefC").iicPrefilled, Def.Index<IndependentIndexClass>.Get(Def.Database<IndependentIndexDef>.Get("TestDefC").iicPrefilled.index));

            Assert.AreSame(Def.Database<IndependentIndexDef>.Get("TestDefA").iicUnfilled, Def.Index<IndependentIndexClass>.Get(Def.Database<IndependentIndexDef>.Get("TestDefA").iicUnfilled.index));
            Assert.AreSame(Def.Database<IndependentIndexDef>.Get("TestDefC").iicUnfilled, Def.Index<IndependentIndexClass>.Get(Def.Database<IndependentIndexDef>.Get("TestDefC").iicUnfilled.index));

            Assert.AreEqual(Def.Database<IndependentIndexDef>.Get("TestDefA").iis, Def.Index<IndependentIndexStruct>.Get(Def.Database<IndependentIndexDef>.Get("TestDefA").iis.index));
            Assert.AreEqual(Def.Database<IndependentIndexDef>.Get("TestDefB").iis, Def.Index<IndependentIndexStruct>.Get(Def.Database<IndependentIndexDef>.Get("TestDefB").iis.index));
            Assert.AreEqual(Def.Database<IndependentIndexDef>.Get("TestDefC").iis, Def.Index<IndependentIndexStruct>.Get(Def.Database<IndependentIndexDef>.Get("TestDefC").iis.index));

            Assert.AreEqual(5, Def.Index<IndependentIndexClass>.Count);
            Assert.AreEqual(3, Def.Index<IndependentIndexStruct>.Count);
        }

        public class ExcessiveIndicesDef : Def.Def
        {
            [Def.Index] public int indexA;
            [Def.Index] public int indexB;
        }

        [Test]
        public void ExcessiveIndices([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ExcessiveIndicesDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <ExcessiveIndicesDef defName=""TestDefA"" />
                    <ExcessiveIndicesDef defName=""TestDefB"" />
                    <ExcessiveIndicesDef defName=""TestDefC"" />
                </Defs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode, rewrite_expectParseErrors: true);

            // It's guaranteed that either indexA or indexB has some behavior that causes 0, 1, and 2 to be distributed among TestDef.
            // This is a massive over-constraint of allowable behavior; loosen it up if it fails somewhere.
            Assert.AreEqual(0, Def.Database<ExcessiveIndicesDef>.Get("TestDefA").indexB);
            Assert.AreEqual(1, Def.Database<ExcessiveIndicesDef>.Get("TestDefB").indexB);
            Assert.AreEqual(2, Def.Database<ExcessiveIndicesDef>.Get("TestDefC").indexB);
        }
    }
}
