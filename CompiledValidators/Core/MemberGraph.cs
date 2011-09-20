using System;
using System.Collections.Generic;
using System.Reflection;
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
        private readonly List<Member> _members;
        private bool _isSealed;

        public MemberGraph()
        {
            _members = new List<Member>();
            _members.Add(null); // root
        }

        public int RootId { get { return 0; } }

        public string GetMemberAccessString(int id)
        {
            var member = _members[id];

            if (member.AccessString != null) return member.AccessString;

            var chain = new Stack<Member>();

            while (member != null)
            {
                chain.Push(member);
                member = member.Parent;
            }

            var builder = new StringBuilder("root");
            while (chain.Count > 0)
            {
                member = chain.Pop();

                switch (member.MemberType)
                {
                    case MemberType.Scalar: builder.AppendFormat(".{0}", member.MemberInfo.Name); break;
                    case MemberType.Collection: builder.AppendFormat(".{0}[]", member.MemberInfo.Name); break;
                    default: throw new NotSupportedException();
                }
            }

            return member.AccessString = builder.ToString();
        }

        public int NewId(int parentId, MemberInfo memberInfo, MemberType memberType = MemberType.Scalar)
        {
            if (_isSealed) throw new InvalidOperationException();

            _members.Add(new Member(_members[parentId], memberInfo, memberType));
            return _members.Count - 1;
        }

        public void Seal()
        {
            _isSealed = true;
            _members.TrimExcess();
        }

        private class Member
        {
            public Member(Member parent, MemberInfo memberInfo, MemberType memberType)
            {
                Parent = parent;
                MemberInfo = memberInfo;
                MemberType = memberType;
            }

            public Member Parent { get; private set; }

            public MemberInfo MemberInfo { get; private set; }

            public MemberType MemberType { get; private set; }

            public string AccessString { get; set; }
        }
    }
}
