using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Model;

namespace UVOCBot.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly IFileSystem _fileSystem;
        private readonly Dictionary<Type, ISettings> _cache = new Dictionary<Type, ISettings>();
        private readonly Mutex _readMutex = new Mutex(false);
        private readonly Mutex _writeMutex = new Mutex(false);
        private bool _isDisposed;

        public SettingsService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public async Task<T> LoadSettings<T>() where T : ISettings, new()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SettingsService));

            T settings;
            if (!_readMutex.WaitOne(1000))
                throw new TimeoutException("Failed to acquire read mutex");

            if (_cache.ContainsKey(typeof(T)))
            {
                settings = (T)_cache[typeof(T)];
            }
            else
            {
                string filePath = GetFilePath<T>();

                if (_fileSystem.File.Exists(filePath))
                {
                    using Stream fs = _fileSystem.File.OpenWrite(GetFilePath<T>());
                    settings = await JsonSerializer.DeserializeAsync<T>(fs).ConfigureAwait(false);
                }
                else
                {
                    settings = (T)new T().Default;
                }

                _cache.Add(typeof(T), settings);
            }

            _readMutex.ReleaseMutex();
            return settings;
        }

        public async Task SaveSettings<T>(T settings) where T : ISettings
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SettingsService));

            if (!_writeMutex.WaitOne(1000))
                throw new TimeoutException("Failed to acquire read mutex");

            using Stream readStream = _fileSystem.File.OpenRead(GetFilePath<T>());
            await JsonSerializer.SerializeAsync(readStream, settings).ConfigureAwait(false);

            _cache[typeof(T)] = settings;
            _writeMutex.ReleaseMutex();
        }

        private string GetFilePath<T>()
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
                    _readMutex.Dispose();
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
