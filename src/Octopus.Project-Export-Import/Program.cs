using System;

namespace OctopusClient
{
    internal class Program
    {
        #region Private Methods

        private static void Main(string[] args)
        {        
            if (args.Length != 3)
            {
                PrintUsage();
            }

            var action = string.Empty;
            var projectName = string.Empty;
            var path = string.Empty;

            foreach (var s in args)
            {
                var argument = s.Split(new[] { ':' }, 2);
                switch (argument[0])
                {
                    case "/action":
                        action = argument[1];
                        break;
                    case "/project":
                        projectName = argument[1];
                        break;
                    case "/path":
                        path = argument[1];
                        break;
                    default:
                        PrintUsage();
                        break;
                }
            }

            if (string.IsNullOrEmpty(action))
            {
                PrintUsage();
                return;
            }

            if (string.IsNullOrEmpty(projectName))
            {
                PrintUsage();
                return;
            }

            if (string.IsNullOrEmpty(path))
            {
                PrintUsage();
                return;
            }

            if (string.Equals(action, "export", StringComparison.CurrentCultureIgnoreCase))
            {
                Helper.CreateFolderIfNotExists(path, projectName);
                Console.WriteLine("---> Exporting project : {0} to : {1}", projectName, path);
                Project.Export(projectName, path);
                Console.WriteLine("---> Project : {0} exported", projectName);
            }

            if (string.Equals(action, "import", StringComparison.CurrentCultureIgnoreCase))
            {
                if (!Project.Exists(projectName))
                {
                    Console.WriteLine("---> Creating project {0} from : {1}", projectName, path);
                    Project.Create(projectName, path);
                    Console.WriteLine("---> Project : {0} imported", projectName);
                }
                else
                {
                    Console.WriteLine("---> Updating project {0} from : {1} ", projectName, path);
                    Project.Update(projectName, path);
                    Console.WriteLine("---> Project : {0} imported", projectName);
                }
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Incorrect Arguments");
            Console.WriteLine(
                "Usage: OctopusClient.exe /action:ACTION(IMPORT | EXPORT) /project:PROJECT /path:FOLDER_PATH");
            Environment.Exit(0);
        }

        #endregion Private Methods
    }
}
