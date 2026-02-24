# Runbook de despliegue staging (F1-T07)

Objetivo: desplegar automaticamente backend a `staging` y validar `GET /health`.

## 1) Provisionar servicio staging en Render

1. Conectar repositorio `backend-alquila` en Render.
2. Crear servicio web usando `render.yaml` (Blueprint) o configuracion equivalente.
3. Confirmar:
   - Runtime: Docker
   - Dockerfile: `./Dockerfile`
   - Healthcheck path: `/health`

## 2) Configurar variables de entorno en Render

Variables requeridas por backend:
- `SUPABASE_URL`
- `SUPABASE_ANON_KEY`
- `SUPABASE_SERVICE_ROLE_KEY`

Opcional:
- `SUPABASE_JWT_SECRET`
- `DATABASE_URL`
- `SENTRY_DSN`

## 3) Configurar secretos en GitHub

En `Settings > Secrets and variables > Actions`, crear:
- `RENDER_DEPLOY_HOOK_URL`: deploy hook de Render para el servicio staging.
- `STAGING_HEALTHCHECK_URL`: URL completa de healthcheck, por ejemplo `https://backend-alquila-staging.onrender.com/health`.

## 4) Flujo de despliegue

- Cada `push` a `main` ejecuta `.github/workflows/deploy-staging.yml`.
- El workflow dispara deploy en Render y espera healthcheck OK.

## 5) Validacion operativa

Comprobar en navegador o curl:

```bash
curl -i https://<staging-host>/health
```

Resultado esperado: `200 OK` con body JSON `{"status":"ok"}`.
