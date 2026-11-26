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
        private readonly DimensionLoaderService _dimensionLoader;

        public Worker(
            ILogger<Worker> logger,
            IConfiguration config,
            IExtractor<SurveyRawRecord> csvExtractor,
            IExtractor<WebReviewRawRecord> dbExtractor,
            IExtractor<SocialCommentRawRecord> apiExtractor,
            StagingService stagingService,
            DimensionLoaderService dimensionLoader)
        {
            _logger = logger;
            _config = config;
            _csvExtractor = csvExtractor;
            _dbExtractor = dbExtractor;
            _apiExtractor = apiExtractor;
            _stagingService = stagingService;
            _dimensionLoader = dimensionLoader;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int interval = _config.GetValue<int>("EtlSettings:IntervalSeconds", 60);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("=============================================================");
                _logger.LogInformation(">>> [ETL Worker] Iniciando ciclo de carga dimensional y extracción: {time}", DateTimeOffset.Now);
                _logger.LogInformation("=============================================================");

                var totalStopwatch = Stopwatch.StartNew();

                try
                {
                    _logger.LogInformation("--- [DataLoader] Verificando y cargando dimensiones maestras en Data Warehouse ---");
                    var dimSummary = await _dimensionLoader.CargarTodasLasDimensionesAsync(stoppingToken);
                    if (dimSummary.Exitoso)
                    {
                        _logger.LogInformation("[DataLoader] Dimensiones cargadas exitosamente en {ms:F2} ms:", dimSummary.TiempoTotal.TotalMilliseconds);
                        _logger.LogInformation("   -> Dimensión Clientes (dbo.Clientes): {count} filas procesadas/aseguradas.", dimSummary.ClientesCargados);
                        _logger.LogInformation("   -> Dimensión Producto (dbo.Producto): {count} filas procesadas/aseguradas.", dimSummary.ProductosCargados);
                        _logger.LogInformation("   -> Dimensión Fuente_Datos (dbo.Fuente_Datos): {count} filas procesadas/aseguradas.", dimSummary.FuentesCargadas);
                    }
                    else
                    {
                        _logger.LogWarning("[DataLoader] Advertencia en carga de dimensiones: {error}", dimSummary.MensajeError);
                    }

                    _logger.LogInformation("--- [Extracción] Ejecución asíncrona concurrente de fuentes de hechos ---");
                    var csvTask = _csvExtractor.ExtractAsync(stoppingToken);
                    var dbTask = _dbExtractor.ExtractAsync(stoppingToken);
                    var apiTask = _apiExtractor.ExtractAsync(stoppingToken);

                    await Task.WhenAll(csvTask, dbTask, apiTask);

                    var csvResult = await csvTask;
                    var dbResult = await dbTask;
                    var apiResult = await apiTask;

                    if (csvResult.IsSuccess)
                    {
                        int inserted = await _stagingService.GuardarEncuestasAsync(csvResult.Data, stoppingToken);
                        _logger.LogInformation(" [{source}] {count} registros extraidos en {ms:F2} ms -> Staging: {inserted} insertados.",
                            csvResult.SourceName, csvResult.RecordsCount, csvResult.ElapsedTime.TotalMilliseconds, inserted);
                    }
                    else
                    {
                        _logger.LogWarning(" [{source}] Fallo en extraccion: {error}", csvResult.SourceName, csvResult.ErrorMessage);
                    }

                    if (dbResult.IsSuccess)
                    {
                        int inserted = await _stagingService.GuardarResenasAsync(dbResult.Data, stoppingToken);
                        _logger.LogInformation(" [{source}] {count} registros extraidos en {ms:F2} ms -> Staging: {inserted} insertados.",
                            dbResult.SourceName, dbResult.RecordsCount, dbResult.ElapsedTime.TotalMilliseconds, inserted);
                    }
                    else
                    {
                        _logger.LogWarning(" [{source}] Fallo en extraccion: {error}", dbResult.SourceName, dbResult.ErrorMessage);
                    }

                    if (apiResult.IsSuccess)
                    {
                        int inserted = await _stagingService.GuardarComentariosAsync(apiResult.Data, stoppingToken);
                        _logger.LogInformation(" [{source}] {count} registros extraidos en {ms:F2} ms -> Staging: {inserted} insertados.",
                            apiResult.SourceName, apiResult.RecordsCount, apiResult.ElapsedTime.TotalMilliseconds, inserted);
                    }
                    else
                    {
                        _logger.LogWarning(" [{source}] Fallo en extraccion: {error}", apiResult.SourceName, apiResult.ErrorMessage);
                    }

                    totalStopwatch.Stop();
                    _logger.LogInformation("-------------------------------------------------------------");
                    _logger.LogInformation(" [ETL Worker] Ciclo completado exitosamente en {ms} ms (Total)", totalStopwatch.ElapsedMilliseconds);
                    _logger.LogInformation("=============================================================\n");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, " [ETL Worker] Error critico durante la orquestacion de extraccion.");
                }

                _logger.LogInformation(" Proxima extraccion programada en {seconds} segundos...", interval);
                await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken);
            }
        }
    }
}
