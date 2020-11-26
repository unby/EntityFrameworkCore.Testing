﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using EntityFrameworkCore.Testing.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using rgvlee.Core.Common.Helpers;

namespace EntityFrameworkCore.Testing.NSubstitute.Extensions
{
    public static partial class ReadOnlyDbSetExtensions
    {
        internal static DbSet<TEntity> CreateMockedReadOnlyDbSet<TEntity>(this DbSet<TEntity> readOnlyDbSet) where TEntity : class
        {
            EnsureArgument.IsNotNull(readOnlyDbSet, nameof(readOnlyDbSet));

            var mockedReadOnlyDbSet = (DbSet<TEntity>) Substitute.For(new[] {
                    typeof(DbSet<TEntity>),
                    typeof(IAsyncEnumerable<TEntity>),
                    typeof(IEnumerable),
                    typeof(IEnumerable<TEntity>),
                    typeof(IInfrastructure<IServiceProvider>),
                    typeof(IListSource),
                    typeof(IQueryable<TEntity>)
                },
                new object[] { });

            var queryable = new List<TEntity>().AsQueryable();
            var asyncEnumerable = new AsyncEnumerable<TEntity>(queryable);
            var mockedQueryProvider = ((IQueryable<TEntity>) readOnlyDbSet).Provider.CreateMockedQueryProvider(asyncEnumerable);

            var invalidOperationException = new InvalidOperationException(
                $"Unable to track an instance of type '{typeof(TEntity).Name}' because it does not have a primary key. Only entity types with primary keys may be tracked.");

            mockedReadOnlyDbSet.Add(Arg.Any<TEntity>()).Throws(callInfo => invalidOperationException);
            mockedReadOnlyDbSet.AddAsync(Arg.Any<TEntity>(), Arg.Any<CancellationToken>()).Throws(callInfo => invalidOperationException);
            mockedReadOnlyDbSet.When(x => x.AddRange(Arg.Any<IEnumerable<TEntity>>())).Do(callInfo => throw invalidOperationException);
            mockedReadOnlyDbSet.When(x => x.AddRange(Arg.Any<TEntity[]>())).Do(callInfo => throw invalidOperationException);
            mockedReadOnlyDbSet.AddRangeAsync(Arg.Any<IEnumerable<TEntity>>(), Arg.Any<CancellationToken>()).Throws(callInfo => invalidOperationException);
            mockedReadOnlyDbSet.AddRangeAsync(Arg.Any<TEntity[]>()).Throws(callInfo => invalidOperationException);

            mockedReadOnlyDbSet.Attach(Arg.Any<TEntity>()).Throws(callInfo => invalidOperationException);
            mockedReadOnlyDbSet.When(x => x.AttachRange(Arg.Any<IEnumerable<TEntity>>())).Do(callInfo => throw invalidOperationException);
            mockedReadOnlyDbSet.When(x => x.AttachRange(Arg.Any<TEntity[]>())).Do(callInfo => throw invalidOperationException);

            ((IListSource) mockedReadOnlyDbSet).ContainsListCollection.Returns(callInfo => false);

            ((IQueryable<TEntity>) mockedReadOnlyDbSet).ElementType.Returns(callInfo => asyncEnumerable.ElementType);
            ((IQueryable<TEntity>) mockedReadOnlyDbSet).Expression.Returns(callInfo => asyncEnumerable.Expression);

            mockedReadOnlyDbSet.Find(Arg.Any<object[]>()).Throws(callInfo => new NullReferenceException());
            mockedReadOnlyDbSet.FindAsync(Arg.Any<object[]>()).Throws(callInfo => new NullReferenceException());
            mockedReadOnlyDbSet.FindAsync(Arg.Any<object[]>(), Arg.Any<CancellationToken>()).Throws(callInfo => new NullReferenceException());

            ((IAsyncEnumerable<TEntity>) mockedReadOnlyDbSet).GetAsyncEnumerator(Arg.Any<CancellationToken>())
                .Returns(callInfo => asyncEnumerable.GetAsyncEnumerator(callInfo.Arg<CancellationToken>()));

            ((IEnumerable) mockedReadOnlyDbSet).GetEnumerator().Returns(callInfo => ((IEnumerable) asyncEnumerable).GetEnumerator());
            ((IEnumerable<TEntity>) mockedReadOnlyDbSet).GetEnumerator().Returns(callInfo => ((IEnumerable<TEntity>) asyncEnumerable).GetEnumerator());

            ((IListSource) mockedReadOnlyDbSet).GetList().Returns(callInfo => asyncEnumerable.ToList());

            ((IInfrastructure<IServiceProvider>) mockedReadOnlyDbSet).Instance.Returns(callInfo => ((IInfrastructure<IServiceProvider>) readOnlyDbSet).Instance);

            mockedReadOnlyDbSet.Local.Throws(callInfo =>
                new InvalidOperationException($"The invoked method cannot be used for the entity type '{typeof(TEntity).Name}' because it does not have a primary key."));

            mockedReadOnlyDbSet.Remove(Arg.Any<TEntity>()).Throws(callInfo => invalidOperationException);
            mockedReadOnlyDbSet.When(x => x.RemoveRange(Arg.Any<IEnumerable<TEntity>>())).Do(callInfo => throw invalidOperationException);
            mockedReadOnlyDbSet.When(x => x.RemoveRange(Arg.Any<TEntity[]>())).Do(callInfo => throw invalidOperationException);

            mockedReadOnlyDbSet.Update(Arg.Any<TEntity>()).Throws(callInfo => invalidOperationException);
            mockedReadOnlyDbSet.When(x => x.UpdateRange(Arg.Any<IEnumerable<TEntity>>())).Do(callInfo => throw invalidOperationException);
            mockedReadOnlyDbSet.When(x => x.UpdateRange(Arg.Any<TEntity[]>())).Do(callInfo => throw invalidOperationException);

            ((IQueryable<TEntity>) mockedReadOnlyDbSet).Provider.Returns(callInfo => mockedQueryProvider);

            mockedReadOnlyDbSet.AsAsyncEnumerable().Returns(asyncEnumerable);
            mockedReadOnlyDbSet.AsQueryable().Returns(asyncEnumerable);

            return mockedReadOnlyDbSet;
        }

        internal static void SetSource<TEntity>(this DbSet<TEntity> mockedReadOnlyDbSet, IEnumerable<TEntity> source) where TEntity : class
        {
            EnsureArgument.IsNotNull(mockedReadOnlyDbSet, nameof(mockedReadOnlyDbSet));
            EnsureArgument.IsNotNull(source, nameof(source));

            var queryable = source.AsQueryable();
            var asyncEnumerable = new AsyncEnumerable<TEntity>(queryable);

            ((IQueryable<TEntity>) mockedReadOnlyDbSet).Expression.Returns(callInfo => asyncEnumerable.Expression);

            ((IAsyncEnumerable<TEntity>) mockedReadOnlyDbSet).GetAsyncEnumerator(Arg.Any<CancellationToken>())
                .Returns(callInfo => asyncEnumerable.GetAsyncEnumerator(callInfo.Arg<CancellationToken>()));

            ((IEnumerable) mockedReadOnlyDbSet).GetEnumerator().Returns(callInfo => ((IEnumerable) asyncEnumerable).GetEnumerator());
            ((IEnumerable<TEntity>) mockedReadOnlyDbSet).GetEnumerator().Returns(callInfo => ((IEnumerable<TEntity>) asyncEnumerable).GetEnumerator());

            var provider = ((IQueryable<TEntity>) mockedReadOnlyDbSet).Provider;
            ((AsyncQueryProvider<TEntity>) provider).SetSource(asyncEnumerable);

            mockedReadOnlyDbSet.AsAsyncEnumerable().Returns(asyncEnumerable);
            mockedReadOnlyDbSet.AsQueryable().Returns(asyncEnumerable);
        }
    }
}