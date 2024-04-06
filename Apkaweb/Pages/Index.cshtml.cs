using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Data;
using System.Security.Claims;

namespace Apkaweb.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Options");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string login, string password)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                bool isBlocked = await IsAccountBlocked(connection, login);
                DateTime lockoutEndDate = await GetLockoutEndDate(connection, login);

                if (isBlocked)
                {
                    return RedirectToPage("/LockedAccount", new { username = login });
                }
                else if (lockoutEndDate > DateTime.Now)
                {
                    TimeSpan remainingTime = lockoutEndDate - DateTime.Now;
                    ViewData["LockoutEndDate"] = $"{remainingTime.Days} days, {remainingTime.Hours} hours, {remainingTime.Minutes} minutes, {remainingTime.Seconds} seconds";
                    string remainingTimeString = $"Your account is temporarily locked. Please try again after: {remainingTime}";
                    
                    return Page(); 
                }

                string query = "SELECT COUNT(*) FROM Users WHERE Username = @Username AND Password = @Password";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", login);
                    command.Parameters.AddWithValue("@Password", password);
                    int count = Convert.ToInt32(await command.ExecuteScalarAsync());

                    if (count > 0)
                    {
                        await ResetFailedLoginAttempts(connection, login);

                        DateTime succesAttemptDate = DateTime.Now;

                        string updateQuery = "UPDATE Users SET SuccesAttemptDate = @SuccesAttemptDate WHERE Username = @Username";
                        using (var updateCommand = new MySqlCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@SuccesAttemptDate", succesAttemptDate);
                            updateCommand.Parameters.AddWithValue("@Username", login);
                            await updateCommand.ExecuteNonQueryAsync();
                        }

                        var claims = new[]
                        {
                            new Claim(ClaimTypes.Name, login),
                            new Claim(ClaimTypes.Role, "User")
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                        return RedirectToPage("/Options");
                    }
                    else
                    {
                        await IncrementFailedLoginAttempts(connection, login);

                        DateTime failedAttemptDate = DateTime.Now;

                        // pobranie aktualnej liczby nieudanych prób logowania dla użytkownika
                        int failedAttempts = await GetFailedLoginAttempts(connection, login);

                        // obliczenie czasu blokady na podstawie ilości nieudanych prób
                        TimeSpan lockoutTime = TimeSpan.FromSeconds(30 * (failedAttempts + 1));

                        // dodanie obliczonego czasu blokady do bieżącej daty i godziny
                        DateTime lockoutEndDateNew = DateTime.Now.Add(lockoutTime);

                        // aktualizacja daty ostatniej nieudanej próby logowania oraz czasu blokady w bazie danych
                        string updateQuery = "UPDATE Users SET FailedAttemptDate = @FailedAttemptDate, LockoutEndDate = @LockoutEndDate WHERE Username = @Username";
                        using (var updateCommand = new MySqlCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@FailedAttemptDate", failedAttemptDate);
                            updateCommand.Parameters.AddWithValue("@LockoutEndDate", lockoutEndDateNew);
                            updateCommand.Parameters.AddWithValue("@Username", login);
                            await updateCommand.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
            return Page();
        }

        private async Task ResetFailedLoginAttempts(MySqlConnection connection, string username)
        {
            string query = "UPDATE Users SET PreviousAttempts = FailedLoginAttempts, FailedLoginAttempts = 0 WHERE Username = @Username";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Username", username);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task<bool> IsAccountBlocked(MySqlConnection connection, string username)
        {
            string query = "SELECT IsBlockEnabled, FailedLoginAttempts, NumberOfAttempts, LockoutEndDate FROM Users WHERE Username = @Username";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Username", username);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (reader.Read())
                    {
                        int isBlockEnabled = reader.GetInt32("IsBlockEnabled");
                        int failedLoginAttempts = reader.GetInt32("FailedLoginAttempts");
                        int numberOfAttempts = reader.GetInt32("NumberOfAttempts");
                        DateTime lockoutEndDate = reader.GetDateTime("LockoutEndDate");

                        // sprawdzanie czy konto jest zablokowane na podstawie ustawień blokady oraz liczby nieudanych prób oraz czy data blokady jeszcze nie minęła
                        return isBlockEnabled == 1 && (numberOfAttempts > 0 && failedLoginAttempts >= numberOfAttempts) && lockoutEndDate > DateTime.Now;
                    }
                    else
                    {
                        // obsługa przypadku gdy użytkownik nie został znaleziony
                        return false;
                    }
                }
            }
        }

        private async Task IncrementFailedLoginAttempts(MySqlConnection connection, string username)
        {
            string query = "UPDATE Users SET FailedLoginAttempts = FailedLoginAttempts + 1 WHERE Username = @Username";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Username", username);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task<int> GetFailedLoginAttempts(MySqlConnection connection, string username)
        {
            string query = "SELECT FailedLoginAttempts FROM Users WHERE Username = @Username";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Username", username);
                object result = await command.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result);
                }
                else
                {
                    return 0;
                }
            }
        }

        private async Task<DateTime> GetLockoutEndDate(MySqlConnection connection, string username)
        {
            string query = "SELECT LockoutEndDate FROM Users WHERE Username = @Username";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Username", username);
                object result = await command.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToDateTime(result);
                }
                else
                {
                    // domyślnie zwracamy bieżącą datę, jeśli LockoutEndDate nie została ustawiona
                    return DateTime.Now;
                }
            }
        }
    }
}
