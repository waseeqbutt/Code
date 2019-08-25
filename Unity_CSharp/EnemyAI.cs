using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterStatus))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    CharacterController controller;
    CharacterStatus characterStatus;
    NavMeshAgent navMeshAgent;

    public enum AIState { Moving = 0, Pausing = 1, Idle = 2, Patrol = 3 }
    public GameObject mainModel;
    public Transform followTarget;
    public float approachDistance = 2.0f;
    public float detectRange = 15.0f;
    public float lostSight = 100.0f;
    public float speed = 4.0f;
    public float patrolSpeed = 2f;
    public bool patrolling = false;
    public bool navmeshSteering = false;
    public Animator animator;

    [HideInInspector]
    public bool flinch = false;

    public bool stability = false;

    public bool freeze = false;

    public Transform attackPrefab;
    public Transform attackPoint;
    public Transform startWeapon;

    public float attackCast = 0.3f;
    public float attackDelay = 0.5f;
    [HideInInspector]
    public AIState followState;
    private float distance = 0.0f;
    float next = 1;
    private Vector3 knock = Vector3.zero;
    [HideInInspector]
    public bool cancelAttack = false;
    private bool attacking = false;
    public AudioClip attackVoice;
    public Transform attackParticle;

    void Start()
    {
        gameObject.tag = "Enemy";

        controller = GetComponent<CharacterController>();
        characterStatus = GetComponent<CharacterStatus>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.speed = speed;
        navMeshAgent.stoppingDistance = approachDistance;

        if (!animator)
        {
            animator = mainModel.GetComponent<Animator>();
        }

        if (patrolling == true)
        {
            followState = AIState.Patrol;
            GetComponent<VWaypointManager>().StartMovement(animator, patrolSpeed);
        }
        else
        {
            followState = AIState.Idle;
        }

        SetWeapon();
    }

    Vector3 GetDestination()
    {
        Vector3 destination = followTarget.position;
        destination.y = transform.position.y;
        return destination;
    }

    void Update()
    {
        if (!controller.isGrounded)
        {
            controller.Move(Vector3.down * 1 * Time.deltaTime);
        }

        if (!followTarget)
            SearchEnemyInSight();

        animator.SetBool("hurt", flinch);

        if (flinch)
        {
        //    controller.Move(knock * 6 * Time.deltaTime);
            return;
        }

        if (freeze)
        {
            return;
        }

        if (!followTarget)
        {
            return;
        }

        //-----------------------------------

        if (followState == AIState.Moving)
        {
            if ((followTarget.position - transform.position).magnitude <= approachDistance)
            {
                followState = AIState.Pausing;

                navMeshAgent.velocity = Vector3.zero;
                navMeshAgent.isStopped = true;

                animator.SetBool("run", false);
                //----Attack----
                StartCoroutine(Attack());
            }
            else if ((followTarget.position - transform.position).magnitude >= lostSight)
            {
                followState = AIState.Idle;
                navMeshAgent.velocity = Vector3.zero;
                navMeshAgent.isStopped = true;
                followTarget = null;
                animator.Play("IdleMode");
                animator.SetBool("run", false);
            }
            else
            {
                navMeshAgent.destination = followTarget.position;
                navMeshAgent.isStopped = false;

                if (navmeshSteering == false)
                {
                    Vector3 destinationy = followTarget.position;
                    destinationy.y = transform.position.y;
                    transform.LookAt(destinationy);
                }
            }
        }
        else if (followState == AIState.Pausing)
        {
            Vector3 destinya = followTarget.position;
            destinya.y = transform.position.y;
            transform.LookAt(destinya);

            distance = (transform.position - GetDestination()).magnitude;
            if (distance > approachDistance)
            {
                followState = AIState.Moving;
                navMeshAgent.isStopped = false;
                animator.SetBool("run", true);
            }
        }
        //----------------Idle Mode--------------
        else if (followState == AIState.Idle)
        {
            Vector3 destinyheight = followTarget.position;
            destinyheight.y = transform.position.y - destinyheight.y;
            float getHealth = characterStatus.maxHealth - characterStatus.health;

            distance = (transform.position - GetDestination()).magnitude;
            if (distance < detectRange && Mathf.Abs(destinyheight.y) <= 4 || getHealth > 0f)
            {
                followState = AIState.Moving;
                navMeshAgent.isStopped = false;
                animator.SetBool("run", true);
            }
        }
        //-----------------------------------
    }

    void SearchEnemyInSight()
    {
        if (Time.time > next)
        {
            next = Time.time + 0.5f;
            Collider[] colliders = Physics.OverlapSphere(attackPoint.position, detectRange);
            foreach (Collider collider in colliders)
            {
                if (collider.tag == "Player" || collider.tag == "Ally")
                {
                    //	if player is not in enemy sight angle
                    if (Vector3.Angle(collider.transform.position - attackPoint.position, attackPoint.forward) > 90)
                    {
                        return;
                    }

                    RaycastHit hit;
					Transform rayTarget = collider.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.UpperChest).transform;
                    if (Physics.Raycast(attackPoint.position, rayTarget.transform.position - attackPoint.position, out hit, detectRange))
                    {
                        Debug.DrawRay(attackPoint.position, rayTarget.transform.position - attackPoint.position, Color.red, 0.5f);

                        if (hit.collider.tag == "Player" || hit.collider.tag == "Ally")
                        {
                            followTarget = hit.transform;
                            FoundEnemyInSight();
                        }
                    }

                    break;
                }
            }
        }
    }

    void FoundEnemyInSight()
    {
        animator.Play("FightMode");
        followState = AIState.Idle;
        if (patrolling)
        {
            GetComponent<VWaypointManager>().StopMovement();
            navMeshAgent.speed = speed;
        }
    }

    public void Flinch(Vector3 dir, Transform attacker = null)
    {
        if (stability)
        {
            return;
        }

        if (characterStatus.hurtSound && characterStatus.health >= 1)
        {
            GetComponent<AudioSource>().clip = characterStatus.hurtSound;
            GetComponent<AudioSource>().Play();
        }
        cancelAttack = true;

        knock = transform.TransformDirection(Vector3.back);
        StartCoroutine(KnockBack());

        if (!followTarget)
        {
            followTarget = attacker;
            FoundEnemyInSight();
        }

        //	followState = AIState.Moving;
    }

    public void SetWeapon()
    {
        if(startWeapon)
        {
            mainModel.GetComponent<NPCAnimatorOverride>().SetAnimationClip(startWeapon.GetComponent<WeaponManager>().animationClip);
            characterStatus.attackDamage = startWeapon.GetComponent<WeaponManager>().damage;
            attackVoice = startWeapon.GetComponent<WeaponManager>().sound;
            attackDelay = startWeapon.GetComponent<WeaponManager>().fireRate;
            attackPrefab = startWeapon.GetComponent<WeaponManager>().attackPrefab;
            //  for #future_improvements
         //   attackPoint = startWeapon.GetComponent<WeaponManager>().attackPoint;
        // attackParticle = startWeapon.GetComponent<WeaponManager>().attackParticle;
        }
    }

    IEnumerator KnockBack()
    {
        flinch = true;
        yield return new WaitForSeconds(0.2f);
        flinch = false;
    }

    IEnumerator Attack()
    {
        cancelAttack = false;
        Transform bulletShootout;
        if (!flinch ||!freeze || !attacking)
        {
            freeze = true;
            attacking = true;
            animator.Play("Attack");
            
            yield return new WaitForSeconds(attackCast);

            if (!cancelAttack)
            {
                if (attackVoice && !flinch)
                {
                    GetComponent<AudioSource>().clip = attackVoice;
                    GetComponent<AudioSource>().Play();
                }
                bulletShootout = Instantiate(attackPrefab, attackPoint.transform.position, attackPrefab.transform.rotation) as Transform;
                bulletShootout.GetComponent<DamageManager>().SetDamageProperties(transform, attackPoint, characterStatus.attackDamage);
                yield return new WaitForSeconds(attackDelay);
                freeze = false;
                attacking = false;
                //	animator.SetBool("run" , true);
                CheckDistance();
            }
            else
            {
                freeze = false;
                attacking = false;
            }

        }

    }

    void CheckDistance()
    {
        if (!followTarget)
        {
            navMeshAgent.isStopped = true;
            animator.SetBool("run", false);
            followState = AIState.Idle;
            return;
        }
        float distancea = (followTarget.position - transform.position).magnitude;
        if (distancea <= approachDistance)
        {
            Vector3 destinya = followTarget.position;
            destinya.y = transform.position.y;
            transform.LookAt(destinya);
            StartCoroutine(Attack());
        }
        else
        {
            followState = AIState.Moving;
            navMeshAgent.isStopped = false;
            animator.SetBool("run", true);
        }
    }

    public void EnemyDetected(Transform target)
    {
        followTarget = target;
    }
}
