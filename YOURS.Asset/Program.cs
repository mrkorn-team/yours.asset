using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

// -----------------------------------------------
// My razor template w/ sidebars 'n simple sign-in
// -----------------------------------------------
// ChatGPTInstructions:
// - RazorPages9 + Bootstrap5/Icons1.13 dark/light, fetch JS standalone class, C# nullable, separated global script/style files, bottom @section Scripts/Styles -- idle (no output)
// -----------------------------------------------

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddHttpContextAccessor();
builder.Services.RegisterAntiforgery(headerName: "antiforgery-token", formFieldName: "antiforgery-token");
builder.Services.RegisterCookieAuthentication(cookieExpireTimeSpan: 3600 * 190);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Error");
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();
}
else
{
  app.UseDeveloperExceptionPage();
  //app.UseCssLiveReload(); // nuget package - using Toolbelt.Extensions.DependencyInjection;
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages()
  .WithStaticAssets();

app.Run();

internal static class Startup
{
  internal static IServiceCollection RegisterAntiforgery(this IServiceCollection services, string? headerName = "antiforgery-token", string? formFieldName = "antiforgery-token")
  {
    services.AddAntiforgery(o =>
    {
      o.HeaderName = headerName ?? "X-CSRF-TOKEN";
      o.FormFieldName = formFieldName ?? "__RequestVerificationToken";
    });
    return services;

    // AntiForgery instruction
    /* =======================================
    // .js
    const fd = new FormData(form);
    const resp = await fetch(url, {
      method: 'post', data: fd,
      headers: {
        'content-type': 'text/html',                                        // <- ex: <h1>hello</h1>
        'content-type': 'application/json',                                 // <- ex: {"name": "anyName"}
        'content-type': 'application/x-www-form-urlencoded; charset=UTF-8', // <- ex: key=value&key=value
        'content-type': 'multipart/form-data; doundary=something...',       // <- ex: var fd = new FormData($form1.get(0)) <- You don’t manually set the Content-Type header with boundary in ajax/fetch. The browser does it automatically. If you try to set it manually, the boundary may not match, and the server can fail to parse.
        'antiforgery-token': document.querySelector('[name="antiforgery-token"]').value
      }
    });
    ==========================================
    // .cshtml
    @page
    @model IndexModel

    // PageModel require to work with formdata(), razor + ajax/fetch
    @Html.AntiForgeryToken()
    ======================================= */
  }

  internal static AuthenticationBuilder RegisterCookieAuthentication(this IServiceCollection services, int cookieExpireTimeSpan = 3600 * 190)
  {
    var authBuilder = services.AddAuthentication(options =>
    {
      options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    }).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
      // Lifetime & renewal
      options.ExpireTimeSpan = TimeSpan.FromSeconds(cookieExpireTimeSpan);
      options.SlidingExpiration = true;

      // Cookie settings (secure defaults)
      options.Cookie.Name = ".ap.user";
      options.Cookie.HttpOnly = true; // prevent JS access
      options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS only
      //options.Cookie.SameSite = SameSiteMode.Strict; // CSRF protection (Lax if you need cross-site login flows)
      //options.Cookie.MaxAge = options.ExpireTimeSpan; // optional, normally redundant

      // Paths
      options.LoginPath = "/auth/login";
      options.LogoutPath = "/auth/logout";
      options.AccessDeniedPath = "/auth/denied";

      // Handle AJAX/fetch redirects
      options.Events.OnRedirectToLogin = context =>
      {
        if (context.Request.IsAjaxRequest())
        {
          context.Response.StatusCode = StatusCodes.Status401Unauthorized;
          return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
      };
      // Handle AJAX/fetch redirects
      options.Events.OnRedirectToAccessDenied = context =>
      {
        if (context.Request.IsAjaxRequest())
        {
          context.Response.StatusCode = StatusCodes.Status403Forbidden;
          return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
      };
    });

    return authBuilder;
  }

  internal static bool IsAjaxRequest(this HttpRequest request)
  {
    if (request == null)
      throw new ArgumentNullException(nameof(request));
    return request.Headers["X-Requested-With"] == "XMLHttpRequest";
  }

  internal static Task SimpleCookieSignInAsync(this HttpContext context, string userEmail)
  {
    if (!context.User.Identity?.IsAuthenticated ?? false)
    {
      var claims = new Claim[] {
        new Claim(ClaimTypes.Email, userEmail),
        new Claim(ClaimTypes.NameIdentifier, "9b15895d-8241-47c6-8449-2aa3d8b77319"),
        new Claim(ClaimTypes.UserData + ":dispname", "korney lat'bang"),
        new Claim(ClaimTypes.UserData + ":picture", "/$dev/admin/dist/assets/img/k2.jpg"),
      };
      var authschm = CookieAuthenticationDefaults.AuthenticationScheme;
      var identity = new ClaimsIdentity(claims, authschm);
      var princpal = new ClaimsPrincipal(identity);
      var persistn = new AuthenticationProperties { IsPersistent = true };
      return context.SignInAsync(authschm, princpal, persistn);
    }
    return Task.CompletedTask;
  }

  internal static Task SimpleCookieSignOutAsync(this HttpContext context)
  {
    return context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
  }
}
