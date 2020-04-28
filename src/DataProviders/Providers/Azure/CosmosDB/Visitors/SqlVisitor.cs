namespace BGuidinger.Xrm.DataProviders.CosmosDB
{
    using Microsoft.Xrm.Sdk.Data.Mappings;
    using Microsoft.Xrm.Sdk.Metadata;
    using Microsoft.Xrm.Sdk.Query;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class SqlVisitor : IQueryExpressionVisitor
    {
        private readonly EntityMetadata _metadata;
        private readonly EntityMap _mapper;

        private const string tableAlias = "a";

        public string Query { get; private set; } = string.Empty;

        public SqlVisitor(EntityMetadata metadata)
        {
            _metadata = metadata;
            _mapper = EntityMapFactory.Create(metadata, new DefaultTypeMapFactory(), null);
        }

        public QueryExpression Visit(QueryExpression query)
        {
            VisitColumnSet(query.ColumnSet, query.TopCount);
            VisitOrderExpressions(query.Orders);
            VisitFilterExpression(query.Criteria);

            return query;
        }

        private ColumnSet VisitColumnSet(ColumnSet columnSet, int? topCount)
        {
            var columns = "*";
            if (columnSet != null && !columnSet.AllColumns && columnSet.Columns.Count > 0)
            {
                columnSet.AddColumn(_metadata.PrimaryIdAttribute);

                var columnNames = columnSet.Columns.Select(x => tableAlias + "." + _mapper.MapAttributeNameExternal(x)).Distinct();
                columns = string.Join(", ", columnNames);
            }

            var top = string.Empty;
            if (topCount != null)
            {
                top = $"TOP {topCount}";
            }

            Query += $"SELECT {top} {columns} FROM {_metadata.ExternalCollectionName} {tableAlias}";
            return columnSet;
        }

        private IEnumerable<OrderExpression> VisitOrderExpressions(IEnumerable<OrderExpression> orders)
        {
            if (orders != null && orders.Count() > 0)
            {
                var orderBys = new List<string>();
                foreach (var order in orders)
                {
                    var attribute = _mapper.MapAttributeNameExternal(order.AttributeName);
                    var direction = order.OrderType == OrderType.Descending ? "DESC" : "ASC";
                    orderBys.Add($"{tableAlias}.{attribute} {direction}");
                }

                Query += $" ORDER BY {string.Join(", ", orderBys)}";
            }

            return orders;
        }

        private FilterExpression VisitFilterExpression(FilterExpression filterExpression)
        {
            if (filterExpression != null && (filterExpression.Filters.Any() || filterExpression.Conditions.Any()))
            {
                var filter = ParseFilter(filterExpression);
                Query += $" WHERE {filter}";
            }
            return filterExpression;
        }

        private string ParseFilter(FilterExpression filter)
        {
            if (!filter.Conditions.Any() && filter.Filters.Count == 1)
            {
                return ParseFilter(filter.Filters.Single());
            }

            var conditions = filter.Filters.Select(x => ParseFilter(x)).Union(filter.Conditions.Select(x => ParseCondition(x)));
            switch (filter.FilterOperator)
            {
                case LogicalOperator.And:
                    return $"({string.Join(" AND ", conditions)})";
                case LogicalOperator.Or:
                    return $"({string.Join(" OR ", conditions)})";
                default:
                    throw new Exception("Operator not implemented: " + filter.FilterOperator);
            }
        }

        private string ParseCondition(ConditionExpression expression)
        {
            var attribute = tableAlias + "." + _mapper.MapAttributeNameExternal(expression.AttributeName);
            var value = expression.Values.FirstOrDefault();

            var attributeMetadata = _metadata.Attributes.FirstOrDefault(x => x.LogicalName == expression.AttributeName);
            switch (attributeMetadata.AttributeType)
            {
                case AttributeTypeCode.Uniqueidentifier:
                case AttributeTypeCode.String:
                    value = $"'{value}'";
                    break;
            }

            switch (expression.Operator)
            {
                case ConditionOperator.Equal:
                    return $"{attribute} = {value}";
                case ConditionOperator.NotEqual:
                    return $"{attribute} != {value}";
                case ConditionOperator.GreaterThan:
                    return $"{attribute} > {value}";
                case ConditionOperator.LessThan:
                    return $"{attribute} < {value}";
                case ConditionOperator.GreaterEqual:
                    return $"{attribute} >= {value}";
                case ConditionOperator.LessEqual:
                    return $"{attribute} <= {value}";
                default:
                    throw new Exception("Operator not implemented: " + expression.Operator);
            }
        }
    }
}