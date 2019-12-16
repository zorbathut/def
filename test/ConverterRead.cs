namespace DefTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    [TestFixture]
    public class ConverterRead : Base
    {
        public class ConverterTestPayload
        {
            public int number = 0;
        }

        public class ConverterDef : Def.Def
        {
            public ConverterTestPayload payload;
        }

        public class ConverterBasicTest : Def.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type>() { typeof(ConverterTestPayload) };
            }

            public override object FromString(string input, Type type, string inputName, int lineNumber)
            {
                return new ConverterTestPayload() {number = int.Parse(input) };
            }

            public override object FromXml(XElement input, Type type, string inputName)
            {
                return new ConverterTestPayload() {number = int.Parse(input.Nodes().OfType<XElement>().First().Nodes().OfType<XText>().First().Value) };
            }
        }

        [Test]
	    public void BasicFunctionality()
	    {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[]{ typeof(ConverterDef) }, explicitConverters: new Type[]{ typeof(ConverterBasicTest) });
            parser.AddString(@"
                <Defs>
                    <ConverterDef defName=""TestDefA"">
                        <payload>4</payload>
                    </ConverterDef>
                    <ConverterDef defName=""TestDefB"">
                        <payload><cargo>8</cargo></payload>
                    </ConverterDef>
                </Defs>");
            parser.Finish();

            Assert.AreEqual(4, Def.Database<ConverterDef>.Get("TestDefA").payload.number);
            Assert.AreEqual(8, Def.Database<ConverterDef>.Get("TestDefB").payload.number);
	    }

        public class EmptyConverter : Def.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type>() { };
            }
        }

        [Test]
	    public void EmptyConverterErr()
	    {
            Def.Parser parser = null;
            ExpectErrors(() => parser = new Def.Parser(explicitOnly: true, explicitConverters: new Type[]{ typeof(EmptyConverter) }));
            parser.Finish();
	    }

        public class StrConv1 : Def.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type>() { typeof(string) };
            }
        }

        public class StrConv2 : Def.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type>() { typeof(string) };
            }
        }

        [Test]
	    public void OverlappingConverters()
	    {
            Def.Parser parser = null;
            ExpectErrors(() => parser = new Def.Parser(explicitOnly: true, explicitConverters: new Type[]{ typeof(StrConv1), typeof(StrConv2) }));
            parser.Finish();
	    }

        public class ConverterStringPayload
        {
            public string payload;
        }

        public class ConverterDictTest : Def.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type>() { typeof(ConverterStringPayload) };
            }

            public override object FromString(string input, Type type, string inputName, int lineNumber)
            {
                return new ConverterStringPayload() { payload = input };
            }
        }

        public class ConverterDictDef : Def.Def
        {
            public Dictionary<ConverterStringPayload, int> payload;
        }

        [Test]
	    public void ConverterDict()
	    {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[]{ typeof(ConverterDictDef) }, explicitConverters: new Type[]{ typeof(ConverterDictTest) });
            parser.AddString(@"
                <Defs>
                    <ConverterDictDef defName=""TestDef"">
                        <payload>
                            <yabba>1</yabba>
                            <dabba>4</dabba>
                            <doo>9</doo>
                        </payload>
                    </ConverterDictDef>
                </Defs>");
            parser.Finish();

            var testDef = Def.Database<ConverterDictDef>.Get("TestDef");

            Assert.AreEqual(1, testDef.payload.Where(kvp => kvp.Key.payload == "yabba").First().Value);
            Assert.AreEqual(4, testDef.payload.Where(kvp => kvp.Key.payload == "dabba").First().Value);
            Assert.AreEqual(9, testDef.payload.Where(kvp => kvp.Key.payload == "doo").First().Value);
	    }

        public class ConverterStringDef : Def.Def
        {
            public ConverterStringPayload payload;
        }

        [Test]
        public void EmptyInputConverter()
        {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[]{ typeof(ConverterStringDef) }, explicitConverters: new Type[]{ typeof(ConverterDictTest) });
            parser.AddString(@"
                <Defs>
                    <ConverterStringDef defName=""TestDef"">
                        <payload></payload>
                    </ConverterStringDef>
                </Defs>");
            parser.Finish();

            var testDef = Def.Database<ConverterStringDef>.Get("TestDef");

            Assert.AreEqual("", testDef.payload.payload);
        }

        public class DefaultFailureConverter : Def.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type>() { typeof(ConverterStringPayload) };
            }
        }

        [Test]
        public void DefaultFailureTestString()
        {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[]{ typeof(ConverterStringDef) }, explicitConverters: new Type[]{ typeof(DefaultFailureConverter) });
            parser.AddString(@"
                <Defs>
                    <ConverterStringDef defName=""TestDef"">
                        <payload>stringfail</payload>
                    </ConverterStringDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            var testDef = Def.Database<ConverterStringDef>.Get("TestDef");
            Assert.IsNull(testDef.payload);
        }

        [Test]
        public void DefaultFailureTestXml()
        {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[]{ typeof(ConverterStringDef) }, explicitConverters: new Type[]{ typeof(DefaultFailureConverter) });
            parser.AddString(@"
                <Defs>
                    <ConverterStringDef defName=""TestDef"">
                        <payload><xmlfail></xmlfail></payload>
                    </ConverterStringDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            var testDef = Def.Database<ConverterStringDef>.Get("TestDef");
            Assert.IsNull(testDef.payload);
        }

        public class NonEmptyPayloadDef : Def.Def
        {
            public ConverterStringPayload payload = new ConverterStringPayload();
        }

        public class DefaultNullConverter : Def.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type>() { typeof(ConverterStringPayload) };
            }

            public override object FromString(string input, Type type, string inputName, int lineNumber)
            {
                return null;
            }
        }

        [Test]
        public void ConvertToNull()
        {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[]{ typeof(NonEmptyPayloadDef) }, explicitConverters: new Type[]{ typeof(DefaultNullConverter) });
            parser.AddString(@"
                <Defs>
                    <NonEmptyPayloadDef defName=""TestDefault"">
                    </NonEmptyPayloadDef>
                    <NonEmptyPayloadDef defName=""TestNull"">
                        <payload>makemenull</payload>
                    </NonEmptyPayloadDef>
                </Defs>");
            parser.Finish();

            Assert.IsNotNull(Def.Database<NonEmptyPayloadDef>.Get("TestDefault").payload);
            Assert.IsNull(Def.Database<NonEmptyPayloadDef>.Get("TestNull").payload);
        }

        public struct ConverterStructObj
        {
            public int intA;
            public int intB;
        }

        public class ConverterStructDef : Def.Def
        {
            public ConverterStructObj replacedDefault;
            public ConverterStructObj initializedUntouched = new ConverterStructObj { intA = 1, intB = 2 };
            public ConverterStructObj initializedReplaced = new ConverterStructObj { intA = 3, intB = 4 };
            public ConverterStructObj initializedTouched = new ConverterStructObj { intA = 5, intB = 6 };
        }

        public class ConverterStructConverter : Def.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type>() { typeof(ConverterStructObj) };
            }

            public override object Record(object model, Type type, Def.Recorder recorder)
            {
                ConverterStructObj cso;

                if (model == null)
                {
                    cso = new ConverterStructObj();
                }
                else
                {
                    cso = (ConverterStructObj)model;
                }

                recorder.Record(ref cso.intA, "intA");
                recorder.Record(ref cso.intB, "intB");

                return cso;
            }
        }

        [Test]
        public void ConverterStruct()
        {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[] { typeof(ConverterStructDef) }, explicitConverters: new Type[] { typeof(ConverterStructConverter) });
            parser.AddString(@"
                <Defs>
                    <ConverterStructDef defName=""TestDef"">
                        <replacedDefault>
                            <intA>20</intA>
                            <intB>21</intB>
                        </replacedDefault>
                        <initializedReplaced>
                            <intA>22</intA>
                            <intB>23</intB>
                        </initializedReplaced>
                        <initializedTouched>
                            <intA>24</intA>
                        </initializedTouched>
                    </ConverterStructDef>
                </Defs>");
            parser.Finish();

            var testDef = Def.Database<ConverterStructDef>.Get("TestDef");

            Assert.AreEqual(20, testDef.replacedDefault.intA);
            Assert.AreEqual(21, testDef.replacedDefault.intB);

            Assert.AreEqual(1, testDef.initializedUntouched.intA);
            Assert.AreEqual(2, testDef.initializedUntouched.intB);

            Assert.AreEqual(22, testDef.initializedReplaced.intA);
            Assert.AreEqual(23, testDef.initializedReplaced.intB);

            Assert.AreEqual(24, testDef.initializedTouched.intA);
            Assert.AreEqual(6, testDef.initializedTouched.intB);
        }

        public class FallbackPayload
        {
            public int number = 0;
        }

        public class FallbackDef : Def.Def
        {
            public FallbackPayload payload;
        }

        public class FallbackConverter : Def.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type>() { typeof(FallbackPayload) };
            }

            public override object FromString(string input, Type type, string inputName, int lineNumber)
            {
                return new FallbackPayload() { number = int.Parse(input) };
            }
        }

        [Test]
        public void Fallback()
        {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[] { typeof(FallbackDef) }, explicitConverters: new Type[] { typeof(FallbackConverter) });
            parser.AddString(@"
                <Defs>
                    <FallbackDef defName=""TestDef"">
                        <payload>
                            4
                            <garbage />
                        </payload>
                    </FallbackDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            Assert.AreEqual(4, Def.Database<FallbackDef>.Get("TestDef").payload.number);
        }
    }
}
