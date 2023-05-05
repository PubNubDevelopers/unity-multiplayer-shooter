PubNub Unity Game Demo
====================================
Welcome to PubNub's Unity Game Demo!

<p align="middle">
  <img src="/Media/in-game.png"/>
</p>

This is a Unity game built using the [Super Multiplayer Shooter Unity](https://assetstore.unity.com/packages/templates/systems/super-multiplayer-shooter-template-124977) game, an online shooting brawler game enhanced with the following PubNub real-time functionality:

* In-App Messaging: Send and receive messages in the lobby and main menu.
* Presence: Detect when knew users are online/offline
* Friend List: Add, Remove, and Check when new players come online/offline
* Leaderboard: Update player scores after matches
* Language Translation: Translate your messages to a variety of languages.
* Profanity Filtering: Block profane and hateful messages while in-game.
* User Metadata: Search and filter for players and view their usernames.

Note: 
* The game is actively in development and more features are planned to be incorporated in the near future.
* PubNub has purchased a Multi Entity License for this template from the Unity Asset Store. We are not actively selling the game, but showcasing how to enhance the game with PubNub's Real-Time capabilities.

## Prerequisites
You'll need to perform the following before getting started.

### Get Your PubNub Keys
1. Sign in to your [PubNub Dashboard](https://admin.pubnub.com/). You are now in the Admin Portal.
2. Click on the generated app and keyset or create your own App and Keyset by giving them a name.
3. Enable Presence by clicking on the slider to turn it on. A pop-up will require that you enter in “ENABLE”. Enter in “ENABLE” in all caps and then press the “Enable” button. Enable the "Generate Leave on TCP FIN or RST checkbox", which generates leave events when clients close their connection (used to track occupancy and remove non-connected clients in app). You can leave the rest of the default settings.
4. Enable App Context.
5. Click on save changes.
6. Save the Pub/Sub Keys.

### Set-up Leaderboard Function
The Profanity Filtering, Language Translation, and Leaderboard entries occur in real time by using the Functions feature. Set this up by:
1. Click on the Functions tab on the left hand side of the portal.
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
//The function then looks at the message and creates a user/score json and sends it back. Putting the highest score in 0 and the lowest score in [9]
//If the score submitted is lower than [9] then trhe messages succeeds without intervention

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

### Create a Photon Account
This game depends on the [Photon Network Engine (PUN v2)](https://www.photonengine.com/pun/) in its current state. You'll need to create a [free account](https://id.photonengine.com/Account) and set-up an application to obtain the AppID necessary to power the multiplayer sync for this game. Save the AppID for later.

## Building

1. Clone the GitHub repository.

	```bash
	git clone https://github.com/PubNubDevelopers/unity-multiplayer-shooter.git
	```  
2. Open the Project in the Unity Hub.
3. In case there are errors thrown related to Photon: You will need to install the photon package, as the current addition is throwing errors when attempting to open the game. Once the repo has been downloaded, perform the following.
-Delete Assets > Photon
-Import the [PUN 2 package](https://assetstore.unity.com/packages/tools/network/pun-2-free-119922) from the Unity Asset Store.
-Once you have downloaded the package, it will ask you to enter in the AppID in the configuration wizard that pops up. Enter the AppId obtained earlier during the Photon account creation.
3. Open Assets > SuperMultiplayerShooter > Scripts > PubNubManager.cs. In the ```InitializePubNub``` function, replace ```SUBSCRIBE_KEY``` and ```PUBLISH_KEY``` with the Pub/Sub keys you obtained earlier, respectively. Save the file.
4. Run the game in the editor.

## Playing the Game
There are three scenes in the game to pay attention to: Assets > SuperMultiplayerShooter:

### MainMenu
When players first launch the game, this is the first Scene that loads (also referred to as MainMenu).

<p align="middle">
  <img src="/Media/main-menu.png"/>
</p>

Players can:
* Search for a Game using Photon's Matchmaking Services
* Host a Custom Game (Lobby, will typically be doing this way). In the custom game, players can send messages, as well send messages that will be translated in real time by selecting the language drop-down menu.

<p align="middle">
  <img src="/Media/lobby.png"/>
</p>

* View Friend list. Add, Remove, and see the Presence status of Friends.
* Search for other users that have logged into the game by clicking on the magnifying glass search icon. Add them as friends
* See the total number of users connected online via the Presence indicator in the top left corner of the screen.
* See the Leaderboard statistics in the bottom left of the screen.

### LoadingScene
The Scene that is loaded between the MainMenu and Game Scenes. No PubNub functionality occurs here.

### Game
The Scene that is loaded where the actual game is played. View the [gameplay video](https://www.youtube.com/watch?v=f6fG-3hO19w) provided by the Super Multiplayer Shooter template developers that showcases gameplay.

<p align="middle">
  <img src="/Media/in-game.png"/>
</p>

Related to PubNub Functionality, Players can:
* Send and send messages via in-app messaging.
* Leaderboard updates stored, sorted, and updated in real time via Functions.

<p align="middle">
  <img src="/Media/chat.png"/>
</p>

## Links
- PubNub Unity SDK: https://developer.dolby.io/demos/GDC-demo-experience/
- PubNub Unity Resources: https://www.pubnub.com/developers/unity-real-time-developer-path/
- Admin Portal (to obtain Pub/Sub API Keys): https://admin.pubnub.com/
- Super Multiplayer Shooter Template Game: https://assetstore.unity.com/packages/templates/systems/super-multiplayer-shooter-template-124977
- PUN 2 Asset: https://assetstore.unity.com/packages/tools/network/pun-2-free-119922
- Photon Account: https://id.photonengine.com/Account/

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
