<# Buzz machine postbuild script v1

Example usage (for project post-build command):

powershell -file "$(SolutionDir)postbuild.ps1" -debug -machinefile "$(TargetPath)" -packageroot "$(SolutionDir)Package" -type "Effect" -platform $(platform)

-debug         if -debug is present, the script will just tell you what it would do and no files will be copied.
-machinefile = path to machine dll (use $(TargetPath) in post-build command)
-packageroot = path to distribution package folder. If set, files will be copied to this location using the correct Buzz folder structure. "$(SolutionDir)Package" works well in the post-build command.
-type        = Effect or Generator (required)
-platform    = x86, x64 or Win32 (use $(platform) in post-build command)
-filelist    = text file containing a list of extra files to copy eg. demo.bmx, help files etc. specified as: filename [@ destination]
               Destination is relative to the relevant Generators or Effects folder as determined by the -type parameter

eg.

MyMachine Demo.bmx
.\Docs\MyMachine.html
.\Docs\*.html @ MyMachine\Docs\
.\Docs\*.jpg @ MyMachine\Docs\
.\Docs\*.css @ MyMachine\Docs\

If -filelist is not specified, the script will use "postbuild.files.txt" in the same folder, if it exists.

#>

# Validate input
param([switch]$elevated, [switch]$debug, $scriptpath, $machinefile, $packageroot="", $type, $platform="x86", $filelist="postbuild.files.txt")

Write-Output "IX postbuild script"
Write-Output "machinefile = `"$machinefile`""
Write-Output "platform = `"$platform`""
Write-Output "type = `"$type`""
Write-Output ""

function Check-Admin
{
   $currentUser = New-Object Security.Principal.WindowsPrincipal $([Security.Principal.WindowsIdentity]::GetCurrent())
   $currentUser.IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
}

# Admin script launches in windpws\system32 so we need to get back to the correct folder.
if(-not $elevated)
{
    $scriptpath = Split-Path $MyInvocation.MyCommand.Path
}
else
{
    Set-Location "$scriptpath"
}

$errors = 0
$fileerrors = 0
Write-Output ""
if(-not $machinefile)
{
    Write-Output "ERROR: Missing required parameter -machinefile"
    $errors += 1
}
elseif(-not(Test-Path -path $machinefile))
{
    $machinefile = $machinefile.Trim()
    Write-Output "ERROR: $machinefile not found."
    $errors += 1
}

if(-not $type)
{
    Write-Output "ERROR: Missing required parameter -type"
    $errors += 1
}
else # Set the appropriate gear path for machine type
{
    $type = $type.Trim()
    switch($type)
    {
        "Generator"  { $relativepath = "Gear\Generators\" }

        "Effect"     { $relativepath = "Gear\Effects\" }

        default
        {
            Write-Output "ERROR: Invalid value for -type ($type)"
            $errors += 1
        }
    }
}

$buzzroot32 = Get-ItemPropertyValue 'Registry::HKCU\Software\Jeskola\Buzz\Settings' -Name BuzzPath32
$buzzroot64 = Get-ItemPropertyValue 'Registry::HKCU\Software\Jeskola\Buzz\Settings' -Name BuzzPath64

$platform = $platform.Trim()
switch($platform) # Find platform appropriate path from the registry
{
    "Win32" { $platform = "x86" }
    "x86" {  }
    "x64" {     }
    "AnyCPU" { }
    default
    {
        Write-Output "ERROR: Invalid platform ($platform)"
        $errors += 1
    }
}

if($packageroot -ne "")
{
    $packageroot = $packageroot.Trim()
    $packagepath32 = "$packageroot\x86\$relativepath"
    $packagepath64 = "$packageroot\x64\$relativepath"
}

$filelist = $filelist.Trim()
if(Test-Path -path $filelist)
{
    Write-Output "Building file list..."
    $copylist = @{}
    $regex = [regex] '^\s*(?<source>[^@]+)\s*@?\s*(?<dest>.*)?\s*$'
    foreach($line in Get-Content $filelist)
    {
        if($line -match $regex)
        {
            $source = $matches['source'].Trim()
            $dest = $matches['dest'].Trim()
            $copylist.Add($source, $dest)
        }
    }

    foreach($key in $copylist.Keys)
    {
        $source = $key;
        $dest = $copylist[$key]
        if(-not(Test-Path -Path "$source"))
        {
            Write-Output "Source not found: $source "
            $fileerrors += 1
        }
    }
}

# Set destination folders
$buzzroot32 = $buzzroot32.Trim()
$buzzpath32 = $buzzroot32 + $relativepath
$buzzroot64 = $buzzroot64.Trim()
$buzzpath64 = $buzzroot64 + $relativepath

if($debug)
{
    Write-Output ""
    Write-Output "DEBUG INFO:"
    Write-Output "scriptpath: $scriptpath"
    Write-Output "elevated: $elevated"
    Write-Output "machinefile: $machinefile"
    Write-Output "platform: $platform"
    Write-Output "relativepath: $relativepath"
    Write-Output "buzzroot32: $buzzroot32"
    Write-Output "buzzpath32: $buzzpath32"
    Write-Output "buzzroot64: $buzzroot64"
    Write-Output "buzzpath64: $buzzpath64"
    Write-Output "packageroot: $packageroot"
    Write-Output "packagepath32: $packagepath32"
    Write-Output "packagepath64: $packagepath64"
    Write-Output ""
    Write-Output "filelist: $filelist"
    Write-Output "..."
    foreach($key in $copylist.Keys)
    {
        $source = $key
        $dest = $copylist[$key]

        if(Test-Path -Path "$source")
        {
            Write-Output "source: `"$source`""
        }
        else
        {
            Write-Output "source (missing): `"$source`""
        }
        Write-Output "dest: `"$dest`""
    }
    Write-Output "..."
}

if($errors -gt 0)
{

    Write-Output ""
    Write-Output "Too many errors! Bye!"
    exit
}
elseif($fileerrors -gt 0)
{
    $options = Write-Output Y N
    switch($host.UI.PromptForChoice("Continue?", "Info", $options, 1))
    {
        "1"
        {
            Write-Output "Bye then!"
            exit
        }
        default { Write-Output "Okeydokey..." }
    }
}

# Attempt to launch this script with admin privileges if it doesn't have them already
if((Check-Admin) -eq $false)
{
    if($elevated -eq $false)
    {
        Write-Output "Launching elevated post-build script..."
        $arglist = "-noprofile"
        if($debug) { $arglist += " -noexit" }
        $arglist += " -file `"{0}`" -elevated -scriptpath `"$scriptpath`""
        if($debug) { $arglist += " -debug" }
        $arglist += " -machinefile `"$machinefile`" -packageroot `"$packageroot`" -type $type -platform $platform"
        if($filelist)
        {
            $arglist += " -filelist `"$filelist`""
        }

        if($debug)
        {
            Write-Output ""
            Write-Output "Arguments for admin script:"
            Write-Output "$arglist"
        }
        Start-Process powershell.exe -Verb RunAs -ArgumentList ("$arglist" -f $myinvocation.MyCommand.Definition)
    }
    exit # Bail out of non-privileged instance
}

function CopyFiles
{
    param ($destpath)
    $destpath = $destpath.Trim()

    if(-not (Test-Path -Path $destpath)) #Create destpath if it doesn't exit
    {
        if($debug)
        {
            Write-Output "DEBUG: Skipped creating folder `"$destpath`""
        }
        else
        {
            Write-Output "Creating folder $destpath"
            New-Item -ItemType "directory" -Path "$destpath"
        }
    }

    if($debug)
    {
        Write-Output "DEBUG: copy $machinefile to $destpath"
    }
    else
    {
        Write-Output "Copying $machinefile to $destpath"
        Copy-Item "$machinefile" "$destpath"
    }

    if($copylist)
    {
        Write-Output "Copying additional files..."
        foreach($key in $copylist.Keys)
        {
            $source = $key
            $dest = $destpath + $copylist[$key]

            if(-not (Test-Path -Path $dest))
            {
                if($debug)
                {
                    Write-Output "DEBUG: Create folder `"$dest`""
                }
                else
                {
                    Write-Output "Folder does not exist: `"$dest`""
                    New-Item -ItemType "directory" -Path "$dest"
                }
            }

            if(Test-Path -Path "$source")
            {
                if($debug)
                {
                    Write-Output "DEBUG: copy file `"$source`" to `"$dest`""
                }
                else
                {
                    Write-Output "Copying `"$source`" to `"$dest`""
                    Copy-Item -Path "$source" -Destination "$dest"
                }
            }
            else
            {
                Write-Output "Skipping missing file: `"$source`""
            }
        }
    }
}

# Copy file to package folders if specified
switch($platform)
{
    "x86"
    {
        if($packagepath32 -ne "")
        {
            Write-Output 'Building package...'
            CopyFiles($packagepath32)
        }
    }

    "x64"
    {
        if($packagepath64 -ne "")
        {
            Write-Output 'Building package...'
            CopyFiles($packagepath64)
        }
    }

    "AnyCPU"
    {
        if($packagepath32 -ne "")
        {
            Write-Output 'Building package...'
            CopyFiles($packagepath32)
        }

        if($packagepath64 -ne "")
        {
            Write-Output 'Building package...'
            CopyFiles($packagepath64)
        }
    }

    default
    {
    }
}


# Copy file to buzz path if it was found
switch($platform) # Find platform appropriate path from the registry
{
    "x86"
    {
        if($buzzpath32 -ne "")
        {
            Write-Output 'Copying to Buzz x86 folders...'
            CopyFiles($buzzpath32)
        }
    }

    "x64"
    {
        if($buzzpath64-ne "")
        {
            Write-Output 'Copying to Buzz x64 folders...'
            CopyFiles($buzzpath64)
        }
    }

    "AnyCPU"
    {
        if($buzzpath32 -ne "")
        {
            Write-Output 'Copying to Buzz x86 folders...'
            CopyFiles($buzzpath32)
        }

        if($buzzpath64-ne "")
        {
            Write-Output 'Copying to Buzz x64 folders...'
            CopyFiles($buzzpath64)
        }
    }

    default
    {
    }
}


