using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RevenueRecognitionSystem.Api.Entities;

namespace RevenueRecognitionSystem.Api.Configurations;

public class DiscountConfiguration : IEntityTypeConfiguration<Discount>
{
    public void Configure(EntityTypeBuilder<Discount> builder)
    {
        builder.ToTable("Discount");
        builder.HasKey(d => d.DiscountId);
        builder.Property(d => d.Name).IsRequired().HasMaxLength(100);
        builder.Property(d => d.Offer).IsRequired();
        builder.Property(d => d.Value).HasColumnType("decimal(5,2)").IsRequired();

        builder.HasData(
            new Discount
            {
                DiscountId = 1, Name = "Black Friday", Offer = DiscountOffer.Contract,
                Value = 15m,
                StartDate = new DateTime(2026, 11, 24),
                EndDate = new DateTime(2026, 11, 30)
            },
            new Discount
            {
                DiscountId = 2, Name = "Spring Promo", Offer = DiscountOffer.Subscription,
                Value = 10m,
                StartDate = new DateTime(2026, 3, 1),
                EndDate = new DateTime(2026, 5, 31)
            });
    }
}
