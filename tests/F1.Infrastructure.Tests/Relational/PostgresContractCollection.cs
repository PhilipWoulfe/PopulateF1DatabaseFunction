using Xunit;

namespace F1.Infrastructure.Tests.Relational;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgresContractCollection : ICollectionFixture<PostgresTestContainerFixture>
{
    public const string Name = "postgres-contract-tests";
}