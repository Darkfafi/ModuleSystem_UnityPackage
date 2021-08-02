using ModuleSystem.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ModuleSystem.Editor
{
	public class ModulesInputOutputDisplayEditor : EditorWindow
	{
		#region Consts

		private static GUIContent _errorIcon = null;
		private static GUIContent _warningIcon = null;
		private static GUIContent _correctIcon = null;
		private static Type[] _externalTypes = null;

		#endregion

		#region (Editor) Variables

		[SerializeField]
		private UnityEngine.Object _targetObject = null;

		[SerializeField]
		private Vector2 _scrollPosition = Vector2.zero;

		[SerializeField]
		private HashSet<ModuleEditorItem> _editorItems = null;

		#endregion

		#region Public Methods

		[MenuItem("ModuleSystem/Modules Inputs Outputs")]
		static void OpenWindow()
		{
			ModulesInputOutputDisplayEditor window = GetWindow<ModulesInputOutputDisplayEditor>();
			window.titleContent = new GUIContent("Modules Inputs & Outputs");
			window.Show();
		}

		public static GUIContent GetIcon(ValidStage state)
		{
			switch(state)
			{
				case ValidStage.Valid:
					return _correctIcon;
				case ValidStage.Warning:
					return _warningIcon;
				case ValidStage.Error:
					return _errorIcon;
			}
			return null;
		}

		public static void DrawIconLabel(IHasValidState stateHolder, Action drawAction)
		{
			GUILayout.BeginHorizontal();
			{
				GUIContent iconContent = new GUIContent(GetIcon(stateHolder.ValidState));
				if(!string.IsNullOrEmpty(stateHolder.ValidStateReason))
				{
					iconContent.tooltip = stateHolder.ValidStateReason;
				}
				GUILayout.Label(iconContent, GUILayout.ExpandWidth(false));
				drawAction?.Invoke();
			}
			GUILayout.EndHorizontal();
		}

		public static Type[] GetAllExternalTypes()
		{
			return AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(x => x.GetTypes())
				.Where(x => x.IsClass && !x.IsAbstract && 
				(x.GetCustomAttributes<ModuleActionInputAttribute>().Count() > 0 || x.GetCustomAttributes<ModuleActionOutputAttribute>().Count() > 0))
				.ToArray();
		}

		#endregion

		#region Lifecycle

		protected void OnGUI()
		{
			if(_errorIcon == null)
			{
				_errorIcon = EditorGUIUtility.IconContent("Error");
			}

			if(_warningIcon == null)
			{
				_warningIcon = EditorGUIUtility.IconContent("Warning");
			}

			if(_correctIcon == null)
			{
				_correctIcon = EditorGUIUtility.IconContent("Installed");
			}

			if(_externalTypes == null)
			{
				_externalTypes = GetAllExternalTypes();
			}

			// Selector
			UnityEngine.Object newObject = EditorGUILayout.ObjectField("Module Processor: ", _targetObject, typeof(UnityEngine.Object), true);
			if(newObject != _targetObject)
			{
				_editorItems = null;
				if (newObject != null)
				{
					if (newObject is GameObject gameObject)
					{
						_targetObject = gameObject.GetComponent<IHaveModuleProcessor>() as UnityEngine.Object;
					}
					else if (newObject is MonoBehaviour monoObject)
					{
						_targetObject = monoObject.GetComponent<IHaveModuleProcessor>() as UnityEngine.Object;
					}
					else if (newObject is IHaveModuleProcessor)
					{
						_targetObject = newObject;
					}
				}
				else
				{
					_targetObject = null;
				}

			}

			// Display
			GUILayout.BeginVertical("box");
			{
				if (_editorItems == null)
				{
					_editorItems = new HashSet<ModuleEditorItem>();
					if (_targetObject != null && _targetObject is IHaveModuleProcessor targetProcessorHolder)
					{
						if (targetProcessorHolder.Processor != null)
						{
							IModule[] modules = targetProcessorHolder.Processor.GetModules();
							for (int i = 0; i < modules.Length; i++)
							{
								_editorItems.Add(new ModuleEditorItem(modules[i], false, _targetObject.name + ">> "));
							}
						}
					}

					for (int i = 0; i < _externalTypes.Length; i++)
					{
						_editorItems.Add(new ModuleEditorItem(_externalTypes[i], true));
					}

					foreach (var item in _editorItems)
					{
						item.Initialize(_editorItems.ToArray());
					}
				}

				if (_editorItems.Count > 0)
				{
					GUILayout.Label($"Modules ({_editorItems.Count})");
					_scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
					{
						foreach (ModuleEditorItem moduleItem in _editorItems)
						{
							GUILayout.Space(10f);
							GUILayout.BeginVertical("box");
							{
								DrawIconLabel(moduleItem, () =>
								{
									moduleItem.IsFoldedOut = EditorGUILayout.Foldout(moduleItem.IsFoldedOut, moduleItem.ItemName);
								});
								if (moduleItem.IsFoldedOut)
								{
									// Show input and output attribute types and make it a foldout so you can see which modules link to which outputs with their input
									GUILayout.BeginVertical("box");
									{
										GUILayout.Label("Inputs:");
										foreach (var inputItem in moduleItem.InputItems)
										{
											DrawIconLabel(inputItem, () =>
											{
												inputItem.IsFoldedOut = EditorGUILayout.Foldout(inputItem.IsFoldedOut, inputItem.InputName);
											});
											if (inputItem.IsFoldedOut)
											{
												ModuleEditorItem[] connectedItems = inputItem.ConnectedItems;
												for (int j = 0; j < connectedItems.Length; j++)
												{
													GUILayout.Label(connectedItems[j].ItemName);
												}
											}
										}
									}
									GUILayout.EndVertical();
									GUILayout.BeginVertical("box");
									{
										GUILayout.Label("Outputs:");
										foreach (var outputItem in moduleItem.OutputItems)
										{
											DrawIconLabel(outputItem, () =>
											{
												outputItem.IsFoldedOut = EditorGUILayout.Foldout(outputItem.IsFoldedOut, outputItem.OutputName);
											});

											if (outputItem.IsFoldedOut)
											{
												ModuleEditorItem[] connectedItems = outputItem.ConnectedItems;
												for (int j = 0; j < connectedItems.Length; j++)
												{
													GUILayout.Label(connectedItems[j].ItemName);
												}
											}
										}
									}
									GUILayout.EndVertical();
								}
							}
							GUILayout.EndVertical();
							GUILayout.Space(10f);
						}
					}
					GUILayout.EndScrollView();
				}
				
			}
			GUILayout.EndVertical();
		}

		protected void OnDisable()
		{
			
		}

		#endregion

		#region Nested

		private class ModuleEditorItem : IHasValidState
		{
			public readonly string ItemName;
			public readonly ModuleEditorInputItem[] InputItems;
			public readonly ModuleEditorOutputItem[] OutputItems;
			public string ValidItemName => ItemName;

			public ValidStage ValidState
			{
				get; private set;
			}

			public string ValidStateReason
			{
				get; private set;
			}

			public bool Initialized
			{
				get; private set;
			}

			public Type ItemType
			{
				get; private set;
			}

			public bool IsExternal
			{
				get; private set;
			}

			public bool IsFoldedOut;

			public ModuleEditorItem(IModule module, bool isExternal, string prefix = "")
				: this(module.GetType(), isExternal, prefix)
			{
				ItemName = (prefix + module.UniqueIdentifier);
			}

			public ModuleEditorItem(Type type, bool isExternal, string prefix = "")
			{
				ItemName = (prefix + type.Name);
				ItemType = type;
				InputItems = type.GetCustomAttributes<ModuleActionInputAttribute>(true).Select(x => new ModuleEditorInputItem(x)).ToArray();
				OutputItems = type.GetCustomAttributes<ModuleActionOutputAttribute>(true).Select(x => new ModuleEditorOutputItem(x)).ToArray();
				IsExternal = isExternal;
			}

			public void Initialize(ModuleEditorItem[] allItems)
			{
				if (!Initialized)
				{
					Initialized = true;

					ValidState = ValidStage.Valid;
					ValidStateReason = string.Empty;

					foreach (var inputItem in InputItems)
					{
						inputItem.Init(allItems, IsExternal);
					}

					foreach (var outputItem in OutputItems)
					{
						outputItem.Init(allItems, IsExternal);
					}

					List<IHasValidState> items = new List<IHasValidState>(InputItems);
					items.AddRange(OutputItems);

					if (items.Count == 0)
					{
						ValidState = ValidStage.Warning;
						ValidStateReason = "No Inputs or Outputs Registered";
					}
					else
					{
						for (int i = 0; i < items.Count; i++)
						{
							IHasValidState validStateItem = items[i];
							if (ValidState < validStateItem.ValidState)
							{
								ValidState = validStateItem.ValidState;
								ValidStateReason = $"{validStateItem.ValidItemName}: {validStateItem.ValidStateReason}";
							}

							if (ValidState == ValidStage.Error)
							{
								break;
							}
						}
					}
				}
			}

			public bool IsConnectedByOutput(ModuleActionInputAttribute input)
			{
				foreach (var outputItem in OutputItems)
				{
					if (input.ActionType.IsAssignableFrom(outputItem.Output.ActionType))
					{
						return true;
					}
				}
				return false;
			}

			public bool IsConnectedByInput(ModuleActionOutputAttribute output)
			{
				foreach (var inputItem in InputItems)
				{
					if (inputItem.Input.ActionType.IsAssignableFrom(output.ActionType))
					{
						return true;
					}
				}
				return false;
			}

			public override bool Equals(object obj)
			{
				if(obj is ModuleEditorItem item)
				{
					return item.ItemType == ItemType;
				}
				return base.Equals(obj);
			}

			public override int GetHashCode()
			{
				return ItemType.GetHashCode();
			}
		}

		private class ModuleEditorInputItem : IHasValidState
		{
			public readonly ModuleActionInputAttribute Input;
			public readonly string InputName;
			public bool IsFoldedOut;

			public string ValidItemName => InputName;

			public ValidStage ValidState
			{
				get; private set;
			}

			public string ValidStateReason
			{
				get; private set;
			}

			public ModuleEditorItem[] ConnectedItems
			{
				get; private set;
			}

			public ModuleEditorInputItem(ModuleActionInputAttribute input)
			{
				Input = input;
				InputName = input.ActionType.Name;
			}

			public void Init(ModuleEditorItem[] allEditorItems, bool isExternal)
			{
				List<ModuleEditorItem> filteredItems = new List<ModuleEditorItem>(allEditorItems);
				
				ValidState = ValidStage.Valid;
				ValidStateReason = string.Empty;

				for (int i = filteredItems.Count - 1; i >= 0; i--)
				{
					if (!filteredItems[i].IsConnectedByOutput(Input))
					{
						filteredItems.RemoveAt(i);
					}
				}
				ConnectedItems = filteredItems.ToArray();

				if(ConnectedItems.Length == 0)
				{
					ValidState = ValidStage.Error;
					ValidStateReason = "No Connected Outputs";
				}
				else if(!isExternal && ConnectedItems.Any(x => x.IsExternal && x.ItemType is IModule))
				{
					ValidState = ValidStage.Warning;
					ValidStateReason = "External Module Outputs";
				}
			}
		}

		private class ModuleEditorOutputItem : IHasValidState
		{
			public readonly ModuleActionOutputAttribute Output;
			public readonly string OutputName;

			public bool IsFoldedOut;

			public string ValidItemName => OutputName;

			public ValidStage ValidState
			{
				get; private set;
			}

			public string ValidStateReason
			{
				get; private set;
			}

			public ModuleEditorItem[] ConnectedItems
			{
				get; private set;
			}

			public ModuleEditorOutputItem(ModuleActionOutputAttribute output)
			{
				Output = output;
				OutputName = output.ActionType.Name;
			}

			public void Init(ModuleEditorItem[] allEditorItems, bool isExternal)
			{
				List<ModuleEditorItem> filteredItems = new List<ModuleEditorItem>(allEditorItems);
				ValidState = ValidStage.Valid;
				ValidStateReason = string.Empty;

				for (int i = filteredItems.Count - 1; i >= 0; i--)
				{
					if (!filteredItems[i].IsConnectedByInput(Output))
					{
						filteredItems.RemoveAt(i);
					}
				}

				ConnectedItems = filteredItems.ToArray();

				if (ConnectedItems.Length == 0)
				{
					ValidState = ValidStage.Error;
					ValidStateReason = "No Connected Inputs";
				}
				else if (!isExternal && ConnectedItems.Any(x => x.IsExternal && x.ItemType is IModule))
				{
					ValidState = ValidStage.Warning;
					ValidStateReason = "External Module Inputs";
				}
			}
		}

		public interface IHasValidState
		{
			string ValidItemName
			{
				get;
			}

			ValidStage ValidState
			{
				get;
			}

			string ValidStateReason
			{
				get;
			}
		}

		public enum ValidStage : int
		{
			Valid = 0, 
			Warning = 1,
			Error = 2
		}

		#endregion
	}
}