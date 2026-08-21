-- the five tables the stats panel reads, one row per player and mode

CREATE TABLE IF NOT EXISTS `player_info_deathmatch` (
  `PlayerId` int(11) NOT NULL,
  `Won` bigint(20) NOT NULL,
  `Loss` bigint(20) NOT NULL,
  `Kills` bigint(20) NOT NULL,
  `KillAssists` bigint(20) NOT NULL,
  `Deaths` bigint(20) NOT NULL,
  `Heal` bigint(20) NOT NULL,
  PRIMARY KEY (`PlayerId`),
  UNIQUE KEY `PlayerId` (`PlayerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

CREATE TABLE IF NOT EXISTS `player_info_touchdown` (
  `PlayerId` int(11) NOT NULL,
  `Won` bigint(20) NOT NULL DEFAULT 0,
  `Loss` bigint(20) NOT NULL DEFAULT 0,
  `TD` bigint(20) NOT NULL DEFAULT 0,
  `TDAssist` bigint(20) NOT NULL DEFAULT 0,
  `Offense` bigint(20) NOT NULL DEFAULT 0,
  `OffenseAssist` bigint(20) NOT NULL DEFAULT 0,
  `Defense` bigint(20) NOT NULL DEFAULT 0,
  `DefenseAssist` bigint(20) NOT NULL DEFAULT 0,
  `Kill` bigint(20) NOT NULL DEFAULT 0,
  `KillAssist` bigint(20) NOT NULL DEFAULT 0,
  `OffenseRebound` bigint(20) NOT NULL DEFAULT 0,
  `Heal` bigint(20) NOT NULL DEFAULT 0,
  PRIMARY KEY (`PlayerId`),
  UNIQUE KEY `PlayerId` (`PlayerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

CREATE TABLE IF NOT EXISTS `player_info_chaser` (
  `PlayerId` int(11) NOT NULL,
  `ChasedWon` bigint(20) NOT NULL DEFAULT 0,
  `ChasedRounds` bigint(20) NOT NULL DEFAULT 0,
  `ChaserWon` bigint(20) NOT NULL DEFAULT 0,
  `ChaserRounds` bigint(20) NOT NULL DEFAULT 0,
  `Kills` bigint(20) NOT NULL DEFAULT 0,
  PRIMARY KEY (`PlayerId`),
  UNIQUE KEY `PlayerId` (`PlayerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

CREATE TABLE IF NOT EXISTS `player_info_battleroyal` (
  `PlayerId` int(11) NOT NULL,
  `Won` bigint(20) NOT NULL DEFAULT 0,
  `Loss` bigint(20) NOT NULL DEFAULT 0,
  `Kills` bigint(20) NOT NULL DEFAULT 0,
  `KillAssists` bigint(20) NOT NULL DEFAULT 0,
  `FirstKilled` bigint(20) NOT NULL DEFAULT 0,
  `FirstPlace` bigint(20) NOT NULL DEFAULT 0,
  PRIMARY KEY (`PlayerId`),
  UNIQUE KEY `PlayerId` (`PlayerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

CREATE TABLE IF NOT EXISTS `player_info_captain` (
  `PlayerId` int(11) NOT NULL,
  `Won` bigint(20) NOT NULL DEFAULT 0,
  `Loss` bigint(20) NOT NULL DEFAULT 0,
  `CPTKilled` bigint(20) NOT NULL DEFAULT 0,
  `CPTCount` bigint(20) NOT NULL DEFAULT 0,
  PRIMARY KEY (`PlayerId`),
  UNIQUE KEY `PlayerId` (`PlayerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;
