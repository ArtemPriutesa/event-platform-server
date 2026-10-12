CREATE OR REPLACE PROCEDURE create_event(
    IN p_organizer_id BIGINT,
    IN p_category_id BIGINT,
    IN p_title VARCHAR(250),
    IN p_description TEXT,
    IN p_starts_at TIMESTAMPTZ,
    IN p_ends_at TIMESTAMPTZ,
    IN p_venue_name VARCHAR(200),
    IN p_address VARCHAR(400),
    IN p_city VARCHAR(100),
    IN p_country VARCHAR(100),
    IN p_capacity INTEGER,
    INOUT p_event_id BIGINT DEFAULT NULL,
    INOUT p_public_id UUID DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    IF p_ends_at <= p_starts_at OR p_capacity <= 0 THEN
        RAISE EXCEPTION 'Event time range or venue capacity is invalid'
            USING ERRCODE = '22023';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM organizers WHERE id = p_organizer_id AND NOT is_deleted
    ) THEN
        RAISE EXCEPTION 'Organizer % was not found', p_organizer_id
            USING ERRCODE = '23503';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM categories WHERE id = p_category_id AND NOT is_deleted
    ) THEN
        RAISE EXCEPTION 'Category % was not found', p_category_id
            USING ERRCODE = '23503';
    END IF;

    INSERT INTO events (
        organizer_id, category_id, title, description, starts_at, ends_at
    )
    VALUES (
        p_organizer_id, p_category_id, p_title, p_description, p_starts_at, p_ends_at
    )
    RETURNING id, public_id INTO p_event_id, p_public_id;

    INSERT INTO event_venues (
        event_id, venue_name, address, city, country, capacity
    )
    VALUES (
        p_event_id, p_venue_name, p_address, p_city, p_country, p_capacity
    );
END;
$$;

CREATE OR REPLACE FUNCTION get_event(p_event_id BIGINT)
RETURNS TABLE (
    id BIGINT,
    public_id UUID,
    title VARCHAR(250),
    description TEXT,
    starts_at TIMESTAMPTZ,
    ends_at TIMESTAMPTZ,
    status VARCHAR(20),
    organizer_id BIGINT,
    organizer_name VARCHAR(200),
    category_id BIGINT,
    category_name VARCHAR(100),
    venue_name VARCHAR(200),
    address VARCHAR(400),
    city VARCHAR(100),
    country VARCHAR(100),
    capacity INTEGER
)
LANGUAGE sql
STABLE
AS $$
    SELECT
        e.id, e.public_id, e.title, e.description, e.starts_at, e.ends_at,
        e.status, e.organizer_id, o.name, e.category_id, c.name,
        v.venue_name, v.address, v.city, v.country, v.capacity
    FROM events AS e
    JOIN organizers AS o ON o.id = e.organizer_id
    JOIN categories AS c ON c.id = e.category_id
    LEFT JOIN event_venues AS v
        ON v.event_id = e.id AND NOT v.is_deleted
    WHERE e.id = p_event_id AND NOT e.is_deleted;
$$;

CREATE OR REPLACE PROCEDURE update_event(
    IN p_event_id BIGINT,
    IN p_category_id BIGINT,
    IN p_title VARCHAR(250),
    IN p_description TEXT,
    IN p_starts_at TIMESTAMPTZ,
    IN p_ends_at TIMESTAMPTZ,
    IN p_venue_name VARCHAR(200),
    IN p_address VARCHAR(400),
    IN p_city VARCHAR(100),
    IN p_country VARCHAR(100),
    IN p_capacity INTEGER
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_updated_rows INTEGER;
BEGIN
    IF p_ends_at <= p_starts_at OR p_capacity <= 0 THEN
        RAISE EXCEPTION 'Event time range or venue capacity is invalid'
            USING ERRCODE = '22023';
    END IF;

    UPDATE events
    SET category_id = p_category_id,
        title = p_title,
        description = p_description,
        starts_at = p_starts_at,
        ends_at = p_ends_at,
        updated_at = now(),
        updated_by = current_user,
        version = version + 1
    WHERE id = p_event_id AND status = 'DRAFT' AND NOT is_deleted;

    GET DIAGNOSTICS v_updated_rows = ROW_COUNT;
    IF v_updated_rows = 0 THEN
        RAISE EXCEPTION 'Only an existing draft event can be updated'
            USING ERRCODE = '23514';
    END IF;

    UPDATE event_venues
    SET venue_name = p_venue_name,
        address = p_address,
        city = p_city,
        country = p_country,
        capacity = p_capacity,
        updated_at = now(),
        updated_by = current_user,
        version = version + 1
    WHERE event_id = p_event_id AND NOT is_deleted;

    GET DIAGNOSTICS v_updated_rows = ROW_COUNT;
    IF v_updated_rows = 0 THEN
        RAISE EXCEPTION 'Event venue details were not found'
            USING ERRCODE = '23514';
    END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE change_event_status(
    IN p_event_id BIGINT,
    IN p_new_status VARCHAR(20)
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_current_status VARCHAR(20);
    v_starts_at TIMESTAMPTZ;
BEGIN
    SELECT status, starts_at
    INTO v_current_status, v_starts_at
    FROM events
    WHERE id = p_event_id AND NOT is_deleted
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Event % was not found', p_event_id
            USING ERRCODE = 'P0002';
    END IF;

    IF NOT (
        (v_current_status = 'DRAFT' AND p_new_status IN ('PUBLISHED', 'CANCELLED'))
        OR (v_current_status = 'PUBLISHED' AND p_new_status IN ('FINISHED', 'CANCELLED'))
    ) THEN
        RAISE EXCEPTION 'Event status transition % -> % is not allowed',
            v_current_status, p_new_status
            USING ERRCODE = '23514';
    END IF;

    IF p_new_status = 'PUBLISHED' AND v_starts_at <= now() THEN
        RAISE EXCEPTION 'An event cannot be published after it has started'
            USING ERRCODE = '23514';
    END IF;

    UPDATE events
    SET status = p_new_status,
        updated_at = now(),
        updated_by = current_user,
        version = version + 1
    WHERE id = p_event_id;
END;
$$;

CREATE OR REPLACE PROCEDURE add_event_tag(
    IN p_event_id BIGINT,
    IN p_tag_id BIGINT
)
LANGUAGE plpgsql
AS $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM events WHERE id = p_event_id AND NOT is_deleted
    ) THEN
        RAISE EXCEPTION 'Event % was not found', p_event_id
            USING ERRCODE = 'P0002';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM tags WHERE id = p_tag_id AND NOT is_deleted
    ) THEN
        RAISE EXCEPTION 'Tag % was not found', p_tag_id
            USING ERRCODE = 'P0002';
    END IF;

    INSERT INTO event_tags (event_id, tag_id)
    VALUES (p_event_id, p_tag_id)
    ON CONFLICT (event_id, tag_id) DO NOTHING;
END;
$$;

CREATE OR REPLACE PROCEDURE soft_delete_event(IN p_event_id BIGINT)
LANGUAGE plpgsql
AS $$
DECLARE
    v_updated_rows INTEGER;
BEGIN
    UPDATE events
    SET is_deleted = TRUE,
        updated_at = now(),
        updated_by = current_user,
        version = version + 1
    WHERE id = p_event_id AND NOT is_deleted;

    GET DIAGNOSTICS v_updated_rows = ROW_COUNT;
    IF v_updated_rows = 0 THEN
        RAISE EXCEPTION 'Event % was not found or is already deleted', p_event_id
            USING ERRCODE = 'P0002';
    END IF;
END;
$$;
