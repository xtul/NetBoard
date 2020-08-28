# NetBoard

NetBoard is an imageboard API using ASP<span>.NET Core 3.1. It is a hobby project and heavily work-in-progress. Many things are subject to change.

For a web interface, please see my [NetChan.Frontend](https://github.com/xtul/NetChan.Frontend) repository.

I started this project to learn to work with .NET technology stack. The primary focus is to make maintenance/expansion simple. For example, if a board has it's name changed, migration should be painless. Adding and customizing new boards should also be straightforward.

# Features
### Currently available

 - Reading boards and their pages, threads, single posts
 - "Classic" and Catalog board view
 - Making new threads and responding to existing ones
 - Setting separate rules for boards, such as max pages or images
 - Password-based post deletion
 - Posting images
 - Storing user-sent images in queue/public directory
 - Generating image thumbnails
 - Reporting posts for administration

### Partially done
- Administration panel *(will be replaced by the NetChan.Frontend project)*
- OAuth2 authorization *(API can be protected but logging in still needs work)*
- Multiple RDBMS support *(only PostgreSQL was tested)*

### To do

- User banning
- Captcha *(likely using hCaptcha - currently IP rate limiter is used as a substitute)*
- Automatic DB table seeding
- ...

# Deployment

NetBoard is not yet ready for production.

The only tested method is to deploy NetBoard on your own server following Microsoft's documentation. A Docker image will be released when the project is ready.


# Contributing

Any help is greatly appreciated. Feel free to contribute to the code or report issues. 

To make changes to the codebase, you will need:

`Visual Studio 2019` - to edit and compile the project file (free version is fine),

`PostgreSQL 12` - or other supported RDBMS.

Enter `NetBoard.Backend` directory and rename `appsettings.default.json` to `appsettings.json`. Modify to your needs (eg. database connection string and currently used database).

After downloading all required NuGet packages, enter NuGet console. Migrate with `add-migration Initial` and `update-database`.

You should be now able to compile NetBoard and start making changes.
