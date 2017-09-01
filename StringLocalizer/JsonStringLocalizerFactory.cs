using System;
using System.Collections.Concurrent;
using System.Linq;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LocalizationCultureCore.StringLocalizer
{
    public class JsonStringLocalizerFactory : IStringLocalizerFactory
    {
        private static readonly string[] _knownViewExtensions = new[] { ".cshtml" };
        private readonly ConcurrentDictionary<string, JsonStringLocalizer> _localizerCache;        
        private readonly IHostingEnvironment _applicationEnvironment;
        private readonly ILogger<JsonStringLocalizerFactory> _logger;
        private string _resourcesRelativePath;

        public JsonStringLocalizerFactory(IHostingEnvironment applicationEnvironment, IOptions<JsonLocalizationOptions> localizationOptions,
                                          ILogger<JsonStringLocalizerFactory> logger)
        {
            if (applicationEnvironment == null) throw new ArgumentNullException(nameof(applicationEnvironment));
            if (localizationOptions == null) throw new ArgumentNullException(nameof(localizationOptions));
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            this._applicationEnvironment = applicationEnvironment;
            this._logger = logger;
            this._localizerCache = new ConcurrentDictionary<string, JsonStringLocalizer>();            
            this._resourcesRelativePath = localizationOptions.Value.ResourcesPath ?? String.Empty;
            if (!String.IsNullOrEmpty(_resourcesRelativePath))
            {
                _resourcesRelativePath = _resourcesRelativePath
                    .Replace(Path.AltDirectorySeparatorChar, '.')
                    .Replace(Path.DirectorySeparatorChar, '.') + ".";
            }
            _logger.LogTrace($"Created {nameof(JsonStringLocalizerFactory)} with:{Environment.NewLine}" +
                $"    (application name: {_applicationEnvironment.ApplicationName}{Environment.NewLine}" +
                $"    (resources relative path: {_resourcesRelativePath})");
        }

        public IStringLocalizer Create(Type resourceSource)
        {
            if (resourceSource == null) throw new ArgumentNullException(nameof(resourceSource));
            Logger.LogTrace($"Getting localizer for type {resourceSource}");
            if(String.IsNullOrEmpty(ResourcesRelativePath)) throw new ArgumentNullException(nameof(ResourcesRelativePath));
            var ResourceBaseName = ApplicationEnvironment.ApplicationName + "." + ResourcesRelativePath;
            return LocalizerCache.GetOrAdd(ResourceBaseName, new JsonStringLocalizer(ResourceBaseName, ApplicationEnvironment.ApplicationName, Logger));
        }

        public IStringLocalizer Create(string BaseName, string Location)
        {
            if(String.IsNullOrEmpty(BaseName)) throw new ArgumentNullException(nameof(BaseName));            
            Logger.LogTrace($"Getting localizer for baseName {BaseName} and location {Location}");
            Location = Location ?? ApplicationEnvironment.ApplicationName;
            var resourceBaseName = Location + "." + ResourcesRelativePath;
            var viewExtension = KnownViewExtensions.FirstOrDefault(extension => resourceBaseName.EndsWith(extension));
            if (viewExtension != null) resourceBaseName = resourceBaseName.Substring(0, resourceBaseName.Length - viewExtension.Length);
            Logger.LogTrace($"Localizer basename: {resourceBaseName}");
            return LocalizerCache.GetOrAdd(resourceBaseName, new JsonStringLocalizer(resourceBaseName, ApplicationEnvironment.ApplicationName, Logger));
        }

        public ILogger Logger
        {
            get { return _logger; }
        }

        public string ResourcesRelativePath
        {
            get { return _resourcesRelativePath; }
        }

        public ConcurrentDictionary<string, JsonStringLocalizer> LocalizerCache
        {
            get { return _localizerCache; }
        }

        public IHostingEnvironment ApplicationEnvironment
        {
            get { return _applicationEnvironment; }
        }

        public string[] KnownViewExtensions
        {
            get { return _knownViewExtensions; }
        }
    }
}
