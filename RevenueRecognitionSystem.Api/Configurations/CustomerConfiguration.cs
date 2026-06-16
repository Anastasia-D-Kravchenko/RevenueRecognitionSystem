using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RevenueRecognitionSystem.Api.Entities;

namespace RevenueRecognitionSystem.Api.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customer");
        builder.HasKey(c => c.CustomerId);

        builder.Property(c => c.Address).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(100);
        builder.Property(c => c.PhoneNumber).IsRequired().HasMaxLength(30);

        builder
            .HasDiscriminator<string>("CustomerType")
            .HasValue<IndividualCustomer>("Individual")
            .HasValue<CompanyCustomer>("Company");
    }
}

public class IndividualCustomerConfiguration : IEntityTypeConfiguration<IndividualCustomer>
{
    public void Configure(EntityTypeBuilder<IndividualCustomer> builder)
    {
        builder.Property(c => c.FirstName).IsRequired().HasMaxLength(50);
        builder.Property(c => c.LastName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Pesel).IsRequired().HasMaxLength(11);
        builder.HasIndex(c => c.Pesel).IsUnique();

        builder.HasData(new IndividualCustomer
        {
            CustomerId = 1,
            FirstName = "John",
            LastName = "Doe",
            Pesel = "92010112345",
            Address = "Wesola 1, Warszawa",
            Email = "john@example.com",
            PhoneNumber = "+48123456789"
        });
    }
}

public class CompanyCustomerConfiguration : IEntityTypeConfiguration<CompanyCustomer>
{
    public void Configure(EntityTypeBuilder<CompanyCustomer> builder)
    {
        builder.Property(c => c.CompanyName).IsRequired().HasMaxLength(150);
        builder.Property(c => c.Krs).IsRequired().HasMaxLength(10);
        builder.HasIndex(c => c.Krs).IsUnique();

        builder.HasData(new CompanyCustomer
        {
            CustomerId = 2,
            CompanyName = "ABC Sp. z o.o.",
            Krs = "0000123456",
            Address = "Marszalkowska 100, Warszawa",
            Email = "office@abc.pl",
            PhoneNumber = "+48222333444"
        });
    }
}
