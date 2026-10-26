CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026195026_InitialCreate') THEN
    CREATE TABLE reservations (
        id bigint GENERATED ALWAYS AS IDENTITY,
        public_id uuid NOT NULL DEFAULT (gen_random_uuid()),
        user_public_id uuid NOT NULL,
        status character varying(20) NOT NULL DEFAULT ('HELD'),
        expires_at timestamp with time zone NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        created_by text NOT NULL DEFAULT (CURRENT_USER),
        updated_at timestamp with time zone,
        updated_by text,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        version bigint NOT NULL DEFAULT 1,
        CONSTRAINT "PK_reservations" PRIMARY KEY (id),
        CONSTRAINT reservations_status_check CHECK (status IN ('HELD', 'CONFIRMED', 'EXPIRED', 'CANCELLED')),
        CONSTRAINT reservations_version_check CHECK (version > 0)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026195026_InitialCreate') THEN
    CREATE TABLE ticket_types (
        id bigint GENERATED ALWAYS AS IDENTITY,
        event_public_id uuid NOT NULL,
        event_title_snapshot character varying(250) NOT NULL,
        name character varying(100) NOT NULL,
        description character varying(1000),
        price numeric(18,2) NOT NULL,
        currency char(3) NOT NULL DEFAULT 'UAH',
        total_quantity integer NOT NULL,
        reserved_quantity integer NOT NULL DEFAULT 0,
        sold_quantity integer NOT NULL DEFAULT 0,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        created_by text NOT NULL DEFAULT (CURRENT_USER),
        updated_at timestamp with time zone,
        updated_by text,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        version bigint NOT NULL DEFAULT 1,
        CONSTRAINT "PK_ticket_types" PRIMARY KEY (id),
        CONSTRAINT ticket_types_inventory_check CHECK (reserved_quantity + sold_quantity <= total_quantity),
        CONSTRAINT ticket_types_price_check CHECK (price >= 0),
        CONSTRAINT ticket_types_reserved_quantity_check CHECK (reserved_quantity >= 0),
        CONSTRAINT ticket_types_sold_quantity_check CHECK (sold_quantity >= 0),
        CONSTRAINT ticket_types_total_quantity_check CHECK (total_quantity >= 0),
        CONSTRAINT ticket_types_version_check CHECK (version > 0)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026195026_InitialCreate') THEN
    CREATE TABLE reservation_details (
        reservation_id bigint NOT NULL,
        contact_name_snapshot character varying(200) NOT NULL,
        contact_email_snapshot character varying(320) NOT NULL,
        notes character varying(1000),
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        created_by text NOT NULL DEFAULT (CURRENT_USER),
        updated_at timestamp with time zone,
        updated_by text,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        version bigint NOT NULL DEFAULT 1,
        CONSTRAINT "PK_reservation_details" PRIMARY KEY (reservation_id),
        CONSTRAINT reservation_details_version_check CHECK (version > 0),
        CONSTRAINT "FK_reservation_details_reservations_reservation_id" FOREIGN KEY (reservation_id) REFERENCES reservations (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026195026_InitialCreate') THEN
    CREATE TABLE reservation_items (
        id bigint GENERATED ALWAYS AS IDENTITY,
        event_public_id uuid NOT NULL,
        event_title_snapshot character varying(250) NOT NULL,
        ticket_type_name_snapshot character varying(100) NOT NULL,
        unit_price numeric(18,2) NOT NULL,
        quantity integer NOT NULL,
        reservation_id bigint NOT NULL,
        ticket_type_id bigint NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        created_by text NOT NULL DEFAULT (CURRENT_USER),
        updated_at timestamp with time zone,
        updated_by text,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        version bigint NOT NULL DEFAULT 1,
        CONSTRAINT "PK_reservation_items" PRIMARY KEY (id),
        CONSTRAINT reservation_items_id_ticket_type_key UNIQUE (id, ticket_type_id),
        CONSTRAINT reservation_items_quantity_check CHECK (quantity > 0),
        CONSTRAINT reservation_items_unit_price_check CHECK (unit_price >= 0),
        CONSTRAINT reservation_items_version_check CHECK (version > 0),
        CONSTRAINT "FK_reservation_items_reservations_reservation_id" FOREIGN KEY (reservation_id) REFERENCES reservations (id) ON DELETE CASCADE,
        CONSTRAINT "FK_reservation_items_ticket_types_ticket_type_id" FOREIGN KEY (ticket_type_id) REFERENCES ticket_types (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026195026_InitialCreate') THEN
    CREATE TABLE tickets (
        id bigint GENERATED ALWAYS AS IDENTITY,
        public_id uuid NOT NULL DEFAULT (gen_random_uuid()),
        reservation_item_id bigint NOT NULL,
        ticket_type_id bigint NOT NULL,
        status character varying(20) NOT NULL DEFAULT ('VALID'),
        issued_at timestamp with time zone NOT NULL DEFAULT (now()),
        used_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (now()),
        created_by text NOT NULL DEFAULT (CURRENT_USER),
        updated_at timestamp with time zone,
        updated_by text,
        is_deleted boolean NOT NULL DEFAULT FALSE,
        version bigint NOT NULL DEFAULT 1,
        CONSTRAINT "PK_tickets" PRIMARY KEY (id),
        CONSTRAINT tickets_used_at_check CHECK ((status = 'USED' AND used_at IS NOT NULL) OR (status <> 'USED' AND used_at IS NULL)),
        CONSTRAINT tickets_version_check CHECK (version > 0),
        CONSTRAINT "FK_tickets_reservation_items_reservation_item_id_ticket_type_id" FOREIGN KEY (reservation_item_id, ticket_type_id) REFERENCES reservation_items (id, ticket_type_id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026195026_InitialCreate') THEN
    CREATE INDEX "IX_reservation_details_is_deleted" ON reservation_details (is_deleted);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026195026_InitialCreate') THEN
    CREATE INDEX "IX_reservation_items_is_deleted" ON reservation_items (is_deleted);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026195026_InitialCreate') THEN
    CREATE INDEX "IX_reservation_items_ticket_type_id" ON reservation_items (ticket_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026195026_InitialCreate') THEN
    CREATE UNIQUE INDEX reservation_items_reservation_ticket_type_key ON reservation_items (reservation_id, ticket_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026195026_InitialCreate') THEN
    CREATE INDEX "IX_reservations_is_deleted" ON reservations (is_deleted);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026195026_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_reservations_public_id" ON reservations (public_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026195026_InitialCreate') THEN
    CREATE INDEX reservations_user_status_created_at_idx ON reservations (user_public_id, status, created_at DESC) INCLUDE (public_id, expires_at) WHERE is_deleted = FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026195026_InitialCreate') THEN
    CREATE INDEX "IX_ticket_types_is_deleted" ON ticket_types (is_deleted);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026195026_InitialCreate') THEN
    CREATE INDEX ticket_types_event_active_idx ON ticket_types (event_public_id) INCLUDE (name, price, currency, total_quantity, reserved_quantity, sold_quantity) WHERE is_deleted = FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026195026_InitialCreate') THEN
    CREATE UNIQUE INDEX ticket_types_event_name_key ON ticket_types (event_public_id, name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026195026_InitialCreate') THEN
    CREATE INDEX "IX_tickets_is_deleted" ON tickets (is_deleted);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026195026_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_tickets_public_id" ON tickets (public_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026195026_InitialCreate') THEN
    CREATE INDEX "IX_tickets_reservation_item_id_ticket_type_id" ON tickets (reservation_item_id, ticket_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026195026_InitialCreate') THEN
    CREATE INDEX tickets_reservation_item_status_idx ON tickets (reservation_item_id, status) INCLUDE (public_id, issued_at, used_at) WHERE is_deleted = FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026195026_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261026195026_InitialCreate', '9.0.1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026220421_AddTicketStatusCheck') THEN
    ALTER TABLE tickets ADD CONSTRAINT tickets_status_check CHECK (status IN ('VALID', 'USED', 'REFUNDED'));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261026220421_AddTicketStatusCheck') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261026220421_AddTicketStatusCheck', '9.0.1');
    END IF;
END $EF$;
COMMIT;
