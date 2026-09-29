using System.Globalization;
using System.Text.RegularExpressions;
using CotizadorPuertas.Core;
using CotizadorPuertas.Web.Models;
using Microsoft.JSInterop;

namespace CotizadorPuertas.Web.Services;

public class DocumentoService
{
    private readonly IJSRuntime _js;
    private readonly CatalogoService _cat;
    private static readonly string[] Meses =
        { "enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre" };

    public static string FechaLarga(DateTime f) => $"{f.Day} de {Meses[f.Month - 1]} de {f.Year}";

    public DocumentoService(IJSRuntime js, CatalogoService cat)
    {
        _js = js;
        _cat = cat;
    }

    public static string Especificacion(string plantilla, string colorPerfil)
    {
        var perfil = colorPerfil switch
        {
            "Blanco" => "color blanco",
            "Negro" => "color negro",
            _ => "anodizado natural",
        };
        return plantilla.Replace("{perfil}", perfil);
    }

    public object Datos(CotizacionDb c)
    {
        var t = Motor.Calcular(c.Datos());
        double D(decimal x) => (double)x;
        var nombre = Regex.Replace($"Cotizacion {c.Numero} {c.Obra}".Trim(), @"[^\w\.\- ]+", "").Trim();

        return new
        {
            archivo = nombre,
            folio = c.Numero,
            fecha = FechaLarga(c.Fecha),
            obra = c.Obra,
            contacto = c.Contacto,
            ubicacion = c.Ubicacion,
            marca = c.Marca,
            distribuidor = c.Distribuidor,
            linea = c.Linea,
            acabado = c.Acabado,
            color = c.Color,
            colorMarco = c.ColorMarco,
            colorPerfil = c.ColorPerfil,
            moneda = c.Moneda,
            especificacion = c.Especificacion,
            partidas = c.Partidas.Select((p, n) => new
            {
                clave = string.IsNullOrWhiteSpace(p.Clave) ? $"P-{n + 1:00}" : p.Clave,
                ubicacion = p.Ubicacion,
                producto = p.Tipo == TiposPuerta.Doble ? "PUERTA DOBLE" : "PUERTA SENCILLA",
                cantidad = p.Cantidad,
                hojaAncho = D(p.HojaAncho),
                hojaAlto = D(p.HojaAlto),
                vanoAncho = D(p.VanoAncho),
                vanoAlto = D(p.VanoAlto),
                precioUnitario = D(p.PrecioUnitario),
                total = D(p.Total),
                insumos = p.Insumos.Where(i => i.Cantidad != 0).Select(i => new
                {
                    cantidad = D(i.Cantidad),
                    descripcion = i.Descripcion,
                    codigo = i.Codigo,
                    precio = D(i.PrecioUnitario),
                    total = D(i.Total),
                }),
            }),
            totales = new
            {
                puertas = t.PuertasTotales,
                material = D(t.Material),
                descuentos = t.Descuentos.Select(x => new { nombre = x.Nombre, porcentaje = D(x.Porcentaje), monto = D(x.Monto) }),
                instalacion = D(t.Instalacion),
                flete = D(t.Flete),
                viaticos = D(t.Viaticos),
                extras = c.Extras.Where(e => e.Monto != 0).Select(e => new { nombre = e.Nombre, monto = D(e.Monto) }),
                subtotal = D(t.Subtotal),
                ivaPct = D(c.IvaPct),
                iva = D(t.Iva),
                total = D(t.Total),
                anticipoPct = D(c.AnticipoPct),
                anticipo = D(t.Anticipo),
                saldo = D(t.Saldo),
            },
            banco = c.Moneda == Monedas.USD ? _cat.Ajuste("banco_usd") : _cat.Ajuste("banco_mxn"),
            tiempoEntrega = c.TiempoEntrega,
            observaciones = c.Observaciones,
            firma = _cat.Ajuste("firma"),
            vendedor = c.Vendedor,
        };
    }

    public async Task<string?> Pdf(CotizacionDb c)
    {
        try
        {
            await _cat.Cargar();
            await _js.InvokeVoidAsync("cotizadorPdf.descargar", Datos(c));
            return null;
        }
        catch (Exception ex) { return "No se pudo generar el PDF: " + ex.Message; }
    }

    public async Task<string?> Excel(CotizacionDb c)
    {
        try
        {
            await _cat.Cargar();
            await _js.InvokeVoidAsync("cotizadorExcel.descargar", Datos(c));
            return null;
        }
        catch (Exception ex) { return "No se pudo generar el Excel: " + ex.Message; }
    }
}
