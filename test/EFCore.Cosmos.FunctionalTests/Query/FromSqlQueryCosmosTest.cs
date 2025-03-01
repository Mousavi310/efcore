﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.TestModels.Northwind;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.EntityFrameworkCore.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore.Query
{
    public class FromSqlQueryCosmosTest : QueryTestBase<NorthwindQueryCosmosFixture<NoopModelCustomizer>>
    {
        private static readonly string _eol = Environment.NewLine;

        public FromSqlQueryCosmosTest(
            NorthwindQueryCosmosFixture<NoopModelCustomizer> fixture,
            ITestOutputHelper testOutputHelper)
            : base(fixture)
        {
            ClearLog();
            //Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
        }

        protected NorthwindContext CreateContext()
            => Fixture.CreateContext();

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public async Task FromSqlRaw_queryable_simple(bool async)
        {
            using var context = CreateContext();
            var query = CosmosQueryableExtensions.FromSqlRaw(context.Set<Customer>(),
                @"SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer"" AND c[""ContactName""] LIKE '%z%'");

            var actual = async
                ? await query.ToArrayAsync()
                : query.ToArray();

            Assert.Equal(14, actual.Length);
            Assert.Equal(14, context.ChangeTracker.Entries().Count());

            AssertSql(
                @"SELECT c
FROM (
    SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer"" AND c[""ContactName""] LIKE '%z%'
) c");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public async Task FromSqlRaw_queryable_incorrect_discriminator_throws(bool async)
        {
            using var context = CreateContext();
            var query = CosmosQueryableExtensions.FromSqlRaw(context.Set<Customer>(),
                @"SELECT * FROM root c WHERE c[""Discriminator""] = ""Order""");

            var exception = async
                ? await Assert.ThrowsAsync<InvalidOperationException>(() => query.ToArrayAsync())
                : Assert.Throws<InvalidOperationException>(() => query.ToArray());

            Assert.Equal(
                CoreStrings.UnableToDiscriminate(context.Model.FindEntityType(typeof(Customer))!.DisplayName(), "Order"),
                exception.Message);
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public async Task FromSqlRaw_queryable_simple_columns_out_of_order(bool async)
        {
            using var context = CreateContext();
            var query = CosmosQueryableExtensions.FromSqlRaw(context.Set<Customer>(),
                @"SELECT c[""id""], c[""Discriminator""], c[""Region""], c[""PostalCode""], c[""Phone""], c[""Fax""], c[""CustomerID""], c[""Country""], c[""ContactTitle""], c[""ContactName""], c[""CompanyName""], c[""City""], c[""Address""] FROM root c WHERE c[""Discriminator""] = ""Customer""");

            var actual = async
                ? await query.ToArrayAsync()
                : query.ToArray();

            Assert.Equal(91, actual.Length);
            Assert.Equal(91, context.ChangeTracker.Entries().Count());

            AssertSql(
                @"SELECT c
FROM (
    SELECT c[""id""], c[""Discriminator""], c[""Region""], c[""PostalCode""], c[""Phone""], c[""Fax""], c[""CustomerID""], c[""Country""], c[""ContactTitle""], c[""ContactName""], c[""CompanyName""], c[""City""], c[""Address""] FROM root c WHERE c[""Discriminator""] = ""Customer""
) c");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public async Task FromSqlRaw_queryable_simple_columns_out_of_order_and_extra_columns(bool async)
        {
            using var context = CreateContext();
            var query = CosmosQueryableExtensions.FromSqlRaw(context.Set<Customer>(),
                @"SELECT c[""id""], c[""Discriminator""], c[""Region""], c[""PostalCode""], c[""PostalCode""] AS Foo, c[""Phone""], c[""Fax""], c[""CustomerID""], c[""Country""], c[""ContactTitle""], c[""ContactName""], c[""CompanyName""], c[""City""], c[""Address""] FROM root c WHERE c[""Discriminator""] = ""Customer""");

            var actual = async
                ? await query.ToArrayAsync()
                : query.ToArray();

            Assert.Equal(91, actual.Length);
            Assert.Equal(91, context.ChangeTracker.Entries().Count());

            AssertSql(
                @"SELECT c
FROM (
    SELECT c[""id""], c[""Discriminator""], c[""Region""], c[""PostalCode""], c[""PostalCode""] AS Foo, c[""Phone""], c[""Fax""], c[""CustomerID""], c[""Country""], c[""ContactTitle""], c[""ContactName""], c[""CompanyName""], c[""City""], c[""Address""] FROM root c WHERE c[""Discriminator""] = ""Customer""
) c");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public async Task FromSqlRaw_queryable_composed(bool async)
        {
            using var context = CreateContext();
            var query = CosmosQueryableExtensions.FromSqlRaw(context.Set<Customer>(),
                @"SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer""").Where(c => c.ContactName.Contains("z"));

            var sql = query.ToQueryString();

            var actual = async
                ? await query.ToArrayAsync()
                : query.ToArray();

            Assert.Equal(14, actual.Length);
            Assert.Equal(14, context.ChangeTracker.Entries().Count());

            AssertSql(
                @"SELECT c
FROM (
    SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer""
) c
WHERE CONTAINS(c[""ContactName""], ""z"")");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task FromSqlRaw_queryable_composed_after_removing_whitespaces(bool async)
        {
            using var context = CreateContext();
            var query = CosmosQueryableExtensions.FromSqlRaw(context.Set<Customer>(),
                    _eol + "    " + _eol + _eol + _eol + "SELECT" + _eol + @"* FROM root c WHERE c[""Discriminator""] = ""Customer""")
                .Where(c => c.ContactName.Contains("z"));

            var actual = async
                ? await query.ToArrayAsync()
                : query.ToArray();

            Assert.Equal(14, actual.Length);

            AssertSql(
                @"SELECT c
FROM (

        " + @"


    SELECT
    * FROM root c WHERE c[""Discriminator""] = ""Customer""
) c
WHERE CONTAINS(c[""ContactName""], ""z"")");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public async Task FromSqlRaw_queryable_composed_compiled(bool async)
        {
            if (async)
            {
                var query = EF.CompileAsyncQuery(
                    (NorthwindContext context) => CosmosQueryableExtensions.FromSqlRaw(context.Set<Customer>(),
                        @"SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer""")
                        .Where(c => c.ContactName.Contains("z")));

                using (var context = CreateContext())
                {
                    var actual = await query(context).ToListAsync();

                    Assert.Equal(14, actual.Count);
                }
            }
            else
            {
                var query = EF.CompileQuery(
                    (NorthwindContext context) => CosmosQueryableExtensions.FromSqlRaw(context.Set<Customer>(),
                        @"SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer""")
                        .Where(c => c.ContactName.Contains("z")));

                using (var context = CreateContext())
                {
                    var actual = query(context).ToArray();

                    Assert.Equal(14, actual.Length);
                }
            }

            AssertSql(
                @"SELECT c
FROM (
    SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer""
) c
WHERE CONTAINS(c[""ContactName""], ""z"")");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task FromSqlRaw_queryable_composed_compiled_with_parameter(bool async)
        {
            if (async)
            {
                var query = EF.CompileAsyncQuery(
                    (NorthwindContext context) => CosmosQueryableExtensions.FromSqlRaw(context.Set<Customer>(),
                        @"SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer"" AND c[""CustomerID""] = {0}", "CONSH")
                        .Where(c => c.ContactName.Contains("z")));

                using (var context = CreateContext())
                {
                    var actual = await query(context).ToListAsync();

                    Assert.Single(actual);
                }
            }
            else
            {
                var query = EF.CompileQuery(
                    (NorthwindContext context) => CosmosQueryableExtensions.FromSqlRaw(context.Set<Customer>(),
                        @"SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer"" AND c[""CustomerID""] = {0}", "CONSH")
                        .Where(c => c.ContactName.Contains("z")));

                using (var context = CreateContext())
                {
                    var actual = query(context).ToArray();

                    Assert.Single(actual);
                }
            }

            AssertSql(
                @"SELECT c
FROM (
    SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer"" AND c[""CustomerID""] = ""CONSH""
) c
WHERE CONTAINS(c[""ContactName""], ""z"")");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task FromSqlRaw_queryable_multiple_line_query(bool async)
        {
            using var context = CreateContext();
            var query = CosmosQueryableExtensions.FromSqlRaw(context.Set<Customer>(),
                @"SELECT *
FROM root c
WHERE c[""Discriminator""] = ""Customer"" AND c[""City""] = 'London'");

            var actual = async
                ? await query.ToArrayAsync()
                : query.ToArray();

            Assert.Equal(6, actual.Length);
            Assert.True(actual.All(c => c.City == "London"));

            AssertSql(
                @"SELECT c
FROM (
    SELECT *
    FROM root c
    WHERE c[""Discriminator""] = ""Customer"" AND c[""City""] = 'London'
) c");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task FromSqlRaw_queryable_composed_multiple_line_query(bool async)
        {
            using var context = CreateContext();
            var query = CosmosQueryableExtensions.FromSqlRaw(context.Set<Customer>(),
                    @"SELECT *
FROM root c
WHERE c[""Discriminator""] = ""Customer""")
                .Where(c => c.City == "London");

            var actual = async
                ? await query.ToArrayAsync()
                : query.ToArray();

            Assert.Equal(6, actual.Length);
            Assert.True(actual.All(c => c.City == "London"));

            AssertSql(
                @"SELECT c
FROM (
    SELECT *
    FROM root c
    WHERE c[""Discriminator""] = ""Customer""
) c
WHERE (c[""City""] = ""London"")");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public async Task FromSqlRaw_queryable_with_parameters(bool async)
        {
            var city = "London";
            var contactTitle = "Sales Representative";

            using var context = CreateContext();
            var query = CosmosQueryableExtensions.FromSqlRaw(context.Set<Customer>(),
                    @"SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer"" AND c[""City""] = {0} AND c[""ContactTitle""] = {1}", city,
                    contactTitle);

            var actual = async
                ? await query.ToArrayAsync()
                : query.ToArray();

            Assert.Equal(3, actual.Length);
            Assert.True(actual.All(c => c.City == "London"));
            Assert.True(actual.All(c => c.ContactTitle == "Sales Representative"));

            AssertSql(
                @"@p0='London'
@p1='Sales Representative'

SELECT c
FROM (
    SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer"" AND c[""City""] = @p0 AND c[""ContactTitle""] = @p1
) c");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public async Task FromSqlRaw_queryable_with_parameters_inline(bool async)
        {
            using var context = CreateContext();
            var query = CosmosQueryableExtensions.FromSqlRaw(context.Set<Customer>(),
                    @"SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer"" AND c[""City""] = {0} AND c[""ContactTitle""] = {1}", "London",
                    "Sales Representative");

            var actual = async
                ? await query.ToArrayAsync()
                : query.ToArray();

            Assert.Equal(3, actual.Length);
            Assert.True(actual.All(c => c.City == "London"));
            Assert.True(actual.All(c => c.ContactTitle == "Sales Representative"));

            AssertSql(
                @"@p0='London'
@p1='Sales Representative'

SELECT c
FROM (
    SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer"" AND c[""City""] = @p0 AND c[""ContactTitle""] = @p1
) c");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public async Task FromSqlRaw_queryable_with_null_parameter(bool async)
        {
            uint? reportsTo = null;

            using var context = CreateContext();
            var query = CosmosQueryableExtensions.FromSqlRaw(context.Set<Employee>(),
                    @"SELECT * FROM root c WHERE c[""Discriminator""] = ""Employee"" AND c[""ReportsTo""] = {0} OR (IS_NULL(c[""ReportsTo""]) AND IS_NULL({0}))", reportsTo);

            var actual = async
                ? await query.ToArrayAsync()
                : query.ToArray();

            Assert.Single(actual);

            AssertSql(
                @"@p0=null

SELECT c
FROM (
    SELECT * FROM root c WHERE c[""Discriminator""] = ""Employee"" AND c[""ReportsTo""] = @p0 OR (IS_NULL(c[""ReportsTo""]) AND IS_NULL(@p0))
) c");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public async Task FromSqlRaw_queryable_with_parameters_and_closure(bool async)
        {
            var city = "London";
            var contactTitle = "Sales Representative";

            using var context = CreateContext();
            var query = CosmosQueryableExtensions.FromSqlRaw(context.Set<Customer>(),
                    @"SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer"" AND c[""City""] = {0}", city)
                .Where(c => c.ContactTitle == contactTitle);
            var queryString = query.ToQueryString();

            var actual = async
                ? await query.ToArrayAsync()
                : query.ToArray();

            Assert.Equal(3, actual.Length);
            Assert.True(actual.All(c => c.City == "London"));
            Assert.True(actual.All(c => c.ContactTitle == "Sales Representative"));

            AssertSql(
                @"@p0='London'
@__contactTitle_1='Sales Representative'

SELECT c
FROM (
    SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer"" AND c[""City""] = @p0
) c
WHERE (c[""ContactTitle""] = @__contactTitle_1)");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task FromSqlRaw_queryable_simple_cache_key_includes_query_string(bool async)
        {
            using var context = CreateContext();
            var query = CosmosQueryableExtensions.FromSqlRaw(context.Set<Customer>(),
                @"SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer"" AND c[""City""] = 'London'");

            var actual = async
                ? await query.ToArrayAsync()
                : query.ToArray();

            Assert.Equal(6, actual.Length);
            Assert.True(actual.All(c => c.City == "London"));

            query = CosmosQueryableExtensions.FromSqlRaw(context.Set<Customer>(),
                @"SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer"" AND c[""City""] = 'Seattle'");

            actual = async
                ? await query.ToArrayAsync()
                : query.ToArray();

            Assert.Single(actual);
            Assert.True(actual.All(c => c.City == "Seattle"));

            AssertSql(
                @"SELECT c
FROM (
    SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer"" AND c[""City""] = 'London'
) c",
                //
                @"SELECT c
FROM (
    SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer"" AND c[""City""] = 'Seattle'
) c");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task FromSqlRaw_queryable_with_parameters_cache_key_includes_parameters(bool async)
        {
            var city = "London";
            var contactTitle = "Sales Representative";
            var sql = @"SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer"" AND c[""City""] = {0} AND c[""ContactTitle""] = {1}";

            using var context = CreateContext();
            var query = CosmosQueryableExtensions.FromSqlRaw(context.Set<Customer>(), sql, city, contactTitle);

            var actual = async
                ? await query.ToArrayAsync()
                : query.ToArray();

            Assert.Equal(3, actual.Length);
            Assert.True(actual.All(c => c.City == "London"));
            Assert.True(actual.All(c => c.ContactTitle == "Sales Representative"));

            city = "Madrid";
            contactTitle = "Accounting Manager";

            query = CosmosQueryableExtensions.FromSqlRaw(context.Set<Customer>(), sql, city, contactTitle);

            actual = async
                ? await query.ToArrayAsync()
                : query.ToArray();

            Assert.Equal(2, actual.Length);
            Assert.True(actual.All(c => c.City == "Madrid"));
            Assert.True(actual.All(c => c.ContactTitle == "Accounting Manager"));

            AssertSql(
                @"@p0='London'
@p1='Sales Representative'

SELECT c
FROM (
    SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer"" AND c[""City""] = @p0 AND c[""ContactTitle""] = @p1
) c",
                //
                @"@p0='Madrid'
@p1='Accounting Manager'

SELECT c
FROM (
    SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer"" AND c[""City""] = @p0 AND c[""ContactTitle""] = @p1
) c");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task FromSqlRaw_queryable_simple_as_no_tracking_not_composed(bool async)
        {
            using var context = CreateContext();
            var query = CosmosQueryableExtensions.FromSqlRaw(context.Set<Customer>(),
                @"SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer""")
                .AsNoTracking();

            var actual = async
                ? await query.ToArrayAsync()
                : query.ToArray();

            Assert.Equal(91, actual.Length);
            Assert.Empty(context.ChangeTracker.Entries());

            AssertSql(
                @"SELECT c
FROM (
    SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer""
) c");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task FromSqlRaw_queryable_simple_projection_composed(bool async)
        {
            using var context = CreateContext();
            var query = CosmosQueryableExtensions.FromSqlRaw(context.Set<Product>(),
                    @"SELECT *
FROM root c
WHERE c[""Discriminator""] = ""Product"" AND NOT c[""Discontinued""] AND ((c[""UnitsInStock""] + c[""UnitsOnOrder""]) < c[""ReorderLevel""])")
                .Select(p => p.ProductName);

            var actual = async
                ? await query.ToArrayAsync()
                : query.ToArray();

            Assert.Equal(2, actual.Length);

            AssertSql(
                @"SELECT c[""ProductName""]
FROM (
    SELECT *
    FROM root c
    WHERE c[""Discriminator""] = ""Product"" AND NOT c[""Discontinued""] AND ((c[""UnitsInStock""] + c[""UnitsOnOrder""]) < c[""ReorderLevel""])
) c");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task FromSqlRaw_composed_with_nullable_predicate(bool async)
        {
            using var context = CreateContext();
            var query = CosmosQueryableExtensions.FromSqlRaw(context.Set<Customer>(),
                @"SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer""")
                .Where(c => c.ContactName == c.CompanyName);

            var actual = async
                ? await query.ToArrayAsync()
                : query.ToArray();

            Assert.Empty(actual);

            AssertSql(
                @"SELECT c
FROM (
    SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer""
) c
WHERE (c[""ContactName""] = c[""CompanyName""])");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task FromSqlRaw_does_not_parameterize_interpolated_string(bool async)
        {
            using var context = CreateContext();
            var propertyName = "OrderID";
            var max = 10250;
            var query = CosmosQueryableExtensions.FromSqlRaw(context.Orders,
                $@"SELECT * FROM root c WHERE c[""Discriminator""] = ""Order"" AND c[""{propertyName}""] < {{0}}", max);

            var actual = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(2, actual.Count);
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task FromSqlRaw_queryable_simple_projection_not_composed(bool async)
        {
            using var context = CreateContext();
            var query = CosmosQueryableExtensions.FromSqlRaw(context.Set<Customer>(),
                @"SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer""")
                .Select(
                    c => new { c.CustomerID, c.City })
                .AsNoTracking();

            var actual = async
                ? await query.ToArrayAsync()
                : query.ToArray();

            Assert.Equal(91, actual.Length);
            Assert.Empty(context.ChangeTracker.Entries());

            AssertSql(
                @"SELECT c[""CustomerID""], c[""City""]
FROM (
    SELECT * FROM root c WHERE c[""Discriminator""] = ""Customer""
) c");
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public async Task FromSqlRaw_queryable_simple_with_missing_key_and_non_tracking_throws(bool async)
        {
            using var context = CreateContext();
            var query = CosmosQueryableExtensions
                .FromSqlRaw(
                    context.Set<Customer>(),
                    @"SELECT * FROM root c WHERE c[""Discriminator""] = ""Category""")
                .AsNoTracking();
            var exception = async
                ? await Assert.ThrowsAsync<InvalidOperationException>(() => query.ToArrayAsync())
                : Assert.Throws<InvalidOperationException>(() => query.ToArray());

            Assert.Equal(CoreStrings.InvalidKeyValue(
                context.Model.FindEntityType(typeof(Customer))!.DisplayName(),
                "CustomerID"),
                exception.Message);
        }

        private void AssertSql(params string[] expected)
            => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

        protected void ClearLog()
            => Fixture.TestSqlLoggerFactory.Clear();
    }
}
