using System.ComponentModel.DataAnnotations;

namespace FinanzasPersonales.Models;

public class Corte
{
    [Display(Name = "Identificador")]
    public int Id { get; set; }

    // No está en el enunciado, pero cada usuario tiene su propio día de corte, así que el corte es por usuario.
    [Display(Name = "Usuario")]
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    [Display(Name = "Año")]
    public int Anio { get; set; }

    public int Mes { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Fecha desde")]
    public DateTime FechaDesde { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de corte")]
    public DateTime FechaCorte { get; set; }

    [DataType(DataType.Currency)]
    [Display(Name = "Balance inicial")]
    public decimal BalanceInicial { get; set; }

    [DataType(DataType.Currency)]
    [Display(Name = "Total ingresos")]
    public decimal TotalIngresos { get; set; }

    [DataType(DataType.Currency)]
    [Display(Name = "Total egresos")]
    public decimal TotalEgresos { get; set; }

    [DataType(DataType.Currency)]
    [Display(Name = "Balance al corte")]
    public decimal BalanceAlCorte { get; set; }

    [Display(Name = "Fecha de proceso")]
    public DateTime FechaProceso { get; set; } = DateTime.Now;
}
