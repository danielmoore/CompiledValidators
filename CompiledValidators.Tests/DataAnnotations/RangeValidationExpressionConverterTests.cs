using CompiledValidators.DataAnnotations;
using NUnit.Framework;
using RangeAttribute = System.ComponentModel.DataAnnotations.RangeAttribute;

namespace CompiledValidators.Tests.DataAnnotations
{
    [TestFixture]
    public class RangeValidationExpressionConverterTests
    {
        private RangeValidationExpressionConverter _sut;
        private static readonly RangeAttribute IntValidator = new RangeAttribute(5, 10);
        private static readonly RangeAttribute DoubleValidator = new RangeAttribute(5d, 10d);
        private static readonly RangeAttribute StringIntValidator = new RangeAttribute(typeof(int), "5", "10");

        [TestFixtureSetUp]
        public void SetUp()
        {
            _sut = new RangeValidationExpressionConverter();
        }

        [Test]
        public void CanConvertRangeAttributes()
        {
            Assert.IsTrue(_sut.CanConvert(IntValidator, typeof(int)));
        }

        [Test]
        public void DoesNotAllowLessthanMin()
        {
            Assert.IsFalse(_sut.Execute(IntValidator, 4));
        }

        [Test]
        public void DoesNotAllowGreaterThanMax()
        {
            Assert.IsFalse(_sut.Execute(IntValidator, 11));
        }

        [Test]
        public void AllowsMax()
        {
            Assert.IsTrue(_sut.Execute(IntValidator, 5));
        }

        [Test]
        public void AllowsMin()
        {
            Assert.IsTrue(_sut.Execute(IntValidator, 10));
        }

        [Test]
        public void CanConvertDouble()
        {
            Assert.IsTrue(_sut.CanConvert(DoubleValidator, typeof(double)));
        }

        [Test]
        public void CannotConvertDoubleWithIntValidator()
        {
            Assert.IsFalse(_sut.CanConvert(IntValidator, typeof(double)));
        }

        [Test]
        public void CanConvertStringIntValidator()
        {
            Assert.IsTrue(_sut.CanConvert(StringIntValidator, typeof(int)));
        }

        [Test]
        public void ConvertsStringToInt()
        {
            Assert.IsTrue(_sut.Execute(StringIntValidator, 7));
        }

        [Test]
        public void NonOperandTypesAreValid()
        {
            Assert.IsTrue(_sut.Execute(IntValidator, 2d));
        }
    }
}
