using System;
using System.Threading.Tasks;
using UVOCBot.Model;
using UVOCBot.Utilities;

namespace UVOCBot.Services.Abstractions
{
    public interface ISettingsService : IDisposable
    {
        /// <summary>
        /// Attemps to load a settings object. If no settings file has been saved previously, a default object will be returned
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        Task<Optional<T>> LoadSettings<T>() where T : ISettings, new();

        /// <summary>
        /// Attempts to save an <see cref="ISettings"/> object to the path obtained from <see cref="GetSettingsFilePath{T}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="settings"></param>
        /// <returns>A value indicating if the save operation completed successfully</returns>
        Task<bool> SaveSettings<T>(T settings) where T : ISettings;

        /// <summary>
        /// Gets the physical file path to which an <see cref="ISettings"/> object would be saved
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        string GetSettingsFilePath<T>() where T : ISettings;
    }
}
