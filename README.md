# Sistema de Gestión de Finanzas Personales

Proyecto final de asignatura — Universidad APEC (Prof. Juan P. Valdez).

Aplicación web para gestionar ingresos, egresos y cortes mensuales, con **cuentas de usuario y dos roles (Administrador y Usuario)**. Está desarrollada con **ASP.NET Core MVC (.NET 10)** y **Entity Framework Core** sobre **SQL Server**, con una interfaz moderna y minimalista.

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

> Cambia estas contraseñas desde **Mi perfil → Cambiar contraseña** si despliegas el sistema.

## Roles y permisos

| Funcionalidad | Usuario | Administrador |
|---|:---:|:---:|
| Registrarse e iniciar sesión | ✔ | ✔ |
| Panel de inicio | Su periodo actual | Resumen global |
| Registrar, editar y anular transacciones | Sólo las suyas | De todos |
| Consulta por criterios | Sólo las suyas | De todos (filtro por usuario) |
| Procesar corte mensual | Su propio corte | De cualquier usuario |
| Ver cortes y reporte de cortes | Sólo los suyos | De todos |
| Editar su perfil y cambiar su contraseña | ✔ | ✔ |
| Gestionar usuarios (roles, contraseñas, activar/inactivar) | — | ✔ |
| Gestionar egresos, ingresos y catálogos | — | ✔ |

Seguridad:
- Todas las páginas exigen sesión iniciada, salvo login y registro.
- Las secciones de administración usan `[Authorize(Roles = "Administrador")]`.
- Un usuario no puede ver ni modificar datos de otro: los datos se filtran en el servidor y, si intenta acceder por URL, recibe 404.
- Las contraseñas se guardan con hash (PBKDF2), nunca en texto plano.
- Si un administrador inactiva una cuenta o le cambia el rol, la sesión de esa persona se cierra en su siguiente petición.
- Un administrador no puede quitarse su propio rol ni inactivar su propia cuenta.
- Todos los formularios usan protección anti-CSRF (`ValidateAntiForgeryToken`).

## Funcionalidades

### Catálogos (Identificador, Descripción, Estado) — Administrador
- **Tipos de Egreso**: Gasto, Inversión, Costo…
- **Tipos de Ingreso**: Salario Base, Horas Extras, Comisiones…
- **Renglones de Egreso**: Comida, Combustible, Recreación…
- **Tipos de Pago**: Efectivo, Tarjeta, Cheque…

Los cuatro comparten un controlador genérico (`CatalogoController<T>`) y las mismas vistas. Incluyen búsqueda, creación, edición, activar/inactivar y validación de descripciones duplicadas.

### Gestión — Administrador
- **Egresos**: tipo de egreso, renglón, tipo de pago por defecto, descripción y estado.
- **Ingresos**: tipo de ingreso, descripción, institución/empleador/cliente y estado.
- **Usuarios**: nombre, correo, rol, cédula/RNC, límite de egresos, tipo de persona (Física/Jurídica), día de corte, estado y restablecimiento de contraseña.
  - La **cédula** (11 dígitos) y el **RNC** (9 dígitos) se validan con su dígito verificador.
  - No se permiten correos ni cédulas duplicados.

### Registro de transacciones
- Tipo de transacción (Ingreso/Egreso). El formulario muestra el selector de gasto o de ingreso según el tipo elegido.
- Al elegir un gasto, se propone automáticamente su **tipo de pago por defecto**.
- Si el tipo de pago es una tarjeta, se exige el **No. de tarjeta**. Por seguridad sólo se guardan los **últimos 4 dígitos**.
- La fecha de transacción la indica el usuario y no puede ser futura. La fecha de registro la asigna el sistema.
- **Aviso de límite de egresos**: si los egresos del periodo superan el límite del usuario, se muestra una advertencia al guardar y en el panel de inicio.
- Las transacciones no se borran: se **anulan** (y pueden reactivarse).
- Las transacciones de un periodo ya cortado quedan **bloqueadas**: no se pueden crear, editar ni anular.

### Proceso de corte mensual
- El corte es por usuario, año y mes.
- **Periodo**: desde el día siguiente al corte del mes anterior hasta el día de corte del usuario.
- **Balance inicial** = balance al corte del mes anterior (0 si es el primero).
- **Balance al corte** = balance inicial + total ingresos − total egresos (sólo transacciones activas).
- Los cortes son consecutivos. Sólo el último puede reprocesarse y no se puede cortar un periodo que no ha terminado.
- El detalle del corte muestra las transacciones del periodo y los egresos agrupados por renglón, y puede imprimirse.

### Consulta y reportes
- **Consulta por criterios**: usuario (admin), tipo de transacción, rango de fechas, tipo de egreso, renglón, tipo de ingreso, tipo de pago e inclusión de anuladas. Muestra totales de ingresos, egresos y neto.
- **Reporte de cortes**: entre fechas y/o por usuario, con totales y versión imprimible.
- **Panel de inicio**
  - Usuario: balance estimado, ingresos y egresos del periodo, avance hacia el límite de egresos, gastos por renglón y movimientos recientes.
  - Administrador: usuarios activos, totales del mes, usuarios que superaron su límite y últimos movimientos de todos.

## Diseño

Interfaz minimalista hecha a medida sobre Bootstrap 5.3:
- Barra lateral fija con navegación por secciones. En móvil se vuelve un menú deslizable.
- Paleta neutra con un único color de acento (esmeralda). Verde para ingresos y rojo para egresos.
- Tipografía **Inter**, tarjetas con borde fino, indicadores (KPI) con cifras grandes y tablas limpias.
- Pantallas de inicio de sesión y registro independientes.
- Estilos de impresión para reportes y cortes.

## Modelo de datos

```
TipoEgreso, TipoIngreso, RenglonEgreso, TipoPago (Id, Descripcion, Estado)
Egreso       (Id, TipoEgresoId, RenglonEgresoId, TipoPagoDefectoId, Descripcion, Estado)
Ingreso      (Id, TipoIngresoId, Descripcion, Institucion, Estado)
Usuario      (Id, Nombre, Email, PasswordHash, Rol, Cedula, LimiteEgresos,
              TipoPersona, DiaCorte, Estado)
Transaccion  (Id, TipoTransaccion, UsuarioId, EgresoId | IngresoId, TipoPagoId,
              FechaTransaccion, FechaRegistro, Monto, NoTarjeta, Comentario, Estado)
Corte        (Id, UsuarioId, Anio, Mes, FechaDesde, FechaCorte, BalanceInicial,
              TotalIngresos, TotalEgresos, BalanceAlCorte, FechaProceso)
```

Decisiones de diseño respecto al enunciado:
- **Usuario.Email / PasswordHash / Rol**: se agregaron para el inicio de sesión y los permisos.
- **Corte.UsuarioId**: el enunciado no lo incluye, pero cada usuario tiene su propio día de corte y límite, así que el corte debe ser por usuario.
- **Usuario.DiaCorte** (1–28) representa la "Fecha de Corte": es el día del mes en que cierra su periodo. Se limita a 28 para que exista en todos los meses.
- **"Gasto o Ingreso"** se modela con dos llaves foráneas (`EgresoId` / `IngresoId`) y una restricción `CHECK` en la base de datos que obliga a llenar exactamente una según el tipo.
- **Estado** funciona como borrado lógico: nada se elimina físicamente y las llaves foráneas usan `Restrict`.

## Cómo ejecutar

Requisitos: [.NET 10 SDK](https://dotnet.microsoft.com/download) y SQL Server LocalDB (viene con Visual Studio) u otra instancia de SQL Server.

```bash
git clone <url-del-repositorio>
cd "Finanzas Personales"
dotnet run --project FinanzasPersonales
```

Abre `http://localhost:5017` e inicia sesión con una de las cuentas de demostración.

Al iniciar, la base de datos se crea y migra automáticamente. Incluye datos de ejemplo: catálogos, egresos, ingresos, las dos cuentas y transacciones de septiembre 2026 del usuario demo.

Para usar otro servidor SQL, cambia `ConnectionStrings:FinanzasDb` en `FinanzasPersonales/appsettings.json`.

Comandos útiles de Entity Framework:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add NombreMigracion --project FinanzasPersonales
dotnet ef database update --project FinanzasPersonales
```

## Pruebas automáticas

El proyecto `FinanzasPersonales.Tests` (xUnit) tiene **146 pruebas**:

```bash
dotnet test
```

| Archivo | Qué verifica |
|---|---|
| `Unitarias/DocumentoIdentidadTests` | Validación y formato de cédula y RNC |
| `Unitarias/ModelosTests` | Reglas de los modelos: montos, fechas futuras, tarjeta, día de corte, contraseñas |
| `Unitarias/PeriodoTests` | Cálculo de periodos de corte (cambio de año, febrero, periodos sin huecos ni solapes) |
| `Unitarias/CorteServiceTests` | Proceso de corte: totales, balance arrastrado, cortes consecutivos, reproceso, bloqueo del periodo, restricción `CHECK` |
| `Unitarias/CuentaServiceTests` | Hash de contraseñas, login, cuentas inactivas, cuentas sembradas |
| `Integracion/AutenticacionYPermisosTests` | La app completa por HTTP: login, registro, cierre de sesión, cambio de contraseña, permisos por rol en cada página, anti-CSRF, inactivación |
| `Integracion/TransaccionesYCorteTests` | Registro de transacciones, aislamiento entre usuarios, tarjeta, aviso de límite, anulación, consulta, corte mensual de punta a punta, catálogos |

Las pruebas de integración levantan la aplicación real en memoria (`WebApplicationFactory`). Las que tocan base de datos usan una **base LocalDB temporal**, creada con las migraciones reales y eliminada al terminar, así que la base de datos de desarrollo no se toca. Requieren SQL Server LocalDB.

## Estructura del proyecto

```
FinanzasPersonales/
├── Controllers/   Cuenta (login, registro, perfil), CatalogoController (genérico),
│                  Egresos, Ingresos, Usuarios, Transacciones, Consulta, Cortes, Home
├── Data/          FinanzasContext (DbContext, configuración y datos semilla)
├── Migrations/    Migraciones de EF Core
├── Models/        Entidades y ViewModels
├── Services/      CorteService (periodos, límite, corte) y CuentaService (autenticación)
├── Validation/    Validación de cédula y RNC dominicanos
├── Views/         Vistas Razor (_Layout con barra lateral, _LayoutAuth para login/registro)
└── wwwroot/css/   site.css (tema minimalista)

FinanzasPersonales.Tests/
├── Infraestructura/  App y base de datos de prueba, utilidades HTTP
├── Unitarias/        Validaciones, periodos, corte y cuentas
└── Integracion/      Flujos completos por HTTP
```
