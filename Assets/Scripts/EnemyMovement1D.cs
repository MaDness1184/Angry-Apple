using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyState
{
    
}

public class EnemyMovement1D : MonoBehaviour
{
    [Header("Velocity")]
    public float walkSpeed = 1f;

    // Private Chached References
    private Rigidbody2D myRidgidbody;
    private Animator myAnimator;

    // Start is called before the first frame update
    void Start()
    {
        myRidgidbody = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        // Make Broccoli move and change direction
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // change direction
    }
}
