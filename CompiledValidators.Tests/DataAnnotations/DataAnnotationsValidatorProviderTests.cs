using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CompiledValidators.DataAnnotations;
using Moq;
using NUnit.Framework;
using RangeAttribute = System.ComponentModel.DataAnnotations.RangeAttribute;

namespace CompiledValidators.Tests.DataAnnotations
{
    [TestFixture]
    public class DataAnnotationsValidatorProviderTests
    {
        private DataAnnotationsValidatorProvider _sut;

        [TestFixtureSetUp]
        public void SetUp()
        {
            _sut = new DataAnnotationsValidatorProvider();
        }

        [Test]
        public void IdentifiesTargetAttributes()
        {
            var memberInfo = GetMemberInfo((TestObject o) => o.Foo);
            Assert.IsTrue(_sut.GetValidators(memberInfo).Contains(memberInfo.GetCustomAttributes(false).Single()));
        }

        [Test]
        public void MergesMetadataAttributes()
        {
            var memberInfo = GetMemberInfo((TestObject o) => o.Foo);
            var metadataMemberInfo = GetMemberInfo((TestObjectMetadata o) => o.Foo);
            Assert.IsTrue(_sut.GetValidators(memberInfo).Contains(metadataMemberInfo.GetCustomAttributes(false).Single()));
        }

        [Test]
        public void IdentifiesMetadataAttributes()
        {
            var memberInfo = GetMemberInfo((TestObject o) => o.Bar);
            var metadataMemberInfo = GetMemberInfo((TestObjectMetadata o) => o.Bar);
            Assert.IsTrue(_sut.GetValidators(memberInfo).Contains(metadataMemberInfo.GetCustomAttributes(false).Single()));
        }

        [Test]
        public void IdentifiesValidatableObjects()
        {
            Assert.IsTrue(_sut.GetValidators(typeof(ValidatableObject)).Contains(null));
        }

        private static MemberInfo GetMemberInfo<TSource, TMember>(Expression<Func<TSource, TMember>> selector)
        {
            return ((MemberExpression)selector.Body).Member;
        }

        [MetadataType(typeof(TestObjectMetadata))]
        private class TestObject
        {
            [Range(3, 5)]
            public int Foo { get; set; }

            public string Bar { get; set; }
        }

        private class TestObjectMetadata
        {
            [Range(4, 6)]
            public int Foo;

            [Required]
            public string Bar;
        }


        private class ValidatableObject : IValidatableObject
        {
            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                throw new NotImplementedException();
            }
        }
    }
}
