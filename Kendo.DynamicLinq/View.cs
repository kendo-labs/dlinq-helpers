using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kendo.DynamicLinq
{
   public class View
    {
        public int Take { get; set; }
        public int Skip { get; set; }
        public List<Sort> Sort { get; set; }
        public Filter Filter { get; set; }
        public List<Aggregator> Aggregates { get; set; }
        /// <summary>
        /// Filter.Value alanında guid değerler strint Type olarak gözükebilir, bu fonksiyon Guid ama string alan FieldType'ları Guid Type olarak günceller.  
        /// </summary>
        public void FieldTypeCheckAll()
        {
            List<Filter> _filters = Filter.Filters.ToList();
            for (int i = 0; i < _filters.Count; i++)
            {
                _filters[i] = FieldTypeCheck(_filters[i]);
            }
            Filter.Filters = (IEnumerable<Filter>)_filters;
            Filter = FieldTypeCheck(Filter);
        }

        public Filter FieldTypeCheck(Filter filter)
        {
            if (filter.Value == null)
            { return filter; }

            Guid _guidResult;
            if (Guid.TryParse(filter.Value.ToString(), out _guidResult))
            {
                filter.Value = _guidResult;
            }
            return filter;
        }
    }
}
