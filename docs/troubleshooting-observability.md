# Troubleshooting de observabilidad (F1-T08)

## Que esta activo

- Manejo central de errores global con:
  - `builder.Services.AddProblemDetails()`
  - `app.UseExceptionHandler()`
- Healthcheck de servicio en `GET /health`.
- Variable opcional para integracion externa: `SENTRY_DSN`.

## Como verificar rapidamente

1. Levantar servicio:
```bash
dotnet run
```

2. Validar healthcheck:
```bash
curl -i http://localhost:5135/health
```
Esperado: `200 OK` con `{"status":"ok"}`.

3. Forzar una ruta protegida sin token:
```bash
curl -i http://localhost:5135/admin
```
Esperado: `401 Unauthorized` (o `403 Forbidden` segun credenciales/policy).

4. Revisar salida de consola del proceso para excepciones no controladas.

## Que mirar cuando falle

- Si la API cae al iniciar:
  - revisar variables requeridas en `.env` y `EnvironmentValidator`.
- Si falla autorizacion:
  - revisar claims/rol del JWT y policies en `AuthorizationPolicies`.
- Si staging no responde:
  - revisar deploy hook y `STAGING_HEALTHCHECK_URL` en GitHub Actions.

## Limitaciones actuales

- No hay sink persistente habilitado por defecto (solo salida del proceso).
- `SENTRY_DSN` esta declarado pero su integracion todavia es opcional/no cableada en runtime.

