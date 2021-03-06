using Xunit;
using FluentAssertions;

namespace Ooorm.Data.SqlServer.Tests
{
    public class WhereClause_Should
    {
        private class DbModel : DbItem<DbModel, int>
        {            
            public string Key { get; set; }
            public int Value { get; set; }
            public bool Active { get; set; }
        }

        private SqlServerQueryProvider<DbModel, int> provider => new SqlServerQueryProvider<DbModel, int>(() => null);
        
        private static readonly string KEY = $"[{nameof(DbModel.Key)}]";
        private static readonly string Value = $"[{nameof(DbModel.Value)}]";
        private static readonly string Active = $"[{nameof(DbModel.Active)}]";

        [Fact]
        public void SupportTrue()
        {
            string clause = provider.WhereClause(m => true);

            clause.Should().Be("WHERE 1");
        }

        [Fact]
        public void SupportFalse()
        {
            string clause = provider.WhereClause(m => false);

            clause.Should().Be("WHERE 0");
        }

        [Fact]
        public void SupportNullChecking()
        {
            string clause = provider.WhereClause(m => m.Key != null);

            clause.Should().Be($"WHERE ({KEY} IS NOT NULL)");
        }

        [Fact]
        public void SupportNotNullChecking()
        {
            string clause = provider.WhereClause(m => m.Key == null);

            clause.Should().Be($"WHERE ({KEY} IS NULL)");
        }

        [Fact]
        public void SupportFieldComparisonToParameterizedValue()
        {
            string clause = provider.WhereClause<DbModel>((m, p) => m.Key == p.Key, new DbModel());

            clause.Should().Be($"WHERE ({KEY} IS NULL)");

            clause = provider.WhereClause<DbModel>((m, p) => m.Key == p.Key, new DbModel { Key = "Hello World" });

            clause.Should().Be($"WHERE ({KEY} = @{nameof(DbModel.Key)})");
        }

        [Fact]
        public void SupportCompoundStatements_WithAnd()
        {
            string clause = provider.WhereClause(m => m.Key == null && m.Value > 2);

            clause.Should().Be($"WHERE (({KEY} IS NULL) AND ({Value} > 2))");
        }

        [Fact]
        public void SupportCompoundStatements_WithOr()
        {
            string clause = provider.WhereClause(m => m.Key == null || m.Value > 2);

            clause.Should().Be($"WHERE (({KEY} IS NULL) OR ({Value} > 2))");
        }

        [Fact]
        public void SupportNestedExpressions()
        {
            string clause = provider.WhereClause(m => (m.Key == null || m.Value > 2) && m.Active == true);

            clause.Should().Be($"WHERE ((({KEY} IS NULL) OR ({Value} > 2)) AND ({Active} = 1))");
        }
    }
}
