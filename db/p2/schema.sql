-- Ticketing bounded context (PostgreSQL / EF Core Code First target).
-- EventPublicId and UserPublicId are external identifiers, never cross-database FKs.

CREATE TABLE ticket_types (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    event_public_id UUID NOT NULL,
    event_title_snapshot VARCHAR(250) NOT NULL,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(1000),
    price NUMERIC(18,2) NOT NULL CHECK (price >= 0),
    currency CHAR(3) NOT NULL DEFAULT 'UAH',
    total_quantity INTEGER NOT NULL CHECK (total_quantity >= 0),
    reserved_quantity INTEGER NOT NULL DEFAULT 0 CHECK (reserved_quantity >= 0),
    sold_quantity INTEGER NOT NULL DEFAULT 0 CHECK (sold_quantity >= 0),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by TEXT NOT NULL DEFAULT current_user,
    updated_at TIMESTAMPTZ,
    updated_by TEXT,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    version BIGINT NOT NULL DEFAULT 1 CHECK (version > 0),
    CONSTRAINT ticket_types_event_name_key UNIQUE (event_public_id, name),
    CONSTRAINT ticket_types_inventory_check
        CHECK (reserved_quantity + sold_quantity <= total_quantity)
);

CREATE TABLE reservations (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    public_id UUID NOT NULL DEFAULT gen_random_uuid() UNIQUE,
    user_public_id UUID NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'HELD'
        CHECK (status IN ('HELD', 'CONFIRMED', 'EXPIRED', 'CANCELLED')),
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by TEXT NOT NULL DEFAULT current_user,
    updated_at TIMESTAMPTZ,
    updated_by TEXT,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    version BIGINT NOT NULL DEFAULT 1 CHECK (version > 0)
);

CREATE TABLE reservation_details (
    reservation_id BIGINT PRIMARY KEY
        REFERENCES reservations(id) ON DELETE CASCADE,
    contact_name_snapshot VARCHAR(200) NOT NULL,
    contact_email_snapshot VARCHAR(320) NOT NULL,
    notes VARCHAR(1000),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by TEXT NOT NULL DEFAULT current_user,
    updated_at TIMESTAMPTZ,
    updated_by TEXT,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    version BIGINT NOT NULL DEFAULT 1 CHECK (version > 0)
);

-- M:N Reservations <-> TicketTypes; each association stores purchase snapshots.
CREATE TABLE reservation_items (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    reservation_id BIGINT NOT NULL
        REFERENCES reservations(id) ON DELETE CASCADE,
    ticket_type_id BIGINT NOT NULL REFERENCES ticket_types(id),
    event_public_id UUID NOT NULL,
    event_title_snapshot VARCHAR(250) NOT NULL,
    ticket_type_name_snapshot VARCHAR(100) NOT NULL,
    unit_price NUMERIC(18,2) NOT NULL CHECK (unit_price >= 0),
    quantity INTEGER NOT NULL CHECK (quantity > 0),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by TEXT NOT NULL DEFAULT current_user,
    updated_at TIMESTAMPTZ,
    updated_by TEXT,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    version BIGINT NOT NULL DEFAULT 1 CHECK (version > 0),
    CONSTRAINT reservation_items_reservation_ticket_type_key
        UNIQUE (reservation_id, ticket_type_id),
    CONSTRAINT reservation_items_id_ticket_type_key
        UNIQUE (id, ticket_type_id)
);

CREATE TABLE tickets (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    public_id UUID NOT NULL DEFAULT gen_random_uuid() UNIQUE,
    reservation_item_id BIGINT NOT NULL,
    ticket_type_id BIGINT NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'VALID'
        CHECK (status IN ('VALID', 'USED', 'REFUNDED')),
    issued_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    used_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by TEXT NOT NULL DEFAULT current_user,
    updated_at TIMESTAMPTZ,
    updated_by TEXT,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    version BIGINT NOT NULL DEFAULT 1 CHECK (version > 0),
    CONSTRAINT tickets_reservation_item_ticket_type_fkey
        FOREIGN KEY (reservation_item_id, ticket_type_id)
        REFERENCES reservation_items(id, ticket_type_id),
    CONSTRAINT tickets_used_at_check
        CHECK ((status = 'USED' AND used_at IS NOT NULL)
            OR (status <> 'USED' AND used_at IS NULL))
);

-- User reservation history, newest first.
CREATE INDEX reservations_user_status_created_at_idx
    ON reservations (user_public_id, status, created_at DESC)
    INCLUDE (public_id, expires_at)
    WHERE is_deleted = FALSE;

-- Available ticket types for one event; deleted ticket types are excluded.
CREATE INDEX ticket_types_event_active_idx
    ON ticket_types (event_public_id)
    INCLUDE (name, price, currency, total_quantity, reserved_quantity, sold_quantity)
    WHERE is_deleted = FALSE;

-- Ticket lookup within a reservation item. public_id has its own unique index.
CREATE INDEX tickets_reservation_item_status_idx
    ON tickets (reservation_item_id, status)
    INCLUDE (public_id, issued_at, used_at)
    WHERE is_deleted = FALSE;
