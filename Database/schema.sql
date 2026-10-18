-- =========================================================
-- Last Chance — MariaDB schema (Contraband Studios)
-- Run: mariadb -u root -p < schema.sql
-- =========================================================

CREATE DATABASE IF NOT EXISTS last_chance
  CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE last_chance;

-- One row per player, holding their fastest escape time.
-- Time stored in milliseconds to avoid float rounding.
CREATE TABLE IF NOT EXISTS leaderboard (
    username      VARCHAR(32)  NOT NULL PRIMARY KEY,
    best_time_ms  INT UNSIGNED NOT NULL,
    achieved_at   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
                               ON UPDATE CURRENT_TIMESTAMP,

    INDEX idx_best_time (best_time_ms)
) ENGINE=InnoDB;

-- =========================================================
-- Queries
-- =========================================================

-- Submit a time (inserts a new player, or keeps the faster of old/new):
--   INSERT INTO leaderboard (username, best_time_ms)
--   VALUES (?, ?)
--   ON DUPLICATE KEY UPDATE
--     best_time_ms = LEAST(best_time_ms, VALUES(best_time_ms));
--
-- Top 10:
--   SELECT username, best_time_ms
--   FROM leaderboard
--   ORDER BY best_time_ms ASC, achieved_at ASC
--   LIMIT 10;
--
-- A player's rank:
--   SELECT COUNT(*) + 1 AS `rank`
--   FROM leaderboard
--   WHERE best_time_ms < (SELECT best_time_ms FROM leaderboard WHERE username = ?);
