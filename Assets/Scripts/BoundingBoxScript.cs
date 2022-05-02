using UnityEngine;
using System.Collections;
using TMPro;

public class BoundingBoxScript : MonoBehaviour
{
    public NetworkResultWithLocation networkResultWithLocation;
    float rotationSpeed = 0.5f;
    TextMeshPro textComponent;

    void Start()
    {
        textComponent = this.GetComponentInChildren<TextMeshPro>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<BoundingBoxScript>().networkResultWithLocation.label == this.networkResultWithLocation.label)
        {
            //If the bounding box label matches the collided bbox, check if probabiltiies are greater and delete the lower confidence bbox
            if(collision.gameObject.GetComponent<BoundingBoxScript>().networkResultWithLocation.prob > this.networkResultWithLocation.prob)
            {
                Destroy(this.gameObject);
            }

        }


    }

    void Update()
    {
        // Corbett
        //Rotate only the text box (the label of the bbox) to face the user 
        Vector3 targetDirection = textComponent.transform.position - Camera.main.transform.position;
        Quaternion rotation = Quaternion.LookRotation(targetDirection, Vector3.up);
        rotation.z = 0;
        rotation.x = 0;
        textComponent.transform.rotation = rotation;
    }
}