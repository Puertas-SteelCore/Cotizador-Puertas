using System.Globalization;

namespace CotizadorPuertas.Web.Services;

public static class Fmt
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Dinero(decimal n) => "$ " + n.ToString("#,##0.00", Inv);
    public static string Dinero(decimal n, string moneda) => Dinero(n) + " " + moneda;
    public static string Corto(decimal n) =>
        Math.Abs(n) >= 1_000_000 ? "$ " + (n / 1_000_000m).ToString("0.##", Inv) + " M"
        : Math.Abs(n) >= 1_000 ? "$ " + (n / 1_000m).ToString("0.#", Inv) + " k"
        : Dinero(n);
    public static string Num(decimal n) => n == Math.Round(n) ? n.ToString("0", Inv) : n.ToString("0.##", Inv);
    public static string M(decimal n) => n.ToString("0.00", Inv);
    public static string Pct(decimal n) => (n * 100).ToString("0.##", Inv) + "%";
    public static string F(double n) => n.ToString("0.##", Inv);
    public static string ClaseEstado(string e) => e switch { "Ganada" => "ganada", "Perdida" => "perdida", _ => "en-proceso" };
}
