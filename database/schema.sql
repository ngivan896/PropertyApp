PRINT 'Creating tables for Property App (SQL Server)...';

IF OBJECT_ID('dbo.payment_records', 'U') IS NOT NULL DROP TABLE dbo.payment_records;
IF OBJECT_ID('dbo.repair_tickets', 'U') IS NOT NULL DROP TABLE dbo.repair_tickets;
IF OBJECT_ID('dbo.properties', 'U') IS NOT NULL DROP TABLE dbo.properties;
IF OBJECT_ID('dbo.users', 'U') IS NOT NULL DROP TABLE dbo.users;

CREATE TABLE dbo.users
(
    id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    user_name NVARCHAR(80) NOT NULL,
    email NVARCHAR(120) NOT NULL UNIQUE,
    password_hash NVARCHAR(255) NOT NULL,
    role NVARCHAR(20) NOT NULL,
    phone NVARCHAR(25) NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE dbo.properties
(
    id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    owner_id UNIQUEIDENTIFIER NOT NULL,
    address_line NVARCHAR(200) NOT NULL,
    unit_label NVARCHAR(40) NULL,
    property_type NVARCHAR(40) NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT fk_properties_owner FOREIGN KEY (owner_id) REFERENCES dbo.users(id)
);

CREATE TABLE dbo.repair_tickets
(
    id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    title NVARCHAR(120) NOT NULL,
    description NVARCHAR(MAX) NULL,
    status NVARCHAR(30) NOT NULL,
    image_url NVARCHAR(255) NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NULL,
    owner_id UNIQUEIDENTIFIER NOT NULL,
    property_id UNIQUEIDENTIFIER NOT NULL,
    assigned_user_id UNIQUEIDENTIFIER NULL,
    CONSTRAINT fk_tickets_owner FOREIGN KEY (owner_id) REFERENCES dbo.users(id),
    CONSTRAINT fk_tickets_property FOREIGN KEY (property_id) REFERENCES dbo.properties(id),
    CONSTRAINT fk_tickets_assignee FOREIGN KEY (assigned_user_id) REFERENCES dbo.users(id)
);

CREATE TABLE dbo.payment_records
(
    id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    property_id UNIQUEIDENTIFIER NOT NULL,
    owner_id UNIQUEIDENTIFIER NOT NULL,
    amount DECIMAL(10,2) NOT NULL,
    description NVARCHAR(200) NULL,
    recorded_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT fk_payments_property FOREIGN KEY (property_id) REFERENCES dbo.properties(id),
    CONSTRAINT fk_payments_owner FOREIGN KEY (owner_id) REFERENCES dbo.users(id)
);

