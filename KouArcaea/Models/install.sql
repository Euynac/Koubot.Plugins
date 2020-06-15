CREATE TABLE IF NOT EXISTS `plugin_Arcaea_song` (
  `song_id` int PRIMARY KEY AUTO_INCREMENT,
  `song_en_id` varchar(100),
  `song_title` varchar(500),
  `song_artist` varchar(200),
  `song_bpm` varchar(20),
  `song_bpm_base` double,
  `song_pack` varchar(200) COMMENT 'songs collection',
  `song_bg_url` varchar(255),
  `song_length` time,
  `song_side` int,
  `chart_all_notes` int,
  `chart_floor_notes` int,
  `chart_sky_notes` int,
  `chart_hold_notes` int,
  `chart_arc_notes` int,
  `notes_per_second` double,
  `chart_rating_class` int,
  `chart_rating` varchar(10),
  `chart_constant` double,
  `chart_designer` varchar(200),
  `plus_fingers` boolean,
  `jacket_designer` varchar(200),
  `jacket_url` varchar(255),
  `jacket_override` boolean,
  `hidden_until_unlocked` boolean,
  `unlock_in_world_mode` varchar(200),
  `version` varchar(100),
  `date` int,
  `remark` varchar(2000)
);

CREATE TABLE IF NOT EXISTS `plugin_Arcaea_song_another_name` (
  `another_name_id` int PRIMARY KEY AUTO_INCREMENT,
  `another_name` varchar(30),
  `song_en_id` varchar(30)
);