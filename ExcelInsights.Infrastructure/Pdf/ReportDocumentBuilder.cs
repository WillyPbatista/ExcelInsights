// =============================================================================
// ReportDocumentBuilder.cs — Infrastructure/Pdf/
// =============================================================================
//
// ¿QUÉ HACE ESTE ARCHIVO?
// Construye la estructura visual completa del PDF usando QuestPDF.
// Recibe un AnalysisResult con todos los datos reales y los convierte
// en un documento con tres secciones: header, estadísticas y errores.
//
// ¿POR QUÉ SEPARADO DE QuestPdfGenerator?
// QuestPdfGenerator tiene una responsabilidad: orquestar la generación
// (crear el Document, llamar al Builder, devolver los bytes).
// ReportDocumentBuilder tiene otra: definir el layout visual.
// Si mezclas ambas en un archivo, cualquier cambio de estilo requiere
// tocar la misma clase que maneja la generación — viola SRP.
//
// CONCEPTOS CLAVE DE QUESTPDF USADOS AQUÍ:
//
//   IDocument    → interfaz que QuestPDF requiere para construir un documento.
//                  Requiere GetMetadata() y Compose().
//
//   container.Page() → define una página con tamaño, márgenes y zonas
//                      (Header, Content, Footer).
//
//   Column()     → apila elementos verticalmente uno bajo el otro.
//   Row()        → pone elementos lado a lado horizontalmente.
//
//   column.Item() → obtiene un "slot" dentro del Column donde se pone
//                   un elemento. Sin Item() no puedes agregar nada.
//
//   Table()      → tabla con columnas de ancho definido. Las celdas se
//                   agregan en orden: izquierda a derecha, fila a fila.
//                   QuestPDF distribuye automáticamente sin coordenadas.
//
//   Element()    → permite pasar un Action<IContainer> como bloque
//                   reutilizable de estilo — equivalente a un componente.
//
// SECCIONES DEL DOCUMENTO:
//   1. Header    → título, nombre del archivo, fecha, badge, totales
//   2. Statistics → tabla con tipo, métricas y métricas por columna
//   3. Errors    → tabla de errores o mensaje "no errors detected"
// =============================================================================

using ExcelInsights.Application.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ExcelInsights.Infrastructure.Pdf;

public class ReportDocumentBuilder : IDocument
{
    // -------------------------------------------------------------------------
    // DATOS Y CONSTANTES DE ESTILO
    // -------------------------------------------------------------------------

    private readonly AnalysisResult _result;

    // Paleta de colores Excel — centralizada aquí para cambiar en un solo lugar
    private const string ExcelGreen  = "#217346";
    private const string LightGreen  = "#EBF1DE";
    private const string DarkGrey    = "#333333";
    private const string ErrorRed    = "#C0392B";
    private const string WarningAmber = "#E67E22";

    public ReportDocumentBuilder(AnalysisResult result)
    {
        _result = result;
    }

    // -------------------------------------------------------------------------
    // INTERFAZ IDocument
    // -------------------------------------------------------------------------

    /// <summary>
    /// Metadatos del documento PDF — título, autor, fecha de creación.
    /// QuestPDF los incluye en las propiedades del archivo PDF.
    /// </summary>
    public DocumentMetadata GetMetadata() => new DocumentMetadata
    {
        Title   = $"Excel Insights Report — {_result.FileName}",
        Author  = "Excel Insights API",
        Subject = "Data Analysis Report"
    };

    /// <summary>
    /// Punto de entrada que QuestPDF llama para construir el documento.
    /// Define la página y delega cada sección a un método privado.
    /// </summary>
    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.PageColor(Colors.White);

            // Estilo de texto base para todo el documento.
            // Cada elemento puede sobrescribir esto con su propio estilo.
            page.DefaultTextStyle(x => x
                .FontSize(10)
                .FontColor(DarkGrey));

            // Las tres zonas del documento
            page.Header().Element(ComposeHeader);
            page.Content().PaddingVertical(16).Column(column =>
            {
                column.Spacing(20);
                BuildSummarySection(column);
                BuildStatisticsSection(column);
                BuildErrorsSection(column);
            });
            page.Footer().Element(ComposeFooter);
        });
    }

    // -------------------------------------------------------------------------
    // HEADER — título, nombre del archivo, fecha, badge
    // -------------------------------------------------------------------------

    /// <summary>
    /// Zona superior del PDF: título a la izquierda, badge a la derecha.
    /// Usa Row para poner los dos elementos lado a lado.
    /// </summary>
    private void ComposeHeader(IContainer container)
    {
        container
            .PaddingBottom(12)
            .BorderBottom(2)
            .BorderColor(ExcelGreen)
            .Row(row =>
            {
                // Lado izquierdo — título y metadatos del archivo
                row.RelativeItem().Column(col =>
                {
                    col.Item()
                        .Text("Excel Insights Report")
                        .FontSize(22)
                        .SemiBold()
                        .FontColor(ExcelGreen);

                    // Nombre del archivo real — viene de _result
                    col.Item()
                        .PaddingTop(2)
                        .Text($"File: {_result.FileName}")
                        .FontSize(10)
                        .Italic()
                        .FontColor(Colors.Grey.Darken1);

                    col.Item()
                        .Text($"Generated: {DateTime.Now:MMMM dd, yyyy HH:mm}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);
                });

                // Lado derecho — badge de estado
                row.ConstantItem(90)
                    .AlignRight()
                    .AlignMiddle()
                    .StatusBadge(_result.InvalidRows == 0 ? "CLEAN" : "ISSUES FOUND");
            });
    }

    // -------------------------------------------------------------------------
    // FOOTER — número de página
    // -------------------------------------------------------------------------

    private void ComposeFooter(IContainer container)
    {
        container
            .PaddingTop(8)
            .BorderTop(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Row(row =>
            {
                row.RelativeItem()
                    .Text("Excel Insights API")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Medium);

                row.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Page ").FontSize(8).FontColor(Colors.Grey.Medium);
                    x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    x.Span(" of ").FontSize(8).FontColor(Colors.Grey.Medium);
                    x.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
    }

    // -------------------------------------------------------------------------
    // SECCIÓN 1 — Resumen general con totales reales
    // -------------------------------------------------------------------------

    /// <summary>
    /// Muestra TotalRows, ValidRows e InvalidRows con datos reales de _result.
    /// Usa un Row con tres columnas iguales para distribuir los números.
    /// </summary>
    private void BuildSummarySection(ColumnDescriptor column)
    {
        column.Item().Column(inner =>
        {
            inner.Item().Element(c => ComposeSectionHeader(c, "Summary"));

            inner.Item()
                .PaddingTop(8)
                .Background(LightGreen)
                .Padding(12)
                .Row(row =>
                {
                    // Cada métrica ocupa el mismo ancho — RelativeItem sin parámetro = peso 1
                    ComposeSummaryMetric(row.RelativeItem(), "Total Rows",   _result.TotalRows.ToString(),   DarkGrey);
                    ComposeSummaryMetric(row.RelativeItem(), "Valid Rows",   _result.ValidRows.ToString(),   ExcelGreen);
                    ComposeSummaryMetric(row.RelativeItem(), "Invalid Rows", _result.InvalidRows.ToString(), _result.InvalidRows > 0 ? ErrorRed : DarkGrey);
                });
        });
    }

    /// <summary>
    /// Un bloque de métrica individual: etiqueta arriba, número grande abajo.
    /// Se reutiliza tres veces en el summary.
    /// </summary>
    private static void ComposeSummaryMetric(IContainer container, string label, string value, string valueColor)
    {
        container.AlignCenter().Column(col =>
        {
            col.Item()
                .Text(label)
                .FontSize(9)
                .FontColor(Colors.Grey.Darken1);

            col.Item()
                .Text(value)
                .FontSize(20)
                .SemiBold()
                .FontColor(valueColor);
        });
    }

    // -------------------------------------------------------------------------
    // SECCIÓN 2 — Tabla de estadísticas por columna
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tabla con una fila por cada columna del Excel analizado.
    /// Muestra: nombre, tipo inferido, confianza, avg, min, max, nulls, únicos.
    /// Si Average/Min/Max son null (columna no numérica), muestra "—".
    /// </summary>
    private void BuildStatisticsSection(ColumnDescriptor column)
    {
        column.Item().Column(inner =>
        {
            inner.Item().Element(c => ComposeSectionHeader(c, "Column Statistics"));

            inner.Item().PaddingTop(8).Table(table =>
            {
                // ── Definición de anchos de columna ──────────────────────────
                // RelativeColumn(N) = N partes del ancho disponible
                // Los números más importantes (Name, Type) tienen más espacio
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(3);  // Name
                    cols.RelativeColumn(2);  // Type
                    cols.RelativeColumn(1);  // Confidence
                    cols.RelativeColumn(2);  // Average
                    cols.RelativeColumn(1);  // Min
                    cols.RelativeColumn(1);  // Max
                    cols.RelativeColumn(1);  // Nulls
                    cols.RelativeColumn(1);  // Unique
                });

                // ── Header de la tabla ───────────────────────────────────────
                table.Header(header =>
                {
                    var headers = new[] { "Column", "Type", "Conf.", "Average", "Min", "Max", "Nulls", "Unique" };

                    foreach (var h in headers)
                    {
                        // IContainer local para el estilo del header
                        // Background verde Excel + texto blanco + padding
                        header.Cell()
                            .Background(ExcelGreen)
                            .PaddingHorizontal(5)
                            .PaddingVertical(4)
                            .Text(h)
                            .FontSize(9)
                            .SemiBold()
                            .FontColor(Colors.White);
                    }
                });

                // ── Filas de datos — una por cada columna del Excel ──────────
                // QuestPDF llena la tabla de izquierda a derecha, fila a fila.
                // No necesitas indicar posición — solo agrega celdas en orden.
                var isOddRow = false;
                foreach (var col in _result.Columns)
                {
                    // Alternamos el fondo para mejorar la legibilidad (zebra striping)
                    isOddRow = !isOddRow;
                    var rowBg = isOddRow ? Colors.White : Colors.Grey.Lighten4;

                    var stats = col.Stats;

                    // Formateamos los valores — "—" si son null (columna no numérica)
                    var avg    = stats?.Average.HasValue == true ? stats.Average.Value.ToString("F1") : "—";
                    var min    = stats?.Min.HasValue     == true ? stats.Min.Value.ToString()          : "—";
                    var max    = stats?.Max.HasValue     == true ? stats.Max.Value.ToString()          : "—";
                    var nulls  = stats?.NullCount.ToString()  ?? "—";
                    var unique = stats?.UniqueCount.ToString() ?? "—";
                    var conf   = $"{col.Confidence:P0}";  // "94%" en lugar de "0.94"

                    var cellValues = new[] { col.Name, col.InferredType, conf, avg, min, max, nulls, unique };

                    foreach (var val in cellValues)
                    {
                        table.Cell()
                            .Background(rowBg)
                            .BorderBottom(1)
                            .BorderColor(Colors.Grey.Lighten2)
                            .PaddingHorizontal(5)
                            .PaddingVertical(3)
                            .Text(val)
                            .FontSize(9);
                    }
                }
            });
        });
    }

    // -------------------------------------------------------------------------
    // SECCIÓN 3 — Tabla de errores detectados
    // -------------------------------------------------------------------------

    /// <summary>
    /// Si hay errores: tabla con RowIndex, ColumnName, Message y Severity.
    /// Las filas de Error se muestran en rojo, las de Warning en naranja.
    /// Si no hay errores: mensaje verde de "No errors detected".
    /// </summary>
    private void BuildErrorsSection(ColumnDescriptor column)
    {
        column.Item().Column(inner =>
        {
            inner.Item().Element(c => ComposeSectionHeader(c, "Errors Detected"));

            // Caso sin errores — mensaje positivo
            if (!_result.Errors.Any())
            {
                inner.Item()
                    .PaddingTop(8)
                    .Text("No errors detected. All data looks clean.")
                    .FontSize(10)
                    .Italic()
                    .FontColor(ExcelGreen);
                return;
            }

            inner.Item().PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(40);  // Row — ancho fijo pequeño
                    cols.RelativeColumn(2);   // Column name
                    cols.RelativeColumn(4);   // Message — más espacio para el texto
                    cols.RelativeColumn(1);   // Severity
                });

                // Header
                table.Header(header =>
                {
                    var headers = new[] { "Row", "Column", "Message", "Severity" };
                    foreach (var h in headers)
                    {
                        header.Cell()
                            .Background(ExcelGreen)
                            .PaddingHorizontal(5)
                            .PaddingVertical(4)
                            .Text(h)
                            .FontSize(9)
                            .SemiBold()
                            .FontColor(Colors.White);
                    }
                });

                // Filas de errores — iteramos _result.Errors reales
                foreach (var error in _result.Errors)
                {
                    // Color según severidad: Error = rojo, Warning = naranja
                    var severityColor = error.Severity == "Error"
                        ? ErrorRed
                        : WarningAmber;

                    // Las primeras 3 celdas (Row, Column, Message) en color neutro
                    var dataCells = new[] { error.RowIndex.ToString(), error.ColumnName, error.Message };
                    foreach (var val in dataCells)
                    {
                        table.Cell()
                            .BorderBottom(1)
                            .BorderColor(Colors.Grey.Lighten2)
                            .PaddingHorizontal(5)
                            .PaddingVertical(3)
                            .Text(val)
                            .FontSize(9);
                    }

                    // La celda de Severity va en su propio color
                    table.Cell()
                        .BorderBottom(1)
                        .BorderColor(Colors.Grey.Lighten2)
                        .PaddingHorizontal(5)
                        .PaddingVertical(3)
                        .Text(error.Severity)
                        .FontSize(9)
                        .SemiBold()
                        .FontColor(severityColor);
                }
            });
        });
    }

    // -------------------------------------------------------------------------
    // COMPONENTE REUTILIZABLE — header de sección
    // -------------------------------------------------------------------------

    /// <summary>
    /// Título de sección con borde inferior verde.
    /// Se usa como Action<IContainer> para pasarlo a Element().
    /// Recibe el container directamente en lugar de devolverlo
    /// para evitar problemas de encadenamiento con QuestPDF.
    /// </summary>
    private static void ComposeSectionHeader(IContainer container, string title)
    {
        container
            .PaddingBottom(4)
            .BorderBottom(2)
            .BorderColor(ExcelGreen)
            .Text(title)
            .FontSize(13)
            .SemiBold()
            .FontColor(ExcelGreen);
    }
}

// =============================================================================
// EXTENSIÓN — StatusBadge
// =============================================================================
//
// Extension method sobre IContainer para el badge del header.
// Vive en el mismo archivo porque solo se usa aquí — si en el futuro
// se usa en más lugares, se mueve a su propio archivo de extensiones.
// =============================================================================

public static class ContainerExtensions
{
    /// <summary>
    /// Badge de estado en la esquina superior derecha del header.
    /// Verde oscuro si está limpio, rojo si tiene errores.
    /// </summary>
    public static void StatusBadge(this IContainer container, string text)
    {
        var bgColor = text == "CLEAN" ? "#217346" : "#C0392B";

        container
            .Background(bgColor)
            .PaddingHorizontal(8)
            .PaddingVertical(4)
            .AlignCenter()
            .Text(text)
            .FontSize(8)
            .SemiBold()
            .FontColor(Colors.White);
    }
}