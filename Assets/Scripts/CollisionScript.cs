using UnityEngine;
using System.Collections;
using TMPro;

public class CollisionScript : MonoBehaviour
{


    void Start()
    {

    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponentInChildren<TextMeshPro>().text == this.gameObject.GetComponentInChildren<TextMeshPro>().text)
        {
            //If the GameObject's name matches the one you suggest, output this message in the console
            Destroy(collision.gameObject);
        }
    }
}