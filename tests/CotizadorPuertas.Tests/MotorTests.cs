using CotizadorPuertas.Core;

namespace CotizadorPuertas.Tests;

public class MotorTests
{
    static Insumo I(decimal cant, decimal precio, string cat = Categorias.Otro) =>
        new() { Categoria = cat, Cantidad = cant, PrecioUnitario = precio };

    static readonly List<TarifaPuerta> Tarifas = new()
    {
        new() { AnchoHasta = 0.60m, AltoHasta = 2.40m, PrecioMxn = 7420m, PrecioUsd = 371m },
        new() { AnchoHasta = 1.00m, AltoHasta = 2.40m, PrecioMxn = 9460m, PrecioUsd = 473m },
        new() { AnchoHasta = 1.20m, AltoHasta = 2.40m, PrecioMxn = 9850m, PrecioUsd = 492.50m },
        new() { AnchoHasta = 1.50m, AltoHasta = 3.20m, PrecioMxn = 13500m, PrecioUsd = 675m },
    };

    [Fact]
    public void Midstorage_descuento_e_instalacion()
    {
        var p = new Partida
        {
            Cantidad = 2, VanoAncho = 0.90m, VanoAlto = 2.20m,
            Insumos = { I(1, 9484), I(1, 2000), I(1, 734.56m), I(1, 750), I(1, 1600), I(1, 127) }
        };
        Assert.Equal(0.83m, p.HojaAncho);
        Assert.Equal(2.16m, p.HojaAlto);
        Assert.Equal(14695.56m, p.PrecioUnitario);

        var t = Motor.Calcular(new DatosCotizacion
        {
            Partidas = { p },
            Descuentos = { new Descuento { Nombre = "descuento especial", Porcentaje = 10 } },
            Instalacion = 1000,
        });
        Assert.Equal(29391.12m, t.Material);
        Assert.Equal(2939.112m, t.DescuentoTotal);
        Assert.Equal(27452.008m, t.Subtotal);
        Assert.Equal(4392.32128m, t.Iva);
        Assert.Equal(31844.32928m, t.Total);
    }

    [Fact]
    public void Cemex_puerta_doble_60_40()
    {
        var p = new Partida
        {
            Tipo = TiposPuerta.Doble, Cantidad = 1, VanoAncho = 2.20m, VanoAlto = 2.50m,
            Insumos = { I(2, 11550), I(1.5m, 2020), I(2, 850), I(1, 750), I(1, 2660), I(1, 500), I(1, 1000), I(1, 750) }
        };
        Assert.Equal(1.065m, p.HojaAncho);
        Assert.Equal(2.46m, p.HojaAlto);

        var t = Motor.Calcular(new DatosCotizacion { Partidas = { p }, Instalacion = 2500 });
        Assert.Equal(33490m, t.Material);
        Assert.Equal(35990m, t.Subtotal);
        Assert.Equal(41748.4m, t.Total);
        Assert.Equal(25049.04m, t.Anticipo);
        Assert.Equal(16699.36m, t.Saldo);
    }

    [Fact]
    public void Clinica_dos_partidas()
    {
        var sencilla = new Partida
        {
            Cantidad = 6,
            Insumos = { I(1, 9850), I(1, 2462.5m), I(1.5m, 2355.18m), I(1, 647.88m), I(1, 1500), I(1, 1355.28m) }
        };
        var doble = new Partida
        {
            Tipo = TiposPuerta.Doble, Cantidad = 4,
            Insumos = { I(2, 9850), I(1.5m, 2355.18m), I(2, 3821.87m), I(2, 1355.28m), I(2, 1500) }
        };
        var t = Motor.Calcular(new DatosCotizacion
        {
            Partidas = { sencilla, doble },
            Descuentos = { new Descuento { Nombre = "DESCUENTO PUERTAS AUTOMATICAS", Porcentaje = 15 } },
            Instalacion = 15000,
        });
        Assert.Equal(10, t.PuertasTotales);
        Assert.Equal(262438.86m, t.Material);
        Assert.Equal(238073.031m, t.Subtotal);
        Assert.Equal(276164.71596m, t.Total);
        Assert.Equal(165698.829576m, t.Anticipo);
        Assert.Equal(110465.886384m, t.Saldo);
    }

    [Fact]
    public void Cargos_y_extras_suman_al_subtotal()
    {
        var t = Motor.Calcular(new DatosCotizacion
        {
            Partidas = { new Partida { Cantidad = 1, Insumos = { I(1, 1000) } } },
            Flete = 200, Viaticos = 300, Instalacion = 100,
            Extras = { new Cargo { Nombre = "Maniobras", Monto = 400 } },
        });
        Assert.Equal(2000m, t.Subtotal);
        Assert.Equal(400m, t.Extras);
        Assert.Equal(2320m, t.Total);
    }

    [Theory]
    [InlineData(0.55, 2.10, 7420)]
    [InlineData(0.60, 2.40, 7420)]
    [InlineData(0.83, 2.16, 9460)]
    [InlineData(1.00, 2.40, 9460)]
    [InlineData(1.065, 2.46, 13500)]
    [InlineData(1.13, 2.06, 9850)]
    [InlineData(0.91, 2.58, 13500)]
    public void Precio_de_puerta_por_medida(double ancho, double alto, double esperado)
    {
        Assert.Equal((decimal)esperado, Motor.PrecioPuerta(Tarifas, (decimal)ancho, (decimal)alto, Monedas.MXN));
    }

    [Fact]
    public void Precio_en_dolares_y_fuera_de_tabla()
    {
        Assert.Equal(492.50m, Motor.PrecioPuerta(Tarifas, 1.10m, 2.10m, Monedas.USD));
        Assert.Null(Motor.PrecioPuerta(Tarifas, 1.60m, 2.10m, Monedas.MXN));
        Assert.Null(Motor.PrecioPuerta(Tarifas, 1.00m, 3.50m, Monedas.MXN));
    }
}
