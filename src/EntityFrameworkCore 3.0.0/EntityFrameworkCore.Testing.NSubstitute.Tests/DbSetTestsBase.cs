﻿using System;
using System.Collections.Generic;
using EntityFrameworkCore.Testing.Common.Tests;
using EntityFrameworkCore.Testing.NSubstitute.Extensions;
using EntityFrameworkCore.Testing.NSubstitute.Helpers;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace EntityFrameworkCore.Testing.NSubstitute.Tests
{
    [TestFixture]
    public abstract class DbSetTestsBase<T> : DbSetTestsBase<TestDbContext, T> where T : TestEntityBase
    {
        protected override TestDbContext CreateMockedDbContext()
        {
            return Create.SubstituteDbContextFor(new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options));
        }

        protected override void AddFromSqlRawResult(DbSet<T> mockedDbSet, IEnumerable<T> expectedResult)
        {
            mockedDbSet.AddFromSqlRawResult(expectedResult);
        }

        protected override void AddFromSqlRawResult(DbSet<T> mockedDbSet, string sql, IEnumerable<T> expectedResult)
        {
            mockedDbSet.AddFromSqlRawResult(sql, expectedResult);
        }

        protected override void AddFromSqlRawResult(DbSet<T> mockedDbSet, string sql, IEnumerable<object> parameters, IEnumerable<T> expectedResult)
        {
            mockedDbSet.AddFromSqlRawResult(sql, parameters, expectedResult);
        }

        protected override void AddFromSqlInterpolatedResult(DbSet<T> mockedDbSet, IEnumerable<T> expectedResult)
        {
            mockedDbSet.AddFromSqlInterpolatedResult(expectedResult);
        }

        protected override void AddFromSqlInterpolatedResult(DbSet<T> mockedDbSet, FormattableString sql, IEnumerable<T> expectedResult)
        {
            mockedDbSet.AddFromSqlInterpolatedResult(sql, expectedResult);
        }

        protected override void AddFromSqlInterpolatedResult(DbSet<T> mockedDbSet, string sql, IEnumerable<object> parameters, IEnumerable<T> expectedResult)
        {
            mockedDbSet.AddFromSqlInterpolatedResult(sql, parameters, expectedResult);
        }
    }
}