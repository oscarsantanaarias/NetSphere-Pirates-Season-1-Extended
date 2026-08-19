-- combi (partner) system
CREATE TABLE IF NOT EXISTS `combi` (
  `Id` int(11) NOT NULL,
  `PlayerId` int(11) NOT NULL,
  `CombiPlayerId` int(11) NOT NULL,
  `Exp` bigint(20) NOT NULL DEFAULT 0,
  `Battle` bigint(20) NOT NULL DEFAULT 0,
  `Match` int(11) NOT NULL DEFAULT 0,
  `Win` bigint(20) NOT NULL DEFAULT 0,
  `Defeat` bigint(20) NOT NULL DEFAULT 0,
  `CombiName` varchar(64) NOT NULL DEFAULT '',
  `CombiMate` varchar(64) NOT NULL DEFAULT '',
  `CombiDate` varchar(32) NOT NULL DEFAULT '',
  `State` int(11) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  KEY `PlayerId` (`PlayerId`) USING BTREE,
  KEY `CombiPlayerId` (`CombiPlayerId`) USING BTREE,
  CONSTRAINT `combi_ibfk_1` FOREIGN KEY (`PlayerId`) REFERENCES `players` (`Id`) ON DELETE CASCADE ON UPDATE NO ACTION,
  CONSTRAINT `combi_ibfk_2` FOREIGN KEY (`CombiPlayerId`) REFERENCES `players` (`Id`) ON DELETE CASCADE ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
