﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts 
{

	public class InstructionsLayer : MonoBehaviour
	{
		public Slider Slider;
		public GameObject PrevInstructionButton;
		public GameObject NextInstructionButton;
		public Text StepNumberText;

		private int StepNumber
		{
			get { return _stepNumber; }
			set
			{
				StepNumberText.text = string.Format("{0} / {1}", value, _instructions.Count);

				PrevInstructionButton.SetActive(value > 1);
				NextInstructionButton.SetActive(value < _instructions.Count);

				Slider.value = value;

				if (_stepNumber == value) {
					return;
				}

				if (_stepNumber < value) {
					for (_stepNumber += 1; _stepNumber <= value; _stepNumber++) {
						_instructions[_stepNumber - 1].Do();
					}
				} else {
					for (; _stepNumber > value; _stepNumber--) {
						_instructions[_stepNumber - 1].Undo();
					}
				}

				_stepNumber = value;
			}
		}

		private int _stepNumber;

		private List<InstructionBase> _instructions; 
		private Session _session;

		public void ShowInstructions(List<InstructionBase> instructions, Session session)
		{
			_session = session;
			_instructions = instructions;

			Slider.maxValue = instructions.Count;
			_stepNumber = Mathf.RoundToInt(Slider.minValue);
			StepNumber = _stepNumber;
			_instructions[0].Do();
		}

		public void OnSliderValueChanged(float value)
		{
			StepNumber = Mathf.RoundToInt(value);
		}

		public void OnNextButtonClicked()
		{
			StepNumber++;
		}

		public void OnPrevButtonClicked()
		{
			StepNumber--;
		}

		private Material _selectedMaterial;

		private void CreateLineMaterial() {
			if (!_selectedMaterial) {
				// Unity has a built-in shader that is useful for drawing
				// simple colored things.
				var shader = Shader.Find("Hidden/Internal-Colored");
				_selectedMaterial = new Material(shader);
				_selectedMaterial.hideFlags = HideFlags.HideAndDontSave;
				// Turn on alpha blending
				_selectedMaterial.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
				_selectedMaterial.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				// Turn backface culling off
				_selectedMaterial.SetInt("_Cull", (int) UnityEngine.Rendering.CullMode.Off);
				// Turn off depth writes
				_selectedMaterial.SetInt("_ZWrite", 0);
			}
		}

		private void OnRenderObject()
		{
			var targetDetails = _instructions[_stepNumber - 1].TargetDetails;
			var combinedBounds = new Bounds();
			var firstDetail = _session.GetDetail(targetDetails.First());

			combinedBounds.center = firstDetail.transform.position;
			foreach (var id in targetDetails)
			{
				var detail = _session.GetDetail(id);

				combinedBounds.Encapsulate(detail.Bounds);
			}

			CreateLineMaterial();
			combinedBounds.DrawGL(_selectedMaterial);

		}
	}
}
