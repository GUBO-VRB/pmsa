namespace pmsa.Tests.Infrastructure;

/// <summary>
/// One application, one database, one clock per test. xUnit builds a fresh instance of a test
/// class for every test method, so nothing leaks between scenarios.
/// </summary>
public abstract class ScenarioTest : IDisposable
{
    protected PmsaApplicationFactory App { get; }

    protected ScenarioTest() : this(new PmsaApplicationFactory())
    {
    }

    protected ScenarioTest(PmsaApplicationFactory factory) => App = factory;

    /// <summary>
    /// Story 004's weekly overview does not exist yet, so scenarios that say "the weekly
    /// overview" use another page that is behind the same authentication requirement. What they
    /// are really asserting — FR-004 — is the same either way.
    /// </summary>
    protected const string SomeProtectedPage = "/Privacy";

    public void Dispose()
    {
        App.Dispose();
        GC.SuppressFinalize(this);
    }
}
