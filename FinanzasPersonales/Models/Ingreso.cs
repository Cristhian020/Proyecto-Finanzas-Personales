using System.ComponentModel.DataAnnotations;

namespace FinanzasPersonales.Models;

public class Ingreso
{
    [Display(Name = "Identificador")]
    public int Id { get; set; }

    [Required(ErrorMessage = "Seleccione el tipo de ingreso.")]
    [Display(Name = "Tipo de ingreso")]
    public int? TipoIngresoId { get; set; }
    public TipoIngreso? TipoIngreso { get; set; }

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(150)]
    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indique la institución, empleador o cliente.")]
    [StringLength(150)]
    [Display(Name = "Institución / Empleador / Cliente")]
    public string Institucion { get; set; } = string.Empty;

    [Display(Name = "Activo")]
    public bool Estado { get; set; } = true;
}
