-- ============================================================
-- XÓA DỮ LIỆU TEST CỦA USER (GIỮ NGUYÊN TÀI KHOẢN)
-- ============================================================
-- Xóa: quotations, quotation_lines, quotation_activities,
--      quotation_owner_history, notifications
-- Giữ: user account, user_roles, user_quotation_settings,
--      refresh_tokens, push_subscriptions
-- ============================================================

DO $$
DECLARE
    v_user_id   UUID;
    v_username  TEXT;
    v_email     TEXT;

    -- ⚠️  THAY GIÁ TRỊ NÀY trước khi chạy:
    v_target    TEXT := 'email_hoac_username_can_reset';

    n_activities    INT;
    n_history       INT;
    n_lines         INT;
    n_quotations    INT;
    n_notifications INT;
BEGIN
    SELECT id, username, email
    INTO   v_user_id, v_username, v_email
    FROM   users
    WHERE  email = v_target OR username = v_target
    LIMIT  1;

    IF v_user_id IS NULL THEN
        RAISE EXCEPTION 'Không tìm thấy user với email/username: "%"', v_target;
    END IF;

    RAISE NOTICE '=== RESET DỮ LIỆU: % (%) ===', v_username, v_email;

    DELETE FROM quotation_activities
    WHERE  quotation_id IN (SELECT id FROM quotations WHERE owner_user_id = v_user_id);
    GET DIAGNOSTICS n_activities = ROW_COUNT;
    RAISE NOTICE '  [1] quotation_activities   : % dòng', n_activities;

    DELETE FROM quotation_owner_history
    WHERE  quotation_id IN (SELECT id FROM quotations WHERE owner_user_id = v_user_id)
        OR old_owner_user_id = v_user_id
        OR new_owner_user_id = v_user_id
        OR actor_user_id     = v_user_id;
    GET DIAGNOSTICS n_history = ROW_COUNT;
    RAISE NOTICE '  [2] quotation_owner_history: % dòng', n_history;

    DELETE FROM quotation_lines
    WHERE  quotation_id IN (SELECT id FROM quotations WHERE owner_user_id = v_user_id);
    GET DIAGNOSTICS n_lines = ROW_COUNT;
    RAISE NOTICE '  [3] quotation_lines        : % dòng', n_lines;

    DELETE FROM quotations WHERE owner_user_id = v_user_id;
    GET DIAGNOSTICS n_quotations = ROW_COUNT;
    RAISE NOTICE '  [4] quotations             : % dòng', n_quotations;

    DELETE FROM notifications WHERE user_id = v_user_id;
    GET DIAGNOSTICS n_notifications = ROW_COUNT;
    RAISE NOTICE '  [5] notifications          : % dòng', n_notifications;

    RAISE NOTICE '=== HOÀN TẤT — tài khoản % vẫn còn ===', v_username;
END $$;
