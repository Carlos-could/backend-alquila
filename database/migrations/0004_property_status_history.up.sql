create table if not exists public.property_status_history (
    id uuid primary key default gen_random_uuid(),
    property_id uuid not null references public.properties(id) on delete cascade,
    previous_status text not null,
    new_status text not null,
    changed_by_user_id uuid references public.users(id) on delete set null,
    changed_by_role text not null,
    reason text,
    changed_at timestamptz not null default now()
);

create index if not exists idx_property_status_history_property_id
    on public.property_status_history(property_id, changed_at desc);
