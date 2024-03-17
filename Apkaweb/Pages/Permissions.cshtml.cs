using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Apkaweb.Pages
{
    public class PermissionsModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public PermissionsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<UserPermission> Users { get; set; } = new List<UserPermission>();

        public async Task OnGetAsync()
        {
            await LoadUserPermissionsAsync();
        }

        public async Task<IActionResult> OnPostGrantPermissionAsync(string username)
        {
            // Implement logic to grant permission to the specified user
            return RedirectToPage("/Permissions");
        }

        public async Task<IActionResult> OnPostRevokePermissionAsync(string username)
        {
            // Implement logic to revoke permission from the specified user
            return RedirectToPage("/Permissions");
        }

        private async Task LoadUserPermissionsAsync()
        {
            Users.Clear();

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT DISTINCT TargetUsername FROM Permissions"; // Fetch distinct TargetUsernames
                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var userPermission = new UserPermission
                            {
                                Username = reader.GetString(reader.GetOrdinal("TargetUsername")),
                                HasPermission = true // Implement logic to check permission here
                            };
                            Users.Add(userPermission);
                        }
                    }
                }
            }

            // Fetch users without permissions and add them to the list
            List<string> usersWithPermissions = Users.Select(u => u.Username).ToList();
            List<string> allUsers = await GetAllUsersAsync(); // Implement a method to fetch all users from the database
            foreach (var user in allUsers)
            {
                if (!usersWithPermissions.Contains(user))
                {
                    Users.Add(new UserPermission { Username = user, HasPermission = false });
                }
            }
        }

        private async Task<List<string>> GetAllUsersAsync()
        {
            List<string> users = new List<string>();

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT DISTINCT Username FROM Users"; // Adjust the query based on your table schema
                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(reader.GetString(reader.GetOrdinal("Username")));
                        }
                    }
                }
            }

            return users;
        }

        public class UserPermission
        {
            public string Username { get; set; }
            public bool HasPermission { get; set; }
        }
    }
}
