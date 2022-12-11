using UnityEngine;

namespace JMor.AimAssist
{
	//[CreateAssetMenu(fileName = "mm_newMutatorMode", menuName = "Scriptable Object/MutatorMode")]
	public abstract class MutatorMode : ScriptableObject
	{
		public abstract Vector2[] MutateInputRange(float input, float output);
	}
}