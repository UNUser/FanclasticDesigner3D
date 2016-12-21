using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


namespace Assets.Scripts {
    public interface IMovable : IBeginDragHandler, IDragHandler, IPointerClickHandler
    {
        bool IsSelected { get; set; }
        DetailsGroup Parent { get; set; }

        List<Transform> GetConnections(Vector3? pos = null);
    }
}
