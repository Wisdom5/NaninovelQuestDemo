using Naninovel;
using UnityEngine;

[InitializeAtRuntime]
public class MysteriousDataChipService : IEngineService
{
    private const string CHIP_PICKED_VAR = "chipPicked";

    private CustomVariableManager _customVariableManager;

    public bool IsPickedUp { get; private set; }

    public UniTask InitializeServiceAsync()
    {
        _customVariableManager = Engine.GetService<CustomVariableManager>();
        IsPickedUp = GetBoolVariable(CHIP_PICKED_VAR);

        Debug.Log("[MysteriousDataChipService] Initialized successfully.");

        return UniTask.CompletedTask;
    }

    private bool GetBoolVariable(string name)
    {
        return bool.TryParse(_customVariableManager.GetVariableValue(name), out var result) && result;
    }

    public void PickUpChip()
    {
        IsPickedUp = true;
        SyncToNaninovel();

        Debug.Log("[MysteriousDataChipService] Chip picked.");
    }

    private void SyncToNaninovel()
    {
        _customVariableManager.SetVariableValue("chipPicked", IsPickedUp.ToString().ToLower());
    }

    public void ResetService()
    {
        IsPickedUp = false;
        SyncToNaninovel();
    }

    public void DestroyService()
    {
        // No cleanup required
    }
}
