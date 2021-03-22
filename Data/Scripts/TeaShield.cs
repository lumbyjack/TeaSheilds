using System;
using System.Text;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using VRage.Game.Components;
using Sandbox.ModAPI;
using Sandbox.Game.EntityComponents;
using VRage.ModAPI;
using TeaSheilds.Shields;
using VRage.ObjectBuilders;

namespace TeaSheilds.Data.Scripts
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_EntityBase), false, new string[] { "LargeGridSmallShield", "LargeGridLargeShield", "SmallGridLargeShield", "SmallGridSmallShield" })]

    public class TeaShield : MyGameLogicComponent
    {

        bool isInit = false;
        int delay = 3;
        bool added = false;
        IMySlimBlock sBlock;
        IMyFunctionalBlock fBlock;
        Sandbox.ModAPI.IMyTerminalBlock sbBlock;
        float points = 0f;
        float max = 0f;
        int chargeBlink = 0;
        bool blink = false;
        int lx = 1;
        MyResourceSinkComponent sink;
        MyDefinitionId electricity = MyResourceDistributorComponent.ElectricityId;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            try
            {
                base.Init(objectBuilder);
                fBlock = Entity as IMyFunctionalBlock;
                sBlock = fBlock.SlimBlock;
                sbBlock = Entity as Sandbox.ModAPI.IMyTerminalBlock;
                NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME;
                isInit = true;
            }
            catch (Exception ex)
            {
                Echo("IAShields threw init exception", ex.ToString());
            }
        }

        public override void Close()
        {
            TeaShieldBase.shieldList.Remove(sBlock);
        }

        public void appendCustomInfo(Sandbox.ModAPI.IMyTerminalBlock block, StringBuilder info)
        {
            info.Clear();
            info.AppendLine("Max Strength: " + ((int)max).ToString() + " points");
            info.AppendLine("Current Strength: " + ((int)points).ToString() + " points");
        }

        public float powerCalc()
        {
            try
            {
                var power = 0f;
                if ((Entity as IMyFunctionalBlock).Enabled)
                    power = (float)((1 - (points / max)) * max / 1000);
                return power;
            }
            catch (Exception ex)
            {
                Echo("IAShields threw exception", ex.ToString());
                return 0f;
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            if (!isInit)
                return;

            if (!added)
                initMe();

            shieldStuff();
        }

        public void initMe()
        {
            try
            {
                max = getMax();
                if (fBlock.Name != "NaN" && !fBlock.Name.Contains("-"))
                    float.TryParse(fBlock.Name, out points);
                if (points > max)
                    points = 0f;
                if (!TeaShieldBase.shieldList.Contains(sBlock))
                    TeaShieldBase.shieldList.Add(sBlock);
                sink = fBlock.ResourceSink as MyResourceSinkComponent;
                sink.SetMaxRequiredInputByType(electricity, max / 1000);
                sink.SetRequiredInputFuncByType(electricity, powerCalc);
                sink.Update();
                fBlock.AppendingCustomInfo += appendCustomInfo;
                added = true;
                for (int i = 1; i < 9; i++)
                    sbBlock.SetEmissiveParts("Emissive" + i, Color.Red, i == 6 ? 1f : 0f);
            }
            catch (Exception ex)
            {
                Echo("IAShields threw exception", ex.ToString());
            }
        }

        public void shieldStuff()
        {
            try
            {
                points = TeaShieldBase.recharge(fBlock, max, fBlock.Enabled && sink.IsPowerAvailable(electricity, powerCalc()));
                sink.Update();

                if (!fBlock.Enabled || !fBlock.IsFunctional || !sink.IsPowerAvailable(electricity, powerCalc()))
                    for (int i = 1; i < 9; i++)
                        sbBlock.SetEmissiveParts("Emissive" + i, Color.Red, i == 6 ? 1f : 0f);
                else
                {
                    sbBlock.SetEmissiveParts("Emissive7", Color.Yellow, 0.25f);
                    sbBlock.SetEmissiveParts("Emissive8", Color.White, 1f);
                    var perc = points / max;
                    var blinkCalc = (int)(perc * 5) + 1;
                    if (perc < 0.99f)
                    {
                        if (++chargeBlink > blinkCalc)
                        {
                            chargeBlink = 0;
                            blink = !blink;
                            for (int i = 1; i < 5; i++)
                                sbBlock.SetEmissiveParts("Emissive" + i, colour(perc), i == lx ? 1f : 0f);
                            lx = lx == 4 ? 1 : lx + 1;
                        }
                        sbBlock.SetEmissiveParts("Emissive5", colour(perc), blink ? (1.3f - perc) : 0.1f);
                        sbBlock.SetEmissiveParts("Emissive6", perc < 0.9f ? Color.Cyan : Color.Green, 1f);
                    }
                    else
                        for (int i = 1; i < 7; i++)
                            sbBlock.SetEmissiveParts("Emissive" + i, Color.Green, 1f);
                }

                fBlock.RefreshCustomInfo();
            }
            catch (Exception ex)
            {
                Echo("IAShields threw exception", ex.ToString());
            }
        }

        public Color colour(float charge)
        {
            return ColorExtensions.HSVtoColor(new Vector3(charge / 3, 1f, 1f));
        }

        public float getMax()
        {
            if (sBlock.BlockDefinition.ToString().Contains("LargeGridSmallShield"))
                return 50000f;
            if (sBlock.BlockDefinition.ToString().Contains("LargeGridLargeShield"))
                return 400000f;
            if (sBlock.BlockDefinition.ToString().Contains("SmallGridLargeShield"))
                return 10000f;
            return 1250f;
        }

        public static void Echo(string msg1, string msg2 = "")
        {
            //      MyAPIGateway.Utilities.ShowMessage(msg1, msg2);
            MyLog.Default.WriteLineAndConsole(msg1 + ": " + msg2);
        }

    }
}
