# Excel Insights API

A .NET 10 REST API that analyzes any Excel file without prior knowledge of its structure. Upload a spreadsheet and get back automatic column type inference, row-by-row validation, per-column statistics, and a downloadable PDF report — all in a single request.

---

## What it does

The API knows nothing about your Excel file before reading it. It figures everything out at runtime:

1. **Parses** the file and extracts all rows and columns as raw strings
2. **Infers** the data type of each column using a voting system (Email, Integer, Decimal, Date, Boolean, FreeText)
3. **Validates** every row against rules specific to each inferred type
4. **Calculates** statistics per column (average, min, max, null count, unique count)
5. **Returns** a structured JSON response or a downloadable PDF report

### Example

Given this Excel:

| Email | Age | Salary |
|---|---|---|
| alice@company.com | 28 | 55000 |
| carlos@ruiz | -5 | 48000 |
| diana@company.com | 30 | -9000 |

The API returns:

```json
{
  "fileName": "employees.xlsx",
  "totalRows": 3,
  "validRows": 1,
  "invalidRows": 2,
  "columns": [
    { "name": "Email",  "inferredType": "Email",   "confidence": 0.67, "stats": { "nullCount": 0, "uniqueCount": 3 } },
    { "name": "Age",    "inferredType": "Integer",  "confidence": 1.0,  "stats": { "average": 17.67, "min": -5, "max": 30 } },
    { "name": "Salary", "inferredType": "Integer",  "confidence": 1.0,  "stats": { "average": 31333, "min": -9000, "max": 55000 } }
  ],
  "errors": [
    { "rowIndex": 3, "columnName": "Email",  "message": "Invalid email format: carlos@ruiz", "severity": "Error" },
    { "rowIndex": 3, "columnName": "Age",    "message": "Negative number found: -5",          "severity": "Error" },
    { "rowIndex": 4, "columnName": "Salary", "message": "Negative number found: -9000",        "severity": "Error" }
  ]
}
```

---

## Endpoints

### `POST /api/excel/analyze`

Analyzes an Excel file and returns a JSON response with full insights.

**Request:** `multipart/form-data` with a `file` field containing the `.xlsx` or `.xls` file.

**Response:** `200 OK` with an `AnalysisResult` JSON object.

```bash
curl -X POST https://localhost:7001/api/excel/analyze \
  -F "file=@employees.xlsx"
```

---

### `POST /api/excel/report`

Performs the same analysis and returns a downloadable PDF report with three sections: summary, column statistics table, and detected errors table.

**Request:** `multipart/form-data` with a `file` field.

**Response:** `200 OK` with `Content-Type: application/pdf`. The file downloads as `report_{filename}.pdf`.

```bash
curl -X POST https://localhost:7001/api/excel/report \
  -F "file=@employees.xlsx" \
  --output report.pdf
```

---

### Error responses

All errors follow a consistent JSON structure:

```json
{ "message": "Description of what went wrong" }
```

| Status | When |
|---|---|
| `400` | Invalid file: wrong extension, empty file, or corrupted content |
| `422` | File exceeds row limit |
| `500` | Unexpected server error |

---

## Architecture

Built with **Clean Architecture** across four projects:

```
ExcelInsights.Domain          → Entities, Value Objects, Enums, StatisticsService
ExcelInsights.Application     → Use cases, Contracts (interfaces), DTOs, MediatR handlers
ExcelInsights.Infrastructure  → ClosedXML parser, validation rules, QuestPDF generator
ExcelInsights.Api             → Minimal API endpoints, middleware, configuration
```

**Dependency rule:** Domain knows nothing. Application knows Domain. Infrastructure knows Application and Domain. Api knows everyone — it's the composition root.

### Key design decisions

**Type inference uses a voting system.** For each column, every non-empty value votes for all types it matches. The type with the most votes wins. Ties are broken by a specificity hierarchy: `Boolean > Integer > Decimal > Date > Email > FreeText`. A column needs at least 60% confidence to be assigned a specific type — otherwise it falls back to FreeText.

**Validation rules are independent.** Each rule (`NegativeNumberRule`, `InvalidEmailRule`, `EmptyCellRule`, `InvalidDateRule`) decides internally whether it applies to a given column type. The `ValidationEngine` passes every cell to every rule — it doesn't need to know which rule applies where. Adding a new rule means creating one file and registering it in DI, without touching the engine.

**The orchestrator eliminates duplication.** `ExcelAnalysisOrchestrator` encapsulates the five shared steps (parse → infer → validate → stats → build result). Both handlers (`AnalyzeExcelHandler` and `GenerateReportHandler`) delegate to it, keeping each handler to two lines of logic.

---

## Tech stack

| Component | Library |
|---|---|
| Framework | .NET 10, ASP.NET Core Minimal APIs |
| Excel parsing | ClosedXML |
| PDF generation | QuestPDF |
| Mediator pattern | MediatR |
| Testing | xUnit + FluentAssertions |

---

## Getting started

### Prerequisites

- .NET 10 SDK
- Any `.xlsx` or `.xls` file to test with

### Run locally

```bash
git clone https://github.com/your-username/excel-insights-api
cd excel-insights-api
dotnet run --project ExcelInsights.Api
```

The API starts at `https://localhost:7001`. Open `https://localhost:7001/swagger` to explore the endpoints interactively.

### Run tests

```bash
dotnet test
```

The test suite has 66 tests covering every layer in isolation:

| Test class | Tests | Covers |
|---|---|---|
| `ClosedXmlExcelParserTests` | 4 | Parser edge cases |
| `ColumnInferrerServiceTests` | 12 | Type inference and hierarchy |
| `ValidationRulesTests` | 16 | Each rule in isolation |
| `ValidationEngineTests` | 7 | Engine orchestration |
| `StatisticsServiceTests` | 10 | Numeric calculations |
| `PdfGeneratorTests` | 8 | PDF validity |
| `UploadFileValidatorTests` | 6 | File validation |
| `ExcelParserErrorHandlingTests` | 3 | Corrupted file handling |

### Configuration

All limits are configurable in `appsettings.json`:

```json
{
  "ExcelInsights": {
    "MaxFileSizeMb": 10,
    "MaxRows": 50000,
    "MinConfidenceThreshold": 0.6
  }
}
```

---

## Validation rules

| Rule | Applies to | Severity | Detects |
|---|---|---|---|
| `NegativeNumberRule` | Integer, Decimal | Error | Values below zero |
| `InvalidEmailRule` | Email | Error | Malformed email addresses |
| `EmptyCellRule` | Any | Warning | Empty or whitespace-only cells |
| `InvalidDateRule` | Date | Error | Values that cannot be parsed as a date |

---

## Project structure

```
ExcelInsights/
├── ExcelInsights.Domain/
│   ├── Entities/          ExcelFile, ColumnDefinition, RowData
│   ├── ValueObjects/      ValidationError, InferredType, ColumnStats
│   ├── Enums/             DataType, ErrorSeverity
│   └── Services/          StatisticsService
│
├── ExcelInsights.Application/
│   ├── Features/
│   │   ├── Analyze/       AnalyzeExcelCommand, AnalyzeExcelHandler
│   │   └── GenerateReport/ GenerateReportCommand, GenerateReportHandler
│   ├── Contracts/         IExcelParser, IColumnInferrer, IValidationEngine, IPdfGenerator
│   ├── DTOs/              AnalysisResult, ColumnSummary, RowErrorDto, ColumnStatsDto
│   └── Common/            ExcelAnalysisOrchestrator, ExcelInsightsSettings, Result<T>
│
├── ExcelInsights.Infrastructure/
│   ├── Excel/
│   │   ├── Rules/         IValidationRule, NegativeNumberRule, InvalidEmailRule, ...
│   │   ├── ClosedXmlExcelParser.cs
│   │   ├── ColumnInferrerService.cs
│   │   └── ValidationEngine.cs
│   └── Pdf/
│       ├── QuestPdfGenerator.cs
│       └── ReportDocumentBuilder.cs
│
├── ExcelInsights.Api/
│   ├── Endpoints/         ExcelEndpoints
│   ├── Middlewares/       GlobalExceptionMiddleware
│   ├── Validators/        UploadFileValidator
│   └── Program.cs
│
└── ExcelInsights.Tests/
    ├── Rules/             ValidationRulesTests
    └── ...                (one test class per service)
```

---

## License

MIT
