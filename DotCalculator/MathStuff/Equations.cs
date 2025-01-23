using System;
using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace DotCalculator;

public class Equations {
    private static (uint Ilvl, int Dmg)? cachedIlvl = null;
    
    public static double CalcCritDmg(int crit, in LevelModifier lvlModifier) {
        var cVal = Math.Floor(200d * (crit - lvlModifier.Sub) / lvlModifier.Div + 1400) / 1000d;
        return cVal;
    }
    
    public static double CalcCritRate(int crit,in LevelModifier lvlModifier) {
        var cVal = Math.Floor(200d * (crit - lvlModifier.Sub) / lvlModifier.Div + 50) / 1000d;
        return cVal;
    }
    
    public static double CalcDet(int det, in LevelModifier lvlModifier) {
        var cVal =  Math.Floor(140d * (det - lvlModifier.Main) / lvlModifier.Div) / 1000d;
        return cVal;
    }
    
    public static double CalcDh(int dh, in LevelModifier lvlModifier) {
        var cVal = Math.Floor(550d * (dh - lvlModifier.Sub) / lvlModifier.Div) / 1000d;
        return cVal;
    }
    
    public static double CalcTenacityDmg(int ten,in LevelModifier lvlModifier) {
        var cVal = Math.Floor(112d * (ten - lvlModifier.Sub) / lvlModifier.Div) / 1000d;
        return cVal;
    }
    
    public static double CalcSpeed(int speed, in LevelModifier lvlModifier) {
        var (_, sub, div) = lvlModifier;
        var cVal = Math.Floor(130d * (speed - sub) / div) / 1000d;
        return cVal;
    }
    
    
    
    public static unsafe (double AvgDamage, double NormalDamage, double CritDamage) CalcExpectedOutput(UIState* uiState, JobId jobId, double det, double critMult, double critRate, double dh, double ten,double speed, in LevelModifier lvlModifier, uint? ilvlSync, IlvlSyncType ilvlSyncType,int potency) {
        try {
            var lvl = uiState->PlayerState.CurrentLevel;
            var ap = uiState->PlayerState.Attributes[(int)(jobId.IsCaster() ? Attributes.AttackMagicPotency : Attributes.AttackPower)];
            var inventoryExcelData = (ushort*)((IntPtr)InventoryManager.Instance() + 9360);
            var weaponBaseDamage = /* phys/magic damage */ inventoryExcelData[jobId.IsCaster() ? 21 : 20] + /* hq bonus */ inventoryExcelData[33];
            if (ilvlSync != null && ( /* equip lvl */ inventoryExcelData[39] > lvl || ilvlSyncType == IlvlSyncType.Strict)) {
                if (cachedIlvl?.Ilvl != ilvlSync)
                    cachedIlvl = (ilvlSync.Value, Service.DataManager.GetExcelSheet<ItemLevel>().GetRow(ilvlSync.Value).PhysicalDamage);
                weaponBaseDamage = Math.Min(cachedIlvl.Value.Dmg, weaponBaseDamage);
            }
            
            var weaponDamage = Math.Floor(weaponBaseDamage + lvlModifier.Main * jobId.AttackModifier() / 1000.0) / 100.0;
            var lvlAttackModifier = jobId.UsesTenacity() ? LevelModifiers.TankAttackModifier(lvl) : LevelModifiers.AttackModifier(lvl);
            var atk = Math.Floor(100 + lvlAttackModifier * (ap - lvlModifier.Main) / lvlModifier.Main) / 100;
            //https://www.akhmorning.com/allagan-studies/how-to-be-a-math-wizard/shadowbringers/damage-and-healing/#damage-over-time
            //damage calculation for DoTs,phys or magic matters, for our purposes we will assume every caster is magic and
            //every non caster is phys but that might not be always true in the future(or even now idk)
            double baseMultiplier;
            if (!jobId.IsCaster())
            {
                //phys
                baseMultiplier = Math.Floor(potency * atk * weaponDamage);
            }
            else
            {
                //magic
                baseMultiplier = Math.Floor(potency * weaponDamage * speed);
            }
            var withDet = Math.Floor(baseMultiplier * (1 + det));
            var withTen = Math.Floor(withDet * (1 + ten));
            if (!jobId.IsCaster())
            {
                withTen = Math.Floor(withTen * (1 + speed));//phys get spd applied here instead
            }
            var normalDamage = Math.Floor(withTen * jobId.TraitModifiers(lvl));
            var avgDamage = Math.Floor(Math.Floor(normalDamage * (1 + (critMult - 1) * critRate)) * (1 + dh * 0.25));
            var critDamage = Math.Floor(normalDamage * critMult);
            return (avgDamage, normalDamage, critDamage);
        } catch (Exception e) {
            Service.Log.Warning(e, "Failed to calculate raw damage");
            return (0, 0, 0);
        }
    }
}
