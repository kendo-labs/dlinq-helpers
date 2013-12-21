#Kendo.DynamicLinq

## Description
Kendo.DynamicLinq implements server paging, filtering and sorting via Dynamic Linq.

## Usage
1. Add the Kendo.DynamicLinq NuGet package to your project.
2. Configure your Kendo DataSource to send its options as JSON.

        parameterMap: function(options, type) {
            return JSON.stringify(options);
        }
3. Import the Kendo.DynamicLinq namespace.
4. Use the `ToDataSourceResult` extension method to apply paging, sorting and filtering.

        [WebMethod]
        public static DataSourceResult Products(int take, int skip, IEnumerable<Sort> sort, Filter filter)
        {
            using (var northwind = new Northwind())
            {
                return northwind.Products
                    .OrderBy(p => p.ProductID) // EF requires ordering for paging
                    // Use a view model to avoid serializing internal Entity Framework properties as JSON
                    .Select(p => new ProductViewModel
                    {
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        UnitPrice = p.UnitPrice,
                        UnitsInStock = p.UnitsInStock,
                        Discontinued = p.Discontinued
                    })
                 .ToDataSourceResult(take, skip, sort, filter);
            }
        }
