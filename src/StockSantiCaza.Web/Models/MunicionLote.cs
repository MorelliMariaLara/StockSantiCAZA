using System.ComponentModel.DataAnnotations;

namespace StockSantiCaza.Web.Models;

public class MunicionLote
{
    public int Id { get; set; }

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    [Required, MaxLength(80)]
    public string NumeroLote { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string Calibre { get; set; } = string.Empty;

    public TipoMunicion TipoMunicion { get; set; }

    public int CantidadDisponible { get; set; }

    public DateTime FechaIngreso { get; set; } = DateTime.UtcNow;

    public DateTime? FechaVencimiento { get; set; }
}
