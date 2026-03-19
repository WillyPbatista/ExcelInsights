/// <summary>
/// RESULT PATTERN: Result&lt;T&gt;
/// 
/// ¿Por qué existe esto?
/// En C# la manera "fácil" de señalar un error es lanzar una excepción.
/// El problema es que las excepciones están diseñadas para situaciones
/// INESPERADAS (falla de red, memoria llena, bug). Pero hay errores
/// que son parte normal del negocio:
///   - El archivo tiene columnas vacías
///   - Un email tiene formato inválido
///   - Una edad es negativa
/// 
/// Estos no son bugs — son casos de negocio esperados. Usar excepciones
/// para controlarlos es como usar una alarma de incendios para avisar
/// que el café está listo. Funciona, pero no es el propósito.
/// 
/// Result&lt;T&gt; te permite devolver éxito o fallo como un VALOR NORMAL:
/// 
///   // En lugar de:
///   throw new InvalidEmailException("email inválido");
/// 
///   // Haces:
///   return Result&lt;AnalysisResult&gt;.Failure("email inválido");
/// 
///   // El caller decide qué hacer:
///   if (!result.IsSuccess)
///       return BadRequest(result.Error);
/// 
/// ¿Cuándo usarlo?
/// Para errores de negocio predecibles en los Handlers.
/// Las excepciones siguen siendo válidas para errores de infraestructura
/// (ej: ClosedXML no puede abrir el archivo porque está corrupto).
/// 
/// Nota: en Issue 1 solo lo defines. Lo usas a partir de Issue 3
/// cuando la validación puede devolver fallos de negocio.
/// </summary>

namespace ExcelInsights.Application.Common;

public class Result<T>
{
    /// <summary>
    /// Indica si la operación fue exitosa.
    /// Siempre verifica esto antes de acceder a Value.
    /// </summary>
    public bool IsSuccess { get; private init; }

    /// <summary>
    /// El valor de retorno si IsSuccess es true.
    /// Es null si la operación falló — por eso es T? (nullable).
    /// Nunca accedas a esto sin verificar IsSuccess primero.
    /// </summary>
    public T? Value { get; private init; }

    /// <summary>
    /// Mensaje descriptivo del error si IsSuccess es false.
    /// Es null si la operación fue exitosa.
    /// </summary>
    public string? Error { get; private init; }

    /// <summary>
    /// Constructor privado — fuerza el uso de los métodos estáticos
    /// Success() y Failure() para construir el objeto.
    /// Así es imposible crear un Result en estado inválido
    /// (ej: IsSuccess=true pero con Error seteado).
    /// </summary>
    private Result() { }

    /// <summary>
    /// Crea un resultado exitoso con el valor dado.
    /// Uso: Result&lt;AnalysisResult&gt;.Success(analysisResult)
    /// </summary>
    public static Result<T> Success(T value) =>
        new() { IsSuccess = true, Value = value };

    /// <summary>
    /// Crea un resultado fallido con el mensaje de error dado.
    /// Uso: Result&lt;AnalysisResult&gt;.Failure("El archivo está vacío")
    /// </summary>
    public static Result<T> Failure(string error) =>
        new() { IsSuccess = false, Error = error };

    /// <summary>
    /// Conversión implícita desde T a Result&lt;T&gt;.
    /// Permite escribir "return value;" en lugar de
    /// "return Result&lt;T&gt;.Success(value);" cuando el método
    /// devuelve Result&lt;T&gt;. Azúcar sintáctica.
    /// </summary>
    public static implicit operator Result<T>(T value) =>
        Success(value);
}