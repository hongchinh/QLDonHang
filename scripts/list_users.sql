-- ============================================================
-- LIỆT KÊ USERS VÀ SỐ LƯỢNG DỮ LIỆU LIÊN QUAN
-- ============================================================
SELECT
    u.username,
    u.email,
    u.full_name,
    u.status,
    u.last_login_at,
    u.created_at,
    (SELECT COUNT(*) FROM quotations       WHERE owner_user_id = u.id) AS so_bao_gia,
    (SELECT COUNT(*) FROM refresh_tokens   WHERE user_id       = u.id) AS refresh_tokens,
    (SELECT COUNT(*) FROM notifications    WHERE user_id       = u.id) AS notifications,
    u.id
FROM users u
WHERE u.is_deleted = false
ORDER BY u.created_at DESC;
