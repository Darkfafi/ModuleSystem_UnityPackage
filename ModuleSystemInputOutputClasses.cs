using System;

namespace ModuleSystem
{
	public interface IHaveModuleProcessor
	{
		ModuleProcessor Processor
		{
			get;
		}
	}

	[AttributeUsage(validOn: AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	public class ModuleActionInputAttribute : Attribute
	{
		public Type ActionType
		{
			get; private set;
		}

		public ModuleActionInputAttribute(Type actionType)
		{
			ActionType = actionType;
		}
	}

	[AttributeUsage(validOn: AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	public class ModuleActionOutputAttribute : Attribute
	{
		public Type ActionType
		{
			get; private set;
		}

		public ModuleActionOutputAttribute(Type actionType)
		{
			ActionType = actionType;
		}
	}
}