using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UVOCBotRemora.Model;
using UVOCBotRemora.Utilities;

namespace UVOCBotRemora.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<SettingsService> _logger;

        private readonly Dictionary<Type, ISettings> _cache = new();
        private readonly Semaphore _semaphore = new(1, 1);
        private bool _isDisposed;

        public SettingsService(IFileSystem fileSystem, ILogger<SettingsService> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<Optional<T>> LoadSettings<T>() where T : ISettings, new()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SettingsService));

            T? settings = default;

            if (_cache.ContainsKey(typeof(T)))
            {
                settings = (T)_cache[typeof(T)];
            }
            else if (!File.Exists(GetSettingsFilePath<T>()))
            {
                settings = (T)new T().Default;
                _cache.Add(typeof(T), settings);
            }
            else
            {
                string filePath = GetSettingsFilePath<T>();
                Stream readStream = Stream.Null;

                if (!_semaphore.WaitOne(1000))
                    return Optional<T>.FromNoValue();

                try
                {
                    readStream = _fileSystem.FileStream.Create(GetSettingsFilePath<T>(), FileMode.Open, FileAccess.Read);
                    settings = await JsonSerializer.DeserializeAsync<T>(readStream).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load settings");
                }
                finally
                {
                    readStream.Dispose();
                }

                _semaphore.Release();

                if (settings is not null)
                    _cache.Add(typeof(T), settings);
            }

            if (settings is null)
                return Optional<T>.FromNoValue();
            else
                return Optional<T>.FromValue(settings);
        }

        /// <inheritdoc />
        public async Task<bool> SaveSettings<T>(T settings) where T : ISettings
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SettingsService));

            if (!_semaphore.WaitOne(1000))
                return false;

            Stream writeStream = Stream.Null;
            try
            {
                writeStream = _fileSystem.FileStream.Create(GetSettingsFilePath<T>(), FileMode.Create, FileAccess.Write);
                await JsonSerializer.SerializeAsync(writeStream, settings).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not save settings");
                return false;
            }
            finally
            {
                writeStream.Dispose();
            }

            _cache[typeof(T)] = settings;
            _semaphore.Release();
            return true;
        }

        /// <inheritdoc/>
        public string GetSettingsFilePath<T>() where T : ISettings
        {
            string filePath = typeof(T).Name;
            return Program.GetAppdataFilePath(_fileSystem, filePath);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _semaphore.Dispose();
                }

                _cache.Clear();
                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
