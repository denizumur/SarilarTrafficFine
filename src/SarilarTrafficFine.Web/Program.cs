using QuestPDF.Infrastructure;
using SarilarTrafficFine.Business.Features.TrafficFines;
using SarilarTrafficFine.Business.Features.Vehicles;
using SarilarTrafficFine.DataAccess;
using SarilarTrafficFine.DataAccess.Seed;

QuestPDF.Settings.License =
    LicenseType.Community;

var builder =
    WebApplication.CreateBuilder(
        args);

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<
    IVehicleService,
    VehicleService>();

builder.Services.AddScoped<
    ITrafficFineService,
    TrafficFineService>();

builder.Services.AddScoped<
    ITrafficFineApprovalQueryService,
    TrafficFineApprovalQueryService>();

builder.Services.AddDataAccess(
    builder.Configuration);

builder.Services.ConfigureApplicationCookie(
    options =>
    {
        options.LoginPath =
            "/Account/Login";

        options.AccessDeniedPath =
            "/Account/AccessDenied";
    });

var app =
    builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.Services
        .SeedDevelopmentDataAsync();
}
else
{
    app.UseExceptionHandler(
        "/Home/Error");

    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}");

app.Run();