# docs/tickets.md

## Convencion de ejecucion tecnica (obligatoria)

Antes de iniciar cualquier ticket, aplicar checklist DoD y reglas core del repositorio.

## Epica F1: Fundaciones
Estado por ticket:
- `Pendiente`: no iniciado
- `En progreso`: implementacion activa
- `Done`: implementado + evidencia tecnica registrada

### F1-T01 - Inicializar repositorio y estructura
Prioridad: P0
Objetivo: estructura base mantenible.
Estado: Done
Evidencia:
- Proyecto ejecutable con `dotnet run` documentado en `README.md`.
- Archivos base presentes: `.editorconfig`, `.gitignore`, `README.md`.
- Estructura por dominios presente en `src/Features`: `Auth`, `Properties`, `Applications`, `Messaging`.

### F1-T02 - Configurar entorno y variables
Prioridad: P0
Objetivo: estandarizar ejecucion en `dev` y `staging`.
Estado: Done
Evidencia:
- `.env.example` presente con variables criticas.
- Validacion fail-fast implementada en `src/Infrastructure/Configuration/EnvironmentValidator.cs`.
- Carga de `.env` en runtime implementada en `src/Infrastructure/Configuration/DotEnvLoader.cs`.
- Variables documentadas en `README.md`.

### F1-T03 - Autenticacion base
Prioridad: P0
Objetivo: autenticacion segura por token.
Estado: Done
Evidencia:
- JWT bearer configurado en `Program.cs` con issuer de Supabase.
- Endpoint protegido `GET /auth/me` implementado.
- Integracion de autenticacion activa via `UseAuthentication`.

### F1-T04 - Roles y autorizacion
Prioridad: P0
Objetivo: controlar acceso por tipo de usuario.
Estado: Done
Evidencia:
- Roles tipados: `inquilino`, `propietario`, `admin` en `src/Features/Auth/UserRoles.cs`.
- Policies por rol en `src/Features/Auth/AuthorizationPolicies.cs`.
- Endpoints protegidos por policy: `/inquilino`, `/propietario`, `/admin`.
- Matriz de permisos documentada en `docs/security/permissions-matrix.md`.
- Pruebas de autorizacion en `tests/Backend.Alquila.Tests/AuthorizationIntegrationTests.cs`.

### F1-T05 - Modelo de datos inicial + migraciones
Prioridad: P0
Objetivo: base de datos preparada para siguientes fases.
Estado: Done
Evidencia:
- Migraciones versionadas: `database/migrations/0001_initial_schema.up.sql` y `database/migrations/0001_initial_schema.down.sql`.
- Runner de migraciones: `src/Infrastructure/Persistence/Migrations/MigrationRunner.cs`.
- Comandos documentados: `dotnet run -- migrate up/down`.
- Diagrama de entidades en `docs/data-model.md`.

### F1-T06 - CI basico
Prioridad: P1
Objetivo: evitar regresiones tempranas.
Estado: Done
Evidencia:
- Workflow CI en `.github/workflows/ci.yml`.
- Etapas configuradas: lint (`dotnet format`), build (`dotnet build`), test (`dotnet test`).
- Triggers en `pull_request` y `push` a `main`.

### F1-T07 - Despliegue staging minimo
Prioridad: P1
Objetivo: validar flujo real fuera de local.
Estado: Done
Evidencia:
- Despliegue staging via `Dockerfile`, `render.yaml` y `.github/workflows/deploy-staging.yml`.
- Healthcheck `GET /health` implementado en `Program.cs`.
- Runbook de despliegue en `docs/staging-deploy-runbook.md`.

### F1-T08 - Observabilidad minima
Prioridad: P2
Objetivo: detectar fallos temprano.
Estado: Done
Evidencia:
- Manejo central de errores con `AddProblemDetails` y `UseExceptionHandler` en `Program.cs`.
- Variable `SENTRY_DSN` declarada como opcion de observabilidad en `.env.example` y `README.md`.
- Guia breve de troubleshooting agregada en `docs/troubleshooting-observability.md`.

