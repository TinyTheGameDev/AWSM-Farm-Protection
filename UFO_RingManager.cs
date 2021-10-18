using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum RingState { GivingUp, Seeking, Sleeping, Returning, Rewinding }
enum RingAttributes { None, Shielding }
public class UFO_RingManager : MonoBehaviour
{
    GameManager gameManager;        //Reference to the Game Manager object
    RingState ringState;            //Refernece to the current ring state
    Vector3 motherShip;             //Reference to the Mothership object
    GameObject target;              //Reference to the current target
    Vector3 targetOrigionalPos;     //Reference to the position the target was at when seeking
    float speed = 5f;               //Movement speed for the ring
    float timer;                    //Reference to time that has passed
    float sleepTime;                //Reference to time in sleeping state
    RingAttributes ringAttribute;   //Reference to chosen ring attribute
    int shieldSize = 0;             //Reference to number of hit's the ring can take.
  
    //Custom Colors
    Color defaultColor = new Color(0f, 38f, 191f);
    private void Start() {
        //Used in AlanPlayground scene
        //SetUpRing(new Vector3(-10f, 5f, 42f), GameObject.FindGameObjectWithTag("Shootable"), null);
    }

    // Update is called once per frame
    void Update()
    {
        //Handle ring when seeking
        if (ringState == RingState.Seeking) {
            timer += Time.deltaTime;
            transform.position = (Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime));
        }

        //Handle ring when sleeping
        if( ringState == RingState.Sleeping) {
            //Sleeping, shhh
            timer += Time.deltaTime;
            speed = 5f;
            if (timer >= sleepTime) {
                ringState = RingState.Returning;
            }
        }

        //Handle ring while returning
        if( ringState == RingState.Returning) {
            transform.position = (Vector3.MoveTowards(transform.position, motherShip, speed * Time.deltaTime));
        }

        //Handle ring while chasing player
        if (ringState == RingState.GivingUp) {
            speed = 15f;
            transform.position = (Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime));
        }

        //If target is below map
        if(target.transform.position.y <= -20f) {
            print("Animal fell through map, resolving issue");
            gameManager.ReduceActiveRings();
            Destroy(target.gameObject);
            Destroy(this.gameObject);
        }

        //Eject if game state changes
        if(gameManager.GetCurrentGameState() == GameState.PlayerInRound && gameManager.GetTimerAsFloat() <= 0f) {
            EjectAnimal();
        }
    }

    //Called to set up the ring after instantiation
    public void SetUpRing(Vector3 motherShip, GameObject target, GameManager gameManager) {
        this.motherShip = motherShip;
        this.target = target;
        this.targetOrigionalPos = target.transform.position;
        this.gameManager = gameManager;
        this.ringState = RingState.Seeking;
        sleepTime = Random.Range(1f, 5f);
        sleepTime = 1f;

        //color
        //GetComponent<Renderer>().material.color = Color.red;

        //Pick attribute
        this.ringAttribute = PickAttribute();
    }

    //Picks a ring attribute (WIP, May be dropped. Just depends on time)
    RingAttributes PickAttribute() {
        int attributeID = -1;// Random.Range(1, 5);
        //if (gameManager.GetRoundCounter() == 1) { attributeID = 1; } //Never have an attribute on round 1
        switch (attributeID) { //Set up attribute specific orb features.
            case 1:
                this.gameObject.name = "Shielding Ring";
                GetComponent<Renderer>().material.color = Color.gray;
                shieldSize = 2;
                return RingAttributes.Shielding;
            default:
                this.gameObject.name = "Base Ring";
                return RingAttributes.None;
        }
    }

    //Eject Code
    private void EjectAnimal() {
        target.GetComponentInChildren<Rotate>().EndRotation();
        target.GetComponentInChildren<Rigidbody>().useGravity = true;
        target.transform.parent = null;
        gameManager.ReduceActiveRings();
        Destroy(gameObject);
    }

    //When something enters the rings trigger
    private void OnTriggerEnter(Collider other) {
        if(other.name == "BasicBullet(Clone)" && shieldSize > 0) {
            Vector3 initialVelocity = other.GetComponent<Rigidbody>().velocity;
            var speed = initialVelocity.magnitude;
            var direction = Vector3.Reflect(initialVelocity.normalized, Vector3.up);
            other.GetComponent<Rigidbody>().velocity = direction * Time.deltaTime;
            other.GetComponent<Rigidbody>().AddForce(-initialVelocity * Time.deltaTime);
            shieldSize--;
            if(shieldSize < 1) { GetComponent<Renderer>().material.color = defaultColor; }
        }

        if(other.CompareTag("Shootable") && other.gameObject == target) {
            //Parent the shootable object
            other.transform.parent = transform;
            other.transform.position = this.GetComponent<Renderer>().bounds.center;

            //Shootable object begins rotation
            other.GetComponent<Rotate>().BeginRotation();
            other.GetComponent<Rigidbody>().useGravity = false;
            other.GetComponent<Rigidbody>().velocity = Vector3.zero;
            timer = 0f;
            ringState = RingState.Sleeping;
        }

        if (other.CompareTag("UFO") && ringState == RingState.Returning) {
            print("Abduction Complete");
            gameManager.ReduceActiveRings();
            gameManager.ReducePigPower(1);
            Destroy(this.gameObject);
        }
    }

    //When something exits the rings trigger
    private void OnTriggerExit(Collider other) {
        if(other.CompareTag("Shootable") && other.gameObject == target) {
            other.transform.parent = null;
            other.GetComponent<Rotate>().EndRotation();
            other.GetComponent<Rigidbody>().useGravity = true;
            gameManager.ReduceActiveRings();
            gameManager.UpdateRemainingPoints(100);
            Destroy(this.gameObject);
        }
    }
}
