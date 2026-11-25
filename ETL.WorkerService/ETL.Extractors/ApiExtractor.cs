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
            }
            catch (Exception)
            {
            }

            var mockData = new List<SocialCommentRawRecord>
            {
                new() { IdComment = "API_001", IdCliente = "C019", IdProducto = "P003", Fuente = "Instagram API", Fecha = DateTime.Now, Comentario = "Excelente producto recibido via API REST." },
                new() { IdComment = "API_002", IdCliente = "C021", IdProducto = "P006", Fuente = "Twitter API", Fecha = DateTime.Now, Comentario = "Buena atencion al cliente reportada desde red social." },
                new() { IdComment = "API_003", IdCliente = "C035", IdProducto = "P008", Fuente = "Facebook Graph API", Fecha = DateTime.Now, Comentario = "Relacion calidad-precio adecuada segun comentarios API." }
            };

            stopwatch.Stop();
            return ExtractionResult<SocialCommentRawRecord>.Success(
                SourceName, $"{SourceType} (Simulado/Fallback)", mockData, stopwatch.Elapsed);
        }
    }
}