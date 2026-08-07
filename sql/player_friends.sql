CREATE TABLE IF NOT EXISTS `player_friends` (
  `Id` int(11) NOT NULL,
  `PlayerId` int(11) NOT NULL,
  `FriendId` int(11) NOT NULL,
  `PlayerState` int(11) NOT NULL DEFAULT 0,
  `FriendState` int(11) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  KEY `PlayerId` (`PlayerId`) USING BTREE,
  KEY `FriendId` (`FriendId`) USING BTREE,
  CONSTRAINT `player_friends_ibfk_1` FOREIGN KEY (`PlayerId`) REFERENCES `players` (`Id`) ON DELETE CASCADE ON UPDATE NO ACTION,
  CONSTRAINT `player_friends_ibfk_2` FOREIGN KEY (`FriendId`) REFERENCES `players` (`Id`) ON DELETE CASCADE ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
