using System.ComponentModel.DataAnnotations;
using FinanzasPersonales.Validation;

namespace FinanzasPersonales.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Indique su correo.")]
    [EmailAddress(ErrorMessage = "Correo no válido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indique su contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Mantener sesión iniciada")]
    public bool Recordarme { get; set; }

    public string? ReturnUrl { get; set; }
}

public class RegistroViewModel : IValidatableObject
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Correo no válido.")]
    [StringLength(150)]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Tipo persona")]
    public TipoPersona TipoPersona { get; set; } = TipoPersona.Fisica;

    [Required(ErrorMessage = "La cédula / RNC es obligatoria.")]
    [StringLength(13)]
    [Display(Name = "Cédula / RNC")]
    public string Cedula { get; set; } = string.Empty;

    [Range(0, 999_999_999, ErrorMessage = "El límite debe ser un monto positivo.")]
    [Display(Name = "Límite de egresos mensual")]
    public decimal LimiteEgresos { get; set; }

    [Range(1, 28, ErrorMessage = "El día de corte debe estar entre 1 y 28.")]
    [Display(Name = "Día de corte")]
    public int DiaCorte { get; set; } = 28;

    [Required(ErrorMessage = "Indique una contraseña.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmarPassword { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var limpio = DocumentoIdentidad.Limpiar(Cedula);
        if (TipoPersona == TipoPersona.Fisica && !DocumentoIdentidad.CedulaValida(limpio))
            yield return new ValidationResult("La cédula no es válida (11 dígitos con dígito verificador).", [nameof(Cedula)]);
        if (TipoPersona == TipoPersona.Juridica && !DocumentoIdentidad.RncValido(limpio))
            yield return new ValidationResult("El RNC no es válido (9 dígitos con dígito verificador).", [nameof(Cedula)]);
    }
}

public class PerfilViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Correo no válido.")]
    [StringLength(150)]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Range(0, 999_999_999, ErrorMessage = "El límite debe ser un monto positivo.")]
    [Display(Name = "Límite de egresos mensual")]
    public decimal LimiteEgresos { get; set; }

    [Range(1, 28, ErrorMessage = "El día de corte debe estar entre 1 y 28.")]
    [Display(Name = "Día de corte")]
    public int DiaCorte { get; set; }

    // Sólo lectura en el perfil.
    public string Cedula { get; set; } = string.Empty;
    public TipoPersona TipoPersona { get; set; }
    public Rol Rol { get; set; }
}

public class CambiarPasswordViewModel
{
    [Required(ErrorMessage = "Indique su contraseña actual.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña actual")]
    public string Actual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indique la nueva contraseña.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    public string Nueva { get; set; } = string.Empty;

    [Compare(nameof(Nueva), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar nueva contraseña")]
    public string Confirmar { get; set; } = string.Empty;
}
