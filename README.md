# AWSM-Farm-Protection
AWSM Farm Protection was a group project of myself and three other students. We were given eight weeks to create an origional game idea and then bring it to life in an editor of our choosing.
Our team used Unity Collab in order to create the game, In this repo you will find the script's that I worked on throughout the project. 

The game features a simple round-based system of:
1. Round Begins
2. UFO Appears
3. Abduction Begins
4. Abduction Ends
5. UFO leaves
6. Round Ends
7. Downtime for Upgrades/Minigame/Ammo Collection begins
8. Downtime ends
9. Round Begins

We chose to publish the game through Itch.io, and our Gold Master release version is available here: https://tinythegamedev.itch.io/awsm-farm-protection

### GameManager.cs
It was decided early on in the first two weeks that our game would be managed using a GameManager system. This script functions off a State Machine to establish the current events occuring in the game. 
- GameStart
- GameOver
- PlayerInRound
- PlayerInDowntime
- GamePaused

When the game first starts, we decided to place the player in a special "Tutorial" phase which is a special Downtime that lasts for roughly 1:30 minutes. During which the GameManager executes the IEnumerator HandleTutorial. This plays the audio in order, and assures the length of each audo segment is executed properly. 

### Rotate.cs
The alien mothership sends out rings/orbs to pick up a randomly picked animal, and return that animal to the mothership before completing the abduction. Rotate.cs is on all of our animal objects, and adds a random spin effect to the animal giving it a silly look while being abducted.

### UFO_RingManager.cs
We had origionally during development discussed having abduction rings, and then later due to time constraints left them as abduction orbs.
There is alot of legacy code in the script, as we also had discussed the possability of having different types of abduction rings/orbs. I had begun the groundwork of having several types, however unfortunatly due to time constraints and to keep the project within scope we ultimatly scrapped this feature.

The UFO_RingManager functions off a State machine for:
- Seeking
- Sleeping
- Returning

When the Orb/Ring is instantiated, it seeks out an animal chosen randomly by the GameManager.
After the animal has been reached, it is placed inside the orb/ring as a child and put into a Sleeping state allowing for a short period of time for the player to react. 
The orb/ring then returns to the mothership. Upon reaching the mothership, the orb/ring reports to the GameManager that the abduction was successful. 
If the player is able to shoot the animal outside the bounds of the orb/ring, then the orb/ring reports to the GameManager that the abduction was failed. 

###### This README is a work in progress.
