using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class LoginModel : PageModel
{
  [BindProperty] public string? Email { get; set; }
  [BindProperty] public string? Password { get; set; }

  public async Task<IActionResult> OnPostAsync()
  {
    // Example credentials
    if (Email == "test@example.com" && Password == "1122")
    {
      await HttpContext.SimpleCookieSignInAsync(Email);

      // Return JSON
      return new JsonResult(new
      {
        success = true,
        message = "Login successful..",
        redirectUrl = "/" // redirect after login
      });
    }

    return new JsonResult(new
    {
      success = false,
      message = "Invalid email or password"
    });
  }
}