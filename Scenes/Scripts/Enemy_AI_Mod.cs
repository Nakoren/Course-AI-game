using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class Enemy_AI_Mod : MonoBehaviour
{

    //Всё настраивается в инспекторе (для тестов)
    //Объекты
    public GameObject ammoProjectile;
    public GameObject playerFantom;
    //Время
    public float fireCooldown;
    public int fireDirections;
    public float targetDelay;
    public float trackTime;
    //Отклонения
    public float aimErrorMargin;
    public int ricochetZoneRays;
    public float ricochetZoneDistanceBetween;
    public float ricochetZoneAnglerDifference;
    public float ricochetSectorDistant;

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

    private void timersTick()
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
    private void directTargeting()
    {
        /*
        Debug.Log("Direct");
        */
        Vector3 trajectory = (playerTransform.position - targetLastPosition) / Time.deltaTime;
        float timeToReach = Vector3.Distance(transform.position, playerTransform.position) / ammoProjectile.GetComponent<Bullet>().Speed;
        Vector3 targetPosition = playerTransform.position + (trajectory * timeToReach) + AimError();
        /*
        Debug.Log(playerTransform.position);
        Debug.Log(targetPosition);
        */
        Vector2 playerDirection = targetPosition - transform.position;
        float bulletRotateZ = Mathf.Atan2(playerDirection.y, playerDirection.x) * Mathf.Rad2Deg - 90;

        fire(bulletRotateZ);
    }
    private void ricochetTargeting()
    {
        if (currentDelay <= 0)
        {
            /*
            Debug.Log("Ricochet");
            */

            List<(Vector2, Vector2)> ricochetZones = getRicochetZones();

            for (int zoneNum = 0;zoneNum < ricochetZones.Count;zoneNum++)
            {
                LayerMask wallsMask = ~(1 << LayerMask.NameToLayer("Walls"));

                RaycastHit2D zoneStart = Physics2D.Raycast(transform.position, ricochetZones[zoneNum].Item1);
                RaycastHit2D zoneEnd = Physics2D.Raycast(transform.position, ricochetZones[zoneNum].Item2);

                Vector2 zoneStartReflectDir = Vector2.Reflect(ricochetZones[zoneNum].Item1,zoneStart.normal);
                Vector2 zoneEndReflectDir = Vector2.Reflect(ricochetZones[zoneNum].Item2, zoneEnd.normal);

                RaycastHit2D zoneStartReflect = Physics2D.Raycast(zoneStart.point, zoneStartReflectDir);
                RaycastHit2D zoneEndReflect = Physics2D.Raycast(zoneEnd.point, zoneEndReflectDir);

                Vector2 reflectSectorStartEdge = zoneStart.point + zoneStartReflectDir * ricochetSectorDistant;
                Vector2 reflectSectorEndEdge = zoneEnd.point + zoneEndReflectDir * ricochetSectorDistant;

                

                Vector2[] ZonePoints = new Vector2[4];
                ZonePoints[0] = zoneStart.point; ZonePoints[1] = reflectSectorStartEdge; ZonePoints[2] = reflectSectorEndEdge; ZonePoints[3] = zoneEnd.point;

                if ((localGeometry.pointInsideZone(predictedPosition, ZonePoints))||(zoneStartReflect.collider.transform.tag == "phantom") || (zoneEndReflect.collider.transform.tag == "phantom")) {

                    float angleStart = Vector2.Angle(new Vector2(1,0), ricochetZones[zoneNum].Item1);
                    float angleEnd = Vector2.Angle(new Vector2(1, 0), ricochetZones[zoneNum].Item2);
                    float angleBetween = Vector2.Angle(ricochetZones[zoneNum].Item1, ricochetZones[zoneNum].Item2);

                    Debug.DrawLine(transform.position, zoneStart.point, Color.cyan, targetDelay);
                    Debug.DrawLine(transform.position, zoneEnd.point, Color.cyan, targetDelay);

                    Debug.DrawLine(zoneStart.point, reflectSectorStartEdge, Color.red, targetDelay);
                    Debug.DrawLine(zoneEnd.point, reflectSectorEndEdge, Color.red, targetDelay);

                    Debug.Log(angleStart+", "+ angleEnd +", "+angleBetween);

                    List<Vector2> hittingRays = new List<Vector2>();
                    int hits = 0;

                    for (int fireDirectionNum = 0; fireDirectionNum < ricochetZoneRays; fireDirectionNum++)
                    {
                        float rayAngle = angleBetween / ricochetZoneRays * fireDirectionNum;
                        float x = Mathf.Cos(rayAngle);
                        float y = Mathf.Sin(rayAngle);

                        Vector2 rayDirection = new Vector2(x, y);
                        RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDirection);
                        Vector2 reflectDir = Vector2.Reflect(rayDirection, hit.normal);
                        RaycastHit2D ricochetHit = Physics2D.Raycast(hit.point, reflectDir);

                        if (ricochetHit.collider.transform.tag == "phantom")
                        {
                            Debug.Log("hit detected");
                            Debug.DrawLine(transform.position, hit.point, Color.yellow, 0.5f);
                            Debug.DrawLine(hit.point, ricochetHit.point, Color.yellow, 0.5f);
                            hittingRays.Add(new Vector2(x, y));
                            hits++;
                        }
                    }
                    if (hits != 0)
                    {
                        Debug.Log("List size: " + hittingRays.Count);
                        Vector2 fireDirection = hittingRays[hits / 2];
                        float bulletRotateZ = Mathf.Atan2(fireDirection.y, fireDirection.x) * Mathf.Rad2Deg - 90;
                        Debug.Log("Ricochet angle: " + bulletRotateZ);
                        fire(bulletRotateZ);
                        return;
                    }

                }
            }   

            currentDelay = targetDelay;
        }
    }

    private List<(Vector2, Vector2)> getRicochetZones()
    {
        List<(Vector2, Vector2)> resList = new List<(Vector2, Vector2)>();

        float x = Mathf.Cos(0);
        float y = Mathf.Sin(0);
        Vector2 currentZoneStart = new Vector2(x, y);
        RaycastHit2D prevHit = Physics2D.Raycast(transform.position, currentZoneStart);

        Vector2 prevRayDirection = currentZoneStart;

        float normalAngle = Mathf.Atan2(prevHit.normal.y, prevHit.normal.x);
        float rayAngle = Mathf.Atan2(y, x);

        float prevCollAngle = normalAngle + rayAngle;
        if (prevCollAngle >= 360) { prevCollAngle = 360 - prevCollAngle; }

        for (int i = 1; i < fireDirections; i++)
        {
            x = Mathf.Cos(2 * Mathf.PI * i / fireDirections);
            y = Mathf.Sin(2 * Mathf.PI * i / fireDirections);
            Vector2 nextRayDirection = new Vector2(x, y);
            //Ray2D ray = new Ray2D(transform.position, newRayDirection);
            RaycastHit2D nextHit = Physics2D.Raycast(transform.position, nextRayDirection);
            normalAngle = Mathf.Atan2(nextHit.normal.y, nextHit.normal.x);
            rayAngle = Mathf.Atan2(y, x);

            float nextCollAngle = normalAngle + rayAngle;
            if (nextCollAngle >= 360) { nextCollAngle = 360 - nextCollAngle; }

            //Далее проводится сравнение предыдущей точки попадания с новой точкой. Это делается для определения, подходит ли новый луч для создания новой зоны отражения.
            //Для этого сравнивается расстояние между точками, их угол столкновения с поверхностью и совпадение коллайдеров.
            if ((Vector2.Distance(prevHit.point, nextHit.point) > ricochetZoneDistanceBetween)&&(math.abs(prevCollAngle - nextCollAngle)>ricochetZoneAnglerDifference)||(prevHit.collider!=nextHit.collider)) 
            {
                resList.Add((currentZoneStart, prevRayDirection));
                currentZoneStart = nextRayDirection;
                prevRayDirection = nextRayDirection;
                prevHit = nextHit;
                prevCollAngle = nextCollAngle;
            }
            else
            {
                prevRayDirection = nextRayDirection;
                prevHit = nextHit;
                prevCollAngle = nextCollAngle;
            }

            
        }
        return resList;
    }
        
    private void fire(float rotateZ)
    {
        Instantiate(ammoProjectile, transform.position, Quaternion.Euler(0f, 0f, rotateZ));
        currentCooldown = fireCooldown;
    }

    Vector3 AimError()
    {
        Vector2 randomPoint = UnityEngine.Random.insideUnitSphere * aimErrorMargin;
        
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
        int choice = UnityEngine.Random.Range(0, Global.roamingNodesTransforms.Length);
        Vector3 res = Global.roamingNodesTransforms[choice].position;
        Debug.Log(res);
        return res;
    }   
   
}
static class localGeometry
{
    static public bool pointInsideZone(Vector2 point, Vector2[] p)
    {
        bool result = false;
        int j = p.Length - 1;
        for (int i = 0; i < p.Length; i++)
        {
            if ((p[i].y < point.y && p[j].y >= point.y || p[j].y < point.y && p[i].y >= point.y) &&
                 (p[i].x + (point.y - p[i].y) / (p[j].y - p[i].y) * (p[j].x - p[i].x) < point.x))
                result = !result;
            j = i;
        }
        //if (result) { Debug.Log("Point is inside"); }
        return result;
    }
}

