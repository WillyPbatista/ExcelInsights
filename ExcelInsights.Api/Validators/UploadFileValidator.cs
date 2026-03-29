using ExcelInsights.Application.Common;

namespace ExcelInsights.Api.Validators
{
    public class UploadFileValidator
    {
        private readonly ExcelInsightsSettings _settings;

        public UploadFileValidator(ExcelInsightsSettings settings)
        {
            _settings = settings;
        }

        public bool Validate(IFormFile file, out string errorMessage)
        {
            if (file == null || file.Length == 0)
            {
                errorMessage = "No se ha proporcionado ningún archivo.";
                return false;
            }

            if (file.Length > _settings.MaxFileSizeBytes)
            {
                errorMessage = $"El archivo excede el tamaño máximo permitido de {_settings.MaxFileSizeBytes} bytes.";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}