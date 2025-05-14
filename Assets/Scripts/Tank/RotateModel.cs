using UnityEngine;

public class RotateModel : MonoBehaviour
{
    public float speed = 30f;
    void Update()
    {
        transform.Rotate(Vector3.up, speed * Time.deltaTime);
    }
}