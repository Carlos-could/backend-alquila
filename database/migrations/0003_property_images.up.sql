create table if not exists public.property_images (
    id uuid primary key default gen_random_uuid(),
    property_id uuid not null references public.properties(id) on delete cascade,
    storage_path text not null,
    public_url text not null,
    mime_type text not null,
    file_size_bytes integer not null check (file_size_bytes > 0),
    display_order integer not null check (display_order >= 0),
    created_at timestamptz not null default now(),
    constraint uq_property_images_property_order unique (property_id, display_order)
);

create index if not exists idx_property_images_property_id
    on public.property_images(property_id);
