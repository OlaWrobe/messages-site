
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using MySql.Data.MySqlClient;
    using System;
    using System.Security.Claims;
    using System.Threading.Tasks;

    namespace Apkaweb.Pages
    {
        public class IndexModel : PageModel
        {
            private readonly IConfiguration _configuration;

            public IndexModel(IConfiguration configuration)
            {
                _configuration = configuration;
            }

            public async Task<IActionResult> OnPostAsync(string login, string password)
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT COUNT(*) FROM Users WHERE Username = @Username AND Password = @Password";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", login);
                        command.Parameters.AddWithValue("@Password", password);

                        int count = Convert.ToInt32(await command.ExecuteScalarAsync());

                        if (count > 0)
                        {
                            // Successful login, create claims
                            var claims = new[]
                            {
                                new Claim(ClaimTypes.Name, login),
                                new Claim(ClaimTypes.Role, "User")
                            };

                            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                            // Sign in the user
                            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                            // Redirect to option page
                            return RedirectToPage("/Options");
                        }
                    }
                }

                // Failed login, stay on the login page
                return Page();
            }
        }
    }