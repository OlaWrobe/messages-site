using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Threading.Tasks;
using static Mysqlx.Expect.Open.Types;

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

        public async Task<IActionResult> OnGetAsync()
        {
            // Retrieve the logged-in user's information from the database
            string loggedInUsername = HttpContext.User.Identity.Name;

            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Assuming you have a Users table with columns: id, Username, Password, FailedLoginAttempts, IsBlockEnabled, NumberOfAttempts
                string query = "SELECT id, Username, Password, FailedLoginAttempts, IsBlockEnabled, NumberOfAttempts FROM Users WHERE Username = @Username";
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
                        }
                        else
                        {
                            // Handle the case where the user is not found
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
                // Update the NumberOfAttempts property in the database
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = "UPDATE Users SET NumberOfAttempts = @NumberOfAttempts WHERE id = @UserId";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId); // Use the provided user ID
                        command.Parameters.AddWithValue("@NumberOfAttempts", numberOfAttempts);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Redirect back to the same page after setting the number of attempts
                return RedirectToPage("/User");
            }
            catch (Exception ex)
            {
                // Log the exception or handle it appropriately
                // For simplicity, let's return an error page
                return RedirectToPage("/Error");
            }
        }
        public async Task<IActionResult> OnPostToggleBlockAsync(int userId)

        {
            try
            {
                

                // Update the IsBlockEnabled property in the database
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = "UPDATE Users SET IsBlockEnabled = CASE WHEN IsBlockEnabled = 0 THEN 1 ELSE 0 END, NumberOfAttempts = CASE WHEN IsBlockEnabled = 0 THEN 0 ELSE 3 END WHERE id = @UserId";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId); // Use the provided user ID
                        command.Parameters.AddWithValue("@IsBlockEnabled", IsBlockEnabled);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Redirect back to the same page after processing the form submission
                return RedirectToPage("/User");
            }
            catch (Exception ex)
            {
                // Log the exception or handle it appropriately
                // For simplicity, let's return an error page
                return RedirectToPage("/Error");
            }
        
        }

    }

}

