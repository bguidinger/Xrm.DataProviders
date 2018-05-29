namespace BGuidinger.Xrm.DataProviders.Dynamics365
{
    using Microsoft.Xrm.Sdk.Data.Mappings;
    using Microsoft.Xrm.Sdk.Metadata;
    using Microsoft.Xrm.Sdk.Query;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ODataQueryExpressionVisitor : IQueryExpressionVisitor
    {
        private Dictionary<string, string> _queryOptions;
        private readonly EntityMap _mapper;

        public string QueryString => _queryOptions.UrlEncode();

        public ODataQueryExpressionVisitor(EntityMetadata metadata)
        {
            _mapper = EntityMapFactory.Create(metadata, new DefaultTypeMapFactory(), null);
            _queryOptions = new Dictionary<string, string>
            {
                { "$count", "true" }
            };
        }

        public QueryExpression Visit(QueryExpression query)
        {
            VisitColumnSet(query.ColumnSet);
            VisitPagingInfo(query.PageInfo);
            VisitOrderExpressions(query.Orders);
            VisitFilterExpression(query.Criteria);

            return query;
        }

        private ColumnSet VisitColumnSet(ColumnSet columnSet)
        {
            if (columnSet != null && !columnSet.AllColumns && columnSet.Columns.Count > 0)
            {
                var columns = columnSet.Columns.Select(x => _mapper.MapAttributeNameExternal(x)).Distinct();
                _queryOptions.Add("$select", $"{string.Join(",", columns)}");
            }
            return columnSet;
        }

        private PagingInfo VisitPagingInfo(PagingInfo pageInfo)
        {
            if (pageInfo != null && !string.IsNullOrEmpty(pageInfo.PagingCookie))
            {
                _queryOptions.Add("$skiptoken", $"<cookie pagenumber='{pageInfo.PageNumber}' pagingcookie='{pageInfo.PagingCookie}' istracking='False' />");
            }
            return pageInfo;
        }

        private IEnumerable<OrderExpression> VisitOrderExpressions(IEnumerable<OrderExpression> orders)
        {
            if (orders != null && orders.Count() > 0)
            {
                var orderBys = new List<string>();
                foreach (var order in orders)
                {
                    var attribute = _mapper.MapAttributeNameExternal(order.AttributeName);
                    var direction = order.OrderType == OrderType.Descending ? "desc" : "asc";
                    orderBys.Add($"{attribute} {direction}");
                }
                _queryOptions.Add("$orderby", $"{string.Join(",", orderBys)}");
            }
            return orders;
        }

        private FilterExpression VisitFilterExpression(FilterExpression filterExpression)
        {
            if (filterExpression != null && (filterExpression.Filters.Any() || filterExpression.Conditions.Any()))
            {
                var filter = ParseFilter(filterExpression);
                _queryOptions.Add("$filter", filter);
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
                    return $"({string.Join(" and ", conditions)})";
                case LogicalOperator.Or:
                    return $"({string.Join(" or ", conditions)})";
                default:
                    throw new Exception("Operator not implemented: " + filter.FilterOperator);
            }
        }

        private string ParseCondition(ConditionExpression expression)
        {
            var attribute = _mapper.MapAttributeNameExternal(expression.AttributeName);
            var value = expression.Values.FirstOrDefault();
            if (value is DateTime)
            {
                value = $"{((DateTime)value).ToString("yyyy-MM-ddTHH:mm:ssZ")}";
            }
            else
            {
                value = "'" + value + "'";
            }

            switch (expression.Operator)
            {
                case ConditionOperator.Equal:
                    return $"{attribute} eq {value}";
                case ConditionOperator.NotEqual:
                    return $"{attribute} ne {value}";
                case ConditionOperator.GreaterThan:
                    return $"{attribute} gt {value}";
                case ConditionOperator.LessThan:
                    return $"{attribute} lt {value}";
                case ConditionOperator.GreaterEqual:
                    return $"{attribute} ge {value}";
                case ConditionOperator.LessEqual:
                    return $"{attribute} le {value}";
                case ConditionOperator.Like:
                    return ParseLike(attribute, (string)value);
                case ConditionOperator.NotLike:
                    return "not " + ParseLike(attribute, (string)value);
                case ConditionOperator.Contains:
                    return $"contains({attribute}, {value})";
                case ConditionOperator.DoesNotContain:
                    return $"not contains({attribute}, {value})";
                case ConditionOperator.BeginsWith:
                    return $"startswith({attribute}, {value})";
                case ConditionOperator.DoesNotBeginWith:
                    return $"not startswith({attribute}, {value})";
                case ConditionOperator.EndsWith:
                    return $"endswith({attribute}, {value})";
                case ConditionOperator.DoesNotEndWith:
                    return $"not endswith({attribute}, {value})";
                case ConditionOperator.Null:
                    return $"{attribute} eq null";
                case ConditionOperator.NotNull:
                    return $"{attribute} ne null";
                case ConditionOperator.In:
                case ConditionOperator.NotIn:
                case ConditionOperator.Between:
                case ConditionOperator.NotBetween:
                    var values = string.Join(",", expression.Values.Select(x => $"'{x}'"));
                    return $"Microsoft.Dynamics.CRM.{expression.Operator}(PropertyName='{attribute}',PropertyValues=[{values}])";
                case ConditionOperator.Above:
                case ConditionOperator.Under:
                case ConditionOperator.NotUnder:
                case ConditionOperator.UnderOrEqual:
                case ConditionOperator.AboveOrEqual:
                case ConditionOperator.LastXHours:
                case ConditionOperator.NextXHours:
                case ConditionOperator.LastXDays:
                case ConditionOperator.NextXDays:
                case ConditionOperator.LastXWeeks:
                case ConditionOperator.NextXWeeks:
                case ConditionOperator.LastXMonths:
                case ConditionOperator.NextXMonths:
                case ConditionOperator.LastXYears:
                case ConditionOperator.NextXYears:
                case ConditionOperator.LastXFiscalYears:
                case ConditionOperator.LastXFiscalPeriods:
                case ConditionOperator.NextXFiscalYears:
                case ConditionOperator.NextXFiscalPeriods:
                case ConditionOperator.OlderThanXYears:
                case ConditionOperator.OlderThanXMonths:
                case ConditionOperator.OlderThanXWeeks:
                case ConditionOperator.OlderThanXDays:
                case ConditionOperator.OlderThanXHours:
                case ConditionOperator.OlderThanXMinutes:
                case ConditionOperator.On:
                case ConditionOperator.OnOrBefore:
                case ConditionOperator.OnOrAfter:
                case ConditionOperator.NotOn:
                case ConditionOperator.InFiscalYear:
                case ConditionOperator.InFiscalPeriod:
                    return $"Microsoft.Dynamics.CRM.{expression.Operator}(PropertyName='{attribute}',PropertyValue={value})";
                case ConditionOperator.Yesterday:
                case ConditionOperator.Today:
                case ConditionOperator.Tomorrow:
                case ConditionOperator.Last7Days:
                case ConditionOperator.Next7Days:
                case ConditionOperator.LastWeek:
                case ConditionOperator.ThisWeek:
                case ConditionOperator.NextWeek:
                case ConditionOperator.LastMonth:
                case ConditionOperator.ThisMonth:
                case ConditionOperator.NextMonth:
                case ConditionOperator.LastYear:
                case ConditionOperator.ThisYear:
                case ConditionOperator.NextYear:
                case ConditionOperator.ThisFiscalYear:
                case ConditionOperator.ThisFiscalPeriod:
                case ConditionOperator.NextFiscalYear:
                case ConditionOperator.NextFiscalPeriod:
                case ConditionOperator.LastFiscalYear:
                case ConditionOperator.LastFiscalPeriod:
                case ConditionOperator.EqualUserId:
                case ConditionOperator.NotEqualUserId:
                case ConditionOperator.EqualUserLanguage:
                case ConditionOperator.EqualUserTeams:
                case ConditionOperator.EqualUserOrUserTeams:
                case ConditionOperator.EqualUserOrUserHierarchy:
                case ConditionOperator.EqualUserOrUserHierarchyAndTeams:
                case ConditionOperator.EqualBusinessId:
                case ConditionOperator.NotEqualBusinessId:
                    return $"Microsoft.Dynamics.CRM.{expression.Operator}(PropertyName='{attribute}')";
                case ConditionOperator.InFiscalPeriodAndYear:
                case ConditionOperator.InOrBeforeFiscalPeriodAndYear:
                case ConditionOperator.InOrAfterFiscalPeriodAndYear:
                    var value1 = expression.Values?[0];
                    var value2 = expression.Values?[1];
                    return $"Microsoft.Dynamics.CRM.{expression.Operator}(PropertyName='{attribute}',PropertyValue1={value1},PropertyValue2={value2})";
                //case ConditionOperator.ChildOf:
                //case ConditionOperator.Mask:
                //case ConditionOperator.NotMask:
                //case ConditionOperator.MasksSelect:
                //case ConditionOperator.ContainValues:
                //case ConditionOperator.DoesNotContainValues:
                default:
                    throw new Exception("Operator not implemented: " + expression.Operator);
            }
        }
        private string ParseLike(string attribute, string value)
        {
            if (value.StartsWith("'%") && value.EndsWith("%'"))
            {
                return $"contains({attribute}, {value})";
            }
            else if (value.StartsWith("'%"))
            {
                return $"endswith({attribute}, {value})";
            }
            else if (value.EndsWith("%'"))
            {
                return $"startswith({attribute}, {value})";
            }
            else
            {
                throw new Exception("Unkown value: " + value);
            }
        }
    }
}