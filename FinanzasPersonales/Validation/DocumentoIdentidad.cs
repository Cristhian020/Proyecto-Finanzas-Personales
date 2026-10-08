namespace FinanzasPersonales.Validation;

/// <summary>Validación de cédula y RNC de República Dominicana.</summary>
public static class DocumentoIdentidad
{
    public static string Limpiar(string? valor) =>
        new((valor ?? "").Where(char.IsDigit).ToArray());

    /// <summary>Cédula: 11 dígitos, el último es verificador (Luhn con pesos 1,2 alternos).</summary>
    public static bool CedulaValida(string cedula)
    {
        if (cedula.Length != 11) return false;
        int suma = 0;
        for (int i = 0; i < 10; i++)
        {
            int prod = (cedula[i] - '0') * (i % 2 == 0 ? 1 : 2);
            suma += prod >= 10 ? prod - 9 : prod;
        }
        int verificador = (10 - suma % 10) % 10;
        return verificador == cedula[10] - '0';
    }

    /// <summary>RNC: 9 dígitos, el último es verificador (módulo 11 con pesos 7,9,8,6,5,4,3,2).</summary>
    public static bool RncValido(string rnc)
    {
        if (rnc.Length != 9) return false;
        int[] pesos = [7, 9, 8, 6, 5, 4, 3, 2];
        int suma = 0;
        for (int i = 0; i < 8; i++) suma += (rnc[i] - '0') * pesos[i];
        int resto = suma % 11;
        int verificador = resto switch { 0 => 2, 1 => 1, _ => 11 - resto };
        return verificador == rnc[8] - '0';
    }

    public static string Formatear(string valor) => valor.Length switch
    {
        11 => $"{valor[..3]}-{valor[3..10]}-{valor[10]}",
        9 => $"{valor[0]}-{valor[1..3]}-{valor[3..8]}-{valor[8]}",
        _ => valor
    };
}
