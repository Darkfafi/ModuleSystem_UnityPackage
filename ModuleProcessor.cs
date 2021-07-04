using ModuleSystem.Core;
using System;
using System.Collections.Generic;

namespace ModuleSystem
{
	public class ModuleProcessor : IDisposable
	{
		#region Events

		public delegate void ModuleActionHandler(ModuleAction moduleAction);
		public event ModuleActionHandler ActionProcessedEvent;
		public event ModuleActionHandler ActionStackProcessedEvent;

		#endregion

		#region Variables

		private bool _isProcessing = false;
		private IModule _lockingModule = null;

		private ModuleAction _initialAction = null;
		private Stack<ModuleAction> _executionStack = new Stack<ModuleAction>();
		private Queue<ModuleAction> _nextActions = new Queue<ModuleAction>();

		private List<IModule> _modules;

		private bool _started = false;

		#endregion

		#region Properties

		public string UniqueIdentifier
		{
			get;
		}

		public bool IsProcessing => _isProcessing || _lockingModule != null;

		public bool IsDisabled
		{
			get; private set;
		}

		#endregion

		public ModuleProcessor(bool startModules, IModule[] modules, string uniqueIdentifierPrefix = "")
		{
			uniqueIdentifierPrefix = string.IsNullOrEmpty(uniqueIdentifierPrefix) ? "Anonymous" : uniqueIdentifierPrefix;
			UniqueIdentifier = string.Concat(uniqueIdentifierPrefix, ": ", Guid.NewGuid().ToString());
			_started = false;
			_modules = new List<IModule>(modules);

			for (int i = 0; i < _modules.Count; i++)
			{
				_modules[i].Init(this);
			}

			if (startModules)
			{
				StartModules();
			}
		}

		#region Public Methods

		public void AddModule(IModule module)
		{
			if (!_modules.Contains(module))
			{
				_modules.Add(module);
				module.Init(this);
				if (_started)
				{
					module.StartModule();
				}
			}
		}

		public void AddModules(IModule[] modules)
		{
			List<IModule> addedModules = new List<IModule>();
			for (int i = 0; i < modules.Length; i++)
			{
				IModule module = modules[i];
				if (!_modules.Contains(module))
				{
					_modules.Add(module);
					module.Init(this);
					addedModules.Add(module);
				}
			}

			if (_started)
			{
				for (int i = 0; i < addedModules.Count; i++)
				{
					IModule module = addedModules[i];
					if (_modules.Contains(module))
					{
						module.StartModule();
					}
				}
			}
		}

		public void StartModules()
		{
			if (!_started)
			{
				for (int i = 0; i < _modules.Count; i++)
				{
					_modules[i].StartModule();
				}

				_started = true;
				TryProcessStack();
			}
		}

		public void EnqueueAction(ModuleAction action)
		{
			if (!_started)
			{
				_nextActions.Enqueue(action);
				return;
			}

			if (!IsDisabled || IsProcessing)
			{
				_nextActions.Enqueue(action);
				TryProcessStack();
			}
		}

		public void SetDisabled(bool isDisabled)
		{
			if (IsDisabled != isDisabled)
			{
				IsDisabled = isDisabled;
			}
		}

		public bool IsLockingModule(IModule module)
		{
			return _lockingModule == module;
		}

		public void Unlock(IModule module)
		{
			if (IsLockingModule(module))
			{
				_lockingModule = null;
				TryProcessStack();
			}
		}

		public void Dispose()
		{
			for (int i = _modules.Count - 1; i >= 0; i--)
			{
				_modules[i].Deinit();
			}

			_modules = null;

			_nextActions.Clear();
			_executionStack.Clear();

			_lockingModule = null;
			_initialAction = null;
			_isProcessing = false;
			_started = false;
		}

		#endregion

		#region Private Methods

		private void TryProcessStack()
		{
			if (IsProcessing)
			{
				return;
			}

			_isProcessing = true;

			// If the stack is empty, but the queue is not, then place the first of the queue on top of the stack
			if (_executionStack.Count == 0 && _nextActions.Count > 0)
			{
				_executionStack.Push(_nextActions.Dequeue());
			}

			// Stack Resolve Loop
			while (_executionStack.Count > 0)
			{
				ModuleAction action = _executionStack.Peek();

				if (_initialAction == null)
				{
					_initialAction = action;
				}

				for (int i = 0; i < _modules.Count; i++)
				{
					IModule module = _modules[i];
					_lockingModule = module;
					if (!action.IsProcessedByModule(module))
					{
						// Processing Callback
						if (action is CallbackModuleAction callbackModule && callbackModule.ModuleSource.UniqueIdentifier == module.UniqueIdentifier)
						{
							callbackModule.TryMarkProcessedByModule(module);
							callbackModule?.ModuleCallback(callbackModule);
							break;
						}
						// Processing Module
						else if (module.TryProcess(action, () => 
						{
							Unlock(module);
						}))
						{
							action.TryMarkProcessedByModule(module);
							if (_lockingModule != null)
							{
								_isProcessing = false;
								return;
							}
							else
							{
								ChainActions(action);

								// If a new actions are on the stack, process those before finishing the processing of the source
								if (_executionStack.Peek().UniqueIdentifier != action.UniqueIdentifier)
								{
									break;
								}
								else
								{
									i = -1;
									continue;
								}
							}
						}
					}

					_lockingModule = null;
				}

				// After the action processing is done, check for chain reactions, if any are added, process them before closing this action
				ChainActions(action);
				if (_executionStack.Peek().UniqueIdentifier != action.UniqueIdentifier)
				{
					continue;
				}

				_executionStack.Pop();

				ActionProcessedEvent?.Invoke(action);

				// If the Stack is completely resolved
				if (_executionStack.Count == 0)
				{
					if (_initialAction != null)
					{
						ModuleAction actionBase = _initialAction;
						_initialAction = null;

						for (int i = 0; i < _modules.Count; i++)
						{
							_modules[i].OnResolvedStack(actionBase);
						}

						ActionStackProcessedEvent?.Invoke(actionBase);
					}

					// Process next in queue, causing the next stack flow on the execution stack
					if (_nextActions.Count > 0)
					{
						_executionStack.Push(_nextActions.Dequeue());
					}
				}
			}

			_isProcessing = false;
		}

		private void ChainActions(ModuleAction source)
		{
			// Stack Chain Actions after source is processed completely
			for (int i = source.ChainedActions.Count - 1; i >= 0; i--)
			{
				ModuleAction chainedAction = source.ChainedActions[i];
				if (chainedAction.TryMarkChainedByProcessor(this))
				{
					_executionStack.Push(chainedAction);
				}
			}
		}

		#endregion
	}
}
