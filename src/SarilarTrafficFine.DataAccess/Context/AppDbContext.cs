using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SarilarTrafficFine.DataAccess.Identity;
using SarilarTrafficFine.Entities.Models;

namespace SarilarTrafficFine.DataAccess.Context;

public sealed class AppDbContext
    : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<TrafficFine> TrafficFines => Set<TrafficFine>();

    public DbSet<ApprovalWorkflow> ApprovalWorkflows =>
        Set<ApprovalWorkflow>();

    public DbSet<ApprovalWorkflowStep> ApprovalWorkflowSteps =>
        Set<ApprovalWorkflowStep>();

    public DbSet<ApprovalHistory> ApprovalHistories =>
        Set<ApprovalHistory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);
    }
}