using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SarilarTrafficFine.Entities.Models;
using SarilarTrafficFine.Business.Constants;

namespace SarilarTrafficFine.DataAccess.Configurations;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PlateNumber)
            .HasMaxLength(VehicleRules.PlateNumberMaxLength)
            .IsRequired();

        builder.Property(x => x.Brand)
            .HasMaxLength(VehicleRules.BrandMaxLength)
            .IsRequired();

        builder.Property(x => x.Model)
            .HasMaxLength(VehicleRules.ModelMaxLength)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(x => x.PlateNumber)
            .IsUnique();

        builder.HasMany(x => x.TrafficFines)
            .WithOne(x => x.Vehicle)
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}