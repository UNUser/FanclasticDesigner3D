using System;
using Assets.Scripts;
using UnityEngine;
using UnityEngine.UI;


public class TutorialLayer : MonoBehaviour
{

	public Transform HintsContainer;

	public Image ZoomImage;
	public Text ZoomText;

	public Image LinkedSelectionImage;
//	public Text LinkedSelectionText;

	public Sprite PinchZoomImage;
	public Sprite MouseZoomImage;

	public Sprite TapSelectionImage;
	public Sprite ClickSelectionImage;

	public GameObject SetColorButton;

	private bool _setColorButtonState;

	private int ActiveHint
	{
		get { return _activeHint; }
		set
		{
			if (_activeHint == value) {
				return;
			}

			var prevHint = HintsContainer.GetChild(_activeHint).gameObject;
			var nextHint = HintsContainer.GetChild(value).gameObject;

			prevHint.SetActive(false);
			nextHint.SetActive(true);

			_activeHint = value;
		}
	}

	private int _activeHint;

	protected void Start()
	{
		var isMobile = Application.isMobilePlatform;

		ZoomImage.sprite = isMobile ? PinchZoomImage : MouseZoomImage;
		LinkedSelectionImage.sprite = isMobile ? TapSelectionImage : ClickSelectionImage;

//		LinkedSelectionText.text = ;
	}

	protected void OnEnable()
	{
		ActiveHint = 0;

		_setColorButtonState = SetColorButton.activeSelf;
		SetColorButton.SetActive(true);

		ZoomText.text = string.Format("TutorialLayer.ZoomHint.Base".Lang(),
			(Application.isMobilePlatform ? "TutorialLayer.ZoomHint.Pinch" : "TutorialLayer.ZoomHint.Wheel").Lang());
	}

	protected void OnDisable()
	{
		SetColorButton.SetActive(_setColorButtonState);
	}

	public void OnNextButtonClicked()
	{
		if (ActiveHint == HintsContainer.childCount - 1) {
			gameObject.SetActive(false);
			AppController.Instance.OnTutorialEnd();

			return;
		}

		++ActiveHint;
	}
}
