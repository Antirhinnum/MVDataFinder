using MVDeserializer;
using MVDeserializer.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace MVDataFinder
{
	internal enum Source
	{
		CommonEvent,
		Enemy,
		Item,
		MapEvent,
		Skill,
		Troop
	}

	internal enum IOType
	{
		Read,
		Write,
		Both,
		Unknown
	}

	internal interface IInfo
	{
		string OutputInfo();
	}

	internal struct SwitchVarLocationInfo
	{
		public Source Source { get; set; }
		public IOType IOType { get; set; }
		public IInfo Info { get; set; }

		public SwitchVarLocationInfo(Source source, IOType type, IInfo info)
		{
			Source = source;
			IOType = type;
			Info = info;
		}
	}

	internal struct CommonEventLocationInfo
	{
		public Source Source { get; set; }
		public IInfo Info { get; set; }

		public CommonEventLocationInfo(Source source, IInfo info)
		{
			Source = source;
			Info = info;
		}
	}

	internal struct CommonEventInfo : IInfo
	{
		internal int Id { get; set; }
		internal CommonEventSource Source { get; set; }

		internal CommonEventInfo(int id, CommonEventSource source)
		{
			Id = id;
			Source = source;
		}

		internal enum CommonEventSource
		{
			Trigger,
			Body
		}

		public string OutputInfo()
		{
			return $"Common Event {Id} ('{Program.Data.CommonEvents[Id].Name}'), found in {Source}";
		}
	}

	internal struct CommonEventAutorunInfo : IInfo
	{
		internal int SwitchId { get; set; }
		internal Trigger AutorunType { get; set; }

		internal CommonEventAutorunInfo(int switchId, Trigger type)
		{
			SwitchId = switchId;
			AutorunType = type;
		}

		public string OutputInfo()
		{
			return $"Runs when Switch {SwitchId} ('{Program.Data.System.Switches[SwitchId]}') is ON. Type: {AutorunType}";
		}
	}

	internal struct MapEventInfo : IInfo
	{
		internal int MapId { get; set; }
		internal int EventId { get; set; }
		internal int PageIndex { get; set; }
		internal MapEventSource Source { get; set; }

		internal MapEventInfo(int mapId, int eventId, MapEventSource source, int pageIndex)
		{
			MapId = mapId;
			EventId = eventId;
			Source = source;
			PageIndex = pageIndex;
		}

		internal enum MapEventSource
		{
			Conditions,
			MovementRoute,
			Body
		}

		public string OutputInfo()
		{
			MapEvent mapEvent = Program.Data.Maps[MapId].Events[EventId];
			return $"Map {MapId} ('{Program.Data.MapInfos[MapId]?.Name ?? "No Info"}'), Event {EventId} ('{mapEvent.Name}') at ({mapEvent.X}, {mapEvent.Y}). Found in {Source} on Page {PageIndex + 1}";
		}
	}

	internal struct EnemyInfo : IInfo
	{
		internal int EnemyId { get; set; }
		internal int ActionIndex { get; set; }

		public EnemyInfo(int enemyId, int actionIndex)
		{
			EnemyId = enemyId;
			ActionIndex = actionIndex;
		}

		public string OutputInfo()
		{
			return $"Enemy {EnemyId} ('{Program.Data.Enemies[EnemyId].Name}'), Action {ActionIndex + 1} Condition";
		}
	}

	internal struct SkillInfo : IInfo
	{
		internal int SkillId { get; set; }

		internal SkillInfo(int skillId)
		{
			SkillId = skillId;
		}

		public string OutputInfo()
		{
			return $"Skill {SkillId} ('{Program.Data.Skills[SkillId].Name}'). Found in Effects.";
		}
	}

	internal struct TroopInfo : IInfo
	{
		internal int TroopId { get; set; }
		internal int PageIndex { get; set; }
		internal TroopSource Source { get; set; }

		internal TroopInfo(int troopId, TroopSource source, int pageIndex)
		{
			TroopId = troopId;
			Source = source;
			PageIndex = pageIndex;
		}

		internal enum TroopSource
		{
			Conditions,
			Body
		}

		public string OutputInfo()
		{
			return $"Troop {TroopId} ('{Program.Data.Troops[TroopId].Name}'). Found in {Source} on Page {PageIndex + 1}";
		}
	}

	internal struct ItemInfo : IInfo
	{
		internal int ItemId { get; set; }

		internal ItemInfo(int itemId)
		{
			ItemId = itemId;
		}

		public string OutputInfo()
		{
			return $"Item {ItemId} ('{Program.Data.Items[ItemId].Name}'). Found in Effects.";
		}
	}

	internal class Program
	{
		private static readonly Regex GameSwitchesValueRegex = new Regex(@"\$gameSwitches.value\(([0-9]*)\)");
		private static readonly Regex GameSwitchesSetValue = new Regex(@"\$gameSwitches.setValue\(([0-9]*)\)");
		private static readonly Regex GameTempReserveCommonEvent = new Regex(@"\$gametemp.reserveCommonEvent\(([0-9]*)\)");
		internal static MVData Data { get; set; }
		private static Dictionary<int, List<SwitchVarLocationInfo>> TrackedSwitchIds { get; } = new Dictionary<int, List<SwitchVarLocationInfo>>();
		private static Dictionary<int, List<SwitchVarLocationInfo>> TrackedVariableIds { get; } = new Dictionary<int, List<SwitchVarLocationInfo>>();
		private static Dictionary<int, List<CommonEventLocationInfo>> TrackedCommonEventIds { get; } = new Dictionary<int, List<CommonEventLocationInfo>>();
		private static bool ShouldSearchForSwitches { get; set; }
		private static bool ShouldSearchForVariables { get; set; }
		private static bool ShouldSearchForCommonEvents { get; set; }

		internal static void Main(string[] args)
		{
			#region Get Project Folder

			Console.WriteLine("RPG Maker MV Switch/Variable Finder");
			Console.WriteLine("Made by Antirhinnum / Snek");
			string gameFolderPath;

			if (args.Length > 0)
			{
				gameFolderPath = args[0];
			}
			else
			{
				Console.WriteLine("Please input the RPG Maker project's file path:");
				gameFolderPath = Console.ReadLine();
			}

			if (!Directory.Exists(gameFolderPath))
			{
				Console.WriteLine("That directory doesn't exist! Press any key to close program.");
				Console.ReadLine();
				Console.Clear();
			}

			if (!File.Exists(Path.Combine(gameFolderPath, "Game.rpgproject")))
			{
				Console.WriteLine("This doesn't look like an RPG Maker MV Project! Press any key to close program.");
				Console.ReadLine();
				Console.Clear();
			}

			#endregion Get Project Folder














			Data = null;
			TrackedSwitchIds.Clear();
			TrackedVariableIds.Clear();
			TrackedCommonEventIds.Clear();

			

			Console.WriteLine("Deserializing the RPG Maker MV Project...");
			Data = MVData.DeserializeFromPath(gameFolderPath);
			AddTrackedData();
			SearchForData();
			DisplayResults();

			Console.WriteLine("Search complete. Press any key to close program.");
			Console.ReadLine();
		}

		private static void AddTrackedData()
		{
			#region Add Switches

			Console.WriteLine("Please input the IDs of the switches you wish to find.");
			Console.WriteLine("Type nothing if you want to stop, type '-1' to find all switches.");
			int numSwitches = Data.System.Switches.Count - 1;
			while (true)
			{
				string input = Console.ReadLine();
				if (int.TryParse(input, out int id))
				{
					if (id > 0 && id <= numSwitches)
					{
						if (TrackedSwitchIds.TryAdd(id, new List<SwitchVarLocationInfo>()))
						{
							Console.WriteLine($"Added Switch {id}, named \"{Data.System.Switches[id]}\"");
						}
						else
						{
							Console.WriteLine($"Switch {id} has already been added!");
						}
					}
					else if (id == -1)
					{
						for (int i = 1; i <= numSwitches; i++)
						{
							TrackedSwitchIds.TryAdd(i, new List<SwitchVarLocationInfo>());
						}
						Console.WriteLine("Added all switches.");
						break;
					}
					else
					{
						Console.WriteLine($"Switch {id} is out of bounds! Switches start at 1 and end at {numSwitches}");
					}
				}
				else if (input == string.Empty)
				{
					break;
				}
				else
				{
					Console.WriteLine("Switch IDs should be integers!");
				}
			}

			ShouldSearchForSwitches = TrackedSwitchIds.Count > 0;

			#endregion Add Switches

			#region Add Variables

			Console.WriteLine("Please input the IDs of the variables you wish to find.");
			Console.WriteLine("Type nothing if you want to stop, type '-1' to find all variables.");
			int numVariables = Data.System.Variables.Count - 1;
			while (true)
			{
				string input = Console.ReadLine();
				if (int.TryParse(input, out int id))
				{
					if (id > 0 && id <= numVariables)
					{
						if (TrackedVariableIds.TryAdd(id, new List<SwitchVarLocationInfo>()))
						{
							Console.WriteLine($"Added Variable {id}, named \"{Data.System.Variables[id]}\"");
						}
						else
						{
							Console.WriteLine($"Variable {id} has already been added!");
						}
					}
					else if (id == -1)
					{
						for (int i = 1; i <= numVariables; i++)
						{
							TrackedVariableIds.TryAdd(i, new List<SwitchVarLocationInfo>());
						}
						Console.WriteLine("Added all variables.");
						break;
					}
					else
					{
						Console.WriteLine($"Variable {id} is out of bounds! Variables start at 1 and end at {numVariables}");
					}
				}
				else if (input == string.Empty)
				{
					break;
				}
				else
				{
					Console.WriteLine("Variable IDs should be integers!");
				}
			}

			ShouldSearchForVariables = TrackedVariableIds.Count > 0;

			#endregion Add Variables

			#region Add Common Events

			Console.WriteLine("Please input the IDs of the Common Events you wish to find.");
			Console.WriteLine("Type nothing if you want to stop, type '-1' to find all Common Events.");
			int numEvents = Data.CommonEvents.Count - 1;
			while (true)
			{
				string input = Console.ReadLine();
				if (int.TryParse(input, out int id))
				{
					if (id > 0 && id <= numEvents)
					{
						if (TrackedCommonEventIds.TryAdd(id, new List<CommonEventLocationInfo>()))
						{
							Console.WriteLine($"Added Common Event {id}, named \"{Data.CommonEvents[id].Name}\"");
						}
						else
						{
							Console.WriteLine($"Common Event {id} has already been added!");
						}
					}
					else if (id == -1)
					{
						for (int i = 1; i <= numEvents; i++)
						{
							TrackedCommonEventIds.TryAdd(i, new List<CommonEventLocationInfo>());
						}
						Console.WriteLine("Added all Common Events.");
						break;
					}
					else
					{
						Console.WriteLine($"Common Event {id} is out of bounds! Common Events start at 1 and end at {numEvents}");
					}
				}
				else if (input == string.Empty)
				{
					break;
				}
				else
				{
					Console.WriteLine("Common Event IDs should be integers!");
				}
			}

			ShouldSearchForCommonEvents = TrackedCommonEventIds.Count > 0;

			#endregion Add Common Events
		}

		private static void SearchForData()
		{




			return;

			foreach (CommonEvent cEvent in Data.CommonEvents)
			{
				if (cEvent == null) continue;

				if (cEvent.Trigger != Trigger.None)
				{
					TryAddSwitchInfo(cEvent.SwitchId, Source.CommonEvent, IOType.Read, new CommonEventInfo(cEvent.Id, CommonEventInfo.CommonEventSource.Trigger));
					TryAddCommonEventInfo(cEvent.Id, Source.CommonEvent, new CommonEventAutorunInfo(cEvent.SwitchId, cEvent.Trigger));
				}

				SearchEventCommands(cEvent.Commands, Source.CommonEvent, new CommonEventInfo(cEvent.Id, CommonEventInfo.CommonEventSource.Body));
			}

			foreach (Map map in Data.Maps)
			{
				if (map == null) continue;

				foreach (MapEvent mapEvent in map.Events)
				{
					if (mapEvent == null) continue;

					for (int i = 0; i < mapEvent.Pages.Count; i++)
					{
						MapPage page = mapEvent.Pages[i];

						if (ShouldSearchForSwitches)
						{
							if (page.Conditions.Switch1Valid)
							{
								TryAddSwitchInfo(page.Conditions.Switch1Id, Source.MapEvent, IOType.Read, new MapEventInfo(map.Id, mapEvent.Id, MapEventInfo.MapEventSource.Conditions, i));
							}
							if (page.Conditions.Switch2Valid)
							{
								TryAddSwitchInfo(page.Conditions.Switch2Id, Source.MapEvent, IOType.Read, new MapEventInfo(map.Id, mapEvent.Id, MapEventInfo.MapEventSource.Conditions, i));
							}
						}
						if (ShouldSearchForVariables)
						{
							if (page.Conditions.VariableValid)
							{
								TryAddVariableInfo(page.Conditions.VariableId, Source.MapEvent, IOType.Read, new MapEventInfo(map.Id, mapEvent.Id, MapEventInfo.MapEventSource.Conditions, i));
							}
						}

						SearchMovementRoute(page.MovementRoute, Source.MapEvent, new MapEventInfo(map.Id, mapEvent.Id, MapEventInfo.MapEventSource.MovementRoute, i));
						SearchEventCommands(page.Contents, Source.MapEvent, new MapEventInfo(map.Id, mapEvent.Id, MapEventInfo.MapEventSource.Body, i));
					}
				}
			}

			foreach (Troop troop in Data.Troops)
			{
				if (troop == null) continue;

				for (int i = 0; i < troop.Pages.Count; i++)
				{
					TroopPage page = troop.Pages[i];

					if (ShouldSearchForSwitches)
					{
						if (page.Conditions.SwitchValid)
						{
							TryAddSwitchInfo(page.Conditions.SwitchId, Source.Troop, IOType.Read, new TroopInfo(troop.Id, TroopInfo.TroopSource.Conditions, i));
						}
					}

					SearchEventCommands(page.Commands, Source.Troop, new TroopInfo(troop.Id, TroopInfo.TroopSource.Body, i));
				}
			}

			if (ShouldSearchForSwitches)
			{
				foreach (Enemy enemy in Data.Enemies)
				{
					if (enemy == null) continue;

					for (int i = 0; i < enemy.ActionPatterns.Count; i++)
					{
						EnemyAction action = enemy.ActionPatterns[i];
						if (action.ConditionType == ConditionType.Switch)
						{
							TryAddSwitchInfo(action.ConditionParam1, Source.Enemy, IOType.Read, new EnemyInfo(enemy.Id, i));
						}
					}
				}
			}

			if (ShouldSearchForCommonEvents)
			{
				foreach (Item item in Data.Items)
				{
					if (item == null) continue;

					foreach (Effect effect in item.Effects)
					{
						if (effect.Code == EffectCode.CommonEvent)
						{
							TryAddCommonEventInfo(effect.DataId, Source.Skill, new ItemInfo(item.Id));
						}
					}
				}

				foreach (Skill skill in Data.Skills)
				{
					if (skill == null) continue;

					foreach (Effect effect in skill.Effects)
					{
						if (effect.Code == EffectCode.CommonEvent)
						{
							TryAddCommonEventInfo(effect.DataId, Source.Skill, new SkillInfo(skill.Id));
						}
					}
				}
			}

		}

		private static void DisplayResults()
		{
			if (ShouldSearchForSwitches)
			{
				Console.WriteLine("Switches found:");
				foreach (KeyValuePair<int, List<SwitchVarLocationInfo>> pair in TrackedSwitchIds)
				{
					if (pair.Value.Count == 0)
					{
						Console.WriteLine($"	Did not find any instances of Switch {pair.Key}  ('{Data.System.Switches[pair.Key]}')!");
						continue;
					}

					Console.WriteLine($"	Switch {pair.Key} ('{Data.System.Switches[pair.Key]}'):");

					foreach (SwitchVarLocationInfo info in pair.Value)
					{
						Console.WriteLine($"		[{(info.IOType == IOType.Read ? "R" : "W")}] {info.Info.OutputInfo()}");
					}
				}
			}

			if (ShouldSearchForVariables)
			{
				Console.WriteLine("Variables found:");
				foreach (KeyValuePair<int, List<SwitchVarLocationInfo>> pair in TrackedVariableIds)
				{
					if (pair.Value.Count == 0)
					{
						Console.WriteLine($"	Did not find any instances of Variable {pair.Key} ('{Data.System.Variables[pair.Key]}')!");
						continue;
					}

					Console.WriteLine($"	Variable {pair.Key} ('{Data.System.Variables[pair.Key]}'):");

					foreach (SwitchVarLocationInfo info in pair.Value)
					{
						Console.WriteLine($"		[{(info.IOType == IOType.Read ? "R" : "W")}] {info.Info.OutputInfo()}");
					}
				}
			}

			if (ShouldSearchForCommonEvents)
			{
				Console.WriteLine("Common Events found:");
				foreach (KeyValuePair<int, List<CommonEventLocationInfo>> pair in TrackedCommonEventIds)
				{
					if (pair.Value.Count == 0)
					{
						Console.WriteLine($"	Did not find any instances of Common Event {pair.Key} ('{Data.CommonEvents[pair.Key].Name}')!");
						continue;
					}

					Console.WriteLine($"	Common Event {pair.Key} ('{Data.CommonEvents[pair.Key].Name}'):");

					foreach (CommonEventLocationInfo info in pair.Value)
					{
						Console.WriteLine($"		{info.Info.OutputInfo()}");
					}
				}
			}
		}

		private static void TryAddSwitchInfo(int switchId, Source source, IOType ioType, IInfo info)
		{
			if (!ShouldSearchForSwitches)
			{
				return;
			}

			if (switchId < 1 || switchId > Data.System.Switches.Count)
			{
				return;
			}

			if (!TrackedSwitchIds.ContainsKey(switchId))
			{
				return;
			}

			TrackedSwitchIds[switchId].Add(new SwitchVarLocationInfo(source, ioType, info));
		}

		private static void TryAddVariableInfo(int variableId, Source source, IOType ioType, IInfo info)
		{
			if (!ShouldSearchForVariables)
			{
				return;
			}

			if (variableId < 1 || variableId > Data.System.Variables.Count)
			{
				return;
			}

			if (!TrackedVariableIds.ContainsKey(variableId))
			{
				return;
			}

			TrackedVariableIds[variableId].Add(new SwitchVarLocationInfo(source, ioType, info));
		}

		private static void TryAddCommonEventInfo(int commonEventId, Source source, IInfo info)
		{
			if (!ShouldSearchForCommonEvents)
			{
				return;
			}

			if (commonEventId < 1 || commonEventId > Data.CommonEvents.Count)
			{
				return;
			}

			if (!TrackedCommonEventIds.ContainsKey(commonEventId))
			{
				return;
			}

			TrackedCommonEventIds[commonEventId].Add(new CommonEventLocationInfo(source, info));
		}

		private static void SearchEventCommands(in IList<EventCommand> commands, in Source source, in IInfo info)
		{
			foreach (EventCommand command in commands)
			{
				IList<object> parameters = command.Parameters;
				switch (command.Code)
				{
					case EventCode.InputNumber:
					case EventCode.SelectItem:
						{
							if (!ShouldSearchForVariables) break;
							int variableId = Convert.ToInt32(parameters[0]);
							TryAddVariableInfo(variableId, source, IOType.Write, info);
							break;
						}

					case EventCode.ControlSwitches:
						{
							if (!ShouldSearchForSwitches) break;
							int startId = Convert.ToInt32(parameters[0]);
							int endId = Convert.ToInt32(parameters[1]);
							for (int i = startId; i <= endId; i++)
							{
								TryAddSwitchInfo(i, source, IOType.Write, info);
							}
							break;
						}

					case EventCode.ControlVariables:
						{
							if (!ShouldSearchForVariables) break;
							int startId = Convert.ToInt32(parameters[0]);
							int endId = Convert.ToInt32(parameters[1]);
							for (int i = startId; i <= endId; i++)
							{
								TryAddVariableInfo(i, source, IOType.Write, info);
							}

							int operand = Convert.ToInt32(parameters[3]);
							if (operand == 1) // Set var(s) to var
							{
								TryAddVariableInfo(Convert.ToInt32(parameters[4]), source, IOType.Read, info);
							}
							else if (operand == 4) // Script
							{
								SearchJS(Convert.ToString(parameters[4]), source, info);
							}
							break;
						}

					case EventCode.ConditionalBranch:
						{
							int checkedValueType = Convert.ToInt32(parameters[0]);
							if (checkedValueType == 0 && ShouldSearchForSwitches) // Switch
							{
								int checkedSwitchId = Convert.ToInt32(parameters[1]);
								TryAddSwitchInfo(checkedSwitchId, source, IOType.Read, info);
							}
							else if (checkedValueType == 1 && ShouldSearchForVariables) // Variable
							{
								int checkedVariable1 = Convert.ToInt32(parameters[1]);
								TryAddVariableInfo(checkedVariable1, source, IOType.Read, info);

								if (Convert.ToInt32(parameters[2]) != 0) // If not 0, then the variable is checked against another variable
								{
									int checkedVariable2 = Convert.ToInt32(parameters[3]);
									TryAddVariableInfo(checkedVariable2, source, IOType.Read, info);
								}
							}
							else if (checkedValueType == 12) // Script
							{
								string js = Convert.ToString(parameters[1]);
								SearchJS(js, source, info);
							}
							break;
						}

					case EventCode.SetMovementRoute:
						MovementRoute route = JsonConvert.DeserializeObject<MovementRoute>(((JObject)parameters[1]).ToString());
						SearchMovementRoute(route, source, info);
						break;

					case EventCode.ChangeGold:
						if (!ShouldSearchForVariables) break;
						if (Convert.ToInt32(parameters[1]) == 0) break; // Constant
						TryAddVariableInfo(Convert.ToInt32(parameters[2]), source, IOType.Read, info);
						break;

					case EventCode.ChangeItems:
					case EventCode.ChangeWeapons:
					case EventCode.ChangeArmors:
						if (!ShouldSearchForVariables) break;
						if (Convert.ToInt32(parameters[2]) == 0) break; // Constant
						TryAddVariableInfo(Convert.ToInt32(parameters[3]), source, IOType.Read, info);
						break;

					case EventCode.ChangeHP:
					case EventCode.ChangeMP:
					case EventCode.ChangeTp:
					case EventCode.ChangeEXP:
					case EventCode.ChangeLevel:
						if (!ShouldSearchForVariables) break;
						if (Convert.ToInt32(parameters[3]) != 0) // Variable - Could use break; like others, but I wanted to do the second check after. Sue me.
						{
							TryAddVariableInfo(Convert.ToInt32(parameters[4]), source, IOType.Read, info);
						}
						if (Convert.ToInt32(parameters[0]) != 0)
						{
							TryAddVariableInfo(Convert.ToInt32(parameters[1]), source, IOType.Read, info);
						}
						break;

					case EventCode.ChangeState:
					case EventCode.RecoverAll:
					case EventCode.ChangeSkill:
						if (!ShouldSearchForVariables) break;
						if (Convert.ToInt32(parameters[0]) == 0) break;
						TryAddVariableInfo(Convert.ToInt32(parameters[1]), source, IOType.Read, info);
						break;

					case EventCode.ChangeParameter:
						if (!ShouldSearchForVariables) break;
						if (Convert.ToInt32(parameters[4]) != 0) // Variable - Could use break; like others, but I wanted to do the second check after. Sue me.
						{
							TryAddVariableInfo(Convert.ToInt32(parameters[5]), source, IOType.Read, info);
						}
						if (Convert.ToInt32(parameters[0]) != 0)
						{
							TryAddVariableInfo(Convert.ToInt32(parameters[1]), source, IOType.Read, info);
						}
						break;

					case EventCode.TransferPlayer:
						if (!ShouldSearchForVariables) break;
						if (Convert.ToInt32(parameters[0]) == 0) break;
						TryAddVariableInfo(Convert.ToInt32(parameters[1]), source, IOType.Read, info);
						TryAddVariableInfo(Convert.ToInt32(parameters[2]), source, IOType.Read, info);
						TryAddVariableInfo(Convert.ToInt32(parameters[3]), source, IOType.Read, info);
						break;

					case EventCode.SetVehicleLocation:
						if (!ShouldSearchForVariables) break;
						if (Convert.ToInt32(parameters[1]) == 0) break;
						TryAddVariableInfo(Convert.ToInt32(parameters[2]), source, IOType.Read, info);
						TryAddVariableInfo(Convert.ToInt32(parameters[3]), source, IOType.Read, info);
						TryAddVariableInfo(Convert.ToInt32(parameters[4]), source, IOType.Read, info);
						break;

					case EventCode.SetEventLocation:
						if (!ShouldSearchForVariables) break;
						if (Convert.ToInt32(parameters[1]) != 1) break;
						TryAddVariableInfo(Convert.ToInt32(parameters[2]), source, IOType.Read, info);
						TryAddVariableInfo(Convert.ToInt32(parameters[3]), source, IOType.Read, info);
						break;

					case EventCode.ShowPicture:
					case EventCode.MovePicture:
						if (!ShouldSearchForVariables) break;
						if (Convert.ToInt32(parameters[3]) == 0) break;
						TryAddVariableInfo(Convert.ToInt32(parameters[4]), source, IOType.Read, info);
						TryAddVariableInfo(Convert.ToInt32(parameters[5]), source, IOType.Read, info);
						break;

					case EventCode.BattleProcessing:
						if (!ShouldSearchForVariables) break;
						if (Convert.ToInt32(parameters[0]) != 1) break;
						TryAddVariableInfo(Convert.ToInt32(parameters[1]), source, IOType.Read, info);
						break;

					case EventCode.GetLocationInfo:
						if (!ShouldSearchForVariables) break;
						if (Convert.ToInt32(parameters[2]) != 0)
						{
							TryAddVariableInfo(Convert.ToInt32(parameters[3]), source, IOType.Read, info);
							TryAddVariableInfo(Convert.ToInt32(parameters[4]), source, IOType.Read, info);
						}
						TryAddVariableInfo(Convert.ToInt32(parameters[0]), source, IOType.Write, info);
						break;

					case EventCode.ChangeEnemyHP:
					case EventCode.ChangeEnemyMP:
					case EventCode.ChangeEnemyTP:
						if (!ShouldSearchForVariables) break;
						if (Convert.ToInt32(parameters[0]) != 0)
						{
							TryAddVariableInfo(Convert.ToInt32(parameters[3]), source, IOType.Read, info);
						}
						break;

					case EventCode.CommonEvent:
						if (!ShouldSearchForCommonEvents) break;
						TryAddCommonEventInfo(Convert.ToInt32(parameters[0]), source, info);
						break;

					case EventCode.Script:
						SearchJS(Convert.ToString(parameters[0]), source, info);
						break;
				}
			}
		}

		private static void SearchMovementRoute(in MovementRoute route, in Source source, in IInfo info)
		{
			foreach (MoveEventCommand command in route.MovementCommands)
			{
				IList<object> parameters = command.Parameters;
				switch (command.Code)
				{
					case MoveEventCode.SwitchOn:
					case MoveEventCode.SwitchOff:
						TryAddSwitchInfo(Convert.ToInt32(parameters[0]), source, IOType.Write, info);
						break;

					case MoveEventCode.Script:
						SearchJS(Convert.ToString(parameters[0]), source, info);
						break;
				}
			}
		}

		private static void SearchJS(string js, Source source, IInfo info)
		{
			if (ShouldSearchForSwitches)
			{
				HashSet<int> switchesFound = new HashSet<int>();
				MatchCollection matches = GameSwitchesValueRegex.Matches(js);
				foreach (Match match in matches)
				{
					if (!match.Success) continue;
					switchesFound.Add(Convert.ToInt32(match.Groups[1].Value));
				}

				foreach (int switchId in switchesFound)
				{
					TryAddSwitchInfo(switchId, source, IOType.Unknown, info);
				}
			}

			if (ShouldSearchForCommonEvents)
			{
				HashSet<int> eventsFound = new HashSet<int>();
				MatchCollection matches = GameTempReserveCommonEvent.Matches(js);
				foreach (Match match in matches)
				{
					if (!match.Success) continue;
					eventsFound.Add(Convert.ToInt32(match.Groups[1].Value));
				}

				foreach (int commonEventId in eventsFound)
				{
					TryAddCommonEventInfo(commonEventId, source, info);
				}
			}
		}
	}
}