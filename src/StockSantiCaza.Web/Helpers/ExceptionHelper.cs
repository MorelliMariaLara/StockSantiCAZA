namespace StockSantiCaza.Web.Helpers;

public static class ExceptionHelper
{
    public static string ObtenerMensaje(Exception ex)
    {
        var actual = ex;
        while (actual.InnerException is not null)
        {
            actual = actual.InnerException;
        }

        return actual.Message;
    }
}
