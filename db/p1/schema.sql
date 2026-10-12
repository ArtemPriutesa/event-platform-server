-- Event Catalog bounded context (PostgreSQL).
-- Public UUIDs are shared as values only; databases have no cross-database FKs.

CREATE TABLE organizers (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    email VARCHAR(320) NOT NULL UNIQUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by TEXT NOT NULL DEFAULT current_user,
    updated_at TIMESTAMPTZ,
    updated_by TEXT,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    version BIGINT NOT NULL DEFAULT 1 CHECK (version > 0)
);

CREATE TABLE categories (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    description VARCHAR(500),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by TEXT NOT NULL DEFAULT current_user,
    updated_at TIMESTAMPTZ,
    updated_by TEXT,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    version BIGINT NOT NULL DEFAULT 1 CHECK (version > 0)
);

CREATE TABLE events (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    public_id UUID NOT NULL DEFAULT gen_random_uuid() UNIQUE,
    organizer_id BIGINT NOT NULL REFERENCES organizers(id),
    category_id BIGINT NOT NULL REFERENCES categories(id),
    title VARCHAR(250) NOT NULL,
    description TEXT,
    starts_at TIMESTAMPTZ NOT NULL,
    ends_at TIMESTAMPTZ NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'DRAFT'
        CHECK (status IN ('DRAFT', 'PUBLISHED', 'FINISHED', 'CANCELLED')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by TEXT NOT NULL DEFAULT current_user,
    updated_at TIMESTAMPTZ,
    updated_by TEXT,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    version BIGINT NOT NULL DEFAULT 1 CHECK (version > 0),
    CONSTRAINT events_valid_time CHECK (ends_at > starts_at),
    CONSTRAINT events_organizer_title_starts_at_key
        UNIQUE (organizer_id, title, starts_at)
);

CREATE TABLE event_venues (
    event_id BIGINT PRIMARY KEY
        REFERENCES events(id) ON DELETE CASCADE,
    venue_name VARCHAR(200) NOT NULL,
    address VARCHAR(400) NOT NULL,
    city VARCHAR(100) NOT NULL,
    country VARCHAR(100) NOT NULL,
    capacity INTEGER NOT NULL CHECK (capacity > 0),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by TEXT NOT NULL DEFAULT current_user,
    updated_at TIMESTAMPTZ,
    updated_by TEXT,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    version BIGINT NOT NULL DEFAULT 1 CHECK (version > 0)
);

CREATE TABLE tags (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name VARCHAR(60) NOT NULL UNIQUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by TEXT NOT NULL DEFAULT current_user,
    updated_at TIMESTAMPTZ,
    updated_by TEXT,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    version BIGINT NOT NULL DEFAULT 1 CHECK (version > 0)
);

CREATE TABLE event_tags (
    event_id BIGINT NOT NULL
        REFERENCES events(id) ON DELETE CASCADE,
    tag_id BIGINT NOT NULL REFERENCES tags(id),
    added_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    added_by TEXT NOT NULL DEFAULT current_user,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by TEXT NOT NULL DEFAULT current_user,
    updated_at TIMESTAMPTZ,
    updated_by TEXT,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    version BIGINT NOT NULL DEFAULT 1 CHECK (version > 0),
    PRIMARY KEY (event_id, tag_id)
);

-- Event discovery by category/status and start time.
CREATE INDEX events_category_status_starts_at_idx
    ON events (category_id, status, starts_at)
    INCLUDE (title, organizer_id)
    WHERE is_deleted = FALSE;

-- Organizer dashboard: upcoming events for one organizer.
CREATE INDEX events_organizer_starts_at_idx
    ON events (organizer_id, starts_at DESC)
    INCLUDE (title, status, category_id)
    WHERE is_deleted = FALSE;

-- Reverse tag lookup: events containing a selected tag.
CREATE INDEX event_tags_tag_event_idx
    ON event_tags (tag_id, event_id)
    WHERE is_deleted = FALSE;
