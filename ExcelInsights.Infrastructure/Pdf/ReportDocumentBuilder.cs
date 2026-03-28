using ExcelInsights.Application.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ExcelInsights.Infrastructure.Pdf
{
    public class ReportDocumentBuilder : IDocument
    {
        private readonly AnalysisResult _result;

        private readonly string ExcelGreen = "#217346";
        private readonly string LightGreen = "#EBF1DE";
        private readonly string DarkGrey = "#333333";

        public ReportDocumentBuilder(AnalysisResult result)
        {
            _result = result;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(DarkGrey));

                page.Header().Element(ComposeHeader);

                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Spacing(20);
                    
                    BuildStatisticsSection(column);
                    BuildErrorsSection(column);
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                });
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Excel Analysis Report")
                        .FontSize(24)
                        .SemiBold()
                        .FontColor(ExcelGreen);

                    column.Item().Text($"Generated on: {DateTime.Now:MMMM dd, yyyy}")
                        .FontSize(10)
                        .Italic();
                });

                row.ConstantItem(100).AlignRight().StatusBadge("COMPLETED");
            });
        }

        private void BuildStatisticsSection(ColumnDescriptor column)
        {
            column.Item().Column(innerColumn =>
            {
                innerColumn.Item().Element(SectionHeader("Summary Statistics"));

                innerColumn.Item().Background(LightGreen).Padding(10).Row(row =>
                {

                    row.RelativeItem().Column(c => {
                        c.Item().Text("Total Rows").Bold();
                        c.Item().Text("1,250"); 
                    });
                    
                    row.RelativeItem().Column(c => {
                        c.Item().Text("Processed").Bold();
                        c.Item().Text("1,248"); 
                    });
                });
            });
        }

        private void BuildErrorsSection(ColumnDescriptor column)
        {
            column.Item().Column(innerColumn =>
            {
                innerColumn.Item().Element(SectionHeader("Analysis Findings & Errors"));

                innerColumn.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("ID");
                        header.Cell().Element(CellStyle).Text("Location");
                        header.Cell().Element(CellStyle).Text("Description");

                        IContainer CellStyle(IContainer container) => 
                            container.DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White))
                                     .Background(ExcelGreen)
                                     .PaddingHorizontal(5)
                                     .PaddingVertical(2);
                    });


                    table.Cell().Element(BodyStyle).Text("1");
                    table.Cell().Element(BodyStyle).Text("Sheet1!A12");
                    table.Cell().Element(BodyStyle).Text("Invalid data format detected.");

                    IContainer BodyStyle(IContainer container) => 
                        container.BorderBottom(1)
                                 .BorderColor(Colors.Grey.Lighten2)
                                 .PaddingHorizontal(5)
                                 .PaddingVertical(2);
                });
            });
        }

        private Action<IContainer> SectionHeader(string title)
        {
            return container => container
                .PaddingBottom(5)
                .BorderBottom(2)
                .BorderColor(ExcelGreen)
                .Text(title)
                .FontSize(14)
                .SemiBold()
                .FontColor(ExcelGreen);
        }
    }

    public static class ContainerExtensions
    {
        public static void StatusBadge(this IContainer container, string text)
        {
            container
                .Background("#217346")
                .PaddingHorizontal(10)
                .PaddingVertical(5)
                .AlignCenter()
                .Text(text)
                .FontSize(10)
                .FontColor(Colors.White)
                .SemiBold();
        }
    }
}