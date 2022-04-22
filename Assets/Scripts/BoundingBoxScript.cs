using UnityEngine;
using System.Collections;
using TMPro;

public class BoundingBoxScript : MonoBehaviour
{
    public NetworkResultWithLocation networkResultWithLocation;

    void Start()
    {
        
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<BoundingBoxScript>().networkResultWithLocation.label == this.networkResultWithLocation.label)
        {
            //If the GameObject's name matches the one you suggest, output this message in the console
            if(collision.gameObject.GetComponent<BoundingBoxScript>().networkResultWithLocation.prob > this.networkResultWithLocation.prob)
            {
                Destroy(this.gameObject);
            }

        }
    }
}