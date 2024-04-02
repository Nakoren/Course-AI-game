using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhantomScript : MonoBehaviour
{
    public float ExistenceTime;
    private void Update()
    {
        ExistenceTime-= Time.deltaTime;
        if (ExistenceTime < 0)
        {
            Destroy(this.gameObject);
        }
    }

}
