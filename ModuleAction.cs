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

		public int ChainedCount
		{
			get; private set;
		}

		public string Nickname
		{
			get
			{
				if (string.IsNullOrEmpty(_nickname))
				{
					_nickname = GetType().Name;
				}
				return _nickname;
			}
		}

		private string _nickname = "";

		#endregion

		public ModuleAction() : this(string.Empty) { }

		public ModuleAction(string nickname)
		{
			_nickname = nickname;
			UniqueIdentifier = Guid.NewGuid().ToString();
			DataMap = new DataMap();
			Root = this;
		}

		#region Public Methods

		public void ChainAction(ModuleAction action)
		{
			if (action.Source != null)
			{
				if (action.Source._chainedActions.Remove(action))
				{
					action.Source.ChainedCount--;
				}
			}

			action.Source = this;
			action.Root = Root;

			_chainedActions.Add(action);
			ChainedCount++;
		}

		public bool HasUpwards<T>(bool inclSelf = false, Predicate<T> predicate = null, Predicate<ModuleAction> chainBlockade = null, bool inclSelfInBlockage = false)
		{
			return TryFindUpwards(out _, inclSelf, predicate, chainBlockade, inclSelfInBlockage);
		}

		public bool TryFindUpwards<T>(out T result, bool inclSelf = false, Predicate<T> predicate = null, Predicate<ModuleAction> chainBlockade = null, bool inclSelfInBlockage = false)
		{
			predicate = predicate ?? new Predicate<T>(x => true);
			chainBlockade = chainBlockade ?? new Predicate<ModuleAction>(x => false);
			ModuleAction source = Source;

			if (inclSelf && this is T castedSelf && predicate(castedSelf))
			{
				result = castedSelf;
				return true;
			}

			if (inclSelfInBlockage && chainBlockade(this))
			{
				result = default;
				return false;
			}

			while (source != null)
			{
				if (source is T castedSource && predicate(castedSource))
				{
					result = castedSource;
					return true;
				}

				if (chainBlockade(source))
				{
					break;
				}

				source = source.Source;
			}
			result = default;
			return false;
		}

		public T[] FindAllUpwards<T>(bool inclSelf = false, Predicate<T> predicate = null)
		{
			predicate = predicate ?? new Predicate<T>(x => true);
			List<T> results = new List<T>();
			ModuleAction source = Source;

			if (inclSelf && this is T castedSelf && predicate(castedSelf))
			{
				results.Add(castedSelf);
			}

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

		public bool TryFindDirectChainAction<T>(out T result, Predicate<T> predicate = null)
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

		public T[] FindAllChained<T>(Predicate<T> predicate = null)
			where T : ModuleAction
		{
			List<T> results = new List<T>();
			Queue<ModuleAction> chainedActions = new Queue<ModuleAction>(ChainedActions);
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

		public bool HasDownwards<T>(bool inclSelf = false, Predicate<T> predicate = null, Predicate<ModuleAction> chainBlockade = null, bool inclSelfInBlockage = false)
		{
			return TryFindDownwards(out _, inclSelf, predicate, chainBlockade, inclSelfInBlockage);
		}

		public bool TryFindDownwards<T>(out T result, bool inclSelf = false, Predicate<T> predicate = null, Predicate<ModuleAction> chainBlockade = null, bool inclSelfInBlockage = false)
		{
			Queue<ModuleAction> chainedActions = new Queue<ModuleAction>(ChainedActions);
			predicate = predicate ?? new Predicate<T>(x => true);
			chainBlockade = chainBlockade ?? new Predicate<ModuleAction>(x => false);

			if (inclSelf && this is T castedSelf && predicate(castedSelf))
			{
				result = castedSelf;
				return true;
			}

			if (inclSelfInBlockage && chainBlockade(this))
			{
				result = default;
				return false;
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
					for (int i = 0, c = action.ChainedActions.Count; i < c; i++)
					{
						chainedActions.Enqueue(action.ChainedActions[i]);
					}
				}
			}
			result = default;
			return false;
		}

		public T[] FindAllDownwards<T>(bool inclSelf = false, Predicate<T> predicate = null, Predicate<ModuleAction> chainBlockade = null, bool inclSelfInBlockage = false)
			where T : ModuleAction
		{
			List<T> results = new List<T>();
			Queue<ModuleAction> chainedActions = new Queue<ModuleAction>(ChainedActions);
			predicate = predicate ?? new Predicate<T>(x => true);
			chainBlockade = chainBlockade ?? new Predicate<ModuleAction>(x => false);

			if (inclSelf && this is T castedSelf && predicate(castedSelf))
			{
				results.Add(castedSelf);
			}

			if (inclSelfInBlockage && chainBlockade(this))
			{
				return results.ToArray();
			}

			while (chainedActions.Count > 0)
			{
				ModuleAction action = chainedActions.Dequeue();
				if (action is T castedAction && predicate(castedAction))
				{
					results.Add(castedAction);
				}

				if (!chainBlockade(action))
				{
					for (int i = 0, c = action.ChainedActions.Count; i < c; i++)
					{
						chainedActions.Enqueue(action.ChainedActions[i]);
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

		internal void MarkProcessedByModule(IModule module)
		{
			if (!IsProcessedByModule(module))
			{
				_processedByModulesList.Add(module.UniqueIdentifier);
			}
		}

		internal bool IsChainedByProcessor(ModuleProcessor processor)
		{
			return _chainedByProcessorList.Contains(processor.UniqueIdentifier);
		}

		internal void MarkChainedByProcessor(ModuleProcessor processor)
		{
			if (!IsChainedByProcessor(processor))
			{
				_chainedByProcessorList.Add(processor.UniqueIdentifier);
			}
		}


		#endregion
	}
}