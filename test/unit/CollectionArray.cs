namespace DecTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    public class CollectionArray : Base
    {
        public class ArrayDec : Dec.Dec
        {
            public int[] dataEmpty = null;
            public int[] dataProvided = new int[] { 10, 20 };
        }

        [Test]
        public void Basic([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(ArrayDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ArrayDec decName=""TestDec"">
                        <dataEmpty>
                            <li>10</li>
                            <li>9</li>
                            <li>8</li>
                            <li>7</li>
                            <li>6</li>
                        </dataEmpty>
                        <dataProvided>
                            <li>10</li>
                            <li>9</li>
                            <li>8</li>
                            <li>7</li>
                            <li>6</li>
                        </dataProvided>
                    </ArrayDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<ArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.dataEmpty, new[] { 10, 9, 8, 7, 6 });
            Assert.AreEqual(result.dataProvided, new[] { 10, 9, 8, 7, 6 });
        }

        [Test]
        public void AsStringError([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ArrayDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ArrayDec decName=""TestDec"">
                        <dataEmpty>nope</dataEmpty>
                        <dataProvided>nope</dataProvided>
                    </ArrayDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            var result = Dec.Database<ArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            // error should default to existing data
            Assert.IsNull(result.dataEmpty);
            Assert.AreEqual(result.dataProvided, new[] { 10, 20 });
        }

        [Test]
        public void Zero([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ArrayDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ArrayDec decName=""TestDec"">
                        <dataEmpty></dataEmpty>
                        <dataProvided></dataProvided>
                    </ArrayDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<ArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.dataEmpty, new int[] { });
            Assert.AreEqual(result.dataProvided, new int[] { });
        }

        [Test]
        public void Null([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ArrayDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ArrayDec decName=""TestDec"">
                        <dataEmpty null=""true""></dataEmpty>
                        <dataProvided null=""true""></dataProvided>
                    </ArrayDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<ArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNull(result.dataEmpty);
            Assert.IsNull(result.dataProvided);
        }

        [Test]
        public void ElementMisparse([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ArrayDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ArrayDec decName=""TestDec"">
                        <dataEmpty>
                            <li>10</li>
                            <li>9</li>
                            <li>8</li>
                            <li>dog</li>
                            <li>6</li>
                        </dataEmpty>
                        <dataProvided>
                            <li>10</li>
                            <li>9</li>
                            <li>8</li>
                            <li>dog</li>
                            <li>6</li>
                        </dataProvided>
                    </ArrayDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            var result = Dec.Database<ArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.dataEmpty, new[] { 10, 9, 8, 0, 6 });
            Assert.AreEqual(result.dataProvided, new[] { 10, 9, 8, 0, 6 });
        }
    }
}
