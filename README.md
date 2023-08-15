PubNub Unity Multiplayer Shooter Game
====================================
Welcome to PubNub's Unity Game!

<p align="middle">
  <img src="/Media/in-game.png"/>
</p>

This is a Unity game built using the [Super Multiplayer Shooter Unity](https://assetstore.unity.com/packages/templates/systems/super-multiplayer-shooter-template-124977) game, an online shooting brawler game enhanced with the following PubNub real-time functionality:

* In-App Messaging: Send and receive messages in the lobby and main menu.
* Presence: Detect when users are online/offline
* Friend List: Add, Remove, and Check when new players come online/offline
* Leaderboard: Update player scores after matches
* Language Translation: Translate your messages to a variety of languages using Message Filters.
* Profanity Filtering: Block profane and hateful messages while in-game using Message Filters.
* User Metadata: Search and filter for players and view their usernames via App Context.
* Player movement: Send small, ephemeral updates for player movement and state

While this README is focused on the PubNub functionality added to the game, please review Assets > SuperMultiplayerShooter > Guide.pdf. The developers of the original asset have provided detail instructions on how to play the game itself, how to add your own skins/weapons/bullets, and various settings to adjust in the game.

Note: 
* The game is actively in development and more features are planned to be incorporated in the near future.
* PubNub has purchased a Multi-Entity License for this template from the Unity Asset Store. We are not actively selling the game, but showcasing how to enhance the game with PubNub's Real-Time capabilities.

## Prerequisites
You'll need to perform the following before getting started.

### Get Your PubNub Keys
1. Sign in to your [PubNub Dashboard](https://admin.pubnub.com/). You are now in the Admin Portal.
2. Click on the generated app and keyset or create your own App and Keyset by giving them a name.
3. Enable Presence by clicking on the slider to turn it on. A pop-up will require that you enter in “ENABLE”. Enter “ENABLE” in all caps and then press the “Enable” button. Enable the "Generate Leave on TCP FIN or RST checkbox", which generates leave events when clients close their connection (used to track occupancy and remove non-connected clients in app). You can leave the rest of the default settings.
4. Enable Stream Controller.
5. Enable Message Persistence. Select a region to save your messages.
6. Enable App Context. Select a region to save your metadata.
7. Click on save changes.
8. Save the Pub/Sub Keys.

### Set up Leaderboard Function
The Profanity Filtering, Language Translation, and Leaderboard entries occur in real time by using the Functions feature. Set this up by:
1. Click on the Functions tab on the left-hand side of the portal.
2. Select your App where you would like to enable the Module.
3. Click on Create New Module.
4. Give the module a name.
5. Enter a description of what the Module is doing.
6. Select the keyset you created earlier to add the Module. Click create.
7. You will be creating three functions, one for each of the three features.
8. Give the function a name.
9. Select Before Publish or Fire event type for Translation and Profanity Filtering Functions. Select the After Publish or Fire event type for the Leaderboard Function.
10. Enter the channel name you wish the Function to intercept or update after a message publish. For this tutorial:
* Language Translation Function Channel Name: ```chat.translate.*```
* Profanity Filtering Function Channel Name: ```chat.game.*```
* Leaderboard Updates Function Channel Name: ```score.*```
11. Click the Create button for each Function.
12. You will be brought to the Function overview page, where you can change settings, test, and even monitor the Function when it is interacting with your game for each Function.
13. In the middle of the screen there should be automatically generated JavaScript code. This is a sample "Hello World" function to showcase an example of how a function would work. You can enter your own JavaScript code to have this Function process this data for your keyset.
14. For the Profanity Filtering Function, follow the walkthrough for the code here: https://www.pubnub.com/integrations/tisane-labs-nlp/
15. For the Language Translation Function, follow the walkthrough for the code here: https://www.pubnub.com/integrations/amazon-translate/
16. For the Leaderboard Function Code, enter the following code:
```
//This function takes a string from a unity game that contains either a username AND and a score, OR a refresh message.
//The function then looks at the message and creates a user/score JSON and sends it back. Putting the highest score in 0 and the lowest score in [9]
//If the score submitted is lower than [9] then the messages succeeds without intervention

//sending a refresh will trigger this function without any intervention.

export default (request) => {
    const db = require("kvstore");
    const pubnub = require("pubnub");
    //uncomment if you want to see the raw message
    //console.log(request.message);

    //The format of the message sent is "{\"username\":\"Bob\",\"score\":\"10\",\"refresh\":\"\"}"  and as such we need to parse it 
    //You wont be able to use the test payload until you remove the parse
    // var json = request.message; //uncomment this and comment the line below to be able to use the test payload (as well as debug console)
    var json = JSON.parse(request.message);
    let { username, score } = json;

    //create some arrays to ultimately be able to position the leaderboard correctly - there's more elegant ways to do this, this function is designed to explain
    var scorearrayprevious = [];
    var scorearraynew = [];
    var usernamearraynew = [];
    var usernamearrayprevious = [];

    //db.removeItem("data"); //uncomment this code if you need to wipe the database -- for future you could always send a message to trigger this, but that is out of the scope for this workshop
    db.get("data").then((value) => {
        if(value){
            //console.log("value", value); //uncomment this if you want to see the value 
            let i = 0;
            //we use some and score > item to parse through where the submitted score will sit, if the score is greater than the item we're on, then it get's slotted in to the array at that spot
            value.score.some(item => {
                if(parseFloat(score) > parseFloat(item) || (parseFloat(item) == 0 && score.length > 0)){ //Parse into float since variables are currently strings
                    //Score
                    scorearraynew = value.score.slice(0, i);
                    scorearrayprevious = value.score.slice(i, value.score.length);
                    console.log("values", scorearraynew, scorearrayprevious);
                    scorearraynew.push(score);
                    var newScoreList = scorearraynew.concat(scorearrayprevious);
                    newScoreList.splice(-1,1);
                    
                    //Username
                    usernamearrayprevious = value.username.slice(0, i);
                    usernamearraynew = value.username.slice(i, value.score.length);
                    console.log("values", usernamearrayprevious, usernamearraynew);
                    usernamearrayprevious.push(username);
                    var newUsername = usernamearrayprevious.concat(usernamearraynew);
                    newUsername.splice(-1,1);
                    
                    value.score = newScoreList;
                    value.username = newUsername;
                    //store the 
                    db.set("data", value);
                    
                    return true; //break out of the loop using Array.prototype.some by returning true
               }
                i++;
            });
            //publish the message to a *new* or *different* channel 
            pubnub.publish({
                "channel": "leaderboard_scores",
                "message": value
            }).then((publishResponse) => {
                console.log("publish response", publishResponse);
            });
        } else {
          //Initial Data, used only on the first call
            db.set("data", {
                "username":["---","---","---","---","---","---","---","---","---","---"], 
                "score":["0","0","0","0","0","0","0","0","0","0"]});
        }
    }); 
    return request.ok();
};
```
17. Press the Save button to save the Functions.
18. Click on Restart Module to run the modules.

### Install Unity
Install [Unity](https://store.unity.com/download-nuo) if you do not have it. The editor used for this game is 2021.3.10f1.

## Building

1. Clone the GitHub repository.

	```bash
	git clone https://github.com/PubNubDevelopers/unity-multiplayer-shooter.git
	```  
2. Open the Project in the Unity Hub.
3. Follow the instructions at https://www.pubnub.com/docs/sdks/unity7 to add PubNub and configure it with your application
4. Run the game in the editor.

## Playing the Game
There are three scenes in the game to pay attention to that exist in Assets > SuperMultiplayerShooter. In File > Build Settings, ensure they are in the following order:
<p align="middle">
  <img src="/Media/scene-order.png"/>
</p>

### MainMenu
When players first launch the game, this is the first Scene that loads (also referred to as MainMenu). Most functionality will take place in Assets > SuperMultiplayerShooter > Scripts > SampleMainMenu.cs.

<p align="middle">
  <img src="/Media/main-menu.png"/>
</p>

Players can:
* Host a Custom Game (Lobby, will typically be done this way). In the custom game, players can send messages, as well as send messages that will be translated in real time by selecting the language drop-down menu.

<p align="middle">
  <img src="/Media/lobby.png"/>
</p>

* View Friend list. Add, Remove, and see the Presence status of Friends.
* Search for other users that have logged into the game by clicking on the magnifying glass search icon. Add them as friends
* See the total number of users connected online via the Presence indicator in the top left corner of the screen.
* See the Leaderboard statistics in the bottom left of the screen.

The following files are of focus to review for this scene that pertain to PubNub Functionality.
- Assets > SuperMultiplayerShooter > Scripts > SampleMainMenu.cs
- Assets > SuperMultiplayerShooter > Scripts > ChatSystem.cs
- Assets > SuperMultiplayerShooter > Scripts > PubNubManager.cs
- Assets > SuperMultiplayerShooter > Scripts > MessageModeration.cs

### LoadingScene
The Scene that is loaded between the MainMenu and Game Scenes. No PubNub functionality occurs here.

### Game
The Scene that is loaded where the actual game is played. View the [gameplay video](https://www.youtube.com/watch?v=f6fG-3hO19w) provided by the Super Multiplayer Shooter template developers that showcase gameplay.

<p align="middle">
  <img src="/Media/in-game.png"/>
</p>

Related to PubNub Functionality, Players can:
* Send and send messages via in-app messaging.
* Leaderboard updates are stored, sorted, and updated in real time via Functions.

<p align="middle">
  <img src="/Media/chat.png"/>
</p>

The following files are of focus to review for this scene that pertain to PubNub Functionality.
- Assets > SuperMultiplayerShooter > Scripts > GameChat.cs
- Assets > SuperMultiplayerShooter > Scripts > Connector.cs
- Assets > SuperMultiplayerShooter > Scripts > ControlsManager.cs
- Assets > SuperMultiplayerShooter > Scripts > PlayerController.cs
- Assets > SuperMultiplayerShooter > Scripts > GameManager.cs
- Assets > SuperMultiplayerShooter > Scripts > UIManager.cs

## Links
- PubNub Unity SDK: https://www.pubnub.com/docs/sdks/unity7
- PubNub Unity Resources: https://www.pubnub.com/developers/unity-real-time-developer-path/
- Admin Portal (to obtain Pub/Sub API Keys): https://admin.pubnub.com/
- Super Multiplayer Shooter Template Game: https://assetstore.unity.com/packages/templates/systems/super-multiplayer-shooter-template-124977

## Implementation Notes

### Lobby Implementation

This application uses [PubNub presence state](https://www.pubnub.com/docs/general/presence/presence-state) to create and track lobby ownership, this allows the lobby to be automatically closed when the owner (creator) goes offline for whatever reason.  New players who join the lobby indicate their membership by publishing a [PubNub message](https://www.pubnub.com/docs/general/messages/publish) to the lobby creator, meaning new players can only join lobbies whose owners are online.

An alternative lobby implementation would be to use [PubNub messages](https://www.pubnub.com/docs/general/messages/publish) in conujnction with [message persistence](https://www.pubnub.com/docs/general/storage), so a lobby creator would publish a message in a `lobbies` channel.  Other players could read the lobby state by reading the `lobbies` channel history, along with any associated lobby metadata, stored in [message actions](https://www.pubnub.com/docs/general/messages/actions#retrieving-actions).

### Item Trading

The implementation of item trading in this appliction is deliberately simple.  Typically when receiving a trade request the recipient will be informed through a notification but this demo will instead immediately initiate the trading window and workflow.  One limitation of this streamlined approach worth bearing in mind is that the recipient of your trade must be online. 

## License
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    https://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
