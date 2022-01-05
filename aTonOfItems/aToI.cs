﻿using System;
using System.Collections.Generic;
using UnityEngine;
using BepInEx;
using RogueLibsCore;

namespace aTonOfItems
{
	[BepInPlugin(pluginGuid, pluginName, pluginVersion)]
	[BepInDependency(RogueLibs.pluginGuid, "3.3.0")]
	public class ATOI : BaseUnityPlugin
	{
		public const string pluginGuid = "abbysssal.streetsofrogue.atoi";
		public const string pluginName = "a Ton of Items";
		public const string pluginVersion = "0.2";

		public void Awake()
		{
			RoguePatcher patcher = new RoguePatcher(this, GetType());

			#region Quantum Fud
			QuantumFudCooldowns = new Dictionary<InvItem, float>();

			Sprite sprite = RogueUtilities.ConvertToSprite(Properties.Resources.QuantumFud);
			CustomItem quantumFud = RogueLibs.CreateCustomItem("QuantumFud", sprite, false,
				new CustomNameInfo("Quantum Fud",
					null, null, null, null,
					"Квантовый хафчик",
					null, null),
				new CustomNameInfo("A very complicated piece of quantum technology. When you eat it, its quantum equivalent clone is consumed, while the original thing remains intact.",
					null, null, null, null,
					"Очень сложное квантовое устройство. При его поедании, потребляется его квантово-эквивалентный клон, в то время как оригинал остаётся нетронутым.",
					null, null),
				item =>
				{
					item.itemType = "Food";
					item.Categories.Add("Food");
					item.Categories.Add("Technology");
					item.itemValue = 180;
					item.healthChange = 1;
					item.cantBeCloned = true;
					item.goesInToolbar = true;
				});
			quantumFud.Prerequisites.Add("FoodProcessor");

			quantumFud.UnlockCost = 10;
			quantumFud.CostInCharacterCreation = 10;
			quantumFud.CostInLoadout = 10;

			quantumFud.UseItem = (item, agent) =>
			{
				if (agent.statusEffects.hasTrait("OilRestoresHealth"))
					agent.SayDialogue("OnlyOilGivesHealth");
				else if (agent.statusEffects.hasTrait("BloodRestoresHealth"))
					agent.SayDialogue("OnlyBloodGivesHealth");
				else if (agent.electronic)
					agent.SayDialogue("OnlyChargeGivesHealth");
				else if (agent.statusEffects.hasTrait("CannibalizeRestoresHealth"))
					agent.SayDialogue("OnlyCannibalizeGivesHealth");
				else if (agent.health == agent.healthMax)
					agent.SayDialogue("HealthFullCantUseItem");
				else
				{
					if (!QuantumFudCooldowns.TryGetValue(item, out float cd))
					{
						QuantumFudCooldowns.Add(item, 0f);
						cd = 0f;
					}
					if (cd == 0f)
					{
						int heal = new ItemFunctions().DetermineHealthChange(item, agent);
						agent.statusEffects.ChangeHealth(heal);
						if (agent.statusEffects.hasTrait("HealthItemsGiveFollowersExtraHealth") || agent.statusEffects.hasTrait("HealthItemsGiveFollowersExtraHealth2"))
							new ItemFunctions().GiveFollowersHealth(agent, heal);
						item.gc.audioHandler.Play(agent, "UseFood");
						new ItemFunctions().UseItemAnim(item, agent);

						QuantumFudCooldowns[item] = 0.5f;
					}
					return;
				}
				item.gc.audioHandler.Play(agent, "CantDo");
			};
			#endregion

			#region Wild Bypasser
			sprite = RogueUtilities.ConvertToSprite(Properties.Resources.WildBypasser);
			CustomItem wildBypasser = RogueLibs.CreateCustomItem("WildBypasser", sprite, false,
				new CustomNameInfo("Wild Bypasser",
					null, null, null, null,
					"Универсальный проход сквозь стены",
					null, null),
				new CustomNameInfo("Warps you in the direction you're facing. Teleports through any amount of walls.",
					null, null, null, null,
					"Перемещает тебя в направлении, в которое ты смотришь. Телепортирует сквозь любое количество стен.",
					null, null),
				item =>
				{
					item.itemType = "Tool";
					item.Categories.Add("Technology");
					item.Categories.Add("Usable");
					item.Categories.Add("Stealth");
					item.itemValue = 60;
					item.initCount = 1;
					item.rewardCount = 1;
					item.stackable = true;
					item.goesInToolbar = true;
				});
			wildBypasser.Prerequisites.Add("WallBypasser");

			wildBypasser.UnlockCost = 10;
			wildBypasser.CostInCharacterCreation = 2;
			wildBypasser.CostInLoadout = 2;

			wildBypasser.UseItem = (item, agent) =>
			{
				Vector3 position = agent.agentHelperTr.localPosition = Vector3.zero;
				TileData tileData;
				int limit = 0;
				do
				{
					position.x += 0.64f;
					agent.agentHelperTr.localPosition = position;
					tileData = GameController.gameController.tileInfo.GetTileData(agent.agentHelperTr.position);

				} while ((GameController.gameController.tileInfo.IsOverlapping(agent.agentHelperTr.position, "Anything") || tileData.wallMaterial != wallMaterialType.None) && limit++ < 250);

				if (limit > 249) return;

				agent.SpawnParticleEffect("Spawn", agent.tr.position);
				agent.Teleport(new Vector3(agent.agentHelperTr.position.x, agent.agentHelperTr.position.y, agent.tr.position.z), false, true);
				agent.rb.velocity = Vector2.zero;

				if (!(agent.statusEffects.hasTrait("ThiefToolsMayNotSubtract2") && GameController.gameController.percentChance(agent.DetermineLuck(80, "ThiefToolsMayNotSubtract", true))) && !(agent.statusEffects.hasTrait("ThiefToolsMayNotSubtract") && GameController.gameController.percentChance(agent.DetermineLuck(40, "ThiefToolsMayNotSubtract", true))))
					item.database.SubtractFromItemCount(item, 1);

				agent.SpawnParticleEffect("Spawn", agent.tr.position, false);
				GameController.gameController.audioHandler.Play(agent, "Spawn");

				new ItemFunctions().UseItemAnim(item, agent);
			};
			#endregion

			#region Blank Voodoo Doll / Voodoo Doll
			VoodooCooldowns = new Dictionary<InvItem, float>();
			VoodooUpdateList = new Dictionary<InvItem, Agent>();
			
			sprite = RogueUtilities.ConvertToSprite(Properties.Resources.VoodooInactive);
			CustomItem blankVoodooDoll = RogueLibs.CreateCustomItem("BlankVoodooDoll", sprite, false,
				new CustomNameInfo("Blank Voodoo Doll",
					null, null, null, null,
					"Непривязанная кукла Вуду",
					null, null),
				new CustomNameInfo("Bind it to someone first. Has limited uses.",
					null, null, null, null,
					"Сначала привяжите её к кому-нибудь. Имеет ограниченное количество использований.",
					null, null),
				item =>
				{
					item.itemType = "Tool";
					item.Categories.Add("Usable");
					item.Categories.Add("Stealth");
					item.Categories.Add("Weird");
					item.itemValue = 100;
					item.initCount = 3;
					item.rewardCount = 3;
					item.stackable = true;
					item.hasCharges = true;
					item.goesInToolbar = true;
				});
			blankVoodooDoll.Prerequisites.Add("Necronomicon");

			blankVoodooDoll.UnlockCost = 10;
			blankVoodooDoll.CostInCharacterCreation = 4;
			blankVoodooDoll.CostInLoadout = 4;

			blankVoodooDoll.TargetFilter = (item, agent, obj) => obj is Agent a && !a.dead;
			blankVoodooDoll.TargetObject = (item, agent, obj) =>
			{
				item.invInterface.HideTarget();

				item.database.DestroyItem(item);
				InvItem newItem = item.database.AddItem("VoodooDoll2", item.invItemCount);

				VoodooUpdateList.Add(newItem, (Agent)obj);
				VoodooCooldowns.Add(newItem, 0f);
			};
			blankVoodooDoll.SetTargetText(new CustomNameInfo("Bind",
				null, null, null, null,
				"Привязать",
				null, null));

			sprite = RogueUtilities.ConvertToSprite(Properties.Resources.Voodoo);
			CustomItem voodooDoll = RogueLibs.CreateCustomItem("VoodooDoll2", sprite, false,
				new CustomNameInfo("Voodoo Doll",
					null, null, null, null,
					"Кукла Вуду",
					null, null),
				new CustomNameInfo("Combine the doll with any weapon/consumable to inflict damage/effects on the victim. Combine with itself to unbind.",
					null, null, null, null,
					"Объедините куклу с любым оружием/расходником для нанесения урона/эффектов жертве. Объедините с самой собой, чтобы убрать связь.",
					null, null),
				item =>
				{
					item.itemType = "Combine";
					item.Categories.Add("Social");
					item.Categories.Add("Stealth");
					item.Categories.Add("Weird");
					item.itemValue = 200;
					item.initCount = 1;
					item.rewardCount = 1;
					item.stackable = true;
					item.hasCharges = true;
				});
			voodooDoll.Available = voodooDoll.AvailableInCharacterCreation = voodooDoll.AvailableInItemTeleporter = false;

			voodooDoll.CombineFilter = (item, agent, otherItem) =>
			{
				if (otherItem.itemType == "WeaponMelee") return true;
				if (otherItem.itemType == "WeaponProjectile") return true;
				if (otherItem.itemType == "Consumable") return true;
				if (item == otherItem) return true;

				return false;
			};
			voodooDoll.CombineItems = (item, agent, otherItem) =>
			{
				foreach (KeyValuePair<InvItem, float> a in VoodooCooldowns)
					if (a.Key.invItemID == item.invItemID && a.Value > 0)
						return;
				Agent target = null;
				foreach (KeyValuePair<InvItem, Agent> a in VoodooUpdateList)
					if (a.Key.invItemID == item.invItemID)
					{
						target = a.Value;
						break;
					}
				if (otherItem == item)
				{
					item.database.DestroyItem(item);
					if (item.invItemCount > 1)
						item.database.AddItem("BlankVoodooDoll", item.invItemCount - 1);

					VoodooCooldowns.Remove(item);
					VoodooUpdateList.Remove(item);

					item.agent.mainGUI.invInterface.HideDraggedItem();
					item.agent.mainGUI.invInterface.HideTarget();
					return;
				}
				if (target == null) return;
				if (otherItem.itemType == "WeaponMelee")
				{
					Quaternion rn = UnityEngine.Random.rotation;
					target.statusEffects.ChangeHealth(-otherItem.meleeDamage / 2, agent);
					target.movement.KnockBackBullet(rn, 80f, true, agent);
					target.relationships.SetRel(agent, "Hateful");
					target.relationships.AddRelHate(agent, 500);
					item.gc.audioHandler.Play(target, "MeleeHitAgentCutSmall");

					VoodooCooldowns[item] = 0.5f;

					string effect = "BloodHit";
					if (target.inhuman || target.mechFilled || target.mechEmpty) effect += "Yellow";
					if (otherItem.meleeDamage > 8) effect += "Med";
					else if (otherItem.meleeDamage >= 12) effect += "Large";

					item.gc.spawnerMain.SpawnParticleEffect(effect, target.tr.position, rn.eulerAngles.z + 90f);
				}
				else if (otherItem.itemType == "WeaponProjectile")
				{
					Vector2 pos = target.curPosition;
					int myRand = new System.Random().Next();
					bulletStatus type;
					string soundName = otherItem.invItemName + "Fire";
					float cd = 0.09f;
					switch (otherItem.invItemName)
					{
						case "Pistol": type = bulletStatus.Normal; cd = 0.09f; break;
						case "Shotgun": type = bulletStatus.Normal; cd = 0.39f; break;
						case "MachineGun": type = bulletStatus.Normal; cd = 0.09f; break;
						case "LaserGun": type = bulletStatus.Laser; cd = 0.13f; break;
						case "Revolver": type = bulletStatus.Revolver; cd = 0.79f; break;
						case "Flamethrower": type = bulletStatus.Fire; cd = 0.05f; break;
						case "FireExtinguisher": type = bulletStatus.FireExtinguisher; cd = 0.05f; break;
						case "WaterCannon": type = bulletStatus.Water2; cd = 0.05f; break;
						case "GhostBlaster": type = bulletStatus.GhostBlaster; soundName = "GhostGibberFire"; cd = 0.05f; break;
						case "RocketLauncher": type = bulletStatus.Rocket; cd = 0.39f; break;
						case "RocketLauncherInf": type = bulletStatus.Rocket; soundName = "RocketLauncherFire"; cd = 2.2f; break;
						case "Taser": type = bulletStatus.Taser; cd = 0.39f; break;
						case "TranquilizerGun": type = bulletStatus.Tranquilizer; soundName = "TranquilizerFire"; cd = 0.39f; break;
						case "ShrinkRay": type = bulletStatus.Shrink; cd = 0.39f; break;
						case "FreezeRay": type = bulletStatus.FreezeRay; cd = 0.39f; break;
						case "WaterPistol": type = bulletStatus.WaterPistol; cd = 0.09f; break;
						case "LeafBlower": type = bulletStatus.LeafBlower; cd = 0.05f; break;
						case "ResearchGun": type = bulletStatus.ResearchGun; cd = 0.05f; break;
						case "OilContainer": new ItemFunctions().UseItem(otherItem, target); return;
						default: type = bulletStatus.None; break;
					}
					if (agent.accuracyStatMod <= 0)
						cd += 0.2f;
					else if (agent.accuracyStatMod == 1)
						cd += 0.1f;
					else if (agent.accuracyStatMod == 3)
						cd -= 0.1f;
					if (otherItem.contents.Contains("RateOfFireMod"))
						cd -= 0.15f;
					VoodooCooldowns[item] = Mathf.Max(cd, 0.09f);

					Bullet bullet = agent.gun.spawnBullet(type, otherItem, myRand, false);
					bullet.curPosition = pos;
					bullet.transform.position = pos;
					bullet.transform.rotation = UnityEngine.Random.rotation;

					bullet.rubber = otherItem.contents.Contains("RubberBulletsMod");
					if (bullet.silenced = otherItem.contents.Contains("Silencer"))
						otherItem.gc.audioHandler.Play(agent, "SilencedGun");
					else
						otherItem.gc.audioHandler.Play(agent, soundName);

					agent.gun.SubtractBullets(1, otherItem);
				}
				else if (otherItem.itemType == "Consumable")
					new ItemFunctions().UseItem(otherItem, target);
			};
			#endregion

			#region SPYTRON 3000
			sprite = RogueUtilities.ConvertToSprite(Properties.Resources.SPYTRON3000);
			CustomItem spytron3000 = RogueLibs.CreateCustomItem("SPYTRON3000", sprite, false,
				new CustomNameInfo("SPYTRON 3000",
					null, null, null, null,
					"Шпионотрон 3000",
					null, null),
				new CustomNameInfo("Always wanted to be someone else? Now you can!",
					null, null, null, null,
					"Всегда хотели быть кем-то другим? Теперь вы можете!",
					null, null),
				item =>
				{
					item.itemType = "Tool";
					item.Categories.Add("Social");
					item.Categories.Add("Stealth");
					item.Categories.Add("Technology");
					item.Categories.Add("Usable");
					item.itemValue = 40;
					item.initCount = 2;
					item.rewardCount = 3;
					item.stackable = true;
					item.goesInToolbar = true;
				});
			spytron3000.Prerequisites.Add("BodySwapper");

			spytron3000.UnlockCost = 10;
			spytron3000.CostInCharacterCreation = 3;
			spytron3000.CostInLoadout = 2;

			spytron3000.TargetFilter = (item, agent, obj) => obj is Agent a && !a.dead && a != agent;
			spytron3000.TargetObject = (item, agent, obj) =>
			{
				Agent target = (Agent)obj;
				
				string prev = agent.agentName;
				agent.agentName = target.agentName;

				agent.relationships.CopyLooks(target);

				agent.gc.audioHandler.Play(agent, "Spawn");
				agent.gc.spawnerMain.SpawnParticleEffect("Spawn", agent.tr.position, 0f);

				foreach (Relationship rel in target.relationships.RelList)
				{
					Relationship otherRel = rel.agent.relationships.GetRelationship(target);

					agent.relationships.SetRel(rel.agent, rel.relType);
					agent.relationships.SetRelHate(rel.agent, 0);
					agent.relationships.GetRelationship(rel.agent).secretHate = rel.secretHate;
					agent.relationships.GetRelationship(rel.agent).mechHate = rel.mechHate;

					rel.agent.relationships.SetRel(agent, otherRel.relType);
					rel.agent.relationships.SetRelHate(agent, 0);
					rel.agent.relationships.GetRelationship(agent).secretHate = otherRel.secretHate;
					rel.agent.relationships.GetRelationship(agent).mechHate = otherRel.mechHate;
				}

				target.relationships.SetRel(agent, "Hateful");
				target.relationships.SetRelHate(agent, 25);
				agent.agentName = prev;

				item.database.SubtractFromItemCount(item, 1);
				item.invInterface.HideTarget();
			};
			spytron3000.SetTargetText(new CustomNameInfo("Disguise",
				null, null, null, null,
				"Замаскироваться",
				null, null));
			#endregion

			#region Cup of Molten Chocolate
			sprite = RogueUtilities.ConvertToSprite(Properties.Resources.CupOfMoltenChocolate);
			CustomItem cupOfMoltenChocolate = RogueLibs.CreateCustomItem("CupOfMoltenChocolate", sprite, false,
				new CustomNameInfo("Cup of Molten Chocolate",
					null, null, null, null,
					"Кружка расплавленного шоколада",
					null, null),
				new CustomNameInfo("That's a nice drink you're enjoying there... AAAAAAAAAAAAAAAAAAAAH!",
					null, null, null, null,
					"",
					null, null),
				item =>
				{
					item.itemType = "WeaponMelee";
					item.weaponCode = weaponType.WeaponMelee;
					item.Categories.Add("Weapons");
					item.Categories.Add("NotRealWeapons");
					item.isWeapon = true;
					item.itemValue = 80;
					item.initCount = 3;
					item.rewardCount = 3;
					item.stackable = true;
					item.meleeDamage = 1;
					item.hitSoundType = "Normal";
				});
			cupOfMoltenChocolate.Prerequisites.Add("Flamethrower");

			cupOfMoltenChocolate.UnlockCost = 10;
			cupOfMoltenChocolate.CostInCharacterCreation = 2;
			cupOfMoltenChocolate.CostInLoadout = 2;

			patcher.Prefix(typeof(PlayfieldObject), "FindDamage", new Type[] { typeof(PlayfieldObject), typeof(bool), typeof(bool), typeof(bool) });
			#endregion

			#region Portable Ammo Dispenser
			sprite = RogueUtilities.ConvertToSprite(Properties.Resources.PortableAmmoDispenser);
			CustomItem portableAmmoDispenser = RogueLibs.CreateCustomItem("PortableAmmoDispenser", sprite, false,
				new CustomNameInfo("Portable Ammo Dispenser",
				null, null, null, null,
				"Портативный раздатчик боеприпасов",
				null, null),
				new CustomNameInfo("Use it to refill your ranged weapons' ammo. For money, of course.",
				null, null, null, null,
				"Используйте для пополнения запаса патронов у оружия дальнего боя. За деньги, конечно же.",
				null, null),
				item =>
				{
					item.itemType = "Combine";
					item.Categories.Add("Technology");
					item.Categories.Add("GunAccessory");
					item.Categories.Add("Guns");
					item.itemValue = 80;
					item.initCount = 1;
					item.rewardCount = 1;
				});
			portableAmmoDispenser.Prerequisites.Add("KillProfiterAmmo");

			portableAmmoDispenser.UnlockCost = 10;
			portableAmmoDispenser.CostInCharacterCreation = 3;
			portableAmmoDispenser.CostInLoadout = 3;

			portableAmmoDispenser.CombineFilter = (item, agent, otherItem) => otherItem.itemType == "WeaponProjectile" && !otherItem.noRefills;
			portableAmmoDispenser.CombineItems = (item, agent, otherItem) =>
			{
				int amountToRefill = otherItem.maxAmmo - otherItem.invItemCount;
				float singleCost = (float)otherItem.itemValue / otherItem.maxAmmo;
				if (agent.oma.superSpecialAbility && (agent.agentName == "Soldier" || agent.agentName == "Doctor"))
					singleCost = 0f;
				if (otherItem.invItemCount >= otherItem.maxAmmo)
				{
					agent.SayDialogue("AmmoDispenserFull");
					agent.gc.audioHandler.Play(agent, "CantDo");
				}
				else if (agent.inventory.money.invItemCount < amountToRefill * singleCost)
				{
					int affordableAmount = (int)Mathf.Floor(agent.inventory.money.invItemCount / singleCost);

					if (affordableAmount == 0)
					{
						agent.SayDialogue("NeedCash");
						agent.gc.audioHandler.Play(agent, "CantDo");
						return;
					}
					agent.inventory.SubtractFromItemCount(agent.inventory.money, (int)Mathf.Ceil(affordableAmount * singleCost));
					otherItem.invItemCount += affordableAmount;
					agent.SayDialogue("AmmoDispenserFilled");
					agent.gc.audioHandler.Play(agent, "BuyItem");
					new ItemFunctions().UseItemAnim(item, agent);
				}
				else
				{
					agent.inventory.money.invItemCount -= (int)Mathf.Ceil(amountToRefill * singleCost);
					otherItem.invItemCount = otherItem.maxAmmo;
					agent.SayDialogue("AmmoDispenserFilled");
					agent.gc.audioHandler.Play(agent, "BuyItem");
					new ItemFunctions().UseItemAnim(item, agent);
				}
				
			};
			portableAmmoDispenser.CombineTooltip = (item, agent, otherItem) =>
			{
				if (otherItem.invItemName == "PortableAmmoDispenser") return null;

				int amountToRefill = otherItem.maxAmmo - otherItem.invItemCount;
				if (amountToRefill < 1) return null;

				float singleCost = (float)otherItem.itemValue / otherItem.maxAmmo;
				if (agent.oma.superSpecialAbility && (agent.agentName == "Soldier" || agent.agentName == "Doctor"))
					singleCost = 0f;
				int amount = (int)Mathf.Ceil(amountToRefill * singleCost);

				return "$" + amount;
			};
			#endregion

			#region Ammo Box
			sprite = RogueUtilities.ConvertToSprite(Properties.Resources.AmmoBox);
			CustomItem ammoBox = RogueLibs.CreateCustomItem("AmmoBox", sprite, false,
				new CustomNameInfo("Ammo Box",
				null, null, null, null,
				"Ящик с боеприпасами",
				null, null),
				new CustomNameInfo("Combine with any refillable weapon to refill it. Limited ammo.",
				null, null, null, null,
				"Объедините с любым пополняемым оружием для пополнения. Ограниченный запас.",
				null, null),
				item =>
				{
					item.itemType = "Combine";
					item.Categories.Add("Technology");
					item.Categories.Add("GunAccessory");
					item.Categories.Add("Guns");
					item.itemValue = 4;
					item.initCount = 100;
					item.rewardCount = 200;
					item.hasCharges = true;
					item.stackable = true;
				});
			ammoBox.Prerequisites.Add(portableAmmoDispenser.Id);

			ammoBox.UnlockCost = 10;
			ammoBox.CostInCharacterCreation = 3;
			ammoBox.CostInLoadout = 3;

			ammoBox.CombineFilter = (item, agent, otherItem) => otherItem.itemType == "WeaponProjectile" && !otherItem.noRefills;
			ammoBox.CombineItems = (item, agent, otherItem) =>
			{
				int amountToRefill = otherItem.maxAmmo - otherItem.invItemCount;
				float singleCost = (float)otherItem.itemValue / otherItem.maxAmmo;
				if (agent.oma.superSpecialAbility && (agent.agentName == "Soldier" || agent.agentName == "Doctor"))
					singleCost = 0f;
				if (otherItem.invItemCount >= otherItem.maxAmmo)
				{
					agent.SayDialogue("AmmoDispenserFull");
					agent.gc.audioHandler.Play(agent, "CantDo");
				}

				int affordableAmount = (int)Mathf.Ceil(item.invItemCount / singleCost);
				int willBeBought = Mathf.Min(affordableAmount, amountToRefill);
				int willBeReduced = (int)Mathf.Min(item.invItemCount, willBeBought * singleCost);

				agent.inventory.SubtractFromItemCount(item, willBeReduced);
				otherItem.invItemCount += willBeBought;
				agent.SayDialogue("AmmoDispenserFilled");
				agent.gc.audioHandler.Play(agent, "BuyItem");
				new ItemFunctions().UseItemAnim(item, agent);
			};
			ammoBox.CombineTooltip = (item, agent, otherItem) =>
			{
				if (otherItem.invItemName == "AmmoBox") return null;

				int amountToRefill = otherItem.maxAmmo - otherItem.invItemCount;
				if (amountToRefill == 0) return null;

				float singleCost = (float)otherItem.itemValue / otherItem.maxAmmo;
				if (agent.oma.superSpecialAbility && (agent.agentName == "Soldier" || agent.agentName == "Doctor"))
					singleCost = 0f;
				int cost = (int)Mathf.Floor(amountToRefill * singleCost);
				int canAfford = (int)Mathf.Ceil(item.invItemCount / singleCost);

				return "+" + Mathf.Min(amountToRefill, canAfford) + " (" + Mathf.Min(cost, item.invItemCount) + ")";
			};
			#endregion

			#region Joke Book
			sprite = RogueUtilities.ConvertToSprite(Properties.Resources.JokeBook);
			CustomItem jokeBook = RogueLibs.CreateCustomItem("JokeBook", sprite, false,
				new CustomNameInfo("Joke Book",
				null, null, null, null,
				"Сборник шуток",
				null, null),
				new CustomNameInfo("Always wanted to be a Comedian? Now you can! (kind of)",
				null, null, null, null,
				"Всегда хотели быть Комиком? Теперь вы можете! (ну, типа)",
				null, null),
				item =>
				{
					item.itemType = "Tool";
					item.Categories.Add("Usable");
					item.Categories.Add("Social");
					item.itemValue = 40;
					item.initCount = 10;
					item.rewardCount = 10;
					item.stackable = true;
					item.hasCharges = true;
					item.goesInToolbar = true;
				});
			jokeBook.Prerequisites.Add("Comedian_BQ");

			jokeBook.UnlockCost = 10;
			jokeBook.CostInCharacterCreation = 3;
			jokeBook.CostInLoadout = 3;

			jokeBook.UseItem = (item, agent) =>
			{
				string prev = agent.specialAbility;
				agent.specialAbility = "Joke";
				agent.statusEffects.PressedSpecialAbility();
				agent.specialAbility = prev;
				item.database.SubtractFromItemCount(item, 1);
				new ItemFunctions().UseItemAnim(item, agent);
			};
			#endregion

			#region Grindstone
			sprite = RogueUtilities.ConvertToSprite(Properties.Resources.Grindstone);
			CustomItem grindstone = RogueLibs.CreateCustomItem("Grindstone", sprite, false,
				new CustomNameInfo("Grindstone",
				null, null, null, null,
				"Точильный камень",
				null, null),
				new CustomNameInfo("Use on melee weapons to sharpen them. Sharpened weapons will ignore all damage-reducing effects.",
				null, null, null, null,
				"Используйте на оружии ближнего боя для их заточки. Заточенные оружия будут игнорировать все защищающие эффекты.",
				null, null),
				item =>
				{
					item.itemType = "Combine";
					item.Categories.Add("Technology");
					item.Categories.Add("MeleeAccessory");
					item.Categories.Add("Melee");
					item.itemValue = 40;
					item.initCount = 10;
					item.rewardCount = 10;
					item.hasCharges = true;
					item.stackable = true;
				});
			grindstone.Prerequisites.Add("MeleeDurabilityDoubler");

			grindstone.UnlockCost = 10;
			grindstone.CostInCharacterCreation = 5;
			grindstone.CostInLoadout = 5;

			grindstone.CombineFilter = (item, agent, otherItem) => otherItem.itemType == "WeaponMelee" && !otherItem.contents.Exists(c => c.StartsWith("Sharpened:"));
			grindstone.CombineItems = (item, agent, otherItem) =>
			{
				otherItem.contents.Add("Sharpened:3");
				item.database.SubtractFromItemCount(item, 1);
				new ItemFunctions().UseItemAnim(item, agent);
			};

			RogueLibs.CreateCustomName("Sharpened:1", "Item",
				new CustomNameInfo("Sharpened (1)",
				null, null, null, null,
				"Заточенный (1)",
				null, null));
			RogueLibs.CreateCustomName("Sharpened:2", "Item",
				new CustomNameInfo("Sharpened (2)",
				null, null, null, null,
				"Заточенный (2)",
				null, null));
			RogueLibs.CreateCustomName("Sharpened:3", "Item",
				new CustomNameInfo("Sharpened (3)",
				null, null, null, null,
				"Заточенный (3)",
				null, null));
			#endregion

			#region Toolkit
			sprite = RogueUtilities.ConvertToSprite(Properties.Resources.Toolkit);
			CustomItem toolkit = RogueLibs.CreateCustomItem("Toolkit", sprite, false,
				new CustomNameInfo("Toolkit",
				null, null, null, null,
				"Набор инструментов",
				null, null),
				new CustomNameInfo("Combines the effects of Wrench, crowbar, window cutter, lockpick and safe buster. Infinite use.",
				null, null, null, null,
				"Объединяет эффекты Гаечного ключа, Монтировки, Стеклореза, Отмычки и Взрывателя сейфов. Бесконечный.",
				null, null),
				item =>
				{
					item.itemType = "Tool";
					item.Categories.Add("Usable");
					item.Categories.Add("Technology");
					item.Categories.Add("Stealth");
					item.itemValue = 80;
					item.initCount = 1;
					item.rewardCount = 1;
				});
			toolkit.Prerequisites.Add("SkeletonKey");

			toolkit.UnlockCost = 10;
			toolkit.CostInCharacterCreation = 10;
			toolkit.CostInLoadout = 10;
			#endregion








		}

		public static bool PlayfieldObject_FindDamage(PlayfieldObject __instance, PlayfieldObject damagerObject, ref bool generic)
		{
			if (!__instance.isAgent || !damagerObject.isMelee) return true;

			Agent ag = (Agent)__instance;
			Melee me = damagerObject.playfieldObjectMelee;
			if (me.agent != ag && me.invItem.invItemName == "CupOfMoltenChocolate" && !ag.statusEffects.hasStatusEffect("Invincible"))
			{
				me.invItem.database.SubtractFromItemCount(me.invItem, 1);

				ag.knockedOut = ag.knockedOutLocal = true;
				__instance.gc.tileInfo.DirtyWalls();
				ag.lastHitByAgent = ag.justHitByAgent2 = me.agent;
				ag.healthBeforeKnockout = ag.health;

				ag.deathMethodItem = ag.deathMethodObject = ag.deathMethod = me.invItem.invItemName;
				ag.deathKiller = me.agent.agentName;

				ag.statusEffects.ChangeHealth(-200f);
				ag.tranqTime = 0;
				string rel = ag.relationships.GetRel(me.agent);
				if (rel != "Aligned" && rel != "Submissive")
				{
					ag.relationships.SetRel(me.agent, "Hateful");
					ag.dead = false;
					ag.relationships.SetRelHate(me.agent, 5);
					ag.dead = true;
				}

				return false;
			}
			else if (me.invItem.contents.Exists(c => c.StartsWith("Sharpened:")))
			{
				string sharpenedStr = me.invItem.contents.Find(c => c.StartsWith("Sharpened:"));
				string numStr = sharpenedStr.Substring("Sharpened:".Length);
				int num = int.Parse(numStr);
				me.invItem.contents.Remove(sharpenedStr);
				if (--num > 0)
					me.invItem.contents.Add("Sharpened:" + num);
				generic = true;

				ag.relationships.SetRel(me.agent, "Hateful");
				ag.relationships.SetRelHate(me.agent, 50);
			}
			return true;
		}

		public void FixedUpdate()
		{
			VoodooCheck();
			QuantumFudCheck();
		}

		public static Dictionary<InvItem, Agent> VoodooUpdateList { get; set; }
		public static Dictionary<InvItem, float> VoodooCooldowns { get; set; }
		public void VoodooCheck()
		{
			Dictionary<InvItem, float> newDic = new Dictionary<InvItem, float>();
			foreach (KeyValuePair<InvItem, float> pair in VoodooCooldowns)
				newDic.Add(pair.Key, Mathf.Max(pair.Value - Time.fixedDeltaTime, 0f));
			VoodooCooldowns = newDic;

			List<InvItem> removal = new List<InvItem>();
			foreach (KeyValuePair<InvItem, Agent> pair in VoodooUpdateList)
			{
				InvItem item = pair.Key;
				if (pair.Value.dead)
				{
					item.database.DestroyItem(item);
					if (item.invItemCount > 1)
						item.database.AddItem("BlankVoodooDoll", item.invItemCount - 1);

					removal.Add(item);

					item.agent.mainGUI.invInterface.HideDraggedItem();
					item.agent.mainGUI.invInterface.HideTarget();
				}
			}
			foreach (InvItem item in removal)
			{
				VoodooUpdateList.Remove(item);
				VoodooCooldowns.Remove(item);
			}
		}

		public static Dictionary<InvItem, float> QuantumFudCooldowns { get; set; }
		public void QuantumFudCheck()
		{
			Dictionary<InvItem, float> newDic = new Dictionary<InvItem, float>();
			foreach (KeyValuePair<InvItem, float> pair in QuantumFudCooldowns)
				newDic.Add(pair.Key, Mathf.Max(pair.Value - Time.fixedDeltaTime, 0f));
			QuantumFudCooldowns = newDic;
		}
	}
}
