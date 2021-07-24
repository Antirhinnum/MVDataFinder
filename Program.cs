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
		MapEvent,
		CommonEvent,
		Troop,
		Enemy
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

	internal struct SwitchVarInfo
	{
		public Source Source { get; set; }
		public IOType IOType { get; set; }
		public IInfo Info { get; set; }

		public SwitchVarInfo(Source source, IOType type, IInfo info)
		{
			Source = source;
			IOType = type;
			Info = info;
		}
	}

	internal struct CommonEventInfo : IInfo
	{
		internal int ID { get; set; }
		internal CommonEventSource Source { get; set; }

		internal CommonEventInfo(int id, CommonEventSource source)
		{
			ID = id;
			Source = source;
		}

		internal enum CommonEventSource
		{
			Trigger,
			Body
		}

		public string OutputInfo()
		{
			return $"Common Event {ID} ('{Program.Data.CommonEvents[ID].Name}'), found in {Source}";
		}
	}

	internal struct MapEventInfo : IInfo
	{
		internal int MapID { get; set; }
		internal int EventID { get; set; }
		internal int PageIndex { get; set; }
		internal MapEventSource Source { get; set; }

		internal MapEventInfo(int mapID, int eventID, MapEventSource source, int pageIndex)
		{
			MapID = mapID;
			EventID = eventID;
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
			MapEvent mapEvent = Program.Data.Maps[MapID].Events[EventID];
			return $"Map {MapID} ('{Program.Data.MapInfos[MapID]?.Name ?? "No Info"}'), Event {EventID} ('{mapEvent.Name}') at ({mapEvent.X}, {mapEvent.Y}). Found in {Source} on Page {PageIndex + 1}";
		}
	}

	internal struct EnemyInfo : IInfo
	{
		internal int EnemyID { get; set; }
		internal int ActionIndex { get; set; }

		public EnemyInfo(int enemyID, int actionIndex)
		{
			EnemyID = enemyID;
			ActionIndex = actionIndex;
		}

		public string OutputInfo()
		{
			return $"Enemy {EnemyID} ('{Program.Data.Enemies[EnemyID].Name}'), Action {ActionIndex + 1} Condition";
		}
	}

	internal struct TroopInfo : IInfo
	{
		internal int TroopID { get; set; }
		internal int PageIndex { get; set; }
		internal TroopSource Source { get; set; }

		internal TroopInfo(int troopID, TroopSource source, int pageIndex)
		{
			TroopID = troopID;
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
			return $"Troop {TroopID} ('{Program.Data.Troops[TroopID].Name}'). Found in {Source} on Page {PageIndex + 1}";
		}
	}

	internal class Program
	{
		private static readonly Regex GameSwitchesValueRegex = new Regex(@"\$gameSwitches.value\(([0-9]*)\)");
		private static readonly Regex GameSwitchesSetValue = new Regex(@"\$gameSwitches.setValue\(([0-9]*)\)");
		internal static MVData Data { get; set; }
		private static Dictionary<int, List<SwitchVarInfo>> TrackedSwitchIDs { get; } = new Dictionary<int, List<SwitchVarInfo>>();
		private static Dictionary<int, List<SwitchVarInfo>> TrackedVariableIDs { get; } = new Dictionary<int, List<SwitchVarInfo>>();
		private static bool ShouldSearchForSwitches { get; set; }
		private static bool ShouldSearchForVariables { get; set; }

		internal static void Main(string[] args)
		{
			Data = null;
			TrackedSwitchIDs.Clear();
			TrackedVariableIDs.Clear();

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

			Console.WriteLine("Deserializing the RPG Maker MV Project...");
			Data = MVData.DeserializeFromPath(gameFolderPath);
			AddSwitchesAndVariables();
			SearchForData();
			DisplayResults();

			Console.WriteLine("Search complete. Press any key to close program.");
			Console.ReadLine();
		}

		private static void AddSwitchesAndVariables()
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
						if (TrackedSwitchIDs.TryAdd(id, new List<SwitchVarInfo>()))
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
							TrackedSwitchIDs.TryAdd(i, new List<SwitchVarInfo>());
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

			ShouldSearchForSwitches = TrackedSwitchIDs.Count > 0;

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
						if (TrackedVariableIDs.TryAdd(id, new List<SwitchVarInfo>()))
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
							TrackedVariableIDs.TryAdd(i, new List<SwitchVarInfo>());
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

			ShouldSearchForVariables = TrackedVariableIDs.Count > 0;

			#endregion Add Variables
		}

		private static void SearchForData()
		{
			foreach (CommonEvent cEvent in Data.CommonEvents)
			{
				if (cEvent == null) continue;

				if (cEvent.Trigger != Trigger.None)
				{
					TryAddSwitchInfo(cEvent.SwitchID.ID, Source.CommonEvent, IOType.Read, new CommonEventInfo(cEvent.ID.ID, CommonEventInfo.CommonEventSource.Trigger));
				}

				SearchEventCommands(cEvent.Commands, Source.CommonEvent, new CommonEventInfo(cEvent.ID.ID, CommonEventInfo.CommonEventSource.Body));
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
								TryAddSwitchInfo(page.Conditions.Switch1ID.ID, Source.MapEvent, IOType.Read, new MapEventInfo(map.ID.ID, mapEvent.ID.ID, MapEventInfo.MapEventSource.Conditions, i));
							}
							if (page.Conditions.Switch2Valid)
							{
								TryAddSwitchInfo(page.Conditions.Switch2ID.ID, Source.MapEvent, IOType.Read, new MapEventInfo(map.ID.ID, mapEvent.ID.ID, MapEventInfo.MapEventSource.Conditions, i));
							}
						}
						if (ShouldSearchForVariables)
						{
							if (page.Conditions.VariableValid)
							{
								TryAddVariableInfo(page.Conditions.VariableID.ID, Source.MapEvent, IOType.Read, new MapEventInfo(map.ID.ID, mapEvent.ID.ID, MapEventInfo.MapEventSource.Conditions, i));
							}
						}

						SearchMovementRoute(page.MovementRoute, Source.MapEvent, new MapEventInfo(map.ID.ID, mapEvent.ID.ID, MapEventInfo.MapEventSource.MovementRoute, i));
						SearchEventCommands(page.Commands, Source.MapEvent, new MapEventInfo(map.ID.ID, mapEvent.ID.ID, MapEventInfo.MapEventSource.Body, i));
					}
				}
			}

			if (ShouldSearchForSwitches)
			{
				foreach (Enemy enemy in Data.Enemies)
				{
					if (enemy == null) continue;

					for (int i = 0; i < enemy.Actions.Count; i++)
					{
						EnemyAction action = enemy.Actions[i];
						if (action.ConditionType == ConditionType.Switch)
						{
							TryAddSwitchInfo(action.ConditionParam1, Source.Enemy, IOType.Read, new EnemyInfo(enemy.ID.ID, i));
						}
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
							TryAddSwitchInfo(page.Conditions.SwitchID.ID, Source.Troop, IOType.Read, new TroopInfo(troop.ID.ID, TroopInfo.TroopSource.Conditions, i));
						}
					}

					SearchEventCommands(page.Commands, Source.Troop, new TroopInfo(troop.ID.ID, TroopInfo.TroopSource.Body, i));
				}
			}

		}

		private static void DisplayResults()
		{
			if (ShouldSearchForSwitches)
			{
				Console.WriteLine("Switches found:");
				foreach (KeyValuePair<int, List<SwitchVarInfo>> pair in TrackedSwitchIDs)
				{
					if (pair.Value.Count == 0)
					{
						Console.WriteLine($"	Did not find any instances of Switch {pair.Key}  ('{Data.System.Switches[pair.Key]}')!");
						continue;
					}

					Console.WriteLine($"	Switch {pair.Key} ('{Data.System.Switches[pair.Key]}'):");

					foreach (SwitchVarInfo info in pair.Value)
					{
						Console.WriteLine($"		{info.Info.OutputInfo()}");
					}
				}
			}

			if (ShouldSearchForVariables)
			{
				Console.WriteLine("Variables found:");
				foreach (KeyValuePair<int, List<SwitchVarInfo>> pair in TrackedVariableIDs)
				{
					if (pair.Value.Count == 0)
					{
						Console.WriteLine($"	Did not find any instances of Variable {pair.Key} ('{Data.System.Variables[pair.Key]}')!");
						continue;
					}

					Console.WriteLine($"	Variable {pair.Key} ('{Data.System.Variables[pair.Key]}'):");

					foreach (SwitchVarInfo info in pair.Value)
					{
						Console.WriteLine($"		{info.Info.OutputInfo()}");
					}
				}
			}
		}

		private static void TryAddSwitchInfo(int switchID, Source source, IOType ioType, IInfo info)
		{
			if (!ShouldSearchForSwitches)
			{
				return;
			}

			if (switchID < 1 || switchID > Data.System.Switches.Count)
			{
				return;
			}

			if (!TrackedSwitchIDs.ContainsKey(switchID))
			{
				return;
			}

			TrackedSwitchIDs[switchID].Add(new SwitchVarInfo(source, ioType, info));
		}

		private static void TryAddVariableInfo(int variableID, Source source, IOType ioType, IInfo info)
		{
			if (!ShouldSearchForVariables)
			{
				return;
			}

			if (variableID < 1 || variableID > Data.System.Variables.Count)
			{
				return;
			}

			if (!TrackedVariableIDs.ContainsKey(variableID))
			{
				return;
			}

			TrackedVariableIDs[variableID].Add(new SwitchVarInfo(source, ioType, info));
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
							int variableID = Convert.ToInt32(parameters[0]);
							TryAddVariableInfo(variableID, source, IOType.Write, info);
							break;
						}

					case EventCode.ControlSwitches:
						{
							if (!ShouldSearchForSwitches) break;
							int startID = Convert.ToInt32(parameters[0]);
							int endID = Convert.ToInt32(parameters[1]);
							for (int i = startID; i <= endID; i++)
							{
								TryAddSwitchInfo(i, source, IOType.Write, info);
							}
							break;
						}

					case EventCode.ControlVariables:
						{
							if (!ShouldSearchForVariables) break;
							int startID = Convert.ToInt32(parameters[0]);
							int endID = Convert.ToInt32(parameters[1]);
							for (int i = startID; i <= endID; i++)
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
								int checkedSwitchID = Convert.ToInt32(parameters[1]);
								TryAddSwitchInfo(checkedSwitchID, source, IOType.Read, info);
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

					case EventCode.Script:
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

				foreach (int switchID in switchesFound)
				{
					TryAddSwitchInfo(switchID, source, IOType.Unknown, info);
				}
			}

			//if (ShouldSearchForVariables)
			//{
			//	HashSet<int> variablesFound = new HashSet<int>();
			//}
		}
	}
}