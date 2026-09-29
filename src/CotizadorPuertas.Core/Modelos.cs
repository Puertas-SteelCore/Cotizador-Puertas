namespace CotizadorPuertas.Core;

public static class Categorias
{
    public const string Puerta = "Puerta";
    public const string Antepecho = "Antepecho";
    public const string Marco = "Marco";
    public const string Bisagra = "Bisagra";
    public const string Cerradura = "Cerradura";
    public const string Tope = "Tope";
    public const string Pasador = "Pasador";
    public const string Ventanilla = "Ventanilla";
    public const string Rejilla = "Rejilla";
    public const string Herraje = "Herraje";
    public const string Otro = "Otro";

    public static readonly string[] Catalogo =
        { Marco, Bisagra, Cerradura, Tope, Pasador, Ventanilla, Rejilla, Herraje };

    public static readonly string[] Todas =
        { Puerta, Antepecho, Marco, Bisagra, Cerradura, Tope, Pasador, Ventanilla, Rejilla, Herraje, Otro };
}

public static class TiposPuerta
{
    public const string Sencilla = "Sencilla";
    public const string Doble = "Doble";
}

public static class Monedas
{
    public const string MXN = "MXN";
    public const string USD = "USD";
}

public class Insumo
{
    public string Categoria { get; set; } = Categorias.Otro;
    public long? ProductoId { get; set; }
    public string Codigo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public decimal Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
    public bool PrecioManual { get; set; }

    public decimal Total => Cantidad * PrecioUnitario;
}

public class Partida
{
    public string Clave { get; set; } = "";
    public string Ubicacion { get; set; } = "";
    public string Tipo { get; set; } = TiposPuerta.Sencilla;
    public int Cantidad { get; set; } = 1;
    public decimal VanoAncho { get; set; } = 0.90m;
    public decimal VanoAlto { get; set; } = 2.20m;
    public List<Insumo> Insumos { get; set; } = new();

    public int Hojas => Tipo == TiposPuerta.Doble ? 2 : 1;
    public decimal HojaAncho => Math.Round(Math.Max(0, VanoAncho - Motor.HolguraAncho) / Hojas, 3);
    public decimal HojaAlto => Math.Round(Math.Max(0, VanoAlto - Motor.HolguraAlto), 3);
    public decimal PrecioUnitario => Insumos.Sum(i => i.Total);
    public decimal Total => PrecioUnitario * Cantidad;
}

public class Cargo
{
    public string Nombre { get; set; } = "";
    public decimal Monto { get; set; }
}

public class Descuento
{
    public string Nombre { get; set; } = "Descuento";
    public decimal Porcentaje { get; set; }
}

public class TarifaPuerta
{
    public decimal AnchoHasta { get; set; }
    public decimal AltoHasta { get; set; }
    public decimal PrecioMxn { get; set; }
    public decimal PrecioUsd { get; set; }

    public decimal Precio(string moneda) => moneda == Monedas.USD ? PrecioUsd : PrecioMxn;
}

public class DatosCotizacion
{
    public List<Partida> Partidas { get; set; } = new();
    public List<Descuento> Descuentos { get; set; } = new();
    public List<Cargo> Extras { get; set; } = new();
    public decimal Instalacion { get; set; }
    public decimal Flete { get; set; }
    public decimal Viaticos { get; set; }
    public decimal IvaPct { get; set; } = 0.16m;
    public decimal AnticipoPct { get; set; } = 0.60m;
}

public record LineaDescuento(string Nombre, decimal Porcentaje, decimal Monto);

public record Totales(
    int PuertasTotales,
    decimal Material,
    IReadOnlyList<LineaDescuento> Descuentos,
    decimal DescuentoTotal,
    decimal Instalacion,
    decimal Flete,
    decimal Viaticos,
    decimal Extras,
    decimal Subtotal,
    decimal Iva,
    decimal Total,
    decimal Anticipo,
    decimal Saldo);
