namespace pmsa.Domain;

/// <summary>
/// A body of work that hours are booked to.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Owned by story 008</strong>, which is not built yet. This is the minimal shape
/// [spec 012](../docs/specs/012-date-range-project-report.md) reads — <c>id</c>, <c>name</c>,
/// <c>client</c> and <c>isActive</c> (Section 5.1) — so that the report has something to group
/// by. Story 008 adds the colour and the create/edit screens; it should extend this type rather
/// than introduce a second one.
/// </para>
/// <para>
/// Names are deliberately <em>not</em> unique: spec 012, EC-14 requires two projects sharing a
/// name to report as two separate slices, disambiguated by their client.
/// </para>
/// </remarks>
public class Project
{
    public const int NameMaxLength = 200;

    public const int ClientMaxLength = 200;

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// A label only. It is not a grouping dimension and confers no access boundary
    /// (spec 012, Section 5.1; spec 007, Section 1.3).
    /// </summary>
    public string? Client { get; private set; }

    /// <summary>
    /// False once archived. An archived project cannot be booked to (spec 001, FR-020) but keeps
    /// every hour it already holds (spec 012, FR-017).
    /// </summary>
    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedAt { get; private set; }

    private Project() { }

    public static Project Create(string name, string? client, DateTimeOffset now) =>
        new()
        {
            Name = name.Trim(),
            Client = string.IsNullOrWhiteSpace(client) ? null : client.Trim(),
            IsActive = true,
            CreatedAt = now,
        };

    /// <summary>Archiving is not deletion — see the <em>Deactivation and archiving are not deletion</em> invariant.</summary>
    public void Archive() => IsActive = false;

    public void Restore() => IsActive = true;

    public void Rename(string name) => Name = name.Trim();
}
