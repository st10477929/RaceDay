using RaceDay.Web.Filters;
using RaceDay.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<SessionExpiredFilter>();
    // Non-nullable strings (e.g. read-only Email on the profile form) are not implicitly required.
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});
builder.Services.AddHttpContextAccessor();

// MVC's own server-side session: holds UserId, Role, FullName and the API's session cookie.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Web.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// Typed client for the Part 2 API. UseCookies = false because ApiClient forwards the API cookie itself.
var apiBase = (builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5080/").TrimEnd('/') + "/";
builder.Services.AddHttpClient<IApiClient, ApiClient>(client => client.BaseAddress = new Uri(apiBase))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Home/Error");

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

app.Run();
