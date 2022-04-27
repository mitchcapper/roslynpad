﻿using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoslynPad.UI
{
    [Export(typeof(IApplicationSettings)), Shared]
    internal class ApplicationSettings : IApplicationSettings
    {
        private const string DefaultConfigFileName = "RoslynPad.json";

        private static readonly JsonSerializerOptions s_serializerOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private readonly ITelemetryProvider? _telemetryProvider;
        private SerializableValues _values;
        private string? _path;

        [ImportingConstructor]
        public ApplicationSettings([Import(AllowDefault = true)] ITelemetryProvider telemetryProvider)
        {
            _telemetryProvider = telemetryProvider;
            _values = new SerializableValues();
            InitializeValues();
        }

        private void InitializeValues()
        {
            _values.PropertyChanged += (_, _) => SaveSettings();
            _values.Settings = this;
        }

        public void LoadDefault() =>
            LoadFrom(Path.Combine(GetDefaultDocumentPath(), DefaultConfigFileName));

        public void LoadFrom(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            LoadSettings(path);

            _path = path;
        }

        public IApplicationSettingsValues Values => _values;

        public string GetDefaultDocumentPath()
        {
            string? documentsPath;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            else // Unix or Mac
            {
                documentsPath = Environment.GetEnvironmentVariable("HOME");
            }

            if (string.IsNullOrEmpty(documentsPath))
            {
                documentsPath = "/";
                _telemetryProvider?.ReportError(new InvalidOperationException("Unable to locate the user documents folder; Using root"));
            }

            return Path.Combine(documentsPath, "RoslynPad");
        }

        private void LoadSettings(string path)
        {
            if (!File.Exists(path))
            {
                _values.LoadDefaultSettings();
                return;
            }

            try
            {
                var json = File.ReadAllText(path);
                if (!haveAddedBoolConverter)
                {
                    haveAddedBoolConverter = true;
                    s_serializerOptions.Converters.Add(new AutoNumberBoolToStringConverter());
                }


                _values = JsonSerializer.Deserialize<SerializableValues>(json, s_serializerOptions) ?? new SerializableValues();
                InitializeValues();
            }
            catch (Exception e)
            {
                _values.LoadDefaultSettings();
                _telemetryProvider?.ReportError(e);
            }
        }
        private static bool haveAddedBoolConverter = false;
        public class AutoNumberBoolToStringConverter : JsonConverter<string>
        {
            public override bool CanConvert(Type typeToConvert)
            {
                return typeof(string) == typeToConvert;
            }
            public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.Number:
                        return reader.TryGetInt64(out long l) ?
                       l.ToString() :
                       reader.GetDouble().ToString();
                    case JsonTokenType.True:
                        return true.ToString();
                    case JsonTokenType.False:
                        return false.ToString();
                    case JsonTokenType.String:
                        return reader.GetString()!;
                    default:
                        using (JsonDocument document = JsonDocument.ParseValue(ref reader))
                        {
                            return document.RootElement.Clone().ToString();
                        }

                }
            }

            public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString());
            }
        }

        private void SaveSettings()
        {
            if (_path == null) return;

            try
            {
                using var stream = File.Create(_path);
                JsonSerializer.Serialize(stream, _values, s_serializerOptions);
            }
            catch (Exception e)
            {
                _telemetryProvider?.ReportError(e);
            }
        }

        private class SerializableValues : NotificationObject, IApplicationSettingsValues
        {
            private const int LiveModeDelayMsDefault = 2000;
            private const int EditorFontSizeDefault = 12;

            private bool _sendErrors;
            private string? _latestVersion;
            private string? _windowBounds;
            private string? _dockLayout;
            private string? _windowState;
            private double _editorFontSize = EditorFontSizeDefault;
            private string? _documentPath;
            private bool _searchFileContents;
            private bool _searchUsingRegex;
            private bool _optimizeCompilation;
            private int _liveModeDelayMs = LiveModeDelayMsDefault;
            private bool _searchWhileTyping;
            private bool _enableBraceCompletion = true;
            private string _defaultPlatformName = string.Empty;
            private double? _windowFontSize;
            private bool _formatDocumentOnComment = true;
            private string? _effectiveDocumentPath;
            private IDictionary<string, string>? _AvalonTextEditorOptionOverrides;
            private IDictionary<string, string>? _CSharpFormattingOptionOverrides;
            public void LoadDefaultSettings()
            {
                SendErrors = true;
                FormatDocumentOnComment = true;
                EditorFontSize = EditorFontSizeDefault;
                LiveModeDelayMs = LiveModeDelayMsDefault;
            }

            public bool SendErrors
            {
                get => _sendErrors;
                set => SetProperty(ref _sendErrors, value);
            }

            public bool EnableBraceCompletion
            {
                get => _enableBraceCompletion;
                set => SetProperty(ref _enableBraceCompletion, value);
            }

            public string? LatestVersion
            {
                get => _latestVersion;
                set => SetProperty(ref _latestVersion, value);
            }

            public string? WindowBounds
            {
                get => _windowBounds;
                set => SetProperty(ref _windowBounds, value);
            }

            public string? DockLayout
            {
                get => _dockLayout;
                set => SetProperty(ref _dockLayout, value);
            }

            public string? WindowState
            {
                get => _windowState;
                set => SetProperty(ref _windowState, value);
            }

            public double EditorFontSize
            {
                get => _editorFontSize;
                set => SetProperty(ref _editorFontSize, value);
            }

            public string? DocumentPath
            {
                get => _documentPath;
                set => SetProperty(ref _documentPath, value);
            }

            public bool SearchFileContents
            {
                get => _searchFileContents;
                set => SetProperty(ref _searchFileContents, value);
            }

            public bool SearchUsingRegex
            {
                get => _searchUsingRegex;
                set => SetProperty(ref _searchUsingRegex, value);
            }

            public bool OptimizeCompilation
            {
                get => _optimizeCompilation;
                set => SetProperty(ref _optimizeCompilation, value);
            }

            public int LiveModeDelayMs
            {
                get => _liveModeDelayMs;
                set => SetProperty(ref _liveModeDelayMs, value);
            }

            public bool SearchWhileTyping
            {
                get => _searchWhileTyping;
                set => SetProperty(ref _searchWhileTyping, value);
            }

            public string DefaultPlatformName
            {
                get => _defaultPlatformName;
                set => SetProperty(ref _defaultPlatformName, value);
            }

            public double? WindowFontSize
            {
                get => _windowFontSize;
                set => SetProperty(ref _windowFontSize, value);
            }

            public bool FormatDocumentOnComment
            {
                get => _formatDocumentOnComment;
                set => SetProperty(ref _formatDocumentOnComment, value);
            }

            [JsonIgnore]
            public string EffectiveDocumentPath
            {
                get
                {
                    if (_effectiveDocumentPath == null)
                    {

                        var userDefinedPath = DocumentPath;
                        _effectiveDocumentPath = !string.IsNullOrEmpty(userDefinedPath) && Directory.Exists(userDefinedPath)
                            ? userDefinedPath!
                            : Settings?.GetDefaultDocumentPath() ?? string.Empty;
                    }

                    return _effectiveDocumentPath;
                }
            }

            [JsonIgnore]
            public IApplicationSettings? Settings { get; set; }
            public IDictionary<string, string>? AvalonTextEditorOptionOverrides
            {
                get => _AvalonTextEditorOptionOverrides;
                set => SetProperty(ref _AvalonTextEditorOptionOverrides, value);
            }
            public IDictionary<string, string>? CSharpFormattingOptionOverrides
            {
                get => _CSharpFormattingOptionOverrides;
                set => SetProperty(ref _CSharpFormattingOptionOverrides, value);
            }
        }
    }
}
