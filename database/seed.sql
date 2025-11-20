PRINT 'Seeding demo data...';

INSERT INTO dbo.users (id, user_name, email, password_hash, role)
VALUES
    ('11111111-1111-1111-1111-111111111111', 'owner_demo', 'owner@example.com', 'changeme-hash', 'Owner'),
    ('22222222-2222-2222-2222-222222222222', 'admin_demo', 'admin@example.com', 'changeme-hash', 'Admin'),
    ('33333333-3333-3333-3333-333333333333', 'tech_demo', 'tech@example.com', 'changeme-hash', 'Worker');

INSERT INTO dbo.properties (id, owner_id, address_line, unit_label, property_type)
VALUES
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111', 'Sunset Avenue 88', 'Unit 12A', 'Condo');

INSERT INTO dbo.repair_tickets (id, title, description, status, owner_id, property_id)
VALUES
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Water leak', 'Kitchen sink leaking', 'Pending', '11111111-1111-1111-1111-111111111111', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');

