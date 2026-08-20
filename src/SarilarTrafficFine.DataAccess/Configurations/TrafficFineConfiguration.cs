using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SarilarTrafficFine.DataAccess.Identity;
using SarilarTrafficFine.Entities.Models;

namespace SarilarTrafficFine.DataAccess.Configurations;

public sealed class TrafficFineConfiguration
    : IEntityTypeConfiguration<TrafficFine>
{
    public void Configure(EntityTypeBuilder<TrafficFine> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedByUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.Status);

        builder.HasIndex(x => x.CurrentApprovalStepId);

        builder.HasOne(x => x.Vehicle)
            .WithMany(x => x.TrafficFines)
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ApprovalWorkflow)
            .WithMany()
            .HasForeignKey(x => x.ApprovalWorkflowId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CurrentApprovalStep)
            .WithMany()
            .HasForeignKey(x => x.CurrentApprovalStepId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
            table.HasCheckConstraint(
                "CK_TrafficFines_Amount_Positive",
                "[Amount] > 0"));
    }
}