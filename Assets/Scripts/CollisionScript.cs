using UnityEngine;
using System.Collections;

public class CollisionScript : MonoBehaviour
{


    void Start()
    {

    }

    void OnCollisionEnter(Collision collision)
    {
        Destroy(gameObject);
    }
}