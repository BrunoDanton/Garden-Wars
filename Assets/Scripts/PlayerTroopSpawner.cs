using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTroopSpawner : TroopSpawner
{
    protected override bool ShouldSpawn(int troopIndex)
    {
        Key key = Key.Digit1 + troopIndex;
        return Keyboard.current[key].wasPressedThisFrame;
    }

    protected override bool ShouldUpgrade()
    {
        return Keyboard.current.qKey.wasPressedThisFrame;
    }
}