/*
 * Development-only deterministic seed data.
 * Inserts 50 customers and 1,000 draft orders when the demo customer set is absent.
 * Run only after applying EF Core migrations.
 */
SET NOCOUNT ON;
IF EXISTS (SELECT 1 FROM dbo.Customers WHERE Email = 'demo.customer.001@example.test')
BEGIN
    PRINT 'Demo data already exists; skipping.';
    RETURN;
END;

DECLARE @Customers TABLE (Id uniqueidentifier NOT NULL PRIMARY KEY);
DECLARE @i int = 1;
DECLARE @customerId uniqueidentifier;
DECLARE @orderId uniqueidentifier;
DECLARE @sku nvarchar(64);
DECLARE @price decimal(18,2);
DECLARE @quantity int;

WHILE @i <= 50
BEGIN
    SET @customerId = NEWID();
    INSERT dbo.Customers (Id, Name, Email, CountryCode, CreatedAtUtc)
    VALUES (
        @customerId,
        CONCAT('Demo Customer ', RIGHT(CONCAT('000', @i), 3)),
        CONCAT('demo.customer.', RIGHT(CONCAT('000', @i), 3), '@example.test'),
        'ZA',
        SYSUTCDATETIME()
    );
    INSERT @Customers (Id) VALUES (@customerId);
    SET @i += 1;
END;

DECLARE @CustomerCount int = (SELECT COUNT(*) FROM @Customers);
SET @i = 1;
WHILE @i <= 1000
BEGIN
    SELECT @customerId = Id
    FROM (
        SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS RowNumber
        FROM @Customers
    ) customers
    WHERE RowNumber = ((@i - 1) % @CustomerCount) + 1;

    SET @orderId = NEWID();
    SET @sku = CONCAT('DEMO-SKU-', RIGHT(CONCAT('000', ((@i - 1) % 100) + 1), 3));
    SET @price = CONVERT(decimal(18,2), 25 + ((@i * 17) % 975));
    SET @quantity = ((@i - 1) % 5) + 1;

    INSERT dbo.Orders (Id, CustomerReference, Status, CreatedAtUtc, CustomerId, CurrencyCode, TotalAmount)
    VALUES (
        @orderId,
        CONCAT('DEMO-', RIGHT(CONCAT('000000', @i), 6)),
        'Draft',
        DATEADD(MINUTE, -@i, SYSUTCDATETIME()),
        @customerId,
        'ZAR',
        @price * @quantity
    );

    INSERT dbo.OrderLineItems (Id, OrderId, ProductSku, Quantity, UnitPrice)
    VALUES (NEWID(), @orderId, @sku, @quantity, @price);

    SET @i += 1;
END;

PRINT 'Inserted 50 demo customers and 1,000 demo orders.';
