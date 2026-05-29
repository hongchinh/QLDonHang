-- ============================================================
-- XÓA USER VÀ TOÀN BỘ DỮ LIỆU LIÊN QUAN (HARD DELETE)
-- ============================================================
-- Cách dùng:
--   1. Thay giá trị v_target bên dưới bằng email hoặc username cần xóa
--   2. Chạy toàn bộ file trong psql hoặc pgAdmin/DBeaver
--
-- Script này xóa cứng (hard delete) theo thứ tự:
--   quotation_activities → quotation_owner_history → quotation_lines
--   → quotations → notifications → push_subscriptions
--   → refresh_tokens → user_quotation_settings → user_roles → users
-- ============================================================

DO $$
DECLARE
    v_user_id    UUID;
    v_username   TEXT;
    v_email      TEXT;

    -- ⚠️  THAY GIÁ TRỊ NÀY trước khi chạy:
    v_target     TEXT := 'email_hoac_username_can_xoa';

    n_activities        INT;
    n_owner_history     INT;
    n_lines             INT;
    n_quotations        INT;
    n_notifications     INT;
    n_push_subs         INT;
    n_tokens            INT;
    n_settings          INT;
    n_roles             INT;
BEGIN
    -- ── Tìm user ──────────────────────────────────────────────
    SELECT id, username, email
    INTO   v_user_id, v_username, v_email
    FROM   users
    WHERE  email = v_target OR username = v_target
    LIMIT  1;

    IF v_user_id IS NULL THEN
        RAISE EXCEPTION 'Không tìm thấy user với email/username: "%"', v_target;
    END IF;

    RAISE NOTICE '=== XÓA USER: % (%) | ID: % ===', v_username, v_email, v_user_id;

    -- ── 1. quotation_activities của quotations thuộc user ──────
    DELETE FROM quotation_activities
    WHERE  quotation_id IN (SELECT id FROM quotations WHERE owner_user_id = v_user_id);
    GET DIAGNOSTICS n_activities = ROW_COUNT;
    RAISE NOTICE '  [1] quotation_activities  : % dòng', n_activities;

    -- ── 2. quotation_owner_history liên quan user ──────────────
    DELETE FROM quotation_owner_history
    WHERE  quotation_id IN (SELECT id FROM quotations WHERE owner_user_id = v_user_id)
        OR old_owner_user_id = v_user_id
        OR new_owner_user_id = v_user_id
        OR actor_user_id     = v_user_id;
    GET DIAGNOSTICS n_owner_history = ROW_COUNT;
    RAISE NOTICE '  [2] quotation_owner_history: % dòng', n_owner_history;

    -- ── 3. quotation_lines của quotations thuộc user ───────────
    DELETE FROM quotation_lines
    WHERE  quotation_id IN (SELECT id FROM quotations WHERE owner_user_id = v_user_id);
    GET DIAGNOSTICS n_lines = ROW_COUNT;
    RAISE NOTICE '  [3] quotation_lines       : % dòng', n_lines;

    -- ── 4. quotations thuộc user ───────────────────────────────
    DELETE FROM quotations WHERE owner_user_id = v_user_id;
    GET DIAGNOSTICS n_quotations = ROW_COUNT;
    RAISE NOTICE '  [4] quotations            : % dòng', n_quotations;

    -- ── 5. notifications ───────────────────────────────────────
    DELETE FROM notifications WHERE user_id = v_user_id;
    GET DIAGNOSTICS n_notifications = ROW_COUNT;
    RAISE NOTICE '  [5] notifications         : % dòng', n_notifications;

    -- ── 6. push_subscriptions ──────────────────────────────────
    DELETE FROM push_subscriptions WHERE user_id = v_user_id;
    GET DIAGNOSTICS n_push_subs = ROW_COUNT;
    RAISE NOTICE '  [6] push_subscriptions    : % dòng', n_push_subs;

    -- ── 7. refresh_tokens ──────────────────────────────────────
    DELETE FROM refresh_tokens WHERE user_id = v_user_id;
    GET DIAGNOSTICS n_tokens = ROW_COUNT;
    RAISE NOTICE '  [7] refresh_tokens        : % dòng', n_tokens;

    -- ── 8. user_quotation_settings ─────────────────────────────
    DELETE FROM user_quotation_settings WHERE user_id = v_user_id;
    GET DIAGNOSTICS n_settings = ROW_COUNT;
    RAISE NOTICE '  [8] user_quotation_settings: % dòng', n_settings;

    -- ── 9. user_roles ──────────────────────────────────────────
    DELETE FROM user_roles WHERE user_id = v_user_id;
    GET DIAGNOSTICS n_roles = ROW_COUNT;
    RAISE NOTICE '  [9] user_roles            : % dòng', n_roles;

    -- ── 10. Xóa user ───────────────────────────────────────────
    DELETE FROM users WHERE id = v_user_id;
    RAISE NOTICE '  [10] users               : 1 dòng (% / %)', v_username, v_email;

    RAISE NOTICE '=== HOÀN TẤT ===';
END $$;
