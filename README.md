# cdn
A utility for easing the pain of putting files on a CDN and how to link to them.
In general, when developing a web app, you want to run and debug while linking to local resources on your development system.
But when running in production, you want such files to reside in a CDN to improve performance and general load times.
However, CDNs will cache aggressively, so different versions of your CSS, JS and related files should have a unique URL.
This utility helps you accomplish running in both environments seamlessly with minimal setup.

## Setup

### Pre-requisites

The build of cdn.exe relies on the .NET Framework 4.7.2. Install this first.

### Azure

- Create an Azure storage account with CDN profile.  This process is described in detail here:  https://docs.microsoft.com/en-us/azure/cdn/cdn-create-a-storage-account-with-cdn
- Get the storage account connection string, container name and CDN Url.  These will be used in the configuration section below.

### Your code

- Create functions or methods in your application that refer to relative paths to javascript or CSS that are used in your web app. An example function might look something like this

```js
function getUrlForCSS(path) {
  // read the fingerprints json file
  var fingerprints = {}; // read this dictionary from the json file
  // if there's an entry for path, use it, otherwise use the local path
  var url = fingerprints[path] || path;
  return '<link href="' + url + '" rel="stylesheet" />';
}
```

- List all such local urls in the json fingerprints file like so:

```json
{
  "/dist/css/app.css": "",
  "/dist/js/app.js": ""
}
```

- Save the json in a file under the root content directory (e.g. /dist/fingerprints.json)  You will want to check this file into source control.

### Configuration

This application is command-line driven and does not prompt for any information.  
You can configure it in three ways.  The application will read the configuration values from these sources with the first one found being used

- Command line option in the form of `--keyName "key value"`
- Configuration file entry in `cdn.json`
- Configuration file entry in `cdn.exe.config`
- Environment variable name matching the keyName

An explanation of each setting can be found in [cdn.json](src/cdn.json)

## Running

You can run the application as part of your build process or CI pipeline or on a schedule.
You should insert the cdn upload step after a production build completes but before it is deployed.
The upload process will create unique file names for the files listed in the fingerprints file.
All other files in the target folder will be uploaded using their existing names.
e.g. `/css/app.css` becomes `/css/app-abcd1234.css` on the CDN but `/css/background.png` remains unchanged

## Working Example

The [example](example) folder contains a fully working version of this process methodology for a simple .NET Core web app.
For this application you could run `cdn --fileList wwwroot/content/fingerprints.json --containerName mycontainer --containerUrl https://storageaccountname.blob.core.windows.net/mycontainer --storageAccountConnectionString "<get this from the azure portal>"`
