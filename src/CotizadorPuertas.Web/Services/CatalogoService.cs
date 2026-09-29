using System.Globalization;
using CotizadorPuertas.Core;
using CotizadorPuertas.Web.Models;
using static Supabase.Postgrest.Constants;

namespace CotizadorPuertas.Web.Services;

public class CatalogoService
{
    private readonly Supabase.Client _supa;
    private readonly AuthService _auth;

    public CatalogoService(Supabase.Client supa, AuthService auth)
    {
        _supa = supa;
        _auth = auth;
    }

    public List<TarifaDb> Tarifas { get; private set; } = new();
    public List<ProductoDb> Productos { get; private set; } = new();
    public List<ColorDb> Colores { get; private set; } = new();
    public Dictionary<string, string> Ajustes { get; private set; } = new();
    public bool Cargado { get; private set; }

    public static readonly string[] ColoresAluminio = { "Aluminio", "Blanco", "Negro" };

    public async Task Cargar(bool forzar = false)
    {
        if (Cargado && !forzar) return;
        await _auth.AsegurarSesion();
        Tarifas = (await _supa.From<TarifaDb>().Order(t => t.AnchoHasta, Ordering.Ascending).Get()).Models;
        Productos = (await _supa.From<ProductoDb>().Order(p => p.Orden, Ordering.Ascending).Get()).Models;
        Colores = (await _supa.From<ColorDb>().Order(c => c.Orden, Ordering.Ascending).Get()).Models;
        Ajustes = (await _supa.From<AjusteDb>().Get()).Models.ToDictionary(a => a.Clave, a => a.Valor);
        Cargado = true;
    }

    public string Ajuste(string clave, string def = "") =>
        Ajustes.TryGetValue(clave, out var v) && !string.IsNullOrWhiteSpace(v) ? v : def;

    public decimal AjusteNum(string clave, decimal def) =>
        decimal.TryParse(Ajuste(clave), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : def;

    public IEnumerable<ProductoDb> DeCategoria(string cat) =>
        Productos.Where(p => p.Categoria == cat && p.Activo).OrderBy(p => p.Orden).ThenBy(p => p.Descripcion);

    public ProductoDb? Producto(long? id) => id is null ? null : Productos.FirstOrDefault(p => p.Id == id);

    public List<TarifaPuerta> TarifasCore() => Tarifas.Select(t => t.ToCore()).ToList();

    public async Task GuardarTarifa(TarifaDb t)
    {
        await _auth.AsegurarSesion();
        if (t.Id == 0) await _supa.From<TarifaDb>().Insert(t);
        else await _supa.From<TarifaDb>().Update(t);
        await Cargar(true);
    }

    public async Task BorrarTarifa(TarifaDb t)
    {
        await _auth.AsegurarSesion();
        await _supa.From<TarifaDb>().Where(x => x.Id == t.Id).Delete();
        await Cargar(true);
    }

    public async Task GuardarProducto(ProductoDb p)
    {
        await _auth.AsegurarSesion();
        if (p.Id == 0) await _supa.From<ProductoDb>().Insert(p);
        else await _supa.From<ProductoDb>().Update(p);
        await Cargar(true);
    }

    public async Task BorrarProducto(ProductoDb p)
    {
        await _auth.AsegurarSesion();
        await _supa.From<ProductoDb>().Where(x => x.Id == p.Id).Delete();
        await Cargar(true);
    }

    public async Task AjustarPrecios(string? categoria, decimal factor)
    {
        await _auth.AsegurarSesion();
        foreach (var p in Productos.Where(p => categoria is null || p.Categoria == categoria).ToList())
        {
            p.PrecioMxn = Math.Round(p.PrecioMxn * factor, 2);
            p.PrecioUsd = Math.Round(p.PrecioUsd * factor, 2);
            await _supa.From<ProductoDb>().Update(p);
        }
        await Cargar(true);
    }

    public async Task GuardarColor(ColorDb c)
    {
        await _auth.AsegurarSesion();
        c.Nombre = c.Nombre.Trim().ToUpperInvariant();
        if (c.Id == 0) await _supa.From<ColorDb>().Insert(c);
        else await _supa.From<ColorDb>().Update(c);
        await Cargar(true);
    }

    public async Task BorrarColor(ColorDb c)
    {
        await _auth.AsegurarSesion();
        await _supa.From<ColorDb>().Where(x => x.Id == c.Id).Delete();
        await Cargar(true);
    }

    public async Task GuardarAjustes(Dictionary<string, string> valores)
    {
        await _auth.AsegurarSesion();
        var filas = valores.Select(kv => new AjusteDb { Clave = kv.Key, Valor = kv.Value ?? "" }).ToList();
        await _supa.From<AjusteDb>().Upsert(filas);
        await Cargar(true);
    }
}
