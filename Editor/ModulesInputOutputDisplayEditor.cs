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

		public static GUIContent GetIcon(int stage)
		{
			switch(stage)
			{
				case 0:
					return _correctIcon;
				case 1:
					return _warningIcon;
				case 2:
					return _errorIcon;
			}
			return null;
		}

		public static void DrawIconLabel(int stage, Action drawAction)
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.Label(GetIcon(stage), GUILayout.ExpandWidth(false));
				drawAction?.Invoke();
			}
			GUILayout.EndHorizontal();
		}

		public static void DrawIconLabel(bool isValid, Action drawAction)
		{
			DrawIconLabel(isValid ? 0 : 2, drawAction);
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
								_editorItems.Add(new ModuleEditorItem(modules[i], _targetObject.name + ">> "));
							}
						}
					}

					for (int i = 0; i < _externalTypes.Length; i++)
					{
						_editorItems.Add(new ModuleEditorItem(_externalTypes[i]));
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
								DrawIconLabel(moduleItem.ValidityStage, () =>
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
											DrawIconLabel(inputItem.IsValid, () =>
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
											DrawIconLabel(outputItem.IsValid, () =>
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

		private class ModuleEditorItem
		{
			public readonly string ItemName;
			public readonly ModuleEditorInputItem[] InputItems;
			public readonly ModuleEditorOutputItem[] OutputItems;

			public int ValidityStage
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

			public bool IsFoldedOut;

			public ModuleEditorItem(IModule module, string prefix = "")
				: this(module.GetType(), prefix)
			{
				ItemName = (prefix + module.UniqueIdentifier);
			}

			public ModuleEditorItem(Type type, string prefix = "")
			{
				ItemName = (prefix + type.Name);
				ItemType = type;
				InputItems = type.GetCustomAttributes<ModuleActionInputAttribute>(true).Select(x => new ModuleEditorInputItem(x)).ToArray();
				OutputItems = type.GetCustomAttributes<ModuleActionOutputAttribute>(true).Select(x => new ModuleEditorOutputItem(x)).ToArray();
			}

			public void Initialize(ModuleEditorItem[] allItems)
			{
				if (!Initialized)
				{
					Initialized = true;

					foreach(var inputItem in InputItems)
					{
						inputItem.Init(allItems);
					}

					foreach (var outputItem in OutputItems)
					{
						outputItem.Init(allItems);
					}

					if(InputItems.Length == 0 && OutputItems.Length == 0)
					{
						ValidityStage = 1;
					}
					else
					{ 
						ValidityStage = InputItems.All(x => x.IsValid) && OutputItems.All(x => x.IsValid) ? 0 : 2;
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

		private class ModuleEditorInputItem
		{
			public readonly ModuleActionInputAttribute Input;
			public readonly string InputName;
			public bool IsFoldedOut;

			public bool IsValid => ConnectedItems.Length > 0;

			public ModuleEditorItem[] ConnectedItems
			{
				get; private set;
			}

			public ModuleEditorInputItem(ModuleActionInputAttribute input)
			{
				Input = input;
				InputName = input.ActionType.Name;
			}

			public void Init(ModuleEditorItem[] allEditorItems)
			{
				List<ModuleEditorItem> filteredItems = new List<ModuleEditorItem>(allEditorItems);
				for (int i = filteredItems.Count - 1; i >= 0; i--)
				{
					if (!filteredItems[i].IsConnectedByOutput(Input))
					{
						filteredItems.RemoveAt(i);
					}
				}
				ConnectedItems = filteredItems.ToArray();
			}
		}

		private class ModuleEditorOutputItem
		{
			public readonly ModuleActionOutputAttribute Output;
			public readonly string OutputName;

			public bool IsFoldedOut;
			public bool IsValid => ConnectedItems.Length > 0;

			public ModuleEditorItem[] ConnectedItems
			{
				get; private set;
			}

			public ModuleEditorOutputItem(ModuleActionOutputAttribute output)
			{
				Output = output;
				OutputName = output.ActionType.Name;
			}

			public void Init(ModuleEditorItem[] allEditorItems)
			{
				List<ModuleEditorItem> filteredItems = new List<ModuleEditorItem>(allEditorItems);
				for (int i = filteredItems.Count - 1; i >= 0; i--)
				{
					if (!filteredItems[i].IsConnectedByInput(Output))
					{
						filteredItems.RemoveAt(i);
					}
				}
				ConnectedItems = filteredItems.ToArray();
			}
		}

		#endregion
	}
}