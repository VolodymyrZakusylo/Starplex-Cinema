using Xunit;

namespace StarPlex.Application.IntegrationTests.Infrastructure;

[CollectionDefinition("IntegrationTestCollection", DisableParallelization = true)]
public class IntegrationTestCollection : ICollectionFixture<DatabaseFixture>
{
}
