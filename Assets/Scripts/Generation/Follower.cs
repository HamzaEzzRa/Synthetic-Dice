using UnityEngine;

[ExecuteAlways]
public class Follower : MonoBehaviour
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Vector3 offset;

    [SerializeField] private bool followX;
    [SerializeField] private bool followY;
    [SerializeField] private bool followZ;

    private void Update()
    {
        if (targetTransform != null)
        {
            Vector3 newPosition = offset;
            if (followX) newPosition.x += targetTransform.position.x;
            if (followY) newPosition.y += targetTransform.position.y;
            if (followZ) newPosition.z += targetTransform.position.z;

            transform.position = newPosition;
        }
    }
}
