# TEKA3003 Miniprojekti

Backlog
https://docs.google.com/spreadsheets/d/1KE2dFqVVBGNNGbfewPiMjXgGZsPpuXjqI6NuIx-8sjQ/edit?usp=sharing

[![.NET](https://github.com/Kirottu/miniprojekti/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Kirottu/miniprojekti/actions/workflows/dotnet.yml)


## Installation

1. Download .NET 10 from https://dotnet.microsoft.com/en-us/download
2. Clone repo
3. Configure appropriate OIDC settings in `appsettings.json`
4. Use command `dotnet run` in project root. Make sure the environment variable `CLIENT_SECRET` is set to the secret from your OIDC provider.
5. Default Browser should open automatically or command line will link to the localhost port page
