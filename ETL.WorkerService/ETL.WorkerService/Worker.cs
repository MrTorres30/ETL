using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ETL.Core;
using ETL.Staging;

namespace ETL.WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _config;
        private readonly IExtractor<SurveyRawRecord> _csvExtractor;
        private readonly IExtractor<WebReviewRawRecord> _dbExtractor;
        private readonly IExtractor<SocialCommentRawRecord> _apiExtractor;
        private readonly StagingService _stagingService;

        public Worker(
            ILogger<Worker> logger,
            IConfiguration config,
            IExtractor<SurveyRawRecord> csvExtractor,
            IExtractor<WebReviewRawRecord> dbExtractor,
            IExtractor<SocialCommentRawRecord> apiExtractor,
            StagingService stagingService)
        {
            _logger = logger;
            _config = config;
            _csvExtractor = csvExtractor;
            _dbExtractor = dbExtractor;
            _apiExtractor = apiExtractor;
            _stagingService = stagingService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int interval = _config.GetValue<int>("EtlSettings:IntervalSeconds", 60);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("=============================================================");
                _logger.LogInformation(">>> [ETL Worker] Iniciando ciclo de extraccion asincrona: {time}", DateTimeOffset.Now);
                _logger.LogInformation("=============================================================");

                var totalStopwatch = Stopwatch.StartNew();

                try
                {
                    //Ejecucion concurrente y en paralelo 
                    var csvTask = _csvExtractor.ExtractAsync(stoppingToken);
                    var dbTask = _dbExtractor.ExtractAsync(stoppingToken);
                    var apiTask = _apiExtractor.ExtractAsync(stoppingToken);

                    await Task.WhenAll(csvTask, dbTask, apiTask);

                    var csvResult = await csvTask;
                    var dbResult = await dbTask;
                    var apiResult = await apiTask;

                    // 1. Procesar Fuente: CSV Encuestas
                    if (csvResult.IsSuccess)
                    {
                        int inserted = await _stagingService.GuardarEncuestasAsync(csvResult.Data, stoppingToken);
                        _logger.LogInformation("✔️ [{source}] {count} registros extraidos en {ms:F2} ms -> Staging: {inserted} insertados.",
                            csvResult.SourceName, csvResult.RecordsCount, csvResult.ElapsedTime.TotalMilliseconds, inserted);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ [{source}] Fallo en extraccion: {error}", csvResult.SourceName, csvResult.ErrorMessage);
                    }

                    // 2. Procesar Fuente: Base de Datos Reseñas
                    if (dbResult.IsSuccess)
                    {
                        int inserted = await _stagingService.GuardarResenasAsync(dbResult.Data, stoppingToken);
                        _logger.LogInformation("✔️ [{source}] {count} registros extraidos en {ms:F2} ms -> Staging: {inserted} insertados.",
                            dbResult.SourceName, dbResult.RecordsCount, dbResult.ElapsedTime.TotalMilliseconds, inserted);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ [{source}] Fallo en extraccion: {error}", dbResult.SourceName, dbResult.ErrorMessage);
                    }

                    // 3. Procesar Fuente: API REST Comentarios
                    if (apiResult.IsSuccess)
                    {
                        int inserted = await _stagingService.GuardarComentariosAsync(apiResult.Data, stoppingToken);
                        _logger.LogInformation("✔️ [{source}] {count} registros extraidos en {ms:F2} ms -> Staging: {inserted} insertados.",
                            apiResult.SourceName, apiResult.RecordsCount, apiResult.ElapsedTime.TotalMilliseconds, inserted);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ [{source}] Fallo en extraccion: {error}", apiResult.SourceName, apiResult.ErrorMessage);
                    }

                    totalStopwatch.Stop();
                    _logger.LogInformation("-------------------------------------------------------------");
                    _logger.LogInformation("🏁 [ETL Worker] Ciclo completado exitosamente en {ms} ms (Total)", totalStopwatch.ElapsedMilliseconds);
                    _logger.LogInformation("=============================================================\n");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [ETL Worker] Error critico durante la orquestacion de extraccion.");
                }

                _logger.LogInformation("⏳ Proxima extraccion programada en {seconds} segundos...", interval);
                await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken);
            }
        }
    }
}
