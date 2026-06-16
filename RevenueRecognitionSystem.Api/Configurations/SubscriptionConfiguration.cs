using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RevenueRecognitionSystem.Api.Entities;

namespace RevenueRecognitionSystem.Api.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscription");
        builder.HasKey(s => s.SubscriptionId);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.RenewalPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(s => s.Status).IsRequired();

        builder.HasOne(s => s.Customer)
            .WithMany(c => c.Subscriptions)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Software)
            .WithMany(sw => sw.Subscriptions)
            .HasForeignKey(s => s.SoftwareId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SubscriptionPaymentConfiguration : IEntityTypeConfiguration<SubscriptionPayment>
{
    public void Configure(EntityTypeBuilder<SubscriptionPayment> builder)
    {
        builder.ToTable("SubscriptionPayment");
        builder.HasKey(p => p.SubscriptionPaymentId);
        builder.Property(p => p.Amount).HasColumnType("decimal(18,2)").IsRequired();

        builder.HasOne(p => p.Subscription)
            .WithMany(s => s.Payments)
            .HasForeignKey(p => p.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
