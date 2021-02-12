using System;
using System.Threading.Tasks;
using UVOCBot.Model;

namespace UVOCBot.Services
{
    public interface ISettingsService : IDisposable
    {
        Task<T> LoadSettings<T>() where T : ISettings, new();
        Task SaveSettings<T>(T settings) where T : ISettings;
    }
}
