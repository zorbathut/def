namespace DefTest
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class Defs : Base
    {
        [Test]
	    public void TrivialParse()
	    {
            var parser = new Def.Parser();
            parser.ParseFromString(@"
                <Defs>
                    <StubDef defName=""TestDef"">
                    </StubDef>
                </Defs>",
                new Type[]{ typeof(StubDef) });

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));
	    }

        [Test]
	    public void TrivialEmptyParse()
	    {
            var parser = new Def.Parser();
            parser.ParseFromString(@"
                <Defs>
                    <StubDef defName=""TestDef"" />
                </Defs>",
                new Type[]{ typeof(StubDef) });

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));
	    }
        
        [Test]
	    public void NonDefType()
	    {
            var parser = new Def.Parser();
            ExpectErrors(() => parser.ParseFromString(@"
                <Defs>
                    <StubDef defName=""TestDef"" />
                </Defs>",
                new Type[]{ typeof(bool), typeof(StubDef) }));

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));
	    }

        [Test]
	    public void MissingDefType()
	    {
            var parser = new Def.Parser();
            ExpectErrors(() => parser.ParseFromString(@"
                <Defs>
                    <NonexistentDef defName=""TestDefA"" />
                    <StubDef defName=""TestDefB"" />
                </Defs>",
                new Type[]{ typeof(StubDef) }));

            Assert.IsNull(Def.Database<StubDef>.Get("TestDefA"));
            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDefB"));
	    }

        [Test]
	    public void MissingDefName()
	    {
            var parser = new Def.Parser();
            ExpectErrors(() => parser.ParseFromString(@"
                <Defs>
                    <StubDef />
                </Defs>",
                new Type[]{ typeof(StubDef) }));
	    }

        [Test]
	    public void InvalidDefName()
	    {
            var parser = new Def.Parser();
            ExpectErrors(() => parser.ParseFromString(@"
                <Defs>
                    <StubDef defName=""1NumberPrefix"" />
                    <StubDef defName=""Contains Spaces"" />
                    <StubDef defName=""HasPunctuation!"" />
                </Defs>",
                new Type[]{ typeof(StubDef) }));

            Assert.IsNull(Def.Database<StubDef>.Get("1NumberPrefix"));
            Assert.IsNull(Def.Database<StubDef>.Get("Contains Spaces"));
            Assert.IsNull(Def.Database<StubDef>.Get("HasPunctuation!"));
	    }

        public class IntDef : Def.Def
        {
            public int value = 4;
        }

        [Test]
	    public void DuplicateField()
	    {
            var parser = new Def.Parser();
            ExpectErrors(() => parser.ParseFromString(@"
                <Defs>
                    <IntDef defName=""TestDef"">
                        <value>3</value>
                        <value>6</value>
                    </IntDef>
                </Defs>",
                new Type[]{ typeof(IntDef) }));

            var result = Def.Database<IntDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(6, result.value);
	    }

        public class DeepParentDef : Def.Def
        {
            public int value = 4;
        }

        public class DeepChildDef : DeepParentDef
        {
            
        }

        [Test]
	    public void HierarchyDeepField()
	    {
            var parser = new Def.Parser();
            parser.ParseFromString(@"
                <Defs>
                    <DeepChildDef defName=""TestDef"">
                        <value>12</value>
                    </DeepChildDef>
                </Defs>",
                new Type[]{ typeof(DeepChildDef) });

            var result = Def.Database<DeepChildDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(12, result.value);
	    }

        public class DupeParentDef : Def.Def
        {
            public int value = 4;
        }

        public class DupeChildDef : DupeParentDef
        {
            new public int value = 8;
        }

        [Test]
	    public void HierarchyDuplicateField()
	    {
            var parser = new Def.Parser();
            ExpectErrors(() => parser.ParseFromString(@"
                <Defs>
                    <DupeChildDef defName=""TestDef"">
                        <value>12</value>
                    </DupeChildDef>
                </Defs>",
                new Type[]{ typeof(DupeChildDef) }));

            var result = Def.Database<DupeChildDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(12, result.value);
            Assert.AreEqual(4, ((DupeParentDef)result).value);
	    }
    }
}