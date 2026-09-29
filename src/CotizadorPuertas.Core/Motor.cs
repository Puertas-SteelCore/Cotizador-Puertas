namespace CotizadorPuertas.Core;

public static class Motor
{
    public const decimal HolguraAncho = 0.07m;
    public const decimal HolguraAlto = 0.04m;

    public static Totales Calcular(DatosCotizacion d)
    {
        var material = d.Partidas.Sum(p => p.Total);
        var puertas = d.Partidas.Sum(p => p.Cantidad);

        var descuentos = d.Descuentos
            .Where(x => x.Porcentaje != 0)
            .Select(x => new LineaDescuento(x.Nombre, x.Porcentaje, material * x.Porcentaje / 100m))
            .ToList();
        var descTotal = descuentos.Sum(x => x.Monto);

        var extras = d.Extras.Sum(x => x.Monto);
        var subtotal = material - descTotal + d.Instalacion + d.Flete + d.Viaticos + extras;
        var iva = subtotal * d.IvaPct;
        var total = subtotal + iva;
        var anticipo = total * d.AnticipoPct;

        return new Totales(puertas, material, descuentos, descTotal, d.Instalacion, d.Flete, d.Viaticos,
            extras, subtotal, iva, total, anticipo, total - anticipo);
    }

    public static TarifaPuerta? TarifaPara(IEnumerable<TarifaPuerta> tarifas, decimal hojaAncho, decimal hojaAlto)
    {
        var a = Math.Round(hojaAncho, 2);
        var h = Math.Round(hojaAlto, 2);
        return tarifas
            .OrderBy(t => t.AnchoHasta).ThenBy(t => t.AltoHasta)
            .FirstOrDefault(t => a <= t.AnchoHasta && h <= t.AltoHasta);
    }

    public static decimal? PrecioPuerta(IEnumerable<TarifaPuerta> tarifas, decimal hojaAncho, decimal hojaAlto, string moneda)
        => TarifaPara(tarifas, hojaAncho, hojaAlto)?.Precio(moneda);

    public static string Medida(decimal m) => m.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
}
