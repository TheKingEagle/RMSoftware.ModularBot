# Getting Started
You will be required to make a discord application.

## Discord Applications

1. Head over to https://discordapp.com/developers/applications/me
2. Click *New App*
3. Choose an app name, (optional) description, and (Optional) app icon
4. Click *Create App*

##### Screenshots
![App Creator](http://cdn.rms0.org/img/NewApp1.png)
![App Creator](http://cdn.rms0.org/img/NewApp2.png)

## Getting your bot's token
1. Scroll through your app page until you see "Bot". Once there, click *Create a bot user*
2. You choose whether or not to make your app's bot public, but leave OAuth grant unchecked.
3. To get your bot user's token, click *Click to Reveal* token. You can regenerate the token if needed, but you will be required to update the ModularBot app config later.

##### Screenshots
![Bot User](http://cdn.rms0.org/img/NewApp3.png)
![Bot User](http://cdn.rms0.org/img/NewApp4.png)

## Adding bot to your guild
1. Open your browser
2. Take this link, and replace {ClientID} with your bot user's clientID https://discordapp.com/oauth2/authorize?client_id={ClientID}&scope=bot
3. Choose the guild you want to add the bot to.
4. Click *Authorize*

## Required Permissions
The bot will fail to run unless the following permissions are enabled.
1. Read Messages
2. Read Message History
2. Send messagess
3. Embed links
4. Attach files

## Recommended permissions
These permissions are optional, but recommended if you use any additional modules.
1. Access to voice channels
2. Use voice activity
3. Move to voice channels with users.
4. Manage Users (Kick/ban) - For administative/moderation modules

##### Screenshots
![Bot User](http://cdn.rms0.org/img/NewApp5.png)
![Bot User](http://cdn.rms0.org/img/NewApp6.png)

## Starting the ModularBot
When you first start the bot, you will be prompted with a first time setup.
This setup will require a main Channel ID, an Authorization token, and a command prefix. [Typically a single symbol]
Follow the wizard for further instructions.

## Getting your channel ID

Please visit http://rms0.org?a=GuildIDs101 for additional instruction.
