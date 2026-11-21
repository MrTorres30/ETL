using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using ETL.Core;
namespace ETL.Extractors
{
    
    public class DatabaseExtractor : IExtractor<WebReviewRawRecord>
    {
        private readonly string _connectionString;
        private readonly string _tableName;
        public string SourceName => "Reseñas Web (SQL Server)";
        public string SourceType => "Database";
        public DatabaseExtractor(string connectionString, string tableName = "Fuente_Resenas_Web")
        {
            _connectionString = connectionString;
            _tableName = tableName;
        }
        public async Task<ExtractionResult<WebReviewRawRecord>> ExtractAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var records = new List<WebReviewRawRecord>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                string query = $"SELECT IdReview, IdCliente, IdProducto, Fecha, Comentario, Rating FROM {_tableName}";
                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    records.Add(new WebReviewRawRecord
                    {
                        IdReview = Convert.ToString(reader["IdReview"]) ?? string.Empty,
                        IdCliente = reader.IsDBNull(reader.GetOrdinal("IdCliente")) ? null : Convert.ToString(reader["IdCliente"]),
                        IdProducto = reader.IsDBNull(reader.GetOrdinal("IdProducto")) ? null : Convert.ToString(reader["IdProducto"]),
                        Fecha = reader.IsDBNull(reader.GetOrdinal("Fecha")) ? null : Convert.ToDateTime(reader["Fecha"]),
                        Comentario = reader.IsDBNull(reader.GetOrdinal("Comentario")) ? string.Empty : Convert.ToString(reader["Comentario"]) ?? string.Empty,
                        Rating = reader.IsDBNull(reader.GetOrdinal("Rating")) ? null : Convert.ToInt32(reader["Rating"])
                    });
                }
                stopwatch.Stop();
                return ExtractionResult<WebReviewRawRecord>.Success(SourceName, SourceType, records, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ExtractionResult<WebReviewRawRecord>.Failure(SourceName, SourceType, ex.Message, stopwatch.Elapsed);
            }
        }
    }
}