param($InstallPath, $ToolsPath, $Package, $Project)

Set-StrictMode -version 2.0

##-------------------------------------------------
## Globals
##-------------------------------------------------

New-Variable -Name ProductTrialKeyPageProductIdPlaceholder -Value '%productId' -Option ReadOnly
New-Variable -Name ProductTrialKeyPage -Value "https://trial.xceed.com/?product_name=$ProductTrialKeyPageProductIdPlaceholder" -Option ReadOnly

[System.Collections.Hashtable] $PackageMap = @{  
    'Xceed.Maui.Toolkit.Plus' = @{ ProductIds = @('TKM') }  
    'Xceed.Products.Grid.Full' = @{ ProductIds = @('GRD') }
    'Xceed.Products.Ftp.Full' = @{ ProductIds = @('FTN') }    
    'Xceed.Products.RealTimeZip.Full' = @{ ProductIds = @('ZRT') }
    'Xceed.Products.SFtp.Full' = @{ ProductIds = @('SFT') }
    'Xceed.Products.Tar.Full' = @{ ProductIds = @('ZIN') }
    'Xceed.Products.Wpf.DataGrid.Base' = @{ ProductIds = @('DGP') }
    'Xceed.Products.Wpf.DataGrid.Full' = @{ ProductIds = @('DGP') }
    'Xceed.Products.Wpf.DataGrid.Themes' = @{ ProductIds = @('DGP') }
    'Xceed.Products.Wpf.Toolkit.AvalonDock' = @{ ProductIds = @('WTK') }
    'Xceed.Products.Wpf.Toolkit.AvalonDock.Themes' = @{ ProductIds = @('WTK') }
    'Xceed.Products.Wpf.Toolkit.Base' = @{ ProductIds = @('WTK') }
    'Xceed.Products.Wpf.Toolkit.Base.Themes' = @{ ProductIds = @('WTK') }
    'Xceed.Products.Wpf.Toolkit.Full' = @{ ProductIds = @('WTK') }
    'Xceed.Products.Wpf.Toolkit.ListBox' = @{ ProductIds = @('WTK') }
    'Xceed.Products.Wpf.Toolkit.ListBox.Themes' = @{ ProductIds = @('WTK') }
    'Xceed.Products.Zip.Full' = @{ ProductIds = @('ZIN') }
    'Xceed.Workbooks.NET' = @{ ProductIds = @('WBN') }
    'Xceed.Words.NET' = @{ ProductIds = @('WDN') }
}

##-------------------------------------------------
## Functions
##-------------------------------------------------

function Find-XceedPackageObject {
    param([Parameter(Mandatory)][string] $PackageId)

    [string] $Key = if( $PackageId -ne $null ) { $PackageId } else { '' }

    return $PackageMap[$Key]
}

function Get-XceedPackageProductIds {
    param([Parameter(Mandatory)][string] $PackageId)

    [System.Collections.Hashtable] $PackageObject = Find-XceedPackageObject -PackageId $PackageId

    if( $PackageObject -ne $null )
    {
        return $PackageObject.ProductIds
    }
    else
    {
        return $null
    }
}

function Get-XceedProductTrialKeyPage {
    param([Parameter(Mandatory)][string] $ProductId)

    if( $ProductId -ne $null )
    {
        return $ProductTrialKeyPage.Replace( $ProductTrialKeyPageProductIdPlaceholder, $ProductId )
    }
    else
    {
        return $null
    }
}

function Get-XceedPackageTrialKeyPages {
    param([Parameter(Mandatory)][string] $PackageId)

    [System.Collections.Hashtable] $PageSet = @{}
    [array] $ProductIds = Get-XceedPackageProductIds -PackageId $PackageId
    $ProductIds = if( $ProductIds -ne $null ) { $ProductIds } else { @() }

    foreach( $ProductId in $ProductIds )
    {
        [string] $Url = Get-XceedProductTrialKeyPage -ProductId $ProductId

        if( ( $Url -ne $null ) -and ( $Url.Length -gt 0 ) )
        {
            $PageSet[$Url] = $true
        }
    }

    return $PageSet.Keys
}

function Open-XceedUrlInBrowser {
    param([Parameter(Mandatory)][string] $Url)

    if( ( $Url -ne $null ) -and ( $Url.Length -gt 0 ) )
    {
        #Get Default browser
        $DefaultSettingPath = 'HKCU:\SOFTWARE\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice'
        $DefaultBrowserName = (Get-Item $DefaultSettingPath | Get-ItemProperty).ProgId
    
        #Handle for Edge
        ##edge will not open with the specified shell open command in the HKCR.
        if($DefaultBrowserName -eq 'AppXq0fevzme2pys62n3e0fbqa7peapykr8v')
        {
            #Open url in edge
            Start Microsoft-edge:$Url
        }
        else
        {
            try
            {
                #Create PSDrive to HKEY_CLASSES_ROOT
                $null = New-PSDrive -PSProvider registry -Root 'HKEY_CLASSES_ROOT' -Name 'HKCR'

                #Get the default browser executable command/path
                $DefaultBrowserOpenCommand = (Get-Item "HKCR:\$DefaultBrowserName\shell\open\command" | Get-ItemProperty).'(default)'
                $DefaultBrowserPath = [regex]::Match($DefaultBrowserOpenCommand,'\".+?\"')

                #Open URL in browser
                Start-Process -FilePath $DefaultBrowserPath ('--new-window', $Url)
            }
            catch
            {
                Throw $_.Exception
            }
            finally
            {
                #Clean up PSDrive for 'HKEY_CLASSES_ROOT
                Remove-PSDrive -Name 'HKCR'
            }
        }
    }
}

##-------------------------------------------------
## Entry Point (Main)
##-------------------------------------------------

$Urls = Get-XceedPackageTrialKeyPages -PackageId $Package.Id

foreach( $Url in $Urls)
{
    Open-XceedUrlInBrowser -Url $Url
}
