using System;
using System.Collections.Generic;
namespace ETL.Core
{
    /// Encapsula el resultado de una extracción, incluyendo métricas para ILogger y Stopwatch.
    public class ExtractionResult<T>
    {
        public string SourceName { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public IReadOnlyList<T> Data { get; set; } = new List<T>();
        public int RecordsCount => Data.Count;
        public TimeSpan ElapsedTime { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public static ExtractionResult<T> Success(string sourceName, string sourceType, IReadOnlyList<T> data, TimeSpan elapsed)
        {
            return new ExtractionResult<T>
            {
                SourceName = sourceName,
                SourceType = sourceType,
                Data = data,
                ElapsedTime = elapsed,
                IsSuccess = true
            };
        }
        public static ExtractionResult<T> Failure(string sourceName, string sourceType, string error, TimeSpan elapsed)
        {
            return new ExtractionResult<T>
            {
                SourceName = sourceName,
                SourceType = sourceType,
                Data = new List<T>(),
                ElapsedTime = elapsed,
                IsSuccess = false,
                ErrorMessage = error
            };
        }
    }
}