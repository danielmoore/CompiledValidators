using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using CompiledValidators.DataAnnotations;
using NUnit.Framework;
using DAValidator = System.ComponentModel.DataAnnotations.Validator;
using RangeAttribute = System.ComponentModel.DataAnnotations.RangeAttribute;

namespace CompiledValidators.Tests
{
    [TestFixture]
    public class Performance
    {
        private const int TestDataCount = (int)1E6;
        private IEnumerable<TestType> _testData;

        [TestFixtureSetUp]
        public void SetUp()
        {
            var gen = new Random(35521);

            var data = new List<TestType>(TestDataCount);
            for (int i = 0; i < TestDataCount; i++)
                data.Add(new TestType { Value1 = gen.NextDouble() });

            _testData = data;
        }

        [Test]
        public void TestDataAnnotations()
        {
            var sw = Stopwatch.StartNew();

            foreach (var item in _testData)
            {
                var ctx = new ValidationContext(item, null, null);
                var errors = new List<ValidationResult>();
                DAValidator.TryValidateObject(item, ctx, errors);
            }

            sw.Stop();
            Assert.Inconclusive("DataAnnotations validated {0} objects in {1} ticks", TestDataCount, sw.ElapsedTicks);
        }

        [Test]
        public void TestCompiledValidators()
        {
            var sw = Stopwatch.StartNew();
            var validator = new Validator(true, new UserAssemblyRecursionPolicy(), new DataAnnotationsValidatorProvider(), new RangeValidationExpressionConverter());

            foreach (var item in _testData)
                validator.IsValid(item);

            sw.Stop();
            Assert.Inconclusive("CompiledValidators validated {0} objects in {1} ticks", TestDataCount, sw.ElapsedTicks);
        }

        public class TestType
        {
            [Range(0.5, 0.75)]
            public double Value1 { get; set; }
        }
    }
}
