using MySql.Data.MySqlClient;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Apkaweb.Pages
{
    public class PermissionsModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public PermissionsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public string SelectedUserWithoutPermission { get; set; }

        [BindProperty]
        public string SelectedUserWithPermission { get; set; }

        public List<SelectListItem> UsersWithoutPermission { get; set; }
        public List<SelectListItem> UsersWithPermission { get; set; }

        public async Task OnGetAsync()
        {
            await PopulateUserOptionsAsync();
        }

        public async Task<IActionResult> OnPostGrantPermissionAsync()
        {
            await GrantPermissionAsync(SelectedUserWithoutPermission);
            return RedirectToPage("./Permissions");
        }

        public async Task<IActionResult> OnPostRevokePermissionAsync()
        {
            await RevokePermissionAsync(SelectedUserWithPermission);
            return RedirectToPage("./Permissions");
        }

        private async Task PopulateUserOptionsAsync()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string loggedInUsername = User.Identity.Name;

                // Query users without permission
                string selectUsersWithoutPermissionQuery = @"
                    SELECT Username
                    FROM Users
                    WHERE Username != @LoggedInUsername
                        AND NOT EXISTS (
                            SELECT 1
                            FROM Permissions
                            WHERE OwnerUsername = @LoggedInUsername
                                AND TargetUsername = Users.Username
                        )";

                // Query users with permission
                string selectUsersWithPermissionQuery = @"
                    SELECT Username
                    FROM Users
                    WHERE Username != @LoggedInUsername
                        AND EXISTS (
                            SELECT 1
                            FROM Permissions
                            WHERE OwnerUsername = @LoggedInUsername
                                AND TargetUsername = Users.Username
                        )";

                UsersWithoutPermission = await GetUserOptionsAsync(selectUsersWithoutPermissionQuery, connection, loggedInUsername);
                UsersWithPermission = await GetUserOptionsAsync(selectUsersWithPermissionQuery, connection, loggedInUsername);
            }
        }

        private async Task<List<SelectListItem>> GetUserOptionsAsync(string query, MySqlConnection connection, string loggedInUsername)
        {
            var userOptions = new List<SelectListItem>();

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@LoggedInUsername", loggedInUsername);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        userOptions.Add(new SelectListItem
                        {
                            Value = reader["Username"].ToString(),
                            Text = reader["Username"].ToString()
                        });
                    }
                }
            }

            return userOptions;
        }

        private async Task GrantPermissionAsync(string selectedUser)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string loggedInUsername = User.Identity.Name;

                string insertQuery = @"
                    INSERT INTO Permissions (OwnerUsername, TargetUsername)
                    VALUES (@OwnerUsername, @TargetUsername)";

                using (var command = new MySqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@OwnerUsername", loggedInUsername);
                    command.Parameters.AddWithValue("@TargetUsername", selectedUser);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task RevokePermissionAsync(string selectedUser)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string loggedInUsername = User.Identity.Name;

                string deleteQuery = @"
                    DELETE FROM Permissions
                    WHERE OwnerUsername = @OwnerUsername
                        AND TargetUsername = @TargetUsername";

                using (var command = new MySqlCommand(deleteQuery, connection))
                {
                    command.Parameters.AddWithValue("@OwnerUsername", loggedInUsername);
                    command.Parameters.AddWithValue("@TargetUsername", selectedUser);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}