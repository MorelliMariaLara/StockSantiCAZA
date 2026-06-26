using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockSantiCaza.Web.Models;

public class CredencialCLU
{
    public int Id { get; set; }

    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    [MaxLength(80)]
    public string? NumeroLegajo { get; set; }

    public DateOnly? FechaEmision { get; set; }

    public DateOnly? FechaVencimiento { get; set; }

    [NotMapped]
    public bool EstaVigente => FechaVencimiento.HasValue
        && FechaVencimiento.Value >= DateOnly.FromDateTime(DateTime.Today);
}
