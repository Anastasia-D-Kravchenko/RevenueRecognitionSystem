using Microsoft.EntityFrameworkCore;
using RevenueRecognitionSystem.Api.Data;
using RevenueRecognitionSystem.Api.DTOs;
using RevenueRecognitionSystem.Api.Entities;
using RevenueRecognitionSystem.Api.Exceptions;

namespace RevenueRecognitionSystem.Api.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _db;

    public CustomerService(AppDbContext db) => _db = db;

    public async Task<int> AddIndividualAsync(CreateIndividualCustomerDto dto)
    {
        if (await _db.IndividualCustomers.AnyAsync(c => c.Pesel == dto.Pesel))
            throw new ConflictException($"Individual with PESEL '{dto.Pesel}' already exists.");

        var customer = new IndividualCustomer
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Pesel = dto.Pesel,
            Address = dto.Address,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber
        };
        _db.IndividualCustomers.Add(customer);
        await _db.SaveChangesAsync();
        return customer.CustomerId;
    }

    public async Task<int> AddCompanyAsync(CreateCompanyCustomerDto dto)
    {
        if (await _db.CompanyCustomers.AnyAsync(c => c.Krs == dto.Krs))
            throw new ConflictException($"Company with KRS '{dto.Krs}' already exists.");

        var customer = new CompanyCustomer
        {
            CompanyName = dto.CompanyName,
            Krs = dto.Krs,
            Address = dto.Address,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber
        };
        _db.CompanyCustomers.Add(customer);
        await _db.SaveChangesAsync();
        return customer.CustomerId;
    }

    public async Task UpdateIndividualAsync(int id, UpdateIndividualCustomerDto dto)
    {
        var customer = await _db.IndividualCustomers.FirstOrDefaultAsync(c => c.CustomerId == id);
        if (customer is null)
            throw new NotFoundException($"Individual customer with id={id} was not found.");
        if (customer.IsDeleted)
            throw new ConflictException("Cannot update a deleted customer.");

        customer.FirstName = dto.FirstName;
        customer.LastName = dto.LastName;
        customer.Address = dto.Address;
        customer.Email = dto.Email;
        customer.PhoneNumber = dto.PhoneNumber;
        await _db.SaveChangesAsync();
    }

    public async Task UpdateCompanyAsync(int id, UpdateCompanyCustomerDto dto)
    {
        var customer = await _db.CompanyCustomers.FirstOrDefaultAsync(c => c.CustomerId == id);
        if (customer is null)
            throw new NotFoundException($"Company customer with id={id} was not found.");

        customer.CompanyName = dto.CompanyName;
        customer.Address = dto.Address;
        customer.Email = dto.Email;
        customer.PhoneNumber = dto.PhoneNumber;
        await _db.SaveChangesAsync();
    }

    public async Task SoftDeleteIndividualAsync(int id)
    {
        var customer = await _db.IndividualCustomers.FirstOrDefaultAsync(c => c.CustomerId == id);
        if (customer is null)
            throw new NotFoundException($"Individual customer with id={id} was not found.");

        customer.FirstName = "DELETED";
        customer.LastName = "DELETED";
        customer.Address = "DELETED";
        customer.Email = "deleted@example.invalid";
        customer.PhoneNumber = "000000000";
        customer.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<CustomerListItemDto>> ListAsync()
    {
        return await _db.Customers
            .Select(c => new CustomerListItemDto(
                c.CustomerId,
                c is CompanyCustomer ? "Company" : "Individual",
                c is CompanyCustomer
                    ? ((CompanyCustomer)c).CompanyName
                    : ((IndividualCustomer)c).FirstName + " " + ((IndividualCustomer)c).LastName,
                c.Email,
                c.PhoneNumber,
                c is IndividualCustomer && ((IndividualCustomer)c).IsDeleted))
            .ToListAsync();
    }
}
