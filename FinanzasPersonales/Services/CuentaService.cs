using System.Security.Claims;
using FinanzasPersonales.Data;
using FinanzasPersonales.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinanzasPersonales.Services;

public static class Roles
{
    public const string Administrador = nameof(Rol.Administrador);
    public const string Usuario = nameof(Rol.Usuario);
}

/// <summary>Autenticación con cookies y contraseñas con hash (PBKDF2 de ASP.NET Core Identity).</summary>
public class CuentaService(FinanzasContext db)
{
    private readonly PasswordHasher<Usuario> hasher = new();

    public void EstablecerPassword(Usuario usuario, string password) =>
        usuario.PasswordHash = hasher.HashPassword(usuario, password);

    public bool VerificarPassword(Usuario usuario, string password)
    {
        if (string.IsNullOrEmpty(usuario.PasswordHash)) return false;
        var resultado = hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, password);
        if (resultado == PasswordVerificationResult.SuccessRehashNeeded) EstablecerPassword(usuario, password);
        return resultado != PasswordVerificationResult.Failed;
    }

    public async Task<Usuario?> ValidarCredencialesAsync(string email, string password)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == email.Trim().ToLower());
        if (usuario is null || !usuario.Estado || !VerificarPassword(usuario, password)) return null;
        await db.SaveChangesAsync(); // por si se re-calculó el hash
        return usuario;
    }

    public static async Task IniciarSesionAsync(HttpContext http, Usuario usuario, bool recordar)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol.ToString())
        };
        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidad),
            new AuthenticationProperties { IsPersistent = recordar });
    }

    /// <summary>Asigna contraseñas iniciales a las cuentas sembradas que aún no tienen.</summary>
    public async Task InicializarCuentasAsync()
    {
        var sinPassword = await db.Usuarios.Where(u => u.PasswordHash == null || u.PasswordHash == "").ToListAsync();
        foreach (var u in sinPassword)
            EstablecerPassword(u, u.Rol == Rol.Administrador ? "Admin123!" : "Demo123!");
        if (sinPassword.Count > 0) await db.SaveChangesAsync();
    }
}

public static class ClaimsPrincipalExtensions
{
    public static int UsuarioId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static bool EsAdmin(this ClaimsPrincipal user) => user.IsInRole(Roles.Administrador);
}
