using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FinanzasPersonales.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RenglonesEgreso",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RenglonesEgreso", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposEgreso",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposEgreso", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposIngreso",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposIngreso", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposPago",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposPago", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rol = table.Column<int>(type: "int", nullable: false),
                    Cedula = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    LimiteEgresos = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TipoPersona = table.Column<int>(type: "int", nullable: false),
                    DiaCorte = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ingresos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoIngresoId = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Institucion = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingresos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ingresos_TiposIngreso_TipoIngresoId",
                        column: x => x.TipoIngresoId,
                        principalTable: "TiposIngreso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Egresos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoEgresoId = table.Column<int>(type: "int", nullable: false),
                    RenglonEgresoId = table.Column<int>(type: "int", nullable: false),
                    TipoPagoDefectoId = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Egresos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Egresos_RenglonesEgreso_RenglonEgresoId",
                        column: x => x.RenglonEgresoId,
                        principalTable: "RenglonesEgreso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Egresos_TiposEgreso_TipoEgresoId",
                        column: x => x.TipoEgresoId,
                        principalTable: "TiposEgreso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Egresos_TiposPago_TipoPagoDefectoId",
                        column: x => x.TipoPagoDefectoId,
                        principalTable: "TiposPago",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Cortes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Anio = table.Column<int>(type: "int", nullable: false),
                    Mes = table.Column<int>(type: "int", nullable: false),
                    FechaDesde = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaCorte = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BalanceInicial = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalIngresos = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalEgresos = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAlCorte = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaProceso = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cortes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cortes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Transacciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoTransaccion = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    EgresoId = table.Column<int>(type: "int", nullable: true),
                    IngresoId = table.Column<int>(type: "int", nullable: true),
                    TipoPagoId = table.Column<int>(type: "int", nullable: false),
                    FechaTransaccion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NoTarjeta = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    Comentario = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transacciones", x => x.Id);
                    table.CheckConstraint("CK_Transaccion_Concepto", "([TipoTransaccion] = 2 AND [EgresoId] IS NOT NULL AND [IngresoId] IS NULL) OR ([TipoTransaccion] = 1 AND [IngresoId] IS NOT NULL AND [EgresoId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_Transacciones_Egresos_EgresoId",
                        column: x => x.EgresoId,
                        principalTable: "Egresos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transacciones_Ingresos_IngresoId",
                        column: x => x.IngresoId,
                        principalTable: "Ingresos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transacciones_TiposPago_TipoPagoId",
                        column: x => x.TipoPagoId,
                        principalTable: "TiposPago",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transacciones_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "RenglonesEgreso",
                columns: new[] { "Id", "Descripcion", "Estado" },
                values: new object[,]
                {
                    { 1, "Comida", true },
                    { 2, "Combustible", true },
                    { 3, "Recreación", true },
                    { 4, "Servicios", true }
                });

            migrationBuilder.InsertData(
                table: "TiposEgreso",
                columns: new[] { "Id", "Descripcion", "Estado" },
                values: new object[,]
                {
                    { 1, "Gasto", true },
                    { 2, "Inversión", true },
                    { 3, "Costo", true }
                });

            migrationBuilder.InsertData(
                table: "TiposIngreso",
                columns: new[] { "Id", "Descripcion", "Estado" },
                values: new object[,]
                {
                    { 1, "Salario Base", true },
                    { 2, "Horas Extras", true },
                    { 3, "Comisiones", true },
                    { 4, "Bonificación", true }
                });

            migrationBuilder.InsertData(
                table: "TiposPago",
                columns: new[] { "Id", "Descripcion", "Estado" },
                values: new object[,]
                {
                    { 1, "Efectivo", true },
                    { 2, "Tarjeta de Crédito", true },
                    { 3, "Tarjeta de Débito", true },
                    { 4, "Cheque", true },
                    { 5, "Transferencia", true }
                });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "Cedula", "DiaCorte", "Email", "Estado", "LimiteEgresos", "Nombre", "PasswordHash", "Rol", "TipoPersona" },
                values: new object[,]
                {
                    { 1, "00100000009", 28, "demo@finanzas.com", true, 25000m, "Usuario Demo", null, 1, 1 },
                    { 2, "00100000017", 28, "admin@finanzas.com", true, 0m, "Administrador", null, 2, 1 }
                });

            migrationBuilder.InsertData(
                table: "Egresos",
                columns: new[] { "Id", "Descripcion", "Estado", "RenglonEgresoId", "TipoEgresoId", "TipoPagoDefectoId" },
                values: new object[,]
                {
                    { 1, "Compra Supermercado", true, 1, 1, 2 },
                    { 2, "Compra colmado", true, 1, 1, 1 },
                    { 3, "Recarga combustible", true, 2, 1, 3 },
                    { 4, "Salida al cine", true, 3, 1, 2 }
                });

            migrationBuilder.InsertData(
                table: "Ingresos",
                columns: new[] { "Id", "Descripcion", "Estado", "Institucion", "TipoIngresoId" },
                values: new object[,]
                {
                    { 1, "Salario Base Unapec", true, "Universidad APEC", 1 },
                    { 2, "Salario Base Consultora AXP", true, "Consultora AXP", 1 },
                    { 3, "Bonificación Consultora", true, "Consultora AXP", 4 }
                });

            migrationBuilder.InsertData(
                table: "Transacciones",
                columns: new[] { "Id", "Comentario", "EgresoId", "Estado", "FechaRegistro", "FechaTransaccion", "IngresoId", "Monto", "NoTarjeta", "TipoPagoId", "TipoTransaccion", "UsuarioId" },
                values: new object[,]
                {
                    { 1, null, null, true, new DateTime(2026, 9, 30, 12, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 45000m, null, 5, 1, 1 },
                    { 2, null, null, true, new DateTime(2026, 9, 30, 12, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 9, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, 30000m, null, 5, 1, 1 },
                    { 3, null, 1, true, new DateTime(2026, 9, 30, 12, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 9, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 8500m, "4321", 2, 2, 1 },
                    { 4, null, 3, true, new DateTime(2026, 9, 30, 12, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 9, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 3200m, "8765", 3, 2, 1 },
                    { 5, "Compra semanal", 2, true, new DateTime(2026, 9, 30, 12, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 9, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 1250m, null, 1, 2, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cortes_UsuarioId_Anio_Mes",
                table: "Cortes",
                columns: new[] { "UsuarioId", "Anio", "Mes" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Egresos_RenglonEgresoId",
                table: "Egresos",
                column: "RenglonEgresoId");

            migrationBuilder.CreateIndex(
                name: "IX_Egresos_TipoEgresoId",
                table: "Egresos",
                column: "TipoEgresoId");

            migrationBuilder.CreateIndex(
                name: "IX_Egresos_TipoPagoDefectoId",
                table: "Egresos",
                column: "TipoPagoDefectoId");

            migrationBuilder.CreateIndex(
                name: "IX_Ingresos_TipoIngresoId",
                table: "Ingresos",
                column: "TipoIngresoId");

            migrationBuilder.CreateIndex(
                name: "IX_Transacciones_EgresoId",
                table: "Transacciones",
                column: "EgresoId");

            migrationBuilder.CreateIndex(
                name: "IX_Transacciones_IngresoId",
                table: "Transacciones",
                column: "IngresoId");

            migrationBuilder.CreateIndex(
                name: "IX_Transacciones_TipoPagoId",
                table: "Transacciones",
                column: "TipoPagoId");

            migrationBuilder.CreateIndex(
                name: "IX_Transacciones_UsuarioId_FechaTransaccion",
                table: "Transacciones",
                columns: new[] { "UsuarioId", "FechaTransaccion" });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Cedula",
                table: "Usuarios",
                column: "Cedula",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cortes");

            migrationBuilder.DropTable(
                name: "Transacciones");

            migrationBuilder.DropTable(
                name: "Egresos");

            migrationBuilder.DropTable(
                name: "Ingresos");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "RenglonesEgreso");

            migrationBuilder.DropTable(
                name: "TiposEgreso");

            migrationBuilder.DropTable(
                name: "TiposPago");

            migrationBuilder.DropTable(
                name: "TiposIngreso");
        }
    }
}
