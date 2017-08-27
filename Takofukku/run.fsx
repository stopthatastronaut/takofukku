#if INTERACTIVE

open System

#I @"C:/users/stopt/appdata/roaming/npm/node_modules/azure-functions-core-tools/bin"
#r "System.Web.Http.dll"
#r "System.Net.Http.Formatting.dll"
#r "Microsoft.Azure.WebJobs.Logging.dll"
#r "Microsoft.Extensions.Logging.dll"
#r "Microsoft.Azure.WebJobs.Host.dll"

open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Host
open Microsoft.Azure.WebJobs.Logging

#endif

#r "System.Net.Http"
#r "System.Net"
#r "Newtonsoft.Json"
#r "binaries/FSharp.data.dll"
#r "binaries/FSharp.data.DesignTime.dll"
#r "binaries/Octopus.Client.dll"
#r "binaries/SharpYaml.dll"
#r "binaries/FSharp.Configuration.dll"
#r "System.Configuration"

open SharpYaml
open FSharp.Data 
open FSharp.Data.JsonExtensions
open Octopus.Client
open System.Configuration
open System.Net
open System.Net.Http
open System.Text
open System.Net.Http.Headers
open System.IO
open Newtonsoft.Json
open FSharp.Core
open FSharp.Configuration

// models

[<Literal>]
let ModelPath =  __SOURCE_DIRECTORY__ + "/models/pushevent.json"
type PushEvent = JsonProvider<ModelPath>

[<Literal>]
let TakoFilePath = __SOURCE_DIRECTORY__ + "/models/takofile.yml"
type TakoFile = YamlConfig<FilePath = TakoFilePath>

// functions

let GetTakoFile(repo: String, token: String, log: TraceWriter) = 
    let targetfile = "https://raw.githubusercontent.com/"+repo+"/master/takofile"

    log.Info(sprintf "Requesting takofile from " + targetfile)

    let header = 
        match token with
        | "" -> 
            log.Info(sprintf "Takofukku found an empty token") |> ignore
            ["X-Tako-Client", "Takofukku/1.0"] 
        | _ ->   
            log.Info(sprintf "Takofukku found a non-empty token") |> ignore
            ["Authorization", "token " + token; "X-Tako-Client", "Takofukku/1.0"]  

    let takofile = Http.RequestString(   
                        targetfile,
                        headers = header
                        ) 
    log.Info(sprintf "Takofile retrieved from github") |> ignore
    takofile 

// refactor, not yet in use
let DeployFromOctopus(server: String, apikey: String, environment: String, project: String, log: TraceWriter) =
    log.Info(sprintf "Running the Octopus Deploy as a function")
    let endpoint = Octopus.Client.OctopusServerEndpoint(server, apikey)
    let octo = Octopus.Client.OctopusRepository(endpoint)

    log.Info(sprintf "Initialised client on " + server)

    let env = octo.Environments.FindByName(environment)
    let prj = octo.Projects.FindByName(project)
    let prc = octo.DeploymentProcesses.Get(prj.DeploymentProcessId)
    let chn = octo.Channels.FindByName(prj, "Default")  // only default channel for now
    let tmpl = octo.DeploymentProcesses.GetTemplate(prc, chn)

    log.Info(sprintf "grabbing template")

    let release = Octopus.Client.Model.ReleaseResource()
    release.ProjectId <- prj.Id
    release.Version <- tmpl.NextVersionIncrement
    for pkg in tmpl.Packages do
        let spkg = Octopus.Client.Model.SelectedPackage()
        spkg.StepName <- pkg.StepName
        spkg.Version <- release.Version
        release.SelectedPackages.Add(spkg)
    
    log.Info(sprintf "Created a new release with version" + release.Version)

    let cRel = octo.Releases.Create(release)
    let depl = Octopus.Client.Model.DeploymentResource()
    depl.ReleaseId <- cRel.Id
    depl.ProjectId <- prj.Id
    depl.EnvironmentId <- env.Id
    octo.Deployments.Create(depl) |> ignore

let CreateReleaseNotes(headcommit: Object) = //placeholder
   // for refactoring the ugly release notes away
   // will use a nice neat Seq operation, I suspect
    ""


// main request responder
let Run(req: System.Net.Http.HttpRequestMessage, log: TraceWriter) =
    async {
        log.Info(sprintf 
            "Takofukku started")        

        let qs = 
            HttpRequestMessageExtensions.GetQueryNameValuePairs(req)
        // Set name to query string
        let octopusAPIKey =
            qs |> Seq.tryFind (fun q -> q.Key = "apikey") 

        let gittoken =
            qs |> Seq.tryFind (fun q -> q.Key = "patoken")

        // make our octopus key exception-safe
        let ok =
            match octopusAPIKey with
            | None -> 
                log.Info(sprintf "I don't have an octopus API Key") |> ignore 
                ""
            | Some x ->
                log.Info(sprintf "I have an octopus API key ") |> ignore
                x.Value
        
        // make our token exception-safe
        let gt = 
            match gittoken with
            | None -> 
                log.Info(sprintf "I don't have a git token") |> ignore
                ""
            | Some x ->
                log.Info(sprintf "I have a git token ") |> ignore
                x.Value

        log.Info(sprintf "Reading async from post body")
        let! data = req.Content.ReadAsStringAsync() |> Async.AwaitTask
        log.Info(sprintf "Post body read") 

        if not (String.IsNullOrEmpty(data)) then
                log.Info(sprintf "We have a post body : " + data)
                let EventData = PushEvent.Parse(data)

                // big ugly string builder for the release notes. I'm getting tired. Shhhh.
                let msg = StringBuilder()
                msg.AppendLine("Release Created by [Takofukku](https://github.com/stopthatastronaut/takofukku)") |> ignore
                msg.AppendLine("") |> ignore
                msg.AppendLine("") |> ignore
                msg.AppendLine("*Head Commit*:") |> ignore
                msg.AppendLine("") |> ignore 
                msg.AppendLine("") |> ignore
                msg.Append("[") |> ignore
                msg.Append(EventData.HeadCommit.Message) |> ignore
                msg.Append("](") |> ignore
                msg.Append(EventData.HeadCommit.Url) |> ignore
                msg.Append(")") |> ignore
                msg.Append(" - [") |> ignore
                msg.Append(EventData.HeadCommit.Author.Username) |> ignore
                msg.Append("](mailto:") |> ignore
                msg.Append(EventData.HeadCommit.Author.Email) |> ignore
                msg.Append(")") |> ignore

                let releasenotes = msg.ToString()

                // split out the ref 
                let targetbranch = 
                    let refsplit = EventData.Ref.Split [|'/'|]
                    refsplit.[2]

                log.Info(sprintf 
                    "We have parsed our post body. Push event arrived from repo " + 
                    EventData.Repository.FullName + 
                    "on branch " +
                    EventData.Ref)  // we need to split that ref
                let tako = GetTakoFile(EventData.Repository.FullName, gt, log) 
                // make that string into an object using the YAML type provider
                let tk = TakoFile()
                tk.LoadText(tako)

                let srv = tk.Server
                let proj = tk.Project

                log.Info(sprintf "Takofile server: " + srv.OriginalString + 
                                " Takofile project: " + proj + 
                                " Target branch: " + targetbranch)


                
                // figure out which branch goes to which environment
                let branchmapping = 
                    tk.Mappings
                    |> Seq.tryFind (fun q -> q.Branch = targetbranch)
                
                let bm =
                    match branchmapping with
                    | None ->
                        log.Info(sprintf "I don't have a branchmapping") |> ignore
                        if targetbranch = "master" then
                           "Production"
                        else
                            "Staging"
                    | Some x ->
                        log.Info(sprintf "My branch mapping is " + x.Environment) |> ignore
                        x.Environment


                // find the branch mapping
                let targetenv = bm
                

                log.Info(sprintf "We've pushed the branch " + EventData.Ref + " and found env: " + 
                            targetenv)


                let endpoint = Octopus.Client.OctopusServerEndpoint(srv.OriginalString, ok)
                let octo = Octopus.Client.OctopusRepository(endpoint)

                log.Info(sprintf "Initialised client on " + srv.OriginalString)


                let env = octo.Environments.FindByName(targetenv)
                let prj = octo.Projects.FindByName(proj)
                let prc = octo.DeploymentProcesses.Get(prj.DeploymentProcessId)
                let chn = octo.Channels.FindByName(prj, "Default")  // only default channel for now
                let tmpl = octo.DeploymentProcesses.GetTemplate(prc, chn)

                log.Info(sprintf "grabbing template")

                let release = Octopus.Client.Model.ReleaseResource()
                release.ProjectId <- prj.Id
                release.Version <- tmpl.NextVersionIncrement
                release.ReleaseNotes <- releasenotes // "Created by Takofukku" // add detailed commit messages here
                for pkg in tmpl.Packages do
                    let spkg = Octopus.Client.Model.SelectedPackage()
                    spkg.StepName <- pkg.StepName
                    spkg.Version <- release.Version
                    release.SelectedPackages.Add(spkg)
                
                log.Info(sprintf "Created a new release with version" + release.Version)

                let cRel = octo.Releases.Create(release)
                let depl = Octopus.Client.Model.DeploymentResource()
                depl.ReleaseId <- cRel.Id
                depl.ProjectId <- prj.Id
                depl.EnvironmentId <- env.Id
                octo.Deployments.Create(depl) |> ignore

                log.Info(sprintf "deployment triggered")

                printfn ""  
                return req.CreateResponse(HttpStatusCode.OK, """{"result" : "ok"}""")
        else
            // no data posted. 
            log.Info(sprintf
                "No data was posted. Invalid request") // print usage at this point

            let usagebody = File.ReadAllText(__SOURCE_DIRECTORY__ + "/usage.txt")
            return req.CreateResponse(HttpStatusCode.OK, usagebody) 

    } |> Async.RunSynchronously  
