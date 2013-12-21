#Kendo.DynamicLinq

## Description
Kendo.DynamicLinq implements server paging, filtering and sorting via Dynamic Linq.

## Usage
1. Add the Kendo.DynamicLinq NuGet package to your project.
1. Configure your Kendo DataSource to send its options as JSON.

        parameterMap: function(options, type) {
            return JSON.stringify(options);
        }
1. Configure the `schema` of the DataSource.

        schema: {
            data: "Data",
            total: "Total"
        }
1. Import the Kendo.DynamicLinq namespace.
1. Use the `ToDataSourceResult` extension method to apply paging, sorting and filtering.

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

## Examples

The following examples use Kendo.DynamicLinq.

[ASP.NET MVC](https://github.com/telerik/kendo-examples-asp-net-mvc/tree/master/grid-crud)
[ASP.NET Web Forms and Page Methods](https://github.com/telerik/kendo-examples-asp-net/tree/master/grid-page-methods-crud)
[ASP.NET Web Forms and WCF](https://github.com/telerik/kendo-examples-asp-net/tree/master/grid-wcf-crud)
[ASP.NET Web Forms and Web Services](https://github.com/telerik/kendo-examples-asp-net/tree/master/grid-web-service-crud)
[ASP.NET Web Forms and Web API](https://github.com/telerik/kendo-examples-asp-net/tree/master/grid-webapi-crud)
