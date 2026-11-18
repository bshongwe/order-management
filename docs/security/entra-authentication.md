# Microsoft Entra authentication and authorization

The API validates bearer access tokens issued by Microsoft Entra ID. Configure these values for the API process (environment variables are recommended):

- `Entra__Authority`: the tenant-specific v2.0 authority, for example `https://login.microsoftonline.com/<tenant-id>/v2.0`.
- `Entra__Audience`: the API's Application ID URI or audience expected in the access token.

Do not use an ID token as an API bearer token. Clients must request an access token for this API.

## Permissions

The API accepts permissions as either Entra application roles in the `roles` claim or delegated permissions in the space-delimited `scp` claim:

- `Orders.Read`: list and retrieve customers and orders.
- `Orders.Write`: create customers and orders and execute order lifecycle commands.
- `Orders.Admin`: reserved for administrative operations; no administrative endpoint is exposed yet.

All endpoints require an authenticated principal by default. The health endpoint and service root are explicitly anonymous. Authorization is enforced by endpoint metadata, not by frontend visibility.

For app-only access, assign the relevant application role to the calling service principal. For delegated access, expose and grant the corresponding delegated scopes. Ensure the access token's audience matches `Entra__Audience`.

## Local development

Leave authority/audience unset only when no protected API requests are being made. There is deliberately no insecure development authentication bypass: configure a test Entra tenant or use a separately isolated test authentication handler in automated API tests. Never ship a mock scheme enabled in a deployed environment.
