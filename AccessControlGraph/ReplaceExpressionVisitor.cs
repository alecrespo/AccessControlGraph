using System.Collections.Generic;
using System.Linq.Expressions;

namespace AccessControlGraph
{
    /// <summary>
    /// Helper for replacing parameter values in Expressions with constants
    /// </summary>
    public class ReplaceExpressionVisitor : ExpressionVisitor
    {
        public Dictionary<string, object> Replaces = new Dictionary<string, object>();

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return Replaces.ContainsKey(node.Name) ? Expression.Constant(Replaces[node.Name]) : base.VisitParameter(node);
        }
    }
}
