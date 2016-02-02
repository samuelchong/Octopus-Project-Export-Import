using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Octopus.Client;
using Octopus.Client.Model;
using Octopus.Common.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace OctopusClient
{
    public class Project
    {
        #region Public Methods

        public static void Create(string projectSlug, string exportPath)
        {
            var project =
                JsonConvert.DeserializeObject<ProjectResource>(
                    File.ReadAllText(Path.Combine(exportPath, string.Format(@"{0}\project.json", projectSlug))));
            var variables =
                JsonConvert.DeserializeObject<VariableSetResource>(
                    File.ReadAllText(Path.Combine(exportPath, string.Format(@"{0}\variables.json", projectSlug))));
            var process =
                JsonConvert.DeserializeObject<DeploymentProcessResource>(
                    File.ReadAllText(Path.Combine(exportPath, string.Format(@"{0}\process.json", projectSlug))));
            var channels =
                JsonConvert.DeserializeObject<ResourceCollection<ChannelResource>>(
                    File.ReadAllText(Path.Combine(exportPath, string.Format(@"{0}\channels.json", projectSlug))));

            var octopusRepository = Context.GetOctopusRepository();

            try
            {
                var createdProject = octopusRepository.Projects.Create(project);

                ImportProcess(octopusRepository, createdProject, process);

                ImportVariables(octopusRepository, createdProject, variables);

                ImportChannels(octopusRepository, createdProject, channels);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public static void DeleteProject(string projectName)
        {
            var octopusRepository = Context.GetOctopusRepository();
            var project = octopusRepository.Projects.FindByName(projectName);
            octopusRepository.Projects.Delete(project);
        }

        public static bool Exists(string projectIdOrSlug)
        {
            Console.WriteLine("---> Trying to find if the project having name = {0} exists", projectIdOrSlug);
            var octopusRepository = Context.GetOctopusRepository();
            return octopusRepository.Projects.FindByName(projectIdOrSlug) != null;
        }

        public static void Export(string projectName, string exportPath)
        {
            try
            {
                var octopusRepository = Context.GetOctopusRepository();

                var project = octopusRepository.Projects.FindByName(projectName);
                var variables = octopusRepository.VariableSets.Get(project.VariableSetId);

                foreach (var variable in variables.Variables)
                {
                    if (!variable.IsSensitive) continue;

                    variable.Value = GetSensitiveVariableValue(project.Id, variable.Id);
                }

                var process = octopusRepository.DeploymentProcesses.Get(project.DeploymentProcessId);
                var channels = octopusRepository.Projects.GetChannels(project);

                File.WriteAllText(Path.Combine(exportPath, string.Format(@"{0}\project.json", projectName)),
                    JsonConvert.SerializeObject(project, Formatting.Indented));
                File.WriteAllText(Path.Combine(exportPath, string.Format(@"{0}\process.json", projectName)),
                    JsonConvert.SerializeObject(process, Formatting.Indented));
                File.WriteAllText(Path.Combine(exportPath, string.Format(@"{0}\variables.json", projectName)),
                    JsonConvert.SerializeObject(variables, Formatting.Indented));
                File.WriteAllText(Path.Combine(exportPath, string.Format(@"{0}\channels.json", projectName)),
                    JsonConvert.SerializeObject(channels, Formatting.Indented));
            }
            catch (Exception e)
            {
                Console.WriteLine("[Error:] -- Exception during export process.");
                Console.WriteLine(e.Message);
            }
        }

        public static JToken GetProject(string projectName)
        {
            var data = Helper.RetrieveJsonResponse(Context.OctopusUri + "/api/projects");
            var jsonData = JObject.Parse(data);
            return jsonData.SelectToken(string.Format("$.Items[?(@.Name == '{0}')]", projectName));
        }

        public static void ImportChannels(OctopusRepository octopusRepository, ProjectResource createdProject,
            ResourceCollection<ChannelResource> channels)
        {
            foreach (var channel in channels.Items)
            {
                try
                {
                    channel.ProjectId = createdProject.Id;
                    if (!channel.Name.Equals("Default"))
                    {
                        octopusRepository.Channels.Create(channel);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public static void ImportProcess(OctopusRepository octopusRepository, ProjectResource createdProject,
            DeploymentProcessResource process)
        {
            process.ProjectId = createdProject.Id;
            process.Id = string.Format("deploymentprocess-{0}", createdProject.Id);
            process.Links["Self"] = string.Format("/api/deploymentprocesses/{0}", createdProject.DeploymentProcessId);
            process.Version = 0;
            octopusRepository.DeploymentProcesses.Modify(process);
        }

        public static void ImportVariables(OctopusRepository octopusRepository, ProjectResource createdProject,
            VariableSetResource variables)
        {
            variables.OwnerId = createdProject.Id;
            variables.Links["Self"] = string.Format("/api/variables/{0}", createdProject.VariableSetId);
            variables.Version = 0;
            octopusRepository.VariableSets.Modify(variables);
        }
        public static void Update(string projectSlug, string path)
        {
            var octopusRepository = Context.GetOctopusRepository();
            var project =
                JsonConvert.DeserializeObject<ProjectResource>(
                    File.ReadAllText(Path.Combine(path, string.Format(@"{0}\project.json", projectSlug))));

            var variableSetResource =
                JsonConvert.DeserializeObject<VariableSetResource>(
                    File.ReadAllText(Path.Combine(path, string.Format(@"{0}\variables.json", projectSlug))));
            var process =
                JsonConvert.DeserializeObject<DeploymentProcessResource>(
                    File.ReadAllText(Path.Combine(path, string.Format(@"{0}\process.json", projectSlug))));
            var channels =
                JsonConvert.DeserializeObject<ResourceCollection<ChannelResource>>(
                    File.ReadAllText(Path.Combine(path, string.Format(@"{0}\channels.json", projectSlug))));

            var sensitiveVariablesDico = new Dictionary<string, string>();

            foreach (var variable in variableSetResource.Variables)
            {
                if (!variable.IsSensitive) continue;
                sensitiveVariablesDico.Add(variable.Id, variable.Value);
                variable.Value = string.Empty;
            }

            UpdateVariablesVersion(projectSlug, octopusRepository, project, variableSetResource);
            UpdateProcessVersion(projectSlug, octopusRepository, project, process);

            try
            {
                //
                project.Links["Self"] = string.Format("/api/projects/{0}", project.Id);
                octopusRepository.Projects.Modify(project);

                // Update variables
                variableSetResource.Links["Self"] = string.Format("/api/variables/{0}", project.VariableSetId);
                octopusRepository.VariableSets.Modify(variableSetResource);

                // Update process
                process.Id = string.Format("deploymentprocess-{0}", project.Id);
                process.Links["Self"] = string.Format("/api/deploymentprocesses/{0}", project.DeploymentProcessId);
                octopusRepository.DeploymentProcesses.Modify(process);

                //Update Channels
                channels.Links["Self"] = string.Format("/api/projects/{0}/channels", project.Id);
                foreach (var channel in channels.Items)
                {
                    channel.Links["Self"] = string.Format("/api/channels/{0}", channel.Id);
                    octopusRepository.Channels.Modify(channel);
                }

                //Update sensitive variables
                UpdateSensitiveVariableValues(project.Id, sensitiveVariablesDico);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public static void UpdateProcessVersion(string projectSlug, OctopusRepository octopusRepository,
            ProjectResource project, DeploymentProcessResource deploymentProcessResource)
        {
            var process = octopusRepository.DeploymentProcesses.Get(project.DeploymentProcessId);
            deploymentProcessResource.Version = process.Version;
        }

        public static void UpdateVariablesVersion(string projectSlug, OctopusRepository octopusRepository,
            ProjectResource project, VariableSetResource varaibleResources)
        {
            var variables = octopusRepository.VariableSets.Get(project.VariableSetId);
            varaibleResources.Version = variables.Version;
        }

        #endregion Public Methods

        #region Private Methods

        private static string GetSensitiveVariableValue(string projectId, string variableId)
        {
            var variableValue = string.Empty;

            var connectionString = ConfigurationManager.AppSettings["DbConnectionString"];

            var dataContext = new OctopusServerDataContext(connectionString);

            var project = dataContext.Projects.FirstOrDefault(p => p.Id == projectId);

            if (project == null) return variableValue;

            var variableSet = dataContext.VariableSets.FirstOrDefault(v => v.Id == project.VariableSetId);

            if (variableSet == null) return variableValue;

            var json = variableSet.JSON;

            if (string.IsNullOrEmpty(json)) return variableValue;

            var variableSetResource = JsonConvert.DeserializeObject<VariableSetResource>(json);

            var variable = variableSetResource.Variables.FirstOrDefault(v => v.Id == variableId);

            if (variable == null) return variableValue;

            variableValue = variable.Value;

            return variableValue;
        }

        private static void UpdateSensitiveVariableValues(string projectId, Dictionary<string, string> sensitiveVariablesDico)
        {
            if (sensitiveVariablesDico.Count == 0)
            {
                return;
            }

            var connectionString = ConfigurationManager.AppSettings["DbConnectionString"];

            var dataContext = new OctopusServerDataContext(connectionString);

            var project = dataContext.Projects.FirstOrDefault(p => p.Id == projectId);

            if (project == null)
            {
                throw new Exception("UpdateSensitiveVariableValues: Can\"t retrieve the project from the database");
            }

            var variableSet = dataContext.VariableSets.FirstOrDefault(v => v.Id == project.VariableSetId);

            if (variableSet == null)
            {
                throw new Exception("UpdateSensitiveVariableValues: Can\"t retrieve the variable set from the database");
            }

            var json = variableSet.JSON;

            if (string.IsNullOrEmpty(json))
            {
                throw new Exception("UpdateSensitiveVariableValues: variable set json is empty");
            }

            var variableList = JsonConvert.DeserializeObject<VariableList>(json);

            foreach (var entry in sensitiveVariablesDico)
            {
                var variable = variableList.Variables.FirstOrDefault(v => v.Id == entry.Key);
                if (variable == null) continue;
                variable.Value = entry.Value;
            }

            var newJson = JsonConvert.SerializeObject(variableList);

            variableSet.JSON = newJson;

            dataContext.SubmitChanges();
        }

        #endregion Private Methods
    }
}