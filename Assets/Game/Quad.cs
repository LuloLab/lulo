using UnityEngine;
using UnityEngine.UI;

public class Quad : MonoBehaviour
{
    public int id, pattern, initFirstPos;
    public bool atHome, hasTarget, moveable, iron;
    public char shape;
    public Color color;
    public SubQuad[] subquads;

    [System.Serializable]
    public struct SubQuad
    {
        public int pos;
        public Material decalMat;
        public SpriteRenderer sprite;
    }

    public int FirstPos
    {
        get
        {
            return subquads[0].pos;
        }
        set
        {
            int delta = value - subquads[0].pos;
            for (int i = 0; i < subquads.Length; i++)
                subquads[i].pos += delta;
        }
    }

    public Quaternion InitRotation => shape == '|' || shape == '/' ? 
        Quaternion.Euler(0, 270, 0) : Quaternion.identity;

    private void Start()
    {
        var coll = gameObject.AddComponent<BoxCollider>();
        coll.material = Camera.main.GetComponent<Game>().quadPhyMat;
    }
    private void OnMouseDrag()
    {
        Vector3 v = new (Input.GetAxis("Mouse X"), 0, Input.GetAxis("Mouse Y"));
        if (v.magnitude > 0.3f)
            Camera.main.GetComponent<Game>().InputMove(this, v);
    }
}
