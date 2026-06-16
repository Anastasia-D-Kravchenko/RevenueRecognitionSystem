using Microsoft.EntityFrameworkCore;
using RevenueRecognitionSystem.Api.Entities;

namespace RevenueRecognitionSystem.Api.Data;

public static class DbInitializer
{
    public static async Task EnsureSeededAsync(AppDbContext db)
    {
        if (await db.Employees.AnyAsync())
            return;

        db.Employees.Add(new Employee
        {
            Login = "admin",
            Role = "Admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!", 12)
        });
        db.Employees.Add(new Employee
        {
            Login = "user",
            Role = "User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!", 12)
        });
        await db.SaveChangesAsync();
    }
}
