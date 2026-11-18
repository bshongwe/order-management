# Customer management

Customers are created with a name, email address and ISO 3166-1 alpha-2 SADC country code. Country codes are normalized to uppercase and validated against the supported SADC member-country list. Email addresses are unique in the database.

## API
- `POST /api/customers` — create a customer.
- `GET /api/customers/{id}` — retrieve a customer.
- `GET /api/customers?search=&page=1&pageSize=20` — search by name or email and paginate.

Page size is limited to 100. Reads use no-tracking projections and stable ordering.

Supported country codes: AO, BW, KM, CD, SZ, LS, MG, MW, MU, MZ, NA, SC, ZA, TZ, ZM and ZW.

Currency pairing will be enforced at order creation, where the selected currency can be validated against the customer's country.
