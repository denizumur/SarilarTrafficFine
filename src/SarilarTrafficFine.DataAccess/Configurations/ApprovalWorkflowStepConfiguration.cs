using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SarilarTrafficFine.Entities.Models;

namespace SarilarTrafficFine.DataAccess.Configurations;

public sealed class ApprovalWorkflowStepConfiguration
    : IEntityTypeConfiguration<ApprovalWorkflowStep>
{
    public void Configure(EntityTypeBuilder<ApprovalWorkflowStep> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.RequiredRole)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.ApprovalWorkflowId,
            x.StepOrder
        }).IsUnique();

        builder.ToTable(table =>
            table.HasCheckConstraint(
                "CK_ApprovalWorkflowSteps_StepOrder_Positive",
                "[StepOrder] > 0"));
    }
}