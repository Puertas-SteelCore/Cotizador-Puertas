window.supaFn = {
  invoke: async function (url, apikey, bearer, bodyJson) {
    try {
      const headers = { "Content-Type": "application/json" };
      if (apikey) headers["apikey"] = apikey;
      if (bearer) headers["Authorization"] = "Bearer " + bearer;
      const resp = await fetch(url, { method: "POST", headers, body: bodyJson });
      let text = "";
      try { text = await resp.text(); } catch (e) { }
      return JSON.stringify({ ok: resp.ok, status: resp.status, body: text });
    } catch (e) {
      return JSON.stringify({ ok: false, status: 0, body: "", err: String((e && e.message) || e) });
    }
  }
};
window.descargas = {
  base64: function (nombre, mime, b64) {
    const bin = atob(b64);
    const bytes = new Uint8Array(bin.length);
    for (let i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i);
    const blob = new Blob([bytes], { type: mime });
    const a = document.createElement("a");
    a.href = URL.createObjectURL(blob);
    a.download = nombre;
    document.body.appendChild(a);
    a.click();
    setTimeout(function () { URL.revokeObjectURL(a.href); a.remove(); }, 1500);
  }
};
