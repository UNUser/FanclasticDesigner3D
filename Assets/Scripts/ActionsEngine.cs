
using UnityEngine;

namespace Assets.Scripts {

	public class ActionsEngine {

		public void Select(DetailBase detail)
		{
			AppController.Instance.SelectedDetails.Clear();
			AppController.Instance.SelectedDetails.Add(detail);
		}

		public void Move(Vector3 offset)
		{
			AppController.Instance.SelectedDetails.Move(offset);
		}

		public void SetColor(Material color)
		{
			AppController.Instance.SelectedDetails.SetColor(color);
		}

		public void CreateDetail()
		{
			
		}

		public void Delete() 
		{

		}

		public void HideDetail()
		{
			AppController.Instance.SelectedDetails.Hide();
		}

		public void ShowDetail()
		{
			AppController.Instance.SelectedDetails.Show();
		}

	}
}
