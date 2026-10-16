using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace OrderProcessing.Shared
{
    public class JsonFileStore
    {
        private readonly string _dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        private string GetFilePath(string fileName)
        {
            return Path.Combine(_dataDirectory, fileName);
        }
        public async Task<List<T>> ReadAsync<T>(string fileName, CancellationToken ct)
        {
            var filePath = GetFilePath(fileName);
            if (!File.Exists(filePath))
            {
                return new List<T>();
            }
            var json = await File.ReadAllTextAsync(filePath, ct);
            try
            {
                return JsonSerializer.Deserialize<List<T>>(json, JsonConfig.Options)
                       ?? new List<T>();
            }
            catch (JsonException ex)
            {
                throw new DataFileCorruptedException(
                    $"The file '{fileName}' contains invalid JSON.", ex);
            }
        }
        public async Task WriteAsync<T>(string fileName, List<T> data, CancellationToken ct)
        {
            Directory.CreateDirectory(_dataDirectory);
            var filePath = GetFilePath(fileName);

            var tempFilePath = Path.Combine(
                              _dataDirectory,
                              $"{Guid.NewGuid()}_{Path.GetFileName(filePath)}");

            var json = JsonSerializer.Serialize(data, JsonConfig.Options);

            try
            {
                await File.WriteAllTextAsync(tempFilePath, json, ct);
                ct.ThrowIfCancellationRequested();
                if(File.Exists(filePath))
                {
                    File.Replace(tempFilePath, filePath, null);
                }
                else
                {
                    File.Move(tempFilePath, filePath);
                }
               
            }
            finally
            { 
                if(File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }

            }

        }
            
    }
}
