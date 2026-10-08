using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FinanzasPersonales.Validation;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FinanzasPersonales.Models;

public enum TipoPersona
{
    [Display(Name = "Física")] Fisica = 1,
    [Display(Name = "Jurídica")] Juridica = 2
}

public enum Rol
{
    [Display(Name = "Usuario")] Usuario = 1,
    [Display(Name = "Administrador")] Administrador = 2
}

public class Usuario : IValidatableObject
{
    [Display(Name = "Identificador")]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Correo no válido.")]
    [StringLength(150)]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Hash de la contraseña (PasswordHasher de ASP.NET Core Identity). Nunca se recibe del formulario.</summary>
    [BindNever]
    public string? PasswordHash { get; set; }

    [Display(Name = "Rol")]
    public Rol Rol { get; set; } = Rol.Usuario;

    /// <summary>Cédula (11 dígitos) para persona física o RNC (9 dígitos) para persona jurídica. Se guarda sin guiones.</summary>
    [Required(ErrorMessage = "La cédula / RNC es obligatoria.")]
    [StringLength(13)]
    [Display(Name = "Cédula / RNC")]
    public string Cedula { get; set; } = string.Empty;

    [Range(0, 999_999_999, ErrorMessage = "El límite debe ser un monto positivo.")]
    [DataType(DataType.Currency)]
    [Display(Name = "Límite de egresos mensual")]
    public decimal LimiteEgresos { get; set; }

    [Display(Name = "Tipo persona")]
    public TipoPersona TipoPersona { get; set; } = TipoPersona.Fisica;

    /// <summary>Día del mes en que cierra el periodo del usuario (1-28 para que exista en todos los meses).</summary>
    [Range(1, 28, ErrorMessage = "El día de corte debe estar entre 1 y 28.")]
    [Display(Name = "Día de corte")]
    public int DiaCorte { get; set; } = 28;

    [Display(Name = "Activo")]
    public bool Estado { get; set; } = true;

    /// <summary>Sólo para el formulario del administrador: nueva contraseña (opcional al editar).</summary>
    [NotMapped]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string? NuevaPassword { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var limpio = DocumentoIdentidad.Limpiar(Cedula);
        if (TipoPersona == TipoPersona.Fisica && !DocumentoIdentidad.CedulaValida(limpio))
            yield return new ValidationResult("La cédula no es válida (11 dígitos con dígito verificador).", [nameof(Cedula)]);
        if (TipoPersona == TipoPersona.Juridica && !DocumentoIdentidad.RncValido(limpio))
            yield return new ValidationResult("El RNC no es válido (9 dígitos con dígito verificador).", [nameof(Cedula)]);
    }
}
