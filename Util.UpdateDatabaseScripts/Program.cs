using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using Util.UpdateDatabaseScripts.Configs;

namespace Util.UpdateDatabaseScripts
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = "";
            string conectionString = "";

            bool isRelease = false;

            if (args.Count() == 3)
            {
                isRelease = Convert.ToBoolean(args[2]);
            }

#if !DEBUG
            if (args.Count()>=2)
            {
                path = args[0];
                conectionString = args[1];
            }
            else
            {
                var strError = "Параметры переданы не верно. Следует передать 2 параметра:\n 1. путь к файлам. \n 2. Строка подключение к базе данных ";
                Console.WriteLine(strError);
                throw new Exception(strError);
            }
#endif

#if DEBUG
            var builder = new ConfigurationBuilder()
                        .AddJsonFile($"appsettings.json", true, true)
                        .AddJsonFile($"appsettings.Development.json", true, true)
                        .AddEnvironmentVariables();

            var config = builder.Build();
            var cfg = config.Get<AppConfig>();
            conectionString = cfg.ConnectionString;
            path = cfg.FolderPath;
#endif

            IFileExecuter fileExecuter = new SqlFileToDBExecuter(conectionString);

            var updater = new UpdateScriptService(fileExecuter, ".git", "UsefulSQLQueries");

            updater.UpdateFromTheDirectory(path, isRelease);

            Console.WriteLine("Процедуры только для PROD:\n" + string.Join(", \n", updater.ProductionOnly));
            Console.WriteLine("\n");
            Console.WriteLine("Процедуры требующие внимания:\n" + string.Join(", \n", updater.Warnings));
            Console.WriteLine("\n");
            Console.WriteLine("Оставшиеся ошибки (файл - ошибка):\n" + string.Join(", \n", updater.Errors.Select(x => x.Key + " - " + x.Value)));
            Console.ReadLine();
        }
    }
}
