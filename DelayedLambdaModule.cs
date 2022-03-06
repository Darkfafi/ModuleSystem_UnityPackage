using System;

namespace ModuleSystem
{
	public class DelayedLambdaModule : DelayedModuleBase
	{
		public delegate bool ModuleHandler(ModuleAction action, Action unlockMethod, ModuleProcessor parent);

		private ModuleHandler _handler;
		private bool _multiProcessing = false;

		public override bool AllowMultiProcessing => _multiProcessing;


		public DelayedLambdaModule(ModuleHandler handler, bool multiProcessing = false)
		{
			_handler = handler;
			_multiProcessing = multiProcessing;
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

	public class DelayedLambdaModule<T> : DelayedModuleBase<T>
		where T : ModuleAction
	{
		public delegate bool ModuleHandler(T action, Action unlockMethod, ModuleProcessor parent);

		private ModuleHandler _handler;
		private bool _multiProcessing = false;

		public override bool AllowMultiProcessing => _multiProcessing;


		public DelayedLambdaModule(ModuleHandler handler, bool multiProcessing = false)
		{
			_handler = handler;
			_multiProcessing = multiProcessing;
		}

		protected override bool TryProcessInternal(T action, Action unlockMethod)
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