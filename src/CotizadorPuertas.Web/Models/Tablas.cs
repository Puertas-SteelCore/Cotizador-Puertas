using CotizadorPuertas.Core;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CotizadorPuertas.Web.Models;

[Table("profiles")]
public class Profile : BaseModel
{
    [PrimaryKey("id", false)] public string Id { get; set; } = "";
    [Column("nombre")] public string Nombre { get; set; } = "";
    [Column("email")] public string? Email { get; set; }
    [Column("rol")] public string Rol { get; set; } = "Vendedor";
    [Column("activo")] public bool Activo { get; set; }
    [Column("creado_en", ignoreOnInsert: true, ignoreOnUpdate: true)] public DateTime? CreadoEn { get; set; }
}

[Table("tarifa_puerta")]
public class TarifaDb : BaseModel
{
    [PrimaryKey("id", false)] public long Id { get; set; }
    [Column("ancho_hasta")] public decimal AnchoHasta { get; set; }
    [Column("alto_hasta")] public decimal AltoHasta { get; set; }
    [Column("precio_mxn")] public decimal PrecioMxn { get; set; }
    [Column("precio_usd")] public decimal PrecioUsd { get; set; }

    public TarifaPuerta ToCore() => new()
    {
        AnchoHasta = AnchoHasta, AltoHasta = AltoHasta, PrecioMxn = PrecioMxn, PrecioUsd = PrecioUsd,
    };
}

[Table("producto")]
public class ProductoDb : BaseModel
{
    [PrimaryKey("id", false)] public long Id { get; set; }
    [Column("categoria")] public string Categoria { get; set; } = Categorias.Herraje;
    [Column("codigo")] public string Codigo { get; set; } = "";
    [Column("descripcion")] public string Descripcion { get; set; } = "";
    [Column("color")] public string? Color { get; set; }
    [Column("precio_mxn")] public decimal PrecioMxn { get; set; }
    [Column("precio_usd")] public decimal PrecioUsd { get; set; }
    [Column("activo")] public bool Activo { get; set; } = true;
    [Column("orden")] public int Orden { get; set; }

    public decimal Precio(string moneda) => moneda == Monedas.USD ? PrecioUsd : PrecioMxn;
}

[Table("color")]
public class ColorDb : BaseModel
{
    [PrimaryKey("id", false)] public long Id { get; set; }
    [Column("nombre")] public string Nombre { get; set; } = "";
    [Column("tipo")] public string Tipo { get; set; } = "Línea";
    [Column("activo")] public bool Activo { get; set; } = true;
    [Column("orden")] public int Orden { get; set; }
}

[Table("ajuste")]
public class AjusteDb : BaseModel
{
    [PrimaryKey("clave", true)] public string Clave { get; set; } = "";
    [Column("valor")] public string Valor { get; set; } = "";
}

[Table("cotizacion")]
public class CotizacionDb : BaseModel
{
    [PrimaryKey("id", false)] public string? Id { get; set; }
    [Column("folio", ignoreOnInsert: true, ignoreOnUpdate: true)] public long Folio { get; set; }
    [Column("fecha")] public DateTime Fecha { get; set; } = DateTime.Today;
    [Column("obra")] public string Obra { get; set; } = "";
    [Column("contacto")] public string Contacto { get; set; } = "";
    [Column("ubicacion")] public string Ubicacion { get; set; } = "";
    [Column("marca")] public string Marca { get; set; } = "MODUMEX";
    [Column("distribuidor")] public string Distribuidor { get; set; } = "SANIMAX";
    [Column("linea")] public string Linea { get; set; } = "SteelCore";
    [Column("acabado")] public string Acabado { get; set; } = "LAMINADO COMPACTO 3 MM";
    [Column("color")] public string Color { get; set; } = "";
    [Column("color_marco")] public string ColorMarco { get; set; } = "Aluminio";
    [Column("color_perfil")] public string ColorPerfil { get; set; } = "Aluminio";
    [Column("moneda")] public string Moneda { get; set; } = Monedas.MXN;
    [Column("estado")] public string Estado { get; set; } = "En proceso";
    [Column("motivo")] public string Motivo { get; set; } = "";
    [Column("partidas")] public List<Partida> Partidas { get; set; } = new();
    [Column("descuentos")] public List<Descuento> Descuentos { get; set; } = new();
    [Column("extras")] public List<Cargo> Extras { get; set; } = new();
    [Column("instalacion")] public decimal Instalacion { get; set; }
    [Column("flete")] public decimal Flete { get; set; }
    [Column("viaticos")] public decimal Viaticos { get; set; }
    [Column("iva_pct")] public decimal IvaPct { get; set; } = 0.16m;
    [Column("anticipo_pct")] public decimal AnticipoPct { get; set; } = 0.60m;
    [Column("puertas")] public int Puertas { get; set; }
    [Column("material")] public decimal Material { get; set; }
    [Column("descuento")] public decimal Descuento { get; set; }
    [Column("subtotal")] public decimal Subtotal { get; set; }
    [Column("iva")] public decimal Iva { get; set; }
    [Column("total")] public decimal Total { get; set; }
    [Column("especificacion")] public string Especificacion { get; set; } = "";
    [Column("tiempo_entrega")] public string TiempoEntrega { get; set; } = "";
    [Column("observaciones")] public string Observaciones { get; set; } = "";
    [Column("creado_por", ignoreOnUpdate: true)] public string? CreadoPor { get; set; }
    [Column("vendedor")] public string Vendedor { get; set; } = "";
    [Column("creado_en", ignoreOnInsert: true, ignoreOnUpdate: true)] public DateTime? CreadoEn { get; set; }
    [Column("actualizado_en", ignoreOnInsert: true, ignoreOnUpdate: true)] public DateTime? ActualizadoEn { get; set; }

    [Newtonsoft.Json.JsonIgnore] public string Numero => Folio > 0 ? $"P.{Folio}" : "Nueva";

    public DatosCotizacion Datos() => new()
    {
        Partidas = Partidas, Descuentos = Descuentos, Extras = Extras,
        Instalacion = Instalacion, Flete = Flete, Viaticos = Viaticos,
        IvaPct = IvaPct, AnticipoPct = AnticipoPct,
    };
}
