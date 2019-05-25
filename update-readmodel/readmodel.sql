CREATE DATABASE  IF NOT EXISTS `readmodel` /*!40100 DEFAULT CHARACTER SET latin1 */;
USE `readmodel`;

SET NAMES utf8 ;

DROP TABLE IF EXISTS `CustomerDetails`;
 SET character_set_client = utf8mb4 ;
CREATE TABLE `CustomerDetails` (
  `CustomerId` varchar(36) NOT NULL,
  `AccountBalance` decimal(10,4) DEFAULT NULL,
  `Created` datetime DEFAULT NULL,
  `Delinquent` tinyint(11) DEFAULT NULL,
  `Description` varchar(16000) DEFAULT NULL,
  `Email` varchar(128) DEFAULT NULL,
  `FirstName` varchar(32) DEFAULT NULL,
  `LastName` varchar(32) DEFAULT NULL,
  `Version` int(11) DEFAULT NULL,
  PRIMARY KEY (`CustomerId`),
  KEY `indx_id` (`CustomerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;


DROP TABLE IF EXISTS `CustomerList`;
 SET character_set_client = utf8mb4 ;
CREATE TABLE `CustomerList` (
  `CustomerId` varchar(36) NOT NULL,
  `AccountBalance` decimal(10,4) DEFAULT NULL,
  `FirstName` varchar(32) DEFAULT NULL,
  `LastName` varchar(32) DEFAULT NULL,
  `Version` int(11) DEFAULT NULL,
  PRIMARY KEY (`CustomerId`),
  KEY `indx_id` (`CustomerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;