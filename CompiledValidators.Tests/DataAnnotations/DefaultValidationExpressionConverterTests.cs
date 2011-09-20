using System.ComponentModel.DataAnnotations;
using CompiledValidators.DataAnnotations;
using Moq;
using NUnit.Framework;

namespace CompiledValidators.Tests.DataAnnotations
{
    [TestFixture]
    public class DefaultValidationExpressionConverterTests
    {
        private static readonly ValidationAttribute Validator = Mock.Of<ValidationAttribute>();
        private DefaultValidationExpressionConverter _sut;

        [TestFixtureSetUp]
        public void SetUp()
        {
            _sut = new DefaultValidationExpressionConverter();
        }

        [Test]
        public void CallsTheIsValidMethod()
        {
            var validationObj = new object();

            _sut.Execute(Validator, validationObj);

            Validator.GetMock().Verify(m => m.IsValid(validationObj));
        }

        [Test]
        public void CanConvertValidationAttributes()
        {
            Assert.IsTrue(_sut.CanConvert(Validator, typeof(object)));
        }
    }
}
