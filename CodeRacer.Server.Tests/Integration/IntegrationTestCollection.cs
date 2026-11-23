using Xunit;

namespace CodeRacer.Server.Tests.Integration;

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<CodeRacerWebApplicationFactory>
{
    public const string Name = "PostgreSQL integration tests";
}
