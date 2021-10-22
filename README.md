streamdeck-steam-launcher

This Stream deck plugin allows you to select one of your Most Recently Played Games on steam!

Note: This plugin is Windows Specific for now

This plugin uses the GetRecentlyPlayedGames api to handle this. Which means it will required an apiToken as well as your steamID

##Getting a Steam API Token
Sign in with your steam account here to receive your api token:
https://steamcommunity.com/dev/apikey

##Getting your Steam Id
Enter in your Steam Name here to get your steam Id
https://www.steamidfinder.com/

##What is Index
the GetRecentlyPlayedGames api request returns a list of most recently played games. Select an index from 0 -> 2 (3 seems to be the max number of games it returns) to select which game will launch when you hit the button!

#Installation instructions
After builiding the project using your IDE of choice. Simple run the `install.bat` located in `Steam Launcher\bin`. This will restart your stream deck software and prompt you to install the plugin
