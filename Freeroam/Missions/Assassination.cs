﻿using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using Freeroam.Utils;
using System;
using System.Threading.Tasks;

namespace Freeroam.Missions
{
    internal class Target
    {
        public Ped targetPed; 
        public Ped[] bodyguards;

        public Target(Ped targetPed, Ped[] bodyguards)
        {
            this.targetPed = targetPed;
            this.bodyguards = bodyguards;
        }
    }

    class Assassination : IMission
    {
        private static Vector3[] targetSpawns = new Vector3[]
        {
            new Vector3(-829f, -1219f, 6f),
            new Vector3(-3021f, 84f, 11f),
            new Vector3(6f, -711f, 45f),
            new Vector3(378f, -1901f, 24f),
            new Vector3(-291f, -428f, 29f),
            new Vector3(-1155f, -524f, 31f)
        };

        private Target[] targets = new Target[6];
        private bool enableTick = false;

        public async void Start()
        {
            Random random = new Random();
            for (int i = 0; i < targets.Length; i++)
            {
                Ped targetPed = await Util.CreatePed(PedHash.Business01AMY, targetSpawns[i]);
                targetPed.Task.StartScenario("WORLD_HUMAN_AA_SMOKE", targetPed.Position);
                Ped[] bodyguards = await SpawnBodyguards(targetPed, random.Next(1, 4));

                Function.Call(Hash.FLASH_MINIMAP_DISPLAY);

                Blip blip = targetPed.AttachBlip();
                blip.Sprite = BlipSprite.Enemy;
                blip.Color = BlipColor.Red;
                blip.Name = Strings.MISSIONS_ASSASSINATION_BLIP;
                blip.Scale = 0.8f;

                targets[i] = new Target(targetPed, bodyguards);
            }

            Util.DisplayHelpText(Strings.MISSIONS_ASSASSINATION_INFO);
            Screen.ShowSubtitle(Strings.MISSIONS_ASSASSINATION_START, 15000);
            enableTick = true;
        }

        public void Stop(bool success)
        {
            if (!success)
            {
                foreach (Target target in targets)
                {
                    if (target != null)
                    {
                        DespawnTargetSquad(target);
                    }
                }
            }
            else
            {
                Screen.ShowNotification(Strings.MISSIONS_ASSASSINATION_ALLTARGETSKILLED);
                BaseScript.TriggerEvent(Events.MONEY_ADD, 5000);
                BaseScript.TriggerEvent(Events.XP_ADD, 30);
                BaseScript.TriggerEvent(Events.XP_ADD,)
            }
        }

        public async Task Tick()
        {
            if (enableTick)
            {
                Ped playerPed = Game.PlayerPed;

                if (playerPed.IsDead)
                {
                    BaseScript.TriggerEvent(Events.MISSION_STOP, false);
                    enableTick = false;
                }
                else
                {
                    int livingTargets = 0;
                    for (int i = targets.Length - 1; i > -1; i--)
                    {
                        if (targets[i] != null)
                        {
                            if (!targets[i].targetPed.IsDead) livingTargets++;
                            else
                            {
                                Screen.ShowNotification(Strings.MISSIONS_ASSASSINATION_TARGETKILLED);

                                Entity killer = targets[i].targetPed.GetKiller();

                                if (killer == playerPed)
                                {
                                    if (Game.Player.WantedLevel < 3) Game.Player.WantedLevel = 3;
                                }

                                DespawnTargetSquad(targets[i]);
                                targets[i] = null;
                            }
                        }
                    }

                    if (livingTargets == 0) BaseScript.TriggerEvent(Events.MISSION_STOP, true);
                }
            }

            await Task.FromResult(0);
        }

        private async Task<Ped[]> SpawnBodyguards(Ped targetPed, int amount)
        {
            PedGroup group = new PedGroup();
            group.Add(targetPed, true);
            group.FormationType = FormationType.Default;
            group.SeparationRange = 1f;

            RelationshipGroup relationship = World.AddRelationshipGroup("_ASSASSIN_TARGETS");
            relationship.SetRelationshipBetweenGroups(new RelationshipGroup(Util.GetHashKey("COP")), Relationship.Respect, true);
            relationship.SetRelationshipBetweenGroups(new RelationshipGroup(Util.GetHashKey("SECURITY_GUARD")), Relationship.Respect, true);
            targetPed.RelationshipGroup = relationship;

            Random random = new Random();
            Ped[] bodyguards = new Ped[amount];
            for (int i = 0; i < amount; i++)
            {
                float x = Util.GetRandomFloat(random, -2, 2);
                float y = Util.GetRandomFloat(random, -2, 2);
                Vector3 spawnPos = targetPed.GetOffsetPosition(new Vector3(x, y, 0f));
                Ped bodyguard = await Util.CreatePed(PedHash.FibOffice01SMM, spawnPos);
                bodyguard.Armor = 300;
                bodyguard.Weapons.Give(WeaponHash.CarbineRifle, int.MaxValue, true, true);

                bodyguard.RelationshipGroup = new RelationshipGroup(Util.GetHashKey("SECURITY_GUARD"));
                group.Add(bodyguard, false);

                bodyguards[i] = bodyguard;
            }

            return bodyguards;
        }

        private void DespawnTargetSquad(Target target)
        {
            target.targetPed.AttachedBlip.Delete();
            target.targetPed.MarkAsNoLongerNeeded();
            foreach (Ped bodyguard in target.bodyguards) bodyguard.MarkAsNoLongerNeeded();
        }
    }
}
