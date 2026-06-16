using Microsoft.EntityFrameworkCore;
using RevenueRecognitionSystem.Api.Entities;

namespace RevenueRecognitionSystem.Api.Data;

public class AppDbContext : DbContext
{
    protected AppDbContext() { }
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<IndividualCustomer> IndividualCustomers => Set<IndividualCustomer>();
    public DbSet<CompanyCustomer> CompanyCustomers => Set<CompanyCustomer>();
    public DbSet<SoftwareCategory> SoftwareCategories => Set<SoftwareCategory>();
    public DbSet<Software> Softwares => Set<Software>();
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractPayment> ContractPayments => Set<ContractPayment>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<SubscriptionPayment> SubscriptionPayments => Set<SubscriptionPayment>();
    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
