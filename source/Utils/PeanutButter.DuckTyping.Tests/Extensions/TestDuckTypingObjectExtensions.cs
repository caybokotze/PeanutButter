﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using PeanutButter.DuckTyping.Extensions;
using PeanutButter.Utils;
using static PeanutButter.RandomGenerators.RandomValueGen;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace PeanutButter.DuckTyping.Tests.Extensions
{
    [TestFixture]
    public class TestDuckTypingObjectExtensions : AssertionHelper
    {
        public interface IHasReadOnlyName
        {
            string Name { get; }
        }

        [Test]
        public void CanDuckAs_GivenTypeWithOnePropertyAndObjectWhichDoesNotImplement_ShouldReturnFalse()
        {
            //--------------- Arrange -------------------
            var obj = new {
                Id = GetRandomInt()
            };

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = obj.CanDuckAs<IHasReadOnlyName>();

            //--------------- Assert -----------------------
            Expect(result, Is.False);
        }

        [Test]
        public void CanDuckAs_GivenTypeWithOnePropertyAndObjectImplementingProperty_ShouldReturnTrue()
        {
            //--------------- Arrange -------------------
            var obj = new {
                Name = GetRandomString()
            };

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = obj.CanDuckAs<IHasReadOnlyName>();

            //--------------- Assert -----------------------
            Expect(result, Is.True);
        }

        public interface IHasReadWriteName
        {
            string Name { get; set; }
        }

        public class HasReadOnlyName
        {
            public string Name { get; }
        }

        [Test]
        public void CanDuckAs_ShouldRequireSameReadWritePermissionsOnProperties()
        {
            //--------------- Arrange -------------------
            var obj = new HasReadOnlyName();

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result1 = obj.CanDuckAs<IHasReadWriteName>();
            var result2 = obj.CanDuckAs<IHasReadOnlyName>();

            //--------------- Assert -----------------------
            Expect(result1, Is.False);
            Expect(result2, Is.True);
        }

        public class HasReadWriteNameAndId
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [Test]
        public void CanDuckAs_ShouldReturnTrueWhenObjectImplementsMoreThanRequiredInterface()
        {
            //--------------- Arrange -------------------
            var obj = new HasReadWriteNameAndId();

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result1 = obj.CanDuckAs<IHasReadOnlyName>();
            var result2 = obj.CanDuckAs<IHasReadWriteName>();

            //--------------- Assert -----------------------
            Expect(result1, Is.True);
            Expect(result2, Is.True);
        }

        public interface ICow
        {
            void Moo();
        }

        public class Duck
        {
            public void Quack()
            {
            }
        }

        [Test]
        public void CanDuckAs_ShouldReturnFalseWhenSrcObjectIsMissingInterfaceMethod()
        {
            //--------------- Arrange -------------------
            var src = new Duck();

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = src.CanDuckAs<ICow>();

            //--------------- Assert -----------------------
            Expect(result, Is.False);
        }

        public class Cow
        {
            public void Moo()
            {
            }
        }

        [Test]
        public void CanDuckAs_ShouldReturnTrueWhenRequiredMethodsExist()
        {
            //--------------- Arrange -------------------
            var src = new Cow();
            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = src.CanDuckAs<ICow>();

            //--------------- Assert -----------------------
            Expect(result, Is.True);
        }

        public class AutoCow
        {
            // ReSharper disable once UnusedParameter.Global
            public void Moo(int howManyTimes)
            {
                /* Empty on purpose */
            }
        }

        [Test]
        public void CanDuckAs_ShouldReturnFalseWhenMethodParametersMisMatch()
        {
            //--------------- Arrange -------------------
            var src = new AutoCow();

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = src.CanDuckAs<ICow>();

            //--------------- Assert -----------------------
            Expect(result, Is.False);
        }


        [Test]
        public void DuckAs_OperatingOnNull_ShouldReturnNull()
        {
            //--------------- Arrange -------------------
            var src = null as object;

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            // ReSharper disable once ExpressionIsAlwaysNull
            var result = src.DuckAs<ICow>();

            //--------------- Assert -----------------------
            Expect(result, Is.Null);
        }

        [Test]
        public void DuckAs_OperatingDuckable_ShouldReturnDuckTypedWrapper()
        {
            //--------------- Arrange -------------------
            var expected = GetRandomString();
            Func<object> makeSource = () =>
                new {
                    Name = expected
                };
            var src = makeSource();

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = src.DuckAs<IHasReadOnlyName>();

            //--------------- Assert -----------------------
            Expect(result, Is.Not.Null);
            Expect(result.Name, Is.EqualTo(expected));
        }

        [Test]
        public void DuckAs_OperatingOnNonDuckable_ShouldReturnNull()
        {
            //--------------- Arrange -------------------
            Func<object> makeSource = () =>
                new {
                    Name = GetRandomString()
                };
            var src = makeSource();
            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = src.DuckAs<IHasReadWriteName>();

            //--------------- Assert -----------------------
            Expect(result, Is.Null);
        }

        [Test]
        public void CanFuzzyDuckAs_OperatingOnSimilarPropertiedThing_ShouldReturnTrue()
        {
            //--------------- Arrange -------------------
            var thing = new { nAmE = GetRandomString() } as object;

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = thing.CanFuzzyDuckAs<IHasReadOnlyName>();

            //--------------- Assert -----------------------
            Assert.IsTrue(result);
        }

        public class LowerCaseCow
        {
            // ReSharper disable once InconsistentNaming
            public void moo()
            {
            }
        }

        [Test]
        public void CanFuzzyDuckAs_OperatingOnSimilarThingWithMethods_ShouldReturnTrue()
        {
            //--------------- Arrange -------------------
            var cow = new LowerCaseCow();

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = cow.CanFuzzyDuckAs<ICow>();

            //--------------- Assert -----------------------
            Assert.IsTrue(result);
        }


        [Test]
        public void FuzzyDuckAs_OperatingOnObjectWhichFuzzyMatchesProperties_ShouldReturnFuzzyDuck()
        {
            //--------------- Arrange -------------------
            var src = new {
                nAmE = GetRandomString()
            } as object;

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = src.FuzzyDuckAs<IHasReadOnlyName>();

            //--------------- Assert -----------------------
            Expect(result, Is.Not.Null);
        }



        [Test]
        public void FuzzyDuckAs_OperatingOnObjectWithFuzzyMatchingMethods_ShouldReturnFuzzyDuck()
        {
            //--------------- Arrange -------------------
            var src = new LowerCaseCow();

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = src.FuzzyDuckAs<ICow>();

            //--------------- Assert -----------------------
            Expect(result, Is.Not.Null);
        }

        public interface ISomeActivityParameters : IActivityParameters<Guid>
        {
        }

        [Test]
        public void DuckAs_ShouldNotBeConfusedByInterfaceInheritence()
        {
            //--------------- Arrange -------------------
            var src = new {
                ActorId = Guid.NewGuid(),
                TaskId = Guid.NewGuid(),
                Payload = Guid.NewGuid()
            };

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = src.DuckAs<ISomeActivityParameters>();

            //--------------- Assert -----------------------
            Expect(() => result.ActorId, Throws.Nothing);
            Expect(() => result.TaskId, Throws.Nothing);
            Expect(() => result.Payload, Throws.Nothing);
        }

        public interface IInterfaceWithPayload
        {
            object Payload { get; set; }
        }

        [Test]
        public void DuckAs_ShouldNotSmashPropertiesOnObjectType()
        {
            //--------------- Arrange -------------------
            var input = new {
                Payload = new {
                    Id = 1,
                    Name = "Moosicle"
                }
            };

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = input.DuckAs<IInterfaceWithPayload>();

            //--------------- Assert -----------------------
            var props = result.Payload.GetType().GetProperties();
            Expect(props.Select(p => p.Name), Does.Contain("Id"));
            Expect(props.Select(p => p.Name), Does.Contain("Name"));
        }

        public interface IInterfaceWithInterfacedPayload
        {
            IInterfaceWithPayload OuterPayload { get; set; }
        }

        [Test]
        public void DuckAs_ShouldNotAllowNonDuckableSubType()
        {
            //--------------- Arrange -------------------
            var input = new {
                OuterPayload = new {
                    Color = "Red"
                }
            };

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = input.DuckAs<IInterfaceWithInterfacedPayload>();

            //--------------- Assert -----------------------
            Assert.IsNotNull(result);
            Assert.IsNull(result.OuterPayload);
        }

        public interface IObjectIdentifier
        {
            object Identifier { get; }
        }
        public interface IGuidIdentifier
        {
            Guid Identifier { get; }
        }

        [Test]
        public void CanDuckAs_ShouldNotTreatGuidAsObject()
        {
            //--------------- Arrange -------------------
            var inputWithGuid = new {
                Identifier = new Guid(),
            };
            var inputWithObject = new {
                Identifier = new object()
            };

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            Expect(inputWithGuid.CanDuckAs<IObjectIdentifier>(), Is.True);
            Expect(inputWithGuid.CanDuckAs<IGuidIdentifier>(), Is.True);
            Expect(inputWithObject.CanDuckAs<IGuidIdentifier>(), Is.False);
            Expect(inputWithObject.CanDuckAs<IObjectIdentifier>(), Is.True);

            //--------------- Assert -----------------------
        }

        [Test]
        public void CanFuzzyDuckAs_ShouldNotTreatGuidAsObject()
        {
            //--------------- Arrange -------------------
            var inputWithGuid = new {
                identifier = new Guid(),
            };
            var inputWithObject = new {
                identifier = new object()
            };

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            Expect(inputWithGuid.CanFuzzyDuckAs<IObjectIdentifier>(), Is.True);
            Expect(inputWithGuid.CanFuzzyDuckAs<IGuidIdentifier>(), Is.True);
            Expect(inputWithObject.CanFuzzyDuckAs<IGuidIdentifier>(), Is.False);
            Expect(inputWithObject.CanFuzzyDuckAs<IObjectIdentifier>(), Is.True);

            //--------------- Assert -----------------------
        }

        public interface IWithStringId
        {
            string Id { get; set; }
        }

        [Test]
        public void FuzzyDuckAs_WhenReadingProperty_ShouldBeAbleToConvertBetweenGuidAndString()
        {
            //--------------- Arrange -------------------
            var input = new WithGuidId()
            {
                id = Guid.NewGuid()
            };
            var expected = input.id.ToString();

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var ducked = input.FuzzyDuckAs<IWithStringId>();

            //--------------- Assert -----------------------
            Assert.IsNotNull(ducked);
            Expect(ducked.Id, Is.EqualTo(expected));
        }

        public interface IWithGuidId
        {
            Guid Id { get; set; }
        }
        public class WithGuidId
        {
            public Guid id { get; set; }
        }
        public class WithStringId
        {
            public string id { get; set; }
        }

        [Test]
        public void FuzzyDuckAs_WhenReadingProperty_ShouldBeAbleToConvertFromStringToGuid()
        {
            //--------------- Arrange -------------------
            var expected = Guid.NewGuid();
            var input = new WithStringId()
            {
                id = expected.ToString()
            };

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var ducked = input.FuzzyDuckAs<IWithGuidId>();

            //--------------- Assert -----------------------
            Expect(ducked, Is.Not.Null);
            Expect(ducked.Id, Is.EqualTo(expected));
        }

        [Test]
        public void FuzzyDuckAs_WhenWritingProperty_ShouldBeAbleToConvertFromGuidToString()
        {
            //--------------- Arrange -------------------
            var newValue = Guid.NewGuid();
            var expected = newValue.ToString();
            var input = new WithStringId()
            {
                id = GetRandomString()
            };

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var ducked = input.FuzzyDuckAs<IWithGuidId>();

            //--------------- Assert -----------------------
            Expect(ducked, Is.Not.Null);
            ducked.Id = newValue;
            Expect(input.id, Is.EqualTo(expected));
            Expect(ducked.Id, Is.EqualTo(newValue));
        }

        [Test]
        public void FuzzyDuckAs_WhenWritingProperty_ShouldBeAbleToConvertFromValidGuidStringToGuid()
        {
            //--------------- Arrange -------------------
            var newGuid = Guid.NewGuid();
            var newValue = newGuid.ToString();
            var input = new WithGuidId();

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var ducked = input.FuzzyDuckAs<IWithStringId>();

            //--------------- Assert -----------------------
            Expect(ducked, Is.Not.Null);
            ducked.Id = newValue;
            Expect(ducked.Id, Is.EqualTo(newValue));
            Expect(input.id, Is.EqualTo(newGuid));
        }



        public interface IHasAnActorId
        {
            Guid ActorId { get; }
        }
        public interface IActivityParametersInherited : IHasAnActorId
        {
            Guid TaskId { get; }
        }

        [Test]
        public void CanFuzzyDuckAs_ShouldFailWhenExpectedToFail()
        {
            //--------------- Arrange -------------------
            var parameters = new {
                travellerId = new Guid(),   // should be actorId!
                taskId = new Guid()
            };

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = parameters.CanFuzzyDuckAs<IActivityParametersInherited>();

            //--------------- Assert -----------------------
            Expect(result, Is.False);
        }

        public interface IDictionaryInner
        {
            string Name { get; set; }
        }

        public interface IDictionaryOuter
        {
            int Id { get; set; }
            IDictionaryInner Inner { get; set; }
        }


        [Test]
        public void CanDuckAs_OperatingOnSingleLevelDictionaryOfStringAndObject_WhenAllPropertiesAreFound_ShouldReturnTrue()
        {
            //--------------- Arrange -------------------
            var expected = GetRandomString();
            var data = new Dictionary<string, object>()
            {{"Name", expected}};

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = data.CanDuckAs<IDictionaryInner>();

            //--------------- Assert -----------------------
            Expect(result, Is.True);
        }

        [Test]
        public void CanDuckAs_OperatingOnSingleLevelDictionaryOfStringAndObject_WhenNullablePropertyIsFound_ShouldReturnTrue()
        {
            //--------------- Arrange -------------------
            var data = new Dictionary<string, object>()
            {{"Name", null}};

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = data.CanDuckAs<IDictionaryInner>();

            //--------------- Assert -----------------------
            Expect(result, Is.True);
        }

        public interface IHaveId
        {
            int Id { get; set; }
        }

        [Test]
        public void CanDuckAs_OperatingOnSingleLevelDictionaryOfStringAndObject_WhenNonNullablePropertyIsFoundAsNull_ShouldReturnFalse()
        {
            //--------------- Arrange -------------------
            var data = new Dictionary<string, object>()
            {{"Id", null}};

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = data.CanDuckAs<IHaveId>();

            //--------------- Assert -----------------------
            Expect(result, Is.False);
        }

        [Test]
        public void CanDuckAs_OperatingOnMultiLevelDictionary_WhenAllPropertiesFound_ShouldReturnTrue()
        {
            //--------------- Arrange -------------------
            var data = new Dictionary<string, object>()
            {
                { "Id", GetRandomInt() },
                { "Inner", new Dictionary<string, object>() { { "Name", GetRandomString() } } }
            };

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = data.CanDuckAs<IDictionaryOuter>();

            //--------------- Assert -----------------------
            Expect(result, Is.True);
        }

        [Test]
        public void DuckAs_OperatingOnDictionaryOfStringAndObject_WhenIsDuckable_ShouldDuck()
        {
            //--------------- Arrange -------------------
            var expectedId = GetRandomInt();
            var expectedName = GetRandomString();
            var input = new Dictionary<string, object>()
            {
                { "Id", expectedId },
                { "Inner", new Dictionary<string, object>() { {  "Name", expectedName } } }
            };
            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = input.DuckAs<IDictionaryOuter>();

            //--------------- Assert -----------------------
            Expect(result, Is.Not.Null);
            Expect(result.Id, Is.EqualTo(expectedId));
            Expect(result.Inner, Is.Not.Null);
            Expect(result.Inner.Name, Is.EqualTo(expectedName));
        }

        [Test]
        public void CanFuzzyDuckAs_OperatingOnAppropriateCaseInsensitiveDictionary_ShouldReturnTrue()
        {
            //--------------- Arrange -------------------
            var expectedId = GetRandomInt();
            var expectedName = GetRandomString();
            var input = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "id", expectedId },
                { "inner", new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { {  "nAmE", expectedName } } }
            };

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = input.CanFuzzyDuckAs<IDictionaryOuter>();

            //--------------- Assert -----------------------
            Expect(result, Is.True);
        }

        [Test]
        public void FuzzyDuckAs_OperatingOnCaseInsensitiveDictionary_ShouldWork()
        {
            //--------------- Arrange -------------------
            var expectedId = GetRandomInt();
            var expectedName = GetRandomString();
            var input = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "id", expectedId },
                { "inner", new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { {  "nAmE", expectedName } } }
            };

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = input.FuzzyDuckAs<IDictionaryOuter>();

            //--------------- Assert -----------------------
            Expect(result, Is.Not.Null);
            Expect(result.Id, Is.EqualTo(expectedId));
            Expect(result.Inner, Is.Not.Null);
            Expect(result.Inner.Name, Is.EqualTo(expectedName));
        }


        [Test]
        public void CanFuzzyDuckAs_OperatingOnWouldBeAppropriateCaseSensitiveDictionary_ShouldReturnTrue()
        {
            //--------------- Arrange -------------------
            var expectedId = GetRandomInt();
            var expectedName = GetRandomString();
            var input = new Dictionary<string, object>()
            {
                { "id", expectedId },
                { "inner", new Dictionary<string, object>() { {  "nAmE", expectedName } } }
            };

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = input.CanFuzzyDuckAs<IDictionaryOuter>();

            //--------------- Assert -----------------------
            Expect(result, Is.True);
        }

        [Test]
        public void FuzzyDuckAs_OperatingOnCaseDifferentCaseSensitiveDictionary_ShouldReturnObject()
        {
            //--------------- Arrange -------------------
            var expectedId = GetRandomInt();
            var expectedName = GetRandomString();
            var input = new Dictionary<string, object>()
            {
                { "id", expectedId },
                { "inner", new Dictionary<string, object>() { {  "nAmE", expectedName } } }
            };

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = input.FuzzyDuckAs<IDictionaryOuter>();

            //--------------- Assert -----------------------
            Expect(result, Is.Not.Null);
            Expect(result.Id, Is.EqualTo(expectedId));
            Expect(result.Inner, Is.Not.Null);
            Expect(result.Inner.Name, Is.EqualTo(expectedName));
        }

        [Test]
        public void InstanceOf_IssueSeenInWildShouldNotHappen()
        {
            //--------------- Arrange -------------------
            var instance = new ActivityParameters<string>(Guid.Empty, Guid.Empty, "foo");

            //--------------- Assume ----------------

            //--------------- Act ----------------------
            var result = instance.DuckAs<ISpecificActivityParameters>();

            //--------------- Assert -----------------------
            Assert.IsNotNull(result);
        }


        public interface IActivityParameters
        {
            Guid ActorId { get; }
            Guid TaskId { get; }
            void DoNothing();
        }
        public interface IActivityParameters<T> : IActivityParameters
        {
            T Payload { get; }
        }
        public interface ISpecificActivityParameters : IActivityParameters<string>
        {
        }
        public class ActivityParameters : IActivityParameters
        {
            public Guid ActorId { get; }
            public Guid TaskId { get; }
            public void DoNothing()
            {
                /* does nothing */
            }

            public ActivityParameters(Guid actorId, Guid taskId)
            {
                ActorId = actorId;
                TaskId = taskId;
            }
        }

        public class ActivityParameters<T> : ActivityParameters, IActivityParameters<T>
        {
            public T Payload { get; set; }

            public ActivityParameters(Guid actorId, Guid taskId, T payload)
                : base(actorId, taskId)
            {
                Payload = payload;
            }
        }

        public interface ICreateMe
        {
            int Id { get; set; }
            string Name { get; set; }
        }
    }
}
