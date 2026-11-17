-- adds the admin screen permissions to a db you already set up (schema.sql has these already)
-- fine to run more than once
-- run: mariadb -u root -p < Database/admin_upgrade.sql

GRANT DELETE ON last_chance.runs    TO 'lastchance_game'@'%';
GRANT DELETE ON last_chance.players TO 'lastchance_game'@'%';
GRANT UPDATE (is_admin) ON last_chance.players TO 'lastchance_game'@'%';
FLUSH PRIVILEGES;
