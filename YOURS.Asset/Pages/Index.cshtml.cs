using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YOURS.Asset.Pages;
[Authorize]
public class IndexModel : PageModel
{
  private readonly ILogger<IndexModel> _logger;

  public IndexModel(ILogger<IndexModel> logger)
  {
    _logger = logger;
  }

  public IActionResult OnGet()
  {
    return RedirectToPage("/sharedfiles/index");
  }
}
