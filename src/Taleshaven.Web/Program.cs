using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Taleshaven.Core.Media;
using Taleshaven.Infrastructure;
using Taleshaven.Infrastructure.Data;
using Taleshaven.Infrastructure.Identity;
using Taleshaven.Web;
using Taleshaven.Web.Components;
using Taleshaven.Web.Components.Account;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("Taleshaven")
    ?? throw new InvalidOperationException("Connection string 'Taleshaven' not found.");
// Uppladdade bilder lagras utanför wwwroot och serveras via /media (se nedan).
var mediaPath = Path.Combine(builder.Environment.ContentRootPath, builder.Configuration["Storage:MediaPath"] ?? "App_Data/media");
// Sajtens första administratörer anges med e-postadress (B18); därefter delar de ut roller på /admin/roles.
var adminEmails = builder.Configuration.GetSection("Admin:Emails").Get<string[]>() ?? [];
builder.Services.AddTaleshavenInfrastructure(connectionString, mediaPath, adminEmails);
builder.Services.AddSingleton<PostNotifier>();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddEntityFrameworkStores<TaleshavenDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>();

// TODO: Ersätt med en riktig e-posttjänst innan driftsättning.
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await app.Services.MigrateTaleshavenDatabaseAsync();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// Porträtt ur biblioteket (B19). Nyckeln är unik per uppladdning, så bilden kan cachas länge.
app.MapGet("/media/portraits/{key}", (string key, IImageStore images, HttpContext context) =>
    {
        var stream = images.OpenPortrait(key);
        if (stream is null)
            return Results.NotFound();

        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.CacheControl = "private, max-age=31536000, immutable";
        return Results.File(stream, IImageStore.PortraitContentType);
    })
    .RequireAuthorization();

app.Run();
