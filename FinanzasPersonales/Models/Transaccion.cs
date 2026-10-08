using System.ComponentModel.DataAnnotations;

namespace FinanzasPersonales.Models;

public enum TipoTransaccion
{
    Ingreso = 1,
    Egreso = 2
}

public class Transaccion : IValidatableObject
{
    [Display(Name = "No. Transacción")]
    public int Id { get; set; }

    [Display(Name = "Tipo de transacción")]
    public TipoTransaccion TipoTransaccion { get; set; } = TipoTransaccion.Egreso;

    [Required(ErrorMessage = "Seleccione el usuario.")]
    [Display(Name = "Usuario")]
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    // "Gasto o Ingreso": se llena uno u otro según el tipo de transacción.
    [Display(Name = "Gasto")]
    public int? EgresoId { get; set; }
    public Egreso? Egreso { get; set; }

    [Display(Name = "Ingreso")]
    public int? IngresoId { get; set; }
    public Ingreso? Ingreso { get; set; }

    [Required(ErrorMessage = "Seleccione el tipo de pago.")]
    [Display(Name = "Tipo de pago")]
    public int? TipoPagoId { get; set; }
    public TipoPago? TipoPago { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de transacción")]
    public DateTime FechaTransaccion { get; set; } = DateTime.Today;

    [Display(Name = "Fecha de registro")]
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    [Range(0.01, 999_999_999, ErrorMessage = "El monto debe ser mayor que cero.")]
    [DataType(DataType.Currency)]
    [Display(Name = "Monto")]
    public decimal Monto { get; set; }

    /// <summary>Por seguridad sólo se guardan los últimos 4 dígitos de la tarjeta.</summary>
    [StringLength(4)]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "Indique los últimos 4 dígitos de la tarjeta.")]
    [Display(Name = "No. Tarjeta CR (últimos 4)")]
    public string? NoTarjeta { get; set; }

    [StringLength(250)]
    public string? Comentario { get; set; }

    /// <summary>true = activa, false = anulada (no cuenta en el corte).</summary>
    [Display(Name = "Activa")]
    public bool Estado { get; set; } = true;

    public string Concepto => TipoTransaccion == TipoTransaccion.Egreso
        ? Egreso?.Descripcion ?? ""
        : Ingreso?.Descripcion ?? "";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TipoTransaccion == TipoTransaccion.Egreso && EgresoId is null)
            yield return new ValidationResult("Seleccione el gasto.", [nameof(EgresoId)]);
        if (TipoTransaccion == TipoTransaccion.Ingreso && IngresoId is null)
            yield return new ValidationResult("Seleccione el ingreso.", [nameof(IngresoId)]);
        if (FechaTransaccion.Date > DateTime.Today)
            yield return new ValidationResult("La fecha de transacción no puede ser futura.", [nameof(FechaTransaccion)]);
    }
}
