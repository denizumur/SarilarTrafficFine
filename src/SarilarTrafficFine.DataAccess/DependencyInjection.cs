using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SarilarTrafficFine.Business.Abstractions.Persistence;
using SarilarTrafficFine.DataAccess.Context;
using SarilarTrafficFine.DataAccess.Identity;
using SarilarTrafficFine.DataAccess.Repositories;
using SarilarTrafficFine.DataAccess.UnitOfWork;

namespace SarilarTrafficFine.DataAccess;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection baðlantý dizesi bulunamadý.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services
            .AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped(
            typeof(IGenericRepository<>),
            typeof(GenericRepository<>));

        services.AddScoped<
            IApprovalWorkflowRepository,
            ApprovalWorkflowRepository>();

        services.AddScoped<
            IUnitOfWork,
            EfUnitOfWork>();

        return services;
    }
}