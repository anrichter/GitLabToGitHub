# GitLabToGitHub

Easy to use Command Line Tool to migrate GitLab Projects to GitHub Repositories.

Select a GitLab Group and a Project, give a name for the new Repository, decide if it is a Private one and let the tool do their work.

## Features

* Browse GitLab Groups / Projects
* Migrates:
  * Git Repository
  * Users / Collaborators with configurable Username Mapping
  * Milestones
  * Issues
  * Issue Comments
  * Labels
* Log manual rework you have to do after Migration
* Platform independent through .NET Core

## Limitiations

* Migrated Issues are creatd by Access Token User
* Migrated Issues are created at Migration time
* Collaborators are created but can't assigned to Issues until thei accept the invitation as a collaborator
* Can't migrate attachments like images

## How to use 

* Install .NET Core 2.2 SDK
* Create a GitLab Access Token with Scope **api**
* Create a GitHub Access Token with Scope **repo**
* Clone the Repository
* Copy `appsettings.json` to `appsettings.production.json` and adjust it
* `dotnet run -c Release`

## Open Source Libraries

* [GitLabApiClient](https://github.com/nmklotas/GitLabApiClient) GitLab API client for .NET
* [LibGitSharp](https://github.com/libgit2/libgit2sharp/) Git Library with .NET API
* [Octokit.net](https://github.com/octokit/octokit.net) A GitHub API client library for .NET