import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

function json(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { ...cors, "Content-Type": "application/json" } });
}

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") return new Response("ok", { headers: cors });
  if (req.method !== "POST") return json({ error: "Método no permitido." }, 405);

  try {
    const url = Deno.env.get("SUPABASE_URL")!;
    const anon = Deno.env.get("SUPABASE_ANON_KEY")!;
    const service = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!;

    const caller = createClient(url, anon, { global: { headers: { Authorization: req.headers.get("Authorization") ?? "" } } });
    const { data: { user }, error: uErr } = await caller.auth.getUser();
    if (uErr || !user) return json({ error: "No autenticado." }, 401);

    const admin = createClient(url, service);
    const { data: yo } = await admin.from("profiles").select("rol, activo").eq("id", user.id).single();
    if (yo?.rol !== "Admin" || !yo?.activo) return json({ error: "Solo un Admin puede gestionar usuarios." }, 403);

    const { action, userId, nombre, email, password, rol } = await req.json();
    const rolValido = rol === "Admin" ? "Admin" : "Vendedor";

    if (action === "crear") {
      if (!email || !password) return json({ error: "Faltan correo o contraseña." }, 400);
      if (String(password).length < 6) return json({ error: "La contraseña debe tener al menos 6 caracteres." }, 400);
      const { data, error } = await admin.auth.admin.createUser({
        email, password, email_confirm: true, user_metadata: { nombre: nombre ?? "" },
      });
      if (error) {
        const m = (error.message ?? "").toLowerCase();
        if (m.includes("already") || m.includes("registered") || m.includes("exists"))
          return json({ error: "Ese correo ya tiene una cuenta." }, 409);
        return json({ error: error.message }, 400);
      }
      const { error: pErr } = await admin.from("profiles").upsert({
        id: data.user!.id, nombre: nombre ?? "", email, rol: rolValido, activo: true,
      });
      if (pErr) return json({ error: pErr.message }, 500);
      return json({ ok: true, id: data.user!.id });
    }

    if (!userId) return json({ error: "Falta el usuario." }, 400);

    if (action === "eliminar") {
      if (userId === user.id) return json({ error: "No puedes eliminar tu propia cuenta." }, 400);
      const { count } = await admin.from("cotizacion").select("id", { count: "exact", head: true }).eq("creado_por", userId);
      if ((count ?? 0) > 0)
        return json({ error: "Este usuario tiene cotizaciones. Desactívalo en lugar de eliminarlo." }, 409);
      const { error } = await admin.auth.admin.deleteUser(userId);
      if (error) return json({ error: error.message }, 400);
      return json({ ok: true });
    }

    if (action === "actualizar") {
      const attrs: Record<string, unknown> = {};
      if (email) { attrs.email = email; attrs.email_confirm = true; }
      if (password) {
        if (String(password).length < 6) return json({ error: "La contraseña debe tener al menos 6 caracteres." }, 400);
        attrs.password = password;
      }
      if (Object.keys(attrs).length > 0) {
        const { error } = await admin.auth.admin.updateUserById(userId, attrs);
        if (error) return json({ error: error.message }, 400);
      }
      const upd: Record<string, unknown> = {};
      if (typeof nombre === "string") upd.nombre = nombre;
      if (email) upd.email = email;
      if (Object.keys(upd).length > 0) {
        const { error } = await admin.from("profiles").update(upd).eq("id", userId);
        if (error) return json({ error: error.message }, 500);
      }
      return json({ ok: true });
    }

    return json({ error: "Acción no válida." }, 400);
  } catch (e) {
    return json({ error: String((e as Error)?.message ?? e) }, 500);
  }
});
