using System;
namespace ETL.Core
{
    public class SocialCommentRawRecord
    {
        public string IdComment { get; set; } = string.Empty;
        public string? IdCliente { get; set; }
        public string? IdProducto { get; set; }
        public string Fuente { get; set; } = string.Empty;
        public DateTime? Fecha { get; set; }
        public string Comentario { get; set; } = string.Empty;
    }
}