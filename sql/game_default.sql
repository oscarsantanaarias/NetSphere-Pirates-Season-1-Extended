-- Valentina Studio --
-- MySQL dump --
-- ---------------------------------------------------------


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
-- ---------------------------------------------------------


-- CREATE DATABASE "s1db game" -----------------------------
CREATE DATABASE IF NOT EXISTS `s1db game` CHARACTER SET utf8 COLLATE utf8_general_ci;
USE `s1db game`;
-- ---------------------------------------------------------


-- CREATE TABLE "accounts" ---------------------------------
CREATE TABLE `accounts` ( 
	`Id` Int( 11 ) AUTO_INCREMENT NOT NULL,
	`Username` VarChar( 40 ) COLLATE utf8_general_ci NOT NULL,
	`Nickname` VarChar( 40 ) COLLATE utf8_general_ci NULL,
	`Password` VarChar( 40 ) COLLATE utf8_general_ci NULL,
	`Salt` VarChar( 40 ) COLLATE utf8_general_ci NULL,
	`SecurityLevel` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	PRIMARY KEY ( `Id` ),
	CONSTRAINT `Nickname` UNIQUE( `Nickname` ),
	CONSTRAINT `Username` UNIQUE( `Username` ) )
COLLATE = utf8_bin
ENGINE = InnoDB
AUTO_INCREMENT = 4;
-- ---------------------------------------------------------


-- CREATE TABLE "bans" -------------------------------------
CREATE TABLE `bans` ( 
	`Id` Int( 11 ) AUTO_INCREMENT NOT NULL,
	`AccountId` Int( 11 ) NOT NULL,
	`Date` BigInt( 20 ) NOT NULL DEFAULT '0',
	`Duration` BigInt( 20 ) NULL,
	`Reason` VarChar( 255 ) COLLATE utf8_general_ci NULL,
	PRIMARY KEY ( `Id` ) )
COLLATE = utf8_bin
ENGINE = InnoDB
AUTO_INCREMENT = 1;
-- ---------------------------------------------------------


-- CREATE TABLE "license_rewards" --------------------------
CREATE TABLE `license_rewards` ( 
	`Id` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	`ShopItemInfoId` Int( 11 ) NOT NULL,
	`ShopPriceId` Int( 11 ) NOT NULL,
	`Color` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	PRIMARY KEY ( `Id` ) )
CHARACTER SET = utf8mb4
COLLATE = utf8mb4_general_ci
ENGINE = InnoDB;
-- ---------------------------------------------------------


-- CREATE TABLE "login_history" ----------------------------
CREATE TABLE `login_history` ( 
	`Id` Int( 11 ) AUTO_INCREMENT NOT NULL,
	`AccountId` Int( 11 ) NOT NULL,
	`Date` BigInt( 20 ) NOT NULL DEFAULT '0',
	`IP` VarChar( 15 ) NULL,
	PRIMARY KEY ( `Id` ) )
COLLATE = utf8_bin
ENGINE = InnoDB
AUTO_INCREMENT = 6;
-- ---------------------------------------------------------


-- CREATE TABLE "nickname_history" -------------------------
CREATE TABLE `nickname_history` ( 
	`Id` Int( 11 ) AUTO_INCREMENT NOT NULL,
	`AccountId` Int( 11 ) NOT NULL,
	`Nickname` VarChar( 40 ) COLLATE utf8_general_ci NOT NULL,
	`ExpireDate` BigInt( 20 ) NULL,
	PRIMARY KEY ( `Id` ) )
COLLATE = utf8_bin
ENGINE = InnoDB
AUTO_INCREMENT = 1;
-- ---------------------------------------------------------


-- CREATE TABLE "player_characters" ------------------------
CREATE TABLE `player_characters` ( 
	`Id` Int( 11 ) NOT NULL,
	`PlayerId` Int( 11 ) NOT NULL,
	`Slot` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	`Gender` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	`BasicHair` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	`BasicFace` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	`BasicShirt` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	`BasicPants` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	`Weapon1Id` Int( 11 ) NULL,
	`Weapon2Id` Int( 11 ) NULL,
	`Weapon3Id` Int( 11 ) NULL,
	`SkillId` Int( 11 ) NULL,
	`HairId` Int( 11 ) NULL,
	`FaceId` Int( 11 ) NULL,
	`ShirtId` Int( 11 ) NULL,
	`PantsId` Int( 11 ) NULL,
	`GlovesId` Int( 11 ) NULL,
	`ShoesId` Int( 11 ) NULL,
	`AccessoryId` Int( 11 ) NULL,
	PRIMARY KEY ( `Id` ) )
CHARACTER SET = utf8mb4
COLLATE = utf8mb4_general_ci
ENGINE = InnoDB;
-- ---------------------------------------------------------


-- CREATE TABLE "player_deny" ------------------------------
CREATE TABLE `player_deny` ( 
	`Id` Int( 11 ) NOT NULL,
	`PlayerId` Int( 11 ) NOT NULL,
	`DenyPlayerId` Int( 11 ) NOT NULL,
	PRIMARY KEY ( `Id` ) )
CHARACTER SET = utf8mb4
COLLATE = utf8mb4_general_ci
ENGINE = InnoDB;
-- ---------------------------------------------------------


-- CREATE TABLE "player_items" -----------------------------
CREATE TABLE `player_items` ( 
	`Id` Int( 11 ) NOT NULL,
	`PlayerId` Int( 11 ) NOT NULL,
	`ShopItemInfoId` Int( 11 ) NOT NULL,
	`ShopPriceId` Int( 11 ) NOT NULL,
	`Effect` Int( 11 ) NOT NULL DEFAULT '0',
	`Color` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	`PurchaseDate` BigInt( 20 ) NOT NULL DEFAULT '0',
	`Durability` Int( 11 ) NOT NULL DEFAULT '0',
	`Count` Int( 11 ) NOT NULL DEFAULT '0',
	PRIMARY KEY ( `Id` ) )
CHARACTER SET = utf8mb4
COLLATE = utf8mb4_general_ci
ENGINE = InnoDB;
-- ---------------------------------------------------------


-- CREATE TABLE "player_licenses" --------------------------
CREATE TABLE `player_licenses` ( 
	`Id` Int( 11 ) NOT NULL,
	`PlayerId` Int( 11 ) NOT NULL,
	`License` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	`FirstCompletedDate` BigInt( 20 ) NOT NULL DEFAULT '0',
	`CompletedCount` Int( 11 ) NOT NULL DEFAULT '0',
	PRIMARY KEY ( `Id` ) )
CHARACTER SET = utf8mb4
COLLATE = utf8mb4_general_ci
ENGINE = InnoDB;
-- ---------------------------------------------------------


-- CREATE TABLE "player_mails" -----------------------------
CREATE TABLE `player_mails` ( 
	`Id` Int( 11 ) AUTO_INCREMENT NOT NULL,
	`PlayerId` Int( 11 ) NOT NULL,
	`SenderPlayerId` Int( 11 ) NOT NULL,
	`SentDate` BigInt( 20 ) NOT NULL DEFAULT '0',
	`Title` VarChar( 100 ) NOT NULL,
	`Message` VarChar( 500 ) NOT NULL,
	`IsMailNew` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	`IsMailDeleted` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	PRIMARY KEY ( `Id` ) )
CHARACTER SET = utf8mb4
COLLATE = utf8mb4_general_ci
ENGINE = InnoDB
AUTO_INCREMENT = 10;
-- ---------------------------------------------------------


-- CREATE TABLE "player_settings" --------------------------
CREATE TABLE `player_settings` ( 
	`Id` Int( 11 ) AUTO_INCREMENT NOT NULL,
	`PlayerId` Int( 11 ) NOT NULL,
	`Setting` VarChar( 512 ) NOT NULL,
	`Value` VarChar( 512 ) NOT NULL,
	PRIMARY KEY ( `Id` ) )
CHARACTER SET = utf8mb4
COLLATE = utf8mb4_general_ci
ENGINE = InnoDB
AUTO_INCREMENT = 18049;
-- ---------------------------------------------------------


-- CREATE TABLE "players" ----------------------------------
CREATE TABLE `players` ( 
	`Id` Int( 11 ) NOT NULL,
	`TutorialState` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	`Level` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	`TotalExperience` Int( 11 ) NOT NULL DEFAULT '63703100',
	`PEN` Int( 11 ) NOT NULL DEFAULT '0',
	`AP` Int( 11 ) NOT NULL DEFAULT '0',
	`Coins1` Int( 11 ) NOT NULL DEFAULT '0',
	`Coins2` Int( 11 ) NOT NULL DEFAULT '0',
	`CurrentCharacterSlot` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	PRIMARY KEY ( `Id` ) )
COLLATE = utf8_bin
ENGINE = InnoDB;
-- ---------------------------------------------------------


-- CREATE TABLE "shop_effect_groups" -----------------------
CREATE TABLE `shop_effect_groups` ( 
	`Id` Int( 11 ) AUTO_INCREMENT NOT NULL,
	`Name` VarChar( 20 ) NOT NULL,
	PRIMARY KEY ( `Id` ) )
CHARACTER SET = utf8mb4
COLLATE = utf8mb4_general_ci
ENGINE = InnoDB
AUTO_INCREMENT = 27;
-- ---------------------------------------------------------


-- CREATE TABLE "shop_effects" -----------------------------
CREATE TABLE `shop_effects` ( 
	`Id` Int( 11 ) AUTO_INCREMENT NOT NULL,
	`EffectGroupId` Int( 11 ) NOT NULL,
	`Effect` BigInt( 20 ) NOT NULL DEFAULT '0',
	PRIMARY KEY ( `Id` ) )
CHARACTER SET = utf8mb4
COLLATE = utf8mb4_general_ci
ENGINE = InnoDB
AUTO_INCREMENT = 26;
-- ---------------------------------------------------------


-- CREATE TABLE "shop_iteminfos" ---------------------------
CREATE TABLE `shop_iteminfos` ( 
	`Id` Int( 11 ) AUTO_INCREMENT NOT NULL,
	`ShopItemId` Int( 11 ) UNSIGNED NOT NULL,
	`PriceGroupId` Int( 11 ) NOT NULL,
	`EffectGroupId` Int( 11 ) NOT NULL,
	`DiscountPercentage` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	`IsEnabled` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	PRIMARY KEY ( `Id` ) )
CHARACTER SET = utf8mb4
COLLATE = utf8mb4_general_ci
ENGINE = InnoDB
AUTO_INCREMENT = 1042;
-- ---------------------------------------------------------


-- CREATE TABLE "shop_items" -------------------------------
CREATE TABLE `shop_items` ( 
	`Id` Int( 10 ) UNSIGNED NOT NULL,
	`RequiredGender` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	`RequiredLicense` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	`Colors` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	`UniqueColors` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	`RequiredLevel` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	`LevelLimit` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	`RequiredMasterLevel` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	`IsOneTimeUse` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	`IsDestroyable` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	PRIMARY KEY ( `Id` ) )
CHARACTER SET = utf8mb4
COLLATE = utf8mb4_general_ci
ENGINE = InnoDB;
-- ---------------------------------------------------------


-- CREATE TABLE "shop_price_groups" ------------------------
CREATE TABLE `shop_price_groups` ( 
	`Id` Int( 11 ) AUTO_INCREMENT NOT NULL,
	`Name` VarChar( 20 ) NULL,
	`PriceType` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	PRIMARY KEY ( `Id` ) )
CHARACTER SET = utf8mb4
COLLATE = utf8mb4_general_ci
ENGINE = InnoDB
AUTO_INCREMENT = 3;
-- ---------------------------------------------------------


-- CREATE TABLE "shop_prices" ------------------------------
CREATE TABLE `shop_prices` ( 
	`Id` Int( 11 ) AUTO_INCREMENT NOT NULL,
	`PriceGroupId` Int( 11 ) NOT NULL,
	`PeriodType` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	`Period` Int( 11 ) NOT NULL DEFAULT '0',
	`Price` Int( 11 ) NOT NULL DEFAULT '0',
	`IsRefundable` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	`Durability` Int( 11 ) NOT NULL DEFAULT '0',
	`IsEnabled` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	PRIMARY KEY ( `Id` ) )
CHARACTER SET = utf8mb4
COLLATE = utf8mb4_general_ci
ENGINE = InnoDB
AUTO_INCREMENT = 3;
-- ---------------------------------------------------------


-- CREATE TABLE "shop_version" -----------------------------
CREATE TABLE `shop_version` ( 
	`Id` Int( 11 ) AUTO_INCREMENT NOT NULL,
	`Version` VarChar( 40 ) NOT NULL,
	PRIMARY KEY ( `Id` ) )
CHARACTER SET = utf8mb4
COLLATE = utf8mb4_general_ci
ENGINE = InnoDB
AUTO_INCREMENT = 2;
-- ---------------------------------------------------------


-- CREATE TABLE "start_items" ------------------------------
CREATE TABLE `start_items` ( 
	`Id` Int( 11 ) AUTO_INCREMENT NOT NULL,
	`ShopItemInfoId` Int( 11 ) NOT NULL,
	`ShopPriceId` Int( 11 ) NOT NULL,
	`ShopEffectId` Int( 11 ) NOT NULL,
	`Color` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	`Count` Int( 11 ) NOT NULL DEFAULT '0',
	`RequiredSecurityLevel` TinyInt( 3 ) UNSIGNED NOT NULL DEFAULT '0',
	PRIMARY KEY ( `Id` ) )
CHARACTER SET = utf8mb4
COLLATE = utf8mb4_general_ci
ENGINE = InnoDB
AUTO_INCREMENT = 1;
-- ---------------------------------------------------------


-- CREATE INDEX "AccountId" --------------------------------
CREATE INDEX `AccountId` USING BTREE ON `bans`( `AccountId` );
-- ---------------------------------------------------------


-- CREATE INDEX "ShopItemInfoId" ---------------------------
CREATE INDEX `ShopItemInfoId` USING BTREE ON `license_rewards`( `ShopItemInfoId` );
-- ---------------------------------------------------------


-- CREATE INDEX "ShopPriceId" ------------------------------
CREATE INDEX `ShopPriceId` USING BTREE ON `license_rewards`( `ShopPriceId` );
-- ---------------------------------------------------------


-- CREATE INDEX "AccountId" --------------------------------
CREATE INDEX `AccountId` USING BTREE ON `login_history`( `AccountId` );
-- ---------------------------------------------------------


-- CREATE INDEX "AccountId" --------------------------------
CREATE INDEX `AccountId` USING BTREE ON `nickname_history`( `AccountId` );
-- ---------------------------------------------------------


-- CREATE INDEX "AccessoryId" ------------------------------
CREATE INDEX `AccessoryId` USING BTREE ON `player_characters`( `AccessoryId` );
-- ---------------------------------------------------------


-- CREATE INDEX "FaceId" -----------------------------------
CREATE INDEX `FaceId` USING BTREE ON `player_characters`( `FaceId` );
-- ---------------------------------------------------------


-- CREATE INDEX "GlovesId" ---------------------------------
CREATE INDEX `GlovesId` USING BTREE ON `player_characters`( `GlovesId` );
-- ---------------------------------------------------------


-- CREATE INDEX "HairId" -----------------------------------
CREATE INDEX `HairId` USING BTREE ON `player_characters`( `HairId` );
-- ---------------------------------------------------------


-- CREATE INDEX "PantsId" ----------------------------------
CREATE INDEX `PantsId` USING BTREE ON `player_characters`( `PantsId` );
-- ---------------------------------------------------------


-- CREATE INDEX "PlayerId" ---------------------------------
CREATE INDEX `PlayerId` USING BTREE ON `player_characters`( `PlayerId` );
-- ---------------------------------------------------------


-- CREATE INDEX "ShirtId" ----------------------------------
CREATE INDEX `ShirtId` USING BTREE ON `player_characters`( `ShirtId` );
-- ---------------------------------------------------------


-- CREATE INDEX "ShoesId" ----------------------------------
CREATE INDEX `ShoesId` USING BTREE ON `player_characters`( `ShoesId` );
-- ---------------------------------------------------------


-- CREATE INDEX "SkillId" ----------------------------------
CREATE INDEX `SkillId` USING BTREE ON `player_characters`( `SkillId` );
-- ---------------------------------------------------------


-- CREATE INDEX "Weapon1Id" --------------------------------
CREATE INDEX `Weapon1Id` USING BTREE ON `player_characters`( `Weapon1Id` );
-- ---------------------------------------------------------


-- CREATE INDEX "Weapon2Id" --------------------------------
CREATE INDEX `Weapon2Id` USING BTREE ON `player_characters`( `Weapon2Id` );
-- ---------------------------------------------------------


-- CREATE INDEX "Weapon3Id" --------------------------------
CREATE INDEX `Weapon3Id` USING BTREE ON `player_characters`( `Weapon3Id` );
-- ---------------------------------------------------------


-- CREATE INDEX "DenyPlayerId" -----------------------------
CREATE INDEX `DenyPlayerId` USING BTREE ON `player_deny`( `DenyPlayerId` );
-- ---------------------------------------------------------


-- CREATE INDEX "PlayerId" ---------------------------------
CREATE INDEX `PlayerId` USING BTREE ON `player_deny`( `PlayerId` );
-- ---------------------------------------------------------


-- CREATE INDEX "PlayerId" ---------------------------------
CREATE INDEX `PlayerId` USING BTREE ON `player_items`( `PlayerId` );
-- ---------------------------------------------------------


-- CREATE INDEX "ShopItemInfoId" ---------------------------
CREATE INDEX `ShopItemInfoId` USING BTREE ON `player_items`( `ShopItemInfoId` );
-- ---------------------------------------------------------


-- CREATE INDEX "ShopPriceId" ------------------------------
CREATE INDEX `ShopPriceId` USING BTREE ON `player_items`( `ShopPriceId` );
-- ---------------------------------------------------------


-- CREATE INDEX "PlayerId" ---------------------------------
CREATE INDEX `PlayerId` USING BTREE ON `player_licenses`( `PlayerId` );
-- ---------------------------------------------------------


-- CREATE INDEX "PlayerId" ---------------------------------
CREATE INDEX `PlayerId` USING BTREE ON `player_mails`( `PlayerId` );
-- ---------------------------------------------------------


-- CREATE INDEX "SenderPlayerId" ---------------------------
CREATE INDEX `SenderPlayerId` USING BTREE ON `player_mails`( `SenderPlayerId` );
-- ---------------------------------------------------------


-- CREATE INDEX "PlayerId" ---------------------------------
CREATE INDEX `PlayerId` USING BTREE ON `player_settings`( `PlayerId` );
-- ---------------------------------------------------------


-- CREATE INDEX "EffectGroupId" ----------------------------
CREATE INDEX `EffectGroupId` USING BTREE ON `shop_effects`( `EffectGroupId` );
-- ---------------------------------------------------------


-- CREATE INDEX "EffectGroupId" ----------------------------
CREATE INDEX `EffectGroupId` USING BTREE ON `shop_iteminfos`( `EffectGroupId` );
-- ---------------------------------------------------------


-- CREATE INDEX "PriceGroupId" -----------------------------
CREATE INDEX `PriceGroupId` USING BTREE ON `shop_iteminfos`( `PriceGroupId` );
-- ---------------------------------------------------------


-- CREATE INDEX "ShopItemId" -------------------------------
CREATE INDEX `ShopItemId` USING BTREE ON `shop_iteminfos`( `ShopItemId` );
-- ---------------------------------------------------------


-- CREATE INDEX "PriceGroupId" -----------------------------
CREATE INDEX `PriceGroupId` USING BTREE ON `shop_prices`( `PriceGroupId` );
-- ---------------------------------------------------------


-- CREATE INDEX "ShopEffectId" -----------------------------
CREATE INDEX `ShopEffectId` USING BTREE ON `start_items`( `ShopEffectId` );
-- ---------------------------------------------------------


-- CREATE INDEX "ShopItemInfoId" ---------------------------
CREATE INDEX `ShopItemInfoId` USING BTREE ON `start_items`( `ShopItemInfoId` );
-- ---------------------------------------------------------


-- CREATE INDEX "ShopPriceId" ------------------------------
CREATE INDEX `ShopPriceId` USING BTREE ON `start_items`( `ShopPriceId` );
-- ---------------------------------------------------------


-- CREATE LINK "bans_ibfk_1" -------------------------------
ALTER TABLE `bans`
	ADD CONSTRAINT `bans_ibfk_1` FOREIGN KEY ( `AccountId` )
	REFERENCES `accounts`( `Id` )
	ON DELETE Cascade
	ON UPDATE Restrict;
-- ---------------------------------------------------------


-- CREATE LINK "license_rewards_ibfk_1" --------------------
ALTER TABLE `license_rewards`
	ADD CONSTRAINT `license_rewards_ibfk_1` FOREIGN KEY ( `ShopItemInfoId` )
	REFERENCES `shop_iteminfos`( `Id` )
	ON DELETE Cascade
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "license_rewards_ibfk_2" --------------------
ALTER TABLE `license_rewards`
	ADD CONSTRAINT `license_rewards_ibfk_2` FOREIGN KEY ( `ShopPriceId` )
	REFERENCES `shop_prices`( `Id` )
	ON DELETE Cascade
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "login_history_ibfk_1" ----------------------
ALTER TABLE `login_history`
	ADD CONSTRAINT `login_history_ibfk_1` FOREIGN KEY ( `AccountId` )
	REFERENCES `accounts`( `Id` )
	ON DELETE Cascade
	ON UPDATE Restrict;
-- ---------------------------------------------------------


-- CREATE LINK "nickname_history_ibfk_1" -------------------
ALTER TABLE `nickname_history`
	ADD CONSTRAINT `nickname_history_ibfk_1` FOREIGN KEY ( `AccountId` )
	REFERENCES `accounts`( `Id` )
	ON DELETE Cascade
	ON UPDATE Restrict;
-- ---------------------------------------------------------


-- CREATE LINK "player_characters_ibfk_1" ------------------
ALTER TABLE `player_characters`
	ADD CONSTRAINT `player_characters_ibfk_1` FOREIGN KEY ( `PlayerId` )
	REFERENCES `players`( `Id` )
	ON DELETE Cascade
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "player_characters_ibfk_10" -----------------
ALTER TABLE `player_characters`
	ADD CONSTRAINT `player_characters_ibfk_10` FOREIGN KEY ( `GlovesId` )
	REFERENCES `player_items`( `Id` )
	ON DELETE Set NULL
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "player_characters_ibfk_11" -----------------
ALTER TABLE `player_characters`
	ADD CONSTRAINT `player_characters_ibfk_11` FOREIGN KEY ( `ShoesId` )
	REFERENCES `player_items`( `Id` )
	ON DELETE Set NULL
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "player_characters_ibfk_12" -----------------
ALTER TABLE `player_characters`
	ADD CONSTRAINT `player_characters_ibfk_12` FOREIGN KEY ( `AccessoryId` )
	REFERENCES `player_items`( `Id` )
	ON DELETE Set NULL
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "player_characters_ibfk_2" ------------------
ALTER TABLE `player_characters`
	ADD CONSTRAINT `player_characters_ibfk_2` FOREIGN KEY ( `Weapon1Id` )
	REFERENCES `player_items`( `Id` )
	ON DELETE Set NULL
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "player_characters_ibfk_3" ------------------
ALTER TABLE `player_characters`
	ADD CONSTRAINT `player_characters_ibfk_3` FOREIGN KEY ( `Weapon2Id` )
	REFERENCES `player_items`( `Id` )
	ON DELETE Set NULL
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "player_characters_ibfk_4" ------------------
ALTER TABLE `player_characters`
	ADD CONSTRAINT `player_characters_ibfk_4` FOREIGN KEY ( `Weapon3Id` )
	REFERENCES `player_items`( `Id` )
	ON DELETE Set NULL
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "player_characters_ibfk_5" ------------------
ALTER TABLE `player_characters`
	ADD CONSTRAINT `player_characters_ibfk_5` FOREIGN KEY ( `SkillId` )
	REFERENCES `player_items`( `Id` )
	ON DELETE Set NULL
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "player_characters_ibfk_6" ------------------
ALTER TABLE `player_characters`
	ADD CONSTRAINT `player_characters_ibfk_6` FOREIGN KEY ( `HairId` )
	REFERENCES `player_items`( `Id` )
	ON DELETE Set NULL
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "player_characters_ibfk_7" ------------------
ALTER TABLE `player_characters`
	ADD CONSTRAINT `player_characters_ibfk_7` FOREIGN KEY ( `FaceId` )
	REFERENCES `player_items`( `Id` )
	ON DELETE Set NULL
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "player_characters_ibfk_8" ------------------
ALTER TABLE `player_characters`
	ADD CONSTRAINT `player_characters_ibfk_8` FOREIGN KEY ( `ShirtId` )
	REFERENCES `player_items`( `Id` )
	ON DELETE Set NULL
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "player_characters_ibfk_9" ------------------
ALTER TABLE `player_characters`
	ADD CONSTRAINT `player_characters_ibfk_9` FOREIGN KEY ( `PantsId` )
	REFERENCES `player_items`( `Id` )
	ON DELETE Set NULL
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "player_deny_ibfk_1" ------------------------
ALTER TABLE `player_deny`
	ADD CONSTRAINT `player_deny_ibfk_1` FOREIGN KEY ( `PlayerId` )
	REFERENCES `players`( `Id` )
	ON DELETE Cascade
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "player_deny_ibfk_2" ------------------------
ALTER TABLE `player_deny`
	ADD CONSTRAINT `player_deny_ibfk_2` FOREIGN KEY ( `DenyPlayerId` )
	REFERENCES `players`( `Id` )
	ON DELETE Cascade
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "player_items_ibfk_1" -----------------------
ALTER TABLE `player_items`
	ADD CONSTRAINT `player_items_ibfk_1` FOREIGN KEY ( `PlayerId` )
	REFERENCES `players`( `Id` )
	ON DELETE Cascade
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "player_items_ibfk_2" -----------------------
ALTER TABLE `player_items`
	ADD CONSTRAINT `player_items_ibfk_2` FOREIGN KEY ( `ShopItemInfoId` )
	REFERENCES `shop_iteminfos`( `Id` )
	ON DELETE Cascade
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "player_items_ibfk_3" -----------------------
ALTER TABLE `player_items`
	ADD CONSTRAINT `player_items_ibfk_3` FOREIGN KEY ( `ShopPriceId` )
	REFERENCES `shop_prices`( `Id` )
	ON DELETE Cascade
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "player_licenses_ibfk_1" --------------------
ALTER TABLE `player_licenses`
	ADD CONSTRAINT `player_licenses_ibfk_1` FOREIGN KEY ( `PlayerId` )
	REFERENCES `players`( `Id` )
	ON DELETE Cascade
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "player_mails_ibfk_1" -----------------------
ALTER TABLE `player_mails`
	ADD CONSTRAINT `player_mails_ibfk_1` FOREIGN KEY ( `PlayerId` )
	REFERENCES `players`( `Id` )
	ON DELETE Cascade
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "player_mails_ibfk_2" -----------------------
ALTER TABLE `player_mails`
	ADD CONSTRAINT `player_mails_ibfk_2` FOREIGN KEY ( `SenderPlayerId` )
	REFERENCES `players`( `Id` )
	ON DELETE Cascade
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "player_settings_ibfk_1" --------------------
ALTER TABLE `player_settings`
	ADD CONSTRAINT `player_settings_ibfk_1` FOREIGN KEY ( `PlayerId` )
	REFERENCES `players`( `Id` )
	ON DELETE Cascade
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "shop_effects_ibfk_1" -----------------------
ALTER TABLE `shop_effects`
	ADD CONSTRAINT `shop_effects_ibfk_1` FOREIGN KEY ( `EffectGroupId` )
	REFERENCES `shop_effect_groups`( `Id` )
	ON DELETE Cascade
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "shop_iteminfos_ibfk_2" ---------------------
ALTER TABLE `shop_iteminfos`
	ADD CONSTRAINT `shop_iteminfos_ibfk_2` FOREIGN KEY ( `PriceGroupId` )
	REFERENCES `shop_price_groups`( `Id` )
	ON DELETE Cascade
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "shop_iteminfos_ibfk_3" ---------------------
ALTER TABLE `shop_iteminfos`
	ADD CONSTRAINT `shop_iteminfos_ibfk_3` FOREIGN KEY ( `EffectGroupId` )
	REFERENCES `shop_effect_groups`( `Id` )
	ON DELETE Cascade
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "shop_iteminfos_ibfk_4" ---------------------
ALTER TABLE `shop_iteminfos`
	ADD CONSTRAINT `shop_iteminfos_ibfk_4` FOREIGN KEY ( `ShopItemId` )
	REFERENCES `shop_items`( `Id` )
	ON DELETE Cascade
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "shop_prices_ibfk_1" ------------------------
ALTER TABLE `shop_prices`
	ADD CONSTRAINT `shop_prices_ibfk_1` FOREIGN KEY ( `PriceGroupId` )
	REFERENCES `shop_price_groups`( `Id` )
	ON DELETE Cascade
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "start_items_ibfk_1" ------------------------
ALTER TABLE `start_items`
	ADD CONSTRAINT `start_items_ibfk_1` FOREIGN KEY ( `ShopItemInfoId` )
	REFERENCES `shop_iteminfos`( `Id` )
	ON DELETE Cascade
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "start_items_ibfk_2" ------------------------
ALTER TABLE `start_items`
	ADD CONSTRAINT `start_items_ibfk_2` FOREIGN KEY ( `ShopPriceId` )
	REFERENCES `shop_prices`( `Id` )
	ON DELETE Cascade
	ON UPDATE No Action;
-- ---------------------------------------------------------


-- CREATE LINK "start_items_ibfk_3" ------------------------
ALTER TABLE `start_items`
	ADD CONSTRAINT `start_items_ibfk_3` FOREIGN KEY ( `ShopEffectId` )
	REFERENCES `shop_effects`( `Id` )
	ON DELETE Cascade
	ON UPDATE No Action;
-- ---------------------------------------------------------


/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
-- ---------------------------------------------------------


