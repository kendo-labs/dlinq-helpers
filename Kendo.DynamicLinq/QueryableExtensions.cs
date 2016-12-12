using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using DynamicExpression = System.Linq.Dynamic.DynamicExpression;

namespace Kendo.DynamicLinq
{
    public static class QueryableExtensions
    {
        /// <summary>
        ///     Applies data processing (paging, sorting, filtering and aggregates) over IQueryable using Dynamic Linq.
        /// </summary>
        /// <typeparam name="T">The type of the IQueryable.</typeparam>
        /// <param name="queryable">The IQueryable which should be processed.</param>
        /// <param name="take">Specifies how many items to take. Configurable via the pageSize setting of the Kendo DataSource.</param>
        /// <param name="skip">Specifies how many items to skip.</param>
        /// <param name="sort">Specifies the current sort order.</param>
        /// <param name="filter">Specifies the current filter.</param>
        /// <param name="group">Specifies the current groups.</param>
        /// <param name="aggregates">Specifies the current aggregates.</param>
        /// <returns>A DataSourceResult object populated from the processed IQueryable.</returns>
        public static DataSourceResult ToDataSourceResult<T>(this IQueryable<T> queryable, int take, int skip,
            IEnumerable<Sort> sort, Filter filter, IEnumerable<Aggregator> aggregates, IEnumerable<Sort> group)
        {
            //the way this extension works it pages the records using skip and take 
            //in order to do that we need at least one sorted property
            if ((sort != null) && !sort.Any())
            {
                var elementType = queryable.ElementType;
                var properties = elementType.GetProperties().ToList();
                //by default make dir desc
                var sortByObject = new Sort
                {
                    Dir = "desc"
                };
                PropertyInfo propertyInfo;
                //look for proerty that is called id
                if (properties.Any(p => p.Name.ToLower() == "id"))
                {
                    propertyInfo = properties.FirstOrDefault(p => p.Name.ToLower() == "id");
                }
                //or contains id
                else if (properties.Any(p => p.Name.ToLower().Contains("id")))
                {
                    propertyInfo = properties.FirstOrDefault(p => p.Name.ToLower().Contains("id"));
                }
                //or just get the first property
                else
                {
                    propertyInfo = properties.FirstOrDefault();
                }
                if (propertyInfo != null)
                {
                    sortByObject.Field = propertyInfo.Name;
                }
                sort = new List<Sort> {sortByObject};
            }
            // Filter the data first
            queryable = Filter(queryable, filter);

            // Calculate the total number of records (needed for paging)
            var total = queryable.Count();

            // Calculate the aggregates
            var aggregate = Aggregate(queryable, aggregates);

            if ((group != null) && group.Any())
            {
                foreach (var source in group.Reverse())
                {
                    sort = sort.Append(new Sort
                    {
                        Field = source.Field,
                        Dir = source.Dir
                    });
                }
            }

            // Sort the data
            queryable = Sort(queryable, sort);

            // Finally page the data
            if (take > 0)
            {
                queryable = Page(queryable, take, skip, sort.Any());
            }

            var result = new DataSourceResult
            {
                Total = total,
                Aggregates = aggregate
            };

            //to use add this to your DataSource
            //schema: {
            //          groups: "Group",
            //          data: "Data"
            //}

            // Group By
            if ((group != null) && group.Any())
            {
                var groupedQuery =
                    queryable.ToList().GroupByMany(group.Select(p => p.Field).ToArray());

                result.Group = groupedQuery;
            }
            else
            {
                result.Data = queryable.ToList();
            }
            return result;
        }

        /// <summary>
        ///     Applies data processing (paging, sorting and filtering) over IQueryable using Dynamic Linq.
        /// </summary>
        /// <typeparam name="T">The type of the IQueryable.</typeparam>
        /// <param name="queryable">The IQueryable which should be processed.</param>
        /// <param name="take">Specifies how many items to take. Configurable via the pageSize setting of the Kendo DataSource.</param>
        /// <param name="skip">Specifies how many items to skip.</param>
        /// <param name="sort">Specifies the current sort order.</param>
        /// <param name="filter">Specifies the current filter.</param>
        /// <returns>A DataSourceResult object populated from the processed IQueryable.</returns>
        public static DataSourceResult ToDataSourceResult<T>(this IQueryable<T> queryable, int take, int skip,
            IEnumerable<Sort> sort, Filter filter)
        {
            return queryable.ToDataSourceResult(take, skip, sort, filter, null, null);
        }

        /// <summary>
        ///     Applies data processing (paging, sorting and filtering) over IQueryable using Dynamic Linq.
        /// </summary>
        /// <typeparam name="T">The type of the IQueryable.</typeparam>
        /// <param name="queryable">The IQueryable which should be processed.</param>
        /// <param name="request">The DataSourceRequest object containing take, skip, order, and filter data.</param>
        /// <returns>A DataSourceResult object populated from the processed IQueryable.</returns>
        public static DataSourceResult ToDataSourceResult<T>(this IQueryable<T> queryable, DataSourceRequest request)
        {
            return queryable.ToDataSourceResult(request.Take, request.Skip, request.Sort, request.Filter, null,
                request.Group);
        }

        public static IEnumerable<T> Append<T>(this IEnumerable<T> source, T item)
        {
            foreach (var i in source)
            {
                yield return i;
            }

            yield return item;
        }

        public static IEnumerable<T> Prepend<T>(this IEnumerable<T> source, T item)
        {
            yield return item;

            foreach (var i in source)
            {
                yield return i;
            }
        }

        private static IQueryable<T> Filter<T>(IQueryable<T> queryable, Filter filter)
        {
            if ((filter != null) && (filter.Logic != null))
            {
                // Collect a flat list of all filters
                var filters = filter.All().Distinct().ToList();

                // Get all filter values as array (needed by the Where method of Dynamic Linq)
                var values = filters.Select(f => f.Value is string ? f.Value.ToString().ToLower() : f.Value).ToArray();

                ////Add toLower() for all filter Fields with type of string in the values 
                for (var i = 0; i < values.Length; i++)
                {
                    if (values[i] is string)
                    {
                        filters[i].Field = string.Format("{0}.ToString().ToLower()", filters[i].Field);
                    }
                    // when we have a decimal value it gets converted to double and the query will break
                    if (values[i] is double)
                    {
                        values[i] = Convert.ToDecimal(values[i]);
                    }
                    if (values[i] is DateTime)
                    {
                        var dateTimeFilterValue = (DateTime) values[i];
                        values[i] = new DateTime(dateTimeFilterValue.Year, dateTimeFilterValue.Month,
                            dateTimeFilterValue.Day, 0, 0, 0);
                    }
                }

                var valuesList = values.ToList();

                //Remove duplicate filters
                //NOTE: we loop, and don't use .distinct for a reason!
                //There is a miniscule chance different columns will filter by the same value, in which case using distinct will remove too many filters
                for (var i = filters.Count - 1; i >= 0; i--)
                {
                    var previousFilter = filters.ElementAtOrDefault(i - 1);

                    if ((previousFilter != null) && filters[i].Equals(previousFilter))
                    {
                        filters.RemoveAt(i);

                        valuesList.RemoveAt(i);
                    }
                }
                var filtersList = filters.ToList();
                for (var i = 0; i < filters.Count; i++)
                {
                    if (filters[i].Value is DateTime && (filters[i].Operator == "eq"))
                    {
                        var filterToEdit = filtersList[i];

                        //Copy the date from the filter
                        var baseDate = ((DateTime) filters[i].Value).Date;

                        //Instead of comparing for exact equality, we compare as greater than the start of the day...
                        filterToEdit.Value = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, 0, 0, 0);
                        filterToEdit.Operator = "gte";
                        valuesList[i] = filterToEdit.Value;

                        //...and less than the end of that same day (we're making an additional filter here)
                        var newFilter = new Filter
                        {
                            Value = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, 23, 59, 59),
                            Field = filters[i].Field,
                            Filters = filters[i].Filters,
                            Operator = "lte",
                            Logic = "and"
                        };

                        //Add that additional filter to the list of filters
                        filtersList.Add(newFilter);
                        valuesList.Add(newFilter.Value);
                    }
                }

                values = valuesList.ToArray();
                filters = filtersList;
                //Set the filters, since we may have editted them
                filter.Filters = filtersList;

                // Create a predicate expression e.g. Field1 = @0 And Field2 > @1
                var predicate = filter.ToExpression(filters);

                // Use the Where method of Dynamic Linq to filter the data
                queryable = queryable.Where(predicate, values);
            }

            return queryable;
        }

        private static object Aggregate<T>(IQueryable<T> queryable, IEnumerable<Aggregator> aggregates)
        {
            if ((aggregates != null) && aggregates.Any())
            {
                var objProps = new Dictionary<DynamicProperty, object>();
                var groups = aggregates.GroupBy(g => g.Field);
                Type type = null;
                foreach (var group in groups)
                {
                    var fieldProps = new Dictionary<DynamicProperty, object>();
                    foreach (var aggregate in group)
                    {
                        var prop = typeof(T).GetProperty(aggregate.Field);
                        var param = Expression.Parameter(typeof(T), "s");
                        var selector = (aggregate.Aggregate == "count") &&
                                       (Nullable.GetUnderlyingType(prop.PropertyType) != null)
                            ? Expression.Lambda(
                                Expression.NotEqual(Expression.MakeMemberAccess(param, prop),
                                    Expression.Constant(null, prop.PropertyType)), param)
                            : Expression.Lambda(Expression.MakeMemberAccess(param, prop), param);
                        var mi = aggregate.MethodInfo(typeof(T));
                        if (mi == null)
                        {
                            continue;
                        }

                        var val = queryable.Provider.Execute(Expression.Call(null, mi,
                                               (aggregate.Aggregate == "count") &&
                                               (Nullable.GetUnderlyingType(prop.PropertyType) == null)
                                                   ? new[] {queryable.Expression}
                                                   : new[] {queryable.Expression, Expression.Quote(selector)}));

                        fieldProps.Add(new DynamicProperty(aggregate.Aggregate, typeof(object)), val);
                    }
                    type = DynamicExpression.CreateClass(fieldProps.Keys);
                    var fieldObj = Activator.CreateInstance(type);
                    foreach (var p in fieldProps.Keys)
                    {
                        type.GetProperty(p.Name).SetValue(fieldObj, fieldProps[p], null);
                    }
                    objProps.Add(new DynamicProperty(group.Key, fieldObj.GetType()), fieldObj);
                }

                type = DynamicExpression.CreateClass(objProps.Keys);

                var obj = Activator.CreateInstance(type);

                foreach (var p in objProps.Keys)
                {
                    type.GetProperty(p.Name).SetValue(obj, objProps[p], null);
                }

                return obj;
            }
            return null;
        }

        private static IQueryable<T> Sort<T>(IQueryable<T> queryable, IEnumerable<Sort> sort)
        {
            if ((sort != null) && sort.Any())
            {
                // Create ordering expression e.g. Field1 asc, Field2 desc
                var ordering = string.Join(",", sort.Reverse().Select(s => s.ToExpression()));

                // Use the OrderBy method of Dynamic Linq to sort the data
                return queryable.OrderBy(ordering);
            }

            return queryable;
        }

        private static IQueryable<T> Page<T>(IQueryable<T> queryable, int take, int skip, bool sorted)
        {
            if (sorted)
            {
                return queryable.Skip(skip).Take(take);
            }
            return queryable.Take(take);
        }
    }
}