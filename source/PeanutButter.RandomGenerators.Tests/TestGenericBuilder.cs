﻿using System;
using System.Collections.Generic;
using System.Linq;
using EmailSpooler.Win32Service.Entity;
using NUnit.Framework;
using PeanutButter.Utils;

namespace PeanutButter.RandomGenerators.Tests
{
    [TestFixture]
    public class TestGenericBuilder: TestBase
    {
        private class SimpleClass
        {
        }

        private class SimpleBuilder : GenericBuilder<SimpleBuilder, SimpleClass>
        {
        }

        [Test]
        public void Create_ReturnsANewInstanceOfTheBuilder()
        {
            //---------------Set up test pack-------------------
            
            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var b1 = SimpleBuilder.Create();
            var b2 = SimpleBuilder.Create();

            //---------------Test Result -----------------------
            Assert.IsNotNull(b1);
            Assert.IsNotNull(b2);
            Assert.IsInstanceOf<SimpleBuilder>(b1);
            Assert.IsInstanceOf<SimpleBuilder>(b2);
            Assert.AreNotEqual(b1, b2);
        }

        [Test]
        public void Build_ReturnsANewInstanceOfTheTargetClass()
        {
            //---------------Set up test pack-------------------
            var builder = SimpleBuilder.Create();
            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var simple1 = builder.Build();
            var simple2 = builder.Build();

            //---------------Test Result -----------------------
            Assert.IsNotNull(simple1);
            Assert.IsNotNull(simple2);
            Assert.IsInstanceOf<SimpleClass>(simple1);
            Assert.IsInstanceOf<SimpleClass>(simple2);
            Assert.AreNotEqual(simple1, simple2);
        }

        public enum SomeValues
        {
            One,
            Two,
            Three,
            Four
        }
        private class NotAsSimpleClass
        {
            public string Name { get; set; }
            public int Value { get; set; }
            public bool Flag { get; set; }
            public DateTime Created { get; set; }
            public decimal Cost { get; set; }
            public double DoubleValue { get; set; }
            public float FloatValue { get; set; }
            public Guid GuidValue { get; set; }
            public decimal? NullableDecimalValue { get; set; }
            public byte[] ByteArrayValue { get; set; }
            public SomeValues EnumValue { get; set; }
        }

        private class NotAsSimpleBuilder : GenericBuilder<NotAsSimpleBuilder, NotAsSimpleClass>
        {
        }

        [Test]
        public void BuildDefault_ReturnsBlankObject()
        {
            //---------------Set up test pack-------------------
            var blank = new NotAsSimpleClass();
            
            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var obj = NotAsSimpleBuilder.BuildDefault();
            var rand = NotAsSimpleBuilder.BuildRandom();

            //---------------Test Result -----------------------
            Assert.IsNotNull(obj);
            Assert.IsInstanceOf<NotAsSimpleClass>(obj);
            Assert.AreEqual(blank.Name, obj.Name);
            Assert.AreEqual(blank.Value, obj.Value);
            Assert.AreEqual(blank.Flag, obj.Flag);
            Assert.AreEqual(blank.Created, obj.Created);
            Assert.AreEqual(blank.Cost, obj.Cost);
        }



        [Test]
        public void WithRandomProps_SetsRandomValuesForAllProperties()
        {
            //---------------Set up test pack-------------------

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var randomItems = new List<NotAsSimpleClass>();
            for (var i = 0; i < RANDOM_TEST_CYCLES; i++)
            {
                randomItems.Add(NotAsSimpleBuilder.Create().WithRandomProps().Build());
            }

            //---------------Test Result -----------------------
            // look for variance
            VarianceAssert.IsVariant<NotAsSimpleClass, string>(randomItems, "Name");
            VarianceAssert.IsVariant<NotAsSimpleClass, int>(randomItems, "Value");
            VarianceAssert.IsVariant<NotAsSimpleClass, bool>(randomItems, "Flag");
            VarianceAssert.IsVariant<NotAsSimpleClass, DateTime>(randomItems, "Created");
            VarianceAssert.IsVariant<NotAsSimpleClass, decimal>(randomItems, "Cost");
            VarianceAssert.IsVariant<NotAsSimpleClass, double>(randomItems, "DoubleValue");
            VarianceAssert.IsVariant<NotAsSimpleClass, float>(randomItems, "FloatValue");
            VarianceAssert.IsVariant<NotAsSimpleClass, Guid>(randomItems, "GuidValue");
            VarianceAssert.IsVariant<NotAsSimpleClass, decimal?>(randomItems, "NullableDecimalValue");
            VarianceAssert.IsVariant<NotAsSimpleClass, byte[]>(randomItems, "ByteArrayValue");
            VarianceAssert.IsVariant<NotAsSimpleClass, SomeValues>(randomItems, "EnumValue");
        }

        public class TestCleverRandomStrings
        {
            public string Email { get; set; }
            public string EmailAddress { get; set; }
            public string Url { get; set; }
            public string Website { get; set; }
            public string Phone { get; set; }
            public string Tel { get; set; }
            public string Mobile { get; set; }
            public string Fax { get; set; }
        }

        public class TestCleverRandomStringsBuilder: GenericBuilder<TestCleverRandomStringsBuilder, TestCleverRandomStrings>
        {
        }

        [Test]
        public void BuildRandom_ShouldAttemptToMakeUsefulStringValues()
        {
            //---------------Set up test pack-------------------
            var sut = TestCleverRandomStringsBuilder.BuildRandom();

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            Assert.IsTrue(LooksLikeEmail(sut.Email), $"{sut.Email} does not look like an email address");
            Assert.IsTrue(LooksLikeEmail(sut.EmailAddress), $"{sut.EmailAddress} does not look like an email address");
            Assert.IsTrue(LooksLikeUrl(sut.Url), $"{sut.Url} does not look like a url");
            Assert.IsTrue(LooksLikeUrl(sut.Website), $"{sut.Website} does not look like a url");
            Assert.IsTrue(IsAllNumeric(sut.Phone), $"{sut.Phone} should be all numeric");
            Assert.IsTrue(IsAllNumeric(sut.Tel), $"{sut.Tel} should be all numeric");
            Assert.IsTrue(IsAllNumeric(sut.Mobile), $"{sut.Mobile} should be all numeric");
            Assert.IsTrue(IsAllNumeric(sut.Fax), $"{sut.Fax} should be all numeric");

            //---------------Test Result -----------------------
        }

        public class TestBooleans
        {
            public bool Enabled { get; set; }
            public bool SomeOtherBoolean { get; set; }
        }

        public class TestBooleansBuilder: GenericBuilder<TestBooleansBuilder, TestBooleans>
        {
        }

        [Test]
        public void BuildRandom_ShouldSetBooleanEnabledPropertyToTrue()
        {
            // special rule which makes dealing with entities which have an Enabled flag
            //  a little less tedious for the user
            //---------------Set up test pack-------------------
            var items = new List<TestBooleans>();

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            for (var i = 0; i < 1000; i++)
            {
                items.Add(TestBooleansBuilder.BuildRandom());
            }

            //---------------Test Result -----------------------
            Assert.IsTrue(items.Select(i => i.Enabled).All(v => v));
            VarianceAssert.IsVariant<TestBooleans, bool>(items, "SomeOtherBoolean");
        }


        private bool IsAllNumeric(string phone)
        {
            return phone.All(c => "0123456789".Contains(c));
        }

        private bool LooksLikeUrl(string url)
        {
            return !string.IsNullOrWhiteSpace(url) &&
                   url.IndexOf("://") > 0 && url.IndexOf("://") < url.Length - 2;
        }

        private bool LooksLikeEmail(string email)
        {
            return !string.IsNullOrWhiteSpace(email) &&
                   email.IndexOf("@") > 0 && email.IndexOf("@") < email.Length - 2 &&
                   email.IndexOf(".") > 0 && email.IndexOf(".") < email.Length - 2;
        }


        private class BuilderInspector : GenericBuilder<BuilderInspector, SimpleClass>
        {
            public static string[] Calls
            {
                get
                {
                    return _calls.ToArray();
                }
            }
            private static List<string> _calls = new List<string>();
            public override BuilderInspector WithRandomProps()
            {
                _calls.Add("WithRandomProps");
                return base.WithRandomProps();
            }

            public override SimpleClass Build()
            {
                _calls.Add("Build");
                return base.Build();
            }
        }
        [Test]
        public void BuildRandom_CallsWithRandomPropsThenBuildAndReturnsResult()
        {
            //---------------Set up test pack-------------------
            Assert.AreEqual(0, BuilderInspector.Calls.Length);
            
            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            BuilderInspector.BuildRandom();

            //---------------Test Result -----------------------
            Assert.AreEqual(2, BuilderInspector.Calls.Length);
            Assert.AreEqual("WithRandomProps", BuilderInspector.Calls[0]);
            Assert.AreEqual("Build", BuilderInspector.Calls[1]);
        }

        public class ComplexMember1
        {
            public string Name { get; set; }
        }

        public class ComplexMember2
        {
            public int Value { get; set; }
        }

        public class ClassWithComplexMembers
        {
            public ComplexMember1 ComplexMember1 { get; set; }
            public virtual ComplexMember2 ComplexMember2 { get; set; }
        }

        private class ClassWithComplexMembersBuilder : GenericBuilder<ClassWithComplexMembersBuilder, ClassWithComplexMembers>
        {
        }

        [Test]
        public void BuildDefault_SetsComplexMembersToNullValue()
        {
            //---------------Set up test pack-------------------
            
            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var obj = ClassWithComplexMembersBuilder.BuildDefault();

            //---------------Test Result -----------------------
            Assert.IsNull(obj.ComplexMember1);
            Assert.IsNull(obj.ComplexMember2);
        }

        [Test]
        public void WithRandomProps_SetsRandomPropertiesForComplexMembersAndTheirProps()
        {
            //---------------Set up test pack-------------------
            
            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var randomItems = new List<ClassWithComplexMembers>();
            for (var i = 0; i < RANDOM_TEST_CYCLES; i++)
            {
                var randomItem = ClassWithComplexMembersBuilder.BuildRandom();
                Assert.IsNotNull(randomItem.ComplexMember1);
                Assert.IsNotNull(randomItem.ComplexMember2);
                randomItems.Add(randomItem);
            }

            //---------------Test Result -----------------------
            Assert.AreEqual(RANDOM_TEST_CYCLES, randomItems.Count);
            VarianceAssert.IsVariant<ClassWithComplexMembers, ComplexMember1>(randomItems, "ComplexMember1");
            VarianceAssert.IsVariant<ClassWithComplexMembers, ComplexMember2>(randomItems, "ComplexMember2");
            var complexMembers1 = randomItems.Select(i => i.ComplexMember1);
            VarianceAssert.IsVariant<ComplexMember1, string>(complexMembers1, "Name");
            var complexMembers2 = randomItems.Select(i => i.ComplexMember2);
            VarianceAssert.IsVariant<ComplexMember2, int>(complexMembers2, "Value");
        }

        [Test]
        public void WhenUsingExistingBuildersWhichWouldCauseStackOverflow_ShouldAttemptToProtectAgainstStackOverflow()
        {
            //---------------Set up test pack-------------------

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            Assert.DoesNotThrow(() => ParentWithBuilderBuilder.BuildRandom());

            //---------------Test Result -----------------------
        }


        public class ParentWithBuilder
        {
            public List<ChildWithBuilder> Children { get; set; }
            public ParentWithBuilder()
            {
                Children = new List<ChildWithBuilder>();
            }
        }
        public class ChildWithBuilder
        {
            public ParentWithBuilder Parent { get; set; }
        }

        public class ParentWithBuilderBuilder: GenericBuilder<ParentWithBuilderBuilder, ParentWithBuilder>
        {
            public override ParentWithBuilderBuilder WithRandomProps()
            {
                return base.WithRandomProps()
                    .WithChild(ChildWithBuilderBuilder.BuildRandom());
            }

            private ParentWithBuilderBuilder WithChild(ChildWithBuilder child)
            {
                return WithProp(o => o.Children.Add(child));
            }
        }

        public class ChildWithBuilderBuilder: GenericBuilder<ChildWithBuilderBuilder, ChildWithBuilder>
        {
        }


        public class Parent
        {
            public Child Child { get; set; }
        }

        public class Child
        {
            public int Id { get; set; }
        }

        public class ParentBuilder: GenericBuilder<ParentBuilder, Parent>
        {
        }

        public class ChildBuilder : GenericBuilder<ChildBuilder, Child>
        {
            public override ChildBuilder WithRandomProps()
            {
                return base.WithRandomProps()
                    .WithProp(o => o.Id = 1337);
            }
        }


        [Test]
        public void WithRandomProps_ShouldReuseKnownBuildersFromSameAssemblyAsType()
        {
            //---------------Set up test pack-------------------

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = ParentBuilder.BuildRandom();

            //---------------Test Result -----------------------
            Assert.AreEqual(1337, result.Child.Id);
        }

        public class EmailBuilder : GenericBuilder<EmailBuilder, Email>
        {
            public override EmailBuilder WithRandomProps()
            {
                return base.WithRandomProps()
                    .WithProp(o => o.Subject = "local is lekker");
            }
        }

        public class EmailRecipientBuilder : GenericBuilder<EmailRecipientBuilder, EmailRecipient>
        {
        }

        [Test]
        public void WithRandomProps_ShouldReuseKnownBuildersFromAllLoadedAssembliesWhenNoBuilderInTypesAssembly()
        {
            //---------------Set up test pack-------------------

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = EmailRecipientBuilder.BuildRandom();

            //---------------Test Result -----------------------
            Assert.AreEqual("local is lekker", result.Email.Subject);
        }

        [TestCase("foo", "foo", 0)]
        [TestCase("bar", "foo", -1)]
        [TestCase("foo", "bar", 1)]
        [TestCase("foo.bar", "foo.bar", 0)]
        [TestCase("foo.bar", "foo.bar.tests", -1)]
        [TestCase("foo.bar", "foo.bar.tests.part2", -2)]
        [TestCase("foo.bar.tests", "foo.bar", 1)]
        [TestCase("foo.bar.tests.part2", "foo.bar", 2)]
        public void MatchIndexFor_GivenArrays_ShouldReturnExpectedResult(string left, string right, int expected)
        {
            //---------------Set up test pack-------------------
            var leftParts = left.Split('.');
            var rightParts = right.Split('.');

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = leftParts.MatchIndexFor(rightParts);

            //---------------Test Result -----------------------
            Assert.AreEqual(expected, result);
        }

        // real-world usage, has inner exception about defining duplicate dynamic module
        public class FakeMessagePlatformSender
        {
            public string Address { get; set; }
            public string ReplyTo { get; set; }
        }
        public class FakeMessagePlatformRecipient
        {
            public string RecipientType { get; set; }
            public string RecipientIdentity { get; set; }
            public string Address { get; set; }
        }
        public class FakeMessagePlatformData
        {
            public int ClientID { get; set; }
            public IEnumerable<FakeMessagePlatformOption> Options { get; set; }
        }

        public class FakeMessageData
        {
            public FakeMessagePlatformSender Sender { get; set; }
            public IEnumerable<FakeMessagePlatformRecipient> Recipients { get; set; }
            public FakeMessagePlatformData Message { get; set; }
            public IEnumerable<string> Protocols { get; set; }
        }

        public class FakeMessagePlatformOption
        {
            public string Name { get; set; }
            public object Value { get; set; }
        }

        public class FakeMessagePlatformOptionBuilder: GenericBuilder<FakeMessagePlatformOptionBuilder, FakeMessagePlatformOption>
        {
        }

        public class FakeMessageDataBuilder: GenericBuilder<FakeMessageDataBuilder, FakeMessageData>
        {
            public override FakeMessageDataBuilder WithRandomProps()
            {
                return base.WithRandomProps()
                    .WithRandomOptions()
                    .WithRandomRecipients()
                    .WithRandomProtocols();
            }

            private FakeMessageDataBuilder WithRandomOptions()
            {
                return WithProp(o => o.Message.Options = RandomValueGen.GetRandomCollection(FakeMessagePlatformOptionBuilder.BuildRandom, 2));
            }

            public FakeMessageDataBuilder AsOneProtocolToOneRecipient()
            {
                return WithRandomProtocols(1, 1)
                    .WithRandomRecipients(1, 1);
            }

            public FakeMessageDataBuilder WithRandomProtocols(int min = 1, int max = 10)
            {
                return WithProp(o => o.Protocols = RandomValueGen.GetRandomCollection(() => RandomValueGen.GetRandomString(), min, max));
            }

            public FakeMessageDataBuilder WithRandomRecipients(int min = 1, int max = 10)
            {
                return WithProp(o => o.Recipients = 
                                    RandomValueGen.GetRandomCollection(FakeMessagePlatformRecipientBuilder.BuildRandom, min, max));
            }

            public FakeMessageDataBuilder WithOption(string name, string value)
            {
                return WithProp(o => o.Message.Options = o.Message.Options
                                                                    .EmptyIfNull()
                                                                    .And(new FakeMessagePlatformOption() {Name = name, Value = value}));
            }
        }

        public class FakeMessagePlatformRecipientBuilder: GenericBuilder<FakeMessagePlatformRecipientBuilder, FakeMessagePlatformRecipient>
        {
        }

        [Test]
        public void ShouldNotThrow()
        {
            //---------------Set up test pack-------------------

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var testData = FakeMessageDataBuilder.Create()
                                .WithRandomProps()
                                .WithOption("message", "hello world")
                                .WithOption("user", "bob saget")
                                .WithOption("AnotherOption", "wibble socks")
                                .AsOneProtocolToOneRecipient()
                                .Build();

            //---------------Test Result -----------------------
        }

        //-- end real-world error example

        public class SomePOCOWithCollection
        {
            public virtual ICollection<string> Strings { get; set; }
        }
        public class SomePOCOWithCollectionBuilder: GenericBuilder<SomePOCOWithCollectionBuilder, SomePOCOWithCollection>
        {
        }

        [Test]
        public void ICollectionShouldBeCreatedEmptyAndNotNull()
        {
            //---------------Set up test pack-------------------
            var sut = SomePOCOWithCollectionBuilder.BuildRandom();
            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = sut.Strings;

            //---------------Test Result -----------------------
            Assert.IsNotNull(result);
            CollectionAssert.IsEmpty(result);
        }

        public class SomePOCOWithArray
        {
            public virtual string[] Strings { get; set; }
        }
        public class SomePOCOWithArrayBuilder: GenericBuilder<SomePOCOWithArrayBuilder, SomePOCOWithArray>
        {
        }

        [Test]
        public void ArrayShouldBeCreatedEmptyAndNotNull()
        {
            //---------------Set up test pack-------------------
            var sut = SomePOCOWithArrayBuilder.BuildRandom();
            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = sut.Strings;

            //---------------Test Result -----------------------
            Assert.IsNotNull(result);
            CollectionAssert.IsEmpty(result);
        }

        public class SomePOCOWithList
        {
            public virtual List<string> Strings { get; set; }
        }
        public class SomePOCOWithListBuilder: GenericBuilder<SomePOCOWithListBuilder, SomePOCOWithList>
        {
        }

        [Test]
        public void ListShouldBeCreatedEmptyAndNotNull()
        {
            //---------------Set up test pack-------------------
            var sut = SomePOCOWithListBuilder.BuildRandom();
            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = sut.Strings;

            //---------------Test Result -----------------------
            Assert.IsNotNull(result);
            CollectionAssert.IsEmpty(result);
        }

        [Test]
        public void WhenUsing_WithFilledCollections_ShouldPutDataInCollections()
        {
            //---------------Set up test pack-------------------
            var sut = SomePOCOWithListBuilder.Create().WithRandomProps().WithFilledCollections().Build();

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = sut.Strings;

            //---------------Test Result -----------------------
            Assert.IsNotNull(result);
            CollectionAssert.IsNotEmpty(result);
        }


    }
}
