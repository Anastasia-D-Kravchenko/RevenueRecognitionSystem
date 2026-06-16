using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RevenueRecognitionSystem.Api.Data;
using RevenueRecognitionSystem.Api.Entities;

namespace RevenueRecognitionSystem.Tests;

internal static class TestHelpers
{
    public static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    public static async Task SeedAsync(AppDbContext db)
    {
        db.SoftwareCategories.Add(new SoftwareCategory { SoftwareCategoryId = 1, Name = "Finance" });
        db.Softwares.Add(new Software
        {
            SoftwareId = 1, Name = "AccPro", Description = "Accounting suite",
            CurrentVersion = "2026.1", YearlyLicensePrice = 5000m, SoftwareCategoryId = 1
        });
        db.IndividualCustomers.Add(new IndividualCustomer
        {
            CustomerId = 1, FirstName = "John", LastName = "Doe", Pesel = "92010112345",
            Address = "addr", Email = "john@example.com", PhoneNumber = "+48111222333"
        });
        await db.SaveChangesAsync();
    }
}
