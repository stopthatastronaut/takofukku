# Takofukku

## What?

たこ
**Tako: Octopus**

フック
**Fukku: Hook**

An Azure function that triggers [Octopus Deploy](https://octopus.com/) Projects from Github push event hooks. 

## What does it do?

When you push to github, it creates a release for you, adds the github head commit as release notes, then deploys the new release.


> **Retirement of Takofukku v1 is now imminent**

> It is nowadays much easier to just use a github action to trigger Octopus Processes. However some other use cases still exist for a webhook bridge, so v2 is under development and this particular repo will be retired in preparation for replacement.

> If you are using TakoFukku (and the stats suggest not many still are, though there DO seem to be one or two), you're advised to migrate your stuff over to a GitHub Action ASAP.

> The original hook.takofukku.io will stay running, though I can't guarantee it'll get any updates.


## Why?

*Don't care why? [Jump to the quickstart](#ok-so-how-do-i-hook-this-up)*

Because there's currently no built-in webhook solution for Octopus Deploy, and users often roll their own, or use a build server to do the triggering. Not every project *needs* a build server though.

Inspired by [Domain's Ops Code pipeline](http://tech.domain.com.au/2015/06/deploy-on-merge-in-domains-devops-repositories/), but with simplified config, a more tweakable deployment engine and a new concept called a **takofile**.

Takofukku is especially strong at deploying code that doesn't pre-built artifacts, so supports scenarios such as

- Using Octopus Deploy as a ghetto build/CI server
- Automating PowerShell tests on multiple targets with Pester
- Ops deployment projects that do simple git pulls, rather than full builds
- Packaging workflows driven out of github
- Run your tests, then call back to Github and create a Release if they pass
- Any project which doesn't use nuget packages
- Anything for which you want to run in Octopus as a post-push task - I _literally_ have projects that do nothing but email me and send me a slack message.

Of course, Octopus being Octopus, you can do pretty much anything with a bit of PowerShell, C#, Bash or F#. I look forward to seeing what kind of weird solutions this inspires, and what sort of things one might want to run on a git push.

## OK, so... What's a **takofile**?

A takofile is not unlike `appveyor.yml` or `.travis.yml`. It's a little file that lives in the root of your github repo, and defines a branch-to-environment mapping, a repo-to-project mapping, and some other common config bits.

## OK, so how do I hook this up?

Go to settings in your github repository and set up a webhook integration that captures the push event. Point that to

`https://hook.takofukku.io/api/Takofukku?apikey=<your octopus api key>`

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

You can add as many mappings as you like. If you don't provide mappings, Takofukku will default to master->Production, everwhere else->Staging

## What about channels?

At present, Takofukku only supports Default as a channel. Full channel support is coming soon. It also might not work on older versions of OD. Yet.

## It always creates a new release. What gives?

That option is not implemented just yet. But it will be soon.

## My repos are private. Can I still use it?

Yes, you can.

`https://hook.takofukku.io/api/Takofukku?apikey=<your octopus api key>&patoken=<github personal access token>`

## About Tokens and API keys

Takofukku doesn't store your tokens or API keys. The source code is in this very repo, so you can check that for yourself. However, it's still worth dedicating a specific API key and token solely to Takofukku, to make key rotation easier and to track exactly what Takofukku gets up to. It's a good idea to rotate these keys periodically, and this process can be automated. There's some [more info here](permissions.md)

While we're talking security, Do use HTTPS for your Octopus server. Github to Takofukku is encrypted, but Takofukku to Octopus is under your control, in your takofile. Do use https. Octopus now [natively supports LetsEncrypt](https://octopus.com/docs/administration/lets-encrypt-integration), so please use it.

## Does this mean I can use Octopus Deploy as a CI server?

Yes, you kinda can. Octopus can run F#, PowerShell, C#Script and bash, so if those languages can run your builds and tests, then Octopus *can* run builds for you. It's not really what Octopus is designed for, but it can work. But definitely don't neglect running tests. If it's PowerShell you're pushing out, I recommend [Pester](https://github.com/Pester/Pester), with an Octopus script step like this:

```
$result = Invoke-Pester -EnableExit
Fail-Step "$result failed Pester tests"
```

Which will run your tests and abort if they fail. To use that, Have a deploy step that throws your code in a sandbox location, then the tests, then move the deployed code into its target location. Like this project, for instance:

![](img/LightweightCI.png)

Yes, it pulls your PowerShell code, Pesters it, then if it passes, pushes it to Production. _Cheap PowerShell CI for the win_ (Yes, the example is lightweight, deliberately so)

A truly awesome version of this would use Octopus's Docker features to test in a disposable container before deploying. That would be very nice indeed. Feel free to try it and report back.

## Can I contribute?

In code, in money, or in beer. Yes.

## Can I fork this and run my own private Takofukku?

Sure. That's why it's open source. It runs on the Azure Functions platform, but shouldn't be too hard to adapt to other platforms. Please do contribute back in, though.

## Does this work with Bitbucket?

Not yet. Bitbucket's hook payload is slightly different, and I haven't written anything to support that difference. Soon though.

## Getting Support

"Ah, I set it all up and it's not working!"

Get in touch with [@cloudyopspoet on Twitter](https://twitter.com/cloudyopspoet) and I'll do what I can. Obviously I don't offer commercial support at the moment, but if you're interested in using this for something mission-critical, do let me know. 

## I know you. You've been talking about this for ages. Why did it take so long?

Shut up. I started writing it in Powershell, then decided C# would be better, remembered I don't really like C#, went back to PowerShell, got doubts about performance and scalability and then, eventually, threw it all away in favour of F#. Which is excellent.

Yes, I never finish anyth