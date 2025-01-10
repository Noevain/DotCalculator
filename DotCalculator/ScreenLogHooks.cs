using System;
using System.Collections.Concurrent;
using Dalamud.Game;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace DotCalculator;

public class ScreenLogHooks : IDisposable
{
    private readonly Plugin _plugin;
    const int MaxStatusesPerGameObject = 30;
    //gameobjectid to running DoT counter
    public ConcurrentDictionary<uint, int> IDtoRunningDamage = new ConcurrentDictionary<uint, int>();
    private readonly unsafe delegate* unmanaged<long, long> getScreenLogManagerDelegate;
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
           nint getScreenLogManagerAddress;
           nint addToScreenLogWithScreenLogKindAddress;
           unsafe
           {
               try
               {

                   // the BattleChara vf number (x8) is near the end of addToScreenLogWithScreenLogKind
                   getScreenLogManagerAddress = Service.SigScanner.ScanText("48 8D 81 F0 22 00 00");
                   addToScreenLogWithScreenLogKindAddress =
                       Service.SigScanner.ScanText("E8 ?? ?? ?? ?? BF ?? ?? ?? ?? EB 39");

               }
               catch (Exception ex)
               {
                   Service.Log.Error(ex, "Sig scan failed.");
                   return;
               }


               this.getScreenLogManagerDelegate = (delegate* unmanaged<long, long>)getScreenLogManagerAddress;

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
        if (option == 0)
        {
            //for DoT, target and source is always the same so need to check
            //who actually owns the status
            StatusManager* targetStatus = target->GetStatusManager();
            var id  = target->GetGameObjectId().ObjectId;
            var statusArray = targetStatus->Status;
            ulong? localPlayerId = Service.ClientState.LocalPlayer?.GameObjectId;
            for (int j = 0; j < MaxStatusesPerGameObject; j++)
            {
                Status status = statusArray[j];
                if (status.StatusId == 0) continue;
                bool sourceIsLocalPlayer = status.SourceId == localPlayerId;
                Service.Log.Debug(status.SourceId.ToString());
                Service.Log.Debug(id.ToString());
                if (sourceIsLocalPlayer)//return early since here we only care if at least 1 status is localPlayer
                {
                    if (IDtoRunningDamage.ContainsKey(id))
                    {
                        IDtoRunningDamage.TryUpdate(id, IDtoRunningDamage[id] + val1, IDtoRunningDamage[id]);
                    }
                    else
                    {
                        IDtoRunningDamage.TryAdd(id, val1);
                    }
                    break;
                }
            }

            Service.Log.Debug($"[Dot damage tick:{val1}]");
        }
        this.addToScreenLogWithScreenLogKindHook!.Original(target, source, flyTextKind, option, actionKind,
                                                           actionId, val1, val2, damageType);
    }

    public void Dispose()
    {
        addToScreenLogWithScreenLogKindHook?.Dispose();
    }
}
