-- Last Chance — MariaDB schema
-- Run once:  mariadb -u root -p < Database/schema.sql

CREATE DATABASE IF NOT EXISTS last_chance
  CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE last_chance;

-- One row per account. Covers registration/login and the admin flag.
CREATE TABLE IF NOT EXISTS players (
    player_id      INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    username       VARCHAR(32)  NOT NULL UNIQUE,
    password_hash  VARCHAR(255) NOT NULL,          -- "iterations.salt.hash" (PBKDF2, never plain text)
    is_admin       BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- One row per completed escape. Every finish is kept; the leaderboard is a query over this.
CREATE TABLE IF NOT EXISTS runs (
    run_id          INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    player_id       INT UNSIGNED NOT NULL,
    finish_time_ms  INT UNSIGNED NOT NULL,         -- milliseconds: exact, sorts correctly, no float rounding
    achieved_at     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_runs_player FOREIGN KEY (player_id)
        REFERENCES players(player_id) ON DELETE CASCADE,
    INDEX idx_runs_time (finish_time_ms),
    INDEX idx_runs_player_time (player_id, finish_time_ms)
);

-- Leaderboard: top 8, one entry per player (their personal best).
-- Ties on time go to whoever got it first.
CREATE OR REPLACE VIEW leaderboard_top8 AS
SELECT username, finish_time_ms, achieved_at
FROM (
    SELECT p.username, r.finish_time_ms, r.achieved_at,
           ROW_NUMBER() OVER (PARTITION BY r.player_id
                              ORDER BY r.finish_time_ms, r.achieved_at) AS rn
    FROM runs r
    JOIN players p ON p.player_id = r.player_id
) best
WHERE rn = 1
ORDER BY finish_time_ms, achieved_at
LIMIT 8;

-- Alternative if the team wants every run eligible (one player can hold several spots):
--   SELECT p.username, r.finish_time_ms, r.achieved_at
--   FROM runs r JOIN players p ON p.player_id = r.player_id
--   ORDER BY r.finish_time_ms, r.achieved_at
--   LIMIT 8;

-- Least-privilege account for the game client (it never needs DROP/ALTER).
-- Change the password, and put the same one in dbconfig.json.
CREATE USER IF NOT EXISTS 'lastchance_game'@'%' IDENTIFIED BY 'CHANGE_ME';
GRANT SELECT, INSERT ON last_chance.players TO 'lastchance_game'@'%';
GRANT SELECT, INSERT ON last_chance.runs    TO 'lastchance_game'@'%';
GRANT SELECT ON last_chance.leaderboard_top8 TO 'lastchance_game'@'%';
FLUSH PRIVILEGES;
