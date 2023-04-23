# POC of how to use Managed Identity with older .net code using system.data.sqlclient.

## Intro
I had the huge pleasure of deploying a .NET Framework application to Azure.
To ensure highest level of security, and to avoid the need for storing credentials etc. in a key vault, I struggled to solve this.

I ended up doing the code you can find here.

If you use it, I hope you will keep a reference to me and my company.

We are a small startup focusing on Azure and SaaS solutions for making life easier and allowing IT administrators to focus on end-user value.

## Using it

Add the HarmonyLib nuget package and copy the .cs file to your project, replace TenantId in the code and ensure to call the Patch method as early as possible in your startup (before any db connections are made)


