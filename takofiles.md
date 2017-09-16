# Takofiles

## What is a takofile?

A takofile is a simple YAML file which outlines some details of your Octopus Instance

An example structure may be as follows

```
---
Server: https://deploy.mycompany.com/
Project: Important Business App
Mappings:
  - 
    Branch: master
    Environment: Production
  - 
    Branch: release
    Environment: Staging
  -
    Branch: develop
    Environment: Dev
CreateRelease: true

````

##### Server

Please use HTTPS here. This is just the address of your Octopus Depoy server. Pretty easy

##### Project

This is the Octopus project related to your github repository. There is a one-to-one mapping of Repository to Project - that is a Takofile can only have one Server and one Project at the present time. Of course, you can have multiple repositories pointing to the same server, or even the same project. 

##### Mappings

This is where you map your [github branch](https://help.github.com/articles/creating-and-deleting-branches-within-your-repository/) to a target environment in Octopus. How you branch is up to you, and it [can be a big topic](https://www.atlassian.com/agile/branching). All you need to know here though is that Takofukku allows you to specify any number of branch-environment mappings, and a push onto a given branch will always trigger a deploy to the corresponding environment.

##### CreateRelease

At the time of writing, a Takofukku invocation always creates new release in Octopus. This is good because you get neat release notes referring you to the current [head](https://www.youtube.com/watch?v=ZaI1co-rt9I). In future there will be an option to switch this off, but chances are it will always default to creating a new release. So this parameter, for now, is optional.

## OK, so what do I do with this takofile?

Simply drop it into the root folder of your repository, and add a new `push` webhook, with application/json as the payload type, and you're done.

Do a new commit to your repo, and watch as Octopus starts running.