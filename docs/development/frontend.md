# Web application

The React + TypeScript application lives in `web/`. It provides customer onboarding, paginated API-backed customer/order lists, order creation, order detail, and lifecycle actions.

## Local development

1. Start SQL Server and RabbitMQ, and apply the EF Core migrations (see the root README).
2. Start the API with its SQL connection string and Microsoft Entra authority/audience configured.
3. Set the API's `Cors:AllowedOrigins` configuration to include `http://localhost:5173`.
4. Run:

```bash
cd web
npm install
npm run dev
```

Open `http://localhost:5173`. Obtain an API access token from your Microsoft Entra application registration with the `Orders.Read` and `Orders.Write` permissions, then paste it into the in-memory token field. The UI does not persist access tokens in local storage or cookies.

## Docker Compose

The root Compose stack serves the web app at `http://localhost:8082` and the API at `http://localhost:8090`. Configure the Entra settings in `.env` before starting the stack. The web container is a static Nginx deployment; API requests are sent to the host-mapped API URL configured at build time.
