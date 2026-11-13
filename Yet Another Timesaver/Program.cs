using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Radzen;
using Yet_Another_Timesaver.Backend;
using Yet_Another_Timesaver.Components;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
// Authentifizierung über Cookies
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";

        options.Cookie.Name = "YATs.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddRadzenComponents();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<TimeService>();

var supportedLanguages = new[] { "en", "de" };

var app = builder.Build();
/*
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}
*/
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/culture/set", (string culture, string redirectUri, HttpContext ctx) =>
{
    ctx.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)));
    return Results.LocalRedirect(redirectUri);
});

app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures(supportedLanguages)
    .AddSupportedUICultures(supportedLanguages));

app.UseAntiforgery();

app.MapStaticAssets();

// Anmeldung
app.MapPost("/account/login", async (
    HttpContext context,
    IAntiforgery antiforgery) =>
{
    // Schutz gegen gefälschte Formular-Anfragen
    await antiforgery.ValidateRequestAsync(context);

    var form = await context.Request.ReadFormAsync();

    var username = form["username"].ToString();
    var password = form["password"].ToString();

    // Fest codierte Test-Zugangsdaten
    const string testUsername = "admin";
    const string testPassword = "Start123";

    if (username != testUsername || password != testPassword)
    {
        return Results.Redirect("/login?error=1");
    }

    var claims = new[]
    {
        new Claim(ClaimTypes.Name, testUsername),
        new Claim(ClaimTypes.NameIdentifier, "test-admin")
    };

    var identity = new ClaimsIdentity(
        claims,
        CookieAuthenticationDefaults.AuthenticationScheme);

    var principal = new ClaimsPrincipal(identity);

    await context.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties
        {
            IsPersistent = true
        });

    return Results.Redirect("/");
})
.AllowAnonymous();


// Abmeldung
app.MapPost("/account/logout", async (
    HttpContext context,
    IAntiforgery antiforgery) =>
{
    await antiforgery.ValidateRequestAsync(context);

    await context.SignOutAsync(
        CookieAuthenticationDefaults.AuthenticationScheme);

    return Results.Redirect("/login");
})
.AllowAnonymous();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
