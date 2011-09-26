using System.ComponentModel;
using Moq;
using NUnit.Framework;

namespace CompiledValidators.Tests
{
    [TestFixture]
    public class DataErrorInfoValidationExpressionConverterTests
    {
        private DataErrorInfoValidationExpressionConverter _sut;

        [TestFixtureSetUp]
        public void SetUp()
        {
            _sut = new DataErrorInfoValidationExpressionConverter();
        }

        [Test]
        public void CanConvertDataErrorInfoTypes()
        {
            Assert.IsTrue(_sut.CanConvert(null, Mock.Of<IDataErrorInfo>().GetType()));
        }

        [Test]
        public void DataErrorInfoTypesWithErrorsAreInvalid()
        {
            Assert.IsFalse(_sut.Execute(null, Mock.Of<IDataErrorInfo>(m => m.Error == "foo")));
        }

        [Test]
        public void DataErrorInfoTypesWithoutErrorsAreValid()
        {
            Assert.IsTrue(_sut.Execute(null, Mock.Of<IDataErrorInfo>(m => m.Error == string.Empty)));
        }
    }
}
