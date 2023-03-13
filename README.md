# WORK IN PROGRESS

# PubNub Unity Game Demo

The Super Multiplayer Shooter Unity template game enhanced with PubNub functionality.
Open the project using the Unity Hub.

If you are receiving errors when attempting to open the game, it might be due to the Photon package (multiplayer engine):
-Delete Assets > Photon
-Import the package PUN 2 https://assetstore.unity.com/packages/tools/network/pun-2-free-119922
-Use the following as the App ID when prompted after downloading package: 02239c15-ff26-4f2f-80ad-2b1b9f537d78

There are three scenes in the game to pay attention to: Assets > SuperMultiplayerShooter:
1. MainMenu - when users first launch the game. Can search for a lobby, host a custom lobby (will typically be doing this way), look at buddy list, online users, filter users, etc. PubNub chat, friend list, presence, and leaderboards used here.
2. LoadingScene - scene that is loaded between MainMenu and Game. No PubNub functionality here.
3. Game - the actual game that is played. PubNub chat and scoring system used here.
