using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTroopSpawner : TroopSpawner
{
    protected override bool ShouldSpawn()
    {
        return Keyboard.current.digit1Key.wasPressedThisFrame;
    }
}