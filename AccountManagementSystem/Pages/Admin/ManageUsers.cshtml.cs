using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace YourAppNamespace.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ManageUsersModel : PageModel
    {
        private readonly IConfiguration _config;

        public ManageUsersModel(IConfiguration config)
        {
            _config = config;
        }

        public List<UserWithRoles> Users { get; set; } = new();

        public class UserWithRoles
        {
            public string Id { get; set; }
            public string Email { get; set; }
            public List<string> Roles { get; set; } = new();
        }

        public async Task OnGetAsync()
        {
            var users = new List<UserWithRoles>();

            using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();

                // Get all users
                using (var cmd = new SqlCommand("SELECT Id, Email FROM AspNetUsers", conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        users.Add(new UserWithRoles
                        {
                            Id = reader.GetString(0),
                            Email = reader.GetString(1)
                        });
                    }
                }

                // Get roles for each user
                foreach (var user in users)
                {
                    using (var cmd = new SqlCommand(@"
                        SELECT r.Name
                        FROM AspNetUserRoles ur
                        JOIN AspNetRoles r ON r.Id = ur.RoleId
                        WHERE ur.UserId = @UserId", conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", user.Id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                user.Roles.Add(reader.GetString(0));
                            }
                        }
                    }
                }
            }

            Users = users;
        }

        public async Task<IActionResult> OnPostChangeRoleAsync(string userId, string roleName, bool assign)
        {
            using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("sp_AssignUserRole", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@RoleName", roleName);
                cmd.Parameters.AddWithValue("@ActionType", assign ? "Assign" : "Remove");

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }

            return RedirectToPage();
        }
    }
}
