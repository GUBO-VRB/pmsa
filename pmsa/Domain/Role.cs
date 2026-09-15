namespace pmsa.Domain;

/// <summary>
/// The three global capability levels of spec 007, Section 2.1. A role grants the same
/// capabilities everywhere — there are no per-project roles (the <em>Roles are global</em>
/// invariant).
/// </summary>
public enum Role
{
    /// <summary>Own time entries and own-hours reporting only.</summary>
    User = 0,

    /// <summary>Everything a User may do, plus projects, budgets, assignments and all entries.</summary>
    Manager = 1,

    /// <summary>Everything a Manager may do, plus account creation, roles and deactivation.</summary>
    Admin = 2,
}
