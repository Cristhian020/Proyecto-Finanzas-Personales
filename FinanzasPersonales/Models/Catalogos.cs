using System.ComponentModel.DataAnnotations;

namespace FinanzasPersonales.Models;

/// <summary>
/// Base para los catálogos simples (Identificador, Descripción, Estado).
/// </summary>
public abstract class Catalogo
{
    [Display(Name = "Identificador")]
    public int Id { get; set; }

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(100)]
    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = string.Empty;

    [Display(Name = "Activo")]
    public bool Estado { get; set; } = true;
}

public class TipoEgreso : Catalogo { }

public class TipoIngreso : Catalogo { }

public class RenglonEgreso : Catalogo { }

public class TipoPago : Catalogo
{
    /// <summary>Indica si el tipo de pago es con tarjeta (exige No. Tarjeta CR en la transacción).</summary>
    public bool EsTarjeta => Descripcion.Contains("tarjeta", StringComparison.OrdinalIgnoreCase);
}
