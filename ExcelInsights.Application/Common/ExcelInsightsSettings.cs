namespace ExcelInsights.Application.Common
{
    public class ExcelInsightsSettings
    {
        public int MaxFileSizeBytes { get; set; }

        public int MaxColumns { get; set; }

        public int MaxRows { get; set; }

        public double InferenceConfidenceThreshold { get; set; }
    }
}