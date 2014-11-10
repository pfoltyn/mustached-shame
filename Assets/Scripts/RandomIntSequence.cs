using UnityEngine;

namespace AssemblyCSharp
{
	public class RandomIntSequence
	{
		readonly int maxValue;
		int previousValue;

		public RandomIntSequence(int maxPlusOneValue)
		{
			this.maxValue = maxPlusOneValue;
			previousValue = 0;
		}

		public int Next() {
			int nextValue = Random.Range(0, maxValue);
			if (nextValue == previousValue) {
				nextValue = (nextValue + 1) % maxValue;
			}
			previousValue = nextValue;
			return nextValue;
		}
	}
}
