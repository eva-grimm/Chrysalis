using Chrysalis.Data;
using Chrysalis.Enums;
using Chrysalis.Extensions;
using Chrysalis.Models;
using Chrysalis.Services;
using Chrysalis.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<BTUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddClaimsPrincipalFactory<BTUserClaimsPrincipalFactory>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();

// registering custom services
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IRolesService, RolesService>();

// custom role policies
builder.Services.AddAuthorization(options =>
{
    // AdPm Policy - Requires Admin or ProjectManager role
    options.AddPolicy(nameof(BTPolicies.AdPm), policy =>
        policy.RequireRole(nameof(BTRoles.Admin), 
            nameof(BTRoles.ProjectManager)));
    // AdPmDev Policy - Requires Admin, ProjectManager, or Developer role
    options.AddPolicy(nameof(BTPolicies.AdPmDev), policy =>
        policy.RequireRole(nameof(BTRoles.Admin),
            nameof(BTRoles.ProjectManager),
            nameof(BTRoles.Developer)));
    // NoDemo Policy - All roles except DemoUser
    options.AddPolicy(nameof(BTPolicies.NoDemo), policy =>
        policy.RequireRole(nameof(BTRoles.Admin),
            nameof(BTRoles.ProjectManager),
            nameof(BTRoles.Developer),
            nameof(BTRoles.Submitter)));
});

builder.Services.AddMvc();

builder.Services.Configure<KestrelServerOptions>(options => options.Limits.MaxRequestBodySize = 1024 * 1024 * 100);

var app = builder.Build();

// access DataUtility
var scope = app.Services.CreateScope();
await DataUtility.ManageDataAsync(scope.ServiceProvider);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
