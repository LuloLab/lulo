using UnityEngine;

public class QuadOut : MonoBehaviour
{
    public void Init(Vector3 vx)
    {
        GetComponent<Collider>().material = Camera.main.GetComponent<Game>().quadPhyMat;

        Debug.Assert(gameObject.GetComponent<Rigidbody>() == null);
        var rigidBody = gameObject.AddComponent<Rigidbody>();
        rigidBody.velocity = vx;
    }

    private void OnDestroy()
    {
        Destroy(GetComponent<Rigidbody>());
        GetComponent<Collider>().material = null;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Camera.main.GetComponent<AudioController>().QuadGround(collision.impulse.magnitude);
    }
}
