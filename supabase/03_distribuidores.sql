create table if not exists public.cuenta_bancaria (
  id bigserial primary key,
  nombre text not null unique,
  moneda text not null default 'MXN' check (moneda in ('MXN','USD')),
  datos text not null default '',
  activo boolean not null default true
);

create table if not exists public.distribuidor (
  id bigserial primary key,
  nombre text not null unique,
  razon_social text not null default '',
  tipo text not null default 'Nacional' check (tipo in ('Nacional','Internacional')),
  cuenta_id bigint references public.cuenta_bancaria(id) on delete set null,
  contacto text not null default '',
  ciudad text not null default '',
  firma text not null default '',
  logo text check (logo is null or length(logo) <= 400000),
  activo boolean not null default true
);

alter table public.cotizacion add column if not exists distribuidor_id bigint references public.distribuidor(id) on delete set null;

alter table public.cuenta_bancaria enable row level security;
alter table public.distribuidor enable row level security;

do $$
declare t text;
begin
  foreach t in array array['cuenta_bancaria','distribuidor'] loop
    execute format('drop policy if exists %1$s_ver on public.%1$s', t);
    execute format('create policy %1$s_ver on public.%1$s for select using (public.es_activo())', t);
    execute format('drop policy if exists %1$s_admin on public.%1$s', t);
    execute format('create policy %1$s_admin on public.%1$s for all using (public.es_admin()) with check (public.es_admin())', t);
  end loop;
end $$;

insert into public.cuenta_bancaria (nombre, moneda, datos) values
  ('SANIMAX - Pesos', 'MXN', 'SANIMAX DEL SURESTE SA DE CV
CUENTA EN PESOS MONEDA NACIONAL
BANCO: BANORTE (BANCO MERCANTIL DEL NORTE SA)
SUCURSAL: 2597 FIESTA II
CUENTA: 0609576283
CLABE: 072-910-006095762831'),
  ('Compact Doors - Nacional (pesos)', 'MXN', 'COMPACT DOORS SA DE CV
RFC: CDO200520CF2
BANCO: BANORTE
SUCURSAL: 2597 PLAZA FIESTA II
CUENTA: 1117902647
CLABE: 072 910 01117902647 5'),
  ('Compact Doors - Internacional (dólares)', 'USD', 'CUENTA EN DÓLARES
BENEFICIARIO: COMPACT DOORS SA DE CV
BANCO: BANORTE
SUCURSAL: 2597 PLAZA FIESTA II
CUENTA: 1117902647
SWIFT: MENOMXMT
CLABE: 072 910 01117902647 5')
on conflict (nombre) do nothing;

insert into public.distribuidor (nombre, razon_social, tipo, cuenta_id, ciudad, firma)
select 'SANIMAX', 'SANIMAX DEL SURESTE SA DE CV', 'Nacional', c.id, 'Mérida, Yucatán', 'LIC. MIGUEL IVAN RUIZ
SANIMAX DEL SURESTE SA DE CV'
from public.cuenta_bancaria c where c.nombre = 'SANIMAX - Pesos'
on conflict (nombre) do nothing;

insert into public.distribuidor (nombre, razon_social, tipo, cuenta_id)
select v.nombre, v.razon, v.tipo, c.id
from (values
  ('BL', 'BL CONSTRUCCIONES Y MANTENIMIENTO INTEGRAL', 'Nacional'),
  ('DISMO', 'DISEÑO MODULAR Y ACCESORIOS', 'Nacional'),
  ('DETECSA', 'DESARROLLOS TECNOLOGICOS PARA SANITARIOS PUBLICOS', 'Nacional'),
  ('MODULAG', 'DESARROLLOS TECNOLOGICOS PARA SANITARIOS PUBLICOS', 'Nacional'),
  ('INNTESA', 'SOLUCIONES SANITARIAS INNTESA', 'Nacional'),
  ('HYSM', 'HERRAMIENTAS Y SOPORTE EN MANUFACTURA', 'Nacional'),
  ('JAIME', 'JAIME ALBERTO ZAVALA MONTES', 'Nacional'),
  ('CHÁVEZARQUITECTOS&ASOC.', 'NOHEMI ALEJANDRA RODRIGUEZ GALLO', 'Nacional'),
  ('PUERTAS AUTOMATICAS', 'PUERTAS AUTOMATICAS DE MERIDA', 'Nacional'),
  ('RPA', 'RPA DISTRIBUICIONES SA/GUATEMALA', 'Internacional'),
  ('SANIAC', 'SANIAC SERVICIOS', 'Nacional'),
  ('SANITEC', 'ESTRUCTURA SANITARIA SANITEC', 'Nacional'),
  ('MODUMEX', 'SOLUCIONES PARA SANITARIOS PUBLICOS DEL CENTRO', 'Nacional'),
  ('VEESCON', 'VEESCON', 'Nacional')
) v(nombre, razon, tipo)
join public.cuenta_bancaria c on c.nombre = case when v.tipo = 'Internacional'
  then 'Compact Doors - Internacional (dólares)' else 'Compact Doors - Nacional (pesos)' end
on conflict (nombre) do nothing;

update public.cotizacion q set distribuidor_id = d.id
from public.distribuidor d
where q.distribuidor_id is null and upper(q.distribuidor) = upper(d.nombre);
