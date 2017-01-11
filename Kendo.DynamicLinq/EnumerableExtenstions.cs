using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;

namespace Kendo.DynamicLinq
{
    public static class EnumerableExtenstions
    {
        public static dynamic GroupByMany<TElement>(this IEnumerable<TElement> elements,
            IEnumerable<Group> groupSelectors)
        {
            //create a new list of Kendo Group Selectors 
            var selectors = new List<GroupSelector<TElement>>(groupSelectors.Count());
            foreach (var selector in groupSelectors)
            {
                //compile the Dynamic Expression Lambda for each one
                var expression =
                    DynamicExpression.ParseLambda(typeof(TElement), typeof(object), selector.Field);
                //add it to the list
                selectors.Add(new GroupSelector<TElement>
                {
                    Selector = (Func<TElement, object>) expression.Compile(),
                    Field = selector.Field,
                    Aggregates = selector.Aggregates
                });
            }
            //call the actual group by method
            return elements.GroupByMany(selectors.ToArray());
        }
        //returned value should look like the following
        //        [{
        //  aggregates: {
        //    FIEL1DNAME: {
        //      FUNCTON1NAME: FUNCTION1VALUE,
        //      FUNCTON2NAME: FUNCTION2VALUE
        //    },
        //    FIELD2NAME: {
        //      FUNCTON1NAME: FUNCTION1VALUE
        //    }
        //  },
        //  field: FIELDNAME, // the field by which the data items are grouped
        //  hasSubgroups: true, // true if there are subgroups
        //  items: [
        //    // either the subgroups or the data items
        //    {
        //      aggregates: {
        //        //nested group aggregates
        //      },
        //      field: NESTEDGROUPFIELDNAME,
        //      hasSubgroups: false,
        //      items: [
        //      // data records
        //      ],
        //      value: NESTEDGROUPVALUE
        //    },
        //    //nestedgroup2, nestedgroup3, etc.
        //  ],
        //  value: VALUE // the group key
        //} /* other groups */
        //]
        //for more info check http://docs.telerik.com/kendo-ui/api/javascript/data/datasource#configuration-schema.groups

        public static dynamic GroupByMany<TElement>(this IEnumerable<TElement> elements,
            params GroupSelector<TElement>[] groupSelectors)
        {
            if (groupSelectors.Length > 0)
            {
                //get selector
                var selector = groupSelectors.First();
                var nextSelectors = groupSelectors.Skip(1).ToArray(); //reduce the list recursively until zero
                return
                    //group by and return 
                    elements.GroupBy(selector.Selector).Select(
                                g => new GroupResult
                                {
                                    Value = g.Key,
                                    Aggregates = selector.Aggregates,
                                    HasSubgroups = groupSelectors.Length > 1,
                                    Count = g.Count(),
                                    //recursivly group the next selectors
                                    Items = g.GroupByMany(nextSelectors),
                                    SelectorField = selector.Field
                                });
            }
            //if there are not more group selectors return data
            return elements;
        }
    }
}