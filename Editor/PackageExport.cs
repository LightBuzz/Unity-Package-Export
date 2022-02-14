using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace LightBuzz.Package
{
    /// <summary>
    /// Displays a Unity Editor window where you can select the package to export.
    /// </summary>
    public class PackageExport : EditorWindow
    {
        [SerializeField] private string _sourceFolder = string.Empty;
        [SerializeField] private string _info = string.Empty;
        [SerializeField] private Vector2 _scrollPosition = Vector2.zero;

        private readonly List<PackageInfo> _packages = new List<PackageInfo>();

        private PackageInfo _selection;
        private ListRequest _listRequest;
        private PackRequest _packRequest;
        private string _destinationFolder = string.Empty;
        private bool _isWorking = false;

        [MenuItem("LightBuzz/Export Package", false, -100)]
        private static void MenuItem_Export()
        {
            GetWindow<PackageExport>(false, "Package Export", true);
        }

        private void Awake()
        {
            EditorApplication.update += OnEditorUpdate;

            _listRequest = Client.List();
        }

        private void OnDestroy()
        {
            _isWorking = false;
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            if (_packages.Count == 0)
            {
                EditorGUILayout.LabelField("Searching for packages...");
                return;
            }

            try
            {
                EditorGUILayout.LabelField("Available packages");

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, false, true, GUILayout.Height(200));

                foreach (PackageInfo package in _packages)
                {
                    if (GUILayout.Button(package.displayName))
                    {
                        _selection = package;
                        _sourceFolder = _selection.resolvedPath;
                    }
                }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(10);

                EditorGUILayout.LabelField("Package path");

                EditorGUILayout.TextField(_sourceFolder);

                if (GUILayout.Button("Export", GUILayout.Height(30.0f)))
                {
                    if (!_isWorking)
                    {
                        _destinationFolder = EditorUtility.OpenFolderPanel
                        (
                            "Select where to export the package", 
                            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                            string.Empty
                        );

                        _isWorking = true;
                        _info = "Exporting package...";

                        Export();
                    }
                }

                EditorGUILayout.LabelField(_info);
            }
            catch
            {
            }
        }

        private void Export()
        {
            if (string.IsNullOrWhiteSpace(_sourceFolder))
            {
                _info = "Package path is empty";
                return;
            }
            if (string.IsNullOrWhiteSpace(_destinationFolder))
            {
                _info = "Destination folder is empty";
                return;
            }
            if (!Directory.Exists(_sourceFolder))
            {
                _info = "Package folder does not exist";
                return;
            }
            if (!Directory.Exists(_destinationFolder))
            {
                _info = "Destination folder does not exist";
                return;
            }

            _packRequest = Client.Pack(_sourceFolder, _destinationFolder);
        }

        private void OnEditorUpdate()
        {
            ListPackages();
            PackPackage();            
        }

        private void ListPackages()
        {
            if (_listRequest != null && _listRequest.IsCompleted)
            {
                if (_listRequest.Status == StatusCode.Success)
                {
                    _packages.Clear();

                    foreach (var package in _listRequest.Result)
                    {
                        _packages.Add(package);
                    }
                }

                _listRequest = null;
            }
        }

        private async void PackPackage()
        {
            if (_packRequest != null && _packRequest.IsCompleted)
            {
                if (_packRequest.Status == StatusCode.Success)
                {
                    _info = "Package exported!";

                    await Task.Delay(300);

                    System.Diagnostics.Process.Start(_destinationFolder);

                    _isWorking = false;
                }
                else
                {
                    _info = "There was an error exporting the package.";
                }

                _packRequest = null;
            }
        }
    }
}
