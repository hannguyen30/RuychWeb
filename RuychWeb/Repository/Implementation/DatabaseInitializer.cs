using Microsoft.AspNetCore.Identity;
using RuychWeb.Models.Domain;
using RuychWeb.Repository.Abstract;

namespace RuychWeb.Repository.Implementation
{
    public class DatabaseInitializer : IDatabaseInitializer
    {
        private readonly UserManager<Account> userManager;
        private readonly RoleManager<Role> roleManager;
        private readonly DataContext _dataContext;

        public DatabaseInitializer(UserManager<Account> userManager, RoleManager<Role> roleManager, DataContext dataContext)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this._dataContext = dataContext;
        }

        public async Task SeedAdminAccountAsync()
        {
            var adminEmail = "admin@gmail.com";
            var adminPassword = "Admin@123";
            var adminRoleName = "Admin";

            // Ensure the role exists
            if (!await roleManager.RoleExistsAsync(adminRoleName))
            {
                var adminRole = new Role { Name = adminRoleName, NormalizedName = adminRoleName.ToUpper() };
                await roleManager.CreateAsync(adminRole);
            }

            // Ensure the admin account exists
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminAccount = new Account
                {
                    UserName = adminRoleName,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminAccount, adminPassword);
                if (result.Succeeded)
                {
                    // Assign the admin role to the admin account
                    await userManager.AddToRoleAsync(adminAccount, adminRoleName);

                    // Create Employee object for the Admin
                    var employee = new Employee
                    {
                        Name = "Admin", // Name of the Admin
                        Phone = "",
                        Email = adminEmail,
                        Birthday = null,
                        AccountId = adminAccount.Id // Link the Employee with the Admin account
                    };

                    // Add the Employee to the database
                    try
                    {
                        _dataContext.Employees.Add(employee);
                        await _dataContext.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        // Log the exception (or use your logging framework)
                        Console.WriteLine($"Error saving Employee: {ex.Message}");
                    }
                }
            }
        }

    }
}
