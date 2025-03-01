﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore
{
    public class GraphUpdatesSqlServerHiLoTest : GraphUpdatesSqlServerTestBase<GraphUpdatesSqlServerHiLoTest.SqlServerFixture>
    {
        public GraphUpdatesSqlServerHiLoTest(SqlServerFixture fixture)
            : base(fixture)
        {
        }

        protected override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
            => facade.UseTransaction(transaction.GetDbTransaction());

        public class SqlServerFixture : GraphUpdatesSqlServerFixtureBase
        {
            protected override string StoreName { get; } = "GraphHiLoUpdatesTest";

            protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
            {
                modelBuilder.UseHiLo();

                base.OnModelCreating(modelBuilder, context);
            }
        }
    }
}
