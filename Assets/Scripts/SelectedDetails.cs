using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Assets.Scripts 
{
    public class SelectedDetails 
    {
		private readonly HashSet<Detail> _details = new HashSet<Detail>();

	    public bool IsSelected(Detail detail)
	    {
		    return _details.Contains(detail);
	    }

	    public void Add(Detail detail)
	    {
		    _details.Add(detail);
	    }

	    public void Remove(Detail detail)
	    {
		    _details.Remove(detail);
	    }

	    public void Clear()
	    {
			// TODO update links
		    _details.Clear();
	    }

	    public void Rotate(Vector3 axis)
	    {
		    if (!_details.Any()) {
			    return;
		    }


	    }

		// отсоединяться может только связанная группа деталей
	    public void Detach()
	    {
			if (!_details.Any()) {
				return;
			}

		    var first = _details.First();
			var targetGroup = first.Group;

		    if (targetGroup == null) {
			    return;
		    }

			targetGroup.Detach(_details);
	    }

		public void Remove() 
		{
			if (!_details.Any()) {
				return;
			}

			Detach();
			Destroy(SelectedDetails.gameObject);
		}
    }
}
