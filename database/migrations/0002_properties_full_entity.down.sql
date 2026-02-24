drop index if exists idx_properties_city_monthly_price;
drop index if exists idx_properties_monthly_price;

alter table public.properties
    drop column if exists status,
    drop column if exists contract_type,
    drop column if exists available_from,
    drop column if exists is_furnished,
    drop column if exists area_m2,
    drop column if exists bathrooms,
    drop column if exists bedrooms,
    drop column if exists deposit_amount;
