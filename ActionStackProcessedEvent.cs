using System;
using UnityEngine;

namespace ModuleSystem.Core
{
	[CreateAssetMenu(menuName = "ModuleSystem/" + nameof(ActionStackProcessedEvent), fileName = nameof(ActionStackProcessedEvent))]
	public class ActionStackProcessedEvent : ScriptableObject
	{
		private Action<ModuleAction> _callback;

		public void Emit(ModuleAction coreAction)
		{
			_callback?.Invoke(coreAction);
		}

		public void AddListener(Action<ModuleAction> method)
		{
			_callback += method;
		}

		public void RemoveListener(Action<ModuleAction> method)
		{
			_callback -= method;
		}
	}
}