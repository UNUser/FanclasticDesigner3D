﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts {

    public class ActionsLog : MonoBehaviour
    {

        public GameObject UndoButton;
        public GameObject RedoButton;


        public void Clear()
        {
            _history.Clear();
            _actionIndex = 0;
            AppController.Instance.SelectedDetails.Clear();
            UndoButton.SetActive(false);
            RedoButton.SetActive(false);
        }

        public List<InstructionBase> GetInstructions(out HashSet<Detail> invalidDetails)
        {
            var instructions = new List<InstructionBase>();
            var actionIndex = _actionIndex;

            invalidDetails = new HashSet<Detail>();

            while (--actionIndex >= 0)
            {
                var nextInstruction = GetInstruction(ref actionIndex, invalidDetails);

                if (nextInstruction == null) {
                    continue;
                }

                instructions.Add(nextInstruction);
            }

            instructions.Reverse();

            return instructions;
        }

        //TODO наверняка можно как-то сделать через yield
        private InstructionBase GetInstruction(ref int actionIndex, HashSet<Detail> invalidDetails)
        {
            var isDeleteInstruction = false;
            var noChanges = true;
            var resultRotation = Quaternion.identity;
            var rotationPivot = Vector3.zero;
            var resultOffset = Vector3.zero;

            // Тут неявно предполагается, что все повороты происходят вокруг одной и той же точки относительно
            // выделенных деталей (это центр общего BoundingBox'a, мешей этих деталей). За счет этого, вращения
            // и перемещения деталей можно просуммировать и сжать все промежуточные действия в одну инструкцию
            // (суммарное вращение в исходной точке и последующее суммарное перемещение).
            while (actionIndex >= 0 && _history[actionIndex].Type != ActionType.Selection
                                    && _history[actionIndex].Type != ActionType.Creation)
            {
                var currentAction = _history[actionIndex];

                actionIndex--;

                if (isDeleteInstruction) {
                    continue;
                }

                switch (currentAction.Type) {
                    case ActionType.Deleting:
                        isDeleteInstruction = true;
                        break;

                    case ActionType.Movement:
                        var moveAction = (MoveAction) currentAction;

                        resultOffset += moveAction.Offset;
                        // Поскольку в инструкциях мы сперва применям вращение, а потом перемещение, то при суммировании действий мы все вращения
                        // должны выполнить в исходной точке выделения деталей (до начала перемещений). Соответственно, если до первого вращения
                        // были какие-то перемещения, то их нужно вычесть из точки вращения, чтобы получить ее положение в момент выделения деталей.
                        if (resultRotation != Quaternion.identity) {
                            rotationPivot -= moveAction.Offset;
                        }
                        break;

                    case ActionType.Rotation:
                        var rotateAction = (RotateAction) currentAction;

                        resultRotation *= rotateAction.RotationDelta;
                        resultOffset += rotateAction.Alignment;
                        // Тут нам нужно исходное положение точки вращения, которое у нее было в момент выделения деталей.
                        // Если до первого вращения не было перемещений, то это будет просто Pivot первого вращения.
                        // Если были, то учтем их позже при обработке ActionType.Movement.
                        rotationPivot = rotateAction.Pivot;
                        break;

                    case ActionType.Coloring:
                        continue;

                    default:
                        Debug.LogError("Unhandled action type: " + currentAction.Type);
                        continue;
                }

                noChanges = false;
            }

            var targetDetails = new HashSet<Detail>();
            var initialAction = _history[actionIndex];

            if (noChanges && initialAction.Type != ActionType.Creation) {
                return null;
            }

            switch (initialAction.Type)
            {
                case ActionType.Creation:
                    var createAction = (CreateAction) initialAction;

                    targetDetails.Add(createAction.Detail);
                    break;
                case ActionType.Selection:
                    var selectAction = (SelectAction) initialAction;

                    targetDetails.UnionWith(selectAction.SelectedDetails);
                    break;
                default:
                    Debug.LogError("Invalid initial action type: " + initialAction.Type);
                    break;
            }

            if (isDeleteInstruction) {
                if (initialAction.Type != ActionType.Creation) {
                    invalidDetails.UnionWith(targetDetails);
                }
                return null;
            }

            var isInvalidCreation = initialAction.Type == ActionType.Creation && targetDetails.IsSubsetOf(invalidDetails);

            // тут невалидная деталь впервые появилась на сцене и в предыдущих действиях ее быть не может
            // поэтому для оптимизации удаляем ее из списка невалидных деталей
            if (isInvalidCreation) {
                invalidDetails.ExceptWith(targetDetails);
                return null;
            }

            var isSingleDetailAction = targetDetails.Count == 1;

            targetDetails.ExceptWith(invalidDetails);

            if (!targetDetails.Any()) {
                return null;
            }

            // обработка операций с единичными деталями
            if (isSingleDetailAction)
            {
                var addInstruction = new InstructionAdd();
                DetailData sourceState;
                var selectAction = initialAction as SelectAction;

                if (selectAction != null)
                {
                    sourceState = selectAction.SourceState;

                    if (sourceState == null) {
                        Debug.LogError("Source state for selected detail is null!");
                        return null;
                    }

                    invalidDetails.Add(targetDetails.First());
                } else if (initialAction is CreateAction) {
                    sourceState = ((CreateAction) initialAction).SourceState;
                } else {
                    Debug.LogError("Invalid initial action: " + initialAction.Type);
                    return null;
                }

                addInstruction.TargetDetails = new HashSet<int>{ sourceState.Id };
                // Для некоторых деталей их геометрический центр, который всегда берется за центральную точку всех вращений,
                // не совпадает с локальным нулем модели. Поэтому чтобы понять, какая итоговая позиция будет у детали, нужно
                // применить к ее исходной позиции все суммарные вращения и перемещения
                addInstruction.Position = Extentions.RotateAndTranslatePoint(sourceState.Position, rotationPivot, resultRotation, resultOffset);
                addInstruction.Rotation = (resultRotation * Quaternion.Euler(sourceState.Rotation)).eulerAngles;

                return addInstruction;
            }

            // далее обработка групповых операций

            // убираем бессмысленные повороты и перемещения
            var resultRotationEuler = resultRotation.eulerAngles;
            var normalizedRotation = (SerializableVector3Half) resultRotationEuler; //TODO определить какой-то эпсилон для вращений
            var normalizedOffset = resultOffset.magnitude < 0.001f ? Vector3.zero : resultOffset;

            if (normalizedOffset  == Vector3.zero && normalizedRotation == Vector3.zero) {
                return null;
            }

            var moveAndRotateInstruction = new InstructionMoveAndRotate();

            moveAndRotateInstruction.TargetDetails = new HashSet<int>(targetDetails.Select(detail => detail.GetInstanceID()));
            moveAndRotateInstruction.Rotation = (Vector3) normalizedRotation;
            moveAndRotateInstruction.Pivot = normalizedRotation == Vector3.zero ? Vector3.zero : rotationPivot;
            moveAndRotateInstruction.Offset = normalizedOffset;

            return moveAndRotateInstruction;
        }

        private readonly List<ActionBase> _history = new List<ActionBase>();
        private int _actionIndex;

        public void Start()
        {
            UndoButton.SetActive(false);
            RedoButton.SetActive(false);
        }

        public void RegisterAction(ActionBase action)
        {
            var tailLength = _history.Count - _actionIndex;

            if (tailLength > 0) {
                for (var i = _actionIndex; i < _history.Count; i++) {
                    var actionCreate = _history[i] as CreateAction;

                    if (actionCreate == null) continue;

                    Destroy(actionCreate.Detail.gameObject);
                }
                _history.RemoveRange(_actionIndex, tailLength);
            }

            _history.Add(action);
            _actionIndex++;

            RedoButton.SetActive(false);
            UndoButton.SetActive(true);

            Debug.Log("Registered: " + action.GetType() + " " + _actionIndex + "/" + _history.Count);
        }

        public void OnUndoButtonClicked()
        {
            _actionIndex--;
            _history[_actionIndex].Undo();

            UndoButton.SetActive(_actionIndex > 0);
            RedoButton.SetActive(true);

            Debug.Log("Undo: " + _actionIndex + "/" + _history.Count);
        }

        public void OnRedoButtonClicked()
        {
            _history[_actionIndex].Do();
            _actionIndex++;

            RedoButton.SetActive(_actionIndex < _history.Count);
            UndoButton.SetActive(true);

            Debug.Log("Redo: " + _actionIndex + "/" + _history.Count);
        }

    }
}
