using System.Threading;
using System.Threading.Tasks;
namespace ETL.Core
{
    /// Contrato genérico para desacoplar las fuentes de datos (CSV, BD, API REST).
    public interface IExtractor<T>
    {
        string SourceName { get; }
        string SourceType { get; }
        Task<ExtractionResult<T>> ExtractAsync(CancellationToken cancellationToken = default);
    }
}