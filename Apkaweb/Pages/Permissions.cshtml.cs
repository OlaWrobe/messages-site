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
            await ManagePermissionAsync(SelectedUserWithoutPermission, true);
            return RedirectToPage("./Permissions");
        }

        public async Task<IActionResult> OnPostRevokePermissionAsync()
        {
            await ManagePermissionAsync(SelectedUserWithPermission, false);
            return RedirectToPage("./Permissions");
        }

        private async Task PopulateUserOptionsAsync()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            string loggedInUsername = User.Identity.Name;

            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            UsersWithoutPermission = await GetUserOptionsAsync(connection, loggedInUsername, false);
            UsersWithPermission = await GetUserOptionsAsync(connection, loggedInUsername, true);
        }

        private async Task<List<SelectListItem>> GetUserOptionsAsync(MySqlConnection connection, string loggedInUsername, bool withPermission)
        {
            var userOptions = new List<SelectListItem>();

            string selectUsersQuery = @"
                    SELECT Username
                    FROM Users
                    WHERE Username != @LoggedInUsername";

            if (withPermission)
            {
                selectUsersQuery += @"
                        AND EXISTS (
                            SELECT 1
                            FROM Permissions
                            WHERE OwnerUsername = @LoggedInUsername
                                AND TargetUsername = Users.Username
                        )";
            }
            else
            {
                selectUsersQuery += @"
                        AND NOT EXISTS (
                            SELECT 1
                            FROM Permissions
                            WHERE OwnerUsername = @LoggedInUsername
                                AND TargetUsername = Users.Username
                        )";
            }

            await using var command = new MySqlCommand(selectUsersQuery, connection);
            command.Parameters.AddWithValue("@LoggedInUsername", loggedInUsername);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                userOptions.Add(new SelectListItem
                {
                    Value = reader["Username"].ToString(),
                    Text = reader["Username"].ToString()
                });
            }

            return userOptions;
        }

        private async Task ManagePermissionAsync(string selectedUser, bool grant)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            string loggedInUsername = User.Identity.Name;

            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = grant ?
                "INSERT INTO Permissions (OwnerUsername, TargetUsername) VALUES (@OwnerUsername, @TargetUsername)" :
                "DELETE FROM Permissions WHERE OwnerUsername = @OwnerUsername AND TargetUsername = @TargetUsername";

            await using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@OwnerUsername", loggedInUsername);
            command.Parameters.AddWithValue("@TargetUsername", selectedUser);

            await command.ExecuteNonQueryAsync();
        }
    }
}
