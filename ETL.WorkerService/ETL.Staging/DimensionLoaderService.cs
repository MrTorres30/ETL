using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace ETL.Staging
{
    public class DimensionLoadingSummary
    {
        public int ClientesCargados { get; set; }
        public int ProductosCargados { get; set; }
        public int FuentesCargadas { get; set; }
        public TimeSpan TiempoTotal { get; set; }
        public bool Exitoso { get; set; }
        public string? MensajeError { get; set; }
    }

    public class DimensionLoaderService
    {
        private readonly string _connectionString;
        private readonly string _dataDirectory;

        public DimensionLoaderService(string connectionString, string dataDirectory = "data")
        {
            _connectionString = connectionString;
            _dataDirectory = dataDirectory;
        }

        private string ResolverRuta(string nombreArchivo)
        {
            string ruta = Path.Combine(_dataDirectory, nombreArchivo);
            if (File.Exists(ruta)) return ruta;

            ruta = Path.Combine(AppContext.BaseDirectory, _dataDirectory, nombreArchivo);
            if (File.Exists(ruta)) return ruta;

            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, _dataDirectory, nombreArchivo)))
            {
                dir = dir.Parent;
            }

            return dir != null ? Path.Combine(dir.FullName, _dataDirectory, nombreArchivo) : Path.Combine(_dataDirectory, nombreArchivo);
        }

        public async Task<DimensionLoadingSummary> CargarTodasLasDimensionesAsync(CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            var summary = new DimensionLoadingSummary();

            try
            {
                summary.ClientesCargados = await CargarClientesAsync(cancellationToken);
                summary.ProductosCargados = await CargarProductosAsync(cancellationToken);
                summary.FuentesCargadas = await CargarFuentesAsync(cancellationToken);
                sw.Stop();
                summary.TiempoTotal = sw.Elapsed;
                summary.Exitoso = true;
            }
            catch (Exception ex)
            {
                sw.Stop();
                summary.TiempoTotal = sw.Elapsed;
                summary.Exitoso = false;
                summary.MensajeError = ex.Message;
            }

            return summary;
        }

        public async Task<int> CargarClientesAsync(CancellationToken cancellationToken = default)
        {
            string ruta = ResolverRuta("clients.csv");
            if (!File.Exists(ruta)) return 0;

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            var existentes = new HashSet<int>();
            using (var cmdCheck = new SqlCommand("SELECT Id_Cliente FROM dbo.Clientes", conn))
            using (var reader = await cmdCheck.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    existentes.Add(reader.GetInt32(0));
                }
            }

            int count = 0;
            using var readerCsv = new StreamReader(ruta);
            string? header = await readerCsv.ReadLineAsync(cancellationToken);

            using var trans = conn.BeginTransaction();
            try
            {
                string? linea;
                while ((linea = await readerCsv.ReadLineAsync(cancellationToken)) != null)
                {
                    if (string.IsNullOrWhiteSpace(linea)) continue;

                    var partes = linea.Split(',');
                    if (partes.Length < 3) continue;

                    if (!int.TryParse(partes[0].Trim(), out int idCliente) || idCliente <= 0) continue;
                    string nombre = partes[1].Trim();
                    string email = partes[2].Trim();

                    if (existentes.Contains(idCliente))
                    {
                        using var cmdUp = new SqlCommand(
                            "UPDATE dbo.Clientes SET Nombre = @nom, Email = @email WHERE Id_Cliente = @id", conn, trans);
                        cmdUp.Parameters.AddWithValue("@id", idCliente);
                        cmdUp.Parameters.AddWithValue("@nom", nombre);
                        cmdUp.Parameters.AddWithValue("@email", email);
                        await cmdUp.ExecuteNonQueryAsync(cancellationToken);
                    }
                    else
                    {
                        using var cmdIn = new SqlCommand(
                            "INSERT INTO dbo.Clientes (Id_Cliente, Nombre, Email) VALUES (@id, @nom, @email)", conn, trans);
                        cmdIn.Parameters.AddWithValue("@id", idCliente);
                        cmdIn.Parameters.AddWithValue("@nom", nombre);
                        cmdIn.Parameters.AddWithValue("@email", email);
                        await cmdIn.ExecuteNonQueryAsync(cancellationToken);
                        existentes.Add(idCliente);
                    }
                    count++;
                }

                await trans.CommitAsync(cancellationToken);
            }
            catch
            {
                await trans.RollbackAsync(cancellationToken);
                throw;
            }

            return count;
        }

        public async Task<int> CargarProductosAsync(CancellationToken cancellationToken = default)
        {
            string ruta = ResolverRuta("products.csv");
            if (!File.Exists(ruta)) return 0;

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            var existentes = new HashSet<int>();
            using (var cmdCheck = new SqlCommand("SELECT Id_Producto FROM dbo.Producto", conn))
            using (var reader = await cmdCheck.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    existentes.Add(reader.GetInt32(0));
                }
            }

            int count = 0;
            using var readerCsv = new StreamReader(ruta);
            string? header = await readerCsv.ReadLineAsync(cancellationToken);

            using var trans = conn.BeginTransaction();
            try
            {
                string? linea;
                while ((linea = await readerCsv.ReadLineAsync(cancellationToken)) != null)
                {
                    if (string.IsNullOrWhiteSpace(linea)) continue;

                    var partes = linea.Split(',');
                    if (partes.Length < 3) continue;

                    if (!int.TryParse(partes[0].Trim(), out int idProducto) || idProducto <= 0) continue;
                    string nombre = partes[1].Trim();
                    string categoria = partes[2].Trim();

                    if (existentes.Contains(idProducto))
                    {
                        using var cmdUp = new SqlCommand(
                            "UPDATE dbo.Producto SET Nombre = @nom, Categoria = @cat WHERE Id_Producto = @id", conn, trans);
                        cmdUp.Parameters.AddWithValue("@id", idProducto);
                        cmdUp.Parameters.AddWithValue("@nom", nombre);
                        cmdUp.Parameters.AddWithValue("@cat", categoria);
                        await cmdUp.ExecuteNonQueryAsync(cancellationToken);
                    }
                    else
                    {
                        using var cmdIn = new SqlCommand(
                            "INSERT INTO dbo.Producto (Id_Producto, Nombre, Categoria) VALUES (@id, @nom, @cat)", conn, trans);
                        cmdIn.Parameters.AddWithValue("@id", idProducto);
                        cmdIn.Parameters.AddWithValue("@nom", nombre);
                        cmdIn.Parameters.AddWithValue("@cat", categoria);
                        await cmdIn.ExecuteNonQueryAsync(cancellationToken);
                        existentes.Add(idProducto);
                    }
                    count++;
                }

                await trans.CommitAsync(cancellationToken);
            }
            catch
            {
                await trans.RollbackAsync(cancellationToken);
                throw;
            }

            return count;
        }

        public async Task<int> CargarFuentesAsync(CancellationToken cancellationToken = default)
        {
            string ruta = ResolverRuta("fuente_datos.csv");
            if (!File.Exists(ruta)) return 0;

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            var existentes = new HashSet<int>();
            using (var cmdCheck = new SqlCommand("SELECT Id_Fuente FROM dbo.Fuente_Datos", conn))
            using (var reader = await cmdCheck.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    existentes.Add(reader.GetInt32(0));
                }
            }

            int count = 0;
            using var readerCsv = new StreamReader(ruta);
            string? header = await readerCsv.ReadLineAsync(cancellationToken);

            using var trans = conn.BeginTransaction();
            try
            {
                string? linea;
                while ((linea = await readerCsv.ReadLineAsync(cancellationToken)) != null)
                {
                    if (string.IsNullOrWhiteSpace(linea)) continue;

                    var partes = linea.Split(',');
                    if (partes.Length < 3) continue;

                    var match = Regex.Match(partes[0].Trim(), @"\d+");
                    int idFuente = match.Success && int.TryParse(match.Value, out int id) ? id : 0;
                    if (idFuente <= 0) continue;

                    string tipoFuente = partes[1].Trim();
                    DateTime fechaCarga = DateTime.TryParse(partes[2].Trim(), out var f) ? f : DateTime.Today;

                    if (existentes.Contains(idFuente))
                    {
                        using var cmdUp = new SqlCommand(
                            "UPDATE dbo.Fuente_Datos SET Tipo_Fuente = @tipo, FechaCarga = @fecha WHERE Id_Fuente = @id", conn, trans);
                        cmdUp.Parameters.AddWithValue("@id", idFuente);
                        cmdUp.Parameters.AddWithValue("@tipo", tipoFuente);
                        cmdUp.Parameters.AddWithValue("@fecha", fechaCarga);
                        await cmdUp.ExecuteNonQueryAsync(cancellationToken);
                    }
                    else
                    {
                        using var cmdIn = new SqlCommand(
                            "INSERT INTO dbo.Fuente_Datos (Id_Fuente, Tipo_Fuente, FechaCarga) VALUES (@id, @tipo, @fecha)", conn, trans);
                        cmdIn.Parameters.AddWithValue("@id", idFuente);
                        cmdIn.Parameters.AddWithValue("@tipo", tipoFuente);
                        cmdIn.Parameters.AddWithValue("@fecha", fechaCarga);
                        await cmdIn.ExecuteNonQueryAsync(cancellationToken);
                        existentes.Add(idFuente);
                    }
                    count++;
                }

                await trans.CommitAsync(cancellationToken);
            }
            catch
            {
                await trans.RollbackAsync(cancellationToken);
                throw;
            }

            return count;
        }
    }
}
