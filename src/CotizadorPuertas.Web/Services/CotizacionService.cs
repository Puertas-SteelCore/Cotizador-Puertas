using CotizadorPuertas.Core;
using CotizadorPuertas.Web.Models;
using Newtonsoft.Json;
using static Supabase.Postgrest.Constants;

namespace CotizadorPuertas.Web.Services;

public class CotizacionService
{
    private readonly Supabase.Client _supa;
    private readonly AuthService _auth;

    public CotizacionService(Supabase.Client supa, AuthService auth)
    {
        _supa = supa;
        _auth = auth;
    }

    public static readonly string[] Estados = { "En proceso", "Ganada", "Perdida" };

    public async Task<List<CotizacionDb>> Listar()
    {
        await _auth.AsegurarSesion();
        var r = await _supa.From<CotizacionDb>().Order(c => c.Folio, Ordering.Descending).Get();
        return r.Models;
    }

    public async Task<CotizacionDb?> Obtener(string id)
    {
        await _auth.AsegurarSesion();
        return await _supa.From<CotizacionDb>().Where(c => c.Id == id).Single();
    }

    public static void Totalizar(CotizacionDb c)
    {
        var t = Motor.Calcular(c.Datos());
        c.Puertas = t.PuertasTotales;
        c.Material = Math.Round(t.Material, 4);
        c.Descuento = Math.Round(t.DescuentoTotal, 4);
        c.Subtotal = Math.Round(t.Subtotal, 4);
        c.Iva = Math.Round(t.Iva, 4);
        c.Total = Math.Round(t.Total, 4);
    }

    public async Task<CotizacionDb> Guardar(CotizacionDb c)
    {
        await _auth.AsegurarSesion();
        c.Descuentos = c.Descuentos.Where(d => d.Porcentaje != 0 || !string.IsNullOrWhiteSpace(d.Nombre)).ToList();
        c.Extras = c.Extras.Where(e => e.Monto != 0 || !string.IsNullOrWhiteSpace(e.Nombre)).ToList();
        Totalizar(c);
        if (string.IsNullOrEmpty(c.Id))
        {
            c.CreadoPor = _auth.UsuarioId;
            c.Vendedor = _auth.Nombre;
            var r = await _supa.From<CotizacionDb>().Insert(c);
            return r.Models.First();
        }
        var u = await _supa.From<CotizacionDb>().Update(c);
        return u.Models.FirstOrDefault() ?? c;
    }

    public async Task CambiarEstado(CotizacionDb c, string estado, string motivo)
    {
        await _auth.AsegurarSesion();
        await _supa.From<CotizacionDb>()
            .Where(x => x.Id == c.Id)
            .Set(x => x.Estado, estado)
            .Set(x => x.Motivo, motivo ?? "")
            .Update();
        c.Estado = estado;
        c.Motivo = motivo ?? "";
    }

    public async Task Borrar(CotizacionDb c)
    {
        await _auth.AsegurarSesion();
        await _supa.From<CotizacionDb>().Where(x => x.Id == c.Id).Delete();
    }

    public static CotizacionDb Copia(CotizacionDb c)
    {
        var json = JsonConvert.SerializeObject(c);
        var n = JsonConvert.DeserializeObject<CotizacionDb>(json)!;
        n.Id = null;
        n.Folio = 0;
        n.Estado = "En proceso";
        n.Motivo = "";
        n.CreadoPor = null;
        n.Vendedor = "";
        n.CreadoEn = null;
        n.ActualizadoEn = null;
        n.Fecha = DateTime.Today;
        return n;
    }

    public async Task<List<Profile>> Usuarios()
    {
        await _auth.AsegurarSesion();
        return (await _supa.From<Profile>().Order(p => p.Nombre, Ordering.Ascending).Get()).Models;
    }

    public async Task GuardarPerfil(Profile p)
    {
        await _auth.AsegurarSesion();
        await _supa.From<Profile>()
            .Where(x => x.Id == p.Id)
            .Set(x => x.Rol, p.Rol)
            .Set(x => x.Activo, p.Activo)
            .Set(x => x.Nombre, p.Nombre)
            .Update();
    }
}
