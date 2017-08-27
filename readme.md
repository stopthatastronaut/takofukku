# Takofukku

## What?

たこ
**Tako: Octopus**

フック
**Fukku: Hook**

An Azure function that triggers Octopus Deploy from from Github push event hooks. 

## Why?

Because there's currently no built-in webhook solution for Octopus deploy, and users often roll their own, or use a build server to trigger. Not every project *needs* a build server though.

Inspired by [Domain's Ops Code pipeline](http://tech.domain.com.au/2015/06/deploy-on-merge-in-domains-devops-repositories/), but with simplified config, a more tweakable deployment engine and a new concept called a **takofile**.

## Ooooh. What's a takofile?

A takofile is not unlike `appveyor.yml` or `.travis.yml`. It's a little file that lives in the root of your github repo, and defines a branch-to-environment mapping, a repo-to-project mapping, and some other common config bits.

## OK, so how do I hook this up?

Go to settings in your github repository and set up a webhook integration that captures the push event. Point that to

`https://hook.takofukku.io/Takofukku?apikey=<your octopus api key>`

Then in the root of your repo, add a takofile as follows

```
---
Server: https://deploy.d.evops.co/
Project: Takofukku
Mappings:
  - 
    Branch: master
    Environment: Production
  - 
    Branch: release
    Environment: Staging
  - 
    Branch: develop
    Environment: UAT
CreateRelease: true
```

You can add as many mappings as you like. If you don't provide mappings, Takofukku will default to master->Production.

## My repos are private. Can I still use it?

Yes, you can.

`https://hook.takofukku.io/Takofukku?apikey=<your octopus api key>&accesstoken=<github personal access token>`

## About Tokens and API keys

Takofukku doesn't store your tokens or API keys. The source code is in this very repo, so you can check that for yourself. However, it's still worth dedicating a specific API key and token solely to Takofukku, to make key rotation easier. It's a good idea to rotate these keys periodically, and this process can be automated.

While we're talking security, Do use https for your server. Github to Takofukku is encrypted, but Takofukku to Octopus is under your control, in your takofile. Do use https. Octopus now natively supports LetsEncrypt, so please use it.

## Does this mean I can use Octopus Deploy as a CI server?

Yes, you kinda can. Octopus can run F#, PowerShell, C#Script and bash, so if those languages can run your builds and tests, then Octopus *can* run builds for you. It's not really what Octopus is designed for, but it can work. But definitely don't neglet running tests. If it's powerShell you're pushing out, I recommend [Pester](https://github.com/Pester/Pester), with an Octopus script step like this:

```
$result = Invoke-Pester -EnableExit
EXIT $result
```

Which will run your tests and abort if they fail. To use that, Have a deploy step that throws your code in a sandbox location, then the tests, then move the deployed code into its target location. A truly awesome version of this would use Octopus's Docker features to test in a disposable container before deploying. That would be very nice indeed.

## Can I contribute?

In code, in money, or in beer. Yes.