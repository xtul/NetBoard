<p float="left">
  <img src="https://raw.githubusercontent.com/xtul/NetBoard/master/NetBoard.Backend/wwwroot/img/logo-sm.png" />
  <h1>NetBoard</h1>
</p>

NetBoard is an imageboard API using ASP<span>.NET Core 3.1. It is a hobby project and heavily work-in-progress. Many things are subject to change. A live version is available on [https://netchan.nl/](https://netchan.nl/).

For a web interface, please see my [NetChan](https://github.com/xtul/NetChan) repository.

I started this project to learn to work with .NET technology stack. The primary focus is to make maintenance/expansion simple. For example, if a board has it's name changed, migration should be painless. Adding and customizing new boards should also be straightforward.

Note: Both NetBoard and NetChan are currently undergoing major changes, with redesigns or complete rewrites of certain features.

# Features
### Currently available

 - Reading boards and their pages, threads, single posts
 - "Classic" and List board view
 - Making new threads and responding to existing ones
 - Posting protected by hCaptcha
 - Setting separate rules for boards, such as max pages or images
 - Password-based post deletion
 - Posting images
 - Storing user-sent images in queue/public directory
 - Generating image thumbnails
 - Reporting posts for administration
 - IP bans *(middleware filters IPs that are in appsettings.json and/or in DB)*
 - Shadow bans *(posts made by shadow banned IPs are only visible to them)*
 - VPN blocking *(thanks to https://github.com/ejrv/VPNs)*

### Partially done
- Administration panel *(will be replaced by the NetChan frontend project)*
- OAuth2 authorization *(API can be protected but logging in still needs work)*
- Multiple RDBMS support *(only PostgreSQL was tested)*

# Contributing

Any help is greatly appreciated. Feel free to contribute to the code or report issues. 

To make changes to the codebase, you will need:

`Visual Studio 2019` - to edit and compile the solution (free version is fine),

`PostgreSQL 13` - or other supported RDBMS.

Make a fork of this repository. Open Visual Studio and press "open a project from repository". Point it to your fork and let it download.

Open `NetBoard.Backend` project and rename `appsettings.default.json` to `appsettings.json`. Modify to your needs (eg. database connection string and currently used database). You may also need to set `NetBoard.Backend` as a starting project.

After downloading all required NuGet packages, enter NuGet console. Migrate with `add-migration Initial` and `update-database`.

You should be now able to compile NetBoard and start making changes.
