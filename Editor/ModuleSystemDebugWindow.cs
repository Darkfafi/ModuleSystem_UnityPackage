using ModuleSystem.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModuleSystem.Editor
{
	[ExecuteInEditMode]
	public class ModuleSystemDebugWindow : EditorWindow
	{
		#region Nested

		public enum Headers : int
		{
			Type = 0,
			Id = 1,
			Processing = 2,
			DataMap = 3,
			Fields = 4,
			ProcessType = 5,
		}

		#endregion

		#region Variables

		[SerializeField]
		private ModuleProcessorViewState _state = null;

		private ModuleProcessorView _moduleProcessorTreeView;

		#endregion

		#region Public Methods

		[MenuItem("ModuleSystem/TreeView")]
		static void OpenWindow()
		{
			ModuleSystemDebugWindow window = GetWindow<ModuleSystemDebugWindow>();

			window.titleContent = new GUIContent("ModuleSystem TreeView");
			window.Show();
		}

		#endregion

		#region Lifecycle

		protected void Update()
		{
			if (_state.HasTarget)
			{
				_moduleProcessorTreeView.Reload();

				// Refresh Update Logics
				if (_state.IsRefreshed)
				{
					_moduleProcessorTreeView.multiColumnHeader.ResizeToFit();
					_state.IsRefreshed = false;
				}
			}
			Repaint();
		}

		protected void OnEnable()
		{
			if (_moduleProcessorTreeView == null)
			{
				if (_state == null)
				{
					_state = new ModuleProcessorViewState();
				}

				_moduleProcessorTreeView = new ModuleProcessorView(_state);
				EditorSceneManager.sceneLoaded += OnSceneLoaded;
				_state.RefreshTarget();
				_state.IsRefreshed = true;
			}
		}

		protected void OnDisable()
		{
			if (_moduleProcessorTreeView != null)
			{
				_moduleProcessorTreeView = null;
				EditorSceneManager.sceneLoaded -= OnSceneLoaded;
			}
		}

		private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
		{
			_state.RefreshTarget();
		}

		public void RefreshTarget()
		{
			if (_moduleProcessorTreeView == null)
			{
				if (_state == null)
				{
					_state = new ModuleProcessorViewState();
				}

				_moduleProcessorTreeView = new ModuleProcessorView(_state);
			}

			if (_state.HasTarget)
			{
				if (_state.TargetStackProcessedEvent != null)
				{
					titleContent.text = $"Processor: {_state.TargetStackProcessedEvent.name}";
				}
				else
				{
					titleContent.text = "No Behaviour Processor";
				}
			}
			else
			{
				titleContent.text = "No Processor";
				EditorGUILayout.LabelField($"No Active {nameof(ActionStackProcessedEvent)} Selected");
			}
		}

		private void OnRefreshData()
		{
			if (_moduleProcessorTreeView != null)
			{
				_moduleProcessorTreeView.multiColumnHeader.ResizeToFit();
			}
		}

		protected void OnGUI()
		{
			if (_moduleProcessorTreeView != null)
			{
				RefreshTarget();

				ActionStackProcessedEvent targetStackEvent = _state.TargetStackProcessedEvent;

				targetStackEvent = EditorGUILayout.ObjectField("Target: ", targetStackEvent, typeof(ActionStackProcessedEvent), true) as ActionStackProcessedEvent;

				if (targetStackEvent != _state.TargetStackProcessedEvent)
				{
					_state.SetTargetContainer(targetStackEvent, false);
				}

				if (_state.HasTarget)
				{
					_moduleProcessorTreeView.OnGUI(new Rect(0, 25, position.width, position.height));
				}
			}
		}

		#endregion

		#region Nested

		[Serializable]
		private class ModuleProcessorViewState : TreeViewState
		{
			[SerializeField]
			private ActionStackProcessedEvent _stackProcessedEvent;

			private List<ModuleActionData> _collectedCoreActions = new List<ModuleActionData>();

			public ActionStackProcessedEvent TargetStackProcessedEvent => _stackProcessedEvent;

			public bool HasTarget => _stackProcessedEvent != null;

			public bool IsRefreshed = false;

			#region Public Methods

			public ModuleActionData[] GetCoreActions()
			{
				return GetCollectedCoreActionsList().ToArray();
			}

			public void RemoveAction(ModuleActionData data)
			{
				GetCollectedCoreActionsList().Remove(data);
			}

			public void RefreshTarget()
			{
				SetTargetContainer(_stackProcessedEvent, true);
			}

			public void SetTargetContainer(ActionStackProcessedEvent target, bool force)
			{
				bool hasChange = _stackProcessedEvent != target;
				if (target == null || hasChange || force)
				{
					if (_stackProcessedEvent != null)
					{
						_stackProcessedEvent.RemoveListener(OnActionStackProcessedEvent);
					}

					if (!force)
					{
						GetCollectedCoreActionsList().Clear();
					}

					_stackProcessedEvent = target;

					if (_stackProcessedEvent != null)
					{
						_stackProcessedEvent.AddListener(OnActionStackProcessedEvent);
					}
				}

				if (hasChange)
				{
					IsRefreshed = true;
				}
			}

			private List<ModuleActionData> GetCollectedCoreActionsList()
			{
				if (_collectedCoreActions == null)
				{
					_collectedCoreActions = new List<ModuleActionData>();
				}

				return _collectedCoreActions;
			}

			private void OnActionStackProcessedEvent(ModuleAction coreAction)
			{
				Queue<(ModuleAction, ModuleActionData)> nextToConvert = new Queue<(ModuleAction, ModuleActionData)>();
				ModuleActionData coreActionData = new ModuleActionData();
				nextToConvert.Enqueue((coreAction, coreActionData));
				coreActionData.ProcessTypeString = "Root (Enqueued)";

				while (nextToConvert.Count > 0)
				{
					(ModuleAction convertingAction, ModuleActionData convertingData) = nextToConvert.Dequeue();
					List<string> options = new List<string>();

					// Processing
					options.Add("~Processing~");
					options.Add("-- Chained By Processors -- ");
					options.AddRange(convertingAction.GetChainedByProcessorsList());
					options.Add("-- Processed By Modules -- ");
					options.AddRange(convertingAction.GetProcessedByModulesList());


					string[] processingOptions = options.ToArray(); ;
					options.Clear();

					// DataMap
					options.Add("~DataMap~");
					options.Add("-- Tags -- ");
					options.AddRange(convertingAction.DataMap.GetTags());

					options.Add("-- Marks -- ");

					string[] markKeys = convertingAction.DataMap.GetMarkKeys();

					for (int i = 0; i < markKeys.Length; i++)
					{
						if (convertingAction.DataMap.TryGetMarkSuffixes(markKeys[i], out string[] suffixes))
						{
							for (int j = 0; j < suffixes.Length; j++)
							{
								options.Add(string.Format("{0}-{1}", markKeys[i], suffixes[j]));
							}
						}
					}

					options.Add("-- Data --");
					foreach (var pair in convertingAction.DataMap.GetDataMapInternal())
					{
						options.Add(string.Format("{0}: {1}", pair.Key, pair.Value));
					}

					string[] dataMapOptions = options.ToArray();

					options.Clear();

					// Fields
					FieldInfo[] infos = convertingAction.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
					options.Add("~Fields~");
					for (int j = 0; j < infos.Length; j++)
					{
						FieldInfo info = infos[j];
						object val = info.GetValue(convertingAction);
						string valString = val == null ? "NULL" : val.ToString();
						options.Add(string.Format("{0}: {1}", info.Name, valString));
					}

					string[] fieldsOptions = options.ToArray();

					convertingData.TypeString = convertingAction.GetType().Name;
					convertingData.UniqueIdentifierString = convertingAction.UniqueIdentifier;
					convertingData.DataMapOptions = dataMapOptions;
					convertingData.FieldOptions = fieldsOptions;
					convertingData.ProcessingOptions = processingOptions;

					// Chained Actions
					IReadOnlyList<ModuleAction> chainedActions = convertingAction.ChainedActions;
					ModuleActionData[] chainedActionsData = new ModuleActionData[chainedActions.Count];
					for (int i = 0, c = chainedActionsData.Length; i < c; i++)
					{
						ModuleActionData actionData = new ModuleActionData();
						nextToConvert.Enqueue((chainedActions[i], actionData));
						chainedActionsData[i] = actionData;
						actionData.ProcessTypeString = "Chained";
					}
					convertingData.ChainedActionsData = chainedActionsData;
				}

				GetCollectedCoreActionsList().Add(coreActionData);
				IsRefreshed = true;
			}

			#endregion

			#region Nested

			public class ModuleActionData
			{
				public string TypeString;
				public string UniqueIdentifierString;
				public string[] DataMapOptions;
				public string[] FieldOptions;
				public string[] ProcessingOptions;
				public string ProcessTypeString;
				public ModuleActionData[] ChainedActionsData;
			}

			#endregion
		}

		private class ModuleProcessorView : TreeView
		{
			#region Variables

			private Dictionary<int, ModuleProcessorViewState.ModuleActionData> _idToAction = new Dictionary<int, ModuleProcessorViewState.ModuleActionData>();

			#endregion

			#region Properties

			private ModuleProcessorViewState _processorState = null;

			#endregion

			public static MultiColumnHeaderState.Column[] CreateColumnHeaders<T>() where T : Enum
			{
				Array e = Enum.GetValues(typeof(T));
				MultiColumnHeaderState.Column[] headers = new MultiColumnHeaderState.Column[e.Length];
				for (int i = 0; i < e.Length; i++)
				{
					headers[i] = new MultiColumnHeaderState.Column
					{
						headerContent = new GUIContent(e.GetValue(i).ToString()),
						autoResize = true,
					};
				}
				return headers;
			}

			public ModuleProcessorView(ModuleProcessorViewState state)
				: base(state, new MultiColumnHeader(new MultiColumnHeaderState(CreateColumnHeaders<Headers>())))
			{
				_processorState = state;
				rowHeight = 20;
				showAlternatingRowBackgrounds = true;
				showBorder = true;
				Reload();
			}

			#region Lifecycle

			protected override void RowGUI(RowGUIArgs args)
			{
				if (_idToAction.TryGetValue(args.item.id, out ModuleProcessorViewState.ModuleActionData action))
				{
					int numOfColumns = args.GetNumVisibleColumns();
					for (int i = 0; i < numOfColumns; i++)
					{
						Headers column = (Headers)args.GetColumn(i);
						Rect cellRect = args.GetCellRect(i);
						args.rowRect = cellRect;
						CenterRectUsingSingleLineHeight(ref cellRect);
						cellRect.x += depthIndentWidth * args.item.depth;

						switch (column)
						{
							case Headers.Type:
								args.label = action.TypeString;
								break;
							case Headers.Id:
								args.label = action.UniqueIdentifierString;
								break;
							case Headers.Processing:
								EditorGUI.Popup(cellRect, 0, action.ProcessingOptions);
								continue;
							case Headers.DataMap:
								EditorGUI.Popup(cellRect, 0, action.DataMapOptions);
								continue;
							case Headers.Fields:
								EditorGUI.Popup(cellRect, 0, action.FieldOptions);
								continue;
							case Headers.ProcessType:
								args.label = action.ProcessTypeString;
								break;
						}
						base.RowGUI(args);
					}
				}
				else
				{
					base.RowGUI(args);
				}
			}

			public override void OnGUI(Rect rect)
			{
				base.OnGUI(rect);

				Event e = Event.current;
				if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete && HasSelection())
				{
					foreach (var id in GetSelection())
					{
						if (_idToAction.TryGetValue(id, out ModuleProcessorViewState.ModuleActionData moduleAction))
						{
							_processorState.RemoveAction(moduleAction);
						}
					}
				}
			}

			protected override TreeViewItem BuildRoot()
			{
				int id = 0;
				_idToAction.Clear();

				TreeViewItem root = CreateTreeViewItem(null);
				root.depth = -1;

				ModuleProcessorViewState.ModuleActionData[] coreActions = _processorState.GetCoreActions();
				if (coreActions.Length > 0)
				{
					for (int i = 0; i < coreActions.Length; i++)
					{
						AddTreeItem(root, coreActions[i]);
					}
				}

				if (root.children == null || root.children.Count == 0)
				{
					root.AddChild(CreateTreeViewItem(null, "N/A"));
				}

				SetupDepthsFromParentsAndChildren(root);

				return root;

				void AddTreeItem(TreeViewItem parentTreeItem, ModuleProcessorViewState.ModuleActionData moduleAction)
				{
					TreeViewItem treeItem = CreateTreeViewItem(moduleAction);
					_idToAction.Add(id, moduleAction);
					parentTreeItem.AddChild(treeItem);

					ModuleProcessorViewState.ModuleActionData[] chainedChildren = moduleAction.ChainedActionsData;
					for (int i = 0; i < chainedChildren.Length; i++)
					{
						AddTreeItem(treeItem, chainedChildren[i]);
					}
				}

				TreeViewItem CreateTreeViewItem(ModuleProcessorViewState.ModuleActionData moduleAction, string fallbackName = "-")
				{
					return new TreeViewItem
					{
						id = ++id,
						displayName = moduleAction != null ? moduleAction.UniqueIdentifierString : fallbackName,
					};
				}
			}

			#endregion
		}

		#endregion
	}
}
