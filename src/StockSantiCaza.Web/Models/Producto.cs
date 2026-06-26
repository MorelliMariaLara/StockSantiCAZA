using System.ComponentModel.DataAnnotations;

namespace StockSantiCaza.Web.Models;

public class Producto
{
    public int Id { get; set; }

    [MaxLength(40)]
    public string? Sku { get; set; }

    [MaxLength(180)]
    public string? Nombre { get; set; }

    [MaxLength(800)]
    public string? Descripcion { get; set; }

    [MaxLength(80)]
    public string? Categoria { get; set; }

    [MaxLength(80)]
    public string? Marca { get; set; }

    [MaxLength(80)]
    public string? Modelo { get; set; }

    [MaxLength(40)]
    public string? Calibre { get; set; }

    public decimal PrecioUnitario { get; set; }

    public int StockActual { get; set; }

    public int StockMinimo { get; set; } = 1;

    public bool Activo { get; set; } = true;

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public ICollection<Arma> Armas { get; set; } = new List<Arma>();

    public ICollection<MunicionLote> LotesMunicion { get; set; } = new List<MunicionLote>();
}
