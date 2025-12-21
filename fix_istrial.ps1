$file = 'c:\SR3H_MACRO\Services\MacroFortActivationService.cs'
$content = [System.IO.File]::ReadAllText($file, [System.Text.Encoding]::UTF8)

# Add IsTrial to GetSubscriptionByEmailAsync
$oldPattern = '                                LastDeviceTransferDate = sub.last_device_transfer_date != null ? DateTime.Parse(sub.last_device_transfer_date.ToString()) : (DateTime?)null
                            };'

$newPattern = '                                LastDeviceTransferDate = sub.last_device_transfer_date != null ? DateTime.Parse(sub.last_device_transfer_date.ToString()) : (DateTime?)null,
                                IsTrial = sub.is_trial ?? false
                            };'

$content = $content -replace [regex]::Escape($oldPattern), $newPattern
[System.IO.File]::WriteAllText($file, $content, [System.Text.Encoding]::UTF8)

Write-Host 'IsTrial mapping added to GetSubscriptionByEmailAsync'
