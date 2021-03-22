using System;
using System.Collections.Generic;

        // get grid
        // get grid centre
        // draw sheild
        // calc HP as size of sheild
        // if entity enters sheild get entity damage and - form sheild hp
        // if sheild hp after damage  < shield hp sheild = off do damage to blocks



using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using VRage.Game.Components;
using Sandbox.ModAPI;

namespace TeaSheilds.Shields
{

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class TeaShieldBase : MySessionComponentBase
    {

        public static bool _isInit;
        public static List<IMySlimBlock> shieldList;
        public static Dictionary<IMySlimBlock, Vector3> vicDict;

        public static int hue;

        // Initialisation

        public override void UpdateBeforeSimulation()
        {
            try
            {
                if (!_isInit)
                    if (MyAPIGateway.Utilities.IsDedicated || MyAPIGateway.Session.Player != null)
                    {
                        shieldList = new List<IMySlimBlock>();
                        vicDict = new Dictionary<IMySlimBlock, Vector3>();
                        MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(10, checkDamage);
                        _isInit = true;
                    }
                    else
                        return;
                foreach (var pair in vicDict)
                    pair.Key.CubeGrid.ColorBlocks(pair.Key.Min, pair.Key.Max, pair.Value);
                vicDict.Clear();
            }
            catch (Exception ex)
            {
                Echo("IAShields threw init exception", ex.ToString());
            }
        }

        // Mod actions

        public static float recharge(IMyFunctionalBlock fBlock, float max, bool working)
        {
            // Charge reverts to 0 if shield damaged below functional, error with reading charge from Name, or some other random issue
            float points = 0f;
            if (!fBlock.IsFunctional || !float.TryParse(fBlock.Name, out points) || points < 0f || points.ToString() == "NaN")
            {
                fBlock.Name = 0f.ToString();
                return 0f;
            }

            // If turned on, recharge at inverse rate. Fastest when discharged, slowest when nearly full. Energy usage is in direct proportion.
            if (working)
            {
                points += max * 0.0025f * (1f - points / max);
                if (points > max)
                    points = max;
            }

            // Naming the block as its charge allows persistence through restarts
            fBlock.Name = points.ToString();

            return points;
        }

        public static void checkDamage(object victim, ref MyDamageInformation info)
        {
            try
            {

                // Shield only protects blocks, and not from grinding
                if (!(victim is IMySlimBlock) || info.Amount == 0f || info.Type == MyDamageType.Grind)
                    return;
                var sBlock = victim as IMySlimBlock;

                // Lowest impact method to get the active shields on target grid
                var tempList = new List<IMySlimBlock>();
                foreach (var shield in shieldList)
                    if (shield.CubeGrid == sBlock.CubeGrid && shield.FatBlock.IsFunctional)
                        tempList.Add(shield);

                if (tempList.Count == 0)
                    return;

                // Get shield points and populate parallel dictionary (used to avoid foreach issues if a shield is simultaneously recharged)
                var tempDict = new Dictionary<IMySlimBlock, float>();
                float totalPoints = 0f;
                foreach (var shield in tempList)
                {
                    float points = 0f;
                    float.TryParse(shield.FatBlock.Name, out points);
                    if (points.ToString() == "NaN" || points < 0f)
                        points = 0f;
                    tempDict.Add(shield, points);
                    totalPoints += points;
                }

                if (info.Amount > totalPoints)
                {
                    // If shield strength is insufficient to stop damage, allow appropriate amount through
                    foreach (var shield in tempList)
                        tempDict[shield] = 0f;
                    info.Amount -= totalPoints;
                }
                else
                {
                    // If shield strength is sufficient to stop damage, proportionally reduce shield(s) strength
                    foreach (var shield in tempList)
                    {
                        tempDict[shield] -= (info.Amount * (tempDict[shield] / totalPoints));
                        if (tempDict[shield] < 0f || tempDict[shield].ToString() == "NaN")
                            tempDict[shield] = 0f;
                    }
                    info.Amount = 0f;

                    // Show effect
                    //          if (info.Amount > 1f)
                    {
                        if (!vicDict.ContainsKey(sBlock))
                            vicDict.Add(sBlock, sBlock.GetColorMask());
                        float hue = 0.3f - info.Amount / totalPoints / 3f;
                        sBlock.CubeGrid.ColorBlocks(sBlock.Min, sBlock.Max, new Vector3(hue, 1f, 1f));
                    }
                }

                // Update block names with current points
                foreach (var pair in tempDict)
                    pair.Key.FatBlock.Name = pair.Value.ToString();

            }
            catch (Exception ex)
            {
                Echo("IAShields exception", ex.ToString());
            }
        }

        public static void Echo(string msg1, string msg2 = "")
        {
            //      MyAPIGateway.Utilities.ShowMessage(msg1, msg2);
            MyLog.Default.WriteLineAndConsole(msg1 + ": " + msg2);
        }
    }
}