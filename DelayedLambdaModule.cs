using System;

namespace ModuleSystem
{
	public class DelayedLambdaModule : DelayedModuleBase
	{
		public delegate bool ModuleHandler(ModuleAction action, Action unlockMethod, ModuleProcessor parent);

		private ModuleHandler _handler;

		public DelayedLambdaModule(ModuleHandler handler)
		{
			_handler = handler;
		}

		protected override bool TryProcessInternal(ModuleAction action, Action unlockMethod)
		{
			return _handler(action, unlockMethod, Processor);
		}

		public override void Deinit()
		{
			_handler = null;
			base.Deinit();
		}
	}
}