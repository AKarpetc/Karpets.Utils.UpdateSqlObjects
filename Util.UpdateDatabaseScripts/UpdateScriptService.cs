using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Util.UpdateDatabaseScripts
{
    public class UpdateScriptService
    {
        private readonly IFileExecuter _fileExecuter;
        private string[] _excludes;

        /// <summary>
        /// Количество проходов при создании объектов БД        
        /// </summary>
        private const int COUNT_REPEATS = 3;

        /// <summary>
        /// Скрипты требующие внимание
        /// </summary>
        public const string WARNING = "--WARNING";

        /// <summary>
        /// Применяемые только на продуктивной системе
        /// </summary>
        public const string PRODUCTION_ONLY = "--PRODUCTION_ONLY";

        /// <summary>
        /// Класс для инициализации перебора скриптов из папки
        /// </summary>
        /// <param name="fileExecuter">Родительская папка скриптов(дочернин папки должны соответствовать объектам SQL Server)</param>
        /// <param name="excludes">Какие папки нужно исключить из выполнения</param>
        public UpdateScriptService(IFileExecuter fileExecuter, params string[] excludes)
        {
            _fileExecuter = fileExecuter;
            _excludes = excludes;

            ProductionOnly = new List<string>();
            Warnings = new List<string>();
            Errors = new List<KeyValuePair<string, string>>();
        }

        public List<string> ProductionOnly { get; set; }

        public List<string> Warnings { get; set; }

        public List<KeyValuePair<string, string>> Errors { get; set; }

        /// <summary>
        /// Метод получает скрипт из файла и применяет в БД
        /// </summary>
        /// <param name="rootDirectory">Корневая директория</param>
        /// <param name="isProduction">Служит для скриптов применимых только на продакшен</param>
        public void UpdateFromTheDirectory(string rootDirectory, bool isProduction)
        {
            var absolutePath = rootDirectory;
            var folderEntries = Directory.GetDirectories(absolutePath);
            var typeFolder = "";

            foreach (string folderName in folderEntries)
            {
                if (_excludes.Any(x => folderName.Contains(x)))
                {
                    continue;
                }

                typeFolder = Path.GetFileName(folderName);
                string[] fileEntries = Directory.GetFiles(folderName);

                var errorsFile = new List<KeyValuePair<string, string>>();

                var forDoing = fileEntries.ToList();

                for (int i = 0; i < COUNT_REPEATS; i++)
                {
                    foreach (string fileName in forDoing)
                    {
                        try
                        {
                            var name = Path.GetFileName(fileName).Split(".")[1];
                            var type = Path.GetFileName(fileName).Split(".")[2];

                            string text = System.IO.File.ReadAllText(fileName);

                            var model = new ExecutingModel
                            {
                                Folder = typeFolder,
                                Name = name,
                                Type = type,
                                Content = text
                            };

                            if (text.Contains(WARNING))
                            {
                                Warnings.Add(fileName);
                            }

                            if (text.Contains(PRODUCTION_ONLY))
                            {
                                ProductionOnly.Add(fileName);
                            }

                            if (!isProduction)
                            {
                                if (text.Contains(PRODUCTION_ONLY)) continue;
                            }

                            _fileExecuter.Execute(model);
                        }
                        catch (Exception ex)
                        {
                            errorsFile.Add(new KeyValuePair<string, string>(fileName, ex.Message));
                        }
                    }

                    if (errorsFile.Count() > 0)
                    {
                        forDoing = errorsFile.Select(x => x.Key).ToList();

                        if (i == COUNT_REPEATS - 1) Errors.AddRange(errorsFile);

                        errorsFile.Clear();
                    }
                    else
                    {
                        break;
                    }
                }
            };
        }
    }
}
