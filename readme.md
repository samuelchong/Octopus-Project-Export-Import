# Octopus Import Export Projects Tool #
This tool is used for importing/exporting octopus projects.
It will import the project with its variables, process, and channels and it also handle sensitive variables.

## Usage ##
Octopus.Project-Export-Import.exe /action:ACTION(IMPORT | EXPORT) /project:PROJECT /path:FOLDER_PATH")

## Caution ##
Make sure to provide correct values for the following configration settings in the app.config file:  
OctopusUri: Your octopus sverver URL  
ApiKey: Your Octopus API key  
DbConnectionString: Connection string to Octopus database"# OctopusDeploy-Project-Export-Import"   
