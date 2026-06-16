using RevenueRecognitionSystem.Api.DTOs;
using RevenueRecognitionSystem.Api.Exceptions;
using RevenueRecognitionSystem.Api.Services;
using Xunit;

namespace RevenueRecognitionSystem.Tests;

public class CustomerServiceTests
{
    [Fact]
    public async Task AddIndividual_NewPesel_Persisted()
    {
        var db = TestHelpers.NewDb();
        var service = new CustomerService(db);
        var id = await service.AddIndividualAsync(new CreateIndividualCustomerDto(
            "Anna", "Kowalska", "99887766554", "addr", "anna@example.com", "+48111222333"));
        Assert.True(id > 0);
    }

    [Fact]
    public async Task AddIndividual_DuplicatePesel_ThrowsConflict()
    {
        var db = TestHelpers.NewDb();
        await TestHelpers.SeedAsync(db);
        var service = new CustomerService(db);
        var dto = new CreateIndividualCustomerDto(
            "Other", "Person", "92010112345", "addr", "x@example.com", "+48000000000");
        await Assert.ThrowsAsync<ConflictException>(() => service.AddIndividualAsync(dto));
    }

    [Fact]
    public async Task AddCompany_NewKrs_Persisted()
    {
        var db = TestHelpers.NewDb();
        var service = new CustomerService(db);
        var id = await service.AddCompanyAsync(new CreateCompanyCustomerDto(
            "ABC", "0000111222", "addr", "x@a.pl", "+48111222333"));
        Assert.True(id > 0);
    }

    [Fact]
    public async Task AddCompany_DuplicateKrs_ThrowsConflict()
    {
        var db = TestHelpers.NewDb();
        var service = new CustomerService(db);
        await service.AddCompanyAsync(new CreateCompanyCustomerDto(
            "ABC", "0000111222", "addr", "x@a.pl", "+48111222333"));
        var dto = new CreateCompanyCustomerDto(
            "Another", "0000111222", "addr", "y@a.pl", "+48222111000");
        await Assert.ThrowsAsync<ConflictException>(() => service.AddCompanyAsync(dto));
    }

    [Fact]
    public async Task UpdateIndividual_NotFound_ThrowsNotFound()
    {
        var db = TestHelpers.NewDb();
        var service = new CustomerService(db);
        var dto = new UpdateIndividualCustomerDto(
            "X", "Y", "addr", "x@x.com", "+48000000000");
        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateIndividualAsync(999, dto));
    }

    [Fact]
    public async Task UpdateIndividual_OverwritesFields()
    {
        var db = TestHelpers.NewDb();
        await TestHelpers.SeedAsync(db);
        var service = new CustomerService(db);
        var dto = new UpdateIndividualCustomerDto(
            "Anna", "Updated", "new addr", "new@x.com", "+48999000111");
        await service.UpdateIndividualAsync(1, dto);
        var c = db.IndividualCustomers.Single();
        Assert.Equal("Updated", c.LastName);
        Assert.Equal("92010112345", c.Pesel);
    }

    [Fact]
    public async Task SoftDelete_OverwritesData_PreservesPesel()
    {
        var db = TestHelpers.NewDb();
        await TestHelpers.SeedAsync(db);
        var service = new CustomerService(db);
        await service.SoftDeleteIndividualAsync(1);
        var c = db.IndividualCustomers.Single();
        Assert.True(c.IsDeleted);
        Assert.Equal("DELETED", c.FirstName);
        Assert.Equal("DELETED", c.LastName);
        Assert.Equal("DELETED", c.Address);
        Assert.Equal("92010112345", c.Pesel);
    }

    [Fact]
    public async Task SoftDelete_NotFound_ThrowsNotFound()
    {
        var db = TestHelpers.NewDb();
        var service = new CustomerService(db);
        await Assert.ThrowsAsync<NotFoundException>(() => service.SoftDeleteIndividualAsync(999));
    }

    [Fact]
    public async Task List_ReturnsBothIndividualsAndCompanies()
    {
        var db = TestHelpers.NewDb();
        await TestHelpers.SeedAsync(db);
        var service = new CustomerService(db);
        await service.AddCompanyAsync(new CreateCompanyCustomerDto(
            "Acme", "0000999888", "addr", "ac@me.pl", "+48555000111"));
        var list = (await service.ListAsync()).ToList();
        Assert.Equal(2, list.Count);
        Assert.Contains(list, c => c.Type == "Individual");
        Assert.Contains(list, c => c.Type == "Company");
    }
}
