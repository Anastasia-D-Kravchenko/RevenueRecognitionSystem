using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RevenueRecognitionSystem.Api.Entities;

namespace RevenueRecognitionSystem.Api.Configurations;

public class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("Contract");
        builder.HasKey(c => c.ContractId);

        builder.Property(c => c.SoftwareVersion).IsRequired().HasMaxLength(20);
        builder.Property(c => c.TotalPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(c => c.Status).IsRequired();

        builder.HasOne(c => c.Customer)
            .WithMany(c => c.Contracts)
            .HasForeignKey(c => c.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Software)
            .WithMany(s => s.Contracts)
            .HasForeignKey(c => c.SoftwareId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ContractPaymentConfiguration : IEntityTypeConfiguration<ContractPayment>
{
    public void Configure(EntityTypeBuilder<ContractPayment> builder)
    {
        builder.ToTable("ContractPayment");
        builder.HasKey(p => p.ContractPaymentId);
        builder.Property(p => p.Amount).HasColumnType("decimal(18,2)").IsRequired();

        builder.HasOne(p => p.Contract)
            .WithMany(c => c.Payments)
            .HasForeignKey(p => p.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
