using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawnTroops : MonoBehaviour
{
    public GameObject troop;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            float offset = transform.lossyScale.z/2 - troop.transform.lossyScale.z/2;
            Vector3 position = transform.position + new Vector3(0, 0, Random.Range(-offset, offset));

            Instantiate(troop, position, Quaternion.identity);
        }
    }
}
