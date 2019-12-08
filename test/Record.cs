namespace DefTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    [TestFixture]
    public class Record : Base
    {
        public class PrimitivesRecordable : Def.IRecordable
        {
            public int intValue;
            public float floatValue;
            public bool boolValue;
            public string stringValue;

            public Type typeValue;

            public void Record(Def.Recorder record)
            {
                record.Record(ref intValue, "intValue");
                record.Record(ref floatValue, "floatValue");
                record.Record(ref boolValue, "boolValue");
                record.Record(ref stringValue, "stringValue");

                record.Record(ref typeValue, "typeValue");
            }
        }

        [Test]
	    public void Primitives()
	    {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[]{ });
            parser.Finish();

            var primitives = new PrimitivesRecordable();
            primitives.intValue = 42;
            primitives.floatValue = 0.1234f;
            primitives.boolValue = true;
            primitives.stringValue = "<This is a test string value with some XML-sensitive characters.>";
            primitives.typeValue = typeof(Def.Def);

            string serialized = Def.Recorder.Write(primitives, pretty: true);
            var deserialized = Def.Recorder.Read<PrimitivesRecordable>(serialized);

            Assert.AreEqual(primitives.intValue, deserialized.intValue);
            Assert.AreEqual(primitives.floatValue, deserialized.floatValue);
            Assert.AreEqual(primitives.boolValue, deserialized.boolValue);
            Assert.AreEqual(primitives.stringValue, deserialized.stringValue);

            Assert.AreEqual(primitives.typeValue, typeof(Def.Def));
        }

        public class ConverterRecordable : Def.IRecordable
        {
            public Converted convertable;

            public void Record(Def.Recorder record)
            {
                record.Record(ref convertable, "convertable");
            }
        }
        public class Converted
        {
            public int a;
            public int b;
            public int c;
        }
        public class ConvertedConverter : Def.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type> { typeof(Converted) };
            }

            public override object FromString(string input, Type type, string inputName, int lineNumber)
            {
                var match = Regex.Match(input, "(-?[0-9]+) (-?[0-9]+) (-?[0-9]+)");
                return new Converted { a = int.Parse(match.Groups[1].Value), b = int.Parse(match.Groups[2].Value), c = int.Parse(match.Groups[3].Value) };
            }

            public override string ToString(object input)
            {
                var converted = input as Converted;
                return $"{converted.a} {converted.b} {converted.c}";
            }
        }

        [Test]
        public void Converter()
        {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[] { }, explicitConversionTypes: new Type[] { typeof(ConvertedConverter) });
            parser.Finish();

            var converted = new ConverterRecordable();
            converted.convertable = new Converted();
            converted.convertable.a = 42;
            converted.convertable.b = 1234;
            converted.convertable.c = -40;

            string serialized = Def.Recorder.Write(converted, pretty: true);
            var deserialized = Def.Recorder.Read<ConverterRecordable>(serialized);

            Assert.AreEqual(converted.convertable.a, deserialized.convertable.a);
            Assert.AreEqual(converted.convertable.b, deserialized.convertable.b);
            Assert.AreEqual(converted.convertable.c, deserialized.convertable.c);
        }

        [Def.StaticReferences]
        public static class StaticReferenceDefs
        {
            static StaticReferenceDefs() { Def.StaticReferencesAttribute.Initialized(); }

            public static StubDef TestDefA;
            public static StubDef TestDefB;
        }
        public class DefRecordable : Def.IRecordable
        {
            public StubDef a;
            public StubDef b;
            public StubDef empty;

            public void Record(Def.Recorder record)
            {
                record.Record(ref a, "a");
                record.Record(ref b, "b");
                record.Record(ref empty, "empty");
            }
        }

        [Test]
        public void Defs()
        {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[] { typeof(StubDef) }, explicitStaticRefs: new Type[] { typeof(StaticReferenceDefs) });
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDefA"" />
                    <StubDef defName=""TestDefB"" />
                </Defs>");
            parser.Finish();

            var defs = new DefRecordable();
            defs.a = StaticReferenceDefs.TestDefA;
            defs.b = StaticReferenceDefs.TestDefB;
            // leave empty empty, of course

            string serialized = Def.Recorder.Write(defs, pretty: true);
            var deserialized = Def.Recorder.Read<DefRecordable>(serialized);

            Assert.AreEqual(defs.a, deserialized.a);
            Assert.AreEqual(defs.b, deserialized.b);
            Assert.AreEqual(defs.empty, deserialized.empty);
        }

        public class RefsChildRecordable : Def.IRecordable
        {
            public void Record(Def.Recorder record)
            {
                // lol
            }
        }

        public class RefsRootRecordable : Def.IRecordable
        {
            public RefsChildRecordable childAone;
            public RefsChildRecordable childAtwo;
            public RefsChildRecordable childB;
            public RefsChildRecordable childEmpty;

            public void Record(Def.Recorder record)
            {
                record.Record(ref childAone, "childAone");
                record.Record(ref childAtwo, "childAtwo");
                record.Record(ref childB, "childB");
                record.Record(ref childEmpty, "childEmpty");
            }
        }
        
        [Test]
        public void Refs()
        {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[] { }, explicitConversionTypes: new Type[] { typeof(ConvertedConverter) });
            parser.Finish();

            var refs = new RefsRootRecordable();
            refs.childAone = new RefsChildRecordable();
            refs.childAtwo = refs.childAone;
            refs.childB = new RefsChildRecordable();
            refs.childEmpty = null;

            string serialized = Def.Recorder.Write(refs, pretty: true);
            var deserialized = Def.Recorder.Read<RefsRootRecordable>(serialized);

            Assert.IsNotNull(deserialized.childAone);
            Assert.IsNotNull(deserialized.childAtwo);
            Assert.IsNotNull(deserialized.childB);
            Assert.IsNull(deserialized.childEmpty);

            Assert.AreEqual(deserialized.childAone, deserialized.childAtwo);
            Assert.AreNotEqual(deserialized.childAone, deserialized.childB);
        }

        public class ContainersRecordable : Def.IRecordable
        {
            public List<int> intList = new List<int>();
            public Dictionary<string, string> stringDict = new Dictionary<string, string>();

            public void Record(Def.Recorder record)
            {
                record.Record(ref intList, "intList");
                record.Record(ref stringDict, "stringDict");
            }
        }

        [Test]
        public void Containers()
        {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[] { });
            parser.Finish();

            var containers = new ContainersRecordable();
            containers.intList.Add(42);
            containers.intList.Add(1234);
            containers.intList.Add(-105);

            containers.stringDict["Key"] = "Value";
            containers.stringDict["Info"] = "Data";

            string serialized = Def.Recorder.Write(containers, pretty: true);
            var deserialized = Def.Recorder.Read<ContainersRecordable>(serialized);

            Assert.AreEqual(containers.intList, deserialized.intList);
            Assert.AreEqual(containers.stringDict, deserialized.stringDict);
        }

        public class ContainersNestedRecordable : Def.IRecordable
        {
            public List<List<int>> intLL = new List<List<int>>();

            public void Record(Def.Recorder record)
            {
                record.Record(ref intLL, "intLL");
            }
        }

        [Test]
        public void ContainersNested()
        {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[] { });
            parser.Finish();

            var nested = new ContainersNestedRecordable();
            nested.intLL.Add(new List<int>());
            nested.intLL.Add(null);
            nested.intLL.Add(new List<int>());
            nested.intLL.Add(new List<int>());
            nested.intLL[0].Add(42);
            nested.intLL[0].Add(95);
            nested.intLL[2].Add(203);

            string serialized = Def.Recorder.Write(nested, pretty: true);
            var deserialized = Def.Recorder.Read<ContainersNestedRecordable>(serialized);

            Assert.AreEqual(nested.intLL, deserialized.intLL);
        }

        public class RecursiveParent : Def.IRecordable
        {
            public List<RecursiveNode> children = new List<RecursiveNode>();

            public void Record(Def.Recorder record)
            {
                record.Record(ref children, "children");
            }
        }

        public class RecursiveNode : Def.IRecordable
        {
            public RecursiveNode childA;
            public RecursiveNode childB;

            public void Record(Def.Recorder record)
            {
                record.Record(ref childA, "childA");
                record.Record(ref childB, "childB");
            }
        }

        [Test]
        public void ContainerRecursive()
        {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[] { });
            parser.Finish();

            var parent = new RecursiveParent();
            parent.children.Add(new RecursiveNode());
            parent.children.Add(new RecursiveNode());
            parent.children.Add(new RecursiveNode());

            parent.children[0].childB = parent.children[1];
            parent.children[1].childA = parent.children[0];

            // look on my works, ye mighty, and despair
            parent.children[2].childA = parent.children[2];
            parent.children[2].childB = parent.children[2];

            string serialized = Def.Recorder.Write(parent, pretty: true);
            var deserialized = Def.Recorder.Read<RecursiveParent>(serialized);

            Assert.IsNull(deserialized.children[0].childA);
            Assert.AreSame(deserialized.children[1], deserialized.children[0].childB);

            Assert.AreSame(deserialized.children[0], deserialized.children[1].childA);
            Assert.IsNull(deserialized.children[1].childB);

            Assert.AreSame(deserialized.children[2], deserialized.children[2].childA);
            Assert.AreSame(deserialized.children[2], deserialized.children[2].childB);

            Assert.AreEqual(3, deserialized.children.Count);
        }

        // hierarchy
    }
}
