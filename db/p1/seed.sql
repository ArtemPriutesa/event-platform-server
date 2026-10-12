BEGIN;

DO $$
DECLARE
    v_organizer_id BIGINT;
    v_category_id BIGINT;
    v_tag_id BIGINT;
    v_event_id BIGINT;
BEGIN
    INSERT INTO organizers (name, email)
    VALUES ('Demo Events', 'hello@demo-events.example')
    ON CONFLICT (email) DO UPDATE SET name = EXCLUDED.name
    RETURNING id INTO v_organizer_id;

    INSERT INTO categories (name, description)
    VALUES ('Concerts', 'Live music events')
    ON CONFLICT (name) DO UPDATE SET description = EXCLUDED.description
    RETURNING id INTO v_category_id;

    INSERT INTO categories (name, description)
    VALUES ('Conferences', 'Professional and technology conferences')
    ON CONFLICT (name) DO UPDATE SET description = EXCLUDED.description;

    INSERT INTO tags (name)
    VALUES ('Live music')
    ON CONFLICT (name) DO UPDATE SET name = EXCLUDED.name
    RETURNING id INTO v_tag_id;

    INSERT INTO events (
        public_id, organizer_id, category_id, title, description, starts_at, ends_at
    )
    VALUES (
        'a1000000-0000-4000-8000-000000000001',
        v_organizer_id,
        v_category_id,
        'Demo Concert',
        'Sample event for the Event Catalog database.',
        '2099-06-15 18:00:00+03',
        '2099-06-15 21:00:00+03'
    )
    ON CONFLICT (organizer_id, title, starts_at)
    DO UPDATE SET description = EXCLUDED.description
    RETURNING id INTO v_event_id;

    INSERT INTO event_venues (
        event_id, venue_name, address, city, country, capacity
    )
    VALUES (
        v_event_id, 'Central Hall', '1 Main Street', 'Kyiv', 'Ukraine', 500
    )
    ON CONFLICT (event_id) DO UPDATE
    SET venue_name = EXCLUDED.venue_name,
        address = EXCLUDED.address,
        city = EXCLUDED.city,
        country = EXCLUDED.country,
        capacity = EXCLUDED.capacity;

    INSERT INTO event_tags (event_id, tag_id)
    VALUES (v_event_id, v_tag_id)
    ON CONFLICT (event_id, tag_id) DO NOTHING;

    IF EXISTS (
        SELECT 1 FROM events WHERE id = v_event_id AND status = 'DRAFT'
    ) THEN
        CALL change_event_status(v_event_id, 'PUBLISHED');
    END IF;
END;
$$;

COMMIT;
