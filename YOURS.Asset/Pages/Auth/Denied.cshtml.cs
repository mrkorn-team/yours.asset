using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class DeniedModel : PageModel
{
  [BindProperty(SupportsGet = true)]
  public string Title { get; set; } = "Page Denied";

  [BindProperty(SupportsGet = true)]
  public string Message { get; set; } = "You have not access this page.";

  public void OnGet()
  {
    // Optional: You can sanitize or log the message here
    if (!string.IsNullOrWhiteSpace(Message))
    {
      Message = Message.Replace("\r", "").Replace("\n", "");
      Title = Title.Replace("\r", "").Replace("\n", "");
    }
  }
}
