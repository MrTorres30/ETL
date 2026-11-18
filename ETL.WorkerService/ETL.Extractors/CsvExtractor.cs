using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using ETL.Core;
namespace ETL.Extractors
{

    public class CsvExtractor : IExtractor<SurveyRawRecord>
    {
        private readonly string _filePath;
        public string SourceName => "Encuestas Internas (CSV)";
        public string SourceType => "CSV";
        public CsvExtractor(string filePath)
        {
            _filePath = filePath;
        }
        public async Task<ExtractionResult<SurveyRawRecord>> ExtractAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var records = new List<SurveyRawRecord>();
            try
            {
                if (!File.Exists(_filePath))
                {
                    stopwatch.Stop();
                    return ExtractionResult<SurveyRawRecord>.Failure(
                        SourceName, SourceType, $"El archivo CSV no existe en la ruta: {_filePath}", stopwatch.Elapsed);
                }
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    Delimiter = ",",
                    MissingFieldFound = null,
                    BadDataFound = null
                };
                using var reader = new StreamReader(_filePath);
                using var csv = new CsvReader(reader, config);
                await foreach (var record in csv.GetRecordsAsync<SurveyRawRecord>(cancellationToken))
                {
                    records.Add(record);
                }
                stopwatch.Stop();
                return ExtractionResult<SurveyRawRecord>.Success(SourceName, SourceType, records, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ExtractionResult<SurveyRawRecord>.Failure(SourceName, SourceType, ex.Message, stopwatch.Elapsed);
            }
        }
    }
}