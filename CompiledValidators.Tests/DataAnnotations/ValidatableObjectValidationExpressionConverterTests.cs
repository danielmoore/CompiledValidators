using System.ComponentModel.DataAnnotations;
using CompiledValidators.DataAnnotations;
using Moq;
using NUnit.Framework;

namespace CompiledValidators.Tests.DataAnnotations
{
    [TestFixture]
    public class ValidatableObjectValidationExpressionConverterTests
    {
        private static readonly IValidatableObject ValidatableObject = Mock.Of<IValidatableObject>();
        private ValidatableObjectValidationExpressionConverter _sut;

        [TestFixtureSetUp]
        public void SetUp()
        {
            _sut = new ValidatableObjectValidationExpressionConverter();
        }

        [Test]
        public void CanConvertValidatableObejct()
        {
            Assert.IsTrue(_sut.CanConvert(null, typeof(IValidatableObject)));
        }

        [Test]
        public void CallsIsValid()
        {
            _sut.Execute(null, ValidatableObject);

            ValidatableObject.GetMock().Verify(m => m.Validate(It.IsAny<ValidationContext>()));
        }
    }
}
