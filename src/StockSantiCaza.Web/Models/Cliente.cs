using System.ComponentModel.DataAnnotations;

namespace StockSantiCaza.Web.Models;

public class Cliente
{
    public int Id { get; set; }

    [MaxLength(160)]
    public string? NombreRazonSocial { get; set; }

    [MaxLength(20)]
    public string? DniCuit { get; set; }

    [MaxLength(180), EmailAddress]
    public string? Email { get; set; }

    [MaxLength(40)]
    public string? Telefono { get; set; }

    [MaxLength(220)]
    public string? Domicilio { get; set; }

    public bool Activo { get; set; } = true;

    public CredencialCLU? CredencialCLU { get; set; }

    public ICollection<Arma> ArmasRegistradas { get; set; } = new List<Arma>();

    public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
}
