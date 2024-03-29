﻿using ModuleSystem.Core;
using System;
using UnityEngine;

namespace ModuleSystem
{
	public abstract class DelayedModuleBehaviourBase : ModuleBehaviourBase
	{
		public override bool TryProcess(ModuleAction action, Action unlockMethod)
		{
			return TryProcessInternal(action, unlockMethod);
		}

		protected abstract bool TryProcessInternal(ModuleAction action, Action unlockMethod);
	}

	public abstract class BasicModuleBehaviourBase : ModuleBehaviourBase
	{
		public override bool TryProcess(ModuleAction action, Action unlockMethod)
		{
			if (TryProcessInternal(action))
			{
				unlockMethod();
				return true;
			}
			return false;
		}

		protected abstract bool TryProcessInternal(ModuleAction action);
	}
}

namespace ModuleSystem.Core
{
	public abstract class ModuleBehaviourBase : MonoBehaviour, IModule
	{
		#region Properties

		public ModuleProcessor Processor
		{
			get; private set;
		}

		public virtual bool AllowMultiProcessing => false;

#if UNITY_EDITOR
		public string UniqueIdentifier
		{
			get
			{
				if (string.IsNullOrEmpty(_uniqueIdentifierCached))
				{
					_uniqueIdentifierCached = string.Concat($" [{GetType().Name}]: ModuleBehaviour-UUID:", GetHashCode());
				}
				return _uniqueIdentifierCached;
			}
		}
		private string _uniqueIdentifierCached = null;
#else
		public string UniqueIdentifier => GetHashCode().ToString();
#endif

		public bool IsLocking => Processor != null && Processor.IsLockingModule(this);


		#endregion

		#region Public Methods

		public virtual void Init(ModuleProcessor parent)
		{
			Processor = parent;
		}

		public virtual void StartModule()
		{

		}

		public virtual void Deinit()
		{
			Processor = null;
#if UNITY_EDITOR
			_uniqueIdentifierCached = null;
#endif
		}

		public virtual void OnResolvedStack(ModuleAction coreAction)
		{

		}

		public virtual void OnResolvedRequest(ModuleActionRequest request)
		{

		}

		public abstract bool TryProcess(ModuleAction action, Action unlockMethod);

		#endregion
	}
}
