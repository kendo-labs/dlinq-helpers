using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Kendo.DynamicLinq.Tests
{
    [KnownType(typeof(Person))]
    public class Person
    {
        public int Age { get; set; }
    }

    [TestFixture]
    public class SerializationTests
    {
        [Test]
        public void DataContractJsonSerializerSerializesEmptyAggregates()
        {
            using (var stream = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(DataSourceResult));

                serializer.WriteObject(stream, new[] { "foo" }.AsQueryable<string>().ToDataSourceResult(1, 0, null, null));

                Assert.AreEqual("{\"Aggregates\":null,\"Data\":[\"foo\"],\"Total\":1}", Encoding.UTF8.GetString(stream.ToArray()));
            }
        }

        [Test]
        public void DataContractJsonSerializerSerializesAggregates()
        {
            using (var stream = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(DataSourceResult), new [] { typeof (Person) });

                var people = new[] { new Person { Age = 30 }, new Person { Age = 30 } };

                serializer.WriteObject(stream, people.AsQueryable().ToDataSourceResult(1, 2, null, null, new [] { new Aggregator { 
                    Aggregate = "sum",
                    Field = "Age"
                } }));

                var json = Encoding.UTF8.GetString(stream.ToArray()).Replace("\"__type\":\"DynamicClass2:#\",", "");

                Assert.AreEqual("{\"Aggregates\":{\"Age\":{\"sum\":60}},\"Data\":[],\"Total\":2}", json);
            }
        }
    }
}
