using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CivilianAI : MonoBehaviour
{
    public Animator animator;
    public GameObject waypoint;
    public float speed = 4f;
    public bool loop = true;
    Rigidbody rb;
    int points = 0;
    int count = 0;
    private Transform target = null;
    private Quaternion rot;

    // Start is called before the first frame update 
    void Start()
    {
        points = waypoint.transform.childCount;
        rb = GetComponent<Rigidbody>();
        rot = transform.rotation;
        SetTarget();
    }

    void Update()
    {
        if (!target)
            return;

        transform.rotation = Quaternion.Lerp(transform.rotation, rot, 10 * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.5f)
        {
            if (animator)
                animator.SetBool("move", true);

            SetTarget();
        }
    }

    void FixedUpdate()
    {
        if (!target)
            return;

        rb.velocity = transform.forward * (speed * 10) * Time.deltaTime;
    }

    private void SetTarget()
    {
        if (count >= points)
        {
            if (!loop)
            {
                target = null;
                rb.velocity = Vector3.zero;

                if (animator)
                    animator.SetBool("move", false);

                return;
            }

            count = 0;
        }

        target = waypoint.transform.GetChild(count);
        rot = Quaternion.LookRotation(target.position - transform.position);
        count++;
    }
}
