BEGIN;

DO $$
DECLARE
    v_event_public_id UUID := 'a1000000-0000-4000-8000-000000000001';
    v_user_public_id UUID := 'b2000000-0000-4000-8000-000000000001';
    v_reservation_public_id UUID := 'c3000000-0000-4000-8000-000000000001';
    v_ticket_public_id UUID := 'd4000000-0000-4000-8000-000000000001';
    v_ticket_type_id BIGINT;
    v_reservation_id BIGINT;
    v_reservation_item_id BIGINT;
    v_ticket_inserted BOOLEAN := FALSE;
BEGIN
    INSERT INTO ticket_types (
        event_public_id, event_title_snapshot, name, description, price,
        currency, total_quantity
    )
    VALUES (
        v_event_public_id, 'Demo Concert', 'Standard',
        'Standard admission to the demo concert.', 500.00, 'UAH', 100
    )
    ON CONFLICT (event_public_id, name) DO UPDATE
    SET event_title_snapshot = EXCLUDED.event_title_snapshot
    RETURNING id INTO v_ticket_type_id;

    INSERT INTO ticket_types (
        event_public_id, event_title_snapshot, name, description, price,
        currency, total_quantity
    )
    VALUES (
        v_event_public_id, 'Demo Concert', 'VIP',
        'Front-row admission to the demo concert.', 1200.00, 'UAH', 20
    )
    ON CONFLICT (event_public_id, name) DO UPDATE
    SET event_title_snapshot = EXCLUDED.event_title_snapshot;

    INSERT INTO reservations (public_id, user_public_id, status, expires_at)
    VALUES (
        v_reservation_public_id, v_user_public_id, 'CONFIRMED',
        '2099-06-15 17:00:00+03'
    )
    ON CONFLICT (public_id) DO UPDATE
    SET user_public_id = EXCLUDED.user_public_id
    RETURNING id INTO v_reservation_id;

    INSERT INTO reservation_details (
        reservation_id, contact_name_snapshot, contact_email_snapshot, notes
    )
    VALUES (
        v_reservation_id, 'Demo Attendee', 'attendee@example.com',
        'Seed example; contact details are a reservation-time snapshot.'
    )
    ON CONFLICT (reservation_id) DO NOTHING;

    INSERT INTO reservation_items (
        reservation_id, ticket_type_id, event_public_id, event_title_snapshot,
        ticket_type_name_snapshot, unit_price, quantity
    )
    VALUES (
        v_reservation_id, v_ticket_type_id, v_event_public_id,
        'Demo Concert', 'Standard', 500.00, 1
    )
    ON CONFLICT (reservation_id, ticket_type_id) DO UPDATE
    SET event_public_id = EXCLUDED.event_public_id
    RETURNING id INTO v_reservation_item_id;

    INSERT INTO tickets (public_id, reservation_item_id, ticket_type_id)
    VALUES (v_ticket_public_id, v_reservation_item_id, v_ticket_type_id)
    ON CONFLICT (public_id) DO NOTHING;
    v_ticket_inserted := FOUND;

    IF v_ticket_inserted THEN
        UPDATE ticket_types
        SET sold_quantity = sold_quantity + 1,
            updated_at = now(),
            updated_by = current_user,
            version = version + 1
        WHERE id = v_ticket_type_id;
    END IF;
END;
$$;

COMMIT;
