create extension if not exists pgcrypto;

create table if not exists public.profiles (
  id uuid primary key references auth.users on delete cascade,
  nombre text not null default '',
  email text,
  rol text not null default 'Vendedor' check (rol in ('Admin','Vendedor')),
  activo boolean not null default false,
  creado_en timestamptz not null default now()
);

create or replace function public.handle_new_user() returns trigger
language plpgsql security definer set search_path = public as $$
begin
  insert into public.profiles (id, nombre, email, rol, activo)
  values (new.id,
          coalesce(new.raw_user_meta_data->>'nombre', split_part(new.email,'@',1)),
          new.email, 'Vendedor', false)
  on conflict (id) do nothing;
  return new;
end $$;

drop trigger if exists on_auth_user_created on auth.users;
create trigger on_auth_user_created after insert on auth.users
  for each row execute function public.handle_new_user();

create or replace function public.es_activo() returns boolean
language sql stable security definer set search_path = public as $$
  select exists (select 1 from public.profiles where id = auth.uid() and activo);
$$;

create or replace function public.es_admin() returns boolean
language sql stable security definer set search_path = public as $$
  select exists (select 1 from public.profiles where id = auth.uid() and activo and rol = 'Admin');
$$;

create table if not exists public.tarifa_puerta (
  id bigserial primary key,
  ancho_hasta numeric(6,3) not null,
  alto_hasta numeric(6,3) not null,
  precio_mxn numeric(14,2) not null default 0,
  precio_usd numeric(14,2) not null default 0
);

create table if not exists public.producto (
  id bigserial primary key,
  categoria text not null,
  codigo text not null default '',
  descripcion text not null,
  color text,
  precio_mxn numeric(14,2) not null default 0,
  precio_usd numeric(14,2) not null default 0,
  activo boolean not null default true,
  orden int not null default 0
);

create table if not exists public.color (
  id bigserial primary key,
  nombre text not null unique,
  tipo text not null default 'Línea' check (tipo in ('Línea','Especial')),
  activo boolean not null default true,
  orden int not null default 0
);

create table if not exists public.ajuste (
  clave text primary key,
  valor text not null default ''
);

create sequence if not exists public.cotizacion_folio_seq start 1000;

create table if not exists public.cotizacion (
  id uuid primary key default gen_random_uuid(),
  folio bigint not null default nextval('public.cotizacion_folio_seq') unique,
  fecha date not null default current_date,
  obra text not null default '',
  contacto text not null default '',
  ubicacion text not null default '',
  marca text not null default 'MODUMEX',
  distribuidor text not null default 'SANIMAX',
  linea text not null default 'SteelCore',
  acabado text not null default 'LAMINADO COMPACTO 3 MM',
  color text not null default '',
  color_marco text not null default 'Aluminio',
  color_perfil text not null default 'Aluminio',
  moneda text not null default 'MXN' check (moneda in ('MXN','USD')),
  estado text not null default 'En proceso' check (estado in ('En proceso','Ganada','Perdida')),
  motivo text not null default '',
  partidas jsonb not null default '[]',
  descuentos jsonb not null default '[]',
  extras jsonb not null default '[]',
  instalacion numeric(14,2) not null default 0,
  flete numeric(14,2) not null default 0,
  viaticos numeric(14,2) not null default 0,
  iva_pct numeric(6,4) not null default 0.16,
  anticipo_pct numeric(6,4) not null default 0.60,
  puertas int not null default 0,
  material numeric(16,4) not null default 0,
  descuento numeric(16,4) not null default 0,
  subtotal numeric(16,4) not null default 0,
  iva numeric(16,4) not null default 0,
  total numeric(16,4) not null default 0,
  especificacion text not null default '',
  tiempo_entrega text not null default '',
  observaciones text not null default '',
  creado_por uuid not null default auth.uid() references public.profiles(id),
  vendedor text not null default '',
  creado_en timestamptz not null default now(),
  actualizado_en timestamptz not null default now()
);

create index if not exists cotizacion_creado_por_idx on public.cotizacion (creado_por);
create index if not exists cotizacion_fecha_idx on public.cotizacion (fecha);

create or replace function public.tocar_actualizado() returns trigger
language plpgsql as $$
begin
  new.actualizado_en := now();
  return new;
end $$;

drop trigger if exists cotizacion_actualizado on public.cotizacion;
create trigger cotizacion_actualizado before update on public.cotizacion
  for each row execute function public.tocar_actualizado();

create or replace function public.proteger_creador() returns trigger
language plpgsql as $$
begin
  if not public.es_admin() then
    new.creado_por := old.creado_por;
    new.folio := old.folio;
  end if;
  return new;
end $$;

drop trigger if exists cotizacion_creador on public.cotizacion;
create trigger cotizacion_creador before update on public.cotizacion
  for each row execute function public.proteger_creador();

alter table public.profiles enable row level security;
alter table public.tarifa_puerta enable row level security;
alter table public.producto enable row level security;
alter table public.color enable row level security;
alter table public.ajuste enable row level security;
alter table public.cotizacion enable row level security;

drop policy if exists profiles_ver on public.profiles;
create policy profiles_ver on public.profiles for select using (id = auth.uid() or public.es_admin());
drop policy if exists profiles_editar on public.profiles;
create policy profiles_editar on public.profiles for update using (public.es_admin()) with check (public.es_admin());

do $$
declare t text;
begin
  foreach t in array array['tarifa_puerta','producto','color','ajuste'] loop
    execute format('drop policy if exists %1$s_ver on public.%1$s', t);
    execute format('create policy %1$s_ver on public.%1$s for select using (public.es_activo())', t);
    execute format('drop policy if exists %1$s_admin on public.%1$s', t);
    execute format('create policy %1$s_admin on public.%1$s for all using (public.es_admin()) with check (public.es_admin())', t);
  end loop;
end $$;

drop policy if exists cotizacion_ver on public.cotizacion;
create policy cotizacion_ver on public.cotizacion for select
  using (public.es_admin() or (creado_por = auth.uid() and public.es_activo()));
drop policy if exists cotizacion_crear on public.cotizacion;
create policy cotizacion_crear on public.cotizacion for insert
  with check (public.es_activo() and creado_por = auth.uid());
drop policy if exists cotizacion_editar on public.cotizacion;
create policy cotizacion_editar on public.cotizacion for update
  using (public.es_admin() or (creado_por = auth.uid() and public.es_activo()))
  with check (public.es_admin() or creado_por = auth.uid());
drop policy if exists cotizacion_borrar on public.cotizacion;
create policy cotizacion_borrar on public.cotizacion for delete using (public.es_admin());

grant usage on sequence public.cotizacion_folio_seq to authenticated;
