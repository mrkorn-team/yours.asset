using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class LogoutModel : PageModel
{
  public async Task<IActionResult> OnGetAsync()
  {
    await HttpContext.SimpleCookieSignOutAsync();
    return RedirectToPage("/auth/login");
  }
}
