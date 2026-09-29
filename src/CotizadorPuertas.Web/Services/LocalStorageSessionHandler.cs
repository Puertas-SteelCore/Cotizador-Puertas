using Blazored.LocalStorage;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace CotizadorPuertas.Web.Services;

public class LocalStorageSessionHandler : IGotrueSessionPersistence<Session>
{
    private const string Key = "puertas.session";
    private readonly ISyncLocalStorageService _ls;

    public LocalStorageSessionHandler(ISyncLocalStorageService ls) => _ls = ls;

    public void SaveSession(Session session) => _ls.SetItem(Key, session);
    public void DestroySession() => _ls.RemoveItem(Key);
    public Session? LoadSession() => _ls.ContainKey(Key) ? _ls.GetItem<Session>(Key) : null;
}
