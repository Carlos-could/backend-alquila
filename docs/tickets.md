# docs/tickets.md

## Convencion de ejecucion tecnica (obligatoria)

Antes de iniciar cualquier ticket, aplicar checklist DoD y reglas core del repositorio.

## Convencion transversal de alcance y estado (aplica a todo el archivo)

Esta convencion aplica retroactivamente a todos los tickets de F1-F5 y a los nuevos.

Campos obligatorios por ticket:
- `Scope`: `BE`, `FE` o `FULLSTACK`.
- `Estado`: usar `BE: <estado>` y `FE: <estado>` cuando `Scope` sea `FULLSTACK`.
- `Evidencia`: separar en bloques `Backend:` y `Frontend:` cuando `Scope` sea `FULLSTACK`.

Regla de interpretacion operativa:
- Si el alcance menciona migraciones, tablas, indices, endpoints, permisos de API, repositorios o tests de integracion, incluye `BE`.
- Si el alcance menciona formularios, vistas, UX, estados de UI, consumo de API o navegacion, incluye `FE`.
- Si mezcla ambos tipos, el ticket es `FULLSTACK` y no puede marcarse `Done` sin BE y FE en `Done`.

Estados permitidos por capa:
- `Pendiente`: no iniciado.
- `En progreso`: implementacion activa.
- `Done`: implementado + evidencia tecnica registrada.

Plantilla corta recomendada:
- `Scope: FULLSTACK`
- `Estado: BE: Done | FE: En progreso`
- `Evidencia:`
- `Backend: ...`
- `Frontend: ...`

## Epica F1: Fundaciones
Estado por ticket:
- `Pendiente`: no iniciado
- `En progreso`: implementacion activa
- `Done`: implementado + evidencia tecnica registrada

### F1-T01 - Inicializar repositorio y estructura
Prioridad: P0
Objetivo: estructura base mantenible.
Scope: FULLSTACK
Estado: BE: Done | FE: Done
Evidencia:
- Backend:
  - Proyecto ejecutable con `dotnet run` documentado en `README.md`.
  - Archivos base presentes: `.editorconfig`, `.gitignore`, `README.md`.
  - Estructura por dominios presente en `src/Features`: `Auth`, `Properties`, `Applications`, `Messaging`.
- Frontend:
  - Proyecto ejecutable documentado en `README.md`.
  - Archivos base presentes: `.editorconfig`, `.gitignore`, `README.md`.
  - Estructura por dominios presente en `src/features`: `auth`, `properties`, `applications`, `messaging`.

### F1-T02 - Configurar entorno y variables
Prioridad: P0
Objetivo: estandarizar ejecucion en `dev` y `staging`.
Scope: FULLSTACK
Estado: BE: Done | FE: Done
Evidencia:
- Backend:
  - `.env.example` presente con variables criticas.
  - Validacion fail-fast implementada en `src/Infrastructure/Configuration/EnvironmentValidator.cs`.
  - Carga de `.env` en runtime implementada en `src/Infrastructure/Configuration/DotEnvLoader.cs`.
  - Variables documentadas en `README.md`.
- Frontend:
  - `.env.example` presente con variables publicas criticas.
  - Validacion implementada en `src/config/env.ts`.
  - Variables documentadas en `README.md`.

### F1-T03 - Autenticacion base
Prioridad: P0
Objetivo: autenticacion segura por token.
Scope: FULLSTACK
Estado: BE: Done | FE: Done
Evidencia:
- Backend:
  - JWT bearer configurado en `Program.cs` con issuer de Supabase.
  - Endpoint protegido `GET /auth/me` implementado.
  - Integracion de autenticacion activa via `UseAuthentication`.
- Frontend:
  - Cliente Supabase en `src/features/auth/supabase-client.ts`.
  - Operaciones `signUp`, `signInWithPassword`, `signOut` en `src/features/auth/storage.ts`.
  - Flujo UI en `src/components/top-nav.tsx` y `src/components/auth-dialog.tsx`.

### F1-T04 - Roles y autorizacion
Prioridad: P0
Objetivo: controlar acceso por tipo de usuario.
Scope: FULLSTACK
Estado: BE: Done | FE: Done
Evidencia:
- Backend:
  - Roles tipados: `inquilino`, `propietario`, `admin` en `src/Features/Auth/UserRoles.cs`.
  - Policies por rol en `src/Features/Auth/AuthorizationPolicies.cs`.
  - Endpoints protegidos por policy: `/inquilino`, `/propietario`, `/admin`.
  - Pruebas de autorizacion en `tests/Backend.Alquila.Tests/AuthorizationIntegrationTests.cs`.
- Frontend:
  - Roles y permisos por ruta en `src/features/auth/roles.ts`.
  - Guard de autorizacion en `src/features/auth/route-guard.tsx`.
  - Rutas protegidas en `src/app/admin/page.tsx` y `src/app/propietario/page.tsx`.

### F1-T05 - Modelo de datos inicial + migraciones
Prioridad: P0
Objetivo: base de datos preparada para siguientes fases.
Scope: BE
Estado: Done
Evidencia:
- Migraciones versionadas: `database/migrations/0001_initial_schema.up.sql` y `database/migrations/0001_initial_schema.down.sql`.
- Runner de migraciones: `src/Infrastructure/Persistence/Migrations/MigrationRunner.cs`.
- Comandos documentados: `dotnet run -- migrate up/down`.
- Diagrama de entidades en `docs/data-model.md`.

### F1-T06 - CI basico
Prioridad: P1
Objetivo: evitar regresiones tempranas.
Scope: FULLSTACK
Estado: BE: Done | FE: Done
Evidencia:
- Backend:
  - Workflow CI en `.github/workflows/ci.yml`.
  - Etapas configuradas: lint (`dotnet format`), build (`dotnet build`), test (`dotnet test tests/Backend.Alquila.Tests/Backend.Alquila.Tests.csproj`).
  - Triggers en `pull_request` y `push` a `main`.
- Frontend:
  - Workflow CI en `frontend-alquila/.github/workflows/ci.yml`.
  - Etapas configuradas: lint (`npm run lint`), typecheck (`npm run typecheck`), test (`npm run test --if-present`), build (`npm run build`).
  - Triggers en `pull_request` y `push` a `main`.

### F1-T07 - Despliegue staging minimo
Prioridad: P1
Objetivo: validar flujo real fuera de local.
Scope: BE
Estado: Done
Evidencia:
- Despliegue staging via `Dockerfile`, `render.yaml` y `.github/workflows/deploy-staging.yml`.
- Healthcheck `GET /health` implementado en `Program.cs`.
- Runbook de despliegue en `docs/staging-deploy-runbook.md`.

### F1-T08 - Observabilidad minima
Prioridad: P2
Objetivo: detectar fallos temprano.
Scope: FULLSTACK
Estado: BE: Done | FE: Done
Evidencia:
- Backend:
  - Manejo central de errores con `AddProblemDetails` y `UseExceptionHandler` en `Program.cs`.
  - Variable `SENTRY_DSN` declarada como opcion de observabilidad en `.env.example` y `README.md`.
  - Guia breve de troubleshooting agregada en `docs/troubleshooting-observability.md`.
- Frontend:
  - Logging estructurado en `src/features/observability/logger.ts`.
  - Normalizacion de errores en `src/features/observability/errors.ts`.
  - Manejo de errores no controlados en `src/app/error.tsx`, `src/app/global-error.tsx`, `src/components/global-error-listeners.tsx`.

## Epica F2: Publicacion y busqueda de inmuebles

### F2-T01 - Entidad `properties` completa
Prioridad: P0
Objetivo: almacenar datos de inmueble de forma consistente.
Estado: Done
Evidencia:
- Migracion incremental creada: `database/migrations/0002_properties_full_entity.up.sql` y rollback `database/migrations/0002_properties_full_entity.down.sql`.
- Tabla `properties` extendida con campos de negocio: deposito, habitaciones, banos, m2, amueblado, disponibilidad, tipo de contrato y estado.
- Reglas de integridad agregadas en base de datos con checks (`monthly_price > 0`, `area_m2 > 0`, etc.).
- Indices de busqueda de propiedades por precio y por ciudad+precio: `idx_properties_monthly_price`, `idx_properties_city_monthly_price`.
- Contratos y validacion base de la entidad agregados en `src/Features/Properties/PropertyContracts.cs` y `src/Features/Properties/PropertyValidator.cs`.
- Pruebas de validacion agregadas en `tests/Backend.Alquila.Tests/PropertyValidatorTests.cs`.

### F2-T02 - API crear/editar inmueble (propietario)
Prioridad: P0
Objetivo: que propietarios gestionen sus anuncios.
Scope: FULLSTACK
Estado: BE: Done | FE: Done
Evidencia:
- Backend:
  - Endpoint `POST /properties` implementado en `src/Features/Properties/PropertyEndpoints.cs`.
  - Endpoint `PATCH /properties/{id}` implementado en `src/Features/Properties/PropertyEndpoints.cs`.
  - Repositorio de persistencia PostgreSQL implementado en `src/Features/Properties/NpgsqlPropertiesRepository.cs`.
  - Repositorio registrado en DI en `Program.cs` (`IPropertiesRepository`).
  - Creacion permitida solo para `propietario` y `admin`.
  - Edicion permitida a `admin` o al propietario dueno del inmueble.
  - Validaciones de payload aplicadas para create/patch con `PropertyValidator`.
  - Pruebas de autorizacion agregadas en `tests/Backend.Alquila.Tests/PropertiesAuthorizationIntegrationTests.cs`.
- Frontend:
  - Cliente API autenticado (Bearer Supabase) en `frontend-alquila/src/features/properties/api.ts`.
  - Panel de propietario para crear/editar inmuebles en `frontend-alquila/src/features/properties/property-management-panel.tsx`.
  - Integracion de la ruta protegida en `frontend-alquila/src/app/propietario/page.tsx`.
  - Validacion local FE: `npm run lint`, `npm run typecheck`, `npm run build`.
