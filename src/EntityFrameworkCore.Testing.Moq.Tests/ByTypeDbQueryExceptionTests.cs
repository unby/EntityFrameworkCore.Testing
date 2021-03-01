﻿using EntityFrameworkCore.Testing.Common.Tests;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace EntityFrameworkCore.Testing.Moq.Tests
{
    public class ByTypeDbQueryExceptionTests : ReadOnlyDbSetExceptionTests<ViewEntity>
    {
        protected TestDbContext MockedDbContext;

        protected override DbSet<ViewEntity> DbSet => MockedDbContext.Query<ViewEntity>();

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            MockedDbContext = Create.MockedDbContextFor<TestDbContext>();
        }
    }
}