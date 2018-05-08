using UnityEngine;

namespace Assets.Scripts 
{
	public class AxisMover : MonoBehaviour
	{
		public GameObject Root;

		public void UpdatePos()
		{
			
		}

		protected void Update()
		{
			Root.transform.rotation = Quaternion.identity;
		}
	}
}
