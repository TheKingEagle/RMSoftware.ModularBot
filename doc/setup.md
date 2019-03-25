# Getting Started
You will be required to make a discord application.

## Discord Applications

1. Head over to https://discordapp.com/developers/applications/
2. Click **New Application**
3. Choose an app name
4. Click **Create**
5. Fill out any other information, then continue below.
##### Screenshots
![Create App](https://cdn.rms0.org/img/docs/mb/001.png)
![Create App](https://cdn.rms0.org/img/docs/mb/002.png)
![Create App](https://cdn.rms0.org/img/docs/mb/003.png)

## Getting your bot's token
1. Scroll through your app page until you see **Bot**. Once there, click **Add Bot**
2. Follow any on screen prompts. then choose whether or not to make your app's bot public, but leave OAuth grant unchecked.
3. Click **Copy Token**

##### Screenshots

![Bot user](https://cdn.rms0.org/img/docs/mb/004.png)
![Bot user](https://cdn.rms0.org/img/docs/mb/005.png)
![Bot user](https://cdn.rms0.org/img/docs/mb/006.png)

## Adding bot to your guild
1. Navigate over to the **Oauth2** section from left navigation.
2. Scroll down to the **OAUTH2 URL GENERATOR**
3. Select **Bot** under **SCOPES**
4. Scroll down to **BOT PERMISSIONS** and select all of the required permissions (SEE BELOW)
5. Click **Copy** on the newly generated link
6. Open that url in browser, and select the guild you want the bot to join. *you will only see guilds you have permission to add a bot*
7. Click **Authorize**

## Required Permissions
The bot will fail to run unless the following permissions are enabled.
1. View Channels
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
![Bot user](https://cdn.rms0.org/img/docs/mb/008_.png)
![Bot user](https://cdn.rms0.org/img/docs/mb/009.png)
![Bot user](https://cdn.rms0.org/img/docs/mb/010.png)

## Starting the ModularBot
When you first start the bot, you will be prompted with a first time setup.
This setup will require a main Channel ID, an Authorization token, and a command prefix. [Typically a single symbol]
Follow the wizard for further instructions.

