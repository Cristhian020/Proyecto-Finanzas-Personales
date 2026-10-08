using System.ComponentModel.DataAnnotations;

namespace FinanzasPersonales.Models;

public class Egreso
{
    [Display(Name = "Identificador")]
    public int Id { get; set; }

    [Required(ErrorMessage = "Seleccione el tipo de egreso.")]
    [Display(Name = "Tipo de egreso")]
    public int? TipoEgresoId { get; set; }
    public TipoEgreso? TipoEgreso { get; set; }

    [Required(ErrorMessage = "Seleccione el renglón de egreso.")]
    [Display(Name = "Renglón de egreso")]
    public int? RenglonEgresoId { get; set; }
    public RenglonEgreso? RenglonEgreso { get; set; }

    [Required(ErrorMessage = "Seleccione el tipo de pago por defecto.")]
    [Display(Name = "Tipo de pago x defecto")]
    public int? TipoPagoDefectoId { get; set; }
    public TipoPago? TipoPagoDefecto { get; set; }

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(150)]
    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = string.Empty;

    [Display(Name = "Activo")]
    public bool Estado { get; set; } = true;
}
