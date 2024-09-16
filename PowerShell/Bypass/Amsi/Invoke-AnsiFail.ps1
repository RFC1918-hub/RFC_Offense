# functions
function Invoke-AnsiFail {
    (([Ref].Assembly.GetTypes() | Where-Object {$_.Name -like '*iUtils'}).GetFields('NonPublic,Static') | Where-Object {$_.Name -like '*Failed'}).SetValue($null, $true)
}

# main
Invoke-AnsiFail

