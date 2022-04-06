using ModuleSystem.Core;
using System;
using System.Collections.Generic;

namespace ModuleSystem
{
	public class ModuleProcessor : IDisposable
	{
		#region Events

		public delegate void ModuleActionHandler(ModuleAction moduleAction, ModuleProcessor processor);
		public delegate void ModuleActionRequestHandler(ModuleActionRequest moduleActionRequest, ModuleProcessor processor);
		public event ModuleActionHandler ActionProcessedEvent;
		public event ModuleActionHandler ActionStackProcessedEvent;
		public event ModuleActionRequestHandler ActionRequestProcessedEvent;

		#endregion

		#region Variables

		public readonly string UniqueIdentifier;

		private bool _isProcessing = false;
		private IModule _lockingModule = null;
		private ModuleAction _lockingModuleAction = null;
		private int _preProcessActionChainCount;

		private ModuleActionRequest _initialActionRequest = null;
		private Stack<ModuleAction> _executionStack = new Stack<ModuleAction>();
		private Queue<ModuleActionRequest> _actionRequests = new Queue<ModuleActionRequest>();

		private List<IModule> _modules;

		private bool _started = false;

		#endregion

		#region Properties

		public IReadOnlyList<IModule> Modules => _modules;

		public bool IsProcessing => _isProcessing || _lockingModule != null;

		public bool IsPaused
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

		public bool HasActionRequest(Predicate<ModuleActionRequest> predicate)
		{
			foreach (var item in _actionRequests)
			{
				if (predicate(item))
				{
					return true;
				}
			}
			return false;
		}

		public bool HasActionRequest(Predicate<ModuleAction> predicate)
		{
			foreach (var item in _actionRequests)
			{
				if (predicate(item.ModuleAction))
				{
					return true;
				}
			}
			return false;
		}

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
			EnqueueAction(new ModuleActionRequest(action));
		}

		public void EnqueueAction(ModuleActionRequest action)
		{
			_actionRequests.Enqueue(action);
			TryProcessStack();
		}

		public void SetPaused(bool isPaused)
		{
			if (IsPaused != isPaused)
			{
				IsPaused = isPaused;
				if (!IsPaused)
				{
					TryProcessStack();
				}
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

				if (_lockingModuleAction != null &&
					_preProcessActionChainCount != _lockingModuleAction.ChainedCount)
				{
					_preProcessActionChainCount = _lockingModuleAction.ChainedCount;
					ChainActions(_lockingModuleAction);
					_lockingModuleAction = null;
				}

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

			_actionRequests.Clear();
			_executionStack.Clear();

			_lockingModule = null;
			_lockingModuleAction = null;
			_initialActionRequest = null;
			_isProcessing = false;
			_started = false;
		}

		#endregion

		#region Private Methods

		private void TryProcessStack()
		{
			if (!_started || IsProcessing)
			{
				return;
			}

			_isProcessing = true;

			// If the stack is empty, but the queue is not, then place the first of the queue on top of the stack
			TrySetNextActionRequest();

			// Stack Resolve Loop
			while (_executionStack.Count > 0)
			{
				ModuleAction action = _executionStack.Peek();
				_preProcessActionChainCount = action.ChainedCount;

				for (int i = 0; i < _modules.Count; i++)
				{
					IModule module = _modules[i];
					_lockingModule = module;

					if (module.AllowMultiProcessing || !action.IsProcessedByModule(module))
					{
						// Processing Callback
						if (action is CallbackModuleAction callbackModule && callbackModule.ModuleSource.UniqueIdentifier == module.UniqueIdentifier)
						{
							callbackModule.MarkProcessedByModule(module);
							callbackModule?.ModuleCallback(callbackModule);
							break;
						}

						// Processing Module
						else if (module.TryProcess(action, ()=> 
						{ 
							Unlock(module); 
						}))
						{
							action.MarkProcessedByModule(module);
							if (_lockingModule != null)
							{
								_lockingModuleAction = action;
								_isProcessing = false;
								return;
							}
							else
							{
								// If the processing caused the 
								if (_preProcessActionChainCount != action.ChainedCount)
								{
									_preProcessActionChainCount = action.ChainedCount;
									ChainActions(action);

									// If a new actions are on the stack, process those before finishing the processing of the source
									if (_executionStack.Peek().UniqueIdentifier != action.UniqueIdentifier)
									{
										break;
									}
								}

								// The action has been processed and can have a different state. Thus all modules should again be handed it to see if they wish to react to it.
								i = -1;
								continue;
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

				// Remove action from Stack, for it is fully processed
				_executionStack.Pop();
				_lockingModuleAction = null;

				ActionProcessedEvent?.Invoke(action, this);

				// If the Stack is completely resolved
				if (_executionStack.Count == 0)
				{
					if (_initialActionRequest != null)
					{
						ModuleActionRequest request = _initialActionRequest;
						_initialActionRequest = null;

						for (int i = 0; i < _modules.Count; i++)
						{
							_modules[i].OnResolvedStack(request.ModuleAction);
						}

						for (int i = 0; i < _modules.Count; i++)
						{
							_modules[i].OnResolvedRequest(request);
						}

						request.MarkAsProcessed();

						ActionStackProcessedEvent?.Invoke(request.ModuleAction, this);
						ActionRequestProcessedEvent?.Invoke(request, this);
					}
				}

				// Process next in queue, causing the next stack flow on the execution stack
				TrySetNextActionRequest();
			}

			_isProcessing = false;
		}

		private void ChainActions(ModuleAction source)
		{
			// Stack Chain Actions after source is processed completely
			for (int i = source.ChainedActions.Count - 1; i >= 0; i--)
			{
				ModuleAction chainedAction = source.ChainedActions[i];
				if (!chainedAction.IsChainedByProcessor(this))
				{
					_executionStack.Push(chainedAction);
					chainedAction.MarkChainedByProcessor(this);
				}
			}
		}

		private void TrySetNextActionRequest()
		{
			if (_executionStack.Count == 0 && _actionRequests.Count > 0 && !IsPaused)
			{
				ModuleActionRequest req = _actionRequests.Dequeue();
				_initialActionRequest = req;
				_executionStack.Push(req.ModuleAction);
			}
		}

		#endregion
	}
}