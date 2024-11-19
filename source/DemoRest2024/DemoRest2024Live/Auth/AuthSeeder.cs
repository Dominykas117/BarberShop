using DemoRest2024Live.Auth.Model;
using Microsoft.AspNetCore.Identity;

namespace DemoRest2024Live.Auth
{
    public class AuthSeeder
    {
        private readonly UserManager<BarberShopClient> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AuthSeeder(UserManager<BarberShopClient> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedAsync()
        {
            await AddDefaultRolesAsync();
            await AddAdminUserAsync();
        }

        private async Task AddAdminUserAsync()
        {
            var newAdminUser = new BarberShopClient()
            {
                UserName = "admin",
                Email = "admin@admin.com"
            };

            var existingAdminUser = await _userManager.FindByNameAsync(newAdminUser.UserName);
            if (existingAdminUser == null)
            {
                var createAdminUserResult = await _userManager.CreateAsync(newAdminUser, "VerySafePassword1!");
                // !!!!!!!!!!!!Important!!!!!!!!!42:30 prisideti 5 environmental variables kad nebutu hardcoded!!!!!!!!!!! 
                if (createAdminUserResult.Succeeded)
                {
                    await _userManager.AddToRolesAsync(newAdminUser, BarberShopRoles.All);
                }
            }
        }

        private async Task AddDefaultRolesAsync()
        {
            foreach (var role in BarberShopRoles.All)
            {
                var roleExists = await _roleManager.RoleExistsAsync(role);
                if (!roleExists)
                    await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}
