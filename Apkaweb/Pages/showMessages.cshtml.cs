using Apkaweb.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Apkaweb.Pages
{
    public class ShowMessagesModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public ShowMessagesModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [BindProperty]
        [Required(ErrorMessage = "Message content is required.")]
        [StringLength(255, ErrorMessage = "Message cannot be longer than 255 characters.")]
        public string NewMessage { get; set; }
        public List<Message> Messages { get; set; } = new List<Message>();

        public async Task OnGetAsync()
        {
            await LoadMessagesAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int messageId)
        {
            // Add your logic to delete message here
            // You may also check permissions before deleting the message
            // Example:
            // if (!CanDeleteMessage(userId))
            // {
            //     return Forbid(); // Return forbidden status if user doesn't have permission
            // }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string deleteQuery = "DELETE FROM Messages WHERE Id = @MessageId";
                using (var command = new MySqlCommand(deleteQuery, connection))
                {
                    command.Parameters.AddWithValue("@MessageId", messageId);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    // Refresh messages after deletion
                    await LoadMessagesAsync();

                    return RedirectToPage("/ShowMessages");
                }
            }
        }

        public bool CanDeleteMessage(string userId, int messageId)
{
    string loggedInUserId = User.Identity.Name;

    if (string.IsNullOrEmpty(loggedInUserId))
    {
        return false; // No logged-in user, cannot delete
    }

    // Check if the logged-in user is the owner of the message
    if (loggedInUserId == userId)
    {
        return true; // The logged-in user can delete their own messages
    }

    // Check if the logged-in user has permission to delete the message
    string connectionString = _configuration.GetConnectionString("DefaultConnection");

    using (var connection = new MySqlConnection(connectionString))
    {
        connection.Open();

        string query = @"
            SELECT COUNT(*)
            FROM Permissions
            WHERE OwnerUsername = @Owner
                AND TargetUsername = @Target";
        using (var command = new MySqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Owner", loggedInUserId);
            command.Parameters.AddWithValue("@Target", userId);

            int count = Convert.ToInt32(command.ExecuteScalar());

            return count > 0;
        }
    }
}

        private async Task LoadMessagesAsync()
        {
            Messages.Clear();

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT * FROM Messages"; // Adjust the query as per your table schema
                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var message = new Message
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Content = reader.GetString(reader.GetOrdinal("Content")),
                                UserId = reader.GetString(reader.GetOrdinal("UserId"))
                            };
                            Messages.Add(message);
                        }
                    }
                }
            }
        }
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadMessagesAsync();
                return Page();
            }

            // Get the actual user ID of the logged-in user
            string userId = User.Identity.Name;

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string insertQuery = "INSERT INTO Messages (Content, UserId) VALUES (@Content, @UserId)";
                using (var command = new MySqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@Content", NewMessage);
                    command.Parameters.AddWithValue("@UserId", userId);

                    await command.ExecuteNonQueryAsync();

                    // Refresh messages after insertion
                    await LoadMessagesAsync();

                    return RedirectToPage("/ShowMessages");
                }
            }
        }
        public bool CanEditMessage(string userId)
        {
            string loggedInUserId = User.Identity.Name;

            if (string.IsNullOrEmpty(loggedInUserId))
            {
                return false; // No logged-in user, cannot edit
            }

            return loggedInUserId == userId;
        }


        private async Task<Message> GetMessageByIdAsync(int messageId)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT * FROM Messages WHERE Id = @MessageId";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@MessageId", messageId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var message = new Message
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Content = reader.GetString(reader.GetOrdinal("Content")),
                                UserId = reader.GetString(reader.GetOrdinal("UserId"))
                            };
                            return message;
                        }
                        else
                        {
                            return null; // Message not found
                        }
                    }
                }
            }
        }
        public async Task<IActionResult> OnPostEditAsync(int messageId, string newContent)
        {
            var message = await GetMessageByIdAsync(messageId);

            if (message == null)
            {
                return NotFound(); // Message not found
            }

            if (!CanEditMessage(message.UserId))
            {
                return Forbid(); // User does not have permission to edit this message
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string updateQuery = "UPDATE Messages SET Content = @NewContent WHERE Id = @MessageId";
                using (var command = new MySqlCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@NewContent", newContent);
                    command.Parameters.AddWithValue("@MessageId", messageId);

                    await command.ExecuteNonQueryAsync();

                    // Refresh messages after updating
                    await LoadMessagesAsync();

                    return RedirectToPage("/ShowMessages");
                }
            }
        }
    }
}
