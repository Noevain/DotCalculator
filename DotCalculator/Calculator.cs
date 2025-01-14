using System.Collections.Concurrent;
using System.Timers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.STD.Helper;

namespace DotCalculator;

public class Calculator
{
    //gameobjectid to running DoT counter
    public ConcurrentDictionary<uint, int> IDtoRunningDamage;
    public Calculator()
    {
        IDtoRunningDamage = new ConcurrentDictionary<uint, int>();
    }

    public void AddDamage(uint id, int damage,Status status)
    {
        if (IDtoRunningDamage.ContainsKey(id))
        {
            IDtoRunningDamage.TryUpdate(id, IDtoRunningDamage[id] + damage, IDtoRunningDamage[id]);
        }
        else
        {
            IDtoRunningDamage.TryAdd(id, damage);
        }
    }

    public void RemoveRunningDamage(uint id)
    {
        IDtoRunningDamage.TryRemove(id, out _);
    }
}
