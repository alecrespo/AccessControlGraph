using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AccessControlGraph
{
    public class ReplaceExpressionVisitor : ExpressionVisitor
    {
        public Dictionary<string, object> Replaces = new Dictionary<string, object>();

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return Replaces.ContainsKey(node.Name) ? Expression.Constant(Replaces[node.Name]) : base.VisitParameter(node);
        }
    }
}
