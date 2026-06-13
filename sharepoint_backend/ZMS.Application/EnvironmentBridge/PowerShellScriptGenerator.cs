namespace ZMS.Application.EnvironmentBridge;

public sealed class PowerShellScriptGenerator : IPowerShellScriptGenerator
{
    public IReadOnlyDictionary<string, string> GenerateScripts(EnvironmentConfig config)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["scripts/lib/Zms.Logging.ps1"] = GenerateLoggingLibrary(),
            ["scripts/lib/Zms.Config.ps1"] = GenerateConfigLibrary(),
            ["scripts/lib/Zms.SharePoint.ps1"] = GenerateSharePointLibrary(),
            ["scripts/lib/Zms.Validation.ps1"] = GenerateValidationLibrary(),
            ["scripts/lib/Zms.Reporting.ps1"] = GenerateReportingLibrary(),
            ["scripts/00-Check-Prerequisites.ps1"] = GeneratePrerequisitesScript(),
            ["scripts/01-Create-SiteCollections.ps1"] = GenerateSiteCollectionsScript(),
            ["scripts/02-Create-Subsites.ps1"] = GenerateSubsitesScript(),
            ["scripts/03-Create-Libraries-Lists-Metadata.ps1"] = GenerateLibrariesListsMetadataScript(),
            ["scripts/04-Create-Groups-Permissions.ps1"] = GenerateGroupsPermissionsScript(),
            ["scripts/05-Create-Folders-And-SampleFiles.ps1"] = GenerateFoldersAndSampleFilesScript(),
            ["scripts/06-Apply-Migration-EdgeCases.ps1"] = GenerateEdgeCasesScript(),
            ["scripts/07-Generate-InventoryReport.ps1"] = GenerateInventoryReportScript(),
            ["scripts/08-Run-Preflight.ps1"] = GenerateRunPreflightScript(),
            ["scripts/09-Run-DryRun.ps1"] = GenerateRunDryRunScript(),
            ["scripts/10-Run-All-Safe.ps1"] = GenerateRunAllSafeScript(),
            ["scripts/11-Run-Discovery-ReadOnly.ps1"] = GenerateDiscoveryReadOnlyScript()
        };
    }

    private static string GenerateLoggingLibrary()
    {
        return """
        Set-StrictMode -Version Latest

        $script:ZmsPackageRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
        $script:ZmsCurrentScriptName = "ZMS"
        $script:ZmsLogPath = $null
        $script:ZmsVerboseLogging = $false

        function Get-ZmsLogPath {
            $logsPath = Join-Path $script:ZmsPackageRoot "logs"
            if (-not (Test-Path $logsPath)) {
                New-Item -ItemType Directory -Force -Path $logsPath | Out-Null
            }

            if (-not $script:ZmsLogPath) {
                $script:ZmsLogPath = Join-Path $logsPath ("zms-execution-{0}.log" -f (Get-Date -Format "yyyy-MM-dd"))
            }

            return $script:ZmsLogPath
        }

        function Write-ZmsLog {
            param(
                [Parameter(Mandatory = $true)][string]$Level,
                [Parameter(Mandatory = $true)][string]$Message,
                [ConsoleColor]$Color = [ConsoleColor]::Gray
            )

            $timestamp = Get-Date -Format "yyyy-MM-ddTHH:mm:ssK"
            $line = "{0} [{1}] [{2}] {3}" -f $timestamp, $Level.ToUpperInvariant(), $script:ZmsCurrentScriptName, $Message
            Add-Content -Path (Get-ZmsLogPath) -Value $line

            if ($Level -ne "DEBUG" -or $script:ZmsVerboseLogging) {
                Write-Host $line -ForegroundColor $Color
            }
        }

        function Write-ZmsInfo {
            param([Parameter(Mandatory = $true)][string]$Message)
            Write-ZmsLog -Level "INFO" -Message $Message -Color Gray
        }

        function Write-ZmsSuccess {
            param([Parameter(Mandatory = $true)][string]$Message)
            Write-ZmsLog -Level "SUCCESS" -Message $Message -Color Green
        }

        function Write-ZmsWarning {
            param([Parameter(Mandatory = $true)][string]$Message)
            Write-ZmsLog -Level "WARNING" -Message $Message -Color Yellow
        }

        function Write-ZmsError {
            param([Parameter(Mandatory = $true)][string]$Message)
            Write-ZmsLog -Level "ERROR" -Message $Message -Color Red
        }

        function Write-ZmsStep {
            param([Parameter(Mandatory = $true)][string]$Message)
            Write-ZmsLog -Level "STEP" -Message $Message -Color Cyan
        }

        function Start-ZmsTranscript {
            param(
                [string]$ScriptName = "ZMS",
                [switch]$VerboseLogging
            )

            $script:ZmsCurrentScriptName = $ScriptName
            $script:ZmsVerboseLogging = [bool]$VerboseLogging
            Write-ZmsInfo "Started $ScriptName"
        }

        function Stop-ZmsTranscript {
            Write-ZmsInfo "Stopped $script:ZmsCurrentScriptName"
        }

        function Add-ZmsExecutionEvent {
            param(
                [Parameter(Mandatory = $true)][string]$StepName,
                [Parameter(Mandatory = $true)][string]$Status,
                [Parameter(Mandatory = $true)][string]$Message,
                [string]$Target = "",
                [string]$ItemType = ""
            )

            $executionPath = Join-Path $script:ZmsPackageRoot "execution"
            if (-not (Test-Path $executionPath)) {
                New-Item -ItemType Directory -Force -Path $executionPath | Out-Null
            }

            $event = [ordered]@{
                timestamp = (Get-Date).ToUniversalTime().ToString("o")
                script = $script:ZmsCurrentScriptName
                step = $StepName
                status = $Status
                itemType = $ItemType
                target = $Target
                message = $Message
            }

            $event | ConvertTo-Json -Compress -Depth 10 | Add-Content -Path (Join-Path $executionPath "execution-events.jsonl")
        }
        """;
    }

    private static string GenerateConfigLibrary()
    {
        return """
        Set-StrictMode -Version Latest

        $script:ZmsPackageRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

        function Get-ZmsPackagePaths {
            param([string]$ConfigPath)

            $root = $script:ZmsPackageRoot
            return [PSCustomObject]@{
                Root = $root
                Config = if ($ConfigPath) { (Resolve-Path -LiteralPath $ConfigPath -ErrorAction SilentlyContinue).Path } else { Join-Path $root "config/zms-spo-environment.json" }
                Logs = Join-Path $root "logs"
                Execution = Join-Path $root "execution"
                Reports = Join-Path $root "reports"
                SampleFiles = Join-Path $root "sample-files"
            }
        }

        function Test-ZmsConfigFile {
            param([Parameter(Mandatory = $true)][string]$ConfigPath)

            if (-not (Test-Path -LiteralPath $ConfigPath)) {
                return [PSCustomObject]@{
                    isValid = $false
                    path = $ConfigPath
                    message = "Config file not found: $ConfigPath"
                }
            }

            try {
                Get-Content -LiteralPath $ConfigPath -Raw | ConvertFrom-Json | Out-Null
                return [PSCustomObject]@{
                    isValid = $true
                    path = (Resolve-Path -LiteralPath $ConfigPath).Path
                    message = "Config file is valid JSON."
                }
            } catch {
                return [PSCustomObject]@{
                    isValid = $false
                    path = $ConfigPath
                    message = "Config file is not valid JSON. $($_.Exception.Message)"
                }
            }
        }

        function Get-ZmsConfig {
            param([Parameter(Mandatory = $true)][string]$ConfigPath)

            $test = Test-ZmsConfigFile -ConfigPath $ConfigPath
            if (-not $test.isValid) {
                throw $test.message
            }

            return Get-Content -LiteralPath $ConfigPath -Raw | ConvertFrom-Json
        }

        function Get-ZmsPropertyValue {
            param(
                [object]$Object,
                [Parameter(Mandatory = $true)][string]$Name
            )

            if ($null -eq $Object) {
                return $null
            }

            $property = $Object.PSObject.Properties[$Name]
            if ($null -eq $property) {
                return $null
            }

            return $property.Value
        }

        function ConvertTo-ZmsArray {
            param([object]$Value)

            if ($null -eq $Value) {
                return @()
            }

            return @($Value)
        }

        function Get-ZmsSiteCollections {
            param([Parameter(Mandatory = $true)][object]$Config)
            return @(ConvertTo-ZmsArray (Get-ZmsPropertyValue -Object $Config -Name "siteCollections"))
        }

        function Get-ZmsEnvironmentSummary {
            param([Parameter(Mandatory = $true)][object]$Config)

            $sites = Get-ZmsSiteCollections -Config $Config
            return [PSCustomObject]@{
                tenantName = Get-ZmsPropertyValue -Object $Config -Name "tenantName"
                adminUrl = Get-ZmsPropertyValue -Object $Config -Name "adminUrl"
                rootUrl = Get-ZmsPropertyValue -Object $Config -Name "rootUrl"
                ownerEmail = Get-ZmsPropertyValue -Object $Config -Name "ownerEmail"
                siteCollections = $sites.Count
                subsites = (@($sites | ForEach-Object { @(ConvertTo-ZmsArray (Get-ZmsPropertyValue -Object $_ -Name "subsites")).Count }) | Measure-Object -Sum).Sum
                libraries = (@($sites | ForEach-Object { @(ConvertTo-ZmsArray (Get-ZmsPropertyValue -Object $_ -Name "libraries")).Count }) | Measure-Object -Sum).Sum
                lists = (@($sites | ForEach-Object { @(ConvertTo-ZmsArray (Get-ZmsPropertyValue -Object $_ -Name "lists")).Count }) | Measure-Object -Sum).Sum
                metadataFields = (@($sites | ForEach-Object { @(ConvertTo-ZmsArray (Get-ZmsPropertyValue -Object $_ -Name "metadataFields")).Count }) | Measure-Object -Sum).Sum
                permissionGroups = (@($sites | ForEach-Object { @(ConvertTo-ZmsArray (Get-ZmsPropertyValue -Object $_ -Name "permissionGroups")).Count }) | Measure-Object -Sum).Sum
                edgeCases = (@($sites | ForEach-Object { @(ConvertTo-ZmsArray (Get-ZmsPropertyValue -Object $_ -Name "edgeCases")).Count }) | Measure-Object -Sum).Sum
            }
        }
        """;
    }

    private static string GenerateSharePointLibrary()
    {
        return """
        Set-StrictMode -Version Latest

        function New-ZmsSharePointResult {
            param(
                [Parameter(Mandatory = $true)][string]$ItemType,
                [Parameter(Mandatory = $true)][string]$Name,
                [string]$Target = "",
                [Parameter(Mandatory = $true)][string]$Status,
                [string]$Action = "",
                [string]$Message = "",
                [string]$Error = ""
            )

            return [PSCustomObject]@{
                itemType = $ItemType
                name = $Name
                target = $Target
                status = $Status
                action = $Action
                message = $Message
                error = $Error
            }
        }

        function Connect-ZmsSharePointAdmin {
            param(
                [Parameter(Mandatory = $true)][string]$AdminUrl,
                [string]$ClientId,
                [switch]$DryRun
            )

            if ($DryRun) {
                Write-ZmsInfo "Dry-run: SharePoint admin connection skipped for $AdminUrl"
                return
            }

            if ([string]::IsNullOrWhiteSpace($ClientId) -or $ClientId -eq "PASTE-PNP-ENTRA-APP-CLIENT-ID-HERE") {
                throw "ClientId is required for SharePoint admin connection."
            }

            Write-ZmsStep "Connecting to SharePoint admin center: $AdminUrl"
            Connect-PnPOnline -Url $AdminUrl -Interactive -ClientId $ClientId
        }

        function Connect-ZmsSharePointSite {
            param(
                [Parameter(Mandatory = $true)][string]$Url,
                [string]$ClientId,
                [switch]$DryRun
            )

            if ($DryRun) {
                Write-ZmsInfo "Dry-run: SharePoint site connection skipped for $Url"
                return
            }

            if ([string]::IsNullOrWhiteSpace($ClientId) -or $ClientId -eq "PASTE-PNP-ENTRA-APP-CLIENT-ID-HERE") {
                throw "ClientId is required for SharePoint site connection."
            }

            Write-ZmsStep "Connecting to SharePoint site: $Url"
            Connect-PnPOnline -Url $Url -Interactive -ClientId $ClientId
        }

        function Test-ZmsSiteExists {
            param(
                [Parameter(Mandatory = $true)][string]$Url,
                [switch]$DryRun
            )

            if ($DryRun) {
                return $false
            }

            try {
                $existing = Get-PnPTenantSite -Url $Url -ErrorAction SilentlyContinue
                return $null -ne $existing
            } catch {
                return $false
            }
        }

        function Test-ZmsWebExists {
            param(
                [Parameter(Mandatory = $true)][string]$Identity,
                [switch]$DryRun
            )

            if ($DryRun) {
                return $false
            }

            try {
                $existing = Get-PnPWeb -Identity $Identity -ErrorAction SilentlyContinue
                return $null -ne $existing
            } catch {
                return $false
            }
        }

        function Test-ZmsListExists {
            param(
                [Parameter(Mandatory = $true)][string]$Identity,
                [switch]$DryRun
            )

            if ($DryRun) {
                return $false
            }

            try {
                $existing = Get-PnPList -Identity $Identity -ErrorAction SilentlyContinue
                return $null -ne $existing
            } catch {
                return $false
            }
        }

        function Test-ZmsFieldExists {
            param(
                [Parameter(Mandatory = $true)][string]$Identity,
                [string]$List,
                [switch]$DryRun
            )

            if ($DryRun) {
                return $false
            }

            try {
                if ($List) {
                    $existing = Get-PnPField -List $List -Identity $Identity -ErrorAction SilentlyContinue
                } else {
                    $existing = Get-PnPField -Identity $Identity -ErrorAction SilentlyContinue
                }
                return $null -ne $existing
            } catch {
                return $false
            }
        }

        function Test-ZmsGroupExists {
            param(
                [Parameter(Mandatory = $true)][string]$Identity,
                [switch]$DryRun
            )

            if ($DryRun) {
                return $false
            }

            try {
                $existing = Get-PnPGroup -Identity $Identity -ErrorAction SilentlyContinue
                return $null -ne $existing
            } catch {
                return $false
            }
        }

        function Convert-ZmsFieldType {
            param([string]$FieldType)

            switch ($FieldType) {
                "Text" { return "Text" }
                "Choice" { return "Choice" }
                "Date" { return "DateTime" }
                "DateTime" { return "DateTime" }
                "Person" { return "User" }
                "User" { return "User" }
                "Currency" { return "Currency" }
                "Boolean" { return "Boolean" }
                "Number" { return "Number" }
                default { return "Text" }
            }
        }

        function Get-ZmsInternalName {
            param([Parameter(Mandatory = $true)][string]$Value)
            return ($Value -replace "[^A-Za-z0-9_]", "_")
        }

        function New-ZmsSiteCollectionSafe {
            param(
                [Parameter(Mandatory = $true)][object]$Site,
                [Parameter(Mandatory = $true)][string]$OwnerEmail,
                [switch]$DryRun
            )

            try {
                if (Test-ZmsSiteExists -Url $Site.url -DryRun:$DryRun) {
                    return New-ZmsSharePointResult -ItemType "SiteCollection" -Name $Site.title -Target $Site.url -Status "skipped" -Action "none" -Message "Site collection already exists."
                }

                if ($DryRun) {
                    return New-ZmsSharePointResult -ItemType "SiteCollection" -Name $Site.title -Target $Site.url -Status "planned" -Action "create" -Message "Would create TeamSiteWithoutMicrosoft365Group site collection."
                }

                New-PnPSite -Type TeamSiteWithoutMicrosoft365Group -Title $Site.title -Url $Site.url -Owner $OwnerEmail -Wait
                return New-ZmsSharePointResult -ItemType "SiteCollection" -Name $Site.title -Target $Site.url -Status "created" -Action "create" -Message "Created site collection."
            } catch {
                return New-ZmsSharePointResult -ItemType "SiteCollection" -Name $Site.title -Target $Site.url -Status "failed" -Action "create" -Message "Failed to create site collection." -Error $_.Exception.Message
            }
        }

        function New-ZmsSubsiteSafe {
            param(
                [Parameter(Mandatory = $true)][object]$Subsite,
                [switch]$DryRun
            )

            $leafUrl = ($Subsite.url -split "/")[-1]
            try {
                if (Test-ZmsWebExists -Identity $leafUrl -DryRun:$DryRun) {
                    return New-ZmsSharePointResult -ItemType "Subsite" -Name $Subsite.title -Target $Subsite.url -Status "skipped" -Action "none" -Message "Subsite already exists."
                }

                if ($DryRun) {
                    return New-ZmsSharePointResult -ItemType "Subsite" -Name $Subsite.title -Target $Subsite.url -Status "planned" -Action "create" -Message "Would create subsite with STS#3 template."
                }

                New-PnPWeb -Title $Subsite.title -Url $leafUrl -Template "STS#3" -Locale 1033
                return New-ZmsSharePointResult -ItemType "Subsite" -Name $Subsite.title -Target $Subsite.url -Status "created" -Action "create" -Message "Created subsite."
            } catch {
                $message = "Subsite creation may be disabled in this tenant. Enable custom script/subsite capability or convert subsites into separate modern sites."
                return New-ZmsSharePointResult -ItemType "Subsite" -Name $Subsite.title -Target $Subsite.url -Status "failed" -Action "create" -Message $message -Error $_.Exception.Message
            }
        }

        function New-ZmsLibrarySafe {
            param(
                [Parameter(Mandatory = $true)][object]$Library,
                [switch]$DryRun
            )

            try {
                if (Test-ZmsListExists -Identity $Library.title -DryRun:$DryRun) {
                    return New-ZmsSharePointResult -ItemType "Library" -Name $Library.title -Target $Library.title -Status "skipped" -Action "none" -Message "Library already exists."
                }

                if ($DryRun) {
                    return New-ZmsSharePointResult -ItemType "Library" -Name $Library.title -Target $Library.title -Status "planned" -Action "create" -Message "Would create document library."
                }

                New-PnPList -Title $Library.title -Template DocumentLibrary -EnableVersioning:([bool]$Library.includeVersioning)
                return New-ZmsSharePointResult -ItemType "Library" -Name $Library.title -Target $Library.title -Status "created" -Action "create" -Message "Created document library."
            } catch {
                return New-ZmsSharePointResult -ItemType "Library" -Name $Library.title -Target $Library.title -Status "failed" -Action "create" -Message "Failed to create library." -Error $_.Exception.Message
            }
        }

        function New-ZmsListSafe {
            param(
                [Parameter(Mandatory = $true)][object]$List,
                [switch]$DryRun
            )

            try {
                if (Test-ZmsListExists -Identity $List.title -DryRun:$DryRun) {
                    return New-ZmsSharePointResult -ItemType "List" -Name $List.title -Target $List.title -Status "skipped" -Action "none" -Message "List already exists."
                }

                if ($DryRun) {
                    return New-ZmsSharePointResult -ItemType "List" -Name $List.title -Target $List.title -Status "planned" -Action "create" -Message "Would create generic SharePoint list."
                }

                New-PnPList -Title $List.title -Template GenericList
                return New-ZmsSharePointResult -ItemType "List" -Name $List.title -Target $List.title -Status "created" -Action "create" -Message "Created list."
            } catch {
                return New-ZmsSharePointResult -ItemType "List" -Name $List.title -Target $List.title -Status "failed" -Action "create" -Message "Failed to create list." -Error $_.Exception.Message
            }
        }

        function New-ZmsFieldSafe {
            param(
                [Parameter(Mandatory = $true)][object]$Field,
                [string]$TargetList,
                [switch]$DryRun
            )

            $internalName = Get-ZmsInternalName -Value $Field.id
            $target = if ($TargetList) { $TargetList } else { "Site" }
            try {
                if (Test-ZmsFieldExists -Identity $internalName -List $TargetList -DryRun:$DryRun) {
                    return New-ZmsSharePointResult -ItemType "Field" -Name $Field.name -Target $target -Status "skipped" -Action "none" -Message "Field already exists on target."
                }

                $type = Convert-ZmsFieldType -FieldType $Field.type
                if ($DryRun) {
                    return New-ZmsSharePointResult -ItemType "Field" -Name $Field.name -Target $target -Status "planned" -Action "create" -Message "Would add field type '$type' to $target."
                }

                if (-not $TargetList -and -not (Test-ZmsFieldExists -Identity $internalName)) {
                    if ($type -eq "Choice") {
                        Add-PnPField -DisplayName $Field.name -InternalName $internalName -Type Choice -Choices @($Field.choices) -AddToDefaultView -Required:([bool]$Field.required)
                    } else {
                        Add-PnPField -DisplayName $Field.name -InternalName $internalName -Type $type -AddToDefaultView -Required:([bool]$Field.required)
                    }
                }

                if ($TargetList) {
                    if (-not (Test-ZmsFieldExists -Identity $internalName)) {
                        if ($type -eq "Choice") {
                            Add-PnPField -DisplayName $Field.name -InternalName $internalName -Type Choice -Choices @($Field.choices) -AddToDefaultView -Required:([bool]$Field.required)
                        } else {
                            Add-PnPField -DisplayName $Field.name -InternalName $internalName -Type $type -AddToDefaultView -Required:([bool]$Field.required)
                        }
                    }
                    Add-PnPFieldToList -List $TargetList -Field $internalName
                }

                return New-ZmsSharePointResult -ItemType "Field" -Name $Field.name -Target $target -Status "created" -Action "create" -Message "Added field type '$type' to $target."
            } catch {
                return New-ZmsSharePointResult -ItemType "Field" -Name $Field.name -Target $target -Status "failed" -Action "create" -Message "Failed to add field to $target." -Error $_.Exception.Message
            }
        }

        function New-ZmsGroupSafe {
            param(
                [Parameter(Mandatory = $true)][object]$Group,
                [switch]$DryRun
            )

            try {
                if (Test-ZmsGroupExists -Identity $Group.name -DryRun:$DryRun) {
                    return New-ZmsSharePointResult -ItemType "Group" -Name $Group.name -Target $Group.role -Status "skipped" -Action "none" -Message "Group already exists."
                }

                if ($DryRun) {
                    return New-ZmsSharePointResult -ItemType "Group" -Name $Group.name -Target $Group.role -Status "planned" -Action "create" -Message "Would create SharePoint group with role '$($Group.role)'."
                }

                New-PnPGroup -Title $Group.name
                return New-ZmsSharePointResult -ItemType "Group" -Name $Group.name -Target $Group.role -Status "created" -Action "create" -Message "Created SharePoint group."
            } catch {
                return New-ZmsSharePointResult -ItemType "Group" -Name $Group.name -Target $Group.role -Status "failed" -Action "create" -Message "Failed to create group." -Error $_.Exception.Message
            }
        }
        """;
    }

    private static string GenerateValidationLibrary()
    {
        return """
        Set-StrictMode -Version Latest

        function New-ZmsValidationResult {
            param(
                [Parameter(Mandatory = $true)][string]$Name,
                [Parameter(Mandatory = $true)][string]$Status,
                [Parameter(Mandatory = $true)][string]$Message,
                [bool]$IsCritical = $false
            )

            return [PSCustomObject]@{
                name = $Name
                status = $Status
                critical = $IsCritical
                message = $Message
            }
        }

        function Test-ZmsPowerShellVersion {
            if ($PSVersionTable.PSVersion.Major -ge 7) {
                return New-ZmsValidationResult -Name "PowerShell version" -Status "pass" -Message "PowerShell $($PSVersionTable.PSVersion) is supported."
            }

            return New-ZmsValidationResult -Name "PowerShell version" -Status "warning" -Message "PowerShell 7 or newer is recommended. Current version: $($PSVersionTable.PSVersion)."
        }

        function Test-ZmsPnPModule {
            $module = Get-Module -ListAvailable -Name PnP.PowerShell | Sort-Object Version -Descending | Select-Object -First 1
            if ($module) {
                return New-ZmsValidationResult -Name "PnP.PowerShell" -Status "pass" -Message "PnP.PowerShell found: $($module.Version)."
            }

            return New-ZmsValidationResult -Name "PnP.PowerShell" -Status "warning" -Message "PnP.PowerShell is not installed. Install it before real execution with: Install-Module PnP.PowerShell -Scope CurrentUser"
        }

        function Test-ZmsConfigSchema {
            param([Parameter(Mandatory = $true)][object]$Config)

            $required = @("tenantName", "adminUrl", "rootUrl", "ownerEmail", "siteCollections")
            $results = @()
            foreach ($name in $required) {
                $value = Get-ZmsPropertyValue -Object $Config -Name $name
                if ($name -eq "siteCollections") {
                    if ($null -eq $value -or @(ConvertTo-ZmsArray $value).Count -eq 0) {
                        $results += New-ZmsValidationResult -Name "Config schema" -Status "fail" -Message "Missing required config property: $name" -IsCritical $true
                    }
                } else {
                    if ($null -eq $value -or ([string]$value).Trim().Length -eq 0) {
                        $results += New-ZmsValidationResult -Name "Config schema" -Status "fail" -Message "Missing required config property: $name" -IsCritical $true
                    }
                }
            }

            $sites = Get-ZmsSiteCollections -Config $Config
            if ($sites.Count -eq 0) {
                $results += New-ZmsValidationResult -Name "Config schema" -Status "fail" -Message "Config must include at least one site collection." -IsCritical $true
            } else {
                $results += New-ZmsValidationResult -Name "Config schema" -Status "pass" -Message "Required config properties are present."
            }

            return $results
        }

        function Test-ZmsTenantUrls {
            param([Parameter(Mandatory = $true)][object]$Config)

            $results = @()
            foreach ($property in @("adminUrl", "rootUrl")) {
                $value = [string](Get-ZmsPropertyValue -Object $Config -Name $property)
                $uri = $null
                if ([Uri]::TryCreate($value, [UriKind]::Absolute, [ref]$uri) -and $uri.Scheme -eq "https") {
                    $results += New-ZmsValidationResult -Name "Tenant URL: $property" -Status "pass" -Message "$property is a valid HTTPS URL."
                } else {
                    $results += New-ZmsValidationResult -Name "Tenant URL: $property" -Status "fail" -Message "$property must be a valid HTTPS URL." -IsCritical $true
                }
            }

            $adminUrl = [string](Get-ZmsPropertyValue -Object $Config -Name "adminUrl")
            if ($adminUrl -and $adminUrl -notmatch "-admin\.sharepoint\.com/?$") {
                $results += New-ZmsValidationResult -Name "Admin URL convention" -Status "warning" -Message "Admin URL should normally end with -admin.sharepoint.com."
            }

            return $results
        }

        function Test-ZmsPermissionReadiness {
            param([Parameter(Mandatory = $true)][object]$Config)

            $results = @()
            $owner = [string](Get-ZmsPropertyValue -Object $Config -Name "ownerEmail")
            if ($owner -match "^[^@\s]+@[^@\s]+\.[^@\s]+$") {
                $results += New-ZmsValidationResult -Name "Owner email" -Status "pass" -Message "Owner email format looks valid."
            } else {
                $results += New-ZmsValidationResult -Name "Owner email" -Status "fail" -Message "Owner email is required for site collection creation." -IsCritical $true
            }

            $clientId = [string](Get-ZmsPropertyValue -Object $Config -Name "clientIdPlaceholder")
            if ([string]::IsNullOrWhiteSpace($clientId) -or $clientId -eq "PASTE-PNP-ENTRA-APP-CLIENT-ID-HERE") {
                $results += New-ZmsValidationResult -Name "Client ID" -Status "warning" -Message "ClientId placeholder is still present. Real execution requires an approved PnP/Entra app client ID."
            } else {
                $results += New-ZmsValidationResult -Name "Client ID" -Status "pass" -Message "ClientId value is present."
            }

            return $results
        }

        function Test-ZmsSubsiteCapabilityWarning {
            param([Parameter(Mandatory = $true)][object]$Config)

            $summary = Get-ZmsEnvironmentSummary -Config $Config
            if ($summary.subsites -gt 0) {
                return New-ZmsValidationResult -Name "Subsite capability" -Status "warning" -Message "$($summary.subsites) subsites are configured. Some tenants disable subsite creation."
            }

            return New-ZmsValidationResult -Name "Subsite capability" -Status "pass" -Message "No subsites are configured."
        }

        function Test-ZmsLongPathRisks {
            param([Parameter(Mandatory = $true)][object]$Config)

            $longPaths = @()
            foreach ($site in Get-ZmsSiteCollections -Config $Config) {
                foreach ($folder in @(ConvertTo-ZmsArray (Get-ZmsPropertyValue -Object $site -Name "folderStructures"))) {
                    if ($folder.path -and $folder.path.Length -gt 180) {
                        $longPaths += "$($site.title): $($folder.path)"
                    }
                }

                foreach ($library in @(ConvertTo-ZmsArray (Get-ZmsPropertyValue -Object $site -Name "libraries"))) {
                    foreach ($folder in @(ConvertTo-ZmsArray (Get-ZmsPropertyValue -Object $library -Name "folders"))) {
                        if ($folder.path -and $folder.path.Length -gt 180) {
                            $longPaths += "$($site.title): $($folder.path)"
                        }
                    }
                }
            }

            if ($longPaths.Count -gt 0) {
                return New-ZmsValidationResult -Name "Long path risks" -Status "warning" -Message "$($longPaths.Count) folder paths exceed the 180 character review threshold."
            }

            return New-ZmsValidationResult -Name "Long path risks" -Status "pass" -Message "No generated folder paths exceed the 180 character review threshold."
        }

        function Test-ZmsPrerequisites {
            param(
                [Parameter(Mandatory = $true)][string]$ConfigPath,
                [switch]$CheckTenantConnection,
                [string]$ClientId
            )

            $results = @()
            $results += Test-ZmsPowerShellVersion
            $results += Test-ZmsPnPModule

            $file = Test-ZmsConfigFile -ConfigPath $ConfigPath
            if (-not $file.isValid) {
                $results += New-ZmsValidationResult -Name "Config file" -Status "fail" -Message $file.message -IsCritical $true
                return $results
            }

            $results += New-ZmsValidationResult -Name "Config file" -Status "pass" -Message $file.message
            $config = Get-ZmsConfig -ConfigPath $ConfigPath
            $results += Test-ZmsConfigSchema -Config $config
            $results += Test-ZmsTenantUrls -Config $config
            $results += Test-ZmsPermissionReadiness -Config $config
            $results += Test-ZmsSubsiteCapabilityWarning -Config $config
            $results += Test-ZmsLongPathRisks -Config $config

            if ($CheckTenantConnection) {
                try {
                    Connect-ZmsSharePointAdmin -AdminUrl $config.adminUrl -ClientId $ClientId
                    $results += New-ZmsValidationResult -Name "Tenant connection" -Status "pass" -Message "Tenant admin connection succeeded."
                } catch {
                    $results += New-ZmsValidationResult -Name "Tenant connection" -Status "fail" -Message "Tenant admin connection failed. $($_.Exception.Message)" -IsCritical $true
                }
            } else {
                $results += New-ZmsValidationResult -Name "Tenant connection" -Status "skipped" -Message "Tenant connection was not checked. Pass -CheckTenantConnection to test it."
            }

            return $results
        }
        """;
    }

    private static string GenerateReportingLibrary()
    {
        return """
        Set-StrictMode -Version Latest

        $script:ZmsPackageRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

        function Export-ZmsJson {
            param(
                [Parameter(Mandatory = $true)][object]$Value,
                [Parameter(Mandatory = $true)][string]$Path
            )

            $directory = Split-Path -Parent $Path
            if (-not (Test-Path $directory)) {
                New-Item -ItemType Directory -Force -Path $directory | Out-Null
            }

            $Value | ConvertTo-Json -Depth 50 | Set-Content -LiteralPath $Path -Encoding UTF8
        }

        function Export-ZmsMarkdownTable {
            param(
                [Parameter(Mandatory = $true)][object[]]$Rows,
                [Parameter(Mandatory = $true)][string[]]$Columns
            )

            $lines = @()
            $lines += "| " + ($Columns -join " | ") + " |"
            $lines += "| " + (($Columns | ForEach-Object { "---" }) -join " | ") + " |"
            foreach ($row in $Rows) {
                $values = foreach ($column in $Columns) {
                    $property = $row.PSObject.Properties[$column]
                    if ($property) { [string]$property.Value } else { "" }
                }
                $lines += "| " + ($values -join " | ") + " |"
            }

            return ($lines -join [Environment]::NewLine)
        }

        function Set-ZmsObjectProperty {
            param(
                [Parameter(Mandatory = $true)][object]$Object,
                [Parameter(Mandatory = $true)][string]$Name,
                [object]$Value
            )

            if ($Object.PSObject.Properties[$Name]) {
                $Object.$Name = $Value
            } else {
                $Object | Add-Member -NotePropertyName $Name -NotePropertyValue $Value
            }
        }

        function Update-ZmsExecutionStatus {
            param(
                [Parameter(Mandatory = $true)][string]$StepName,
                [Parameter(Mandatory = $true)][string]$Status,
                [int]$Created = 0,
                [int]$Skipped = 0,
                [int]$Failed = 0,
                [string]$Message = ""
            )

            $executionPath = Join-Path $script:ZmsPackageRoot "execution"
            if (-not (Test-Path $executionPath)) {
                New-Item -ItemType Directory -Force -Path $executionPath | Out-Null
            }

            $statusPath = Join-Path $executionPath "execution-status.json"
            if (Test-Path $statusPath) {
                $doc = Get-Content -LiteralPath $statusPath -Raw | ConvertFrom-Json
            } else {
                $doc = [PSCustomObject]@{
                    status = "not_started"
                    lastRunAt = $null
                    steps = @()
                }
            }

            $steps = @($doc.steps)
            $step = $steps | Where-Object { $_.name -eq $StepName } | Select-Object -First 1
            if (-not $step) {
                $step = [PSCustomObject]@{
                    name = $StepName
                    status = "pending"
                    created = 0
                    skipped = 0
                    failed = 0
                    message = ""
                    lastRunAt = $null
                }
                $steps += $step
            }

            Set-ZmsObjectProperty -Object $step -Name "status" -Value $Status
            Set-ZmsObjectProperty -Object $step -Name "created" -Value $Created
            Set-ZmsObjectProperty -Object $step -Name "skipped" -Value $Skipped
            Set-ZmsObjectProperty -Object $step -Name "failed" -Value $Failed
            Set-ZmsObjectProperty -Object $step -Name "message" -Value $Message
            Set-ZmsObjectProperty -Object $step -Name "lastRunAt" -Value ((Get-Date).ToUniversalTime().ToString("o"))

            Set-ZmsObjectProperty -Object $doc -Name "steps" -Value $steps
            Set-ZmsObjectProperty -Object $doc -Name "lastRunAt" -Value ((Get-Date).ToUniversalTime().ToString("o"))

            $failedTotal = (@($steps | ForEach-Object { [int]$_.failed }) | Measure-Object -Sum).Sum
            $running = @($steps | Where-Object { $_.status -eq "running" }).Count
            if ($failedTotal -gt 0) {
                Set-ZmsObjectProperty -Object $doc -Name "status" -Value "completed_with_errors"
            } elseif ($running -gt 0) {
                Set-ZmsObjectProperty -Object $doc -Name "status" -Value "in_progress"
            } else {
                Set-ZmsObjectProperty -Object $doc -Name "status" -Value "completed"
            }

            Export-ZmsJson -Value $doc -Path $statusPath
        }

        function New-ZmsPreflightReport {
            param(
                [Parameter(Mandatory = $true)][object]$Config,
                [Parameter(Mandatory = $true)][object[]]$Checks,
                [string]$Path = (Join-Path $script:ZmsPackageRoot "execution/preflight-report.md")
            )

            $summary = Get-ZmsEnvironmentSummary -Config $Config
            $lines = @()
            $lines += "# zettalogixmigrationsuite Preflight Report"
            $lines += ""
            $lines += "- Generated: $((Get-Date).ToUniversalTime().ToString("o"))"
            $lines += "- Tenant: $($summary.tenantName)"
            $lines += "- Admin URL: $($summary.adminUrl)"
            $lines += "- Root URL: $($summary.rootUrl)"
            $lines += "- Site Collections: $($summary.siteCollections)"
            $lines += "- Subsites: $($summary.subsites)"
            $lines += "- Libraries: $($summary.libraries)"
            $lines += "- Lists: $($summary.lists)"
            $lines += "- Metadata Fields: $($summary.metadataFields)"
            $lines += "- Permission Groups: $($summary.permissionGroups)"
            $lines += "- Edge Cases: $($summary.edgeCases)"
            $lines += ""
            $lines += "## Checks"
            $lines += ""
            $rows = foreach ($check in $Checks) {
                [PSCustomObject]@{
                    Check = $check.name
                    Status = $check.status
                    Critical = $check.critical
                    Message = $check.message
                }
            }
            $lines += Export-ZmsMarkdownTable -Rows $rows -Columns @("Check", "Status", "Critical", "Message")

            $directory = Split-Path -Parent $Path
            if (-not (Test-Path $directory)) {
                New-Item -ItemType Directory -Force -Path $directory | Out-Null
            }
            $lines -join [Environment]::NewLine | Set-Content -LiteralPath $Path -Encoding UTF8
            return $Path
        }

        function New-ZmsDryRunReport {
            param(
                [Parameter(Mandatory = $true)][object]$Config,
                [string]$Path = (Join-Path $script:ZmsPackageRoot "execution/dry-run-report.md")
            )

            $summary = Get-ZmsEnvironmentSummary -Config $Config
            $statusPath = Join-Path $script:ZmsPackageRoot "execution/execution-status.json"
            $eventsPath = Join-Path $script:ZmsPackageRoot "execution/execution-events.jsonl"

            $lines = @()
            $lines += "# zettalogixmigrationsuite Dry-Run Report"
            $lines += ""
            $lines += "- Generated: $((Get-Date).ToUniversalTime().ToString("o"))"
            $lines += "- Tenant: $($summary.tenantName)"
            $lines += "- No SharePoint objects were created."
            $lines += ""
            $lines += "## Expected Objects"
            $lines += ""
            $lines += "- Site Collections: $($summary.siteCollections)"
            $lines += "- Subsites: $($summary.subsites)"
            $lines += "- Libraries: $($summary.libraries)"
            $lines += "- Lists: $($summary.lists)"
            $lines += "- Metadata Fields: $($summary.metadataFields)"
            $lines += "- Permission Groups: $($summary.permissionGroups)"
            $lines += "- Edge Cases: $($summary.edgeCases)"
            $lines += ""
            $lines += "## Step Status"
            $lines += ""

            if (Test-Path $statusPath) {
                $status = Get-Content -LiteralPath $statusPath -Raw | ConvertFrom-Json
                $rows = foreach ($step in @($status.steps)) {
                    [PSCustomObject]@{
                        Step = $step.name
                        Status = $step.status
                        Created = $step.created
                        Skipped = $step.skipped
                        Failed = $step.failed
                    }
                }
                $lines += Export-ZmsMarkdownTable -Rows $rows -Columns @("Step", "Status", "Created", "Skipped", "Failed")
            } else {
                $lines += "No execution status file was found."
            }

            $lines += ""
            $lines += "## Planned Actions"
            $lines += ""
            if (Test-Path $eventsPath) {
                $eventLines = Get-Content -LiteralPath $eventsPath
                foreach ($line in $eventLines) {
                    try {
                        $event = $line | ConvertFrom-Json
                        if ($event.status -eq "planned") {
                            $lines += "- $($event.step): $($event.itemType) $($event.target) - $($event.message)"
                        }
                    } catch {
                        $lines += "- $line"
                    }
                }
            } else {
                $lines += "No planned-action event log was found."
            }

            $directory = Split-Path -Parent $Path
            if (-not (Test-Path $directory)) {
                New-Item -ItemType Directory -Force -Path $directory | Out-Null
            }
            $lines -join [Environment]::NewLine | Set-Content -LiteralPath $Path -Encoding UTF8
            return $Path
        }

        function New-ZmsExecutionSummary {
            param(
                [Parameter(Mandatory = $true)][object]$Config,
                [string]$Path = (Join-Path $script:ZmsPackageRoot "reports/execution-summary.md")
            )

            $summary = Get-ZmsEnvironmentSummary -Config $Config
            $statusPath = Join-Path $script:ZmsPackageRoot "execution/execution-status.json"
            $lines = @()
            $lines += "# zettalogixmigrationsuite Execution Summary"
            $lines += ""
            $lines += "- Generated: $((Get-Date).ToUniversalTime().ToString("o"))"
            $lines += "- Tenant: $($summary.tenantName)"
            $lines += ""
            if (Test-Path $statusPath) {
                $status = Get-Content -LiteralPath $statusPath -Raw | ConvertFrom-Json
                $lines += "- Overall Status: $($status.status)"
                $lines += "- Last Run At: $($status.lastRunAt)"
                $lines += ""
                $rows = foreach ($step in @($status.steps)) {
                    [PSCustomObject]@{
                        Step = $step.name
                        Status = $step.status
                        Created = $step.created
                        Skipped = $step.skipped
                        Failed = $step.failed
                    }
                }
                $lines += Export-ZmsMarkdownTable -Rows $rows -Columns @("Step", "Status", "Created", "Skipped", "Failed")
            }

            $directory = Split-Path -Parent $Path
            if (-not (Test-Path $directory)) {
                New-Item -ItemType Directory -Force -Path $directory | Out-Null
            }
            $lines -join [Environment]::NewLine | Set-Content -LiteralPath $Path -Encoding UTF8
            return $Path
        }
        """;
    }

    private static string GenerateCommonScriptHeader(string synopsis)
    {
        return $$"""
        <#
        .SYNOPSIS
        {{synopsis}}

        Generated by ZMS. Review before running in any tenant.
        This script does not contain secrets and does not call Microsoft Graph directly.
        #>
        """;
    }

    private static string GenerateImportBlock()
    {
        return """
        Set-StrictMode -Version Latest
        $ErrorActionPreference = "Stop"

        $LibPath = Join-Path $PSScriptRoot "lib"
        . (Join-Path $LibPath "Zms.Logging.ps1")
        . (Join-Path $LibPath "Zms.Config.ps1")
        . (Join-Path $LibPath "Zms.SharePoint.ps1")
        . (Join-Path $LibPath "Zms.Validation.ps1")
        . (Join-Path $LibPath "Zms.Reporting.ps1")

        function Get-ZmsPowerShellExecutable {
            $pwshCommand = Get-Command pwsh -ErrorAction SilentlyContinue
            if ($pwshCommand) {
                return $pwshCommand.Source
            }

            Write-ZmsWarning "pwsh was not found on PATH. Falling back to the current PowerShell host for child scripts."
            return (Get-Process -Id $PID).Path
        }

        function Invoke-ZmsPowerShellScript {
            param(
                [Parameter(Mandatory = $true)][string]$ScriptPath,
                [Parameter(Mandatory = $true)][string]$ConfigPath,
                [string]$ClientId,
                [switch]$DryRun,
                [switch]$VerboseLogging,
                [switch]$CreateLargeFiles,
                [switch]$UseLiveTenant
            )

            $arguments = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $ScriptPath, "-ConfigPath", $ConfigPath)
            if ($ClientId) {
                $arguments += @("-ClientId", $ClientId)
            }
            if ($DryRun) {
                $arguments += "-DryRun"
            }
            if ($VerboseLogging) {
                $arguments += "-VerboseLogging"
            }
            if ($CreateLargeFiles) {
                $arguments += "-CreateLargeFiles"
            }
            if ($UseLiveTenant) {
                $arguments += "-UseLiveTenant"
            }

            & (Get-ZmsPowerShellExecutable) @arguments | ForEach-Object { Write-Host $_ }
            $scriptExitCode = $LASTEXITCODE
            return $scriptExitCode
        }
        """;
    }

    private static string GenerateDiscoveryReadOnlyImportBlock()
    {
        return """
        Set-StrictMode -Version Latest
        $ErrorActionPreference = "Stop"

        $LibPath = Join-Path $PSScriptRoot "lib"
        . (Join-Path $LibPath "Zms.Logging.ps1")
        . (Join-Path $LibPath "Zms.Config.ps1")

        function Connect-ZmsDiscoverySharePointSite {
            param(
                [Parameter(Mandatory = $true)][string]$Url,
                [Parameter(Mandatory = $true)][string]$ClientId
            )

            if ([string]::IsNullOrWhiteSpace($ClientId)) {
                throw "ClientId is required for read-only SharePoint discovery."
            }

            Write-ZmsStep "Connecting read-only to $Url"
            Connect-PnPOnline -Url $Url -Interactive -ClientId $ClientId
        }
        """;
    }

    private static string GenerateResultHandlingBlock(string stepName)
    {
        return $$"""
        function Complete-ZmsStepFromResults {
            param([object[]]$Results)

            $created = @($Results | Where-Object { $_.status -eq "created" }).Count
            $planned = @($Results | Where-Object { $_.status -eq "planned" }).Count
            $skipped = @($Results | Where-Object { $_.status -eq "skipped" }).Count
            $failed = @($Results | Where-Object { $_.status -eq "failed" }).Count
            $status = if ($failed -gt 0) { "completed_with_errors" } elseif ($planned -gt 0) { "dry_run_completed" } else { "completed" }
            Update-ZmsExecutionStatus -StepName "{{stepName}}" -Status $status -Created $created -Skipped $skipped -Failed $failed -Message "$created created, $planned planned, $skipped skipped, $failed failed."
            return $failed
        }

        function Write-ZmsSafeResult {
            param([Parameter(Mandatory = $true)][object]$Result)

            Add-ZmsExecutionEvent -StepName "{{stepName}}" -Status $Result.status -Message $Result.message -Target $Result.target -ItemType $Result.itemType
            switch ($Result.status) {
                "created" { Write-ZmsSuccess "$($Result.itemType): $($Result.name) - $($Result.message)" }
                "planned" { Write-ZmsInfo "DRY-RUN $($Result.itemType): $($Result.name) - $($Result.message)" }
                "skipped" { Write-ZmsWarning "SKIPPED $($Result.itemType): $($Result.name) - $($Result.message)" }
                "failed" { Write-ZmsError "FAILED $($Result.itemType): $($Result.name) - $($Result.error)" }
                default { Write-ZmsInfo "$($Result.itemType): $($Result.name) - $($Result.message)" }
            }
        }
        """;
    }

    private static string GeneratePrerequisitesScript()
    {
        return GenerateCommonScriptHeader("Check local prerequisites for ZMS SharePoint Online environment automation.") + """
        param(
            [string]$ConfigPath = (Join-Path $PSScriptRoot "..\config\zms-spo-environment.json"),
            [switch]$CheckTenantConnection,
            [string]$ClientId,
            [switch]$VerboseLogging
        )

        """ + GenerateImportBlock() + """

        $exitCode = 0
        Start-ZmsTranscript -ScriptName (Split-Path -Leaf $PSCommandPath) -VerboseLogging:$VerboseLogging

        try {
            Write-ZmsStep "Running prerequisite and preflight checks"
            Update-ZmsExecutionStatus -StepName "Check prerequisites" -Status "running" -Message "Prerequisite checks started."

            $config = Get-ZmsConfig -ConfigPath $ConfigPath
            $effectiveClientId = if ($ClientId) { $ClientId } else { $config.clientIdPlaceholder }
            $checks = @(Test-ZmsPrerequisites -ConfigPath $ConfigPath -CheckTenantConnection:$CheckTenantConnection -ClientId $effectiveClientId)
            foreach ($check in $checks) {
                switch ($check.status) {
                    "pass" { Write-ZmsSuccess "$($check.name): $($check.message)" }
                    "warning" { Write-ZmsWarning "$($check.name): $($check.message)" }
                    "skipped" { Write-ZmsInfo "$($check.name): $($check.message)" }
                    "fail" { Write-ZmsError "$($check.name): $($check.message)" }
                    default { Write-ZmsInfo "$($check.name): $($check.message)" }
                }
            }

            $summary = Get-ZmsEnvironmentSummary -Config $config
            Write-ZmsInfo "Environment summary: $($summary.siteCollections) site collections, $($summary.subsites) subsites, $($summary.libraries) libraries, $($summary.lists) lists."
            $reportPath = New-ZmsPreflightReport -Config $config -Checks $checks
            Write-ZmsSuccess "Preflight report written to $reportPath"

            $criticalFailures = @($checks | Where-Object { $_.status -eq "fail" -and $_.critical }).Count
            if ($criticalFailures -gt 0) {
                Update-ZmsExecutionStatus -StepName "Check prerequisites" -Status "failed" -Failed $criticalFailures -Message "$criticalFailures critical preflight check(s) failed."
                $exitCode = 1
            } else {
                Update-ZmsExecutionStatus -StepName "Check prerequisites" -Status "completed" -Message "Preflight completed with no critical failures."
            }
        } catch {
            Write-ZmsError $_.Exception.Message
            Update-ZmsExecutionStatus -StepName "Check prerequisites" -Status "failed" -Failed 1 -Message $_.Exception.Message
            $exitCode = 1
        } finally {
            Stop-ZmsTranscript
        }

        exit $exitCode
        """;
    }

    private static string GenerateSiteCollectionsScript()
    {
        return GenerateCommonScriptHeader("Create SharePoint Online site collections from ZMS config.") + """
        param(
            [string]$ConfigPath = (Join-Path $PSScriptRoot "..\config\zms-spo-environment.json"),
            [string]$ClientId,
            [switch]$DryRun,
            [switch]$VerboseLogging
        )

        """ + GenerateImportBlock() + GenerateResultHandlingBlock("Create Site Collections") + """

        $exitCode = 0
        Start-ZmsTranscript -ScriptName (Split-Path -Leaf $PSCommandPath) -VerboseLogging:$VerboseLogging

        try {
            $config = Get-ZmsConfig -ConfigPath $ConfigPath
            $effectiveClientId = if ($ClientId) { $ClientId } else { $config.clientIdPlaceholder }
            Update-ZmsExecutionStatus -StepName "Create Site Collections" -Status "running" -Message "Site collection step started."
            Connect-ZmsSharePointAdmin -AdminUrl $config.adminUrl -ClientId $effectiveClientId -DryRun:$DryRun

            $results = @()
            foreach ($site in Get-ZmsSiteCollections -Config $config) {
                Write-ZmsStep "Processing site collection: $($site.title) [$($site.url)]"
                $result = New-ZmsSiteCollectionSafe -Site $site -OwnerEmail $config.ownerEmail -DryRun:$DryRun
                $results += $result
                Write-ZmsSafeResult -Result $result
            }

            $failed = Complete-ZmsStepFromResults -Results $results
            if ($failed -gt 0 -and -not $DryRun) {
                $exitCode = 1
            }
        } catch {
            Write-ZmsError $_.Exception.Message
            Update-ZmsExecutionStatus -StepName "Create Site Collections" -Status "failed" -Failed 1 -Message $_.Exception.Message
            $exitCode = 1
        } finally {
            Stop-ZmsTranscript
        }

        exit $exitCode
        """;
    }

    private static string GenerateSubsitesScript()
    {
        return GenerateCommonScriptHeader("Create SharePoint subsites from ZMS config.") + """
        param(
            [string]$ConfigPath = (Join-Path $PSScriptRoot "..\config\zms-spo-environment.json"),
            [string]$ClientId,
            [switch]$DryRun,
            [switch]$VerboseLogging
        )

        """ + GenerateImportBlock() + GenerateResultHandlingBlock("Create Subsites") + """

        $exitCode = 0
        Start-ZmsTranscript -ScriptName (Split-Path -Leaf $PSCommandPath) -VerboseLogging:$VerboseLogging

        try {
            $config = Get-ZmsConfig -ConfigPath $ConfigPath
            $effectiveClientId = if ($ClientId) { $ClientId } else { $config.clientIdPlaceholder }
            Update-ZmsExecutionStatus -StepName "Create Subsites" -Status "running" -Message "Subsite step started."

            $results = @()
            foreach ($site in Get-ZmsSiteCollections -Config $config) {
                Connect-ZmsSharePointSite -Url $site.url -ClientId $effectiveClientId -DryRun:$DryRun
                foreach ($subsite in @(ConvertTo-ZmsArray $site.subsites)) {
                    Write-ZmsStep "Processing subsite: $($subsite.title) [$($subsite.url)]"
                    $result = New-ZmsSubsiteSafe -Subsite $subsite -DryRun:$DryRun
                    $results += $result
                    Write-ZmsSafeResult -Result $result
                }
            }

            $failed = Complete-ZmsStepFromResults -Results $results
            if ($failed -gt 0) {
                Write-ZmsWarning "Subsite creation may be disabled in this tenant. Enable custom script/subsite capability or convert subsites into separate modern sites."
                if (-not $DryRun) {
                    $exitCode = 1
                }
            }
        } catch {
            Write-ZmsError $_.Exception.Message
            Update-ZmsExecutionStatus -StepName "Create Subsites" -Status "failed" -Failed 1 -Message $_.Exception.Message
            $exitCode = 1
        } finally {
            Stop-ZmsTranscript
        }

        exit $exitCode
        """;
    }

    private static string GenerateLibrariesListsMetadataScript()
    {
        return GenerateCommonScriptHeader("Create libraries, lists, and metadata fields from ZMS config.") + """
        param(
            [string]$ConfigPath = (Join-Path $PSScriptRoot "..\config\zms-spo-environment.json"),
            [string]$ClientId,
            [switch]$DryRun,
            [switch]$VerboseLogging
        )

        """ + GenerateImportBlock() + GenerateResultHandlingBlock("Create Libraries Lists Metadata") + """

        $exitCode = 0
        Start-ZmsTranscript -ScriptName (Split-Path -Leaf $PSCommandPath) -VerboseLogging:$VerboseLogging

        try {
            $config = Get-ZmsConfig -ConfigPath $ConfigPath
            $effectiveClientId = if ($ClientId) { $ClientId } else { $config.clientIdPlaceholder }
            Update-ZmsExecutionStatus -StepName "Create Libraries Lists Metadata" -Status "running" -Message "Libraries, lists, and metadata step started."

            $results = @()
            foreach ($site in Get-ZmsSiteCollections -Config $config) {
                Connect-ZmsSharePointSite -Url $site.url -ClientId $effectiveClientId -DryRun:$DryRun

                foreach ($field in @(ConvertTo-ZmsArray $site.metadataFields)) {
                    Write-ZmsStep "Processing site field: $($field.name) [$($field.type)]"
                    $result = New-ZmsFieldSafe -Field $field -DryRun:$DryRun
                    $results += $result
                    Write-ZmsSafeResult -Result $result
                }

                foreach ($library in @(ConvertTo-ZmsArray $site.libraries)) {
                    $result = New-ZmsLibrarySafe -Library $library -DryRun:$DryRun
                    $results += $result
                    Write-ZmsSafeResult -Result $result

                    foreach ($fieldId in @(ConvertTo-ZmsArray $library.metadataFieldIds)) {
                        $field = @(ConvertTo-ZmsArray $site.metadataFields) | Where-Object { $_.id -eq $fieldId } | Select-Object -First 1
                        if ($field) {
                            Write-ZmsStep "Processing library field: $($field.name) [$($field.type)] -> $($library.title)"
                            $fieldResult = New-ZmsFieldSafe -Field $field -TargetList $library.title -DryRun:$DryRun
                            $results += $fieldResult
                            Write-ZmsSafeResult -Result $fieldResult
                        }
                    }
                }

                foreach ($list in @(ConvertTo-ZmsArray $site.lists)) {
                    $result = New-ZmsListSafe -List $list -DryRun:$DryRun
                    $results += $result
                    Write-ZmsSafeResult -Result $result

                    foreach ($field in @(ConvertTo-ZmsArray $list.columns)) {
                        Write-ZmsStep "Processing list field: $($field.name) [$($field.type)] -> $($list.title)"
                        $fieldResult = New-ZmsFieldSafe -Field $field -TargetList $list.title -DryRun:$DryRun
                        $results += $fieldResult
                        Write-ZmsSafeResult -Result $fieldResult
                    }
                }
            }

            $failed = Complete-ZmsStepFromResults -Results $results
            if ($failed -gt 0 -and -not $DryRun) {
                $exitCode = 1
            }
        } catch {
            Write-ZmsError $_.Exception.Message
            Update-ZmsExecutionStatus -StepName "Create Libraries Lists Metadata" -Status "failed" -Failed 1 -Message $_.Exception.Message
            $exitCode = 1
        } finally {
            Stop-ZmsTranscript
        }

        exit $exitCode
        """;
    }

    private static string GenerateGroupsPermissionsScript()
    {
        return GenerateCommonScriptHeader("Create SharePoint groups and apply configured permissions.") + """
        param(
            [string]$ConfigPath = (Join-Path $PSScriptRoot "..\config\zms-spo-environment.json"),
            [string]$ClientId,
            [switch]$DryRun,
            [switch]$VerboseLogging
        )

        """ + GenerateImportBlock() + GenerateResultHandlingBlock("Create Groups Permissions") + """

        # Strong safety rule: Permission script does not remove existing site collection admins or owners.
        $exitCode = 0
        Start-ZmsTranscript -ScriptName (Split-Path -Leaf $PSCommandPath) -VerboseLogging:$VerboseLogging

        try {
            $config = Get-ZmsConfig -ConfigPath $ConfigPath
            $effectiveClientId = if ($ClientId) { $ClientId } else { $config.clientIdPlaceholder }
            Update-ZmsExecutionStatus -StepName "Create Groups Permissions" -Status "running" -Message "Groups and permissions step started."
            Write-ZmsWarning "Permission script does not remove existing site collection admins or owners."

            $results = @()
            foreach ($site in Get-ZmsSiteCollections -Config $config) {
                Connect-ZmsSharePointSite -Url $site.url -ClientId $effectiveClientId -DryRun:$DryRun

                foreach ($group in @(ConvertTo-ZmsArray $site.permissionGroups)) {
                    $result = New-ZmsGroupSafe -Group $group -DryRun:$DryRun
                    $results += $result
                    Write-ZmsSafeResult -Result $result

                    try {
                        if ($DryRun) {
                            $permissionResult = New-ZmsSharePointResult -ItemType "PermissionLevel" -Name $group.name -Target $group.role -Status "planned" -Action "assign" -Message "Would assign role '$($group.role)' to group '$($group.name)'."
                        } else {
                            Set-PnPGroupPermissions -Identity $group.name -AddRole $group.role
                            $permissionResult = New-ZmsSharePointResult -ItemType "PermissionLevel" -Name $group.name -Target $group.role -Status "created" -Action "assign" -Message "Assigned role '$($group.role)' to group."
                        }
                    } catch {
                        $permissionResult = New-ZmsSharePointResult -ItemType "PermissionLevel" -Name $group.name -Target $group.role -Status "failed" -Action "assign" -Message "Failed to assign role." -Error $_.Exception.Message
                    }
                    $results += $permissionResult
                    Write-ZmsSafeResult -Result $permissionResult
                }

                foreach ($rule in @(ConvertTo-ZmsArray $site.permissionRules)) {
                    if ($rule.inheritance -ne "Broken") {
                        continue
                    }

                    $pathParts = $rule.targetPath -split "/"
                    $listTitle = $pathParts[0]
                    Write-ZmsWarning "Restricted area configured: $($rule.targetPath)"
                    try {
                        if ($DryRun) {
                            $ruleResult = New-ZmsSharePointResult -ItemType "PermissionRule" -Name $rule.id -Target $rule.targetPath -Status "planned" -Action "break-inheritance" -Message "Would break inheritance on configured area and copy existing role assignments."
                        } else {
                            Set-PnPList -Identity $listTitle -BreakRoleInheritance -CopyRoleAssignments
                            foreach ($groupName in @(ConvertTo-ZmsArray $rule.groups)) {
                                Set-PnPListPermission -Identity $listTitle -Group $groupName -AddRole "Contribute"
                            }
                            $ruleResult = New-ZmsSharePointResult -ItemType "PermissionRule" -Name $rule.id -Target $rule.targetPath -Status "created" -Action "break-inheritance" -Message "Applied configured broken inheritance rule."
                        }
                    } catch {
                        $ruleResult = New-ZmsSharePointResult -ItemType "PermissionRule" -Name $rule.id -Target $rule.targetPath -Status "failed" -Action "break-inheritance" -Message "Failed to apply permission rule." -Error $_.Exception.Message
                    }
                    $results += $ruleResult
                    Write-ZmsSafeResult -Result $ruleResult
                }
            }

            $failed = Complete-ZmsStepFromResults -Results $results
            if ($failed -gt 0 -and -not $DryRun) {
                $exitCode = 1
            }
        } catch {
            Write-ZmsError $_.Exception.Message
            Update-ZmsExecutionStatus -StepName "Create Groups Permissions" -Status "failed" -Failed 1 -Message $_.Exception.Message
            $exitCode = 1
        } finally {
            Stop-ZmsTranscript
        }

        exit $exitCode
        """;
    }

    private static string GenerateFoldersAndSampleFilesScript()
    {
        return GenerateCommonScriptHeader("Create folder structures and placeholder sample files.") + """
        param(
            [string]$ConfigPath = (Join-Path $PSScriptRoot "..\config\zms-spo-environment.json"),
            [string]$ClientId,
            [switch]$CreateLargeFiles,
            [switch]$DryRun,
            [switch]$VerboseLogging
        )

        """ + GenerateImportBlock() + GenerateResultHandlingBlock("Create Folders And Sample Files") + """

        function Ensure-ZmsSampleFile {
            param(
                [Parameter(Mandatory = $true)][string]$Path,
                [Parameter(Mandatory = $true)][string]$Text
            )

            if (-not (Test-Path -LiteralPath $Path)) {
                $directory = Split-Path -Parent $Path
                if (-not (Test-Path $directory)) {
                    New-Item -ItemType Directory -Force -Path $directory | Out-Null
                }
                $Text | Set-Content -LiteralPath $Path -Encoding UTF8
            }
        }

        $exitCode = 0
        Start-ZmsTranscript -ScriptName (Split-Path -Leaf $PSCommandPath) -VerboseLogging:$VerboseLogging

        try {
            $config = Get-ZmsConfig -ConfigPath $ConfigPath
            $paths = Get-ZmsPackagePaths -ConfigPath $ConfigPath
            $effectiveClientId = if ($ClientId) { $ClientId } else { $config.clientIdPlaceholder }
            Update-ZmsExecutionStatus -StepName "Create Folders And Sample Files" -Status "running" -Message "Folders and sample files step started."

            $results = @()
            foreach ($site in Get-ZmsSiteCollections -Config $config) {
                Connect-ZmsSharePointSite -Url $site.url -ClientId $effectiveClientId -DryRun:$DryRun

                foreach ($library in @(ConvertTo-ZmsArray $site.libraries)) {
                    foreach ($folder in @(ConvertTo-ZmsArray $library.folders)) {
                        $relativeFolder = $folder.path.Replace($library.title + "/", "")
                        $targetPath = "$($library.title)/$relativeFolder"
                        try {
                            if ($DryRun) {
                                $folderResult = New-ZmsSharePointResult -ItemType "Folder" -Name $folder.name -Target $targetPath -Status "planned" -Action "create" -Message "Would ensure folder exists."
                            } else {
                                Resolve-PnPFolder -SiteRelativePath $targetPath | Out-Null
                                $folderResult = New-ZmsSharePointResult -ItemType "Folder" -Name $folder.name -Target $targetPath -Status "created" -Action "ensure" -Message "Ensured folder exists."
                            }
                        } catch {
                            $folderResult = New-ZmsSharePointResult -ItemType "Folder" -Name $folder.name -Target $targetPath -Status "failed" -Action "create" -Message "Failed to ensure folder." -Error $_.Exception.Message
                        }
                        $results += $folderResult
                        Write-ZmsSafeResult -Result $folderResult
                    }

                    if ($library.sampleFileCount -gt 0) {
                        $sampleFilePath = Join-Path $paths.SampleFiles "$($library.id)-sample.txt"
                        if ($DryRun) {
                            $fileResult = New-ZmsSharePointResult -ItemType "SampleFile" -Name "$($library.id)-sample.txt" -Target $library.title -Status "planned" -Action "upload" -Message "Would generate small local sample file if missing and upload one placeholder file."
                        } else {
                            Ensure-ZmsSampleFile -Path $sampleFilePath -Text "ZMS placeholder sample file for $($library.title)"
                            Add-PnPFile -Path $sampleFilePath -Folder $library.title | Out-Null
                            $fileResult = New-ZmsSharePointResult -ItemType "SampleFile" -Name "$($library.id)-sample.txt" -Target $library.title -Status "created" -Action "upload" -Message "Uploaded placeholder sample file."
                        }
                        $results += $fileResult
                        Write-ZmsSafeResult -Result $fileResult
                    }
                }
            }

            if ($CreateLargeFiles) {
                Write-ZmsWarning "CreateLargeFiles was requested. Keep generated files small unless storage impact is approved."
            } else {
                Write-ZmsInfo "Large sample files skipped by default."
            }

            $failed = Complete-ZmsStepFromResults -Results $results
            if ($failed -gt 0 -and -not $DryRun) {
                $exitCode = 1
            }
        } catch {
            Write-ZmsError $_.Exception.Message
            Update-ZmsExecutionStatus -StepName "Create Folders And Sample Files" -Status "failed" -Failed 1 -Message $_.Exception.Message
            $exitCode = 1
        } finally {
            Stop-ZmsTranscript
        }

        exit $exitCode
        """;
    }

    private static string GenerateEdgeCasesScript()
    {
        return GenerateCommonScriptHeader("Apply migration edge-case examples from ZMS config.") + """
        param(
            [string]$ConfigPath = (Join-Path $PSScriptRoot "..\config\zms-spo-environment.json"),
            [string]$ClientId,
            [switch]$CreateLargeFiles,
            [switch]$DryRun,
            [switch]$VerboseLogging
        )

        """ + GenerateImportBlock() + GenerateResultHandlingBlock("Apply Migration Edge Cases") + """

        $exitCode = 0
        Start-ZmsTranscript -ScriptName (Split-Path -Leaf $PSCommandPath) -VerboseLogging:$VerboseLogging

        try {
            $config = Get-ZmsConfig -ConfigPath $ConfigPath
            $effectiveClientId = if ($ClientId) { $ClientId } else { $config.clientIdPlaceholder }
            Update-ZmsExecutionStatus -StepName "Apply Migration Edge Cases" -Status "running" -Message "Migration edge case step started."

            $results = @()
            foreach ($site in Get-ZmsSiteCollections -Config $config) {
                Connect-ZmsSharePointSite -Url $site.url -ClientId $effectiveClientId -DryRun:$DryRun
                foreach ($edgeCase in @(ConvertTo-ZmsArray $site.edgeCases)) {
                    Write-ZmsStep "Preparing migration edge case: $($edgeCase.title)"
                    $message = "Edge case documented for review. Invalid SharePoint filename examples are documented instead of forcing upload failures."
                    $resultStatus = if ($DryRun) { "planned" } else { "created" }
                    $result = New-ZmsSharePointResult -ItemType "MigrationEdgeCase" -Name $edgeCase.title -Target $edgeCase.affectedPath -Status $resultStatus -Action "document" -Message $message
                    $results += $result
                    Write-ZmsSafeResult -Result $result
                }

                foreach ($folder in @(ConvertTo-ZmsArray $site.folderStructures)) {
                    if ($folder.longPathExample -or $folder.archived -or $folder.largeFilePlaceholder) {
                        $message = "Would preserve configured folder characteristic: archived=$($folder.archived), longPath=$($folder.longPathExample), largeFile=$($folder.largeFilePlaceholder)."
                        $folderStatus = if ($DryRun) { "planned" } else { "skipped" }
                        $folderResult = New-ZmsSharePointResult -ItemType "EdgeFolder" -Name $folder.name -Target $folder.path -Status $folderStatus -Action "review" -Message $message
                        $results += $folderResult
                        Write-ZmsSafeResult -Result $folderResult
                    }
                }
            }

            if ($CreateLargeFiles) {
                Write-ZmsWarning "Optional large file generation was requested. No huge files are created by default; review storage impact before adding them."
            } else {
                Write-ZmsInfo "Large file edge cases were documented only. No large files were generated."
            }

            $failed = Complete-ZmsStepFromResults -Results $results
            if ($failed -gt 0 -and -not $DryRun) {
                $exitCode = 1
            }
        } catch {
            Write-ZmsError $_.Exception.Message
            Update-ZmsExecutionStatus -StepName "Apply Migration Edge Cases" -Status "failed" -Failed 1 -Message $_.Exception.Message
            $exitCode = 1
        } finally {
            Stop-ZmsTranscript
        }

        exit $exitCode
        """;
    }

    private static string GenerateInventoryReportScript()
    {
        return GenerateCommonScriptHeader("Generate local CSV and JSON inventory reports from ZMS config.") + """
        param(
            [string]$ConfigPath = (Join-Path $PSScriptRoot "..\config\zms-spo-environment.json"),
            [string]$ClientId,
            [switch]$UseLiveTenant,
            [switch]$VerboseLogging
        )

        """ + GenerateImportBlock() + """

        $exitCode = 0
        Start-ZmsTranscript -ScriptName (Split-Path -Leaf $PSCommandPath) -VerboseLogging:$VerboseLogging

        try {
            $config = Get-ZmsConfig -ConfigPath $ConfigPath
            $paths = Get-ZmsPackagePaths -ConfigPath $ConfigPath
            Update-ZmsExecutionStatus -StepName "Generate Inventory Report" -Status "running" -Message "Inventory report step started."
            $summary = Get-ZmsEnvironmentSummary -Config $config

            $inventory = foreach ($site in Get-ZmsSiteCollections -Config $config) {
                $liveListCount = $null
                if ($UseLiveTenant) {
                    $effectiveClientId = if ($ClientId) { $ClientId } else { $config.clientIdPlaceholder }
                    Connect-ZmsSharePointSite -Url $site.url -ClientId $effectiveClientId
                    $liveListCount = @(Get-PnPList).Count
                }

                [PSCustomObject]@{
                    siteCollection = $site.title
                    url = $site.url
                    department = $site.department
                    expectedSubsites = @(ConvertTo-ZmsArray $site.subsites).Count
                    expectedLibraries = @(ConvertTo-ZmsArray $site.libraries).Count
                    expectedLists = @(ConvertTo-ZmsArray $site.lists).Count
                    expectedMetadataFields = @(ConvertTo-ZmsArray $site.metadataFields).Count
                    expectedPermissionGroups = @(ConvertTo-ZmsArray $site.permissionGroups).Count
                    expectedEdgeCases = @(ConvertTo-ZmsArray $site.edgeCases).Count
                    liveListCount = $liveListCount
                }
            }

            $csvPath = Join-Path $paths.Reports "environment-inventory.csv"
            $jsonPath = Join-Path $paths.Reports "environment-inventory.json"
            $summaryPath = Join-Path $paths.Reports "environment-summary.md"
            $inventory | Export-Csv -NoTypeInformation -Path $csvPath
            Export-ZmsJson -Value $inventory -Path $jsonPath

            $lines = @()
            $lines += "# Environment Summary"
            $lines += ""
            $lines += "- Tenant: $($summary.tenantName)"
            $lines += "- Site Collections: $($summary.siteCollections)"
            $lines += "- Subsites: $($summary.subsites)"
            $lines += "- Libraries: $($summary.libraries)"
            $lines += "- Lists: $($summary.lists)"
            $lines += "- Metadata Fields: $($summary.metadataFields)"
            $lines += "- Permission Groups: $($summary.permissionGroups)"
            $lines += "- Edge Cases: $($summary.edgeCases)"
            $lines -join [Environment]::NewLine | Set-Content -LiteralPath $summaryPath -Encoding UTF8

            Write-ZmsSuccess "Generated inventory reports: $csvPath, $jsonPath, $summaryPath"
            Update-ZmsExecutionStatus -StepName "Generate Inventory Report" -Status "completed" -Created 3 -Message "Inventory reports generated."
        } catch {
            Write-ZmsError $_.Exception.Message
            Update-ZmsExecutionStatus -StepName "Generate Inventory Report" -Status "failed" -Failed 1 -Message $_.Exception.Message
            $exitCode = 1
        } finally {
            Stop-ZmsTranscript
        }

        exit $exitCode
        """;
    }

    private static string GenerateRunPreflightScript()
    {
        return GenerateCommonScriptHeader("Run all safe preflight checks without creating SharePoint objects.") + """
        param(
            [string]$ConfigPath = (Join-Path $PSScriptRoot "..\config\zms-spo-environment.json"),
            [switch]$VerboseLogging
        )

        """ + GenerateImportBlock() + """

        $exitCode = 0
        Start-ZmsTranscript -ScriptName (Split-Path -Leaf $PSCommandPath) -VerboseLogging:$VerboseLogging

        try {
            Write-ZmsStep "Running safe preflight orchestration"
            $preflightScript = Join-Path $PSScriptRoot "00-Check-Prerequisites.ps1"
            $childExitCode = Invoke-ZmsPowerShellScript -ScriptPath $preflightScript -ConfigPath $ConfigPath -VerboseLogging:$VerboseLogging
            if ($childExitCode -ne 0) {
                $exitCode = $childExitCode
            }

            $config = Get-ZmsConfig -ConfigPath $ConfigPath
            $checks = @(Test-ZmsPrerequisites -ConfigPath $ConfigPath)
            New-ZmsPreflightReport -Config $config -Checks $checks | Out-Null
            if ($exitCode -eq 0) {
                Write-ZmsSuccess "Preflight orchestration completed. No tenant changes were made."
            } else {
                Write-ZmsError "Preflight orchestration completed with failures. No tenant changes were made."
            }
        } catch {
            Write-ZmsError $_.Exception.Message
            Update-ZmsExecutionStatus -StepName "Check prerequisites" -Status "failed" -Failed 1 -Message $_.Exception.Message
            $exitCode = 1
        } finally {
            Stop-ZmsTranscript
        }

        exit $exitCode
        """;
    }

    private static string GenerateRunDryRunScript()
    {
        return GenerateCommonScriptHeader("Simulate full environment creation without changing the SharePoint tenant.") + """
        param(
            [string]$ConfigPath = (Join-Path $PSScriptRoot "..\config\zms-spo-environment.json"),
            [switch]$VerboseLogging
        )

        """ + GenerateImportBlock() + """

        $exitCode = 0
        Start-ZmsTranscript -ScriptName (Split-Path -Leaf $PSCommandPath) -VerboseLogging:$VerboseLogging

        try {
            Write-ZmsStep "Running full dry-run. No SharePoint objects will be created."
            $scripts = @(
                "01-Create-SiteCollections.ps1",
                "02-Create-Subsites.ps1",
                "03-Create-Libraries-Lists-Metadata.ps1",
                "04-Create-Groups-Permissions.ps1",
                "05-Create-Folders-And-SampleFiles.ps1",
                "06-Apply-Migration-EdgeCases.ps1"
            )

            foreach ($script in $scripts) {
                $scriptPath = Join-Path $PSScriptRoot $script
                Write-ZmsStep "Dry-running $script"
                $childExitCode = Invoke-ZmsPowerShellScript -ScriptPath $scriptPath -ConfigPath $ConfigPath -DryRun -VerboseLogging:$VerboseLogging
                if ($childExitCode -ne 0) {
                    Write-ZmsError "$script dry-run failed with exit code $childExitCode"
                    $exitCode = $childExitCode
                    break
                }
            }

            $config = Get-ZmsConfig -ConfigPath $ConfigPath
            $reportPath = New-ZmsDryRunReport -Config $config
            Write-ZmsSuccess "Dry-run report written to $reportPath"
        } catch {
            Write-ZmsError $_.Exception.Message
            $exitCode = 1
        } finally {
            Stop-ZmsTranscript
        }

        exit $exitCode
        """;
    }

    private static string GenerateRunAllSafeScript()
    {
        return GenerateCommonScriptHeader("Run real SharePoint creation in safe order after explicit admin confirmation.") + """
        param(
            [string]$ConfigPath = (Join-Path $PSScriptRoot "..\config\zms-spo-environment.json"),
            [string]$ClientId,
            [switch]$VerboseLogging,
            [switch]$CreateLargeFiles
        )

        """ + GenerateImportBlock() + """

        $exitCode = 0
        Start-ZmsTranscript -ScriptName (Split-Path -Leaf $PSCommandPath) -VerboseLogging:$VerboseLogging

        try {
            Write-ZmsWarning "WARNING: This script can create SharePoint Online objects in the configured tenant."
            Write-ZmsWarning "Only run real execution in a test tenant or approved SharePoint environment."
            Write-ZmsWarning "This package never deletes existing tenant content and skips existing objects."
            $confirmation = Read-Host "Type CREATE ZMS TEST ENVIRONMENT to continue"
            if ($confirmation -ne "CREATE ZMS TEST ENVIRONMENT") {
                Write-ZmsWarning "Confirmation did not match. Real execution aborted before any tenant changes."
                exit 2
            }

            $scripts = @(
                "01-Create-SiteCollections.ps1",
                "02-Create-Subsites.ps1",
                "03-Create-Libraries-Lists-Metadata.ps1",
                "04-Create-Groups-Permissions.ps1",
                "05-Create-Folders-And-SampleFiles.ps1",
                "06-Apply-Migration-EdgeCases.ps1",
                "07-Generate-InventoryReport.ps1"
            )

            foreach ($script in $scripts) {
                $scriptPath = Join-Path $PSScriptRoot $script
                Write-ZmsStep "Running $script"
                if ($script -eq "05-Create-Folders-And-SampleFiles.ps1" -or $script -eq "06-Apply-Migration-EdgeCases.ps1") {
                    $childExitCode = Invoke-ZmsPowerShellScript -ScriptPath $scriptPath -ConfigPath $ConfigPath -ClientId $ClientId -VerboseLogging:$VerboseLogging -CreateLargeFiles:$CreateLargeFiles
                } elseif ($script -eq "07-Generate-InventoryReport.ps1") {
                    $childExitCode = Invoke-ZmsPowerShellScript -ScriptPath $scriptPath -ConfigPath $ConfigPath -ClientId $ClientId -VerboseLogging:$VerboseLogging -UseLiveTenant
                } else {
                    $childExitCode = Invoke-ZmsPowerShellScript -ScriptPath $scriptPath -ConfigPath $ConfigPath -ClientId $ClientId -VerboseLogging:$VerboseLogging
                }

                if ($childExitCode -ne 0) {
                    Write-ZmsError "$script failed with exit code $childExitCode. Stopping before later steps."
                    $exitCode = $childExitCode
                    break
                }
            }

            $config = Get-ZmsConfig -ConfigPath $ConfigPath
            New-ZmsExecutionSummary -Config $config | Out-Null
            Write-ZmsSuccess "Execution summary generated."
        } catch {
            Write-ZmsError $_.Exception.Message
            $exitCode = 1
        } finally {
            Stop-ZmsTranscript
        }

        exit $exitCode
        """;
    }

    private static string GenerateDiscoveryReadOnlyScript()
    {
        return GenerateCommonScriptHeader("Run read-only SharePoint discovery and export backend-compatible inventory JSON/CSV. This script never creates, updates, deletes, uploads, migrates, or changes permissions.") + """

        # This script is read-only. It performs discovery only and must not modify the SharePoint tenant.
        param(
            [string]$ConfigPath = (Join-Path $PSScriptRoot "..\config\zms-spo-environment.json"),
            [string]$ClientId,
            [string]$OutputPath = (Join-Path $PSScriptRoot "..\discovery-output"),
            [switch]$IncludeFiles,
            [switch]$IncludePermissions,
            [switch]$IncludeMetadata,
            [switch]$IncludeSubsites,
            [int]$PageSize = 500,
            [int]$MaxItemsPerLibrary = 1000,
            [switch]$VerboseLogging
        )

        """ + GenerateDiscoveryReadOnlyImportBlock() + """

        $exitCode = 0
        $scanStartedAt = (Get-Date).ToUniversalTime()
        $scanId = "live-{0}" -f $scanStartedAt.ToString("yyyyMMddHHmmss")
        $outputDirectory = [System.IO.Path]::GetFullPath($OutputPath)
        $rawDirectory = Join-Path $outputDirectory "raw"
        $scanLogPath = Join-Path $outputDirectory "scan-log.txt"

        $script:warnings = @()
        $script:errors = @()
        $script:siteCollections = @()
        $script:inventoryItems = @()
        $script:permissionRisks = @()
        $script:metadataFindings = @()
        $script:migrationRisks = @()
        $script:rawSites = @()
        $script:rawLibraries = @()
        $script:rawLists = @()
        $script:rawFields = @()
        $script:rawPermissions = @()
        $script:rawFiles = @()

        function Write-ZmsDiscoveryLog {
            param(
                [Parameter(Mandatory = $true)][string]$Level,
                [Parameter(Mandatory = $true)][string]$Message
            )

            $line = "{0} [{1}] {2}" -f (Get-Date).ToUniversalTime().ToString("o"), $Level.ToUpperInvariant(), $Message
            Add-Content -LiteralPath $scanLogPath -Value $line
            if ($Level -eq "ERROR") {
                Write-ZmsError $Message
            } elseif ($Level -eq "WARNING") {
                Write-ZmsWarning $Message
            } elseif ($VerboseLogging -or $Level -ne "DEBUG") {
                Write-ZmsInfo $Message
            }
        }

        function Add-ZmsDiscoveryWarning {
            param([Parameter(Mandatory = $true)][string]$Message)
            $script:warnings += $Message
            Write-ZmsDiscoveryLog -Level "WARNING" -Message $Message
        }

        function Add-ZmsDiscoveryError {
            param([Parameter(Mandatory = $true)][string]$Message)
            $script:errors += $Message
            Write-ZmsDiscoveryLog -Level "ERROR" -Message $Message
        }

        function ConvertTo-ZmsDiscoveryJson {
            param(
                [Parameter(Mandatory = $true)][object]$Value,
                [Parameter(Mandatory = $true)][string]$Path
            )

            $Value | ConvertTo-Json -Depth 80 | Set-Content -LiteralPath $Path -Encoding UTF8
        }

        function Export-ZmsDiscoveryCsv {
            param(
                [Parameter(Mandatory = $true)][object[]]$Rows,
                [Parameter(Mandatory = $true)][string[]]$Headers,
                [Parameter(Mandatory = $true)][string]$Path
            )

            if ($Rows.Count -gt 0) {
                $Rows | Export-Csv -NoTypeInformation -LiteralPath $Path -Encoding UTF8
            } else {
                ($Headers -join ",") | Set-Content -LiteralPath $Path -Encoding UTF8
            }
        }

        function ConvertTo-ZmsArraySafe {
            param([object]$Value)
            if ($null -eq $Value) {
                return @()
            }

            return @($Value)
        }

        function Get-ZmsFieldValue {
            param(
                [object]$Item,
                [Parameter(Mandatory = $true)][string]$Name
            )

            if ($null -eq $Item -or $null -eq $Item.FieldValues) {
                return $null
            }

            if ($Item.FieldValues.ContainsKey($Name)) {
                return $Item.FieldValues[$Name]
            }

            return $null
        }

        function Get-ZmsTextValue {
            param([object]$Value)
            if ($null -eq $Value) {
                return ""
            }

            if ($Value -is [string]) {
                return $Value
            }

            if ($Value.PSObject.Properties["LookupValue"]) {
                return [string]$Value.LookupValue
            }

            if ($Value.PSObject.Properties["Email"]) {
                return [string]$Value.Email
            }

            return [string]$Value
        }

        function Get-ZmsRiskLevelFromText {
            param([string]$Value)
            if ([string]::IsNullOrWhiteSpace($Value)) {
                return "Low"
            }

            if ($Value -match "(?i)confidential|restricted|payroll|security|audit|tax|contracts") {
                return "High"
            }

            if ($Value -match "(?i)archive|archived|legal|finance|hr") {
                return "Medium"
            }

            return "Low"
        }

        function Add-ZmsMigrationRisk {
            param(
                [Parameter(Mandatory = $true)][string]$RiskType,
                [Parameter(Mandatory = $true)][string]$Site,
                [Parameter(Mandatory = $true)][string]$LibraryOrPath,
                [string]$Path = "",
                [string]$RiskLevel = "Low",
                [Parameter(Mandatory = $true)][string]$Description,
                [Parameter(Mandatory = $true)][string]$RecommendedAction
            )

            $script:migrationRisks += [PSCustomObject]@{
                id = [guid]::NewGuid().ToString("D")
                riskType = $RiskType
                site = $Site
                libraryOrPath = $LibraryOrPath
                path = $Path
                riskLevel = $RiskLevel
                description = $Description
                recommendedAction = $RecommendedAction
            }
        }

        function Get-ZmsPermissionSummary {
            param([object]$ClientObject)

            $groups = @()
            $users = @()
            $accessLevels = @()

            try {
                $assignments = @(Get-PnPProperty -ClientObject $ClientObject -Property RoleAssignments -ErrorAction Stop)
                foreach ($assignment in $assignments) {
                    $member = Get-PnPProperty -ClientObject $assignment -Property Member -ErrorAction Stop
                    $bindings = @(Get-PnPProperty -ClientObject $assignment -Property RoleDefinitionBindings -ErrorAction Stop)
                    $principalName = if ($member.Title) { [string]$member.Title } elseif ($member.LoginName) { [string]$member.LoginName } else { "Unknown Principal" }

                    if ($member.PrincipalType -match "SharePointGroup|SecurityGroup") {
                        $groups += $principalName
                    } else {
                        $users += $principalName
                    }

                    foreach ($binding in $bindings) {
                        if ($binding.Name) {
                            $accessLevels += [string]$binding.Name
                        }
                    }
                }
            } catch {
                Add-ZmsDiscoveryWarning "Could not expand role assignments. $($_.Exception.Message)"
            }

            return [PSCustomObject]@{
                groups = @($groups | Sort-Object -Unique)
                users = @($users | Sort-Object -Unique)
                accessLevels = @($accessLevels | Sort-Object -Unique)
            }
        }

        function Test-ZmsSystemList {
            param([Parameter(Mandatory = $true)][object]$List)

            if ($List.Hidden) {
                return $true
            }

            $systemTitles = @(
                "app packages",
                "app requests",
                "cache profiles",
                "content and structure reports",
                "converted forms",
                "form templates",
                "list template gallery",
                "maintenance log library",
                "master page gallery",
                "microfeed",
                "preservation hold library",
                "solution gallery",
                "style library",
                "taxonomieshiddenlist",
                "theme gallery",
                "user information list",
                "web part gallery",
                "workflow history",
                "workflow tasks"
            )

            return $systemTitles -contains ([string]$List.Title).ToLowerInvariant()
        }

        function Get-ZmsWebsToScan {
            param(
                [Parameter(Mandatory = $true)][string]$SiteUrl,
                [Parameter(Mandatory = $true)][string]$SiteTitle
            )

            $webs = @()
            $rootWeb = Get-PnPWeb -Includes Title,Url,Webs,Lists,RoleAssignments,HasUniqueRoleAssignments -ErrorAction Stop
            $webs += [PSCustomObject]@{
                title = $rootWeb.Title
                url = $rootWeb.Url
                parentUrl = ""
                isRoot = $true
            }

            if ($IncludeSubsites) {
                $queue = New-Object System.Collections.Queue
                foreach ($child in @(Get-PnPProperty -ClientObject $rootWeb -Property Webs -ErrorAction Stop)) {
                    $queue.Enqueue([PSCustomObject]@{ web = $child; parentUrl = $rootWeb.Url })
                }

                while ($queue.Count -gt 0) {
                    $entry = $queue.Dequeue()
                    $childWeb = $entry.web
                    $webs += [PSCustomObject]@{
                        title = $childWeb.Title
                        url = $childWeb.Url
                        parentUrl = $entry.parentUrl
                        isRoot = $false
                    }

                    try {
                        foreach ($grandChild in @(Get-PnPProperty -ClientObject $childWeb -Property Webs -ErrorAction Stop)) {
                            $queue.Enqueue([PSCustomObject]@{ web = $grandChild; parentUrl = $childWeb.Url })
                        }
                    } catch {
                        Add-ZmsDiscoveryWarning "Could not enumerate subsites under $($childWeb.Url). $($_.Exception.Message)"
                    }
                }
            }

            return $webs
        }

        function Get-ZmsRequiredFieldNames {
            param([object[]]$Fields)
            return @(
                $Fields |
                    Where-Object { $_.required -and -not $_.hidden -and -not $_.readOnly } |
                    ForEach-Object { $_.internalName } |
                    Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
                    Sort-Object -Unique
            )
        }

        function New-ZmsInventoryItem {
            param(
                [string]$SiteCollection,
                [string]$Subsite = "",
                [string]$Library = "",
                [string]$ItemType,
                [string]$Path,
                [int]$FileCount = 0,
                [long]$SizeBytes = 0,
                [int]$MetadataCount = 0,
                [string]$PermissionStatus = "Inherited",
                [string]$RiskLevel = "Low",
                [string]$ReadinessStatus = "Ready"
            )

            return [PSCustomObject]@{
                id = [guid]::NewGuid().ToString("D")
                siteCollection = $SiteCollection
                subsite = $Subsite
                library = $Library
                itemType = $ItemType
                path = $Path
                fileCount = $FileCount
                sizeBytes = $SizeBytes
                metadataCount = $MetadataCount
                permissionStatus = $PermissionStatus
                riskLevel = $RiskLevel
                readinessStatus = $ReadinessStatus
            }
        }

        function New-ZmsMetadataField {
            param(
                [object]$Field,
                [int]$MissingValueCount = 0
            )

            $mappingRisk = "Low"
            if ($Field.Required -and $MissingValueCount -gt 0) {
                $mappingRisk = "High"
            } elseif ($Field.Required -or ([string]$Field.TypeAsString) -match "(?i)User|Taxonomy|Lookup") {
                $mappingRisk = "Medium"
            }

            $choices = @()
            try {
                if ($Field.Choices) {
                    $choices = @($Field.Choices)
                }
            } catch {
                $choices = @()
            }

            return [PSCustomObject]@{
                id = if ($Field.Id) { [string]$Field.Id } else { [guid]::NewGuid().ToString("D") }
                name = [string]$Field.Title
                internalName = [string]$Field.InternalName
                fieldType = [string]$Field.TypeAsString
                required = [bool]$Field.Required
                hidden = [bool]$Field.Hidden
                readOnly = [bool]$Field.ReadOnlyField
                choices = $choices
                defaultValue = [string]$Field.DefaultValue
                missingValueCount = $MissingValueCount
                mappedTargetField = ""
                mappingRisk = $mappingRisk
            }
        }

        function Get-ZmsListItemsSafe {
            param(
                [Parameter(Mandatory = $true)][object]$List,
                [string[]]$FieldNames
            )

            if (-not $IncludeFiles) {
                return @()
            }

            $safeLimit = [Math]::Max(1, $MaxItemsPerLibrary)
            $safePageSize = [Math]::Max(1, $PageSize)
            $query = "<View Scope='RecursiveAll'><RowLimit>$safeLimit</RowLimit></View>"
            $fields = @("FileRef", "FileLeafRef", "File_x0020_Size", "SMTotalFileStreamSize", "FSObjType", "Modified", "Author", "Editor", "ContentType") + $FieldNames
            $fields = @($fields | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)

            try {
                return @(Get-PnPListItem -List $List.Title -PageSize $safePageSize -Query $query -Fields $fields -ErrorAction Stop)
            } catch {
                Add-ZmsDiscoveryWarning "Could not read list items from $($List.Title). $($_.Exception.Message)"
                return @()
            }
        }

        function Add-ZmsPermissionRisk {
            param(
                [string]$Site,
                [string]$Location,
                [string]$InheritanceStatus,
                [object]$PermissionSummary,
                [string]$RiskLevel,
                [string]$RecommendedAction
            )

            $finding = [PSCustomObject]@{
                id = [guid]::NewGuid().ToString("D")
                site = $Site
                libraryOrFolder = $Location
                inheritanceStatus = $InheritanceStatus
                groups = @(ConvertTo-ZmsArraySafe $PermissionSummary.groups)
                users = @(ConvertTo-ZmsArraySafe $PermissionSummary.users)
                accessLevels = @(ConvertTo-ZmsArraySafe $PermissionSummary.accessLevels)
                riskLevel = $RiskLevel
                recommendedAction = $RecommendedAction
            }

            $script:permissionRisks += $finding
            $script:rawPermissions += $finding
            Add-ZmsMigrationRisk -RiskType "Broken Permissions" -Site $Site -LibraryOrPath $Location -Path $Location -RiskLevel $RiskLevel -Description "Unique or broken permission inheritance detected." -RecommendedAction $RecommendedAction
            return $finding
        }

        function Add-ZmsPathAndContentRisks {
            param(
                [string]$Site,
                [string]$Library,
                [string]$Path,
                [long]$SizeBytes = 0,
                [string]$ItemType = "File"
            )

            if ($Path.Length -gt 350) {
                Add-ZmsMigrationRisk -RiskType "Long Paths" -Site $Site -LibraryOrPath $Library -Path $Path -RiskLevel "High" -Description "$ItemType path exceeds 350 characters." -RecommendedAction "Shorten folder and file names before migration."
            } elseif ($Path.Length -gt 250) {
                Add-ZmsMigrationRisk -RiskType "Long Paths" -Site $Site -LibraryOrPath $Library -Path $Path -RiskLevel "Medium" -Description "$ItemType path exceeds 250 characters." -RecommendedAction "Review path length against target platform limits."
            }

            if ($ItemType -eq "File") {
                if ($SizeBytes -gt 524288000) {
                    Add-ZmsMigrationRisk -RiskType "Large Files" -Site $Site -LibraryOrPath $Library -Path $Path -RiskLevel "High" -Description "File is larger than 500 MB." -RecommendedAction "Validate large file migration handling and bandwidth planning."
                } elseif ($SizeBytes -gt 104857600) {
                    Add-ZmsMigrationRisk -RiskType "Large Files" -Site $Site -LibraryOrPath $Library -Path $Path -RiskLevel "Medium" -Description "File is larger than 100 MB." -RecommendedAction "Review large files before migration batching."
                }
            }

            if ($Path -match "(?i)/?archive/?|/archived/|2021|2022|2023") {
                Add-ZmsMigrationRisk -RiskType "Archived Content" -Site $Site -LibraryOrPath $Library -Path $Path -RiskLevel "Medium" -Description "$ItemType appears to be archived or older content." -RecommendedAction "Confirm retention and archive migration rules."
            }

            if ($Path -match "(?i)confidential|restricted|payroll|security|audit|tax|contracts") {
                Add-ZmsMigrationRisk -RiskType "Restricted Content" -Site $Site -LibraryOrPath $Library -Path $Path -RiskLevel "High" -Description "$ItemType path indicates restricted or sensitive content." -RecommendedAction "Validate target permissions and access mapping before migration."
            }
        }

        function Get-ZmsReadinessStatus {
            param([string]$RiskLevel)
            if ($RiskLevel -eq "High") {
                return "Needs remediation"
            }

            if ($RiskLevel -eq "Medium") {
                return "Review"
            }

            return "Ready"
        }

        function Get-ZmsReadinessScore {
            $allRiskLevels = @()
            $allRiskLevels += @($script:migrationRisks | ForEach-Object { $_.riskLevel })
            $allRiskLevels += @($script:permissionRisks | ForEach-Object { $_.riskLevel })
            $allRiskLevels += @($script:metadataFindings | ForEach-Object { $_.mappingRisk })

            $score = 100
            foreach ($level in $allRiskLevels) {
                if ($level -eq "High" -or $level -eq "Critical") {
                    $score -= 3
                } elseif ($level -eq "Medium") {
                    $score -= 2
                } elseif ($level -eq "Low") {
                    $score -= 1
                }
            }

            return [Math]::Max(0, $score)
        }

        Start-ZmsTranscript -ScriptName (Split-Path -Leaf $PSCommandPath) -VerboseLogging:$VerboseLogging

        try {
            New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
            New-Item -ItemType Directory -Force -Path $rawDirectory | Out-Null
            "ZMS read-only SharePoint discovery started at $($scanStartedAt.ToString("o"))" | Set-Content -LiteralPath $scanLogPath -Encoding UTF8

            $config = Get-ZmsConfig -ConfigPath $ConfigPath
            $effectiveClientId = if ($ClientId) { $ClientId } else { $config.clientIdPlaceholder }

            if ([string]::IsNullOrWhiteSpace($effectiveClientId) -or $effectiveClientId -eq "PASTE-PNP-ENTRA-APP-CLIENT-ID-HERE" -or $effectiveClientId -eq "PNP_CLIENT_ID_PLACEHOLDER") {
                throw "ClientId is required for live read-only discovery."
            }

            foreach ($site in Get-ZmsSiteCollections -Config $config) {
                Write-ZmsStep "Read-only discovery for $($site.url)"
                $siteStartedAt = Get-Date

                try {
                    Connect-ZmsDiscoverySharePointSite -Url $site.url -ClientId $effectiveClientId
                    $webs = @(Get-ZmsWebsToScan -SiteUrl $site.url -SiteTitle $site.title)
                    $siteLibraries = @()
                    $siteLists = @()
                    $siteFields = @()
                    $sitePermissions = @()
                    $siteFiles = @()
                    $siteFolders = @()
                    $siteStorageBytes = 0
                    $siteSubsites = @(
                        $webs |
                            Where-Object { -not $_.isRoot } |
                            ForEach-Object {
                                [PSCustomObject]@{
                                    id = [guid]::NewGuid().ToString("D")
                                    title = $_.title
                                    url = $_.url
                                    description = "Discovered subsite under $($_.parentUrl)"
                                }
                            }
                    )

                    $script:inventoryItems += New-ZmsInventoryItem -SiteCollection $site.title -ItemType "Site Collection" -Path $site.url

                    foreach ($webInfo in $webs) {
                        Connect-ZmsDiscoverySharePointSite -Url $webInfo.url -ClientId $effectiveClientId
                        $web = Get-PnPWeb -Includes Title,Url,Lists,RoleAssignments,HasUniqueRoleAssignments -ErrorAction Stop
                        $subsiteName = if ($webInfo.isRoot) { "" } else { $webInfo.title }

                        $script:rawSites += [PSCustomObject]@{
                            title = $web.Title
                            url = $web.Url
                            parentUrl = $webInfo.parentUrl
                            hasUniqueRoleAssignments = [bool]$web.HasUniqueRoleAssignments
                        }

                        if (-not $webInfo.isRoot) {
                            $script:inventoryItems += New-ZmsInventoryItem -SiteCollection $site.title -Subsite $webInfo.title -ItemType "Subsite" -Path $webInfo.url
                        }

                        if ($IncludePermissions -and $web.HasUniqueRoleAssignments) {
                            $summary = Get-ZmsPermissionSummary -ClientObject $web
                            $finding = Add-ZmsPermissionRisk -Site $site.title -Location $web.Url -InheritanceStatus "Unique" -PermissionSummary $summary -RiskLevel (Get-ZmsRiskLevelFromText $web.Url) -RecommendedAction "Review unique web permissions and map target access groups."
                            $sitePermissions += $finding
                        }

                        $lists = @(
                            Get-PnPList -Includes Title,Description,BaseTemplate,BaseType,Hidden,ItemCount,RootFolder,HasUniqueRoleAssignments,Fields,ContentTypes,RoleAssignments -ErrorAction Stop |
                                Where-Object { -not (Test-ZmsSystemList -List $_) }
                        )

                        foreach ($list in $lists) {
                            $isLibrary = ([string]$list.BaseType -eq "DocumentLibrary" -or [int]$list.BaseTemplate -eq 101)
                            $rootFolder = Get-PnPProperty -ClientObject $list -Property RootFolder -ErrorAction SilentlyContinue
                            $listPath = if ($rootFolder -and $rootFolder.ServerRelativeUrl) { [string]$rootFolder.ServerRelativeUrl } else { $list.Title }
                            $permissionStatus = if ($list.HasUniqueRoleAssignments) { "Broken" } else { "Inherited" }
                            $riskLevel = if ($list.HasUniqueRoleAssignments) { Get-ZmsRiskLevelFromText "$($list.Title) $listPath" } else { Get-ZmsRiskLevelFromText "$($list.Title) $listPath" }

                            $fields = @()
                            if ($IncludeMetadata) {
                                try {
                                    foreach ($field in @(Get-PnPProperty -ClientObject $list -Property Fields -ErrorAction Stop)) {
                                        $fieldRecord = New-ZmsMetadataField -Field $field
                                        $fields += $fieldRecord
                                        if (-not $fieldRecord.hidden) {
                                            $script:rawFields += [PSCustomObject]@{
                                                site = $site.title
                                                webUrl = $web.Url
                                                list = $list.Title
                                                title = $fieldRecord.name
                                                internalName = $fieldRecord.internalName
                                                type = $fieldRecord.fieldType
                                                required = $fieldRecord.required
                                                hidden = $fieldRecord.hidden
                                                readOnly = $fieldRecord.readOnly
                                            }
                                        }
                                    }
                                } catch {
                                    Add-ZmsDiscoveryWarning "Could not read fields from $($list.Title) at $($web.Url). $($_.Exception.Message)"
                                }
                            }

                            $contentTypes = @()
                            try {
                                foreach ($contentType in @(Get-PnPProperty -ClientObject $list -Property ContentTypes -ErrorAction Stop)) {
                                    if ($contentType.Name) {
                                        $contentTypes += [string]$contentType.Name
                                    }
                                }
                            } catch {
                                Add-ZmsDiscoveryWarning "Could not read content types from $($list.Title). $($_.Exception.Message)"
                            }

                            $permissionEntries = @()
                            if ($IncludePermissions -and $list.HasUniqueRoleAssignments) {
                                $summary = Get-ZmsPermissionSummary -ClientObject $list
                                $permissionFinding = Add-ZmsPermissionRisk -Site $site.title -Location $listPath -InheritanceStatus "Broken" -PermissionSummary $summary -RiskLevel $riskLevel -RecommendedAction "Review unique library/list permissions and map target access groups."
                                $permissionEntries += $permissionFinding
                                $sitePermissions += $permissionFinding
                            }

                            $requiredFieldNames = if ($IncludeMetadata) { Get-ZmsRequiredFieldNames -Fields $fields } else { @() }
                            $items = if ($isLibrary -and $IncludeFiles) { @(Get-ZmsListItemsSafe -List $list -FieldNames $requiredFieldNames) } else { @() }
                            $libraryFiles = @()
                            $libraryFolders = @()
                            $missingByField = @{}
                            foreach ($requiredField in $requiredFieldNames) {
                                $missingByField[$requiredField] = 0
                            }

                            foreach ($item in $items) {
                                $path = Get-ZmsTextValue (Get-ZmsFieldValue -Item $item -Name "FileRef")
                                if ([string]::IsNullOrWhiteSpace($path)) {
                                    continue
                                }

                                $name = Get-ZmsTextValue (Get-ZmsFieldValue -Item $item -Name "FileLeafRef")
                                $itemType = if ((Get-ZmsTextValue (Get-ZmsFieldValue -Item $item -Name "FSObjType")) -eq "1") { "Folder" } else { "File" }
                                $fileSize = 0L
                                $sizeValue = Get-ZmsFieldValue -Item $item -Name "File_x0020_Size"
                                if ($null -eq $sizeValue) {
                                    $sizeValue = Get-ZmsFieldValue -Item $item -Name "SMTotalFileStreamSize"
                                }

                                if ($null -ne $sizeValue) {
                                    [long]::TryParse([string]$sizeValue, [ref]$fileSize) | Out-Null
                                }

                                foreach ($requiredField in $requiredFieldNames) {
                                    $fieldValue = Get-ZmsFieldValue -Item $item -Name $requiredField
                                    if ($null -eq $fieldValue -or [string]::IsNullOrWhiteSpace((Get-ZmsTextValue $fieldValue))) {
                                        $missingByField[$requiredField] = [int]$missingByField[$requiredField] + 1
                                    }
                                }

                                Add-ZmsPathAndContentRisks -Site $site.title -Library $list.Title -Path $path -SizeBytes $fileSize -ItemType $itemType

                                if ($itemType -eq "Folder") {
                                    $folderRecord = [PSCustomObject]@{
                                        id = [guid]::NewGuid().ToString("D")
                                        name = $name
                                        path = $path
                                        archived = ($path -match "(?i)/?archive/?|/archived/|2021|2022|2023")
                                        longPathRisk = ($path.Length -gt 250)
                                        duplicateIndicator = $false
                                        depth = @($path -split "/").Count
                                        fileCount = 0
                                        sizeBytes = 0
                                    }
                                    $libraryFolders += $folderRecord
                                    $siteFolders += $folderRecord
                                    $script:inventoryItems += New-ZmsInventoryItem -SiteCollection $site.title -Subsite $subsiteName -Library $list.Title -ItemType "Folder" -Path $path -SizeBytes 0 -MetadataCount $fields.Count -PermissionStatus $permissionStatus -RiskLevel (Get-ZmsRiskLevelFromText $path) -ReadinessStatus (Get-ZmsReadinessStatus (Get-ZmsRiskLevelFromText $path))
                                } else {
                                    $fileRecord = [PSCustomObject]@{
                                        name = $name
                                        path = $path
                                        sizeBytes = $fileSize
                                        largeFileRisk = ($fileSize -gt 104857600)
                                        longPathRisk = ($path.Length -gt 250)
                                        duplicateIndicator = $false
                                        modified = Get-ZmsTextValue (Get-ZmsFieldValue -Item $item -Name "Modified")
                                        author = Get-ZmsTextValue (Get-ZmsFieldValue -Item $item -Name "Author")
                                        editor = Get-ZmsTextValue (Get-ZmsFieldValue -Item $item -Name "Editor")
                                        contentType = Get-ZmsTextValue (Get-ZmsFieldValue -Item $item -Name "ContentType")
                                    }
                                    $libraryFiles += $fileRecord
                                    $siteFiles += $fileRecord
                                    $script:rawFiles += [PSCustomObject]@{
                                        site = $site.title
                                        webUrl = $web.Url
                                        library = $list.Title
                                        name = $name
                                        path = $path
                                        sizeBytes = $fileSize
                                        modified = $fileRecord.modified
                                        contentType = $fileRecord.contentType
                                    }
                                    $siteStorageBytes += $fileSize
                                    $script:inventoryItems += New-ZmsInventoryItem -SiteCollection $site.title -Subsite $subsiteName -Library $list.Title -ItemType "File" -Path $path -FileCount 1 -SizeBytes $fileSize -MetadataCount $fields.Count -PermissionStatus $permissionStatus -RiskLevel (Get-ZmsRiskLevelFromText $path) -ReadinessStatus (Get-ZmsReadinessStatus (Get-ZmsRiskLevelFromText $path))
                                }
                            }

                            foreach ($duplicateGroup in @($libraryFiles | Group-Object -Property name,sizeBytes | Where-Object { $_.Count -gt 1 })) {
                                foreach ($duplicateItem in $duplicateGroup.Group) {
                                    $duplicateItem.duplicateIndicator = $true
                                }

                                Add-ZmsMigrationRisk -RiskType "Duplicate Content" -Site $site.title -LibraryOrPath $list.Title -Path $duplicateGroup.Group[0].path -RiskLevel "Medium" -Description "Potential duplicate file name and size detected in the same library." -RecommendedAction "Review duplicate files before migration."
                            }

                            if ($IncludeMetadata) {
                                foreach ($field in $fields) {
                                    $missingCount = if ($missingByField.ContainsKey($field.internalName)) { [int]$missingByField[$field.internalName] } else { 0 }
                                    if ($missingCount -gt 0 -or $field.mappingRisk -ne "Low") {
                                        $mappingRisk = if ($missingCount -gt 0 -and $field.required) { "High" } else { $field.mappingRisk }
                                        $metadataFinding = [PSCustomObject]@{
                                            id = [guid]::NewGuid().ToString("D")
                                            site = $site.title
                                            library = $list.Title
                                            fieldName = $field.name
                                            fieldType = $field.fieldType
                                            required = $field.required
                                            missingValueCount = $missingCount
                                            mappedTargetField = ""
                                            mappingRisk = $mappingRisk
                                        }
                                        $script:metadataFindings += $metadataFinding

                                        if ($missingCount -gt 0) {
                                            $metadataRiskLevel = if ($field.required) { "Medium" } else { "Low" }
                                            Add-ZmsMigrationRisk -RiskType "Missing Metadata" -Site $site.title -LibraryOrPath $list.Title -Path $listPath -RiskLevel $metadataRiskLevel -Description "Metadata field '$($field.name)' has missing values in sampled items." -RecommendedAction "Clean up required metadata before migration."
                                        }
                                    }
                                }
                            }

                            if ($isLibrary) {
                                $libraryRecord = [PSCustomObject]@{
                                    id = if ($list.Id) { [string]$list.Id } else { [guid]::NewGuid().ToString("D") }
                                    title = $list.Title
                                    type = "Document Library"
                                    description = [string]$list.Description
                                    url = $listPath
                                    fileCount = $libraryFiles.Count
                                    folderCount = $libraryFolders.Count
                                    sizeBytes = ($libraryFiles | Measure-Object -Property sizeBytes -Sum).Sum
                                    brokenInheritance = [bool]$list.HasUniqueRoleAssignments
                                    hasArchivedFolders = (@($libraryFolders | Where-Object { $_.archived }).Count -gt 0)
                                    contentTypes = @($contentTypes | Sort-Object -Unique)
                                    metadataFields = @($fields | Where-Object { -not $_.hidden })
                                    permissions = $permissionEntries
                                    folders = $libraryFolders
                                    files = $libraryFiles
                                }
                                $siteLibraries += $libraryRecord
                                $script:rawLibraries += [PSCustomObject]@{
                                    site = $site.title
                                    webUrl = $web.Url
                                    title = $list.Title
                                    url = $listPath
                                    itemCount = $list.ItemCount
                                    fileCount = $libraryFiles.Count
                                    folderCount = $libraryFolders.Count
                                    hasUniqueRoleAssignments = [bool]$list.HasUniqueRoleAssignments
                                    contentTypes = @($contentTypes | Sort-Object -Unique)
                                }
                                $script:inventoryItems += New-ZmsInventoryItem -SiteCollection $site.title -Subsite $subsiteName -Library $list.Title -ItemType "Library" -Path $listPath -FileCount $libraryFiles.Count -SizeBytes $libraryRecord.sizeBytes -MetadataCount $fields.Count -PermissionStatus $permissionStatus -RiskLevel $riskLevel -ReadinessStatus (Get-ZmsReadinessStatus $riskLevel)
                            } else {
                                $listRecord = [PSCustomObject]@{
                                    id = if ($list.Id) { [string]$list.Id } else { [guid]::NewGuid().ToString("D") }
                                    title = $list.Title
                                    description = [string]$list.Description
                                    itemCount = [int]$list.ItemCount
                                    fields = @($fields | Where-Object { -not $_.hidden })
                                }
                                $siteLists += $listRecord
                                $script:rawLists += [PSCustomObject]@{
                                    site = $site.title
                                    webUrl = $web.Url
                                    title = $list.Title
                                    itemCount = [int]$list.ItemCount
                                    hasUniqueRoleAssignments = [bool]$list.HasUniqueRoleAssignments
                                }
                                $script:inventoryItems += New-ZmsInventoryItem -SiteCollection $site.title -Subsite $subsiteName -Library $list.Title -ItemType "List" -Path $listPath -FileCount ([int]$list.ItemCount) -MetadataCount $fields.Count -PermissionStatus $permissionStatus -RiskLevel $riskLevel -ReadinessStatus (Get-ZmsReadinessStatus $riskLevel)
                            }

                            $siteFields += @($fields | Where-Object { -not $_.hidden })
                        }
                    }

                    Connect-ZmsDiscoverySharePointSite -Url $site.url -ClientId $effectiveClientId
                    $siteGroups = @()
                    if ($IncludePermissions) {
                        try {
                            $siteUsers = @(Get-PnPUser -ErrorAction SilentlyContinue | ForEach-Object { if ($_.Email) { $_.Email } elseif ($_.LoginName) { $_.LoginName } else { $_.Title } })
                            foreach ($group in @(Get-PnPGroup -ErrorAction Stop)) {
                                $siteGroups += [PSCustomObject]@{
                                    id = if ($group.Id) { [string]$group.Id } else { [guid]::NewGuid().ToString("D") }
                                    name = [string]$group.Title
                                    role = ""
                                    users = @($siteUsers | Sort-Object -Unique)
                                }
                            }
                        } catch {
                            Add-ZmsDiscoveryWarning "Could not read SharePoint groups for $($site.url). $($_.Exception.Message)"
                        }
                    }

                    $siteCollection = [PSCustomObject]@{
                        id = if ($site.id) { [string]$site.id } else { [guid]::NewGuid().ToString("D") }
                        title = if ($site.title) { [string]$site.title } else { [string]$site.url }
                        url = [string]$site.url
                        department = if ($site.department) { [string]$site.department } else { "" }
                        description = if ($site.description) { [string]$site.description } else { "Live SharePoint discovery result" }
                        fileCount = $siteFiles.Count
                        folderCount = $siteFolders.Count
                        sizeBytes = $siteStorageBytes
                        subsites = $siteSubsites
                        libraries = $siteLibraries
                        lists = $siteLists
                        metadataFields = @($siteFields | Sort-Object -Property name,fieldType -Unique)
                        sharePointGroups = $siteGroups
                        permissions = $sitePermissions
                        configuredRisks = @()
                    }
                    $script:siteCollections += $siteCollection

                    $elapsed = [Math]::Round(((Get-Date) - $siteStartedAt).TotalSeconds, 1)
                    Write-ZmsDiscoveryLog -Level "INFO" -Message "Completed $($site.url) in $elapsed seconds."
                } catch {
                    Add-ZmsDiscoveryError "Failed to scan $($site.url). $($_.Exception.Message)"
                    continue
                }
            }

            $scanCompletedAt = (Get-Date).ToUniversalTime()
            $summary = [PSCustomObject]@{
                siteCollections = $script:siteCollections.Count
                subsites = (@($script:siteCollections | ForEach-Object { $_.subsites.Count }) | Measure-Object -Sum).Sum
                libraries = (@($script:siteCollections | ForEach-Object { $_.libraries.Count }) | Measure-Object -Sum).Sum
                lists = (@($script:siteCollections | ForEach-Object { $_.lists.Count }) | Measure-Object -Sum).Sum
                files = (@($script:siteCollections | ForEach-Object { $_.fileCount }) | Measure-Object -Sum).Sum
                folders = (@($script:siteCollections | ForEach-Object { $_.folderCount }) | Measure-Object -Sum).Sum
                totalStorageBytes = (@($script:siteCollections | ForEach-Object { $_.sizeBytes }) | Measure-Object -Sum).Sum
                metadataFields = $script:rawFields.Count
                permissionGroups = (@($script:siteCollections | ForEach-Object { $_.sharePointGroups.Count }) | Measure-Object -Sum).Sum
                brokenInheritanceCount = @($script:permissionRisks | Where-Object { $_.inheritanceStatus -match "(?i)broken|unique" }).Count
                longPathRisks = @($script:migrationRisks | Where-Object { $_.riskType -eq "Long Paths" }).Count
                largeFileRisks = @($script:migrationRisks | Where-Object { $_.riskType -eq "Large Files" }).Count
                missingMetadataIssues = @($script:metadataFindings | Where-Object { $_.missingValueCount -gt 0 -or $_.mappingRisk -in @("Medium", "High", "Critical") }).Count
                readinessScore = Get-ZmsReadinessScore
            }

            $scanResult = [PSCustomObject]@{
                scanId = $scanId
                scanName = "Live SharePoint Discovery"
                mode = "live-import"
                status = if ($script:errors.Count -gt 0) { "partial" } else { "completed" }
                startedAt = $scanStartedAt.ToString("o")
                completedAt = $scanCompletedAt.ToString("o")
                summary = $summary
                siteCollections = $script:siteCollections
                inventoryItems = $script:inventoryItems
                permissionRisks = $script:permissionRisks
                metadataFindings = $script:metadataFindings
                migrationRisks = $script:migrationRisks
                warnings = $script:warnings
                errors = $script:errors
            }

            ConvertTo-ZmsDiscoveryJson -Value $scanResult -Path (Join-Path $outputDirectory "scan-result.json")
            ConvertTo-ZmsDiscoveryJson -Value $summary -Path (Join-Path $outputDirectory "scan-summary.json")
            ConvertTo-ZmsDiscoveryJson -Value $script:rawSites -Path (Join-Path $rawDirectory "sites.json")
            ConvertTo-ZmsDiscoveryJson -Value $script:rawLibraries -Path (Join-Path $rawDirectory "libraries.json")
            ConvertTo-ZmsDiscoveryJson -Value $script:rawLists -Path (Join-Path $rawDirectory "lists.json")
            ConvertTo-ZmsDiscoveryJson -Value $script:rawFields -Path (Join-Path $rawDirectory "fields.json")
            ConvertTo-ZmsDiscoveryJson -Value $script:rawPermissions -Path (Join-Path $rawDirectory "permissions.json")
            ConvertTo-ZmsDiscoveryJson -Value $script:rawFiles -Path (Join-Path $rawDirectory "files.json")

            Export-ZmsDiscoveryCsv -Rows $script:inventoryItems -Headers @("siteCollection", "subsite", "library", "itemType", "path", "fileCount", "sizeBytes", "metadataCount", "permissionStatus", "riskLevel", "readinessStatus") -Path (Join-Path $outputDirectory "inventory.csv")
            Export-ZmsDiscoveryCsv -Rows $script:permissionRisks -Headers @("site", "libraryOrFolder", "inheritanceStatus", "groups", "users", "accessLevels", "riskLevel", "recommendedAction") -Path (Join-Path $outputDirectory "permissions.csv")
            Export-ZmsDiscoveryCsv -Rows $script:metadataFindings -Headers @("site", "library", "fieldName", "fieldType", "required", "missingValueCount", "mappedTargetField", "mappingRisk") -Path (Join-Path $outputDirectory "metadata.csv")
            Export-ZmsDiscoveryCsv -Rows $script:migrationRisks -Headers @("riskType", "site", "libraryOrPath", "path", "riskLevel", "description", "recommendedAction") -Path (Join-Path $outputDirectory "risks.csv")

            Write-ZmsSuccess "Read-only discovery exported to $outputDirectory"
            if ($script:errors.Count -gt 0) {
                $exitCode = 2
                Write-ZmsWarning "Discovery completed with partial results. Review scan-log.txt and scan-result.json errors."
            }
        } catch {
            Add-ZmsDiscoveryError $_.Exception.Message
            $exitCode = 1
        } finally {
            Stop-ZmsTranscript
        }

        exit $exitCode
        """;
    }
}
