using System;
using System.Collections.Concurrent;
using System.Linq;
using Dalamud.Game;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.Sheets;
using Status = FFXIVClientStructs.FFXIV.Client.Game.Status;

namespace DotCalculator;

public class ScreenLogHooks : IDisposable
{
    private readonly Plugin _plugin;
    const int MaxStatusesPerGameObject = 30;
    //gameobjectid to running DoT counter
    private unsafe delegate void AddToScreenLogWithScreenLogKindDelegate(
        Character* target,
        Character* source,
        FlyTextKind logKind,
        byte option,
        byte actionKind,
        int actionId,
        int val1,
        int val2,
        byte damageType);
    private readonly Hook<AddToScreenLogWithScreenLogKindDelegate>? addToScreenLogWithScreenLogKindHook;
    
    public ScreenLogHooks(Plugin plugin)
    {
           _plugin = plugin;
           nint addToScreenLogWithScreenLogKindAddress;
           unsafe
           {
               try
               {

                   // the BattleChara vf number (x8) is near the end of addToScreenLogWithScreenLogKind
                   addToScreenLogWithScreenLogKindAddress =
                       Service.SigScanner.ScanText("E8 ?? ?? ?? ?? BF ?? ?? ?? ?? EB 39");

               }
               catch (Exception ex)
               {
                   Service.Log.Error(ex, "Sig scan failed.");
                   return;
               }
               this.addToScreenLogWithScreenLogKindHook =
                   Service.GameInteropProvider.HookFromAddress<AddToScreenLogWithScreenLogKindDelegate>(
                       addToScreenLogWithScreenLogKindAddress, this.AddToScreenLogWithScreenLogKindDetour);
               this.addToScreenLogWithScreenLogKindHook.Enable();
           }
    }
    
    private unsafe void AddToScreenLogWithScreenLogKindDetour(
        Character* target,
        Character* source,
        FlyTextKind flyTextKind,
        byte option, // 0 = DoT / 1 = % increase / 2 = blocked / 3 = parried / 4 = resisted / 5 = default
        byte actionKind,
        int actionId,
        int val1,
        int val2,
        byte damageType)
    {
        try
        {
            if (_plugin.InPvp)
            {
                this.addToScreenLogWithScreenLogKindHook!.Original(target, source, flyTextKind, option, actionKind,
                                                                   actionId, val1, val2, damageType);
                return;
            }
            if (option == 0)//damageType 0 for normal dots,2 for ground dots
            {
                //for DoT, target and source is always the same so need to check
                //who actually owns the status
                StatusManager* targetStatus = target->GetStatusManager();
                var id = target->GetGameObjectId().ObjectId;
                var statusArray = targetStatus->Status;
                ulong? localPlayerId = Service.ClientState.LocalPlayer?.GameObjectId;
                bool isGroundDoT = damageType == 2;
                if (!isGroundDoT){
                    for (int j = 0; j < MaxStatusesPerGameObject; j++)
                    {
                        Status status = statusArray[j];
                        if (status.StatusId == 0) continue;
                        bool sourceIsLocalPlayer = status.SourceId == localPlayerId;
                        Service.Log.Verbose(isGroundDoT.ToString());
                        if (sourceIsLocalPlayer)
                        {
                            _plugin.calculator.AddDamage(id, val1, status.StatusId);
                        }
                    }
                }
                else
                {
                    //so... Salted earth has no status effect on target as far as I can tell
                    //cringe
                    var salted = Service.ClientState.LocalPlayer?.StatusList.ToList()
                                        .FirstOrDefault(x => x.StatusId == 749);
                    if (salted != null)
                    {
                        _plugin.calculator.AddDamage(id, val1, salted.StatusId);
                    }
                }
                
                //Service.Log.Verbose($"[Dot damage tick:{val1}]");
            }

            else if (option == 5 && flyTextKind == FlyTextKind.DebuffFading)
            {
                Service.Log.Verbose("Debuff fading");
                var id = target->GetGameObjectId().ObjectId;
                if (_plugin.calculator.IDtoRunningDamage.ContainsKey(id))
                {
                    Service.Log.Verbose("Checking statuses");
                    StatusManager* targetStatus = target->GetStatusManager();
                    var statusArray = targetStatus->Status;
                    bool shouldRemove = false;
                    for (int j = 0; j < MaxStatusesPerGameObject; j++)
                    {
                        Status status = statusArray[j];
                        if (status.StatusId == 0) continue;
                        ulong? localPlayerId = Service.ClientState.LocalPlayer?.GameObjectId;
                        bool sourceIsLocalPlayer = status.SourceId == localPlayerId;
                        if (sourceIsLocalPlayer) //return early since here we only care if at least 1 status is localPlayer
                        {
                            //Status effect fading are still on the target with 0 duration
                            //when this hook is ran
                            if (status.RemainingTime == 0)
                            {
                                shouldRemove = true;
                                break;
                            }
                        }
                    }
                    if (shouldRemove)
                    {
                        _plugin.calculator.RemoveRunningDamage(id);
                    }
                }
            }

            this.addToScreenLogWithScreenLogKindHook!.Original(target, source, flyTextKind, option, actionKind,
                                                               actionId, val1, val2, damageType);
        }catch (Exception ex)
        {
            Service.Log.Error(ex.ToString());
            this.addToScreenLogWithScreenLogKindHook!.Original(target, source, flyTextKind, option, actionKind,
                                                               actionId, val1, val2, damageType);
            return;
        }
    }

    public void Dispose()
    {
        addToScreenLogWithScreenLogKindHook?.Dispose();
    }
}
