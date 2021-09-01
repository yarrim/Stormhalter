param($filter = ".")
write-host @"
This script checks any Stormhalter .Mapproj files in the current path for gameplay or style issues. It outputs a list of all the issues as a text table.
If you want to review these entries more closely, open this script in Powershell_ISE.exe and look at the code.
Korazail, on the Stormhalter discord, can may be able to help with fixing issues this script identifies.
"@

$columnWalls = "32,33,34,143,149,159,225" #these walls are exempted from 'Needs a ruin' type checks because they are columns.

$issues = @()
$Segments = Get-ChildItem *.mapproj |  ? {$_.name -match $filter} | % {
    $Source = [xml](gc $_)
    $Segment = $Source.segment.name

    #    Static tiles instead of floors. Find places a staticcomponent is used without an indestructible wall, counter, altar, obstruction or similar floor-type component.
    $Source.SelectNodes("//tile[component[@type='StaticComponent' and not(static='114')] and 
                         not(component[@type='FloorComponent'] or component[@type='WaterComponent'] or component[@type='IceComponent'] or component[@type='StaircaseComponent']) and 
                         not(component[@type='WallComponent' and indestructible='true'] or component[@type='CounterComponent'] or component[@type='AltarComponent'] or component[@type='ObstructionComponent'])       ]") | % {
                         $issues += [pscustomobject][ordered]@{Segment = $segment;RegionID=$_.parentnode.id;RegionName=$_.parentnode.name;X =$_.x;Y=$_.y;Issue="Issue: Walkable StaticComponent without a floor-type"}}

    #    at least one WallComponent that is destructible
    #    no WallComponents that are indestructible 
    #    no floor, water or ice component underneath.
    #Similar to the above static-as-floor, this also shows places a breakable wall has NO floor under it.
    $source.selectNodes("//tile[component[@type='WallComponent' and not(indestructible='true')] and
                         not(component[@type='WallComponent' and indestructible='true']) and 
                         not(component[@type='FloorComponent' or @type='WaterComponent' or @type='IceComponent'])]") | % {
                         $issues += [pscustomobject][ordered]@{Segment = $segment;RegionID=$_.parentnode.id;RegionName=$_.parentnode.name;X =$_.x;Y=$_.y;Issue="Issue: Destructible wall without a floor-type component"}}

    #    Indestructible walls over a FloorComponent instead of staticcomponent
    $source.selectNodes("//tile[component[@type='WallComponent' and indestructible='true'] and
                         (component[@type='FloorComponent' or @type='WaterComponent' or @type='IceComponent'])]") | % {
                         $issues += [pscustomobject][ordered]@{Segment = $segment;RegionID=$_.parentnode.id;RegionName=$_.parentnode.name;X =$_.x;Y=$_.y;Issue="Warn: Indestructible wall with a Floor-type component"}}

    # TODO access to void/ see if we can find places where there is no neighboring tile, but the tile is walkable.

    #    more than one floor or water component
    #    most of these seem to be bridges. better to have a floor component and a static?
    $source.SelectNodes("//tile[count(component[@type='WaterComponent' or @type='FloorComponent' or @type='IceComponent'])>1]") | % {
                         $issues += [pscustomobject][ordered]@{Segment = $segment;RegionID=$_.parentnode.id;RegionName=$_.parentnode.name;X =$_.x;Y=$_.y;Issue="Warn: Multiple Floor-type components"}}

    #    WaterComponents, but a movementCost not 3. 
    $source.SelectNodes("//tile[component[@type='WaterComponent' and not(movementCost=3)]]") | % {
                         $issues += [pscustomobject][ordered]@{Segment = $segment;RegionID=$_.parentnode.id;RegionName=$_.parentnode.name;X =$_.x;Y=$_.y;Issue="Issue: Water with a non-standard movementCost"}}

    #    WallComponents that are not Indestructible but have a ruins static of 0
    $source.selectNodes("//tile[component[@type='WallComponent' and not(indestructible='true' or wall='32' or wall='33') and (ruins='0' and destroyed='0')]]") | ? {$_.wall -notin $columnWalls.Split(",") } | % { # more exceptions will need to be added here as more tile sets are processed. This covers town and dungeon corners (32\33)
                         $issues += [pscustomobject][ordered]@{Segment = $segment;RegionID=$_.parentnode.id;RegionName=$_.parentnode.name;X =$_.x;Y=$_.y;Issue="Style: Destructible wall without a ruins or destroyed static defined"}}

    #    Multiple components with same static ID
    $(foreach ($tile in $Source.SelectNodes("//tile[count(component[@type='StaticComponent' or @type='FloorComponent' or @type='WaterComponent' or @type='IceComponent' or @type='WallComponent'])>1]")) {
        $tile.selectnodes("./component") | select @{n='tile';e={$tile}}, @{n="staticID";e={$_.static + $_.ground + $_.water +$_.wall}}| group staticid
    }) | ? {$_.count -gt 1} | % {$issues += [pscustomobject][ordered]@{Segment = $segment;RegionID=$_.group.tile[0].parentnode.id;RegionName=$_.group.tile[0].parentnode.name;X =$_.group.tile[0].x;Y=$_.group.tile[0].y;Issue="Warn: Multiple components with same static: $($_.name)"}}
}
$issues | ft

continue;#ends normal script. The following can be used in Powershell ISE to work with the segment files manually, but won't be executed if you run the ps1.

#Some things can be fixed in WorldForge, some can be handled in bulk via XML manipulation. When using this method to bulk change, open 
#the file in WorldForge and re-save it to fix prettyprinting so the diff doesn't explode

#read
$targetFile = ".\Bloodlands-cleaned.mapproj"
$segment = [xml](get-content $targetFile)
#write
$Segment.OuterXml | Set-Content $targetFile #remember to re-save in worldforge


#this helper function uses one call to either override an existing subelement "property" or to create one if the element doesn't already exist.
filter Set-ElementProperty {
param ([Parameter(Mandatory,ValueFromPipeline)][System.Xml.XmlElement]$Target,[string]$Property,[string]$Value)
    if (@($Target.selectNodes("./$Property")).count -eq 0) { #element doesn't exist, create it
        $Element = $Target.OwnerDocument.CreateElement($Property)
        $Element.InnerText=$Value
        [void]($target.AppendChild($Element))
    } else { #element exists, just overwrite.
        $Target.$Property=$Value
    }
}

#Walls that can be blown generally need a destroyed and/or ruin static.
#these walls don't have one, but there are likely to be big chunks we can fix all at once:
$walls = $segment.selectNodes("//component[@type='WallComponent' and not(indestructible='true') and (ruins='0' and destroyed='0')]")
$walls | ? {$_.wall -notin $columnWalls.Split(",") }| group wall #notin excludes columns that don't need rubble.
<#-- 
results when I ran this on Kes:
Count Name                      Group                                                                                                                                                                                               
----- ----                      -----                                                                                                                                                                                               
  336 31                        {component, component, component, component...} 
    1 33                        {component}                                                                                                                                                                                         
   42 32                        {component, component, component, component...}                                                                                                                                                     
   24 104                       {component, component, component, component...}                                                                                                                                                     
    5 168                       {component, component, component, component...}                                                                                                                                                     
    1 409                       {component}                                                                                                                                                                                         
--#>
# In this case, the 33 and 32s are the little column doodads and can be ignored. 
# the 31s are the big Granite pillars and ought to all be made indestructible with limited exceptions.
# the 104s are the spikes around portals and should indestructible
# the 168s are the pillars to the east of the -2 leng portal. These need a ruin, but can probably have no destroyed. 170 looks good
# the 409 is a pillar doodad in the hall of legends that should be indestructible to match its neighbors.

#And here are the bulk changes:
# All the Wall=104 Portal spikes should be indestructible.
$Segment.SelectNodes("//component[@type='WallComponent' and wall='104']") | Set-ElementProperty -Property indestructible -Value true
# All the Wall=31 pillars should be indestructible.
$Segment.SelectNodes("//component[@type='WallComponent' and wall='31']") | Set-ElementProperty -Property indestructible -Value true
# all the 168s should get a ruin of 170
$Segment.SelectNodes("//component[@type='WallComponent' and wall='168']") | Set-ElementProperty -Property ruins -Value 170

#Here's another fun one:
# When a tile has an indestructible wall on it, any Floor\Water\Ice components can be changed to static, since they will never be navigable. 
# Difference between a Static and a Floor\water\ice is the 'Ground' element and movementCost. Cache the ground ID, then remove all children and add it back as a static element
$segment.selectNodes("//tile[component[@type='WallComponent' and indestructible='true'] and (component[@type='FloorComponent' or @type='WaterComponent' or @type='IceComponent'])]") | % {
    $_.selectnodes("./component[@type='FloorComponent' or @type='WaterComponent' or @type='IceComponent']") | % {
        $_.setAttribute("type","StaticComponent")
        $static = $_.ground
        foreach ($child in $_.childnodes) { $_.removeChild($child) }
        $_ | Set-ElementProperty -Property static -Value $static
    }
}

#Secret Doors audit:
#Find doors and compare their secretID to nearby WallComponents to see if they match.
$segment.SelectNodes("//component[@type='DoorComponent' and isSecret='true']") | % {
$tile = $_.parentnode; $secretid = $_.secretid
$neighbors = ($tile.SelectSingleNode("../tile[@x='$([int]$tile.x-1)' and @y='$([int]$tile.y)']"),
              $tile.SelectSingleNode("../tile[@x='$([int]$tile.x+1)' and @y='$([int]$tile.y)']"),
              $tile.SelectSingleNode("../tile[@x='$([int]$tile.x)' and @y='$([int]$tile.y-1)']"),
              $tile.SelectSingleNode("../tile[@x='$([int]$tile.x)' and @y='$([int]$tile.y+1)']"))
$neighborWallids = $neighbors | % {$_.selectnodes("./component[@type='WallComponent']").wall}
if ($_.secretId -notin $neighborWallids) { $tile | select @{n='Region';e={$_.parentnode.name}}, x, y , @{n='secretid';e={$secretid}}, @{n='neighbors';e={$neighborWallids -join ", "}}} 
} | ft


#Replace statics with a specific static ID with a floorcomponent.
$segment.selectNodes("//component[@type='StaticComponent' and static='13']") | % {
    $static = $_.static
    foreach ($child in $_.childnodes) { $_.removeChild($child) }
    $_.setAttribute("type","FloorComponent")
    $_ | Set-ElementProperty -Property ground -Value $static
}


#Find places where there are duplicate floors or statics and remove one of them
$(foreach ($tile in $segment.SelectNodes("//tile[count(component[@type='StaticComponent' or @type='FloorComponent' or @type='WaterComponent' or @type='IceComponent' or @type='WallComponent'])>1]")) {
        $tile.selectnodes("./component") | select @{n='tile';e={$tile}}, @{n="staticID";e={$_.static + $_.ground + $_.water +$_.wall}}| group staticid
    }) | ? {$_.count -gt 1} | % {
    $_.group[0].tile} | % { #deduplicate becuase we aggregated based on components, and need to get to a parent tile
        $floors = $_.selectnodes("./component[@type='FloorComponent']")
        if ($floors.count -gt 1 -and ($floors.ground | select -Unique).count -eq 1) {$_;$floors[($floors.count -1)].parentnode.removeChild($floors[($floors.count -1)])} # if we have more than one floor, and all the floors have the same static, remove the last floor
        $floors = $_.selectnodes("./component[@type='WaterComponent']")
        if ($floors.count -gt 1 -and ($floors.water | select -Unique).count -eq 1) {$_;$floors[($floors.count -1)].parentnode.removeChild($floors[($floors.count -1)])} # if we have more than one floor, and all the floors have the same static, remove the last floor
        $floors = $_.selectnodes("./component[@type='IceComponent']")
        if ($floors.count -gt 1 -and ($floors.ice | select -Unique).count -eq 1) {$_;$floors[($floors.count -1)].parentnode.removeChild($floors[($floors.count -1)])} # if we have more than one floor, and all the floors have the same static, remove the last floor
        $floors = $_.selectnodes("./component[@type='StaticComponent']")
        if ($floors.count -gt 1 -and ($floors.static | select -Unique).count -eq 1) {$_;$floors[($floors.count -1)].parentnode.removeChild($floors[($floors.count -1)])} # if we have more than one floor, and all the floors have the same static, remove the last floor
    }


