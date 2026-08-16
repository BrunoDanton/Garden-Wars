using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

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
        if ((Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) && transform.position.x >= -maxAbsDistance)
        {
            Vector3 position = transform.position;
            position.x -= speed * Time.deltaTime;
            transform.position = position;
        }
    
        if ((Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) && transform.position.x <= maxAbsDistance)
        {
            Vector3 position = transform.position;
            position.x += speed * Time.deltaTime;
            transform.position = position;
        }
    }
}


