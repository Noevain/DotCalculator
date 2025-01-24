using System.Collections.Concurrent;
using System.Timers;
using Dalamud.Game.ClientState.Structs;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.STD.Helper;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Text.SeStringHandling;
using Lumina.Excel.Sheets;
using Status = FFXIVClientStructs.FFXIV.Client.Game.Status;

namespace DotCalculator;

public class Calculator
{
    //gameobjectid to running DoT counter
    public ConcurrentDictionary<uint, int> IDtoRunningDamage;
    private Plugin _plugin;
    private SeStringBuilder _seStringBuilder = new SeStringBuilder();
    public Calculator(Plugin plugin)
    {
        _plugin = plugin;
        IDtoRunningDamage = new ConcurrentDictionary<uint, int>();
        _seStringBuilder.AddText("Dot Damage:");
    }

    public void AddDamage(uint id, int damage,uint statusID)
    {
        if (IDtoRunningDamage.ContainsKey(id))
        {
            var dmg = CalculateDamage(damage, statusID);
            IDtoRunningDamage.TryUpdate(id, IDtoRunningDamage[id] + dmg, IDtoRunningDamage[id]);
        }
        else
        {
            var dmg = CalculateDamage(damage, statusID);
            IDtoRunningDamage.TryAdd(id, dmg);
        }
        
        if (_plugin.Config.FlyTextEnabled)
        {
            if (_plugin.calculator.IDtoRunningDamage.TryGetValue(id, out var runningDamage))
            {
                Service.FlyTextGui.AddFlyText(FlyTextKind.Damage, 1,
                                              (uint)_plugin.calculator.IDtoRunningDamage[id], 0,
                                              _seStringBuilder.Build(), SeString.Empty, 0, 0, 0);
            }
        }
    }

    
    public int CalculateDamage(int damage, uint statusId)
    {
        //we will split it into comps to make sure I am not missing anything
        int status_potency = StatusToPotency(statusId);
        if (status_potency == -1)
        {
            Service.Log.Warning($"Status ID not recognized for calculating damage:{statusId}");
            return 0;
        }

        unsafe
        {
            var uiState = UIState.Instance();
            var lvl = uiState->PlayerState.CurrentLevel;
            var levelModifier = LevelModifiers.LevelTable[lvl];
            var jobId = (JobId)uiState->PlayerState.CurrentClassJobId;
            var det = Equations.CalcDet(uiState->PlayerState.Attributes[(int)Attributes.Determination],levelModifier);
            var critdmg = Equations.CalcCritDmg(uiState->PlayerState.Attributes[(int)Attributes.CriticalHit],
                                                levelModifier);
            var critrate = Equations.CalcCritRate(uiState->PlayerState.Attributes[(int)Attributes.CriticalHit],
                                                  levelModifier);
            var dh = Equations.CalcDh(uiState->PlayerState.Attributes[(int)Attributes.DirectHit],levelModifier);
            var ten = Equations.CalcTenacityDmg(uiState->PlayerState.Attributes[(int)Attributes.Tenacity],levelModifier);
            double speed;
            if (!jobId.IsCaster())
            {
                speed = Equations.CalcSpeed(uiState->PlayerState.Attributes[(int)Attributes.SkillSpeed],levelModifier);
            }
            else
            {
                speed = Equations.CalcSpeed(uiState->PlayerState.Attributes[(int)Attributes.SpellSpeed], levelModifier);
            }
            var (ilvlSync, ilvlSyncType) = IlvlSync.GetCurrentIlvlSync();
            var avgDamage= Equations.CalcExpectedOutput(UIState.Instance(),jobId,det,critdmg,critrate,dh,ten,speed,levelModifier,ilvlSync,ilvlSyncType,status_potency);
            Service.Log.Verbose($"Calculated Damage: {avgDamage}");
            Service.Log.Verbose($"Damage tick: {damage}");
            return (int)avgDamage;
        }
    }


    //so the status effect has no information on the potency
    //you could either match the name to the action to get the potency,or just hardcode it....
    //actually turns out the client doesn't know potencies and you'd have to text parse action description
    //to find out potencies.....
    //this definitely will not bite me in the ass later!
    public int StatusToPotency(uint id)
    {
        switch (id)
        {
            //Melee DPS
            //Dragoon
            case 2719: return 45;//Chaotic Spring
            case 118: return 40;//Chaos Thrust
            //Ninja
            case 502: return 80;//Doton
            //Samurai
            case 1228: return 50;//Higanbana
            //Physical Range
            //Bard
            case 124: return 15;//Venomous Bite
            case 129: return 20;//Windbite
            case 1201: return 25;//Stormbite
            case 1200: return 20;//Caustic Bite
            //Magical DPS
            //Black Mage
            case 161: return 45;//Thunder
            case 162: return 30;//Thunder II
            case 163: return 50;//Thunder III
            case 1210: return 40;//Thunder IV
            case 3871: return 60;//High Thunder
            case 3872: return 40;//High Thunder II
            //Tanks
            //DRK,the reason why this plugin exist.
            case 749: return 50;//Salted Earth
            //GNB
            case 1837: return 60;//Sonic break
            //Healers
            //AST
            case 838: return 50;//Combust
            case 843: return 60;//Combust II
            case 1881: return 70;//Combust III
            //SCH
            case 179: return 20;//Bio
            case 189: return 40;//Bio II
            case 1895: return 70;//Biolysis
            //WHM
            case 143: return 30;//Aero
            case 144: return 50;//Aero II
            case 1871: return 65;//Dia
            //SGE
            case 2614: return 40;//Dosis
            case 2615: return 60;//Dosis II
            case 2616: return 75;//Dosis III
            case 3897: return 40;//Dyskrasia(AoE 82 dot)
            default: return -1;
        }
    }

    public void RemoveRunningDamage(uint id)
    {
        IDtoRunningDamage.TryRemove(id, out _);
    }
}
