using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float speed = 3;
    [SerializeField] private float towerAbsDistanceToOrigin = 50;
    [SerializeField] private float cameraOffset = 10;
    private float maxAbsDistance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        maxAbsDistance = towerAbsDistanceToOrigin - cameraOffset;
    }

    // Update is called once per frame
    void Update()
    {
        if (InputManager.Instance.MoveCameraLeftHeld && transform.position.x >= -maxAbsDistance)
        {
            Vector3 position = transform.position;
            position.x -= speed * Time.deltaTime;
            transform.position = position;
        }

        if (InputManager.Instance.MoveCameraRightHeld && transform.position.x <= maxAbsDistance)
        {
            Vector3 position = transform.position;
            position.x += speed * Time.deltaTime;
            transform.position = position;
        }
    }
}