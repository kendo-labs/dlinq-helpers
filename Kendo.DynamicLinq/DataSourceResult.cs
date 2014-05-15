using System.Collections;

namespace Kendo.DynamicLinq
{
    /// <summary>
    /// Describes the result of Kendo DataSource read operation. 
    /// </summary>
    public class DataSourceResult
    {
        /// <summary>
        /// Represents a single page of processed data.
        /// </summary>
        public IEnumerable Data { get; set; }

        /// <summary>
        /// The total number of records available.
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Represents a requested aggregates.
        /// </summary>
        public dynamic Aggregates { get; set; }
    }
}
