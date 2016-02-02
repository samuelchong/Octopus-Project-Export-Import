using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Octopus.Client;
using Octopus.Client.Model;

namespace OctopusClient.UnitTests
{
    internal class ProjectTest
    {
        #region Private Fields

        private readonly string _basePath = AppDomain.CurrentDomain.BaseDirectory;
        private readonly OctopusRepository _octopusRepository = Context.GetOctopusRepository();

        #endregion Private Fields

        #region Public Methods

        [TearDown]
        public void Cleanup()
        {

        }

        [Test]
        public void CreateTest()
        {
            var projectName = "Test Runner";
            var path = _basePath;
            Project.DeleteProject(projectName);
            if (!Project.Exists(projectName))
            {
                Project.Create(projectName, path);
            }

            var expectedProject = _octopusRepository.Projects.FindByName(projectName);
            var expectedVariableSetResource = _octopusRepository.VariableSets.Get(expectedProject.VariableSetId);
            var expectedProcess = _octopusRepository.DeploymentProcesses.Get(expectedProject.DeploymentProcessId);
            var expectedChannels = _octopusRepository.Projects.GetChannels(expectedProject);

            Assert.IsNotNull(expectedProject);
            Assert.IsNotNull(expectedVariableSetResource);
            Assert.IsNotNull(expectedProcess);
            Assert.IsNotNull(expectedChannels);

            var variableSetResource = (VariableSetResource)GetFromJson(projectName, path)["variables"];
            var processResource = (DeploymentProcessResource)GetFromJson(projectName, path)["process"];
            var channelsResource = (ResourceCollection<ChannelResource>)GetFromJson(projectName, path)["channels"];

            Assert.IsTrue(expectedVariableSetResource.Variables.VariablesAreEqual(variableSetResource.Variables));
            Assert.IsTrue(expectedChannels.Items.ChannelsAreEquals(channelsResource.Items));
            Assert.IsTrue(expectedProcess.Steps.ProcessesAreEquals(processResource.Steps));
        }

        [Test]
        public void ExportTest()
        {
            var projectName = "Test Runner";
            var path = _basePath;
            Helper.CreateFolderIfNotExists(path, projectName);
            Project.Export(projectName, path);
            var createdFolderInfo = new DirectoryInfo(Path.Combine(path, projectName));
            var exportedFiles = createdFolderInfo.GetFiles();
            Assert.IsNotNull(exportedFiles);
            Assert.AreEqual(exportedFiles.Length, 4);
            foreach (var exportedFile in exportedFiles)
            {
                var fileContent = File.ReadAllText(exportedFile.FullName);
                var result = IsValidJson(fileContent);
                Assert.IsTrue(result);
            }
        }

        [SetUp]
        public void Setup()
        {

        }
        [Test]
        public void UpdateTest()
        {
            var projectName = "Test Runner";
            var path = _basePath;
            if (!Project.Exists(projectName))
            {
                Assert.Fail("Project does not exist");
            }
            Project.Update(projectName,path);
            var expectedProject = _octopusRepository.Projects.FindByName(projectName);
            var expectedVariableSetResource = _octopusRepository.VariableSets.Get(expectedProject.VariableSetId);
            var expectedProcess = _octopusRepository.DeploymentProcesses.Get(expectedProject.DeploymentProcessId);
            var expectedChannels = _octopusRepository.Projects.GetChannels(expectedProject);
            
            Assert.IsNotNull(expectedProject);
            Assert.IsNotNull(expectedVariableSetResource);
            Assert.IsNotNull(expectedProcess);
            Assert.IsNotNull(expectedChannels);

            var variableSetResource = (VariableSetResource)GetFromJson(projectName, path)["variables"];
            var processResource = (DeploymentProcessResource)GetFromJson(projectName, path)["process"];
            var channelsResource = (ResourceCollection<ChannelResource>)GetFromJson(projectName, path)["channels"];
            
            Assert.IsTrue(expectedVariableSetResource.Variables.VariablesAreEqual(variableSetResource.Variables));
            Assert.IsTrue(expectedChannels.Items.ChannelsAreEquals(channelsResource.Items));
            Assert.IsTrue(expectedProcess.Steps.ProcessesAreEquals(processResource.Steps));
        }

        #endregion Public Methods

        #region Private Methods

        private static IDictionary<string, object> GetFromJson(string projectName, string path)
        {
            var variables =
                  JsonConvert.DeserializeObject<VariableSetResource>(
                      File.ReadAllText(Path.Combine(path, string.Format(@"{0}\variables.json", projectName))));
            var process =
               JsonConvert.DeserializeObject<DeploymentProcessResource>(
                   File.ReadAllText(Path.Combine(path, string.Format(@"{0}\process.json", projectName))));
            var channels =
               JsonConvert.DeserializeObject<ResourceCollection<ChannelResource>>(
                   File.ReadAllText(Path.Combine(path, string.Format(@"{0}\channels.json", projectName))));

            var componentsDico = new Dictionary<string, object>
            {
                {"variables", variables},
                {"process", process},
                {"channels", channels}
            };

            return componentsDico;
        }

        private static bool IsValidJson(string strInput)
        {
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) ||
                (strInput.StartsWith("[") && strInput.EndsWith("]")))
            {
                try
                {
                    JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException)
                {
                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return false;
        }

        #endregion Private Methods
    }
}
