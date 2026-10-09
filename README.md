# Sistema de Gestión de Finanzas Personales

Proyecto final de asignatura — Universidad APEC (Prof. Juan P. Valdez).

Aplicación web para gestionar ingresos, egresos y cortes mensuales, con **cuentas de usuario y dos roles (Administrador y Usuario)
## Tecnologías

| Capa | Tecnología |
|---|---|
| Backend | ASP.NET Core MVC 10 (C#) |
| ORM | Entity Framework Core 10 (Code First + Migraciones) |
| Base de datos | SQL Server LocalDB (cambiable en `appsettings.json`) |
| Autenticación | Cookies de ASP.NET Core + `PasswordHasher` de Identity (PBKDF2) |
| Frontend | Razor Views, Bootstrap 5.3, Bootstrap Icons, tipografía Inter, jQuery Validation |

## Cuentas de demostración

La base de datos se crea con dos cuentas listas para probar:

| Rol | Correo | Contraseña |
|---|---|---|
| Administrador | `admin@finanzas.com` | `Admin123!` |
| Usuario | `demo@finanzas.com` | `Demo123!` |
