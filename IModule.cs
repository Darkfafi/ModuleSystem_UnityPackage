using System;

namespace ModuleSystem.Core
{
	public interface IModule
	{
		string UniqueIdentifier
		{
			get;
		}

		bool AllowMultiProcessing
		{
			get;
		}

		bool TryProcess(ModuleAction action, Action unlockMethod);
		void OnResolvedStack(ModuleAction coreAction);
		void Init(ModuleProcessor parent);
		void StartModule();
		void Deinit();
		void OnResolvedRequest(ModuleActionRequest request);
	}
}