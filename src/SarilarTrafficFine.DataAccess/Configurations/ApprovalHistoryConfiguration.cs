using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SarilarTrafficFine.DataAccess.Identity;
using SarilarTrafficFine.Entities.Models;

namespace SarilarTrafficFine.DataAccess.Configurations;

public sealed class ApprovalHistoryConfiguration
    : IEntityTypeConfiguration<ApprovalHistory>
{
    public void Configure(EntityTypeBuilder<ApprovalHistory> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ActionByUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.ActionByUserName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Comment)
            .HasMaxLength(1000);

        builder.Property(x => x.PreviousState)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.NewState)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.WorkflowStepName)
            .HasMaxLength(100);

        builder.HasIndex(x => new
        {
            x.TrafficFineId,
            x.ActionAt
        });

        builder.HasOne(x => x.TrafficFine)
            .WithMany(x => x.ApprovalHistories)
            .HasForeignKey(x => x.TrafficFineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.WorkflowStep)
            .WithMany()
            .HasForeignKey(x => x.WorkflowStepId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.ActionByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}