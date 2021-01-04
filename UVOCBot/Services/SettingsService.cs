using Serilog;
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
        private readonly Semaphore _semaphore = new Semaphore(1, 1);
        private bool _isDisposed;

        public SettingsService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public async Task<T> LoadSettings<T>() where T : ISettings, new()
        {
            try
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(SettingsService));

                T settings;

                if (!_semaphore.WaitOne(1000))
                    throw new TimeoutException("Failed to acquire read mutex");

                if (_cache.ContainsKey(typeof(T)))
                {
                    settings = (T)_cache[typeof(T)];
                }
                else
                {
                    string filePath = GetFilePath<T>();

                    try
                    {
                        using Stream readStream = _fileSystem.FileStream.Create(GetFilePath<T>(), FileMode.Open, FileAccess.Read);
                        settings = await JsonSerializer.DeserializeAsync<T>(readStream).ConfigureAwait(true);
                    }
                    catch
                    {
                        settings = (T)new T().Default;
                    }

                    _cache.Add(typeof(T), settings);
                }

                _semaphore.Release();
                return settings;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not load settings");
                throw;
            }
        }

        public async Task SaveSettings<T>(T settings) where T : ISettings
        {
            try
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(SettingsService));

                if (!_semaphore.WaitOne(1000))
                    throw new TimeoutException("Failed to acquire read mutex");

                Stream writeStream = _fileSystem.FileStream.Create(GetFilePath<T>(), FileMode.Create, FileAccess.Write);
                await JsonSerializer.SerializeAsync(writeStream, settings).ConfigureAwait(false);

                _cache[typeof(T)] = settings;
                _semaphore.Release();
            } catch (Exception ex)
            {
                Log.Error(ex, "Could not save settings");
                throw;
            }
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
