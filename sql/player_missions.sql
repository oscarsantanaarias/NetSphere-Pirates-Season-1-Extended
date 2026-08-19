-- CREATE TABLE "player_missions" --------------------------
CREATE TABLE IF NOT EXISTS `player_missions` (
	`Id` Int( 11 ) AUTO_INCREMENT NOT NULL,
	`PlayerId` Int( 11 ) NOT NULL,
	`MissionId` Int( 11 ) NOT NULL,
	`Slot` Int( 11 ) NOT NULL DEFAULT '0',
	`Progress` Int( 11 ) NOT NULL DEFAULT '0',
	`Completed` TinyInt( 1 ) NOT NULL DEFAULT '0',
	PRIMARY KEY ( `Id` ),
	CONSTRAINT `unique_player_mission` UNIQUE( `PlayerId`, `MissionId` ) )
ENGINE = InnoDB;
-- ---------------------------------------------------------

-- for databases that already have the table without the slot column
ALTER TABLE `player_missions` ADD COLUMN IF NOT EXISTS `Slot` Int( 11 ) NOT NULL DEFAULT '0';
