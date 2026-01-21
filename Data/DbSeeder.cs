using BookMart.Constants;
using BookMart.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookMart.Data
{
    public class DbSeeder
    {

        public static async Task SeedDefaultData(IServiceProvider service)
        {
            var userMgr = service.GetService<UserManager<IdentityUser>>();
            var roleMgr = service.GetService<RoleManager<IdentityRole>>();
            var dbContext = service.GetService<ApplicationDbContext>();

            // Seed Roles
            if (roleMgr != null)
            {
                if (!await roleMgr.RoleExistsAsync(Roles.Admin.ToString()))
                {
                    await roleMgr.CreateAsync(new IdentityRole(Roles.Admin.ToString()));
                }

                if (!await roleMgr.RoleExistsAsync(Roles.User.ToString()))
                {
                    await roleMgr.CreateAsync(new IdentityRole(Roles.User.ToString()));
                }
            }

            // Seed Admin User
            if (userMgr != null)
            {
                var admin = new IdentityUser
                {
                    UserName = "admin@gmail.com",
                    Email = "admin@gmail.com",
                    EmailConfirmed = true
                };

                var userInDb = await userMgr.FindByEmailAsync(admin.Email);
                if (userInDb is null)
                {
                    await userMgr.CreateAsync(admin, "Admin@123");
                    await userMgr.AddToRoleAsync(admin, Roles.Admin.ToString());
                }
            }

            // Seed OrderStatus
            if (dbContext != null)
            {
                var orderStatuses = new[]
                {
                    new OrderStatus { StatusId = 1, StatusName = "Pending" },
                    new OrderStatus { StatusId = 2, StatusName = "Processing" },
                    new OrderStatus { StatusId = 3, StatusName = "Shipped" },
                    new OrderStatus { StatusId = 4, StatusName = "Delivered" },
                    new OrderStatus { StatusId = 5, StatusName = "Cancelled" }
                };

                foreach (var status in orderStatuses)
                {
                    var existingStatus = await dbContext.orderStatuses
                        .FirstOrDefaultAsync(s => s.StatusName == status.StatusName);

                    if (existingStatus == null)
                    {
                        dbContext.orderStatuses.Add(status);
                    }
                }

                await dbContext.SaveChangesAsync();
            }
        }
    }
}
