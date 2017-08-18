using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Kendo.DynamicLinq
{
    [DataContract(Name = "groupresult")]
    public class GroupResult
    {
        //small letter properties are kendo js properties so please execuse the warnings
        //for more info check http://docs.telerik.com/kendo-ui/api/javascript/data/datasource#configuration-schema.groups
        [DataMember(Name = "value")]
        public object Value { get; set; }

        public string SelectorField { get; set; }
        [DataMember(Name = "field")]
        public string Field
        {
            get { return string.Format("{0} ({1})", this.SelectorField, this.Count); }
        }
        public int Count { get; set; }

        [DataMember(Name = "aggregates")]
        public IEnumerable<Aggregator> Aggregates { get; set; }

        [DataMember(Name = "items")]
        public dynamic Items { get; set; }

        [DataMember(Name = "hasSubgroups")]
        public bool HasSubgroups { get; set; } // true if there are subgroups

        public override string ToString()
        {
            return string.Format("{0} ({1})", this.Value, this.Count);
        }
    }
}