using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using CompiledValidators.DataAnnotations;
using NUnit.Framework;

namespace CompiledValidators.Tests.DataAnnotations
{
    [TestFixture]
    public class ErrorMessageTests
    {
        private Validator _sut;

        [TestFixtureSetUp]
        public void SetUp()
        {
            _sut = new Validator(true, new UserAssemblyRecursionPolicy(), new DataAnnotationsValidatorProvider(), new ValidatableObjectValidationExpressionConverter(), new DefaultValidationExpressionConverter());
        }

        [Test]
        public void ReportsErrorsFromValidationAttributes()
        {
            var result = _sut.Validate(new TestType1(), false).Single();
            Assert.AreEqual("root.Value1", result.MemberName);
            Assert.AreEqual("foo", result.ErrorMessage);
        }

        [Test]
        public void ReportsMemberErrorsFromValidatableObjects()
        {
            var result = _sut.Validate(new TestType2(), false).Single();
            Assert.AreEqual("root.Value1", result.MemberName);
            Assert.AreEqual("bar", result.ErrorMessage);
        }

        [Test]
        public void ReportsSelfErrorsFromValidatableObject()
        {
            var result = _sut.Validate(new TestType3(), false).Single();
            Assert.AreEqual("root", result.MemberName);
            Assert.AreEqual("something", result.ErrorMessage);
        }

        [Test]
        public void ReportsChildMemberValidatableObjectErrors()
        {
            var result = _sut.Validate(new TestType4(), false).Single();
            Assert.AreEqual("root.Value1.Value1", result.MemberName);
            Assert.AreEqual("bar", result.ErrorMessage);
        }

        [Test]
        public void ReportsChildValidatableObjectErrors()
        {
            var result = _sut.Validate(new TestType5(), false).Single();
            Assert.AreEqual("root.Value1", result.MemberName);
            Assert.AreEqual("something", result.ErrorMessage);
        }

        private class TestType1
        {
            [Invalid("foo")]
            public int Value1 = 4;
        }

        private class TestType2 : IValidatableObject
        {
            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                return new[] { new ValidationResult("bar", new[] { "Value1" }) };
            }
        }

        private class TestType3 : IValidatableObject
        {
            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                return new[] { new ValidationResult("something") };
            }
        }

        private class TestType4 : IValidatableObject
        {
            public TestType2 Value1 = new TestType2();

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                return new ValidationResult[0];
            }
        }

        private class TestType5 : IValidatableObject
        {
            public TestType3 Value1 = new TestType3();

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                return new ValidationResult[0];
            }
        }

        private class InvalidAttribute : ValidationAttribute
        {
            private readonly string _errorFormat;

            public InvalidAttribute(string errorFormat)
            {
                _errorFormat = errorFormat;
            }

            public override bool IsValid(object value)
            {
                return false;
            }

            public override string FormatErrorMessage(string name)
            {
                return string.Format(_errorFormat, name);
            }
        }
    }
}
