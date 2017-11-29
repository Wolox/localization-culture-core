using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace LocalizationCultureCore.StringLocalizer
{
    public class JsonStringLocalizer : IStringLocalizer
    {
        private readonly ConcurrentDictionary<string, Lazy<JObject>> _resourceObjectCache;
        private readonly string _baseName;
        private readonly string _applicationName;
        private readonly ILogger _logger;
        private readonly string _resourceFileLocation;
        private readonly char _jsonSplitter;
        private readonly CultureInfo _currentCulture;

        public JsonStringLocalizer(string baseName, string applicationName, ILogger logger) 
                :this(baseName, applicationName, logger, CultureInfo.CurrentUICulture){}
        
        public JsonStringLocalizer(string baseName, string applicationName, ILogger logger, CultureInfo currentCulture)
        {
            if (String.IsNullOrEmpty(baseName)) throw new ArgumentNullException(nameof(baseName));
            if (String.IsNullOrEmpty(applicationName)) throw new ArgumentNullException(nameof(applicationName));
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            this._resourceObjectCache = new ConcurrentDictionary<string, Lazy<JObject>>();
            this._baseName = baseName;
            this._applicationName = applicationName;
            this._logger = logger;
            this._jsonSplitter = ':';
            this._currentCulture = currentCulture;
            _resourceFileLocation = LocalizerUtil.TrimPrefix(_baseName, _applicationName).Trim('.');
            _logger.LogTrace($"Resource file location base path: {_resourceFileLocation}");
        }

        public virtual LocalizedString this[string name]
        {
            get
            {
                if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
                var value = GetLocalizedString(name, CurrentCulture);
                return new LocalizedString(name, value ?? name, resourceNotFound: value == null);
            }
        }

        public virtual LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
                var format = GetLocalizedString(name, CurrentCulture);
                var value = string.Format(format ?? name, arguments);
                return new LocalizedString(name, value, resourceNotFound: format == null);
            }
        }

        public IStringLocalizer WithCulture(CultureInfo culture) => new JsonStringLocalizer(BaseName, _applicationName, Logger, culture);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            var cultures = new List<CultureInfo> { CurrentCulture };
            var currentCulture = CurrentCulture;
            CultureInfo previousCulture = null;
            var strings = new List<LocalizedString>();
            if(includeParentCultures)
            {
                do
                {
                    previousCulture = currentCulture;
                    currentCulture = currentCulture.Parent;
                    if(!String.IsNullOrEmpty(currentCulture.Name))
                        cultures.Add(currentCulture);
                } while(previousCulture != currentCulture);
            }
            foreach(var culture in cultures)
            {
                var resourceObject = GetResourceObject(culture);
                if(resourceObject != null)
                {
                    var values = resourceObject.Values();
                    strings.AddRange(from value in values select new LocalizedString(value.Path, value.Value<string>()));
                }
                else Logger.LogInformation($"No resource file found or error occurred for base name {BaseName} and culture {culture}.");
            }
            return strings;
        }

        protected string GetLocalizedString(string name, CultureInfo culture)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            var currentCulture = culture;
            CultureInfo previousCulture = null;
            do
            {
                var resourceObject = GetResourceObject(currentCulture);
                if (resourceObject == null) Logger.LogInformation($"No resource file found or error occurred for base name {BaseName}, culture {currentCulture} and key '{name}'");
                else
                {
                    JToken value = TryGetValue(resourceObject, name);
                    if(value != null) return value.ToString();
                }
                previousCulture = currentCulture;
                currentCulture = currentCulture.Parent;
                Logger.LogTrace($"Switching to parent culture {currentCulture} for key '{name}'.");
            } while (previousCulture != currentCulture);
            Logger.LogInformation($"Could not find key '{name}' in resource file for base name {BaseName} and culture {CultureInfo.CurrentCulture}");
            return null;
        }

        private JObject GetResourceObject(CultureInfo currentCulture)
        {
            if (currentCulture == null) throw new ArgumentNullException(nameof(currentCulture));
            Logger.LogTrace($"Attempt to get resource object for culture {currentCulture}");
            var cultureSuffix = currentCulture.Name;
            cultureSuffix = cultureSuffix == "." ? "" : cultureSuffix;

            var LazyJObjectGetter = new Lazy<JObject>(() =>
            {
                // First attempt to find a resource file location that exists.
                string resourcePath = ResourceFileLocation + Path.DirectorySeparatorChar + cultureSuffix + ".json";
                if(!ResourceExists(resourcePath, cultureSuffix)) return null;
                // Found a resource file path: attempt to parse it into a JObject.
                try
                {
                    var resourceFileStream = new FileStream(resourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                    using (resourceFileStream)
                    {
                        var resourceReader = new JsonTextReader(new StreamReader(resourceFileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true));
                        using (resourceReader)
                        {
                            return JObject.Load(resourceReader);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError($"Error occurred attempting to read JSON resource file {resourcePath}: {e}");
                    return null;
                }

            }, LazyThreadSafetyMode.ExecutionAndPublication);
            return ResourceObjectCache.GetOrAdd(cultureSuffix, LazyJObjectGetter).Value;
        }

        private JToken TryGetValue(JObject resource, string name)
        {
            JToken jTokenValue = null;
            string[] keys = name.Split(JsonSplitter);
            jTokenValue = resource[keys[0]];            
            for(var i = 1; i < keys.Length; i++)
            {
                jTokenValue = jTokenValue[keys[i]];
            }
            return jTokenValue;
        }

        private bool ResourceExists(string path, string cultureSuffix)
        {
            if (File.Exists(path))
            {
                Logger.LogInformation($"Resource file location {path} found");
                return true;
            }
            Logger.LogTrace($"Resource file location {path} does not exist");
            Logger.LogTrace($"No resource file found for suffix {cultureSuffix}");
            return false;
        }

        public string ResourceFileLocation
        {
            get { return _resourceFileLocation; }
        }

        public string BaseName
        {
            get { return _baseName; }
        }

        public char JsonSplitter 
        {
            get { return _jsonSplitter; }
        }

        public ILogger Logger
        {
            get { return _logger; }
        }

        public ConcurrentDictionary<string, Lazy<JObject>> ResourceObjectCache
        {
            get { return _resourceObjectCache; }
        }

        public CultureInfo CurrentCulture
        {
            get { return _currentCulture; }
        }
    }
}
