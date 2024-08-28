
using UnityEngine;

class SelfDestroy : MonoBehaviour
{
    public float time = 2.0f;

    void Start()
    {
        Destroy(gameObject, time);
    }
}