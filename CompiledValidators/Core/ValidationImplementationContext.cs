using System.Linq.Expressions;

namespace CompiledValidators.Core
{
    internal class ValidationImplementationContext<TDelegate> where TDelegate : class
    {
        private readonly Expression<TDelegate> _validationExpression;
        private TDelegate _compiledValidationExpression;

        public ValidationImplementationContext(Expression<TDelegate> validationExpression, MemberGraph memberGraph)
        {
            _validationExpression = validationExpression;
            MemberGraph = memberGraph;
        }

        public TDelegate Validate
        {
            get { return _compiledValidationExpression ?? (_compiledValidationExpression = _validationExpression.Compile()); }
        }

        public MemberGraph MemberGraph { get; private set; }
    }
}
