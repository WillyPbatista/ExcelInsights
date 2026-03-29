using ExcelInsights.Application.Common;

namespace ExcelInsights.Api.Validators;

/// <summary>
/// Valida un archivo subido antes de que llegue al parser.
/// Método estático porque no tiene estado interno — recibe todo
/// lo que necesita como parámetros y no guarda nada.
/// </summary>
public static class UploadFileValidator
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".xlsx", ".xls" };

    /// <summary>
    /// Valida el archivo contra las reglas de negocio.
    /// Devuelve null si el archivo es válido.
    /// Devuelve un mensaje de error si algo está mal.
    /// </summary>
    public static string? Validate(IFormFile file, ExcelInsightsSettings settings)
    {
        // 1. Archivo presente y no vacío
        if (file is null || file.Length == 0)
            return "No se ha proporcionado ningún archivo o está vacío.";

        // 2. Extensión permitida
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return $"El formato '{extension}' no está permitido. Solo se aceptan archivos .xlsx y .xls.";

        // 3. Tamaño máximo
        // Convertimos MB → bytes aquí para que appsettings sea legible por humanos
        var maxBytes = (long)settings.MaxFileSizeMb * 1024 * 1024;
        if (file.Length > maxBytes)
            return $"El archivo supera el tamaño máximo de {settings.MaxFileSizeMb} MB.";

        return null; // válido — sin errores
    }
}