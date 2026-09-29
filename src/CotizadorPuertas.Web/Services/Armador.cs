using CotizadorPuertas.Core;
using CotizadorPuertas.Web.Models;

namespace CotizadorPuertas.Web.Services;

public class Armador
{
    private readonly CatalogoService _cat;

    public Armador(CatalogoService cat) => _cat = cat;

    public Partida Nueva(string tipo, string clave, string moneda, string colorMarco)
    {
        var p = new Partida { Tipo = tipo, Clave = clave, Cantidad = 1 };
        if (tipo == TiposPuerta.Doble)
        {
            p.VanoAncho = 1.80m;
            p.Insumos.Add(Especial(Categorias.Puerta, 2));
            p.Insumos.Add(DeProducto(Categorias.Marco, 1.5m, moneda, colorMarco));
            p.Insumos.Add(DeProducto(Categorias.Bisagra, 2, moneda));
            p.Insumos.Add(DeProducto(Categorias.Tope, 1, moneda));
            p.Insumos.Add(DeProducto(Categorias.Pasador, 1, moneda));
            p.Insumos.Add(DeProducto(Categorias.Cerradura, 1, moneda));
        }
        else
        {
            p.Insumos.Add(Especial(Categorias.Puerta, 1));
            p.Insumos.Add(DeProducto(Categorias.Marco, 1, moneda, colorMarco));
            p.Insumos.Add(Especial(Categorias.Antepecho, 1));
            p.Insumos.Add(DeProducto(Categorias.Bisagra, 1, moneda));
            p.Insumos.Add(DeProducto(Categorias.Cerradura, 1, moneda));
        }
        Recalcular(p, moneda);
        return p;
    }

    public Insumo Especial(string categoria, decimal cantidad) => new()
    {
        Categoria = categoria,
        Cantidad = cantidad,
        Descripcion = categoria == Categorias.Puerta
            ? _cat.Ajuste("desc_puerta", "PUERTA LAMINADO COMPACTO 3 MM")
            : _cat.Ajuste("desc_antepecho", "ANTEPECHO LAMINADO COMPACTO 3 MM"),
    };

    public Insumo DeProducto(string categoria, decimal cantidad, string moneda, string? color = null)
    {
        var i = new Insumo { Categoria = categoria, Cantidad = cantidad };
        var prod = Elegir(categoria, color);
        if (prod != null) Asignar(i, prod, moneda);
        return i;
    }

    public ProductoDb? Elegir(string categoria, string? color)
    {
        var lista = _cat.DeCategoria(categoria).ToList();
        if (color != null)
        {
            var c = lista.FirstOrDefault(x => string.Equals(x.Color, color, StringComparison.OrdinalIgnoreCase));
            if (c != null) return c;
        }
        return lista.FirstOrDefault();
    }

    public void Asignar(Insumo i, ProductoDb prod, string moneda)
    {
        i.ProductoId = prod.Id;
        i.Codigo = prod.Codigo;
        i.Descripcion = prod.Descripcion;
        i.PrecioUnitario = prod.Precio(moneda);
        i.PrecioManual = false;
    }

    public decimal? PrecioPuerta(Partida p, string moneda) =>
        Motor.PrecioPuerta(_cat.TarifasCore(), p.HojaAncho, p.HojaAlto, moneda);

    public void Recalcular(Partida p, string moneda)
    {
        var puerta = PrecioPuerta(p, moneda) ?? 0;
        foreach (var i in p.Insumos.Where(x => !x.PrecioManual))
        {
            if (i.Categoria == Categorias.Puerta) i.PrecioUnitario = puerta;
            else if (i.Categoria == Categorias.Antepecho)
                i.PrecioUnitario = Math.Round(puerta * _cat.AjusteNum("antepecho_factor", 0.25m), 2);
            else if (_cat.Producto(i.ProductoId) is { } prod) i.PrecioUnitario = prod.Precio(moneda);
        }
    }

    public void CambiarColorMarco(IEnumerable<Partida> partidas, string color, string moneda)
    {
        var prod = Elegir(Categorias.Marco, color);
        if (prod is null) return;
        foreach (var i in partidas.SelectMany(p => p.Insumos).Where(i => i.Categoria == Categorias.Marco))
        {
            var actual = _cat.Producto(i.ProductoId);
            if (actual is null || !string.Equals(actual.Color, color, StringComparison.OrdinalIgnoreCase))
                Asignar(i, prod, moneda);
        }
    }

    public bool FueraDeTabla(Partida p) =>
        p.Insumos.Any(i => i.Categoria == Categorias.Puerta && !i.PrecioManual) && PrecioPuerta(p, Monedas.MXN) is null;
}
