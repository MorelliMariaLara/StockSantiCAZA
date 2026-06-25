using System.ComponentModel.DataAnnotations;

namespace StockSantiCaza.Web.Models;

public class Arma
{
    public int Id { get; set; }

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    [Required, MaxLength(80)]
    public string NumeroSerie { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Marca { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Modelo { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string Calibre { get; set; } = string.Empty;

    public TipoArma TipoArma { get; set; }

    public EstadoTramiteAnmac EstadoTramiteAnmac { get; set; } = EstadoTramiteAnmac.PendienteAutorizacion;

    [MaxLength(80)]
    public string? NumeroTenenciaAnmac { get; set; }

    public int? ClienteActualId { get; set; }
    public Cliente? ClienteActual { get; set; }

    public DateTime? FechaTransferencia { get; set; }

    public bool DisponibleParaVenta => ClienteActualId is null
        && EstadoTramiteAnmac is EstadoTramiteAnmac.Autorizado;
}
