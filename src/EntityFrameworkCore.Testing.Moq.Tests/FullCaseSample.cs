using Apps72.Dev.Data.DbMocker;
using AutoFixture;
using EntityFrameworkCore.Testing.Common.Tests;
using EntityFrameworkCore.Testing.Moq.Helpers;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System.Linq;

namespace EntityFrameworkCore.Testing.Moq.Tests
{
    public class FullCaseSample : BaseForTests
    {
        [Test]
        public void Sample()
        {
            var entity = Fixture.Create<TestEntity>();

            // external dependency
            var conn = new MockDbConnection();
            conn.Mocks.When(w => w.CommandText.Equals("select 5 from test")).ReturnsRow(5);

            var mockedDbContext = new MockedDbContextBuilder<TestDbContext>().UseDbConnection(conn).MockedDbContext;
            mockedDbContext.TestEntities.Add(entity);
            mockedDbContext.SaveChanges();

            var connection = mockedDbContext.Database.GetDbConnection();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "select 5 from test";
                Assert.That(command.ExecuteNonQuery(), Is.EqualTo(5));
            }

            Assert.NotNull(mockedDbContext.TestEntities.FirstOrDefault());
        }
    }
}