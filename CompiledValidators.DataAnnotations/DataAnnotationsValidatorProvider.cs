using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace CompiledValidators.DataAnnotations
{
    public class DataAnnotationsValidatorProvider : IValidatorProvider
    {
        private readonly Dictionary<Type, Dictionary<string, IEnumerable<ValidationAttribute>>> _memberValidators = new Dictionary<Type, Dictionary<string, IEnumerable<ValidationAttribute>>>();

        public IEnumerable<ValidatorInfo> GetValidators(MemberInfo member)
        {
            var type = member as Type ?? member.DeclaringType;

            Dictionary<string, IEnumerable<ValidationAttribute>> typeMemberValidators;
            if (!_memberValidators.TryGetValue(type, out typeMemberValidators))
                _memberValidators.Add(type, typeMemberValidators = GetTypeValidators(type));

            IEnumerable<ValidationAttribute> result;
            if (!typeMemberValidators.TryGetValue(member.Name, out result))
                result = new ValidationAttribute[0];

            var infos = result.Select(x => new ErrorMessageValidatorInfo(x, () => x.FormatErrorMessage(member.Name)));

            if (member is Type && typeof(IValidatableObject).IsAssignableFrom(type))
                return new ValidatorInfo[] { new MemberErrorValidatorInfo(null, GetTargetValidationErrorMessages) }.Concat(infos);
            else
                return infos;
        }

        private Dictionary<string, IEnumerable<ValidationAttribute>> GetTypeValidators(Type type)
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

        private static IEnumerable<MemberValidationErrorMessage> GetTargetValidationErrorMessages(object target)
        {
            return ((IValidatableObject)target)
                .Validate(new ValidationContext(target, null, null))
                .SelectMany(r => r.MemberNames
                    .DefaultIfEmpty()
                    .Select(n => new MemberValidationErrorMessage(n, r.ErrorMessage)));
        }

        private static IEnumerable<MemberInfo> GetReadValues(Type type)
        {
            return type.GetFields().Concat<MemberInfo>(type.GetProperties());
        }

        private static IEnumerable<ValidationAttribute> FilterAttributes(IEnumerable<object> attributes)
        {
            return attributes.OfType<ValidationAttribute>().ToArray();
        }
    }
}
