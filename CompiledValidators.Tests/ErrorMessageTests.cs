using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;

namespace CompiledValidators.Tests
{
    public class ErrorMessageTests
    {
        private Validator _sut;

        [TestFixtureSetUp]
        public void SetUp()
        {
            _sut = new Validator(true, new UserAssemblyRecursionPolicy(), new ValidatorProvider(), new ValidationExpressionConverter());
        }

        [Test]
        public void ReportsCorrectErrorMessages()
        {
            var testObj = new TestType();
            var results = _sut.Validate(testObj, false);
            Assert.AreEqual(2, results.Count());

            var value1Error = results.Single(r => r.MemberName == "root.Value1");
            var value2Error = results.Single(r => r.MemberName == "root.Value2");
            Assert.AreEqual(TestType.Value1ErrorMessage, value1Error.ErrorMessage);
            Assert.AreEqual(TestType.Value2ErrorMessage, value2Error.ErrorMessage);
        }

        [Test]
        public void ResolvesDynamicErrors()
        {
            var result = _sut.Validate(new TestType2(), false).Single();
            Assert.AreEqual("root.Value1", result.MemberName);
            Assert.AreEqual("3 is invalid", result.ErrorMessage);
        }

        [Test]
        public void ResolvesLazyErrors()
        {
            var result = _sut.Validate(new TestType3(), false).Single();
            Assert.AreEqual("root.Value1", result.MemberName);
            Assert.AreEqual("Value1 is invalid", result.ErrorMessage);
        }

        private class TestType
        {
            public const string Value1ErrorMessage = "Value1";
            [Invalid(Value1ErrorMessage)]
            public object Value1 = new object();

            public const string Value2ErrorMessage = "Value2";
            [Invalid(Value2ErrorMessage)]
            public object Value2 = new object();
        }

        private class TestType2
        {
            [InvalidInt]
            public int Value1 = 3;
        }

        private class TestType3
        {
            [InvalidMember]
            public int Value1 = 5;
        }

        #region Test Infrastructure

        private class ValidatorProvider : IValidatorProvider
        {
            public IEnumerable<ValidatorInfo> GetValidators(MemberInfo member)
            {
                foreach (var attr in member.GetCustomAttributes(false))
                {
                    var invalidAttr = attr as InvalidAttribute;
                    if (invalidAttr != null)
                        yield return new ErrorMessageValidatorInfo(invalidAttr, invalidAttr.ErrorMessage);
                    else
                    {
                        var invalidMemberAttribute = attr as InvalidMemberAttribute;
                        if (invalidMemberAttribute != null)
                            yield return new ErrorMessageValidatorInfo(attr, () => invalidMemberAttribute.GetErrorMessage(member.Name));
                        else
                        {
                            var invalidIntAttr = attr as InvalidIntAttribute;
                            if (invalidIntAttr != null)
                                yield return new MemberErrorValidatorInfo(invalidIntAttr, o => new[] { new MemberValidationErrorMessage(null, invalidIntAttr.GetErrorMessage((int)((FieldInfo)member).GetValue(o))) });
                        }
                    }
                }
            }
        }

        private class ValidationExpressionConverter : IValidationExpressionConverter
        {
            public Expression Convert(object validator, Expression member)
            {
                return Expression.Constant(false);
            }

            public bool CanConvert(object validator, Type memberType)
            {
                return validator is InvalidAttribute || validator is InvalidIntAttribute || validator is InvalidMemberAttribute;
            }
        }

        private class InvalidAttribute : Attribute
        {
            public InvalidAttribute(string errorMessage)
            {
                ErrorMessage = errorMessage;
            }

            public string ErrorMessage { get; private set; }
        }

        private class InvalidMemberAttribute : Attribute
        {
            public string GetErrorMessage(string memberName)
            {
                return string.Format("{0} is invalid", memberName);
            }
        }

        private class InvalidIntAttribute : Attribute
        {
            public string GetErrorMessage(int value)
            {
                return string.Format("{0} is invalid", value);
            }
        }

        #endregion
    }
}
