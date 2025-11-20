# Database Schema Plan

## Engine Choice
- **Preferred**: Amazon RDS for MySQL 8.0 (meets coursework requirement).
- If RDS instance was created with SQL Server by mistake, recreate using MySQL/PostgreSQL to stay compliant.

## Entities

### users
- `id` (CHAR(26), primary key, ULID for easy sorting)
- `full_name` (VARCHAR 120)
- `email` (VARCHAR 160, unique)
- `password_hash` (VARCHAR 255)
- `role` (ENUM: `owner`, `admin`, `worker`)
- `phone` (VARCHAR 30, nullable)
- `created_at` (TIMESTAMP default CURRENT_TIMESTAMP)
- `status` (ENUM: `active`, `suspended`)

### properties
- `id` (CHAR(26) PK)
- `owner_id` (CHAR(26) FK → users.id)
- `name` (VARCHAR 120)
- `address_line` (VARCHAR 255)
- `unit_no` (VARCHAR 50)
- `city` (VARCHAR 80)
- `state` (VARCHAR 80)
- `postal_code` (VARCHAR 20)
- `created_at` (TIMESTAMP default CURRENT_TIMESTAMP)

### repair_tickets
- `id` (CHAR(26) PK)
- `property_id` (CHAR(26) FK → properties.id)
- `requester_id` (CHAR(26) FK → users.id) // owner who opened ticket
- `assignee_id` (CHAR(26) FK → users.id, nullable) // worker assigned
- `title` (VARCHAR 120)
- `description` (TEXT)
- `status` (ENUM: `pending`, `assigned`, `in_progress`, `completed`, `closed`)
- `priority` (ENUM: `low`, `medium`, `high`)
- `image_url` (VARCHAR 255, nullable)
- `created_at` (TIMESTAMP default CURRENT_TIMESTAMP)
- `updated_at` (TIMESTAMP default CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP)

### payment_records (optional)
- `id` (CHAR(26) PK)
- `property_id` (CHAR(26) FK)
- `payer_id` (CHAR(26) FK → users.id)
- `amount` (DECIMAL(10,2))
- `status` (ENUM: `pending`, `paid`, `failed`, `refunded`)
- `reference_no` (VARCHAR 80)
- `due_date` (DATE)
- `paid_at` (TIMESTAMP nullable)

### ticket_history
- `id` (CHAR(26) PK)
- `ticket_id` (CHAR(26) FK → repair_tickets.id)
- `actor_id` (CHAR(26) FK → users.id)
- `action_type` (ENUM: `created`, `status_change`, `comment`, `attachment`)
- `note` (TEXT)
- `created_at` (TIMESTAMP default CURRENT_TIMESTAMP)

## Sample MySQL DDL
```sql
CREATE TABLE users (
  id CHAR(26) PRIMARY KEY,
  full_name VARCHAR(120) NOT NULL,
  email VARCHAR(160) NOT NULL UNIQUE,
  password_hash VARCHAR(255) NOT NULL,
  role ENUM('owner','admin','worker') NOT NULL,
  phone VARCHAR(30),
  status ENUM('active','suspended') NOT NULL DEFAULT 'active',
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE properties (
  id CHAR(26) PRIMARY KEY,
  owner_id CHAR(26) NOT NULL,
  name VARCHAR(120) NOT NULL,
  address_line VARCHAR(255) NOT NULL,
  unit_no VARCHAR(50),
  city VARCHAR(80),
  state VARCHAR(80),
  postal_code VARCHAR(20),
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_properties_owner FOREIGN KEY (owner_id) REFERENCES users(id)
);

CREATE TABLE repair_tickets (
  id CHAR(26) PRIMARY KEY,
  property_id CHAR(26) NOT NULL,
  requester_id CHAR(26) NOT NULL,
  assignee_id CHAR(26),
  title VARCHAR(120) NOT NULL,
  description TEXT NOT NULL,
  status ENUM('pending','assigned','in_progress','completed','closed') DEFAULT 'pending',
  priority ENUM('low','medium','high') DEFAULT 'medium',
  image_url VARCHAR(255),
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT fk_tickets_property FOREIGN KEY (property_id) REFERENCES properties(id),
  CONSTRAINT fk_tickets_requester FOREIGN KEY (requester_id) REFERENCES users(id),
  CONSTRAINT fk_tickets_assignee FOREIGN KEY (assignee_id) REFERENCES users(id)
);

CREATE TABLE ticket_history (
  id CHAR(26) PRIMARY KEY,
  ticket_id CHAR(26) NOT NULL,
  actor_id CHAR(26) NOT NULL,
  action_type ENUM('created','status_change','comment','attachment') NOT NULL,
  note TEXT,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_history_ticket FOREIGN KEY (ticket_id) REFERENCES repair_tickets(id),
  CONSTRAINT fk_history_actor FOREIGN KEY (actor_id) REFERENCES users(id)
);

CREATE TABLE payment_records (
  id CHAR(26) PRIMARY KEY,
  ticket_id CHAR(26) NOT NULL,
  payer_id CHAR(26) NOT NULL,
  amount DECIMAL(10,2) NOT NULL,
  status ENUM('pending','paid','failed','refunded') DEFAULT 'pending',
  reference_no VARCHAR(80),
  due_date DATETIME,
  paid_at TIMESTAMP NULL,
  CONSTRAINT fk_payment_ticket FOREIGN KEY (ticket_id) REFERENCES repair_tickets(id),
  CONSTRAINT fk_payment_payer FOREIGN KEY (payer_id) REFERENCES users(id)
);
```

### Seed Snippets
```sql
INSERT INTO users (id, full_name, email, password_hash, role)
VALUES
('01HZY31HABCDXYZ1234567890', 'Admin One', 'admin@propertyapp.io', '<hashed>', 'admin'),
('01HZY31JOWNERXYZ123456789', 'Owner Jane', 'owner@propertyapp.io', '<hashed>', 'owner'),
('01HZY31JWORKERXYZ12345678', 'Tech Max', 'worker@propertyapp.io', '<hashed>', 'worker');

INSERT INTO properties (id, owner_id, name, address_line, city, state, postal_code)
VALUES
('01HZYA1PROPXYZ12345678901', '01HZY31JOWNERXYZ123456789', 'Lakeview Condo', '12 Lake Rd', 'Austin', 'TX', '78701');

INSERT INTO repair_tickets (id, property_id, requester_id, assignee_id, title, description, status, priority)
VALUES
('01HZYA2TICKETXYZ12345678', '01HZYA1PROPXYZ12345678901', '01HZY31JOWNERXYZ123456789', '01HZY31JWORKERXYZ12345678',
 'Aircon leaking', 'Water dripping from AC vent', 'assigned', 'high');
```

## Seeding Plan
- Default admin account with temporary password.
- Two sample owners + properties.
- Two technicians.
- At least three repair tickets to demonstrate dashboard statistics.

## Next Actions
1. Confirm RDS engine/version → update `.env` `DB_PROVIDER`.
2. Run the SQL DDL once the endpoint is available.
3. Scaffold EF Core entities and migrations to stay in sync with the physical schema.

