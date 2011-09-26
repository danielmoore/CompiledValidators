using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompiledValidators.Core
{
    internal enum MemberType
    {
        Scalar,
        Collection
    }

    internal class MemberGraph
    {
        public static readonly string RootMemberName = "root";

        private readonly List<Member> _members;
        private readonly List<Validator> _validators;
        private bool _isSealed;

        public MemberGraph()
        {
            _members = new List<Member> { null }; // [0] = root
            _validators = new List<Validator>();
        }

        public int RootId { get { return 0; } }

        public IEnumerable<MemberValidationErrorMessage> GetErrorMessages(int validatorId, object memberValue)
        {
            var validator = _validators[validatorId];
            if (validator.ErrorMessages != null)
                return validator.ErrorMessages;

            var errorMessageValidatorInfo = validator.Info as ErrorMessageValidatorInfo;
            if (errorMessageValidatorInfo != null)
                return validator.ErrorMessages = new[] { new MemberValidationErrorMessage(GetMemberAccessString(validator.MemberId), errorMessageValidatorInfo.GetErrorMessage()) };

            var memberErrorValidatorInfo = validator.Info as MemberErrorValidatorInfo;
            if (memberErrorValidatorInfo != null)
                return memberErrorValidatorInfo
                    .GetErrorMessages(memberValue)
                    .Select(x => new MemberValidationErrorMessage(GetMemberAccessString(validator.MemberId, x.MemberName), x.ErrorMessage))
                    .ToArray();

            return validator.ErrorMessages = new[] { new MemberValidationErrorMessage(GetMemberAccessString(validator.MemberId), "Unspecified error") };
        }

        private string GetMemberAccessString(int id, string additional = null)
        {
            if (id == RootId) return additional != null ? string.Format("{0}.{1}", RootMemberName, additional) : RootMemberName;

            var member = _members[id];

            if (member.AccessString != null) return member.AccessString;

            var chain = new Stack<Member>();

            while (member != null)
            {
                chain.Push(member);
                member = member.Parent;
            }

            var builder = new StringBuilder(RootMemberName);
            while (chain.Count > 0)
            {
                member = chain.Pop();

                switch (member.MemberType)
                {
                    case MemberType.Scalar: builder.AppendFormat(".{0}", member.Name); break;
                    case MemberType.Collection: builder.AppendFormat(".{0}[]", member.Name); break;
                    default: throw new NotSupportedException();
                }
            }

            if (!string.IsNullOrEmpty(additional))
                builder.AppendFormat(".{0}", additional);

            return member.AccessString = builder.ToString();
        }

        public int NewMemberId(int parentId, string memberName, MemberType memberType = MemberType.Scalar)
        {
            if (_isSealed) throw new InvalidOperationException();

            _members.Add(new Member(_members[parentId], memberName, memberType));
            return _members.Count - 1;
        }

        public int NewValidatorId(int memberId, ValidatorInfo validatorInfo)
        {
            if (_isSealed) throw new InvalidOperationException();

            _validators.Add(new Validator(memberId, validatorInfo));
            return _validators.Count - 1;
        }

        public void Seal()
        {
            _isSealed = true;
            _members.TrimExcess();
            _validators.TrimExcess();
        }

        private class Validator
        {
            public Validator(int memberId, ValidatorInfo info)
            {
                MemberId = memberId;
                Info = info;
            }

            public int MemberId { get; private set; }

            public ValidatorInfo Info { get; private set; }

            public IEnumerable<MemberValidationErrorMessage> ErrorMessages { get; set; }
        }

        private class Member
        {
            public Member(Member parent, string name, MemberType memberType)
            {
                Parent = parent;
                Name = name;
                MemberType = memberType;
            }

            public Member Parent { get; private set; }

            public string Name { get; private set; }

            public MemberType MemberType { get; private set; }

            public string AccessString { get; set; }
        }
    }
}
