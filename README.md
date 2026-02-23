# Backend Alquila

Base API para el MVP de Alquila.

## Ejecutar (1 comando)

```bash
dotnet run
```

App local: `http://localhost:5135`  
Healthcheck: `http://localhost:5135/health`

## Variables de entorno

Copiar `.env.example` a `.env` y completar:

- `SUPABASE_URL` (requerida)
- `SUPABASE_ANON_KEY` (requerida)
- `SUPABASE_SERVICE_ROLE_KEY` (requerida)
- `SUPABASE_JWT_SECRET` (opcional en F1)
- `DATABASE_URL` (opcional en F1)
- `SENTRY_DSN` (opcional)

Si falta una variable critica, el backend falla al iniciar con mensaje claro.

## Migraciones de base de datos (F1-T05)

Scripts versionados:
- `database/migrations/0001_initial_schema.up.sql`
- `database/migrations/0001_initial_schema.down.sql`

Comandos:

```bash
dotnet run -- migrate up
dotnet run -- migrate down
```

Requiere `DATABASE_URL` valido (PostgreSQL).

Diagrama y descripcion de entidades:
- `docs/data-model.md`

## Autorizacion por roles (F1-T04)

Roles soportados:
- `inquilino`
- `propietario`
- `admin`

Rutas protegidas:
- `GET /auth/me` (requiere usuario autenticado)
- `GET /inquilino` (solo `inquilino`)
- `GET /propietario` (solo `propietario`)
- `GET /admin` (solo `admin`)

Matriz de permisos:
- `docs/security/permissions-matrix.md`

Para probar rutas protegidas, enviar JWT en header:

```bash
Authorization: Bearer <access_token>
```

## Estructura inicial (F1-T01)

```text
src/
  Features/
    Auth/
    Properties/
    Applications/
    Messaging/
  Infrastructure/
    Configuration/
```
