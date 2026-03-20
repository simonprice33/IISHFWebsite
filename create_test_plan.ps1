$outputPath = "C:\Projects\iishf\IISHFWebsite\ITC_Validation_Test_Plan.xlsx"

$data = @(
    @{Section="1. Team-Level Fields"; Num="1.1"; Scenario="Submit with no team name selected"; Expected="Error: team name required"},
    @{Section="1. Team-Level Fields"; Num="1.2"; Scenario="Submit with no team signatory entered"; Expected="Error: signatory required"},
    @{Section="1. Team-Level Fields"; Num="1.3"; Scenario="Enter the team name as the signatory (same value in both fields)"; Expected="Error: signatory cannot match team name"},
    @{Section="1. Team-Level Fields"; Num="1.4"; Scenario="Submit with no issuing country selected"; Expected="Error: issuing country required"},
    @{Section="1. Team-Level Fields"; Num="1.5"; Scenario="All four team-level fields valid"; Expected="No team-level errors"},

    @{Section="2. Player Row - Field Validation"; Num="2.1"; Scenario="License number is blank"; Expected="Invalid license number at row X"},
    @{Section="2. Player Row - Field Validation"; Num="2.2"; Scenario="License number contains letters (e.g. ABC123)"; Expected="Invalid license number at row X"},
    @{Section="2. Player Row - Field Validation"; Num="2.3"; Scenario="Jersey number is blank"; Expected="Invalid jersey number at row X"},
    @{Section="2. Player Row - Field Validation"; Num="2.4"; Scenario="Jersey number contains letters"; Expected="Invalid jersey number at row X"},
    @{Section="2. Player Row - Field Validation"; Num="2.5"; Scenario="First name is blank"; Expected="Player name first is required"},
    @{Section="2. Player Row - Field Validation"; Num="2.6"; Scenario="Last name is blank"; Expected="Player last name is required"},
    @{Section="2. Player Row - Field Validation"; Num="2.7"; Scenario="Date of birth is blank"; Expected="Date of birth is required"},
    @{Section="2. Player Row - Field Validation"; Num="2.8"; Scenario="Nationality is blank"; Expected="Nationality is required"},
    @{Section="2. Player Row - Field Validation"; Num="2.9"; Scenario="Role is blank"; Expected="Roster member role is required"},
    @{Section="2. Player Row - Field Validation"; Num="2.10"; Scenario="Gender is blank"; Expected="Roster member gender is required"},
    @{Section="2. Player Row - Field Validation"; Num="2.11"; Scenario="All player row fields valid"; Expected="No row-level errors"},

    @{Section="3. Age Validation - Non-Title Event"; Num="3.1"; Scenario="U13 - Male born in event year (turns 13)"; Expected="Valid"},
    @{Section="3. Age Validation - Non-Title Event"; Num="3.2"; Scenario="U13 - Male born year before (turns 14)"; Expected="Invalid"},
    @{Section="3. Age Validation - Non-Title Event"; Num="3.3"; Scenario="U13 - Female born in event year (turns 13)"; Expected="Valid"},
    @{Section="3. Age Validation - Non-Title Event"; Num="3.4"; Scenario="U13 - Female born 2 years before (turns 15)"; Expected="Invalid"},
    @{Section="3. Age Validation - Non-Title Event"; Num="3.5"; Scenario="U16 - Male turns 15 in event year"; Expected="Valid"},
    @{Section="3. Age Validation - Non-Title Event"; Num="3.6"; Scenario="U16 - Male turns 16 in event year"; Expected="Invalid"},
    @{Section="3. Age Validation - Non-Title Event"; Num="3.7"; Scenario="U16 - Female turns 16 in event year"; Expected="Valid"},
    @{Section="3. Age Validation - Non-Title Event"; Num="3.8"; Scenario="U16 - Female turns 17 in event year"; Expected="Invalid"},
    @{Section="3. Age Validation - Non-Title Event"; Num="3.9"; Scenario="U19 - Male turns 18 in event year"; Expected="Valid"},
    @{Section="3. Age Validation - Non-Title Event"; Num="3.10"; Scenario="U19 - Male turns 19 in event year"; Expected="Invalid"},
    @{Section="3. Age Validation - Non-Title Event"; Num="3.11"; Scenario="U19 - Female turns 19 in event year"; Expected="Valid"},
    @{Section="3. Age Validation - Non-Title Event"; Num="3.12"; Scenario="U19 - Female turns 20 in event year"; Expected="Invalid"},
    @{Section="3. Age Validation - Non-Title Event"; Num="3.13"; Scenario="U10 - Player turns 9 in event year"; Expected="Valid"},
    @{Section="3. Age Validation - Non-Title Event"; Num="3.14"; Scenario="U10 - Player turns 10 in event year"; Expected="Invalid"},
    @{Section="3. Age Validation - Non-Title Event"; Num="3.15"; Scenario="Senior - Player turns 19 in event year"; Expected="Valid"},
    @{Section="3. Age Validation - Non-Title Event"; Num="3.16"; Scenario="Senior - Player turns 18 in event year"; Expected="Invalid"},
    @{Section="3. Age Validation - Non-Title Event"; Num="3.17"; Scenario="Senior Women - Player turns 16 in event year"; Expected="Valid"},
    @{Section="3. Age Validation - Non-Title Event"; Num="3.18"; Scenario="Senior Women - Player turns 15 in event year"; Expected="Invalid"},
    @{Section="3. Age Validation - Non-Title Event"; Num="3.19"; Scenario="Masters - Player turns 45 in event year"; Expected="Valid"},
    @{Section="3. Age Validation - Non-Title Event"; Num="3.20"; Scenario="Masters - Player turns 44 in event year"; Expected="Invalid"},
    @{Section="3. Age Validation - Non-Title Event"; Num="3.21"; Scenario="Women - Player turns 35 in event year"; Expected="Valid"},
    @{Section="3. Age Validation - Non-Title Event"; Num="3.22"; Scenario="Women - Player turns 34 in event year"; Expected="Invalid"},

    @{Section="4. Age Validation - Title Event (Non-Championship)"; Num="4.1"; Scenario="U13 - Male turns 14 (1 extra year allowed)"; Expected="Valid"},
    @{Section="4. Age Validation - Title Event (Non-Championship)"; Num="4.2"; Scenario="U13 - Male turns 15"; Expected="Invalid"},
    @{Section="4. Age Validation - Title Event (Non-Championship)"; Num="4.3"; Scenario="U13 - Female turns 15 (2 extra years allowed)"; Expected="Valid"},
    @{Section="4. Age Validation - Title Event (Non-Championship)"; Num="4.4"; Scenario="U13 - Female turns 16"; Expected="Invalid"},
    @{Section="4. Age Validation - Title Event (Non-Championship)"; Num="4.5"; Scenario="U16 - Male turns 16 (1 extra year)"; Expected="Valid"},
    @{Section="4. Age Validation - Title Event (Non-Championship)"; Num="4.6"; Scenario="U16 - Male turns 17"; Expected="Invalid"},
    @{Section="4. Age Validation - Title Event (Non-Championship)"; Num="4.7"; Scenario="U16 - Female turns 18 (2 extra years)"; Expected="Valid"},
    @{Section="4. Age Validation - Title Event (Non-Championship)"; Num="4.8"; Scenario="U19 - Male turns 19 (1 extra year)"; Expected="Valid"},
    @{Section="4. Age Validation - Title Event (Non-Championship)"; Num="4.9"; Scenario="U19 - Male turns 20"; Expected="Invalid"},

    @{Section="5. Roster Count Validation"; Num="5.1"; Scenario="7 players total"; Expected="Error: minimum roster count not met"},
    @{Section="5. Roster Count Validation"; Num="5.2"; Scenario="Exactly 8 players"; Expected="No count error"},
    @{Section="5. Roster Count Validation"; Num="5.3"; Scenario="Non-title: 0 netminders"; Expected="Error: minimum netminder count not met"},
    @{Section="5. Roster Count Validation"; Num="5.4"; Scenario="Non-title: exactly 1 netminder"; Expected="Valid"},
    @{Section="5. Roster Count Validation"; Num="5.5"; Scenario="Title event: only 1 netminder"; Expected="Error: minimum 2 netminders required"},
    @{Section="5. Roster Count Validation"; Num="5.6"; Scenario="Title event: 2 netminders"; Expected="Valid"},
    @{Section="5. Roster Count Validation"; Num="5.7"; Scenario="0 captains listed"; Expected="Error: missing Captain"},
    @{Section="5. Roster Count Validation"; Num="5.8"; Scenario="2 captains listed"; Expected="Error: too many Captains"},
    @{Section="5. Roster Count Validation"; Num="5.9"; Scenario="Exactly 1 captain"; Expected="Valid"},
    @{Section="5. Roster Count Validation"; Num="5.10"; Scenario="0 assistant captains listed"; Expected="Error: missing Assistant Captain"},
    @{Section="5. Roster Count Validation"; Num="5.11"; Scenario="2 assistant captains listed"; Expected="Error: too many Assistant Captains"},
    @{Section="5. Roster Count Validation"; Num="5.12"; Scenario="Exactly 1 assistant captain"; Expected="Valid"},

    @{Section="6. Bench Officials"; Num="6.1"; Scenario="Title event: no bench officials added"; Expected="Error: at least one bench official required"},
    @{Section="6. Bench Officials"; Num="6.2"; Scenario="Title event: 1 bench official with all fields completed"; Expected="Valid"},
    @{Section="6. Bench Officials"; Num="6.3"; Scenario="Non-title event: no bench officials"; Expected="No error"},
    @{Section="6. Bench Officials"; Num="6.4"; Scenario="Bench official with blank first name"; Expected="Official name first is required"},
    @{Section="6. Bench Officials"; Num="6.5"; Scenario="Bench official with blank last name"; Expected="Official last name is required"},
    @{Section="6. Bench Officials"; Num="6.6"; Scenario="Bench official with blank date of birth"; Expected="Date of birth is required"},
    @{Section="6. Bench Officials"; Num="6.7"; Scenario="Bench official with blank nationality"; Expected="Nationality is required"},
    @{Section="6. Bench Officials"; Num="6.8"; Scenario="Bench official with blank role"; Expected="Roster member role is required"},
    @{Section="6. Bench Officials"; Num="6.9"; Scenario="Bench official with blank gender"; Expected="Roster member gender is required"},
    @{Section="6. Bench Officials"; Num="6.10"; Scenario="Two bench officials with the same license number"; Expected="Error: duplicate license number"},

    @{Section="7. Duplicate Checks"; Num="7.1"; Scenario="Two players with the same license number"; Expected="Both rows flagged: Duplicate license number: X"},
    @{Section="7. Duplicate Checks"; Num="7.2"; Scenario="Two players with the same jersey number"; Expected="Both rows flagged: Duplicate jersey number: X"},
    @{Section="7. Duplicate Checks"; Num="7.3"; Scenario="Same person (name + license) in both players and bench officials"; Expected="Both rows flagged: cannot be listed as both player and bench official"},
    @{Section="7. Duplicate Checks"; Num="7.4"; Scenario="All license and jersey numbers unique"; Expected="No duplicate errors"},

    @{Section="8. Guest Player Rules (Class B / Non-Title)"; Num="8.1"; Scenario="Non-title event, 1 guest player, not a select/combination team"; Expected="Valid"},
    @{Section="8. Guest Player Rules (Class B / Non-Title)"; Num="8.2"; Scenario="Non-title event, 2 or more guest players"; Expected="Error: guest player allowance exceeded"},
    @{Section="8. Guest Player Rules (Class B / Non-Title)"; Num="8.3"; Scenario="Guest player present but no permission letter uploaded"; Expected="Error: guest player missing permission letters"},
    @{Section="8. Guest Player Rules (Class B / Non-Title)"; Num="8.4"; Scenario="Guest player with matching permission letter uploaded"; Expected="Valid"},
    @{Section="8. Guest Player Rules (Class B / Non-Title)"; Num="8.5"; Scenario="Select/combination team with multiple guests"; Expected="No guest count error (select/combination teams are exempt)"},

    @{Section="9. End-to-End Flows"; Num="9.1"; Scenario="Click Validate with all errors present"; Expected="All errors listed, no submission"},
    @{Section="9. End-to-End Flows"; Num="9.2"; Scenario="Click Save Draft with errors present"; Expected="Saves without validation (draft skips all validation)"},
    @{Section="9. End-to-End Flows"; Num="9.3"; Scenario="Click Submit with all fields valid"; Expected="Form submits successfully"},
    @{Section="9. End-to-End Flows"; Num="9.4"; Scenario="Fix all errors then click Submit"; Expected="No errors shown, submits successfully"},
    @{Section="9. End-to-End Flows"; Num="9.5"; Scenario="Submit a valid form, then click Save Draft again"; Expected="Draft saves again without showing stale errors"}
)

$xl = New-Object -ComObject Excel.Application
$xl.Visible = $false
$xl.DisplayAlerts = $false

$wb = $xl.Workbooks.Add()
$ws = $wb.Worksheets.Item(1)
$ws.Name = "ITC Validation Test Plan"

# BGR colour values for Excel COM
$brandBlue = [long]0x00DA9500   # #0095DA
$darkNavy  = [long]0x00653800   # #003865
$lightBlue = [long]0x00FBF4E8   # #E8F4FB
$white     = [long]0x00FFFFFF
$green     = [long]0x00C6EFCE
$red       = [long]0x00FFC7CE
$yellow    = [long]0x00FFEB9C

# -- Title row --
$titleRange = $ws.Range("A1:F1")
$titleRange.Merge()
$titleRange.Value2 = "ITC Validation Test Plan"
$titleRange.Font.Name = "Arial"
$titleRange.Font.Size = 16
$titleRange.Font.Bold = $true
$titleRange.Font.Color = $white
$titleRange.Interior.Color = $brandBlue
$titleRange.HorizontalAlignment = -4108
$titleRange.VerticalAlignment   = -4108
$ws.Rows.Item(1).RowHeight = 38

# -- Header row --
$headers = @("Section","Test #","Scenario","Expected Result","Pass / Fail","Notes")
for ($c = 1; $c -le 6; $c++) {
    $cell = $ws.Cells.Item(2, $c)
    $cell.Value2 = $headers[$c - 1]
    $cell.Font.Name = "Arial"
    $cell.Font.Size = 10
    $cell.Font.Bold = $true
    $cell.Font.Color = $white
    $cell.Interior.Color = $darkNavy
    $cell.HorizontalAlignment = -4108
    $cell.VerticalAlignment   = -4108
    $cell.WrapText = $true
}
$ws.Rows.Item(2).RowHeight = 28

# Freeze panes below row 2
$ws.Application.ActiveWindow.SplitRow = 2
$ws.Application.ActiveWindow.FreezePanes = $true

# -- Data rows --
$row = 3
$sectionStart = 3
$currentSection = $data[0].Section
$sectionRanges = [System.Collections.ArrayList]::new()

$validExpected = @(
    "valid","no row-level errors","no team-level errors","no count error","no error",
    "no duplicate errors","no guest count error (select/combination teams are exempt)",
    "form submits successfully","no errors shown, submits successfully",
    "draft saves again without showing stale errors",
    "saves without validation (draft skips all validation)"
)

foreach ($item in $data) {
    if ($item.Section -ne $currentSection) {
        [void]$sectionRanges.Add(@{Start=$sectionStart; End=($row-1); Name=$currentSection})
        $currentSection = $item.Section
        $sectionStart = $row
    }

    $rowBg = if (($row % 2) -eq 0) { $lightBlue } else { $white }

    $ws.Cells.Item($row, 2).Value2 = $item.Num
    $ws.Cells.Item($row, 3).Value2 = $item.Scenario
    $ws.Cells.Item($row, 4).Value2 = $item.Expected

    # Colour-code Expected Result cell
    $expLower = $item.Expected.ToLower()
    $expCell  = $ws.Cells.Item($row, 4)
    if ($validExpected -contains $expLower) {
        $expCell.Interior.Color = $green
    } elseif ($expLower -eq "invalid" -or $expLower -like "error*" -or $expLower -like "*flagged*" -or $expLower -like "*required*" -or $expLower -like "all errors listed*") {
        $expCell.Interior.Color = $red
    } else {
        $expCell.Interior.Color = $yellow
    }

    for ($c = 2; $c -le 6; $c++) {
        $cell = $ws.Cells.Item($row, $c)
        $cell.Font.Name = "Arial"
        $cell.Font.Size = 10
        $cell.WrapText  = $true
        $cell.VerticalAlignment = -4108
        if ($c -ne 4) { $cell.Interior.Color = $rowBg }
    }
    $ws.Rows.Item($row).RowHeight = 36
    $row++
}
[void]$sectionRanges.Add(@{Start=$sectionStart; End=($row-1); Name=$currentSection})

# -- Section column merges --
$sectionBgs = @(
    [long]0x00F0E6D5,  # warm peach
    [long]0x00FBF4E8,  # light blue
    [long]0x00F5E8F5,  # light purple
    [long]0x00E8F5E8,  # light green
    [long]0x00FDF5E6,  # light amber
    [long]0x00E8F0F5,  # steel blue
    [long]0x00F5E8E8,  # rose
    [long]0x00F5F5E8,  # cream
    [long]0x00E8F5F5   # cyan
)
$si = 0
foreach ($sr in $sectionRanges) {
    $rng = $ws.Range($ws.Cells.Item($sr.Start, 1), $ws.Cells.Item($sr.End, 1))
    $rng.Merge()
    $rng.Value2 = $sr.Name
    $rng.Font.Name = "Arial"
    $rng.Font.Size = 10
    $rng.Font.Bold = $true
    $rng.Interior.Color = $sectionBgs[$si % $sectionBgs.Count]
    $rng.HorizontalAlignment = -4108
    $rng.VerticalAlignment   = -4108
    $rng.WrapText = $true
    $si++
}

# -- Column widths --
$ws.Columns.Item(1).ColumnWidth = 28
$ws.Columns.Item(2).ColumnWidth = 8
$ws.Columns.Item(3).ColumnWidth = 55
$ws.Columns.Item(4).ColumnWidth = 46
$ws.Columns.Item(5).ColumnWidth = 12
$ws.Columns.Item(6).ColumnWidth = 28

# -- Borders --
$allCells = $ws.Range($ws.Cells.Item(1,1), $ws.Cells.Item($row-1, 6))
$allCells.Borders.LineStyle = 1
$allCells.Borders.Weight    = 2

# -- Pass/Fail dropdown validation --
$pvRange = $ws.Range($ws.Cells.Item(3,5), $ws.Cells.Item($row-1, 5))
$dv = $pvRange.Validation
$dv.Delete()
$dv.Add(3, 1, 1, "Pass,Fail,N/A")
$dv.ShowInput = $true
$dv.ShowError = $false

# -- Conditional formatting on Pass/Fail column --
$cfRange = $ws.Range($ws.Cells.Item(3,5), $ws.Cells.Item($row-1, 5))
$cf1 = $cfRange.FormatConditions.Add(1, 3, "Pass")
$cf1.Interior.Color = $green
$cf1.Font.Bold = $true
$cf2 = $cfRange.FormatConditions.Add(1, 3, "Fail")
$cf2.Interior.Color = $red
$cf2.Font.Bold = $true

$wb.SaveAs($outputPath, 51)
$wb.Close($false)
$xl.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($xl) | Out-Null

Write-Output "Saved: $outputPath"
