﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Xml;
using Microsoft.WindowsAzure.Storage.Table;

namespace GitHub.WindowsAzure.Table.Queryable.ExpressionTranslators.Methods
{
    /// <summary>
    ///     Linq Where method translator.
    ///     <see cref="http://msdn.microsoft.com/en-us/library/windowsazure/dd894031.aspx" />
    /// </summary>
    public class WhereTranslator : ExpressionVisitor, IMethodTranslator
    {
        private readonly Dictionary<ExpressionType, string> _logicalOperators =
            new Dictionary<ExpressionType, string>
                {
                    {ExpressionType.AndAlso, "and"},
                    {ExpressionType.OrElse, "or"},
                    {ExpressionType.Not, "not"},
                    {ExpressionType.Equal, QueryComparisons.Equal},
                    {ExpressionType.NotEqual, QueryComparisons.NotEqual},
                    {ExpressionType.GreaterThan, QueryComparisons.GreaterThan},
                    {ExpressionType.GreaterThanOrEqual, QueryComparisons.GreaterThanOrEqual},
                    {ExpressionType.LessThan, QueryComparisons.LessThan},
                    {ExpressionType.LessThanOrEqual, QueryComparisons.LessThanOrEqual}
                };

        private StringBuilder _filter;

        public string Translate(MethodCallExpression method)
        {
            _filter = new StringBuilder();

            var lambda = (LambdaExpression) StripQuotes(method.Arguments[1]);
            Visit(lambda.Body);

            return UnwrapParentheses(_filter.ToString());
        }

        private static string UnwrapParentheses(string filter)
        {
            if (filter.Length < 2)
            {
                throw new ArgumentOutOfRangeException("filter");
            }

            if (filter[0] == '(' && filter[filter.Length - 1] == ')')
            {
                return UnwrapParentheses(filter.Substring(1, filter.Length - 2));
            }

            return filter;
        }

        private static Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
            {
                e = ((UnaryExpression) e).Operand;
            }

            return e;
        }

        protected override Expression VisitUnary(UnaryExpression unary)
        {
            if (!_logicalOperators.ContainsKey(unary.NodeType))
            {
                throw new NotSupportedException(
                    string.Format("The binary operator '{0}' is not supported", unary.NodeType));
            }

            _filter.AppendFormat(" {0} ", _logicalOperators[unary.NodeType]);

            Visit(unary.Operand);

            return unary;
        }

        protected override Expression VisitBinary(BinaryExpression binary)
        {
            bool paranthesesRequired = _logicalOperators.ContainsKey(binary.NodeType) &&
                                       (_logicalOperators.ContainsKey(binary.Left.NodeType) ||
                                        _logicalOperators.ContainsKey(binary.Right.NodeType));

            if (paranthesesRequired)
            {
                _filter.Append("(");
            }


            Visit(binary.Left);

            if (!_logicalOperators.ContainsKey(binary.NodeType))
            {
                throw new NotSupportedException(
                    string.Format("The binary operator '{0}' is not supported", binary.NodeType));
            }

            _filter.AppendFormat(" {0} ", _logicalOperators[binary.NodeType]);

            Visit(binary.Right);

            if (paranthesesRequired)
            {
                _filter.Append(")");
            }

            return binary;
        }

        protected override Expression VisitConstant(ConstantExpression constant)
        {
            if (constant.Value == null)
            {
                _filter.Append("null");
            }
            else
            {
                switch (Type.GetTypeCode(constant.Value.GetType()))
                {
                    case TypeCode.String:
                        _filter.AppendFormat("'{0}'", constant.Value);
                        break;

                    case TypeCode.DateTime:
                        _filter.AppendFormat(
                            "datetime'{0}'",
                            XmlConvert.ToString((DateTime) constant.Value,
                                                XmlDateTimeSerializationMode.RoundtripKind));
                        break;

                    case TypeCode.Single:
                    case TypeCode.Double:
                        _filter.AppendFormat("{0:#.0#}", constant.Value);
                        break;

                    case TypeCode.Int64:
                        _filter.AppendFormat("{0}L", constant.Value);
                        break;

                    case TypeCode.Boolean:
                        _filter.Append(constant.Value.ToString().ToLowerInvariant());
                        break;

                    case TypeCode.Object:

                        if (constant.Value is Guid)
                        {
                            _filter.AppendFormat("guid'{0}'", constant.Value);
                        }
                        else
                        {
                            throw new NotSupportedException(
                                string.Format("The constant for '{0}' is not supported", constant.Value));
                        }
                        break;

                    default:
                        _filter.Append(constant.Value);
                        break;
                }
            }
            return constant;
        }

        protected override Expression VisitMember(MemberExpression member)
        {
            if (member.Expression != null && member.Expression.NodeType == ExpressionType.Parameter)
            {
                _filter.Append(member.Member.Name);
                return member;
            }

            throw new NotSupportedException(
                string.Format("The member '{0}' is not supported", member.Member.Name));
        }
    }
}