using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using ETL.Core;
namespace ETL.Extractors
{
    /// Extractor para comentarios de redes sociales mediante consumo de una API REST.
    /// Utiliza HttpClient para llamadas asíncronas seguras con manejo de excepciones y timeout.
    public class ApiExtractor : IExtractor<SocialCommentRawRecord>
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpointUrl;
        public string SourceName => "Comentarios Redes Sociales (API REST)";
        public string SourceType => "REST API";
        public ApiExtractor(HttpClient httpClient, string endpointUrl)
        {
            _httpClient = httpClient;
            _endpointUrl = endpointUrl;
        }
        public async Task<ExtractionResult<SocialCommentRawRecord>> ExtractAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // Si la URL es válida y alcanzable, consumimos el endpoint REST
                if (Uri.TryCreate(_endpointUrl, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(_endpointUrl))
                {
                    var response = await _httpClient.GetAsync(uri, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        var data = await response.Content.ReadFromJsonAsync<List<SocialCommentRawRecord>>(cancellationToken: cancellationToken);
                        stopwatch.Stop();
                        return ExtractionResult<SocialCommentRawRecord>.Success(
                            SourceName, SourceType, data ?? new List<SocialCommentRawRecord>(), stopwatch.Elapsed);
                    }
                }
                //Agg un Fallback para pruebas locales en caso de trabajar offline
                stopwatch.Stop();
                return ExtractionResult<SocialCommentRawRecord>.Failure(
                    SourceName, SourceType, $"No se pudo conectar a la API en: {_endpointUrl}", stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ExtractionResult<SocialCommentRawRecord>.Failure(SourceName, SourceType, ex.Message, stopwatch.Elapsed);
            }
        }
    }
}