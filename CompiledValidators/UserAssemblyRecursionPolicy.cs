using System;
using System.Collections.Generic;
using System.Reflection;

namespace CompiledValidators
{
    public class UserAssemblyRecursionPolicy : IRecursionPolicy
    {
        private readonly Dictionary<Assembly, bool> _assemblyCache = new Dictionary<Assembly, bool>();

        public PolicyOptions GetPolicy(MemberInfo member)
        {
            var asm = GetAssembly(member);

            bool result;
            if (!_assemblyCache.TryGetValue(asm, out result))
                _assemblyCache.Add(asm, result = IsFxAssembly(asm));

            return result ? PolicyOptions.NoFollow : PolicyOptions.None;
        }

        private static Assembly GetAssembly(MemberInfo member)
        {
            var property = member as PropertyInfo;
            if (property != null)
                return property.PropertyType.Assembly;
            else
            {
                var field = member as FieldInfo;
                if (field != null)
                    return field.FieldType.Assembly;
                else
                    throw new ArgumentException("Not a property or a field", "member");
            }
        }

        private static bool IsFxAssembly(Assembly asm)
        {
            return asm.GetType("FXAssembly") != null;
        }
    }
}
