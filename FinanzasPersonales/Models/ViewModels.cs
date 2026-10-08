using System.ComponentModel.DataAnnotations;

namespace FinanzasPersonales.Models;

/// <summary>Filtros de la consulta por criterios de transacciones.</summary>
public class ConsultaFiltro
{
    [Display(Name = "Usuario")] public int? UsuarioId { get; set; }
    [Display(Name = "Tipo de transacción")] public TipoTransaccion? TipoTransaccion { get; set; }
    [DataType(DataType.Date), Display(Name = "Desde")] public DateTime? Desde { get; set; }
    [DataType(DataType.Date), Display(Name = "Hasta")] public DateTime? Hasta { get; set; }
    [Display(Name = "Tipo de egreso")] public int? TipoEgresoId { get; set; }
    [Display(Name = "Renglón de egreso")] public int? RenglonEgresoId { get; set; }
    [Display(Name = "Tipo de ingreso")] public int? TipoIngresoId { get; set; }
    [Display(Name = "Tipo de pago")] public int? TipoPagoId { get; set; }
    [Display(Name = "Incluir anuladas")] public bool IncluirAnuladas { get; set; }
}

public class ConsultaViewModel
{
    public ConsultaFiltro Filtro { get; set; } = new();
    public List<Transaccion> Resultados { get; set; } = [];
    public decimal TotalIngresos => Resultados.Where(t => t.Estado && t.TipoTransaccion == TipoTransaccion.Ingreso).Sum(t => t.Monto);
    public decimal TotalEgresos => Resultados.Where(t => t.Estado && t.TipoTransaccion == TipoTransaccion.Egreso).Sum(t => t.Monto);
}

public class ProcesarCorteViewModel
{
    [Required(ErrorMessage = "Seleccione el usuario.")]
    [Display(Name = "Usuario")] public int? UsuarioId { get; set; }

    [Range(2000, 2100)]
    [Display(Name = "Año")] public int Anio { get; set; } = DateTime.Today.AddMonths(-1).Year;

    [Range(1, 12)]
    public int Mes { get; set; } = DateTime.Today.AddMonths(-1).Month;
}

/// <summary>Filtros del reporte de corte.</summary>
public class ReporteCorteFiltro
{
    [Display(Name = "Usuario")] public int? UsuarioId { get; set; }
    [DataType(DataType.Date), Display(Name = "Desde")] public DateTime? Desde { get; set; }
    [DataType(DataType.Date), Display(Name = "Hasta")] public DateTime? Hasta { get; set; }
}

public class ReporteCorteViewModel
{
    public ReporteCorteFiltro Filtro { get; set; } = new();
    public List<Corte> Cortes { get; set; } = [];
}

public class DetalleCorteViewModel
{
    public Corte Corte { get; set; } = null!;
    public List<Transaccion> Transacciones { get; set; } = [];
    public List<(string Renglon, decimal Total)> EgresosPorRenglon { get; set; } = [];
}

public class DashboardViewModel
{
    public bool EsAdmin { get; set; }
    public string Nombre { get; set; } = "";
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public int Usuarios { get; set; }
    public int Transacciones { get; set; }
    public decimal Ingresos { get; set; }
    public decimal Egresos { get; set; }
    public decimal LimiteEgresos { get; set; }
    public decimal? BalanceUltimoCorte { get; set; }
    public List<Transaccion> Ultimas { get; set; } = [];
    public List<(string Renglon, decimal Total)> EgresosPorRenglon { get; set; } = [];
    public List<(Usuario Usuario, decimal Gastado)> SobreLimite { get; set; } = [];

    public decimal Neto => Ingresos - Egresos;
    public int PorcentajeLimite => LimiteEgresos <= 0 ? 0 : (int)Math.Min(100, Math.Round(Egresos / LimiteEgresos * 100));
}
