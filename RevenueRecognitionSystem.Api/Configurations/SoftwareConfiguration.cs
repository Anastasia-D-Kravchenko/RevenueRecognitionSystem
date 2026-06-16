using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RevenueRecognitionSystem.Api.Entities;

namespace RevenueRecognitionSystem.Api.Configurations;

public class SoftwareCategoryConfiguration : IEntityTypeConfiguration<SoftwareCategory>
{
    public void Configure(EntityTypeBuilder<SoftwareCategory> builder)
    {
        builder.ToTable("SoftwareCategory");
        builder.HasKey(c => c.SoftwareCategoryId);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(50);
        builder.HasIndex(c => c.Name).IsUnique();

        builder.HasData(
            new SoftwareCategory { SoftwareCategoryId = 1, Name = "Finance" },
            new SoftwareCategory { SoftwareCategoryId = 2, Name = "Education" },
            new SoftwareCategory { SoftwareCategoryId = 3, Name = "Productivity" });
    }
}

public class SoftwareConfiguration : IEntityTypeConfiguration<Software>
{
    public void Configure(EntityTypeBuilder<Software> builder)
    {
        builder.ToTable("Software");
        builder.HasKey(s => s.SoftwareId);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Description).IsRequired().HasMaxLength(500);
        builder.Property(s => s.CurrentVersion).IsRequired().HasMaxLength(20);
        builder.Property(s => s.YearlyLicensePrice).HasColumnType("decimal(18,2)").IsRequired();

        builder.HasOne(s => s.Category)
            .WithMany(c => c.Softwares)
            .HasForeignKey(s => s.SoftwareCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new Software
            {
                SoftwareId = 1, Name = "AccPro", Description = "Accounting suite",
                CurrentVersion = "2026.1", YearlyLicensePrice = 5000m, SoftwareCategoryId = 1
            },
            new Software
            {
                SoftwareId = 2, Name = "ClassNote", Description = "Classroom management",
                CurrentVersion = "10.4", YearlyLicensePrice = 1200m, SoftwareCategoryId = 2
            });
    }
}
