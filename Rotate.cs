using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour
{

    public bool rotating = false;
    Vector3 rotateAmount;
    GameObject parent;
    // Update is called once per frame
    void Update()
    {
        if (rotating) {
            //transform.RotateAround(parent.transform.position, CalculateRotateAmount(), 200f * Time.deltaTime);
           transform.Rotate(rotateAmount * Time.deltaTime);
        }

        //If animal falls through the map
        if(transform.position.y <= -20f) {
            Destroy(gameObject);
        }
    }

    Vector3 CalculateRotateAmount() {
        float x = Random.Range(0f, 360f);
        float y = Random.Range(0f, 360f);
        float z = Random.Range(0f, 360f);
        return new Vector3(x, y, z);
    }

    public void BeginRotation() {
        float x = Random.Range(0f, 360f);
        float y = Random.Range(0f, 360f);
        float z = Random.Range(0f, 360f);
        rotateAmount = new Vector3(x, y, z);

        rotating = true;
        GetComponent<Animator>().SetBool("Rotating", rotating);
        parent = transform.parent.gameObject;

        GetComponent<AudioSource>().Play();
    }

    public void EndRotation() {
        rotating = false;
        transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, 0f));
        GetComponent<Animator>().SetBool("Rotating", rotating);
    }


    private void Start() {
        CalculateRotateAmount();
    }
}
