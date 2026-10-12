DO $$
DECLARE
    v_event_id BIGINT;
    v_status VARCHAR(20);
BEGIN
    SELECT id INTO v_event_id
    FROM events
    WHERE public_id = 'a1000000-0000-4000-8000-000000000001'
      AND NOT is_deleted;

    IF v_event_id IS NULL THEN
        RAISE EXCEPTION 'Run the Event Catalog seed before this verification';
    END IF;

    BEGIN
        CALL change_event_status(v_event_id, 'PUBLISHED');
        RAISE EXCEPTION 'Verification failed: invalid transition was accepted'
            USING ERRCODE = 'P0001';
    EXCEPTION
        WHEN check_violation THEN
            RAISE NOTICE 'PASS: invalid event status transition was rejected';
    END;

    SELECT status INTO v_status FROM events WHERE id = v_event_id;
    IF v_status <> 'PUBLISHED' THEN
        RAISE EXCEPTION 'Verification failed: event status changed unexpectedly';
    END IF;

    RAISE NOTICE 'PASS: event remains PUBLISHED after rejected transition';
END;
$$;
