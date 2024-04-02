using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Global: MonoBehaviour
{
    public static bool Game_status = true;
    public GameObject roamingNodes;

    public static Transform[] roamingNodesTransforms;

    private void Start()
    {
        roamingNodesTransforms = roamingNodes.GetComponentsInChildren<Transform>();
    }
}
