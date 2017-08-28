## Permissions required for Takofukku

You're sending an API key to a third party. So let's just be clear about what's needed 

## Octopus Deploy:

The user with the API key will need the ability to

- Create a new release in the project and environment(s) specified in your Takofile.
- Deploy that release

The takofukku code in this repo is literally the code that's running in production, but don't take my word for it. 

Give takofukku the minimal rights, lock it down to just the project(s) you need it to see, and don't give it admin rights.

## Github

If your repo is public, Takofukku has all the rights it needs. The world can read your takofile, so can Takofukku.

If your repo is private, takofukku needs a personal access token with the ability to read the takofile from the root of the repo. It doesn't need anything else

As for Octopus, consider going minimal-permissions


## Questions

I am [@cloudyopspoet on twitter](https://twitter.com/cloudyopspoet). Ask me.
