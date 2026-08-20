-- which arcade stages a player has cleared, one row per stage and difficulty
CREATE TABLE IF NOT EXISTS `player_info_arcade` (
  `PlayerId` int(11) NOT NULL,
  `ClearedStages` tinyint(3) unsigned NOT NULL,
  `Difficulty` tinyint(3) unsigned NOT NULL DEFAULT 1,
  PRIMARY KEY (`PlayerId`,`ClearedStages`,`Difficulty`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
