namespace StockSantiCaza.Web.Models;

public enum ProductoCategoria
{
    General = 1,
    Arma = 2,
    Municion = 3
}

public enum TipoArma
{
    Pistola = 1,
    Revolver = 2,
    Escopeta = 3,
    Rifle = 4,
    Carabina = 5,
    AireComprimido = 6,
    Otro = 99
}

public enum EstadoTramiteAnmac
{
    NoAplica = 0,
    PendienteAutorizacion = 1,
    Autorizado = 2,
    Entregado = 3,
    Observado = 4,
    Rechazado = 5
}

public enum TipoMunicion
{
    Cartucho = 1,
    Bala = 2,
    Posta = 3,
    Fulminante = 4,
    Otro = 99
}

public enum TipoComprobante
{
    Presupuesto = 0,
    FacturaA = 1,
    FacturaB = 2,
    FacturaC = 3
}

public enum EstadoVenta
{
    Borrador = 0,
    Confirmada = 1,
    Facturada = 2,
    Anulada = 3
}

public enum TipoMovimientoStock
{
    Ingreso = 1,
    EgresoVenta = 2,
    Ajuste = 3,
    AnulacionVenta = 4
}

public enum TipoMovimientoProveedor
{
    Deuda = 1,
    Pago = 2
}

public enum RolUsuario
{
    Administrador = 1,
    Vendedor = 2
}
