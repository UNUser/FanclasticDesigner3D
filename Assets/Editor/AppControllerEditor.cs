using Assets.Scripts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Assets.Editor
{

    /// <summary>
    /// Перед билдом приложения или по нажатию на кнопку "Update Models Sets" на основе содержимого
    /// директории DemoModels (лежит в корне проекта) обновляется содержимое директории Assets/Resources/ModelsSets/
    /// - генерируется файл ModelsSetsTree.json, задающий содержимое окна с наборами моделей
    /// - файлы всех моделей копируются в Assets/Resources/ModelsSets/ModelsFiles/
    /// - перед копированием из файлов удаляется форматирование для уменьшения размера
    /// - при копировании при совпадении имен файлов проверяется совпадение их содержимого
    /// </summary>
    [CustomEditor(typeof(AppController))]
    public class AppControllerEditor : UnityEditor.Editor, IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; }}
        public void OnPreprocessBuild(BuildReport report)
        {
            UpdateModelsSets();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Update Models Sets")) {
                UpdateModelsSets();
            }
        }

        public void UpdateModelsSets()
        {
            var projectPath = Directory.GetParent(Application.dataPath).FullName;
            var setsModelsSourcePath = Path.Combine(projectPath, "DemoModels");
            var sourceFilesFullPathList = Directory.GetFiles(setsModelsSourcePath, "*.fcl", SearchOption.AllDirectories);
            var sourceFilesRelativePathList = sourceFilesFullPathList
                .Select(s => s.Remove(0, setsModelsSourcePath.Length + 1).Replace('\\', '/'))
                .ToArray();

            UpdateModelsFiles(sourceFilesFullPathList);
            UpdateModelsSetsTreeFile(sourceFilesRelativePathList);

            Debug.Log("Models sets files updated!");
        }

        private void UpdateModelsFiles(string[] sourceFilesFullPathList) {
            var destinationDirectoryPath = Path.Combine(Application.dataPath, "Resources/ModelsSets/ModelsFiles");
            var copiedFiles = new Dictionary<string, string>();

            Directory.Delete(destinationDirectoryPath, true);
            Directory.CreateDirectory(destinationDirectoryPath);

            foreach (var sourceFilePath in sourceFilesFullPathList) {
                var fileName = Path.GetFileNameWithoutExtension(sourceFilePath);

                var fileContent = File.ReadAllText(sourceFilePath);

                var destinationFilePath = Path.Combine(destinationDirectoryPath, fileName + ".json");
                var fileMinifiedContent = MinifyJsonString(fileContent);

                if (copiedFiles.ContainsKey(fileName)) {
                    var prevFileMinifiedContent = copiedFiles[fileName];

                    if (prevFileMinifiedContent != fileMinifiedContent) {
                        Debug.LogError("Same file name but different content: " + sourceFilePath);
                    }

                    continue;
                }

                File.WriteAllText(destinationFilePath, fileMinifiedContent);

                copiedFiles.Add(fileName, fileMinifiedContent);
            }
        }

        private void AddFileToSetsTree(SetsTreeCategory setsTree, string fileRelativePath) {
            var fileName = Path.GetFileNameWithoutExtension(fileRelativePath);
            var directoryPath = Path.GetDirectoryName(fileRelativePath).Replace("\\", "/");
            var directoriesInPath = directoryPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            var currentDirectory = setsTree;

            foreach (var directoryName in directoriesInPath) {
                var directoryMatchPredicate = new Predicate<SetsTreeItem>(
                    item => item is SetsTreeCategory &&
                    item.Name == directoryName);
                var directoryIndex = currentDirectory.Content.FindIndex(directoryMatchPredicate);

                if (directoryIndex < 0) {
                    currentDirectory.Content.Add(new SetsTreeCategory { Name = directoryName });
                    directoryIndex = currentDirectory.Content.Count - 1;
                }

                currentDirectory = currentDirectory.Content[directoryIndex] as SetsTreeCategory;
            }

            currentDirectory.Content.Add(new SetsTreeModel { Name = fileName });
        }

        private void UpdateModelsSetsTreeFile(string[] filesRelativePathList) {
            var setsTree = new SetsTreeCategory { Name = "ModelsSets" };

            Array.Sort(filesRelativePathList);

            foreach (var filePath in filesRelativePathList) {
                AddFileToSetsTree(setsTree, filePath);
            }

            var setsTreeFilePath = Path.Combine(Application.dataPath, "Resources/ModelsSets/ModelsSetsTree.json");
            var settings = new JsonSerializerSettings {
                TypeNameHandling = TypeNameHandling.Auto
            };
            var setsTreeJson = JsonConvert.SerializeObject(setsTree, Formatting.Indented, settings);

            File.WriteAllText(setsTreeFilePath, setsTreeJson);
        }

        private string MinifyJsonString(string json) {
            using (var stringReader = new StringReader(json))
            using (var stringWriter = new StringWriter()) {
                using (JsonReader jsonReader = new JsonTextReader(stringReader))
                using (JsonWriter jsonWriter = new JsonTextWriter(stringWriter)) {
                    jsonWriter.Formatting = Formatting.None;
                    jsonWriter.WriteToken(jsonReader);
                }
                return stringWriter.ToString();
            }
        }
    }
}
