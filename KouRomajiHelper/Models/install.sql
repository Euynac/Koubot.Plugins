CREATE TABLE IF NOT EXISTS `plugin_romaji_pair` (
  `id` int AUTO_INCREMENT PRIMARY KEY,
  `romaji_key` varchar(20) unique,
  `zh_value` varchar(20)
);