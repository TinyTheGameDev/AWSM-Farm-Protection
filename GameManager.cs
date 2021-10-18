using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//List of Game States
public enum GameState {
    GameStart,
    GameOver,
    PlayerInRound,
    PlayerInDowntime,
    GamePaused
}
public class GameManager : MonoBehaviour
{
    //Round Lifecycle
    //Round Begins -> UFO Appears -> Abduction Begins -> Abduction Ends -> UFO leaves -> Round Ends -> Downtime for Upgrades/Minigame begins -> Downtime ends -> Round Begins

    GameObject player;                                  //Reference to Player gameobject
    GameObject playerGunSnapPoint;                      //Reference to Snap Point on player for the gun
    GameObject playerGun;                               //Reference to Snap Point on player for the gun
    GameObject motherShip;                              //Reference to the Mothership
    GameObject playerUI;                                //Reference to Player UI
    GameObject gameOverUI;                              //Reference to Game Over UI
    GameObject pauseUI;                                 //Reference to Pause UI
    GameObject gun;                                     //Reference to player's gun

    [Header("Round Stats")]
    [SerializeField] GameState currentState;            //Current state the game is in
    GameState previousState;                            //Previous state to return after pause
    bool gameEnded;                                     //Reference to if game has ended. To prevent loops
        //Score & Point Info
    int remainingPoints = 0;                            //How many currency points the player has
    [SerializeField] int totalScore = 0;                //Player's overall total score
    [SerializeField] int pigPower;                      //How much livestock is left on the map
    [SerializeField] int maxPigPower;                   //Reference to the maximum number of livestock
    [SerializeField] int dangerCounter = 0;             //How much livestock is in danger.
    int savedAnimalsThisRound = 0;                      //Tracker for how much livestock was saved in a round
    [SerializeField] int animalsAbducted = 0;           //Reference to how many animals have been abducted
    int maxAnimalsAbducted = 50;                        //Reference to the maximum number of animals that can be abducted before the game ends
        //Round Info
    [SerializeField] int roundCounter = 0;              //What round player is currently on
    [SerializeField] float roundLength;                 //Length of the round.
    [SerializeField] float downtimeLength;              //Length of downtime.
    [SerializeField] float timer = 0f;                  //How much time has elapsed
        //UFO Info
    [Header("UFO Stats")]
    [SerializeField] float ringSpawnLength;             //How often do rings appear
    [SerializeField] float ringTimer = 0f;              //How much time between a ring appearing has elapsed
    [SerializeField] int maxActiveRings = 1;            //Maximum number of rings active at a time
    [SerializeField] int activeRings = 0;               //Tracker for Active rings
    [SerializeField] List<GameObject> activeAnimals;    //List of animal locations, represented by box

    [Header("Bullet Info")]
        //Bullet Info
    [SerializeField] int baseBulletsUsed = 0;           //Tracker for base bullets used.
    [SerializeField] List<GameObject> refillPrefabs;    //Reference to refill objects

    [Header("Prefabs")]
        //UI
    [SerializeField] GameObject gameOverUIPrefab;       //Reference to Game Over prefab
    [SerializeField] GameObject pauseUIPrefab;          //Reference to Pause UI prefab
    [SerializeField] GameObject playerUIPrefab;         //Reference to Player UI prefab
        //Objects
    [SerializeField] GameObject motherShipPrefab;       //Reference to the mothership Prefab
    [SerializeField] GameObject ringPrefab;             //Reference to the ring prefab
    [SerializeField] GameObject playerPrefab;           //Reference to Player Prefab
    [SerializeField] GameObject gunPrefab;              //Reference to Gun Prefab
    [SerializeField] List<GameObject> animalPrefabs;    //Refernece to Animal Prefabs
    [SerializeField] List<GameObject> grassPrefabs;     //Reference to Grass Prefabs
    [SerializeField] List<GameObject> treePrefabs;      //Reference to Tree  Prefabs

    [Header("GUN Voice Acting")] //References to GUN Voice Acting
    [SerializeField] List<AudioClip> introSounds;
    [SerializeField] List<AudioClip> animalEnteredOrbSounds;
    [SerializeField] List<AudioClip> abductionSuccessSounds;
    [SerializeField] AudioClip breakBeginSound;
    [SerializeField] AudioClip break_AnimalShipmentSound;
    [SerializeField] AudioClip breakEndSound;
    [SerializeField] AudioClip breakSkipSound;
    [SerializeField] AudioClip breakTutorialSound;
    [SerializeField] AudioClip gameOverSound;
    [SerializeField] AudioClip fiveLeftSound;
    [SerializeField] AudioClip oneLeftSound;
    [SerializeField] AudioClip halfAbductedSound;
    [SerializeField] AudioClip skipTutorialSound;
    
    //Private random variables
    float mapX = 100f;                                  //Maximum float value of X axis for map
    float mapZ = 100f;                                  //Maximum float value of Z axis for map
    int maxGrassObjects = 300;                          //Reference to maximum number of Grass spawned on map
    int maxTreeObjects = 15;                            //Reference to maximum number of trees spawned on map
    float pigPowerPercent;                              //Percent of pigs on field versus total pig power
    bool rngSpawn = true;                               //Refernece to allowing random spawns
    bool spawnWall = true;                              //Reference to spawning a box of trees
    float tutorialLength = 120f;                        //Reference to the length of the tutorial downtime
    bool playedTutorial = false;                        //Reference to if the Tutorial has played
    bool tutorialActive = false;                        //Refernece to if the Tutorial is playing
    // Start is called before the first frame update
    void Start()
    {
        BeginGame();
        //BeginRound();
        BeginDowntime(true);
    }

    // Update is called once per frame
    void Update()
    {
        //Pause Check
        HandlePauseGame();
        HandleEndDowntime();
        if (currentState != GameState.GamePaused) {
            //Handle Round Timer
            timer -= Time.deltaTime;
            if (timer <= 0f) {
                if (currentState == GameState.PlayerInDowntime) {
                    BeginRound();
                } else if (currentState == GameState.PlayerInRound && activeRings == 0) {
                    BeginDowntime(false);
                }
            }

            //Handle UFO Spawn Timer
            if (currentState == GameState.PlayerInRound && timer >= 0f) {
                ringTimer -= Time.deltaTime;
                if (ringTimer <= 0f && activeRings < maxActiveRings && activeAnimals.Count >= 1) {
                    SpawnRing();
                }
            }

            if (currentState == GameState.GameOver && !gameEnded) {
                EndGame();
            }
        }
    }

    //Begin the game
    void BeginGame() {
        print("GM: Beginning Game");
        //Set the value of pigPower to the maxPigPower
        pigPower = maxPigPower;

        if(spawnWall) {
            //Spawn Game Boundaries
            Vector3 pos = new Vector3(0f, 0f, 0f);
            Vector3 inc = new Vector3(0f, 0f, 0f);

            //Cube (20,4,-20)
            pos = new Vector3(-80, 0, -20);
            inc = new Vector3(2f, 0f, 0f);
            for (int i = 0; i < 100; i++) {
                GameObject t = Instantiate(treePrefabs[1], pos, Quaternion.Euler(0f, 0f, 0f));
                pos += inc;
            }

            //Cube (1) (20,4,150)
            pos = new Vector3(-80, 0, 180);
            inc = new Vector3(2f, 0f, 0f);
            for (int i = 0; i < 100; i++) {
                GameObject t = Instantiate(treePrefabs[1], pos, Quaternion.Euler(0f, 0f, 0f));
                pos += inc;
            }

            //Cube (2) (-80,4,80)
            pos = new Vector3(-80, 0, -20);
            inc = new Vector3(0f, 0f, 2f);
            for (int i = 0; i < 100; i++) {
                GameObject t = Instantiate(treePrefabs[1], pos, Quaternion.Euler(0f, 0f, 0f));
                pos += inc;
            }

            //Cube (3) (120,4,80)
            pos = new Vector3(120, 0, -20);
            inc = new Vector3(0f, 0f, 2f);
            for (int i = 0; i < 100; i++) {
                GameObject t = Instantiate(treePrefabs[1], pos, Quaternion.Euler(0f, 0f, 0f));
                pos += inc;
            }
        }

        if (motherShipPrefab) {
            //Spawn Mothership
            motherShip = Instantiate(motherShipPrefab) as GameObject;
            motherShip.transform.position = new Vector3(20f, 55f, 85f);
            motherShip.name = "Mothership";
        }

        if (playerPrefab) {
            //Spawn Player
            player = Instantiate(playerPrefab) as GameObject;
            player.transform.position = new Vector3(0f, 5f, 0f);
            player.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
            player.name = "Player";
            gun = GameObject.FindGameObjectWithTag("GUN");
        }

        if (playerUIPrefab) {
            //Spawn Player UI
            playerUI = Instantiate(playerUIPrefab) as GameObject;
            playerUI.name = "Player UI";
        }

        //Random Populate Farm when enabled
        if (rngSpawn) {
            //Spawn Grass
            for (int a = 1; a <= maxGrassObjects; a++) {
                int grassID = Random.Range(0, grassPrefabs.Count - 1);
                GameObject grass = Instantiate(grassPrefabs[grassID]);
                grass.transform.localScale = new Vector3(1f, 1f, 1f);
                grass.transform.position = GetRandomPointAtSpecialY(.75f);
            }

            //Spawn Trees
            for (int a = 1; a <= maxTreeObjects; a++) {
                int treeID = Random.Range(0, treePrefabs.Count - 1);
                GameObject tree = Instantiate(treePrefabs[treeID]);
                //tree.transform.localScale = new Vector3(1f, 1f, 1f);
                tree.transform.position = GetRandomPointAtSpecialY(0f);
            }

            //Spawn Animals
            for (int a = 1; a <= pigPower; a++) {
                int animalID = Random.Range(0, animalPrefabs.Count - 1);
                GameObject animal = Instantiate(animalPrefabs[animalID]);
                animal.transform.localScale = new Vector3(.6f, .6f, .6f);
                animal.transform.position = GetRandomPointAtMap();
            }
        }

        //Game begins UI Call
        playerUI.GetComponent<UIManager>().BeginNotificationAlert("Press 'N' on the keyboard to skip the tutorial.");
    }

    //Game Over *Thanos Snap*
    public void EndGame() {
        //Set current game state
        currentState = GameState.GameOver;

        if(gameOverUIPrefab && !gameOverUI) {
            //Spawn Game Over Menu
            gameOverUI = Instantiate(gameOverUIPrefab) as GameObject;
            gameOverUI.name = "GameOver UI";
        }

        //Disable Player Movement
        player.GetComponent<PLayerMovement>().enabled = false;

        //Re-Enable cursor
        Cursor.lockState = CursorLockMode.None;
        Camera.main.GetComponent<MouseLook>().enabled = false;
        Time.timeScale = 0f;
        Cursor.visible = true;

        //Play Audio
        GetComponent<AudioSource>().clip = gameOverSound;
        GetComponent<AudioSource>().Play();

        //Flag GameEnded as true
        gameEnded = true;
    }

    //End Downtime early by keypress
    public void HandleEndDowntime() {
        if (currentState == GameState.PlayerInDowntime && Input.GetKeyDown(KeyCode.N)) {
            if(!playedTutorial) {
                StopCoroutine(HandleTutorial());
                GetComponent<AudioSource>().Stop();
                GetComponent<AudioSource>().clip = skipTutorialSound;
                GetComponent<AudioSource>().Play();
                tutorialActive = false;
                playedTutorial = true;
            }
            timer = 0f;
        }
    }

    //Pause Game.
    void HandlePauseGame() {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) {
            //Set previous state to current state
            if (currentState != GameState.GamePaused) { previousState = currentState; }

            //Handle Pause Logic
            if (currentState != GameState.GamePaused) {
                //Pause the Game
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.Confined;

                //Pause audio
                AudioListener.pause = true;

                //Enable Pause UI
                if (!pauseUI) {
                    pauseUI = Instantiate(pauseUIPrefab) as GameObject;
                    pauseUI.name = "Pause UI";
                } else {
                    pauseUI.SetActive(true);
                }

                //Set Current State
                currentState = GameState.GamePaused;
            } else {
                //Unpause the game
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;

                //Unpause audio
                AudioListener.pause = false;

                //Disable Pause UI
                pauseUI.SetActive(false);

                //Return to previous state
                currentState = previousState;
            }
        }
    }

    //UI Pause Game - Public so it can be called from UI
    public void HandleUIPause(bool quittingGame) {
         //Set previous state to current state
         if (currentState != GameState.GamePaused) { previousState = currentState; }

         //Handle Pause Logic
         if (currentState != GameState.GamePaused) {
             //Pause the Game
             Time.timeScale = 0f;
             Cursor.lockState = CursorLockMode.Confined;

            //Pause audio
            AudioListener.pause = true;

            //Enable Pause UI
            if (!pauseUI) {
                 pauseUI = Instantiate(pauseUIPrefab) as GameObject;
                 pauseUI.name = "Pause UI";
             } else {
                 pauseUI.SetActive(true);
             }

             //Set Current State
             currentState = GameState.GamePaused;
         } else {
            //Unpause the game
            if (!quittingGame) {
                Cursor.lockState = CursorLockMode.Locked;
            }
            Time.timeScale = 1f;

            //Unpause audio
            AudioListener.pause = false;

            //Disable Pause UI
            pauseUI.SetActive(false);

            //Return to previous state
            currentState = previousState;
         }
    }

    //Begin Round (Ends Downtime)
    void BeginRound() {
        //Enable the mothership (TODO: Fade in)
        motherShip.SetActive(true);

        //Increase round count
        roundCounter++;

        //Increase round length
        if (roundCounter % 2 == 0) { roundLength += 10f; } //Round length increases on even rounds?

        //Respawn animals every 5 rounds
        if (roundCounter % 5 == 0) {
            //Spawn Animals
            for(int i = 0; i < 10; i++) {
                if (pigPower < maxPigPower) {
                    int animalID = Random.Range(0, animalPrefabs.Count - 1);
                    GameObject animal = Instantiate(animalPrefabs[animalID]);
                    animal.transform.localScale = new Vector3(.6f, .6f, .6f);
                    animal.transform.position = GetRandomPointAtMap();
                    activeAnimals.Add(animal);
                    pigPower++;
                }
            }
        } 
 
        //Increase maximum number of rings
        maxActiveRings++;

        //Reset the timer
        timer = roundLength;

        //Set current game state
        currentState = GameState.PlayerInRound;

        //Find all shootable targets, and assemble animal pool
        activeAnimals.Clear();
        activeAnimals.AddRange(GameObject.FindGameObjectsWithTag("Shootable"));

        //Sort list by distance, so we can prioritize in lower rounds
        if (roundCounter < 3) {
            activeAnimals.Sort(SortByDistance);
        }

        //Reset animals saved counter
        savedAnimalsThisRound = 0;

        //New Round UI Call
        if (roundCounter != 1) {
            playerUI.GetComponent<UIManager>().BeginNotificationAlert("Round " + roundCounter + " beginning!");
        }
    }

    //sort distance from mothership, return priority
    int SortByDistance(GameObject a, GameObject b) {
        float squaredRangeA = (a.transform.position - motherShip.transform.position).sqrMagnitude;
        float squaredRangeB = (b.transform.position - motherShip.transform.position).sqrMagnitude;
        return squaredRangeA.CompareTo(squaredRangeB);
    }

    //Begin downtime (Ends Round)
    void BeginDowntime(bool specialTutorialDowntime) {
        //Disable the mothership (TODO: Fade out -low priority)
        motherShip.SetActive(false);

        //Set current game state
        currentState = GameState.PlayerInDowntime;

        //Reset the timer
        if (specialTutorialDowntime) {
            timer = tutorialLength;
            if (!tutorialActive && !playedTutorial) { StartCoroutine(HandleTutorial()); }
        } else {
            //Set timer to downtime length
            timer = downtimeLength;
            if (roundCounter == 1) {
                //Play Audio
                GetComponent<AudioSource>().clip = breakTutorialSound;
                GetComponent<AudioSource>().Play();
            } else if (roundCounter % 5 == 0) {
                //Play Audio
                GetComponent<AudioSource>().clip = break_AnimalShipmentSound;
                GetComponent<AudioSource>().Play();
            } else {
                //Play Audio
                GetComponent<AudioSource>().clip = breakBeginSound;
                GetComponent<AudioSource>().Play();
            }
        }

        //Update Score
        CalculateScore();

        //Spawn Ammo
        for(int i = 0; i < savedAnimalsThisRound; i++) {
            int ammoID = Random.Range(0, refillPrefabs.Count - 1);
            GameObject ammoBox = Instantiate(refillPrefabs[ammoID]);
            ammoBox.transform.localScale = new Vector3(5f, 5f, 5f);
            ammoBox.transform.position = GetRandomPointAtMotherShip();
            ammoBox.GetComponent<Ammoitem>().SetDestination(GetSmallRandomPointAtMap());
        }
    }

    void SpawnRing() {
        //Pick active animal
        int abductionID;
        if (roundCounter < 3) {
            abductionID = Random.Range(0, activeAnimals.Count / 4);
        } else {
            abductionID = Random.Range(0, activeAnimals.Count - 1);
        }

        //Spawn Ring
        GameObject ring = Instantiate(ringPrefab) as GameObject;
        ring.transform.parent = null;
        ring.transform.position = GetRandomPointAtMotherShip() - new Vector3(0f, 2f, 0f);

        //Give abduction data to ring
        ring.GetComponent<UFO_RingManager>().SetUpRing(GetRandomPointAtMotherShip(), activeAnimals[abductionID], this);

        //Increase active rings counter
        activeRings++;

        //Remove chosen animal from active pool
        activeAnimals.RemoveAt(abductionID);
        dangerCounter ++;

        //Reset Timer
        ringTimer = ringSpawnLength;
    }

    //pick a random point underneath the mothership's position
    Vector3 GetRandomPointAtMotherShip() {
        return new Vector3(
            Random.Range(motherShip.transform.position.x - 10f, motherShip.transform.position.x + 10f),
            motherShip.transform.position.y,
            Random.Range(motherShip.transform.position.z - 10f, motherShip.transform.position.z + 10f)
            );
    }

    //pick a random point on the map, using the Mothership as the map center
    Vector3 GetRandomPointAtMap() {
        return new Vector3(
            Random.Range(motherShip.transform.position.x - mapX, motherShip.transform.position.x + mapX),
            3f,
            Random.Range(motherShip.transform.position.z - mapZ, motherShip.transform.position.z + mapZ)
            );
    }

    //pick a random point on the map/2, using the Mothership as the map center. 
    Vector3 GetSmallRandomPointAtMap() {
        return new Vector3(
            Random.Range(motherShip.transform.position.x - (mapX/2), motherShip.transform.position.x + (mapX/2)),
            3f,
            Random.Range(motherShip.transform.position.z - (mapZ/2), motherShip.transform.position.z + (mapZ/2))
            );
    }

    //pick a random point at the provided Y level
    Vector3 GetRandomPointAtSpecialY(float y) {
        return new Vector3(
            Random.Range(motherShip.transform.position.x - (mapX*2), motherShip.transform.position.x + (mapX*2)),
            y,
            Random.Range(motherShip.transform.position.z - (mapZ*2), motherShip.transform.position.z + (mapZ*2))
            );
    }

    //Get, Set, Calculate
    public string GetCurrentRound() { return roundCounter.ToString(); }
    public string GetTotalBaseBulletsUsed() { return baseBulletsUsed.ToString(); }
    public string GetTotalScore() { return totalScore.ToString(); }
    public string GetRemainingPoints() {
        CalculateScore();
        return "Score: " + totalScore.ToString();
    }
    public void UpdateRemainingPoints(int mod) { remainingPoints += mod; savedAnimalsThisRound++; }
    public int GetRoundCounter() { return roundCounter; }
    public int GetDangerCounter() { return dangerCounter; }
    public int GetPigPower() { return pigPower; } 
    public GameState GetCurrentGameState() { return currentState; }
    public float GetPigPowerPercent() {
        pigPowerPercent = ((float)animalsAbducted / (float)maxAnimalsAbducted);
        //pigPowerPercent = ((float)maxAnimalsAbducted-animalsAbducted / (float)maxAnimalsAbducted);
        return pigPowerPercent;
    }
    public string GetTimer() {
        //Calculate Minutes
        if(timer <= 0f) { timer = 0f; }
        float minutes = Mathf.FloorToInt(timer / 60);

        //Calculate Seconds using Modulo to return remainder
        float seconds = Mathf.FloorToInt(timer % 60);

        //String Format
        string timerFormat = string.Format("{0:00}:{1:00}", minutes, seconds);
        if (currentState == GameState.PlayerInRound) {
            return "Round Time Remaining: " + timerFormat;
        } else if (currentState == GameState.PlayerInDowntime) {
            return "Break Time Remaining: " + timerFormat;
        } else if (currentState == GameState.GamePaused) {
            return "Game Paused";
        } else if (currentState == GameState.GameOver){
            return "Game Over";
        } else { 
            return "Error";
        }
    }
    public float GetTimerAsFloat() {
        return timer;
    }

    //Calculate total score
    public void CalculateScore() {
        //Equation: ((RP+TAS)*FR)-TAU
        totalScore = ((remainingPoints + baseBulletsUsed) * roundCounter) - pigPower;
        if(totalScore <= 0) { totalScore = 0; }
    }

    //Calculate Pig Power
    public void ReducePigPower(int pigPowerMod) {
        pigPower -= pigPowerMod;
        animalsAbducted++;
        
        //Game Over Check
        if(animalsAbducted >= maxAnimalsAbducted) { currentState = GameState.GameOver; }
        if (pigPower <= 0) { currentState = GameState.GameOver; }

        //Play 50% Audio Check
        if (animalsAbducted == (maxAnimalsAbducted / 2)) {
            GetComponent<AudioSource>().clip = halfAbductedSound;
            GetComponent<AudioSource>().Play();
        } else if (animalsAbducted == (maxAnimalsAbducted - 5)) {
            GetComponent<AudioSource>().clip = fiveLeftSound;
            GetComponent<AudioSource>().Play();
        } else if (animalsAbducted == (maxAnimalsAbducted - 1)) {
            GetComponent<AudioSource>().clip = oneLeftSound;
            GetComponent<AudioSource>().Play();
        } else if (pigPower >= 1) {
            int abductionSoundID = Random.Range(0, abductionSuccessSounds.Count - 1);
            GetComponent<AudioSource>().clip = abductionSuccessSounds[abductionSoundID];
            GetComponent<AudioSource>().Play();
        }
    }

    //Calculate Bullet's Used
    public void IncreaseBasicBulletsFired(int countMod) {
        baseBulletsUsed += countMod;
        print("GM: Modifying Basic Bullets Used by " + countMod);
    }

    //Calculate Active Rings
    public void ReduceActiveRings() {
        activeRings--;
        dangerCounter--;
    }

    //Tutorial Sequence
    int tutorialPhase = 0;
    private IEnumerator HandleTutorial() {
        tutorialActive = true;
        //playerUI.GetComponent<UIManager>().BeginNotificationAlert("BOOP: Hey there, I’m BOOP Gun, and uuh, not to make you freak out or anything, but uuuh, you’re about to be invaded by aliens. Need some Assistance?");
        if (!playedTutorial) {
            GetComponent<AudioSource>().clip = introSounds[tutorialPhase];
            GetComponent<AudioSource>().Play();
        }
        yield return new WaitForSeconds(introSounds[tutorialPhase].length + .5f);
        //playerUI.GetComponent<UIManager>().BeginNotificationAlert("BOOP: WEEEEEEL, TOO BAD, because the aliens have just arrived. You’re going to need my help to save your livestock, so, uuh, get shooting!");
        tutorialPhase++;
        if (!playedTutorial) {
            GetComponent<AudioSource>().clip = introSounds[tutorialPhase];
            GetComponent<AudioSource>().Play();
        }
        yield return new WaitForSeconds(introSounds[tutorialPhase].length + .5f);
        //playerUI.GetComponent<UIManager>().BeginNotificationAlert("BOOP: Great! I shoot scientifically advanced projectiles that exert strong forces on whatever they hit. DON’T WORRY, they won’t harm the animals.");
        tutorialPhase++;
        if (!playedTutorial) {
            GetComponent<AudioSource>().clip = introSounds[tutorialPhase];
            GetComponent<AudioSource>().Play();
        }
        yield return new WaitForSeconds(introSounds[tutorialPhase].length + .5f);
        //playerUI.GetComponent<UIManager>().BeginNotificationAlert("BOOP: Though I’m just a gun, so you’re going to have to do the aiming. You’re going to need to shoot the animals that are captured by gravitational spheres to get them out, otherwise they’ll be abducted by the aliens! If they take too many, it’s GAME OVER! ");
        tutorialPhase++;
        if (!playedTutorial) {
            GetComponent<AudioSource>().clip = introSounds[tutorialPhase];
            GetComponent<AudioSource>().Play();
        }
        yield return new WaitForSeconds(introSounds[tutorialPhase].length + .5f);
        //playerUI.GetComponent<UIManager>().BeginNotificationAlert("BOOP: As for how to blast em, There are three types of ammo you can use: Basic, Impact, and Meteor. Basic ammunition is unlimited (though don’t shoot TOO much? It’s bad for my health), so it’s a good ammo type to be used to herd animals or get low to the ground spheres. ");
        tutorialPhase++;
        if (!playedTutorial) {
            GetComponent<AudioSource>().clip = introSounds[tutorialPhase];
            GetComponent<AudioSource>().Play();
        }
        yield return new WaitForSeconds(introSounds[tutorialPhase].length + .5f);
        //playerUI.GetComponent<UIManager>().BeginNotificationAlert("BOOP: Impact ammo, as you may guess, have a MASSIVE amount of knockback to them, making them useful for getting spheres JUUUUST out of reach in one go.");
        tutorialPhase++;
        if (!playedTutorial) {
            GetComponent<AudioSource>().clip = introSounds[tutorialPhase];
            GetComponent<AudioSource>().Play();
        }
        yield return new WaitForSeconds(introSounds[tutorialPhase].length + .5f);
        //playerUI.GetComponent<UIManager>().BeginNotificationAlert("BOOP: The Meteor ammo is still undergoing development, so it’s a bit testy. However, it is STRONG, so use it from a distance for best effect.");
        tutorialPhase++;
        if (!playedTutorial) {
            GetComponent<AudioSource>().clip = introSounds[tutorialPhase];
            GetComponent<AudioSource>().Play();
        }
        yield return new WaitForSeconds(introSounds[tutorialPhase].length + .5f);
        //playerUI.GetComponent<UIManager>().BeginNotificationAlert("BOOP: To change ammo types all you gotta do is scroll up or down on the [MOUSE WHEEL], or use the [NUMBER KEYS 1, 2, AND/OR 3] to change which weapon you’re using.");
        tutorialPhase++;
        if (!playedTutorial) {
            GetComponent<AudioSource>().clip = introSounds[tutorialPhase];
            GetComponent<AudioSource>().Play();
        }
        yield return new WaitForSeconds(introSounds[tutorialPhase].length + .5f);
        //playerUI.GetComponent<UIManager>().BeginNotificationAlert("BOOP: The Aliens will attack you in waves, with each wave harder than the last. Thankfully between waves, there’s some downtime where you can prepare yourself and restock on ammunition. You’re going to need to stay sharp if you’re going to save all of your animals.");
        tutorialPhase++;
        if (!playedTutorial) {
            GetComponent<AudioSource>().clip = introSounds[tutorialPhase];
            GetComponent<AudioSource>().Play();
        }
        yield return new WaitForSeconds(introSounds[tutorialPhase].length + .5f);
        //playerUI.GetComponent<UIManager>().BeginNotificationAlert("BOOP: Luckily, things SHOULD become easier once there are fewer animals. . . wait, what’s that? You get a shipment of more animals every few days? Oh. Then, uuuuuh. . . nevermind?");
        tutorialPhase++;
        if (!playedTutorial) {
            GetComponent<AudioSource>().clip = introSounds[tutorialPhase];
            GetComponent<AudioSource>().Play();
        }
        yield return new WaitForSeconds(introSounds[tutorialPhase].length + .5f);
        //playerUI.GetComponent<UIManager>().BeginNotificationAlert("BOOP: Also, you shou- oh whoops, looks like the aliens are here. Steel yourself and protect your livestock! ");
        //tutorialPhase++;
        //if (!playedTutorial) {
        //    GetComponent<AudioSource>().clip = introSounds[tutorialPhase];
        //    GetComponent<AudioSource>().Play();
        //}
        //yield return new WaitForSeconds(introSounds[tutorialPhase].length + .5f);
        playedTutorial = true;
    }
}
