using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Taleshaven.Core.Media;
using Taleshaven.Infrastructure;
using Taleshaven.Infrastructure.Data;
using Taleshaven.Infrastructure.Identity;
using Taleshaven.Infrastructure.Media;
using Taleshaven.Infrastructure.Portraits;
using Taleshaven.Web;
using Taleshaven.Web.Components;
using Taleshaven.Web.Components.Account;

// "images cut …" klipper ut ett källark till inkorgen (B65) i stället för att starta webbplatsen.
if (args is ["images", .. var imageArgs])
{
    Environment.ExitCode = SheetCommand.Run(imageArgs, Console.Out);
    return;
}

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
// Bildbibliotekets manifest skrivs bara i utvecklingsmiljön; i produktion läses det bara vid start (B65).
builder.Services.AddTaleshavenInfrastructure(connectionString, mediaPath, adminEmails, writeImageManifest: builder.Environment.IsDevelopment());
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

// E-post (B70): SMTP enligt avsnittet Email i konfigurationen; utan Email:Host skickas inga mejl.
var emailOptions = builder.Configuration.GetSection("Email").Get<EmailOptions>() ?? new EmailOptions();
if (string.IsNullOrWhiteSpace(emailOptions.Host))
    builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
else
{
    builder.Services.AddSingleton(emailOptions);
    builder.Services.AddSingleton<IEmailSender<ApplicationUser>, SmtpEmailSender>();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await app.Services.MigrateTaleshavenDatabaseAsync();
}

// Bildbiblioteket (B65): bilder i manifestet som saknas i databasen läggs till, och i utvecklingsläge läses inkorgen
// assets/new_images/ i repots rot. Ett fel här ska inte hindra sajten från att starta.
try
{
    var inbox = app.Environment.IsDevelopment()
        ? Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, builder.Configuration["Storage:InboxPath"] ?? "../../assets/new_images"))
        : null;
    await ImageLibrarySync.RunAsync(app.Services, inbox, adminEmails);
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Microsoft.EntityFrameworkCore.DbUpdateException)
{
    app.Logger.LogError(ex, "Bildbiblioteket kunde inte synkas vid start.");
}

if (!app.Environment.IsDevelopment())
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

// Bilder ur biblioteket (B19, B65). Nyckeln är unik per uppladdning, så bilden kan cachas länge.
app.MapGet("/media/images/{key}", (string key, IImageStore images, HttpContext context) =>
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
