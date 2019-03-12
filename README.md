## FixBindingRedirects

Small utility to fix issues with loading DLLs in your ASP.NET application due to out of date 
binding redirect entries in your WEB.config files.

Just run in the folder where your WEB.config file (and BIN folder with DLLs under) resides,
and it will update existing binding redirect entries in WEB.config file to latest versions
of DLLs under your BIN directory.