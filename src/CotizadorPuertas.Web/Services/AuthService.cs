using System.Text.Json;
using CotizadorPuertas.Web.Models;
using Microsoft.JSInterop;

namespace CotizadorPuertas.Web.Services;

public class AuthService
{
    private readonly Supabase.Client _supa;
    private readonly IJSRuntime _js;
    private readonly string _url;
    private readonly string _anon;

    public AuthService(Supabase.Client supa, IConfiguration cfg, IJSRuntime js)
    {
        _supa = supa;
        _js = js;
        _url = (cfg["Supabase:Url"] ?? "").TrimEnd('/');
        _anon = cfg["Supabase:AnonKey"] ?? "";
    }

    public Profile? Perfil { get; private set; }
    public bool Listo { get; private set; }
    public event Action? Cambio;

    public bool Autenticado => _supa.Auth.CurrentUser != null && Perfil is { Activo: true };
    public bool EsAdmin => Perfil is { Rol: "Admin", Activo: true };
    public string UsuarioId => _supa.Auth.CurrentUser?.Id ?? "";
    public string Nombre => string.IsNullOrWhiteSpace(Perfil?.Nombre) ? (Perfil?.Email ?? "") : Perfil!.Nombre;

    public async Task Inicializar()
    {
        try
        {
            if (_supa.Auth.CurrentUser != null)
            {
                await AsegurarSesion();
                await CargarPerfil();
            }
        }
        catch { Perfil = null; }
        Listo = true;
        Cambio?.Invoke();
    }

    public Task MarcarListo()
    {
        Listo = true;
        Cambio?.Invoke();
        return Task.CompletedTask;
    }

    public async Task AsegurarSesion()
    {
        var s = _supa.Auth.CurrentSession;
        if (s is null) return;
        if (s.ExpiresAt() <= DateTime.UtcNow.AddMinutes(2))
        {
            try { await _supa.Auth.RefreshSession(); } catch { }
        }
    }

    public async Task<string?> IniciarSesion(string email, string password)
    {
        try
        {
            var session = await _supa.Auth.SignIn(email.Trim(), password);
            if (session?.User == null) return "Correo o contraseña incorrectos.";
            await CargarPerfil();
            if (Perfil is null || !Perfil.Activo)
            {
                await _supa.Auth.SignOut();
                Perfil = null;
                return "Tu cuenta todavía no está activa. Pídele al administrador que la active.";
            }
            Cambio?.Invoke();
            return null;
        }
        catch (Exception ex)
        {
            var e = ex.Message.ToLowerInvariant();
            if (e.Contains("invalid")) return "Correo o contraseña incorrectos.";
            if (e.Contains("rate") || e.Contains("too many")) return "Demasiados intentos. Espera un momento.";
            if (e.Contains("network") || e.Contains("fetch")) return "Sin conexión. Revisa tu internet.";
            return "No se pudo iniciar sesión.";
        }
    }

    public async Task CerrarSesion()
    {
        try { await _supa.Auth.SignOut(); } catch { }
        Perfil = null;
        Cambio?.Invoke();
    }

    private async Task CargarPerfil()
    {
        var uid = _supa.Auth.CurrentUser?.Id;
        if (uid is null) { Perfil = null; return; }
        Perfil = await _supa.From<Profile>().Where(p => p.Id == uid).Single();
    }

    public async Task<(bool ok, string? error)> Usuarios(Dictionary<string, object?> cuerpo)
    {
        try
        {
            await AsegurarSesion();
            var url = $"{_url}/functions/v1/usuarios";
            var bearer = _supa.Auth.CurrentSession?.AccessToken;
            var res = await _js.InvokeAsync<string>("supaFn.invoke", url, _anon, bearer, JsonSerializer.Serialize(cuerpo));
            using var doc = JsonDocument.Parse(res);
            var root = doc.RootElement;
            if (root.GetProperty("ok").GetBoolean()) return (true, null);
            var status = root.TryGetProperty("status", out var st) ? st.GetInt32() : 0;
            var body = root.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";
            string? err = null;
            try
            {
                using var d2 = JsonDocument.Parse(body);
                if (d2.RootElement.TryGetProperty("error", out var e)) err = e.GetString();
                else if (d2.RootElement.TryGetProperty("message", out var m)) err = m.GetString();
            }
            catch { }
            if (status == 404) err = "La función \"usuarios\" no está publicada en Supabase.";
            if (status == 0) err = "No se pudo conectar con el servidor.";
            return (false, err ?? $"Error {status}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
