using System.ComponentModel.DataAnnotations;

namespace StockSantiCaza.Web.Models;

public class CategoriaStock
{
    public int Id { get; set; }

    [MaxLength(80)]
    public string Nombre { get; set; } = string.Empty;

    public bool RequiereSerie { get; set; }

    public bool RequiereLote { get; set; }

    public bool Activo { get; set; } = true;
}
