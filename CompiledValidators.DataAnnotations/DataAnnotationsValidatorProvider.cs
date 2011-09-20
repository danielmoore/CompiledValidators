using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace CompiledValidators.DataAnnotations
{
    public class DataAnnotationsValidatorProvider : IValidatorProvider
    {
        private readonly Dictionary<Type, Dictionary<string, IEnumerable<object>>> _memberValidators = new Dictionary<Type, Dictionary<string, IEnumerable<object>>>();

        public IEnumerable<object> GetValidators(MemberInfo member)
        {
            var type = member as Type ?? member.DeclaringType;

            Dictionary<string, IEnumerable<object>> typeMemberValidators;
            if (!_memberValidators.TryGetValue(type, out typeMemberValidators))
                _memberValidators.Add(type, typeMemberValidators = GetTypeValidators(type));

            IEnumerable<object> result;
            if (!typeMemberValidators.TryGetValue(member.Name, out result))
                result = new object[0];

            if (member is Type && typeof(IValidatableObject).IsAssignableFrom(type))
                return new object[] { null }.Concat(result);
            else
                return result;
        }

        private Dictionary<string, IEnumerable<object>> GetTypeValidators(Type type)
        {
            var members = GetReadValues(type);

            var metdataAttr = type.GetCustomAttributes(false).OfType<MetadataTypeAttribute>().SingleOrDefault();

            if (metdataAttr != null)
                return GetReadValues(metdataAttr.MetadataClassType)
                    .Join(members, o => o.Name, i => i.Name, (o, i) => new { o.Name, Validators = o.GetCustomAttributes(false).Concat(i.GetCustomAttributes(false)) })
                    .ToDictionary(x => x.Name, x => FilterAttributes(x.Validators));
            else
                return members.ToDictionary(m => m.Name, m => FilterAttributes(m.GetCustomAttributes(false)));
        }

        private IEnumerable<MemberInfo> GetReadValues(Type type)
        {
            return type.GetFields().Concat<MemberInfo>(type.GetProperties());
        }

        private IEnumerable<object> FilterAttributes(IEnumerable<object> attributes)
        {
            return attributes.OfType<ValidationAttribute>().ToArray();
        }
    }
}
