using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

public class Enemy_AI : MonoBehaviour
{
    public GameObject ammoProjectile;
    public GameObject playerFantom;
    public float fireCooldown;
    public int fireDirections;
    public float targetDelay;
    public float trackTime;
    public float aimErrorMargin;

    private Vector3 targetLastPosition;
    private NavMeshAgent agent; 
    private Transform playerTransform;
    private Vector3 predictedPosition;
    private float currentCooldown;
    private float currentDelay;
    private float trackTimer;
    private Vector3 roamDestination;
    private bool roamingStatus = true;
    private bool trackStatus = false;
    private GameObject trackedPhantom;

    private int currentBehaviour;
    //1-roam, 2-Direct fire, 3-Predict fire

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        playerTransform = GameObject.Find("Character").GetComponent<Transform>();
        currentDelay = targetDelay;
        currentCooldown = fireCooldown;
        currentBehaviour = 1;
        targetLastPosition = playerTransform.position;
        roamDestination = RandomRoamingPoint();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (Global.Game_status == false)
        {
            return;
        }
        timersTick();
        Vector2 playerDirection = playerTransform.position - transform.position;
        RaycastHit2D hitToPlayer = Physics2D.Raycast(transform.position, playerDirection);

        Debug.DrawLine(transform.position,playerTransform.position,Color.magenta);

        if (hitToPlayer.collider.transform.tag == "player")
        {
            currentBehaviour = 2;
            trackTimer = trackTime;
            Debug.DrawLine(transform.position, playerTransform.position, Color.magenta);
        }
        else
        {
            Debug.DrawLine(transform.position, playerTransform.position, Color.red);
            if (trackTimer > 0)
            {
                currentBehaviour = 3;
            }
            else
            {
                currentBehaviour = 1;
            }
        }

        if (currentBehaviour == 1)
        {
            trackStatus = false;
            if ((!roamingStatus) || (Vector3.Distance(transform.position,roamDestination)<1))
            {
                roamDestination = RandomRoamingPoint();
            }
            roamingStatus = true;
            moveRoaming();
        }
        if (currentBehaviour == 2)
        {
            trackStatus = false;
            roamingStatus = false;
            moveDirect();
            if (currentCooldown <= 0)
            {
                directTargeting();
            }
        }
        if (currentBehaviour == 3)
        {
            roamingStatus = false;
            if (!trackStatus)
            {
                if (trackedPhantom != null)
                {
                   Destroy(trackedPhantom);
                }
                Vector3 trajectory = (playerTransform.position - targetLastPosition) / Time.deltaTime;
                predictedPosition = playerTransform.position + (trajectory) + AimError();
                trackedPhantom = Instantiate(playerFantom, predictedPosition, Quaternion.identity);
                trackStatus = true;
            }
            
            Vector3 phantomDirection = predictedPosition - transform.position;
            RaycastHit2D phantomHit = Physics2D.Raycast(transform.position,phantomDirection);
            if (!(phantomHit.collider.tag == "phantom"))
            {
                if (currentCooldown <= 0)
                {
                    ricochetTargeting();
                }
            }
            movePredict();
        }
        targetLastPosition = playerTransform.position;
    }

    void timersTick()
    {
        if (currentDelay > 0)
        {
            currentDelay -= Time.deltaTime;
        }
        if (currentCooldown > 0)
        {
            currentCooldown -= Time.deltaTime;
        }
        if (trackTimer > 0)
        {
            trackTimer -= Time.deltaTime;
        }
    }
    void directTargeting()
    {
        Debug.Log("Direct");
        Vector3 trajectory = (playerTransform.position - targetLastPosition) / Time.deltaTime;
        float timeToReach = Vector3.Distance(transform.position, playerTransform.position) / ammoProjectile.GetComponent<Bullet>().Speed;
        Vector3 targetPosition = playerTransform.position + (trajectory * timeToReach) + AimError();
        Debug.Log(playerTransform.position);
        Debug.Log(targetPosition);
        Vector2 playerDirection = targetPosition - transform.position;
        float bulletRotateZ = Mathf.Atan2(playerDirection.y, playerDirection.x) * Mathf.Rad2Deg - 90;

        fire(bulletRotateZ);
    }
    void ricochetTargeting()
    {
        if (currentDelay <= 0)
        {
            Debug.Log("Ricochet");
            for (int i = 0; i < fireDirections; i++)
            {
                float x = Mathf.Cos(2 * Mathf.PI * i / fireDirections);
                float y = Mathf.Sin(2 * Mathf.PI * i / fireDirections);
                Vector2 dirRayDirection = new Vector2(x, y);
                Ray2D ray = new Ray2D(transform.position, dirRayDirection);
                RaycastHit2D hit = Physics2D.Raycast(transform.position, dirRayDirection);
                Vector2 reflectDir = Vector2.Reflect(ray.direction, hit.normal);
                RaycastHit2D ricochetHit = Physics2D.Raycast(hit.point, reflectDir);

                Debug.Log("Pred");
                Debug.DrawLine(transform.position, hit.point,Color.cyan, 2f);
                Debug.DrawLine(hit.point,ricochetHit.point,Color.magenta,2f);

                if (ricochetHit.collider.transform.tag == "phantom")
                {
                    float bulletRotateZ = Mathf.Atan2(y,x)*Mathf.Rad2Deg - 90;
                    fire(bulletRotateZ);
                    return;
                }

            }
            currentDelay = targetDelay;
        }
    }
        
    void fire(float rotateZ)
    {
        Instantiate(ammoProjectile, transform.position, Quaternion.Euler(0f, 0f, rotateZ));
        currentCooldown = fireCooldown;
    }

    Vector3 AimError()
    {
        Vector2 randomPoint = Random.insideUnitSphere * aimErrorMargin;
        
        return randomPoint;
    }

    void moveDirect()
    {
        agent.SetDestination(playerTransform.position);
    }

    void movePredict()
    {
        agent.SetDestination(predictedPosition);
    }

    void moveRoaming()
    {
        agent.SetDestination(roamDestination);
    }
    Vector3 RandomRoamingPoint()
    {
        int choice = Random.Range(0, Global.roamingNodesTransforms.Length);
        Vector3 res = Global.roamingNodesTransforms[choice].position;
        Debug.Log(res);
        return res;
    }
    
}