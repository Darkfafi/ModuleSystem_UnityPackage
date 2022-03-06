using System;

namespace ModuleSystem
{
	public class ModuleActionRequest<T> : ModuleActionRequest
		where T : ModuleAction
	{
		public readonly T TModuleAction;

		public ModuleActionRequest(T moduleAction) 
			: base(moduleAction)
		{
			TModuleAction = moduleAction;
		}
	}

	public class ModuleActionRequest : IDisposable
	{
		public readonly ModuleAction ModuleAction;
		private Action _onProcessedCallbacks = null;

		public bool IsProcessed
		{
			get; private set;
		}

		public ModuleActionRequest(ModuleAction moduleAction)
		{
			ModuleAction = moduleAction;
		}

		public void OnProcessed(Action callback)
		{
			if(IsProcessed)
			{
				callback?.Invoke();
			}
			else
			{
				_onProcessedCallbacks += callback;
			}
		}

		internal void MarkAsProcessed()
		{
			if(!IsProcessed)
			{
				IsProcessed = true;
				_onProcessedCallbacks?.Invoke();
				Dispose();
			}
		}

		public void Dispose()
		{
			_onProcessedCallbacks = null;
		}
	}
}