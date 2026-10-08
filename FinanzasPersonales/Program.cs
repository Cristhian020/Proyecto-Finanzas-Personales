using System.Globalization;
using System.Security.Claims;
using FinanzasPersonales.Data;
using FinanzasPersonales.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Todas las páginas requieren sesión iniciada salvo las marcadas con [AllowAnonymous].
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())));

builder.Services.AddDbContext<FinanzasContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("FinanzasDb")));
builder.Services.AddScoped<CorteService>();
builder.Services.AddScoped<CuentaService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Cuenta/Login";
        options.AccessDeniedPath = "/Cuenta/AccesoDenegado";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Events.OnValidatePrincipal = async ctx =>
        {
            // Cierra la sesión si la cuenta fue inactivada o le cambiaron el rol.
            var db = ctx.HttpContext.RequestServices.GetRequiredService<FinanzasContext>();
            var id = int.TryParse(ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var n) ? n : 0;
            var usuario = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (usuario is null || !usuario.Estado || !ctx.Principal!.IsInRole(usuario.Rol.ToString()))
            {
                ctx.RejectPrincipal();
                await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

var app = builder.Build();

// Aplica las migraciones pendientes al iniciar (crea la base de datos si no existe)
// y asigna las contraseñas iniciales de las cuentas de ejemplo.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<FinanzasContext>().Database.Migrate();
    await scope.ServiceProvider.GetRequiredService<CuentaService>().InicializarCuentasAsync();
}

// Cultura dominicana: formato de fechas dd/MM/yyyy y moneda RD$.
var cultura = (CultureInfo)new CultureInfo("es-DO").Clone();
cultura.NumberFormat.CurrencySymbol = "RD$";
cultura.NumberFormat.CurrencyNegativePattern = 1; // -RD$n
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cultura),
    SupportedCultures = [cultura],
    SupportedUICultures = [cultura]
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

// Permite que las pruebas de integración (WebApplicationFactory) arranquen la aplicación.
public partial class Program { }
