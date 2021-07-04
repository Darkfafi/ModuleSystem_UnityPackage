namespace ModuleSystem
{
	public class BasicLambdaModule : BasicModuleBase
	{
		public delegate bool ModuleHandler(ModuleAction action, ModuleProcessor parent);

		private ModuleHandler _handler;

		public BasicLambdaModule(ModuleHandler handler)
		{
			_handler = handler;
		}

		protected override bool TryProcessInternal(ModuleAction action)
		{
			return _handler(action, Processor);
		}

		public override void Deinit()
		{
			_handler = null;
			base.Deinit();
		}
	}

	public class BasicLambdaModule<T> : BasicModuleBase<T> where T : ModuleAction
	{
		public delegate bool ModuleHandler(T action, ModuleProcessor parent);

		private ModuleHandler _handler;

		public BasicLambdaModule(ModuleHandler handler)
		{
			_handler = handler;
		}

		protected override bool TryProcessInternal(T action)
		{
			return _handler(action, Processor);
		}

		public override void Deinit()
		{
			_handler = null;
			base.Deinit();
		}
	}
}