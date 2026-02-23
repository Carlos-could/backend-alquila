create extension if not exists pgcrypto;

create table if not exists public.users (
    id uuid primary key default gen_random_uuid(),
    auth_user_id uuid not null unique,
    email text not null unique,
    role text not null check (role in ('inquilino', 'propietario', 'admin')),
    full_name text,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists public.properties (
    id uuid primary key default gen_random_uuid(),
    owner_user_id uuid not null references public.users(id) on delete cascade,
    title text not null,
    description text,
    city text not null,
    neighborhood text,
    address text,
    monthly_price numeric(12,2) not null check (monthly_price > 0),
    currency char(3) not null default 'EUR',
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists public.applications (
    id uuid primary key default gen_random_uuid(),
    property_id uuid not null references public.properties(id) on delete cascade,
    applicant_user_id uuid not null references public.users(id) on delete cascade,
    status text not null default 'enviada',
    notes text,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint uq_applications_property_applicant unique(property_id, applicant_user_id)
);

create table if not exists public.documents (
    id uuid primary key default gen_random_uuid(),
    user_id uuid not null references public.users(id) on delete cascade,
    application_id uuid references public.applications(id) on delete set null,
    document_type text not null,
    storage_path text not null,
    status text not null default 'subido',
    uploaded_at timestamptz not null default now(),
    verified_at timestamptz
);

create table if not exists public.messages (
    id uuid primary key default gen_random_uuid(),
    application_id uuid not null references public.applications(id) on delete cascade,
    sender_user_id uuid not null references public.users(id) on delete cascade,
    body text not null,
    created_at timestamptz not null default now()
);

create table if not exists public.deals (
    id uuid primary key default gen_random_uuid(),
    application_id uuid not null unique references public.applications(id) on delete cascade,
    property_id uuid not null references public.properties(id) on delete cascade,
    landlord_user_id uuid not null references public.users(id) on delete restrict,
    tenant_user_id uuid not null references public.users(id) on delete restrict,
    status text not null default 'pendiente',
    commission_amount numeric(12,2) not null default 0,
    currency char(3) not null default 'EUR',
    closed_at timestamptz,
    created_at timestamptz not null default now()
);

create index if not exists idx_properties_owner_user_id on public.properties(owner_user_id);
create index if not exists idx_properties_city on public.properties(city);
create index if not exists idx_applications_property_id on public.applications(property_id);
create index if not exists idx_applications_applicant_user_id on public.applications(applicant_user_id);
create index if not exists idx_documents_user_id on public.documents(user_id);
create index if not exists idx_documents_application_id on public.documents(application_id);
create index if not exists idx_messages_application_id on public.messages(application_id);
create index if not exists idx_deals_property_id on public.deals(property_id);
