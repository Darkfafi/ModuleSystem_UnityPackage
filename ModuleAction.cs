using ModuleSystem.Core;
using System;
using System.Collections.Generic;

namespace ModuleSystem
{
	public class ModuleAction
	{
		#region Variables

		public readonly string UniqueIdentifier;
		private readonly List<ModuleAction> _chainedActions = new List<ModuleAction>();
		private readonly HashSet<string> _processedByModulesList = new HashSet<string>();
		private readonly HashSet<string> _chainedByProcessorList = new HashSet<string>();

		#endregion

		#region Properties

		public ModuleAction Root
		{
			get; private set;
		}

		public ModuleAction Source
		{
			get; private set;
		}

		public DataMap DataMap
		{
			get; private set;
		}

		public IReadOnlyList<ModuleAction> ChainedActions => _chainedActions;

		#endregion

		public ModuleAction()
		{
			UniqueIdentifier = Guid.NewGuid().ToString();
			DataMap = new DataMap();
			Root = this;
		}

		#region Public Methods

		public void ChainAction(ModuleAction action)
		{
			if (action.Source != null)
			{
				action.Source._chainedActions.Remove(action);
			}

			action.Source = this;
			action.Root = Root;

			_chainedActions.Add(action);
		}

		public bool HasUpwards<T>(Predicate<T> predicate, Predicate<ModuleAction> chainBlockade)
		{
			return TryFindUpwards(predicate, chainBlockade, out _);
		}

		public bool TryFindUpwards<T>(Predicate<T> predicate, Predicate<ModuleAction> chainBlockade, out T result)
		{
			predicate = predicate ?? new Predicate<T>(x => true);
			chainBlockade = chainBlockade ?? new Predicate<ModuleAction>(x => false);
			ModuleAction source = Source;
			while (source != null)
			{
				if (chainBlockade(source))
				{
					break;
				}

				if (source is T castedSource && predicate(castedSource))
				{
					result = castedSource;
					return true;
				}
				source = source.Source;
			}
			result = default;
			return false;
		}

		public T[] FindAllUpwards<T>(Predicate<T> predicate)
		{
			predicate = predicate ?? new Predicate<T>(x => true);
			List<T> results = new List<T>();
			ModuleAction source = Source;
			while (source != null)
			{
				if (source is T castedSource && predicate(castedSource))
				{
					results.Add(castedSource);
				}
				source = source.Source;
			}
			return results.ToArray();
		}

		public bool TryFindDirectChainAction<T>(Predicate<T> predicate, out T result)
		{
			predicate = predicate ?? new Predicate<T>(x => true);
			for (int i = 0; i < _chainedActions.Count; i++)
			{
				ModuleAction chainedAction = _chainedActions[i];
				if (chainedAction is T castedChainedAction && predicate(castedChainedAction))
				{
					result = castedChainedAction;
					return true;
				}
			}
			result = default;
			return false;
		}

		public T[] FindAllChained<T>(Predicate<T> predicate)
			where T : ModuleAction
		{
			List<T> results = new List<T>();
			Queue<ModuleAction> chainedActions = new Queue<ModuleAction>(_chainedActions);
			predicate = predicate ?? new Predicate<T>(x => true);
			for (int i = 0; i < _chainedActions.Count; i++)
			{
				if (_chainedActions[i] is T castedChainedAction && predicate(castedChainedAction))
				{
					results.Add(castedChainedAction);
				}
			}
			return results.ToArray();
		}

		public bool HasDownwards<T>(Predicate<T> predicate, bool inclSelf)
		{
			return TryFindDownwards(predicate, null, inclSelf, out _);
		}

		public bool TryFindDownwards<T>(Predicate<T> predicate, Predicate<ModuleAction> chainBlockade, bool inclSelf, out T result)
		{
			Queue<ModuleAction> chainedActions = new Queue<ModuleAction>(_chainedActions);
			predicate = predicate ?? new Predicate<T>(x => true);
			chainBlockade = chainBlockade ?? new Predicate<ModuleAction>(x => false);

			if (inclSelf && this is T castedSelf && predicate(castedSelf))
			{
				result = castedSelf;
				return true;
			}

			while (chainedActions.Count > 0)
			{
				ModuleAction action = chainedActions.Dequeue();
				if (action is T castedAction && predicate(castedAction))
				{
					result = castedAction;
					return true;
				}

				if (!chainBlockade(action))
				{
					for (int i = 0, c = action._chainedActions.Count; i < c; i++)
					{
						chainedActions.Enqueue(action._chainedActions[i]);
					}
				}
			}
			result = default;
			return false;
		}

		public T[] FindAllDownwards<T>(Predicate<T> predicate, Predicate<ModuleAction> chainBlockade)
			where T : ModuleAction
		{
			List<T> results = new List<T>();
			Queue<ModuleAction> chainedActions = new Queue<ModuleAction>(_chainedActions);
			predicate = predicate ?? new Predicate<T>(x => true);
			chainBlockade = chainBlockade ?? new Predicate<ModuleAction>(x => false);

			while (chainedActions.Count > 0)
			{
				ModuleAction action = chainedActions.Dequeue();
				if (action is T castedAction && predicate(castedAction))
				{
					results.Add(castedAction);
				}

				if (!chainBlockade(action))
				{
					for (int i = 0, c = action._chainedActions.Count; i < c; i++)
					{
						chainedActions.Enqueue(action._chainedActions[i]);
					}
				}
			}
			return results.ToArray();
		}

		public IReadOnlyCollection<string> GetProcessedByModulesList()
		{
			return _processedByModulesList;
		}

		public IReadOnlyCollection<string> GetChainedByProcessorsList()
		{
			return _chainedByProcessorList;
		}

		#endregion

		#region Internal Methods

		internal bool IsProcessedByModule(IModule module)
		{
			return _processedByModulesList.Contains(module.UniqueIdentifier);
		}

		internal bool TryMarkProcessedByModule(IModule module)
		{
			return _processedByModulesList.Add(module.UniqueIdentifier);
		}

		internal bool IsChainedByProcessor(ModuleProcessor processor)
		{
			return _chainedByProcessorList.Contains(processor.UniqueIdentifier);
		}

		internal bool TryMarkChainedByProcessor(ModuleProcessor processor)
		{
			return _chainedByProcessorList.Add(processor.UniqueIdentifier);
		}


		#endregion
	}
}
