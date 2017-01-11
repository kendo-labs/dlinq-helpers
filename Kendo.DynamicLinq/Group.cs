using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Kendo.DynamicLinq
{
    public class Group : Sort
    {
        [DataMember(Name = "aggregates")]
        public IEnumerable<Aggregator> Aggregates { get; set; }
    }
}