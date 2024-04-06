using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using System;
using System.ComponentModel;
using System.Data;
using System.Threading.Tasks;

namespace Apkaweb.Pages
{
    public class UserModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public UserModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public int UserId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int FailedLoginAttempts { get; set; }
        public int IsBlockEnabled { get; set; }
        public int NumberOfAttempts { get; set; }
        public DateTime FailedAttemptDate { get; set; }
        public DateTime SuccesAttemptDate { get; set; }
        public int PreviousAttempts { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            string loggedInUsername = HttpContext.User.Identity.Name;

            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT id, Username, Password, FailedLoginAttempts, IsBlockEnabled, NumberOfAttempts, FailedAttemptDate, SuccesAttemptDate,PreviousAttempts FROM Users WHERE Username = @Username";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", loggedInUsername);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            UserId = reader.GetInt32("id");
                            Username = reader.GetString("Username");
                            Password = reader.GetString("Password");
                            FailedLoginAttempts = reader.GetInt32("FailedLoginAttempts");
                            IsBlockEnabled = reader.GetInt32("IsBlockEnabled");
                            NumberOfAttempts = reader.GetInt32("NumberOfAttempts");
                            FailedAttemptDate = reader.GetDateTime("FailedAttemptDate");
                            SuccesAttemptDate = reader.GetDateTime("SuccesAttemptDate");
                            PreviousAttempts = reader.GetInt32("PreviousAttempts");
                        }
                        else
                        {
                            return RedirectToPage("/Error");
                        }
                    }
                }
            }
            return Page();
        }

        public async Task<IActionResult> OnPostSetNumberOfAttemptsAsync(int userId, int numberOfAttempts)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = "UPDATE Users SET NumberOfAttempts = @NumberOfAttempts WHERE id = @UserId";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@NumberOfAttempts", numberOfAttempts);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                return RedirectToPage("/User");
            }
            catch (Exception ex)
            {
                return RedirectToPage("/Error");
            }
        }

        public async Task<IActionResult> OnPostToggleBlockAsync(int userId)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = "UPDATE Users SET IsBlockEnabled = CASE WHEN IsBlockEnabled = 0 THEN 1 ELSE 0 END, NumberOfAttempts = CASE WHEN IsBlockEnabled = 0 THEN 0 ELSE 3 END WHERE id = @UserId";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@IsBlockEnabled", IsBlockEnabled);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                return RedirectToPage("/User");
            }
            catch (Exception ex)
            {
                return RedirectToPage("/Error");
            }
        }
    }
}
