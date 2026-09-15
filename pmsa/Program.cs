using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using pmsa.Data;
using pmsa.Domain;
using pmsa.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddDbContext<PmsaDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Pmsa") ?? "Data Source=pmsa.db"));

builder.Services.Configure<PasswordHashingOptions>(builder.Configuration.GetSection("PasswordHashing"));
builder.Services.Configure<SeedAdminOptions>(builder.Configuration.GetSection(SeedAdminOptions.SectionName));

builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<AdminInvariantLock>();
builder.Services.AddScoped<SignInService>();
builder.Services.AddScoped<PasswordChangeService>();
builder.Services.AddScoped<PeopleAdministrationService>();
builder.Services.AddScoped<AdminSeeder>();

builder.Services
    .AddAuthentication(AuthenticationDefaults.Scheme)
    .AddCookie(AuthenticationDefaults.Scheme, options =>
    {
        options.Cookie.Name = AuthenticationDefaults.CookieName;
        options.Cookie.HttpOnly = true;                                 // NFR-002
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;        // NFR-002
        options.Cookie.SameSite = SameSiteMode.Lax;                     // NFR-002
        options.Cookie.IsEssential = true;

        // FR-007: 12 hours from sign-in, never extended by activity. The cookie itself is
        // non-persistent (see SignIn.cshtml.cs), so closing the browser also ends the session.
        options.ExpireTimeSpan = AuthenticationDefaults.SessionLifetime;
        options.SlidingExpiration = false;

        options.LoginPath = AuthenticationDefaults.SignInPath;          // FR-004
        options.LogoutPath = AuthenticationDefaults.SignOutPath;
        options.AccessDeniedPath = AuthenticationDefaults.ForbiddenPath; // FR-010
        options.ReturnUrlParameter = "returnUrl";                        // NFR-008

        // FR-017 and FR-021: role and active state are read from the Person on every request.
        options.EventsType = typeof(RevalidatePrincipalEvents);
    });

builder.Services.AddScoped<RevalidatePrincipalEvents>();

// Let the cookie handler read the clock the rest of the application reads, so that session
// expiry (FR-007) and lockout expiry (FR-008) can be exercised by the same fake clock.
builder.Services.AddOptions<CookieAuthenticationOptions>(AuthenticationDefaults.Scheme)
    .Configure<TimeProvider>((options, clock) => options.TimeProvider = clock);

builder.Services.AddAuthorizationBuilder()
    // FR-004: every page requires an authenticated session unless it opts out explicitly.
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy(AuthorizationPolicies.RequireManager, policy =>
        policy.RequireRole(nameof(Role.Manager), nameof(Role.Admin)))
    .AddPolicy(AuthorizationPolicies.RequireAdmin, policy =>
        policy.RequireRole(nameof(Role.Admin)));

builder.Services.AddRazorPages(options =>
{
    // The sign-in page and the error page are the only pages reachable without a session.
    options.Conventions.AllowAnonymousToPage(AuthenticationDefaults.SignInPath);
    options.Conventions.AllowAnonymousToPage("/Error");

    // Section 2.1: account administration is Admin-only, enforced at the route so that FR-010
    // holds for a directly-addressed request with no interface involved.
    options.Conventions.AuthorizeFolder("/Admin", AuthorizationPolicies.RequireAdmin);

    options.Conventions.ConfigureFilter(new NoStoreFilter());           // NFR-005
    options.Conventions.ConfigureFilter(new ForcePasswordChangeFilter()); // FR-014
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();                                              // NFR-002

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Static assets are endpoints too, so the fallback policy of FR-004 would otherwise demand a
// session for the stylesheet the sign-in page is asking for. Stylesheets and scripts are not
// the confidential material that policy exists to protect — pages are.
app.MapStaticAssets().AllowAnonymous();

app.MapRazorPages()
   .WithStaticAssets();

// Bring the schema up to date and seed the first Admin (FR-020). Both steps are idempotent
// (NFR-007), so they run on every start.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PmsaDbContext>();
    await db.Database.MigrateAsync();

    var seedOptions = scope.ServiceProvider.GetRequiredService<IOptions<SeedAdminOptions>>().Value;
    await scope.ServiceProvider.GetRequiredService<AdminSeeder>().SeedAsync(seedOptions);
}

app.Run();

/// <summary>Exposed so that the test project can host the application through WebApplicationFactory.</summary>
public partial class Program;
