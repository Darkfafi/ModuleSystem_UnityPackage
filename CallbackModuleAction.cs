using ModuleSystem.Core;

namespace ModuleSystem
{
	public class CallbackModuleAction : ModuleAction
	{
		public delegate void CallbackHandler(CallbackModuleAction sourceAction);

		public readonly IModule ModuleSource;
		public readonly CallbackHandler ModuleCallback;

		public CallbackModuleAction(IModule source, CallbackHandler callback)
		{
			ModuleSource = source;
			ModuleCallback = callback;
		}
	}
}