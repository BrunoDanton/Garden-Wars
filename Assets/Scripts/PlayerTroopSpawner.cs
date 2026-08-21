public class PlayerTroopSpawner : TroopSpawner
{
    protected override bool ShouldSpawn(int troopIndex)
    {
        return InputManager.Instance.WasTroopSpawnKeyPressed(troopIndex);
    }

    protected override bool ShouldUpgrade()
    {
        return InputManager.Instance.WasUpgradeKeyPressed();
    }
}