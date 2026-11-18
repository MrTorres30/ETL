using System;
namespace ETL.Core
{
    public class WebReviewRawRecord
    {
        public string IdReview { get; set; } = string.Empty;
        public string? IdCliente { get; set; }
        public string? IdProducto { get; set; }
        public DateTime? Fecha { get; set; }
        public string Comentario { get; set; } = string.Empty;
        public int? Rating { get; set; }
    }
}