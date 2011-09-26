using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;

namespace CompiledValidators.Tests
{
    [TestFixture]
    public class SimpleValidationTests
    {
        private Validator _sut;

        [TestFixtureSetUp]
        public void SetUp()
        {
            _sut = new Validator(true, new UserAssemblyRecursionPolicy(), new AttributeValidatorProvider(), new ValidationExpressionConverter());
        }

        [Test]
        public void NullIsInvalid()
        {
            Assert.IsFalse(_sut.IsValid<object>(null));
        }

        [Test]
        public void NullReturnsRootInvalidError()
        {
            Assert.AreEqual(Validator.RootNullValidationError, _sut.ValidateToFirstError<object>(null).Single());
        }

        [Test]
        public void EmptyTypesAreValid()
        {
            Assert.IsTrue(_sut.IsValid(new EmptyClass()));
        }

        [Test]
        public void TypeWithNoValidatorsAreValid()
        {
            Assert.IsTrue(_sut.IsValid(new NoValidatorsClass()));
        }

        [Test]
        public void TypesWithOneInvalidFieldAreInvalid()
        {
            Assert.IsFalse(_sut.IsValid(new PartialValidClass()));
        }

        [Test]
        public void TypesWithValidFieldsAreValid()
        {
            Assert.IsTrue(_sut.IsValid(new ValidClass()));
        }

        [Test]
        public void TypeWithInvalidMmebersAreInvalid()
        {
            Assert.IsFalse(_sut.IsValid(new ValidClassWithInvalidMember()));
        }

        [Test]
        public void TypeWithValidMembersAreValid()
        {
            Assert.IsTrue(_sut.IsValid(new ValidClassWithValidMember()));
        }

        [Test]
        public void TypesWithEmptyMembersAreValid()
        {
            Assert.IsTrue(_sut.IsValid(new ValidClassWithEmptyMember()));
        }

        [Test]
        public void TypesWithNullMembersAreValid()
        {
            Assert.IsTrue(_sut.IsValid(new ValidClassWithNullMember()));
        }

        [Test]
        public void TypesWithValidMemberListsAreValid()
        {
            Assert.IsTrue(_sut.IsValid(new ValidClassWithValidMemberList()));
        }

        [Test]
        public void TypeWithAnyInvalidMembersInMemberListAreInvalid()
        {
            Assert.IsFalse(_sut.IsValid(new ValidClassWithOneInvalidInMemberList()));
        }

        [Test]
        public void SelfInvalidatingTypesAreInvalid()
        {
            Assert.IsFalse(_sut.IsValid(new InvalidEmptyType()));
        }

        [Test]
        public void InvalidClassesWithPropertiesAreInvalid()
        {
            Assert.IsFalse(_sut.IsValid(new InvalidClassWithProperties()));
        }

        [Test]
        public void FirstInvalidObjectIsReturned()
        {
            var obj = new ValidClassWithOneInvalidInMemberList();
            Assert.AreSame(obj.Value2[0], _sut.ValidateToFirstError(obj).Single().Object);
        }

        [Test]
        public void CorrectlyIdentifiesInvalidMember()
        {
            Assert.AreEqual("root.Value1", _sut.ValidateToFirstError(new PartialValidClass()).Single().MemberName);
        }

        [Test]
        public void CorrectlyIdentifiesAllInvalidMembers()
        {
            var obj = new MultiInvalidClass();
            var invalidMembers = new object[] { obj, obj.Value3[0], obj.Value4 };
            var result = _sut.Validate(obj, false).Select(v => v.Object).ToArray();

            Assert.AreEqual(invalidMembers.Length, result.Length);

            for (int i = 0; i < result.Length; i++)
                Assert.AreSame(result[i], invalidMembers[i]);
        }

        [Test]
        public void IgnoresStaticFields()
        {
            Assert.IsTrue(_sut.IsValid(new ValidClassWithInvalidStaticMember()));
        }

        public class EmptyClass
        {
        }

        public class ValidClassWithInvalidStaticMember : ValidClass
        {
            [Invalid]
            public static int Value2;
        }

        public class NoValidatorsClass
        {
            public int Value1;
        }

        public class PartialValidClass
        {
            [Invalid]
            public int Value1;

            [Valid]
            public int Value2;
        }

        public class ValidClass
        {
            [Valid]
            public int Value1;
        }

        public class ValidClassWithInvalidMember : ValidClass
        {
            public PartialValidClass Value2 = new PartialValidClass();
        }

        public class ValidClassWithValidMember : ValidClass
        {
            public ValidClass Value2 = new ValidClass();
        }

        public class ValidClassWithEmptyMember : ValidClass
        {
            public EmptyClass Value2 = new EmptyClass();
        }

        public class ValidClassWithNullMember : ValidClass
        {
            public PartialValidClass Value2;
        }

        public class ValidClassWithValidMemberList : ValidClass
        {
            public List<ValidClass> Value2 = new List<ValidClass>
            {
                new ValidClass(),
                new ValidClass()
            };
        }

        public class ValidClassWithOneInvalidInMemberList : ValidClass
        {
            public List<PartialValidClass> Value2 = new List<PartialValidClass>
            {
                new PartialValidClass()
            };
        }

        public class MultiInvalidClass : PartialValidClass
        {
            public List<PartialValidClass> Value3 = new List<PartialValidClass>
            {
                new PartialValidClass()
            };

            public PartialValidClass Value4 = new PartialValidClass();
        }

        [Invalid]
        public class InvalidEmptyType
        {
        }

        public class InvalidClassWithProperties
        {
            [Invalid]
            public int Value1 { get; set; }
        }

        #region Test Infrastructure

        private class ValidationExpressionConverter : IValidationExpressionConverter
        {
            public Expression Convert(object validator, System.Linq.Expressions.Expression member)
            {
                return validator is ValidAttribute ? Expression.Constant(true) : Expression.Constant(false);
            }

            public bool CanConvert(object validator, Type memberType)
            {
                return validator is ValidAttribute || validator is InvalidAttribute;
            }
        }

        private class ValidAttribute : Attribute { }
        private class InvalidAttribute : Attribute { }

        #endregion
    }
}
