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
    private SeStringBuilder _seStringBuilder = new SeStringBuilder();
    //gameobjectid to running DoT counter
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
               _seStringBuilder.AddText("Dot Damage:");
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
            /*Service.Log.Debug(option.ToString());
            Service.Log.Debug(actionKind.ToString());
            Service.Log.Debug(actionId.ToString());
            Service.Log.Debug(val1.ToString());
            Service.Log.Debug(val2.ToString());
            Service.Log.Debug(flyTextKind.ToString());
            */

            if (option == 0)
            {
                //for DoT, target and source is always the same so need to check
                //who actually owns the status
                StatusManager* targetStatus = target->GetStatusManager();
                var id = target->GetGameObjectId().ObjectId;
                var statusArray = targetStatus->Status;
                ulong? localPlayerId = Service.ClientState.LocalPlayer?.GameObjectId;
                for (int j = 0; j < MaxStatusesPerGameObject; j++)
                {
                    Status status = statusArray[j];
                    if (status.StatusId == 0) continue;
                    bool sourceIsLocalPlayer = status.SourceId == localPlayerId;
                    //Service.Log.Debug(status.SourceId.ToString());
                    //Service.Log.Debug(id.ToString());
                    //Service.Log.Debug(status.RemainingTime.ToString());
                    if (sourceIsLocalPlayer) //return early since here we only care if at least 1 status is localPlayer
                    {
                        _plugin.calculator.AddDamage(id, val1, status);

                        if (_plugin.Config.FlyTextEnabled)
                        {
                            if (_plugin.calculator.IDtoRunningDamage.TryGetValue(id, out var runningDamage))
                            {
                                Service.FlyTextGui.AddFlyText(FlyTextKind.Damage, 1, (uint)_plugin.calculator.IDtoRunningDamage[id], 0,
                                                              _seStringBuilder.Build(), SeString.Empty, 0, 0, 0);
                            }
                            else
                            {
                                Service.FlyTextGui.AddFlyText(FlyTextKind.Damage, 1, (uint)val1, 0,
                                                              _seStringBuilder.Build(), SeString.Empty, 0, 0, 0);
                            }
                        }

                        break;
                    }
                }
                
                //Service.Log.Debug($"[Dot damage tick:{val1}]");
            }

            else if (option == 5 && flyTextKind == FlyTextKind.DebuffFading)
            {
                Service.Log.Debug("Debuff fading");
                var id = target->GetGameObjectId().ObjectId;
                if (_plugin.calculator.IDtoRunningDamage.ContainsKey(id))
                {
                    Service.Log.Debug("Checking statuses");
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
                            if (status.RemainingTime == 0)
                            {
                                shouldRemove = true;
                                break;
                            }
                        }
                    }
                    Service.Log.Debug(shouldRemove.ToString());
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
