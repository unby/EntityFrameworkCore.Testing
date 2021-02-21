﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using EntityFrameworkCore.Testing.Common;
using EntityFrameworkCore.Testing.Common.Helpers;
using EntityFrameworkCore.Testing.Moq.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Moq;
using rgvlee.Core.Common.Extensions;
using rgvlee.Core.Common.Helpers;

namespace EntityFrameworkCore.Testing.Moq.Helpers
{
    internal class NoSetUpDefaultValueProvider<TDbContext> : DefaultValueProvider where TDbContext : DbContext
    {
        private static readonly ILogger<NoSetUpDefaultValueProvider<TDbContext>> Logger = LoggingHelper.CreateLogger<NoSetUpDefaultValueProvider<TDbContext>>();

        private readonly TDbContext _dbContext;

        private readonly List<IEntityType> _allModelEntityTypes;

        private readonly List<PropertyInfo> _dbContextModelProperties;

        public NoSetUpDefaultValueProvider(TDbContext dbContext)
        {
            _dbContext = dbContext;
            _allModelEntityTypes = _dbContext.Model.GetEntityTypes().ToList();
            _dbContextModelProperties = _dbContext
                .GetType()
                .GetProperties()
                .Where(x => x.PropertyType.IsGenericType &&
                            (x.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) || x.PropertyType.GetGenericTypeDefinition() == typeof(DbQuery<>)))
                .ToList();
        }

        protected override object GetDefaultValue(Type type, Mock mock)
        {
            var lastInvocation = mock.Invocations.Last();
            var lastInvocationIsByType = lastInvocation.Method.Name.Equals("Query") || lastInvocation.Method.Name.Equals("Set");
            var dbContextModelProperty = _dbContextModelProperties.SingleOrDefault(x => x.GetMethod.Name.Equals(lastInvocation.Method.Name));
            var lastInvocationIsByProperty = dbContextModelProperty != null;

            if (lastInvocationIsByType || lastInvocationIsByProperty)
            {
                var setType = lastInvocationIsByType ? lastInvocation.Method.GetGenericArguments().Single() : dbContextModelProperty.PropertyType.GetGenericArguments().Single();

                Logger.LogDebug("Setting up setType: '{setType}'", setType);

                var entityType = _allModelEntityTypes.SingleOrDefault(x => x.ClrType.Equals(setType));
                if (entityType == null)
                {
                    throw new InvalidOperationException(
                        string.Format(ExceptionMessages.CannotCreateDbSetTypeNotIncludedInModel,
                        lastInvocation.Method.GetGenericArguments().Single().Name));
                }

                var setUpDbSetForMethod = typeof(NoSetUpDefaultValueProvider<TDbContext>)
                    .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                    .Single(x => x.Name.Equals(entityType.FindPrimaryKey() != null ? "SetUpDbSetFor" : "SetUpDbQueryFor"));

                setUpDbSetForMethod.MakeGenericMethod(setType).Invoke(this, new[] { mock });

                return lastInvocation.Method.Invoke(mock.Object, null);
            }

            return type.GetDefaultValue();
        }

        private void SetUpDbSetFor<TEntity>(Mock<TDbContext> dbContextMock) where TEntity : class
        {
            var mockedDbSet = _dbContext.Set<TEntity>().CreateMockedDbSet();

            var property = typeof(TDbContext).GetProperties().SingleOrDefault(p => p.PropertyType == typeof(DbSet<TEntity>));

            if (property != null)
            {
                var expression = ExpressionHelper.CreatePropertyExpression<TDbContext, DbSet<TEntity>>(property);
                dbContextMock.Setup(expression).Returns(mockedDbSet);
            }
            else
            {
                Logger.LogDebug($"Could not find a DbContext property for type '{typeof(TEntity)}'");
            }

            dbContextMock.Setup(m => m.Set<TEntity>()).Returns(mockedDbSet);

            dbContextMock.Setup(m => m.Add(It.IsAny<TEntity>())).Returns((TEntity providedEntity) => _dbContext.Add(providedEntity));
            dbContextMock.Setup(m => m.AddAsync(It.IsAny<TEntity>(), It.IsAny<CancellationToken>()))
                .Returns((TEntity providedEntity, CancellationToken providedCancellationToken) => _dbContext.AddAsync(providedEntity, providedCancellationToken));

            dbContextMock.Setup(m => m.Attach(It.IsAny<TEntity>())).Returns((TEntity providedEntity) => _dbContext.Attach(providedEntity));
            dbContextMock.Setup(m => m.AttachRange(It.IsAny<object[]>())).Callback((object[] providedEntities) => _dbContext.AttachRange(providedEntities));
            dbContextMock.Setup(m => m.AttachRange(It.IsAny<IEnumerable<object>>())).Callback((IEnumerable<object> providedEntities) => _dbContext.AttachRange(providedEntities));

            dbContextMock.Setup(m => m.Entry(It.IsAny<TEntity>())).Returns((TEntity providedEntity) => _dbContext.Entry(providedEntity));

            dbContextMock.Setup(m => m.Find<TEntity>(It.IsAny<object[]>())).Returns((object[] providedKeyValues) => _dbContext.Find<TEntity>(providedKeyValues));
            dbContextMock.Setup(m => m.Find(typeof(TEntity), It.IsAny<object[]>()))
                .Returns((Type providedType, object[] providedKeyValues) => _dbContext.Find(providedType, providedKeyValues));
            dbContextMock.Setup(m => m.FindAsync<TEntity>(It.IsAny<object[]>())).Returns((object[] providedKeyValues) => _dbContext.FindAsync<TEntity>(providedKeyValues));
            dbContextMock.Setup(m => m.FindAsync<TEntity>(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns((object[] providedKeyValues, CancellationToken providedCancellationToken) => _dbContext.FindAsync<TEntity>(providedKeyValues, providedCancellationToken));

            dbContextMock.Setup(m => m.Remove(It.IsAny<TEntity>())).Returns((TEntity providedEntity) => _dbContext.Remove(providedEntity));

            dbContextMock.Setup(m => m.Update(It.IsAny<TEntity>())).Returns((TEntity providedEntity) => _dbContext.Update(providedEntity));
        }

        private void SetUpDbQueryFor<TQuery>(Mock<TDbContext> dbContextMock) where TQuery : class
        {
            var mockedDbQuery = _dbContext.Query<TQuery>().CreateMockedDbQuery();

            var property = typeof(TDbContext).GetProperties().SingleOrDefault(p => p.PropertyType == typeof(DbQuery<TQuery>));

            if (property != null)
            {
                var expression = ExpressionHelper.CreatePropertyExpression<TDbContext, DbQuery<TQuery>>(property);
                dbContextMock.Setup(expression).Returns(mockedDbQuery);
            }
            else
            {
                Logger.LogDebug($"Could not find a DbContext property for type '{typeof(TQuery)}'");
            }

            dbContextMock.Setup(m => m.Query<TQuery>()).Returns(mockedDbQuery);
        }
    }
}