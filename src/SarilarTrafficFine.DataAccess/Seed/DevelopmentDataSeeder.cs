using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SarilarTrafficFine.Business.Constants;
using SarilarTrafficFine.DataAccess.Context;
using SarilarTrafficFine.DataAccess.Identity;
using SarilarTrafficFine.Entities.Models;

namespace SarilarTrafficFine.DataAccess.Seed;

public static class DevelopmentDataSeeder
{
    private const string DemoPassword = "Demo123!";

    public static async Task SeedDevelopmentDataAsync(
        this IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var roleManager =
            scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await EnsureRoleAsync(roleManager, RoleNames.Operator);
        await EnsureRoleAsync(roleManager, RoleNames.Manager);
        await EnsureRoleAsync(roleManager, RoleNames.Finance);

        await EnsureUserAsync(
            userManager,
            "operator@demo.local",
            RoleNames.Operator);

        await EnsureUserAsync(
            userManager,
            "manager@demo.local",
            RoleNames.Manager);

        await EnsureUserAsync(
            userManager,
            "finance@demo.local",
            RoleNames.Finance);

        await EnsureTrafficFineWorkflowAsync(dbContext);
    }

    private static async Task EnsureRoleAsync(
        RoleManager<IdentityRole> roleManager,
        string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var result = await roleManager.CreateAsync(
            new IdentityRole(roleName));

        EnsureSucceeded(
            result,
            $"'{roleName}' rolü oluþturulamadý.");
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string roleName)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult =
                await userManager.CreateAsync(user, DemoPassword);

            EnsureSucceeded(
                createResult,
                $"'{email}' demo kullanýcýsý oluþturulamadý.");
        }

        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            var roleResult =
                await userManager.AddToRoleAsync(user, roleName);

            EnsureSucceeded(
                roleResult,
                $"'{email}' kullanýcýsýna '{roleName}' rolü atanamadý.");
        }
    }

    private static async Task EnsureTrafficFineWorkflowAsync(
        AppDbContext dbContext)
    {
        var exists = await dbContext.ApprovalWorkflows
            .AnyAsync(x => x.Code == WorkflowCodes.TrafficFine);

        if (exists)
        {
            return;
        }

        var workflow = new ApprovalWorkflow
        {
            Code = WorkflowCodes.TrafficFine,
            Name = "Trafik Cezasý Onay Akýþý",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            Steps =
            [
                new ApprovalWorkflowStep
                {
                    StepOrder = 1,
                    Name = "Yönetici Onayý",
                    RequiredRole = RoleNames.Manager
                },
                new ApprovalWorkflowStep
                {
                    StepOrder = 2,
                    Name = "Finans Onayý",
                    RequiredRole = RoleNames.Finance
                }
            ]
        };

        dbContext.ApprovalWorkflows.Add(workflow);

        await dbContext.SaveChangesAsync();
    }

    private static void EnsureSucceeded(
        IdentityResult result,
        string message)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join(
            "; ",
            result.Errors.Select(x => x.Description));

        throw new InvalidOperationException(
            $"{message} {errors}");
    }
}