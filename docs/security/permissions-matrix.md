# Permissions Matrix (F1-T04)

Roles:
- `inquilino`
- `propietario`
- `admin`

Protected endpoints:

| Endpoint | inquilino | propietario | admin |
|---|---|---|---|
| `GET /auth/me` | Allow | Allow | Allow |
| `GET /inquilino` | Allow | Deny | Deny |
| `GET /propietario` | Deny | Allow | Deny |
| `GET /admin` | Deny | Deny | Allow |

Role resolution order in JWT claims:
1. `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`
2. `role`
3. `user_role`
4. `app_role`
5. `app_metadata.role`
6. `user_metadata.role`

If no supported role claim is found, authorization is denied.
