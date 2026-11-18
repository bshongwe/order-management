# Orders

New orders require a customer ID, currency and one or more line items. The API resolves the customer and validates the currency against their SADC country.

```http
POST /api/orders
Content-Type: application/json

{
  "customerId": "00000000-0000-0000-0000-000000000000",
  "currencyCode": "ZAR",
  "items": [{ "productSku": "SKU-100", "quantity": 2, "unitPrice": 125.50 }]
}
```

The server calculates line and order totals. Clients cannot submit a total. Monetary values use `decimal(18,2)`; unit prices with more than two decimal places are rejected.

| Country | Accepted currency |
|---|---|
| AO | AOA |
| BW | BWP |
| KM | KMF |
| CD | CDF |
| SZ | SZL, ZAR |
| LS | LSL, ZAR |
| MG | MGA |
| MW | MWK |
| MU | MUR |
| MZ | MZN |
| NA | NAD, ZAR |
| SC | SCR |
| ZA | ZAR |
| TZ | TZS |
| ZM | ZMW |
| ZW | ZWL, USD |

ZAR is accepted for Namibia, Lesotho and Eswatini to reflect their Common Monetary Area arrangements. This is not currency conversion; amounts remain denominated in the order currency.

The migration adds nullable customer, currency and total columns so historical orders remain readable without fabricated identities or currencies. New API-created orders always have a customer ID, currency and at least one line item. The legacy `CustomerReference` column remains for historical records; new rows store the customer ID as its compatibility reference.
