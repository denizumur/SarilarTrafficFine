using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SarilarTrafficFine.Entities.Models;

namespace SarilarTrafficFine.DataAccess.Configurations;

public sealed class ApprovalWorkflowConfiguration
    : IEntityTypeConfiguration<ApprovalWorkflow>
{
    public void Configure(EntityTypeBuilder<ApprovalWorkflow> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        builder.HasMany(x => x.Steps)
            .WithOne(x => x.ApprovalWorkflow)
            .HasForeignKey(x => x.ApprovalWorkflowId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}