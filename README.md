### ScoredBot Repository
 ScoredBot Twitch bot. Created by ScoredOne.
 
 #### About
 - Currently version 0.2.1
 - Uses LibVLC, youtube-dl and FFMPEG to download and manage song requests.
 - Made to be a dedicated bot for a streamer to manage all aspects of their stream. From song requests, deep chat management and beyond.
 
 #### Features:
 - Currently Song Requests is the only major feature included with the bot.
 - Offline Song Request system implemented.
 - Caches all secondary playlist songs and the next few songs that will play next.
 - Bot login can be entered via Bot Login -> Enter Bot Login Details.
 - Twitch OAuth can be found here: [Twitch OAuth](https://twitchapps.com/tmi/)
 
 #### Source
 - NUGet packages have been cleaned out to reduce size. 
 - To build and run, go into Manage NUGet Packages and it should prompt at the top to restore.
 - Then clean and rebuild the project and it should run.
 - Support only for 64x. 84x was removed due to FFMPEG limitations.