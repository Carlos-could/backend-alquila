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

Si falta una variable crítica, el backend falla al iniciar con mensaje claro.

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
