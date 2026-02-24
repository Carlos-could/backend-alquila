alter table public.properties
    add column if not exists deposit_amount numeric(12,2) not null default 0 check (deposit_amount >= 0),
    add column if not exists bedrooms integer not null default 0 check (bedrooms >= 0),
    add column if not exists bathrooms integer not null default 0 check (bathrooms >= 0),
    add column if not exists area_m2 numeric(10,2) not null default 1 check (area_m2 > 0),
    add column if not exists is_furnished boolean not null default false,
    add column if not exists available_from date not null default current_date,
    add column if not exists contract_type text not null default 'long_term' check (contract_type in ('long_term', 'temporary', 'monthly')),
    add column if not exists status text not null default 'pendiente' check (status in ('pendiente', 'publicado', 'rechazado'));

create index if not exists idx_properties_monthly_price on public.properties(monthly_price);
create index if not exists idx_properties_city_monthly_price on public.properties(city, monthly_price);
