using CompiledValidators.Tests.Infrastructure;
using NUnit.Framework;

namespace CompiledValidators.Tests
{
    [TestFixture, Ignore]
    public class RecursionTests
    {
        private Validator _sut;

        [TestFixtureSetUp]
        public void SetUp()
        {
            _sut = new Validator(true, new UserAssemblyRecursionPolicy(), new AttributeValidatorProvider(), new ValidationExpressionConverter());
        }

        [Test, Timeout(500)] // timeout prevents infinite recursion.
        public void ValidatesLeaf()
        {
            Assert.IsTrue(_sut.IsValid(new Node()));
        }

        public class Node
        {
            public Node Left;

            public Node Right;
        }
    }
}
