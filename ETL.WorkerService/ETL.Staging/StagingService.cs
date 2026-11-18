using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using ETL.Core;
namespace ETL.Staging
{
    /// Servicio de Staging encargado de persistir los datos crudos extraídos
    /// Cumple con el requerimiento de almacenamiento temporal de la práctica.
    public class StagingService
    {
        private readonly string _connectionString;
        private readonly string _tempDirectory;
        public StagingService(string connectionString, string tempDirectory = "temp_staging")
        {
            _connectionString = connectionString;
            _tempDirectory = tempDirectory;
            if (!Directory.Exists(_tempDirectory))
            {
                Directory.CreateDirectory(_tempDirectory);
            }
        }
        public async Task<int> GuardarEncuestasAsync(IEnumerable<SurveyRawRecord> records, CancellationToken cancellationToken = default)
        {
          
            string jsonPath = Path.Combine(_tempDirectory, $"surveys_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
            
            int count = 0;
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            foreach (var r in records)
            {
                string sql = @"INSERT INTO stg_Encuestas_Csv 
                               (IdOpinion, IdCliente, IdProducto, Fecha, Comentario, Clasificacion, PuntajeSatisfaccion, Fuente, FechaExtraccion)
                               VALUES 
                               (@IdOpinion, @IdCliente, @IdProducto, @Fecha, @Comentario, @Clasificacion, @PuntajeSatisfaccion, @Fuente, GETDATE());";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@IdOpinion", r.IdOpinion);
                cmd.Parameters.AddWithValue("@IdCliente", (object?)r.IdCliente ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IdProducto", (object?)r.IdProducto ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Fecha", (object?)r.Fecha ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Comentario", r.Comentario ?? string.Empty);
                cmd.Parameters.AddWithValue("@Clasificacion", r.Clasificacion ?? string.Empty);
                cmd.Parameters.AddWithValue("@PuntajeSatisfaccion", (object?)r.PuntajeSatisfaccion ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Fuente", r.Fuente ?? "CSV");
                await cmd.ExecuteNonQueryAsync(cancellationToken);
                count++;
            }
            return count;
        }
        public async Task<int> GuardarResenasAsync(IEnumerable<WebReviewRawRecord> records, CancellationToken cancellationToken = default)
        {
            // Guardar copia en un archivo temporal JSON
            string jsonPath = Path.Combine(_tempDirectory, $"web_reviews_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
            // 2. Insertar en tabla staging de SQL Server
            int count = 0;
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            foreach (var r in records)
            {
                string sql = @"INSERT INTO stg_Resenas_Database 
                               (IdReview, IdCliente, IdProducto, Fecha, Comentario, Rating, FechaExtraccion)
                               VALUES 
                               (@IdReview, @IdCliente, @IdProducto, @Fecha, @Comentario, @Rating, GETDATE());";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@IdReview", r.IdReview);
                cmd.Parameters.AddWithValue("@IdCliente", (object?)r.IdCliente ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IdProducto", (object?)r.IdProducto ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Fecha", (object?)r.Fecha ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Comentario", r.Comentario ?? string.Empty);
                cmd.Parameters.AddWithValue("@Rating", (object?)r.Rating ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
                count++;
            }
            return count;
        }
        public async Task<int> GuardarComentariosAsync(IEnumerable<SocialCommentRawRecord> records, CancellationToken cancellationToken = default)
        {
            // 1. Guardar copia en archivo temporal JSON
            string jsonPath = Path.Combine(_tempDirectory, $"social_comments_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
            // 2. Insertar en tabla staging de SQL Server
            int count = 0;
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            foreach (var r in records)
            {
                string sql = @"INSERT INTO stg_Comentarios_Api 
                               (IdComment, IdCliente, IdProducto, Fuente, Fecha, Comentario, FechaExtraccion)
                               VALUES 
                               (@IdComment, @IdCliente, @IdProducto, @Fuente, @Fecha, @Comentario, GETDATE());";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@IdComment", r.IdComment);
                cmd.Parameters.AddWithValue("@IdCliente", (object?)r.IdCliente ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IdProducto", (object?)r.IdProducto ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Fuente", r.Fuente ?? string.Empty);
                cmd.Parameters.AddWithValue("@Fecha", (object?)r.Fecha ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Comentario", r.Comentario ?? string.Empty);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
                count++;
            }
            return count;
        }
    }
}