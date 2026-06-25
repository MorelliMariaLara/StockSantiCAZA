using System.ComponentModel.DataAnnotations;

namespace StockSantiCaza.Web.Models;

public class Usuario
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [Required, MaxLength(60)]
    public string Login { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    public RolUsuario Rol { get; set; } = RolUsuario.Vendedor;

    public bool Activo { get; set; } = true;

    public ICollection<Venta> VentasRealizadas { get; set; } = new List<Venta>();
}
